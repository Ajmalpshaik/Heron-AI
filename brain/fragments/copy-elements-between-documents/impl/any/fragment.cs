// NOT STANDALONE. Assumes `doc`, `app`, `sourceDocumentTitle`, `categories`,
// `elementIds` and `offsetMm` are in scope; leaves `copied`, `copiedIds`,
// `report` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// COPIES ELEMENTS FROM ANOTHER OPEN DOCUMENT INTO THE ONE IN FRONT - Copy to
// Clipboard in one window and Paste Aligned in the other. Either side can be
// a project or a family: forms and lines from one family into another, model
// elements from one project into this one.
//
//   sourceDocumentTitle   part of the other document's title
//   categories            category names, comma separated, or "all" for
//                         every model element; EMPTY when ids are given
//   elementIds            ids in the OTHER document, comma separated
//   offsetMm              "x,y,z" moved on the way; EMPTY keeps them aligned
//
// View-specific elements (detail lines, text, dimensions) belong to a view
// and are left, counted: Revit copies those view to view. Types and
// materials the copies need come with them, as Revit's own paste brings them.
//
// READ BACK: every id Revit returns is looked up in this document.

const double MmPerFoot = 304.8;
var copied = 0;
var copiedIds = new List<ElementId>();
var lines = new List<string>();
var findings = new List<string>();
string refusal = null;

var wanted = (sourceDocumentTitle ?? "").Trim();
Document source = null;
var openTitles = new List<string>();
foreach (Document candidate in app.Documents)
{
    if (candidate.Equals(doc)) continue;
    var title = "";
    try { title = candidate.Title ?? ""; } catch { }
    openTitles.Add(title);
    if (wanted.Length > 0 && title.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0 && source == null) source = candidate;
}

var culture = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;
var offset = XYZ.Zero;
var offsetText = (offsetMm ?? "").Trim();
if (offsetText != "")
{
    var parts = offsetText.Split(',');
    double x, y, z;
    if (parts.Length == 3 && double.TryParse(parts[0].Trim(), numberStyle, culture, out x)
        && double.TryParse(parts[1].Trim(), numberStyle, culture, out y)
        && double.TryParse(parts[2].Trim(), numberStyle, culture, out z)) offset = new XYZ(x / MmPerFoot, y / MmPerFoot, z / MmPerFoot);
    else refusal = "The offset is x,y,z in millimetres. NOTHING WAS COPIED.";
}

if (refusal == null && source == null)
    refusal = "No other open document matches \"" + sourceDocumentTitle + "\" - both have to be open in this Revit; open now: "
        + (openTitles.Count > 0 ? string.Join(", ", openTitles) : "nothing else") + ". NOTHING WAS COPIED.";

var picked = new List<ElementId>();
var viewOwned = 0;
if (refusal == null)
{
    var idTexts = new HashSet<string>((elementIds ?? "").Split(',').Select(t => t.Trim()).Where(t => t != ""));
    var names = new HashSet<string>((categories ?? "").Split(',').Select(t => t.Trim().ToLowerInvariant()).Where(t => t != ""));
    var everything = names.Contains("all");
    if (idTexts.Count == 0 && names.Count == 0) refusal = "Give category names, \"all\", or element ids from the other document. NOTHING WAS COPIED.";
    else
    {
        foreach (var element in new FilteredElementCollector(source).WhereElementIsNotElementType())
        {
            var byId = idTexts.Contains(element.Id.ToString()) || idTexts.Contains(element.UniqueId);
            var category = element.Category;
            var byCategory = false;
            if (!byId && category != null && names.Count > 0)
            {
                var own = category.Name.ToLowerInvariant();
                var parent = category.Parent != null ? category.Parent.Name.ToLowerInvariant() : "";
                // "all" means model elements with an extent - a material,
                // a sketch plane or a type has none and is not pasted.
                byCategory = everything ? category.CategoryType == CategoryType.Model && element.get_BoundingBox(null) != null
                    : names.Contains(own) || names.Contains(parent);
            }
            if (!byId && !byCategory) continue;
            if (element.OwnerViewId != ElementId.InvalidElementId) { viewOwned++; continue; }
            picked.Add(element.Id);
        }
        if (picked.Count == 0) refusal = "Nothing in \"" + source.Title + "\" matched" + (viewOwned > 0 ? " outside a view (" + viewOwned + " view-specific left)" : "") + ". NOTHING WAS COPIED.";
    }
}

if (refusal == null)
{
    try
    {
        var arrived = ElementTransformUtils.CopyElements(source, picked, doc, Transform.CreateTranslation(offset), new CopyPasteOptions());
        doc.Regenerate();
        foreach (var id in arrived)
            if (doc.GetElement(id) != null) { copied++; copiedIds.Add(id); }
        var byCategory = copiedIds.Select(id => doc.GetElement(id)).Where(e => e.Category != null)
            .GroupBy(e => e.Category.Name).Select(g => g.Key + " " + g.Count());
        lines.Add(picked.Count + " element(s) picked in \"" + source.Title + "\"; " + copied + " arrived in \"" + doc.Title
            + "\" and read back: " + string.Join(", ", byCategory) + ".");
        if (viewOwned > 0) lines.Add(viewOwned + " view-specific element(s) left - Revit copies those view to view.");
    }
    catch (Exception ex) { refusal = "Revit refused the copy - " + ex.Message + ". NOTHING WAS COPIED."; }
}

if (refusal != null) lines.Add(refusal);
var report = string.Join(" | ", lines);
findings.Add(copied + " element(s) copied from another open document.");
findings.AddRange(lines);
