// NOT STANDALONE. Assumes `doc`, `numbering` and `arcLengthMm` are in scope,
// and leaves `changed`, `alreadyThat`, `findings` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One undo puts both back.
//
// THE PROJECT-WIDE HALF OF SHEET ISSUES/REVISIONS: the Numbering switch (Per
// Project / Per Sheet) and the revision cloud Arc length. Empty leaves a
// setting alone.
//
// THE NUMBERING SWITCH RENUMBERS WHAT IS ALREADY PRINTED. Per Project, a sheet
// prints each revision's project-wide number; Per Sheet, it numbers the
// revisions it carries 1, 2, 3. Revit's API documentation says the change
// reaches issued revisions too. An issued revision's number is on drawings
// that have gone out, so the switch is refused while any ISSUED revision shows
// on a sheet, naming each one and its sheets. An issued revision on no sheet
// prints nowhere and does not stop it.
//
// THE ARC LENGTH IS PAPER SPACE, IN MILLIMETRES HERE. Revit holds it in
// internal feet and is asked first whether the value is acceptable. It then
// rounds what it stores, and NOT to the millimetre: measured 2026-09-28 on
// "heron ai bulding" (Revit 2024), RoundRevisionCloudSpacing turned 20 mm into
// 20.042 mm and 12.5 mm into 12.502 mm - a 1/128 inch grid - while the model
// already held exactly 20 mm, typed in the dialog. Comparing against the
// rounded value would rewrite a setting that is already right. So the value
// ASKED is what is compared, within a tenth of a millimetre on paper - wider
// than that grid's half step, far below anything a print shows, never a tight
// equality (5b-237) - and the value Revit stores is what is reported.
//
// COMPARED FIRST, READ BACK AFTER. A setting already at the value asked is
// named in `alreadyThat` and not written. A write that does not read back after
// the other has gone in throws, and the add-in rolls both back; with nothing
// written yet it is a refusal with Revit's own words and what it would have
// done, which is what makes a run with no transaction open a measurement.

var changed = "";
var alreadyThat = "";
var findings = "";
var refused = "";

{
    var invariant = System.Globalization.CultureInfo.InvariantCulture;
    Func<Exception, string> revitSaid = failure => (failure.InnerException ?? failure).Message.TrimEnd('.');
    Func<double, string> mm = feet => (feet * 304.8).ToString("0.###", invariant) + " mm";
    Func<RevisionNumbering, string> modeName = value =>
        value == RevisionNumbering.PerSheet ? "Per Sheet" : "Per Project";

    var settings = RevisionSettings.GetRevisionSettings(doc);
    var problems = new List<string>();

    // ---- numbering --------------------------------------------------------
    RevisionNumbering? wantedMode = null;
    var modeWord = (numbering ?? "").Trim().ToLowerInvariant().Replace(" ", "").Replace("-", "");
    if (modeWord.Length > 0)
    {
        if (modeWord == "perproject" || modeWord == "project") wantedMode = RevisionNumbering.PerProject;
        else if (modeWord == "persheet" || modeWord == "sheet") wantedMode = RevisionNumbering.PerSheet;
        else problems.Add("numbering takes 'per project' or 'per sheet', not '" + numbering + "'");
    }

    // ---- arc length -------------------------------------------------------
    double? wantedFeet = null;
    var arcText = (arcLengthMm ?? "").Trim();
    if (arcText.Length > 0)
    {
        double given;
        if (!double.TryParse(arcText, System.Globalization.NumberStyles.Float, invariant, out given))
            problems.Add("arcLengthMm takes a number of millimetres on paper - 20 or 12.5, not '"
                       + arcLengthMm + "'");
        else if (given <= 0)
            problems.Add("an arc length must be more than 0 mm - Revit refuses zero");
        else
        {
            var raw = given / 304.8;
            if (!settings.IsAcceptableRevisionCloudSpacing(raw))
                problems.Add("Revit does not accept an arc length of " + arcText + " mm - "
                           + "IsAcceptableRevisionCloudSpacing said no");
            else
                wantedFeet = raw;
        }
    }

    if (wantedMode == null && wantedFeet == null && problems.Count == 0)
        problems.Add("nothing was asked for - numbering and arcLengthMm were both empty");

    var currentMode = settings.RevisionNumbering;
    var currentFeet = settings.RevisionCloudSpacing;

    // ---- the issued revisions a numbering switch would renumber ----------
    if (problems.Count == 0 && wantedMode != null && wantedMode.Value != currentMode)
    {
        var shownOn = new Dictionary<ElementId, List<string>>();
        foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(ViewSheet)))
        {
            var sheet = element as ViewSheet;
            if (sheet == null || sheet.IsTemplate) continue;
            var ids = sheet.GetAllRevisionIds();
            if (ids == null) continue;
            foreach (var id in ids)
            {
                if (!shownOn.ContainsKey(id)) shownOn[id] = new List<string>();
                shownOn[id].Add(sheet.SheetNumber);
            }
        }
        var issuedOnSheets = new List<string>();
        foreach (var id in Revision.GetAllRevisionIds(doc))
        {
            var revision = doc.GetElement(id) as Revision;
            if (revision == null || !revision.Issued || !shownOn.ContainsKey(id)) continue;
            issuedOnSheets.Add("Seq. " + revision.SequenceNumber + " '" + revision.Description
                             + "' on " + string.Join(", ", shownOn[id].ToArray()));
        }
        if (issuedOnSheets.Count > 0)
            problems.Add("switching " + modeName(currentMode) + " to " + modeName(wantedMode.Value)
                       + " would renumber ISSUED revisions on sheets already sent out: "
                       + string.Join("; ", issuedOnSheets.ToArray()));
    }

    if (problems.Count > 0)
        refused = string.Join("; ", problems.ToArray()) + ". Nothing was changed.";
    else
    {
        var plan = new List<string>();
        var already = new List<string>();
        var done = new List<string>();
        var missed = new List<string>();

        if (wantedMode != null)
        {
            if (wantedMode.Value == currentMode) already.Add("numbering " + modeName(currentMode));
            else plan.Add("numbering " + modeName(currentMode) + " -> " + modeName(wantedMode.Value));
        }
        if (wantedFeet != null)
        {
            if (Math.Abs(wantedFeet.Value - currentFeet) * 304.8 < 0.1) already.Add("arc length " + mm(currentFeet));
            else plan.Add("arc length " + mm(currentFeet) + " -> " + mm(wantedFeet.Value));
        }

        if (wantedMode != null && wantedMode.Value != currentMode)
        {
            try
            {
                settings.RevisionNumbering = wantedMode.Value;
                var now = settings.RevisionNumbering;
                if (now == wantedMode.Value) done.Add("numbering " + modeName(currentMode) + " -> " + modeName(now));
                else missed.Add("numbering reads back " + modeName(now));
            }
            catch (Exception failure) { missed.Add("numbering (" + revitSaid(failure) + ")"); }
        }

        if (missed.Count == 0 && wantedFeet != null && Math.Abs(wantedFeet.Value - currentFeet) * 304.8 >= 0.1)
        {
            try
            {
                settings.RevisionCloudSpacing = wantedFeet.Value;
                var now = settings.RevisionCloudSpacing;
                if (Math.Abs(now - wantedFeet.Value) * 304.8 < 0.1)
                    done.Add("arc length " + mm(currentFeet) + " -> " + mm(now));
                else missed.Add("arc length reads back " + mm(now) + ", not " + mm(wantedFeet.Value));
            }
            catch (Exception failure) { missed.Add("arc length (" + revitSaid(failure) + ")"); }
        }

        if (missed.Count > 0 && done.Count > 0)
            throw new InvalidOperationException(
                "SET_REVISION_SETTINGS would have been half done, so nothing is kept: "
                + string.Join("; ", done.ToArray()) + " went in, then " + string.Join("; ", missed.ToArray()) + ".");
        if (missed.Count > 0)
            refused = "Revit would not change the revision settings: " + string.Join("; ", missed.ToArray())
                    + ". Nothing was changed. It would have: " + string.Join("; ", plan.ToArray()) + ".";

        changed = string.Join("; ", done.ToArray());
        alreadyThat = string.Join("; ", already.ToArray());
    }

    findings = "Read back: numbering " + modeName(settings.RevisionNumbering)
             + ", arc length " + mm(settings.RevisionCloudSpacing) + " on paper ("
             + settings.RevisionCloudSpacing.ToString("0.######", invariant) + " ft)";
}
