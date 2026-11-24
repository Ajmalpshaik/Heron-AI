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
    /// writing on is a deliberate act by the modeller: Changes, in Heron's
    /// Settings window in Revit. It was a line in heron.config until D-166,
    /// and that is what the next paragraph but two is about.
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
    /// in Revit, exactly as Changes is turned on: Admin lets an Admin
    /// operation through, and Publish a Publish one.
    ///
    /// Both are off by default, both are read fresh on every call, and NEITHER
    /// DOES ANYTHING ON ITS OWN: each needs Changes as well, because
    /// Changes is the switch that says Heron may touch this model at all, and
    /// the two above it only widen what that means. They are independent of
    /// each other - Admin on says nothing about Publish.
    ///
    /// What they do NOT change is Constitution Article 7: deleting, purging,
    /// workset changes and anything touching a link still need the owner's
    /// explicit yes in the chat for that specific change. No switch here can
    /// say yes on his behalf, and none of these three tries to.
    ///
    /// THE THREE SWITCHES ARE HELD HERE, IN MEMORY, SINCE D-166 (2026-11-16).
    ///
    /// They were write.enabled, admin.enabled and publish.enabled in
    /// heron.config - in the modeller's own profile, writable by every program
    /// running as the modeller, read fresh on every request, and named by the
    /// refusal below as the way to turn writing on. An AI host with file
    /// access could do exactly that, and Heron's own words told it how
    /// (proposal F45). Now:
    ///
    ///   - each is a field of THIS class, in the Revit process, set only by
    ///     SetWriteEnabled, SetAdminEnabled and SetPublishEnabled - which only
    ///     the Settings window's own commands call, after a person pressed ON
    ///     and answered the question;
    ///   - all three are OFF whenever Revit starts, because a static field
    ///     starts false - the owner's choice, D-166 item 2;
    ///   - each Revit holds its own: Changes on in 2024 is not on in 2027;
    ///   - no request sets one, and no file does. HeronConfig does not declare
    ///     the three keys, so a line naming one is ignored as unknown;
    ///   - no refusal names a file, only Settings.
    ///
    /// WHAT THIS DOES NOT STOP, from D-166: a program that drives Revit's own
    /// screen as the modeller presses the button as a person would, and a
    /// program that writes into Revit's memory on purpose.
    ///
    /// Ask to Keep is NOT one of them. It lets nothing through, its risky
    /// side is OFF - the default - and it stays in heron.config, as D-126 and
    /// D-138 have it.
    /// </summary>
    public static class HeronPermissions
    {
        /// <summary>
        /// The highest risk level Heron will run without the user turning
        /// writing on. Everything up to and including Execute changes what is
        /// shown, never what exists.
        /// </summary>
        public const HeronRisk ReadOnlyCeiling = HeronRisk.Execute;

        /// <summary>
        /// The name of the switch that lifts the ceiling to Modify - Changes.
        ///
        /// A NAME, NO LONGER A SETTINGS KEY, since D-166: RefusedBy answers
        /// with it, and the gate and the run guard turn it into
        /// write_disabled and its sentence. It keeps the spelling it had in
        /// heron.config so that every reader of RefusedBy still matches.
        /// </summary>
        public const string WriteEnabledKey = "write.enabled";

        /// <summary>
        /// The name of the switch that lets an Admin operation through -
        /// D-106. Only while Changes is on as well. A name, as above.
        /// </summary>
        public const string AdminEnabledKey = "admin.enabled";

        /// <summary>
        /// The name of the switch that lets a Publish operation through -
        /// D-106. Only while Changes is on as well. A name, as above.
        /// </summary>
        public const string PublishEnabledKey = "publish.enabled";

        // THE THREE SWITCHES THEMSELVES - D-166. False when Revit starts,
        // because that is what a static bool starts as, and that IS the rule.
        // One lock, so a snapshot is never half of one click and half of the
        // next.
        private static readonly object SwitchLock = new object();
        private static bool _changes;
        private static bool _admin;
        private static bool _publish;

        /// <summary>
        /// THE ADMINISTRATOR'S CEILING - D-166 layer 3. Who may turn each
        /// switch on at all on this PC, as the add-in reads it from
        /// HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Heron, which only an
        /// administrator can write. A switch the ceiling holds off cannot be
        /// turned on in Settings, and is off whatever was pressed before.
        ///
        /// A HOOK, NOT A REGISTRY READ, because this kernel depends on
        /// nothing - the same shape as HeronAudit.ReportLoss. The add-in sets
        /// it at start-up; the kernel test host sets its own. Unset, there is
        /// no ceiling, which is what a PC with no policy key has.
        ///
        /// FAILS CLOSED: a reader that throws, or answers nothing, holds all
        /// three off. Code sets this, never a request and never a file.
        /// </summary>
        public static Func<HeronPolicy> Policy = () => HeronPolicy.NoCeiling;

        /// <summary>The ceiling as it stands now, read fresh - closed when it cannot be read.</summary>
        public static HeronPolicy CurrentPolicy()
        {
            var reader = Policy;
            if (reader == null) return HeronPolicy.NoCeiling;
            try
            {
                return reader() ?? HeronPolicy.Unreadable;
            }
            catch (Exception)
            {
                // A ceiling nobody can read is not a ceiling that is up. The
                // registry's own faults are caught where it is read; anything
                // left is unexpected, and an unexpected answer about who may
                // change the model is answered no.
                return HeronPolicy.Unreadable;
            }
        }

        /// <summary>
        /// Config key for D-126: Revit asks the modeller Keep or Discard before
        /// a change from a chat is kept. It lets nothing through - it only
        /// decides whether a change Changes already allowed is asked about.
        /// Off by default since D-138; the Ask to Keep button turns it on.
        /// </summary>
        public const string AskBeforeKeepingKey = "write.askBeforeKeeping";

        /// <summary>
        /// What RefusedBy answers for a level no switch opens. None exists
        /// today - Admin is the top of HeronRisk - and this is here so that a
        /// level added above it is refused, by name, until somebody decides
        /// which switch opens it, rather than borrowing one that does not.
        /// </summary>
        public const string NoSwitch = "(no switch)";

        /// <summary>
        /// What the three switches say now, as one snapshot - D-166.
        ///
        /// Read fresh each time rather than cached by the caller. A modeller
        /// who turns writing off mid-session has done so for a reason, and a
        /// "true" kept from before they changed their mind is the worst way
        /// to discover it was read once.
        /// </summary>
        public static HeronSwitches Switches()
        {
            var policy = CurrentPolicy();
            lock (SwitchLock)
            {
                // A CEILING THAT COMES DOWN TURNS THE SWITCH OFF FOR GOOD. Held
                // on and then held off by the ceiling, the switch is cleared
                // here - so lifting the ceiling again turns nothing on. Only
                // a person pressing ON in Settings ever does that.
                if (!policy.AllowsChanges) _changes = false;
                if (!policy.AllowsAdmin) _admin = false;
                if (!policy.AllowsPublish) _publish = false;
                return HeronSwitches.Of(_changes, _admin, _publish,
                                        !policy.AllowsChanges, !policy.AllowsAdmin,
                                        !policy.AllowsPublish);
            }
        }

        /// <summary>Whether an operation at this risk level may proceed.</summary>
        public static bool Allows(HeronRisk risk)
        {
            if (risk <= ReadOnlyCeiling) return true;
            return Allows(risk, Switches());
        }

        /// <summary>
        /// The same answer, from a snapshot the caller has already taken.
        ///
        /// HERE SO THAT ONE DECISION IS MADE FROM ONE LOOK. The gate in the
        /// add-in needs the verdict, the words and which switch refused, and
        /// three looks would let the modeller's click land between them - a
        /// refusal naming a switch they had just turned on. It is also what
        /// lets tests/Heron.Kernel.TestHost run every combination of the
        /// three switches without touching the ones this process holds.
        ///
        /// FAILS CLOSED: no snapshot refuses everything above the ceiling,
        /// and a level with no switch of its own is refused whatever is on.
        /// </summary>
        public static bool Allows(HeronRisk risk, HeronSwitches switches)
        {
            if (risk <= ReadOnlyCeiling) return true;
            if (switches == null) return false;

            // CHANGES FIRST, FOR EVERYTHING ABOVE THE CEILING. Admin and
            // Publish widen what a writable Heron may do; they never make a
            // read-only one writable (D-106).
            if (!switches.Changes) return false;

            switch (risk)
            {
                case HeronRisk.Modify:
                    return true;
                case HeronRisk.Publish:
                    return switches.Publish;
                case HeronRisk.Admin:
                    return switches.Admin;
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
        public static string RefusedBy(HeronRisk risk, HeronSwitches switches)
        {
            if (Allows(risk, switches)) return null;

            switch (risk)
            {
                case HeronRisk.Modify:
                    return WriteEnabledKey;
                case HeronRisk.Publish:
                    return switches != null && switches.Publish ? WriteEnabledKey : PublishEnabledKey;
                case HeronRisk.Admin:
                    return switches != null && switches.Admin ? WriteEnabledKey : AdminEnabledKey;
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
            return Explain(risk, Switches());
        }

        /// <summary>
        /// The same words, from a snapshot the caller has already taken. Null
        /// when the level is allowed.
        ///
        /// NO FILE IS NAMED, SINCE D-166. These words said "or set
        /// write.enabled = true in" heron.config's path, and the reader of a
        /// refusal is as often the AI that was refused as the modeller - so
        /// Heron's own refusal was the instruction to turn writing on behind
        /// the modeller's back. They name Settings, and say only the modeller
        /// can do it there.
        /// </summary>
        public static string Explain(HeronRisk risk, HeronSwitches switches)
        {
            var refusedBy = RefusedBy(risk, switches);
            if (refusedBy == null) return null;

            if (risk == HeronRisk.Modify)
            {
                return switches != null && switches.ChangesLocked
                    ? HeldOff("Changes", "change the model")
                    : ExplainChanges();
            }

            if (refusedBy == NoSwitch)
            {
                return "That would need the '" + risk.ToString().ToUpperInvariant() +
                       "' permission level, which no switch in Heron grants, so nothing " +
                       "was sent to Revit.";
            }

            var name = SwitchName(risk);
            var ownLocked = switches != null
                            && (risk == HeronRisk.Admin ? switches.AdminLocked : switches.PublishLocked);
            var what = risk == HeronRisk.Admin
                // Plain words for what an ADMIN fragment in this library does.
                // The cards decide which fragment is which; this only says the
                // kind of thing, so it does not go stale when one is added.
                ? "change how the project itself is set up - a project or global " +
                  "parameter, a workset, or a new family file"
                : "send something out of the model or save it - an export, a print, " +
                  "a save, or a sync with central";

            // HELD OFF BY THE ADMINISTRATOR'S CEILING - D-166 layer 3. Said
            // before anything else, because no press in Settings can help.
            if (refusedBy == WriteEnabledKey && switches != null && switches.ChangesLocked)
                return HeldOff("Changes", what);
            if (refusedBy != WriteEnabledKey && ownLocked)
                return HeldOff(name, what);

            // ITS OWN SWITCH IS ON, AND CHANGES IS WHAT IS LEFT.
            if (refusedBy == WriteEnabledKey)
            {
                return name + " is on, but Changes is off, and " + name + " only works " +
                       "while Changes is on as well - so nothing was sent to Revit. To " +
                       "allow it, the modeller turns on Changes in Heron's Settings in Revit " +
                       "(Heron > AI Bridge > Settings). " + WhereInSettings + " " + OnlyThere +
                       " It takes effect straight away.";
            }

            // ITS OWN SWITCH IS OFF. Say whether Changes is off too, so one
            // refusal is enough to get both switches right.
            var changesOff = switches == null || !switches.Changes;
            var way = changesOff
                ? "the modeller turns on both Changes and " + name + " in Heron's Settings " +
                  "in Revit (Heron > AI Bridge > Settings). " + name + " only works while " +
                  "Changes is on as well."
                : "the modeller turns on " + name + " in Heron's Settings in Revit " +
                  "(Heron > AI Bridge > Settings).";

            return "That would " + what + ". Heron's " + name + " switch is off, so " +
                   "nothing was sent to Revit. " + name + " is off whenever Revit starts and " +
                   "stays off until the modeller turns it on - it is their decision, not " +
                   "Heron's. To allow it, " + way + " " + WhereInSettings + " " + OnlyThere +
                   " It takes effect " +
                   "straight away, and turning " + name + " off again stops it just as quickly.";
        }

        /// <summary>
        /// The refusal for a switch the administrator's ceiling holds off -
        /// D-166 layer 3. Settings cannot help, so it does not send anybody
        /// there; it says who can.
        /// </summary>
        private static string HeldOff(string name, string what)
        {
            return "That would " + what + ". Heron's " + name + " switch is held off on this " +
                   "PC by a Windows policy - one its administrator set, or one Heron could not " +
                   "read - so nothing was sent to Revit. Heron's Settings cannot turn it on while " +
                   "that policy stands; only the PC's administrator can lift it.";
        }

        /// <summary>
        /// What every refusal says about who may turn a switch on - D-166.
        /// Said to the AI as much as to the modeller, because the AI reads it.
        /// </summary>
        private const string OnlyThere =
            "Only the modeller can, there: no file, setting or request turns it on.";

        /// <summary>
        /// WHERE IN THE WINDOW - row 5b-2019. On Revit 2024 the modeller,
        /// told to turn Changes on, answered "cant see in settings", and the
        /// chat had to read the add-in's C# to say where it is. The refusal is
        /// what the chat reads, so the refusal says it. The rows are in the
        /// order SettingsCommand.Settings() draws them.
        /// </summary>
        private const string WhereInSettings =
            "In that window each switch is one row - Changes at the top, then Admin, then Publish - " +
            "with a button beside it that reads OFF: press it, and choose Turn on in the question that " +
            "opens in front of the window.";

        /// <summary>
        /// The refusal for a MODIFY operation with Changes off - the words
        /// this class has always given, with D-166's place to turn it on.
        /// </summary>
        private static string ExplainChanges()
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
                   // Since D-150 (2026-11-07) the switches live in Heron's Settings
                   // window, and the sentence names Changes there - which also settles
                   // 5b-4's "padlock or Changes": the padlock left the ribbon.
                   //
                   // AND SINCE D-166 (2026-11-16) IT NAMES NOTHING ELSE. It went on
                   // "or set write.enabled = true in" heron.config's path - which the
                   // AI reading this refusal could do without the modeller.
                   "the modeller turns on Changes in Heron's Settings in Revit (Heron > AI " +
                   "Bridge > Settings). " + WhereInSettings + " " + OnlyThere +
                   " It takes effect straight away, and " +
                   "it is off again whenever Revit starts. Turning it off stops Heron " +
                   "changing anything again, just as immediately.";
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

        /// <summary>
        /// Whether writing is currently switched on.
        ///
        /// Allows() answers "may THIS risk proceed", which is the question a
        /// caller has. This answers "what does the switch say", which is the
        /// question a user interface has - a button showing the state must
        /// show the switch itself, not the verdict for some particular risk.
        /// </summary>
        public static bool WriteEnabled()
        {
            return Switches().Changes;
        }

        /// <summary>
        /// Whether the Admin switch is on, read fresh - the question the Admin
        /// button asks, which is the SETTING and not the verdict. Admin can
        /// read ON while nothing at Admin may run, because Changes is off; the
        /// button shows the switch, and the refusal says the rest.
        /// </summary>
        public static bool AdminEnabled()
        {
            return Switches().Admin;
        }

        /// <summary>Whether the Publish switch is on, read fresh. See AdminEnabled.</summary>
        public static bool PublishEnabled()
        {
            return Switches().Publish;
        }

        /// <summary>
        /// Whether Revit asks Keep or Discard before keeping a change - D-126.
        /// Read fresh, like every switch here, so the owner's edit to the file
        /// - or his click on the Ask to Keep button, D-138 - counts from the
        /// next change without a restart.
        ///
        /// OFF BY DEFAULT SINCE D-138 - the owner, 2026-10-18: "make the box
        /// off by default". A value nobody can read is the default too, OFF,
        /// as heron_config.truthy reads it: D-126 read it as ASK while the
        /// default was on, and the two readers must agree, or the chat's wait
        /// is shorter than the box it is waiting on. The ribbon button shows
        /// what this answers, so a typo shows as off.
        /// </summary>
        public static bool AsksBeforeKeeping()
        {
            return HeronConfig.Load().GetBool(AskBeforeKeepingKey, false);
        }

        // ASK TO DELETE - D-197. True when Revit starts, because the field is
        // set so here, and that IS the rule: the owner's answer of 2026-11-23,
        // "one switch, boxes on". Held in memory like D-166's three and for
        // the same reason, the other way round: its risky side is OFF, so no
        // file may hold it, or a program writing heron.config could turn
        // Revit's Delete box off. HeronConfig declares no key for it.
        private static bool _asksInRevit = true;

        /// <summary>
        /// Whether Revit shows its own two boxes - D-197, the Ask to Delete
        /// switch in Heron's Settings: D-142's "Delete N elements?" before a
        /// kept change that deletes, and D-119's Keep or Discard before a DRAFT
        /// fragment's one run is kept. ON whenever Revit starts.
        ///
        /// OFF, NEITHER BOX IS SHOWN: a kept change that deletes is kept as it
        /// ran, and a DRAFT's one run is kept with nobody asked. The chat's
        /// yes in words, asked before the call (Constitution Article 7), is
        /// then the only yes. Ask to Keep is a switch of its own and is not
        /// read here.
        /// </summary>
        public static bool AsksInRevit()
        {
            lock (SwitchLock) return _asksInRevit;
        }

        /// <summary>
        /// Turns Ask to Delete on or off, and says what it now is - D-197.
        /// Only the Settings window's own command calls it, after a person
        /// pressed it and, on the way off, answered the question. Nothing on
        /// the wire may (Golden Rule 19), and no file is written.
        /// </summary>
        public static bool SetAsksInRevit(bool asks)
        {
            lock (SwitchLock) _asksInRevit = asks;
            return asks;
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
        /// ONLY A PERSON PRESSING ON IN HERON'S SETTINGS REACHES IT, having
        /// answered the question. Nothing automatic may call it, and nothing
        /// on the wire does - Heron must never grant itself permission to
        /// write. Since D-166 it writes no file: the switch is held here, for
        /// the life of this Revit.
        /// </summary>
        public static bool SetWriteEnabled(bool enabled)
        {
            // Held off by the administrator's ceiling, ON is not taken.
            if (enabled && !CurrentPolicy().AllowsChanges) enabled = false;
            lock (SwitchLock) _changes = enabled;
            return enabled;
        }

        /// <summary>
        /// Turns the Admin switch on or off - SetWriteEnabled's rules, for the
        /// same reasons. Only a person pressing the Admin button reaches it.
        /// </summary>
        public static bool SetAdminEnabled(bool enabled)
        {
            if (enabled && !CurrentPolicy().AllowsAdmin) enabled = false;
            lock (SwitchLock) _admin = enabled;
            return enabled;
        }

        /// <summary>
        /// Turns the Publish switch on or off - SetWriteEnabled's rules, for
        /// the same reasons. Only a person pressing the Publish button reaches it.
        /// </summary>
        public static bool SetPublishEnabled(bool enabled)
        {
            if (enabled && !CurrentPolicy().AllowsPublish) enabled = false;
            lock (SwitchLock) _publish = enabled;
            return enabled;
        }

        /// <summary>
        /// Turns Revit's Keep or Discard question on or off - D-138, the Ask
        /// to Keep button. SetWriteEnabled's rules, for the same reasons: only
        /// a person pressing that button reaches it, and nothing on the wire
        /// may (Golden Rule 19, D-126 item 5).
        ///
        /// IT LETS NOTHING THROUGH. Off, a change Changes already allowed is
        /// kept without the question - still one undo step - and on, it is
        /// asked about again from the next change.
        /// </summary>
        public static bool SetAsksBeforeKeeping(bool asks)
        {
            return SetSwitch(AskBeforeKeepingKey, asks);
        }

        /// <summary>
        /// The one place a switch is written to heron.config - Ask to Keep's,
        /// the only one left there since D-166 - with one spelling of true
        /// and false, so the file cannot hold "True" from one button and "on"
        /// from another.
        /// </summary>
        private static bool SetSwitch(string key, bool enabled)
        {
            var config = HeronConfig.Load();
            config.Set(key, enabled ? "true" : "false");
            config.Save();
            return enabled;
        }
    }

    /// <summary>
    /// What Changes, Admin and Publish say at one moment - D-166.
    ///
    /// Immutable, so a decision made from one cannot change half way through.
    /// HeronPermissions.Switches() gives the live one; Of() builds any other,
    /// which is how a test runs every combination without touching the
    /// switches this process holds.
    /// </summary>
    public sealed class HeronSwitches
    {
        private HeronSwitches(bool changes, bool admin, bool publish,
                              bool changesLocked, bool adminLocked, bool publishLocked)
        {
            Changes = changes;
            Admin = admin;
            Publish = publish;
            ChangesLocked = changesLocked;
            AdminLocked = adminLocked;
            PublishLocked = publishLocked;
        }

        /// <summary>Changes is held off by the administrator's ceiling - D-166 layer 3.</summary>
        public bool ChangesLocked { get; private set; }

        /// <summary>Admin is held off by the administrator's ceiling.</summary>
        public bool AdminLocked { get; private set; }

        /// <summary>Publish is held off by the administrator's ceiling.</summary>
        public bool PublishLocked { get; private set; }

        /// <summary>Changes - write.enabled until D-166. Lets Modify through.</summary>
        public bool Changes { get; private set; }

        /// <summary>Admin - D-106. Lets Admin through, with Changes.</summary>
        public bool Admin { get; private set; }

        /// <summary>Publish - D-106. Lets Publish through, with Changes.</summary>
        public bool Publish { get; private set; }

        /// <summary>A snapshot of these three values.</summary>
        public static HeronSwitches Of(bool changes, bool admin, bool publish)
        {
            return new HeronSwitches(changes, admin, publish, false, false, false);
        }

        /// <summary>A snapshot that also says which switches the ceiling holds off.</summary>
        public static HeronSwitches Of(bool changes, bool admin, bool publish,
                                       bool changesLocked, bool adminLocked, bool publishLocked)
        {
            return new HeronSwitches(changes && !changesLocked, admin && !adminLocked,
                                     publish && !publishLocked,
                                     changesLocked, adminLocked, publishLocked);
        }
    }

    /// <summary>
    /// Which switches may be turned on at all on this PC - D-166 layer 3, the
    /// administrator's ceiling. Read by the add-in from
    /// HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Heron: AllowChanges, AllowAdmin and
    /// AllowPublish, each a DWORD - 0 holds that switch off, 1 or missing lets
    /// the modeller turn it on. No key at all is no ceiling.
    ///
    /// It can only keep a switch OFF. It cannot say a person turned one on;
    /// that is still the Settings window, and only there.
    /// </summary>
    public sealed class HeronPolicy
    {
        private HeronPolicy(bool changes, bool admin, bool publish)
        {
            AllowsChanges = changes;
            AllowsAdmin = admin;
            AllowsPublish = publish;
        }

        /// <summary>The modeller may turn Changes on here.</summary>
        public bool AllowsChanges { get; private set; }

        /// <summary>The modeller may turn Admin on here.</summary>
        public bool AllowsAdmin { get; private set; }

        /// <summary>The modeller may turn Publish on here.</summary>
        public bool AllowsPublish { get; private set; }

        /// <summary>No policy key: nothing is held off.</summary>
        public static readonly HeronPolicy NoCeiling = new HeronPolicy(true, true, true);

        /// <summary>A policy that could not be read: everything is held off - closed.</summary>
        public static readonly HeronPolicy Unreadable = new HeronPolicy(false, false, false);

        /// <summary>A ceiling of these three values.</summary>
        public static HeronPolicy Of(bool allowChanges, bool allowAdmin, bool allowPublish)
        {
            return new HeronPolicy(allowChanges, allowAdmin, allowPublish);
        }
    }
}
