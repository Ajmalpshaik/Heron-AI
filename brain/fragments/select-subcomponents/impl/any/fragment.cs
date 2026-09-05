// NOT STANDALONE. Assumes `doc`, `parents` and `recursive` are in scope; leaves
// `elements`, `withoutSubComponents` and `findings` behind.
//
// A CATEGORY SWEEP NEVER FINDS THESE. A nested shared family instance is a real
// element with its own category and parameters, but a collector over the model
// returns its PARENT. So "count the sensors" comes back short by every sensor
// built into a piece of equipment, and the answer does not look wrong.
//
// NOT THE SAME AS HOSTING. A door in a wall is hosted - two elements, one
// recorded as the other's parent. A family built out of families is nested. A
// modeller says "inside" for both.
//
// EVERY ID IS VISITED ONCE. Nesting is followed as deep as somebody built it,
// and a cycle would otherwise loop forever.

var elements = new List<Element>();
var withoutSubComponents = 0;
var findings = new List<string>();

if (parents == null || parents.Count == 0)
{
    findings.Add("No parent elements were given - hand in the instances to open");
}
else
{
    var notInstances = 0;
    var visited = new List<ElementId>();
    var queue = new Queue<Element>();

    foreach (var parent in parents)
        if (parent != null) queue.Enqueue(parent);

    var deepest = 0;
    var depthOf = new Dictionary<string, int>();

    foreach (var parent in parents)
        if (parent != null) depthOf[parent.Id.ToString()] = 0;

    while (queue.Count > 0)
    {
        var current = queue.Dequeue();
        var currentDepth = depthOf.ContainsKey(current.Id.ToString()) ? depthOf[current.Id.ToString()] : 0;

        var instance = current as FamilyInstance;
        if (instance == null)
        {
            if (currentDepth == 0) notInstances++;
            continue;
        }

        ICollection<ElementId> childIds = null;
        try { childIds = instance.GetSubComponentIds(); }
        catch { childIds = null; }

        if (childIds == null || childIds.Count == 0)
        {
            if (currentDepth == 0) withoutSubComponents++;
            continue;
        }

        foreach (var childId in childIds)
        {
            var already = false;
            foreach (var seen in visited)
            {
                if (seen == childId) { already = true; break; }
            }
            if (already) continue;
            visited.Add(childId);

            var child = doc.GetElement(childId);
            if (child == null) continue;

            elements.Add(child);
            if (currentDepth + 1 > deepest) deepest = currentDepth + 1;

            if (recursive)
            {
                depthOf[childId.ToString()] = currentDepth + 1;
                queue.Enqueue(child);
            }
        }
    }

    findings.Add(string.Format("{0} nested element(s) found under {1} parent(s){2}. {3} of the "
        + "parents have no sub-components at all, which is the ordinary case",
        elements.Count,
        parents.Count,
        recursive ? string.Format(", followed {0} level(s) deep", deepest) : ", one level only",
        withoutSubComponents));

    if (notInstances > 0)
        findings.Add(string.Format("{0} of what was handed in are not family instances - a wall, a "
            + "duct or a system family cannot have sub-components, so they were counted rather than "
            + "skipped in silence", notInstances));

    if (elements.Count > 0)
        findings.Add("These are the elements a category sweep of the model does NOT return, because "
            + "a collector gives the parent instead. Any count taken without them is short");
}
