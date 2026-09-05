// NOT STANDALONE. Assumes `doc`, `otherModelPath`, `categories`,
// `parameterNames` and `maxRows` are in scope; leaves `findings`, `added`,
// `removed`, `changed` and `sameSaveVersion` behind.
//
// READ ONLY on both documents. Opens no transaction on either, and closes the
// file it opened.
//
// THE VERSION LINE IS READ FIRST, AND IT IS WHAT SAYS WHETHER TO BELIEVE THE
// DIFF. Same version GUID and save count means the same model at the same save -
// any difference reported after that is a fault in the comparison.
//
// MATCHED ON ElementId. A save-as preserves ids, so on a model and an earlier
// save of ITSELF the id is the identity. The earlier library's composite-key
// mode is not carried over: it reports an edited Mark as one element added and
// one removed, which is the noise a change report must not produce.
//
// OPENED DETACHED, WORKSETS CLOSED, WRONG USER ALLOWED. Every real project is
// workshared: a bare open touches the central, loading worksets costs minutes
// this does not need, and the path handed over is usually somebody's local,
// which Revit otherwise refuses. All three are harmless on a plain file.
//
// Element.VersionGuid IS REACHED BY REFLECTION AND NEVER NAMED - it does not
// exist on Revit 2020, and that is a compile error a try/catch cannot hold. The
// lookup happens once.
//
// Document.GetChangedElements IS NOT USED, though it is faster on 2023+. It
// needs an episode GUID from a previous point in time, and nothing here has
// anywhere to have recorded one.

var findings = new List<string>();
var added = 0;
var removed = 0;
var changed = 0;
var sameSaveVersion = false;

Document other = null;

// Named the way a modeller would recognise the element on a drawing, not by id
// alone. An id on its own is unusable in a change report handed to anybody.
Func<Element, string> describe = element =>
{
    if (element == null) return "(gone)";
    var categoryName = element.Category != null ? element.Category.Name : "(no category)";
    var typeName = "";
    var type = element.Document.GetElement(element.GetTypeId()) as ElementType;
    if (type != null) typeName = " / " + type.Name;
    return categoryName + typeName + " '" + element.Name + "' (id " + element.Id + ")";
};

// AsValueString FIRST, so a size reads "450x250" in each model's own units and
// no units API is named. A value that only looks different because the two files
// display different units therefore reports as different - which is honest, and
// is said in the summary.
Func<Element, string, string> valueOf = (element, parameterName) =>
{
    if (element == null) return "(gone)";
    try
    {
        var parameter = element.LookupParameter(parameterName);
        if (parameter == null)
        {
            var type = element.Document.GetElement(element.GetTypeId()) as ElementType;
            if (type != null) parameter = type.LookupParameter(parameterName);
        }
        if (parameter == null) return "(absent)";
        if (!parameter.HasValue) return "(blank)";

        var shown = parameter.AsValueString();
        if (!string.IsNullOrEmpty(shown)) return shown;
        if (parameter.StorageType == StorageType.String) return parameter.AsString() ?? "(blank)";
        if (parameter.StorageType == StorageType.Integer) return parameter.AsInteger().ToString();
        if (parameter.StorageType == StorageType.ElementId)
        {
            var target = element.Document.GetElement(parameter.AsElementId());
            return target == null ? "(none)" : target.Name;
        }
        return "(no readable value)";
    }
    catch (Exception) { return "(unreadable)"; }
};

if (string.IsNullOrEmpty(otherModelPath) || !System.IO.File.Exists(otherModelPath))
{
    findings.Add("There is no file at '" + (otherModelPath ?? "")
        + "', so NOTHING was compared. That is not a model with no changes");
}
else if (categories == null || categories.Count == 0)
{
    findings.Add("No categories were given, so nothing was compared. A comparison with no categories "
        + "would report a clean diff over an empty set, which reads exactly like good news");
}
else
{
    try
    {
        // ---- identity first, because it decides whether the diff means anything ----
        var thisVersion = "";
        var otherVersion = "";
        var thisSaves = -1;
        var otherSaves = -1;
        try
        {
            var stamp = Document.GetDocumentVersion(doc);
            thisVersion = stamp.VersionGUID.ToString();
            thisSaves = stamp.NumberOfSaves;
        }
        catch (Exception) { }

        var openOptions = new OpenOptions();
        openOptions.DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets;
        openOptions.AllowOpeningLocalByWrongUser = true;
        openOptions.Audit = false;
        try
        {
            openOptions.SetOpenWorksetsConfiguration(
                new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets));
        }
        catch (Exception) { }

        var modelPath = ModelPathUtils.ConvertUserVisiblePathToModelPath(otherModelPath);

        try
        {
            other = doc.Application.OpenDocumentFile(modelPath, openOptions);
        }
        catch (Exception ex)
        {
            // THE TWO REVIT EXCEPTION TYPES ARE MATCHED BY NAME, NOT CAUGHT BY TYPE, and that is a
            // rule rather than a preference: a fragment may use only what the harness declares it
            // has in scope, and the Revit exceptions namespace is not in that list. Catching
            // the fully-qualified exception type would smuggle in a namespace the
            // executor was never told to supply - green here, missing at the PC. The type NAME is
            // the same fact and needs nothing declared. (The qualified name is not written out even
            // in this comment: tools/check-structure.py reads the whole file, and it is right to -
            // the point of the rule is that the name does not appear in brain/ at all.)
            var kind = ex.GetType().Name;

            if (kind == "CorruptModelException")
            {
                // REVIT SAYS "CORRUPT" FOR A FILE FROM ANOTHER RELEASE, and that word sends people
                // looking for damage that is not there. The header reads without opening the file.
                var savedIn = "unknown";
                var running = "unknown";
                try { savedIn = BasicFileInfo.Extract(otherModelPath).Format; } catch (Exception) { }
                try { running = doc.Application.VersionNumber; } catch (Exception) { }

                findings.Add(savedIn != "unknown" && savedIn != running
                    ? "That file was saved in Revit " + savedIn + " and this is Revit " + running
                        + " - it is a VERSION MISMATCH, not damage. Upgrade it or open it in " + savedIn
                    : "Revit reports that file as corrupt, and its header says " + savedIn
                        + " against this Revit " + running + " - so this one really does look damaged");
            }
            else if (kind == "CannotOpenBothCentralAndLocalException")
            {
                findings.Add("That file is already open in this Revit session, or its central/local "
                    + "twin is. Close it first - nothing was compared");
            }
            else
            {
                findings.Add("That file could not be opened: " + ex.Message + ". Nothing was compared");
            }
        }

        if (other != null)
        {
            try
            {
                var stamp = Document.GetDocumentVersion(other);
                otherVersion = stamp.VersionGUID.ToString();
                otherSaves = stamp.NumberOfSaves;
            }
            catch (Exception) { }

            sameSaveVersion = thisVersion.Length > 0
                && thisVersion == otherVersion
                && thisSaves == otherSaves;

            if (sameSaveVersion)
            {
                findings.Add("THESE TWO FILES ARE THE SAME MODEL AT THE SAME SAVE - same version id, "
                    + "same save count. Anything reported below as a change is a fault in this "
                    + "comparison, not a change in the model");
            }
            else if (thisVersion.Length == 0 || otherVersion.Length == 0)
            {
                findings.Add("The save stamp could not be read on one of the two files, so there is "
                    + "no check here that they are genuinely different saves");
            }
            else
            {
                findings.Add("Different saves: this model at save " + thisSaves + ", the other at save "
                    + otherSaves + ". The diff below is worth reading");
            }

            // ---- the two sets, by category ----
            var categoryIds = new List<ElementId>();
            var categoryNames = new List<string>();
            foreach (var category in categories)
            {
                if (category == null) continue;
                categoryIds.Add(category.Id);
                categoryNames.Add(category.Name);
            }

            var mine = new Dictionary<ElementId, Element>();
            var theirs = new Dictionary<ElementId, Element>();

            if (categoryIds.Count == 0)
            {
                findings.Add("Every category handed in was null, so nothing was compared");
            }
            else
            {
                var filter = new ElementMulticategoryFilter(categoryIds);

                foreach (var element in new FilteredElementCollector(doc)
                    .WherePasses(filter).WhereElementIsNotElementType())
                {
                    if (element != null) mine[element.Id] = element;
                }
                foreach (var element in new FilteredElementCollector(other)
                    .WherePasses(filter).WhereElementIsNotElementType())
                {
                    if (element != null) theirs[element.Id] = element;
                }

                // The per-element stamp, looked up ONCE. Absent on 2020, in which case every
                // element takes the full parameter walk and the summary says so.
                var versionStamp = typeof(Element).GetProperty("VersionGuid");
                var skippedByStamp = 0;

                var addedRows = new List<string>();
                var removedRows = new List<string>();
                var changedRows = new List<string>();

                foreach (var pair in mine)
                {
                    if (theirs.ContainsKey(pair.Key)) continue;
                    added++;
                    if (addedRows.Count < maxRows)
                        addedRows.Add("  ADDED    " + describe(pair.Value));
                }

                foreach (var pair in theirs)
                {
                    if (mine.ContainsKey(pair.Key)) continue;
                    removed++;
                    if (removedRows.Count < maxRows)
                        removedRows.Add("  REMOVED  " + describe(pair.Value));
                }

                foreach (var pair in mine)
                {
                    Element older = null;
                    if (!theirs.TryGetValue(pair.Key, out older) || older == null) continue;

                    if (versionStamp != null)
                    {
                        try
                        {
                            var a = versionStamp.GetValue(pair.Value, null);
                            var b = versionStamp.GetValue(older, null);
                            if (a != null && a.Equals(b)) { skippedByStamp++; continue; }
                        }
                        catch (Exception) { }
                    }

                    var differences = new List<string>();
                    foreach (var parameterName in parameterNames)
                    {
                        if (string.IsNullOrEmpty(parameterName)) continue;
                        var now = valueOf(pair.Value, parameterName);
                        var then = valueOf(older, parameterName);
                        if (now == then) continue;
                        differences.Add(parameterName + ": '" + then + "' -> '" + now + "'");
                    }

                    if (differences.Count == 0) continue;
                    changed++;
                    if (changedRows.Count < maxRows)
                    {
                        changedRows.Add("  CHANGED  " + describe(pair.Value) + " | "
                            + string.Join("; ", differences.ToArray()));
                    }
                }

                foreach (var row in addedRows) findings.Add(row);
                foreach (var row in removedRows) findings.Add(row);
                foreach (var row in changedRows) findings.Add(row);

                if (added + removed + changed > maxRows)
                {
                    findings.Add("  ... only the first " + maxRows
                        + " of each kind are listed. The counts above are complete");
                }

                findings.Add(versionStamp == null
                    ? "Every matched element took the FULL parameter walk - this Revit has no "
                        + "per-element change stamp, so nothing could be skipped"
                    : skippedByStamp + " matched element(s) were skipped as unchanged on Revit's own "
                        + "per-element stamp, without reading a parameter");

                findings.Add("COVERED: " + string.Join(", ", categoryNames.ToArray())
                    + " | parameters compared: "
                    + (parameterNames.Count == 0
                        ? "NONE - so no element could be reported as changed, only added or removed"
                        : string.Join(", ", new List<string>(parameterNames).ToArray()))
                    + ". A CLEAN DIFF IS NOT A PROOF THAT NOTHING CHANGED - anything outside those "
                    + "categories, and any parameter not named, was never looked at");
            }
        }
    }
    finally
    {
        if (other != null)
        {
            try { other.Close(false); } catch (Exception) { }
        }
    }
}

findings.Insert(0, added + " added, " + removed + " removed, " + changed
    + " changed on the parameters named. Both models were read only; the other file was opened "
    + "detached and closed again"
    + (sameSaveVersion ? ". READ THE SAME-SAVE WARNING BELOW BEFORE THE NUMBERS" : ""));
