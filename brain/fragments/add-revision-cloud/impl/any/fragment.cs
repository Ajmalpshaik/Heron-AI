// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `revisionId` and
// `paddingMm` are in scope; leaves `created`, `refused`, `viewRefused` and
// `refusalReasons` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20).
//
// ONE CLOUD PER ELEMENT, NOT ONE AROUND THE SET, and this is the decision most
// worth arguing with. A single cloud spanning scattered elements covers
// everything between them, and a revision cloud is a STATEMENT THAT THE WORK
// INSIDE IT CHANGED. Over-clouding says work was revised that was not, on a
// drawing somebody signs; under-clouding gives three tight clouds where one
// loose one would have done, which is untidy and true. Between an untidy
// drawing and a false one, this takes untidy.
//
// TWO CAUSES USED TO SHARE ONE BOOL, AND NEITHER SAID WHY.
//
// Until 2026-09-09 `viewRefused` was set both when the VIEW could not carry a
// cloud and when the REVISION did not exist, and this file's own comment
// admitted the conflation. Those are opposite fixes - change the view, or make
// the revision - and a caller told only `viewRefused true` cannot tell which,
// so it goes looking at the drawing when the revision is what is missing.
//
// `viewRefused` now means exactly its name. A missing revision is its own
// sentence, and `refusalReasons` carries the words for both, along with the
// three ways a single element can miss out. Section 3h.1 of
// docs/FRAGMENT-ISSUES.md: a fragment that cannot use its input must refuse
// AND SAY WHY - and this one refused without ever saying why.
//
// AND AN EMPTY SET IS A REFUSAL TOO. Handed nothing, with a good view and a
// real revision, it used to answer `created 0`, `refused 0`, `viewRefused
// false` - four zeroes that read as "there was nothing to cloud" and mean "you
// gave me nothing".
//
// THE PER-ELEMENT REASONS ARE COUNTED, NOT LISTED ONE BY ONE. Three hundred
// elements outside the view would be three hundred identical sentences; one
// sentence carrying the count is the same information and can be read.
//
// THE VIEW HAS TO BE ONE A CLOUD CAN LIVE IN, and the ones that are refused
// are refused for different reasons:
//
//   a 3D view      Revit's own Revision Cloud tool is unavailable there. A
//                  cloud is a 2D annotation and has no plane to sit in
//   a schedule     has no geometry at all
//   a SHEET        this is the surprising one, because clouding straight onto
//                  a sheet is normal practice. A sheet's coordinates are PAPER
//                  coordinates and the elements' bounding boxes are MODEL
//                  coordinates; mapping between them needs each viewport's own
//                  transform and scale. Drawing paper-sized rectangles from
//                  model numbers would put a cloud kilometres off the sheet,
//                  so it refuses rather than produce that
//   a template     is not a drawing anybody issues
//
// THE BOX IS ASKED FOR IN THIS VIEW, not in the model. get_BoundingBox(view)
// returns null when the element is not visible there, which is exactly the
// right refusal: a cloud around something the drawing does not show marks a
// change nobody can see.

var padding = paddingMm / 304.8;

var created = new List<ElementId>();
var refused = new List<ElementId>();
var refusalReasons = new List<string>();
var viewRefused = false;

var revision = doc.GetElement(revisionId) as Revision;

// The view's own name, so the sentence names the drawing the caller is looking
// at rather than making them work out which view they passed.
var viewName = "the view given";
try { if (view != null) viewName = "'" + view.Name + "'"; }
catch { viewName = "the view given"; }

// WHICH of the five, not merely that one of them happened. Each has a
// different fix, and the header above says what each one is.
string viewProblem = null;

if (view == null)
    viewProblem = "no view was given, and a cloud is drawn ON a view";
else if (view.IsTemplate)
    viewProblem = viewName + " is a view TEMPLATE, which is not a drawing anybody issues. "
        + "Cloud the view the template drives";
else if (view is View3D)
    viewProblem = viewName + " is a 3D view. A cloud is a 2D annotation and has no plane to "
        + "sit in there - Revit's own Revision Cloud tool is unavailable in a 3D view too";
else if (view is ViewSchedule)
    viewProblem = viewName + " is a schedule, which has no geometry to draw a cloud around";
else if (view is ViewSheet)
    viewProblem = viewName + " is a SHEET, and this is the surprising one because clouding "
        + "straight onto a sheet is normal practice. A sheet's coordinates are PAPER "
        + "coordinates and these elements' bounding boxes are MODEL coordinates; mapping "
        + "between them needs each viewport's own transform and scale. Cloud the view inside "
        + "the viewport instead - drawing paper-sized rectangles from model numbers would put "
        + "the cloud kilometres off the sheet";

// A SEPARATE SENTENCE, because the fix is separate. `viewRefused` is not set
// for this: there may be nothing wrong with the view at all.
var revisionMissing = revision == null;

if (viewProblem != null)
{
    viewRefused = true;
    refusalReasons.Add(viewProblem);
}

if (revisionMissing)
{
    refusalReasons.Add("there is no revision in this project with the id given, and this "
        + "fragment will not invent one - a cloud carrying no revision is a shape on a "
        + "drawing that no register mentions. CREATE_REVISION is what makes one");
}

var handed = 0;
foreach (var element in elements)
{
    if (element != null) handed++;
}

if (viewProblem == null && !revisionMissing && handed == 0)
{
    refusalReasons.Add("nothing was handed in to cloud. NOTHING WAS DRAWN - a cloud is drawn "
        + "around elements, and there were none");
}

// The three ways a single element misses out, counted rather than listed: one
// sentence per cause carrying its count, not one sentence per element.
var notVisibleHere = 0;
var tooSmallToCloud = 0;
var revitDeclined = 0;

if (viewProblem != null || revisionMissing)
{
    // Everything is refused together. Reporting each element as individually
    // refused would send somebody looking at the elements, and the reasons
    // above already say where to look instead.
    foreach (var element in elements)
    {
        if (element != null) refused.Add(element.Id);
    }
}
else
{
    var origin = view.Origin;
    var right = view.RightDirection;
    var up = view.UpDirection;

    foreach (var element in elements)
    {
        if (element == null) continue;

        var box = element.get_BoundingBox(view);
        if (box == null) { refused.Add(element.Id); notVisibleHere++; continue; }

        // Every corner is projected onto the view's own axes. Using the box's
        // X and Y directly would be right only for a view square to the world,
        // and a section through a duct run at 30 degrees is not.
        double minU = 0, maxU = 0, minV = 0, maxV = 0;
        bool first = true;

        for (int i = 0; i < 8; i++)
        {
            var corner = new XYZ((i & 1) == 0 ? box.Min.X : box.Max.X,
                                 (i & 2) == 0 ? box.Min.Y : box.Max.Y,
                                 (i & 4) == 0 ? box.Min.Z : box.Max.Z);
            var offset = corner - origin;

            var u = offset.DotProduct(right);
            var v = offset.DotProduct(up);

            if (first) { minU = maxU = u; minV = maxV = v; first = false; }
            else
            {
                minU = Math.Min(minU, u); maxU = Math.Max(maxU, u);
                minV = Math.Min(minV, v); maxV = Math.Max(maxV, v);
            }
        }

        minU -= padding; maxU += padding;
        minV -= padding; maxV += padding;

        // A cloud with no size is not a cloud, and Revit rejects a line shorter
        // than its own tolerance. A zero-padding request against a
        // point-shaped element is a real way to arrive here.
        if ((maxU - minU) < 1e-6 || (maxV - minV) < 1e-6)
        {
            refused.Add(element.Id);
            tooSmallToCloud++;
            continue;
        }

        // Built ON THE VIEW'S PLANE, which is what makes these curves legal
        // input: the model corners are dropped onto it by keeping only their
        // components along the view's own two axes.
        var a = origin + right * minU + up * minV;
        var b = origin + right * maxU + up * minV;
        var c = origin + right * maxU + up * maxV;
        var d = origin + right * minU + up * maxV;

        // A closed loop, in order. Revit turns these into the cloud's arcs; an
        // open or out-of-order loop is rejected as a shape rather than drawn
        // wrongly, which is the one mercy in this API.
        var outline = new List<Curve>();
        outline.Add(Line.CreateBound(a, b));
        outline.Add(Line.CreateBound(b, c));
        outline.Add(Line.CreateBound(c, d));
        outline.Add(Line.CreateBound(d, a));

        var cloud = RevisionCloud.Create(doc, view, revisionId, outline);
        if (cloud == null) { refused.Add(element.Id); revitDeclined++; continue; }

        created.Add(cloud.Id);
    }

    // One sentence per cause, each carrying its count. Written after the loop
    // so the counts are final.
    if (notVisibleHere > 0)
    {
        refusalReasons.Add(string.Format(
            "{0} element(s) are not visible in {1}, so there was nothing to draw around. The "
            + "box is asked for IN THIS VIEW on purpose - a cloud around something the drawing "
            + "does not show marks a change nobody can see", notVisibleHere, viewName));
    }

    if (tooSmallToCloud > 0)
    {
        refusalReasons.Add(string.Format(
            "{0} element(s) came out with no width or no height once projected onto {1}, and a "
            + "cloud with no size is not a cloud. Give a padding - a zero padding against a "
            + "point-shaped element arrives here every time", tooSmallToCloud, viewName));
    }

    if (revitDeclined > 0)
    {
        refusalReasons.Add(string.Format(
            "{0} element(s) had a rectangle Revit itself would not turn into a cloud",
            revitDeclined));
    }
}
