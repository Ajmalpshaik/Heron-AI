// Heron-Agent:  HERON-REVIT-UI-022
// Heron-Step:   5
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Documents;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The progress banner - docs/28's HERON-REVIT-UI-022, and the thing
    /// docs/25 section 6 calls "the cheapest trust feature in the platform".
    ///
    /// WHAT IT IS FOR, IN ONE SENTENCE. Revit runs one thing at a time on the
    /// thread that also draws the screen, so while Heron works Revit is
    /// genuinely frozen - and a frozen application with no explanation reads
    /// as a crash, while the same freeze with a banner reads as progress
    /// (docs/25 section 6a, settled as the answer instead of removing the
    /// freeze). Until this file existed the only thing Revit showed was the
    /// ribbon button's connected picture, which says a pipe is open and says
    /// NOTHING about whether anything is happening right now.
    ///
    /// THE ONE CONSTRAINT THAT DECIDES THE WHOLE DESIGN. Revit draws on the
    /// same thread it works on, so for as long as a job runs Revit can paint
    /// nothing at all. A banner living on that thread is therefore a STILL
    /// PICTURE for precisely the stretch it exists to cover.
    ///
    /// That was measured rather than assumed. With the thread blocked for
    /// 3000 ms the way Execute blocks it, a DispatcherTimer on that thread
    /// fired ZERO times and the sweep did not advance by one pixel - 357.6 px
    /// before, 357.6 px after. The same test with the thread free: nine ticks
    /// a second and a moving sweep.
    ///
    /// SO THE BANNER DOES NOT LIVE ON REVIT'S THREAD. It runs its own STA
    /// thread with its own dispatcher, started in the constructor, and that
    /// dispatcher goes on pumping while Revit's is blocked. This is what lets
    /// the sweep actually sweep and the elapsed time actually count - during
    /// the freeze, which is the only time either is worth anything.
    ///
    /// Begin and End are still called from where they always were. They now
    /// post ACROSS to this thread rather than onto Revit's, and because both
    /// post, they arrive in the order they were called.
    ///
    /// THE THREAD IS A BACKGROUND THREAD, deliberately: it must never be the
    /// reason Revit's process refuses to exit.
    ///
    /// IT SAYS WHETHER HERON IS READING OR CHANGING, and that is the half the
    /// earlier project never had. "Something is happening" is worth little to
    /// somebody whose real question is whether his model is being touched.
    /// The read/change split comes from HeronOperationRegistry BY NAME - the
    /// caller decides which operation to ask for and never how dangerous it
    /// is (Golden Rule 19), so no text arriving on the pipe can make a write
    /// wear the reading colour.
    ///
    /// IT NAMES THE MODEL, and without that the title is only half an
    /// answer. "Heron AI is changing your model" is reassuring exactly until
    /// you remember two Revits are open, and then it is the opposite - the
    /// one question it raises is the one it does not answer. The name comes
    /// in as a plain string from the caller; this file learns nothing about
    /// documents and keeps its one useful property, that it can be compiled
    /// and driven with no Revit anywhere near it.
    ///
    /// IT ALSO SAYS HOW IT ENDED. A refusal - Revit busy, writing switched
    /// off, nothing selected - used to be invisible from Revit: the chat got
    /// a sentence and the screen showed nothing at all. The outcome line
    /// holds for a moment after the work so the answer is where the person is
    /// looking.
    ///
    /// THE COUNT IS KEPT OFF THE DISPATCHER, and that is not a detail. Begin
    /// arrives from a listener thread and End from Revit's own, so two
    /// different threads move it and neither can be trusted to have seen the
    /// other's work. When End still ran INLINE on Revit's thread while Begin
    /// was posted, an End could overtake the Begin of its own job: the
    /// stranded Begin then raised a banner nothing was left to lower, and it
    /// stayed up over an idle Revit until Revit was restarted - D-56, seen
    /// 2026-09-08.
    ///
    /// Both now post to the banner's own thread, which removes that
    /// particular overtaking. The rule stays anyway, because it never
    /// depended on the overtaking: a count that is only correct while the
    /// threading happens to cooperate is not a count.
    ///
    /// So the count is the truth and the drawing merely follows it. Begin and
    /// End move the count with Interlocked ON THE CALLER'S THREAD, before the
    /// hop; the posted action only RENDERS whatever the count says at the
    /// moment it finally runs. An action that arrives late is then a no-op,
    /// which is exactly what it should be.
    ///
    /// NOTHING HERE MAY THROW. A cosmetic fault must never cost a request, so
    /// every entry point swallows and logs. The banner failing means no
    /// banner; it does not mean a failed job.
    ///
    /// NOT A DIALOG, AND NOT CLICKABLE. It is click-through
    /// (WS_EX_TRANSPARENT), never activated, and has no taskbar entry, so it
    /// cannot steal a click or the focus from the person using Revit -
    /// Golden Rule 8, background work must not interfere with user work.
    /// </summary>
    internal sealed class HeronActivityBanner
    {
        // BACK TO 460, where this started. It went to 560 while the name
        // and the outcome shared the second line and would not both fit;
        // giving the name a line of its own is what made the width
        // unnecessary again. Widening and heightening for the same reason
        // would have paid twice for one problem.
        private const double BannerWidth = 460;

        // 98 rather than 78, for the third line. That is 20 px more over a
        // ribbon the card is click-through above anyway, and it buys each
        // of the three a full width of its own: the model no longer
        // competes with the job for room, so neither has to trim.
        private const double BannerHeight = 98;
        private const double TopMargin = 12;
        private const double SweepWidth = 130;

        /// <summary>
        /// How long the finished/failed line is held before the banner hides.
        ///
        /// It is also what stops a fast job strobing: a count that takes 40 ms
        /// still leaves its answer up for this long, so the card never appears
        /// and vanishes inside one blink. A second job arriving meanwhile
        /// cancels the hide, so a batch of fragments shows one steady banner
        /// rather than twenty flashes.
        /// </summary>
        private static readonly TimeSpan OutcomeHold = TimeSpan.FromMilliseconds(1400);

        private static readonly TimeSpan SweepDuration = TimeSpan.FromSeconds(1.1);

        /// <summary>
        /// How often the elapsed time is redrawn while a job runs.
        ///
        /// Ten times a second, because the number is not there to be read
        /// precisely - it is there to be seen MOVING. The question a frozen
        /// Revit raises is "has this died?", and a counter that changes once
        /// a second answers it about as well as one that never changes at
        /// all.
        /// </summary>
        private static readonly TimeSpan ElapsedTick = TimeSpan.FromMilliseconds(100);

        private static readonly Color CardColour = Color.FromRgb(0x23, 0x26, 0x29);
        private static readonly Color TextPrimary = Color.FromRgb(0xF2, 0xF5, 0xF7);
        private static readonly Color TextSecondary = Color.FromRgb(0x9A, 0xA5, 0xAD);
        private static readonly Color ReadingColour = Color.FromRgb(0x2F, 0x9B, 0xE3);
        private static readonly Color ChangingColour = Color.FromRgb(0xF0, 0xA3, 0x2C);
        private static readonly Color DoneColour = Color.FromRgb(0x4C, 0xC3, 0x8A);
        private static readonly Color FailedColour = Color.FromRgb(0xE5, 0x53, 0x4B);

        /// <summary>
        /// The banner's OWN dispatcher, on a thread of its own - not Revit's.
        /// Null when the banner is switched off, or when the thread could not
        /// be started, and OnUi treats both the same way: no banner.
        /// </summary>
        private readonly Dispatcher _ui;
        private readonly Action<string> _log;
        private readonly bool _enabled;

        private Window _window;
        private TextBlock _title;
        private TextBlock _model;
        private TextBlock _detail;
        private TextBlock _chipText;
        private Border _chip;
        private Ellipse _lamp;
        private Border _sweep;
        private Border _track;
        private DoubleAnimation _travel;
        private DispatcherTimer _hideTimer;
        private DispatcherTimer _elapsedTimer;
        private TranslateTransform _cardTransform;
        private Border _card;
        private SolidColorBrush _titleBrush;
        private SolidColorBrush _chipBrush;
        private SolidColorBrush _lampBrush;
        private LinearGradientBrush _sweepBrush;
        private SolidColorBrush _trackBrush;
        private DoubleAnimation _lampPulse;

        /// <summary>Is the lamp already breathing? See StartLampPulse.</summary>
        private bool _lampPulsing;


        /// <summary>
        /// When the banner went up, as a Stopwatch stamp. Set when the count
        /// goes from none to one, NOT on every Begin: a batch of fragments is
        /// one banner, so it should show one running total rather than
        /// restarting at zero twenty times.
        /// </summary>
        private long _startedTicks;

        /// <summary>
        /// What the live lines say, kept so a tick can redraw them with a new
        /// time without needing the job handed to it again.
        /// </summary>
        private string _liveJob;
        private string _liveModel;

        /// <summary>
        /// How many jobs are in flight. Written from listener threads AND from
        /// Revit's thread, read on Revit's thread when drawing - so it moves
        /// with Interlocked, never with a plain ++ or --.
        /// </summary>
        private int _active;

        /// <summary>The newest job raised, and how the last one ended.</summary>
        private Raised _raised;
        private Ended _ended;

        /// <summary>
        /// Is the outcome already on screen with its hold running? Without
        /// this, a render arriving late would show the same answer again and
        /// restart the hold.
        /// </summary>
        private bool _holding;

        /// <summary>Revit's own top-level window - what the banner centres on.</summary>
        private IntPtr _host;

        /// <summary>
        /// Starts the banner's own UI thread, and is the only place that
        /// does. It no longer matters which thread constructs this - the
        /// banner brings its own.
        /// </summary>
        public HeronActivityBanner(bool enabled, Action<string> log)
        {
            _log = log ?? delegate { };
            _enabled = enabled;

            // Nothing is started for a banner that is switched off. A thread
            // nobody will ever post to is pure cost.
            if (_enabled) _ui = StartUiThread();
        }

        /// <summary>
        /// The thread the banner lives on.
        ///
        /// STA because every WPF window requires it. BACKGROUND because this
        /// must never be the thread that keeps Revit's process alive after
        /// Revit has gone.
        ///
        /// The wait is BOUNDED and it is on the CALLER'S thread, which is
        /// Revit's during OnStartup. A banner whose thread will not start is
        /// allowed to cost Revit a moment; it is not allowed to cost Revit
        /// the load. Failing returns null, and every path through OnUi
        /// already treats a null dispatcher as "no banner".
        /// </summary>
        private Dispatcher StartUiThread()
        {
            try
            {
                var ready = new ManualResetEventSlim(false);
                Dispatcher started = null;

                var thread = new Thread(delegate ()
                {
                    started = Dispatcher.CurrentDispatcher;
                    ready.Set();
                    Dispatcher.Run();
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Name = "Heron activity banner";
                thread.Start();

                if (!ready.Wait(TimeSpan.FromSeconds(5)))
                {
                    _log("Activity banner thread did not start - no banner this session.");
                    return null;
                }

                return started;
            }
            catch (Exception ex)
            {
                _log("Activity banner thread could not be created: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Raise the banner. Called from a bridge listener thread, BEFORE the
        /// work is handed to Revit - see the constraint in the class comment.
        /// </summary>
        /// <param name="job">What is being done, in the user's words.</param>
        /// <param name="changesModel">
        /// From the tool registry, looked up by operation name. Decides the
        /// colour and the word, and nothing on the wire can influence it.
        /// </param>
        /// <param name="model">
        /// Which model is about to be worked on, or null when nothing is
        /// known. This is the LAST ONE SEEN rather than the one the job will
        /// actually touch - it cannot be otherwise, because the document can
        /// only be read on Revit's thread and this is called before Revit has
        /// the job. End corrects it from the answer the work itself gave.
        /// </param>
        public void Begin(string job, bool changesModel, string model)
        {
            if (!_enabled) return;

            // COUNTED HERE, on the caller's thread, before the hop - see the
            // class comment. Counting inside the posted action is what let an
            // End overtake its own Begin.
            //
            // The clock starts on the transition from none to one, so a run
            // of fragments shows one total climbing rather than twenty
            // restarts.
            if (Interlocked.Increment(ref _active) == 1)
                Volatile.Write(ref _startedTicks, Stopwatch.GetTimestamp());
            Volatile.Write(ref _raised, new Raised(job, changesModel, model));

            OnUi(Render);
        }

        /// <summary>
        /// The work is over. Called from Revit's own thread the moment the job
        /// finishes, or from the listener thread when Revit never took it -
        /// whichever is true is the honest moment to say so.
        /// </summary>
        /// <param name="ok">Did it do what was asked?</param>
        /// <param name="outcome">Plain-English result, already translated.</param>
        /// <param name="milliseconds">
        /// How long the operation itself took, or a negative number when it
        /// never ran. It is the NEVER-RAN signal that matters here: a refusal
        /// shows no duration at all, because there was none. When it did run,
        /// the figure on the card comes from the banner's own clock instead -
        /// see ShowOutcome for why the two must not be mixed.
        /// </param>
        /// <param name="model">
        /// Which model was ACTUALLY worked on, read out of the answer the
        /// operation returned. This is the authoritative one: it is what the
        /// job did, not what was in front beforehand. Null when the operation
        /// names no document - a refusal that never reached one, for
        /// instance - and the announced name then stands.
        /// </param>
        public void End(bool ok, string outcome, long milliseconds, string model)
        {
            if (!_enabled) return;

            // The outcome is published BEFORE the count drops, so a render
            // caused by that drop cannot find the count settled and the answer
            // still missing.
            Volatile.Write(ref _ended, new Ended(ok, outcome, milliseconds, model));
            Release();

            OnUi(Render);
        }

        /// <summary>
        /// One job less in flight, and never fewer than none.
        ///
        /// Every Begin is matched by exactly one End - RevitJob.TakeBannerEnd
        /// is what guarantees the one - and Begin now counts before the job
        /// can be queued, so the floor cannot be reached. It is here because
        /// being wrong costs differently in each direction: a count stuck
        /// BELOW zero is one silent banner, a count stuck ABOVE zero is a
        /// banner that never comes down, which is the whole of D-56.
        /// </summary>
        private void Release()
        {
            while (true)
            {
                var now = Volatile.Read(ref _active);
                if (now <= 0) return;
                if (Interlocked.CompareExchange(ref _active, now - 1, now) == now) return;
            }
        }

        /// <summary>
        /// Revit's thread. Draws what the count says NOW, rather than what was
        /// true when this was posted.
        /// </summary>
        private void Render()
        {
            if (Volatile.Read(ref _active) > 0)
            {
                var job = Volatile.Read(ref _raised);
                if (job == null) return;

                StopHideTimer();
                if (!Show()) return;

                _holding = false;
                Paint(job.ChangesModel, job.Job, job.Model);
                return;
            }

            // Nothing is in flight. Say how it ended and start the hold - but
            // only once, so a render arriving after the answer is already up
            // leaves the hold running instead of restarting it.
            if (_holding || _window == null) return;

            // The answer's own model where it has one, and the announced
            // one where it does not. That is not a second guess: an
            // operation naming no document is one that never reached a
            // document, so the name already on screen is still the only
            // one in play. What it must never do is blank out at the end
            // and leave the outcome floating over no model at all.
            var end = Volatile.Read(ref _ended);
            var raised = Volatile.Read(ref _raised);
            var model = end != null && !string.IsNullOrEmpty(end.Model)
                ? end.Model
                : (raised == null ? null : raised.Model);

            _holding = true;
            ShowOutcome(end == null || end.Ok,
                        end == null ? null : end.Outcome,
                        end == null ? -1 : end.Milliseconds,
                        model);
            StartHideTimer(OutcomeHold);
        }

        /// <summary>
        /// Revit is closing. Take the window down, then the thread.
        ///
        /// Both happen inside ONE posted action, in order, because the
        /// shutdown ends the message loop - anything queued behind it never
        /// runs at all. Posting the two separately would be a race between
        /// destroying the window and destroying the thread that owns it.
        /// </summary>
        public void Shutdown()
        {
            OnUi(delegate
            {
                StopHideTimer();
                StopElapsed();
                Interlocked.Exchange(ref _active, 0);
                Destroy();

                // Last. Nothing posted after this will ever run.
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            });
        }

        // ----- the UI thread hop -------------------------------------------

        /// <summary>
        /// Runs on the banner's own thread, and never throws back at the
        /// caller.
        ///
        /// BeginInvoke rather than Invoke, always. The caller is either a
        /// listener thread or Revit's, and neither should wait on drawing:
        /// Revit's thread in particular calls End on its way out of a job,
        /// and making it wait there would put painting back on the critical
        /// path the whole design exists to keep clear.
        /// </summary>
        private void OnUi(Action action)
        {
            try
            {
                var ui = _ui;
                if (ui == null || ui.HasShutdownStarted || ui.HasShutdownFinished) return;

                if (ui.CheckAccess()) { Guarded(action); return; }

                // Send priority, so it is ahead of the rendering and idle work
                // that Revit's own ExternalEvent is queued behind.
                ui.BeginInvoke(DispatcherPriority.Send, (Action)delegate { Guarded(action); });
            }
            catch (Exception ex)
            {
                _log("Activity banner could not be reached: " + ex.Message);
            }
        }

        private void Guarded(Action action)
        {
            try { action(); }
            catch (Exception ex)
            {
                // One cosmetic fault must not cost a request, and must not
                // keep costing one either - so the window goes and the next
                // job builds a fresh one.
                _log("Activity banner failed: " + ex);
                try { Destroy(); } catch (Exception) { }
            }
        }

        // ----- what it says ------------------------------------------------

        private void AnimateColor(SolidColorBrush brush, Color to)
        {
            if (brush.Color == to) return;
            
            // Safety signal (Rule 12): do not let an animation blur Reading and Changing together.
            if (to == ReadingColour || to == ChangingColour)
            {
                brush.BeginAnimation(SolidColorBrush.ColorProperty, null);
                brush.Color = to;
            }
            else
            {
                var anim = new ColorAnimation(to, new Duration(TimeSpan.FromMilliseconds(250)));
                brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
            }
        }

        private void AnimateSweepColor(Color to)
        {
            var transparentTo = Color.FromArgb(0, to.R, to.G, to.B);
            
            _sweepBrush.GradientStops[0].BeginAnimation(GradientStop.ColorProperty, null);
            _sweepBrush.GradientStops[0].Color = transparentTo;
            
            _sweepBrush.GradientStops[1].BeginAnimation(GradientStop.ColorProperty, null);
            _sweepBrush.GradientStops[1].Color = to;
            
            _sweepBrush.GradientStops[2].BeginAnimation(GradientStop.ColorProperty, null);
            _sweepBrush.GradientStops[2].Color = to;
            
            _sweepBrush.GradientStops[3].BeginAnimation(GradientStop.ColorProperty, null);
            _sweepBrush.GradientStops[3].Color = transparentTo;
            
            AnimateColor(_trackBrush, Shade(to));
        }

        private void Paint(bool changesModel, string job, string model)
        {
            var accent = changesModel ? ChangingColour : ReadingColour;

            _title.Text = changesModel
                ? "Heron AI is changing your model"
                : "Heron AI is reading your model";
            AnimateColor(_titleBrush, TextPrimary);

            _liveModel = model;
            _liveJob = string.IsNullOrEmpty(job) ? "Working" : job;
            ShowLive();
            StartElapsed();

            _chipText.Text = changesModel ? "CHANGING" : "READING";
            AnimateColor(_chipBrush, accent);
            AnimateColor(_lampBrush, accent);
            AnimateSweepColor(accent);
            
            _track.Visibility = Visibility.Visible;

            StartAnimation();
            StartLampPulse();
        }

        /// <summary>
        /// Starts the lamp breathing, and only ONCE.
        ///
        /// Paint runs on every render and a render happens at every Begin, so
        /// a run of fragments reached this once per job. Restarting a
        /// repeating animation returns it to its first frame, so the lamp was
        /// dragged back to 0.4 at every job instead of breathing. Measured
        /// across eight jobs back to back it never once got past 0.62, where
        /// a single job left alone sweeps the full 0.40 to 1.00.
        ///
        /// The flag is cleared by StopLampPulse, which is the only thing that
        /// genuinely stops the animation - so the next job after an outcome
        /// starts a fresh pulse, and nothing else does.
        /// </summary>
        private void StartLampPulse()
        {
            if (_lampPulsing || _lamp == null || _lampPulse == null) return;

            _lamp.BeginAnimation(UIElement.OpacityProperty, _lampPulse);
            _lampPulsing = true;
        }

        /// <summary>
        /// Stops it and leaves the lamp solid - an outcome is not breathing.
        /// </summary>
        private void StopLampPulse()
        {
            if (_lamp != null)
            {
                _lamp.BeginAnimation(UIElement.OpacityProperty, null);
                _lamp.Opacity = 1.0;
            }

            _lampPulsing = false;
        }

        private void ShowOutcome(bool ok, string outcome, long milliseconds, string model)
        {
            StopElapsed();

            var accent = ok ? DoneColour : FailedColour;

            _title.Text = ok ? "Heron AI has finished" : "Heron AI stopped";

            var rest = string.IsNullOrEmpty(outcome)
                ? (ok ? "Done" : "Did not finish")
                : outcome;

            if (milliseconds >= 0)
            {
                rest += "  -  " + Elapsed(ElapsedMilliseconds());
            }

            SetDetail(model, rest);

            _chipText.Text = ok ? "DONE" : "STOPPED";
            AnimateColor(_chipBrush, accent);
            AnimateColor(_lampBrush, accent);

            StopLampPulse();

            StopAnimation();
            _track.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// The lower two lines: WHICH MODEL, then what is being done to it.
        ///
        /// A line each, because they answer different questions and a reader
        /// scanning a frozen Revit should not have to parse one line to find
        /// the model in it. Sharing a line also made them compete: the
        /// longer the project name, the less of the outcome survived, and
        /// "Writing is switched off" trimming to "Writing is switched..."
        /// is the one word that mattered going missing.
        ///
        /// WHEN THERE IS NO NAME THE LINE GOES, rather than sitting blank.
        /// An empty row in the middle of a card reads as something that
        /// failed to load. The words are centred as a stack, so what is
        /// left re-centres itself and the card never jumps.
        /// </summary>
        private void SetDetail(string model, string rest)
        {
            var named = !string.IsNullOrEmpty(model);
            _model.Text = named ? model : string.Empty;
            _model.Visibility = named ? Visibility.Visible : Visibility.Collapsed;

            _detail.Text = string.IsNullOrEmpty(rest) ? string.Empty : rest;
            _detail.Foreground = new SolidColorBrush(TextSecondary);
        }

        /// <summary>
        /// Seconds once past a second, because "1400 ms" is a developer's unit
        /// and the person reading this is looking at a frozen Revit.
        /// </summary>
        private static string Elapsed(long milliseconds)
        {
            if (milliseconds < 1000)
                return milliseconds.ToString(CultureInfo.InvariantCulture) + " ms";

            return (milliseconds / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + " s";
        }

        /// <summary>The accent at track strength - the same hue, much darker.</summary>
        private static Color Shade(Color colour)
        {
            return Color.FromRgb((byte)(colour.R / 4), (byte)(colour.G / 4), (byte)(colour.B / 4));
        }

        // ----- the window ---------------------------------------------------

        /// <summary>
        /// ONE window, hidden and shown rather than created and destroyed. A
        /// batch of fragments is a run of jobs back to back, and building a
        /// window per job strobes.
        /// </summary>
        private bool Show()
        {
            if (_window == null) Build();
            if (_window == null) return false;

            if (!_window.IsVisible)
            {
                PositionOver(_host);
                _cardTransform.Y = -15;
                _card.Opacity = 0;
                _window.Show();
            }

            var slide = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(200))) 
            { 
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } 
            };
            var fade = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(150)));
            _cardTransform.BeginAnimation(TranslateTransform.YProperty, slide);
            _card.BeginAnimation(UIElement.OpacityProperty, fade);

            return true;
        }

        private void Build()
        {
            _titleBrush = new SolidColorBrush(TextPrimary);
            _chipBrush = new SolidColorBrush(ReadingColour);
            _lampBrush = new SolidColorBrush(ReadingColour);
            _sweepBrush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0) };
            _sweepBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, ReadingColour.R, ReadingColour.G, ReadingColour.B), 0.0));
            _sweepBrush.GradientStops.Add(new GradientStop(ReadingColour, 0.2));
            _sweepBrush.GradientStops.Add(new GradientStop(ReadingColour, 0.8));
            _sweepBrush.GradientStops.Add(new GradientStop(Color.FromArgb(0, ReadingColour.R, ReadingColour.G, ReadingColour.B), 1.0));
            _trackBrush = new SolidColorBrush(Shade(ReadingColour));

            _title = Line(15, FontWeights.SemiBold, TextPrimary);
            _title.Foreground = _titleBrush;

            // Between the two in size as well as in position: louder than
            // the job because it answers the more urgent question, quieter
            // than the title because the title is what the eye lands on.
            _model = Line(12.5, FontWeights.SemiBold, TextPrimary);
            _model.Margin = new Thickness(0, 4, 0, 0);
            _model.TextTrimming = TextTrimming.CharacterEllipsis;

            _detail = Line(11, FontWeights.Normal, TextSecondary);
            _detail.Margin = new Thickness(0, 3, 0, 0);
            _detail.TextTrimming = TextTrimming.CharacterEllipsis;
            Typography.SetNumeralAlignment(_detail, FontNumeralAlignment.Tabular);

            _lamp = new Ellipse
            {
                Width = 10,
                Height = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Fill = _lampBrush,
            };
            _lampPulse = new DoubleAnimation(0.4, 1.0, new Duration(TimeSpan.FromMilliseconds(600)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };
            _lampPulse.Freeze();

            var mark = new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(20),
                Background = new SolidColorBrush(Color.FromRgb(0x14, 0x17, 0x1A)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                Child = _lamp,
            };

            var words = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(13, 0, 12, 0),
            };
            words.Children.Add(_title);
            words.Children.Add(_model);
            words.Children.Add(_detail);

            _chipText = Line(10, FontWeights.Bold, ReadingColour);
            _chipText.Foreground = _chipBrush;
            _chip = new Border
            {
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(1),
                BorderBrush = _chipBrush,
                Padding = new Thickness(7, 3, 7, 3),
                VerticalAlignment = VerticalAlignment.Center,
                Child = _chipText,
            };

            var row = new Grid { Margin = new Thickness(16, 0, 16, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(mark);
            Grid.SetColumn(words, 1);
            row.Children.Add(words);
            Grid.SetColumn(_chip, 2);
            row.Children.Add(_chip);

            // An indeterminate sweep, not a percentage. A request carries no
            // progress data - Revit does not report how far through a script
            // it is - and a bar that fills at a made-up rate is a lie the user
            // will time their own work against.
            _sweep = new Border
            {
                Width = SweepWidth,
                Height = 3,
                Background = _sweepBrush,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            var lane = new Canvas { Height = 3 };
            lane.Children.Add(_sweep);
            _track = new Border
            {
                Background = _trackBrush,
                CornerRadius = new CornerRadius(0, 0, 10, 10),
                ClipToBounds = true,
                Child = lane,
            };

            var stack = new Grid();
            stack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            stack.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3) });
            stack.Children.Add(row);
            Grid.SetRow(_track, 1);
            stack.Children.Add(_track);

            // Driven straight onto the property rather than through a
            // Storyboard. A Storyboard is only controllable - stoppable - when
            // it was begun with the containing-object overload, and getting
            // that wrong throws at the moment the banner tries to settle. This
            // form has one way to start it and one to clear it, and neither
            // depends on how it was begun. Frozen so the same instance can be
            // reused for every job.
            _travel = new DoubleAnimation
            {
                From = -SweepWidth,
                To = BannerWidth,
                Duration = new Duration(SweepDuration),
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            _travel.Freeze();

            _cardTransform = new TranslateTransform();
            _card = new Border
            {
                RenderTransform = _cardTransform,
                Background = new SolidColorBrush(CardColour),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x38, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Child = stack,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 18,
                    ShadowDepth = 4,
                    Direction = 270,
                    Opacity = 0.45,
                    Color = Colors.Black,
                },
            };

            _window = new Window
            {
                Width = BannerWidth,
                Height = BannerHeight,
                Content = _card,
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                ShowActivated = false,
                Topmost = true,
                // Without this WPF cannot render an alpha background, and the
                // rounded card arrives inside a solid black rectangle with no
                // shadow. It is required by - not merely compatible with -
                // WindowStyle.None plus a transparent background.
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                FontFamily = new FontFamily("Segoe UI"),
                WindowStartupLocation = WindowStartupLocation.Manual,
                Title = "Heron AI",
            };

            _host = Process.GetCurrentProcess().MainWindowHandle;
            if (_host != IntPtr.Zero)
            {
                // Owned by Revit, so it minimises and restores with Revit
                // instead of floating over whatever the user switched to.
                new WindowInteropHelper(_window).Owner = _host;
            }

            _window.SourceInitialized += delegate { MakeUninteractable(); PositionOver(_host); };
            _window.Closed += delegate { Forget(); };
        }

        private static TextBlock Line(double size, FontWeight weight, Color colour)
        {
            return new TextBlock
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = size,
                FontWeight = weight,
                Foreground = new SolidColorBrush(colour),
                Text = string.Empty,
            };
        }

        /// <summary>
        /// Centred on Revit's own window rather than on the primary screen, so
        /// it lands over the Revit being worked on when two are open on two
        /// monitors - which is exactly the arrangement docs/25 section 6a
        /// recommends for real parallel work.
        ///
        /// GetWindowRect answers in physical pixels and WPF positions in
        /// device-independent ones. On a 150% display, using the raw numbers
        /// puts the banner a third of a screen off - so they are converted
        /// through this window's own composition target, which is available
        /// from SourceInitialized onwards and knows the DPI of the monitor it
        /// actually landed on.
        /// </summary>
        private void PositionOver(IntPtr host)
        {
            double left, top, width;

            RECT box;
            if (host != IntPtr.Zero && GetWindowRect(host, out box) && OnScreen(box))
            {
                double scaleX = 1.0, scaleY = 1.0;
                var source = PresentationSource.FromVisual(_window);
                if (source != null && source.CompositionTarget != null)
                {
                    var device = source.CompositionTarget.TransformFromDevice;
                    scaleX = device.M11;
                    scaleY = device.M22;
                }

                left = box.Left * scaleX;
                top = box.Top * scaleY;
                width = (box.Right - box.Left) * scaleX;
            }
            else
            {
                // Revit has no main window, or is minimised. A banner in
                // roughly the right region beats one placed at -32000.
                var work = SystemParameters.WorkArea;
                left = work.Left;
                top = work.Top;
                width = work.Width;
            }

            _window.Left = left + Math.Max(0, (width - BannerWidth) / 2.0);
            _window.Top = top + TopMargin;
        }

        /// <summary>
        /// Is that a real window rectangle?
        ///
        /// A minimised window answers GetWindowRect with something near
        /// -32000, which is Windows' way of parking it. Centring on that puts
        /// the banner where no monitor is, and a banner nobody can see is
        /// worse than none: it looks like the feature does not work.
        /// </summary>
        private static bool OnScreen(RECT box)
        {
            return box.Right > box.Left
                && box.Bottom > box.Top
                && box.Left > -30000
                && box.Top > -30000;
        }

        /// <summary>
        /// Makes the banner invisible to the mouse and to focus.
        ///
        /// WS_EX_TRANSPARENT passes every click straight through to the Revit
        /// underneath, so a banner sitting over the ribbon can never eat a
        /// button press - Golden Rule 8. WS_EX_NOACTIVATE keeps it from taking
        /// focus, and WS_EX_TOOLWINDOW keeps it out of Alt+Tab.
        ///
        /// Guarded rather than assumed: on any release where the call is not
        /// available the result is a banner that can be clicked, which is a
        /// far smaller problem than an add-in that throws during a job.
        /// </summary>
        private void MakeUninteractable()
        {
            try
            {
                var handle = new WindowInteropHelper(_window).Handle;
                if (handle == IntPtr.Zero) return;

                var current = GetWindowLongPtr(handle, GWL_EXSTYLE).ToInt64();
                SetWindowLongPtr(handle, GWL_EXSTYLE,
                    new IntPtr(current | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW));
            }
            catch (Exception ex)
            {
                _log("Activity banner is clickable - " + ex.Message);
            }
        }

        // ----- lifetime -----------------------------------------------------

        private void StartHideTimer(TimeSpan delay)
        {
            if (_hideTimer == null)
            {
                _hideTimer = new DispatcherTimer(DispatcherPriority.Background, _ui);
                _hideTimer.Tick += OnHideTick;
            }

            _hideTimer.Stop();
            _hideTimer.Interval = delay > TimeSpan.Zero ? delay : TimeSpan.FromMilliseconds(1);
            _hideTimer.Start();
        }

        private void OnHideTick(object sender, EventArgs args)
        {
            StopHideTimer();

            if (Volatile.Read(ref _active) > 0) return;

            if (_window != null && _window.IsVisible)
            {
                var slide = new DoubleAnimation(-15, new Duration(TimeSpan.FromMilliseconds(150))) 
                { 
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } 
                };
                var fade = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(150)));
                
                fade.Completed += (s, e) => 
                {
                    if (Volatile.Read(ref _active) > 0) return;
                    try { if (_window != null) _window.Hide(); }
                    catch (Exception ex) { _log("Activity banner would not hide: " + ex.Message); }
                    StopAnimation();
                    StopElapsed();
                };

                _cardTransform.BeginAnimation(TranslateTransform.YProperty, slide);
                _card.BeginAnimation(UIElement.OpacityProperty, fade);
            }
            else
            {
                StopAnimation();
                StopElapsed();
            }
        }

        private void StopHideTimer()
        {
            if (_hideTimer != null) _hideTimer.Stop();
        }

        /// <summary>
        /// The live line, with the time so far on the end of it.
        ///
        /// Same shape as the finished card deliberately - "17 pipes - 340 ms"
        /// there, "Counting pipes - 340 ms" here - so the number does not
        /// appear to move when the job ends.
        /// </summary>
        private void ShowLive()
        {
            SetDetail(_liveModel, _liveJob + "  -  " + Elapsed(ElapsedMilliseconds()));
        }

        /// <summary>
        /// How long the banner has been up. Stopwatch stamps rather than
        /// DateTime, because this is a duration and the wall clock can move
        /// under it.
        /// </summary>
        private long ElapsedMilliseconds()
        {
            var started = Volatile.Read(ref _startedTicks);
            if (started == 0) return 0;

            var delta = Stopwatch.GetTimestamp() - started;
            if (delta <= 0) return 0;

            return delta * 1000L / Stopwatch.Frequency;
        }

        private void StartElapsed()
        {
            if (_elapsedTimer == null)
            {
                _elapsedTimer = new DispatcherTimer(DispatcherPriority.Render, _ui);
                _elapsedTimer.Tick += OnElapsedTick;
            }

            _elapsedTimer.Interval = ElapsedTick;
            _elapsedTimer.Start();
        }

        /// <summary>
        /// The count is the truth here too. A tick that arrives after the job
        /// ended must not paint a running time over a finished outcome - the
        /// same class of mistake as D-56, in a different place.
        /// </summary>
        private void OnElapsedTick(object sender, EventArgs args)
        {
            if (_detail == null || Volatile.Read(ref _active) <= 0)
            {
                StopElapsed();
                return;
            }

            ShowLive();
        }

        private void StopElapsed()
        {
            if (_elapsedTimer != null) _elapsedTimer.Stop();
        }

        private void StartAnimation()
        {
            if (_sweep != null && _travel != null)
                _sweep.BeginAnimation(Canvas.LeftProperty, _travel);
        }

        /// <summary>
        /// Clears the animation off the property. Passing null is the defined
        /// way to remove one, and it leaves Canvas.Left back at its own value
        /// rather than frozen wherever the sweep happened to be.
        /// </summary>
        private void StopAnimation()
        {
            if (_sweep != null)
                _sweep.BeginAnimation(Canvas.LeftProperty, null);
        }

        private void Destroy()
        {
            StopAnimation();
            StopElapsed();
            var window = _window;
            _window = null;
            if (window != null) window.Close();
            Forget();
        }

        private void Forget()
        {
            // The next window starts blank, so it is free to show an outcome.
            _holding = false;

            _window = null;
            _title = null;
            _model = null;
            _detail = null;
            _chip = null;
            _chipText = null;
            _lamp = null;
            _sweep = null;
            _track = null;
            _travel = null;
            _lampPulsing = false;
            _liveJob = null;
            _liveModel = null;
        }

        // ----- what to draw --------------------------------------------------

        /// <summary>
        /// What a raise asked for. Immutable, so it crosses threads on a
        /// single reference write and cannot be read half-changed.
        /// </summary>
        private sealed class Raised
        {
            public Raised(string job, bool changesModel, string model)
            {
                Job = job;
                ChangesModel = changesModel;
                Model = model;
            }

            public string Job { get; private set; }
            public bool ChangesModel { get; private set; }
            public string Model { get; private set; }
        }

        /// <summary>How the last job ended.</summary>
        private sealed class Ended
        {
            public Ended(bool ok, string outcome, long milliseconds, string model)
            {
                Ok = ok;
                Outcome = outcome;
                Milliseconds = milliseconds;
                Model = model;
            }

            public bool Ok { get; private set; }
            public string Outcome { get; private set; }
            public long Milliseconds { get; private set; }
            public string Model { get; private set; }
        }

        // ----- Win32 --------------------------------------------------------

        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TRANSPARENT = 0x00000020L;
        private const long WS_EX_TOOLWINDOW = 0x00000080L;
        private const long WS_EX_NOACTIVATE = 0x08000000L;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        // The Ptr forms, because Heron is built x64 only
        // (Directory.Build.props) and the 32-bit entry points do not exist
        // there. Using the int forms would truncate an extended style on the
        // way back in.
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr value);
    }
}
