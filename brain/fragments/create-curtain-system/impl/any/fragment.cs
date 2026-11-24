// NOT STANDALONE. Assumes `doc`, `elements`, `host`, `curtainSystemType` and
// `faces` are in scope; leaves `created`, `facesTaken`, `facesAsked`,
// `readBack`, `alreadyNear`, `alreadyGlazed`, `refused` and `findings` behind.
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
//   faces              vertical (the default), top, underside, sloped, all,
//                      face numbers as every reply lists them ("3, 5 and 9"),
//                      joined ("vertical and sloped") or taken away ("all but
//                      top and underside")
//
// EVERY REFUSAL IS MADE BEFORE THE FIRST WRITE: no host, a host that is not a
// mass or a generic model, a type the model does not have, a face word this
// does not know, a face number the host has not got, a host with no face Revit
// can reference, no face of the kind asked, or A FACE ASKED THAT ALREADY
// CARRIES A CURTAIN SYSTEM - never a second skin laid over the first. One host
// refused refuses the call - nothing is half made.
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
var alreadyGlazed = 0;
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
//
// A face is named by its KIND - vertical, top, underside, sloped - or by its
// NUMBER, as every reply lists a host's faces. Several join with "and" or a
// comma ("vertical and sloped", "3, 5 and 9", "4 to 7"); "all but ..." or
// "all except ..." takes every face less the ones named ("all but top and
// underside"). Numbers name one host's faces, so they need ONE host.

var facesText = (faces ?? "").Trim();
var kindWords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "vertical", "vertical" }, { "upright", "vertical" }, { "side", "vertical" }, { "sides", "vertical" },
    { "top", "top" }, { "tops", "top" }, { "roof", "top" },
    { "underside", "underside" }, { "undersides", "underside" }, { "bottom", "underside" },
    { "soffit", "underside" }, { "soffits", "underside" },
    { "sloped", "sloped or curved" }, { "sloping", "sloped or curved" }, { "tilted", "sloped or curved" },
    { "inclined", "sloped or curved" }, { "slanted", "sloped or curved" }, { "curved", "sloped or curved" },
};
var allWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "all", "every", "everything" };
var butWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "but", "except", "excluding", "without", "minus" };
var fillerWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "the", "a", "face", "faces", "of", "and", "or", "on", "its", "only", "just", "number", "numbers", "no", "nos",
      "mass", "this", "that", "these", "those", "host" };
var wantAll = false;
var excluding = false;
var kindsIn = new HashSet<string>();
var kindsOut = new HashSet<string>();
var numbersIn = new SortedSet<int>();
var numbersOut = new SortedSet<int>();
var unknownWords = new List<string>();
foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(
    facesText.ToLowerInvariant(), @"\d+\s*(?:-|to)\s*\d+|\d+|[a-z]+"))
{
    var word = m.Value;
    var range = System.Text.RegularExpressions.Regex.Match(word, @"^(\d+)\s*(?:-|to)\s*(\d+)$");
    if (range.Success || char.IsDigit(word[0]))
    {
        int from, to;
        if (!int.TryParse(range.Success ? range.Groups[1].Value : word, out from)) from = 0;
        if (!int.TryParse(range.Success ? range.Groups[2].Value : word, out to)) to = 0;
        if (from < 1 || to < from || to - from > 999)
            problems.Add("\"" + word + "\" is not a face number or a range of them - faces are numbered from 1, as the "
                + "reply lists them.");
        else
            for (var n = from; n <= to; n++) (excluding ? numbersOut : numbersIn).Add(n);
    }
    else if (butWords.Contains(word)) excluding = true;
    else if (allWords.Contains(word)) wantAll = true;
    else if (kindWords.ContainsKey(word)) (excluding ? kindsOut : kindsIn).Add(kindWords[word]);
    else if (!fillerWords.Contains(word)) unknownWords.Add(word);
}
if (excluding && kindsIn.Count == 0 && numbersIn.Count == 0) wantAll = true;   // "except the top": every other face
if (!wantAll && kindsIn.Count == 0 && numbersIn.Count == 0 && unknownWords.Count == 0) kindsIn.Add("vertical");
if (unknownWords.Count > 0)
    problems.Add("\"" + facesText + "\" says " + string.Join(", ", unknownWords.Distinct().Select(w => "\"" + w + "\"")
        .ToArray()) + ", which this does not know for a face. Say vertical (the upright side faces, the default), top, "
        + "underside, sloped (tilted or curved), all, face numbers as the reply lists them (\"3, 5 and 9\"), or join "
        + "them (\"vertical and sloped\", \"all but top and underside\").");
var usesNumbers = numbersIn.Count > 0 || numbersOut.Count > 0;
Func<IEnumerable<int>, string> numberList = list => string.Join(", ", list.Select(n => n.ToString(invariant)).ToArray());
// The faces asked for, in words - what the reply says was meant.
var facesKind = (wantAll ? "all" : "")
    + (wantAll ? "" : string.Join(" and ", kindsIn.Select(k => k == "sloped or curved" ? "sloped" : k)
        .Concat(numbersIn.Count > 0 ? new[] { "face(s) " + numberList(numbersIn) } : new string[0]).ToArray()))
    + (kindsOut.Count + numbersOut.Count > 0 ? " but " + string.Join(" and ", kindsOut
        .Select(k => k == "sloped or curved" ? "sloped" : k)
        .Concat(numbersOut.Count > 0 ? new[] { "face(s) " + numberList(numbersOut) } : new string[0]).ToArray()) : "");
Func<int, string, bool> isAsked = (number, kind) =>
    (wantAll || kindsIn.Contains(kind) || numbersIn.Contains(number))
    && !kindsOut.Contains(kind) && !numbersOut.Contains(number);

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
// EACH HOST'S FACES, numbered and judged by the way each one faces in the model
// ---------------------------------------------------------------------------

// The way a face looks, at its middle: a compass point in the model's own
// directions (project north is +Y) and how far it tilts from level.
Func<XYZ, string> facing = n =>
{
    if (n == null) return "direction not read";
    var up = Math.Asin(Math.Max(-1.0, Math.Min(1.0, n.Z))) * 180.0 / Math.PI;
    if (up >= 89.0) return "looks up";
    if (up <= -89.0) return "looks down";
    var bearing = Math.Atan2(n.X, n.Y) * 180.0 / Math.PI;
    if (bearing < 0) bearing += 360.0;
    var points = new[] { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };
    var side = "looks " + points[(int)Math.Round(bearing / 45.0) % 8];
    return Math.Abs(up) < 1.0 ? side
        : side + ", " + Math.Round(Math.Abs(up)).ToString(invariant) + " deg " + (up > 0 ? "up" : "down") + " from level";
};
Func<Face, Transform, XYZ> middleNormal = (face, placed) =>
{
    XYZ n = null;
    var planar = face as PlanarFace;
    try
    {
        if (planar != null) n = planar.FaceNormal;
        else
        {
            var box = face.GetBoundingBox();
            n = face.ComputeNormal(new UV((box.Min.U + box.Max.U) / 2.0, (box.Min.V + box.Max.V) / 2.0));
        }
    }
    catch (Exception) { n = null; }
    return n == null || n.GetLength() < 1e-9 ? null : placed.OfVector(n).Normalize();
};
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
Func<IEnumerable<string>, string> tally = list => string.Join(", ", kinds
    .Select(k => new { k, n = list.Count(f => f == k) }).Where(x => x.n > 0).Select(x => x.n + " " + x.k).ToArray());

var standing = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_CurtaSystem)
    .WhereElementIsNotElementType().Where(e => e is CurtainSystem).Cast<CurtainSystem>().ToList();
Func<BoundingBoxXYZ, BoundingBoxXYZ, bool> overlaps = (a, b) => a != null && b != null
    && a.Min.X <= b.Max.X + OneMillimetre && b.Min.X <= a.Max.X + OneMillimetre
    && a.Min.Y <= b.Max.Y + OneMillimetre && b.Min.Y <= a.Max.Y + OneMillimetre
    && a.Min.Z <= b.Max.Z + OneMillimetre && b.Min.Z <= a.Max.Z + OneMillimetre;
// A point of the model lies on a face of the host when the face, in the
// family's own coordinates, holds it to within 5 mm.
var onFaceWithin = 5.0 * OneMillimetre;
Func<Face, Transform, XYZ, bool> liesOn = (face, placed, point) =>
{
    try
    {
        var hit = face.Project(placed.Inverse.OfPoint(point));
        return hit != null && hit.Distance <= onFaceWithin;
    }
    catch (Exception) { return false; }
};

// Each host's plan: the host, its chosen faces' references, how many, their
// area in square metres, what its faces are, and the numbers of those chosen.
var plans = new List<Tuple<Element, ReferenceArray, int, double, string, string>>();
var oneHostForNumbers = !usesNumbers || hosts.Count <= 1;
if (!oneHostForNumbers)
    problems.Add("Face numbers name the faces of ONE mass, and " + hosts.Count + " are selected - name the one meant, "
        + "or ask by kind (vertical, top, underside, sloped).");
if (unknownWords.Count == 0 && oneHostForNumbers)
{
    var reading = new Options { ComputeReferences = true };
    foreach (var each in hosts)
    {
        GeometryElement shape = null;
        try { shape = each.get_Geometry(reading); } catch (Exception) { shape = null; }
        // Each face: the face, how it is placed in the model, and its kind.
        var found = new List<Tuple<Face, Transform, string>>();
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
                    if (face.Reference != null) found.Add(Tuple.Create(face, placed, kindOf(face, placed)));
            }
        };
        try { walk(shape, Transform.Identity, false); }
        catch (Exception ex) { problems.Add(describe(each) + "'s faces could not be read: " + ex.Message); continue; }
        if (nestedLeftOut > 0)
            findings.Add(describe(each) + ": " + nestedLeftOut + " family(ies) nested in it were left out - only its own "
                + "faces are offered.");
        if (found.Count == 0)
        {
            problems.Add(describe(each) + " has no face Revit can reference - nothing to put a curtain system on.");
            continue;
        }

        // WHICH OF ITS FACES ALREADY CARRY A CURTAIN SYSTEM. Revit gives a
        // curtain system no host face to read back, so it is found from the
        // system itself: each of its curtain grids (one per face) is read cell
        // by cell, and the face that holds the middle of EVERY edge of a cell
        // is the face that grid sits on. A cell's outer edges also touch the
        // next face, but never all of them at once.
        var glazedBy = new Dictionary<int, List<string>>();
        var near = standing.Where(s => overlaps(each.get_BoundingBox(null), s.get_BoundingBox(null))).ToList();
        var unreadNear = new List<string>();
        foreach (var system in near)
        {
            var cellsRead = 0;
            CurtainGridSet grids = null;
            try { grids = system.CurtainGrids; } catch (Exception) { grids = null; }
            if (grids != null)
                foreach (CurtainGrid grid in grids)
                {
                    ICollection<CurtainCell> cells = null;
                    try { cells = grid.GetCurtainCells(); } catch (Exception) { cells = null; }
                    if (cells == null) continue;
                    var tried = 0;
                    foreach (var cell in cells)
                    {
                        if (tried >= 5) break;   // a grid is one face: a few cells settle which
                        var middles = new List<XYZ>();
                        try
                        {
                            foreach (CurveArray loop in cell.CurveLoops)
                                foreach (Curve edge in loop) middles.Add(edge.Evaluate(0.5, true));
                        }
                        catch (Exception) { middles.Clear(); }
                        if (middles.Count == 0) continue;
                        tried++;
                        cellsRead++;
                        var at = found.FindIndex(f => middles.All(p => liesOn(f.Item1, f.Item2, p)));
                        if (at < 0) continue;
                        if (!glazedBy.ContainsKey(at)) glazedBy[at] = new List<string>();
                        if (!glazedBy[at].Contains(system.Id.ToString())) glazedBy[at].Add(system.Id.ToString());
                        break;
                    }
                }
            if (cellsRead == 0) unreadNear.Add(system.Id.ToString());
        }
        if (near.Count > 0)
        {
            alreadyNear += near.Count;
            findings.Add(near.Count + " curtain system(s) already stand within the outline of " + describe(each) + " - "
                + string.Join(", ", near.Take(10).Select(s => s.Id.ToString()).ToArray()) + "."
                + (unreadNear.Count > 0 ? " Which faces " + string.Join(", ", unreadNear.ToArray()) + " sit on could "
                    + "not be read from their curtain grids - Revit may lay this one over them." : ""));
        }

        if (usesNumbers)
        {
            var beyond = numbersIn.Concat(numbersOut).Where(n => n > found.Count).Distinct().ToList();
            if (beyond.Count > 0)
                problems.Add(describe(each) + " has " + found.Count + " face(s), numbered 1 to " + found.Count
                    + " - face " + numberList(beyond) + " is not one of them.");
        }

        // The numbered list every reply carries, so faces can be named by number.
        var chosenAt = Enumerable.Range(0, found.Count).Where(i => isAsked(i + 1, found[i].Item3)).ToList();
        var listed = new List<string>();
        for (var i = 0; i < found.Count && i < 60; i++)
            listed.Add("face " + (i + 1) + ": " + found[i].Item3 + ", " + facing(middleNormal(found[i].Item1, found[i].Item2))
                + ", " + Math.Round(found[i].Item1.Area * SquareFeetToSquareMetres, 1).ToString(invariant) + " m2"
                + (glazedBy.ContainsKey(i) ? ", carries curtain system " + string.Join(", ", glazedBy[i].ToArray()) : "")
                + (chosenAt.Contains(i) ? " - ASKED" : ""));
        findings.Add(describe(each) + " - its faces, numbered as Revit reads them now (read them again after the mass "
            + "is edited; directions in the model's own, project north up): " + string.Join("; ", listed.ToArray())
            + (found.Count > 60 ? "; and " + (found.Count - 60) + " more" : "") + ".");

        if (chosenAt.Count == 0)
        {
            problems.Add(describe(each) + " has no face that is " + facesKind + " - its " + found.Count + " face(s) are "
                + tally(found.Select(f => f.Item3)) + ". Ask for another kind, or name faces by number from the list.");
            continue;
        }

        // NEVER A SECOND SKIN: a face asked that already carries a curtain
        // system refuses the call - Revit would lay a second one over it
        // (NEEDS-CHECKING JM8: on Revit 2020 it did).
        var doubled = chosenAt.Where(i => glazedBy.ContainsKey(i)).ToList();
        alreadyGlazed += doubled.Count;
        if (doubled.Count > 0)
        {
            var bare = chosenAt.Where(i => !glazedBy.ContainsKey(i)).Select(i => i + 1).ToList();
            problems.Add(describe(each) + ": face(s) " + numberList(doubled.Select(i => i + 1)) + " already carry a "
                + "curtain system (" + string.Join(", ", doubled.SelectMany(i => glazedBy[i]).Distinct().ToArray())
                + "), and a second one would be laid over it. "
                + (bare.Count > 0 ? "Of the faces asked, " + numberList(bare) + " are bare - ask for those by number. "
                    : "Every face asked already carries one. ")
                + "To replace a curtain system, delete it first.");
            continue;
        }

        var references = new ReferenceArray();
        foreach (var i in chosenAt) references.Append(found[i].Item1.Reference);
        plans.Add(Tuple.Create(each, references, chosenAt.Count,
            chosenAt.Sum(i => found[i].Item1.Area) * SquareFeetToSquareMetres, tally(found.Select(f => f.Item3)),
            numberList(chosenAt.Select(i => i + 1))));
    }
}

if (problems.Count > 0) refused = string.Join(" ", problems.Distinct().ToArray()) + " NOTHING WAS MADE.";

// ---------------------------------------------------------------------------
// MAKE EACH ONE, THEN READ IT ALL BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var made = new List<Tuple<Tuple<Element, ReferenceArray, int, double, string, string>, List<ElementId>>>();
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
        lines.Add(describe(plan.Item1) + ", " + plan.Item3 + " face(s) asked - face(s) " + plan.Item6 + " - "
            + Math.Round(plan.Item4, 2).ToString(invariant) + " m2 (its faces: " + plan.Item5 + "): "
            + string.Join("; ", systemsHere));
    }
    readBack = string.Join(" | ", lines);
    findings.Insert(0, "Made " + created.Count + " curtain system(s) of \"" + fullName(chosenType) + "\" on "
        + facesTaken + " of " + facesAsked + " face(s) asked - " + facesKind + " - of " + plans.Count
        + " mass(es) or generic model(s), each read back.");
    findings.Add(readBack);
}

if (refused != null) findings.Add(refused);
