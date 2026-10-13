// NOT STANDALONE. Assumes `doc` and `viewName` are in scope; leaves
// `previewView`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// THE VIEW A FAMILY'S PREVIEW PICTURE IS TAKEN FROM - the picture the family
// browser and Windows Explorer show - set to a named view of the family in
// front, and refreshed on every save. A clean 3D view, reference planes
// hidden, makes the picture people choose the family by.
//
// The picture itself is made by Revit when the family is saved: this sets
// where it comes from, and the next save draws it. SAVE_DOCUMENT saves.
//
// READ BACK: the preview view is read off the document's settings again.

var findings = new List<string>();
string refused = null;
var previewView = "";

View view = null;
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
{
    var v = element as View;
    if (v != null && !v.IsTemplate && string.Equals(v.Name, (viewName ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) { view = v; break; }
}

if (!doc.IsFamilyDocument) refused = "This is a project, not a family, so NOTHING WAS CHANGED. Open the family first.";
else if (view == null) refused = "No view is named \"" + viewName + "\" in this family. NOTHING WAS CHANGED.";

if (refused == null)
{
    var settings = doc.GetDocumentPreviewSettings();
    if (!settings.IsViewIdValidForPreview(view.Id))
        refused = "Revit will not take a preview from \"" + view.Name + "\". A plan, elevation or 3D view will do. NOTHING WAS CHANGED.";
    else
    {
        try
        {
            settings.PreviewViewId = view.Id;
            settings.ForceViewUpdate(true);
            var back = doc.GetElement(doc.GetDocumentPreviewSettings().PreviewViewId) as View;
            previewView = back == null ? "" : back.Name;
            if (previewView == view.Name)
                findings.Add("The preview picture will be taken from \"" + view.Name + "\" (read back) and drawn on the next save.");
            else refused = "Asked for \"" + view.Name + "\" and the preview reads back as \"" + previewView + "\".";
        }
        catch (Exception ex) { refused = "Revit refused: " + ex.Message + ". NOTHING WAS CHANGED."; }
    }
}
if (refused != null) findings.Add(refused);
