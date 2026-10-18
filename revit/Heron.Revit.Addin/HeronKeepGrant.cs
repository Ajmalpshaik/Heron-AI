// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Globalization;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// One Keep per job - D-138. The modeller's third answer to the Keep or
    /// Discard box, "Keep, and keep the rest of this job without asking",
    /// remembered: which chat it was given to, on which model, at what time,
    /// and when that chat last sent a change.
    ///
    /// WHY IT EXISTS. The owner, 2026-10-18, of D-126's box: "While I'm doing
    /// another thing I have to click OK, OK, OK, OK. That is not good." A
    /// family is six to ten changes; with the box on every one, a job he
    /// walked away from stopped at the first and waited for him.
    ///
    /// ONLY THE MODELLER'S CLICK GIVES IT - Golden Rule 19 and D-126 item 5.
    /// Give is called from one place, RevitFragment.Run, after that link was
    /// pressed in Revit and the change it asked about was kept. No request
    /// field reaches it. The chat's id it is held by is the bridge's envelope
    /// (heron_bridge_client.RESERVED_KEYS - an operation's own arguments
    /// cannot set it), and it only says WHO was given the grant, never that a
    /// grant is wanted.
    ///
    /// IT ENDS, AND ASKS AGAIN, WHEN:
    ///   * that chat has sent no change for IdleLimit - ten minutes;
    ///   * another chat, or another model, asks - Decide ends it there;
    ///   * the Ask to Keep switch is turned off (End, from the button);
    ///   * its model closes (End, from DocumentClosing);
    ///   * Revit closes - it is held in memory only, never in heron.config.
    ///
    /// IT NEVER COVERS a publish - an export, a save, a sync, which Discard
    /// cannot take back - or a DRAFT's one run (D-119). Decide is told, and
    /// answers AlwaysAsked: the box is shown, and the grant stands for the
    /// change after.
    ///
    /// THIS FILE NAMES NO AUTODESK TYPE, like HeronKeepQuestion: the clock is
    /// passed in, so tests/Heron.KeepQuestion.TestHost runs every rule here on
    /// any machine. Whether Revit honours it is NEEDS-CHECKING Group HG's.
    /// </summary>
    internal sealed class HeronKeepGrant
    {
        /// <summary>
        /// How long the chat may send no change before the grant ends. The
        /// owner's job is a run of changes a minute or two apart; ten quiet
        /// minutes is a job that has ended, or a modeller who should be asked.
        /// </summary>
        public static readonly TimeSpan IdleLimit = TimeSpan.FromMinutes(10);

        /// <summary>No grant is held: ask, as D-126 always did.</summary>
        public const string NoGrant = "none";

        /// <summary>The grant covers this change: keep it without the box.</summary>
        public const string Covered = "covered";

        /// <summary>
        /// The grant's chat and model, but a publish or a DRAFT's one run:
        /// asked, and the grant stands.
        /// </summary>
        public const string AlwaysAsked = "always asked";

        /// <summary>Ended: the chat sent no change for IdleLimit.</summary>
        public const string EndedIdle = "idle";

        /// <summary>Ended: another chat asked.</summary>
        public const string EndedOtherChat = "other chat";

        /// <summary>Ended: a change came for another model.</summary>
        public const string EndedOtherModel = "other model";

        /// <summary>Ended: the Ask to Keep switch was turned off.</summary>
        public const string EndedSwitch = "switch";

        /// <summary>Ended: the grant's model closed.</summary>
        public const string EndedClosed = "closed";

        /// <summary>The box's third link, and its second line.</summary>
        public const string KeepRestLink = "Keep, and keep the rest of this job without asking";

        /// <summary>
        /// The one grant this Revit holds. Read and changed on Revit's own
        /// thread - a job, Idling, a closing model, the ribbon button - and
        /// locked all the same, because a lock costs nothing here and a
        /// second thread later would otherwise cost a wrong answer.
        /// </summary>
        internal static readonly HeronKeepGrant Session = new HeronKeepGrant();

        private readonly object _gate = new object();
        private string _client;
        private string _document;
        private string _grantedAt;
        private DateTime _lastUtc;

        /// <summary>
        /// Can a grant be held for this chat on this model at all? Not for a
        /// request with no chat id or a model with no key: with nothing to
        /// tell "the same chat" from "another one", the next change could not
        /// be shown to be the job the modeller meant. The third link is not
        /// offered then.
        /// </summary>
        public static bool CanHold(string client, string document)
        {
            return !string.IsNullOrEmpty(client) && !string.IsNullOrEmpty(document);
        }

        /// <summary>
        /// Gives the grant - the modeller pressed the third link and the change
        /// was kept. Replaces any grant held before. False, and nothing held,
        /// when CanHold says no.
        /// </summary>
        /// <param name="client">The chat's id, from the bridge's envelope.</param>
        /// <param name="document">The model's key - RevitOperations.DocumentKey.</param>
        /// <param name="nowUtc">Now, for the idle clock.</param>
        /// <param name="nowLocal">Now on the PC's clock, for the HH:MM the
        /// chat and the ribbon are told.</param>
        public bool Give(string client, string document, DateTime nowUtc, DateTime nowLocal)
        {
            lock (_gate)
            {
                if (!CanHold(client, document))
                {
                    Clear();
                    return false;
                }
                _client = client;
                _document = document;
                _grantedAt = nowLocal.ToString("HH:mm", CultureInfo.InvariantCulture);
                _lastUtc = nowUtc;
                return true;
            }
        }

        /// <summary>
        /// Whether a change about to be kept is covered - asked once per change
        /// that would otherwise be asked, by RevitFragment.Run before the box.
        /// Returns NoGrant, Covered, AlwaysAsked, or one of the Ended reasons -
        /// and an Ended answer has ended the grant. Covered and AlwaysAsked
        /// restart the idle clock: the chat sent a change.
        ///
        /// IN THIS ORDER: a clock that ran past the limit, or BACKWARDS, ends it
        /// first - a PC clock put back would otherwise stretch ten minutes into
        /// an hour, and the safe answer to a clock nobody can trust is to ask.
        /// </summary>
        /// <param name="alwaysAsked">A publish, or a DRAFT's one run.</param>
        /// <param name="grantedAt">The HH:MM of the grant this was decided
        /// against - also of one this call ended - or null with none.</param>
        public string Decide(string client, string document, bool alwaysAsked, DateTime nowUtc,
                             out string grantedAt)
        {
            lock (_gate)
            {
                grantedAt = _grantedAt;
                if (_client == null) return NoGrant;

                var idle = nowUtc - _lastUtc;
                if (idle > IdleLimit || idle < TimeSpan.Zero)
                {
                    Clear();
                    return EndedIdle;
                }
                if (!string.Equals(client, _client, StringComparison.Ordinal))
                {
                    Clear();
                    return EndedOtherChat;
                }
                if (!string.Equals(document, _document, StringComparison.Ordinal))
                {
                    Clear();
                    return EndedOtherModel;
                }

                _lastUtc = nowUtc;
                return alwaysAsked ? AlwaysAsked : Covered;
            }
        }

        /// <summary>Did Decide's answer end the grant?</summary>
        public static bool Ends(string decided)
        {
            return decided == EndedIdle || decided == EndedOtherChat || decided == EndedOtherModel;
        }

        /// <summary>
        /// Is a grant held and still inside its idle limit? What the ribbon
        /// shows, read again from Idling so it stops saying so once the ten
        /// minutes are up. It ends nothing; End does.
        /// </summary>
        public bool Active(DateTime nowUtc)
        {
            lock (_gate)
            {
                if (_client == null) return false;
                var idle = nowUtc - _lastUtc;
                return idle <= IdleLimit && idle >= TimeSpan.Zero;
            }
        }

        /// <summary>Is the grant held for this model? For a model that is closing.</summary>
        public bool HeldFor(string document)
        {
            lock (_gate)
            {
                return _client != null && !string.IsNullOrEmpty(document)
                    && string.Equals(document, _document, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Ends the grant. Returns the HH:MM of the one ended, or null when
        /// none was held - so a caller logs an ending only when there was one.
        /// </summary>
        public string End()
        {
            lock (_gate)
            {
                var had = _client == null ? null : _grantedAt;
                Clear();
                return had;
            }
        }

        /// <summary>The HH:MM the grant held now was given at, or null.</summary>
        public string GrantedAt
        {
            get { lock (_gate) { return _client == null ? null : _grantedAt; } }
        }

        private void Clear()
        {
            _client = null;
            _document = null;
            _grantedAt = null;
        }

        /// <summary>The third link's second line.</summary>
        public static string KeepRestNote(string name)
        {
            return "Keep this one as \"Heron: " + Named(name) + "\". This chat's next changes to "
                 + "this model are then kept without this box, until " + Minutes()
                 + " minutes pass with none. Each is still its own undo step. An export, a save "
                 + "or a sync, and a tool that has never been proved, are still asked.";
        }

        /// <summary>
        /// The verdict's first sentence for a change kept under the grant -
        /// said, so a box that did not appear is never read as one answered.
        /// </summary>
        public static string KeptSentence(string grantedAt)
        {
            return "Kept WITHOUT ASKING the modeller, under the grant they gave in Revit at "
                 + Named(grantedAt) + " - \"" + KeepRestLink + "\" - because this change came "
                 + "from the same chat, on the same model, within " + Minutes()
                 + " minutes of its last.";
        }

        /// <summary>WithAnswer's sentence for the third link.</summary>
        public static string ChoseKeepRest()
        {
            return "You were asked in Revit and pressed KEEP, AND KEEP THE REST OF THIS JOB WITHOUT "
                 + "ASKING after seeing the result: this chat's next changes to this model are kept "
                 + "without the box until " + Minutes() + " minutes pass with none. An export, a "
                 + "save or a sync, and a tool that has never been proved, are still asked.";
        }

        /// <summary>
        /// Why the box is up although the modeller gave a grant - added to the
        /// box, so a box they thought they had dismissed for the job explains
        /// itself. Null when no grant was in play.
        /// </summary>
        /// <param name="decided">What Decide answered.</param>
        /// <param name="grantedAt">Decide's grantedAt.</param>
        /// <param name="publish">A publish, rather than a DRAFT's one run -
        /// only read for AlwaysAsked.</param>
        public static string WhyAskedAgain(string decided, string grantedAt, bool publish)
        {
            var given = "At " + Named(grantedAt) + " you chose to keep the rest of a job without asking";
            switch (decided)
            {
                case AlwaysAsked:
                    return given + ", and that still stands for this chat's next change - but "
                         + (publish ? "an export, a save or a sync" : "a tool that has never been proved")
                         + " is always asked.";
                case EndedIdle:
                    return given + ". That has ended: " + Reason(EndedIdle) + ".";
                case EndedOtherChat:
                    return given + ". That has ended: " + Reason(EndedOtherChat) + ".";
                case EndedOtherModel:
                    return given + ". That has ended: " + Reason(EndedOtherModel) + ".";
                default:
                    return null;
            }
        }

        /// <summary>The add-in log's line for a grant that ended.</summary>
        public static string EndedLine(string why, string grantedAt)
        {
            return "Keep the rest of this job: the grant given at " + Named(grantedAt)
                 + " ended - " + Reason(why) + ".";
        }

        /// <summary>Why a grant ended, in the modeller's words.</summary>
        public static string Reason(string why)
        {
            switch (why)
            {
                case EndedIdle: return Minutes() + " minutes passed with no change from that chat";
                case EndedOtherChat: return "this change comes from another chat";
                case EndedOtherModel: return "this change is to another model";
                case EndedSwitch: return "the Ask to Keep switch was turned off";
                case EndedClosed: return "its model closed";
                default: return "(no reason recorded)";
            }
        }

        private static string Minutes()
        {
            return ((int)IdleLimit.TotalMinutes).ToString(CultureInfo.InvariantCulture);
        }

        private static string Named(string text)
        {
            return string.IsNullOrEmpty(text) ? "(unknown)" : text;
        }
    }
}
