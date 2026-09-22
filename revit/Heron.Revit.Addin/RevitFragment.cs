// Heron-Agent:  HERON-REVIT-CMP-021
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;

using Heron.Core;
using Autodesk.Revit.UI;
using Heron.Bridge;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Runs a fragment's C# against the open model - D-28's in-process Roslyn
    /// executor, and the thing 343 DRAFT fragments have been waiting for.
    ///
    /// TWO PATHS THROUGH ONE METHOD, and only one of them can write.
    ///
    ///   run_fragment_read   opens NO transaction. Revit refuses every model
    ///                       change made outside one, so a fragment run this
    ///                       way CANNOT alter the model - the enforcement is
    ///                       Revit's, not a check of ours that could be
    ///                       forgotten.
    ///   run_fragment_write  opens ONE TransactionGroup, so the whole job is
    ///                       one undo, and inside it one Transaction for each
    ///                       setup step that writes and one for the fragment.
    ///
    /// THIS PARAGRAPH SAID THE WHOLE FILE WAS READ ONLY until 2026-09-23 -
    /// "Nothing here opens a transaction", and a write path that "belongs in
    /// its own file beside RevitWrite.cs" - for as long as the write path had
    /// been built into Run below. A reviewer trusting the summary would not
    /// have looked for the transactions, and they were the half with no
    /// failure handling.
    ///
    /// EVERY TRANSACTION THE WRITE PATH OPENS ANSWERS REVIT'S FAILURES ITSELF,
    /// from 2026-09-23 - see Discipline. A warning is dismissed and reported;
    /// anything else rolls the whole job back, quoted in Revit's words; and
    /// Revit's own resolution, which for an error can be to DELETE the
    /// elements it names, is never applied. RevitWrite, the move path, has had
    /// that discipline from the start and this path had none. The rule itself
    /// is in HeronFailureNote, proved without Revit by
    /// tests/Heron.FailureNote.TestHost.
    ///
    /// WHAT A FRAGMENT FINDS IN SCOPE IS HeronFragmentGlobals, and it is a
    /// public TOP-LEVEL type for a reason found by running this rather than by
    /// reading it: a script compiles into its OWN assembly, so a globals type
    /// nested inside this internal class was unreachable and every fragment
    /// failed on the line naming `doc`. Read that file before moving it back.
    ///
    /// THE IMPORTS ARE A CONTRACT WITH tools/check-fragments-compile.py.
    /// That gate compiles every fragment against a fixed set of namespaces and
    /// says so in its own comment: a namespace declared there and not here
    /// compiles green and fails at the PC. HeronFragmentImports holds the one
    /// list and tests/test_fragment_imports.py proves the two agree.
    /// </summary>
    internal static class RevitFragment
    {
        /// <summary>
        /// Compiled scripts, keyed by the source itself.
        ///
        /// Compiling is the slow half by a wide margin, and proving a fragment
        /// means running it more than once. Keyed by source rather than by
        /// name because an edited fragment with the same name is a different
        /// program, and serving the old one from a cache would be the worst
        /// kind of wrong answer: right yesterday.
        /// </summary>
        private static readonly Dictionary<string, Script<object>> Compiled =
            new Dictionary<string, Script<object>>(StringComparer.Ordinal);

        public static string Run(UIApplication app, string request)
        {
            return Run(app, request, false);
        }

        /// <summary>
        /// The same executor, optionally inside a transaction so a fragment at
        /// risk: MODIFY can run at all.
        ///
        /// WHY THIS IS ONE METHOD AND NOT TWO. Everything before the script
        /// runs - choosing the document, binding the contract, compiling - is
        /// identical, and 130 write fragments were unrunnable precisely because
        /// that work had no second entry point. Two copies of it would drift,
        /// and the half that drifts is the half nobody exercises.
        ///
        /// The write is gated before it reaches here: `run_fragment_write` is
        /// declared at Modify in the tool registry, and RevitOperations refuses
        /// on risk BEFORE routing. This method never decides its own risk -
        /// Golden Rule 19.
        /// </summary>
        public static string Run(UIApplication app, string request, bool writing)
        {
            var source = Json.ReadString(request, "source");
            var name = Json.ReadString(request, "name") ?? "(unnamed)";

            if (string.IsNullOrEmpty(source))
            {
                return Json.Error("no_source",
                    "No fragment source was sent. This operation runs the C# it is given; " +
                    "it does not read the fragment library itself, which lives on the " +
                    "client's disk and not in Revit.");
            }

            // WHICH MODEL. Named, or the one in front.
            //
            // THE ACTIVE DOCUMENT IS A CHOICE, NOT A LIMIT. Revit is perfectly
            // able to read a document that is open and not in front - it is
            // how TRANSFER_VIEWS_BETWEEN_DOCUMENTS works, and it is what makes
            // "read that project, write into this one" possible at all.
            // Defaulting to the active one is only right when nobody said
            // otherwise, and the first proving run showed the cost of assuming
            // it: a run intended for one model came back reading another,
            // because that window was open but not ACTIVE.
            var wanted = Json.ReadString(request, "document");

            // AND ITS PATH, WHICH IS THE HALF THAT TELLS TWO OPEN MODELS APART
            // WHEN THEY SHARE A NAME. A title is a display name: two Revit
            // sessions really did have a document called Project1 open in each,
            // which is the case DocumentPin was written around. A saved model's
            // path is unique, so it is tried FIRST and the title is the
            // fallback an unsaved model deserves.
            //
            // Absent on every caller that does not send one - the proving
            // client, and any chat that has not pinned a model yet - so this
            // changes nothing for them.
            var wantedPath = Json.ReadString(request, "documentPath");

            Document target = null;
            var openTitles = new List<string>();
            var titleMatches = 0;

            foreach (Document candidate in app.Application.Documents)
            {
                if (candidate == null) continue;

                // A LINKED document is skipped, and that is deliberate rather
                // than an oversight. Application.Documents contains the links
                // too, so on this very model six of them would appear here as
                // if they were projects to choose. A link is reached through
                // its RevitLinkInstance - SELECT_FROM_LINK - because two links
                // can share a title, and a link's geometry is in its own
                // coordinates until the instance transform is applied.
                if (candidate.IsLinked) continue;

                openTitles.Add(candidate.Title);

                // THE PATH DECIDES WHEN THERE IS ONE, and it decides alone: a
                // saved model's path is unique among what is open, so a match
                // here needs no tie-break and cannot be ambiguous.
                if (!string.IsNullOrEmpty(wantedPath))
                {
                    var here = "";
                    try { here = candidate.PathName; } catch { }
                    if (!string.IsNullOrEmpty(here)
                        && string.Equals(here, wantedPath, StringComparison.OrdinalIgnoreCase))
                    {
                        target = candidate;
                    }
                    continue;
                }

                if (!string.IsNullOrEmpty(wanted)
                    && string.Equals(candidate.Title, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    target = candidate;
                    titleMatches++;
                }
            }

            // TWO OPEN MODELS WITH THE SAME NAME IS A REFUSAL, NOT A TIE-BREAK.
            //
            // The loop above used to let the LAST match win, silently. That is
            // the worst available answer on a path that is about to commit a
            // transaction: it is indistinguishable from a correct one, and
            // which document it picks depends on the order Revit happens to
            // hand its documents back. Two models called Project1 is the exact
            // case DocumentPin was written around, so it is not hypothetical.
            if (titleMatches > 1)
            {
                return Json.Error("ambiguous_document",
                    titleMatches + " open models are called \"" + wanted + "\", so naming one "
                    + "cannot say which was meant and NOTHING has been run. Close the one you "
                    + "do not want, or save them so they can be told apart by their file paths.");
            }

            if (target == null
                && (!string.IsNullOrEmpty(wanted) || !string.IsNullOrEmpty(wantedPath)))
            {
                // Say which one was looked for. When a path was sent, the path
                // is what failed to match - naming the title instead would send
                // a reader looking at a model that IS open under that name.
                var looked = string.IsNullOrEmpty(wantedPath)
                    ? "No open model called \"" + wanted + "\""
                    : "No open model saved at \"" + wantedPath + "\"";
                return Json.Error("no_such_document",
                    looked + ". Open: "
                    + (openTitles.Count == 0 ? "(none)" : string.Join(", ", openTitles))
                    + ". A model that is not open cannot be read, and Heron will not open one - "
                    + "opening a project is a decision with a lock and a load time behind it.");
            }

            UIDocument uidoc = null;

            if (target == null)
            {
                uidoc = app.ActiveUIDocument;
                if (uidoc == null || uidoc.Document == null)
                {
                    return Json.Error("no_document",
                        "No model is open in Revit, so there is nothing for a fragment to read.");
                }
                target = uidoc.Document;
            }
            else
            {
                // A UIDocument for a document that is open but not in front.
                // It is built rather than left null so that a fragment needing
                // `uidoc` still composes - but see the note below: what it
                // reports is about the SCREEN, and the screen is showing
                // something else.
                try { uidoc = new UIDocument(target); } catch { uidoc = null; }
            }

            // WHICH MODEL THE CHAT THINKS IT IS IN, CHECKED BEFORE ANYTHING
            // RUNS. Golden Rule 20, enforced on the only side that can still
            // stop.
            //
            // The server compared its pin against the REPLY, and on this path
            // the reply is built after group.Assimilate() has COMMITTED. So a
            // user who switched model between two messages had the change
            // applied to the model in front and was then told "Nothing has
            // been sent to Revit" - a sentence that was false about work
            // already sitting in their undo stack. A refusal that arrives
            // after the act is a description, not a guard. It is the same
            // defect fixed for SelectByCategory on 2026-09-11; this path never
            // had the guard at all. Found by a review 2026-09-15.
            //
            // HERE, AND NOT LOWER. Above the contract binding and the compile
            // because those read the model and cost seconds, and above the
            // TransactionGroup because below it there is nothing left to
            // refuse - only something to undo.
            //
            // ON THE READ PATH TOO, because it is one method and a read of the
            // wrong model is the answer SelectByCategory was fixed for. The
            // proving client sends no key and is pointed at a model
            // deliberately, so nothing there changes.
            //
            // `expectProject` is the caller's pinned project key, or empty
            // when the chat has not pinned one yet. Empty means "do not
            // check": a first request has nothing to compare against, and
            // refusing it would make the pin unobtainable.
            var expectProject = Json.ReadString(request, "expectProject");
            if (!string.IsNullOrEmpty(expectProject))
            {
                var title = "";
                try { title = target.Title; } catch { }
                if (string.IsNullOrEmpty(title)) title = "(unnamed)";

                // Said once so the two refusals below cannot drift apart, and
                // said in the past tense it has earned: this returns above the
                // TransactionGroup, so it is a fact rather than a hope.
                var nothing = writing ? "NOTHING was written." : "NOTHING was read.";

                var here = RevitOperations.ProjectKey(target);

                // NULL IS A FAILED CHECK, NOT A SKIPPED ONE.
                //
                // A family document has no Project Information and therefore
                // no project key. `here != expectProject` would already refuse
                // it, and it is still spelt out: the two cases deserve
                // different sentences, and a refusal that depends on null
                // falling through a string comparison reads like an accident
                // that a later edit could "tidy" into letting null pass.
                //
                // WHY IT REFUSES, ON A WRITE. The chat pinned a project. A
                // family editor BECOMES the active document the moment it is
                // opened, so this is the ordinary way a write lands somewhere
                // nobody pointed at. An unidentifiable target cannot be shown
                // to be the pinned one, and "cannot prove it is the right
                // model" is the same answer as "is the wrong model" when the
                // next thing this method does is commit a transaction.
                //
                // It costs a chat pinned to a project the ability to write
                // into a family without repinning. That is the price, it is
                // paid in a visible refusal that says what to do, and it buys
                // the guarantee that every write this path commits went into a
                // document the user named.
                if (string.IsNullOrEmpty(here))
                {
                    return Json.Error("wrong_document",
                        "This chat has been working on a project, and \"" + title
                        + "\" has no Project Information - a family, or something Heron "
                        + "cannot identify as the model this chat was pointed at. " + nothing
                        + " Open the project you meant, or say 'use this model' to move "
                        + "this chat onto this one deliberately.");
                }

                if (here != expectProject)
                {
                    // NOT "the one in front of Revit now", which is what the
                    // selection guard says. This path can be given a document
                    // BY NAME, so the model it chose need not be the one on
                    // screen, and naming the wrong one is how a correct
                    // refusal gets read as a bug.
                    return Json.Error("wrong_document",
                        "This chat has been working on another model, and the one this "
                        + "would have run in is \"" + title + "\". " + nothing
                        + " Say so explicitly if you meant to change model.");
                }
            }

            var globals = new HeronFragmentGlobals
            {
                doc = target,
                uidoc = uidoc,
                app = app.Application,
                __heron = new Dictionary<string, object>(StringComparer.Ordinal),
            };

            // WHAT THIS FRAGMENT SAYS IT NEEDS, sent with the source because
            // the contract lives in fragment.yaml on the client's disk and
            // Revit has never seen it - the same reason the source is sent
            // rather than a name.
            //
            // ABSENT IS NOT THE SAME AS EMPTY, and both are legal. A caller
            // that sends no `needs` at all gets the old behaviour: doc, uidoc
            // and app, and nothing else. A fragment that genuinely needs
            // nothing beyond those sends an empty list. Only the first is
            // ambiguous, and it is treated as "this caller has not been
            // updated" rather than as "this fragment needs nothing" - which
            // would turn every unbound need into a compile error blamed on
            // the fragment.
            var needs = Json.ReadObjectArray(request, "needs");

            // THE CALLER'S HALF. A contract may declare a need as
            // `source: request` - a view, a category, a name to match, a
            // distance - and nothing could supply one, so 288 of the 308
            // unproven fragments could not be run at all. The refusal below
            // said so in as many words: "until there is a way to pass them".
            // This is that way.
            //
            // SENT AS {name, value} PAIRS rather than as one object, because
            // that reuses the reader `needs` already goes through. A second
            // JSON shape would need a second parser, and a second parser is a
            // second thing to get wrong on a wire that is already parsed by
            // hand.
            //
            // EVERY VALUE CROSSES AS TEXT and becomes its declared type in
            // here, where the model is. A view NAME is not a view: only this
            // side can look one up, and only this side can tell that two
            // views answer to the same name.
            var supplied = new Dictionary<string, string>(StringComparer.Ordinal);
            var givenValues = Json.ReadObjectArray(request, "values");
            if (givenValues != null)
            {
                foreach (var pair in givenValues)
                {
                    string givenName, givenText;
                    if (pair.TryGetValue("name", out givenName)
                        && !string.IsNullOrEmpty(givenName)
                        && pair.TryGetValue("value", out givenText))
                    {
                        supplied[givenName] = givenText;
                    }
                }
            }

            // WHICH CHAT IS ASKING. The bridge puts this on every request and
            // HeronLease already reads it the same way; the chain has to know
            // it too, because what one fragment leaves for the next is that
            // CHAT'S working state and nobody else's.
            var client = Json.ReadString(request, "client");

            // A CHAIN IS OPENED DELIBERATELY, NOT INHERITED. What the previous
            // fragment left would otherwise outlive the batch that produced
            // it: a fragment run an hour later against the same model would
            // bind elements collected by something nobody remembers running,
            // report them as coming "from the previous fragment", and be right
            // about the words and wrong about the run. The client says "reset"
            // on the first fragment of a batch and nothing on the rest.
            var resetting = string.Equals(Json.ReadString(request, "chain"), "reset",
                                          StringComparison.Ordinal);

            // WHAT THE CALLER BELIEVES IT IS ABOUT TO CONSUME.
            //
            // Empty is the default and means today's behaviour exactly: the
            // chain is whatever survived, taken on trust, and the client
            // normally resets it. Non-empty turns the trust into a CHECK -
            // "these elements must be the ones select-by-categories left, and
            // it must have run with categories=Pipes" - and a mismatch refuses
            // rather than binding something plausible. docs/36.
            var expect = Json.ReadString(request, "expectChain");
            if (expect != null) expect = expect.Trim();

            // A CONTRADICTION THAT WOULD OTHERWISE LOOK LIKE AN EMPTY CHAIN.
            // Resetting clears the very values the expectation is about, so
            // the check could only ever fail, and it would fail saying
            // "nothing is carried" - which reads as the PRODUCER having done
            // nothing. Naming the real fault here saves that hunt.
            if (resetting && !string.IsNullOrEmpty(expect))
            {
                return Json.Error("chain_contradiction",
                    "This request both RESETS the chain and expects to consume "
                    + "'" + expect + "' from it. Resetting clears exactly what "
                    + "the expectation is about, so it could only fail - and it "
                    + "would fail saying nothing was carried, which reads as the "
                    + "earlier fragment having done nothing. Send one or the "
                    + "other.");
            }

            if (resetting)
            {
                Forget(client);
            }

            var bound = new HashSet<string>(StringComparer.Ordinal)
            {
                "doc", "uidoc", "app"
            };

            // WHAT THIS RUN WAS ASKED FOR, rendered once and carried on the
            // chain so the NEXT fragment can check it got the right list
            // rather than merely a list. Sorted, because a dictionary's order
            // is not a fact about the request and two identical runs must
            // render identically.
            var ranWith = DescribeSupplied(supplied);

            var prologue = "";
            Script<object> script = null;

            // WHAT A WRITE SETUP STEP LEAVES CANNOT BE BOUND BEFORE IT RUNS.
            //
            // Binding and compiling belong HERE for almost every run: before
            // the model is touched, so a request that cannot be satisfied is
            // refused cheaply and no transaction is ever opened. That ordering
            // is deliberate and it stays.
            //
            // It is wrong for exactly one shape of run - a write phase carrying
            // setup steps that THEMSELVES write. Those are deferred into the
            // fragment's own TransactionGroup (see RunSetupSteps) so that the
            // fragment can see what they made. But binding sixty lines earlier
            // decided the fragment's needs before any of it existed, so what a
            // deferred step leaves could reach the NEXT STEP and never the
            // fragment under test.
            //
            // MEASURED 2026-09-17 on Snowdon-scratch.rvt: create-line drew its
            // two model lines, RunSetupSteps called Remember on them, and
            // find-overlapping-lines was still refused with "'elements' was
            // never supplied" - because it had been told so before the lines
            // existed. That is the whole of "build the case on purpose",
            // which fragment-proving rule 2 sends you to on a clean model.
            //
            // So when, and ONLY when, there are setup steps to run first, the
            // binding waits for them. Every other run keeps the old order
            // exactly - and that is what keeps this small enough to trust: a
            // run with no write setup cannot behave differently, because
            // nothing about it moved.
            var setupSteps = Json.ReadObjectArray(request, "setup");
            var bindAfterSetup = writing && setupSteps != null && setupSteps.Count > 0;

            // The bind and the compile as ONE act, in one place, because a
            // prologue that bound is useless without the script it was written
            // for - and two copies of this would drift.
            Func<string> bindAndCompile = () =>
            {
                if (needs != null)
                {
                    string binding;
                    var refusal = BindNeeds(needs, globals, target, uidoc, bound, client,
                                            supplied, expect, out prologue, out binding);
                    if (refusal != null) return refusal;
                    Note = binding;
                }
                else
                {
                    Note = null;
                }

                return Compile(prologue + source, PrologueLines(prologue), out script);
            };

            if (!bindAfterSetup)
            {
                var early = bindAndCompile();
                if (early != null) return early;
            }

            // APPLY IS THE DELIBERATE ACT. Absent means run it and roll back,
            // which is this path's preview: not a prediction of what would
            // happen, but a record of what DID, undone. A caller that forgets
            // the flag gets the safe half.
            var apply = writing
                && string.Equals(Json.ReadString(request, "apply"), "true",
                                 StringComparison.OrdinalIgnoreCase);

            ScriptState<object> state = null;
            string threw = null;

            // Only meaningful on the writing path with apply off. Reported so a
            // reader never has to infer a rollback from the request that asked
            // for one - see SafeRollBack.
            var rolledBack = false;

            // WHAT REVIT SAID WHILE THE JOB RAN - every setup step that writes
            // and the fragment itself - and what Heron did about each. Filled
            // by the preprocessor Discipline puts on every transaction below,
            // read by the refusal and by the verdict. Empty on the read path,
            // which opens no transaction for Revit to post anything to.
            var said = new HeronFailureNote();

            if (!writing)
            {
                threw = RunScript(script, globals, name, out state);
            }
            else
            {
                // ONE TRANSACTION GROUP, so one Ctrl+Z puts the model back -
                // Golden Rule 16. Named for the fragment, so Revit's own undo
                // history reads as something a person did.
                var label = "Heron: " + name;
                using (var group = new TransactionGroup(target, label))
                {
                    group.Start();

                    // SETUP THAT HAS TO CHANGE THE MODEL, inside this group.
                    //
                    // A proof arranges the model before the fragment under test
                    // runs - select these, then set the selection. Those are
                    // READ and EXECUTE, and the client runs them through
                    // run_fragment_read, which opens no transaction.
                    //
                    // SOME ARRANGEMENTS NEED A WRITE. copy-parameter-value
                    // needs a parameter that HAS a value; edit-text-values
                    // needs a text note to edit. Sent as setup through the read
                    // path those throw, and sent as their own write they are
                    // their own transaction group - committed and kept, or
                    // rolled back before the fragment under test ever sees
                    // them. Neither is an arrangement.
                    //
                    // Here they are steps INSIDE the group the fragment under
                    // test is already in: each commits so the next step and the
                    // fragment can see it, and the GROUP decides whether any of
                    // it survives. `apply` still means exactly what it meant -
                    // assimilate the lot, or roll the lot back.
                    //
                    // ONLY ON THE WRITE PATH. A read run has no group to put
                    // them in, and a setup step that writes has no business on
                    // a path whose whole guarantee is that Revit refuses it.
                    var setupError = RunSetupSteps(request, globals, target, uidoc,
                                                   client, supplied, group, label, said);
                    if (setupError != null) return setupError;

                    // NOW the fragment can be given what the setup just made.
                    // See bindAfterSetup above for why this waits.
                    if (bindAfterSetup)
                    {
                        var late = bindAndCompile();
                        if (late != null)
                        {
                            // THE SETUP HAS ALREADY WRITTEN. Refusing without
                            // rolling back would leave its changes sitting in
                            // the model under a group nobody closes - a preview
                            // that altered something, which is the one outcome
                            // this path must never produce. Every other refusal
                            // on this page is free because nothing had run yet;
                            // this one is not, so it pays for itself here.
                            SafeRollBack(group);
                            return late;
                        }
                    }

                    using (var transaction = new Transaction(target, label))
                    {
                        transaction.Start();

                        // BEFORE THE FRAGMENT RUNS, not before the commit: the
                        // options belong to the transaction, and a fragment
                        // that throws is rolled back through them too.
                        Discipline(transaction, said, null);

                        threw = RunScript(script, globals, name, out state);

                        if (threw != null)
                        {
                            SafeRollBack(transaction);
                            SafeRollBack(group);
                            return threw;
                        }

                        // NOT COMMITTED MEANS DISCIPLINE ROLLED IT BACK, almost
                        // always: Revit posted something that was not a plain
                        // warning. `said` holds Revit's words for it, and the
                        // refusal quotes them - "Revit did not accept the
                        // change" was all this said until 2026-09-23, which
                        // tells a modeller nothing they can act on.
                        //
                        // AND "THE MODEL IS EXACTLY AS IT WAS" IS SAID ONLY
                        // WHEN REVIT REPORTED THE GROUP ROLLED BACK. It used to
                        // be said unconditionally, on the strength of having
                        // asked - the 2026-09-09 mistake WithVerdict was fixed
                        // for, still standing on this branch. A write setup
                        // step committed inside this group before the fragment
                        // ran, so the group's rollback is what undoes it.
                        if (transaction.Commit() != TransactionStatus.Committed)
                        {
                            return Json.Error("operation_failed",
                                RefusedBy("'" + name + "' ran, and Revit would not keep it.",
                                          said, SafeRollBack(group)));
                        }
                    }

                    // ASSIMILATE, so the whole group collapses to ONE undo step
                    // rather than leaving the inner transaction visible as its
                    // own. Rolling back instead is what makes the default a
                    // preview.
                    if (apply) group.Assimilate();
                    else rolledBack = SafeRollBack(group);
                }
            }

            if (threw != null) return threw;

            Remember(name, state, target, bound, client, globals.__heron, ranWith);
            var answer = Report(name, state, target, uidoc, app.ActiveUIDocument, bound,
                                globals.__heron);

            return writing ? WithVerdict(answer, apply, name, rolledBack, said) : answer;
        }

        /// <summary>
        /// Runs the arrangement steps that have to change the model, each in
        /// its own transaction inside the caller's group. Null when they all
        /// ran; a refusal to hand straight back when one did not.
        ///
        /// EACH STEP COMMITS. The next step, and the fragment under test, have
        /// to be able to SEE what this one wrote - a parameter filled by step
        /// two is the thing step three exists to copy. Rolling each step back
        /// would leave the arrangement unmade, which is the defect this method
        /// was written for.
        ///
        /// THE GROUP STILL DECIDES. Committing a step is not keeping it: the
        /// caller assimilates the group on `apply` and rolls it back otherwise,
        /// so a proof run leaves the model exactly as it was, arrangement and
        /// all. That is why they belong in the caller's group rather than in
        /// one of their own.
        ///
        /// A STEP THAT FAILS STOPS EVERYTHING. A half-made arrangement is worse
        /// than none: the fragment under test would run against a model that is
        /// neither what it was nor what was asked for, and report about it with
        /// complete confidence. The group is rolled back before returning.
        /// </summary>
        private static string RunSetupSteps(string request,
                                            HeronFragmentGlobals globals,
                                            Document target,
                                            UIDocument uidoc,
                                            string client,
                                            Dictionary<string, string> supplied,
                                            TransactionGroup group,
                                            string label,
                                            HeronFailureNote said)
        {
            var steps = Json.ReadObjectArray(request, "setup");
            if (steps == null || steps.Count == 0) return null;

            // A SETUP STEP RUNS ON THE CALLER'S OWN VALUES, the same ones the
            // fragment under test was given, so what it left is described the
            // same way. Rendered once rather than per step.
            var ranWith = DescribeSupplied(supplied);

            for (var index = 0; index < steps.Count; index++)
            {
                var step = steps[index];

                string stepSource;
                step.TryGetValue("source", out stepSource);
                if (string.IsNullOrEmpty(stepSource))
                {
                    SafeRollBack(group);
                    return Json.Error("setup_no_source",
                        "A setup step was sent with no source, so the arrangement could not "
                        + "be made and nothing was run. This operation runs the C# it is "
                        + "given; it does not read the fragment library itself.");
                }

                string stepName;
                if (!step.TryGetValue("name", out stepName) || string.IsNullOrEmpty(stepName))
                    stepName = "(unnamed setup step)";

                // EACH STEP BINDS ITS OWN NEEDS, against the same chain. What
                // step one leaves is what step two binds - that is the whole
                // point of running them in order - so `bound` starts fresh per
                // step while the chain behind it does not.
                var stepBound = new HashSet<string>(StringComparer.Ordinal)
                {
                    "doc", "uidoc", "app"
                };

                var stepNeeds = ReadStepNeeds(step);

                var stepPrologue = "";
                if (stepNeeds != null)
                {
                    string stepBinding;
                    // NO EXPECTATION INSIDE A FRAGMENT. The caller's
                    // expectation is about what THIS fragment consumes on the
                    // way in; a step consumes what the step before it left,
                    // whose `By` is this fragment rather than the producer the
                    // caller named. Re-checking it here would refuse every
                    // multi-step fragment on its second step.
                    var refusal = BindNeeds(stepNeeds, globals, target, uidoc, stepBound,
                                            client, supplied, null,
                                            out stepPrologue, out stepBinding);
                    if (refusal != null)
                    {
                        SafeRollBack(group);
                        return Json.Error("setup_failed",
                            "The arrangement step '" + stepName + "' could not be given what "
                            + "it needs, so nothing was run and the model is as it was. "
                            + refusal);
                    }
                }

                Script<object> stepScript;
                var compileError = Compile(stepPrologue + stepSource,
                                           PrologueLines(stepPrologue), out stepScript);
                if (compileError != null)
                {
                    SafeRollBack(group);
                    return Json.Error("setup_failed",
                        "The arrangement step '" + stepName + "' did not compile, so nothing "
                        + "was run and the model is as it was. " + compileError);
                }

                using (var stepTransaction = new Transaction(target, label + " - " + stepName))
                {
                    stepTransaction.Start();

                    // THE SAME DISCIPLINE AS THE FRAGMENT'S OWN TRANSACTION, and
                    // for the same reason: a setup step is C# that writes, and
                    // an error in it left to Revit's own handling could delete
                    // elements before the fragment under test ever ran. Named
                    // for the step, so the reader can tell whose warning it was.
                    Discipline(stepTransaction, said, stepName);

                    ScriptState<object> stepState;
                    var threw = RunScript(stepScript, globals, stepName, out stepState);
                    if (threw != null)
                    {
                        SafeRollBack(stepTransaction);
                        SafeRollBack(group);
                        return threw;
                    }

                    if (stepTransaction.Commit() != TransactionStatus.Committed)
                    {
                        return Json.Error("setup_failed",
                            RefusedBy("Revit would not keep the arrangement step '" + stepName
                                      + "', so the fragment under test never ran.",
                                      said, SafeRollBack(group)));
                    }

                    // WHAT IT LEFT, for the step after it and for the fragment
                    // under test. Without this the chain ends at the first
                    // setup step and the arrangement cannot be built up.
                    Remember(stepName, stepState, target, stepBound, client, globals.__heron,
                             ranWith);
                }
            }

            return null;
        }

        /// <summary>
        /// A setup step's `needs`, read out of the step rather than the
        /// request. Json.ReadObjectArray reads a named array from a JSON
        /// document; a step arrives already parsed into a flat dictionary, so
        /// its needs come across as the text of that array and are re-read here
        /// rather than parsed a second way.
        /// </summary>
        private static List<Dictionary<string, string>> ReadStepNeeds(
            Dictionary<string, string> step)
        {
            string needsText;
            if (!step.TryGetValue("needs", out needsText) || string.IsNullOrEmpty(needsText))
                return null;
            return Json.ReadObjectArray("{\"needs\":" + needsText + "}", "needs");
        }

        /// <summary>
        /// Compile once, keep it. Returns null on success, or the refusal.
        ///
        /// A COMPILE FAILURE HERE IS NOT THE SAME AS A FAILURE IN THE GATE.
        /// tools/check-fragments-compile.py builds against the reference
        /// assemblies for a release; this builds against the assemblies Revit
        /// has actually loaded. When those two disagree, this one is right -
        /// and the disagreement is worth reporting rather than smoothing over,
        /// because it means the gate is checking something the model is not.
        ///
        /// THIS PARAGRAPH SPENT ITS LIFE ABOVE THE WRONG METHOD. It sat as a
        /// second, adjacent summary in front of RunSetupSteps, which C#
        /// accepts - the last one wins - so it compiled clean while Compile
        /// itself had no documentation at all and RunSetupSteps was headed by
        /// a paragraph about compiling. Nothing catches that; it was found by
        /// reading the file. FRAGMENT-ISSUES section 5b.
        /// </summary>
        private static string Compile(string source, int prologueLines, out Script<object> script)
        {
            script = null;

            lock (Compiled)
            {
                if (Compiled.TryGetValue(source, out script)) return null;
            }

            ScriptOptions options;
            try
            {
                options = ScriptOptions.Default
                    .WithReferences(
                        typeof(object).Assembly,                    // mscorlib
                        typeof(Enumerable).Assembly,                // System.Core
                        typeof(Document).Assembly,                  // RevitAPI
                        typeof(UIDocument).Assembly)                // RevitAPIUI
                    .WithImports(HeronFragmentImports.Namespaces);
            }
            catch (Exception failure)
            {
                return Json.Error("script_options",
                    "Could not set up the script host: " + Innermost(failure).Message);
            }

            // THE CACHE STAYS KEYED ON THE ORIGINAL SOURCE, above and below.
            // The guard is a pure function of it, so keying on the rewritten
            // text would be the same lookup with extra work - and the key has
            // to be the text that arrived, because that is what changes when
            // somebody edits a fragment.cs.
            //
            // Guarded because RunScript's catch cannot catch a stack overflow:
            // the runtime fails fast and takes Revit with it. See
            // HeronStackGuard, which falls back to `source` untouched if it
            // cannot do its job.
            var guarded = HeronStackGuard.Apply(source);

            var candidate = CSharpScript.Create<object>(guarded, options, typeof(HeronFragmentGlobals));

            var diagnostics = candidate.Compile();

            var errors = diagnostics
                .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .Select(d => d.ToString())
                .ToList();

            if (errors.Count > 0)
            {
                // THE LINE NUMBERS ARE NOT THE FRAGMENT'S. The host puts one
                // generated line in front of the snippet for each need it
                // bound, so every error below is that many lines further down
                // than the same error in fragment.cs. Said plainly here
                // because the alternative is somebody reading line 14 of a
                // nine-line fragment and concluding the compiler is wrong.
                var offset = prologueLines == 0 ? "" :
                    " (line numbers include " + prologueLines +
                    " generated line(s) the host put in front of the fragment - " +
                    "subtract " + prologueLines + " to find the line in fragment.cs)";

                return Json.Error("compile_failed",
                    "The fragment did not compile against the assemblies this Revit has loaded: " +
                    string.Join("; ", errors.Take(5)) +
                    (errors.Count > 5 ? " ... and " + (errors.Count - 5) + " more" : "") +
                    offset);
            }

            lock (Compiled)
            {
                Compiled[source] = candidate;
            }

            script = candidate;
            return null;
        }

        /// <summary>
        /// What the fragment left behind.
        ///
        /// A fragment's contract is its `provides` list, and every one of them
        /// is an ordinary local variable in the script. Roslyn hands those
        /// back by name, so the report is read out of the run rather than
        /// being something the fragment had to remember to build.
        /// </summary>
        private static string Report(string name, ScriptState<object> state,
                                     Document target, UIDocument uidoc, UIDocument active,
                                     HashSet<string> bound,
                                     IDictionary<string, object> handedIn)
        {
            // THE ANSWER ALWAYS NAMES THE DOCUMENT, and the model it ran
            // against is the first thing on it. A bare result is how somebody
            // acts on an answer that came from a model they were not looking
            // at - and during the first proving run this fragment reported two
            // levels called "Level 1" and "Level 2" from what was plainly not
            // the sample building anybody had in mind. Without the title on
            // the line, that reads as a fact about the project.
            var title = "";
            try { title = target.Title; } catch { }

            // AND ITS IDENTITY, NOT ONLY ITS NAME. A title is what a person
            // reads; it is not what tells two models apart, and this reply was
            // the ONE operation sending the name alone. Every other op sends
            // documentPath and projectKey, so the server's document pin held
            // "project:<uid>" from a read tool and then read "title:Project1"
            // back off every fragment run - two strings, one model - and
            // refused the write naming the SAME model on both sides of its
            // "but". Reported 2026-09-15 against an UNSAVED model, where the
            // path was absent too and nothing could stand in for the key.
            //
            // Read with the same caution as the title above, and for the same
            // reason: this runs AFTER the fragment, and a fragment is other
            // people's code that may have closed the document it was given.
            var path = "";
            try { path = target.PathName; } catch { }

            string projectKey = null;
            try { projectKey = RevitOperations.ProjectKey(target); } catch { }

            var view = "";
            try { view = uidoc == null || uidoc.ActiveView == null ? "" : uidoc.ActiveView.Name; }
            catch { }

            // WHETHER THE MODEL READ IS THE ONE ON SCREEN. When it is not,
            // anything a fragment says about a selection or an active view is
            // about a window nobody is looking at - true, and easy to misread.
            var inFront = true;
            try
            {
                inFront = active != null && active.Document != null
                    && active.Document.Title == title;
            }
            catch { }

            var parts = new List<string>
            {
                Json.Str("ran", name),
                Json.Str("document", title),

                // THE SAME IDENTITY EVERY OTHER OPERATION SENDS, and the half
                // of the guard above that makes it reachable a second time.
                //
                // This path reported the title and nothing else, so
                // DocumentPin.key_of fell to its "title:" fallback and
                // pinned.project_key - which reads only a "project:" key -
                // stayed None. Two consequences, both found 2026-09-15:
                //
                //   * expectProject went out EMPTY on every later call, so the
                //     add-in gate above never ran for a chat whose pin came
                //     from a fragment
                //   * one model pinned "project:..." by another tool and
                //     "title:..." by this one looked like two documents, and
                //     the server refused a model it was already working in
                //
                // Null on a family document, deliberately - see ProjectKey.
                Json.Str("documentPath", string.IsNullOrEmpty(path) ? null : path),
                Json.Str("projectKey", projectKey),
                Json.Str("activeView", view),
                Json.Bool("wasActiveDocument", inFront),
            };

            // WHERE THE INPUTS CAME FROM, on the answer itself. A fragment
            // that ran on the selection and one that ran on the previous
            // fragment's output produce the same shape of result, and reading
            // the second as the first is how somebody concludes a filter is
            // broken when it was never consulted.
            if (!string.IsNullOrEmpty(Note)) parts.Add(Json.Str("bound", Note));

            var left = new List<string>();

            foreach (var variable in state.Variables)
            {
                // Skip what the host put in. Those are the `needs`, and
                // echoing a whole Document back is neither useful nor small.
                //
                // THIS USED TO NAME THE THREE GLOBALS AND NOTHING ELSE, which
                // was correct exactly as long as the host could bind only
                // three names. The moment it could bind `elements`, the list
                // the host had just handed IN came back OUT under `provides`,
                // reported as something the fragment produced - a filter that
                // did nothing would have looked identical to one that worked.
                // The set is whatever was actually bound for this run - unless
                // the fragment REPLACED it, which is the one case where a bound
                // name is genuinely an output. See StillTheHosts.
                object current = null;
                try { current = variable.Value; } catch { continue; }

                if (bound != null && bound.Contains(variable.Name)
                    && StillTheHosts(handedIn, variable.Name, current)) continue;

                left.Add(Json.Str(variable.Name, Describe(current)));
            }

            parts.Add("\"provides\":{" + string.Join(",", left) + "}");
            parts.Add(Json.Num("providesCount", left.Count));

            return Json.Ok(parts.ToArray());
        }

        /// <summary>
        /// Is this variable STILL the object the host handed in?
        ///
        /// THE TEST IS IDENTITY, NOT NAME, and that is the whole point. Both
        /// the answer and the chain skip what the host put in - echoing a
        /// Document back is useless, and a filter that did nothing must not
        /// look like one that worked by handing its own input back as output.
        ///
        /// But skipping by NAME made the opposite mistake, and it was worse.
        /// A filter fragment narrows by REASSIGNING its input - `elements =
        /// remaining;` is how all 31 of them end. Skipping the name threw that
        /// away: find-untagged-elements narrowed 625 to 560, and the next
        /// fragment in the chain counted 625, having fallen back to the raw
        /// selection. Silently. A chain of "find the untagged ones, then tag
        /// them" would have tagged all 625, including the 65 already tagged.
        /// Found by running two fragments in one chain, 2026-09-08.
        ///
        /// Identity separates the two cases exactly: untouched means the same
        /// object, so it is still the host's and is skipped; replaced means a
        /// new one, which is precisely what the fragment produced.
        /// </summary>
        private static bool StillTheHosts(IDictionary<string, object> handedIn,
                                          string name, object value)
        {
            if (handedIn == null) return false;
            object original;
            if (!handedIn.TryGetValue(name, out original)) return false;
            return ReferenceEquals(original, value);
        }

        /// <summary>
        /// Run the compiled script, or return the refusal. Pulled out because
        /// the write path needs it INSIDE a transaction and the read path
        /// needs it outside one, and a second copy would be the half that
        /// drifts.
        /// </summary>
        private static string RunScript(Script<object> script, HeronFragmentGlobals globals,
                                        string name, out ScriptState<object> state)
        {
            state = null;
            try
            {
                // The fragments never await, so this completes inline on the
                // Revit API thread - which is where it has to run. If a
                // fragment ever does await, this is the line that will deadlock
                // and the reason will not be obvious.
                state = script.RunAsync(globals).GetAwaiter().GetResult();
                return null;
            }
            catch (Exception failure)
            {
                // A fragment that throws is a finding, not a crash. The message
                // is Revit's own and is far more use than "it failed".
                return Json.Error("fragment_threw",
                    "'" + name + "' threw while running: " + Innermost(failure).Message);
            }
        }

        /// <summary>
        /// Roll back without letting the rollback itself become the failure.
        ///
        /// The same hazard RevitWrite.SafeRollBack exists for, and for the same
        /// reason: rolling back something already rolled back throws, and it
        /// throws from the catch block where the real error is still being
        /// handled - so the useful message is lost and replaced by a confusing
        /// one, at the worst possible moment.
        /// </summary>
        private static bool SafeRollBack(TransactionGroup group)
        {
            // IT RETURNS WHETHER THE MODEL WAS ACTUALLY PUT BACK, and that
            // return value is the whole point of the 2026-09-09 change.
            //
            // This was `void`. Both ways it can fail to roll anything back -
            // a group whose status is not Started, so the call is SKIPPED, and
            // a RollBack() that throws, so the call is SWALLOWED - left no
            // trace whatsoever, and every caller went on to report "nothing was
            // kept" because that is what it had ASKED for. A failed rollback
            // and a clean one produced identical output, which is why three
            // incidents in section 1c have no explained mechanism: nothing was
            // ever in a position to notice.
            //
            // Revit's own status AFTER the attempt is the only evidence there
            // is, so it is what gets returned. It is not a guarantee the model
            // is untouched - 2026-09-09 showed Revit's bookkeeping and the
            // model can disagree - but "Revit says RolledBack" is a fact, and
            // the sentence it supports is one that was checked.
            try
            {
                if (group.GetStatus() == TransactionStatus.Started) group.RollBack();
                return group.GetStatus() == TransactionStatus.RolledBack;
            }
            catch { return false; }
        }

        private static void SafeRollBack(Transaction transaction)
        {
            try
            {
                if (transaction.GetStatus() == TransactionStatus.Started) transaction.RollBack();
            }
            catch { }
        }

        /// <summary>
        /// Put a started transaction under Heron's failure rule, before anything
        /// runs in it. EVERY Transaction this file opens goes through here -
        /// tests/test_failure_note.py counts them - so none can reach Revit's
        /// own failure handling.
        ///
        /// WHY IT MATTERS. When a transaction commits, Revit posts what went
        /// wrong and asks the transaction's preprocessor first; with none, it
        /// goes to Revit's own handling, and Revit's handling of an ERROR is a
        /// resolution - one of which can be to DELETE the elements the error
        /// names. Until 2026-09-23 no transaction here had a preprocessor, while
        /// RevitWrite's move path always had one (earlier-brain plan, H4).
        ///
        /// SetClearAfterRollback, because Revit's own documentation of
        /// ProceedWithRollBack says that without it "default failure processing
        /// will continue, and failures may be delivered to the user even though
        /// the transaction will be rolled back" - a dialog after the fact, in
        /// front of whoever is at the machine, about a job that did not happen.
        /// It covers the rollback of a fragment that THREW as well: whatever it
        /// had posted is cleared with it, and the refusal says why it stopped.
        /// </summary>
        private static void Discipline(Transaction transaction, HeronFailureNote said, string step)
        {
            var options = transaction.GetFailureHandlingOptions();
            options.SetFailuresPreprocessor(new FailureDiscipline(said, step));
            options.SetClearAfterRollback(true);
            transaction.SetFailureHandlingOptions(options);
        }

        /// <summary>
        /// Reads what Revit posted at one commit and does what HeronFailureNote
        /// decides: dismiss every warning, or roll the whole transaction back.
        /// It NEVER returns Continue with anything left in the list and never
        /// asks Revit to resolve anything, so Revit's own resolution never runs.
        ///
        /// FAILS CLOSED AT EVERY STEP IT CANNOT COMPLETE. A list it cannot read,
        /// a severity it cannot read, a warning Revit will not let it dismiss -
        /// each is a rollback with the reason recorded, because each is a
        /// failure that would otherwise be left to Revit's handling, which is
        /// the one thing this exists to prevent. Nothing is allowed to throw out
        /// of here into Revit's failure processing.
        /// </summary>
        private sealed class FailureDiscipline : IFailuresPreprocessor
        {
            private readonly HeronFailureNote _said;
            private readonly string _step;

            public FailureDiscipline(HeronFailureNote said, string step)
            {
                _said = said;
                _step = step;
            }

            public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
            {
                try
                {
                    var messages = accessor.GetFailureMessages();

                    // EVERY MESSAGE IS JUDGED BEFORE ANY WARNING IS TOUCHED -
                    // the move path's order. A batch holding one error rolls
                    // back whole, and its warnings are neither deleted nor
                    // counted: nothing was kept for them to be about.
                    var batch = new List<HeronFailureNote.Posted>(messages.Count);
                    foreach (var message in messages) batch.Add(Read(message));

                    if (_said.RollsBack(_step, batch, (int)FailureSeverity.Warning))
                        return FailureProcessingResult.ProceedWithRollBack;

                    // RollsBack answered false, so EVERY message is a warning.
                    for (var i = 0; i < messages.Count; i++)
                    {
                        try
                        {
                            accessor.DeleteWarning(messages[i]);
                        }
                        catch (Exception failure)
                        {
                            _said.CouldNotDismiss(_step, batch[i].Text, batch[i].Elements,
                                                  Innermost(failure).Message);
                            return FailureProcessingResult.ProceedWithRollBack;
                        }
                    }

                    return FailureProcessingResult.Continue;
                }
                catch (Exception failure)
                {
                    _said.Unreadable(_step, Innermost(failure).Message);
                    return FailureProcessingResult.ProceedWithRollBack;
                }
            }

            /// <summary>
            /// One message, as numbers and words. The severity is read FIRST and
            /// a severity that cannot be read becomes -1, which is not a warning,
            /// so it rolls back. The words and the element count are only ever
            /// description - a message whose text cannot be read is still judged.
            /// </summary>
            private static HeronFailureNote.Posted Read(FailureMessageAccessor message)
            {
                int severity;
                try { severity = (int)message.GetSeverity(); }
                catch (Exception) { severity = -1; }

                string text = null;
                try { text = message.GetDescriptionText(); }
                catch (Exception) { }

                var elements = 0;
                try
                {
                    var ids = message.GetFailingElementIds();
                    elements = ids == null ? 0 : ids.Count;
                }
                catch (Exception) { }

                return new HeronFailureNote.Posted(severity, text, elements);
            }
        }

        /// <summary>
        /// A refusal after Revit would not keep a write: what stopped it, in
        /// Revit's words, and what happened to the model - which is said only
        /// as far as Revit confirmed it.
        /// </summary>
        private static string RefusedBy(string what, HeronFailureNote said, bool rolledBack)
        {
            var why = said.RefusedCount > 0
                ? said.RefusedSentence()
                : "Revit gave no reason Heron could read.";

            var model = rolledBack
                ? "Revit confirmed the rollback, so the model is exactly as it was."
                : "Revit did NOT confirm the rollback, so THE MODEL MAY STILL HOLD PART OF THIS "
                  + "JOB. Check what it would have changed before trusting anything here, and do "
                  + "not save until you have.";

            return what + " " + why + " Heron rolled the whole job back rather than let Revit "
                 + "resolve it its own way - Revit's own fix for an error can be to delete the "
                 + "elements it names. " + model;
        }

        /// <summary>
        /// Say, on the answer itself, whether the model was left changed.
        ///
        /// A WRITE THAT WAS ROLLED BACK LOOKS EXACTLY LIKE ONE THAT WAS KEPT -
        /// same counts, same findings, same everything, because the fragment
        /// genuinely did the work both times. The only difference is whether it
        /// survived, and nothing else on the reply says so. Somebody reading
        /// "renamed 47 views" and finding 47 unrenamed views would be right to
        /// distrust every number Heron has ever given them.
        /// </summary>
        private static string WithVerdict(string answer, bool applied, string name,
                                          bool rolledBack, HeronFailureNote said)
        {
            if (string.IsNullOrEmpty(answer) || !answer.EndsWith("}", StringComparison.Ordinal))
                return answer;

            // "NOTHING WAS KEPT" USED TO BE SAID ON THE STRENGTH OF `applied`
            // ALONE, which is the request rather than the result. On
            // 2026-09-09 a rollback did not hold and this sentence claimed it
            // had (section 1c). It is now only said when Revit reported the
            // group RolledBack; when it did not, that is what is said instead,
            // because a write that may still be standing is the one thing a
            // reader must not have to guess at.
            string verdict;
            if (applied)
            {
                // ROW 117. THIS BRANCH USED TO SAY "the model was CHANGED", AND IT SAID IT
                // FROM `applied` - THE CALLER'S REQUEST TO KEEP THE TRANSACTION, NOT A
                // STATEMENT THAT ANY WORK WAS DONE. Measured 2026-09-17 on `test projject`:
                // RENAME_ELEMENTS with a `find` that matched nothing reported
                // `notMatched 10, planned 0, renamed 0` and then claimed the model had
                // changed. It is the SAME FAMILY as the `rolledBack` branch below, which was
                // fixed on 2026-09-09 for exactly this reason - a write describing its INTENT
                // where a reader will take it as an OUTCOME.
                //
                // IT CANNOT READ THE COUNTS AND DECIDE FOR ITSELF, and that was considered
                // rather than skipped. Which number means work and which means bookkeeping is
                // D-52's `role`, and that lives in `fragment.yaml` on the brain side - this
                // side is handed the C# and the bindings, never the contract. "All the
                // numbers are zero" would ALSO have got the measured case wrong, because
                // `notMatched 10` is non-zero in the very run that changed nothing. So this
                // says what is CERTAIN - the transaction was kept - and sends the reader to
                // the counts, which are already in the same reply.
                //
                // THE UNDO SENTENCE IS NOW CONDITIONAL, and that is the half that was
                // actually dangerous rather than merely untrue: told to press Ctrl+Z after a
                // run that did nothing, a modeller undoes whatever they did BEFORE it.
                // AND IT MUST NOT SAY THE MODEL IS UNCHANGED, WHICH THE FIRST
                // VERSION DID. A request carrying deferred write SETUP steps
                // runs them through RunSetupSteps inside THIS SAME group, and
                // Assimilate keeps whatever they did - so "the counts show no
                // work, therefore nothing changed" is false exactly when an
                // arrangement was built and the fragment under test then found
                // nothing. Found by review on PR #198, and it is this row's own
                // mistake made twice: describing the whole group's outcome from
                // one part's counts.
                // AND IT STILL MUST NOT PROMISE AN UNDO STEP, WHICH THE SECOND
                // VERSION DID. Row 117's first repair made Ctrl+Z conditional
                // for exactly this reason; rewording it for the setup-steps
                // case put the unconditional promise straight back. Two
                // sentences, the same defect, half an hour apart - found by
                // review on PR #198.
                //
                // THE HAZARD IS NOT THE UNTRUE HALF, IT IS THE INSTRUCTION. If
                // nothing was written there may be NO new undo entry, and a
                // modeller told to press Ctrl+Z then undoes whatever they did
                // BEFORE running this. Saying "if anything was written" costs
                // one clause and cannot send anybody backwards.
                verdict = "'" + name + "' was KEPT: Revit accepted the transaction rather than "
                        + "rolling it back. THE COUNTS ABOVE ARE WHAT THIS FRAGMENT DID and "
                        + "nothing more - anything a setup step did was kept in the same group "
                        + "and is not counted here, so counts of zero do not by themselves mean "
                        + "the model is untouched. IF anything was written, all of it is ONE "
                        + "undo step; if nothing was, there is no new undo entry and Ctrl+Z "
                        + "would undo whatever you did before this.";
            }
            else if (rolledBack)
            {
                verdict = "NOTHING WAS KEPT. '" + name + "' ran for real and Revit reported the "
                        + "transaction group rolled back afterwards, so the counts above are "
                        + "what it actually did rather than a guess. Send it again with apply "
                        + "to keep it.";
            }
            else
            {
                verdict = "THE ROLLBACK DID NOT REPORT SUCCESS. '" + name + "' ran for real, the "
                        + "rollback was requested, and Revit did not afterwards report the "
                        + "transaction group as rolled back - so THE MODEL MAY STILL HOLD THIS "
                        + "CHANGE. Check the thing that was written before trusting anything "
                        + "here, and do not save until you have.";
            }

            // WHAT REVIT RAISED AND HERON DISMISSED, on the verdict itself -
            // the one sentence the chat prints whole, so it reaches the
            // modeller without the server having to learn a new field. Empty
            // when nothing was raised, so a clean run reads exactly as it did.
            var dismissed = said == null ? "" : said.DismissedSentence();
            if (dismissed.Length > 0) verdict += " " + dismissed;

            // AND EVERY ONE OF THEM, for anything reading the reply rather than
            // the sentence: the sentence quotes a few kinds and counts the rest,
            // and this is where "the rest" is. `warnings` is the move path's
            // field name for the same count, so the two replies agree.
            var listed = new List<string>();
            if (said != null)
            {
                foreach (var one in said.Dismissed)
                {
                    listed.Add(Json.Obj(Json.Str("text", one.Text),
                                        Json.Num("times", one.Times),
                                        Json.Num("elements", one.Elements),
                                        Json.Str("step", one.Step)));
                }
            }

            return answer.Substring(0, answer.Length - 1)
                 + "," + Json.Bool("applied", applied)
                 + "," + Json.Bool("rolledBack", rolledBack)
                 + "," + Json.Num("warnings", said == null ? 0 : said.DismissedCount)
                 + "," + Json.Arr("warningsDismissed", listed)
                 + "," + Json.Str("verdict", verdict) + "}";
        }

        /// <summary>How many generated lines sit in front of the snippet.</summary>
        private static int PrologueLines(string prologue)
        {
            if (string.IsNullOrEmpty(prologue)) return 0;
            var count = 0;
            foreach (var c in prologue) if (c == '\n') count++;
            return count;
        }

        /// <summary>
        /// Where this run's inputs came from, for the answer to carry. Set by
        /// BindNeeds, read by Report. Single-threaded by construction: every
        /// operation arrives on the Revit API thread, one at a time.
        /// </summary>
        private static string Note;

        /// <summary>
        /// What the LAST fragment left behind, so the next one can consume it.
        /// D-29's whole design is a filter's provides feeding an action's
        /// needs, and this is the only place that hand-off can live: the
        /// client cannot hold a Revit Element across a wire.
        ///
        /// ONE CHAIN PER CHAT, AND THAT IS THE WHOLE POINT OF THIS TYPE.
        /// It began as a single static dictionary, which is correct exactly as
        /// long as one chat can talk to one Revit at a time - and that is true
        /// today only because HeronLease refuses the second one. So the lease
        /// was load-bearing for a bug rather than for a policy: remove it, let
        /// two chats interleave small read fragments the way the dispatcher's
        /// queue already allows, and chat B's action would bind chat A's
        /// filter output. Two people, one clipboard.
        ///
        /// Nobody would have seen it. Both chats get a plausible answer, both
        /// are told the elements came "from the previous fragment", and the
        /// only wrong thing is WHOSE.
        ///
        /// KEYED BY DOCUMENT TOO, AND DROPPED WHEN IT CHANGES. Elements belong
        /// to the document they were read from. Handing a list from one model
        /// to a fragment running against another is not a slightly wrong
        /// answer - Revit throws, or an id that means one thing in one file
        /// means something else in the other.
        /// </summary>
        private sealed class Chain
        {
            public readonly Dictionary<string, object> Values =
                new Dictionary<string, object>(StringComparer.Ordinal);

            public string Document;         // whose elements these are
            public string By;               // the fragment that left them

            // WHAT THE PRODUCER RAN WITH, and the reason it is here rather
            // than only `By`. Two runs of `select-by-category-name` leave
            // `elements` under the same name and by the same fragment; only
            // the inputs tell "the pipes in this view" from "the walls in
            // another". A consumer checking the fragment name alone would
            // accept either. docs/36.
            public string Inputs;

            public DateTime TouchedUtc;
        }

        private static readonly Dictionary<string, Chain> Chains =
            new Dictionary<string, Chain>(StringComparer.Ordinal);

        /// <summary>
        /// How long a chat's carried values survive without being touched.
        ///
        /// A chain is working state between two fragments of one batch, which
        /// is seconds. This is a housekeeping bound rather than a feature: a
        /// Revit left open for a week must not accumulate a dictionary of
        /// Element ids per chat that ever spoke to it.
        /// </summary>
        private static readonly TimeSpan ChainLifetime = TimeSpan.FromMinutes(30);

        /// <summary>The most chats whose chains are kept at once.</summary>
        private const int MaxChains = 16;

        /// <summary>
        /// This chat's chain, or null.
        ///
        /// NO CLIENT ID MEANS NO CHAIN, DELIBERATELY. A request that does not
        /// say who is asking cannot be given carried values, because there is
        /// no honest answer to "whose were they" - and falling back to a
        /// shared bucket is exactly the bug this type exists to remove. Such a
        /// caller still runs fragments; it just cannot chain them, and the
        /// refusal it gets names what was missing like any other.
        /// </summary>
        /// <summary>
        /// Why the carried values are NOT the ones the caller asked for, or
        /// null when they are.
        ///
        /// THE SHAPE OF AN EXPECTATION is the producer's name, optionally with
        /// the inputs the caller cares about:
        ///
        ///     select-by-categories
        ///     select-by-categories where categories=Pipes, Pipe Fittings
        ///
        /// ONLY THE NAMED INPUTS ARE COMPARED, and the rest are ignored on
        /// purpose. Demanding every value match would make a caller restate
        /// the producer's whole request to consume one list, and it would
        /// refuse a correct hand-over the moment an unrelated default moved.
        /// The caller declares what it cares about; docs/36 s8.
        ///
        /// EVERY REFUSAL NAMES BOTH SIDES. "the chain does not match" sends a
        /// reader to look at the wrong fragment; "you expected X and
        /// select-by-level left Y" is the answer itself.
        /// </summary>
        private static string ChainDisagrees(Chain chain,
                                             Dictionary<string, object> carried,
                                             string expect,
                                             Document target)
        {
            string wantedBy = expect;
            string wantedInputs = null;

            var split = expect.IndexOf(" where ", StringComparison.OrdinalIgnoreCase);
            if (split >= 0)
            {
                wantedBy = expect.Substring(0, split).Trim();
                wantedInputs = expect.Substring(split + 7).Trim();
            }

            // NOTHING CARRIED AT ALL. Said apart from a mismatch because they
            // are different faults: one is "the producer did not run", the
            // other is "a different producer ran".
            if (chain == null || carried == null || carried.Count == 0)
            {
                return Json.Error("chain_empty",
                    "This request expects values left by '" + wantedBy + "', and "
                    + "nothing is carried for this chat on '" + target.Title + "'. "
                    + "Either that fragment has not run yet, it ran against another "
                    + "model, or the chain was reset between them. NOTHING WAS BOUND "
                    + "and no fragment ran.");
            }

            if (!string.Equals(chain.By, wantedBy, StringComparison.OrdinalIgnoreCase))
            {
                return Json.Error("chain_mismatch",
                    "This request expects values left by '" + wantedBy + "', but what "
                    + "is carried was left by '" + (chain.By ?? "(unknown)") + "'"
                    + (string.IsNullOrEmpty(chain.Inputs)
                        ? "" : " running with " + chain.Inputs)
                    + ". Binding them anyway is how a job acts on elements nobody "
                    + "remembers collecting. NOTHING WAS BOUND and no fragment ran.");
            }

            if (!string.IsNullOrEmpty(wantedInputs))
            {
                var actual = chain.Inputs ?? "";

                foreach (var piece in wantedInputs.Split(';'))
                {
                    var want = piece.Trim();
                    if (want.Length == 0) continue;

                    if (!SuppliedCarries(actual, want))
                    {
                        return Json.Error("chain_inputs_differ",
                            "This request expects '" + wantedBy + "' to have run with "
                            + "'" + want + "', and what is carried was produced by it "
                            + "running with " + (actual.Length == 0
                                ? "no caller values at all" : "'" + actual + "'")
                            + ". The right fragment left these, with different inputs - "
                            + "so they are a different set of elements. NOTHING WAS "
                            + "BOUND and no fragment ran.");
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Whether what the producer RAN WITH carries exactly this pair.
        ///
        /// A SEGMENT, NOT A SUBSTRING, and the difference is the whole check.
        /// This was `actual.IndexOf(want, OrdinalIgnoreCase) >= 0`, and a
        /// substring search PASSES the case this refusal exists to stop:
        /// `DescribeSupplied` renders "categories=Pipes; view=Level 10", so
        /// an expectation of `categories=Pipe` is found inside
        /// `categories=Pipes`, and `view=Level 1` inside `view=Level 10`.
        /// Those are different sets of elements - which is this refusal's own
        /// closing sentence. It was loose in the other direction too:
        /// `s=Pipes` is a substring of `categories=Pipes`, so a truncated
        /// NAME passed as well.
        ///
        /// SPLITTING ON ';' IS WHAT THE OTHER SIDE ALREADY DOES. The caller's
        /// `wantedInputs` is split the same way before it gets here, so a
        /// value containing a semicolon is already two expectations rather
        /// than one - the two sides now agree about that rather than
        /// disagreeing quietly. `DescribeSupplied` does not escape a
        /// semicolon in a value either; that is a rendering limit of the
        /// chain and is the same on both sides.
        ///
        /// FRAGMENT-ISSUES section 5b.
        /// </summary>
        private static bool SuppliedCarries(string actual, string want)
        {
            foreach (var piece in (actual ?? "").Split(';'))
            {
                if (string.Equals(piece.Trim(), want, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static Chain ChainFor(string client, bool create)
        {
            if (string.IsNullOrEmpty(client)) return null;

            Sweep();

            Chain chain;
            if (Chains.TryGetValue(client, out chain))
            {
                chain.TouchedUtc = DateTime.UtcNow;
                return chain;
            }

            if (!create) return null;

            chain = new Chain { TouchedUtc = DateTime.UtcNow };
            Chains[client] = chain;
            return chain;
        }

        private static void Forget(string client)
        {
            if (string.IsNullOrEmpty(client)) return;
            Chains.Remove(client);
        }

        /// <summary>
        /// Drop what is stale, and the oldest if there are too many. Element
        /// ids are small, but a dictionary per chat for the life of a Revit
        /// process is still a leak nobody would ever look for.
        /// </summary>
        private static void Sweep()
        {
            var now = DateTime.UtcNow;

            var stale = new List<string>();
            foreach (var entry in Chains)
                if (now - entry.Value.TouchedUtc > ChainLifetime) stale.Add(entry.Key);
            foreach (var key in stale) Chains.Remove(key);

            while (Chains.Count > MaxChains)
            {
                string oldest = null;
                var when = DateTime.MaxValue;
                foreach (var entry in Chains)
                    if (entry.Value.TouchedUtc < when) { when = entry.Value.TouchedUtc; oldest = entry.Key; }
                if (oldest == null) break;
                Chains.Remove(oldest);
            }
        }

        /// <summary>
        /// Turn the declared contract into real values and a typed prologue.
        /// Returns null when every need is bound, or the refusal.
        ///
        /// THE PROLOGUE IS THE COMPILE GATE'S METHOD SIGNATURE, WRITTEN OUT.
        /// tools/check-fragments-compile.py wraps a snippet in a method whose
        /// parameters are its declared needs; this puts the same names in
        /// scope as locals of the same declared types. That is why the two
        /// agree - not because two lists are kept level, but because both are
        /// generated from the one contract.
        ///
        /// IT REFUSES RATHER THAN SUPPLYING AN EMPTY LIST. An action handed
        /// zero elements reports "0 changed" and looks like a success, and a
        /// modeller reads that as "there was nothing to do". Every fragment in
        /// this library was written to distinguish those; the host must not be
        /// the thing that collapses them.
        /// </summary>
        private static string BindNeeds(List<Dictionary<string, string>> needs,
                                        HeronFragmentGlobals globals,
                                        Document target,
                                        UIDocument uidoc,
                                        HashSet<string> bound,
                                        string client,
                                        Dictionary<string, string> supplied,
                                        string expect,
                                        out string prologue,
                                        out string binding)
        {
            prologue = "";
            binding = null;

            // THIS CHAT'S carried values, and no other chat's.
            var chain = ChainFor(client, false);
            var carried = chain != null && chain.Document == target.Title
                ? chain.Values : null;

            // THE EXPECTATION, CHECKED ONCE AND BEFORE ANYTHING BINDS.
            //
            // Once, because a per-need check would say the same thing as many
            // times as the fragment has needs. Before, because the point is to
            // refuse INSTEAD of binding - a check after the fact is the
            // binding note, which this repository already has and which is
            // read by a person afterwards (fragment-proving rule 5). docs/36.
            if (!string.IsNullOrEmpty(expect))
            {
                var wrong = ChainDisagrees(chain, carried, expect, target);
                if (wrong != null) return wrong;
            }

            var lines = new StringBuilder();
            var how = new List<string>();
            var unmet = new List<string>();
            var fromRequest = new List<string>();

            // The selection, read ONCE. It belongs to the document on screen,
            // so it is only a candidate when that is the document being read.
            IList<ElementId> selected = null;
            if (uidoc != null)
            {
                try
                {
                    var active = uidoc.Document;
                    if (active != null && active.Title == target.Title)
                    {
                        var ids = uidoc.Selection.GetElementIds();
                        if (ids != null && ids.Count > 0) selected = new List<ElementId>(ids);
                    }
                }
                catch { selected = null; }
            }

            // HOW MANY NEEDS COULD THE SELECTION ANSWER. This is asked before
            // anything is bound, and it is the reason UNJOIN_GEOMETRY cannot
            // quietly run on one set of elements twice. "first" and "second"
            // are both unbound lists of elements; one selection cannot say
            // which is which, and picking one is a wrong answer that looks
            // exactly like a right one.
            var selectable = 0;
            foreach (var need in needs)
            {
                string nm, ty, src;
                if (!Fields(need, out nm, out ty, out src)) continue;
                if (bound.Contains(nm)) continue;
                if (src == "request") continue;
                // Under the name the CHAIN would carry it by, which is not
                // always the need's own - see Binds. Asking for `targets` here
                // is what made a need the chain could fill look like one only
                // the selection could, and sent it to the selection branch.
                if (carried != null && carried.ContainsKey(Binds(need, nm))) continue;
                if (IsElementList(ty)) selectable++;
            }

            foreach (var need in needs)
            {
                string name, type, source;
                if (!Fields(need, out name, out type, out source))
                {
                    return Json.Error("bad_contract",
                        "A needs entry arrived without a usable name and type. The " +
                        "contract crossing the wire is the fragment's own, so this is a " +
                        "fault in fragment.yaml or in the client that read it, not " +
                        "something the model can answer.");
                }

                // The three the host has always had. Already in scope as
                // fields on the globals object - a prologue line would only
                // shadow them.
                if (name == "doc" || name == "uidoc" || name == "app")
                {
                    bound.Add(name);
                    continue;
                }

                if (!IsIdentifier(name))
                {
                    return Json.Error("bad_contract",
                        "'" + name + "' is not a usable C# name, and this generates code. " +
                        "A need's name becomes a local variable.");
                }

                if (!IsTypeName(type))
                {
                    return Json.Error("bad_contract",
                        "'" + type + "' is not a usable type for need '" + name + "'. " +
                        "A need's declared type is written into generated code, so it is " +
                        "checked here rather than handed to the compiler as-is.");
                }

                // A REQUEST-SOURCED NEED IS THE CALLER'S, NOT THE MODEL'S -
                // a category, a name to match, a distance. Nothing here can
                // invent one, and inventing one is exactly how a job runs
                // against the wrong category and reports success.
                if (source == "request")
                {
                    string givenText;
                    if (supplied == null || !supplied.TryGetValue(name, out givenText))
                    {
                        fromRequest.Add(name + " (" + type + ")");
                        continue;
                    }

                    // A VALUE THAT CANNOT BECOME ITS DECLARED TYPE STOPS THE
                    // RUN. It is the caller's mistake and it is fixable at the
                    // keyboard - a misspelt view name, a distance typed with a
                    // unit on it - so it is named rather than dropped. Falling
                    // back to "unbound" here would report a missing value the
                    // caller can see they supplied.
                    string problem;
                    var given = FromRequest(givenText, type, name, target, selected,
                                            out problem);
                    if (problem != null) return Json.Error("bad_request_value", problem);

                    globals.__heron[name] = given;
                    bound.Add(name);

                    lines.Append(type).Append(" ").Append(name)
                         .Append(" = (").Append(type).Append(")__heron[\"")
                         .Append(name).Append("\"];\n");

                    how.Add(name + " as given" + Size(given));
                    continue;
                }

                object value = null;
                string origin = null;

                // The name the CHAIN carries this by, which is not always the
                // need's own - see Binds. Everything else below stays the
                // need's own name: the variable, the globals key, the prologue.
                var wanted = Binds(need, name);

                // 1. THE CHAIN. What the previous fragment THIS CHAT ran
                //    left behind, under that name, in THIS document.
                if (carried != null && carried.ContainsKey(wanted))
                {
                    value = carried[wanted];
                    origin = "from " + (chain.By ?? "the previous fragment");
                    // NAME THE ALIAS IN THE READ-BACK. A proof is judged on
                    // this line (fragment-proving rule 5), and "targets from
                    // select-by-categories" hides the one thing a reader of a
                    // two-set fragment needs to check - that both roles were
                    // filled from the same set on purpose.
                    if (wanted != name) origin += " as '" + wanted + "'";
                }

                // 2. THE SELECTION, and only when it is unambiguous.
                else if (selected != null && IsElementList(type) && selectable == 1)
                {
                    value = selected;
                    origin = "from the selection";
                }

                // 3. WHAT A CREATOR JUST MADE.
                //
                // EVERY creation fragment leaves its new elements under
                // `created`, and EVERY consumer asks for `elements`. Measured
                // 2026-09-17: 51 proven creation fragments, and not one of
                // them provides `elements`. So "build the case on purpose" -
                // draw the overlapping lines, then look for overlaps - could
                // not be wired up at all. One fragment labelled the box NEW
                // and the next only ever looked for a box marked ITEMS.
                //
                // That is not six fragments blocked, it is the whole strategy
                // for proving anything against a clean model, which is what
                // fragment-proving rule 2 tells you to fall back on.
                //
                // NARROW ON PURPOSE, because a wrong bind here is a confident
                // wrong answer - the thing this file exists to refuse:
                //
                //   * only the name `elements`, which is the one every
                //     consumer uses; nothing else is guessed at
                //   * only an element list, never an id list
                //   * only when nothing else filled it - the chain under its
                //     own name and the Revit selection both win
                //   * NEVER when the contract set `binds` itself. An explicit
                //     alias is the author's decision, and silently overriding
                //     it would hide a typo in a `binds` line behind a bind
                //     that happens to work.
                //
                // The read-back names it, so a proof judged on the binding
                // note (rule 5) still shows where the elements came from.
                else if (name == "elements" && wanted == name && IsElementList(type)
                         && carried != null && carried.ContainsKey("created"))
                {
                    value = carried["created"];
                    origin = "from " + (chain.By ?? "the previous fragment")
                           + " as 'created'";
                }

                if (value == null)
                {
                    // SAY WHICH NAME WAS LOOKED FOR when it is not the need's
                    // own. "targets was never supplied" sends a reader hunting
                    // for a fragment that provides `targets`, and none does -
                    // what is missing is `elements`, and a `binds` line with a
                    // typo in it looks identical until the message says so.
                    unmet.Add(wanted == name
                        ? name + " (" + type + ")"
                        : name + " (" + type + ", filled from '" + wanted + "')");
                    continue;
                }

                var shaped = Shape(value, type, target);
                if (shaped == null)
                {
                    unmet.Add(name + " (" + type + ") - nothing usable survived");
                    continue;
                }

                globals.__heron[name] = shaped;
                bound.Add(name);

                lines.Append(type).Append(" ").Append(name)
                     .Append(" = (").Append(type).Append(")__heron[\"")
                     .Append(name).Append("\"];\n");

                // `value` is what was CARRIED and `shaped` is what survived
                // the revival. Passing both is what lets the note say "2 of
                // 1128" instead of "(2)" - row 75.
                //
                // NAMED, BECAUSE BOTH ARE `object` AND A SWAP COMPILES. Read
                // positionally this line is two identical-looking arguments,
                // and getting them the wrong way round prints "1128 of 2" -
                // a sentence that is wrong in the confident direction.
                // tests/test_binding_note.py asserts this very call, because
                // the test host links the FORMATTER and cannot see this file.
                how.Add(name + " " + origin
                        + Size(shaped: shaped, before: value));
            }

            if (fromRequest.Count > 0)
            {
                // NOT "there is no way to pass these" any more - there is,
                // and saying otherwise sent the reader looking for work that
                // is already done. What is missing now is the value itself.
                return Json.Error("needs_request_values",
                    "'" + string.Join("', '", fromRequest.ToArray()) + "' " +
                    (fromRequest.Count == 1 ? "is a value the CALLER supplies" :
                                              "are values the CALLER supplies") +
                    ", not something the model holds - a view, a category, a name to " +
                    "match, a distance. Say which " +
                    (fromRequest.Count == 1 ? "one is meant" : "ones are meant") +
                    " and this will run; guessing " +
                    (fromRequest.Count == 1 ? "it" : "them") +
                    " is how a job runs against the wrong thing and reports success.");
            }

            if (unmet.Count > 0)
            {
                var why = selected == null
                    ? "Nothing is selected in Revit, and no earlier fragment in this " +
                      "session left a value of that name."
                    : (selectable > 1
                        ? "There is a selection, but this fragment needs " + selectable +
                          " separate sets of elements and one selection cannot say which " +
                          "is which. Run the fragments that produce them first."
                        : "The selection does not fit, and no earlier fragment in this " +
                          "session left a value of that name.");

                return Json.Error("needs_unbound",
                    "Cannot run: '" + string.Join("', '", unmet.ToArray()) +
                    "' " + (unmet.Count == 1 ? "was" : "were") + " never supplied. " + why +
                    " Running anyway would report 0 results, which reads as 'there was " +
                    "nothing to find' rather than 'nobody was asked'.");
            }

            prologue = lines.ToString();
            binding = how.Count == 0 ? null : string.Join("; ", how.ToArray());
            return null;
        }

        /// <summary>
        /// Keep what this fragment left, for the next one. Only the names the
        /// fragment itself produced - anything the host bound is already the
        /// previous step's and re-storing it would make a value look one
        /// fragment fresher than it is.
        /// </summary>
        /// <summary>
        /// The caller's own values for one run, as one comparable line.
        ///
        /// ONLY WHAT THE CALLER TYPED. The document, the selection and the
        /// chain are not in here: they are context, they change under a
        /// conversation, and a consumer comparing them would refuse a correct
        /// hand-over every time the active view moved.
        ///
        /// Sorted by name. A dictionary's order is not a fact about the
        /// request, and two identical requests have to render identically or
        /// the comparison is a coin toss.
        /// </summary>
        private static string DescribeSupplied(Dictionary<string, string> supplied)
        {
            if (supplied == null || supplied.Count == 0) return "";

            var names = new List<string>(supplied.Keys);
            names.Sort(StringComparer.Ordinal);

            var parts = new List<string>();
            foreach (var name in names)
            {
                var value = supplied[name];
                parts.Add(name + "=" + (value == null ? "" : value));
            }

            return string.Join("; ", parts.ToArray());
        }

        private static void Remember(string name, ScriptState<object> state,
                                     Document target, HashSet<string> bound,
                                     string client,
                                     IDictionary<string, object> handedIn,
                                     string ranWith)
        {
            // A caller that did not say who it is gets no chain - see ChainFor.
            var chain = ChainFor(client, true);
            if (chain == null) return;

            if (chain.Document != target.Title) chain.Values.Clear();

            chain.Document = target.Title;
            chain.By = name;
            chain.Inputs = ranWith;

            foreach (var variable in state.Variables)
            {
                object value = null;
                try { value = variable.Value; } catch { continue; }
                if (value == null) continue;

                // Untouched host input - not this fragment's to leave behind.
                // Replaced, and it IS: see StillTheHosts.
                if (bound != null && bound.Contains(variable.Name)
                    && StillTheHosts(handedIn, variable.Name, value)) continue;

                // ELEMENTS ARE KEPT AS IDS. An Element is a handle into an
                // open document and goes stale - a regenerate, an undo, or
                // another job deleting something leaves an object that throws
                // on its next property read. An id can be checked.
                var elements = value as IEnumerable<Element>;
                if (elements != null)
                {
                    var ids = new List<ElementId>();
                    try { foreach (var e in elements) if (e != null) ids.Add(e.Id); }
                    catch { continue; }
                    chain.Values[variable.Name] = ids;
                    continue;
                }

                chain.Values[variable.Name] = value;
            }
        }

        /// <summary>
        /// The stored value, as the declared type wants it - and re-read from
        /// the document, so an element deleted since it was collected is gone
        /// rather than throwing later on a property nobody expected to fail.
        /// </summary>
        /// <summary>
        /// Every view carrying one name. Name throws on a handful of view
        /// kinds rather than returning empty, and one unreadable name must
        /// not stop the search for a view sitting right there.
        /// </summary>
        private static List<View> ViewsNamed(Document doc, string text)
        {
            var found = new List<View>();
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
            {
                var candidate = element as View;
                if (candidate == null) continue;
                string readable;
                try { readable = candidate.Name; } catch { continue; }
                if (string.Equals(readable, text, StringComparison.Ordinal))
                    found.Add(candidate);
            }
            return found;
        }

        /// <summary>
        /// One view, by name, or a refusal that says how to name it uniquely.
        ///
        /// A NAME THAT MATCHES TWICE IS NEVER CHOSEN FROM. Revit lets a floor
        /// plan and a ceiling plan share a name, and the sample model this was
        /// built against does exactly that for nine of its eleven levels - so
        /// this is the common case, not the corner one. Taking the first is a
        /// wrong answer that looks exactly like a right one.
        ///
        /// SO THERE IS A WAY TO SAY WHICH: "FloorPlan: L2". The plain name is
        /// tried FIRST and wins outright when it is unique, which keeps a view
        /// genuinely called "FloorPlan: L2" reachable by its own name. Only
        /// when the plain name cannot decide is the prefix read as a type.
        ///
        /// The refusal lists what is actually there, because a message telling
        /// somebody to be more specific without saying what the choices are
        /// makes them go and look it up.
        /// </summary>
        private static object OneView(Document doc, string text, out string problem)
        {
            problem = null;

            var found = ViewsNamed(doc, text);
            if (found.Count == 1) return found[0];

            var mark = text.IndexOf(':');
            if (mark > 0)
            {
                var kind = text.Substring(0, mark).Trim();
                var rest = text.Substring(mark + 1).Trim();
                if (rest.Length > 0)
                {
                    var narrowed = new List<View>();
                    foreach (var candidate in ViewsNamed(doc, rest))
                    {
                        if (string.Equals(candidate.ViewType.ToString(), kind,
                                          StringComparison.OrdinalIgnoreCase))
                            narrowed.Add(candidate);
                    }
                    if (narrowed.Count == 1) return narrowed[0];
                    if (narrowed.Count > 1)
                    {
                        problem = narrowed.Count + " views in " + doc.Title + " are called \""
                                + rest + "\" and are all " + kind + ", so even the type does "
                                + "not say which is meant. Rename one.";
                        return null;
                    }
                }
            }

            if (found.Count == 0)
            {
                problem = "No view called \"" + text + "\" in " + doc.Title
                        + ". The name has to match the Project Browser exactly, capitals "
                        + "included. Two views may share a name, and then it is written "
                        + "\"FloorPlan: L2\".";
                return null;
            }

            var choices = new List<string>();
            var templates = 0;
            foreach (var view in found)
            {
                if (view.IsTemplate) templates++;
                var option = view.ViewType + ": " + text;
                if (!choices.Contains(option)) choices.Add(option);
            }

            problem = found.Count + " views in " + doc.Title + " are called \"" + text
                    + "\", so the name does not say which one is meant"
                    + (templates > 0
                          ? " (" + templates + " of them "
                            + (templates == 1 ? "is a view template" : "are view templates") + ")"
                          : "")
                    + ". Say which: " + string.Join(", or ", choices.ToArray()) + ".";
            return null;
        }

        /// <summary>
        /// One point, from three numbers in MILLIMETRES.
        ///
        /// THE UNIT IS THE WHOLE DECISION, and it is millimetres because that is
        /// what this library already says everywhere else. `HeronUnits` exists
        /// for one job - millimetres to Revit's internal feet - 59 caller values
        /// across the fragment library are named `...Mm`, and D3 in
        /// NEEDS-CHECKING, called there the single most important line in the
        /// file, is written "200 mm. Not 200 feet".
        ///
        /// IT WAS ALREADY DECIDED, BY THE PEOPLE WHO COULD NOT PASS A POINT.
        /// `array-elements-radial` takes `centreXMm` and `centreYMm`;
        /// `place-detail-item` takes `atXMm` and `atYMm`. Those are points, split
        /// into millimetre scalars because there was no way to send one. The
        /// workaround named the unit; this only writes it down.
        ///
        /// A DIRECTION IS SAFE UNDER THE SAME RULE, and that had to be checked
        /// rather than assumed. Six needs are a direction rather than a
        /// position, and dividing all three components by one number does not
        /// change where a vector points. All six were read: `array-elements`,
        /// `move-to-ray-hit`, `probe-around-elements` and `check-surface-fit`
        /// call `Normalize()`; `check-obstructions` hands it to
        /// `ReferenceIntersector.FindNearest` and `place-family-on-face` uses it
        /// as a facing vector. NONE uses the magnitude. So one rule covers both
        /// and no fragment has to say which kind it wanted.
        ///
        /// THE BOUND IS `HeronUnits.MaxMillimetres` - 100 km. A number past that
        /// is a transcription error or a value that arrived in the wrong unit,
        /// and it is refused rather than converted. That is the same guard the
        /// move path already applies to a distance, applied to a coordinate.
        /// </summary>
        private static XYZ OnePoint(string text, out string problem)
        {
            problem = null;

            var parts = Parts(text);
            if (parts.Count != 3)
            {
                problem = "A point is three numbers in MILLIMETRES, separated by commas - "
                        + "\"0, 0, 0\" or \"5000, 3000, 2800\". \"" + text + "\" has "
                        + parts.Count + " number" + (parts.Count == 1 ? "" : "s") + " in it.";
                return null;
            }

            var ordinates = new double[3];
            for (var index = 0; index < 3; index++)
            {
                double millimetres;
                if (!double.TryParse(parts[index], NumberStyles.Float,
                                     CultureInfo.InvariantCulture, out millimetres))
                {
                    problem = "\"" + parts[index] + "\" is not a number, and a point is three "
                            + "of them in millimetres. Type digits only - 250 or 250.5, not "
                            + "250mm.";
                    return null;
                }

                // NOT A NUMBER AT ALL, AND THE BOUNDS BELOW CANNOT SEE IT.
                // double.TryParse accepts "NaN", "nan" and "NAN" whatever
                // NumberStyles is set to - the culture's NaNSymbol is honoured
                // regardless - and NaN compares FALSE against every bound, so
                // a greater-than / less-than pair passes it straight through
                // to new XYZ(...). Measured 2026-09-20 against the real parse
                // and the real comparison; FRAGMENT-ISSUES section 5b, row 3.
                if (double.IsNaN(millimetres) || double.IsInfinity(millimetres))
                {
                    problem = "\"" + parts[index] + "\" is not a real number, so Heron cannot "
                            + "place a point at it. A value like that usually arrives from a "
                            + "calculation that did not work out. Type digits only - 250 or 250.5.";
                    return null;
                }

                // THE BOUND, ASKED OF THE PART THAT OWNS IT. HeronUnits already
                // refuses NaN, both infinities and anything past 100 km; this
                // used to be a hand-written pair of comparisons beside it, and
                // the two drifted. One guard, one answer.
                if (!HeronUnits.IsUsableMillimetres(millimetres))
                {
                    problem = "\"" + parts[index] + "\" is further than 100 km from the origin, "
                            + "which is not a coordinate in any building. Heron reads a point in "
                            + "MILLIMETRES - a number this size usually means it was typed in "
                            + "another unit, or has a digit too many.";
                    return null;
                }

                ordinates[index] = HeronUnits.MillimetresToFeet(millimetres);
            }

            return new XYZ(ordinates[0], ordinates[1], ordinates[2]);
        }

        /// <summary>
        /// Several points: semicolons BETWEEN points, commas WITHIN one.
        ///
        ///     "0,0,0; 5000,0,0; 5000,3000,0"
        ///
        /// TWO SEPARATORS BECAUSE ONE CANNOT DO IT. Every other list here is
        /// comma-separated, and a comma-separated list of points is ambiguous
        /// the moment it is read: "0,0,0,1000,0,0" is two points only if you
        /// already know they come in threes, and a list with a number missing
        /// then silently becomes a different, valid-looking list. A separator
        /// that cannot express the mistake is worth more than consistency with
        /// the flat lists.
        /// </summary>
        private static List<XYZ> ManyPoints(string text, out string problem)
        {
            problem = null;

            var points = new List<XYZ>();
            foreach (var piece in (text ?? "").Split(';'))
            {
                var trimmed = piece.Trim();
                if (trimmed.Length == 0) continue;

                var point = OnePoint(trimmed, out problem);
                if (point == null) return null;
                points.Add(point);
            }

            if (points.Count == 0)
            {
                problem = "No points were given. Separate them with semicolons and their "
                        + "three millimetre ordinates with commas - "
                        + "\"0,0,0; 5000,0,0; 5000,3000,0\".";
                return null;
            }
            return points;
        }

        /// <summary>
        /// PAIRS of points: a PIPE between pairs, a semicolon between the two
        /// points of one pair, commas within a point.
        ///
        ///     "0,0,0; 5000,0,0 | 5000,0,0; 5000,3000,0"
        ///
        /// A THIRD SEPARATOR, AND ONLY NOW. The refusal that stood here said a
        /// third separator is a decision to make when a second fragment wants
        /// one, rather than on the strength of a single need. It stayed a
        /// refusal for as long as that was honest. `create-line` is the one
        /// fragment, `pointPairs` was the ONLY need in the library that could
        /// not be typed at all, and the owner asked for it by name - so the
        /// decision is made, and the reason is written here rather than left to
        /// be re-derived.
        ///
        /// THE PIPE IS NOT A STYLE CHOICE. It has to be a character that cannot
        /// appear inside a number and has to look nothing like the two already
        /// in use, because the mistake this shape invites is a PAIR boundary
        /// read as a POINT boundary - which draws a valid-looking set of the
        /// wrong lines instead of raising anything.
        ///
        /// EXACTLY TWO POINTS PER PAIR, REFUSED OTHERWISE. Three in a pair is a
        /// mistyped semicolon, and taking the first two would draw one line and
        /// silently drop a point somebody meant to use.
        /// </summary>
        private static List<IList<XYZ>> PointPairs(string text, out string problem)
        {
            problem = null;

            var pairs = new List<IList<XYZ>>();
            foreach (var piece in (text ?? "").Split('|'))
            {
                var trimmed = piece.Trim();
                if (trimmed.Length == 0) continue;

                var points = ManyPoints(trimmed, out problem);
                if (points == null) return null;

                if (points.Count != 2)
                {
                    problem = "\"" + trimmed + "\" has " + points.Count + " point"
                            + (points.Count == 1 ? "" : "s") + " in it, and a line is drawn "
                            + "between TWO. Separate the two ends of one line with a "
                            + "SEMICOLON and one line from the next with a PIPE - "
                            + "\"0,0,0; 5000,0,0 | 0,0,0; 0,5000,0\" is two lines.";
                    return null;
                }
                pairs.Add(points);
            }

            if (pairs.Count == 0)
            {
                problem = "No point pairs were given. One line is two points in millimetres "
                        + "separated by a semicolon - \"0,0,0; 5000,0,0\" - and one line is "
                        + "separated from the next by a pipe.";
                return null;
            }
            return pairs;
        }

        /// <summary>
        /// ONE LINE, from two points: a semicolon between them, commas within.
        ///
        ///     "0,0,0; 5000,0,0"
        ///
        /// NO NEW SPELLING, AND THAT IS THE POINT. This is exactly the inside
        /// of one `PointPairs` pair - the shape settled on 2026-09-14 - and
        /// `PointPairs`' own refusal already calls a pair of points "a line".
        /// A `Line` need was being refused while the parser that reads one sat
        /// four methods above it, so what was missing was a dispatch row and a
        /// bound curve, not a decision about syntax.
        ///
        /// MILLIMETRES, because `OnePoint` converts and everything downstream
        /// is in Revit's internal feet by the time it gets here.
        ///
        /// A LINE OF NO LENGTH IS REFUSED RATHER THAN THROWN. `Line.CreateBound`
        /// raises on two points closer together than Revit's short-curve
        /// tolerance, and an exception out of here is reported as the FRAGMENT
        /// failing - the fragment has not run yet. The tolerance is read off the
        /// application rather than written down, because it is Revit's number
        /// and not this file's.
        /// </summary>
        private static object OneLine(Document doc, string text, out string problem)
        {
            problem = null;

            var points = ManyPoints(text, out problem);
            if (points == null) return null;

            if (points.Count != 2)
            {
                problem = "\"" + text + "\" has " + points.Count + " point"
                        + (points.Count == 1 ? "" : "s") + " in it, and a line is drawn "
                        + "between TWO. Separate the two ends with a SEMICOLON and their "
                        + "three MILLIMETRE ordinates with commas - \"0,0,0; 5000,0,0\".";
                return null;
            }

            return BoundLine(doc, points[0], points[1], text, out problem);
        }

        /// <summary>
        /// SEVERAL LINES - the `PointPairs` spelling exactly, a PIPE between
        /// them, handed back as curves.
        ///
        ///     "0,0,0; 5000,0,0 | 5000,0,0; 5000,3000,0"
        ///
        /// THE SAME PARSER, UNCHANGED, and no second syntax to learn: a caller
        /// who can write `create-line`'s `pointPairs` can write
        /// `place-line-based-family`'s `curves`. Every curve produced is a
        /// straight one, and that is a real limit of this rule rather than an
        /// oversight - an arc needs three points and a fourth question (which
        /// way it bulges), and nothing in the library asks for a list of them.
        /// </summary>
        private static object ManyLines(Document doc, string text, out string problem)
        {
            problem = null;

            var pairs = PointPairs(text, out problem);
            if (pairs == null) return null;

            var curves = new List<Curve>();
            foreach (var pair in pairs)
            {
                var line = BoundLine(doc, pair[0], pair[1], text, out problem) as Curve;
                if (line == null) return null;
                curves.Add(line);
            }
            return curves;
        }

        /// <summary>
        /// Two points to a bound line, or the refusal Revit would have thrown.
        ///
        /// THE DISTANCE IS CHECKED BEFORE THE CALL, not caught after it. Both
        /// answers are a refusal either way; the difference is the sentence. A
        /// caught exception says "Curve length is too small for Revit's
        /// tolerance", which names a tolerance nobody has in millimetres and
        /// does not say which pair of numbers is wrong.
        /// </summary>
        private static object BoundLine(Document doc, XYZ from, XYZ to, string text,
                                        out string problem)
        {
            problem = null;

            var shortest = 0.0;
            try { shortest = doc.Application.ShortCurveTolerance; } catch { }

            var length = from.DistanceTo(to);
            if (length <= shortest)
            {
                problem = "The two ends of a line in \"" + text + "\" are "
                        + HeronUnits.DescribeMillimetres(HeronUnits.FeetToMillimetres(length))
                        + " apart, and Revit will not make a line shorter than "
                        + HeronUnits.DescribeMillimetres(
                              HeronUnits.FeetToMillimetres(shortest))
                        + ". Check the two points are not the same one typed twice.";
                return null;
            }

            return Line.CreateBound(from, to);
        }

        /// <summary>
        /// The graphic overrides a view carries, written the way Revit's own
        /// override dialog puts them.
        ///
        ///     "halftone=true; transparency=50; projection-line-colour=255,0,0"
        ///
        /// SEMICOLONS BETWEEN SETTINGS, because a colour is already three
        /// comma-separated numbers and a comma cannot do both jobs.
        ///
        /// NINE SETTINGS, NOT TWENTY-FOUR, AND THE SHORT LIST IS THE POINT.
        /// OverrideGraphicSettings carries far more than this; what is here is
        /// what the Visibility/Graphics override dialog puts in front of a
        /// modeller. A property nobody asked for is a property nobody checks,
        /// and an unknown key is refused by NAMING the nine - so a wrong
        /// spelling is one line away from a right one rather than a shrug.
        ///
        /// THE FILL PATTERNS ARE LEFT OUT ON PURPOSE. A pattern is an ELEMENT,
        /// and naming one is a filter's job rather than a string parse - the
        /// same split that keeps finding a FamilySymbol out of
        /// SET_SHEET_TITLE_BLOCK. Their COLOURS are here, because a colour is
        /// three numbers and nothing else.
        ///
        /// AMERICAN SPELLING IS ACCEPTED WHEREVER BRITISH IS. The Revit API
        /// spells it one way and this repository the other, and refusing over
        /// that would be the tool being right about nothing.
        /// </summary>
        private static object OneOverride(string text, out string problem)
        {
            problem = null;

            var settings = new OverrideGraphicSettings();
            var given = 0;

            foreach (var piece in (text ?? "").Split(';'))
            {
                var trimmed = piece.Trim();
                if (trimmed.Length == 0) continue;

                var split = trimmed.IndexOf('=');
                if (split <= 0)
                {
                    problem = "\"" + trimmed + "\" is not a setting. Each one is a name, an "
                            + "equals sign and a value - \"halftone=true\" - and several are "
                            + "separated with semicolons.";
                    return null;
                }

                var key = trimmed.Substring(0, split).Trim().ToLowerInvariant()
                                 .Replace("color", "colour").Replace(" ", "-");
                var value = trimmed.Substring(split + 1).Trim();
                given++;

                if (key == "halftone")
                {
                    bool flag;
                    if (!bool.TryParse(value, out flag))
                    {
                        problem = "\"" + value + "\" is not true or false, and halftone is "
                                + "one or the other.";
                        return null;
                    }
                    settings.SetHalftone(flag);
                    continue;
                }

                if (key == "transparency" || key == "surface-transparency")
                {
                    int percent;
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                      out percent) || percent < 0 || percent > 100)
                    {
                        problem = "\"" + value + "\" is not a percentage from 0 to 100, which "
                                + "is what Revit's surface transparency is.";
                        return null;
                    }
                    settings.SetSurfaceTransparency(percent);
                    continue;
                }

                if (key == "projection-line-weight" || key == "cut-line-weight")
                {
                    int weight;
                    if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                      out weight) || weight < 1 || weight > 16)
                    {
                        problem = "\"" + value + "\" is not a line weight. Revit's are whole "
                                + "numbers from 1 to 16.";
                        return null;
                    }
                    if (key == "projection-line-weight") settings.SetProjectionLineWeight(weight);
                    else settings.SetCutLineWeight(weight);
                    continue;
                }

                if (key == "detail-level")
                {
                    var wantedLevel = value.Trim().ToLowerInvariant();
                    if (wantedLevel == "coarse") settings.SetDetailLevel(ViewDetailLevel.Coarse);
                    else if (wantedLevel == "medium") settings.SetDetailLevel(ViewDetailLevel.Medium);
                    else if (wantedLevel == "fine") settings.SetDetailLevel(ViewDetailLevel.Fine);
                    else
                    {
                        problem = "\"" + value + "\" is not a detail level. Revit's are "
                                + "Coarse, Medium and Fine.";
                        return null;
                    }
                    continue;
                }

                if (key == "projection-line-colour" || key == "cut-line-colour"
                    || key == "surface-colour" || key == "cut-colour")
                {
                    var colour = OneColour(value, out problem) as Color;
                    if (colour == null) return null;
                    if (key == "projection-line-colour") settings.SetProjectionLineColor(colour);
                    else if (key == "cut-line-colour") settings.SetCutLineColor(colour);
                    else if (key == "surface-colour") settings.SetSurfaceForegroundPatternColor(colour);
                    else settings.SetCutForegroundPatternColor(colour);
                    continue;
                }

                problem = "\"" + key + "\" is not a graphic override Heron can set. It takes "
                        + "halftone, transparency, detail-level, projection-line-colour, "
                        + "cut-line-colour, surface-colour, cut-colour, "
                        + "projection-line-weight and cut-line-weight - separated with "
                        + "semicolons, like \"halftone=true; transparency=50\".";
                return null;
            }

            if (given == 0)
            {
                problem = "No overrides were given, and an empty set of them would leave the "
                        + "view exactly as it is while reporting that it had been changed. "
                        + "Name at least one - \"halftone=true\".";
                return null;
            }
            return settings;
        }

        /// <summary>
        /// A TABLE OF VALUES BY NAME, written the way the overrides above are.
        ///
        ///     "Walls=150; Structural Framing=50"    a clearance per category
        ///     "view=*-Mech*; sheet=A-*"             a name pattern per kind
        ///
        /// SEMICOLONS BETWEEN ENTRIES, an equals sign inside each - the same
        /// shape as `OneOverride` and for the same reason: one separator has to
        /// survive a value that already contains commas.
        ///
        /// TWO NEEDS IN THE WHOLE LIBRARY, AND BOTH WERE UNRUNNABLE.
        /// `check-minimum-clearance.rules` is `IDictionary&lt;string, double&gt;`
        /// and `check-model-standards.namePatterns` is
        /// `IDictionary&lt;string, string&gt;`; `FromRequest` refused `IDictionary`
        /// by name and neither could be omitted, so between the two refusals
        /// neither fragment had ever executed a line. Measured 2026-09-15 on
        /// Project1 both ways - `[needs_request_values]` without the value and
        /// `[bad_request_value]` with one. FRAGMENT-ISSUES row 98.
        ///
        /// THE KEY IS KEPT EXACTLY AS TYPED, which is the one place this must
        /// NOT copy `OneOverride`. That method lower-cases its keys and turns
        /// spaces into hyphens, because its keys are nine words it defines
        /// itself. These keys are the MODEL'S words - `target.Category.Name`
        /// returns "Structural Framing", capital S, capital F, with the space -
        /// and a normalised key would match nothing and report a clean sweep.
        ///
        /// A NUMBER IS LEFT IN THE UNIT IT WAS TYPED IN. D-71 puts length
        /// conversion in the FRAGMENT, not at this boundary, precisely because
        /// this method cannot know whether a number is a length, an airflow or
        /// a count - and here it does not even know what the KEY means. So 150
        /// arrives as 150 and `check-minimum-clearance` divides by 304.8 itself,
        /// the same line its `defaultClearance` goes through.
        ///
        /// AN EMPTY TABLE IS ALLOWED AND MEANS "NO ENTRIES". Both fragments
        /// document that reading - `defaultClearance` catches whatever the table
        /// does not name, and a section with no pattern reports NOT CHECKED
        /// rather than a pass. Refusing it would make the ordinary case - one
        /// blanket clearance, no per-category exceptions - the one that cannot
        /// be asked for.
        ///
        /// A REPEATED KEY IS REFUSED rather than last-one-wins. Typing `Walls`
        /// twice means one of the two numbers is silently thrown away, and
        /// which one depends on an ordering nobody can see.
        /// </summary>
        private static object NamedValues(string text, bool asNumbers, string need,
                                          out string problem)
        {
            problem = null;

            var numbers = new Dictionary<string, double>();
            var words = new Dictionary<string, string>();
            var shown = asNumbers ? "\"Walls=150; Structural Framing=50\""
                                  : "\"view=*-Mech*; sheet=A-*\"";

            foreach (var piece in (text ?? "").Split(';'))
            {
                var trimmed = piece.Trim();
                if (trimmed.Length == 0) continue;

                var split = trimmed.IndexOf('=');
                if (split <= 0)
                {
                    problem = "\"" + trimmed + "\" is not an entry for '" + need + "'. Each one "
                            + "is a name, an equals sign and a value, and several are separated "
                            + "with semicolons - " + shown + ".";
                    return null;
                }

                // Everything after the FIRST equals sign is the value, so a
                // pattern may contain one.
                var key = trimmed.Substring(0, split).Trim();
                var value = trimmed.Substring(split + 1).Trim();

                if (key.Length == 0)
                {
                    problem = "\"" + trimmed + "\" has a value with nothing named in front of "
                            + "it. Each entry is a name, an equals sign and a value - " + shown
                            + ".";
                    return null;
                }

                if (numbers.ContainsKey(key) || words.ContainsKey(key))
                {
                    problem = "'" + key + "' is named twice in '" + need + "'. One of the two "
                            + "values would be thrown away and which one depends on an order "
                            + "nobody can see, so say it once.";
                    return null;
                }

                if (!asNumbers) { words[key] = value; continue; }

                double number;
                if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture,
                                     out number))
                {
                    problem = "\"" + value + "\" is not a number, and '" + key + "' in '" + need
                            + "' is one. Type digits only - 150 or 150.5, not 150mm.";
                    return null;
                }
                numbers[key] = number;
            }

            return asNumbers ? (object)numbers : (object)words;
        }

        /// <summary>
        /// The KIND of value a parameter holds - a length, a number, a yes/no -
        /// as Revit's own SpecTypeId.
        ///
        /// BY REFLECTION, AND NOT BECAUSE REFLECTION IS PLEASANT. ForgeTypeId
        /// and SpecTypeId arrived at Revit 2021, and this file compiles for 2020
        /// to 2027 from ONE source with no version #if anywhere in it (D-05).
        /// Naming either type here would break the 2020 build outright. The
        /// fragments that want one already declare revit: ["2022", ...], so a
        /// caller on 2020 is never asked for it - and if one is, this refuses in
        /// words rather than the add-in failing to load.
        ///
        /// THE SPEC IS NAMED BY WHAT THE PARAMETER HOLDS, not by an API
        /// identifier. Somebody adding a parameter says "it's a length"; nobody
        /// says "autodesk.spec.aec:length-2.0.0", and asking them to would be
        /// this library doing the opposite of its job.
        ///
        /// TEXT AND YES/NO LIVE ON NESTED CLASSES - SpecTypeId.String.Text and
        /// SpecTypeId.Boolean.YesNo - which is why the table below carries an
        /// owner as well as a member.
        /// </summary>
        private static object OneSpecTypeId(string text, out string problem)
        {
            problem = null;

            var said = (text ?? "").Trim();
            var lowered = said.ToLowerInvariant().Replace(" ", "").Replace("-", "")
                              .Replace("/", "").Replace("_", "");

            string owner = null;
            string member = null;

            if (lowered == "length" || lowered == "distance") member = "Length";
            else if (lowered == "number" || lowered == "decimal") member = "Number";
            else if (lowered == "angle") member = "Angle";
            else if (lowered == "area") member = "Area";
            else if (lowered == "volume") member = "Volume";
            else if (lowered == "currency" || lowered == "cost") member = "Currency";
            else if (lowered == "mass") member = "Mass";
            else if (lowered == "text" || lowered == "string")
            {
                owner = "String"; member = "Text";
            }
            else if (lowered == "yesno" || lowered == "boolean" || lowered == "bool")
            {
                owner = "Boolean"; member = "YesNo";
            }
            else if (lowered == "integer" || lowered == "wholenumber")
            {
                owner = "Int"; member = "Integer";
            }
            else
            {
                problem = "\"" + said + "\" is not a kind of value Heron knows. Say what the "
                        + "parameter HOLDS - Length, Number, Integer, Angle, Area, Volume, "
                        + "Mass, Currency, Text or YesNo.";
                return null;
            }

            try
            {
                var specs = typeof(Document).Assembly.GetType("Autodesk.Revit.DB.SpecTypeId");
                if (specs == null)
                {
                    problem = "This Revit release has no SpecTypeId, so the kind of a value "
                            + "cannot be named here. It arrived at Revit 2021.";
                    return null;
                }

                var holder = specs;
                if (owner != null)
                {
                    holder = specs.GetNestedType(owner, System.Reflection.BindingFlags.Public);
                    if (holder == null)
                    {
                        problem = "This Revit release has no SpecTypeId." + owner + ", so \""
                                + said + "\" cannot be named on it.";
                        return null;
                    }
                }

                var property = holder.GetProperty(member,
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (property == null)
                {
                    problem = "This Revit release has no SpecTypeId"
                            + (owner == null ? "" : "." + owner) + "." + member + ", so \""
                            + said + "\" cannot be named on it.";
                    return null;
                }

                var built = property.GetValue(null, null);
                if (built == null)
                {
                    problem = "Revit returned nothing for SpecTypeId"
                            + (owner == null ? "" : "." + owner) + "." + member + ".";
                    return null;
                }
                return built;
            }
            catch (Exception ex)
            {
                problem = "\"" + said + "\" could not be turned into a Revit value kind: "
                        + ex.Message;
                return null;
            }
        }

        /// <summary>
        /// A value to put INTO a parameter, carrying its own type - Revit's
        /// ParameterValue.
        ///
        ///     "length 2700"   "number 4.5"   "integer 3"
        ///     "text Level 2"  "yesno true"
        ///
        /// THE KIND COMES FIRST BECAUSE THE VALUE CANNOT SAY IT. "2700" is a
        /// length, a count and a price depending on the parameter it is going
        /// into, and a built ParameterValue is an OBJECT with that choice
        /// already made inside it. Guessing from the digits would put a double
        /// into an integer parameter and be refused by Revit - or worse, be
        /// accepted somewhere it means something else.
        ///
        /// AND THIS IS WHERE A LENGTH IS CONVERTED, WHICH IS D-71's EXCEPTION
        /// RATHER THAN A BREACH OF IT. D-71 says a FRAGMENT converts its own
        /// millimetres, because the fragment is what knows which of its values
        /// are lengths. Here the value crosses as a finished object, and
        /// SET_GLOBAL_PARAMETER's contract says so itself - "Already built,
        /// carrying its own type. A length arrives in internal feet." A fragment
        /// handed a DoubleParameterValue cannot tell a length from a count, so
        /// it cannot be the one to convert; the only place that knows is the
        /// line where the caller typed the word "length".
        /// </summary>
        private static object OneParameterValue(string text, out string problem)
        {
            problem = null;

            var said = (text ?? "").Trim();
            var space = said.IndexOf(' ');
            var kind = space > 0 ? said.Substring(0, space).Trim().ToLowerInvariant() : "";
            var rest = space > 0 ? said.Substring(space + 1).Trim() : "";

            if (kind == "text" || kind == "string") return new StringParameterValue(rest);

            if (kind == "yesno" || kind == "boolean" || kind == "bool")
            {
                bool flag;
                if (!bool.TryParse(rest, out flag))
                {
                    problem = "\"" + rest + "\" is not true or false, and a yes/no parameter "
                            + "holds one or the other.";
                    return null;
                }
                return new IntegerParameterValue(flag ? 1 : 0);
            }

            if (kind == "integer" || kind == "whole")
            {
                int whole;
                if (!int.TryParse(rest, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                  out whole))
                {
                    problem = "\"" + rest + "\" is not a whole number. Type digits only - 3, "
                            + "not 3.0 and not 3mm.";
                    return null;
                }
                return new IntegerParameterValue(whole);
            }

            if (kind == "number")
            {
                double number;
                if (!double.TryParse(rest, NumberStyles.Float, CultureInfo.InvariantCulture,
                                     out number))
                {
                    problem = "\"" + rest + "\" is not a number. Type digits only - 4.5, not "
                            + "4.5mm.";
                    return null;
                }

                // NOT A REAL NUMBER, AND THIS BRANCH WAS MISSED THE FIRST TIME.
                // The `length` case below was guarded on 2026-09-20 and this one
                // was not, which a Codex review on PR #219 caught: `number NaN`
                // parses (TryParse honours the culture's NaN symbol whatever
                // NumberStyles says) and went straight into a
                // DoubleParameterValue, so a SET_GLOBAL_PARAMETER request
                // reached GlobalParameter.SetValue with a NaN in it. There is no
                // millimetre bound to apply here - a unitless number may be any
                // finite value - so the check is only that it IS finite.
                // FRAGMENT-ISSUES section 5b, row 3.
                if (double.IsNaN(number) || double.IsInfinity(number))
                {
                    problem = "\"" + rest + "\" is not a real number, so Heron will not put it "
                            + "in a parameter. A value like that usually arrives from a "
                            + "calculation that did not work out. Type digits only - 4.5.";
                    return null;
                }

                return new DoubleParameterValue(number);
            }

            if (kind == "length")
            {
                double millimetres;
                if (!double.TryParse(rest, NumberStyles.Float, CultureInfo.InvariantCulture,
                                     out millimetres))
                {
                    problem = "\"" + rest + "\" is not a number, and a length is one in "
                            + "MILLIMETRES. Type digits only - 2700, not 2700mm.";
                    return null;
                }
                // NOT A NUMBER AT ALL. The same hole as the point parser above
                // and the more dangerous half of it: this value goes into a
                // DoubleParameterValue, so a NaN that gets past here is written
                // to a parameter on somebody's model rather than used for one
                // geometry call. FRAGMENT-ISSUES section 5b, row 3.
                if (double.IsNaN(millimetres) || double.IsInfinity(millimetres))
                {
                    problem = "\"" + rest + "\" is not a real number, so Heron will not put it "
                            + "in a parameter. A value like that usually arrives from a "
                            + "calculation that did not work out. Type digits only - 2700.";
                    return null;
                }

                // THE BOUND, ASKED OF THE PART THAT OWNS IT - see the note in
                // the point parser above.
                if (!HeronUnits.IsUsableMillimetres(millimetres))
                {
                    problem = "\"" + rest + "\" is longer than 100 km, which is not a length "
                            + "in any building. Heron reads a length in MILLIMETRES - a number "
                            + "this size usually means it was typed in another unit.";
                    return null;
                }
                return new DoubleParameterValue(HeronUnits.MillimetresToFeet(millimetres));
            }

            problem = "A parameter value has to say what KIND it is first, because \"" + said
                    + "\" on its own could be a length, a count or a price and those are "
                    + "different things inside Revit. Type one of \"length 2700\" "
                    + "(millimetres), \"number 4.5\", \"integer 3\", \"text Level 2\" or "
                    + "\"yesno true\".";
            return null;
        }

        /// <summary>
        /// Every element of a given CLASS whose name matches, read the way Revit
        /// writes it.
        ///
        /// TWO SPELLINGS, because Revit's own interface uses both: the name on
        /// its own ("Generic - 200mm") and the family and type together ("Basic
        /// Wall: Generic - 200mm"), which is what the Properties palette and
        /// every type selector show. The second exists because the first is not
        /// unique - "Standard" is the type name of a dozen unrelated families in
        /// an ordinary project. Only types have a family, so only they get it.
        ///
        /// WHY THE CLASS IS TESTED IN .NET RATHER THAN GIVEN TO REVIT'S FILTER.
        /// OfClass would be the obvious route and it is not used, because four
        /// of the classes this has to accept are ABSTRACT - HostObjAttributes,
        /// MEPCurveType, FilterElement, ElementType - and which abstract classes
        /// ElementClassFilter accepts is a runtime question this file cannot
        /// answer at compile time on eight releases. IsInstanceOfType is plain
        /// .NET, means exactly the same thing, and cannot throw. The two coarse
        /// collectors below narrow the walk to the right half of the model
        /// first, so the cost is one type test per candidate.
        /// </summary>
        private static List<Element> ElementsNamed(Document doc, Type wanted, string text)
        {
            var collector = typeof(ElementType).IsAssignableFrom(wanted)
                ? new FilteredElementCollector(doc).WhereElementIsElementType()
                : new FilteredElementCollector(doc).WhereElementIsNotElementType();

            var found = new List<Element>();
            foreach (var element in collector)
            {
                if (element == null || !wanted.IsInstanceOfType(element)) continue;

                string readable;
                try { readable = element.Name; } catch { continue; }
                if (string.Equals(readable, text, StringComparison.Ordinal))
                {
                    found.Add(element);
                    continue;
                }

                var type = element as ElementType;
                if (type == null) continue;
                string family;
                try { family = type.FamilyName; } catch { continue; }
                if (!string.IsNullOrEmpty(family)
                    && string.Equals(family + ": " + readable, text, StringComparison.Ordinal))
                    found.Add(element);
            }
            return found;
        }

        /// <summary>
        /// One element of a named class, or a refusal that says how to name it.
        ///
        /// `kind` is what to call it in the message - "wall type", "phase" - and
        /// it is passed rather than derived from the class name because the
        /// reader is a modeller. "No HostObjAttributes called ..." names the
        /// class this file happens to filter on; "No wall, floor, ceiling or
        /// roof type called ..." is the sentence that says what to type next.
        /// </summary>
        private static object OneOfClass(Document doc, Type wanted, string kind,
                                         string text, out string problem)
        {
            problem = null;

            var found = ElementsNamed(doc, wanted, text);
            if (found.Count == 1) return found[0];

            var areTypes = typeof(ElementType).IsAssignableFrom(wanted);

            if (found.Count == 0)
            {
                problem = "No " + kind + " called \"" + text + "\" in " + doc.Title
                        + ". The name has to match what Revit shows, capitals included"
                        + (areTypes
                              ? " - the Properties palette writes it \"Basic Wall: Generic "
                                + "- 200mm\", and either that or just \"Generic - 200mm\" "
                                + "works when the short name is unique."
                              : ".");
                return null;
            }

            var choices = new List<string>();
            foreach (var element in found)
            {
                var type = element as ElementType;
                string family = null;
                if (type != null) { try { family = type.FamilyName; } catch { family = null; } }
                var option = string.IsNullOrEmpty(family) ? text : family + ": " + text;
                if (!choices.Contains(option)) choices.Add(option);
            }

            problem = found.Count + " " + kind + "s in " + doc.Title + " are called \""
                    + text + "\", so the name does not say which one is meant."
                    + (choices.Count > 1
                          ? " Say which: " + string.Join(", or ", choices.ToArray()) + "."
                          : " Rename one.");
            return null;
        }

        /// <summary>
        /// One element, by name, or a refusal that says how to name it.
        ///
        /// THE NEED'S NAME DECIDES WHICH OF TWO THINGS THIS IS, AND THAT IS THE
        /// WHOLE RULE. Fragments declare a need as `Element` meaning two
        /// different things:
        ///
        ///   a TYPE to build with     wallType, floorType, ceilingType,
        ///                            regionType, runType, hostType
        ///   a specific INSTANCE      reference (align to THIS duct), target,
        ///                            source, run, start, host
        ///
        /// The first has a name a modeller already says out loud, and it is the
        /// name Revit itself prints. THE SECOND HAS NO NAME AT ALL - a wall is
        /// not called anything, and Element.Name on an instance returns its
        /// TYPE's name, so "Generic - 200mm" would match every wall in the model
        /// rather than the one that was meant. Searching instances would turn a
        /// missing rule into a WRONG ANSWER, which is the trade this file exists
        /// to refuse.
        ///
        /// So instances are refused, and the refusal says which kind of thing
        /// was asked for. A caller who misspelt a type name and a caller who
        /// wants "that duct there" have different problems, and one message
        /// cannot serve both.
        ///
        /// THIS PARAGRAPH DESCRIBED AN INTENTION UNTIL 2026-09-10, NOT THE CODE.
        /// Nothing refused: the body was one line resolving against every
        /// ElementType in the model, so an instance-meaning need bound a TYPE
        /// and the fragment ran on it. See the body for what draws the line now.
        ///
        /// THE NARROWER DECLARATIONS ARE BETTER, AND ARE NOW AVAILABLE. A
        /// contract saying WallType resolves among wall types alone, where
        /// "Generic - 200mm" is unique; the same name against every element type
        /// in the model may not be. This branch stays for the needs that really
        /// are "some type", and for contracts nobody has narrowed yet.
        /// </summary>
        /// <summary>
        /// Does this text mean "the element I have selected in Revit"?
        ///
        /// ONE WORD, SPELLED A FEW WAYS, AND NOTHING CLEVERER. A modeller typing
        /// this is answering the refusal above, which says SELECT IT IN REVIT -
        /// so the word has to be the obvious one rather than a syntax to learn.
        /// It is compared case-insensitively and trimmed, and that is all: no
        /// prefix matching, because "selection set" is a different thing in
        /// Revit and must not quietly resolve to this.
        /// </summary>
        private static bool IsSelectionWord(string text)
        {
            var trimmed = (text ?? "").Trim();
            return string.Equals(trimmed, "selected", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "selection", StringComparison.OrdinalIgnoreCase)
                || string.Equals(trimmed, "the selected one", StringComparison.OrdinalIgnoreCase);
        }

        private static object OneElement(Document doc, string need, string text,
                                         IList<ElementId> selected, out string problem)
        {
            // THE NEED'S NAME DECIDES, BECAUSE THE TYPE CANNOT. `wallType` and
            // `reference` are both written `Element` in a contract and mean
            // opposite things. Only one of them has a name to resolve, so the
            // name is the only thing that can tell them apart here.
            //
            // WHY AN ALLOWLIST RATHER THAN A LIST OF THE INSTANCE NAMES. A list
            // of `reference, target, host, ...` would silently mis-bind the next
            // contract that says `Element duct` - which is this defect exactly,
            // found 2026-09-10 with all fourteen bare `Element` needs meaning an
            // instance and every one of them answering emptily instead of
            // refusing. Refusing by default means a NEW name is refused rather
            // than guessed at, and a guess here is a confident wrong answer.
            if (!IsTypeNeedName(need))
            {
                // "SELECT IT IN REVIT AND RUN THIS AGAIN" WAS ADVICE WITH NO WAY
                // TO TAKE IT. The refusal below has told callers to select the
                // element since 2026-09-10 and there was no word they could then
                // type to mean it, so THIRTEEN fragments were unreachable on a
                // sentence that read like a workaround. Measured 2026-09-13:
                // align-elements, distribute-along-run, filter-elements-by-type,
                // join-geometry, match-element-type, measure-available-fall,
                // measure-distance, read-ceiling-grid, select-by-host,
                // select-group-members, select-touching, trace-connectivity.
                //
                // `selected` IS THE WORD, AND IT MEANS EXACTLY ONE. Not "the
                // first of them" - a Revit selection has no order this code may
                // rely on, and picking one of three would be a confident wrong
                // answer wearing a right one's clothes, which is the defect this
                // whole function exists to refuse. Zero and many are both
                // refused, and both say how many were found so the caller can
                // see what happened without guessing.
                if (IsSelectionWord(text))
                {
                    problem = null;
                    if (selected == null || selected.Count == 0)
                    {
                        problem = "'" + need + "' means one particular element and \"" + text
                                + "\" means the one selected in Revit - but NOTHING is selected. "
                                + "Click the element in the model and run this again.";
                        return null;
                    }
                    if (selected.Count != 1)
                    {
                        problem = "'" + need + "' means ONE particular element and \"" + text
                                + "\" means the one selected in Revit - but " + selected.Count
                                + " are selected. A Revit selection has no order this can rely "
                                + "on, so taking one of them would be a guess. Select just the "
                                + "one that is meant.";
                        return null;
                    }
                    var picked = doc.GetElement(selected[0]);
                    if (picked == null)
                    {
                        problem = "The selected element is not in " + doc.Title + ".";
                        return null;
                    }
                    return picked;
                }

                problem = "'" + need + "' asks for ONE PARTICULAR ELEMENT in the model, "
                        + "and a typed name cannot say which one. An instance has no name "
                        + "of its own - Element.Name on one returns its TYPE's name, so \""
                        + text + "\" would match every element of that type rather than "
                        + "the one that was meant. SELECT IT IN REVIT and pass \"selected\", "
                        + "or name a TYPE if that is what was meant. (A need named like "
                        + "'wallType' or 'floorType' is a type to build with, and that one "
                        + "is typed by name.)";
                return null;
            }
            return OneOfClass(doc, typeof(ElementType), "element type", text, out problem);
        }

        /// <summary>
        /// A colour, three numbers 0 to 255, comma separated.
        ///
        /// THERE IS NO NAME TO LOOK UP, and that is the whole reason this is
        /// three numbers rather than a word. Revit stores a colour as three
        /// bytes and calls it nothing; accepting "red" would be this file
        /// choosing a red, and somebody else living with it on a drawing they
        /// signed. The same argument as OnePoint's, for the same kind of value.
        /// </summary>
        private static object OneColour(string text, out string problem)
        {
            problem = null;

            var parts = Parts(text);
            if (parts.Count != 3)
            {
                problem = "\"" + text + "\" is not a colour. Type three numbers from 0 to "
                        + "255, comma separated - \"255,0,0\" is red.";
                return null;
            }

            var channels = new byte[3];
            for (int i = 0; i < 3; i++)
            {
                int number;
                if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture,
                                  out number) || number < 0 || number > 255)
                {
                    problem = "\"" + parts[i] + "\" is not a number from 0 to 255, and a "
                            + "colour is three of them - \"255,0,0\" is red.";
                    return null;
                }
                channels[i] = (byte)number;
            }

            return new Color(channels[0], channels[1], channels[2]);
        }

        /// <summary>
        /// An `ElementId`, resolved the way everything else here is: by NAMING
        /// the thing and taking its id.
        ///
        /// A CALLER DOES NOT KNOW AN ID AND SHOULD NOT HAVE TO. Revit shows a
        /// level called "Level 1" and a title block called "A1 metric"; the
        /// number underneath is bookkeeping that changes when the file is copied
        /// and appears in no dialog a modeller opens. So the caller types the
        /// name and this takes `.Id` off whatever came back.
        ///
        /// WHICH ALSO KEEPS THIS FILE CLEAR OF `new ElementId(int)` - the
        /// constructor that became `long` at Revit 2024 and has no version `#if`
        /// anywhere in this add-in (D-05). An id that is never CONSTRUCTED
        /// cannot break on the release where constructing one changed.
        ///
        /// THE NEED'S NAME DECIDES THE CLASS, the same rule and the same reason
        /// as OneElement. `levelId` and `sheetId` are both written `ElementId`
        /// in a contract and mean completely different searches; the declared
        /// type cannot tell them apart and the name can.
        ///
        /// AND A NAME THIS TABLE DOES NOT HOLD IS REFUSED, not resolved against
        /// every element in the model. `elementIds` and `linkedElementIds` mean
        /// *those ones there* - the same case OneElement refuses - and searching
        /// for them by text would turn a missing rule into a confident wrong
        /// answer. Refusing by default means the NEXT contract to declare an id
        /// is refused rather than guessed at.
        /// </summary>
        private static object OneIdNamed(Document doc, string need, string text,
                                         out string problem)
        {
            problem = null;
            var name = need ?? "";

            // "NO ELEMENT" IS A REAL ANSWER FOR AN ID, AND THERE WAS NO WAY TO
            // SAY IT. Four fragments declare cases that turn on
            // `ElementId.InvalidElementId` meaning DELIBERATELY NONE:
            // `cap-open-pipe-ends` (capTypeId - let Revit choose the cap),
            // `create-sheet` (titleblockTypeId - a sheet with no title block),
            // `export-model-to-nwc` (scopeViewId - the WHOLE model rather than
            // one view), and the same shape for setting only one of Phase or
            // Phase Filter. Every one of them was unreachable, because this
            // function tried to resolve the text to an existing named element
            // and `BindNeeds` forbids omitting the value. Found by review on
            // PR #138.
            //
            // BLANK COUNTS, AND ONLY BECAUSE AN OMISSION CANNOT REACH HERE.
            // `BindNeeds` reports a need nobody supplied as unbound and never
            // calls this, so blank text means the caller typed nothing on
            // purpose. `none` is spelled out for the same reason `selected` is:
            // the obvious word, not a syntax to learn.
            var said = (text ?? "").Trim();
            if (said.Length == 0
                || string.Equals(said, "none", StringComparison.OrdinalIgnoreCase)
                || string.Equals(said, "invalid", StringComparison.OrdinalIgnoreCase)
                || string.Equals(said, "InvalidElementId", StringComparison.OrdinalIgnoreCase))
            {
                return ElementId.InvalidElementId;
            }

            Type wanted = null;
            string kind = null;

            if (name == "levelId" || name == "baseLevelId" || name == "topLevelId")
            {
                wanted = typeof(Level); kind = "level";
            }
            else if (name == "phaseId")
            {
                wanted = typeof(Phase); kind = "phase";
            }
            else if (name == "phaseFilterId")
            {
                wanted = typeof(PhaseFilter); kind = "phase filter";
            }
            // A SHEET IS NAMED BY ITS NUMBER, NOT BY ITS TITLE. `OneOfClass`
            // matches `Element.Name`, and on a ViewSheet that is the TITLE -
            // "Ground Floor Services", often shared between sheets and often
            // just "Unnamed". The identifier a modeller says out loud is the
            // SheetNumber: this repository's own utterance is "put the door
            // schedule on sheet A101". Typing A101 found nothing and typing a
            // common title was refused as ambiguous, so sheet-placing fragments
            // were unreachable by the only name anybody uses.
            //
            // NUMBER FIRST, THEN TITLE, AND AMBIGUITY STILL REFUSED. The number
            // is unique in Revit by construction, so it can never be the
            // ambiguous half; falling back to the title keeps the old spelling
            // working for anyone whose sheets are titled uniquely.
            if (name == "sheetId")
            {
                var byNumber = new List<ViewSheet>();
                var byTitle = new List<ViewSheet>();
                foreach (ViewSheet sheet in new FilteredElementCollector(doc)
                             .OfClass(typeof(ViewSheet)).WhereElementIsNotElementType())
                {
                    if (sheet == null) continue;
                    if (string.Equals(sheet.SheetNumber, (text ?? "").Trim(),
                                      StringComparison.OrdinalIgnoreCase))
                        byNumber.Add(sheet);
                    else if (string.Equals(sheet.Name, (text ?? "").Trim(),
                                           StringComparison.OrdinalIgnoreCase))
                        byTitle.Add(sheet);
                }
                var hits = byNumber.Count > 0 ? byNumber : byTitle;
                if (hits.Count == 1) return hits[0].Id;
                if (hits.Count > 1)
                {
                    problem = "\"" + text + "\" is the TITLE of " + hits.Count + " sheets in "
                            + doc.Title + ", so it does not say which one. Use the sheet "
                            + "NUMBER - it is unique.";
                    return null;
                }
                problem = "No sheet numbered or titled \"" + text + "\" in " + doc.Title
                        + ". A sheet is normally named by its NUMBER, like \"A101\".";
                return null;
            }
            else if (name == "planViewId" || name == "scopeViewId"
                     || name == "targetViewId" || name == "templateId")
            {
                wanted = typeof(View); kind = "view";
            }
            else if (name == "viewFamilyTypeId")
            {
                wanted = typeof(ViewFamilyType); kind = "view family type";
            }
            else if (name == "revisionId" || name == "revisionIds")
            {
                wanted = typeof(Revision); kind = "revision";
            }
            else if (name == "textTypeId")
            {
                wanted = typeof(TextNoteType); kind = "text type";
            }
            else if (name == "titleblockTypeId" || name == "tagTypeId"
                     || name == "tagTypeHintId" || name == "capTypeId")
            {
                wanted = typeof(FamilySymbol); kind = "family type";
            }
            else if (name == "newTypeId" || name == "insulationTypeId")
            {
                wanted = typeof(ElementType); kind = "element type";
            }

            if (wanted != null)
            {
                // A VIEW HAS ITS OWN LOOKUP, AND THE GENERIC ONE CANNOT REACH
                // HALF THE VIEWS IN ANY REAL MODEL. Every level normally carries
                // a floor plan AND a ceiling plan under the SAME name, so
                // OneOfClass finds two, cannot tell them apart, and dead-ends on
                // "Rename one." - which is advice nobody should take about their
                // own model to satisfy a tool.
                //
                // OneView already reads the "FloorPlan: L2" spelling and lists
                // the choices when it still cannot decide. These four needs were
                // simply never sent to it, because they are declared ElementId
                // and the View branch is keyed on the declared type.
                //
                // MEASURED 2026-09-17 on Snowdon Towers Sample HVAC: all eleven
                // plan names are duplicated, so `place-rooms` could not be run
                // at all - every level it was pointed at refused.
                if (wanted == typeof(View))
                {
                    var view = OneView(doc, text, out problem) as Element;
                    return view == null ? null : view.Id;
                }

                var found = OneOfClass(doc, wanted, kind, text, out problem) as Element;
                return found == null ? null : found.Id;
            }

            // A CATEGORY IS NOT AN ELEMENT and has its own lookup, which already
            // handles both the Visibility/Graphics spelling and the API one.
            if (name == "categoryId" || name == "categoryIds")
            {
                var single = OneCategory(doc, text);
                if (single != null) return single.Id;
                problem = "No category called \"" + text + "\" in " + doc.Title + ".";
                return null;
            }

            problem = "'" + need + "' is an id, and Heron resolves one by NAMING the thing "
                    + "it belongs to - a level, a sheet, a view, a type. There is no rule "
                    + "for this name yet, so it refuses rather than searching every element "
                    + "in " + doc.Title + " and binding whatever happened to match \""
                    + text + "\". If it means one particular element, SELECT IT IN REVIT "
                    + "and the contract should ask for the element rather than its id.";
            return null;
        }

        /// <summary>
        /// Does this need-name mean a TYPE to build with, rather than one
        /// particular element in the model?
        ///
        /// Both are declared `Element`, so the name is all there is to go on -
        /// see OneElement for why that is a refusal rather than a lookup.
        /// </summary>
        private static bool IsTypeNeedName(string need)
        {
            return (need ?? "").EndsWith("Type", StringComparison.Ordinal);
        }

        /// <summary>One level, by name, on the same rule as a view.</summary>
        private static object OneLevel(Document doc, string text, out string problem)
        {
            problem = null;
            var found = new List<Level>();
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Level)))
            {
                var candidate = element as Level;
                if (candidate == null) continue;
                string readable;
                try { readable = candidate.Name; } catch { continue; }
                if (string.Equals(readable, text, StringComparison.Ordinal))
                    found.Add(candidate);
            }

            if (found.Count == 1) return found[0];
            if (found.Count == 0)
            {
                problem = "No level called \"" + text + "\" in " + doc.Title
                        + ". The name has to match the Project Browser exactly.";
                return null;
            }
            problem = found.Count + " levels in " + doc.Title + " are called \"" + text
                    + "\". A level name has to be unique to be usable as one.";
            return null;
        }

        /// <summary>
        /// One category name to its BuiltInCategory, WITHOUT touching ElementId.
        ///
        /// The obvious route - read the Category's Id and cast it - crosses the
        /// 32-to-64-bit ElementId change at Revit 2024, and nothing in this
        /// add-in carries a version #if today. Matching the enum by name, then
        /// by the display name Revit gives it, reaches the same answer and
        /// compiles identically on all eight releases.
        /// </summary>
        private static bool OneBuiltInCategory(Document doc, string text, out BuiltInCategory found)
        {
            found = BuiltInCategory.INVALID;
            var trimmed = (text ?? "").Trim();
            if (trimmed.Length == 0) return false;

            foreach (var attempt in new[] { trimmed, "OST_" + trimmed })
            {
                try
                {
                    found = (BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), attempt, true);
                    return true;
                }
                catch { }
            }

            // What a modeller actually types - "Ducts", "Mechanical Equipment".
            foreach (Category category in doc.Settings.Categories)
            {
                if (category == null) continue;
                string readable;
                try { readable = category.Name; } catch { continue; }
                if (!string.Equals(readable, trimmed, StringComparison.OrdinalIgnoreCase)) continue;

                foreach (BuiltInCategory candidate in Enum.GetValues(typeof(BuiltInCategory)))
                {
                    Category resolved = null;
                    try { resolved = Category.GetCategory(doc, candidate); } catch { }
                    if (resolved == null) continue;
                    string other;
                    try { other = resolved.Name; } catch { continue; }
                    if (string.Equals(other, readable, StringComparison.Ordinal))
                    {
                        found = candidate;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>One category object, by the name Revit shows.</summary>
        private static Category OneCategory(Document doc, string text)
        {
            var trimmed = (text ?? "").Trim();
            foreach (Category category in doc.Settings.Categories)
            {
                if (category == null) continue;
                string readable;
                try { readable = category.Name; } catch { continue; }
                if (string.Equals(readable, trimmed, StringComparison.OrdinalIgnoreCase))
                    return category;
            }
            return null;
        }

        /// <summary>
        /// A SET OF ELEMENTS, NAMED BY CATEGORY - the second set, and in this
        /// library it is only ever the second set.
        ///
        /// ALL THREE CUSTOMERS WERE READ BEFORE THIS WAS WRITTEN, and all three
        /// are the same shape: a first set that arrives down the chain and a
        /// second the caller names. `check-room-mep-completeness` takes
        /// `elements` (the rooms) and `devices`; `connect-air-terminals` takes
        /// `elements` (the terminals) and `ducts`; `propose-mep-openings` takes
        /// `elements` (the services) and `hosts`. Every one of the three says so
        /// in its own contract comment - "handing them in keeps the question of
        /// which model they come from with whoever knows the answer" - and
        /// `propose-mep-openings` names the precedent outright: the same shape
        /// FIND_CLASHES uses for its second set.
        ///
        /// SO IT IS A CATEGORY NAME, WHICH IS HOW THE FIRST SET IS FOUND.
        /// `select-by-category-name` is step one of the setup chain everything
        /// here is proved with; it matches `doc.Settings.Categories` by name and
        /// collects what is not a type. This does the same lookup - `OneCategory`,
        /// already used by the `Category` branches - so the two sets are named
        /// the same way and a reader has one rule to hold rather than two.
        ///
        /// SEVERAL CATEGORIES, COMMA SEPARATED, because a second set is often
        /// more than one - "Ducts, Duct Fittings" - and every other list here is
        /// already comma separated. An element named twice is counted once: two
        /// overlapping category names are a typing slip, not an instruction to
        /// hand the same wall over twice.
        ///
        /// THE WHOLE MODEL, NOT A VIEW, AND THAT HAS TO BE SAID OUT LOUD. There
        /// is no view in scope here - this method is handed the document and the
        /// selection and nothing else - so "Walls" means every wall in the file.
        /// For a second set that is usually what is wanted, and the read-back
        /// prints the count beside the name so an unexpectedly large one is
        /// visible before anybody reads the result.
        ///
        /// A NAME THAT IS NOT A CATEGORY IS REFUSED; A CATEGORY THAT IS EMPTY IS
        /// NOT. Those are different events. "Duct" is a mistyped "Ducts" and
        /// nothing downstream can recover from it, so it stops here. "Ducts" in
        /// a model with no ducts is a real and useful answer - it is half of
        /// every negative case this second set has - so it contributes nothing
        /// and says nothing.
        ///
        /// AND `selected` IS REFUSED HERE, WHERE A LIST OF IDS ACCEPTS IT.
        /// Narrower on purpose, the same way `View3D` is narrower than `View`.
        /// The first set of all three of these fragments arrives from the
        /// selection, so the word would hand the identical elements to both
        /// roles - which is exactly the arrangement `generate-jobs.py` already
        /// marks as unarrangeable for `unjoin-geometry` and `switch-join-order`,
        /// and it produces a confident answer that means nothing at all.
        /// </summary>
        private static object ManyByCategory(Document doc, string need, string text,
                                             out string problem)
        {
            problem = null;

            if (IsSelectionWord(text))
            {
                problem = "\"" + (text ?? "").Trim() + "\" cannot fill '" + need + "'. This "
                        + "is the SECOND set, and the first one is what is selected in Revit "
                        + "- so the word would hand the same elements to both, and the answer "
                        + "would mean nothing. Name a category instead - \"Ducts\", or "
                        + "\"Ducts, Duct Fittings\" for more than one.";
                return null;
            }

            var named = Parts(text);
            var elements = new List<Element>();
            var already = new List<ElementId>();

            foreach (var part in named)
            {
                var category = OneCategory(doc, part);
                if (category == null)
                {
                    problem = "No category called \"" + part + "\" in " + doc.Title
                            + ". '" + need + "' is a set of elements named by CATEGORY - "
                            + "type what the Visibility/Graphics list shows, and separate "
                            + "several with commas.";
                    return null;
                }

                try
                {
                    foreach (var element in new FilteredElementCollector(doc)
                                 .WhereElementIsNotElementType()
                                 .OfCategoryId(category.Id))
                    {
                        if (element == null) continue;
                        if (already.Contains(element.Id)) continue;
                        already.Add(element.Id);
                        elements.Add(element);
                    }
                }
                catch { }
            }

            // AN EMPTY SECOND SET IS A VALUE, NOT A MISSING ONE - the same rule
            // the id list already applies, and for a stronger reason here. "No
            // ducts to tap into" and "no devices to look for" are the negative
            // case these three fragments are proved with, and refusing it would
            // put that case out of reach. A BLANK cannot be an omission: a need
            // nobody supplied is reported unbound by `BindNeeds` and never
            // reaches this method, so blank text means the caller typed nothing
            // on purpose.
            //
            // TEXT THAT IS NOT BLANK AND NAMES NOTHING IS STILL REFUSED -
            // ",,," is a typing slip rather than a decision, and it must not
            // read as the deliberate empty set above.
            if (named.Count == 0 && !string.IsNullOrWhiteSpace(text))
            {
                problem = "Nothing was named for '" + need + "'. Name a category - "
                        + "\"Ducts\" - and separate several with commas.";
                return null;
            }
            return elements;
        }

        /// <summary>
        /// AN ELECTRICAL PANEL, BY ITS OWN PANEL NAME.
        ///
        /// ONE NEED IN THE LIBRARY - `create-electrical-circuit`'s `panel` - and
        /// the class is keyed on the need's NAME for the same reason `OneIdNamed`
        /// keys on it: a `FamilyInstance` is an INSTANCE, and `Element.Name` on
        /// an instance returns its TYPE's name. Resolving a panel that way would
        /// match every panel of that type in the building and bind whichever came
        /// back first, which is the confident wrong answer `OneElement` exists to
        /// refuse.
        ///
        /// A PANEL IS THE EXCEPTION, THE SAME WAY A ROOM IS. `SpatialElement` is
        /// accepted above because a Room's Name is its own Name PARAMETER -
        /// "Office 101", typed by whoever laid it out. A panel's Panel Name is
        /// the same kind of thing: "LP-1", typed by whoever laid out the
        /// distribution, unique on the panel schedule by construction, and the
        /// name the fragment itself reads back on the way out as `PanelName`.
        ///
        /// SEARCHED BY THE PARAMETER, NOT BY THE CATEGORY. Every instance
        /// carrying a non-blank Panel Name is a candidate, whatever category it
        /// was modelled in - a panel family put in Electrical Fixtures rather
        /// than Electrical Equipment is somebody else's problem, not a reason
        /// this cannot find it.
        ///
        /// BLANK MEANS DELIBERATELY NONE, and the contract says so in its own
        /// words: "optional - empty leaves the circuit unassigned". The fragment
        /// then reports the circuit as unassigned out loud. Same spelling as
        /// `OneIdNamed`'s, and reachable for the same reason - an omission is
        /// reported unbound and never gets here.
        /// </summary>
        private static object OnePanelNamed(Document doc, string need, string text,
                                            out string problem)
        {
            problem = null;

            if (need != "panel")
            {
                problem = "'" + need + "' is one particular family instance, and there is no "
                        + "rule for that name yet. The one instance Heron can name is an "
                        + "electrical PANEL, by its Panel Name - because that is a name the "
                        + "instance carries itself. Everything else written FamilyInstance "
                        + "has only its TYPE's name, which would match every one of them.";
                return null;
            }

            var said = (text ?? "").Trim();
            if (said.Length == 0
                || string.Equals(said, "none", StringComparison.OrdinalIgnoreCase))
            {
                // The circuit is created and left unassigned, which the fragment
                // reports in its own findings. A real state Revit allows.
                return null;
            }

            var found = new List<FamilyInstance>();
            var known = new List<string>();

            foreach (FamilyInstance instance in new FilteredElementCollector(doc)
                         .OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType())
            {
                if (instance == null) continue;

                string carries = null;
                try
                {
                    var parameter = instance.get_Parameter(BuiltInParameter.RBS_ELEC_PANEL_NAME);
                    if (parameter != null) carries = parameter.AsString();
                }
                catch { continue; }

                if (string.IsNullOrEmpty(carries)) continue;
                if (!known.Contains(carries)) known.Add(carries);
                if (string.Equals(carries, said, StringComparison.OrdinalIgnoreCase))
                    found.Add(instance);
            }

            if (found.Count == 1) return found[0];

            if (found.Count == 0)
            {
                known.Sort(StringComparer.OrdinalIgnoreCase);
                problem = "No panel called \"" + said + "\" in " + doc.Title + ". A panel is "
                        + "named by its PANEL NAME - the one on the panel schedule, not the "
                        + "family type"
                        + (known.Count == 0
                              ? ". Nothing in this model carries a Panel Name at all."
                              : ". This model has: " + string.Join(", ", known.ToArray()) + ".")
                        + " Leave it blank to create the circuit unassigned.";
                return null;
            }

            problem = found.Count + " panels in " + doc.Title + " carry the Panel Name \""
                    + said + "\", so it does not say which one is meant. A panel name is "
                    + "meant to be unique - two the same is a fault in the model rather "
                    + "than in what was typed.";
            return null;
        }

        /// <summary>
        /// A list value, split on commas.
        ///
        /// An empty entry is dropped rather than passed on: "Ducts,,Pipes" is a
        /// typing slip, and a blank name resolves to nothing useful in every
        /// branch below.
        /// </summary>
        private static List<string> Parts(string text)
        {
            var parts = new List<string>();
            foreach (var piece in (text ?? "").Split(','))
            {
                var trimmed = piece.Trim();
                if (trimmed.Length > 0) parts.Add(trimmed);
            }
            return parts;
        }

        /// <summary>
        /// Turn one caller-supplied string into the type the contract declares.
        ///
        /// TEXT IS ALL THAT CROSSES THE WIRE, deliberately. The client has no
        /// Document and cannot resolve a view name, so resolution belongs where
        /// the model is. It also means the client never has to know which Revit
        /// release it is talking to.
        ///
        /// WHAT IS DELIBERATELY NOT HERE, and why - because an absent type
        /// looks identical to an overlooked one:
        ///
        ///   IList&lt;Reference&gt;   A Reference is a FACE: a particular solid, on a
        ///                  particular element, seen in a particular view,
        ///                  produced by a mouse coming to rest on geometry.
        ///                  There is no text that names one - not a name, not a
        ///                  number, not a coordinate - so this is not a rule
        ///                  waiting to be written. THE KEYBOARD CANNOT SAY IT.
        ///                  `place-family-on-face` needs Revit's own picking,
        ///                  which is a different mechanism from anything here.
        ///                  D-72.
        ///
        ///   IDictionary    one blank holds one value, and a pair needs two.
        ///
        /// Both are refused BY NAME below, saying so.
        ///
        /// AND FOUR THAT USED TO BE ON THIS LIST, left here because a list that
        /// only ever gets shorter is how a claim disappears unnoticed:
        ///
        ///   XYZ            WAS here, until the unit was settled: a point is
        ///                  three numbers in MILLIMETRES. See OnePoint. PAIRS of
        ///                  points joined it on 2026-09-14 - see PointPairs, and
        ///                  the pipe that separates them.
        ///
        ///   ElementId      its constructor changed from int to long at Revit
        ///                  2024. Resolved by NAMING the thing it belongs to and
        ///                  taking `.Id`, so the constructor is never reached;
        ///                  and a LIST of them also takes the word `selected`.
        ///
        ///   OverrideGraphicSettings, ForgeTypeId, ParameterValue
        ///                  objects with no name to look up, so each is BUILT
        ///                  from what was typed. Added 2026-09-14 at the owner's
        ///                  request - see OneOverride, OneSpecTypeId and
        ///                  OneParameterValue. D-72.
        ///
        /// AND ONE THAT IS HALF HERE. `Element` resolves to an element TYPE by
        /// name and refuses a specific INSTANCE, because an instance has no name
        /// of its own - see OneElement. That boundary is a refusal rather than a
        /// gap: "which duct" is a question a text value cannot answer, and the
        /// mechanism that can answer it is the selection, not this method.
        ///
        /// Returns null with `problem` set. Every message says what to type
        /// instead, because every one of these is fixable at the keyboard.
        /// </summary>
        private static object FromRequest(string text, string type, string need,
                                          Document doc, IList<ElementId> selected,
                                          out string problem)
        {
            problem = null;
            var wanted = (type ?? "").Replace(" ", "");

            if (wanted == "string" || wanted == "String") return text;

            if (wanted == "int" || wanted == "Int32")
            {
                int whole;
                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                 out whole))
                    return whole;
                problem = "\"" + text + "\" is not a whole number, and this value is declared "
                        + "as one. Type digits only - 250, not 250mm.";
                return null;
            }

            if (wanted == "double" || wanted == "Double")
            {
                double number;
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture,
                                    out number))
                    return number;
                problem = "\"" + text + "\" is not a number, and this value is declared as "
                        + "one. Type digits only - 250 or 250.5, not 250mm.";
                return null;
            }

            if (wanted == "bool" || wanted == "Boolean")
            {
                bool flag;
                if (bool.TryParse(text, out flag)) return flag;
                problem = "\"" + text + "\" is not true or false, and this value is declared "
                        + "as one.";
                return null;
            }

            if (wanted == "View") return OneView(doc, text, out problem);
            if (wanted == "Level") return OneLevel(doc, text, out problem);
            if (wanted == "Element")
                return OneElement(doc, need, text, selected, out problem);

            // A POINT, IN MILLIMETRES. See OnePoint for why that unit and
            // why a direction needs no separate rule.
            if (wanted == "XYZ") return OnePoint(text, out problem);
            if (wanted == "IList<XYZ>" || wanted == "List<XYZ>"
                || wanted == "ICollection<XYZ>" || wanted == "IEnumerable<XYZ>")
                return ManyPoints(text, out problem);

            // PAIRS OF POINTS - one need in the whole library (`create-line`'s
            // `pointPairs`) and, until 2026-09-14, the only need that could not
            // be typed at all. A pipe between pairs; see PointPairs for why a
            // third separator was held back until a fragment actually wanted it.
            if (wanted == "IList<IList<XYZ>>" || wanted == "List<IList<XYZ>>"
                || wanted == "IList<List<XYZ>>" || wanted == "List<List<XYZ>>"
                || wanted == "ICollection<IList<XYZ>>" || wanted == "IEnumerable<IList<XYZ>>")
                return PointPairs(text, out problem);

            // A LINE, AND A LIST OF CURVES - the pair parser above, one layer
            // on. Both were refused while the thing that reads them sat here
            // already: `PointPairs`' own message calls two points "a line", and
            // a `Line` need was told there was no way to write it. `rotate-
            // elements-about-axis` wants an axis and `place-line-based-family`
            // wants the curves to lay a family along, and neither had ever run.
            //
            // THE DIMENSION FRAGMENTS DO NOT COME BACK WITH THIS, and saying so
            // here is the point of the sentence. `create-linear-dimension`
            // declares a `Line` AND an `IList<Reference>`; the second is a FACE
            // and D-72 settled that a keyboard cannot say one. It stays blocked,
            // and this rule must not be sold as having freed it.
            if (wanted == "Line") return OneLine(doc, text, out problem);
            if (wanted == "IList<Curve>" || wanted == "List<Curve>"
                || wanted == "ICollection<Curve>" || wanted == "IEnumerable<Curve>")
                return ManyLines(doc, text, out problem);

            // A TABLE BY NAME - the two request-sourced dictionaries in the
            // library, and the reason both fragments that own one had never run
            // a line. See NamedValues for the shape and for why the key is kept
            // exactly as typed while an override's key is not.
            if (wanted == "IDictionary<string,double>" || wanted == "Dictionary<string,double>"
                || wanted == "IDictionary<String,Double>")
                return NamedValues(text, true, need, out problem);
            if (wanted == "IDictionary<string,string>" || wanted == "Dictionary<string,string>"
                || wanted == "IDictionary<String,String>")
                return NamedValues(text, false, need, out problem);

            // THE NARROWED ONES. Each row is a deliberate act of declaring a
            // type receivable, and the list is short because it is exactly the
            // set some contract actually asks for - not everything that could
            // in principle be looked up by name.
            //
            // WHY typeof(...) AND NOT A STRING. These are compiled on all eight
            // releases, so a class that does not exist on one of them is a build
            // failure here rather than a refusal in front of a model. A
            // reflection lookup by name would compile everywhere and fail
            // nowhere until it mattered.
            //
            // THREE OF THEM ARE DELIBERATELY A BASE CLASS, because the fragment
            // asking is deliberately polymorphic and narrowing further would
            // break it:
            //
            //   HostObjAttributes  CREATE_FROM_ROOM_BOUNDARIES branches on
            //                      `hostType is CeilingType` / `is FloorType`
            //   MEPCurveType       CREATE_ELECTRICAL_RUN builds a cable tray or
            //                      a conduit from the same value
            //   FilterElement      APPLY_VIEW_FILTER says it in its own comment:
            //                      a rule filter and a selection filter share a
            //                      base class and a view does not care which
            //
            // Narrower than Element is the point; narrower than the fragment can
            // use is a regression dressed as precision.
            if (wanted == "WallType")
                return OneOfClass(doc, typeof(WallType), "wall type", text, out problem);
            if (wanted == "FloorType")
                return OneOfClass(doc, typeof(FloorType), "floor type", text, out problem);
            if (wanted == "CeilingType")
                return OneOfClass(doc, typeof(CeilingType), "ceiling type", text, out problem);
            // THE FOURTH SIBLING, AND IT WAS THE MEP CASE ALL OVER AGAIN.
            // `RoofType` derives from `HostObjAttributes`, which this method has
            // accepted since 2026-09-09, so the LOOKUP has always worked - only
            // the string comparison above stood in the way, exactly as it did
            // for `DuctType` and `PipeType` under `MEPCurveType`. `create-roof`
            // declares `RoofType`, fell through to the catch-all, and had never
            // run a line.
            //
            // AND THE CONTRACT MUST NOT BE WIDENED TO SAY `HostObjAttributes`
            // INSTEAD. The declared type becomes the generated variable's
            // STATIC type - `HostObjAttributes roofType = ...` - and
            // `NewFootPrintRoof` takes a `RoofType`, so that edit trades a
            // refusal for a build failure on all eight releases. The resolver
            // row is the missing half; the contract is already right.
            if (wanted == "RoofType")
                return OneOfClass(doc, typeof(RoofType), "roof type", text, out problem);
            if (wanted == "FilledRegionType")
                return OneOfClass(doc, typeof(FilledRegionType), "filled region type",
                                  text, out problem);
            if (wanted == "HostObjAttributes")
                return OneOfClass(doc, typeof(HostObjAttributes),
                                  "wall, floor, ceiling or roof type", text, out problem);
            if (wanted == "MEPCurveType")
                return OneOfClass(doc, typeof(MEPCurveType),
                                  "duct, pipe, cable tray or conduit type", text, out problem);
            if (wanted == "FamilySymbol")
                return OneOfClass(doc, typeof(FamilySymbol), "family type", text, out problem);
            if (wanted == "Phase")
                return OneOfClass(doc, typeof(Phase), "phase", text, out problem);
            if (wanted == "FilterElement")
                return OneOfClass(doc, typeof(FilterElement), "view filter", text, out problem);

            // THE MEP TYPE CLASSES. Six rows, and every one of them was already
            // reachable - `DuctType`, `PipeType` and `FlexDuctType` all derive
            // from `MEPCurveType`, which this file has accepted since 2026-09-09.
            // Only the STRING MATCH stood in the way: the dispatch compares the
            // name a contract declared, so a contract saying `DuctType` fell
            // through to the refusal while the identical object arrived happily
            // under `MEPCurveType`. Found 2026-09-13, on `create-duct`'s first
            // call, and it is FRAGMENT-ISSUES row 28's cheapest half.
            //
            // Fully qualified rather than imported. `Mechanical`, `Plumbing` and
            // `Architecture` each carry names that collide with `DB` on some
            // release - `Space`, `Opening` - and this file compiles on eight.
            if (wanted == "DuctType")
                return OneOfClass(doc, typeof(Autodesk.Revit.DB.Mechanical.DuctType),
                                  "duct type", text, out problem);
            if (wanted == "FlexDuctType")
                return OneOfClass(doc, typeof(Autodesk.Revit.DB.Mechanical.FlexDuctType),
                                  "flexible duct type", text, out problem);
            if (wanted == "MechanicalSystemType")
                return OneOfClass(doc, typeof(Autodesk.Revit.DB.Mechanical.MechanicalSystemType),
                                  "duct system type", text, out problem);
            if (wanted == "PipeType")
                return OneOfClass(doc, typeof(Autodesk.Revit.DB.Plumbing.PipeType),
                                  "pipe type", text, out problem);
            if (wanted == "PipingSystemType")
                return OneOfClass(doc, typeof(Autodesk.Revit.DB.Plumbing.PipingSystemType),
                                  "pipe system type", text, out problem);
            if (wanted == "MEPSystemType")
                return OneOfClass(doc, typeof(MEPSystemType),
                                  "duct or pipe system type", text, out problem);

            // A 3D VIEW BY NAME. Narrower than `View` on purpose: the three
            // fragments that ask for one cast a ray through it, and a floor plan
            // handed to `ReferenceIntersector` is a run that cannot work.
            if (wanted == "View3D")
                return OneOfClass(doc, typeof(View3D), "3D view", text, out problem);

            if (wanted == "Material")
                return OneOfClass(doc, typeof(Material), "material", text, out problem);

            if (wanted == "RevitLinkInstance")
                return OneOfClass(doc, typeof(RevitLinkInstance), "linked model",
                                  text, out problem);

            // A ROOM OR A SPACE BY NAME, AND THIS ONE IS NOT THE INSTANCE TRAP.
            // `Element.Name` on an ordinary instance returns its TYPE's name,
            // which is why OneElement refuses instances outright. A Room's Name
            // is its own Name PARAMETER - "Office 101", typed by whoever laid
            // the room out - so it names one room and not a thousand.
            if (wanted == "SpatialElement")
                return OneOfClass(doc, typeof(SpatialElement), "room or space",
                                  text, out problem);

            // AND A PANEL, BY THE SAME ARGUMENT ONE LAYER ALONG. An electrical
            // panel is a FamilyInstance, so `Element.Name` gives its type's name
            // and would match every panel of that type - but its PANEL NAME is
            // its own, like a room's. The need's name is what says this is the
            // one instance with a name to look up; see OnePanelNamed.
            if (wanted == "FamilyInstance")
                return OnePanelNamed(doc, need, text, out problem);

            // AN ENUM THE CALLER TYPES BY NAME. Only this one: its three values
            // have not moved 2020 to 2027. IFCVersion is deliberately NOT here -
            // its members differ per release, and a name that resolves on 2024
            // and refuses on 2021 is worse than a refusal on both.
            if (wanted == "ViewDuplicateOption")
            {
                foreach (ViewDuplicateOption option in
                         Enum.GetValues(typeof(ViewDuplicateOption)))
                {
                    if (string.Equals(option.ToString(), text,
                                      StringComparison.OrdinalIgnoreCase))
                        return option;
                }
                problem = "\"" + text + "\" is not one of Revit's duplicate options. "
                        + "Type Duplicate, WithDetailing or AsDependent.";
                return null;
            }

            // A COLOUR, THREE NUMBERS 0-255. The same shape as a point and for
            // the same reason - there is no name to look up, and "red" is a
            // preference rather than a value.
            if (wanted == "Color")
                return OneColour(text, out problem);

            // AN ID IS THE THING, NOT A NUMBER. See OneIdNamed.
            if (wanted == "ElementId")
                return OneIdNamed(doc, need, text, out problem);

            if (wanted == "IList<ElementId>" || wanted == "List<ElementId>"
                || wanted == "ICollection<ElementId>" || wanted == "IEnumerable<ElementId>")
            {
                // `selected` MEANS THE WHOLE SELECTION HERE, where for a
                // singular `Element` it means exactly ONE (see OneElement).
                // That is not an inconsistency: the ambiguity OneElement
                // refuses - which of three did you mean - cannot arise when the
                // answer is allowed to be plural.
                //
                // ROW 68 LISTED `filter-elements-by-id` AMONG THE THIRTEEN
                // `selected` UNBLOCKED, AND IT WAS WRONG. The word reached
                // OneElement and never reached here, so `elementIds` fell
                // through to OneIdNamed - which has no rule for that name and
                // correctly refuses. The fragment stayed unreachable for four
                // days while the register said it was not.
                if (IsSelectionWord(text))
                {
                    if (selected == null || selected.Count == 0)
                    {
                        problem = "\"" + (text ?? "").Trim() + "\" means what is selected in "
                                + "Revit, and nothing is selected. Select the elements and run "
                                + "this again.";
                        return null;
                    }
                    return new List<ElementId>(selected);
                }

                var ids = new List<ElementId>();
                foreach (var part in Parts(text))
                {
                    var one = OneIdNamed(doc, need, part, out problem) as ElementId;
                    if (one == null) return null;
                    ids.Add(one);
                }
                // AN EXPLICITLY EMPTY LIST IS A VALUE, NOT A MISSING ONE, and
                // refusing it here blocked a case the fragment itself declares.
                // `set-sheet-revisions/tests/cases.yaml` names "an empty revision
                // list -> nothing changed on any sheet" as a case that must run,
                // and this branch turned it into `bad_request_value` so the
                // fragment never executed. Found by review on PR #138.
                //
                // A BLANK IS DIFFERENT FROM AN OMISSION and only one of them
                // reaches here: `BindNeeds` reports a need nobody supplied as
                // unbound and never calls this at all. So arriving with blank
                // text means the caller TYPED nothing, deliberately.
                if (ids.Count == 0)
                {
                    if (string.IsNullOrWhiteSpace(text)) return ids;
                    problem = "Nothing was named. Separate several with commas.";
                    return null;
                }
                return ids;
            }

            // THE SECOND SET, NAMED BY CATEGORY. The element-shaped twin of the
            // id list above, and it is deliberately NOT the same rule: this one
            // refuses `selected`, because the first set of all three fragments
            // that ask for one already arrives that way. See ManyByCategory.
            if (wanted == "IList<Element>" || wanted == "List<Element>"
                || wanted == "ICollection<Element>" || wanted == "IEnumerable<Element>")
                return ManyByCategory(doc, need, text, out problem);

            if (wanted == "IList<Color>" || wanted == "List<Color>")
            {
                var colours = new List<Color>();
                foreach (var piece in (text ?? "").Split(';'))
                {
                    var trimmed = piece.Trim();
                    if (trimmed.Length == 0) continue;
                    var one = OneColour(trimmed, out problem) as Color;
                    if (one == null) return null;
                    colours.Add(one);
                }
                if (colours.Count == 0)
                {
                    problem = "No colours were given. Separate them with semicolons and "
                            + "their three 0-255 components with commas - "
                            + "\"255,0,0; 0,0,255\".";
                    return null;
                }
                return colours;
            }

            // THE STRUCTURED VALUES. Each is an object a caller cannot name -
            // it has to be BUILT from what they typed - and each was a named
            // refusal until a fragment the owner asked for needed it.
            if (wanted == "OverrideGraphicSettings") return OneOverride(text, out problem);
            if (wanted == "ForgeTypeId") return OneSpecTypeId(text, out problem);
            if (wanted == "ParameterValue") return OneParameterValue(text, out problem);

            if (wanted == "Category")
            {
                var single = OneCategory(doc, text);
                if (single != null) return single;
                problem = "No category called \"" + text + "\" in " + doc.Title + ".";
                return null;
            }

            if (wanted == "BuiltInCategory")
            {
                BuiltInCategory one;
                if (OneBuiltInCategory(doc, text, out one)) return one;
                problem = "\"" + text + "\" is not a category Revit knows. Type what the "
                        + "Visibility/Graphics list shows - Ducts, Mechanical Equipment - "
                        + "or the API name, OST_DuctCurves.";
                return null;
            }

            // ---- lists, comma separated -------------------------------------

            if (wanted == "IList<string>" || wanted == "List<string>"
                || wanted == "ICollection<string>" || wanted == "IEnumerable<string>")
                return Parts(text);

            if (wanted == "IList<int>" || wanted == "List<int>")
            {
                var numbers = new List<int>();
                foreach (var part in Parts(text))
                {
                    int whole;
                    if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture,
                                      out whole))
                    {
                        problem = "\"" + part + "\" is not a whole number, and this is a list "
                                + "of them. Separate them with commas - 1,2,3.";
                        return null;
                    }
                    numbers.Add(whole);
                }
                return numbers;
            }

            if (wanted == "IList<double>" || wanted == "List<double>")
            {
                var numbers = new List<double>();
                foreach (var part in Parts(text))
                {
                    double number;
                    if (!double.TryParse(part, NumberStyles.Float, CultureInfo.InvariantCulture,
                                         out number))
                    {
                        problem = "\"" + part + "\" is not a number, and this is a list of "
                                + "them. Separate them with commas - 100,250.5,400.";
                        return null;
                    }
                    numbers.Add(number);
                }
                return numbers;
            }

            if (wanted == "IList<View>" || wanted == "List<View>"
                || wanted == "ICollection<View>" || wanted == "IEnumerable<View>")
            {
                var views = new List<View>();
                foreach (var part in Parts(text))
                {
                    string trouble;
                    var one = OneView(doc, part, out trouble) as View;
                    if (one == null) { problem = trouble; return null; }
                    views.Add(one);
                }
                if (views.Count == 0)
                {
                    problem = "No views were named. Separate them with commas - "
                            + "\"L2, L3, Model Linking\".";
                    return null;
                }
                return views;
            }

            if (wanted == "IList<BuiltInCategory>" || wanted == "List<BuiltInCategory>"
                || wanted == "ICollection<BuiltInCategory>")
            {
                var categories = new List<BuiltInCategory>();
                foreach (var part in Parts(text))
                {
                    BuiltInCategory one;
                    if (!OneBuiltInCategory(doc, part, out one))
                    {
                        problem = "\"" + part + "\" is not a category Revit knows. Type what "
                                + "Visibility/Graphics shows - Ducts, Mechanical Equipment - "
                                + "and separate several with commas.";
                        return null;
                    }
                    categories.Add(one);
                }
                if (categories.Count == 0)
                {
                    problem = "No categories were named. Separate them with commas - "
                            + "\"Ducts, Duct Fittings\".";
                    return null;
                }
                return categories;
            }

            if (wanted == "IList<Category>" || wanted == "List<Category>"
                || wanted == "ICollection<Category>")
            {
                var categories = new List<Category>();
                foreach (var part in Parts(text))
                {
                    var one = OneCategory(doc, part);
                    if (one == null)
                    {
                        problem = "No category called \"" + part + "\" in " + doc.Title + ".";
                        return null;
                    }
                    categories.Add(one);
                }
                if (categories.Count == 0)
                {
                    problem = "No categories were named. Separate them with commas.";
                    return null;
                }
                return categories;
            }

            // ---- named refusals, so an absent type is not read as an oversight

            // WHAT IS LEFT OF THE OLD POINT REFUSAL. A point, a list of points
            // and a list of PAIRS are all accepted above. Anything nesting them
            // deeper than that is not, and this refusal is kept rather than
            // deleted for the same reason it was written: an absent type must
            // not read as an overlooked one.
            if (wanted.IndexOf("XYZ", StringComparison.Ordinal) >= 0)
            {
                problem = "A point is three numbers in millimetres, a list of points is "
                        + "written \"0,0,0; 5000,0,0\" and pairs of them are separated with "
                        + "a pipe, but \"" + type + "\" nests them deeper than that and "
                        + "there is no way to write it yet.";
                return null;
            }

            // A FACE, AND IT IS NOT A MISSING RULE. Without this the catch-all
            // below ends "this is not one of them yet", and `yet` is exactly
            // wrong: a Reference is produced by a mouse coming to rest on
            // geometry, and no text names one. `place-family-on-face` needs
            // Revit's own picking, which is a different mechanism from anything
            // in this method. D-72.
            if (wanted.IndexOf("Reference", StringComparison.Ordinal) >= 0)
            {
                problem = "A face cannot be typed in, and this is not a rule waiting to be "
                        + "written. Revit identifies a face as a particular solid, on a "
                        + "particular element, seen in a particular view - it is what a mouse "
                        + "lands on, and no name, number or coordinate says which one. This "
                        + "needs Revit's own picking.";
                return null;
            }

            if (wanted.IndexOf("ElementId", StringComparison.Ordinal) >= 0)
            {
                problem = "An element id cannot be typed in yet. Its type changed size at "
                        + "Revit 2024 and this add-in builds for 2020 to 2027 from one source, "
                        + "so accepting one needs that handled deliberately. Name the thing "
                        + "instead, or select it.";
                return null;
            }

            problem = "Heron can be handed a view, a level, a category, an element type - "
                    + "including a wall, floor, ceiling, filled region, MEP curve or family "
                    + "type - a phase, a view filter, a point in millimetres, a name, a "
                    + "number, or true/false, and lists of most of those. \"" + type + "\" is not one of them yet, so "
                    + "this fragment still has no way to receive it.";
            return null;
        }

        /// <summary>
        /// A carried value, turned into the shape the next fragment declared -
        /// or null when there is genuinely nothing usable in it.
        ///
        /// AN EMPTY LIST AND A LIST THAT RESOLVED TO NOTHING ARE DIFFERENT
        /// EVENTS, AND UNTIL 2026-09-14 THIS RETURNED null FOR BOTH. The caller
        /// reports a null as "nothing usable survived", so a fragment whose
        /// input is legitimately empty could never run at all.
        ///
        /// `describe-blank-parameters` is the proof. It needs `blank` AND
        /// `absent`, both left by `read-element-parameters`, and those two are
        /// mutually exclusive by construction: on 22 ducts, asking for
        /// "Comments" gives `blank 22, absent 0`, and asking for a name nothing
        /// carries gives `absent 22, blank 0`. One of the pair is ALWAYS empty,
        /// the empty one was nulled, and the run stopped on it every time.
        ///
        /// THE OTHER CASE IS REAL AND MUST KEEP FAILING. A list that HELD ids
        /// and resolved none of them is the linked-element hazard: a linked
        /// element's id belongs to the link's document, so `doc.GetElement`
        /// finds nothing - or worse, finds an unrelated host element that
        /// happens to share the number. Carrying an empty list there would say
        /// "there were none" about something nobody could look at.
        ///
        /// So: empty IN means empty OUT, and ids that all fail to resolve still
        /// mean null. The distinction Codex asked for in the dispatch on
        /// PR #138, one layer over.
        /// </summary>
        private static object Shape(object value, string type, Document doc)
        {
            var ids = value as IList<ElementId>;

            if (ids != null && IsElementList(type))
            {
                if (ids.Count == 0) return new List<Element>();

                var live = new List<Element>();
                foreach (var id in ids)
                {
                    Element found = null;
                    try { found = doc.GetElement(id); } catch { }
                    if (found != null) live.Add(found);
                }
                // Held ids, resolved none - not the same as having been empty.
                return live.Count == 0 ? null : (object)live;
            }

            if (ids != null && type.IndexOf("ElementId", StringComparison.Ordinal) >= 0)
            {
                return value;
            }

            var already = value as IList<Element>;
            if (already != null && IsElementList(type))
                return value;

            return value;
        }

        // THE RULE LIVES IN HeronBindingNote.cs, LINKED BY SOURCE INTO A
        // TEST HOST. It counts two collections and touches no Autodesk type,
        // so splitting it out is what makes it provable on a machine with no
        // Revit - the same move HeronStackGuard.cs and HeronActivityBanner.cs
        // already make. There is exactly one copy of it.
        private static string Size(object shaped)
        {
            return HeronBindingNote.Size(shaped, null);
        }

        private static string Size(object shaped, object before)
        {
            return HeronBindingNote.Size(shaped, before);
        }

        private static bool Fields(Dictionary<string, string> need,
                                   out string name, out string type, out string source)
        {
            name = null; type = null; source = "host";
            if (need == null) return false;
            if (!need.TryGetValue("name", out name) || string.IsNullOrEmpty(name)) return false;
            if (!need.TryGetValue("type", out type) || string.IsNullOrEmpty(type)) return false;
            string declared;
            if (need.TryGetValue("source", out declared) && !string.IsNullOrEmpty(declared))
                source = declared;
            return true;
        }

        /// <summary>
        /// The PROVIDED name that fills this need, which is not always its own.
        ///
        /// Most needs are filled by a provide of the same name: an action needs
        /// `elements` and a filter provides `elements`. Two fragments want
        /// something that could not say - FIND_NEAREST_ELEMENTS needs TWO sets
        /// of elements, the things to measure FROM and the things to measure
        /// TO, and only one of them can be called `elements`. So a need may
        /// declare `binds: elements`, meaning "fill me from a provide called
        /// elements", and keep a name that says what the set is FOR.
        ///
        /// THE CONTRACT ALREADY SAID THIS AND THIS FILE WAS NOT LISTENING.
        /// `need_binds` in brain/heron_fragment.py has read `binds` since the
        /// key existed - composition, the graph and the job generator all
        /// honour it - and the executor looked the need up under its own name.
        /// Five fragments declare it and none of them could run.
        ///
        /// WHAT THAT ACTUALLY DID IS WORSE THAN A REFUSAL, and it is why this
        /// is a fault rather than a gap. `targets` went unfound in the chain,
        /// fell through to the selection branch as the only unbound list of
        /// elements, and bound WHATEVER WAS SELECTED IN REVIT. Measured
        /// 2026-09-15 against Project1: `elements from select-by-categories
        /// (2); targets from the selection (1)` - the fragment measured two
        /// pipes against one unrelated leftover element and reported a clean
        /// run. A refusal is visible; that is not.
        ///
        /// IT RENAMES, IT DOES NOT CONVERT. The declared type still has to
        /// match what the chain carries; `Shape` is unchanged and a value of
        /// the wrong type is refused exactly as before.
        ///
        /// ONLY THE CHAIN LOOKUP USES THIS. A request-sourced need is named by
        /// the caller at the keyboard - `--set metric=gap` - so it is found
        /// under its OWN name, and heron_fragment.py refuses a contract that
        /// puts `binds` on one. The prologue, the globals key and the variable
        /// the fragment reads are all the need's own name too: `binds` says
        /// where the value comes from, never what it is called once it is here.
        /// </summary>
        private static string Binds(Dictionary<string, string> need, string name)
        {
            string bound;
            if (need != null && need.TryGetValue("binds", out bound)
                && !string.IsNullOrEmpty(bound))
                return bound;
            return name;
        }

        /// <summary>An IList/ICollection/IEnumerable of Element, not of ElementId.</summary>
        private static bool IsElementList(string type)
        {
            if (string.IsNullOrEmpty(type)) return false;
            if (type.IndexOf("ElementId", StringComparison.Ordinal) >= 0) return false;
            return type.IndexOf("<Element>", StringComparison.Ordinal) >= 0;
        }

        /// <summary>
        /// A C# identifier. Checked because a need's name is written into
        /// generated source, and a contract is a file on somebody's disk.
        /// </summary>
        private static bool IsIdentifier(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            if (!char.IsLetter(text[0]) && text[0] != '_') return false;
            foreach (var c in text)
                if (!char.IsLetterOrDigit(c) && c != '_') return false;
            return !Keywords.Contains(text);
        }

        /// <summary>
        /// Names that read perfectly in a contract and cannot be variables.
        /// `params`, `object`, `event`, `base`, `string`, `fixed` and `in` are
        /// all words a Revit contract would reach for without a second
        /// thought. Without this the prologue fails to compile and the error
        /// names a line the fragment did not write, so the fragment gets
        /// blamed for the host's generated code.
        ///
        /// tests/test_fragment_needs.py checks the whole library against the
        /// same list, so a contract like this is caught at the desk. This is
        /// the second line, for a contract that reaches the machine anyway.
        /// </summary>
        private static readonly HashSet<string> Keywords =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "abstract", "as", "base", "bool", "break", "byte", "case",
                "catch", "char", "checked", "class", "const", "continue",
                "decimal", "default", "delegate", "do", "double", "else",
                "enum", "event", "explicit", "extern", "false", "finally",
                "fixed", "float", "for", "foreach", "goto", "if", "implicit",
                "in", "int", "interface", "internal", "is", "lock", "long",
                "namespace", "new", "null", "object", "operator", "out",
                "override", "params", "private", "protected", "public",
                "readonly", "ref", "return", "sbyte", "sealed", "short",
                "sizeof", "stackalloc", "static", "string", "struct", "switch",
                "this", "throw", "true", "try", "typeof", "uint", "ulong",
                "unchecked", "unsafe", "ushort", "using", "virtual", "void",
                "volatile", "while",
            };

        /// <summary>
        /// A type NAME - letters, digits, dots for a namespace, and the
        /// brackets and commas a generic needs. Deliberately narrow: this
        /// string is written into code, so anything it does not recognise is
        /// refused with the type named, rather than reaching the compiler and
        /// coming back as an error about a line the fragment did not write.
        /// </summary>
        private static bool IsTypeName(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (var c in text)
            {
                if (char.IsLetterOrDigit(c)) continue;
                if (c == '_' || c == '.' || c == '<' || c == '>' ||
                    c == ',' || c == ' ' || c == '[' || c == ']') continue;
                return false;
            }
            return char.IsLetter(text[0]) || text[0] == '_';
        }

        /// <summary>
        /// One value, as a line a person can read.
        ///
        /// Deliberately NOT a general serializer. A fragment leaves elements,
        /// ids, dictionaries and counts behind, and a proof needs to see the
        /// SHAPE and the SIZE of each - "184 element(s)" answers the question
        /// a proof asks, where a page of ids does not. Anything that needs the
        /// ids themselves is a later operation with its own contract.
        /// </summary>
        private static string Describe(object value)
        {
            if (value == null) return "(null)";

            if (value is string) return (string)value;
            if (value is bool) return ((bool)value) ? "true" : "false";
            if (value is int || value is long || value is double)
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);

            // A CREATED ELEMENT'S ID AND THE ABSENCE OF ONE MUST NOT RENDER THE
            // SAME WORD. They did, and it cost fifteen fragments. `created` is
            // a bare ElementId on 29 fragments and the ONLY declared result on
            // 15 DRAFT ones - create-sheet, create-floor, create-text-note,
            // create-schedule and the rest - so "ElementId" is what the judge
            // saw for both a creator that built something and one that refused.
            //
            // Measured 2026-09-10 proving the creators against Project1:
            // create-ceiling's NEGATIVE case reported `created ElementId` while
            // its own findings said "A ceiling needs at least three boundary
            // points and 2 were given". Nothing was created; the id was
            // InvalidElementId; the two were indistinguishable in the record.
            //
            // NO IntegerValue AND NO CONSTRUCTOR. IntegerValue is deprecated at
            // 2024 and the constructor changed from int to long there, and
            // nothing in this file carries a version #if. ToString() and
            // InvalidElementId are on every release 2020-2027.
            //
            // "(null)" for the invalid one because heron_validate._as_count
            // already reads that as zero - so the empty case needs no new rule
            // at the other end, and the "N item(s)" shape is the one it already
            // parses for a quantity.
            if (value is ElementId)
            {
                var id = (ElementId)value;
                return id == ElementId.InvalidElementId
                    ? "(null)"
                    : "1 item(s) [id " + id + "]";
            }

            var dictionary = value as IDictionary;
            if (dictionary != null) return dictionary.Count + " entry(ies)";

            var list = value as ICollection;
            if (list != null)
            {
                // The first few, so a proof can see WHAT came back and not
                // only how much. A count alone cannot tell a right answer from
                // a plausible one.
                var sample = new List<string>();
                var taken = 0;
                foreach (var item in list)
                {
                    if (taken++ >= 3) break;
                    sample.Add(ShortName(item));
                }

                return list.Count + " item(s)"
                    + (sample.Count > 0 ? " [" + string.Join(", ", sample) + (list.Count > 3 ? ", ..." : "") + "]" : "");
            }

            return value.GetType().Name;
        }

        private static string ShortName(object item)
        {
            if (item == null) return "(null)";

            var element = item as Element;
            if (element != null)
            {
                var label = "";
                try { label = element.Name; } catch { }
                return string.IsNullOrEmpty(label) ? element.GetType().Name : label;
            }

            var text = Convert.ToString(item, System.Globalization.CultureInfo.InvariantCulture);
            if (text == null) return "(null)";
            return text.Length > 60 ? text.Substring(0, 60) + "..." : text;
        }

        /// <summary>
        /// The exception that actually happened.
        ///
        /// A script failure arrives wrapped, and the outer message is almost
        /// always "Exception has been thrown by the target of an invocation" -
        /// which tells a modeller nothing at all.
        /// </summary>
        private static Exception Innermost(Exception failure)
        {
            while (failure.InnerException != null) failure = failure.InnerException;
            return failure;
        }
    }
}
