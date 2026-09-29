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

// WHAT COUNTS AS "STILL THE SAME". Text is compared EXACTLY - a Comment
// changed in Revit from "A" to " A " is a change, and trimming would have
// written over it (Codex review of #362). A number, a length or a tick box
// is compared by its display string trimmed, because that is the form both
// sides read it in.
Func<Parameter, string, bool> unchanged = (p, was) =>
    p.StorageType == StorageType.String
        ? (readValue(p) ?? "") == (was ?? "")
        : same(readValue(p)) == same(was);

// A TICK BOX IS FOUND BY WHAT IT IS, NOT BY WHAT IT SAYS. Version 1 looked for
// the display words "Yes" or "No", which a Revit in another language does not
// show. This is write-element-parameters' own version-safe check, the same
// text, so the two cannot disagree about which parameter is a Yes/No.
// SpecTypeId.Boolean.YesNo on 2022 and later; null on 2020 and 2021, which
// answer through ParameterType instead.
var yesNoSpecs = typeof(Document).Assembly.GetType(typeof(Document).Namespace + ".SpecTypeId+Boolean");
var yesNoProperty = yesNoSpecs == null ? null : yesNoSpecs.GetProperty("YesNo",
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
var yesNoSpec = yesNoProperty == null ? null : yesNoProperty.GetValue(null, null);

Func<Parameter, bool> isYesNo = p =>
{
    if (p.StorageType != StorageType.Integer) return false;
    try
    {
        var definition = p.Definition;
        if (definition == null) return false;

        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null && yesNoSpec != null)
        {
            var spec = getDataType.Invoke(definition, null);
            if (spec == null) return false;
            // NameEquals, because a spec id carries its version -
            // "...bool-1.0.0" - and a parameter made under another version
            // must still read as a tick box.
            var nameEquals = spec.GetType().GetMethod("NameEquals", new[] { yesNoSpec.GetType() });
            return nameEquals != null
                ? (bool)nameEquals.Invoke(spec, new[] { yesNoSpec })
                : spec.Equals(yesNoSpec);
        }

        var parameterType = definition.GetType().GetProperty("ParameterType");
        var kind = parameterType == null ? null : parameterType.GetValue(definition, null);
        return kind != null && kind.ToString() == "YesNo";
    }
    catch (Exception)
    {
        // Not known to be a tick box, so it takes the version 3 road, where
        // SetValueString refused the two Yes/No values measured rather than
        // writing them wrongly.
        return false;
    }
};

var ticked = new[] { "yes", "true", "on", "1", "ticked" };
var unticked = new[] { "no", "false", "off", "0", "unticked" };

// ONE NAME, SEVERAL PARAMETERS - AND WHEN ONLY ONE OF THEM CAN BE WRITTEN, THAT
// ONE IS MEANT. A sheet carries its own Sheet Number and, being a view, a second
// read-only "Sheet Number" saying which sheet it sits on; version 1 refused
// both, so no sheet number could be edited (the owner, 2026-09-29, on the
// first table he opened). D-54 s3 forbids GUESSING between parameters that
// could each be written; when every one but one is read-only there is nothing
// to guess, because a write can only land on that one. Two or more writable
// still refuses, exactly as before (FRAGMENT-ISSUES 5b-203).
Func<Element, string, Parameter> theOne = (element, parameterName) =>
{
    var found = element.GetParameters(parameterName);
    if (found.Count == 1) return found[0];
    Parameter writable = null;
    foreach (var candidate in found)
    {
        if (candidate.IsReadOnly) continue;
        if (writable != null) return null;       // two could be written: refuse
        writable = candidate;
    }
    return writable;
};

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
        var target = matches.Count > 0 ? theOne(element, name) : null;
        if (matches.Count == 0) why = "it no longer has a parameter called " + name;
        else if (target == null)
            why = "two parameters on it are called " + name + " and more than one can be written";
        else if (target.IsReadOnly) why = name + " is read-only";
        else if (target.StorageType == StorageType.ElementId
                 || target.StorageType == StorageType.None)
            why = name + " holds a reference to another element, which cannot be typed as text";
        else if (!unchanged(target, was))
            why = "it was changed in Revit since the table was read - it now reads '"
                  + (readValue(target) ?? "") + "'";
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
        var p = theOne(step.Item1, step.Item2);
        var want = step.Item3;
        var ok = false;

        if (isYesNo(p))
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
        var again = theOne(step.Item1, step.Item2);
        var now = again == null ? null : readValue(again);
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
