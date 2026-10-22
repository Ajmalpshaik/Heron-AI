// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Which elements a fragment write added, changed and deleted, as Revit
    /// itself reported them - collected so the Keep or Discard question
    /// (D-126) can show them before anything is kept.
    ///
    /// REVIT'S OWN LIST, NOT THE FRAGMENT'S. A fragment reports what it meant
    /// to do in its `provides`; DocumentChanged reports what every committed
    /// transaction actually touched, setup steps included. The question is
    /// about the model, so it asks the model's own bookkeeping.
    ///
    /// LISTENING ONLY BETWEEN Begin AND End. RevitFragment begins after its
    /// TransactionGroup starts and ends before it isolates anything on screen,
    /// so the transaction that shows the change is never counted as part of
    /// it. Dispose ends it too, and the caller holds this in a `using`, so the
    /// handler can never outlive the run - an event handler left attached to
    /// the Application would hear every model change for the rest of the
    /// session.
    ///
    /// AND THE SHORT CHANGE REPORT (5b-407) IS BUILT ON IT, not beside it: the
    /// same events feed a HeronChangeReport, which splits the setup steps from
    /// the fragment by transaction name and keeps the counting rule where it
    /// can be proved without Revit. Describe reads each element's category
    /// once, with the group still open, and is the report the reply carries.
    ///
    /// IT READS NOTHING HEAVY INSIDE THE EVENT. Revit raises DocumentChanged
    /// while it is finishing a transaction, so the handler keeps ids and the
    /// transaction's names and returns - and NOTHING MAY ESCAPE INTO REVIT'S
    /// EVENT: one it cannot read is counted as unreadable, so the report can
    /// say its numbers are incomplete. Every member used here is the same on
    /// 2020 to 2027, read with tools/api-surface --members.
    ///
    /// WHETHER REVIT RAISES IT FOR EACH COMMIT INSIDE A GROUP, for an
    /// edit-scope fragment's own transactions and on a group's rollback is NOT
    /// KNOWN from here - NEEDS-CHECKING Group EV says so. Until it is, a report
    /// that saw no event at all says it saw nothing, never that nothing
    /// changed (HeronChangeReport.Described.Witnessed).
    /// </summary>
    internal sealed class RevitChangedSet : IDisposable
    {
        private readonly Application _application;
        private readonly Document _document;
        private readonly HashSet<ElementId> _added = new HashSet<ElementId>();
        private readonly HashSet<ElementId> _changed = new HashSet<ElementId>();
        private readonly HashSet<ElementId> _deleted = new HashSet<ElementId>();
        private bool _listening;

        private readonly HeronChangeReport _report;

        // The ids themselves, so a category is read from the ElementId Revit
        // handed over - no ElementId is ever built from a number here (its
        // constructor changed from int to long at 2024, 5b-441).
        private readonly Dictionary<string, ElementId> _ids =
            new Dictionary<string, ElementId>(StringComparer.Ordinal);

        /// <param name="label">The run's TransactionGroup name, which is what
        /// tells a setup step's commit from the fragment's own.</param>
        public RevitChangedSet(Application application, Document document, string label)
        {
            _application = application;
            _document = document;
            _report = new HeronChangeReport(label);
        }

        /// <summary>Added and still there at the end of the run.</summary>
        public ICollection<ElementId> Added { get { return _added; } }

        /// <summary>Changed, and not added by this same run.</summary>
        public ICollection<ElementId> Changed { get { return _changed; } }

        /// <summary>
        /// Deleted, and not added by this same run. Only a count here: a
        /// deleted element cannot be selected or shown while the group holds it.
        /// </summary>
        public int Deleted { get { return _deleted.Count; } }

        /// <summary>
        /// The same elements, by id - D-142's delete box rolls the run back,
        /// finds each of them in the model again and shows them, and keeps the
        /// second run only if it deletes exactly these.
        /// </summary>
        public ICollection<ElementId> DeletedIds { get { return _deleted; } }

        public void Begin()
        {
            if (_listening || _application == null) return;
            _application.DocumentChanged += OnChanged;
            _listening = true;
        }

        public void End()
        {
            _report.Seal();
            if (!_listening) return;
            _listening = false;
            try { _application.DocumentChanged -= OnChanged; }
            catch (Exception ex) { HeronApplication.Log("Could not stop listening for changes: " + ex.Message); }
        }

        public void Dispose()
        {
            End();
        }

        /// <summary>
        /// Stop listening, and read each element's category once - the one
        /// moment it can be read, with the group still open, before Assimilate
        /// or the rollback.
        /// </summary>
        public HeronChangeReport.Described Describe()
        {
            // GONE WITHOUT A WORD - row 5b-1241. An edit scope cancelled (a
            // refused stair) takes back what its Start made with no committed
            // transaction, so no event names it deleted. Whatever the model no
            // longer holds is told to the report, and taken off the lists the
            // Keep box selects from, BEFORE the report is sealed.
            foreach (var pair in _ids)
            {
                bool gone;
                try { gone = _document.GetElement(pair.Value) == null; }
                catch (Exception) { gone = false; }
                if (!gone) continue;
                _report.Vanished(pair.Key);
                if (!_added.Remove(pair.Value) && _changed.Remove(pair.Value))
                    _deleted.Add(pair.Value);
            }
            End();
            return _report.Describe(CategoryOf);
        }

        private string CategoryOf(string key)
        {
            ElementId id;
            if (!_ids.TryGetValue(key, out id)) return null;
            try
            {
                var element = _document.GetElement(id);
                return element == null || element.Category == null ? null : element.Category.Name;
            }
            catch (Exception) { return null; }
        }

        private void OnChanged(object sender, DocumentChangedEventArgs args)
        {
            try
            {
                // ANOTHER DOCUMENT IS NOT THIS RUN'S CHANGE. A fragment that edits a
                // family document and loads it back commits in two models, and an
                // id from the family would select an unrelated element here.
                if (args == null || args.Operation != UndoOperation.TransactionCommitted) return;
                var changedIn = args.GetDocument();
                if (changedIn == null || !changedIn.Equals(_document)) return;

                var added = args.GetAddedElementIds();
                var modified = args.GetModifiedElementIds();
                var deleted = args.GetDeletedElementIds();

                foreach (var id in added) _added.Add(id);

                foreach (var id in modified)
                {
                    if (!_added.Contains(id)) _changed.Add(id);
                }

                // ADDED AND THEN DELETED IN ONE RUN is no change at all, and is
                // taken off the added list rather than counted as a deletion.
                foreach (var id in deleted)
                {
                    if (_added.Remove(id)) continue;
                    _changed.Remove(id);
                    _deleted.Add(id);
                }

                _report.Committed(args.GetTransactionNames(), Keep(added), Keep(modified),
                                  Text(deleted));
            }
            catch (Exception)
            {
                _report.CouldNotRead();
            }
        }

        /// <summary>Each id as text, kept for the category read later.</summary>
        private List<string> Keep(ICollection<ElementId> ids)
        {
            var text = new List<string>();
            if (ids == null) return text;
            foreach (var id in ids)
            {
                if (id == null) continue;
                var key = id.ToString();
                _ids[key] = id;
                text.Add(key);
            }
            return text;
        }

        /// <summary>Each id as text, and nothing kept: a deleted element has no category to read.</summary>
        private static List<string> Text(ICollection<ElementId> ids)
        {
            var text = new List<string>();
            if (ids == null) return text;
            foreach (var id in ids)
            {
                if (id != null) text.Add(id.ToString());
            }
            return text;
        }
    }
}
