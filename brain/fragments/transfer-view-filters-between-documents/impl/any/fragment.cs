// NOT STANDALONE. Assumes `doc`, `app`, `sourceDocumentTitle` and
// `nameContains` are in scope, and leaves `copied`, `clashed`, `weakened` and
// `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The undo belongs to THIS
// document - the source project is only read, and never written to.
//
// A FILTER ARRIVES IN THE PROJECT, NOT ON A VIEW.
//
// ParameterFilterElement is project-wide. Transferring it makes it available
// and puts it on nothing; APPLY_VIEW_FILTER is the separate act that changes
// what anybody can see. Same shape as a transferred legend arriving empty.
//
// AND IT CAN ARRIVE WEAKER THAN IT LEFT.
//
// A filter's rules point at parameters. Built on a shared or project parameter
// the destination does not have, Revit brings what it can and the filter can
// land holding fewer categories than it started with - present, correctly
// named, and matching nothing. That is invisible until somebody applies it and
// sees no change, so every landed filter is read back and its categories
// counted against the source's. One that shrank is named in `weakened` instead
// of being reported as copied.
//
// CATEGORIES ARE WHAT IS COUNTED, ON PURPOSE. The rule-reading API changed
// spelling between releases; GetCategories() did not. A count of categories
// answers "did this land intact" without a version branch to maintain.
//
// A DUPLICATE NAME CANNOT BE HANDLED FROM HERE - the handler Revit wants is a
// CLASS, and a fragment is compiled inside a single method. Each filter is
// copied on its own, a clash names that filter, and the rest go through. Same
// limit as TRANSFER_VIEWS_BETWEEN_DOCUMENTS, for the same reason.

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
    // What is already here, by name. Revit refuses a clash rather than merging,
    // so knowing the names first turns a thrown exception into a sentence that
    // names the filter.
    var hereByName = new Dictionary<string, int>();
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ParameterFilterElement)))
    {
        var already = element as ParameterFilterElement;
        if (already == null) continue;
        string existingName = "";
        try { existingName = already.Name ?? ""; } catch { }
        if (existingName.Length > 0 && !hereByName.ContainsKey(existingName))
            hereByName.Add(existingName, 1);
    }

    var wanted = new List<ParameterFilterElement>();
    foreach (var element in new FilteredElementCollector(source).OfClass(typeof(ParameterFilterElement)))
    {
        var candidate = element as ParameterFilterElement;
        if (candidate == null) continue;
        string candidateName = "";
        try { candidateName = candidate.Name ?? ""; } catch { }
        if (filterText.Length > 0 &&
            candidateName.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0) continue;
        wanted.Add(candidate);
    }

    if (wanted.Count == 0)
    {
        refused = "No view filters in '" + source.Title + "'" +
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
                // Named before attempting rather than after failing: Revit
                // would throw, and a thrown exception here reads as a broken
                // fragment rather than a filter that is already present.
                clashed.Add(name + " (already here)");
                continue;
            }

            int sourceCategories = 0;
            try { sourceCategories = one.GetCategories().Count; } catch { }

            try
            {
                var toCopy = new List<ElementId>();
                toCopy.Add(one.Id);
                var arrived = ElementTransformUtils.CopyElements(
                    source, toCopy, doc, Transform.Identity, options);

                ParameterFilterElement landed = null;
                foreach (var id in arrived)
                {
                    landed = doc.GetElement(id) as ParameterFilterElement;
                    if (landed != null) break;
                }

                if (landed == null)
                {
                    clashed.Add(name + " (nothing came back)");
                    continue;
                }

                int landedCategories = 0;
                try { landedCategories = landed.GetCategories().Count; } catch { }

                if (landedCategories < sourceCategories)
                {
                    // Present, named right, and matching less than it did.
                    // Reported apart from `copied` because calling this a
                    // success is exactly what gets discovered weeks later, on
                    // somebody's sheet.
                    weakened.Add(name + " (" + landedCategories.ToString() + " of " +
                                 sourceCategories.ToString() + " categories - a rule may point at a " +
                                 "parameter this project does not have)");
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
