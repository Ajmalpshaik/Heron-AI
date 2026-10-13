// NOT STANDALONE. Assumes `doc` is in scope; leaves `elements`, `unusedNames`,
// `usedCount` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE IMAGES IN MANAGE IMAGES THAT NOTHING USES - placed in no view or sheet,
// and named by no image parameter of any element or type - handed on as
// `elements`, so DELETE_ELEMENTS removes them in the next step and only after
// the modeller has seen the list. This deletes nothing itself.
//
// "USED" IS LOOKED FOR EVERYWHERE AN IMAGE CAN BE: every placed image (its
// type is the image), and every parameter of every element and every type
// whose value is an element id naming an image - Image, Type Image and any
// shared or family parameter of that kind. An image used only as a schedule's
// header picture is not seen by that search, and the card says so.

var elements = new List<Element>();
var unusedNames = new List<string>();
var findings = new List<string>();

var images = new Dictionary<ElementId, Element>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ImageType)))
    images[element.Id] = element;

var used = new HashSet<ElementId>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ImageInstance)))
    used.Add(element.GetTypeId());

Action<Element> scan = element =>
{
    try
    {
        foreach (Parameter p in element.Parameters)
        {
            if (p == null || p.StorageType != StorageType.ElementId || !p.HasValue) continue;
            var id = p.AsElementId();
            if (id != null && images.ContainsKey(id)) used.Add(id);
        }
    }
    catch { }
};
if (images.Count > 0)
{
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType()) scan(element);
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsElementType()) scan(element);
}

foreach (var pair in images)
{
    if (used.Contains(pair.Key)) continue;
    elements.Add(pair.Value);
    unusedNames.Add(pair.Value.Name);
}
var usedCount = images.Count - elements.Count;

findings.Add(images.Count + " image(s) in Manage Images; " + elements.Count + " used nowhere"
    + (elements.Count == 0 ? "." : ": " + string.Join(", ", unusedNames.Take(20)) + (unusedNames.Count > 20 ? ", and more." : "."))
    + " An image used only as a schedule header is not seen by this search - check those before deleting.");
