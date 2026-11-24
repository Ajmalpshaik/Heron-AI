// NOT STANDALONE. Assumes `doc`, `wantPinned` and `categories` are in scope;
// leaves `elements`, `scanned`, `unresolvedElements` and `findings` behind.
//
// VERSION 2 (2026-11-24, D-64, row 5b-1909, work-note item WN-7 of issue
// #1005) - AN ELEMENT THAT WILL NOT SAY WHETHER IT IS PINNED IS COUNTED.
// Version 1 passed over it without a word, though `scanned` counted it, so
// "N of M scanned" mixed it in with the ones that were read.
// `unresolvedElements` carries the count every time; the sentence naming it
// is added only when it is above 0, so every other answer reads as version
// 1's.
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
// VERSION 2 (D-64): elements whose pinned state Revit would not give.
var unresolvedElements = 0;
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
        // is still counted in the scan - and, since version 2 (D-64), counted
        // as passed over.
        unresolvedElements++;
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

// VERSION 2 (D-64): said only when an element was passed over.
if (unresolvedElements > 0)
    findings.Add(string.Format("{0} of the scanned element(s) would not say whether they are pinned, so they "
        + "are in neither answer", unresolvedElements));

if (!bounded && wantPinned)
    findings.Add("This was an unbounded sweep, so most of that count will be linked models, imported "
        + "CAD and datums - Revit pins those on insert and most teams pin grids and levels early. "
        + "Give a category list to ask the question that was meant");

if (!bounded && !wantPinned)
    findings.Add("This was an unbounded sweep for UNPINNED elements, which is very nearly the whole "
        + "model. That is not a finding - give a category list, such as grids or levels, to make it "
        + "one");
