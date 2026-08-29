// NOT STANDALONE. Assumes `elements` is in scope.
//
// `countedNothing` is provided separately and deliberately. A caller that only
// reads `count` cannot tell a real zero from a filter that matched nothing and
// reported success - which is the exact defect D-30's negative case exists to
// catch. Making the zero its own signal means the reporting layer has to say
// something about it rather than printing "0" and moving on.

var count = elements.Count;
var countedNothing = count == 0;
