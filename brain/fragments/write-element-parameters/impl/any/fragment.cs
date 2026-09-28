// NOT STANDALONE. Assumes `doc`, `elements`, `parameterName` and `value` are in
// scope, and leaves `written`, `snapped`, `unverified`, `readOnly`, `absent`,
// `ambiguous`, `refused`, `alreadyThat` and `findings` behind.
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
// A parameter that stores an ElementId is REFUSED, not guessed at. "Level 2" as
// text cannot be resolved to an id without deciding which Level 2, and D-33
// says Heron asks rather than assumes.
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

foreach (var element in elements)
{
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

        default:
            // ElementId, and None. Refused by name rather than attempted.
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

if (pending.Count > 0)
{
    doc.Regenerate();

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
