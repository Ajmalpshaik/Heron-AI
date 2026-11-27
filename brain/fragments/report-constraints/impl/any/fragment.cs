// NOT STANDALONE. Assumes `elements` and `doc` are in scope, and leaves
// `constrainedBy`, `lockedTo`, `unconstrained`, `constraintsScanned` and
// `unresolvedConstraints` behind.
//
// VERSION 2 (2026-11-24, D-64, row 5b-1909, work-note item WN-7 of issue
// #1005) - A CONSTRAINT THAT WILL NOT SAY WHAT IT HOLDS IS COUNTED. Version 1
// passed over a constraint whose references Revit would not give without a
// word, so an element it held could be listed in `unconstrained` - a failed
// read answered as a no (D-52). `unresolvedConstraints` carries the count:
// above 0, an element in `unconstrained` may be held by one of them. Every
// other output reads as version 1's.
//
// VERSION 3 (2026-11-26, row 5b-2071, issue #1478) - ONLY AN ELEMENT
// SOMETHING HOLDS IS A KEY. Versions 1 and 2 keyed `constrainedBy` and
// `lockedTo` by every element given, an empty list for a free one, so on
// Revit 2020 three free walls read "constrainedBy 3 entry(ies), lockedTo 3
// entry(ies)" - an echo of the input, never empty, and D-30's empty leg could
// not be shown. The lists are built as before and only the ones with anything
// in them are handed back; a free element is in `unconstrained`, as it was.
//
// READ ONLY. Opens no transaction and unlocks nothing - see the last note.
//
// A CONSTRAINT IS NOT A PROPERTY OF THE ELEMENT.
//
// It is a SEPARATE element holding references to two or more things. There is
// no `wall.Constraints` to read. The only way to find what is holding something
// is to walk EVERY constraint in the model and ask whether any of its
// references points back at it. That is an API fact, not an oversight, and it
// is why this is slow and why nothing surfaces a constraint by accident.
//
// The model is walked ONCE and an index built, rather than once per element.
// The naive shape of this - every constraint, for every element - is quadratic
// and unusable on a real model.
//
// THE FAR END IS THE USEFUL HALF. "This element is constrained" tells nobody
// anything. "This element is locked to THAT COLUMN" is the answer.
//
// IT NEVER UNLOCKS. Deleting a locked dimension deletes the DIMENSION - the
// annotation goes off the drawing along with the lock, which is almost never
// what "unlock it" means. That is a person's decision.

// The working index, one list per element given. Version 3 hands back only
// the lists with something in them - see the end.
var heldBy = new Dictionary<ElementId, IList<ElementId>>();
var heldTo = new Dictionary<ElementId, IList<ElementId>>();
var unconstrained = new List<ElementId>();
int constraintsScanned = 0;
// VERSION 2 (D-64): constraints whose references Revit would not give.
var unresolvedConstraints = 0;

var wanted = new HashSet<ElementId>();
foreach (var element in elements)
{
    if (element == null) continue;
    wanted.Add(element.Id);
    heldBy[element.Id] = new List<ElementId>();
    heldTo[element.Id] = new List<ElementId>();
}

// One pass over the constraints, building both directions at once.
foreach (var constraint in new FilteredElementCollector(doc)
             .OfCategory(BuiltInCategory.OST_Constraints)
             .WhereElementIsNotElementType()
             .ToElements())
{
    constraintsScanned++;

    // Both a locked dimension and an alignment are a Dimension underneath, so
    // one cast covers the pair.
    var dimension = constraint as Dimension;
    if (dimension == null) continue;

    ReferenceArray references = null;
    try { references = dimension.References; } catch { }
    if (references == null) { unresolvedConstraints++; continue; }

    var touched = new List<ElementId>();
    foreach (Reference reference in references)
    {
        if (reference == null) continue;
        try
        {
            var id = reference.ElementId;
            if (id != ElementId.InvalidElementId && !touched.Contains(id)) touched.Add(id);
        }
        catch { }
    }

    // Every element of interest on this constraint learns about the constraint
    // AND about the others on the far end of it.
    foreach (var id in touched)
    {
        if (!wanted.Contains(id)) continue;

        heldBy[id].Add(constraint.Id);
        foreach (var other in touched)
        {
            if (other == id) continue;
            if (!heldTo[id].Contains(other)) heldTo[id].Add(other);
        }
    }
}

// VERSION 3: a key only for an element something holds. An element on a
// constraint that touches nothing else has a `constrainedBy` entry and no
// `lockedTo` one - each is handed back on its own count.
var constrainedBy = new Dictionary<ElementId, IList<ElementId>>();
var lockedTo = new Dictionary<ElementId, IList<ElementId>>();
foreach (var id in wanted)
{
    if (heldBy[id].Count == 0) unconstrained.Add(id);
    else constrainedBy[id] = heldBy[id];
    if (heldTo[id].Count > 0) lockedTo[id] = heldTo[id];
}
