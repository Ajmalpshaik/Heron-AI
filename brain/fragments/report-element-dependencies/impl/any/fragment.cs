// NOT STANDALONE. Assumes `elements` and `doc` are in scope, and leaves
// `dependents`, `joined`, `undeletable`, `standalone`, `dimensionsDeleted`,
// `dimensionsCut`, `dimensionLines`, `dimensionsRead`, `dimensionsUnread` and
// `goesTotal` behind.
//
// READ ONLY. Opens no transaction and changes nothing - which is the point:
// the question is asked BEFORE the change, not discovered after it.
//
// `dependents` IS REVIT'S OWN LIST - AND IT IS NOT EVERYTHING A DELETE TAKES.
//
// `GetDependentElements(null)` - null meaning "no filter, everything" - returns
// what Revit itself treats as attached: a tag referencing the element, a family
// hosted in it, the sketch a floor is drawn from, an opening cut through it.
// Version 1 said it was "the same set it removes on delete". It is not: on
// 2026-11-23, Revit 2020, three in-place units reported 36 dependents and the
// delete of them took 62 elements, and cut 26 more dimensions (FRAGMENT-ISSUES
// row 5b-1990, issue #1278). Passing a filter to the same call only narrows
// the same list, so it cannot find what the list leaves out.
//
// VERSION 2 - EVERY DIMENSION THAT MEASURES TO WHAT GOES, IN EVERY VIEW.
//
// Every dimension in the model is read - not the active view's, the model's:
// a collector with no view - and each one whose references touch what goes
// (the elements given AND every dependent Revit listed for them) is named:
//
//   DELETED - too few references would be left for it to stand: fewer than
//             two of a linear string, or any of an angular, radial, diameter,
//             arc-length or spot dimension. In `dimensionsDeleted` only when
//             Revit did NOT already list it as a dependent, so no dimension
//             is counted twice.
//   CUT     - a linear string keeping two or more references: Revit removes
//             the segments to what goes and the dimension stays, changed.
//             Not deleted, so not in the delete count - but a changed
//             drawing nobody asked for, so it is named in `dimensionsCut`.
//
// Each one is a line in `dimensionLines` with its view, so the question
// before a delete can name them by view. `goesTotal` is every distinct
// element the delete is expected to take - the ones given, Revit's
// dependents and `dimensionsDeleted` - the number Revit's Delete box should
// show. THE DELETED/CUT RULE IS READ FROM WHAT REVIT DID ON 2026-11-23, NOT
// FROM ANY API PROMISE: nothing in the API says in advance what a delete will
// do to a dimension, so the rule is checked in Revit before this is trusted
// (NEEDS-CHECKING group QT). A dimension whose references Revit will not give
// is in `dimensionsUnread` - looked at, not answered - never counted as clear.
//
// IT ALWAYS CONTAINS THE ELEMENT ITSELF.
//
// Not a defect, and not something to strip blindly - it is why the count reads
// one higher than people expect. It is removed here by identity and the
// remainder reported, so the number is never quietly wrong in either direction.
//
// "CAN I DELETE IT" IS A DIFFERENT QUESTION.
//
// An element with zero dependents can still be undeletable: pinned, the last
// level, owned by another user in a workshared model. `CanDeleteElement` is
// Revit's own answer and is asked separately. Reporting only the dependents
// would say "nothing attached, safe to remove" about something that cannot be
// removed at all.
//
// JOINED IS NOT DEPENDENT, AND MERGING THEM WOULD LIE TWICE.
//
// Revit does not delete a joined neighbour - it re-cleans the geometry. A wall
// joined to three others changes SHAPE when one goes, in a drawing nobody was
// looking at. Listing it as a dependent would say it gets deleted, which is
// false; leaving it out would say nothing happens to it, which is also false.
//
// `GetJoinedElements` THROWS for an element that cannot be joined at all - a
// tag, a view, an annotation - rather than returning an empty list. On a mixed
// selection that is the difference between a report and a crash, so it is
// guarded per element.

var dependents = new Dictionary<ElementId, IList<ElementId>>();
var joined = new Dictionary<ElementId, IList<ElementId>>();
var undeletable = new List<ElementId>();
var standalone = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;
    var id = element.Id;

    var attached = new List<ElementId>();
    try
    {
        var reported = element.GetDependentElements(null);
        if (reported != null)
        {
            foreach (var dependentId in reported)
            {
                // The element itself is always in there. Excluded by identity,
                // not by trimming the first entry - the order is not promised.
                if (dependentId == id) continue;
                attached.Add(dependentId);
            }
        }
    }
    catch { }

    // VERSION 3 (row 5b-2068, issue #1478): ONLY AN ELEMENT SOMETHING GOES
    // WITH IS A KEY, as `joined` already was. Version 2 wrote an empty list for
    // every element given, so three free walls read "dependents 3 entry(ies)" -
    // an echo of the input, never empty, and D-30's empty leg could not be
    // shown. A free element is in `standalone` instead (when Revit allows the
    // delete and nothing is joined to it).
    if (attached.Count > 0) dependents[id] = attached;

    var joinedTo = new List<ElementId>();
    try
    {
        var neighbours = JoinGeometryUtils.GetJoinedElements(doc, element);
        if (neighbours != null) joinedTo.AddRange(neighbours);
    }
    catch { }

    if (joinedTo.Count > 0) joined[id] = joinedTo;

    bool canDelete = false;
    try { canDelete = DocumentValidation.CanDeleteElement(doc, id); } catch { }
    if (!canDelete) undeletable.Add(id);

    // Nothing attached, nothing joined, and Revit will allow it. Said as its
    // own fact rather than left to be inferred from three empty collections,
    // because that inference is exactly where "I checked" and "I found nothing
    // to check" get confused.
    if (canDelete && attached.Count == 0 && joinedTo.Count == 0) standalone.Add(id);
}

// EVERY DIMENSION IN THE MODEL THAT MEASURES TO WHAT GOES - VERSION 2.
//
// What goes: the elements given and every dependent Revit listed for them.
// A dimension that measures to one of a unit's own parts goes the same way
// as one that measures to the unit.
var goes = new HashSet<ElementId>();
foreach (var element in elements)
{
    if (element == null) continue;
    goes.Add(element.Id);
}
foreach (var listed in dependents.Values)
    foreach (var dependentId in listed) goes.Add(dependentId);

var dimensionsDeleted = new List<ElementId>();
var dimensionsCut = new List<ElementId>();
var dimensionLines = new List<string>();
var dimensionsUnread = new List<ElementId>();
var dimensionsRead = 0;

// No view handed to the collector, on purpose: a dimension in a section, an
// elevation or a 3D view nobody has open is cut or deleted all the same.
// `OfClass(typeof(Dimension))` also returns the 2025-and-later subclasses -
// LinearDimension, RadialDimension, ArcLengthDimension - and spot dimensions,
// which derive from it on every release.
var allDimensions = new List<Element>();
try { allDimensions.AddRange(new FilteredElementCollector(doc).OfClass(typeof(Dimension)).ToElements()); }
catch { }

foreach (var candidate in allDimensions)
{
    var dimension = candidate as Dimension;
    if (dimension == null) continue;
    dimensionsRead++;

    var referenceCount = 0;
    var onWhatGoes = new List<ElementId>();
    var readable = false;
    try
    {
        if (dimension.AreReferencesAvailable)
        {
            var references = dimension.References;
            if (references != null)
            {
                readable = true;
                foreach (Reference reference in references)
                {
                    if (reference == null) continue;
                    referenceCount++;
                    // A reference into a LINKED model names the link instance
                    // here; the link is never what goes, so it never matches.
                    var target = reference.ElementId;
                    if (target != null && goes.Contains(target)) onWhatGoes.Add(target);
                }
            }
        }
    }
    catch { readable = false; }

    if (!readable)
    {
        dimensionsUnread.Add(dimension.Id);
        continue;
    }
    if (onWhatGoes.Count == 0) continue;

    var shape = DimensionShape.Unknown;
    try { shape = dimension.DimensionShape; } catch { }

    // One Revit already lists, or one given, goes whatever its references say.
    var alreadyListed = goes.Contains(dimension.Id);
    var left = referenceCount - onWhatGoes.Count;
    var deleted = alreadyListed || shape != DimensionShape.Linear || left < 2;

    if (deleted) { if (!alreadyListed) dimensionsDeleted.Add(dimension.Id); }
    else dimensionsCut.Add(dimension.Id);

    var viewName = "(no view)";
    try
    {
        var owner = doc.GetElement(dimension.OwnerViewId) as View;
        if (owner != null) viewName = owner.Name;
    }
    catch { }

    var kind = "Dimensions";
    try { if (dimension.Category != null) kind = dimension.Category.Name; } catch { }

    var measured = new List<string>();
    foreach (var target in onWhatGoes)
    {
        var said = target.ToString();
        if (!measured.Contains(said)) measured.Add(said);
    }

    dimensionLines.Add(viewName + " | " + kind + " (" + shape + ") " + dimension.Id + " | "
        + (deleted
            ? (alreadyListed ? "DELETED - already among what goes" : "DELETED - Revit did not list it")
            : "CUT - stays, changed")
        + " | " + onWhatGoes.Count + " of its " + referenceCount + " reference(s) on what goes, " + left
        + " stay | measures " + string.Join(", ", measured));
}

// Every distinct element the delete is expected to take: what was given,
// Revit's dependents and the dimensions Revit did not list. A cut dimension
// is changed, not deleted, so it is not counted here. IT ALWAYS COUNTS THE
// ELEMENTS GIVEN, so it is never 0 for a selection - which is why the contract
// calls it accounting from version 3 (row 5b-2068): the number DELETE_ELEMENTS
// is handed, not the evidence that anything goes with them.
var goesTotal = goes.Count + dimensionsDeleted.Count;
