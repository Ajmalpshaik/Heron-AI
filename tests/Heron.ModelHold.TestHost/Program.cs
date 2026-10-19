// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Heron.Revit.Addin;

namespace Heron.ModelHold.TestHost
{
    /// <summary>
    /// One model, one chat at a time - D-141 - checked without Revit.
    ///
    /// The owner's design, 2026-10-18: "One chat can access multiple models.
    /// The same model cannot be accessed by multiple chats at the same time.
    /// After one finishes, then the next goes; then the first can come back."
    /// Every rule of it that needs no Revit is run here: who may claim, who is
    /// refused and what they are told, the three quiet minutes, the chat that
    /// says it is done, the ribbon's Free this model, a closing model, and a
    /// request from an older client that names no conversation.
    ///
    /// WHETHER REVIT HONOURS IT IS NOT HERE. Which model a request runs
    /// against, and that a refused request reaches nothing, are
    /// RevitOperations' - read as text by tests/test_model_hold.py, and owed a
    /// real Revit in NEEDS-CHECKING Group HK.
    /// </summary>
    internal static class Program
    {
        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            Console.WriteLine((condition ? "  ok    " : "  FAIL  ") + what);
            if (!condition) Failures.Add(what);
        }

        private static int Main()
        {
            const string chatA = "a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1";
            const string chatB = "b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2";
            const string tower = "c95db604-0abd-4bc5-9ebd-3ece37b3ce52|C:\\Jobs\\Tower.rvt|Tower";
            const string podium = "|C:\\Jobs\\Podium.rvt|Podium";
            var t0 = new DateTime(2026, 10, 18, 9, 0, 0, DateTimeKind.Utc);
            var quiet = HeronModelHold.QuietFor(HeronModelHold.DefaultMinutes);

            Console.WriteLine("1. the owner's three minutes, and the key a request carries");
            Check(HeronModelHold.DefaultMinutes == 3 && quiet == TimeSpan.FromMinutes(3),
                  "the quiet time is three minutes by default");
            Check(HeronModelHold.QuietFor(0) == TimeSpan.FromMinutes(1)
                  && HeronModelHold.QuietFor(-5) == TimeSpan.FromMinutes(1),
                  "and never under one, whatever heron.config says");
            Check(HeronModelHold.MinutesKey == "bridge.modelHoldMinutes",
                  "it is heron.config's bridge.modelHoldMinutes");
            Check(HeronConversation.EnvelopeField == "conversation",
                  "the chat travels as `conversation` in the request envelope");

            Console.WriteLine();
            Console.WriteLine("2. a model is held by the chat whose request ran against it");
            var hold = new HeronModelHold();
            var first = hold.Claim(chatA, tower, "Tower", t0, quiet);
            Check(first.Granted && first.NewlyHeld && first.Message == null,
                  "chat A's first request on Tower runs, and Tower is held for chat A");
            var again = hold.Claim(chatA, tower, "Tower", t0.AddMinutes(1), quiet);
            Check(again.Granted && !again.NewlyHeld,
                  "chat A's next request runs too - it renews the hold, it is not a new one");
            Check(hold.Holder(tower, t0.AddMinutes(1), quiet) == chatA, "Holder names chat A");

            Console.WriteLine();
            Console.WriteLine("3. another chat is refused before anything runs - read or change");
            var refused = hold.Claim(chatB, tower, "Tower", t0.AddMinutes(1), quiet);
            Check(!refused.Granted, "chat B's request for Tower is refused");
            Check(refused.Message != null
                  && refused.Message.StartsWith("Model 'Tower' is in use by another chat. It is free "
                                                + "when that chat says it is done, after 3 quiet minutes, "
                                                + "or with Free this model on the Heron ribbon",
                                                StringComparison.Ordinal),
                  "in the owner's words: in use by another chat, and the three ways it frees");
            Check(refused.Message != null && refused.Message.EndsWith("Nothing was sent to the model.",
                                                                     StringComparison.Ordinal),
                  "and it says nothing was sent to the model");
            Check(refused.Message != null && refused.Message.Contains("about 3 minutes from now"),
                  "and when it frees at the latest - 3 minutes after chat A's last request");
            Check(hold.Holder(tower, t0.AddMinutes(1), quiet) == chatA,
                  "a refused request takes nothing from the holder");
            var later = hold.Claim(chatB, tower, "Tower", t0.AddMinutes(3), quiet);
            Check(!later.Granted && later.Message.Contains("about 1 minute from now"),
                  "two minutes on it says about one minute - one, not ones");

            Console.WriteLine();
            Console.WriteLine("4. one chat may hold several models; another model is another chat's to take");
            Check(hold.Claim(chatA, podium, "Podium", t0.AddMinutes(2), quiet).Granted,
                  "chat A can work on Podium as well as Tower");
            var two = new HeronModelHold();
            two.Claim(chatA, tower, "Tower", t0, quiet);
            Check(two.Claim(chatB, podium, "Podium", t0, quiet).Granted,
                  "chat B on another model in the same Revit is not refused");
            Check(two.Count(t0, quiet) == 2, "two models held, one by each chat");

            Console.WriteLine();
            Console.WriteLine("5. three quiet minutes free it - counted from the END of the request");
            var idle = new HeronModelHold();
            idle.Claim(chatA, tower, "Tower", t0, quiet);
            Check(!idle.Claim(chatB, tower, "Tower", t0.AddMinutes(3), quiet).Granted,
                  "exactly three minutes after chat A's last request it is still held");
            Check(idle.Claim(chatB, tower, "Tower", t0.AddMinutes(3).AddSeconds(1), quiet).Granted,
                  "past three quiet minutes chat B gets it - nobody closed chat A");
            Check(!idle.Claim(chatA, tower, "Tower", t0.AddMinutes(4), quiet).Granted,
                  "and now chat A is the one told it is in use");
            var longJob = new HeronModelHold();
            longJob.Claim(chatA, tower, "Tower", t0, quiet);
            longJob.Touch(chatA, tower, t0.AddMinutes(4));
            Check(!longJob.Claim(chatB, tower, "Tower", t0.AddMinutes(5), quiet).Granted,
                  "a job that ran four minutes is not four quiet ones - Touch at its end restarts them");
            longJob.Touch(chatB, tower, t0.AddMinutes(6));
            Check(longJob.Claim(chatB, tower, "Tower", t0.AddMinutes(7).AddSeconds(1), quiet).Granted,
                  "and another chat's Touch renews nothing of chat A's");
            var clock = new HeronModelHold();
            clock.Claim(chatA, tower, "Tower", t0, quiet);
            Check(!clock.Claim(chatB, tower, "Tower", t0.AddHours(-1), quiet).Granted
                  && clock.Claim(chatB, tower, "Tower", t0.AddHours(-1).AddMinutes(3).AddSeconds(1), quiet).Granted,
                  "a PC clock put back an hour restarts the quiet time - it cannot hold a model for the hour");

            Console.WriteLine();
            Console.WriteLine("6. the chat says it is done - revit_release");
            var done = new HeronModelHold();
            done.Claim(chatA, tower, "Tower", t0, quiet);
            done.Claim(chatA, podium, "Podium", t0, quiet);
            done.Claim(chatB, "|C:\\Jobs\\Other.rvt|Other", "Other", t0, quiet);
            var none = done.Release(chatB, tower, t0, quiet);
            Check(none.Count == 0 && done.Holder(tower, t0, quiet) == chatA,
                  "a chat cannot free another chat's model this way");
            var one = done.Release(chatA, podium, t0, quiet);
            Check(one.Count == 1 && one[0] == "Podium" && done.Holder(podium, t0, quiet) == null
                  && done.Holder(tower, t0, quiet) == chatA,
                  "one model: Podium is freed and Tower is still chat A's");
            Check(done.Claim(chatB, podium, "Podium", t0, quiet).Granted,
                  "so chat B gets Podium at once - after one finishes, the next goes");
            done.Release(chatB, podium, t0, quiet);
            done.Claim(chatA, podium, "Podium", t0.AddMinutes(1), quiet);
            var all = done.Release(chatA, null, t0.AddMinutes(1), quiet);
            Check(all.Count == 2 && all.Contains("Tower") && all.Contains("Podium"),
                  "all: every model chat A holds is freed, and named");
            Check(done.Holder("|C:\\Jobs\\Other.rvt|Other", t0.AddMinutes(1), quiet) == chatB,
                  "and chat B's own model is untouched");
            Check(done.Claim(chatA, tower, "Tower", t0.AddMinutes(1), quiet).Granted,
                  "then the first can come back");
            Check(done.Release(null, null, t0, quiet).Count == 0
                  && done.Release("", tower, t0, quiet).Count == 0,
                  "a request with no conversation frees nothing");
            var stale = new HeronModelHold();
            stale.Claim(chatA, tower, "Tower", t0, quiet);
            Check(stale.Release(chatA, null, t0.AddMinutes(10), quiet).Count == 0,
                  "a hold already past its quiet time is not named as freed now");

            Console.WriteLine();
            Console.WriteLine("7. Free this model on the ribbon, and a closing model - whoever holds it");
            var ribbon = new HeronModelHold();
            ribbon.Claim(chatA, tower, "Tower", t0, quiet);
            Check(ribbon.Free(tower, t0.AddMinutes(1), quiet) == chatA,
                  "Free names the chat it freed the model from");
            Check(ribbon.Claim(chatB, tower, "Tower", t0.AddMinutes(1), quiet).Granted,
                  "and another chat gets it at once - chat A was never closed");
            Check(ribbon.Free(podium, t0, quiet) == null && ribbon.Free(null, t0, quiet) == null,
                  "a model nobody holds frees nothing, and says so with null");
            var lapsed = new HeronModelHold();
            lapsed.Claim(chatA, tower, "Tower", t0, quiet);
            Check(lapsed.Free(tower, t0.AddMinutes(10), quiet) == null,
                  "a hold already past its quiet time is not reported as freed");
            var cleared = new HeronModelHold();
            cleared.Claim(chatA, tower, "Tower", t0, quiet);
            cleared.Claim(chatB, podium, "Podium", t0, quiet);
            cleared.Clear();
            Check(cleared.Count(t0, quiet) == 0, "disconnecting the bridge clears every hold");

            Console.WriteLine();
            Console.WriteLine("8. an older client that sends no conversation");
            var old = new HeronModelHold();
            var free = old.Claim(null, tower, "Tower", t0, quiet);
            Check(free.Granted && !free.NewlyHeld && old.Count(t0, quiet) == 0,
                  "on a free model it runs as before, and holds nothing - nobody to hold it for");
            old.Claim(chatA, tower, "Tower", t0, quiet);
            Check(!old.Claim(null, tower, "Tower", t0, quiet).Granted
                  && !old.Claim("", tower, "Tower", t0, quiet).Granted,
                  "on a model a chat holds it is refused - it cannot be shown to be the holder");
            var oneRequest = HeronConversation.ChatOf(null);
            Check(HeronConversation.IsOneRequest(oneRequest)
                  && oneRequest != HeronConversation.ChatOf(null)
                  && HeronConversation.ChatOf(chatA) == chatA,
                  "ChatOf gives a request with none a chat of its own, new each time, and "
                  + "leaves a real conversation as it is");
            Check(!old.Claim(oneRequest, tower, "Tower", t0, quiet).Granted
                  && old.Claim(oneRequest, podium, "Podium", t0, quiet).Granted
                  && old.Holder(podium, t0, quiet) == null,
                  "a chat of one request holds nothing either");
            Check(HeronConversation.ChatOf(HeronConversation.OneRequestPrefix + "forged") !=
                      HeronConversation.OneRequestPrefix + "forged",
                  "a conversation sent dressed as a one-request key is not taken as one");
            Check(HeronConversation.Short(chatA) == "a1a1a1a1"
                  && HeronConversation.Short(oneRequest) == "(one request)"
                  && HeronConversation.Short(null) == "(none)",
                  "the log names a chat by eight characters, never the whole id");

            Console.WriteLine();
            Console.WriteLine("8a. looking is not claiming - revit_health's count");
            var look = new HeronModelHold();
            Check(look.Check(chatB, tower, "Tower", t0, quiet).Granted && look.Count(t0, quiet) == 0,
                  "a look at a free model runs and holds nothing");
            Check(look.Claim(chatA, tower, "Tower", t0, quiet).Granted,
                  "so chat A can still take it straight after");
            var looked = look.Check(chatB, tower, "Tower", t0.AddMinutes(1), quiet);
            Check(!looked.Granted && looked.Message.StartsWith("Model 'Tower' is in use by another chat",
                                                               StringComparison.Ordinal),
                  "a look at another chat's model is refused like any request - no way round a hold");
            Check(look.Check(chatA, tower, "Tower", t0.AddMinutes(2), quiet).Granted
                  && look.Claim(chatB, tower, "Tower", t0.AddMinutes(3).AddSeconds(1), quiet).Granted,
                  "and the holder looking renews nothing - its hold still ends three minutes after "
                  + "its last real request");
            Check(!look.Check(null, tower, "Tower", t0.AddMinutes(4), quiet).Granted,
                  "an older client's look at a held model is refused too");

            Console.WriteLine();
            Console.WriteLine("9. no model, nothing to hold");
            var bare = new HeronModelHold();
            Check(bare.Claim(chatA, null, null, t0, quiet).Granted
                  && bare.Claim(chatB, "", "", t0, quiet).Granted
                  && bare.Count(t0, quiet) == 0,
                  "a request with no model runs - the operation says what is missing");
            var unnamed = new HeronModelHold();
            unnamed.Claim(chatA, tower, "", t0, quiet);
            Check(unnamed.Claim(chatB, tower, "", t0, quiet).Message.StartsWith("Model '(unnamed)'",
                                                                               StringComparison.Ordinal),
                  "a model with no title is still named in the refusal");

            Console.WriteLine();
            if (Failures.Count > 0)
            {
                Console.WriteLine("FAILED - " + Failures.Count + " of " + _checks + " checks");
                foreach (var failure in Failures) Console.WriteLine("  - " + failure);
                return 1;
            }
            Console.WriteLine("PASSED - " + _checks + " checks");
            return 0;
        }
    }
}
