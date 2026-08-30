// NOT STANDALONE. Assumes `doc`, `elements` and `offset` are in scope; leaves
// `copies` and `notCopied` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). `offset` is in internal FEET.
//
// WHY THERE IS NO POSITION CHECK HERE, WHEN THE MOVE AND ROTATE FRAGMENTS ARE
// ALMOST ENTIRELY POSITION CHECKS.
//
// Those two call an API that returns NOTHING and can silently do nothing. This
// one returns the ids it created. A copy that did not happen is simply absent
// from that list, so the evidence arrives with the answer and a before-and-after
// comparison would be measuring something already known.
//
// The count is still checked rather than assumed equal: `notCopied` is the
// difference. Revit can return fewer than it was given - an element that cannot
// be duplicated is dropped without complaint - and a caller that assumed one
// copy per element would then pair up the wrong ids downstream.
//
// COPYING IS NOT MOVING, and the whole set is copied in ONE call on purpose.
// Copying elements one at a time breaks the relationships between them: a duct
// and its fitting copied separately arrive unconnected, where copying both
// together preserves the join. That is why this takes a list rather than
// looping.

var copies = new List<ElementId>();
var notCopied = 0;

var source = new List<ElementId>();
foreach (var element in elements)
{
    if (element != null) source.Add(element.Id);
}

if (source.Count > 0)
{
    var made = ElementTransformUtils.CopyElements(doc, source, offset);
    if (made != null) copies.AddRange(made);

    // Fewer back than went in. Not an error and not silence either - the
    // caller needs to know the lists do not line up before it starts pairing
    // originals with copies.
    notCopied = source.Count - copies.Count;
    if (notCopied < 0) notCopied = 0;
}
