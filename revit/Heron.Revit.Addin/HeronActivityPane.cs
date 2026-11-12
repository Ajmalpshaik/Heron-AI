// Heron-Agent:  none
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Autodesk.Revit.UI;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// THE ACTIVITY PANE (D-163) - Heron's recent changes, docked in Revit, each
    /// with Select and Zoom, and Undo on the newest one kept.
    ///
    /// IT NEVER CALLS THE REVIT API. A dockable pane is modeless, so a click
    /// here is outside Revit's API context. Each button hands a request to the
    /// add-in's ONE dispatcher (D-09) from a background thread and returns at
    /// once; the dispatcher raises its one ExternalEvent, and the work runs on
    /// Revit's thread through the gate, the model hold and the audit trail -
    /// exactly as a chat's request does. The answer comes back to the status
    /// line on this pane's own thread.
    ///
    /// WHAT IT LISTS IS THE RUN RECORDS (HeronActivity), read every few
    /// seconds while the pane is shown - never a list of its own.
    ///
    /// UNDO IS REVIT'S OWN, AND GUARDED. The button sends undo_last (D-157):
    /// the add-in posts Revit's Undo only while Heron's last kept change is
    /// still the top of the undo list, and says why not otherwise. It needs
    /// Changes ON, as it changes the model.
    /// </summary>
    internal sealed class HeronActivityPane : IDockablePaneProvider
    {
        /// <summary>Fixed for ever - Revit remembers where a pane was docked by it.</summary>
        internal static readonly DockablePaneId Id =
            new DockablePaneId(new Guid("a3e1c7d2-5b84-4f0e-9c61-2d8f4b7e1a95"));

        internal const string Title = "Heron Activity";

        private const double Edge = 14;

        // Shades are brand White at an opacity (docs/brand, D-149).
        private static readonly Color LineShade = Color.FromArgb(0x24, 0xFF, 0xFF, 0xFF);

        private readonly Func<string, string> _dispatch;
        private readonly Action<string> _log;
        private readonly Grid _root = new Grid();
        private readonly StackPanel _rows = new StackPanel();
        private readonly TextBlock _empty = new TextBlock();
        private readonly TextBlock _status = new TextBlock();
        private DispatcherTimer _timer;
        private string _shown = "";
        private bool _busy;

        /// <param name="dispatch">The add-in's one dispatcher: a request in, its answer out.
        /// Called from a background thread only.</param>
        internal HeronActivityPane(Func<string, string> dispatch, Action<string> log)
        {
            _dispatch = dispatch;
            _log = log ?? (_ => { });
            Build();
            Refresh();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _timer.Tick += (s, e) => { if (_root.IsVisible) Refresh(); };
            _timer.Start();
        }

        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = _root;
            data.InitialState = new DockablePaneState { DockPosition = DockPosition.Right };
        }

        private void Build()
        {
            _root.Background = new SolidColorBrush(HeronWindowStyle.CardColour);
            _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var top = new StackPanel { Margin = new Thickness(Edge, Edge, Edge, 8) };
            top.Children.Add(HeronWindowStyle.Body("Heron activity", HeronWindowStyle.TextPrimary, 16));
            var lead = HeronWindowStyle.Body("Heron's recent changes, newest first.",
                                             HeronWindowStyle.TextMuted, 11.5);
            lead.Margin = new Thickness(0, 2, 0, 0);
            top.Children.Add(lead);
            Grid.SetRow(top, 0);
            _root.Children.Add(top);

            var area = new Grid { Margin = new Thickness(Edge - 6, 0, Edge - 6, 0) };
            area.Children.Add(new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = _rows,
            });
            _empty.FontFamily = new FontFamily(HeronWindowStyle.BodyFont);
            _empty.FontSize = 12;
            _empty.Foreground = new SolidColorBrush(HeronWindowStyle.TextMuted);
            _empty.TextWrapping = TextWrapping.Wrap;
            _empty.Margin = new Thickness(6, 10, 6, 0);
            _empty.Text = "No change from Heron is recorded on this PC yet. Each change a chat makes "
                        + "appears here.";
            area.Children.Add(_empty);
            Grid.SetRow(area, 1);
            _root.Children.Add(area);

            _status.FontFamily = new FontFamily(HeronWindowStyle.BodyFont);
            _status.FontSize = 11;
            _status.Foreground = new SolidColorBrush(HeronWindowStyle.TextMuted);
            _status.TextWrapping = TextWrapping.Wrap;
            _status.Margin = new Thickness(Edge, 8, Edge, 12);
            Grid.SetRow(_status, 2);
            _root.Children.Add(_status);
        }

        /// <summary>Re-reads the records; redraws only when they changed.</summary>
        private void Refresh()
        {
            List<HeronActivity.Row> rows;
            try { rows = HeronActivity.Read(HeronPaths.Runs); }
            catch (Exception ex)
            {
                _log("Heron Activity could not read the run records: " + ex.Message);
                rows = new List<HeronActivity.Row>();
            }

            var key = new System.Text.StringBuilder();
            foreach (var row in rows) key.Append(row.Run).Append('|');
            if (key.ToString() == _shown) return;
            _shown = key.ToString();

            _rows.Children.Clear();
            var undoable = HeronActivity.Undoable(rows);
            foreach (var row in rows) _rows.Children.Add(Draw(row, ReferenceEquals(row, undoable)));
            _empty.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private UIElement Draw(HeronActivity.Row row, bool undoable)
        {
            var box = new StackPanel { Margin = new Thickness(6, 6, 6, 8) };

            var name = HeronWindowStyle.Body(row.Fragment, HeronWindowStyle.TextPrimary, 13);
            name.FontWeight = FontWeights.SemiBold;
            name.TextTrimming = TextTrimming.CharacterEllipsis;
            name.TextWrapping = TextWrapping.NoWrap;
            box.Children.Add(name);

            var when = row.AtUtc.HasValue ? row.AtUtc.Value.ToLocalTime().ToString("HH:mm") + "  ·  " : "";
            var line = HeronWindowStyle.Body(when + row.Document, HeronWindowStyle.TextMuted, 11.5);
            line.TextTrimming = TextTrimming.CharacterEllipsis;
            box.Children.Add(line);

            var outcome = HeronWindowStyle.Body(row.Outcome + "  ·  " + row.Counts,
                row.Kept ? HeronWindowStyle.TextSecondary : HeronWindowStyle.TextMuted, 11.5);
            outcome.Margin = new Thickness(0, 2, 0, 0);
            box.Children.Add(outcome);

            var tools = new WrapPanel { Margin = new Thickness(0, 6, 0, 0) };
            if (row.Ids.Count > 0)
            {
                tools.Children.Add(Tool("Select", "Select this change's " + row.Ids.Count
                    + " element(s) in Revit. Nothing in the model changes.",
                    () => HeronActivity.ShowRequest(row, false)));
                tools.Children.Add(Tool("Zoom", "Select them and zoom the view to them.",
                    () => HeronActivity.ShowRequest(row, true)));
            }
            if (undoable)
            {
                tools.Children.Add(Tool("Undo", "Revit's own Undo for this change - only while it is "
                    + "still the top of the undo list. Needs Changes on.", HeronActivity.UndoRequest));
            }
            if (tools.Children.Count > 0) box.Children.Add(tools);

            return new Border
            {
                BorderBrush = new SolidColorBrush(LineShade),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = box,
            };
        }

        private Button Tool(string label, string tip, Func<string> request)
        {
            var button = HeronWindowStyle.FlatButton(label, 11.5);
            button.ToolTip = tip;
            button.Margin = new Thickness(0, 0, 6, 0);
            button.Click += (s, e) => Send(label, request());
            return button;
        }

        /// <summary>
        /// One request to the dispatcher, OFF this thread - the dispatcher waits
        /// for Revit's thread, which is this one, so waiting here would wait for
        /// itself. One at a time; the answer lands on the status line.
        /// </summary>
        private void Send(string what, string request)
        {
            if (_busy || _dispatch == null) return;
            _busy = true;
            _status.Text = what + "...";
            var ui = _root.Dispatcher;
            Task.Run(() =>
            {
                string said;
                try { said = HeronActivity.Said(_dispatch(request)); }
                catch (Exception ex) { said = "Heron could not send that to Revit: " + ex.Message; }
                ui.BeginInvoke(new Action(() =>
                {
                    _status.Text = said;
                    _busy = false;
                    _shown = "";
                    Refresh();
                }));
            });
        }
    }
}
