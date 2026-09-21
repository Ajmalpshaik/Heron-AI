// Heron-Agent:  HERON-OPS-STP-007
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System.Threading;

namespace Heron.Core
{
    /// <summary>
    /// Emergency Stop. One switch that stops Heron doing anything further to
    /// the model, reachable while something is already going wrong.
    ///
    /// WHAT IT IS FOR. The user is watching Revit, sees Heron start doing
    /// something they did not mean, and needs it to stop NOW - without
    /// finding the chat window, composing a sentence, and waiting for a
    /// model that is mid-operation to come back and read it. The ribbon
    /// button is in front of them and takes one click.
    ///
    /// WHAT IT HONESTLY CANNOT DO. It cannot interrupt a Revit API call that
    /// has already begun. Revit runs the operation on its own thread and does
    /// not offer a way in. So this stops the NEXT thing, and the thing after
    /// that, and it is checked at every point where Heron is about to act
    /// rather than once at the start. An operation already inside its
    /// transaction finishes or rolls back on its own terms.
    ///
    /// Saying that plainly matters. A stop button believed to be an abort
    /// button is worse than no button, because the user stops reaching for
    /// Ctrl+Z - which IS the thing that undoes what already happened.
    ///
    /// IT DOES NOT PERSIST. Restarting Revit clears it. That is deliberate:
    /// the stop exists for a situation happening right now, and a flag left
    /// on disk from a bad afternoon three weeks ago would surface as Heron
    /// mysteriously refusing to work, with no memory of why. Restarting Revit
    /// is a bigger, clearer reset than the button, and the user knows they
    /// did it.
    /// </summary>
    public static class HeronStop
    {
        // int rather than bool: Interlocked has no bool overload, and a plain
        // bool would be written on Revit's UI thread and read on bridge
        // listener threads with nothing ordering the two.
        private static int _stopped;

        /// <summary>Is Heron currently stopped?</summary>
        public static bool IsStopped
        {
            get { return Interlocked.CompareExchange(ref _stopped, 0, 0) != 0; }
        }

        /// <summary>
        /// Stop. Returns true if this call is what changed the state, so the
        /// caller can log and tell the user once rather than on every click.
        /// </summary>
        public static bool Stop()
        {
            return Interlocked.Exchange(ref _stopped, 1) == 0;
        }

        /// <summary>
        /// Resume. Returns true if this call is what changed the state.
        ///
        /// Deliberately a separate act from stopping. Nothing clears this
        /// automatically - not a new request, not a new chat, not the
        /// operation that was refused succeeding on a retry. It was set by a
        /// person and it is cleared by a person.
        /// </summary>
        public static bool Resume()
        {
            return Interlocked.Exchange(ref _stopped, 0) != 0;
        }

        /// <summary>
        /// The refusal, in the user's terms.
        ///
        /// IT USED TO NAME A BUTTON THAT IS NOT THERE. The sentence read
        /// "Press Heron AI &gt; Emergency Stop again to let it work", and that
        /// ribbon button was removed on the owner's instruction on 2026-09-06
        /// (D-46). The same decision names the replacement in its own words -
        /// "the Heron button IS that control. It calls bridge.Stop(), so
        /// nothing can reach Revit at all" - so there was a working control,
        /// and the message sent the reader hunting for one that is not on the
        /// ribbon. Nothing can set this flag today, so nobody has seen it;
        /// the switch is kept for the file-based kill switch docs/21 section
        /// 4 describes, and on the day that is built this is the first thing
        /// a worried modeller reads.
        ///
        /// AND THE OBVIOUS REPLACEMENT IS ALSO WRONG, WHICH IS WHY THIS
        /// NAMES NO BUTTON AT ALL. The register's own repair said to name
        /// the Heron button as D-46 does. D-46 is about STOPPING: that
        /// button calls bridge.Stop(), which disconnects the bridge. It does
        /// not clear this flag. The only thing that calls Resume() is
        /// EmergencyStopCommand (Commands.cs), and that command has no
        /// ribbon entry - so today there is nothing a user can press that
        /// lets Heron work again, and a message that named one would be the
        /// same defect wearing a newer button. Checked by reading every
        /// caller of Stop, Resume and IsStopped rather than assumed.
        ///
        /// WHOEVER BUILDS THE KILL SWITCH OWNS THIS SENTENCE. The way to
        /// clear the stop arrives with the mechanism that can set it, and
        /// this line has to name it on the same day - see docs/21 section 4.
        /// The Ctrl+Z half is unchanged: it was the most useful thing in the
        /// message and it is still true.
        /// FRAGMENT-ISSUES section 5b, row 2.
        /// </summary>
        public const string Message =
            "Heron is stopped. Nothing was sent to Revit. Ctrl+Z in Revit undoes anything " +
            "that already happened, and Heron stays stopped until whoever stopped it lets " +
            "it work again.";
    }
}
