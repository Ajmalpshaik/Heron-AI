// NOT STANDALONE. Assumes `doc`, `systemType`, `pipeType`, `level` and `points`
// are in scope; leaves `created`, `tooShort` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Points are internal FEET.
//
// ===========================================================================
// WHAT THIS MAKES IS PIPE. IT IS NOT A SYSTEM.
// ===========================================================================
//
// Each segment is created on its own. Nothing here adds a fitting, a tap or a
// reducer, and nothing here asserts that two segments sharing an endpoint come
// out CONNECTED - that is a question about Revit's behaviour, and this fragment
// does not answer questions it has not measured.
//
// The failure this guards against looks exactly like success: a run that reads
// correctly on screen, prints correctly, and has an open end at every joint.
// TRACE_CONNECTIVITY is how it is found and CONNECT_OPEN_ENDS is what closes
// it, and the proof cases below require the check rather than suggesting it.
//
// SIZE IS NOT SET HERE, AND THE REASON IS MEASURED, NOT TIDINESS. Ask a pipe
// for 77 mm and Revit returns TRUE from the write and gives you 80 mm - it
// snaps to the nearest size the pipe type's segment table carries. A write that
// succeeds while doing something else is exactly the defect this project exists
// around, so it is caught in ONE place: SET_MEP_SIZE, which also has to serve
// resizing pipe that already exists.
//
// A SEGMENT TOO SHORT TO EXIST IS SKIPPED, NOT ATTEMPTED. Two points closer
// together than the document's own short-curve tolerance cannot make a curve.
// The tolerance is read FROM THE DOCUMENT rather than written here as a number,
// because it is the document's setting and not a constant of the API.
//
// ONE BAD SEGMENT DOES NOT LOSE THE REST. Each create is guarded and a refusal
// is counted rather than thrown - a run that fails at segment nine should still
// leave the eight that worked, with the failure reported.

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
        var pipe = Pipe.Create(doc, systemType.Id, pipeType.Id, level.Id, start, end);
        if (pipe != null) created.Add(pipe);
        else refused++;
    }
    catch
    {
        // A pipe type with no segment or routing preference that suits is the
        // usual cause, and it is a project question rather than a fault here.
        // Counted, so a run that half-worked says so instead of reading as a
        // clean success.
        refused++;
    }
}
