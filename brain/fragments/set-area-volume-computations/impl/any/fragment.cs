// NOT STANDALONE. Assumes `doc` and `settings` are in scope; leaves
// `settingReport`, `changed`, `alreadyAsAsked`, `roomsWithVolume`,
// `spacesWithVolume`, `notAProject`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// THE WHOLE AREA AND VOLUME COMPUTATIONS DIALOG, IN THE WORDS IT SHOWS
// (Architecture > Room & Area > Area and Volume Computations):
//
//   Volume Computations      Areas only / Areas and Volumes
//   Room Area Computation    At wall finish / At wall center /
//                            At wall core layer / At wall core center
//   New Area Scheme          a name, or a name, a colon and a description
//   Rename Area Scheme       the old name, "->", the new name
//   Area Scheme Description  a name, a colon and its new description
//
// Several schemes in one entry are separated with "|". The three separators -
// ":", "->" and "|" - are characters Revit refuses in a name, so a name can
// never be split in the wrong place; a description may hold a colon, and is
// everything after the first one.
//
// ADMIN, AND WHY (D-106). These are how the project is set up. Room Area
// Computation re-measures EVERY room in the project at once, and an area
// scheme is a classification the whole team works inside - the same reason
// CREATE_WORKSET is ADMIN. So it runs only while the owner's Admin switch is
// on, with Changes.
//
// NO DELETE, ON PURPOSE. Deleting an area scheme deletes its area plans and the
// areas on them (Constitution Article 7). It stays in the dialog, where Revit
// itself says what goes with it.
//
// A NEW SCHEME IS A COPY. Revit's API has no call that makes an area scheme
// from nothing, in any release 2020 to 2027 - `AreaScheme` declares only
// IsGrossBuildingArea. What the dialog's New button makes is an ordinary,
// non-Gross Building scheme, and copying an ordinary scheme with
// ElementTransformUtils.CopyElement is the route Revit's API leaves for it. The
// copy is renamed, its description replaced, and checked: it must not be the
// Gross Building scheme, and nothing else - no area plan, no area - may come
// with it. A project with only the Gross Building scheme has nothing ordinary
// to copy, and is refused by name rather than copied into a second Gross
// Building scheme.
//
// EVERYTHING IS CHECKED BEFORE ANYTHING IS WRITTEN. A word the dialog does not
// show, a scheme that is not there, a name Revit would refuse or one already
// taken stops the call with nothing changed. A setting already as asked is
// counted and left alone - never written again.
//
// READ BACK, ALL OR NOTHING. After the last write the model is regenerated and
// every setting and scheme read again. One that does not read as asked fails
// the call, and the host rolls the whole call back. When Volume Computations
// changes, the placed rooms and spaces that have a volume are counted before
// and after, so the answer shows what the switch did to this model.

var findings = new List<string>();
var settingReport = "";
var changed = 0;
var alreadyAsAsked = 0;
var roomsWithVolume = 0;
var spacesWithVolume = 0;
var notAProject = false;
string refused = null;

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

// THE SETTINGS: the key they are named by, and the words of the dialog.
var keys = new List<Tuple<string, string>>
{
    Tuple.Create("volumecomputations", "Volume Computations"),
    Tuple.Create("roomareacomputation", "Room Area Computation"),
    Tuple.Create("newareascheme", "New Area Scheme"),
    Tuple.Create("renameareascheme", "Rename Area Scheme"),
    Tuple.Create("areaschemedescription", "Area Scheme Description"),
};
var keyAliases = new Dictionary<string, string>
{
    { "volumecomputation", "volumecomputations" },
    { "roomareacomputations", "roomareacomputation" },
    { "newareaschemes", "newareascheme" },
    { "renameareaschemes", "renameareascheme" },
    { "areaschemedescriptions", "areaschemedescription" },
};

// The dialog's two volume choices. "(faster)" is part of how it shows Areas
// only, so it is accepted with or without.
var volumeChoices = new Dictionary<string, bool>
{
    { "areasonly", false }, { "areasonlyfaster", false }, { "areasandvolumes", true },
};
Func<bool, string> volumeWords = on => on ? "Areas and Volumes" : "Areas only";

// The dialog's four boundary choices, in its order. "centre" is the same word.
var boundaryChoices = new List<Tuple<SpatialElementBoundaryLocation, string>>
{
    Tuple.Create(SpatialElementBoundaryLocation.Finish, "At wall finish"),
    Tuple.Create(SpatialElementBoundaryLocation.Center, "At wall center"),
    Tuple.Create(SpatialElementBoundaryLocation.CoreBoundary, "At wall core layer"),
    Tuple.Create(SpatialElementBoundaryLocation.CoreCenter, "At wall core center"),
};
Func<SpatialElementBoundaryLocation, string> boundaryWords = location =>
{
    var match = boundaryChoices.FirstOrDefault(c => c.Item1 == location);
    return match == null ? location.ToString() : match.Item2;
};
Func<string, SpatialElementBoundaryLocation?> boundaryFrom = said =>
{
    var heard = squash(said).Replace("centre", "center");
    foreach (var choice in boundaryChoices)
    {
        var shown = squash(choice.Item2);
        if (heard == shown || "at" + heard == shown) return choice.Item1;
    }
    return null;
};

// A placed, bounded room or space, and whether Revit holds a volume for it -
// through ROOM_VOLUME, which carries the not-computed state.
Func<BuiltInCategory, Tuple<int, int>> volumesIn = category =>
{
    int placed = 0, withVolume = 0;
    foreach (var element in new FilteredElementCollector(doc).OfCategory(category).WhereElementIsNotElementType())
    {
        var spatial = element as SpatialElement;
        if (spatial == null) continue;
        try
        {
            if (spatial.Location == null || spatial.Area <= 0) continue;
            placed++;
            var volume = spatial.get_Parameter(BuiltInParameter.ROOM_VOLUME);
            if (volume != null && volume.HasValue && volume.AsDouble() > 0) withVolume++;
        }
        catch (Exception) { }
    }
    return Tuple.Create(placed, withVolume);
};

// THE DESCRIPTION is the scheme's own text parameter that the Area Schemes tab
// shows beside its name.
Func<AreaScheme, Parameter> descriptionOf = scheme =>
{
    try
    {
        var text = scheme.get_Parameter(BuiltInParameter.ALL_MODEL_DESCRIPTION);
        return text != null && text.StorageType == StorageType.String ? text : null;
    }
    catch (Exception) { return null; }
};
Func<AreaScheme, string> descriptionText = scheme =>
{
    var text = descriptionOf(scheme);
    return text == null ? null : (text.AsString() ?? "");
};

var problems = new List<string>();
AreaVolumeSettings computations = null;
var schemes = new List<AreaScheme>();

// What was asked, sorted into the dialog's parts.
bool? volumeAsked = null;
SpatialElementBoundaryLocation? boundaryAsked = null;
var renamesAsked = new List<string>();
var createsAsked = new List<string>();
var descriptionsAsked = new List<string>();

if (doc.IsFamilyDocument)
{
    notAProject = true;
    refused = "The document in front, \"" + doc.Title + "\", is a family. Area and Volume Computations belong to a "
        + "project - ask with the project in front. Nothing was changed.";
}
else
{
    try { computations = AreaVolumeSettings.GetAreaVolumeSettings(doc); }
    catch (Exception ex) { problems.Add("Revit would not hand over this project's Area and Volume Computations: " + ex.Message); }
    if (computations == null && problems.Count == 0)
        problems.Add("Revit handed over no Area and Volume Computations for this project.");
    schemes = new FilteredElementCollector(doc).OfClass(typeof(AreaScheme)).Cast<AreaScheme>().ToList();

    var offered = string.Join(", ", keys.Select(k => k.Item2));
    if (settings == null || settings.Count == 0)
        problems.Add("No settings were given - \"Volume Computations=Areas and Volumes\", semicolons between, from "
            + offered + ".");
    else
        foreach (var pair in settings)
        {
            var key = squash(pair.Key);
            if (keyAliases.ContainsKey(key)) key = keyAliases[key];
            var match = keys.FirstOrDefault(k => k.Item1 == key);
            if (match == null)
            {
                problems.Add("\"" + pair.Key + "\" is not one of the settings this sets - " + offered + ".");
                continue;
            }
            var said = (pair.Value ?? "").Trim();
            if (key == "volumecomputations")
            {
                bool on;
                if (volumeChoices.TryGetValue(squash(said), out on)) volumeAsked = on;
                else problems.Add("\"" + said + "\" is not a Volume Computations choice - the dialog offers Areas only "
                    + "and Areas and Volumes.");
            }
            else if (key == "roomareacomputation")
            {
                boundaryAsked = boundaryFrom(said);
                if (boundaryAsked == null)
                    problems.Add("\"" + said + "\" is not a Room Area Computation choice - the dialog offers "
                        + string.Join(", ", boundaryChoices.Select(c => c.Item2)) + ".");
            }
            else
            {
                var each = said.Split('|').Select(e => e.Trim()).Where(e => e.Length > 0).ToList();
                if (each.Count == 0) problems.Add(match.Item2 + " was named with nothing after it.");
                if (key == "newareascheme") createsAsked.AddRange(each);
                else if (key == "renameareascheme") renamesAsked.AddRange(each);
                else descriptionsAsked.AddRange(each);
            }
        }
}

// ---------------------------------------------------------------------------
// WORK OUT EVERY CHANGE BEFORE MAKING ANY. Names are followed through the
// renames first, then the new schemes, then the descriptions - the order they
// are written in - so a rename can free a name a new scheme takes.
// ---------------------------------------------------------------------------

var names = schemes.ToDictionary(s => s.Id, s => s.Name);       // scheme -> name after this call
var newNames = new List<string>();                                // schemes this call makes
Func<string, List<ElementId>> idsNamed = name =>
{
    var exact = names.Where(n => n.Value == name).Select(n => n.Key).ToList();
    return exact.Count > 0 ? exact
        : names.Where(n => string.Equals(n.Value, name, StringComparison.OrdinalIgnoreCase)).Select(n => n.Key).ToList();
};
Func<string, bool> taken = name =>
    names.Values.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
    || newNames.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
Func<string> schemeList = () => names.Count == 0 ? "none"
    : string.Join(", ", names.Values.Select(n => "\"" + n + "\""));
Func<string, string> badName = name =>
    name.Length == 0 ? "an empty name"
    : !NamingUtils.IsValidName(name) ? "\"" + name + "\", which holds a character Revit refuses in a name"
    : null;

var planRenames = new List<Tuple<ElementId, string, string>>();   // scheme, from, to
var planCreates = new List<Tuple<string, string>>();              // name, description (null = none given)
var planDescriptions = new List<Tuple<ElementId, string, string>>(); // scheme, its name after this call, description
var whatIsCopied = schemes.FirstOrDefault(s => { try { return !s.IsGrossBuildingArea; } catch (Exception) { return false; } });

if (refused == null && problems.Count == 0)
{
    if (computations != null && volumeAsked.HasValue && computations.ComputeVolumes == volumeAsked.Value)
    {
        alreadyAsAsked++;
        volumeAsked = null;
    }
    if (computations != null && boundaryAsked.HasValue)
    {
        SpatialElementBoundaryLocation now = SpatialElementBoundaryLocation.Finish;
        var read = true;
        try { now = computations.GetSpatialElementBoundaryLocation(SpatialElementType.Room); }
        catch (Exception ex) { read = false; problems.Add("Room Area Computation could not be read: " + ex.Message); }
        if (read && now == boundaryAsked.Value)
        {
            alreadyAsAsked++;
            boundaryAsked = null;
        }
    }

    foreach (var entry in renamesAsked)
    {
        var arrow = entry.IndexOf("->", StringComparison.Ordinal);
        if (arrow < 0)
        {
            problems.Add("\"" + entry + "\" is not a rename - write the old name, \"->\", and the new name.");
            continue;
        }
        var from = entry.Substring(0, arrow).Trim();
        var to = entry.Substring(arrow + 2).Trim();
        var bad = badName(to);
        if (bad != null) { problems.Add("A scheme cannot be renamed to " + bad + "."); continue; }
        var found = idsNamed(from);
        if (found.Count == 0)
        {
            if (idsNamed(to).Count == 1) { alreadyAsAsked++; continue; }   // renamed already
            problems.Add("There is no area scheme called \"" + from + "\" - the schemes are " + schemeList() + ".");
            continue;
        }
        if (found.Count > 1)
        {
            problems.Add("More than one area scheme is called \"" + from + "\" in different capitals - name it exactly.");
            continue;
        }
        if (names[found[0]] == to) { alreadyAsAsked++; continue; }
        var was = names[found[0]];
        names.Remove(found[0]);
        if (taken(to))
        {
            names[found[0]] = was;
            problems.Add("\"" + to + "\" is already an area scheme's name - two schemes cannot share one.");
            continue;
        }
        names[found[0]] = to;
        planRenames.Add(Tuple.Create(found[0], was, to));
    }

    foreach (var entry in createsAsked)
    {
        var colon = entry.IndexOf(':');
        var name = (colon < 0 ? entry : entry.Substring(0, colon)).Trim();
        var description = colon < 0 ? null : entry.Substring(colon + 1).Trim();
        var bad = badName(name);
        if (bad != null) { problems.Add("A new area scheme cannot be called " + bad + "."); continue; }
        if (taken(name))
        {
            if (newNames.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
            {
                problems.Add("\"" + name + "\" is asked for twice as a new area scheme - say it once.");
                continue;
            }
            alreadyAsAsked++;
            findings.Add("An area scheme called \"" + name + "\" is already there, so none was made"
                + (description == null ? "." : ", and its description was left as it is - Area Scheme Description "
                    + "changes it."));
            continue;
        }
        if (whatIsCopied == null)
        {
            problems.Add("Revit's API cannot make an area scheme from nothing; Heron makes one by copying an ordinary "
                + "scheme, and this project has only the Gross Building scheme. Make the first one in Area and Volume "
                + "Computations > Area Schemes > New, then ask again.");
            break;
        }
        newNames.Add(name);
        planCreates.Add(Tuple.Create(name, description));
    }

    foreach (var entry in descriptionsAsked)
    {
        var colon = entry.IndexOf(':');
        if (colon < 0)
        {
            problems.Add("\"" + entry + "\" is not a description - write the scheme's name, a colon, and the description.");
            continue;
        }
        var name = entry.Substring(0, colon).Trim();
        var description = entry.Substring(colon + 1).Trim();
        var planned = planCreates.FirstOrDefault(c => string.Equals(c.Item1, name, StringComparison.OrdinalIgnoreCase));
        if (planned != null)
        {
            planCreates[planCreates.IndexOf(planned)] = Tuple.Create(planned.Item1, description);
            continue;
        }
        var found = idsNamed(name);
        if (found.Count != 1)
        {
            problems.Add(found.Count == 0
                ? "There is no area scheme called \"" + name + "\" - the schemes are " + schemeList() + "."
                : "More than one area scheme is called \"" + name + "\" in different capitals - name it exactly.");
            continue;
        }
        var scheme = schemes.First(s => s.Id == found[0]);
        if (descriptionOf(scheme) == null)
        {
            problems.Add("Revit gives the area scheme \"" + names[found[0]] + "\" no description that can be read here.");
            continue;
        }
        if (descriptionText(scheme) == description) { alreadyAsAsked++; continue; }
        planDescriptions.Add(Tuple.Create(found[0], names[found[0]], description));
    }
}

if (refused == null && problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems);

// ---------------------------------------------------------------------------
// WRITE, THEN READ EVERYTHING BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var rows = new List<string>();
    var roomsBefore = volumesIn(BuiltInCategory.OST_Rooms);
    var spacesBefore = volumesIn(BuiltInCategory.OST_MEPSpaces);
    SpatialElementBoundaryLocation? boundaryBefore = null;
    try { boundaryBefore = computations.GetSpatialElementBoundaryLocation(SpatialElementType.Room); }
    catch (Exception) { }
    SpatialElementBoundaryLocation? spaceBoundaryBefore = null;
    try { spaceBoundaryBefore = computations.GetSpatialElementBoundaryLocation(SpatialElementType.Space); }
    catch (Exception) { }

    if (volumeAsked.HasValue)
    {
        try { computations.ComputeVolumes = volumeAsked.Value; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not set Volume Computations to " + volumeWords(volumeAsked.Value)
                + ": " + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
    }
    if (boundaryAsked.HasValue)
    {
        try { computations.SetSpatialElementBoundaryLocation(boundaryAsked.Value, SpatialElementType.Room); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not set Room Area Computation to "
                + boundaryWords(boundaryAsked.Value) + ": " + ex.Message + " The call failed, and Heron rolls the whole "
                + "call back.");
        }
    }

    foreach (var rename in planRenames)
    {
        try { doc.GetElement(rename.Item1).Name = rename.Item3; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not rename the area scheme \"" + rename.Item2 + "\" to \""
                + rename.Item3 + "\": " + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
    }

    var made = new List<Tuple<ElementId, string, string>>();         // scheme, name, description asked
    foreach (var create in planCreates)
    {
        ICollection<ElementId> copies;
        try { copies = ElementTransformUtils.CopyElement(doc, whatIsCopied.Id, XYZ.Zero); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not copy the area scheme \"" + names[whatIsCopied.Id]
                + "\" to make \"" + create.Item1 + "\": " + ex.Message + " The call failed, and Heron rolls the whole "
                + "call back.");
        }
        var copied = (copies ?? new List<ElementId>()).Select(id => doc.GetElement(id)).Where(e => e != null).ToList();
        var newScheme = copied.OfType<AreaScheme>().FirstOrDefault();
        var others = copied.Where(e => !(e is AreaScheme)).ToList();
        if (newScheme == null)
            throw new InvalidOperationException("Revit copied the area scheme \"" + names[whatIsCopied.Id] + "\" and no "
                + "new area scheme came back, so \"" + create.Item1 + "\" was not made. The call failed, and Heron rolls "
                + "the whole call back.");
        if (newScheme.IsGrossBuildingArea)
            throw new InvalidOperationException("The copy came back as a Gross Building scheme, which a project holds one "
                + "of. The call failed, and Heron rolls the whole call back.");
        if (others.Any(e => e is View || e is Area))
            throw new InvalidOperationException("Copying the scheme \"" + names[whatIsCopied.Id] + "\" also copied "
                + others.Count + " area plan(s) or area(s) with it, which a new scheme must not hold. The call failed, "
                + "and Heron rolls the whole call back.");
        try { newScheme.Name = create.Item1; }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not name the new area scheme \"" + create.Item1 + "\": "
                + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
        var text = descriptionOf(newScheme);
        if (text != null && !text.IsReadOnly) text.Set(create.Item2 ?? "");
        else if (create.Item2 != null)
            throw new InvalidOperationException("Revit gives the new area scheme \"" + create.Item1 + "\" no description "
                + "that can be written here. The call failed, and Heron rolls the whole call back.");
        if (others.Count > 0)
            findings.Add("Copying \"" + names[whatIsCopied.Id] + "\" to make \"" + create.Item1 + "\" also brought "
                + others.Count + " other element(s) with it: " + string.Join(", ", others.Select(e =>
                    (e.Category == null ? e.GetType().Name : e.Category.Name) + " \"" + e.Name + "\"")) + ".");
        made.Add(Tuple.Create(newScheme.Id, create.Item1, create.Item2));
    }

    foreach (var describe in planDescriptions)
    {
        var scheme = doc.GetElement(describe.Item1) as AreaScheme;
        var text = scheme == null ? null : descriptionOf(scheme);
        if (text == null || text.IsReadOnly)
            throw new InvalidOperationException("Revit holds the description of the area scheme \"" + describe.Item2
                + "\" read-only. The call failed, and Heron rolls the whole call back.");
        text.Set(describe.Item3);
    }

    // READ BACK - a fresh look at the settings and the schemes.
    doc.Regenerate();
    var now = AreaVolumeSettings.GetAreaVolumeSettings(doc);
    var after = new FilteredElementCollector(doc).OfClass(typeof(AreaScheme)).Cast<AreaScheme>().ToList();
    Func<string, string> failed = what => what + " The call failed, and Heron rolls the whole call back.";

    if (volumeAsked.HasValue)
    {
        if (now.ComputeVolumes != volumeAsked.Value)
            throw new InvalidOperationException(failed("Volume Computations reads " + volumeWords(now.ComputeVolumes)
                + " after the call, not " + volumeWords(volumeAsked.Value) + "."));
        rows.Add("Volume Computations " + volumeWords(!volumeAsked.Value) + " -> " + volumeWords(now.ComputeVolumes));
        changed++;
    }
    if (boundaryAsked.HasValue)
    {
        var boundaryNow = now.GetSpatialElementBoundaryLocation(SpatialElementType.Room);
        if (boundaryNow != boundaryAsked.Value)
            throw new InvalidOperationException(failed("Room Area Computation reads " + boundaryWords(boundaryNow)
                + " after the call, not " + boundaryWords(boundaryAsked.Value) + "."));
        rows.Add("Room Area Computation " + (boundaryBefore.HasValue ? boundaryWords(boundaryBefore.Value) : "unread")
            + " -> " + boundaryWords(boundaryNow));
        changed++;
        try
        {
            var spaceNow = now.GetSpatialElementBoundaryLocation(SpatialElementType.Space);
            findings.Add(spaceNow == boundaryNow
                ? "Revit reports Spaces measured " + boundaryWords(spaceNow) + " too"
                  + (spaceBoundaryBefore.HasValue && spaceBoundaryBefore.Value != spaceNow
                      ? " (they read " + boundaryWords(spaceBoundaryBefore.Value) + " before)." : ".")
                : "Revit reports Spaces still measured " + boundaryWords(spaceNow) + " - the dialog's one setting is "
                  + "the room one, and it did not move the Spaces.");
        }
        catch (Exception) { }
        findings.Add("Every room's area is re-measured by Room Area Computation, so room schedules, tags and anything "
            + "reading a room's Area change with it.");
    }
    foreach (var rename in planRenames)
    {
        var scheme = after.FirstOrDefault(s => s.Id == rename.Item1);
        if (scheme == null || scheme.Name != rename.Item3)
            throw new InvalidOperationException(failed("The area scheme \"" + rename.Item2 + "\" does not read \""
                + rename.Item3 + "\" after the call."));
        rows.Add("area scheme \"" + rename.Item2 + "\" renamed \"" + scheme.Name + "\"");
        changed++;
    }
    foreach (var create in made)
    {
        var scheme = after.FirstOrDefault(s => s.Id == create.Item1);
        if (scheme == null || scheme.Name != create.Item2)
            throw new InvalidOperationException(failed("The new area scheme \"" + create.Item2 + "\" is not there "
                + "after the call."));
        var description = descriptionText(scheme);
        if (description != null && description != (create.Item3 ?? ""))
            throw new InvalidOperationException(failed("The new area scheme \"" + create.Item2 + "\" reads the "
                + "description \"" + description + "\", not the one asked for."));
        rows.Add("area scheme \"" + scheme.Name + "\" made"
            + (description == null ? "" : description.Length == 0 ? " with no description"
                : ", description \"" + description + "\""));
        changed++;
    }
    foreach (var describe in planDescriptions)
    {
        var scheme = after.FirstOrDefault(s => s.Id == describe.Item1);
        var description = scheme == null ? null : descriptionText(scheme);
        if (description != describe.Item3)
            throw new InvalidOperationException(failed("The area scheme \"" + describe.Item2 + "\" does not read the "
                + "description asked for after the call."));
        rows.Add("area scheme \"" + describe.Item2 + "\" description now \"" + description + "\"");
        changed++;
    }

    var roomsAfter = volumesIn(BuiltInCategory.OST_Rooms);
    var spacesAfter = volumesIn(BuiltInCategory.OST_MEPSpaces);
    roomsWithVolume = roomsAfter.Item2;
    spacesWithVolume = spacesAfter.Item2;

    settingReport = string.Join("; ", rows);
    findings.Insert(0, changed + " setting(s) changed and " + alreadyAsAsked + " already as asked, in \"" + doc.Title
        + "\" - read back: " + (rows.Count == 0 ? "nothing needed changing." : settingReport + "."));
    if (volumeAsked.HasValue)
        findings.Add(string.Format("Volumes now: {0} of {1} placed room(s) and {2} of {3} placed space(s) have one "
            + "(before: {4} room(s) and {5} space(s)).", roomsAfter.Item2, roomsAfter.Item1, spacesAfter.Item2,
            spacesAfter.Item1, roomsBefore.Item2, spacesBefore.Item2));
    if (made.Count > 0)
        findings.Add("Each new area scheme was copied from \"" + names[whatIsCopied.Id] + "\" and holds no area plan "
            + "yet - one is made in View > Plan Views > Area Plan, choosing the scheme.");
}

if (refused != null) findings.Add(refused);
