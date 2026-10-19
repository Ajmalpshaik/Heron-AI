// NOT STANDALONE. Assumes `doc`, `typeName`, `diameter`, `standardBendDiameter`,
// `hookBendDiameter`, `stirrupBendDiameter` and `modelDiameter` are in scope;
// leaves `created`, `readBack`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// EVERY VALUE IS THE MODELLER'S, IN MILLIMETRES (D-33, D-71). Nothing is
// defaulted but the model diameter, which is the nominal one when left out.
// Revit's limits are checked before anything is made: every bend diameter must
// be larger than the bar diameter (SetBarTypeDiameters, every release).

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString("0.##", invariant);
Func<RebarBarType, double> nominalOf = t =>
{
#if REVIT2020 || REVIT2021
    return t.BarDiameter;
#else
    return t.BarNominalDiameter;
#endif
};
Func<RebarBarType, double> modelOf = t =>
{
#if REVIT2020 || REVIT2021
    return t.BarDiameter;
#else
    return t.BarModelDiameter;
#endif
};

var created = new List<ElementId>();
var readBack = "";
var refused = "";
var findings = new List<string>();

// A length in millimetres, "16" or "16 mm". NaN, with `refused` set, otherwise.
Func<string, string, double> millimetres = (text, what) =>
{
    var cleaned = (text ?? "").Trim();
    if (cleaned.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) cleaned = cleaned.Substring(0, cleaned.Length - 2).Trim();
    double value;
    if (!double.TryParse(cleaned, System.Globalization.NumberStyles.Float, invariant, out value) || value <= 0)
    {
        if (refused.Length == 0)
            refused = "'" + (text ?? "") + "' is not a " + what + ". Give it in millimetres - 16 or 16 mm. Nothing was made";
        return double.NaN;
    }
    return value / 304.8;
};

var name = (typeName ?? "").Trim();
var nominal = millimetres(diameter, "bar diameter");
var standard = millimetres(standardBendDiameter, "standard bend diameter");
var hook = millimetres(hookBendDiameter, "hook bend diameter");
var stirrup = millimetres(stirrupBendDiameter, "stirrup/tie bend diameter");
var modelText = (modelDiameter ?? "").Trim();
var model = (modelText.Length == 0 || modelText.Equals("nominal", StringComparison.OrdinalIgnoreCase))
    ? nominal : millimetres(modelText, "model bar diameter");

if (refused.Length == 0 && name.Length == 0)
    refused = "No name was given for the bar type, and Heron does not choose one. Nothing was made";

var existing = new FilteredElementCollector(doc).OfClass(typeof(RebarBarType)).Cast<RebarBarType>().ToList();
if (refused.Length == 0)
{
    var same = existing.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.Ordinal));
    if (same != null)
        refused = "A Rebar Bar Type named '" + name + "' is already in the model, at " + mm(nominalOf(same))
            + " mm. Nothing was made - give another name, or use that one";
}
#if REVIT2020 || REVIT2021
if (refused.Length == 0 && Math.Abs(model - nominal) > 1e-9)
    refused = "This Revit holds one bar diameter only - a separate model diameter arrived with Revit 2022. "
        + "Leave the model diameter out. Nothing was made";
#endif
if (refused.Length == 0)
{
    var largest = Math.Max(nominal, model);
    var tooSmall = new List<string>();
    if (standard <= largest) tooSmall.Add("standard bend " + mm(standard) + " mm");
    if (hook <= largest) tooSmall.Add("hook bend " + mm(hook) + " mm");
    if (stirrup <= largest) tooSmall.Add("stirrup/tie bend " + mm(stirrup) + " mm");
    if (tooSmall.Count > 0)
        refused = "Revit needs every bend diameter larger than the bar diameter (" + mm(largest) + " mm), and "
            + string.Join(", ", tooSmall) + " is not. Nothing was made";
}

if (refused.Length == 0)
{
    RebarBarType made = null;
    try
    {
        made = RebarBarType.Create(doc);
        made.Name = name;
        var options = new BarTypeDiameterOptions(made);
#if REVIT2020 || REVIT2021
        options.BarDiameter = nominal;
#else
        options.BarNominalDiameter = nominal;
        options.BarModelDiameter = model;
#endif
        options.StandardBendDiameter = standard;
        options.StandardHookBendDiameter = hook;
        options.StirrupTieBendDiameter = stirrup;
        made.SetBarTypeDiameters(options);
        doc.Regenerate();

        created.Add(made.Id);
        readBack = "'" + made.Name + "': nominal " + mm(nominalOf(made)) + " mm, model " + mm(modelOf(made))
            + " mm, bend diameters standard " + mm(made.StandardBendDiameter) + ", hook "
            + mm(made.StandardHookBendDiameter) + ", stirrup/tie " + mm(made.StirrupTieBendDiameter) + " mm";
        var wrong = new List<string>();
        if (Math.Abs(nominalOf(made) - nominal) > 0.01 / 304.8) wrong.Add("nominal diameter");
        if (Math.Abs(modelOf(made) - model) > 0.01 / 304.8) wrong.Add("model diameter");
        if (Math.Abs(made.StandardBendDiameter - standard) > 0.01 / 304.8) wrong.Add("standard bend");
        if (Math.Abs(made.StandardHookBendDiameter - hook) > 0.01 / 304.8) wrong.Add("hook bend");
        if (Math.Abs(made.StirrupTieBendDiameter - stirrup) > 0.01 / 304.8) wrong.Add("stirrup/tie bend");
        findings.Add("Made Rebar Bar Type " + readBack + ".");
        if (wrong.Count > 0) findings.Add("Read back different from what was asked: " + string.Join(", ", wrong) + ".");
        var sameSize = existing.Where(t => Math.Abs(nominalOf(t) - nominal) < 0.05 / 304.8).Select(t => "'" + t.Name + "'").ToList();
        if (sameSize.Count > 0)
            findings.Add("The model already had a bar of " + mm(nominal) + " mm: " + string.Join(", ", sameSize)
                + ". A placing tool given the diameter alone will now ask which one.");
    }
    catch (Exception ex)
    {
        refused = "Revit refused the bar type: " + ex.Message + ". Nothing was kept";
        if (made != null && made.IsValidObject)
        {
            try { doc.Delete(made.Id); } catch (Exception) { }
        }
        created.Clear();
        readBack = "";
    }
}

if (refused.Length > 0) findings.Add(refused + ".");
