// NOT STANDALONE. Assumes `doc`, `elements` and `sharedCoordinates` are in
// scope; leaves `locations`, `withoutLocation` and `findings` behind.
//
// A READ. It opens no transaction and needs none.
//
// THREE KINDS OF POSITION, AND THE REPORT SAYS WHICH. Equipment is at a POINT.
// A duct RUNS from one place to another and has no single position at all.
// Some elements have neither. Printing one set of coordinates for all three
// makes the second kind a lie: the midpoint of a thirty-metre run is a place
// the duct passes through, not where it is.
//
// THE BOUNDING-BOX CENTRE IS NAMED AS A FALLBACK EVERY TIME, never dressed up
// as a location - for anything L-shaped or sloped it is a point in fresh air.
//
// COORDINATES ARE MEASURED FROM THE PROJECT'S INTERNAL ORIGIN BY DEFAULT, which
// is not necessarily the survey point or the base point. Good for comparing
// elements with each other; not a coordinate to hand a surveyor.
//
// VERSION 3 - SHARED COORDINATES WHEN ASKED FOR. With `sharedCoordinates` set,
// every position is ALSO given as east / north / elevation in the model's
// shared coordinates, through the active project location. Which way the
// location's transform runs is NOT ASSUMED: it is tried both ways against the
// project base point and the survey point, whose own internal and shared
// positions Revit reports, and used only if one direction reproduces both.
// If neither does, the reply says so and gives internal numbers only - a
// shared coordinate that is wrong by a rotation is worse than none.
//
// WHICH NORTH. A facing direction is in PROJECT north - the internal Y axis -
// and the reply says so. With shared coordinates it is also turned into TRUE
// north, by the same transform.
//
// THE SITE IS ALWAYS REPORTED: the angle from project north to true north, and
// where the project base point and the survey point are, both internal and
// shared. It is what makes the internal numbers mean anything outside the
// model.
//
// DISTANCE FROM THE INTERNAL ORIGIN IS A NUMBER, NOT A VERDICT. The farthest
// position reported is given in metres. It is FLAGGED only beyond the limit
// Revit's own help states for geometry from the internal origin - 20 miles
// (about 32 km) - and that limit is named as Revit's, not Heron's.

const double MillimetresPerFoot = 304.8;
// Revit's own documented limit for model geometry from the internal origin:
// 20 miles, in feet. Not a Heron threshold - nothing nearer is called far.
const double RevitOriginLimitFeet = 20.0 * 5280.0;
// Half a millimetre in feet - the agreement asked of the transform check.
const double Tolerance = 0.0005 / 0.3048;

var locations = new List<string>();
var withoutLocation = 0;
var findings = new List<string>();

Func<XYZ, string> asMm = p => string.Format("({0:0}, {1:0}, {2:0}) mm",
    p.X * MillimetresPerFoot, p.Y * MillimetresPerFoot, p.Z * MillimetresPerFoot);
Func<XYZ, string> asShared = p => string.Format("E {0:0}, N {1:0}, elev {2:0} mm",
    p.X * MillimetresPerFoot, p.Y * MillimetresPerFoot, p.Z * MillimetresPerFoot);

// ---- The site: true north, base point, survey point, and the transform ------

BasePoint projectBase = null;
BasePoint survey = null;
try { projectBase = BasePoint.GetProjectBasePoint(doc); } catch (Exception) { projectBase = null; }
try { survey = BasePoint.GetSurveyPoint(doc); } catch (Exception) { survey = null; }

double? trueNorthDegrees = null;
Transform toShared = null;
var sharedWhyNot = "";

try
{
    var place = doc.ActiveProjectLocation;
    if (place == null)
    {
        sharedWhyNot = "this model has no active project location";
    }
    else
    {
        try
        {
            var position = place.GetProjectPosition(XYZ.Zero);
            if (position != null) trueNorthDegrees = position.Angle * 180.0 / Math.PI;
        }
        catch (Exception) { }

        Transform total = null;
        try { total = place.GetTotalTransform(); } catch (Exception) { total = null; }

        if (total == null)
        {
            sharedWhyNot = "the active project location gave no transform";
        }
        else
        {
            // WHICH WAY THE TRANSFORM RUNS IS MEASURED, NOT ASSUMED. Each base
            // point carries its own internal AND shared position; the direction
            // that maps one onto the other for every point Revit gave is the one.
            var checkPoints = new List<BasePoint>();
            if (projectBase != null) checkPoints.Add(projectBase);
            if (survey != null) checkPoints.Add(survey);

            Func<Transform, bool> agrees = t =>
            {
                if (checkPoints.Count == 0) return false;
                foreach (var point in checkPoints)
                {
                    try
                    {
                        if (t.OfPoint(point.Position).DistanceTo(point.SharedPosition) > Tolerance) return false;
                    }
                    catch (Exception) { return false; }
                }
                return true;
            };

            Transform inverse = null;
            try { inverse = total.Inverse; } catch (Exception) { inverse = null; }

            if (inverse != null && agrees(inverse)) toShared = inverse;
            else if (agrees(total)) toShared = total;
            else if (checkPoints.Count == 0)
                sharedWhyNot = "neither the project base point nor the survey point could be read, "
                    + "so which way the location's transform runs could not be checked";
            else
                sharedWhyNot = "the location's transform, either way round, does not reproduce the "
                    + "shared positions Revit gives the base point and survey point";
        }
    }
}
catch (Exception)
{
    sharedWhyNot = "the active project location could not be read";
}

// ---- The elements ----------------------------------------------------------

double farthest = 0.0;
string farthestWhat = "";
Action<ElementId, XYZ> measure = (id, p) =>
{
    var d = p.GetLength();
    if (d > farthest) { farthest = d; farthestWhat = "id " + id; }
};

var wantShared = sharedCoordinates && toShared != null;
Func<XYZ, string> sharedPart = p => wantShared ? "; shared " + asShared(toShared.OfPoint(p)) : "";

if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given");
}
else
{
    var points = 0;
    var runs = 0;
    var boxes = 0;

    foreach (var element in elements)
    {
        if (element == null) continue;

        var atPoint = element.Location as LocationPoint;
        if (atPoint != null)
        {
            points++;
            // THE TURN IS PART OF WHERE A POINT-PLACED THING IS. Two units at the same
            // point facing opposite ways are not the same placement, and copying one
            // layout to another room needs both halves.
            string turned = "";
            try { turned = string.Format(", turned {0:0.#} degrees", atPoint.Rotation * 180.0 / Math.PI); }
            catch { }
            var instance = element as FamilyInstance;
            if (instance != null)
            {
                try
                {
                    var facing = instance.FacingOrientation;
                    turned += string.Format(", facing ({0:0.###}, {1:0.###}) in project north", facing.X, facing.Y);
                    if (wantShared)
                    {
                        var trueFacing = toShared.OfVector(facing);
                        turned += string.Format(", ({0:0.###}, {1:0.###}) in true north", trueFacing.X, trueFacing.Y);
                    }
                }
                catch { }
            }
            measure(element.Id, atPoint.Point);
            locations.Add(string.Format("id {0}: AT {1}{2}{3}", element.Id, asMm(atPoint.Point),
                sharedPart(atPoint.Point), turned));
            continue;
        }

        var alongCurve = element.Location as LocationCurve;
        if (alongCurve != null && alongCurve.Curve != null)
        {
            runs++;
            var start = alongCurve.Curve.GetEndPoint(0);
            var end = alongCurve.Curve.GetEndPoint(1);
            measure(element.Id, start);
            measure(element.Id, end);
            locations.Add(string.Format("id {0}: RUNS from {1}{2} to {3}{4}",
                element.Id, asMm(start), sharedPart(start), asMm(end), sharedPart(end)));
            continue;
        }

        var box = element.get_BoundingBox(null);
        if (box != null)
        {
            boxes++;
            var centre = (box.Min + box.Max) / 2.0;
            measure(element.Id, centre);
            locations.Add(string.Format("id {0}: no position of its own - the CENTRE OF ITS BOUNDING "
                + "BOX is {1}{2}", element.Id, asMm(centre), sharedPart(centre)));
            continue;
        }

        withoutLocation++;
        locations.Add(string.Format("id {0}: no position and no bounding box", element.Id));
    }

    findings.Add(string.Format("{0} element(s): {1} at a point, {2} running between two, {3} reported "
        + "by the centre of a bounding box, and {4} with no position at all",
        elements.Count, points, runs, boxes, withoutLocation));

    if (runs > 0)
        findings.Add("An element that RUNS has no single position. Both ends are given because the "
            + "midpoint of a long run is somewhere it passes through, not where it is");

    if (boxes > 0)
        findings.Add("A bounding-box centre is a fallback and is named as one. For an L-shaped or "
            + "sloped element it is a point in fresh air, outside the element entirely");

    // WHICH COORDINATES, SAID EVERY TIME.
    if (wantShared)
        findings.Add("Each position is given twice: first in millimetres from the project's INTERNAL "
            + "origin, then as east / north / elevation in the model's SHARED coordinates (the active "
            + "project location). Facing directions are given in project north and in true north");
    else if (sharedCoordinates)
        findings.Add("Shared coordinates were asked for and are NOT given: " + sharedWhyNot
            + ". The positions are from the project's internal origin only");
    else
        findings.Add("These are measured from the project's INTERNAL origin (the default), which is not "
            + "necessarily the survey point or the project base point. Good for comparing elements with "
            + "each other; for a coordinate to hand a surveyor, ask for shared coordinates. Facing "
            + "directions are in project north");

    if (farthestWhat.Length > 0)
    {
        var line = string.Format("The farthest position reported ({0}) is {1:0.0} m from the internal origin",
            farthestWhat, farthest * MillimetresPerFoot / 1000.0);
        if (farthest > RevitOriginLimitFeet)
            line += ". THAT IS BEYOND THE 20 MILES (ABOUT 32 KM) REVIT'S OWN HELP GIVES AS THE LIMIT "
                + "for geometry from the internal origin";
        findings.Add(line);
    }
}

// ---- The site, said every time -----------------------------------------------

findings.Add(trueNorthDegrees.HasValue
    ? string.Format("Project north is turned {0:0.###} degrees from true north (the active project "
        + "location's angle)", trueNorthDegrees.Value)
    : "The angle between project north and true north could not be read");

Action<string, BasePoint> site = (what, point) =>
{
    if (point == null) { findings.Add(what + ": could not be read"); return; }
    try
    {
        var internalAt = point.Position;
        var sharedAt = point.SharedPosition;
        findings.Add(string.Format("{0}: {1} from the internal origin ({2:0.0} m away); shared {3}",
            what, asMm(internalAt), internalAt.GetLength() * MillimetresPerFoot / 1000.0, asShared(sharedAt)));
    }
    catch (Exception) { findings.Add(what + ": its position could not be read"); }
};
site("Project base point", projectBase);
site("Survey point", survey);
