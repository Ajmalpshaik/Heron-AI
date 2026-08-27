// Heron-Agent:  none
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
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

            var identity = new BridgeIdentity(revitVersion, "0.1.0-testhost");
            using (var bridge = new BridgeServer(identity, Console.WriteLine))
            {
                bridge.Start();

                Console.WriteLine();
                Console.WriteLine("  Pretending to be Revit " + revitVersion);
                Console.WriteLine("  Pipe       " + identity.PipeName);
                Console.WriteLine("  Process    " + identity.ProcessId);
                Console.WriteLine("  Announced  " + identity.DiscoveryFilePath);
                Console.WriteLine();
                Console.WriteLine("  Now run:   python python\\heron_bridge_client.py ping");
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
