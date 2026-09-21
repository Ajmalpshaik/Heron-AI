// Heron-Agent:  HERON-INS-ORC-001
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Heron.Installer.App
{
    /// <summary>
    /// The installer window - one window, one list, two buttons.
    ///
    /// IT DECIDES NOTHING. Every rule about what a modeller is offered lives
    /// in InstallerScreen, which runs against a fake Revit on a machine with
    /// no Windows. This reads that model and draws it. If a rule ever appears
    /// in this file it has left the only place it could be checked.
    ///
    /// NOT RUN. It is WPF and this repository is developed on Linux, so not
    /// one pixel of it has been drawn. Stage 4's screen recording is owed on
    /// the owner's machine - rows AB1 to AB7 in docs/NEEDS-CHECKING.md.
    /// </summary>
    public sealed class InstallerWindow : Window
    {
        private readonly InstallerScreen _screen;
        private readonly Dictionary<string, CheckBox> _productTicks =
            new Dictionary<string, CheckBox>(StringComparer.Ordinal);
        private readonly Dictionary<string, CheckBox> _releaseTicks =
            new Dictionary<string, CheckBox>(StringComparer.Ordinal);

        private readonly Button _install;
        private readonly TextBlock _report;

        /// <summary>Raised when Install is pressed. The window never installs.</summary>
        public event Action<IReadOnlyList<string>, IReadOnlyList<string>> InstallPressed;

        public InstallerWindow(InstallerScreen screen, string version)
        {
            if (screen == null) throw new ArgumentNullException("screen");
            _screen = screen;

            Title = "Heron Installer" + (string.IsNullOrEmpty(version) ? "" : "   " + version);
            Width = 620;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanMinimize;

            var page = new StackPanel { Margin = new Thickness(18) };

            page.Children.Add(Heading("Revit versions found on this PC"));
            page.Children.Add(ReleaseList());

            page.Children.Add(Heading("Products"));
            page.Children.Add(ProductList());

            page.Children.Add(Heading("Install location"));
            page.Children.Add(Quiet(InstallerScreen.InstallLocation));
            page.Children.Add(Quiet(InstallerScreen.InstallLocationNote));

            // BEFORE Install, never after it fails - Stage 4 item 6.
            page.Children.Add(Notice(screen.CloseRevitFirst));

            if (screen.NothingFound != null)
                page.Children.Add(Notice(screen.NothingFound));

            _report = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 12, 0, 0),
            };
            page.Children.Add(_report);

            _install = new Button
            {
                Content = "Install",
                Width = 110,
                Height = 28,
                Margin = new Thickness(0, 0, 10, 0),
                IsEnabled = screen.AnythingToOffer,
            };
            _install.Click += OnInstall;

            var close = new Button { Content = "Close", Width = 110, Height = 28 };
            close.Click += delegate { Close(); };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 18, 0, 0),
            };
            buttons.Children.Add(_install);
            buttons.Children.Add(close);
            page.Children.Add(buttons);

            Content = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = page,
            };
        }

        /// <summary>
        /// Said while the engine waits for a Revit to be closed.
        ///
        /// A SILENT WAIT IS INDISTINGUISHABLE FROM A HANG, and the one thing
        /// the user has to do - close Revit - is the one thing they will not
        /// think of unless the window says it while they are looking at it.
        /// </summary>
        public void Waiting(string why)
        {
            _report.Text = why;
        }

        /// <summary>Told what happened, once the engine has finished.</summary>
        public void Report(InstallReport report)
        {
            if (report == null) return;

            var lines = new List<string>();
            if (report.Abandoned) lines.Add(report.AbandonedBecause);
            foreach (var result in report.Results) lines.Add(result.Message);
            foreach (var skipped in report.Skipped) lines.Add(skipped.Explanation);

            _report.Text = lines.Count == 0
                ? "Nothing was installed."
                : string.Join(Environment.NewLine, lines.ToArray());

            // PUT THE BUTTON BACK. An install that failed - a bad download,
            // a folder something still had open - is one the user should be
            // able to try again after fixing it. Leaving Install greyed
            // forces them to close the window and start over, and looks like
            // Heron has decided they are finished.
            _install.IsEnabled = _screen.AnythingToOffer;
        }

        // ------------------------------------------------------------ parts
        private UIElement ReleaseList()
        {
            var panel = new WrapPanel { Margin = new Thickness(12, 4, 0, 10) };

            foreach (var release in _screen.Releases)
            {
                var tick = new CheckBox
                {
                    Content = "Revit " + release.Release,
                    IsChecked = release.Chosen,
                    Margin = new Thickness(0, 4, 24, 4),
                    MinWidth = 120,
                };
                _releaseTicks[release.Release] = tick;
                panel.Children.Add(tick);
            }

            if (_screen.Releases.Count == 0)
                panel.Children.Add(Quiet("None."));

            return panel;
        }

        private UIElement ProductList()
        {
            var panel = new StackPanel { Margin = new Thickness(12, 4, 0, 10) };
            var headings = new List<ProductRow>();

            foreach (var row in _screen.Products)
            {
                var line = new Grid { Margin = new Thickness(row.IsPiece ? 26 : 0, 3, 0, 3) };
                line.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                line.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var tick = new CheckBox
                {
                    // A BARE STRING IN A CHECKBOX CLIPS, AND CLIPS SILENTLY.
                    // Found 2026-09-21 the first time this window was ever
                    // drawn - NEEDS-CHECKING AB1. The window is a fixed 620
                    // wide and ResizeMode.CanMinimize, so the AI Bridge row
                    // lost its last word - "The three buttons that exist
                    // today." rendered as "...that exist" - with no ellipsis
                    // to say so, and it still read as a finished sentence.
                    //
                    // NEITHER WAY OUT EXISTED: the window cannot be widened,
                    // and ToolTip below carries WhyNot, not the description.
                    //
                    // A TextBlock that wraps costs one object and NO width
                    // number. Widening to fit today's longest line would clip
                    // the next line that outgrows it, just as quietly - and
                    // R-3 says adding a product is a line in a file, never a
                    // change in here. Every other block in this file already
                    // wraps; this was the one that could not.
                    Content = new TextBlock
                    {
                        Text = row.Description == null
                            ? row.Name
                            : row.Name + "    " + row.Description,
                        TextWrapping = TextWrapping.Wrap,
                    },
                    IsEnabled = row.CanBeTicked,

                    // THE REASON IS ON THE ROW - R-10. A greyed tick box with
                    // no explanation is the same silence as leaving it out.
                    ToolTip = row.WhyNot ?? row.IfYouInstallAgain,
                };
                _productTicks[row.Id] = tick;

                Grid.SetColumn(tick, 0);
                line.Children.Add(tick);

                var state = Quiet(row.State);
                state.HorizontalAlignment = HorizontalAlignment.Right;
                Grid.SetColumn(state, 1);
                line.Children.Add(state);

                panel.Children.Add(line);
                if (row.IsHeading) headings.Add(row);

                // A TOOLTIP IS NOT ENOUGH ON ITS OWN. Somebody who does not
                // hover never sees it, so the sentence is printed as well.
                if (row.WhyNot != null)
                    panel.Children.Add(Small(row.WhyNot, row.IsPiece ? 46 : 20));
                else if (row.IfYouInstallAgain != null)
                    panel.Children.Add(Small(row.IfYouInstallAgain, row.IsPiece ? 46 : 20));
            }

            // AFTER THE WHOLE LIST, not while it is being built. A heading is
            // drawn before its pieces, so wiring it inside the loop above
            // found none of them and quietly did nothing - which is exactly
            // the kind of defect that survives a compile and a screenshot.
            foreach (var heading in headings) Follow(heading, _productTicks[heading.Id]);

            return panel;
        }

        /// <summary>
        /// A tab's tick moves the ticks of its pieces, and theirs move it
        /// back.
        ///
        /// WITHOUT THIS, ticking `Heron` would install the AI Bridge and the
        /// tools while both of their boxes sat empty on screen - which is
        /// correct (InstallerScreen.ToInstall resolves the roll-up) and looks
        /// wrong, and a user who cannot see what they are about to install
        /// has no way to change their mind.
        ///
        /// Unticking ONE piece leaves the other ticked and clears the tab.
        /// That is R-34 on screen: the tools without the AI connector is a
        /// supported install, not a half of one, and the tab box going empty
        /// must not read as "nothing from Heron".
        ///
        /// A GREYED PIECE IS NEVER MOVED. It cannot be installed, and
        /// ToInstall would drop it anyway; ticking it on screen would promise
        /// something that is not going to happen.
        /// </summary>
        private void Follow(ProductRow heading, CheckBox headingTick)
        {
            var pieces = new List<CheckBox>();
            foreach (var id in heading.Installs)
            {
                CheckBox piece;
                if (_productTicks.TryGetValue(id, out piece) && piece.IsEnabled)
                    pieces.Add(piece);
            }
            if (pieces.Count == 0) return;

            var moving = false;

            RoutedEventHandler fromHeading = delegate
            {
                if (moving) return;
                moving = true;
                foreach (var piece in pieces) piece.IsChecked = headingTick.IsChecked;
                moving = false;
            };
            headingTick.Checked += fromHeading;
            headingTick.Unchecked += fromHeading;

            RoutedEventHandler fromPiece = delegate
            {
                if (moving) return;
                moving = true;
                var all = true;
                foreach (var piece in pieces) if (piece.IsChecked != true) all = false;
                headingTick.IsChecked = all;
                moving = false;
            };
            foreach (var piece in pieces)
            {
                piece.Checked += fromPiece;
                piece.Unchecked += fromPiece;
            }
        }

        private void OnInstall(object sender, RoutedEventArgs e)
        {
            var ticked = new List<string>();
            foreach (var pair in _productTicks)
                if (pair.Value.IsChecked == true) ticked.Add(pair.Key);

            foreach (var release in _screen.Releases)
            {
                CheckBox tick;
                if (_releaseTicks.TryGetValue(release.Release, out tick))
                    release.Chosen = tick.IsChecked == true;
            }

            // RESOLVED BY THE SCREEN MODEL, not here: a heading's tick becomes
            // its pieces and a greyed row installs nothing.
            var products = _screen.ToInstall(ticked);
            var releases = _screen.ChosenReleases();

            if (products.Count == 0 || releases.Count == 0)
            {
                _report.Text = products.Count == 0
                    ? "Nothing is ticked, so there is nothing to install."
                    : "No Revit version is ticked, so there is nowhere to install to.";
                return;
            }

            _install.IsEnabled = false;
            var handler = InstallPressed;
            if (handler != null) handler(products, releases);
        }

        // ------------------------------------------------------------ small
        private static TextBlock Heading(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 10, 0, 2),
            };
        }

        private static TextBlock Quiet(string text)
        {
            return new TextBlock
            {
                Text = text ?? "",
                Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                Margin = new Thickness(12, 1, 0, 1),
                TextWrapping = TextWrapping.Wrap,
            };
        }

        private static TextBlock Small(string text, double indent)
        {
            var block = Quiet(text);
            block.FontSize = 11;
            block.Margin = new Thickness(indent, 0, 0, 6);
            return block;
        }

        private static TextBlock Notice(string text)
        {
            return new TextBlock
            {
                Text = text ?? "",
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 14, 0, 0),
            };
        }
    }
}
