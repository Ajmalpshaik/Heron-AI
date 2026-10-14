// NOT STANDALONE. Assumes `doc` and `rows` are in scope, and leaves `written`,
// `resultJson`, `stale`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one: the operation's
// TransactionGroup makes the whole Apply one entry in Revit's undo list.
//
// TWO WAYS TO NAME THE ELEMENTS, AND THE FIRST ROW SAYS WHICH (version 2).
//
//   BY ID - version 1, for the Companion's table and the loads Finalize:
//   rows `id|uniqueId|parameter|was|new`. Its code below is unchanged but
//   for two `keyed` guards that make it step aside.
//
//   BY ANY PARAMETER VALUE - a room's Number, a sheet's Sheet Number, a
//   Mark: a first row `key=Number|in=Rooms`, then `5|Name=Lift 02` for each
//   element. A caller cannot get element ids out of a reply - a list stops
//   at three - so giving 13 rooms 13 names took 26 calls, a find and a write
//   per room (FRAGMENT-ISSUES 5b-459). A key that names no element, or more
//   than one, refuses THAT ROW by name and never guesses.
//
// BY ID: TWO PASSES, AND THE FIRST WRITES NOTHING. Every row is checked
// against the model before any row is written - still there, the same
// element, the parameter there once, writable, and still holding the value
// the table SHOWED. One failure and nothing is written at all (Article 12c;
// the owner's answer to docs/40 section 18 question 2). A half-applied table
// is the "preview that expired" the Constitution forbids.
//
// THE BACKSLASH AND THE QUOTE ARE BUILT FROM THEIR CHARACTER CODES; see
// read-element-table for why.

var written = 0;
var stale = new List<string>();
var refused = new List<string>();
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


// THE STORED VALUE, beside the display text. A length shown as "1000 mm" can
// hold two different raw values that round to the same text, and comparing
// only the text would miss a change made in Revit between the two (Codex
// review of #362). Numbers are compared by what Revit stores.
Func<Parameter, string> storedValue = p =>
{
    try
    {
        if (p.StorageType == StorageType.Double)
            return p.AsDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        if (p.StorageType == StorageType.Integer)
            return p.AsInteger().ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (p.StorageType == StorageType.String) return p.AsString() ?? "";
    }
    catch { }
    return null;
};

// A MEMBER OF A MODEL GROUP whose parameter does not vary across groups: a
// change to it is copied into the same element in every placement of the
// group, so one row would silently edit elements the table never showed
// (Codex review of #362). Such a cell is not editable.
Func<Element, Parameter, bool> groupLocked = (element, p) =>
{
    if (element.GroupId == null || element.GroupId == ElementId.InvalidElementId) return false;
    try
    {
        var definition = p.Definition as InternalDefinition;
        return definition == null || !definition.VariesAcrossGroups;
    }
    catch { return true; }
};

// ------------------------------------------------------------- which way
// BY KEY when the first row is key=... - an id row starts with an element id,
// so nothing a version 1 caller sends can land there by accident. One
// top-level name only: every top-level variable is printed in the reply.
var keyed = (rows ?? "").Split(';').Where(r => r.Trim().Length > 0).Take(1)
    .Any(r => r.IndexOf('=') > 0 && Uri.UnescapeDataString(r.Substring(0, r.IndexOf('=')))
                                        .Trim().Equals("key", StringComparison.OrdinalIgnoreCase));

// ------------------------------------------------------------------ parse
var plan = new List<Tuple<Element, string, string>>();   // element, parameter, new value
var staleJson = new List<string>();

foreach (var raw in (keyed ? "" : (rows ?? "")).Split(';'))
{
    if (raw.Trim().Length == 0) continue;
    var f = raw.Split('|');
    if (f.Length != 5 && f.Length != 6)
    {
        findings.Add("A row did not have its five parts - id, UniqueId, parameter, old value, " +
                     "new value, and optionally the stored old value - so nothing was written.");
        stale.Add("(unreadable row)");
        continue;
    }
    var id = Uri.UnescapeDataString(f[0]);
    var uniqueId = Uri.UnescapeDataString(f[1]);
    var name = Uri.UnescapeDataString(f[2]);
    var was = Uri.UnescapeDataString(f[3]);
    var want = Uri.UnescapeDataString(f[4]);
    var wasStored = f.Length == 6 ? Uri.UnescapeDataString(f[5]) : "";

    string why = null;
    string now = null;
    string nowStored = null;
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
        else if (groupLocked(element, target))
            why = "it is in a model group, and " + name + " would change in every copy of the group";
        else if (!unchanged(target, was)
                 || (wasStored.Length > 0 && target.StorageType != StorageType.String
                     && storedValue(target) != wasStored))
        {
            now = readValue(target) ?? "";
            nowStored = storedValue(target);
            why = "it was changed in Revit since the table was read - it now reads '" + now + "'";
        }
    }

    if (why != null)
    {
        stale.Add(id + ": " + why);
        // WHAT REVIT HOLDS NOW, for a cell that was changed there: the table
        // takes it as its new "was", so the next Apply is checked against
        // the model as it is, not refused for ever (Codex review of #362).
        staleJson.Add("{" + esc("id") + ": " + esc(id) + ", " + esc("name") + ": " + esc(name)
                      + ", " + esc("why") + ": " + esc(why)
                      + (now == null ? "" : ", " + esc("now") + ": " + esc(now)
                         + ", " + esc("nowRaw") + ": " + esc(nowStored)) + "}");
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
    if (!keyed) findings.Add("No rows were given, so nothing was written.");
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
        var nowRaw = again == null ? null : storedValue(again);
        written++;
        readBack.Add("{" + esc("id") + ": " + esc(step.Item1.Id.ToString()) + ", "
                     + esc("name") + ": " + esc(step.Item2) + ", "
                     + esc("value") + ": " + esc(now) + ", "
                     + esc("raw") + ": " + esc(nowRaw) + "}");
    }
}

resultJson = "{" + esc("format") + ": 1, "
           + esc("applied") + ": " + (applied ? "true" : "false") + ", "
           + esc("written") + ": " + written + ", "
           + esc("stale") + ": [" + string.Join(", ", staleJson.ToArray()) + "], "
           + esc("readBack") + ": [" + string.Join(", ", readBack.ToArray()) + "]}";

if (keyed)
{
    // ============================================================== by key
    // Version 2. The first row names the key; every row after it names ONE
    // element by the value it holds and gives what to write on it. Nothing
    // here runs for the id rows above, and nothing above runs for these.

    var keyedRows = new List<string>();
    foreach (var raw in (rows ?? "").Split(';'))
    {
        if (raw.Trim().Length > 0) keyedRows.Add(raw);
    }
    var header = keyedRows[0];
    keyedRows.RemoveAt(0);

    // ------------------------------------------------------------ the header
    // key=   the parameter whose value names each element - Number, Mark.
    // in=    the categories to look in, comma-separated, by the name
    //        Visibility/Graphics shows. REQUIRED: a door's Mark and a
    //        window's Mark can both read W1, and a key must never land on a
    //        kind of element nobody meant.
    // Any other name=value NARROWS the search to elements that hold that
    // value too - Level=Level 14 when every floor numbers its rooms from 1.
    string keyName = null;
    var categoryNames = new List<string>();
    var narrowing = new List<Tuple<string, string>>();
    string problem = null;

    foreach (var part in header.Split('|'))
    {
        if (part.Trim().Length == 0) continue;
        var at = part.IndexOf('=');
        var word = at < 0 ? "" : Uri.UnescapeDataString(part.Substring(0, at)).Trim();
        if (word.Length == 0)
        {
            problem = "The first row's '" + Uri.UnescapeDataString(part) + "' is not name=value - " +
                      "the first row is key=<parameter>|in=<categories>.";
            break;
        }
        var said = part.Substring(at + 1);
        if (word.Equals("key", StringComparison.OrdinalIgnoreCase))
        {
            keyName = Uri.UnescapeDataString(said).Trim();
        }
        else if (word.Equals("in", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var one in said.Split(','))
            {
                var name = Uri.UnescapeDataString(one).Trim();
                if (name.Length > 0) categoryNames.Add(name);
            }
        }
        else
        {
            narrowing.Add(Tuple.Create(word, Uri.UnescapeDataString(said)));
        }
    }

    if (problem == null && string.IsNullOrEmpty(keyName))
        problem = "The first row names no key - write key=<parameter>, for example key=Number.";
    if (problem == null && categoryNames.Count == 0)
        problem = "The first row says no category to look in - add in=<categories>, for example " +
                  "in=Rooms, so a key cannot land on a kind of element you did not mean.";

    var categories = new List<Category>();
    if (problem == null)
    {
        foreach (var wanted in categoryNames)
        {
            Category found = null;
            foreach (Category category in doc.Settings.Categories)
            {
                if (string.Equals(category.Name, wanted, StringComparison.OrdinalIgnoreCase))
                {
                    found = category;
                    break;
                }
            }
            if (found == null)
            {
                problem = "This model has no category called '" + wanted + "'. Give it as " +
                          "Visibility/Graphics shows it - Rooms, Sheets, Doors.";
                break;
            }
            if (!categories.Any(c => c.Id == found.Id)) categories.Add(found);
        }
    }

    var where = string.Join(", ", categoryNames.ToArray());
    if (narrowing.Count > 0)
    {
        where += " with " + string.Join(" and ",
            narrowing.Select(n => n.Item1 + " '" + n.Item2 + "'").ToArray());
    }

    // ------------------------------------------------------ the candidates
    // Every element in those categories - never a type - in THIS model; a
    // linked model's elements are not written here. A value is compared as
    // the Properties palette shows it, exactly, ignoring spaces at the ends:
    // "01" is not "1", and "Lift 1" is not "lift 1".
    Func<Element, string, string, bool> holds = (element, name, value) =>
    {
        foreach (Parameter p in element.GetParameters(name))
        {
            if (same(readValue(p)) == same(value)) return true;
        }
        return false;
    };

    var byKey = new Dictionary<string, List<Element>>(StringComparer.Ordinal);
    var candidates = 0;
    var carryingKey = 0;
    if (problem == null)
    {
        foreach (var category in categories)
        {
            var collector = new FilteredElementCollector(doc)
                .OfCategoryId(category.Id)
                .WhereElementIsNotElementType();
            foreach (var element in collector)
            {
                if (narrowing.Any(n => !holds(element, n.Item1, n.Item2))) continue;
                candidates++;

                // AN ELEMENT IS LISTED ONCE PER VALUE IT SHOWS under the key's
                // name - a sheet carries two parameters called Sheet Number.
                var keyParameters = element.GetParameters(keyName);
                if (keyParameters.Count > 0) carryingKey++;
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var p in keyParameters)
                {
                    var value = same(readValue(p));
                    if (!seen.Add(value)) continue;
                    List<Element> list;
                    if (!byKey.TryGetValue(value, out list))
                    {
                        list = new List<Element>();
                        byKey[value] = list;
                    }
                    list.Add(element);
                }
            }
        }

        if (candidates == 0)
            problem = "There is no element in " + where + " in this model, so no key can name one.";
        else if (carryingKey == 0)
            problem = "No element in " + where + " has a parameter called " + keyName + ".";
    }

    // ----------------------------------------------------------- the rows
    // EVERY KEY IS MATCHED AGAINST THE MODEL AS IT WAS BEFORE ANY ROW IS
    // WRITTEN, so a row that renumbers an element never moves another row
    // onto it. A ROW IS REFUSED ON ITS OWN, never guessed: a key that names
    // no element, or several, or is given twice, writes nothing for that row
    // and says why by name; the other rows are written.
    var refusedJson = new List<string>();
    Action<string, string> refuse = (key, why) =>
    {
        refused.Add(key + ": " + why);
        refusedJson.Add("{" + esc("key") + ": " + esc(key) + ", " + esc("why") + ": " + esc(why) + "}");
    };

    Func<List<Element>, string> idsOf = list =>
        string.Join(", ", list.Take(5).Select(e => e.Id.ToString()).ToArray())
        + (list.Count > 5 ? " and " + (list.Count - 5) + " more" : "");

    // The same five reasons a cell cannot be written as the id rows above,
    // in the same words.
    Func<Element, string, string> whyNot = (element, name) =>
    {
        var matches = element.GetParameters(name);
        var target = matches.Count > 0 ? theOne(element, name) : null;
        if (matches.Count == 0) return "it has no parameter called " + name;
        if (target == null) return "two parameters on it are called " + name + " and more than one can be written";
        if (target.IsReadOnly) return name + " is read-only";
        if (target.StorageType == StorageType.ElementId || target.StorageType == StorageType.None)
            return name + " holds a reference to another element, which cannot be typed as text";
        if (groupLocked(element, target))
            return "it is in a model group, and " + name + " would change in every copy of the group";
        return null;
    };

    var keyedPlan = new List<Tuple<string, Element, List<Tuple<string, string>>>>();
    if (problem == null)
    {
        var parsed = new List<Tuple<string, List<Tuple<string, string>>>>();
        var timesGiven = new Dictionary<string, int>(StringComparer.Ordinal);
        var rowNumber = 1;
        foreach (var raw in keyedRows)
        {
            rowNumber++;
            var f = raw.Split('|');
            var key = same(Uri.UnescapeDataString(f[0]));
            var pairs = new List<Tuple<string, string>>();
            string bad = key.Length == 0 ? "it has no key before the first |" : null;
            for (var i = 1; i < f.Length && bad == null; i++)
            {
                if (f[i].Trim().Length == 0) continue;
                var at = f[i].IndexOf('=');
                var name = at < 0 ? "" : Uri.UnescapeDataString(f[i].Substring(0, at)).Trim();
                if (name.Length == 0) bad = "'" + Uri.UnescapeDataString(f[i]) + "' is not parameter=value";
                else if (pairs.Any(x => x.Item1 == name)) bad = name + " is given twice";
                else pairs.Add(Tuple.Create(name, Uri.UnescapeDataString(f[i].Substring(at + 1))));
            }
            if (bad == null && pairs.Count == 0) bad = "it gives no parameter=value to write";
            if (bad != null)
            {
                refuse(key.Length > 0 ? key : "(row " + rowNumber + ")", "not written - " + bad);
                continue;
            }
            parsed.Add(Tuple.Create(key, pairs));
            timesGiven[key] = (timesGiven.ContainsKey(key) ? timesGiven[key] : 0) + 1;
        }

        var matched = new List<Tuple<string, Element, List<Tuple<string, string>>>>();
        foreach (var row in parsed)
        {
            var key = row.Item1;
            List<Element> hits;
            byKey.TryGetValue(key, out hits);
            if (timesGiven[key] > 1)
                refuse(key, "given on " + timesGiven[key] + " rows, so which value is meant would be a guess - not written");
            else if (hits == null || hits.Count == 0)
                refuse(key, "no element in " + where + " has " + keyName + " '" + key + "' - not written");
            else if (hits.Count > 1)
                refuse(key, hits.Count + " elements in " + where + " have " + keyName + " '" + key + "' (ids "
                       + idsOf(hits) + ") - which one is meant would be a guess, so none is written");
            else
                matched.Add(Tuple.Create(key, hits[0], row.Item2));
        }

        // TWO ROWS ON ONE ELEMENT - two keys that are both values it shows -
        // would write over each other, so neither is written.
        var rowsOnElement = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var row in matched)
        {
            var id = row.Item2.Id.ToString();
            rowsOnElement[id] = (rowsOnElement.ContainsKey(id) ? rowsOnElement[id] : 0) + 1;
        }

        foreach (var row in matched)
        {
            var element = row.Item2;
            if (rowsOnElement[element.Id.ToString()] > 1)
            {
                refuse(row.Item1, "another row's key names the same element (id " + element.Id + ") - not written");
                continue;
            }
            string why = null;
            foreach (var pair in row.Item3)
            {
                why = whyNot(element, pair.Item1);
                if (why != null) break;
            }
            if (why != null)
            {
                refuse(row.Item1, why + " - not written (id " + element.Id + ")");
                continue;
            }
            keyedPlan.Add(row);
        }
    }

    // ------------------------------------------------------------ write
    // EACH ROW IN ITS OWN SUB-TRANSACTION: a value Revit turns down takes
    // back that row's other values and no other row's. The whole call is
    // still ONE entry in Revit's undo list - the operation's TransactionGroup.
    var done = new List<Tuple<string, Element, List<Tuple<string, string>>>>();
    foreach (var row in keyedPlan)
    {
        var element = row.Item2;
        string turnedDown = null;
        using (var attempt = new SubTransaction(doc))
        {
            attempt.Start();
            try
            {
                foreach (var pair in row.Item3)
                {
                    var target = theOne(element, pair.Item1);
                    var asked = pair.Item2 ?? "";
                    var ok = false;
                    if (isYesNo(target))
                    {
                        var said = same(asked).ToLowerInvariant();
                        if (Array.IndexOf(ticked, said) >= 0) ok = target.Set(1);
                        else if (Array.IndexOf(unticked, said) >= 0) ok = target.Set(0);
                    }
                    else if (target.StorageType == StorageType.String)
                    {
                        ok = target.Set(asked);
                    }
                    else
                    {
                        ok = target.SetValueString(asked);
                    }
                    if (!ok)
                    {
                        turnedDown = "Revit would not take '" + asked + "' for " + pair.Item1;
                        break;
                    }
                }
            }
            catch (Exception failure)
            {
                // REVIT'S OWN WORDS FIRST - a reply cuts a line at 60 characters.
                turnedDown = failure.Message + " (Revit, writing this row)";
            }
            if (turnedDown == null) attempt.Commit();
            else attempt.RollBack();
        }

        if (turnedDown != null)
        {
            refuse(row.Item1, turnedDown + " - nothing on this row was kept (id " + element.Id + ")");
            continue;
        }
        done.Add(row);
    }

    // ------------------------------------------------------- read back
    // After ONE regeneration, every value is read from the model again: what
    // Revit holds now, not what was asked.
    var keyedReadBack = new List<string>();
    if (done.Count > 0) doc.Regenerate();
    foreach (var row in done)
    {
        written++;
        foreach (var pair in row.Item3)
        {
            var again = theOne(row.Item2, pair.Item1);
            var now = again == null ? null : readValue(again);
            var nowRaw = again == null ? null : storedValue(again);
            keyedReadBack.Add("{" + esc("key") + ": " + esc(row.Item1) + ", "
                         + esc("id") + ": " + esc(row.Item2.Id.ToString()) + ", "
                         + esc("name") + ": " + esc(pair.Item1) + ", "
                         + esc("asked") + ": " + esc(pair.Item2) + ", "
                         + esc("value") + ": " + esc(now) + ", "
                         + esc("raw") + ": " + esc(nowRaw) + "}");
            if (again != null && again.StorageType == StorageType.String && (now ?? "") != (pair.Item2 ?? ""))
                findings.Add(row.Item1 + ": " + pair.Item1 + " reads back '" + now + "', not '" + pair.Item2 + "'");
        }
    }

    if (problem != null)
        findings.Insert(0, "Nothing was written. " + problem);
    else if (refused.Count > 0)
        findings.Insert(0, string.Format("{0} row(s) not written, {1} written - refused says why, by key.",
                                         refused.Count, written));
    else if (keyedRows.Count == 0)
        findings.Add("No rows were given after the key row, so nothing was written.");

    resultJson = "{" + esc("format") + ": 2, "
               + esc("applied") + ": " + (written > 0 ? "true" : "false") + ", "
               + esc("written") + ": " + written + ", "
               + esc("key") + ": " + esc(keyName) + ", "
               + esc("in") + ": [" + string.Join(", ", categoryNames.Select(n => esc(n)).ToArray()) + "], "
               + esc("where") + ": [" + string.Join(", ", narrowing.Select(n => "{" + esc("name") + ": "
                   + esc(n.Item1) + ", " + esc("value") + ": " + esc(n.Item2) + "}").ToArray()) + "], "
               + esc("problem") + ": " + esc(problem) + ", "
               + esc("stale") + ": [], "
               + esc("refused") + ": [" + string.Join(", ", refusedJson.ToArray()) + "], "
               + esc("readBack") + ": [" + string.Join(", ", keyedReadBack.ToArray()) + "]}";
}
