// NOT STANDALONE. Assumes `doc`, `elements`, `host`, `curtainSystemType` and
// `faces` are in scope; leaves `created`, `facesTaken`, `facesAsked`,
// `readBack`, `alreadyNear`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one. One call
// is one undo entry.
//
// CURTAIN SYSTEM BY FACE - a curtain system of a NAMED type on the faces of a
// mass or a generic model placed in the project, through
// Document.Create.NewCurtainSystem2(ReferenceArray, CurtainSystemType), called
// once per host so each curtain system made is known by the host it sits on.
//
//   host               left out, or "selected": the masses and generic models
//                      in `elements` - the selection, or what a select step
//                      handed on. Otherwise ONE of them, by its element id as
//                      Revit prints it, its unique id, its family name, its
//                      "Family : Type" or its Mark
//   curtainSystemType  a curtain system type by name - never chosen here
//   faces              vertical (the default), top or all
//
// EVERY REFUSAL IS MADE BEFORE THE FIRST WRITE: no host, a host that is not a
// mass or a generic model, a type the model does not have, a face word this
// does not know, a host with no face Revit can reference, or no face of the
// kind asked. One host refused refuses the call - nothing is half made.
//
// THE FACES' REFERENCES COME FROM THE FAMILY'S OWN GEOMETRY.
// GetSymbolGeometry() with no transform is what the API reference calls
// "suitable for creating new Revit elements referencing the original
// element"; a copy from GetInstanceGeometry is not. The instance's transform
// turns each NORMAL into the model's directions and touches nothing else.
//
// READ BACK, MEASURED: each curtain system Revit made, its type, and on each of
// its curtain grids - one per face, the API reference says - the U and V grid
// lines, the panels and the mullions. A type that does not read back as named,
// or no curtain system at all, THROWS, and the host rolls the whole call back.

const double SquareFeetToSquareMetres = 0.09290304;   // (304.8 / 1000) squared
const double OneMillimetre = 1.0 / 304.8;             // D-20: feet
// Level to within 1 degree: |normal.Z| at most sin 1 degree. Straight up (or
// down) to within 1 degree: |normal.Z| at least cos 1 degree.
var levelWithin = Math.Sin(Math.PI / 180.0);
var upWithin = Math.Cos(Math.PI / 180.0);

var created = new List<Element>();
var facesTaken = 0;
var facesAsked = 0;
var readBack = "";
var alreadyNear = 0;
string refused = null;
var findings = new List<string>();

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var problems = new List<string>();

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
Func<string, string, bool> sameText = (a, b) =>
    string.Equals((a ?? "").Trim(), (b ?? "").Trim(), StringComparison.OrdinalIgnoreCase);

// ---------------------------------------------------------------------------
// WHICH FACES - vertical when nothing is said, the card's one default
// ---------------------------------------------------------------------------

var facesSaid = squash(faces);
if (facesSaid.EndsWith("faces")) facesSaid = facesSaid.Substring(0, facesSaid.Length - 5);
else if (facesSaid.EndsWith("face")) facesSaid = facesSaid.Substring(0, facesSaid.Length - 4);
string facesKind = null;
if (facesSaid == "" || facesSaid == "vertical" || facesSaid == "side" || facesSaid == "sides") facesKind = "vertical";
else if (facesSaid == "top") facesKind = "top";
else if (facesSaid == "all" || facesSaid == "every") facesKind = "all";
else
    problems.Add("\"" + (faces ?? "").Trim() + "\" is not a kind of face this knows - say vertical (the upright side "
        + "faces, the default), top, or all (every face, the underside included).");

// ---------------------------------------------------------------------------
// THE CURTAIN SYSTEM TYPE, BY NAME - never chosen here
// ---------------------------------------------------------------------------

var typeText = (curtainSystemType ?? "").Trim();
var systemTypes = new FilteredElementCollector(doc).WhereElementIsElementType()
    .Where(e => e is CurtainSystemType).Cast<CurtainSystemType>().ToList();
Func<ElementType, string> familyOf = t =>
{
    try { return t.FamilyName; } catch (Exception) { return null; }
};
Func<ElementType, string> fullName = t =>
    string.IsNullOrEmpty(familyOf(t)) ? t.Name : familyOf(t) + " : " + t.Name;
Func<ElementType, string, bool> typeFits = (t, text) =>
{
    if (sameText(t.Name, text)) return true;
    var colon = text.IndexOf(':');
    return colon >= 0 && sameText(familyOf(t), text.Substring(0, colon)) && sameText(t.Name, text.Substring(colon + 1));
};

CurtainSystemType chosenType = null;
var typesKnown = string.Join(", ", systemTypes.Select(t => "\"" + fullName(t) + "\"")
    .OrderBy(s => s, StringComparer.OrdinalIgnoreCase).Take(30).ToArray());
if (systemTypes.Count == 0)
    problems.Add("This model has no curtain system type at all, so there is nothing to make one of.");
else if (typeText.Length == 0)
    problems.Add("No curtain system type was named, and this never picks one. This model has " + typesKnown + ".");
else
{
    var exact = systemTypes.Where(t => string.Equals(t.Name, typeText, StringComparison.Ordinal)).ToList();
    var fits = exact.Count > 0 ? exact : systemTypes.Where(t => typeFits(t, typeText)).ToList();
    if (fits.Count == 1) chosenType = fits[0];
    else if (fits.Count == 0)
        problems.Add("No curtain system type called \"" + typeText + "\" in this model. It has " + typesKnown + ".");
    else
        problems.Add("\"" + typeText + "\" names " + fits.Count + " curtain system types - say which: "
            + string.Join(", or ", fits.Select(t => "\"" + fullName(t) + "\"").ToArray()) + ".");
}

// ---------------------------------------------------------------------------
// THE HOSTS - the selection, or one named
// ---------------------------------------------------------------------------

var massCategory = new ElementId(BuiltInCategory.OST_Mass);
var genericCategory = new ElementId(BuiltInCategory.OST_GenericModel);
Func<Element, bool> canHost = e =>
    e != null && e.Category != null && (e.Category.Id == massCategory || e.Category.Id == genericCategory);
Func<Element, ElementType> typeOf = e => doc.GetElement(e.GetTypeId()) as ElementType;
Func<Element, string> markOf = e =>
{
    var p = e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
    var mark = p == null ? null : p.AsString();
    return string.IsNullOrEmpty(mark) ? null : mark;
};
Func<Element, string> describe = e =>
{
    var type = typeOf(e);
    return (e.Category != null ? e.Category.Name : "Element") + " " + e.Id + " \""
        + (type != null ? fullName(type) : e.Name) + "\"" + (markOf(e) != null ? ", Mark \"" + markOf(e) + "\"" : "");
};
// A name fits an element by its family, its type, "Family : Type" or its Mark.
Func<Element, string, bool> nameFits = (e, text) =>
{
    if (sameText(markOf(e), text)) return true;
    var type = typeOf(e);
    return type != null && (sameText(familyOf(type), text) || typeFits(type, text));
};
Func<Element, string> notAHost = e =>
    describe(e) + " is not a mass or a generic model - Revit puts a curtain system on their faces only. A curtain "
    + "WALL drawn along a line is CREATE_WALL's.";

var hostText = (host ?? "").Trim();
var bySelection = hostText.Length == 0 || sameText(hostText, "selected") || sameText(hostText, "selection")
    || sameText(hostText, "the selected one");
var hosts = new List<Element>();
if (bySelection)
{
    var seen = new HashSet<string>();
    var others = 0;
    foreach (var e in elements ?? new List<Element>())
    {
        if (e == null || !seen.Add(e.Id.ToString())) continue;
        if (canHost(e)) hosts.Add(e);
        else { others++; if (others <= 5) problems.Add(notAHost(e)); }
    }
    if (others > 5) problems.Add((others - 5) + " more selected element(s) are not masses or generic models either.");
    if (seen.Count == 0)
        problems.Add("Nothing is selected and no host was named - select the mass in Revit, or name it: its element "
            + "id, its family name or its Mark.");
}
else
{
    Element named = null;
    try { named = doc.GetElement(hostText); } catch (Exception) { named = null; }   // a unique id
    if (named == null && hostText.All(char.IsDigit))
        foreach (var id in new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElementIds())
            if (id.ToString() == hostText) { named = doc.GetElement(id); break; }
    if (named != null && !(named is ElementType))
    {
        if (canHost(named)) hosts.Add(named);
        else problems.Add(notAHost(named));
    }
    else
    {
        var candidates = new FilteredElementCollector(doc)
            .WherePasses(new LogicalOrFilter(new ElementCategoryFilter(BuiltInCategory.OST_Mass),
                new ElementCategoryFilter(BuiltInCategory.OST_GenericModel)))
            .WhereElementIsNotElementType().ToList();
        var fits = candidates.Where(e => nameFits(e, hostText)).ToList();
        if (fits.Count == 1) hosts.Add(fits[0]);
        else if (fits.Count > 1)
            problems.Add("\"" + hostText + "\" fits " + fits.Count + " masses or generic models, so it does not say which - "
                + "name one by its element id: " + string.Join("; ", fits.Take(10).Select(describe).ToArray())
                + (fits.Count > 10 ? "; and " + (fits.Count - 10) + " more" : "") + ".");
        else
        {
            // Not a mass or a generic model by that name - is it something else?
            // Then the answer is its category, not "no such thing".
            var elsewhere = new FilteredElementCollector(doc).WhereElementIsNotElementType()
                .Where(e => e.Category != null && nameFits(e, hostText)).Take(3).ToList();
            if (elsewhere.Count > 0) foreach (var e in elsewhere) problems.Add(notAHost(e));
            else
                problems.Add("No mass or generic model in this model is called \"" + hostText + "\" - by its family, its "
                    + "type, its Mark or its id. "
                    + (candidates.Count == 0 ? "The model has none."
                        : "It has " + string.Join("; ", candidates.Take(10).Select(describe).ToArray())
                          + (candidates.Count > 10 ? "; and " + (candidates.Count - 10) + " more" : "") + "."));
        }
    }
    if (elements != null && elements.Count > 0)
        findings.Add("The selection was not used - the host was named.");
}

// ---------------------------------------------------------------------------
// EACH HOST'S FACES, judged by the way each one faces in the model
// ---------------------------------------------------------------------------

Func<Face, Transform, string> kindOf = (face, placed) =>
{
    var normals = new List<XYZ>();
    var planar = face as PlanarFace;
    if (planar != null) normals.Add(planar.FaceNormal);
    else
    {
        // A curved face is judged at nine points across it, and only a face
        // whose every point agrees is of that kind.
        BoundingBoxUV box = null;
        try { box = face.GetBoundingBox(); } catch (Exception) { box = null; }
        if (box != null)
            for (var i = 0; i < 3; i++)
                for (var j = 0; j < 3; j++)
                {
                    var at = new UV(box.Min.U + (box.Max.U - box.Min.U) * (0.1 + 0.4 * i),
                        box.Min.V + (box.Max.V - box.Min.V) * (0.1 + 0.4 * j));
                    try { normals.Add(face.ComputeNormal(at)); } catch (Exception) { }
                }
    }
    var turned = normals.Where(n => n != null && n.GetLength() > 1e-9).Select(n => placed.OfVector(n).Normalize()).ToList();
    if (turned.Count == 0) return "unread";
    if (turned.All(n => Math.Abs(n.Z) <= levelWithin)) return "vertical";
    if (turned.All(n => n.Z >= upWithin)) return "top";
    if (turned.All(n => n.Z <= -upWithin)) return "underside";
    return "sloped or curved";
};
var kinds = new[] { "vertical", "top", "underside", "sloped or curved", "unread" };
Func<List<Tuple<Face, string>>, string> tally = list => string.Join(", ", kinds
    .Select(k => new { k, n = list.Count(f => f.Item2 == k) }).Where(x => x.n > 0).Select(x => x.n + " " + x.k).ToArray());

var standing = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_CurtaSystem)
    .WhereElementIsNotElementType().Where(e => e is CurtainSystem).ToList();
Func<BoundingBoxXYZ, BoundingBoxXYZ, bool> overlaps = (a, b) => a != null && b != null
    && a.Min.X <= b.Max.X + OneMillimetre && b.Min.X <= a.Max.X + OneMillimetre
    && a.Min.Y <= b.Max.Y + OneMillimetre && b.Min.Y <= a.Max.Y + OneMillimetre
    && a.Min.Z <= b.Max.Z + OneMillimetre && b.Min.Z <= a.Max.Z + OneMillimetre;

// Each host's plan: the host, its chosen faces' references, how many, their
// area in square metres, and what its faces are.
var plans = new List<Tuple<Element, ReferenceArray, int, double, string>>();
if (facesKind != null)
{
    var reading = new Options { ComputeReferences = true };
    foreach (var each in hosts)
    {
        GeometryElement shape = null;
        try { shape = each.get_Geometry(reading); } catch (Exception) { shape = null; }
        var found = new List<Tuple<Face, string>>();
        var nestedLeftOut = 0;
        Action<GeometryElement, Transform, bool> walk = null;
        walk = (geometry, placed, inside) =>
        {
            if (geometry == null) return;
            foreach (GeometryObject item in geometry)
            {
                var nested = item as GeometryInstance;
                if (nested != null)
                {
                    // The host's own family, once; a family nested in it is left out.
                    if (!inside) walk(nested.GetSymbolGeometry(), placed.Multiply(nested.Transform), true);
                    else nestedLeftOut++;
                    continue;
                }
                var solid = item as Solid;
                if (solid == null || solid.Faces.Size == 0) continue;
                foreach (Face face in solid.Faces)
                    if (face.Reference != null) found.Add(Tuple.Create(face, kindOf(face, placed)));
            }
        };
        try { walk(shape, Transform.Identity, false); }
        catch (Exception ex) { problems.Add(describe(each) + "'s faces could not be read: " + ex.Message); continue; }
        if (nestedLeftOut > 0)
            findings.Add(describe(each) + ": " + nestedLeftOut + " family(ies) nested in it were left out - only its own "
                + "faces are offered.");

        var chosen = found.Where(f => facesKind == "all" || f.Item2 == facesKind).ToList();
        if (found.Count == 0)
        {
            problems.Add(describe(each) + " has no face Revit can reference - nothing to put a curtain system on.");
            continue;
        }
        if (chosen.Count == 0)
        {
            problems.Add(describe(each) + " has no " + (facesKind == "top" ? "top face looking straight up"
                : "upright face standing straight") + " - its " + found.Count + " face(s) are " + tally(found)
                + ". Say top or all instead.");
            continue;
        }

        var references = new ReferenceArray();
        foreach (var f in chosen) references.Append(f.Item1.Reference);
        plans.Add(Tuple.Create(each, references, chosen.Count, chosen.Sum(f => f.Item1.Area) * SquareFeetToSquareMetres,
            tally(found)));

        var near = standing.Where(s => overlaps(each.get_BoundingBox(null), s.get_BoundingBox(null))).ToList();
        if (near.Count > 0)
        {
            alreadyNear += near.Count;
            findings.Add(near.Count + " curtain system(s) already stand within the outline of " + describe(each) + " - "
                + string.Join(", ", near.Take(10).Select(s => s.Id.ToString()).ToArray()) + ". Whether they sit on the "
                + "same faces is not read; Revit may lay this one over them.");
        }
    }
}

if (problems.Count > 0) refused = string.Join(" ", problems.Distinct().ToArray()) + " NOTHING WAS MADE.";

// ---------------------------------------------------------------------------
// MAKE EACH ONE, THEN READ IT ALL BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var made = new List<Tuple<Tuple<Element, ReferenceArray, int, double, string>, List<ElementId>>>();
    foreach (var plan in plans)
    {
        ICollection<ElementId> ids;
        try { ids = doc.Create.NewCurtainSystem2(plan.Item2, chosenType); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not put a curtain system of \"" + fullName(chosenType)
                + "\" on " + plan.Item3 + " face(s) of " + describe(plan.Item1) + ": " + ex.Message + " The call failed, "
                + "and Heron rolls the whole call back.");
        }
        if (ids == null || ids.Count == 0)
            throw new InvalidOperationException("Revit made no curtain system on " + describe(plan.Item1) + " and raised "
                + "nothing. The call failed, and Heron rolls the whole call back.");
        facesAsked += plan.Item3;
        made.Add(Tuple.Create(plan, ids.ToList()));
    }

    doc.Regenerate();

    var lines = new List<string>();
    foreach (var pair in made)
    {
        var plan = pair.Item1;
        var takenHere = 0;
        var systemsHere = new List<string>();
        foreach (var id in pair.Item2)
        {
            var system = doc.GetElement(id) as CurtainSystem;
            if (system == null)
                throw new InvalidOperationException("Revit handed back " + id + " as a curtain system, and it reads as "
                    + "none. The call failed, and Heron rolls the whole call back.");
            var typeNow = system.CurtainSystemType;
            if (typeNow == null || typeNow.Id != chosenType.Id)
                throw new InvalidOperationException("Curtain system " + id + " reads type "
                    + (typeNow == null ? "none" : "\"" + fullName(typeNow) + "\"") + ", not \"" + fullName(chosenType)
                    + "\" as named. The call failed, and Heron rolls the whole call back.");

            var grids = new List<string>();
            var k = 0;
            var all = system.CurtainGrids;
            if (all != null)
                foreach (CurtainGrid grid in all)
                {
                    k++;
                    var panelIds = grid.GetPanelIds();
                    var panels = panelIds.Count(p => doc.GetElement(p) != null);
                    var empty = panelIds.Count - panels;
                    grids.Add("grid " + k + ": U lines " + grid.GetUGridLineIds().Count + ", V lines "
                        + grid.GetVGridLineIds().Count + ", panels " + panels + (empty > 0 ? " (" + empty + " empty)" : "")
                        + ", mullions " + grid.GetMullionIds().Count);
                }
            takenHere += k;
            created.Add(system);
            systemsHere.Add("curtain system " + id + " of \"" + fullName(typeNow) + "\", " + k + " face(s) taken - "
                + (grids.Count == 0 ? "no curtain grid reads back" : string.Join("; ", grids)));
        }
        facesTaken += takenHere;
        if (takenHere != plan.Item3)
            findings.Add("On " + describe(plan.Item1) + " Revit holds " + takenHere + " curtain grid(s) for the "
                + plan.Item3 + " face(s) handed to it - one grid per face, its reference says - so "
                + (takenHere < plan.Item3 ? "some faces carry none." : "it reads more grids than faces."));
        lines.Add(describe(plan.Item1) + ", " + plan.Item3 + " face(s) asked, "
            + Math.Round(plan.Item4, 2).ToString(invariant) + " m2 (its faces: " + plan.Item5 + "): "
            + string.Join("; ", systemsHere));
    }
    readBack = string.Join(" | ", lines);
    findings.Insert(0, "Made " + created.Count + " curtain system(s) of \"" + fullName(chosenType) + "\" on "
        + facesTaken + " of " + facesAsked + " face(s) asked - " + facesKind + " faces - of " + plans.Count
        + " mass(es) or generic model(s), each read back.");
    findings.Add(readBack);
}

if (refused != null) findings.Add(refused);
