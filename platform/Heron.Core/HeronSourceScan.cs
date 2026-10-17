// Heron-Agent:  none
// Heron-Step:   17
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System.Text;
using System.Text.RegularExpressions;

namespace Heron.Core
{
    /// <summary>
    /// What a fragment's own C# source opens, read before it is compiled -
    /// for the one question the read and show paths cannot leave to Revit.
    ///
    /// THOSE PATHS STAND ON ONE SENTENCE: no transaction is open, so Revit
    /// refuses every change. A fragment that opens a Transaction of its own
    /// walks straight through it - with none open outside, Revit lets it start
    /// one and commit. D-112's edit-mode fragments do exactly that on purpose
    /// and declare it, and the add-in keeps them to the write path. One that
    /// does it without declaring it, or that declared noTransactionOpen - a
    /// promise to open none (D-125) - is asked here, before anything runs.
    ///
    /// READ AS CODE, NOT AS TEXT. Comments and string literals are blanked
    /// first, so a header saying "IT CANNOT RUN INSIDE A TRANSACTION" or a
    /// message naming one is not mistaken for opening one.
    ///
    /// WHAT IT CANNOT SEE: a transaction opened through reflection, or by a
    /// method of another assembly the fragment calls. It stops a fragment that
    /// says what it does; Revit's own refusal, and the read path's count of
    /// the model before and after (row 5b-350), stay behind it. Revit-free, so
    /// tests/Heron.Kernel.TestHost runs it.
    /// </summary>
    public static class HeronSourceScan
    {
        private static readonly Regex Opened = new Regex(
            @"\bnew\s+(?:Autodesk\.Revit\.DB\.)?(Transaction|TransactionGroup|SubTransaction)\s*\("
            + @"|\b(Transaction|TransactionGroup|SubTransaction)\s+@?\w+\s*=\s*new\s*\(",
            RegexOptions.CultureInvariant);

        /// <summary>
        /// The Revit transaction class the source constructs - "Transaction",
        /// "TransactionGroup" or "SubTransaction" - or null when it constructs
        /// none. Null for null or empty source.
        /// </summary>
        public static string OpensTransaction(string source)
        {
            if (string.IsNullOrEmpty(source)) return null;
            var match = Opened.Match(CodeOnly(source));
            if (!match.Success) return null;
            return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        }

        /// <summary>
        /// The source with every comment and every string or character literal
        /// replaced by spaces - line breaks kept, so nothing moves line. Regular,
        /// verbatim (@"..."), interpolated ($"...") and both together.
        /// </summary>
        public static string CodeOnly(string source)
        {
            if (string.IsNullOrEmpty(source)) return source ?? "";

            var code = new StringBuilder(source.Length);
            var i = 0;
            while (i < source.Length)
            {
                var c = source[i];
                var next = i + 1 < source.Length ? source[i + 1] : '\0';

                if (c == '/' && next == '/')
                {
                    while (i < source.Length && source[i] != '\n') { code.Append(' '); i++; }
                    continue;
                }
                if (c == '/' && next == '*')
                {
                    code.Append("  ");
                    i += 2;
                    while (i < source.Length && !(source[i] == '*' && i + 1 < source.Length
                                                  && source[i + 1] == '/'))
                    {
                        code.Append(source[i] == '\n' ? '\n' : ' ');
                        i++;
                    }
                    if (i < source.Length) { code.Append("  "); i += 2; }
                    continue;
                }

                // A STRING: its prefix ($, @, or both) and its body blanked.
                var start = i;
                var verbatim = false;
                while (i < source.Length && (source[i] == '$' || source[i] == '@'))
                {
                    if (source[i] == '@') verbatim = true;
                    i++;
                }
                if (i < source.Length && source[i] == '"' && (i > start || c == '"'))
                {
                    for (var k = start; k <= i; k++) code.Append(' ');
                    i++;
                    while (i < source.Length)
                    {
                        if (verbatim && source[i] == '"' && i + 1 < source.Length
                            && source[i + 1] == '"')
                        {
                            code.Append("  ");
                            i += 2;
                            continue;
                        }
                        if (!verbatim && source[i] == '\\' && i + 1 < source.Length)
                        {
                            code.Append("  ");
                            i += 2;
                            continue;
                        }
                        if (source[i] == '"') { code.Append(' '); i++; break; }
                        code.Append(source[i] == '\n' ? '\n' : ' ');
                        i++;
                    }
                    continue;
                }
                i = start;

                if (c == '\'')
                {
                    code.Append(' ');
                    i++;
                    while (i < source.Length && source[i] != '\'' && source[i] != '\n')
                    {
                        if (source[i] == '\\' && i + 1 < source.Length) { code.Append(' '); i++; }
                        code.Append(' ');
                        i++;
                    }
                    if (i < source.Length && source[i] == '\'') { code.Append(' '); i++; }
                    continue;
                }

                code.Append(c);
                i++;
            }
            return code.ToString();
        }
    }
}
