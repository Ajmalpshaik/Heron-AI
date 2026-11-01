// NOT STANDALONE. Assumes `doc`, `elements`, `parameterName`, `value` and
// `clear` are in scope, and leaves `written`, `snapped`, `unverified`,
// `readOnly`, `absent`, `ambiguous`, `refused`, `alreadyThat`, `findings`,
// `cleared` and `alreadyEmpty` behind.
//
// VERSION 6 (2026-11-01) - REMOVE_PARAMETER_VALUE MERGED IN: `clear` true
// empties the parameter instead of writing a value. See "CLEARING" below.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one. Golden Rule 16 wants one
// user action to be one undo entry, and that is owned by the operation's
// TransactionGroup - a fragment opening its own transaction is how a single
// "set the airflow" turns into forty undo steps.
//
// WHY THE VALUE IS TEXT, AND WHY THIS IS NOT A UNIT CONVERSION.
//
// The obvious version of this fragment takes a double and writes it. It is
// wrong, and it fails quietly, which is worse.
//
// Revit stores lengths in decimal FEET. Ajmal speaks millimetres. So a
// length-shaped fragment would convert with mm / 304.8 (D-20 - plain
// arithmetic, exact on every release). But this fragment is also asked to set
// an AIRFLOW, an AREA, a temperature and a comment, and those are stored in
// their own internal units or in none at all. A blanket /304.8 writes 200 mm
// into an airflow as 0.656 of whatever that parameter measures, reports
// success, and is found weeks later by somebody reading a schedule.
//
// `SetValueString` is the call that already knows. It parses the text in the
// DOCUMENT's own display units - the units the user is looking at while they
// type - and it is the only route here that cannot be wrong about which
// quantity it is writing. It also returns FALSE rather than throwing when the
// text does not parse, which is reported rather than swallowed.
//
// A parameter that stores an ElementId takes a NAME since version 5 - a
// material, a level, or a family type as "Family: Type" - looked up exactly,
// and refused by name when it names none or two. Never guessed: D-33 says
// Heron asks rather than assumes. See "an element reference takes a name".
//
// ===========================================================================
// A TRUE RETURN VALUE IS NOT EVIDENCE THAT THE VALUE WAS WRITTEN.
// ===========================================================================
//
// This is the whole reason this fragment is at version 2, and it is the same
// defect that was found in Heron's own move path: reporting the number that was
// ASKED FOR as the number that HAPPENED.
//
// Revit accepts a size and then SNAPS it to the nearest size its type allows.
// The set returns TRUE. Nothing throws. Ask a pipe for 77 and it becomes 80,
// and a fragment that trusts the return value reports a clean success while the
// model holds a different number. Version 1 of this file did exactly that - and
// its own proof case asked a human to check that "the number shown in Revit
// MATCHES WHAT WAS TYPED", which the code had no way of knowing.
//
// THE CHECK USES THE SNAP'S OWN TIMING, and that is why it needs no string
// parsing, no culture and no display-rounding tolerance:
//
//   * immediately after the set, the parameter still reports WHAT WAS ASKED FOR
//   * the snap happens at REGENERATION, not at the set
//
// So read the internal double straight after the set, regenerate once, read it
// again, and compare. Two numbers from the same API in the same units. The
// alternative - comparing `AsValueString()` against the user's text - has to
// cope with "300" against "300.0 mm", with thousands separators, and with a
// display rounded to the project's decimal places, and would raise false alarms
// on all three.
//
// ONE regeneration for the whole batch, not one per element. The snap is
// applied to everything pending, so a single regenerate settles all of them,
// and regenerating inside the loop turns a 400-element write into 400 model
// updates.
//
// `unverified` is separated from `snapped` and is NOT counted as written. It
// means the check could not be run - the parameter vanished between the write
// and the read-back, which should not happen and is reported rather than
// assumed away. Silence about a check that did not run is how an unproven
// claim becomes a believed one.
//
// ===========================================================================
// A YES/NO PARAMETER IS A TICK BOX, NOT A NUMBER THAT PARSES. (Version 4.)
// ===========================================================================
//
// Revit stores a Yes/No parameter as an Integer, 1 or 0, so version 3 sent it
// down the numeric road above - and `SetValueString` returned FALSE for it,
// "No" and "0" alike. Measured 2026-09-27 on 15 fan coil units in "heron ai
// bulding" (Revit 2024), their instance tick box "Show_Clearance": refused 15,
// written 0, twice (FRAGMENT-ISSUES 5b-247).
//
// So a Yes/No parameter is found first and written with `Set(1)` or `Set(0)`.
// The words are the ones a modeller says of a tick box - Yes/No, True/False,
// On/Off, 1/0, Ticked/Unticked, any case. Anything else is refused and the
// refusal names the value and those words; "Maybe" is never guessed at.
//
// THE KIND IS READ BY REFLECTION, NOT BY `#if`. Read from the reference
// assemblies of all eight releases on 2026-09-27: `Definition.GetDataType()`
// and `SpecTypeId.Boolean.YesNo` exist from 2022; `Definition.ParameterType`
// exists in 2020, 2021 and 2022 and is gone from 2023. The add-in compiles
// fragments with no release symbols (5b-181), so an `#if` would take the same
// branch on every release. The member present on the running Revit is looked
// up and asked - the way `set-family-type-values` reads a family parameter's
// kind.
//
// The read-back is the one above, unchanged: the element joins `pending` with
// the 1 or 0 asked for, and after the one regeneration `AsInteger` has to
// equal it before it counts as written.
//
// An element that ALREADY holds the value asked for is not written again and
// is counted in `alreadyThat`, so a second run says so rather than claiming
// fifteen fresh writes. Only a Yes/No parameter is checked this way; every
// other storage type goes exactly the road it went in version 3.

var written = 0;
var snapped = new List<ElementId>();
var unverified = new List<ElementId>();
var readOnly = new List<ElementId>();
var absent = new List<ElementId>();
var ambiguous = new List<ElementId>();
var refused = new List<ElementId>();
var alreadyThat = new List<ElementId>();
var findings = new List<string>();

// ===========================================================================
// CLEARING (version 6, from REMOVE_PARAMETER_VALUE).
// ===========================================================================
//
// AN EMPTY PARAMETER AND A ZERO ARE NOT THE SAME THING. A blank in a schedule
// means nobody has said; 0 means somebody said none. An empty value written as
// text clears a text parameter and NOT a length, an airflow or an element
// reference - so `clear` true asks Revit's own ClearValue, which works on
// every storage kind that can be cleared, and never writes a 0.
//
//   REVIT REFUSES ClearValue ON A PLAIN TEXT BUILT-IN - Comments, measured
//   2026-09-13 on `test projject`, every one refused "Cannot call Clear...".
//   For a TEXT parameter the empty string IS the empty state, so that is the
//   fallback, and only for text: a number or a reference that will not clear
//   is refused, never set to zero.
//   A TYPE PARAMETER REACHED FROM AN INSTANCE IS REFUSED. A type is shared;
//   clearing it from forty instances empties it on every other instance in
//   the project. Hand in the types if that is what is meant.
//   ALREADY EMPTY is `alreadyEmpty`, not `cleared`, and every clear is READ
//   BACK - empty after, or it is `unverified`.
//   A READ-ONLY PARAMETER (Area, Volume, Length - computed by Revit) is
//   `readOnly`, reported, never skipped in silence.
//
// `clear` with a value as well is two opposite asks, and is refused before
// anything is touched.
var cleared = 0;
var alreadyEmpty = new List<ElementId>();
var clearOnType = 0;
var clearRefusals = new List<string>();
var notThere = 0;
var clearAndValue = clear && !string.IsNullOrEmpty(value);
if (clearAndValue)
{
    findings.Add(string.Format("clear was asked AND a value ('{0}') was given - two opposite asks, so "
        + "NOTHING WAS CHANGED. To empty '{1}', give clear=true and leave value empty; to write a "
        + "value, leave clear out", value, parameterName));
}

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

// ===========================================================================
// AN ELEMENT REFERENCE TAKES A NAME (FRAGMENT-ISSUES 5b-1259).
// ===========================================================================
//
// A parameter that holds another element - a Material, a Level, a type - was
// refused or handed to SetValueString, which takes no name: on Revit 2020 a
// column type's Material was refused 'TRG_Material_Teak_Wood' that way, and a
// mullion type's Material before it. So a NAME is now looked up, exactly:
// a material and a level by Name, a family type as "Family: Type", the way
// Properties shows it. Matching none, or two, is refused by name - never
// guessed (D-33) - and the id Revit holds afterwards is read back.
//
// WHAT THE NAME MAY BE is narrowed first, so a name cannot land on the wrong
// kind of element: by what the parameter holds now (a type: types of that
// category only), else by its definition - Material, or on 2020 and 2021 a
// FamilyType - read by reflection like the Yes/No above, since
// `ParameterType` is gone from 2023 and `SpecTypeId.Reference` begins at
// 2022. A parameter holding <By Category> and saying nothing more is matched
// against all three kinds, and a name two of them share is refused.
// Anything else it holds - a phase, a view - is refused by name.
//
// No ElementId is built from a number: the id comes from the element found.
var materialSpecProperty = (typeof(Document).Assembly.GetType(typeof(Document).Namespace + ".SpecTypeId+Reference")
    ?? typeof(object)).GetProperty("Material",
    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
var materialSpec = materialSpecProperty == null ? null : materialSpecProperty.GetValue(null, null);

// kind, category (types only), name, id - every material, level and type in
// this document, read once, the first time a name is asked for.
List<Tuple<string, ElementId, string, ElementId>> namedElements = null;

// The id a name resolves to, or null and the reason in words.
Func<Parameter, string, Tuple<ElementId, string>> referenceByName = (p, text) =>
{
    var said = (text ?? "").Trim();
    var kind = "any";
    ElementId categoryId = null;
    try
    {
        var heldId = p.AsElementId();
        var held = heldId == null || heldId == ElementId.InvalidElementId ? null : doc.GetElement(heldId);
        if (held is Material) kind = "material";
        else if (held is Level) kind = "level";
        else if (held is ElementType)
        {
            kind = "type";
            if (held.Category != null) categoryId = held.Category.Id;
        }
        else if (held != null)
        {
            return Tuple.Create((ElementId)null, "it holds a " + held.GetType().Name + " ('" + held.Name
                + "'), and only a material, a level or a family type can be named");
        }
        else if (p.Definition != null)
        {
            var definition = p.Definition;
            var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
            if (getDataType != null && materialSpec != null)
            {
                var spec = getDataType.Invoke(definition, null);
                var nameEquals = spec == null ? null
                    : spec.GetType().GetMethod("NameEquals", new[] { materialSpec.GetType() });
                if (spec != null && (nameEquals != null
                        ? (bool)nameEquals.Invoke(spec, new[] { materialSpec })
                        : spec.Equals(materialSpec)))
                    kind = "material";
            }
            else
            {
                var parameterType = definition.GetType().GetProperty("ParameterType");
                var dataKind = parameterType == null ? null : parameterType.GetValue(definition, null);
                var word = dataKind == null ? "" : dataKind.ToString();
                if (word == "Material") kind = "material";
                else if (word == "FamilyType") kind = "type";
            }
        }
    }
    catch (Exception)
    {
        kind = "any";
        categoryId = null;
    }

    var what = kind == "material" ? "material" : kind == "level" ? "level"
        : kind == "type" ? "family type" + (categoryId == null ? "" : " of that category")
        : "material, level or family type";
    if (said.Length == 0) return Tuple.Create((ElementId)null, "a blank names no " + what);

    if (namedElements == null)
    {
        namedElements = new List<Tuple<string, ElementId, string, ElementId>>();
        foreach (var m in new FilteredElementCollector(doc).OfClass(typeof(Material)))
            namedElements.Add(Tuple.Create("material", (ElementId)null, m.Name, m.Id));
        foreach (var l in new FilteredElementCollector(doc).OfClass(typeof(Level)))
            namedElements.Add(Tuple.Create("level", (ElementId)null, l.Name, l.Id));
        foreach (var e in new FilteredElementCollector(doc).WhereElementIsElementType())
        {
            var t = e as ElementType;
            if (t == null || string.IsNullOrEmpty(t.FamilyName)) continue;
            namedElements.Add(Tuple.Create("type", t.Category == null ? null : t.Category.Id,
                t.FamilyName + ": " + t.Name, t.Id));
        }
    }

    var hits = namedElements.Where(n => (kind == "any" || n.Item1 == kind)
        && (categoryId == null || categoryId.Equals(n.Item2))
        && n.Item3 == said).ToList();
    if (hits.Count == 1) return Tuple.Create(hits[0].Item4, (string)null);
    if (hits.Count == 0)
    {
        return Tuple.Create((ElementId)null, "no " + what + " in this model is called '" + said + "'"
            + (kind == "type" || kind == "any" ? " - a family type is named 'Family: Type', as Properties shows it" : "")
            + ". Names are matched exactly");
    }
    return Tuple.Create((ElementId)null, hits.Count + " elements in this model are called '" + said + "' ("
        + string.Join(", ", hits.Take(3).Select(n => n.Item1 + " " + n.Item4).ToArray())
        + ") - which one is meant would be a guess");
};

// What the text means for a tick box: 1, 0, or null for "not a Yes/No word".
var tickedWords = new[] { "yes", "true", "on", "1", "ticked" };
var untickedWords = new[] { "no", "false", "off", "0", "unticked" };
var said = (value ?? "").Trim().ToLowerInvariant();
int? tickAsked = Array.IndexOf(tickedWords, said) >= 0 ? 1
    : Array.IndexOf(untickedWords, said) >= 0 ? 0
    : (int?)null;
var notAYesNoWord = 0;

// Integer parameters report through AsInteger and read 0 from AsDouble, so the
// read has to branch. It is written once and used on both sides of the
// regeneration, so the two comparisons cannot drift apart.
Func<Parameter, double> numericValue = p =>
    p.StorageType == StorageType.Integer ? (double)p.AsInteger() : p.AsDouble();

// Accepted by the set, and therefore NOT YET BELIEVED. Parallel lists rather
// than a carried Parameter, because the parameter is looked up again after the
// regeneration rather than held across it.
var pending = new List<Element>();
var asked = new List<double>();

// Version 5: an element reference set by name, and the id it was set to -
// believed, like a number, only after the one regeneration.
var pendingReference = new List<Element>();
var askedReference = new List<ElementId>();

foreach (var element in elements)
{
    // Version 6, REMOVE_PARAMETER_VALUE's rule: an element deleted since it was
    // found is counted and passed over, and the rest of the batch completes.
    if (element == null || !element.IsValidObject) { notThere++; continue; }
    if (clearAndValue) { refused.Add(element.Id); continue; }

    // TWO PARAMETERS CAN ANSWER TO ONE NAME, AND THEN A WRITE BY NAME IS A COIN
    // TOSS. A shared or project parameter can be bound beside a built-in one of
    // the same name, and a curtain wall type carries its grid and mullion
    // settings twice, once for each direction. LookupParameter then returns "the
    // first one encountered", which Autodesk's own reference says "is determined
    // at random" - and the read-back below looks the name up the same way, so it
    // would confirm whichever one was written. D-54 s3: a name that matches twice
    // is refused, never chosen from. FRAGMENT-ISSUES 5b-203.
    if (element.GetParameters(parameterName).Count > 1)
    {
        ambiguous.Add(element.Id);
        continue;
    }

    var parameter = element.LookupParameter(parameterName);

    if (clear)
    {
        if (parameter == null)
        {
            // On the TYPE rather than the instance - a different, wider act.
            Element elementType = null;
            try { elementType = doc.GetElement(element.GetTypeId()); } catch { elementType = null; }
            if (elementType != null && elementType.LookupParameter(parameterName) != null)
            {
                clearOnType++;
                refused.Add(element.Id);
            }
            else absent.Add(element.Id);
            continue;
        }
        if (parameter.IsReadOnly) { readOnly.Add(element.Id); continue; }

        bool emptyNow = !parameter.HasValue
            || (parameter.StorageType == StorageType.String && string.IsNullOrEmpty(parameter.AsString()));
        if (emptyNow) { alreadyEmpty.Add(element.Id); continue; }

        var done = false;
        string why = null;
        try { parameter.ClearValue(); done = true; }
        catch (Exception ex) { why = ex.Message; }
        if (!done && parameter.StorageType == StorageType.String)
        {
            try { done = parameter.Set(string.Empty); }
            catch (Exception ex) { why = ex.Message; }
        }
        if (!done)
        {
            refused.Add(element.Id);
            clearRefusals.Add(string.Format("{0} (id {1}): {2}", element.Name, element.Id,
                why ?? "Revit would not clear it"));
            continue;
        }

        // READ BACK. A clear that threw nothing is not evidence it took.
        var emptyAfter = false;
        try
        {
            emptyAfter = !parameter.HasValue
                || (parameter.StorageType == StorageType.String && string.IsNullOrEmpty(parameter.AsString()));
        }
        catch { emptyAfter = false; }
        if (emptyAfter) cleared++;
        else unverified.Add(element.Id);
        continue;
    }

    if (parameter == null)
    {
        // ABSENT IS A FAMILY QUESTION, NOT A TYPING ONE. The parameter does not
        // exist on this element at all, so no value would have helped. Kept
        // apart from read-only for that reason - they need different answers.
        absent.Add(element.Id);
        continue;
    }

    if (parameter.IsReadOnly)
    {
        readOnly.Add(element.Id);
        continue;
    }

    if (isYesNo(parameter))
    {
        if (tickAsked == null)
        {
            refused.Add(element.Id);
            notAYesNoWord++;
            continue;
        }

        if (parameter.HasValue && parameter.AsInteger() == tickAsked.Value)
        {
            alreadyThat.Add(element.Id);
            continue;
        }

        if (!parameter.Set(tickAsked.Value))
        {
            refused.Add(element.Id);
            continue;
        }

        // Believed only after the regeneration, like every other number here.
        pending.Add(element);
        asked.Add(tickAsked.Value);
        continue;
    }

    var ok = false;
    var numeric = false;

    switch (parameter.StorageType)
    {
        case StorageType.String:
            ok = parameter.Set(value);
            break;

        case StorageType.Integer:
        case StorageType.Double:
            // Units belong to the document, not to this code. See the note above.
            ok = parameter.SetValueString(value);
            numeric = true;
            break;

        case StorageType.ElementId:
            // Version 5: a name, resolved exactly, or refused with the reason.
            var named = referenceByName(parameter, value);
            var setRef = false;
            if (named.Item1 != null)
            {
                try { setRef = parameter.Set(named.Item1); }
                catch (Exception) { setRef = false; }
            }
            if (!setRef)
            {
                refused.Add(element.Id);
                var line = "'" + value + "' was not written to '" + parameterName + "': "
                    + (named.Item2 ?? "Revit would not take it there");
                if (!findings.Contains(line)) findings.Add(line);
                continue;
            }
            pendingReference.Add(element);
            askedReference.Add(named.Item1);
            continue;

        default:
            // None. Refused by name rather than attempted.
            refused.Add(element.Id);
            continue;
    }

    if (!ok)
    {
        refused.Add(element.Id);
        continue;
    }

    if (!numeric)
    {
        // Text does not go through the size table, so there is nothing for a
        // regeneration to change. Checked here rather than deferred.
        if (parameter.AsString() == value) written++;
        else snapped.Add(element.Id);
        continue;
    }

    // Read BEFORE the regeneration: this is what Revit parsed the text as, and
    // it is the only moment it is available.
    pending.Add(element);
    asked.Add(numericValue(parameter));
}

if (pending.Count > 0 || pendingReference.Count > 0)
{
    doc.Regenerate();

    // Version 5: the id Revit holds now must be the one the name resolved to.
    for (var i = 0; i < pendingReference.Count; i++)
    {
        var parameter = pendingReference[i].LookupParameter(parameterName);
        if (parameter == null) unverified.Add(pendingReference[i].Id);
        else if (askedReference[i].Equals(parameter.AsElementId())) written++;
        else snapped.Add(pendingReference[i].Id);
    }

    for (var i = 0; i < pending.Count; i++)
    {
        var parameter = pending[i].LookupParameter(parameterName);

        if (parameter == null)
        {
            unverified.Add(pending[i].Id);
            continue;
        }

        var stored = numericValue(parameter);
        var want = asked[i];

        // Both sides came from the same API in the same internal units, so the
        // only tolerance needed is for floating point itself - not for anything
        // Revit might reasonably have done to the number.
        if (Math.Abs(stored - want) <= Math.Abs(want) * 1e-9 + 1e-12) written++;
        else snapped.Add(pending[i].Id);
    }
}

if (notAYesNoWord > 0)
{
    findings.Add(string.Format(
        "'{0}' is not a Yes/No value, so {1} element(s) were left as they were. " +
        "'{2}' is a Yes/No parameter: say Yes or No - True/False, On/Off, 1/0 " +
        "and Ticked/Unticked mean the same.",
        value, notAYesNoWord, parameterName));
}

// VERSION 6 - what a clear did, said the way REMOVE_PARAMETER_VALUE said it.
if (notThere > 0)
{
    findings.Add(string.Format("{0} element(s) handed in are no longer in the model - deleted since they "
        + "were found - and were passed over; the rest were done", notThere));
}
if (clear && !clearAndValue)
{
    if (clearOnType > 0)
    {
        findings.Add(string.Format("{0} element(s) carry '{1}' on their TYPE, not on the instance. "
            + "Clearing it there would empty it on every other instance using that type, so it was NOT "
            + "done - hand in the types if that is what you want", clearOnType, parameterName));
    }
    if (readOnly.Count > 0)
    {
        findings.Add(string.Format("{0} element(s) carry '{1}' as READ-ONLY and it cannot be cleared. "
            + "Area, Volume and Length are computed by Revit", readOnly.Count, parameterName));
    }
    foreach (var line in clearRefusals.Take(10)) findings.Add("not cleared - " + line);
    if (clearRefusals.Count > 10)
        findings.Add(string.Format("and {0} more not cleared", clearRefusals.Count - 10));
    findings.Add(string.Format("{0} cleared, {1} already empty. An empty field and a zero are different "
        + "things in a schedule - blank means nobody has said, 0 means somebody said none",
        cleared, alreadyEmpty.Count));
}
