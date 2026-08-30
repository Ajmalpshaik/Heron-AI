// NOT STANDALONE. Assumes `doc`, `elements`, `width`, `height` and `diameter`
// are in scope; leaves `sized`, `snapped`, `notApplicable` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ===========================================================================
// A TRUE RETURN VALUE IS NOT EVIDENCE THAT THE SIZE WAS WRITTEN.
// ===========================================================================
//
// Revit snaps a size to the nearest one the type's size table carries. The set
// returns TRUE, nothing throws, and a run asked for 77 comes out 80. A fragment
// that trusts the return value reports a clean success over a different number,
// and the model and the answer disagree from then on.
//
// THE CHECK USES THE SNAP'S OWN TIMING:
//
//   * immediately after the set, the parameter still reports WHAT WAS ASKED FOR
//   * the snap happens at REGENERATION, not at the set
//
// So read the internal double straight after the set, regenerate once, read it
// again, and compare. Two numbers from the same API in the same units - no
// string parsing, no display units, no rounding tolerance.
//
// THIS LOGIC HAS A SECOND HOME, DELIBERATELY, AND THEY MUST STAY IN STEP.
// WRITE_ELEMENT_PARAMETERS carries the same before/after check for a parameter
// named by the user. This fragment cannot delegate to it: that one finds a
// parameter by NAME, which is the display name and therefore the language
// Revit is running in, and "Width" finds nothing on a French install. Sizes are
// reached by BuiltInParameter here for exactly that reason. Two homes for one
// lesson is a cost; a size that silently does nothing in another language is a
// bigger one.
//
// ZERO MEANS "LEAVE IT ALONE", not "set it to zero". Setting a duct width to
// zero is refused by Revit anyway, and treating 0 as a request would make it
// impossible to set a diameter without also stating a width.
//
// A SIZE THAT DOES NOT APPLY TO THE SHAPE IS NOT A FAILURE. Width and height do
// not exist on round duct, diameter does not exist on rectangular. A mixed list
// is a normal thing to be handed, so those are named separately from a genuine
// refusal.

var sized = 0;
var snapped = new List<ElementId>();
var notApplicable = new List<ElementId>();
var refused = new List<ElementId>();

// Integer storage reads through AsInteger and returns 0 from AsDouble. Sizes
// are doubles, but the read is written once and used on both sides of the
// regeneration so the two comparisons cannot drift apart.
Func<Parameter, double> numericValue = p =>
    p.StorageType == StorageType.Integer ? (double)p.AsInteger() : p.AsDouble();

// Accepted by the set, and therefore not yet believed. Three parallel lists,
// because the parameter is looked up again after the regeneration rather than
// held across it.
var pending = new List<Element>();
var pendingParam = new List<BuiltInParameter>();
var pendingAsked = new List<double>();

foreach (var element in elements)
{
    if (element == null) continue;

    var wanted = new List<KeyValuePair<BuiltInParameter, double>>();
    if (width > 0) wanted.Add(new KeyValuePair<BuiltInParameter, double>(BuiltInParameter.RBS_CURVE_WIDTH_PARAM, width));
    if (height > 0) wanted.Add(new KeyValuePair<BuiltInParameter, double>(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM, height));
    if (diameter > 0) wanted.Add(new KeyValuePair<BuiltInParameter, double>(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM, diameter));

    var touched = false;
    var missed = false;

    foreach (var request in wanted)
    {
        var parameter = element.get_Parameter(request.Key);

        if (parameter == null || parameter.IsReadOnly)
        {
            // The shape does not carry this size. Noted per ELEMENT below, not
            // per parameter, so a round duct in a rectangular batch is reported
            // once rather than twice.
            missed = true;
            continue;
        }

        if (!parameter.Set(request.Value))
        {
            refused.Add(element.Id);
            touched = true;
            continue;
        }

        // Read BEFORE the regeneration: this is the value Revit took, and it is
        // the only moment it is available.
        pending.Add(element);
        pendingParam.Add(request.Key);
        pendingAsked.Add(numericValue(parameter));
        touched = true;
    }

    if (!touched && missed) notApplicable.Add(element.Id);
}

if (pending.Count > 0)
{
    // ONE regeneration for the whole batch. The snap is applied to everything
    // pending, and regenerating inside the loop turns a 400-element resize into
    // 400 model updates.
    doc.Regenerate();

    // An element with both a width and a height appears twice, so a snap on
    // either must not count the element as sized. Collected first, decided after.
    var snappedIds = new List<ElementId>();
    var okIds = new List<ElementId>();

    for (var i = 0; i < pending.Count; i++)
    {
        var parameter = pending[i].get_Parameter(pendingParam[i]);

        if (parameter == null)
        {
            // The check could not be run. Not counted as sized, and not assumed
            // away - silence about a check that did not happen is how an
            // unproven claim becomes a believed one.
            if (!snappedIds.Contains(pending[i].Id)) snappedIds.Add(pending[i].Id);
            continue;
        }

        var stored = numericValue(parameter);
        var want = pendingAsked[i];

        // Both sides came from the same API in the same internal units, so the
        // only tolerance needed is for floating point itself.
        if (Math.Abs(stored - want) <= Math.Abs(want) * 1e-9 + 1e-12)
        {
            if (!okIds.Contains(pending[i].Id)) okIds.Add(pending[i].Id);
        }
        else
        {
            if (!snappedIds.Contains(pending[i].Id)) snappedIds.Add(pending[i].Id);
        }
    }

    foreach (var id in snappedIds) snapped.Add(id);
    foreach (var id in okIds)
    {
        // A width that landed and a height that snapped is a SNAPPED element,
        // not a sized one. The element is only sized if nothing about it moved.
        if (!snappedIds.Contains(id)) sized++;
    }
}
