// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  test
// See docs/29-metadata-standard.md

// ROW 5b-575 IS A .NET-RELEASE DEFECT. Built for 2020 to 2024 - which
// tools/check-compile.py does, as it builds every project on every release -
// this is the stub at the bottom: .NET Framework loads two compilers side by
// side, has no AssemblyLoadContext, and HeronScriptHost calls the engine
// directly there, so there is nothing for this host to arrange.
#if REVIT2025_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using Heron.Revit.Addin;

namespace Heron.ScriptIsolation.TestHost
{
    /// <summary>The Revit API object a fragment is handed - standing in for a Document.</summary>
    public sealed class ProbeDocument
    {
        public string Name;
    }

    /// <summary>What a script finds in scope - standing in for HeronFragmentGlobals.</summary>
    public sealed class ProbeGlobals
    {
        public ProbeDocument doc;
    }

    /// <summary>
    /// A static the script calls. If the script reached a SECOND copy of this
    /// assembly, Touched here would stay null - the same way a second copy of
    /// RevitAPI would hand a fragment types Revit does not use.
    /// </summary>
    public static class ProbeApi
    {
        public static object Touched;

        public static void Touch(object seen)
        {
            Touched = seen;
        }
    }

    /// <summary>
    /// One case of row 5b-575 per process - a load context cannot be emptied,
    /// so each arrangement of "who loaded which Roslyn first" needs a fresh
    /// one. tests/test_script_isolation.py runs them all.
    ///
    ///   --first none   no other add-in has loaded a compiler
    ///   --first core   another add-in's Microsoft.CodeAnalysis and
    ///                  Microsoft.CodeAnalysis.Scripting are loaded first -
    ///                  Revit 2026.5 on the owner's laptop
    ///   --first all    all four of its files first - Revit 2027.3 there
    ///   --other-after  and the other add-in loads and USES its compiler
    ///                  after Heron has compiled - Heron must not have left
    ///                  its own in the shared context
    ///
    ///   --door heron   HeronScriptHost, the door the add-in uses. THE FIX
    ///   --door direct  the engine loaded into the DEFAULT context, where the
    ///                  add-in's Roslyn calls ran until this repair. A
    ///                  CONTROL: it must reproduce the row, or the case is not
    ///                  arranged the way Revit was and the heron door's pass
    ///                  would mean nothing
    ///
    /// Every case also hooks the default context's Resolving the way an
    /// add-in host does - a name nothing has loaded is looked for in Heron's
    /// folder - so a door that falls back to the default context for the
    /// compiler is caught loading Heron's copy there.
    /// </summary>
    internal static class Program
    {
        private const string Script =
            "ProbeApi.Touch(doc);\n" +
            "var seen = doc;\n" +
            "int answer = doc.Name.Length;\n" +
            "var names = new List<string> { doc.Name, \"\" };\n" +
            "int count = names.Where(n => { return n.Length > 0; }).Count();\n";

        private static int _checks;
        private static readonly List<string> Failures = new List<string>();

        private static void Check(bool condition, string what)
        {
            _checks++;
            Console.WriteLine((condition ? "  ok    " : "  FAIL  ") + what);
            if (!condition) Failures.Add(what);
        }

        private static int Main(string[] args)
        {
            var first = Arg(args, "--first", "none");
            var door = Arg(args, "--door", "heron");
            var otherAfter = args.Contains("--other-after");
            // Where HeronScriptHost looks: the folder its own assembly came from.
            var heronFolder = Path.GetDirectoryName(typeof(HeronScriptHost).Assembly.Location);

            Console.WriteLine("Row 5b-575 case: --first " + first + " --door " + door
                              + (otherAfter ? " --other-after" : "")
                              + ", on .NET " + Environment.Version);

            // REVIT'S RESOLVER, as far as this case needs it: a name the default
            // context has not loaded is looked for in Heron's folder.
            AssemblyLoadContext.Default.Resolving += (context, name) =>
            {
                var file = Path.Combine(heronFolder, name.Name + ".dll");
                return File.Exists(file) ? context.LoadFromAssemblyPath(file) : null;
            };

            var other = Other.Find();
            if (other == null)
            {
                Console.WriteLine("FAILED before it began: the other add-in's Roslyn was not downloaded"
                                  + " - restore this project, which downloads it.");
                return 1;
            }

            if (first == "core") other.Load(Other.Core);
            else if (first == "all") other.Load(Other.All);
            var defaultBefore = RoslynIn(AssemblyLoadContext.Default);

            try
            {
                if (door == "heron") ThroughTheDoor(first, heronFolder, other, defaultBefore);
                else if (door == "direct") TheOldWay(first, heronFolder, other);
                else
                {
                    Console.WriteLine("FAILED: --door is heron or direct, not " + door);
                    return 1;
                }

                if (otherAfter) TheOtherAddinAfter(door, other);
            }
            catch (Exception failure)
            {
                Check(false, "the case ran to the end - it threw " + failure.GetType().Name + ": "
                              + failure.Message);
            }

            if (Failures.Count == 0)
            {
                Console.WriteLine("PASSED - " + _checks + " checks");
                return 0;
            }

            Console.WriteLine("FAILED - " + Failures.Count + " of " + _checks + ":");
            foreach (var failure in Failures) Console.WriteLine("  " + failure);
            return 1;
        }

        // -----------------------------------------------------------------
        // THE FIX: HeronScriptHost, the add-in's only door to the compiler.
        // -----------------------------------------------------------------

        private static void ThroughTheDoor(string first, string heronFolder, Other other,
                                           List<Assembly> defaultBefore)
        {
            var references = new[] { typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(ProbeDocument).Assembly };
            var imports = new[] { "System", "System.Collections.Generic", "System.Linq", "Heron.ScriptIsolation.TestHost" };

            var options = HeronScriptHost.Options(references, imports);
            var guarded = HeronScriptHost.Guard(Script);
            Check(guarded.Contains("HeronRunClock.Check"),
                  "the guard ran through the door and put the run clock into the script's lambda");

            var errors = new List<string>();
            var compiled = HeronScriptHost.Compile(guarded, options, typeof(ProbeGlobals), errors);
            Check(compiled != null && errors.Count == 0,
                  "the script compiled" + (errors.Count == 0 ? "" : " - " + string.Join("; ", errors)));

            var compiler = HeronScriptHost.Compiler();
            Check(compiler.Contains(" 4.8.") && compiler.Contains(heronFolder),
                  "it compiled on Heron's own C# compiler, 4.8 from Heron's folder - " + compiler);

            if (compiled != null)
            {
                var doc = new ProbeDocument { Name = "EQ-2026 Snowdon Towers Sample HVAC" };
                ProbeApi.Touched = null;
                var left = HeronScriptHost.Run(compiled, new ProbeGlobals { doc = doc });

                Check(ReferenceEquals(ProbeApi.Touched, doc),
                      "the script called THIS assembly's ProbeApi - the default context's copy, not a second one");
                var seen = left.FirstOrDefault(v => v.Name == "seen");
                Check(seen != null && ReferenceEquals(seen.Value, doc),
                      "the script's `doc` is the very object handed in, so its type is the host's type");
                var answer = left.FirstOrDefault(v => v.Name == "answer");
                Check(answer != null && Equals(answer.Value, doc.Name.Length),
                      "and the variables it left come back by name, with their values");
                Check(string.Join(",", left.Select(v => v.Name)) == "seen,answer,names,count",
                      "in the order the script declared them - " + string.Join(",", left.Select(v => v.Name)));
            }

            // A compile error crosses the door as the compiler's words.
            var broken = new List<string>();
            var none = HeronScriptHost.Compile("int x = notDeclared;", options, typeof(ProbeGlobals), broken);
            Check(none == null && broken.Any(e => e.Contains("CS0103")),
                  "a script that does not compile comes back null, with the compiler's error - "
                  + string.Join("; ", broken));

            // What a fragment throws arrives as itself, not wrapped.
            var thrower = HeronScriptHost.Compile("throw new InvalidOperationException(\"from the fragment\");",
                                                  options, typeof(ProbeGlobals), new List<string>());
            Exception caught = null;
            try { HeronScriptHost.Run(thrower, new ProbeGlobals { doc = new ProbeDocument { Name = "x" } }); }
            catch (Exception failure) { caught = failure; }
            Check(caught is InvalidOperationException && caught.Message == "from the fragment",
                  "what a script throws arrives unwrapped - "
                  + (caught == null ? "nothing was thrown" : caught.GetType().Name + ": " + caught.Message));

            // THE RUN CLOCK IS THE DEFAULT CONTEXT'S. A guarded runaway must be
            // stopped by the clock this process started; a script bound to a
            // second HeronRunClock would loop for ever, so it runs on a
            // background thread and is given up on after five seconds.
            var runaway = HeronScriptHost.Compile(HeronScriptHost.Guard("while (true) { }"), options,
                                                  typeof(ProbeGlobals), new List<string>());
            Exception stopped = null;
            var finished = false;
            var thread = new Thread(() =>
            {
                try
                {
                    using (HeronRunClock.Begin(TimeSpan.FromMilliseconds(200)))
                    using (HeronRunClock.Watch())
                    {
                        HeronScriptHost.Run(runaway, new ProbeGlobals { doc = new ProbeDocument { Name = "x" } });
                    }
                }
                catch (Exception failure) { stopped = failure; }
                finished = true;
            });
            thread.IsBackground = true;
            thread.Start();
            thread.Join(TimeSpan.FromSeconds(5));
            Check(finished && stopped != null && stopped.Message.StartsWith("Heron stopped this job", StringComparison.Ordinal),
                  "a guarded runaway is stopped by THIS process's run clock - "
                  + (!finished ? "still running after 5 s" : stopped == null ? "it returned" : stopped.Message));

            // WHERE EVERYTHING LANDED.
            var heronContext = AssemblyLoadContext.All.FirstOrDefault(c => c.Name == "Heron C# compiler");
            Check(heronContext != null, "Heron's own load context exists, named \"Heron C# compiler\"");
            if (heronContext != null)
            {
                var mine = RoslynIn(heronContext);
                var names = mine.Select(a => a.GetName().Name).OrderBy(n => n).ToList();
                Check(names.SequenceEqual(new[] { "Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.CSharp",
                                                  "Microsoft.CodeAnalysis.CSharp.Scripting", "Microsoft.CodeAnalysis.Scripting" }),
                      "it holds all four compiler files - " + string.Join(", ", names));
                Check(mine.All(a => a.GetName().Version.ToString().StartsWith("4.8.", StringComparison.Ordinal)
                                    && Path.GetDirectoryName(a.Location) == heronFolder),
                      "every one 4.8, from Heron's folder - "
                      + string.Join(", ", mine.Select(a => a.GetName().Name + " " + a.GetName().Version)));
                Check(heronContext.Assemblies.Any(a => a.GetName().Name == HeronScriptHost.EngineAssembly),
                      "Heron.Scripting is in it");
                Check(!heronContext.Assemblies.Any(a => a.GetName().Name == typeof(Program).Assembly.GetName().Name
                                                        || a.GetName().Name == "Heron.Core"),
                      "and nothing the script shares with the host is - no second copy of this assembly or Heron.Core");
            }

            var defaultAfter = RoslynIn(AssemblyLoadContext.Default);
            Check(!AssemblyLoadContext.Default.Assemblies.Any(a => a.GetName().Name == HeronScriptHost.EngineAssembly),
                  "Heron.Scripting is NOT in the default context");
            Check(defaultAfter.Select(Describe).OrderBy(s => s).SequenceEqual(defaultBefore.Select(Describe).OrderBy(s => s)),
                  "the default context holds exactly the Roslyn it held before Heron compiled - "
                  + (defaultAfter.Count == 0 ? "none" : string.Join(", ", defaultAfter.Select(Describe))));
            if (first != "none")
            {
                Check(defaultAfter.All(a => a.GetName().Version.ToString() == other.Version + ".0"),
                      "the other add-in's " + other.Version + " is still what the default context holds");
            }
        }

        // -----------------------------------------------------------------
        // THE CONTROL: the engine in the default context, as the add-in's
        // Roslyn calls were until this repair. It must FAIL the way row 5b-575
        // was measured, or the case does not model Revit.
        // -----------------------------------------------------------------

        private static void TheOldWay(string first, string heronFolder, Other other)
        {
            var engine = AssemblyLoadContext.Default.LoadFromAssemblyPath(
                Path.Combine(heronFolder, HeronScriptHost.EngineAssembly + ".dll"));
            var type = engine.GetType("Heron.Revit.Addin.HeronScriptEngine", true);
            Func<string, MethodInfo> method = name => type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic);

            var references = new[] { typeof(object).Assembly, typeof(Enumerable).Assembly, typeof(ProbeDocument).Assembly };
            var imports = new[] { "System", "System.Collections.Generic", "System.Linq", "Heron.ScriptIsolation.TestHost" };

            Exception threw = null;
            string compiler = null;
            try
            {
                var options = method("Options").Invoke(null, new object[] { references, imports });
                var errors = new List<string>();
                method("Compile").Invoke(null, new object[] { Script, options, typeof(ProbeGlobals), errors });
                compiler = (string)method("Compiler").Invoke(null, null);
            }
            catch (TargetInvocationException failure) { threw = failure.InnerException; }

            if (first == "core")
            {
                Check(threw is TypeLoadException && threw.Message.Contains("CommonGetSemanticModel"),
                      "CONTROL: the old way fails as row 5b-575 measured on Revit 2026.5 - "
                      + (threw == null ? "it compiled on " + compiler : threw.GetType().Name + ": " + threw.Message));
            }
            else if (first == "all")
            {
                Check(threw == null && compiler != null && compiler.Contains(" " + other.Version + ".")
                      && !compiler.Contains(heronFolder),
                      "CONTROL: the old way compiles on the OTHER add-in's compiler, as on Revit 2027.3 - "
                      + (threw == null ? compiler : threw.GetType().Name + ": " + threw.Message));
            }
            else
            {
                Check(threw == null && compiler != null && compiler.Contains(heronFolder),
                      "CONTROL: alone, the old way compiles on Heron's compiler, from the default context - "
                      + (threw == null ? compiler : threw.GetType().Name + ": " + threw.Message));
                Check(RoslynIn(AssemblyLoadContext.Default).Any(a => Path.GetDirectoryName(a.Location) == heronFolder),
                      "CONTROL: and leaves Heron's compiler in the default context, for every add-in after it");
            }
        }

        // -----------------------------------------------------------------
        // THE OTHER ADD-IN, AFTER HERON. It loads its own four files and
        // compiles with them, as AJ Tools would on its first use.
        // -----------------------------------------------------------------

        private static void TheOtherAddinAfter(string door, Other other)
        {
            Exception threw = null;
            object answer = null;
            try
            {
                var loaded = other.Load(Other.All);
                var scripting = loaded.First(a => a.GetName().Name == "Microsoft.CodeAnalysis.CSharp.Scripting");
                var csharpScript = scripting.GetType("Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript", true);
                var evaluate = csharpScript.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .First(m => m.Name == "EvaluateAsync" && !m.IsGenericMethod && m.GetParameters().Length == 5);
                var task = evaluate.Invoke(null, new object[] { "1 + 1", null, null, null, CancellationToken.None });
                answer = task.GetType().GetProperty("Result").GetValue(task);
            }
            catch (Exception failure)
            {
                threw = failure is TargetInvocationException && failure.InnerException != null
                    ? failure.InnerException : failure;
            }

            if (door == "heron")
            {
                Check(threw == null && Equals(answer, 2),
                      "the other add-in, loading its own " + other.Version + " after Heron, compiles and runs - "
                      + (threw == null ? "1 + 1 = " + answer : threw.GetType().Name + ": " + threw.Message));
            }
            else
            {
                Check(threw != null,
                      "CONTROL: after the old way, the other add-in cannot load its own compiler - "
                      + (threw == null ? "it answered " + answer : threw.GetType().Name + ": " + threw.Message));
            }
        }

        // -----------------------------------------------------------------

        private static List<Assembly> RoslynIn(AssemblyLoadContext context)
        {
            return context.Assemblies
                .Where(a => a.GetName().Name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
                .ToList();
        }

        private static string Describe(Assembly assembly)
        {
            return assembly.GetName().Name + " " + assembly.GetName().Version;
        }

        private static string Arg(string[] args, string name, string fallback)
        {
            var at = Array.IndexOf(args, name);
            return at >= 0 && at + 1 < args.Length ? args[at + 1] : fallback;
        }

        /// <summary>
        /// The other add-in's Roslyn, from the NuGet cache this project's
        /// restore downloaded it into. 4.14.0 - the measured version - where
        /// the runtime can run it, else 4.13.0.
        /// </summary>
        private sealed class Other
        {
            public static readonly string[] Core = { "Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.Scripting" };
            public static readonly string[] All =
            {
                "Microsoft.CodeAnalysis", "Microsoft.CodeAnalysis.Scripting",
                "Microsoft.CodeAnalysis.CSharp", "Microsoft.CodeAnalysis.CSharp.Scripting",
            };

            private static readonly Dictionary<string, string> Package = new Dictionary<string, string>
            {
                { "Microsoft.CodeAnalysis", "microsoft.codeanalysis.common" },
                { "Microsoft.CodeAnalysis.Scripting", "microsoft.codeanalysis.scripting.common" },
                { "Microsoft.CodeAnalysis.CSharp", "microsoft.codeanalysis.csharp" },
                { "Microsoft.CodeAnalysis.CSharp.Scripting", "microsoft.codeanalysis.csharp.scripting" },
            };

            public string Version;
            private string root;

            public static Other Find()
            {
                var root = typeof(Program).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                    .Where(a => a.Key == "NuGetPackageRoot").Select(a => a.Value).FirstOrDefault();
                if (string.IsNullOrEmpty(root)) return null;

                var version = Environment.Version.Major >= 9 ? "4.14.0" : "4.13.0";
                var probe = Path.Combine(root, Package["Microsoft.CodeAnalysis"], version, "lib", "net8.0",
                                         "Microsoft.CodeAnalysis.dll");
                return File.Exists(probe) ? new Other { Version = version, root = root } : null;
            }

            /// <summary>Load these files into the default context by path, as an add-in's own do.</summary>
            public List<Assembly> Load(string[] names)
            {
                var loaded = new List<Assembly>();
                foreach (var name in names)
                {
                    var already = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(a => a.GetName().Name == name);
                    if (already != null && already.GetName().Version.ToString() == Version + ".0")
                    {
                        loaded.Add(already);
                        continue;
                    }
                    loaded.Add(AssemblyLoadContext.Default.LoadFromAssemblyPath(
                        Path.Combine(root, Package[name], Version, "lib", "net8.0", name + ".dll")));
                }
                return loaded;
            }
        }
    }
}
#else
namespace Heron.ScriptIsolation.TestHost
{
    internal static class Program
    {
        private static int Main()
        {
            System.Console.WriteLine("Row 5b-575 is a .NET-release defect - build this host with RevitVersion 2025 or later.");
            return 1;
        }
    }
}
#endif
