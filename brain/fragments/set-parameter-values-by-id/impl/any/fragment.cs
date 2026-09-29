// NOT STANDALONE. Assumes `doc` and `rows` are in scope, and leaves `written`,
// `resultJson`, `stale` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one: the operation's
// TransactionGroup makes the whole Apply one entry in Revit's undo list.
//
// TWO PASSES, AND THE FIRST WRITES NOTHING. Every row is checked against the
// model before any row is written - still there, the same element, the
// parameter there once, writable, and still holding the value the table
// SHOWED. One failure and nothing is written at all (Article 12c; the owner's
// answer to docs/40 section 18 question 2). A half-applied table is the
// "preview that expired" the Constitution forbids.
//
// THE BACKSLASH AND THE QUOTE ARE BUILT FROM THEIR CHARACTER CODES; see
// read-element-table for why.

var written = 0;
var stale = new List<string>();
var findings = new List<string>();
var resultJson = "";

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

Func<string, string> same = text => (text ?? "").Trim();

var ticked = new[] { "yes", "true", "on", "1", "ticked" };
var unticked = new[] { "no", "false", "off", "0", "unticked" };

// ------------------------------------------------------------------ parse
var plan = new List<Tuple<Element, string, string>>();   // element, parameter, new value
var staleJson = new List<string>();

foreach (var raw in (rows ?? "").Split(';'))
{
    if (raw.Trim().Length == 0) continue;
    var f = raw.Split('|');
    if (f.Length != 5)
    {
        findings.Add("A row did not have its five parts - id, UniqueId, parameter, old value, " +
                     "new value - so nothing was written.");
        stale.Add("(unreadable row)");
        continue;
    }
    var id = Uri.UnescapeDataString(f[0]);
    var uniqueId = Uri.UnescapeDataString(f[1]);
    var name = Uri.UnescapeDataString(f[2]);
    var was = Uri.UnescapeDataString(f[3]);
    var want = Uri.UnescapeDataString(f[4]);

    string why = null;
    Element element = null;
    try { element = doc.GetElement(uniqueId); } catch { }

    if (element == null) why = "it is no longer in the model";
    else if (element.Id.ToString() != id) why = "the id no longer names the same element";
    else
    {
        var matches = element.GetParameters(name);
        if (matches.Count == 0) why = "it no longer has a parameter called " + name;
        else if (matches.Count > 1) why = "two parameters on it are called " + name;
        else if (matches[0].IsReadOnly) why = name + " is read-only";
        else if (matches[0].StorageType == StorageType.ElementId
                 || matches[0].StorageType == StorageType.None)
            why = name + " holds a reference to another element, which cannot be typed as text";
        else if (same(readValue(matches[0])) != same(was))
            why = "it was changed in Revit since the table was read - it now reads '"
                  + (readValue(matches[0]) ?? "") + "'";
    }

    if (why != null)
    {
        stale.Add(id + ": " + why);
        staleJson.Add("{" + esc("id") + ": " + esc(id) + ", " + esc("name") + ": " + esc(name)
                      + ", " + esc("why") + ": " + esc(why) + "}");
        continue;
    }
    plan.Add(Tuple.Create(element, name, want));
}

// ------------------------------------------------------------------ write
var readBack = new List<string>();
var applied = stale.Count == 0 && plan.Count > 0;

if (stale.Count > 0)
{
    findings.Add(string.Format(
        "Nothing was written: {0} row(s) no longer match what the table showed. " +
        "Refresh the table, look at them, and apply again.", stale.Count));
}
else if (plan.Count == 0)
{
    findings.Add("No rows were given, so nothing was written.");
}
else
{
    var refusedHere = new List<string>();
    foreach (var step in plan)
    {
        var p = step.Item1.GetParameters(step.Item2)[0];
        var want = step.Item3;
        var shown = same(readValue(p)).ToLowerInvariant();
        var ok = false;

        if (p.StorageType == StorageType.Integer && (shown == "yes" || shown == "no"))
        {
            var said = same(want).ToLowerInvariant();
            if (Array.IndexOf(ticked, said) >= 0) ok = p.Set(1);
            else if (Array.IndexOf(unticked, said) >= 0) ok = p.Set(0);
        }
        else if (p.StorageType == StorageType.String)
        {
            ok = p.Set(want ?? "");
        }
        else
        {
            ok = p.SetValueString(want ?? "");
        }

        if (!ok) refusedHere.Add(step.Item1.Id + ": Revit would not take '" + want + "' for " + step.Item2);
    }

    if (refusedHere.Count > 0)
    {
        // A value Revit turned down after the check passed. Thrown, so the
        // operation's TransactionGroup rolls EVERY row back: all or nothing
        // holds on this side of the check as well.
        throw new InvalidOperationException("Nothing was kept. " + string.Join("; ", refusedHere.ToArray()));
    }

    doc.Regenerate();

    foreach (var step in plan)
    {
        var matches = step.Item1.GetParameters(step.Item2);
        var now = matches.Count == 1 ? readValue(matches[0]) : null;
        written++;
        readBack.Add("{" + esc("id") + ": " + esc(step.Item1.Id.ToString()) + ", "
                     + esc("name") + ": " + esc(step.Item2) + ", "
                     + esc("value") + ": " + esc(now) + "}");
    }
}

resultJson = "{" + esc("format") + ": 1, "
           + esc("applied") + ": " + (applied ? "true" : "false") + ", "
           + esc("written") + ": " + written + ", "
           + esc("stale") + ": [" + string.Join(", ", staleJson.ToArray()) + "], "
           + esc("readBack") + ": [" + string.Join(", ", readBack.ToArray()) + "]}";
