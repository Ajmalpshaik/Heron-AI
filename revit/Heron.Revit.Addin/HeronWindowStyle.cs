// Heron-Agent:  HERON-REVIT-UI-022
// Heron-Step:   5
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// What every Heron window is made of - HERON-REVIT-UI-022.
    ///
    /// WHY THIS FILE EXISTS. There are two Heron windows now (Bridge Status,
    /// and the one that asks before letting Heron change a model) and there
    /// will be more. The moment the second one was written, every colour,
    /// every font size and all three button templates existed in two places -
    /// and a palette in two places is a palette that will disagree, quietly,
    /// on whichever one somebody forgets. Same argument as HeronPaths: one
    /// place per fact.
    ///
    /// THE COLOURS ARE HeronActivityBanner'S, TO THE BYTE. The banner came
    /// first and is the only other thing Heron draws inside Revit; two Heron
    /// surfaces using two different darks read as two products. What the
    /// accents MEAN is the house rule rather than the banner's: blue is
    /// active and primary, amber is a warning and nothing else, red is danger
    /// and nothing else, grey is the quiet default.
    ///
    /// NOTHING HERE TOUCHES THE REVIT API, deliberately, and that is what
    /// lets every window built on it be compiled into a bare WPF host and
    /// LOOKED AT without closing Revit. The one thing that sounds like Revit
    /// - taking Revit's window as the owner - goes through
    /// Process.MainWindowHandle, which is Windows, not Autodesk.
    /// </summary>
    internal static class HeronWindowStyle
    {
        internal static readonly Color CardColour = Color.FromRgb(0x23, 0x26, 0x29);
        internal static readonly Color HeaderColour = Color.FromRgb(0x1A, 0x1D, 0x20);
        internal static readonly Color InsetColour = Color.FromRgb(0x1B, 0x1F, 0x22);
        internal static readonly Color TextPrimary = Color.FromRgb(0xF2, 0xF5, 0xF7);
        internal static readonly Color TextSecondary = Color.FromRgb(0x9A, 0xA5, 0xAD);
        internal static readonly Color TextMuted = Color.FromRgb(0x6F, 0x7A, 0x82);
        internal static readonly Color AccentColour = Color.FromRgb(0x2F, 0x9B, 0xE3);
        internal static readonly Color WarnColour = Color.FromRgb(0xF0, 0xA3, 0x2C);
        internal static readonly Color DangerColour = Color.FromRgb(0xE5, 0x53, 0x4B);
        internal static readonly Color QuietColour = Color.FromRgb(0x7E, 0x8A, 0x93);

        /// <summary>
        /// For a rail that HOLDS - the preview, the single undo. Green means
        /// "this still protects you", never "this is a good idea".
        /// </summary>
        internal static readonly Color SafeColour = Color.FromRgb(0x4C, 0xC3, 0x8A);

        internal const double Gutter = 22;

        internal const string BodyFont = "Segoe UI";

        /// <summary>
        /// Consolas for pipe names, process ids and paths. A proportional font
        /// makes "heron.2024.11908" and "l1llO0" genuinely ambiguous, and
        /// those are exactly the strings somebody reads out to somebody else
        /// when something has gone wrong.
        /// </summary>
        internal const string MonoFont = "Consolas";

        /// <summary>
        /// A finished Heron window: the card, its chrome, and the coloured
        /// strip along its top edge, which is the one part readable from far
        /// enough away to be taken in while walking back to the desk.
        ///
        /// The caller supplies the body and keeps hold of the strip, because
        /// what colour it should be is the caller's business and can change
        /// while the window is open.
        /// </summary>
        internal sealed class Shell
        {
            internal Window Window { get; private set; }
            internal Border AccentStrip { get; private set; }

            internal Shell(Window window, Border strip)
            {
                Window = window;
                AccentStrip = strip;
            }

            internal void Close(Action<string> log)
            {
                try { Window.Close(); }
                catch (Exception ex)
                {
                    if (log != null) log("A Heron window would not close: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Builds the window around <paramref name="body"/>.
        ///
        /// <paramref name="subtitle"/> is the grey half of the title - "Bridge
        /// Status", "Changes". The bold half is always "Heron", because a
        /// person looking at a window needs to know which tool put it there
        /// before they need to know what it is about.
        /// </summary>
        internal static Shell Build(
            string subtitle, double cardWidth, UIElement body,
            Color accent, Action onClose, Action<string> log)
        {
            var strip = new Border
            {
                Height = 3,
                CornerRadius = new CornerRadius(14, 14, 0, 0),
                Background = new SolidColorBrush(accent),
            };

            var window = new Window
            {
                Width = cardWidth + 24,          // room for the shadow to fall
                SizeToContent = SizeToContent.Height,
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

            var stack = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(strip, Dock.Top);
            stack.Children.Add(strip);

            var content = new StackPanel();
            content.Children.Add(TitleBar(window, subtitle, onClose));
            content.Children.Add(body);
            stack.Children.Add(content);

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

            window.Content = new Border { Padding = new Thickness(12), Child = card };

            // Owned by Revit, so it centres on the Revit being used rather
            // than on the primary screen, and cannot be left behind when the
            // user alt-tabs away. Process.MainWindowHandle rather than
            // anything from the Revit API, because this file deliberately
            // references none of it.
            try
            {
                var host = Process.GetCurrentProcess().MainWindowHandle;
                if (host != IntPtr.Zero)
                    new WindowInteropHelper(window).Owner = host;
                else
                    window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            catch (Exception ex)
            {
                // An unowned window still works. It just centres on the wrong
                // screen, which is worth a log line and nothing more.
                if (log != null)
                    log("A Heron window could not take Revit as its owner: " + ex.Message);
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            // Esc closes. With WindowStyle.None there is no system menu and no
            // title bar of Revit's own, so the key every dialog in Windows
            // answers to has to be wired by hand or the window has exactly one
            // way out.
            window.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.Key != Key.Escape) return;
                if (onClose != null) onClose(); else window.Close();
            };

            return new Shell(window, strip);
        }

        private static UIElement TitleBar(Window window, string subtitle, Action onClose)
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
            title.Inlines.Add(new Run("     " + subtitle)
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
            close.Click += delegate
            {
                if (onClose != null) onClose(); else window.Close();
            };

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
                try { if (e.ButtonState == MouseButtonState.Pressed) window.DragMove(); }
                catch (InvalidOperationException) { /* released mid-drag; harmless */ }
            };

            return host;
        }

        /// <summary>The strip along the bottom that the action buttons sit in.</summary>
        internal static Border Footer(UIElement content)
        {
            return new Border
            {
                Margin = new Thickness(0, 20, 0, 0),
                Padding = new Thickness(Gutter - 10, 13, Gutter, 13),
                Background = new SolidColorBrush(HeaderColour),
                CornerRadius = new CornerRadius(0, 0, 13, 13),
                Child = content,
            };
        }

        internal static TextBlock Caption(string text)
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

        internal static TextBlock Body(string text, Color colour, double size)
        {
            return new TextBlock
            {
                Text = text,
                FontFamily = new FontFamily(BodyFont),
                FontSize = size,
                Foreground = new SolidColorBrush(colour),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = size + 7,
            };
        }

        /// <summary>A path or an identifier, in its own inset box.</summary>
        internal static UIElement PathLine(string path)
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

        internal static Button PrimaryButton(string label, Color fill)
        {
            return new Button
            {
                Content = label,
                FontFamily = new FontFamily(BodyFont),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,

                // Dark text on a lit button, not white. Both the blue and the
                // amber are light enough that white on them fails to separate
                // at 13px, and the amber is the one that matters - it is the
                // button that grants Heron permission to edit.
                Foreground = new SolidColorBrush(Color.FromRgb(0x0B, 0x14, 0x1A)),
                Padding = new Thickness(18, 9, 18, 10),
                Margin = new Thickness(9, 0, 0, 0),
                Cursor = Cursors.Hand,
                Template = SolidButtonTemplate(fill),
            };
        }

        internal static Button GhostButton(string label)
        {
            return new Button
            {
                Content = label,
                FontFamily = new FontFamily(BodyFont),
                FontSize = 13,
                Foreground = new SolidColorBrush(TextSecondary),
                Padding = new Thickness(16, 9, 16, 10),
                Cursor = Cursors.Hand,
                Template = OutlineButtonTemplate(),
            };
        }

        internal static Button FlatButton(string label, double fontSize)
        {
            return new Button
            {
                Content = label,
                Foreground = new SolidColorBrush(TextSecondary),
                FontFamily = new FontFamily(BodyFont),
                FontSize = fontSize,
                Padding = new Thickness(10, 6, 10, 7),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                Template = FlatButtonTemplate(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF), 8),
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

        internal static ControlTemplate SolidButtonTemplate(Color fill)
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

        internal static ControlTemplate OutlineButtonTemplate()
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

        internal static ControlTemplate FlatButtonTemplate(Color hoverColour, double radius)
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
