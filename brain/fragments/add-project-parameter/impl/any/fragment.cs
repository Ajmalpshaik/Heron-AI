// NOT STANDALONE. Assumes `doc`, `app`, `sharedParameterFile`, `groupName`,
// `parameterName`, `categories` and `instanceBinding` are in scope; leaves
// `findings`, `bound`, `boundCategories`, `categoriesNotBound` and
// `sharedParameterFileRestored` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ===========================================================================
// TWO CALLS HERE MOVED ACROSS THE VERSION RANGE, SO NEITHER IS WRITTEN DOWN.
// ===========================================================================
//
//   ExternalDefinitionCreationOptions(name, SPEC)
//       took a parameter-type ENUM up to Revit 2021 and a ForgeTypeId from
//       2022. The reference assemblies show its `Type` property on 2020 and
//       GONE by 2024. The constructor is chosen by its OWN second parameter
//       type - not by asking whether ForgeTypeId exists, because 2021 ships
//       ForgeTypeId while this constructor there still takes the old enum.
//
//   BindingMap.Insert(definition, binding, GROUP)
//       the same move, and Revit 2027 removed the old group enum outright.
//       The method is chosen by its third parameter type and the group value
//       looked up by name.
//
// Neither name can appear in source that must compile on 2020 AND 2027, so
// both are resolved at run time. That is not cleverness for its own sake - it
// is the only shape that spans the range.
//
// A SHARED PARAMETER FILE IS NEVER INVENTED. The path is asked for. A shared
// parameter written into a temporary folder nobody knows about cannot be
// re-used on the next project or matched by a colleague, which is the entire
// point of it being shared.
//
// AN EXISTING BINDING IS EXTENDED, NEVER REPLACED. ReInsert overwrites: called
// with only today's categories it silently strips the parameter off every
// category it was bound to before, and the values go with them. So the current
// categories are read first and merged in.
//
// AND THE RESULT IS READ BACK OUT OF THE DOCUMENT. Insert returning true is
// not proof that the binding is there.
//
// ===========================================================================
// VERSION 2 (FRAGMENT-ISSUES 5b-316).
// ===========================================================================
//
// THE APPLICATION'S SHARED PARAMETER FILE IS PUT BACK. Version 1 pointed
// Application.SharedParametersFilename at the requested path and left it
// there. That setting belongs to the Revit session, not to the document: no
// transaction holds it, so Undo cannot reverse it, and the modeller's Manage >
// Shared Parameters stayed pointed at this path in every project afterwards.
// The value in force is read first and restored in a `finally`, whatever
// happened, and the restore is READ BACK - `sharedParameterFileRestored` is
// true only when the setting reads as it did before.
//
// A CATEGORY REVIT WILL NOT BIND IS NAMED, NOT DROPPED. Version 1 put every
// category asked for into the set and reported only what came back, so a
// category that cannot carry a project parameter vanished from the answer.
// Category.AllowsBoundParameters is asked before a category is added, and
// after the binding each category asked for is looked for in what the
// document now holds. Every one missing, for either reason, is in
// `categoriesNotBound` with the reason.

var findings = new List<string>();
var bound = false;
var boundCategories = new List<string>();
var categoriesNotBound = new List<string>();
var sharedParameterFileRestored = false;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;

// The namespace is taken FROM A TYPE ALREADY IN SCOPE rather than written out.
// Two reasons and both matter: a fragment that spells the API namespace in a
// string has hard-coded something it does not own, and Heron's own structure
// check refuses it outside the adapter layer - correctly, because that is how
// the boundary stays real rather than being a rule people remember.
var dbNamespace = typeof(Document).Namespace;

// The constructor, picked by its own signature.
var optionsCtor = typeof(ExternalDefinitionCreationOptions).GetConstructors()
    .Where(c => c.GetParameters().Length == 2 && c.GetParameters()[0].ParameterType == typeof(string))
    .OrderBy(c => c.GetParameters()[1].ParameterType.Name == "ForgeTypeId" ? 0 : 1)
    .FirstOrDefault();

// The binding call, picked the same way. Newest first.
Func<string, System.Reflection.MethodInfo> bindMethod = which => typeof(BindingMap).GetMethods()
    .Where(m => m.Name == which && m.GetParameters().Length == 3
             && m.GetParameters()[0].ParameterType == typeof(Definition))
    .OrderBy(m => m.GetParameters()[2].ParameterType.Name == "ForgeTypeId" ? 0 : 1)
    .FirstOrDefault();

// "Text" is the leaf name in both worlds. The newer one files it under String.
Func<System.Type, object> textSpec = specType =>
{
    if (specType.Name != "ForgeTypeId") return Enum.Parse(specType, "Text");
    var names = new[] { dbNamespace + ".SpecTypeId+String", dbNamespace + ".SpecTypeId" };
    foreach (var name in names)
    {
        var owner = revitAssembly.GetType(name);
        var property = owner == null ? null : owner.GetProperty("Text", flags);
        if (property != null) return property.GetValue(null);
    }
    throw new InvalidOperationException("This Revit has no Text spec");
};

// PG_DATA on the old enum is GroupTypeId.Data on the new one - the same place
// in the properties palette, named twice.
Func<System.Type, object> dataGroup = groupType =>
{
    if (groupType.Name != "ForgeTypeId") return Enum.Parse(groupType, "PG_DATA");
    var owner = revitAssembly.GetType(dbNamespace + ".GroupTypeId");
    var property = owner == null ? null : owner.GetProperty("Data", flags);
    if (property == null) throw new InvalidOperationException("This Revit has no Data parameter group");
    return property.GetValue(null);
};

if (optionsCtor == null)
{
    findings.Add("This Revit has no ExternalDefinitionCreationOptions(name, spec) constructor - nothing done");
}
else if (categories == null || categories.Count == 0)
{
    findings.Add("No categories given, so there is nothing to bind the parameter to - nothing done");
}
else if (!categories.Any(each => each != null && each.AllowsBoundParameters))
{
    foreach (var each in categories)
        if (each != null)
            categoriesNotBound.Add(each.Name +" (Revit does not let a project parameter be bound to it)");
    findings.Add("None of the categories asked for can carry a project parameter, so nothing was done "
        + "and the shared parameter file was not touched: " + string.Join(", ", categoriesNotBound));
    // Nothing was changed, so there is nothing to put back.
    sharedParameterFileRestored = true;
}
else
{
    // Read BEFORE anything is changed, so it can be put back whatever happens.
    string previousSharedFile = null;
    var previousRead = false;
    try { previousSharedFile = app.SharedParametersFilename; previousRead = true; }
    catch (Exception ex)
    {
        findings.Add("The shared parameter file Revit had set could not be read (" + ex.Message
            + "), so it could not be put back and nothing was done");
    }

    if (previousRead)
    {
        try
        {
            // Create the file if it is not there. The header is what makes Revit
            // recognise it as a shared parameter file rather than a text file.
            if (!System.IO.File.Exists(sharedParameterFile))
            {
                System.IO.File.WriteAllText(sharedParameterFile,
                    "# This is a Revit shared parameter file.\n"
                    + "*META\tVERSION\tMINVERSION\n"
                    + "META\t2\t1\n"
                    + "*GROUP\tID\tNAME\n"
                    + "*PARAM\tGUID\tNAME\tDATATYPE\tDATACATEGORY\tGROUP\tVISIBLE\tDESCRIPTION\tUSERMODIFIABLE\n");
                findings.Add(string.Format("Created a new shared parameter file at {0}", sharedParameterFile));
            }
            app.SharedParametersFilename = sharedParameterFile;

            var file = app.OpenSharedParameterFile();
            if (file == null)
            {
                findings.Add(string.Format("Revit could not open '{0}' as a shared parameter file", sharedParameterFile));
            }
            else
            {
                var group = file.Groups.get_Item(groupName) ?? file.Groups.Create(groupName);

                // Reuse rather than duplicate. Two definitions of one name in one
                // group means two different GUIDs, and a colleague's model then
                // has a parameter that looks the same and is not.
                var definition = group.Definitions.get_Item(parameterName) as ExternalDefinition;
                var reusedDefinition = definition != null;
                if (definition == null)
                {
                    var options = (ExternalDefinitionCreationOptions)optionsCtor.Invoke(
                        new object[] { parameterName, textSpec(optionsCtor.GetParameters()[1].ParameterType) });
                    definition = group.Definitions.Create(options) as ExternalDefinition;
                }

                if (definition == null)
                {
                    findings.Add(string.Format("Could not create a definition for '{0}'", parameterName));
                }
                else
                {
                    var categorySet = app.Create.NewCategorySet();
                    var asked = new List<Category>();
                    foreach (var category in categories)
                    {
                        if (category == null) continue;
                        // Asked BEFORE it goes in. A category that cannot carry a
                        // project parameter is named, never quietly left out.
                        if (!category.AllowsBoundParameters)
                        {
                            categoriesNotBound.Add(category.Name
                                + " (Revit does not let a project parameter be bound to it)");
                            continue;
                        }
                        categorySet.Insert(category);
                        asked.Add(category);
                    }

                    // MERGE with whatever is already bound. ReInsert replaces, and
                    // replacing with today's list alone is how a category loses a
                    // parameter and every value in it.
                    var already = new List<string>();
                    var existing = doc.ParameterBindings.get_Item(definition) as ElementBinding;
                    if (existing != null && existing.Categories != null)
                    {
                        foreach (Category category in existing.Categories)
                        {
                            if (category == null) continue;
                            if (!categorySet.Contains(category)) categorySet.Insert(category);
                            already.Add(category.Name);
                        }
                    }

                    var binding = instanceBinding
                        ? (ElementBinding)app.Create.NewInstanceBinding(categorySet)
                        : (ElementBinding)app.Create.NewTypeBinding(categorySet);

                    var insert = bindMethod(existing == null ? "Insert" : "ReInsert");
                    if (insert == null)
                    {
                        findings.Add("This Revit has no BindingMap.Insert(definition, binding, group)");
                    }
                    else
                    {
                        insert.Invoke(doc.ParameterBindings, new object[]
                        {
                            definition, binding, dataGroup(insert.GetParameters()[2].ParameterType)
                        });

                        // READ IT BACK OUT OF THE DOCUMENT. A returned true is not
                        // a binding.
                        var after = doc.ParameterBindings.get_Item(definition) as ElementBinding;
                        if (after != null && after.Categories != null)
                        {
                            foreach (Category category in after.Categories)
                                if (category != null) boundCategories.Add(category.Name);
                        }

                        // EVERY CATEGORY ASKED FOR IS LOOKED FOR IN WHAT IS NOW BOUND.
                        // One Revit accepted into the set and then left out of the
                        // binding is named here rather than missing from the list.
                        foreach (var category in asked)
                        {
                            var there = after != null && after.Categories != null
                                && after.Categories.Contains(category);
                            if (!there)
                                categoriesNotBound.Add(category.Name
                                    + " (asked for, and NOT in the binding Revit holds afterwards)");
                        }

                        bound = boundCategories.Count > 0;
                        findings.Add(string.Format(
                            "'{0}' is {1} to {2} categor{3}: {4}",
                            parameterName,
                            bound ? (instanceBinding ? "bound as an INSTANCE parameter" : "bound as a TYPE parameter")
                                  : "NOT bound - the write was accepted and the document has no binding for it",
                            boundCategories.Count, boundCategories.Count == 1 ? "y" : "ies",
                            string.Join(", ", boundCategories)));

                        if (reusedDefinition)
                            findings.Add(string.Format("The definition already existed in group '{0}' and was "
                                + "reused - no second parameter of that name was made", groupName));
                        if (already.Count > 0)
                            findings.Add(string.Format("It was already bound to {0} - those were KEPT rather "
                                + "than replaced", string.Join(", ", already)));
                        if (categoriesNotBound.Count > 0)
                            findings.Add(string.Format("NOT bound to {0} categor{1}: {2}",
                                categoriesNotBound.Count, categoriesNotBound.Count == 1 ? "y" : "ies",
                                string.Join(", ", categoriesNotBound)));
                    }
                }
            }
        }
        catch (Exception ex)
        {
            findings.Add(string.Format("Adding the project parameter failed: {0}", ex.Message));
        }
        finally
        {
            // PUT THE SESSION'S SETTING BACK, and read it to be sure. Undo cannot
            // do this - the setting is outside every transaction (5b-316).
            try
            {
                app.SharedParametersFilename = previousSharedFile ?? "";
                var now = app.SharedParametersFilename ?? "";
                sharedParameterFileRestored = string.Equals(now, previousSharedFile ?? "",
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                findings.Add("Putting back the shared parameter file Revit had set failed: " + ex.Message);
            }

            findings.Add(sharedParameterFileRestored
                ? string.Format("Revit's shared parameter file setting was put back to {0}",
                    string.IsNullOrEmpty(previousSharedFile) ? "none, as it was" : "'" + previousSharedFile + "'")
                : string.Format("Revit's shared parameter file setting could NOT be put back - Manage > Shared "
                    + "Parameters may now point at '{0}'. It was '{1}'; set it back there by hand",
                    sharedParameterFile, previousSharedFile ?? ""));
        }
    }
}
