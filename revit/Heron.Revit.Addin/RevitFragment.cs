// Heron-Agent:  HERON-REVIT-CMP-021
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections;
using System.Collections.Generic;
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
            };

            Script<object> script;
            var compileError = Compile(source, out script);
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

            return Report(name, state, target, uidoc, app.ActiveUIDocument);
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
        private static string Compile(string source, out Script<object> script)
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
                return Json.Error("compile_failed",
                    "The fragment did not compile against the assemblies this Revit has loaded: " +
                    string.Join("; ", errors.Take(5)) +
                    (errors.Count > 5 ? " ... and " + (errors.Count - 5) + " more" : ""));
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
                                     Document target, UIDocument uidoc, UIDocument active)
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

            var left = new List<string>();

            foreach (var variable in state.Variables)
            {
                // Skip what the host put in. Those are the `needs`, and
                // echoing a whole Document back is neither useful nor small.
                if (variable.Name == "doc" || variable.Name == "uidoc" || variable.Name == "app")
                    continue;

                left.Add(Json.Str(variable.Name, Describe(variable.Value)));
            }

            parts.Add("\"provides\":{" + string.Join(",", left) + "}");
            parts.Add(Json.Num("providesCount", left.Count));

            return Json.Ok(parts.ToArray());
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
