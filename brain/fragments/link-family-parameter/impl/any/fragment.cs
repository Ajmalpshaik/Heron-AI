// NOT STANDALONE. Assumes `doc`, `category`, `system` and `links` are in scope;
// leaves `linkReport`, `changed`, `alreadyLinked`, `matched`, `notAFamily`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE "ASSOCIATE FAMILY PARAMETER" BUTTON, FOR ELEMENTS ALREADY IN THE FAMILY.
// ADD_FAMILY_CONNECTOR ties the size of a connector it has just made; this
// links or unlinks any parameter Revit lets be linked, on elements that were
// there before the call - a connector's Flow, a solid's Material or Visible.
// Revit decides what can be linked (CanElementParameterBeAssociated), so the
// family's own answer is what is offered, never a list typed here.
//
// THE ELEMENTS ARE NAMED, NEVER GUESSED (D-33). A category is matched to the
// element's Revit category or to what the Properties palette calls it - "Duct
// Connector", "Extrusion", a nested family's name - with case and spaces
// ignored and a plural read as its singular. A system narrows the choice to
// connectors of that system classification. A selector that takes nothing is
// refused with what the family has.
//
// A LINK REVIT WOULD REFUSE IS REFUSED HERE FIRST, BY NAME. Revit's own rule is
// that the two parameters hold the same kind of value - "the parameter types of
// these two input parameter should be same" (RevitAPI.xml) - so a mismatch is
// refused naming both parameters and both kinds before anything changes. The
// kind moved at 2022, Definition.ParameterType before and GetDataType() from,
// so both are reached by reflection; a kind that cannot be read on either side
// is left to Revit to judge rather than refused on a guess.
//
// NOTHING IS CHANGED UNTIL EVERYTHING HAS BEEN CHECKED, so a refusal leaves the
// family as it was. A refusal from Revit part-way THROWS, and the host rolls
// back what came before it: one undo, all or nothing. Its words say the call
// FAILED and is rolled back, never that nothing was kept - the rollback is the
// host's, and whether it held cannot be seen from here (Codex, PR #356).
//
// EVERY LINK IS READ BACK, and one that does not read what was asked THROWS.
// One already as asked is reported "already linked" and not counted - which
// also makes a run with no transaction open a safe way to read the links: it
// changes nothing when nothing needs changing, and when something would, Revit
// refuses and the refusal says how each link stands.

var findings = new List<string>();
var linkReport = "";
var changed = 0;
var alreadyLinked = 0;
var matched = 0;
var notAFamily = false;
string refused = null;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// "SupplyAir" read as "Supply Air", the way a modeller writes it.
Func<string, string> spaced = name =>
{
    var text = new System.Text.StringBuilder();
    for (var i = 0; i < name.Length; i++)
    {
        if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) text.Append(' ');
        text.Append(name[i]);
    }
    return text.ToString();
};

// ONE NAME FOR ANOTHER with case and spaces ignored, and a plural read as its
// singular - "Duct Connectors" is the Duct Connector the palette shows.
Func<string, string, bool> sameWord = (said, label) =>
{
    var a = squash(said);
    var b = squash(label);
    if (a.Length == 0 || b.Length == 0) return false;
    return a == b || a == b + "s" || a + "s" == b;
};

// A SYSTEM IN EITHER ORDER. Revit's list says "Hydronic Supply" where its enum
// says SupplyHydronic, so the same words in another order are the same system.
Func<string, string, bool> sameSystem = (said, enumName) =>
{
    if (squash(said) == squash(enumName)) return true;
    var one = said.ToLowerInvariant().Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
        .OrderBy(w => w).ToArray();
    var two = spaced(enumName).ToLowerInvariant().Split(' ').Where(w => w.Length > 0).OrderBy(w => w).ToArray();
    return one.Length > 0 && one.SequenceEqual(two);
};

// TWO KINDS ARE THE SAME KIND WHATEVER VERSION THEIR IDS CARRY. A ForgeTypeId
// names its version - "...:airFlow-2.0.0" - and plain equality may compare it,
// so a kind a template made under an older version would read as some other
// kind. NameEquals compares the name alone, from 2021; before that a kind is
// an enum and plain equality is exact.
Func<object, object, bool> sameKind = (first, second) =>
{
    if (first == null || second == null) return false;
    var nameEquals = first.GetType().GetMethod("NameEquals", new[] { second.GetType() });
    if (nameEquals != null)
    {
        try { return (bool)nameEquals.Invoke(first, new[] { second }); }
        catch (Exception) { }
    }
    return first.Equals(second);
};

// WHAT KIND OF VALUE A PARAMETER HOLDS, and the words Revit shows for it -
// "Air Flow", "Length", "Material". Item1 is null when the kind cannot be
// read, and then Revit judges the link rather than this.
Func<Definition, Tuple<object, string>> kindOf = definition =>
{
    var unknown = Tuple.Create<object, string>(null, "a kind that could not be read");
    if (definition == null) return unknown;
    try
    {
        var labels = revitAssembly.GetType(dbNamespace + ".LabelUtils");
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            if (spec == null) return unknown;
            var typeIdProperty = spec.GetType().GetProperty("TypeId");
            var typeId = typeIdProperty == null ? null : typeIdProperty.GetValue(spec, null) as string;
            if (string.IsNullOrEmpty(typeId)) return unknown;
            string words = null;
            var forSpec = labels == null ? null
                : labels.GetMethod("GetLabelForSpec", flags, null, new[] { spec.GetType() }, null);
            if (forSpec != null)
            {
                try { words = forSpec.Invoke(null, new[] { spec }) as string; }
                catch (Exception) { words = null; }
            }
            return Tuple.Create<object, string>(spec, string.IsNullOrEmpty(words) ? typeId : words);
        }

        var property = definition.GetType().GetProperty("ParameterType");
        var old = property == null ? null : property.GetValue(definition, null);
        if (old == null || old.ToString() == "Invalid") return unknown;
        string oldWords = null;
        var forType = labels == null ? null
            : labels.GetMethod("GetLabelFor", flags, null, new[] { old.GetType() }, null);
        if (forType != null)
        {
            try { oldWords = forType.Invoke(null, new[] { old }) as string; }
            catch (Exception) { oldWords = null; }
        }
        return Tuple.Create<object, string>(old, string.IsNullOrEmpty(oldWords) ? spaced(old.ToString()) : oldWords);
    }
    catch (Exception)
    {
        return unknown;
    }
};

// WHAT THE PROPERTIES PALETTE CALLS AN ELEMENT: a connector by its domain, a
// solid by its form, a nested family by its family's name, anything else by
// its Revit category.
Func<Element, string> kindName = e =>
{
    var connector = e as ConnectorElement;
    if (connector != null)
    {
        var domain = "";
        try { domain = connector.Domain.ToString(); } catch (Exception) { domain = ""; }
        if (domain == "DomainHvac") return "Duct Connector";
        if (domain == "DomainPiping") return "Pipe Connector";
        if (domain == "DomainElectrical") return "Electrical Connector";
        if (domain == "DomainCableTrayConduit") return "Cable Tray or Conduit Connector";
        return "Connector";
    }
    var form = e as GenericForm;
    if (form != null)
    {
        var shape = e is Extrusion ? "Extrusion" : e is Blend ? "Blend" : e is Revolution ? "Revolve"
            : e is SweptBlend ? "Swept Blend" : e is Sweep ? "Sweep" : "Form";
        var solid = true;
        try { solid = form.IsSolid; } catch (Exception) { solid = true; }
        return solid ? shape : "Void " + shape;
    }
    var nested = e as FamilyInstance;
    if (nested != null)
    {
        try
        {
            if (nested.Symbol != null && nested.Symbol.Family != null) return nested.Symbol.Family.Name;
        }
        catch (Exception) { }
    }
    if (e.Category != null) return e.Category.Name;
    return spaced(e.GetType().Name);
};

// THE SYSTEM A CONNECTOR SERVES, as its classification is named. Empty for
// anything that is not a connector.
Func<Element, string> classificationOf = e =>
{
    var connector = e as ConnectorElement;
    if (connector == null) return "";
    try { return connector.SystemClassification.ToString(); }
    catch (Exception) { return ""; }
};

Func<Element, string> describe = e =>
{
    var served = classificationOf(e);
    return kindName(e) + (served.Length > 0 ? " (" + spaced(served) + ")" : "") + " id " + e.Id.ToString();
};

Func<FamilyParameter, string> nameOf = fp =>
{
    if (fp == null) return "not linked";
    try { return fp.Definition.Name; }
    catch (Exception) { return "?"; }
};

// WHAT A PARAMETER READS, in Revit's own words - never a number re-derived
// here, so no unit is named in this file.
Func<Parameter, string> reads = p =>
{
    try
    {
        var text = p.AsValueString() ?? "";
        if (text.Length == 0 && p.StorageType == StorageType.ElementId)
        {
            var referenced = doc.GetElement(p.AsElementId());
            text = referenced == null ? "" : referenced.Name;
        }
        if (text.Length == 0 && p.StorageType == StorageType.String) text = p.AsString() ?? "";
        return text;
    }
    catch (Exception)
    {
        return "";
    }
};

// Each link asked for: the element parameter's name, the family parameter's
// name as typed, and whether it is an unlink.
var asked = new List<Tuple<string, string, bool>>();

// Each link to make: the element, its parameter, the family parameter (null to
// unlink), the parameter's name as typed, and the family parameter it is
// linked to now.
var plans = new List<Tuple<Element, Parameter, FamilyParameter, string, FamilyParameter>>();
var chosen = new List<Element>();
var leftOut = 0;
var saidCategory = (category ?? "").Trim();
var saidSystem = (system ?? "").Trim();
var everyCategory = squash(saidCategory) == "all";
var everySystem = squash(saidSystem) == "all";

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family "
        + "Editor. A link between an element's parameter and a family parameter lives inside the family - "
        + "open it for editing first (OPEN_FAMILY_FOR_EDITING). Nothing was changed.";
}
else
{
    var fm = doc.FamilyManager;
    var problems = new List<string>();

    var familyParameters = new List<FamilyParameter>();
    foreach (FamilyParameter fp in fm.Parameters) if (fp != null) familyParameters.Add(fp);

    // A FAMILY PARAMETER BY NAME - exactly as typed first, then the one whose
    // name differs only in case, and only when exactly one does.
    Func<string, FamilyParameter> familyParameterNamed = name =>
    {
        FamilyParameter exact = null;
        try { exact = fm.get_Parameter(name); } catch (Exception) { exact = null; }
        if (exact != null) return exact;
        var loose = familyParameters.Where(fp => string.Equals(nameOf(fp), name, StringComparison.OrdinalIgnoreCase)).ToList();
        return loose.Count == 1 ? loose[0] : null;
    };

    Func<Parameter, bool> canLink = p =>
    {
        try { return p != null && fm.CanElementParameterBeAssociated(p); }
        catch (Exception) { return false; }
    };

    // An element's parameters by name, case ignored - the name in Properties.
    Func<Element, string, List<Parameter>> parametersNamed = (e, name) =>
    {
        var found = new List<Parameter>();
        foreach (Parameter p in e.Parameters)
        {
            var own = "";
            try { own = p.Definition == null ? "" : p.Definition.Name; } catch (Exception) { own = ""; }
            if (string.Equals(own, name, StringComparison.OrdinalIgnoreCase)) found.Add(p);
        }
        return found;
    };

    Func<Element, List<string>> linkableNames = e =>
    {
        var names = new List<string>();
        foreach (Parameter p in e.Parameters)
        {
            if (!canLink(p)) continue;
            try { if (p.Definition != null) names.Add(p.Definition.Name); } catch (Exception) { }
        }
        return names.Distinct().OrderBy(n => n).ToList();
    };

    // THE FAMILY PARAMETERS THAT COULD DRIVE ONE ELEMENT PARAMETER: the same
    // kind of value, or a kind that cannot be read and so is Revit's to judge.
    Func<Parameter, List<string>> couldDrive = own =>
    {
        var ownKind = kindOf(own.Definition);
        return familyParameters
            .Where(fp =>
            {
                try
                {
                    if (fp.StorageType != own.StorageType) return false;
                    var theirs = kindOf(fp.Definition);
                    return ownKind.Item1 == null || theirs.Item1 == null || sameKind(ownKind.Item1, theirs.Item1);
                }
                catch (Exception) { return false; }
            })
            .Select(nameOf).OrderBy(n => n).ToList();
    };

    // THE LINKS ASKED FOR.
    if (links == null || links.Count == 0)
        problems.Add("No links were given - \"Flow=Actual_Supply_Air_Flow\", semicolons between, or "
            + "\"Flow=none\" to unlink.");
    else
    {
        foreach (var pair in links)
        {
            var own = (pair.Key ?? "").Trim();
            var wanted = (pair.Value ?? "").Trim();

            // THE SAME PARAMETER TWICE, in another case - "Flow=A; flow=A". The
            // table keeps both keys and both find the one parameter, so it would
            // be planned twice (Codex, PR #356).
            var twice = asked.FirstOrDefault(a => string.Equals(a.Item1, own, StringComparison.OrdinalIgnoreCase));
            if (twice != null)
            {
                problems.Add("\"" + twice.Item1 + "\" and \"" + own + "\" name the same parameter twice in the links - "
                    + "say it once.");
                continue;
            }

            if (wanted.Length == 0)
            {
                problems.Add("\"" + own + "=\" names no family parameter - write \"" + own + "=none\" to unlink it.");
                continue;
            }
            var unlink = string.Equals(wanted, "none", StringComparison.OrdinalIgnoreCase);
            if (unlink && familyParameters.Any(fp => string.Equals(nameOf(fp), wanted, StringComparison.OrdinalIgnoreCase)))
            {
                problems.Add("This family has a parameter called \"" + wanted + "\", so \"" + own + "=" + wanted
                    + "\" could mean unlink it or link it to that parameter - rename the parameter first.");
                continue;
            }
            asked.Add(Tuple.Create(own, unlink ? "none" : wanted, unlink));
        }
    }

    if (saidCategory.Length == 0)
        problems.Add("No elements were named - a category such as \"Duct Connector\" or \"Extrusion\", or \"all\".");

    // A BLANK SYSTEM IS NOT "ALL". Read as all, a missing classification on
    // "Duct Connector" would relink supply, return and exhaust alike, and the
    // choice would be this code's rather than the modeller's (Codex, PR #356).
    if (saidSystem.Length == 0)
        problems.Add("No system was named - a connector classification such as \"Supply Air\", or \"all\" for "
            + "every element whatever its system. A blank is not read as all.");

    // A SYSTEM REVIT DOES NOT HAVE is refused with the close ones, before the
    // family is searched for it.
    if (!everySystem && saidSystem.Length > 0)
    {
        var classifications = Enum.GetNames(typeof(MEPSystemClassification))
            .Where(n => n != "UndefinedSystemClassification" && n != "Fitting" && n != "Global").ToList();
        if (!classifications.Any(n => sameSystem(saidSystem, n)))
        {
            var near = classifications
                .Where(n => squash(n).Contains(squash(saidSystem)) || squash(saidSystem).Contains(squash(n)))
                .Select(spaced).Take(8).ToList();
            problems.Add("No system classification is called \"" + saidSystem + "\"." + (near.Count > 0
                ? " Close: " + string.Join(", ", near) + "."
                : " Revit's names are used - Supply Air, Return Air, Exhaust Air, Hydronic Supply, Hydronic "
                  + "Return, Domestic Cold Water, Sanitary, Power Balanced - or \"all\"."));
        }
    }

    // EVERY ELEMENT THAT HAS SOMETHING TO LINK.
    var candidates = new List<Element>();
    if (problems.Count == 0)
    {
        foreach (var e in new FilteredElementCollector(doc).WhereElementIsNotElementType())
        {
            var linkable = false;
            try
            {
                foreach (Parameter p in e.Parameters)
                {
                    if (canLink(p)) { linkable = true; break; }
                }
            }
            catch (Exception) { linkable = false; }
            if (linkable) candidates.Add(e);
        }

        // A CATEGORY MATCHES EXACTLY FIRST, case aside. Only when no element
        // carries the name exactly is the loose match used - spaces and
        // punctuation ignored, a plural read as its singular - and a loose
        // match that reaches two DIFFERENT sets of elements, nested families
        // "Pump" and "Pumps" say, is refused rather than taking both (Codex,
        // PR #356). Two names on the same elements are one set, not two.
        Func<Element, List<string>> labelsOf = e =>
        {
            var labels = new List<string> { kindName(e) };
            if (e.Category != null) labels.Add(e.Category.Name);
            return labels;
        };
        Func<string, string, bool> sameName = (one, two) =>
            string.Equals((one ?? "").Trim(), (two ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
        var takenBy = new List<string>();
        var ambiguous = false;
        if (!everyCategory)
        {
            var allLabels = candidates.SelectMany(labelsOf).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            takenBy = allLabels.Where(l => sameName(l, saidCategory)).ToList();
            if (takenBy.Count == 0)
            {
                takenBy = allLabels.Where(l => sameWord(saidCategory, l)).ToList();
                var sets = takenBy
                    .Select(l => string.Join(",", candidates.Where(c => labelsOf(c).Any(x => sameName(x, l)))
                        .Select(c => c.Id.ToString()).OrderBy(id => id)))
                    .Distinct().ToList();
                if (sets.Count > 1)
                {
                    ambiguous = true;
                    problems.Add("\"" + saidCategory + "\" could mean " + string.Join(" or ", takenBy.Select(l => "\"" + l + "\""))
                        + ", which are different elements - name one of them exactly.");
                }
            }
        }

        foreach (var e in candidates)
        {
            if (ambiguous) break;
            if (!everyCategory && !labelsOf(e).Any(l => takenBy.Any(t => sameName(t, l)))) continue;
            if (!everySystem)
            {
                var served = classificationOf(e);
                if (served.Length == 0 || !sameSystem(saidSystem, served)) continue;
            }
            chosen.Add(e);
        }

        // "all" TAKES ONLY WHAT HAS EVERY PARAMETER NAMED; a named category
        // must have them all, or the call is refused below.
        if (everyCategory)
        {
            var before = chosen.Count;
            chosen = chosen.Where(e => asked.All(a => parametersNamed(e, a.Item1).Any(canLink))).ToList();
            leftOut = before - chosen.Count;
        }

        if (chosen.Count == 0 && !ambiguous)
        {
            var inventory = candidates
                .GroupBy(e => kindName(e) + (classificationOf(e).Length > 0 ? " (" + spaced(classificationOf(e)) + ")" : ""))
                .OrderBy(g => g.Key)
                .Select(g => g.Count() + " " + g.Key).ToList();
            var categories = candidates.Where(e => e.Category != null).Select(e => e.Category.Name)
                .Distinct().OrderBy(n => n).ToList();
            problems.Add("Nothing in this family matches \"" + saidCategory + "\""
                + (everySystem ? "" : " on the system \"" + saidSystem + "\"")
                + (everyCategory && leftOut > 0 ? " with " + string.Join(" and ", asked.Select(a => "\"" + a.Item1 + "\"")) + " to link" : "")
                + ". What it has that can be linked: "
                + (inventory.Count > 0 ? string.Join(", ", inventory) : "nothing")
                + (categories.Count > 0 ? " - in the Revit categories " + string.Join(", ", categories) : "") + ".");
        }
    }

    matched = chosen.Count;

    // EVERY ELEMENT, EVERY LINK, CHECKED BEFORE THE FIRST CHANGE.
    //
    // THE SAME REFUSAL ON MANY ELEMENTS IS SAID ONCE, with every element it
    // applies to named in it. Measured 2026-09-28 on the fan coil family:
    // eight extrusions refused one Length parameter said the same sentence
    // eight times. Each sentence carries <<ELEMENTS>> where the elements go.
    var perElement = new List<Tuple<string, List<string>>>();
    Action<string, Element> refuseFor = (sentence, e) =>
    {
        var found = perElement.FirstOrDefault(g => g.Item1 == sentence);
        if (found == null)
        {
            found = Tuple.Create(sentence, new List<string>());
            perElement.Add(found);
        }
        found.Item2.Add(describe(e));
    };

    if (problems.Count == 0)
    {
        foreach (var e in chosen)
        {
            foreach (var link in asked)
            {
                var named = parametersNamed(e, link.Item1);
                var linkable = named.Where(canLink).ToList();
                if (linkable.Count == 0)
                {
                    if (named.Count == 0)
                        refuseFor("No parameter called \"" + link.Item1 + "\" on <<ELEMENTS>>. What can be linked there: "
                            + string.Join(", ", linkableNames(e)) + ".", e);
                    else
                        refuseFor("Revit does not let \"" + link.Item1 + "\" be linked to a family parameter on <<ELEMENTS>>"
                            + (named.Any(p => p.IsReadOnly)
                                ? " - it is read-only there" + (e is ConnectorElement
                                    ? ", as a connector's flow is unless its Flow Configuration is Preset" : "")
                                : "") + ".", e);
                    continue;
                }
                if (linkable.Count > 1)
                {
                    refuseFor(linkable.Count + " parameters called \"" + link.Item1 + "\" can be linked on <<ELEMENTS>>, "
                        + "and which one is meant cannot be told from the name - nothing was changed rather than one "
                        + "picked.", e);
                    continue;
                }

                var own = linkable[0];
                // A READ THAT FAILED IS NOT "NOT LINKED" - null is what an
                // unlinked parameter answers too (Codex, PR #356).
                FamilyParameter now = null;
                string unreadable = null;
                try { now = fm.GetAssociatedFamilyParameter(own); }
                catch (Exception ex) { unreadable = ex.Message; }
                if (unreadable != null)
                {
                    refuseFor("Revit could not say which family parameter \"" + link.Item1 + "\" is linked to on "
                        + "<<ELEMENTS>>: " + unreadable, e);
                    continue;
                }

                FamilyParameter target = null;
                if (!link.Item3)
                {
                    target = familyParameterNamed(link.Item2);
                    if (target == null)
                    {
                        var drivers = couldDrive(own);
                        refuseFor("No family parameter is called \"" + link.Item2 + "\"." + (drivers.Count > 0
                            ? " The ones that could drive \"" + link.Item1 + "\" (" + kindOf(own.Definition).Item2 + ") on "
                              + "<<ELEMENTS>>: " + string.Join(", ", drivers) + "."
                            : " None in this family could drive \"" + link.Item1 + "\" (" + kindOf(own.Definition).Item2
                              + ") on <<ELEMENTS>> - ADD_FAMILY_PARAMETERS makes one."), e);
                        continue;
                    }

                    var ownKind = kindOf(own.Definition);
                    var targetKind = kindOf(target.Definition);
                    var kindsDiffer = ownKind.Item1 != null && targetKind.Item1 != null && !sameKind(ownKind.Item1, targetKind.Item1);
                    var storesDiffer = false;
                    try { storesDiffer = own.StorageType != target.StorageType; } catch (Exception) { storesDiffer = false; }
                    if (kindsDiffer || storesDiffer)
                    {
                        var drivers = couldDrive(own);
                        refuseFor("\"" + nameOf(target) + "\" (" + targetKind.Item2 + ") cannot drive \"" + link.Item1
                            + "\" (" + ownKind.Item2 + ") on <<ELEMENTS>> - Revit links two parameters only when they "
                            + "hold the same kind of value." + (drivers.Count > 0
                                ? " The ones that could drive it: " + string.Join(", ", drivers) + "."
                                : " None in this family could - ADD_FAMILY_PARAMETERS makes one."), e);
                        continue;
                    }
                }

                plans.Add(Tuple.Create(e, own, target, link.Item1, now));
            }
        }
    }

    foreach (var refusal in perElement)
    {
        var onWhat = refusal.Item2.Count == 1
            ? refusal.Item2[0]
            : refusal.Item2.Count + " elements (" + string.Join(", ", refusal.Item2.Take(20))
              + (refusal.Item2.Count > 20 ? " and " + (refusal.Item2.Count - 20) + " more" : "") + ")";
        problems.Add(refusal.Item1.Replace("<<ELEMENTS>>", onWhat));
    }

    if (problems.Count > 0)
    {
        var shown = problems.Take(12).ToList();
        refused = "Nothing was changed. " + string.Join(" ", shown)
            + (problems.Count > shown.Count ? " And " + (problems.Count - shown.Count) + " more like these." : "");
    }
}

// ---------------------------------------------------------------------------
// LINK, THEN READ EVERY ONE BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var fm = doc.FamilyManager;
    Func<FamilyParameter, FamilyParameter, bool> same = (first, second) =>
        first == null ? second == null : second != null && first.Id == second.Id;

    // HOW EVERY LINK STOOD, for any refusal from here on - a run with no
    // transaction open is refused by Revit at the first change, and this is
    // what makes that refusal a reading.
    var asItWas = string.Join("; ", plans.Select(p => describe(p.Item1) + " " + p.Item4 + " " + nameOf(p.Item5)));

    var any = false;
    foreach (var plan in plans)
    {
        if (same(plan.Item5, plan.Item3)) continue;
        try
        {
            fm.AssociateElementParameterToFamilyParameter(plan.Item2, plan.Item3);
            any = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused to " + (plan.Item3 == null
                    ? "unlink \"" + plan.Item4 + "\" on " + describe(plan.Item1)
                    : "link \"" + plan.Item4 + "\" on " + describe(plan.Item1) + " to \"" + nameOf(plan.Item3) + "\"")
                + ": " + ex.Message + " The call failed there, and Heron rolls the whole call back - read the "
                + "links again to see that it did. As they stood before the call: " + asItWas + ".");
        }
    }

    if (any) doc.Regenerate();

    var rows = new List<string>();
    foreach (var e in chosen)
    {
        var parts = new List<string>();
        foreach (var plan in plans.Where(p => p.Item1.Id == e.Id))
        {
            // A READ-BACK THAT FAILED FAILS THE CALL. Taken as null it would pass
            // an unlink it never saw (Codex, PR #356).
            FamilyParameter after = null;
            try
            {
                after = fm.GetAssociatedFamilyParameter(plan.Item2);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("\"" + plan.Item4 + "\" on " + describe(e) + " could not be read "
                    + "back after the call: " + ex.Message + " The call failed, and Heron rolls the whole call back - "
                    + "read the links again to see that it did. As they stood before the call: " + asItWas + ".");
            }
            if (!same(after, plan.Item3))
                throw new InvalidOperationException("\"" + plan.Item4 + "\" on " + describe(e) + " reads " + nameOf(after)
                    + " after the call, not " + (plan.Item3 == null ? "unlinked" : "\"" + nameOf(plan.Item3) + "\"")
                    + ". The call failed, and Heron rolls the whole call back - read the links again to see that "
                    + "it did. As they stood before the call: " + asItWas + ".");

            var value = reads(plan.Item2);
            var tail = value.Length > 0 ? " (reads " + value + ")" : "";
            if (same(plan.Item5, plan.Item3))
            {
                alreadyLinked++;
                parts.Add(plan.Item4 + (after == null ? " already not linked" : " already linked to " + nameOf(after)) + tail);
            }
            else
            {
                changed++;
                parts.Add(plan.Item4 + " " + nameOf(plan.Item5) + " -> " + nameOf(after) + tail);
            }
        }
        rows.Add(describe(e) + ": " + string.Join("; ", parts));
    }
    linkReport = string.Join("  ||  ", rows);

    findings.Add("Family '" + doc.Title + "': " + changed + " link(s) changed and " + alreadyLinked
        + " already as asked, on " + matched + " element(s) matching \"" + saidCategory + "\""
        + (everySystem ? "" : " on the system \"" + saidSystem + "\"") + " - read back from the family.");
    if (leftOut > 0)
        findings.Add(leftOut + " element(s) matched \"all\" but have no " + string.Join(" or ", asked.Select(a => "\"" + a.Item1 + "\""))
            + " that can be linked, and were left alone.");
    if (plans.Any(p => p.Item3 == null && p.Item5 != null))
        findings.Add("An unlinked parameter keeps the value it had and can be typed into again in Properties.");
    if (changed > 0)
        findings.Add("The links are in the family only. Placed units in a project follow them once the family is "
            + "loaded into that project again, and the family has not been saved.");
}

if (refused != null) findings.Add(refused);
