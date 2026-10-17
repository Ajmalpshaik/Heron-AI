// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Heron.Bridge;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// What a running Heron job may do to the documents around it, and what
    /// the modeller is told about what it tried - the judgement and the
    /// sentence, with nothing Revit-shaped in it.
    ///
    /// WHY IT EXISTS. Constitution Article 10 says Heron never synchronises,
    /// publishes or shares on its own initiative, "neither is any export to a
    /// shared location", and Golden Rule 17 says the same of Sync With
    /// Central. Until this class nothing in code held either: the add-in
    /// listened to four Revit events and none of them was a save, a sync or
    /// an export, so a MODIFY fragment calling doc.Save() or an export ran
    /// with nothing in its way. HeronRunGuard listens for those now, while a
    /// job runs, and does what this decides.
    ///
    /// SPLIT OUT SO IT CAN BE PROVED WITHOUT REVIT, the move HeronFailureNote
    /// and HeronBindingNote already make: this file names no Autodesk type,
    /// so tests/Heron.RunGuard.TestHost links it BY SOURCE and there is
    /// exactly one copy of the rule.
    ///
    /// THE RULE IS DECIDED BY WHAT THE JOB IS SEEN TO DO, not by what it says
    /// it is. The add-in never sees a fragment's own declared risk - the
    /// client picks the operation from it, and a caller that skips the client
    /// can send anything (FRAGMENT-ISSUES 5b-254, D-106). What the add-in
    /// does know for certain is the OPERATION'S risk, from the tool registry
    /// by name (Golden Rule 19), and what Revit reports the job doing. So:
    ///
    ///   PUBLISH       stands aside. Saving, syncing and exporting are what a
    ///                 Publish job is FOR, the owner turned its switch on, and
    ///                 save-document and the export fragments run there
    ///   ADMIN         may start a family or project file and save it; may not
    ///                 save, sync, export or print a model that was already
    ///                 open - create-family-document does the first
    ///   ANYTHING      may not start a new document, and may not save, sync,
    ///   ELSE          export or print one that was open when the job began
    ///
    /// A DOCUMENT THE JOB ITSELF OPENED IS ITS OWN, at every level: opening a
    /// family file to read it, upgrade it or edit it in the background and
    /// saving THAT is not publishing anybody's model. A document the job
    /// CREATED is its own only where creating it was allowed - at Admin -
    /// because a file written for a job that was refused is a file on disk
    /// that no rollback reaches.
    ///
    /// A CHANGE TO ANOTHER OPEN MODEL cannot be cancelled - Revit raises it
    /// after the transaction has committed - so it is recorded, the job is
    /// refused, and the sentence says plainly that the other model's change
    /// stands. D-47 allows a job to cross models; it also says such a job
    /// must say so BEFORE it starts, and none does yet. Reading another model
    /// - transfer-views-between-documents, the pattern D-47's correction
    /// names - changes nothing there and is never caught.
    /// </summary>
    internal sealed class HeronRunPolicy
    {
        /// <summary>What a job was seen trying to send out of a model.</summary>
        internal enum Act
        {
            Save,
            SaveAs,
            Sync,
            Export,
            ExportView,
            Print,
        }

        /// <summary>
        /// Where a document came from, as far as this job is concerned. A
        /// document Heron cannot place is treated as OpenAtStart - the one
        /// that is protected - never as the job's own.
        /// </summary>
        internal enum Origin
        {
            OpenAtStart,
            OpenedByJob,
            CreatedByJob,
        }

        /// <summary>
        /// One kind of thing seen, against one document, and how often. Two
        /// views of one export arrive as two events; the modeller needs one
        /// line saying twice, not two lines.
        /// </summary>
        internal sealed class Seen
        {
            internal readonly Act What;
            internal readonly string Document;
            internal readonly bool Cancelled;
            internal int Times;

            internal Seen(Act what, string document, bool cancelled)
            {
                What = what;
                Document = document;
                Cancelled = cancelled;
                Times = 1;
            }
        }

        /// <summary>A change committed in another open model, and how often.</summary>
        internal sealed class Elsewhere
        {
            internal readonly string Document;
            internal readonly string Transactions;
            internal int Times;

            internal Elsewhere(string document, string transactions)
            {
                Document = document;
                Transactions = transactions;
                Times = 1;
            }
        }

        private readonly List<Seen> _stopped = new List<Seen>();
        private readonly List<string> _refusedDocuments = new List<string>();
        private readonly List<Elsewhere> _elsewhere = new List<Elsewhere>();
        private readonly List<string> _opened = new List<string>();
        private readonly List<string> _closed = new List<string>();
        private readonly List<KeyValuePair<string, string>> _notClosed =
            new List<KeyValuePair<string, string>>();

        public HeronRunPolicy(string operation, HeronRisk risk)
        {
            Operation = operation ?? "(none)";
            Risk = risk;
        }

        /// <summary>The operation the job arrived as, for the log.</summary>
        internal string Operation { get; private set; }

        /// <summary>That operation's risk, from the tool registry by name.</summary>
        internal HeronRisk Risk { get; private set; }

        /// <summary>
        /// Publish refuses nothing. The owner turned the Publish switch on,
        /// and a save, a sync and an export are what a Publish job exists to
        /// do - so the guard has no opinion there, and says so by recording
        /// nothing at all.
        /// </summary>
        internal bool StandsAside
        {
            get { return Risk == HeronRisk.Publish; }
        }

        // ------------------------------------------------------- the decisions

        /// <summary>
        /// Whether a save, sync, export or print about to happen is cancelled.
        ///
        /// The act does not change the answer today, and it is still passed:
        /// the record names it, and a rule that one day treats a print
        /// differently from a sync is one edit here rather than a new
        /// argument at every caller.
        /// </summary>
        internal bool Cancels(Act act, Origin origin)
        {
            if (StandsAside) return false;

            switch (origin)
            {
                case Origin.OpenedByJob:
                    return false;
                case Origin.CreatedByJob:
                    // Its own only where starting it was allowed. Under a read
                    // or a change the creation itself is refused, and a file
                    // written for a refused job is one no rollback can reach.
                    return Risk != HeronRisk.Admin;
                default:
                    return true;
            }
        }

        /// <summary>
        /// Whether starting a new family or project document is refused. Only
        /// an Admin job may - create-family-document is ADMIN - and a Publish
        /// job is not second-guessed.
        /// </summary>
        internal bool RefusesNewDocument
        {
            get { return !StandsAside && Risk != HeronRisk.Admin; }
        }

        /// <summary>
        /// Whether a committed change to this document is a change somewhere
        /// the job was not sent. The model it ran in is never "elsewhere" -
        /// its changes are what the transaction group and the gate govern -
        /// and neither is a document the job opened or created itself.
        /// </summary>
        internal bool IsElsewhere(Origin origin, bool isTarget)
        {
            if (StandsAside || isTarget) return false;
            return origin == Origin.OpenAtStart;
        }

        // -------------------------------------------------------- the record

        /// <summary>
        /// A save, sync, export or print the guard stopped - or tried to.
        /// `cancelled` false means Revit said the event could not be
        /// cancelled, or cancelling it failed, so it may have happened.
        /// </summary>
        internal void Stopped(Act act, string document, bool cancelled)
        {
            var title = Name(document);
            foreach (var one in _stopped)
            {
                if (one.What == act && one.Cancelled == cancelled
                    && string.Equals(one.Document, title, StringComparison.Ordinal))
                {
                    one.Times++;
                    return;
                }
            }
            _stopped.Add(new Seen(act, title, cancelled));
        }

        /// <summary>
        /// A new document the job started. Recorded as refused only when
        /// RefusesNewDocument says so; true when it was.
        /// </summary>
        internal bool Created(string document)
        {
            if (!RefusesNewDocument) return false;
            _refusedDocuments.Add(Name(document));
            return true;
        }

        /// <summary>A committed change to another open model.</summary>
        internal void ChangedElsewhere(string document, IList<string> transactions)
        {
            var title = Name(document);
            var named = transactions == null || transactions.Count == 0
                ? ""
                : string.Join(", ", Quoted(transactions));

            foreach (var one in _elsewhere)
            {
                if (string.Equals(one.Document, title, StringComparison.Ordinal)
                    && string.Equals(one.Transactions, named, StringComparison.Ordinal))
                {
                    one.Times++;
                    return;
                }
            }
            _elsewhere.Add(new Elsewhere(title, named));
        }

        /// <summary>A document Revit reported opening while the job ran.</summary>
        internal void Opened(string document)
        {
            _opened.Add(Name(document));
        }

        /// <summary>A background document the job left open, closed unsaved.</summary>
        internal void Closed(string document)
        {
            _closed.Add(Name(document));
        }

        /// <summary>A background document the job left open that would not close.</summary>
        internal void CouldNotClose(string document, string why)
        {
            _notClosed.Add(new KeyValuePair<string, string>(Name(document),
                string.IsNullOrEmpty(why) ? "Revit gave no reason" : why));
        }

        // ---------------------------------------------------------- verdicts

        internal IList<Seen> StoppedActs { get { return _stopped; } }
        internal IList<string> RefusedDocuments { get { return _refusedDocuments; } }
        internal IList<Elsewhere> ChangesElsewhere { get { return _elsewhere; } }
        internal IList<string> ClosedDocuments { get { return _closed; } }

        /// <summary>
        /// Did the job do anything it may not? If so it is refused, and on the
        /// write path rolled back - whatever was or was not cancelled, because
        /// a job that tried to publish is not the job that was asked for.
        /// </summary>
        internal bool Refuses
        {
            get { return _stopped.Count > 0 || _refusedDocuments.Count > 0 || _elsewhere.Count > 0; }
        }

        /// <summary>
        /// Did it change a model Heron's rollback cannot reach? The worse of
        /// the two refusals, and it carries its own error code, because the
        /// next step differs: somebody has to look at that model.
        /// </summary>
        internal bool ChangedOtherModel
        {
            get { return _elsewhere.Count > 0; }
        }

        /// <summary>
        /// Everything refused, counted one per event Revit raised - the audit
        /// line's `blocked`.
        /// </summary>
        internal int BlockedCount
        {
            get
            {
                var count = _refusedDocuments.Count;
                foreach (var one in _stopped) count += one.Times;
                foreach (var one in _elsewhere) count += one.Times;
                return count;
            }
        }

        /// <summary>Leftover background documents closed - the audit line's `closed`.</summary>
        internal int ClosedCount
        {
            get { return _closed.Count; }
        }

        /// <summary>Is there anything at all to put on the reply?</summary>
        internal bool HasReport
        {
            get
            {
                return Refuses || _opened.Count > 0 || _closed.Count > 0 || _notClosed.Count > 0;
            }
        }

        // ---------------------------------------------------------- sentences

        /// <summary>
        /// The refusal the modeller reads: what was stopped, what could not
        /// be, why, what happened to the model it ran in, and - when one was
        /// changed - what happened to the other.
        ///
        /// `modelState` is the caller's, because only the caller knows it: a
        /// group Revit confirmed rolled back, one it did not, or a read that
        /// opened none. `fragmentSaid` is the fragment's own words when it
        /// stopped too - usually because the save it called was cancelled
        /// under it - and is quoted rather than lost.
        /// </summary>
        internal string Refusal(string modelState, string fragmentSaid)
        {
            var text = new StringBuilder();
            text.Append("Heron's run guard refused this job. It ran as ")
                .Append(KindOfJob())
                .Append(", and it did something a job of that kind may not do.");

            var cancelled = new List<string>();
            var happened = new List<string>();
            foreach (var one in _stopped)
            {
                var line = Describe(one.What) + " '" + one.Document + "'" + Times(one.Times);
                if (one.Cancelled) cancelled.Add(line);
                else happened.Add(line);
            }

            if (cancelled.Count > 0)
            {
                text.Append(" Stopped before it happened: ")
                    .Append(string.Join("; ", cancelled.ToArray())).Append('.');
            }

            if (happened.Count > 0)
            {
                text.Append(" COULD NOT BE STOPPED - Revit would not let it be cancelled, so it ")
                    .Append("may already have happened: ")
                    .Append(string.Join("; ", happened.ToArray()))
                    .Append(". Check where it writes before anybody uses what is there.");
            }

            if (_stopped.Count > 0)
            {
                text.Append(" Saving, syncing, exporting and printing a model that was already ")
                    .Append("open is a Publish job, run with the Publish switch on ")
                    .Append("(Constitution Article 10) - Heron never does it as part of anything else.");
            }

            if (_refusedDocuments.Count > 0)
            {
                text.Append(" It started a new document - ")
                    .Append(string.Join(", ", Quoted(_refusedDocuments)))
                    .Append(" - and starting a family or project file is an Admin job, run with ")
                    .Append("the Admin switch on. Heron closes it unsaved as the job ends, unless a ")
                    .Append("window in Revit is showing it.");
            }

            foreach (var one in _elsewhere)
            {
                text.Append(" It changed '").Append(one.Document).Append("', another open model");
                if (one.Transactions.Length > 0) text.Append(" (").Append(one.Transactions).Append(')');
                text.Append(Times(one.Times))
                    .Append(". THAT CHANGE IS COMMITTED IN '").Append(one.Document)
                    .Append("' AND HERON CANNOT UNDO IT: Revit keeps one undo list per model, and ")
                    .Append("Heron's rollback reaches only the model this job ran in. Look at '")
                    .Append(one.Document).Append("' before saving it.");
            }

            if (!string.IsNullOrEmpty(modelState)) text.Append(' ').Append(modelState.Trim());

            if (!string.IsNullOrEmpty(fragmentSaid))
                text.Append(" What the job itself said: ").Append(fragmentSaid.Trim());

            return text.ToString();
        }

        /// <summary>
        /// Why a job that ran is not being kept, when the switch that let it
        /// through was turned off - or Heron was stopped - while it ran. Null
        /// when it may still be kept.
        ///
        /// ASKED AGAIN AT THE LAST MOMENT, with a config read fresh, because
        /// Article 13 says never cache a permission decision and the gate's
        /// answer is seconds old by the time a long job ends. A modeller who
        /// turns Changes off mid-job has done so for a reason, and keeping the
        /// change anyway is reading that switch once.
        ///
        /// The stop blocks Modify and above, exactly as the gate's does; the
        /// switches are the gate's own question, asked through
        /// HeronPermissions so the two cannot disagree on what is allowed.
        /// </summary>
        internal static string Withdrawn(HeronRisk risk, HeronConfig config, bool stopped)
        {
            if (stopped && risk >= HeronRisk.Modify)
                return "Heron was stopped while it ran";

            if (risk <= HeronPermissions.ReadOnlyCeiling) return null;

            var refusedBy = HeronPermissions.RefusedBy(risk, config);
            if (refusedBy == null) return null;

            switch (refusedBy)
            {
                case HeronPermissions.WriteEnabledKey:
                    return "the Changes switch was turned off while it ran";
                case HeronPermissions.AdminEnabledKey:
                    return "the Admin switch was turned off while it ran";
                case HeronPermissions.PublishEnabledKey:
                    return "the Publish switch was turned off while it ran";
                default:
                    return "no switch allows a job at this level any more";
            }
        }

        /// <summary>
        /// The refusal for a job Withdrawn stopped from being kept. It RAN -
        /// so never "nothing was sent to Revit", the gate's sentence, which is
        /// true only of a job refused before it started.
        /// </summary>
        internal static string WithdrawnRefusal(string fragment, string why, bool stopped,
                                                string modelState)
        {
            var text = new StringBuilder();
            text.Append('\'').Append(Name(fragment)).Append("' ran, and ").Append(why)
                .Append(", so Heron did not keep it.");
            if (!string.IsNullOrEmpty(modelState)) text.Append(' ').Append(modelState.Trim());
            text.Append(stopped
                ? " Heron stays stopped until whoever stopped it lets it work again."
                : " Turn the switch back on and send this again if you still want it kept.");
            return text.ToString();
        }

        // ------------------------------------------------------- on the reply

        /// <summary>
        /// The record, as fields on the end of a reply - every stopped act,
        /// every refused document, every change elsewhere, what was opened and
        /// what was closed. The reply comes back unchanged when there is
        /// nothing to say, or when it is not a JSON object.
        /// </summary>
        internal string Splice(string reply)
        {
            if (!HasReport || string.IsNullOrEmpty(reply)) return reply;

            var trimmed = reply.TrimEnd();
            if (!trimmed.EndsWith("}", StringComparison.Ordinal)) return reply;

            var stopped = new List<string>();
            foreach (var one in _stopped)
            {
                stopped.Add(Json.Obj(Json.Str("act", Word(one.What)),
                                     Json.Str("document", one.Document),
                                     Json.Bool("cancelled", one.Cancelled),
                                     Json.Num("times", one.Times)));
            }

            var elsewhere = new List<string>();
            foreach (var one in _elsewhere)
            {
                elsewhere.Add(Json.Obj(Json.Str("document", one.Document),
                                       Json.Str("transactions", one.Transactions),
                                       Json.Num("times", one.Times)));
            }

            var notClosed = new List<string>();
            foreach (var one in _notClosed)
            {
                notClosed.Add(Json.Obj(Json.Str("document", one.Key), Json.Str("why", one.Value)));
            }

            return trimmed.Substring(0, trimmed.Length - 1)
                 + "," + Json.Num("guardBlocked", BlockedCount)
                 + "," + Json.Arr("guardStopped", stopped)
                 + "," + Json.Arr("guardRefusedDocuments", Documents(_refusedDocuments))
                 + "," + Json.Arr("guardChangedElsewhere", elsewhere)
                 + "," + Json.Arr("guardOpened", Documents(_opened))
                 + "," + Json.Arr("guardClosed", Documents(_closed))
                 + "," + Json.Arr("guardNotClosed", notClosed) + "}";
        }

        // ------------------------------------------------------------ helpers

        /// <summary>The job, in the words a modeller would use.</summary>
        private string KindOfJob()
        {
            if (Risk <= HeronPermissions.ReadOnlyCeiling) return "a read";
            switch (Risk)
            {
                case HeronRisk.Modify: return "a change to the model";
                case HeronRisk.Admin: return "a project setup change";
                default: return "a job at the " + Risk.ToString().ToUpperInvariant() + " level";
            }
        }

        private static string Describe(Act act)
        {
            switch (act)
            {
                case Act.Save: return "a save of";
                case Act.SaveAs: return "a Save As of";
                case Act.Sync: return "a sync with central of";
                case Act.Export: return "an export from";
                case Act.ExportView: return "a view export from";
                default: return "a print of";
            }
        }

        /// <summary>The act as the reply names it - one word, never a sentence.</summary>
        private static string Word(Act act)
        {
            switch (act)
            {
                case Act.Save: return "save";
                case Act.SaveAs: return "saveAs";
                case Act.Sync: return "sync";
                case Act.Export: return "export";
                case Act.ExportView: return "exportView";
                default: return "print";
            }
        }

        private static string Times(int times)
        {
            return times > 1
                ? " (" + times.ToString(CultureInfo.InvariantCulture) + " times)"
                : "";
        }

        /// <summary>
        /// A document's title as the record keeps it. A document Revit could
        /// not name is still recorded - a hole in the record would be a
        /// refusal nobody can see the reason for.
        /// </summary>
        private static string Name(string document)
        {
            return string.IsNullOrEmpty(document) ? "(unnamed)" : document;
        }

        private static string[] Quoted(IList<string> names)
        {
            var quoted = new string[names.Count];
            for (var i = 0; i < names.Count; i++) quoted[i] = "'" + Name(names[i]) + "'";
            return quoted;
        }

        private static List<string> Documents(IList<string> titles)
        {
            var listed = new List<string>(titles.Count);
            foreach (var title in titles) listed.Add(Json.Obj(Json.Str("document", title)));
            return listed;
        }
    }
}
