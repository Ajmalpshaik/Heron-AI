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
    /// (D-118) can show them before anything is kept.
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
    /// MINIMAL ON PURPOSE. It keeps the ids and nothing else, so a later short
    /// change report can be built on it rather than beside it.
    /// </summary>
    internal sealed class RevitChangedSet : IDisposable
    {
        private readonly Application _application;
        private readonly Document _document;
        private readonly HashSet<ElementId> _added = new HashSet<ElementId>();
        private readonly HashSet<ElementId> _changed = new HashSet<ElementId>();
        private int _deleted;
        private bool _listening;

        public RevitChangedSet(Application application, Document document)
        {
            _application = application;
            _document = document;
        }

        /// <summary>Added and still there at the end of the run.</summary>
        public ICollection<ElementId> Added { get { return _added; } }

        /// <summary>Changed, and not added by this same run.</summary>
        public ICollection<ElementId> Changed { get { return _changed; } }

        /// <summary>
        /// Deleted, and not added by this same run. Only a count: a deleted
        /// element cannot be selected or shown.
        /// </summary>
        public int Deleted { get { return _deleted; } }

        public void Begin()
        {
            if (_listening || _application == null) return;
            _application.DocumentChanged += OnChanged;
            _listening = true;
        }

        public void End()
        {
            if (!_listening) return;
            _listening = false;
            try { _application.DocumentChanged -= OnChanged; }
            catch (Exception ex) { HeronApplication.Log("Could not stop listening for changes: " + ex.Message); }
        }

        public void Dispose()
        {
            End();
        }

        private void OnChanged(object sender, DocumentChangedEventArgs args)
        {
            // ANOTHER DOCUMENT IS NOT THIS RUN'S CHANGE. A fragment that edits a
            // family document and loads it back commits in two models, and an
            // id from the family would select an unrelated element here.
            if (args == null || args.Operation != UndoOperation.TransactionCommitted) return;
            var changedIn = args.GetDocument();
            if (changedIn == null || !changedIn.Equals(_document)) return;

            foreach (var id in args.GetAddedElementIds()) _added.Add(id);

            foreach (var id in args.GetModifiedElementIds())
            {
                if (!_added.Contains(id)) _changed.Add(id);
            }

            // ADDED AND THEN DELETED IN ONE RUN is no change at all, and is
            // taken off the added list rather than counted as a deletion.
            foreach (var id in args.GetDeletedElementIds())
            {
                if (_added.Remove(id)) continue;
                _changed.Remove(id);
                _deleted++;
            }
        }
    }
}
