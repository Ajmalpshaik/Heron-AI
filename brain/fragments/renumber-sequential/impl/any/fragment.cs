// NOT STANDALONE. Assumes `elements`, `parameterName`, `prefix`, `startAt`,
// `existingValues` and `width` (version 3) are in scope; leaves `renumbered`,
// `refused`, `ambiguous`, `collisions` and `widthRefused` (version 3) behind.
//
// VERSION 3 (2026-11-07, row 5b-215) - THE NUMBER CAN BE ZERO-PADDED. Version 2
// wrote the bare number, so one call could not give "Office 01" to "Office 09"
// and "Office 10" to "Office 14": on 2026-09-24 that took two passes and a
// one-room fix. `width` is how many digits the number has at least - 2 gives
// 01..09 then 10..14 - padded with zeros on the left, NEVER CUT: 100 with
// width 2 stays 100, because a cut number is a duplicate waiting to happen.
// Left out it is 0, the bare number, version 2's job. A width below 0 or above
// 9 is refused before anything is worked out, and `widthRefused` says so.
// (The prefix's own trailing space - "Office " - arrives since 2026-11-07 when
// it is written in double quotes, prefix="Office ".)
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// TWO PASSES, AND THE ORDER OF THEM IS THE POINT.
//
// Pass one works out every value and checks it against what the model already
// holds. Pass two writes, and only if pass one found nothing wrong. Renumbering
// half a corridor and stopping is worse than not starting: the drawing now has
// two numbering schemes in it and no record of where the change stopped.
//
// A COLLISION IS NOT A WARNING. Revit accepts two doors called D-05 without
// complaint - it is a legal model, it just is not a correct drawing, and
// nobody finds out until it is printed or scheduled. So a clash refuses the
// whole batch, and names the values that clashed so the caller can pick a
// different prefix or start number rather than guess.
//
// `existingValues` IS AN INPUT, NOT SOMETHING READ HERE. What counts as
// "already taken" depends on the scope somebody means - this level, this
// building, the whole model - and that is a question about the job, not about
// the API. Collecting it inside this fragment would bake one answer in.
//
// THE ELEMENTS BEING RENUMBERED ARE EXCLUDED FROM THEIR OWN COLLISION CHECK.
// A door already called D-05 that is about to be renumbered to D-05 is not a
// clash with itself, and treating it as one would refuse the commonest batch
// of all: re-running a numbering that is already correct.

var renumbered = 0;
var refused = new List<ElementId>();
var ambiguous = new List<ElementId>();
var collisions = new List<string>();

var taken = new HashSet<string>(existingValues ?? new List<string>());

// VERSION 3 - the digits, zero-padded to `width`, never cut.
string widthRefused = null;
if (width < 0 || width > 9)
{
    widthRefused = "width " + width + " is not a number of digits this pads to - say 0 (the bare "
        + "number) up to 9, e.g. 2 for 01, 02 ... 10. NOTHING WAS RENUMBERED.";
}
Func<int, string> digits = n =>
    width > 0 && n >= 0 ? n.ToString().PadLeft(width, '0') : n.ToString();

// What each element currently holds, so it can be discounted from the clash
// check, and so an unchanged value is not rewritten for nothing.
var current = new Dictionary<ElementId, string>();
var planned = new List<KeyValuePair<Element, string>>();
var next = startAt;

foreach (var element in (widthRefused == null ? elements : new List<Element>()))
{
    if (element == null) continue;

    // A NAME TWO PARAMETERS SHARE GETS NO NUMBER. A shared or project parameter
    // can be bound beside a built-in one of the same name, and LookupParameter
    // then returns one of them - "determined at random", in Autodesk's own
    // reference - so a number would land in a field no tag reads. It is skipped
    // BEFORE a number is given out, so the sequence has no gap where it would
    // have been (D-54 s3, FRAGMENT-ISSUES 5b-203).
    if (element.GetParameters(parameterName).Count > 1)
    {
        ambiguous.Add(element.Id);
        continue;
    }

    var parameter = element.LookupParameter(parameterName);
    if (parameter == null || parameter.IsReadOnly)
    {
        // Named, never skipped. A read-only or missing parameter is the
        // commonest reason a renumber "did nothing", and it is invisible
        // unless it is reported.
        refused.Add(element.Id);
        continue;
    }

    var was = parameter.AsString();
    current[element.Id] = was ?? string.Empty;

    var value = (prefix ?? string.Empty) + digits(next);
    next++;

    planned.Add(new KeyValuePair<Element, string>(element, value));
}

// Its own batch is not a clash with itself.
foreach (var was in current.Values)
{
    if (!string.IsNullOrEmpty(was)) taken.Remove(was);
}

var withinBatch = new HashSet<string>();
foreach (var entry in planned)
{
    if (taken.Contains(entry.Value) || !withinBatch.Add(entry.Value))
    {
        collisions.Add(entry.Value);
    }
}

if (collisions.Count == 0)
{
    foreach (var entry in planned)
    {
        var parameter = entry.Key.LookupParameter(parameterName);
        if (parameter == null || parameter.IsReadOnly)
        {
            refused.Add(entry.Key.Id);
            continue;
        }

        parameter.Set(entry.Value);

        // READ BACK, because Set returning true is not evidence that the value
        // stored is the value asked for - the parameter path in this repository
        // has already been caught reporting a size Revit then snapped to
        // something else. A string does not snap, but a refusal that returns
        // true still has to be caught by looking.
        var stored = parameter.AsString();
        if (stored == entry.Value) renumbered++;
        else refused.Add(entry.Key.Id);
    }
}
