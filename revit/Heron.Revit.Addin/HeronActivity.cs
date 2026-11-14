// Heron-Agent:  none
// Heron-Step:   7
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Heron.Bridge;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// What the Activity pane lists - Heron's recent changes, read from the
    /// run records the dispatcher keeps (row 5b-407) - with nothing Revit- or
    /// WPF-shaped in it, so tests/Heron.Activity.TestHost runs it without
    /// either. D-163.
    ///
    /// THE RECORD IS THE SOURCE, NOT A SECOND LIST. Every change a chat makes
    /// through revit_change already writes one, whole, under its workflow id;
    /// a pane that kept its own list would be a second account of the same
    /// work that could disagree with the first. A record from an add-in before
    /// D-163 has no top-level `kind` and reads as "outcome not recorded" -
    /// never guessed.
    /// </summary>
    internal static class HeronActivity
    {
        /// <summary>The most changes the pane lists - the newest first.</summary>
        internal const int MostShown = 15;

        /// <summary>The most element ids one row offers to select.</summary>
        internal const int MostIds = 500;

        /// <summary>One change, as the pane draws it.</summary>
        internal sealed class Row
        {
            internal string Run;
            internal string Fragment;
            internal string Document;
            internal DateTime? AtUtc;
            internal string Kind;
            internal int Added;
            internal int Modified;
            internal int Deleted;
            internal readonly List<string> Ids = new List<string>();
            internal int IdsTotal;

            /// <summary>Whether this change is in the model now - kept, as far as Heron knows.</summary>
            internal bool Kept { get { return Kind == "kept"; } }

            /// <summary>The outcome in a modeller's words.</summary>
            internal string Outcome
            {
                get
                {
                    switch (Kind)
                    {
                        case "kept": return "Kept";
                        case "rolledBack": return "Tried, nothing kept";
                        case "discarded": return "Discarded by you";
                        case "unchanged": return "Nothing changed";
                        case "notHeld": return "Rollback did not hold - look at the model";
                        case "notConfirmed": return "Rollback not confirmed - look at the model";
                        default: return "Outcome not recorded";
                    }
                }
            }

            /// <summary>"2 added, 5 changed, 1 deleted" - or "no element counted".</summary>
            internal string Counts
            {
                get
                {
                    var parts = new List<string>();
                    if (Added > 0) parts.Add(Added + " added");
                    if (Modified > 0) parts.Add(Modified + " changed");
                    if (Deleted > 0) parts.Add(Deleted + " deleted");
                    return parts.Count == 0 ? "no element counted" : string.Join(", ", parts.ToArray());
                }
            }
        }

        /// <summary>
        /// The newest `most` records in `folder`, newest first. A record that
        /// will not read is left out, never a reason the pane fails. Newest by
        /// the workflow id itself, which starts with its UTC time - no file's
        /// clock is trusted for it.
        /// </summary>
        internal static List<Row> Read(string folder, int most = MostShown)
        {
            var rows = new List<Row>();
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return rows;

            string[] files;
            try { files = Directory.GetFiles(folder, "wf-*.json"); }
            catch (Exception) { return rows; }
            Array.Sort(files, StringComparer.Ordinal);
            Array.Reverse(files);

            foreach (var file in files)
            {
                if (rows.Count >= most) break;
                try
                {
                    var row = Parse(File.ReadAllText(file));
                    if (row != null) rows.Add(row);
                }
                catch (Exception) { }
            }
            return rows;
        }

        /// <summary>One record's text as a row, or null when it is not a run record.</summary>
        internal static Row Parse(string json)
        {
            if (Json.ReadLong(json, "record") != 1) return null;
            var row = new Row
            {
                Run = Json.ReadString(json, "workflow"),
                Fragment = Json.ReadString(json, "fragment") ?? "(a change)",
                Document = Json.ReadString(json, "document") ?? "",
                Kind = Json.ReadString(json, "kind") ?? "",
            };
            DateTime at;
            if (DateTime.TryParse(Json.ReadString(json, "at"), CultureInfo.InvariantCulture,
                                  DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out at))
                row.AtUtc = at;

            var elements = Json.ReadObjectArray(json, "elements");
            if (elements != null)
            {
                foreach (var element in elements)
                {
                    string change, id;
                    element.TryGetValue("change", out change);
                    element.TryGetValue("id", out id);
                    if (change == "added") row.Added++;
                    else if (change == "modified") row.Modified++;
                    else if (change == "deleted") row.Deleted++;
                    else continue;

                    // A DELETED ELEMENT IS NOT OFFERED: it is not in the model
                    // to select. Added and changed ones are, up to MostIds.
                    if (change == "deleted" || string.IsNullOrEmpty(id)) continue;
                    row.IdsTotal++;
                    if (row.Ids.Count < MostIds) row.Ids.Add(id);
                }
            }
            return row;
        }

        /// <summary>
        /// The one row that may offer Undo: the NEWEST change, only when it was
        /// kept. Whether it is still the top of Revit's undo list is the
        /// add-in's undo guard's to say when Undo is pressed (D-157) - this only
        /// decides where the button is shown.
        /// </summary>
        internal static Row Undoable(IList<Row> rows)
        {
            return rows != null && rows.Count > 0 && rows[0].Kept ? rows[0] : null;
        }

        /// <summary>The request Select or Zoom sends for a row (D-163's show_elements).</summary>
        internal static string ShowRequest(Row row, bool zoom)
        {
            return Json.Obj(
                Json.Str("op", "show_elements"),
                Json.Str("ids", string.Join(",", row.Ids.ToArray())),
                Json.Str("zoom", zoom ? "true" : "false"),
                Json.Str("document", row.Document),
                // NEVER TAKES THE MODEL FROM A CHAT (D-141): a look only.
                Json.Str("claim", "no"));
        }

        /// <summary>The request Undo sends - D-157's undo_last, a look at the hold only.</summary>
        internal static string UndoRequest()
        {
            return Json.Obj(Json.Str("op", "undo_last"), Json.Str("claim", "no"));
        }

        /// <summary>The words a reply ends in on the pane's status line.</summary>
        internal static string Said(string reply)
        {
            if (string.IsNullOrEmpty(reply)) return "Revit did not answer.";
            return Json.ReadString(reply, "said") ?? Json.ReadString(reply, "message")
                   ?? Json.ReadString(reply, "error") ?? "Done.";
        }
    }
}
