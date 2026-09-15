// NOT STANDALONE. Assumes `doc`, `segmentName`, `materialName`, `copyFrom`,
// `nominalEqualsOd`, `replaceSizes`, `scheduleName`, `roughness` and `addSizes` are
// in scope; leaves
// `createdId`, `sizesCopied`, `sizesAdded`, `relabelled`, `alreadyThere`,
// `nearMisses`, `rejected`, `problem`, `scheduleCreated` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// IT COPIES RATHER THAN INVENTS. A wall thickness on a refrigerant line is a
// pressure rating, and typing one from memory is how a 40-bar line gets
// modelled thinner than it is. Copied sizes are read in internal feet and
// handed straight back in internal feet - never converted, never rounded.
//
// ONLY addSizes IS CONVERTED, because only it comes from a person, in
// millimetres. Divided by 304.8 as plain arithmetic - no units API, which is
// the call that breaks at 2021.
//
// NOMINAL = OD IS A DECISION. Water tube is called by DN; refrigerant tube by
// its outside diameter. The flag decides, and the count of what was actually
// relabelled is reported - a catalog already labelled by OD relabels nothing,
// which is a different fact from copying nothing.
//
// MATERIAL AND SCHEDULE ARE READ-ONLY ONCE CREATED. Create is the only place
// either can be set, so both are resolved BEFORE anything is made and a name
// matching nothing stops the whole thing rather than leaving a segment nobody
// wanted.
//
// EVERY SIZE IS MADE usedInSizeLists AND usedInSizing. A size that is neither
// sits in the table and never reaches the pipe tool, which looks like the
// segment failed to take.

var findings = new List<string>();
var alreadyThere = new List<string>();
var nearMisses = new List<string>();
var rejected = new List<string>();
// WHY IT REFUSED, AS A STRING. Revit's own words matter here - a segment is
// identified by its material AND its schedule, and only the message says
// which of the two was the clash. A list entry is abbreviated before it
// reaches a reader; a string is not.
var problem = "";
ElementId createdId = null;
var sizesCopied = 0;
var sizesAdded = 0;
var relabelled = 0;

var wantedName = (segmentName ?? "").Trim();
var wantedMaterial = (materialName ?? "").Trim();
var wantedSource = (copyFrom ?? "").Trim();
var proceed = true;

if (wantedName.Length == 0)
{
    findings.Add("No name was given for the new segment.");
    proceed = false;
}

// EVERY PIPE SEGMENT, ONCE. Used for the duplicate check and the source
// lookup, so a project is not walked twice.
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

// ALREADY THERE IS NOT A FAILURE, and it is not a licence to make a second
// one either - two segments of one name is a dropdown nobody can use (D-52).
if (proceed)
{
    foreach (var segment in segments)
    {
        if (segment.Name != null
            && string.Equals(segment.Name, wantedName, StringComparison.OrdinalIgnoreCase))
        {
            alreadyThere.Add(segment.Name);
            findings.Add(string.Format("A pipe segment called '{0}' already exists, so nothing "
                + "was created. Delete it first, or choose another name.", segment.Name));
            proceed = false;
            break;
        }
    }
}

// THE SOURCE. Its sizes AND its schedule type are both taken from here - a
// segment must belong to a schedule, and copying the source's is the only
// choice that cannot be wrong for a copy of it.
PipeSegment source = null;
if (proceed)
{
    foreach (var segment in segments)
    {
        if (segment.Name != null
            && string.Equals(segment.Name, wantedSource, StringComparison.OrdinalIgnoreCase))
        {
            source = segment;
            break;
        }
    }

    if (source == null)
    {
        foreach (var segment in segments)
        {
            if (segment.Name == null || wantedSource.Length == 0) continue;
            if (segment.Name.IndexOf(wantedSource, StringComparison.OrdinalIgnoreCase) >= 0)
                nearMisses.Add(segment.Name);
        }
        findings.Add(string.Format("No pipe segment is called '{0}' to copy sizes from. {1}",
            wantedSource,
            nearMisses.Count == 0
                ? "Nothing came close - REPORT_PIPE_SEGMENTS lists what this project has."
                : string.Format("{0} name(s) came close and are listed.", nearMisses.Count)));
        proceed = false;
    }
}

// THE MATERIAL, BY NAME, RESOLVED BEFORE ANYTHING IS CREATED. See the header.
ElementId materialId = null;
if (proceed)
{
    var materials = new List<Element>();
    try
    {
        foreach (var found in new FilteredElementCollector(doc).OfClass(typeof(Material)).ToElements())
            materials.Add(found);
    }
    catch (Exception) { }

    foreach (var material in materials)
    {
        if (material.Name != null
            && string.Equals(material.Name, wantedMaterial, StringComparison.OrdinalIgnoreCase))
        {
            materialId = material.Id;
            break;
        }
    }

    if (materialId == null)
    {
        foreach (var material in materials)
        {
            if (material.Name == null || wantedMaterial.Length == 0) continue;
            if (material.Name.IndexOf(wantedMaterial, StringComparison.OrdinalIgnoreCase) >= 0)
                nearMisses.Add(material.Name);
        }
        findings.Add(string.Format("No material is called '{0}'. It cannot be set after the "
            + "segment is made, so nothing was created.", wantedMaterial));
        proceed = false;
    }
}

// THE SCHEDULE, AND WHY THIS IS NOT OPTIONAL DETAIL. Revit keys a segment by
// its material AND its schedule TOGETHER, and refuses a pair already in use:
// "The MaterialId and ScheduleId was already used by another pipe segment."
// So a SECOND copper segment cannot reuse the first one's schedule, however
// sensible that would be. Naming a schedule that does not exist CREATES it,
// because the alternative is refusing a request that is entirely reasonable
// and telling nobody why.
ElementId scheduleId = null;
var scheduleCreated = "";
var roughnessSet = "";

if (proceed)
{
    var wantedSchedule = (scheduleName ?? "").Trim();

    if (wantedSchedule.Length == 0)
    {
        // Reusing the source's schedule only works when the material differs.
        scheduleId = source.ScheduleTypeId;
    }
    else
    {
        try { scheduleId = PipeScheduleType.GetPipeScheduleId(doc, wantedSchedule); }
        catch (Exception) { scheduleId = null; }

        if (scheduleId == null || scheduleId.Equals(ElementId.InvalidElementId))
        {
            try
            {
                var madeSchedule = PipeScheduleType.Create(doc, wantedSchedule);
                if (madeSchedule != null)
                {
                    scheduleId = madeSchedule.Id;
                    scheduleCreated = wantedSchedule;
                }
            }
            catch (Exception error)
            {
                problem = error.Message;
                findings.Add(string.Format("The schedule '{0}' could not be created: {1}",
                    wantedSchedule, error.Message));
                proceed = false;
            }
        }
    }
}

var sizes = new List<MEPSize>();

// REPLACE EXISTS BECAUSE A METRIC CATALOG SHARES NO SIZE WITH AN IMPERIAL
// ONE. Adding EN 12735's 12 mm to a table of imperial ODs makes a segment
// that offers both and is honestly neither, which is worse than either. The
// source is still needed with replaceSizes on - a segment must belong to a
// pipe SCHEDULE, and the source's is the only one that cannot be wrong for a
// project that already uses it.
if (proceed && !replaceSizes)
{
    // COPIED SIZES STAY IN FEET. See the header.
    try
    {
        foreach (var size in source.GetSizes())
        {
            if (size == null) continue;

            var nominal = size.NominalDiameter;
            if (nominalEqualsOd && nominal != size.OuterDiameter)
            {
                nominal = size.OuterDiameter;
                relabelled++;
            }

            sizes.Add(new MEPSize(nominal, size.InnerDiameter, size.OuterDiameter, true, true));
            sizesCopied++;
        }
    }
    catch (Exception error)
    {
        findings.Add(string.Format("The sizes of '{0}' could not be read: {1}",
            source.Name, error.Message));
        proceed = false;
    }
}
else if (proceed)
{
    findings.Add(string.Format("Sizes of '{0}' were NOT copied - the table is built from "
        + "addSizes alone. Its pipe schedule is still taken from it.", source.Name));
}

// EXTRA SIZES, IN MILLIMETRES, AS "OD x WALL". The only numbers a person
// types here, and the only ones converted.
if (proceed && (addSizes ?? "").Trim().Length > 0)
{
    foreach (var piece in addSizes.Split(','))
    {
        var entry = piece.Trim();
        if (entry.Length == 0) continue;

        var parts = entry.Split('x', 'X');
        if (parts.Length != 2)
        {
            rejected.Add(string.Format("'{0}' is not 'OD x wall', e.g. '22.22x1.14'", entry));
            continue;
        }

        double odMm, wallMm;
        if (!double.TryParse(parts[0].Trim(), out odMm)
            || !double.TryParse(parts[1].Trim(), out wallMm))
        {
            rejected.Add(string.Format("'{0}' has a number that could not be read", entry));
            continue;
        }

        // A WALL THAT EATS THE PIPE IS NOT A PIPE. Caught here rather than
        // stored, because Revit will accept a negative bore without a word.
        if (odMm <= 0 || wallMm <= 0 || (wallMm * 2.0) >= odMm)
        {
            rejected.Add(string.Format("'{0}' gives a bore of zero or less - check OD and wall",
                entry));
            continue;
        }

        var outer = odMm / 304.8;
        var inner = (odMm - (wallMm * 2.0)) / 304.8;

        // The nominal for an added size follows the same rule as a copied
        // one; without the flag there is nothing else it could sensibly be.
        sizes.Add(new MEPSize(outer, inner, outer, true, true));
        sizesAdded++;
    }
}

if (proceed && sizes.Count == 0)
{
    findings.Add("No size survived, so nothing was created - a segment with no size cannot be "
        + "drawn with and would only look like one that works.");
    proceed = false;
}

if (proceed)
{
    try
    {
        var made = PipeSegment.Create(doc, materialId, scheduleId, sizes);
        if (made != null)
        {
            made.Name = wantedName;
            createdId = made.Id;

            // ROUGHNESS DEFAULTS TO ZERO, AND ZERO IS FRICTIONLESS PIPE.
            // Nothing in Revit warns, every pressure-drop and flow figure on
            // the system comes out optimistic, and the segment looks complete
            // in the dialog. Found by a modeller reading the dialog, not by
            // any check - a new segment sat at 0.00000 mm beside a stock one
            // at 0.00254. Copying the source's is the same argument as
            // copying its sizes: a value somebody already trusts beats a
            // default nobody chose.
            var wantedRoughness = (roughness ?? "").Trim();
            try
            {
                if (wantedRoughness.Length == 0)
                {
                    made.Roughness = source.Roughness;
                    roughnessSet = string.Format("{0:0.#####} (copied from '{1}')",
                        source.Roughness * 304.8, source.Name);
                }
                else
                {
                    double mm;
                    if (double.TryParse(wantedRoughness, out mm) && mm >= 0)
                    {
                        made.Roughness = mm / 304.8;
                        roughnessSet = string.Format("{0:0.#####} (as given)", mm);
                    }
                    else
                    {
                        findings.Add(string.Format("'{0}' could not be read as a roughness in "
                            + "millimetres, so the source's was used instead.", wantedRoughness));
                        made.Roughness = source.Roughness;
                        roughnessSet = string.Format("{0:0.#####} (copied - the value given "
                            + "could not be read)", source.Roughness * 304.8);
                    }
                }
            }
            catch (Exception error)
            {
                findings.Add(string.Format("The roughness could not be set: {0}", error.Message));
            }
        }
    }
    catch (Exception error)
    {
        problem = error.Message;
        findings.Add(string.Format("The segment could not be created: {0}", error.Message));
    }
}

if (createdId != null)
{
    findings.Add(string.Format("'{0}' created from '{1}': {2} size(s) copied, {3} added, {4} "
        + "nominal(s) relabelled to the OD. Nothing uses it yet - pointing a pipe type at it "
        + "is SET_ROUTING_PREFERENCE.",
        wantedName, source.Name, sizesCopied, sizesAdded, relabelled));
    if (scheduleCreated.Length > 0)
    {
        findings.Add(string.Format("A new pipe schedule '{0}' was created for it. Revit keys a "
            + "segment by material AND schedule, so a second copper segment needs its own.",
            scheduleCreated));
    }
}

if (rejected.Count > 0)
{
    findings.Add(string.Format("{0} added size(s) were REJECTED and are not in the segment - "
        + "a dropped size is a size nobody can draw", rejected.Count));
}

findings.Insert(0, string.Format("{0}; {1} size(s) in the new segment, all of them in the size "
    + "list and usable for sizing",
    createdId == null ? "NOTHING was created" : "segment created", sizes.Count));
