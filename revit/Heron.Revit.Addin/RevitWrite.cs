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
    /// Written on a machine with no Revit and no .NET SDK. It has never been
    /// compiled, never loaded and has never moved anything. Do not point it
    /// at a real project until it has been through Revit on a machine that
    /// can build it. HeronPermissions keeps it switched off until then.
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
                    return ExecuteMove(app, Json.ReadString(request, "token"));

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
            var refusal = Refuse(HeronRisk.Modify);
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
                Json.Str("token", preview.Token),
                Json.Num("willMove", movable.Count),
                Json.Num("willSkip", skipped.Count),
                Json.Str("category", preview.Category),
                Json.Str("distance", HeronUnits.DescribeMillimetres(millimetres)),
                Json.Str("document", doc.Title),
                Json.Str("documentPath", string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName),
                Json.Num("expiresInSeconds", (long)PreviewLifetime.TotalSeconds),
                Json.Str("summary", Describe(movable.Count, skipped.Count, preview.Category) +
                                    " up " + HeronUnits.DescribeMillimetres(millimetres) +
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
            var refusal = Refuse(HeronRisk.Modify);
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
                return Json.Error("document_changed",
                    "That preview was made for " + preview.DocumentTitle + ", but " +
                    doc.Title + " is in front now. Heron will not move elements in a model " +
                    "you did not approve. Nothing was changed - ask again.");
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
            var name = "Heron: move " + preview.Category + " up " +
                       HeronUnits.DescribeMillimetres(preview.MillimetresUp);

            var handler = new CollectWarnings();
            var workflow = HeronAudit.NewWorkflowId();

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

                        ElementTransformUtils.MoveElements(doc, movable, up);

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

            HeronAudit.Record(workflow, "move_elements", true, new[]
            {
                new KeyValuePair<string, string>("document", doc.Title),
                new KeyValuePair<string, string>("category", preview.Category),
                new KeyValuePair<string, string>("moved", movable.Count.ToString(CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("millimetres", preview.MillimetresUp.ToString("0.###", CultureInfo.InvariantCulture)),
                new KeyValuePair<string, string>("warnings", handler.Count.ToString(CultureInfo.InvariantCulture)),
            });

            return Json.Ok(
                Json.Num("moved", movable.Count),
                Json.Num("skipped", skipped.Count),
                Json.Str("category", preview.Category),
                Json.Str("distance", HeronUnits.DescribeMillimetres(preview.MillimetresUp)),
                Json.Str("document", doc.Title),
                Json.Str("documentPath", string.IsNullOrEmpty(doc.PathName) ? null : doc.PathName),
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
        /// This is not a theoretical worry. The owner's existing Revit add-in
        /// shipped exactly this bug and fixed it in July 2026: an unguarded
        /// rollback in a catch block threw a second time, the exception escaped
        /// before the result was ever reported, and the caller waited forever
        /// on an answer that was never coming.
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
        /// CreationGUID is the document's own identity and survives being
        /// saved, renamed and moved. Title and path are carried too, so that
        /// a mismatch can be EXPLAINED to the user in words they recognise -
        /// a bare GUID comparison can refuse correctly and still leave the
        /// user with no idea which two models it is talking about.
        /// </summary>
        private static string DocumentKey(Document doc)
        {
            return doc.CreationGUID.ToString("N") + "|" + (doc.PathName ?? "") + "|" + doc.Title;
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
        /// </summary>
        private sealed class CollectWarnings : IFailuresPreprocessor
        {
            public int Count;

            public FailureProcessingResult PreprocessFailures(FailuresAccessor accessor)
            {
                var messages = accessor.GetFailureMessages();

                foreach (var message in messages)
                {
                    if (message.GetSeverity() == FailureSeverity.Error)
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
