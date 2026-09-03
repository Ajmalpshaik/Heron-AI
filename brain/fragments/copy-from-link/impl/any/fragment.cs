// NOT STANDALONE. Assumes `doc`, `linkInstance` and `linkedElementIds` are in
// scope; leaves `created` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE LINK'S TRANSFORM IS THE WHOLE JOB. A link is placed with a position and a
// rotation, and an element's coordinates inside it are in the LINK's world, not
// this model's. Copy without the transform and everything lands in the wrong
// place - usually somewhere plausible, which is far worse than obviously wrong.
//
// The transform comes from `Instance.GetTotalTransform`, which is on the base
// class: `RevitLinkInstance` itself declares only a constructor, so a member
// check that stops at the declared type reports it missing.
//
// THE COPIES ARE OURS AND STOP TRACKING THE LINK. Reload the link after the
// other team moves something and these stay where they were. Right for grids
// somebody is about to work to; a trap for anything still changing - so it is
// said in the result rather than discovered at the next issue.
//
// THE NEW IDS COME BACK, so the next step can act on the copies alone.

var created = new List<ElementId>();
var findings = new List<string>();

if (linkInstance == null)
{
    findings.Add("No link instance was given, and without one there is neither a source document nor a "
        + "transform to place the copies with");
}
else if (linkedElementIds == null || linkedElementIds.Count == 0)
{
    findings.Add("No element ids were given from inside the link");
}
else
{
    var source = linkInstance.GetLinkDocument();

    if (source == null)
    {
        findings.Add(string.Format("'{0}' is not loaded, so there is nothing to copy out of it. "
            + "RELOAD_LINKS is what loads it", linkInstance.Name));
    }
    else
    {
        try
        {
            var transform = linkInstance.GetTotalTransform();

            var madeIds = ElementTransformUtils.CopyElements(
                source, linkedElementIds, doc, transform, new CopyPasteOptions());

            if (madeIds != null)
                foreach (var id in madeIds) created.Add(id);

            // READ IT BACK. Revit returns the ids it made, and a copy that did
            // not happen is one that is not in that list - but an id that
            // resolves to nothing is not a copy either.
            var real = 0;
            foreach (var id in created) if (doc.GetElement(id) != null) real++;

            findings.Add(string.Format(
                "{0} of {1} element(s) copied out of '{2}' and placed through the link's own transform. "
                + "{3} of the returned ids resolve to real elements. They are OURS now and do NOT track "
                + "the link - reload it and these stay where they are",
                created.Count, linkedElementIds.Count, linkInstance.Name, real));

            if (created.Count < linkedElementIds.Count)
                findings.Add(string.Format("{0} were not copied - Revit refuses some kinds of element "
                    + "across documents, and it does so quietly by returning fewer ids",
                    linkedElementIds.Count - created.Count));
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("Copying out of '{0}' failed: {1}", linkInstance.Name, ex.Message));
        }
    }
}
