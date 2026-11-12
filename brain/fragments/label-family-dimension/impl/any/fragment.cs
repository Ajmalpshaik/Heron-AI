// NOT STANDALONE. Assumes `doc`, `fromPlane`, `toPlane`, `parameterName`,
// `centrePlane` and `pinFreeEnd` are in scope; leaves `labelled`, `freeEnd`,
// `pinnedEnd`, `equalised`, `distanceMm`, `angleDegrees`, `notAFamily`,
// `refused` and `findings` behind.
//
// VERSION 3 (2026-11-11, FRAGMENT-ISSUES 5b-1498) - AN ANGLE TOO. When the
// parameter named is an ANGLE (ParameterType.Angle before 2022,
// SpecTypeId.Angle through GetDataType from 2022, both read by reflection,
// newest first), the two planes are
// two reference planes that MEET - not parallel - and the label is an angular
// dimension, FamilyItemFactory.NewAngularDimension(View, Arc, Reference,
// Reference), on an arc centred where the two planes cross in a floor plan or
// elevation that looks along the line they meet on. The same safety order as
// a distance: the parameter must already hold the angle the planes make on
// one side or the other (checked before anything is made, a difference
// refuses the call with both numbers), one label per parameter and per pair
// of planes, read back after the regeneration - the label, the angle the
// dimension reads, and neither plane turned or moved. With a LENGTH parameter
// every line below runs exactly as version 2's did; only the refusal of a
// parameter that is neither now names the angle too.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// LABELLING MOVES THE PLANES TO THE PARAMETER'S VALUE. A new length reads zero,
// so a label put on first collapses the planes onto each other - reported from
// an earlier family build. The distance is therefore measured and compared with
// the parameter BEFORE anything is made, and a difference refuses the call.
//
// ONE LABEL PER PARAMETER. A second dimension driving the same parameter fights
// the first as soon as anything moves, so an existing label refuses the call.
//
// THE EQ IS MADE FIRST, THEN THE LABEL - both reported working in that order in
// an earlier family build - and it needs the centre plane already midway,
// because an EQ moves planes until the sides match.
//
// ANY REFUSAL FROM REVIT AFTER THE CHECKS THROWS, so the host rolls the whole
// call back: an EQ left without its label, or a label on the wrong planes, is
// worse than neither.
//
// VERSION 2 - WHICH END A FLEX WOULD MOVE IS SAID BEFORE THE LABEL IS MADE.
// A labelled dimension drives the distance between its two ends, and when the
// parameter changes Revit moves whichever end is least held. Seen 2026-10-22
// on Revit 2020 (session 78736, FRAGMENT-ISSUES 5b-1326): "Trap Depth" was
// labelled from a NEW plane "Floor Top" - unpinned, no lock, no dimension on
// it - to "Trap Top", which carried two labelled dimensions; the label read
// back fine, and the next SET_FAMILY_TYPE_VALUES 300 -> 400 moved FLOOR TOP up
// 100 mm and left Trap Top where it was, twice, until Floor Top was pinned.
// So each end is read: a level, a pinned plane, a plane that defines the
// origin, or one a labelled dimension, an EQ or a lock references, is HELD; a
// plane none of those touch is FREE, and a free end is named in `freeEnd` and
// the findings as the end a flex may move, with the pin route. Nothing is
// pinned on this fragment's own initiative (Article 7): `pinFreeEnd` asks for
// it, and the pin is read back after the regeneration like everything else.
// A LEVEL NOBODY PINNED is held only against a free plane: beside a pinned
// end it is the end a flex moves (2026-10-24, IU10, 5b-1332 - see looseLevel).

var findings = new List<string>();
var labelled = "";
var equalised = false;
var distanceMm = 0.0;
var notAFamily = false;
string refused = null;
var freeEnd = "";
var pinnedEnd = "";
// VERSION 3: the angle read off a labelled ANGULAR dimension, in degrees; 0
// for a distance.
var angleDegrees = 0.0;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;

Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;
var axisWords = new[] { "left-right", "front-back", "height" };

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

// EVERY PLANE THIS CAN DIMENSION TO: (element, reference, axis, position in
// feet, name). Reference planes by their own name; levels count as height
// planes, because a height is dimensioned from Ref. Level.
var datums = new List<Tuple<Element, Reference, int, double, string>>();
if (doc.IsFamilyDocument)
{
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
    {
        var at = axisOf(rp.Normal);
        if (at < 0) continue;
        datums.Add(Tuple.Create((Element)rp, rp.GetReference(), at, along(rp.GetPlane().Origin, at), ownName(rp)));
    }
    foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
        // The level's own PLANE reference - what a dimension or an alignment to
        // a level is made against - rather than a reference to the element.
        datums.Add(Tuple.Create((Element)level, level.GetPlaneReference(), 2, level.Elevation, level.Name));
}

Func<string, List<Tuple<Element, Reference, int, double, string>>> named = wanted =>
    datums.Where(d => d.Item5.Length > 0
                   && string.Equals(d.Item5, (wanted ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
          .ToList();

// TWO KINDS ARE THE SAME KIND WHATEVER VERSION THEIR IDS CARRY. A ForgeTypeId
// names its version - "...:length-2.0.0" - and plain equality may compare it,
// so a length a template made under an older version would read as some other
// kind. NameEquals compares the name alone, from 2021; before that a kind is
// an enum and plain equality is exact.
Func<object, object, bool> sameKind = (first, second) =>
{
    if (first == null || second == null) return false;
    var nameEquals = first.GetType().GetMethod("NameEquals", new[] { second.GetType() });
    if (nameEquals != null)
    {
        try { return (bool)nameEquals.Invoke(first, new[] { second }); }
        catch (Exception) { }
    }
    return first.Equals(second);
};

// Is this parameter a length? Read the way its own release allows.
Func<FamilyParameter, bool> isLength = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            var owner = revitAssembly.GetType(dbNamespace + ".SpecTypeId");
            var property = owner == null ? null : owner.GetProperty("Length", flags);
            var length = property == null ? null : property.GetValue(null);
            return sameKind(spec, length);
        }
        var old = definition.GetType().GetProperty("ParameterType");
        var value = old == null ? null : old.GetValue(definition);
        return value != null && value.ToString() == "Length";
    }
    catch (Exception)
    {
        return false;
    }
};

// VERSION 3: is this parameter an ANGLE? Read as isLength reads a length -
// SpecTypeId.Angle through GetDataType where the release has it, the
// ParameterType enum's "Angle" before (SET_FAMILY_TYPE_VALUES reads it the
// same way).
Func<FamilyParameter, bool> isAngle = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            var owner = revitAssembly.GetType(dbNamespace + ".SpecTypeId");
            var property = owner == null ? null : owner.GetProperty("Angle", flags);
            var angleSpec = property == null ? null : property.GetValue(null);
            return sameKind(spec, angleSpec);
        }
        var old = definition.GetType().GetProperty("ParameterType");
        var value = old == null ? null : old.GetValue(definition);
        return value != null && value.ToString() == "Angle";
    }
    catch (Exception)
    {
        return false;
    }
};

Tuple<Element, Reference, int, double, string> a = null, b = null, centre = null;
FamilyParameter parameter = null;
View view = null;

// VERSION 3 - AN ANGLE: the two planes, the angle the parameter holds (in
// radians, Revit's own unit for an angle) and how close a reading must be.
var angular = false;
ReferencePlane angleFrom = null, angleTo = null;
var angleTarget = 0.0;
var angleTolerance = 0.01 * Math.PI / 180.0;
Func<double, string> deg = radians => Math.Round(radians * 180.0 / Math.PI, 3).ToString(invariant);

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. A family "
        + "dimension is labelled inside the family - open it first.";
}
else
{
    var fm = doc.FamilyManager;
    var problems = new List<string>();

    Func<string, string, Tuple<Element, Reference, int, double, string>> one = (wanted, role) =>
    {
        var found = named(wanted);
        if ((wanted ?? "").Trim().Length == 0) { problems.Add("No " + role + " was named."); return null; }
        if (found.Count == 0)
        {
            var offered = datums.Where(d => d.Item5.Length > 0).Select(d => d.Item5).Distinct()
                .OrderBy(n => n).Take(15).ToList();
            problems.Add("No reference plane or level is called \"" + wanted.Trim() + "\". This family has "
                + (offered.Count == 0 ? "no named ones." : string.Join(", ", offered) + "."));
            return null;
        }
        if (found.Count > 1) { problems.Add("\"" + wanted.Trim() + "\" names " + found.Count + " planes - rename one first."); return null; }
        return found[0];
    };

    // VERSION 3 - AN ANGLE. Decided by the parameter's kind alone, so a call
    // with a length takes version 2's path below untouched.
    var earlyName = parameterName == null ? "" : parameterName.Trim();
    FamilyParameter early = null;
    try { early = earlyName.Length == 0 ? null : fm.get_Parameter(earlyName); } catch (Exception) { early = null; }
    angular = early != null && isAngle(early);

    if (!angular)
    {
        a = one(fromPlane, "first plane");
        b = one(toPlane, "second plane");
        var wantsCentre = !string.IsNullOrEmpty(centrePlane) && centrePlane.Trim().Length > 0;
        if (wantsCentre) centre = one(centrePlane, "centre plane");

        var wantedParameter = parameterName == null ? "" : parameterName.Trim();
        parameter = wantedParameter.Length == 0 ? null : fm.get_Parameter(wantedParameter);

        if (wantedParameter.Length == 0) problems.Add("No parameter was named to label the dimension with.");
        else if (parameter == null) problems.Add("\"" + wantedParameter + "\" is not a parameter of this family - ADD_FAMILY_PARAMETERS makes one.");
        else if (!isLength(parameter)) problems.Add("\"" + wantedParameter + "\" is not a length, and only a length can label a straight dimension"
            + " - or an angle, between two planes that meet (version 3).");
        else if (parameter.IsReporting) problems.Add("\"" + wantedParameter + "\" is a reporting parameter - it would read the distance, not drive it.");

        if (problems.Count == 0)
        {
            if (a.Item1.Id == b.Item1.Id) problems.Add("Both ends are \"" + a.Item5 + "\" - a dimension needs two planes.");
            else if (a.Item3 != b.Item3) problems.Add("\"" + a.Item5 + "\" controls " + axisWords[a.Item3] + " and \"" + b.Item5 + "\" controls " + axisWords[b.Item3] + " - they are not parallel.");
            else if (Math.Abs(a.Item4 - b.Item4) < halfMillimetre) problems.Add("\"" + a.Item5 + "\" and \"" + b.Item5
                + "\" are in the same place - there is no distance to label. Pin the one that must stay (SET_PIN_STATE, "
                + "pinned=true), move the other to where it belongs, set \"" + (parameter == null ? wantedParameter : parameter.Definition.Name)
                + "\" to that distance (SET_FAMILY_TYPE_VALUES), then label - a label put on first would drive the free one.");
        }

        if (problems.Count == 0 && fm.CurrentType == null)
            problems.Add("This family has no type yet, so \"" + parameter.Definition.Name + "\" holds no value "
                + "and a label would drive the planes to nothing. SET_FAMILY_TYPE_VALUES makes the first type.");

        if (problems.Count == 0)
        {
            // THE GUARD THIS FRAGMENT EXISTS AROUND: the parameter must already say
            // what the planes say.
            var apart = Math.Abs(a.Item4 - b.Item4);
            var holds = fm.CurrentType.AsDouble(parameter);
            if (!holds.HasValue || Math.Abs(holds.Value - apart) > halfMillimetre)
                problems.Add("\"" + a.Item5 + "\" and \"" + b.Item5 + "\" are " + mm(apart) + " mm apart and \""
                    + parameter.Definition.Name + "\" holds " + (holds.HasValue ? mm(holds.Value) + " mm" : "no value")
                    + " in type \"" + fm.CurrentType.Name + "\". Labelling now would MOVE the planes to that "
                    + "value. Set it to " + mm(apart) + " first with SET_FAMILY_TYPE_VALUES, or name the planes "
                    + "that are that far apart.");

            foreach (var d in new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>())
            {
                FamilyParameter label = null;
                try { label = d.FamilyLabel; } catch (Exception) { }
                if (label == null) continue;

                if (label.Id == parameter.Id)
                {
                    problems.Add("\"" + parameter.Definition.Name + "\" already labels a dimension in this family. "
                        + "A second label on it would fight the first as soon as anything moves.");
                    break;
                }

                // ONE LABEL PER PAIR OF PLANES TOO. Two parameters each driving the
                // same two planes agree only while their values do; the first flex
                // that changes one of them is a constraint Revit cannot satisfy.
                var measured = new List<ElementId>();
                try { foreach (Reference r in d.References) measured.Add(r.ElementId); } catch (Exception) { }
                if (measured.Contains(a.Item1.Id) && measured.Contains(b.Item1.Id))
                {
                    problems.Add("A dimension labelled \"" + label.Definition.Name + "\" already measures \"" + a.Item5
                        + "\" to \"" + b.Item5 + "\". A second label on the same two planes would fight it as soon as "
                        + "either parameter changes.");
                    break;
                }
            }
        }

        if (problems.Count == 0 && centre != null)
        {
            var lo = Math.Min(a.Item4, b.Item4);
            var hi = Math.Max(a.Item4, b.Item4);
            if (centre.Item3 != a.Item3)
                problems.Add("\"" + centre.Item5 + "\" controls " + axisWords[centre.Item3] + ", not " + axisWords[a.Item3] + " - it cannot centre these planes.");
            else if (Math.Abs(centre.Item4 - (lo + hi) / 2) > halfMillimetre)
                problems.Add("\"" + centre.Item5 + "\" is not midway between \"" + a.Item5 + "\" and \"" + b.Item5
                    + "\" (it is at " + mm(centre.Item4) + " mm, midway is " + mm((lo + hi) / 2) + " mm). An EQ "
                    + "would move the planes until it is, so nothing was made.");
        }

        if (problems.Count == 0)
        {
            var views = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
                .Where(v => !v.IsTemplate).ToList();
            view = a.Item3 == 2
                ? views.Where(v => v.ViewType == ViewType.Elevation)
                       .OrderBy(v => Math.Abs(v.ViewDirection.Y) > 0.9999 ? 0 : 1).FirstOrDefault()
                : views.FirstOrDefault(v => v.ViewType == ViewType.FloorPlan);
            if (view == null)
                problems.Add(a.Item3 == 2
                    ? "This family has no elevation view, and a height is dimensioned in one."
                    : "This family has no floor plan, and a left-right or front-back size is dimensioned in one.");
        }

    }
    else
    {
        parameter = early;

        // THE TWO PLANES, BY NAME. Any reference plane, square to the family
        // or not - an angle is between planes that are not. A level is not
        // taken: it lies flat, and an angle in this family is drawn between
        // two reference planes, one of them on the level if it must be.
        var everyPlane = new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>()
            .Where(x => ownName(x).Length > 0).ToList();
        Func<string, string, ReferencePlane> planeNamed = (said, which) =>
        {
            var text = (said ?? "").Trim();
            if (text.Length == 0) { problems.Add("No " + which + " was named."); return null; }
            var found = everyPlane.Where(x => string.Equals(ownName(x), text, StringComparison.OrdinalIgnoreCase)).ToList();
            if (found.Count == 1) return found[0];
            if (found.Count > 1) { problems.Add("\"" + text + "\" names " + found.Count + " planes - rename one first."); return null; }
            var isLevel = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>()
                .Any(x => string.Equals(x.Name, text, StringComparison.OrdinalIgnoreCase));
            problems.Add(isLevel
                ? "\"" + text + "\" is a level. An angle is labelled between two reference planes - draw one on the "
                  + "level (CREATE_REFERENCE_PLANES) and name that."
                : "No reference plane is called \"" + text + "\". This family has "
                  + (everyPlane.Count == 0 ? "no named ones." : string.Join(", ", everyPlane.Select(ownName).Distinct()
                      .OrderBy(n => n).Take(15)) + "."));
            return null;
        };
        angleFrom = planeNamed(fromPlane, "first plane");
        angleTo = planeNamed(toPlane, "second plane");

        if (!string.IsNullOrEmpty(centrePlane) && centrePlane.Trim().Length > 0)
            problems.Add("A centre plane keeps a DISTANCE equal either side with an EQ; \"" + earlyName + "\" is an "
                + "angle, and an angle has no EQ here. Leave the centre plane out.");
        if (parameter.IsReporting)
            problems.Add("\"" + earlyName + "\" is a reporting parameter - it would read the angle, not drive it.");

        XYZ fromNormal = null, toNormal = null, meetsAlong = null;
        if (problems.Count == 0)
        {
            fromNormal = angleFrom.Normal.Normalize();
            toNormal = angleTo.Normal.Normalize();
            var crossing = fromNormal.CrossProduct(toNormal);
            if (angleFrom.Id == angleTo.Id)
                problems.Add("Both ends are \"" + ownName(angleFrom) + "\" - an angle needs two planes.");
            else if (crossing.GetLength() < Math.Sin(angleTolerance))
                problems.Add("\"" + ownName(angleFrom) + "\" and \"" + ownName(angleTo) + "\" are parallel - they never "
                    + "meet, so they make no angle. A LENGTH parameter labels the distance between them.");
            else meetsAlong = crossing.Normalize();
        }

        // THE VIEW: a floor plan or an elevation that looks ALONG the line the
        // two planes meet on, so both are seen edge on, as lines. Two upright
        // planes meet on a vertical line and are labelled in the floor plan.
        if (problems.Count == 0)
        {
            var looking = new FilteredElementCollector(doc).OfClass(typeof(View)).Cast<View>()
                .Where(x => !x.IsTemplate && (x.ViewType == ViewType.FloorPlan || x.ViewType == ViewType.Elevation))
                .Where(x => Math.Abs(x.ViewDirection.Normalize().DotProduct(meetsAlong)) > 0.9999)
                .OrderBy(x => x.ViewType == ViewType.FloorPlan ? 0 : 1).ToList();
            view = looking.FirstOrDefault();
            if (view == null)
                problems.Add("\"" + ownName(angleFrom) + "\" and \"" + ownName(angleTo) + "\" meet on a line that no floor "
                    + "plan or elevation of this family looks along, so no view shows both edge on to draw the angle "
                    + "in. Two upright planes are labelled in the floor plan.");
        }

        if (problems.Count == 0 && fm.CurrentType == null)
            problems.Add("This family has no type yet, so \"" + earlyName + "\" holds no value and a label would turn "
                + "the planes to nothing. SET_FAMILY_TYPE_VALUES makes the first type.");

        // THE GUARD, FOR AN ANGLE: the parameter must already hold the angle
        // the planes make - on one side of where they cross or the other, the
        // angle or what it leaves of 180 degrees. Labelled first, Revit would
        // TURN a plane to the parameter's value.
        if (problems.Count == 0)
        {
            var acute = Math.Acos(Math.Min(1.0, Math.Abs(fromNormal.DotProduct(toNormal))));
            var obtuse = Math.PI - acute;
            var holdsAngle = fm.CurrentType.AsDouble(parameter);
            if (holdsAngle.HasValue && Math.Abs(holdsAngle.Value - acute) <= angleTolerance) angleTarget = acute;
            else if (holdsAngle.HasValue && Math.Abs(holdsAngle.Value - obtuse) <= angleTolerance) angleTarget = obtuse;
            else
                problems.Add("\"" + ownName(angleFrom) + "\" and \"" + ownName(angleTo) + "\" meet at " + deg(acute)
                    + " degrees (" + deg(obtuse) + " on the other side) and \"" + earlyName + "\" holds "
                    + (holdsAngle.HasValue ? deg(holdsAngle.Value) + " degrees" : "no value") + " in type \""
                    + fm.CurrentType.Name + "\". Labelling now would TURN a plane to that value. Set it to "
                    + deg(acute) + " or " + deg(obtuse) + " first with SET_FAMILY_TYPE_VALUES, or name the planes "
                    + "that make that angle.");
        }

        // ONE LABEL PER PARAMETER, AND PER PAIR OF PLANES - as for a distance.
        if (problems.Count == 0)
        {
            foreach (var labelledAlready in new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>())
            {
                FamilyParameter labelOn = null;
                try { labelOn = labelledAlready.FamilyLabel; } catch (Exception) { }
                if (labelOn == null) continue;
                if (labelOn.Id == parameter.Id)
                {
                    problems.Add("\"" + earlyName + "\" already labels a dimension in this family. A second label on "
                        + "it would fight the first as soon as anything moves.");
                    break;
                }
                var ends = new List<ElementId>();
                try { foreach (Reference r in labelledAlready.References) ends.Add(r.ElementId); } catch (Exception) { }
                if (ends.Contains(angleFrom.Id) && ends.Contains(angleTo.Id))
                {
                    problems.Add("A dimension labelled \"" + labelOn.Definition.Name + "\" already measures \""
                        + ownName(angleFrom) + "\" to \"" + ownName(angleTo) + "\". A second label on the same two "
                        + "planes would fight it as soon as either parameter changes.");
                    break;
                }
            }
        }
    }

    if (problems.Count > 0) refused = "Nothing was made. " + string.Join(" ", problems);
}

if (refused == null && !angular)
{
    var fm = doc.FamilyManager;
    var axis = a.Item3;
    var lo = Math.Min(a.Item4, b.Item4) - 0.5;
    var hi = Math.Max(a.Item4, b.Item4) + 0.5;
    var level = view.GenLevel;
    var z0 = level == null ? 0.0 : level.Elevation;

    // PLACED OUTSIDE: past the furthest plane of the axis the line is offset
    // along, so the dimension does not sit on top of the geometry.
    var right = view.RightDirection;
    var across = axis == 0 ? 1 : axis == 1 ? 0 : (Math.Abs(right.X) > 0.9 ? 0 : 1);
    var furthest = datums.Where(d => d.Item3 == across).Select(d => Math.Abs(d.Item4)).DefaultIfEmpty(0).Max();

    Func<double, Line> lineAt = margin =>
    {
        var offset = -(furthest + margin / 304.8);
        if (axis == 0) return Line.CreateBound(new XYZ(lo, offset, z0), new XYZ(hi, offset, z0));
        if (axis == 1) return Line.CreateBound(new XYZ(offset, lo, z0), new XYZ(offset, hi, z0));
        return Line.CreateBound(new XYZ(right.X * offset, right.Y * offset, lo),
                                new XYZ(right.X * offset, right.Y * offset, hi));
    };

    var first = a.Item4 <= b.Item4 ? a : b;
    var last = a.Item4 <= b.Item4 ? b : a;
    Dimension dimension;
    Dimension eq = null;

    // WHICH END A FLEX WOULD MOVE (version 2). What holds each end is read
    // BEFORE the label exists: the dimension about to be made would otherwise
    // count as holding both. Three grades:
    //
    //   HARD  a level, a pinned plane, or one that defines the origin - a flex
    //         never moves it
    //   SOFT  a labelled dimension drives it from elsewhere, or an EQ or a lock
    //         ties it to another plane or level - a flex moves it only through
    //         that chain, and Revit would sooner move an end it holds less
    //   FREE  none of those - Revit's first choice to move
    //
    // A lock or an EQ to GEOMETRY ties the geometry to the plane, not the plane
    // to anything (EXTRUDE_BETWEEN_PLANES leaves one on every plane it uses),
    // so only one whose other references are all planes or levels counts. A
    // lock or an EQ running between these two ends holds them to each other,
    // not either to anything: it is named, and not counted. A plain dimension
    // - unlabelled, unlocked, no EQ - holds nothing.
    var datumIds = new HashSet<ElementId>(datums.Select(d => d.Item1.Id));
    var holds = new Dictionary<ElementId, List<string>>();
    string betweenEnds = null;
    foreach (var d in new FilteredElementCollector(doc).OfClass(typeof(Dimension)).Cast<Dimension>())
    {
        FamilyParameter label = null;
        try { label = d.FamilyLabel; } catch (Exception) { }
        var locked = false;
        try { locked = d.IsLocked; } catch (Exception) { }
        var equal = false;
        try { equal = d.AreSegmentsEqual; } catch (Exception) { }
        if (label == null && !locked && !equal) continue;

        var refs = new List<ElementId>();
        try { foreach (Reference r in d.References) refs.Add(r.ElementId); } catch (Exception) { }
        if (refs.Contains(a.Item1.Id) && refs.Contains(b.Item1.Id))
        {
            // A labelled one was refused above; a lock or an EQ here holds the
            // two ends to each other.
            if (label == null) betweenEnds = equal ? "An EQ" : "A lock";
            continue;
        }
        if (label == null && refs.Any(id => !datumIds.Contains(id))) continue;

        var what = label != null ? "the dimension labelled \"" + label.Definition.Name + "\"" : equal ? "an EQ" : "a lock";
        foreach (var id in refs)
        {
            List<string> by;
            if (!holds.TryGetValue(id, out by)) holds[id] = by = new List<string>();
            if (!by.Contains(what)) by.Add(what);
        }
    }

    // A LEVEL IS HELD FAST AGAINST A FREE PLANE, NOT AGAINST A FIXED ONE.
    // Measured 2026-10-24 on Revit 2020 (session 83568, NEEDS-CHECKING IU10,
    // FRAGMENT-ISSUES 5b-1332): "Height P" labelled from Ref. Level - a level,
    // NOT pinned - to a PINNED plane at 250 mm, then flexed to 400: Revit moved
    // REF. LEVEL down 150 mm and the pinned plane stayed. The same level against
    // a free plane ("Height 2", 220 -> 500) stayed where it was and the plane
    // moved. So an unpinned level counts as held against a free end, and as the
    // end that moves against one held fast; a pinned level is held either way.
    Func<Tuple<Element, Reference, int, double, string>, bool> looseLevel = end =>
    {
        if (!(end.Item1 is Level)) return false;
        var isPinned = false;
        try { isPinned = end.Item1.Pinned; } catch (Exception) { }
        return !isPinned;
    };

    // What holds an end fast, in words - "" when nothing does.
    Func<Tuple<Element, Reference, int, double, string>, string> hardHold = end =>
    {
        if (end.Item1 is Level) return looseLevel(end) ? "a level" : "a pinned level";
        var why = new List<string>();
        var isPinned = false;
        try { isPinned = end.Item1.Pinned; } catch (Exception) { }
        if (isPinned) why.Add("pinned");
        Parameter origin = null;
        try { origin = end.Item1.get_Parameter(BuiltInParameter.DATUM_PLANE_DEFINES_ORIGIN); } catch (Exception) { }
        if (origin != null && origin.AsInteger() == 1) why.Add("defines the origin");
        return string.Join(" and ", why);
    };
    // What holds an end through a chain, in words - "" when nothing does.
    Func<Tuple<Element, Reference, int, double, string>, string> softHold = end =>
    {
        List<string> by;
        return holds.TryGetValue(end.Item1.Id, out by) ? "held by " + string.Join(" and ", by) : "";
    };
    Func<string, string, string> holdWords = (hard, soft) =>
        hard.Length > 0 && soft.Length > 0 ? hard + ", " + soft : hard.Length > 0 ? hard : soft.Length > 0 ? soft : "held by nothing";

    var aHard = hardHold(a);
    var aSoft = softHold(a);
    var bHard = hardHold(b);
    var bSoft = softHold(b);
    var aFree = aHard.Length == 0 && aSoft.Length == 0;
    var bFree = bHard.Length == 0 && bSoft.Length == 0;
    var drives = parameter.Definition.Name;
    var pinRoute = "Pin it (SET_PIN_STATE, pinned=true) or dimension it to a level";

    // The end pinFreeEnd may pin, and - when it may pin nothing - why.
    Tuple<Element, Reference, int, double, string> toPin = null;
    string noPinWhy = null;

    if (centre != null)
    {
        // THE EQ SETTLES IT: both ends move equally about the centre plane, so
        // neither is free on its own, and a pinned end would fight the EQ.
        findings.Add("With the EQ across \"" + centre.Item5 + "\", \"" + a.Item5 + "\" and \"" + b.Item5
            + "\" move equally about it when \"" + drives + "\" changes - no end is free to move on its own.");
        noPinWhy = "the EQ across \"" + centre.Item5 + "\" moves both ends equally, and a pinned end would fight it";
    }
    else if (aHard.Length > 0 && bHard.Length > 0 && looseLevel(a) != looseLevel(b))
    {
        // THE LEVEL MOVES (5b-1332): one end is a level nobody pinned, the
        // other is held fast - pinned, or the origin - so a flex moves the
        // level, and every form standing on it with it.
        var movingLevel = looseLevel(a) ? a : b;
        var fixedEnd = looseLevel(a) ? b : a;
        var fixedWhy = looseLevel(a) ? bHard : aHard;
        findings.Add("\"" + fixedEnd.Item5 + "\" is " + fixedWhy + " and \"" + movingLevel.Item5 + "\" is a level that is not "
            + "pinned, so a flex of \"" + drives + "\" MOVES \"" + movingLevel.Item5 + "\" - and every form standing on it - "
            + "and leaves \"" + fixedEnd.Item5 + "\" where it is (measured on Revit 2020). Unpin \"" + fixedEnd.Item5
            + "\" (SET_PIN_STATE, pinned=false) so the flex moves it instead, or label from a plane that is free.");
        noPinWhy = "\"" + movingLevel.Item5 + "\" is the end a flex would move, and pinning the level is not what pinFreeEnd "
            + "does - unpin \"" + fixedEnd.Item5 + "\" instead";
    }
    else if (aHard.Length > 0 && bHard.Length > 0 && looseLevel(a) && looseLevel(b))
    {
        findings.Add("Both ends are levels and neither is pinned - \"" + a.Item5 + "\" and \"" + b.Item5 + "\" - so "
            + "which one a flex of \"" + drives + "\" moves is Revit's choice. Pin the one that must stay "
            + "(SET_PIN_STATE, pinned=true).");
        noPinWhy = "both ends are levels, and pinning a level is not what pinFreeEnd does";
    }
    else if (aHard.Length > 0 && bHard.Length > 0)
    {
        findings.Add("Both ends are held fast - \"" + a.Item5 + "\" is " + aHard + "; \"" + b.Item5 + "\" is " + bHard
            + " - so a flex of \"" + drives + "\" has nothing it can move. Unpin the one meant to move "
            + "(SET_PIN_STATE, pinned=false), or label from a plane that can.");
        noPinWhy = "both ends are already held fast";
    }
    else if (aFree && bFree)
    {
        freeEnd = "Both ends are held by nothing - neither \"" + a.Item5 + "\" nor \"" + b.Item5 + "\" is pinned, a level "
            + "or an origin plane, and no labelled dimension, EQ or lock references either - so which one a flex of \""
            + drives + "\" moves is Revit's choice. Pin the one that must stay (SET_PIN_STATE, pinned=true) or dimension "
            + "it to a level; asked with pinFreeEnd=true, this call pins \"" + a.Item5 + "\", the first named.";
        toPin = a;
    }
    else if (aFree || bFree)
    {
        var loose = aFree ? a : b;
        var other = aFree ? b : a;
        var otherHard = aFree ? bHard : aHard;
        var otherSoft = aFree ? bSoft : aSoft;
        if (otherHard.Length > 0)
        {
            // THE ORDINARY HEIGHT: one end fixed, the other free to follow the
            // parameter. Nothing to warn of, and nothing to pin - pinned, the
            // flex would have nothing left to move.
            findings.Add("\"" + other.Item5 + "\" is " + otherHard + " and \"" + loose.Item5 + "\" is held by nothing, "
                + "so a flex of \"" + drives + "\" moves \"" + loose.Item5 + "\" - the ordinary case, as a height from "
                + "Ref. Level is.");
            noPinWhy = looseLevel(other)
                ? "\"" + other.Item5 + "\" is a level, so \"" + loose.Item5 + "\" is the end a flex moves - pinned, the "
                  + "flex would move the level instead (5b-1332)"
                : "\"" + other.Item5 + "\" is " + otherHard + ", so \"" + loose.Item5 + "\" is the only end a flex "
                  + "can move - pinned, the flex would have nothing";
        }
        else
        {
            // THE CASE OF 2026-10-22: the other end is held only through a
            // chain, so Revit moves the free one and the geometry stays.
            freeEnd = "\"" + loose.Item5 + "\" is held by nothing - not pinned, not a level, not an origin plane, and no "
                + "labelled dimension, EQ or lock references it - while \"" + other.Item5 + "\" is " + otherSoft + ". A flex "
                + "of \"" + drives + "\" may move \"" + loose.Item5 + "\" and leave \"" + other.Item5 + "\" where it is. "
                + pinRoute + "; asked with pinFreeEnd=true, this call pins it first.";
            toPin = loose;
        }
    }
    else
    {
        findings.Add("Each end is held: \"" + a.Item5 + "\" is " + holdWords(aHard, aSoft) + "; \"" + b.Item5 + "\" is "
            + holdWords(bHard, bSoft) + ".");
        noPinWhy = "both ends are already held";
    }

    if (betweenEnds != null)
        findings.Add(betweenEnds + " already runs between \"" + a.Item5 + "\" and \"" + b.Item5 + "\" and holds their "
            + "distance - the label will fight it at the first flex. Unlock or remove it first.");

    // THE PIN IS THE MODELLER'S TO ASK FOR (Article 7), and it goes on BEFORE
    // the label so that the first flex already moves the right end.
    Element pinnedPlane = null;
    if (pinFreeEnd && toPin != null)
    {
        pinnedPlane = toPin.Item1;
        try { pinnedPlane.Pinned = true; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not pin \"" + toPin.Item5 + "\": " + ex.Message
                + " NOTHING from this call was kept.");
        }
    }
    else if (pinFreeEnd)
        findings.Add("pinFreeEnd was asked and nothing was pinned: " + noPinWhy + ".");

    try
    {
        if (centre != null)
        {
            var three = new ReferenceArray();
            three.Append(first.Item2);
            three.Append(centre.Item2);
            three.Append(last.Item2);
            eq = doc.FamilyCreate.NewDimension(view, lineAt(300), three);
            eq.AreSegmentsEqual = true;
        }

        var two = new ReferenceArray();
        two.Append(first.Item2);
        two.Append(last.Item2);
        dimension = doc.FamilyCreate.NewDimension(view, lineAt(centre != null ? 600 : 300), two);
        dimension.FamilyLabel = parameter;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the dimension between \"" + a.Item5 + "\" and \""
            + b.Item5 + "\": " + ex.Message + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    // READ BACK, AFTER THE REGENERATION - the label, the EQ, the distance the
    // dimension reads, and where the planes are now. The guard above made the
    // parameter agree with the planes, so NOTHING should have moved; a plane
    // that did, or a reading that differs, is the collapse this exists to stop,
    // and it is thrown rather than reported.
    var readLabel = dimension.FamilyLabel;
    var reads = dimension.Value;
    distanceMm = reads.HasValue ? Math.Round(reads.Value * 304.8, 2) : 0.0;
    equalised = eq != null && eq.AreSegmentsEqual;

    Func<Element, int, double> positionNow = (element, index) =>
    {
        var plane = element as ReferencePlane;
        if (plane != null) return along(plane.GetPlane().Origin, index);
        var level = element as Level;
        return level != null ? level.Elevation : double.NaN;
    };

    if (readLabel == null || readLabel.Id != parameter.Id)
        throw new InvalidOperationException("The dimension between \"" + a.Item5 + "\" and \"" + b.Item5
            + "\" was made and does not carry \"" + parameter.Definition.Name + "\" afterwards. NOTHING from "
            + "this call was kept.");

    if (centre != null && !equalised)
        throw new InvalidOperationException("The EQ across \"" + first.Item5 + "\", \"" + centre.Item5
            + "\" and \"" + last.Item5 + "\" did not hold. NOTHING from this call was kept.");

    var apartBefore = Math.Abs(a.Item4 - b.Item4);
    if (!reads.HasValue || Math.Abs(reads.Value - apartBefore) > halfMillimetre)
        throw new InvalidOperationException("The labelled dimension reads "
            + (reads.HasValue ? mm(reads.Value) + " mm" : "nothing") + " where the planes were " + mm(apartBefore)
            + " mm apart. NOTHING from this call was kept.");

    foreach (var plane in centre != null ? new[] { a, b, centre } : new[] { a, b })
    {
        var now = positionNow(plane.Item1, axis);
        if (double.IsNaN(now) || Math.Abs(now - plane.Item4) > halfMillimetre)
            throw new InvalidOperationException("\"" + plane.Item5 + "\" moved from " + mm(plane.Item4) + " to "
                + mm(now) + " mm when the dimension was labelled. NOTHING from this call was kept.");
    }

    // THE PIN, READ BACK (version 2): asked for, set before the label, and
    // still there after Revit regenerated - or nothing is kept.
    if (pinnedPlane != null)
    {
        var stillPinned = false;
        try { stillPinned = pinnedPlane.Pinned; } catch (Exception) { }
        if (!stillPinned)
            throw new InvalidOperationException("\"" + toPin.Item5 + "\" was pinned before the label and does not read "
                + "back pinned afterwards. NOTHING from this call was kept.");
        var moves = toPin == a ? b : a;
        pinnedEnd = "\"" + toPin.Item5 + "\" pinned first, as asked (pinFreeEnd), and reads back pinned: a flex of \""
            + drives + "\" now leaves it where it is and moves \"" + moves.Item5 + "\" instead.";
        freeEnd += " Pinned now - see pinnedEnd.";
    }

    labelled = parameter.Definition.Name + " drives \"" + first.Item5 + "\" to \"" + last.Item5 + "\", "
        + "reading " + distanceMm.ToString(invariant) + " mm";

    findings.Add(labelled + ", in \"" + view.Name + "\"."
        + (centre != null
            ? " An EQ across \"" + first.Item5 + "\", \"" + centre.Item5 + "\" and \"" + last.Item5
              + "\" keeps them centred as it changes."
            : " With no centre plane, \"" + first.Item5 + "\" and \"" + last.Item5 + "\" can move "
              + "unequally - right for a height from Ref. Level, wrong for a box meant to stay centred."));

    findings.Add("The planes now follow \"" + parameter.Definition.Name + "\". Geometry follows only if it "
        + "is LOCKED to them - EXTRUDE_BETWEEN_PLANES builds a box that is. FLEX_FAMILY proves it by "
        + "changing the value and reading the geometry back.");

    if (freeEnd.Length > 0) findings.Add(freeEnd);
    if (pinnedEnd.Length > 0) findings.Add(pinnedEnd);
}

// ---------------------------------------------------------------------------
// VERSION 3 - THE ANGLE, MADE AND READ BACK
// ---------------------------------------------------------------------------

if (refused == null && angular)
{
    var fromName = ownName(angleFrom);
    var toName = ownName(angleTo);
    var drivesAngle = parameter.Definition.Name;
    var fromNormalBefore = angleFrom.Normal.Normalize();
    var toNormalBefore = angleTo.Normal.Normalize();
    var fromOriginBefore = angleFrom.GetPlane().Origin;
    var toOriginBefore = angleTo.GetPlane().Origin;
    var meetsAlong = fromNormalBefore.CrossProduct(toNormalBefore).Normalize();

    // WHERE THE TWO PLANES CROSS, in the view's own plane: the one point on
    // both planes and on the plane of the view - a floor plan at its level, an
    // elevation through its origin. The arc is centred there, because an arc
    // centred anywhere else reads a different angle with no error at all
    // (CREATE_ANGULAR_DIMENSION's lesson).
    var viewLevel = view.GenLevel;
    var onView = view.ViewType == ViewType.FloorPlan
        ? new XYZ(0, 0, viewLevel == null ? 0.0 : viewLevel.Elevation)
        : view.Origin;
    var across = fromNormalBefore.DotProduct(toNormalBefore.CrossProduct(meetsAlong));
    var meet = (toNormalBefore.CrossProduct(meetsAlong) * fromNormalBefore.DotProduct(fromOriginBefore)
              + meetsAlong.CrossProduct(fromNormalBefore) * toNormalBefore.DotProduct(toOriginBefore)
              + fromNormalBefore.CrossProduct(toNormalBefore) * meetsAlong.DotProduct(onView)) / across;

    // EACH PLANE AS SEEN IN THE VIEW, as an arm from where they cross towards
    // the middle of the plane as it is drawn - so the arc sits between the two
    // arms as they appear, not behind them.
    Func<ReferencePlane, XYZ, XYZ> armOf = (plane, normal) =>
    {
        var trace = meetsAlong.CrossProduct(normal).Normalize();
        var middle = (plane.BubbleEnd + plane.FreeEnd) / 2.0;
        return (middle - meet).DotProduct(trace) < -1e-6 ? trace.Negate() : trace;
    };
    Func<XYZ, XYZ, double> angleBetween = (one, two) =>
        Math.Acos(Math.Max(-1.0, Math.Min(1.0, one.DotProduct(two))));
    var fromArm = armOf(angleFrom, fromNormalBefore);
    var toArm = armOf(angleTo, toNormalBefore);
    // THE SIDE THE PARAMETER HOLDS. The two arms as drawn make the angle or
    // what it leaves of 180 degrees; when it is the other one, the arc goes
    // to the other side of the second plane.
    if (Math.Abs(angleBetween(fromArm, toArm) - angleTarget) > angleTolerance) toArm = toArm.Negate();
    var sweep = angleBetween(fromArm, toArm);
    var square = (toArm - fromArm * fromArm.DotProduct(toArm)).Normalize();

    // THE RADIUS: well inside the shorter arm as drawn, and 300 mm when an
    // arm is drawn short of where they cross or behind it.
    Func<ReferencePlane, XYZ, double> reach = (plane, arm) =>
        Math.Max((plane.BubbleEnd - meet).DotProduct(arm), (plane.FreeEnd - meet).DotProduct(arm));
    var shorter = Math.Min(reach(angleFrom, fromArm), reach(angleTo, toArm));
    var radius = shorter > 200.0 / 304.8 ? shorter * 0.6 : 300.0 / 304.8;
    // THE ARC TURNS COUNTER-CLOCKWISE AS THE VIEW SEES IT. Measured 2026-11-12
    // on Revit 2020 (Group NE, NE6, row 5b-1565): the 150 degree side drawn from the first
    // arm turned clockwise seen from above, and Revit read it as 330 - the
    // angle the other way round. So when the arc's normal faces away from the
    // viewer it is drawn from the second arm instead: the same arc, the other
    // way round, its normal towards the viewer.
    var startArm = fromArm;
    var towards = square;
    if (fromArm.CrossProduct(square).DotProduct(view.ViewDirection) < 0)
    {
        startArm = toArm;
        towards = (fromArm - toArm * toArm.DotProduct(fromArm)).Normalize();
    }
    var arc = Arc.Create(meet, radius, 0.0, sweep, startArm, towards);

    // WHICH PLANE A FLEX WOULD TURN. Read before the label exists, as for a
    // distance, but only what holds a plane FAST - pinned, or defining the
    // origin. Which plane Revit turns, and about which point, when the angle
    // changes has not been measured yet (NEEDS-CHECKING Group NE): it is said
    // as expected, never as known.
    Func<ReferencePlane, string> heldFast = plane =>
    {
        var why = new List<string>();
        var isPinned = false;
        try { isPinned = plane.Pinned; } catch (Exception) { }
        if (isPinned) why.Add("pinned");
        Parameter origin = null;
        try { origin = plane.get_Parameter(BuiltInParameter.DATUM_PLANE_DEFINES_ORIGIN); } catch (Exception) { }
        if (origin != null && origin.AsInteger() == 1) why.Add("defines the origin");
        return string.Join(" and ", why);
    };
    var fromFast = heldFast(angleFrom);
    var toFast = heldFast(angleTo);
    ReferencePlane pinAngleEnd = null;
    string noAnglePin = null;
    if (fromFast.Length > 0 && toFast.Length > 0)
    {
        findings.Add("Both planes are held fast - \"" + fromName + "\" is " + fromFast + "; \"" + toName + "\" is " + toFast
            + " - so a flex of \"" + drivesAngle + "\" has nothing it can turn. Unpin the one meant to turn "
            + "(SET_PIN_STATE, pinned=false).");
        noAnglePin = "both planes are already held fast";
    }
    else if (fromFast.Length > 0 || toFast.Length > 0)
    {
        var stays = fromFast.Length > 0 ? fromName : toName;
        var turns = fromFast.Length > 0 ? toName : fromName;
        findings.Add("\"" + stays + "\" is " + (fromFast.Length > 0 ? fromFast : toFast) + ", so a flex of \""
            + drivesAngle + "\" is expected to turn \"" + turns + "\" - expected, not yet measured in Revit.");
        noAnglePin = "\"" + stays + "\" is held fast already, so \"" + turns + "\" is the only plane a flex can turn - "
            + "pinned, the flex would have nothing";
    }
    else
    {
        freeEnd = "Neither \"" + fromName + "\" nor \"" + toName + "\" is pinned or defines the origin, so which one a "
            + "flex of \"" + drivesAngle + "\" turns is Revit's choice. Pin the one that must stay (SET_PIN_STATE, "
            + "pinned=true); asked with pinFreeEnd=true, this call pins \"" + fromName + "\", the first named.";
        pinAngleEnd = angleFrom;
    }

    if (pinFreeEnd && pinAngleEnd != null)
    {
        try { pinAngleEnd.Pinned = true; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not pin \"" + fromName + "\": " + ex.Message
                + " NOTHING from this call was kept.");
        }
    }
    else if (pinFreeEnd)
        findings.Add("pinFreeEnd was asked and nothing was pinned: " + noAnglePin + ".");

    Dimension angleDimension = null;
    try
    {
        angleDimension = doc.FamilyCreate.NewAngularDimension(view, arc, angleFrom.GetReference(), angleTo.GetReference());
        if (angleDimension == null) throw new InvalidOperationException("Revit returned no dimension.");
        angleDimension.FamilyLabel = parameter;
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit refused the angle between \"" + fromName + "\" and \"" + toName
            + "\" in \"" + view.Name + "\": " + ex.Message + " NOTHING from this call was kept.");
    }

    doc.Regenerate();

    // READ BACK, AFTER THE REGENERATION - the label, the angle the dimension
    // reads, and both planes where they were. The guard made the parameter
    // agree with the planes, so NOTHING should have turned; a plane that did
    // is thrown, not reported.
    var angleLabel = angleDimension.FamilyLabel;
    if (angleLabel == null || angleLabel.Id != parameter.Id)
        throw new InvalidOperationException("The angle between \"" + fromName + "\" and \"" + toName + "\" was made and "
            + "does not carry \"" + drivesAngle + "\" afterwards. NOTHING from this call was kept.");

    var angleReads = angleDimension.Value;
    if (!angleReads.HasValue || Math.Abs(angleReads.Value - angleTarget) > angleTolerance)
        throw new InvalidOperationException("The labelled angle reads " + (angleReads.HasValue
                ? deg(angleReads.Value) + " degrees" : "nothing") + " where \"" + drivesAngle + "\" holds "
            + deg(angleTarget) + ". NOTHING from this call was kept.");

    foreach (var check in new[] {
        Tuple.Create(angleFrom, fromName, fromNormalBefore, fromOriginBefore),
        Tuple.Create(angleTo, toName, toNormalBefore, toOriginBefore) })
    {
        var normalNow = check.Item1.Normal.Normalize();
        var originNow = check.Item1.GetPlane().Origin;
        var turned = angleBetween(normalNow, check.Item3);
        if (turned > angleTolerance && Math.PI - turned > angleTolerance)
            throw new InvalidOperationException("\"" + check.Item2 + "\" turned " + deg(turned) + " degrees when the "
                + "angle was labelled. NOTHING from this call was kept.");
        if (Math.Abs(normalNow.DotProduct(check.Item4 - originNow)) > halfMillimetre)
            throw new InvalidOperationException("\"" + check.Item2 + "\" moved when the angle was labelled. NOTHING "
                + "from this call was kept.");
    }

    if (pinFreeEnd && pinAngleEnd != null)
    {
        var stillPinned = false;
        try { stillPinned = pinAngleEnd.Pinned; } catch (Exception) { }
        if (!stillPinned)
            throw new InvalidOperationException("\"" + fromName + "\" was pinned before the label and does not read "
                + "back pinned afterwards. NOTHING from this call was kept.");
        pinnedEnd = "\"" + fromName + "\" pinned first, as asked (pinFreeEnd), and reads back pinned: a flex of \""
            + drivesAngle + "\" is expected to turn \"" + toName + "\" instead - expected, not yet measured.";
        freeEnd += " Pinned now - see pinnedEnd.";
    }

    angleDegrees = Math.Round(angleReads.Value * 180.0 / Math.PI, 3);
    labelled = drivesAngle + " drives the angle between \"" + fromName + "\" and \"" + toName + "\", reading "
        + angleDegrees.ToString(invariant) + " degrees";
    findings.Add(labelled + ", in \"" + view.Name + "\", on an arc centred where the two planes cross.");
    findings.Add("The planes now follow \"" + drivesAngle + "\". Geometry follows only if it is LOCKED to them - "
        + "LOCK_FAMILY_LINES_TO_PLANES (DRAFT, not yet proved) locks a line along a plane. FLEX_FAMILY proves it by "
        + "changing the angle and "
        + "reading the geometry back.");
    if (freeEnd.Length > 0) findings.Add(freeEnd);
    if (pinnedEnd.Length > 0) findings.Add(pinnedEnd);
}

if (refused != null) findings.Add(refused);
