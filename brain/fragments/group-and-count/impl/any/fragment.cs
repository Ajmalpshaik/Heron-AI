// NOT STANDALONE. Assumes `values` is in scope - normally from
// READ_ELEMENT_PARAMETERS.
//
// Ordered by count descending, then by the value itself, so the answer is
// stable between runs. An unstable order in a report reads as the model having
// changed when nothing has.
//
// Elements whose parameter was blank or absent are NOT here: they never reach
// `values`. That is deliberate - a breakdown that silently folds them into an
// empty-string row reports a size called "" and buries a data problem inside a
// size table.

var groups = values
    .GroupBy(pair => pair.Value)
    .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
    .OrderByDescending(kv => kv.Value)
    .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
    .ToList();
