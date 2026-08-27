// Heron-Agent:  none
// Heron-Step:   1
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  bridge
// See docs/29-metadata-standard.md

using System;
using System.Text;

namespace Heron.Bridge
{
    /// <summary>
    /// Deliberately tiny JSON helpers.
    ///
    /// Step 1's wire format is two fields deep. Pulling in a serializer would
    /// mean shipping a dependency into Revit's assembly load context, where
    /// version conflicts with other add-ins are a known and unpleasant class
    /// of bug. When the protocol grows past trivial - Step 2 or 3 - replace
    /// this with a real serializer and a versioned schema, and delete it
    /// without ceremony.
    /// </summary>
    internal static class Json
    {
        public static string Ok(string bodyFields)
        {
            if (string.IsNullOrEmpty(bodyFields))
                return "{\"ok\": true}";
            return "{\"ok\": true, " + bodyFields + "}";
        }

        public static string Error(string code, string message)
        {
            return "{\"ok\": false, \"error\": \"" + Escape(code) +
                   "\", \"message\": \"" + Escape(message) + "\"}";
        }

        /// <summary>
        /// Reads a top-level string value. Good enough for a flat, one-line
        /// request; not a parser, and not pretending to be one.
        /// </summary>
        public static string ReadString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return null;

            var needle = "\"" + key + "\"";
            var i = json.IndexOf(needle, StringComparison.Ordinal);
            if (i < 0) return null;

            i = json.IndexOf(':', i + needle.Length);
            if (i < 0) return null;
            i++;

            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
            if (i >= json.Length || json[i] != '"') return null;
            i++;

            var sb = new StringBuilder();
            while (i < json.Length && json[i] != '"')
            {
                if (json[i] == '\\' && i + 1 < json.Length)
                {
                    i++;
                    switch (json[i])
                    {
                        case 'n': sb.Append('\n'); break;
                        case 't': sb.Append('\t'); break;
                        case 'r': sb.Append('\r'); break;
                        default: sb.Append(json[i]); break;
                    }
                }
                else
                {
                    sb.Append(json[i]);
                }
                i++;
            }
            return sb.ToString();
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", " ")
                .Replace("\n", " ");
        }
    }
}
