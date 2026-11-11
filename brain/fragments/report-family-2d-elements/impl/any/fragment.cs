// NOT STANDALONE. Assumes `doc` and `which` are in scope; leaves `elementCount`,
// `elementList`, `scanned`, `notAFamily`, `refused` and `findings` behind.
//
// READS ONLY. Nothing is changed and nothing is selected.
//
// THE 2D SIDE OF THE FAMILY OPEN IN THE FAMILY EDITOR, element by element -
// the part REPORT_FAMILY_FORMS (forms) and REPORT_FAMILY_LABELS (labels) do
// not read:
//   - DETAIL LINES (a 2D family's lines, in its view) and SYMBOLIC LINES (a 3D
//     family's plan or elevation symbol, on a plane) - kind, view or plane,
//     ends in millimetres (an arc's centre and radius), line style, and for a
//     symbolic line its visibility settings;
//   - FILLED and MASKING regions - view, type, extent, how many edges its
//     sketch has, and the locks on those edges;
//   - NESTED FAMILIES - a detail item or annotation in its OWNER VIEW, or a
//     model family on its host plane - type, point, rotation, mirrored, and
//     which of its instance and type parameters are linked to this family's.
// FOR EVERY ONE: the dimensions and alignments that reference it - "locked
// along Head Top", "locked at an end to Tip", "labelled Length to ..." - and
// what its Visible parameter is associated with. The question asked before
// locking, labelling, hiding or linking 2D, and after it, to see it took.
//
// A FILLED REGION'S EDGES ARE ITS OWN, never listed as loose lines: its
// sketch's curves are read with GetDependentElements and counted under it.
//
// ONE LINE, NOT A LIST, rows "  ||  " apart - a list is cut to three entries
// before anyone reads it (REPORT_FAMILY_FORMS' reason).
//
// `which` NARROWS IT: left out or "all" for everything; kinds with commas
// between - "detail line", "symbolic line", "line" (both), "filled region",
// "masking region", "region" (both), "nested"; or the long UniqueIds the 2D
// tools gave back, commas between. Ids and kinds are not mixed, and one id
// that is not a 2D element of this family refuses the call before anything is
// read, each bad one named.

var findings = new List<string>();
var elementCount = 0;
var elementList = "";
var scanned = 0;
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
Func<XYZ, string> at = p => mm(p.X) + "," + mm(p.Y) + "," + mm(p.Z);
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// The kinds a call may name, squashed, and the rows each one keeps.
var kindWords = new Dictionary<string, string[]>
{
    { "detailline", new[] { "detail line" } }, { "detaillines", new[] { "detail line" } },
    { "symbolicline", new[] { "symbolic line" } }, { "symboliclines", new[] { "symbolic line" } },
    { "line", new[] { "detail line", "symbolic line" } }, { "lines", new[] { "detail line", "symbolic line" } },
    { "filledregion", new[] { "filled region" } }, { "filledregions", new[] { "filled region" } },
    { "maskingregion", new[] { "masking region" } }, { "maskingregions", new[] { "masking region" } },
    { "region", new[] { "filled region", "masking region" } }, { "regions", new[] { "filled region", "masking region" } },
    { "nested", new[] { "nested" } }, { "nestedfamily", new[] { "nested" } }, { "nestedfamilies", new[] { "nested" } },
};
var parts = (which ?? "").Split(',').Select(p => p.Trim())
    .Where(p => squash(p).Length > 0 && squash(p) != "all" && squash(p) != "notnamed").ToList();
var words = parts.Where(p => kindWords.ContainsKey(squash(p))).Select(squash).ToList();
var ids = parts.Where(p => !kindWords.ContainsKey(squash(p))).ToList();
var wantedKinds = new HashSet<string>(words.SelectMany(w => kindWords[w]));

// The kind of a 2D element, or null when it is not one this card reads.
Func<Element, string> kindOf = e =>
{
    if (e is SymbolicCurve) return "symbolic line";
    if (e is DetailCurve) return "detail line";
    var region = e as FilledRegion;
    if (region != null) return region.IsMasking ? "masking region" : "filled region";
    if (e is FamilyInstance) return "nested";
    return null;
};

var problems = new List<string>();
var namedIds = new HashSet<ElementId>();
if (doc.IsFamilyDocument && words.Count == 0)
    foreach (var id in ids)
    {
        Element element = null;
        try { element = doc.GetElement(id); }
        catch (Exception) { element = null; }
        if (element == null)
            problems.Add("\"" + id + "\" is not an id in this family - " + (id.All(char.IsDigit)
                ? "that is an element number; give the long id the 2D tools gave back"
                : "nor a kind (detail line, symbolic line, line, filled region, masking region, region, nested)"));
        else if (kindOf(element) == null)
            problems.Add("\"" + id + "\" is " + (string.IsNullOrEmpty(element.Name) ? "an element" : "\"" + element.Name + "\"")
                + (element.Category != null ? " (" + element.Category.Name + ")" : "") + ", not a line, region or nested family");
        else namedIds.Add(element.Id);
    }

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family's 2D is read "
        + "inside the family - open it first.";
}
else if (words.Count > 0 && ids.Count > 0)
    refused = "Nothing was read. Name 2D elements by their ids OR by kind, not both in one call - \""
        + string.Join(", ", ids) + "\" read as ids beside \"" + string.Join(", ", words) + "\".";
else if (problems.Count > 0)
    refused = "Nothing was read. " + string.Join("; ", problems) + ".";

if (refused != null) findings.Add(refused);
else
{
    // ---- NAMES of what a dimension reaches ------------------------------------
    Func<ElementId, string> nameOf = id =>
    {
        var e = doc.GetElement(id);
        if (e == null) return "an element no longer here";
        var plane = e as ReferencePlane;
        if (plane != null)
        {
            var named = plane.get_Parameter(BuiltInParameter.DATUM_TEXT);
            var text = named == null ? null : named.AsString();
            return string.IsNullOrEmpty(text) ? "an unnamed reference plane " + e.UniqueId : text;
        }
        if (e is Level) return e.Name;
        var kind = kindOf(e);
        return (kind ?? (e.Category != null ? e.Category.Name : "element")) + " " + e.UniqueId;
    };

    // ---- EVERY DIMENSION AND ALIGNMENT, once, by what it references -------------
    var dimensions = new List<Tuple<Dimension, List<ElementId>>>();
    foreach (var d in new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>())
    {
        var refIds = new List<ElementId>();
        try { foreach (Reference r in d.References) if (r != null) refIds.Add(r.ElementId); }
        catch (Exception) { continue; }
        dimensions.Add(Tuple.Create(d, refIds));
    }

    // How a dimension holds the element `self` (or any of `ownIds`, for a
    // region's edges), in words: the other ends by name.
    Func<IEnumerable<ElementId>, Curve, List<string>> holdsOf = (ownIds, curve) =>
    {
        var own = new HashSet<ElementId>(ownIds);
        var said = new List<string>();
        foreach (var t in dimensions)
        {
            if (!t.Item2.Any(own.Contains)) continue;
            var d = t.Item1;
            var others = t.Item2.Where(i => !own.Contains(i)).Distinct().ToList();
            var otherNames = others.Count == 0 ? "itself" : string.Join(", ", others.Select(nameOf));
            string label = null;
            try { label = d.FamilyLabel == null ? null : d.FamilyLabel.Definition.Name; } catch (Exception) { label = null; }
            var locked = false;
            try { locked = d.NumberOfSegments == 0 && d.IsLocked; } catch (Exception) { locked = false; }
            var equal = false;
            try { equal = d.NumberOfSegments > 1 && d.AreSegmentsEqual; } catch (Exception) { equal = false; }

            if (label != null) said.Add("labelled " + label + " to " + otherNames);
            else if (equal) said.Add("EQ with " + otherNames);
            else if (locked)
            {
                // ALONG or AT AN END: a straight line lying in a plane that
                // runs along it is locked along it; a plane crossing it holds
                // an end.
                var how = "locked to ";
                var line = curve as Line;
                if (line != null && others.Count == 1)
                {
                    var plane = doc.GetElement(others[0]) as ReferencePlane;
                    if (plane != null)
                        how = Math.Abs(plane.Normal.DotProduct(line.Direction)) < 1e-6 ? "locked along " : "locked at an end to ";
                }
                said.Add(how + otherNames);
            }
            else said.Add("dimensioned (not locked) to " + otherNames);
        }
        return said;
    };

    // Visible, and what it is linked to.
    Func<Element, string> visibleOf = e =>
    {
        var visible = e.get_Parameter(BuiltInParameter.IS_VISIBLE_PARAM);
        if (visible == null) return "";
        FamilyParameter linked = null;
        try { linked = doc.FamilyManager.GetAssociatedFamilyParameter(visible); } catch (Exception) { linked = null; }
        return "Visible " + (visible.AsInteger() == 1 ? "yes" : "no")
            + (linked != null ? " (linked to " + linked.Definition.Name + ")" : "");
    };

    Func<Element, string> whereOf = e =>
    {
        if (e.OwnerViewId != ElementId.InvalidElementId)
        {
            var view = doc.GetElement(e.OwnerViewId) as View;
            return "in view " + (view == null ? "(gone)" : view.Name);
        }
        var curveElement = e as CurveElement;
        if (curveElement != null && curveElement.SketchPlane != null) return "on plane " + curveElement.SketchPlane.Name;
        var nested = e as FamilyInstance;
        if (nested != null && nested.Host != null) return "on " + (nested.Host is ReferencePlane ? nameOf(nested.Host.Id) : nested.Host.Name);
        return "no view or plane";
    };

    // ---- A REGION'S EDGES are its own -----------------------------------------
    var regions = new FilteredElementCollector(doc).OfClass(typeof(FilledRegion)).Cast<FilledRegion>().ToList();
    var edgesOf = new Dictionary<ElementId, List<ElementId>>();
    var edgeIds = new HashSet<ElementId>();
    foreach (var region in regions)
    {
        var edges = new List<ElementId>();
        try
        {
            foreach (var id in region.GetDependentElements(new ElementClassFilter(typeof(CurveElement))))
                edges.Add(id);
        }
        catch (Exception) { }
        edgesOf[region.Id] = edges;
        foreach (var id in edges) edgeIds.Add(id);
    }

    var candidates = new List<Element>();
    foreach (var c in new FilteredElementCollector(doc).OfClass(typeof(CurveElement)).Cast<CurveElement>())
        if ((c is DetailCurve || c is SymbolicCurve) && !edgeIds.Contains(c.Id)) candidates.Add(c);
    candidates.AddRange(regions);
    candidates.AddRange(new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>());
    scanned = candidates.Count;

    var rows = new List<string>();
    var lockCount = 0;
    foreach (var e in candidates.OrderBy(x => kindOf(x)).ThenBy(x => x.UniqueId))
    {
        var kind = kindOf(e);
        if (kind == null) continue;
        if (ids.Count > 0 && !namedIds.Contains(e.Id)) continue;
        if (wantedKinds.Count > 0 && !wantedKinds.Contains(kind)) continue;

        var bits = new List<string> { kind + " " + e.UniqueId + " (" + whereOf(e) + ")" };
        List<string> holds;

        var curveElement = e as CurveElement;
        var region = e as FilledRegion;
        var nested = e as FamilyInstance;
        if (curveElement != null)
        {
            var curve = curveElement.GeometryCurve;
            var arc = curve as Arc;
            if (arc != null)
                bits.Add(curve.IsBound
                    ? "arc from " + at(curve.GetEndPoint(0)) + " to " + at(curve.GetEndPoint(1)) + ", centre " + at(arc.Center) + ", radius " + mm(arc.Radius) + " mm"
                    : "circle centre " + at(arc.Center) + ", radius " + mm(arc.Radius) + " mm");
            else if (curve != null && curve.IsBound)
                bits.Add("from " + at(curve.GetEndPoint(0)) + " to " + at(curve.GetEndPoint(1)) + " mm, " + mm(curve.Length) + " long");
            else if (curve != null) bits.Add("a closed " + curve.GetType().Name.ToLowerInvariant());
            var style = curveElement.LineStyle as GraphicsStyle;
            bits.Add("style " + (style == null || style.GraphicsStyleCategory == null ? "(none)" : style.GraphicsStyleCategory.Name));
            var symbolic = e as SymbolicCurve;
            if (symbolic != null)
            {
                FamilyElementVisibility seen = null;
                try { seen = symbolic.GetVisibility(); } catch (Exception) { seen = null; }
                if (seen != null)
                {
                    var levels = new[] { seen.IsShownInCoarse ? "coarse" : null, seen.IsShownInMedium ? "medium" : null,
                                         seen.IsShownInFine ? "fine" : null }.Where(s => s != null).ToList();
                    var views = new[] { seen.IsShownInTopBottom ? "plan" : null, seen.IsShownInFrontBack ? "front/back" : null,
                                        seen.IsShownInLeftRight ? "left/right" : null }.Where(s => s != null).ToList();
                    bits.Add("shows in " + (levels.Count == 0 ? "no detail level" : string.Join(", ", levels)) + " | "
                        + (views.Count == 0 ? "no plan or elevation" : string.Join(", ", views)));
                }
            }
            holds = holdsOf(new[] { e.Id }, curve);
        }
        else if (region != null)
        {
            var type = doc.GetElement(region.GetTypeId());
            bits.Add("type " + (type == null ? "(none)" : type.Name));
            double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
            var loops = 0;
            try
            {
                foreach (var loop in region.GetBoundaries())
                {
                    loops++;
                    foreach (var c in loop)
                        foreach (var p in new[] { c.GetEndPoint(0), c.GetEndPoint(1) })
                        {
                            minX = Math.Min(minX, p.X); minY = Math.Min(minY, p.Y);
                            maxX = Math.Max(maxX, p.X); maxY = Math.Max(maxY, p.Y);
                        }
                }
            }
            catch (Exception) { }
            if (loops > 0)
                bits.Add(loops + " loop(s), X " + mm(minX) + " to " + mm(maxX) + ", Y " + mm(minY) + " to " + mm(maxY) + " mm");
            var edges = edgesOf.ContainsKey(region.Id) ? edgesOf[region.Id] : new List<ElementId>();
            bits.Add(edges.Count + " sketch edge(s)");
            holds = holdsOf(edges.Concat(new[] { region.Id }), null);
        }
        else
        {
            var symbol = nested.Symbol;
            bits.Add((symbol == null ? "(no type)" : (symbol.Family == null ? "" : symbol.Family.Name + " : ") + symbol.Name)
                + (nested.Category != null ? " (" + nested.Category.Name + ")" : ""));
            var point = nested.Location as LocationPoint;
            if (point != null)
            {
                var turned = 0.0;
                try { turned = point.Rotation * 180.0 / Math.PI; } catch (Exception) { turned = 0.0; }
                bits.Add("at " + at(point.Point) + " mm, turned " + Math.Round(turned, 2).ToString(invariant) + " deg");
            }
            if (nested.Mirrored) bits.Add("mirrored");
            var links = new List<string>();
            foreach (Parameter p in nested.Parameters)
            {
                FamilyParameter linked = null;
                try { linked = doc.FamilyManager.GetAssociatedFamilyParameter(p); } catch (Exception) { linked = null; }
                if (linked != null) links.Add(p.Definition.Name + "=" + linked.Definition.Name);
            }
            if (symbol != null)
                foreach (Parameter p in symbol.Parameters)
                {
                    FamilyParameter linked = null;
                    try { linked = doc.FamilyManager.GetAssociatedFamilyParameter(p); } catch (Exception) { linked = null; }
                    if (linked != null) links.Add("type " + p.Definition.Name + "=" + linked.Definition.Name);
                }
            if (links.Count > 0) bits.Add("linked " + string.Join("; ", links.OrderBy(s => s)));
            holds = holdsOf(new[] { e.Id }, null);
        }

        var visible = visibleOf(e);
        if (visible.Length > 0) bits.Add(visible);
        lockCount += holds.Count(h => h.StartsWith("locked") || h.StartsWith("labelled") || h.StartsWith("EQ"));
        bits.Add(holds.Count == 0 ? "nothing holds it" : string.Join("; ", holds));
        rows.Add(string.Join(", ", bits));
    }

    elementCount = rows.Count;
    elementList = string.Join("  ||  ", rows);
    findings.Add(elementCount + " 2D element(s) of " + scanned + " lines, regions and nested families in the family"
        + (words.Count > 0 ? " match \"" + string.Join(", ", words) + "\"" : ids.Count > 0 ? ", by id" : "")
        + "; " + lockCount + " lock(s), label(s) or EQ on them.");
    if (scanned == 0) findings.Add("This family has no detail lines, symbolic lines, regions or nested families yet.");
}
