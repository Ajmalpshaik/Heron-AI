// NOT STANDALONE. Assumes `doc` is in scope; leaves `elements`, `findings`,
// `onNoSheets`, `revisionTable`, `numbering`, `numberingSequences` and
// `arcLength` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE SHEETS ARE COLLECTED ONCE AND ASKED PER REVISION, not the other way
// round. A project with 40 revisions and 300 sheets would otherwise mean 40
// collector passes over the whole model, and the cost of this check is what
// decides whether anybody runs it before an issue.
//
// GetAllRevisionIds ON A SHEET RETURNS BOTH WAYS A REVISION GETS THERE - a
// cloud drawn on the sheet or on a view placed on it, AND a revision ticked on
// the sheet by hand with no cloud anywhere. Both print. A sheet showing a
// revision is issued under it however it got there, so both are counted, and
// separating them would answer a question nobody asked while getting the
// printed set wrong.
//
// A REVISION ON NO SHEETS IS THE ONE WORTH FINDING. It is either not yet
// clouded, or clouded on a view that is on no sheet - and the second is
// invisible in Revit until somebody notices the drawing went out unmarked.
//
// THE REST IS THE SHEET ISSUES/REVISIONS DIALOG, READ ROW BY ROW. Every column
// of its table - sequence, numbering, date, description, issued, issued to,
// issued by, show - plus the number each revision actually PRINTS, the Per
// Project / Per Sheet switch, every numbering sequence with its settings, and
// the cloud arc length. Each is one STRING, because a list reaches the reply
// cut to three entries of sixty characters and this list IS the answer.
//
// THE NUMBERING API IS NOT THE SAME ON EVERY RELEASE, AND THE COMPILER CANNOT
// BE ASKED WHICH ONE THIS IS. Revit 2022 brought named numbering sequences
// (`RevisionNumberingSequence`, `Revision.RevisionNumberingSequenceId`,
// `NumericRevisionSettings.MinimumDigits`); 2020 and 2021 have one numeric and
// one alphanumeric scheme per project, read off `RevisionSettings`, and each
// revision says which with `Revision.NumberType` - both REMOVED in 2023. The
// host compiles this with no release symbol defined, so a `#if` would take
// the same branch on every release. Every member that exists on only part of
// the span is therefore reached BY NAME, and the one this Revit has is used.

var elements = new List<Element>();
var findings = new List<string>();
var onNoSheets = new List<ElementId>();
var revisionTable = "";
var numbering = "";
var numberingSequences = "";
var arcLength = "";

{
    var invariant = System.Globalization.CultureInfo.InvariantCulture;

    // Every sheet, once.
    var sheets = new List<ViewSheet>();
    foreach (var sheet in new FilteredElementCollector(doc)
                              .OfClass(typeof(ViewSheet))
                              .Cast<ViewSheet>())
    {
        if (sheet != null && !sheet.IsTemplate) sheets.Add(sheet);
    }

    // Which revisions each sheet shows, worked out once per sheet rather than once
    // per revision per sheet.
    var shownOn = new Dictionary<ElementId, List<ViewSheet>>();

    foreach (var sheet in sheets)
    {
        var shown = sheet.GetAllRevisionIds();
        if (shown == null) continue;

        foreach (var revisionId in shown)
        {
            if (revisionId == null || revisionId == ElementId.InvalidElementId) continue;
            if (!shownOn.ContainsKey(revisionId)) shownOn[revisionId] = new List<ViewSheet>();
            shownOn[revisionId].Add(sheet);
        }
    }

    // ---- the project-wide settings: the bottom of the dialog -----------------

    var revisionSettings = RevisionSettings.GetRevisionSettings(doc);

    var perSheet = false;
    try
    {
        perSheet = revisionSettings.RevisionNumbering == RevisionNumbering.PerSheet;
        numbering = perSheet
            ? "Per Sheet - each sheet numbers its own revisions 1, 2, 3 in sequence order"
            : "Per Project - every sheet prints a revision's project-wide number";
    }
    catch (Exception failure)
    {
        numbering = "could not be read: " + failure.Message;
    }

    // Paper space, internal feet. Millimetres for the report only (D-20), so no
    // units API is called and the 2021 units move cannot reach this.
    try
    {
        var spacingFeet = revisionSettings.RevisionCloudSpacing;
        arcLength = (spacingFeet * 304.8).ToString("0.###", invariant) + " mm on paper ("
                  + spacingFeet.ToString("0.######", invariant) + " ft, Revit's internal value)";
    }
    catch (Exception failure)
    {
        arcLength = "could not be read: " + failure.Message;
    }

    // ---- which numbering each revision uses, whichever API this release has ---

    var revisionClass = typeof(Revision);
    var sequenceIdProperty = revisionClass.GetProperty("RevisionNumberingSequenceId");   // 2022 on
    var numberTypeProperty = revisionClass.GetProperty("NumberType");                    // 2020 to 2022
    var sequenceClass = revisionClass.Assembly.GetType(
        revisionClass.Namespace + ".RevisionNumberingSequence");                         // 2022 on
    var minimumDigitsProperty = typeof(NumericRevisionSettings).GetProperty("MinimumDigits"); // 2022 on

    Func<Revision, string> numberingOf = revision =>
    {
        try
        {
            if (sequenceIdProperty != null)
            {
                var id = sequenceIdProperty.GetValue(revision, null) as ElementId;
                if (id == null || id == ElementId.InvalidElementId) return "None";
                var sequence = doc.GetElement(id);
                return sequence == null ? "None" : sequence.Name;
            }
            if (numberTypeProperty != null)
            {
                var kind = numberTypeProperty.GetValue(revision, null);
                return kind == null ? "" : kind.ToString();
            }
        }
        catch (Exception failure)
        {
            return "unreadable (" + failure.Message + ")";
        }
        return "not exposed by this release";
    };

    Func<string, string> quoted = text => string.IsNullOrEmpty(text) ? "(blank)" : "'" + text + "'";

    Func<NumericRevisionSettings, string> describeNumeric = settings =>
    {
        if (settings == null) return "numeric, settings unreadable";
        var digits = "";
        if (minimumDigitsProperty != null)
        {
            try { digits = ", minimum digits " + minimumDigitsProperty.GetValue(settings, null); }
            catch (Exception failure) { digits = ", minimum digits unreadable (" + failure.Message + ")"; }
        }
        return "numeric" + digits + ", starting number " + settings.StartNumber
             + ", prefix " + quoted(settings.Prefix) + ", suffix " + quoted(settings.Suffix);
    };

    Func<AlphanumericRevisionSettings, string> describeAlphanumeric = settings =>
    {
        if (settings == null) return "alphanumeric, settings unreadable";
        var values = settings.GetSequence();
        var list = values == null ? new List<string>() : new List<string>(values);
        return "alphanumeric, custom sequence " + string.Join(", ", list.ToArray())
             + " (" + list.Count + " values), prefix " + quoted(settings.Prefix)
             + ", suffix " + quoted(settings.Suffix);
    };

    // ---- the table: one row per revision, in sequence --------------------------

    var rows = new List<string>();
    var usedBy = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

    // In sequence, because that is the order the revision schedule prints in and
    // the order somebody reads a revision history.
    foreach (var revisionId in Revision.GetAllRevisionIds(doc))
    {
        var revision = doc.GetElement(revisionId) as Revision;
        if (revision == null) continue;

        elements.Add(revision);

        var carrying = shownOn.ContainsKey(revisionId) ? shownOn[revisionId] : new List<ViewSheet>();

        // Sorted by sheet number so the list reads as a drawing register. Numbers
        // are compared as text because sheet numbers are not numbers - "M-101"
        // and "A2.03" are both normal.
        carrying.Sort(delegate (ViewSheet a, ViewSheet b)
        {
            return string.Compare(a.SheetNumber ?? "", b.SheetNumber ?? "",
                                  StringComparison.OrdinalIgnoreCase);
        });

        var scheme = numberingOf(revision);
        if (!usedBy.ContainsKey(scheme)) usedBy[scheme] = new List<string>();
        usedBy[scheme].Add("Seq. " + revision.SequenceNumber);

        // WHAT IT PRINTS. Per Project, Revit has one number for the revision; Per
        // Sheet it has one per sheet, and asking for the project number throws -
        // so each sheet is asked for its own.
        string prints;
        if (carrying.Count == 0)
        {
            string projectNumber = null;
            if (!perSheet)
            {
                try { projectNumber = revision.RevisionNumber; } catch { projectNumber = null; }
            }
            prints = projectNumber == null
                ? "prints nowhere - on no sheet"
                : "would print " + quoted(projectNumber) + ", but it is on no sheet";
        }
        else
        {
            var perSheetNumbers = new List<string>();
            foreach (var sheet in carrying)
            {
                string onSheet;
                try { onSheet = sheet.GetRevisionNumberOnSheet(revisionId); }
                catch (Exception failure) { onSheet = "unreadable (" + failure.Message + ")"; }
                perSheetNumbers.Add(sheet.SheetNumber + " prints " + quoted(onSheet));
            }
            prints = string.Join(", ", perSheetNumbers.ToArray());
        }

        string show;
        switch (revision.Visibility)
        {
            case RevisionVisibility.CloudAndTagVisible: show = "Cloud and Tag"; break;
            case RevisionVisibility.TagVisible: show = "Tag"; break;
            case RevisionVisibility.Hidden: show = "None"; break;
            default: show = revision.Visibility.ToString(); break;
        }

        rows.Add("Seq. " + revision.SequenceNumber
               + ": numbering " + scheme
               + ", date " + quoted(revision.RevisionDate)
               + ", description " + quoted(revision.Description)
               + ", " + (revision.Issued ? "ISSUED" : "not issued")
               + ", issued to " + quoted(revision.IssuedTo)
               + ", issued by " + quoted(revision.IssuedBy)
               + ", show " + show
               + "; " + prints
               + " (id " + revision.Id + ")");

        if (carrying.Count == 0)
        {
            onNoSheets.Add(revisionId);
            findings.Add(string.Format(
                "{0}  {1}  - ON NO SHEETS. Either not clouded yet, or clouded on a view "
                + "that is on no sheet",
                revision.RevisionDate, revision.Description));
            continue;
        }

        var numbers = new List<string>();
        foreach (var sheet in carrying) numbers.Add(sheet.SheetNumber);

        findings.Add(string.Format("{0}  {1}  - {2} sheet{3}: {4}",
                                   revision.RevisionDate,
                                   revision.Description,
                                   carrying.Count,
                                   carrying.Count == 1 ? "" : "s",
                                   string.Join(", ", numbers.ToArray())));
    }

    revisionTable = string.Join(" || ", rows.ToArray());

    // ---- the numbering sequences: Customize Numbering ---------------------------

    var sequenceLines = new List<string>();

    Func<string, string> usedByText = name =>
        usedBy.ContainsKey(name)
            ? " - used by " + string.Join(", ", usedBy[name].ToArray())
            : " - used by no revision";

    if (sequenceClass != null)
    {
        // 2022 ON: named sequences, each its own element.
        try
        {
            var getAll = sequenceClass.GetMethod("GetAllRevisionNumberingSequences",
                                                 new[] { typeof(Document) });
            var numberTypeOfSequence = sequenceClass.GetProperty("NumberType");
            var getNumeric = sequenceClass.GetMethod("GetNumericRevisionSettings", Type.EmptyTypes);
            var getAlphanumeric = sequenceClass.GetMethod("GetAlphanumericRevisionSettings", Type.EmptyTypes);

            var ids = getAll.Invoke(null, new object[] { doc }) as IEnumerable<ElementId>;
            var sequences = new List<Element>();
            if (ids != null)
                foreach (var id in ids)
                {
                    var sequence = doc.GetElement(id);
                    if (sequence != null) sequences.Add(sequence);
                }
            sequences.Sort(delegate (Element a, Element b)
            {
                return string.Compare(a.Name ?? "", b.Name ?? "", StringComparison.OrdinalIgnoreCase);
            });

            foreach (var sequence in sequences)
            {
                string described;
                try
                {
                    var kind = numberTypeOfSequence.GetValue(sequence, null);
                    if (kind != null && kind.Equals(RevisionNumberType.Alphanumeric))
                        described = describeAlphanumeric(
                            getAlphanumeric.Invoke(sequence, null) as AlphanumericRevisionSettings);
                    else if (kind != null && kind.Equals(RevisionNumberType.Numeric))
                        described = describeNumeric(
                            getNumeric.Invoke(sequence, null) as NumericRevisionSettings);
                    else
                        described = kind == null ? "type unreadable" : kind.ToString();
                }
                catch (Exception failure)
                {
                    var inner = failure.InnerException ?? failure;
                    described = "settings unreadable (" + inner.Message + ")";
                }
                sequenceLines.Add("'" + sequence.Name + "': " + described + usedByText(sequence.Name));
            }
        }
        catch (Exception failure)
        {
            var inner = failure.InnerException ?? failure;
            sequenceLines.Add("the numbering sequences could not be read: " + inner.Message);
        }
    }
    else
    {
        // 2020 AND 2021: one numeric and one alphanumeric scheme for the whole
        // project, with no names - each revision picks one by NumberType.
        var settingsClass = typeof(RevisionSettings);
        var getProjectNumeric = settingsClass.GetMethod("GetNumericRevisionSettings", Type.EmptyTypes);
        var getProjectAlphanumeric = settingsClass.GetMethod("GetAlphanumericRevisionSettings", Type.EmptyTypes);
        try
        {
            if (getProjectNumeric != null)
                sequenceLines.Add("Numeric (the project's one numeric scheme): " + describeNumeric(
                    getProjectNumeric.Invoke(revisionSettings, null) as NumericRevisionSettings)
                    + usedByText("Numeric"));
            if (getProjectAlphanumeric != null)
                sequenceLines.Add("Alphanumeric (the project's one alphanumeric scheme): "
                    + describeAlphanumeric(
                        getProjectAlphanumeric.Invoke(revisionSettings, null) as AlphanumericRevisionSettings)
                    + usedByText("Alphanumeric"));
            if (getProjectNumeric == null && getProjectAlphanumeric == null)
                sequenceLines.Add("this release exposes neither numbering sequences nor the "
                    + "project's numbering schemes");
        }
        catch (Exception failure)
        {
            var inner = failure.InnerException ?? failure;
            sequenceLines.Add("the numbering schemes could not be read: " + inner.Message);
        }
    }

    numberingSequences = string.Join(" || ", sequenceLines.ToArray());
}
