// Heron-Agent:  HERON-REVIT-CMP-021
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Everything Heron asks of the C# compiler, and the one file besides
    /// HeronStackGuard that names a Roslyn type. Reached ONLY through
    /// HeronScriptHost - read that file first.
    ///
    /// WHY IT IS ITS OWN FILE, AND ON A .NET REVIT ITS OWN ASSEMBLY - row
    /// 5b-575. On Revit 2025 and later every add-in shares Revit's default load
    /// context, and an assembly name is loaded there ONCE, by whichever add-in
    /// asks first. On the owner's laptop, 2026-10-17, another add-in had loaded
    /// Microsoft.CodeAnalysis 4.14 before Heron; Heron's 4.8 C# layer then
    /// bound to that core and every fragment compile threw TypeLoadException
    /// ("Method 'CommonGetSemanticModel' ... does not have an implementation").
    /// So on those releases this file and the guard are built into
    /// Heron.Scripting.dll, which HeronScriptHost loads into a load context of
    /// Heron's own, where Microsoft.CodeAnalysis means the copy beside Heron.
    /// On 2020 to 2024 they are built into the add-in exactly as before.
    ///
    /// EVERY MEMBER TAKES AND RETURNS ONLY THE RUNTIME'S OWN TYPES - string,
    /// object, Type, Assembly, IList, Func. Those are the same types in every
    /// load context, so the add-in can hold what comes back without naming
    /// Roslyn. A compiled script is handed out as `object` and only ever
    /// handed back here. Add a member with a Roslyn type in its signature and
    /// the add-in can no longer call it on a .NET Revit.
    ///
    /// NOTHING HERE DECIDES ANYTHING. Which references, which imports, what
    /// counts as a refusal and how it is worded all stay in RevitFragment;
    /// this is the same five calls it made before, moved behind a door.
    /// </summary>
    internal static class HeronScriptEngine
    {
        /// <summary>The script options for these references and imports.</summary>
        internal static object Options(Assembly[] references, string[] imports)
        {
            return ScriptOptions.Default
                .WithReferences(references)
                .WithImports(imports);
        }

        /// <summary>
        /// Compile once. Returns the compiled script, or null with every
        /// error-severity diagnostic added to `errors` in the compiler's words.
        /// </summary>
        internal static object Compile(string source, object options, Type globals,
                                       IList<string> errors)
        {
            var script = CSharpScript.Create<object>(source, (ScriptOptions)options, globals);

            foreach (var diagnostic in script.Compile())
            {
                if (diagnostic.Severity == DiagnosticSeverity.Error)
                    errors.Add(diagnostic.ToString());
            }

            return errors.Count == 0 ? script : null;
        }

        /// <summary>Emit what Compile checked, without running it - the warm-up's second half.</summary>
        internal static void Emit(object compiled)
        {
            ((Script<object>)compiled).CreateDelegate();
        }

        /// <summary>
        /// Run a compiled script against its globals. Returns every variable it
        /// left, by name, in the order it declared them; each value is read when
        /// its getter is called, as ScriptState read it. What the script throws
        /// is thrown on, unwrapped - RevitFragment.RunScript reports it.
        /// </summary>
        internal static IList<KeyValuePair<string, Func<object>>> Run(object compiled, object globals)
        {
            var state = ((Script<object>)compiled).RunAsync(globals).GetAwaiter().GetResult();

            var left = new List<KeyValuePair<string, Func<object>>>();
            foreach (var variable in state.Variables)
            {
                var held = variable;
                left.Add(new KeyValuePair<string, Func<object>>(held.Name, () => held.Value));
            }
            return left;
        }

        /// <summary>HeronStackGuard.Apply - it parses with Roslyn, so it lives on this side of the door.</summary>
        internal static string Guard(string source)
        {
            return HeronStackGuard.Apply(source);
        }

        /// <summary>
        /// Which C# compiler this is and where it was loaded from - for the
        /// warm-up's log line, which is how a run in Revit shows row 5b-575 is
        /// repaired without opening a debugger.
        /// </summary>
        internal static string Compiler()
        {
            var csharp = typeof(CSharpCompilation).Assembly;
            return "Microsoft.CodeAnalysis.CSharp " + csharp.GetName().Version + " from " + csharp.Location;
        }
    }
}
