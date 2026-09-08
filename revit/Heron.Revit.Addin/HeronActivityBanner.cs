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
    /// THE ONE CONSTRAINT THAT DECIDES THE WHOLE DESIGN. Nothing can be drawn
    /// while Revit's thread is busy. So the banner MUST be raised BEFORE the
    /// work starts, from the listener thread, marshalled onto Revit's own
    /// dispatcher - never on a timer that "shows it if the job is slow",
    /// because that timer can only fire on the very thread the job has
    /// already taken. Everything else here follows from that.
    ///
    /// IT SAYS WHETHER HERON IS READING OR CHANGING, and that is the half the
    /// earlier project never had. "Something is happening" is worth little to
    /// somebody whose real question is whether his model is being touched.
    /// The read/change split comes from HeronOperationRegistry BY NAME - the
    /// caller decides which operation to ask for and never how dangerous it
    /// is (Golden Rule 19), so no text arriving on the pipe can make a write
    /// wear the reading colour.
    ///
    /// IT ALSO SAYS HOW IT ENDED. A refusal - Revit busy, writing switched
    /// off, nothing selected - used to be invisible from Revit: the chat got
    /// a sentence and the screen showed nothing at all. The outcome line
    /// holds for a moment after the work so the answer is where the person is
    /// looking.
    ///
    /// THE COUNT IS KEPT OFF THE DISPATCHER, and that is not a detail. Begin
    /// is always called from a listener thread, so it is POSTED and waits its
    /// turn; End is called from Revit's own thread inside Execute, so it runs
    /// INLINE, immediately. While Revit is inside Execute it cannot pump, so a
    /// Begin posted during that window sits behind the very work it announces
    /// - and the End of that same job overtakes it. Counting inside the posted
    /// action therefore let a job be ended before it was begun: the stranded
    /// Begin then raised a banner nothing was left to lower, and it stayed up
    /// over an idle Revit until Revit was restarted - D-56, seen 2026-09-08.
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
        private const double BannerWidth = 460;
        private const double BannerHeight = 78;
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

        private static readonly Color CardColour = Color.FromRgb(0x23, 0x26, 0x29);
        private static readonly Color TextPrimary = Color.FromRgb(0xF2, 0xF5, 0xF7);
        private static readonly Color TextSecondary = Color.FromRgb(0x9A, 0xA5, 0xAD);
        private static readonly Color ReadingColour = Color.FromRgb(0x2F, 0x9B, 0xE3);
        private static readonly Color ChangingColour = Color.FromRgb(0xF0, 0xA3, 0x2C);
        private static readonly Color DoneColour = Color.FromRgb(0x4C, 0xC3, 0x8A);
        private static readonly Color FailedColour = Color.FromRgb(0xE5, 0x53, 0x4B);

        /// <summary>
        /// Revit's own UI dispatcher, captured on Revit's thread during
        /// OnStartup. Application.Current is null inside Revit, so there is no
        /// other way to reach it from the listener threads that call Begin and
        /// End.
        /// </summary>
        private readonly Dispatcher _ui;
        private readonly Action<string> _log;
        private readonly bool _enabled;

        private Window _window;
        private TextBlock _title;
        private TextBlock _detail;
        private TextBlock _chipText;
        private Border _chip;
        private Ellipse _lamp;
        private Border _sweep;
        private Border _track;
        private DoubleAnimation _travel;
        private DispatcherTimer _hideTimer;

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
        /// MUST be constructed on Revit's own thread - it captures that
        /// thread's dispatcher, and a banner built anywhere else would post
        /// its work to a thread that never draws anything.
        /// </summary>
        public HeronActivityBanner(bool enabled, Action<string> log)
        {
            _ui = Dispatcher.CurrentDispatcher;
            _log = log ?? delegate { };
            _enabled = enabled;
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
        public void Begin(string job, bool changesModel)
        {
            if (!_enabled) return;

            // COUNTED HERE, on the caller's thread, before the hop - see the
            // class comment. Counting inside the posted action is what let an
            // End overtake its own Begin.
            Interlocked.Increment(ref _active);
            Volatile.Write(ref _raised, new Raised(job, changesModel));

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
        /// How long it took, or a negative number when it never ran. Shown
        /// because the freeze is the complaint: "12 s" turns a hang into a
        /// duration.
        /// </param>
        public void End(bool ok, string outcome, long milliseconds)
        {
            if (!_enabled) return;

            // The outcome is published BEFORE the count drops, so a render
            // caused by that drop cannot find the count settled and the answer
            // still missing.
            Volatile.Write(ref _ended, new Ended(ok, outcome, milliseconds));
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
                Paint(job.ChangesModel, job.Job);
                return;
            }

            // Nothing is in flight. Say how it ended and start the hold - but
            // only once, so a render arriving after the answer is already up
            // leaves the hold running instead of restarting it.
            if (_holding || _window == null) return;

            var end = Volatile.Read(ref _ended);
            _holding = true;
            ShowOutcome(end == null || end.Ok,
                        end == null ? null : end.Outcome,
                        end == null ? -1 : end.Milliseconds);
            StartHideTimer(OutcomeHold);
        }

        /// <summary>Revit is closing. Take the window down with it.</summary>
        public void Shutdown()
        {
            OnUi(delegate
            {
                StopHideTimer();
                Interlocked.Exchange(ref _active, 0);
                Destroy();
            });
        }

        // ----- the UI thread hop -------------------------------------------

        /// <summary>
        /// Runs on Revit's thread, and never throws back at the caller.
        ///
        /// BeginInvoke rather than Invoke, always: Invoke from a listener
        /// thread would block that thread until Revit is idle, which is
        /// exactly when Revit is running the job the caller is waiting for.
        /// A banner is not worth a deadlock.
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

        private void Paint(bool changesModel, string job)
        {
            var accent = changesModel ? ChangingColour : ReadingColour;

            _title.Text = changesModel
                ? "Heron AI is changing your model"
                : "Heron AI is reading your model";
            _title.Foreground = new SolidColorBrush(TextPrimary);

            _detail.Text = string.IsNullOrEmpty(job) ? "Working" : job;
            _detail.Foreground = new SolidColorBrush(TextSecondary);

            _chipText.Text = changesModel ? "CHANGING" : "READING";
            _chipText.Foreground = new SolidColorBrush(accent);
            _chip.BorderBrush = new SolidColorBrush(accent);

            _lamp.Fill = new SolidColorBrush(accent);
            _sweep.Background = new SolidColorBrush(accent);
            _track.Background = new SolidColorBrush(Shade(accent));
            _track.Visibility = Visibility.Visible;

            StartAnimation();
        }

        private void ShowOutcome(bool ok, string outcome, long milliseconds)
        {
            var accent = ok ? DoneColour : FailedColour;

            _title.Text = ok ? "Heron AI has finished" : "Heron AI stopped";
            _detail.Text = string.IsNullOrEmpty(outcome)
                ? (ok ? "Done" : "Did not finish")
                : outcome;

            if (milliseconds >= 0)
            {
                _detail.Text += "  -  " + Elapsed(milliseconds);
            }

            _chipText.Text = ok ? "DONE" : "STOPPED";
            _chipText.Foreground = new SolidColorBrush(accent);
            _chip.BorderBrush = new SolidColorBrush(accent);
            _lamp.Fill = new SolidColorBrush(accent);

            // The sweep is a claim that something is still happening. Nothing
            // is, so it goes rather than freezing mid-travel.
            StopAnimation();
            _track.Visibility = Visibility.Collapsed;
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
                // Before Show, so it never appears in the old place first.
                // On the very first raise there is no HWND yet and this reads
                // no DPI scale - SourceInitialized fires inside Show and
                // corrects it before anything is drawn.
                PositionOver(_host);
                _window.Show();
            }
            return true;
        }

        private void Build()
        {
            _title = Line(15, FontWeights.SemiBold, TextPrimary);
            _detail = Line(11, FontWeights.Normal, TextSecondary);
            _detail.Margin = new Thickness(0, 3, 0, 0);
            _detail.TextTrimming = TextTrimming.CharacterEllipsis;

            _lamp = new Ellipse
            {
                Width = 10,
                Height = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Fill = new SolidColorBrush(ReadingColour),
            };

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
            words.Children.Add(_detail);

            _chipText = Line(10, FontWeights.Bold, ReadingColour);
            _chip = new Border
            {
                CornerRadius = new CornerRadius(3),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(ReadingColour),
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
                Background = new SolidColorBrush(ReadingColour),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            var lane = new Canvas { Height = 3 };
            lane.Children.Add(_sweep);
            _track = new Border
            {
                Background = new SolidColorBrush(Shade(ReadingColour)),
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
            };
            _travel.Freeze();

            var card = new Border
            {
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
                Content = card,
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

            // A job that started while the outcome was on screen owns the
            // banner now - hiding it here would take away a live one.
            if (Volatile.Read(ref _active) > 0) return;

            try { if (_window != null) _window.Hide(); }
            catch (Exception ex) { _log("Activity banner would not hide: " + ex.Message); }

            StopAnimation();
        }

        private void StopHideTimer()
        {
            if (_hideTimer != null) _hideTimer.Stop();
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
            _chip = null;
            _chipText = null;
            _lamp = null;
            _sweep = null;
            _track = null;
            _travel = null;
        }

        // ----- what to draw --------------------------------------------------

        /// <summary>
        /// What a raise asked for. Immutable, so it crosses threads on a
        /// single reference write and cannot be read half-changed.
        /// </summary>
        private sealed class Raised
        {
            public Raised(string job, bool changesModel)
            {
                Job = job;
                ChangesModel = changesModel;
            }

            public string Job { get; private set; }
            public bool ChangesModel { get; private set; }
        }

        /// <summary>How the last job ended.</summary>
        private sealed class Ended
        {
            public Ended(bool ok, string outcome, long milliseconds)
            {
                Ok = ok;
                Outcome = outcome;
                Milliseconds = milliseconds;
            }

            public bool Ok { get; private set; }
            public string Outcome { get; private set; }
            public long Milliseconds { get; private set; }
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
