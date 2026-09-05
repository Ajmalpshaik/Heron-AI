// NOT STANDALONE. Assumes `doc`, `materialName`, `includePaint` and
// `categories` are in scope; leaves `elements`, `scanned` and `findings` behind.
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

var elements = new List<Element>();
var scanned = 0;
var findings = new List<string>();

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

        foreach (var element in collector)
        {
            scanned++;
            try
            {
                var ids = element.GetMaterialIds(includePaint);
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
    }
}
