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

        /// <summary>
        /// A refusal that carries fields of its own beside the code and the
        /// message - the workflow id of a run still going, whether a rollback
        /// Revit was asked for held. A field that is null or empty is left out,
        /// as Ok leaves it out, so a caller can pass what it knows and nothing
        /// else. The code and the message always come first and are never
        /// repeated: a field named "error" or "message" here would be a second
        /// key, which a reader may take either way.
        ///
        /// Added 2026-11-20 for FRAGMENT-ISSUES 5b rows 1159 and 1559: until
        /// then an error reply had room for nothing a program could read
        /// except its code.
        /// </summary>
        public static string Error(string code, string message, params string[] fields)
        {
            var plain = Error(code, message);
            if (fields == null || fields.Length == 0) return plain;

            var sb = new StringBuilder(plain, 0, plain.Length - 1, plain.Length + 64);
            foreach (var field in fields)
            {
                if (string.IsNullOrEmpty(field)) continue;
                sb.Append(", ").Append(field);
            }
            return sb.Append('}').ToString();
        }

        /// <summary>
        /// A REFUSAL AFTER A ROLLBACK, SAYING WHETHER THE ROLLBACK HELD - the
        /// same two fields an answered write carries: `rolledBack` (Revit said
        /// it rolled back AND the rollback check found nothing left standing,
        /// for every rollback the job asked for) and `rollbackReported`
        /// (Revit said so, for every one). FRAGMENT-ISSUES 5b row 1559: until
        /// 2026-11-20 an error reply had no field for either, so the chat could
        /// tell a rollback Revit confirmed from one it did not only by reading
        /// a sentence.
        ///
        /// Changes nothing that is not one of the add-in's own refusals
        /// (Error's opening), a job that asked for no rollback, or a reply that
        /// already says either at its top level - the path that wrote it knew
        /// more than a tally does, and two keys of one name may be read either
        /// way. Pure text, so it is proved without Revit by
        /// tests/Heron.FailureNote.TestHost.
        /// </summary>
        public static string WithRollback(string reply, int rollbacks, bool held, bool reported)
        {
            if (rollbacks <= 0 || string.IsNullOrEmpty(reply)) return reply;
            if (!reply.StartsWith("{\"ok\": false, ", StringComparison.Ordinal)) return reply;
            if (!reply.EndsWith("}", StringComparison.Ordinal)) return reply;
            if (HasKey(reply, "rolledBack") || HasKey(reply, "rollbackReported")) return reply;

            return reply.Substring(0, reply.Length - 1)
                 + ", " + Bool("rolledBack", held)
                 + ", " + Bool("rollbackReported", reported) + "}";
        }

        /// <summary>
        /// A REFUSAL FROM A JOB THAT WORKS OUTSIDE THE MODEL, ONCE ITS CODE HAS
        /// RUN, SAYING THAT WHAT IT DID OUT THERE STANDS - FRAGMENT-ISSUES 5b
        /// row 1933. An Admin or a Publish job that Heron's clock, a stop, a
        /// Revit dialog or the run guard ends part way had its group rolled
        /// back, or - D-192 - had no group at all; a file it already wrote, or
        /// a document it already made, saved or closed, is outside every
        /// rollback. Until 2026-11-21 the refusal carried no field for that,
        /// and the chat's outcome line said only ROLLED BACK.
        ///
        /// Adds `outsideStands` true - the field a verdict of the same kind of
        /// job carries (row 5b-1594) - and `ranOutsideTheModel` true when the
        /// job ran with no group open in its model (D-192). And `sentence` to
        /// the message, unless the message already says it: `mark` is the
        /// words every such sentence of the add-in's starts with.
        ///
        /// Changes nothing that is not one of the add-in's own refusals
        /// (Error's opening), and nothing that already says either field at
        /// its top level - its writer knew more. Pure text, so it is proved
        /// without Revit by tests/Heron.FailureNote.TestHost; which refusals
        /// it is asked of is the caller's (RevitFragment.OutsideStood).
        /// </summary>
        public static string WithOutsideStands(string reply, bool ranOutsideTheModel,
                                               string sentence, string mark)
        {
            if (string.IsNullOrEmpty(reply)) return reply;
            if (!reply.StartsWith("{\"ok\": false, ", StringComparison.Ordinal)) return reply;
            if (!reply.EndsWith("}", StringComparison.Ordinal)) return reply;
            if (HasKey(reply, "outsideStands") || HasKey(reply, "ranOutsideTheModel")) return reply;

            var said = reply;
            var message = ReadString(reply, "message");
            var saysItAlready = !string.IsNullOrEmpty(message) && !string.IsNullOrEmpty(mark)
                                && message.IndexOf(mark, StringComparison.Ordinal) >= 0;
            if (!string.IsNullOrEmpty(sentence) && !saysItAlready)
            {
                var words = string.IsNullOrEmpty(message) ? sentence : message + " " + sentence;
                said = Set(reply, "message", "\"" + Escape(words) + "\"");
            }

            return said.Substring(0, said.Length - 1)
                 + ", " + Bool("outsideStands", true)
                 + (ranOutsideTheModel ? ", " + Bool("ranOutsideTheModel", true) : "") + "}";
        }

        /// <summary>
        /// The document with its top-level `key` set to `value` - raw JSON
        /// text, as Obj or a quoted string writes it - replacing every earlier
        /// top-level `key` rather than adding a second. Returned unchanged when
        /// it does not parse as one object, or when key or value is missing.
        ///
        /// Added 2026-11-20 for FRAGMENT-ISSUES 5b row 1163: a run record is
        /// written once when its job ends, and an undo of the run seen later is
        /// stamped onto it - the latest only, so a record undone, redone and
        /// undone again says undone. The separators between the fields it
        /// keeps are rewritten as ", "; their text is kept whole.
        /// </summary>
        public static string Set(string json, string key, string value)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key) || value == null) return json;

            var without = Without(json, key);
            if (without == null) return json;

            var body = without.Substring(0, without.Length - 1);
            var empty = body.TrimEnd().EndsWith("{", StringComparison.Ordinal);
            return body + (empty ? "" : ", ") + "\"" + Escape(key) + "\": " + value + "}";
        }

        /// <summary>
        /// The outermost object with every `key` at its top level taken out,
        /// or null when it does not parse. Ends with its closing brace; what
        /// follows that brace is not kept.
        /// </summary>
        private static string Without(string json, string key)
        {
            var i = SkipWhitespace(json, 0);
            if (i >= json.Length || json[i] != '{') return null;

            var sb = new StringBuilder(json.Length + 16);
            sb.Append('{');
            i++;
            var first = true;

            while (true)
            {
                i = SkipWhitespace(json, i);
                if (i >= json.Length) return null;
                if (json[i] == '}') return sb.Append('}').ToString();
                if (json[i] != '"') return null;

                var start = i;
                string name;
                i = ReadStringToken(json, i, out name);
                if (i < 0) return null;

                i = SkipWhitespace(json, i);
                if (i >= json.Length || json[i] != ':') return null;
                i = SkipWhitespace(json, i + 1);
                if (i >= json.Length) return null;

                if (json[i] == '"')
                {
                    string ignored;
                    i = ReadStringToken(json, i, out ignored);
                }
                else
                {
                    i = SkipValue(json, i);
                }
                if (i < 0) return null;

                if (!string.Equals(name, key, StringComparison.Ordinal))
                {
                    if (!first) sb.Append(", ");
                    sb.Append(json, start, i - start);
                    first = false;
                }

                i = SkipWhitespace(json, i);
                if (i >= json.Length) return null;
                if (json[i] == ',') { i++; continue; }
                if (json[i] == '}') return sb.Append('}').ToString();
                return null;
            }
        }

        /// <summary>
        /// Whether the outermost object holds `key`, whatever its value. The
        /// same walk as ReadString - outermost object only - so a key inside
        /// a nested value, or spelled inside a string, is not a match. False
        /// for a document that does not parse as far as the key.
        /// </summary>
        public static bool HasKey(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return false;

            var i = SkipWhitespace(json, 0);
            if (i >= json.Length || json[i] != '{') return false;
            i++;

            while (true)
            {
                i = SkipWhitespace(json, i);
                if (i >= json.Length || json[i] == '}') return false;
                if (json[i] != '"') return false;

                string name;
                i = ReadStringToken(json, i, out name);
                if (i < 0) return false;
                if (string.Equals(name, key, StringComparison.Ordinal)) return true;

                i = SkipWhitespace(json, i);
                if (i >= json.Length || json[i] != ':') return false;
                i = SkipWhitespace(json, i + 1);
                if (i >= json.Length) return false;

                if (json[i] == '"')
                {
                    string ignored;
                    i = ReadStringToken(json, i, out ignored);
                }
                else
                {
                    i = SkipValue(json, i);
                }
                if (i < 0) return false;

                i = SkipWhitespace(json, i);
                if (i >= json.Length || json[i] != ',') return false;
                i++;
            }
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
        /// Reads a top-level WHOLE NUMBER, or null if the key is absent, is
        /// not a number, is a number with a fraction or an exponent, does not
        /// fit a long, or the document does not parse as far as the key.
        ///
        /// ADDED 2026-10-09 FOR FRAGMENT-ISSUES section 5b, row 159. A fragment
        /// write's reply carries `warnings` - how many warnings Revit raised
        /// and Heron dismissed - as a number written by Num above, and
        /// ReadString answers null for every number by design. So the audit
        /// line, which reads its other fields off the reply with ReadString,
        /// had no way to keep it: the trail that exists to answer "what
        /// happened to my model" said a fragment ran and not that Revit warned.
        ///
        /// THE SAME WALK AS ReadString - outermost object only, first match
        /// wins - so the two can never disagree about which key a document
        /// holds. NULL RATHER THAN 0 for anything it cannot read: "Revit raised
        /// none" and "the reply did not say" are different facts, and a reader
        /// that answered 0 for the second would record the first.
        /// </summary>
        public static long? ReadLong(string json, string key)
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
                    // A string, even one that spells a number, is not a
                    // number. If this is the key we wanted, the answer is null.
                    if (matched) return null;
                    string ignored;
                    i = ReadStringToken(json, i, out ignored);
                    if (i < 0) return null;
                }
                else
                {
                    var start = i;
                    i = SkipValue(json, i);
                    if (i < 0) return null;
                    if (matched) return WholeNumber(json.Substring(start, i - start));
                }

                i = SkipWhitespace(json, i);
                if (i >= json.Length) return null;
                if (json[i] == ',') { i++; continue; }
                return null;                              // '}', or malformed
            }
        }

        /// <summary>
        /// One JSON integer - an optional minus sign and digits, nothing else -
        /// as a long, or null. "3.0", "1e3", "+3", true, null, an object and
        /// an array are all refused rather than rounded or guessed at.
        /// </summary>
        private static long? WholeNumber(string token)
        {
            token = token.Trim();
            var first = token.Length > 0 && token[0] == '-' ? 1 : 0;
            if (first >= token.Length) return null;
            for (var k = first; k < token.Length; k++)
            {
                if (token[k] < '0' || token[k] > '9') return null;
            }

            long value;
            if (!long.TryParse(token, NumberStyles.AllowLeadingSign,
                               CultureInfo.InvariantCulture, out value)) return null;
            return value;
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
