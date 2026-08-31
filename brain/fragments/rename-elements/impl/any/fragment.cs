// NOT STANDALONE. Assumes `elements`, `find` and `replaceWith` are in scope;
// leaves `renamed`, `notMatched`, `refused` and `collisions` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// TWO PASSES. Every intended name is worked out and checked before anything is
// written, for the same reason RENUMBER_SEQUENTIAL does it: a half-renamed set
// carries two conventions and no record of where the change stopped.
//
// AN ELEMENT WHOSE NAME DOES NOT CONTAIN `find` IS NOT AN ERROR AND NOT A
// SUCCESS. Replacing nothing leaves the name identical, and counting it would
// report forty renamed when four changed. It goes to `notMatched`, which is
// also the honest answer to "did it work" - usually the search text was wrong.
//
// AN EMPTY `find` WOULD MATCH EVERYWHERE and .NET's Replace on an empty string
// is not a rename anybody asked for. Refused outright rather than interpreted.

var renamed = 0;
var notMatched = new List<ElementId>();
var refused = new List<ElementId>();
var collisions = new List<string>();

var planned = new List<KeyValuePair<Element, string>>();

if (string.IsNullOrEmpty(find))
{
    // Nothing is attempted. Every element is refused BY NAME rather than the
    // batch silently doing nothing, which would read as "it ran and changed
    // nothing", the least useful true statement available.
    foreach (var element in elements)
    {
        if (element != null) refused.Add(element.Id);
    }
}
else
{
    foreach (var element in elements)
    {
        if (element == null) continue;

        string was;
        try
        {
            was = element.Name;
        }
        catch (Exception)
        {
            // Some elements have no readable name at all. Cannot be read means
            // cannot be renamed, and guessing one would write a name nobody chose.
            refused.Add(element.Id);
            continue;
        }

        if (string.IsNullOrEmpty(was) || was.IndexOf(find, StringComparison.Ordinal) < 0)
        {
            notMatched.Add(element.Id);
            continue;
        }

        planned.Add(new KeyValuePair<Element, string>(
            element, was.Replace(find, replaceWith ?? string.Empty)));
    }

    // Two elements landing on one name. Revit throws for a type and accepts it
    // for an instance; neither outcome is wanted mid-batch, so it stops here.
    var seen = new HashSet<string>();
    foreach (var entry in planned)
    {
        if (!seen.Add(entry.Value)) collisions.Add(entry.Value);
    }

    if (collisions.Count == 0)
    {
        foreach (var entry in planned)
        {
            try
            {
                entry.Key.Name = entry.Value;
            }
            catch (Exception)
            {
                refused.Add(entry.Key.Id);
                continue;
            }

            // READ BACK. The set threw nothing, which is not evidence it took -
            // the standing lesson from the parameter path, where Revit returned
            // TRUE and stored a different value.
            if (entry.Key.Name == entry.Value) renamed++;
            else refused.Add(entry.Key.Id);
        }
    }
}
