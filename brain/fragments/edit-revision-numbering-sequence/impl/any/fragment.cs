// NOT STANDALONE. Assumes `doc`, `name`, `newName`, `minimumDigits`,
// `startingNumber`, `prefix`, `suffix` and `customSequence` are in scope, and
// leaves `changed`, `alreadyThat`, `findings` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One undo puts it all back.
//
// THE EDIT NUMBERING SEQUENCE DIALOG: rename, and for a numeric sequence its
// minimum digits, starting number, prefix and suffix; for an alphanumeric one
// its custom sequence, prefix and suffix. Empty leaves a field alone; <blank>
// writes an empty prefix or suffix.
//
// THE TYPE IS FIXED ONCE MADE, so a value belonging to the other type is
// refused by name: a numeric sequence has no custom list, an alphanumeric one
// has no minimum digits or starting number.
//
// A SEQUENCE AN ISSUED REVISION USES KEEPS ITS SETTINGS. Changing them changes
// the number that revision prints, on drawings that have gone out - so a
// settings change is refused while any issued revision uses the sequence,
// naming each. A RENAME changes no printed number and is allowed.
//
// COMPARED FIRST, READ BACK AFTER. A field already at the value asked is named
// in `alreadyThat` and not written, so a repeat changes nothing. A write Revit
// took that reads back different throws, and so does a refusal after another
// write went in - the add-in rolls the whole call back. Only when Revit refused
// the first write, so nothing was stored, is it a refusal with Revit's own
// words and what it would have done.
//
// 2022 ON ONLY - earlier releases have no named sequences; see the contract.

var changed = "";
var alreadyThat = "";
var findings = "";
var refused = "";

{
    Func<string, string> quoted = value => string.IsNullOrEmpty(value) ? "(blank)" : "'" + value + "'";
    Func<Exception, string> revitSaid = failure => (failure.InnerException ?? failure).Message.TrimEnd('.');
    Func<string, string> asked = given =>
    {
        var typed = (given ?? "").Trim();
        if (typed.Length == 0) return null;
        if (string.Equals(typed, "<blank>", StringComparison.OrdinalIgnoreCase)) return "";
        return typed;
    };
    Func<RevisionNumberingSequence, string> describe = sequence =>
    {
        if (sequence.NumberType == RevisionNumberType.Alphanumeric)
        {
            var a = sequence.GetAlphanumericRevisionSettings();
            return "'" + sequence.SequenceName + "': alphanumeric, custom sequence "
                 + string.Join(", ", a.GetSequence().ToArray()) + ", prefix " + quoted(a.Prefix)
                 + ", suffix " + quoted(a.Suffix);
        }
        var n = sequence.GetNumericRevisionSettings();
        return "'" + sequence.SequenceName + "': numeric, minimum digits " + n.MinimumDigits
             + ", starting number " + n.StartNumber + ", prefix " + quoted(n.Prefix)
             + ", suffix " + quoted(n.Suffix);
    };

    var problems = new List<string>();
    var existing = new List<RevisionNumberingSequence>();
    foreach (var id in RevisionNumberingSequence.GetAllRevisionNumberingSequences(doc))
    {
        var sequence = doc.GetElement(id) as RevisionNumberingSequence;
        if (sequence != null) existing.Add(sequence);
    }
    var names = existing.Select(s => s.SequenceName).ToList();

    var wantedName = (name ?? "").Trim();
    var target = existing.FirstOrDefault(s => string.Equals(s.SequenceName, wantedName, StringComparison.OrdinalIgnoreCase));

    if (target == null)
        refused = "No numbering sequence called '" + wantedName + "' in " + doc.Title + " - it has "
                + (names.Count == 0 ? "none" : string.Join(", ", names.ToArray())) + ". Nothing was changed.";
    else
    {
        // ---- rename -------------------------------------------------------
        var renameTo = (newName ?? "").Trim();
        const string forbidden = "{}[]|;<>?`~";
        var renaming = renameTo.Length > 0 && renameTo != target.SequenceName;
        if (renaming)
        {
            if (renameTo.IndexOfAny(forbidden.ToCharArray()) >= 0)
                problems.Add("'" + renameTo + "' has a character Revit forbids in a sequence name - "
                           + "none of { } [ ] | ; < > ? ` ~");
            else if (existing.Any(s => s.Id != target.Id
                         && string.Equals(s.SequenceName, renameTo, StringComparison.OrdinalIgnoreCase)))
                problems.Add("'" + renameTo + "' is already another numbering sequence's name");
        }

        // ---- settings, built from what it holds now -------------------------
        var digitsText = (minimumDigits ?? "").Trim();
        var startText = (startingNumber ?? "").Trim();
        var listText = (customSequence ?? "").Trim();
        var wantedPrefix = asked(prefix);
        var wantedSuffix = asked(suffix);

        var isNumeric = target.NumberType == RevisionNumberType.Numeric;
        NumericRevisionSettings numeric = null;
        AlphanumericRevisionSettings alphanumeric = null;
        var fieldsAsked = new List<string>();
        var already = new List<string>();
        var plan = new List<string>();

        if (isNumeric)
        {
            if (listText.Length > 0)
                problems.Add("'" + target.SequenceName + "' is NUMERIC, so it has no custom list - "
                           + "Revit fixes a sequence's type when it is made");
            var now = target.GetNumericRevisionSettings();
            numeric = new NumericRevisionSettings(now);
            int whole;
            if (digitsText.Length > 0)
            {
                if (!int.TryParse(digitsText, out whole) || whole < 1)
                    problems.Add("minimumDigits is a whole number of 1 or more, not '" + digitsText + "'");
                else if (whole == now.MinimumDigits) already.Add("minimum digits " + whole);
                else { plan.Add("minimum digits " + now.MinimumDigits + " -> " + whole); numeric.MinimumDigits = whole; fieldsAsked.Add("minimum digits"); }
            }
            if (startText.Length > 0)
            {
                if (!int.TryParse(startText, out whole) || whole < 0)
                    problems.Add("startingNumber is a whole number of 0 or more, not '" + startText + "'");
                else if (whole == now.StartNumber) already.Add("starting number " + whole);
                else { plan.Add("starting number " + now.StartNumber + " -> " + whole); numeric.StartNumber = whole; fieldsAsked.Add("starting number"); }
            }
            if (wantedPrefix != null)
            {
                if (wantedPrefix == (now.Prefix ?? "")) already.Add("prefix " + quoted(wantedPrefix));
                else { plan.Add("prefix " + quoted(now.Prefix) + " -> " + quoted(wantedPrefix)); numeric.Prefix = wantedPrefix; fieldsAsked.Add("prefix"); }
            }
            if (wantedSuffix != null)
            {
                if (wantedSuffix == (now.Suffix ?? "")) already.Add("suffix " + quoted(wantedSuffix));
                else { plan.Add("suffix " + quoted(now.Suffix) + " -> " + quoted(wantedSuffix)); numeric.Suffix = wantedSuffix; fieldsAsked.Add("suffix"); }
            }
            if (fieldsAsked.Count > 0 && problems.Count == 0 && !numeric.IsValid())
                problems.Add("Revit calls those numeric settings invalid");
        }
        else
        {
            if (digitsText.Length > 0 || startText.Length > 0)
                problems.Add("'" + target.SequenceName + "' is ALPHANUMERIC, so it has no minimum digits "
                           + "or starting number - Revit fixes a sequence's type when it is made");
            var now = target.GetAlphanumericRevisionSettings();
            alphanumeric = new AlphanumericRevisionSettings(now);
            if (listText.Length > 0)
            {
                var values = listText.Split(',').Select(v => v.Trim()).Where(v => v.Length > 0).ToList();
                var current = now.GetSequence().ToList();
                if (values.SequenceEqual(current)) already.Add("custom sequence " + string.Join(", ", values.ToArray()));
                else
                {
                    plan.Add("custom sequence " + string.Join(", ", current.ToArray()) + " -> " + string.Join(", ", values.ToArray()));
                    alphanumeric.SetSequence(values);
                    fieldsAsked.Add("custom sequence");
                }
            }
            if (wantedPrefix != null)
            {
                if (wantedPrefix == (now.Prefix ?? "")) already.Add("prefix " + quoted(wantedPrefix));
                else { plan.Add("prefix " + quoted(now.Prefix) + " -> " + quoted(wantedPrefix)); alphanumeric.Prefix = wantedPrefix; fieldsAsked.Add("prefix"); }
            }
            if (wantedSuffix != null)
            {
                if (wantedSuffix == (now.Suffix ?? "")) already.Add("suffix " + quoted(wantedSuffix));
                else { plan.Add("suffix " + quoted(now.Suffix) + " -> " + quoted(wantedSuffix)); alphanumeric.Suffix = wantedSuffix; fieldsAsked.Add("suffix"); }
            }
            if (fieldsAsked.Count > 0 && problems.Count == 0 && !alphanumeric.IsValid())
                problems.Add("Revit calls those alphanumeric settings invalid - the custom sequence needs at least one value");
        }

        if (renameTo.Length > 0 && !renaming) already.Add("name '" + target.SequenceName + "'");
        if (renaming) plan.Add("name '" + target.SequenceName + "' -> '" + renameTo + "'");

        // ---- the issued revisions whose printed number a settings change moves
        var users = new List<string>();
        var issuedUsers = new List<string>();
        foreach (var id in Revision.GetAllRevisionIds(doc))
        {
            var revision = doc.GetElement(id) as Revision;
            if (revision == null || revision.RevisionNumberingSequenceId != target.Id) continue;
            var label = "Seq. " + revision.SequenceNumber + " " + quoted(revision.Description);
            users.Add(label);
            if (revision.Issued) issuedUsers.Add(label);
        }
        if (fieldsAsked.Count > 0 && issuedUsers.Count > 0)
            problems.Add("'" + target.SequenceName + "' numbers ISSUED revision(s) "
                       + string.Join(", ", issuedUsers.ToArray()) + ", and changing its "
                       + string.Join(", ", fieldsAsked.ToArray()) + " would change the number they print on "
                       + "drawings already sent out. A rename is allowed; the settings are not");

        if (renameTo.Length == 0 && digitsText.Length == 0 && startText.Length == 0 && listText.Length == 0
            && wantedPrefix == null && wantedSuffix == null && problems.Count == 0)
            problems.Add("nothing was asked for - every value was empty");

        if (problems.Count > 0)
            refused = string.Join("; ", problems.ToArray()) + ". Nothing was changed.";
        else
        {
            var done = new List<string>();
            var missed = new List<string>();
            // A WRITE REVIT TOOK THAT READS BACK DIFFERENT is stored, so it
            // throws below rather than being reported as "nothing changed".
            var storedWrong = false;
            var before = describe(target);

            if (fieldsAsked.Count > 0)
            {
                try
                {
                    if (isNumeric)
                    {
                        target.SetNumericRevisionSettings(numeric);
                        if (target.GetNumericRevisionSettings().IsEqual(numeric)) done.Add(string.Join(", ", fieldsAsked.ToArray()));
                        else { storedWrong = true; missed.Add("settings read back as " + describe(target)); }
                    }
                    else
                    {
                        target.SetAlphanumericRevisionSettings(alphanumeric);
                        if (target.GetAlphanumericRevisionSettings().IsEqual(alphanumeric)) done.Add(string.Join(", ", fieldsAsked.ToArray()));
                        else { storedWrong = true; missed.Add("settings read back as " + describe(target)); }
                    }
                }
                catch (Exception failure) { missed.Add("settings (" + revitSaid(failure) + ")"); }
            }

            if (missed.Count == 0 && renaming)
            {
                try
                {
                    target.SequenceName = renameTo;
                    if (target.SequenceName == renameTo) done.Add("renamed to '" + renameTo + "'");
                    else { storedWrong = true; missed.Add("name reads back '" + target.SequenceName + "'"); }
                }
                catch (Exception failure) { missed.Add("rename (" + revitSaid(failure) + ")"); }
            }

            if (missed.Count > 0 && (done.Count > 0 || storedWrong))
                throw new InvalidOperationException(
                    "EDIT_REVISION_NUMBERING_SEQUENCE did not read back as asked, so nothing is kept: "
                    + (done.Count > 0 ? string.Join("; ", done.ToArray()) + " went in, then " : "")
                    + string.Join("; ", missed.ToArray()) + ".");
            if (missed.Count > 0)
                refused = "Revit would not change '" + target.SequenceName + "': " + string.Join("; ", missed.ToArray())
                        + ". Nothing was changed. It would have: " + string.Join("; ", plan.ToArray()) + ".";
            else if (done.Count > 0)
                changed = string.Join("; ", plan.ToArray()) + ". Before: " + before + ".";
        }

        alreadyThat = string.Join("; ", already.ToArray());
        findings = "Read back: " + describe(target) + (users.Count == 0 ? " - used by no revision"
                 : " - used by " + string.Join(", ", users.ToArray()));
    }
}
