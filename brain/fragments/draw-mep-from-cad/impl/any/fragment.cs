// NOT STANDALONE. Assumes `doc`, `imports`, `cadName`, `layerNames`, `drawAs`,
// `systemType`, `runType`, `level`, `middleElevationMm`, `defaultSize`,
// `sizes`, `mode`, `sizeStepMm`, `minWidthMm`, `maxWidthMm`, `toleranceMm`,
// `bridgeMm`, `hintReachMm`, `minLengthMm`, `maxRuns`, `previewOnly` and
// `allOrNothing` are in scope; leaves `created`, `fittings`, `failedJoins`,
// `runsWithoutSize`, `sizeConflicts`, `skipped`, `skippedWhy`, `layersRead`,
// `runsFound`, `refused` and `findings` behind.
//
// CAD LINES TO REAL DUCTS AND PIPES - docs/48 section 5 item 6, the owner's
// design of 2026-11-09. The host looks at the drawing (revit_view_image) and
// reads the size tags off the picture, because the Revit API cannot read DWG
// text; THIS reads the GEOMETRY itself, inside Revit, from the layers named.
// The coordinates come from the CAD, never from the picture - a picture is not
// precise. Only the size HINTS come from the picture, and a hint is matched to
// the nearest run within `hintReachMm`, so it needs to be near, not exact.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and opens none.
//
// WHAT "ONE FAILED FITTING DOES NOT LOSE THE RUN" CAN AND CANNOT MEAN. Each
// fitting is placed inside its own SubTransaction, so a fitting Revit REFUSES
// BY THROWING is taken back alone and listed in `failedJoins`. But Revit also
// POSTS failures - a run shorter than its two fittings is "modified to be in
// the opposite direction" (duct-layout's note, FIT_MEP_JOINTS' measured
// lesson) - and those are handled only when the executor commits the whole
// call: its failure discipline (RevitFragment, Discipline) rolls the WHOLE call
// back, one undo, nothing kept (Article 8). A SubTransaction cannot keep the
// rest past that. So the room a fitting needs is CHECKED FIRST (`roomFor`
// below): each leg's length against the fittings at both its ends, from a safe
// estimate of each fitting's reach (an elbow 1.5 x the largest size meeting
// there, a tee, cross or transition 1 x, a tap 1 x the branch, plus a margin).
// A joint without room is SKIPPED and listed in `failedJoins` before Revit is
// asked, so Revit has nothing to reverse. An estimate can still be short, and
// what Revit posts then still rolls the whole call back - NEEDS-CHECKING MX6
// measures it. `allOrNothing` true throws when any joint was left unfitted, so
// nothing is kept unless every joint is (FIT_MEP_JOINTS' rule); left out it is
// false - the owner's design - and which is the default is his to confirm
// (FRAGMENT-ISSUES row 5b-1480).
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20). Points are Revit's INTERNAL
// coordinates in millimetres - the same numbers EXTRACT_CAD_CURVES' listOnly
// gives and this card's `runsFound` gives back - not the Project Base Point's or
// the survey point's.
//
// HOW THE LINES BECOME RUNS
// -------------------------
//   1. Read every Line and PolyLine on the named layers of ONE CAD file (exact
//      names, case counts - EXTRACT_CAD_CURVES' rule). Arcs, splines and
//      ellipses are COUNTED and skipped - an arc is usually a radius elbow,
//      which Revit draws itself from the routing preferences. A curve whose two
//      ends are at different heights, a piece shorter than `minLengthMm`, and a
//      block inside the drawing (not opened) are counted too. More than 40
//      pieces per run of `maxRuns` is refused before any searching is done.
//   2. DOUBLE (a duct drawn as two parallel lines): two parallel lines
//      `minWidthMm` to `maxWidthMm` apart, overlapping along their length, with
//      no third parallel line between them, are one duct: its centreline is
//      the middle of the overlap and its width the spacing. A third line lying
//      exactly midway, the only one between them, is the duct's drawn
//      CENTRELINE and is dropped, said in findings. Pairs are chosen as a chain
//      is: a line with only ONE possible partner over that stretch is paired
//      first, again and again, so ducts side by side pair as ducts, never as
//      the gaps between them; when no line is left with one choice, the nearest
//      pair is taken and marked DOUBTFUL in runsFound and findings, and so is a
//      pair beside a parallel line left with no partner. SINGLE: one line is
//      one run. AUTO: double for a duct, single for a pipe.
//   3. Pieces in one straight line, of one width, less than `bridgeMm` apart
//      are one run - a line broken for a tag, or a main whose side line is
//      broken where a branch enters. NOT when the gap sits on another run:
//      that is a cross, or a crossing drawn with a break.
//   4. Joints, every possible one found first and the NEAREST taken first:
//      two run ends whose lines cross within `bridgeMm` (or half the other
//      run's width) of both ends meet there - an elbow; an end landing on the
//      side of another run, within half that run's width - a tee that splits
//      the main (a cross when two land at one point), or, when the type's
//      routing preference is TAPS, a tap on the main, which is not cut; two
//      ends in one line with different widths - a transition. An end is
//      trimmed back only by a small overshoot, and never carried across, or to
//      a point on, another run: one run stopping either side of another is a
//      CROSSING DRAWN WITH A BREAK, listed and left unjoined. Two runs crossing
//      in their middles are listed as a crossing too. Anything else is an open
//      end.
//   5. Sizes: each size hint belongs to the ONE run nearest it, within
//      `hintReachMm` - of two near one run, the nearer wins and the other is
//      listed in sizeConflicts; a run with none takes the size of a tagged run
//      it continues across an ELBOW (never across a transition); otherwise the
//      default, and it is listed. In DOUBLE mode the width is the spacing
//      rounded to `sizeStepMm`, and a hint wins over it - listed in
//      `sizeConflicts` where they disagree. For a PIPE the spacing is the drawn
//      outside diameter, never a nominal size, so it is never used as one.
//   6. Draw every run as one duct or pipe at the level's elevation plus
//      `middleElevationMm`, set its size and read it back; then each joint's
//      fitting, after its room is checked, by Revit's own routing preferences
//      (NewElbowFitting, NewTeeFitting, NewCrossFitting, NewTransitionFitting,
//      NewTakeoffFitting for a tap), chosen from the connectors as
//      FIT_MEP_JOINTS (PROVEN) chooses - its route re-authored here, not called.
//      A joint where a run that should meet it was not drawn gets no fitting
//      and is listed.
//
// THE SEARCHES ARE BOUNDED. Every neighbour search - parallel partners,
// pieces to join, joint candidates - looks only in the cells of a grid sized
// on `maxWidthMm` and `bridgeMm`, never across the whole drawing.

var mmPerFoot = 304.8;
var invariant = System.Globalization.CultureInfo.InvariantCulture;

var created = new List<ElementId>();
var fittings = new List<ElementId>();
var failedJoins = new List<string>();
var runsWithoutSize = new List<string>();
var sizeConflicts = new List<string>();
var skipped = 0;
var skippedWhy = new List<string>();
var layersRead = new List<string>();
var runsFound = "";
var refused = "";
var findings = new List<string>();

Func<string, bool> isNone = t => t == null || t.Trim().Length == 0
    || t.Trim().Equals("none", StringComparison.OrdinalIgnoreCase);
Func<double, string> mm = f => Math.Round(f * mmPerFoot).ToString(invariant);
Func<double, double, string> at = (x, y) => "(" + mm(x) + ", " + mm(y) + ") mm";
Func<double, string> num = v => v.ToString("0.#", invariant);

// ---------------------------------------------------------------------------
// The values, checked before anything is read
// ---------------------------------------------------------------------------

var kind = (drawAs ?? "").Trim().ToLowerInvariant();
if (kind == "ducts" || kind == "ductwork") kind = "duct";
if (kind == "pipes" || kind == "piping") kind = "pipe";
var modeWord = isNone(mode) ? "auto" : mode.Trim().ToLowerInvariant();

var step = sizeStepMm;
var tol = toleranceMm / mmPerFoot;
var bridge = bridgeMm / mmPerFoot;
var minW = minWidthMm / mmPerFoot;
var maxW = maxWidthMm / mmPerFoot;
var reachHint = hintReachMm / mmPerFoot;
var minLen = minLengthMm / mmPerFoot;

if (doc.IsFamilyDocument)
    refused = "this is a family, and ducts and pipes are drawn in a project. Nothing was drawn";
else if (kind != "duct" && kind != "pipe")
    refused = "drawAs \"" + (drawAs ?? "") + "\" is neither duct nor pipe - say which this layer is to become. "
        + "Nothing was drawn";
else if (modeWord != "auto" && modeWord != "double" && modeWord != "single")
    refused = "mode \"" + mode + "\" is not one this reads - double (a duct drawn as two parallel lines), single "
        + "(one line is one run) or auto (double for a duct, single for a pipe). Nothing was drawn";
else if (systemType == null || runType == null || level == null)
    refused = "a system type, a " + kind + " type and a level are all needed. Nothing was drawn";
else if (kind == "duct" && !(runType is DuctType))
    refused = "\"" + runType.Name + "\" is not a duct type, and drawAs is duct. Nothing was drawn";
else if (kind == "pipe" && !(runType is PipeType))
    refused = "\"" + runType.Name + "\" is not a pipe type, and drawAs is pipe. Nothing was drawn";
else if (kind == "duct" && !(systemType is MechanicalSystemType))
    refused = "\"" + systemType.Name + "\" is not a duct system type, and drawAs is duct. Nothing was drawn";
else if (kind == "pipe" && !(systemType is PipingSystemType))
    refused = "\"" + systemType.Name + "\" is not a piping system type, and drawAs is pipe. Nothing was drawn";
else if (double.IsNaN(middleElevationMm) || Math.Abs(middleElevationMm) > 100000)
    refused = "middleElevationMm " + num(middleElevationMm) + " is not a height this draws at - give the runs' "
        + "middle elevation above the level in millimetres, within 100 m. Nothing was drawn";
else if (!(toleranceMm > 0 && toleranceMm <= 500))
    refused = "toleranceMm " + num(toleranceMm) + " must be above 0 and at most 500. Nothing was drawn";
else if (!(bridgeMm >= 0 && bridgeMm <= 5000))
    refused = "bridgeMm " + num(bridgeMm) + " must be from 0 to 5000. Nothing was drawn";
else if (!(minWidthMm > 0 && maxWidthMm > minWidthMm && maxWidthMm <= 5000))
    refused = "the spacing of a double-line duct must be a range above 0 and at most 5000 mm - minWidthMm "
        + num(minWidthMm) + ", maxWidthMm " + num(maxWidthMm) + ". Nothing was drawn";
else if (!(sizeStepMm >= 1 && sizeStepMm <= 500))
    refused = "sizeStepMm " + num(sizeStepMm) + " must be from 1 to 500. Nothing was drawn";
else if (!(hintReachMm >= 0 && hintReachMm <= 20000))
    refused = "hintReachMm " + num(hintReachMm) + " must be from 0 to 20000. Nothing was drawn";
else if (!(minLengthMm > 0 && minLengthMm <= 5000))
    refused = "minLengthMm " + num(minLengthMm) + " must be above 0 and at most 5000. Nothing was drawn";
else if (maxRuns < 1 || maxRuns > 5000)
    refused = "maxRuns " + maxRuns + " must be from 1 to 5000. Nothing was drawn";

var doubleMode = modeWord == "double" || (modeWord == "auto" && kind == "duct");
// A pipe is always round; a duct type's shape is its own (MEPCurveType.Shape).
var isRound = refused.Length == 0 && (kind == "pipe" || runType.Shape == ConnectorProfileType.Round);

// ---------------------------------------------------------------------------
// Sizes as the modeller writes them: 600x400, 600X400, 600×400; Ø150, ø150,
// d150, DN150, 150dia, or a bare 150 - a diameter. Millimetres.
// ---------------------------------------------------------------------------

Func<string, double[]> readSize = text =>
{
    if (text == null) return null;
    var t = text.Trim().Replace('×', 'x').Replace('X', 'x').Replace(" ", "");
    if (t.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) t = t.Substring(0, t.Length - 2);
    var rect = System.Text.RegularExpressions.Regex.Match(t, @"^(\d+(?:\.\d+)?)x(\d+(?:\.\d+)?)$");
    if (rect.Success)
    {
        var w = double.Parse(rect.Groups[1].Value, invariant);
        var h = double.Parse(rect.Groups[2].Value, invariant);
        return w > 0 && h > 0 && w <= 10000 && h <= 10000 ? new[] { w, h } : null;
    }
    var round = System.Text.RegularExpressions.Regex.Match(t,
        @"^(?:Ø|ø|⌀|DN|dn|Dn|d|D|dia\.?|DIA\.?|Dia\.?)?(\d+(?:\.\d+)?)(?:Ø|ø|⌀|dia|DIA|Dia)?$");
    if (round.Success)
    {
        var d = double.Parse(round.Groups[1].Value, invariant);
        return d > 0 && d <= 10000 ? new[] { d } : null;
    }
    return null;
};
Func<double[], string> sizeText = s => s == null ? "?" : s.Length == 2
    ? num(s[0]) + "x" + num(s[1]) : "Ø" + num(s[0]);

double[] defaultParsed = null;
if (refused.Length == 0)
{
    defaultParsed = readSize(defaultSize);
    if (defaultParsed == null)
        refused = "defaultSize \"" + (defaultSize ?? "") + "\" cannot be read - write 400x300 for a rectangular "
            + "duct, Ø150 or d150 for a round duct or a pipe, in millimetres. Nothing was drawn";
    else if (isRound && defaultParsed.Length != 1)
        refused = "\"" + runType.Name + "\" is round, so defaultSize must be a diameter (Ø150), not "
            + sizeText(defaultParsed) + ". Nothing was drawn";
    else if (!isRound && defaultParsed.Length != 2)
        refused = "\"" + runType.Name + "\" is " + runType.Shape.ToString().ToLowerInvariant() + ", so defaultSize "
            + "must be width x height (400x300), not " + sizeText(defaultParsed) + ". Nothing was drawn";
}

// The hints the host read off the picture: size@x,y in millimetres, ";" between.
var hintSize = new List<double[]>();
var hintX = new List<double>();
var hintY = new List<double>();
var hintText = new List<string>();
if (refused.Length == 0 && !isNone(sizes))
{
    var unreadable = new List<string>();
    foreach (var raw in sizes.Split(';'))
    {
        var one = raw.Trim();
        if (one.Length == 0) continue;
        var atSign = one.IndexOf('@');
        double[] s = atSign > 0 ? readSize(one.Substring(0, atSign)) : null;
        var xy = atSign > 0 ? one.Substring(atSign + 1).Split(',') : new string[0];
        double hx = 0, hy = 0;
        if (s == null || xy.Length < 2 || xy.Length > 3
            || !double.TryParse(xy[0].Trim(), System.Globalization.NumberStyles.Float, invariant, out hx)
            || !double.TryParse(xy[1].Trim(), System.Globalization.NumberStyles.Float, invariant, out hy))
        {
            unreadable.Add("\"" + one + "\"");
            continue;
        }
        hintSize.Add(s);
        hintX.Add(hx / mmPerFoot);
        hintY.Add(hy / mmPerFoot);
        hintText.Add(sizeText(s) + "@" + num(hx) + "," + num(hy));
    }
    if (unreadable.Count > 0)
        refused = unreadable.Count + " size hint(s) cannot be read: " + string.Join(", ", unreadable.ToArray())
            + ". Write each as size@x,y in millimetres, ; between them - 600x400@12500,30200; Ø150@14000,30200. "
            + "Nothing was drawn";
}

// ---------------------------------------------------------------------------
// The CAD drawing - ONE, selected or named
// ---------------------------------------------------------------------------

var cads = new List<ImportInstance>();
// The file's name as Manage Links and revit_imports show it: the CAD type's name.
Func<ImportInstance, string> fileOf = import =>
{
    var type = doc.GetElement(import.GetTypeId());
    if (type != null && !string.IsNullOrEmpty(type.Name)) return type.Name;
    return import.Category != null ? import.Category.Name : import.Name;
};
Func<string, string, bool> nameMatches = (name, wanted) =>
{
    if (string.IsNullOrEmpty(name)) return false;
    if (name.Equals(wanted, StringComparison.OrdinalIgnoreCase)) return true;
    var dot = name.LastIndexOf('.');
    return dot > 0 && name.Substring(0, dot).Equals(wanted, StringComparison.OrdinalIgnoreCase);
};
Func<string> cadList = () =>
{
    var names = new List<string>();
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)))
    {
        var shown = fileOf((ImportInstance)e);
        if (!names.Contains(shown)) names.Add(shown);
        if (names.Count >= 20) break;
    }
    return names.Count == 0 ? "this model has no imported or linked CAD file of its own"
        : "the CAD files in this model: " + string.Join(", ", names.ToArray());
};
// A CAD file INSIDE a Revit link: named, so "not here" is never said of it.
Func<string, string> linkHolding = wanted =>
{
    foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(RevitLinkInstance)))
    {
        var linked = ((RevitLinkInstance)e).GetLinkDocument();
        if (linked == null) continue;
        foreach (var i in new FilteredElementCollector(linked).OfClass(typeof(ImportInstance)))
        {
            var import = (ImportInstance)i;
            var type = linked.GetElement(import.GetTypeId());
            if (nameMatches(import.Name, wanted) || (type != null && nameMatches(type.Name, wanted))
                || (import.Category != null && nameMatches(import.Category.Name, wanted)))
                return e.Name;
        }
    }
    return null;
};

if (refused.Length == 0)
{
    if (!isNone(cadName))
    {
        var wanted = cadName.Trim();
        foreach (var e in new FilteredElementCollector(doc).OfClass(typeof(ImportInstance)))
        {
            var import = e as ImportInstance;
            if (import == null) continue;
            if (nameMatches(import.Name, wanted) || nameMatches(fileOf(import), wanted)
                || (import.Category != null && nameMatches(import.Category.Name, wanted))) cads.Add(import);
        }
        if (cads.Count == 0)
        {
            var link = linkHolding(wanted);
            refused = link != null
                ? "\"" + wanted + "\" is inside the Revit link " + link + ", not in this model - a CAD file inside a "
                    + "Revit link is not read. Open that model and draw there, or link the CAD into this one. Nothing was drawn"
                : "no imported or linked CAD file is called \"" + wanted + "\" - " + cadList() + ". Nothing was drawn";
        }
        else if (cads.Count > 1)
            refused = cads.Count + " CAD files are called \"" + wanted + "\" (it is placed more than once) - select "
                + "the one to read in Revit and leave cadName out. Nothing was drawn";
        else if (imports != null && imports.Count > 0)
            findings.Add("cadName was given, so the CAD file was found by its name and the selection was not used");
    }
    else
    {
        var linksSelected = new List<string>();
        if (imports != null)
            foreach (var e in imports)
            {
                var import = e as ImportInstance;
                if (import != null) cads.Add(import);
                else if (e is RevitLinkInstance) linksSelected.Add(e.Name);
            }
        if (cads.Count > 1)
            refused = cads.Count + " CAD files are selected (" + string.Join(", ", cads.Select(c => fileOf(c)).ToArray())
                + ") - this reads ONE per call. Select the one to draw from, or name it in cadName. Nothing was drawn";
        else if (cads.Count == 0 && linksSelected.Count > 0)
            refused = "the selection is the Revit link " + string.Join(", ", linksSelected.ToArray()) + ", not a CAD "
                + "file - a CAD file inside a Revit link is not read. Open that model and draw there, or link the CAD "
                + "into this one. Nothing was drawn";
        else if (cads.Count == 0)
            refused = "no CAD file was selected or named - select the linked or imported CAD in Revit, or give "
                + "its name in cadName; " + cadList() + ". Nothing was drawn";
    }
}

// ---------------------------------------------------------------------------
// Read the lines on the named layers - at most 40 pieces per run of maxRuns
// ---------------------------------------------------------------------------

var wantedLayers = new List<string>();
if (layerNames != null)
    foreach (var name in layerNames)
        if (!string.IsNullOrEmpty(name) && name.Trim().Length > 0 && !wantedLayers.Contains(name.Trim()))
            wantedLayers.Add(name.Trim());

var layerCounts = new Dictionary<string, int>();
var lineA = new List<XYZ>();
var lineB = new List<XYZ>();
var lineLayer = new List<string>();
int arcs = 0, splines = 0, notFlat = 0, tooShort = 0, blocks = 0, overflow = 0;
var readByLayer = new Dictionary<string, int>();
var arcsByLayer = new Dictionary<string, int>();
// THE COST IS BOUNDED BY maxRuns: a run is two edges and a few breaks and caps,
// so more than 40 pieces per run allowed is refused before any search is done.
var pieceLimit = Math.Min(Math.Max(maxRuns, 1) * 40, 100000);
var viewOnlyIn = "";

if (refused.Length == 0)
{
    Action<string, Dictionary<string, int>> bump = (key, table) =>
    {
        int was;
        table.TryGetValue(key, out was);
        table[key] = was + 1;
    };
    Action<string, XYZ, XYZ> take = (layer, a, b) =>
    {
        if (Math.Abs(a.Z - b.Z) > 1.0 / mmPerFoot) { notFlat++; return; }
        if (a.DistanceTo(b) < minLen) { tooShort++; return; }
        if (lineA.Count >= pieceLimit) { overflow++; return; }
        lineA.Add(new XYZ(a.X, a.Y, 0));
        lineB.Add(new XYZ(b.X, b.Y, 0));
        lineLayer.Add(layer);
        bump(layer, readByLayer);
    };

    foreach (var import in cads)
    {
        var options = new Options();
        if (import.ViewSpecific)
        {
            var owner = doc.GetElement(import.OwnerViewId) as View;
            if (owner != null) { options.View = owner; viewOnlyIn = owner.Name; }
        }
        var geometry = import.get_Geometry(options);
        if (geometry == null) continue;

        foreach (GeometryObject top in geometry)
        {
            var nested = top as GeometryInstance;
            if (nested == null) continue;

            // GetInstanceGeometry: the import's own placement is applied, so the
            // coordinates are where the linework sits in this model.
            foreach (GeometryObject item in nested.GetInstanceGeometry())
            {
                var style = doc.GetElement(item.GraphicsStyleId) as GraphicsStyle;
                var layer = style != null && style.GraphicsStyleCategory != null
                    ? style.GraphicsStyleCategory.Name : "(no layer)";
                bump(layer, layerCounts);
                if (!wantedLayers.Contains(layer)) continue;

                if (item is Line)
                {
                    var line = (Line)item;
                    take(layer, line.GetEndPoint(0), line.GetEndPoint(1));
                }
                else if (item is PolyLine)
                {
                    var points = ((PolyLine)item).GetCoordinates();
                    for (var i = 0; i < points.Count - 1; i++)
                        if (points[i].DistanceTo(points[i + 1]) > 1e-6) take(layer, points[i], points[i + 1]);
                }
                else if (item is Arc) { arcs++; bump(layer, arcsByLayer); }
                else if (item is Curve) splines++;
                else if (item is GeometryInstance) blocks++;
            }
        }
    }

    var survey = new List<string>();
    foreach (var pair in layerCounts.OrderBy(p => p.Key, StringComparer.Ordinal))
        survey.Add("'" + pair.Key + "' (" + pair.Value + ")");
    var surveyText = survey.Count == 0 ? "no layers with geometry were found" : string.Join(", ", survey.ToArray());

    var missing = wantedLayers.Where(l => !layerCounts.ContainsKey(l)).ToList();
    if (wantedLayers.Count == 0)
        refused = "no layer was named, so nothing was drawn. The layers and their object counts: " + surveyText
            + ". Name the layers to draw exactly as listed, including case";
    else if (missing.Count > 0 && viewOnlyIn.Length > 0)
        refused = "no layer called " + string.Join(", ", missing.Select(l => "'" + l + "'").ToArray()) + " came back from "
            + "this CAD - it is placed in view '" + viewOnlyIn + "' only, and Revit hands out only the layers SHOWN "
            + "there, so the layer may be in the drawing and turned off in that view (Visibility/Graphics, Imported "
            + "Categories). Layer names are matched exactly, including case. The layers shown: " + surveyText
            + ". Nothing was drawn";
    else if (missing.Count > 0)
        refused = "no layer called " + string.Join(", ", missing.Select(l => "'" + l + "'").ToArray())
            + " is in the CAD drawing - layer names are matched exactly, including case. The layers are: "
            + surveyText + ". Nothing was drawn";
    else if (overflow > 0)
        refused = "more than " + pieceLimit + " straight pieces are on the layers named - the most this reads for "
            + "maxRuns " + maxRuns + " (40 pieces a run). Name fewer layers, split the drawing, or raise maxRuns. "
            + "Nothing was drawn";

    foreach (var layer in wantedLayers)
    {
        int read, arcCount;
        readByLayer.TryGetValue(layer, out read);
        arcsByLayer.TryGetValue(layer, out arcCount);
        layersRead.Add("'" + layer + "': " + read + " straight piece(s) read" + (arcCount > 0 ? ", " + arcCount
            + " arc(s) skipped" : "") + ", taken as " + (doubleMode ? "double lines" : "single lines"));
    }

    if (arcs > 0) { skipped += arcs; skippedWhy.Add(arcs + " arc(s) - an arc is usually a radius elbow, which Revit "
        + "draws itself from the routing preferences; the straight lines on either side are joined instead"); }
    if (splines > 0) { skipped += splines; skippedWhy.Add(splines + " spline(s) or ellipse(s) - never approximated"); }
    if (notFlat > 0) { skipped += notFlat; skippedWhy.Add(notFlat + " line(s) whose two ends are at different heights"); }
    if (tooShort > 0) { skipped += tooShort; skippedWhy.Add(tooShort + " line(s) shorter than " + num(minLengthMm) + " mm"); }
    if (blocks > 0) { skipped += blocks; skippedWhy.Add(blocks + " block(s) inside the drawing on these layers - "
        + "not opened; explode them in CAD, or read their lines another way"); }
    if (refused.Length == 0 && lineA.Count == 0)
        refused = "the layers named hold no straight line this can draw - " + string.Join("; ", skippedWhy.ToArray())
            + ". Nothing was drawn";
}

// ---------------------------------------------------------------------------
// A grid, so every neighbour search looks only at the cells nearby
// ---------------------------------------------------------------------------

Func<int, int, long> cellOf = (cx, cy) => ((long)cx << 32) | (uint)cy;
// boxes are [minX, minY, maxX, maxY] in feet; size is the cell's side.
Func<List<double[]>, double, Dictionary<long, List<int>>> gridOf = (boxes, size) =>
{
    var grid = new Dictionary<long, List<int>>();
    for (var i = 0; i < boxes.Count; i++)
    {
        var b = boxes[i];
        if (b == null) continue;
        for (var cx = (int)Math.Floor(b[0] / size); cx <= (int)Math.Floor(b[2] / size); cx++)
            for (var cy = (int)Math.Floor(b[1] / size); cy <= (int)Math.Floor(b[3] / size); cy++)
            {
                List<int> here;
                var key = cellOf(cx, cy);
                if (!grid.TryGetValue(key, out here)) { here = new List<int>(); grid[key] = here; }
                here.Add(i);
            }
    }
    return grid;
};
Func<Dictionary<long, List<int>>, double, double[], List<int>> gridNear = (grid, size, box) =>
{
    var found = new HashSet<int>();
    for (var cx = (int)Math.Floor(box[0] / size); cx <= (int)Math.Floor(box[2] / size); cx++)
        for (var cy = (int)Math.Floor(box[1] / size); cy <= (int)Math.Floor(box[3] / size); cy++)
        {
            List<int> here;
            if (grid.TryGetValue(cellOf(cx, cy), out here)) foreach (var i in here) found.Add(i);
        }
    return found.ToList();
};
Func<double, double, double, double, double, double[]> boxAround = (x0, y0, x1, y1, pad) => new[]
{
    Math.Min(x0, x1) - pad, Math.Min(y0, y1) - pad, Math.Max(x0, x1) + pad, Math.Max(y0, y1) + pad
};

// ---------------------------------------------------------------------------
// Lines to centreline segments: [ax, ay, bx, by, width] in feet (width 0 single)
// ---------------------------------------------------------------------------

var seg = new List<double[]>();
var segDoubtful = new List<bool>();
var sinParallel = Math.Sin(Math.PI / 180.0);

if (refused.Length == 0)
{
    var n = lineA.Count;
    var layerIndex = lineLayer.Select(l => wantedLayers.IndexOf(l)).ToArray();
    var ux = new double[n]; var uy = new double[n]; var len = new double[n];
    for (var i = 0; i < n; i++)
    {
        var dx = lineB[i].X - lineA[i].X; var dy = lineB[i].Y - lineA[i].Y;
        len[i] = Math.Sqrt(dx * dx + dy * dy);
        ux[i] = dx / len[i]; uy[i] = dy / len[i];
    }

    if (!doubleMode)
    {
        for (var i = 0; i < n; i++)
        {
            seg.Add(new[] { lineA[i].X, lineA[i].Y, lineB[i].X, lineB[i].Y, 0.0 });
            segDoubtful.Add(false);
        }
    }
    else
    {
        // Signed offset of point (px,py) from line i, along its left normal.
        Func<int, double, double, double> offsetOf = (i, px, py) =>
            -uy[i] * (px - lineA[i].X) + ux[i] * (py - lineA[i].Y);
        Func<int, double, double, double> along = (i, px, py) =>
            ux[i] * (px - lineA[i].X) + uy[i] * (py - lineA[i].Y);

        // Every parallel neighbour of each line within the widest spacing, on its
        // own layer - looked for only in the grid cells around the line.
        var cell = maxW + 2 * tol;
        var lineBoxes = new List<double[]>();
        for (var i = 0; i < n; i++) lineBoxes.Add(boxAround(lineA[i].X, lineA[i].Y, lineB[i].X, lineB[i].Y, 0));
        var lineGrid = gridOf(lineBoxes, cell);

        var near = new List<int>[n];
        var nearOff = new List<double>[n];
        var nearLo = new List<double>[n];
        var nearHi = new List<double>[n];
        for (var i = 0; i < n; i++)
        {
            near[i] = new List<int>(); nearOff[i] = new List<double>(); nearLo[i] = new List<double>(); nearHi[i] = new List<double>();
            var b = lineBoxes[i];
            foreach (var j in gridNear(lineGrid, cell, new[] { b[0] - maxW - tol, b[1] - maxW - tol, b[2] + maxW + tol, b[3] + maxW + tol }))
            {
                if (i == j || layerIndex[i] != layerIndex[j]) continue;
                if (Math.Abs(ux[i] * uy[j] - uy[i] * ux[j]) > sinParallel) continue;
                var o1 = offsetOf(i, lineA[j].X, lineA[j].Y);
                var o2 = offsetOf(i, lineB[j].X, lineB[j].Y);
                if (Math.Abs(o1 - o2) > tol) continue;
                var off = (o1 + o2) / 2;
                if (Math.Abs(off) > maxW + tol) continue;
                var t1 = along(i, lineA[j].X, lineA[j].Y);
                var t2 = along(i, lineB[j].X, lineB[j].Y);
                var lo = Math.Max(0, Math.Min(t1, t2));
                var hi = Math.Min(len[i], Math.Max(t1, t2));
                if (hi - lo < minLen) continue;
                near[i].Add(j); nearOff[i].Add(off); nearLo[i].Add(lo); nearHi[i].Add(hi);
            }
        }

        // A DRAWN CENTRELINE: a line exactly midway between two lines that would
        // pair, and the only line between them, is not an edge. Left in, it pairs
        // with each edge as two half-width ducts. NOT when either outer line has a
        // possible partner on its FAR side: three edges equally spaced are then a
        // duct beside another duct's edge, not a duct and its centreline.
        Func<int, int, bool> partnerBeyond = (outer, k) =>
        {
            var toward = 0.0;
            for (var m = 0; m < near[outer].Count; m++) if (near[outer][m] == k) toward = nearOff[outer][m];
            if (toward == 0) return false;
            for (var m = 0; m < near[outer].Count; m++)
            {
                var o = nearOff[outer][m];
                if (Math.Sign(o) == Math.Sign(toward)) continue;
                if (Math.Abs(o) >= minW - tol && Math.Abs(o) <= maxW + tol) return true;
            }
            return false;
        };
        var centre = new bool[n];
        for (var k = 0; k < n; k++)
            for (var a = 0; a < near[k].Count && !centre[k]; a++)
                for (var b = 0; b < near[k].Count && !centre[k]; b++)
                {
                    double oa = nearOff[k][a], ob = nearOff[k][b];
                    if (oa >= 0 || ob <= 0 || Math.Abs(oa + ob) > tol) continue;
                    if (ob - oa < minW - tol || ob - oa > maxW + tol) continue;
                    var lo = Math.Max(nearLo[k][a], nearLo[k][b]); var hi = Math.Min(nearHi[k][a], nearHi[k][b]);
                    if (hi - lo < minLen) continue;
                    var alone = true;
                    for (var m = 0; m < near[k].Count && alone; m++)
                    {
                        if (m == a || m == b) continue;
                        var o = nearOff[k][m];
                        if (o <= oa + tol || o >= ob - tol) continue;
                        if (Math.Min(nearHi[k][m], hi) - Math.Max(nearLo[k][m], lo) >= minLen) alone = false;
                    }
                    if (alone && !partnerBeyond(near[k][a], k) && !partnerBeyond(near[k][b], k)) centre[k] = true;
                }
        var centres = centre.Count(c => c);
        if (centres > 0)
        {
            skipped += centres;
            skippedWhy.Add(centres + " line(s) lying exactly midway between the two edges of a duct, taken as its drawn "
                + "centreline and left out - not paired as an edge");
        }

        // Candidates: a plausible spacing, and no third parallel line between.
        var candI = new List<int>(); var candJ = new List<int>(); var candK = new List<int>();
        var candLoJ = new List<double>(); var candHiJ = new List<double>();
        var candOf = new List<int>[n];
        for (var i = 0; i < n; i++) candOf[i] = new List<int>();
        for (var i = 0; i < n; i++)
        {
            if (centre[i]) continue;
            for (var k = 0; k < near[i].Count; k++)
            {
                var j = near[i][k];
                if (j < i || centre[j]) continue;
                var off = nearOff[i][k];
                if (Math.Abs(off) < minW - tol || Math.Abs(off) > maxW + tol) continue;
                var between = false;
                for (var m = 0; m < near[i].Count && !between; m++)
                {
                    if (m == k || centre[near[i][m]]) continue;
                    var o = nearOff[i][m];
                    if (Math.Sign(o) != Math.Sign(off) || Math.Abs(o) <= tol || Math.Abs(off) - Math.Abs(o) <= tol) continue;
                    var shared = Math.Min(nearHi[i][m], nearHi[i][k]) - Math.Max(nearLo[i][m], nearLo[i][k]);
                    if (shared >= minLen) between = true;
                }
                if (between) continue;
                var lo = nearLo[i][k]; var hi = nearHi[i][k];
                var j0 = along(j, lineA[i].X + ux[i] * lo, lineA[i].Y + uy[i] * lo);
                var j1 = along(j, lineA[i].X + ux[i] * hi, lineA[i].Y + uy[i] * hi);
                candOf[i].Add(candI.Count); candOf[j].Add(candI.Count);
                candI.Add(i); candJ.Add(j); candK.Add(k);
                candLoJ.Add(Math.Min(j0, j1)); candHiJ.Add(Math.Max(j0, j1));
            }
        }

        var used = new List<double[]>[n];
        for (var i = 0; i < n; i++) used[i] = new List<double[]>();
        Func<int, double, double, bool> free = (i, lo, hi) =>
        {
            foreach (var u in used[i])
                if (Math.Min(u[1], hi) - Math.Max(u[0], lo) > tol) return false;
            return true;
        };
        // The candidate's stretch on one of its two lines.
        Func<int, int, double[]> stretchOn = (c, line) => candI[c] == line
            ? new[] { nearLo[candI[c]][candK[c]], nearHi[candI[c]][candK[c]] }
            : new[] { candLoJ[c], candHiJ[c] };
        Func<int, bool> candFree = c =>
        {
            var si = stretchOn(c, candI[c]); var sj = stretchOn(c, candJ[c]);
            return free(candI[c], si[0], si[1]) && free(candJ[c], sj[0], sj[1]);
        };
        var decided = new bool[candI.Count];
        // How many live choices a line has over this candidate's stretch of it.
        Func<int, int, int> choices = (c, line) =>
        {
            var s = stretchOn(c, line);
            var count = 0;
            foreach (var other in candOf[line])
            {
                if (decided[other] || !candFree(other)) continue;
                var o = stretchOn(other, line);
                if (Math.Min(o[1], s[1]) - Math.Max(o[0], s[0]) > tol) count++;
            }
            return count;
        };

        var order = Enumerable.Range(0, candI.Count).OrderBy(c => Math.Abs(nearOff[candI[c]][candK[c]])).ToList();
        var accepted = new List<int>();
        var acceptedDoubt = new List<bool>();
        var isAccepted = new bool[candI.Count];
        var acceptedOn = new List<int>[n];      // line -> index into `accepted`
        for (var i = 0; i < n; i++) acceptedOn[i] = new List<int>();
        var work = new Queue<int>(order);
        Action<int> reexamine = line =>
        {
            foreach (var c2 in candOf[line])
            {
                var far = candI[c2] == line ? candJ[c2] : candI[c2];
                foreach (var c3 in candOf[far]) if (!decided[c3]) work.Enqueue(c3);
                if (!decided[c2]) work.Enqueue(c2);
            }
        };
        Action<int, bool> accept = (c, doubt) =>
        {
            int i = candI[c], j = candJ[c];
            var si = stretchOn(c, i); var sj = stretchOn(c, j);
            used[i].Add(si); used[j].Add(sj);
            decided[c] = true; isAccepted[c] = true;
            acceptedOn[i].Add(accepted.Count); acceptedOn[j].Add(accepted.Count);
            accepted.Add(c); acceptedDoubt.Add(doubt);
            reexamine(i); reexamine(j);
        };

        // AS A CHAIN IS PAIRED: a line with one choice over a stretch takes it,
        // and every line near it is looked at again, until no line is left with
        // one choice; then the nearest pair left is taken and marked doubtful,
        // and the forcing starts again from there.
        var nextDoubt = 0;
        while (true)
        {
            while (work.Count > 0)
            {
                var c = work.Dequeue();
                if (decided[c]) continue;
                if (!candFree(c)) { decided[c] = true; continue; }
                if (choices(c, candI[c]) == 1 || choices(c, candJ[c]) == 1) accept(c, false);
            }
            while (nextDoubt < order.Count && (decided[order[nextDoubt]] || !candFree(order[nextDoubt]))) nextDoubt++;
            if (nextDoubt >= order.Count) break;
            accept(order[nextDoubt], true);
        }

        // A pair beside a parallel line that lost its only partner to it is doubtful too.
        for (var c = 0; c < candI.Count; c++)
        {
            if (isAccepted[c]) continue;
            int i = candI[c], j = candJ[c];
            var si = stretchOn(c, i); var sj = stretchOn(c, j);
            var iFree = free(i, si[0], si[1]); var jFree = free(j, sj[0], sj[1]);
            if (iFree == jFree) continue;
            var takenLine = iFree ? j : i; var st = iFree ? sj : si;
            foreach (var a in acceptedOn[takenLine])
            {
                var o = stretchOn(accepted[a], takenLine);
                if (Math.Min(o[1], st[1]) - Math.Max(o[0], st[0]) > tol) acceptedDoubt[a] = true;
            }
        }

        var paired = new bool[n];
        var pairedLength = 0.0;
        var totalLength = 0.0;
        for (var i = 0; i < n; i++) if (!centre[i]) totalLength += len[i];
        for (var a = 0; a < accepted.Count; a++)
        {
            var c = accepted[a];
            int i = candI[c], k = candK[c];
            double lo = nearLo[i][k], hi = nearHi[i][k], off = nearOff[i][k];
            paired[i] = true; paired[candJ[c]] = true;
            pairedLength += 2 * (hi - lo);
            var nx = -uy[i] * off / 2; var ny = ux[i] * off / 2;
            seg.Add(new[] { lineA[i].X + ux[i] * lo + nx, lineA[i].Y + uy[i] * lo + ny,
                            lineA[i].X + ux[i] * hi + nx, lineA[i].Y + uy[i] * hi + ny, Math.Abs(off) });
            segDoubtful.Add(acceptedDoubt[a]);
        }

        var lonely = Enumerable.Range(0, n).Count(i => !paired[i] && !centre[i]);
        if (lonely > 0)
        {
            skipped += lonely;
            skippedWhy.Add(lonely + " line(s) found no parallel partner " + num(minWidthMm) + " to " + num(maxWidthMm)
                + " mm away - end caps, fitting outlines and text underlines are usually these");
        }
        if (totalLength > 0 && pairedLength / totalLength < 0.5)
            findings.Add("only " + Math.Round(100 * pairedLength / totalLength) + "% of the length on these layers paired "
                + "as double lines - if the drawing is single-line, run it again with mode single");
    }
}

// ---------------------------------------------------------------------------
// One straight line, one width, gaps under bridgeMm: one run
// ---------------------------------------------------------------------------

var alive = new List<bool>();
foreach (var s in seg) alive.Add(true);
Func<double[], double> segLen = s => Math.Sqrt((s[2] - s[0]) * (s[2] - s[0]) + (s[3] - s[1]) * (s[3] - s[1]));
var sameWidth = Math.Max(tol, step / 2 / mmPerFoot);
var segCell = Math.Max(bridge, maxW) + 2 * tol;
var segGrid = new Dictionary<long, List<int>>();
Action rebuildSegGrid = () =>
{
    var boxes = new List<double[]>();
    for (var i = 0; i < seg.Count; i++) boxes.Add(boxAround(seg[i][0], seg[i][1], seg[i][2], seg[i][3], 0));
    segGrid = gridOf(boxes, segCell);
};
Func<int, int, bool> inLineWith = (c, i) =>
{
    if (i < 0 || i >= seg.Count) return false;
    var a = seg[i]; var b = seg[c];
    var la = segLen(a);
    var ax = (a[2] - a[0]) / la; var ay = (a[3] - a[1]) / la;
    return Math.Abs(-ay * (b[0] - a[0]) + ax * (b[1] - a[1])) <= tol
        && Math.Abs(-ay * (b[2] - a[0]) + ax * (b[3] - a[1])) <= tol;
};
// Does (px,py) lie on the inside of a piece other than i and j? Dead pieces are
// asked too: they are still linework, now part of the run they were joined to.
Func<double, double, int, int, bool> onAnotherRun = (px, py, i, j) =>
{
    foreach (var c in gridNear(segGrid, segCell, new[] { px - tol, py - tol, px + tol, py + tol }))
    {
        if (c == i || c == j) continue;
        var s = seg[c]; var l = segLen(s);
        var cx = (s[2] - s[0]) / l; var cy = (s[3] - s[1]) / l;
        // A dead piece in line with i or j is part of that run, not another one.
        if (!alive[c] && (inLineWith(c, i) || inLineWith(c, j))) continue;
        var t = cx * (px - s[0]) + cy * (py - s[1]);
        if (t <= tol || t >= l - tol) continue;
        if (Math.Abs(-cy * (px - s[0]) + cx * (py - s[1])) <= tol) return true;
    }
    return false;
};

if (refused.Length == 0)
{
    var changed = true;
    var passes = 0;
    while (changed && passes < 50)
    {
        changed = false;
        passes++;
        rebuildSegGrid();
        for (var i = 0; i < seg.Count; i++)
        {
            if (!alive[i]) continue;
            var pad = bridge + 2 * tol;
            foreach (var j in gridNear(segGrid, segCell, boxAround(seg[i][0], seg[i][1], seg[i][2], seg[i][3], pad)))
            {
                if (i == j || !alive[i] || !alive[j]) continue;
                var a = seg[i]; var b = seg[j];
                var la = segLen(a);
                var aux = (a[2] - a[0]) / la; var auy = (a[3] - a[1]) / la;
                var lb = segLen(b);
                var bux = (b[2] - b[0]) / lb; var buy = (b[3] - b[1]) / lb;
                if (Math.Abs(aux * buy - auy * bux) > sinParallel) continue;
                if (Math.Abs(a[4] - b[4]) > sameWidth) continue;
                var o1 = -auy * (b[0] - a[0]) + aux * (b[1] - a[1]);
                var o2 = -auy * (b[2] - a[0]) + aux * (b[3] - a[1]);
                if (Math.Abs(o1) > tol || Math.Abs(o2) > tol) continue;
                var t1 = aux * (b[0] - a[0]) + auy * (b[1] - a[1]);
                var t2 = aux * (b[2] - a[0]) + auy * (b[3] - a[1]);
                var lo = Math.Min(t1, t2); var hi = Math.Max(t1, t2);
                var gap = Math.Max(lo - la, -hi);
                if (gap > bridge + tol) continue;
                // A GAP THAT SITS ON ANOTHER RUN IS A JOINT OR A CROSSING, NOT A
                // BREAK: two branch lines touching a main from either side are a
                // cross; one run stopping either side of another is a crossing.
                if (gap > -tol)
                {
                    var g = lo > la ? (la + lo) / 2 : hi / 2;
                    if (onAnotherRun(a[0] + aux * g, a[1] + auy * g, i, j)) continue;
                }
                var start = Math.Min(0, lo); var finish = Math.Max(la, hi);
                var width = la >= lb ? a[4] : b[4];
                seg[i] = new[] { a[0] + aux * start, a[1] + auy * start, a[0] + aux * finish, a[1] + auy * finish, width };
                segDoubtful[i] = segDoubtful[i] || segDoubtful[j];
                alive[j] = false;
                changed = true;
            }
        }
    }
    rebuildSegGrid();
}

// ---------------------------------------------------------------------------
// Joints. An END is segment * 2 + (0 at the start, 1 at the finish).
// ---------------------------------------------------------------------------

var live = Enumerable.Range(0, seg.Count).Where(i => alive[i]).ToList();
var endNode = new Dictionary<int, int>();
var nodeX = new List<double>();
var nodeY = new List<double>();
var nodeEnds = new List<List<int>>();
var splitSeg = new List<int>();
var splitNode = new List<int>();
var brokenCrossings = new List<string>();
// TAPS: the duct or pipe type's routing preference says how a branch joins a
// main. Taps: the main is NOT cut and the branch is joined by a takeoff, as
// FIT_MEP_JOINTS does - its "tap, not tee" (2026-09-23).
var tapsWanted = refused.Length == 0 && runType.RoutingPreferenceManager != null
    && runType.RoutingPreferenceManager.PreferredJunctionType == PreferredJunctionType.Tap;

Func<int, double[]> endPoint = e =>
{
    var s = seg[e / 2];
    return e % 2 == 0 ? new[] { s[0], s[1] } : new[] { s[2], s[3] };
};
Func<int, double[]> outward = e =>
{
    var s = seg[e / 2];
    var l = segLen(s);
    var dx = (s[2] - s[0]) / l; var dy = (s[3] - s[1]) / l;
    return e % 2 == 0 ? new[] { -dx, -dy } : new[] { dx, dy };
};
Func<int, int, bool> nodeHasSeg = (node, s) => nodeEnds[node].Any(e => e / 2 == s) || splitSeg
    .Where((x, k) => splitNode[k] == node).Contains(s);
Func<double, double, List<int>, int> nodeNear = (x, y, segs) =>
{
    for (var k = 0; k < nodeX.Count; k++)
    {
        var dx = nodeX[k] - x; var dy = nodeY[k] - y;
        if (Math.Sqrt(dx * dx + dy * dy) <= tol && !segs.Any(s => nodeHasSeg(k, s))) return k;
    }
    return -1;
};

if (refused.Length == 0)
{
    var ends = new List<int>();
    foreach (var s in live) { ends.Add(s * 2); ends.Add(s * 2 + 1); }
    var endBoxes = new List<double[]>();
    for (var e = 0; e < seg.Count * 2; e++) endBoxes.Add(null);
    foreach (var e in ends) { var p = endPoint(e); endBoxes[e] = new[] { p[0], p[1], p[0], p[1] }; }
    var endCell = 2 * (Math.Max(bridge, maxW / 2) + tol) + tol;
    var endGrid = gridOf(endBoxes, endCell);
    var sinCorner = Math.Sin(5 * Math.PI / 180.0);
    var sinBranch = Math.Sin(20 * Math.PI / 180.0);

    // Does the stretch from (x0,y0) to (x1,y1) cross the inside of a run other
    // than s1 and s2? An end is never carried across another run to a joint.
    Func<double, double, double, double, int, int, bool> crossesARun = (x0, y0, x1, y1, s1, s2) =>
    {
        var px = x1 - x0; var py = y1 - y0;
        var lp = Math.Sqrt(px * px + py * py);
        if (lp <= tol) return false;
        foreach (var c in gridNear(segGrid, segCell, boxAround(x0, y0, x1, y1, tol)))
        {
            if (c == s1 || c == s2 || !alive[c]) continue;
            var q = seg[c];
            var qx = q[2] - q[0]; var qy = q[3] - q[1];
            var den = px * qy - py * qx;
            if (Math.Abs(den) < 1e-12) continue;
            var gx = q[0] - x0; var gy = q[1] - y0;
            var u = (gx * qy - gy * qx) / den; var v = (gx * py - gy * px) / den;
            var lq = Math.Sqrt(qx * qx + qy * qy);
            if (u * lp > tol && u * lp < lp - tol && v * lq > tol && v * lq < lq - tol) return true;
        }
        return false;
    };
    // A joint whose stretch lies across, or ends on, a third run is a crossing
    // drawn with a break - said, and never joined.
    Func<double, double, double, double, int, int, bool> overARun = (x0, y0, x1, y1, s1, s2) =>
        crossesARun(x0, y0, x1, y1, s1, s2) || onAnotherRun((x0 + x1) / 2, (y0 + y1) / 2, s1, s2);
    Action<double, double> brokenAt = (x, y) =>
    {
        var where = at(x, y);
        if (!brokenCrossings.Contains(where)) brokenCrossings.Add(where);
    };

    // EVERY POSSIBLE JOINT, then the nearest first - so a branch end 10 mm
    // from a main tees into it rather than turning a corner a metre away.
    // kind 0 corner (e1, e2), 1 tee (end e1 onto run e2), 2 in line (e1, e2).
    var jKind = new List<int>(); var jE1 = new List<int>(); var jE2 = new List<int>();
    var jX = new List<double>(); var jY = new List<double>(); var jCost = new List<double>();

    foreach (var e1 in ends)
    {
        var a = endPoint(e1);
        foreach (var e2 in gridNear(endGrid, endCell, new[] { a[0] - endCell, a[1] - endCell, a[0] + endCell, a[1] + endCell }))
        {
            if (e2 <= e1 || e1 / 2 == e2 / 2) continue;
            var b = endPoint(e2);
            var d1 = outward(e1); var d2 = outward(e2);
            var w1 = seg[e1 / 2][4]; var w2 = seg[e2 / 2][4];
            var reach1 = Math.Max(bridge, w2 / 2) + tol;
            var reach2 = Math.Max(bridge, w1 / 2) + tol;
            var gx = b[0] - a[0]; var gy = b[1] - a[1];
            if (Math.Sqrt(gx * gx + gy * gy) > reach1 + reach2) continue;
            var cross = d1[0] * d2[1] - d1[1] * d2[0];
            if (Math.Abs(cross) < sinCorner)
            {
                // IN LINE, FACING: a width change (same widths were joined into one run).
                if (d1[0] * d2[0] + d1[1] * d2[1] > -0.9998) continue;
                var lateral = Math.Abs(d1[0] * gy - d1[1] * gx);
                var gap = d1[0] * gx + d1[1] * gy;
                if (lateral > tol || gap < -tol || gap > bridge + tol) continue;
                jKind.Add(2); jE1.Add(e1); jE2.Add(e2);
                jX.Add((a[0] + b[0]) / 2); jY.Add((a[1] + b[1]) / 2); jCost.Add(Math.Max(gap, 0));
                continue;
            }
            // a + d1*t1 = b + d2*t2. Positive t carries an end on; negative
            // trims it back - only a small overshoot, never into the run.
            var t1 = (gx * d2[1] - gy * d2[0]) / cross;
            var t2 = (gx * d1[1] - gy * d1[0]) / cross;
            var l1 = segLen(seg[e1 / 2]); var l2 = segLen(seg[e2 / 2]);
            var trim1 = Math.Min(2 * tol + w2 / 2, l1 - minLen);
            var trim2 = Math.Min(2 * tol + w1 / 2, l2 - minLen);
            if (t1 > reach1 || t2 > reach2 || t1 < -trim1 || t2 < -trim2) continue;
            jKind.Add(0); jE1.Add(e1); jE2.Add(e2);
            jX.Add(a[0] + d1[0] * t1); jY.Add(a[1] + d1[1] * t1);
            jCost.Add(Math.Abs(t1) + Math.Abs(t2));
        }
    }

    foreach (var e in ends)
    {
        var a = endPoint(e); var d = outward(e);
        foreach (var m in gridNear(segGrid, segCell, new[] { a[0] - maxW, a[1] - maxW, a[0] + maxW, a[1] + maxW }))
        {
            if (m == e / 2 || !alive[m]) continue;
            var s = seg[m]; var lm = segLen(s);
            var mx = (s[2] - s[0]) / lm; var my = (s[3] - s[1]) / lm;
            var cross = d[0] * my - d[1] * mx;
            if (Math.Abs(cross) < sinBranch) continue;
            var gx = s[0] - a[0]; var gy = s[1] - a[1];
            var t = (gx * my - gy * mx) / cross;          // along the branch, to the main's axis
            var tm = (gx * d[1] - gy * d[0]) / cross;     // along the main, from its start
            var reach = s[4] / 2 + 2 * tol;
            if (t > reach || t < -reach) continue;
            if (tm < minLen || tm > lm - minLen) continue;   // both halves of the main long enough to draw
            jKind.Add(1); jE1.Add(e); jE2.Add(m);
            jX.Add(a[0] + d[0] * t); jY.Add(a[1] + d[1] * t); jCost.Add(Math.Abs(t));
        }
    }

    foreach (var c in Enumerable.Range(0, jKind.Count).OrderBy(c => jCost[c]).ThenBy(c => jKind[c]))
    {
        int e1 = jE1[c], e2 = jE2[c];
        if (jKind[c] == 1)
        {
            if (endNode.ContainsKey(e1)) continue;
            var a = endPoint(e1);
            if (crossesARun(a[0], a[1], jX[c], jY[c], e1 / 2, e2)
                || onAnotherRun((a[0] + jX[c]) / 2, (a[1] + jY[c]) / 2, e1 / 2, e2)) { brokenAt(jX[c], jY[c]); continue; }
            var node = -1;
            for (var k = 0; k < splitSeg.Count && node < 0; k++)
            {
                if (splitSeg[k] != e2) continue;
                var dx = nodeX[splitNode[k]] - jX[c]; var dy = nodeY[splitNode[k]] - jY[c];
                if (Math.Sqrt(dx * dx + dy * dy) <= 2 * tol && !nodeHasSeg(splitNode[k], e1 / 2)) node = splitNode[k];
            }
            if (node < 0)
            {
                node = nodeX.Count;
                nodeX.Add(jX[c]); nodeY.Add(jY[c]); nodeEnds.Add(new List<int>());
                splitSeg.Add(e2); splitNode.Add(node);
            }
            nodeEnds[node].Add(e1);
            endNode[e1] = node;
            continue;
        }

        var has1 = endNode.ContainsKey(e1); var has2 = endNode.ContainsKey(e2);
        if (has1 && has2) continue;
        var p1 = endPoint(e1); var p2 = endPoint(e2);
        if (jKind[c] == 2 ? overARun(p1[0], p1[1], p2[0], p2[1], e1 / 2, e2 / 2)
                          : crossesARun(p1[0], p1[1], jX[c], jY[c], e1 / 2, e2 / 2)
                            || crossesARun(p2[0], p2[1], jX[c], jY[c], e1 / 2, e2 / 2))
        {
            if (jKind[c] == 2 && !has1 && !has2) brokenAt(jX[c], jY[c]);
            continue;
        }
        if (!has1 && !has2)
        {
            var node = jKind[c] == 0 ? nodeNear(jX[c], jY[c], new List<int> { e1 / 2, e2 / 2 }) : -1;
            if (node < 0)
            {
                node = nodeX.Count;
                nodeX.Add(jX[c]); nodeY.Add(jY[c]); nodeEnds.Add(new List<int>());
            }
            nodeEnds[node].Add(e1); nodeEnds[node].Add(e2);
            endNode[e1] = node; endNode[e2] = node;
            continue;
        }
        if (jKind[c] != 0) continue;
        // A third end arriving at a corner already made - three ends at one point.
        var held = has1 ? endNode[e1] : endNode[e2];
        var other = has1 ? e2 : e1;
        var ox = nodeX[held] - jX[c]; var oy = nodeY[held] - jY[c];
        if (Math.Sqrt(ox * ox + oy * oy) > 2 * tol || nodeHasSeg(held, other / 2)) continue;
        nodeEnds[held].Add(other);
        endNode[other] = held;
    }

    // A break later bridged by a real joint is not a crossing.
    brokenCrossings.RemoveAll(w => Enumerable.Range(0, nodeX.Count).Any(k => at(nodeX[k], nodeY[k]) == w));
}

// ---------------------------------------------------------------------------
// Runs: each segment from its (moved) ends, split at the tees on it - never at
// a tap, where the main runs on uncut
// ---------------------------------------------------------------------------

var runX0 = new List<double>(); var runY0 = new List<double>();
var runX1 = new List<double>(); var runY1 = new List<double>();
var runW = new List<double>(); var runN0 = new List<int>(); var runN1 = new List<int>();
var runDoubtful = new List<bool>(); var runSeg = new List<int>();
var nodeTapMain = new Dictionary<int, int>();   // tap node -> the run it taps into
var runsTooShort = 0;

if (refused.Length == 0)
{
    foreach (var s in live)
    {
        var src = seg[s];
        var x0 = src[0]; var y0 = src[1]; var x1 = src[2]; var y1 = src[3];
        int n0 = -1, n1 = -1;
        if (endNode.ContainsKey(s * 2)) { n0 = endNode[s * 2]; x0 = nodeX[n0]; y0 = nodeY[n0]; }
        if (endNode.ContainsKey(s * 2 + 1)) { n1 = endNode[s * 2 + 1]; x1 = nodeX[n1]; y1 = nodeY[n1]; }
        var l = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0));
        if (l < 1e-9) { runsTooShort++; continue; }
        var dx = (x1 - x0) / l; var dy = (y1 - y0) / l;

        var cuts = new List<double[]>();   // [t, node]
        for (var k = 0; k < splitSeg.Count; k++)
        {
            if (splitSeg[k] != s) continue;
            var node = splitNode[k];
            var t = dx * (nodeX[node] - x0) + dy * (nodeY[node] - y0);
            if (t > tol && t < l - tol) cuts.Add(new[] { t, (double)node });
        }
        cuts = cuts.OrderBy(c => c[0]).ToList();

        var px = x0; var py = y0; var pn = n0;
        if (!tapsWanted)
            foreach (var cut in cuts)
            {
                var node = (int)cut[1];
                runX0.Add(px); runY0.Add(py); runX1.Add(nodeX[node]); runY1.Add(nodeY[node]);
                runW.Add(src[4]); runN0.Add(pn); runN1.Add(node); runDoubtful.Add(segDoubtful[s]); runSeg.Add(s);
                px = nodeX[node]; py = nodeY[node]; pn = node;
            }
        runX0.Add(px); runY0.Add(py); runX1.Add(x1); runY1.Add(y1);
        runW.Add(src[4]); runN0.Add(pn); runN1.Add(n1); runDoubtful.Add(segDoubtful[s]); runSeg.Add(s);
    }

    // A run trimmed below the shortest length is dropped, and said.
    for (var r = runX0.Count - 1; r >= 0; r--)
    {
        var l = Math.Sqrt((runX1[r] - runX0[r]) * (runX1[r] - runX0[r]) + (runY1[r] - runY0[r]) * (runY1[r] - runY0[r]));
        if (l >= minLen) continue;
        runX0.RemoveAt(r); runY0.RemoveAt(r); runX1.RemoveAt(r); runY1.RemoveAt(r);
        runW.RemoveAt(r); runN0.RemoveAt(r); runN1.RemoveAt(r); runDoubtful.RemoveAt(r); runSeg.RemoveAt(r);
        runsTooShort++;
    }
    if (tapsWanted)
        for (var k = 0; k < splitSeg.Count; k++)
        {
            var main = runSeg.IndexOf(splitSeg[k]);
            if (main >= 0) nodeTapMain[splitNode[k]] = main;
        }
    if (runsTooShort > 0)
    {
        skipped += runsTooShort;
        skippedWhy.Add(runsTooShort + " run(s) shorter than " + num(minLengthMm) + " mm once their ends were joined");
    }

    if (runX0.Count == 0)
        refused = "nothing on the layers named makes a run" + (doubleMode ? " - no two lines paired as a double-line "
            + "duct (mode " + modeWord + ")" : "") + ". " + string.Join("; ", skippedWhy.ToArray()) + ". Nothing was drawn";
    else if (runX0.Count > maxRuns)
        refused = runX0.Count + " runs were found on the layers named - more than the " + maxRuns + " this draws in "
            + "one call (maxRuns). Name fewer layers, or raise maxRuns. Nothing was drawn";
}

// Each node's runs, by run index and which end.
var runsAt = new Dictionary<int, List<int>>();   // node -> run * 2 + end
if (refused.Length == 0)
    for (var r = 0; r < runX0.Count; r++)
    {
        if (runN0[r] >= 0) { if (!runsAt.ContainsKey(runN0[r])) runsAt[runN0[r]] = new List<int>(); runsAt[runN0[r]].Add(r * 2); }
        if (runN1[r] >= 0) { if (!runsAt.ContainsKey(runN1[r])) runsAt[runN1[r]] = new List<int>(); runsAt[runN1[r]].Add(r * 2 + 1); }
    }

// The direction a run leaves a node along, for the angle of a two-way joint.
Func<int, double[]> leaving = end =>
{
    var r = end / 2;
    return end % 2 == 0 ? new[] { runX1[r] - runX0[r], runY1[r] - runY0[r] } : new[] { runX0[r] - runX1[r], runY0[r] - runY1[r] };
};
Func<List<int>, bool> inLine = pair =>
{
    var a = leaving(pair[0]); var b = leaving(pair[1]);
    return (a[0] * b[0] + a[1] * b[1]) / Math.Sqrt((a[0] * a[0] + a[1] * a[1]) * (b[0] * b[0] + b[1] * b[1])) < -0.9998;
};

// ---------------------------------------------------------------------------
// Sizes: the nearest hint, else carried across an elbow, else the default
// ---------------------------------------------------------------------------

var runHint = new int[runX0.Count];
var runCarried = new bool[runX0.Count];
var hintUsed = new bool[hintSize.Count];
var runSize = new double[runX0.Count][];
var runFrom = new string[runX0.Count];

if (refused.Length == 0)
{
    // EACH HINT BELONGS TO THE ONE RUN NEAREST IT - a tag beside one duct never
    // sizes the duct next to it. Of several near one run, the nearest wins and
    // the others are listed.
    for (var r = 0; r < runX0.Count; r++) runHint[r] = -1;
    var runHintDistance = new double[runX0.Count];
    var hintRun = new int[hintSize.Count];
    for (var h = 0; h < hintSize.Count; h++)
    {
        hintRun[h] = -1;
        var best = reachHint;
        for (var r = 0; r < runX0.Count; r++)
        {
            var lx = runX1[r] - runX0[r]; var ly = runY1[r] - runY0[r];
            var t = Math.Max(0, Math.Min(1, ((hintX[h] - runX0[r]) * lx + (hintY[h] - runY0[r]) * ly) / (lx * lx + ly * ly)));
            var cx = runX0[r] + lx * t - hintX[h]; var cy = runY0[r] + ly * t - hintY[h];
            var d = Math.Sqrt(cx * cx + cy * cy);
            if (d <= best) { best = d; hintRun[h] = r; }
        }
        if (hintRun[h] < 0) continue;
        var rr = hintRun[h];
        if (runHint[rr] < 0 || best < runHintDistance[rr]) { runHint[rr] = h; runHintDistance[rr] = best; }
    }
    for (var h = 0; h < hintSize.Count; h++)
    {
        if (hintRun[h] < 0) continue;
        var rr = hintRun[h];
        if (runHint[rr] == h) { hintUsed[h] = true; continue; }
        sizeConflicts.Add("the hint " + hintText[h] + " is nearest run " + (rr + 1) + " " + at(runX0[rr], runY0[rr]) + " to "
            + at(runX1[rr], runY1[rr]) + ", which has the nearer hint " + hintText[runHint[rr]] + " - " + hintText[h]
            + " was NOT used; if it labels another run, give it that run's middle");
    }

    // A run with no hint takes one from a tagged run it continues across a
    // two-way joint AT AN ANGLE - an elbow; a size does not change at a plain
    // bend. Never across an in-line joint, which is a transition.
    var queue = new Queue<int>(Enumerable.Range(0, runX0.Count).Where(r => runHint[r] >= 0));
    while (queue.Count > 0)
    {
        var r = queue.Dequeue();
        foreach (var node in new[] { runN0[r], runN1[r] })
        {
            if (node < 0 || !runsAt.ContainsKey(node) || runsAt[node].Count != 2 || inLine(runsAt[node])) continue;
            foreach (var end in runsAt[node])
            {
                var o = end / 2;
                if (o == r || runHint[o] >= 0) continue;
                runHint[o] = runHint[r];
                runCarried[o] = true;
                queue.Enqueue(o);
            }
        }
    }

    for (var r = 0; r < runX0.Count; r++)
    {
        var where = "run " + (r + 1) + " " + at(runX0[r], runY0[r]) + " to " + at(runX1[r], runY1[r]);
        var hint = runHint[r] >= 0 ? hintSize[runHint[r]] : null;
        var hintName = runHint[r] >= 0 ? hintText[runHint[r]] : "";
        var spacingMm = runW[r] * mmPerFoot;
        var rounded = Math.Max(step, Math.Round(spacingMm / step) * step);

        if (hint != null && (isRound ? hint.Length != 1 : hint.Length != 2))
        {
            if (!runCarried[r])
                sizeConflicts.Add(where + ": the hint " + hintName + " is " + (hint.Length == 2 ? "width x height" : "a diameter")
                    + " and \"" + runType.Name + "\" is " + (isRound ? "round" : "not round") + " - the hint was not used");
            hint = null;
        }

        if (kind == "pipe")
        {
            if (hint != null) { runSize[r] = new[] { hint[0] }; runFrom[r] = runCarried[r] ? "carried from " + hintName : "hint " + hintName; }
            else
            {
                runSize[r] = new[] { defaultParsed[0] };
                runFrom[r] = "default";
                runsWithoutSize.Add(where + ": the default " + sizeText(runSize[r]) + (doubleMode
                    ? " (the lines are " + num(Math.Round(spacingMm)) + " mm apart - a drawn outside diameter, never used as a size)" : ""));
            }
        }
        else if (isRound)
        {
            if (hint != null && (!doubleMode || !runCarried[r]))
            {
                runSize[r] = new[] { hint[0] };
                runFrom[r] = runCarried[r] ? "carried from " + hintName : "hint " + hintName;
                if (doubleMode && Math.Abs(hint[0] - spacingMm) > Math.Max(step / 2, toleranceMm))
                    sizeConflicts.Add(where + ": the hint " + hintName + " against lines " + num(Math.Round(spacingMm))
                        + " mm apart - the hint was used");
            }
            else if (doubleMode) { runSize[r] = new[] { rounded }; runFrom[r] = "spacing " + num(Math.Round(spacingMm)) + " mm"; }
            else
            {
                runSize[r] = new[] { defaultParsed[0] };
                runFrom[r] = "default";
                runsWithoutSize.Add(where + ": the default " + sizeText(runSize[r]));
            }
        }
        else
        {
            if (hint != null && (!doubleMode || !runCarried[r]))
            {
                runSize[r] = new[] { hint[0], hint[1] };
                runFrom[r] = runCarried[r] ? "carried from " + hintName : "hint " + hintName;
                if (doubleMode && Math.Abs(hint[0] - spacingMm) > Math.Max(step / 2, toleranceMm))
                    sizeConflicts.Add(where + ": the hint " + hintName + " against lines " + num(Math.Round(spacingMm))
                        + " mm apart - the hint was used" + (Math.Abs(hint[1] - spacingMm) <= Math.Max(step / 2, toleranceMm)
                        ? ". Its HEIGHT matches the spacing: the tag may be written height x width" : ""));
            }
            else if (doubleMode)
            {
                runSize[r] = new[] { rounded, hint != null ? hint[1] : defaultParsed[1] };
                runFrom[r] = "width from the spacing, height " + (hint != null ? "carried from " + hintName : "default");
                if (hint == null)
                    runsWithoutSize.Add(where + ": width " + num(rounded) + " from the lines, height " + num(defaultParsed[1])
                        + " from the default");
            }
            else
            {
                runSize[r] = new[] { defaultParsed[0], defaultParsed[1] };
                runFrom[r] = "default";
                runsWithoutSize.Add(where + ": the default " + sizeText(runSize[r]));
            }
        }
    }

    var unused = Enumerable.Range(0, hintSize.Count).Where(h => hintRun[h] < 0).Select(h => hintText[h]).ToList();
    if (unused.Count > 0)
        findings.Add(unused.Count + " size hint(s) are within " + num(hintReachMm) + " mm of no run and were not used: "
            + string.Join(", ", unused.Take(20).ToArray()) + " - check their coordinates against the runs listed");
}

// ---------------------------------------------------------------------------
// What was found - also the whole answer in a preview
// ---------------------------------------------------------------------------

int elbowsFound = 0, teesFound = 0, crossesFound = 0, inLineFound = 0, tapsFound = 0, openEnds = 0;
if (refused.Length == 0)
{
    foreach (var pair in runsAt)
    {
        if (nodeTapMain.ContainsKey(pair.Key)) { tapsFound += pair.Value.Count; continue; }
        var count = pair.Value.Count;
        if (count == 2) { if (inLine(pair.Value)) inLineFound++; else elbowsFound++; }
        else if (count == 3) teesFound++;
        else if (count == 4) crossesFound++;
    }
    // An end meeting nothing, or meeting a joint no other run reached.
    Func<int, bool> openAt = node => node < 0 || (!nodeTapMain.ContainsKey(node) && runsAt[node].Count < 2);
    for (var r = 0; r < runX0.Count; r++)
    {
        if (openAt(runN0[r])) openEnds++;
        if (openAt(runN1[r])) openEnds++;
    }

    // Runs that cross without meeting - drawn at one height, they clash.
    var crossings = new List<string>();
    for (var p = 0; p < runX0.Count; p++)
        for (var q = p + 1; q < runX0.Count; q++)
        {
            double px = runX1[p] - runX0[p], py = runY1[p] - runY0[p];
            double qx = runX1[q] - runX0[q], qy = runY1[q] - runY0[q];
            var den = px * qy - py * qx;
            if (Math.Abs(den) < 1e-9) continue;
            var gx = runX0[q] - runX0[p]; var gy = runY0[q] - runY0[p];
            var s = (gx * qy - gy * qx) / den; var t = (gx * py - gy * px) / den;
            var lp = Math.Sqrt(px * px + py * py); var lq = Math.Sqrt(qx * qx + qy * qy);
            if (s * lp <= tol || s * lp >= lp - tol || t * lq <= tol || t * lq >= lq - tol) continue;
            // A tap sits on the main's axis inside it - a joint, not a clash.
            var hx = runX0[p] + px * s; var hy = runY0[p] + py * s;
            if (nodeTapMain.Keys.Any(k => Math.Abs(nodeX[k] - hx) <= tol && Math.Abs(nodeY[k] - hy) <= tol)) continue;
            if (crossings.Count < 10) crossings.Add(at(hx, hy));
            else crossings.Add("");
        }
    if (crossings.Count > 0)
        findings.Add(crossings.Count + " place(s) where two runs cross without meeting - they are drawn at one height "
            + "and clash there; one usually goes over the other: " + string.Join(", ", crossings.Where(c => c.Length > 0).ToArray()));
    if (brokenCrossings.Count > 0)
        findings.Add(brokenCrossings.Count + " crossing(s) drawn with a break - a run stopping either side of another, "
            + "or short of it - left unjoined, each end open; one usually goes over the other: "
            + string.Join(", ", brokenCrossings.Take(10).ToArray()));

    var doubtfulRuns = Enumerable.Range(0, runX0.Count).Where(r => runDoubtful[r]).ToList();
    if (doubtfulRuns.Count > 0)
        findings.Add(doubtfulRuns.Count + " run(s) come from a DOUBTFUL pairing of parallel lines - other parallel lines "
            + "close by could have been paired another way (ducts side by side, a stray line); check them in the "
            + "picture: runs " + string.Join(", ", doubtfulRuns.Take(30).Select(r => (r + 1).ToString()).ToArray()));

    double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
    for (var i = 0; i < lineA.Count; i++)
    {
        minX = Math.Min(minX, Math.Min(lineA[i].X, lineB[i].X)); maxX = Math.Max(maxX, Math.Max(lineA[i].X, lineB[i].X));
        minY = Math.Min(minY, Math.Min(lineA[i].Y, lineB[i].Y)); maxY = Math.Max(maxY, Math.Max(lineA[i].Y, lineB[i].Y));
    }

    var lines = new List<string>();
    lines.Add("linework on the layers named spans " + at(minX, minY) + " to " + at(maxX, maxY)
        + " (internal coordinates)");
    for (var r = 0; r < runX0.Count && r < 300; r++)
    {
        var l = Math.Sqrt((runX1[r] - runX0[r]) * (runX1[r] - runX0[r]) + (runY1[r] - runY0[r]) * (runY1[r] - runY0[r]));
        lines.Add("run " + (r + 1) + ": " + at(runX0[r], runY0[r]) + " to " + at(runX1[r], runY1[r]) + ", " + mm(l)
            + " mm, " + sizeText(runSize[r]) + " (" + runFrom[r] + ")" + (runDoubtful[r] ? " DOUBTFUL PAIR - check" : ""));
    }
    if (runX0.Count > 300) lines.Add("... and " + (runX0.Count - 300) + " more run(s)");
    runsFound = string.Join(" | ", lines.ToArray());

    findings.Add(runX0.Count + " run(s) found, with " + elbowsFound + " elbow(s), " + teesFound + " tee(s), "
        + crossesFound + " cross(es), " + tapsFound + " tap(s) and " + inLineFound + " in-line joint(s) to fit, and "
        + openEnds + " open end(s)" + (tapsWanted ? " - the type's routing preference is Taps, so a branch taps the "
        + "main and the main is not cut" : "")
        + (hintSize.Count > 0 ? "; " + hintUsed.Count(u => u) + " of " + hintSize.Count + " size hint(s) used" : ""));
    if (previewOnly)
        findings.Add("PREVIEW - NOTHING WAS DRAWN. runsFound lists each run and the size it would get; run again "
            + "without previewOnly to draw them");
}

// ---------------------------------------------------------------------------
// Draw - each run, then each joint's fitting: its room checked first, then
// placed in its own SubTransaction
// ---------------------------------------------------------------------------

if (refused.Length == 0 && !previewOnly)
{
    var z = level.ProjectElevation + middleElevationMm / mmPerFoot;
    var made = new MEPCurve[runX0.Count];
    var plan = new double[runX0.Count];      // each run's size seen in plan, feet - width or diameter
    var notMade = new List<string>();
    var snapped = new List<string>();

    Func<Element, BuiltInParameter, double, bool> setSize = (e, which, feet) =>
    {
        var p = e.get_Parameter(which);
        if (p == null || p.IsReadOnly) return false;
        p.Set(feet);
        return true;
    };

    for (var r = 0; r < runX0.Count; r++)
    {
        var p0 = new XYZ(runX0[r], runY0[r], z);
        var p1 = new XYZ(runX1[r], runY1[r], z);
        var where = "run " + (r + 1) + " " + at(runX0[r], runY0[r]) + " to " + at(runX1[r], runY1[r]);
        plan[r] = runSize[r][0] / mmPerFoot;
        try
        {
            made[r] = kind == "duct"
                ? (MEPCurve)Duct.Create(doc, systemType.Id, runType.Id, level.Id, p0, p1)
                : Pipe.Create(doc, systemType.Id, runType.Id, level.Id, p0, p1);
        }
        catch (Exception ex)
        {
            notMade.Add(where + ": " + ex.Message);
            continue;
        }
        if (made[r] == null) { notMade.Add(where + ": Revit returned nothing"); continue; }
        created.Add(made[r].Id);

        var want = runSize[r];
        try
        {
            bool set;
            if (kind == "pipe") set = setSize(made[r], BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, want[0] / mmPerFoot);
            else if (isRound) set = setSize(made[r], BuiltInParameter.RBS_CURVE_DIAMETER_PARAM, want[0] / mmPerFoot);
            else set = setSize(made[r], BuiltInParameter.RBS_CURVE_WIDTH_PARAM, want[0] / mmPerFoot)
                     & setSize(made[r], BuiltInParameter.RBS_CURVE_HEIGHT_PARAM, want[1] / mmPerFoot);
            if (!set) { snapped.Add(where + ": its size could not be set - Revit holds it read-only"); continue; }
        }
        catch (Exception ex)
        {
            snapped.Add(where + ": Revit refused " + sizeText(want) + " - " + ex.Message);
            continue;
        }

        // READ BACK - Revit snaps a pipe size to its segment's sizes and says nothing.
        Func<BuiltInParameter, double> readBack = which =>
        {
            var p = made[r].get_Parameter(which);
            return p == null ? double.NaN : Math.Round(p.AsDouble() * mmPerFoot, 1);
        };
        var got = kind == "pipe" ? new[] { readBack(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM) }
            : isRound ? new[] { readBack(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM) }
            : new[] { readBack(BuiltInParameter.RBS_CURVE_WIDTH_PARAM), readBack(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM) };
        var differs = false;
        for (var k = 0; k < want.Length; k++) if (double.IsNaN(got[k]) || Math.Abs(got[k] - want[k]) > 0.5) differs = true;
        if (differs) snapped.Add(where + ": asked " + sizeText(want) + ", Revit made it " + sizeText(got));
        if (!double.IsNaN(got[0])) plan[r] = Math.Max(plan[r], got[0] / mmPerFoot);
    }

    if (notMade.Count > 0)
    {
        skipped += notMade.Count;
        skippedWhy.Add(notMade.Count + " run(s) Revit would not create: " + string.Join(" | ", notMade.Take(20).ToArray()));
    }
    if (snapped.Count > 0)
        findings.Add(snapped.Count + " run(s) are not at the size asked - " + string.Join(" | ", snapped.Take(20).ToArray()));

    // ROOM, BEFORE REVIT IS ASKED. A fitting Revit cannot fit is POSTED as a
    // failure that rolls the whole call back at the commit - a SubTransaction
    // does not stop it. So each leg's length is held against a safe estimate of
    // what the fittings at BOTH its ends take from it, and a joint without room
    // is skipped and listed. The estimate: an elbow reaches 1.5 x the largest
    // size meeting there along each leg (duct-layout's measured 1.5 W radius
    // elbow, 450 mm on a 300 duct); a tee or cross 1 x the largest; a
    // transition 1 x the larger on the larger run (FIT_MEP_JOINTS' measured
    // 664 mm for 850 to 300 sits in it); a tap 1 x the branch on the branch;
    // and a margin beside each, so some straight run is always left.
    var margin = Math.Max(50.0, toleranceMm) / mmPerFoot;
    var taken = new double[runX0.Count * 2];
    Func<int, double> lengthOf = r => Math.Sqrt((runX1[r] - runX0[r]) * (runX1[r] - runX0[r]) + (runY1[r] - runY0[r]) * (runY1[r] - runY0[r]));
    // needs[k] for legs[k]: what this fitting takes from that leg. Returns "" or why not.
    Func<List<int>, List<double>, string> roomFor = (legs, needs) =>
    {
        for (var k = 0; k < legs.Count; k++)
        {
            var r = legs[k] / 2;
            var other = legs[k] % 2 == 0 ? r * 2 + 1 : r * 2;
            var want = taken[other] + needs[k] + margin;
            if (lengthOf(r) < want)
                return "no room - run " + (r + 1) + " is " + mm(lengthOf(r)) + " mm long and would need about " + mm(want)
                    + " mm for this fitting" + (taken[other] > 0 ? " and the one at its other end" : "")
                    + "; left unfitted so Revit has nothing to reverse";
        }
        for (var k = 0; k < legs.Count; k++) taken[legs[k]] = needs[k];
        return "";
    };

    // THE FITTINGS, one joint at a time, each in its own SubTransaction.
    int elbows = 0, tees = 0, crosses = 0, transitions = 0, unions = 0, taps = 0;
    foreach (var pair in runsAt.OrderBy(p => p.Key))
    {
        var node = pair.Key;
        var where = at(nodeX[node], nodeY[node]);
        var here = new XYZ(nodeX[node], nodeY[node], z);
        var isTap = nodeTapMain.ContainsKey(node);
        if (!isTap && pair.Value.Count < 2) continue;

        var connectors = new List<Connector>();
        var legs = new List<int>();
        var missing = 0;
        foreach (var end in pair.Value)
        {
            var curve = made[end / 2];
            if (curve == null) { missing++; continue; }
            Connector best = null;
            var bestD = double.MaxValue;
            foreach (Connector c in curve.ConnectorManager.Connectors)
            {
                if (c.ConnectorType != ConnectorType.End) continue;
                var d = c.Origin.DistanceTo(here);
                if (d < bestD) { bestD = d; best = c; }
            }
            if (best != null && bestD <= tol) { connectors.Add(best); legs.Add(end); } else missing++;
        }
        MEPCurve mainCurve = isTap ? made[nodeTapMain[node]] : null;

        // AN INCOMPLETE JOINT GETS NO FITTING: a tee with one run missing is not an elbow.
        if (missing > 0 || (isTap && mainCurve == null))
        {
            failedJoins.Add(where + ": " + (isTap ? "a tap - " : "") + (missing + (isTap && mainCurve == null ? 1 : 0))
                + " of the runs meeting here " + (missing == 1 ? "was" : "were") + " not drawn or could not be found at "
                + "the joint - no fitting placed, rather than a wrong one");
            continue;
        }
        if (!isTap && connectors.Count > 4)
        {
            failedJoins.Add(where + ": " + connectors.Count + " runs meet here, and no fitting joins more than four");
            continue;
        }

        var largest = legs.Max(e => plan[e / 2]);
        if (isTap)
        {
            // EACH BRANCH ITS OWN TAKEOFF ON THE UNCUT MAIN - FIT_MEP_JOINTS' route.
            for (var k = 0; k < connectors.Count; k++)
            {
                var room = roomFor(new List<int> { legs[k] }, new List<double> { plan[legs[k] / 2] });
                if (room.Length > 0) { failedJoins.Add(where + ": the tap - " + room); continue; }
                SubTransaction tapJoint = null;
                try
                {
                    tapJoint = new SubTransaction(doc);
                    tapJoint.Start();
                    var tap = doc.Create.NewTakeoffFitting(connectors[k], mainCurve);
                    if (tap == null || !connectors[k].IsConnected)
                    {
                        tapJoint.RollBack();
                        failedJoins.Add(where + ": the tap was " + (tap == null ? "not returned by Revit"
                            : "built but the branch end does not report being joined to it") + " - taken back, the branch left open");
                        continue;
                    }
                    if (tapJoint.Commit() != TransactionStatus.Committed)
                    {
                        failedJoins.Add(where + ": the tap was built and Revit would not keep it - the branch left open");
                        continue;
                    }
                    fittings.Add(tap.Id);
                    taps++;
                }
                catch (Exception ex)
                {
                    var undo = "";
                    try { if (tapJoint != null && tapJoint.HasStarted() && !tapJoint.HasEnded()) tapJoint.RollBack(); }
                    catch (Exception again) { undo = " Taking it back failed too (" + again.Message + ") - check this joint."; }
                    failedJoins.Add(where + ": Revit would not build the tap - " + ex.Message + undo);
                }
                finally
                {
                    if (tapJoint != null) tapJoint.Dispose();
                }
            }
            continue;
        }

        var what = connectors.Count == 2 ? (inLine(legs) ? "transition" : "elbow") : connectors.Count == 3 ? "tee" : "cross";
        var needs = new List<double>();
        foreach (var e in legs)
            needs.Add(what == "elbow" ? 1.5 * largest
                : what == "transition" ? (plan[e / 2] >= largest - 1e-9 ? largest : 0)
                : largest);
        var roomSaid = roomFor(legs, needs);
        if (roomSaid.Length > 0) { failedJoins.Add(where + ": the " + what + " - " + roomSaid); continue; }

        SubTransaction joint = null;
        try
        {
            joint = new SubTransaction(doc);
            joint.Start();
            FamilyInstance fitting = null;
            if (connectors.Count == 2)
            {
                var first = connectors[0]; var second = connectors[1];
                var facing = first.CoordinateSystem.BasisZ.DotProduct(second.CoordinateSystem.BasisZ);
                if (facing > -0.999) { what = "elbow"; fitting = doc.Create.NewElbowFitting(first, second); }
                else
                {
                    var sameSize = first.Shape == second.Shape && (first.Shape == ConnectorProfileType.Round
                        ? Math.Abs(first.Radius - second.Radius) < 1e-6
                        : Math.Abs(first.Width - second.Width) < 1e-6 && Math.Abs(first.Height - second.Height) < 1e-6);
                    // The larger end first, so the transition sits in the larger run (FIT_MEP_JOINTS' measured rule).
                    var firstArea = first.Shape == ConnectorProfileType.Round ? first.Radius * first.Radius * Math.PI : first.Width * first.Height;
                    var secondArea = second.Shape == ConnectorProfileType.Round ? second.Radius * second.Radius * Math.PI : second.Width * second.Height;
                    if (!sameSize && firstArea < secondArea) { var swap = first; first = second; second = swap; }
                    what = sameSize ? "union" : "transition";
                    fitting = sameSize ? doc.Create.NewUnionFitting(first, second) : doc.Create.NewTransitionFitting(first, second);
                }
            }
            else
            {
                // The MAIN is the pair most nearly head-on.
                int mainA = 0, mainB = 1;
                var most = double.MaxValue;
                for (var a = 0; a < connectors.Count; a++)
                    for (var b = a + 1; b < connectors.Count; b++)
                    {
                        var f = connectors[a].CoordinateSystem.BasisZ.DotProduct(connectors[b].CoordinateSystem.BasisZ);
                        if (f < most) { most = f; mainA = a; mainB = b; }
                    }
                var branches = Enumerable.Range(0, connectors.Count).Where(k => k != mainA && k != mainB).ToList();
                fitting = connectors.Count == 3
                    ? doc.Create.NewTeeFitting(connectors[mainA], connectors[mainB], connectors[branches[0]])
                    : doc.Create.NewCrossFitting(connectors[mainA], connectors[mainB], connectors[branches[0]], connectors[branches[1]]);
            }

            var joined = fitting != null && connectors.All(c => c.IsConnected);
            if (!joined)
            {
                joint.RollBack();
                failedJoins.Add(where + ": the " + what + " was " + (fitting == null ? "not returned by Revit"
                    : "built but not every run end reports being joined to it") + " - taken back, the runs left open here");
                continue;
            }
            if (joint.Commit() != TransactionStatus.Committed)
            {
                failedJoins.Add(where + ": the " + what + " was built and Revit would not keep it - the runs left open here");
                continue;
            }
            fittings.Add(fitting.Id);
            if (what == "elbow") elbows++;
            else if (what == "tee") tees++;
            else if (what == "cross") crosses++;
            else if (what == "transition") transitions++;
            else unions++;
        }
        catch (Exception ex)
        {
            var undo = "";
            try { if (joint != null && joint.HasStarted() && !joint.HasEnded()) joint.RollBack(); }
            catch (Exception again) { undo = " Taking it back failed too (" + again.Message + ") - check this joint."; }
            failedJoins.Add(where + ": Revit would not build the " + what + " - " + ex.Message + undo);
        }
        finally
        {
            if (joint != null) joint.Dispose();
        }
    }

    if (failedJoins.Count > 0 && allOrNothing)
        throw new InvalidOperationException(failedJoins.Count + " joint(s) could not be fitted and allOrNothing is "
            + "set, so NOTHING was kept: " + string.Join(" | ", failedJoins.Take(30).ToArray()));

    findings.Add(created.Count + " " + kind + "(s) drawn at " + num(middleElevationMm) + " mm above " + level.Name
        + " as " + runType.Name + " on " + systemType.Name + "; " + fittings.Count + " fitting(s) placed by the routing "
        + "preferences - " + elbows + " elbow(s), " + tees + " tee(s), " + crosses + " cross(es), " + taps + " tap(s), "
        + transitions + " transition(s), " + unions + " union(s)"
        + (failedJoins.Count > 0 ? "; " + failedJoins.Count + " joint(s) NOT fitted - the runs are NOT connected there, "
            + "listed in failedJoins" : "")
        + (runsWithoutSize.Count > 0 ? "; " + runsWithoutSize.Count + " run(s) took the default size, listed" : "")
        + ". The CAD file is untouched. A failure Revit raises only when the call is kept still rolls the WHOLE call "
        + "back, one undo, nothing kept");
}
