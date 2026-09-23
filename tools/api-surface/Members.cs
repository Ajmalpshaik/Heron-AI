// Heron-Agent:  none
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  tool
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

/// <summary>
/// `--members`: one type's public members, SIGNATURE and all, as every cached
/// release ships it.
///
///     dotnet run --project tools/api-surface -- --members WorksharingUtils
///     dotnet run --project tools/api-surface -- --members ElementId.IntegerValue
///     dotnet run --project tools/api-surface -- --members Duct --inherited
///
/// WHY IT EXISTS. The other two modes read NAMES. `check-api-surface.py` asks
/// whether every member Heron calls exists, and `api-changes.py` asks what
/// each release stopped shipping - and both say in their own words what that
/// cannot see: a member still there whose parameters, return type or setter
/// changed, and an `[Obsolete]` mark, which is an attribute and not a name.
/// This answers exactly those, for one type at a time, so whoever writes the
/// next fragment can read what a call looks like on 2020 AND on 2027 before
/// writing it, instead of finding out from a compile on a release they did
/// not build.
///
/// IT READS THE CACHE THE OTHER TWO ALREADY FILL - tools/api-surface/.assemblies,
/// one folder per release, fetched from NuGet by `check-api-surface.py`. No
/// Revit, no network, and no copy of the release list: it reads the folders it
/// finds and names every one, so a release that was never fetched is visibly
/// absent rather than silently treated as having nothing.
///
/// EXIT CODES: 0 found, 1 no such type in any release read, 2 the question
/// cannot be answered as asked (no name, or a short name that means more than
/// one type), 3 NOT RUN - nothing cached to read, which is not an answer about
/// Revit and must not be read as "the type does not exist".
/// </summary>
static class Members
{
    const BindingFlags Declared = BindingFlags.Public | BindingFlags.Instance |
                                  BindingFlags.Static | BindingFlags.DeclaredOnly;
    const BindingFlags Everything = BindingFlags.Public | BindingFlags.Instance |
                                    BindingFlags.Static | BindingFlags.FlattenHierarchy;

    /// <summary>One line of the answer, as one release ships it.</summary>
    sealed class Line
    {
        public int Rank;          // ctor, property, method, field, event, type
        public long Order;        // an enum value's number, so values read in order
        public string Kind;
        public string Name;
        public string Text;
        public string From;       // declaring type, when --inherited brought it in
        public string Obsolete;   // null when not marked

        public string Key => Kind + "|" + Text + "|" + From;
    }

    public static int Run(string[] args)
    {
        string query = null;
        var inherited = false;
        var dirs = new List<string>();
        foreach (var arg in args.Skip(1))
        {
            if (arg == "--inherited") inherited = true;
            else if (query == null) query = arg;
            else dirs.Add(arg);
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            Console.WriteLine("usage: --members <Type>|<Type.Member> [--inherited] [<release dir> ...]");
            Console.WriteLine("  A short name (WorksharingUtils) or a full one (Autodesk.Revit.DB.WorksharingUtils).");
            return 2;
        }

        string from = null;
        if (dirs.Count == 0)
        {
            from = Cache();
            if (from != null)
                dirs.AddRange(Directory.GetDirectories(from)
                                       .Where(d => Directory.GetFiles(d, "*.dll").Length > 0)
                                       .OrderBy(d => Path.GetFileName(d), StringComparer.Ordinal));
        }

        var readable = dirs.Where(d => Directory.Exists(d) &&
                                       Directory.GetFiles(d, "*.dll").Length > 0).ToList();
        if (readable.Count == 0)
        {
            Console.WriteLine("NOT RUN - there are no reference assemblies to read" +
                              (dirs.Count > 0 ? " in " + string.Join(", ", dirs) + "."
                               : from != null ? " in " + from + "."
                               : ", and no tools/api-surface/.assemblies cache was found."));
            Console.WriteLine("Run `python tools/check-api-surface.py` once - it fetches every");
            Console.WriteLine("release into that cache. Nothing is claimed about any type.");
            return 3;
        }

        foreach (var missing in dirs.Except(readable))
            Console.WriteLine("  not read: " + missing + " - no assemblies there");

        var releases = readable.Select(d => Path.GetFileName(d.TrimEnd('/', '\\'))).ToList();

        // Per release: the type's header, and its lines. Read one release at a
        // time and let its load context go, because each holds a 33 MB
        // RevitAPI.dll open and there are eight.
        var headers = new Dictionary<string, string>();          // release -> header
        var lines = new Dictionary<string, List<Line>>();        // release -> lines
        var found = new SortedSet<string>(StringComparer.Ordinal);
        var near = new SortedSet<string>(StringComparer.Ordinal);
        string member = null;

        foreach (var dir in readable)
        {
            var release = Path.GetFileName(dir.TrimEnd('/', '\\'));
            var paths = Directory.GetFiles(dir, "*.dll").ToList();
            paths.AddRange(Directory.GetFiles(
                Path.GetDirectoryName(typeof(object).Assembly.Location), "*.dll"));
            using var mlc = new MetadataLoadContext(
                new PathAssemblyResolver(paths), "System.Private.CoreLib");

            var types = new List<Type>();
            foreach (var path in Directory.GetFiles(dir, "*.dll"))
            {
                try { types.AddRange(Loaded(mlc.LoadFromAssemblyPath(Path.GetFullPath(path)))); }
                catch (Exception) { /* not a .NET assembly - nothing to read in it */ }
            }

            var (matches, filter) = Match(types, query);
            if (matches.Count == 0)
            {
                foreach (var t in types)
                    if (Nested(t).IndexOf(Last(query), StringComparison.OrdinalIgnoreCase) >= 0)
                        near.Add(Nested(t));
                continue;
            }

            foreach (var t in matches) found.Add(Nested(t));
            if (found.Count > 1) continue;      // reported below, once

            member = filter;
            var type = matches[0];
            headers[release] = Header(type);
            lines[release] = Read(type, inherited, filter);
        }

        if (found.Count > 1)
        {
            Console.WriteLine("'" + query + "' names more than one type - say which:");
            foreach (var name in found) Console.WriteLine("  " + name);
            return 2;
        }

        if (found.Count == 0)
        {
            Console.WriteLine("No public type called '" + query + "' in any release read (" +
                              string.Join(", ", releases) + ").");
            if (near.Count > 0)
            {
                Console.WriteLine("Types whose name contains '" + Last(query) + "':");
                foreach (var name in near.Take(15)) Console.WriteLine("  " + name);
                if (near.Count > 15) Console.WriteLine("  ... and " + (near.Count - 15) + " more");
            }
            return 1;
        }

        var full = found.First();
        Console.WriteLine(full + (member == null ? "" : "   - only members named " + member));
        Console.WriteLine("read " + releases.Count + " release(s): " + string.Join(", ", releases) +
                          (from == null ? "" : "   (" + from + ")"));

        var has = releases.Where(headers.ContainsKey).ToList();
        var lacks = releases.Where(r => !headers.ContainsKey(r)).ToList();
        // In release order, so a type that changed shape reads oldest first.
        foreach (var group in has.GroupBy(r => headers[r]))
            Console.WriteLine("  " + Span(group.ToList(), releases) + "  " + group.Key);
        if (lacks.Count > 0)
            Console.WriteLine("  " + Span(lacks, releases) + "  (the type is not in these)");
        Console.WriteLine();

        // Every distinct line, in the order a reader looks for them, each
        // with the releases that have it EXACTLY as written.
        var seen = new Dictionary<string, Line>();
        var where = new Dictionary<string, List<string>>();
        var marked = new Dictionary<string, List<KeyValuePair<string, string>>>();
        foreach (var release in has)
        {
            foreach (var line in lines[release])
            {
                if (!seen.ContainsKey(line.Key))
                {
                    seen[line.Key] = line;
                    where[line.Key] = new List<string>();
                    marked[line.Key] = new List<KeyValuePair<string, string>>();
                }
                where[line.Key].Add(release);
                if (line.Obsolete != null)
                    marked[line.Key].Add(new KeyValuePair<string, string>(release, line.Obsolete));
            }
        }

        if (seen.Count == 0)
        {
            Console.WriteLine(member == null
                ? "  no public members" + (inherited ? "" : " declared on it - try --inherited")
                : "  no public member named " + member + (inherited ? "" : " declared on it - try --inherited"));
            return 0;
        }

        var ordered = seen.Values
            .OrderBy(l => l.Rank).ThenBy(l => l.Order).ThenBy(l => l.Name, StringComparer.Ordinal)
            .ThenBy(l => releases.IndexOf(where[l.Key][0])).ThenBy(l => l.Text, StringComparer.Ordinal)
            .ToList();
        var width = ordered.Max(l => Span(where[l.Key], releases).Length);

        foreach (var line in ordered)
        {
            var span = Span(where[line.Key], releases);
            Console.WriteLine("  " + span.PadRight(width) + "  " + line.Kind.PadRight(8) + "  " + line.Text +
                              (line.From == null ? "" : "   (from " + line.From + ")"));
            foreach (var mark in marked[line.Key].GroupBy(m => m.Value))
            {
                Console.WriteLine("  " + new string(' ', width) + "  " + new string(' ', 8) + "  " +
                                  "OBSOLETE in " + Span(mark.Select(m => m.Key).ToList(), releases) +
                                  ": " + mark.Key);
            }
        }

        Console.WriteLine();
        Console.WriteLine(ordered.Count + " distinct line(s). A line whose releases are fewer than the");
        Console.WriteLine("type's was added, removed or RE-SIGNED in between - its other form is the");
        Console.WriteLine("line beside it. Public members only" +
                          (inherited ? ", inherited ones marked (from ...)." : ", declared on this type."));
        return 0;
    }

    // ------------------------------------------------------------ finding

    /// <summary>
    /// The cache the other two modes fill. From the repository root when run
    /// there, else from beside this tool's own project file - so the command
    /// works whichever way it was started.
    /// </summary>
    static string Cache()
    {
        var here = Path.Combine(Directory.GetCurrentDirectory(), "tools", "api-surface", ".assemblies");
        if (Directory.Exists(here)) return here;

        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ApiSurface.csproj")))
            {
                var cache = Path.Combine(dir.FullName, ".assemblies");
                return Directory.Exists(cache) ? cache : null;
            }
        }
        return null;
    }

    static IEnumerable<Type> Loaded(Assembly asm)
    {
        Type[] types;
        // A release whose reference assembly cannot fully resolve is still
        // worth reading for the types that did load - the same reasoning the
        // --dump mode gives.
        try { types = asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray(); }
        return types.Where(t => t != null && (t.IsPublic || t.IsNestedPublic));
    }

    /// <summary>
    /// The types `query` names, and the member filter if it named one.
    ///
    /// Tried in order: the full name; the name a person types for a nested
    /// type (Outer.Inner); the short name. Only when NONE of those finds a
    /// type is the last segment taken as a member - so `ElementId.Value`
    /// means the member Value of ElementId, while a nested type of the same
    /// shape still wins when one exists.
    /// </summary>
    static (List<Type>, string) Match(List<Type> types, string query)
    {
        var hits = Find(types, query);
        if (hits.Count > 0) return (hits, null);

        var dot = query.LastIndexOf('.');
        if (dot > 0 && dot < query.Length - 1)
        {
            hits = Find(types, query.Substring(0, dot));
            if (hits.Count > 0) return (hits, query.Substring(dot + 1));
        }
        return (new List<Type>(), null);
    }

    static List<Type> Find(List<Type> types, string name)
    {
        var full = types.Where(t => t.FullName == name || Nested(t) == name).ToList();
        if (full.Count > 0) return full;

        var shortName = types.Where(t => ShortNested(t) == name).ToList();
        if (shortName.Count > 0) return shortName;

        return types.Where(t => t.Name == name).ToList();
    }

    static string Last(string query)
    {
        var dot = query.LastIndexOf('.');
        return dot >= 0 && dot < query.Length - 1 ? query.Substring(dot + 1) : query;
    }

    /// <summary>A.B.Outer.Inner - the full name as C# spells it.</summary>
    static string Nested(Type t) => (t.FullName ?? t.Name).Replace('+', '.');

    /// <summary>Outer.Inner - the name without its namespace.</summary>
    static string ShortNested(Type t) =>
        t.IsNested && t.DeclaringType != null ? ShortNested(t.DeclaringType) + "." + t.Name : t.Name;

    // ------------------------------------------------------------ reading

    static string Header(Type type)
    {
        var parts = new List<string> { "public" };
        try
        {
            if (type.IsEnum) parts.Add("enum");
            else if (type.IsInterface) parts.Add("interface");
            else if (type.IsValueType) parts.Add("struct");
            else if (type.BaseType != null && type.BaseType.FullName == "System.MulticastDelegate") parts.Add("delegate");
            else
            {
                if (type.IsAbstract && type.IsSealed) parts.Add("static");
                else if (type.IsAbstract) parts.Add("abstract");
                else if (type.IsSealed) parts.Add("sealed");
                parts.Add("class");
            }

            if (!type.IsEnum && !type.IsValueType && !type.IsInterface && type.BaseType != null &&
                type.BaseType.FullName != "System.Object" && type.BaseType.FullName != "System.MulticastDelegate")
                parts.Add(": " + Name(type.BaseType));
        }
        catch (Exception ex) { parts.Add("(its kind could not be read here: " + Why(ex) + ")"); }

        var obsolete = Obsolete(type);
        if (obsolete != null) parts.Add("   OBSOLETE: " + obsolete);
        return string.Join(" ", parts);
    }

    static List<Line> Read(Type type, bool inherited, string filter)
    {
        var lines = new List<Line>();
        MemberInfo[] members;
        try { members = type.GetMembers(inherited ? Everything : Declared); }
        catch (Exception ex)
        {
            lines.Add(new Line { Rank = 9, Kind = "?", Name = "", Text = "its members could not be read here: " + Why(ex) });
            return lines;
        }

        foreach (var member in members)
        {
            if (filter != null && member.Name != filter && !(member is ConstructorInfo && filter == "ctor")) continue;

            Line line;
            try { line = Describe(type, member); }
            catch (Exception ex)
            {
                // NEVER DROPPED. A member whose signature names a type this
                // machine cannot resolve - WPF's, off Windows, for the ribbon
                // images in RevitAPIUI - is still a member, and leaving it out
                // would read as the release not having it. The missing
                // assembly is named WITHOUT its version, so one member reads
                // as one line rather than one per .NET generation.
                line = new Line { Rank = RankOf(member), Kind = KindOf(member), Name = member.Name,
                                  Text = member.Name + "   (" + Unresolved(ex) + ")" };
            }
            if (line == null) continue;

            if (inherited && member.DeclaringType != null && member.DeclaringType != type)
            {
                if (member.DeclaringType.FullName == "System.Object") continue;
                line.From = ShortNested(member.DeclaringType);
            }
            lines.Add(line);
        }
        return lines;
    }

    /// <summary>One member as C# would declare it, or null for one nobody calls by name.</summary>
    static Line Describe(Type owner, MemberInfo member)
    {
        switch (member)
        {
            case ConstructorInfo ctor:
                if (ctor.IsStatic) return null;          // a type initialiser is not something to call
                return new Line { Rank = 0, Kind = "ctor", Name = owner.Name,
                                  Text = "new " + ShortNested(owner) + "(" + Parameters(ctor.GetParameters()) + ")",
                                  Obsolete = Obsolete(ctor) };

            case PropertyInfo property:
            {
                var get = property.GetGetMethod(false);
                var set = property.GetSetMethod(false);
                var index = property.GetIndexParameters();
                var access = (get != null ? "get; " : "") + (set != null ? "set; " : "");
                var isStatic = (get ?? set)?.IsStatic == true;
                // A NAMED INDEXED PROPERTY IS NOT AN INDEXER. The Revit API is
                // C++/CLI and has several - Element.BoundingBox[View] is one -
                // and C# cannot call them by name: it calls get_BoundingBox(view).
                // Printing `this[View]` for it would send a reader to write
                // element[view], which does not compile. Only one called Item
                // is the real `this[...]`.
                var name = index.Length == 0 ? property.Name
                         : property.Name == "Item" ? "this[" + Parameters(index) + "]"
                         : property.Name + "[" + Parameters(index) + "]";
                var calledAs = index.Length == 0 || property.Name == "Item" ? ""
                             : "   - in C#, get_" + property.Name + "(...)" +
                               (set != null ? " and set_" + property.Name + "(...)" : "");
                return new Line { Rank = 1, Kind = "property", Name = property.Name,
                                  Text = (isStatic ? "static " : "") + Name(property.PropertyType) + " " + name +
                                         " { " + access + "}" + calledAs,
                                  Obsolete = Obsolete(property) ?? Obsolete(get) ?? Obsolete(set) };
            }

            case MethodInfo method:
                // get_X, set_X, add_X, remove_X, op_ - the property, event or
                // operator already says it, in the form a caller writes.
                if (method.IsSpecialName && !method.Name.StartsWith("op_", StringComparison.Ordinal)) return null;
                var op = Operator(method.Name);
                if (op != null)
                {
                    var conversion = op == "implicit" || op == "explicit";
                    return new Line { Rank = 2, Kind = "operator", Name = method.Name,
                                      Text = "static " + (conversion
                                                 ? op + " operator " + Name(method.ReturnType)
                                                 : Name(method.ReturnType) + " operator " + op) +
                                             "(" + Parameters(method.GetParameters()) + ")",
                                      Obsolete = Obsolete(method) };
                }
                return new Line { Rank = 2, Kind = "method", Name = method.Name,
                                  Text = (method.IsStatic ? "static " : "") + Name(method.ReturnType) + " " +
                                         method.Name + Generic(method) + "(" + Parameters(method.GetParameters()) + ")",
                                  Obsolete = Obsolete(method) };

            case FieldInfo field:
                if (field.IsSpecialName) return null;    // an enum's value__
                if (owner.IsEnum && field.IsStatic)
                {
                    var raw = field.GetRawConstantValue();
                    long order = 0;
                    try { order = Convert.ToInt64(raw, CultureInfo.InvariantCulture); }
                    catch (OverflowException) { order = long.MaxValue; }
                    return new Line { Rank = 3, Kind = "value", Name = field.Name, Order = order,
                                      Text = field.Name + " = " + Constant(raw),
                                      Obsolete = Obsolete(field) };
                }
                return new Line { Rank = 3, Kind = "field", Name = field.Name,
                                  Text = (field.IsLiteral ? "const " : field.IsStatic ? "static " : "") +
                                         (field.IsInitOnly ? "readonly " : "") + Name(field.FieldType) + " " + field.Name +
                                         (field.IsLiteral ? " = " + Constant(field.GetRawConstantValue()) : ""),
                                  Obsolete = Obsolete(field) };

            case EventInfo evt:
                return new Line { Rank = 4, Kind = "event", Name = evt.Name,
                                  Text = "event " + Name(evt.EventHandlerType) + " " + evt.Name,
                                  Obsolete = Obsolete(evt) ?? Obsolete(evt.GetAddMethod(false)) };

            case Type nested:
                return new Line { Rank = 5, Kind = "type", Name = nested.Name,
                                  Text = ShortNested(nested) + "   - look it up by that name",
                                  Obsolete = Obsolete(nested) };
        }
        return null;
    }

    static string Parameters(ParameterInfo[] parameters)
    {
        var parts = new List<string>(parameters.Length);
        foreach (var p in parameters)
        {
            var type = p.ParameterType;
            var prefix = "";
            if (type.IsByRef)
                prefix = p.IsOut ? "out " : "ref ";
            else if (IsParams(p))
                prefix = "params ";

            var text = prefix + Name(type) + " " + (string.IsNullOrEmpty(p.Name) ? "arg" + p.Position : p.Name);
            if (p.IsOptional)
            {
                object value = null;
                try { value = p.RawDefaultValue; } catch (Exception) { }
                text += " = " + (value == null || value is DBNull || value == Missing.Value
                                    ? "default" : Constant(value));
            }
            parts.Add(text);
        }
        return string.Join(", ", parts);
    }

    static int RankOf(MemberInfo member) =>
        member is ConstructorInfo ? 0 : member is PropertyInfo ? 1 : member is MethodInfo ? 2 :
        member is FieldInfo ? 3 : member is EventInfo ? 4 : 5;

    static string KindOf(MemberInfo member) =>
        member is ConstructorInfo ? "ctor" : member is PropertyInfo ? "property" : member is MethodInfo ? "method" :
        member is FieldInfo ? "field" : member is EventInfo ? "event" : "type";

    /// <summary>The C# spelling of an operator method, or null for an ordinary method.</summary>
    static string Operator(string name)
    {
        switch (name)
        {
            case "op_Equality": return "==";
            case "op_Inequality": return "!=";
            case "op_LessThan": return "<";
            case "op_GreaterThan": return ">";
            case "op_LessThanOrEqual": return "<=";
            case "op_GreaterThanOrEqual": return ">=";
            case "op_Addition": return "+";
            case "op_Subtraction": return "-";
            case "op_Multiply": return "*";
            case "op_Division": return "/";
            case "op_UnaryNegation": return "-";
            case "op_Implicit": return "implicit";
            case "op_Explicit": return "explicit";
        }
        return null;
    }

    /// <summary>
    /// Why a signature could not be read, without the version number that
    /// would split one member into a line per .NET generation.
    /// </summary>
    static string Unresolved(Exception ex)
    {
        var said = Why(ex);
        var match = System.Text.RegularExpressions.Regex.Match(said, "Could not find assembly '([^,']+)");
        return match.Success
            ? "its signature names a type from " + match.Groups[1].Value +
              ", which this machine has not got - read this one on Windows"
            : "its signature could not be read here: " + said;
    }

    static bool IsParams(ParameterInfo p)
    {
        try
        {
            return p.GetCustomAttributesData()
                    .Any(a => a.AttributeType.FullName == "System.ParamArrayAttribute");
        }
        catch (Exception) { return false; }
    }

    static string Generic(MethodInfo method) =>
        method.IsGenericMethodDefinition
            ? "<" + string.Join(", ", method.GetGenericArguments().Select(a => a.Name)) + ">"
            : "";

    /// <summary>
    /// `[Obsolete]`'s message, "ERROR: " first when it is marked as an error
    /// rather than a warning, or null when the member is not marked. Read from
    /// the attribute DATA, because a metadata-only load cannot construct the
    /// attribute itself - and never allowed to throw, because a mark that
    /// cannot be read must not cost the line it sits on.
    /// </summary>
    static string Obsolete(MemberInfo member)
    {
        if (member == null) return null;
        try
        {
            foreach (var data in member.GetCustomAttributesData())
            {
                string name;
                try { name = data.AttributeType.FullName; }
                catch (Exception) { continue; }
                if (name != "System.ObsoleteAttribute") continue;

                var args = data.ConstructorArguments;
                var message = args.Count > 0 ? args[0].Value as string : null;
                var error = args.Count > 1 && args[1].Value is bool isError && isError;
                return (error ? "ERROR: " : "") +
                       (string.IsNullOrEmpty(message) ? "(no message)" : "\"" + message.Trim() + "\"");
            }
        }
        catch (Exception ex) { return "(could not be read here: " + Why(ex) + ")"; }
        return null;
    }

    /// <summary>A type as a person writes it in C#: short, with its arguments.</summary>
    static string Name(Type type)
    {
        if (type == null) return "?";
        if (type.IsByRef) return Name(type.GetElementType());
        if (type.IsPointer) return Name(type.GetElementType()) + "*";
        if (type.IsArray)
            return Name(type.GetElementType()) + "[" + new string(',', type.GetArrayRank() - 1) + "]";
        if (type.IsGenericParameter) return type.Name;

        if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            var args = type.GetGenericArguments();
            if (definition.FullName == "System.Nullable`1") return Name(args[0]) + "?";
            var bare = ShortNested(definition);
            var tick = bare.IndexOf('`');
            if (tick >= 0) bare = bare.Substring(0, tick);
            return bare + "<" + string.Join(", ", args.Select(Name)) + ">";
        }

        switch (type.FullName)
        {
            case "System.Void": return "void";
            case "System.Boolean": return "bool";
            case "System.Byte": return "byte";
            case "System.SByte": return "sbyte";
            case "System.Char": return "char";
            case "System.Int16": return "short";
            case "System.UInt16": return "ushort";
            case "System.Int32": return "int";
            case "System.UInt32": return "uint";
            case "System.Int64": return "long";
            case "System.UInt64": return "ulong";
            case "System.Single": return "float";
            case "System.Double": return "double";
            case "System.Decimal": return "decimal";
            case "System.String": return "string";
            case "System.Object": return "object";
        }
        return ShortNested(type);
    }

    static string Constant(object value)
    {
        switch (value)
        {
            case null: return "null";
            case string s: return "\"" + s + "\"";
            case bool b: return b ? "true" : "false";
            case char c: return "'" + c + "'";
            case IFormattable f: return f.ToString(null, CultureInfo.InvariantCulture);
            default: return value.ToString();
        }
    }

    /// <summary>
    /// Releases as a reader wants them: runs of consecutive years collapsed,
    /// "2020-2027", and anything else listed. A run is collapsed only when
    /// the years really are consecutive - two cached releases, 2020 and
    /// 2024, must never print as "2020-2024" and claim the three between.
    /// </summary>
    static string Span(List<string> have, List<string> order)
    {
        var sorted = have.OrderBy(order.IndexOf).ToList();
        var parts = new List<string>();
        var i = 0;
        while (i < sorted.Count)
        {
            var j = i;
            while (j + 1 < sorted.Count && Next(sorted[j], sorted[j + 1])) j++;
            parts.Add(j == i ? sorted[i] : sorted[i] + "-" + sorted[j]);
            i = j + 1;
        }
        return string.Join(", ", parts);
    }

    static bool Next(string a, string b) =>
        int.TryParse(a, NumberStyles.None, CultureInfo.InvariantCulture, out var x) &&
        int.TryParse(b, NumberStyles.None, CultureInfo.InvariantCulture, out var y) &&
        y == x + 1;

    static string Why(Exception ex)
    {
        var inner = ex;
        while (inner.InnerException != null) inner = inner.InnerException;
        var text = inner.Message ?? inner.GetType().Name;
        var line = text.IndexOf('\n');
        return (line >= 0 ? text.Substring(0, line) : text).Trim();
    }
}
