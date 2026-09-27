// NOT STANDALONE. Assumes `doc`, `elements`, `description`, `revisionDate`,
// `issuedBy`, `issuedTo`, `show`, `numbering` and `issueState` are in scope,
// and leaves `changed`, `alreadyThat`, `nowIssued`, `revisionName`,
// `findings` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// ONE REVISION, HANDED IN BY A FIND - NEVER THE SELECTION, NEVER A GUESS.
//
// The revision arrives from the fragment before this one -
// SELECT_BY_PARAMETER_VALUE on the Revisions category, by `Revision Sequence`
// (the number the Sheet Issues/Revisions dialog shows, unique by construction)
// or by `Revision Description`. Descriptions repeat across a job - two issues called
// "For Construction" is normal - so a find that brings back two is refused
// with both named, rather than editing whichever came first on a document
// about to go out.
//
// AN ISSUED REVISION IS LOCKED, AND THE REFUSAL NAMES IT. Revit refuses to
// change the description, date, issued to, issued by or numbering of an
// issued revision (its own API documentation says so, per property). Asked
// for any of those on an issued one, this changes NOTHING and says which
// revision and which fields. `issueState=unissue` in the same call un-issues
// it first, because that is the explicit decision Revit's lock is asking for.
// Show is the one column Revit leaves open on an issued revision, and so does
// this.
//
// THE ISSUED TICK MOVES ONLY WHEN `issueState` SAYS SO. "issue" is applied
// after every other column, "unissue" before them.
//
// EMPTY MEANS LEAVE ALONE; THE WORD <blank> WRITES AN EMPTY VALUE.
//
// EVERY FIELD IS COMPARED BEFORE IT IS WRITTEN AND READ BACK AFTER. A field
// already holding the wanted value is named in `alreadyThat` and not written,
// so a second identical run changes nothing and says so. A write that does
// not read back, after something else in this call has already changed,
// throws - and the add-in rolls the whole call back, never half an edit. If
// nothing had changed yet, it is a refusal carrying Revit's own words and what
// the call would have done, which is what makes a run with no transaction open
// a measurement.

var changed = "";
var alreadyThat = "";
var nowIssued = false;
var revisionName = "";
var findings = "";
var refused = "";

{
    Func<string, string> asked = given =>
    {
        var typed = (given ?? "").Trim();
        if (typed.Length == 0) return null;
        if (string.Equals(typed, "<blank>", StringComparison.OrdinalIgnoreCase)) return "";
        return typed;
    };
    Func<string, string> quoted = value => string.IsNullOrEmpty(value) ? "(blank)" : "'" + value + "'";
    Func<Exception, string> revitSaid = failure => (failure.InnerException ?? failure).Message.TrimEnd('.');

    var problems = new List<string>();

    // ---- which revision --------------------------------------------------------
    var handedIn = new List<Revision>();
    var notRevisions = 0;
    if (elements != null)
        foreach (var element in elements)
        {
            var candidate = element as Revision;
            if (candidate != null) handedIn.Add(candidate);
            else if (element != null) notRevisions++;
        }

    Revision target = null;
    if (handedIn.Count == 1) target = handedIn[0];
    else if (handedIn.Count == 0)
        problems.Add("no revision was handed in" + (notRevisions > 0
            ? " - " + notRevisions + " element(s) came in and none of them is a revision" : "")
            + ". Find it first with SELECT_BY_PARAMETER_VALUE, categories=Revisions, "
            + "parameterName=Revision Sequence (the number the dialog shows) or Revision Description");
    else
    {
        var labels = new List<string>();
        foreach (var candidate in handedIn)
            labels.Add("Seq. " + candidate.SequenceNumber + " " + quoted(candidate.Description));
        problems.Add(handedIn.Count + " revisions were handed in (" + string.Join(", ", labels.ToArray())
            + ") and this edits ONE. Find it by Revision Sequence, which is unique");
    }

    // ---- show ------------------------------------------------------------------
    RevisionVisibility? wantedShow = null;
    var showWord = (show ?? "").Trim().ToLowerInvariant().Replace("&", "and").Replace(" ", "");
    if (showWord.Length > 0)
    {
        if (showWord == "cloudandtag") wantedShow = RevisionVisibility.CloudAndTagVisible;
        else if (showWord == "tag") wantedShow = RevisionVisibility.TagVisible;
        else if (showWord == "none" || showWord == "hidden") wantedShow = RevisionVisibility.Hidden;
        else problems.Add("show takes 'cloud and tag', 'tag' or 'none' - the dialog's three "
                        + "choices - not '" + show + "'");
    }
    Func<RevisionVisibility, string> showName = value =>
        value == RevisionVisibility.CloudAndTagVisible ? "Cloud and Tag"
        : value == RevisionVisibility.TagVisible ? "Tag" : "None";

    // ---- the issued tick: its own value ----------------------------------------
    var issueWord = (issueState ?? "").Trim().ToLowerInvariant();
    if (issueWord.Length > 0 && issueWord != "issue" && issueWord != "unissue")
        problems.Add("issueState takes 'issue', 'unissue' or nothing, not '" + issueState + "'");

    // ---- numbering: a sequence by name (2022 on) or a kind (2020, 2021) --------
    var revisionClass = typeof(Revision);
    var sequenceIdProperty = revisionClass.GetProperty("RevisionNumberingSequenceId");   // 2022 on
    var numberTypeProperty = revisionClass.GetProperty("NumberType");                    // 2020 to 2022
    var sequenceClass = revisionClass.Assembly.GetType(
        revisionClass.Namespace + ".RevisionNumberingSequence");                         // 2022 on

    object numberingValue = null;
    var numberingName = "";
    var wantedNumbering = (numbering ?? "").Trim();
    if (wantedNumbering.Length > 0)
    {
        if (sequenceIdProperty != null && sequenceClass != null)
        {
            var names = new List<string>();
            var getAll = sequenceClass.GetMethod("GetAllRevisionNumberingSequences", new[] { typeof(Document) });
            var ids = getAll.Invoke(null, new object[] { doc }) as IEnumerable<ElementId>;
            if (ids != null)
                foreach (var id in ids)
                {
                    var sequence = doc.GetElement(id);
                    if (sequence == null) continue;
                    names.Add(sequence.Name);
                    if (string.Equals(sequence.Name, wantedNumbering, StringComparison.OrdinalIgnoreCase))
                    {
                        numberingValue = sequence.Id;
                        numberingName = sequence.Name;
                    }
                }
            if (numberingValue == null && string.Equals(wantedNumbering, "none", StringComparison.OrdinalIgnoreCase))
            {
                numberingValue = ElementId.InvalidElementId;
                numberingName = "None";
            }
            if (numberingValue == null)
                problems.Add("no numbering sequence called '" + wantedNumbering + "' in " + doc.Title
                           + " - it has " + (names.Count == 0 ? "none" : string.Join(", ", names.ToArray())));
        }
        else if (numberTypeProperty != null)
        {
            var kind = wantedNumbering.ToLowerInvariant();
            if (kind == "numeric") numberingValue = RevisionNumberType.Numeric;
            else if (kind == "alphanumeric") numberingValue = RevisionNumberType.Alphanumeric;
            else if (kind == "none") numberingValue = RevisionNumberType.None;
            else problems.Add("this Revit has no named numbering sequences - a revision is numbered "
                            + "'numeric', 'alphanumeric' or 'none', not '" + wantedNumbering + "'");
            if (numberingValue != null) numberingName = numberingValue.ToString();
        }
        else problems.Add("this Revit exposes no way to choose a revision's numbering");
    }

    Func<Revision, object> currentNumbering = r =>
    {
        if (sequenceIdProperty != null) return sequenceIdProperty.GetValue(r, null);
        if (numberTypeProperty != null) return numberTypeProperty.GetValue(r, null);
        return null;
    };
    Func<object, string> numberingLabel = value =>
    {
        var id = value as ElementId;
        if (id != null)
        {
            var element = id == ElementId.InvalidElementId ? null : doc.GetElement(id);
            return element == null ? "None" : element.Name;
        }
        return value == null ? "(unknown)" : value.ToString();
    };

    var textFields = new[]
    {
        new { Label = "description", Wanted = asked(description) },
        new { Label = "date", Wanted = asked(revisionDate) },
        new { Label = "issued by", Wanted = asked(issuedBy) },
        new { Label = "issued to", Wanted = asked(issuedTo) },
    };

    var nothingAsked = wantedShow == null && numberingValue == null && issueWord.Length == 0;
    foreach (var field in textFields) if (field.Wanted != null) nothingAsked = false;
    if (nothingAsked && problems.Count == 0)
        problems.Add("nothing was asked for - every value was empty, so there is nothing to change. "
                   + "Empty leaves a field alone; <blank> writes an empty value");

    if (problems.Count > 0)
    {
        refused = string.Join("; ", problems.ToArray()) + ". Nothing was changed.";
    }
    else
    {
        revisionName = "Seq. " + target.SequenceNumber + " " + quoted(target.Description);

        Func<string, string> readText = label =>
            label == "description" ? target.Description
            : label == "date" ? target.RevisionDate
            : label == "issued by" ? target.IssuedBy
            : target.IssuedTo;
        Action<string, string> writeText = (label, value) =>
        {
            if (label == "description") target.Description = value;
            else if (label == "date") target.RevisionDate = value;
            else if (label == "issued by") target.IssuedBy = value;
            else target.IssuedTo = value;
        };

        // WHAT WOULD MOVE, worked out before anything does.
        var plan = new List<string>();
        var already = new List<string>();
        var lockedAsked = new List<string>();
        foreach (var field in textFields)
        {
            if (field.Wanted == null) continue;
            var now = readText(field.Label) ?? "";
            if (now == field.Wanted) { already.Add(field.Label + " " + quoted(now)); continue; }
            plan.Add(field.Label + " " + quoted(now) + " -> " + quoted(field.Wanted));
            lockedAsked.Add(field.Label);
        }
        if (numberingValue != null)
        {
            var nowNumbering = currentNumbering(target);
            if (nowNumbering != null && nowNumbering.Equals(numberingValue))
                already.Add("numbering " + numberingName);
            else
            {
                plan.Add("numbering " + numberingLabel(nowNumbering) + " -> " + numberingName);
                lockedAsked.Add("numbering");
            }
        }
        if (wantedShow != null)
        {
            if (target.Visibility == wantedShow.Value) already.Add("show " + showName(target.Visibility));
            else plan.Add("show " + showName(target.Visibility) + " -> " + showName(wantedShow.Value));
        }

        var wasIssued = target.Issued;
        if (issueWord == "issue")
        {
            if (wasIssued) already.Add("issued");
            else plan.Add("then issued");
        }
        else if (issueWord == "unissue")
        {
            if (!wasIssued) already.Add("not issued");
            else plan.Insert(0, "un-issued first");
        }

        if (wasIssued && issueWord != "unissue" && lockedAsked.Count > 0)
        {
            // THE LOCK, NAMED. Nothing is attempted: Revit would refuse each of
            // these on an issued revision, and a partial edit is worse than none.
            refused = revisionName + " is ISSUED, and Revit locks the "
                    + string.Join(", ", lockedAsked.ToArray()) + " of an issued revision. "
                    + "Nothing was changed. Un-issuing it first (issueState=unissue) is the "
                    + "owner's decision to take, because the issue it records has gone out.";
        }
        else
        {
            var done = new List<string>();
            var missed = new List<string>();

            // 1. UN-ISSUE FIRST, when asked: every locked column needs it.
            if (issueWord == "unissue" && wasIssued)
            {
                try
                {
                    target.Issued = false;
                    if (!target.Issued) done.Add("un-issued");
                    else missed.Add("un-issue (it still reads back as issued)");
                }
                catch (Exception failure) { missed.Add("un-issue (" + revitSaid(failure) + ")"); }
            }

            // 2. The text columns.
            if (missed.Count == 0)
                foreach (var field in textFields)
                {
                    if (field.Wanted == null) continue;
                    var before = readText(field.Label) ?? "";
                    if (before == field.Wanted) continue;
                    try { writeText(field.Label, field.Wanted); }
                    catch (Exception failure) { missed.Add(field.Label + " (" + revitSaid(failure) + ")"); break; }
                    var after = readText(field.Label) ?? "";
                    if (after == field.Wanted) done.Add(field.Label + " " + quoted(before) + " -> " + quoted(after));
                    else { missed.Add(field.Label + " reads back " + quoted(after)); break; }
                }

            // 3. Numbering.
            if (missed.Count == 0 && numberingValue != null)
            {
                var before = currentNumbering(target);
                if (before == null || !before.Equals(numberingValue))
                {
                    try
                    {
                        if (sequenceIdProperty != null) sequenceIdProperty.SetValue(target, numberingValue, null);
                        else numberTypeProperty.SetValue(target, numberingValue, null);
                        var after = currentNumbering(target);
                        if (after != null && after.Equals(numberingValue))
                            done.Add("numbering " + numberingLabel(before) + " -> " + numberingLabel(after));
                        else missed.Add("numbering reads back " + numberingLabel(after));
                    }
                    catch (Exception failure) { missed.Add("numbering " + numberingName + " (" + revitSaid(failure) + ")"); }
                }
            }

            // 4. Show - the one column an issued revision still takes.
            if (missed.Count == 0 && wantedShow != null && target.Visibility != wantedShow.Value)
            {
                var before = target.Visibility;
                try
                {
                    target.Visibility = wantedShow.Value;
                    if (target.Visibility == wantedShow.Value)
                        done.Add("show " + showName(before) + " -> " + showName(target.Visibility));
                    else missed.Add("show reads back " + showName(target.Visibility));
                }
                catch (Exception failure) { missed.Add("show (" + revitSaid(failure) + ")"); }
            }

            // 5. ISSUE LAST, when asked: it locks everything above.
            if (missed.Count == 0 && issueWord == "issue" && !target.Issued)
            {
                try
                {
                    target.Issued = true;
                    if (target.Issued) done.Add("issued");
                    else missed.Add("issue (it still reads back as not issued)");
                }
                catch (Exception failure) { missed.Add("issue (" + revitSaid(failure) + ")"); }
            }

            if (missed.Count > 0 && done.Count > 0)
                throw new InvalidOperationException(
                    "EDIT_REVISION on " + revisionName + " would have been half done, so nothing is "
                    + "kept: " + string.Join("; ", done.ToArray()) + " went in, then "
                    + string.Join("; ", missed.ToArray()) + ".");

            if (missed.Count > 0)
                refused = "Revit would not change " + revisionName + ": "
                        + string.Join("; ", missed.ToArray()) + ". Nothing was changed. It would have: "
                        + string.Join("; ", plan.ToArray()) + ".";

            changed = string.Join("; ", done.ToArray());
        }

        alreadyThat = string.Join("; ", already.ToArray());

        try { nowIssued = target.Issued; } catch (Exception failure) { findings = "issued unreadable: " + revitSaid(failure) + ". "; }

        findings += "Read back: Seq. " + target.SequenceNumber
                  + ": numbering " + numberingLabel(currentNumbering(target))
                  + ", date " + quoted(target.RevisionDate)
                  + ", description " + quoted(target.Description)
                  + ", " + (target.Issued ? "ISSUED" : "not issued")
                  + ", issued to " + quoted(target.IssuedTo)
                  + ", issued by " + quoted(target.IssuedBy)
                  + ", show " + showName(target.Visibility)
                  + " (id " + target.Id + ")";
    }
}
