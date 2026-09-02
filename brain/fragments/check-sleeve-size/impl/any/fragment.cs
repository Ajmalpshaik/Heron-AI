// NOT STANDALONE. Assumes `doc`, `sleeves`, `annularClearance` and
// `maxOversize` are in scope; leaves `findings`, `undersized`, `orphaned` and
// `unreadable` behind.
//
// READ ONLY. Opens no transaction and needs none. Sizes are internal FEET.
//
// THE SERVICE IS FOUND BY GEOMETRY, NOT BY A PARAMETER. A sleeve family almost
// never records what goes through it - that link is stored nowhere - so the
// pairing is made by asking which service occupies the same space.
//
// A SLEEVE WITH NOTHING THROUGH IT IS A FINDING. It is an orphan left by a run
// that moved, or a run moved and the hole did not. Omitting it makes both
// invisible.
//
// REQUIRED = SERVICE SIZE + 2 x INSULATION + 2 x ANNULAR CLEARANCE. Insulation
// is read from the real insulation element rather than assumed: a 50 mm jacket
// turns a comfortable sleeve into a tight one.
//
// SLEEVE SIZE IS READ BY PARAMETER NAME AND FAMILIES DO NOT AGREE ON NAMES. The
// usual ones are tried and the one that answered is NAMED per row, so a family
// calling it something else reads as unreadable rather than as passing.

var findings = new List<string>();
var undersized = new List<ElementId>();
var orphaned = new List<ElementId>();
var unreadable = 0;

var SIZE_NAMES = new[] { "Diameter", "Sleeve Diameter", "Nominal Diameter", "Opening Diameter",
                         "Width", "Sleeve Width", "Opening Width", "Height", "Sleeve Height" };
var SERVICE_CATEGORIES = new[] { BuiltInCategory.OST_PipeCurves, BuiltInCategory.OST_DuctCurves,
                                 BuiltInCategory.OST_CableTray, BuiltInCategory.OST_Conduit };

Func<Element, string, double> readNamed = (element, name) =>
{
    var parameter = element.LookupParameter(name);
    if (parameter == null || !parameter.HasValue || parameter.StorageType != StorageType.Double) return -1;
    return parameter.AsDouble();
};

// The services, collected once. Doing it per sleeve is the same query run
// hundreds of times.
var services = new List<Element>();
foreach (var category in SERVICE_CATEGORIES)
{
    foreach (var element in new FilteredElementCollector(doc)
        .OfCategory(category).WhereElementIsNotElementType())
    {
        services.Add(element);
    }
}

foreach (var sleeve in sleeves)
{
    if (sleeve == null || !sleeve.IsValidObject) continue;

    var sleeveBox = sleeve.get_BoundingBox(null);
    if (sleeveBox == null)
    {
        unreadable++;
        findings.Add(string.Format("sleeve {0}: no readable geometry - not checked", sleeve.Id));
        continue;
    }

    // Which service passes through. The box test is the whole test here and it
    // is stated rather than dressed up: a sleeve is a short element and a
    // service crossing its box is, in practice, through it. A service running
    // past in the next bay shares no box with a sleeve this small.
    Element through = null;
    foreach (var service in services)
    {
        var serviceBox = service.get_BoundingBox(null);
        if (serviceBox == null) continue;
        if (serviceBox.Max.X < sleeveBox.Min.X || serviceBox.Min.X > sleeveBox.Max.X) continue;
        if (serviceBox.Max.Y < sleeveBox.Min.Y || serviceBox.Min.Y > sleeveBox.Max.Y) continue;
        if (serviceBox.Max.Z < sleeveBox.Min.Z || serviceBox.Min.Z > sleeveBox.Max.Z) continue;
        through = service;
        break;
    }

    if (through == null)
    {
        orphaned.Add(sleeve.Id);
        findings.Add(string.Format("sleeve {0}: NOTHING passes through it. Either an orphan left when a "
            + "run moved, or the run moved and the hole did not", sleeve.Id));
        continue;
    }

    // The sleeve's own size, by whichever name its family used.
    var sleeveSize = -1.0;
    var sizeNameUsed = "";
    foreach (var name in SIZE_NAMES)
    {
        var value = readNamed(sleeve, name);
        if (value > 0) { sleeveSize = value; sizeNameUsed = name; break; }
    }

    if (sleeveSize <= 0)
    {
        unreadable++;
        findings.Add(string.Format("sleeve {0}: no size parameter this recognises. Tried {1}. Add the "
            + "family's own name to the list rather than reading this as a pass",
            sleeve.Id, string.Join(", ", SIZE_NAMES)));
        continue;
    }

    // The service's size, and its real insulation.
    var serviceSize = readNamed(through, "Outside Diameter");
    if (serviceSize <= 0) serviceSize = readNamed(through, "Diameter");
    if (serviceSize <= 0) serviceSize = readNamed(through, "Width");
    if (serviceSize <= 0)
    {
        var serviceBox = through.get_BoundingBox(null);
        if (serviceBox != null)
        {
            var dx = serviceBox.Max.X - serviceBox.Min.X;
            var dy = serviceBox.Max.Y - serviceBox.Min.Y;
            var dz = serviceBox.Max.Z - serviceBox.Min.Z;
            // The two SMALLEST extents are the cross-section; the largest is the
            // run's length and is not a size.
            var sorted = new List<double> { dx, dy, dz };
            sorted.Sort();
            serviceSize = sorted[1];
        }
    }

    var insulation = 0.0;
    try
    {
        var insulationIds = InsulationLiningBase.GetInsulationIds(doc, through.Id);
        foreach (var id in insulationIds)
        {
            var wrap = doc.GetElement(id) as InsulationLiningBase;
            if (wrap != null && wrap.Thickness > insulation) insulation = wrap.Thickness;
        }
    }
    catch { }   // an element that cannot carry a wrap throws, and that IS the filter

    var required = serviceSize + (2 * insulation) + (2 * annularClearance);
    var slack = sleeveSize - required;

    var describe = string.Format("sleeve {0} ({1} = {2:0.#} mm) round {3} {4:0.#} mm{5}: needs {6:0.#} mm",
        sleeve.Id, sizeNameUsed, sleeveSize * 304.8,
        through.Category == null ? "service" : through.Category.Name,
        serviceSize * 304.8,
        insulation > 0 ? string.Format(" + {0:0.#} mm insulation", insulation * 304.8) : " (no insulation)",
        required * 304.8);

    if (slack < 0)
    {
        undersized.Add(sleeve.Id);
        findings.Add(describe + string.Format("  - UNDERSIZED by {0:0.#} mm", -slack * 304.8));
    }
    else if (slack > maxOversize)
    {
        findings.Add(describe + string.Format("  - OVERSIZED by {0:0.#} mm. Costs fire-stopping and "
            + "gets queried on site", slack * 304.8));
    }
    else
    {
        findings.Add(describe + string.Format("  - ok, {0:0.#} mm spare", slack * 304.8));
    }
}

findings.Insert(0, string.Format("{0} sleeve(s) checked: {1} undersized, {2} with nothing through them, "
    + "{3} unreadable", sleeves.Count, undersized.Count, orphaned.Count, unreadable));
