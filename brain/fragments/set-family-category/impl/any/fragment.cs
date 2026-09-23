// NOT STANDALONE. Assumes `doc` and `categoryName` are in scope; leaves
// `category`, `changed`, `wasCategory`, `parametersGained`, `notAFamily`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE CATEGORY IS MATCHED BY THE NAME REVIT SHOWS, never guessed. A name that
// matches nothing is refused with the close ones listed. Taking the nearest
// would file the family under a category nobody asked for, and a family in the
// wrong category schedules in the wrong place while looking perfectly normal.
//
// THE ONE WRITE IS THE CATEGORY, and when Revit refuses it this THROWS rather
// than reporting and carrying on. A Revit exception caught inside a transaction
// can leave that transaction marked failed while everything after it appears to
// succeed - reported from an earlier family build, where five writes after a
// caught failure "succeeded" and none of them was kept. Throwing hands the
// failure to the host, which rolls the whole call back and quotes Revit.
//
// THE REPORT IS READ BACK FROM THE FAMILY: the category it has afterwards, and
// how many parameters it gained, because a category switch brings that
// category's own built-in parameters with it.

var findings = new List<string>();
var category = "";
var wasCategory = "";
var changed = false;
var parametersGained = 0;
var notAFamily = false;
string refused = null;

var wanted = categoryName == null ? "" : categoryName.Trim();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor, so it has "
        + "no family category to change. Open the family first - OPEN_FAMILY_FOR_EDITING for one "
        + "loaded in the project, or File, New, Family for a new one.";
}
else if (wanted.Length == 0)
{
    refused = "No category was named. It is matched to the names in Revit's Family Category and "
        + "Parameters dialog - \"Air Terminals\", \"Mechanical Equipment\" - and is never guessed.";
}
else
{
    var family = doc.OwnerFamily;
    wasCategory = family.FamilyCategory == null ? "" : family.FamilyCategory.Name;

    // The document's own category list, in the language Revit runs in.
    var matches = new List<Category>();
    var allNames = new List<string>();
    foreach (Category candidate in doc.Settings.Categories)
    {
        if (candidate == null) continue;
        allNames.Add(candidate.Name);
        if (string.Equals(candidate.Name, wanted, StringComparison.OrdinalIgnoreCase))
            matches.Add(candidate);
    }

    if (matches.Count == 0)
    {
        // CLOSE NAMES, OFFERED AND NOT TAKEN. "Air Terminal" beside "Air
        // Terminals" is the usual miss; choosing it here would be a guess.
        var lowered = wanted.ToLowerInvariant();
        var near = allNames
            .Where(n => n.ToLowerInvariant().Contains(lowered) || lowered.Contains(n.ToLowerInvariant()))
            .Distinct()
            .OrderBy(n => n)
            .Take(8)
            .ToList();

        refused = "No category called \"" + wanted + "\" exists in this Revit, so nothing changed."
            + (near.Count > 0
                ? " Close names: " + string.Join(", ", near) + "."
                : " The name has to be one from the Family Category and Parameters dialog.");
    }
    else if (matches.Count > 1)
    {
        refused = "\"" + wanted + "\" names " + matches.Count + " categories in this Revit, so "
            + "nothing changed rather than one of them being picked.";
    }
    else
    {
        var target = matches[0];

        if (family.FamilyCategory != null && family.FamilyCategory.Id == target.Id)
        {
            category = target.Name;
            findings.Add("The family is already in \"" + target.Name + "\", so nothing changed.");
        }
        else
        {
            var before = doc.FamilyManager.Parameters.Size;

            try
            {
                family.FamilyCategory = target;
            }
            catch (Exception ex)
            {
                // THROWN, NOT REPORTED - see the header. Revit's words are the
                // useful part: which categories a family may take is its rule.
                throw new InvalidOperationException("Revit would not put this family in \""
                    + target.Name + "\": " + ex.Message + " A loadable family cannot take a "
                    + "system category such as Walls, and a model family cannot become an "
                    + "annotation one. Nothing was kept.");
            }

            doc.Regenerate();

            // READ BACK. The family's category afterwards is the answer, not
            // the assignment having returned.
            var now = doc.OwnerFamily.FamilyCategory;
            category = now == null ? "" : now.Name;
            changed = now != null && now.Id == target.Id;
            parametersGained = doc.FamilyManager.Parameters.Size - before;

            if (!changed)
            {
                findings.Add("The category was set to \"" + target.Name + "\" and the family reads \""
                    + category + "\" afterwards. Revit did not keep the change.");
            }
            else
            {
                findings.Add("The family was \"" + (wasCategory.Length == 0 ? "(no category)" : wasCategory)
                    + "\" and is now \"" + category + "\", read back from the family.");

                if (parametersGained > 0)
                    findings.Add("The switch added " + parametersGained + " parameter(s) of the new "
                        + "category's own - its built-in fields, such as flow on an air terminal.");
                else if (parametersGained < 0)
                    findings.Add("The family has " + (-parametersGained) + " FEWER parameter(s) "
                        + "than before: the old category's own built-in fields went with it.");
                else
                    findings.Add("The parameter count did not change.");
            }
        }
    }
}

if (refused != null) findings.Add(refused);
