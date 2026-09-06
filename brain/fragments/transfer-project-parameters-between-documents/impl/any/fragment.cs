// NOT STANDALONE. Assumes `doc`, `app`, `sourceDocumentTitle`,
// `sharedParameterFile` and `nameContains` are in scope, and leaves `bound`,
// `clashed`, `skipped`, `regrouped` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). The undo belongs to THIS
// document - the source project is only read.
//
// ===========================================================================
// TWO CALLS MOVED ACROSS THE VERSION RANGE, SO NEITHER IS WRITTEN DOWN.
// ===========================================================================
//
//   BindingMap.Insert(definition, binding, GROUP)
//       took a group ENUM up to Revit 2021 and a ForgeTypeId from 2022, and
//       2027 removed the old enum outright. The method is chosen by its THIRD
//       PARAMETER'S TYPE and the group value is produced to match - the same
//       technique ADD_PROJECT_PARAMETER uses, for the same reason.
//
//   Reading the SOURCE's group is that break in reverse.
//       Definition.ParameterGroup is on 2020 and gone by 2027;
//       Definition.GetGroupTypeId has never existed on 2020. A matched pair
//       where neither spelling works at both ends, so both are reached by
//       name and neither appears as a call. Where neither answers, the
//       parameter is filed under Data and SAID SO in `regrouped`.
//
// A SHARED PARAMETER IS MATCHED BY GUID, NEVER BY NAME. Two files can hold
// "AJ_Status" as two different parameters, and binding the wrong one produces
// a project that looks correct and shares nothing with anybody.
//
// A NON-SHARED PROJECT PARAMETER CANNOT BE CARRIED. It exists only inside its
// own file and the API cannot recreate one elsewhere. Those are NAMED, because
// a transfer that quietly leaves out three of ten is worse than one that
// refuses.

var bound = new List<string>();
var clashed = new List<string>();
var skipped = new List<string>();
var regrouped = new List<string>();
string refused = "";

string wantedTitle = (sourceDocumentTitle ?? "").Trim();
string filterText = (nameContains ?? "").Trim();
var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
string dbNamespace = typeof(Document).Namespace;

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

// The binding call, newest signature first.
var insertMethod = typeof(BindingMap).GetMethods()
    .Where(m => m.Name == "Insert" && m.GetParameters().Length == 3
             && m.GetParameters()[0].ParameterType == typeof(Definition))
    .OrderBy(m => m.GetParameters()[2].ParameterType.Name == "ForgeTypeId" ? 0 : 1)
    .FirstOrDefault();

// The Data group, in whichever world this Revit lives in.
Func<System.Type, object> dataGroup = groupType =>
{
    if (groupType.Name != "ForgeTypeId") return Enum.Parse(groupType, "PG_DATA");
    var owner = revitAssembly.GetType(dbNamespace + ".GroupTypeId");
    var property = owner == null ? null : owner.GetProperty("Data", flags);
    if (property == null) throw new InvalidOperationException("This Revit has no Data parameter group");
    return property.GetValue(null);
};

// The source definition's own group, if this release will say. Returns null
// when neither spelling answers - which is a real outcome, not an error.
Func<Definition, System.Type, object> groupOf = (definition, groupType) =>
{
    try
    {
        if (groupType.Name == "ForgeTypeId")
        {
            var getter = definition.GetType().GetMethod("GetGroupTypeId", System.Type.EmptyTypes);
            if (getter != null) return getter.Invoke(definition, null);
            return null;
        }
        var property = definition.GetType().GetProperty("ParameterGroup");
        if (property != null) return property.GetValue(definition, null);
        return null;
    }
    catch { return null; }
};

DefinitionFile definitionFile = null;
if (source != null)
{
    try
    {
        if (!string.IsNullOrEmpty(sharedParameterFile))
            app.SharedParametersFilename = sharedParameterFile;
        definitionFile = app.OpenSharedParameterFile();
    }
    catch { definitionFile = null; }
}

if (source == null)
{
    refused = "No open project matching '" + sourceDocumentTitle + "'. Both projects have to be open " +
              "in the same Revit - open now: " + string.Join(", ", openTitles.ToArray()) + ".";
}
else if (insertMethod == null)
{
    refused = "This Revit has no BindingMap.Insert(definition, binding, group) - nothing done.";
}
else if (definitionFile == null)
{
    refused = "The shared parameter file '" + sharedParameterFile + "' could not be opened. A shared " +
              "parameter's definition lives in that file, not in the project, so nothing can be bound " +
              "without it.";
}
else
{
    var groupType = insertMethod.GetParameters()[2].ParameterType;

    // Every shared definition in the file, by GUID. Matched on GUID and never
    // on name: two files can hold one name as two different parameters.
    var inFile = new Dictionary<Guid, ExternalDefinition>();
    foreach (DefinitionGroup fileGroup in definitionFile.Groups)
    {
        foreach (Definition entry in fileGroup.Definitions)
        {
            var external = entry as ExternalDefinition;
            if (external == null) continue;
            if (!inFile.ContainsKey(external.GUID)) inFile.Add(external.GUID, external);
        }
    }

    // Which of the source's parameters are shared, by name -> GUID.
    var sharedInSource = new Dictionary<string, Guid>();
    foreach (var element in new FilteredElementCollector(source).OfClass(typeof(SharedParameterElement)))
    {
        var shared = element as SharedParameterElement;
        if (shared == null) continue;
        string sharedName = "";
        try { sharedName = shared.Name ?? ""; } catch { }
        if (sharedName.Length > 0 && !sharedInSource.ContainsKey(sharedName))
            sharedInSource.Add(sharedName, shared.GuidValue);
    }

    // What is already bound HERE, by name. Read once, before anything is
    // inserted, so this fragment does not trip over its own work.
    var boundHere = new Dictionary<string, int>();
    var hereIterator = doc.ParameterBindings.ForwardIterator();
    while (hereIterator.MoveNext())
    {
        var definition = hereIterator.Key;
        if (definition == null) continue;
        string name = "";
        try { name = definition.Name ?? ""; } catch { }
        if (name.Length > 0 && !boundHere.ContainsKey(name)) boundHere.Add(name, 1);
    }

    int looked = 0;
    var iterator = source.ParameterBindings.ForwardIterator();
    while (iterator.MoveNext())
    {
        var definition = iterator.Key;
        if (definition == null) continue;

        string name = "";
        try { name = definition.Name ?? ""; } catch { }
        if (name.Length == 0) continue;
        if (filterText.Length > 0 &&
            name.IndexOf(filterText, StringComparison.OrdinalIgnoreCase) < 0) continue;
        looked++;

        if (boundHere.ContainsKey(name))
        {
            clashed.Add(name + " (already bound here, left alone)");
            continue;
        }

        if (!sharedInSource.ContainsKey(name))
        {
            skipped.Add(name + " (a NON-SHARED project parameter - it exists only inside that file " +
                        "and the API cannot recreate one here)");
            continue;
        }

        var guid = sharedInSource[name];
        if (!inFile.ContainsKey(guid))
        {
            skipped.Add(name + " (not in the shared parameter file given - its definition lives there, " +
                        "and matching by name instead could bind a different parameter)");
            continue;
        }

        var sourceBinding = iterator.Current as ElementBinding;
        if (sourceBinding == null)
        {
            skipped.Add(name + " (its binding could not be read)");
            continue;
        }

        var categorySet = app.Create.NewCategorySet();
        try
        {
            foreach (Category category in sourceBinding.Categories)
                if (category != null) categorySet.Insert(category);
        }
        catch { }

        if (categorySet.IsEmpty)
        {
            skipped.Add(name + " (bound to no category this project has)");
            continue;
        }

        bool instance = sourceBinding is InstanceBinding;
        var binding = instance
            ? (ElementBinding)app.Create.NewInstanceBinding(categorySet)
            : (ElementBinding)app.Create.NewTypeBinding(categorySet);

        object group = groupOf(definition, groupType);
        bool fellBack = false;
        if (group == null)
        {
            group = dataGroup(groupType);
            fellBack = true;
        }

        try
        {
            var ok = insertMethod.Invoke(doc.ParameterBindings,
                                         new object[] { inFile[guid], binding, group });
            // Insert returning true is not proof. Read it back out of the
            // document - the same rule ADD_PROJECT_PARAMETER records.
            bool landed = false;
            var check = doc.ParameterBindings.ForwardIterator();
            while (check.MoveNext())
            {
                var got = check.Key;
                if (got == null) continue;
                string gotName = "";
                try { gotName = got.Name ?? ""; } catch { }
                if (gotName == name) { landed = true; break; }
            }

            if (!landed)
            {
                skipped.Add(name + " (Revit reported " + (ok == null ? "nothing" : ok.ToString()) +
                            " and the binding is not in the document)");
            }
            else if (fellBack)
            {
                regrouped.Add(name + " (filed under Data - this release would not say which group it " +
                              "was in over there)");
            }
            else
            {
                bound.Add(name);
            }
        }
        catch (Exception)
        {
            skipped.Add(name + " (Revit refused the binding)");
        }
    }

    if (looked == 0)
    {
        refused = "No project parameters in '" + source.Title + "'" +
                  (filterText.Length > 0 ? " matching '" + filterText + "'" : "") + ".";
    }
}
