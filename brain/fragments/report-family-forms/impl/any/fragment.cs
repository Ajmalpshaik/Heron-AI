// NOT STANDALONE. Assumes `doc` and `which` are in scope; leaves `formCount`,
// `formList`, `combinationList`, `scanned`, `notAFamily`, `elements`,
// `twoDCount`, `twoDList`, `refused` and `findings` behind.
//
// VERSION 5 (2026-11-11, row 5b-1496) - THE FAMILY'S 2D SIDE TOO, on the
// owner's word: no second tool for the same kind of job. Beside the forms,
// every DETAIL LINE, SYMBOLIC LINE, FILLED and MASKING REGION and NESTED
// FAMILY in the family is read into `twoDList`: its view or plane, ends in
// millimetres (an arc's centre and radius), line style or region type, a
// symbolic line's visibility settings, its Visible and what that is linked to,
// a nested family's type, point, rotation, mirror and parameter links - and
// every dimension and alignment that references it, in words: "locked along
// Head Top", "locked at an end to Tip", "labelled Length to ...", "EQ with".
// A filled region's sketch edges are its own (GetDependentElements), counted
// under it and never listed as loose lines. `which` takes the 2D kinds - "2d",
// "detail line", "symbolic line", "line", "filled region", "masking region",
// "region", "nested" - and their ids, mixed with forms' ids as one list. A call
// naming only form words reads no 2D, and one naming only 2D words reads no
// form, so every version 4 call answers its forms exactly as before.
//
// READS ONLY. Nothing is changed, and nothing is highlighted - SET_SELECTION
// does that with the `elements` this leaves, and DELETE_ELEMENTS deletes them.
//
// VERSION 4 (2026-11-01) took in SELECT_FAMILY_FORMS, which picked the same
// forms by the same ids or kinds and handed them on, and is DEPRECATED, merged
// into this card. What it did that this did not is here now: the forms read
// are handed on as `elements` - in the order their ids were named, each once
// - for SET_SELECTION, DELETE_ELEMENTS or any tool that takes elements; a call
// that reads nothing says why in `refused`; and "freeform" is a kind, as a
// FreeFormElement is a form. ALL OR NOTHING as that card was: one id that is
// not a form refuses the call and `elements` stays EMPTY, so a delete after it
// can never reach part of a list.
//
// NOTHING NAMED, NOTHING HANDED ON - the conflict, and the safer rule kept.
// SELECT_FAMILY_FORMS refused a call that named no form; this card reads
// every form when `which` is left out (row 5b-488). Both stand: left out,
// `which` binds the card's own "not named" - every form is READ, as before,
// and NONE is handed on, so a delete chained after it has nothing to delete.
// Typing "all" names every form and hands them all on.
//
// EVERY FORM IN THE FAMILY OPEN IN THE FAMILY EDITOR, with the id a later step
// names it by: what kind it is, solid or void, the plane its sketch is on, its
// extent and a solid's volume, its subcategory, where it shows, and the join or
// cut it belongs to. The question asked before locking, labelling, cutting or
// hiding anything - and the only way to name a form made by hand, which no form
// tool gave an id for.
//
// ONE LINE, NOT A LIST. A list is cut to three entries before anyone reads it,
// and the whole of this is the answer - the reason REPORT_FAMILY_PARAMETERS
// answers on one line too.
//
// `which` NARROWS IT: "all" or nothing for every form, or kinds and sides with
// commas between - "void", "extrusion", "solid, revolve". A form is kept when it
// matches every word given, so "void, extrusion" is the void extrusions.
//
// OR THE IDS THE FORM TOOLS GAVE BACK (version 3, row 5b-1312) - the long
// UniqueId each answers with and this lists, commas between - so one form's
// extents are read after a flex without the rest. Read as SELECT_FAMILY_FORMS
// reads them: a part that is not a kind or side is an id; ids and words are not
// mixed in one call; and one id that is not a form of this family refuses the
// whole call, each bad one named, before anything is read. The short number
// Select by ID shows is refused with that said - an ElementId built from typed
// text is the constructor that changed at 2024.

var findings = new List<string>();
var formCount = 0;
var formList = "";
// VERSION 5: the family's 2D side.
var twoDCount = 0;
var twoDList = "";
var combinationList = "";
var scanned = 0;
var notAFamily = false;
// VERSION 4, from SELECT_FAMILY_FORMS.
var elements = new List<Element>();
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

Func<GenericForm, string> kindOf = f => f is Extrusion ? "extrusion" : f is Revolution ? "revolve"
    : f is Blend ? "blend" : f is SweptBlend ? "swept blend" : f is Sweep ? "sweep"
    : f is FreeFormElement ? "freeform" : "form";

// The sketch a form stands on, by its plane's name - the work plane it was
// drawn on. Each kind keeps it under a different name.
Func<GenericForm, string> planeOf = f =>
{
    Sketch sketch = null;
    try
    {
        if (f is Extrusion) sketch = ((Extrusion)f).Sketch;
        else if (f is Revolution) sketch = ((Revolution)f).Sketch;
        else if (f is Blend) sketch = ((Blend)f).BottomSketch;
        else if (f is SweptBlend) sketch = ((SweptBlend)f).PathSketch;
        else if (f is Sweep) sketch = ((Sweep)f).PathSketch;
    }
    catch (Exception) { }
    var plane = sketch == null ? null : sketch.SketchPlane;
    return plane == null ? "(no sketch plane)" : "on " + plane.Name;
};

var known = new[] { "solid", "void", "extrusion", "revolve", "revolution", "blend", "sweep", "sweptblend",
                    "freeform" };
// LEFT OUT, `which` is the card's "not named": every form read, none handed
// on (version 4). Anything typed - "all" included - names what is handed on.
var notNamed = squash(which) == "notnamed";
var parts = (which ?? "").Split(',').Select(p => p.Trim())
    .Where(p => squash(p).Length > 0 && squash(p) != "all" && squash(p) != "notnamed").ToList();
// VERSION 5: the 2D kinds, squashed, and the rows each one keeps.
var twoDWords = new Dictionary<string, string[]>
{
    { "2d", new[] { "detail line", "symbolic line", "filled region", "masking region", "nested" } },
    { "detailline", new[] { "detail line" } }, { "detaillines", new[] { "detail line" } },
    { "symbolicline", new[] { "symbolic line" } }, { "symboliclines", new[] { "symbolic line" } },
    { "line", new[] { "detail line", "symbolic line" } }, { "lines", new[] { "detail line", "symbolic line" } },
    { "filledregion", new[] { "filled region" } }, { "filledregions", new[] { "filled region" } },
    { "maskingregion", new[] { "masking region" } }, { "maskingregions", new[] { "masking region" } },
    { "region", new[] { "filled region", "masking region" } }, { "regions", new[] { "filled region", "masking region" } },
    { "nested", new[] { "nested" } }, { "nestedfamily", new[] { "nested" } }, { "nestedfamilies", new[] { "nested" } },
};
var words = parts.Select(squash).Where(w => known.Contains(w)).ToList();
var words2D = parts.Select(squash).Where(w => twoDWords.ContainsKey(w)).ToList();
var ids = parts.Where(p => !known.Contains(squash(p)) && !twoDWords.ContainsKey(squash(p))).ToList();
var wanted2D = new HashSet<string>(words2D.SelectMany(w => twoDWords[w]));

// VERSION 5: the 2D kind of an element, or null when it is not one read here.
Func<Element, string> kind2D = e =>
{
    if (e is SymbolicCurve) return "symbolic line";
    if (e is DetailCurve) return "detail line";
    var asRegion = e as FilledRegion;
    if (asRegion != null) return asRegion.IsMasking ? "masking region" : "filled region";
    if (e is FamilyInstance) return "nested";
    return null;
};

var rows = new List<string>();
var joins = new List<string>();
var named = new HashSet<string>();
// The forms - and, since version 5, the 2D elements - named by id, in the
// order named, each once - the order `elements` hands them on in
// (SELECT_FAMILY_FORMS' rule).
var namedInOrder = new List<Element>();
var problems = new List<string>();

// VERSION 5: a filled region's sketch edges, found once - they are the
// region's own, never read or handed on as loose lines.
var edgesOf = new Dictionary<ElementId, List<ElementId>>();
var regionOfEdge = new Dictionary<ElementId, FilledRegion>();
var regions = new List<FilledRegion>();
var edgesUnread = 0;
if (doc.IsFamilyDocument)
{
    regions = new FilteredElementCollector(doc).OfClass(typeof(FilledRegion)).Cast<FilledRegion>().ToList();
    foreach (var region in regions)
    {
        var edges = new List<ElementId>();
        try
        {
            foreach (var id in region.GetDependentElements(new ElementClassFilter(typeof(CurveElement))))
                edges.Add(id);
        }
        catch (Exception) { edgesUnread++; }
        edgesOf[region.Id] = edges;
        foreach (var id in edges) regionOfEdge[id] = region;
    }
}
var named2D = false;

if (doc.IsFamilyDocument && words.Count == 0 && words2D.Count == 0)
{
    foreach (var id in ids)
    {
        Element element = null;
        try { element = doc.GetElement(id); }
        catch (Exception) { element = null; }

        var form = element as GenericForm;
        if (element == null)
            problems.Add("\"" + id + "\" is not an id in this family - "
                + (id.All(char.IsDigit)
                    ? "that is an element number; give the long id the form tools gave back"
                    : "nor a kind of form or a side (solid, void, extrusion, revolve, blend, sweep, swept blend, "
                      + "freeform) or of 2D (2d, detail line, symbolic line, line, filled region, masking region, "
                      + "region, nested)"));
        else if (form == null && kind2D(element) == null)
            problems.Add("\"" + id + "\" is "
                + (string.IsNullOrEmpty(element.Name) ? "an element" : "\"" + element.Name + "\"")
                + (element.Category != null ? " (" + element.Category.Name + ")" : "")
                + ", not a form, a line, a region or a nested family");
        else if (regionOfEdge.ContainsKey(element.Id))
            problems.Add("\"" + id + "\" is an edge of the filled region " + regionOfEdge[element.Id].UniqueId
                + " - name the region; its edges are read under it");
        else if (named.Add(element.UniqueId))
        {
            namedInOrder.Add(element);
            if (form == null) named2D = true;
        }
    }
}

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family's forms are "
        + "read inside the family - open it first.";
    findings.Add(refused);
}
else if ((words.Count > 0 || words2D.Count > 0) && ids.Count > 0)
{
    refused = "Nothing was read. Name forms by their ids OR by kind and side, not both in one call - \""
        + string.Join(", ", ids) + "\" read as ids beside \"" + string.Join(", ", words.Concat(words2D)) + "\". A "
        + "word that is neither is read as an id: name solid or void, and extrusion, revolve, blend, sweep, swept "
        + "blend or freeform; or 2d, detail line, symbolic line, line, filled region, masking region, region or "
        + "nested; or all.";
    findings.Add(refused);
}
else if (problems.Count > 0)
{
    refused = "Nothing was read. " + string.Join("; ", problems) + ".";
    findings.Add(refused);
}
else
{
    var forms = new FilteredElementCollector(doc).OfClass(typeof(GenericForm)).Cast<GenericForm>().ToList();
    scanned = forms.Count;
    foreach (var f in forms.OrderBy(x => kindOf(x)).ThenBy(x => x.UniqueId))
    {
        var kind = kindOf(f);
        var side = f.IsSolid ? "solid" : "void";
        var matches = words.All(w => w == side || w == squash(kind) || (w == "revolution" && kind == "revolve"));
        if (!matches || (ids.Count > 0 && !named.Contains(f.UniqueId))) continue;
        // VERSION 5: a call naming only 2D kinds reads no form.
        if (words.Count == 0 && words2D.Count > 0) continue;
        if (ids.Count == 0 && !notNamed) elements.Add(f);

        var extent = "";
        var box = f.get_BoundingBox(null);
        if (box != null)
            extent = "X " + mm(box.Min.X) + " to " + mm(box.Max.X) + ", Y " + mm(box.Min.Y) + " to " + mm(box.Max.Y)
                + ", Z " + mm(box.Min.Z) + " to " + mm(box.Max.Z) + " mm";

        var volume = 0.0;
        if (f.IsSolid)
        {
            var geometry = f.get_Geometry(new Options());
            if (geometry != null)
                foreach (GeometryObject piece in geometry)
                {
                    var body = piece as Solid;
                    if (body != null && body.Volume > 0) volume += body.Volume;
                }
        }

        var shows = "";
        var seen = f.GetVisibility();
        if (seen != null)
        {
            var levels = new[] { seen.IsShownInCoarse ? "coarse" : null, seen.IsShownInMedium ? "medium" : null,
                                 seen.IsShownInFine ? "fine" : null }.Where(s => s != null).ToList();
            var kinds = new[] { seen.IsShownInTopBottom ? "plan" : null, seen.IsShownInFrontBack ? "front/back" : null,
                                seen.IsShownInLeftRight ? "left/right" : null }.Where(s => s != null).ToList();
            shows = "shows in " + (levels.Count == 0 ? "no detail level" : string.Join(", ", levels)) + " | "
                + (kinds.Count == 0 ? "no plan or elevation" : string.Join(", ", kinds));
        }

        var inJoin = new List<string>();
        foreach (GeomCombination one in f.Combinations) inJoin.Add(one.UniqueId);

        rows.Add(side + " " + kind + " " + f.UniqueId + " (" + planeOf(f) + ")"
            + (extent.Length > 0 ? ": " + extent : "")
            + (f.IsSolid ? ", " + Math.Round(volume * 28.316846592, 3).ToString(invariant) + " L" : "")
            + ", subcategory " + (f.Subcategory == null ? "(the family's own)" : f.Subcategory.Name)
            + (shows.Length > 0 ? ", " + shows : "")
            + (inJoin.Count > 0 ? ", in " + string.Join(" and ", inJoin) : ""));
    }

    foreach (var c in new FilteredElementCollector(doc).OfClass(typeof(GeomCombination)).Cast<GeomCombination>())
    {
        var members = new List<string>();
        foreach (CombinableElement m in c.AllMembers)
        {
            var mf = m as GenericForm;
            members.Add(mf == null ? m.UniqueId : (mf.IsSolid ? "solid " : "void ") + kindOf(mf) + " " + m.UniqueId);
        }
        joins.Add(c.UniqueId + ": " + string.Join(", ", members));
    }

    // ---- VERSION 5: THE FAMILY'S 2D SIDE ----------------------------------------
    // Read when nothing narrowed the call, when 2D kinds were named, or when
    // ids were named (then only those). A call naming only form words reads
    // none, so it answers as version 4 did.
    var rows2D = new List<string>();
    var scanned2D = 0;
    var locks2D = 0;
    var dimensionsUnread = 0;
    var read2D = words2D.Count > 0 || (words.Count == 0 && (ids.Count == 0 || named2D));
    if (read2D)
    {
        Func<ElementId, string> nameOf = id =>
        {
            var e = doc.GetElement(id);
            if (e == null) return "an element no longer here";
            var plane = e as ReferencePlane;
            if (plane != null)
            {
                var datum = plane.get_Parameter(BuiltInParameter.DATUM_TEXT);
                var text = datum == null ? null : datum.AsString();
                return string.IsNullOrEmpty(text) ? "an unnamed reference plane " + e.UniqueId : text;
            }
            if (e is Level) return e.Name;
            var gf = e as GenericForm;
            if (gf != null) return kindOf(gf) + " " + e.UniqueId;
            return (kind2D(e) ?? (e.Category != null ? e.Category.Name : "element")) + " " + e.UniqueId;
        };

        // Every dimension and alignment, once, with the ids it references.
        var dimensions = new List<Tuple<Dimension, List<ElementId>>>();
        foreach (var d in new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>())
        {
            var refIds = new List<ElementId>();
            try { foreach (Reference r in d.References) if (r != null) refIds.Add(r.ElementId); }
            catch (Exception) { dimensionsUnread++; continue; }
            dimensions.Add(Tuple.Create(d, refIds));
        }

        // What holds an element (or a region by its edges), in words.
        Func<ElementId, Curve> curveOf = id =>
        {
            var asCurve = doc.GetElement(id) as CurveElement;
            return asCurve == null ? null : asCurve.GeometryCurve;
        };
        Func<IEnumerable<ElementId>, List<string>> holdsOf = ownIds =>
        {
            var own = new HashSet<ElementId>(ownIds);
            var said = new List<string>();
            foreach (var t in dimensions)
            {
                if (!t.Item2.Any(own.Contains)) continue;
                var d = t.Item1;
                var others = t.Item2.Where(i => !own.Contains(i)).Distinct().ToList();
                // The line - or the region's edge - this dimension holds.
                var curve = curveOf(t.Item2.First(own.Contains));
                var segments = 0;
                try { segments = d.NumberOfSegments; } catch (Exception) { segments = 0; }
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
                    // ALONG or AT AN END: a straight line in a plane running along
                    // it is locked along it; a plane crossing it holds an end.
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
                else if (segments > 1) said.Add("a dimension of " + segments + " segments to " + otherNames
                    + " (locks per segment not read)");
                else said.Add("dimensioned (not locked) to " + otherNames);
            }
            return said;
        };

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
            var asCurve = e as CurveElement;
            if (asCurve != null && asCurve.SketchPlane != null) return "on plane " + asCurve.SketchPlane.Name;
            var asNested = e as FamilyInstance;
            if (asNested != null && asNested.Host != null) return "on " + nameOf(asNested.Host.Id);
            return "no view or plane";
        };

        Func<XYZ, string> at = p => mm(p.X) + "," + mm(p.Y) + "," + mm(p.Z);

        // A REGION'S EDGES ARE ITS OWN, never listed as loose lines - found
        // once, before the ids were read.

        var candidates = new List<Element>();
        foreach (var c in new FilteredElementCollector(doc).OfClass(typeof(CurveElement)).Cast<CurveElement>())
            if ((c is DetailCurve || c is SymbolicCurve) && !regionOfEdge.ContainsKey(c.Id)) candidates.Add(c);
        candidates.AddRange(regions);
        candidates.AddRange(new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>());
        scanned2D = candidates.Count;

        foreach (var e in candidates.OrderBy(x => kind2D(x)).ThenBy(x => x.UniqueId))
        {
            var kind = kind2D(e);
            if (kind == null) continue;
            if (ids.Count > 0 && !named.Contains(e.UniqueId)) continue;
            if (wanted2D.Count > 0 && !wanted2D.Contains(kind)) continue;
            // Handed on only when named by a 2D kind (or by id, below): "all"
            // hands on forms alone, as version 4 did, so a delete chained after
            // "all" can never reach a line it did not before.
            if (ids.Count == 0 && !notNamed && words2D.Count > 0) elements.Add(e);

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
                        ? "arc from " + at(curve.GetEndPoint(0)) + " to " + at(curve.GetEndPoint(1)) + ", centre "
                          + at(arc.Center) + ", radius " + mm(arc.Radius) + " mm"
                        : "circle centre " + at(arc.Center) + ", radius " + mm(arc.Radius) + " mm");
                else if (curve != null && curve.IsBound)
                    bits.Add("from " + at(curve.GetEndPoint(0)) + " to " + at(curve.GetEndPoint(1)) + " mm, "
                        + mm(curve.Length) + " long");
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
                                             seen.IsShownInFine ? "fine" : null }.Where(x => x != null).ToList();
                        var views = new[] { seen.IsShownInTopBottom ? "plan" : null, seen.IsShownInFrontBack ? "front/back" : null,
                                            seen.IsShownInLeftRight ? "left/right" : null }.Where(x => x != null).ToList();
                        bits.Add("shows in " + (levels.Count == 0 ? "no detail level" : string.Join(", ", levels)) + " | "
                            + (views.Count == 0 ? "no plan or elevation" : string.Join(", ", views)));
                    }
                }
                holds = holdsOf(new[] { e.Id });
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
                holds = holdsOf(edges.Concat(new[] { region.Id }));
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
                if (links.Count > 0) bits.Add("linked " + string.Join("; ", links.OrderBy(x => x)));
                holds = holdsOf(new[] { e.Id });
            }

            var visibleText = visibleOf(e);
            if (visibleText.Length > 0) bits.Add(visibleText);
            locks2D += holds.Count(h => h.StartsWith("locked") || h.StartsWith("labelled") || h.StartsWith("EQ"));
            bits.Add(holds.Count == 0 ? "nothing holds it" : string.Join("; ", holds));
            rows2D.Add(string.Join(", ", bits));
        }
    }
    twoDCount = rows2D.Count;
    twoDList = string.Join("  ||  ", rows2D);
    if (read2D && edgesUnread > 0)
        findings.Add("Revit did not give the sketch edges of " + edgesUnread + " filled region(s): those regions read "
            + "\"0 sketch edge(s)\", their edges may be listed as loose lines, and locks on them are not read.");
    if (read2D && dimensionsUnread > 0)
        findings.Add(dimensionsUnread + " dimension(s) or alignment(s) could not be read, so a \"nothing holds it\" "
            + "above may be wrong.");
    if (read2D)
        findings.Add(twoDCount + " 2D element(s) of " + scanned2D + " lines, regions and nested families"
            + (words2D.Count > 0 ? " match \"" + string.Join(", ", words2D) + "\"" : ids.Count > 0 ? ", by id" : "")
            + "; " + locks2D + " lock(s), label(s) or EQ on them.");

    // BY ID, IN THE ORDER NAMED (version 4) - the order a caller listed them
    // in is the order they are handed on.
    if (ids.Count > 0) foreach (var f in namedInOrder) elements.Add(f);

    formCount = rows.Count;
    formList = string.Join("  ||  ", rows);
    combinationList = string.Join("  ||  ", joins);
    findings.Add(formCount + " form(s) of " + scanned + " in the family" + (words.Count > 0 ? " match \""
        + string.Join(", ", words) + "\"" : ids.Count > 0 ? ", by id" : "") + "; " + joins.Count + " join(s) or cut(s).");
    if (scanned == 0) findings.Add("This family has no forms yet - nothing has been modelled in it.");
    findings.Add(notNamed
        ? "No form was named, so none is handed on - every form was read. Name them by id, by kind and side, or "
          + "say all, to hand them to SET_SELECTION or DELETE_ELEMENTS."
        : elements.Count + (read2D ? " form(s) and 2D element(s)" : " form(s)") + " handed on as elements - "
          + "SET_SELECTION highlights them; DELETE_ELEMENTS deletes them once the modeller has said yes.");
}
