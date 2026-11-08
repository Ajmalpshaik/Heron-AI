// Heron-Agent:  HERON-USR-SKL-003
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Text;
using Heron.Revit.Addin;

namespace Heron.CommandStore.TestHost
{
    /// <summary>
    /// `parse FILE` prints what the panel reads from one command file, one
    /// field a line, or "null". `quote ARG...` prints the command line the
    /// panel would build from those arguments. tests/test_command_store.py
    /// reads both.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = new UTF8Encoding(false);
            if (args.Length >= 2 && args[0] == "parse")
            {
                var command = HeronCommandStore.Read(args[1]);
                if (command == null) { Console.WriteLine("null"); return 0; }
                Console.WriteLine("name=" + command.Name);
                Console.WriteLine("capability=" + command.Capability);
                Console.WriteLine("changes=" + (command.Changes ? "yes" : "no"));
                Console.WriteLine("description=" + command.Description);
                foreach (var input in command.Inputs)
                    Console.WriteLine("input=" + input.Name + "|" + input.Label + "|" + input.Default);
                return 0;
            }
            if (args.Length >= 1 && args[0] == "quote")
            {
                var line = new StringBuilder();
                for (var i = 1; i < args.Length; i++) HeronCommandStore.Append(line, args[i]);
                Console.WriteLine(line.ToString());
                return 0;
            }
            Console.Error.WriteLine("usage: parse FILE | quote ARG...");
            return 2;
        }
    }
}
