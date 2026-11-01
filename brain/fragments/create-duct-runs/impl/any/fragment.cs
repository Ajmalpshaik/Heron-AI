// NOT STANDALONE. Assumes `doc`, `systemType`, `ductType`, `level` and `runs`
// are in scope; leaves `elements`, `runsDrawn`, `segments`, `sized` and
// `summary` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// DUCT OR PIPE (version 2, 2026-11-01). CREATE_PIPE, which drew straight pipe
// between consecutive points, is DEPRECATED and merged into this card, the way
// CREATE_FLEX_PIPE took flex duct: the TYPE decides what is drawn. `ductType`
// keeps its name so every call written for version 1 runs unchanged, and is
// now any MEPCurveType - a DuctType draws Duct.Create, a PipeType draws
// Pipe.Create. `systemType` is an MEPSystemType that must go with it - a duct
// system with a duct type, a piping system with a pipe type - checked by name
// before Revit is asked. A pipe run's size is a diameter, d50, set through the
// pipe's own diameter parameter and read back as a duct's is: Revit snaps a
// pipe size to its segment's sizes and reports success (77 asked, 80 made),
// so a snap fails the whole job, named. Any other type is refused by name.
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20). `runs` arrives as the caller typed
// it, so this is the one place its numbers are converted.
//
// EVERY RUN IN ONE CALL. CREATE_DUCT draws one polyline per call, and five
// small rooms took twenty-five of them plus two sizing calls on 2026-09-23.
// Here every run arrives together, each with its own size:
//
//     850x195: 3075,-2100,3122.5; 3075,-1100,3122.5 | 300x300: 3075,-1100,3122.5; 3075,8100,3122.5
//
// Runs are separated by | . A run is an optional size - WxH for rectangular,
// d250 for round, in millimetres - then a colon, then its points in
// millimetres, x,y,z each, separated by ; . Consecutive points make one duct
// segment each, exactly as CREATE_DUCT does. No size means the duct type's own.
//
// IT GUESSES NO ROUTE. Every point is given, in order, and used as given. The
// thinking about where ducts go happens before this call, not in it.
//
// IT DOES NOT CONNECT ANYTHING. Segments that share a point are NOT joined
// here. FIT_MEP_JOINTS puts the fittings in, and it can take these ducts
// straight from here: the new ducts are left as `elements` for exactly that.
//
// CREATE_PIPE's rule that kept the segments that worked and COUNTED the
// refused ones is NOT carried - the conflict, and the safer rule kept.
//
// ALL OR NOTHING - ARTICLE 8. A run that cannot be read, a segment too short to
// exist, a duct Revit will not create, or a size the duct type snaps to
// something else - any one of them THROWS, the whole job is rolled back, and the
// message names each problem. Half a layout that looks drawn is worse than
// none.

var mmPerFoot = 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;

var elements = new List<Element>();
var runsDrawn = 0;
var segments = 0;
var sized = 0;
string summary = null;

var problems = new List<string>();

// WHAT IS DRAWN, decided by the type (version 2). Refused by name before a
// single run is read, so a wrong pair never reaches Revit.
var isPipe = ductType is PipeType;
var isDuct = ductType is DuctType;
var drawn = isPipe ? "pipe" : "duct";
if (!isPipe && !isDuct)
    problems.Add("'" + (ductType == null ? "(none)" : ductType.Name) + "' is neither a duct type nor a pipe type - "
        + "this draws straight duct or straight pipe; flex is CREATE_FLEX_PIPE, cable tray and conduit "
        + "CREATE_ELECTRICAL_RUN");
else if (isDuct && !(systemType is MechanicalSystemType))
    problems.Add("'" + ductType.Name + "' is a duct type and '" + (systemType == null ? "(none)" : systemType.Name)
        + "' is not a duct system - name a duct system type, Supply Air or Return Air say");
else if (isPipe && !(systemType is PipingSystemType))
    problems.Add("'" + ductType.Name + "' is a pipe type and '" + (systemType == null ? "(none)" : systemType.Name)
        + "' is not a piping system - name a piping system type, Domestic Cold Water or Sanitary say");

// Parse everything FIRST, so a typing mistake in the last run refuses the job
// before a single duct exists.
var runPoints = new List<List<XYZ>>();
var runWidth = new List<double>();
var runHeight = new List<double>();
var runDiameter = new List<double>();
var runLabel = new List<string>();

var runTexts = (runs ?? "").Split('|');

for (var r = 0; r < runTexts.Length; r++)
{
    var text = runTexts[r].Trim();
    if (text.Length == 0) continue;

    var label = "run " + (runLabel.Count + 1);
    double width = 0, height = 0, diameter = 0;
    var body = text;

    var colon = text.IndexOf(':');
    if (colon >= 0)
    {
        var size = text.Substring(0, colon).Trim().ToLowerInvariant();
        body = text.Substring(colon + 1);

        if (size.StartsWith("d"))
        {
            if (!double.TryParse(size.Substring(1).Trim(), numberStyle, invariant, out diameter) || diameter <= 0)
                problems.Add(label + ": the size '" + size + "' is not a diameter in millimetres - write d250");
        }
        else if (size.Contains("x") && isPipe)
        {
            problems.Add(label + ": the size '" + size + "' is width x height, and a pipe is round - write d50");
        }
        else if (size.Contains("x"))
        {
            var sides = size.Split('x');
            if (sides.Length != 2
                || !double.TryParse(sides[0].Trim(), numberStyle, invariant, out width)
                || !double.TryParse(sides[1].Trim(), numberStyle, invariant, out height)
                || width <= 0 || height <= 0)
                problems.Add(label + ": the size '" + size + "' is not width x height in millimetres - write 300x300");
        }
        else
        {
            problems.Add(label + ": the size '" + size + "' is neither WxH nor d250");
        }
    }

    var points = new List<XYZ>();
    foreach (var raw in body.Split(';'))
    {
        var point = raw.Trim();
        if (point.Length == 0) continue;

        var parts = point.Split(',');
        double x, y, z;
        if (parts.Length != 3
            || !double.TryParse(parts[0].Trim(), numberStyle, invariant, out x)
            || !double.TryParse(parts[1].Trim(), numberStyle, invariant, out y)
            || !double.TryParse(parts[2].Trim(), numberStyle, invariant, out z))
        {
            problems.Add(label + ": '" + point + "' is not a point - write x,y,z in millimetres");
            continue;
        }

        points.Add(new XYZ(x / mmPerFoot, y / mmPerFoot, z / mmPerFoot));
    }

    if (points.Count < 2) problems.Add(label + ": a run needs at least two points");

    runPoints.Add(points);
    runWidth.Add(width / mmPerFoot);
    runHeight.Add(height / mmPerFoot);
    runDiameter.Add(diameter / mmPerFoot);
    runLabel.Add(label);
}

if (runLabel.Count == 0) problems.Add("no runs were given - write them as size: x,y,z; x,y,z | size: ...");

if (problems.Count > 0)
{
    throw new InvalidOperationException(string.Format(
        "{0} problem(s) reading the runs, so nothing was drawn: {1}",
        problems.Count, string.Join(" | ", problems)));
}

// Draw, and ask for each run's size as its segments are made.
var shortest = doc.Application.ShortCurveTolerance;
var pending = new List<Element>();
var pendingParam = new List<BuiltInParameter>();
var pendingAsked = new List<double>();
var pendingLabel = new List<string>();

for (var r = 0; r < runPoints.Count; r++)
{
    var points = runPoints[r];
    var label = runLabel[r];

    for (var i = 0; i + 1 < points.Count; i++)
    {
        var start = points[i];
        var end = points[i + 1];

        if (start.DistanceTo(end) <= shortest)
        {
            problems.Add(label + ", segment " + (i + 1) + ": its two points are too close for a " + drawn + " to exist");
            continue;
        }

        MEPCurve duct = null;
        try
        {
            duct = isPipe
                ? (MEPCurve)Pipe.Create(doc, systemType.Id, ductType.Id, level.Id, start, end)
                : Duct.Create(doc, systemType.Id, ductType.Id, level.Id, start, end);
        }
        catch (Exception failure)
        {
            problems.Add(label + ", segment " + (i + 1) + ": Revit would not create it - " + failure.Message);
            continue;
        }

        if (duct == null)
        {
            problems.Add(label + ", segment " + (i + 1) + ": Revit returned no " + drawn);
            continue;
        }

        elements.Add(duct);
        segments++;

        var wanted = new List<KeyValuePair<BuiltInParameter, double>>();
        if (runWidth[r] > 0) wanted.Add(new KeyValuePair<BuiltInParameter, double>(BuiltInParameter.RBS_CURVE_WIDTH_PARAM, runWidth[r]));
        if (runHeight[r] > 0) wanted.Add(new KeyValuePair<BuiltInParameter, double>(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM, runHeight[r]));
        if (runDiameter[r] > 0) wanted.Add(new KeyValuePair<BuiltInParameter, double>(
            isPipe ? BuiltInParameter.RBS_PIPE_DIAMETER_PARAM : BuiltInParameter.RBS_CURVE_DIAMETER_PARAM, runDiameter[r]));

        foreach (var request in wanted)
        {
            var parameter = duct.get_Parameter(request.Key);
            if (parameter == null || parameter.IsReadOnly || !parameter.Set(request.Value))
            {
                problems.Add(label + ", segment " + (i + 1) + ": the " + drawn + " type would not take that size - is it the right shape?");
                continue;
            }
            pending.Add(duct);
            pendingParam.Add(request.Key);
            pendingAsked.Add(request.Value);
            pendingLabel.Add(label + ", segment " + (i + 1));
        }
    }

    runsDrawn++;
}

// A SIZE IS READ BACK, NOT ASSUMED. Revit snaps a size to one the duct or pipe type
// allows, silently, and the snap is only visible after a regenerate - the
// same check SET_MEP_SIZE makes.
if (pending.Count > 0)
{
    doc.Regenerate();

    var sizedIds = new List<ElementId>();
    var snappedIds = new List<ElementId>();

    for (var i = 0; i < pending.Count; i++)
    {
        var parameter = pending[i].get_Parameter(pendingParam[i]);
        var stored = parameter == null ? double.NaN : parameter.AsDouble();
        var want = pendingAsked[i];

        if (double.IsNaN(stored) || Math.Abs(stored - want) > Math.Abs(want) * 1e-9 + 1e-12)
        {
            if (!snappedIds.Contains(pending[i].Id))
            {
                snappedIds.Add(pending[i].Id);
                problems.Add(pendingLabel[i] + ": asked for " + (want * mmPerFoot).ToString("0.#", invariant)
                    + " mm and the " + drawn + " type made it "
                    + (double.IsNaN(stored) ? "nothing" : (stored * mmPerFoot).ToString("0.#", invariant)) + " mm");
            }
        }
        else if (!sizedIds.Contains(pending[i].Id))
        {
            sizedIds.Add(pending[i].Id);
        }
    }

    foreach (var id in sizedIds) if (!snappedIds.Contains(id)) sized++;
}

if (problems.Count > 0)
{
    throw new InvalidOperationException(string.Format(
        "{0} problem(s) drawing the runs, so NOTHING was kept - one request is one change, "
        + "whole or not at all (Article 8): {1}",
        problems.Count, string.Join(" | ", problems)));
}

summary = string.Format(
    "{0} run(s) drawn as {1} {3} segment(s); {2} sized as asked. Nothing is joined yet - "
    + "FIT_MEP_JOINTS puts the fittings in and can take these {3}s straight from here",
    runsDrawn, segments, sized, drawn);
