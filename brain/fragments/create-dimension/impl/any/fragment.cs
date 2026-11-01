// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `axis` and `offsetMm` are
// in scope; leaves `created`, `refused`, `notLinear`, `noReference`,
// `dimensionedCount` and `problem` behind.
//
// VERSION 3 (2026-11-01) - DIMENSION_FAMILY_INSTANCES MERGED IN, AND WHAT IT
// HAD THAT THIS LACKED:
//
//   THE FAMILY-INSTANCE ROUTE, BESIDE THE CENTRELINE WALK. An air terminal, a
//   fixture, a piece of equipment has no centreline; it publishes BUILT-IN
//   CENTRE REFERENCES - CenterLeftRight, CenterFrontBack - and a strong
//   reference on its body. Along `x` the CenterLeftRight is asked for first,
//   along `y` the CenterFrontBack, then the strong reference: centre
//   references are what make a dimension HOLD when the element later moves.
//   The instances are sorted along the axis first, so the string reads
//   left-to-right or bottom-to-top as a drafter would draw it, and it sits
//   `offsetMm` square to the axis. An axis MUST be named for them - x or y -
//   because "across" is worked out from runs, and an instance has no run
//   direction; guessing one would measure the wrong way and look plausible.
//
//   `noReference` - instances that publish no centre and no strong reference,
//   duct fittings and accessories among them (all four come back empty).
//   Reported by id; fewer than two left draws nothing.
//
//   A ROOM CANNOT BE DIMENSIONED HERE. A room's boundary is not a family
//   instance and offers none of these references; dimensioning the nearest
//   thing this CAN reach - the fixtures inside it - would put a real string
//   on a drawing nobody asked for. A room, space or area handed in goes to
//   `notLinear` and `problem` says so, naming DIMENSION_ROOMS, which puts room
//   sizes on the plan, and MEASURE_ROOM_DIMENSIONS, which reads them.
//
//   ONE STRING, ONE KIND. Runs and family instances handed in together are
//   REFUSED, nothing drawn, and `problem` says how many of each: a duct's
//   centreline and a diffuser's centre plane in one string has not been
//   measured on any release, and drawing half the set while dropping the rest
//   reads as a finished dimension. Hand each kind in on its own.
//
//   THE SAFER RULES OF THIS CARD APPLY TO BOTH ROUTES: a dimension Revit makes
//   holding fewer than two references is deleted again, never left in the
//   caller's transaction; an axis pointing straight out of the view is
//   refused; Revit's own refusal is caught and said.
//
// VERSION 2 (2026-11-01) - DIMENSION_MEP_RUNS MERGED IN, AND WHAT IT HAD:
//
//   AN AXIS MAY BE NAMED. Empty measures ACROSS the runs, worked out from them
//   as version 1 did - right at any angle. `x` or `y` measures along that axis
//   of the model, which is what reaches RISERS: vertical pipes seen end-on in
//   a plan have no direction across them in the view, and version 1 refused
//   them; along a named axis their centrelines dimension like any other.
//
//   IT READS BACK. `dimensionedCount` is the references the dimension Revit
//   made actually holds - never `Dimension.Curve`, which throws "The input
//   curve is not bound" on a dimension this new. Fewer than two will not
//   survive a commit, and `problem` says so.
//
//   A REFUSAL BY REVIT IS CAUGHT. NewDimension throwing is one dimension's
//   problem and is said in `problem`; let out of here, it would abort the
//   caller's transaction and every change made before it (Golden Rule 16).
//
//   `problem` SAYS WHY NOTHING WAS DRAWN, in one sentence. (Version 2 named
//   DIMENSION_FAMILY_INSTANCES there for air terminals, fixtures and
//   equipment; version 3 dimensions them itself.)
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// MILLIMETRES TO FEET BY ARITHMETIC (D-20).
//
// A REFERENCE ONLY EXISTS IF IT WAS ASKED FOR, and this is the fact the whole
// fragment turns on. Reading an element's geometry the ordinary way returns
// shapes with NO Reference on them - null, silently - and NewDimension needs
// References, not points. Two options have to be set:
//
//   ComputeReferences        without it every Reference is null
//   IncludeNonVisibleObjects an MEP centreline is non-visible geometry, so
//                            without it a duct yields its solid and no line
//
// Set neither and the code runs, finds nothing, draws nothing, and reports no
// error. That is why the refusals here are counted and returned rather than
// letting an empty result look like a quiet success.
//
// THE DIMENSION LINE'S DIRECTION IS WHAT IS BEING MEASURED. NewDimension does
// not measure the gap between references in the abstract; it measures ALONG
// the line it is given. A line parallel to the ducts measures nothing useful.
// So the measuring axis is built perpendicular to the runs and in the view's
// plane: the view's own normal crossed with the run direction, which is the
// only direction that is both across the runs and drawable on this sheet.
//
// EVERY RUN MUST BE PARALLEL TO THE FIRST. A dimension string across ducts
// that are not parallel measures the distance to a point that depends on where
// along the run you happen to look - a number that changes with nothing
// changing. Elements out of parallel go to `refused` rather than being folded
// into the string.

var offset = offsetMm / 304.8;

ElementId created = null;
var refused = new List<ElementId>();
var notLinear = new List<ElementId>();
var noReference = new List<ElementId>();
int dimensionedCount = 0;
string problem = "";

// Left out (bound as "across") or empty is across the runs; x and y are the
// model's own axes.
var wantedAxis = (axis ?? "").Trim().ToLowerInvariant();
if (wantedAxis == "across") wantedAxis = "";
XYZ namedAxis = null;
if (wantedAxis == "x") namedAxis = XYZ.BasisX;
else if (wantedAxis == "y") namedAxis = XYZ.BasisY;
else if (wantedAxis.Length > 0)
    problem = "axis must be x, y or left empty to measure across the runs - guessing one would "
            + "measure the wrong direction and the drawing would still look plausible, so NOTHING "
            + "WAS DRAWN";

// VERSION 3. SORT WHAT WAS HANDED IN BY KIND, before any geometry is read: a
// straight run (a location LINE) takes the centreline walk; a family
// instance without one takes the built-in centre references; a room, space or
// area is refused; anything else is not linear.
var runElements = new List<Element>();
var instances = new List<FamilyInstance>();
int roomsHanded = 0;

foreach (var element in elements)
{
    if (problem.Length > 0) break;
    if (element == null) continue;

    // A ROOM IS NEVER DIMENSIONED HERE - its boundary offers no reference
    // this can hold, and the fixtures inside it are not what was asked for.
    if (element is SpatialElement)
    {
        notLinear.Add(element.Id);
        roomsHanded++;
        continue;
    }

    // A quick structural test before touching geometry. A straight location
    // curve is a run - a duct, pipe, conduit or cable tray.
    var location = element.Location as LocationCurve;
    if (location != null && location.Curve is Line) { runElements.Add(element); continue; }

    var instance = element as FamilyInstance;
    if (instance != null) { instances.Add(instance); continue; }

    notLinear.Add(element.Id);
}

// The room sentence, said whatever else happens: rooms handed in are never
// silently left out of a string that was drawn.
string roomSentence = roomsHanded == 0 ? "" :
    roomsHanded + " room(s), space(s) or area(s) were handed in and CANNOT BE DIMENSIONED HERE - "
    + "a room's boundary offers none of the references this draws to, and dimensioning the "
    + "fixtures inside it would put a string nobody asked for on the drawing. DIMENSION_ROOMS "
    + "puts room sizes on the plan; MEASURE_ROOM_DIMENSIONS reads them";

bool mixed = runElements.Count > 0 && instances.Count > 0;

if (problem.Length > 0)
{
    // An axis that is not one: nothing was looked at, nothing is drawn.
}
else if (mixed)
{
    // ONE STRING, ONE KIND (version 3). Refused whole, nothing drawn.
    foreach (var run in runElements) refused.Add(run.Id);
    foreach (var instance in instances) refused.Add(instance.Id);
    problem = string.Format("{0} run(s) and {1} family instance(s) were handed in together. One "
            + "dimension string is drawn through one kind - run centrelines, or the centre "
            + "references of family instances - and a string mixing the two has not been "
            + "measured, so NOTHING WAS DRAWN. Dimension the runs and the instances in two calls",
            runElements.Count, instances.Count);
}
else if (instances.Count > 0)
{
    // ---- THE FAMILY-INSTANCE ROUTE (version 3, from DIMENSION_FAMILY_INSTANCES)
    //
    // A ZERO HERE MEANS "USE THE OTHER ROUTE", NEVER "IMPOSSIBLE". Duct fittings
    // and accessories publish no references at all, and the first conclusion
    // drawn from that was "MEP cannot be dimensioned" - true of THIS route,
    // not of MEP: ducts and pipes dimension by their centrelines.
    if (namedAxis == null)
    {
        foreach (var instance in instances) refused.Add(instance.Id);
        problem = "family instances are dimensioned along an axis - name x for the horizontal "
                + "spacing in plan or y for the vertical. Across is worked out from runs, and an "
                + "instance has none; guessing one would measure the wrong direction. NOTHING "
                + "WAS DRAWN";
    }
    else if (Math.Abs(namedAxis.DotProduct(view.ViewDirection)) > 0.999)
    {
        foreach (var instance in instances) refused.Add(instance.Id);
        problem = string.Format("the axis named, {0}, points straight out of this view, so a "
                + "dimension along it cannot be drawn here. Name the other axis. NOTHING WAS "
                + "DRAWN", wantedAxis);
    }
    else
    {
        // The reference types worth asking for, in the order that produces the
        // dimension a drafter would place. Centre references are what makes a
        // dimension HOLD when the element later moves; a strong reference on
        // the body does not.
        var wantedTypes = wantedAxis == "x"
            ? new[] { FamilyInstanceReferenceType.CenterLeftRight,
                      FamilyInstanceReferenceType.StrongReference }
            : new[] { FamilyInstanceReferenceType.CenterFrontBack,
                      FamilyInstanceReferenceType.StrongReference };

        // Sorted along the axis first, so the chain reads left-to-right or
        // bottom-to-top the way somebody would draw it by hand.
        var ordered = new List<FamilyInstance>(instances);
        ordered.Sort(delegate (FamilyInstance a, FamilyInstance b)
        {
            var pa = a.Location as LocationPoint;
            var pb = b.Location as LocationPoint;
            if (pa == null || pb == null) return 0;
            double va = wantedAxis == "x" ? pa.Point.X : pa.Point.Y;
            double vb = wantedAxis == "x" ? pb.Point.X : pb.Point.Y;
            return va.CompareTo(vb);
        });

        var array = new ReferenceArray();
        var held = new List<ElementId>();
        XYZ firstPoint = null;
        XYZ lastPoint = null;

        foreach (var instance in ordered)
        {
            Reference picked = null;
            foreach (var type in wantedTypes)
            {
                IList<Reference> found = null;
                try { found = instance.GetReferences(type); } catch { }
                if (found != null && found.Count > 0) { picked = found[0]; break; }
            }

            if (picked == null)
            {
                // No usable reference. Reported by id rather than dropped: a
                // short string with no explanation reads as this choosing, when
                // the content had nothing to hold on to.
                noReference.Add(instance.Id);
                continue;
            }

            array.Append(picked);
            held.Add(instance.Id);
            var point = instance.Location as LocationPoint;
            if (point != null)
            {
                if (firstPoint == null) firstPoint = point.Point;
                lastPoint = point.Point;
            }
        }

        if (array.Size < 2)
        {
            refused.AddRange(held);
            problem = "fewer than two of these expose a reference a dimension can hold on to, so "
                    + "nothing was drawn - a one-reference dimension is degenerate and Revit drops "
                    + "it at commit. If these are duct fittings or accessories, they publish none; "
                    + "ducts and pipes handed in on their own dimension by their centrelines";
        }
        else if (firstPoint == null || lastPoint == null)
        {
            refused.AddRange(held);
            problem = "these have no readable location, so there is no line to put the dimension "
                    + "on. NOTHING WAS DRAWN";
        }
        else
        {
            // `offsetMm` square to the axis, in plan. Millimetres to Revit's
            // internal feet by plain arithmetic (D-20).
            Line line = wantedAxis == "x"
                ? Line.CreateBound(
                      new XYZ(firstPoint.X, firstPoint.Y + offset, firstPoint.Z),
                      new XYZ(lastPoint.X, firstPoint.Y + offset, firstPoint.Z))
                : Line.CreateBound(
                      new XYZ(firstPoint.X + offset, firstPoint.Y, firstPoint.Z),
                      new XYZ(firstPoint.X + offset, lastPoint.Y, firstPoint.Z));

            Dimension made = null;
            try
            {
                made = doc.Create.NewDimension(view, line, array);
            }
            catch (Exception ex)
            {
                problem = "Revit refused the dimension: " + ex.Message + ". NOTHING WAS DRAWN";
            }

            // THE READ-BACK, and NOT via Dimension.Curve - that throws "The
            // input curve is not bound" on a dimension this new.
            if (made != null)
            {
                created = made.Id;
                try { dimensionedCount = made.References == null ? 0 : made.References.Size; }
                catch { dimensionedCount = 0; }
                if (dimensionedCount < 2)
                {
                    // THIS CARD'S SAFER RULE, now on this route too: taken out
                    // again rather than left in the caller's transaction.
                    try { doc.Delete(made.Id); } catch { }
                    created = null;
                    dimensionedCount = 0;
                    problem = "Revit made a dimension holding fewer than two references, which "
                            + "will not survive a commit, so it was taken out again. NOTHING WAS "
                            + "DRAWN";
                }
                else if (dimensionedCount < array.Size)
                {
                    problem = string.Format("the dimension holds {0} of the {1} references asked "
                            + "for - Revit dropped {2}. Check which instances it measures before "
                            + "relying on it", dimensionedCount, array.Size,
                            array.Size - dimensionedCount);
                }
            }
            else if (problem.Length == 0)
            {
                problem = "no dimension was created, and Revit reported no reason";
            }

            if (dimensionedCount < 2) refused.AddRange(held);
        }
    }
}
else
{
// ---- THE CENTRELINE WALK, versions 1 and 2 ---------------------------------
var options = new Options();
options.ComputeReferences = true;
options.IncludeNonVisibleObjects = true;
options.View = view;

XYZ runDirection = null;

var references = new List<Reference>();
var positions = new List<double>();
var anchors = new List<XYZ>();

// The ids that made it into the string, kept so that abandoning the string
// later refuses exactly those and not the ones already refused for their own
// reason. An id appearing twice in `refused` would report more failures than
// there were elements.
var accepted = new List<ElementId>();

// Collected first, measured second: the measuring axis cannot be built until
// the first run's direction is known, and it has to apply to all of them.
var centrelines = new List<KeyValuePair<ElementId, Line>>();

foreach (var element in runElements)
{
    var geometry = element.get_Geometry(options);
    if (geometry == null) { refused.Add(element.Id); continue; }

    Line centreline = null;

    foreach (var part in geometry)
    {
        // The centreline comes back as a Line carrying a Reference. Anything
        // without one is unusable here however right it looks: NewDimension
        // takes References and a Line with a null Reference cannot be given
        // to it.
        var line = part as Line;
        if (line == null) continue;
        if (line.Reference == null) continue;

        centreline = line;
        break;
    }

    if (centreline == null) { refused.Add(element.Id); continue; }

    centrelines.Add(new KeyValuePair<ElementId, Line>(element.Id, centreline));
}

foreach (var entry in centrelines)
{
    var centreline = entry.Value;

    var along = centreline.GetEndPoint(1) - centreline.GetEndPoint(0);
    if (along.GetLength() < 1e-9) { refused.Add(entry.Key); continue; }
    along = along.Normalize();

    if (runDirection == null) runDirection = along;
    else if (Math.Abs(along.DotProduct(runDirection)) < 0.999)
    {
        // Out of parallel by more than about two and a half degrees. The
        // distance between two non-parallel runs is not one number, so there
        // is nothing honest to put in the string.
        refused.Add(entry.Key);
        continue;
    }

    references.Add(centreline.Reference);
    anchors.Add(centreline.GetEndPoint(0));
    accepted.Add(entry.Key);
}

// Two references is the minimum a dimension can hold. One is not a short
// dimension, it is not a dimension.
if (references.Count >= 2 && runDirection != null)
{
    // ACROSS THE RUNS, unless an axis was named. The view's own normal crossed
    // with the run direction is the only direction that is both across the
    // runs and drawable on this sheet.
    var across = namedAxis ?? view.ViewDirection.CrossProduct(runDirection);

    // Zero length means the runs point straight at the viewer - ducts seen
    // end-on in a section, risers in a plan. There is no direction across
    // them that can be drawn on this view, so the whole string is refused
    // rather than drawn along an arbitrary axis - unless an axis was named.
    if (across.GetLength() < 1e-9)
    {
        refused.AddRange(accepted);
        problem = "these runs point straight at the viewer - risers in a plan, or ducts seen end-on "
                + "- so there is no direction across them on this view. Name the axis, x or y, to "
                + "dimension them along it. NOTHING WAS DRAWN";
    }
    else if (namedAxis != null && Math.Abs(namedAxis.DotProduct(view.ViewDirection)) > 0.999)
    {
        // The axis named points straight out of this view - y in a section
        // looking north. A dimension line along it cannot be drawn on the view.
        refused.AddRange(accepted);
        problem = string.Format("the axis named, {0}, points straight out of this view, so a "
                + "dimension along it cannot be drawn here. Name the other axis, or leave it out. "
                + "NOTHING WAS DRAWN", wantedAxis);
    }
    else if (namedAxis != null && Math.Abs(namedAxis.DotProduct(runDirection)) > 0.999)
    {
        // The axis named runs ALONG the runs. A dimension measured along
        // parallel lines has nothing to measure between them.
        refused.AddRange(accepted);
        problem = string.Format("the axis named, {0}, runs along these runs rather than across them, "
                + "and a dimension along parallel runs has nothing to measure between them. Name the "
                + "other axis, or leave it out. NOTHING WAS DRAWN", wantedAxis);
    }
    else
    {
        across = across.Normalize();

        foreach (var anchor in anchors) positions.Add(anchor.DotProduct(across));

        double lowest = positions[0], highest = positions[0];
        for (int i = 1; i < positions.Count; i++)
        {
            lowest = Math.Min(lowest, positions[i]);
            highest = Math.Max(highest, positions[i]);
        }

        // The string sits OFFSET clear of the elements it measures: along the
        // runs when measuring across them, and square to the named axis in the
        // view's plane when one was named - for risers, the run direction is
        // the view's own and an offset along it would not move the line on
        // the drawing. It spans from the outermost reference to the outermost
        // so every witness line has something to land on.
        var aside = runDirection;
        if (namedAxis != null)
        {
            var square = view.ViewDirection.CrossProduct(across);
            if (square.GetLength() > 1e-9) aside = square.Normalize();
        }
        var basePoint = anchors[0] - across * anchors[0].DotProduct(across) + aside * offset;

        var from = basePoint + across * lowest;
        var to = basePoint + across * highest;

        if ((to - from).GetLength() > 1e-6)
        {
            var dimensionLine = Line.CreateBound(from, to);

            var array = new ReferenceArray();
            foreach (var reference in references) array.Append(reference);

            Dimension dimension = null;
            try
            {
                dimension = doc.Create.NewDimension(view, dimensionLine, array);
            }
            catch (Exception ex)
            {
                problem = "Revit refused the dimension: " + ex.Message + ". NOTHING WAS DRAWN";
            }

            // THE READ-BACK, and NOT via Dimension.Curve - that throws "The
            // input curve is not bound" on a dimension this new.
            if (dimension != null)
            {
                created = dimension.Id;
                try { dimensionedCount = dimension.References == null ? 0 : dimension.References.Size; }
                catch { dimensionedCount = 0; }
                if (dimensionedCount < 2)
                {
                    // Degenerate, and it would not survive the commit. Taken
                    // out again rather than left in the caller's transaction
                    // while every run is reported refused - two answers that
                    // contradict each other.
                    try { doc.Delete(dimension.Id); } catch { }
                    created = null;
                    dimensionedCount = 0;
                    problem = "Revit made a dimension holding fewer than two references, which will "
                            + "not survive a commit, so it was taken out again. NOTHING WAS DRAWN";
                }
                else if (dimensionedCount < references.Count)
                {
                    problem = string.Format("the dimension holds {0} of the {1} centrelines asked for - "
                            + "Revit dropped {2}. Check which runs it measures before relying on it",
                            dimensionedCount, references.Count, references.Count - dimensionedCount);
                }
            }
            else if (problem.Length == 0)
            {
                problem = "no dimension was created, and Revit reported no reason";
            }

            if (dimensionedCount < 2) refused.AddRange(accepted);
        }
        else
        {
            // Every run sits on the same line across - they are collinear, so
            // the gap being dimensioned is zero and there is nothing to draw.
            refused.AddRange(accepted);
            problem = "every run sits on one line across, so the gap to dimension is zero. NOTHING "
                    + "WAS DRAWN";
        }
    }
}
else
{
    // Fewer than two usable references. One reference is not a short
    // dimension, it is not a dimension.
    refused.AddRange(accepted);
    if (roomsHanded > 0 && runElements.Count == 0 && notLinear.Count == roomsHanded)
    {
        // Only rooms were handed in: the room sentence below is the whole answer.
        problem = "NOTHING WAS DRAWN";
    }
    else if (runElements.Count == 0)
    {
        problem = "nothing handed in is a straight run - a duct, pipe, conduit or cable tray - or a "
                + "family instance - an air terminal, fixture or piece of equipment - so there is "
                + "nothing here to dimension. NOTHING WAS DRAWN";
    }
    else
    {
        problem = "fewer than two of these gave up a centreline parallel to the first, so nothing "
                + "was drawn - a one-reference dimension is degenerate and Revit drops it at "
                + "commit. If these are air terminals or equipment, hand them in on their own with "
                + "an axis, x or y";
    }
}
}

// A ROOM HANDED IN IS SAID WHATEVER ELSE HAPPENED (version 3).
if (roomSentence.Length > 0)
    problem = problem.Length == 0 ? roomSentence : problem + ". " + roomSentence;
