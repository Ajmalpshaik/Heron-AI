// Heron-Agent:  none
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Core;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Revit's Talk button - step 1 of D-104.
    ///
    /// The modeller types what they want, or presses Win+H and says it, and
    /// it goes - with what they have selected - into the Claude Code chat they
    /// already have open. Revit does not wait for the answer: this saves the
    /// message and returns, and the work comes back into the model through
    /// Heron's tools, the pipe and the ExternalEvent, exactly as it does when
    /// the words are typed in the chat.
    ///
    /// IT RUNS IN THE API CONTEXT, like every ribbon command, so reading the
    /// selection here is safe. The window it opens is modal and touches no
    /// API; the mailbox that saves the message touches no API either. This
    /// file is the only part of Talk that knows Revit exists.
    ///
    /// IT CHANGES NOTHING in the model and opens no transaction.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public sealed class TalkCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null || uidoc.Document == null)
            {
                message = "Open a model first. Talk sends what you say about the model in front of you, " +
                          "so there has to be one. Nothing was sent.";
                return Result.Failed;
            }

            // THE CHAT REACHES THIS REVIT THROUGH THE BRIDGE, so a Revit that
            // is not connected cannot be worked on, whatever is said about it.
            // Connecting stays a deliberate act (docs/00e) - this says so
            // rather than doing it on the modeller's behalf.
            var bridge = HeronApplication.Bridge;
            if (bridge == null || !bridge.IsRunning)
            {
                TaskDialog.Show("Heron",
                    "This Revit is not connected, so the chat could not work on it.\n\n" +
                    "Press  Heron > AI Bridge > Heron  on the ribbon first, then Talk. Nothing was sent.");
                return Result.Cancelled;
            }

            var selection = HeronTalkSelection.Read(uidoc);
            var mailbox = new HeronTalkMailbox(
                HeronPaths.Talk,
                Process.GetCurrentProcess().Id,
                uiapp.Application.VersionNumber);

            var sent = HeronTalkWindow.Ask(
                HeronTalkMailbox.Describe(selection),
                delegate(string text) { return Send(mailbox, text, selection); },
                HeronApplication.Log);

            if (sent == null)
            {
                message = "The Talk window could not open, so nothing was sent. The log that says why is in " +
                          HeronApplication.LogDirectory + ".";
                return Result.Failed;
            }

            return sent == true ? Result.Succeeded : Result.Cancelled;
        }

        /// <summary>
        /// Saves one message. NULL when it was saved, or the sentence the
        /// window shows when it was not - and every such sentence says that
        /// nothing was sent, because "did it half-go?" is the first question.
        /// </summary>
        private static string Send(HeronTalkMailbox mailbox, string text, TalkSelection selection)
        {
            if (!mailbox.SomeoneListening(DateTime.UtcNow))
            {
                return "No chat is listening, so nothing was sent. Start Claude with Heron Talk - " +
                       "mcp\\heron-talk.cmd in the Heron folder - then press Send again. Your words are still here.";
            }

            try
            {
                var number = mailbox.Send(text, selection, DateTime.UtcNow);
                HeronApplication.Log(string.Format(CultureInfo.InvariantCulture,
                    "Talk: message {0} saved for the chat, with {1} selected in {2}.",
                    number, selection.Total, selection.Document));
                return null;
            }
            catch (Exception ex)
            {
                HeronApplication.Log("Talk: could not save a message: " + ex);
                return "Heron could not save the message, so nothing was sent. Windows said: " + ex.Message;
            }
        }
    }

    /// <summary>
    /// What is selected, read once, as text - the only Revit reading Talk does.
    /// </summary>
    internal static class HeronTalkSelection
    {
        internal static TalkSelection Read(UIDocument uidoc)
        {
            var doc = uidoc.Document;
            var selection = new TalkSelection
            {
                Document = doc.Title,
                DocumentPath = doc.PathName,
            };

            try
            {
                var view = uidoc.ActiveView;
                selection.View = view == null ? "" : view.Name;
            }
            catch
            {
                selection.View = "";
            }

            ICollection<ElementId> ids;
            try
            {
                ids = uidoc.Selection.GetElementIds();
            }
            catch (Exception ex)
            {
                // A selection that cannot be read is sent as "nothing
                // selected" rather than stopping the modeller from talking at
                // all - and the log says which it was.
                HeronApplication.Log("Talk: the selection could not be read: " + ex.Message);
                return selection;
            }
            if (ids == null) return selection;

            selection.Total = ids.Count;
            foreach (var id in ids)
            {
                if (selection.Elements.Count >= HeronTalkMailbox.MaxSaved) break;

                Element element;
                try { element = doc.GetElement(id); }
                catch { element = null; }
                if (element == null)
                {
                    selection.Unreadable++;
                    continue;
                }

                selection.Elements.Add(new TalkElement
                {
                    // ToString, not IntegerValue: IntegerValue is gone in 2026
                    // and ToString is on every release 2020-2027.
                    Id = id.ToString(),
                    UniqueId = element.UniqueId,
                    Category = CategoryName(element),
                    Name = NameOf(element),
                });
            }
            return selection;
        }

        private static string CategoryName(Element element)
        {
            try
            {
                var category = element.Category;
                return category == null ? "" : category.Name;
            }
            catch
            {
                return "";
            }
        }

        private static string NameOf(Element element)
        {
            try { return element.Name ?? ""; }
            catch { return ""; }
        }
    }
}
