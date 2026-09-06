// NOT STANDALONE. Assumes `doc`, `app`, `sourceDocumentTitle` and
// `nameContains` are in scope, and leaves `created`, `clashed`, `weakened` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The undo belongs to THIS
// document - the source project is only read, never written to.
//
// A LINE STYLE IS A SUBCATEGORY, NOT AN ELEMENT.
//
// There is nothing to CopyElements. Each style is built here with
// NewSubcategory and then given the source's weight, colour and pattern, which
// is why this fragment looks different from the other two transfer fragments
// despite doing the same kind of job.
//
// THE PATTERN IS A SEPARATE ELEMENT, AND IT IS WHERE THIS FAILS QUIETLY.
//
// A style whose pattern is missing here lands SOLID - present, correctly
// named, right colour, right weight, drawing the wrong thing, and discovered
// on an issued drawing. So the pattern is matched by NAME first, and brought
// over as an element when there is no match. If even that fails the style is
// still made, left solid, and named in `weakened` instead of `created`.
//
// AN EXISTING NAME IS NEVER OVERWRITTEN. Two projects can hold one name with
// different weights, and quietly replacing this project's standards with
// another's is worse than doing nothing.
//
// NO ElementId IS EVER TURNED INTO AN int - that value changed width at 2024.
// Ids are compared with ElementId.InvalidElementId and resolved with
// GetElement, which reads the same on every release.

var created = new List<string>();
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

var sourceLines = source == null ? null : Category.GetCategory(source, BuiltInCategory.OST_Lines);
var hereLines = Category.GetCategory(doc, BuiltInCategory.OST_Lines);

if (source == null)
{
    refused = "No open project matching '" + sourceDocumentTitle + "'. Both projects have to be open " +
              "in the same Revit - open now: " + string.Join(", ", openTitles.ToArray()) + ".";
}
else if (sourceLines == null || hereLines == null)
{
    refused = "The Lines category could not be read in one of the two projects.";
}
else
{
    // What is already here, by name. Read once: this decides skip-or-build for
    // every style, and re-reading it inside the loop would miss the ones this
    // fragment has just made.
    var hereByName = new Dictionary<string, int>();
    foreach (Category existing in hereLines.SubCategories)
    {
        string existingName = "";
        try { existingName = existing.Name ?? ""; } catch { }
        if (existingName.Length > 0 && !hereByName.ContainsKey(existingName))
            hereByName.Add(existingName, 1);
    }

    // Line patterns already in this project, by name. Matching by NAME is the
    // whole point: the same pattern in two projects has two different ids, and
    // an id carried across from the source means nothing here.
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

    var wanted = new List<Category>();
    foreach (Category candidate in sourceLines.SubCategories)
    {
        string candidateName = "";
        try { candidateName = candidate.Name ?? ""; } catch { }
        if (candidateName.Length == 0) continue;
        if (filterText.Length > 0 &&
            candidateName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0) continue;
        wanted.Add(candidate);
    }

    if (wanted.Count == 0)
    {
        refused = "No line styles in '" + source.Title + "'" +
                  (filterText.Length > 0 ? " matching '" + filterText + "'" : "") + ".";
    }
    else
    {
        var copyOptions = new CopyPasteOptions();

        foreach (var one in wanted)
        {
            string name = "";
            try { name = one.Name ?? ""; } catch { }

            if (hereByName.ContainsKey(name))
            {
                clashed.Add(name + " (already here, left as it was)");
                continue;
            }

            try
            {
                var made = doc.Settings.Categories.NewSubcategory(hereLines, name);
                if (made == null)
                {
                    clashed.Add(name + " (Revit made nothing)");
                    continue;
                }

                try
                {
                    int weight = one.GetLineWeight(GraphicsStyleType.Projection) ?? 1;
                    made.SetLineWeight(weight, GraphicsStyleType.Projection);
                }
                catch { }

                try
                {
                    var colour = one.LineColor;
                    if (colour != null && colour.IsValid)
                        made.LineColor = new Color(colour.Red, colour.Green, colour.Blue);
                }
                catch { }

                // The pattern. A built-in one - solid - has no LinePatternElement
                // behind it in the source, so GetElement returns null and there is
                // nothing to carry: solid here is already solid there.
                string patternName = "";
                bool patternWanted = false;
                try
                {
                    var sourcePatternId = one.GetLinePatternId(GraphicsStyleType.Projection);
                    if (sourcePatternId != null && sourcePatternId != ElementId.InvalidElementId)
                    {
                        var sourcePattern = source.GetElement(sourcePatternId) as LinePatternElement;
                        if (sourcePattern != null)
                        {
                            patternWanted = true;
                            try { patternName = sourcePattern.Name ?? ""; } catch { }

                            ElementId here = ElementId.InvalidElementId;
                            if (patternName.Length > 0 && patternsHere.ContainsKey(patternName))
                            {
                                here = patternsHere[patternName];
                            }
                            else
                            {
                                var toCopy = new List<ElementId>();
                                toCopy.Add(sourcePatternId);
                                var arrived = ElementTransformUtils.CopyElements(
                                    source, toCopy, doc, Transform.Identity, copyOptions);
                                foreach (var id in arrived)
                                {
                                    var landed = doc.GetElement(id) as LinePatternElement;
                                    if (landed == null) continue;
                                    here = landed.Id;
                                    // Remember it: several styles usually share one
                                    // pattern, and copying it again would make a
                                    // second pattern with the same name.
                                    if (patternName.Length > 0 && !patternsHere.ContainsKey(patternName))
                                        patternsHere.Add(patternName, here);
                                    break;
                                }
                            }

                            if (here != ElementId.InvalidElementId)
                            {
                                made.SetLinePatternId(here, GraphicsStyleType.Projection);
                                patternWanted = false;
                            }
                        }
                    }
                }
                catch { }

                if (patternWanted)
                {
                    // Made, named right, and drawing solid when it should not be.
                    // Reported apart from `created` because this is precisely what
                    // gets found on an issued sheet rather than here.
                    weakened.Add(name + " (pattern '" + patternName + "' could not be brought over - " +
                                 "the style is here and draws SOLID)");
                }
                else
                {
                    created.Add(name);
                }
            }
            catch (Exception)
            {
                clashed.Add(name);
            }
        }
    }
}
