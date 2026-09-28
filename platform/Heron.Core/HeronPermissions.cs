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
    ///
    /// PUBLISH AND ADMIN HAVE A SWITCH EACH, SINCE D-106 (2026-09-28).
    ///
    /// Until then this class refused both outright, with a comment saying
    /// they were "not reachable in Phase 0 or Phase 1 at all". That was the
    /// rule, and on 2026-09-28 it refused ADD_PROJECT_PARAMETER (risk ADMIN)
    /// when the owner asked for two project parameters. He decided that both
    /// levels become reachable, and ONLY through a switch he turns on himself
    /// in Revit, exactly as Changes is turned on:
    ///
    ///     admin.enabled   = true    lets an Admin operation through
    ///     publish.enabled = true    lets a Publish operation through
    ///
    /// Both default to false, both are read fresh on every call, and NEITHER
    /// DOES ANYTHING ON ITS OWN: each needs write.enabled as well, because
    /// Changes is the switch that says Heron may touch this model at all, and
    /// the two above it only widen what that means. They are independent of
    /// each other - Admin on says nothing about Publish.
    ///
    /// What they do NOT change is Constitution Article 7: deleting, purging,
    /// workset changes and anything touching a link still need the owner's
    /// explicit yes in the chat for that specific change. No switch here can
    /// say yes on his behalf, and none of these three tries to.
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
        /// Config key that lets an Admin operation through - D-106. Only
        /// while write.enabled is on as well.
        /// </summary>
        public const string AdminEnabledKey = "admin.enabled";

        /// <summary>
        /// Config key that lets a Publish operation through - D-106. Only
        /// while write.enabled is on as well.
        /// </summary>
        public const string PublishEnabledKey = "publish.enabled";

        /// <summary>
        /// What RefusedBy answers for a level no switch opens. None exists
        /// today - Admin is the top of HeronRisk - and this is here so that a
        /// level added above it is refused, by name, until somebody decides
        /// which switch opens it, rather than borrowing one that does not.
        /// </summary>
        public const string NoSwitch = "(no switch)";

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
            return Allows(risk, HeronConfig.Load());
        }

        /// <summary>
        /// The same answer, from a config the caller has already read.
        ///
        /// HERE SO THAT ONE DECISION IS MADE FROM ONE READ. The gate in the
        /// add-in needs the verdict, the words and which switch refused, and
        /// three loads of the file would let the owner's click land between
        /// them - a refusal naming a switch he had just turned on. It is also
        /// what lets tests/Heron.Kernel.TestHost run every combination of the
        /// three switches without touching anybody's real heron.config.
        ///
        /// FAILS CLOSED: no config refuses everything above the ceiling, and
        /// a level with no switch of its own is refused whatever is on.
        /// </summary>
        public static bool Allows(HeronRisk risk, HeronConfig config)
        {
            if (risk <= ReadOnlyCeiling) return true;
            if (config == null) return false;

            // CHANGES FIRST, FOR EVERYTHING ABOVE THE CEILING. Admin and
            // Publish widen what a writable Heron may do; they never make a
            // read-only one writable (D-106).
            if (!config.GetBool(WriteEnabledKey, false)) return false;

            switch (risk)
            {
                case HeronRisk.Modify:
                    return true;
                case HeronRisk.Publish:
                    return config.GetBool(PublishEnabledKey, false);
                case HeronRisk.Admin:
                    return config.GetBool(AdminEnabledKey, false);
                default:
                    // A level above Admin, added later, is refused here until
                    // somebody decides which switch opens it - so adding one
                    // is a deliberate edit to this method and not an accident
                    // of an unhandled case, which is what this line said about
                    // Publish and Admin before D-106.
                    return false;
            }
        }

        /// <summary>
        /// Which switch refused an operation at this level, as its config key
        /// - WriteEnabledKey, AdminEnabledKey or PublishEnabledKey - or NULL
        /// when nothing refused it. NoSwitch for a level no switch opens.
        ///
        /// WHEN TWO ARE OFF, THE LEVEL'S OWN SWITCH IS NAMED. An Admin
        /// operation with Admin off is refused by Admin whatever Changes says,
        /// because Admin is the switch the request was about; Changes is named
        /// only when the level's own switch is on and Changes is what is left.
        /// Explain says both either way.
        /// </summary>
        public static string RefusedBy(HeronRisk risk, HeronConfig config)
        {
            if (Allows(risk, config)) return null;

            switch (risk)
            {
                case HeronRisk.Modify:
                    return WriteEnabledKey;
                case HeronRisk.Publish:
                    return IsOn(config, PublishEnabledKey) ? WriteEnabledKey : PublishEnabledKey;
                case HeronRisk.Admin:
                    return IsOn(config, AdminEnabledKey) ? WriteEnabledKey : AdminEnabledKey;
                default:
                    return NoSwitch;
            }
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
            if (risk <= ReadOnlyCeiling) return null;
            return Explain(risk, HeronConfig.Load(), HeronConfig.FilePath);
        }

        /// <summary>
        /// The same words, from a config the caller has already read, naming
        /// the settings file it was given. Null when the level is allowed.
        ///
        /// THE PATH IS PASSED IN rather than read here because asking
        /// HeronConfig.FilePath creates the settings folder when it is not
        /// there - and a test that creates something in the owner's own data
        /// folder has changed his installation to check it. The add-in passes
        /// the real path; the kernel test host passes a stand-in.
        /// </summary>
        public static string Explain(HeronRisk risk, HeronConfig config, string configPath)
        {
            var refusedBy = RefusedBy(risk, config);
            if (refusedBy == null) return null;

            if (risk == HeronRisk.Modify) return ExplainChanges(configPath);

            if (refusedBy == NoSwitch)
            {
                return "That would need the '" + risk.ToString().ToUpperInvariant() +
                       "' permission level, which no switch in Heron grants, so nothing " +
                       "was sent to Revit.";
            }

            var name = SwitchName(risk);
            var key = risk == HeronRisk.Admin ? AdminEnabledKey : PublishEnabledKey;
            var what = risk == HeronRisk.Admin
                // Plain words for what an ADMIN fragment in this library does.
                // The cards decide which fragment is which; this only says the
                // kind of thing, so it does not go stale when one is added.
                ? "change how the project itself is set up - a project or global " +
                  "parameter, a workset, or a new family file"
                : "send something out of the model or save it - an export, a print, " +
                  "a save, or a sync with central";

            // ITS OWN SWITCH IS ON, AND CHANGES IS WHAT IS LEFT.
            if (refusedBy == WriteEnabledKey)
            {
                return name + " is on, but Changes is off, and " + name + " only works " +
                       "while Changes is on as well - so nothing was sent to Revit. To " +
                       "allow it, turn on Changes in Revit's Heron ribbon (Heron > AI " +
                       "Bridge > Changes), or set " + WriteEnabledKey + " = true in " +
                       configPath + ". It takes effect straight away.";
            }

            // ITS OWN SWITCH IS OFF. Say whether Changes is off too, so one
            // refusal is enough to get both switches right.
            var changesOff = !IsOn(config, WriteEnabledKey);
            var way = changesOff
                ? "turn on both Changes and " + name + " in Revit's Heron ribbon " +
                  "(Heron > AI Bridge), or set " + WriteEnabledKey + " = true and " + key +
                  " = true in " + configPath + ". " + name + " only works while Changes " +
                  "is on as well."
                : "turn on " + name + " in Revit's Heron ribbon (Heron > AI Bridge > " +
                  name + "), or set " + key + " = true in " + configPath + ".";

            return "That would " + what + ". Heron's " + name + " switch is off, so " +
                   "nothing was sent to Revit. " + name + " is off by default and stays off " +
                   "until you turn it on - it is your decision, not Heron's. To allow it, " +
                   way + " It takes effect straight away, and turning " + name + " off " +
                   "again stops it just as quickly.";
        }

        /// <summary>
        /// The refusal for a MODIFY operation with Changes off - the words
        /// this class has always given, unchanged by D-106.
        /// </summary>
        private static string ExplainChanges(string configPath)
        {
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
                   WriteEnabledKey + " = true in " + configPath +
                   ". It takes effect straight away - Allows() reads that file fresh every " +
                   "time, so there is nothing to restart. Setting it back to false stops " +
                   "Heron changing anything again, just as immediately.";
        }

        /// <summary>
        /// The word on the ribbon button for a level's own switch. Admin and
        /// Publish are both the level's name and the button's label, on
        /// purpose: the refusal, the button and the setting all say one word.
        /// </summary>
        private static string SwitchName(HeronRisk risk)
        {
            return risk == HeronRisk.Admin ? "Admin" : "Publish";
        }

        private static bool IsOn(HeronConfig config, string key)
        {
            return config != null && config.GetBool(key, false);
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
        /// Whether the Admin switch is on, read fresh - the question the Admin
        /// button asks, which is the SETTING and not the verdict. Admin can
        /// read ON while nothing at Admin may run, because Changes is off; the
        /// button shows the switch, and the refusal says the rest.
        /// </summary>
        public static bool AdminEnabled()
        {
            return HeronConfig.Load().GetBool(AdminEnabledKey, false);
        }

        /// <summary>Whether the Publish switch is on, read fresh. See AdminEnabled.</summary>
        public static bool PublishEnabled()
        {
            return HeronConfig.Load().GetBool(PublishEnabledKey, false);
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
            return SetSwitch(WriteEnabledKey, enabled);
        }

        /// <summary>
        /// Turns the Admin switch on or off - SetWriteEnabled's rules, for the
        /// same reasons. Only a person pressing the Admin button reaches it.
        /// </summary>
        public static bool SetAdminEnabled(bool enabled)
        {
            return SetSwitch(AdminEnabledKey, enabled);
        }

        /// <summary>
        /// Turns the Publish switch on or off - SetWriteEnabled's rules, for
        /// the same reasons. Only a person pressing the Publish button reaches it.
        /// </summary>
        public static bool SetPublishEnabled(bool enabled)
        {
            return SetSwitch(PublishEnabledKey, enabled);
        }

        /// <summary>
        /// The one place a switch is written. Three setters and one spelling
        /// of true and false, so the file cannot hold "True" from one button
        /// and "on" from another.
        /// </summary>
        private static bool SetSwitch(string key, bool enabled)
        {
            var config = HeronConfig.Load();
            config.Set(key, enabled ? "true" : "false");
            config.Save();
            return enabled;
        }
    }
}
