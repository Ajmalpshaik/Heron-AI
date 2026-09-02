// NOT STANDALONE. Assumes `elements`, `parameterName`, `prefix`, `startAt` and
// `existingValues` are in scope; leaves `renumbered`, `refused` and
// `collisions` behind.
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
var collisions = new List<string>();

var taken = new HashSet<string>(existingValues ?? new List<string>());

// What each element currently holds, so it can be discounted from the clash
// check, and so an unchanged value is not rewritten for nothing.
var current = new Dictionary<ElementId, string>();
var planned = new List<KeyValuePair<Element, string>>();
var next = startAt;

foreach (var element in elements)
{
    if (element == null) continue;

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

    var value = (prefix ?? string.Empty) + next.ToString();
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
