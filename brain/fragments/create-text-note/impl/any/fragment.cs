// NOT STANDALONE. Assumes `doc`, `view`, `at`, `noteText` and `textTypeId` are
// in scope; leaves `created` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// NO UNIT CONVERSION HERE. The point arrives as an XYZ, which is already in
// Revit's feet. A millimetre input would need D-20 arithmetic; this one does
// not have one, and adding a conversion "for consistency" would move every
// note by a factor of 304.8.
//
// THE VIEW HAS TO BE ONE A NOTE CAN LIVE IN. A text note is 2D annotation and
// there is no plane for it in a 3D view or a schedule; a view template is not
// a drawing anybody issues. Each is refused with the reason, because the three
// have different fixes and "it did not work" has none.
//
// EMPTY TEXT THROWS, and that is worth knowing rather than discovering. Revit
// rejects an empty or whitespace-only note outright, so it is caught here where
// the message can say what to do about it.
//
// THE TEXT TYPE IS NOT INVENTED. Passing the wrong id makes a note that reads
// correctly on screen and fails the project's drawing standard - the kind of
// error that survives every check until somebody prints. So the id is required
// from the request and validated as a real TextNoteType rather than defaulted
// to whichever one the collector happens to return first.

ElementId created = null;
string refused = null;

var text = noteText ?? "";

if (view == null || view.IsTemplate)
{
    refused = "a text note needs a real view - a view template is not a drawing";
}
else if (view is View3D)
{
    refused = "a text note is 2D annotation and a 3D view has no plane for it - "
            + "put it on the plan or the section instead";
}
else if (view is ViewSchedule)
{
    refused = "a schedule has no drawing area for a note";
}
else if (text.Trim().Length == 0)
{
    refused = "Revit rejects an empty note - there is nothing to place";
}
else if (!(doc.GetElement(textTypeId) is TextNoteType))
{
    refused = "the text type is not a TextNoteType in this project, and the style "
            + "decides the height and font the drawing is checked against";
}
else if (at == null)
{
    refused = "a note needs a point to sit at";
}
else
{
    var note = TextNote.Create(doc, view.Id, at, text, textTypeId);
    if (note == null) refused = "Revit declined to place the note";
    else created = note.Id;
}
