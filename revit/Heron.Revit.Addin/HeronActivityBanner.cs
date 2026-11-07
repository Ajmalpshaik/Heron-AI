// Heron-Agent:  HERON-REVIT-UI-022
// Heron-Step:   5
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
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
    ///
    /// THE LOOK SINCE 2026-11-07 is the designer's status notification - the
    /// "Heron status bar" design in the owner's brand kit (D-149) - drawn as
    /// it is drawn there, information only by the owner's choice: a smoked
    /// glass card with rounded corners and a hairline rim of white and the
    /// state's colour; the colour washed across it with two soft blooms; a
    /// skewed sheen travelling over it, a slowly turning aura on the rim and
    /// a breathing glow while a job runs; the slide down from above and a
    /// small settle. Two lines: "Heron AI  ·  Changing" in the state's colour
    /// with three dots and the time, then the message - the model first, then
    /// the job. Reading and changing are both the brand's orange, as the
    /// design draws thinking and working; the WORD says which, so whether the
    /// model is being touched is still answered. Done is Success, a stop is
    /// Error. Two things differ from the design on purpose: the time counts in
    /// tenths of a second, because it is there to be seen moving while Revit
    /// is frozen (the freeze test samples it); and there are NO BUTTONS -
    /// Revit is frozen while a job runs, so a button could do nothing, and the
    /// card stays click-through (Golden Rule 8).
    internal sealed class HeronActivityBanner
    {
        // BACK TO 460, where this started. It went to 560 while the name
        // and the outcome shared the second line and would not both fit;
        // giving the name a line of its own is what made the width
        // unnecessary again. Widening and heightening for the same reason
        // would have paid twice for one problem.
        private const double BannerWidth = 460;

        // 66: the status design's two lines - the small top line and the
        // message - at its 14 px padding.
        private const double BannerHeight = 66;

        // Room around the card for its glow to fall, inside the window.
        private const double GlowPad = 18;
        private const double TopMargin = 16;
        private const double SweepWidth = 115;

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

        private static readonly TimeSpan SweepDuration = TimeSpan.FromSeconds(3.2);

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

        // THE BRAND'S COLOURS (D-149, docs/brand/README.md), here rather than
        // taken from HeronWindowStyle because this file is compiled ALONE into
        // Heron.Banner.TestHost, and that is what lets the banner be driven
        // and photographed with no Revit. Every value is in the palette table;
        // shades are these at an opacity.
        private static readonly Color Ink = Color.FromRgb(0x0F, 0x0F, 0x10);
        private static readonly Color Mist = Color.FromRgb(0xF7, 0xF5, 0xF2);
        private static readonly Color AccentColour = Color.FromRgb(0xFD, 0x64, 0x1A);
        private static readonly Color DoneColour = Color.FromRgb(0x3E, 0xCF, 0x8E);
        private static readonly Color FailedColour = Color.FromRgb(0xFF, 0x5A, 0x4E);
        private static readonly Color White = Color.FromRgb(0xFF, 0xFF, 0xFF);
        private static readonly Color AccentLight = Color.FromRgb(0xFF, 0x9A, 0x5C);

        /// <summary>
        /// The banner's OWN dispatcher, on a thread of its own - not Revit's.
        /// Null when the banner is switched off, or when the thread could not
        /// be started, and OnUi treats both the same way: no banner.
        /// </summary>
        private readonly Dispatcher _ui;
        private readonly Action<string> _log;
        private readonly bool _enabled;

        private Window _window;

        /// <summary>The message: the model, then the job - or how it ended.</summary>
        private TextBlock _title;

        /// <summary>The time so far, beside the dots on the top line. Heron.Banner.TestHost samples it.</summary>
        private TextBlock _detail;

        /// <summary>
        /// The small top line - "Heron AI  ·  Changing". Named for the chip it
        /// replaced, which Heron.Banner.TestHost reads by name.
        /// </summary>
        private TextBlock _chipText;

        private StackPanel _dots;
        private StackPanel _eyebrow;
        private Border _sweep;
        private Canvas _lane;
        private DoubleAnimation _travel;
        private DispatcherTimer _hideTimer;
        private DispatcherTimer _elapsedTimer;
        private TranslateTransform _cardTransform;
        private ScaleTransform _cardScale;
        private Grid _card;
        private Grid _aura;
        private RotateTransform _auraTurn;
        private LinearGradientBrush _auraBrush;
        private SolidColorBrush _chipBrush;
        private LinearGradientBrush _rimBrush;
        private LinearGradientBrush _washBrush;
        private RadialGradientBrush _bloomLeft;
        private RadialGradientBrush _bloomRight;
        private Ellipse _bloomL;
        private Ellipse _bloomR;
        private DropShadowEffect _glow;

        /// <summary>Are the dots, the rim and the glow already moving? See StartLampPulse.</summary>
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

        /// <summary>
        /// 1 while Revit asks the modeller something and the card must not
        /// cover it - see StepAside. An int for Volatile, set from Revit's
        /// thread and read on the banner's.
        /// </summary>
        private int _aside;

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
        /// Out of the way while Revit asks the modeller something - D-126's
        /// Keep or Discard - and back afterwards.
        ///
        /// THE CARD IS TOPMOST, so it would sit over the dialog it is waiting
        /// on. Hidden rather than moved: there is no place over Revit where a
        /// card centred on its window is sure to miss a dialog centred on it
        /// too. Called from Revit's thread; posted, like Begin and End.
        /// </summary>
        public void StepAside(bool aside)
        {
            if (!_enabled) return;
            Volatile.Write(ref _aside, aside ? 1 : 0);
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
            // STEPPED ASIDE: nothing is drawn until the question is answered.
            // The next render after StepAside(false) shows the card again.
            if (Volatile.Read(ref _aside) != 0)
            {
                if (_window != null && _window.IsVisible) _window.Hide();
                return;
            }

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

        private static void AnimateColor(SolidColorBrush brush, Color to)
        {
            if (brush == null || brush.Color == to) return;
            var anim = new ColorAnimation(to, new Duration(TimeSpan.FromMilliseconds(250)));
            brush.BeginAnimation(SolidColorBrush.ColorProperty, anim);
        }

        private static Color At(Color colour, byte alpha)
        {
            return Color.FromArgb(alpha, colour.R, colour.G, colour.B);
        }

        /// <summary>
        /// The state's colour in each place the design puts it: the top line
        /// and its dots, the rim, the wash, the blooms, the aura and the glow.
        /// </summary>
        private void SetAccent(Color accent)
        {
            AnimateColor(_chipBrush, accent);

            // The rim - 120 degrees, white 55, the colour at 70, white 8, the colour at 45.
            _rimBrush.GradientStops[0].Color = At(White, 0x8C);
            _rimBrush.GradientStops[1].Color = At(accent, 0xB3);
            _rimBrush.GradientStops[2].Color = At(White, 0x14);
            _rimBrush.GradientStops[3].Color = At(accent, 0x73);

            // The wash - 16, 4 and 8 per cent, left to right.
            _washBrush.GradientStops[0].Color = At(accent, 0x29);
            _washBrush.GradientStops[1].Color = At(accent, 0x0A);
            _washBrush.GradientStops[2].Color = At(accent, 0x14);

            // The blooms. For orange, the design's own two pictures - 12 per
            // cent on the left; 10 per cent fading through Mist at 4 on the
            // right. For any other state, the state's colour at 18 and 14.
            var orange = accent == AccentColour;
            _bloomLeft.GradientStops[0].Color = At(accent, orange ? (byte)0x1F : (byte)0x2E);
            _bloomLeft.GradientStops[1].Color = At(accent, 0x00);
            _bloomRight.GradientStops[0].Color = At(accent, orange ? (byte)0x1A : (byte)0x24);
            _bloomRight.GradientStops[1].Color = orange ? At(Mist, 0x0A) : At(accent, 0x12);
            _bloomRight.GradientStops[2].Color = At(orange ? Mist : accent, 0x00);

            // The aura - the colour and its light end, with gaps, turning.
            _auraBrush.GradientStops[0].Color = At(accent, 0xFF);
            _auraBrush.GradientStops[1].Color = orange ? AccentLight : At(accent, 0xB3);
            _auraBrush.GradientStops[2].Color = At(accent, 0x00);
            _auraBrush.GradientStops[3].Color = At(accent, 0xFF);
            _auraBrush.GradientStops[4].Color = At(accent, 0x00);

            _glow.Color = accent;
        }

        /// <summary>
        /// The design's text-in when what the card says changes: up 4 px and
        /// in, over 350 ms.
        /// </summary>
        private static void TextIn(UIElement element)
        {
            if (element == null) return;
            var shift = element.RenderTransform as TranslateTransform;
            if (shift == null)
            {
                shift = new TranslateTransform();
                element.RenderTransform = shift;
            }
            var duration = new Duration(TimeSpan.FromMilliseconds(350));
            shift.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(4, 0, duration) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, duration));
        }

        /// <summary>The design's island bump when the state changes: 420 ms, a breath.</summary>
        private void Bump()
        {
            if (_cardScale == null) return;
            var x = new DoubleAnimationUsingKeyFrames { Duration = new Duration(TimeSpan.FromMilliseconds(420)) };
            x.KeyFrames.Add(new LinearDoubleKeyFrame(1.01, KeyTime.FromPercent(0.4)));
            x.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(1.0)));
            var y = new DoubleAnimationUsingKeyFrames { Duration = new Duration(TimeSpan.FromMilliseconds(420)) };
            y.KeyFrames.Add(new LinearDoubleKeyFrame(0.97, KeyTime.FromPercent(0.4)));
            y.KeyFrames.Add(new LinearDoubleKeyFrame(1.005, KeyTime.FromPercent(0.7)));
            y.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(1.0)));
            _cardScale.BeginAnimation(ScaleTransform.ScaleXProperty, x);
            _cardScale.BeginAnimation(ScaleTransform.ScaleYProperty, y);
        }

        /// <summary>The message: the model first, so it is never the part trimmed away.</summary>
        private static string Message(string model, string words)
        {
            return string.IsNullOrEmpty(model) ? words : model + "  ·  " + words;
        }

        private void Paint(bool changesModel, string job, string model)
        {
            // Both orange, as the design draws thinking and working. The WORD
            // is what says whether the model is being touched - the half of
            // the answer this banner exists for - and it comes from the
            // registry by name, never from the wire (Golden Rule 19).
            var eyebrow = changesModel ? "Heron AI  ·  Changing" : "Heron AI  ·  Reading";
            var changed = _chipText.Text != eyebrow;
            _chipText.Text = eyebrow;
            SetAccent(AccentColour);

            _liveModel = model;
            _liveJob = string.IsNullOrEmpty(job) ? "Working" : job;
            var message = Message(model, _liveJob);
            if (_title.Text != message)
            {
                _title.Text = message;
                _title.ToolTip = message;
                TextIn(_title);
            }
            if (changed)
            {
                TextIn(_eyebrow);
                Bump();
            }

            _dots.Visibility = Visibility.Visible;
            _detail.Visibility = Visibility.Visible;
            ShowLive();
            StartElapsed();

            _lane.Visibility = Visibility.Visible;

            StartAnimation();
            StartLampPulse();
        }

        /// <summary>
        /// Starts the dots, the aura, the glow and the blooms - ONCE.
        ///
        /// Paint runs on every render and a render happens at every Begin, so
        /// a run of fragments reached this once per job. Restarting a
        /// repeating animation returns it to its first frame: the lamp this
        /// replaced was dragged back to its dimmest at every job instead of
        /// breathing, measured across eight jobs back to back.
        ///
        /// The flag is cleared by StopLampPulse, which is the only thing that
        /// genuinely stops them - so the next job after an outcome starts them
        /// fresh, and nothing else does.
        /// </summary>
        private void StartLampPulse()
        {
            if (_lampPulsing || _dots == null) return;

            // The dots: 1.2 s each, a fifth of a second apart - faint, lit and
            // lifted 2 px at 40 per cent, faint again.
            for (var i = 0; i < _dots.Children.Count; i++)
            {
                var dot = _dots.Children[i] as FrameworkElement;
                if (dot == null) continue;
                var begin = TimeSpan.FromMilliseconds(150 * i);
                var fade = new DoubleAnimationUsingKeyFrames
                {
                    Duration = new Duration(TimeSpan.FromMilliseconds(1200)),
                    RepeatBehavior = RepeatBehavior.Forever,
                    BeginTime = begin,
                };
                fade.KeyFrames.Add(new LinearDoubleKeyFrame(0.25, KeyTime.FromPercent(0)));
                fade.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, KeyTime.FromPercent(0.4)));
                fade.KeyFrames.Add(new LinearDoubleKeyFrame(0.25, KeyTime.FromPercent(0.8)));
                fade.KeyFrames.Add(new LinearDoubleKeyFrame(0.25, KeyTime.FromPercent(1.0)));
                dot.BeginAnimation(UIElement.OpacityProperty, fade);

                var lift = new DoubleAnimationUsingKeyFrames
                {
                    Duration = new Duration(TimeSpan.FromMilliseconds(1200)),
                    RepeatBehavior = RepeatBehavior.Forever,
                    BeginTime = begin,
                };
                lift.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0)));
                lift.KeyFrames.Add(new LinearDoubleKeyFrame(-2, KeyTime.FromPercent(0.4)));
                lift.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(0.8)));
                lift.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromPercent(1.0)));
                var shift = dot.RenderTransform as TranslateTransform;
                if (shift == null) { shift = new TranslateTransform(); dot.RenderTransform = shift; }
                shift.BeginAnimation(TranslateTransform.YProperty, lift);
            }

            // The aura on the rim: shown, and turning once every 6 seconds.
            _aura.BeginAnimation(UIElement.OpacityProperty,
                new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(500))));
            _auraTurn.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(0, 360,
                new Duration(TimeSpan.FromSeconds(6))) { RepeatBehavior = RepeatBehavior.Forever });

            // The glow breathing - 2.4 s, the design's heron-glow.
            _glow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0.22, 0.34,
                new Duration(TimeSpan.FromMilliseconds(1200)))
            {
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            });

            // The blooms drifting - 5 s on the left, 6 s on the right, the other way.
            Drift(_bloomL, 5, false);
            Drift(_bloomR, 6, true);

            _lampPulsing = true;
        }

        /// <summary>The design's heron-bloom: fades to 60 per cent and drifts 14 px, and back.</summary>
        private static void Drift(Ellipse bloom, double seconds, bool reverse)
        {
            if (bloom == null) return;
            var half = new Duration(TimeSpan.FromSeconds(seconds / 2));
            var ease = new SineEase { EasingMode = EasingMode.EaseInOut };
            bloom.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(reverse ? 0.6 : 1.0, reverse ? 1.0 : 0.6, half)
            {
                AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = ease,
            });
            var shift = bloom.RenderTransform as TranslateTransform;
            if (shift == null) { shift = new TranslateTransform(); bloom.RenderTransform = shift; }
            shift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(reverse ? 14 : 0, reverse ? 0 : 14, half)
            {
                AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = ease,
            });
        }

        /// <summary>
        /// Stops them and leaves the card still - an outcome is not moving -
        /// with the design's one flash of the outcome's colour, then a soft glow.
        /// </summary>
        private void StopLampPulse()
        {
            if (_dots != null)
            {
                foreach (FrameworkElement dot in _dots.Children)
                {
                    dot.BeginAnimation(UIElement.OpacityProperty, null);
                    dot.Opacity = 1.0;
                    var shift = dot.RenderTransform as TranslateTransform;
                    if (shift != null) shift.BeginAnimation(TranslateTransform.YProperty, null);
                }
            }
            if (_aura != null)
            {
                _aura.BeginAnimation(UIElement.OpacityProperty,
                    new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(500))));
            }
            if (_auraTurn != null) _auraTurn.BeginAnimation(RotateTransform.AngleProperty, null);
            if (_glow != null)
            {
                // heron-flash: the colour bright, then settling - 0.9 s.
                _glow.BeginAnimation(DropShadowEffect.OpacityProperty, new DoubleAnimation(0.65, 0.28,
                    new Duration(TimeSpan.FromMilliseconds(900))) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            }
            foreach (var bloom in new[] { _bloomL, _bloomR })
            {
                if (bloom == null) continue;
                bloom.BeginAnimation(UIElement.OpacityProperty, null);
                bloom.Opacity = 1.0;
                var shift = bloom.RenderTransform as TranslateTransform;
                if (shift != null) shift.BeginAnimation(TranslateTransform.XProperty, null);
            }

            _lampPulsing = false;
        }

        private void ShowOutcome(bool ok, string outcome, long milliseconds, string model)
        {
            StopElapsed();

            _chipText.Text = ok ? "Heron AI  ·  Done" : "Heron AI  ·  Needs attention";
            var message = Message(model, string.IsNullOrEmpty(outcome)
                ? (ok ? "Done" : "Did not finish")
                : outcome);
            _title.Text = message;
            _title.ToolTip = message;

            // The time it took stays beside the word, so the number does not
            // appear to vanish when the job ends.
            _detail.Text = milliseconds >= 0 ? Elapsed(ElapsedMilliseconds()) : string.Empty;
            _detail.Visibility = milliseconds >= 0 ? Visibility.Visible : Visibility.Collapsed;
            _dots.Visibility = Visibility.Collapsed;

            SetAccent(ok ? DoneColour : FailedColour);
            TextIn(_eyebrow);
            TextIn(_title);
            Bump();

            StopLampPulse();
            StopAnimation();
            _lane.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Seconds in tenths once past a second, because the number is there to
        /// be seen MOVING while Revit is frozen; "1400 ms" is a developer's
        /// unit, and the design's 0:01 moves too seldom to answer "has this
        /// died?".
        /// </summary>
        private static string Elapsed(long milliseconds)
        {
            if (milliseconds < 1000)
                return milliseconds.ToString(CultureInfo.InvariantCulture) + " ms";

            return (milliseconds / 1000.0).ToString("0.0", CultureInfo.InvariantCulture) + " s";
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
                _cardTransform.Y = -(BannerHeight + 40);
                _card.Opacity = 0;
                _window.Show();
            }

            // The design's DynamicIsland: down from above the edge in 560 ms,
            // a small overshoot and settle; in fully by 240 ms.
            var slide = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(560)))
            {
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
            };
            var fade = new DoubleAnimation(1, new Duration(TimeSpan.FromMilliseconds(240)));
            _cardTransform.BeginAnimation(TranslateTransform.YProperty, slide);
            _card.BeginAnimation(UIElement.OpacityProperty, fade);

            return true;
        }

        private void Build()
        {
            _chipBrush = new SolidColorBrush(AccentColour);

            // THE TOP LINE: "Heron AI  ·  Changing" in the state's colour,
            // 11 px semibold, then three 3 px dots and the time.
            _chipText = Line(11, FontWeights.SemiBold, AccentColour);
            _chipText.Foreground = _chipBrush;
            _chipText.TextTrimming = TextTrimming.CharacterEllipsis;

            _dots = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(6, 0, 0, 3),
            };
            for (var i = 0; i < 3; i++)
            {
                _dots.Children.Add(new Ellipse
                {
                    Width = 3,
                    Height = 3,
                    Margin = new Thickness(0, 0, 2, 0),
                    Fill = _chipBrush,
                });
            }

            _detail = Line(11, FontWeights.SemiBold, At(White, 0x66));
            _detail.Margin = new Thickness(6, 0, 0, 0);
            Typography.SetNumeralAlignment(_detail, FontNumeralAlignment.Tabular);

            _eyebrow = new StackPanel { Orientation = Orientation.Horizontal, Height = 15 };
            _eyebrow.Children.Add(_chipText);
            _eyebrow.Children.Add(_dots);
            _eyebrow.Children.Add(_detail);

            // THE MESSAGE: 14 px semibold, Mist.
            _title = Line(14, FontWeights.SemiBold, Mist);
            _title.Margin = new Thickness(0, 2, 0, 0);
            _title.TextTrimming = TextTrimming.CharacterEllipsis;

            var words = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(18, 0, 14, 0),
            };
            words.Children.Add(_eyebrow);
            words.Children.Add(_title);

            // THE GLASS: smoke, Ink at 92 to 86 per cent across at 135 degrees.
            var glass = new Border
            {
                CornerRadius = new CornerRadius(20),
                Background = new LinearGradientBrush(At(Ink, 0xEB), At(Ink, 0xDB), 45),
            };

            // THE WASH: the state's colour, left to right.
            _washBrush = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
            _washBrush.GradientStops.Add(new GradientStop(At(AccentColour, 0x29), 0.0));
            _washBrush.GradientStops.Add(new GradientStop(At(AccentColour, 0x0A), 0.5));
            _washBrush.GradientStops.Add(new GradientStop(At(AccentColour, 0x14), 1.0));
            var wash = new Border { Background = _washBrush };

            // THE BLOOMS: the design's two, where it places them.
            _bloomLeft = new RadialGradientBrush();
            _bloomLeft.GradientStops.Add(new GradientStop(At(AccentColour, 0x1F), 0.0));
            _bloomLeft.GradientStops.Add(new GradientStop(At(AccentColour, 0x00), 1.0));
            _bloomRight = new RadialGradientBrush();
            _bloomRight.GradientStops.Add(new GradientStop(At(AccentColour, 0x1A), 0.0));
            _bloomRight.GradientStops.Add(new GradientStop(At(Mist, 0x0A), 0.6));
            _bloomRight.GradientStops.Add(new GradientStop(At(Mist, 0x00), 1.0));
            var blooms = new Canvas();
            _bloomL = new Ellipse { Width = 200, Height = 120, Fill = _bloomLeft };
            Canvas.SetLeft(_bloomL, -61.2);
            Canvas.SetTop(_bloomL, -41.2);
            _bloomR = new Ellipse { Width = 220, Height = 120, Fill = _bloomRight };
            Canvas.SetLeft(_bloomR, BannerWidth - 220 + 38.8);
            Canvas.SetTop(_bloomR, 18.8);
            blooms.Children.Add(_bloomL);
            blooms.Children.Add(_bloomR);

            // THE SHEEN while a job runs: a quarter of the card wide, skewed
            // 20 degrees, white at 12 per cent - travelling across in 3.2 s. An
            // indeterminate sweep, not a percentage: a request carries no
            // progress data, and a bar that fills at a made-up rate is a lie
            // the user will time their own work against.
            var sheen = new LinearGradientBrush { StartPoint = new Point(0, 0.5), EndPoint = new Point(1, 0.5) };
            sheen.GradientStops.Add(new GradientStop(At(White, 0x00), 0.0));
            sheen.GradientStops.Add(new GradientStop(At(White, 0x1F), 0.5));
            sheen.GradientStops.Add(new GradientStop(At(White, 0x00), 1.0));
            _sweep = new Border
            {
                Width = SweepWidth,
                Height = BannerHeight,
                Background = sheen,
                RenderTransform = new SkewTransform(-20, 0, SweepWidth / 2, BannerHeight / 2),
            };
            _lane = new Canvas();
            _lane.Children.Add(_sweep);
            Canvas.SetLeft(_sweep, -1.2 * SweepWidth);

            // THE LINE OF LIGHT along the top edge, white at 70 per cent in the middle.
            var highlight = new Border
            {
                Height = 1,
                Margin = new Thickness(20, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Top,
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0.5),
                    EndPoint = new Point(1, 0.5),
                    GradientStops =
                    {
                        new GradientStop(At(White, 0x00), 0.0),
                        new GradientStop(At(White, 0xB3), 0.5),
                        new GradientStop(At(White, 0x00), 1.0),
                    },
                },
            };

            var layers = new Grid
            {
                Clip = new RectangleGeometry(new Rect(0, 0, BannerWidth, BannerHeight), 20, 20),
            };
            layers.Children.Add(glass);
            layers.Children.Add(wash);
            layers.Children.Add(blooms);
            layers.Children.Add(_lane);
            layers.Children.Add(highlight);
            layers.Children.Add(words);

            // THE RIM: a 1.2 px hairline at 120 degrees.
            _rimBrush = new LinearGradientBrush { StartPoint = new Point(0, 0.21), EndPoint = new Point(1, 0.79) };
            _rimBrush.GradientStops.Add(new GradientStop(At(White, 0x8C), 0.0));
            _rimBrush.GradientStops.Add(new GradientStop(At(AccentColour, 0xB3), 0.35));
            _rimBrush.GradientStops.Add(new GradientStop(At(White, 0x14), 0.6));
            _rimBrush.GradientStops.Add(new GradientStop(At(AccentColour, 0x73), 1.0));
            var rim = new Border
            {
                CornerRadius = new CornerRadius(20),
                BorderThickness = new Thickness(1.2),
                BorderBrush = _rimBrush,
            };

            // THE AURA while a job runs: the colour travelling round the rim,
            // a crisp line over a soft halo. WPF has no conic gradient, so a
            // turning linear one stands in for the design's - the same colours
            // and gaps, the same 6 s turn.
            _auraTurn = new RotateTransform(0, 0.5, 0.5);
            _auraBrush = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(1, 1),
                RelativeTransform = _auraTurn,
            };
            _auraBrush.GradientStops.Add(new GradientStop(AccentColour, 0.0));
            _auraBrush.GradientStops.Add(new GradientStop(AccentLight, 0.12));
            _auraBrush.GradientStops.Add(new GradientStop(At(AccentColour, 0x00), 0.34));
            _auraBrush.GradientStops.Add(new GradientStop(AccentColour, 0.62));
            _auraBrush.GradientStops.Add(new GradientStop(At(AccentColour, 0x00), 0.9));
            var halo = new Border
            {
                Margin = new Thickness(-1),
                CornerRadius = new CornerRadius(21),
                BorderThickness = new Thickness(3),
                BorderBrush = _auraBrush,
                Opacity = 0.3,
                Effect = new BlurEffect { Radius = 8 },
            };
            var line = new Border
            {
                CornerRadius = new CornerRadius(20),
                BorderThickness = new Thickness(1),
                BorderBrush = _auraBrush,
            };
            _aura = new Grid { Opacity = 0, IsHitTestVisible = false };
            _aura.Children.Add(halo);
            _aura.Children.Add(line);

            // THE GLOW: the state's colour beneath the card, breathing while a
            // job runs - the ONLY effect on the card itself.
            _glow = new DropShadowEffect
            {
                Color = AccentColour,
                ShadowDepth = 6,
                Direction = 270,
                BlurRadius = 26,
                Opacity = 0.28,
            };

            _cardTransform = new TranslateTransform();
            _cardScale = new ScaleTransform(1, 1);
            var transforms = new TransformGroup();
            transforms.Children.Add(_cardScale);
            transforms.Children.Add(_cardTransform);

            var body = new Grid { Effect = _glow };
            body.Children.Add(layers);
            body.Children.Add(rim);

            _card = new Grid
            {
                Width = BannerWidth,
                Height = BannerHeight,
                RenderTransform = transforms,
                RenderTransformOrigin = new Point(0.5, 0.5),
            };
            _card.Children.Add(body);
            _card.Children.Add(_aura);

            // Driven straight onto the property rather than through a
            // Storyboard. A Storyboard is only controllable - stoppable - when
            // it was begun with the containing-object overload, and getting
            // that wrong throws at the moment the banner tries to settle. This
            // form has one way to start it and one to clear it, and neither
            // depends on how it was begun. Frozen so the same instance can be
            // reused for every job.
            _travel = new DoubleAnimation
            {
                From = -1.2 * SweepWidth,
                To = 3.2 * SweepWidth,
                Duration = new Duration(SweepDuration),
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            _travel.Freeze();

            _window = new Window
            {
                Width = BannerWidth + 2 * GlowPad,
                Height = BannerHeight + 2 * GlowPad,
                // The glow's room is a frame around the card, not a margin on
                // it, so whatever photographs the window's content - the test
                // host does - gets the whole card and its glow.
                Content = new Border { Padding = new Thickness(GlowPad), Child = _card },
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                ShowActivated = false,
                Topmost = true,
                // Without this WPF cannot render an alpha background, and the
                // rounded card arrives inside a solid black rectangle with no
                // glow. It is required by - not merely compatible with -
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

            _window.Left = left + Math.Max(0, (width - _window.Width) / 2.0);
            _window.Top = top + TopMargin - GlowPad;
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
                var slide = new DoubleAnimation(-(BannerHeight + 40), new Duration(TimeSpan.FromMilliseconds(380)))
                { 
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } 
                };
                var fade = new DoubleAnimation(0, new Duration(TimeSpan.FromMilliseconds(300)))
                {
                    BeginTime = TimeSpan.FromMilliseconds(80),
                };

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
            if (_detail != null) _detail.Text = Elapsed(ElapsedMilliseconds());
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
            _detail = null;
            _chipText = null;
            _dots = null;
            _eyebrow = null;
            _sweep = null;
            _lane = null;
            _travel = null;
            _card = null;
            _cardScale = null;
            _aura = null;
            _auraTurn = null;
            _glow = null;
            _bloomL = null;
            _bloomR = null;
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
