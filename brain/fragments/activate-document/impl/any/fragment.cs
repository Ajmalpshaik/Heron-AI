// NOT STANDALONE. Assumes `doc`, `uidoc` and `documentPath` are in scope;
// leaves `activated`, `activeAfter`, `wasAlready`, `leftBehindWritten`,
// `problem` and `findings` behind.
//
// IT CANNOT RUN INSIDE A TRANSACTION, which is why it is EXECUTE. Activating
// a document while the current one is modifiable is refused.
//
// IT EXISTS BECAUSE THERE WAS NO WAY BACK OUT OF A FAMILY.
// SWITCH_ACTIVE_PROJECT moves between open PROJECTS and refuses from inside a
// family: "Changing the active view is not applicable to inactive documents."
// So opening a family stranded every fragment after it - and one looking for
// loaded families found none, because a family document holds none. Measured
// 2026-09-16: five "opens" failed and five size tables were written into the
// one family that happened to be in front.
//
// activeAfter IS READ BACK FROM REVIT, NEVER ASSUMED FROM THE PATH. Believing
// a switch happened when it did not is exactly how those five tables landed
// in the wrong family, and an argument echoed back would have hidden it.
//
// VERSION 2, 2026-10-07 - "ALREADY IN FRONT" IS JUDGED BY THE WINDOW IN FRONT,
// BY ITS FILE. Version 1 compared the title of `doc` - the document Heron was
// pointed at, which need not be in front - with the file name asked for. Aimed
// at a car family behind a tyre family and asked for the car's file, it said
// "already in front" and switched nothing: the very failure this exists to
// prevent (row 5b-359). Two files of one name in two folders matched as well.
//
// VERSION 3, 2026-11-18 - THE DOCUMENT LEFT BEHIND IS READ BEFORE AND AFTER.
// On Revit 2020 the document in front was WRITTEN TO ITS FILE within about a
// second of this card bringing another document forward, with no save asked -
// each time the document brought forward already had a window (row 5b-1851).
// This card calls no save, and the trace found nothing of Heron's around it
// that does - so whatever writes it, the reply must say so rather than leave
// the modeller to find it on disk. Whether the document in front
// had unsaved changes, and its file's time, are read before the call and
// again after it. A file whose time moved is `leftBehindWritten` and a
// finding in the modeller's words - and unsaved changes gone count only when
// that time cannot be read, so a write is said only where the file shows it.
// Unsaved changes still there are said too, with a caution when the document
// brought forward was already open in a window, because a write that comes a
// moment after the job ends is one no read here can see.
// Reads only - nothing here saves, closes or changes anything.

var findings = new List<string>();
var activated = false;
var activeAfter = "";
var wasAlready = false;
var leftBehindWritten = false;
var problem = "";

var path = (documentPath ?? "").Trim();

if (path.Length == 0)
{
    findings.Add("No document path was given.");
}
else if (!System.IO.File.Exists(path))
{
    // Checked separately: Revit's failure for a missing file and its failure
    // for a file it will not open read the same from here, and they need
    // opposite things done about them.
    findings.Add(string.Format("There is no file at \"{0}\".", path));
}
else
{
    // THE WINDOW IN FRONT, asked of Revit - not `doc`, which is only the
    // document this run was aimed at.
    Document inFront = null;
    try
    {
        var active = uidoc == null ? null : uidoc.Application.ActiveUIDocument;
        inFront = active == null ? null : active.Document;
    }
    catch (Exception) { inFront = null; }

    var before = "";
    var frontPath = "";
    try
    {
        before = inFront == null ? "" : inFront.Title;
        frontPath = inFront == null ? "" : inFront.PathName;
    }
    catch (Exception) { before = ""; frontPath = ""; }

    Func<string, string> canonical = delegate (string p)
    {
        if (string.IsNullOrEmpty(p)) return "";
        try { return System.IO.Path.GetFullPath(p).TrimEnd('\\', '/'); }
        catch (Exception) { return p.Trim(); }
    };

    // ALREADY IN FRONT IS NOT A FAILURE, and not a reason to reopen either -
    // reopening would be a real action with real risk, taken for nothing.
    if (frontPath.Length > 0
        && string.Equals(canonical(frontPath), canonical(path), StringComparison.OrdinalIgnoreCase))
    {
        wasAlready = true;
        activated = true;
        activeAfter = before;
        findings.Add(string.Format("'{0}' was already the document in front - nothing was "
            + "changed.", before));
    }
    else
    {
        // VERSION 3: THE DOCUMENT THAT WILL BE LEFT BEHIND, BEFORE THE CALL -
        // whether it holds unsaved changes, and when its file was last written
        // (UTC, compared; shown in local time, as Explorer shows it). A read
        // that fails is "not known", never "nothing changed". And whether the
        // file asked for is already open in a window: the switch the write was
        // seen after, 3 times out of 3.
        var dirtyBefore = false;
        var dirtyBeforeKnown = false;
        DateTime? stampBefore = null;
        if (inFront != null)
        {
            try { dirtyBefore = inFront.IsModified; dirtyBeforeKnown = true; }
            catch (Exception) { dirtyBeforeKnown = false; }
            try
            {
                if (frontPath.Length > 0 && System.IO.File.Exists(frontPath))
                    stampBefore = System.IO.File.GetLastWriteTimeUtc(frontPath);
            }
            catch (Exception) { stampBefore = null; }
        }

        var targetInAWindow = false;
        try
        {
            foreach (Document open in doc.Application.Documents)
            {
                if (open == null || open.IsLinked) continue;
                var here = "";
                try { here = open.PathName ?? ""; }
                catch (Exception) { here = ""; }
                if (here.Length == 0 || !string.Equals(canonical(here), canonical(path),
                                                       StringComparison.OrdinalIgnoreCase))
                    continue;
                // Windows that cannot be counted are counted as one: the
                // caution below is the cautious answer.
                try { targetInAWindow = new UIDocument(open).GetOpenUIViews().Count > 0; }
                catch (Exception) { targetInAWindow = true; }
            }
        }
        catch (Exception) { targetInAWindow = false; }

        try
        {
            var uiapp = uidoc.Application;
            var opened = uiapp.OpenAndActivateDocument(path);
            if (opened != null && opened.Document != null) activeAfter = opened.Document.Title;
            activated = activeAfter.Length > 0;
        }
        catch (Exception error)
        {
            problem = error.Message;
            findings.Add(string.Format("Revit would not activate \"{0}\": {1}", path,
                error.Message));
        }

        if (activated)
        {
            findings.Add(string.Format("'{0}' is now the document in front (was '{1}').",
                activeAfter, before.Length == 0 ? "<unknown>" : before));
        }

        // VERSION 3: AND AFTER IT - READ BACK, as activeAfter is, whether Revit
        // took the activation or refused it. A WRITE IS SAID ONLY WHERE THE
        // FILE SHOWS ONE: its time moved - or, only when that time cannot be
        // read, the unsaved changes went. Changes gone from a file whose time
        // did not move are said as that, never as a write.
        if (inFront != null)
        {
            var dirtyAfter = false;
            var dirtyAfterKnown = false;
            try { dirtyAfter = inFront.IsModified; dirtyAfterKnown = true; }
            catch (Exception) { dirtyAfterKnown = false; }

            DateTime? stampAfter = null;
            try
            {
                if (frontPath.Length > 0 && System.IO.File.Exists(frontPath))
                    stampAfter = System.IO.File.GetLastWriteTimeUtc(frontPath);
            }
            catch (Exception) { stampAfter = null; }

            var stampKnown = stampBefore.HasValue && stampAfter.HasValue;
            var stampMoved = stampKnown && stampAfter.Value != stampBefore.Value;
            var dirtyKnown = dirtyBeforeKnown && dirtyAfterKnown;
            var changesGone = dirtyKnown && dirtyBefore && !dirtyAfter;
            leftBehindWritten = stampMoved || (changesGone && !stampKnown && frontPath.Length > 0);

            var behindName = before.Length == 0
                ? "The document that was in front" : "'" + before + "'";

            // Local time, as Explorer shows it; the date when it is not today,
            // and the milliseconds when the seconds alone read the same.
            var today = DateTime.Now.Date;
            Func<DateTime, bool, string> clock = delegate (DateTime utc, bool fine)
            {
                var local = utc.ToLocalTime();
                return local.ToString((local.Date == today ? "" : "yyyy-MM-dd ")
                    + (fine ? "HH:mm:ss.fff" : "HH:mm:ss"),
                    System.Globalization.CultureInfo.InvariantCulture);
            };

            if (leftBehindWritten)
            {
                var signs = new List<string>();
                if (stampMoved)
                {
                    var sameSecond = clock(stampBefore.Value, false)
                        == clock(stampAfter.Value, false);
                    signs.Add(string.Format("its file's time moved from {0} to {1}",
                        clock(stampBefore.Value, sameSecond), clock(stampAfter.Value, sameSecond)));
                }
                if (changesGone) signs.Add("it had unsaved changes before and has none now");
                findings.Add(string.Format("{0} WAS WRITTEN TO ITS FILE while Revit {1} - {2}. "
                    + "Heron did not ask Revit to save it (row 5b-1851). A save cannot be "
                    + "undone: the file at {3} now holds what was open.",
                    behindName, activated ? "switched windows" : "was asked to switch windows",
                    string.Join(", and ", signs), frontPath));
            }
            else if (changesGone)
            {
                findings.Add(string.Format("{0} no longer shows unsaved changes, {1} - Heron "
                    + "saved nothing, and cannot say what changed it (row 5b-1851).", behindName,
                    frontPath.Length == 0
                        ? "and it has no file" : "but its file's time did not change"));
            }
            else if (activated && dirtyKnown && dirtyBefore && dirtyAfter)
            {
                findings.Add(string.Format("{0}, now behind, still had unsaved changes when Heron "
                    + "looked - Heron saved nothing.{1}", behindName, targetInAWindow
                    ? " The document brought forward was already open in a window, and a switch "
                      + "like that has been seen to make Revit save the document left behind, "
                      + "unasked (row 5b-1851) - look at its file's time before trusting that it "
                      + "is unsaved."
                    : ""));
            }

            if (!stampKnown && !dirtyKnown)
            {
                findings.Add(string.Format("Heron could not read whether {0} was written while "
                    + "Revit switched windows - neither its file's time nor its unsaved changes "
                    + "could be read.", before.Length == 0 ? "the document that was in front"
                    : "'" + before + "'"));
            }
        }
    }
}

findings.Insert(0, string.Format("{0}; in front now: {1}",
    activated ? (wasAlready ? "already in front" : "activated") : "NOT activated",
    activeAfter.Length == 0 ? "<unchanged>" : activeAfter));
