// NOT STANDALONE. Assumes `view`, `topMm`, `cutMm`, `bottomMm` and `apply` are
// in scope; leaves `wasTopMm`, `wasCutMm`, `wasBottomMm`, `applied` and
// `viewRefused` behind.
//
// ASSUMES AN OPEN TRANSACTION when `apply` is true (Golden Rule 16). With
// `apply` false it is a pure read and needs none.
//
// THE OLD VALUES COME BACK WHETHER OR NOT ANYTHING IS SET. That is the point of
// the `apply` flag: the same fragment answers "what is the view range" and
// "change it", and the second reports what it changed FROM. Setting a view
// range blind is how a coordinated drawing quietly starts showing the storey
// above, and the way back is the number this returns.
//
// OFFSETS ARE RELATIVE TO THE LEVEL the range is measured from, which is what
// a modeller means by "2300 above the level". No level is changed here.
//
// MILLIMETRES TO FEET IS ARITHMETIC (D-20): 1 ft = 304.8 mm exactly.

var wasTopMm = 0.0;
var wasCutMm = 0.0;
var wasBottomMm = 0.0;
var applied = false;
var viewRefused = false;

const double MillimetresPerFoot = 304.8;

var plan = view as ViewPlan;
if (plan == null || plan.IsTemplate)
{
    // A section, an elevation, a 3D view, a schedule - none has a view range.
    // Refused by name: a silent no-op reads as the tool being broken while the
    // user waits for the plan to change.
    viewRefused = true;
}
else
{
    var range = plan.GetViewRange();

    wasTopMm = range.GetOffset(PlanViewPlane.TopClipPlane) * MillimetresPerFoot;
    wasCutMm = range.GetOffset(PlanViewPlane.CutPlane) * MillimetresPerFoot;
    wasBottomMm = range.GetOffset(PlanViewPlane.BottomClipPlane) * MillimetresPerFoot;

    if (apply)
    {
        range.SetOffset(PlanViewPlane.TopClipPlane, topMm / MillimetresPerFoot);
        range.SetOffset(PlanViewPlane.CutPlane, cutMm / MillimetresPerFoot);
        range.SetOffset(PlanViewPlane.BottomClipPlane, bottomMm / MillimetresPerFoot);

        plan.SetViewRange(range);

        // READ BACK from the view, not from the object that was handed in.
        // Revit rejects an incoherent range - a cut plane above the top, a
        // bottom above the cut - and it does so without always throwing.
        var now = plan.GetViewRange();
        var top = now.GetOffset(PlanViewPlane.TopClipPlane) * MillimetresPerFoot;
        var cut = now.GetOffset(PlanViewPlane.CutPlane) * MillimetresPerFoot;

        applied = Math.Abs(top - topMm) < 1.0 && Math.Abs(cut - cutMm) < 1.0;
    }
}
