// Heron-Agent:  HERON-REVIT-TSA-006, HERON-REVIT-TRN-005, HERON-REVIT-WRN-016, HERON-REVIT-CTX-007, HERON-REVIT-ELE-010, HERON-KRN-EVD-014, HERON-KRN-HUM-018
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Bridge;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// The first code in Heron that can change a model.
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit. It COMPILES - Revit 2020 through
    /// 2024, 0 warnings, since 2026-08-28 - and that is the whole of what is
    /// known about it. It has never been loaded into Revit and has never
    /// moved anything.
    ///
    /// Compiling proves the API surface agrees. It does not prove that a duct
    /// moves 200 millimetres rather than 200 feet, and this file is where that
    /// would happen. Do not point it at a real project until NEEDS-CHECKING.md
    /// group D has passed - D3 in particular, which is "move them, then MEASURE
    /// one". HeronPermissions keeps it switched off until then.
    /// ===================================================================
    ///
    /// It is deliberately shaped to REFUSE rather than to guess. Every check
    /// below exists because the alternative is a change that lands somewhere
    /// nobody was looking:
    ///
    ///   Golden Rule 16  one TransactionGroup, so one Ctrl+Z puts it back
    ///   Golden Rule 17  nothing writes without a preview the user accepted
    ///   Golden Rule 20  the document is pinned by identity, not by which
    ///                   window happens to be in front
    ///   Golden Rule 21  the preview expires, and the count is taken again
    ///                   immediately before the write
    ///
    /// The order matters. Each check is cheap and each one refuses before
    /// anything irreversible starts, so the expensive, dangerous part is only
    /// reached when everything else has already agreed.
    /// </summary>
    internal static class RevitWrite
    {
        /// <summary>
        /// How long an accepted preview stays valid.
        ///
        /// Short on purpose. A preview is a statement about a model at one
        /// instant, and the model is being edited by a person - and possibly
        /// by other people through a central file. Two minutes is long enough
        /// to read a sentence and answer it, and short enough that a preview
        /// found in yesterday's chat scrollback is dead rather than dangerous.
        /// </summary>
        private static readonly TimeSpan PreviewLifetime = TimeSpan.FromMinutes(2);

        private static readonly object PreviewLock = new object();
        private static Preview _pending;

        /// <summary>
        /// What the user was shown and therefore what they agreed to.
        ///
        /// The id SET is kept, not just the count. Golden Rule 21 requires the
        /// count to be taken again before writing, but a count alone cannot
        /// tell the difference between "nothing changed" and "one duct was
        /// deleted while another was drawn". Both give 247. Only one of them
        /// is the model the user looked at.
        /// </summary>
        private sealed class Preview
        {
            public string Token;
            public string DocumentKey;
            public string DocumentTitle;
            public string Category;
            public double MillimetresUp;
            public HashSet<ElementId> Ids;
            public List<ElementId> Skipped;
            public DateTime CreatedUtc;

            public bool HasExpired
            {
                get { return DateTime.UtcNow - CreatedUtc > PreviewLifetime; }
            }
        }

        // ------------------------------------------------------------ routing

        public static string Run(UIApplication app, string request, string op)
        {
            switch (op)
            {
                case "preview_move":
                    return PreviewMove(app,
                        Json.ReadString(request, "category"),
                        Json.ReadString(request, "millimetres"));

                case "move_elements":
                    // "approvalToken", NOT "token". BridgeServer.Dispatch has already
                    // spent "token" on authentication before this method is reached, so a
                    // request carrying the approval slip under that name never arrives -
                    // it is refused at the door as unauthenticated. One key cannot be two
                    // secrets. Found 2026-09-07, the first time the write path was run.
                    return ExecuteMove(app, Json.ReadString(request, "approvalToken"));

                default:
                    return null;      // not ours; RevitOperations reports it
            }
        }

        // ------------------------------------------------------------ preview

        /// <summary>
        /// Works out what WOULD happen, changes nothing, and remembers it so
        /// the answer can be checked again before it is acted on.
        ///
        /// Counting is a read, so this needs no permission beyond READ and no
        /// transaction. It is refused when writing is switched off anyway -
        /// offering a preview of something Heron is not allowed to do wastes
        /// the user's time and reads as though the change is about to happen.
        /// </summary>
        private static string PreviewMove(UIApplication app, string category, string millimetresText)
        {
            // This operation is declared ANALYZE - it genuinely changes
            // nothing - so the gate in RevitOperations has already let it
            // through. What is checked here is the risk of the operation it is
            // a preview OF: showing somebody a change Heron is not permitted
            // to make reads as though it is about to happen, and wastes the
            // time they spend deciding.
            //
            // Read from the registry rather than named as a literal, so this
            // and the gate cannot come to disagree about what a move costs.
            var refusal = Refuse(HeronOperationRegistry.RiskOf("move_elements"));
            if (refusal != null) return refusal;

            double millimetres;
            var bad = ReadDistance(millimetresText, out millimetres);
            if (bad != null) return bad;

            Document doc;
            var noDoc = PinnedDocument(app, out doc);
            if (noDoc != null) return noDoc;

            BuiltInCategory builtIn;
            var unknown = RevitOperations.ResolveCategory(category, out builtIn);
            if (unknown != null) return unknown;

            List<ElementId> movable, skipped;
            Partition(doc, builtIn, out movable, out skipped);

            if (movable.Count == 0)
            {
                return Json.Error("nothing_to_move",
                    "Nothing in " + doc.Title + " can be moved: " +
                    Describe(0, skipped.Count, category) + ".");
            }

            var preview = new Preview
            {
                Token = Guid.NewGuid().ToString("N").Substring(0, 12),
                DocumentKey = DocumentKey(doc),
                DocumentTitle = doc.Title,
                Category = category.Trim(),
                MillimetresUp = millimetres,
                Ids = new HashSet<ElementId>(movable),
                Skipped = skipped,
                CreatedUtc = DateTime.UtcNow,
            };

            lock (PreviewLock) { _pending = preview; }

            return Json.Ok(
                // Named apart from the session token on purpose - see the note at the
                // move_elements case above.
                Json.Str("approvalToken", preview.Token),
                Json.Num("willMove", movable.Count),
                Json.Num("willSkip", skipped.Count),
                Json.Str("category", preview.Category),
                Json.Str("distance", HeronUnits.DescribeMillimetres(millimetres)),
                Json.Str("document", doc.Title),
                Json.Str("documentPath", string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Num("expiresInSeconds", (long)PreviewLifetime.TotalSeconds),
                Json.Str("summary", Describe(movable.Count, skipped.Count, preview.Category) +
                                    " " + HeronUnits.DescribeVerticalMove(millimetres) +
                                    " in " + doc.Title));
        }

        // ------------------------------------------------------------ execute

        /// <summary>
        /// Applies an accepted preview, having proved it is still true.
        ///
        /// EVERY refusal here happens before the transaction opens. Once
        /// ElementTransformUtils has run there is a change in the model that
        /// only a rollback removes, so the cheap checks are spent first and
        /// the expensive one last.
        /// </summary>
        private static string ExecuteMove(UIApplication app, string token)
        {
            // Belt and braces. The gate in RevitOperations is the primary
            // check and has already run; this is the second one, kept because
            // this is the only method in Heron that can change a model and the
            // cost of a check is nothing against the cost of one being
            // missed. It reads the SAME registry entry, so the two cannot
            // drift into disagreeing.
            var refusal = Refuse(HeronOperationRegistry.RiskOf("move_elements"));
            if (refusal != null) return refusal;

            Preview preview;
            lock (PreviewLock) { preview = _pending; }

            if (preview == null)
            {
                return Json.Error("no_preview",
                    "There is nothing waiting to be approved. Ask for the change again and " +
                    "Heron will show what it would do first.");
            }

            if (string.IsNullOrEmpty(token) || token != preview.Token)
            {
                // Not pedantry. If two chats are talking to one Revit, an
                // approval meant for one preview must never execute another.
                return Json.Error("wrong_preview",
                    "That approval does not match what Heron is waiting on. Ask for the " +
                    "change again so you are approving what will actually happen.");
            }

            if (preview.HasExpired)
            {
                lock (PreviewLock) { _pending = null; }
                return Json.Error("preview_expired",
                    "That preview is more than " + (long)PreviewLifetime.TotalMinutes +
                    " minutes old, so it no longer describes the model. Ask again for a " +
                    "fresh one - nothing was changed.");
            }

            Document doc;
            var noDoc = PinnedDocument(app, out doc);
            if (noDoc != null) return noDoc;

            // GOLDEN RULE 20. The preview was made about ONE document. If the
            // user has since clicked into another project - or closed and
            // reopened this one - the front window is no longer the thing they
            // approved. Following it is how a change lands in the wrong
            // building with every step individually correct.
            if (DocumentKey(doc) != preview.DocumentKey)
            {
                lock (PreviewLock) { _pending = null; }

                // TWO DIFFERENT THINGS HAVE HAPPENED, and Golden Rule 20 treats
                // them differently, so Heron must not report them alike.
                //
                //   Still open, just not in front  -> the user clicked away.
                //      Recoverable in one click, and saying so saves them
                //      re-doing the whole request.
                //
                //   Gone                           -> "if the pinned document
                //      closes, stop". Telling someone to click back to a model
                //      that is no longer open sends them looking for something
                //      that does not exist, at the exact moment they are
                //      already unsure what just happened to their work.
                if (IsStillOpen(app, preview.DocumentKey))
                {
                    return Json.Error("document_not_in_front",
                        "That preview was made for " + preview.DocumentTitle + ", but " +
                        doc.Title + " is in front now. " + preview.DocumentTitle +
                        " is still open - click back to it and ask again. Nothing was changed.");
                }

                return Json.Error("document_closed",
                    preview.DocumentTitle + " has been closed since that preview, so Heron has " +
                    "stopped rather than moving elements in " + doc.Title +
                    " instead. Nothing was changed.");
            }

            // GOLDEN RULE 21. Take the count again, right now, against the
            // same document. A preview accepted for 247 must never quietly
            // execute on 261.
            BuiltInCategory builtIn;
            var unknown = RevitOperations.ResolveCategory(preview.Category, out builtIn);
            if (unknown != null) return unknown;

            List<ElementId> movable, skipped;
            Partition(doc, builtIn, out movable, out skipped);

            var now = new HashSet<ElementId>(movable);
            if (!now.SetEquals(preview.Ids))
            {
                lock (PreviewLock) { _pending = null; }
                return Json.Error("model_moved_on",
                    "The model changed since that preview - it described " + preview.Ids.Count +
                    " " + preview.Category + " and there are now " + now.Count +
                    ". Nothing was changed. Ask again to see the current picture.");
            }

            var up = new XYZ(0, 0, HeronUnits.MillimetresToFeet(preview.MillimetresUp));
            // WHAT REVIT'S OWN UNDO HISTORY WILL SAY, so it has to read like
            // something a person did - "Heron: move ducts down 50 mm", never
            // "up -50 mm". Golden Rule 16 makes this the one entry the user
            // sees for the whole operation; it is the label on their undo.
            var name = "Heron: move " + preview.Category + " " +
                       HeronUnits.DescribeVerticalMove(preview.MillimetresUp);

            var handler = new CollectWarnings();
            var workflow = HeronAudit.NewWorkflowId();

            // WHAT ACTUALLY MOVED, which is not the same as what was asked to.
            // Filled in by Verify. Declared out here because the answer has to
            // outlive the transaction that produced it.
            int reallyMoved = 0, partly = 0, blocked = 0, unverified = 0;

            // GOLDEN RULE 16. ONE group, named, so the whole thing is a single
            // entry in Revit's undo stack whatever happened inside it. The
            // user must be able to reverse Heron with one keystroke, and a
            // change that takes four Ctrl+Z to undo is one the user stops
            // trusting after the first time it surprises them.
            using (var group = new TransactionGroup(doc, name))
            {
                group.Start();
                try
                {
                    using (var transaction = new Transaction(doc, name))
                    {
                        transaction.Start();

                        var options = transaction.GetFailureHandlingOptions();
                        options.SetFailuresPreprocessor(handler);
                        options.SetClearAfterRollback(true);
                        transaction.SetFailureHandlingOptions(options);

                        // WHERE EVERYTHING WAS, BEFORE. The only evidence that
                        // survives the next four lines - see Verify below for
                        // why "it did not throw" is not evidence at all.
                        var before = ProbeAll(doc, movable);

                        ElementTransformUtils.MoveElements(doc, movable, up);

                        // Positions do not update until the document has
                        // regenerated, so a check before this would compare
                        // each element against itself and pass every time.
                        doc.Regenerate();

                        Verify(doc, movable, before, up,
                               out reallyMoved, out partly, out blocked, out unverified);

                        if (transaction.Commit() != TransactionStatus.Committed)
                        {
                            SafeRollBack(group);
                            return Failed(workflow, preview, handler,
                                "Revit did not accept the change, so nothing was moved.");
                        }
                    }

                    group.Assimilate();
                }
                catch (Exception ex)
                {
                    // Complete rollback, then say so. A partial move is worse
                    // than no move: it looks like it worked.
                    //
                    // The rollback CANNOT be allowed to throw here. If it did,
                    // its exception would replace the one being reported - and
                    // the user would be told the group could not be rolled back
                    // instead of what actually went wrong with their move. The
                    // second error is the less useful of the two, and it would
                    // arrive at the worst possible moment. See SafeRollBack.
                    SafeRollBack(group);
                    return Failed(workflow, preview, handler,
                        "The move failed and was rolled back completely, so the model is as " +
                        "it was. Revit said: " + ex.Message);
                }
            }

            lock (PreviewLock) { _pending = null; }

            // After the group is assimilated, never inside it: a redraw is not
            // part of the change and must not be able to affect whether the
            // change is reported as having happened.
            TryRefresh(app);

            // WHAT WAS TOUCHED, not just how many. docs/21 section 13 and
            // docs/12 section 5: the audit log carries document identity,
            // element UniqueIds and the transaction group name, all keyed by
            // Workflow ID.
            //
            // The count alone cannot answer the question anybody actually asks
            // after something goes wrong - "WHICH ducts did it move?" - and
            // that question is the entire point of an append-only record. With
            // the ids in the log it is a query; without them the log can only
            // confirm that something happened to some number of things.
            //
            // UniqueId rather than ElementId on purpose: an ElementId is only
            // meaningful inside one open document, and the log outlives the
            // session. A UniqueId identifies the element across saves, and
            // across the central file.
            HeronAudit.Record(workflow, "move_elements", true, new[]
            {
                new KeyValuePair<string, string>("document", doc.Title),
                new KeyValuePair<string, string>("documentId", preview.DocumentKey),
                new KeyValuePair<string, string>("category", preview.Category),
                // VERIFIED against the model, not the number Heron asked to
                // move. The four are recorded separately because an audit that
                // says "moved 5" when 5 did not budge is worse than no audit.
                new KeyValuePair<string, string>("moved", reallyMoved.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("attempted", movable.Count.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("partly", partly.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("blocked", blocked.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("unverified", unverified.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("millimetres", preview.MillimetresUp.ToString("0.###", CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("warnings", handler.Count.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("undoEntry", name),
                new KeyValuePair<string, string>("elements", UniqueIds(doc, movable)),
                new KeyValuePair<string, string>("skippedElements", UniqueIds(doc, skipped)),
            });

            return Json.Ok(
                Json.Num("moved", reallyMoved),
                Json.Num("skipped", skipped.Count),
                // Three counts that are normally zero and must never be
                // folded into "moved" when they are not. `blocked` in
                // particular is the one Revit itself will not tell you about.
                Json.Num("partly", partly),
                Json.Num("blocked", blocked),
                Json.Num("unverified", unverified),
                Json.Str("category", preview.Category),
                // Carries its DIRECTION, because the one caller that reads
                // this builds the sentence "Moved 9 ducts <distance> in X" -
                // and a bare "-50 mm" there is how the reply came to say
                // "Moved 9 ducts -50 mm".
                Json.Str("distance", HeronUnits.DescribeVerticalMove(preview.MillimetresUp)),
                Json.Str("document", doc.Title),
                Json.Str("documentPath", string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Num("warnings", handler.Count),
                Json.Str("undo", "One Ctrl+Z in Revit puts this back."),
                Json.Str("undoEntry", name));
        }

        // ------------------------------------------------------------ helpers

        /// <summary>
        /// Roll back, and never throw doing it.
        ///
        /// A rollback is always a SECOND thing going wrong: something already
        /// failed, and this is the cleanup. If the cleanup throws too, its
        /// exception escapes and buries the original - the user is told the
        /// group was not in a rollback-able state, which tells them nothing,
        /// instead of what actually happened to their model.
        ///
        /// This is not a theoretical worry - it is a shipped bug in real Revit
        /// tooling, met and fixed before Heron existed. The shape: an unguarded
        /// rollback in a catch block throws a second time, that exception
        /// escapes before the result is ever reported, and the caller waits
        /// forever on an answer that is never coming. Heron's version of the
        /// hazard is the same one because the API is: TransactionGroup has no
        /// state in which RollBack() is guaranteed to succeed.
        ///
        /// Both guards are kept on purpose. The status check avoids provoking
        /// an exception in the ordinary case; the catch handles everything the
        /// status check cannot see - a group left un-rollback-able by an
        /// Assimilate() that failed part way, or a GetStatus() that throws on
        /// its own. Cheap first, then total.
        /// </summary>
        private static void SafeRollBack(TransactionGroup group)
        {
            try
            {
                if (group.GetStatus() == TransactionStatus.Started) group.RollBack();
            }
            catch
            {
                // Nothing more can be done to the group, and saying so would
                // replace a useful message with a useless one. The `using`
                // block's Dispose() rolls back anything still open.
            }
        }

        private static string Failed(string workflow, Preview preview, CollectWarnings handler,
                                     string message)
        {
            lock (PreviewLock) { _pending = null; }
            HeronAudit.Record(workflow, "move_elements", false, new[]
            {
                new KeyValuePair<string, string>("document", preview.DocumentTitle),
                new KeyValuePair<string, string>("category", preview.Category),
                new KeyValuePair<string, string>("reason", message),
                new KeyValuePair<string, string>("warnings", handler.Count.ToString(CultureInfo.InvariantCulture)),
            });
            return Json.Error("move_failed", message);
        }

        /// <summary>
        /// Is the pinned document still open in this Revit, even if it is not
        /// the one in front?
        ///
        /// LINKED DOCUMENTS ARE EXCLUDED. Application.Documents contains every
        /// loaded link as well as the projects the user opened - an
        /// architectural model linked into an MEP job is in that collection and
        /// is not something the user ever chose to work in. Counting one as
        /// "still open" would let a refusal point at a model that cannot be
        /// clicked back to.
        /// </summary>
        private static bool IsStillOpen(UIApplication app, string documentKey)
        {
            if (app == null || app.Application == null) return false;

            try
            {
                foreach (Document open in app.Application.Documents)
                {
                    if (open == null || open.IsLinked) continue;
                    if (DocumentKey(open) == documentKey) return true;
                }
            }
            catch
            {
                // Enumerating documents is a convenience for the wording of a
                // refusal that is happening either way. If it fails, the
                // refusal still stands - it just takes the more cautious of
                // the two messages.
                return false;
            }

            return false;
        }

        /// <summary>
        /// A point on this element that can be compared before and after a move.
        ///
        /// Not for reporting and not for geometry - only for answering "did
        /// this actually move?". Three routes because elements keep their
        /// position in different places: a point for a placed family, a curve
        /// for a duct or pipe, and a bounding box for everything else.
        ///
        /// Returns null when none of the three works, and the caller counts
        /// that as UNVERIFIED rather than as moved. A thing that cannot be
        /// checked must never be reported as checked.
        /// </summary>
        private static XYZ ProbePoint(Element element)
        {
            if (element == null) return null;

            var point = element.Location as LocationPoint;
            if (point != null) return point.Point;

            var curve = element.Location as LocationCurve;
            if (curve != null && curve.Curve != null) return curve.Curve.Evaluate(0.5, true);

            try
            {
                var box = element.get_BoundingBox(null);
                if (box != null) return (box.Min + box.Max) * 0.5;
            }
            catch
            {
                // A bounding box can throw on an element with no geometry in
                // the current view. Not knowing where something is is a normal
                // outcome here, and it is reported as such.
            }
            return null;
        }

        private static Dictionary<ElementId, XYZ> ProbeAll(Document doc, IList<ElementId> ids)
        {
            var where = new Dictionary<ElementId, XYZ>();
            foreach (var id in ids)
            {
                var point = ProbePoint(doc.GetElement(id));
                if (point != null) where[id] = point;
            }
            return where;
        }

        /// <summary>
        /// Did the elements actually move, and by how much.
        ///
        /// THE REASON THIS EXISTS, and it is not a hypothetical.
        ///
        /// `ElementTransformUtils.MoveElements` RETURNS NORMALLY AND MOVES
        /// NOTHING when an element cannot be moved - a member of a group is the
        /// case that matters here. No exception, no return value, no warning.
        /// Counting "it did not throw" as "it moved" reports *"Moved 5, skipped
        /// 0"* for five air terminals that have not shifted by a millimetre.
        /// That was proved against a real model in the owner's earlier work,
        /// and it is exactly the "succeeded and did nothing" failure
        /// [D-30](../../docs/DECISIONS.md) makes every fragment prove against.
        ///
        /// Heron already skips PINNED elements before it gets here, which
        /// covers one half of that case and not the other: a group member is
        /// not pinned, so it passes the filter and then silently does not move.
        ///
        /// The only honest evidence is the position itself, so that is what is
        /// compared. Four outcomes, kept apart because they need different
        /// answers from the user:
        ///
        ///   moved       it is where it was asked to be
        ///   partly      it moved, but not the whole way - constrained by a
        ///               host or an attachment, which is legitimate
        ///   blocked     it did not move at all, and Revit reported no error
        ///   unverified  its position could not be read either side
        ///
        /// Nothing here rolls the move back. A blocked element is a fact to
        /// report, not a failure - and rolling back the ones that DID move
        /// because one did not would be its own surprise.
        /// </summary>
        private static void Verify(Document doc, IList<ElementId> ids,
                                   Dictionary<ElementId, XYZ> before, XYZ asked,
                                   out int moved, out int partly,
                                   out int blocked, out int unverified)
        {
            moved = 0; partly = 0; blocked = 0; unverified = 0;

            // One millimetre, in Revit's feet. The tolerance has to be smaller
            // than the smallest move worth asking for and larger than the noise
            // in a coordinate, and a millimetre is both.
            var tolerance = HeronUnits.MillimetresToFeet(1.0);

            // A move of nothing is a move nobody can measure. Asked for zero,
            // every element is where it should be, and comparing would report
            // all of them blocked.
            var askedForNothing = asked.GetLength() < tolerance;

            foreach (var id in ids)
            {
                var now = ProbePoint(doc.GetElement(id));
                XYZ was;

                if (askedForNothing) { moved++; continue; }
                if (now == null || !before.TryGetValue(id, out was)) { unverified++; continue; }

                if (now.DistanceTo(was) < tolerance) blocked++;
                else if (now.DistanceTo(was + asked) > tolerance) partly++;
                else moved++;
            }
        }

        /// <summary>
        /// Redraw, so the user actually SEES what just happened.
        ///
        /// It is cosmetic, and it is in its own try/catch for a reason that is
        /// not cosmetic at all: this runs AFTER the change has been committed.
        /// If a refresh threw - an active view invalidated mid-operation, say -
        /// and that exception reached the outer catch, Heron would report an
        /// already-committed move as a failure and then try to roll back a
        /// group it can no longer roll back. The user would be told nothing
        /// happened, while their ducts had in fact moved.
        ///
        /// Also a shipped bug in real Revit tooling rather than a hypothetical,
        /// and the same lesson as SafeRollBack from the other direction:
        /// cleanup and cosmetics must never be able to change what gets
        /// reported about the real work. Neither of them IS the work.
        /// </summary>
        private static void TryRefresh(UIApplication app)
        {
            try
            {
                var uiDoc = app == null ? null : app.ActiveUIDocument;
                if (uiDoc != null) uiDoc.RefreshActiveView();
            }
            catch
            {
                // Never turn a committed success into a reported failure.
            }
        }

        /// <summary>
        /// The UniqueIds of these elements, comma separated, for the audit log.
        ///
        /// Every id, not a sample. A truncated list answers "roughly what did
        /// Heron touch?", which is not a question anybody asks - they ask
        /// whether ONE specific duct was moved, and a sample cannot answer
        /// that. Several hundred ids is a few kilobytes in an append-only file;
        /// the alternative is a record that cannot settle an argument.
        ///
        /// Never throws. An audit failure must not take down the operation
        /// being audited, and this runs after a change has already committed.
        /// </summary>
        private static string UniqueIds(Document doc, IList<ElementId> ids)
        {
            if (ids == null || ids.Count == 0) return null;

            try
            {
                var parts = new List<string>(ids.Count);
                foreach (var id in ids)
                {
                    var element = doc.GetElement(id);
                    if (element != null) parts.Add(element.UniqueId);
                }
                return parts.Count == 0 ? null : string.Join(",", parts.ToArray());
            }
            catch
            {
                return null;
            }
        }

        /// <summary>Permission and emergency stop, in that order.</summary>
        private static string Refuse(HeronRisk risk)
        {
            if (HeronStop.IsStopped) return Json.Error("stopped", HeronStop.Message);

            var why = HeronPermissions.Explain(risk);
            if (why != null) return Json.Error("write_disabled", why);

            return null;
        }

        /// <summary>
        /// The distance, as a string, parsed invariantly.
        ///
        /// It crosses the wire as a string rather than a JSON number for one
        /// reason: Heron's own JSON reader can read strings and nothing else,
        /// and this code cannot be compiled on the machine it was written on.
        /// Widening a parser that IS proven, without being able to build it,
        /// to serve code that is NOT proven, is the wrong trade. The Python
        /// side has already validated the number; this re-validates it because
        /// the wire is not a place to extend trust.
        /// </summary>
        private static string ReadDistance(string text, out double millimetres)
        {
            millimetres = 0;

            if (string.IsNullOrEmpty(text))
            {
                return Json.Error("no_distance", "No distance was given. Try '200 mm'.");
            }

            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture,
                                 out millimetres))
            {
                return Json.Error("bad_distance",
                    "'" + text + "' is not a distance Heron can read. Try a number of " +
                    "millimetres, like 200.");
            }

            if (!HeronUnits.IsUsableMillimetres(millimetres))
            {
                return Json.Error("bad_distance",
                    "'" + text + "' is not a distance Heron will act on.");
            }

            if (Math.Abs(millimetres) < 0.001)
            {
                return Json.Error("zero_distance",
                    "Moving something by zero would change nothing. Say how far to move it.");
            }

            return null;
        }

        /// <summary>
        /// A document's identity, stable across saves and window switches.
        ///
        /// The identity is the UniqueId of the document's own Project
        /// Information element. It is created with the document, it survives
        /// being saved, renamed and moved, and it is different for every
        /// open model - which is all Golden Rule 20 asks of it.
        ///
        /// It is NOT Document.CreationGUID, and that is deliberate.
        /// CreationGUID reads like the obvious answer and does not exist in
        /// Revit 2020 - checked against the shipped RevitAPI.dll for both
        /// releases, not from memory. Using it compiled on 2024 and failed
        /// on 2020, which is D-05 and docs/16 exactly: the runtime table is
        /// never extrapolated, and a member that arrived mid-range is a
        /// version boundary whether or not anybody noticed it.
        ///
        /// A #if could have papered over it. This does not need one - the
        /// same expression is correct on every release from 2020 to 2027 -
        /// and that is worth more than the branch it avoids, for the same
        /// reason D-20 prefers arithmetic to UnitUtils: there is nothing
        /// here for Autodesk to move underneath it.
        ///
        /// Title and path are carried too, so that a mismatch can be
        /// EXPLAINED to the user in words they recognise - a bare identity
        /// comparison can refuse correctly and still leave the user with no
        /// idea which two models it is talking about.
        /// </summary>
        private static string DocumentKey(Document doc)
        {
            // ProjectInformation is present in every project document. A
            // family document has none, and Heron does not write to those -
            // but falling back to the path is cheaper than a null reference
            // inside a refusal that exists to keep the user safe.
            var info = doc.ProjectInformation;
            var identity = info == null ? "no-project-info" : info.UniqueId;

            return identity + "|" + (doc.PathName ?? "") + "|" + doc.Title;
        }

        /// <summary>
        /// The document to act on, resolved fresh - never cached (docs/03 §4).
        /// </summary>
        private static string PinnedDocument(UIApplication app, out Document doc)
        {
            doc = null;
            var uiDoc = app == null ? null : app.ActiveUIDocument;
            if (uiDoc == null || uiDoc.Document == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            doc = uiDoc.Document;

            if (doc.IsReadOnly)
            {
                return Json.Error("read_only",
                    doc.Title + " is open read-only, so nothing in it can be changed.");
            }

            return null;
        }

        /// <summary>
        /// Splits a category into what can move and what cannot, WITHOUT
        /// changing anything.
        ///
        /// The two reasons an element is left alone are the two the user will
        /// meet in real work:
        ///
        ///   PINNED       somebody pinned it on purpose. Moving a pinned
        ///                element either fails or silently defeats the pin,
        ///                and both are worse than reporting it.
        ///   OWNED BY
        ///   SOMEONE ELSE on a workshared model, another person holds it.
        ///                Revit would refuse mid-transaction and take the
        ///                whole operation down with it.
        ///
        /// Finding them BEFORE the transaction is what lets the preview say
        /// "12 are owned by another user and will be skipped" - which is the
        /// sentence the build order asks for, and it can only be honest if
        /// the check happens here rather than as an error later.
        /// </summary>
        private static void Partition(Document doc, BuiltInCategory builtIn,
                                      out List<ElementId> movable, out List<ElementId> skipped)
        {
            movable = new List<ElementId>();
            skipped = new List<ElementId>();

            var workshared = doc.IsWorkshared;

            var found = new FilteredElementCollector(doc)
                .OfCategory(builtIn)
                .WhereElementIsNotElementType()
                .ToElementIds();

            foreach (var id in found)
            {
                var element = doc.GetElement(id);
                if (element == null) continue;

                if (element.Pinned)
                {
                    skipped.Add(id);
                    continue;
                }

                if (workshared)
                {
                    var status = WorksharingUtils.GetCheckoutStatus(doc, id);
                    if (status == CheckoutStatus.OwnedByOtherUser)
                    {
                        skipped.Add(id);
                        continue;
                    }
                }

                movable.Add(id);
            }
        }

        private static string Describe(int movable, int skipped, string category)
        {
            var text = movable.ToString("N0", CultureInfo.InvariantCulture) + " " + category;
            if (skipped > 0)
            {
                text += ", skipping " + skipped.ToString("N0", CultureInfo.InvariantCulture) +
                        " that are pinned or owned by someone else";
            }
            return text;
        }

        /// <summary>
        /// Revit Warning Agent. Warnings are swallowed so the operation does
        /// not stop on a dialog nobody is there to click - but they are
        /// COUNTED, and the count is reported and audited.
        ///
        /// Errors are different and are never swallowed: an error means Revit
        /// could not do what was asked, and continuing past one produces a
        /// model that is subtly wrong rather than obviously unchanged. Those
        /// roll the whole thing back.
        ///
        /// AND SO IS ANYTHING ABOVE AN ERROR, WHICH THIS USED TO MISS. The
        /// test was `== FailureSeverity.Error`, and `FailureSeverity` has a
        /// fourth member above it: read out of the shipped `RevitAPI.dll` by
        /// reflection rather than from memory, it is
        /// `None=0, Warning=1, Error=2, DocumentCorruption=3` - the same on
        /// Revit 2020 and Revit 2024. So `DocumentCorruption` matched neither
        /// branch, was not rolled back, was not counted, and fell through to
        /// Continue - leaving `warnings: 0` in the reply and in the audit while
        /// Revit was reporting the worst thing it can report. The comparison is
        /// `>=` so that a severity added above these is caught by arriving
        /// rather than by somebody remembering to add a branch for it.
        /// FRAGMENT-ISSUES section 5b, row 10.
        /// </summary>
        private sealed class CollectWarnings : IFailuresPreprocessor
        {
            public int Count;

            public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
            {
                var messages = accessor.GetFailureMessages();

                foreach (var message in messages)
                {
                    if (message.GetSeverity() >= FailureSeverity.Error)
                    {
                        return FailureProcessingResult.ProceedWithRollBack;
                    }
                }

                foreach (var message in messages)
                {
                    if (message.GetSeverity() == FailureSeverity.Warning)
                    {
                        Count++;
                        accessor.DeleteWarning(message);
                    }
                }

                return FailureProcessingResult.Continue;
            }
        }
    }
}
