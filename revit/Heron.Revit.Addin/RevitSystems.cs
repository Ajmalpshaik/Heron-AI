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
    /// ======================== WHAT HAS BEEN RUN ========================
    /// Written on a machine with no Revit and compiled on 2020 through
    /// 2027. Tracked against two models on 2026-09-19 and signed
    /// (brain/agent-proofs/HERON-REVIT-SYS-030.yaml) - which showed that
    /// the answer follows the model, and could not show that it was right.
    ///
    /// It was not, twice over. Every duct, pipe and fitting was reported on
    /// NO SYSTEM, on every model, because what a system holds was read from
    /// MEPSystem.Elements alone - see Systems() below. The 2026-09-19 record
    /// already carried it: `onNoSystem` equalled `mepElementsExamined` in
    /// BOTH models. And every open end on a system read as joined, because
    /// the system's own logical reference was counted as a partner - see
    /// IsJoined(). Both are in section 5 of docs/FRAGMENT-ISSUES.md.
    ///
    /// What this file has been run against since is recorded in
    /// docs/NEEDS-CHECKING.md Group N and in the proof file - NOT HERE. The
    /// proof's fingerprint is a hash of this whole file, comments included,
    /// so a comment recording a run would make the proof of that run stale.
    /// ===================================================================
    ///
    /// WHY A MODELLER CARES, IN ONE SENTENCE
    /// --------------------------------------
    /// A duct run that LOOKS joined on screen and is not connected carries
    /// no flow, is not on the system it looks part of, and every number
    /// downstream of it is quietly short: the schedule prints, the sizing
    /// calculates, the system browser shows a tidy tree, and none of them
    /// mentions the element that is sitting there unattached.
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
    /// worth a person's eye, because nothing is attached to it.
    ///
    /// CONNECTED TO NOTHING IS NOT THE SAME QUESTION AS ON NO SYSTEM
    /// ---------------------------------------------------------------
    /// This file said they were until 2026-09-22 - "an element connected
    /// to nothing is on no system by definition". Nothing here counted
    /// that; it was reasoned, and the count it leaned on was the defect
    /// above. So the two are now answered apart, each from what it is
    /// about, and neither is inferred from the other: `connectedToNothing`
    /// from each element's connectors, `onNoSystem` from what each system
    /// holds.
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
    /// what each system reports it holds, and hands that back.
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
                // attached to nothing in any direction. Whether it is ON a
                // system is `onNoSystem`'s question, not this one's.
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
                // ON NO SYSTEM: every MEP element no duct or pipe system
                // holds - in its run, among its terminals, or as its base
                // equipment. Electrical circuits are not read here, so an
                // electrical element examined above is counted in this
                // whatever circuit it is on, and the answer says so.
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
                    + "connector in any direction, so nothing is attached "
                    + "to them at all. A join is a PHYSICAL one: the "
                    + "reference a connector holds to its own duct or pipe "
                    + "system is not counted as one, or every open end on a "
                    + "system reads as joined. Whether an element is ON a system is "
                    + "a separate question and `onNoSystem` answers it, "
                    + "counted from what each duct and pipe system holds - "
                    + "its run, its terminals and its base equipment - and "
                    + "never inferred from connectivity. Electrical circuits "
                    + "are not read, so anything electrical counts as on no "
                    + "system here. Each system's `elements` is its "
                    + "`network` (ducts or pipes and their fittings) plus "
                    + "its `components` (terminals and base equipment), "
                    + "each element counted once. Nothing here asserts what "
                    + "Revit considers a valid system."));
        }

        /// <summary>
        /// One row per system of the given class, with what is on it.
        ///
        /// MechanicalSystem and PipingSystem are asked separately rather
        /// than through MEPSystem, because the answer a modeller wants
        /// says "duct" or "pipe" and deriving that from the base class
        /// afterwards is a guess this can avoid making.
        ///
        /// WHAT IS ON A SYSTEM IS THREE SETS, BECAUSE REVIT KEEPS THREE.
        /// Autodesk's API notes say so, word for word the same in every
        /// release from 2020 to 2027:
        ///
        ///   MEPSystem.Elements            "Terminal elements in the system
        ///                                  ... doesn't include the base
        ///                                  equipment or panel"
        ///   MechanicalSystem.DuctNetwork  "The ducts and fittings contained
        ///                                  within the system"
        ///   PipingSystem.PipingNetwork    "Pipes and fittings which are
        ///                                  contained in this system"
        ///   MEPSystem.BaseEquipment       "The base panel or equipment of
        ///                                  the system"
        ///
        /// This read the first of them alone until 2026-09-22. Measured on
        /// Project1, Revit 2024: "Mechanical Supply Air 1 - 0 element(s)"
        /// and "3 MEP element(s) are on NO SYSTEM", while both ducts' own
        /// System Name parameter read Mechanical Supply Air 1.
        ///
        /// THE RUN AND THE COMPONENTS ARE REPORTED APART as well as
        /// together, so each can be checked against Revit on its own - the
        /// components are what N2 in docs/NEEDS-CHECKING.md holds up
        /// against the System Browser. An element reached both ways is
        /// counted once, under the run, so the two always add up to
        /// `elements`.
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

                // BY UNIQUE ID, so an element reached by two routes is one
                // element. The count is of things, not of routes to them.
                var counted = new HashSet<string>(StringComparer.Ordinal);
                var network = Count(Network(system), counted, onASystem);
                var components = Count(Components(system), counted, onASystem);

                rows.Add(Json.Obj(
                    Json.Str("id", system.UniqueId),
                    Json.Str("name", SafeName(system)),
                    Json.Str("kind", kind),
                    Json.Str("systemType", TypeNameOf(doc, system)),
                    Json.Num("elements", counted.Count),
                    Json.Num("network", network),
                    Json.Num("components", components)));
            }
            return rows;
        }

        /// <summary>
        /// How many of these were not counted already - and every one of
        /// them is marked as on a system, whichever route reached it.
        /// </summary>
        private static int Count(IEnumerable<Element> these,
                                 HashSet<string> counted,
                                 Dictionary<string, bool> onASystem)
        {
            var added = 0;
            foreach (var on in these)
            {
                if (on == null) continue;
                onASystem[on.UniqueId] = true;
                if (counted.Add(on.UniqueId)) added++;
            }
            return added;
        }

        /// <summary>
        /// A system's RUN - its ducts or pipes and their fittings. Never an
        /// exception mid-answer.
        /// </summary>
        private static IEnumerable<Element> Network(MEPSystem system)
        {
            ElementSet set = null;
            try
            {
                var duct = system as MechanicalSystem;
                var pipe = system as PipingSystem;
                if (duct != null) set = duct.DuctNetwork;
                else if (pipe != null) set = pipe.PipingNetwork;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { set = null; }
            return Each(set);
        }

        /// <summary>
        /// A system's COMPONENTS - its terminals, and its base equipment,
        /// which Elements leaves out by Autodesk's own account. Never an
        /// exception mid-answer.
        /// </summary>
        private static IEnumerable<Element> Components(MEPSystem system)
        {
            ElementSet set = null;
            try { set = system.Elements; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { set = null; }
            foreach (var one in Each(set)) yield return one;

            FamilyInstance equipment = null;
            try { equipment = system.BaseEquipment; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { equipment = null; }
            if (equipment != null) yield return equipment;
        }

        private static IEnumerable<Element> Each(ElementSet set)
        {
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
        /// Is this connector PHYSICALLY joined to another element?
        ///
        /// ASKED OF THE REFERENCES, not of a status flag. A connector can
        /// report itself connected and hand back nothing to be connected
        /// TO - the same discipline RevitLinks applies to a link that says
        /// it is loaded and produces no document. What the geometry says
        /// beats what the record says.
        ///
        /// A DUCT OR PIPE SYSTEM IS NOT A PARTNER. AllRefs holds "both
        /// physical connection and logical connection", in Autodesk's own
        /// words, and a connector on a duct or pipe system holds a logical
        /// reference to that system. Counted as a join until 2026-09-22, it
        /// made every open end on a system look joined: on Project1, Revit
        /// 2024, `withAnOpenConnector` read 0 while the find-dead-ends
        /// fragment walked the same two ducts and found both open ends. The
        /// report-connectors fragment learned this rule first, and its
        /// purpose says so.
        ///
        /// AN ELECTRICAL CIRCUIT'S REFERENCE IS STILL COUNTED, on purpose.
        /// For an electrical device a circuit is usually the only connection
        /// there is, and dropping it would list every light fitting on a
        /// circuit as connected to nothing. That is a decision about
        /// electrical, which this agent does not read, and it is not made
        /// here.
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
                    var owner = other.Owner;
                    if (owner == null) continue;
                    if (owner is MechanicalSystem || owner is PipingSystem) continue;
                    if (connector.Owner != null
                        && owner.Id == connector.Owner.Id) continue;
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
