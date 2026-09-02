// NOT STANDALONE. Assumes `elements` and `doc` are in scope, and leaves
// `constrainedBy`, `lockedTo`, `unconstrained` and `constraintsScanned` behind.
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

var constrainedBy = new Dictionary<ElementId, IList<ElementId>>();
var lockedTo = new Dictionary<ElementId, IList<ElementId>>();
var unconstrained = new List<ElementId>();
int constraintsScanned = 0;

var wanted = new HashSet<ElementId>();
foreach (var element in elements)
{
    if (element == null) continue;
    wanted.Add(element.Id);
    constrainedBy[element.Id] = new List<ElementId>();
    lockedTo[element.Id] = new List<ElementId>();
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
    if (references == null) continue;

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

        constrainedBy[id].Add(constraint.Id);
        foreach (var other in touched)
        {
            if (other == id) continue;
            if (!lockedTo[id].Contains(other)) lockedTo[id].Add(other);
        }
    }
}

foreach (var id in wanted)
{
    if (constrainedBy[id].Count == 0) unconstrained.Add(id);
}
