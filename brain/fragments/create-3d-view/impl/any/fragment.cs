// NOT STANDALONE. Assumes `doc`, `viewFamilyTypeId` and `name` are in scope;
// leaves `viewId`, `named` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE NAME IS SET SEPARATELY AND CAN FAIL ON ITS OWN. Revit refuses a view
// name that is already used, and it throws when it does - which would abandon
// a view that has ALREADY BEEN CREATED. So the view is made first, the name
// attempted after, and `named` reports whether it took. A caller gets a usable
// view with Revit's default name rather than an exception and an orphan.
//
// That split is the whole reason `named` exists as a separate output. Folding
// it into `refused` would report a complete failure for a view that is sitting
// in the Project Browser working perfectly.
//
// AN ISOMETRIC AND NOTHING ELSE. No section box, no isolation, no camera. Each
// of those is its own capability acting ON this view, and doing them here
// would make the fragment unusable for the cases that want only one.

var viewId = ElementId.InvalidElementId;
var named = false;
var refused = false;

try
{
    var view = View3D.CreateIsometric(doc, viewFamilyTypeId);
    if (view == null)
    {
        refused = true;
    }
    else
    {
        viewId = view.Id;

        if (!string.IsNullOrEmpty(name))
        {
            try
            {
                view.Name = name;
                named = view.Name == name;
            }
            catch (Exception)
            {
                // Almost always a name already in use. The VIEW IS FINE and is
                // reported as created; only the name did not take.
                named = false;
            }
        }
    }
}
catch (Exception)
{
    refused = true;
}
