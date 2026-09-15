// NOT STANDALONE. Assumes `doc` and `nameContains` are in scope; leaves
// `elements`, `segmentCount`, `segmentList`, `sizeTable`, `scanned`, `noSizes`
// and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE NAMES COME BACK AS ONE STRING, DELIBERATELY. A list is abbreviated to
// its first three entries by the time it reaches a reader, so five copper
// segments show as three and read as "there are three". One string is not
// abbreviated. `findings` still carries a line per segment for anyone reading
// the record rather than the reply.
//
// THE SIZE RANGE IS THE POINT. Refrigerant line sets are sized by imperial
// OUTSIDE diameter - 6.35, 9.52, 12.7 mm - and water tube by metric DN - 15,
// 22, 28. Both are called copper and the name cannot tell them apart, so the
// OD range is what shows a segment holds the wrong sizes for the job.
//
// A SEGMENT WITH NO SIZES IS A REAL ANSWER. It exists, it appears in a
// dropdown, and nothing can be drawn with it - which looks like a fault
// somewhere else entirely.
//
// SIZES ARE INTERNAL FEET, MULTIPLIED BY 304.8. Plain arithmetic at the edge,
// never a units API - that is the call that breaks at Revit 2021.

var findings = new List<string>();
var noSizes = new List<string>();
// THE SEGMENTS THEMSELVES, SO THE ANSWER CAN BE ACTED ON. A report that
// finds a thing and cannot hand it to the next step is half a tool: a pipe
// segment is a settings object, not geometry, so it can never be clicked in
// a view, and `filter-elements-by-id` refuses a raw number by design. Naming
// it here is the ONLY way anything downstream - set-selection, then
// delete-elements - can reach one.
var elements = new List<Element>();
var segmentCount = 0;
var scanned = 0;
var segmentList = "";
var sizeTable = "";

var needle = (nameContains ?? "").Trim();
var rows = new List<string>();

// AN EXACT NAME BEATS A PREFIX. "TRG_Copper_Refrigerant" is a substring of
// "TRG_Copper_Refrigerant_EN12735", so asking for the first returned both and
// the size table went quiet - it only prints when the needle found ONE. A
// name that matches a segment exactly is not an ambiguous request, so it is
// treated as naming that segment and nothing else.
var exactName = false;

// THE SIZE TABLE IS ONLY USEFUL WHEN ONE SEGMENT IS MEANT. Every size of
// every segment on one line is unreadable, and the range already answers
// "is this the right family of sizes". So the individual sizes are kept per
// segment and only reported when the needle narrowed to exactly one - the
// moment somebody is actually choosing.
var tables = new List<string>();

var segments = new List<PipeSegment>();
try
{
    foreach (var found in new FilteredElementCollector(doc).OfClass(typeof(PipeSegment)).ToElements())
    {
        var segment = found as PipeSegment;
        if (segment != null) segments.Add(segment);
    }
}
catch (Exception) { }

scanned = segments.Count;

// See the note beside `exactName`.
if (needle.Length > 0)
{
    foreach (var segment in segments)
    {
        if (segment.Name != null
            && string.Equals(segment.Name, needle, StringComparison.OrdinalIgnoreCase))
        {
            exactName = true;
            break;
        }
    }
}

foreach (var segment in segments)
{
    var name = segment.Name ?? "<unnamed>";

    // The material is half the identity - two segments called copper may be
    // pointed at different materials, and the takeoff follows the material.
    var materialName = "<no material>";
    try
    {
        var material = doc.GetElement(segment.MaterialId);
        if (material != null) materialName = material.Name;
    }
    catch (Exception) { }

    // NARROWING LOOKS AT BOTH NAME AND MATERIAL, because "copper" is as likely
    // to be the material as the name, and a needle that matched only one of
    // them would miss segments that are plainly what was asked for.
    if (exactName)
    {
        // Something is called exactly this, so only that one was meant.
        if (!string.Equals(name, needle, StringComparison.OrdinalIgnoreCase)) continue;
    }
    else if (needle.Length > 0
        && name.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0
        && materialName.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
        continue;

    segmentCount++;
    elements.Add(segment);

    var sizeCount = 0;
    var smallestOdMm = double.MaxValue;
    var largestOdMm = double.MinValue;
    var inSizeLists = 0;
    var eachSize = new List<string>();

    try
    {
        foreach (var size in segment.GetSizes())
        {
            if (size == null) continue;
            sizeCount++;

            var odMm = size.OuterDiameter * 304.8;
            var idMm = size.InnerDiameter * 304.8;
            if (odMm < smallestOdMm) smallestOdMm = odMm;
            if (odMm > largestOdMm) largestOdMm = odMm;

            // A size not in the size lists never reaches the dropdown, so a
            // segment can be full of sizes and still offer none. It is marked
            // rather than dropped, because a missing row reads as a missing
            // size rather than an unusable one.
            if (size.UsedInSizeLists) inSizeLists++;

            // WALL COMES FROM OD MINUS ID, HALVED. It is the number that says
            // whether a tube is rated for the pressure, and Revit stores it
            // nowhere else.
            //
            // NOMINAL IS REPORTED BESIDE OD BECAUSE THE TWO DISAGREEING IS A
            // REAL DEFECT, not a rounding. Nominal is what a tag, a schedule
            // and the size dropdown show; OD is what gets ordered and brazed.
            // A catalog listing nominal 15 against OD 12.7 tags a half-inch
            // refrigerant line as 15 mm, and nothing in Revit objects.
            var nomMm = size.NominalDiameter * 304.8;
            eachSize.Add(string.Format("nom{0:0.##}/OD{1:0.##}/w{2:0.##}{3}",
                nomMm, odMm, (odMm - idMm) / 2.0, size.UsedInSizeLists ? "" : "(hidden)"));
        }
    }
    catch (Exception) { }

    tables.Add(string.Format("{0}: {1}", name, string.Join(", ", eachSize.ToArray())));

    if (sizeCount == 0)
    {
        noSizes.Add(name);
        rows.Add(string.Format("{0} [{1}] NO SIZES", name, materialName));
        findings.Add(string.Format("'{0}' ({1}) carries NO size at all - it cannot be drawn with",
            name, materialName));
        continue;
    }

    rows.Add(string.Format("{0} [{1}] {2} size(s) OD {3:0.##}-{4:0.##}mm",
        name, materialName, sizeCount, smallestOdMm, largestOdMm));

    findings.Add(string.Format("'{0}' ({1}): {2} size(s), OD {3:0.##} to {4:0.##} mm, {5} in the "
        + "size list", name, materialName, sizeCount, smallestOdMm, largestOdMm, inSizeLists));

    if (inSizeLists == 0)
    {
        findings.Add(string.Format("    '{0}' has sizes but NONE is in the size list, so nothing "
            + "of it appears in the dropdown", name));
    }
}

segmentList = string.Join("  ||  ", rows.ToArray());

// See the note beside `tables` - the size table is reported only when the
// needle narrowed to exactly one segment.
if (segmentCount == 1 && tables.Count == 1) sizeTable = tables[0];

findings.Insert(0, string.Format("{0} pipe segment(s) reported of {1} scanned; {2} carry no sizes",
    segmentCount, scanned, noSizes.Count));

if (segmentCount == 0 && scanned > 0)
{
    findings.Add(string.Format("Nothing matched '{0}'. The {1} segment(s) in this project are "
        + "named something else - run it again with an empty needle to see them all",
        needle, scanned));
}
