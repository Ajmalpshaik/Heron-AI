// Heron-Agent:  HERON-REVIT-LNK-015
// Heron-Step:   4
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Heron.Bridge;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Linked models. HERON-REVIT-LNK-015, and the register's row is short:
    /// "read only; never modifies a link's source".
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit. It COMPILES on 2020 through 2027
    /// and that is the whole of what is known about it. NEEDS-CHECKING group
    /// L is where it stops being a claim.
    /// ===================================================================
    ///
    /// THE ONE THING A MODELLER NEEDS TO KNOW ABOUT LINKS
    /// ---------------------------------------------------
    /// Elements in a linked model ARE NOT IN THE HOST DOCUMENT. A
    /// FilteredElementCollector over the host never sees them, so
    /// `count_elements` saying "412 ducts" in a project whose ductwork is
    /// linked in means 412 of the host's own ducts and none of anybody
    /// else's - which, on a real MEP job, is very often zero out of several
    /// thousand.
    ///
    /// That is not a defect in the counter. It is what a link IS. But a
    /// number given without it is a number a person will act on, so this
    /// operation reports the host count beside every link's own count, and
    /// says in words that the two were never added together.
    ///
    /// NOTHING IS OPENED, LOADED OR UNLOADED
    /// ---------------------------------------
    /// Risk READ. `GetLinkDocument()` returns the document Revit ALREADY has
    /// in memory for a loaded link, or null - it loads nothing. A link that
    /// is unloaded stays unloaded, and its element count comes back as "not
    /// known" rather than as zero, because reloading somebody's link to
    /// answer a question is exactly the modification this row forbids.
    ///
    /// A MISSING LINK IS A NORMAL ANSWER
    /// -----------------------------------
    /// Links break constantly on real jobs - a consultant renames a folder,
    /// a file moves off a shared drive, somebody opens a central file
    /// detached. Revit already knows: `LinkedFileStatus` says so. It is
    /// reported as the status it is, never flattened into "0 elements".
    /// </summary>
    internal static class RevitLinks
    {
        /// <summary>
        /// Every linked model in the active document, loaded or not.
        ///
        /// Two lists, because they are two different facts. A link TYPE is
        /// the reference to a file; a link INSTANCE is one placement of it.
        /// One type can be placed several times - a repeated floor, a
        /// mirrored wing - and "how many links are there" has two right
        /// answers depending on which is meant, so both are given.
        /// </summary>
        public static string List(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            // Placements first, keyed by the type they place, so each type's
            // record can say how many times it appears without a second pass.
            var placements = new Dictionary<string, int>(StringComparer.Ordinal);
            var instances = new List<Element>();
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(RevitLinkInstance)))
            {
                var instance = element as RevitLinkInstance;
                if (instance == null) continue;
                instances.Add(instance);

                var owner = KeyOf(doc, instance.GetTypeId());
                if (owner == null) continue;
                int already;
                placements[owner] = placements.TryGetValue(owner, out already)
                                    ? already + 1 : 1;
            }

            var rows = new List<string>();
            var loaded = 0;
            var counted = 0;
            long inLinks = 0;

            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(typeof(RevitLinkType)))
            {
                var type = element as RevitLinkType;
                if (type == null) continue;

                var key = type.UniqueId;
                int placed;
                if (!placements.TryGetValue(key, out placed)) placed = 0;

                var status = StatusOf(doc, type);
                if (status == "loaded") loaded++;

                // The linked document only if Revit already has it. One
                // instance is enough - every placement of a type shares the
                // same document, and opening one is not something this code
                // can do anyway.
                Document linked = null;
                foreach (var each in instances)
                {
                    var instance = (RevitLinkInstance)each;
                    if (!string.Equals(KeyOf(doc, instance.GetTypeId()), key,
                                      StringComparison.Ordinal)) continue;
                    linked = instance.GetLinkDocument();
                    if (linked != null) break;
                }

                var fields = new List<string>
                {
                    Json.Str("name", SafeName(type)),
                    Json.Str("status", status),
                    Json.Num("placements", placed),
                    Json.Bool("nested", IsNested(type)),
                    Json.Str("path", PathOf(doc, type)),
                };

                if (linked != null)
                {
                    var own = new FilteredElementCollector(linked)
                                  .WhereElementIsNotElementType()
                                  .GetElementCount();
                    fields.Add(Json.Num("elements", own));
                    fields.Add(Json.Str("title", linked.Title));
                    inLinks += own;
                    counted++;
                }
                else
                {
                    // NOT ZERO. Zero is a count somebody took; this is a
                    // count nobody could take, and the two must not read
                    // the same in an answer a person acts on.
                    fields.Add(Json.Str("elements", null));
                    fields.Add(Json.Str("elementsWhy",
                        "the link is not loaded in this session, and reading " +
                        "it would mean loading it"));
                }

                rows.Add(Json.Obj(fields.ToArray()));
            }

            var host = new FilteredElementCollector(doc)
                           .WhereElementIsNotElementType()
                           .GetElementCount();

            return Json.Ok(
                Json.Arr("links", rows),
                Json.Num("linkTypes", rows.Count),
                Json.Num("placements", instances.Count),
                Json.Num("loaded", loaded),
                Json.Num("hostElements", host),
                Json.Num("linkedElements", inLinks),
                Json.Num("countedLinks", counted),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads",
                    rows.Count == 0
                    ? "this model has no linked models"
                    : "linked models, read only. THE TWO COUNTS ARE NOT ADDED "
                      + "TOGETHER: hostElements is what this model contains "
                      + "and what count_elements reports; linkedElements is "
                      + "what the loaded links contain and is invisible to "
                      + "every filter over the host. "
                      + (counted == rows.Count
                         ? "Every link was loaded."
                         : "Only " + counted.ToString(
                               System.Globalization.CultureInfo.InvariantCulture)
                           + " of " + rows.Count.ToString(
                               System.Globalization.CultureInfo.InvariantCulture)
                           + " links could be counted; the rest are not loaded "
                           + "and were left alone.")));
        }

        /// <summary>
        /// What Revit says about the file behind a link.
        ///
        /// Read from the external file reference rather than inferred from
        /// whether GetLinkDocument() returned something. "Not loaded" and
        /// "the file is gone" are different problems with different fixes,
        /// and a null document cannot tell them apart.
        /// </summary>
        private static string StatusOf(Document doc, RevitLinkType type)
        {
            try
            {
                if (!ExternalFileUtils.IsExternalFileReference(doc, type.Id))
                {
                    return "not an external reference";
                }

                var reference = ExternalFileUtils.GetExternalFileReference(doc, type.Id);
                if (reference == null) return "unknown";

                switch (reference.GetLinkedFileStatus())
                {
                    case LinkedFileStatus.Loaded: return "loaded";
                    case LinkedFileStatus.Unloaded: return "unloaded";
                    case LinkedFileStatus.NotFound: return "not found";
                    case LinkedFileStatus.LocallyUnloaded: return "unloaded on this machine";
                    case LinkedFileStatus.InClosedWorkset: return "in a closed workset";
                    case LinkedFileStatus.Invalid: return "invalid";
                    default: return "unknown";
                }
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                // A link whose reference cannot be read is still a link, and
                // dropping the row would under-report the model. Golden Rule
                // 14: never silently discard.
                return "unknown";
            }
        }

        /// <summary>
        /// The path the user would see in Manage Links, or null.
        ///
        /// ConvertModelPathToUserVisiblePath is what turns a server path into
        /// the RSN:// or BIM 360 form a person recognises. A raw ModelPath
        /// printed straight out means nothing to anybody.
        /// </summary>
        private static string PathOf(Document doc, RevitLinkType type)
        {
            try
            {
                if (!ExternalFileUtils.IsExternalFileReference(doc, type.Id)) return null;

                var reference = ExternalFileUtils.GetExternalFileReference(doc, type.Id);
                if (reference == null) return null;

                var path = reference.GetAbsolutePath();
                if (path == null) return null;

                var shown = ModelPathUtils.ConvertModelPathToUserVisiblePath(path);
                return string.IsNullOrEmpty(shown) ? null : shown;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        /// <summary>
        /// Whether this link arrived through another link.
        ///
        /// A nested link is not something the user attached to THIS model, so
        /// "remove it" and "reload it" are answered in a different file by a
        /// different person. Saying which is which costs one property.
        /// </summary>
        private static bool IsNested(RevitLinkType type)
        {
            try
            {
                return type.IsNestedLink;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return false;
            }
        }

        /// <summary>A link's name, never an exception mid-answer.</summary>
        private static string SafeName(Element element)
        {
            try
            {
                return string.IsNullOrEmpty(element.Name) ? "(unnamed)" : element.Name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return "(unnamed)";
            }
        }

        /// <summary>
        /// A link type's stable key, as a string.
        ///
        /// IT IS UniqueId AND NOT THE INTEGER, AND THE COMPILER IS WHY.
        /// This started as ElementId.IntegerValue with a comment saying it
        /// was "the property every version has had". Building against the
        /// 2026 and 2027 reference assemblies says otherwise:
        ///
        ///     error CS1061: 'ElementId' does not contain a definition
        ///                   for 'IntegerValue'
        ///
        /// It is not deprecated there - it is GONE, and 2020 through 2025
        /// compiled the same line without complaint. That is D-05 exactly:
        /// never extrapolate across releases, and it is also why
        /// tools/check-compile.py runs all eight rather than one.
        ///
        /// UniqueId is a string on every supported release, it is what
        /// survives a save and reopen, and these keys live for the length of
        /// one call.
        /// </summary>
        private static string KeyOf(Document doc, ElementId id)
        {
            if (doc == null || id == null) return null;
            var element = doc.GetElement(id);
            return element == null ? null : element.UniqueId;
        }
    }
}
