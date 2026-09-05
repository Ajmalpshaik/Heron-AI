// NOT STANDALONE. Assumes `doc`, `linkName` and `categories` are in scope;
// leaves `elements`, `linkTransform` and `findings` behind.
//
// WHAT COMES BACK BELONGS TO ANOTHER DOCUMENT. These elements live in the
// linked file. Their ids resolve only inside it, so any fragment that calls
// doc.GetElement(element.Id) on one gets the wrong element or nothing - and
// nothing can be written into a link from the host session anyway. Safe to
// READ off the object; not safe to move, set, tag or delete.
//
// THE GEOMETRY IS IN THE LINK'S OWN COORDINATES. Where the link was placed with
// an offset or a rotation, a point read off one of these is not a point in the
// host model until the transform is applied - which is why the transform is
// handed back rather than left for somebody to remember.
//
// AN UNLOADED LINK IS A REFUSAL. Zero elements would read as "the structural
// model is empty" rather than "nobody loaded it".
//
// A NAME MATCHING SEVERAL LINKS IS ALSO A REFUSAL. Two structural links are two
// right answers and no way to choose.

var elements = new List<Element>();
var linkTransform = Transform.Identity;
var findings = new List<string>();

var wanted = string.IsNullOrEmpty(linkName) ? "" : linkName.Trim();

var loaded = new List<RevitLinkInstance>();
var allNames = new List<string>();

foreach (var instance in new FilteredElementCollector(doc)
    .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
{
    allNames.Add(instance.Name ?? "(unnamed)");
    if (wanted.Length == 0) continue;
    if ((instance.Name ?? "").IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0)
        loaded.Add(instance);
}

if (wanted.Length == 0)
{
    findings.Add(string.Format("No link was named. This model has: {0}. LIST_LINKED_MODELS reports "
        + "them properly, with their state",
        allNames.Count == 0 ? "no links at all" : string.Join(", ", allNames.ToArray())));
}
else if (loaded.Count == 0)
{
    findings.Add(string.Format("No link matches '{0}'. This model has: {1}",
        wanted,
        allNames.Count == 0 ? "no links at all" : string.Join(", ", allNames.ToArray())));
}
else if (loaded.Count > 1)
{
    var matched = new List<string>();
    foreach (var instance in loaded) matched.Add("'" + (instance.Name ?? "(unnamed)") + "'");

    findings.Add(string.Format("AMBIGUOUS: {0} links match '{1}' - {2}. Two of them are two right "
        + "answers, so name one exactly rather than have this pick",
        loaded.Count, wanted, string.Join(", ", matched.ToArray())));
}
else
{
    var instance = loaded[0];
    var linked = instance.GetLinkDocument();

    if (linked == null)
    {
        findings.Add(string.Format("'{0}' is present but NOT LOADED, so it has no document to read. "
            + "That is different from it being empty, which is what a zero here would look like",
            instance.Name));
    }
    else
    {
        linkTransform = instance.GetTotalTransform();

        var collector = new FilteredElementCollector(linked).WhereElementIsNotElementType();
        if (categories != null && categories.Count > 0)
            collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

        foreach (var element in collector) elements.Add(element);

        findings.Add(string.Format("{0} element(s) inside linked model '{1}'{2}",
            elements.Count, instance.Name,
            categories == null || categories.Count == 0
                ? ", across every category"
                : string.Format(", within {0} category/categories", categories.Count)));

        findings.Add("THESE BELONG TO THE LINKED DOCUMENT, NOT THIS ONE. Read them - name, category, "
            + "parameters, geometry - but nothing may move, set, tag or delete them, and any fragment "
            + "that re-resolves their ids against this document will get the wrong element or none");

        if (!linkTransform.IsIdentity)
            findings.Add("The link is placed with an offset or a rotation, so a point read off one of "
                + "these elements is NOT a point in this model until the returned transform is "
                + "applied. Any distance or clash reasoning across the boundary needs that step");
    }
}
