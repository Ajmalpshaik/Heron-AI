// NOT STANDALONE. Assumes `doc`, `systemType`, `ductOrPipeType`, `level` and
// `points` are in scope; leaves `elements`, `tooShort`, `notMade`,
// `typesLoaded`, `report`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry and
// the caller owns it; nothing here opens or commits one. Points are internal
// FEET - the add-in reads the modeller's millimetres once, upstream (D-20).
//
// REVIT'S DUCT PLACEHOLDER AND PIPE PLACEHOLDER: the single-line rough-in a
// run is drawn as before its fittings are wanted. The same run of points as
// CREATE_DUCT and CREATE_PIPE - one placeholder between each consecutive pair,
// so two points give one and five give four - made by the two calls Revit has
// for it, unchanged 2020 to 2027 (read with tools/api-surface --members on
// 2026-11-01):
//   Duct.CreatePlaceholder(doc, systemTypeId, ductTypeId, levelId, start, end)
//   Pipe.CreatePlaceholder(doc, systemTypeId, pipeTypeId, levelId, start, end)
//
// DUCT OR PIPE IS DECIDED BY THE SYSTEM TYPE, NEVER GUESSED. A duct system
// type (Supply Air) makes duct placeholders and needs a duct type; a piping
// system type (Domestic Cold Water) makes pipe placeholders and needs a pipe
// type. A duct type with a piping system, or the other way round, is refused
// by name - Revit would refuse it too, but later and in its own words.
//
// THE SYSTEM TYPE AND THE DUCT OR PIPE TYPE ARE NEVER CHOSEN HERE (D-33). Each
// is named exactly as the Project Browser shows it - "Supply Air", "Radius
// Elbows / Taps", or with its family, "Rectangular Duct: Radius Elbows / Taps".
// Either left out, or a name the model does not have: NOTHING IS DRAWN and
// the loaded ones come back in `typesLoaded` - only the matching discipline's
// once the system type is known.
//
// A SEGMENT TOO SHORT TO EXIST IS SKIPPED, NOT ATTEMPTED - counted in
// `tooShort`, against the document's own short-curve tolerance, as CREATE_DUCT
// does. ONE SEGMENT REVIT REFUSES DOES NOT LOSE THE REST: it is named in
// `notMade` with Revit's words and the loop goes on.
//
// READ BACK: every placeholder made is asked IsPlaceholder after a regenerate,
// and its length is measured from its own location line. One that reads back
// as a real duct or pipe is named in `findings`, never counted as a
// placeholder. Ids stay ElementId, never numbers (D-05).
//
// IT CONNECTS NOTHING AND SIZES NOTHING. Placeholders come out at the type's
// own size; joining them and turning them into real ducts or pipes are
// CONVERT_MEP_PLACEHOLDERS' job and Revit's.

var invariant = System.Globalization.CultureInfo.InvariantCulture;

var elements = new List<Element>();
var tooShort = 0;
var notMade = new List<string>();
var typesLoaded = new List<string>();
var findings = new List<string>();
string refused = "";
string report = "";

Func<double, string> mm = feet => (feet * 304.8).ToString("0", invariant);
Func<XYZ, string> mmPoint = q => mm(q.X) + "," + mm(q.Y) + "," + mm(q.Z);

// A type is named as Revit shows it: its own name, or "Family: Name" as the
// Properties palette writes it ("Family : Name" read the same).
Func<Element, string, bool> isNamed = (e, text) =>
{
    string own;
    try { own = e.Name; } catch { return false; }
    if (string.Equals(own, text, StringComparison.Ordinal)) return true;
    var asType = e as ElementType;
    if (asType == null) return false;
    string family;
    try { family = asType.FamilyName; } catch { return false; }
    if (string.IsNullOrEmpty(family)) return false;
    return string.Equals(family + ": " + own, text.Replace(" : ", ": "), StringComparison.Ordinal);
};
Func<Element, string> shown = e =>
{
    var asType = e as ElementType;
    string family = null;
    if (asType != null) { try { family = asType.FamilyName; } catch { family = null; } }
    return string.IsNullOrEmpty(family) ? "'" + e.Name + "'" : "'" + family + ": " + e.Name + "'";
};
Func<string, bool> leftOut = text =>
    text.Length == 0 || text.Equals("none", StringComparison.OrdinalIgnoreCase);

// ---- what the model has ------------------------------------------------------------------
var ductSystems = new FilteredElementCollector(doc).OfClass(typeof(MechanicalSystemType))
    .Cast<Element>().OrderBy(e => e.Name, StringComparer.Ordinal).ToList();
var pipeSystems = new FilteredElementCollector(doc).OfClass(typeof(PipingSystemType))
    .Cast<Element>().OrderBy(e => e.Name, StringComparer.Ordinal).ToList();
var ductTypes = new FilteredElementCollector(doc).OfClass(typeof(DuctType))
    .Cast<Element>().OrderBy(e => shown(e), StringComparer.Ordinal).ToList();
var pipeTypes = new FilteredElementCollector(doc).OfClass(typeof(PipeType))
    .Cast<Element>().OrderBy(e => shown(e), StringComparer.Ordinal).ToList();

Func<List<Element>, string> listOf = list =>
    list.Count == 0 ? "none" : string.Join(", ", list.Select(e => shown(e)).ToArray());
Action<bool, bool> listTypes = (ducts, pipes) =>
{
    typesLoaded.Clear();
    if (ducts)
    {
        typesLoaded.Add("Duct system types: " + listOf(ductSystems));
        typesLoaded.Add("Duct types: " + listOf(ductTypes));
    }
    if (pipes)
    {
        typesLoaded.Add("Piping system types: " + listOf(pipeSystems));
        typesLoaded.Add("Pipe types: " + listOf(pipeTypes));
    }
};

// ---- the system type decides duct or pipe ------------------------------------------------
var systemText = (systemType ?? "").Trim();
var typeText = (ductOrPipeType ?? "").Trim();
Element systemElement = null;
Element curveTypeElement = null;
var isDuct = false;

if (leftOut(systemText))
{
    listTypes(true, true);
    refused = "No system type was named - Heron never chooses one. Name a duct system type for "
            + "duct placeholders or a piping system type for pipe placeholders; the loaded ones are "
            + "in typesLoaded. Nothing was drawn";
}
else
{
    var ductHits = ductSystems.Where(e => isNamed(e, systemText)).ToList();
    var pipeHits = pipeSystems.Where(e => isNamed(e, systemText)).ToList();
    if (ductHits.Count + pipeHits.Count == 0)
    {
        listTypes(true, true);
        refused = "No duct or piping system type is called '" + systemText + "' - the name has to "
                + "match the Project Browser, capitals included. The loaded ones are in "
                + "typesLoaded. Nothing was drawn";
    }
    else if (ductHits.Count + pipeHits.Count > 1)
    {
        listTypes(ductHits.Count > 0, pipeHits.Count > 0);
        refused = (ductHits.Count + pipeHits.Count) + " system types are called '" + systemText
                + "' - " + string.Join(", ", ductHits.Concat(pipeHits).Select(e => shown(e)
                    + (e is MechanicalSystemType ? " (duct)" : " (piping)")).ToArray())
                + " - so the name does not say which. Nothing was drawn";
    }
    else
    {
        isDuct = ductHits.Count == 1;
        systemElement = isDuct ? ductHits[0] : pipeHits[0];
    }
}

// ---- the duct or pipe type, of the same discipline --------------------------------------
if (refused.Length == 0)
{
    var discipline = isDuct ? "duct" : "pipe";
    var sameKind = isDuct ? ductTypes : pipeTypes;
    var otherKind = isDuct ? pipeTypes : ductTypes;
    if (leftOut(typeText))
    {
        listTypes(isDuct, !isDuct);
        refused = "No " + discipline + " type was named - Heron never chooses one. "
                + shown(systemElement) + " is a " + (isDuct ? "duct" : "piping") + " system type, "
                + "so name one of the " + discipline + " types in typesLoaded. Nothing was drawn";
    }
    else
    {
        var hits = sameKind.Where(e => isNamed(e, typeText)).ToList();
        if (hits.Count == 1) curveTypeElement = hits[0];
        else if (hits.Count > 1)
        {
            listTypes(isDuct, !isDuct);
            refused = hits.Count + " " + discipline + " types are called '" + typeText + "' - "
                    + string.Join(", or ", hits.Select(e => shown(e)).ToArray())
                    + ". Say which, with its family. Nothing was drawn";
        }
        else if (otherKind.Any(e => isNamed(e, typeText)))
        {
            listTypes(isDuct, !isDuct);
            refused = "'" + typeText + "' is a " + (isDuct ? "pipe" : "duct") + " type and "
                    + shown(systemElement) + " is a " + (isDuct ? "duct" : "piping")
                    + " system type - a " + (isDuct ? "duct" : "pipe") + " placeholder needs a "
                    + discipline + " type. Nothing was drawn";
        }
        else
        {
            listTypes(isDuct, !isDuct);
            refused = "No " + discipline + " type is called '" + typeText + "' - the loaded "
                    + discipline + " types are in typesLoaded. Nothing was drawn";
        }
    }
}

// ---- the level and the points -----------------------------------------------------------
if (refused.Length == 0 && level == null)
    refused = "No level was named, and a placeholder is drawn on one. Nothing was drawn";
var pointList = points == null ? new List<XYZ>() : points.ToList();
if (refused.Length == 0 && pointList.Count < 2)
    refused = "A placeholder needs two points, and " + pointList.Count + " was given - give the run "
            + "as points in millimetres, x,y,z each, separated by ; . Nothing was drawn";

// ---- draw ------------------------------------------------------------------------------
if (refused.Length == 0)
{
    var shortest = doc.Application.ShortCurveTolerance;
    for (var i = 0; i + 1 < pointList.Count; i++)
    {
        var start = pointList[i];
        var end = pointList[i + 1];
        var segment = "segment " + (i + 1);
        if (start == null || end == null)
        {
            notMade.Add(segment + ": a point is missing");
            continue;
        }
        if (start.DistanceTo(end) <= shortest)
        {
            // A fault in the POINTS, which the caller can fix - counted apart.
            tooShort++;
            findings.Add(segment + " from " + mmPoint(start) + " to " + mmPoint(end)
                       + " mm is too short to draw, so it was skipped");
            continue;
        }
        try
        {
            MEPCurve made = isDuct
                ? (MEPCurve)Duct.CreatePlaceholder(doc, systemElement.Id, curveTypeElement.Id, level.Id, start, end)
                : (MEPCurve)Pipe.CreatePlaceholder(doc, systemElement.Id, curveTypeElement.Id, level.Id, start, end);
            if (made == null) notMade.Add(segment + ": Revit made nothing and said nothing");
            else elements.Add(made);
        }
        catch (Exception ex)
        {
            notMade.Add(segment + " from " + mmPoint(start) + " to " + mmPoint(end) + " mm: "
                      + "Revit refused it - " + ex.Message);
        }
    }

    // ---- read back ----------------------------------------------------------------------
    if (elements.Count > 0) doc.Regenerate();
    var kind = isDuct ? "duct placeholder" : "pipe placeholder";
    var lines = new List<string>();
    foreach (var made in elements)
    {
        var placeholder = isDuct ? ((Duct)made).IsPlaceholder : ((Pipe)made).IsPlaceholder;
        var location = made.Location as LocationCurve;
        var length = location == null || location.Curve == null ? "length unread" : mm(location.Curve.Length) + " mm";
        if (!placeholder)
            findings.Add("id " + made.Id + " reads back as a real " + (isDuct ? "duct" : "pipe")
                       + ", not a placeholder");
        lines.Add("id " + made.Id + " " + length);
    }
    report = elements.Count + " " + kind + "(s) drawn on " + level.Name + ", system type "
           + shown(systemElement) + ", " + (isDuct ? "duct" : "pipe") + " type "
           + shown(curveTypeElement)
           + (lines.Count > 0 ? ": " + string.Join("; ", lines.ToArray()) : "")
           + ". " + tooShort + " segment(s) too short to draw, " + notMade.Count
           + " refused by Revit" + (notMade.Count > 0 ? " - " + string.Join("; ", notMade.ToArray()) : "")
           + ". Nothing is connected or converted - CONVERT_MEP_PLACEHOLDERS turns them into real "
           + (isDuct ? "ducts" : "pipes");
}
else
{
    report = refused;
}
