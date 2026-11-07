// Heron-Agent:  HERON-REVIT-UI-022
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// What one switch asks before it goes to its risky side - Changes, Admin
    /// and Publish on the way on (D-19, D-106), Ask to Keep on the way off
    /// (D-138). The words and nothing else; HeronSwitchWindow decides how they
    /// are drawn, and the toggle commands decide what they say.
    ///
    /// SHORT, by the owner's word of 2026-11-07: "no one will read this much,
    /// make it simple and powerful". One question, one line, and only the ticks
    /// and warnings a person deciding needs.
    /// </summary>
    internal sealed class HeronSwitchQuestion
    {
        /// <summary>The switch's own word - "Admin", "Publish". Also the window's subtitle.</summary>
        internal string Name { get; set; }

        /// <summary>The picture beside the question - the switch's own padlock, file name in Resources.</summary>
        internal string Icon { get; set; }

        /// <summary>The one large line: the question, short - "Turn Admin on?".</summary>
        internal string Headline { get; set; }

        /// <summary>One line under it: what Heron will be able to do.</summary>
        internal string What { get; set; }

        /// <summary>
        /// The ticked lines - few and short. EACH MUST BE TRUE IN THE CODE: a
        /// promise put where the decision is made is a rail, and if one stops
        /// being true this list is the first thing that has to change.
        /// </summary>
        internal string[] Promises { get; set; }

        /// <summary>
        /// What this switch does that Ctrl+Z cannot take back, with a warning
        /// sign. Said on the way in because "every change is one Ctrl+Z" does
        /// not cover it.
        /// </summary>
        internal string Caution { get; set; }

        /// <summary>Null, or one warning line when Changes is off right now.</summary>
        internal string ChangesOff { get; set; }

        /// <summary>
        /// A muted line under the rest. Kept for the plain fallback's sake;
        /// the short window does not draw it.
        /// </summary>
        internal string Note { get; set; }

        /// <summary>The amber button - "Turn on".</summary>
        internal string TurnOn { get; set; }

        /// <summary>
        /// The quiet button, the safe answer. Null says "Leave off", which is
        /// what Changes, Admin and Publish want; Ask to Keep (D-138) asks on the
        /// way OFF, and its safe answer is "Keep asking".
        /// </summary>
        internal string Decline { get; set; }
    }

    /// <summary>
    /// The question a switch asks - HeronChangesWindow, and Admin, Publish and
    /// Ask to Keep through their commands.
    ///
    /// THE SHORT FORM, 2026-11-07: the switch's padlock large on the left, the
    /// question beside it, one line, then a tick for each promise and a warning
    /// sign for what Ctrl+Z cannot take back. Symbols carry what paragraphs
    /// used to.
    ///
    /// Whatever made the long form right is kept:
    ///
    ///   * AMBER, not red and not green - turning on a switch the owner
    ///     reached for deliberately is neither danger nor good news.
    ///   * NO DEFAULT BUTTON. The main action grants a permission, and a
    ///     confirmation that leaning on Enter can clear is not one.
    ///   * Esc and the X both mean no, so every reflex that dismisses a
    ///     window lands on the safe answer.
    ///   * It asks only on the way to the risky side - on for a padlock, off
    ///     for Ask to Keep - and its quiet button says so (Decline).
    ///
    /// IT TOUCHES NO REVIT API and decides nothing. It returns what the person
    /// said, and the command does the rest.
    /// </summary>
    internal sealed class HeronSwitchWindow
    {
        private const double CardWidth = 440;
        private const int IconSize = 56;

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
        /// Asks. TRUE to go ahead, FALSE to leave it, and NULL when the window
        /// could not be shown at all - which is not "no", and the caller asks
        /// again some other way (AskPlainly).
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

                // WHICH THING ASKED, at the moment it asks.
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
            body.Children.Add(Lines());
            body.Children.Add(Footer());

            _shell = HeronWindowStyle.Build(
                _question.Name, CardWidth, body, HeronWindowStyle.WarnColour,
                delegate { Decide(false); },
                _log);
        }

        private UIElement Question()
        {
            var picture = new Image
            {
                Width = IconSize,
                Height = IconSize,
                Margin = new Thickness(0, 0, 16, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            try
            {
                var icon = new IconLoader(Assembly.GetExecutingAssembly().Location)
                    .LoadSized(_question.Icon, IconSize * 2);
                if (icon != null) picture.Source = icon;
                else picture.Visibility = Visibility.Collapsed;
            }
            catch (Exception) { picture.Visibility = Visibility.Collapsed; }

            var headline = new TextBlock
            {
                Text = _question.Headline,
                FontFamily = new FontFamily(HeronWindowStyle.BodyFont),
                FontSize = 20,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(HeronWindowStyle.TextPrimary),
                TextWrapping = TextWrapping.Wrap,
            };
            var what = HeronWindowStyle.Body(_question.What, HeronWindowStyle.TextSecondary, 13);
            what.Margin = new Thickness(0, 5, 0, 0);

            var words = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            words.Children.Add(headline);
            words.Children.Add(what);

            // A Grid, not a horizontal StackPanel: in a StackPanel a long line
            // is measured against infinite width and leaves the card instead
            // of wrapping.
            var row = new Grid
            {
                Margin = new Thickness(HeronWindowStyle.Gutter, 22, HeronWindowStyle.Gutter, 0),
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(picture, 0);
            Grid.SetColumn(words, 1);
            row.Children.Add(picture);
            row.Children.Add(words);
            return row;
        }

        /// <summary>A tick for each promise, a warning sign for each caution.</summary>
        private UIElement Lines()
        {
            var list = new StackPanel
            {
                Margin = new Thickness(HeronWindowStyle.Gutter, 16, HeronWindowStyle.Gutter, 0),
            };
            foreach (var promise in _question.Promises ?? new string[0])
                list.Children.Add(Line("✓", promise, HeronWindowStyle.SafeColour, HeronWindowStyle.TextSecondary));
            foreach (var warning in new[] { _question.Caution, _question.ChangesOff })
            {
                if (string.IsNullOrEmpty(warning)) continue;
                list.Children.Add(Line("⚠", warning, HeronWindowStyle.WarnColour, HeronWindowStyle.WarnColour));
            }
            return list;
        }

        private static UIElement Line(string symbol, string text, Color symbolColour, Color textColour)
        {
            var mark = new TextBlock
            {
                Text = symbol,
                FontFamily = new FontFamily(HeronWindowStyle.BodyFont),
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(symbolColour),
                Width = 24,
                VerticalAlignment = VerticalAlignment.Top,
            };
            var words = HeronWindowStyle.Body(text, textColour, 13);

            var row = new Grid { Margin = new Thickness(0, 3, 0, 3) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(mark, 0);
            Grid.SetColumn(words, 1);
            row.Children.Add(mark);
            row.Children.Add(words);
            return row;
        }

        private UIElement Footer()
        {
            var leave = HeronWindowStyle.GhostButton(_question.Decline ?? "Leave off");
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
