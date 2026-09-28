// NOT STANDALONE. Assumes `doc`, `name`, `numberType`, `copyFrom`,
// `minimumDigits`, `startingNumber`, `prefix`, `suffix` and `customSequence`
// are in scope, and leaves `created`, `findings` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// NEW AND DUPLICATE IN CUSTOMIZE NUMBERING > NUMBERING. With `copyFrom` empty
// it is New: `numberType` says numeric or alphanumeric, and the rest fill the
// Edit Numbering Sequence dialog. With `copyFrom` naming a sequence it is
// Duplicate: that sequence's settings are copied, its type is kept, and any
// value given here replaces the copied one.
//
// THE TYPE IS FIXED ONCE MADE - Revit gives a sequence no way to change it -
// so a value that belongs to the other type is refused by name rather than
// dropped: a numeric sequence has no custom list, an alphanumeric one has no
// minimum digits or starting number.
//
// EMPTY LEAVES REVIT'S DEFAULT (or the copied value); <blank> WRITES AN EMPTY
// PREFIX OR SUFFIX.
//
// EVERYTHING IS CHECKED BEFORE ANYTHING IS MADE - the name (not empty, not
// taken, none of the characters Revit forbids), the numbers, and the settings
// Revit itself calls valid. The new sequence is READ BACK field by field; one
// that does not read back as asked throws, and the add-in rolls it back.
//
// 2022 ON ONLY. Before 2022 a project had one numeric and one alphanumeric
// scheme and no named sequences, so there is nothing to create; the contract
// says so in `revit:` rather than pretending.

ElementId created = null;
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

    // ---- the name -----------------------------------------------------------
    var newName = (name ?? "").Trim();
    const string forbidden = "{}[]|;<>?`~";
    if (newName.Length == 0) problems.Add("a numbering sequence needs a name");
    else if (newName.IndexOfAny(forbidden.ToCharArray()) >= 0)
        problems.Add("'" + newName + "' has a character Revit forbids in a sequence name - "
                   + "none of { } [ ] | ; < > ? ` ~");
    else if (names.Any(n => string.Equals(n, newName, StringComparison.OrdinalIgnoreCase)))
        problems.Add("'" + newName + "' is already a numbering sequence in " + doc.Title
                   + " - it has " + string.Join(", ", names.ToArray()));

    // ---- what it copies, and which type ---------------------------------------
    RevisionNumberingSequence source = null;
    var copyName = (copyFrom ?? "").Trim();
    if (copyName.Length > 0)
    {
        source = existing.FirstOrDefault(s => string.Equals(s.SequenceName, copyName, StringComparison.OrdinalIgnoreCase));
        if (source == null)
            problems.Add("no numbering sequence called '" + copyName + "' to duplicate - "
                       + doc.Title + " has " + string.Join(", ", names.ToArray()));
    }

    var typeWord = (numberType ?? "").Trim().ToLowerInvariant();
    RevisionNumberType? kind = null;
    if (typeWord == "numeric") kind = RevisionNumberType.Numeric;
    else if (typeWord == "alphanumeric" || typeWord == "alphabetic") kind = RevisionNumberType.Alphanumeric;
    else if (typeWord.Length > 0)
        problems.Add("numberType takes 'numeric' or 'alphanumeric', not '" + numberType + "'");

    if (source != null)
    {
        if (kind != null && kind.Value != source.NumberType)
            problems.Add("'" + source.SequenceName + "' is " + source.NumberType.ToString().ToLowerInvariant()
                       + ", and a duplicate keeps its type - Revit fixes a sequence's type when it is made");
        kind = source.NumberType;
    }
    else if (kind == null && copyName.Length == 0)
        problems.Add("say numberType=numeric or numberType=alphanumeric for a new sequence, or "
                   + "copyFrom=<a sequence> to duplicate one");

    // ---- the settings ---------------------------------------------------------
    var digitsText = (minimumDigits ?? "").Trim();
    var startText = (startingNumber ?? "").Trim();
    var listText = (customSequence ?? "").Trim();
    var wantedPrefix = asked(prefix);
    var wantedSuffix = asked(suffix);

    NumericRevisionSettings numeric = null;
    AlphanumericRevisionSettings alphanumeric = null;
    var plan = "";

    if (kind == RevisionNumberType.Numeric)
    {
        if (listText.Length > 0)
            problems.Add("a numeric sequence has no custom list - customSequence belongs to an "
                       + "alphanumeric one, and the type cannot change once made");
        numeric = source != null
            ? new NumericRevisionSettings(source.GetNumericRevisionSettings())
            : new NumericRevisionSettings();
        int whole;
        if (digitsText.Length > 0)
        {
            if (int.TryParse(digitsText, out whole) && whole >= 1) numeric.MinimumDigits = whole;
            else problems.Add("minimumDigits is a whole number of 1 or more, not '" + digitsText + "'");
        }
        if (startText.Length > 0)
        {
            if (int.TryParse(startText, out whole) && whole >= 0) numeric.StartNumber = whole;
            else problems.Add("startingNumber is a whole number of 0 or more, not '" + startText + "'");
        }
        if (wantedPrefix != null) numeric.Prefix = wantedPrefix;
        if (wantedSuffix != null) numeric.Suffix = wantedSuffix;
        if (problems.Count == 0 && !numeric.IsValid())
            problems.Add("Revit calls those numeric settings invalid");
        plan = "numeric, minimum digits " + numeric.MinimumDigits + ", starting number "
             + numeric.StartNumber + ", prefix " + quoted(numeric.Prefix) + ", suffix " + quoted(numeric.Suffix);
    }
    else if (kind == RevisionNumberType.Alphanumeric)
    {
        if (digitsText.Length > 0 || startText.Length > 0)
            problems.Add("an alphanumeric sequence has no minimum digits or starting number - those "
                       + "belong to a numeric one, and the type cannot change once made");
        alphanumeric = source != null
            ? new AlphanumericRevisionSettings(source.GetAlphanumericRevisionSettings())
            : new AlphanumericRevisionSettings();
        if (listText.Length > 0)
        {
            var values = listText.Split(',').Select(v => v.Trim()).Where(v => v.Length > 0).ToList();
            alphanumeric.SetSequence(values);
        }
        if (wantedPrefix != null) alphanumeric.Prefix = wantedPrefix;
        if (wantedSuffix != null) alphanumeric.Suffix = wantedSuffix;
        if (problems.Count == 0 && !alphanumeric.IsValid())
            problems.Add("an alphanumeric sequence needs its custom sequence - customSequence=A, B, C, "
                       + "D - and Revit calls these settings invalid without one");
        plan = "alphanumeric, custom sequence " + string.Join(", ", alphanumeric.GetSequence().ToArray())
             + ", prefix " + quoted(alphanumeric.Prefix) + ", suffix " + quoted(alphanumeric.Suffix);
    }

    if (problems.Count > 0)
        refused = string.Join("; ", problems.ToArray()) + ". Nothing was created.";
    else
    {
        RevisionNumberingSequence made = null;
        try
        {
            made = numeric != null
                ? RevisionNumberingSequence.CreateNumericSequence(doc, newName, numeric)
                : RevisionNumberingSequence.CreateAlphanumericSequence(doc, newName, alphanumeric);
        }
        catch (Exception failure)
        {
            refused = "Revit would not create the numbering sequence '" + newName + "': "
                    + revitSaid(failure) + ". Nothing was created. It would have been " + plan
                    + (source != null ? ", duplicated from '" + source.SequenceName + "'" : "") + ".";
        }

        if (made != null)
        {
            // READ BACK, field by field.
            var off = new List<string>();
            if (made.SequenceName != newName) off.Add("name reads back '" + made.SequenceName + "'");
            if (numeric != null)
            {
                var back = made.GetNumericRevisionSettings();
                if (!back.IsEqual(numeric)) off.Add("settings read back as " + describe(made));
            }
            else
            {
                var back = made.GetAlphanumericRevisionSettings();
                if (!back.IsEqual(alphanumeric)) off.Add("settings read back as " + describe(made));
            }
            if (off.Count > 0)
                throw new InvalidOperationException("The new numbering sequence did not read back as asked, so "
                    + "nothing is kept: " + string.Join("; ", off.ToArray()) + ".");

            created = made.Id;
            findings = "Read back: " + describe(made)
                     + (source != null ? " - duplicated from '" + source.SequenceName + "'" : "")
                     + ". Used by no revision yet: EDIT_REVISION numbering=" + made.SequenceName
                     + " puts a revision on it.";
        }
    }
}
