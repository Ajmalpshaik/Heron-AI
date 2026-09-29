// NOT STANDALONE. Assumes `elements`, `parameterNames` and `maxRows` are in
// scope, and leaves `tableJson`, `rowCount`, `truncated` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is set.
//
// ONE STRING, BECAUSE A LIST CROSSES AS THREE NAMES. The Companion page needs
// every row; a list output reaches the caller as its first three items
// (FRAGMENT-ISSUES 5b-195). So the table is built here as JSON, format 1, and
// crosses whole. The JSON is written by hand: net472 has no serialiser this
// code can rely on, and the shape is small and fixed.
//
// THE BACKSLASH AND THE QUOTE ARE BUILT FROM THEIR CHARACTER CODES, because the
// tools that write these files have eaten typed backslashes before (the
// owner's memory, "heredocs and Write eat backslashes").

var findings = new List<string>();
var truncated = 0;
var rowCount = 0;
var tableJson = "";

var cap = maxRows > 0 ? maxRows : 2000;
var backslash = ((char)92).ToString();
var quote = ((char)34).ToString();

Func<string, string> esc = text =>
{
    if (text == null) return "null";
    var sb = new System.Text.StringBuilder(quote);
    foreach (var c in text)
    {
        if (c == (char)92) sb.Append(backslash + backslash);
        else if (c == (char)34) sb.Append(backslash + quote);
        else if (c < (char)32) sb.Append(backslash + "u" + ((int)c).ToString("x4"));
        else sb.Append(c);
    }
    return sb.Append(quote).ToString();
};

var names = new List<string>();
foreach (var part in (parameterNames ?? "").Split(','))
{
    var name = part.Trim();
    if (name.Length > 0 && !names.Contains(name)) names.Add(name);
}
if (names.Count == 0)
{
    findings.Add("No parameter was named, so there is no column to show. Name them " +
                 "separated by commas - for example: Mark, Comments, Flow.");
}

// The display string first, in the project's units, then the raw text - the
// same reading read-element-parameters makes, and the exact string the write
// half compares before it writes (Article 12c).
Func<Parameter, string> readValue = p =>
{
    string text = null;
    try { text = p.AsValueString(); } catch { }
    if (string.IsNullOrWhiteSpace(text))
    {
        try { text = p.AsString(); } catch { }
    }
    return text;
};

var rows = new List<string>();
foreach (var e in elements)
{
    if (e == null || names.Count == 0) continue;
    if (rows.Count >= cap) { truncated++; continue; }

    Element typeElement = null;
    try { typeElement = e.Document.GetElement(e.GetTypeId()); } catch { }

    string family = null;
    var instance = e as FamilyInstance;
    if (instance != null && instance.Symbol != null && instance.Symbol.Family != null)
        family = instance.Symbol.Family.Name;

    var cells = new List<string>();
    foreach (var name in names)
    {
        string value = null;
        var editable = false;
        string why = null;

        var matches = e.GetParameters(name);
        if (matches.Count > 1)
        {
            why = "two parameters on this element share the name - which one is meant is yours to say";
            value = readValue(matches[0]);
        }
        else if (matches.Count == 1)
        {
            var p = matches[0];
            value = readValue(p);
            if (p.IsReadOnly) why = "read-only - Revit sets it";
            else if (p.StorageType == StorageType.ElementId || p.StorageType == StorageType.None)
                why = "it holds a reference to another element, which cannot be typed as text";
            else editable = true;
        }
        else
        {
            Parameter tp = null;
            if (typeElement != null)
            {
                var typeMatches = typeElement.GetParameters(name);
                if (typeMatches.Count >= 1) tp = typeMatches[0];
            }
            if (tp != null)
            {
                value = readValue(tp);
                why = "a type parameter - changing it changes every element of this type";
            }
            else
            {
                why = "this element does not have that parameter";
            }
        }

        cells.Add(esc(name) + ": {" + esc("value") + ": " + esc(value) + ", "
                  + esc("editable") + ": " + (editable ? "true" : "false") + ", "
                  + esc("why") + ": " + esc(why) + "}");
    }

    rows.Add("{" + esc("id") + ": " + esc(e.Id.ToString()) + ", "
             + esc("uniqueId") + ": " + esc(e.UniqueId) + ", "
             + esc("category") + ": " + esc(e.Category == null ? null : e.Category.Name) + ", "
             + esc("family") + ": " + esc(family) + ", "
             + esc("type") + ": " + esc(typeElement == null ? null : typeElement.Name) + ", "
             + esc("cells") + ": {" + string.Join(", ", cells.ToArray()) + "}}");
}

rowCount = rows.Count;

var columnList = new List<string>();
foreach (var name in names) columnList.Add(esc(name));

tableJson = "{" + esc("format") + ": 1, "
          + esc("columns") + ": [" + string.Join(", ", columnList.ToArray()) + "], "
          + esc("rows") + ": [" + string.Join(", ", rows.ToArray()) + "], "
          + esc("truncated") + ": " + truncated + "}";

if (truncated > 0)
{
    findings.Add(string.Format(
        "Only the first {0} elements are in the table; {1} more were left out. " +
        "Ask for fewer at a time, or raise maxRows.", cap, truncated));
}
