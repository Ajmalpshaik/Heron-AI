// NOT STANDALONE. Assumes `uidoc`, `doc`, `viewName` and `command` are in
// scope; leaves `posted`, `activeView`, `refused` and `findings` behind.
//
// NO TRANSACTION, AND IT CHANGES NOTHING IN THE MODEL. It asks Revit to open its
// own Render dialog - or Render in Cloud, or the Render Gallery - on the 3D
// view in front, by posting Revit's postable command. The card declares the
// ambient `noTransactionOpen` (D-125), so the show door runs it with none open.
//
// NO REVIT RELEASE LETS AN ADD-IN RUN THE RENDER. Read off the API reference of
// every release 2020 to 2027 on 2026-10-20: View3D reads and writes rendering
// settings and nothing renders. So the modeller presses Render in the dialog,
// and READ_RENDERED_IMAGES reads back what was saved - nothing is called
// rendered until an image has been read back.
//
// THE COMMAND IS POSTED, NOT RUN. UIApplication.PostCommand puts it in Revit's
// queue "to be invoked when control returns from the current API context" -
// after this fragment has answered. So `posted` says Revit took the request,
// never that the dialog is open.
//
// THE VIEW MUST BE THE ONE ON SCREEN. Revit's Render dialog renders the active
// view; a view change and a posted command both wait for the API context to
// end, and which runs first is not documented - so a view that is not in front
// is refused, and OPEN_VIEW puts it there first.

var findings = new List<string>();
var posted = false;
var activeView = "";
string refused = null;

var words = new Dictionary<string, PostableCommand>
{
    { "render", PostableCommand.Render },
    { "renderincloud", PostableCommand.RenderInCloud }, { "cloud", PostableCommand.RenderInCloud },
    { "rendergallery", PostableCommand.RenderGallery }, { "gallery", PostableCommand.RenderGallery },
};
Func<string, string> squash = text =>
    new string((text ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

View active = null;
try { active = uidoc == null ? null : uidoc.ActiveView; } catch (Exception) { active = null; }
if (active != null) activeView = active.Name ?? "";

PostableCommand which = PostableCommand.Render;
if (uidoc == null) refused = "No project is open in front, so NOTHING HAS BEEN SENT TO REVIT.";
else if (!words.TryGetValue(squash(command), out which))
    refused = "'" + command + "' is not a render command. Say Render, Render in Cloud or Render Gallery. "
        + "NOTHING HAS BEEN SENT TO REVIT.";
else if (active == null)
    refused = "Revit has no view in front, so NOTHING HAS BEEN SENT TO REVIT.";
else if (!(active is View3D) && which == PostableCommand.Render)
    refused = "The view in front, '" + active.Name + "', is not a 3D view, and Revit renders a 3D view. Open the 3D "
        + "view first (OPEN_VIEW), then ask again. NOTHING HAS BEEN SENT TO REVIT.";
else if (!string.IsNullOrEmpty((viewName ?? "").Trim()) && squash(viewName) != "active"
         && !string.Equals(viewName.Trim(), active.Name, StringComparison.OrdinalIgnoreCase))
    refused = "'" + viewName.Trim() + "' is not the view in front ('" + active.Name + "'), and Revit's Render dialog "
        + "renders the view in front. Open '" + viewName.Trim() + "' first (OPEN_VIEW), then ask again. "
        + "NOTHING HAS BEEN SENT TO REVIT.";

if (refused == null)
{
    var id = RevitCommandId.LookupPostableCommandId(which);
    var app = uidoc.Application;
    if (id == null)
        refused = "This Revit has no " + which + " command to post. NOTHING HAS BEEN SENT TO REVIT.";
    else if (!app.CanPostCommand(id))
        refused = "Revit will not take " + which + " now - it is unavailable in this view or in this session (Render in "
            + "Cloud and the Render Gallery are Autodesk's online service). NOTHING HAS BEEN SENT TO REVIT.";
    else
    {
        try
        {
            app.PostCommand(id);
            posted = true;
        }
        catch (Exception ex)
        {
            refused = "Revit would not post " + which + ": " + ex.Message + " (Revit takes one posted command at a time). "
                + "NOTHING HAS BEEN SENT TO REVIT.";
        }
    }
}

if (posted)
    findings.Add("Asked Revit for its " + (which == PostableCommand.Render ? "Render dialog" : which.ToString())
        + " on '" + activeView + "'. It opens when this answer returns - THE REQUEST IS QUEUED, NOT YET DONE. "
        + (which == PostableCommand.Render
            ? "Press Render in it: no Revit release lets an add-in press it. When it finishes, Save to Project or "
                + "Export there, then READ_RENDERED_IMAGES reads back what was saved. Nothing is rendered until an "
                + "image has been read back."
            : "What it sends or shows is Autodesk's service, and READ_RENDERED_IMAGES reads back only what is saved "
                + "in the project or on this PC."));
if (refused != null) findings.Add(refused);
