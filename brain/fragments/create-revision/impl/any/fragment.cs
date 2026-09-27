// NOT STANDALONE. Assumes `doc`, `description`, `revisionDate`, `issuedBy`,
// `issuedTo`, `show`, `numbering` and `issueState` are in scope; leaves
// `created`, `refused`, `sequence` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16) and does not open one.
//
// IT FILLS EVERY COLUMN OF THE SHEET ISSUES/REVISIONS ROW IT ADDS, and an
// empty value leaves Revit's own default. The word <blank> writes an empty
// value on purpose - an empty field in a chat is otherwise indistinguishable
// from one nobody mentioned.
//
// `Issued` IS SET ONLY WHEN ASKED, BY ITS OWN VALUE, AND LAST. Once a
// revision is issued Revit locks its description, date, issued to, issued by
// and numbering, and refuses to put any new cloud on it. Creating one already
// issued makes a revision nobody can cloud - right for recording an issue that
// has already gone out, wrong by default. So it is never implied by any other
// value, and every other column is written before it.
//
// EVERYTHING IS CHECKED BEFORE ANYTHING IS MADE. A misspelt show word or a
// numbering sequence the project does not have refuses with nothing created.
//
// EVERY COLUMN IS READ BACK. A value that does not read back after the
// revision exists means a half-made row, and a half-made row is not left in
// the model: the fragment throws, and the add-in rolls the whole group back.
//
// THE DATE IS NOT PARSED OR REFORMATTED. RevisionDate is free text in Revit and
// goes straight onto the titleblock, so the project's own convention is the
// only one that matters - "02/09/26" and "2 Sep 2026" are both correct
// somewhere, and a fragment that tidied either into the other would change what
// a signed drawing says.
//
// THE NUMBERING API IS NOT THE SAME ON EVERY RELEASE. 2022 on, a revision
// points at a named numbering sequence (`RevisionNumberingSequenceId`); 2020
// and 2021 say Numeric, Alphanumeric or None (`NumberType`, gone from 2023).
// The add-in compiles fragments with no release symbol defined, so both are
// reached by name and the one this Revit has is used.
//
// THE SEQUENCE NUMBER IS READ BACK, NOT ASSUMED. Revit assigns it, and it is
// what the titleblock's revision schedule sorts on - so the caller gets told
// which position this revision actually took rather than being left to count.

ElementId created = null;
string refused = null;
var sequence = 0;
var findings = "";

{
    Func<string, string> asked = given =>
    {
        var typed = (given ?? "").Trim();
        if (typed.Length == 0) return null;
        if (string.Equals(typed, "<blank>", StringComparison.OrdinalIgnoreCase)) return "";
        return typed;
    };
    Func<string, string> quoted = value => string.IsNullOrEmpty(value) ? "(blank)" : "'" + value + "'";
    Func<Exception, string> revitSaid = failure =>
        (failure.InnerException ?? failure).Message.TrimEnd('.');

    var problems = new List<string>();

    var descriptionText = (description ?? "").Trim();
    if (descriptionText.Length == 0 || string.Equals(descriptionText, "<blank>", StringComparison.OrdinalIgnoreCase))
    {
        // An unlabelled row on a signed drawing. Revit allows it; a drawing office
        // does not.
        problems.Add("a revision needs a description - it is the line that appears in the "
                   + "titleblock's revision schedule, and an empty one is a row nobody can read");
    }

    // ---- show: the dialog's Show column --------------------------------------
    RevisionVisibility? wantedShow = null;
    var showWord = (show ?? "").Trim().ToLowerInvariant().Replace("&", "and").Replace(" ", "");
    if (showWord.Length > 0)
    {
        if (showWord == "cloudandtag") wantedShow = RevisionVisibility.CloudAndTagVisible;
        else if (showWord == "tag") wantedShow = RevisionVisibility.TagVisible;
        else if (showWord == "none" || showWord == "hidden") wantedShow = RevisionVisibility.Hidden;
        else problems.Add("show takes 'cloud and tag', 'tag' or 'none' - the three choices of the "
                        + "dialog's Show column - not '" + show + "'");
    }

    // ---- issued: its own value, never implied ---------------------------------
    var issueWord = (issueState ?? "").Trim().ToLowerInvariant();
    var issueIt = issueWord == "issue";
    if (issueWord.Length > 0 && !issueIt)
        problems.Add("issueState takes 'issue' or nothing - a new revision is not issued unless "
                   + "that is asked for in so many words - not '" + issueState + "'");

    // ---- numbering: a sequence by name (2022 on) or a kind (2020, 2021) ------
    var revisionClass = typeof(Revision);
    var sequenceIdProperty = revisionClass.GetProperty("RevisionNumberingSequenceId");   // 2022 on
    var numberTypeProperty = revisionClass.GetProperty("NumberType");                    // 2020 to 2022
    var sequenceClass = revisionClass.Assembly.GetType(
        revisionClass.Namespace + ".RevisionNumberingSequence");                         // 2022 on

    object numberingValue = null;       // an ElementId on 2022 on, a RevisionNumberType before
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
                    var candidate = doc.GetElement(id);
                    if (candidate == null) continue;
                    names.Add(candidate.Name);
                    if (string.Equals(candidate.Name, wantedNumbering, StringComparison.OrdinalIgnoreCase))
                    {
                        numberingValue = candidate.Id;
                        numberingName = candidate.Name;
                    }
                }
            if (numberingValue == null && string.Equals(wantedNumbering, "none", StringComparison.OrdinalIgnoreCase))
            {
                // NO SEQUENCE. Whether this Revit accepts that is Revit's to say,
                // and the read-back below reports it either way.
                numberingValue = ElementId.InvalidElementId;
                numberingName = "None";
            }
            if (numberingValue == null)
                problems.Add("no numbering sequence called '" + wantedNumbering + "' in " + doc.Title
                           + " - it has " + (names.Count == 0 ? "none" : string.Join(", ", names.ToArray()))
                           + ". CREATE_REVISION_NUMBERING_SEQUENCE makes a new one");
        }
        else if (numberTypeProperty != null)
        {
            var kind = wantedNumbering.ToLowerInvariant();
            if (kind == "numeric") numberingValue = RevisionNumberType.Numeric;
            else if (kind == "alphanumeric") numberingValue = RevisionNumberType.Alphanumeric;
            else if (kind == "none") numberingValue = RevisionNumberType.None;
            else problems.Add("this Revit has no named numbering sequences - it numbers a revision "
                            + "'numeric', 'alphanumeric' or 'none', not '" + wantedNumbering + "'");
            if (numberingValue != null) numberingName = numberingValue.ToString();
        }
        else
        {
            problems.Add("this Revit exposes no way to choose a revision's numbering");
        }
    }

    if (problems.Count > 0)
    {
        refused = string.Join("; ", problems.ToArray()) + ". Nothing was created.";
    }
    else
    {
        Revision revision = null;
        try { revision = Revision.Create(doc); }
        catch (Exception failure)
        {
            refused = "Revit would not create the revision: " + revitSaid(failure)
                    + ". Nothing was created. It would have been: description " + quoted(descriptionText)
                    + (asked(revisionDate) != null ? ", date " + quoted(asked(revisionDate)) : "")
                    + (asked(issuedBy) != null ? ", issued by " + quoted(asked(issuedBy)) : "")
                    + (asked(issuedTo) != null ? ", issued to " + quoted(asked(issuedTo)) : "")
                    + (wantedShow != null ? ", show " + show.Trim() : "")
                    + (numberingName.Length > 0 ? ", numbering " + numberingName : "")
                    + (issueIt ? ", then issued" : "");
        }

        if (revision == null && refused == null)
            refused = "Revit declined to create the revision. Nothing was created.";

        if (revision != null)
        {
            // A HALF-MADE ROW IS NOT LEFT BEHIND. The revision exists now, so any
            // column that will not take its value throws, and the add-in rolls the
            // group back with Revit's own words in the message.
            var missed = new List<string>();
            Action<string, string, Func<string>, Action<string>> write = (label, wanted, read, put) =>
            {
                if (wanted == null) return;
                try { put(wanted); }
                catch (Exception failure) { missed.Add(label + " (" + revitSaid(failure) + ")"); return; }
                var now = "";
                try { now = read() ?? ""; } catch (Exception failure) { now = "unreadable: " + revitSaid(failure); }
                if (now != wanted) missed.Add(label + " reads back " + quoted(now) + ", not " + quoted(wanted));
            };

            write("description", descriptionText, () => revision.Description, v => revision.Description = v);
            write("date", asked(revisionDate), () => revision.RevisionDate, v => revision.RevisionDate = v);
            write("issued by", asked(issuedBy), () => revision.IssuedBy, v => revision.IssuedBy = v);
            write("issued to", asked(issuedTo), () => revision.IssuedTo, v => revision.IssuedTo = v);

            if (wantedShow != null)
            {
                try
                {
                    revision.Visibility = wantedShow.Value;
                    if (revision.Visibility != wantedShow.Value)
                        missed.Add("show reads back " + revision.Visibility);
                }
                catch (Exception failure) { missed.Add("show (" + revitSaid(failure) + ")"); }
            }

            if (numberingValue != null)
            {
                try
                {
                    if (sequenceIdProperty != null)
                    {
                        sequenceIdProperty.SetValue(revision, numberingValue, null);
                        var now = sequenceIdProperty.GetValue(revision, null) as ElementId;
                        if (now == null || !now.Equals(numberingValue))
                            missed.Add("numbering reads back a different sequence");
                    }
                    else
                    {
                        numberTypeProperty.SetValue(revision, numberingValue, null);
                        var now = numberTypeProperty.GetValue(revision, null);
                        if (now == null || !now.Equals(numberingValue))
                            missed.Add("numbering reads back " + now);
                    }
                }
                catch (Exception failure) { missed.Add("numbering " + numberingName + " (" + revitSaid(failure) + ")"); }
            }

            // LAST, and only when asked: issuing locks every column above.
            if (issueIt && missed.Count == 0)
            {
                try
                {
                    revision.Issued = true;
                    if (!revision.Issued) missed.Add("issued reads back as not issued");
                }
                catch (Exception failure) { missed.Add("issued (" + revitSaid(failure) + ")"); }
            }

            if (missed.Count > 0)
                throw new InvalidOperationException(
                    "The new revision would not take every value, so nothing is kept: "
                    + string.Join("; ", missed.ToArray()) + ".");

            created = revision.Id;

            // Where it landed in the sequence, read from Revit rather than counted
            // here: the order is what the revision schedule sorts on.
            var all = Revision.GetAllRevisionIds(doc);
            for (var i = 0; i < all.Count; i++)
            {
                if (all[i] == revision.Id) { sequence = i + 1; break; }
            }

            string shownAs;
            switch (revision.Visibility)
            {
                case RevisionVisibility.CloudAndTagVisible: shownAs = "Cloud and Tag"; break;
                case RevisionVisibility.TagVisible: shownAs = "Tag"; break;
                default: shownAs = "None"; break;
            }
            string scheme = "";
            try
            {
                if (sequenceIdProperty != null)
                {
                    var id = sequenceIdProperty.GetValue(revision, null) as ElementId;
                    var element = id == null ? null : doc.GetElement(id);
                    scheme = element == null ? "None" : element.Name;
                }
                else if (numberTypeProperty != null)
                    scheme = "" + numberTypeProperty.GetValue(revision, null);
            }
            catch (Exception failure) { scheme = "unreadable (" + revitSaid(failure) + ")"; }

            findings = "Read back: Seq. " + revision.SequenceNumber
                     + ": numbering " + scheme
                     + ", date " + quoted(revision.RevisionDate)
                     + ", description " + quoted(revision.Description)
                     + ", " + (revision.Issued ? "ISSUED" : "not issued")
                     + ", issued to " + quoted(revision.IssuedTo)
                     + ", issued by " + quoted(revision.IssuedBy)
                     + ", show " + shownAs
                     + " (id " + revision.Id + ")";
        }
    }
}
