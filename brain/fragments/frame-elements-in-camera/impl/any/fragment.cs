// NOT STANDALONE. Assumes `doc`, `elements`, `viewName`, `direction`,
// `lookDownDeg`, `distanceMm`, `marginPercent`, `shot`, `side`,
// `eyeHeightMm`, `lensMm`, `composition`, `levelCamera` and `aspect` are in scope;
// leaves `camera`, `madeNew`, `framed`, `readBack`, `lens`, `noGeometry`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A PERSPECTIVE CAMERA THAT FRAMES THE ELEMENTS HANDED IN - "a close-up of this
// duct heater". The elements' box is taken; the eye stands off it in the
// compass direction asked (from project north, plan up), looking down at the
// angle asked, at the distance asked or two and a half times the box's
// half-diagonal; and the view's crop box is set to the elements' box seen from
// that eye, which Revit's API reference says moves "the crop region and far
// clip plane to the smallest values which will completely contain the 3D box".
//
// ONE JOB WHETHER THE VIEW EXISTS OR IS NEW: a perspective 3D view of that name
// is re-aimed; with none, one is made - what the ribbon's Camera tool makes.
// An isometric view of that name is refused, not turned: SET_VIEW_CAMERA turns
// a view perspective when that is what is wanted.
//
// THE CROP BOX IS GIVEN IN THE VIEW'S OWN FRAME - the transform Revit hands back
// with the crop box - so no assumption is made about where that frame sits.
// A PERSPECTIVE crop window is on Revit's near plane, millimetres in front of
// the eye, so the elements' corners are projected onto it (version 2, row
// 5b-1281 - version 1 set the box itself and drew the elements as a dot).
// `framed` counts the elements whose projected corners lie inside the window
// Revit reads back afterwards; the proof row still has the picture looked at.
//
// VERSION 3 - CINEMATIC SHOTS (row 5b-1287). Six optional inputs - a named
// `shot`, the `side` to stand on, `eyeHeightMm`, `lensMm`, `composition` and
// `levelCamera`. With all six left out the version 2 road below runs line for
// line, so a version 2 call frames exactly as before. With any of them:
//   - THE SIDE. front, back, left, right and the four corners are the
//     elements' own faces: their plan grid is the length-weighted direction of
//     their straight location lines (walls, ducts, beams), or project north
//     when they have none; the FRONT is the long face looking nearest project
//     south - Heron's starting rule, said in the answer, never known - and
//     left and right are as you face the front. A compass word or degrees is
//     where the eye stands, exactly. Version 2's `direction` still works, but
//     not beside `side`.
//   - THE SHOT. Each one is Heron's STARTING values (D-33), the table on the
//     card: how far the camera steps round off a face, the look angle, the eye
//     height, the lens, the composition, and whether the camera is level.
//     Any one of them given on its own wins over the shot's.
//   - THE LENS. Revit's API has no field of view (row 5b-1267), but the crop
//     window IS one: it sits on the near plane, so the angle across is
//     2 x atan(half-width / near). A 35 mm-film lens of f mm is a window of
//     half-width near x 18 / f and half-height near x 12 / f (a 36 x 24 frame).
//     The window is that size, and the eye is pulled back - the smallest
//     distance at which the elements fit with the margin - so the lens decides
//     how close the camera stands, as it does for a photographer.
//   - A LEVEL CAMERA looks dead level, so every vertical edge stays vertical:
//     two-point perspective from a corner, one-point square to a face. The
//     window is then SHIFTED up or down on the near plane to take in the
//     elements - what an architectural photographer's shift lens does.
//   - COMPOSITION moves the window, never the eye: centre, thirds (the
//     elements' middle on the left or right third line) or low-horizon (the
//     horizon on the bottom third line). One that cannot hold the elements
//     from that angle falls back to centre and says so.
//   - INTERIOR stands INSIDE the room - the room handed in, or the room at
//     the elements' middle - 600 mm in from the room's own boundary on the
//     side named (distanceMm when given), looking straight across: nothing is
//     pulled back, so the lens alone decides what is seen, and `framed` cannot
//     count a room the eye stands inside.
// The answer reads back the eye, the point aimed at, the eye height above the
// base level, the look angle, the lens and the angle across - from the crop
// window Revit holds afterwards - and `framed` as before.
//
// VERSION 4 - FOUR FAULTS FOUND MAKING REAL VIEWS ON REVIT 2020'S SAMPLE HOUSE.
//   - THE INTERIOR EYE (row 5b-1303) was 600 mm in from the room's BOUNDING
//     BOX, and stood inside a double door's frame where the box ran past the
//     wall: the Raytracer drew the picture black. The eye now comes in from
//     the box's face until it is inside the room (Room.IsPointInRoom,
//     Space.IsPointInSpace), stands the inset in from there, and steps further
//     in, 100 mm at a time, until nothing solid is within 150 mm of it - or
//     refuses, naming what is in the way.
//   - A WINDOW THAT CANNOT HOLD THE ELEMENTS (row 5b-1304) - distanceMm given,
//     or no distance found for the lens - was centred on the projected corners
//     of their box, and the corners nearest the eye project far off, so the
//     picture slid down to the floor. Such a window is now centred on the
//     elements' middle, and the answer says so.
//   - A NEW VIEW'S PHASE (row 5b-1307): Revit gives a new view the phase
//     filter 'Show All', which draws earlier-phase work grey. A view made here
//     takes the active view's phase (else the project's last) and the model's
//     own 'Show Complete' filter, and the answer names both.
//   - THE FAR CLIP AND THE WINDOW'S DISTANCE (row 5b-1302) say when they could
//     not do their job, and the crop box's Z bottom is again set past the
//     deepest corner, for a release that honours it.
//
// A VIEW A TEMPLATE CONTROLS is refused rather than half-changed.

const double MmPerFoot = 304.8;
var findings = new List<string>();
string refused = null;
View3D camera = null;
var madeNew = false;
var framed = 0;
var readBack = "";
var lens = "";
var noGeometry = new List<ElementId>();
var culture = System.Globalization.CultureInfo.InvariantCulture;

Func<string, string> squash = text =>
    new string((text ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
Func<string, double?> number = text =>
{
    double value;
    var t = (text ?? "").Trim().TrimEnd('°', '%').Trim();
    return double.TryParse(t, System.Globalization.NumberStyles.Float, culture, out value) ? (double?)value : null;
};
Func<double, string> mm = v => Math.Round(v * MmPerFoot).ToString(culture);
Func<double, string> deg = v => Math.Round(v, 1).ToString(culture);
// A version 3 input left out reads auto; so does an empty one or "none".
Func<string, bool> isAuto = text => { var said = squash(text); return said.Length == 0 || said == "auto" || said == "none"; };

// The compass, clockwise from project north (plan up).
var compass = new Dictionary<string, double>
{
    { "north", 0 }, { "n", 0 }, { "northeast", 45 }, { "ne", 45 }, { "east", 90 }, { "e", 90 },
    { "southeast", 135 }, { "se", 135 }, { "south", 180 }, { "s", 180 }, { "southwest", 225 }, { "sw", 225 },
    { "west", 270 }, { "w", 270 }, { "northwest", 315 }, { "nw", 315 },
};
var compassNames = new[] { "north", "north-north-east", "north-east", "east-north-east", "east", "east-south-east",
    "south-east", "south-south-east", "south", "south-south-west", "south-west", "west-south-west", "west",
    "west-north-west", "north-west", "north-north-west" };
Func<double, double> wrap = a => ((a % 360) + 360) % 360;
Func<double, string> compassOf = a => compassNames[(int)Math.Round(wrap(a) / 22.5) % 16];

// ---- version 3's inputs: read first, so `cinematic` decides the road ----
// THE SHOTS - Heron's STARTING values (D-33), the card's table: how far the
// camera steps round off a face (clockwise in plan), look-down degrees
// (negative looks up), eye height in mm above the base level (-1: on the line
// through the elements' centre at the look angle), lens mm, margin percent,
// level camera (1) or not (0).
var shots = new Dictionary<string, double[]>
{
    { "eyelevel",     new double[] { 25,   0, 1600, 35, 10, 1 } },
    { "lowangle",     new double[] { 30,  -6,  450, 24, 10, 0 } },
    { "highangle",    new double[] { 30,  25,   -1, 35, 10, 0 } },
    { "aerial",       new double[] { 45,  40,   -1, 35, 10, 0 } },
    { "wormseye",     new double[] { 20, -25,  150, 20, 10, 0 } },
    { "threequarter", new double[] { 45,   0, 1600, 35, 10, 1 } },
    { "front",        new double[] {  0,   0, 1600, 50, 10, 1 } },
    { "interior",     new double[] {  0,   0, 1400, 24, 10, 1 } },
    { "closeup",      new double[] { 30,  20,   -1, 85, 15, 0 } },
    { "cinematic",    new double[] { 30,   0, 1200, 28, 10, 1 } },
};
var shotComposition = new Dictionary<string, string>
{
    { "eyelevel", "centre" }, { "lowangle", "thirds-left" }, { "highangle", "centre" }, { "aerial", "centre" },
    { "wormseye", "centre" }, { "threequarter", "thirds-left" }, { "front", "centre" }, { "interior", "centre" },
    { "closeup", "centre" }, { "cinematic", "thirds-left" },
};
var shotTitle = new Dictionary<string, string>
{
    { "eyelevel", "Eye-Level" }, { "lowangle", "Low-Angle Hero" }, { "highangle", "High-Angle" },
    { "aerial", "Aerial" }, { "wormseye", "Worms-Eye" }, { "threequarter", "Three-Quarter" },
    { "front", "Front" }, { "interior", "Interior" }, { "closeup", "Close-Up" }, { "cinematic", "Cinematic" },
};
// THE PICTURE'S SHAPE, width over height: a 35 mm frame's 3:2 for every shot
// but the cinematic one, which is the cinema's 2.39:1. `aspect` changes it.
var shotAspect = new Dictionary<string, double> { { "cinematic", 2.39 } };
var shotAlias = new Dictionary<string, string>
{
    { "none", "none" },
    { "eyelevel", "eyelevel" }, { "twopoint", "eyelevel" }, { "twopointperspective", "eyelevel" },
    { "street", "eyelevel" }, { "streetlevel", "eyelevel" },
    { "lowangle", "lowangle" }, { "low", "lowangle" }, { "hero", "lowangle" }, { "lowanglehero", "lowangle" },
    { "heroshot", "lowangle" },
    { "highangle", "highangle" }, { "high", "highangle" },
    { "aerial", "aerial" }, { "birdseye", "aerial" }, { "birdseyeview", "aerial" }, { "aerialoblique", "aerial" },
    { "oblique", "aerial" }, { "drone", "aerial" },
    { "wormseye", "wormseye" }, { "worms", "wormseye" }, { "worm", "wormseye" }, { "wormseyeview", "wormseye" },
    { "threequarter", "threequarter" }, { "threequarters", "threequarter" }, { "34", "threequarter" },
    { "corner", "threequarter" },
    { "front", "front" }, { "frontelevation", "front" }, { "elevation", "front" }, { "onepoint", "front" },
    { "onepointperspective", "front" }, { "straighton", "front" },
    { "interior", "interior" }, { "interioronepoint", "interior" }, { "inside", "interior" },
    { "closeup", "closeup" }, { "detail", "closeup" }, { "closeupdetail", "closeup" }, { "product", "closeup" },
    { "cinematic", "cinematic" }, { "cinema", "cinematic" }, { "film", "cinematic" }, { "filmlook", "cinematic" },
    { "movie", "cinematic" }, { "cinematiclook", "cinematic" }, { "cinematicview", "cinematic" },
};
// The elements' own faces, as degrees round from the FRONT, clockwise in plan:
// standing outside the front and facing it, left is a quarter turn clockwise.
var faces = new Dictionary<string, double>
{
    { "front", 0 }, { "left", 90 }, { "back", 180 }, { "rear", 180 }, { "right", 270 },
    { "frontleft", 45 }, { "backleft", 135 }, { "rearleft", 135 }, { "backright", 225 }, { "rearright", 225 },
    { "frontright", 315 },
};

var shotKey = squash(shot);
if (shotKey.Length == 0) shotKey = "none";
var sideKey = squash(side);
if (sideKey.Length == 0) sideKey = "none";
if (!shotAlias.ContainsKey(shotKey))
    refused = "'" + shot + "' is not a shot - say eye-level, low-angle (hero), high-angle, aerial (bird's-eye), "
        + "worm's-eye, three-quarter, front (one-point), interior, close-up (detail), cinematic, or none. NOTHING WAS CHANGED.";
else shotKey = shotAlias[shotKey];
double? sideCompass = null;
if (refused == null && sideKey != "none" && !faces.ContainsKey(sideKey))
{
    if (compass.ContainsKey(sideKey)) sideCompass = compass[sideKey];
    else
    {
        var a = number(side);
        if (a == null || a < 0 || a > 360)
            refused = "'" + side + "' is not a side - say front, back, left, right, front-left, front-right, back-left, "
                + "back-right, a compass word such as south-west, degrees clockwise from project north, or none. "
                + "NOTHING WAS CHANGED.";
        else sideCompass = a.Value;
    }
}
double? eyeHeightAsked = null;
if (refused == null && !isAuto(eyeHeightMm))
{
    eyeHeightAsked = number((eyeHeightMm ?? "").Trim().ToLowerInvariant().Replace("mm", ""));
    if (eyeHeightAsked == null || eyeHeightAsked < -100000 || eyeHeightAsked > 2000000)
        refused = "'" + eyeHeightMm + "' is not an eye height - millimetres above the elements' base level, such as "
            + "1600, or auto. NOTHING WAS CHANGED.";
}
double? lensAsked = null;
if (refused == null && !isAuto(lensMm))
{
    var lensWords = new Dictionary<string, double>
    {
        { "ultrawide", 16 }, { "wide", 24 }, { "wideangle", 24 }, { "normal", 50 }, { "standard", 50 },
        { "tele", 85 }, { "telephoto", 85 }, { "portrait", 85 },
    };
    if (lensWords.ContainsKey(squash(lensMm))) lensAsked = lensWords[squash(lensMm)];
    else lensAsked = number((lensMm ?? "").Trim().ToLowerInvariant().Replace("mm", ""));
    if (lensAsked == null || lensAsked < 12 || lensAsked > 300)
        refused = "'" + lensMm + "' is not a lens - a 35 mm-film focal length from 12 to 300 (24 wide, 35, 50 "
            + "normal, 85 tele), the words ultra-wide, wide, normal or tele, or auto. NOTHING WAS CHANGED.";
}
string compositionAsked = null;
if (refused == null && !isAuto(composition))
{
    var c = squash(composition);
    if (c == "centre" || c == "center" || c == "middle" || c == "centred" || c == "centered") compositionAsked = "centre";
    else if (c == "thirds" || c == "ruleofthirds" || c == "thirdsleft" || c == "leftthird") compositionAsked = "thirds-left";
    else if (c == "thirdsright" || c == "rightthird") compositionAsked = "thirds-right";
    else if (c == "lowhorizon" || c == "horizonlow") compositionAsked = "low-horizon";
    else
        refused = "'" + composition + "' is not a composition - say centre, thirds (thirds-left or thirds-right), "
            + "low-horizon, or auto. NOTHING WAS CHANGED.";
}
bool? levelAsked = null;
if (refused == null && !isAuto(levelCamera))
{
    var l = squash(levelCamera);
    if (l == "true" || l == "yes" || l == "on" || l == "level" || l == "1") levelAsked = true;
    else if (l == "false" || l == "no" || l == "off" || l == "0" || l == "tilted" || l == "aim") levelAsked = false;
    else
        refused = "'" + levelCamera + "' is not a level-camera answer - say true (look dead level, verticals "
            + "vertical), false (aim at the elements' middle), or auto. NOTHING WAS CHANGED.";
}
// THE PICTURE'S SHAPE (row 5b-1287): width over height - 16:9, 3:2, 4:3,
// 2.39:1 (cinema), 1:1, 9:16 - or the words cinema, widescreen, square,
// portrait; auto is the shot's own (3:2, cinematic 2.39:1).
double? aspectAsked = null;
if (refused == null && !isAuto(aspect))
{
    var aspectText = (aspect ?? "").Trim().ToLowerInvariant().Replace(" ", "");
    var aspectWords = new Dictionary<string, double>
    {
        { "cinema", 2.39 }, { "cinemascope", 2.39 }, { "anamorphic", 2.39 }, { "widescreen", 16.0 / 9 },
        { "square", 1 }, { "portrait", 2.0 / 3 }, { "landscape", 1.5 },
    };
    if (aspectWords.ContainsKey(aspectText)) aspectAsked = aspectWords[aspectText];
    else
    {
        var parts = aspectText.Split(':', 'x', '/');
        double aw, ah;
        if (parts.Length == 2 && double.TryParse(parts[0], System.Globalization.NumberStyles.Float, culture, out aw)
            && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, culture, out ah) && aw > 0 && ah > 0)
            aspectAsked = aw / ah;
        else if (parts.Length == 1 && double.TryParse(parts[0], System.Globalization.NumberStyles.Float, culture, out aw) && aw > 0)
            aspectAsked = aw;
    }
    if (aspectAsked == null || aspectAsked < 0.2 || aspectAsked > 5)
        refused = "'" + aspect + "' is not a picture shape - say 16:9, 3:2, 4:3, 2.39:1 (cinema), 1:1, 9:16, the words "
            + "cinema, widescreen, square or portrait, or auto. NOTHING WAS CHANGED.";
}
// Any of the seven turns version 3's road on; none of them, and version 2's runs.
var cinematic = refused == null && (shotKey != "none" || sideKey != "none" || eyeHeightAsked != null
    || lensAsked != null || compositionAsked != null || levelAsked != null || aspectAsked != null);

// ---- version 2's inputs - `auto` is version 2's own default ----
var directionGiven = squash(direction) != "auto";
var directionAsked = directionGiven ? direction : "south-east";
double azimuth = 0;
var d = squash(directionAsked);
if (refused == null)
{
    if (compass.ContainsKey(d)) azimuth = compass[d];
    else
    {
        var a = number(directionAsked);
        if (a == null || a < 0 || a > 360)
            refused = "'" + direction + "' is not a direction - say north, south-east and so on, or degrees clockwise from "
                + "project north. NOTHING WAS CHANGED.";
        else azimuth = a.Value;
    }
}
if (refused == null && directionGiven && sideKey != "none")
    refused = "direction '" + direction + "' and side '" + side + "' both say where the camera stands - give one. "
        + "NOTHING WAS CHANGED.";
var lookDownGiven = squash(lookDownDeg) != "auto";
var lookDown = lookDownGiven ? number(lookDownDeg) : 30;
if (refused == null && (lookDown == null || lookDown < -60 || lookDown > 89))
    refused = "'" + lookDownDeg + "' is not a look-down angle - degrees below level, -60 to 89. NOTHING WAS CHANGED.";
if (refused == null && lookDownGiven && levelAsked == true && Math.Abs(lookDown.Value) > 1e-9)
    refused = "a level camera looks dead level, so lookDownDeg " + lookDownDeg + " cannot be kept with it - leave "
        + "one out. NOTHING WAS CHANGED.";
double? distance = null;
if (refused == null && squash(distanceMm) != "auto")
{
    distance = number(distanceMm);
    if (distance == null || distance <= 0)
        refused = "'" + distanceMm + "' is not a distance - millimetres from the elements' centre, or auto. "
            + "NOTHING WAS CHANGED.";
    else distance = distance / MmPerFoot;
}
var marginGiven = squash(marginPercent) != "auto";
var margin = marginGiven ? number(marginPercent) : (cinematic && shotKey != "none" ? shots[shotKey][4] : 10);
if (refused == null && (margin == null || margin < 0 || margin > 200))
    refused = "'" + marginPercent + "' is not a margin - a percentage, 0 to 200. NOTHING WAS CHANGED.";

// The elements' box, in model coordinates.
XYZ low = null, high = null;
var boxed = new List<Element>();
if (refused == null)
{
    if (elements == null || elements.Count == 0)
        refused = "No elements were handed in - select what the camera should frame. NOTHING WAS CHANGED.";
    else
    {
        foreach (var element in elements)
        {
            if (element == null) continue;
            var box = element.get_BoundingBox(null);
            if (box == null) { noGeometry.Add(element.Id); continue; }
            var corners = new[] { box.Transform.OfPoint(box.Min), box.Transform.OfPoint(box.Max) };
            foreach (var p in corners)
            {
                low = low == null ? p : new XYZ(Math.Min(low.X, p.X), Math.Min(low.Y, p.Y), Math.Min(low.Z, p.Z));
                high = high == null ? p : new XYZ(Math.Max(high.X, p.X), Math.Max(high.Y, p.Y), Math.Max(high.Z, p.Z));
            }
            boxed.Add(element);
        }
        if (boxed.Count == 0)
            refused = "None of the " + elements.Count + " element(s) has geometry to frame. NOTHING WAS CHANGED.";
    }
}

var name = (viewName ?? "").Trim();
if (refused == null && (name.Length == 0 || squash(name) == "auto"))
{
    var first = boxed[0];
    name = (cinematic && shotKey != "none" ? "Heron " + shotTitle[shotKey] + " - " : "Heron Frame - ")
        + (first.Category != null ? first.Category.Name + " " : "") + first.Name
        + (boxed.Count > 1 ? " and " + (boxed.Count - 1) + " more" : "");
}

if (refused == null)
{
    foreach (var candidate in new FilteredElementCollector(doc).OfClass(typeof(View3D)))
    {
        var v = candidate as View3D;
        if (v != null && !v.IsTemplate && string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase)) { camera = v; break; }
    }
    if (camera != null && !camera.IsPerspective)
    {
        refused = "'" + camera.Name + "' is an isometric 3D view. This frames elements in a perspective view - name a new "
            + "one, or turn this one perspective with SET_VIEW_CAMERA first. NOTHING WAS CHANGED.";
        camera = null;
    }
    else if (camera != null && camera.ViewTemplateId != ElementId.InvalidElementId)
    {
        refused = "'" + camera.Name + "' is controlled by a view template, which owns its orientation. NOTHING WAS CHANGED.";
        camera = null;
    }
    else if (camera == null)
    {
        var threeD = new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>()
            .FirstOrDefault(t => t.ViewFamily == ViewFamily.ThreeDimensional);
        if (threeD == null) refused = "The project has no 3D view type, so NOTHING WAS MADE.";
        else
        {
            try
            {
                camera = View3D.CreatePerspective(doc, threeD.Id);
                camera.Name = name;
                madeNew = true;
            }
            catch (Exception ex)
            {
                refused = "Revit refused the new perspective view: " + ex.Message + ". NOTHING WAS MADE.";
                camera = null;
            }
        }
    }
}

// A NEW VIEW'S PHASE (version 4, row 5b-1307). Revit gives a new view the
// phase filter 'Show All', and on the sample house that drew the existing
// building in its grey override - the owner's interior renders came out grey
// until he changed the Phase Filter by hand. A view made here takes the active
// view's phase, or the project's last when the active view has none, and the
// model's own filter named 'Show Complete' - looked up, never made. A view
// that already existed keeps the phase its modeller gave it.
Func<View, string> phaseOf = v =>
{
    var ph = v.get_Parameter(BuiltInParameter.VIEW_PHASE);
    var pf = v.get_Parameter(BuiltInParameter.VIEW_PHASE_FILTER);
    var phaseElement = ph != null ? doc.GetElement(ph.AsElementId()) : null;
    var filterElement = pf != null ? doc.GetElement(pf.AsElementId()) : null;
    return "phase " + (phaseElement != null ? "'" + phaseElement.Name + "'" : "none") + ", phase filter "
        + (filterElement != null ? "'" + filterElement.Name + "'" : "none");
};
if (refused == null && madeNew && camera != null)
{
    try
    {
        Phase phaseWanted = null;
        var phaseFrom = "";
        var active = doc.ActiveView;
        var activePhase = active != null ? active.get_Parameter(BuiltInParameter.VIEW_PHASE) : null;
        if (activePhase != null) phaseWanted = doc.GetElement(activePhase.AsElementId()) as Phase;
        if (phaseWanted != null) phaseFrom = "the active view's, '" + active.Name + "'";
        else
        {
            foreach (Phase candidate in doc.Phases) phaseWanted = candidate;
            if (phaseWanted != null) phaseFrom = "the project's last";
        }
        var complete = new FilteredElementCollector(doc).OfClass(typeof(PhaseFilter)).Cast<PhaseFilter>()
            .FirstOrDefault(f => string.Equals(f.Name, "Show Complete", StringComparison.OrdinalIgnoreCase));
        var phaseParameter = camera.get_Parameter(BuiltInParameter.VIEW_PHASE);
        var filterParameter = camera.get_Parameter(BuiltInParameter.VIEW_PHASE_FILTER);
        var said = new List<string>();
        if (phaseWanted != null && phaseParameter != null && !phaseParameter.IsReadOnly)
        {
            if (phaseParameter.AsElementId() != phaseWanted.Id) phaseParameter.Set(phaseWanted.Id);
            said.Add(phaseParameter.AsElementId() == phaseWanted.Id ? "phase '" + phaseWanted.Name + "' (" + phaseFrom + ")"
                : "phase '" + phaseWanted.Name + "' NOT taken - Revit kept its own");
        }
        else said.Add("its phase left as Revit made it - " + (phaseWanted == null ? "the project has no phase to give it"
            : "the view's phase cannot be set"));
        if (complete != null && filterParameter != null && !filterParameter.IsReadOnly)
        {
            if (filterParameter.AsElementId() != complete.Id) filterParameter.Set(complete.Id);
            said.Add(filterParameter.AsElementId() == complete.Id ? "phase filter 'Show Complete', so earlier phases are not drawn grey"
                : "phase filter 'Show Complete' NOT taken - Revit kept its own");
        }
        else said.Add(complete == null ? "no phase filter named 'Show Complete' is in this model, so the view keeps Revit's "
            + "default - choose one in its Properties if earlier phases draw grey" : "the view's phase filter cannot be set");
        findings.Add("The new view: " + string.Join("; ", said) + ".");
    }
    catch (Exception ex)
    {
        findings.Add("The new view's phase could not be set (" + ex.Message + ") - it keeps Revit's default, which may draw "
            + "earlier phases grey.");
    }
}

Func<XYZ, XYZ> upFor = look =>
    new XYZ(look.X, look.Y, 0).GetLength() < 1e-9 ? XYZ.BasisY
        : look.CrossProduct(XYZ.BasisZ).Normalize().CrossProduct(look).Normalize();
var cinematicText = "";
var reachedCamera = refused == null && camera != null;

// WHERE THE CROP WINDOW REALLY IS (row 5b-1301). Measured 2026-10-21 on Revit
// 2020: a perspective view's crop box reads its Z top as the far clip over a
// thousand - 40.2 mm at Revit's 40232 mm far clip, 92.9 mm at 92888 mm - but
// its X and Y are drawn on a plane 0.1 ft (30.48 mm) in front of the eye,
// whatever the far clip. The crop SHAPE lies there (GetCropShape), it did not
// move with the far clip at 40232, 92888 or 200000 mm, and the picture agreed:
// a window worked out on the 92.9 mm reading drew the building three times too
// small. So the window's distance is read from the crop shape, 0.1 ft when the
// shape cannot be read - and since version 4 (row 5b-1302) that is SAID: the
// same number places the window and checks it, so nothing else would show it.
var windowAssumed = false;
Func<View3D, double> windowDistance = view =>
{
    try
    {
        var o3 = view.GetOrientation();
        foreach (var loop in view.GetCropRegionShapeManager().GetCropShape())
            foreach (Curve c in loop)
            {
                var along = (c.GetEndPoint(0) - o3.EyePosition).DotProduct(o3.ForwardDirection);
                if (along > 1e-6 && along < 1.0) return along;
            }
    }
    catch (Exception) { }
    windowAssumed = true;
    return 0.1;
};
// The far clip is a parameter: Revit ignores a crop box's Z bottom on a
// perspective view (row 5b-1301) - a camera 50 m away kept its 40232 mm far
// clip and cut both columns out of the picture. Pushed out, never pulled in;
// since version 4 (row 5b-1302) a far clip that cannot be pushed out is said.
string farClipProblem = null;
Action<View3D, double> reachFar = (view, deepest) =>
{
    var farClip = view.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR);
    if (farClip != null && farClip.AsDouble() >= deepest * 1.1) return;
    string why = farClip == null ? "the view has no far clip offset" : farClip.IsReadOnly ? "its far clip offset is read-only" : null;
    if (why == null)
    {
        try { if (!farClip.Set(deepest * 1.1)) why = "Revit did not take the new offset"; }
        catch (Exception ex) { why = ex.Message; }
    }
    if (why != null)
        farClipProblem = "The far clip could not be pushed out to " + mm(deepest * 1.1) + " mm (" + why + "), so elements "
            + "further from the eye than " + (farClip != null ? mm(farClip.AsDouble()) + " mm" : "its far clip")
            + " may be cut out of the picture - look at the view.";
};

if (reachedCamera && !cinematic)
{
    // ---- VERSION 2'S ROAD, line for line ----
    var centre = (low + high) * 0.5;
    var halfDiagonal = Math.Max((high - low).GetLength() * 0.5, 100 / MmPerFoot);
    var standOff = distance ?? halfDiagonal * 2.5;
    var az = azimuth * Math.PI / 180.0;
    var el = lookDown.Value * Math.PI / 180.0;
    var toEye = new XYZ(Math.Sin(az) * Math.Cos(el), Math.Cos(az) * Math.Cos(el), Math.Sin(el));
    var eye = centre + toEye * standOff;
    var forward = toEye.Negate();
    XYZ up;
    if (new XYZ(forward.X, forward.Y, 0).GetLength() < 1e-9) up = XYZ.BasisY;
    else up = forward.CrossProduct(XYZ.BasisZ).Normalize().CrossProduct(forward).Normalize();

    try
    {
        if (camera.IsLocked) camera.Unlock();
        camera.SetOrientation(new ViewOrientation3D(eye, up, forward));
        doc.Regenerate();

        // THE CROP WINDOW OF A PERSPECTIVE VIEW SITS ON REVIT'S OWN NEAR PLANE
        // (version 2, row 5b-1281). Measured 2026-10-20 on Revit 2020: a new
        // perspective view's crop box reads X -14.3 to 14.3, Y -10.7 to 10.7
        // mm at Z -40.2 mm - a window 40 mm in front of the eye. Version 1 set
        // the elements' box itself, metres wide and metres away; the view then
        // drew the elements as a dot in the middle of the picture
        // (EXPORT_VIEW_IMAGE, three views). So each corner is now projected
        // onto that near plane - x times near over its depth - and the window
        // is the projected corners plus the margin; the near plane is left
        // where Revit has it. A near plane a version-1 run moved metres away is
        // put back at 40.2 mm, a new view's.
        // ROW 5b-1301 CORRECTED THE DISTANCE: the window is drawn 0.1 ft in
        // front of the eye, and the 40.2 mm was the far clip over a thousand -
        // windowDistance() above.
        var frame = camera.CropBox.Transform;
        var toView = frame.Inverse;
        var nearPlane = windowDistance(camera);
        double? pxLo = null, pxHi = null, pyLo = null, pyHi = null, deepest = null;
        foreach (var x in new[] { low.X, high.X })
            foreach (var y in new[] { low.Y, high.Y })
                foreach (var z in new[] { low.Z, high.Z })
                {
                    var p = toView.OfPoint(new XYZ(x, y, z));
                    var depth = Math.Max(-p.Z, nearPlane);
                    var px = p.X * nearPlane / depth;
                    var py = p.Y * nearPlane / depth;
                    pxLo = pxLo == null ? px : Math.Min(pxLo.Value, px);
                    pxHi = pxHi == null ? px : Math.Max(pxHi.Value, px);
                    pyLo = pyLo == null ? py : Math.Min(pyLo.Value, py);
                    pyHi = pyHi == null ? py : Math.Max(pyHi.Value, py);
                    deepest = deepest == null ? depth : Math.Max(deepest.Value, depth);
                }
        var growX = (pxHi.Value - pxLo.Value) * (margin.Value / 200.0);
        var growY = (pyHi.Value - pyLo.Value) * (margin.Value / 200.0);
        reachFar(camera, deepest.Value);
        doc.Regenerate();
        var wanted = new BoundingBoxXYZ();
        wanted.Transform = frame;
        // The Z bottom past the deepest corner, for a release that honours it -
        // Revit 2020 ignores it on a perspective view (rows 5b-1301, 5b-1302).
        wanted.Min = new XYZ(pxLo.Value - growX, pyLo.Value - growY, Math.Min(camera.CropBox.Min.Z, -deepest.Value * 1.1));
        wanted.Max = new XYZ(pxHi.Value + growX, pyHi.Value + growY, camera.CropBox.Max.Z);
        camera.CropBoxActive = true;
        camera.CropBox = wanted;
        doc.Regenerate();
    }
    catch (Exception ex)
    {
        refused = "Revit refused the camera: " + ex.Message + ". " + (madeNew ? "The new view was made; it is not framed."
            : "NOTHING WAS CHANGED.");
    }
}

if (reachedCamera && cinematic)
{
    // ---- VERSION 3'S ROAD - a named shot, a side, a height, a lens ----
    var hasShot = shotKey != "none";
    var shotValues = hasShot ? shots[shotKey] : null;
    var interior = shotKey == "interior";

    // THE PLAN GRID: the length-weighted direction of the elements' straight
    // location lines, folded into a quarter turn (a wall and the wall square
    // to it agree); project north when there are none.
    var lines = new List<double[]>();
    foreach (var element in boxed)
    {
        var lc = element.Location as LocationCurve;
        var line = lc != null ? lc.Curve as Line : null;
        if (line == null) continue;
        var flat = new XYZ(line.Direction.X, line.Direction.Y, 0);
        if (flat.GetLength() < 1e-6) continue;
        var a = wrap(Math.Atan2(flat.X, flat.Y) * 180.0 / Math.PI) % 90;
        lines.Add(new[] { a, line.Length * flat.GetLength() });
    }
    double grid = 0;
    if (lines.Count > 0)
    {
        var bins = new double[180];
        foreach (var l in lines) bins[(int)(l[0] * 2) % 180] += l[1];
        // A bin and its two neighbours, round the quarter turn, so a grid on
        // a bin's edge is not split in two.
        Func<int, double> around = b => bins[(b + 179) % 180] + bins[b] + bins[(b + 1) % 180];
        var best = 0;
        for (var b = 1; b < 180; b++) if (around(b) > around(best)) best = b;
        var middle = best / 2.0 + 0.25;
        double sum = 0, weight = 0;
        foreach (var l in lines)
        {
            var off = l[0] - middle;
            if (off > 45) off -= 90;
            if (off < -45) off += 90;
            if (Math.Abs(off) <= 1.0) { sum += off * l[1]; weight += l[1]; }
        }
        grid = weight > 0 ? wrap(middle + sum / weight) % 90 : middle;
    }
    var gr = grid * Math.PI / 180.0;
    var axis1 = new XYZ(Math.Sin(gr), Math.Cos(gr), 0);
    var axis2 = new XYZ(Math.Cos(gr), -Math.Sin(gr), 0);
    // THE ELEMENTS' BOX ON THAT GRID. An element on a straight location line
    // gives its own plan rectangle - the line, widened by the width its box
    // gives back once the line's slant is taken out (a box of a wall turned
    // 20 degrees is metres wider than the wall, and the camera would stand
    // back for nothing); any other element gives its box's corners.
    double u0 = double.MaxValue, u1 = double.MinValue, v0 = double.MaxValue, v1 = double.MinValue;
    foreach (var element in boxed)
    {
        var box = element.get_BoundingBox(null);
        double bxLo = double.MaxValue, bxHi = double.MinValue, byLo = double.MaxValue, byHi = double.MinValue;
        foreach (var x in new[] { box.Min.X, box.Max.X })
            foreach (var y in new[] { box.Min.Y, box.Max.Y })
                foreach (var z in new[] { box.Min.Z, box.Max.Z })
                {
                    var p = box.Transform.OfPoint(new XYZ(x, y, z));
                    bxLo = Math.Min(bxLo, p.X); bxHi = Math.Max(bxHi, p.X);
                    byLo = Math.Min(byLo, p.Y); byHi = Math.Max(byHi, p.Y);
                }
        var plan = new List<XYZ>
        {
            new XYZ(bxLo, byLo, 0), new XYZ(bxHi, byLo, 0), new XYZ(bxLo, byHi, 0), new XYZ(bxHi, byHi, 0),
        };
        var lc = element.Location as LocationCurve;
        var line = lc != null ? lc.Curve as Line : null;
        if (line != null)
        {
            var a = line.GetEndPoint(0);
            var b = line.GetEndPoint(1);
            var flat = new XYZ(b.X - a.X, b.Y - a.Y, 0);
            var run = flat.GetLength();
            if (run > 1e-6)
            {
                var along = flat / run;
                var across = new XYZ(-along.Y, along.X, 0);
                var c = Math.Abs(along.X);
                var sn = Math.Abs(along.Y);
                // A rectangle run long and t wide, slanted, has a box
                // run*c + t*sn across X and run*sn + t*c across Y.
                var thick = c >= sn ? (byHi - byLo - run * sn) / c : (bxHi - bxLo - run * c) / sn;
                thick = Math.Max(0, thick) / 2;
                plan = new List<XYZ>
                {
                    new XYZ(a.X, a.Y, 0) + across * thick, new XYZ(a.X, a.Y, 0) - across * thick,
                    new XYZ(b.X, b.Y, 0) + across * thick, new XYZ(b.X, b.Y, 0) - across * thick,
                };
            }
        }
        foreach (var p in plan)
        {
            var u = p.X * axis1.X + p.Y * axis1.Y;
            var v = p.X * axis2.X + p.Y * axis2.Y;
            u0 = Math.Min(u0, u); u1 = Math.Max(u1, u); v0 = Math.Min(v0, v); v1 = Math.Max(v1, v);
        }
    }
    var corners8 = new List<XYZ>();
    foreach (var u in new[] { u0, u1 })
        foreach (var v in new[] { v0, v1 })
            foreach (var z in new[] { low.Z, high.Z })
                corners8.Add(axis1 * u + axis2 * v + new XYZ(0, 0, z));
    var centre = axis1 * ((u0 + u1) / 2) + axis2 * ((v0 + v1) / 2) + new XYZ(0, 0, (low.Z + high.Z) / 2);
    var halfDiagonal = Math.Max(Math.Sqrt((u1 - u0) * (u1 - u0) + (v1 - v0) * (v1 - v0)
        + (high.Z - low.Z) * (high.Z - low.Z)) * 0.5, 100 / MmPerFoot);

    // THE FRONT: the long face looking nearest project south (Heron's
    // starting rule - the modeller names a compass side when it is another).
    var longU = u1 - u0;
    var longV = v1 - v0;
    var candidates = new List<double>();
    if (longV >= longU * 0.95) { candidates.Add(wrap(grid)); candidates.Add(wrap(grid + 180)); }
    if (longU >= longV * 0.95) { candidates.Add(wrap(grid + 90)); candidates.Add(wrap(grid + 270)); }
    var frontAz = candidates.OrderBy(a => Math.Abs(wrap(a) - 180)).ThenBy(a => a).First();

    // WHERE THE CAMERA STANDS - an azimuth from the elements, clockwise from
    // project north; a face takes the shot's step round, a corner or a
    // compass word is exact.
    // A shot with no side stands on the front - an interior at the LEFT end,
    // so it looks down the room's length.
    var sideUsed = sideKey;
    if (sideKey == "none" && !directionGiven && hasShot) sideUsed = interior ? "left" : "front";
    double standAz;
    string standText;
    if (sideUsed != "none" && faces.ContainsKey(sideUsed))
    {
        var faceAz = wrap(frontAz + faces[sideUsed]);
        var isFace = sideUsed == "front" || sideUsed == "back" || sideUsed == "rear" || sideUsed == "left"
            || sideUsed == "right";
        // The step round: a side face steps toward the front; the front and
        // the back step toward the left side.
        var toward = sideUsed == "front" ? "the left side" : sideUsed == "right" ? "the front" : sideUsed == "left"
            ? "the front" : "the left side";
        var sign = sideUsed == "front" || sideUsed == "right" ? 1 : -1;
        var swing = isFace && hasShot ? shotValues[0] * sign : 0;
        standAz = wrap(faceAz + swing);
        standText = "the " + (sideKey == "none" ? sideUsed : side.Trim()) + " side (its face looks "
            + compassOf(faceAz) + ")" + (swing != 0 ? ", stepped " + deg(Math.Abs(swing)) + "° round toward "
                + toward : "")
            + "; the front was taken as the " + (Math.Abs(longU - longV) <= Math.Max(longU, longV) * 0.05
                ? "face" : "long face") + " looking " + compassOf(frontAz) + " (" + deg(frontAz) + "°)";
    }
    else if (sideCompass != null) { standAz = sideCompass.Value; standText = "the " + side.Trim() + " side"; }
    else { standAz = azimuth; standText = "the " + directionAsked.Trim() + " of the elements"; }

    // THE EYE HEIGHT - above the level MOST of the elements stand on (the
    // lowest of those that tie), or the bottom of their box when none has a
    // level. On the owner's word, 2026-10-21 (row 5b-1301): it was the LOWEST
    // level any of them stood on, and on Revit 2020's sample house one
    // partition on 'Foundation', 800 mm down, put the eye-level camera 800 mm
    // above the ground floor and, on the hill side, inside the ground.
    double baseZ = low.Z;
    string baseName = "the bottom of the elements' box";
    Level baseLevel = null;
    var standingOn = new Dictionary<ElementId, int>();
    var levelsSeen = new Dictionary<ElementId, Level>();
    foreach (var element in boxed)
    {
        var lv = element.LevelId != ElementId.InvalidElementId ? doc.GetElement(element.LevelId) as Level : null;
        if (lv == null) continue;
        var key = lv.Id;
        standingOn[key] = standingOn.ContainsKey(key) ? standingOn[key] + 1 : 1;
        levelsSeen[key] = lv;
    }
    foreach (var key in standingOn.Keys)
    {
        var lv = levelsSeen[key];
        if (baseLevel == null || standingOn[key] > standingOn[baseLevel.Id]
            || (standingOn[key] == standingOn[baseLevel.Id] && lv.ProjectElevation < baseLevel.ProjectElevation))
            baseLevel = lv;
    }
    if (baseLevel != null) { baseZ = baseLevel.ProjectElevation; baseName = "'" + baseLevel.Name + "'"; }

    // THE LOOK. lookDownDeg wins; then levelCamera; then the shot's own.
    var level = levelAsked ?? (hasShot ? shotValues[5] > 0.5 : false);
    var aimAtCentre = levelAsked == false && hasShot && shotValues[5] > 0.5 && !lookDownGiven;
    double pitch = lookDownGiven ? lookDown.Value : level ? 0 : hasShot ? shotValues[1] : lookDown.Value;
    if (lookDownGiven) level = Math.Abs(pitch) < 1e-9;
    double? eyeH = eyeHeightAsked != null ? eyeHeightAsked / MmPerFoot
        : hasShot && shotValues[2] >= 0 ? shotValues[2] / MmPerFoot
        : level ? 1600 / MmPerFoot : (double?)null;
    var lensF = lensAsked ?? (hasShot ? shotValues[3] : (compositionAsked != null && compositionAsked != "centre" ? 35 : (double?)null));
    var comp = compositionAsked ?? (hasShot ? shotComposition[shotKey] : "centre");
    var shape = aspectAsked ?? (hasShot && shotAspect.ContainsKey(shotKey) ? shotAspect[shotKey] : 1.5);
    if (lensF == null && aspectAsked != null) lensF = 35;

    var sa = Math.Sin(standAz * Math.PI / 180.0);
    var ca = Math.Cos(standAz * Math.PI / 180.0);
    var pr = pitch * Math.PI / 180.0;
    var fixedLook = new XYZ(-sa * Math.Cos(pr), -ca * Math.Cos(pr), -Math.Sin(pr));
    Func<double, XYZ> eyeFor = dist => eyeH != null
        ? new XYZ(centre.X + sa * dist, centre.Y + ca * dist, baseZ + eyeH.Value)
        : centre + fixedLook.Negate() * dist;
    Func<XYZ, XYZ> lookFor = eyeAt => aimAtCentre ? (centre - eyeAt).Normalize() : fixedLook;

    // The elements seen from an eye, as tangents on the picture (x across,
    // y up) - the near plane's own size does not matter until the window is
    // made: [x low, x high, y low, y high, nearest depth].
    Func<XYZ, XYZ, double[]> seen = (eyeAt, look) =>
    {
        var upAt = upFor(look);
        var rightAt = upAt.CrossProduct(look.Negate());
        double xl = double.MaxValue, xh = double.MinValue, yl = double.MaxValue, yh = double.MinValue;
        var nearest = double.MaxValue;
        foreach (var c in corners8)
        {
            var q = c - eyeAt;
            var depth = q.DotProduct(look);
            nearest = Math.Min(nearest, depth);
            if (depth <= 1e-6) continue;
            var tx = q.DotProduct(rightAt) / depth;
            var ty = q.DotProduct(upAt) / depth;
            xl = Math.Min(xl, tx); xh = Math.Max(xh, tx); yl = Math.Min(yl, ty); yh = Math.Max(yh, ty);
        }
        return new[] { xl, xh, yl, yh, nearest };
    };
    // Where the window goes, in the same tangents, and whether the elements
    // fit it with the margin: [x middle, y middle, fits].
    var m = margin.Value / 100.0;
    Func<double[], double, double, double, string, double[]> place = (b, tw, th, horizon, how) =>
    {
        var cx = (b[0] + b[1]) / 2;
        var cy = (b[2] + b[3]) / 2;
        var sw = (b[1] - b[0]) / 2 * (1 + m);
        var sh = (b[3] - b[2]) / 2 * (1 + m);
        double ox = cx, oy = cy;
        var fits = sw <= tw;
        if (how == "thirds-left") { ox = cx + tw / 3; fits = sw <= tw * 2 / 3; }
        if (how == "thirds-right") { ox = cx - tw / 3; fits = sw <= tw * 2 / 3; }
        if (how == "low-horizon") { oy = horizon + th / 3; fits = fits && cy + sh <= oy + th && cy - sh >= oy - th; }
        else fits = fits && sh <= th;
        return new[] { ox, oy, fits ? 1 : 0 };
    };
    Func<XYZ, double> horizonOf = look => Math.Tan(Math.Asin(Math.Max(-1, Math.Min(1, -look.Z))));

    XYZ eye = null, forward = null;
    double standOff = 0;
    var compUsed = comp;
    // True when the lens found the distance at which the elements fit: the
    // window then holds them by construction, on the edge of fitting, and is
    // never second-guessed below (row 5b-1304's change is for the other roads).
    var lensFitted = false;
    if (interior)
    {
        // INSIDE THE ROOM, looking straight across (version 4, row 5b-1303).
        // Version 3 stood the eye 600 mm in from the face of the elements' BOX.
        // On Revit 2020's sample house the box of 'Kitchen & Dining 101' ran to
        // x 7560 mm, past its wall line x 6860 to 7062, so side right put the
        // eye at x 6960 - inside a double door's frame - and the Raytracer drew
        // the picture black. So the eye comes in from the box's face, 50 mm at a
        // time and then halving, until it is inside the room - the room handed
        // in, or the one at the elements' middle - stands the inset in from
        // there, and steps further in, 100 mm at a time, until nothing solid is
        // within 150 mm of it (ElementIntersectsSolidFilter on a cube round the
        // eye), never past half way to the far side. No such point is refused,
        // naming what is in the way.
        var outward = new XYZ(sa, ca, 0);
        var reach = corners8.Max(c => (c - centre).DotProduct(outward));
        var farSide = -corners8.Max(c => (c - centre).DotProduct(outward.Negate()));
        var inset = distance ?? 600 / MmPerFoot;
        var eyeZ = baseZ + (eyeH ?? 1400 / MmPerFoot);
        Func<double, XYZ> outAt = s => new XYZ(centre.X + sa * s, centre.Y + ca * s, eyeZ);
        var room = boxed.OfType<Room>().FirstOrDefault();
        var space = room == null ? boxed.OfType<Space>().FirstOrDefault() : null;
        var roomFound = false;
        if (room == null && space == null)
        {
            try { room = doc.GetRoomAtPoint(outAt(0)); } catch (Exception) { room = null; }
            roomFound = room != null;
        }
        var haveRoom = room != null || space != null;
        var roomName = room != null ? "'" + room.Name + "'" : space != null ? "'" + space.Name + "'" : "the elements' box";
        Func<XYZ, bool> inRoom = p =>
        {
            try { return room != null ? room.IsPointInRoom(p) : space == null || space.IsPointInSpace(p); }
            catch (Exception) { return false; }
        };
        // Where the room really starts on that side, coming in from the box.
        var wallAt = reach;
        var entered = !haveRoom;
        if (haveRoom)
        {
            double? lastOut = null;
            for (var s = reach; s >= farSide; s -= 50 / MmPerFoot)
            {
                if (inRoom(outAt(s))) { wallAt = s; entered = true; break; }
                lastOut = s;
            }
            if (entered && lastOut != null)
            {
                double o = lastOut.Value, i = wallAt;
                for (var k = 0; k < 12; k++) { var mid = (o + i) / 2; if (inRoom(outAt(mid))) i = mid; else o = mid; }
                wallAt = i;
            }
        }
        var middle = (wallAt + farSide) / 2;
        var firstTry = wallAt - inset;
        if (firstTry <= middle) firstTry = (wallAt + middle) / 2;
        // Nothing solid within 150 mm of the eye - a wall, a door's frame, a
        // cupboard. Rooms, spaces and areas are not solids here.
        var clearance = 150 / MmPerFoot;
        var notSolid = new List<ElementId> { new ElementId(BuiltInCategory.OST_Rooms),
            new ElementId(BuiltInCategory.OST_MEPSpaces), new ElementId(BuiltInCategory.OST_Areas) };
        string clearanceUnread = null;
        Func<XYZ, List<Element>> inTheWayAt = p =>
        {
            var found = new List<Element>();
            try
            {
                var lo = p - new XYZ(clearance, clearance, clearance);
                var c1 = lo;
                var c2 = lo + new XYZ(2 * clearance, 0, 0);
                var c3 = lo + new XYZ(2 * clearance, 2 * clearance, 0);
                var c4 = lo + new XYZ(0, 2 * clearance, 0);
                var cube = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop>
                {
                    CurveLoop.Create(new List<Curve>
                        { Line.CreateBound(c1, c2), Line.CreateBound(c2, c3), Line.CreateBound(c3, c4), Line.CreateBound(c4, c1) }),
                }, XYZ.BasisZ, 2 * clearance);
                foreach (var e in new FilteredElementCollector(doc).WhereElementIsNotElementType()
                    .WherePasses(new BoundingBoxIntersectsFilter(new Outline(lo, p + new XYZ(clearance, clearance, clearance))))
                    .WherePasses(new ElementIntersectsSolidFilter(cube)))
                    if (e.Category != null && e.Category.CategoryType == CategoryType.Model && !notSolid.Contains(e.Category.Id))
                        found.Add(e);
            }
            catch (Exception ex) { clearanceUnread = ex.Message; }
            return found;
        };
        Func<List<Element>, string> nameThem = list => string.Join(", ", list.Take(3).Select(e => e.Category.Name + " '"
            + e.Name + "' (id " + e.Id + ")")) + (list.Count > 3 ? " and " + (list.Count - 3) + " more" : "");
        var at = firstTry;
        var clear = false;
        List<Element> firstInTheWay = null;
        if (entered)
            for (; at >= middle - 1e-9; at -= 100 / MmPerFoot)
            {
                if (!inRoom(outAt(at))) continue;
                var there = inTheWayAt(outAt(at));
                if (there.Count == 0) { clear = true; break; }
                if (firstInTheWay == null) firstInTheWay = there;
            }
        var sideWord = sideKey != "none" ? side.Trim() : sideUsed != "none" ? sideUsed : directionAsked.Trim();
        if (!entered)
            refused = "The line through the middle of " + roomName + " toward its " + sideWord + " side never enters the room "
                + mm(eyeZ - baseZ) + " mm up - the room may be unplaced, not enclosed, or lower than the eye. Select its "
                + "walls, or stand the camera by hand with SET_VIEW_CAMERA.";
        else if (!clear)
            refused = "No point from " + mm(inset) + " mm in from " + roomName + (haveRoom ? "'s own boundary" : "") + " on the "
                + sideWord + " side to half way across, " + mm(eyeZ - baseZ) + " mm up, is "
                + (firstInTheWay != null ? "clear of solids - " + nameThem(firstInTheWay) + " in the way" : "inside the room")
                + ". Give a distanceMm, another side, or stand the camera by hand with SET_VIEW_CAMERA.";
        if (refused != null)
            refused += " " + (madeNew ? "The new view was made; it is not framed." : "NOTHING WAS CHANGED.");
        else
        {
            standOff = at;
            eye = outAt(at);
            forward = lookFor(eye);
            findings.Add("Interior: the eye stands " + mm(wallAt - at) + " mm in from " + roomName
                + (haveRoom ? "'s own boundary" : "") + " on the " + sideWord + " side"
                + (haveRoom && reach - wallAt > 1 / MmPerFoot ? ", which is " + mm(reach - wallAt) + " mm inside the elements' "
                    + "box there" : "")
                + (at < firstTry - 1e-9 ? " - " + mm(firstTry - at) + " mm further in than " + mm(wallAt - firstTry) + " mm"
                    + (firstInTheWay != null ? ", to be clear of " + nameThem(firstInTheWay) : "") : "")
                + (roomFound ? "; the room is the one at the elements' middle" : "")
                + (haveRoom ? "" : "; no room was handed in or found at their middle, so the room's boundary was not used")
                + (clearanceUnread == null ? "; nothing solid is within 150 mm of it." : "; whether anything solid is near it "
                    + "could not be checked (" + clearanceUnread + ") - look at the view."));
        }
    }
    else if (lensF == null)
    {
        // No lens: the window is fitted to the elements, as version 2 does.
        standOff = distance ?? halfDiagonal * 2.5;
        eye = eyeFor(standOff);
        forward = lookFor(eye);
    }
    else if (distance != null)
    {
        standOff = distance.Value;
        eye = eyeFor(standOff);
        forward = lookFor(eye);
    }
    else
    {
        // THE LENS DECIDES THE DISTANCE: the nearest eye at which the elements
        // fit the lens's window with the margin, found by stepping out 2% at a
        // time and then halving the last step.
        var tw0 = 18.0 / lensF.Value;
        var th0 = tw0 / shape;
        Func<double, string, bool> fitsAt = (dist, how) =>
        {
            var e = eyeFor(dist);
            var f = lookFor(e);
            var b = seen(e, f);
            if (b[4] < 1.0 || b[0] > b[1]) return false;
            return place(b, tw0, th0, horizonOf(f), how)[2] > 0.5;
        };
        Func<string, double?> nearestFit = how =>
        {
            var lowD = 1.0;
            var highD = Math.Max(halfDiagonal * 400, 3300);
            var steps = 600;
            var ratio = Math.Pow(highD / lowD, 1.0 / steps);
            double previous = lowD, found = -1;
            for (var i = 0; i <= steps; i++)
            {
                var dist = lowD * Math.Pow(ratio, i);
                if (fitsAt(dist, how)) { found = dist; break; }
                previous = dist;
            }
            if (found < 0) return null;
            if (found > lowD)
                for (var i = 0; i < 40; i++)
                {
                    var mid = (previous + found) / 2;
                    if (fitsAt(mid, how)) found = mid; else previous = mid;
                }
            return found;
        };
        var nearestD = nearestFit(comp);
        if (nearestD == null && comp != "centre")
        {
            nearestD = nearestFit("centre");
            if (nearestD != null)
            {
                findings.Add("A " + comp + " composition cannot hold the elements from this angle with this lens - "
                    + "they were centred instead.");
                compUsed = "centre";
            }
        }
        lensFitted = nearestD != null;
        if (nearestD == null)
        {
            findings.Add("The elements do not fit a " + deg(lensF.Value) + " mm lens from this angle at any distance "
                + "up to " + mm(Math.Max(halfDiagonal * 400, 3300)) + " mm - the eye stands at two and a half "
                + "times their half-diagonal and the picture may cut them.");
            nearestD = halfDiagonal * 2.5;
        }
        standOff = nearestD.Value;
        eye = eyeFor(standOff);
        forward = lookFor(eye);
    }

    // An interior with nowhere to stand was refused above (row 5b-1303): the
    // view is then left as it is.
    if (refused == null) try
    {
        var upward = upFor(forward);
        if (camera.IsLocked) camera.Unlock();
        camera.SetOrientation(new ViewOrientation3D(eye, upward, forward));
        doc.Regenerate();

        // The window, in Revit's own frame for the view, at the distance it is
        // drawn (row 5b-1301, windowDistance above).
        var frame = camera.CropBox.Transform;
        var toView = frame.Inverse;
        var nearPlane = windowDistance(camera);
        double xl = double.MaxValue, xh = double.MinValue, yl = double.MaxValue, yh = double.MinValue, deepest = 0;
        foreach (var c in corners8)
        {
            var p = toView.OfPoint(c);
            var depth = Math.Max(-p.Z, nearPlane);
            deepest = Math.Max(deepest, depth);
            xl = Math.Min(xl, p.X / depth); xh = Math.Max(xh, p.X / depth);
            yl = Math.Min(yl, p.Y / depth); yh = Math.Max(yh, p.Y / depth);
        }
        double wxLo, wxHi, wyLo, wyHi;
        var horizon = horizonOf(forward);
        if (lensF == null)
        {
            var growX = (xh - xl) * (margin.Value / 200.0);
            var growY = (yh - yl) * (margin.Value / 200.0);
            wxLo = xl - growX; wxHi = xh + growX; wyLo = yl - growY; wyHi = yh + growY;
        }
        else
        {
            var tw = 18.0 / lensF.Value;
            var th = tw / shape;
            double ox, oy;
            if (interior)
            {
                ox = compUsed == "thirds-left" ? tw / 3 : compUsed == "thirds-right" ? -tw / 3 : 0;
                oy = compUsed == "low-horizon" ? horizon + th / 3 : 0;
            }
            else
            {
                var at = place(new[] { xl, xh, yl, yh }, tw, th, horizon, compUsed);
                ox = at[0];
                oy = at[1];
                // A WINDOW THAT CANNOT HOLD THEM (version 4, row 5b-1304). With
                // distanceMm given, or no distance found for the lens, the window
                // was still put on the middle of the box's projected corners -
                // and the corners nearest the eye project far off the picture,
                // so on Revit 2020's sample house ten dining chairs and their
                // tables at 2500 mm, cinematic, gave a window at Y -36 to -19 mm
                // and a picture of table legs. A composition that cannot hold
                // them is dropped for centre; a window that cannot hold them even
                // centred is put on the elements' MIDDLE - straight ahead when the
                // middle is behind the window - on each side it is too small for,
                // and the answer says so.
                if (at[2] < 0.5 && !lensFitted)
                {
                    var box4 = new[] { xl, xh, yl, yh };
                    var centred = compUsed == "centre" ? at : place(box4, tw, th, horizon, "centre");
                    var compWas = compUsed;
                    compUsed = "centre";
                    if (centred[2] > 0.5)
                    {
                        ox = centred[0];
                        oy = centred[1];
                        findings.Add("A " + compWas + " composition cannot hold the elements from here with this lens - they "
                            + "were centred instead.");
                    }
                    else
                    {
                        var middleSeen = toView.OfPoint(centre);
                        var middleDepth = -middleSeen.Z;
                        var mx = middleDepth > nearPlane ? middleSeen.X / middleDepth : 0;
                        var my = middleDepth > nearPlane ? middleSeen.Y / middleDepth : 0;
                        var wide = (xh - xl) / 2 * (1 + margin.Value / 100.0) > tw * (1 + 1e-6);
                        var tall = (yh - yl) / 2 * (1 + margin.Value / 100.0) > th * (1 + 1e-6);
                        ox = wide ? mx : centred[0];
                        oy = tall ? my : centred[1];
                        findings.Add("At " + mm(standOff) + " mm from their middle the elements are "
                            + (wide && tall ? "wider and taller" : wide ? "wider" : "taller") + " than a " + deg(lensF.Value)
                            + " mm lens takes in, so the picture is centred on their middle and shows only the part of them "
                            + "that fits" + (compWas != "centre" ? " (composition " + compWas + " dropped)" : "")
                            + " - give a larger distanceMm, a wider lens, or leave distanceMm out for the lens to set it.");
                    }
                }
            }
            wxLo = ox - tw; wxHi = ox + tw; wyLo = oy - th; wyHi = oy + th;
        }
        reachFar(camera, deepest);
        doc.Regenerate();
        var wanted = new BoundingBoxXYZ();
        wanted.Transform = frame;
        // The Z bottom past the deepest corner, as on version 2's road (row 5b-1302).
        wanted.Min = new XYZ(wxLo * nearPlane, wyLo * nearPlane, Math.Min(camera.CropBox.Min.Z, -deepest * 1.1));
        wanted.Max = new XYZ(wxHi * nearPlane, wyHi * nearPlane, camera.CropBox.Max.Z);
        camera.CropBoxActive = true;
        camera.CropBox = wanted;
        doc.Regenerate();

        var target = eye + forward * Math.Max((centre - eye).DotProduct(forward), 1 / MmPerFoot);
        var lookText = Math.Abs(forward.Z) < 1e-6 ? "level - verticals stay vertical"
            : forward.Z < 0 ? "looking down " + deg(Math.Asin(-forward.Z) * 180 / Math.PI) + "°"
            : "looking up " + deg(Math.Asin(forward.Z) * 180 / Math.PI) + "°";
        cinematicText = (hasShot ? "Shot " + shotTitle[shotKey].ToLowerInvariant() + ": " : "Camera: ")
            + "stood on " + standText + ", azimuth " + deg(standAz) + "° (" + compassOf(standAz) + "); eye "
            + mm(eye.Z - baseZ) + " mm above " + baseName + ", " + mm(standOff) + " mm "
            + (interior ? "out from the elements' middle, inside them" : eyeH != null ? "from their middle in plan"
                : "from their middle") + "; aimed at " + mm(target.X) + "," + mm(target.Y) + "," + mm(target.Z)
            + " mm; " + lookText + "; " + (lensF != null ? deg(lensF.Value) + " mm lens asked" : "no lens asked - "
                + "the window fitted to the elements") + (lensF != null ? "; picture " + Math.Round(shape, 2).ToString(culture) + ":1" : "")
            + "; composition " + compUsed + ".";
        if (interior)
            findings.Add("The camera stands inside the elements, so their boxes cannot be inside the picture - "
                + "`framed` counts none of a room it stands in; look at the view.");
    }
    catch (Exception ex)
    {
        refused = "Revit refused the camera: " + ex.Message + ". " + (madeNew ? "The new view was made; it is not framed."
            : "NOTHING WAS CHANGED.");
    }
}

if (reachedCamera)
{
    // ---- read back ----
    var o = camera.GetOrientation();
    var crop = camera.CropBox;
    var back = crop.Transform.Inverse;
    var drawnAt = camera.IsPerspective ? windowDistance(camera) : 0;
    foreach (var element in boxed)
    {
        var box = element.get_BoundingBox(null);
        var inside = true;
        foreach (var x in new[] { box.Min.X, box.Max.X })
            foreach (var y in new[] { box.Min.Y, box.Max.Y })
                foreach (var z in new[] { box.Min.Z, box.Max.Z })
                {
                    // Seen through the window: the corner projected onto the
                    // plane the window is drawn on (row 5b-1301).
                    var p = back.OfPoint(box.Transform.OfPoint(new XYZ(x, y, z)));
                    var near = drawnAt;
                    var depthHere = -p.Z;
                    var tol = 0.01 / MmPerFoot;
                    if (depthHere <= 0) { inside = false; continue; }
                    var sx = camera.IsPerspective ? p.X * near / depthHere : p.X;
                    var sy = camera.IsPerspective ? p.Y * near / depthHere : p.Y;
                    if (sx < crop.Min.X - tol || sx > crop.Max.X + tol || sy < crop.Min.Y - tol || sy > crop.Max.Y + tol
                        || p.Z < crop.Min.Z - tol) inside = false;
                }
        if (inside) framed++;
    }
    var farActive = camera.get_Parameter(BuiltInParameter.VIEWER_BOUND_ACTIVE_FAR);
    var farOffset = camera.get_Parameter(BuiltInParameter.VIEWER_BOUND_OFFSET_FAR);
    readBack = "'" + camera.Name + "': eye " + mm(o.EyePosition.X) + "," + mm(o.EyePosition.Y) + "," + mm(o.EyePosition.Z)
        + " mm looking " + Math.Round(o.ForwardDirection.X, 4).ToString(culture) + ","
        + Math.Round(o.ForwardDirection.Y, 4).ToString(culture) + "," + Math.Round(o.ForwardDirection.Z, 4).ToString(culture)
        + ", " + (camera.IsPerspective ? "perspective" : "isometric") + "; crop box in the view's frame X "
        + mm(crop.Min.X) + " to " + mm(crop.Max.X) + ", Y " + mm(crop.Min.Y) + " to " + mm(crop.Max.Y) + ", Z "
        + mm(crop.Min.Z) + " to " + mm(crop.Max.Z) + " mm; far clip "
        + (farActive != null && farActive.AsInteger() == 1
            ? "on at " + (farOffset != null ? mm(farOffset.AsDouble()) + " mm" : "an offset Revit did not give") : "off")
        + "; " + framed + " of " + boxed.Count + " element(s) inside that crop box"
        + (camera.IsPerspective ? ", its window drawn " + mm(drawnAt) + " mm in front of the eye." : ".");
    // Version 4 (row 5b-1307): the view's phase and phase filter, as it holds them.
    readBack += " Its " + phaseOf(camera) + ".";

    // THE LENS THE WINDOW AMOUNTS TO - version 3. The window sits on the near
    // plane, so its angle across is the two edges' angles from the eye, and a
    // 35 mm-film lens of that width is 36 x near / width.
    var nearHere = drawnAt;
    var width = crop.Max.X - crop.Min.X;
    var height = crop.Max.Y - crop.Min.Y;
    if (camera.IsPerspective && nearHere > 0 && width > 0 && height > 0)
        lens = "about " + Math.Round(36 * nearHere / width).ToString(culture) + " mm on 35 mm film - "
            + deg((Math.Atan(crop.Max.X / nearHere) - Math.Atan(crop.Min.X / nearHere)) * 180 / Math.PI) + "° across, "
            + deg((Math.Atan(crop.Max.Y / nearHere) - Math.Atan(crop.Min.Y / nearHere)) * 180 / Math.PI)
            + "° up and down, the window " + mm(width) + " x " + mm(height) + " mm at " + mm(nearHere)
            + " mm in front of the eye";
    if (cinematic)
    {
        var eyeElevation = camera.get_Parameter(BuiltInParameter.VIEWER_EYE_ELEVATION);
        var targetElevation = camera.get_Parameter(BuiltInParameter.VIEWER_TARGET_ELEVATION);
        readBack += " " + cinematicText + " Lens: " + (lens.Length > 0 ? lens : "not read") + "."
            + (eyeElevation != null && targetElevation != null
                ? " Revit shows Eye Elevation " + mm(eyeElevation.AsDouble()) + " mm and Target Elevation "
                    + mm(targetElevation.AsDouble()) + " mm."
                : "");
    }
    if (refused == null && !cinematic)
        findings.Add((madeNew ? "Made perspective view '" : "Re-aimed '") + camera.Name + "' at " + boxed.Count
            + " element(s) from the " + (compass.ContainsKey(d) ? directionAsked.Trim() : azimuth + "°") + ", looking down "
            + lookDown + "°: " + readBack);
    if (refused == null && cinematic)
        findings.Insert(0, (madeNew ? "Made perspective view '" : "Re-aimed '") + camera.Name + "' at " + boxed.Count
            + " element(s): " + readBack);
}
// Version 4 (row 5b-1302): what the two helpers could not do, said.
if (reachedCamera && windowAssumed)
    findings.Add("The view's crop shape could not be read, so its window was taken as drawn 30 mm (0.1 ft) in front of "
        + "the eye, as Revit 2020 draws it - the lens and `framed` above lean on that; look at the picture.");
if (reachedCamera && farClipProblem != null) findings.Add(farClipProblem);
if (noGeometry.Count > 0) findings.Add(noGeometry.Count + " element(s) have no geometry and were left out of the frame");
if (refused != null) findings.Add(refused);
