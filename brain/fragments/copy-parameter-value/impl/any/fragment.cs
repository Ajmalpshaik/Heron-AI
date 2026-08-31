// NOT STANDALONE. Assumes `doc`, `elements`, `fromParameter` and `toParameter`
// are in scope, and leaves `copied`, `sourceBlank`, `typeMismatch`,
// `readOnly`, `absent`, `unverified` and `resolvedFrom` behind.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one - Golden Rule 16.
//
// THE STORED VALUE MOVES, NEVER THE DISPLAYED ONE.
//
// Reading a length as text gives "2500" - a ROUNDED rendering of a number that
// is not exactly 2500. Writing that text into another length parameter stores
// the rounded value AS THE TRUTH, and the two parameters then disagree by an
// amount too small to see and too real to ignore once something totals them.
//
// So storage types must MATCH, and a mismatch is REFUSED rather than converted.
// A number copied into a text parameter is the case that looks harmless and is
// not: it is a one-way door, and what comes back is the rounded value.
//
// EACH SIDE IS RESOLVED INDEPENDENTLY. The source often lives on the TYPE and
// the target on the INSTANCE - Type Mark into Comments is exactly that shape.
//
// AN EMPTY SOURCE IS NOT COPIED. Writing a blank over a filled target destroys
// data to no purpose, and nobody notices until the schedule is issued.

var copied = new List<ElementId>();
var sourceBlank = new List<ElementId>();
var typeMismatch = new List<ElementId>();
var readOnly = new List<ElementId>();
var absent = new List<ElementId>();
var unverified = new List<ElementId>();
var resolvedFrom = new Dictionary<ElementId, string>();

foreach (var element in elements)
{
    if (element == null) continue;

    Element elementType = null;
    try { elementType = doc.GetElement(element.GetTypeId()); } catch { }

    var source = element.LookupParameter(fromParameter);
    string from = "instance";
    if (source == null && elementType != null)
    {
        source = elementType.LookupParameter(fromParameter);
        from = "type";
    }

    // The TARGET is only ever taken on the instance. Writing to a type
    // parameter changes every element of that type at once, which is a far
    // larger edit than the one asked for and would be invisible in the count.
    var target = element.LookupParameter(toParameter);

    if (source == null || target == null) { absent.Add(element.Id); continue; }
    if (target.IsReadOnly) { readOnly.Add(element.Id); continue; }
    if (source.StorageType != target.StorageType) { typeMismatch.Add(element.Id); continue; }
    if (!source.HasValue) { sourceBlank.Add(element.Id); continue; }

    bool accepted = false;
    try
    {
        if (source.StorageType == StorageType.String)
        {
            var value = source.AsString();
            if (string.IsNullOrEmpty(value)) { sourceBlank.Add(element.Id); continue; }
            accepted = target.Set(value);
        }
        else if (source.StorageType == StorageType.Double) accepted = target.Set(source.AsDouble());
        else if (source.StorageType == StorageType.Integer) accepted = target.Set(source.AsInteger());
        else if (source.StorageType == StorageType.ElementId) accepted = target.Set(source.AsElementId());
    }
    catch { readOnly.Add(element.Id); continue; }

    if (!accepted) { readOnly.Add(element.Id); continue; }

    // READ BACK, comparing like with like rather than through text.
    bool matches = false;
    try
    {
        if (source.StorageType == StorageType.String)
            matches = target.AsString() == source.AsString();
        else if (source.StorageType == StorageType.Double)
            matches = Math.Abs(target.AsDouble() - source.AsDouble()) < 1e-9;
        else if (source.StorageType == StorageType.Integer)
            matches = target.AsInteger() == source.AsInteger();
        else if (source.StorageType == StorageType.ElementId)
            matches = target.AsElementId() == source.AsElementId();
    }
    catch { }

    if (matches)
    {
        copied.Add(element.Id);
        resolvedFrom[element.Id] = from;
    }
    else unverified.Add(element.Id);
}
