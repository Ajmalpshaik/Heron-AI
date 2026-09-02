// NOT STANDALONE. Assumes `doc`, `view` and `elements` are in scope; leaves
// `alreadyTagged` and `untagged` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// COLLECTED ONCE FOR THE VIEW, NOT ASKED PER ELEMENT. A view with two thousand
// elements would otherwise mean two thousand collector passes, and the cost of
// this check is what decides whether anybody runs it before issuing.
//
// THERE IS NO ONE ACCESSOR THAT SPANS 2020 TO 2027, AND THE COMPILER IS WHAT
// ESTABLISHED THAT RATHER THAN ANY DOCUMENTATION.
//
//   TaggedElementId            2020 to 2026. REMOVED IN 2027
//   GetTaggedLocalElementIds   ADDED IN 2022, with multi-reference tags
//
// The overlap is 2022-2026 and neither covers the whole span. The first
// version of this fragment used TaggedElementId for all eight releases,
// compiled clean on 2020 and 2022, and failed on 2027 - which is precisely the
// "worked in 2020, errors on the new one" class this gate exists to catch, and
// it would otherwise have been found by the owner mid-job.
//
// So the split is by compile symbol, which is this repository's existing
// pattern for a version difference (Directory.Build.props defines REVIT2020
// through REVIT2027).
//
// >> A NOTE FOR WHOEVER BUILDS THE EXECUTOR (D-28, unbuilt): this fragment
// >> REQUIRES the in-process Roslyn compilation to define the same REVIT20xx
// >> symbol that MSBuild does. It is the first fragment in the library to
// >> depend on one. A Roslyn host that defines no symbols will take the #else
// >> branch and fail to compile on 2020 and 2021 - silently, and only there.
//
// A TAG POINTING INTO A LINKED MODEL IS NOT A TAG ON THIS ELEMENT.
// `LinkElementId` carries both a host id and a linked id, and reading the host
// id blindly would credit a tag on a linked duct to a duct in this model that
// happens to share an id. The two id spaces are unrelated, so only tags whose
// host id is real are counted.

var tagged = new HashSet<ElementId>();

var tags = new FilteredElementCollector(doc, view.Id)
    .OfClass(typeof(IndependentTag))
    .Cast<IndependentTag>();

foreach (var tag in tags)
{
#if REVIT2020 || REVIT2021
    // The only accessor these two releases have. A tag into a link has
    // InvalidElementId as its HOST id: skipped rather than recorded, because
    // crediting it would report an element tagged when nothing in this model
    // points at it, and it would be issued untagged.
    var target = tag.TaggedElementId;
    if (target == null) continue;
    if (target.HostElementId == null) continue;
    if (target.HostElementId == ElementId.InvalidElementId) continue;

    tagged.Add(target.HostElementId);
#else
    // 2022 onwards. One tag can reference SEVERAL elements, so every id it
    // carries counts as tagged - a multi-reference tag genuinely tags all of
    // them, and taking only the first would report the rest untagged and have
    // somebody add a second tag on top of one that already reads correctly.
    // This accessor returns LOCAL ids only, so the linked-model case above is
    // handled by the API rather than needing the guard.
    foreach (var id in tag.GetTaggedLocalElementIds())
    {
        if (id == null || id == ElementId.InvalidElementId) continue;
        tagged.Add(id);
    }
#endif
}

// The untagged set REPLACES `elements`, which is deliberate: it is what the
// next step acts on, and it is the name every action in this library consumes.
// Read the incoming list first, then rebind - the two cannot be the same object
// while it is being walked.
var alreadyTagged = new List<ElementId>();
var remaining = new List<Element>();

foreach (var element in elements)
{
    if (element == null) continue;

    if (tagged.Contains(element.Id)) alreadyTagged.Add(element.Id);
    else remaining.Add(element);
}

elements = remaining;
