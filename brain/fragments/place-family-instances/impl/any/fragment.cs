// NOT STANDALONE. Assumes `doc`, `symbol`, `points` and `level` are in scope,
// and leaves `placed` and `failed` behind.
//
// ASSUMES AN OPEN TRANSACTION. Golden Rule 16 - one layout is one undo entry,
// so the TransactionGroup belongs to the operation and not to this fragment.
//
// ACTIVATE THE SYMBOL FIRST. THIS IS THE WHOLE REASON THIS IS A FRAGMENT.
//
// A FamilySymbol that has never been placed in this document is INACTIVE. Ask
// NewFamilyInstance for one anyway and it throws, or on some releases returns
// an instance that is not really there. Either way the caller sees a failure
// with no obvious cause, in code that looks correct, and the fix is one line
// that is only obvious once somebody has lost an afternoon to it.
//
// Activate() must also be followed by Regenerate() before the symbol is used -
// the activation is not visible to the rest of the API until the document has
// regenerated. Doing it once, outside the loop, rather than per point.
//
// POINTS ARE IN INTERNAL FEET. No conversion here (D-20): whoever worked out
// the spacing in millimetres converted once, where the user was.
//
// WHAT HAPPENS WHEN REVIT REFUSES ONE POINT, AND WHY THERE IS NO try/catch.
//
// A null return is counted - that costs nothing and loses nothing. A refusal
// that THROWS is deliberately left to propagate, and this is a real decision
// rather than an omission:
//
//   Golden Rule 16 and Phase 1's own definition of done say a failed operation
//   LEAVES THE MODEL UNTOUCHED. The operation above owns the TransactionGroup,
//   so an exception here rolls the whole layout back and the user gets a clean
//   model and a reason. Swallowing it here would quietly convert that into a
//   partial layout - 37 sprinklers placed, 3 missing, nothing rolled back and
//   no transaction aware anything went wrong.
//
//   WHETHER A LAYOUT SHOULD BE PARTIAL OR ALL-OR-NOTHING IS THE OWNER'S CALL,
//   not this fragment's. A modeller may well prefer 37 and a list of 3. That is
//   a question to ask, not to answer here by choosing which exceptions to hide.
//
// It also keeps this file to UNQUALIFIED type names, as every other fragment
// here does. Catching Revit's own exception type needs its full namespace, and
// that namespace outside revit/ trips the adapter boundary check - which fired
// on the first draft of this fragment, and then fired again on the COMMENT that
// explained why, exactly as it once did on the checker itself.

var placed = new List<ElementId>();
var failed = 0;

if (!symbol.IsActive)
{
    symbol.Activate();
    doc.Regenerate();
}

foreach (var point in points)
{
    if (point == null) { failed++; continue; }

    var instance = doc.Create.NewFamilyInstance(
        point, symbol, level, StructuralType.NonStructural);

    // A null return is not an exception and is not a placement. Counted as a
    // failure rather than added as an id nothing can resolve.
    if (instance == null) failed++;
    else placed.Add(instance.Id);
}
