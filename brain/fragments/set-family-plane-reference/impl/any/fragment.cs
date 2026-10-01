// NOT STANDALONE. Assumes `doc` and `planes` are in scope; leaves
// `planeReport`, `changed`, `alreadySet`, `notAFamily`, `refused` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// IS REFERENCE AND DEFINES ORIGIN - the two rows of a reference plane's
// Properties that decide how a family behaves once it is placed in a project:
// which of its planes a project can dimension and snap to - Left, Right,
// Front, Back, Top, Bottom and the three centres, one plane each; Strong or
// Weak Reference for any other; Not a Reference for a plane only the family's
// own build needs - and which planes fix the point it is placed by.
//
// THE NUMBERS BEHIND IS REFERENCE ARE NOT DOCUMENTED, SO NONE IS ASSUMED. Each
// value is written and READ BACK in Revit's own words. A number another plane
// already carries with the words asked for is tried first - Revit's own pair -
// then the one FamilyInstanceReferenceType gives, the enum the API says
// "corresponds to" these values, then the others in turn. A value whose words
// never come back fails the call. Revit's English words are what is compared.
//
// SIDES AND CENTRES SUIT THE WAY A PLANE FACES: Left, Right and Center
// (Left/Right) a plane facing left or right; Front, Back and Center
// (Front/Back) one facing front or back; Top, Bottom and Center (Elevation) a
// horizontal one. Each of those nine is one plane's at a time, so one already
// held by a plane the call does not name is refused with that plane's name -
// what the other plane becomes is the modeller's to say, in the same call.
//
// DEFINES ORIGIN MOVES, IT IS NEVER ADDED. "origin" makes a plane the one plane
// facing its way that defines the family's origin - the point a project places
// the family by - and the plane that did is cleared, and both are reported.
//
// NOTHING ELSE MAY MOVE. Both rows of every plane are read before the first
// write and after the last. A plane the call did not name that reads
// differently - Revit moving a side from it when another plane takes it, say -
// is put back, and one that will not go back fails the call.

var findings = new List<string>();
var planeReport = "";
var changed = 0;
var alreadySet = 0;
var notAFamily = false;
string refused = null;

Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Replace("centre", "center").Where(c => char.IsLetterOrDigit(c)).ToArray());

// Which world axis a direction lies along: 0 X, 1 Y, 2 Z, -1 none of them.
Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
var facing = new[] { "faces left or right", "faces front or back", "is horizontal" };

// IS REFERENCE AS REVIT OFFERS IT: the key it is compared by, Revit's words,
// the axis a plane holding it must face (-1 any), and the API enum member that
// "corresponds to" it.
var settings = new List<Tuple<string, string, int, string>>
{
    Tuple.Create("notareference", "Not a Reference", -1, "NotAReference"),
    Tuple.Create("strongreference", "Strong Reference", -1, "StrongReference"),
    Tuple.Create("weakreference", "Weak Reference", -1, "WeakReference"),
    Tuple.Create("left", "Left", 0, "Left"),
    Tuple.Create("centerleftright", "Center (Left/Right)", 0, "CenterLeftRight"),
    Tuple.Create("right", "Right", 0, "Right"),
    Tuple.Create("front", "Front", 1, "Front"),
    Tuple.Create("centerfrontback", "Center (Front/Back)", 1, "CenterFrontBack"),
    Tuple.Create("back", "Back", 1, "Back"),
    Tuple.Create("bottom", "Bottom", 2, "Bottom"),
    Tuple.Create("centerelevation", "Center (Elevation)", 2, "CenterElevation"),
    Tuple.Create("top", "Top", 2, "Top"),
};
var shorthand = new Dictionary<string, string>
{
    { "none", "notareference" }, { "notreference", "notareference" },
    { "strong", "strongreference" }, { "weak", "weakreference" },
};

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};
Func<ReferencePlane, Parameter> isReferenceOf = rp =>
    rp.get_Parameter(BuiltInParameter.ELEM_REFERENCE_NAME) ?? rp.get_Parameter(BuiltInParameter.ELEM_REFERENCE_NAME_2D_XZ);
Func<ReferencePlane, Parameter> originOf = rp => rp.get_Parameter(BuiltInParameter.DATUM_PLANE_DEFINES_ORIGIN);
Func<Parameter, string> words = p =>
{
    try { return p == null ? "" : (p.AsValueString() ?? ""); }
    catch (Exception) { return ""; }
};
Func<Parameter, int?> stored = p =>
{
    try { return p == null || p.StorageType != StorageType.Integer ? (int?)null : p.AsInteger(); }
    catch (Exception) { return null; }
};
Func<Parameter, string> yesNo = p => stored(p) == 1 ? "Yes" : "No";

// Every reference plane as it stands before the call: the plane, its name, the
// axis it faces, Is Reference stored and in words, and Defines Origin stored.
var before = new List<Tuple<ReferencePlane, string, int, int?, string, int?>>();
// Each plane named: the plane, its name, the setting asked for (null to leave
// Is Reference alone), and whether it is to define the origin.
var plans = new List<Tuple<ReferencePlane, string, Tuple<string, string, int, string>, bool>>();
var problems = new List<string>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front, \"" + doc.Title + "\", is a project, not a family open in the Family Editor. "
        + "A reference plane's Is Reference is set inside the family - open it for editing first "
        + "(OPEN_FAMILY_FOR_EDITING). Nothing was changed.";
}
else
{
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
        before.Add(Tuple.Create(rp, ownName(rp), axisOf(rp.Normal), stored(isReferenceOf(rp)), words(isReferenceOf(rp)),
            stored(originOf(rp))));
    var namedPlanes = before.Where(b => b.Item2.Length > 0).Select(b => b.Item2).Distinct().OrderBy(n => n).ToList();
    var offered = string.Join(", ", settings.Select(s => s.Item2)) + ", each optionally followed by \", origin\"";

    if (planes == null || planes.Count == 0)
        problems.Add("No planes were named - \"Box Left=Left; Box Right=Right; Insert=Strong Reference, origin\", "
            + "semicolons between.");
    else
        foreach (var pair in planes)
        {
            var name = (pair.Key ?? "").Trim();
            var found = before.Where(b => string.Equals(b.Item2, name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (found.Count == 0)
            {
                problems.Add("No reference plane is called \"" + name + "\". This family's named planes: "
                    + (namedPlanes.Count == 0 ? "none" : string.Join(", ", namedPlanes.Take(30))) + ".");
                continue;
            }
            if (found.Count > 1)
            {
                problems.Add("\"" + name + "\" names " + found.Count + " planes - rename one first.");
                continue;
            }
            var plane = found[0];
            if (plans.Any(p => p.Item1.Id == plane.Item1.Id))
            {
                problems.Add("\"" + name + "\" is named twice - say it once.");
                continue;
            }

            Tuple<string, string, int, string> setting = null;
            var origin = false;
            var bad = false;
            foreach (var part in (pair.Value ?? "").Split(new[] { ',', '+' }).Select(p => p.Trim()).Where(p => p.Length > 0))
            {
                var key = squash(part);
                if (key == "origin" || key == "definesorigin") { origin = true; continue; }
                if (shorthand.ContainsKey(key)) key = shorthand[key];
                var match = settings.FirstOrDefault(s => s.Item1 == key);
                if (match == null)
                {
                    problems.Add("\"" + part + "\" for \"" + name + "\" is not one of Revit's Is Reference values - "
                        + offered + ".");
                    bad = true;
                }
                else if (setting != null && setting.Item1 != match.Item1)
                {
                    problems.Add("\"" + name + "\" is given both " + setting.Item2 + " and " + match.Item2 + " - a plane "
                        + "has one.");
                    bad = true;
                }
                else setting = match;
            }
            if (bad) continue;
            if (setting == null && !origin)
            {
                problems.Add("Nothing was asked of \"" + name + "\" - " + offered + ".");
                continue;
            }
            if (setting != null && isReferenceOf(plane.Item1) == null)
            {
                problems.Add("\"" + name + "\" has no Is Reference row.");
                continue;
            }
            if (setting != null && isReferenceOf(plane.Item1).IsReadOnly)
            {
                problems.Add("Revit does not let \"" + name + "\"'s Is Reference be changed here.");
                continue;
            }
            if (setting != null && setting.Item3 >= 0 && plane.Item3 != setting.Item3)
            {
                problems.Add(setting.Item2 + " goes on a plane that " + facing[setting.Item3] + ", and \"" + name + "\" "
                    + (plane.Item3 < 0 ? "is at an angle" : facing[plane.Item3]) + ".");
                continue;
            }
            if (origin && plane.Item3 < 0)
            {
                problems.Add("\"" + name + "\" is at an angle, and the origin is fixed by planes that face left or "
                    + "right, front or back, or up.");
                continue;
            }
            if (origin && (originOf(plane.Item1) == null || originOf(plane.Item1).IsReadOnly))
            {
                problems.Add("Revit does not let \"" + name + "\" define the origin - it has no Defines Origin row it "
                    + "lets be set.");
                continue;
            }
            plans.Add(Tuple.Create(plane.Item1, plane.Item2, setting, origin));
        }

    // EACH SIDE AND CENTRE IS ONE PLANE'S. Two named for one, or one held by a
    // plane the call leaves alone, is refused - never moved by this code.
    foreach (var side in plans.Where(p => p.Item3 != null && p.Item3.Item3 >= 0).GroupBy(p => p.Item3.Item1))
    {
        if (side.Count() > 1)
            problems.Add(side.First().Item3.Item2 + " is asked of " + string.Join(" and ", side.Select(p => "\"" + p.Item2 + "\""))
                + " - it is one plane's.");
        // Held now by a plane that is not taking it here and is not being given
        // something else in this call.
        var holder = before.FirstOrDefault(b => squash(b.Item5) == side.Key
            && !side.Any(p => p.Item1.Id == b.Item1.Id)
            && !plans.Any(p => p.Item1.Id == b.Item1.Id && p.Item3 != null));
        if (holder != null)
            problems.Add(side.First().Item3.Item2 + " is already held by " + (holder.Item2.Length > 0
                    ? "\"" + holder.Item2 + "\". Name that plane in the same call with what it becomes - Strong "
                      + "Reference, say - so the side is moved by the modeller, not by Heron."
                    : "an unnamed plane, which cannot be named in a call - give it a name in Properties first."));
    }
    foreach (var axis in plans.Where(p => p.Item4).GroupBy(p => axisOf(p.Item1.Normal)).Where(g => g.Count() > 1))
        problems.Add(string.Join(" and ", axis.Select(p => "\"" + p.Item2 + "\"")) + " both face the same way, and one "
            + "plane each way defines the origin.");

    if (problems.Count > 0)
    {
        var shown = problems.Take(12).ToList();
        refused = "Nothing was changed. " + string.Join(" ", shown)
            + (problems.Count > shown.Count ? " And " + (problems.Count - shown.Count) + " more like these." : "");
    }
}

// ---------------------------------------------------------------------------
// WRITE, PUT BACK ANYTHING ELSE THAT MOVED, READ EVERYTHING BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    // What the call asked of each plane: its Is Reference, its origin.
    var settingAsked = new HashSet<ElementId>(plans.Where(p => p.Item3 != null).Select(p => p.Item1.Id));
    var originAsked = new HashSet<ElementId>(plans.Where(p => p.Item4).Select(p => p.Item1.Id));
    var cleared = new List<Tuple<ReferencePlane, string, string>>();
    var rows = new List<string>();
    // The number each Is Reference value was written as - evidence for what
    // Revit stores, which nothing documents (NEEDS-CHECKING BP4).
    var numbers = new List<string>();

    // A side or a centre is written after every plain reference, so a plane
    // giving one up in the same call has given it up first.
    foreach (var plan in plans.Where(p => p.Item3 != null).OrderBy(p => p.Item3.Item3 >= 0 ? 1 : 0))
    {
        var row = isReferenceOf(plan.Item1);
        if (squash(words(row)) == plan.Item3.Item1) { alreadySet++; continue; }

        var heard = new List<string>();
        var candidates = new List<int>();
        foreach (var b in before.Where(b => squash(b.Item5) == plan.Item3.Item1 && b.Item4.HasValue)) candidates.Add(b.Item4.Value);
        try { candidates.Add((int)Enum.Parse(typeof(FamilyInstanceReferenceType), plan.Item3.Item4)); }
        catch (Exception) { }
        for (var v = 0; v <= 30; v++) candidates.Add(v);

        var found = false;
        foreach (var value in candidates.Distinct())
        {
            try { row.Set(value); }
            catch (Exception) { continue; }
            var said = words(row);
            if (squash(said) == plan.Item3.Item1)
            {
                found = true;
                var entry = plan.Item3.Item2 + " " + value;
                if (!numbers.Contains(entry)) numbers.Add(entry);
                break;
            }
            if (said.Length > 0 && !heard.Contains(said)) heard.Add(said);
        }
        if (!found)
            throw new InvalidOperationException("Revit offered no Is Reference value on \"" + plan.Item2 + "\" that reads \""
                + plan.Item3.Item2 + "\" - it read " + (heard.Count == 0 ? "nothing" : string.Join(", ", heard))
                + ". The call failed, and Heron rolls the whole call back.");
    }

    foreach (var plan in plans.Where(p => p.Item4))
    {
        var row = originOf(plan.Item1);
        if (stored(row) == 1) { alreadySet++; continue; }
        try { row.Set(1); }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit would not let \"" + plan.Item2 + "\" define the origin: " + ex.Message
                + " The call failed, and Heron rolls the whole call back.");
        }
        // EVERY PLANE FACING THE SAME WAY THAT DEFINED THE ORIGIN BEFORE THE
        // CALL gives it up - whether Revit cleared it on its own when this one
        // took it or not, so the put-back below never hands it back.
        var axis = axisOf(plan.Item1.Normal);
        foreach (var other in before.Where(b => b.Item1.Id != plan.Item1.Id && b.Item3 == axis && b.Item6 == 1))
        {
            cleared.Add(Tuple.Create(other.Item1, other.Item2, plan.Item2));
            if (stored(originOf(other.Item1)) != 1) continue;
            try { originOf(other.Item1).Set(0); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit would not clear Defines Origin on \"" + other.Item2 + "\" when \""
                    + plan.Item2 + "\" took it: " + ex.Message + " The call failed, and Heron rolls the whole call back.");
            }
        }
    }

    // NOTHING ELSE MAY MOVE: a row the call did not ask about goes back to how
    // it read, and an origin cleared on purpose is the one exception.
    var putBack = new List<string>();
    foreach (var b in before)
    {
        var row = isReferenceOf(b.Item1);
        if (!settingAsked.Contains(b.Item1.Id) && b.Item4.HasValue && stored(row) != b.Item4)
        {
            try { row.Set(b.Item4.Value); putBack.Add("\"" + b.Item2 + "\" Is Reference"); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("\"" + b.Item2 + "\"'s Is Reference was not asked about, and it moved "
                    + "from " + b.Item5 + " to " + words(row) + " while this ran and would not go back: " + ex.Message
                    + " The call failed, and Heron rolls the whole call back.");
            }
        }
        var origin = originOf(b.Item1);
        if (!originAsked.Contains(b.Item1.Id) && !cleared.Any(c => c.Item1.Id == b.Item1.Id)
            && b.Item6.HasValue && stored(origin) != b.Item6)
        {
            try { origin.Set(b.Item6.Value); putBack.Add("\"" + b.Item2 + "\" Defines Origin"); }
            catch (Exception ex)
            {
                throw new InvalidOperationException("\"" + b.Item2 + "\"'s Defines Origin was not asked about, and it "
                    + "changed while this ran and would not go back: " + ex.Message + " The call failed, and Heron rolls "
                    + "the whole call back.");
            }
        }
    }

    doc.Regenerate();

    foreach (var b in before)
    {
        var plan = plans.FirstOrDefault(p => p.Item1.Id == b.Item1.Id);
        var row = isReferenceOf(b.Item1);
        var nowWords = words(row);
        var nowOrigin = stored(originOf(b.Item1));
        var wasCleared = cleared.FirstOrDefault(c => c.Item1.Id == b.Item1.Id);

        // A CLEARED ORIGIN must read cleared, named in the call or not; any
        // other plane's Defines Origin must read as it did unless it was asked.
        var parts = new List<string>();
        if (wasCleared != null)
        {
            if (nowOrigin == 1)
                throw new InvalidOperationException("\"" + b.Item2 + "\" still defines the origin after \""
                    + wasCleared.Item3 + "\" took it. The call failed, and Heron rolls the whole call back.");
            parts.Add("Defines Origin Yes -> No (moved to \"" + wasCleared.Item3 + "\")");
        }
        else if ((plan == null || !plan.Item4) && nowOrigin != b.Item6)
            throw new InvalidOperationException("\"" + b.Item2 + "\" reads Defines Origin " + yesNo(originOf(b.Item1))
                + " after the call, and nothing asked for that. The call failed, and Heron rolls the whole call back.");

        if (plan == null)
        {
            if (stored(row) != b.Item4)
                throw new InvalidOperationException("\"" + b.Item2 + "\" was not named, and reads Is Reference "
                    + nowWords + " after the call where it read " + b.Item5 + ". The call failed, and Heron rolls the "
                    + "whole call back.");
        }
        else
        {
            if (plan.Item3 != null)
            {
                if (squash(nowWords) != plan.Item3.Item1)
                    throw new InvalidOperationException("\"" + plan.Item2 + "\" reads Is Reference " + nowWords + " after "
                        + "the call, not " + plan.Item3.Item2 + ". The call failed, and Heron rolls the whole call back.");
                if (squash(b.Item5) != plan.Item3.Item1) parts.Add("Is Reference " + b.Item5 + " -> " + nowWords);
            }
            else if (stored(row) != b.Item4)
                throw new InvalidOperationException("\"" + plan.Item2 + "\" reads Is Reference " + nowWords + " after the "
                    + "call where it read " + b.Item5 + ", and only its origin was asked. The call failed, and Heron "
                    + "rolls the whole call back.");
            if (plan.Item4)
            {
                if (nowOrigin != 1)
                    throw new InvalidOperationException("\"" + plan.Item2 + "\" does not define the origin after the call. "
                        + "The call failed, and Heron rolls the whole call back.");
                if (b.Item6 != 1) parts.Add("Defines Origin No -> Yes");
            }
        }
        if (parts.Count > 0)
        {
            rows.Add("\"" + b.Item2 + "\": " + string.Join("; ", parts));
            changed++;
        }
    }

    planeReport = rows.Count == 0 ? "Every plane named already read as asked." : string.Join("  ||  ", rows);
    findings.Add(changed + " plane(s) changed and " + alreadySet + " setting(s) already as asked - every plane in the "
        + "family read back after the call.");
    if (numbers.Count > 0)
        findings.Add("Is Reference was written as " + string.Join(", ", numbers) + " - the numbers Revit stores for "
            + "those words on this release, each read back in its own words.");
    if (putBack.Count > 0)
        findings.Add("Revit moved " + string.Join(", ", putBack) + " while this ran, which the call did not ask "
            + "about; each was put back and read back.");
    if (cleared.Count > 0)
        findings.Add("The family's origin - the point a project places it by - now lies on "
            + string.Join(" and ", plans.Where(p => p.Item4).Select(p => "\"" + p.Item2 + "\"")) + ". Units already "
            + "placed in a project take the new origin when the family is loaded into it again, so check them then.");
    if (plans.Any(p => p.Item3 != null && p.Item3.Item1 == "notareference"))
        findings.Add("A plane that is Not a Reference cannot be dimensioned or snapped to in a project.");
}

if (refused != null) findings.Add(refused);
