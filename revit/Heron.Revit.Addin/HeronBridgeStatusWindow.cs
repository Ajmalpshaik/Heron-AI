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
        // The palette, the chrome and the buttons all come from
        // HeronWindowStyle, which exists so that the two Heron windows cannot
        // drift into two different darks. These are aliases so the drawing
        // code below reads as it always did.
        // ------------------------------------------------------------------
        private static readonly Color InsetColour = HeronWindowStyle.InsetColour;
        private static readonly Color TextPrimary = HeronWindowStyle.TextPrimary;
        private static readonly Color TextSecondary = HeronWindowStyle.TextSecondary;
        private static readonly Color TextMuted = HeronWindowStyle.TextMuted;
        private static readonly Color AccentColour = HeronWindowStyle.AccentColour;
        private static readonly Color WarnColour = HeronWindowStyle.WarnColour;
        private static readonly Color DangerColour = HeronWindowStyle.DangerColour;
        private static readonly Color QuietColour = HeronWindowStyle.QuietColour;

        private const double CardWidth = 470;
        private const double Gutter = HeronWindowStyle.Gutter;
        private const string BodyFont = HeronWindowStyle.BodyFont;
        private const string MonoFont = HeronWindowStyle.MonoFont;

        private readonly Func<HeronBridgeStatus> _read;
        private readonly Action _toggle;
        private readonly Action<string> _log;

        private HeronWindowStyle.Shell _shell;

        // Everything that changes when the state does. Held rather than
        // rebuilt, so connecting from inside the window redraws the answer
        // without the whole card flickering.
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
                _shell.Window.ShowDialog();
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
            body.Children.Add(BuildState());
            body.Children.Add(BuildWriteCard());
            body.Children.Add(BuildDetails());
            body.Children.Add(BuildFooter());

            // The card, the chrome, the drag, Esc and Revit as the owner all
            // come from HeronWindowStyle. What is left here is the only part
            // that is about BRIDGES rather than about windows.
            _shell = HeronWindowStyle.Build(
                "Bridge Status", CardWidth, body, StateColour(_status.State),
                Close, _log);
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

            var copy = HeronWindowStyle.FlatButton("Copy details", 12.5);
            copy.Click += delegate { CopyDetails(); };

            _secondary = HeronWindowStyle.GhostButton(string.Empty);
            _secondary.Click += delegate { Invoke(_secondaryAction); };

            _primary = HeronWindowStyle.PrimaryButton(string.Empty, AccentColour);
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

            return HeronWindowStyle.Footer(row);
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

            _shell.AccentStrip.Background = new SolidColorBrush(accent);
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
                _details.Children.Add(HeronWindowStyle.Caption("THIS SESSION"));
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
                _details.Children.Add(HeronWindowStyle.Caption("ANNOUNCED IN"));
                _details.Children.Add(HeronWindowStyle.PathLine(_status.DiscoveryFilePath));
            }

            if (_status.State == HeronBridgeState.DidNotStart
                && !string.IsNullOrEmpty(_status.LogFolder))
            {
                _details.Children.Add(HeronWindowStyle.Caption("LOGS"));
                _details.Children.Add(HeronWindowStyle.PathLine(_status.LogFolder));
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
            _shell.Close(_log);
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


        /// <summary>
        /// One line of the session list: a label, and a value in Consolas.
        ///
        /// A Grid, not a horizontal StackPanel. Inside a StackPanel the value
        /// is measured against infinite width, so TextTrimming never fires
        /// and a long one simply leaves the card. Nothing here is long today
        /// - a pipe name is sixteen characters - which is exactly the kind of
        /// thing that stays true until it does not.
        /// </summary>
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
    }
}
