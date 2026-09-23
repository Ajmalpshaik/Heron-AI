// Heron-Agent:  none
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  tool
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

// ApiSurface <assembly.dll> <apiDir> [<apiDir> ...]
//
// Reads every Autodesk.Revit type and member the assembly REFERENCES - from the
// compiled metadata, so it is exactly what the code calls rather than what a
// regex over the source can find - and reports any that do not exist in each
// target release's reference assemblies.
//
// This exists because the compiler cannot reach every release. Revit 2025+
// needs the Windows Desktop SDK for WPF, so tools/check-compile.py reports
// those as SKIPPED off Windows. This closes that hole for the failure that
// actually matters: a member that is simply absent from a release.
//
// LIMIT, and it is a real one: matching is by NAME. A member that still exists
// but changed SIGNATURE passes here and would fail a real compile. That is why
// this supplements check-compile.py rather than replacing it.
//
// `--members <Type>` (Members.cs) is the other half: it prints one type's
// parameters, return types, get/set and [Obsolete] marks on every cached
// release, so a changed signature can at least be SEEN before it is written.
// It does not change what this mode checks.
class Program
{
    record Ref(string TypeFullName, string Member);

    static HashSet<Ref> ReadRevitRefs(string dll)
    {
        var found = new HashSet<Ref>();
        using var fs = File.OpenRead(dll);
        using var pe = new PEReader(fs);
        var md = pe.GetMetadataReader();

        string ScopeName(EntityHandle h)
        {
            if (h.Kind == HandleKind.AssemblyReference)
                return md.GetString(md.GetAssemblyReference((AssemblyReferenceHandle)h).Name);
            if (h.Kind == HandleKind.TypeReference)
                return ScopeName(md.GetTypeReference((TypeReferenceHandle)h).ResolutionScope);
            return "";
        }

        string TypeName(TypeReferenceHandle h)
        {
            var tr = md.GetTypeReference(h);
            var ns = md.GetString(tr.Namespace);
            var nm = md.GetString(tr.Name);
            return string.IsNullOrEmpty(ns) ? nm : ns + "." + nm;
        }

        bool IsRevit(string asm) =>
            asm.Equals("RevitAPI", StringComparison.OrdinalIgnoreCase) ||
            asm.Equals("RevitAPIUI", StringComparison.OrdinalIgnoreCase);

        // Types referenced at all.
        foreach (var h in md.TypeReferences)
        {
            var tr = md.GetTypeReference(h);
            if (IsRevit(ScopeName(tr.ResolutionScope))) found.Add(new Ref(TypeName(h), null));
        }

        // Members actually called.
        foreach (var h in md.MemberReferences)
        {
            var mr = md.GetMemberReference(h);
            if (mr.Parent.Kind != HandleKind.TypeReference) continue;
            var trh = (TypeReferenceHandle)mr.Parent;
            if (!IsRevit(ScopeName(md.GetTypeReference(trh).ResolutionScope))) continue;
            found.Add(new Ref(TypeName(trh), md.GetString(mr.Name)));
        }
        return found;
    }

    /// <summary>
    /// Every public type and member one release ships, one per line.
    ///
    /// The existing mode asks "does everything Heron calls exist here" -
    /// a question about Heron. This asks what the RELEASE contains, which
    /// is a question about Revit, and it is the only way to see a member
    /// disappear that Heron does not happen to call today. A fragment
    /// somewhere does.
    ///
    /// Public only. A protected or internal member changing breaks
    /// nothing a fragment can reach, and including them would bury the
    /// changes that matter under ones nobody can act on.
    /// </summary>
    static int Dump(string dir, string outPath)
    {
        var paths = Directory.GetFiles(dir, "*.dll").ToList();
        paths.AddRange(Directory.GetFiles(
            Path.GetDirectoryName(typeof(object).Assembly.Location), "*.dll"));
        using var mlc = new MetadataLoadContext(
            new PathAssemblyResolver(paths), "System.Private.CoreLib");

        var lines = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.GetFiles(dir, "*.dll"))
        {
            Assembly asm;
            try { asm = mlc.LoadFromAssemblyPath(Path.GetFullPath(path)); }
            catch { continue; }

            Type[] types;
            // A release whose reference assembly cannot fully resolve is
            // still worth reading for the types that did load. Dropping
            // the lot would report the whole release as deleted.
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex)
            { types = ex.Types.Where(t => t != null).ToArray(); }

            foreach (var type in types)
            {
                if (type == null || !type.IsPublic && !type.IsNestedPublic)
                    continue;
                lines.Add(type.FullName);
                const BindingFlags F = BindingFlags.Public |
                                       BindingFlags.Instance |
                                       BindingFlags.Static |
                                       BindingFlags.DeclaredOnly;
                MemberInfo[] members;
                try { members = type.GetMembers(F); }
                catch { continue; }
                foreach (var member in members)
                {
                    // The NAME, not the signature. An overload added is
                    // not a member removed, and listing every signature
                    // turns one rename into forty lines of noise.
                    lines.Add(type.FullName + "." + member.Name);
                }
            }
        }

        File.WriteAllLines(outPath, lines);
        Console.WriteLine("{0}: {1} public type(s) and member(s) -> {2}",
                          Path.GetFileName(dir.TrimEnd('/')), lines.Count,
                          outPath);
        return 0;
    }

    static int Main(string[] args)
    {
        // --dump <assembly dir> <out file>. A separate mode rather than a
        // separate program: the metadata loading, the resolver and the
        // core-library trick are the awkward part and there is no reason
        // for two copies of them.
        if (args.Length == 3 && args[0] == "--dump") return Dump(args[1], args[2]);

        // --members <Type> [--inherited] [<release dir> ...]. One type's
        // SIGNATURES on every cached release - the half the LIMIT above says
        // this file cannot see. Its own file, Members.cs, says how.
        if (args.Length >= 1 && args[0] == "--members") return Members.Run(args);

        var addin = args[0];
        var refs = ReadRevitRefs(addin);
        Console.WriteLine("Heron references {0} distinct Revit types/members.", refs.Count);
        Console.WriteLine();

        int worstMissing = 0;
        foreach (var dir in args.Skip(1))
        {
            var label = Path.GetFileName(dir.TrimEnd('/'));
            var paths = Directory.GetFiles(dir, "*.dll").ToList();
            paths.AddRange(Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location), "*.dll"));
            using var mlc = new MetadataLoadContext(new PathAssemblyResolver(paths), "System.Private.CoreLib");
            var asms = Directory.GetFiles(dir, "*.dll").Select(p => mlc.LoadFromAssemblyPath(Path.GetFullPath(p))).ToList();

            Type Find(string full) => asms.Select(a => a.GetType(full, false, false)).FirstOrDefault(t => t != null);

            bool HasMember(Type t, string name)
            {
                const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                for (var cur = t; cur != null; cur = cur.BaseType)
                {
                    if (name == ".ctor" || name == ".cctor")
                    { if (cur.GetConstructors(F).Length > 0) return true; continue; }
                    if (cur.GetMember(name, F).Length > 0) return true;
                }
                foreach (var i in t.GetInterfaces()) if (i.GetMember(name, BindingFlags.Public | BindingFlags.Instance).Length > 0) return true;
                return false;
            }

            var missingTypes = new List<string>();
            var missingMembers = new List<string>();

            foreach (var r in refs.OrderBy(x => x.TypeFullName).ThenBy(x => x.Member))
            {
                var t = Find(r.TypeFullName);
                if (t == null) { missingTypes.Add(r.TypeFullName); continue; }
                if (r.Member == null) continue;
                if (!HasMember(t, r.Member)) missingMembers.Add(r.TypeFullName + "." + r.Member);
            }

            var mt = missingTypes.Distinct().ToList();
            Console.WriteLine("=== Revit {0} ===", label);
            if (mt.Count == 0 && missingMembers.Count == 0)
                Console.WriteLine("   OK   every referenced type and member exists");
            else
            {
                foreach (var x in mt) Console.WriteLine("   MISSING TYPE    " + x);
                foreach (var x in missingMembers) Console.WriteLine("   MISSING MEMBER  " + x);
                worstMissing += mt.Count + missingMembers.Count;
            }
            Console.WriteLine();
        }
        return worstMissing == 0 ? 0 : 1;
    }
}
