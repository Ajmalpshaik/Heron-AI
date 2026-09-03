// NOT STANDALONE. Assumes `doc`, `level`, `floorType` and `boundary` are in
// scope; leaves `created` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). Lengths are internal FEET.
//
// ===========================================================================
// THE CREATION CALL IS DIFFERENT AT EACH END OF THE RANGE.
// ===========================================================================
//
//   up to 2021   doc.Create.NewFloor(CurveArray, FloorType, Level, bool)
//   from 2022    Floor.Create(Document, IList<CurveLoop>, ElementId, ElementId)
//
// Checked against the reference assemblies: Floor.Create is absent on 2020 and
// NewFloor is gone by 2024. Naming either in source breaks the other build, so
// both are looked up at run time and whichever exists is used. The boundary is
// built into both shapes because the two want different ones - that is the only
// real difference between the paths.
//
// THE BASE HEIGHT IS THE LEVEL'S PROJECT ELEVATION, NOT ITS ELEVATION. On a
// model set out to a survey datum those differ by the survey offset, and the
// wrong one puts the slab metres out with no error of any kind.
//
// THE LOOP CLOSES ITSELF and must not cross itself.
//
// IT MAKES ONE FLOOR, NOT A STOREY. Nothing here cuts an opening in anything.

var created = ElementId.InvalidElementId;
var findings = new List<string>();

var newStyle = typeof(Floor).GetMethod("Create",
    new[] { typeof(Document), typeof(IList<CurveLoop>), typeof(ElementId), typeof(ElementId) });

var creation = doc.Create;
System.Reflection.MethodInfo oldStyle = null;
foreach (var candidate in creation.GetType().GetMethods())
{
    if (candidate.Name != "NewFloor") continue;
    var parameters = candidate.GetParameters();
    if (parameters.Length == 4 && parameters[0].ParameterType == typeof(CurveArray))
    {
        oldStyle = candidate;
        break;
    }
}

if (newStyle == null && oldStyle == null)
{
    findings.Add("This Revit has neither Floor.Create nor Document.Create.NewFloor - nothing here can "
        + "make a floor on it");
}
else if (boundary == null || boundary.Count < 3)
{
    findings.Add(string.Format("A floor needs at least three boundary points and {0} were given",
        boundary == null ? 0 : boundary.Count));
}
else
{
    // NOT level.Elevation - see the header.
    var z = level.ProjectElevation;

    var loopCurves = new List<Curve>();
    var arrayCurves = new CurveArray();
    var badPoint = false;

    for (var i = 0; i < boundary.Count; i++)
    {
        var a = boundary[i];
        var b = boundary[(i + 1) % boundary.Count];
        if (a == null || b == null) { badPoint = true; break; }
        try
        {
            var edge = Line.CreateBound(new XYZ(a.X, a.Y, z), new XYZ(b.X, b.Y, z));
            loopCurves.Add(edge);
            arrayCurves.Append(edge);
        }
        catch
        {
            badPoint = true;
            break;
        }
    }

    if (badPoint)
    {
        findings.Add("Two boundary points are in the same place, so one edge has no length - nothing "
            + "was created");
    }
    else
    {
        try
        {
            Element made = null;

            if (newStyle != null)
            {
                var loops = new List<CurveLoop> { CurveLoop.Create(loopCurves) };
                made = (Element)newStyle.Invoke(null,
                    new object[] { doc, (IList<CurveLoop>)loops, floorType.Id, level.Id });
            }
            else
            {
                // The old one wants the TYPE object, not its id, and a bool for
                // whether the floor is structural.
                made = (Element)oldStyle.Invoke(creation,
                    new object[] { arrayCurves, floorType, level, false });
            }

            if (made == null)
            {
                findings.Add("Revit returned no floor - the usual cause is a boundary that crosses "
                    + "itself");
            }
            else
            {
                created = made.Id;
                findings.Add(string.Format("Floor created on '{0}' using the {1} call. It is a slab and "
                    + "nothing has been cut through it", level.Name,
                    newStyle != null ? "Revit 2022 and later" : "pre-2022"));
            }
        }
        catch (Exception ex)
        {
            var why = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
            findings.Add(string.Format("Floor creation failed: {0}", why));
        }
    }
}
