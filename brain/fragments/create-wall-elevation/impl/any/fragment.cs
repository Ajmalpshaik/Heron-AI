// NOT STANDALONE. Assumes `doc`, `planView`, `wall`, `side`, `offsetMm` and
// `scale` are in scope; leaves `created`, `marker`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// TURNED TO THE WALL, AND CHECKED. The marker's first view looks in its
// default direction; the marker is turned about its own point until that view
// looks along the wall's normal, and the view's direction is READ BACK - a
// view that does not face the wall square is reported, not assumed.

View created = null;
Element marker = null;
var refused = new List<string>();
var findings = new List<string>();

var mm = 304.8;
var target = wall as Wall;
var wantedSide = (side ?? "").Trim().ToLowerInvariant();
Line line = null;
if (target != null)
{
    var place = target.Location as LocationCurve;
    if (place != null) line = place.Curve as Line;
}

if (planView == null || !(planView is ViewPlan) || planView.IsTemplate)
    refused.Add("The marker goes in a PLAN view - a floor or ceiling plan, not a template. NOTHING WAS MADE.");
else if (target == null)
    refused.Add("The element given is not a wall. NOTHING WAS MADE.");
else if (line == null)
    refused.Add("Wall " + target.Id + " is not straight, so it has no one face to look at square. NOTHING WAS MADE.");
else if (wantedSide != "exterior" && wantedSide != "interior")
    refused.Add("\"" + side + "\" is not exterior or interior. NOTHING WAS MADE.");
else if (offsetMm <= 0)
    refused.Add("The marker needs to sit in front of the wall - an offset greater than zero. NOTHING WAS MADE.");
else
{
    // The elevation type: "Building" for an outside view, "Interior" for an
    // inside one, when the template names them so; any elevation type else.
    ViewFamilyType elevationType = null;
    ViewFamilyType preferred = null;
    var wantWord = wantedSide == "exterior" ? "building" : "interior";
    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)))
    {
        var candidate = element as ViewFamilyType;
        if (candidate == null || candidate.ViewFamily != ViewFamily.Elevation) continue;
        if (elevationType == null) elevationType = candidate;
        if (preferred == null && candidate.Name.ToLowerInvariant().Contains(wantWord)) preferred = candidate;
    }
    if (preferred != null) elevationType = preferred;

    if (elevationType == null)
    {
        refused.Add("This project has no Elevation view family type - a template setting. NOTHING WAS MADE.");
    }
    else
    {
        var outward = target.Orientation;                       // the exterior side, flips included
        var normal = wantedSide == "exterior" ? outward : outward.Negate();
        var flat = new XYZ(normal.X, normal.Y, 0).Normalize();
        var mid = line.Evaluate(0.5, true);
        var reach = target.Width / 2.0 + offsetMm / mm;
        var height = planView.GenLevel != null ? planView.GenLevel.Elevation : mid.Z;
        var origin = new XYZ(mid.X + flat.X * reach, mid.Y + flat.Y * reach, height);
        var looking = flat.Negate();                            // from the marker toward the wall

        ElevationMarker placed = null;
        try { placed = ElevationMarker.CreateElevationMarker(doc, elevationType.Id, origin, scale); }
        catch (Exception ex) { refused.Add("Revit would not place the marker - " + ex.Message); }

        if (placed != null)
        {
            marker = placed;
            ViewSection view = null;
            try { view = placed.CreateElevation(doc, planView.Id, 0); }
            catch (Exception ex) { refused.Add("The marker was placed and Revit would not make its view - " + ex.Message); }

            if (view != null)
            {
                created = view;
                var now = view.ViewDirection.Negate();
                var nowFlat = new XYZ(now.X, now.Y, 0).Normalize();
                var angle = Math.Atan2(nowFlat.X * looking.Y - nowFlat.Y * looking.X, nowFlat.DotProduct(looking));
                if (Math.Abs(angle) > 1e-6)
                {
                    var axis = Line.CreateBound(origin, origin + XYZ.BasisZ);
                    ElementTransformUtils.RotateElement(doc, placed.Id, axis, angle);
                }
                doc.Regenerate();

                var after = view.ViewDirection.Negate();
                var afterFlat = new XYZ(after.X, after.Y, 0).Normalize();
                var offBy = afterFlat.AngleTo(looking) * 180.0 / Math.PI;

                findings.Add(string.Format("Made '{0}' ({1}) looking at the {2} face of wall {3}, the marker {4:0} mm "
                    + "in front of it, at 1:{5}.", view.Name, elevationType.Name, wantedSide, target.Id, offsetMm, scale));
                if (offBy > 0.5)
                    refused.Add(string.Format("THE VIEW DOES NOT FACE THE WALL SQUARE - it is {0:0.#} degrees off after "
                        + "turning. Turn the marker by hand before using it.", offBy));
                else
                    findings.Add(string.Format("Read back: the view faces the wall square ({0:0.##} degrees off).", offBy));
                findings.Add("The view keeps Revit's default extents - SET_VIEW_CROP crops it around the wall.");
            }
        }
    }
}

foreach (var reason in refused) findings.Add(reason);
