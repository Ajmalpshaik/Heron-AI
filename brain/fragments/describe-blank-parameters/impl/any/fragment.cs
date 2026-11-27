// NOT STANDALONE. Assumes `blank`, `absent` and `parameterName` are in scope -
// normally `blank` and `absent` from READ_ELEMENT_PARAMETERS - and leaves
// `findings` and `flagged` behind.
//
// TWO FINDINGS, NOT ONE, and the separation is the whole reason this exists.
//
//   blank    the parameter is there and empty  -> somebody fills it in
//   absent   the element cannot have it at all -> a family or category problem
//
// One line saying "12 have no system name" sends a modeller to fill in 12
// values, three of which have nowhere to go. The fix for those three is not
// modelling work at all, and merging the two hides that completely.

var findings = new List<string>();

if (blank.Count > 0)
{
    findings.Add(blank.Count + " element(s) have " + parameterName +
                 " but it is empty - these can be filled in");
}

if (absent.Count > 0)
{
    findings.Add(absent.Count + " element(s) do NOT have " + parameterName +
                 " at all - that is a family or category question, not a " +
                 "value to type in");
}

// VERSION 2 (2026-11-26, FRAGMENT-ISSUES row 5b-2067, issue #1477) - A COUNT
// BESIDE THE SENTENCES. `findings` is prose, and prose is never judged as a
// quantity (D-51), so a run that rightly said nothing - every element had a
// value - still had no number to be zero, and the empty leg D-30 asks for
// could never be shown. `flagged` is how many elements the findings name,
// blank and absent together: 0 exactly when `findings` is empty. Kept as ONE
// number on purpose - the two are kept apart in `findings`, where a person
// reads them; this only says whether there was anything to say.
var flagged = blank.Count + absent.Count;
