// NOT STANDALONE. Assumes `doc` and `elements` are in scope, and leaves
// `roots`, `assemblyParts`, `unitCount`, `rootsOutsideSet` and `cycles`
// behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// TWO WALKS, AND THE UPWARD ONE IS THE POINT.
//
// Walking DOWN from a host is easy and answers a question nobody asked: it
// assumes you already have the host. A category filter hands you the nested
// FAN, because a nested part can sit in a different category from the unit it
// belongs to - so the AHU is not in the set at all and the fan looks like a
// piece of equipment in its own right. Asking each element for its host and
// following that to the top is what puts the two back together.
//
// A CYCLE HANGS REVIT RATHER THAN THROWING.
//
// Nothing should be its own ancestor. A corrupt or half-copied family has
// managed it, and an unguarded loop on the host relationship simply never
// returns. Every id visited on the way up is remembered; a repeat stops the
// walk and names the element, which is a report rather than a hang.
//
// INSULATION IS A HOST RELATIONSHIP TOO.
//
// Duct insulation, duct lining and pipe insulation are separate elements
// belonging to a host, and they inflate a count exactly as a nested part does.
// They share one base type, so both directions of the walk cover all three
// kinds with a single branch.

var roots = new Dictionary<ElementId, ElementId>();
var assemblyParts = new Dictionary<ElementId, IList<ElementId>>();
var rootsOutsideSet = new List<ElementId>();
var cycles = new List<ElementId>();

var given = new HashSet<ElementId>();
foreach (var element in elements) if (element != null) given.Add(element.Id);

foreach (var element in elements)
{
    if (element == null) continue;

    // ---- up, to the outermost host ----
    var visited = new HashSet<ElementId>();
    Element current = element;
    bool looped = false;

    while (current != null)
    {
        if (!visited.Add(current.Id)) { looped = true; break; }

        var instance = current as FamilyInstance;
        if (instance != null)
        {
            Element host = null;
            try { host = instance.SuperComponent; } catch { }
            if (host != null) { current = host; continue; }
        }

        var wrap = current as InsulationLiningBase;
        if (wrap != null)
        {
            Element wrapped = null;
            try { wrapped = doc.GetElement(wrap.HostElementId); } catch { }
            if (wrapped != null) { current = wrapped; continue; }
        }

        break;
    }

    if (looped) { cycles.Add(element.Id); continue; }
    if (current == null) continue;

    roots[element.Id] = current.Id;

    // A root that is not in the set the caller filtered is the filter's blind
    // spot: it caught a nested part and missed the unit. Named once.
    if (!given.Contains(current.Id) && !rootsOutsideSet.Contains(current.Id))
        rootsOutsideSet.Add(current.Id);

    if (assemblyParts.ContainsKey(current.Id)) continue;

    // ---- down, to every part however deep ----
    var parts = new List<ElementId>();
    var seen = new HashSet<ElementId>();
    var pending = new Queue<Element>();
    seen.Add(current.Id);
    pending.Enqueue(current);

    while (pending.Count > 0)
    {
        var node = pending.Dequeue();

        var instance = node as FamilyInstance;
        if (instance != null)
        {
            ICollection<ElementId> children = null;
            try { children = instance.GetSubComponentIds(); } catch { }
            if (children != null)
            {
                foreach (var id in children)
                {
                    if (!seen.Add(id)) continue;
                    Element child = null;
                    try { child = doc.GetElement(id); } catch { }
                    if (child == null) continue;
                    parts.Add(id);
                    pending.Enqueue(child);
                }
            }
        }

        var wraps = new List<ElementId>();
        try
        {
            var insulation = InsulationLiningBase.GetInsulationIds(doc, node.Id);
            if (insulation != null) wraps.AddRange(insulation);
        }
        catch { }
        try
        {
            var lining = InsulationLiningBase.GetLiningIds(doc, node.Id);
            if (lining != null) wraps.AddRange(lining);
        }
        catch { }

        foreach (var id in wraps)
        {
            if (!seen.Add(id)) continue;
            Element wrap = null;
            try { wrap = doc.GetElement(id); } catch { }
            if (wrap == null) continue;
            parts.Add(id);
            pending.Enqueue(wrap);
        }
    }

    assemblyParts[current.Id] = parts;
}

// The number that disagrees with every schedule, and the reason this exists.
int unitCount = assemblyParts.Count;
