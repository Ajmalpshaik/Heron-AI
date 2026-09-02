// NOT STANDALONE. Assumes `values` (element -> group key, from a parameter
// read) and `quantities` (element -> the number to add up) are in scope, and
// leaves `totals`, `counts`, `missing` and `incomplete` behind.
//
// READ ONLY, and in fact no Revit call at all - arithmetic over what two other
// fragments already read. That is why it is separate: nothing here can break on
// a version change, and the same summing serves sheet-metal area, insulation,
// tray weight and concrete volume rather than being written once per material.
//
// AN ELEMENT WITH NO QUANTITY IS THE WHOLE DIFFICULTY.
//
// Three things could be done with it and two of them lie:
//
//   add zero    the total looks complete when it is not - the classic
//               "reported the number that was asked for" failure
//   drop it     the count disagrees with a count taken over the same set,
//               and nobody can see why
//   count it    which is what happens here. `missing` holds how many per
//               group, so a total resting on partial data says so on the
//               same line as the number.
//
// `incomplete` is the same fact once, for a caller that has to decide whether
// this may be quoted at all.

var totals = new Dictionary<string, double>();
var counts = new Dictionary<string, int>();
var missing = new Dictionary<string, int>();

foreach (var pair in values)
{
    var id = pair.Key;
    var key = pair.Value ?? "";

    if (!totals.ContainsKey(key))
    {
        totals[key] = 0.0;
        counts[key] = 0;
        missing[key] = 0;
    }

    counts[key] = counts[key] + 1;

    double quantity;
    if (quantities.TryGetValue(id, out quantity)) totals[key] = totals[key] + quantity;
    else missing[key] = missing[key] + 1;
}

bool incomplete = false;
foreach (var pair in missing)
{
    if (pair.Value > 0) { incomplete = true; break; }
}
