// NOT STANDALONE. Assumes `values` is in scope - normally from
// READ_ELEMENT_PARAMETERS - and `quantities`, which is NULL when nothing
// handed one on. Leaves `groups`, `counts`, `totals`, `missing` and
// `incomplete` behind.
//
// Ordered by count descending, then by the value itself, so the answer is
// stable between runs. An unstable order in a report reads as the model having
// changed when nothing has.
//
// Elements whose parameter was blank or absent are NOT here: they never reach
// `values`. That is deliberate - a breakdown that silently folds them into an
// empty-string row reports a size called "" and buries a data problem inside a
// size table. A key that IS the empty string - an unfilled parameter read as
// one - is its own group, counted and totalled, never discarded.
//
// VERSION 2 (2026-11-01) - SUM_BY_GROUP MERGED IN. When a measuring fragment
// hands on `quantities` (MEASURE_RUN_QUANTITIES does, beside `values`), each
// group's quantity is added up as well as counted - the line a quantity
// surveyor prices: "forty lengths of 300x150 totalling 212 metres".
//
// AN ELEMENT WITH NO QUANTITY IS THE WHOLE DIFFICULTY. Three things could be
// done with it and two of them lie:
//
//   add zero    the total looks complete when it is not
//   drop it     the count disagrees with a count taken over the same set
//   count it    which is what happens here. `missing` holds how many per
//               group, so a total resting on partial data says so on the
//               same line as the number, and `incomplete` says it once
//
// NOT HANDED ON AND HANDED ON EMPTY ARE DIFFERENT ANSWERS. With no
// `quantities` at all (null - the binder's word for "nothing filled it") this
// counts, as version 1 did, and `totals` and `missing` stay empty. Handed on
// EMPTY, every element is missing a quantity and `incomplete` is true - a
// measure that found nothing is not a takeoff of zero.
//
// The grouping decides the population: a quantity for an element NOT in
// `values` is added to no group.

var groups = values
    .GroupBy(pair => pair.Value ?? "")
    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
    .OrderByDescending(kv => kv.Value)
    .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
    .ToList();

var counts = new Dictionary<string, int>();
foreach (var row in groups) counts[row.Key] = row.Value;

var totals = new Dictionary<string, double>();
var missing = new Dictionary<string, int>();
bool incomplete = false;

if (quantities != null)
{
    foreach (var row in groups)
    {
        totals[row.Key] = 0.0;
        missing[row.Key] = 0;
    }

    foreach (var pair in values)
    {
        var key = pair.Value ?? "";
        double quantity;
        if (quantities.TryGetValue(pair.Key, out quantity)) totals[key] = totals[key] + quantity;
        else missing[key] = missing[key] + 1;
    }

    foreach (var pair in missing)
    {
        if (pair.Value > 0) { incomplete = true; break; }
    }
}
