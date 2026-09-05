// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `removed`
// and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16), so a drawing's worth of tags is
// one undo.
//
// IT DELETES THE TAG, NEVER THE THING TAGGED. That is the whole risk here.
// A set gathered loosely by category could hold the ducts as well as their
// tags, and a delete that took both would be the most damaging thing in this
// library - so anything that is not a tag is REFUSED and NAMED, never skipped
// quietly.
//
// A TAG IS VIEW-SPECIFIC. The same element tagged in three views has three
// tags, and removing one leaves the other two.

var removed = 0;
var findings = new List<string>();

if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given - hand in the tags to remove");
}
else
{
    var notTags = new List<string>();
    var refused = new List<string>();

    foreach (var element in elements)
    {
        if (element == null) continue;

        if (!(element is IndependentTag))
        {
            notTags.Add(string.Format("id {0} is a {1}", element.Id,
                element.Category == null ? "category-less element" : element.Category.Name));
            continue;
        }

        try
        {
            doc.Delete(element.Id);
            removed++;
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("id {0}: {1}", element.Id, ex.Message));
        }
    }

    findings.Add(string.Format("{0} tag(s) removed from {1} element(s) handed in. {2} of them were "
        + "NOT tags and nothing was deleted for those",
        removed, elements.Count, notTags.Count));

    foreach (var wrong in notTags) findings.Add("Not a tag, left alone: " + wrong);
    foreach (var failure in refused) findings.Add("Refused: " + failure);

    if (notTags.Count > 0)
        findings.Add("Those are named rather than counted because a set that contains model elements "
            + "is a wrong selection, and it must not read the same as a view that was only partly "
            + "tagged");

    if (removed > 0)
        findings.Add("The tagged elements themselves are untouched. A tag is view-specific, so the "
            + "same element tagged in another view still has that tag");
}
