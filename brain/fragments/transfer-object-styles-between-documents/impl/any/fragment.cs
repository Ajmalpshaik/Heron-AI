// NOT STANDALONE. Assumes `doc`, `app`, `sourceDocumentTitle` and
// `nameContains` are in scope, and leaves `changed`, `created`, `skipped`,
// `weakened` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The undo belongs to THIS
// document, and it matters more here than anywhere else in this library: this
// is the fragment that changes how the whole model draws, and one Ctrl+Z has
// to put all of it back.
//
// THIS ONE OVERWRITES. Every other transfer skips a name that is already here,
// because arriving twice is a mistake. Walls exists in both projects always,
// and the whole job is to change what Walls looks like here.
//
// SO NOTHING IS TOUCHED UNLESS IT DIFFERS, and everything touched is named
// with what moved. A transfer that restyles two hundred categories and reports
// a number is not something anybody can check.
//
// THE MATERIAL AND THE LINE PATTERN ARE THE TWO THAT GO QUIET. Both are
// separate elements matched BY NAME - the same thing in two projects has two
// different ids. A missing pattern is brought over. A missing MATERIAL is not:
// carrying a material drags its appearance asset and its own patterns behind
// it, which is TRANSFER_MATERIALS_BETWEEN_DOCUMENTS's job and not a side
// effect of styling a category. That category keeps the material it had and
// says so.
//
// NO ElementId IS TURNED INTO AN int, and no category is resolved through
// BuiltInCategory - that would mean an int, and that value changed width at
// 2024. Categories are matched by name: both documents are in the same Revit
// and therefore the same language.

var changed = new List<string>();
var created = new List<string>();
var skipped = new List<string>();
var weakened = new List<string>();
string refused = "";

string wantedTitle = (sourceDocumentTitle ?? "").Trim();
string filterText = (nameContains ?? "").Trim();

Document source = null;
var openTitles = new List<string>();
foreach (Document candidate in app.Documents)
{
    if (candidate == null || candidate.IsFamilyDocument) continue;
    string title = "";
    try { title = candidate.Title ?? ""; } catch { }
    openTitles.Add(title);
    if (candidate.Equals(doc)) continue;
    if (wantedTitle.Length > 0 &&
        title.IndexOf(wantedTitle, StringComparison.OrdinalIgnoreCase) >= 0)
        source = candidate;
}

if (source == null)
{
    refused = "No open project matching '" + sourceDocumentTitle + "'. Both projects have to be open " +
              "in the same Revit - open now: " + string.Join(", ", openTitles.ToArray()) + ".";
}
else
{
    // Line patterns here, by name. Rebuilt as patterns are brought over so one
    // pattern shared by twenty categories is copied once.
    var patternsHere = new Dictionary<string, ElementId>();
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(LinePatternElement)))
    {
        var pattern = element as LinePatternElement;
        if (pattern == null) continue;
        string patternName = "";
        try { patternName = pattern.Name ?? ""; } catch { }
        if (patternName.Length > 0 && !patternsHere.ContainsKey(patternName))
            patternsHere.Add(patternName, pattern.Id);
    }

    // Materials here, by name. Never created from here - see the header.
    var materialsHere = new Dictionary<string, Material>();
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Material)))
    {
        var material = element as Material;
        if (material == null) continue;
        string materialName = "";
        try { materialName = material.Name ?? ""; } catch { }
        if (materialName.Length > 0 && !materialsHere.ContainsKey(materialName))
            materialsHere.Add(materialName, material);
    }

    var copyOptions = new CopyPasteOptions();

    // Applies one source category's settings onto one here, and reports only
    // what actually moved. Returns the "what moved" text, or "" for no change.
    Func<Category, Category, string, string> applyTo = (from, onto, label) =>
    {
        var moved = new List<string>();

        try
        {
            int? fromWeight = from.GetLineWeight(GraphicsStyleType.Projection);
            int? ontoWeight = onto.GetLineWeight(GraphicsStyleType.Projection);
            if (fromWeight.HasValue && fromWeight != ontoWeight)
            {
                onto.SetLineWeight(fromWeight.Value, GraphicsStyleType.Projection);
                moved.Add("projection weight " +
                          (ontoWeight.HasValue ? ontoWeight.Value.ToString() : "-") +
                          " to " + fromWeight.Value.ToString());
            }
        }
        catch { }

        // Cut weight only where the category can be cut. Asking elsewhere
        // returns nothing useful on any release.
        try
        {
            bool cuttable = false;
            try { cuttable = from.IsCuttable && onto.IsCuttable; } catch { }
            if (cuttable)
            {
                int? fromCut = from.GetLineWeight(GraphicsStyleType.Cut);
                int? ontoCut = onto.GetLineWeight(GraphicsStyleType.Cut);
                if (fromCut.HasValue && fromCut != ontoCut)
                {
                    onto.SetLineWeight(fromCut.Value, GraphicsStyleType.Cut);
                    moved.Add("cut weight " +
                              (ontoCut.HasValue ? ontoCut.Value.ToString() : "-") +
                              " to " + fromCut.Value.ToString());
                }
            }
        }
        catch { }

        try
        {
            var fromColour = from.LineColor;
            var ontoColour = onto.LineColor;
            bool differs = fromColour != null && fromColour.IsValid &&
                           (ontoColour == null || !ontoColour.IsValid ||
                            ontoColour.Red != fromColour.Red ||
                            ontoColour.Green != fromColour.Green ||
                            ontoColour.Blue != fromColour.Blue);
            if (differs)
            {
                onto.LineColor = new Color(fromColour.Red, fromColour.Green, fromColour.Blue);
                moved.Add("colour");
            }
        }
        catch { }

        try
        {
            var fromPatternId = from.GetLinePatternId(GraphicsStyleType.Projection);
            var ontoPatternId = onto.GetLinePatternId(GraphicsStyleType.Projection);
            if (fromPatternId != null && fromPatternId != ElementId.InvalidElementId)
            {
                var fromPattern = source.GetElement(fromPatternId) as LinePatternElement;
                if (fromPattern != null)
                {
                    string patternName = "";
                    try { patternName = fromPattern.Name ?? ""; } catch { }

                    ElementId here = ElementId.InvalidElementId;
                    if (patternName.Length > 0 && patternsHere.ContainsKey(patternName))
                    {
                        here = patternsHere[patternName];
                    }
                    else
                    {
                        var toCopy = new List<ElementId>();
                        toCopy.Add(fromPatternId);
                        var arrived = ElementTransformUtils.CopyElements(
                            source, toCopy, doc, Transform.Identity, copyOptions);
                        foreach (var id in arrived)
                        {
                            var landedPattern = doc.GetElement(id) as LinePatternElement;
                            if (landedPattern == null) continue;
                            here = landedPattern.Id;
                            if (patternName.Length > 0 && !patternsHere.ContainsKey(patternName))
                                patternsHere.Add(patternName, here);
                            break;
                        }
                    }

                    if (here != ElementId.InvalidElementId && here != ontoPatternId)
                    {
                        onto.SetLinePatternId(here, GraphicsStyleType.Projection);
                        moved.Add("line pattern " + patternName);
                    }
                }
            }
        }
        catch { }

        try
        {
            var fromMaterial = from.Material;
            if (fromMaterial != null)
            {
                string materialName = "";
                try { materialName = fromMaterial.Name ?? ""; } catch { }
                if (materialName.Length > 0)
                {
                    if (materialsHere.ContainsKey(materialName))
                    {
                        var ontoMaterial = onto.Material;
                        string ontoMaterialName = "";
                        try { ontoMaterialName = ontoMaterial == null ? "" : (ontoMaterial.Name ?? ""); }
                        catch { }
                        if (ontoMaterialName != materialName)
                        {
                            onto.Material = materialsHere[materialName];
                            moved.Add("material " + materialName);
                        }
                    }
                    else
                    {
                        // Kept, not created. Carrying a material drags its
                        // appearance asset behind it and that is the materials
                        // transfer's job, run before this one.
                        weakened.Add(label + " (material '" + materialName + "' is not in this project - " +
                                     "the category keeps the material it had. Run " +
                                     "TRANSFER_MATERIALS_BETWEEN_DOCUMENTS first)");
                    }
                }
            }
        }
        catch { }

        return moved.Count == 0 ? "" : string.Join(", ", moved.ToArray());
    };

    // Destination categories by name, top level.
    var hereByName = new Dictionary<string, Category>();
    foreach (Category here in doc.Settings.Categories)
    {
        string hereName = "";
        try { hereName = here.Name ?? ""; } catch { }
        if (hereName.Length > 0 && !hereByName.ContainsKey(hereName))
            hereByName.Add(hereName, here);
    }

    int looked = 0;
    foreach (Category from in source.Settings.Categories)
    {
        string name = "";
        try { name = from.Name ?? ""; } catch { }
        if (name.Length == 0) continue;
        if (filterText.Length > 0 &&
            name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0) continue;
        looked++;

        if (!hereByName.ContainsKey(name))
        {
            // The set of top-level categories is Revit's, not a project's.
            skipped.Add(name + " (no such category in this project, and a top-level category cannot be made)");
            continue;
        }

        var onto = hereByName[name];
        string what = applyTo(from, onto, name);
        if (what.Length > 0) changed.Add(name + ": " + what);

        // Subcategories. A firm's own sub-style is made here if it is missing.
        Dictionary<string, Category> subsHere = new Dictionary<string, Category>();
        try
        {
            foreach (Category sub in onto.SubCategories)
            {
                string subName = "";
                try { subName = sub.Name ?? ""; } catch { }
                if (subName.Length > 0 && !subsHere.ContainsKey(subName)) subsHere.Add(subName, sub);
            }
        }
        catch { }

        try
        {
            foreach (Category fromSub in from.SubCategories)
            {
                string subName = "";
                try { subName = fromSub.Name ?? ""; } catch { }
                if (subName.Length == 0) continue;
                string label = name + " : " + subName;

                Category ontoSub = null;
                if (subsHere.ContainsKey(subName))
                {
                    ontoSub = subsHere[subName];
                }
                else
                {
                    bool canAdd = false;
                    try { canAdd = onto.CanAddSubcategory; } catch { }
                    if (!canAdd)
                    {
                        skipped.Add(label + " (this category takes no subcategories here)");
                        continue;
                    }
                    try
                    {
                        ontoSub = doc.Settings.Categories.NewSubcategory(onto, subName);
                        if (ontoSub != null) created.Add(label);
                    }
                    catch { ontoSub = null; }
                    if (ontoSub == null)
                    {
                        skipped.Add(label + " (Revit would not make it)");
                        continue;
                    }
                }

                string subWhat = applyTo(fromSub, ontoSub, label);
                if (subWhat.Length > 0) changed.Add(label + ": " + subWhat);
            }
        }
        catch { }
    }

    if (looked == 0)
    {
        refused = "No categories in '" + source.Title + "'" +
                  (filterText.Length > 0 ? " matching '" + filterText + "'" : "") + ".";
    }
}
