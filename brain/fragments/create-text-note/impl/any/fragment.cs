// NOT STANDALONE. Assumes `doc`, `view`, `at`, `noteText`, `textTypeId`,
// `listType` and `listStartNumber` are in scope; leaves `created`,
// `unverified`, `noteFormatting` and `refused` behind.
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
//
// ===========================================================================
// VERSION 2 - PARAGRAPHS, AND A NUMBERED OR BULLETED LIST
// ===========================================================================
//
// A PARAGRAPH BREAK IS TYPED `\r`, A SOFT LINE BREAK (Shift+Enter) `\v`, AND A
// BACKSLASH `\\` - the marks EDIT_TEXT_VALUES takes and READ_ELEMENT_PARAMETERS
// shows a note's wording in. A chat's values arrive one per line, so without
// them a note could only ever be one paragraph. A real line feed counts as a
// paragraph break: Revit allows none in a note. TextNote.Create itself makes a
// new line at '\r' and '\v' - its own remarks, the same in RevitAPI.xml for
// every release 2020 to 2027. A paragraph end typed at the very end is the
// closing one Revit adds anyway, so it is dropped rather than left as an
// empty last paragraph nobody can see.
//
// `listType` IS ONE WORD FOR EVERY PARAGRAPH, OR ONE PER PARAGRAPH WITH COMMAS
// BETWEEN, and the last one carries on to the paragraphs after it - so
// "None, ArabicNumbers" keeps a GENERAL NOTES heading out of the numbering.
// Left out it is "None", and the note is made exactly as version 1 made it.
// The words are Revit's list types - None, Bullet, ArabicNumbers,
// LowerCaseLetters, UpperCaseLetters - or the plain ones EDIT_TEXT_VALUES uses:
// no list, bullets, numbers, lowercase letters, uppercase letters.
//
// `listStartNumber` IS THE NUMBER THE FIRST NUMBERED OR LETTERED PARAGRAPH
// TAKES - 3 for "3.", 2 for "b.". Left out it is 1, Revit's own start. A later
// list in the same note, after a paragraph with no list, starts at 1 as Revit
// starts it.
//
// REFUSED BEFORE THE NOTE IS MADE, each with what to do: a list word Revit has
// not got, more list words than paragraphs, a start number that is not a whole
// number or is outside Revit's range, a start number other than 1 on a note
// with nothing numbered or lettered, and two DIFFERENT lists on paragraphs
// side by side. That last one is Autodesk's own rule (SetListType's remarks,
// every release 2020 to 2027): paragraphs next to each other in lists at the
// same indent are ONE list, and a list type set on one reaches all of them -
// so a bullet straight after a number cannot stand, and saying so beats a
// note that comes back numbered where bullets were asked for.
//
// IT READS THE NOTE BACK. After the list is set, the wording and every
// paragraph's list type are read again from the note itself, each list over
// its paragraph WITH the paragraph's closing carriage return - the range the
// getters need, measured for EDIT_TEXT_VALUES version 4 on Revit 2020 - and the
// start number over the first numbered paragraph. A note that reads back other
// than asked is in the drawing, so it goes in `unverified`, never `created`,
// and `noteFormatting` names the paragraph; Ctrl+Z takes it out with the rest
// of the operation's one undo entry.

ElementId created = null;
// Made, but reads back other than asked - the note is in the drawing. Never a
// note made as asked (D-52).
ElementId unverified = null;
// The paragraphs and each one's list, as READ BACK from the note. A sentence
// about what was made, never a thing it found (D-52).
string noteFormatting = null;
string refused = null;

// `\r` a paragraph end, `\v` a soft line break, `\\` a backslash. Anything else
// after a backslash is left as typed.
var given = (noteText ?? "").Replace("\r\n", "\r").Replace('\n', '\r');
var built = new System.Text.StringBuilder();
for (int c = 0; c < given.Length; c++)
{
    if (given[c] == '\\' && c + 1 < given.Length)
    {
        var mark = given[c + 1];
        if (mark == 'r') { built.Append('\r'); c++; continue; }
        if (mark == 'v') { built.Append('\v'); c++; continue; }
        if (mark == '\\') { built.Append('\\'); c++; continue; }
    }
    built.Append(given[c]);
}
var text = built.ToString().TrimEnd('\r');
var paragraphs = text.Split('\r');

// The list each paragraph is to wear, decided before anything is made.
var wantedLists = new List<ListType>();
string listProblem = null;
Func<ListType, string> listName = value =>
    value == ListType.None ? "no list" : value.ToString();
var listWords = (listType ?? "").Trim().Length == 0 ? new[] { "None" } : (listType ?? "").Split(',');
foreach (var rawWord in listWords)
{
    var word = rawWord.Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
    if (word == "none" || word == "nolist") wantedLists.Add(ListType.None);
    else if (word == "bullet" || word == "bullets") wantedLists.Add(ListType.Bullet);
    else if (word == "arabicnumbers" || word == "numbers") wantedLists.Add(ListType.ArabicNumbers);
    else if (word == "lowercaseletters") wantedLists.Add(ListType.LowerCaseLetters);
    else if (word == "uppercaseletters") wantedLists.Add(ListType.UpperCaseLetters);
    else if (word.Length == 0)
    {
        listProblem = "listType has an empty place between two commas, so NO NOTE WAS MADE. Give one "
            + "list type per paragraph, or one for the whole note";
        break;
    }
    else
    {
        listProblem = string.Format(
            "'{0}' is not a list Revit has, so NO NOTE WAS MADE. The list types are None, Bullet, "
            + "ArabicNumbers, LowerCaseLetters and UpperCaseLetters (or no list, bullets, numbers, "
            + "lowercase letters, uppercase letters) - one for every paragraph, or one per paragraph "
            + "with commas between", rawWord.Trim());
        break;
    }
}
if (listProblem == null && wantedLists.Count > paragraphs.Length)
{
    listProblem = string.Format(
        "{0} list types were given for a note of {1} paragraph(s), so NO NOTE WAS MADE. A paragraph "
        + "ends where \\r is typed; a soft line break (Shift+Enter) is \\v and stays inside its "
        + "paragraph. Give one list type for the whole note, or one per paragraph",
        wantedLists.Count, paragraphs.Length);
}
var lists = new List<ListType>();
for (int k = 0; k < paragraphs.Length; k++)
    lists.Add(wantedLists.Count == 0 ? ListType.None : wantedLists[Math.Min(k, wantedLists.Count - 1)]);
if (listProblem == null)
{
    for (int k = 0; k + 1 < lists.Count; k++)
    {
        if (lists[k] != ListType.None && lists[k + 1] != ListType.None && lists[k] != lists[k + 1])
        {
            listProblem = string.Format(
                "paragraphs {0} and {1} are side by side and were asked to be {2} and {3}, so NO NOTE "
                + "WAS MADE. Revit makes paragraphs next to each other in lists ONE list with one kind of "
                + "number or bullet - put a paragraph with no list between them, or give both the same",
                k + 1, k + 2, listName(lists[k]), listName(lists[k + 1]));
            break;
        }
    }
}

// The first paragraph a start number can sit on: numbers or letters, not a bullet.
int firstNumbered = -1;
for (int k = 0; k < lists.Count; k++)
{
    if (lists[k] == ListType.ArabicNumbers || lists[k] == ListType.LowerCaseLetters
        || lists[k] == ListType.UpperCaseLetters) { firstNumbered = k; break; }
}
int startNumber = 1;
if (listProblem == null)
{
    var startText = (listStartNumber ?? "").Trim();
    if (startText.Length > 0 && !int.TryParse(startText, out startNumber))
    {
        listProblem = string.Format(
            "the list start number '{0}' is not a whole number, so NO NOTE WAS MADE. Give 1 for 1. or "
            + "a., 3 for 3. or c.", startText);
    }
    else if (startText.Length == 0) startNumber = 1;
}
if (listProblem == null && startNumber != 1 && firstNumbered < 0)
{
    listProblem = string.Format(
        "a list start number of {0} was given and no paragraph is numbered or lettered, so NO NOTE WAS "
        + "MADE. A bullet list and a paragraph with no list have no number to start from - give "
        + "listType ArabicNumbers, LowerCaseLetters or UpperCaseLetters", startNumber);
}
if (listProblem == null && startNumber != 1)
{
    // Revit's own range for a start number, asked of a FormattedText of its
    // own before the note exists. A Revit that will not say leaves the
    // setter below to refuse, which is caught.
    int lowest = 1, highest = int.MaxValue;
    try
    {
        using (var probe = new FormattedText())
        {
            lowest = probe.GetMinimumListStartNumber();
            highest = probe.GetMaximumListStartNumber();
        }
    }
    catch { lowest = 1; highest = int.MaxValue; }
    if (startNumber < lowest || startNumber > highest)
    {
        listProblem = string.Format(
            "a list start number of {0} is outside what Revit takes ({1} to {2}), so NO NOTE WAS MADE",
            startNumber, lowest, highest);
    }
}

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
else if (listProblem != null)
{
    refused = listProblem;
}
else
{
    var note = TextNote.Create(doc, view.Id, at, text, textTypeId);
    if (note == null) refused = "Revit declined to place the note";
    else
    {
        // Where each paragraph starts in the note's own text.
        var starts = new List<int>();
        int position = 0;
        foreach (var piece in paragraphs) { starts.Add(position); position += piece.Length + 1; }

        bool anyList = false;
        foreach (var one in lists) if (one != ListType.None) anyList = true;

        string setFailed = null;
        if (anyList)
        {
            try
            {
                var shaped = note.GetFormattedText();
                for (int k = 0; k < paragraphs.Length; k++)
                {
                    if (lists[k] == ListType.None) continue;
                    shaped.SetListType(new TextRange(starts[k], paragraphs[k].Length + 1), lists[k]);
                }
                if (startNumber != 1 && firstNumbered >= 0)
                    shaped.SetListStartNumber(
                        new TextRange(starts[firstNumbered], paragraphs[firstNumbered].Length + 1), startNumber);
                note.SetFormattedText(shaped);
            }
            catch (Exception ex) { setFailed = ex.Message; }
        }

        // READ BACK - the wording, then every paragraph's list, from the note.
        var drift = new List<string>();
        var readLists = new List<ListType>();
        int startAfter = -1;
        if (setFailed != null)
        {
            drift.Add(string.Format("Revit made the note but would not take its list ({0})", setFailed));
        }
        else
        {
            try
            {
                var back = note.GetFormattedText();
                var wording = (back.GetPlainText() ?? "").TrimEnd('\r', '\n');
                if (wording != text)
                    drift.Add("its wording reads back different from what was asked");
                else
                {
                    for (int k = 0; k < paragraphs.Length; k++)
                    {
                        var read = back.GetListType(new TextRange(starts[k], paragraphs[k].Length + 1));
                        readLists.Add(read);
                        if (read != lists[k])
                            drift.Add(string.Format("paragraph {0} was asked to be {1} and reads back {2}",
                                k + 1, listName(lists[k]), listName(read)));
                    }
                    if (firstNumbered >= 0)
                    {
                        startAfter = back.GetListStartNumber(
                            new TextRange(starts[firstNumbered], paragraphs[firstNumbered].Length + 1));
                        if (startAfter != startNumber)
                            drift.Add(string.Format("the list was asked to start at {0} and starts at {1}",
                                startNumber, startAfter));
                    }
                }
            }
            catch (Exception ex)
            {
                drift.Add(string.Format("Revit would not hand its text back to be checked ({0})", ex.Message));
            }
        }

        if (drift.Count > 0)
        {
            unverified = note.Id;
            noteFormatting = string.Format("note {0}: made, and in the drawing, but it READS BACK DIFFERENT - "
                + "{1}. Ctrl+Z takes it out", note.Id, string.Join("; ", drift));
        }
        else
        {
            created = note.Id;
            // "1 no list, 2-4 ArabicNumbers from 1" - runs of paragraphs with the same list.
            var runs = new List<string>();
            int first = 0;
            for (int k = 1; k <= readLists.Count; k++)
            {
                if (k < readLists.Count && readLists[k] == readLists[first]) continue;
                var span = k - 1 == first ? (first + 1).ToString() : (first + 1) + "-" + k;
                runs.Add(span + " " + listName(readLists[first])
                    + (first <= firstNumbered && firstNumbered < k ? " from " + startAfter : ""));
                first = k;
            }
            noteFormatting = string.Format("note {0}: {1} paragraph(s); read back {2}", note.Id,
                paragraphs.Length, string.Join(", ", runs));
        }
    }
}
