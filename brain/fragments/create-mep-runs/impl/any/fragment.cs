// NOT STANDALONE. Assumes `doc`, `systemType`, `ductType`, `level`, `runs`,
// `maxLengthMm` and `runKind` are in scope; leaves `elements`, `runsDrawn`,
// `segments`, `sized`, `lengthMm` and `summary` behind.
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
// CABLE TRAY, CONDUIT, FLEX DUCT AND FLEX PIPE (version 3, 2026-11-01).
// CREATE_ELECTRICAL_RUN, which drew cable tray or conduit between consecutive
// points, and CREATE_FLEX_PIPE, which made ONE flexible duct or pipe along the
// points given, are DEPRECATED and merged into this card. The type still
// decides:
//
//   a DuctType           Duct.Create per pair      duct system required
//   a PipeType           Pipe.Create per pair      piping system required
//   a CableTrayType      CableTray.Create per pair NO system type - leave it out
//   a ConduitType        Conduit.Create per pair   NO system type - leave it out
//   a FlexDuctType       FlexDuct.Create, ONE curve through every point of the run
//   a flex pipe type     FlexPipe.Create, ONE curve through every point of the run
//                        - Revit's own FlexPipe.IsFlexPipeTypeId decides it
//
// Tray and conduit carry no system in Revit, so `systemType` is optional
// (version 3) and a system NAMED with one is refused rather than ignored: it
// says the caller meant something other than containment. CREATE_ELECTRICAL_RUN
// asked "tray" or "conduit" in a word as well as the type, so the two could
// disagree; `runKind` carries that - left out the type alone decides, given it
// must agree with the type or nothing is drawn.
//
// A FLEX RUN HAS A REQUIRED MAXIMUM LENGTH, CREATE_FLEX_PIPE'S RULE. A flex drop
// is listed for a maximum length and is exceeded by accident - two points
// further apart than they looked - so `maxLengthMm` must be greater than zero
// for a flex type, and a run longer than it is REFUSED BEFORE ANYTHING IS MADE.
// Given for a rigid type, the same limit holds for each run there too.
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
// A flex run sharing an endpoint with a diffuser or a sprinkler is not joined
// to it either - CONNECT_OPEN_ENDS joins it, TRACE_CONNECTIVITY proves it.
//
// CREATE_PIPE's and CREATE_ELECTRICAL_RUN's rule that kept the segments that
// worked and COUNTED the refused ones is NOT carried - the conflict, and the
// safer rule kept. CREATE_ELECTRICAL_RUN and CREATE_FLEX_PIPE set no size and
// left the snap to SET_MEP_SIZE; here a size is optional per run and read back.
//
// ALL OR NOTHING - ARTICLE 8. A run that cannot be read, a segment too short to
// exist, a duct Revit will not create, a flex run over its limit, or a size the
// type snaps to something else - any one of them THROWS, the whole job is
// rolled back, and the message names each problem. Half a layout that looks
// drawn is worse than none.

var mmPerFoot = 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;
var numberStyle = System.Globalization.NumberStyles.Float;

var elements = new List<Element>();
var runsDrawn = 0;
var segments = 0;
var sized = 0;
var lengthMm = 0.0;
string summary = null;

var problems = new List<string>();

// WHAT IS DRAWN, decided by the type. Refused by name before a single run is
// read, so a wrong pair never reaches Revit. Flex is asked first: Revit's own
// test decides a flex pipe type, and a flex duct type is its own class.
var isFlexDuct = ductType is FlexDuctType;
var isFlexPipe = !isFlexDuct && ductType != null && FlexPipe.IsFlexPipeTypeId(doc, ductType.Id);
var isFlex = isFlexDuct || isFlexPipe;
var isTray = !isFlex && ductType is CableTrayType;
var isConduit = !isFlex && ductType is ConduitType;
var isPipe = !isFlex && ductType is PipeType;
var isDuct = !isFlex && ductType is DuctType;
var drawn = isFlexDuct ? "flex duct" : isFlexPipe ? "flex pipe" : isTray ? "cable tray"
    : isConduit ? "conduit" : isPipe ? "pipe" : "duct";
var typeName = ductType == null ? "(none)" : ductType.Name;
var systemName = systemType == null ? "(none)" : systemType.Name;

if (!isPipe && !isDuct && !isTray && !isConduit && !isFlex)
{
    problems.Add("'" + typeName + "' is not a duct, pipe, cable tray, conduit, flex duct or flex pipe type - "
        + "name one as the Project Browser lists it");
}
else if ((isDuct || isFlexDuct) && !(systemType is MechanicalSystemType))
{
    problems.Add("'" + typeName + "' is a " + drawn + " type and '" + systemName
        + "' is not a duct system - name a duct system type, Supply Air or Return Air say");
}
else if ((isPipe || isFlexPipe) && !(systemType is PipingSystemType))
{
    problems.Add("'" + typeName + "' is a " + drawn + " type and '" + systemName
        + "' is not a piping system - name a piping system type, Domestic Cold Water or Sanitary say");
}
else if ((isTray || isConduit) && systemType != null)
{
    // VERSION 3. Revit gives cable tray and conduit no system; one named here
    // says the caller meant duct or pipe, and is refused rather than ignored.
    problems.Add("'" + typeName + "' is a " + drawn + " type, and " + drawn + " carries no system in Revit - "
        + "leave systemType out, or name a duct or pipe type if '" + systemName + "' is what was meant");
}

// RUN KIND, CREATE_ELECTRICAL_RUN's "asked for, never guessed" (version 3).
// Left out it is "any" and the type decides; said, it must agree with the type.
var kindSaid = (runKind ?? "").Trim().ToLowerInvariant();
if (kindSaid.Length > 0 && kindSaid != "any")
{
    string kindMeant = null;
    if (kindSaid == "duct") kindMeant = "duct";
    else if (kindSaid == "pipe") kindMeant = "pipe";
    else if (kindSaid == "tray" || kindSaid == "cable tray" || kindSaid == "cabletray") kindMeant = "cable tray";
    else if (kindSaid == "conduit") kindMeant = "conduit";
    else if (kindSaid == "flex duct" || kindSaid == "flexible duct") kindMeant = "flex duct";
    else if (kindSaid == "flex pipe" || kindSaid == "flexible pipe") kindMeant = "flex pipe";

    if (kindMeant == null)
        problems.Add("runKind '" + runKind + "' is not a kind this draws - say duct, pipe, tray, conduit, "
            + "flex duct or flex pipe, or leave it out and the type decides. Guessing makes a drawing that "
            + "looks right and a schedule that is wrong");
    else if (kindMeant != drawn)
        problems.Add("runKind says " + kindMeant + " and '" + typeName + "' is a " + drawn
            + " type - the two must agree, so nothing was drawn");
}

// FLEX NEEDS ITS LIMIT (version 3, CREATE_FLEX_PIPE's rule).
if (isFlex && !(maxLengthMm > 0))
    problems.Add("a maximum length greater than zero is required for a " + drawn + " - it is listed for "
        + "one in most specifications, and it is asked for rather than assumed");

// Parse everything FIRST, so a typing mistake in the last run refuses the job
// before a single duct exists.
var runPoints = new List<List<XYZ>>();
var runWidth = new List<double>();
var runHeight = new List<double>();
var runDiameter = new List<double>();
var runLabel = new List<string>();

var takesDiameter = isDuct || isPipe || isConduit || isFlex;
var takesWidth = isDuct || isTray || isFlexDuct;

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

        if (size.StartsWith("d") && !takesDiameter)
        {
            problems.Add(label + ": the size '" + size + "' is a diameter, and a " + drawn + " is width x height - write 300x100");
        }
        else if (size.StartsWith("d"))
        {
            if (!double.TryParse(size.Substring(1).Trim(), numberStyle, invariant, out diameter) || diameter <= 0)
                problems.Add(label + ": the size '" + size + "' is not a diameter in millimetres - write d250");
        }
        else if (size.Contains("x") && !takesWidth)
        {
            problems.Add(label + ": the size '" + size + "' is width x height, and a " + drawn + " is round - write d50");
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

    // THE LIMIT, MEASURED ALONG THE ROUTE AS GIVEN, BEFORE ANYTHING IS MADE
    // (version 3). Required for flex above; held for any run when given.
    if (maxLengthMm > 0 && points.Count >= 2)
    {
        var routeFt = 0.0;
        for (var i = 1; i < points.Count; i++) routeFt += points[i].DistanceTo(points[i - 1]);
        var routeMm = routeFt * mmPerFoot;
        if (routeMm > maxLengthMm)
            problems.Add(label + ": the route measures " + Math.Round(routeMm).ToString(invariant)
                + " mm, over the " + Math.Round(maxLengthMm).ToString(invariant)
                + " mm limit given - shorten the route or raise the limit deliberately");
    }

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

// Which parameter takes each part of a size, for what is drawn.
var widthParam = isTray ? BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM : BuiltInParameter.RBS_CURVE_WIDTH_PARAM;
var heightParam = isTray ? BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM : BuiltInParameter.RBS_CURVE_HEIGHT_PARAM;
var diameterParam = (isPipe || isFlexPipe) ? BuiltInParameter.RBS_PIPE_DIAMETER_PARAM
    : isConduit ? BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM
    : BuiltInParameter.RBS_CURVE_DIAMETER_PARAM;

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

    // The pieces this run is made of: every consecutive pair for a rigid run,
    // and ONE piece through every point for a flex run (CREATE_FLEX_PIPE).
    var made = new List<KeyValuePair<string, MEPCurve>>();
    var tooShort = false;

    for (var i = 0; i + 1 < points.Count; i++)
    {
        if (points[i].DistanceTo(points[i + 1]) <= shortest)
        {
            problems.Add(label + ", segment " + (i + 1) + ": its two points are too close for a " + drawn + " to exist");
            tooShort = true;
        }
    }
    if (tooShort) continue;

    if (isFlex)
    {
        MEPCurve flex = null;
        string flexRefusal = null;
        try
        {
            flex = isFlexDuct
                ? (MEPCurve)FlexDuct.Create(doc, systemType.Id, ductType.Id, level.Id, points)
                : FlexPipe.Create(doc, systemType.Id, ductType.Id, level.Id, points);
        }
        catch (Exception failure)
        {
            flexRefusal = label + ": Revit would not create the " + drawn + " - " + failure.Message;
        }
        if (flexRefusal != null) problems.Add(flexRefusal);
        else if (flex == null) problems.Add(label + ": Revit returned no " + drawn + " and raised no error");
        else made.Add(new KeyValuePair<string, MEPCurve>(label, flex));
    }
    else
    {
        for (var i = 0; i + 1 < points.Count; i++)
        {
            var start = points[i];
            var end = points[i + 1];

            MEPCurve duct = null;
            try
            {
                if (isTray) duct = CableTray.Create(doc, ductType.Id, start, end, level.Id);
                else if (isConduit) duct = Conduit.Create(doc, ductType.Id, start, end, level.Id);
                else if (isPipe) duct = Pipe.Create(doc, systemType.Id, ductType.Id, level.Id, start, end);
                else duct = Duct.Create(doc, systemType.Id, ductType.Id, level.Id, start, end);
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

            made.Add(new KeyValuePair<string, MEPCurve>(label + ", segment " + (i + 1), duct));
        }
    }

    foreach (var piece in made)
    {
        var duct = piece.Value;
        elements.Add(duct);
        segments++;

        var wanted = new List<KeyValuePair<BuiltInParameter, double>>();
        if (runWidth[r] > 0) wanted.Add(new KeyValuePair<BuiltInParameter, double>(widthParam, runWidth[r]));
        if (runHeight[r] > 0) wanted.Add(new KeyValuePair<BuiltInParameter, double>(heightParam, runHeight[r]));
        if (runDiameter[r] > 0) wanted.Add(new KeyValuePair<BuiltInParameter, double>(diameterParam, runDiameter[r]));

        foreach (var request in wanted)
        {
            var parameter = duct.get_Parameter(request.Key);
            if (parameter == null || parameter.IsReadOnly || !parameter.Set(request.Value))
            {
                problems.Add(piece.Key + ": the " + drawn + " type would not take that size - is it the right shape?");
                continue;
            }
            pending.Add(duct);
            pendingParam.Add(request.Key);
            pendingAsked.Add(request.Value);
            pendingLabel.Add(piece.Key);
        }
    }

    // The length drawn, measured along the points as given (CREATE_FLEX_PIPE's
    // `lengthMm`, now for every kind).
    if (made.Count > 0)
    {
        var routeFt = 0.0;
        for (var i = 1; i < points.Count; i++) routeFt += points[i].DistanceTo(points[i - 1]);
        lengthMm += routeFt * mmPerFoot;
    }

    runsDrawn++;
}

// A SIZE IS READ BACK, NOT ASSUMED. Revit snaps a size to one the type allows,
// silently, and the snap is only visible after a regenerate - the same check
// SET_MEP_SIZE makes.
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
    "{0} run(s) drawn as {1} {3} piece(s), {4} mm along the points given; {2} sized as asked. "
    + "NOTHING IS CONNECTED - sharing a point is not a joint in Revit. ",
    runsDrawn, segments, sized, drawn, Math.Round(lengthMm).ToString(invariant))
    + (isFlex
        ? "CONNECT_OPEN_ENDS joins a flex end to its " + (isFlexDuct ? "diffuser or branch" : "sprinkler or tee")
          + ", and TRACE_CONNECTIVITY proves it"
          + (maxLengthMm > 0 ? "; every run is within the " + Math.Round(maxLengthMm).ToString(invariant) + " mm limit" : "")
        : "FIT_MEP_JOINTS puts the fittings in and can take these " + drawn + " pieces straight from here");
