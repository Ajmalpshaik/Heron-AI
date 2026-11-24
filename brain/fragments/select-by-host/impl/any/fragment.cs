// NOT STANDALONE. Assumes `doc`, `host` and `categories` are in scope; leaves
// `elements`, `unresolvedElements` and `findings` behind.
//
// VERSION 2 (2026-11-24, D-64, row 5b-1909, work-note item WN-7 of issue
// #1005) - AN ELEMENT THAT WILL NOT SAY ITS HOST IS COUNTED. Version 1 read
// an element whose host Revit would not give as not hosted here, without a
// word, so "Nothing is HOSTED on it" could be said with one of them hosted
// there - a failed read answered as a no (D-52). `unresolvedElements` carries
// the count every time; the sentence naming it is added only when it is above
// 0, so every other answer reads as version 1's.
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
// VERSION 2 (D-64): elements whose host Revit would not give.
var unresolvedElements = 0;

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
            // An element that will not answer either question is passed over -
            // counted (version 2, D-64), never read as not hosted here.
            unresolvedElements++;
        }
    }

    var hostName = string.IsNullOrEmpty(host.Name) ? "that element" : host.Name;

    findings.Add(string.Format("{0} element(s) are hosted on '{1}' (id {2}): {3} host-based family "
        + "instance(s) and {4} insulation or lining wrap(s){5}",
        elements.Count, hostName, hostId, byFamilyHost, byInsulationHost,
        categories == null || categories.Count == 0
            ? ""
            : string.Format(", within {0} category/categories", categories.Count)));

    // VERSION 2 (D-64): said only when an element was passed over.
    if (unresolvedElements > 0)
        findings.Add(string.Format("{0} element(s) would not say what hosts them, so they were passed over - "
            + "one of them may be hosted on it", unresolvedElements));

    if (elements.Count == 0)
        findings.Add("Nothing is HOSTED on it. If the question was about fittings or accessories in "
            + "an MEP run, they are CONNECTED rather than hosted and this filter cannot see them - "
            + "TRACE_CONNECTIVITY follows connectors. If it was about things merely touching it, "
            + "hosting is not that relationship either");
}
