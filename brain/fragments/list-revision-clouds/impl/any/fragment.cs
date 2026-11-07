// NOT STANDALONE. Assumes `doc`, `elements`, `sheets`, `views` and `revision`
// are in scope; leaves `elements`, `cloudTable` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// EVERY REVISION CLOUD, AND WHERE IT PRINTS. A cloud is drawn in ONE view - a
// plan, a section, a drafting view or the sheet itself - and prints on every
// sheet that view is placed on. `RevisionCloud.GetSheetIds()` answers the
// second; the owner view answers the first. Both are reported, because "the
// cloud is on sheet 377" and "the cloud is drawn in Level 2 Ducts" are the two
// things a modeller needs to go and find it.
//
// THE ANSWER `elements` IS THE CLOUDS THEMSELVES, so the next step acts on
// exactly what was listed: SET_SELECTION selects and zooms to them,
// EDIT_REVISION_CLOUDS moves them to another revision or deletes them,
// WRITE_ELEMENT_PARAMETERS sets their Comments or Mark.
//
// THE FILTERS. `sheets` (numbers, or titles), `views` (names, or "Floor Plan:
// Level 2"), `revision` ("Seq. 20", 20, a description or a date), each a list
// with commas between, each left out to mean every one. Handed `elements` -
// the selection, or what the step before found - narrows it too: clouds keep
// those clouds, sheets and views act as `sheets` and `views`. A word that
// matches nothing is named, never dropped silently.
//
// THE TAG API PARTS AT 2022. `IndependentTag.TaggedLocalElementId` is 2020 to
// 2022, `GetTaggedLocalElementIds()` 2022 on, and the host compiles fragments
// with no release symbol defined - so a `#if` would take one branch on every
// release. Both are reached BY NAME and the one this Revit has is used.

var findings = new List<string>();
var cloudTable = "";
var handed = elements == null ? new List<Element>() : new List<Element>(elements);
elements = new List<Element>();

Func<string, string> quoted = text => string.IsNullOrEmpty(text) ? "(blank)" : "'" + text + "'";

Func<string, List<string>> words = text =>
{
    var list = new List<string>();
    var said = (text ?? "").Trim();
    if (said.Length == 0 || string.Equals(said, "all", StringComparison.OrdinalIgnoreCase)
        || string.Equals(said, "none", StringComparison.OrdinalIgnoreCase)) return list;
    foreach (var part in said.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        if (part.Trim().Length > 0) list.Add(part.Trim());
    return list;
};

Func<Revision, string> label = r =>
    "Seq. " + r.SequenceNumber + " " + quoted(r.Description) + " " + quoted(r.RevisionDate);

Func<View, string> viewLabel = v =>
    v is ViewSheet ? "sheet " + ((ViewSheet)v).SheetNumber + " " + quoted(v.Name)
                   : v.ViewType + ": " + quoted(v.Name);

if (doc.IsFamilyDocument)
{
    findings.Add("'" + doc.Title + "' is a family, and a family holds no revision clouds. "
               + "Bring a project to the front.");
}
else
{
    // ---- the filters ---------------------------------------------------------

    var allSheets = new List<ViewSheet>();
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)))
    {
        var s = e as ViewSheet;
        if (s != null && !s.IsTemplate) allSheets.Add(s);
    }

    var sheetFilter = new HashSet<ElementId>();
    foreach (var word in words(sheets))
    {
        var byNumber = allSheets.FindAll(s => string.Equals(s.SheetNumber, word, StringComparison.OrdinalIgnoreCase));
        var hits = byNumber.Count > 0 ? byNumber
                 : allSheets.FindAll(s => string.Equals(s.Name, word, StringComparison.OrdinalIgnoreCase));
        if (hits.Count == 0) findings.Add("No sheet numbered or titled '" + word + "' - left out of the filter.");
        foreach (var s in hits) sheetFilter.Add(s.Id);
    }

    var viewFilter = new HashSet<ElementId>();
    var viewWords = words(views);
    if (viewWords.Count > 0)
    {
        var allViews = new List<View>();
        foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(View)))
        {
            var v = e as View;
            if (v != null && !v.IsTemplate) allViews.Add(v);
        }
        foreach (var word in viewWords)
        {
            var hits = allViews.FindAll(v =>
                string.Equals(v.Name, word, StringComparison.OrdinalIgnoreCase)
                || string.Equals(v.ViewType + ": " + v.Name, word, StringComparison.OrdinalIgnoreCase));
            if (hits.Count == 0) findings.Add("No view called '" + word + "' - left out of the filter.");
            if (hits.Count > 1)
            {
                var names = new List<string>();
                foreach (var v in hits) names.Add(viewLabel(v));
                findings.Add("'" + word + "' names " + hits.Count + " views (" + string.Join(", ", names.ToArray())
                           + ") - all of them are used. Say \"Floor Plan: " + word + "\" for one.");
            }
            foreach (var v in hits) viewFilter.Add(v.Id);
        }
    }

    var revisionFilter = new HashSet<ElementId>();
    var revisionWords = words(revision);
    var refusedRevision = false;
    foreach (var word in revisionWords)
    {
        var bare = word;
        if (bare.StartsWith("Seq.", StringComparison.OrdinalIgnoreCase)) bare = bare.Substring(4).Trim();
        else if (bare.StartsWith("Seq", StringComparison.OrdinalIgnoreCase)) bare = bare.Substring(3).Trim();
        var hits = new List<Revision>();
        foreach (var id in Revision.GetAllRevisionIds(doc))
        {
            var r = doc.GetElement(id) as Revision;
            if (r == null) continue;
            if (string.Equals(r.SequenceNumber.ToString(), bare, StringComparison.Ordinal)
                || string.Equals(r.Id.ToString(), word, StringComparison.Ordinal)
                || string.Equals(r.Name ?? "", word, StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.Description ?? "", word, StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.RevisionDate ?? "", word, StringComparison.OrdinalIgnoreCase))
                hits.Add(r);
        }
        if (hits.Count == 1) { revisionFilter.Add(hits[0].Id); continue; }
        refusedRevision = true;
        if (hits.Count == 0)
            findings.Add("revision '" + word + "' names no revision in this project. Give its sequence, like Seq. 20.");
        else
        {
            var names = new List<string>();
            foreach (var r in hits) names.Add(label(r));
            findings.Add("revision '" + word + "' matches " + hits.Count + " revisions (" + string.Join("; ", names.ToArray())
                       + "). Give its sequence.");
        }
    }

    // What was handed in narrows it too.
    var cloudFilter = new HashSet<ElementId>();
    var ignored = 0;
    foreach (var e in handed)
    {
        if (e == null) continue;
        if (e is RevisionCloud) cloudFilter.Add(e.Id);
        else if (e is ViewSheet) sheetFilter.Add(e.Id);
        else if (e is View) viewFilter.Add(e.Id);
        else ignored++;
    }
    if (ignored > 0)
        findings.Add(ignored + " element(s) handed in are not clouds, sheets or views, and did not narrow the list.");

    // A FILTER WHOSE EVERY WORD MISSED IS NOT "NO FILTER". A typed sheet that
    // does not exist must not widen the answer to every cloud in the project.
    var sheetAsked = words(sheets).Count > 0 || sheetFilter.Count > 0;
    var viewAsked = viewWords.Count > 0 || viewFilter.Count > 0;

    if (refusedRevision || (sheetAsked && sheetFilter.Count == 0) || (viewAsked && viewFilter.Count == 0))
    {
        findings.Add("NOTHING WAS LISTED - a filter matched nothing, and listing every cloud instead "
                   + "would answer a question nobody asked.");
    }
    else
    {
        // ---- which sheet each view sits on, once ------------------------------
        var sheetOfView = new Dictionary<ElementId, ElementId>();
        foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(Viewport)))
        {
            var vp = e as Viewport;
            if (vp != null && !sheetOfView.ContainsKey(vp.ViewId)) sheetOfView[vp.ViewId] = vp.SheetId;
        }

        // ---- every cloud tag, once ------------------------------------------
        var tagOf = new Dictionary<ElementId, List<ElementId>>();
        var tagType = typeof(IndependentTag);
        var many = tagType.GetMethod("GetTaggedLocalElementIds", Type.EmptyTypes);   // 2022 on
        var one = tagType.GetProperty("TaggedLocalElementId");                      // 2020 to 2022
        foreach (var e in new FilteredElementCollector(doc)
                              .OfCategory(BuiltInCategory.OST_RevisionCloudTags)
                              .WhereElementIsNotElementType())
        {
            var tag = e as IndependentTag;
            if (tag == null) continue;
            var tagged = new List<ElementId>();
            try
            {
                if (many != null)
                {
                    var ids = many.Invoke(tag, null) as IEnumerable<ElementId>;
                    if (ids != null) tagged.AddRange(ids);
                }
                else if (one != null)
                {
                    var id = one.GetValue(tag, null) as ElementId;
                    if (id != null) tagged.Add(id);
                }
            }
            catch { }
            foreach (var id in tagged)
            {
                if (!tagOf.ContainsKey(id)) tagOf[id] = new List<ElementId>();
                tagOf[id].Add(tag.Id);
            }
        }

        // ---- the clouds -----------------------------------------------------
        var rows = new List<KeyValuePair<string, string>>();
        foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(RevisionCloud)))
        {
            var cloud = e as RevisionCloud;
            if (cloud == null) continue;

            var onSheets = new HashSet<ElementId>();
            try { foreach (var id in cloud.GetSheetIds()) onSheets.Add(id); } catch { }
            var owner = doc.GetElement(cloud.OwnerViewId) as View;
            if (owner is ViewSheet) onSheets.Add(owner.Id);
            if (owner != null && sheetOfView.ContainsKey(owner.Id)) onSheets.Add(sheetOfView[owner.Id]);

            if (cloudFilter.Count > 0 && !cloudFilter.Contains(cloud.Id)) continue;
            if (revisionFilter.Count > 0 && !revisionFilter.Contains(cloud.RevisionId)) continue;
            if (viewFilter.Count > 0 && !viewFilter.Contains(cloud.OwnerViewId)) continue;
            if (sheetFilter.Count > 0)
            {
                var any = false;
                foreach (var id in onSheets) if (sheetFilter.Contains(id)) { any = true; break; }
                if (!any) continue;
            }

            elements.Add(cloud);

            var rev = doc.GetElement(cloud.RevisionId) as Revision;
            var sheetNumbers = new List<string>();
            foreach (var id in onSheets)
            {
                var s = doc.GetElement(id) as ViewSheet;
                if (s != null) sheetNumbers.Add(s.SheetNumber);
            }
            sheetNumbers.Sort(StringComparer.OrdinalIgnoreCase);

            string comments = null, mark = null;
            try
            {
                var p = cloud.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (p != null) comments = p.AsString();
                p = cloud.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                if (p != null) mark = p.AsString();
            }
            catch { }

            var tags = new List<string>();
            if (tagOf.ContainsKey(cloud.Id)) foreach (var id in tagOf[cloud.Id]) tags.Add(id.ToString());

            var line = "cloud " + cloud.Id + ": "
                + (rev == null ? "NO REVISION" : label(rev))
                + "; drawn in " + (owner == null ? "an unreadable view" : viewLabel(owner))
                + "; prints on " + (sheetNumbers.Count == 0 ? "NO SHEET" : "sheet " + string.Join(", ", sheetNumbers.ToArray()))
                + "; Comments " + quoted(comments) + "; Mark " + quoted(mark)
                + "; tag " + (tags.Count == 0 ? "none" : string.Join(", ", tags.ToArray()));
            var key = (sheetNumbers.Count == 0 ? "~" : sheetNumbers[0]) + "|" + (rev == null ? 0 : rev.SequenceNumber).ToString("D6");
            rows.Add(new KeyValuePair<string, string>(key, line));
        }

        // In register order, then by revision: the order a modeller checks a set in.
        rows.Sort((a, b) => string.Compare(a.Key, b.Key, StringComparison.OrdinalIgnoreCase));
        var lines = new List<string>();
        foreach (var row in rows) lines.Add(row.Value);
        cloudTable = string.Join(" || ", lines.ToArray());

        if (elements.Count == 0)
            findings.Add("No revision cloud matches - the filters are in the lines above, if any were given.");
        else
            findings.Add(elements.Count + " revision cloud(s). Each line: its revision, the view it is drawn "
                       + "in, the sheets it prints on, Comments, Mark and tag.");
    }
}
