// Heron-Agent:  none
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.IO;
using System.Threading;

namespace Heron.Bridge.TestHost
{
    /// <summary>
    /// Pretends to be Revit, so the bridge can be proven without it.
    ///
    /// Everything below the Revit API - the pipe, the per-process naming, the
    /// discovery file, the framing, ping - is exercised here. If this works
    /// and the real add-in does not, the fault is in the add-in, not the
    /// bridge. That is the point of keeping Heron.Bridge Revit-free.
    ///
    ///     dotnet run --project tests/Heron.Bridge.TestHost -- 2024
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var revitVersion = args.Length > 0 ? args[0] : "2024";
            var seconds = 0;
            if (args.Length > 1) int.TryParse(args[1], out seconds);

            // "cycle" exercises the ribbon toggle without Revit: connect,
            // disconnect, connect again. Stop() clears the listener threads and
            // removes the discovery file, so a second Start() has to rebuild
            // both - the path a user takes every time they press the button
            // twice, and the one most likely to be left broken.
            var cycle = args.Length > 2 &&
                        string.Equals(args[2], "cycle", StringComparison.OrdinalIgnoreCase);

            var identity = new BridgeIdentity(revitVersion, "0.1.0-testhost");
            using (var bridge = new BridgeServer(identity, Console.WriteLine))
            {
                // "test_wait" stands in for Revit working on a request: it
                // answers after `ms` milliseconds, so the suite can connect a
                // newer chat while an answer is still owed - the case that lost
                // a chat's answer about walls already built (FRAGMENT-ISSUES
                // section 5b, row 458). Every other operation gets what the
                // bridge says with no handler at all.
                bridge.RequestHandler = request =>
                {
                    if (Json.ReadString(request, "op") != "test_wait")
                        return Json.Error("unknown_op",
                            "No handler for that here. This host answers ping, info and test_wait.");
                    int ms;
                    int.TryParse(Json.ReadString(request, "ms"), out ms);
                    Thread.Sleep(Math.Max(0, Math.Min(ms, 10000)));
                    return Json.Ok(Json.Num("waitedMs", ms));
                };

                if (cycle)
                {
                    bridge.Start();
                    Console.WriteLine("  cycle: started   running=" + bridge.IsRunning +
                                      "  announced=" + File.Exists(identity.DiscoveryFilePath));
                    bridge.Stop();
                    Console.WriteLine("  cycle: stopped   running=" + bridge.IsRunning +
                                      "  announced=" + File.Exists(identity.DiscoveryFilePath));
                }

                bridge.Start();
                if (cycle)
                {
                    Console.WriteLine("  cycle: restarted running=" + bridge.IsRunning +
                                      "  announced=" + File.Exists(identity.DiscoveryFilePath));
                }

                Console.WriteLine();
                Console.WriteLine("  Pretending to be Revit " + revitVersion);
                Console.WriteLine("  Pipe       " + identity.PipeName);
                Console.WriteLine("  Token      " + identity.Token);
                Console.WriteLine("  Process    " + identity.ProcessId);
                Console.WriteLine("  Announced  " + identity.DiscoveryFilePath);
                Console.WriteLine();
                Console.WriteLine("  Now run:   python mcp\\client\\heron_bridge_client.py ping");
                Console.WriteLine();

                if (seconds > 0)
                {
                    Console.WriteLine("  Exiting in " + seconds + "s.");
                    Thread.Sleep(seconds * 1000);
                }
                else
                {
                    Console.WriteLine("  Ctrl+C to stop.");
                    var stop = new ManualResetEvent(false);
                    Console.CancelKeyPress += delegate (object s, ConsoleCancelEventArgs e)
                    {
                        e.Cancel = true;
                        stop.Set();
                    };
                    stop.WaitOne();
                }

                Console.WriteLine("Stopping.");
            }

            return 0;
        }
    }
}
