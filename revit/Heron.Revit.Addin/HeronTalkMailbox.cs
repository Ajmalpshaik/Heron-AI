// Heron-Agent:  none
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Heron.Bridge;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>One element of a saved selection, as text.</summary>
    internal sealed class TalkElement
    {
        /// <summary>
        /// The ElementId as text. ToString() is on every release 2020-2027;
        /// IntegerValue is not, and the constructor changed at 2024.
        /// </summary>
        internal string Id;

        /// <summary>The id that survives copy, sync and reopening (docs/03 s7).</summary>
        internal string UniqueId;

        internal string Category;
        internal string Name;
    }

    /// <summary>What was selected at the moment the modeller pressed Send.</summary>
    internal sealed class TalkSelection
    {
        internal string Document;
        internal string DocumentPath;
        internal string View;

        /// <summary>How many were selected - which can be more than were saved.</summary>
        internal int Total;

        /// <summary>
        /// Selected, and gone by the time they were read - deleted, or in a
        /// model that changed underneath. Counted, never listed: there is
        /// nothing left to name.
        /// </summary>
        internal int Unreadable;

        internal readonly List<TalkElement> Elements = new List<TalkElement>();
    }

    /// <summary>
    /// The short memory behind Revit's Talk button - D-104.
    ///
    /// WHAT IT DOES. It writes what the modeller typed or said, and what they
    /// had selected, into Heron's derived folder, where the chat that listens
    /// picks it up and pushes it into the open Claude Code session. Two files
    /// per message:
    ///
    ///     talk\&lt;revit pid&gt;\selection-7.json   every element: id, UniqueId, category, name
    ///     talk\&lt;revit pid&gt;\message-7.json     the words, the model, the view, and only
    ///                                         the COUNT and the categories
    ///
    /// The message stays small however much is selected, and the chat reads
    /// the list only when it needs it - from this file, so reading it touches
    /// neither the model nor the pipe and can never interrupt the modeller.
    ///
    /// WHY FILES AND NOT THE PIPE. The bridge hands its pipe to the newest
    /// connection (docs/25). Anything polling it for messages would cut off
    /// the chat's own requests - and batch-prove's - in the middle of a reply.
    /// A file is written once, read once, and disturbs nobody.
    ///
    /// THE MESSAGE IS WRITTEN LAST, and atomically, because its appearance is
    /// the signal. The selection it names is already complete on disk by
    /// then, so a listener can never find a message whose list is still being
    /// written. The listener claims a message by renaming it, which is what
    /// makes exactly one chat take it.
    ///
    /// IT TOUCHES NO REVIT API. What was selected arrives already read, from
    /// HeronTalk.cs, so this file compiles into tests/Heron.Talk.TestHost and
    /// the format it writes is checked against the Python that reads it
    /// (tests/test_talk_contract.py). The format is FORMAT below; the reader
    /// is mcp/server/heron_talk.py, and the two change together or not at all.
    /// </summary>
    internal sealed class HeronTalkMailbox
    {
        /// <summary>The file format. heron_talk.py refuses one it does not know.</summary>
        internal const int Format = 1;

        /// <summary>
        /// The most elements one selection saves. Far more than a modeller
        /// selects by hand; the cap exists so a select-all on a large model
        /// cannot hold Revit's thread while a list of a million is written.
        /// The count is always the true one, and the file says when the list
        /// stopped short.
        /// </summary>
        internal const int MaxSaved = 50000;

        /// <summary>The longest message the Talk window accepts.</summary>
        internal const int MaxText = 4000;

        /// <summary>How many messages each Revit remembers. Older ones are forgotten.</summary>
        internal const int Keep = 20;

        /// <summary>
        /// How recently a listening chat must have said so. The listener
        /// refreshes its file every few seconds (heron_talk.HEARTBEAT_S), so a
        /// file older than this belongs to a chat that has closed.
        /// </summary>
        internal static readonly TimeSpan ListenerFresh = TimeSpan.FromSeconds(20);

        private static readonly Regex Numbered =
            new Regex(@"^(message|selection)-(\d+)(\.taken)?\.json$",
                      RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private readonly string _root;
        private readonly int _revitPid;
        private readonly string _revitVersion;

        internal HeronTalkMailbox(string root, int revitPid, string revitVersion)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentException("A talk folder is needed.", "root");
            _root = root;
            _revitPid = revitPid;
            _revitVersion = revitVersion ?? "";
        }

        /// <summary>This Revit's own folder. Named by process, like the bridge's pipe.</summary>
        internal string Folder
        {
            get { return Path.Combine(_root, _revitPid.ToString(CultureInfo.InvariantCulture)); }
        }

        /// <summary>
        /// Is a chat listening? Only a chat started for Talk writes a listener
        /// file, and one that is not listening would never collect a message -
        /// so the button refuses rather than leaving words nobody will read.
        /// </summary>
        internal bool SomeoneListening(DateTime nowUtc)
        {
            try
            {
                if (!Directory.Exists(_root)) return false;
                foreach (var file in Directory.GetFiles(_root, "listener-*.json"))
                {
                    var age = nowUtc - File.GetLastWriteTimeUtc(file);
                    if (age <= ListenerFresh) return true;
                }
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            return false;
        }

        /// <summary>
        /// Saves the selection, then the message, and returns the message's
        /// number. Throws when the disk refuses - the caller says so, and says
        /// that nothing was sent.
        /// </summary>
        internal int Send(string text, TalkSelection selection, DateTime nowUtc)
        {
            if (text == null) text = "";
            text = text.Trim();
            if (text.Length == 0) throw new ArgumentException("There is nothing to send.", "text");
            if (text.Length > MaxText) text = text.Substring(0, MaxText);
            if (selection == null) selection = new TalkSelection();

            var folder = Folder;
            Directory.CreateDirectory(folder);

            var number = NextNumber(folder);
            var stamp = nowUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
            var categories = CategoryFields(selection);
            var hasSelection = selection.Total > 0;

            if (hasSelection)
            {
                var elements = new List<string>(selection.Elements.Count);
                foreach (var element in selection.Elements)
                {
                    if (element == null) continue;
                    elements.Add(Json.Obj(
                        Json.Str("id", element.Id ?? ""),
                        Json.Str("uniqueId", element.UniqueId ?? ""),
                        Json.Str("category", element.Category ?? ""),
                        Json.Str("name", element.Name ?? "")));
                }

                HeronAtomicWrite.WriteAllText(
                    Path.Combine(folder, "selection-" + Text(number) + ".json"),
                    Json.Obj(
                        Json.Num("format", Format),
                        Json.Num("selection", number),
                        Json.Str("takenUtc", stamp),
                        Json.Num("revitPid", _revitPid),
                        Json.Str("revitVersion", _revitVersion),
                        Json.Str("document", selection.Document ?? ""),
                        Json.Str("documentPath", selection.DocumentPath ?? ""),
                        Json.Str("view", selection.View ?? ""),
                        Json.Num("total", selection.Total),
                        Json.Num("saved", elements.Count),
                        Json.Num("unreadable", selection.Unreadable),
                        // STOPPED AT MaxSaved - which is a different thing
                        // from an element that could not be read, and the
                        // reader says which.
                        Json.Bool("truncated", elements.Count + selection.Unreadable < selection.Total),
                        Json.Arr("categories", categories),
                        Json.Arr("elements", elements)) + "\n");
            }

            // LAST, because its appearance is what a listener waits for.
            HeronAtomicWrite.WriteAllText(
                Path.Combine(folder, "message-" + Text(number) + ".json"),
                Json.Obj(
                    Json.Num("format", Format),
                    Json.Num("message", number),
                    Json.Str("text", text),
                    Json.Str("sentUtc", stamp),
                    Json.Num("revitPid", _revitPid),
                    Json.Str("revitVersion", _revitVersion),
                    Json.Str("document", selection.Document ?? ""),
                    Json.Str("documentPath", selection.DocumentPath ?? ""),
                    Json.Str("view", selection.View ?? ""),
                    hasSelection ? Json.Num("selection", number) : "\"selection\": null",
                    Json.Num("selected", selection.Total),
                    Json.Arr("categories", categories)) + "\n");

            Prune(folder, number);
            return number;
        }

        /// <summary>
        /// One line a person can read: which model, which view, and what is
        /// selected. Shown in the Talk window so the modeller sees what will
        /// travel with their words before they send it.
        /// </summary>
        internal static string Describe(TalkSelection selection)
        {
            if (selection == null) return "";
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(selection.Document)) parts.Add(selection.Document);
            if (!string.IsNullOrEmpty(selection.View)) parts.Add(selection.View);

            if (selection.Total == 0)
            {
                parts.Add("nothing selected");
            }
            else
            {
                var counts = CountByCategory(selection);
                var named = new List<string>();
                foreach (var pair in counts)
                {
                    if (named.Count == 3) break;
                    named.Add(pair.Key + " " + Text(pair.Value));
                }
                var more = counts.Count > 3 ? ", and " + Text(counts.Count - 3) + " more" : "";
                parts.Add(Text(selection.Total) + " selected: " + string.Join(", ", named.ToArray()) + more);
            }
            return string.Join("  ·  ", parts.ToArray());
        }

        /// <summary>
        /// The short memory ends when this Revit closes. Derived state, so
        /// removing it is always safe - and the guard is asked anyway, because
        /// a delete that trusts its own path is how a cleanup reaches the
        /// wrong folder.
        /// </summary>
        internal static void Forget(string root, int revitPid)
        {
            if (string.IsNullOrEmpty(root)) return;
            var folder = Path.Combine(root, revitPid.ToString(CultureInfo.InvariantCulture));
            try
            {
                if (!Directory.Exists(folder)) return;
                if (!HeronPaths.IsSafeToDelete(folder)) return;
                Directory.Delete(folder, true);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        // -------------------------------------------------------------- inside

        /// <summary>Categories, most first, then by name - so the order is stable.</summary>
        internal static List<KeyValuePair<string, int>> CountByCategory(TalkSelection selection)
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var element in selection.Elements)
            {
                if (element == null) continue;
                var name = string.IsNullOrEmpty(element.Category) ? "(no category)" : element.Category;
                int seen;
                counts.TryGetValue(name, out seen);
                counts[name] = seen + 1;
            }
            var list = new List<KeyValuePair<string, int>>(counts);
            list.Sort(delegate(KeyValuePair<string, int> a, KeyValuePair<string, int> b)
            {
                var byCount = b.Value.CompareTo(a.Value);
                return byCount != 0 ? byCount : string.CompareOrdinal(a.Key, b.Key);
            });
            return list;
        }

        private static List<string> CategoryFields(TalkSelection selection)
        {
            var fields = new List<string>();
            foreach (var pair in CountByCategory(selection))
                fields.Add(Json.Obj(Json.Str("name", pair.Key), Json.Num("count", pair.Value)));
            return fields;
        }

        /// <summary>
        /// One more than any number this Revit has used - claimed messages
        /// included, so a number is never handed out twice while it runs.
        /// </summary>
        private static int NextNumber(string folder)
        {
            var highest = 0;
            foreach (var file in Directory.GetFiles(folder, "*.json"))
            {
                int number;
                if (TryNumber(Path.GetFileName(file), out number) && number > highest) highest = number;
            }
            return highest + 1;
        }

        /// <summary>Forgets everything older than the last Keep messages.</summary>
        private static void Prune(string folder, int newest)
        {
            var oldestKept = newest - Keep + 1;
            if (oldestKept <= 1) return;
            try
            {
                foreach (var file in Directory.GetFiles(folder, "*.json"))
                {
                    int number;
                    if (!TryNumber(Path.GetFileName(file), out number)) continue;
                    if (number < oldestKept) File.Delete(file);
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static bool TryNumber(string name, out int number)
        {
            number = 0;
            var match = Numbered.Match(name ?? "");
            return match.Success &&
                   int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out number);
        }

        private static string Text(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
