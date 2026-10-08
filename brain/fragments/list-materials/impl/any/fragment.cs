// NOT STANDALONE. Assumes `doc` is in scope; leaves `materialCount`,
// `materialList`, `materialIds` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// ONE COLLECTOR OVER Material AND NOTHING ELSE. That is the whole cost, and
// it is why this is quick on any model. Whether anything USES a material is
// FIND_UNUSED_MATERIALS' question, and answering it means reading every
// element, every type and every painted face - a walk this deliberately does
// not make, so a quick list never quietly becomes a slow one.
//
// THE DOCUMENT IN FRONT, AND ONLY IT. In the Family Editor that is the
// family's own materials - never the project it will be loaded into, and
// never another document open beside it.
//
// SORTED BY NAME, IGNORING CASE - meant to be the Material Browser's order,
// so the two can be compared line by line. That they agree is not measured
// (NEEDS-CHECKING CX5).
//
// THE ID IS PRINTED, NEVER READ AS A NUMBER. `ElementId.ToString()` is on
// every release; `IntegerValue` is deprecated at 2024 and gone by 2026.
//
// THE WORKING LISTS ARE INSIDE A BLOCK. The add-in reports every top-level
// variable a fragment leaves (RevitFragment.Report), so a helper list or the
// per-class tally declared out here would arrive on the answer beside the four
// names above - the tally as "N entry(ies)", which the server flags as content
// NOT SENT.

var materialCount = 0;
var materialList = "";
var materialIds = new List<ElementId>();
var findings = new List<string>();

{
    var found = new List<Material>();

    foreach (var material in new FilteredElementCollector(doc)
                                 .OfClass(typeof(Material))
                                 .Cast<Material>())
    {
        if (material != null) found.Add(material);
    }

    found.Sort(delegate (Material a, Material b)
    {
        return StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name);
    });

    var rows = new List<string>();
    var byClass = new SortedDictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    foreach (var material in found)
    {
        materialIds.Add(material.Id);

        // An empty class is a real state - a material made by hand and never
        // given one - and it is said rather than left blank or left out.
        var materialClass = (material.MaterialClass ?? "").Trim();
        var shown = materialClass.Length > 0 ? materialClass : "no class";

        rows.Add(string.Format("{0} ({1}, id {2})", material.Name, shown, material.Id));

        int already;
        byClass.TryGetValue(shown, out already);
        byClass[shown] = already + 1;
    }

    materialCount = found.Count;
    materialList = string.Join("  ||  ", rows);

    var kind = doc.IsFamilyDocument ? "family" : "project";

    if (materialCount == 0)
    {
        findings.Add(string.Format("No materials at all in this {0}, '{1}'.", kind, doc.Title));
    }
    else
    {
        var classes = new List<string>();
        foreach (var pair in byClass) classes.Add(pair.Key + " " + pair.Value);

        findings.Add(string.Format("{0} material(s) in this {1}, '{2}'. By class: {3}.",
            materialCount, kind, doc.Title, string.Join(", ", classes)));
    }

    findings.Add("Every material the " + kind + " holds, used or not. Which ones nothing uses is "
        + "FIND_UNUSED_MATERIALS, which reads every element, type and painted face to say so.");
}
