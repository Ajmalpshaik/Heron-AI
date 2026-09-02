// NOT STANDALONE. Assumes `doc`, `views` and `scopeBoxName` are in scope;
// leaves `assigned` and `refused` behind.
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A SCOPE BOX CANNOT BE CREATED FROM CODE ON ANY RELEASE HERE. Revit exposes no
// create call. The box is drawn once by hand and this puts it on as many views
// as the set needs - which is where the repeated work is.
//
// CLEARING IS THE SAME OPERATION. An empty name writes InvalidElementId, which
// is what "None" holds in the properties palette.
//
// THE VALUE IS WRITTEN AS AN ElementId, NEVER AS A NUMBER. The type went 64-bit
// at 2024 and reading one as an integer was removed by 2026; an id compared to
// an id is unaffected by both.

var assigned = 0;
var refused = new List<string>();

var clearing = string.IsNullOrEmpty(scopeBoxName);
Element scopeBox = null;

if (!clearing)
{
    foreach (var element in new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_VolumeOfInterest).WhereElementIsNotElementType())
    {
        if (string.Equals(element.Name, scopeBoxName, StringComparison.OrdinalIgnoreCase))
        {
            scopeBox = element;
            break;
        }
    }
}

if (!clearing && scopeBox == null)
{
    var available = new List<string>();
    foreach (var element in new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_VolumeOfInterest).WhereElementIsNotElementType())
    {
        available.Add(element.Name);
    }

    refused.Add(available.Count == 0
        ? string.Format("no scope box called '{0}', and this model has NONE at all. Revit exposes no "
            + "way to create one from code - draw it once by hand under the View tab, then run this",
            scopeBoxName)
        : string.Format("no scope box called '{0}'. This model has: {1}",
            scopeBoxName, string.Join(", ", available)));
}
else
{
    var targetId = scopeBox == null ? ElementId.InvalidElementId : scopeBox.Id;

    foreach (var view in views)
    {
        if (view == null || !view.IsValidObject) continue;

        var parameter = view.get_Parameter(BuiltInParameter.VIEWER_VOLUME_OF_INTEREST_CROP);

        if (parameter == null)
        {
            refused.Add(string.Format("'{0}' ({1}) has no scope box property - a schedule, a legend "
                + "and a sheet cannot take one", view.Name, view.ViewType));
            continue;
        }
        if (parameter.IsReadOnly)
        {
            refused.Add(string.Format("'{0}': the scope box is read-only here, usually because a view "
                + "template controls it - change the template instead", view.Name));
            continue;
        }

        try
        {
            parameter.Set(targetId);
            assigned++;
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}': {1}", view.Name, ex.Message));
        }
    }
}
