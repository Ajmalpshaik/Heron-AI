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
using System.Windows.Media;
using System.Windows.Threading;
using Autodesk.Revit.UI;
using Heron.Core;
using TextBox = System.Windows.Controls.TextBox;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// THE HERON PANEL - the modeller's saved commands, docked in Revit like
    /// Dynamo Player (docs/49, the owner's idea of 2026-11-08). A list, a
    /// search box, the chosen command's inputs, and Run.
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

        private readonly Action<string> _log;
        private readonly Grid _root = new Grid();
        private readonly TextBox _search = new TextBox();
        private readonly ListBox _list = new ListBox();
        private readonly StackPanel _detail = new StackPanel();
        private readonly TextBlock _answer = new TextBlock();
        private readonly Dictionary<string, TextBox> _boxes = new Dictionary<string, TextBox>();
        private List<SavedCommand> _commands = new List<SavedCommand>();
        private FileSystemWatcher _watcher;
        private DispatcherTimer _settle;
        private Button _run;
        private bool _running;

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
            _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var top = new StackPanel { Margin = new Thickness(12, 12, 12, 6) };
            top.Children.Add(HeronWindowStyle.Body("Saved commands", HeronWindowStyle.TextPrimary, 15));
            top.Children.Add(HeronWindowStyle.Body(
                "Save a job in the chat, then run it here.", HeronWindowStyle.TextMuted, 11.5));
            _search.Margin = new Thickness(0, 10, 0, 0);
            _search.Padding = new Thickness(6, 4, 6, 4);
            _search.FontFamily = new FontFamily(HeronWindowStyle.BodyFont);
            _search.ToolTip = "Search your saved commands.";
            _search.TextChanged += (s, e) => Fill();
            top.Children.Add(_search);
            Grid.SetRow(top, 0);
            _root.Children.Add(top);

            _list.Margin = new Thickness(12, 0, 12, 6);
            _list.Background = new SolidColorBrush(HeronWindowStyle.InsetColour);
            _list.Foreground = new SolidColorBrush(HeronWindowStyle.TextPrimary);
            _list.BorderThickness = new Thickness(0);
            _list.FontFamily = new FontFamily(HeronWindowStyle.BodyFont);
            _list.SelectionChanged += (s, e) => Show(_list.SelectedItem as SavedCommand);
            Grid.SetRow(_list, 1);
            _root.Children.Add(_list);

            var bottom = new StackPanel { Margin = new Thickness(12, 0, 12, 12) };
            bottom.Children.Add(_detail);
            _answer.Margin = new Thickness(0, 10, 0, 0);
            _answer.TextWrapping = TextWrapping.Wrap;
            _answer.FontFamily = new FontFamily(HeronWindowStyle.BodyFont);
            _answer.FontSize = 11.5;
            _answer.Foreground = new SolidColorBrush(HeronWindowStyle.TextSecondary);
            bottom.Children.Add(_answer);
            Grid.SetRow(bottom, 2);
            _root.Children.Add(bottom);
        }

        private void Fill()
        {
            var wanted = (_search.Text ?? "").Trim();
            var picked = _list.SelectedItem as ListBoxItem;
            var chosen = picked == null ? null : picked.Tag as SavedCommand;
            _list.Items.Clear();
            foreach (var command in _commands)
            {
                if (wanted.Length > 0 &&
                    command.Name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) < 0 &&
                    command.Description.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                _list.Items.Add(new ListBoxItem
                {
                    Content = command.Name,
                    Tag = command,
                    ToolTip = string.IsNullOrEmpty(command.Description) ? null : command.Description,
                    Padding = new Thickness(8, 5, 8, 5),
                });
            }
            // Keep the one that was chosen, found again by name - the list is
            // rebuilt whenever the folder changes.
            for (var i = 0; chosen != null && i < _list.Items.Count; i++)
            {
                var item = (ListBoxItem)_list.Items[i];
                if (((SavedCommand)item.Tag).Name == chosen.Name) { _list.SelectedIndex = i; return; }
            }
            if (_list.Items.Count == 0) Show(null);
        }

        private void Show(object picked)
        {
            var item = picked as ListBoxItem;
            var command = item != null ? item.Tag as SavedCommand : picked as SavedCommand;
            _detail.Children.Clear();
            _boxes.Clear();
            _run = null;
            if (command == null)
            {
                _detail.Children.Add(HeronWindowStyle.Body(
                    _commands.Count == 0
                        ? "No commands are saved yet. In the chat, after a job, say \"save that as a command\"."
                        : "Pick a command.",
                    HeronWindowStyle.TextMuted, 11.5));
                return;
            }

            _detail.Children.Add(HeronWindowStyle.Body(command.Name, HeronWindowStyle.TextPrimary, 13));
            if (!string.IsNullOrEmpty(command.Description))
                _detail.Children.Add(HeronWindowStyle.Body(command.Description, HeronWindowStyle.TextMuted, 11.5));
            _detail.Children.Add(HeronWindowStyle.Body(
                command.Changes ? "Changes the model. Revit asks Keep or Discard." : "Only reads the model.",
                command.Changes ? HeronWindowStyle.WarnColour : HeronWindowStyle.SafeColour, 11));

            foreach (var input in command.Inputs)
            {
                var caption = HeronWindowStyle.Caption(input.Label);
                caption.Margin = new Thickness(0, 8, 0, 2);
                _detail.Children.Add(caption);
                var box = new TextBox
                {
                    Text = input.Default,
                    Padding = new Thickness(6, 4, 6, 4),
                    FontFamily = new FontFamily(HeronWindowStyle.BodyFont),
                };
                _boxes[input.Name] = box;
                _detail.Children.Add(box);
            }

            _run = HeronWindowStyle.PrimaryButton("Run", HeronWindowStyle.AccentColour);
            _run.Margin = new Thickness(0, 12, 0, 0);
            _run.HorizontalAlignment = HorizontalAlignment.Left;
            _run.IsEnabled = !_running;
            _run.ToolTip = "Run this command on the model in front.";
            _run.Click += (s, e) => Run(command);
            _detail.Children.Add(_run);
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
            Fill();
            if (_list.SelectedItem == null) Show(null);
            if (unreadable.Count > 0)
                _answer.Text = "Could not read: " + string.Join(", ", unreadable.ToArray());
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
                _answer.Text = why;
                return;
            }

            Busy(true, "Running " + command.Name + "...");
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
                Busy(false, "Heron could not start the command: " + ex.Message +
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
            _root.Dispatcher.BeginInvoke(new Action(() => Busy(false, said)));
        }

        private void Busy(bool running, string said)
        {
            _running = running;
            if (_run != null) _run.IsEnabled = !running;
            _answer.Text = said ?? "";
        }
    }
}
