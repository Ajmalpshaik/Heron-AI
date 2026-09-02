// NOT STANDALONE. Assumes `doc`, `runKind`, `runType`, `level` and `points` are
// in scope; leaves `created`, `tooShort`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Points are internal FEET.
//
// TRAY AND CONDUIT ARE THE SAME JOB WITH TWO ELEMENTS BEHIND IT, and which one
// is wanted is asked for rather than guessed. They carry different sizes - a
// tray has a width and a height, a conduit a diameter - and they schedule
// separately, so making the wrong one produces a drawing that looks right and a
// schedule that is wrong.
//
// WHAT THIS MAKES IS CONTAINMENT. IT IS NOT A SYSTEM. Nothing here adds a
// fitting or asserts that two segments sharing an endpoint arrive CONNECTED.
// TRACE_CONNECTIVITY finds out; CONNECT_OPEN_ENDS is the fix.
//
// SIZE IS NOT SET HERE. SET_MEP_SIZE applies it afterwards, and is also where
// Revit's snap to the nearest size the type allows is caught - once, in one
// place, for every kind of run.
//
// A SEGMENT TOO SHORT TO EXIST IS SKIPPED, NOT ATTEMPTED, and the tolerance is
// read FROM THE DOCUMENT rather than written here: it is the document's setting
// and not a constant of the API.
//
// ONE BAD SEGMENT DOES NOT LOSE THE REST.

var created = new List<Element>();
var tooShort = 0;
var refused = 0;
var findings = new List<string>();

var kind = (runKind ?? "").Trim().ToLower();
var wantTray = kind == "tray" || kind == "cabletray" || kind == "cable tray";
var wantConduit = kind == "conduit";

var shortest = doc.Application.ShortCurveTolerance;

if (!wantTray && !wantConduit)
{
    findings.Add(string.Format("'{0}' is not a kind of containment this can draw - ask for \"tray\" or "
        + "\"conduit\". They are different elements and guessing produces a schedule that is wrong "
        + "while the drawing looks right", runKind));
}
else if (runType == null)
{
    findings.Add("No type was given, so there is nothing to draw the run with");
}
else
{
    for (var i = 0; i + 1 < points.Count; i++)
    {
        var start = points[i];
        var end = points[i + 1];

        if (start == null || end == null) { refused++; continue; }

        if (start.DistanceTo(end) <= shortest)
        {
            // Counted apart from `refused` because it is a question about the
            // POINTS and not about the model - the caller can fix it.
            tooShort++;
            continue;
        }

        try
        {
            Element made = wantTray
                ? (Element)CableTray.Create(doc, runType.Id, start, end, level.Id)
                : (Element)Conduit.Create(doc, runType.Id, start, end, level.Id);

            if (made != null) created.Add(made);
            else refused++;
        }
        catch
        {
            // A type whose family does not suit is the usual cause - a conduit
            // type handed in for a tray, most often. Counted, so a run that half
            // worked says so instead of reading as a clean success.
            refused++;
        }
    }

    findings.Add(string.Format("{0} {1} segment(s) created, {2} refused, {3} skipped as too short. "
        + "NOTHING is connected and no size has been set",
        created.Count, wantTray ? "cable tray" : "conduit", refused, tooShort));
}
