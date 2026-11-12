// NOT STANDALONE. Assumes `doc`, `editScopeFailures`, `elements`,
// `assemblyName` and `namingCategory` are in scope; leaves `assembly`,
// `memberIds`, `alreadyInOne`, `refused` and `findings` behind.
//
// AN ASSEMBLY FROM THE ELEMENTS HANDED IN - Create Assembly in the ribbon - so
// CREATE_ASSEMBLY_VIEWS can then make its shop drawings. Named by the caller
// or by Revit.
//
// ELEMENTS ALREADY IN AN ASSEMBLY ARE LEFT OUT AND NAMED: Revit puts an element
// in one assembly only. The rest are checked with Revit's own test before
// anything is made, so a set Revit would refuse is refused here with the
// reason rather than half-made.
//
// THE NAMING CATEGORY decides the assembly's category and its default name.
// Given by name it must be one Revit accepts for this set; empty takes the
// first element's category when Revit accepts it.
//
// ===========================================================================
// VERSION 2 (FRAGMENT-ISSUES 5b-1117 and 5b-1553).
// THIS FRAGMENT OPENS ITS OWN TRANSACTIONS, AND ONLY BECAUSE IT HAS TO.
// ===========================================================================
//
// REVIT GIVES A NEW ASSEMBLY ITS TYPE ONLY WHEN THE TRANSACTION THAT MADE IT IS
// COMMITTED. Its own remark on AssemblyInstance.Create, word for word the same
// on every release 2020 to 2027: "Transaction must be committed after calling
// this method before performing any action on the newly created instance.
// Assembly type is assigned after the transaction for creating assembly
// instance is complete." Version 1 ran in the ONE transaction the add-in opens
// for a change, and named the assembly and read its name inside it. Both
// recorded runs answered "No valid type for the assembly instance" - and
// version 1 then said "NOTHING WAS MADE" with `memberIds` filled and the
// assembly still in that transaction, which was judged a pass, twice.
//
// SO IT IS TWO TRANSACTIONS IN ONE GROUP, the way a stair is made (D-112).
// Declaring `editScopeFailures` makes the add-in run this inside its
// TransactionGroup with NO transaction of its own open:
//   1. "Heron: create assembly" - the assembly is made, and COMMITTED.
//   2. "Heron: name assembly"   - it is named, and its type name and members
//                                 are read back.
// The group is the add-in's, so the modeller still gets ONE undo entry and a
// preview still rolls all of it back. Every transaction here takes
// `editScopeFailures`, so what Revit says reaches the verdict as it would from
// the add-in's own transaction.
//
// THE SWITCH IS DECIDED FOR THIS. D-115 item 1 (Accepted): "A fragment that
// Revit refuses inside a transaction declares `editScopeFailures` too, whatever
// the reason", and its item 3 asks each such card to name the Revit refusal
// that made it need the switch. It is this one, measured on Revit 2020 on
// 2026-10-18 and again on 2026-10-21, inside the transaction that made the
// assembly: "No valid type for the assembly instance". D-125 item 2
// (Accepted) keeps the switch's meaning - "a fragment that opens its own
// transactions".
//
// NOT MEASURED, AND THE WHOLE CARD RESTS ON IT: whether Revit assigns the type
// at a commit that is INSIDE an open TransactionGroup. The remark says "the
// transaction", and the stair cards show a commit inside the group is a real
// commit - but no run has shown an assembly named this way. If Revit does not,
// the type is looked for FIRST after the commit and the answer says exactly
// that - not that a name was refused.
//
// ALL OR NOTHING, AND THE NOTHING IS REVIT'S OWN ROLLBACK. Once step 1 is
// committed there is an assembly in the group. Anything that then fails - no
// type, a name Revit will not take, a name that belongs to other assemblies,
// members that do not read back - THROWS, with the reason in Revit's words.
// The add-in rolls a fragment that throws back whole, group and all, and says
// what its own check of the model found; the model is then exactly as it was
// and there is no undo entry. The first cut of this version took the assembly
// apart again with Disassemble and returned a refusal: on a kept run that left
// "make, then unmake" in the undo list and touched elements beside "NOTHING
// WAS MADE". Only a rollback is exact, so nothing here undoes its own work.
//
// A NAME IS NEVER TAKEN FROM OTHER ASSEMBLIES. Revit's reference on
// AssemblyTypeName: "All matching assembly instances share this name" - one
// assembly TYPE, shared by every assembly that matches. So Revit may give a
// new assembly an EXISTING type, and naming the new one would rename all of
// them. The assemblies already in the model are read before anything is
// made; if the new one comes back sharing its type with any of them under
// another name, or the name asked for is already another type's, it stops.
//
// A RESULT MEANS AN ASSEMBLY THAT EXISTS. `assembly` and `memberIds` are filled
// only after step 2 is committed, from what was read back there: an assembly
// with a type, under the name asked for when one was asked for.

var findings = new List<string>();
string refused = null;
AssemblyInstance assembly = null;
var memberIds = new List<ElementId>();
var alreadyInOne = new List<ElementId>();

Action<Transaction> discipline = t =>
{
    var options = t.GetFailureHandlingOptions();
    options.SetFailuresPreprocessor(editScopeFailures);
    options.SetClearAfterRollback(true);
    t.SetFailureHandlingOptions(options);
};

// Revit's own words for a transaction it would not keep. The failure
// discipline the add-in hands in records what Revit posted, and its ToString
// says it; an add-in older than that answers with its type name, not quoted.
Func<string> revitSaid = () =>
{
    var text = editScopeFailures == null ? null : editScopeFailures.ToString();
    return text != null && text.StartsWith("Revit would not let it through", StringComparison.Ordinal)
        ? " - " + text : "";
};
Func<Exception, string> words = error => (error.Message ?? "").TrimEnd('.', ' ');

var ids = new List<ElementId>();
Element first = null;
foreach (var element in elements)
{
    if (element == null || !element.IsValidObject) continue;
    if (element.AssemblyInstanceId != null && element.AssemblyInstanceId != ElementId.InvalidElementId)
    {
        alreadyInOne.Add(element.Id);
        continue;
    }
    ids.Add(element.Id);
    if (first == null) first = element;
}

if (ids.Count == 0)
    refused = alreadyInOne.Count > 0
        ? "Every element handed in is already in an assembly, so NOTHING WAS MADE."
        : "No elements were handed in, so NOTHING WAS MADE.";

ElementId categoryId = null;
var categoryName = "";
if (refused == null)
{
    var wanted = (namingCategory ?? "").Trim();
    if (wanted == "")
    {
        categoryId = first.Category == null ? null : first.Category.Id;
        categoryName = first.Category == null ? "" : first.Category.Name;
    }
    else
    {
        foreach (Category category in doc.Settings.Categories)
            if (category != null && string.Equals(category.Name, wanted, StringComparison.OrdinalIgnoreCase))
            {
                categoryId = category.Id;
                categoryName = category.Name;
                break;
            }
        if (categoryId == null) refused = "No category is named \"" + wanted + "\", so NOTHING WAS MADE.";
    }
    if (refused == null && categoryId == null) refused = "The first element has no category to name the assembly by, so NOTHING WAS MADE.";
}

if (refused == null)
{
    bool valid = false, namingOk = false;
    try { valid = AssemblyInstance.AreElementsValidForAssembly(doc, ids, ElementId.InvalidElementId); } catch { }
    try { namingOk = AssemblyInstance.IsValidNamingCategory(doc, categoryId, ids); } catch { }
    if (!valid) refused = "Revit will not put these elements in one assembly - a view-specific element, a host "
        + "and its hosted element split apart, or an element type Revit does not assemble. NOTHING WAS MADE.";
    else if (!namingOk) refused = "Revit will not name an assembly of these elements by \"" + categoryName
        + "\". Name another category of the elements. NOTHING WAS MADE.";
}

var wantedName = (assemblyName ?? "").Trim();
var madeId = ElementId.InvalidElementId;

// The house words for a stop: the add-in rolls a fragment that throws back
// whole, and adds what its own check of the model found.
var keptNothing = " The call failed, and Heron rolls the whole call back - no assembly is kept.";

// EVERY ASSEMBLY ALREADY IN THE MODEL, read before anything is made: which
// type each has, and what that type is called. See the header.
var assembliesBefore = new HashSet<ElementId>();
var typeOfExisting = new Dictionary<ElementId, ElementId>();
var nameOfType = new Dictionary<ElementId, string>();
if (refused == null)
{
    foreach (var each in new FilteredElementCollector(doc).OfClass(typeof(AssemblyInstance)).ToElements())
    {
        var existing = each as AssemblyInstance;
        if (existing == null) continue;
        assembliesBefore.Add(existing.Id);
        var existingType = existing.GetTypeId();
        if (existingType == null || existingType == ElementId.InvalidElementId) continue;
        typeOfExisting[existing.Id] = existingType;
        if (nameOfType.ContainsKey(existingType)) continue;
        try { nameOfType[existingType] = existing.AssemblyTypeName ?? ""; }
        catch (Exception) { nameOfType[existingType] = ""; }
    }
}

// ---------------------------------------------------------------------------
// 1. MAKE IT, AND COMMIT - Revit assigns the type at this commit, not before.
// ---------------------------------------------------------------------------
if (refused == null)
{
    string notMade = null;      // why Revit made nothing, without the last words
    using (var making = new Transaction(doc, "Heron: create assembly"))
    {
        making.Start();
        discipline(making);
        try
        {
            var made = AssemblyInstance.Create(doc, ids, categoryId);
            if (made == null) notMade = "Revit returned no assembly.";
            else madeId = made.Id;
        }
        catch (Exception ex)
        {
            notMade = "Revit refused the assembly: " + words(ex) + ".";
        }

        if (notMade != null)
            making.RollBack();
        else if (making.Commit() != TransactionStatus.Committed)
            notMade = "Revit would not keep the new assembly" + revitSaid() + ".";
    }

    // "NOTHING WAS MADE" IS READ: the assemblies in the model are looked for
    // again, and one that was not there before stops the job.
    if (notMade != null)
    {
        var appeared = 0;
        foreach (var each in new FilteredElementCollector(doc).OfClass(typeof(AssemblyInstance)).ToElementIds())
            if (!assembliesBefore.Contains(each)) appeared++;
        if (appeared > 0)
            throw new InvalidOperationException(notMade + " And " + appeared + " assembly(ies) that were "
                + "not in the model before are in it now." + keptNothing);
        refused = notMade + " NOTHING WAS MADE.";
    }
}

// ---------------------------------------------------------------------------
// 2. NAME IT, AND READ IT BACK - in a second transaction, after the commit.
//    From here on there is an assembly in the group, so every refusal THROWS.
// ---------------------------------------------------------------------------
if (refused == null)
{
    string stopped = null;      // why, in Revit's words - thrown below
    var readBack = new List<ElementId>();
    string typeName = null;

    using (var naming = new Transaction(doc, "Heron: name assembly"))
    {
        naming.Start();
        discipline(naming);
        try
        {
            var made = doc.GetElement(madeId) as AssemblyInstance;
            var madeTypeId = made == null ? ElementId.InvalidElementId : (made.GetTypeId() ?? ElementId.InvalidElementId);
            string nameNow = null;

            // THE TYPE FIRST - the one thing not measured. Asked before any name
            // is tried, so that Revit not having assigned a type is never
            // reported as a name it refused.
            if (made == null)
            {
                stopped = "Revit kept the new assembly, and it was not in the model afterwards.";
            }
            else if (madeTypeId == ElementId.InvalidElementId || doc.GetElement(madeTypeId) == null)
            {
                stopped = "Revit made the assembly but had not given it an assembly type after the "
                    + "transaction that made it was committed, so it could not be named or used.";
            }
            else
            {
                try { nameNow = made.AssemblyTypeName; }
                catch (Exception ex)
                {
                    stopped = "Revit made the assembly but had not given it a usable assembly type after "
                        + "the transaction that made it was committed (" + words(ex) + "), so it could "
                        + "not be named or used.";
                }
            }

            if (stopped == null && wantedName != "" && !string.Equals(nameNow, wantedName, StringComparison.Ordinal))
            {
                // WHOSE TYPE IS IT? See the header.
                var sharing = typeOfExisting.Count(pair => pair.Value == madeTypeId);
                var taken = nameOfType.Any(pair => pair.Key != madeTypeId
                    && string.Equals(pair.Value, wantedName, StringComparison.OrdinalIgnoreCase));
                if (sharing > 0)
                {
                    stopped = "Revit matched the new assembly to the assembly type \"" + nameNow + "\", which "
                        + sharing + " assembly(ies) already in the model share. Naming it \"" + wantedName
                        + "\" would rename all of them, so it was not named. Ask again with no name, or with \""
                        + nameNow + "\".";
                }
                else if (taken)
                {
                    stopped = "Another assembly type in this model is already called \"" + wantedName
                        + "\", so the new assembly was not given that name. Ask again with a name no "
                        + "assembly has.";
                }
                else
                {
                    // REVIT'S OWN WORDS ARE KEPT: a character Revit does not
                    // allow in a name is said as Revit says it.
                    try { made.AssemblyTypeName = wantedName; }
                    catch (Exception ex) { stopped = "Revit refused the name \"" + wantedName + "\": " + words(ex) + "."; }
                }
            }

            if (stopped == null)
            {
                doc.Regenerate();
                typeName = made.AssemblyTypeName;
                readBack.AddRange(made.GetMemberIds());

                if (string.IsNullOrEmpty(typeName))
                    stopped = "The assembly read back with no name.";
                else if (wantedName != "" && !string.Equals(typeName, wantedName, StringComparison.Ordinal))
                    stopped = "The assembly read back named \"" + typeName + "\", not \"" + wantedName + "\".";
                else if (readBack.Count == 0)
                    stopped = "The assembly read back with no members.";
                else
                {
                    // AND NO OTHER ASSEMBLY'S NAME MOVED. Read, because the
                    // guard above is reasoning and this is the model.
                    foreach (var known in typeOfExisting)
                    {
                        var other = doc.GetElement(known.Key) as AssemblyInstance;
                        string otherName = null;
                        try { otherName = other == null ? null : other.AssemblyTypeName; } catch (Exception) { }
                        if (other == null || !string.Equals(otherName ?? "", nameOfType[known.Value], StringComparison.Ordinal))
                        {
                            stopped = "Naming the new assembly changed an assembly that was already in the "
                                + "model: one called \"" + nameOfType[known.Value] + "\" now reads \""
                                + (otherName ?? "<gone>") + "\".";
                            break;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            stopped = "Revit would not let the new assembly be named or read: " + words(ex) + ".";
        }

        if (stopped != null)
            naming.RollBack();
        else if (naming.Commit() != TransactionStatus.Committed)
            stopped = "Revit would not keep the assembly's name" + revitSaid() + ".";
    }

    // OUTSIDE EVERY TRY, so that it leaves the fragment. See the header.
    if (stopped != null)
        throw new InvalidOperationException(stopped + keptNothing);

    // ONLY NOW IS ANYTHING REPORTED AS MADE.
    assembly = doc.GetElement(madeId) as AssemblyInstance;
    if (assembly == null)
        throw new InvalidOperationException("The assembly was made and named, and is not in the model "
            + "afterwards." + keptNothing);
    memberIds.AddRange(readBack);
    findings.Add("Made assembly \"" + typeName + "\" with " + memberIds.Count + " member(s), read "
        + "back, from " + ids.Count + " offered" + (alreadyInOne.Count > 0 ? "; " + alreadyInOne.Count + " already in an assembly left out." : "."));
    if (memberIds.Count != ids.Count)
        findings.Add("Revit's assembly holds " + memberIds.Count + " member(s) where " + ids.Count
            + " were offered - look at it in the Project Browser before it is used.");
}

if (refused != null) findings.Add(refused);
