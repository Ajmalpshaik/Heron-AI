// NOT STANDALONE. Assumes `doc`, `level`, `areaSchemeName` and `viewName` are in
// scope; leaves `created`, `existing`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE SCHEME IS NAMED, NEVER GUESSED. Gross Building and Rentable measure to
// different wall faces; a name that matches none lists the ones there are.
//
// ONE PER LEVEL AND SCHEME. An area plan already there for this level in this
// scheme is reported by name and nothing is made.
//
// NO BOUNDARY IS DRAWN. CREATE_AREA_BOUNDARY_LINES is that step.

View created = null;
var existing = "";
var refused = new List<string>();
var findings = new List<string>();

var wanted = (areaSchemeName ?? "").Trim();
var name = (viewName ?? "").Trim();

if (level == null)
{
    refused.Add("No level was given, so NOTHING WAS CREATED.");
}
else if (wanted.Length == 0)
{
    refused.Add("No area scheme was named, so NOTHING WAS CREATED. Gross Building and Rentable give "
        + "different totals, so which one is asked for rather than assumed.");
}
else
{
    AreaScheme scheme = null;
    var schemes = new List<string>();
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(AreaScheme)))
    {
        schemes.Add(element.Name);
        if (string.Equals(element.Name, wanted, StringComparison.OrdinalIgnoreCase))
            scheme = element as AreaScheme;
    }

    if (scheme == null)
    {
        refused.Add("No area scheme is called '" + wanted + "', so NOTHING WAS CREATED. The schemes in "
            + "this model: " + (schemes.Count == 0 ? "none" : string.Join(", ", schemes)) + ".");
    }
    else
    {
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)))
        {
            var plan = element as ViewPlan;
            if (plan == null || plan.IsTemplate || plan.ViewType != ViewType.AreaPlan) continue;
            AreaScheme planScheme = null;
            try { planScheme = plan.AreaScheme; } catch { planScheme = null; }
            if (planScheme == null || planScheme.Id != scheme.Id) continue;
            if (plan.GenLevel == null || plan.GenLevel.Id != level.Id) continue;
            existing = plan.Name;
            break;
        }

        if (existing.Length > 0)
        {
            refused.Add("Level '" + level.Name + "' already has the area plan '" + existing + "' in the '"
                + scheme.Name + "' scheme, so NOTHING WAS CREATED. Place areas in that one - a second "
                + "plan of one scheme invites areas placed twice.");
        }
        else
        {
            try
            {
                var plan = ViewPlan.CreateAreaPlan(doc, scheme.Id, level.Id);
                if (plan == null)
                {
                    refused.Add("Revit returned no area plan and raised no error.");
                }
                else
                {
                    created = plan;
                    if (name.Length > 0)
                    {
                        try { plan.Name = name; }
                        catch (Exception ex)
                        {
                            findings.Add("Made the area plan, and Revit would not name it '" + name
                                + "' - " + ex.Message + " It keeps the name '" + plan.Name + "'.");
                        }
                    }
                    findings.Insert(0, "Made the area plan '" + plan.Name + "' for level '" + level.Name
                        + "' in the '" + scheme.Name + "' scheme.");
                    findings.Add("NO AREA BOUNDARY WAS DRAWN. CREATE_AREA_BOUNDARY_LINES draws them round "
                        + "the exterior walls; PLACE_AREAS then puts the areas in.");
                }
            }
            catch (Exception ex)
            {
                refused.Add("Revit refused to make the area plan - " + ex.Message + " Nothing was created.");
            }
        }
    }
}

foreach (var reason in refused) findings.Add(reason);
