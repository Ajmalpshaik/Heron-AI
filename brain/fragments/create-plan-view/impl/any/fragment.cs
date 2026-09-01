// NOT STANDALONE. Assumes `doc`, `levelId`, `planKind` and `viewName` are in
// scope; leaves `created` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE VIEW TYPE IS FOUND BY WHAT IT IS, NOT BY ITS NAME. Every project renames
// its view family types, so matching on `ViewFamily.FloorPlan` finds the right
// one in a template nobody here has seen. Matching on the string "Floor Plan"
// finds it in an English project set up the default way and nowhere else.
//
// FLOOR AND CEILING ARE DIFFERENT DRAWINGS, and the difference is not cosmetic:
// a reflected ceiling plan looks UPWARD. Handing somebody an RCP when they
// wanted a floor plan gives a drawing that is correct, unexpected and wastes
// the trip, so the kind is required rather than defaulted.
//
// THIS DOES NOT CHECK WHETHER A PLAN ALREADY EXISTS. Revit's own Plan Views
// dialog hides levels that already have one, which reads like a rule and is
// not: the API creates a second happily, and a second plan on a level is a
// normal thing to want. Refusing here would block a legitimate job on a
// misreading of the UI.

ElementId created = null;
string refused = null;

var kind = (planKind ?? "").Trim().ToLowerInvariant();

var wantCeiling = kind.Contains("ceiling") || kind.Contains("rcp");
var wantFloor = kind.Contains("floor") || kind.Contains("plan") && !wantCeiling;

if (!wantCeiling && !wantFloor)
{
    refused = "say which plan - a floor plan or a reflected ceiling plan. They are "
            + "different drawings and an RCP looks upward";
}
else if (!(doc.GetElement(levelId) is Level))
{
    refused = "that is not a level in this project, and a plan is a plan OF a level";
}
else
{
    var family = wantCeiling ? ViewFamily.CeilingPlan : ViewFamily.FloorPlan;

    ViewFamilyType planType = null;
    foreach (var candidate in new FilteredElementCollector(doc)
                                  .OfClass(typeof(ViewFamilyType))
                                  .Cast<ViewFamilyType>())
    {
        if (candidate.ViewFamily == family) { planType = candidate; break; }
    }

    if (planType == null)
    {
        refused = string.Format(
            "this project's template carries no {0} view type, so there is nothing to "
            + "create one from", wantCeiling ? "ceiling plan" : "floor plan");
    }
    else
    {
        var view = ViewPlan.Create(doc, planType.Id, levelId);

        if (view == null)
        {
            refused = "Revit declined to create the plan";
        }
        else
        {
            // Revit names it after the level, which is a real name. An empty
            // request leaves that rather than writing blank over it.
            var name = (viewName ?? "").Trim();
            if (name.Length > 0) view.Name = name;

            created = view.Id;
        }
    }
}
