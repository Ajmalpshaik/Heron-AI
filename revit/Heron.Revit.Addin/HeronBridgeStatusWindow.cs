// Heron-Agent:  HERON-REVIT-UI-022
// Heron-Step:   5
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Which bridge state a session is in. Three, not two - "Heron never
    /// started" is a different problem from "Heron started and is not
    /// connected", and what the person should do about it differs in each.
    /// </summary>
    internal enum HeronBridgeState
    {
        /// <summary>OnStartup failed. There is nothing to connect, and a log says why.</summary>
        DidNotStart,

        /// <summary>Loaded and idle. This Revit is invisible to every chat, by design.</summary>
        NotConnected,

        /// <summary>The pipe is open and this session is announced.</summary>
        Connected,
    }

    /// <summary>
    /// Everything the status window draws: plain strings and one enum.
    ///
    /// DELIBERATELY NOT A BridgeServer. The window is handed a snapshot the
    /// caller took rather than a live object it could read at any moment,
    /// which buys two things:
    ///
    ///   * the window compiles and runs with no Revit and no bridge anywhere
    ///     near it, so how it LOOKS can be proven on a screen without closing
    ///     Revit or opening a model, and
    ///   * it cannot read one field half-way through a reconnect and draw a
    ///     state that never existed.
    ///
    /// Every value here is already-formatted text, because culture and
    /// formatting are decided once, in the caller, and not argued about here.
    /// </summary>
    internal sealed class HeronBridgeStatus
    {
        public HeronBridgeState State { get; set; }

        public string PipeName { get; set; }
        public string ProcessId { get; set; }
        public string RevitVersion { get; set; }
        public string AddinVersion { get; set; }
        public string ProtocolVersion { get; set; }
        public string DiscoveryFilePath { get; set; }
        public string LogFolder { get; set; }

        /// <summary>Whether Heron may change the model right now - the ribbon padlock.</summary>
        public bool WriteEnabled { get; set; }
    }

    /// <summary>
    /// The Bridge Status window - HERON-REVIT-UI-022.
    ///
    /// WHAT IT REPLACED, AND WHY THAT WAS NOT GOOD ENOUGH. Until 2026-09-20
    /// this was a TaskDialog holding six lines of aligned text. It was honest
    /// and it was unreadable: a pipe name, a process id and two file paths in
    /// one grey block, with nothing saying which of them anybody should care
    /// about. The question actually being asked - "can a chat reach this
    /// Revit right now?" - was answered in the first word and then buried
    /// under five lines that only matter on the day something is wrong.
    ///
    /// So this window is built around ONE answer, stated large, with the
    /// evidence underneath it for that day. Asked for by Ajmal, 2026-09-20.
    ///
    /// MODAL, AND THEREFORE SAFE. It is shown from inside an IExternalCommand,
    /// so it runs in the API context and blocks Revit while it is up. That is
    /// the right shape for a question the user just asked and is waiting on.
    /// It is NOT the shape for anything that lives alongside Revit while work
    /// continues - that would be modeless, and a modeless window may never
    /// touch the Revit API directly (see the conventions skill).
    ///
    /// IT TOUCHES NO REVIT API AT ALL, on purpose. Connecting and
    /// disconnecting arrive as a delegate from the command; re-reading the
    /// state arrives as another. The window therefore knows nothing about
    /// bridges, documents or elements, so nothing in it can crash Revit or
    /// change a model. It also means the whole file compiles into a bare WPF
    /// harness and can be LOOKED AT - the only way its appearance gets proven
    /// without a Revit restart.
    ///
    /// NO SUCCESS POPUP, anywhere. Connecting from this window changes the
    /// window - the lamp, the headline, the list - and says nothing else.
    /// Copying to the clipboard writes one line into the footer that clears
    /// itself. A dialog that has been dismissed tells nobody anything; a
    /// window still showing the right state tells everybody.
    ///
    /// NOTHING HERE MAY THROW OUT. A cosmetic fault must never cost the user
    /// their command, so every entry point catches. A status window that
    /// fails means no status window; it must not mean a failed click.
    /// </summary>
    internal sealed class HeronBridgeStatusWindow
    {
        // ------------------------------------------------------------------
        // The palette.
        //
        // These are HeronActivityBanner's colours, to the byte, and that is
        // the whole argument for them: the banner is the only other thing
        // Heron draws inside Revit, and two Heron surfaces using two
        // different darks and two different blues read as two products.
        //
        // What the accents MEAN is the house rule rather than the banner's:
        // blue is active and primary, amber is a warning and nothing else,
        // red is danger and nothing else, grey is the quiet default.
        // ------------------------------------------------------------------
        private static readonly Color CardColour = Color.FromRgb(0x23, 0x26, 0x29);
        private static readonly Color HeaderColour = Color.FromRgb(0x1A, 0x1D, 0x20);
        private static readonly Color InsetColour = Color.FromRgb(0x1B, 0x1F, 0x22);
        private static readonly Color TextPrimary = Color.FromRgb(0xF2, 0xF5, 0xF7);
        private static readonly Color TextSecondary = Color.FromRgb(0x9A, 0xA5, 0xAD);
        private static readonly Color TextMuted = Color.FromRgb(0x6F, 0x7A, 0x82);
        private static readonly Color AccentColour = Color.FromRgb(0x2F, 0x9B, 0xE3);
        private static readonly Color WarnColour = Color.FromRgb(0xF0, 0xA3, 0x2C);
        private static readonly Color DangerColour = Color.FromRgb(0xE5, 0x53, 0x4B);
        private static readonly Color QuietColour = Color.FromRgb(0x7E, 0x8A, 0x93);

        private const double CardWidth = 470;
        private const double Gutter = 22;

        private const string BodyFont = "Segoe UI";

        // Consolas for pipe names, process ids and paths. A proportional font
        // makes "heron.2024.11908" and "l1llO0" genuinely ambiguous, and these
        // are exactly the strings somebody reads out to somebody else when
        // something has gone wrong.
        private const string MonoFont = "Consolas";

        private readonly Func<HeronBridgeStatus> _read;
        private readonly Action _toggle;
        private readonly Action<string> _log;

        private Window _window;

        // Everything that changes when the state does. Held rather than
        // rebuilt, so connecting from inside the window redraws the answer
        // without the whole card flickering.
        private Border _accentStrip;
        private Ellipse _lamp;
        private DropShadowEffect _lampGlow;
        private TextBlock _headline;
        private TextBlock _explain;
        private Border _writeCard;
        private Border _writeChip;
        private TextBlock _writeChipText;
        private TextBlock _writeExplain;
        private StackPanel _details;
        private TextBlock _note;
        private Button _primary;
        private Button _secondary;
        private Action _primaryAction;
        private Action _secondaryAction;
        private DispatcherTimer _noteTimer;

        private HeronBridgeStatus _status;

        private HeronBridgeStatusWindow(
            Func<HeronBridgeStatus> read, Action toggle, Action<string> log)
        {
            _read = read;
            _toggle = toggle;
            _log = log ?? delegate { };
        }

        /// <summary>
        /// Builds the window and blocks until it is closed.
        ///
        /// <paramref name="read"/> is called every time the answer could have
        /// changed - once to open, and again after every connect or
        /// disconnect. It is never cached, for the same reason a Document is
        /// never cached: a snapshot taken once and trusted for ever is how a
        /// window ends up confidently describing a session that has moved on.
        ///
        /// <paramref name="toggle"/> connects or disconnects and is allowed to
        /// throw. The message lands in the footer rather than in a second
        /// dialog stacked on top of this one.
        /// </summary>
        /// <summary>
        /// FALSE means nothing was shown, and the caller must answer the
        /// question some other way. A person who pressed Bridge Status and
        /// got no window at all has been told nothing - and would reasonably
        /// conclude the button is broken rather than that the answer is
        /// somewhere else. See StatusCommand for the plain fallback.
        /// </summary>
        internal static bool Show(
            Func<HeronBridgeStatus> read, Action toggle, Action<string> log)
        {
            if (read == null) return false;
            return new HeronBridgeStatusWindow(read, toggle, log).ShowInternal();
        }

        private bool ShowInternal()
        {
            try
            {
                _status = _read();
                if (_status == null) return false;

                Build();
                Render();
                _window.ShowDialog();
                return true;
            }
            catch (Exception ex)
            {
                // The command has to survive a window that will not draw.
                // There is nothing useful to show from HERE - the thing that
                // shows things is what failed - so this goes to the log and
                // the caller is told to fall back.
                _log("Bridge Status window failed to open: " + ex);
                return false;
            }
        }

        // ==================================================================
        // Building
        // ==================================================================

        private void Build()
        {
            var body = new StackPanel();
            body.Children.Add(BuildTitleBar());
            body.Children.Add(BuildState());
            body.Children.Add(BuildWriteCard());
            body.Children.Add(BuildDetails());
            body.Children.Add(BuildFooter());

            // The state strip. Three pixels of colour along the top edge -
            // the one part of this window readable from far enough away to be
            // taken in while walking back to the desk.
            _accentStrip = new Border
            {
                Height = 3,
                CornerRadius = new CornerRadius(14, 14, 0, 0),
                Background = new SolidColorBrush(AccentColour),
            };

            var stack = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(_accentStrip, Dock.Top);
            stack.Children.Add(_accentStrip);
            stack.Children.Add(body);

            var card = new Border
            {
                Background = new SolidColorBrush(CardColour),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x38, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Child = stack,

                // The ONLY shadow in the window. One on the card reads as
                // depth; one on every row reads as mud, and on a software
                // -rendered transparent window it costs frames for nothing.
                Effect = new DropShadowEffect
                {
                    BlurRadius = 24,
                    ShadowDepth = 6,
                    Direction = 270,
                    Opacity = 0.5,
                    Color = Colors.Black,
                },
            };

            _window = new Window
            {
                Width = CardWidth + 24,          // room for the shadow to fall
                SizeToContent = SizeToContent.Height,
                Content = new Border { Padding = new Thickness(12), Child = card },
                WindowStyle = WindowStyle.None,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,

                // Required by, not merely compatible with, WindowStyle.None
                // plus a transparent background: without it the rounded card
                // arrives inside a solid black rectangle and the shadow is a
                // grey smear. The same finding as the banner's.
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                FontFamily = new FontFamily(BodyFont),
                Title = "Heron",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            // Owned by Revit, so it centres on the Revit being used rather
            // than on the primary screen, and cannot be left behind when the
            // user alt-tabs away. Process.MainWindowHandle rather than
            // anything from the Revit API, because this file deliberately
            // references none of it.
            try
            {
                var host = Process.GetCurrentProcess().MainWindowHandle;
                if (host != IntPtr.Zero)
                    new WindowInteropHelper(_window).Owner = host;
                else
                    _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            catch (Exception ex)
            {
                // An unowned window still works. It just centres on the wrong
                // screen, which is worth a log line and nothing more.
                _log("Bridge Status could not take Revit as its owner: " + ex.Message);
                _window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // Esc closes. With WindowStyle.None there is no system menu and no
            // title bar of Revit's own, so the key every dialog in Windows
            // answers to has to be wired by hand or this window has exactly
            // one way out.
            _window.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) Close();
            };

            _window.Closed += delegate { StopNoteTimer(); };
        }

        private UIElement BuildTitleBar()
        {
            var mark = new Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(AccentColour),
                VerticalAlignment = VerticalAlignment.Center,
                Child = new TextBlock
                {
                    Text = "H",
                    FontFamily = new FontFamily(BodyFont),
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x0E, 0x17, 0x1D)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };

            var title = new TextBlock
            {
                Margin = new Thickness(11, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            title.Inlines.Add(new Run("Heron")
            {
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(TextPrimary),
            });
            title.Inlines.Add(new Run("     Bridge Status")
            {
                FontSize = 13,
                Foreground = new SolidColorBrush(TextMuted),
            });

            var left = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
            };
            left.Children.Add(mark);
            left.Children.Add(title);

            var close = new Button
            {
                Content = "✕",
                Width = 30,
                Height = 26,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(TextSecondary),
                FontSize = 12,
                Cursor = Cursors.Hand,
                Template = FlatButtonTemplate(Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF), 7),
            };
            close.Click += delegate { Close(); };

            var bar = new Grid { Height = 54 };
            bar.Children.Add(left);
            bar.Children.Add(close);

            var host = new Border
            {
                Background = new SolidColorBrush(HeaderColour),
                Padding = new Thickness(Gutter, 0, 12, 0),
                Child = bar,
            };

            // A window with no title bar cannot be moved, and one that lands
            // over the very thing the user wanted to look at is worse than a
            // plain dialog. Dragging the header moves it.
            host.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e)
            {
                try { if (e.ButtonState == MouseButtonState.Pressed) _window.DragMove(); }
                catch (InvalidOperationException) { /* released mid-drag; harmless */ }
            };

            return host;
        }

        private UIElement BuildState()
        {
            _lampGlow = new DropShadowEffect
            {
                BlurRadius = 16,
                ShadowDepth = 0,
                Opacity = 0.9,
                Color = AccentColour,
            };

            _lamp = new Ellipse
            {
                Width = 11,
                Height = 11,
                Fill = new SolidColorBrush(AccentColour),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 11, 0),
                Effect = _lampGlow,
            };

            _headline = new TextBlock
            {
                FontFamily = new FontFamily(BodyFont),
                FontSize = 21,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(TextPrimary),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var line = new StackPanel { Orientation = Orientation.Horizontal };
            line.Children.Add(_lamp);
            line.Children.Add(_headline);

            _explain = new TextBlock
            {
                FontFamily = new FontFamily(BodyFont),
                FontSize = 13,
                Foreground = new SolidColorBrush(TextSecondary),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20,
                Margin = new Thickness(0, 8, 0, 0),
            };

            var stack = new StackPanel { Margin = new Thickness(Gutter, 20, Gutter, 0) };
            stack.Children.Add(line);
            stack.Children.Add(_explain);
            return stack;
        }

        private UIElement BuildWriteCard()
        {
            var label = new TextBlock
            {
                Text = "Changes",
                FontFamily = new FontFamily(BodyFont),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(TextPrimary),
                VerticalAlignment = VerticalAlignment.Center,
            };

            _writeChipText = new TextBlock
            {
                FontFamily = new FontFamily(BodyFont),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            _writeChip = new Border
            {
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(9, 2, 9, 3),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Child = _writeChipText,
            };

            var head = new Grid();
            head.Children.Add(label);
            head.Children.Add(_writeChip);

            _writeExplain = new TextBlock
            {
                FontFamily = new FontFamily(BodyFont),
                FontSize = 12,
                Foreground = new SolidColorBrush(TextMuted),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 17,
                Margin = new Thickness(0, 5, 0, 0),
            };

            var text = new StackPanel();
            text.Children.Add(head);
            text.Children.Add(_writeExplain);

            _writeCard = new Border
            {
                Margin = new Thickness(Gutter, 17, Gutter, 0),
                Padding = new Thickness(14, 11, 14, 12),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(InsetColour),
                BorderThickness = new Thickness(1),
                Child = text,
            };
            return _writeCard;
        }

        private UIElement BuildDetails()
        {
            _details = new StackPanel { Margin = new Thickness(Gutter, 4, Gutter, 0) };
            return _details;
        }

        private UIElement BuildFooter()
        {
            _note = new TextBlock
            {
                FontFamily = new FontFamily(BodyFont),
                FontSize = 12,
                Foreground = new SolidColorBrush(TextMuted),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(8, 0, 0, 0),
            };

            var copy = new Button
            {
                Content = "Copy details",
                Foreground = new SolidColorBrush(TextSecondary),
                FontFamily = new FontFamily(BodyFont),
                FontSize = 12.5,
                Padding = new Thickness(10, 6, 10, 7),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Template = FlatButtonTemplate(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF), 8),
            };
            copy.Click += delegate { CopyDetails(); };

            _secondary = GhostButton();
            _secondary.Click += delegate { Invoke(_secondaryAction); };

            _primary = PrimaryButton();
            _primary.Click += delegate { Invoke(_primaryAction); };
            _primary.IsDefault = true;

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            buttons.Children.Add(_secondary);
            buttons.Children.Add(_primary);

            // A DockPanel rather than a StackPanel: a StackPanel hands its
            // last child all the width it asks for, so the note would size to
            // its text and trim nothing. Filling the remainder is what makes
            // the trim happen.
            var left = new DockPanel
            {
                LastChildFill = true,
                VerticalAlignment = VerticalAlignment.Center,
            };
            DockPanel.SetDock(copy, Dock.Left);
            left.Children.Add(copy);
            left.Children.Add(_note);

            // TWO COLUMNS, not two children overlaid on one. A Grid cell
            // holds both at full width, so a long failure message in the note
            // would run straight under the buttons and be read as far as the
            // word it collided with. The star column clips it instead, which
            // is what TextTrimming has been asking for all along.
            var row = new Grid();
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(left, 0);
            Grid.SetColumn(buttons, 1);
            row.Children.Add(left);
            row.Children.Add(buttons);

            return new Border
            {
                Margin = new Thickness(0, 20, 0, 0),
                Padding = new Thickness(Gutter - 10, 13, Gutter, 13),
                Background = new SolidColorBrush(HeaderColour),
                CornerRadius = new CornerRadius(0, 0, 13, 13),
                Child = row,
            };
        }

        // ==================================================================
        // Drawing the state
        // ==================================================================

        /// <summary>
        /// Puts every changing part of the window in step with
        /// <see cref="_status"/>. Called once on open, and again after every
        /// connect or disconnect.
        /// </summary>
        private void Render()
        {
            var accent = StateColour(_status.State);

            _accentStrip.Background = new SolidColorBrush(accent);
            _lamp.Fill = new SolidColorBrush(accent);
            _lampGlow.Color = accent;

            // The glow is what separates "connected" from "not connected" at
            // a glance. A dark lamp for a dark state is the same argument the
            // ribbon button already makes with its picture.
            _lampGlow.Opacity = _status.State == HeronBridgeState.NotConnected ? 0.0 : 0.9;

            switch (_status.State)
            {
                case HeronBridgeState.Connected:
                    _headline.Text = "Connected";
                    _explain.Text =
                        "A chat can reach this Revit session. Nothing here gets changed unless "
                        + "Changes is on and you say yes to the preview.";
                    break;

                case HeronBridgeState.NotConnected:
                    _headline.Text = "Not connected";
                    _explain.Text =
                        "This Revit is private. No chat can see it or reach it until you connect - "
                        + "and that is the point. A session you never offered up stays yours.";
                    break;

                default:
                    _headline.Text = "Heron did not start";
                    _explain.Text =
                        "The add-in loaded but could not start, so there is nothing to connect. "
                        + "The log that says why is in the folder below.";
                    break;
            }

            // HIDDEN WHEN NOTHING IS RUNNING. The setting is still whatever
            // it is, so "OFF" would not be a lie - but the sentence under it
            // would be: "Heron can read this model" is not true of a Heron
            // that did not start. A row that is accurate and misleading is
            // worse than no row.
            _writeCard.Visibility = _status.State == HeronBridgeState.DidNotStart
                ? Visibility.Collapsed : Visibility.Visible;

            if (_writeCard.Visibility == Visibility.Visible) RenderWrite();
            RenderDetails();
            RenderButtons();
        }

        private void RenderWrite()
        {
            // AMBER FOR ON, GREY FOR OFF, and never green for either. Green
            // would read as "good", and whether Heron may edit this model is
            // not a thing with a good answer - it is a thing with a state the
            // person is entitled to see without it being interpreted for them.
            var on = _status.WriteEnabled;
            var colour = on ? WarnColour : QuietColour;

            _writeChipText.Text = on ? "ON" : "OFF";
            _writeChipText.Foreground = new SolidColorBrush(colour);
            _writeChip.BorderBrush = new SolidColorBrush(
                Color.FromArgb(0x8C, colour.R, colour.G, colour.B));
            _writeChip.Background = new SolidColorBrush(
                Color.FromArgb(on ? (byte)0x22 : (byte)0x14, colour.R, colour.G, colour.B));

            _writeExplain.Text = on
                ? "Heron may move, edit and create elements when you ask it to. It previews first, "
                  + "and every change is one Ctrl+Z."
                : "Heron can read this model and nothing else. The padlock on the ribbon changes it.";

            _writeCard.BorderBrush = new SolidColorBrush(
                on ? Color.FromArgb(0x59, colour.R, colour.G, colour.B)
                   : Color.FromArgb(0x1E, 0xFF, 0xFF, 0xFF));
        }

        private void RenderDetails()
        {
            _details.Children.Clear();

            // WHAT IS SHOWN DEPENDS ON WHAT IS TRUE. A pipe name printed
            // beside "Not connected" names something that is not open, and
            // the reader has no way of telling. So rows that exist only while
            // connected appear only while connected.
            var rows = new List<KeyValuePair<string, string>>();

            if (_status.State == HeronBridgeState.Connected)
                rows.Add(new KeyValuePair<string, string>("Pipe", _status.PipeName));

            rows.Add(new KeyValuePair<string, string>("Process", _status.ProcessId));
            rows.Add(new KeyValuePair<string, string>("Revit", _status.RevitVersion));
            rows.Add(new KeyValuePair<string, string>("Add-in", _status.AddinVersion));
            rows.Add(new KeyValuePair<string, string>("Protocol", _status.ProtocolVersion));

            var list = new StackPanel();
            foreach (var row in rows)
            {
                if (string.IsNullOrEmpty(row.Value)) continue;
                list.Children.Add(DetailRow(row.Key, row.Value));
            }

            // An empty box under a heading is how a window says "something
            // went wrong here" without meaning to. When Heron did not start
            // there is no identity and therefore no row, so the whole section
            // goes rather than standing there hollow.
            if (list.Children.Count > 0)
            {
                _details.Children.Add(Caption("THIS SESSION"));
                _details.Children.Add(new Border
                {
                    Padding = new Thickness(14, 10, 14, 11),
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(InsetColour),
                    Margin = new Thickness(0, 7, 0, 0),
                    Child = list,
                });
            }

            if (_status.State == HeronBridgeState.Connected
                && !string.IsNullOrEmpty(_status.DiscoveryFilePath))
            {
                _details.Children.Add(Caption("ANNOUNCED IN"));
                _details.Children.Add(PathLine(_status.DiscoveryFilePath));
            }

            if (_status.State == HeronBridgeState.DidNotStart
                && !string.IsNullOrEmpty(_status.LogFolder))
            {
                _details.Children.Add(Caption("LOGS"));
                _details.Children.Add(PathLine(_status.LogFolder));
            }
        }

        private void RenderButtons()
        {
            switch (_status.State)
            {
                case HeronBridgeState.Connected:
                    // Close is the default, NOT Disconnect. Enter pressed on a
                    // window somebody opened in order to READ would otherwise
                    // cut the session off, and that is the only thing here
                    // with a consequence.
                    SetPrimary("Close", Close);
                    SetSecondary("Disconnect", Toggle);
                    break;

                case HeronBridgeState.NotConnected:
                    SetPrimary("Connect this session", Toggle);
                    SetSecondary("Close", Close);
                    break;

                default:
                    SetPrimary("Close", Close);
                    SetSecondary("Open log folder", OpenLogFolder);
                    break;
            }
        }

        // ==================================================================
        // Actions
        // ==================================================================

        private void Toggle()
        {
            if (_toggle == null) return;
            try
            {
                _toggle();

                // Read the state back rather than assume the toggle did what
                // it was asked. A bridge that half-started and rolled itself
                // back must not leave this window claiming otherwise - the
                // same rule the ribbon icon follows.
                var after = _read();
                if (after != null) _status = after;
                Render();
            }
            catch (Exception ex)
            {
                // In the footer, not in a second dialog on top of this one.
                // The user is already looking here.
                Note("Could not change the connection: " + ex.Message, DangerColour);
                _log("Bridge Status toggle failed: " + ex);

                try
                {
                    var after = _read();
                    if (after != null) { _status = after; Render(); }
                }
                catch (Exception readError)
                {
                    _log("Bridge Status could not re-read the bridge: " + readError.Message);
                }
            }
        }

        private void CopyDetails()
        {
            // Only what is actually known. A pasted report reading
            // "Process:" with nothing after it makes the reader wonder what
            // was lost in the copy, and the answer is nothing - there was
            // never a value.
            var text = new StringBuilder();
            text.AppendLine("Heron bridge status");
            text.AppendLine("State:     " + _headline.Text);
            if (_status.State == HeronBridgeState.Connected)
                Line(text, "Pipe:      ", _status.PipeName);
            Line(text, "Process:   ", _status.ProcessId);
            Line(text, "Revit:     ", _status.RevitVersion);
            Line(text, "Add-in:    ", _status.AddinVersion);
            Line(text, "Protocol:  ", _status.ProtocolVersion);
            if (_status.State != HeronBridgeState.DidNotStart)
                text.AppendLine("Changes:   " + (_status.WriteEnabled ? "ON" : "off"));
            if (_status.State == HeronBridgeState.Connected)
                Line(text, "Announced: ", _status.DiscoveryFilePath);
            Line(text, "Logs:      ", _status.LogFolder);

            try
            {
                // Clipboard.SetText fails often enough inside a host
                // application to be worth catching: another process holding
                // the clipboard open returns CLIPBRD_E_CANT_OPEN, and an
                // unhandled one would take the window down over a convenience.
                Clipboard.SetText(text.ToString());
                Note("Copied. Paste it wherever you are reporting this.", TextMuted);
            }
            catch (Exception ex)
            {
                Note("Windows would not let Heron use the clipboard just now.", DangerColour);
                _log("Bridge Status could not copy to the clipboard: " + ex.Message);
            }
        }

        private static void Line(StringBuilder text, string label, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            text.AppendLine(label + value);
        }

        private void OpenLogFolder()
        {
            var folder = _status.LogFolder;
            if (string.IsNullOrEmpty(folder)) return;
            try
            {
                // UseShellExecute set rather than left to the default: it is
                // true on .NET Framework and FALSE on .NET 8 and 10, so Revit
                // 2025 and up would fail to open a folder that opened fine on
                // 2024. One property, three runtimes, same behaviour.
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                Note("Could not open that folder. The path is above.", DangerColour);
                _log("Bridge Status could not open the log folder: " + ex.Message);
            }
        }

        private void Close()
        {
            try { _window.Close(); }
            catch (Exception ex) { _log("Bridge Status would not close: " + ex.Message); }
        }

        private void Invoke(Action action)
        {
            if (action == null) return;
            try { action(); }
            catch (Exception ex) { _log("Bridge Status action failed: " + ex); }
        }

        // ==================================================================
        // Small parts
        // ==================================================================

        private static Color StateColour(HeronBridgeState state)
        {
            if (state == HeronBridgeState.Connected) return AccentColour;
            if (state == HeronBridgeState.DidNotStart) return DangerColour;
            return QuietColour;
        }

        private void SetPrimary(string label, Action action)
        {
            _primary.Content = label;
            _primaryAction = action;
        }

        private void SetSecondary(string label, Action action)
        {
            _secondary.Content = label;
            _secondaryAction = action;
            _secondary.Visibility = action == null
                ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <summary>
        /// One line in the footer that clears itself. This is the "status line
        /// instead of a popup" rule with the smallest surface it can have: it
        /// never blocks, never needs dismissing, and is gone before it becomes
        /// clutter.
        /// </summary>
        private void Note(string text, Color colour)
        {
            _note.Text = text;
            _note.Foreground = new SolidColorBrush(colour);

            StopNoteTimer();
            _noteTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
            _noteTimer.Tick += delegate
            {
                StopNoteTimer();
                _note.Text = string.Empty;
            };
            _noteTimer.Start();
        }

        private void StopNoteTimer()
        {
            if (_noteTimer == null) return;
            _noteTimer.Stop();
            _noteTimer = null;
        }

        private static TextBlock Caption(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily(BodyFont),
                FontSize = 10.5,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(TextMuted),
                Margin = new Thickness(2, 15, 0, 0),
            };
        }

        private static UIElement DetailRow(string label, string value)
        {
            var name = new TextBlock
            {
                Text = label,
                Width = 80,
                FontFamily = new FontFamily(BodyFont),
                FontSize = 12.5,
                Foreground = new SolidColorBrush(TextSecondary),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var read = new TextBlock
            {
                Text = value,
                FontFamily = new FontFamily(MonoFont),
                FontSize = 12.5,
                Foreground = new SolidColorBrush(TextPrimary),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };

            // Same reason as the footer: inside a horizontal StackPanel the
            // value is measured against infinite width, so TextTrimming never
            // fires and a long one simply leaves the card. Nothing here is
            // long today - a pipe name is sixteen characters - which is
            // exactly the kind of thing that stays true until it does not.
            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });

            Grid.SetColumn(name, 0);
            Grid.SetColumn(read, 1);
            row.Children.Add(name);
            row.Children.Add(read);
            return row;
        }

        private static UIElement PathLine(string path)
        {
            return new Border
            {
                Margin = new Thickness(0, 7, 0, 0),
                Padding = new Thickness(14, 10, 14, 10),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(InsetColour),
                Child = new TextBlock
                {
                    Text = path,
                    FontFamily = new FontFamily(MonoFont),
                    FontSize = 11.5,
                    Foreground = new SolidColorBrush(TextSecondary),
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 17,
                },
            };
        }

        private static Button PrimaryButton()
        {
            return new Button
            {
                FontFamily = new FontFamily(BodyFont),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x0B, 0x14, 0x1A)),
                Padding = new Thickness(18, 9, 18, 10),
                Margin = new Thickness(9, 0, 0, 0),
                Cursor = Cursors.Hand,
                Template = SolidButtonTemplate(AccentColour),
            };
        }

        private static Button GhostButton()
        {
            return new Button
            {
                FontFamily = new FontFamily(BodyFont),
                FontSize = 13,
                Foreground = new SolidColorBrush(TextSecondary),
                Padding = new Thickness(16, 9, 16, 10),
                Cursor = Cursors.Hand,
                Template = OutlineButtonTemplate(),
            };
        }

        // ------------------------------------------------------------------
        // Button templates.
        //
        // Written out rather than themed, because Revit's own resources are in
        // scope here and a plain WPF Button inherits a grey 3D chrome that
        // belongs to no decade anybody wants. A ControlTemplate is the only
        // way to be certain what this draws, on every release from 2020 to
        // 2027, without shipping a resource dictionary.
        //
        // The triggers carry a TARGET NAME. A Setter in ControlTemplate.
        // Triggers with no TargetName applies to the templated Button, not to
        // the Border inside it - so a background setter without one silently
        // does nothing, which is the easy mistake here and it is invisible
        // until somebody hovers.
        // ------------------------------------------------------------------

        private const string TemplateRoot = "HeronButtonRoot";

        private static ControlTemplate SolidButtonTemplate(Color fill)
        {
            var border = TemplateBorder(new SolidColorBrush(fill), 9);
            var template = new ControlTemplate(typeof(Button)) { VisualTree = border };

            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(UIElement.OpacityProperty, 0.87));
            template.Triggers.Add(hover);

            var pressed = new Trigger
            {
                Property = System.Windows.Controls.Primitives.ButtonBase.IsPressedProperty,
                Value = true,
            };
            pressed.Setters.Add(new Setter(UIElement.OpacityProperty, 0.70));
            template.Triggers.Add(pressed);

            return template;
        }

        private static ControlTemplate OutlineButtonTemplate()
        {
            var border = TemplateBorder(Brushes.Transparent, 9);
            border.SetValue(Border.BorderThicknessProperty, new Thickness(1));
            border.SetValue(Border.BorderBrushProperty,
                new SolidColorBrush(Color.FromArgb(0x3A, 0xFF, 0xFF, 0xFF)));

            var template = new ControlTemplate(typeof(Button)) { VisualTree = border };

            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty,
                new SolidColorBrush(Color.FromArgb(0x16, 0xFF, 0xFF, 0xFF)), TemplateRoot));
            template.Triggers.Add(hover);

            return template;
        }

        private static ControlTemplate FlatButtonTemplate(Color hoverColour, double radius)
        {
            var border = TemplateBorder(Brushes.Transparent, radius);
            var template = new ControlTemplate(typeof(Button)) { VisualTree = border };

            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty,
                new SolidColorBrush(hoverColour), TemplateRoot));
            template.Triggers.Add(hover);

            return template;
        }

        private static FrameworkElementFactory TemplateBorder(Brush background, double radius)
        {
            var border = new FrameworkElementFactory(typeof(Border), TemplateRoot);
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            border.SetValue(Border.BackgroundProperty, background);
            border.SetValue(Border.PaddingProperty,
                new TemplateBindingExtension(Control.PaddingProperty));

            var content = new FrameworkElementFactory(typeof(ContentPresenter));
            content.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            content.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(content);

            return border;
        }
    }
}
