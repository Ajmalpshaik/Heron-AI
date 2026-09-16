// Heron-Agent:  none
// Heron-Step:   5
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Heron.Revit.Addin;

namespace Heron.Banner.TestHost
{
    /// <summary>
    /// Drives the real banner through every state a real job puts it in and
    /// saves each one.
    ///
    /// Begin is called from a BACKGROUND thread and End from the main one,
    /// because that is the pattern RevitDispatcher actually uses - capturing
    /// it any other way would be capturing a different program.
    ///
    /// The window is reached through the banner's OWN dispatcher, because
    /// that is where it lives. Application.Current.Windows cannot see it at
    /// all, which is itself a confirmation that the thread split is real.
    /// </summary>
    internal static class Program
    {
        private static string _outDir;
        private static string _mode;
        private static HeronActivityBanner _banner;
        private static Dispatcher _ui;

        [STAThread]
        private static int Main(string[] args)
        {
            _mode = args.Length > 0 ? args[0].ToLowerInvariant() : "shots";
            _outDir = args.Length > 1 ? args[1] : Path.Combine(Path.GetTempPath(), "heron-banner");

            if (_mode != "shots" && _mode != "freeze")
            {
                Console.Error.WriteLine("Usage: Heron.Banner.TestHost <shots|freeze> [output folder]");
                return 2;
            }

            Directory.CreateDirectory(_outDir);

            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.Startup += OnStartup;
            return app.Run();
        }

        private static async void OnStartup(object sender, StartupEventArgs e)
        {
            try
            {
                if (_mode == "freeze") await FreezeTest.Run(_outDir);
                else await Capture();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Test host failed: " + ex);
                Environment.ExitCode = 1;
            }

            Application.Current.Shutdown();
        }

        private static async Task Capture()
        {
            _banner = new HeronActivityBanner(true, m => Console.WriteLine("[banner] " + m));
            _ui = (Dispatcher)Field("_ui").GetValue(_banner);

            if (_ui == null)
            {
                Console.Error.WriteLine("The banner has no dispatcher - its thread did not start.");
                Environment.ExitCode = 1;
                return;
            }

            Console.WriteLine("banner thread " + _ui.Thread.ManagedThreadId
                + ", host thread " + Thread.CurrentThread.ManagedThreadId);
            Console.WriteLine("writing to " + _outDir);
            Console.WriteLine();

            // A real MEP name, because the question about the model line is
            // whether a long one still reads.
            const string BigName = "TRG-XX-ZZ-M3-M-0001-MEP-Model";

            await Begin("Counting pipes in the active view", false, "Project1");
            Save("1-reading-short-name.png");

            await End(true, "17 pipes", 340, "Project1");
            Save("2-finished.png");

            await Begin("Capping 13 open pipe ends", true, BigName);
            Save("3-changing-long-name.png");

            // -1 is the never-ran signal: a refusal shows no duration at all.
            await End(false, "Writing is switched off", -1, BigName);
            Save("4-stopped.png");

            // The degrade case. The model line must DISAPPEAR and the other
            // two re-centre, not leave a blank row.
            await Begin("Counting pipes in the active view", false, null);
            Save("5-no-name-known.png");

            await End(true, "17 pipes", 340, null);
            Save("6-finished-no-name.png");

            _banner.Shutdown();
            await Task.Delay(400);
        }

        /// <summary>Begin as the dispatcher does it - off the UI thread.</summary>
        private static async Task Begin(string job, bool changes, string model)
        {
            await Task.Run(() => _banner.Begin(job, changes, model));
            await Settle();
        }

        /// <summary>End as the dispatcher does it - from the calling thread.</summary>
        private static async Task End(bool ok, string outcome, long ms, string model)
        {
            _banner.End(ok, outcome, ms, model);
            await Settle();
        }

        /// <summary>Let the banner's own thread finish drawing.</summary>
        private static async Task Settle()
        {
            await Task.Delay(300);
            _ui.Invoke(delegate { }, DispatcherPriority.ApplicationIdle);
            await Task.Delay(120);
        }

        internal static FieldInfo Field(string name)
        {
            return typeof(HeronActivityBanner).GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance);
        }

        /// <summary>
        /// A still of whatever is on the card right now.
        ///
        /// RenderTargetBitmap captures a frozen frame, so an animation will
        /// not LOOK animated here. That is what the freeze mode's numeric
        /// sampling is for; this mode answers what it says and how it is laid
        /// out, not whether it moves.
        /// </summary>
        private static void Save(string name)
        {
            var path = Path.Combine(_outDir, name);

            // Rendered ON the banner's thread: a visual may only be touched
            // by the thread that owns it.
            _ui.Invoke(delegate
            {
                var window = (Window)Field("_window").GetValue(_banner);
                if (window == null) { Console.WriteLine(name + "  ->  NO WINDOW"); return; }

                var card = window.Content as FrameworkElement;
                if (card == null) { Console.WriteLine(name + "  ->  NO CONTENT"); return; }

                // 2x, so 10 px and 11 px text can be read back off the image.
                var bitmap = new RenderTargetBitmap(
                    (int)Math.Ceiling(card.ActualWidth * 2),
                    (int)Math.Ceiling(card.ActualHeight * 2),
                    192, 192, PixelFormats.Pbgra32);
                bitmap.Render(card);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var stream = File.Create(path)) encoder.Save(stream);

                // The identity of what was photographed, so a run can show
                // that every state is ONE window rather than a new one each
                // time - which is what stops a batch strobing.
                var handle = new WindowInteropHelper(window).Handle;
                var chip = (TextBlock)Field("_chipText").GetValue(_banner);

                Console.WriteLine(string.Format("{0,-26} hwnd={1}  window#{2}  chip=\"{3}\"",
                    name, handle, window.GetHashCode(), chip == null ? "?" : chip.Text));
            });
        }
    }
}
