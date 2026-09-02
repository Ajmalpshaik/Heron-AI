// NOT STANDALONE. Assumes `doc`, `elements`, `field`, `mode`, `text` and
// `replacement` are in scope, and leaves `changed`, `untouched`, `blank`,
// `readOnly`, `absent` and `unverified` behind.
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
// THE ELEMENT'S NAME IS JUST ANOTHER FIELD.
//
// `field` of "Name" edits Element.Name; anything else is a parameter. The
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

var changed = new List<ElementId>();
var untouched = new List<ElementId>();
var blank = new List<ElementId>();
var readOnly = new List<ElementId>();
var absent = new List<ElementId>();
var unverified = new List<ElementId>();

bool editingName = string.Equals((field ?? "").Trim(), "Name",
                                 StringComparison.OrdinalIgnoreCase);
string how = (mode ?? "").Trim().ToLowerInvariant();

Func<string, string> transform = current =>
{
    if (how == "prefix") return text + current;
    if (how == "suffix") return current + text;
    if (how == "replace")
    {
        if (string.IsNullOrEmpty(text)) return current;
        return current.Replace(text, replacement ?? "");
    }
    return current;
};

foreach (var element in elements)
{
    if (element == null) continue;

    string current = null;
    Parameter parameter = null;

    if (editingName)
    {
        try { current = element.Name; } catch { }
    }
    else
    {
        parameter = element.LookupParameter(field);
        if (parameter == null)
        {
            // The type is asked second, never first: an instance parameter of
            // the same name is the one the modeller edited.
            Element elementType = null;
            try { elementType = doc.GetElement(element.GetTypeId()); } catch { }
            if (elementType != null) parameter = elementType.LookupParameter(field);
        }

        if (parameter == null) { absent.Add(element.Id); continue; }
        if (parameter.StorageType != StorageType.String) { readOnly.Add(element.Id); continue; }
        if (parameter.IsReadOnly) { readOnly.Add(element.Id); continue; }
        try { current = parameter.AsString(); } catch { }
    }

    if (string.IsNullOrEmpty(current)) { blank.Add(element.Id); continue; }

    string wanted = transform(current);
    if (wanted == current) { untouched.Add(element.Id); continue; }

    bool accepted = false;
    try
    {
        if (editingName) { element.Name = wanted; accepted = true; }
        else accepted = parameter.Set(wanted);
    }
    catch { readOnly.Add(element.Id); continue; }

    if (!accepted) { readOnly.Add(element.Id); continue; }

    // READ BACK. A count of attempts is not a count of changes - which is the
    // failure this whole project is built around.
    string now = null;
    try
    {
        if (editingName) now = element.Name;
        else now = parameter.AsString();
    }
    catch { }

    if (now == null) unverified.Add(element.Id);
    else if (now == wanted) changed.Add(element.Id);
    else unverified.Add(element.Id);
}
