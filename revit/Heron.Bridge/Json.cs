// Heron-Agent:  none
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  bridge
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Heron.Bridge
{
    /// <summary>
    /// A small, correct JSON reader and writer for the bridge protocol.
    ///
    /// Deliberately hand-written rather than pulled from a package: a
    /// serializer would be loaded into Revit's assembly load context, where
    /// version conflicts with other add-ins are a known and unpleasant class
    /// of bug. The bridge speaks one flat object per line, so the whole of
    /// what is needed fits here.
    ///
    /// This replaced a version that found a key by searching the raw text for
    /// it. That worked for {"op":"ping"} and would not have survived Step 2,
    /// where requests carry arguments: a search cannot tell a key from the
    /// same characters appearing inside somebody's value. This one parses.
    ///
    /// Every value written goes through Escape. A document name is user text
    /// and will contain a quote or a backslash eventually.
    /// </summary>
    public static class Json
    {
        // ------------------------------------------------------------ writing

        /// <summary>A quoted, escaped "name": "value" pair.</summary>
        public static string Str(string name, string value)
        {
            if (value == null) return "\"" + Escape(name) + "\": null";
            return "\"" + Escape(name) + "\": \"" + Escape(value) + "\"";
        }

        /// <summary>A "name": number pair. Never culture-dependent.</summary>
        public static string Num(string name, long value)
        {
            return "\"" + Escape(name) + "\": " +
                   value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>A "name": true/false pair.</summary>
        public static string Bool(string name, bool value)
        {
            return "\"" + Escape(name) + "\": " + (value ? "true" : "false");
        }

        /// <summary>
        /// One nested object built from the same field writers: {f1, f2}.
        ///
        /// Added for HERON-REVIT-LNK-015, which is the first operation whose
        /// answer is a LIST OF THINGS rather than a list of values. Before it,
        /// every answer was flat and several ids were sent as one
        /// comma-joined string - fine for ids, useless for records, because
        /// splitting "name,status,path,name,status,path" back into rows on
        /// the far side is a format nobody declared.
        /// </summary>
        public static string Obj(params string[] fields)
        {
            if (fields == null || fields.Length == 0) return "{}";

            var sb = new StringBuilder("{");
            var first = true;
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field)) continue;
                if (!first) sb.Append(", ");
                sb.Append(field);
                first = false;
            }
            return sb.Append('}').ToString();
        }

        /// <summary>
        /// A "name": [ ... ] pair. The items are written already - each is
        /// whatever Obj, Str or Num produced - so nothing is escaped twice.
        /// An empty list is written as [], never as null: nothing found and
        /// nothing asked are different answers.
        /// </summary>
        public static string Arr(string name, IList<string> items)
        {
            var sb = new StringBuilder("\"").Append(Escape(name)).Append("\": [");
            if (items != null)
            {
                var first = true;
                foreach (var item in items)
                {
                    if (string.IsNullOrEmpty(item)) continue;
                    if (!first) sb.Append(", ");
                    sb.Append(item);
                    first = false;
                }
            }
            return sb.Append(']').ToString();
        }

        /// <summary>A success response carrying zero or more fields.</summary>
        public static string Ok(params string[] fields)
        {
            if (fields == null || fields.Length == 0) return "{\"ok\": true}";

            var sb = new StringBuilder("{\"ok\": true");
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field)) continue;
                sb.Append(", ").Append(field);
            }
            return sb.Append('}').ToString();
        }

        /// <summary>
        /// A refusal. The code is for the client to branch on; the message is
        /// for a person to read.
        /// </summary>
        public static string Error(string code, string message)
        {
            return "{\"ok\": false, " + Str("error", code) + ", " +
                   Str("message", message) + "}";
        }

        // ------------------------------------------------------------ reading

        /// <summary>
        /// Reads a top-level string value, or null if the key is absent, is
        /// not a string, or the document does not parse.
        ///
        /// Only the outermost object is searched. A key of the same name
        /// nested inside a value is not a match, which is the whole reason
        /// this is a parser and not a search.
        /// </summary>
        public static string ReadString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;

            var i = SkipWhitespace(json, 0);
            if (i >= json.Length || json[i] != '{') return null;
            i++;

            while (true)
            {
                i = SkipWhitespace(json, i);
                if (i >= json.Length || json[i] == '}') return null;

                if (json[i] != '"') return null;          // a key must be a string
                string name;
                i = ReadStringToken(json, i, out name);
                if (i < 0) return null;

                i = SkipWhitespace(json, i);
                if (i >= json.Length || json[i] != ':') return null;
                i = SkipWhitespace(json, i + 1);
                if (i >= json.Length) return null;

                var matched = string.Equals(name, key, StringComparison.Ordinal);

                if (json[i] == '"')
                {
                    string value;
                    i = ReadStringToken(json, i, out value);
                    if (i < 0) return null;
                    if (matched) return value;
                }
                else
                {
                    // Not a string. If this is the key we wanted, it is not a
                    // string value, so the answer is null either way.
                    if (matched) return null;
                    i = SkipValue(json, i);
                    if (i < 0) return null;
                }

                i = SkipWhitespace(json, i);
                if (i >= json.Length) return null;
                if (json[i] == ',') { i++; continue; }
                if (json[i] == '}') return null;
                return null;                              // malformed
            }
        }

        /// <summary>
        /// Reads a top-level ARRAY OF FLAT OBJECTS, as a list of string maps.
        /// Returns null if the key is absent or is not such an array, and an
        /// EMPTY list if it is an array with nothing in it - the caller has to
        /// tell those apart, because "no needs were sent" and "this fragment
        /// needs nothing" are different facts and only one of them is a bug.
        ///
        /// This exists for `needs`, which is the fragment's contract crossing
        /// the wire. Its values are all strings - a name, a type, a source -
        /// so anything richer is skipped rather than half-read: a reader that
        /// quietly drops the part it did not understand is how a contract
        /// arrives looking complete and missing a term.
        ///
        /// TYPES CONTAIN COMMAS AND ANGLE BRACKETS. `IDictionary&lt;ElementId,
        /// double&gt;` is a real declared type in this library, which is
        /// exactly why the needs list is sent as JSON and parsed, rather than
        /// packed into a delimited string somebody would have to guess the
        /// escaping for.
        /// </summary>
        public static List<Dictionary<string, string>> ReadObjectArray(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;

            var i = SkipWhitespace(json, 0);
            if (i >= json.Length || json[i] != '{') return null;
            i++;

            while (true)
            {
                i = SkipWhitespace(json, i);
                if (i >= json.Length || json[i] == '}') return null;
                if (json[i] != '"') return null;

                string name;
                i = ReadStringToken(json, i, out name);
                if (i < 0) return null;

                i = SkipWhitespace(json, i);
                if (i >= json.Length || json[i] != ':') return null;
                i = SkipWhitespace(json, i + 1);
                if (i >= json.Length) return null;

                if (string.Equals(name, key, StringComparison.Ordinal))
                {
                    if (json[i] != '[') return null;      // present, wrong shape
                    return ReadFlatObjects(json, i);
                }

                if (json[i] == '"')
                {
                    string ignored;
                    i = ReadStringToken(json, i, out ignored);
                    if (i < 0) return null;
                }
                else
                {
                    i = SkipValue(json, i);
                    if (i < 0) return null;
                }

                i = SkipWhitespace(json, i);
                if (i >= json.Length) return null;
                if (json[i] == ',') { i++; continue; }
                return null;
            }
        }

        /// <summary>
        /// The array itself, starting at s[i] == '['. Non-string members of an
        /// object are stepped over; a member that is not an object at all
        /// makes the whole read fail rather than silently shortening the list.
        /// </summary>
        private static List<Dictionary<string, string>> ReadFlatObjects(string s, int i)
        {
            var items = new List<Dictionary<string, string>>();
            i = SkipWhitespace(s, i + 1);
            if (i < s.Length && s[i] == ']') return items;   // empty, and that is a fact

            while (i < s.Length)
            {
                i = SkipWhitespace(s, i);
                if (i >= s.Length || s[i] != '{') return null;
                i++;

                var item = new Dictionary<string, string>(StringComparer.Ordinal);

                i = SkipWhitespace(s, i);
                if (i < s.Length && s[i] == '}') { i++; }
                else
                {
                    while (true)
                    {
                        i = SkipWhitespace(s, i);
                        if (i >= s.Length || s[i] != '"') return null;

                        string field;
                        i = ReadStringToken(s, i, out field);
                        if (i < 0) return null;

                        i = SkipWhitespace(s, i);
                        if (i >= s.Length || s[i] != ':') return null;
                        i = SkipWhitespace(s, i + 1);
                        if (i >= s.Length) return null;

                        if (s[i] == '"')
                        {
                            string value;
                            i = ReadStringToken(s, i, out value);
                            if (i < 0) return null;
                            item[field] = value;
                        }
                        else
                        {
                            i = SkipValue(s, i);
                            if (i < 0) return null;
                        }

                        i = SkipWhitespace(s, i);
                        if (i >= s.Length) return null;
                        if (s[i] == ',') { i++; continue; }
                        if (s[i] == '}') { i++; break; }
                        return null;
                    }
                }

                items.Add(item);

                i = SkipWhitespace(s, i);
                if (i >= s.Length) return null;
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') return items;
                return null;
            }

            return null;                                  // unterminated
        }

        private static int SkipWhitespace(string s, int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            return i;
        }

        /// <summary>
        /// Reads one quoted string starting at s[i] == '"'. Returns the index
        /// just past the closing quote, or -1 if it never closes.
        /// </summary>
        private static int ReadStringToken(string s, int i, out string value)
        {
            value = null;
            if (i >= s.Length || s[i] != '"') return -1;
            i++;

            var sb = new StringBuilder();
            while (i < s.Length)
            {
                var c = s[i];

                if (c == '"')
                {
                    value = sb.ToString();
                    return i + 1;
                }

                if (c != '\\') { sb.Append(c); i++; continue; }

                i++;
                if (i >= s.Length) return -1;
                switch (s[i])
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case '/': sb.Append('/'); break;
                    case '\\': sb.Append('\\'); break;
                    case '"': sb.Append('"'); break;
                    case 'u':
                        if (i + 4 >= s.Length) return -1;
                        int code;
                        if (!int.TryParse(s.Substring(i + 1, 4),
                                          NumberStyles.HexNumber,
                                          CultureInfo.InvariantCulture, out code)) return -1;
                        sb.Append((char)code);
                        i += 4;
                        break;
                    default:
                        return -1;                        // not a valid escape
                }
                i++;
            }
            return -1;                                    // unterminated
        }

        /// <summary>
        /// Steps over a value that is not a string - number, literal, object or
        /// array - including anything nested inside it. Returns the index just
        /// past it, or -1 if it is malformed.
        /// </summary>
        private static int SkipValue(string s, int i)
        {
            if (i >= s.Length) return -1;

            if (s[i] == '{' || s[i] == '[')
            {
                var depth = 0;
                while (i < s.Length)
                {
                    var c = s[i];
                    if (c == '"')
                    {
                        string ignored;
                        i = ReadStringToken(s, i, out ignored);
                        if (i < 0) return -1;
                        continue;                         // quotes hide braces
                    }
                    if (c == '{' || c == '[') depth++;
                    else if (c == '}' || c == ']')
                    {
                        depth--;
                        if (depth == 0) return i + 1;
                    }
                    i++;
                }
                return -1;
            }

            while (i < s.Length && s[i] != ',' && s[i] != '}' && s[i] != ']') i++;
            return i;
        }

        // ----------------------------------------------------------- escaping

        /// <summary>
        /// One escaper for the whole assembly.
        ///
        /// INTERNAL RATHER THAN PRIVATE SINCE 2026-09-21. BridgeIdentity, in
        /// this same assembly and this same namespace, carried its own copy
        /// that handled only the backslash and the double quote - so a
        /// newline or a control character in any value would have written a
        /// discovery file that is not JSON, and the discovery file is what
        /// every client reads to find Revit. Nothing untrusted reaches it
        /// today, and that was checked; the defect is the second definition,
        /// which is the same drift HeronPermissions warns about in its own
        /// words. FRAGMENT-ISSUES section 5b, row 21.
        /// </summary>
        internal static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var sb = new StringBuilder(value.Length + 8);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        // Any remaining control character would make the line
                        // unparseable for the client.
                        if (c < ' ')
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}
