// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `tagType`, `spotType`,
// `tagAt`, `spotsAt` and `face` are in scope, and leaves `tagged`, `newTags`,
// `newSpots`, `alreadyTagged`, `notAnnotated`, `typesLoaded`, `report`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One plan of beam annotations is
// one undo entry, and the caller owns it; nothing here opens or commits one.
//
// REVIT'S ANNOTATE > BEAM ANNOTATIONS: on each beam, in one go, a beam tag at
// its start, middle or end, and spot elevations on its top or bottom at one or
// both ends. IndependentTag.Create(Document, ElementId symId, ElementId
// ownerDBViewId, Reference, bool addLeader, TagOrientation, XYZ) and
// doc.Create.NewSpotElevation(View, Reference, XYZ origin, XYZ bend, XYZ end,
// XYZ refPt, bool hasLeader) - the same signatures 2020 to 2027
// (tools/api-surface --members, 2026-11-02).
//
// THE FACE IS TAKEN THE WAY ADD_SPOT_ELEVATIONS TAKES IT - Options with
// ComputeReferences and Fine detail, every planar face with a reference
// collected first, never first-one-wins - and chosen by a POSITIVE test on its
// normal: up for the top, down for the bottom. Math.Abs would let the soffit
// through for a top (ADD_SPOT_ELEVATIONS' -300 mm lesson). At each end the
// face directly over (or under) the beam's line there is used, the highest top
// or the lowest bottom - a steel beam's top flange, not its web.
//
// AN END IS NOT THE VERY END. A spot or a tag at the point where the beam meets
// its column lands on the column. Every "start" and "end" here is 5% of the
// beam's length in from that end, never more than 300 mm.
//
// PLANS ONLY - floor, structural and reflected ceiling plans. Revit's own Beam
// Annotations works in a plan; any other view is refused BY NAME and nothing is
// placed.
//
// A BEAM ALREADY TAGGED IN THIS VIEW IS LEFT ALONE - no tag and no spot - and
// named, so running it twice stacks nothing.
//
// THE TYPES ARE THE MODELLER'S OR THE PROJECT'S DEFAULT, AND THE ANSWER SAYS
// WHICH. A type named that the model has not got: NOTHING IS PLACED and the
// loaded types are listed. No default and none named: the same. Never the
// first one loaded (D-33).
//
// EVERYTHING PLACED IS READ BACK - a tag's type (IndependentTag.Create does not
// reliably honour the type passed to it - TAG_ELEMENTS_IN_VIEW measured it),
// a spot's type, view and the value Revit shows. One that reads back wrong is
// removed again inside the same undo.

const double MillimetresPerFoot = 304.8;
const double OneMillimetre = 1.0 / MillimetresPerFoot;

var tagged = 0;
var newTags = new List<ElementId>();
var newSpots = new List<ElementId>();
var alreadyTagged = new List<ElementId>();
var notAnnotated = new List<ElementId>();
var typesLoaded = new List<string>();
var lines = new List<string>();
var findings = new List<string>();
string refused = "";

var culture = System.Globalization.CultureInfo.InvariantCulture;
var framingCategory = new ElementId(BuiltInCategory.OST_StructuralFraming);
var framingTagCategory = new ElementId(BuiltInCategory.OST_StructuralFramingTags);
var multiTagCategory = new ElementId(BuiltInCategory.OST_MultiCategoryTags);
var spotCategory = new ElementId(BuiltInCategory.OST_SpotElevations);

Func<double, string> mm = v => Math.Round(v * MillimetresPerFoot, 1).ToString("0.#", culture);

// How a type reads in the Properties palette - "Structural Framing Tag: Standard".
Func<ElementType, string> typeLabel = t =>
{
    if (t == null) return "(no type)";
    string family = null;
    try { family = t.FamilyName; } catch { family = null; }
    return string.IsNullOrEmpty(family) ? t.Name : family + ": " + t.Name;
};

// A type is named as Revit shows it: its own name, or "Family: Name".
Func<ElementType, string, bool> isNamed = (t, text) =>
{
    string own;
    try { own = t.Name; } catch { return false; }
    if (string.Equals(own, text, StringComparison.Ordinal)) return true;
    string family;
    try { family = t.FamilyName; } catch { return false; }
    if (string.IsNullOrEmpty(family)) return false;
    return string.Equals(family + ": " + own, text.Replace(" : ", ": "), StringComparison.Ordinal);
};

// ---- what was asked ---------------------------------------------------------

Func<string, string> said = s => (s ?? "").Trim().ToLowerInvariant().Replace("-", " ").Replace("_", " ");

var tagText = said(tagAt);
string tagWhere = null;
if (tagText.Length == 0 || tagText == "middle" || tagText == "mid" || tagText == "centre" || tagText == "center"
    || tagText == "mid span" || tagText == "midspan")
    tagWhere = "middle";
else if (tagText == "start" || tagText == "end") tagWhere = tagText;
else if (tagText == "none" || tagText == "no") tagWhere = "none";

var spotText = said(spotsAt);
string spotWhere = null;
if (spotText.Length == 0 || spotText == "both" || spotText == "ends" || spotText == "both ends") spotWhere = "both";
else if (spotText == "start" || spotText == "end") spotWhere = spotText;
else if (spotText == "none" || spotText == "no") spotWhere = "none";

var faceText = said(face);
string faceWhere = null;
if (faceText.Length == 0 || faceText == "top" || faceText == "top of steel" || faceText == "top of beam")
    faceWhere = "top";
else if (faceText == "bottom" || faceText == "bottom of steel" || faceText == "bottom of beam")
    faceWhere = "bottom";

var wantTag = tagWhere != null && tagWhere != "none";
var wantSpots = spotWhere != null && spotWhere != "none";

// ---- the types the model has -----------------------------------------------

var tagTypes = new FilteredElementCollector(doc)
    .OfClass(typeof(FamilySymbol))
    .Cast<FamilySymbol>()
    .Where(t => t.Category != null && (t.Category.Id == framingTagCategory || t.Category.Id == multiTagCategory))
    .OrderBy(t => typeLabel(t), StringComparer.Ordinal)
    .ToList();
var spotTypes = new FilteredElementCollector(doc)
    .OfClass(typeof(SpotDimensionType))
    .Cast<SpotDimensionType>()
    .Where(t =>
    {
        try { return t.StyleType == DimensionStyleType.SpotElevation; }
        catch { return false; }
    })
    .OrderBy(t => typeLabel(t), StringComparer.Ordinal)
    .ToList();
typesLoaded.Add("Structural framing and multi-category tag types: " + (tagTypes.Count == 0
    ? "none in this model - load a Structural Framing Tag family first (Insert > Load Family)"
    : string.Join(", ", tagTypes.Select(t => "'" + typeLabel(t) + "'").ToArray())));
typesLoaded.Add("Spot elevation types: " + (spotTypes.Count == 0
    ? "none in this model"
    : string.Join(", ", spotTypes.Select(t => "'" + typeLabel(t) + "'").ToArray())));

// ---- refused before anything is placed -------------------------------------

FamilySymbol chosenTag = null;
string tagHow = "";
SpotDimensionType chosenSpot = null;
string spotHow = "";

var tagName = (tagType ?? "").Trim();
var spotName = (spotType ?? "").Trim();
var noTagName = tagName.Length == 0 || tagName.Equals("none", StringComparison.OrdinalIgnoreCase);
var noSpotName = spotName.Length == 0 || spotName.Equals("none", StringComparison.OrdinalIgnoreCase);

if (view == null)
{
    refused = "No view was named. Beam annotations go in one plan; name it as the Project Browser shows it.";
}
else if (view.IsTemplate)
{
    refused = "\"" + view.Name + "\" is a view template, and a template holds no annotation. Name the plan itself.";
}
else if (view.ViewType != ViewType.FloorPlan && view.ViewType != ViewType.EngineeringPlan
    && view.ViewType != ViewType.CeilingPlan)
{
    refused = "\"" + view.Name + "\" is a " + view.ViewType + " view, and beam annotations go only in a plan - a "
            + "floor plan, a structural (framing) plan or a reflected ceiling plan. Name one of those.";
}
else if (tagWhere == null)
{
    refused = "'" + tagAt + "' is not a place along a beam this knows - say \"start\", \"middle\" or \"end\" for "
            + "the tag, or \"none\" for no tag.";
}
else if (spotWhere == null)
{
    refused = "'" + spotsAt + "' is not where spot elevations go on a beam - say \"both\" for both ends, \"start\", "
            + "\"end\", or \"none\" for no spot elevations.";
}
else if (faceWhere == null)
{
    refused = "'" + face + "' is not a face of a beam this knows - say \"top\" (top of steel) or \"bottom\".";
}
else if (!wantTag && !wantSpots)
{
    refused = "Neither a tag nor a spot elevation was asked for, so there is nothing to place.";
}

if (refused.Length == 0 && wantTag)
{
    if (noTagName)
    {
        ElementId defaultId = null;
        try { defaultId = doc.GetDefaultFamilyTypeId(framingTagCategory); } catch { defaultId = null; }
        chosenTag = defaultId == null || defaultId == ElementId.InvalidElementId
            ? null : doc.GetElement(defaultId) as FamilySymbol;
        if (chosenTag == null)
            refused = "No beam tag type was named and this project has no default Structural Framing Tag, so none is "
                    + "chosen for you. Name one - the loaded ones are in typesLoaded.";
        else
            tagHow = "the project's default structural framing tag";
    }
    else
    {
        var hits = tagTypes.Where(t => isNamed(t, tagName)).ToList();
        if (hits.Count == 1) { chosenTag = hits[0]; tagHow = "as named"; }
        else if (hits.Count > 1)
            refused = hits.Count + " tag types are called '" + tagName + "' - "
                    + string.Join(", ", hits.Select(t => "'" + typeLabel(t) + "'").ToArray())
                    + " - so the name does not say which. Name it with its family.";
        else
        {
            // A tag of ANOTHER category by that name is named as such: Revit places a
            // tag of the wrong category without a word, and it prints blank.
            var elsewhere = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Where(t => t.Category != null && isNamed(t, tagName))
                .Select(t => "'" + typeLabel(t) + "' (" + t.Category.Name + ")")
                .Distinct()
                .Take(3)
                .ToList();
            refused = elsewhere.Count > 0
                ? "'" + tagName + "' is " + string.Join(", ", elsewhere.ToArray()) + ", not a structural framing or "
                  + "multi-category tag, and would print blank on a beam. The loaded ones are in typesLoaded."
                : "No beam tag type is called '" + tagName + "' - the name has to match the Project Browser, "
                  + "capitals included. The loaded ones are in typesLoaded.";
        }
    }
}

if (refused.Length == 0 && wantSpots)
{
    if (noSpotName)
    {
        ElementId defaultId = null;
        try { defaultId = doc.GetDefaultElementTypeId(ElementTypeGroup.SpotElevationType); } catch { defaultId = null; }
        chosenSpot = defaultId == null || defaultId == ElementId.InvalidElementId
            ? null : doc.GetElement(defaultId) as SpotDimensionType;
        if (chosenSpot == null)
            refused = "No spot elevation type was named and this project has no default one, so none is chosen for "
                    + "you. Name one - the loaded ones are in typesLoaded.";
        else
            spotHow = "the project's default spot elevation type";
    }
    else
    {
        var hits = spotTypes.Where(t => isNamed(t, spotName)).ToList();
        if (hits.Count == 1) { chosenSpot = hits[0]; spotHow = "as named"; }
        else if (hits.Count > 1)
            refused = hits.Count + " spot elevation types are called '" + spotName + "' - "
                    + string.Join(", ", hits.Select(t => "'" + typeLabel(t) + "'").ToArray())
                    + " - so the name does not say which. Name it with its family.";
        else
        {
            var elsewhere = new FilteredElementCollector(doc)
                .OfClass(typeof(SpotDimensionType))
                .Cast<SpotDimensionType>()
                .Where(t => isNamed(t, spotName))
                .Select(t => "'" + typeLabel(t) + "' (" + t.StyleType + ")")
                .Distinct()
                .Take(3)
                .ToList();
            refused = elsewhere.Count > 0
                ? "'" + spotName + "' is " + string.Join(", ", elsewhere.ToArray()) + ", not a spot elevation type. "
                  + "The loaded ones are in typesLoaded."
                : "No spot elevation type is called '" + spotName + "' - the name has to match the Project Browser, "
                  + "capitals included. The loaded ones are in typesLoaded.";
        }
    }
}

var given = new List<Element>();
var seen = new HashSet<ElementId>();
foreach (var element in elements ?? new List<Element>())
    if (element != null && seen.Add(element.Id)) given.Add(element);

if (refused.Length == 0 && given.Count == 0)
    refused = "No beams were handed in. Select the beams, or ask for every beam in the plan.";

if (refused.Length > 0) refused += " Nothing was placed.";

if (refused.Length == 0)
{
    // Beams Revit draws in this plan - a beam not in it cannot be annotated here.
    var inView = new HashSet<ElementId>(new FilteredElementCollector(doc, view.Id)
        .OfCategory(BuiltInCategory.OST_StructuralFraming)
        .WhereElementIsNotElementType()
        .ToElementIds());

    // Beams already carrying a tag in this view.
    var hasTag = new HashSet<ElementId>();
    foreach (var existing in new FilteredElementCollector(doc, view.Id)
                                 .OfClass(typeof(IndependentTag))
                                 .Cast<IndependentTag>())
    {
#if REVIT2020 || REVIT2021
        var one = existing.TaggedLocalElementId;
        if (one != null) hasTag.Add(one);
#else
        foreach (var hostId in existing.GetTaggedLocalElementIds()) hasTag.Add(hostId);
#endif
    }

    // Spot elevations already in this view, so a re-run stacks nothing.
    var spotsThere = new List<XYZ>();
    foreach (var element in new FilteredElementCollector(doc, view.Id).OfClass(typeof(SpotDimension)))
    {
        var spot = element as SpotDimension;
        if (spot == null || spot.Category == null || spot.Category.Id != spotCategory) continue;
        try { if (spot.Origin != null) spotsThere.Add(spot.Origin); } catch { }
    }

    var options = new Options();
    options.ComputeReferences = true;
    options.DetailLevel = ViewDetailLevel.Fine;

    // EVERY planar face with a reference, from the beam and from inside its
    // family instance - collected before one is chosen.
    Func<Element, List<PlanarFace>> facesOf = element =>
    {
        var found = new List<PlanarFace>();
        GeometryElement geometry = null;
        try { geometry = element.get_Geometry(options); } catch { geometry = null; }
        if (geometry == null) return found;
        var solids = new List<Solid>();
        foreach (GeometryObject top in geometry)
        {
            var solid = top as Solid;
            if (solid != null) { solids.Add(solid); continue; }
            var nested = top as GeometryInstance;
            if (nested == null) continue;
            foreach (GeometryObject inner in nested.GetInstanceGeometry())
            {
                var innerSolid = inner as Solid;
                if (innerSolid != null) solids.Add(innerSolid);
            }
        }
        foreach (var solid in solids)
        {
            if (solid.Faces.Size == 0) continue;
            foreach (Face one in solid.Faces)
            {
                var planar = one as PlanarFace;
                if (planar == null || one.Reference == null) continue;
                found.Add(planar);
            }
        }
        return found;
    };

    var scale = view.Scale > 0 ? view.Scale : 100;
    var paperStep = 5.0 * scale / MillimetresPerFoot;   // 5 mm on the sheet
    var right = view.RightDirection;
    var up = view.UpDirection;
    var wantTop = faceWhere == "top";

    var corrections = 0;
    var tagRefusals = 0;
    var spotsAlready = 0;
    var spotRefusals = 0;
    var readWrong = 0;
    string firstRefusal = null;

    // What is said about each beam, in order; a spot's words are filled in after
    // the one regeneration that lets Revit work its value out.
    var beamLines = new List<KeyValuePair<string, List<string>>>();
    var pendingSpots = new List<Tuple<SpotDimension, List<string>, int, string, XYZ>>();
    var beamOfSpot = new List<Element>();
    var placedFor = new Dictionary<ElementId, int>();

    foreach (var element in given)
    {
        var name = element.Category != null && element.Category.Id == framingCategory
            ? "beam " + element.Name + " (id " + element.Id + ")"
            : (element.Category != null ? element.Category.Name : "element") + " " + element.Id;
        var pieces = new List<string>();
        beamLines.Add(new KeyValuePair<string, List<string>>(name, pieces));

        bool inThisModel = false;
        try { inThisModel = element.Document != null && element.Document.Equals(doc); } catch { }
        if (!inThisModel)
        {
            notAnnotated.Add(element.Id);
            pieces.Add("of another model - left alone");
            continue;
        }
        if (element.Category == null || element.Category.Id != framingCategory)
        {
            notAnnotated.Add(element.Id);
            pieces.Add("not structural framing - beam annotations go on beams only. Left alone");
            continue;
        }
        var beam = element as FamilyInstance;
        if (beam == null || beam.StructuralType != StructuralType.Beam)
        {
            notAnnotated.Add(element.Id);
            pieces.Add("structural framing but not a beam (" + (beam == null ? "no family instance" : beam.StructuralType.ToString())
                + ") - a brace is not given beam annotations. Left alone");
            continue;
        }
        if (!inView.Contains(element.Id))
        {
            notAnnotated.Add(element.Id);
            pieces.Add("not shown in \"" + view.Name + "\" - its view range or visibility hides it. Left alone");
            continue;
        }
        if (hasTag.Contains(element.Id))
        {
            alreadyTagged.Add(element.Id);
            pieces.Add("already carries a tag in \"" + view.Name + "\" - left alone, no tag and no spot elevation placed");
            continue;
        }
        var location = beam.Location as LocationCurve;
        var curve = location == null ? null : location.Curve;
        if (curve == null || curve.Length < OneMillimetre)
        {
            notAnnotated.Add(element.Id);
            pieces.Add("has no location line to measure a start, middle or end along. Left alone");
            continue;
        }

        var inset = Math.Min(0.05 * curve.Length, 300.0 / MillimetresPerFoot) / curve.Length;
        Func<string, XYZ> pointAt = where =>
            curve.Evaluate(where == "start" ? inset : where == "end" ? 1.0 - inset : 0.5, true);

        placedFor[element.Id] = 0;

        if (wantTag)
        {
            IndependentTag tag = null;
            try
            {
                tag = IndependentTag.Create(doc, chosenTag.Id, view.Id, new Reference(beam), false,
                    TagOrientation.Horizontal, pointAt(tagWhere));
            }
            catch (Exception failure)
            {
                if (firstRefusal == null) firstRefusal = failure.Message;
                tagRefusals++;
                pieces.Add("no tag - Revit said: " + failure.Message);
            }
            if (tag != null)
            {
                string wrong = null;
                try
                {
                    if (tag.GetTypeId() != chosenTag.Id) { tag.ChangeTypeId(chosenTag.Id); corrections++; }
                    if (tag.GetTypeId() != chosenTag.Id) wrong = "it carries another tag type";
                    else if (tag.OwnerViewId != view.Id) wrong = "it belongs to another view";
                }
                catch (Exception failure) { wrong = "it could not be read back - " + failure.Message; }
                if (wrong != null)
                {
                    try { doc.Delete(tag.Id); } catch { }
                    readWrong++;
                    pieces.Add("the tag read back wrong - " + wrong + " - and was removed");
                }
                else
                {
                    tagged++;
                    newTags.Add(tag.Id);
                    placedFor[element.Id]++;
                    pieces.Add("tag " + tag.Id + " at the " + tagWhere);
                }
            }
        }

        if (wantSpots)
        {
            var sideFaces = facesOf(beam)
                .Where(f => wantTop ? f.FaceNormal.Z > 0.7 : f.FaceNormal.Z < -0.7)
                .ToList();
            if (sideFaces.Count == 0)
            {
                pieces.Add("no " + faceWhere + " face Revit gives a reference for - no spot elevation (a beam sloped "
                    + "more than 45 degrees has none here)");
            }
            else
            {
                var ends = spotWhere == "both" ? new[] { "start", "end" } : new[] { spotWhere };
                foreach (var end in ends)
                {
                    var onLine = pointAt(end);
                    PlanarFace best = null;
                    XYZ bestPoint = null;
                    foreach (var candidate in sideFaces)
                    {
                        IntersectionResult hit = null;
                        try { hit = candidate.Project(onLine); } catch { hit = null; }
                        if (hit == null || !candidate.IsInside(hit.UVPoint)) continue;
                        var p = hit.XYZPoint;
                        if (Math.Abs(p.X - onLine.X) > paperStep || Math.Abs(p.Y - onLine.Y) > paperStep) continue;
                        if (best == null || (wantTop ? p.Z > bestPoint.Z : p.Z < bestPoint.Z)) { best = candidate; bestPoint = p; }
                    }
                    if (best == null)
                    {
                        pieces.Add("no spot elevation at the " + end + " - no " + faceWhere + " face lies over the beam's "
                            + "line there (its line may run along an edge: Y Justification)");
                        continue;
                    }
                    if (spotsThere.Any(q => q.DistanceTo(bestPoint) < OneMillimetre))
                    {
                        spotsAlready++;
                        pieces.Add("a spot elevation is already at the " + end + " - left alone");
                        continue;
                    }
                    SpotDimension made = null;
                    try
                    {
                        var bend = bestPoint.Add(up.Multiply(paperStep));
                        var tail = bend.Add(right.Multiply(paperStep));
                        made = doc.Create.NewSpotElevation(view, best.Reference, bestPoint, bend, tail, bestPoint, false);
                    }
                    catch (Exception failure)
                    {
                        if (firstRefusal == null) firstRefusal = failure.Message;
                        spotRefusals++;
                        pieces.Add("no spot elevation at the " + end + " - Revit said: " + failure.Message);
                        continue;
                    }
                    if (made == null)
                    {
                        spotRefusals++;
                        pieces.Add("no spot elevation at the " + end + " - Revit placed none");
                        continue;
                    }
                    spotsThere.Add(bestPoint);
                    // A placeholder, replaced once the value can be read.
                    pieces.Add("");
                    pendingSpots.Add(Tuple.Create(made, pieces, pieces.Count - 1, end, bestPoint));
                    beamOfSpot.Add(element);
                }
            }
        }
    }

    // ONE regeneration, then every spot read back: its type, its view, its value.
    if (pendingSpots.Count > 0)
    {
        foreach (var pending in pendingSpots)
        {
            try { if (pending.Item1.GetTypeId() != chosenSpot.Id) pending.Item1.SpotDimensionType = chosenSpot; } catch { }
        }
        try { doc.Regenerate(); } catch { }
    }
    for (var i = 0; i < pendingSpots.Count; i++)
    {
        var pending = pendingSpots[i];
        var made = pending.Item1;
        string wrong = null;
        string shows = null;
        try
        {
            if (made.GetTypeId() != chosenSpot.Id) wrong = "it carries another type";
            else if (made.OwnerViewId != view.Id) wrong = "it belongs to another view";
            else shows = made.ValueString;
        }
        catch (Exception failure) { wrong = "it could not be read back - " + failure.Message; }

        if (wrong != null)
        {
            try { doc.Delete(made.Id); } catch { }
            readWrong++;
            pending.Item2[pending.Item3] = "the spot elevation at the " + pending.Item4 + " read back wrong - " + wrong
                + " - and was removed";
            continue;
        }
        newSpots.Add(made.Id);
        placedFor[beamOfSpot[i].Id]++;
        var at = "on the " + faceWhere + " face at " + mm(pending.Item5.Z) + " mm from the internal origin";
        pending.Item2[pending.Item3] = string.IsNullOrWhiteSpace(shows)
            ? "spot elevation " + made.Id + " at the " + pending.Item4 + ", " + at + " - Revit handed back no value "
              + "for it; read it on the drawing"
            : "spot elevation " + made.Id + " at the " + pending.Item4 + " showing \""
              + shows.Replace("\r\n", " / ").Replace("\n", " / ").Replace("\r", " / ").Trim() + "\", " + at;
    }

    foreach (var entry in beamLines)
    {
        lines.Add(entry.Key + ": " + string.Join("; ", entry.Value.ToArray()) + ".");
    }
    foreach (var element in given)
    {
        int count;
        if (placedFor.TryGetValue(element.Id, out count) && count == 0) notAnnotated.Add(element.Id);
    }

    // THE WHOLE ANSWER AS ONE LINE - every count in words.
    findings.Add(given.Count + " element(s) handed in, in \"" + view.Name + "\": "
        + tagged + " beam tag(s) placed" + (wantTag ? " at the " + tagWhere + " as '" + typeLabel(chosenTag) + "' ("
            + tagHow + ")" : " (none asked for)") + "; "
        + newSpots.Count + " spot elevation(s) placed" + (wantSpots ? " at " + (spotWhere == "both" ? "both ends" : "the "
            + spotWhere) + ", on the " + faceWhere.ToUpperInvariant() + " face, as '" + typeLabel(chosenSpot) + "' ("
            + spotHow + ")" : " (none asked for)") + ". Not placed: "
        + alreadyTagged.Count + " beam(s) already tagged here, " + notAnnotated.Count + " element(s) given nothing, "
        + spotsAlready + " spot(s) already there, " + (tagRefusals + spotRefusals) + " refused by Revit, "
        + readWrong + " read back wrong and removed.");
    findings.Add("Starts and ends are 5% of each beam's length in from that end, never more than 300 mm, so nothing "
        + "lands on the column the beam meets.");
    if (corrections > 0)
        findings.Add(corrections + " tag(s) came out of Revit as another type and were changed to '" + typeLabel(chosenTag)
            + "' - IndependentTag.Create does not always honour the type it is given.");

    bool tagsHidden = false, spotsHidden = false;
    try { tagsHidden = view.GetCategoryHidden(framingTagCategory); } catch { }
    try { spotsHidden = view.GetCategoryHidden(spotCategory); } catch { }
    if (tagsHidden && tagged > 0)
        findings.Add("Structural Framing Tags are turned off in \"" + view.Name + "\" - its Visibility/Graphics or view "
            + "template - so the " + tagged + " tag(s) placed are in the model and not shown there.");
    if (spotsHidden && newSpots.Count > 0)
        findings.Add("Spot Elevations are turned off in \"" + view.Name + "\" - so the " + newSpots.Count
            + " spot(s) placed are in the model and not shown there.");
    if (newSpots.Count > 0)
        findings.Add("The value each spot shows is printed above ON PURPOSE: a wrong face gives a plausible level "
            + "and a clean count. Read them against the beam's own Properties before the drawing goes out.");
    if (firstRefusal != null)
        findings.Add("Revit's first refusal said: " + firstRefusal);
}
else
{
    lines.Add(refused);
}

var report = string.Join(" | ", lines);
findings.AddRange(lines);
