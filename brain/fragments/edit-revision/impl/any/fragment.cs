// NOT STANDALONE. Assumes `doc`, `elements`, `confirmCount`, `description`,
// `revisionDate`, `issuedBy`, `issuedTo`, `show`, `numbering` and `issueState`
// are in scope, and leaves `changed`, `alreadyThat`, `nowIssued`,
// `revisionName`, `findings` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THE REVISIONS ARE HANDED IN BY A FIND - NEVER THE SELECTION, NEVER A GUESS.
//
// They arrive from the fragment before this one - SELECT_BY_PARAMETER_VALUE on
// the Revisions category, by `Revision Sequence` (the number the Sheet
// Issues/Revisions dialog shows, unique by construction) or by `Revision
// Description`. ONE revision needs nothing more. SEVERAL are changed only when
// `confirmCount` says how many. Descriptions repeat across a job - two issues
// called "For Construction" is normal - so a find that brings back more than
// the caller said, or several with no `confirmCount` at all, is refused with
// every one named, rather than editing whatever it caught on documents about
// to go out.
//
// VERSION 3, 2026-10-10: SEVERAL REVISIONS IN ONE CALL. Seven "IFI - Issued
// for Information" revisions on a Revit 2020 job had to be issued, and later
// un-issued and renumbered. Version 2 took one per call - a find and an edit
// for each, fourteen calls a pass and seven entries in the undo list. Now the
// whole set is one call, one undo entry (the add-in's TransactionGroup), and
// all or nothing.
//
// THE ORDER ACROSS THE SET IS FIXED, because one revision's write can be
// refused for the state of another: un-issue every one asked, from the LAST
// sequence to the first; then the text columns; then numbering, last to first;
// then show; then issue every one asked, first to last. Those are the orders
// Revit took on 2026-10-10 one revision per call (version 2, Revit 2020): issued
// Seq. 1 up to Seq. 7, then un-issued and renumbered Seq. 7 down to Seq. 1 -
// after un-issuing and renumbering Seq. 1 FIRST was refused because Seq. 2 to
// Seq. 7 were still issued. So the renumbering check below judges each
// revision against the state the set will be in when numbering is written - a
// revision of the set asked to un-issue counts as un-issued.
//
// AN ISSUED REVISION IS LOCKED, AND THE REFUSAL NAMES IT. Revit refuses to
// change the description, date, issued to, issued by or numbering of an
// issued revision (its own API documentation says so, per property). Asked
// for any of those on an issued one, this changes NOTHING - on any revision of
// the set - and says which revisions and which fields. `issueState=unissue` in
// the same call un-issues them first, because that is the explicit decision
// Revit's lock is asking for. Show is the one column Revit leaves open on an
// issued revision, and so does this.
//
// THE ISSUED TICK MOVES ONLY WHEN `issueState` SAYS SO.
//
// EMPTY MEANS LEAVE ALONE; THE WORD <blank> WRITES AN EMPTY VALUE. The values
// given go to every revision of the set alike.
//
// A NUMBERING CHANGE RENUMBERS THE REVISIONS AFTER IT. Revit numbers the
// revisions on one numbering sequence one after another, in sequence order, so
// moving a revision off a sequence (or onto one) changes the number every LATER
// revision on either sequence prints. Where one of those is ISSUED when
// numbering is written, the change is refused, naming it - the same rule
// REORDER_REVISION keeps. Found by review before merge, 2026-09-28: Seq. 1 moved
// to 'Custom' made an issued Seq. 2 print 1 instead of 2, and nothing said so.
//
// EVERY FIELD IS COMPARED BEFORE IT IS WRITTEN AND READ BACK AFTER. A field
// already holding the wanted value is named in `alreadyThat` and not written,
// so a second identical run changes nothing and says so. A write Revit took
// but that reads back different THROWS, and so does any refusal after another
// write went in, on this revision or another - the add-in rolls the whole call
// back, never half a set and never a stored value under a reply saying nothing
// changed. Only when Revit refused the very first write, so nothing was stored,
// is it a refusal carrying Revit's own words and what the call would have done
// - which is what makes a run with no transaction open a measurement.
//
// ONE STRING PER RESULT, revisions apart by " || " as LIST_REVISIONS does: the
// add-in's reply prints a string whole and cuts a list to its first three.

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
    Func<Revision, string> nameOf = r => "Seq. " + r.SequenceNumber + " " + quoted(r.Description);

    var problems = new List<string>();

    // ---- which revisions -------------------------------------------------------
    var targets = new List<Revision>();
    var notRevisions = 0;
    if (elements != null)
        foreach (var element in elements)
        {
            var candidate = element as Revision;
            if (candidate == null)
            {
                if (element != null) notRevisions++;
                continue;
            }
            // The same revision handed in twice is one revision.
            if (!targets.Any(t => t.Id == candidate.Id)) targets.Add(candidate);
        }
    // The dialog's order, which is the order Revit numbers them in.
    targets.Sort((a, b) => a.SequenceNumber.CompareTo(b.SequenceNumber));

    var labels = new List<string>();
    foreach (var target in targets) labels.Add(nameOf(target));
    var listed = string.Join(", ", labels.ToArray());

    // ---- how many the caller said it means ---------------------------------------
    var countWord = (confirmCount ?? "").Trim();
    var confirmed = 0;
    var countUnreadable = false;
    if (countWord.Length > 0)
    {
        int whole;
        if (int.TryParse(countWord, System.Globalization.NumberStyles.Integer,
                         System.Globalization.CultureInfo.InvariantCulture, out whole) && whole > 0)
            confirmed = whole;
        else
            countUnreadable = true;
    }

    if (targets.Count == 0)
        problems.Add("no revision was handed in" + (notRevisions > 0
            ? " - " + notRevisions + " element(s) came in and none of them is a revision" : "")
            + ". Find it first with SELECT_BY_PARAMETER_VALUE, categories=Revisions, "
            + "parameterName=Revision Sequence (the number the dialog shows) or Revision Description");
    else if (countUnreadable)
        problems.Add("confirmCount takes how many revisions the find brought back, as a whole number, not '"
                   + confirmCount + "'. It brought back " + targets.Count + " (" + listed + "): check these are "
                   + "the ones meant, then give confirmCount=" + targets.Count);
    else if (confirmed > 0 && confirmed != targets.Count)
        problems.Add("the find brought back " + targets.Count + " revision(s) (" + listed + ") and confirmCount says "
                   + confirmed + " - check the find caught exactly the revisions meant, and give the number it "
                   + "brought back");
    else if (countWord.Length == 0 && targets.Count > 1)
        problems.Add(targets.Count + " revisions were handed in (" + listed + "). More than one is changed only "
                   + "when confirmCount says how many: check these are the ones meant, then give confirmCount="
                   + targets.Count + " - or find one by Revision Sequence, which is unique");

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
        var many = targets.Count > 1;
        revisionName = many ? targets.Count + " revisions: " + listed : labels[0];
        // `confirmCount` counts REVISIONS, so anything else the find caught is
        // said rather than dropped without a word.
        if (many && notRevisions > 0)
            findings = notRevisions + " element(s) that are not revisions came in and were left alone. ";
        // Whose line this is - said only when there is more than one.
        Func<int, string> who = i => many ? labels[i] + ": " : "";
        // One string for the whole set: a revision's items apart by "; ",
        // revisions apart by " || ", a revision with nothing to say left out.
        Func<List<List<string>>, string> perRevision = lists =>
        {
            if (!many) return string.Join("; ", lists[0].ToArray());
            var parts = new List<string>();
            for (var i = 0; i < lists.Count; i++)
                if (lists[i].Count > 0) parts.Add(labels[i] + ": " + string.Join("; ", lists[i].ToArray()));
            return string.Join(" || ", parts.ToArray());
        };

        Func<Revision, string, string> readText = (r, label) =>
            label == "description" ? r.Description
            : label == "date" ? r.RevisionDate
            : label == "issued by" ? r.IssuedBy
            : r.IssuedTo;
        Action<Revision, string, string> writeText = (r, label, value) =>
        {
            if (label == "description") r.Description = value;
            else if (label == "date") r.RevisionDate = value;
            else if (label == "issued by") r.IssuedBy = value;
            else r.IssuedTo = value;
        };

        // WHAT WOULD MOVE, revision by revision, worked out before anything does.
        var plans = new List<List<string>>();
        var alreadies = new List<List<string>>();
        var lockedAsked = new List<List<string>>();
        var wasIssued = new List<bool>();
        foreach (var target in targets)
        {
            var plan = new List<string>();
            var already = new List<string>();
            var locked = new List<string>();
            foreach (var field in textFields)
            {
                if (field.Wanted == null) continue;
                var now = readText(target, field.Label) ?? "";
                if (now == field.Wanted) { already.Add(field.Label + " " + quoted(now)); continue; }
                plan.Add(field.Label + " " + quoted(now) + " -> " + quoted(field.Wanted));
                locked.Add(field.Label);
            }
            if (numberingValue != null)
            {
                var nowNumbering = currentNumbering(target);
                if (nowNumbering != null && nowNumbering.Equals(numberingValue))
                    already.Add("numbering " + numberingName);
                else
                {
                    plan.Add("numbering " + numberingLabel(nowNumbering) + " -> " + numberingName);
                    locked.Add("numbering");
                }
            }
            if (wantedShow != null)
            {
                if (target.Visibility == wantedShow.Value) already.Add("show " + showName(target.Visibility));
                else plan.Add("show " + showName(target.Visibility) + " -> " + showName(wantedShow.Value));
            }
            if (issueWord == "issue")
            {
                if (target.Issued) already.Add("issued");
                else plan.Add("then issued");
            }
            else if (issueWord == "unissue")
            {
                if (!target.Issued) already.Add("not issued");
                else plan.Insert(0, "un-issued first");
            }
            plans.Add(plan);
            alreadies.Add(already);
            lockedAsked.Add(locked);
            wasIssued.Add(target.Issued);
        }

        // HOW ISSUED A REVISION WILL BE WHEN NUMBERING IS WRITTEN: one of the set
        // asked to un-issue is un-issued by then; one asked to issue is not yet.
        Func<Revision, bool> issuedWhenNumbered = r =>
            targets.Any(t => t.Id == r.Id) && issueWord == "unissue" ? false : r.Issued;

        // WHY ANY REVISION OF THE SET CANNOT TAKE THIS - the lock first, then the
        // issued revisions a numbering change would renumber: every later one on
        // the sequence it leaves or the one it joins.
        var reasons = new List<string>();
        var anyLock = false;
        IList<ElementId> inOrder = null;
        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            if (wasIssued[i] && issueWord != "unissue" && lockedAsked[i].Count > 0)
            {
                anyLock = true;
                reasons.Add(labels[i] + " is ISSUED, and Revit locks the "
                          + string.Join(", ", lockedAsked[i].ToArray()) + " of an issued revision");
                continue;
            }
            if (numberingValue == null) continue;
            var numberingBefore = currentNumbering(target);
            if (numberingBefore != null && numberingBefore.Equals(numberingValue)) continue;

            if (inOrder == null) inOrder = Revision.GetAllRevisionIds(doc);
            var renumberedIssued = new List<string>();
            var pastTarget = false;
            foreach (var id in inOrder)
            {
                if (id == target.Id) { pastTarget = true; continue; }
                if (!pastTarget) continue;
                var later = doc.GetElement(id) as Revision;
                if (later == null || !issuedWhenNumbered(later)) continue;
                var scheme = currentNumbering(later);
                if (scheme != null && (scheme.Equals(numberingBefore) || scheme.Equals(numberingValue)))
                    renumberedIssued.Add(nameOf(later) + " (" + numberingLabel(scheme) + ")");
            }
            if (renumberedIssued.Count > 0)
                reasons.Add("Moving " + labels[i] + " from numbering " + numberingLabel(numberingBefore)
                          + " to " + numberingName + " would renumber " + string.Join(", ", renumberedIssued.ToArray())
                          + ", which " + (renumberedIssued.Count == 1 ? "is" : "are") + " ISSUED - that number is "
                          + "printed on drawings already sent out");
        }

        if (reasons.Count > 0)
        {
            // NOTHING IS ATTEMPTED, on any revision: Revit would refuse the locked
            // columns, and a set half changed is worse than none.
            if (!many)
                refused = reasons[0] + ". Nothing was changed."
                        + (anyLock ? " Un-issuing it first (issueState=unissue) is the owner's decision to "
                                   + "take, because the issue it records has gone out." : "");
            else
                refused = reasons.Count + " of the " + targets.Count + " revisions cannot take this, so none of "
                        + "them was changed: " + string.Join("; ", reasons.ToArray()) + "."
                        + (anyLock ? " Un-issuing them first (issueState=unissue) is the owner's decision to "
                                   + "take, because the issues they record have gone out." : "");
        }
        else
        {
            var done = new List<List<string>>();
            foreach (var target in targets) done.Add(new List<string>());
            var missed = new List<string>();
            // A WRITE REVIT TOOK THAT READS BACK DIFFERENT. It is stored, so it
            // is never reported as "nothing changed" - it throws below. Only the
            // WRITE sits in each try: a read-back that throws after Revit took the
            // value is not caught here, so it ends the call and the add-in rolls
            // everything back, rather than reporting a stored value as refused.
            var storedWrong = false;

            // 1. UN-ISSUE FIRST, every one asked, from the LAST sequence to the
            //    first: every locked column needs it, and one still issued
            //    blocks renumbering the revisions before it.
            if (issueWord == "unissue")
                for (var i = targets.Count - 1; i >= 0 && missed.Count == 0; i--)
                {
                    var target = targets[i];
                    if (!wasIssued[i]) continue;
                    try { target.Issued = false; }
                    catch (Exception failure) { missed.Add(who(i) + "un-issue (" + revitSaid(failure) + ")"); continue; }
                    if (!target.Issued) done[i].Add("un-issued");
                    else { storedWrong = true; missed.Add(who(i) + "un-issue (it still reads back as issued)"); }
                }

            // 2. The text columns, every revision.
            for (var i = 0; i < targets.Count && missed.Count == 0; i++)
            {
                var target = targets[i];
                foreach (var field in textFields)
                {
                    if (field.Wanted == null) continue;
                    var before = readText(target, field.Label) ?? "";
                    if (before == field.Wanted) continue;
                    try { writeText(target, field.Label, field.Wanted); }
                    catch (Exception failure) { missed.Add(who(i) + field.Label + " (" + revitSaid(failure) + ")"); break; }
                    var after = readText(target, field.Label) ?? "";
                    if (after == field.Wanted) done[i].Add(field.Label + " " + quoted(before) + " -> " + quoted(after));
                    else { storedWrong = true; missed.Add(who(i) + field.Label + " reads back " + quoted(after)); break; }
                }
            }

            // 3. Numbering, from the LAST sequence to the first.
            if (numberingValue != null)
                for (var i = targets.Count - 1; i >= 0 && missed.Count == 0; i--)
                {
                    var target = targets[i];
                    var before = currentNumbering(target);
                    if (before != null && before.Equals(numberingValue)) continue;
                    try
                    {
                        if (sequenceIdProperty != null) sequenceIdProperty.SetValue(target, numberingValue, null);
                        else numberTypeProperty.SetValue(target, numberingValue, null);
                    }
                    catch (Exception failure) { missed.Add(who(i) + "numbering " + numberingName + " (" + revitSaid(failure) + ")"); continue; }
                    var after = currentNumbering(target);
                    if (after != null && after.Equals(numberingValue))
                        done[i].Add("numbering " + numberingLabel(before) + " -> " + numberingLabel(after));
                    else { storedWrong = true; missed.Add(who(i) + "numbering reads back " + numberingLabel(after)); }
                }

            // 4. Show - the one column an issued revision still takes.
            if (wantedShow != null)
                for (var i = 0; i < targets.Count && missed.Count == 0; i++)
                {
                    var target = targets[i];
                    if (target.Visibility == wantedShow.Value) continue;
                    var before = target.Visibility;
                    try { target.Visibility = wantedShow.Value; }
                    catch (Exception failure) { missed.Add(who(i) + "show (" + revitSaid(failure) + ")"); continue; }
                    if (target.Visibility == wantedShow.Value)
                        done[i].Add("show " + showName(before) + " -> " + showName(target.Visibility));
                    else { storedWrong = true; missed.Add(who(i) + "show reads back " + showName(target.Visibility)); }
                }

            // 5. ISSUE LAST, every one asked: it locks everything above.
            if (issueWord == "issue")
                for (var i = 0; i < targets.Count && missed.Count == 0; i++)
                {
                    var target = targets[i];
                    if (target.Issued) continue;
                    try { target.Issued = true; }
                    catch (Exception failure) { missed.Add(who(i) + "issue (" + revitSaid(failure) + ")"); continue; }
                    if (target.Issued) done[i].Add("issued");
                    else { storedWrong = true; missed.Add(who(i) + "issue (it still reads back as not issued)"); }
                }

            var anyDone = done.Any(d => d.Count > 0);
            if (missed.Count > 0 && (anyDone || storedWrong))
                throw new InvalidOperationException(
                    "EDIT_REVISION on " + revisionName + " did not read back as asked, so nothing is "
                    + "kept: " + (anyDone ? perRevision(done) + " went in, then " : "")
                    + string.Join("; ", missed.ToArray()) + ".");

            if (missed.Count > 0)
                refused = "Revit would not change " + revisionName + ": "
                        + string.Join("; ", missed.ToArray()) + ". Nothing was changed. It would have: "
                        + perRevision(plans) + ".";

            changed = perRevision(done);
        }

        alreadyThat = perRevision(alreadies);

        // THE WHOLE ROW OF EVERY REVISION, READ BACK. `nowIssued` is true only
        // when every one of them is issued.
        var readBack = new List<string>();
        var allIssued = true;
        for (var i = 0; i < targets.Count; i++)
        {
            var target = targets[i];
            try { if (!target.Issued) allIssued = false; }
            catch (Exception failure)
            {
                allIssued = false;
                findings += who(i) + "issued unreadable: " + revitSaid(failure) + ". ";
            }
            readBack.Add("Seq. " + target.SequenceNumber
                       + ": numbering " + numberingLabel(currentNumbering(target))
                       + ", date " + quoted(target.RevisionDate)
                       + ", description " + quoted(target.Description)
                       + ", " + (target.Issued ? "ISSUED" : "not issued")
                       + ", issued to " + quoted(target.IssuedTo)
                       + ", issued by " + quoted(target.IssuedBy)
                       + ", show " + showName(target.Visibility)
                       + " (id " + target.Id + ")");
        }
        nowIssued = allIssued;
        findings += "Read back: " + string.Join(" || ", readBack.ToArray());
    }
}
