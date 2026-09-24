// NOT STANDALONE. Assumes `doc`, `elements`, `parameterName`, `find`,
// `replaceWith`, `prefix` and `suffix` are in scope; leaves `edited`,
// `typesEdited`, `unchanged`, `absent`, `ambiguous` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One batch, one undo entry.
//
// THE EXISTING VALUE IS THE INPUT. That is what separates this from writing a
// parameter: the old text is read, edited, and written back. Nothing is
// invented, and an element whose value cannot be read is refused rather than
// given a new one built out of nothing.
//
// IT MUST BE SAFE TO RUN TWICE.
//
// A prefix already on the value is not added again. Batch jobs get re-run -
// after an undo, after the filter is widened, after somebody is unsure whether
// it took - and a prefix that stacks makes the second run worse than the
// first, in a way that then has to be unpicked by hand.
//
// A TYPE PARAMETER IS EDITED ONCE AND REACHES FURTHER THAN THE SELECTION.
//
// Type Mark, Manufacturer and Model live on the TYPE. Forty instances of one
// type resolve to one parameter: writing it forty times would apply the prefix
// forty times, and the value would be wrong in a way that looks like the
// fragment worked. It is written once per type, and the types touched are
// named, because every other instance of that type changed too - including the
// ones nobody selected.
//
// THE READ-BACK IS THE ONLY EVIDENCE.
//
// Setting a parameter returns true in cases where the value does not change:
// read-only, driven by a formula, owned by another user on a workshared model.
// Every value is read back and compared with what was intended, and anything
// that did not take is REFUSED rather than counted as done. Reporting what was
// asked for instead of what landed is the defect this whole library is built
// around.
//
// A NAME TWO PARAMETERS SHARE IS NOT EDITED.
//
// A shared or project parameter can be bound beside a built-in one of the same
// name, and then LookupParameter returns one of them - "determined at random",
// in Autodesk's own reference. The read-back would look the name up the same
// way and agree with itself, so the only safe answer is to touch neither: the
// element goes in `ambiguous` (D-54 s3, FRAGMENT-ISSUES 5b-203). Checked on the
// instance, and on the type when the search falls through to it.

int edited = 0;
var typesEdited = new List<ElementId>();
var unchanged = new List<ElementId>();
var absent = new List<ElementId>();
var ambiguous = new List<ElementId>();
var refused = new List<ElementId>();

Func<string, string> editText = current =>
{
    string result = current ?? "";

    // Replace first: a prefix added before the replace would be inside the
    // text the replace then searches, which makes the order of two independent
    // edits matter in a way nobody could predict.
    if (!string.IsNullOrEmpty(find))
        result = result.Replace(find, replaceWith ?? "");

    if (!string.IsNullOrEmpty(prefix) &&
        !result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        result = prefix + result;

    if (!string.IsNullOrEmpty(suffix) &&
        !result.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        result = result + suffix;

    return result;
};

// One entry per parameter actually written, so a shared type appears once
// however many instances pointed at it.
var doneTypes = new HashSet<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;

    // The instance first, the type second - never the other way round. A
    // parameter present on both would otherwise be edited on the type, which
    // changes every instance in the project rather than the ones asked for.
    Parameter parameter = null;
    Element owner = element;
    bool onType = false;

    bool twice = false;
    try { twice = element.GetParameters(parameterName).Count > 1; } catch { }
    if (twice) { ambiguous.Add(element.Id); continue; }

    try { parameter = element.LookupParameter(parameterName); } catch { }
    if (parameter == null || parameter.StorageType != StorageType.String)
    {
        Element type = null;
        try { type = doc.GetElement(element.GetTypeId()); } catch { }
        Parameter typeParameter = null;
        if (type != null)
        {
            bool typeTwice = false;
            try { typeTwice = type.GetParameters(parameterName).Count > 1; } catch { }
            if (typeTwice) { ambiguous.Add(element.Id); continue; }

            try { typeParameter = type.LookupParameter(parameterName); } catch { }
        }

        if (typeParameter != null && typeParameter.StorageType == StorageType.String)
        {
            parameter = typeParameter;
            owner = type;
            onType = true;
        }
        else
        {
            // Either no such parameter, or it is not text. A number has no
            // meaningful prefix and forcing one through the string setter is
            // how a numeric parameter ends up unreadable.
            absent.Add(element.Id);
            continue;
        }
    }

    if (onType && !doneTypes.Add(owner.Id)) continue;

    if (parameter.IsReadOnly) { refused.Add(element.Id); continue; }

    string was = null;
    try { was = parameter.AsString(); } catch { refused.Add(element.Id); continue; }

    string wanted = editText(was);
    if (wanted == (was ?? ""))
    {
        // Nothing to do: the prefix is already there, or the search text is
        // not in this value. Counting it as edited would report forty changed
        // when four were.
        unchanged.Add(element.Id);
        continue;
    }

    try { parameter.Set(wanted); }
    catch { refused.Add(element.Id); continue; }

    // What actually landed.
    string now = null;
    try { now = parameter.AsString(); } catch { }

    if (now == wanted)
    {
        edited++;
        if (onType) typesEdited.Add(owner.Id);
    }
    else
        refused.Add(element.Id);
}
