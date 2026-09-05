// NOT STANDALONE. Assumes `doc`, `wantPinned` and `categories` are in scope;
// leaves `elements`, `scanned` and `findings` behind.
//
// AN UNBOUNDED "EVERYTHING PINNED" IS MOSTLY LINKS AND DATUMS. Revit pins a
// linked model and an imported CAD file on insert, and most teams pin grids and
// levels early, so the answer to the unbounded question is dominated by things
// nobody asked about. The report names which case it just answered rather than
// handing back a number with no shape to it.
//
// THE UNPINNED SWEEP IS THE USEFUL ONE AND ALSO THE ENORMOUS ONE. "Which grids
// are not pinned" is a real check; "which elements are not pinned" is nearly
// the whole model. The scan size is reported beside the count so the second
// cannot be read as a finding.
//
// A PINNED GROUP AND ITS MEMBERS ARE SEPARATE FACTS. Pinning a group instance
// does not pin what is inside it, and each element is reported on its own.

var elements = new List<Element>();
var scanned = 0;
var findings = new List<string>();

var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
if (categories != null && categories.Count > 0)
    collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

foreach (var element in collector)
{
    scanned++;
    try
    {
        if (element.Pinned == wantPinned) elements.Add(element);
    }
    catch
    {
        // An element that will not report its pin state is not a match, and it
        // is still counted in the scan.
    }
}

var bounded = categories != null && categories.Count > 0;

findings.Add(string.Format("{0} of {1} scanned element(s) are {2}{3}",
    elements.Count,
    scanned,
    wantPinned ? "pinned" : "NOT pinned",
    bounded
        ? string.Format(", within {0} category/categories", categories.Count)
        : ", across every category"));

if (!bounded && wantPinned)
    findings.Add("This was an unbounded sweep, so most of that count will be linked models, imported "
        + "CAD and datums - Revit pins those on insert and most teams pin grids and levels early. "
        + "Give a category list to ask the question that was meant");

if (!bounded && !wantPinned)
    findings.Add("This was an unbounded sweep for UNPINNED elements, which is very nearly the whole "
        + "model. That is not a finding - give a category list, such as grids or levels, to make it "
        + "one");
