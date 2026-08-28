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

    static int Main(string[] args)
    {
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
