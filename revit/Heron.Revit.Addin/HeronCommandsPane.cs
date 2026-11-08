// Heron-Agent:  HERON-USR-SKL-003
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Autodesk.Revit.UI;
using Heron.Core;
using TextBox = System.Windows.Controls.TextBox;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// THE HERON PANEL - the modeller's saved commands, docked in Revit like
    /// Dynamo Player (docs/49, the owner's idea of 2026-11-08).
    ///
    /// TWO PAGES, NOT ONE. The list page holds the search box and every saved
    /// command; a click opens that command's own page - its inputs, Run and
    /// the answer - with "All commands" to go back. The first version stacked
    /// the inputs under the list in one unscrolled column: the inputs pushed
    /// the list off the pane, a long answer ran off the bottom, and there was
    /// no way back to the other commands (Ajmal PS, Revit 2020, 2026-11-08).
    /// Each page scrolls on its own, so a narrow or short pane loses nothing.
    ///
    /// IT NEVER CALLS THE REVIT API. A dockable pane is modeless, so a click
    /// here is outside Revit's API context. Run starts the Python runner as a
    /// separate process and returns at once; the runner reaches this Revit
    /// over the pipe like any chat, so the change is made on Revit's thread by
    /// the dispatcher, through the gate, with Revit's own Keep or Discard
    /// question - and Revit stays free to ask it while this waits.
    ///
    /// NO RESTART FOR A NEW COMMAND: the folder is watched, so a command the
    /// chat saves appears here within a second.
    /// </summary>
    internal sealed class HeronCommandsPane : IDockablePaneProvider
    {
        /// <summary>Fixed for ever - Revit remembers where a pane was docked by it.</summary>
        internal static readonly DockablePaneId Id =
            new DockablePaneId(new Guid("6b7f6c4e-2d35-4b8a-9a3f-3c1e9f0a6d21"));

        internal const string Title = "Heron Commands";

        private const double Edge = 14;

        // Shades are brand White at an opacity (docs/brand, D-149).
        private static readonly Color HoverShade = Color.FromArgb(0x14, 0xFF, 0xFF, 0xFF);
        private static readonly Color LineShade = Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF);

        private readonly Action<string> _log;
        private readonly Grid _root = new Grid();

        // The list page.
        private readonly Grid _listPage = new Grid();
        private readonly TextBox _search = new TextBox();
        private readonly TextBlock _searchHint = new TextBlock();
        private readonly StackPanel _rows = new StackPanel();
        private readonly TextBlock _empty = new TextBlock();
        private readonly TextBlock _status = new TextBlock();

        // The command page.
        private readonly Grid _commandPage = new Grid();
        private readonly ScrollViewer _commandScroll = new ScrollViewer();
        private readonly StackPanel _detail = new StackPanel();
        private readonly Dictionary<string, TextBox> _boxes = new Dictionary<string, TextBox>();
        private TextBox _answer;
        private Button _run;

        private List<SavedCommand> _commands = new List<SavedCommand>();
        private SavedCommand _open;
        private FileSystemWatcher _watcher;
        private DispatcherTimer _settle;
        private bool _running;
        private string _runningName;
        private string _answerFor;
        private string _answerText = "";
        private string _unreadable = "";

        internal HeronCommandsPane(Action<string> log)
        {
            _log = log ?? (_ => { });
            Build();
            Reload();
            Watch();
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = _root;
            data.InitialState = new DockablePaneState { DockPosition = DockPosition.Right };
        }

        // ------------------------------------------------------------ drawing

        private void Build()
        {
            _root.Background = new SolidColorBrush(HeronWindowStyle.CardColour);
            BuildListPage();
            BuildCommandPage();
            _root.Children.Add(_listPage);
            _root.Children.Add(_commandPage);
            ShowList();
        }

        private void BuildListPage()
        {
            _listPage.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _listPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _listPage.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var top = new StackPanel { Margin = new Thickness(Edge, Edge, Edge, 8) };
            top.Children.Add(HeronWindowStyle.Body("Saved commands", HeronWindowStyle.TextPrimary, 16));
            var lead = HeronWindowStyle.Body(
                "Save a job in the chat, then run it here.", HeronWindowStyle.TextMuted, 11.5);
            lead.Margin = new Thickness(0, 2, 0, 0);
            top.Children.Add(lead);
            top.Children.Add(SearchBox());
            Grid.SetRow(top, 0);
            _listPage.Children.Add(top);

            // THE LIST SCROLLS ON ITS OWN: twenty commands in a short pane
            // keep the search box and the status line in view.
            var listArea = new Grid { Margin = new Thickness(Edge - 6, 0, Edge - 6, 0) };
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _rows,
            };
            listArea.Children.Add(scroll);
            _empty.FontFamily = new FontFamily(HeronWindowStyle.BodyFont);
            _empty.FontSize = 12;
            _empty.Foreground = new SolidColorBrush(HeronWindowStyle.TextMuted);
            _empty.TextWrapping = TextWrapping.Wrap;
            _empty.LineHeight = 19;
            _empty.Margin = new Thickness(6, 10, 6, 0);
            listArea.Children.Add(_empty);
            Grid.SetRow(listArea, 1);
            _listPage.Children.Add(listArea);

            Grid.SetRow(_status, 2);
            StatusLine(_status);
            _listPage.Children.Add(_status);
        }

        private UIElement SearchBox()
        {
            // A WATERMARK, NOT A LABEL: the hint sits inside the box and goes
            // the moment anything is typed. It takes no clicks, so a click on
            // it lands in the box.
            StyleInput(_search);
            _search.ToolTip = "Search your saved commands by name or description.";
            _search.TextChanged += (s, e) =>
            {
                _searchHint.Visibility = string.IsNullOrEmpty(_search.Text)
                    ? Visibility.Visible : Visibility.Collapsed;
                Fill();
            };
            _search.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) { _search.Text = ""; e.Handled = true; }
                else if (e.Key == Key.Enter && _rows.Children.Count == 1)
                {
                    var only = _rows.Children[0] as Button;
                    if (only != null) Open(only.Tag as SavedCommand);
                    e.Handled = true;
                }
            };

            _searchHint.Text = "Search commands";
            _searchHint.FontFamily = new FontFamily(HeronWindowStyle.BodyFont);
            _searchHint.FontSize = 12.5;
            _searchHint.Foreground = new SolidColorBrush(HeronWindowStyle.TextMuted);
            _searchHint.IsHitTestVisible = false;
            _searchHint.VerticalAlignment = VerticalAlignment.Center;
            _searchHint.Margin = new Thickness(11, 0, 0, 0);

            var holder = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            holder.Children.Add(_search);
            holder.Children.Add(_searchHint);
            return holder;
        }

        private void BuildCommandPage()
        {
            _commandPage.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _commandPage.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var back = HeronWindowStyle.FlatButton("←  All commands", 12.5);
            back.HorizontalAlignment = HorizontalAlignment.Left;
            back.Margin = new Thickness(Edge - 10, 8, 0, 0);
            back.ToolTip = "Back to the list of saved commands (Esc).";
            back.Click += (s, e) => ShowList();
            Grid.SetRow(back, 0);
            _commandPage.Children.Add(back);

            // THE WHOLE COMMAND PAGE SCROLLS: a command with many inputs, or an
            // answer of forty lines, never pushes Run or the answer out of reach.
            _detail.Margin = new Thickness(Edge, 6, Edge, Edge);
            _commandScroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _commandScroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            _commandScroll.Content = _detail;
            Grid.SetRow(_commandScroll, 1);
            _commandPage.Children.Add(_commandScroll);

            _commandPage.PreviewKeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) { ShowList(); e.Handled = true; }
            };
        }

        // ------------------------------------------------------------ the list page

        private void ShowList()
        {
            _open = null;
            _commandPage.Visibility = Visibility.Collapsed;
            _listPage.Visibility = Visibility.Visible;
            Fill();
            UpdateStatus();
        }

        private void Fill()
        {
            var wanted = (_search.Text ?? "").Trim();
            _rows.Children.Clear();
            foreach (var command in _commands)
            {
                if (wanted.Length > 0 &&
                    command.Name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) < 0 &&
                    command.Description.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                _rows.Children.Add(Row(command));
            }

            if (_commands.Count == 0)
            {
                _empty.Text = "No commands are saved yet.\n\nIn the Claude chat, after a job you liked, " +
                              "say \"save that as a command\" - it appears here at once.";
                _empty.Visibility = Visibility.Visible;
            }
            else if (_rows.Children.Count == 0)
            {
                _empty.Text = "No command matches \"" + wanted + "\".";
                _empty.Visibility = Visibility.Visible;
            }
            else
            {
                _empty.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// One command as a row: its name, what it does in a line, and whether
        /// it changes the model - the one thing to know before clicking Run.
        /// </summary>
        private Button Row(SavedCommand command)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var words = new StackPanel();
            var name = HeronWindowStyle.Body(command.Name, HeronWindowStyle.TextPrimary, 13);
            name.FontWeight = FontWeights.SemiBold;
            name.TextTrimming = TextTrimming.CharacterEllipsis;
            name.TextWrapping = TextWrapping.NoWrap;
            words.Children.Add(name);
            if (!string.IsNullOrEmpty(command.Description))
            {
                var line = HeronWindowStyle.Body(command.Description, HeronWindowStyle.TextMuted, 11.5);
                line.Margin = new Thickness(0, 2, 0, 0);
                line.MaxHeight = 2 * line.LineHeight;
                line.TextTrimming = TextTrimming.WordEllipsis;
                words.Children.Add(line);
            }
            grid.Children.Add(words);

            var tag = Badge(command.Changes);
            tag.Margin = new Thickness(10, 1, 0, 0);
            tag.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(tag, 1);
            grid.Children.Add(tag);

            var row = new Button
            {
                Content = grid,
                Tag = command,
                Padding = new Thickness(8, 9, 8, 10),
                Margin = new Thickness(0, 0, 0, 2),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Cursor = Cursors.Hand,
                Template = HeronWindowStyle.RowButtonTemplate(HoverShade, 8),
                ToolTip = "Open " + command.Name,
            };
            row.Click += (s, e) => Open(command);
            return row;
        }

        /// <summary>CHANGES in the warning colour, READS in the safe one.</summary>
        private static Border Badge(bool changes)
        {
            var colour = changes ? HeronWindowStyle.WarnColour : HeronWindowStyle.SafeColour;
            return new Border
            {
                CornerRadius = new CornerRadius(5),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(colour),
                Padding = new Thickness(6, 1, 6, 2),
                Child = new TextBlock
                {
                    Text = changes ? "CHANGES" : "READS",
                    FontFamily = new FontFamily(HeronWindowStyle.BodyFont),
                    FontSize = 9.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(colour),
                },
            };
        }

        private void UpdateStatus()
        {
            string text;
            if (_running) text = "Running " + _runningName + "...";
            else if (_unreadable.Length > 0) text = _unreadable;
            else if (_commands.Count > 0)
                text = _commands.Count == 1 ? "1 saved command" : _commands.Count + " saved commands";
            else text = "";
            _status.Text = text;
            _status.Visibility = text.Length == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        // ------------------------------------------------------------ the command page

        private void Open(SavedCommand command)
        {
            if (command == null) return;
            _open = command;
            DrawCommand(command);
            _listPage.Visibility = Visibility.Collapsed;
            _commandPage.Visibility = Visibility.Visible;
            _commandScroll.ScrollToTop();

            // THE CURSOR GOES WHERE THE HAND GOES NEXT: the first input, or
            // Run when there is nothing to type.
            _commandPage.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                TextBox first = null;
                foreach (var input in command.Inputs)
                    if (_boxes.TryGetValue(input.Name, out first)) break;
                if (first != null) { first.Focus(); first.SelectAll(); }
                else if (_run != null) _run.Focus();
            }));
        }

        private void DrawCommand(SavedCommand command)
        {
            _detail.Children.Clear();
            _boxes.Clear();

            var name = HeronWindowStyle.Body(command.Name, HeronWindowStyle.TextPrimary, 16);
            name.FontWeight = FontWeights.SemiBold;
            _detail.Children.Add(name);
            if (!string.IsNullOrEmpty(command.Description))
            {
                var about = HeronWindowStyle.Body(command.Description, HeronWindowStyle.TextSecondary, 12);
                about.Margin = new Thickness(0, 4, 0, 0);
                _detail.Children.Add(about);
            }

            var risk = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
            risk.Children.Add(Badge(command.Changes));
            var riskWords = HeronWindowStyle.Body(
                command.Changes ? "Revit asks Keep or Discard before anything is kept."
                                : "Only reads the model. Nothing is changed.",
                HeronWindowStyle.TextMuted, 11.5);
            riskWords.Margin = new Thickness(8, 0, 0, 0);
            riskWords.VerticalAlignment = VerticalAlignment.Center;
            risk.Children.Add(riskWords);
            _detail.Children.Add(risk);

            if (command.Inputs.Count > 0)
            {
                _detail.Children.Add(Divider());
                foreach (var input in command.Inputs)
                {
                    var caption = HeronWindowStyle.Caption(input.Label);
                    caption.Margin = new Thickness(1, 10, 0, 4);
                    _detail.Children.Add(caption);
                    var box = new TextBox { Text = input.Default };
                    StyleInput(box);
                    box.KeyDown += (s, e) =>
                    {
                        if (e.Key == Key.Enter) { Run(command); e.Handled = true; }
                    };
                    _boxes[input.Name] = box;
                    _detail.Children.Add(box);
                }
            }

            _run = HeronWindowStyle.PrimaryButton("Run", HeronWindowStyle.AccentColour);
            _run.Margin = new Thickness(0, 18, 0, 0);
            _run.MinWidth = 96;
            _run.HorizontalAlignment = HorizontalAlignment.Left;
            _run.ToolTip = "Run " + command.Name + " on the model in front (Enter).";
            _run.Click += (s, e) => Run(command);
            _detail.Children.Add(_run);

            // THE ANSWER IS TEXT YOU CAN SELECT AND COPY - a sheet number or
            // an element id from it is often the next thing typed somewhere.
            _detail.Children.Add(Divider());
            var heading = HeronWindowStyle.Caption("ANSWER");
            heading.Margin = new Thickness(1, 10, 0, 4);
            _detail.Children.Add(heading);
            _answer = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                MinHeight = 64,
                FontFamily = new FontFamily(HeronWindowStyle.BodyFont),
                FontSize = 12,
                Foreground = new SolidColorBrush(HeronWindowStyle.TextSecondary),
                Background = new SolidColorBrush(HeronWindowStyle.InsetColour),
                BorderBrush = new SolidColorBrush(LineShade),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 7, 8, 7),
                CaretBrush = new SolidColorBrush(HeronWindowStyle.TextPrimary),
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };
            _detail.Children.Add(_answer);

            RefreshCommandPage();
        }

        /// <summary>Run's state and the answer, for the command on show now.</summary>
        private void RefreshCommandPage()
        {
            if (_open == null) return;
            var thisOne = _running && _runningName == _open.Name;
            if (_run != null)
            {
                _run.IsEnabled = !_running;
                _run.Content = thisOne ? "Running..." : "Run";
                _run.ToolTip = _running && !thisOne
                    ? _runningName + " is still running - one command at a time."
                    : "Run " + _open.Name + " on the model in front (Enter).";
            }
            if (_answer != null)
            {
                if (thisOne) _answer.Text = "Running " + _open.Name + "... Revit may ask you Keep or Discard.";
                else if (_answerFor == _open.Name) _answer.Text = _answerText;
                else _answer.Text = "Nothing run yet. Set the values above and press Run.";
                _answer.Foreground = new SolidColorBrush(
                    _answerFor == _open.Name || thisOne ? HeronWindowStyle.TextSecondary : HeronWindowStyle.TextMuted);
            }
        }

        private static Border Divider()
        {
            return new Border
            {
                Height = 1,
                Background = new SolidColorBrush(LineShade),
                Margin = new Thickness(0, 16, 0, 2),
            };
        }

        private static void StyleInput(TextBox box)
        {
            box.FontFamily = new FontFamily(HeronWindowStyle.BodyFont);
            box.FontSize = 12.5;
            box.Padding = new Thickness(8, 6, 8, 6);
            box.Foreground = new SolidColorBrush(HeronWindowStyle.TextPrimary);
            box.Background = new SolidColorBrush(HeronWindowStyle.InsetColour);
            box.BorderBrush = new SolidColorBrush(LineShade);
            box.BorderThickness = new Thickness(1);
            box.CaretBrush = new SolidColorBrush(HeronWindowStyle.TextPrimary);
            box.SelectionBrush = new SolidColorBrush(HeronWindowStyle.AccentColour);
        }

        private static void StatusLine(TextBlock line)
        {
            line.FontFamily = new FontFamily(HeronWindowStyle.BodyFont);
            line.FontSize = 11;
            line.Foreground = new SolidColorBrush(HeronWindowStyle.TextMuted);
            line.TextWrapping = TextWrapping.Wrap;
            line.Margin = new Thickness(Edge, 8, Edge, 12);
        }

        // ------------------------------------------------------------ the folder

        private void Reload()
        {
            List<string> unreadable;
            try { _commands = HeronCommandStore.Load(out unreadable); }
            catch (Exception ex)
            {
                _log("Heron panel could not read the saved commands: " + ex.Message);
                _commands = new List<SavedCommand>();
                unreadable = new List<string>();
            }
            _unreadable = unreadable.Count > 0
                ? "Could not read: " + string.Join(", ", unreadable.ToArray())
                : "";

            // A COMMAND ON SHOW STAYS ON SHOW while the folder changes round
            // it - what was typed in its boxes is kept. Only when it was
            // deleted does the page go back to the list.
            if (_open != null)
            {
                SavedCommand still = null;
                foreach (var command in _commands)
                    if (string.Equals(command.Name, _open.Name, StringComparison.OrdinalIgnoreCase)) still = command;
                if (still == null) ShowList();
                else _open = still;
            }
            Fill();
            UpdateStatus();
        }

        private void Watch()
        {
            var folder = HeronPaths.CommandsPersonal;
            if (folder == null) return;
            try
            {
                // WATCH THE PARENT TOO: "commands\personal" does not exist
                // until the first save, and a watcher on a missing folder
                // throws. Created, Changed, Renamed and Deleted all mean "read
                // it again"; a short pause lets a save finish first.
                var parent = Path.GetDirectoryName(folder);
                if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);
                _watcher = new FileSystemWatcher(parent)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName,
                };
                FileSystemEventHandler changed = (s, e) => Later();
                _watcher.Created += changed;
                _watcher.Changed += changed;
                _watcher.Deleted += changed;
                _watcher.Renamed += (s, e) => Later();
                _watcher.EnableRaisingEvents = true;

                _settle = new DispatcherTimer(DispatcherPriority.Background, _root.Dispatcher)
                {
                    Interval = TimeSpan.FromMilliseconds(400),
                };
                _settle.Tick += (s, e) => { _settle.Stop(); Reload(); };
            }
            catch (Exception ex)
            {
                _log("Heron panel cannot watch for new commands: " + ex.Message);
            }
        }

        private void Later()
        {
            // The watcher's thread: hop to the pane's own before touching it.
            _root.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_settle == null) return;
                _settle.Stop();
                _settle.Start();
            }));
        }

        // ------------------------------------------------------------ running

        private void Run(SavedCommand command)
        {
            if (_running) return;
            var inputs = new List<KeyValuePair<string, string>>();
            foreach (var input in command.Inputs)
            {
                TextBox box;
                var value = _boxes.TryGetValue(input.Name, out box) ? (box.Text ?? "") : "";
                inputs.Add(new KeyValuePair<string, string>(input.Name, value));
            }

            string why;
            var start = HeronCommandStore.RunnerStart(command.Name,
                Process.GetCurrentProcess().Id, inputs, out why);
            if (start == null)
            {
                Answer(command.Name, why);
                return;
            }

            Busy(command.Name);
            try
            {
                var process = Process.Start(start);
                _log("Heron panel started saved command '" + command.Name + "'.");
                // BOTH STREAMS READ AT ONCE, off Revit's thread: a process
                // whose answer outgrows the pipe's buffer waits for a reader,
                // so reading one stream only after it exits can wait for ever.
                var name = command.Name;
                System.Threading.Tasks.Task.Run(() => Finished(process, name));
            }
            catch (Exception ex)
            {
                Answer(command.Name, "Heron could not start the command: " + ex.Message +
                                     ". Nothing was sent to Revit.");
            }
        }

        private void Finished(Process process, string name)
        {
            // Read on a worker thread; shown on the pane's.
            string output, error;
            int code;
            try
            {
                var reading = process.StandardOutput.ReadToEndAsync();
                error = process.StandardError.ReadToEnd().Trim();
                output = reading.Result.Trim();
                process.WaitForExit();
                code = process.ExitCode;
            }
            catch (Exception ex)
            {
                output = "";
                error = ex.Message;
                code = -1;
            }
            finally
            {
                process.Dispose();
            }
            var said = output.Length > 0 ? output
                : "The command '" + name + "' ended without an answer (exit " + code + "). " +
                  (error.Length > 0 ? error : "See the Heron log.");
            _log("Heron panel: '" + name + "' ended, exit " + code + ".");
            _root.Dispatcher.BeginInvoke(new Action(() => Answer(name, said)));
        }

        private void Busy(string name)
        {
            _running = true;
            _runningName = name;
            UpdateStatus();
            RefreshCommandPage();
        }

        /// <summary>
        /// The answer, kept with the command it belongs to - so going back to
        /// the list while it runs loses nothing, and opening it again shows it.
        /// </summary>
        private void Answer(string name, string said)
        {
            _running = false;
            _runningName = null;
            _answerFor = name;
            _answerText = said ?? "";
            UpdateStatus();
            if (_open == null && _listPage.Visibility == Visibility.Visible)
                _status.Text = name + " finished - open it to read the answer.";
            RefreshCommandPage();
        }
    }
}
