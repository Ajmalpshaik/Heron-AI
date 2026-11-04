// NOT STANDALONE. Assumes `doc`, `setupName`, `mode`, `newName` and `kind` are
// in scope, and leaves `done`, `kindUsed`, `nowCalled`, `setupsNow`, `refused`
// and `note` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// ONE CARD FOR EVERY KIND THE API REACHES. A DWG/DXF setup is an
// ExportDWGSettings element, a DGN setup ExportDGNSettings, a PDF setup
// ExportPDFSettings (2022 on), a print setting PrintSetting. The first three
// are made with their type's Create, renamed through the element's name and
// removed with Document.Delete. Print settings have no Create: they are
// copied, renamed and deleted through the print manager's PrintSetup, as the
// Print dialog does it.
//
// DWG AND DXF ARE ONE SETUP. One element holds both halves, so a copy carries
// the DXF options as well as the DWG ones.
//
// THE PRINT MANAGER IS NOT TRANSACTIONAL. Making a print setting current is
// application state an Undo does not put back (MANAGE_SHEET_SETS found the
// same of the print range). The current setting is read first and put back
// in a finally - or, when it was the one deleted, the In-Session setting is
// made current - and `note` says which.
//
// EVERY ANSWER IS READ BACK. What the kind holds afterwards comes from Revit's
// own list, not from what was asked, and a change Revit reported and the list
// does not show is not called done.

bool done = false;
string kindUsed = "";
string nowCalled = "";
string setupsNow = "";
string refused = "";
string note = "";

string operation = (mode ?? "").Trim().ToLowerInvariant();
if (operation == "copy" || operation == "duplicate as") operation = "duplicate";
if (operation == "new" || operation == "make" || operation == "add") operation = "create";
if (operation == "remove") operation = "delete";

string source = (setupName ?? "").Trim();
string target = (newName ?? "").Trim();
if (string.Equals(target, "none", StringComparison.OrdinalIgnoreCase)) target = "";

string wantedKind = (kind ?? "").Trim().ToLowerInvariant();
if (wantedKind == "any" || wantedKind == "all") wantedKind = "";
if (wantedKind == "dxf" || wantedKind == "dwg/dxf" || wantedKind == "cad" || wantedKind == "autocad") wantedKind = "dwg";
if (wantedKind == "printing" || wantedKind == "print setting" || wantedKind == "print settings") wantedKind = "print";

Func<string, string> words = k => k == "dwg" ? "DWG/DXF" : k == "dgn" ? "DGN" : k == "pdf" ? "PDF" : "print";

// Every setup of one kind, by name, as Revit lists it.
Func<string, List<string>> namesOf = k =>
{
    var list = new List<string>();
    try
    {
        if (k == "dwg") list.AddRange(ExportDWGSettings.ListNames(doc));
        else if (k == "dgn") list.AddRange(ExportDGNSettings.ListNames(doc));
#if !REVIT2020 && !REVIT2021
        else if (k == "pdf") list.AddRange(ExportPDFSettings.ListNames(doc));
#endif
        else if (k == "print")
            foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(PrintSetting)))
                if (element != null) list.Add(element.Name ?? "");
    }
    catch { }
    list.Sort(StringComparer.OrdinalIgnoreCase);
    return list;
};

Func<List<string>, string, string> exact = (list, name) =>
    list.FirstOrDefault(one => string.Equals((one ?? "").Trim(), name, StringComparison.OrdinalIgnoreCase));

if (operation != "create" && operation != "duplicate" && operation != "rename" && operation != "delete")
    refused = "Mode '" + mode + "' is not one of create, duplicate, rename, delete.";
else if (source.Length == 0)
    refused = "No setup name was given.";
else if (wantedKind.Length > 0 && wantedKind != "dwg" && wantedKind != "dgn" && wantedKind != "pdf" && wantedKind != "print")
    refused = "'" + kind + "' is not a kind of setup this manages. It manages DWG/DXF, DGN, PDF and print setups. "
        + "IFC, Navisworks, DWF and image export keep no setup the Revit API can reach.";
else if ((operation == "duplicate" || operation == "rename") && target.Length == 0)
    refused = "No new name was given to " + (operation == "rename" ? "rename '" : "copy '") + source + "' to.";
#if REVIT2020 || REVIT2021
else if (wantedKind == "pdf")
    refused = "PDF export setups need Revit 2022 or later - this Revit has no PDF export of its own, so there is no "
        + "PDF setup to " + operation + ".";
#endif

// ---- Which kind, and the setup's exact name --------------------------------
string found = "";
if (refused.Length == 0)
{
    if (operation == "create")
    {
        if (wantedKind.Length == 0)
            refused = "Say which kind of setup to create - DWG/DXF, DGN or PDF.";
        else if (wantedKind == "print")
            refused = "A print setting is made by saving the paper size, orientation and fit under a name - that is "
                + "SET_PRINT_SETTINGS. This copies, renames and deletes print settings.";
        else
        {
            kindUsed = wantedKind;
            // For create the new setup's name is setupName; newName is not used.
            target = source;
        }
    }
    else
    {
        var kinds = wantedKind.Length > 0 ? new List<string> { wantedKind }
            : new List<string> { "dwg", "dgn", "pdf", "print" };
        var hits = new List<string>();
        var everyName = new List<string>();
        foreach (var k in kinds)
        {
            var list = namesOf(k);
            foreach (var one in list) everyName.Add(words(k) + " '" + one + "'");
            var match = exact(list, source);
            if (match != null) { hits.Add(k); found = match; }
        }

        if (hits.Count == 0)
            refused = "No " + (wantedKind.Length > 0 ? words(wantedKind) + " " : "") + "export setup called '" + source + "'. "
                + (everyName.Count > 0
                    ? "The setups there are: " + string.Join(", ", everyName.Take(30)) + (everyName.Count > 30 ? " and more." : ".")
                    : "This project has none of that kind.")
                + " Nothing was changed.";
        else if (hits.Count > 1)
            refused = "'" + source + "' is the name of a " + string.Join(" setup and a ", hits.Select(words))
                + " setup - say which kind. Nothing was changed.";
        else kindUsed = hits[0];
    }
}

// ---- A new name already in use ---------------------------------------------
if (refused.Length == 0 && operation != "delete")
{
    var taken = exact(namesOf(kindUsed), target);
    bool sameSetup = operation == "rename" && string.Equals(found, taken, StringComparison.Ordinal);
    if (taken != null && !sameSetup)
        refused = "There is already a " + words(kindUsed) + " setup called '" + taken + "'. Nothing was changed.";
    else if (operation == "rename" && string.Equals(found, target, StringComparison.Ordinal))
        refused = "'" + found + "' is already called that. Nothing was changed.";
#if !REVIT2020 && !REVIT2021
    else if (kindUsed == "pdf")
    {
        bool valid = true;
        try { valid = ExportPDFSettings.IsValidName(doc, target); } catch { }
        if (!valid) refused = "Revit will not take '" + target + "' as a PDF setup name. Nothing was changed.";
    }
#endif
}

// ---- DWG/DXF ---------------------------------------------------------------
if (refused.Length == 0 && kindUsed == "dwg")
{
    try
    {
        if (operation == "create")
        {
            ExportDWGSettings.Create(doc, target);
            done = true;
        }
        else
        {
            var settings = ExportDWGSettings.FindByName(doc, found);
            if (settings == null) refused = "Revit listed '" + found + "' and then could not find it. Nothing was changed.";
            else if (operation == "duplicate")
            {
                var copy = ExportDWGSettings.Create(doc, target, settings.GetDWGExportOptions());
                try { copy.SetDXFExportOptions(settings.GetDXFExportOptions()); }
                catch { note = "The DWG options were copied; Revit would not copy the DXF half."; }
                try
                {
                    int before = settings.GetDWGExportOptions().GetExportLayerTable().Count;
                    int after = copy.GetDWGExportOptions().GetExportLayerTable().Count;
                    if (before != after)
                        note = (note.Length > 0 ? note + " " : "") + "The copy has " + after + " layer rows where '"
                            + found + "' has " + before + ".";
                }
                catch { }
                done = true;
            }
            else if (operation == "rename") { settings.Name = target; done = true; }
            else
            {
                string active = "";
                try { var a = ExportDWGSettings.GetActivePredefinedSettings(doc); if (a != null) active = a.Name ?? ""; } catch { }
                doc.Delete(settings.Id);
                if (string.Equals(active, found, StringComparison.Ordinal))
                    note = "'" + found + "' was the active DWG/DXF setup - the export dialog will open on another one.";
                done = true;
            }
        }
    }
    catch (Exception failure)
    {
        done = false;
        refused = "Revit refused to " + operation + " the DWG/DXF setup '" + (found.Length > 0 ? found : target)
            + "': " + failure.Message;
    }
}

// ---- DGN -------------------------------------------------------------------
if (refused.Length == 0 && kindUsed == "dgn")
{
    try
    {
        if (operation == "create")
        {
            ExportDGNSettings.Create(doc, target);
            done = true;
        }
        else
        {
            var settings = ExportDGNSettings.FindByName(doc, found);
            if (settings == null) refused = "Revit listed '" + found + "' and then could not find it. Nothing was changed.";
            else if (operation == "duplicate")
            {
                var copy = ExportDGNSettings.Create(doc, target, settings.GetDGNExportOptions());
                try
                {
                    int before = settings.GetDGNExportOptions().GetExportLayerTable().Count;
                    int after = copy.GetDGNExportOptions().GetExportLayerTable().Count;
                    if (before != after)
                        note = "The copy has " + after + " level rows where '" + found + "' has " + before + ".";
                }
                catch { }
                done = true;
            }
            else if (operation == "rename") { settings.Name = target; done = true; }
            else
            {
                string active = "";
                try { var a = ExportDGNSettings.GetActivePredefinedSettings(doc); if (a != null) active = a.Name ?? ""; } catch { }
                doc.Delete(settings.Id);
                if (string.Equals(active, found, StringComparison.Ordinal))
                    note = "'" + found + "' was the active DGN setup - the export dialog will open on another one.";
                done = true;
            }
        }
    }
    catch (Exception failure)
    {
        done = false;
        refused = "Revit refused to " + operation + " the DGN setup '" + (found.Length > 0 ? found : target)
            + "': " + failure.Message;
    }
}

// ---- PDF (Revit 2022 and later) --------------------------------------------
#if !REVIT2020 && !REVIT2021
if (refused.Length == 0 && kindUsed == "pdf")
{
    try
    {
        if (operation == "create")
        {
            ExportPDFSettings.Create(doc, target, new PDFExportOptions());
            done = true;
        }
        else
        {
            var settings = ExportPDFSettings.FindByName(doc, found);
            if (settings == null) refused = "Revit listed '" + found + "' and then could not find it. Nothing was changed.";
            else if (operation == "duplicate") { ExportPDFSettings.Create(doc, target, settings.GetOptions()); done = true; }
            else if (operation == "rename") { settings.Name = target; done = true; }
            else
            {
                string active = "";
                try { var a = ExportPDFSettings.GetActivePredefinedSettings(doc); if (a != null) active = a.Name ?? ""; } catch { }
                doc.Delete(settings.Id);
                if (string.Equals(active, found, StringComparison.Ordinal))
                    note = "'" + found + "' was the active PDF setup - the export dialog will open on another one.";
                done = true;
            }
        }
    }
    catch (Exception failure)
    {
        done = false;
        refused = "Revit refused to " + operation + " the PDF setup '" + (found.Length > 0 ? found : target)
            + "': " + failure.Message;
    }
}
#endif

// ---- Print settings, through the print manager -----------------------------
if (refused.Length == 0 && kindUsed == "print")
{
    PrintSetup setup = null;
    IPrintSetting currentBefore = null;
    PrintSetting acted = null;
    try
    {
        setup = doc.PrintManager.PrintSetup;
        currentBefore = setup.CurrentPrintSetting;
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(PrintSetting)))
        {
            var candidate = element as PrintSetting;
            if (candidate != null && string.Equals(candidate.Name ?? "", found, StringComparison.Ordinal)) { acted = candidate; break; }
        }
    }
    catch { refused = "The print manager would not open its print setup. Nothing was changed."; }

    if (refused.Length == 0 && acted == null)
        refused = "Revit listed the print setting '" + found + "' and then could not find it. Nothing was changed.";

    if (refused.Length == 0)
    {
        var beforeSetting = currentBefore as PrintSetting;
        bool currentWasActed = beforeSetting != null && beforeSetting.Id == acted.Id;
        try
        {
            setup.CurrentPrintSetting = acted;
            bool ok = operation == "duplicate" ? setup.SaveAs(target)
                : operation == "rename" ? setup.Rename(target)
                : setup.Delete();
            if (ok) done = true;
            else refused = "Revit did not " + operation + " the print setting '" + found + "'. Nothing was changed.";
        }
        catch (Exception failure)
        {
            refused = "Revit refused to " + operation + " the print setting '" + found + "': " + failure.Message;
        }
        finally
        {
            // PUT THE CURRENT PRINT SETTING BACK, whatever happened above.
            try
            {
                if (operation == "delete" && done && currentWasActed)
                {
                    setup.CurrentPrintSetting = setup.InSession;
                    note = "'" + found + "' was the current print setting; the Print dialog is now on <In-Session>.";
                }
                else if (currentBefore != null)
                {
                    setup.CurrentPrintSetting = currentBefore;
                    var back = setup.CurrentPrintSetting as PrintSetting;
                    note = "The current print setting is " + (back != null ? "'" + back.Name + "'" : "<In-Session>")
                        + ", as it was before.";
                }
            }
            catch
            {
                note = "The current print setting was NOT put back - it was "
                    + (beforeSetting != null ? "'" + (beforeSetting.Name ?? "") + "'" : "<In-Session>")
                    + " before. Check File - Print - Setup.";
            }
        }
    }
}

// ---- Read back -------------------------------------------------------------
if (kindUsed.Length > 0)
{
    var after = namesOf(kindUsed);
    setupsNow = string.Join("; ", after);
    if (done)
    {
        bool targetThere = exact(after, target) != null;
        bool sourceThere = exact(after, found) != null;
        bool caseOnly = operation == "rename" && string.Equals(found, target, StringComparison.OrdinalIgnoreCase);
        // A rename is read back letter for letter: "Setup 1" to "setup 1" is a
        // rename only if the list now spells it the new way.
        bool targetSpelt = after.Any(one => string.Equals((one ?? "").Trim(), target, StringComparison.Ordinal));

        if ((operation == "create" || operation == "duplicate") && !targetThere)
            refused = "Revit reported the " + operation + " done, and no " + words(kindUsed) + " setup called '" + target + "' is listed.";
        else if (operation == "rename" && (!targetSpelt || (sourceThere && !caseOnly)))
            refused = "Revit reported the rename done, and the list still reads '" + found + "' rather than '" + target + "'.";
        else if (operation == "delete" && sourceThere)
            refused = "Revit reported '" + found + "' deleted, and it is still listed.";

        if (refused.Length > 0) done = false;
        else nowCalled = operation == "delete" ? "" : exact(after, target);
    }
}
if (kindUsed.Length > 0) kindUsed = words(kindUsed);
