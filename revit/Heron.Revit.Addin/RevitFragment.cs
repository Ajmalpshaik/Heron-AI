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
    /// READ ONLY, AND THAT IS A STRUCTURAL GUARANTEE RATHER THAN A PROMISE.
    /// Nothing here opens a transaction. Revit refuses every model change made
    /// outside one, so a fragment run through this path CANNOT alter the
    /// model - the enforcement is Revit's, not a check of ours that could be
    /// forgotten. The write path is a separate operation and belongs in its
    /// own file beside RevitWrite.cs, so that everything able to change a
    /// model stays in one place a reviewer can read end to end.
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

            Document target = null;
            var openTitles = new List<string>();

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

                if (!string.IsNullOrEmpty(wanted)
                    && string.Equals(candidate.Title, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    target = candidate;
                }
            }

            if (!string.IsNullOrEmpty(wanted) && target == null)
            {
                return Json.Error("no_such_document",
                    "No open model called \"" + wanted + "\". Open: "
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
            if (string.Equals(Json.ReadString(request, "chain"), "reset",
                              StringComparison.Ordinal))
            {
                Forget(client);
            }

            var bound = new HashSet<string>(StringComparer.Ordinal)
            {
                "doc", "uidoc", "app"
            };

            var prologue = "";

            if (needs != null)
            {
                string binding;
                var refusal = BindNeeds(needs, globals, target, uidoc, bound, client,
                                        supplied, out prologue, out binding);
                if (refusal != null) return refusal;
                Note = binding;
            }
            else
            {
                Note = null;
            }

            Script<object> script;
            var compileError = Compile(prologue + source, PrologueLines(prologue), out script);
            if (compileError != null) return compileError;

            ScriptState<object> state;
            try
            {
                // The fragments never await, so this completes inline on the
                // Revit API thread - which is where it has to run. If a
                // fragment ever does await, this is the line that will deadlock
                // and the reason will not be obvious.
                state = script.RunAsync(globals).GetAwaiter().GetResult();
            }
            catch (Exception failure)
            {
                // A fragment that throws is a finding, not a crash. The
                // message is Revit's own and is far more use than "it failed".
                return Json.Error("fragment_threw",
                    "'" + name + "' threw while running: " + Innermost(failure).Message);
            }

            Remember(name, state, target, bound, client);
            return Report(name, state, target, uidoc, app.ActiveUIDocument, bound);
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

            var candidate = CSharpScript.Create<object>(source, options, typeof(HeronFragmentGlobals));

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
                                     HashSet<string> bound)
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
                // The set is now whatever was actually bound for this run.
                if (bound != null && bound.Contains(variable.Name)) continue;

                left.Add(Json.Str(variable.Name, Describe(variable.Value)));
            }

            parts.Add("\"provides\":{" + string.Join(",", left) + "}");
            parts.Add(Json.Num("providesCount", left.Count));

            return Json.Ok(parts.ToArray());
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
                                        out string prologue,
                                        out string binding)
        {
            prologue = "";
            binding = null;

            // THIS CHAT'S carried values, and no other chat's.
            var chain = ChainFor(client, false);
            var carried = chain != null && chain.Document == target.Title
                ? chain.Values : null;

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
                if (carried != null && carried.ContainsKey(nm)) continue;
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
                    var given = FromRequest(givenText, type, target, out problem);
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

                // 1. THE CHAIN. What the previous fragment THIS CHAT ran
                //    left behind, under this name, in THIS document.
                if (carried != null && carried.ContainsKey(name))
                {
                    value = carried[name];
                    origin = "from " + (chain.By ?? "the previous fragment");
                }

                // 2. THE SELECTION, and only when it is unambiguous.
                else if (selected != null && IsElementList(type) && selectable == 1)
                {
                    value = selected;
                    origin = "from the selection";
                }

                if (value == null)
                {
                    unmet.Add(name + " (" + type + ")");
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

                how.Add(name + " " + origin + Size(shaped));
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
        private static void Remember(string name, ScriptState<object> state,
                                     Document target, HashSet<string> bound,
                                     string client)
        {
            // A caller that did not say who it is gets no chain - see ChainFor.
            var chain = ChainFor(client, true);
            if (chain == null) return;

            if (chain.Document != target.Title) chain.Values.Clear();

            chain.Document = target.Title;
            chain.By = name;

            foreach (var variable in state.Variables)
            {
                if (bound != null && bound.Contains(variable.Name)) continue;

                object value = null;
                try { value = variable.Value; } catch { continue; }
                if (value == null) continue;

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
        ///   XYZ            a point. The Revit API works in FEET internally and
        ///                  this library talks millimetres; which unit a typed
        ///                  number is in has to be decided, not guessed, and a
        ///                  units error is the one mistake this repository has
        ///                  already written a check for - D3 in NEEDS-CHECKING.
        ///   ElementId      its constructor changed from int to long at Revit
        ///                  2024. Nothing in this add-in carries a version #if,
        ///                  and the first one should not arrive as a side effect
        ///                  of a proving session.
        ///
        /// Both are refused BY NAME below, saying so.
        ///
        /// Returns null with `problem` set. Every message says what to type
        /// instead, because every one of these is fixable at the keyboard.
        /// </summary>
        private static object FromRequest(string text, string type, Document doc,
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

            if (wanted == "XYZ" || wanted.IndexOf("<XYZ>", StringComparison.Ordinal) >= 0)
            {
                problem = "A point cannot be typed in yet. The Revit API works in feet and "
                        + "this library talks millimetres, so which unit the number is in has "
                        + "to be settled before one can be accepted - guessing it is exactly "
                        + "the mistake D3 exists to catch.";
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

            problem = "Heron can be handed a view, a level, a category, a name, a number, or "
                    + "true/false - and lists of those. \"" + type + "\" is not one of them "
                    + "yet, so this fragment still has no way to receive it.";
            return null;
        }

        private static object Shape(object value, string type, Document doc)
        {
            var ids = value as IList<ElementId>;

            if (ids != null && IsElementList(type))
            {
                var live = new List<Element>();
                foreach (var id in ids)
                {
                    Element found = null;
                    try { found = doc.GetElement(id); } catch { }
                    if (found != null) live.Add(found);
                }
                return live.Count == 0 ? null : (object)live;
            }

            if (ids != null && type.IndexOf("ElementId", StringComparison.Ordinal) >= 0)
            {
                return ids.Count == 0 ? null : value;
            }

            var already = value as IList<Element>;
            if (already != null && IsElementList(type))
                return already.Count == 0 ? null : value;

            return value;
        }

        private static string Size(object shaped)
        {
            var list = shaped as ICollection;
            return list == null ? "" : " (" + list.Count + ")";
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

            if (value is ElementId) return "ElementId";

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
