// NOT STANDALONE. Assumes `doc`, `coverName` and `distance` are in scope;
// leaves `created`, `readBack`, `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A cover in Revit is a TYPE a host's cover parameter points at. The name and
// the distance are both the modeller's; the distance arrives in millimetres.

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString("0.##", invariant);

var created = new List<ElementId>();
var readBack = "";
var refused = "";
var findings = new List<string>();

var name = (coverName ?? "").Trim();
var text = (distance ?? "").Trim();
if (text.EndsWith("mm", StringComparison.OrdinalIgnoreCase)) text = text.Substring(0, text.Length - 2).Trim();
double asked;
var parsed = double.TryParse(text, System.Globalization.NumberStyles.Float, invariant, out asked);

var covers = new FilteredElementCollector(doc).OfClass(typeof(RebarCoverType)).Cast<RebarCoverType>().ToList();
if (name.Length == 0)
    refused = "No name was given for the cover type, and Heron does not choose one. Nothing was made";
else if (!parsed || asked < 0)
    refused = "'" + (distance ?? "") + "' is not a cover distance. Give it in millimetres - 40 or 40 mm. Nothing was made";
else
{
    var same = covers.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal));
    if (same != null)
        refused = "A Rebar Cover Type named '" + name + "' is already in the model, at " + mm(same.CoverDistance)
            + " mm. Nothing was made - give another name, or use that one";
}

if (refused.Length == 0)
{
    try
    {
        var feet = asked / 304.8;
        var made = RebarCoverType.Create(doc, name, feet);
        doc.Regenerate();
        created.Add(made.Id);
        readBack = "'" + made.Name + "' " + mm(made.CoverDistance) + " mm";
        findings.Add("Made Rebar Cover Type " + readBack + ".");
        if (Math.Abs(made.CoverDistance - feet) > 0.01 / 304.8)
            findings.Add("Read back " + mm(made.CoverDistance) + " mm where " + mm(feet) + " mm was asked.");
        var sameDistance = covers.Where(c => Math.Abs(c.CoverDistance - feet) < 0.05 / 304.8).Select(c => "'" + c.Name + "'").ToList();
        if (sameDistance.Count > 0)
            findings.Add("The model already had " + mm(feet) + " mm under " + string.Join(", ", sameDistance)
                + ". SET_REBAR_COVER given the distance alone will now ask which one.");
    }
    catch (Exception ex)
    {
        refused = "Revit refused the cover type: " + ex.Message + ". Nothing was kept";
        created.Clear();
        readBack = "";
    }
}

if (refused.Length > 0) findings.Add(refused + ".");
