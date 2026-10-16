// NOT STANDALONE. Assumes `doc`, `view`, `between`, `lineFrom` and `lineTo` are
// in scope; leaves `created`, `measured`, `refused` and `findings`.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Lengths are internal FEET.
//
// THE PLAIN DIMENSION. CREATE_DIMENSION is the duct-spacing one and says so in
// its own purpose; every other dimension fragment here is equally specific.
// Measured 2026-09-18, none of them answered "dimension from this to that".
//
// THE CALL IS NewDimension, NOT NewLinearDimension, AND THE DIFFERENCE COSTS A
// COMPILE. The obvious-sounding name belongs to the FAMILY EDITOR's creation
// object, which a project document cannot reach. What `doc.Create` returns
// inherits the base item factory instead, and NewDimension is the one that lives
// there. Checked 2026-09-18 against the reference assemblies: it held on every
// release 2020 to 2027.
//
// WHAT IT MEASURES BETWEEN IS TYPED, AND ONLY WHAT HAS A NAME OR AN ID. Version 1
// took Revit `Reference` objects, which nothing can type (FRAGMENT-ISSUES
// section 6, D-72) - so it could be run by nobody. A face picked with the mouse
// still cannot be said; a grid, a level, a named reference plane, a drawn line
// and a wall's exterior or interior face can, and those are what a drawing
// dimensions to most of the time:
//   grid A | level Level 2 | plane Box Left | line 123456 | wall 123456 exterior
// `|` between them; two is the minimum, three or more make a chained string.
// An id is the number Revit shows (Manage > IDs of Selection) or a unique id.
//
// VERSION 3 (2026-10-15, FRAGMENT-ISSUES 5b-482) REACHES A FAMILY INSTANCE, so
// a builders-work opening, a fitting or a box can be dimensioned from a grid,
// a level, or itself. Two words more; every word above reads as it did:
//   family 123456 Left         one of the instance's own references - Left,
//                              Right, Top, Bottom, Front, Back,
//                              CenterLeftRight, CenterFrontBack,
//                              CenterElevation - or a reference plane's NAME,
//                              strong or weak. One the family does not
//                              publish is refused with the list it does.
//   face 123456 top            the element's planar solid face pointing most
//                              toward left, right, top or bottom OF THIS
//                              VIEW (near and far: toward and away from
//                              whoever looks). ANY element, even a family
//                              that publishes no named reference at all -
//                              the face is read from the geometry this view
//                              shows, with references computed.
// The face is the outermost one when two face the same way, and one more than
// a few degrees off square to the view is said so in `findings`.
//
// VERSION 4 (2026-10-15, FRAGMENT-ISSUES 5b-486) REACHES INTO A LOADED LINK.
// On a real project (Revit 2020) the openings to dimension were Generic Model
// cut-outs in a LINKED model, tagged in the host's plans - and every word
// above looks only in the host, so none of them could be named. One word
// more, in front of any other; every word above reads as it did:
//   link 6347789 face 6667244 left        the placed link's id as Revit
//   link 4355-BNVD-3D-21P60-ST001I family 6667244 CenterLeftRight
//                                         shows it, its unique id, or the
//                                         linked model's name with or
//                                         without ".rvt" ("quotes" round a
//                                         name with a word of these in it)
// The word after it - grid, level, plane, line, wall, family or face - is
// read exactly as above but INSIDE THE LINK'S OWN DOCUMENT: its ids, its
// names, its geometry (read with references computed and no view, since a
// view of this model cannot read the link's elements; the detail level is
// this view's). Directions are carried into this model by the link's
// placement, so `face ID left` still means left OF THIS VIEW. The reference
// found is then carried across with Reference.CreateLinkReference, which is
// what lets a dimension in this model hold an element of another - and, as
// Revit 2020 was measured to need (see `link` below), written into the one
// form NewDimension accepts before it is used.
// A link that is not loaded, or hidden in this view, is refused naming the
// links this model has; an id not in that link is refused naming where it
// IS - this model or another loaded link - when it is anywhere. A link
// inside a link is refused: one link is as far as Revit carries a reference.
//
// THE LINE IS WHERE THE STRING SITS, two points in millimetres, and it is the
// caller's: where a dimension goes on a sheet is a drafting decision (D-33).
// THE SAME POINT GIVEN TWICE (version 3) asks for the string to run THROUGH
// that point, square to what it measures - the direction is read off the
// references, which fix it, not guessed. The point is moved into the view's
// own plane, as DIMENSION_ELEMENT_FACES places its strings. A blank point
// cannot be offered: the binder refuses any absent value but a switch
// (HeronBindingNote.AbsentValue), and changing that is a rule for every tool.
//
// READ BACK: the dimension's references are counted and every segment's value is
// read, so the reply says what Revit measured rather than what was asked for.

var created = ElementId.InvalidElementId;
var measured = "";
string refused = null;
var findings = new List<string>();

Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(System.Globalization.CultureInfo.InvariantCulture);

// WHERE THE WORDS LOOK (version 4). This model, until `link` points them into
// a loaded link's document for the one word after it; then back. `toHost` is
// that link's placement here, and the two helpers below carry a direction or
// a point read inside the link into this model's coordinates - and leave one
// read in this model exactly as it was read.
Document source = doc;
RevitLinkInstance viaLink = null;
var toHost = Transform.Identity;
var inLink = "";
var linkWords = "";
var linkedAsked = 0;
Func<XYZ, XYZ> hostVector = v => v == null || viaLink == null ? v : toHost.OfVector(v);
Func<XYZ, XYZ> hostPoint = p => p == null || viaLink == null ? p : toHost.OfPoint(p);

// ONE ELEMENT BY THE NUMBER REVIT SHOWS OR ITS UNIQUE ID. ElementId(long) does not
// exist before 2024 and ElementId(int) is gone by 2026, so the number is matched
// against each candidate's own Id.ToString() rather than constructed.
Func<string, Type, Element> byId = (text, kind) =>
{
    var said = (text ?? "").Trim();
    if (said.Length == 0) return null;
    Element found = null;
    try { found = source.GetElement(said); } catch (Exception) { found = null; }
    if (found != null) return kind.IsInstanceOfType(found) ? found : null;
    foreach (var e in new FilteredElementCollector(source).OfClass(kind).WhereElementIsNotElementType())
        if (e.Id.ToString() == said) return e;
    return null;
};

// ANY ELEMENT BY ITS NUMBER OR UNIQUE ID, for `face`. byId above walks one
// class, and `face` takes any element, so the number is matched against what
// THIS VIEW shows - a face the view does not show cannot be dimensioned in it.
// Inside a link no view of this model can list the link's elements, so the
// whole link is searched, and whether the view shows it is Revit's to say.
Func<string, Element> anyById = text =>
{
    var said = (text ?? "").Trim();
    if (said.Length == 0) return null;
    Element found = null;
    try { found = source.GetElement(said); } catch (Exception) { found = null; }
    if (found != null) return found;
    var looked = viaLink == null ? new FilteredElementCollector(doc, view.Id) : new FilteredElementCollector(source);
    foreach (var e in looked.WhereElementIsNotElementType())
        if (e.Id.ToString() == said) return e;
    return null;
};

// THE LINKS THIS MODEL PLACES, and how one is said back: the linked file's
// name - "Structure.rvt" - and the id Revit shows for the placed link.
Func<RevitLinkInstance, string> fileOf = placed =>
{
    var name = placed.Name ?? "";
    var colon = name.IndexOf(" : ", StringComparison.Ordinal);
    return colon < 0 ? name : name.Substring(0, colon);
};
Func<List<RevitLinkInstance>> links = () => new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance))
    .Cast<RevitLinkInstance>().ToList();
Func<RevitLinkInstance, string> linkSaid = placed => "'" + fileOf(placed) + "' (id " + placed.Id
    + (placed.GetLinkDocument() == null ? ", NOT LOADED" : "") + ")";
Func<RevitLinkInstance, string> linkCalled = placed => "'" + fileOf(placed) + "' (id " + placed.Id + ")";

// WHERE AN ID IS, when it is not where it was looked for - this model, or
// another loaded link - so a refusal names what IS there, not only what is
// not. Only on the way to a refusal: it reads every loaded link through.
Func<string, string> elsewhere = said =>
{
    var places = new List<string>();
    Func<Element, string> called = e => (e.Category == null ? "" : e.Category.Name + " ") + "'" + e.Name + "'";
    if (viaLink != null)
    {
        foreach (var e in new FilteredElementCollector(doc).WhereElementIsNotElementType())
            if (e.Id.ToString() == said) { places.Add("this model itself, " + called(e) + " - said without `link`"); break; }
    }
    foreach (var other in links())
    {
        if (viaLink != null && other.Id == viaLink.Id) continue;
        var otherDoc = other.GetLinkDocument();
        if (otherDoc == null) continue;
        foreach (var e in new FilteredElementCollector(otherDoc).WhereElementIsNotElementType())
            if (e.Id.ToString() == said)
            {
                places.Add("link " + linkSaid(other) + ", " + called(e) + " - \"link " + other.Id + " ...\"");
                break;
            }
    }
    return places.Count == 0 ? "" : " Element " + said + " is in " + string.Join(", and in ", places) + ".";
};

// WHICH WAY EACH REFERENCE MEASURES ACROSS - the normal of the plane it names,
// in the model, and a point on it - for the one-point line below. Left null
// where it cannot be read without guessing: an arc grid, a named family plane.
XYZ lastNormal = null;
XYZ lastPoint = null;

// ONE TYPED REFERENCE, or null with the reason added to `problems`.
var problems = new List<string>();
Func<string, Reference> referenceOf = null;
referenceOf = token =>
{
    var text = (token ?? "").Trim();
    var space = text.IndexOf(' ');
    var word = (space < 0 ? text : text.Substring(0, space)).ToLowerInvariant();
    var rest = space < 0 ? "" : text.Substring(space + 1).Trim();
    lastNormal = null;
    lastPoint = null;
    if (word == "grid")
    {
        var grid = new FilteredElementCollector(source).OfClass(typeof(Grid)).Cast<Grid>()
            .FirstOrDefault(g => string.Equals(g.Name, rest, StringComparison.OrdinalIgnoreCase));
        if (grid == null) { problems.Add("No grid is called \"" + rest + "\"" + inLink + "."); return null; }
        var gridLine = grid.Curve as Line;
        if (gridLine != null) { lastNormal = hostVector(gridLine.Direction.CrossProduct(XYZ.BasisZ)); lastPoint = hostPoint(gridLine.GetEndPoint(0)); }
        return new Reference(grid);
    }
    if (word == "level")
    {
        var level = new FilteredElementCollector(source).OfClass(typeof(Level)).Cast<Level>()
            .FirstOrDefault(l => string.Equals(l.Name, rest, StringComparison.OrdinalIgnoreCase));
        if (level == null) { problems.Add("No level is called \"" + rest + "\"" + inLink + "."); return null; }
        lastNormal = hostVector(XYZ.BasisZ);
        lastPoint = hostPoint(new XYZ(0, 0, level.Elevation));
        return level.GetPlaneReference();
    }
    if (word == "plane")
    {
        var plane = new FilteredElementCollector(source).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>()
            .FirstOrDefault(p => string.Equals(p.Name, rest, StringComparison.OrdinalIgnoreCase));
        if (plane == null) { problems.Add("No reference plane is called \"" + rest + "\"" + inLink + "."); return null; }
        lastNormal = hostVector(plane.Normal);
        lastPoint = hostPoint(plane.BubbleEnd);
        return plane.GetReference();
    }
    if (word == "line")
    {
        var line = byId(rest, typeof(CurveElement)) as CurveElement;
        if (line == null) { problems.Add("No model or detail line has the id \"" + rest + "\"" + inLink + "." + elsewhere(rest)); return null; }
        var reference = line.GeometryCurve == null ? null : line.GeometryCurve.Reference;
        if (reference == null) problems.Add("Line " + rest + " gives Revit no reference to dimension to.");
        var straight = line.GeometryCurve as Line;
        if (straight != null) { lastNormal = hostVector(straight.Direction).CrossProduct(view.ViewDirection); lastPoint = hostPoint(straight.GetEndPoint(0)); }
        return reference;
    }
    if (word == "wall")
    {
        var parts = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var wall = parts.Length == 0 ? null : byId(parts[0], typeof(Wall)) as Wall;
        if (wall == null)
        {
            problems.Add("No wall has the id \"" + (parts.Length == 0 ? "" : parts[0]) + "\"" + inLink + "."
                + (parts.Length == 0 ? "" : elsewhere(parts[0])));
            return null;
        }
        var side = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";
        if (side != "exterior" && side != "interior")
        {
            problems.Add("Say which face of wall " + parts[0] + ": \"" + linkWords + "wall " + parts[0] + " exterior\" or \""
                + linkWords + "wall " + parts[0] + " interior\".");
            return null;
        }
        var faces = HostObjectUtils.GetSideFaces(wall, side == "exterior" ? ShellLayerType.Exterior : ShellLayerType.Interior);
        if (faces == null || faces.Count == 0) { problems.Add("Wall " + parts[0] + " has no " + side + " face Revit can name."); return null; }
        var located = wall.Location as LocationCurve;
        lastNormal = hostVector(wall.Orientation);
        lastPoint = located == null || located.Curve == null ? null : hostPoint(located.Curve.GetEndPoint(0));
        return faces[0];
    }
    if (word == "family")
    {
        // `family ID WHICH` - WHICH may be a plane name with spaces in it.
        var cut = rest.IndexOf(' ');
        var idText = cut < 0 ? rest : rest.Substring(0, cut);
        var which = cut < 0 ? "" : rest.Substring(cut + 1).Trim();
        var instance = byId(idText, typeof(FamilyInstance)) as FamilyInstance;
        if (instance == null) { problems.Add("No family instance has the id \"" + idText + "\"" + inLink + "." + elsewhere(idText)); return null; }

        // EVERY REFERENCE IT PUBLISHES, with its kind and its name - asked of
        // every kind, so a refusal can list them all.
        var published = new List<Tuple<FamilyInstanceReferenceType, string, Reference>>();
        foreach (FamilyInstanceReferenceType kind in Enum.GetValues(typeof(FamilyInstanceReferenceType)))
        {
            if (kind == FamilyInstanceReferenceType.NotAReference) continue;
            IList<Reference> found = null;
            try { found = instance.GetReferences(kind); } catch (Exception) { found = null; }
            if (found == null) continue;
            foreach (var r in found)
            {
                string name = null;
                try { name = instance.GetReferenceName(r); } catch (Exception) { name = null; }
                published.Add(Tuple.Create(kind, name ?? "", r));
            }
        }

        // "Center (Left/Right)" as the Is Reference list writes it, "centre"
        // as it is said here, and CenterLeftRight all read alike.
        Func<string, string> squash = s => new string((s ?? "").ToLowerInvariant().Replace("centre", "center")
            .Where(char.IsLetterOrDigit).ToArray());
        Func<FamilyInstanceReferenceType, bool> named = k =>
            k == FamilyInstanceReferenceType.StrongReference || k == FamilyInstanceReferenceType.WeakReference;
        var wanted = squash(which);
        var hit = wanted.Length == 0 ? null
            : published.FirstOrDefault(p => !named(p.Item1) && squash(p.Item1.ToString()) == wanted);
        if (hit == null && which.Length > 0)
            hit = published.FirstOrDefault(p => string.Equals(p.Item2.Trim(), which, StringComparison.OrdinalIgnoreCase));
        Reference picked = hit == null ? null : hit.Item3;
        if (picked == null && which.Length > 0)
        {
            try { picked = instance.GetReferenceByName(which); } catch (Exception) { picked = null; }
        }

        if (picked == null)
        {
            var offered = published.Select(p => named(p.Item1)
                    ? (p.Item1 == FamilyInstanceReferenceType.StrongReference ? "strong " : "weak ")
                        + (p.Item2.Length == 0 ? "(no name)" : "\"" + p.Item2 + "\"")
                    : p.Item1.ToString())
                .Distinct().ToList();
            var symbol = instance.Symbol;
            var called = symbol == null ? "" : " (" + symbol.FamilyName + ": " + symbol.Name + ")";
            problems.Add((which.Length == 0
                    ? "Say which reference of family instance " + idText + inLink + called + " - \"" + linkWords + "family "
                        + idText + " Left\". "
                    : "Family instance " + idText + inLink + called + " has no reference called \"" + which + "\". ")
                + (offered.Count == 0 ? "It publishes none." : "It publishes " + string.Join(", ", offered) + ".")
                + " \"" + linkWords + "face " + idText + " left|right|top|bottom\" reaches its solid faces whatever it publishes.");
            return null;
        }

        // The direction a positional reference measures across is the
        // instance's own axis; a plane known only by its name is not guessed.
        FamilyInstanceReferenceType kindOf = FamilyInstanceReferenceType.NotAReference;
        try { kindOf = instance.GetReferenceType(picked); } catch (Exception) { }
        var placed = instance.GetTransform();
        if (kindOf == FamilyInstanceReferenceType.Left || kindOf == FamilyInstanceReferenceType.CenterLeftRight
            || kindOf == FamilyInstanceReferenceType.Right) lastNormal = hostVector(placed.BasisX);
        else if (kindOf == FamilyInstanceReferenceType.Front || kindOf == FamilyInstanceReferenceType.CenterFrontBack
            || kindOf == FamilyInstanceReferenceType.Back) lastNormal = hostVector(placed.BasisY);
        else if (kindOf == FamilyInstanceReferenceType.Bottom || kindOf == FamilyInstanceReferenceType.CenterElevation
            || kindOf == FamilyInstanceReferenceType.Top) lastNormal = hostVector(placed.BasisZ);
        lastPoint = hostPoint(placed.Origin);
        return picked;
    }
    if (word == "face")
    {
        var parts = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var idText = parts.Length == 0 ? "" : parts[0];
        var side = parts.Length > 1 ? parts[1].ToLowerInvariant() : "";
        var toward = side == "left" ? view.RightDirection.Negate()
            : side == "right" ? view.RightDirection
            : side == "top" ? view.UpDirection
            : side == "bottom" ? view.UpDirection.Negate()
            : side == "near" ? view.ViewDirection
            : side == "far" ? view.ViewDirection.Negate()
            : null;
        if (toward == null)
        {
            problems.Add("Say which face of element " + idText + inLink + ", as this view shows it: \"" + linkWords + "face "
                + idText + " left\", right, top, bottom, near or far.");
            return null;
        }
        toward = toward.Normalize();
        var element = anyById(idText);
        if (element == null)
        {
            problems.Add("No element with the id \"" + idText + "\" is in "
                + (viaLink == null ? "'" + view.Name + "'" : "link '" + fileOf(viaLink) + "'") + "." + elsewhere(idText));
            return null;
        }

        // Inside a link the geometry is read with no view - a view of this
        // model cannot read another document's elements - at this view's
        // detail level, so the faces are the ones this view draws.
        GeometryElement shown = null;
        var reading = new Options { ComputeReferences = true };
        if (viaLink == null) reading.View = view; else reading.DetailLevel = view.DetailLevel;
        try { shown = element.get_Geometry(reading); }
        catch (Exception) { shown = null; }
        if (shown == null)
        {
            problems.Add("Element " + idText + inLink + " shows no geometry" + (viaLink == null ? " in '" + view.Name + "'" : "")
                + ".");
            return null;
        }

        // EVERY PLANAR FACE WITH A REFERENCE, a family's symbol geometry
        // included. A GeometryInstance is read through GetSymbolGeometry, NOT
        // GetInstanceGeometry: the instance copy carries references Revit will
        // not dimension to, and the symbol's - reached through this instance -
        // are the ones it will. Its transform is applied to the normal and the
        // point only, so the face is judged where it sits in the model - and,
        // inside a link, the link's placement too, so it is judged where it
        // sits in THIS model, against this view's left and right.
        Reference best = null;
        double bestDot = 0, bestAt = 0;
        XYZ bestNormal = null, bestPoint = null;
        Action<GeometryElement, Transform> walk = null;
        walk = (geometry, placed) =>
        {
            if (geometry == null) return;
            foreach (GeometryObject item in geometry)
            {
                var nested = item as GeometryInstance;
                if (nested != null) { walk(nested.GetSymbolGeometry(), placed.Multiply(nested.Transform)); continue; }
                var solid = item as Solid;
                if (solid == null || solid.Faces.Size == 0) continue;
                foreach (Face face in solid.Faces)
                {
                    var planar = face as PlanarFace;
                    if (planar == null || planar.Reference == null) continue;
                    var normal = placed.OfVector(planar.FaceNormal).Normalize();
                    var point = placed.OfPoint(planar.Origin);
                    var dot = Math.Round(normal.DotProduct(toward), 3);
                    var at = point.DotProduct(toward);
                    if (dot <= 0.5) continue;
                    if (best == null || dot > bestDot || (dot == bestDot && at > bestAt + 1e-6))
                    {
                        best = planar.Reference; bestDot = dot; bestAt = at; bestNormal = normal; bestPoint = point;
                    }
                }
            }
        };
        try { walk(shown, viaLink == null ? Transform.Identity : toHost); }
        catch (Exception ex) { problems.Add("Element " + idText + inLink + "'s geometry could not be read: " + ex.Message); return null; }
        if (best == null)
        {
            problems.Add("Element " + idText + inLink + " has no flat face toward the " + side + " in '" + view.Name + "'.");
            return null;
        }
        var off = Math.Acos(Math.Min(1.0, bestDot)) * 180.0 / Math.PI;
        if (off > 1.0)
            findings.Add(string.Format("The {0} face of element {1} is {2} degrees off square to '{3}'.", side, idText + inLink,
                Math.Round(off, 1).ToString(System.Globalization.CultureInfo.InvariantCulture), view.Name));
        lastNormal = bestNormal;
        lastPoint = bestPoint;
        return best;
    }
    if (word == "link")
    {
        // `link LINK WORD ...` - LINK is the placed link's id or unique id, or
        // the linked model's name, which may hold spaces: the longest run of
        // words before one of the seven words that names a link is the link.
        if (viaLink != null)
        {
            problems.Add("\"" + text + "\" asks for a link inside link '" + fileOf(viaLink) + "'. Revit carries a "
                + "reference across one link to this model, not two - open the model that places that link to dimension in it.");
            return null;
        }
        var placedLinks = links();
        Func<string, List<RevitLinkInstance>> namedLinks = said =>
        {
            var wanted = (said ?? "").Trim();
            if (wanted.Length == 0) return new List<RevitLinkInstance>();
            var bare = wanted.EndsWith(".rvt", StringComparison.OrdinalIgnoreCase) ? wanted.Substring(0, wanted.Length - 4) : wanted;
            var exact = placedLinks.Where(l => l.Id.ToString() == wanted
                || string.Equals(l.UniqueId, wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(l.Name, wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileOf(l), wanted, StringComparison.OrdinalIgnoreCase)
                || string.Equals(fileOf(l), bare + ".rvt", StringComparison.OrdinalIgnoreCase)).ToList();
            return exact.Count > 0 ? exact
                : placedLinks.Where(l => (l.Name ?? "").IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        };
        // `link` is among the words that END a link's name, so a link named
        // twice reaches the refusal for a link inside a link, below.
        var wordsInside = new[] { "grid", "level", "plane", "line", "wall", "family", "face", "link" };
        string linkText = null, inner = null;
        var matched = new List<RevitLinkInstance>();
        if (rest.StartsWith("\""))
        {
            var close = rest.IndexOf('"', 1);
            if (close > 0)
            {
                linkText = rest.Substring(1, close - 1);
                inner = rest.Substring(close + 1).Trim();
                matched = namedLinks(linkText);
            }
        }
        else
        {
            var parts = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (var at = parts.Length - 1; at >= 1 && linkText == null; at--)
            {
                if (!wordsInside.Contains(parts[at].ToLowerInvariant())) continue;
                var candidate = string.Join(" ", parts, 0, at);
                var found = namedLinks(candidate);
                if (found.Count == 0) continue;
                linkText = candidate;
                inner = string.Join(" ", parts, at, parts.Length - at);
                matched = found;
            }
        }
        var listed = placedLinks.Count == 0 ? "" : " This model's links: " + string.Join(", ", placedLinks.Select(l => linkSaid(l))) + ".";
        if (placedLinks.Count == 0)
        {
            problems.Add("This model places no Revit link, so \"" + text + "\" has nothing to look into.");
            return null;
        }
        // A LINK NAMED WITH NOTHING KNOWN AFTER IT - "link 6347789", or a word
        // this does not take - is said to be that, not a link that is not there.
        var bare = matched.Count > 0 ? matched : namedLinks(rest.Trim('"'));
        if (bare.Count != 1 && matched.Count == 0 && rest.Length > 0) bare = namedLinks(rest.Split(' ')[0].Trim('"'));
        if (matched.Count == 0 && bare.Count == 1)
        {
            problems.Add("Say what inside link " + linkSaid(bare[0]) + " to dimension to - \"" + text + "\" names no grid, "
                + "level, plane, line, wall, family or face after it: \"link " + bare[0].Id + " face ID left\".");
            return null;
        }
        if (matched.Count == 0)
        {
            problems.Add("\"" + text + "\" names no link this model places. Say the link by its id or its model's name, then "
                + "what inside it: \"link " + placedLinks[0].Id + " face ID left\"." + listed);
            return null;
        }
        if (matched.Count > 1)
        {
            problems.Add("\"" + linkText + "\" names " + matched.Count + " placed links - " + string.Join(", ",
                matched.Select(l => "'" + (l.Name ?? "") + "' (id " + l.Id + ")")) + ". Each is a right answer, so say the one "
                + "meant by its id: \"link " + matched[0].Id + " " + inner + "\".");
            return null;
        }
        var linkInstance = matched[0];
        var linked = linkInstance.GetLinkDocument();
        if (linked == null)
        {
            problems.Add("Link " + linkCalled(linkInstance) + " is placed in this model but NOT LOADED, so nothing inside it "
                + "can be dimensioned. Reload it in Manage Links and ask again." + listed);
            return null;
        }
        var hidden = false;
        try { hidden = linkInstance.IsHidden(view) || view.GetCategoryHidden(linkInstance.Category.Id); } catch (Exception) { hidden = false; }
        if (hidden)
        {
            problems.Add("Link " + linkSaid(linkInstance) + " is hidden in '" + view.Name + "', and a dimension can only "
                + "measure what the view shows.");
            return null;
        }
        if (string.IsNullOrEmpty(inner) || !wordsInside.Contains(inner.Split(' ')[0].ToLowerInvariant()))
        {
            problems.Add("Say what inside link " + linkSaid(linkInstance) + " to dimension to: \"link " + linkInstance.Id
                + " face ID left\", or grid, level, plane, line, wall or family after the link, as in this model.");
            return null;
        }

        // THE WORD, READ INSIDE THE LINK, then carried across. Everything it
        // reads is the link's; every direction and point it leaves is carried
        // into this model by the link's placement.
        Reference inside = null;
        source = linked;
        viaLink = linkInstance;
        toHost = linkInstance.GetTotalTransform();
        inLink = " in link '" + fileOf(linkInstance) + "'";
        linkWords = "link " + linkInstance.Id + " ";
        try { inside = referenceOf(inner); }
        finally
        {
            source = doc;
            viaLink = null;
            toHost = Transform.Identity;
            inLink = "";
            linkWords = "";
        }
        if (inside == null) return null;
        Reference carried = null;
        var why = "";
        try { carried = inside.CreateLinkReference(linkInstance); }
        catch (Exception ex) { carried = null; why = " " + ex.Message; }
        if (carried == null)
        {
            problems.Add("Revit would not carry \"" + inner + "\" across link " + linkSaid(linkInstance) + " to this model."
                + why);
            return null;
        }

        // REVIT WILL NOT DIMENSION TO THE REFERENCE AS CreateLinkReference
        // HANDS IT BACK. Measured on Revit 2020 on 2026-10-15 (NEEDS-CHECKING
        // Group EH), in a plan with a view template, one without, and a
        // section. Written out it reads "<link>:RVTLINK/<link type>:<the
        // element>..." and NewDimension refuses it - "not geometric
        // references" for a family's faces and planes, "invalid number of
        // references" for a wall's faces. Written "<link>:0:RVTLINK:<the
        // element>..." and read back, the same reference is drawn and
        // measures what was built: a linked box's face 2000 mm from a grid,
        // its centre plane 2500, a linked wall 200 thick, a linked model line
        // 1500.
        // AND A REFERENCE TO A WHOLE ELEMENT names its first surface. A grid,
        // a level and a reference plane come across as the element alone -
        // "<link>:0:RVTLINK:<the element>" - and are still refused ("invalid
        // number of references"); with ":0:SURFACE" after them, a linked grid
        // measured 500 mm, a linked level 1600 and a linked plane 1000, each
        // where it was drawn. (":1:..." measured nonsense, -304.8 mm.)
        // Those two forms are rewritten, and any other is used as it comes.
        var written = "";
        try { written = carried.ConvertToStableRepresentation(doc) ?? ""; } catch (Exception) { written = ""; }
        var mark = written.IndexOf(":RVTLINK/", StringComparison.Ordinal);
        var resume = mark < 0 ? -1 : written.IndexOf(':', mark + ":RVTLINK/".Length);
        var form = resume > 0 ? written.Substring(0, mark) + ":0:RVTLINK" + written.Substring(resume) : written;
        var whole = form.LastIndexOf(":0:RVTLINK:", StringComparison.Ordinal);
        if (whole >= 0 && form.Substring(whole + ":0:RVTLINK:".Length).All(ch => char.IsDigit(ch) || ch == '-'))
            form += ":0:SURFACE";
        if (form != written)
        {
            Reference rewritten = null;
            try { rewritten = Reference.ParseFromStableRepresentation(doc, form); }
            catch (Exception) { rewritten = null; }
            if (rewritten != null) carried = rewritten;
        }
        linkedAsked++;
        return carried;
    }
    problems.Add("\"" + text + "\" is not something this can dimension to. Say grid NAME, level NAME, plane NAME, "
        + "line ID, wall ID exterior|interior, family ID Left|Right|Top|Bottom|Front|Back|a plane's name, "
        + "or face ID left|right|top|bottom|near|far - and any of them inside a loaded link with the link first: "
        + "link ID|NAME face ID left.");
    return null;
};

if (view == null)
{
    refused = "No view was given, and a dimension lives on one view.";
}
else if (view.IsTemplate)
{
    refused = string.Format("'{0}' is a view TEMPLATE, not a view - a template "
        + "carries settings and holds no annotation.", view.Name);
}
else if (doc.IsFamilyDocument)
{
    refused = "The document in front is a family open in the Family Editor. A family's dimensions are "
        + "LABEL_FAMILY_DIMENSION's; this draws one in a project view.";
}
else if (lineFrom == null || lineTo == null)
{
    refused = "The line the dimension sits on needs two different points, in millimetres - \"0,-1000,0\" and "
        + "\"6000,-1000,0\" - or one point given twice for the string to run through.";
}
else
{
    var tokens = (between ?? "").Split('|').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
    var array = new ReferenceArray();
    var normals = new List<XYZ>();
    var stations = new List<XYZ>();
    foreach (var token in tokens)
    {
        var reference = referenceOf(token);
        if (reference != null) { array.Append(reference); normals.Add(lastNormal); stations.Add(lastPoint); }
    }
    if (tokens.Count < 2)
        problems.Add(string.Format("A dimension measures BETWEEN things and {0} was given - two is the minimum, "
            + "and three or more make a chained string.", tokens.Count));

    // ONE POINT GIVEN TWICE: the string runs through it, square to what it
    // measures. The first reference whose direction can be read sets it, flat
    // in the view; the point is moved into the view's own plane.
    Line placedOn = null;
    if (problems.Count == 0 && lineFrom.DistanceTo(lineTo) < 0.01)
    {
        var facing = view.ViewDirection.Normalize();
        XYZ across = null;
        foreach (var normal in normals)
        {
            if (normal == null) continue;
            var flat = normal - facing * normal.DotProduct(facing);
            if (flat.GetLength() > 1e-6) { across = flat.Normalize(); break; }
        }
        if (across == null)
        {
            problems.Add("The same point was given twice, which asks for the string to run through it square to what "
                + "it measures - and none of these references says which way that is in '" + view.Name + "' (a "
                + "reference plane known only by its name, or one seen edge-on). Give two different points.");
        }
        else
        {
            var through = lineFrom - facing * (lineFrom - view.Origin).DotProduct(facing);
            double low = -1.0, high = 1.0;
            foreach (var station in stations)
            {
                if (station == null) continue;
                var along = (station - through).DotProduct(across);
                low = Math.Min(low, along - 1.0);
                high = Math.Max(high, along + 1.0);
            }
            placedOn = Line.CreateBound(through + across * low, through + across * high);
            findings.Add(string.Format("The string runs through {0},{1},{2} mm in '{3}', square to what it measures.",
                mm(through.X), mm(through.Y), mm(through.Z), view.Name));
        }
    }

    if (problems.Count > 0)
    {
        refused = "Nothing was drawn. " + string.Join(" ", problems);
    }
    else
    {
        Dimension made = null;
        try
        {
            made = doc.Create.NewDimension(view, placedOn ?? Line.CreateBound(lineFrom, lineTo), array);
        }
        catch (Exception ex)
        {
            refused = "Revit refused the dimension: " + ex.Message + " The usual causes are a reference the view "
                + "does not show, or references that are not parallel to each other and across the line.";
        }
        if (refused == null && made == null)
            refused = "Revit returned no dimension. The usual cause is a reference that is not visible in this "
                + "view - a dimension can only measure what the view shows.";
        if (refused == null)
        {
            doc.Regenerate();
            // READ BACK: one value for a single dimension, one per segment for a string.
            var values = new List<string>();
            if (made.NumberOfSegments > 1)
            {
                foreach (DimensionSegment segment in made.Segments)
                    values.Add(segment.Value.HasValue ? mm(segment.Value.Value) + " mm" : "(no value)");
            }
            else if (made.Value.HasValue)
            {
                values.Add(mm(made.Value.Value) + " mm");
            }
            if (made.References.Size != array.Size || values.Count == 0)
                throw new InvalidOperationException("The dimension does not read back as asked: " + made.References.Size
                    + " reference(s) of " + array.Size + ", " + values.Count + " value(s). The call failed, and Heron "
                    + "rolls the whole call back.");
            created = made.Id;
            measured = string.Join(" + ", values);
            findings.Add(string.Format("Dimension drawn on '{0}' across {1} reference(s), reading {2} - read back.",
                view.Name, array.Size, measured));
            // AND WHICH OF THEM REVIT HOLDS INSIDE A LINK - a reference carried
            // across names the linked element, read back off the dimension.
            if (linkedAsked > 0)
            {
                var linkedBack = 0;
                foreach (Reference held in made.References)
                    if (held.LinkedElementId != ElementId.InvalidElementId) linkedBack++;
                findings.Add(string.Format("{0} of its references were asked for inside a link, and Revit reads back {1} "
                    + "as an element of a linked model.", linkedAsked, linkedBack));
            }
        }
    }
}

if (refused != null) findings.Add(refused);
