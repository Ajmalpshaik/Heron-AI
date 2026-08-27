// Heron-Agent:  none
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  bridge
// See docs/29-metadata-standard.md

using System;
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
    internal static class Json
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

        private static string Escape(string value)
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
