// NOT STANDALONE. Assumes `blank`, `absent` and `parameterName` are in scope -
// normally `blank` and `absent` from READ_ELEMENT_PARAMETERS.
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
