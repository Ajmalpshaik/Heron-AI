// NOT STANDALONE. Assumes `doc`, `elements` and `designOptionName` are in scope;
// leaves `findings`, `newElementIds`, `copied` and `wasPossible` behind.
//
// IT WRITES, AND IT OPENS NO TRANSACTION. Golden Rule 16 - the caller's
// transaction covers the copy, so the whole operation is one undo entry.
//
// Element.DesignOption IS READ-ONLY. There is no "reassign this element to that
// option" call anywhere in the API. The only route left is the one Revit's own
// interface ultimately uses: with the target option ACTIVE, copy in place. The
// copies belong to the option; the originals stay in the main model.
//
// NO PUBLIC API ACTIVATES A DESIGN OPTION, ON ANY RELEASE. The only member
// matching "ActiveDesignOption" anywhere is the read-only
// DesignOption.GetActiveDesignOptionId - there is no setter on Document or on
// DesignOption, and the option-set class is internal. So this CHECKS which
// option is active and stops with the menu path when it is the wrong one, rather
// than copying into whichever one happened to be active.
//
// IT NEVER ACTIVATES OR DEACTIVATES ANYTHING, INCLUDING AFTERWARDS. It cannot
// set the active option, so it must not clear the one the modeller set up.
//
// THERE IS NO deleteOriginals FLAG. Copying and deleting are two jobs - compose
// DELETE_ELEMENTS after this one, so the destructive half stays visible in the
// operation and separately refusable.

var findings = new List<string>();
var newElementIds = new List<ElementId>();
var copied = 0;
var wasPossible = false;

DesignOption target = null;
var available = new List<string>();

foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(DesignOption)))
{
    var option = element as DesignOption;
    if (option == null) continue;
    available.Add(option.Name);
    if (target == null
        && !string.IsNullOrEmpty(designOptionName)
        && string.Equals(option.Name, designOptionName, StringComparison.OrdinalIgnoreCase))
    {
        target = option;
    }
}

if (available.Count == 0)
{
    findings.Add("This model has NO design options at all, so nothing could be put in one. Design "
        + "options are created in Manage - Design Options, and no API here creates one either");
}
else if (target == null)
{
    findings.Add("There is no design option called '" + (designOptionName ?? "")
        + "' in this model. The ones that exist are: " + string.Join(", ", available.ToArray())
        + ". NOTHING was copied");
}
else
{
    var activeId = DesignOption.GetActiveDesignOptionId(doc);

    if (activeId == null || activeId != target.Id)
    {
        // THE REFUSAL, AND IT IS THE MOST USEFUL THING THIS FRAGMENT DOES.
        // Copying into whichever option is active would be a wrong answer that
        // looks like a right one.
        findings.Add("'" + target.Name + "' IS NOT THE OPTION CURRENTLY BEING EDITED, and no public "
            + "Revit API on any release can make one active - the only member matching "
            + "\"ActiveDesignOption\" anywhere is the read-only getter this fragment just called. "
            + "NOTHING WAS COPIED");
        findings.Add("Make it active by hand first - the Design Option dropdown on the status bar, or "
            + "Manage - Design Options - select '" + target.Name + "' - Edit Selected - and run this "
            + "again. It will then copy into whichever option is active at that point");
    }
    else if (elements == null || elements.Count == 0)
    {
        findings.Add("'" + target.Name + "' is active and ready, but no elements were handed in, so "
            + "nothing was copied");
    }
    else
    {
        var sourceIds = new List<ElementId>();
        foreach (var element in elements)
        {
            if (element == null || !element.IsValidObject) continue;
            sourceIds.Add(element.Id);
        }

        if (sourceIds.Count == 0)
        {
            findings.Add("Every element handed in had already gone from the model, so nothing was "
                + "copied");
        }
        else
        {
            try
            {
                var results = ElementTransformUtils.CopyElements(doc, sourceIds, XYZ.Zero);
                if (results != null)
                {
                    foreach (var id in results) newElementIds.Add(id);
                }
                copied = newElementIds.Count;
                wasPossible = true;

                if (copied != sourceIds.Count)
                {
                    findings.Add(copied + " copies came back for " + sourceIds.Count
                        + " element(s) asked for. Revit refuses to copy some kinds of element, and "
                        + "the difference is what did NOT go into the option");
                }
            }
            catch (Exception ex)
            {
                findings.Add("The copy failed and NOTHING went into '" + target.Name + "': "
                    + ex.Message + ". No transaction was opened here, so the caller's rollback "
                    + "covers whatever else was in the same operation");
            }
        }
    }
}

findings.Insert(0, wasPossible
    ? copied + " element(s) COPIED into design option '" + target.Name
        + "'. THE ORIGINALS ARE STILL IN THE MAIN MODEL AND WERE NOT TOUCHED - Element.DesignOption is "
        + "read-only, so there is no move, only a copy. To remove them, compose DELETE_ELEMENTS after "
        + "this one, where the deletion is visible and separately refusable"
    : "Nothing was copied into a design option. The reason is below, and it is a reason rather than a "
        + "failure - read it before trying again");
