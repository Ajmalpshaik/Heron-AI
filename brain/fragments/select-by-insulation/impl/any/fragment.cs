// NOT STANDALONE. Assumes `doc`, `categories`, `wantInsulated` and
// `countLiningAsInsulated` are in scope; leaves `elements`, `scanned` and
// `findings` behind.
//
// IT ASKS THE INSULATION, NOT THE DUCT. A duct carries no reliable "am I
// insulated" parameter - the truth is a separate element recording which host
// it wraps. Reading the wrappers is the real relationship; a parameter is
// whatever somebody last typed.
//
// THE LOOKUP IS BUILT ONCE. Asking each duct "is anything wrapping you" is a
// whole-model scan per duct. One pass over the wrappers gives the same answer,
// and on a real model that is seconds against minutes.
//
// LINING IS NOT INSULATION. External thermal insulation and internal acoustic
// lining are different elements doing different jobs, and "insulated" means
// either depending on who is asking - so it is asked, and the report says
// which was counted.

var elements = new List<Element>();
var scanned = 0;
var findings = new List<string>();

var covered = new List<ElementId>();

Action<FilteredElementCollector> collectHosts = source =>
{
    foreach (var wrapper in source)
    {
        var covering = wrapper as InsulationLiningBase;
        if (covering == null) continue;
        try
        {
            var hostId = covering.HostElementId;
            if (hostId == ElementId.InvalidElementId) continue;

            var already = false;
            foreach (var known in covered)
            {
                if (known == hostId) { already = true; break; }
            }
            if (!already) covered.Add(hostId);
        }
        catch
        {
            // A wrapper that will not name its host tells us nothing about any
            // duct, and must not be allowed to stop the pass.
        }
    }
};

collectHosts(new FilteredElementCollector(doc).OfClass(typeof(DuctInsulation)));
collectHosts(new FilteredElementCollector(doc).OfClass(typeof(PipeInsulation)));
if (countLiningAsInsulated)
    collectHosts(new FilteredElementCollector(doc).OfClass(typeof(DuctLining)));

var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
if (categories != null && categories.Count > 0)
    collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

foreach (var element in collector)
{
    scanned++;

    var isCovered = false;
    foreach (var hostId in covered)
    {
        if (hostId == element.Id) { isCovered = true; break; }
    }

    if (isCovered == wantInsulated) elements.Add(element);
}

findings.Add(string.Format("{0} of {1} scanned element(s) are {2}. {3} host(s) in this model carry "
    + "insulation or lining in total{4}",
    elements.Count,
    scanned,
    wantInsulated ? "INSULATED" : "BARE - nothing wrapping them",
    covered.Count,
    countLiningAsInsulated
        ? ", and internal LINING was counted as insulated"
        : ", and internal lining was NOT counted - only external insulation"));

if (categories == null || categories.Count == 0)
    findings.Add("No category was given, so this swept every element in the model. Almost all of "
        + "them are bare because almost none of them could be insulated - name the duct or pipe "
        + "categories to make the count mean something");

if (!wantInsulated && elements.Count > 0)
    findings.Add("This is what has nothing on it, not a work list. Fittings, flex and equipment sit "
        + "in the same categories, and whether each one SHOULD be insulated is a judgement");
