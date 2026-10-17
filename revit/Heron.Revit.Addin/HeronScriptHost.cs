// Heron-Agent:  HERON-REVIT-CMP-021
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if REVIT2025_OR_GREATER
using System.Runtime.Loader;
#endif

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The one door between the add-in and the C# compiler, and the ONLY place
    /// the compiler's Revit-version branch lives (docs/16's adapter rule).
    /// RevitFragment calls these six members and names no Roslyn type.
    ///
    /// THE DEFECT IT REPAIRS - FRAGMENT-ISSUES row 5b-575, measured 2026-10-17
    /// on the owner's laptop. On a .NET Revit (2025, 2026, 2027) every add-in
    /// shares Revit's DEFAULT load context, and an assembly name is loaded
    /// there once, by whichever add-in asks first. The owner's AJ Tools ships
    /// Microsoft.CodeAnalysis 4.14. On Revit 2026.5 it had loaded the two core
    /// files before Heron; Heron's own C# layer (4.8) then came from Heron's
    /// folder, bound to the 4.14 core, and every fragment compile - the
    /// start-up warm-up first - threw TypeLoadException: "Method
    /// 'CommonGetSemanticModel' in type ... CSharpCompilation ... does not
    /// have an implementation". On Revit 2027.3 AJ Tools had loaded all four
    /// files first, so Heron compiled on AJ Tools' compiler - right by
    /// accident, in an order that is a race. And the reverse holds: when Heron
    /// asked first, its 4.8 sat in the shared context for every add-in after
    /// it.
    ///
    /// THE REPAIR, on 2025 and later only. Heron.Scripting.dll - this
    /// folder's HeronScriptEngine.cs and HeronStackGuard.cs, built as an
    /// assembly of their own - is loaded into a load context that belongs to
    /// Heron, CompilerContext below. In it, Heron.Scripting and every
    /// Microsoft.CodeAnalysis* name come from the folder Heron runs from, and
    /// NOTHING ELSE does: the Revit API, this add-in, Heron.Core and the
    /// runtime all resolve from the default context, so a compiled fragment
    /// sees the SAME RevitAPI types Revit hands it, the same
    /// HeronFragmentGlobals and the same HeronRunClock. What crosses the door
    /// is the runtime's own types only - see HeronScriptEngine.
    ///
    /// WHY NOT REVIT 2026'S MANIFEST ISOLATION. A `ManifestSettings` element in
    /// Heron.addin would isolate the whole add-in on 2026 and 2027 - and
    /// crashes Revit 2025 or older that reads it (tools/check-package.py
    /// refuses it), so every manifest would have to become per release, and
    /// Revit 2025, which is just as exposed, would still not be covered. A
    /// load context is plain .NET from 2025 on and needs no manifest at all.
    ///
    /// REVIT 2020 TO 2024 ARE NOT TOUCHED. On .NET Framework both compilers
    /// already load side by side (the row's warm-ups on 2020 and 2024), and
    /// the engine is built into this assembly there and called directly - the
    /// same calls RevitFragment made before this file existed.
    ///
    /// PROVED WITHOUT REVIT by tests/Heron.ScriptIsolation.TestHost, which
    /// loads another add-in's newer Roslyn into the default context first, the
    /// way AJ Tools did, and holds this door to Heron's own compiler and to
    /// the shared types; tests/test_script_isolation.py builds and runs it.
    /// Not yet run in Revit - NEEDS-CHECKING group GW.
    /// </summary>
    internal static class HeronScriptHost
    {
        public static object Options(Assembly[] references, string[] imports)
        {
#if REVIT2025_OR_GREATER
            return Engine.Get().Options(references, imports);
#else
            return HeronScriptEngine.Options(references, imports);
#endif
        }

        public static object Compile(string source, object options, Type globals, IList<string> errors)
        {
#if REVIT2025_OR_GREATER
            return Engine.Get().Compile(source, options, globals, errors);
#else
            return HeronScriptEngine.Compile(source, options, globals, errors);
#endif
        }

        public static void Emit(object compiled)
        {
#if REVIT2025_OR_GREATER
            Engine.Get().Emit(compiled);
#else
            HeronScriptEngine.Emit(compiled);
#endif
        }

        /// <summary>
        /// Run, and hand back every variable the script left, in its order.
        /// What the script throws arrives as itself.
        /// </summary>
        public static IList<Variable> Run(object compiled, object globals)
        {
#if REVIT2025_OR_GREATER
            var left = Engine.Get().Run(compiled, globals);
#else
            var left = HeronScriptEngine.Run(compiled, globals);
#endif
            var variables = new List<Variable>(left.Count);
            foreach (var pair in left) variables.Add(new Variable(pair.Key, pair.Value));
            return variables;
        }

        /// <summary>
        /// One variable a script left - what ScriptVariable was, on this side
        /// of the door. Value is read when asked, as ScriptVariable's was, and
        /// may throw; RevitFragment's readers catch that and skip the name.
        /// </summary>
        internal sealed class Variable
        {
            private readonly Func<object> read;

            public Variable(string name, Func<object> read)
            {
                Name = name;
                this.read = read;
            }

            public string Name { get; private set; }

            public object Value { get { return read(); } }
        }

        public static string Guard(string source)
        {
#if REVIT2025_OR_GREATER
            return Engine.Get().Guard(source);
#else
            return HeronScriptEngine.Guard(source);
#endif
        }

        /// <summary>"Microsoft.CodeAnalysis.CSharp 4.8.0.0 from C:\...\Heron\..." - for the log.</summary>
        public static string Compiler()
        {
#if REVIT2025_OR_GREATER
            return Engine.Get().Compiler() + ", in Heron's own load context";
#else
            return HeronScriptEngine.Compiler();
#endif
        }

#if REVIT2025_OR_GREATER
        /// <summary>The assembly HeronScriptEngine and HeronStackGuard are built into on 2025 and later.</summary>
        internal const string EngineAssembly = "Heron.Scripting";

        /// <summary>
        /// The names Heron's load context takes from Heron's folder. Everything
        /// else falls through to the default context, on purpose: a second
        /// RevitAPI, or a second copy of this add-in, would be types that look
        /// right and are not the ones Revit hands a fragment.
        /// </summary>
        internal static bool Owns(string name)
        {
            return name == EngineAssembly
                || name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal);
        }

        /// <summary>
        /// HeronScriptEngine's members, bound once as delegates. A delegate,
        /// not MethodInfo.Invoke: what a fragment throws then arrives as itself,
        /// not wrapped in a TargetInvocationException, and RunScript reports
        /// Revit's own words as it always has.
        /// </summary>
        private sealed class Engine
        {
            public Func<Assembly[], string[], object> Options;
            public Func<string, object, Type, IList<string>, object> Compile;
            public Action<object> Emit;
            public Func<object, object, IList<KeyValuePair<string, Func<object>>>> Run;
            public Func<string, string> Guard;
            public Func<string> Compiler;

            private static readonly object Gate = new object();
            private static Engine bound;
            private static string failed;

            /// <summary>
            /// Bound on first use - the warm-up's background thread, as a rule -
            /// and kept. A failure is kept too and said again on every call,
            /// rather than half-loading a second context each time.
            /// </summary>
            public static Engine Get()
            {
                lock (Gate)
                {
                    if (bound != null) return bound;
                    if (failed != null) throw new InvalidOperationException(failed);

                    var folder = Folder();
                    try
                    {
                        bound = Load(folder);
                        return bound;
                    }
                    catch (Exception failure)
                    {
                        var inner = failure is TargetInvocationException && failure.InnerException != null
                            ? failure.InnerException : failure;
                        failed = "Heron could not load its own C# compiler from " + folder + ": "
                               + inner.Message + " Reinstall Heron for this Revit release.";
                        throw new InvalidOperationException(failed, inner);
                    }
                }
            }

            /// <summary>
            /// The folder this add-in was loaded from - where
            /// Heron.Scripting.targets put the compiler. The add-in's OWN
            /// location, as IconLoader reads it, and not HeronPaths.Product:
            /// that is Heron.Core's folder, and in a shared default context
            /// Heron.Core is loaded once, from whichever Heron product asked
            /// first.
            /// </summary>
            private static string Folder()
            {
                return Path.GetDirectoryName(typeof(HeronScriptHost).Assembly.Location);
            }

            private static Engine Load(string folder)
            {
                var context = new CompilerContext(folder);
                var assembly = context.LoadFromAssemblyName(new AssemblyName(EngineAssembly));
                var type = assembly.GetType("Heron.Revit.Addin.HeronScriptEngine", true);

                return new Engine
                {
                    Options = Bind<Func<Assembly[], string[], object>>(type, "Options"),
                    Compile = Bind<Func<string, object, Type, IList<string>, object>>(type, "Compile"),
                    Emit = Bind<Action<object>>(type, "Emit"),
                    Run = Bind<Func<object, object, IList<KeyValuePair<string, Func<object>>>>>(type, "Run"),
                    Guard = Bind<Func<string, string>>(type, "Guard"),
                    Compiler = Bind<Func<string>>(type, "Compiler"),
                };
            }

            private static T Bind<T>(Type type, string name) where T : class
            {
                var method = type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                if (method == null)
                    throw new MissingMethodException(type.FullName, name);
                return (T)(object)method.CreateDelegate(typeof(T));
            }
        }

        /// <summary>
        /// Heron's own load context. Takes Heron.Scripting and the compiler
        /// from `folder`; hands every other name back to the default context
        /// by returning null - the runtime's documented fallback.
        ///
        /// A COMPILER FILE MISSING FROM HERON'S FOLDER IS AN ERROR, NOT A
        /// FALLBACK. Returning null for it would quietly bind another add-in's
        /// copy from the default context - row 5b-575 again, with nothing to
        /// say so. A satellite (a translated message file) may be missing;
        /// the compiler then speaks English.
        /// </summary>
        private sealed class CompilerContext : AssemblyLoadContext
        {
            private readonly string folder;

            public CompilerContext(string folder) : base("Heron C# compiler")
            {
                this.folder = folder;
            }

            protected override Assembly Load(AssemblyName name)
            {
                if (name.Name == null || !Owns(name.Name)) return null;

                var satellite = !string.IsNullOrEmpty(name.CultureName);
                var file = satellite
                    ? Path.Combine(folder, name.CultureName, name.Name + ".dll")
                    : Path.Combine(folder, name.Name + ".dll");

                if (File.Exists(file)) return LoadFromAssemblyPath(file);
                if (satellite) return null;

                throw new FileNotFoundException(
                    name.Name + ".dll is not in Heron's folder, and Heron will not use another add-in's copy.",
                    file);
            }
        }
#endif
    }
}
