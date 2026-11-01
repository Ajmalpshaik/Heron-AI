// NOT STANDALONE. Assumes `doc`, `numbers`, `names` and `titleBlockName` are in
// scope, and leaves `created`, `numberedAs`, `namedAs`, `numberTaken`,
// `refused`, `titleBlockNotFound`, `noTitleblock`, `titleBlockProblem` and
// `notCreated` behind.
//
// ASSUMES AN OPEN TRANSACTION. It does not start one - Golden Rule 16.
//
// THE NUMBER IS CHECKED BEFORE ANYTHING IS CREATED.
//
// ViewSheet.Create makes the sheet with a number REVIT picks; setting the
// wanted one is a second step that can be refused for a clash. So a clash
// leaves a REAL SHEET in the register under the wrong number, and a count
// reports the job as done. That is the same shape DUPLICATE_SHEETS was
// re-authored to remove, where the earlier version reported "Duplicated 0 of 1"
// while the sheet count went 2 to 3 and an orphan stayed behind.
//
// Checking first removes the failure instead of cleaning up after it, and it
// makes a RE-RUN create nothing rather than a second set of misnumbered sheets.
//
// THE TITLE BLOCK HAS NO DEFAULT. Revit will create a sheet with none at all,
// and a register full of blank-bordered sheets is discovered at issue. Which
// title block a project uses is a project decision.
//
// NUMBER AND NAME ARE READ BACK SEPARATELY because they fail independently: the
// number can be refused while the name is accepted, and the result looks
// correct in any list showing only one of them.
//
// VERSION 2 (2026-11-01) - CREATE_SHEET MERGED IN, AND ITS RULES WITH IT.
//
// THE CLASH CHECK IGNORES CAPITALS. "M-101" and "m-101" are the same drawing to
// everybody except a string comparison, and Revit refuses the pair too. The
// line in `notCreated` names the sheet that already holds the number.
//
// AN EMPTY NUMBER IS REFUSED. Revit has no unnumbered sheets, and creating one
// to number it afterwards is the orphan above.
//
// A FAILED ViewSheet.Create IS A REFUSAL, NOT A TAKEN NUMBER. Version 1 put it
// in `numberTaken`, which sent the reader looking for a clash that was not
// there. It is a line in `notCreated` now, carrying Revit's own words - "Revit
// declined" does not say WHICH rule was broken and Revit's sentence does.
//
// THE TITLE BLOCK IS MATCHED SO TWO FAMILIES ARE TOLD APART. Two title block
// families can each have a type called "A1". The name may be written
// "Family: Type"; a plain type name that two families share is refused,
// naming both, never settled by taking the first.
//
// NO TITLE BLOCK ONLY WHEN THE CALLER SAYS `none` OUTRIGHT. CREATE_SHEET made a
// bare sheet whenever no title block came; this card refused. The safer rule
// is kept: a name that is not found refuses and creates nothing, and only the
// word `none` makes sheets with no title block - flagged in `noTitleblock`,
// because such a sheet looks finished and prints bare.

var created = new List<ElementId>();
var numberedAs = new Dictionary<ElementId, string>();
var namedAs = new Dictionary<ElementId, string>();
var numberTaken = new List<string>();
var refused = new List<ElementId>();
bool titleBlockNotFound = false;
bool noTitleblock = false;
string titleBlockProblem = "";
var notCreated = new List<string>();

var askedTitleBlock = (titleBlockName ?? "").Trim();

// By NAME among the title block types - a project standard is written in names,
// and an id is not portable between models. "Family: Type" or "Family : Type"
// names one exactly; a plain type name must belong to one family only.
ElementId titleBlockId = ElementId.InvalidElementId;
bool titleBlockChosen = false;

if (string.Equals(askedTitleBlock, "none", StringComparison.OrdinalIgnoreCase))
{
    // Said outright. The sheets are made bare and the answer says so.
    noTitleblock = true;
    titleBlockChosen = true;
}
else
{
    string askedFamily = null, askedType = null;
    int colon = askedTitleBlock.IndexOf(':');
    if (colon > 0)
    {
        askedFamily = askedTitleBlock.Substring(0, colon).Trim();
        askedType = askedTitleBlock.Substring(colon + 1).Trim();
    }

    var matches = new List<ElementId>();
    var matchNames = new List<string>();
    foreach (var type in new FilteredElementCollector(doc)
                 .OfCategory(BuiltInCategory.OST_TitleBlocks)
                 .WhereElementIsElementType()
                 .ToElements())
    {
        string typeName = null, familyName = null;
        try { typeName = type.Name; } catch { }
        var elementType = type as ElementType;
        if (elementType != null)
        {
            try { familyName = elementType.FamilyName; } catch { }
        }
        if (typeName == null) continue;

        bool byType = askedTitleBlock.Length > 0
                      && string.Equals(typeName, askedTitleBlock, StringComparison.OrdinalIgnoreCase);
        bool byBoth = askedFamily != null && familyName != null
                      && string.Equals(familyName, askedFamily, StringComparison.OrdinalIgnoreCase)
                      && string.Equals(typeName, askedType, StringComparison.OrdinalIgnoreCase);
        if (!byType && !byBoth) continue;

        matches.Add(type.Id);
        matchNames.Add((familyName ?? "(no family)") + ": " + typeName);
    }

    if (matches.Count == 1)
    {
        titleBlockId = matches[0];
        titleBlockChosen = true;
    }
    else if (matches.Count > 1)
    {
        titleBlockNotFound = true;
        titleBlockProblem = "\"" + askedTitleBlock + "\" is a title block type in "
            + matches.Count + " families, so it does not say which: "
            + string.Join(", or ", matchNames.ToArray())
            + ". Name it as Family: Type. Nothing was created.";
    }
    else
    {
        titleBlockNotFound = true;
        titleBlockProblem = (askedTitleBlock.Length == 0
                ? "No title block was named"
                : "No title block type called \"" + askedTitleBlock + "\" is loaded in " + doc.Title)
            + ". Name one as it shows in the Project Browser - Family: Type - or say none "
            + "for sheets with no title block. Nothing was created.";
    }
}

if (titleBlockChosen)
{
    // Every number in the register, WITHOUT CASE, with the sheet holding it so
    // a clash can be named. Placeholders count: a placeholder holds its number
    // precisely so nothing else takes it.
    var taken = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var sheet in new FilteredElementCollector(doc)
                 .OfClass(typeof(ViewSheet))
                 .Cast<ViewSheet>())
    {
        if (sheet == null) continue;
        string number = null, holder = null;
        try { number = sheet.SheetNumber; } catch { }
        try { holder = sheet.Name; } catch { }
        if (string.IsNullOrEmpty(number) || taken.ContainsKey(number)) continue;
        taken[number] = holder ?? "";
    }

    for (int i = 0; i < numbers.Count; i++)
    {
        string wantedNumber = (numbers[i] ?? "").Trim();

        if (wantedNumber.Length == 0)
        {
            notCreated.Add("(blank, entry " + (i + 1) + "): a sheet needs a number - Revit has "
                         + "no unnumbered sheets");
            continue;
        }

        // Checked BEFORE creating. Nothing is left behind to tidy up.
        string holderName;
        if (taken.TryGetValue(wantedNumber, out holderName))
        {
            numberTaken.Add(wantedNumber);
            notCreated.Add(wantedNumber + ": already used by \"" + holderName + "\" - Revit "
                         + "refuses a duplicate number, capitals or not");
            continue;
        }

        ViewSheet sheet = null;
        string revitSaid = null;
        try { sheet = ViewSheet.Create(doc, titleBlockId); }
        catch (Exception failure) { revitSaid = failure.Message; }
        if (sheet == null)
        {
            notCreated.Add(wantedNumber + ": Revit declined to create the sheet"
                + (revitSaid == null ? "" : ". Revit said: " + string.Join(" ", revitSaid.Split())));
            continue;
        }

        created.Add(sheet.Id);
        taken[wantedNumber] = names != null && i < names.Count ? (names[i] ?? "") : "";

        try { sheet.SheetNumber = wantedNumber; } catch { }
        if (names != null && i < names.Count && !string.IsNullOrWhiteSpace(names[i]))
        {
            try { sheet.Name = names[i].Trim(); } catch { }
        }

        string actualNumber = null, actualName = null;
        try { actualNumber = sheet.SheetNumber; } catch { }
        try { actualName = sheet.Name; } catch { }

        if (actualNumber != null) numberedAs[sheet.Id] = actualNumber;
        if (actualName != null) namedAs[sheet.Id] = actualName;

        if (actualNumber != wantedNumber) refused.Add(sheet.Id);
    }
}
