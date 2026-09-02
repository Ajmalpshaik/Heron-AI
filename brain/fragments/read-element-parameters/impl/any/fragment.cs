// NOT STANDALONE. Assumes `elements` and `parameterName` are in scope.
//
// THREE OUTCOMES, NOT TWO, and this is the whole point of the fragment.
//
//   values   the parameter is there and has something in it
//   blank    the parameter is there and is empty
//   absent   the element does not have that parameter at all
//
// Collapsing blank and absent into "no value" is the common shortcut and it
// hides the difference that matters: a blank parameter is a modelling job, an
// absent one is a family or a category problem, and they go to different people.
// A report that says "12 have no system name" when 9 of them cannot have one is
// a report that sends somebody to fix the wrong thing.
//
// ----------------------------------------------------------------------
// VERSION 2 - TWO WAYS THIS ANSWERED "ABSENT" WHEN THE VALUE WAS RIGHT THERE
// ----------------------------------------------------------------------
//
// Version 1 asked `e.LookupParameter(name)` and nothing else. That is correct
// for an instance parameter on a family instance and wrong for two whole
// classes of question a modeller asks every day. Both were found by reading
// what a mature library had already had to solve, not by hitting them here.
//
// 1. THE PARAMETER LIVES ON THE TYPE. Size, fire rating, manufacturer, most of
//    a schedule's columns - all held once on the type, not per instance.
//    LookupParameter on the instance returns null for every one of them, so
//    "what size are these" came back as `absent` for the entire set. The type
//    is now asked second, and `resolvedFrom` records which one answered, so
//    "they all share one value" stays distinguishable from "they each carry
//    the same value" - a different fix in the model.
//
// 2. "LEVEL" IS NOT ONE PARAMETER. There is no single way to ask an element
//    which level it is on, and this is the trap that produces a confident
//    wrong answer rather than an error:
//
//      a wall           has NO level-named parameter at all; only Element.LevelId
//      a duct or pipe   calls it "Reference Level", and Element.LevelId is invalid
//      an air terminal  does have "Level", which is why a spot check looks fine
//
//    Ask for "Level" across a mixed set with LookupParameter alone and the
//    walls and the ducts come back absent while the terminals answer. The
//    report then reads as "most of the model has no level", which is not a
//    thing that can be true. `Element.LevelId` is tried first and five
//    built-in parameters after it, RBS_START_LEVEL_PARAM last because it is
//    the one that catches MEP curves.
//
// The special case is LEVEL ONLY. Category, Family and Type are presentation -
// they are how a table is labelled, not something read off the element - and
// D-20 keeps that at the edge where the user is. Adding them here would put
// this fragment in the business of formatting.

var values = new Dictionary<ElementId, string>();
var blank = new List<ElementId>();
var absent = new List<ElementId>();
var resolvedFrom = new Dictionary<ElementId, string>();

bool askingForLevel = string.Equals((parameterName ?? "").Trim(), "Level",
                                    StringComparison.OrdinalIgnoreCase);

// AsValueString() first: it gives the number as the user sees it, with the
// project's own units, which is what a modeller checks against. AsString()
// is the fallback for text parameters, where AsValueString returns null.
Func<Parameter, string> readValue = p =>
{
    string text = null;
    try { text = p.AsValueString(); } catch { }
    if (string.IsNullOrWhiteSpace(text))
    {
        try { text = p.AsString(); } catch { }
    }
    return text;
};

foreach (var e in elements)
{
    if (e == null) continue;

    if (askingForLevel)
    {
        // Element.LevelId is the only thing that answers for a wall, and it is
        // invalid on an MEP curve - so neither route alone is enough.
        ElementId levelId = ElementId.InvalidElementId;
        try { levelId = e.LevelId; } catch { }

        if (levelId == ElementId.InvalidElementId)
        {
            Parameter lp = null;
            try
            {
                lp = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                     ?? e.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM)
                     ?? e.get_Parameter(BuiltInParameter.LEVEL_PARAM)
                     ?? e.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)
                     ?? e.get_Parameter(BuiltInParameter.RBS_START_LEVEL_PARAM);
            }
            catch { }
            if (lp != null)
            {
                try { levelId = lp.AsElementId(); } catch { }
            }
        }

        if (levelId == null || levelId == ElementId.InvalidElementId)
        {
            // Genuinely not on a level - a view-specific annotation, a group, a
            // level itself. That is `absent`, and it is a real answer.
            absent.Add(e.Id);
            continue;
        }

        var levelElement = e.Document.GetElement(levelId);
        var levelName = levelElement == null ? null : levelElement.Name;
        if (string.IsNullOrWhiteSpace(levelName)) blank.Add(e.Id);
        else
        {
            values[e.Id] = levelName;
            resolvedFrom[e.Id] = "level";
        }
        continue;
    }

    var p = e.LookupParameter(parameterName);
    string from = "instance";

    if (p == null)
    {
        // The type is asked SECOND, never first: an instance parameter of the
        // same name must win, because that is the one the modeller edited.
        Element typeElement = null;
        try { typeElement = e.Document.GetElement(e.GetTypeId()); } catch { }
        if (typeElement != null)
        {
            p = typeElement.LookupParameter(parameterName);
            from = "type";
        }
    }

    if (p == null)
    {
        absent.Add(e.Id);
        continue;
    }

    var text = readValue(p);
    if (string.IsNullOrWhiteSpace(text)) blank.Add(e.Id);
    else
    {
        values[e.Id] = text;
        resolvedFrom[e.Id] = from;
    }
}
