// NOT STANDALONE. Assumes `elements` and `doc` are in scope, and leaves
// `roots`, `partsOf`, `rootOf` and `cyclic` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A COUNT OF NESTED EQUIPMENT IS NOT A COUNT OF EQUIPMENT.
//
// An AHU with a nested fan, coil and filter is FOUR family instances in one
// category. Every schedule counting instances returns four. There is ONE unit
// on site. The answer is wrong in the direction that looks plausible, which is
// the direction nobody checks.
//
// TWO RELATIONSHIPS, NOT ONE.
//
//   FamilyInstance.SuperComponent      the family this instance is nested in
//   InsulationLiningBase.HostElementId the duct or pipe a wrap belongs to
//
// The second is the same problem in different clothes: insulation and lining
// are separate ELEMENTS belonging to a host, so a count of insulated ductwork
// is double the ductwork unless the walk follows them too.
//
// THE CLIMB IS GUARDED AGAINST A CYCLE.
//
// Nothing should ever be its own ancestor. A corrupt or half-copied family has
// produced one, and an unguarded while-loop hangs Revit rather than failing -
// worse than a wrong number, because it costs the session. Every id visited is
// remembered; a repeat stops the climb and the element is reported in `cyclic`
// rather than silently attached to whichever root it reached last.
//
// BOTH NUMBERS ARE REPORTED.
//
// `roots` is the honest count; the caller still has elements.Count. Returning
// only the corrected figure leaves nobody able to see WHY the schedule
// disagrees - and the schedule is the thing that gets issued.

var roots = new List<ElementId>();
var partsOf = new Dictionary<ElementId, IList<ElementId>>();
var rootOf = new Dictionary<ElementId, ElementId>();
var cyclic = new List<ElementId>();

// One step up, by whichever relationship applies. Null means this element is
// already a root.
Func<Element, Element> parentOf = element =>
{
    var instance = element as FamilyInstance;
    if (instance != null)
    {
        try { if (instance.SuperComponent != null) return instance.SuperComponent; }
        catch { }
    }

    var wrap = element as InsulationLiningBase;
    if (wrap != null)
    {
        try
        {
            var hostId = wrap.HostElementId;
            if (hostId != ElementId.InvalidElementId) return doc.GetElement(hostId);
        }
        catch { }
    }

    return null;
};

foreach (var element in elements)
{
    if (element == null) continue;

    var seen = new HashSet<ElementId>();
    var current = element;
    bool looped = false;

    while (true)
    {
        if (!seen.Add(current.Id)) { looped = true; break; }

        var parent = parentOf(current);
        if (parent == null) break;
        current = parent;
    }

    if (looped) { cyclic.Add(element.Id); continue; }

    var rootId = current.Id;
    rootOf[element.Id] = rootId;

    if (!partsOf.ContainsKey(rootId))
    {
        partsOf[rootId] = new List<ElementId>();
        roots.Add(rootId);
    }

    // A root reached from itself is not one of its own parts. Everything else
    // that climbed to it is.
    if (rootId != element.Id) partsOf[rootId].Add(element.Id);
}
