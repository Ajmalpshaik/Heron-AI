// NOT STANDALONE. Assumes `doc` is in scope; leaves `volumeComputation`,
// `roomAreaComputation`, `areaSchemes`, `placedRooms`, `roomsWithVolume`,
// `placedSpaces`, `spacesWithVolume`, `notAProject` and `findings` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE WHOLE AREA AND VOLUME COMPUTATIONS DIALOG, BOTH TABS, IN ITS OWN WORDS
// (Architecture > Room & Area > Area and Volume Computations):
//
//   Computations   Volume Computations     Areas only / Areas and Volumes
//                  Room Area Computation   At wall finish / At wall center /
//                                          At wall core layer / At wall core center
//   Area Schemes   every scheme - its name, its description, whether it is the
//                  Gross Building one, and how many areas and area plans it holds
//
// A QUESTION ABOUT A SETTING IS ANSWERED HERE, NEVER BY THE WRITE (D-86).
// "Is volume computation on?" needs nothing changed to answer, so the answer
// comes from reading the setting - SET_AREA_VOLUME_COMPUTATIONS is the change.
//
// A VOLUME OF ZERO IS USUALLY THIS SETTING, NOT THE ROOM. With Volume
// Computations at Areas only, Revit computes no volume, and a schedule reads
// as a building with none. So beside the setting the answer says how many
// placed rooms and spaces actually HAVE a volume right now: the setting and
// what it does to the model, read together, so nobody hunts for a modelling
// fault that is a switch. The counts are context, never a change it made.
//
// THE BOUNDARY LOCATION IS ALSO ASKED FOR SPACES. The dialog shows one Room
// Area Computation; Revit's API takes the kind of spatial element as an
// argument. What Revit answers for Spaces is reported as Revit's answer - not
// as a second setting the dialog shows.
//
// AN AREA SCHEME WITH NO AREAS IS STILL A SCHEME. REPORT_AREAS reaches schemes
// through the Areas placed in them, so a scheme nobody has used yet is
// invisible there. Here every scheme is collected for itself, which is the list
// the Area Schemes tab shows. The count of area plans and areas beside each is
// what deleting that scheme would take with it - which is why no delete is
// offered anywhere in Heron (Constitution Article 7).
//
// A FAMILY HAS NO SUCH DIALOG. In the Family Editor the settings belong to the
// project the family is loaded into, so a family document is answered with
// that sentence and nothing else.

var findings = new List<string>();
var volumeComputation = "";
var roomAreaComputation = "";
var areaSchemes = new List<string>();
var placedRooms = 0;
var roomsWithVolume = 0;
var placedSpaces = 0;
var spacesWithVolume = 0;
var notAProject = false;

// The dialog's words for each boundary location, in the order it lists them,
// and what each means for a room's area.
var boundaryWords = new Dictionary<SpatialElementBoundaryLocation, string>
{
    { SpatialElementBoundaryLocation.Finish, "At wall finish" },
    { SpatialElementBoundaryLocation.Center, "At wall center" },
    { SpatialElementBoundaryLocation.CoreBoundary, "At wall core layer" },
    { SpatialElementBoundaryLocation.CoreCenter, "At wall core center" },
};
var boundaryMeaning = new Dictionary<SpatialElementBoundaryLocation, string>
{
    { SpatialElementBoundaryLocation.Finish, "to the finish face of each bounding wall" },
    { SpatialElementBoundaryLocation.Center, "to the centre line of each bounding wall" },
    { SpatialElementBoundaryLocation.CoreBoundary, "to the face of each bounding wall's core layers" },
    { SpatialElementBoundaryLocation.CoreCenter, "to the centre of each bounding wall's core" },
};
Func<SpatialElementBoundaryLocation, string> wordsFor = location =>
    boundaryWords.ContainsKey(location) ? boundaryWords[location] : location.ToString();

// A placed, bounded room or space, and whether Revit has a volume for it.
// ROOM_VOLUME rather than a property: the parameter is the same on both kinds
// and carries the not-computed state that a property would hand back as 0.
Func<SpatialElement, bool> isPlaced = spatial =>
{
    try { return spatial.Location != null && spatial.Area > 0; }
    catch (Exception) { return false; }
};
Func<SpatialElement, bool> hasVolume = spatial =>
{
    try
    {
        var volume = spatial.get_Parameter(BuiltInParameter.ROOM_VOLUME);
        return volume != null && volume.HasValue && volume.AsDouble() > 0;
    }
    catch (Exception) { return false; }
};

// THE DESCRIPTION is the scheme's own text parameter that Revit shows beside
// its name in the Area Schemes tab. Unread is said as unread, never as blank.
Func<AreaScheme, string> descriptionOf = scheme =>
{
    try
    {
        var text = scheme.get_Parameter(BuiltInParameter.ALL_MODEL_DESCRIPTION);
        if (text != null && text.StorageType == StorageType.String) return text.AsString() ?? "";
    }
    catch (Exception) { }
    return null;
};

if (doc.IsFamilyDocument)
{
    notAProject = true;
    findings.Add("The document in front, \"" + doc.Title + "\", is a family. Area and Volume Computations "
        + "belong to a project - ask with the project in front.");
}
else
{
    // -----------------------------------------------------------------------
    // THE COMPUTATIONS TAB
    // -----------------------------------------------------------------------
    AreaVolumeSettings settings = null;
    try { settings = AreaVolumeSettings.GetAreaVolumeSettings(doc); }
    catch (Exception ex)
    {
        findings.Add("Revit would not hand over this project's Area and Volume Computations: " + ex.Message);
    }

    var computesVolumes = false;
    if (settings != null)
    {
        computesVolumes = settings.ComputeVolumes;
        volumeComputation = computesVolumes ? "Areas and Volumes" : "Areas only";

        SpatialElementBoundaryLocation roomBoundary = SpatialElementBoundaryLocation.Finish;
        try
        {
            roomBoundary = settings.GetSpatialElementBoundaryLocation(SpatialElementType.Room);
            roomAreaComputation = wordsFor(roomBoundary);
        }
        catch (Exception ex)
        {
            findings.Add("Room Area Computation could not be read: " + ex.Message);
        }

        if (roomAreaComputation.Length > 0)
        {
            var meaning = boundaryMeaning.ContainsKey(roomBoundary) ? " - every room's area is measured "
                + boundaryMeaning[roomBoundary] : "";
            findings.Add("Room Area Computation: " + roomAreaComputation + meaning + ".");

            // Revit's answer for Spaces, said as Revit's answer.
            try
            {
                var spaceBoundary = settings.GetSpatialElementBoundaryLocation(SpatialElementType.Space);
                findings.Add(spaceBoundary == roomBoundary
                    ? "Revit reports Spaces measured the same way, " + wordsFor(spaceBoundary) + "."
                    : "Revit reports Spaces measured " + wordsFor(spaceBoundary) + " - NOT the same as rooms. The "
                      + "dialog shows only the room setting.");
            }
            catch (Exception) { }
        }
    }

    // What the volume setting means in this model right now.
    foreach (var element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Rooms)
        .WhereElementIsNotElementType())
    {
        var room = element as SpatialElement;
        if (room == null || !isPlaced(room)) continue;
        placedRooms++;
        if (hasVolume(room)) roomsWithVolume++;
    }
    foreach (var element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_MEPSpaces)
        .WhereElementIsNotElementType())
    {
        var space = element as SpatialElement;
        if (space == null || !isPlaced(space)) continue;
        placedSpaces++;
        if (hasVolume(space)) spacesWithVolume++;
    }

    if (volumeComputation.Length > 0)
    {
        var counts = string.Format("{0} of {1} placed room(s) and {2} of {3} placed space(s) have a volume right now",
            roomsWithVolume, placedRooms, spacesWithVolume, placedSpaces);
        findings.Insert(0, computesVolumes
            ? "Volume Computations: Areas and Volumes - Revit computes volumes. " + counts + "."
            : "Volume Computations: Areas only - Revit computes NO volumes, so a room or space volume reads as not "
              + "computed. " + counts + ". Switching it to Areas and Volumes is a project setting "
              + "(SET_AREA_VOLUME_COMPUTATIONS, behind the Admin switch).");
    }

    // -----------------------------------------------------------------------
    // THE AREA SCHEMES TAB
    // -----------------------------------------------------------------------
    var areasIn = new Dictionary<ElementId, int>();
    foreach (var element in new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Areas)
        .WhereElementIsNotElementType())
    {
        var area = element as Area;
        if (area == null) continue;
        AreaScheme owner = null;
        try { owner = area.AreaScheme; } catch (Exception) { }
        if (owner == null) continue;
        areasIn[owner.Id] = areasIn.ContainsKey(owner.Id) ? areasIn[owner.Id] + 1 : 1;
    }

    var plansIn = new Dictionary<ElementId, int>();
    foreach (var plan in new FilteredElementCollector(doc).OfClass(typeof(ViewPlan)).Cast<ViewPlan>())
    {
        if (plan.IsTemplate || plan.ViewType != ViewType.AreaPlan) continue;
        AreaScheme owner = null;
        try { owner = plan.AreaScheme; } catch (Exception) { }
        if (owner == null) continue;
        plansIn[owner.Id] = plansIn.ContainsKey(owner.Id) ? plansIn[owner.Id] + 1 : 1;
    }

    foreach (var scheme in new FilteredElementCollector(doc).OfClass(typeof(AreaScheme)).Cast<AreaScheme>())
    {
        var description = descriptionOf(scheme);
        var gross = false;
        try { gross = scheme.IsGrossBuildingArea; } catch (Exception) { }

        areaSchemes.Add(string.Format("{0}{1} - description {2} - {3} area(s) on {4} area plan(s)",
            scheme.Name,
            gross ? " (the Gross Building scheme)" : "",
            description == null ? "not read" : "\"" + description + "\"",
            areasIn.ContainsKey(scheme.Id) ? areasIn[scheme.Id] : 0,
            plansIn.ContainsKey(scheme.Id) ? plansIn[scheme.Id] : 0));
    }

    findings.Add(areaSchemes.Count == 0
        ? "Area Schemes: none - this project has no area scheme at all."
        : string.Format("Area Schemes: {0} - listed one per line. Deleting a scheme in the dialog also deletes "
            + "its area plans and the areas on them, so the counts beside each are what it would take with it.",
            areaSchemes.Count));
}
