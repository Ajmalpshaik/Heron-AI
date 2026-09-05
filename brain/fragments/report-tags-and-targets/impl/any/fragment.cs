// NOT STANDALONE. Assumes `doc` and `view` are in scope, and leaves
// `tagTargets`, `linkedTags`, `orphanTags`, `multiTags` and `tagCount` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE LINK-AWARE ACCESSOR, NOT THE LOCAL ONE.
//
// The local accessor returns only targets in THIS document. On an MEP job the
// architecture is a link, so every room tag and door tag comes back as an
// empty set - not an error, just nothing - and the report says the sheet has
// no tags on the very elements a coordination sheet exists to annotate. The
// link-aware form carries the pair (which link, which element inside it),
// which is what makes a linked target reportable at all.
//
// THE VERSION SPLIT IS A COMPILE-TIME ONE AND HAS TO BE.
//
// Before 2022 a tag pointed at exactly one element and the API was a property.
// From 2022 it can point at several and that property was REMOVED. Naming
// either directly picks a side and stops the fragment compiling on the other -
// and a try/catch cannot help, because a missing member fails the compile, not
// the run.
//
// HOW A LINKED TARGET IS TOLD FROM A LOCAL ONE.
//
// The pair carries an invalid link id when the target is in this document.
// That is the only reliable test: the two id spaces are unrelated, so reading
// the host id of a linked pair credits the tag to whatever element in this
// model happens to share that number.

var tagTargets = new Dictionary<ElementId, IList<ElementId>>();
var linkedTags = new Dictionary<ElementId, string>();
var orphanTags = new List<ElementId>();
var multiTags = new List<ElementId>();

var tags = new FilteredElementCollector(doc, view.Id)
    .OfClass(typeof(IndependentTag))
    .WhereElementIsNotElementType()
    .Cast<IndependentTag>()
    .ToList();

int tagCount = tags.Count;

// Link names resolved once. A report that says "the target is in link 418291"
// is not a report anybody can act on.
var linkNames = new Dictionary<ElementId, string>();
foreach (var link in new FilteredElementCollector(doc)
             .OfClass(typeof(RevitLinkInstance))
             .WhereElementIsNotElementType())
{
    string name = "(unnamed link)";
    try { if (!string.IsNullOrEmpty(link.Name)) name = link.Name; } catch { }
    linkNames[link.Id] = name;
}

foreach (var tag in tags)
{
    var targets = new List<ElementId>();
    var links = new List<string>();

#if REVIT2020 || REVIT2021
    // One target, one property. The pair still has to be split, because a tag
    // into a link is exactly as common here as it is later.
    LinkElementId pair = null;
    try { pair = tag.TaggedElementId; } catch { }
    if (pair != null)
    {
        ElementId linkId = pair.LinkInstanceId;
        ElementId targetId = pair.LinkedElementId != ElementId.InvalidElementId
            ? pair.LinkedElementId
            : pair.HostElementId;

        if (targetId != null && targetId != ElementId.InvalidElementId)
        {
            targets.Add(targetId);
            if (linkId != null && linkId != ElementId.InvalidElementId)
                links.Add(linkNames.ContainsKey(linkId) ? linkNames[linkId] : "(unknown link)");
        }
    }
#else
    // 2022 onwards. Several targets are legitimate and every one of them is a
    // real annotation - taking the first would report the rest as untagged and
    // have somebody add a second tag over one that already reads correctly.
    ICollection<LinkElementId> pairs = null;
    try { pairs = tag.GetTaggedElementIds(); } catch { }
    if (pairs != null)
    {
        foreach (var pair in pairs)
        {
            if (pair == null) continue;
            ElementId linkId = pair.LinkInstanceId;
            ElementId targetId = pair.LinkedElementId != ElementId.InvalidElementId
                ? pair.LinkedElementId
                : pair.HostElementId;

            if (targetId == null || targetId == ElementId.InvalidElementId) continue;
            targets.Add(targetId);
            if (linkId != null && linkId != ElementId.InvalidElementId)
                links.Add(linkNames.ContainsKey(linkId) ? linkNames[linkId] : "(unknown link)");
        }
    }
#endif

    if (targets.Count == 0)
    {
        // Still drawing on the sheet, pointing at nothing. This is the finding.
        orphanTags.Add(tag.Id);
        continue;
    }

    tagTargets[tag.Id] = targets;
    if (targets.Count > 1) multiTags.Add(tag.Id);
    if (links.Count > 0) linkedTags[tag.Id] = string.Join(", ", links.Distinct().ToArray());
}
