// NOT STANDALONE. Assumes `doc` and `elements` are in scope, and leaves
// `identities`, `areas`, `volumes`, `unplaced`, `unenclosed`,
// `volumeNotComputed` and `notSpatial` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// ROOMS AND SPACES ARE ONE JOB, AND THE BASE TYPE IS WHY.
//
// `Room` and `Space` live in two different namespaces and behave as one thing
// here: number, name, level and area are all declared on `SpatialElement`,
// which both derive from. Reading through the base means a mixed set of rooms
// and spaces needs no branch, and it is also what keeps this fragment off the
// category-specific types.
//
// THREE STATES LOOK IDENTICAL IN A SCHEDULE AND ARE NOT THE SAME PROBLEM.
//
//   no location, zero area    NOT PLACED. It exists in the schedule and was
//                             never put in the model. Somebody places it or
//                             deletes it
//   a location, zero area     NOT ENCLOSED. It was placed where the
//                             boundaries do not close. A wall, a room-bounding
//                             setting, or a gap
//   an area, zero volume      volume computation is OFF for the project.
//                             Nothing is wrong with the room at all
//
// A schedule that prints 0 for all three sends somebody hunting for a
// modelling fault in the one case where there is none.
//
// SQUARE FEET AND CUBIC FEET. D-20 converts at the edge. The area comes
// straight off the element and the volume off its parameter, so both are in
// the units Revit stores and neither is rounded here.

var identities = new Dictionary<ElementId, string>();
var areas = new Dictionary<ElementId, double>();
var volumes = new Dictionary<ElementId, double>();
var unplaced = new List<ElementId>();
var unenclosed = new List<ElementId>();
var volumeNotComputed = new List<ElementId>();
var notSpatial = new List<ElementId>();

foreach (var element in elements)
{
    var spatial = element as SpatialElement;
    if (spatial == null) { notSpatial.Add(element == null ? ElementId.InvalidElementId : element.Id); continue; }

    double area = 0;
    try { area = spatial.Area; } catch { }

    // The location is what separates "never placed" from "placed badly", and
    // it is the only way to tell them apart: both report zero area.
    bool placed = false;
    try { placed = spatial.Location != null; } catch { }

    if (area > 0)
        areas[spatial.Id] = area;
    else if (!placed)
        unplaced.Add(spatial.Id);
    else
        unenclosed.Add(spatial.Id);

    // ROOM_VOLUME, not a Volume property. The parameter is present on both
    // categories and every release, and it carries the "not computed" state
    // as an absent value where a property would hand back a plain zero - and
    // a zero here is indistinguishable from a room with no volume, which is
    // not a thing that exists.
    Parameter volumeParameter = null;
    try { volumeParameter = spatial.get_Parameter(BuiltInParameter.ROOM_VOLUME); } catch { }

    double volume = 0;
    if (volumeParameter != null && volumeParameter.HasValue)
    {
        try { volume = volumeParameter.AsDouble(); } catch { }
    }

    if (volume > 0) volumes[spatial.Id] = volume;
    else if (area > 0) volumeNotComputed.Add(spatial.Id);

    string number = "";
    string name = "";
    string level = "";
    try { number = spatial.Number ?? ""; } catch { }
    try { name = spatial.Name ?? ""; } catch { }
    try
    {
        // Not every spatial element resolves a level - an unplaced one often
        // does not - so this is read separately from the rest of the identity
        // and left blank rather than costing the number and the name.
        var host = spatial.Level;
        if (host != null) level = host.Name ?? "";
    }
    catch { }

    identities[spatial.Id] = (number.Length > 0 ? number + " " : "") + name +
                             (level.Length > 0 ? ", " + level : "");
}
