// NOT STANDALONE. Assumes `doc` and `texts` are in scope; leaves `textIds`,
// `textCount`, `placed`, `notAFamily`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one - so every
// text in the list is ONE entry in the Family Editor's undo list.
//
// 3D MODEL TEXT IN THE FAMILY BEING EDITED - the Family Editor's Model Text
// button, typed rather than clicked: a nameplate, a raised or engraved label, a
// panel's legend. Real geometry that shows in 3D and in elevation and is
// edited afterwards as text, where dot-matrix lettering built of square
// extrusions was neither (the gap this was written for, 2026-10-12).
//
// THE LIST. One row per text, each starting `text=`; a row runs until the next
// `text=`. Fields are `name=value`, a field running until the next field's
// name. A text holding `=`, or the word `text=`, is put in double quotes:
//   text="FIRE ALARM CONTROL PANEL" workPlane=Center (Front/Back) at=0,1200
//     heightMm=40 depthMm=2 align=center
// Fields: text, workPlane, at, heightMm, depthMm (all five needed); align,
// readFrom, offsetMm, material, detailLevels and views (each optional). The
// same list may be a FILE, its full path given instead, in the temporary
// folder - read exactly as CREATE_FAMILY_FORMS reads its file (5b-430).
//
// WHERE: `at` is millimetres in the plane's OWN TWO MODEL COORDINATES, the way
// the form tools write a profile - on a level or a horizontal plane X,Y as seen
// in plan; on a plane facing front or back X,Z; on a plane facing left or
// right Y,Z. Never relative to the plane and never mirrored by which way it
// faces. `offsetMm` moves the text off the plane along the model axis it faces,
// positive up (+Z), back (+Y) or right (+X) - onto a box's front face without a
// plane of its own there.
//
// WHICH WAY IT READS: `readFrom` names the side the text is read from - top or
// bottom on a horizontal plane, front or back on a front-back one, left or
// right on a left-right one - and the defaults are the Family Editor's own
// views: top, front, right. Revit lays model text along its sketch plane's own
// X direction with its own Y as up, and a reference plane's sketch plane may
// face either way, so the sketch plane made on the named plane is KEPT only when
// its axes already read from that side and the text sits on the plane itself;
// otherwise a sketch plane is made at the same place with the axes that do, and
// the answer says that text does not move with the named plane.
//
// HEIGHT IS A TYPE. Revit keeps a model text's size on its Model Text TYPE. A
// type of exactly that size is used when the family has one; otherwise the
// first type, by name, is duplicated as "<height> mm" - "40 mm", "2.5 mm" -
// keeping that type's font, and its Text Size set. A family with no Model Text
// type at all is refused: there is nothing to copy one from.
//
// ALL OR NOTHING. Every row is checked before anything is placed, each problem
// named with its row. Each text is then read back - its text, type and size,
// depth and alignment, any material or visibility asked for - and one that
// Revit refuses or that reads back wrong throws, and the host rolls the whole
// call back.
//
// WHERE THE LETTERS LAND IS MEASURED, NOT TRUSTED (version 2, row 5b-1315). On
// Revit 2020 a text on a work plane offset -2560 mm from Center (Front/Back)
// came out at Y -5120 - the offset twice - and an align=center text at X -650
// ran from -650, as if aligned left: the point NewModelText is given is not
// simply where the letters go. So each text is measured once placed - its left
// end, centre or right end along the text, its foot against the baseline, its
// back face against the plane - and one that missed by more than half a
// millimetre is placed again moved by the miss, which lands it whichever way
// Revit read the point. One that still misses by a millimetre refuses the whole
// call, naming where it was asked and where it reached.

var findings = new List<string>();
var textIds = "";
var textCount = 0;
var placed = "";
var notAFamily = false;
string refused = null;

var invariant = System.Globalization.CultureInfo.InvariantCulture;
var ignoreCase = System.Text.RegularExpressions.RegexOptions.IgnoreCase;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString(invariant);
Func<double, string> plain = value => Math.Round(value, 2).ToString(invariant);
var halfMillimetre = 0.5 / 304.8;
Func<string, string> squash = text =>
    new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
Func<string, string> shortened = text =>
{
    var flat = System.Text.RegularExpressions.Regex.Replace(text ?? "", @"\s+", " ").Trim();
    return flat.Length > 80 ? flat.Substring(0, 77) + "..." : flat;
};

// Which world axis a direction lies along: 0 X, 1 Y, 2 Z, -1 none of them.
Func<XYZ, int> axisOf = n =>
    Math.Abs(n.X) > 0.9999 ? 0 : Math.Abs(n.Y) > 0.9999 ? 1 : Math.Abs(n.Z) > 0.9999 ? 2 : -1;
Func<XYZ, int, double> along = (point, index) => index == 0 ? point.X : index == 1 ? point.Y : point.Z;
var axisLetters = new[] { "X", "Y", "Z" };
var facing = new[] { "facing left or right", "facing front or back", "horizontal" };
// The two model coordinates `at` is written in, per axis the plane faces.
var inPlane = new[] { new[] { 1, 2 }, new[] { 0, 2 }, new[] { 0, 1 } };

// THE SIDE A TEXT IS READ FROM, per axis the plane faces, and the sketch
// plane axes - along the text, then up it - that read correctly from there.
// Each pair's cross product points at the reader.
var sides = new Dictionary<string, Tuple<int, XYZ, XYZ>>
{
    { "top",    Tuple.Create(2, new XYZ(1, 0, 0),  new XYZ(0, 1, 0)) },
    { "bottom", Tuple.Create(2, new XYZ(1, 0, 0),  new XYZ(0, -1, 0)) },
    { "front",  Tuple.Create(1, new XYZ(1, 0, 0),  new XYZ(0, 0, 1)) },
    { "back",   Tuple.Create(1, new XYZ(-1, 0, 0), new XYZ(0, 0, 1)) },
    { "right",  Tuple.Create(0, new XYZ(0, 1, 0),  new XYZ(0, 0, 1)) },
    { "left",   Tuple.Create(0, new XYZ(0, -1, 0), new XYZ(0, 0, 1)) },
};
var defaultSide = new[] { "right", "front", "top" };

Func<XYZ, string> direction = d =>
{
    if (d == null) return "no direction";
    if (Math.Abs(d.Z) > 0.9999) return d.Z > 0 ? "+Z (up)" : "-Z (down)";
    if (Math.Abs(d.X) > 0.9999) return d.X > 0 ? "+X (right)" : "-X (left)";
    if (Math.Abs(d.Y) > 0.9999) return d.Y > 0 ? "+Y (back)" : "-Y (front)";
    return "(" + Math.Round(d.X, 3).ToString(invariant) + ", " + Math.Round(d.Y, 3).ToString(invariant) + ", "
        + Math.Round(d.Z, 3).ToString(invariant) + ")";
};

Func<ReferencePlane, string> ownName = rp =>
{
    var named = rp.get_Parameter(BuiltInParameter.DATUM_TEXT);
    var text = named == null ? null : named.AsString();
    return string.IsNullOrEmpty(text) ? "" : text;
};

Func<string, double?> number = text =>
{
    double value;
    var said = System.Text.RegularExpressions.Regex.Replace((text ?? "").Trim(), @"\s*mm$", "", ignoreCase);
    if (!double.TryParse(said, System.Globalization.NumberStyles.Float, invariant, out value)) return null;
    if (double.IsNaN(value) || double.IsInfinity(value)) return null;
    return value;
};

Func<string, double[]> pair = text =>
{
    var parts = (text ?? "").Split(',');
    if (parts.Length != 2) return null;
    var first = number(parts[0]);
    var second = number(parts[1]);
    return first.HasValue && second.HasValue ? new[] { first.Value, second.Value } : null;
};

// ---- a material, read as SET_FAMILY_FORM_MATERIAL reads one ------------------

var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
var revitAssembly = typeof(Document).Assembly;
var dbNamespace = typeof(Document).Namespace;

// TWO KINDS ARE THE SAME KIND WHATEVER VERSION THEIR IDS CARRY - from 2021 a
// kind is a ForgeTypeId compared by name; before that an enum.
Func<object, object, bool> sameKind = (first, second) =>
{
    if (first == null || second == null) return false;
    var nameEquals = first.GetType().GetMethod("NameEquals", new[] { second.GetType() });
    if (nameEquals != null)
    {
        try { return (bool)nameEquals.Invoke(first, new[] { second }); }
        catch (Exception) { }
    }
    return first.Equals(second);
};

// Is this family parameter a MATERIAL parameter? From 2022 its data type is
// compared with the material spec; before that its parameter type is read -
// that enum's Material member is gone from 2023.
Func<FamilyParameter, bool> isMaterial = p =>
{
    try
    {
        var definition = p.Definition;
        var getDataType = definition.GetType().GetMethod("GetDataType", System.Type.EmptyTypes);
        if (getDataType != null)
        {
            var spec = getDataType.Invoke(definition, null);
            var owner = revitAssembly.GetType(dbNamespace + ".SpecTypeId+Reference");
            var property = owner == null ? null : owner.GetProperty("Material", flags);
            var wanted = property == null ? null : property.GetValue(null);
            return sameKind(spec, wanted);
        }
        var old = definition.GetType().GetProperty("ParameterType");
        var value = old == null ? null : old.GetValue(definition);
        return value != null && value.ToString() == "Material";
    }
    catch (Exception)
    {
        return false;
    }
};

// ---- the planes and the types ----------------------------------------------

// (element, axis it faces, position along that axis in feet, name) for every
// named reference plane and every level.
var datums = new List<Tuple<Element, int, double, string>>();
var textTypes = new List<ModelTextType>();
if (doc.IsFamilyDocument)
{
    foreach (var rp in new FilteredElementCollector(doc).OfClass(typeof(ReferencePlane)).Cast<ReferencePlane>())
    {
        var name = ownName(rp);
        if (name.Length == 0) continue;
        var at = axisOf(rp.Normal);
        datums.Add(Tuple.Create((Element)rp, at, at < 0 ? 0.0 : along(rp.GetPlane().Origin, at), name));
    }
    foreach (var level in new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>())
        datums.Add(Tuple.Create((Element)level, 2, level.Elevation, level.Name));
    textTypes = new FilteredElementCollector(doc).OfClass(typeof(ModelTextType)).Cast<ModelTextType>()
        .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase).ToList();
}

// A type's Text Size in feet, or null where it has none.
Func<ModelTextType, double?> sizeOf = t =>
{
    var p = t.get_Parameter(BuiltInParameter.MODEL_TEXT_SIZE);
    return p == null || p.StorageType != StorageType.Double ? (double?)null : p.AsDouble();
};
Func<ModelTextType, string> fontOf = t =>
{
    var p = t.get_Parameter(BuiltInParameter.TEXT_FONT);
    var font = p == null ? null : p.AsString();
    return string.IsNullOrEmpty(font) ? "its font" : font;
};

// ---- the rows ---------------------------------------------------------------

var fieldNames = new[] { "text", "workPlane", "at", "heightMm", "depthMm", "align", "readFrom", "offsetMm",
    "material", "detailLevels", "views" };
var mostRows = 200;
var problems = new List<string>();

// ONE ENTRY PER ROW in each list, in the order listed - what the row asks for,
// already checked. A row with a problem adds nothing, and nothing is placed.
var rowText = new List<string>();
var rowPlane = new List<Tuple<Element, int, double, string>>();
var rowAt = new List<double[]>();
var rowHeight = new List<double>();
var rowDepth = new List<double>();
var rowAlign = new List<HorizontalAlign>();
var rowSide = new List<string>();
var rowOffset = new List<double>();
var rowParameter = new List<FamilyParameter>();
var rowMaterial = new List<Material>();
var rowByCategory = new List<bool>();
var rowShows = new List<bool[]>();

if (!doc.IsFamilyDocument)
{
    notAFamily = true;
    refused = "The document in front is a project, not a family open in the Family Editor. Model text is placed "
        + "inside the family - open it first. In a project, CREATE_TEXT_NOTE writes a 2D text note in a view.";
}
else
{
    // THE LIST, OR THE FILE THAT HOLDS IT. A value with no `text=` in it is read
    // as a file's full path, and only a file in the temporary folder is read.
    var said = (texts ?? "").Trim();
    string rowsText = null;
    if (said.Length == 0)
        problems.Add("No text was listed. Each row starts text= - text=\"PANEL\" workPlane=Center (Front/Back) "
            + "at=0,1200 heightMm=40 depthMm=2 - or give the full path of a file that holds the rows.");
    else if (System.Text.RegularExpressions.Regex.IsMatch(said, @"\btext\s*=", ignoreCase))
        rowsText = said;
    else
    {
        var path = said.Trim('"', '\'');
        var temporary = System.IO.Path.GetFullPath(System.IO.Path.GetTempPath());
        // 5b-430: Revit's temporary folder may be a GUID-named sub-folder of the
        // user's, while the chat writes to the user's own %TEMP%\heron-work.
        // That one folder of the parent is read as well - never the rest of it.
        string shared = null;
        var temporaryName = System.IO.Path.GetFileName(temporary.TrimEnd(System.IO.Path.DirectorySeparatorChar,
            System.IO.Path.AltDirectorySeparatorChar));
        Guid guidName;
        if (Guid.TryParse(temporaryName ?? "", out guidName))
        {
            var parent = System.IO.Path.GetDirectoryName(temporary.TrimEnd(System.IO.Path.DirectorySeparatorChar,
                System.IO.Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrEmpty(parent))
                shared = System.IO.Path.Combine(parent, "heron-work") + System.IO.Path.DirectorySeparatorChar;
        }
        string full = null;
        try { if (System.IO.Path.IsPathRooted(path)) full = System.IO.Path.GetFullPath(path); }
        catch (Exception) { full = null; }
        if (full == null)
            problems.Add("\"" + shortened(said) + "\" is neither a list of texts - each row starts text= - nor the "
                + "full path of a file that holds one.");
        else if (!full.StartsWith(temporary, StringComparison.OrdinalIgnoreCase)
            && (shared == null || !full.StartsWith(shared, StringComparison.OrdinalIgnoreCase)))
            problems.Add("The file " + full + " is not in the temporary folder, " + temporary
                + (shared == null ? "" : ", nor in " + shared) + ". Write the list under "
                + (shared ?? temporary + "heron-work") + " - the folder both the chat and Revit can see.");
        else if (!System.IO.File.Exists(full))
            problems.Add("There is no file at " + full + ".");
        else
        {
            try { rowsText = System.IO.File.ReadAllText(full); }
            catch (Exception ex) { problems.Add("The file " + full + " could not be read: " + ex.Message); }
        }
    }

    // QUOTED TEXT IS KEPT WHOLE. Each "..." is set aside before the rows and
    // fields are found, so a text holding `=` or the word text= is one value.
    var quoted = new List<string>();
    var pieces = new List<string>();
    if (rowsText != null)
    {
        var kept = rowsText.Replace("\r", "\n").Split('\n').Where(line => !line.TrimStart().StartsWith("#"));
        var whole = string.Join(" ", kept).Trim();
        whole = System.Text.RegularExpressions.Regex.Replace(whole, "\"([^\"]*)\"", m =>
        {
            quoted.Add(m.Groups[1].Value);
            return "\u0001" + (quoted.Count - 1).ToString(invariant) + "\u0001";
        });
        if (whole.IndexOf('"') >= 0)
            problems.Add("A double quote in the list is not closed - a quoted text needs one at each end.");
        pieces = System.Text.RegularExpressions.Regex.Split(whole, @"(?=\btext\s*=)", ignoreCase)
            .Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
        if (pieces.Count > 0 && !System.Text.RegularExpressions.Regex.IsMatch(pieces[0], @"^text\s*=", ignoreCase))
        {
            problems.Add("The list has \"" + shortened(pieces[0]) + "\" before its first row - every row starts text=.");
            pieces.RemoveAt(0);
        }
        if (pieces.Count == 0 && problems.Count == 0)
            problems.Add("The list holds no rows - each row starts text=.");
        if (pieces.Count > mostRows)
            problems.Add("The list has " + pieces.Count + " rows, and one call places at most " + mostRows
                + ". Split it into calls of up to " + mostRows + ".");
    }
    Func<string, string> unquote = value => System.Text.RegularExpressions.Regex.Replace(value ?? "",
        "\u0001(\\d+)\u0001", m => quoted[int.Parse(m.Groups[1].Value, invariant)]);

    if (rowsText != null && textTypes.Count == 0)
        problems.Add("This family holds no Model Text type, so there is none to copy one of the asked height from. "
            + "Place one model text by hand with the Family Editor's Model Text button - it loads the type - and ask "
            + "again.");

    for (var r = 0; r < pieces.Count && pieces.Count <= mostRows; r++)
    {
        var where = "Row " + (r + 1);
        var problemsBefore = problems.Count;

        // FIELDS, each running until the next field's name. A name that is not
        // a field is refused rather than read as part of the value before it.
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var marks = new List<Tuple<int, int, string>>();
        foreach (System.Text.RegularExpressions.Match m in
            System.Text.RegularExpressions.Regex.Matches(pieces[r], @"\b([A-Za-z]+)\s*="))
        {
            var known = fieldNames.FirstOrDefault(n => string.Equals(n, m.Groups[1].Value,
                StringComparison.OrdinalIgnoreCase));
            if (known == null)
                problems.Add(where + ": \"" + m.Groups[1].Value + "\" is not a field. A row takes text, workPlane, at, "
                    + "heightMm and depthMm; and if wanted align, readFrom, offsetMm, material, detailLevels and views. "
                    + "A text holding \"=\" goes in double quotes.");
            else marks.Add(Tuple.Create(m.Index, m.Index + m.Length, known));
        }
        for (var k = 0; k < marks.Count; k++)
        {
            var end = k + 1 < marks.Count ? marks[k + 1].Item1 : pieces[r].Length;
            var value = pieces[r].Substring(marks[k].Item2, end - marks[k].Item2).Trim();
            if (fields.ContainsKey(marks[k].Item3))
                problems.Add(where + " gives " + marks[k].Item3 + " twice.");
            else fields[marks[k].Item3] = unquote(value);
        }
        Func<string, string> field = name => fields.ContainsKey(name) ? fields[name] : null;

        // THE TEXT.
        var words = field("text") ?? "";
        if (words.Trim().Length == 0) problems.Add(where + " has no text.");
        else if (words.IndexOf('\n') >= 0 || words.IndexOf('\r') >= 0)
            problems.Add(where + ": a text is one line here - give each line its own row.");

        // THE PLANE.
        Tuple<Element, int, double, string> plane = null;
        var wanted = (field("workPlane") ?? "").Trim();
        var found = datums.Where(d => string.Equals(d.Item4, wanted, StringComparison.OrdinalIgnoreCase)).ToList();
        if (wanted.Length == 0) problems.Add(where + " names no workPlane.");
        else if (found.Count == 0)
            problems.Add(where + ": no reference plane or level is called \"" + wanted + "\". This family has "
                + (datums.Count == 0 ? "no named ones."
                    : string.Join(", ", datums.Select(d => d.Item4).Distinct().OrderBy(n => n).Take(20)) + "."));
        else if (found.Count > 1) problems.Add(where + ": \"" + wanted + "\" names " + found.Count + " planes - rename one first.");
        else if (found[0].Item2 < 0)
            problems.Add(where + ": \"" + wanted + "\" is at an angle. `at` is written in the model's own "
                + "coordinates, so the plane must face left-right, front-back or up.");
        else plane = found[0];

        // WHERE ON IT.
        var at = pair(field("at"));
        if (field("at") == null) problems.Add(where + " gives no `at` - the text's point in the plane's two model "
            + "coordinates, millimetres, as \"0,1200\".");
        else if (at == null) problems.Add(where + ": at \"" + field("at") + "\" is not two numbers \"U,V\" in millimetres.");

        // HEIGHT AND DEPTH.
        var height = number(field("heightMm"));
        if (!height.HasValue || height.Value <= 0)
            problems.Add(where + ": heightMm \"" + (field("heightMm") ?? "") + "\" is not a text height in millimetres "
                + "above 0.");
        var depth = number(field("depthMm"));
        if (!depth.HasValue || depth.Value <= 0)
            problems.Add(where + ": depthMm \"" + (field("depthMm") ?? "") + "\" is not a depth in millimetres above 0 - "
                + "how far the letters stand out.");

        // ALIGNMENT - Revit's own default is left.
        var align = HorizontalAlign.Left;
        var alignSaid = squash(field("align") ?? "left");
        if (alignSaid == "left") align = HorizontalAlign.Left;
        else if (alignSaid == "center" || alignSaid == "centre" || alignSaid == "middle") align = HorizontalAlign.Center;
        else if (alignSaid == "right") align = HorizontalAlign.Right;
        else problems.Add(where + ": align \"" + field("align") + "\" is not left, center or right.");

        // THE SIDE IT IS READ FROM.
        string side = null;
        if (plane != null)
        {
            var sideSaid = squash(field("readFrom") ?? defaultSide[plane.Item2]);
            if (!sides.ContainsKey(sideSaid))
                problems.Add(where + ": readFrom \"" + field("readFrom") + "\" is not top, bottom, front, back, left or right.");
            else if (sides[sideSaid].Item1 != plane.Item2)
                problems.Add(where + ": \"" + plane.Item4 + "\" is " + facing[plane.Item2] + ", so its text is read from "
                    + string.Join(" or ", sides.Where(s => s.Value.Item1 == plane.Item2).Select(s => s.Key))
                    + ", not " + sideSaid + ".");
            else side = sideSaid;
        }

        // OFF THE PLANE.
        var offset = 0.0;
        if (field("offsetMm") != null)
        {
            var given = number(field("offsetMm"));
            if (!given.HasValue) problems.Add(where + ": offsetMm \"" + field("offsetMm") + "\" is not a number of millimetres.");
            else offset = given.Value;
        }

        // A MATERIAL, IF ASKED: a family material parameter, a material in the
        // family, or "by category" - never guessed between them.
        FamilyParameter parameter = null;
        Material chosen = null;
        var byCategory = false;
        var materialSaid = (field("material") ?? "").Trim();
        if (field("material") != null)
        {
            if (materialSaid.Length == 0)
                problems.Add(where + ": material is empty - name a family material parameter, a material in the "
                    + "family, or \"by category\".");
            else if (squash(materialSaid) == "bycategory" || squash(materialSaid) == "none")
                byCategory = true;
            else
            {
                FamilyParameter asParameter = null;
                foreach (FamilyParameter p in doc.FamilyManager.Parameters)
                    if (string.Equals(p.Definition.Name, materialSaid, StringComparison.OrdinalIgnoreCase))
                    {
                        asParameter = p;
                        break;
                    }
                var asMaterial = new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>()
                    .FirstOrDefault(m => string.Equals(m.Name, materialSaid, StringComparison.OrdinalIgnoreCase));
                if (asParameter != null && asMaterial != null)
                    problems.Add(where + ": \"" + materialSaid + "\" is both a family parameter and a material in this "
                        + "family, so it does not say which is meant. Rename one of them.");
                else if (asParameter != null && !isMaterial(asParameter))
                    problems.Add(where + ": \"" + asParameter.Definition.Name + "\" is a family parameter, and not a "
                        + "MATERIAL one - Revit links a Material only to a material parameter.");
                else if (asParameter != null && asParameter.IsReporting)
                    problems.Add(where + ": \"" + asParameter.Definition.Name + "\" is a reporting parameter, and "
                        + "cannot drive a material.");
                else if (asParameter != null) parameter = asParameter;
                else if (asMaterial != null) chosen = asMaterial;
                else
                    problems.Add(where + ": \"" + shortened(materialSaid) + "\" is neither a family parameter nor a "
                        + "material in this family. Its materials include "
                        + string.Join(", ", new FilteredElementCollector(doc).OfClass(typeof(Material))
                            .Cast<Material>().Select(m => m.Name).OrderBy(n => n).Take(15)) + ". CREATE_MATERIAL makes one.");
            }
        }

        // WHERE IT SHOWS, IF ASKED: detail levels and kinds of view, as
        // SET_FAMILY_FORM_VISIBILITY reads them. One given alone leaves the
        // other at all - what new model text shows in already.
        bool[] shows = null;
        if (field("detailLevels") != null || field("views") != null)
        {
            shows = new bool[6];
            foreach (var word in (field("detailLevels") ?? "all").Split(',').Select(squash).Where(w => w.Length > 0))
            {
                if (word == "all") { shows[0] = true; shows[1] = true; shows[2] = true; }
                else if (word == "coarse") shows[0] = true;
                else if (word == "medium") shows[1] = true;
                else if (word == "fine") shows[2] = true;
                else problems.Add(where + ": \"" + word + "\" is not a detail level. Name coarse, medium and fine, or all.");
            }
            if (!shows[0] && !shows[1] && !shows[2])
                problems.Add(where + ": no detail level was named. Text shown in none would vanish from every project "
                    + "view - name coarse, medium or fine, or all.");
            foreach (var word in (field("views") ?? "all").Split(',').Select(squash).Where(w => w.Length > 0))
            {
                if (word == "all") { shows[3] = true; shows[4] = true; shows[5] = true; }
                else if (word == "plan" || word == "planrcp" || word == "rcp" || word == "top" || word == "topbottom")
                    shows[3] = true;
                else if (word == "front" || word == "back" || word == "frontback") shows[4] = true;
                else if (word == "left" || word == "right" || word == "leftright" || word == "side") shows[5] = true;
                else problems.Add(where + ": \"" + word + "\" is not a kind of view. Name plan, front and left, or all.");
            }
            if (!shows[3] && !shows[4] && !shows[5])
                problems.Add(where + ": no kind of view was named. Text shown in none would vanish from plans and "
                    + "elevations - name plan, front or left, or all.");
        }

        if (problems.Count > problemsBefore) continue;

        rowText.Add(words);
        rowPlane.Add(plane);
        rowAt.Add(at);
        rowHeight.Add(height.Value);
        rowDepth.Add(depth.Value);
        rowAlign.Add(align);
        rowSide.Add(side);
        rowOffset.Add(offset);
        rowParameter.Add(parameter);
        rowMaterial.Add(chosen);
        rowByCategory.Add(byCategory);
        rowShows.Add(shows);
    }

    if (problems.Count > 0) refused = "Nothing was placed. " + string.Join(" ", problems.Distinct());
}

// ---------------------------------------------------------------------------
// PLACE, THEN READ EVERY TEXT BACK
// ---------------------------------------------------------------------------

if (refused == null)
{
    var keptNothing = " The call failed, and Heron rolls the whole call back - none of the texts is kept.";
    var fm = doc.FamilyManager;
    var ids = new List<string>();
    var lines = new List<string>();

    // ONE TYPE PER HEIGHT, found or made once, in feet to the hundredth of a millimetre.
    var typeFor = new Dictionary<long, ModelTextType>();
    var madeTypes = new List<string>();
    Func<double, ModelTextType> typeOfHeight = heightMm =>
    {
        var key = (long)Math.Round(heightMm * 100);
        if (typeFor.ContainsKey(key)) return typeFor[key];
        var feet = heightMm / 304.8;
        var match = textTypes.FirstOrDefault(t => sizeOf(t).HasValue && Math.Abs(sizeOf(t).Value - feet) < 0.005 / 304.8);
        if (match == null)
        {
            var source = textTypes[0];
            var name = plain(heightMm) + " mm";
            var taken = new HashSet<string>(textTypes.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
            for (var n = 2; taken.Contains(name); n++) name = plain(heightMm) + " mm (" + n.ToString(invariant) + ")";
            try
            {
                match = (ModelTextType)source.Duplicate(name);
                var size = match.get_Parameter(BuiltInParameter.MODEL_TEXT_SIZE);
                if (size == null || size.IsReadOnly)
                    throw new InvalidOperationException("the new type has no Text Size that can be set");
                size.Set(feet);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit refused a Model Text type of " + plain(heightMm) + " mm, copied from \""
                    + source.Name + "\": " + ex.Message + "." + keptNothing);
            }
            textTypes.Add(match);
            madeTypes.Add("\"" + name + "\" (" + fontOf(match) + ", copied from \"" + source.Name + "\")");
        }
        typeFor[key] = match;
        return match;
    };

    // ONE SKETCH PLANE PER (plane, side, offset), kept on the named plane where
    // its axes already read from that side.
    var sketchFor = new Dictionary<string, Tuple<SketchPlane, bool>>();
    Func<int, Tuple<SketchPlane, bool>> sketchOf = r =>
    {
        var plane = rowPlane[r];
        var axis = plane.Item2;
        var want = sides[rowSide[r]];
        var key = plane.Item1.Id.ToString() + "|" + rowSide[r] + "|" + plain(rowOffset[r]);
        if (sketchFor.ContainsKey(key)) return sketchFor[key];
        SketchPlane sketch = null;
        var onNamed = false;
        try
        {
            if (Math.Abs(rowOffset[r]) < 1e-9)
            {
                var hosted = SketchPlane.Create(doc, plane.Item1.Id);
                var surface = hosted.GetPlane();
                if (axisOf(surface.Normal) == axis && Math.Abs(along(surface.Origin, axis) - plane.Item3) < halfMillimetre
                    && surface.XVec.IsAlmostEqualTo(want.Item2) && surface.YVec.IsAlmostEqualTo(want.Item3))
                {
                    sketch = hosted;
                    onNamed = true;
                }
                else doc.Delete(hosted.Id);
            }
            if (sketch == null)
            {
                var origin = new double[3];
                origin[axis] = plane.Item3 + rowOffset[r] / 304.8;
                sketch = SketchPlane.Create(doc, Plane.CreateByOriginAndBasis(new XYZ(origin[0], origin[1], origin[2]),
                    want.Item2, want.Item3));
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Revit refused a work plane on \"" + plane.Item4 + "\" for row " + (r + 1)
                + ": " + ex.Message + "." + keptNothing);
        }
        var made = Tuple.Create(sketch, onNamed);
        sketchFor[key] = made;
        return made;
    };

    for (var r = 0; r < rowText.Count; r++)
    {
        var where = "Row " + (r + 1);
        var plane = rowPlane[r];
        var axis = plane.Item2;
        var u = inPlane[axis][0];
        var v = inPlane[axis][1];
        var type = typeOfHeight(rowHeight[r]);
        var sketch = sketchOf(r);
        var xyz = new double[3];
        xyz[axis] = plane.Item3 + rowOffset[r] / 304.8;
        xyz[u] = rowAt[r][0] / 304.8;
        xyz[v] = rowAt[r][1] / 304.8;
        var position = new XYZ(xyz[0], xyz[1], xyz[2]);
        var depthFeet = rowDepth[r] / 304.8;
        var heightFeet = rowHeight[r] / 304.8;
        var surface = sketch.Item1.GetPlane();
        var runs = surface.XVec.Normalize();
        var rises = surface.YVec.Normalize();
        // Toward the side the text is read from - the way the letters stand out.
        var outward = runs.CrossProduct(rises).Normalize();
        var alignWord = rowAlign[r] == HorizontalAlign.Center ? "centre"
            : rowAlign[r] == HorizontalAlign.Right ? "right end" : "left end";

        Func<XYZ, ModelText> placeAt = point =>
        {
            ModelText text;
            try
            {
                text = doc.FamilyCreate.NewModelText(rowText[r], type, sketch.Item1, point, rowAlign[r], depthFeet);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Revit refused " + where + "'s text \"" + shortened(rowText[r]) + "\" on \""
                    + plane.Item4 + "\": " + ex.Message + "." + keptNothing);
            }
            if (text == null)
                throw new InvalidOperationException("Revit made nothing for " + where + "'s text \"" + shortened(rowText[r])
                    + "\"." + keptNothing);
            doc.Regenerate();
            return text;
        };

        // WHERE THE LETTERS LANDED AGAINST WHERE THEY WERE ASKED (row 5b-1315), in feet
        // from the asked point: how far the alignment point is along the text, the
        // letters' foot up it and their back face out from the plane - each 0 when it
        // landed - then the letters' extent along, up and out. Null with no extent.
        // The foot is let off a band round the baseline - a descender hangs below it,
        // and the box is the letters', not the line's.
        Func<ModelText, double[]> landing = text =>
        {
            var box = text.get_BoundingBox(null);
            if (box == null) return null;
            var low = new[] { double.MaxValue, double.MaxValue, double.MaxValue };
            var high = new[] { double.MinValue, double.MinValue, double.MinValue };
            foreach (var cx in new[] { box.Min.X, box.Max.X })
                foreach (var cy in new[] { box.Min.Y, box.Max.Y })
                    foreach (var cz in new[] { box.Min.Z, box.Max.Z })
                    {
                        var gap = new XYZ(cx, cy, cz) - position;
                        var k = new[] { gap.DotProduct(runs), gap.DotProduct(rises), gap.DotProduct(outward) };
                        for (var i = 0; i < 3; i++)
                        {
                            low[i] = Math.Min(low[i], k[i]);
                            high[i] = Math.Max(high[i], k[i]);
                        }
                    }
            var offAlong = rowAlign[r] == HorizontalAlign.Center ? (low[0] + high[0]) / 2
                : rowAlign[r] == HorizontalAlign.Right ? high[0] : low[0];
            var offUp = low[1] < -0.4 * heightFeet || low[1] > 0.25 * heightFeet ? low[1] : 0.0;
            // The letters' face nearer the plane is the one that sits on it, whichever
            // way they stand out.
            var offOut = Math.Abs(low[2]) <= Math.Abs(high[2]) ? low[2] : high[2];
            return new[] { offAlong, offUp, offOut, low[0], high[0], low[1], high[1], low[2], high[2] };
        };
        Func<double[], double, bool> lands = (off, within) =>
            off != null && Math.Abs(off[0]) <= within && Math.Abs(off[1]) <= within && Math.Abs(off[2]) <= within;
        Func<double[], string> offWords = off => mm(off[0]) + " mm along the text, " + mm(off[1]) + " mm up and "
            + mm(off[2]) + " mm off the plane";
        Func<ModelText, string> reachOf = text =>
        {
            var box = text.get_BoundingBox(null);
            return box == null ? "nowhere - no extent" : string.Join(", ", new[] { 0, 1, 2 }.Select(i =>
                axisLetters[i] + " " + mm(along(box.Min, i)) + " to " + mm(along(box.Max, i)))) + " mm";
        };

        var made = placeAt(position);
        var landed = landing(made);
        if (landed == null)
            throw new InvalidOperationException(where + "'s text has no extent in the model - Revit made it with no "
                + "geometry." + keptNothing);

        // OFF WHERE ASKED: PLACED AGAIN ONCE, BY WHAT IT MISSED. Revit 2020 took the
        // point as more than a point on the sketch plane - an offset plane's letters
        // went to twice the offset, and an align=center text started at the point.
        // Moving the point by the miss lands it whichever reading Revit made; a text
        // that still misses refuses the whole call.
        string placedAgain = null;
        if (!lands(landed, halfMillimetre))
        {
            var missed = offWords(landed);
            var firstReach = reachOf(made);
            var corrected = position - runs.Multiply(landed[0]) - rises.Multiply(landed[1]) - outward.Multiply(landed[2]);
            try { doc.Delete(made.Id); }
            catch (Exception ex)
            {
                throw new InvalidOperationException(where + "'s letters landed " + missed + " from where they were asked, and "
                    + "Revit would not take them away to place them again: " + ex.Message + "." + keptNothing);
            }
            if (!sketch.Item1.IsValidObject)
                throw new InvalidOperationException(where + "'s letters landed " + missed + " from where they were asked, and "
                    + "their work plane went with them when they were taken away." + keptNothing);
            made = placeAt(corrected);
            landed = landing(made);
            placedAgain = where + ": Revit put the letters " + missed + " from where they were asked (they reached "
                + firstReach + "); placed again by that much";
        }
        if (!lands(landed, 1.0 / 304.8))
            throw new InvalidOperationException(where + "'s letters do not land where they were asked - their " + alignWord
                + " at " + axisLetters[u] + " " + plain(rowAt[r][0]) + ", " + axisLetters[v] + " " + plain(rowAt[r][1])
                + " mm on " + axisLetters[axis] + " " + mm(xyz[axis]) + " mm: they reach " + reachOf(made)
                + (landed == null ? "" : ", missing by " + offWords(landed)) + (placedAgain == null ? "" : ", even placed again")
                + "." + keptNothing);
        if (placedAgain != null)
            findings.Add(placedAgain + ", they land where asked.");
        if (landed[8] <= halfMillimetre)
            findings.Add(where + ": the letters stand back into the plane, away from the " + rowSide[r]
                + " they read from - look at it in the Family Editor.");

        // ITS MATERIAL, set and read back.
        var materialWords = "";
        if (rowParameter[r] != null || rowMaterial[r] != null || rowByCategory[r])
        {
            var materialField = made.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
            if (materialField == null)
                throw new InvalidOperationException(where + "'s model text has no Material field." + keptNothing);
            var expected = rowParameter[r] != null ? rowParameter[r].Definition.Name
                : rowByCategory[r] ? "<By Category>" : rowMaterial[r].Name;
            try
            {
                if (rowParameter[r] != null)
                {
                    if (!fm.CanElementParameterBeAssociated(materialField))
                        throw new InvalidOperationException("Revit says its Material cannot be linked");
                    fm.AssociateElementParameterToFamilyParameter(materialField, rowParameter[r]);
                }
                else materialField.Set(rowByCategory[r] ? ElementId.InvalidElementId : rowMaterial[r].Id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(where + ": Revit would not give the text the material " + expected
                    + ": " + ex.Message + "." + keptNothing);
            }
            var link = fm.GetAssociatedFamilyParameter(materialField);
            var nowId = materialField.AsElementId();
            var nowMaterial = nowId == null || nowId == ElementId.InvalidElementId ? null : doc.GetElement(nowId) as Material;
            if (rowParameter[r] != null && (link == null || link.Definition.Name != rowParameter[r].Definition.Name))
                throw new InvalidOperationException(where + "'s Material is not linked to \"" + expected + "\"." + keptNothing);
            if (rowMaterial[r] != null && (nowMaterial == null || nowMaterial.Id != rowMaterial[r].Id))
                throw new InvalidOperationException(where + "'s Material reads " + (nowMaterial == null ? "<By Category>"
                    : "\"" + nowMaterial.Name + "\"") + ", not \"" + expected + "\"." + keptNothing);
            if (rowByCategory[r] && nowMaterial != null)
                throw new InvalidOperationException(where + "'s Material reads \"" + nowMaterial.Name + "\", not <By Category>."
                    + keptNothing);
            materialWords = rowParameter[r] != null ? ", material <- " + expected : ", material " + expected;
        }

        // WHERE IT SHOWS, set and read back.
        var showsWords = "";
        if (rowShows[r] != null)
        {
            var s = rowShows[r];
            try
            {
                var setting = new FamilyElementVisibility(FamilyElementVisibilityType.Model);
                setting.IsShownInCoarse = s[0];
                setting.IsShownInMedium = s[1];
                setting.IsShownInFine = s[2];
                setting.IsShownInTopBottom = s[3];
                setting.IsShownInFrontBack = s[4];
                setting.IsShownInLeftRight = s[5];
                made.SetVisibility(setting);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(where + ": Revit would not set where the text shows: " + ex.Message + "."
                    + keptNothing);
            }
            var now = made.GetVisibility();
            if (now == null || now.IsShownInCoarse != s[0] || now.IsShownInMedium != s[1] || now.IsShownInFine != s[2]
                || now.IsShownInTopBottom != s[3] || now.IsShownInFrontBack != s[4] || now.IsShownInLeftRight != s[5])
                throw new InvalidOperationException(where + "'s visibility does not read back as asked." + keptNothing);
            var levelWords = new[] { "coarse", "medium", "fine" }.Where((w, i) => s[i]).ToList();
            var viewWords = new[] { "plan", "front", "left" }.Where((w, i) => s[3 + i]).ToList();
            showsWords = ", shown at " + string.Join(", ", levelWords) + " in " + string.Join(", ", viewWords);
        }

        doc.Regenerate();

        // READ BACK - what the API holds, then where the letters actually are.
        if (made.Text != rowText[r])
            throw new InvalidOperationException(where + " reads \"" + shortened(made.Text) + "\", not \"" + shortened(rowText[r])
                + "\"." + keptNothing);
        if (made.ModelTextType == null || made.ModelTextType.Id != type.Id)
            throw new InvalidOperationException(where + "'s type is not \"" + type.Name + "\"." + keptNothing);
        var size = sizeOf(made.ModelTextType);
        if (!size.HasValue || Math.Abs(size.Value - rowHeight[r] / 304.8) > 0.01 / 304.8)
            throw new InvalidOperationException(where + "'s type \"" + type.Name + "\" reads "
                + (size.HasValue ? mm(size.Value) + " mm" : "no size") + ", not " + plain(rowHeight[r]) + " mm." + keptNothing);
        if (Math.Abs(made.Depth - depthFeet) > 0.01 / 304.8)
            throw new InvalidOperationException(where + "'s depth reads " + mm(made.Depth) + " mm, not "
                + plain(rowDepth[r]) + " mm." + keptNothing);
        if (made.HorizontalAlignment != rowAlign[r])
            throw new InvalidOperationException(where + "'s alignment reads " + made.HorizontalAlignment + ", not "
                + rowAlign[r] + "." + keptNothing);
        var bounds = made.get_BoundingBox(null);
        if (bounds == null)
            throw new InvalidOperationException(where + "'s text has no extent in the model - Revit made it with no "
                + "geometry." + keptNothing);

        var thickness = along(bounds.Max, axis) - along(bounds.Min, axis);
        if (Math.Abs(thickness - depthFeet) > halfMillimetre)
            findings.Add(where + ": the letters measure " + mm(thickness) + " mm through the plane where " + plain(rowDepth[r])
                + " mm deep was asked - look at it in the Family Editor.");

        ids.Add(made.UniqueId);
        lines.Add("\"" + shortened(rowText[r]) + "\" (id " + made.Id.ToString() + ") on \"" + plane.Item4 + "\" ("
            + facing[axis] + ", " + axisLetters[axis] + " " + mm(plane.Item3) + " mm"
            + (Math.Abs(rowOffset[r]) > 1e-9 ? ", offset " + plain(rowOffset[r]) + " mm" : "") + "), at "
            + axisLetters[u] + " " + plain(rowAt[r][0]) + ", " + axisLetters[v] + " " + plain(rowAt[r][1]) + " mm, type \""
            + type.Name + "\" " + mm(size.Value) + " mm, " + mm(made.Depth) + " mm deep, aligned "
            + made.HorizontalAlignment.ToString().ToLowerInvariant() + ", read from the " + rowSide[r]
            + " - running " + direction(surface.XVec) + ", up " + direction(surface.YVec)
            + (sketch.Item2 ? ", sketched on the plane itself" : ", on a work plane of its own that does NOT move with \""
                + plane.Item4 + "\"")
            + materialWords + showsWords + ". Its letters reach " + string.Join(", ", new[] { 0, 1, 2 }.Select(i =>
                axisLetters[i] + " " + mm(along(bounds.Min, i)) + " to " + mm(along(bounds.Max, i)))) + " mm - its "
            + alignWord + " where asked, measured" + (placedAgain == null ? "" : ", placed a second time after Revit's "
                + "first placing missed"));
    }

    textIds = string.Join(",", ids);
    textCount = ids.Count;
    placed = string.Join("  ||  ", lines) + ".";
    findings.Add("Placed " + textCount + " model text(s) - one entry in the undo list: " + placed);
    if (madeTypes.Count > 0)
        findings.Add("New Model Text type(s): " + string.Join("; ", madeTypes) + ".");
    if (sketchFor.Values.Any(s => !s.Item2))
        findings.Add("Text on a work plane of its own stays where it was placed when the named plane moves. For text "
            + "that must follow a plane, give a plane whose own axes read from that side and no offsetMm - or make a "
            + "reference plane where the text goes (CREATE_REFERENCE_PLANES).");
}

if (refused != null) findings.Add(refused);
