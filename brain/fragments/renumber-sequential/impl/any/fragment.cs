// NOT STANDALONE. Assumes `doc`, `elements`, `parameterName`, `startAt`,
// `prefix`, `padding` and `sortBy` are in scope, and leaves `numbered`,
// `refused`, `readOnly`, `absent` and `unverified` behind.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one - Golden Rule 16.
//
// THE ORDER IS THE WHOLE JOB.
//
// Numbering in whatever order a collector happened to return is the one outcome
// nobody wants: it LOOKS like work was done, and produces a drawing where 104
// sits next to 118. So the order is a request input, and `none` - the order I
// was handed - is a real answer for a caller who has already sorted them.
//
// x AND y ARE PROJECT AXES, NOT THE BUILDING'S. On a site rotated to true
// north, sorting by X walks diagonally across the rooms. That is a stated limit
// rather than a defect; sort by name, or hand them over already ordered.
//
// A DUPLICATE NUMBER IS REFUSED BY REVIT, and it is the normal way this job
// fails - renumbering from 101 while some room already holds 105. Each refusal
// is reported WITH ITS NUMBER so the clash is visible rather than inferred from
// a short count. Nothing is renumbered twice to dodge it: a silent second pass
// makes the final numbers depend on collision order.

var numbered = new Dictionary<ElementId, string>();
var refused = new Dictionary<ElementId, string>();
var readOnly = new List<ElementId>();
var absent = new List<ElementId>();
var unverified = new List<ElementId>();

Func<Element, XYZ> placeOf = element =>
{
    try
    {
        var point = element.Location as LocationPoint;
        if (point != null) return point.Point;
        var curve = element.Location as LocationCurve;
        if (curve != null && curve.Curve != null) return curve.Curve.Evaluate(0.5, true);
        var box = element.get_BoundingBox(null);
        if (box != null) return (box.Min + box.Max) * 0.5;
    }
    catch { }
    return null;
};

var ordered = new List<Element>();
foreach (var element in elements)
{
    if (element != null) ordered.Add(element);
}

string how = (sortBy ?? "").Trim().ToLowerInvariant();
if (how == "x" || how == "y")
{
    // An element with no position sorts last rather than throwing - it is still
    // renumbered, just at the end, which is better than losing it.
    ordered.Sort(delegate (Element a, Element b)
    {
        var pa = placeOf(a);
        var pb = placeOf(b);
        if (pa == null && pb == null) return 0;
        if (pa == null) return 1;
        if (pb == null) return -1;
        double va = how == "x" ? pa.X : pa.Y;
        double vb = how == "x" ? pb.X : pb.Y;
        return va.CompareTo(vb);
    });
}
else if (how == "name")
{
    ordered.Sort(delegate (Element a, Element b)
    {
        string na = "", nb = "";
        try { na = a.Name ?? ""; } catch { }
        try { nb = b.Name ?? ""; } catch { }
        return string.Compare(na, nb, StringComparison.OrdinalIgnoreCase);
    });
}
// "none" and anything unrecognised keep the order handed in. That is a real
// answer, not a fallback: the caller may have sorted them deliberately.

int next = startAt;

foreach (var element in ordered)
{
    var parameter = element.LookupParameter(parameterName);
    if (parameter == null) { absent.Add(element.Id); continue; }
    if (parameter.IsReadOnly || parameter.StorageType != StorageType.String)
    {
        readOnly.Add(element.Id);
        continue;
    }

    string value = (prefix ?? "") + next.ToString().PadLeft(padding > 0 ? padding : 1, '0');

    bool accepted = false;
    try { accepted = parameter.Set(value); }
    catch { refused[element.Id] = value; next++; continue; }

    if (!accepted) { refused[element.Id] = value; next++; continue; }

    // READ BACK. Revit refuses a duplicate room or sheet number by rejecting
    // the write, and a count of attempts would report those as done.
    string now = null;
    try { now = parameter.AsString(); } catch { }

    if (now == value) numbered[element.Id] = value;
    else if (now == null) unverified.Add(element.Id);
    else refused[element.Id] = value;

    // The number is consumed either way. Reusing it after a refusal would make
    // the sequence depend on which elements happened to clash.
    next++;
}
