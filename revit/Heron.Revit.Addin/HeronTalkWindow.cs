// Heron-Agent:  none
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The box behind Revit's Talk button - step 1 of D-104.
    ///
    /// ONE QUESTION: WHAT DO YOU WANT? The modeller types it, or presses Win+H
    /// and says it - Windows voice typing writes into any text box, so this
    /// one needs no speech code of its own (D-34: turning words into meaning
    /// is the host's job, and turning sound into words is Windows'). Step 2
    /// replaces the typing with press-and-hold; this box stays as the way to
    /// read back what was heard.
    ///
    /// WHAT TRAVELS WITH THE WORDS IS SHOWN BEFORE THEY GO: the model, the
    /// view and what is selected, in one line above the box. A message that
    /// carried a selection the modeller had forgotten about would be answered
    /// about the wrong elements, and the first they would know is the answer.
    ///
    /// A REFUSAL KEEPS THE WORDS. When nothing is listening, or the disk says
    /// no, the sentence saying so appears under the box and the window stays
    /// open with the text still in it - retyping a request because the chat
    /// was not started yet is the kind of cost that makes a tool not worth
    /// using.
    ///
    /// NO SUCCESS MESSAGE. Sending closes the window; the answer arrives in
    /// the chat, and the work arrives in the model.
    ///
    /// IT TOUCHES NO REVIT API and decides nothing: the command reads the
    /// selection before this opens and does the saving when told to, the same
    /// split HeronChangesWindow keeps.
    /// </summary>
    internal sealed class HeronTalkWindow
    {
        private const double CardWidth = 520;

        private readonly string _context;
        private readonly Func<string, string> _send;
        private readonly Action<string> _log;

        private HeronWindowStyle.Shell _shell;
        private TextBox _box;
        private TextBlock _status;
        private bool _sent;

        private HeronTalkWindow(string context, Func<string, string> send, Action<string> log)
        {
            _context = context ?? "";
            _send = send;
            _log = log ?? delegate { };
        }

        /// <summary>
        /// Opens the box. TRUE when a message was sent, FALSE when the
        /// modeller closed it without sending, NULL when it could not be shown
        /// at all - and then nothing was sent either.
        ///
        /// <paramref name="send"/> is handed the words and answers NULL when
        /// they were saved, or the sentence to show when they were not.
        /// </summary>
        internal static bool? Ask(string context, Func<string, string> send, Action<string> log)
        {
            if (send == null) throw new ArgumentNullException("send");
            return new HeronTalkWindow(context, send, log).AskInternal();
        }

        private bool? AskInternal()
        {
            try
            {
                Build();
                _shell.Window.ShowDialog();
                return _sent;
            }
            catch (Exception ex)
            {
                _log("The Talk window failed to open: " + ex);
                return null;
            }
        }

        private void Build()
        {
            var body = new StackPanel();
            body.Children.Add(Context());
            body.Children.Add(Box());
            body.Children.Add(Hint());
            body.Children.Add(Status());
            body.Children.Add(Footer());

            _shell = HeronWindowStyle.Build(
                "Talk", CardWidth, body, HeronWindowStyle.AccentColour,
                delegate { _shell.Close(_log); }, _log);

            // Straight into the box, so the modeller can start typing - or
            // press Win+H and start talking - without reaching for the mouse.
            _shell.Window.Loaded += delegate
            {
                _box.Focus();
                Keyboard.Focus(_box);
            };
        }

        private UIElement Context()
        {
            var label = HeronWindowStyle.Caption("GOES WITH YOUR WORDS");

            var line = HeronWindowStyle.Body(
                _context.Length == 0 ? "No model in front." : _context,
                HeronWindowStyle.TextSecondary, 12.5);
            line.Margin = new Thickness(0, 5, 0, 0);

            var stack = new StackPanel
            {
                // The caption carries its own top margin.
                Margin = new Thickness(HeronWindowStyle.Gutter, 4, HeronWindowStyle.Gutter, 0),
            };
            stack.Children.Add(label);
            stack.Children.Add(line);
            return stack;
        }

        private UIElement Box()
        {
            _box = new TextBox
            {
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MaxLength = HeronTalkMailbox.MaxText,
                MinHeight = 96,
                MaxHeight = 220,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(10, 8, 10, 8),
                FontFamily = new FontFamily(HeronWindowStyle.BodyFont),
                FontSize = 14,
                Foreground = new SolidColorBrush(HeronWindowStyle.TextPrimary),
                CaretBrush = new SolidColorBrush(HeronWindowStyle.TextPrimary),
                Background = new SolidColorBrush(HeronWindowStyle.InsetColour),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x38, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
            };

            // ENTER SENDS, SHIFT+ENTER STARTS A NEW LINE - the convention of
            // every chat box a modeller has used, so it is not taught here.
            _box.PreviewKeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key != Key.Enter) return;
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift) return;
                e.Handled = true;
                Send();
            };

            return new Border
            {
                Margin = new Thickness(HeronWindowStyle.Gutter, 14, HeronWindowStyle.Gutter, 0),
                Child = _box,
            };
        }

        private UIElement Hint()
        {
            var hint = HeronWindowStyle.Body(
                "Press Win+H to speak instead of typing.  Enter sends; Shift+Enter starts a new line.",
                HeronWindowStyle.TextMuted, 12);
            hint.Margin = new Thickness(HeronWindowStyle.Gutter + 2, 9, HeronWindowStyle.Gutter, 0);
            return hint;
        }

        private UIElement Status()
        {
            _status = HeronWindowStyle.Body("", HeronWindowStyle.WarnColour, 12.5);
            _status.Margin = new Thickness(HeronWindowStyle.Gutter + 2, 10, HeronWindowStyle.Gutter, 0);
            _status.Visibility = Visibility.Collapsed;
            return _status;
        }

        private UIElement Footer()
        {
            var cancel = HeronWindowStyle.GhostButton("Cancel");
            cancel.Click += delegate { _shell.Close(_log); };

            var send = HeronWindowStyle.PrimaryButton("Send", HeronWindowStyle.AccentColour);
            send.Click += delegate { Send(); };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
            };
            buttons.Children.Add(cancel);
            buttons.Children.Add(send);

            return HeronWindowStyle.Footer(buttons);
        }

        private void Send()
        {
            var text = _box.Text ?? "";
            if (text.Trim().Length == 0)
            {
                Say("Type or say what you want first. Nothing was sent.");
                return;
            }

            string refused;
            try
            {
                refused = _send(text);
            }
            catch (Exception ex)
            {
                _log("Talk: sending failed: " + ex);
                refused = "Heron could not send that, so nothing was sent. Windows said: " + ex.Message;
            }

            if (refused != null)
            {
                Say(refused);
                return;
            }

            _sent = true;
            _shell.Close(_log);
        }

        private void Say(string sentence)
        {
            _status.Text = sentence;
            _status.Visibility = Visibility.Visible;
            _box.Focus();
        }
    }
}
