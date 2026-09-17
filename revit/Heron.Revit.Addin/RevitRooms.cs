// Heron-Agent:  HERON-REVIT-RM-028
// Heron-Step:   4
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

namespace Heron.Revit.Addin
{
    /// <summary>
    /// Rooms, spaces and areas. HERON-REVIT-RM-028, whose row gives the
    /// reason an MEP job cares: "MEP loads and schedules depend on spaces
    /// existing and being bounded".
    ///
    /// ============================ NEVER RUN ============================
    /// Written on a machine with no Revit, and on a day another session had
    /// the only Revit open. It COMPILES on 2020 through 2027 and that is the
    /// whole of what is known about it.
    /// ===================================================================
    ///
    /// A ROOM AND A SPACE ARE NOT THE SAME THING, AND THAT IS THE POINT
    /// -----------------------------------------------------------------
    /// The architect places ROOMS. The MEP engineer places SPACES, usually
    /// in a linked model, over the top of those rooms. They are separate
    /// elements in separate categories and they go out of step constantly -
    /// an architect adds a partition, the rooms update, and the spaces do
    /// not until somebody presses the button.
    ///
    /// So both are reported, side by side, and the difference between the
    /// two counts is stated rather than left for the reader to spot.
    ///
    /// UNPLACED AND UNBOUNDED ARE DIFFERENT FAILURES
    /// -----------------------------------------------
    /// A room can be wrong in two ways that look identical in a schedule and
    /// are fixed completely differently:
    ///
    ///   UNPLACED - the room exists in the schedule and sits nowhere in the
    ///   model. Somebody deleted the enclosing walls, or the room was never
    ///   dragged out of the browser. Its area is zero and it still appears
    ///   in every room schedule and every area total.
    ///
    ///   NOT ENCLOSED - the room is placed, but the boundary leaks. Revit
    ///   reports it as "not enclosed" and gives it zero area. A gap of one
    ///   millimetre between two walls does it.
    ///
    /// Both are counted separately, because "23 rooms have a problem" is not
    /// something anybody can act on and "14 unplaced, 6 not enclosed" is two
    /// different afternoons.
    ///
    /// THE THIRD STATE IS NAMED AND NOT COUNTED, ON PURPOSE
    /// ------------------------------------------------------
    /// REDUNDANT rooms - two in one enclosure, both computing an area, so the
    /// total exceeds the floor plate - are the other classic fault, and this
    /// agent CANNOT SEE THEM. Revit reports redundancy through its Warnings
    /// list rather than through any member on the room, and matching a
    /// warning by its description text is locale-dependent: it would work in
    /// English and silently report zero everywhere else.
    ///
    /// The first draft of this file carried a `roomsRedundant` counter whose
    /// detector returned false unconditionally. That is a gate reporting zero
    /// for ever and being read as evidence the check works - the mistake
    /// RevitSheets.cs declines to make about sheet numbers two files away.
    /// It was removed rather than shipped. The answer says the state is not
    /// checked, so nobody reads a silence as a clean bill.
    ///
    /// AREAS ARE ASKED FOR, NEVER CALCULATED
    /// ---------------------------------------
    /// Revit holds an area in square feet whatever the project is set to, so
    /// `room.Area` returns 215.3 for a room of 20 m2. The same defect as
    /// lengths and the same fix: every area here is `AsValueString()` in the
    /// project's own units and the raw double is used only to decide whether
    /// something is zero.
    ///
    /// NOTHING IS SET
    /// ----------------
    /// The register gives this row MODIFY - the highest level it could ever
    /// require. Reading requires none of it. Placing a room, deleting an
    /// unplaced one or changing a boundary all change somebody's area
    /// schedule, which on most jobs is a contractual document.
    /// </summary>
    internal static class RevitRooms
    {
        /// <summary>
        /// Every room, space and area in the active document, with the two
        /// states it can see counted separately, and the third named as one
        /// it cannot.
        /// </summary>
        public static string List(UIApplication app)
        {
            var doc = RevitOperations.ActiveDocument(app);
            if (doc == null)
            {
                return Json.Error("no_document",
                    "No model is open in Revit. Open one and ask again.");
            }

            var rooms = Gather(doc, BuiltInCategory.OST_Rooms);
            var spaces = Gather(doc, BuiltInCategory.OST_MEPSpaces);
            var areas = Gather(doc, BuiltInCategory.OST_Areas);

            if (rooms.Total == 0 && spaces.Total == 0 && areas.Total == 0)
            {
                return Json.Ok(
                    Json.Num("rooms", 0),
                    Json.Num("spaces", 0),
                    Json.Num("areas", 0),
                    Json.Str("document", doc.Title),
                    Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                    Json.Bool("modifiedAnything", false),
                    Json.Str("reads",
                        "This model has no rooms, no MEP spaces and no areas. "
                      + "That is not a fault - plenty of models never get any - "
                      + "but it does mean any question about areas, occupancy "
                      + "or MEP loads has nothing in this model to answer from."));
            }

            return Json.Ok(
                Json.Num("rooms", rooms.Total),
                Json.Num("roomsUnplaced", rooms.Unplaced),
                Json.Num("roomsNotEnclosed", rooms.NotEnclosed),
                Json.Num("spaces", spaces.Total),
                Json.Num("spacesUnplaced", spaces.Unplaced),
                Json.Num("spacesNotEnclosed", spaces.NotEnclosed),
                Json.Num("areas", areas.Total),
                Json.Num("areasUnplaced", areas.Unplaced),
                Json.Arr("examples", rooms.Examples.Count > 0
                                     ? rooms.Examples : spaces.Examples),
                Json.Str("document", doc.Title),
                Json.Str("projectKey", RevitOperations.ProjectKey(doc)),
                Json.Bool("modifiedAnything", false),
                Json.Str("reads", Reads(rooms, spaces, areas)));
        }

        /// <summary>What the numbers mean, one actionable sentence per state.</summary>
        private static string Reads(Tally rooms, Tally spaces, Tally areas)
        {
            var said = new List<string>();

            if (rooms.Total > 0 && spaces.Total > 0 && rooms.Total != spaces.Total)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "There are {0} room(s) and {1} MEP space(s). Rooms are the "
                  + "architect's and spaces are the engineer's, and they go out "
                  + "of step whenever a partition moves and nobody presses the "
                  + "button - so the difference of {2} is the first thing to "
                  + "look at", rooms.Total, spaces.Total,
                    Math.Abs(rooms.Total - spaces.Total)));
            }

            Describe(said, "room", rooms);
            Describe(said, "MEP space", spaces);

            if (areas.Unplaced > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} area(s) are unplaced", areas.Unplaced));
            }

            if (said.Count == 0)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "{0} room(s), {1} MEP space(s) and {2} area(s), and every "
                  + "one of them is placed and enclosed. REDUNDANT rooms are "
                  + "not checked - Revit reports those in its Warnings list - "
                  + "so this is not a clean bill on that one state. Areas are "
                  + "as Revit prints them, in the project's own units.",
                    rooms.Total, spaces.Total, areas.Total);
            }

            return string.Join(". ", said.ToArray())
                 + ". Unplaced and not-enclosed are counted separately on purpose "
                 + "- they look identical in a schedule and each is a different "
                 + "afternoon's work. REDUNDANT rooms, two in one enclosure, are "
                 + "NOT CHECKED here: Revit reports those through its Warnings "
                 + "list and matching one by its text would work in English and "
                 + "silently report zero everywhere else. Areas are as Revit "
                 + "prints them, in the project's own units. Nothing was changed.";
        }

        /// <summary>One sentence per failing state, and nothing for a clean one.</summary>
        private static void Describe(List<string> said, string what, Tally tally)
        {
            if (tally.Unplaced > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} {1}(s) are UNPLACED - they exist in the schedule and sit "
                  + "nowhere in the model, with zero area, and they still appear "
                  + "in every total", tally.Unplaced, what));
            }
            if (tally.NotEnclosed > 0)
            {
                said.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0} {1}(s) are NOT ENCLOSED - placed, but the boundary leaks "
                  + "and Revit gives them zero area. A one-millimetre gap between "
                  + "two walls does it", tally.NotEnclosed, what));
            }
        }

        /// <summary>Counts for one category, and a few named examples.</summary>
        private sealed class Tally
        {
            public int Total;
            public int Unplaced;
            public int NotEnclosed;
            public readonly List<string> Examples = new List<string>();
        }

        /// <summary>
        /// Walk one category and sort every element into the states that
        /// matter.
        ///
        /// HOW UNPLACED AND NOT-ENCLOSED ARE TOLD APART, since both give zero
        /// area and the difference is the whole value of the answer: a room
        /// that was never placed has NO LOCATION at all, and one whose
        /// boundary leaks has a location and no area. `Location` is the
        /// question that separates them, and it is asked of the element
        /// rather than inferred from the number.
        ///
        /// REDUNDANCY IS NOT LOOKED FOR HERE. It lives in Revit's Warnings
        /// list, not on the room, and matching a warning by its text works in
        /// English and reports zero in every other language. The class summary
        /// says so, and the answer says so, so a silence is not read as a pass.
        /// </summary>
        private static Tally Gather(Document doc, BuiltInCategory category)
        {
            var tally = new Tally();

            IList<Element> found;
            try
            {
                found = new FilteredElementCollector(doc)
                            .OfCategory(category)
                            .WhereElementIsNotElementType()
                            .ToElements();
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException)
            {
                return tally;
            }

            foreach (var element in found)
            {
                if (element == null) continue;
                tally.Total++;

                var placed = HasLocation(element);
                var area = AreaOf(element);

                if (!placed)
                {
                    tally.Unplaced++;
                    Remember(tally, element, "unplaced");
                    continue;
                }

                if (area <= 0.0)
                {
                    // Placed, and no area. Revit's own word for this is "not
                    // enclosed", and it is the state a one-millimetre gap
                    // produces.
                    tally.NotEnclosed++;
                    Remember(tally, element, "not enclosed");
                    continue;
                }

                // Placed, enclosed, and with an area. Nothing more is
                // asked of it - redundancy lives in Revit's Warnings list and
                // is named in this class's summary as a state not checked.
            }

            return tally;
        }

        /// <summary>
        /// A few named examples, bounded.
        ///
        /// Five rather than all of them: the COUNT is the finding and the
        /// names are there so somebody can go and look at one. A list of
        /// four hundred room names is not an answer anybody reads.
        /// </summary>
        private static void Remember(Tally tally, Element element, string why)
        {
            if (tally.Examples.Count >= 5) return;
            tally.Examples.Add(Json.Obj(
                Json.Str("name", SafeName(element)),
                Json.Str("number", NumberOf(element)),
                Json.Str("state", why)));
        }

        /// <summary>
        /// Is this room placed anywhere at all?
        ///
        /// An unplaced room has no Location. That is the honest separator
        /// between "never placed" and "placed but leaking", which both report
        /// zero area and are completely different jobs to fix.
        /// </summary>
        private static bool HasLocation(Element element)
        {
            try { return element.Location != null; }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return false; }
        }

        /// <summary>
        /// The raw area, used ONLY to decide whether it is zero.
        ///
        /// Never printed. Revit holds an area in square feet whatever the
        /// project is set to, and a bare 215.3 is read as square metres by
        /// the next person. The printed value, where one is printed, comes
        /// from AsValueString.
        /// </summary>
        private static double AreaOf(Element element)
        {
            try
            {
                var p = element.get_Parameter(BuiltInParameter.ROOM_AREA);
                return p == null ? 0.0 : p.AsDouble();
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return 0.0; }
        }

        /// <summary>The room number, or null.</summary>
        private static string NumberOf(Element element)
        {
            try
            {
                var p = element.get_Parameter(BuiltInParameter.ROOM_NUMBER);
                if (p == null) return null;
                var shown = p.AsString();
                return string.IsNullOrEmpty(shown) ? null : shown;
            }
            catch (Autodesk.Revit.Exceptions.ApplicationException) { return null; }
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
