// Heron-Agent:  HERON-REVIT-UI-022
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// What one switch above Changes asks before it goes on - D-106. The
    /// words and nothing else; HeronSwitchWindow decides how they are drawn,
    /// and AdminToggleCommand and PublishToggleCommand decide what they say.
    /// </summary>
    internal sealed class HeronSwitchQuestion
    {
        /// <summary>The switch's own word - "Admin", "Publish". Also the window's subtitle.</summary>
        internal string Name { get; set; }

        /// <summary>The one large line: what the person is being asked for.</summary>
        internal string Headline { get; set; }

        /// <summary>The paragraph under it: what Heron will be able to do.</summary>
        internal string What { get; set; }

        /// <summary>
        /// The ticked rows. EACH MUST BE TRUE IN THE CODE, for the reason
        /// HeronChangesWindow gives about its own three: a promise put where
        /// the decision is made is a rail, and if one stops being true this
        /// list is the first thing that has to change.
        /// </summary>
        internal string[] Promises { get; set; }

        /// <summary>
        /// What this switch does that Ctrl+Z cannot take back, in amber
        /// beside the lamp. Said on the way in because it is the one thing
        /// the Changes window's "every change is one Ctrl+Z" does not cover.
        /// </summary>
        internal string Caution { get; set; }

        /// <summary>Null, or one amber line when Changes is off right now.</summary>
        internal string ChangesOff { get; set; }

        /// <summary>The muted line under the card.</summary>
        internal string Note { get; set; }

        /// <summary>The amber button - "Turn Admin on".</summary>
        internal string TurnOn { get; set; }
    }

    /// <summary>
    /// The question the Admin and Publish buttons ask on the way on - D-106.
    ///
    /// HeronChangesWindow's shape, and deliberately NOT HeronChangesWindow.
    /// That window has been seen in Revit (NEEDS-CHECKING Z10) and this one
    /// has not, so it is left exactly as it was rather than bent to serve
    /// three switches. Whatever made it right is kept here on purpose:
    ///
    ///   * AMBER, not red and not green - turning on a switch the owner
    ///     reached for deliberately is neither danger nor good news.
    ///   * NO DEFAULT BUTTON. The main action grants a permission, and a
    ///     confirmation that leaning on Enter can clear is not one.
    ///   * Esc and the X both mean no, so every reflex that dismisses a
    ///     window lands on the safe answer.
    ///   * It asks on the way on and never on the way off.
    ///
    /// ONE THING IS ADDED: a line saying what Ctrl+Z cannot take back. An
    /// export is a file already written and a sync has already reached
    /// everybody on the project, so "every change is one Ctrl+Z" - true of
    /// Changes - is not a promise this window may make.
    ///
    /// IT TOUCHES NO REVIT API and decides nothing. It returns what the person
    /// said, and the command does the rest.
    /// </summary>
    internal sealed class HeronSwitchWindow
    {
        private const double CardWidth = 470;

        private readonly HeronSwitchQuestion _question;
        private readonly Action<string> _log;
        private HeronWindowStyle.Shell _shell;
        private bool _turnOn;

        private HeronSwitchWindow(HeronSwitchQuestion question, Action<string> log)
        {
            _question = question;
            _log = log ?? delegate { };
        }

        /// <summary>
        /// Asks. TRUE to turn the switch on, FALSE to leave it off, and NULL
        /// when the window could not be shown at all - which is not "no", and
        /// the caller asks again some other way (AskPlainly).
        /// </summary>
        internal static bool? Ask(HeronSwitchQuestion question, Action<string> log)
        {
            if (question == null) return null;
            return new HeronSwitchWindow(question, log).AskInternal();
        }

        private bool? AskInternal()
        {
            try
            {
                Build();

                // WHICH THING ASKED, at the moment it asks - the fact the
                // Changes window's log line was added to record.
                _log(_question.Name + " window asked.");

                _shell.Window.ShowDialog();
                return _turnOn;
            }
            catch (Exception ex)
            {
                _log("The " + _question.Name + " window failed to open: " + ex);
                return null;
            }
        }

        private void Build()
        {
            var body = new StackPanel();
            body.Children.Add(Question());
            body.Children.Add(Promises());
            body.Children.Add(Footer());

            _shell = HeronWindowStyle.Build(
                _question.Name, CardWidth, body, HeronWindowStyle.WarnColour,
                delegate { Decide(false); },
                _log);
        }

        private UIElement Question()
        {
            var lamp = new Ellipse
            {
                Width = 11,
                Height = 11,
                Fill = new SolidColorBrush(HeronWindowStyle.WarnColour),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 11, 0),
            };

            var headline = new TextBlock
            {
                Text = _question.Headline,
                FontFamily = new FontFamily(HeronWindowStyle.BodyFont),
                FontSize = 19,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(HeronWindowStyle.TextPrimary),
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
            };

            // A Grid, for the reason Promise() gives: in a horizontal
            // StackPanel a long headline is measured against infinite width
            // and leaves the card instead of wrapping.
            var line = new Grid();
            line.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            line.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });
            Grid.SetColumn(lamp, 0);
            Grid.SetColumn(headline, 1);
            line.Children.Add(lamp);
            line.Children.Add(headline);

            var what = HeronWindowStyle.Body(
                _question.What, HeronWindowStyle.TextSecondary, 13);
            what.Margin = new Thickness(0, 9, 0, 0);

            var stack = new StackPanel
            {
                Margin = new Thickness(HeronWindowStyle.Gutter, 20, HeronWindowStyle.Gutter, 0),
            };
            stack.Children.Add(line);
            stack.Children.Add(what);

            // THE TWO AMBER LINES. What cannot be undone, and - only when it
            // is true right now - that Changes is off, so turning this on
            // alone will not let anything through yet.
            foreach (var warning in new[] { _question.Caution, _question.ChangesOff })
            {
                if (string.IsNullOrEmpty(warning)) continue;
                var said = HeronWindowStyle.Body(warning, HeronWindowStyle.WarnColour, 12.5);
                said.Margin = new Thickness(0, 9, 0, 0);
                stack.Children.Add(said);
            }
            return stack;
        }

        private UIElement Promises()
        {
            var list = new StackPanel();
            foreach (var promise in _question.Promises ?? new string[0])
                list.Children.Add(Promise(promise));

            var card = new Border
            {
                Margin = new Thickness(HeronWindowStyle.Gutter, 17, HeronWindowStyle.Gutter, 0),
                Padding = new Thickness(14, 12, 14, 12),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(HeronWindowStyle.InsetColour),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x1E, 0xFF, 0xFF, 0xFF)),
                Child = list,
            };

            var stack = new StackPanel();
            stack.Children.Add(card);

            if (!string.IsNullOrEmpty(_question.Note))
            {
                var note = HeronWindowStyle.Body(
                    _question.Note, HeronWindowStyle.TextMuted, 12);
                note.Margin = new Thickness(HeronWindowStyle.Gutter + 2, 13, HeronWindowStyle.Gutter, 0);
                stack.Children.Add(note);
            }
            return stack;
        }

        private static UIElement Promise(string text)
        {
            var tick = new TextBlock
            {
                Text = "✓",
                FontFamily = new FontFamily(HeronWindowStyle.BodyFont),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(HeronWindowStyle.SafeColour),
                Width = 22,
                VerticalAlignment = VerticalAlignment.Top,
            };

            var words = HeronWindowStyle.Body(text, HeronWindowStyle.TextSecondary, 12.5);

            var row = new Grid { Margin = new Thickness(0, 4, 0, 4) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });

            Grid.SetColumn(tick, 0);
            Grid.SetColumn(words, 1);
            row.Children.Add(tick);
            row.Children.Add(words);
            return row;
        }

        private UIElement Footer()
        {
            var leave = HeronWindowStyle.GhostButton("Leave it off");
            leave.Click += delegate { Decide(false); };

            var turnOn = HeronWindowStyle.PrimaryButton(
                _question.TurnOn, HeronWindowStyle.WarnColour);
            turnOn.Click += delegate { Decide(true); };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            buttons.Children.Add(leave);
            buttons.Children.Add(turnOn);

            return HeronWindowStyle.Footer(buttons);
        }

        private void Decide(bool turnOn)
        {
            _turnOn = turnOn;
            _shell.Close(_log);
        }
    }
}
