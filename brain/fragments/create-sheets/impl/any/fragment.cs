// NOT STANDALONE. Assumes `doc`, `numbers`, `names`, `titleBlockName`,
// `elements`, `numberSuffix` and `namePrefix` are in scope, and leaves
// `created`, `numberedAs`, `namedAs`, `numberTaken`, `refused`,
// `titleBlockNotFound`, `noTitleblock`, `titleBlockProblem`, `notCreated` and
// `notDuplicated` behind.
//
// VERSION 3 (2026-11-01) - DUPLICATE_SHEETS MERGED IN. Two ways to say which
// sheets to make, and the first one given decides:
//
//   NUMBERS GIVEN - versions 1 and 2 exactly, on the title block named.
//   `elements` is not read, so a selection never adds a copy.
//
//   NO NUMBERS, SHEETS HANDED IN WITH A `numberSuffix` - a copy of each:
//   the source's own title block, its number + the suffix, `namePrefix` + its
//   name. No suffix is refused: the copy would collide with its source, and
//   "make a new sheet" said with sheets selected must not copy them.
//
// NEITHER JOB COPIES THE VIEWS ON A SHEET. Both make sheets with nothing
// placed on them; DUPLICATE_VIEWS and PLACE_VIEW_ON_SHEET do that.
//
// `numberTaken` holds every clash in either job - a number already in the
// register, or one claimed earlier in the same batch (DUPLICATE_SHEETS'
// collidedNumbers); `notDuplicated` the sources no copy came back for.
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
var notDuplicated = new List<ElementId>();

// VERSION 3: WHICH JOB. The card's `absent:` words - "none" for a list or a
// prefix not given, "from the source sheets" for a title block not named -
// are read here as nothing given. A sheet numbered "none" is not a sheet
// anybody means.
Func<string, string, bool> sameText = (a, b) =>
    string.Equals((a ?? "").Trim(), b, StringComparison.OrdinalIgnoreCase);
Func<IList<string>, bool> leftOut = list =>
    list == null || list.Count == 0
    || (list.Count == 1 && (sameText(list[0], "none") || (list[0] ?? "").Trim().Length == 0));

bool byNumbers = !leftOut(numbers);
if (leftOut(names)) names = null;
var suffix = (numberSuffix ?? "").Trim();
if (sameText(suffix, "none")) suffix = "";
var prefix = namePrefix ?? "";
if (sameText(prefix, "none")) prefix = "";
// A NAME PREFIX KEEPS ITS SPACE - "DRAFT " is how a modeller types one, and
// trimming it would run the prefix into the name.

var askedTitleBlock = (titleBlockName ?? "").Trim();
bool titleBlockLeftOut = sameText(askedTitleBlock, "from the source sheets");
if (titleBlockLeftOut) askedTitleBlock = "";

bool copying = !byNumbers && elements != null && elements.Any(e => e != null);

if (byNumbers)
{
// ---- NUMBERS GIVEN: versions 1 and 2, exactly. `elements` is NOT read, so a
// sheet that happens to be selected is never copied by a request that named
// its numbers.

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
}
else if (copying)
{
    // ---- VERSION 3: COPIES OF THE SHEETS HANDED IN, from DUPLICATE_SHEETS.
    //
    // Each source sheet gives a new one: the SOURCE'S OWN title block, the
    // number made by adding `numberSuffix` to the source's, and the name
    // `namePrefix` + the source's name. THE VIEWS ON THE SOURCE ARE NOT
    // COPIED - neither job here places anything on a sheet; DUPLICATE_VIEWS
    // and PLACE_VIEW_ON_SHEET do, and welding them in would hide two
    // read-backs inside a third.
    //
    // THE NUMBER IS CHECKED BEFORE ANY SHEET EXISTS, as for a typed number,
    // capitals ignored - and each number made is added as it goes, so two
    // sources in one batch cannot both claim one new number. Measured where
    // DUPLICATE_SHEETS was re-authored from: "Duplicated 0 of 1 sheet(s), 1
    // failed" while the sheet count went 2 to 3 and an orphan remained. So
    // nothing is created that would then have to be deleted.
    if (suffix.Length == 0)
    {
        // A copy numbered like its source collides with it, every time - and
        // a request that gave no numbers and no suffix may have meant "make a
        // new sheet" with sheets selected. Refused, nothing made.
        foreach (var element in elements) if (element != null) notDuplicated.Add(element.Id);
        notCreated.Add("No sheet numbers were given, and no numberSuffix to number copies of the "
                     + elements.Count(e => e != null) + " thing(s) handed in - a copy numbered like "
                     + "its source would clash with it. Give the numbers, or the suffix for the "
                     + "copies (\"-COPY\"). Nothing was created.");
    }
    else
    {
        if (!titleBlockLeftOut && askedTitleBlock.Length > 0)
            titleBlockProblem = "The copies take each source sheet's own title block, so \""
                + askedTitleBlock + "\" was not used.";

        var taken = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var existing in new FilteredElementCollector(doc)
                     .OfClass(typeof(ViewSheet))
                     .Cast<ViewSheet>())
        {
            if (existing == null) continue;
            string number = null, holder = null;
            try { number = existing.SheetNumber; } catch { }
            try { holder = existing.Name; } catch { }
            if (string.IsNullOrEmpty(number) || taken.ContainsKey(number)) continue;
            taken[number] = holder ?? "";
        }

        foreach (var element in elements)
        {
            if (element == null) continue;
            var source = element as ViewSheet;
            if (source == null || source.IsTemplate)
            {
                notDuplicated.Add(element.Id);
                notCreated.Add("'" + element.Name + "': not a sheet, so there is nothing to copy");
                continue;
            }

            var wantedNumber = (source.SheetNumber ?? "") + suffix;

            // THE CHECK THAT MAKES THE ORPHAN IMPOSSIBLE. Before anything exists.
            string holderName;
            if (taken.TryGetValue(wantedNumber, out holderName))
            {
                numberTaken.Add(wantedNumber);
                notDuplicated.Add(source.Id);
                notCreated.Add(wantedNumber + ": already used by \"" + holderName + "\" - Revit "
                             + "refuses a duplicate number, capitals or not");
                continue;
            }

            // THE SOURCE'S TITLE BLOCK, so the copy matches its set. A
            // placeholder has none, and is a reservation in the register, not
            // a drawing; a sheet stripped of its title block has none either.
            // Neither is guessed at: an arbitrary title block would make a
            // sheet that does not match the set it belongs to.
            var sourceTitleBlockType = ElementId.InvalidElementId;
            if (!source.IsPlaceholder)
            {
                foreach (var tb in new FilteredElementCollector(doc, source.Id)
                                       .OfCategory(BuiltInCategory.OST_TitleBlocks)
                                       .WhereElementIsNotElementType())
                {
                    sourceTitleBlockType = tb.GetTypeId();
                    break;
                }
            }
            if (sourceTitleBlockType == ElementId.InvalidElementId)
            {
                notDuplicated.Add(source.Id);
                notCreated.Add(source.SheetNumber + ": "
                    + (source.IsPlaceholder ? "a placeholder sheet" : "a sheet with no title block")
                    + " - there is no title block to copy, so no copy was made");
                continue;
            }

            ViewSheet made = null;
            string revitSaid = null;
            try { made = ViewSheet.Create(doc, sourceTitleBlockType); }
            catch (Exception failure) { revitSaid = failure.Message; }
            if (made == null)
            {
                notDuplicated.Add(source.Id);
                notCreated.Add(wantedNumber + ": Revit declined to create the sheet"
                    + (revitSaid == null ? "" : ". Revit said: " + string.Join(" ", revitSaid.Split())));
                continue;
            }

            created.Add(made.Id);
            var wantedName = prefix + (source.Name ?? "");
            taken[wantedNumber] = wantedName;

            try { made.SheetNumber = wantedNumber; } catch { }
            try { made.Name = wantedName; } catch { }

            // READ BACK, number and name apart - they fail independently.
            string actualNumber = null, actualName = null;
            try { actualNumber = made.SheetNumber; } catch { }
            try { actualName = made.Name; } catch { }
            if (actualNumber != null) numberedAs[made.Id] = actualNumber;
            if (actualName != null) namedAs[made.Id] = actualName;

            // A COPY WHOSE NUMBER DID NOT TAKE IS NOT THE COPY ASKED FOR - but it
            // EXISTS, so it stays in `created` with its real number in
            // numberedAs, is named in `refused`, and its source is in
            // notDuplicated. The register must show the truth.
            if (!string.Equals(actualNumber, wantedNumber, StringComparison.OrdinalIgnoreCase))
            {
                refused.Add(made.Id);
                notDuplicated.Add(source.Id);
            }
        }
    }
}
else
{
    notCreated.Add("No sheet numbers were given and no sheets were handed in to copy, so there is "
                 + "nothing to create. Give the numbers and names, or select the sheets to copy "
                 + "with a numberSuffix.");
}
