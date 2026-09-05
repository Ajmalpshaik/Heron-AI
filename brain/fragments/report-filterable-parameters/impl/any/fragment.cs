// NOT STANDALONE. Assumes `doc` and `categoryIds` are in scope, and leaves
// `commonParameters`, `perCategory`, `filterableCategories` and
// `unfilterableCategories` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// TWO QUESTIONS, AND BOTH ARE ASKED OF REVIT RATHER THAN INFERRED.
//
// Which categories may be filtered, and which parameters those categories
// allow IN COMMON. The second is the one that surprises people: the legal set
// SHRINKS as categories are added, because it is the intersection. Two
// categories that each allow a parameter can allow nothing together, and the
// only way to see which one narrowed it is to ask them separately too.
//
// THE CATEGORY TEST IS ASKED TWICE, DELIBERATELY.
//
// Membership of the filterable list and Revit's own removal call can disagree
// at the edges, and the create call follows the stricter of the two. Asking
// both and keeping what survives is the answer that matches what actually
// happens when the filter is built.
//
// IDS ALONE ARE NOT AN ANSWER, AND THE ID IS NEVER READ AS A NUMBER.
//
// A list of parameter ids is not something anybody can choose from, so each is
// resolved to a name. A project or shared parameter is a real element in this
// document. A built-in one is not - and the obvious way to name it, converting
// its id back to a number and casting that to the enumeration, is exactly the
// read that broke when the id went 64-bit and the integer accessor was
// removed. So the conversion is run the OTHER WAY: the enumeration is walked
// once, each member turned INTO an id, and only the ones being asked about are
// kept. That direction is stable on every release, and it is a few thousand
// cheap constructions once, not per parameter.

var commonParameters = new Dictionary<ElementId, string>();
var perCategory = new Dictionary<ElementId, int>();
var filterableCategories = new List<ElementId>();
var unfilterableCategories = new List<ElementId>();

ICollection<ElementId> allFilterable = null;
try { allFilterable = ParameterFilterUtilities.GetAllFilterableCategories(); } catch { }

foreach (var categoryId in categoryIds)
{
    if (categoryId == null) continue;
    bool listed = allFilterable == null || allFilterable.Contains(categoryId);
    if (listed) filterableCategories.Add(categoryId);
    else unfilterableCategories.Add(categoryId);
}

// Revit's own removal call, over the ones that passed the membership test. It
// is the answer the create call agrees with, so anything it drops is moved to
// the rejected side rather than being argued with.
try
{
    var candidates = new List<ElementId>(filterableCategories);
    var accepted = ParameterFilterUtilities.RemoveUnfilterableCategories(candidates);
    if (accepted != null)
    {
        var kept = new List<ElementId>();
        foreach (var categoryId in filterableCategories)
        {
            if (accepted.Contains(categoryId)) kept.Add(categoryId);
            else unfilterableCategories.Add(categoryId);
        }
        filterableCategories = kept;
    }
}
catch { }

// Each category on its own, so the one narrowing the common set is visible.
foreach (var categoryId in filterableCategories)
{
    try
    {
        var single = new List<ElementId>();
        single.Add(categoryId);
        var allowed = ParameterFilterUtilities.GetFilterableParametersInCommon(doc, single);
        perCategory[categoryId] = allowed == null ? 0 : allowed.Count;
    }
    catch { perCategory[categoryId] = 0; }
}

// And the intersection, which is what a filter over the whole set can use.
if (filterableCategories.Count > 0)
{
    ICollection<ElementId> common = null;
    try { common = ParameterFilterUtilities.GetFilterableParametersInCommon(doc, filterableCategories); }
    catch { }

    if (common != null)
    {
        // First pass: whatever this document can name directly.
        var unnamed = new List<ElementId>();
        foreach (var parameterId in common)
        {
            if (parameterId == null) continue;
            string name = null;
            try
            {
                var definition = doc.GetElement(parameterId) as ParameterElement;
                if (definition != null && definition.GetDefinition() != null)
                    name = definition.GetDefinition().Name;
            }
            catch { }

            if (name != null) commonParameters[parameterId] = name;
            else unnamed.Add(parameterId);
        }

        // Second pass, for the built-ins: walk the enumeration and turn each
        // member into an id, rather than turning an id into a number.
        if (unnamed.Count > 0)
        {
            var wanted = new HashSet<ElementId>(unnamed);
            foreach (BuiltInParameter builtIn in Enum.GetValues(typeof(BuiltInParameter)))
            {
                if (wanted.Count == 0) break;
                ElementId candidate = null;
                try { candidate = new ElementId(builtIn); } catch { continue; }
                if (candidate == null || !wanted.Contains(candidate)) continue;

                string label = builtIn.ToString();
                // The human label where Revit has one - some members have none
                // and throw rather than returning the enum name.
                try { label = LabelUtils.GetLabelFor(builtIn); } catch { }
                commonParameters[candidate] = label;
                wanted.Remove(candidate);
            }

            // Anything the enumeration did not account for is still reported,
            // under its id. A silently dropped parameter would read as one the
            // filter cannot use, which is the opposite of the truth.
            foreach (var parameterId in unnamed)
                if (!commonParameters.ContainsKey(parameterId))
                    commonParameters[parameterId] = parameterId.ToString();
        }
    }
}
