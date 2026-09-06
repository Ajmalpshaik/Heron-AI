// NOT STANDALONE. Assumes `doc`, `app`, `sourceDocumentTitle` and
// `nameContains` are in scope, and leaves `copied`, `clashed`, `weakened` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The undo belongs to THIS
// document - the source project is only read.
//
// A MATERIAL IS AN ELEMENT, so this is a document-to-document copy. The work
// is not the copy; it is checking what came with it.
//
// A material is a thin thing pointing at fat ones: an appearance asset, a
// thermal asset, a structural asset, and up to four fill patterns. Each is a
// separate element. Revit usually brings them; usually is not good enough, so
// every landed material is compared against its source property by property
// and anything missing is named in `weakened`.
//
// THE APPEARANCE ASSET IS THE ONE THAT FOOLS PEOPLE. Without it the material
// keeps its shading colour, so it looks right in a shaded view, in the
// material browser, and in every list. It is wrong only in a rendered or
// realistic view - opened last, often by somebody else.
//
// NO ElementId IS TURNED INTO AN int - that value changed width at 2024.

var copied = new List<string>();
var clashed = new List<string>();
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
    // What is here already, by name. Read once: re-reading inside the loop
    // would start matching the materials this fragment has just brought over.
    var hereByName = new Dictionary<string, int>();
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(Material)))
    {
        var already = element as Material;
        if (already == null) continue;
        string existingName = "";
        try { existingName = already.Name ?? ""; } catch { }
        if (existingName.Length > 0 && !hereByName.ContainsKey(existingName))
            hereByName.Add(existingName, 1);
    }

    var wanted = new List<Material>();
    foreach (var element in new FilteredElementCollector(source).OfClass(typeof(Material)))
    {
        var candidate = element as Material;
        if (candidate == null) continue;
        string candidateName = "";
        try { candidateName = candidate.Name ?? ""; } catch { }
        if (candidateName.Length == 0) continue;
        if (filterText.Length > 0 &&
            candidateName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0) continue;
        wanted.Add(candidate);
    }

    if (wanted.Count == 0)
    {
        refused = "No materials in '" + source.Title + "'" +
                  (filterText.Length > 0 ? " matching '" + filterText + "'" : "") + ".";
    }
    else
    {
        var options = new CopyPasteOptions();

        foreach (var one in wanted)
        {
            string name = "";
            try { name = one.Name ?? ""; } catch { }

            if (hereByName.ContainsKey(name))
            {
                clashed.Add(name + " (already here, left as it was)");
                continue;
            }

            // What the source has, read BEFORE the copy. Comparing against the
            // source afterwards is the only way to tell "this material has no
            // appearance asset" from "its appearance asset did not come".
            bool hadAppearance = false;
            bool hadThermal = false;
            bool hadStructural = false;
            bool hadSurfacePattern = false;
            bool hadCutPattern = false;
            try { hadAppearance = one.AppearanceAssetId != ElementId.InvalidElementId; } catch { }
            try { hadThermal = one.ThermalAssetId != ElementId.InvalidElementId; } catch { }
            try { hadStructural = one.StructuralAssetId != ElementId.InvalidElementId; } catch { }
            try { hadSurfacePattern = one.SurfaceForegroundPatternId != ElementId.InvalidElementId; } catch { }
            try { hadCutPattern = one.CutForegroundPatternId != ElementId.InvalidElementId; } catch { }

            try
            {
                var toCopy = new List<ElementId>();
                toCopy.Add(one.Id);
                var arrived = ElementTransformUtils.CopyElements(
                    source, toCopy, doc, Transform.Identity, options);

                Material landed = null;
                foreach (var id in arrived)
                {
                    landed = doc.GetElement(id) as Material;
                    if (landed != null) break;
                }

                if (landed == null)
                {
                    clashed.Add(name + " (nothing came back)");
                    continue;
                }

                var missing = new List<string>();
                if (hadAppearance)
                {
                    bool got = false;
                    try { got = landed.AppearanceAssetId != ElementId.InvalidElementId; } catch { }
                    // Named first and named plainly: this is the one that looks
                    // correct everywhere except a rendered view.
                    if (!got) missing.Add("appearance asset - it will look right in a shaded view and " +
                                          "wrong in a rendered one");
                }
                if (hadSurfacePattern)
                {
                    bool got = false;
                    try { got = landed.SurfaceForegroundPatternId != ElementId.InvalidElementId; } catch { }
                    if (!got) missing.Add("surface pattern");
                }
                if (hadCutPattern)
                {
                    bool got = false;
                    try { got = landed.CutForegroundPatternId != ElementId.InvalidElementId; } catch { }
                    if (!got) missing.Add("cut pattern");
                }
                if (hadThermal)
                {
                    bool got = false;
                    try { got = landed.ThermalAssetId != ElementId.InvalidElementId; } catch { }
                    if (!got) missing.Add("thermal asset");
                }
                if (hadStructural)
                {
                    bool got = false;
                    try { got = landed.StructuralAssetId != ElementId.InvalidElementId; } catch { }
                    if (!got) missing.Add("structural asset");
                }

                if (missing.Count > 0)
                {
                    weakened.Add(name + " (arrived without its " +
                                 string.Join(", ", missing.ToArray()) + ")");
                }
                else
                {
                    copied.Add(name);
                }
            }
            catch (Exception)
            {
                clashed.Add(name);
            }
        }
    }
}
