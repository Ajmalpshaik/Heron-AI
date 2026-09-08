// NOT STANDALONE. Assumes `doc`, `symbol`, `faces`, `points` and `direction` are
// in scope; leaves `created`, `placed`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// PLACE_FAMILY_INSTANCES CANNOT DO THIS. That one places at a point on a level,
// so a diffuser sits at a height that looks right and knows nothing about the
// ceiling. Placed here it is HOSTED: it moves with its host and dies with it.
//
// THE HOST IS VERIFIED AFTER PLACING, NOT ASSUMED. An instance that came back
// unhosted is the exact failure this exists to prevent, and it looks completely
// correct on screen.

var created = new List<FamilyInstance>();
var refused = new List<string>();
var findings = new List<string>();
var placed = 0;

if (symbol == null || !symbol.IsValidObject)
{
    findings.Add("No family type was given, so NOTHING WAS PLACED.");
}
else if (faces == null || points == null || faces.Count == 0)
{
    findings.Add("No faces were given, so NOTHING WAS PLACED. A face cannot be guessed - "
        + "the host has to be named.");
}
else if (faces.Count != points.Count)
{
    findings.Add(faces.Count + " face(s) and " + points.Count + " point(s) were given. They are "
        + "paired by position, so the counts must match. NOTHING WAS PLACED rather than half of it.");
}
else if (symbol.Family == null || symbol.Family.FamilyPlacementType != FamilyPlacementType.WorkPlaneBased)
{
    // Checked rather than attempted. The face overload raises an exception from
    // inside Revit for a level-based family, and it says nothing a modeller can
    // act on.
    findings.Add("\"" + symbol.Name + "\" is not a face-based family - its placement type is "
        + (symbol.Family == null ? "unknown" : symbol.Family.FamilyPlacementType.ToString())
        + ", and placing on a face needs a work-plane-based one. NOTHING WAS PLACED. "
        + "PLACE_FAMILY_INSTANCES is what places a level-based family.");
}
else
{
    // AN UNACTIVATED SYMBOL PLACES NOTHING AND RAISES NOTHING - the most silent
    // failure in the Revit API. The caller's transaction is what makes this legal.
    if (!symbol.IsActive)
    {
        symbol.Activate();
        findings.Add("\"" + symbol.Name + "\" was not activated in this project and has been. "
            + "An unactivated type places nothing and reports no error.");
    }

    var facing = direction == null ? XYZ.BasisX : direction;

    for (var i = 0; i < faces.Count; i++)
    {
        var face = faces[i];
        var point = points[i];

        if (face == null || point == null)
        {
            refused.Add("entry " + (i + 1) + " - no face or no point");
            continue;
        }

        // A reference into a linked model cannot host an element in THIS
        // document. Easy mistake: the ceiling you clicked is often in the
        // architectural link, and Revit's own error for it is obscure.
        var hostElement = doc.GetElement(face.ElementId);
        if (hostElement == null)
        {
            refused.Add("entry " + (i + 1) + " - the face names nothing in this project. "
                + "A face in a linked model cannot host an element here");
            continue;
        }
        if (hostElement is RevitLinkInstance)
        {
            refused.Add("entry " + (i + 1) + " - that face belongs to a LINKED model. "
                + "An element in this project cannot be hosted by another file");
            continue;
        }

        try
        {
            var instance = doc.Create.NewFamilyInstance(face, point, facing, symbol);

            if (instance == null)
            {
                refused.Add("entry " + (i + 1) + " - Revit returned nothing and raised nothing");
                continue;
            }

            // VERIFIED, NOT ASSUMED. Unhosted is the failure this fragment
            // exists to prevent, and nothing on screen would show it.
            if (instance.Host == null)
            {
                refused.Add("entry " + (i + 1) + " - placed but came back with NO HOST, which is "
                    + "the failure this exists to prevent. Id " + instance.Id);
                continue;
            }

            created.Add(instance);
            placed++;
        }
        catch (Exception ex)
        {
            refused.Add("entry " + (i + 1) + " - Revit refused: " + ex.Message);
        }
    }

    findings.Add("Placed " + placed + " of " + faces.Count + " instance(s), each verified as hosted "
        + "by the face it was put on.");
}

if (refused.Count > 0)
{
    findings.Add(refused.Count + " placement(s) were refused: " + string.Join("; ", refused.ToArray()));
}
