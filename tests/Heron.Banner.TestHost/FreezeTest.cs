// Heron-Agent:  none
// Heron-Step:   5
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Heron.Revit.Addin;

namespace Heron.Banner.TestHost
{
    /// <summary>
    /// Reproduces Revit's actual topology and then freezes it.
    ///
    /// A main window is created on the MAIN thread so that
    /// Process.MainWindowHandle resolves to it and the banner takes it as its
    /// OWNER, exactly as it takes Revit's - a cross-thread owner, which is the
    /// part of this that could go wrong. That thread is then blocked the way
    /// Execute blocks Revit's, and the banner is interrogated FROM ANOTHER
    /// THREAD through its own dispatcher.
    ///
    /// That last detail is the whole test. If the banner still lived on the
    /// blocked thread, every one of those calls would hang until the freeze
    /// was over, and the timeout below would report it.
    ///
    /// WHAT A PASS LOOKS LIKE: sweep changed 5 of 5, time changed 5 of 5, and
    /// the thread gone after Shutdown. What it looked like before the banner
    /// had a thread of its own, on 2026-09-17: zero timer ticks across 3000 ms
    /// and a sweep that had not moved one pixel, 357.6 px before and after.
    /// </summary>
    internal static class FreezeTest
    {
        private const int BlockMilliseconds = 3000;
        private const int Samples = 6;

        public static async Task Run(string outDir)
        {
            // Stand in for Revit's main window, so the ownership under test is
            // a real cross-thread one rather than an approximation.
            var host = new Window
            {
                Title = "Stand-in host",
                Width = 900,
                Height = 600,
                Background = Brushes.DimGray,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
            };
            host.Show();
            await Task.Delay(400);

            Console.WriteLine("host window  " + Process.GetCurrentProcess().MainWindowHandle
                + " on thread " + Thread.CurrentThread.ManagedThreadId);

            var banner = new HeronActivityBanner(true, m => Console.WriteLine("[banner] " + m));
            var ui = (Dispatcher)Program.Field("_ui").GetValue(banner);

            if (ui == null)
            {
                Console.Error.WriteLine("The banner has no dispatcher - its thread did not start.");
                Environment.ExitCode = 1;
                return;
            }

            var ownThread = ui.Thread.ManagedThreadId != Thread.CurrentThread.ManagedThreadId;
            Console.WriteLine("banner       thread " + ui.Thread.ManagedThreadId
                + (ownThread ? "   (its own - good)" : "   (SAME AS HOST - the point of this is gone)"));

            await Task.Run(() => banner.Begin(
                "Capping 13 open pipe ends", true, "TRG-XX-ZZ-M3-M-0001-MEP-Model"));
            await Task.Delay(600);

            var sweep = (FrameworkElement)Program.Field("_sweep").GetValue(banner);
            var detail = (TextBlock)Program.Field("_detail").GetValue(banner);
            if (sweep == null || detail == null)
            {
                Console.Error.WriteLine("The banner never built its window.");
                Environment.ExitCode = 1;
                return;
            }

            var lines = new List<string>();
            var positions = new List<double>();

            var sampler = Task.Run(async delegate
            {
                for (var i = 0; i < Samples; i++)
                {
                    await Task.Delay(BlockMilliseconds / (Samples + 1));
                    try
                    {
                        ui.Invoke(delegate
                        {
                            positions.Add(Canvas.GetLeft(sweep));
                            lines.Add(detail.Text);
                        },
                        DispatcherPriority.Send, CancellationToken.None,
                        TimeSpan.FromMilliseconds(800));
                    }
                    catch (TimeoutException)
                    {
                        // The banner's thread did not answer inside 800 ms,
                        // which on a blocked host means it is not really its
                        // own thread.
                        positions.Add(double.NaN);
                        lines.Add("<banner thread did not answer>");
                    }
                }
            });

            // THE FREEZE. This is Execute: the host's own thread, blocked.
            var clock = Stopwatch.StartNew();
            Thread.Sleep(BlockMilliseconds);
            clock.Stop();

            await sampler;

            Console.WriteLine();
            Console.WriteLine("--- host thread BLOCKED for " + clock.ElapsedMilliseconds + " ms ---");
            for (var i = 0; i < lines.Count; i++)
            {
                Console.WriteLine(string.Format("  sample {0}: sweep={1,8}   line=\"{2}\"",
                    i + 1,
                    double.IsNaN(positions[i]) ? "n/a" : positions[i].ToString("0.0"),
                    lines[i]));
            }

            var moved = Changes(positions);
            var ticked = Changes(lines);

            Console.WriteLine();
            Console.WriteLine("  sweep changed between samples : " + moved + " of " + (Samples - 1));
            Console.WriteLine("  time  changed between samples : " + ticked + " of " + (Samples - 1));

            Save(banner, Path.Combine(outDir, "during-freeze.png"), ui);

            banner.End(true, "13 pipes capped", clock.ElapsedMilliseconds, "TRG-XX-ZZ-M3-M-0001-MEP-Model");
            await Task.Delay(500);
            Save(banner, Path.Combine(outDir, "after-freeze.png"), ui);

            banner.Shutdown();
            await Task.Delay(400);
            Console.WriteLine("  banner thread still alive      : " + ui.Thread.IsAlive
                + "   (false is the pass; it must not outlive Revit)");

            var passed = ownThread && moved == Samples - 1 && ticked == Samples - 1;
            Console.WriteLine();
            Console.WriteLine(passed
                ? "PASS - the banner kept drawing while its owner could not."
                : "FAIL - see the samples above.");

            if (!passed) Environment.ExitCode = 1;
            host.Close();
        }

        private static int Changes<T>(List<T> values)
        {
            var n = 0;
            for (var i = 1; i < values.Count; i++)
            {
                if (values[i] is double)
                {
                    var a = (double)(object)values[i];
                    var b = (double)(object)values[i - 1];
                    if (Math.Abs(a - b) > 0.5) n++;
                }
                else if (!Equals(values[i], values[i - 1])) n++;
            }
            return n;
        }

        private static void Save(HeronActivityBanner banner, string path, Dispatcher ui)
        {
            ui.Invoke(delegate
            {
                var window = (Window)Program.Field("_window").GetValue(banner);
                if (window == null) { Console.WriteLine("  no window to save"); return; }

                var card = (FrameworkElement)window.Content;
                var bitmap = new RenderTargetBitmap(
                    (int)Math.Ceiling(card.ActualWidth * 2),
                    (int)Math.Ceiling(card.ActualHeight * 2),
                    192, 192, PixelFormats.Pbgra32);
                bitmap.Render(card);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using (var stream = File.Create(path)) encoder.Save(stream);
                Console.WriteLine("  saved " + Path.GetFileName(path));
            });
        }
    }
}
