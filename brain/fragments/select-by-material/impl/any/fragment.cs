// NOT STANDALONE. Assumes `doc`, `materialName`, `includePaint`, `categories`
// and `includeLinks` are in scope; leaves `elements`, `scanned`, `findings`,
// `linksSearched` and `linkedMatches` behind.
//
// THERE IS NO MATERIAL-TO-ELEMENTS LOOKUP IN REVIT, so this asks every element
// in scope what it is made of. That is why the category bound matters more here
// than in most filters, and why the scanned count is reported beside the answer
// rather than left implicit.
//
// PAINT IS A DIFFERENT QUESTION AND IS OFF BY DEFAULT. A material reaches an
// element either as its real make-up - the type's compound structure or the
// family's geometry - or as PAINT on one face. "What is this made of" and
// "what did we paint" have different answers, and folding them together
// silently inflates the first.
//
// A MISSED NAME IS REFUSED WITH THE REAL NAMES. Revit material names are long,
// near-duplicated and punctuated inconsistently - "Steel, Galvanized" against
// "Steel - Galvanized" - so a miss is far more often a spelling than an
// absence, and a bare zero reads as the wrong one.
//
// LINKS ARE READ ONLY WHEN ASKED FOR, AND THEY ARE COUNTED, NEVER SELECTED -
// D-59. Absent `includeLinks` means host only, which is what this did before
// and what its proof measured. When it is set, each loaded link is read with
// the same test and its matches are reported as TEXT in `linkedMatches`, one
// line per link. They never enter `elements`: that list feeds the next
// fragment in a chain, and the chain revives ids against the HOST document, so
// a linked id that happens to be in use in the host binds an unrelated element
// silently (FRAGMENT-ISSUES row 75, 2 of 1128 measured).
//
// NESTED LINKS ARE NOT READ, AND THE ANSWER COUNTS THEM - see
// SELECT_BY_CATEGORY_NAME, which carries the same rule.
//
// A LINK HAS ITS OWN MATERIALS, SO THE NAME IS LOOKED FOR IN EACH LINK. The
// architect's materials are usually in the architectural link and nowhere in
// the host, so a link is read even when this model has no material of that
// name - and a link without one says so on its own line.
//
// IN A FAMILY, EVERY FORM IS SCANNED WHATEVER THE CATEGORY LIST SAYS (version
// 3). A form in the Family Editor carries no category of its own - measured
// 2026-10-08, Revit 2024: Category is null on a form with no subcategory, and
// is the subcategory itself ("Ring", under Generic Models) on one that has
// one - so neither passes a filter for the family's category, and "Generic
// Models" in a Generic Model family scanned 0 of 0. The list still bounds
// everything else in the family - nested families, lines. A solid form whose
// material is <By Category> reports none, and the answer counts those.

var elements = new List<Element>();
var scanned = 0;
var findings = new List<string>();
var formsScanned = 0;
var formsByCategory = 0;

var wanted = string.IsNullOrEmpty(materialName) ? "" : materialName.Trim();

if (wanted.Length == 0)
{
    findings.Add("No material was named - say which material to look for");
}
else
{
    Material target = null;
    var available = new List<string>();

    foreach (var candidate in new FilteredElementCollector(doc)
        .OfClass(typeof(Material)).Cast<Material>())
    {
        available.Add(candidate.Name);
        if (target == null && string.Equals(candidate.Name, wanted, StringComparison.OrdinalIgnoreCase))
            target = candidate;
    }

    if (target == null)
    {
        // The list is the useful part: these names are long and near-duplicated,
        // so the next attempt should be a correction rather than another guess.
        var shown = available.Count > 40 ? available.GetRange(0, 40) : available;
        findings.Add(string.Format("No material is called '{0}'. This model has {1}: {2}{3}",
            wanted,
            available.Count == 0 ? "none" : string.Format("{0}", available.Count),
            string.Join(", ", shown.ToArray()),
            available.Count > shown.Count ? string.Format(" ... and {0} more", available.Count - shown.Count) : ""));
    }
    else
    {
        var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
        if (categories != null && categories.Count > 0)
            collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

        var inScope = collector.ToList();
        if (doc.IsFamilyDocument)
        {
            var seen = new HashSet<ElementId>(inScope.Select(e => e.Id));
            foreach (var form in new FilteredElementCollector(doc).OfClass(typeof(GenericForm)))
                if (seen.Add(form.Id)) inScope.Add(form);
        }

        foreach (var element in inScope)
        {
            scanned++;
            if (element is GenericForm) formsScanned++;
            try
            {
                var ids = element.GetMaterialIds(includePaint);
                // A void shows nothing, so its empty material is not news.
                if ((ids == null || ids.Count == 0) && element is GenericForm && ((GenericForm)element).IsSolid)
                {
                    var field = element.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                    if (field != null && field.AsElementId() == ElementId.InvalidElementId) formsByCategory++;
                }
                if (ids == null) continue;

                foreach (var id in ids)
                {
                    if (id == target.Id) { elements.Add(element); break; }
                }
            }
            catch
            {
                // An element that will not report its materials is not a match,
                // and is still counted in the scan.
            }
        }

        var bounded = categories != null && categories.Count > 0;

        findings.Add(string.Format("{0} of {1} scanned element(s) use '{2}'{3}. {4}",
            elements.Count, scanned, target.Name,
            bounded ? string.Format(", within {0} category/categories", categories.Count)
                    : ", across every category",
            includePaint
                ? "PAINT was included, so this is what is made of it OR painted with it"
                : "Paint was NOT included - this is what is really made of it"));

        if (!bounded)
            findings.Add("This asked every element in the model what it is made of. A category list "
                + "bounds the scan and makes the count mean something narrower");

        if (doc.IsFamilyDocument)
            findings.Add(string.Format("This is a family: all {0} form(s) in it were scanned whatever the "
                + "category list says - a form carries no category of its own, or only its subcategory, so a "
                + "category would miss it.{1}", formsScanned,
                formsByCategory > 0
                    ? string.Format(" {0} form(s) take their material <By Category> and report none - set one "
                        + "with SET_FAMILY_FORM_MATERIAL to have it found", formsByCategory)
                    : ""));
    }
}

// ---- D-59: which links, only when asked for --------------------------------

var linksSearched = 0;
var linkedMatches = new List<string>();
var linkedTotal = 0;
var nestedLinks = 0;
var linkBlocked = "";

// One entry per link FILE, keyed by link type - a file placed twice is one
// model placed twice, and counting placements would report a job with four
// links as having nine. LIST_LINKED_MODELS' rule, as REPORT_AREAS applies it.
var linkTypes = new List<ElementId>();
var linkDocs = new List<Document>();
var linkPlacements = new List<List<RevitLinkInstance>>();

if (includeLinks)
{
    foreach (var instance in new FilteredElementCollector(doc)
        .OfClass(typeof(RevitLinkInstance)).Cast<RevitLinkInstance>())
    {
        if (instance == null) continue;

        var typeId = instance.GetTypeId();
        if (typeId == null || typeId == ElementId.InvalidElementId) continue;

        var known = linkTypes.IndexOf(typeId);
        if (known >= 0) { linkPlacements[known].Add(instance); continue; }

        // LOADED IS ESTABLISHED BY ASKING FOR THE DOCUMENT, never by a status.
        Document linked = null;
        try { linked = instance.GetLinkDocument(); }
        catch (Exception) { linked = null; }
        if (linked == null) continue;

        linkTypes.Add(typeId);
        linkDocs.Add(linked);
        linkPlacements.Add(new List<RevitLinkInstance> { instance });

        try
        {
            nestedLinks += new FilteredElementCollector(linked)
                .OfClass(typeof(RevitLinkInstance)).GetElementCount();
        }
        catch (Exception) { }
    }
}

if (wanted.Length == 0 && linkDocs.Count > 0)
    linkBlocked = "Links NOT read: no material was named";

for (var i = 0; i < linkDocs.Count && linkBlocked.Length == 0; i++)
{
    var linked = linkDocs[i];

    Material linkTarget = null;
    try
    {
        foreach (var candidate in new FilteredElementCollector(linked)
            .OfClass(typeof(Material)).Cast<Material>())
        {
            if (string.Equals(candidate.Name, wanted, StringComparison.OrdinalIgnoreCase))
            {
                linkTarget = candidate;
                break;
            }
        }
    }
    catch (Exception) { }

    linksSearched++;

    if (linkTarget == null)
    {
        linkedMatches.Add(string.Format("{0}: no material called '{1}'", linked.Title, wanted));
        continue;
    }

    var linkedCollector = new FilteredElementCollector(linked).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        linkedCollector = linkedCollector.WherePasses(new ElementMulticategoryFilter(categories));

    var linkScanned = 0;
    var linkMatched = 0;
    foreach (var element in linkedCollector)
    {
        linkScanned++;
        try
        {
            var ids = element.GetMaterialIds(includePaint);
            if (ids != null && ids.Contains(linkTarget.Id)) linkMatched++;
        }
        catch (Exception) { }
    }

    linkedTotal += linkMatched;
    linkedMatches.Add(string.Format("{0}: {1} of {2} use '{3}'", linked.Title, linkMatched,
        linkScanned, linkTarget.Name));
}

// THE ANSWER SAYS WHAT IT READ. Asked-and-found, asked-and-none-loaded and not
// asked read differently on purpose - D-59's own worked example.
if (!includeLinks)
    linkedMatches.Insert(0, "Host model only - links not read");
else if (linkBlocked.Length > 0)
    linkedMatches.Insert(0, linkBlocked);
else if (linksSearched == 0)
    linkedMatches.Insert(0, "Links asked for, NONE loaded - host only");
else
    linkedMatches.Insert(0, string.Format("{0} link(s) read: {1} match(es), NOT selected",
        linksSearched, linkedTotal));

if (includeLinks && nestedLinks > 0)
    linkedMatches.Add(string.Format("{0} link placement(s) nested inside those links were NOT "
        + "read", nestedLinks));

if (linksSearched > 0)
    linkedMatches.Add("Linked elements are counted here, never selected - the next step would "
        + "look them up in this model and could bind the wrong element");
