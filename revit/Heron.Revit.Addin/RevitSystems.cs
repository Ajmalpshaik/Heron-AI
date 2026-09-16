// Heron-Agent:  HERON-REVIT-SYS-030
// Heron-Step:   4
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  revit
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Heron.Bridge;

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Duct and pipe systems, and the elements that are not on one.
    /// HERON-REVIT-SYS-030, whose row ends "including unconnected
    /// elements" - which is the half a modeller actually loses days to.
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit. It COMPILES on 2020 through 2027
    /// and that is the whole of what is known about it. Nothing below has
    /// been in front of a model.
    /// ===================================================================
    ///
    /// WHY A MODELLER CARES, IN ONE SENTENCE
    /// --------------------------------------
    /// A duct run that LOOKS joined on screen and is not connected carries
    /// no flow, appears on no system, and every number downstream of it is
    /// quietly short: the schedule prints, the sizing calculates, the
    /// system browser shows a tidy tree, and none of them mentions the
    /// element that is sitting there unattached.
    ///
    /// On a federated Qatar job that is the difference between a riser
    /// that balances and one that is discovered on site.
    ///
    /// AN OPEN CONNECTOR IS NOT A FAULT, AND THIS FILE DOES NOT SAY IT IS
    /// -------------------------------------------------------------------
    /// The end of every run is an open connector and is supposed to be. A
    /// duct stub waiting for next week's coordination is an open connector
    /// on purpose. So the answer REPORTS and does not judge: how many
    /// connectors each element has, how many are joined, and which
    /// elements have none joined AT ALL - that last group being the one
    /// worth a person's eye, because an element connected to nothing is on
    /// no system by definition.
    ///
    /// The register gives this row MODIFY, which its own column header
    /// defines as "the HIGHEST permission level it can require". Reading
    /// requires none of it, and connecting two elements from a machine
    /// that has never opened Revit is not something this file will be the
    /// first to try. Nothing here opens a transaction.
    ///
    /// EVERY NUMBER IS COUNTED, NOT REASONED ABOUT
    /// ---------------------------------------------
    /// The same discipline as RevitPhases: the compiler checks names and
    /// cannot check a claim about behaviour. So this does not assert what
    /// Revit considers a valid system, or that an unconnected element is
    /// always wrong. It counts what each element reports about itself and
    /// hands that back.
    /// </summary>
    internal static class RevitSystems
    {
        /// <summary>
        /// Every duct and pipe system in the active document, what is on
        /// each, and the MEP elements connected to nothing.
        /// </summary>
        public static string List(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            var rows = new List<string>();
            var onASystem = new Dictionary<string, bool>(StringComparer.Ordinal);

            rows.AddRange(Systems(doc, typeof(MechanicalSystem), "duct", onASystem));
            rows.AddRange(Systems(doc, typeof(PipingSystem), "pipe", onASystem));

            // THE SECOND HALF, AND THE REASON THE ROW EXISTS. Every MEP
            // element is asked what it reports about its own connectors.
            var loose = new List<string>();
            var openEnded = 0;
            var examined = 0;
            var noConnectors = 0;
            var offSystem = 0;

            foreach (var element in new FilteredElementCollector(doc)
                                        .WhereElementIsNotElementType())
            {
                if (element == null) continue;

                var manager = ConnectorsOf(element);
                if (manager == null) continue;   // not an MEP element at all

                examined++;

                if (!onASystem.ContainsKey(element.UniqueId)) offSystem++;

                var total = 0;
                var joined = 0;
                foreach (Connector connector in Safe(manager))
                {
                    if (connector == null) continue;
                    total++;
                    if (IsJoined(connector)) joined++;
                }

                if (total == 0) { noConnectors++; continue; }
                if (joined < total) openEnded++;
                if (joined > 0) continue;

                // CONNECTED TO NOTHING AT ALL. Not "has an open end" - the
                // end of a run is supposed to be open. This element is
                // attached to nothing in any direction, which is why it is
                // on no system.
                loose.Add(Json.Obj(
                    Json.Str("id", element.UniqueId),
                    Json.Str("name", SafeName(element)),
                    Json.Str("category", CategoryOf(element)),
                    Json.Num("connectors", total),
                    Json.Str("level", LevelOf(doc, element))));
            }

            return Json.Ok(
                Json.Arr("systems", rows),
                Json.Arr("connectedToNothing", loose),
                Json.Num("systemCount", rows.Count),
                Json.Num("mepElementsExamined", examined),
                Json.Num("withAnOpenConnector", openEnded),
                Json.Num("connectedToNothingCount", loose.Count),
                Json.Num("reportedNoConnectors", noConnectors),
                // ON NO SYSTEM IS A WIDER GROUP THAN CONNECTED TO NOTHING,
                // and the gap between the two is the useful part. An element
                // can be joined to its neighbour and still sit on no system -
                // a run whose equipment was never set, or one joined into a
                // branch that itself connects to nothing. Reported apart so
                // the two questions stay separate.
                Json.Num("onNoSystem", offSystem),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads",
                    "AN OPEN CONNECTOR IS NOT A FAULT. The end of every run "
                    + "is one, and a stub waiting for coordination is one on "
                    + "purpose - which is why `withAnOpenConnector` is a "
                    + "count and not a list. `connectedToNothing` is the "
                    + "group worth an eye: those elements report no joined "
                    + "connector in any direction, so they are on no system, "
                    + "carry no flow, and are missing from every total "
                    + "downstream without anything saying so. Nothing here "
                    + "asserts what Revit considers a valid system - this "
                    + "code has never run against Revit and a compiler "
                    + "checks names, not behaviour. `onNoSystem` is the "
                    + "wider group: an element can be joined to its "
                    + "neighbour and still be on no system, and the gap "
                    + "between that count and `connectedToNothingCount` is "
                    + "where a half-built run shows up."));
        }

        /// <summary>
        /// One row per system of the given class, with what is on it.
        ///
        /// MechanicalSystem and PipingSystem are asked separately rather
        /// than through MEPSystem, because the answer a modeller wants
        /// says "duct" or "pipe" and deriving that from the base class
        /// afterwards is a guess this can avoid making.
        /// </summary>
        private static List<string> Systems(Document doc, Type which,
                                            string kind,
                                            Dictionary<string, bool> onASystem)
        {
            var rows = new List<string>();
            foreach (var element in new FilteredElementCollector(doc)
                                        .OfClass(which))
            {
                var system = element as MEPSystem;
                if (system == null) continue;

                var members = 0;
                foreach (Element on in Members(system))
                {
                    if (on == null) continue;
                    members++;
                    onASystem[on.UniqueId] = true;
                }

                rows.Add(Json.Obj(
                    Json.Str("id", system.UniqueId),
                    Json.Str("name", SafeName(system)),
                    Json.Str("kind", kind),
                    Json.Str("systemType", TypeNameOf(doc, system)),
                    Json.Num("elements", members)));
            }
            return rows;
        }

        /// <summary>What is on a system, never an exception mid-answer.</summary>
        private static IEnumerable<Element> Members(MEPSystem system)
        {
            ElementSet set = null;
            try { set = system.Elements; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { set = null; }
            if (set == null) yield break;
            foreach (Element one in set) yield return one;
        }

        /// <summary>
        /// The connector manager an element reports, or null when it has
        /// none - which is how "not an MEP element" is established here.
        ///
        /// A duct or a pipe answers through MEPCurve; a piece of equipment
        /// or a fitting answers through its MEPModel. Anything else - a
        /// wall, a view, a level - answers neither and is skipped, rather
        /// than being counted as an MEP element with nothing connected.
        /// </summary>
        private static ConnectorManager ConnectorsOf(Element element)
        {
            try
            {
                var curve = element as MEPCurve;
                if (curve != null) return curve.ConnectorManager;

                var instance = element as FamilyInstance;
                if (instance == null) return null;
                var model = instance.MEPModel;
                return model == null ? null : model.ConnectorManager;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        private static IEnumerable<Connector> Safe(ConnectorManager manager)
        {
            ConnectorSet set = null;
            try { set = manager.Connectors; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { set = null; }
            if (set == null) yield break;
            foreach (Connector one in set) yield return one;
        }

        /// <summary>
        /// Is this connector joined to another?
        ///
        /// ASKED OF THE REFERENCES, not of a status flag. A connector can
        /// report itself connected and hand back nothing to be connected
        /// TO - the same discipline RevitLinks applies to a link that says
        /// it is loaded and produces no document. What the geometry says
        /// beats what the record says.
        /// </summary>
        private static bool IsJoined(Connector connector)
        {
            try
            {
                var refs = connector.AllRefs;
                if (refs == null) return false;
                foreach (Connector other in refs)
                {
                    if (other == null) continue;
                    if (other.Owner == null) continue;
                    if (connector.Owner != null
                        && other.Owner.Id == connector.Owner.Id) continue;
                    return true;
                }
                return false;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return false;
            }
        }

        /// <summary>The system type's name, or null.</summary>
        private static string TypeNameOf(Document doc, MEPSystem system)
        {
            try
            {
                var typeId = system.GetTypeId();
                if (typeId == null || typeId == ElementId.InvalidElementId) return null;
                var type = doc.GetElement(typeId);
                return type == null ? null : SafeName(type);
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        /// <summary>
        /// The level an element sits on, or null.
        ///
        /// A modeller chasing an unconnected duct needs to know WHERE, and
        /// a name on its own sends them looking through the whole tower.
        /// </summary>
        private static string LevelOf(Document doc, Element element)
        {
            try
            {
                var id = element.LevelId;
                if (id == null || id == ElementId.InvalidElementId) return null;
                var level = doc.GetElement(id);
                return level == null ? null : SafeName(level);
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        private static string CategoryOf(Element element)
        {
            try
            {
                var category = element.Category;
                return category == null ? null : category.Name;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return null;
            }
        }

        /// <summary>A name, never an exception mid-answer.</summary>
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
    }
}
