// NOT STANDALONE. Assumes `doc`, `name`, `newName`, `minimumDigits`,
// `startingNumber`, `prefix`, `suffix` and `customSequence` are in scope, and
// leaves `changed`, `alreadyThat`, `findings` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One undo puts it all back.
//
// ONE REVISION NUMBERING, EDITED - ON EVERY RELEASE 2020 TO 2027, WHERE THE
// RELEASES KEEP IT IN TWO DIFFERENT PLACES.
//   2022 on: named sequences, each its own element - the Edit button under
//            Customize Numbering. Picked by its name.
//   2020, 2021: ONE numeric and ONE alphanumeric scheme for the whole project,
//            with no names - Sheet Issues/Revisions > Numbering Options. Picked
//            by name=Numeric or name=Alphanumeric, the two words LIST_REVISIONS
//            prints for them; with no name, the values asked say which (a custom
//            list belongs only to the alphanumeric one).
// For a numeric one: minimum digits (2022 on only), starting number, prefix and
// suffix; for an alphanumeric one: its custom sequence, prefix and suffix; on
// 2022 on, a new name. Empty leaves a field alone; <blank> writes an empty
// prefix or suffix.
//
// THE COMPILER CANNOT BE ASKED WHICH RELEASE THIS IS. `RevisionNumberingSequence`,
// `Revision.RevisionNumberingSequenceId` and `NumericRevisionSettings.MinimumDigits`
// arrive in 2022; `RevisionSettings`' Get/Set(Alpha)NumericRevisionSettings and
// `Revision.NumberType` exist 2020 to 2022 and are gone from 2023. So every one
// of those is reached BY NAME and the one this Revit has is used - 2022, which
// has both, takes the named sequences, as LIST_REVISIONS does. Only members every
// release 2020 to 2027 ships are named in the code.
//
// THE CUSTOM LIST IS TAKEN EXACTLY AS GIVEN: commas between values, the spaces
// round each value dropped. Nothing is filled in or thrown away - an empty value
// (two commas together, or one at an end), the same value twice (capitals
// ignored), and an ellipsis standing for values not written out are each refused
// by name, and so is a list with no values in it.
//
// THE TYPE IS FIXED, so a value belonging to the other type is refused by name:
// a numeric scheme has no custom list, an alphanumeric one has no minimum digits
// or starting number.
//
// A NUMBERING AN ISSUED REVISION USES KEEPS ITS SETTINGS. Changing them changes
// the number that revision prints, on drawings that have gone out - so a
// settings change is refused while any issued revision uses it, naming each. A
// RENAME changes no printed number and is allowed. Which revisions use which
// numbering is never changed here: that is EDIT_REVISION's `numbering`.
//
// COMPARED FIRST, READ BACK AFTER. A field already at the value asked is named
// in `alreadyThat` and not written, so a repeat changes nothing. A write Revit
// took that reads back different throws, and so does a refusal after another
// write went in - the add-in rolls the whole call back. Only when Revit refused
// the first write, so nothing was stored, is it a refusal with Revit's own
// words and what it would have done.

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

    // ---- the members that exist on only part of 2020 to 2027, by name ------------
    var revisionClass = typeof(Revision);
    var sequenceClass = revisionClass.Assembly.GetType(
        revisionClass.Namespace + ".RevisionNumberingSequence");                              // 2022 on
    var sequenceIdProperty = revisionClass.GetProperty("RevisionNumberingSequenceId");          // 2022 on
    var numberTypeProperty = revisionClass.GetProperty("NumberType");                           // 2020 to 2022
    var minimumDigitsProperty = typeof(NumericRevisionSettings).GetProperty("MinimumDigits");   // 2022 on
    var settingsClass = typeof(RevisionSettings);
    var projectGetNumeric = settingsClass.GetMethod("GetNumericRevisionSettings", Type.EmptyTypes);          // 2020 to 2022
    var projectSetNumeric = settingsClass.GetMethod("SetNumericRevisionSettings",
                                                    new[] { typeof(NumericRevisionSettings) });              // 2020 to 2022
    var projectGetAlphanumeric = settingsClass.GetMethod("GetAlphanumericRevisionSettings", Type.EmptyTypes); // 2020 to 2022
    var projectSetAlphanumeric = settingsClass.GetMethod("SetAlphanumericRevisionSettings",
                                                         new[] { typeof(AlphanumericRevisionSettings) });    // 2020 to 2022

    var namedRoute = sequenceClass != null && sequenceIdProperty != null;
    var projectRoute = !namedRoute && projectGetNumeric != null && projectSetNumeric != null
                    && projectGetAlphanumeric != null && projectSetAlphanumeric != null && numberTypeProperty != null;

    // ---- what was asked ----------------------------------------------------------
    var wantedName = (name ?? "").Trim();
    var renameTo = (newName ?? "").Trim();
    var digitsText = (minimumDigits ?? "").Trim();
    var startText = (startingNumber ?? "").Trim();
    var listText = (customSequence ?? "").Trim();
    var wantedPrefix = asked(prefix);
    var wantedSuffix = asked(suffix);

    // ---- the one numbering this call edits, whichever release this is -----------
    var problems = new List<string>();
    string targetLabel = null;
    var isNumeric = false;
    Element targetSequence = null;                                  // 2022 on
    Func<NumericRevisionSettings> readNumeric = null;
    Action<NumericRevisionSettings> writeNumeric = null;
    Func<AlphanumericRevisionSettings> readAlphanumeric = null;
    Action<AlphanumericRevisionSettings> writeAlphanumeric = null;
    Func<string> readName = null;
    Action<string> writeName = null;                                // null on 2020 and 2021: no names
    Func<Revision, bool> usesIt = null;
    var otherNames = new List<string>();                            // 2022 on: for a rename clash

    if (namedRoute)
    {
        var getAll = sequenceClass.GetMethod("GetAllRevisionNumberingSequences", new[] { typeof(Document) });
        var nameProperty = sequenceClass.GetProperty("SequenceName");
        var typeProperty = sequenceClass.GetProperty("NumberType");
        var sequenceGetNumeric = sequenceClass.GetMethod("GetNumericRevisionSettings", Type.EmptyTypes);
        var sequenceSetNumeric = sequenceClass.GetMethod("SetNumericRevisionSettings",
                                                         new[] { typeof(NumericRevisionSettings) });
        var sequenceGetAlphanumeric = sequenceClass.GetMethod("GetAlphanumericRevisionSettings", Type.EmptyTypes);
        var sequenceSetAlphanumeric = sequenceClass.GetMethod("SetAlphanumericRevisionSettings",
                                                              new[] { typeof(AlphanumericRevisionSettings) });

        var existing = new List<Element>();
        var ids = getAll.Invoke(null, new object[] { doc }) as IEnumerable<ElementId>;
        if (ids != null)
            foreach (var id in ids)
            {
                var sequence = doc.GetElement(id);
                if (sequence != null) existing.Add(sequence);
            }
        Func<Element, string> nameOf = sequence => (nameProperty.GetValue(sequence, null) as string) ?? "";
        var names = existing.Select(nameOf).ToList();

        targetSequence = existing.FirstOrDefault(s => string.Equals(nameOf(s), wantedName, StringComparison.OrdinalIgnoreCase));
        if (targetSequence == null)
            problems.Add((wantedName.Length == 0 ? "name is empty - say which numbering sequence"
                                                 : "No numbering sequence called '" + wantedName + "'")
                       + " in " + doc.Title + " - it has "
                       + (names.Count == 0 ? "none" : string.Join(", ", names.ToArray())));
        else
        {
            var sequence = targetSequence;
            var kind = typeProperty.GetValue(sequence, null);
            isNumeric = kind != null && kind.Equals(RevisionNumberType.Numeric);
            readNumeric = () => sequenceGetNumeric.Invoke(sequence, null) as NumericRevisionSettings;
            writeNumeric = settings => sequenceSetNumeric.Invoke(sequence, new object[] { settings });
            readAlphanumeric = () => sequenceGetAlphanumeric.Invoke(sequence, null) as AlphanumericRevisionSettings;
            writeAlphanumeric = settings => sequenceSetAlphanumeric.Invoke(sequence, new object[] { settings });
            readName = () => nameOf(sequence);
            writeName = text => nameProperty.SetValue(sequence, text, null);
            usesIt = revision =>
            {
                var id = sequenceIdProperty.GetValue(revision, null) as ElementId;
                return id != null && id.Equals(sequence.Id);
            };
            foreach (var other in existing)
                if (!other.Id.Equals(sequence.Id)) otherNames.Add(nameOf(other));
            targetLabel = nameOf(sequence);
        }
    }
    else if (projectRoute)
    {
        // 2020 AND 2021: the project's two schemes, on RevisionSettings.
        var revisionSettings = RevisionSettings.GetRevisionSettings(doc);
        var choice = wantedName;
        if (choice.Length == 0)
        {
            var numericAsked = digitsText.Length > 0 || startText.Length > 0;
            if (listText.Length > 0 && !numericAsked) choice = "Alphanumeric";
            else if (numericAsked && listText.Length == 0) choice = "Numeric";
        }
        if (string.Equals(choice, "Numeric", StringComparison.OrdinalIgnoreCase)) isNumeric = true;
        else if (!string.Equals(choice, "Alphanumeric", StringComparison.OrdinalIgnoreCase))
            problems.Add("this Revit keeps ONE numeric and ONE alphanumeric revision numbering for the whole "
                       + "project, with no names - name=Numeric or name=Alphanumeric, "
                       + (wantedName.Length == 0 ? "and the values asked do not say which"
                                                 : "not '" + wantedName + "'"));
        if (problems.Count == 0)
        {
            var kind = isNumeric ? RevisionNumberType.Numeric : RevisionNumberType.Alphanumeric;
            targetLabel = isNumeric ? "Numeric" : "Alphanumeric";
            readNumeric = () => projectGetNumeric.Invoke(revisionSettings, null) as NumericRevisionSettings;
            writeNumeric = settings => projectSetNumeric.Invoke(revisionSettings, new object[] { settings });
            readAlphanumeric = () => projectGetAlphanumeric.Invoke(revisionSettings, null) as AlphanumericRevisionSettings;
            writeAlphanumeric = settings => projectSetAlphanumeric.Invoke(revisionSettings, new object[] { settings });
            var label = targetLabel;
            readName = () => label;
            usesIt = revision =>
            {
                var used = numberTypeProperty.GetValue(revision, null);
                return used != null && used.Equals(kind);
            };
        }
    }
    else
        problems.Add("this Revit exposes neither named numbering sequences nor the project's numbering "
                   + "schemes, so there is nothing this can edit");

    Func<string> describe = () =>
    {
        var where = namedRoute ? "'" + readName() + "'"
                  : readName() + " (the project's one " + readName().ToLowerInvariant() + " scheme)";
        if (!isNumeric)
        {
            var a = readAlphanumeric();
            var list = a.GetSequence().ToList();
            return where + ": alphanumeric, custom sequence " + string.Join(", ", list.ToArray())
                 + " (" + list.Count + " values), prefix " + quoted(a.Prefix) + ", suffix " + quoted(a.Suffix);
        }
        var n = readNumeric();
        var digits = minimumDigitsProperty == null ? "" : ", minimum digits " + minimumDigitsProperty.GetValue(n, null);
        return where + ": numeric" + digits + ", starting number " + n.StartNumber
             + ", prefix " + quoted(n.Prefix) + ", suffix " + quoted(n.Suffix);
    };

    if (targetLabel == null)
        refused = string.Join("; ", problems.ToArray()) + ". Nothing was changed.";
    else
    {
        // ---- rename: named sequences only -------------------------------------------
        const string forbidden = "{}[]|;<>?`~";
        var renaming = renameTo.Length > 0 && renameTo != targetLabel;
        if (renaming)
        {
            if (writeName == null)
                problems.Add("Revit 2020 and 2021 give the project's two revision numberings no names, so '"
                           + targetLabel + "' cannot be renamed - named sequences arrive in Revit 2022");
            else if (renameTo.IndexOfAny(forbidden.ToCharArray()) >= 0)
                problems.Add("'" + renameTo + "' has a character Revit forbids in a sequence name - "
                           + "none of { } [ ] | ; < > ? ` ~");
            else if (otherNames.Any(other => string.Equals(other, renameTo, StringComparison.OrdinalIgnoreCase)))
                problems.Add("'" + renameTo + "' is already another numbering sequence's name");
        }

        // ---- settings, built from what it holds now -----------------------------------
        NumericRevisionSettings numeric = null;
        AlphanumericRevisionSettings alphanumeric = null;
        var fieldsAsked = new List<string>();
        var already = new List<string>();
        var plan = new List<string>();

        if (isNumeric)
        {
            if (listText.Length > 0)
                problems.Add("'" + targetLabel + "' is NUMERIC, so it has no custom list - "
                           + "the alphanumeric numbering holds that");
            var now = readNumeric();
            numeric = new NumericRevisionSettings(now);
            int whole;
            if (digitsText.Length > 0)
            {
                if (minimumDigitsProperty == null)
                    problems.Add("this Revit's numeric revision numbering has no minimum digits - that setting "
                               + "arrives in Revit 2022");
                else if (!int.TryParse(digitsText, out whole) || whole < 1)
                    problems.Add("minimumDigits is a whole number of 1 or more, not '" + digitsText + "'");
                else
                {
                    var nowDigits = (int)minimumDigitsProperty.GetValue(now, null);
                    if (whole == nowDigits) already.Add("minimum digits " + whole);
                    else
                    {
                        plan.Add("minimum digits " + nowDigits + " -> " + whole);
                        minimumDigitsProperty.SetValue(numeric, whole, null);
                        fieldsAsked.Add("minimum digits");
                    }
                }
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
                problems.Add("'" + targetLabel + "' is ALPHANUMERIC, so it has no minimum digits "
                           + "or starting number - the numeric numbering holds those");
            var now = readAlphanumeric();
            alphanumeric = new AlphanumericRevisionSettings(now);
            if (listText.Length > 0)
            {
                // THE WHOLE LIST, EXACTLY AS GIVEN - checked before anything is written.
                var listProblems = new List<string>();
                var values = listText.Split(',').Select(v => v.Trim()).ToList();
                if (string.Equals(listText, "<blank>", StringComparison.OrdinalIgnoreCase)
                    || values.All(v => v.Length == 0))
                    listProblems.Add("customSequence holds no values, and Revit needs at least one - give "
                                   + "them in order with commas between, as AA, BB, CC");
                else
                {
                    var emptyAt = new List<string>();
                    var ellipsisAt = new List<string>();
                    for (var i = 0; i < values.Count; i++)
                    {
                        if (values[i].Length == 0) emptyAt.Add((i + 1).ToString());
                        else if (values[i].IndexOf((char)0x2026) >= 0 || values[i].Contains("...")) ellipsisAt.Add((i + 1).ToString());
                    }
                    if (emptyAt.Count > 0)
                        listProblems.Add("customSequence has an empty value at position " + string.Join(", ", emptyAt.ToArray())
                                       + " of " + values.Count + " - two commas with nothing between, or a comma at "
                                       + "an end. The list is taken exactly as given, so nothing is dropped");
                    if (ellipsisAt.Count > 0)
                        listProblems.Add("customSequence has an ellipsis at position " + string.Join(", ", ellipsisAt.ToArray())
                                       + " - nothing is filled in, so write every value out");
                    var repeated = values.Where(v => v.Length > 0)
                                         .GroupBy(v => v, StringComparer.OrdinalIgnoreCase)
                                         .Where(g => g.Count() > 1)
                                         .Select(g => "'" + g.Key + "' " + g.Count() + " times")
                                         .ToList();
                    if (repeated.Count > 0)
                        listProblems.Add("customSequence repeats " + string.Join(", ", repeated.ToArray())
                                       + " (capitals ignored) - two revisions would print the same number");
                }
                if (listProblems.Count > 0) problems.AddRange(listProblems);
                else
                {
                    var current = now.GetSequence().ToList();
                    if (values.SequenceEqual(current))
                        already.Add("custom sequence " + string.Join(", ", values.ToArray()) + " (" + values.Count + " values)");
                    else
                    {
                        plan.Add("custom sequence " + string.Join(", ", current.ToArray()) + " (" + current.Count + " values) -> "
                               + string.Join(", ", values.ToArray()) + " (" + values.Count + " values)");
                        alphanumeric.SetSequence(values);
                        fieldsAsked.Add("custom sequence");
                    }
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
                problems.Add("Revit calls those alphanumeric settings invalid");
        }

        if (renameTo.Length > 0 && !renaming) already.Add("name '" + targetLabel + "'");
        if (renaming && writeName != null) plan.Add("name '" + targetLabel + "' -> '" + renameTo + "'");

        // ---- the issued revisions whose printed number a settings change moves --------
        var users = new List<string>();
        var issuedUsers = new List<string>();
        foreach (var id in Revision.GetAllRevisionIds(doc))
        {
            var revision = doc.GetElement(id) as Revision;
            if (revision == null || !usesIt(revision)) continue;
            var label = "Seq. " + revision.SequenceNumber + " " + quoted(revision.Description);
            users.Add(label);
            if (revision.Issued) issuedUsers.Add(label);
        }
        if (fieldsAsked.Count > 0 && issuedUsers.Count > 0)
            problems.Add("'" + targetLabel + "' numbers ISSUED revision(s) "
                       + string.Join(", ", issuedUsers.ToArray()) + ", and changing its "
                       + string.Join(", ", fieldsAsked.ToArray()) + " would change the number they print on "
                       + "drawings already sent out" + (writeName != null ? ". A rename is allowed; the settings are not" : ""));

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
            var before = describe();

            if (fieldsAsked.Count > 0)
            {
                try
                {
                    if (isNumeric)
                    {
                        writeNumeric(numeric);
                        if (readNumeric().IsEqual(numeric)) done.Add(string.Join(", ", fieldsAsked.ToArray()));
                        else { storedWrong = true; missed.Add("settings read back as " + describe()); }
                    }
                    else
                    {
                        writeAlphanumeric(alphanumeric);
                        var stored = readAlphanumeric();
                        if (stored.IsEqual(alphanumeric)
                            && stored.GetSequence().SequenceEqual(alphanumeric.GetSequence()))
                            done.Add(string.Join(", ", fieldsAsked.ToArray()));
                        else { storedWrong = true; missed.Add("settings read back as " + describe()); }
                    }
                }
                catch (Exception failure) { missed.Add("settings (" + revitSaid(failure) + ")"); }
            }

            if (missed.Count == 0 && renaming)
            {
                try
                {
                    writeName(renameTo);
                    if (readName() == renameTo) { done.Add("renamed to '" + renameTo + "'"); targetLabel = renameTo; }
                    else { storedWrong = true; missed.Add("name reads back '" + readName() + "'"); }
                }
                catch (Exception failure) { missed.Add("rename (" + revitSaid(failure) + ")"); }
            }

            if (missed.Count > 0 && (done.Count > 0 || storedWrong))
                throw new InvalidOperationException(
                    "EDIT_REVISION_NUMBERING_SEQUENCE did not read back as asked, so nothing is kept: "
                    + (done.Count > 0 ? string.Join("; ", done.ToArray()) + " went in, then " : "")
                    + string.Join("; ", missed.ToArray()) + ".");
            if (missed.Count > 0)
                refused = "Revit would not change '" + targetLabel + "': " + string.Join("; ", missed.ToArray())
                        + ". Nothing was changed. It would have: " + string.Join("; ", plan.ToArray()) + ".";
            else if (done.Count > 0)
                changed = string.Join("; ", plan.ToArray()) + ". Before: " + before + ".";
        }

        alreadyThat = string.Join("; ", already.ToArray());
        findings = "Read back: " + describe() + (users.Count == 0 ? " - used by no revision"
                 : " - used by " + string.Join(", ", users.ToArray()));
    }
}
