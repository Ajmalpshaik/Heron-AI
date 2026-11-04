// NOT STANDALONE. Assumes `doc`, `app`, `sourceDocumentTitle`, `kinds`,
// `nameContains` and `duplicates` are in scope, and leaves `copied`,
// `copiedIds`, `skipped`, `refusedCount`, `refused`, `report` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The undo belongs to THIS
// document - the source project is only read: it is collected from and handed
// to CopyElements as the source, and nothing here writes to it.
//
// REVIT'S MANAGE > TRANSFER PROJECT STANDARDS, for a fixed list of kinds. A
// type or a pattern is an element, so each kind is one document-to-document
// copy - the shape TRANSFER_MATERIALS_BETWEEN_DOCUMENTS and
// TRANSFER_VIEW_FILTERS_BETWEEN_DOCUMENTS use. The kinds those and the other
// TRANSFER_*_BETWEEN_DOCUMENTS cards carry, each with checks of its own, are
// NOT copied here: they are named, per kind, with the card that does them.
//
// AN EXISTING NAME IS KEPT, NEVER OVERWRITTEN. Each kind's names here are read
// first and a source type whose name is already here is never handed to the
// copy - it is counted skipped, and this project's type stays exactly as it
// was. Revit's API has only two answers to a duplicate type name - use the
// destination's, or abort (DuplicateTypeAction, 2020 to 2027) - and no
// overwrite, so "overwrite" is refused and pointed at Revit's own dialog.
//
// THE ONE QUESTION A SNIPPET CANNOT ANSWER. CopyPasteOptions takes an
// IDuplicateTypeNamesHandler, and that needs a class, which a fragment cannot
// declare. A type copied here that USES another type already named here - an
// arrowhead, a profile, a stacked wall's members - meets Revit's "Duplicate
// Types" dialog. Heron declines every dialog it has no row for, and the whole
// run is rolled back: nothing is kept, and nothing of this project is
// replaced. Counted here as a refusal and stopped at once, never retried item
// by item into the same dialog.
//
// NO ElementId IS EVER TURNED INTO AN int - that value changed width at 2024.

var copiedIds = new List<ElementId>();
var findings = new List<string>();
int copied = 0;
int skipped = 0;
int refusedCount = 0;
string refused = "";
string report = "";

// THE FIXED LIST. Key (letters only, singular), the words said back, the class
// collected. Nothing outside it is copied; "all" and "everything" are refused.
string[] kindKeys = {
    "fillpattern", "linepattern", "texttype", "dimensiontype", "walltype",
    "floortype", "rooftype", "ceilingtype", "filledregiontype", "gridtype", "leveltype" };
string[] kindWords = {
    "fill patterns", "line patterns", "text types", "dimension types", "wall types",
    "floor types", "roof types", "ceiling types", "filled region types", "grid types", "level types" };
Type[] kindClasses = {
    typeof(FillPatternElement), typeof(LinePatternElement), typeof(TextNoteType),
    typeof(DimensionType), typeof(WallType), typeof(FloorType), typeof(RoofType),
    typeof(CeilingType), typeof(FilledRegionType), typeof(GridType), typeof(LevelType) };

// Kinds another card carries, with checks this one does not make. Named back
// per kind, never copied here.
string[] elsewhereKeys = {
    "material", "linestyle", "objectstyle", "viewfilter", "filter", "viewtemplate",
    "view", "schedule", "projectparameter" };
string[] elsewhereCards = {
    "TRANSFER_MATERIALS_BETWEEN_DOCUMENTS", "TRANSFER_LINE_STYLES_BETWEEN_DOCUMENTS",
    "TRANSFER_OBJECT_STYLES_BETWEEN_DOCUMENTS", "TRANSFER_VIEW_FILTERS_BETWEEN_DOCUMENTS",
    "TRANSFER_VIEW_FILTERS_BETWEEN_DOCUMENTS", "TRANSFER_VIEWS_BETWEEN_DOCUMENTS",
    "TRANSFER_VIEWS_BETWEEN_DOCUMENTS", "TRANSFER_VIEWS_BETWEEN_DOCUMENTS",
    "TRANSFER_PROJECT_PARAMETERS_BETWEEN_DOCUMENTS" };

Func<string, string> keyOf = delegate (string said)
{
    var letters = new System.Text.StringBuilder();
    foreach (char c in (said ?? "").ToLowerInvariant())
        if (c >= 'a' && c <= 'z') letters.Append(c);
    string k = letters.ToString();
    if (k.EndsWith("s")) k = k.Substring(0, k.Length - 1);
    if (k == "textnotetype" || k == "textstyle") k = "texttype";
    if (k == "dimensionstyle") k = "dimensiontype";
    return k;
};

// "Office Standard.rvt", "Office Standard" and "office standard" are one name;
// "Office" is not - the source is named EXACTLY, never by a part of it.
Func<string, string> plainTitle = delegate (string t)
{
    string s = (t ?? "").Trim();
    string lower = s.ToLowerInvariant();
    if (lower.EndsWith(".rvt") || lower.EndsWith(".rte")) s = s.Substring(0, s.Length - 4);
    return s.Trim().ToLowerInvariant();
};

string wanted = plainTitle(sourceDocumentTitle);
string filterText = (nameContains ?? "").Trim();
if (string.Equals(filterText, "all", StringComparison.OrdinalIgnoreCase)) filterText = "";
string dupWord = keyOf(duplicates);

// THE KINDS ASKED FOR, split on commas, semicolons, "&" and " and ".
var askedRaw = new List<string>();
foreach (var part in (kinds ?? "").Replace(" and ", ",").Replace(" AND ", ",").Replace("&", ",").Replace(";", ",").Split(','))
{
    if (part.Trim().Length > 0) askedRaw.Add(part.Trim());
}

var chosen = new List<int>();
var elsewhere = new List<string>();
var unknown = new List<string>();
bool wantsEverything = false;
foreach (var said in askedRaw)
{
    string k = keyOf(said);
    if (k == "all" || k == "everything" || k == "allstandard" || k == "projectstandard")
    {
        wantsEverything = true;
        continue;
    }
    int at = Array.IndexOf(kindKeys, k);
    if (at >= 0) { if (!chosen.Contains(at)) chosen.Add(at); continue; }
    int other = Array.IndexOf(elsewhereKeys, k);
    if (other >= 0) { elsewhere.Add(said + " - not copied here: " + elsewhereCards[other] + " does it, with its own checks"); continue; }
    unknown.Add(said);
}

string listSaid = string.Join(", ", kindWords);

if (doc == null || doc.IsFamilyDocument)
{
    refused = "This copies project standards into a PROJECT, and the document in front is a family. " +
              "NOTHING WAS COPIED.";
}
else if (dupWord == "overwrite" || dupWord == "replace" || dupWord == "overwritedestination")
{
    refused = "Revit's API has no overwrite for a type already here - only 'use this project's' or " +
              "'abort'. To overwrite, use Manage > Transfer Project Standards > Overwrite in Revit; " +
              "that replaces each type's settings and only Undo takes it back, item by item it cannot " +
              "be. NOTHING WAS COPIED.";
}
else if (dupWord.Length > 0 && dupWord != "keep" && dupWord != "keepdestination" &&
         dupWord != "keepexisting" && dupWord != "newonly" && dupWord != "skip")
{
    refused = "'" + duplicates + "' is not an answer for a name already here - say keep (this " +
              "project's stays; the default) or leave it out. NOTHING WAS COPIED.";
}
else if (wantsEverything || askedRaw.Count == 0)
{
    refused = "Name the kinds to bring over - never everything at once. The list: " + listSaid +
              ". (Materials, line styles, object styles, view filters, views and project parameters " +
              "each have a transfer of their own.) NOTHING WAS COPIED.";
}
else if (unknown.Count > 0)
{
    refused = "Not a kind this copies: " + string.Join(", ", unknown.ToArray()) + ". The list: " +
              listSaid + ". NOTHING WAS COPIED.";
}
else if (chosen.Count == 0)
{
    refused = "Every kind named has a transfer of its own: " + string.Join("; ", elsewhere.ToArray()) +
              ". NOTHING WAS COPIED.";
    refusedCount = elsewhere.Count;
}
else
{
    // THE SOURCE: another PROJECT open in this Revit, named exactly. A link is
    // never one - it is reached through its RevitLinkInstance, and two links
    // can share a title.
    Document source = null;
    var openTitles = new List<string>();
    var matches = new List<string>();
    bool matchedFamily = false;
    bool matchedLink = false;
    foreach (Document candidate in app.Documents)
    {
        if (candidate == null) continue;
        string title = "";
        string path = "";
        try { title = candidate.Title ?? ""; } catch { }
        try { path = candidate.PathName ?? ""; } catch { }
        bool isLink = false;
        try { isLink = candidate.IsLinked; } catch { }
        if (candidate.Equals(doc)) continue;
        if (!isLink && !candidate.IsFamilyDocument) openTitles.Add(title);

        string fileName = "";
        try { fileName = System.IO.Path.GetFileName(path) ?? ""; } catch { }
        bool named = wanted.Length > 0 &&
                     (plainTitle(title) == wanted || (fileName.Length > 0 && plainTitle(fileName) == wanted));
        if (!named) continue;
        if (isLink) { matchedLink = true; continue; }
        if (candidate.IsFamilyDocument) { matchedFamily = true; continue; }
        source = candidate;
        matches.Add(title + (path.Length > 0 ? " (" + path + ")" : ""));
    }

    if (matches.Count > 1)
    {
        refused = "Two open projects carry the name '" + sourceDocumentTitle + "': " +
                  string.Join("; ", matches.ToArray()) + ". Close the one not meant. NOTHING WAS COPIED.";
        source = null;
    }
    else if (source == null)
    {
        refused = "No other open project is named '" + sourceDocumentTitle + "'" +
                  (matchedFamily ? " - a FAMILY of that name is open, and standards come from a project or template" : "") +
                  (matchedLink ? " - a LINK of that name is loaded, and a link is never the source here" : "") +
                  ". Both have to be open in this Revit; open projects now: " +
                  (openTitles.Count == 0 ? "none but this one" : string.Join(", ", openTitles.ToArray())) +
                  ". NOTHING WAS COPIED.";
    }

    if (source != null)
    {
        var copyOptions = new CopyPasteOptions();
        var lines = new List<string>();
        bool stopped = false;

        foreach (int at in chosen)
        {
            string word = kindWords[at];
            bool isFill = kindClasses[at] == typeof(FillPatternElement);
            if (stopped)
            {
                lines.Add(word + ": not tried - the run had already stopped");
                continue;
            }

            // A fill pattern's name is unique per target: "Diagonal up" can be
            // a drafting AND a model pattern, so the target is part of the key.
            Func<Element, string> keyFor = delegate (Element e)
            {
                string n = "";
                try { n = e.Name ?? ""; } catch { }
                if (n.Length == 0) return "";
                if (isFill)
                {
                    var fp = e as FillPatternElement;
                    string target = "";
                    try { target = fp == null ? "" : fp.GetFillPattern().Target.ToString(); } catch { }
                    return n + " (" + target.ToLowerInvariant() + ")";
                }
                return n;
            };

            var here = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in new FilteredElementCollector(doc).OfClass(kindClasses[at]))
            {
                string k = keyFor(e);
                if (k.Length > 0) here.Add(k);
            }

            var toCopy = new List<ElementId>();
            var toCopyNames = new List<string>();
            var keptNames = new List<string>();
            foreach (var e in new FilteredElementCollector(source).OfClass(kindClasses[at]))
            {
                string k = keyFor(e);
                if (k.Length == 0) continue;
                if (filterText.Length > 0 && k.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (here.Contains(k)) { keptNames.Add(k); continue; }
                here.Add(k);
                toCopy.Add(e.Id);
                toCopyNames.Add(k);
            }

            skipped += keptNames.Count;
            int kindCopied = 0;
            int kindRefused = 0;
            var refusedNames = new List<string>();

            Func<List<ElementId>, int> copyAndReadBack = delegate (List<ElementId> ids)
            {
                int landed = 0;
                var arrived = ElementTransformUtils.CopyElements(source, ids, doc, Transform.Identity, copyOptions);
                if (arrived == null) return 0;
                foreach (var id in arrived)
                {
                    if (id == null || id == ElementId.InvalidElementId) continue;
                    var back = doc.GetElement(id);
                    if (back == null) continue;
                    copiedIds.Add(id);
                    if (kindClasses[at].IsInstanceOfType(back)) landed++;
                }
                return landed;
            };

            if (toCopy.Count > 0)
            {
                try
                {
                    kindCopied = copyAndReadBack(toCopy);
                }
                catch (Exception ex)
                {
                    if (ex.GetType().Name == "OperationCanceledException")
                    {
                        // Revit asked a question - "Duplicate Types" - and Heron
                        // declined it. The run is rolled back; trying again item by
                        // item would only meet the same dialog.
                        kindRefused = toCopy.Count;
                        refusedNames.Add("all " + toCopy.Count + " - Revit asked about a type name already here " +
                                         "and the question was declined, so the whole run is rolled back");
                        stopped = true;
                    }
                    else
                    {
                        // One bad type stops a whole copy. Try each on its own so the
                        // rest still arrive, and name the ones Revit refused.
                        for (int i = 0; i < toCopy.Count; i++)
                        {
                            var one = new List<ElementId>();
                            one.Add(toCopy[i]);
                            try
                            {
                                int got = copyAndReadBack(one);
                                if (got > 0) kindCopied += got;
                                else { kindRefused++; refusedNames.Add(toCopyNames[i] + " (nothing arrived)"); }
                            }
                            catch (Exception each)
                            {
                                kindRefused++;
                                refusedNames.Add(toCopyNames[i] + " (" + each.Message + ")");
                                if (each.GetType().Name == "OperationCanceledException") { stopped = true; break; }
                            }
                        }
                    }
                }

                if (!stopped && kindCopied + kindRefused < toCopy.Count)
                {
                    findings.Add(word + ": " + toCopy.Count + " picked, " + kindCopied + " read back here - fewer than picked.");
                }
            }

            copied += kindCopied;
            refusedCount += kindRefused;
            string line = word + ": " + kindCopied + " copied, " + keptNames.Count +
                          " skipped (already here, this project's kept), " + kindRefused + " refused";
            if (toCopy.Count == 0 && keptNames.Count == 0)
                line += " - none in '" + source.Title + "'" + (filterText.Length > 0 ? " matching '" + filterText + "'" : "");
            if (refusedNames.Count > 0) line += " [refused: " + string.Join("; ", refusedNames.ToArray()) + "]";
            lines.Add(line);
        }

        foreach (var e in elsewhere)
        {
            lines.Add(e);
            refusedCount++;
        }

        report = "From '" + source.Title + "' into '" + doc.Title + "': " + string.Join("  ||  ", lines.ToArray()) + ".";
        findings.Insert(0, report);
        findings.Add(copied + " copied, " + skipped + " skipped as already here, " + refusedCount + " refused.");
        if (skipped > 0)
            findings.Add("A name already here was KEPT AS IT IS - never overwritten. Revit's own Transfer Project " +
                         "Standards has Overwrite for that; this does not.");
        if (copied > 0)
            findings.Add("What the copied types use - materials, fill patterns, profiles - came with them as Revit's " +
                         "paste brings it. '" + source.Title + "' was only read.");
        if (stopped)
            findings.Add("STOPPED: Revit asked about a type name already here and Heron declined - NOTHING FROM THIS " +
                         "RUN IS KEPT. Bring the kinds over one at a time, or use Manage > Transfer Project Standards.");
    }
}

if (refused.Length > 0)
{
    report = refused;
    findings.Add(refused);
}
