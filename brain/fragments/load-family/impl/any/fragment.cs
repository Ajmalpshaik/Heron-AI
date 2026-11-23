// NOT STANDALONE. Assumes `doc`, `familyLoadOptions`, `familyPath`, `reload`
// and `overwriteValues` are in scope; leaves `created`, `refused`,
// `typeNames`, `reloaded`, `typesAdded`, `typesKept`, `materialsAdded`,
// `materialsChanged` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one. `doc`
// may be a project OR a family document - loading into a family is how a
// nested family gets in, and LoadFamily is the same call on both.
//
// RELOADING IS ASKED FOR, NEVER ASSUMED. With `reload` left off, a family of
// the same name already in `doc` is REFUSED, exactly as version 1 did: silently
// overwriting is how a colleague's edited type is replaced by the office
// standard mid-job, taking its parameter values with it. Constitution
// Article 7 lists family reloads among the operations that need explicit
// confirmation for that specific operation - `reload=true` is that
// confirmation, typed by the caller, never defaulted.
//
// THE OVERWRITE NEEDS AN IFamilyLoadOptions, WHICH NEEDS A CLASS, and a
// fragment is a body of statements with nowhere to declare one. Version 1
// stopped there. D-114 has the add-in supply the class through the ambient
// need `familyLoadOptions` - a factory whose bool is Revit's
// overwriteParameterValues - the same way `doc` and D-112's failure
// discipline are supplied. This fragment still decides WHETHER and HOW; the
// object only answers Revit's "Family Already Exists" dialog as told:
//
//   reload=true                         "Overwrite the existing version"
//   reload=true, overwriteValues=true   "Overwrite the existing version and
//                                        its parameter values"
//
// A SHARED NESTED FAMILY found inside the loaded file takes the FILE's version
// too (FamilySource.Family) - the nut edited and saved on disk is the one the
// reload was asked for.
//
// MATERIALS ARE READ BEFORE AND AFTER, because a load carries the family's
// material definitions in and can overwrite same-named materials in `doc` with
// no dialog naming it. Measured 2026-09-16: six copper fitting families reset a
// project's Copper colour from 184,115,51 to 128,0,0. No element count can see
// that, so every material whose colour, transparency, shininess, smoothness or
// appearance asset moved is named in `materialsChanged`, before -> after.
//
// THE FILE IS CHECKED BEFORE THE CALL, because LoadFamily's own failure for a
// missing path is indistinguishable from its failure for an already-loaded
// family - both are a bare `false` - and those two need completely different
// things done about them.
//
// VERSION 3, 2026-10-07 - A FILE OPEN IN REVIT WITH UNSAVED CHANGES IS REFUSED.
// LoadFamily reads the FILE, never the window: a tyre family built in its own
// window and not yet saved would load as whatever its file last held - an
// empty template - and report success (row 5b-360). Open and saved is fine:
// then the file and the window agree.
//
// VERSION 4, 2026-11-23 - MATERIALS ARE COMPARED BY ELEMENT ID, NEVER BY NAME.
// Versions 1 to 3 keyed the before-reading on the material's name, believing
// names unique in a document. They are not: a project upgraded and copied
// into for years holds three materials named Default, three named Poche, two
// each of Phase-Demo, Phase-Exist and Phase-Temp. The last of each name read
// won the before-reading, so every other one was compared against a
// DIFFERENT material, and a NEW load reported 8 "changes" that never happened
// (row 5b-1995, Revit 2020, 2026-11-23 - an old Poche already blue at id 51455
// listed as "0,0,0 -> 0,0,255"). Now each material is read by its own id before
// the load and the SAME id after: an id not there before is in
// `materialsAdded`, an id whose reading moved is in `materialsChanged`. A
// name the document holds more than once is never paired by guess - its
// lines carry the id, and `findings` says the name repeats.

ElementId created = null;
string refused = null;
var typeNames = new List<string>();
string reloaded = null;
var typesAdded = new List<string>();
var typesKept = new List<string>();
var materialsAdded = new List<string>();
var materialsChanged = new List<string>();
var findings = new List<string>();

var path = (familyPath ?? "").Trim();

if (path.Length == 0)
{
    refused = "no family file was given";
}
else if (!path.ToLowerInvariant().EndsWith(".rfa"))
{
    refused = string.Format(
        "\"{0}\" is not a .rfa file. A project (.rvt) is linked, not loaded, and a "
        + "template (.rft) is what a family is built FROM", path);
}
else if (!System.IO.File.Exists(path))
{
    // Checked separately because LoadFamily returns a bare `false` for both a
    // missing file and an already-loaded family, and those need opposite
    // responses.
    refused = string.Format("there is no file at \"{0}\"", path);
}
else if (overwriteValues && !reload)
{
    // Refused rather than read as a reload. Overwriting values is the
    // stronger of Revit's two answers, and it is never inferred from a switch
    // that only makes sense once the weaker one has been asked for.
    refused = "overwriteValues=true only means something when a loaded family is "
        + "being reloaded. Say reload=true as well if the family in the "
        + (doc.IsFamilyDocument ? "family" : "project")
        + " should be overwritten, values and all";
}
else
{
    // THE SAME FILE OPEN IN A WINDOW WITH CHANGES NOT SAVED - found by its
    // path among what this Revit has open, links aside.
    var wantedFile = path;
    try { wantedFile = System.IO.Path.GetFullPath(path); }
    catch (Exception) { wantedFile = path; }
    foreach (Document open in doc.Application.Documents)
    {
        if (open == null || open.IsLinked || open.Equals(doc)) continue;
        var openFile = "";
        try { openFile = open.PathName; }
        catch (Exception) { openFile = ""; }
        if (openFile.Length == 0) continue;
        try { openFile = System.IO.Path.GetFullPath(openFile); }
        catch (Exception) { }
        if (string.Equals(openFile, wantedFile, StringComparison.OrdinalIgnoreCase)
            && open.IsModified)
        {
            refused = string.Format(
                "\"{0}\" is open in Revit with changes that are NOT SAVED, and a load reads "
                + "the file - which does not have them. Nothing was loaded. Save it first "
                + "(SAVE_DOCUMENT names it by this path), then load it", open.Title);
            break;
        }
    }
}

// Every check above that declines sets `refused`, so reaching here with none
// set means the file is a .rfa that exists and is not open with unsaved work.
if (refused == null)
{
    // Revit names a loaded family after its file. Looked up FIRST, so a
    // family that is already here is refused by name - or, when a reload was
    // asked for, so its types can be compared before and after.
    var familyName = System.IO.Path.GetFileNameWithoutExtension(path);
    Family existing = null;
    foreach (Family candidate in new FilteredElementCollector(doc).OfClass(typeof(Family)))
    {
        if (string.Equals(candidate.Name, familyName, StringComparison.OrdinalIgnoreCase))
        {
            existing = candidate;
            break;
        }
    }

    var where = doc.IsFamilyDocument ? "this family" : "the project";

    if (doc.IsFamilyDocument && doc.OwnerFamily != null
        && string.Equals(doc.OwnerFamily.Name, familyName, StringComparison.OrdinalIgnoreCase)
        && existing == null)
    {
        // A family document's own name is its file's, so this is very likely
        // the family being loaded into itself - which Revit refuses anyway,
        // and saying so beats its bare `false`.
        refused = string.Format(
            "\"{0}\" has the same name as the family open here. A family cannot be "
            + "loaded into itself - open the family it should be NESTED in and load "
            + "it there", familyName);
    }
    else if (existing != null && !reload)
    {
        refused = string.Format(
            "a family named \"{0}\" is ALREADY in {1}, so nothing was loaded. Reloading "
            + "overwrites it, and that is never done without being asked: say "
            + "reload=true to take the file's version (the types' values in {1} are "
            + "kept), or reload=true with overwriteValues=true to take the file's "
            + "values as well", existing.Name, where);
    }
    else
    {
        var before = new HashSet<string>(StringComparer.Ordinal);
        if (existing != null)
        {
            foreach (var typeId in existing.GetFamilySymbolIds())
            {
                var symbol = doc.GetElement(typeId);
                if (symbol != null) before.Add(symbol.Name);
            }
        }

        // One reading per material. Its NAME is not part of it and never the
        // key: a document can hold several materials of one name (version 4).
        Func<Material, string> describe = delegate (Material m)
        {
            var colour = m.Color;
            var rgb = colour != null && colour.IsValid
                ? colour.Red + "," + colour.Green + "," + colour.Blue
                : "none";
            return "colour " + rgb
                + ", transparency " + m.Transparency
                + ", shininess " + m.Shininess
                + ", smoothness " + m.Smoothness
                + ", appearance asset " + m.AppearanceAssetId;
        };

        // Keyed by ElementId, which is unique and survives the load. ElementId
        // compares by value, so the after-reading finds the same material.
        var materialsBefore = new Dictionary<ElementId, string>();
        foreach (Material material in new FilteredElementCollector(doc).OfClass(typeof(Material)))
            materialsBefore[material.Id] = describe(material);

        Family family;
        bool loaded;
        IFamilyLoadOptions answer = null;
        if (reload)
        {
            answer = familyLoadOptions(overwriteValues);
            loaded = doc.LoadFamily(path, answer, out family);
        }
        else
        {
            loaded = doc.LoadFamily(path, out family);
        }

        // Revit can report success and hand back no family when it REPLACED
        // one - the family is the one that was already here.
        if (loaded && family == null && existing != null && existing.IsValidObject)
            family = existing;

        if (!loaded || family == null)
        {
            if (existing != null && answer != null && answer.ToString() == "not asked")
            {
                // Revit never reached its "Family Already Exists" question:
                // the file holds the same version as the family already here.
                refused = string.Format(
                    "Revit did not reload \"{0}\": the file is the same version as the "
                    + "family already in {1}, so there was nothing to overwrite. If it was "
                    + "edited, SAVE it first - a reload reads the file on disk, not the "
                    + "window it is open in", existing.Name, where);
            }
            else
            {
                refused = string.Format(
                    "Revit did not load \"{0}\". The usual reason is a family saved by a "
                    + "NEWER Revit than this one, which Revit itself refuses", path);
            }
        }
        else
        {
            created = family.Id;
            reloaded = existing == null
                ? "no - it was not in " + where + " yet, so it was loaded new"
                : (overwriteValues ? "version and values" : "version");

            // The types are what somebody actually needs next - a family with
            // one type and a family with forty are placed very differently,
            // and the names are what PLACE_FAMILY_INSTANCES gets asked for.
            foreach (var typeId in family.GetFamilySymbolIds())
            {
                var symbol = doc.GetElement(typeId);
                if (symbol == null) continue;
                typeNames.Add(symbol.Name);
                if (before.Contains(symbol.Name)) typesKept.Add(symbol.Name);
                else typesAdded.Add(symbol.Name);
            }

            // Read once, then counted by name, so a name held more than once
            // is labelled with each material's id rather than paired by guess.
            var materialsAfter = new List<Material>();
            foreach (Material material in new FilteredElementCollector(doc).OfClass(typeof(Material)))
                materialsAfter.Add(material);
            var nameCount = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var material in materialsAfter)
            {
                int seen;
                nameCount.TryGetValue(material.Name, out seen);
                nameCount[material.Name] = seen + 1;
            }

            foreach (var material in materialsAfter)
            {
                var repeated = nameCount[material.Name] > 1;
                var label = repeated
                    ? material.Name + " (id " + material.Id + ")"
                    : material.Name;
                string was;
                var now = describe(material);
                if (!materialsBefore.TryGetValue(material.Id, out was))
                    materialsAdded.Add(label);
                else if (was != now)
                    materialsChanged.Add(label + ": " + was + " -> " + now);
            }

            // ONE line naming every repeated name, so a modeller who knows
            // the project has three Poche materials reads why each line
            // carries an id - and why none was "changed" into another.
            var repeatedNames = new List<string>();
            foreach (var pair in nameCount)
                if (pair.Value > 1) repeatedNames.Add(pair.Key + " (" + pair.Value + ")");
            if (repeatedNames.Count > 0)
            {
                repeatedNames.Sort(StringComparer.OrdinalIgnoreCase);
                findings.Add(string.Format(
                    "{0} holds more than one material of these names: {1}. Each material "
                    + "was compared with ITSELF by element id, before and after the load - "
                    + "never with another of its name - and any added or changed line for "
                    + "one of them carries its id",
                    where.Substring(0, 1).ToUpperInvariant() + where.Substring(1),
                    string.Join(", ", repeatedNames.ToArray())));
            }

            typeNames.Sort(StringComparer.OrdinalIgnoreCase);
            typesAdded.Sort(StringComparer.OrdinalIgnoreCase);
            typesKept.Sort(StringComparer.OrdinalIgnoreCase);
            materialsAdded.Sort(StringComparer.OrdinalIgnoreCase);
            materialsChanged.Sort(StringComparer.OrdinalIgnoreCase);
            findings.Sort(StringComparer.OrdinalIgnoreCase);
        }
    }
}
