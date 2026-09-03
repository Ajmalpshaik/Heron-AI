// NOT STANDALONE. Assumes `doc`, `level`, `ceilingType`, `boundary` and
// `heightAboveLevel` are in scope; leaves `created` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Lengths are internal FEET.
//
// ===========================================================================
// THE CREATE METHOD DOES NOT EXIST ON EVERY RELEASE, SO IT IS NOT WRITTEN DOWN.
// ===========================================================================
//
// Ceiling.Create arrived in Revit 2022. Naming it in source would fail to
// compile on 2020, so it is looked up by name at run time and its absence is
// REPORTED AS AN ANSWER: that release cannot make a ceiling, and the way round
// it is to draw one by hand once.
//
// That distinction is the whole reason this fragment exists. The earlier
// library recorded the job as impossible, having tested only 2020, and the
// sentence carried no version - so it read as "ceilings cannot be made" and the
// capability went unbuilt while two of three installed Revits could do it.
//
// THE BASE HEIGHT IS THE LEVEL'S PROJECT ELEVATION, NOT ITS ELEVATION. Every
// XYZ the API takes is project-internal. `Level.Elevation` is measured from
// whatever the level type's Elevation Base parameter says - Project or Shared -
// so on a model set out to a survey datum the two differ by the survey offset,
// and the ceiling lands at the wrong height with NO error of any kind.
//
// THE LOOP CLOSES ITSELF. The last point joins back to the first, so the caller
// does not repeat it. A loop that crosses itself is refused by Revit, and that
// refusal is reported rather than swallowed.

var created = ElementId.InvalidElementId;
var findings = new List<string>();

// Looked up by name - see the header. Naming the type would break 2020.
var ceilingClass = typeof(Element).Assembly.GetType(typeof(Document).Namespace + ".Ceiling");
var ceilingCategory = Category.GetCategory(doc, BuiltInCategory.OST_Ceilings);
var createMethod = ceilingClass == null ? null : ceilingClass.GetMethod("Create",
    new[] { typeof(Document), typeof(IList<CurveLoop>), typeof(ElementId), typeof(ElementId) });

if (createMethod == null)
{
    findings.Add("This Revit cannot create a ceiling - the method arrived in Revit 2022 and this "
        + "release is older. Draw ONE by hand (Architecture, then Ceiling) and everything else here "
        + "works on it: the height, the grid and the clearance checks all read a ceiling that exists");
}
else if (boundary == null || boundary.Count < 3)
{
    findings.Add(string.Format("A ceiling needs at least three boundary points and {0} were given",
        boundary == null ? 0 : boundary.Count));
}
else if (ceilingType == null || ceilingType.Category == null
         || ceilingCategory == null || ceilingType.Category.Id != ceilingCategory.Id)
{
    // Checked by CATEGORY rather than by class, so a wall type handed in by
    // mistake is refused here instead of producing something strange. The
    // comparison is ElementId to ElementId - reading either as a number is what
    // broke at 2024 and what disappeared at 2027.
    findings.Add("The type given is not a ceiling type");
}
else
{
    // NOT level.Elevation - see the header. This is the defect that leaves no
    // trace.
    var z = level.ProjectElevation;

    var edges = new List<Curve>();
    var badPoint = false;

    for (var i = 0; i < boundary.Count; i++)
    {
        var a = boundary[i];
        var b = boundary[(i + 1) % boundary.Count];
        if (a == null || b == null) { badPoint = true; break; }
        try
        {
            edges.Add(Line.CreateBound(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z)));
        }
        catch
        {
            // Two points in the same place. Named, because "the boundary has a
            // repeated point" is something the caller can fix and a rolled-back
            // transaction is not.
            badPoint = true;
            break;
        }
    }

    if (badPoint)
    {
        findings.Add("Two boundary points are in the same place, so one edge has no length - "
            + "nothing was created");
    }
    else
    {
        try
        {
            var loops = new List<CurveLoop> { CurveLoop.Create(edges) };
            var made = (Element)createMethod.Invoke(null,
                new object[] { doc, (IList<CurveLoop>)loops, ceilingType.Id, level.Id });

            if (made == null)
            {
                findings.Add("Revit returned no ceiling - the usual cause is a boundary that crosses "
                    + "itself");
            }
            else
            {
                created = made.Id;

                var heightParameter = made.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM);
                var heightSet = false;
                if (heightParameter != null && !heightParameter.IsReadOnly)
                {
                    heightParameter.Set(heightAboveLevel);
                    doc.Regenerate();
                    // READ IT BACK, in millimetres, because that is the number
                    // somebody will check on screen.
                    var got = heightParameter.AsDouble();
                    heightSet = Math.Abs(got - heightAboveLevel) * 304.8 < 1.0;
                    findings.Add(string.Format(
                        "Ceiling created on '{0}' at {1:0} mm above it{2}",
                        level.Name, got * 304.8,
                        heightSet ? "" : string.Format(" - ASKED for {0:0} mm and Revit settled on "
                            + "{1:0} mm", heightAboveLevel * 304.8, got * 304.8)));
                }
                else
                {
                    findings.Add(string.Format("Ceiling created on '{0}', but its height above the "
                        + "level could not be set and is whatever the type gives", level.Name));
                }
            }
        }
        catch (Exception ex)
        {
            // The reflection call wraps whatever Revit threw, and the inner one
            // is the message worth reading.
            var why = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            findings.Add(string.Format("Ceiling creation failed: {0}", why));
        }
    }
}
