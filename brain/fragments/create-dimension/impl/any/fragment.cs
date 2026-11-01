// NOT STANDALONE. Assumes `doc`, `view`, `elements`, `axis` and `offsetMm` are
// in scope; leaves `created`, `refused`, `notLinear`, `noReference`,
// `notVisibleInView`, `dimensionedCount` and `problem` behind.
//
// VERSION 4 (2026-11-01) - DIMENSION_GRIDS_AND_LEVELS MERGED IN, AND WHAT IT
// HAD THAT THIS LACKED:
//
//   GRIDS AND LEVELS, A THIRD ROUTE. A grid, a level and a reference plane are
//   the three things Revit lets a dimension reference AS THE WHOLE ELEMENT -
//   new Reference(grid) - with no geometry read; the centreline walk returns
//   nothing for a grid. Grids are measured across the plan (the view normal
//   crossed with the grid direction), levels up the elevation, each level
//   placed by its ProjectElevation and never its Elevation (FRAGMENT-ISSUES
//   5b-179: Elevation is off by the Elevation Base in model coordinates).
//   Reference planes are left out on purpose and go to notLinear.
//
//   `notVisibleInView` - a datum the view is not showing is set aside BEFORE
//   anything is drawn: Revit throws when asked to dimension to it, and that
//   throw takes the whole string down. Said in `problem` whatever else
//   happens.
//
//   An arc grid, or a grid out of parallel, is refused by id. Levels in a
//   plan are refused - the plan looks straight down the axis the string
//   would measure. Grids and levels are never one string.
//
//   THE SAFER RULE IN EACH CONFLICT IS KEPT. That card dimensioned the grids
//   and set a wall or a duct handed in with them aside; here any two kinds
//   handed in together - runs, family instances, grids, levels - are refused
//   whole, naming each, nothing drawn. That card let Revit's refusal out to
//   abort the caller's transaction and left a dimension holding fewer than
//   two references in it; here both are this card's rules on the datum route
//   too. A named axis is refused for datums: their direction is worked out
//   from them.
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

// VERSION 4. Grids and levels, referenced as the whole element. Asked of the
// view once, per class, and only when a datum was handed in: a view-scoped
// collector is the only honest answer to "will Revit let me dimension to this
// here".
var grids = new List<Grid>();
var levels = new List<Level>();
var notVisibleInView = new List<ElementId>();
HashSet<ElementId> visibleDatumIds = null;

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

    // VERSION 4 (DIMENSION_GRIDS_AND_LEVELS merged in). A grid or a level is
    // tested FIRST, before any location: it is referenced as itself, and a
    // datum the view is not showing is set aside now - Revit throws when asked
    // to dimension to it, and that throw takes the whole string down with the
    // datums that were fine.
    var datumGrid = element as Grid;
    var datumLevel = element as Level;
    if (datumGrid != null || datumLevel != null)
    {
        if (visibleDatumIds == null)
        {
            visibleDatumIds = new HashSet<ElementId>();
            foreach (var shown in new FilteredElementCollector(doc, view.Id).OfClass(typeof(Grid)))
                visibleDatumIds.Add(shown.Id);
            foreach (var shown in new FilteredElementCollector(doc, view.Id).OfClass(typeof(Level)))
                visibleDatumIds.Add(shown.Id);
        }
        if (!visibleDatumIds.Contains(element.Id)) { notVisibleInView.Add(element.Id); continue; }
        if (datumGrid != null) grids.Add(datumGrid); else levels.Add(datumLevel);
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

// The datum sentence, said whatever else happens (version 4).
string datumSentence = notVisibleInView.Count == 0 ? "" :
    notVisibleInView.Count + " grid(s) or level(s) are not shown in this view and were left out - "
    + "Revit throws when asked to dimension to something the view is not showing, and that "
    + "throw would have lost the whole string";

// ONE STRING, ONE KIND - runs, family instances, grids, levels. Version 3
// refused runs with instances; version 4 refuses ANY two kinds together,
// whole, naming each - the safer of the two cards' rules (the owner: "in
// every conflict keep the safer rule"). DIMENSION_GRIDS_AND_LEVELS
// dimensioned the grids and set a wall or a duct handed in with them aside;
// here that set is refused, because a string drawn through part of what was
// handed in reads as the whole job done.
var kindsHanded = new List<string>();
if (runElements.Count > 0) kindsHanded.Add(runElements.Count + " run(s)");
if (instances.Count > 0) kindsHanded.Add(instances.Count + " family instance(s)");
if (grids.Count > 0) kindsHanded.Add(grids.Count + " grid(s)");
if (levels.Count > 0) kindsHanded.Add(levels.Count + " level(s)");
bool mixed = kindsHanded.Count > 1;
bool datums = grids.Count > 0 || levels.Count > 0;

if (problem.Length > 0)
{
    // An axis that is not one: nothing was looked at, nothing is drawn.
}
else if (mixed)
{
    // ONE STRING, ONE KIND (versions 3 and 4). Refused whole, nothing drawn.
    foreach (var run in runElements) refused.Add(run.Id);
    foreach (var instance in instances) refused.Add(instance.Id);
    foreach (var grid in grids) refused.Add(grid.Id);
    foreach (var level in levels) refused.Add(level.Id);
    problem = string.Join(", ", kindsHanded) + " were handed in together. One dimension "
            + "string is drawn through one kind - run centrelines, the centre references of "
            + "family instances, grids, or levels - and a string mixing them has not been "
            + "measured"
            + (grids.Count > 0 && levels.Count > 0
                ? " (grids and levels are measured along axes at right angles to each other, so "
                  + "one string across both measures the wrong thing for whichever kind loses)"
                : "")
            + ", so NOTHING WAS DRAWN. Dimension each kind in its own call";
}
else if (datums)
{
    // ---- THE DATUM ROUTE (version 4, from DIMENSION_GRIDS_AND_LEVELS) ------
    //
    // A GRID AND A LEVEL ARE REFERENCED AS THE WHOLE ELEMENT - new
    // Reference(grid) - with no geometry read at all; the geometry route
    // returns nothing for a grid. Reference planes are NOT taken: they are not
    // what a setting-out drawing dimensions to, they are usually invisible on
    // the sheet, and one swept in would put a witness line to nothing anybody
    // can see. They go to notLinear with everything else this cannot use.
    //
    // THE LINE IS WHERE THE STRING SITS, NOT WHAT IT MEASURES. Revit works the
    // measurement out from the references; the line decides where the text
    // and witness lines land, so it is built ACROSS the datums in the view
    // plane, with offsetMm sliding it clear along the other axis.
    var datumIds = new List<ElementId>();
    foreach (var grid in grids) datumIds.Add(grid.Id);
    foreach (var level in levels) datumIds.Add(level.Id);

    var references = new List<Reference>();
    var positions = new List<double>();
    var accepted = new List<ElementId>();
    XYZ across = null;
    XYZ placementSeed = null;

    if (namedAxis != null)
    {
        // A datum string is measured ACROSS the datums - grids across the
        // plan, levels up the elevation - and that direction is worked out
        // from them. A named x or y could point along the grids or flat
        // across the levels, and would measure nothing; refused rather than
        // guessed which the caller meant.
        refused.AddRange(datumIds);
        problem = "grids and levels are measured across themselves, and the direction is worked out "
                + "from them - leave axis out. NOTHING WAS DRAWN";
    }
    else if (grids.Count > 0)
    {
        XYZ gridDirection = null;
        var lines = new List<Line>();

        foreach (var grid in grids)
        {
            var line = grid.Curve as Line;
            if (line == null)
            {
                // AN ARC GRID IS REFUSED: it has no one direction, so no single
                // axis a string across it measures along. Named, never dropped.
                refused.Add(grid.Id);
                continue;
            }

            var along = line.GetEndPoint(1) - line.GetEndPoint(0);
            if (along.GetLength() < 1e-9) { refused.Add(grid.Id); continue; }
            along = along.Normalize();

            if (gridDirection == null)
            {
                gridDirection = along;
                placementSeed = line.GetEndPoint(0);
            }
            else if (Math.Abs(along.DotProduct(gridDirection)) < 0.999)
            {
                // Out of parallel by more than about two and a half degrees:
                // the gap between them is not one number.
                refused.Add(grid.Id);
                continue;
            }

            references.Add(new Reference(grid));
            lines.Add(line);
            accepted.Add(grid.Id);
        }

        // Across the grids and drawable on this view.
        if (gridDirection != null) across = view.ViewDirection.CrossProduct(gridDirection);
        if (across != null && across.GetLength() >= 1e-9)
        {
            across = across.Normalize();
            foreach (var line in lines) positions.Add(line.GetEndPoint(0).DotProduct(across));
        }
        else across = null;
    }
    else
    {
        // Levels stack vertically, so the measuring axis is up. A PLAN LOOKS
        // STRAIGHT DOWN IT: no level-to-level string can be drawn there, and
        // Revit would take the whole string down. Refused with the reason.
        if (Math.Abs(XYZ.BasisZ.DotProduct(view.ViewDirection)) > 0.001)
        {
            refused.AddRange(datumIds);
            problem = "levels cannot be dimensioned in this view - it looks along the vertical, which "
                    + "is the axis the dimension would have to measure. A section or an elevation is "
                    + "where floor to floor heights go. NOTHING WAS DRAWN";
        }
        else
        {
            across = XYZ.BasisZ;
            placementSeed = view.Origin;
            foreach (var level in levels)
            {
                references.Add(new Reference(level));

                // ProjectElevation, NOT Elevation (FRAGMENT-ISSUES 5b-179).
                // These positions build the dimension LINE, in model
                // coordinates. Elevation is measured from the level type's
                // Elevation Base - a survey point, a moved base point - and is
                // off by that offset in model space; two level types on
                // different bases can even put two heights at one number,
                // which the zero check below would then refuse.
                positions.Add(level.ProjectElevation);
                accepted.Add(level.Id);
            }
        }
    }

    if (problem.Length > 0)
    {
        // Refused above; nothing drawn.
    }
    else if (references.Count >= 2 && across != null && placementSeed != null)
    {
        var offsetDirection = across.CrossProduct(view.ViewDirection);
        if (offsetDirection.GetLength() < 1e-9)
        {
            refused.AddRange(accepted);
            problem = "the measuring axis and the view direction are the same line, so the string "
                    + "has nowhere to sit on this view. NOTHING WAS DRAWN";
        }
        else
        {
            offsetDirection = offsetDirection.Normalize();

            double lowest = positions[0], highest = positions[0];
            for (int i = 1; i < positions.Count; i++)
            {
                lowest = Math.Min(lowest, positions[i]);
                highest = Math.Max(highest, positions[i]);
            }

            var basePoint = placementSeed - across * placementSeed.DotProduct(across)
                          + offsetDirection * offset;
            var from = basePoint + across * lowest;
            var to = basePoint + across * highest;

            if ((to - from).GetLength() > 1e-6)
            {
                var array = new ReferenceArray();
                foreach (var reference in references) array.Append(reference);

                // THIS CARD'S SAFER RULES, now on the datum route too, which
                // had none of them: Revit's refusal caught and said rather
                // than let out to abort the caller's transaction; the
                // dimension read back; one holding fewer than two references
                // taken out again.
                Dimension made = null;
                try
                {
                    made = doc.Create.NewDimension(view, Line.CreateBound(from, to), array);
                }
                catch (Exception ex)
                {
                    problem = "Revit refused the dimension: " + ex.Message + ". NOTHING WAS DRAWN";
                }

                if (made != null)
                {
                    created = made.Id;
                    try { dimensionedCount = made.References == null ? 0 : made.References.Size; }
                    catch { dimensionedCount = 0; }
                    if (dimensionedCount < 2)
                    {
                        try { doc.Delete(made.Id); } catch { }
                        created = null;
                        dimensionedCount = 0;
                        problem = "Revit made a dimension holding fewer than two references, which "
                                + "will not survive a commit, so it was taken out again. NOTHING WAS "
                                + "DRAWN";
                    }
                    else if (dimensionedCount < array.Size)
                    {
                        problem = string.Format("the dimension holds {0} of the {1} datums asked "
                                + "for - Revit dropped {2}. Check which it measures before relying "
                                + "on it", dimensionedCount, array.Size, array.Size - dimensionedCount);
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
                // Two grids drawn on top of each other, or two levels at one
                // elevation: the string would measure zero.
                refused.AddRange(accepted);
                problem = "every datum given sits at the same position along the measuring axis, so "
                        + "the string would measure zero. NOTHING WAS DRAWN";
            }
        }
    }
    else
    {
        refused.AddRange(accepted);
        problem = references.Count == 1
            ? "only one grid or level was usable. One reference is not a short dimension, it is not "
              + "a dimension. NOTHING WAS DRAWN"
            : "no grid here could be dimensioned - an arc grid has no single direction, and grids out "
              + "of parallel have no one gap between them. NOTHING WAS DRAWN";
    }
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
    if (runElements.Count == 0 && notLinear.Count == roomsHanded
        && (roomsHanded > 0 || notVisibleInView.Count > 0))
    {
        // Only rooms, or only datums this view is not showing, were handed
        // in: the sentences below are the whole answer.
        problem = "NOTHING WAS DRAWN";
    }
    else if (runElements.Count == 0)
    {
        problem = "nothing handed in is a straight run - a duct, pipe, conduit or cable tray - a "
                + "family instance - an air terminal, fixture or piece of equipment - or a grid or "
                + "a level, so there is nothing here to dimension. NOTHING WAS DRAWN";
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

// A DATUM THE VIEW IS NOT SHOWING IS SAID WHATEVER ELSE HAPPENED (version 4).
if (datumSentence.Length > 0)
    problem = problem.Length == 0 ? datumSentence : problem + ". " + datumSentence;
