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
    /// The one question Heron asks before it may change anything -
    /// HERON-REVIT-UI-022, and the visible half of D-19.
    ///
    /// WHAT IT REPLACED. A TaskDialog with two command links. It was correct
    /// and it read like a Windows error: one grey block of prose, in which
    /// the sentence that matters - "Heron will be able to move, edit and
    /// create elements" - sat at the same weight as the sentence about
    /// heron.config. Asked for by Ajmal, 2026-09-20, in the same breath as
    /// the Bridge Status window.
    ///
    /// SO THE PROMISES ARE SEPARATED FROM THE GRANT. What the user is being
    /// asked for is one line, large. What stays true afterwards - the
    /// preview, the single undo, that it survives a restart - is three short
    /// rows underneath, each ticked, because those are the things somebody
    /// hesitating actually wants to know and they are impossible to find in a
    /// paragraph.
    ///
    /// AMBER, NOT RED, AND NOT GREEN. Amber is this repository's warning
    /// colour and nothing else. Red is for danger - something has gone wrong,
    /// or is about to be destroyed - and turning on a setting the owner
    /// deliberately reached for is neither. Green would say "good", and
    /// whether Heron may edit a model is not a thing with a good answer.
    ///
    /// NO DEFAULT BUTTON, deliberately, and this is the one place that rule
    /// is worth breaking the house style for. Enter normally runs the main
    /// action; here the main action is granting write permission, and a
    /// confirmation you can clear by leaning on the Enter key is not a
    /// confirmation. Esc and the X both leave it off, so every reflex that
    /// dismisses a window lands on the safe answer.
    ///
    /// IT ASKS ON THE WAY ON AND NEVER ON THE WAY OFF. Making the safe
    /// direction slower is how people learn to click through warnings, and
    /// there is nothing to confirm about becoming read-only. That is D-19's
    /// shape and this file does not change it.
    ///
    /// IT TOUCHES NO REVIT API and decides nothing. It returns what the
    /// person said and the command does the rest, which is what lets it be
    /// compiled into a bare WPF host and looked at without closing Revit.
    /// </summary>
    internal sealed class HeronChangesWindow
    {
        private const double CardWidth = 470;

        private readonly Action<string> _log;
        private HeronWindowStyle.Shell _shell;
        private bool _turnOn;

        private HeronChangesWindow(Action<string> log)
        {
            _log = log ?? delegate { };
        }

        /// <summary>
        /// Asks. TRUE to turn changes on, FALSE to leave them off, and NULL
        /// when the window could not be shown at all.
        ///
        /// Null is not "no". The caller must ask again some other way, because
        /// a permission question that silently answers itself is the one
        /// outcome this must never have - in either direction. See
        /// WriteToggleCommand for the plain fallback.
        /// </summary>
        internal static bool? Ask(Action<string> log)
        {
            return new HeronChangesWindow(log).AskInternal();
        }

        private bool? AskInternal()
        {
            try
            {
                Build();
                _shell.Window.ShowDialog();
                return _turnOn;
            }
            catch (Exception ex)
            {
                _log("The Changes window failed to open: " + ex);
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
                "Changes", CardWidth, body, HeronWindowStyle.WarnColour,
                // The X in the corner is a way out, and every way out that is
                // not the amber button means "no".
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
                Text = "Let Heron change this model?",
                FontFamily = new FontFamily(HeronWindowStyle.BodyFont),
                FontSize = 19,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(HeronWindowStyle.TextPrimary),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var line = new StackPanel { Orientation = Orientation.Horizontal };
            line.Children.Add(lamp);
            line.Children.Add(headline);

            var what = HeronWindowStyle.Body(
                "Heron will be able to move, edit and create elements when you ask it to. "
                + "Until you turn this on it can only read.",
                HeronWindowStyle.TextSecondary, 13);
            what.Margin = new Thickness(0, 9, 0, 0);

            var stack = new StackPanel
            {
                Margin = new Thickness(HeronWindowStyle.Gutter, 20, HeronWindowStyle.Gutter, 0),
            };
            stack.Children.Add(line);
            stack.Children.Add(what);
            return stack;
        }

        /// <summary>
        /// The three things that stay true after you say yes.
        ///
        /// These are not decoration and they are not reassurance for its own
        /// sake - each is a rail that exists in the code, and putting them
        /// where the decision is made is the difference between a person
        /// granting permission and a person guessing. If any of them ever
        /// stops being true, this list is the first thing that must change.
        /// </summary>
        private UIElement Promises()
        {
            var list = new StackPanel();
            list.Children.Add(Promise("You see a preview first, and nothing is kept until you say yes."));
            list.Children.Add(Promise("Every change is one Ctrl+Z."));
            list.Children.Add(Promise("It stays on until you turn it off, including after Revit restarts."));

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

            var note = HeronWindowStyle.Body(
                "The padlock on the ribbon always shows which state you are in, so a session left "
                + "writable never looks like a safe one.",
                HeronWindowStyle.TextMuted, 12);
            note.Margin = new Thickness(HeronWindowStyle.Gutter + 2, 13, HeronWindowStyle.Gutter, 0);

            var stack = new StackPanel();
            stack.Children.Add(card);
            stack.Children.Add(note);
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

            // A Grid, not a horizontal StackPanel: inside a StackPanel the
            // sentence is measured against infinite width, so it never wraps
            // and simply leaves the card.
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
                "Turn changes on", HeronWindowStyle.WarnColour);
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
