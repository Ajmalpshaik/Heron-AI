// NOT STANDALONE. Assumes `doc`, `sweeps`, `profile` and `flipped` are in
// scope; leaves `changed`, `profileReport`, `notAFamily`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// A SWEEP DRAWN WITH A LOADED PROFILE FAMILY - the sweep's Profile in its
// Properties, set to a profile family loaded into this one instead of a shape
// sketched in place: a handrail's section, a skirting, a gasket, a pipe
// insulation ring. The profile is its own family, so one profile serves many
// sweeps, a change to it reaches all of them when it is loaded again, and a
// Family Type parameter can swap it per type (ADD_FAMILY_TYPE_PARAMETER).
//
// THE SWEEPS ARE NAMED BY ID - the id CREATE_FAMILY_SWEEP gives back, or
// REPORT_FAMILY_FORMS reads - commas between. A swept blend has two profiles
// and is refused by name rather than half set.
//
// THE PROFILE IS A TYPE OF A PROFILE FAMILY ALREADY LOADED HERE, named
// "Family : Type" or by its type name when that is unique; a name not loaded
// is refused with the profiles this family holds - LOAD_FAMILY first.
//
// READ BACK, ALL OR NOTHING: each sweep's Profile, its flip, and that it still
// holds a solid are read again; one that does not read as asked fails the call,
// and the host rolls the whole call back.

var findings = new List<string>();
var changed = 0;
var profileReport = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
Func<FamilySymbol, string> label = s => (s.Family == null ? "" : s.Family.Name + " : ") + s.Name;

var problems = new List<string>();
var targets = new List<Sweep>();
FamilySymbol chosen = null;

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A sweep's profile is set inside the family - open it first.";
}
else
{
    var profileCategory = new ElementId(BuiltInCategory.OST_ProfileFamilies);
    var loaded = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).Cast<FamilySymbol>()
        .Where(s => s.Family != null && s.Family.FamilyCategory != null && s.Family.FamilyCategory.Id == profileCategory)
        .ToList();
    var key = squash(profile);
    var whole = loaded.Where(s => squash(label(s)) == key).ToList();
    var bare = loaded.Where(s => squash(s.Name) == key).ToList();
    chosen = whole.Count == 1 ? whole[0] : bare.Count == 1 ? bare[0] : null;
    if (chosen == null)
        problems.Add((key.Length == 0 ? "No profile was named." : whole.Count + bare.Count > 1
                ? "\"" + profile + "\" names more than one loaded profile - write it \"Family : Type\"."
                : "No profile family loaded here is called \"" + profile + "\".")
            + " Profiles in this family: " + (loaded.Count == 0 ? "none - LOAD_FAMILY a profile family first"
                : string.Join(", ", loaded.Select(label).OrderBy(n => n).Take(15))) + ".");

    var tokens = (sweeps ?? "").Split(',').Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
    if (tokens.Count == 0) problems.Add("No sweep was named - its id, commas between.");
    foreach (var token in tokens)
    {
        var element = doc.GetElement(token);
        if (element is SweptBlend)
            problems.Add("\"" + token + "\" is a swept blend, which has two profiles; only a sweep is set here.");
        else if (!(element is Sweep))
            problems.Add("\"" + token + "\" is not the id of a sweep in this family.");
        else if (!targets.Any(t => t.Id == element.Id)) targets.Add((Sweep)element);
    }
    foreach (var sweep in targets)
    {
        var row = sweep.get_Parameter(BuiltInParameter.PROFILE_FAM_TYPE);
        if (row == null || row.StorageType != StorageType.ElementId)
            problems.Add("The sweep " + sweep.UniqueId + " shows no Profile Revit lets be set.");
        else if (row.IsReadOnly)
            problems.Add("Revit holds the sweep " + sweep.UniqueId + "'s Profile read-only"
                + (sweep.ProfileSketch != null ? " - it is drawn with a sketched profile, which Revit keeps for the "
                    + "sweep it was sketched in" : "") + ".");
    }

    if (problems.Count > 0) refused = "Nothing was changed. " + string.Join(" ", problems);
}

// ---------------------------------------------------------------------------
// SET, THEN READ EVERY SWEEP BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    if (!chosen.IsActive) chosen.Activate();
    var rows = new List<string>();
    var unflipped = 0;
    foreach (var sweep in targets)
    {
        var row = sweep.get_Parameter(BuiltInParameter.PROFILE_FAM_TYPE);
        var before = row.AsElementId();
        var flipBefore = sweep.get_Parameter(BuiltInParameter.PROFILE_FLIPPED_HOR);
        var wasFlipped = flipBefore != null && flipBefore.AsInteger() == 1;
        try
        {
            if (before != chosen.Id) row.Set(chosen.Id);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not set the sweep " + sweep.UniqueId + "'s profile to "
                + label(chosen) + ": " + ex.Message + " The call failed, and Heron rolls the whole call back.");
        }
        // THE FLIP IS READ AGAIN AFTER THE PROFILE IS SET: whether a sweep drawn
        // with a sketched profile carries Profile Is Flipped at all, or only once
        // a loaded profile is in, no remark says (BT13).
        var flipRow = sweep.get_Parameter(BuiltInParameter.PROFILE_FLIPPED_HOR);
        var flipNow = flipRow == null ? (int?)null : flipRow.AsInteger();
        if (flipNow != (flipped ? 1 : 0))
        {
            if (flipRow == null || flipRow.IsReadOnly)
            {
                if (flipped || flipNow == 1)
                    throw new InvalidOperationException("The sweep " + sweep.UniqueId + " has no Profile Is Flipped "
                        + "Revit lets be " + (flipped ? "set" : "cleared") + " - the profile is set, the flip cannot be. "
                        + "The call failed, and Heron rolls the whole call back.");
            }
            else
            {
                try { flipRow.Set(flipped ? 1 : 0); }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Revit would not " + (flipped ? "flip" : "unflip") + " the sweep "
                        + sweep.UniqueId + "'s profile: " + ex.Message + " The call failed, and Heron rolls the whole call "
                        + "back.");
                }
            }
        }
        if (wasFlipped && !flipped) unflipped++;
        if (before != chosen.Id || wasFlipped != flipped) changed++;
    }

    doc.Regenerate();

    foreach (var sweep in targets)
    {
        var now = sweep.get_Parameter(BuiltInParameter.PROFILE_FAM_TYPE).AsElementId();
        if (now != chosen.Id)
            throw new InvalidOperationException("The sweep " + sweep.UniqueId + " reads another profile after the call, "
                + "not " + label(chosen) + ". The call failed, and Heron rolls the whole call back.");
        var flipRow = sweep.get_Parameter(BuiltInParameter.PROFILE_FLIPPED_HOR);
        if ((flipRow == null && flipped) || (flipRow != null && flipRow.AsInteger() != (flipped ? 1 : 0)))
            throw new InvalidOperationException("The sweep " + sweep.UniqueId + " does not read "
                + (flipped ? "flipped" : "unflipped") + " as asked. The call failed, and Heron rolls the whole call back.");
        var volume = 0.0;
        var geometry = sweep.get_Geometry(new Options());
        if (geometry != null)
            foreach (GeometryObject piece in geometry)
            {
                var body = piece as Solid;
                if (body != null && body.Volume > 0) volume += body.Volume;
            }
        if (sweep.IsSolid && volume <= 0)
            throw new InvalidOperationException("The sweep " + sweep.UniqueId + " holds no solid with " + label(chosen)
                + " as its profile - the profile does not sweep along this path. The call failed, and Heron rolls the "
                + "whole call back.");
        rows.Add(sweep.UniqueId + (sweep.IsSolid ? ", " + Math.Round(volume * 28.316846592, 3).ToString(invariant)
            + " L" : ", void"));
    }

    profileReport = targets.Count + " sweep(s) drawn with " + label(chosen) + (flipped ? ", flipped" : "") + ": "
        + string.Join("; ", rows) + " - read back.";
    findings.Add(profileReport);
    if (unflipped > 0)
        findings.Add(unflipped + " sweep(s) were flipped before and are NOT flipped now, as asked.");
    findings.Add(changed + " changed. A Family Type parameter of the Profiles category can now swap this profile per "
        + "type - ADD_FAMILY_TYPE_PARAMETER, naming these sweeps.");
}

if (refused != null) findings.Add(refused);
