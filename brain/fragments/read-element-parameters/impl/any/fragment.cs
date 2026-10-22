// NOT STANDALONE. Assumes `elements` and `parameterName` are in scope, and
// leaves `values`, `blank`, `absent`, `resolvedFrom` and `noteWording`.
//
// THREE OUTCOMES, NOT TWO, and this is the whole point of the fragment.
//
//   values   the parameter is there and has something in it
//   blank    the parameter is there and is empty
//   absent   the element does not have that parameter at all
//
// Collapsing blank and absent into "no value" is the common shortcut and it
// hides the difference that matters: a blank parameter is a modelling job, an
// absent one is a family or a category problem, and they go to different people.
// A report that says "12 have no system name" when 9 of them cannot have one is
// a report that sends somebody to fix the wrong thing.
//
// ----------------------------------------------------------------------
// VERSION 2 - TWO WAYS THIS ANSWERED "ABSENT" WHEN THE VALUE WAS RIGHT THERE
// ----------------------------------------------------------------------
//
// Version 1 asked `e.LookupParameter(name)` and nothing else. That is correct
// for an instance parameter on a family instance and wrong for two whole
// classes of question a modeller asks every day. Both were found by reading
// what a mature library had already had to solve, not by hitting them here.
//
// 1. THE PARAMETER LIVES ON THE TYPE. Size, fire rating, manufacturer, most of
//    a schedule's columns - all held once on the type, not per instance.
//    LookupParameter on the instance returns null for every one of them, so
//    "what size are these" came back as `absent` for the entire set. The type
//    is now asked second, and `resolvedFrom` records which one answered, so
//    "they all share one value" stays distinguishable from "they each carry
//    the same value" - a different fix in the model.
//
// 2. "LEVEL" IS NOT ONE PARAMETER. There is no single way to ask an element
//    which level it is on, and this is the trap that produces a confident
//    wrong answer rather than an error:
//
//      a wall           has NO level-named parameter at all; only Element.LevelId
//      a duct or pipe   calls it "Reference Level", and Element.LevelId is invalid
//      an air terminal  does have "Level", which is why a spot check looks fine
//
//    Ask for "Level" across a mixed set with LookupParameter alone and the
//    walls and the ducts come back absent while the terminals answer. The
//    report then reads as "most of the model has no level", which is not a
//    thing that can be true. `Element.LevelId` is tried first and five
//    built-in parameters after it, RBS_START_LEVEL_PARAM last because it is
//    the one that catches MEP curves.
//
// The special case is LEVEL ONLY. Category, Family and Type are presentation -
// they are how a table is labelled, not something read off the element - and
// D-20 keeps that at the edge where the user is. Adding them here would put
// this fragment in the business of formatting.
//
// ----------------------------------------------------------------------
// VERSION 3 - "Text" ON A TEXT NOTE IS WHAT THE NOTE SAYS
// ----------------------------------------------------------------------
//
// No Heron tool read a note's wording: asked to "read what the text notes in
// this view say", a chat had to send scratch code. This fragment could not do
// it either, because a note's wording is no parameter - MEASURED 2026-10-17,
// Revit 2020: LookupParameter("Text") on a TextNote is null and
// GetParameters("Text") is empty, so every note answered `absent`.
//
// So `Text` on a text note reads the note itself, by the rule EDIT_TEXT_VALUES
// writes it by: its plain text WITHOUT the closing carriage return Revit always
// adds. `values` carries that wording as it is, `resolvedFrom` says "note", and
// `noteWording` says it so a chat can SEE it - a dictionary in a reply arrives
// as a count. There each note is one line: every paragraph end shown as \r,
// every soft line break (Shift+Enter) as \v and a backslash as \\, which are
// the marks EDIT_TEXT_VALUES takes back; then each paragraph's list type,
// indent and the number the list starts from, as Revit's FormattedText reads
// them; then any character a keyboard does not type plainly - a curly
// apostrophe, U+2019, was in the real note that started this, and a replace
// typed with a straight one finds nothing. Past 20,000 characters the rest are
// counted, not printed.
//
// VERSION 4 (2026-10-17, FRAGMENT-ISSUES 5b-737) adds what EDIT_TEXT_VALUES
// version 5 can now SET, so a check reads exactly what a format wrote: each
// paragraph's bold, italic and underline - "bold 1; partly bold 3; underline
// 5-6", all of a paragraph, part of it, or left out when none - and every
// list's own start number, where version 3 read the first paragraph's only.
//
// Not a text note, or not "Text": everything above, exactly as version 2.

var values = new Dictionary<ElementId, string>();
var blank = new List<ElementId>();
var absent = new List<ElementId>();
var resolvedFrom = new Dictionary<ElementId, string>();

bool askingForLevel = string.Equals((parameterName ?? "").Trim(), "Level",
                                    StringComparison.OrdinalIgnoreCase);
bool askingText = string.Equals((parameterName ?? "").Trim(), "Text",
                                StringComparison.OrdinalIgnoreCase);
// One line per text note read, notes apart by a line break - a note's own
// wording can never hold one (Revit refuses a line feed in a note), so the
// separator cannot be mistaken for the text. Null when no note was read.
string noteWording = null;

// AsValueString() first: it gives the number as the user sees it, with the
// project's own units, which is what a modeller checks against. AsString()
// is the fallback for text parameters, where AsValueString returns null.
Func<Parameter, string> readValue = p =>
{
    string text = null;
    try { text = p.AsValueString(); } catch { }
    if (string.IsNullOrWhiteSpace(text))
    {
        try { text = p.AsString(); } catch { }
    }
    return text;
};

foreach (var e in elements)
{
    if (e == null) continue;

    var note = askingText ? e as TextNote : null;
    if (note != null)
    {
        FormattedText shape = null;
        string plain = null;
        try { shape = note.GetFormattedText(); plain = shape.GetPlainText(); } catch { }
        if (plain == null) { try { plain = note.Text; } catch { } }

        var wording = (plain ?? "").TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(wording)) { blank.Add(e.Id); continue; }
        values[e.Id] = wording;
        resolvedFrom[e.Id] = "note";

        var pieces = wording.Split('\r');
        var lists = new List<string>();
        var styles = new List<string>();
        if (shape == null) lists.Add("unreadable - Revit gave no formatted text");
        else
        {
            // Runs of paragraphs with the same list and indent: "1-8 ArabicNumbers from 1".
            // Read over each paragraph WITH its closing carriage return, so an
            // empty paragraph has a range too.
            int at = 0, first = 0;
            var kinds = new List<string>();
            for (int k = 0; k < pieces.Length; k++)
            {
                var whole = new TextRange(at, pieces[k].Length + 1);
                string kind;
                try { kind = shape.GetListType(whole).ToString(); } catch { kind = "unreadable"; }
                if (kind == "None") kind = "no list";
                int indent = -1;
                try { indent = shape.GetIndentLevel(whole); } catch { }
                if (indent > 0) kind += " indent " + indent;
                kinds.Add(kind);
                at += pieces[k].Length + 1;
            }
            // Each run's own start number (version 4): a note can hold two
            // lists, and the second can start anywhere. -1 off a bullet or no list.
            Func<int, int> startOf = k =>
            {
                int startsAt = 0;
                for (int j = 0; j < k; j++) startsAt += pieces[j].Length + 1;
                try { return shape.GetListStartNumber(new TextRange(startsAt, pieces[k].Length + 1)); }
                catch { return -1; }
            };
            for (int k = 1; k <= kinds.Count; k++)
            {
                if (k < kinds.Count && kinds[k] == kinds[first]) continue;
                var startNumber = startOf(first);
                lists.Add((k - 1 == first ? (first + 1).ToString() : (first + 1) + "-" + k) + " "
                    + kinds[first] + (startNumber > 0 ? " from " + startNumber : ""));
                first = k;
            }

            // Version 4: bold, italic and underline, paragraph by paragraph -
            // "bold 1; partly bold 3; underline 5-6". A paragraph is all one
            // way, partly, or not at all, as Revit's FormatStatus says.
            at = 0;
            var all = new List<int>[3];
            var part = new List<int>[3];
            for (int s = 0; s < 3; s++) { all[s] = new List<int>(); part[s] = new List<int>(); }
            for (int k = 0; k < pieces.Length; k++)
            {
                if (pieces[k].Length > 0)
                {
                    var chars = new TextRange(at, pieces[k].Length);
                    for (int s = 0; s < 3; s++)
                    {
                        var status = FormatStatus.None;
                        try
                        {
                            status = s == 0 ? shape.GetBoldStatus(chars)
                                : s == 1 ? shape.GetItalicStatus(chars) : shape.GetUnderlineStatus(chars);
                        }
                        catch { }
                        if (status == FormatStatus.All) all[s].Add(k + 1);
                        else if (status == FormatStatus.Mixed) part[s].Add(k + 1);
                    }
                }
                at += pieces[k].Length + 1;
            }
            Func<List<int>, string> runsOf = numbers =>
            {
                var said = new List<string>();
                for (int a = 0; a < numbers.Count; a++)
                {
                    int b = a;
                    while (b + 1 < numbers.Count && numbers[b + 1] == numbers[b] + 1) b++;
                    said.Add(b == a ? numbers[a].ToString() : numbers[a] + "-" + numbers[b]);
                    a = b;
                }
                return string.Join(", ", said);
            };
            string[] styleWords = { "bold", "italic", "underline" };
            for (int s = 0; s < 3; s++)
            {
                if (all[s].Count > 0) styles.Add(styleWords[s] + " " + runsOf(all[s]));
                if (part[s].Count > 0) styles.Add("partly " + styleWords[s] + " " + runsOf(part[s]));
            }
        }

        var unusual = new List<string>();
        foreach (var ch in wording)
        {
            if (ch <= 126 || ch == '\v') continue;
            var named = string.Format("{0} (U+{1:X4})", ch, (int)ch);
            if (!unusual.Contains(named)) unusual.Add(named);
        }

        // A note in no list at all - most of them - says so in two words.
        var listed = lists.Count == 1 && lists[0].EndsWith(" no list") ? "no list"
            : "lists: " + string.Join(", ", lists);
        var line = string.Format("note {0}, {1} paragraph(s): {2} || {3}{4}{5}", e.Id, pieces.Length,
            wording.Replace("\\", "\\\\").Replace("\r", "\\r").Replace("\v", "\\v"),
            listed,
            styles.Count > 0 ? " || " + string.Join("; ", styles) : "",
            unusual.Count > 0 ? " || not typed plainly: " + string.Join(", ", unusual) : "");
        noteWording = noteWording == null ? line : noteWording + "\n" + line;
        continue;
    }

    if (askingForLevel)
    {
        // Element.LevelId is the only thing that answers for a wall, and it is
        // invalid on an MEP curve - so neither route alone is enough.
        ElementId levelId = ElementId.InvalidElementId;
        try { levelId = e.LevelId; } catch { }

        if (levelId == ElementId.InvalidElementId)
        {
            Parameter lp = null;
            try
            {
                lp = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                     ?? e.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM)
                     ?? e.get_Parameter(BuiltInParameter.LEVEL_PARAM)
                     ?? e.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)
                     ?? e.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
            }
            catch { }
            if (lp != null)
            {
                try { levelId = lp.AsElementId(); } catch { }
            }
        }

        if (levelId == null || levelId == ElementId.InvalidElementId)
        {
            // Genuinely not on a level - a view-specific annotation, a group, a
            // level itself. That is `absent`, and it is a real answer.
            absent.Add(e.Id);
            continue;
        }

        var levelElement = e.Document.GetElement(levelId);
        var levelName = levelElement == null ? null : levelElement.Name;
        if (string.IsNullOrWhiteSpace(levelName)) blank.Add(e.Id);
        else
        {
            values[e.Id] = levelName;
            resolvedFrom[e.Id] = "level";
        }
        continue;
    }

    var p = e.LookupParameter(parameterName);
    string from = "instance";

    if (p == null)
    {
        // The type is asked SECOND, never first: an instance parameter of the
        // same name must win, because that is the one the modeller edited.
        Element typeElement = null;
        try { typeElement = e.Document.GetElement(e.GetTypeId()); } catch { }
        if (typeElement != null)
        {
            p = typeElement.LookupParameter(parameterName);
            from = "type";
        }
    }

    if (p == null)
    {
        absent.Add(e.Id);
        continue;
    }

    var text = readValue(p);
    if (string.IsNullOrWhiteSpace(text)) blank.Add(e.Id);
    else
    {
        values[e.Id] = text;
        resolvedFrom[e.Id] = from;
    }
}

// Past 20,000 characters the remaining notes are counted, not printed - their
// wording is still in `values`. A note is never cut in half.
if (noteWording != null && noteWording.Length > 20000)
{
    var every = noteWording.Split('\n');
    var printed = new List<string>();
    int size = 0;
    foreach (var one in every)
    {
        if (printed.Count > 0 && size + one.Length > 20000) break;
        printed.Add(one);
        size += one.Length + 1;
    }
    noteWording = string.Join("\n", printed) + (printed.Count < every.Length
        ? string.Format("\n... and {0} more text note(s) not printed here - read fewer at a time",
            every.Length - printed.Count)
        : "");
}

// ----------------------------------------------------------------------
// VERSION 5 - THE ANSWER IN WORDS, SO IT SURVIVES ANY FORMATTER
// ----------------------------------------------------------------------
//
// `values` and `resolvedFrom` are dictionaries, and a dictionary reached a
// chat as its count alone: on 2026-10-22 (Revit 2020, session 78736) one
// parameter read on four connectors answered "values 4 entry(ies)" and not
// one value (FRAGMENT-ISSUES 5b-1325). The add-in now sends a dictionary's
// first entries beside its count; this line is the same answer in the
// fragment's own words, so it holds whatever add-in or formatter carries it:
// each element's id, its value and where the value came from, the first
// twelve, then how many more; then the blank and the absent ones by id. A
// value is cut at 60 characters, as a list item is - a text note's wording is
// whole in `noteWording`.
var findings = "";
{
    var asked = (parameterName ?? "").Trim();
    var shownAtMost = 12;
    Func<string, string> cut = text => text == null ? "" : text.Length > 60 ? text.Substring(0, 60) + "..." : text;
    Func<IList<ElementId>, string> ids = list =>
        string.Join(", ", list.Take(shownAtMost).Select(id => id.ToString()))
        + (list.Count > shownAtMost ? " ... and " + (list.Count - shownAtMost) + " more" : "");

    var said = new List<string>();
    foreach (var pair in values.Take(shownAtMost))
    {
        string from;
        said.Add(pair.Key + ": " + cut(pair.Value)
            + (resolvedFrom.TryGetValue(pair.Key, out from) ? " (" + from + ")" : ""));
    }
    var line = "\"" + asked + "\": " + values.Count + " of " + elements.Count + " carry a value, "
        + blank.Count + " blank, " + absent.Count + " absent.";
    if (said.Count > 0)
        line += " " + string.Join("; ", said)
            + (values.Count > shownAtMost
                ? " ... and " + (values.Count - shownAtMost) + " more - read fewer at a time, or GROUP_AND_COUNT "
                  + "with expect_from read-element-parameters."
                : ".");
    if (blank.Count > 0) line += " Blank: " + ids(blank) + ".";
    if (absent.Count > 0) line += " Absent: " + ids(absent) + ".";
    findings = line;
}
