// NOT STANDALONE. Assumes `doc`, `elements`, `field`, `mode`, `text` and
// `replacement` are in scope, and leaves `changed`, `untouched`, `blank`,
// `readOnly`, `absent`, `ambiguous`, `unverified`, `refused`,
// `noteFormatting`, `typesEdited`, `collisions`, `notMatched` and
// `familyRename` behind.
//
// VERSION 8 (2026-11-01) - RENAME_FAMILY MERGED IN: field "Family" renames the
// LOADED FAMILY behind what is handed in - a family, one of its types or an
// instance - one level above the type name field Name edits. Its rules come
// with it: more than one family is refused (family names are unique, so one
// name over three takes on the first and quietly not the rest), an in-place
// family is refused, a system family has none and lands in `absent`, and the
// types that follow are counted and named. Mode "set" - the new name whole -
// is allowed for this field and field Text only; prefix, suffix (never
// stacked) and replace work too, and the batch collision check of version 7
// runs against every other family's name first, so a name already taken is
// refused before Revit is asked. `familyRename` says what changed and that the
// .rfa on disk keeps its own name.
//
// VERSION 7 (2026-11-01) - RENAME_ELEMENTS MERGED IN: for field Name every new
// name is worked out first and checked against the others and against the
// names already in the model; one clash and NOTHING is renamed, `collisions`
// says which. `notMatched` names what a replace found nothing to replace in.
// An unreadable name is refused in `readOnly`, never edited as blank.
//
// VERSION 6 (2026-11-01) - EDIT_PARAMETER_TEXT MERGED IN, AND ITS THREE RULES
// WITH IT: a prefix or suffix already there is not added again, so a re-run
// never stacks; a type's parameter is written ONCE however many of its
// instances are handed in, and the types changed are named in `typesEdited`
// because every other instance of them changed too; and a non-text instance
// parameter gives way to a text one of the same name on the type.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one - Golden Rule 16 wants
// one user action to be one undo entry, and that belongs to the operation's
// TransactionGroup.
//
// READ, TRANSFORM, WRITE - WHICH IS WHY THIS IS NOT WRITE_ELEMENT_PARAMETERS.
//
// That one sets a LITERAL. "Put AJ- in front of every mark" cannot be done that
// way, because the value is different on every element.
//
// THE ELEMENT'S NAME IS JUST ANOTHER FIELD - AND SO IS A TEXT NOTE'S WORDING.
//
// `field` of "Name" edits Element.Name; `field` of "Text" on a text note edits
// the note's own wording, which is no parameter at all (version 3, 2026-10-12,
// the gap fill; written through its FormattedText since version 4, below);
// anything else is a parameter. The
// earlier library had a rename fragment and a prefix/suffix fragment carrying
// the same four transformations twice - so a fix to the replace rule could
// reach one and miss the other.
//
// TEXT ONLY, ON PURPOSE.
//
// A number is REFUSED rather than edited through its display text. That round
// trip is lossy: a length displayed as 2500 is not exactly 2500 internally, and
// writing the display back rewrites a rounded value as the truth.
//
// A BLANK VALUE IS LEFT ALONE. A prefix on an empty parameter creates a value
// where the modeller had none - a different edit from the one asked for, and it
// would look like data afterwards.
//
// ===========================================================================
// AN EDIT IT CANNOT MAKE IS REFUSED. IT NEVER COMES BACK AS "NOTHING CHANGED".
// ===========================================================================
//
// Found in front of a model on 2026-09-09: `field=text` against seven text
// notes reported all seven `absent` and changed nothing. That reads as "I
// looked and there was nothing to do", and what actually happened was "I could
// not see what you gave me" - the field is called Text, and a mistyped name
// answered exactly like a correct one on a set with nothing to change.
//
// Three inputs can each make the whole request unworkable, and each is now
// checked BEFORE the first element is touched:
//
//   field   nothing to look for
//   mode    not prefix, suffix or replace - `transform` would hand every value
//           straight back and every element would land in `untouched`
//   text    prefix and suffix would add nothing, replace would search for
//           nothing
//
// Two more can only be known after LOOKING, so they are decided after the
// loop - which is safe precisely because both are paths on which nothing was
// written:
//
//   the field is on NOT ONE element handed in - a spelling mistake
//   the field is not TEXT on any of them - a number, and this fragment will
//   not edit a number through its display text
//
// Where a refusal is decided after looking, the list the unusable input filled
// is CLEARED. `absent: 7` on a run that did nothing reads as a finding, and it
// is not one - the count is in the refusal, which is where it belongs.
//
// ===========================================================================
// VERSION 4 - A TEXT NOTE IS EDITED THROUGH ITS FORMATTED TEXT, SO ITS
// NUMBERING SURVIVES
// ===========================================================================
//
// Version 3 wrote a note's wording with `note.Text = wanted`, and Autodesk's own
// reference says what that costs: "all previous formating ... will be lost".
// MEASURED 2026-10-17, Revit 2020, rolled back: on a real project's note whose
// 8 paragraphs were a 1-2-3 list, a one-word replace came back with list type
// None on EVERY paragraph; on a scratch copy of the rst_basic sample the same
// write also turned one bold word into a whole bold note and dropped an
// underlined and an italic paragraph. So every mode on a text note now goes
// through note.GetFormattedText(), an edit of that copy, and SetFormattedText:
//
//   replace   SetPlainText on each matched range, last match first so the
//             earlier positions do not move. MEASURED: list type, indent, list
//             start number, bold, italic, underline and all caps all kept; the
//             new words take the formatting of the first character they
//             replace (bold "DETAIL A" -> bold "DETAIL B"). A match that runs
//             over a paragraph end joins the two paragraphs, which keep the
//             first one's list
//   prefix    inserted at 0 - it takes the first character's formatting
//   suffix    inserted before the closing carriage return - it takes the last
//             character's
//   set       SetPlainText over the whole wording, then EACH NEW PARAGRAPH IS
//             GIVEN BACK the formatting of the old paragraph in the same place,
//             and every extra new paragraph the old LAST one's: list type,
//             indent, and bold/italic/underline/all caps where the old paragraph
//             was all one way. MEASURED: SetPlainText alone kept the list type
//             but put every paragraph at indent 0 and spread the first word's
//             bold over the whole note, so the rule is applied rather than left
//             to Revit. A paragraph that was PARTLY bold (or italic, ...) cannot
//             say which new word should be, so it is written plain and the
//             reply says so. The first paragraph keeps the list's start number.
//
// A PARAGRAPH BREAK IS TYPED `\r` AND A SOFT LINE BREAK `\v`, for a text note
// only, and a backslash itself `\\` - the marks READ_ELEMENT_PARAMETERS shows a
// note's wording in. A chat's values arrive one per line, so a real paragraph
// break cannot be typed at all; without these a whole new wording could only
// ever be one paragraph. Anything else after a backslash is left as typed.
//
// IT READS THE FORMATTING BACK. After the write, each paragraph's list type
// and indent (and the start number) are read again and compared with what the
// rule above meant them to be; a note where any differs goes in `unverified`,
// never `changed`, and `noteFormatting` names the paragraph.
//
// ===========================================================================
// VERSION 5 - MODE "format" SETS A NOTE'S LISTS AND STYLES, AND LEAVES ITS
// WORDS ALONE
// ===========================================================================
//
// Version 4 kept formatting that was already there; nothing could SET it - put
// numbers on a note that has none, turn bullets into numbers or letters, start
// a list at 3, indent a paragraph, make one bold (2026-10-17, FRAGMENT-ISSUES
// 5b-737). Mode "format" does, on field Text only, through the same
// GetFormattedText -> edit -> SetFormattedText, and never changes a character.
//
//   text          what to set, several joined with ';', each followed by the
//                 paragraphs it is for: "numbers 2-4; bold 1". The words:
//                 numbers, bullets, lowercase letters, uppercase letters, no
//                 list; start N; indent N, no indent; bold, italic, underline,
//                 and not bold, not italic, not underlined
//   replacement   the paragraphs for any instruction that names none - "2-11",
//                 "1, 3", "first", "last", "all". Empty means every paragraph
//
// A PARAGRAPH IS WHAT ENTER ENDS. A line Revit wraps by itself is not one, and
// a Shift+Enter break stays inside its paragraph - so "the first line" is
// paragraph 1. Each instruction is applied to whole paragraphs, closing
// carriage return included, which is where Autodesk's reference says a list's
// own number takes its bold from.
//
// CHECKED BEFORE ANYTHING IS APPLIED, per note: a paragraph past the note's
// last one, an indent past Revit's maximum, a start number outside Revit's
// range or on a paragraph that is not numbered or lettered. Any of them and
// that note is not written at all - `absent` for a missing paragraph, `readOnly`
// for the rest - because the edits are made on a copy and only SetFormattedText
// reaches the note.
//
// REVIT CHANGES A WHOLE LIST AT ONCE. Its reference: consecutive list
// paragraphs at one indent are one list, and changing one's list type changes
// them all, even outside the range. So every paragraph is read back, NAMED OR
// NOT, and one that reads back other than meant - a bulleted paragraph 1
// renumbered with the 2-4 it is listed with - puts the note in `unverified`
// with the paragraph said. Every FormattedText member used here reads the same
// in RevitAPI for each release 2020 to 2027, so there is no #if.

var changed = new List<ElementId>();
var untouched = new List<ElementId>();
var blank = new List<ElementId>();
var readOnly = new List<ElementId>();
var absent = new List<ElementId>();
var ambiguous = new List<ElementId>();
var unverified = new List<ElementId>();
string refused = null;
// One line per text note edited: what happened to its paragraphs' list,
// indent and text style, read back after the write. A string, because a list
// in the reply is cut to three items of 60 characters.
string noteFormatting = null;

bool editingName = string.Equals((field ?? "").Trim(), "Name",
                                 StringComparison.OrdinalIgnoreCase);
// VERSION 8 - the loaded family itself (RENAME_FAMILY, merged in). Before
// version 8 "Family" was looked up as a parameter, and the built-in one of that
// name holds an element id, so it was refused as not text: nothing that worked
// stops working.
bool editingFamily = string.Equals((field ?? "").Trim(), "Family",
                                   StringComparison.OrdinalIgnoreCase);
// One line about the family renamed, or why it was not (version 8).
string familyRename = null;
// A text note's wording. Decided per element below, because a set handed in
// can mix text notes with other elements; on anything that is not a note,
// "Text" is looked up as a parameter like any other name.
bool askingText = string.Equals((field ?? "").Trim(), "Text",
                                StringComparison.OrdinalIgnoreCase);
// Revit hands a note's text back with a closing carriage return; compared
// without it, so a read-back is not called altered for a character nobody typed.
Func<string, string> noteText = raw => (raw ?? "").TrimEnd('\r', '\n');
string how = (mode ?? "").Trim().ToLowerInvariant();

// DECIDED BEFORE ANY ELEMENT IS READ, let alone written. Each of these three
// makes every element answer the same way, which is why the failure looked
// like a fragment correctly finding nothing.
if (string.IsNullOrEmpty((field ?? "").Trim()))
{
    refused = "no field was given, so there is nothing to edit. Name the parameter to change, "
        + "or 'Name' for the element's own name. NOTHING WAS CHANGED";
}
else if (how.Length == 0)
{
    refused = "no mode was given, so there is nothing to do. Use prefix, suffix or replace. "
        + "NOTHING WAS CHANGED";
}
else if (how == "set" && !askingText && !editingFamily)
{
    refused = "'set' writes the whole wording of a text note, or a family's whole new name, and is "
        + "for field Text and field Family only. To set one value on a parameter, that is "
        + "WRITE_ELEMENT_PARAMETERS. NOTHING WAS CHANGED";
}
else if (how == "format" && !askingText)
{
    refused = "'format' sets a text note's numbering, bullets, indents and bold, italic or "
        + "underline, and is for field Text only. NOTHING WAS CHANGED";
}
else if (how != "prefix" && how != "suffix" && how != "replace" && how != "set" && how != "format")
{
    refused = string.Format(
        "'{0}' is not an edit this fragment makes, so NOTHING WAS CHANGED. It makes three: "
        + "prefix (put text in front), suffix (put text after) and replace (swap one piece of "
        + "text for another) - and, for a text note, set (its whole wording) and format (its "
        + "lists and styles). Carrying on would hand "
        + "every value straight back and report them all untouched, which reads exactly like an "
        + "edit that had nothing to do", mode);
}
else if (string.IsNullOrEmpty(text))
{
    refused = string.Format(
        "no text was given, so a '{0}' has nothing to work with and NOTHING WAS CHANGED. For "
        + "prefix and suffix this is the text to add; for replace it is the text to look for; "
        + "for format it is what to set, e.g. 'numbers 2-4; bold 1'. "
        + "To DELETE a piece of text, give it as the text and leave the replacement empty",
        how);
}

// MODE "format" - version 5. What `text` asks for is read here, BEFORE any
// element is, so a word it does not know stops the run with nothing touched.
// Each instruction is {what, value} with its paragraphs: an empty list of
// spans is every paragraph, null is "none named, use replacement's". A span is
// {from, to}, 1-based; -1 is the last paragraph, whatever the note's count.
var stepKinds = new List<int[]>();       // {0 list | 1 start | 2 indent | 3 bold | 4 italic | 5 underline, value}
var stepSpans = new List<List<int[]>>();
var stepSaid = new List<string>();
List<int[]> defaultSpans = new List<int[]>();

// "2-11", "1, 3", "2 to last", "first", "all" -> spans; null when it is not one.
Func<string, List<int[]>> spansOf = said =>
{
    var flat = System.Text.RegularExpressions.Regex.Replace(" " + (said ?? "").ToLowerInvariant() + " ",
        @"(\d+|first|last)\s*(?:-|to)\s*(\d+|last)", "$1-$2");
    flat = System.Text.RegularExpressions.Regex.Replace(flat,
        @"\b(on|for|to|in|of|the|paragraphs?|paras?|lines?|only|every|whole|note)\b", " ");
    flat = System.Text.RegularExpressions.Regex.Replace(flat, @"\band\b", ",").Trim().Trim(',').Trim();
    var spans = new List<int[]>();
    if (flat.Length == 0 || flat == "all") return spans;
    foreach (var part in flat.Split(','))
    {
        var piece = part.Trim();
        if (piece.Length == 0) continue;
        var ends = piece.Split('-');
        if (ends.Length > 2) return null;
        var pair = new int[2];
        for (int e = 0; e < ends.Length; e++)
        {
            var end = ends[e].Trim();
            int n;
            if (end == "first") n = 1;
            else if (end == "last") n = -1;
            else if (!int.TryParse(end, out n) || n < 1) return null;
            pair[e] = n;
        }
        if (ends.Length == 1) pair[1] = pair[0];
        if (pair[0] == -1 && pair[1] != -1) return null;
        if (pair[1] != -1 && pair[0] > pair[1]) return null;
        spans.Add(pair);
    }
    return spans;
};

if (refused == null && how == "format")
{
    // {words, kind, value}, longest first so "not bold" is never read as
    // "bold". A list's value is Revit's own ListType, by name.
    string arabic = ((int)ListType.ArabicNumbers).ToString(), bullet = ((int)ListType.Bullet).ToString(),
        lower = ((int)ListType.LowerCaseLetters).ToString(), upper = ((int)ListType.UpperCaseLetters).ToString(),
        noList = ((int)ListType.None).ToString();
    var words = new List<string[]>
    {
        new[] { "numbers", "0", arabic }, new[] { "numbered list", "0", arabic }, new[] { "numbered", "0", arabic },
        new[] { "numbering", "0", arabic }, new[] { "number", "0", arabic }, new[] { "arabic numbers", "0", arabic },
        new[] { "bullets", "0", bullet }, new[] { "bulleted", "0", bullet }, new[] { "bullet points", "0", bullet },
        new[] { "bullet", "0", bullet },
        new[] { "lowercase letters", "0", lower }, new[] { "lower case letters", "0", lower },
        new[] { "small letters", "0", lower },
        new[] { "uppercase letters", "0", upper }, new[] { "upper case letters", "0", upper },
        new[] { "capital letters", "0", upper },
        new[] { "no list", "0", noList }, new[] { "not a list", "0", noList }, new[] { "no numbering", "0", noList },
        new[] { "no numbers", "0", noList }, new[] { "no bullets", "0", noList }, new[] { "none", "0", noList },
        new[] { "remove list", "0", noList }, new[] { "remove numbering", "0", noList },
        new[] { "remove bullets", "0", noList }, new[] { "clear list", "0", noList },
        new[] { "no indent", "2", "0" }, new[] { "not indented", "2", "0" }, new[] { "remove indent", "2", "0" },
        new[] { "bold", "3", "1" }, new[] { "not bold", "3", "0" }, new[] { "no bold", "3", "0" },
        new[] { "remove bold", "3", "0" },
        new[] { "italic", "4", "1" }, new[] { "italics", "4", "1" }, new[] { "not italic", "4", "0" },
        new[] { "no italic", "4", "0" }, new[] { "no italics", "4", "0" }, new[] { "remove italic", "4", "0" },
        new[] { "underline", "5", "1" }, new[] { "underlined", "5", "1" }, new[] { "not underlined", "5", "0" },
        new[] { "no underline", "5", "0" }, new[] { "remove underline", "5", "0" },
    };
    words.Sort((a, b) => b[0].Length.CompareTo(a[0].Length));

    string unknown = null, badRange = null;
    foreach (var piece in text.Split(';'))
    {
        var said = piece.Trim();
        if (said.Length == 0) continue;
        var low = System.Text.RegularExpressions.Regex.Replace(said.ToLowerInvariant(), @"\s+", " ");
        int kind = -1, value = 0;
        string rest = null;
        var numbered = System.Text.RegularExpressions.Regex.Match(low,
            @"^(?:list )?(start(?:s|ing)?|indent)(?: level)?(?: (?:at|from|with|to))? (\d+)\b(.*)$");
        if (numbered.Success)
        {
            kind = numbered.Groups[1].Value.StartsWith("indent") ? 2 : 1;
            value = int.Parse(numbered.Groups[2].Value);
            rest = numbered.Groups[3].Value;
        }
        else
        {
            foreach (var word in words)
            {
                if (low != word[0] && !low.StartsWith(word[0] + " ")) continue;
                kind = int.Parse(word[1]);
                value = int.Parse(word[2]);
                rest = low.Substring(word[0].Length);
                break;
            }
        }
        var spans = rest == null ? null : (rest.Trim().Length == 0 ? null : spansOf(rest));
        if (kind < 0) { unknown = said; break; }
        if (rest.Trim().Length > 0 && spans == null) { unknown = said; badRange = rest.Trim(); break; }
        stepKinds.Add(new[] { kind, value });
        stepSpans.Add(spans);
        stepSaid.Add(said);
    }

    defaultSpans = spansOf(replacement);
    if (badRange != null)
    {
        refused = string.Format(
            "in '{0}', '{1}' is not a set of paragraphs, so NOTHING WAS CHANGED. Paragraphs are "
            + "counted from 1 at the top: '2-4', '1, 3', '2 to last', 'first', 'last' or 'all'",
            unknown, badRange);
    }
    else if (unknown != null || stepKinds.Count == 0)
    {
        refused = string.Format(
            "'{0}' is not formatting this mode sets, so NOTHING WAS CHANGED. It sets numbers, "
            + "bullets, lowercase letters, uppercase letters or no list; start N (the list's first "
            + "number or letter); indent N or no indent; bold, italic, underline, and not bold, not "
            + "italic, not underlined - each followed by the paragraphs it is for: '2-4', '1, 3', "
            + "'first', 'last' or 'all'. Several are joined with ';', e.g. 'numbers 2-4; bold 1'. "
            + "One that names no paragraphs takes the replacement's, and an empty replacement means "
            + "every paragraph", unknown ?? text);
    }
    else if (defaultSpans == null)
    {
        refused = string.Format(
            "'{0}' is not a set of paragraphs, so NOTHING WAS CHANGED. For format the replacement "
            + "names the paragraphs an instruction without its own is for: '2-11', '1, 3', 'first', "
            + "'last' or 'all', or empty for every paragraph", replacement);
    }
}

var handed = 0;
var notText = 0;
// VERSION 6 - ONE WRITE PER TYPE. A type's parameter is edited once however many
// of its instances are handed in; each later instance lands where the first did.
// `typesEdited` names the types whose value changed, because every instance of
// them changed too - including the ones nobody handed in.
var typesEdited = new List<ElementId>();
var typeLanded = new Dictionary<ElementId, List<ElementId>>();
var typeNotText = new HashSet<ElementId>();
// Text notes a format left alone because a paragraph it named is not there.
var pastEnd = 0;

// The names that ARE there, so the next question is already answered - the
// same courtesy ADD_SCHEDULE_FIELDS pays with `availableFields`. Read off the
// FIRST element only: on a mixed selection the full list is long and no more
// useful than one real example, and this is a sentence a person reads.
//
// IT LISTS THE TYPE'S PARAMETERS AS WELL AS THE INSTANCE'S, because the loop
// below LOOKS AT BOTH - instance first, then the type. Listing only the
// instance would print a list that omits the very name that would have worked,
// and send somebody to check the spelling of a parameter that was never
// missing. A hint narrower than the search it explains is worse than no hint:
// it is evidence for a wrong conclusion.
Func<IList<Element>, string> namesOnHand = given =>
{
    var available = new List<string>();
    Action<Element> collect = source =>
    {
        if (source == null) return;
        try
        {
            foreach (Parameter candidate in source.Parameters)
            {
                var definition = candidate.Definition;
                if (definition == null) continue;
                var name = definition.Name ?? "";
                if (name.Length > 0 && !available.Contains(name)) available.Add(name);
            }
        }
        catch { }
    };

    foreach (var element in given)
    {
        if (element == null) continue;
        collect(element);

        // Guarded separately: an element with no type, or one whose type will
        // not answer, should cost the type half of the list and not the hint.
        Element elementType = null;
        try { elementType = doc.GetElement(element.GetTypeId()); }
        catch { elementType = null; }
        collect(elementType);
        break;
    }
    available.Sort(StringComparer.OrdinalIgnoreCase);

    if (available.Count == 0)
        return "The first element and its type report no parameters at all";
    var shown = available.Count > 12 ? available.GetRange(0, 12) : available;
    return "The first element and its type carry: " + string.Join(", ", shown)
        + (available.Count > shown.Count
            ? string.Format(", and {0} more", available.Count - shown.Count)
            : "");
};

Func<string, string> transform = current =>
{
    // VERSION 6 - A PREFIX OR SUFFIX ALREADY THERE IS NOT ADDED AGAIN, so a
    // re-run leaves AJ_M-01 and never AJ_AJ_M-01 (EDIT_PARAMETER_TEXT's rule,
    // merged in). Batch jobs get re-run - after an undo, after a filter is
    // widened, after somebody is not sure it took.
    if (how == "prefix")
        return current.StartsWith(text, StringComparison.OrdinalIgnoreCase) ? current : text + current;
    if (how == "suffix")
        return current.EndsWith(text, StringComparison.OrdinalIgnoreCase) ? current : current + text;
    if (how == "set") return text;
    if (how == "replace")
    {
        if (string.IsNullOrEmpty(text)) return current;
        return current.Replace(text, replacement ?? "");
    }
    return current;
};

// ===========================================================================
// VERSION 7 (2026-11-01) - RENAME_ELEMENTS MERGED IN: A NAME EDIT IS PLANNED
// FOR THE WHOLE BATCH, AND ONE CLASH STOPS ALL OF IT
// ===========================================================================
//
// RENAME_ELEMENTS worked every intended name out before writing one, and
// stopped the whole batch on two names landing as one: Revit throws on a
// duplicate type or view name and quietly accepts a duplicate instance name,
// and neither is wanted half way through forty renames. Version 6 wrote names
// one at a time, so a clash at element 31 left 30 renamed and the rest not -
// two conventions in one model and no record of where it stopped.
//
// So for field Name, BEFORE ANY ELEMENT IS WRITTEN:
//
//   1. every element's new name is worked out with the same `transform` the
//      write uses, so the plan and the write cannot disagree
//   2. the new names are checked AGAINST EACH OTHER - two the same, whatever
//      kind of element, is a clash (RENAME_ELEMENTS' rule)
//   3. each is checked AGAINST THE MODEL - a name already held by another
//      element of the same kind: any view or view template for a view, the
//      same family for a loadable family's type, the same class and category
//      for anything else. Capitals are ignored. An element ALSO in the batch
//      still holds its old name when the first write lands, so a swap (A to
//      B and B to A) is a clash too - do it in two runs, through a name
//      nobody holds
//   4. a new name left EMPTY by a replace is a clash - Revit refuses it
//
// Any one of them and NOTHING IS WRITTEN: `collisions` names each clash and
// `refused` says why. All or nothing is the safer of the two cards' rules.
//
// `notMatched` (RENAME_ELEMENTS' output) is filled after the loop: for mode
// replace, the elements whose text did not contain the words to replace - on
// any field, not only Name. It is `untouched` for a replace, said in
// RENAME_ELEMENTS' word, and the honest clue that the search text was wrong.
var collisions = new List<string>();
var notMatched = new List<ElementId>();

// ===========================================================================
// VERSION 8 (2026-11-01) - FIELD "Family": THE LOADED FAMILY, RENAMED ONCE
// ===========================================================================
//
// RENAME_FAMILY's job, with its rules. Renaming a family changes it
// EVERYWHERE: every type in it and every instance of those types follow,
// across the model, whether or not they were handed in - so the types are
// counted and named in `typesEdited`, and `familyRename` says so. MORE THAN
// ONE FAMILY IS A REFUSAL, not a loop: family names must be unique, so one
// literal name over three takes on the first and quietly not on the rest. AN
// IN-PLACE FAMILY IS REFUSED: it is tied to the element that made it. THE FILE
// ON DISK IS UNTOUCHED - reloading the .rfa brings the old name back.
//
// AND VERSION 7'S COLLISION CHECK RUNS FIRST: the new name against every other
// family in the model, capitals ignored, and an empty one refused - so a name
// already taken is refused before Revit is asked, never left to Revit's own
// throw (RENAME_FAMILY reported Revit's refusal after the attempt).
if (refused == null && editingFamily)
{
    var families = new List<Family>();
    foreach (var element in elements)
    {
        if (element == null) continue;
        // Handed the family itself, one of its types, or an instance of one.
        Family family = element as Family;
        if (family == null)
        {
            var symbol = element as FamilySymbol;
            if (symbol != null) family = symbol.Family;
        }
        if (family == null)
        {
            var instance = element as FamilyInstance;
            if (instance != null && instance.Symbol != null) family = instance.Symbol.Family;
        }
        if (family == null) { absent.Add(element.Id); continue; }
        if (!families.Any(known => known.Id == family.Id)) families.Add(family);
    }

    if (families.Count == 0)
    {
        refused = string.Format("nothing handed in belongs to a loaded family, so NOTHING WAS RENAMED - "
            + "system families such as walls, ducts and pipes have no family to rename; their TYPE names "
            + "are field Name's. {0} element(s) looked at", absent.Count);
        absent.Clear();
    }
    else if (families.Count > 1)
    {
        refused = string.Format(
            "what was handed in covers {0} different families, and one name cannot serve them all - Revit "
            + "needs family names to be unique, so NOTHING WAS RENAMED. They are: {1}",
            families.Count, string.Join(", ", families.Select(f => "'" + f.Name + "'").ToArray()));
    }
    else if (families[0].IsInPlace)
    {
        refused = string.Format("'{0}' is an in-place family - it is tied to the element that made it, and "
            + "renaming it does not mean what it looks like it means. NOTHING WAS RENAMED", families[0].Name);
    }
    else
    {
        var family = families[0];
        var wasCalled = family.Name ?? "";
        var wanted = transform(wasCalled);

        if (wanted == wasCalled)
        {
            untouched.Add(family.Id);
            familyRename = string.Format("'{0}' already reads that way - nothing was renamed", wasCalled);
        }
        else
        {
            if (wanted.Trim().Length == 0)
                collisions.Add(string.Format("'{0}' (id {1}) would be left with NO name", wasCalled, family.Id));
            else
            {
                foreach (var other in new FilteredElementCollector(doc).OfClass(typeof(Family)).ToElements())
                {
                    if (other.Id == family.Id) continue;
                    string held = null;
                    try { held = other.Name; } catch { held = null; }
                    if (string.Equals(held, wanted.Trim(), StringComparison.OrdinalIgnoreCase))
                        collisions.Add(string.Format("'{0}' - planned for family id {1}, and already the name "
                            + "of family id {2}", wanted.Trim(), family.Id, other.Id));
                }
            }

            if (collisions.Count > 0)
            {
                refused = string.Format("the new family name clashes, so NOTHING WAS RENAMED: {0}. Two "
                    + "families cannot share a name, and inventing a suffix would leave a family nobody can "
                    + "find by the name they asked for", string.Join("; ", collisions.ToArray()));
            }
            else
            {
                var typeIds = new List<ElementId>();
                try { typeIds.AddRange(family.GetFamilySymbolIds()); } catch { }
                try
                {
                    family.Name = wanted.Trim();
                    // READ IT BACK. The name Revit settled on is the answer.
                    var nowCalled = (doc.GetElement(family.Id) as Family).Name;
                    if (nowCalled == wanted.Trim())
                    {
                        changed.Add(family.Id);
                        typesEdited.AddRange(typeIds);
                    }
                    else unverified.Add(family.Id);
                    familyRename = string.Format(
                        "'{0}' is now '{1}'. Its {2} type(s) and EVERY instance of them changed with it, "
                        + "across the whole model and not only what was handed in. The .rfa file on disk keeps "
                        + "its own name - reloading from it will bring '{0}' back",
                        wasCalled, nowCalled, typeIds.Count);
                }
                catch (Exception ex)
                {
                    // Refused by Revit - a question for the modeller, never
                    // worked round by inventing a name.
                    readOnly.Add(family.Id);
                    familyRename = string.Format("'{0}' could not be renamed to '{1}': {2}",
                        wasCalled, wanted.Trim(), ex.Message);
                }
            }
        }
    }
    if (familyRename == null && refused != null) familyRename = refused;
}

if (refused == null && editingName)
{
    var plannedName = new Dictionary<string, ElementId>(StringComparer.OrdinalIgnoreCase);
    // The kind of element a name must be unique within, and the elements
    // whose names are checked - kind -> (an example element, the batch's ids).
    var kinds = new Dictionary<string, Element>();
    var plannedByKind = new Dictionary<string, List<KeyValuePair<ElementId, string>>>();

    Func<Element, string> kindOf = e =>
    {
        if (e is View) return "view";
        var symbol = e as FamilySymbol;
        if (symbol != null && symbol.Family != null) return "family " + symbol.Family.Id;
        return e.GetType().FullName + " " + (e.Category == null ? "-" : e.Category.Id.ToString());
    };

    foreach (var element in elements)
    {
        if (element == null) continue;
        string was = null;
        try { was = element.Name; } catch { was = null; }
        if (string.IsNullOrEmpty(was)) continue;
        var wanted = transform(was);
        if (wanted == was) continue;

        if (wanted.Trim().Length == 0)
        {
            collisions.Add(string.Format("'{0}' (id {1}) would be left with NO name", was, element.Id));
            continue;
        }

        ElementId already;
        if (plannedName.TryGetValue(wanted, out already))
        {
            collisions.Add(string.Format("'{0}' - planned for both id {1} and id {2}",
                wanted, already, element.Id));
            continue;
        }
        plannedName[wanted] = element.Id;

        string kind;
        try { kind = kindOf(element); } catch { kind = element.GetType().FullName + " -"; }
        if (!kinds.ContainsKey(kind))
        {
            kinds[kind] = element;
            plannedByKind[kind] = new List<KeyValuePair<ElementId, string>>();
        }
        plannedByKind[kind].Add(new KeyValuePair<ElementId, string>(element.Id, wanted));
    }

    // AGAINST THE MODEL, one collector per kind of element in the batch.
    foreach (var entry in kinds)
    {
        var example = entry.Value;
        // Every holder of each name - two others may share one, and the
        // element being renamed must not hide the other.
        var inUse = new Dictionary<string, List<ElementId>>(StringComparer.OrdinalIgnoreCase);
        IEnumerable<Element> others = null;
        try
        {
            if (example is View)
                others = new FilteredElementCollector(doc).OfClass(typeof(View)).ToElements();
            else if (example is FamilySymbol && ((FamilySymbol)example).Family != null)
            {
                var familyId = ((FamilySymbol)example).Family.Id;
                others = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).ToElements()
                    .Where(o => { var s = o as FamilySymbol; return s != null && s.Family != null
                        && s.Family.Id == familyId; });
            }
            else
            {
                var cls = example.GetType();
                var catId = example.Category == null ? null : example.Category.Id;
                others = new FilteredElementCollector(doc).OfClass(cls).ToElements()
                    .Where(o => o.GetType() == cls
                        && (catId == null ? o.Category == null : (o.Category != null && o.Category.Id == catId)));
            }
        }
        catch
        {
            // OfClass refuses some classes (a room is one). The category is
            // asked instead; with neither, the names cannot be checked against
            // the model, and a rename that cannot be checked is not made.
            try
            {
                var cls = example.GetType();
                if (example.Category != null)
                    others = new FilteredElementCollector(doc).OfCategoryId(example.Category.Id)
                        .ToElements().Where(o => o.GetType() == cls);
            }
            catch { others = null; }
        }

        if (others == null)
        {
            collisions.Add(string.Format("the names already used by {0} elements could not be read, so "
                + "a new name for id {1} cannot be checked against them", example.GetType().Name,
                example.Id));
            continue;
        }

        foreach (var other in others)
        {
            string held = null;
            try { held = other.Name; } catch { held = null; }
            if (string.IsNullOrEmpty(held)) continue;
            if (!inUse.ContainsKey(held)) inUse[held] = new List<ElementId>();
            inUse[held].Add(other.Id);
        }

        foreach (var plan in plannedByKind[entry.Key])
        {
            List<ElementId> holders;
            if (!inUse.TryGetValue(plan.Value, out holders)) continue;
            var holder = holders.FirstOrDefault(h => h != plan.Key);
            if (holder != null)
                collisions.Add(string.Format("'{0}' - planned for id {1}, and already the name of id {2}",
                    plan.Value, plan.Key, holder));
        }
    }

    if (collisions.Count > 0)
    {
        refused = string.Format("{0} of the new names clash - with each other, with a name already in "
            + "the model, or by leaving a name empty - so NOTHING WAS RENAMED: {1}. Every name is "
            + "checked before the first is written, because Revit refuses a duplicate type or view "
            + "name and accepts a duplicate instance name, and a batch half renamed carries two "
            + "conventions", collisions.Count, string.Join("; ", collisions.Take(5).ToArray())
            + (collisions.Count > 5 ? string.Format("; and {0} more", collisions.Count - 5) : ""));
    }
}

foreach (var element in elements)
{
    // An unworkable request stops here rather than reporting on each element
    // in turn. Every one of them would answer identically anyway, and a list
    // of identical non-answers is what made this hard to see. Field Family
    // (version 8) was done whole, above.
    if (refused != null || editingFamily) break;
    if (element == null) continue;
    handed++;

    string current = null;
    Parameter parameter = null;
    var note = askingText ? element as TextNote : null;

    // Format is a text note's alone: anything else handed in has nothing it
    // could set, and is counted so the refusal after the loop can say so.
    if (how == "format" && note == null) { absent.Add(element.Id); continue; }

    // A TEXT NOTE'S WORDING, version 4: read, edited and written back through
    // its FormattedText, so its list, indent and text style survive - see the
    // header. Everything for a note happens in this block; the parameter and
    // Name paths below are version 3's, unchanged.
    if (note != null)
    {
        // `\r` a paragraph end, `\v` a soft line break, `\\` a backslash. A real
        // line feed can only mean a new paragraph: Revit allows none in a note.
        Func<string, string> typed = raw =>
        {
            var given = (raw ?? "").Replace("\r\n", "\r").Replace('\n', '\r');
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
            return built.ToString();
        };

        // Each paragraph of `words` as {start, length, list type, indent, bold,
        // italic, underline, all caps}; -1 where Revit gave no answer. A
        // paragraph's list and indent are read over it WITH its closing
        // carriage return, so an empty paragraph has a range too; a style needs
        // at least one character.
        Func<FormattedText, string, List<int[]>> paragraphsOf = (shape, words) =>
        {
            var found = new List<int[]>();
            int at = 0;
            foreach (var piece in words.Split('\r'))
            {
                var row = new[] { at, piece.Length, -1, -1, -1, -1, -1, -1 };
                var whole = new TextRange(at, piece.Length + 1);
                try { row[2] = (int)shape.GetListType(whole); } catch { }
                try { row[3] = shape.GetIndentLevel(whole); } catch { }
                if (piece.Length > 0)
                {
                    var chars = new TextRange(at, piece.Length);
                    try { row[4] = (int)shape.GetBoldStatus(chars); } catch { }
                    try { row[5] = (int)shape.GetItalicStatus(chars); } catch { }
                    try { row[6] = (int)shape.GetUnderlineStatus(chars); } catch { }
                    try { row[7] = (int)shape.GetAllCapsStatus(chars); } catch { }
                }
                found.Add(row);
                at += piece.Length + 1;
            }
            return found;
        };
        Func<int, string> listName = value =>
            value < 0 ? "unreadable" : value == (int)ListType.None ? "no list" : ((ListType)value).ToString();
        // "1-8 ArabicNumbers from 1" - runs of paragraphs with the same list and
        // indent, each run's start number as `startOf` gives it (-1 for none).
        Func<List<int[]>, Func<int, int>, string> listsOf = (rows, startOf) =>
        {
            var runs = new List<string>();
            int first = 0;
            for (int k = 1; k <= rows.Count; k++)
            {
                if (k < rows.Count && rows[k][2] == rows[first][2] && rows[k][3] == rows[first][3]) continue;
                var span = k - 1 == first ? (first + 1).ToString() : (first + 1) + "-" + k;
                var startNumber = startOf(first);
                runs.Add(span + " " + listName(rows[first][2])
                    + (rows[first][3] > 0 ? " indent " + rows[first][3] : "")
                    + (startNumber > 0 ? " from " + startNumber : ""));
                first = k;
            }
            return string.Join(", ", runs);
        };
        string[] styleNames = { "", "", "", "", "bold", "italic", "underline", "all caps" };
        // "1, 3-4" - paragraph numbers, 0-based in, 1-based out, runs joined.
        Func<List<int>, string> numbersOf = picked =>
        {
            var sorted = new List<int>(picked);
            sorted.Sort();
            var parts = new List<string>();
            for (int a = 0; a < sorted.Count; a++)
            {
                int b = a;
                while (b + 1 < sorted.Count && sorted[b + 1] == sorted[b] + 1) b++;
                parts.Add(b == a ? (sorted[a] + 1).ToString() : (sorted[a] + 1) + "-" + (sorted[b] + 1));
                a = b;
            }
            return string.Join(", ", parts);
        };
        // "bold 1; partly bold 3; underline 2-4" - or "no bold, italic or underline".
        Func<List<int[]>, string> stylesOf = rows =>
        {
            var styled = new List<string>();
            for (int s = 4; s < 7; s++)
            {
                var all = new List<int>();
                var part = new List<int>();
                for (int k = 0; k < rows.Count; k++)
                {
                    if (rows[k][s] == (int)FormatStatus.All) all.Add(k);
                    else if (rows[k][s] == (int)FormatStatus.Mixed) part.Add(k);
                }
                if (all.Count > 0) styled.Add(styleNames[s] + " " + numbersOf(all));
                if (part.Count > 0) styled.Add("partly " + styleNames[s] + " " + numbersOf(part));
            }
            return styled.Count == 0 ? "no bold, italic or underline" : string.Join("; ", styled);
        };
        // The number a list starts at, read over paragraph k; -1 for none.
        Func<FormattedText, List<int[]>, int, int> startAt = (shape, rows, k) =>
        {
            try { return shape.GetListStartNumber(new TextRange(rows[k][0], rows[k][1] + 1)); }
            catch { return -1; }
        };

        FormattedText shaped = null;
        string plain = null;
        try { shaped = note.GetFormattedText(); plain = shaped.GetPlainText(); }
        catch (Exception ex)
        {
            readOnly.Add(element.Id);
            noteFormatting = (noteFormatting == null ? "" : noteFormatting + " ## ")
                + string.Format("note {0}: Revit would not hand over its formatted text ({1}), so it "
                    + "was NOT edited", element.Id, ex.Message);
            continue;
        }

        var wording = noteText(plain);
        var find = typed(text);
        var putIn = typed(replacement);
        if (wording.Length == 0 && how != "set") { blank.Add(element.Id); continue; }

        // MODE "format" (version 5): the words stay exactly as they are; the
        // paragraphs' lists and styles change - see the header.
        if (how == "format")
        {
            var rows = paragraphsOf(shaped, wording);
            var asked = string.Format("'{0}'{1}", text,
                stepSpans.Exists(own => own == null) && defaultSpans.Count > 0
                    ? " on paragraphs " + replacement : "");
            Func<int, string> statusName = value => value == (int)FormatStatus.All ? "on"
                : value == (int)FormatStatus.None ? "off" : value == (int)FormatStatus.Mixed ? "partly" : "unreadable";

            // Which paragraphs each instruction is for, 0-based, checked against
            // THIS note before anything is applied.
            var stepParagraphs = new List<List<int>>();
            string missing = null;
            for (int s = 0; s < stepKinds.Count && missing == null; s++)
            {
                var spans = stepSpans[s] ?? defaultSpans;
                var picked = new List<int>();
                if (spans.Count == 0) for (int k = 0; k < rows.Count; k++) picked.Add(k);
                foreach (var span in spans)
                {
                    int lo = span[0] < 0 ? rows.Count : span[0];
                    int hi = span[1] < 0 ? rows.Count : span[1];
                    if (hi > rows.Count || lo > hi)
                    {
                        missing = string.Format("note {0} has {1} paragraph(s), so '{2}'{3} names paragraph {4}, "
                            + "which is not there - it was NOT edited", element.Id, rows.Count, stepSaid[s],
                            stepSpans[s] == null && defaultSpans.Count > 0 ? " on " + replacement : "",
                            Math.Max(lo, hi));
                        break;
                    }
                    for (int k = lo; k <= hi; k++) if (!picked.Contains(k - 1)) picked.Add(k - 1);
                }
                picked.Sort();
                stepParagraphs.Add(picked);
            }
            if (missing != null)
            {
                absent.Add(element.Id);
                pastEnd++;
                noteFormatting = (noteFormatting == null ? "" : noteFormatting + " ## ") + missing;
                continue;
            }

            // Applied to the COPY in order, and the rule written down as it goes:
            // `meant` is how every paragraph should read back, `named` which
            // paragraphs an instruction named for each field, `startsMeant` each
            // start number asked {paragraph, number}.
            var meant = new List<int[]>();
            foreach (var row in rows) meant.Add((int[])row.Clone());
            var named = new List<int>[8];
            for (int f = 0; f < 8; f++) named[f] = new List<int>();
            var startsMeant = new List<int[]>();
            bool startMoves = false;
            string notTaken = null;
            for (int s = 0; s < stepKinds.Count && notTaken == null; s++)
            {
                int kind = stepKinds[s][0], value = stepKinds[s][1];
                var picked = stepParagraphs[s];
                // One range per run of neighbouring paragraphs, closing carriage
                // return included - how a person selects them in Revit's editor.
                for (int a = 0; a < picked.Count && notTaken == null; a++)
                {
                    int b = a;
                    while (b + 1 < picked.Count && picked[b + 1] == picked[b] + 1) b++;
                    int at = rows[picked[a]][0];
                    var run = new TextRange(at, rows[picked[b]][0] + rows[picked[b]][1] + 1 - at);
                    try
                    {
                        if (kind == 0) shaped.SetListType(run, (ListType)value);
                        else if (kind == 1)
                        {
                            int lowest = shaped.GetMinimumListStartNumber();
                            int highest = shaped.GetMaximumListStartNumber();
                            if (value < lowest || value > highest)
                                notTaken = string.Format("'{0}': Revit starts a list at {1} to {2}", stepSaid[s],
                                    lowest, highest);
                            for (int k = picked[a]; k <= picked[b] && notTaken == null; k++)
                            {
                                var type = shaped.GetListType(new TextRange(rows[k][0], rows[k][1] + 1));
                                if (type != ListType.ArabicNumbers && type != ListType.LowerCaseLetters
                                    && type != ListType.UpperCaseLetters)
                                    notTaken = string.Format("'{0}': paragraph {1} is {2}, and only numbers or "
                                        + "letters have a start - make it numbers first, e.g. 'numbers {1}; start "
                                        + "{3} {1}'", stepSaid[s], k + 1, listName((int)type), value);
                            }
                            if (notTaken == null)
                            {
                                if (startAt(shaped, rows, picked[a]) != value) startMoves = true;
                                shaped.SetListStartNumber(run, value);
                                startsMeant.Add(new[] { picked[a], value });
                            }
                        }
                        else if (kind == 2)
                        {
                            int deepest = shaped.GetMaximumIndentLevel();
                            if (value > deepest)
                                notTaken = string.Format("'{0}': Revit indents a paragraph {1} level(s) at most",
                                    stepSaid[s], deepest);
                            else shaped.SetIndentLevel(run, value);
                        }
                        else if (kind == 3) shaped.SetBoldStatus(run, value == 1);
                        else if (kind == 4) shaped.SetItalicStatus(run, value == 1);
                        else shaped.SetUnderlineStatus(run, value == 1);
                    }
                    catch (Exception ex)
                    {
                        notTaken = string.Format("'{0}': Revit would not take it ({1})", stepSaid[s], ex.Message);
                    }
                    a = b;
                }
                if (notTaken != null) break;

                // Row fields: 2 list, 3 indent, 4 bold, 5 italic, 6 underline.
                int slot = kind == 0 ? 2 : kind == 2 ? 3 : kind >= 3 ? kind + 1 : -1;
                if (slot < 0) continue;
                foreach (var k in picked)
                {
                    // An empty paragraph has no character to carry a style.
                    if (slot >= 4 && rows[k][1] == 0) continue;
                    meant[k][slot] = slot >= 4
                        ? (value == 1 ? (int)FormatStatus.All : (int)FormatStatus.None)
                        : value;
                    if (!named[slot].Contains(k)) named[slot].Add(k);
                }
            }

            // Nothing is written for a note that will not take every
            // instruction: the edits were on a copy, so it is as it was.
            if (notTaken != null)
            {
                readOnly.Add(element.Id);
                noteFormatting = (noteFormatting == null ? "" : noteFormatting + " ## ")
                    + string.Format("note {0}: NOT edited - {1}", element.Id, notTaken);
                continue;
            }

            bool differs = startMoves;
            for (int k = 0; k < rows.Count && !differs; k++)
                for (int f = 2; f < 7; f++)
                    if (meant[k][f] != rows[k][f]) { differs = true; break; }
            if (!differs)
            {
                untouched.Add(element.Id);
                noteFormatting = (noteFormatting == null ? "" : noteFormatting + " ## ")
                    + string.Format("note {0}: already as {1} asks - nothing written", element.Id, asked);
                continue;
            }

            string formatFailed = null;
            try { note.SetFormattedText(shaped); }
            catch (Exception ex) { formatFailed = ex.Message; }
            if (formatFailed != null)
            {
                readOnly.Add(element.Id);
                noteFormatting = (noteFormatting == null ? "" : noteFormatting + " ## ")
                    + string.Format("note {0}: Revit would not take the formatting ({1}), so it was NOT "
                        + "changed", element.Id, formatFailed);
                continue;
            }

            // READ BACK every paragraph, named or not, against the rule.
            FormattedText shapedBack = null;
            string plainBack = null;
            try { shapedBack = note.GetFormattedText(); plainBack = shapedBack.GetPlainText(); } catch { }
            if (plainBack == null || noteText(plainBack) != wording)
            {
                unverified.Add(element.Id);
                noteFormatting = (noteFormatting == null ? "" : noteFormatting + " ## ")
                    + string.Format("note {0}: Revit took the formatting but its WORDING READS BACK "
                        + "DIFFERENT, and format never changes a word", element.Id);
                continue;
            }
            var got = paragraphsOf(shapedBack, wording);
            var off = new List<string>();
            for (int k = 0; k < got.Count && k < meant.Count; k++)
            {
                for (int f = 2; f < 8; f++)
                {
                    if (f >= 4 && rows[k][1] == 0) continue;
                    if (meant[k][f] == got[k][f]) continue;
                    Func<int, string> shown = v => f == 2 ? listName(v) : f == 3 ? "indent " + v
                        : styleNames[f] + " " + statusName(v);
                    off.Add(named[f].Contains(k)
                        ? string.Format("paragraph {0} was set to {1} and reads back {2}", k + 1,
                            shown(meant[k][f]), shown(got[k][f]))
                        : string.Format("paragraph {0} was not named and reads back {1}, not {2}{3}", k + 1,
                            shown(got[k][f]), shown(meant[k][f]),
                            f <= 3 ? " - Revit gives every paragraph of one list the same list type" : ""));
                }
            }
            foreach (var start in startsMeant)
            {
                int startsBack = startAt(shapedBack, got, start[0]);
                if (startsBack != start[1])
                    off.Add(string.Format("the list at paragraph {0} was set to start at {1} and reads back {2}",
                        start[0] + 1, start[1], startsBack));
            }

            string formatSaid;
            if (off.Count > 0)
            {
                unverified.Add(element.Id);
                formatSaid = string.Format("note {0}: the words are as they were but the formatting READS "
                    + "BACK DIFFERENT from {1} - {2}", element.Id, asked, string.Join("; ", off));
            }
            else
            {
                changed.Add(element.Id);
                formatSaid = string.Format("note {0}: {1} - {2} paragraph(s), words unchanged; read back "
                    + "{3}; {4}", element.Id, asked, got.Count,
                    listsOf(got, k => got[k][2] == (int)ListType.ArabicNumbers
                        || got[k][2] == (int)ListType.LowerCaseLetters
                        || got[k][2] == (int)ListType.UpperCaseLetters ? startAt(shapedBack, got, k) : -1),
                    stylesOf(got));
            }
            noteFormatting = (noteFormatting == null ? "" : noteFormatting + " ## ") + formatSaid;
            continue;
        }

        // Version 6: a prefix or suffix the note already has is not added again.
        string wantedNote = how == "prefix"
                ? (wording.StartsWith(find, StringComparison.OrdinalIgnoreCase) ? wording : find + wording)
            : how == "suffix"
                ? (wording.EndsWith(find, StringComparison.OrdinalIgnoreCase) ? wording : wording + find)
            : how == "set" ? find
            : (find.Length == 0 ? wording : wording.Replace(find, putIn));
        if (wantedNote == wording) { untouched.Add(element.Id); continue; }

        var before = paragraphsOf(shaped, wording);
        int startBefore = -1;
        try { startBefore = shaped.GetListStartNumber(new TextRange(0, before[0][1] + 1)); } catch { }
        var notKept = new List<string>();

        string writeFailed = null;
        try
        {
            if (how == "replace")
            {
                // Every match, left to right as String.Replace finds them, then
                // written last first so the earlier positions still hold.
                var hits = new List<int>();
                int searchFrom = 0;
                while (searchFrom <= wording.Length - find.Length)
                {
                    int hit = wording.IndexOf(find, searchFrom, StringComparison.Ordinal);
                    if (hit < 0) break;
                    hits.Add(hit);
                    searchFrom = hit + find.Length;
                }
                for (int h = hits.Count - 1; h >= 0; h--)
                    shaped.SetPlainText(new TextRange(hits[h], find.Length), putIn);
            }
            else if (how == "prefix") shaped.SetPlainText(new TextRange(0, 0), find);
            else if (how == "suffix") shaped.SetPlainText(new TextRange(wording.Length, 0), find);
            else
            {
                shaped.SetPlainText(new TextRange(0, wording.Length), find);
                int at = 0;
                var pieces = find.Split('\r');
                for (int k = 0; k < pieces.Length; k++)
                {
                    // New paragraph k wears old paragraph k; extra ones the last.
                    var old = before[Math.Min(k, before.Count - 1)];
                    var whole = new TextRange(at, pieces[k].Length + 1);
                    if (old[2] >= 0 && old[2] != (int)ListType.Mixed) shaped.SetListType(whole, (ListType)old[2]);
                    if (old[3] >= 0) shaped.SetIndentLevel(whole, old[3]);
                    if (pieces[k].Length > 0)
                    {
                        var chars = new TextRange(at, pieces[k].Length);
                        for (int s = 4; s < 8; s++)
                        {
                            if (old[s] < 0) continue;
                            bool on = old[s] == (int)FormatStatus.All;
                            if (old[s] == (int)FormatStatus.Mixed && k < before.Count)
                                notKept.Add(string.Format("paragraph {0} was partly {1} and is now not {1}",
                                    k + 1, styleNames[s]));
                            if (s == 4) shaped.SetBoldStatus(chars, on);
                            else if (s == 5) shaped.SetItalicStatus(chars, on);
                            else if (s == 6) shaped.SetUnderlineStatus(chars, on);
                            else shaped.SetAllCapsStatus(chars, on);
                        }
                    }
                    at += pieces[k].Length + 1;
                }
                if (startBefore > 0)
                {
                    try { shaped.SetListStartNumber(new TextRange(0, pieces[0].Length + 1), startBefore); }
                    catch { }
                }
            }
            note.SetFormattedText(shaped);
        }
        catch (Exception ex) { writeFailed = ex.Message; }

        if (writeFailed != null)
        {
            readOnly.Add(element.Id);
            noteFormatting = (noteFormatting == null ? "" : noteFormatting + " ## ")
                + string.Format("note {0}: Revit would not take the edit ({1}), so it was NOT changed",
                    element.Id, writeFailed);
            continue;
        }

        // READ BACK - the wording first, then every paragraph's formatting
        // against what the rule above meant it to be.
        FormattedText back = null;
        string backPlain = null;
        try { back = note.GetFormattedText(); backPlain = back.GetPlainText(); } catch { }
        if (backPlain == null || noteText(backPlain) != wantedNote)
        {
            unverified.Add(element.Id);
            noteFormatting = (noteFormatting == null ? "" : noteFormatting + " ## ")
                + string.Format("note {0}: Revit took the edit but its wording READS BACK DIFFERENT "
                    + "from what was asked", element.Id);
            continue;
        }

        var after = paragraphsOf(back, wantedNote);
        int startAfter = -1;
        try { startAfter = back.GetListStartNumber(new TextRange(0, after[0][1] + 1)); } catch { }
        var drift = new List<string>();
        if (how == "set" || after.Count == before.Count)
        {
            for (int k = 0; k < after.Count; k++)
            {
                var old = before[Math.Min(k, before.Count - 1)];
                if (old[2] != after[k][2])
                    drift.Add(string.Format("paragraph {0} was {1} and reads back {2}", k + 1,
                        listName(old[2]), listName(after[k][2])));
                if (old[3] != after[k][3])
                    drift.Add(string.Format("paragraph {0} was at indent {1} and reads back {2}", k + 1,
                        old[3], after[k][3]));
                for (int s = 4; s < 8; s++)
                {
                    if (old[s] < 0 || after[k][s] < 0) continue;
                    // A partly styled paragraph may change by the edit itself
                    // (replaced words take the first one's style); under set it
                    // is written plain, as said in `notKept`.
                    int expected = how == "set"
                        ? (old[s] == (int)FormatStatus.All ? (int)FormatStatus.All : (int)FormatStatus.None)
                        : old[s];
                    if (how != "set" && old[s] == (int)FormatStatus.Mixed) continue;
                    if (expected != after[k][s])
                        drift.Add(string.Format("paragraph {0}'s {1} reads back {2}", k + 1, styleNames[s],
                            ((FormatStatus)after[k][s]).ToString()));
                }
            }
        }
        else
        {
            // A replace that ran over a paragraph end, or put one in: the
            // paragraphs no longer pair one to one, so each must at least wear
            // a list some paragraph had before.
            var had = new List<int>();
            foreach (var row in before) if (!had.Contains(row[2])) had.Add(row[2]);
            for (int k = 0; k < after.Count; k++)
                if (!had.Contains(after[k][2]))
                    drift.Add(string.Format("paragraph {0} reads back {1}, which no paragraph had before",
                        k + 1, listName(after[k][2])));
        }
        if (startBefore > 0 && startAfter != startBefore)
            drift.Add(string.Format("the list started at {0} and now starts at {1}", startBefore, startAfter));

        string said;
        if (drift.Count > 0)
        {
            unverified.Add(element.Id);
            said = string.Format("note {0}: the wording is as asked but its formatting READS BACK "
                + "DIFFERENT - {1}", element.Id, string.Join("; ", drift));
        }
        else
        {
            changed.Add(element.Id);
            said = string.Format("note {0}: {1} paragraph(s) before, {2} after; read back {3}; indent "
                + "and text style {4}", element.Id, before.Count, after.Count,
                listsOf(after, k => k == 0 ? startAfter : -1),
                how == "set" ? "given back paragraph by paragraph" : "kept");
            if (notKept.Count > 0) said += "; NOT KEPT: " + string.Join("; ", notKept);
        }
        noteFormatting = (noteFormatting == null ? "" : noteFormatting + " ## ") + said;
        continue;
    }

    // The type whose parameter this element's field resolved to, or null for an
    // instance parameter or the element's own name. `land` records where the
    // element landed and, for a type, where its later instances land too.
    ElementId heldType = null;
    Action<List<ElementId>> land = list =>
    {
        list.Add(element.Id);
        if (heldType != null) typeLanded[heldType] = list;
    };

    if (editingName)
    {
        // A name that cannot be read cannot be renamed (RENAME_ELEMENTS'
        // rule, version 7): refused in `readOnly`, never edited as if blank.
        try { current = element.Name; }
        catch { land(readOnly); continue; }
    }
    else
    {
        // A FIELD NAME TWO PARAMETERS SHARE IS NOT EDITED. A shared or project
        // parameter can be bound beside a built-in one of the same name, and
        // LookupParameter then returns one of them - "determined at random", in
        // Autodesk's own reference - and the read-back below would agree with
        // itself. Checked on the instance, and on the type when the search
        // reaches it (D-54 s3, FRAGMENT-ISSUES 5b-203).
        if (element.GetParameters(field).Count > 1) { ambiguous.Add(element.Id); continue; }

        parameter = element.LookupParameter(field);
        // The type is asked second, never first: an instance parameter of the
        // same name is the one the modeller edited. Version 6: an instance
        // parameter that is NOT TEXT gives way to a text one of the same name on
        // the type - where Type Mark, Manufacturer and Model live - as
        // EDIT_PARAMETER_TEXT did; with no text one there, the instance's stands
        // and is reported as not text.
        if (parameter == null || parameter.StorageType != StorageType.String)
        {
            Element elementType = null;
            try { elementType = doc.GetElement(element.GetTypeId()); } catch { }
            int onType = 0;
            if (elementType != null) { try { onType = elementType.GetParameters(field).Count; } catch { } }
            // Ambiguous on the type only where the type would be the one
            // edited: always when the instance has no such field, and - for a
            // non-text instance field - only when one of the type's is text.
            // Otherwise the instance's field stands, as in version 5.
            if (onType > 1)
            {
                bool typeWouldBeUsed = parameter == null;
                if (!typeWouldBeUsed)
                {
                    try
                    {
                        foreach (Parameter candidate in elementType.GetParameters(field))
                            if (candidate.StorageType == StorageType.String) typeWouldBeUsed = true;
                    }
                    catch { }
                }
                if (typeWouldBeUsed) { ambiguous.Add(element.Id); continue; }
            }
            else
            {
                Parameter typeParameter = null;
                if (elementType != null) typeParameter = elementType.LookupParameter(field);
                if (typeParameter != null
                    && (parameter == null || typeParameter.StorageType == StorageType.String))
                {
                    parameter = typeParameter;
                    heldType = elementType.Id;
                }
            }
        }

        // A TYPE HANDED IN ITSELF, beside its own instances, shares their
        // once-per-type record - or a replace whose new text holds the old
        // would grow once for the type and once for its instances.
        if (heldType == null && parameter != null && element is ElementType) heldType = element.Id;

        if (parameter == null) { absent.Add(element.Id); continue; }

        // ONE WRITE PER TYPE (version 6). Forty instances of one type resolve
        // to ONE parameter: editing it forty times would put the prefix on
        // forty times, and a replace whose new text holds the old would grow
        // on every pass. The first instance decides; the rest land with it.
        if (heldType != null && typeLanded.ContainsKey(heldType))
        {
            typeLanded[heldType].Add(element.Id);
            if (typeNotText.Contains(heldType)) notText++;
            continue;
        }

        // Counted apart from the other two ways into `readOnly`, and only so
        // that "was it text on ANY of them" can be answered below. A locked
        // text field is a real finding; a number is a wrong request.
        if (parameter.StorageType != StorageType.String)
        {
            notText++;
            if (heldType != null) typeNotText.Add(heldType);
            land(readOnly);
            continue;
        }
        if (parameter.IsReadOnly) { land(readOnly); continue; }
        // A value that cannot be read is not a blank one: it is refused, as
        // EDIT_PARAMETER_TEXT did, never edited as if empty.
        try { current = parameter.AsString(); }
        catch { land(readOnly); continue; }
    }

    // A blank stays blank. (A note's wording being SET, the one exception,
    // was handled with the rest of the note above.)
    if (string.IsNullOrEmpty(current))
    {
        land(blank);
        continue;
    }
    if (current == null) current = "";
    if (how == "set") { land(absent); continue; }

    string wanted = transform(current);
    if (wanted == current) { land(untouched); continue; }

    bool accepted = false;
    try
    {
        if (editingName) { element.Name = wanted; accepted = true; }
        else accepted = parameter.Set(wanted);
    }
    catch { land(readOnly); continue; }

    if (!accepted) { land(readOnly); continue; }

    // READ BACK. A count of attempts is not a count of changes - which is the
    // failure this whole project is built around.
    string now = null;
    try
    {
        if (editingName) now = element.Name;
        else now = parameter.AsString();
    }
    catch { }

    if (now == null) land(unverified);
    else if (now == wanted)
    {
        land(changed);
        if (heldType != null) typesEdited.Add(heldType);
    }
    else land(unverified);
}

// AFTER THE LOOP, AND ONLY BECAUSE NOTHING WAS WRITTEN ON EITHER PATH. Both
// branches below are reached only when every element fell out at `absent` or
// at the not-text check, and both of those `continue` before a single Set.
if (refused == null && handed > 0 && absent.Count == handed && how == "format")
{
    refused = pastEnd > 0
        ? string.Format(
            "the paragraphs named are not there in {0} of the {1} element(s) handed in{2}, so NOTHING "
            + "WAS CHANGED: {3}. Count the paragraphs with READ_ELEMENT_PARAMETERS parameterName Text, "
            + "or say 'all' or 'last'", pastEnd, handed,
            pastEnd < handed ? " and the rest are not text notes" : "", noteFormatting)
        : string.Format(
            "'format' sets a text note's lists and styles and not one of the {0} element(s) handed in "
            + "is a text note, so NOTHING WAS CHANGED. Filter for Text Notes first", handed);
    absent.Clear();
}
else if (refused == null && !editingName && handed > 0 && absent.Count == handed && how == "set")
{
    refused = string.Format(
        "'set' writes a text note's wording and not one of the {0} element(s) handed in is a text "
        + "note, so NOTHING WAS CHANGED. Filter for Text Notes first", handed);
    absent.Clear();
}
else if (refused == null && !editingName && handed > 0 && absent.Count == handed)
{
    refused = string.Format(
        "not one of the {0} element(s) handed in carries a field called '{1}', so NOTHING WAS "
        + "CHANGED. Check the spelling - the name wanted is the parameter's own, not the "
        + "schedule heading, and it is matched exactly. {2}",
        handed, field, namesOnHand(elements));
    absent.Clear();
}
else if (refused == null && !editingName && handed > 0 && notText == handed)
{
    refused = string.Format(
        "'{0}' is not a text field on any of the {1} element(s) handed in, so NOTHING WAS "
        + "CHANGED. A number is not edited through its display text: a length shown as 2500 is "
        + "not exactly 2500 inside, and writing the display back would store the rounded "
        + "figure as the truth", field, handed);
    readOnly.Clear();
}

// VERSION 7 - RENAME_ELEMENTS' `notMatched`: for a replace, what did not hold
// the words to replace. Never an edit made (D-52).
if (how == "replace") notMatched.AddRange(untouched);
