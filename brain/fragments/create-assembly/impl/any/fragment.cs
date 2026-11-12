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
// NOT MEASURED, AND THE WHOLE CARD RESTS ON IT: whether Revit assigns the type
// at a commit that is INSIDE an open TransactionGroup. The remark says "the
// transaction", and the stair cards show a commit inside the group is a real
// commit - but no run has shown an assembly named this way. If Revit does not,
// step 2 fails, and what follows is what makes that safe.
//
// ALL OR NOTHING, AND THE NOTHING IS READ. Anything that fails after step 1 -
// the name refused, the type not there, the members not readable - takes the
// assembly apart again in a third transaction (Disassemble, which removes the
// assembly and RELEASES its members; Delete would take the members with it).
// Then the model is READ: the assembly gone, its type gone, every element back
// in no assembly. Only then is "NOTHING WAS MADE" said. If any of that does
// not read so, this THROWS, and the add-in rolls the whole group back and
// says so - the one thing that can still restore the model exactly.
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
var madeTypeId = ElementId.InvalidElementId;

// ---------------------------------------------------------------------------
// 1. MAKE IT, AND COMMIT - Revit assigns the type at this commit, not before.
// ---------------------------------------------------------------------------
if (refused == null)
{
    using (var making = new Transaction(doc, "Heron: create assembly"))
    {
        making.Start();
        discipline(making);
        try
        {
            var made = AssemblyInstance.Create(doc, ids, categoryId);
            if (made == null) refused = "Revit returned no assembly, so NOTHING WAS MADE.";
            else madeId = made.Id;
        }
        catch (Exception ex)
        {
            refused = "Revit refused the assembly: " + words(ex) + ". NOTHING WAS MADE.";
        }

        if (refused != null)
            making.RollBack();
        else if (making.Commit() != TransactionStatus.Committed)
            refused = "Revit would not keep the new assembly" + revitSaid() + ". NOTHING WAS MADE.";
    }

    // A transaction Revit would not keep is one Revit rolled back - and that
    // is read, like every other "nothing" here.
    if (refused != null && madeId != ElementId.InvalidElementId && doc.GetElement(madeId) != null)
        throw new InvalidOperationException("Revit would not keep the new assembly, and it is still in "
            + "the model. Heron stopped here so that the whole job is rolled back.");
}

// ---------------------------------------------------------------------------
// 2. NAME IT, AND READ IT BACK - in a second transaction, after the commit.
// ---------------------------------------------------------------------------
string afterMaking = null;      // what went wrong once the assembly was already kept
if (refused == null)
{
    var readBack = new List<ElementId>();
    string typeName = null;
    using (var naming = new Transaction(doc, "Heron: name assembly"))
    {
        naming.Start();
        discipline(naming);
        try
        {
            var made = doc.GetElement(madeId) as AssemblyInstance;
            if (made == null)
            {
                afterMaking = "the assembly was not there to name once Revit had kept it";
            }
            else
            {
                madeTypeId = made.GetTypeId() ?? ElementId.InvalidElementId;
                if (wantedName != "")
                {
                    // REVIT'S OWN WORDS ARE KEPT: a name already taken, or one
                    // with a character Revit does not allow, is said as Revit
                    // says it.
                    try { made.AssemblyTypeName = wantedName; }
                    catch (Exception ex) { afterMaking = "Revit refused the name \"" + wantedName + "\": " + words(ex); }
                }

                if (afterMaking == null)
                {
                    doc.Regenerate();
                    typeName = made.AssemblyTypeName;
                    readBack.AddRange(made.GetMemberIds());
                    madeTypeId = made.GetTypeId() ?? ElementId.InvalidElementId;

                    if (string.IsNullOrEmpty(typeName) || madeTypeId == ElementId.InvalidElementId)
                        afterMaking = "the assembly read back with no type and no name";
                    else if (wantedName != "" && !string.Equals(typeName, wantedName, StringComparison.Ordinal))
                        afterMaking = "the assembly read back named \"" + typeName + "\", not \"" + wantedName + "\"";
                    else if (readBack.Count == 0)
                        afterMaking = "the assembly read back with no members";
                }
            }
        }
        catch (Exception ex)
        {
            afterMaking = "Revit would not let the new assembly be named or read: " + words(ex);
        }

        if (afterMaking != null)
            naming.RollBack();
        else if (naming.Commit() != TransactionStatus.Committed)
            afterMaking = "Revit would not keep the assembly's name" + revitSaid();
    }

    if (afterMaking == null)
    {
        // ONLY NOW IS ANYTHING REPORTED AS MADE.
        assembly = doc.GetElement(madeId) as AssemblyInstance;
        if (assembly == null)
            throw new InvalidOperationException("The assembly was made and named, and is not in the model "
                + "afterwards. Heron stopped here so that the whole job is rolled back.");
        memberIds.AddRange(readBack);
        findings.Add("Made assembly \"" + typeName + "\" with " + memberIds.Count + " member(s), read "
            + "back, from " + ids.Count + " offered" + (alreadyInOne.Count > 0 ? "; " + alreadyInOne.Count + " already in an assembly left out." : "."));
        if (memberIds.Count != ids.Count)
            findings.Add("Revit's assembly holds " + memberIds.Count + " member(s) where " + ids.Count
                + " were offered - look at it in the Project Browser before it is used.");
    }
}

// ---------------------------------------------------------------------------
// 3. ALL OR NOTHING - and the nothing is READ. See the header.
// ---------------------------------------------------------------------------
if (refused == null && afterMaking != null)
{
    string notTakenApart = null;
    using (var undoing = new Transaction(doc, "Heron: take the assembly apart again"))
    {
        undoing.Start();
        discipline(undoing);
        try
        {
            var made = doc.GetElement(madeId) as AssemblyInstance;
            // Disassemble, never Delete: deleting an assembly takes its
            // members with it.
            if (made != null) made.Disassemble();
        }
        catch (Exception ex) { notTakenApart = words(ex); }

        if (notTakenApart != null)
            undoing.RollBack();
        else if (undoing.Commit() != TransactionStatus.Committed)
            notTakenApart = "Revit would not keep the change" + revitSaid();
    }

    // READ, NOT ASSUMED. Outside every try on purpose: what is thrown here has
    // to leave the fragment, so that the add-in rolls the whole group back.
    var assemblyLeft = doc.GetElement(madeId) != null;
    var typeLeft = madeTypeId != ElementId.InvalidElementId && doc.GetElement(madeTypeId) != null;
    var notReleased = 0;
    foreach (var id in ids)
    {
        var member = doc.GetElement(id);
        if (member == null
            || (member.AssemblyInstanceId != null && member.AssemblyInstanceId != ElementId.InvalidElementId))
            notReleased++;
    }

    if (assemblyLeft || typeLeft || notReleased > 0)
    {
        throw new InvalidOperationException(string.Format("Revit made the assembly, but {0}. Taking it "
            + "apart again did not leave the model as it was{1}: {2}{3}{4}. Heron stopped here so that "
            + "the whole job is rolled back - nothing of it is kept.",
            afterMaking,
            notTakenApart == null ? "" : " (" + notTakenApart + ")",
            assemblyLeft ? "the assembly is still there; " : "",
            typeLeft ? "its assembly type is still in the project; " : "",
            notReleased > 0 ? notReleased + " element(s) are still in an assembly, or gone" : "the elements are released"));
    }

    refused = "Revit made the assembly, but " + afterMaking + ". It was taken apart again and the model "
        + "read: the assembly and its type are gone and all " + ids.Count + " element(s) are in no "
        + "assembly, as before. NOTHING WAS MADE.";
}

if (refused != null) findings.Add(refused);
