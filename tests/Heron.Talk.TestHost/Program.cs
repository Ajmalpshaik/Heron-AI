// Heron-Agent:  none
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using Heron.Core;
using Heron.Revit.Addin;

namespace Heron.Talk.TestHost
{
    /// <summary>
    /// Drives the Talk mailbox one step at a time, for
    /// tests/test_talk_contract.py - which reads each step back with the
    /// Python that the chat uses. The checks live on the Python side,
    /// because the claim being tested is that the TWO halves agree.
    ///
    ///     root                          the folder HeronPaths calls Talk
    ///     listening  --root R           is a chat listening, as the button sees it
    ///     send       --root R --text T  save a message with the fixed selection
    ///                [--empty]          ... with nothing selected
    ///                [--truncated]      ... with more selected than was saved
    ///     describe                      the line the Talk window shows
    ///     forget     --root R           what Revit does to its folder on closing
    ///
    /// Every answer is one line of key=value, so the suite parses nothing
    /// clever.
    /// </summary>
    internal static class Program
    {
        /// <summary>A process id no real Revit in the test will have.</summary>
        internal const int Pid = 424242;

        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("usage: root | listening | send | describe | forget");
                return 2;
            }

            var root = Option(args, "--root") ?? HeronPaths.Talk;
            var mailbox = new HeronTalkMailbox(root, Pid, "2024");

            switch (args[0])
            {
                case "root":
                    Console.WriteLine("root=" + HeronPaths.Talk);
                    return 0;

                case "listening":
                    Console.WriteLine("listening=" + (mailbox.SomeoneListening(DateTime.UtcNow) ? "true" : "false"));
                    return 0;

                case "send":
                    var selection = Has(args, "--empty") ? new TalkSelection
                    {
                        Document = Fixed().Document,
                        DocumentPath = Fixed().DocumentPath,
                        View = Fixed().View,
                    } : Fixed();
                    if (Has(args, "--truncated")) selection.Total += 2;
                    var number = mailbox.Send(Option(args, "--text") ?? "", selection, DateTime.UtcNow);
                    Console.WriteLine("message=" + number);
                    Console.WriteLine("folder=" + mailbox.Folder);
                    return 0;

                case "describe":
                    Console.WriteLine("describe=" + HeronTalkMailbox.Describe(Fixed()));
                    Console.WriteLine("empty=" + HeronTalkMailbox.Describe(new TalkSelection
                    {
                        Document = "Project1",
                        View = "Level 1",
                    }));
                    return 0;

                case "forget":
                    HeronTalkMailbox.Forget(root, Pid);
                    Console.WriteLine("exists=" + (Directory.Exists(mailbox.Folder) ? "true" : "false"));
                    return 0;
            }

            Console.WriteLine("unknown step: " + args[0]);
            return 2;
        }

        /// <summary>
        /// The selection every send carries. Chosen to break a careless
        /// writer: a quote and a backslash in the model's name, a name that
        /// is not ASCII, an element with no category, and one element that
        /// could not be read.
        /// </summary>
        private static TalkSelection Fixed()
        {
            var selection = new TalkSelection
            {
                Document = "Tower \"A\" \\ MEP",
                DocumentPath = "C:\\Projects\\Tower \"A\"\\MEP.rvt",
                View = "Level 1 - Mech",
            };
            selection.Elements.AddRange(new List<TalkElement>
            {
                new TalkElement { Id = "1491217", UniqueId = "u-1", Category = "Ducts", Name = "Rectangular Duct" },
                new TalkElement { Id = "1491220", UniqueId = "u-2", Category = "Ducts", Name = "Rectangular Duct" },
                new TalkElement { Id = "1491223", UniqueId = "u-3", Category = "Ducts", Name = "Round Duct" },
                new TalkElement { Id = "1491300", UniqueId = "u-4", Category = "Duct Fittings", Name = "Elbow \u00D8200 \u2013 90\u00B0" },
                new TalkElement { Id = "77", UniqueId = "u-5", Category = "", Name = "" },
            });
            selection.Unreadable = 1;
            selection.Total = selection.Elements.Count + selection.Unreadable;
            return selection;
        }

        private static string Option(string[] args, string name)
        {
            for (var i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        private static bool Has(string[] args, string name)
        {
            return Array.IndexOf(args, name) >= 0;
        }
    }
}
