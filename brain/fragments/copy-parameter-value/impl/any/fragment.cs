// NOT STANDALONE. Assumes `elements`, `fromName` and `toName` are in scope;
// leaves `copied`, `sourceEmpty`, `ambiguous`, `refused` and `typeMismatch`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE STORAGE TYPES ARE COMPARED BEFORE ANYTHING IS WRITTEN, and a mismatch
// stops the WHOLE batch. One wrong-typed pair found halfway leaves a set that
// is half converted with no record of where it stopped - the same reason
// RENUMBER_SEQUENTIAL checks its collisions up front.
//
// WHAT IS STORED, NOT WHAT IS DISPLAYED. A length shown as "2400 mm" is stored
// as feet. Copying the display string into a text field writes a unit-suffixed
// string that no later calculation can use, and it looks perfectly right on a
// schedule. Matching storage types and moving the internal value is what makes
// the copy exact - and it is why no unit conversion appears in this fragment.
//
// AN EMPTY SOURCE IS NOT COPIED. Writing a blank over a filled destination
// destroys data to no purpose, and it is indistinguishable afterwards from the
// destination never having been filled.
//
// A NAME TWO PARAMETERS SHARE IS NOT COPIED FROM OR TO. A shared or project
// parameter can be bound beside a built-in one of the same name, and then
// LookupParameter returns one of them - "determined at random", in Autodesk's
// own reference. Copying from the wrong one moves a value nobody chose; copying
// into the wrong one fills a field no schedule is reading. Either side matching
// twice puts the element in `ambiguous`, untouched (D-54 s3, FRAGMENT-ISSUES
// 5b-203).

var copied = 0;
var sourceEmpty = new List<ElementId>();
var ambiguous = new List<ElementId>();
var refused = new List<ElementId>();
var typeMismatch = false;

// Pass one: prove every pair agrees before a single write.
var pairs = new List<KeyValuePair<Parameter, Parameter>>();

foreach (var element in elements)
{
    if (element == null) continue;

    if (element.GetParameters(fromName).Count > 1
        || element.GetParameters(toName).Count > 1)
    {
        ambiguous.Add(element.Id);
        continue;
    }

    var from = element.LookupParameter(fromName);
    var to = element.LookupParameter(toName);

    if (from == null || to == null || to.IsReadOnly)
    {
        refused.Add(element.Id);
        continue;
    }

    if (from.StorageType != to.StorageType)
    {
        typeMismatch = true;
        break;
    }

    if (!from.HasValue)
    {
        sourceEmpty.Add(element.Id);
        continue;
    }

    pairs.Add(new KeyValuePair<Parameter, Parameter>(from, to));
}

if (!typeMismatch)
{
    foreach (var pair in pairs)
    {
        var from = pair.Key;
        var to = pair.Value;

        try
        {
            if (from.StorageType == StorageType.String) to.Set(from.AsString());
            else if (from.StorageType == StorageType.Double) to.Set(from.AsDouble());
            else if (from.StorageType == StorageType.Integer) to.Set(from.AsInteger());
            else if (from.StorageType == StorageType.ElementId) to.Set(from.AsElementId());
            else
            {
                refused.Add(to.Element.Id);
                continue;
            }
        }
        catch (Exception)
        {
            refused.Add(to.Element.Id);
            continue;
        }

        // READ BACK. Set returning without throwing is not evidence the value
        // stored - this repository has already been caught by a parameter that
        // returned TRUE and snapped the value to something else.
        var ok = from.StorageType == StorageType.String
            ? to.AsString() == from.AsString()
            : from.StorageType == StorageType.Integer
                ? to.AsInteger() == from.AsInteger()
                : from.StorageType == StorageType.ElementId
                    ? to.AsElementId() == from.AsElementId()
                    : Math.Abs(to.AsDouble() - from.AsDouble()) < 0.0000001;

        if (ok) copied++;
        else refused.Add(to.Element.Id);
    }
}
