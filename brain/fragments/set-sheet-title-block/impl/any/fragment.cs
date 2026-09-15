// NOT STANDALONE. Assumes `doc`, `elements` and `symbol` are in scope; leaves
// `swappedInPlace`, `replaced`, `notASheet`, `alreadyCarried` and `refused`
// behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// TWO DIFFERENT OPERATIONS AND ONLY ONE IS SAFE. Same family: the type is
// swapped on the existing instance and everything on it is kept. Different
// family: the old title block is DELETED and a new one placed, and anything
// typed into the old instance goes with it. They are counted separately and the
// replaced sheets are named one by one - "48 sheets updated" hides the four
// that quietly lost data.
//
// SHEET PARAMETERS ARE NOT AT RISK EITHER WAY. Sheet Number, Sheet Name and the
// issue date live on the SHEET. What a replace loses is what lived on the title
// block INSTANCE.
//
// A TYPE THAT IS NOT A TITLE BLOCK IS REFUSED. Revit will place almost anything
// on a sheet, and the result is a drawing with a door on it.
//
// ONE COLLECTOR, NOT ONE PER SHEET. The title block instances are gathered once
// and matched by OwnerViewId - a collector inside the loop is the same query
// run fifty times.
//
// READ FIRST, WRITE, READ BACK. Every count is what a second read found.

var swappedInPlace = 0;
var replaced = new List<string>();
var notASheet = 0;
var alreadyCarried = 0;
var refused = new List<string>();

if (symbol == null || !symbol.IsValidObject)
{
    refused.Add("no title block type was given, so no sheet was changed");
}
else
{
    // Is this actually a title block? Compared as ElementId to ElementId.
    var isTitleBlock = false;
    try
    {
        var titleBlocks = Category.GetCategory(doc, BuiltInCategory.OST_TitleBlocks);
        isTitleBlock = symbol.Category != null && titleBlocks != null
            && symbol.Category.Id == titleBlocks.Id;
    }
    catch (Exception) { }

    if (!isTitleBlock)
    {
        refused.Add(string.Format("'{0}' is not a title block type. Revit will place it on a sheet "
            + "and the result is a drawing with the wrong thing on it", symbol.Name));
    }
    else
    {
        try
        {
            if (!symbol.IsActive) { symbol.Activate(); doc.Regenerate(); }
        }
        catch (Exception ex)
        {
            refused.Add(string.Format("'{0}' could not be made ready for placing: {1}", symbol.Name,
                ex.Message));
        }

        if (refused.Count == 0)
        {
            // Gathered ONCE. A collector inside the loop is this same query run
            // once per sheet.
            var existingBlocks = new List<FamilyInstance>();
            foreach (var element in new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsNotElementType())
            {
                var instance = element as FamilyInstance;
                if (instance != null && instance.IsValidObject) existingBlocks.Add(instance);
            }

            foreach (var element in elements)
            {
                var sheet = element as ViewSheet;
                if (sheet == null || !sheet.IsValidObject) { notASheet++; continue; }

                FamilyInstance onThisSheet = null;
                foreach (var candidate in existingBlocks)
                {
                    if (candidate.OwnerViewId == sheet.Id) { onThisSheet = candidate; break; }
                }

                var sameFamily = false;
                var wasCalled = "(no title block)";
                // READ BEFORE THE WRITE, because after it `onThisSheet` IS the
                // new type - the same instance, re-pointed. Without this there
                // is nothing left to compare against and a swap that swapped
                // NOTHING is indistinguishable from one that did.
                ElementId wasTypeId = null;
                if (onThisSheet != null)
                {
                    try
                    {
                        wasCalled = onThisSheet.Symbol.Family.Name + " : " + onThisSheet.Symbol.Name;
                        wasTypeId = onThisSheet.Symbol.Id;
                        sameFamily = onThisSheet.Symbol.Family.Id == symbol.Family.Id;
                    }
                    catch (Exception) { }
                }

                try
                {
                    if (onThisSheet != null && sameFamily)
                    {
                        onThisSheet.Symbol = symbol;
                    }
                    else
                    {
                        if (onThisSheet != null) doc.Delete(onThisSheet.Id);
                        doc.Create.NewFamilyInstance(XYZ.Zero, symbol, sheet);
                    }
                }
                catch (Exception ex)
                {
                    refused.Add(string.Format("sheet {0} - {1}: {2}", sheet.SheetNumber, sheet.Name,
                        ex.Message));
                    continue;
                }

                // Read back off the sheet. A fresh collector, because the
                // instance list above is now out of date for this sheet.
                FamilyInstance nowThere = null;
                try
                {
                    foreach (var element2 in new FilteredElementCollector(doc)
                        .OfCategory(BuiltInCategory.OST_TitleBlocks)
                        .WhereElementIsNotElementType())
                    {
                        var instance = element2 as FamilyInstance;
                        if (instance == null || !instance.IsValidObject) continue;
                        if (instance.OwnerViewId == sheet.Id) { nowThere = instance; break; }
                    }
                }
                catch (Exception) { }

                if (nowThere == null)
                {
                    refused.Add(string.Format("sheet {0} - the change was accepted and a read back "
                        + "finds NO title block on the sheet", sheet.SheetNumber));
                    continue;
                }

                if (nowThere.Symbol.Id != symbol.Id)
                {
                    refused.Add(string.Format("sheet {0} - still carries '{1} : {2}' after the "
                        + "change", sheet.SheetNumber, nowThere.Symbol.Family.Name,
                        nowThere.Symbol.Name));
                    continue;
                }

                if (onThisSheet != null && sameFamily)
                {
                    // A SWAP THAT SWAPPED NOTHING IS NOT A SWAP. The sheet was
                    // already on this exact type, so the assignment above was a
                    // call that changed nothing - and counting it says "1 sheet
                    // updated" to somebody whose drawing set is untouched. The
                    // fragment's own positive case says "the same family, A
                    // DIFFERENT TYPE"; this is the other half of that sentence.
                    if (wasTypeId != null && wasTypeId == symbol.Id) alreadyCarried++;
                    else swappedInPlace++;
                }
                else if (onThisSheet == null)
                {
                    // NOTHING WAS DELETED, SO IT MUST NOT SAY SO. A sheet that
                    // never had a border and a sheet that lost its data are
                    // different outcomes and only one of them needs checking
                    // afterwards - cases.yaml names this distinction itself.
                    replaced.Add(string.Format("sheet {0} - had NO title block, and '{1} : {2}' was "
                        + "placed on it. Nothing was lost; there was nothing there",
                        sheet.SheetNumber, symbol.Family.Name, symbol.Name));
                }
                else
                {
                    replaced.Add(string.Format("sheet {0} - '{1}' was DELETED and replaced. Anything "
                        + "typed into that title block instance is gone; the sheet's own number, "
                        + "name and dates are not", sheet.SheetNumber, wasCalled));
                }
            }
        }
    }
}
