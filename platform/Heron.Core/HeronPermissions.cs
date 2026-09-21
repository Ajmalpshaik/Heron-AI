// Heron-Agent:  HERON-KRN-PRM-003
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;

namespace Heron.Core
{
    /// <summary>
    /// The seven permission levels of docs/12 section 1, in order of how much
    /// they can cost if wrong.
    ///
    /// The ORDER is the point - it is what makes "is this allowed?" a
    /// comparison rather than a list of special cases. The boundary that
    /// matters sits between Execute and Modify: everything at Modify or above
    /// changes something the user cares about and cannot always undo.
    /// </summary>
    public enum HeronRisk
    {
        Read = 0,
        Analyze = 1,
        Suggest = 2,
        Execute = 3,
        Modify = 4,
        Publish = 5,
        Admin = 6,
    }

    /// <summary>
    /// Permission Manager. Decides whether an operation at a given risk level
    /// may run at all, before anything is done and before the user is asked
    /// to approve anything.
    ///
    /// GOLDEN RULE 19 - no text Heron reads may raise Heron's own permission
    /// level. The risk of an operation is DECLARED by the operation, in code,
    /// at the point it is registered. It is never parsed out of a request,
    /// never read from a model, and never inferred from what an element is
    /// called. That is why the level arrives here as an enum from a switch
    /// statement rather than as a string from the wire: a string from the wire
    /// is exactly the thing an instruction hidden in a parameter value could
    /// set, and an enum from a switch is not reachable that way at all.
    ///
    /// WHY WRITING IS OFF BY DEFAULT.
    ///
    /// Heron already has one safety property of this shape: a Revit that was
    /// never connected is invisible, because autoConnect defaults to false.
    /// Nothing reaches a model the user did not offer up.
    ///
    /// The write path deserves the same treatment, and the REASON has changed
    /// while the default has not. This paragraph said the path "has never
    /// compiled, never loaded and never moved anything" and told the reader
    /// to delete it once the path was proven against a real model. It has
    /// been: NEEDS-CHECKING B8, 2026-09-07, Revit 2024 - three ducts moved up
    /// 200 mm, the first time Heron changed a model - and the add-in compiles
    /// on 2020 through 2027 in CI. The sentence outlived the day it was true
    /// by a fortnight, which is the same expiry Explain() below records about
    /// the message a USER reads; this is the one a MAINTAINER reads, and it
    /// was left behind when that one was corrected.
    ///
    /// WHAT HAS NOT CHANGED IS WHY THE DEFAULT IS OFF, and it never expires:
    /// changing somebody's model is their decision. The instruction to delete
    /// this paragraph is withdrawn with it - a paragraph whose removal is
    /// conditional on a measurement is a paragraph that goes stale the day
    /// the measurement lands, which is exactly what happened here. Turning
    /// writing on is a deliberate act by someone who has read it:
    ///
    ///     write.enabled = true      in %APPDATA%\Heron\config\heron.config
    ///
    /// WHAT IS STILL NOT PROVEN IS THE DISTANCE. D3 in NEEDS-CHECKING: move
    /// them, then MEASURE one. Nobody has put a tape on what it did, and
    /// that - not the compile, and not the run - is what would catch a unit
    /// error. FRAGMENT-ISSUES section 5b, the never-compiled sentence's last
    /// copy.
    /// </summary>
    public static class HeronPermissions
    {
        /// <summary>
        /// The highest risk level Heron will run without the user turning
        /// writing on. Everything up to and including Execute changes what is
        /// shown, never what exists.
        /// </summary>
        public const HeronRisk ReadOnlyCeiling = HeronRisk.Execute;

        /// <summary>Config key that lifts the ceiling to Modify.</summary>
        public const string WriteEnabledKey = "write.enabled";

        /// <summary>
        /// Whether an operation at this risk level may proceed.
        ///
        /// Reads the config FRESH each time rather than caching. A user who
        /// turns writing off mid-session has done so for a reason, and a
        /// cached "true" from before they changed their mind is the worst
        /// possible way to discover the value was read once at startup.
        /// </summary>
        public static bool Allows(HeronRisk risk)
        {
            if (risk <= ReadOnlyCeiling) return true;

            // Publish and Admin are not reachable in Phase 0 or Phase 1 at
            // all. They are refused here rather than left to a caller that
            // does not exist yet, so that adding one is a deliberate edit to
            // this method and not an accident of an unhandled case.
            if (risk > HeronRisk.Modify) return false;

            var config = HeronConfig.Load();
            return config.GetBool(WriteEnabledKey, false);
        }

        /// <summary>
        /// Why a refusal happened, in the user's terms and with the way
        /// forward in it.
        ///
        /// A refusal that only says "not permitted" sends the user hunting
        /// through documentation for a setting whose name they do not know.
        /// </summary>
        public static string Explain(HeronRisk risk)
        {
            if (Allows(risk)) return null;

            if (risk > HeronRisk.Modify)
            {
                return "That would need the '" + risk.ToString().ToUpperInvariant() +
                       "' permission level, which Heron does not grant to anything yet.";
            }

            // THE REASON HAD GONE STALE IN THE TEXT A USER READS. It said the
            // write path "has never been run against a real model", which was
            // true when it was written and has not been since. How many
            // fragments at risk MODIFY are PROVEN is derived, never typed
            // here - the library answers it - and a proof under D-30 IS a
            // recorded run against a named real model, so one of them is
            // enough to make that sentence false. The DEFAULT does not change -
            // it is off, and it stays off - only the reason given for it,
            // which is now the one that will not expire.
            return "Heron's ability to change the model is switched off, so nothing was sent " +
                   "to Revit. This is the default and it stays the default: changing " +
                   "somebody's model is their decision to make, not Heron's. To turn it on, " +
                   // THE TAB IS CALLED `Heron` AND THE PANEL `AI Bridge`, and this
                   // sentence said "the Heron AI ribbon" until 2026-09-21. The rename
                   // landed in #213, which corrected the messages on the Python side -
                   // heron_session.py, heron_mcp_server.py and the bridge client all
                   // say "Heron > AI Bridge > Heron" - and left the two in
                   // platform/Heron.Core behind. This one is LIVE: it is what a
                   // modeller reads every time a write is refused, so it was sending
                   // them to a tab that does not exist. FRAGMENT-ISSUES 5b-4.
                   //
                   // The button's own label is `Changes`; whether this sentence should
                   // call it that or keep describing the padlock is the owner's call
                   // and 5b-4 stays open for it. The PATH is not a judgement.
                   "use the padlock button under  Heron > AI Bridge  on the ribbon, or set " +
                   WriteEnabledKey + " = true in " + HeronConfig.FilePath +
                   ". It takes effect straight away - Allows() reads that file fresh every " +
                   "time, so there is nothing to restart. Setting it back to false stops " +
                   "Heron changing anything again, just as immediately.";
        }

        /// <summary>
        /// Whether writing is currently switched on.
        ///
        /// Allows() answers "may THIS risk proceed", which is the question a
        /// caller has. This answers "what does the switch say", which is the
        /// question a user interface has - a button showing the state must
        /// show the setting itself, not the verdict for some particular risk.
        /// Read fresh, for the same reason Allows() is.
        /// </summary>
        public static bool WriteEnabled()
        {
            return HeronConfig.Load().GetBool(WriteEnabledKey, false);
        }

        /// <summary>
        /// Turns writing on or off, and says what it now is.
        ///
        /// HERE RATHER THAN IN THE CALLER, so the key and the spelling of its
        /// value live in one place. A ribbon command writing
        /// `config.Set("write.enabled", "true")` for itself is a second
        /// definition of this setting, and the day the two disagree the
        /// button reports a state the permission gate does not honour.
        ///
        /// HeronConfig.Save is ADMIN-level and documented as only ever
        /// reached from a deliberate human action. That is what this is: a
        /// person pressing a button. Nothing automatic may call it - Heron
        /// must never grant itself permission to write.
        /// </summary>
        public static bool SetWriteEnabled(bool enabled)
        {
            var config = HeronConfig.Load();
            config.Set(WriteEnabledKey, enabled ? "true" : "false");
            config.Save();
            return enabled;
        }
    }
}
