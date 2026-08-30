// NOT STANDALONE. Assumes `doc`, `systemType`, `ductType`, `level` and `points`
// are in scope; leaves `created`, `tooShort` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ===========================================================================
// WHAT THIS MAKES IS DUCT. IT IS NOT A SYSTEM.
// ===========================================================================
//
// Each segment is created on its own. Nothing here adds a fitting, a tap or a
// transition, and nothing here asserts that two segments sharing an endpoint
// come out CONNECTED - that is a question about Revit's behaviour, and this
// fragment does not answer questions it has not measured.
//
// The failure this guards against is specific and it looks like success: a run
// that reads correctly on screen, prints correctly, and has an open end at
// every joint. Nothing about the drawing says so. TRACE_CONNECTIVITY is how it
// is found, and the proof cases below require it rather than suggesting it.
//
// SIZE IS NOT SET HERE. The duct comes out at its type's size, and SET_MEP_SIZE
// applies width, height or diameter afterwards. That split is not tidiness:
// Revit SNAPS a size to the nearest one the type carries and returns success
// while doing it, so the check for that belongs in one place, and that place
// also has to serve resizing duct that already exists.
//
// A SEGMENT TOO SHORT TO EXIST IS SKIPPED, NOT ATTEMPTED. Two points closer
// together than the document's own short-curve tolerance cannot make a curve,
// and Revit rejects it. The tolerance is read FROM THE DOCUMENT rather than
// written here as a number, because it is the document's setting and not a
// constant of the API.
//
// ONE BAD SEGMENT DOES NOT LOSE THE REST. Each create is guarded, and a refusal
// is counted rather than thrown - a duct run that fails at segment nine should
// still leave the eight that worked, with the failure reported.

var created = new List<Element>();
var tooShort = 0;
var refused = 0;

// The document's own setting. Reading it here rather than hard-coding a number
// means this stays right on a document configured differently.
var shortest = doc.Application.ShortCurveTolerance;

for (var i = 0; i + 1 < points.Count; i++)
{
    var start = points[i];
    var end = points[i + 1];

    if (start == null || end == null)
    {
        refused++;
        continue;
    }

    if (start.DistanceTo(end) <= shortest)
    {
        // Counted apart from `refused` because it is a question about the
        // POINTS, not about the model. Somebody passed the same point twice,
        // or two rounded to the same place - which they can fix.
        tooShort++;
        continue;
    }

    try
    {
        var duct = Duct.Create(doc, systemType.Id, ductType.Id, level.Id, start, end);
        if (duct != null) created.Add(duct);
        else refused++;
    }
    catch
    {
        // A duct type with no matching routing preferences is the usual cause,
        // and it is a project question rather than a fault here. Counted, so a
        // run that half-worked says so instead of reading as a clean success.
        refused++;
    }
}
