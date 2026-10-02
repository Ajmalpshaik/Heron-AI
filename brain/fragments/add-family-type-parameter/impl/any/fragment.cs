// NOT STANDALONE. Assumes `doc`, `parameterName`, `category`, `instance`,
// `parameterGroup`, `elements` and `typeValues` are in scope; leaves
// `parameterReport`, `linked`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A FAMILY TYPE PARAMETER - Family Types, New Parameter, "Family Type" - that
// lets each type of this family choose WHICH TYPE of a nested part it uses: the
// 5 kW or the 7 kW motor, the lever or the wheel handle, the round or the
// square profile a sweep is drawn with. The nested family's type becomes a
// value in Family Types instead of a fixed choice.
//
// THREE STEPS, ONE JOB, IN THE ORDER THAT KEEPS THE GEOMETRY STILL:
//   1. the parameter is made for one CATEGORY - only a family of that category
//      already loaded into this one can be its value, which is Revit's rule;
//   2. its value in the current type is set to the type the nested parts
//      already show, so nothing moves when they are linked;
//   3. the nested copies named - or a sweep's profile - are linked to it, the
//      Label in their Properties. Revit's own remarks do not say which of a
//      nested copy's parameters carries that link, so each candidate is offered
//      to Revit's own "can it be linked" test and the one it takes is reported.
// Then each family type named in `typeValues` takes its nested type.
//
// THE KIND IS REVIT'S: a nested copy's type parameter holds an element, and a
// value is offered only from Revit's own list of the types this parameter can
// take - a name not on it is refused with the list.
//
// ALL OR NOTHING, READ BACK: every link and every type's value are read again;
// one that does not read as asked fails the call, and the host rolls the whole
// call back. The current type is put back as it was.

var findings = new List<string>();
var parameterReport = "";
var linked = 0;
var notAFamily = false;
string refused = null;

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// AddParameter(name, GROUP, CATEGORY, isInstance) - the overload that makes a
// Family Type parameter, picked by its own signature, the newer group kind first.
var addParameter = typeof(FamilyManager).GetMethods()
    .Where(m => m.Name == "AddParameter")
    .Where(m =>
    {
        var p = m.GetParameters();
        return p.Length == 4 && p[0].ParameterType == typeof(string) && p[2].ParameterType == typeof(Category)
            && p[3].ParameterType == typeof(bool);
    })
    .OrderBy(m => m.GetParameters()[1].ParameterType.Name == "ForgeTypeId" ? 0 : 1)
    .FirstOrDefault();
var groupType = addParameter == null ? null : addParameter.GetParameters()[1].ParameterType;

// EVERY GROUP THIS REVIT OFFERS, under the label the Properties palette shows -
// the same reading ADD_FAMILY_PARAMETERS makes.
var groups = new List<KeyValuePair<string, object>>();
if (groupType != null)
{
    if (groupType.IsEnum)
    {
        var label = typeof(LabelUtils).GetMethod("GetLabelFor", new[] { groupType });
        foreach (var value in Enum.GetValues(groupType))
        {
            string text = null;
            try { text = label == null ? null : (string)label.Invoke(null, new[] { value }); }
            catch (Exception) { }
            if (text == null && value.ToString() == "INVALID") text = "Other";
            groups.Add(new KeyValuePair<string, object>(text ?? value.ToString(), value));
        }
    }
    else
    {
        var owner = revitAssembly.GetType(dbNamespace + ".GroupTypeId");
        var label = typeof(LabelUtils).GetMethod("GetLabelForGroup", new[] { groupType });
        if (owner != null)
            foreach (var property in owner.GetProperties(flags))
            {
                if (property.PropertyType != groupType) continue;
                var value = property.GetValue(null);
                if (value == null) continue;
                string text = null;
                try { text = label == null ? null : (string)label.Invoke(null, new[] { value }); }
                catch (Exception) { }
                groups.Add(new KeyValuePair<string, object>(text ?? property.Name, value));
            }
        // "OTHER" HAS NO GroupTypeId PROPERTY - it is the EMPTY id, where the
        // older releases had INVALID (LabelUtils.GetLabelForGroup's remark), so
        // it is offered as ADD_FAMILY_PARAMETERS offers it.
        groups.Add(new KeyValuePair<string, object>("Other", Activator.CreateInstance(groupType)));
    }
}

// The name a nested type is offered under: "Family : Type", and the type alone.
Func<ElementId, string> typeLabel = id =>
{
    var e = doc.GetElement(id) as ElementType;
    if (e == null) return "(none)";
    var symbol = e as FamilySymbol;
    return symbol != null && symbol.Family != null ? symbol.Family.Name + " : " + symbol.Name : e.Name;
};

var problems = new List<string>();
Category wanted = null;
object groupValue = null;
var name = (parameterName ?? "").Trim();
// Each element to link: the element, the parameter of it that carries the link,
// and the type it shows now.
var targets = new List<Tuple<Element, Parameter, ElementId>>();
var carriersTaken = new List<string>();
FamilyParameter existing = null;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A Family Type parameter is made inside a family - open it first.";
}
else
{
    var fm = doc.FamilyManager;
    if (addParameter == null) problems.Add("This Revit has no AddParameter(name, group, category, instance) that this "
        + "recognises.");
    if (name.Length == 0) problems.Add("No parameter name was given.");
    if (fm.CurrentType == null)
        problems.Add("The family has no type yet, and a Family Type parameter needs one to hold its value - "
            + "SET_FAMILY_TYPE_VALUES makes the first.");

    // THE CATEGORY, as Revit names it - and a family of it loaded in here. Read
    // from the loaded families first: Profiles is a category the document's own
    // category list does not hold, and a profile family is a common value here.
    var said = (category ?? "").Trim();
    var loaded = new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>()
        .Where(f => !f.IsOwnerFamily && f.FamilyCategory != null).ToList();
    wanted = loaded.Select(f => f.FamilyCategory)
        .FirstOrDefault(c => string.Equals(c.Name, said, StringComparison.OrdinalIgnoreCase));
    if (wanted == null)
        foreach (Category c in doc.Settings.Categories)
            if (string.Equals(c.Name, said, StringComparison.OrdinalIgnoreCase)) { wanted = c; break; }
    if (wanted == null)
        problems.Add("No category is called \"" + said + "\". The families loaded in here are of: "
            + (loaded.Count == 0 ? "none - LOAD_FAMILY first" : string.Join(", ", loaded.Select(f => f.FamilyCategory.Name)
                .Distinct().OrderBy(n => n))) + ".");
    else if (!loaded.Any(f => f.FamilyCategory.Id == wanted.Id))
        problems.Add("No family of the " + wanted.Name + " category is loaded into this family, and Revit takes a "
            + "Family Type parameter only for a category it holds - LOAD_FAMILY one first.");

    // THE GROUP - Construction unless named.
    var wantedGroup = (parameterGroup ?? "").Trim();
    if (wantedGroup.Length == 0) wantedGroup = "Construction";
    foreach (var entry in groups)
        if (string.Equals(entry.Key.Trim(), wantedGroup, StringComparison.OrdinalIgnoreCase)) { groupValue = entry.Value; break; }
    if (groupType != null && groupValue == null)
        problems.Add("No group called \"" + wantedGroup + "\" exists in this Revit - for example "
            + string.Join(", ", groups.Select(g => g.Key).Distinct().OrderBy(k => k).Take(12)) + ".");

    // A PARAMETER OF THAT NAME ALREADY HERE is used again only when it holds an
    // element - the way a Family Type parameter does - and refused otherwise.
    if (name.Length > 0)
    {
        existing = fm.get_Parameter(name);
        if (existing != null && existing.StorageType != StorageType.ElementId)
            problems.Add("\"" + name + "\" is already in this family and holds a " + existing.StorageType + ", not a "
                + "family type. Use another name.");
    }

    // THE ELEMENTS TO LINK: unique ids, or a nested family's name for every copy
    // of it placed in here.
    var tokens = (elements ?? "").Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
    if (tokens.Count == 0)
        problems.Add("No nested copy or sweep was named to link - a nested family's name, or ids, commas between.");
    var picked = new List<Element>();
    foreach (var token in tokens)
    {
        var byId = doc.GetElement(token);
        if (byId != null) { picked.Add(byId); continue; }
        var copies = new FilteredElementCollector(doc).OfClass(typeof(FamilyInstance)).Cast<FamilyInstance>()
            .Where(i => i.Symbol != null && i.Symbol.Family != null
                && string.Equals(i.Symbol.Family.Name, token, StringComparison.OrdinalIgnoreCase)).ToList();
        if (copies.Count == 0)
            problems.Add("\"" + token + "\" is neither an id in this family nor a nested family with a copy placed in it.");
        picked.AddRange(copies.Cast<Element>());
    }

    foreach (var element in picked.GroupBy(e => e.Id).Select(g => g.First()))
    {
        var instanceOf = element as FamilyInstance;
        var candidates = instanceOf != null
            ? new[] { BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM, BuiltInParameter.ELEM_TYPE_PARAM,
                      BuiltInParameter.SYMBOL_ID_PARAM }
            : new[] { BuiltInParameter.PROFILE_FAM_TYPE };
        Parameter carrier = null;
        foreach (var bip in candidates)
        {
            var p = element.get_Parameter(bip);
            if (p == null || p.StorageType != StorageType.ElementId) continue;
            bool can;
            try { can = fm.CanElementParameterBeAssociated(p); } catch (Exception) { can = false; }
            if (can) { carrier = p; carriersTaken.Add(bip.ToString()); break; }
        }
        var shows = carrier == null ? ElementId.InvalidElementId : carrier.AsElementId();
        // The category of the type it shows - read from its family, because a
        // profile's own Category may be the internal one the document hides.
        var shownElement = shows == null || shows == ElementId.InvalidElementId ? null : doc.GetElement(shows);
        var shownSymbol = shownElement as FamilySymbol;
        var shownCategory = shownSymbol != null && shownSymbol.Family != null && shownSymbol.Family.FamilyCategory != null
            ? shownSymbol.Family.FamilyCategory
            : (shownElement == null ? null : shownElement.Category);
        if (carrier == null)
            problems.Add("\"" + element.Name + "\" (" + element.UniqueId + ") has no type or profile Revit lets be linked "
                + "to a family parameter - only a nested family's copy or a sweep's profile can be.");
        else if (instanceOf == null && (shows == null || shows == ElementId.InvalidElementId))
            problems.Add("\"" + element.Name + "\" (" + element.UniqueId + ") is drawn with a sketched profile, not a "
                + "loaded profile family - choose a loaded profile for it first, in the sweep's Profile list.");
        else if (wanted != null && (shownCategory == null || shownCategory.Id != wanted.Id))
            problems.Add("\"" + element.Name + "\" shows a " + (shownCategory == null ? "type of no category"
                : shownCategory.Name + " type") + ", not " + wanted.Name + " - a Family Type parameter swaps types of "
                + "its own category only.");
        else targets.Add(Tuple.Create(element, carrier, shows));
    }

    // A PARAMETER ALREADY HERE IS REUSED ONLY WHEN NOTHING IT DRIVES MOVES. It
    // must take the type the named parts show, and where it already drives
    // parts it must already hold that type - setting it would swap them.
    var showingNow = targets.Select(t => t.Item3).Distinct().ToList();
    if (existing != null && problems.Count == 0 && showingNow.Count == 1)
    {
        List<ElementId> canTake = null;
        try { canTake = doc.OwnerFamily.GetFamilyTypeParameterValues(existing.Id).ToList(); } catch (Exception) { }
        ElementId holds = null;
        try { holds = fm.CurrentType == null ? null : fm.CurrentType.AsElementId(existing); } catch (Exception) { }
        var drives = existing.AssociatedParameters == null ? 0 : existing.AssociatedParameters.Size;
        if (canTake == null)
            problems.Add("\"" + name + "\" is already in this family but is not a Family Type parameter. Use another name.");
        else if (!canTake.Contains(showingNow[0]))
            problems.Add("\"" + name + "\" is already in this family and takes another category's types than "
                + typeLabel(showingNow[0]) + ". Use another name.");
        else if (drives > 0 && (holds == null || holds != showingNow[0]))
            problems.Add("\"" + name + "\" is already in this family, holds "
                + (holds == null || holds == ElementId.InvalidElementId ? "nothing" : typeLabel(holds))
                + " in the current type and already drives " + drives + " part(s); linking parts that show "
                + typeLabel(showingNow[0]) + " would swap those. Use another name, or give the parts the same type first.");
    }

    if (problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// MAKE OR REUSE, SET THE CURRENT VALUE, LINK, THEN EACH TYPE
// ---------------------------------------------------------------------------

if (refused == null)
{
    var fm = doc.FamilyManager;
    var startType = fm.CurrentType;
    var made = false;
    var parameter = existing;
    if (parameter == null)
    {
        try { parameter = addParameter.Invoke(fm, new object[] { name, groupValue, wanted, instance }) as FamilyParameter; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused the Family Type parameter \"" + name + "\": "
                + (ex.InnerException != null ? ex.InnerException.Message : ex.Message) + " The call failed, and Heron "
                + "rolls the whole call back.");
        }
        if (parameter == null)
            throw new InvalidOperationException("Revit made no parameter \"" + name + "\". The call failed, and Heron "
                + "rolls the whole call back.");
        made = true;
    }

    // THE TYPES THIS PARAMETER CAN TAKE - Revit's own list.
    var offered = new List<ElementId>();
    try { offered = doc.OwnerFamily.GetFamilyTypeParameterValues(parameter.Id).ToList(); }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit does not read \"" + name + "\" as a Family Type parameter: "
            + ex.Message + " The call failed, and Heron rolls the whole call back.");
    }
    Func<string, ElementId> offeredNamed = text =>
    {
        var key = squash(text);
        var whole = offered.Where(id => squash(typeLabel(id)) == key).ToList();
        if (whole.Count == 1) return whole[0];
        var bare = offered.Where(id => doc.GetElement(id) != null && squash(doc.GetElement(id).Name) == key).ToList();
        return bare.Count == 1 ? bare[0] : null;
    };

    // Each family type named, and the nested type it takes - checked before the
    // first value is written.
    var plans = new List<Tuple<FamilyType, ElementId, string>>();
    var unknown = new List<string>();
    if (typeValues != null)
        foreach (var pair in typeValues)
        {
            FamilyType familyType = null;
            foreach (FamilyType t in fm.Types)
                if (string.Equals(t.Name, (pair.Key ?? "").Trim(), StringComparison.OrdinalIgnoreCase)) { familyType = t; break; }
            var value = offeredNamed(pair.Value);
            // "Small" and "small" are one type; two values for it would leave the
            // second and report the first as Revit's failure.
            if (familyType != null && plans.Any(p => p.Item1.Name == familyType.Name))
                unknown.Add("the type \"" + familyType.Name + "\" is given twice");
            else if (familyType == null) unknown.Add("no family type is called \"" + pair.Key + "\"");
            else if (value == null) unknown.Add("\"" + pair.Value + "\" is not one of the types it can take - "
                + string.Join(", ", offered.Select(typeLabel).Take(15)));
            else plans.Add(Tuple.Create(familyType, value, pair.Value));
        }
    if (unknown.Count > 0)
        throw new InvalidOperationException("Nothing was kept. " + string.Join("; ", unknown) + ". The call failed, and "
            + "Heron rolls the whole call back.");

    // THE CURRENT TYPE TAKES WHAT THE NESTED PARTS SHOW NOW, so linking moves
    // nothing.
    var showing = targets.Select(t => t.Item3).Distinct().ToList();
    if (showing.Count > 1)
        throw new InvalidOperationException("The parts named show " + showing.Count + " different types now - "
            + string.Join(", ", showing.Select(typeLabel)) + ". One parameter gives them all one type; name the copies "
            + "that share it. The call failed, and Heron rolls the whole call back.");
    try { fm.Set(parameter, showing[0]); }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Revit would not set \"" + name + "\" to " + typeLabel(showing[0]) + ": "
            + ex.Message + " The call failed, and Heron rolls the whole call back.");
    }

    var already = 0;
    foreach (var t in targets)
    {
        var before = fm.GetAssociatedFamilyParameter(t.Item2);
        if (before != null && before.Id == parameter.Id) { already++; continue; }
        try { fm.AssociateElementParameterToFamilyParameter(t.Item2, parameter); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not link \"" + t.Item1.Name + "\"'s " + t.Item2.Definition.Name
                + " to \"" + name + "\": " + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
        linked++;
    }

    foreach (var plan in plans)
    {
        fm.CurrentType = plan.Item1;
        try { fm.Set(parameter, plan.Item2); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not give the type \"" + plan.Item1.Name + "\" the nested type "
                + typeLabel(plan.Item2) + ": " + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
    }
    fm.CurrentType = startType;
    doc.Regenerate();

    // READ BACK: every link, then every type's value.
    foreach (var t in targets)
    {
        var now = fm.GetAssociatedFamilyParameter(t.Item2);
        if (now == null || now.Id != parameter.Id)
            throw new InvalidOperationException("\"" + t.Item1.Name + "\" does not read as linked to \"" + name + "\" "
                + "after the call. The call failed, and Heron rolls the whole call back.");
    }
    var values = new List<string>();
    foreach (FamilyType ft in fm.Types)
    {
        ElementId value = null;
        try { value = ft.AsElementId(parameter); } catch (Exception) { }
        var plan = plans.FirstOrDefault(p => p.Item1.Name == ft.Name);
        if (plan != null && (value == null || value != plan.Item2))
            throw new InvalidOperationException("The type \"" + ft.Name + "\" reads " + (value == null ? "nothing"
                : typeLabel(value)) + " for \"" + name + "\", not " + typeLabel(plan.Item2) + ". The call failed, and "
                + "Heron rolls the whole call back.");
        values.Add(ft.Name + " = " + (value == null || value == ElementId.InvalidElementId ? "(none)" : typeLabel(value)));
    }

    parameterReport = "\"" + name + "\" - Family Type of " + wanted.Name + ", " + (parameter.IsInstance ? "instance" : "type")
        + (made ? ", made now" : ", already in the family") + "; linked: " + string.Join(", ", targets.Select(t =>
            t.Item1.Name + " by its " + t.Item2.Definition.Name)) + "; values: " + string.Join("; ", values) + ".";
    findings.Add(parameterReport);
    findings.Add(linked + " link(s) made and " + already + " already as asked. It can take "
        + offered.Count + " type(s): " + string.Join(", ", offered.Select(typeLabel).Take(12))
        + (offered.Count > 12 ? ", ..." : "") + ".");
    findings.Add("Revit took the link on " + string.Join(", ", carriersTaken.Distinct()) + " (NEEDS-CHECKING BV8). "
        + "A nested type added later is offered here once it is loaded into this family.");
}

if (refused != null) findings.Add(refused);
