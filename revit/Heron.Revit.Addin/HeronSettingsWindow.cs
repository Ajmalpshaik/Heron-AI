// Heron-Agent:  HERON-REVIT-UI-022
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// One switch as the Settings window shows it - D-150. The words and the
    /// delegates; the window decides how it is drawn, and the command that
    /// opens the window decides what each one does.
    /// </summary>
    internal sealed class HeronSetting
    {
        /// <summary>"Changes", "Admin", "Publish", "Ask to Keep", "Companion".</summary>
        internal string Name { get; set; }

        /// <summary>The one short line under the name: what ON lets happen.</summary>
        internal Func<string> Line { get; set; }

        /// <summary>The setting as it is NOW, read fresh every time the row is drawn.</summary>
        internal Func<bool> IsOn { get; set; }

        /// <summary>
        /// True for the three padlocks, whose ON is the state to notice (Error,
        /// D-149). False for Ask to Keep, whose ON is the safe side (Success),
        /// and for the Companion, whose ON only shows a page (Success too).
        /// </summary>
        internal bool OnIsRisky { get; set; }

        /// <summary>The picture beside the name - the padlock the ribbon used to carry.</summary>
        internal Func<string> Icon { get; set; }

        /// <summary>
        /// Runs the switch's own toggle - the same command, and the same
        /// question, as the ribbon button it replaces. Null when it went
        /// through or was declined; otherwise the sentence to show.
        /// </summary>
        internal Func<string> Toggle { get; set; }
    }

    /// <summary>
    /// Heron's Settings window - D-150, the owner's word of 2026-11-07: the four
    /// switches leave the ribbon and live here, each one's state being its own
    /// button. Changes, Admin, Publish and Ask to Keep - and the Companion's,
    /// which joined them on 2026-11-08 (D-150's amendment), so the ribbon's
    /// Companion is one push button with no arrow.
    ///
    /// KEPT SIMPLE ON PURPOSE - "very simple, understandable": per switch its
    /// padlock picture, its name, ONE short line and the state button. The
    /// detail the ribbon tooltips used to carry lives in the question each
    /// switch asks on the way to its risky side, which is unchanged.
    ///
    /// IT DECIDES NOTHING. Pressing a state runs the switch's own command - the
    /// same question on the way on (or, for Ask to Keep, on the way off), the
    /// same "a window that cannot draw is asked again plainly", the same file -
    /// and then every row is drawn again from the settings read back. One
    /// switch's answer can change another's line (Admin says when Changes is
    /// off), so all four are read again, never just the one pressed.
    ///
    /// IT TOUCHES NO REVIT API. It is shown modally from SettingsCommand, so a
    /// toggle it runs is on Revit's own thread, inside that command, exactly as
    /// a ribbon press was.
    /// </summary>
    internal sealed class HeronSettingsWindow
    {
        private const double CardWidth = 470;
        private const int IconSize = 32;

        private readonly IList<HeronSetting> _settings;
        private readonly IconLoader _icons;
        private readonly Action<string> _log;
        private HeronWindowStyle.Shell _shell;
        private StackPanel _rows;
        private TextBlock _problem;

        private HeronSettingsWindow(IList<HeronSetting> settings, IconLoader icons, Action<string> log)
        {
            _settings = settings;
            _icons = icons;
            _log = log ?? delegate { };
        }

        /// <summary>
        /// Shows the window until it is closed. FALSE when it could not be
        /// shown at all, so the caller can say so some other way.
        /// </summary>
        internal static bool Show(IList<HeronSetting> settings, IconLoader icons, Action<string> log)
        {
            if (settings == null) return false;
            var window = new HeronSettingsWindow(settings, icons, log);
            try
            {
                window.Build();
                window._log("Settings window opened.");
                window._shell.Window.ShowDialog();
                return true;
            }
            catch (Exception ex)
            {
                window._log("The Settings window failed to open: " + ex);
                return false;
            }
        }

        private void Build()
        {
            _rows = new StackPanel();
            Redraw();

            var card = new Border
            {
                Margin = new Thickness(HeronWindowStyle.Gutter, 18, HeronWindowStyle.Gutter, 0),
                Padding = new Thickness(14, 6, 14, 6),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(HeronWindowStyle.InsetColour),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x1E, 0xF7, 0xF5, 0xF2)),
                Child = _rows,
            };

            _problem = HeronWindowStyle.Body("", HeronWindowStyle.DangerColour, 12);
            _problem.Margin = new Thickness(HeronWindowStyle.Gutter + 2, 10, HeronWindowStyle.Gutter, 0);
            _problem.Visibility = Visibility.Collapsed;

            var note = HeronWindowStyle.Body(
                "Admin and Publish work only while Changes is on.",
                HeronWindowStyle.TextMuted, 12);
            note.Margin = new Thickness(HeronWindowStyle.Gutter + 2, 12, HeronWindowStyle.Gutter, 0);

            var body = new StackPanel();
            body.Children.Add(card);
            body.Children.Add(_problem);
            body.Children.Add(note);
            body.Children.Add(Footer());

            _shell = HeronWindowStyle.Build(
                "Settings", CardWidth, body, HeronWindowStyle.AccentColour,
                delegate { _shell.Close(_log); },
                _log);
        }

        private void Redraw()
        {
            _rows.Children.Clear();
            for (var i = 0; i < _settings.Count; i++)
            {
                if (i > 0)
                {
                    _rows.Children.Add(new Border
                    {
                        Height = 1,
                        Background = new SolidColorBrush(Color.FromArgb(0x14, 0xF7, 0xF5, 0xF2)),
                    });
                }
                _rows.Children.Add(Row(_settings[i]));
            }
        }

        private UIElement Row(HeronSetting setting)
        {
            bool on;
            try { on = setting.IsOn(); }
            catch (Exception ex)
            {
                _log("Settings: " + setting.Name + " could not be read: " + ex.Message);
                on = false;
            }

            var picture = new Image
            {
                Width = IconSize,
                Height = IconSize,
                Margin = new Thickness(0, 0, 12, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            BitmapSource icon = null;
            try { icon = _icons == null ? null : _icons.LoadLarge(setting.Icon()); }
            catch (Exception) { /* a row with no picture still works */ }
            if (icon != null) picture.Source = icon;

            var name = new TextBlock
            {
                Text = setting.Name,
                FontFamily = new FontFamily(HeronWindowStyle.BodyFont),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(HeronWindowStyle.TextPrimary),
            };
            var line = HeronWindowStyle.Body(
                SafeLine(setting), HeronWindowStyle.TextSecondary, 12);
            line.Margin = new Thickness(0, 2, 0, 0);

            var words = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            words.Children.Add(name);
            words.Children.Add(line);

            // THE STATE IS THE BUTTON. Lit when on - Error for a padlock,
            // Success for Ask to Keep - and quiet when off.
            var state = on
                ? HeronWindowStyle.PrimaryButton("ON",
                    setting.OnIsRisky ? HeronWindowStyle.DangerColour : HeronWindowStyle.SafeColour)
                : HeronWindowStyle.GhostButton("OFF");
            state.Width = 64;
            state.Margin = new Thickness(12, 0, 0, 0);
            state.VerticalAlignment = VerticalAlignment.Center;
            state.ToolTip = on ? "Click to turn " + setting.Name + " off"
                               : "Click to turn " + setting.Name + " on";
            state.Click += delegate { Press(setting); };

            var row = new Grid { Margin = new Thickness(0, 10, 0, 10) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(picture, 0);
            Grid.SetColumn(words, 1);
            Grid.SetColumn(state, 2);
            row.Children.Add(picture);
            row.Children.Add(words);
            row.Children.Add(state);
            return row;
        }

        private static string SafeLine(HeronSetting setting)
        {
            try { return setting.Line(); }
            catch (Exception) { return ""; }
        }

        private void Press(HeronSetting setting)
        {
            string problem;
            try { problem = setting.Toggle(); }
            catch (Exception ex)
            {
                _log("Settings: " + setting.Name + " toggle threw: " + ex);
                problem = "Could not change " + setting.Name + ", so it is as it was. " + ex.Message;
            }

            _problem.Text = problem ?? "";
            _problem.Visibility = string.IsNullOrEmpty(problem) ? Visibility.Collapsed : Visibility.Visible;

            // Every row again, from the settings read back - one answer can
            // change another switch's line.
            Redraw();
        }

        private UIElement Footer()
        {
            var close = HeronWindowStyle.GhostButton("Close");
            close.Click += delegate { _shell.Close(_log); };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            buttons.Children.Add(close);
            return HeronWindowStyle.Footer(buttons);
        }
    }
}
