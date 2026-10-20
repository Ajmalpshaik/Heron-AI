// NOT STANDALONE. Assumes `doc`, `hookName`, `angle`, `extension` and
// `hookStyle` are in scope; leaves `created`, `readBack`, `refused` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// The angle is degrees, the extension a multiple of the bar diameter (how
// Revit stores it), the style standard or stirrup/tie - all the modeller's.
// Revit's own limits are checked first: 0 < angle <= 180 degrees and
// 0 < extension <= 99 (RebarHookType.Create's remarks).

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> degrees = radians => Math.Round(radians * 180.0 / Math.PI, 2).ToString("0.##", invariant);

var created = new List<ElementId>();
var readBack = "";
var refused = "";
var findings = new List<string>();

var name = (hookName ?? "").Trim();
var angleText = (angle ?? "").Trim().TrimEnd('°').Trim();
if (angleText.EndsWith("deg", StringComparison.OrdinalIgnoreCase)) angleText = angleText.Substring(0, angleText.Length - 3).Trim();
var extensionText = (extension ?? "").Trim();
if (extensionText.EndsWith("d", StringComparison.OrdinalIgnoreCase)) extensionText = extensionText.Substring(0, extensionText.Length - 1).Trim();
var styleText = (hookStyle ?? "").Trim().ToLowerInvariant();

double angleDegrees, multiplier;
var style = RebarStyle.Standard;
if (name.Length == 0)
    refused = "No name was given for the hook type, and Heron does not choose one. Nothing was made";
else if (!double.TryParse(angleText, System.Globalization.NumberStyles.Float, invariant, out angleDegrees)
         || angleDegrees <= 0 || angleDegrees > 180)
    refused = "'" + (angle ?? "") + "' is not a hook angle Revit takes - give degrees, more than 0 and no more than 180. Nothing was made";
else if (!double.TryParse(extensionText, System.Globalization.NumberStyles.Float, invariant, out multiplier)
         || multiplier <= 0 || multiplier > 99)
    refused = "'" + (extension ?? "") + "' is not an extension Revit takes - give it as a multiple of the bar diameter, "
        + "12 for 12d, more than 0 and no more than 99. Nothing was made";
else if (styleText != "standard" && styleText != "stirrup/tie" && styleText != "stirrup" && styleText != "tie")
    refused = "'" + (hookStyle ?? "") + "' is not a hook style. Say standard, for main bars, or stirrup/tie, for links. Nothing was made";
else
{
    style = styleText == "standard" ? RebarStyle.Standard : RebarStyle.StirrupTie;
    var same = new FilteredElementCollector(doc).OfClass(typeof(RebarHookType)).Cast<RebarHookType>()
        .FirstOrDefault(h => string.Equals(h.Name, name, StringComparison.Ordinal));
    if (same != null)
        refused = "A Rebar Hook Type named '" + name + "' is already in the model, at " + degrees(same.HookAngle)
            + " degrees. Nothing was made - give another name, or use that one";
    else
    {
        RebarHookType made = null;
        try
        {
            made = RebarHookType.Create(doc, angleDegrees * Math.PI / 180.0, multiplier);
            made.Name = name;
            made.Style = style;
            doc.Regenerate();
            created.Add(made.Id);
            readBack = "'" + made.Name + "' " + degrees(made.HookAngle) + " degrees, extension "
                + made.StraightLineMultiplier.ToString("0.##", invariant) + " x bar diameter, for "
                + (made.Style == RebarStyle.StirrupTie ? "stirrup/tie" : "standard") + " bars";
            findings.Add("Made Rebar Hook Type " + readBack + ".");
            if (made.Style != style || Math.Abs(made.HookAngle - angleDegrees * Math.PI / 180.0) > 1e-6
                || Math.Abs(made.StraightLineMultiplier - multiplier) > 1e-6)
                findings.Add("Read back different from what was asked.");
        }
        catch (Exception ex)
        {
            refused = "Revit refused the hook type: " + ex.Message + ". Nothing was kept";
            if (made != null && made.IsValidObject)
            {
                try { doc.Delete(made.Id); } catch (Exception) { }
            }
            created.Clear();
            readBack = "";
        }
    }
}

if (refused.Length > 0) findings.Add(refused + ".");
