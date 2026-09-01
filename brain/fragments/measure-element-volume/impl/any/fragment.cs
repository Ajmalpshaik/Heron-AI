// NOT STANDALONE. Assumes `elements` is in scope; leaves `volumeM3`, `areaM2`,
// `totalVolumeM3`, `totalAreaM2` and `noSolid` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// MEASURED FROM THE SOLIDS, NOT FROM A PARAMETER. Revit publishes a volume
// parameter on some categories and not on others, and where it exists it can
// be excluded by a project setting or left stale. The geometry is what the
// element actually is, and it answers for every category the same way.
//
// UNITS ARE ARITHMETIC (D-20). One foot is 0.3048 m exactly, so a square foot
// is 0.3048 x 0.3048 and a cubic foot is that cubed - exact by definition of
// the international inch, with nothing for 2021's unit rewrite to reach and
// nothing for Autodesk to move underneath it.
//
// GEOMETRY IS ASKED FOR WITHOUT A VIEW, so it is the element's real solid
// rather than what some view happens to show. A view-dependent read would
// return the cut or cropped shape and quietly under-measure.
//
// NESTED GEOMETRY IS WALKED. A family instance carries its solids inside
// GeometryInstances, and reading only the top level finds nothing at all for
// most equipment - which would report a real element as having no solid.
//
// INSULATION IS NOT INCLUDED. It is a separate element wrapped around the duct,
// so this is the bare duct's area. Pricing lagging means measuring the
// insulation elements: a different selection, not a different number.

var volumeM3 = new Dictionary<ElementId, double>();
var areaM2 = new Dictionary<ElementId, double>();
var noSolid = new List<ElementId>();
var totalVolumeM3 = 0.0;
var totalAreaM2 = 0.0;

const double MetresPerFoot = 0.3048;
var cubic = MetresPerFoot * MetresPerFoot * MetresPerFoot;
var square = MetresPerFoot * MetresPerFoot;

var options = new Options();
options.ComputeReferences = false;
options.IncludeNonVisibleObjects = false;

foreach (var element in elements)
{
    if (element == null) continue;
    if (volumeM3.ContainsKey(element.Id) || noSolid.Contains(element.Id)) continue;

    var volume = 0.0;
    var area = 0.0;

    try
    {
        var geometry = element.get_Geometry(options);
        if (geometry != null)
        {
            foreach (var item in geometry)
            {
                var solid = item as Solid;
                if (solid != null && solid.Volume > 0.0)
                {
                    volume += solid.Volume;
                    area += solid.SurfaceArea;
                    continue;
                }

                // A family instance keeps its solids one level down. Reading
                // only the top level finds nothing for most equipment.
                var nested = item as GeometryInstance;
                if (nested == null) continue;

                var inside = nested.GetInstanceGeometry();
                if (inside == null) continue;

                foreach (var part in inside)
                {
                    var innerSolid = part as Solid;
                    if (innerSolid == null || innerSolid.Volume <= 0.0) continue;

                    volume += innerSolid.Volume;
                    area += innerSolid.SurfaceArea;
                }
            }
        }
    }
    catch (Exception)
    {
        noSolid.Add(element.Id);
        continue;
    }

    if (volume <= 0.0)
    {
        // No solid at all - a line, an annotation, a view-only element.
        // Named rather than recorded as zero, which would sit in a takeoff
        // total looking like a measured nothing.
        noSolid.Add(element.Id);
        continue;
    }

    var metres3 = volume * cubic;
    var metres2 = area * square;

    volumeM3[element.Id] = metres3;
    areaM2[element.Id] = metres2;
    totalVolumeM3 += metres3;
    totalAreaM2 += metres2;
}
