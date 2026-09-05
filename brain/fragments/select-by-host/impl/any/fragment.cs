// NOT STANDALONE. Assumes `doc`, `host` and `categories` are in scope; leaves
// `elements` and `findings` behind.
//
// TWO HOST MECHANISMS, AND ONE OF THEM IS EASY TO MISS. Host-based families
// record their parent on FamilyInstance.Host. Duct and pipe insulation and
// lining record theirs on InsulationLiningBase.HostElementId and have no
// FamilyInstance at all - so a filter that reads only the first finds no
// insulation and reports a confident zero.
//
// MEP FITTINGS ARE CONNECTED, NOT HOSTED. An elbow, a tee or a valve in a run
// has connectors and no host, so "what is on this pipe" comes back empty here -
// correct, and useless. That is the single most likely misuse of this filter,
// so the empty answer says it in words and names TRACE_CONNECTIVITY.
//
// PROXIMITY IS NOT HOSTING. Nothing here finds an element that merely touches,
// overlaps or shares a face with the host, and nothing here should.

var elements = new List<Element>();
var findings = new List<string>();

if (host == null)
{
    findings.Add("No host element was given - name the wall, ceiling, duct or other parent to "
        + "search under");
}
else
{
    var hostId = host.Id;

    var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

    var byFamilyHost = 0;
    var byInsulationHost = 0;

    foreach (var element in collector)
    {
        if (element.Id == hostId) continue;   // the host is not hosted on itself

        try
        {
            var instance = element as FamilyInstance;
            if (instance != null && instance.Host != null && instance.Host.Id == hostId)
            {
                elements.Add(element);
                byFamilyHost++;
                continue;
            }

            var wrap = element as InsulationLiningBase;
            if (wrap != null && wrap.HostElementId == hostId)
            {
                elements.Add(element);
                byInsulationHost++;
            }
        }
        catch
        {
            // An element that will not answer either question is not hosted here.
        }
    }

    var hostName = string.IsNullOrEmpty(host.Name) ? "that element" : host.Name;

    findings.Add(string.Format("{0} element(s) are hosted on '{1}' (id {2}): {3} host-based family "
        + "instance(s) and {4} insulation or lining wrap(s){5}",
        elements.Count, hostName, hostId, byFamilyHost, byInsulationHost,
        categories == null || categories.Count == 0
            ? ""
            : string.Format(", within {0} category/categories", categories.Count)));

    if (elements.Count == 0)
        findings.Add("Nothing is HOSTED on it. If the question was about fittings or accessories in "
            + "an MEP run, they are CONNECTED rather than hosted and this filter cannot see them - "
            + "TRACE_CONNECTIVITY follows connectors. If it was about things merely touching it, "
            + "hosting is not that relationship either");
}
