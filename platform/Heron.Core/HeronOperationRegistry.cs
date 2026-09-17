// Heron-Agent:  HERON-KRN-TOL-009
// Heron-Step:   3
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;
using System.Collections.Generic;

namespace Heron.Core
{
    /// <summary>
    /// Tool Registry. Every operation Heron will run, and the risk level each
    /// one carries.
    ///
    /// WHY THIS FILE EXISTS AT ALL. docs/12 section 71 is one sentence:
    ///
    ///     Risk level is declared in the tool registry, not decided per call.
    ///
    /// Before this, it was decided per call - two literals inside the write
    /// path, one for the preview and one for the move. That was SAFE (a literal
    /// in code cannot be raised by anything Heron reads, so Golden Rule 19
    /// held) but it was not AUDITABLE: the question "which of Heron's
    /// operations can change my model?" could only be answered by reading every
    /// operation. Now it is answered by reading one table.
    ///
    /// The difference matters most for the operation nobody has written yet.
    /// Under the old arrangement, a new write that simply forgot to check
    /// permission would write unchecked, and nothing would notice. Under this
    /// one, the gate runs before routing and an operation that is not declared
    /// here does not run at all.
    ///
    /// GOLDEN RULE 19 - the risk of an operation is looked up BY NAME in this
    /// table. It is never read from the request, never inferred from what an
    /// element is called, and never supplied by the caller. A request can ask
    /// for an operation; it cannot say how dangerous that operation is. Those
    /// are different questions and only one of them is the caller's to answer.
    ///
    /// FAILS CLOSED. An operation absent from this table is refused - not
    /// treated as READ. Every entry here is a deliberate act of declaring
    /// something safe enough to run, and silence is the absence of that act,
    /// not a synonym for zero risk.
    /// </summary>
    public static class HeronOperationRegistry
    {
        /// <summary>
        /// The declaration. Adding an operation to Heron means adding a row
        /// here, and the row is where a reviewer looks first.
        ///
        /// The levels come from docs/12 section 1, and the boundary that
        /// matters sits between Execute and Modify: everything at Modify or
        /// above changes something the user cares about.
        /// </summary>
        private static readonly Dictionary<string, HeronRisk> Declared =
            new Dictionary<string, HeronRisk>(StringComparer.Ordinal)
            {
                // Reads. Nothing in the model changes, nothing on screen changes.
                { "ping",               HeronRisk.Read },
                { "info",               HeronRisk.Read },
                { "count_elements",     HeronRisk.Read },

                // HERON-REVIT-LNK-015. Read: it lists the links the model
                // already has and reads only the linked documents Revit has
                // already loaded. Nothing is loaded, unloaded or reloaded -
                // the register's row is "never modifies a link's source", and
                // reloading somebody's link to answer a question would be
                // exactly that.
                { "list_links",         HeronRisk.Read },
                { "list_phases",        HeronRisk.Read },
                { "list_systems",       HeronRisk.Read },

                // HERON-REVIT-PAR-011. Read, although the register's row for
                // that agent is MODIFY - the column is "the HIGHEST level it
                // can require", and this operation reads. Writing a parameter
                // will be a SEPARATE entry at its own risk when it exists,
                // rather than this one quietly widening: the risk is looked
                // up by name, so one name must mean one thing.
                { "read_parameters",    HeronRisk.Read },

                // HERON-REVIT-GRP-033. Read, and the same note applies as
                // above: the register's row is MODIFY because that column
                // is the highest level the ROW can require. This one lists
                // what is grouped. Ungrouping would be a separate entry.
                { "list_groups",        HeronRisk.Read },

                // HERON-REVIT-LVL-027. Read, and the same note as the two
                // above: the register's row is MODIFY because that column is
                // the highest level the ROW can require. This one lists what
                // the levels and grids are and what sits on them. Renaming a
                // level, or moving its elevation - which drags every hosted
                // element with it - would be a separate entry.
                { "list_levels",        HeronRisk.Read },

                // HERON-REVIT-WRK-014. Read. It lists the worksets and reads
                // a bounded sample of ownership; it creates no workset, opens
                // or closes none, borrows nothing and relinquishes nothing.
                // Relinquishing somebody's borrowed element loses their
                // unsynchronised work, so that will be its own entry at its
                // own risk if it is ever written.
                { "list_worksets",      HeronRisk.Read },

                // HERON-REVIT-VIE-013 and HERON-REVIT-SHT-029. Read, same
                // reasoning as the rest of this block. Applying a view
                // template changes what everybody sees in that view, and
                // renumbering a sheet breaks every drawing reference pointing
                // at it across the whole set - so both writes, if they are
                // ever built, are separate entries at their own risk.
                { "list_views",         HeronRisk.Read },
                { "list_sheets",        HeronRisk.Read },

                // HERON-REVIT-RM-028. Read. Placing a room, deleting an
                // unplaced one or moving a boundary all change somebody's area
                // schedule, which on most jobs is a contractual document.
                { "list_rooms",         HeronRisk.Read },

                // HERON-REVIT-SCH-026. Read. It says which schedules exist and
                // what governs them; it does not read their ROWS, which is the
                // export agent's job at PUBLISH. Adding a field or changing a
                // filter changes what everybody downstream is pricing from.
                { "list_schedules",     HeronRisk.Read },

                // HERON-REVIT-FAM-012. Read, and deliberately NOT a load.
                // Loading a family merges one document into another: its
                // materials overwrite the project's, silently, with no count
                // moving. If a load is ever built it is a separate entry at a
                // much higher risk than this one.
                { "list_families",      HeronRisk.Read },

                // HERON-REVIT-EXP-018. READ, although that agent's row is
                // PUBLISH - the column is the highest level the ROW can
                // require, and asking whether an export WOULD be sound
                // requires none of it. Nothing is written, printed or sent.
                // The export itself is a separate entry at PUBLISH for
                // whoever builds the destination-and-overwrite rails.
                { "check_export",       HeronRisk.Read },

                // HERON-REVIT-IMP-019. Read. An import is irreversible in the
                // way that matters - its layers and text styles stay after the
                // import is deleted - so importing is not an operation here.
                { "list_imports",       HeronRisk.Read },

                // HERON-REVIT-DIM-031. Read. Creating a dimension, clearing an
                // override or deleting a tag all change a drawing somebody may
                // already have checked.
                { "list_annotation",    HeronRisk.Read },

                // GIVING THE SESSION BACK. Read, because it cannot touch a
                // model - it hands back a claim, and only the chat that holds
                // it may. It is the OPPOSITE of a takeover: a second chat can
                // never use this to free somebody else's Revit.
                { "release",            HeronRisk.Read },

                // ANALYZE - reads the model and computes over it. preview_move
                // is here rather than at Modify because it genuinely changes
                // nothing: it is the description of a change, not the change.
                // The operation it describes is gated separately, where the
                // refusal can say so in the user's own terms.
                { "preview_move",       HeronRisk.Analyze },

                // EXECUTE - a non-destructive model action. Selecting alters
                // what is highlighted, not what exists, so Ctrl+Z has nothing
                // to undo afterwards. It is still the first thing the user can
                // SEE, which is why it is not merely a Read.
                { "select_by_category", HeronRisk.Execute },

                // ANALYZE - runs a fragment's own C# against the model and
                // reports what it left behind. It is here rather than at
                // Modify for a structural reason, not a hopeful one: the
                // executor opens NO TRANSACTION, and Revit refuses every model
                // change made outside one. The guarantee is Revit's, not a
                // check of ours that could be forgotten.
                //
                // Running a fragment that WRITES is the row below. This one
                // stays at Analyze on the same structural grounds: no
                // transaction, so Revit itself refuses any change.
                { "run_fragment_read",  HeronRisk.Analyze },

                // MODIFY - the same executor, inside a TransactionGroup, so a
                // fragment at risk: MODIFY can finally run. 130 of them had no
                // engine at all: written, compiling on eight releases, and
                // unable to execute a single line.
                //
                // THE PREVIEW IS THE RUN ITSELF, ROLLED BACK. move_elements
                // predicts its change before making it, which works because
                // moving a duct 200mm is describable in advance. A fragment is
                // arbitrary C# and is not. So this executes for real inside a
                // TransactionGroup, reports exactly what happened, and then
                // rolls back unless the caller said apply - which is a STRONGER
                // guarantee than a prediction, because nothing is being guessed
                // at. Rolling back is the default; committing is the deliberate
                // act, and the caller has to say so in as many words.
                { "run_fragment_write", HeronRisk.Modify },

                // MODIFY. It needs a preview the user accepted, and writing
                // must be switched on (HeronPermissions, D-19).
                { "move_elements",      HeronRisk.Modify },
            };

        /// <summary>Every declared operation, for listing and for tests.</summary>
        public static IEnumerable<string> Operations
        {
            get
            {
                var names = new List<string>(Declared.Keys);
                names.Sort(StringComparer.Ordinal);
                return names;
            }
        }

        /// <summary>Is this a declared operation at all?</summary>
        public static bool IsDeclared(string op)
        {
            return !string.IsNullOrEmpty(op) && Declared.ContainsKey(op);
        }

        /// <summary>
        /// The declared risk of an operation. Throws for an undeclared one
        /// rather than returning a default: there is no sensible default, and
        /// a caller that has not checked IsDeclared first has a bug that
        /// should be loud.
        /// </summary>
        public static HeronRisk RiskOf(string op)
        {
            HeronRisk risk;
            if (op != null && Declared.TryGetValue(op, out risk)) return risk;

            throw new ArgumentException(
                "Operation '" + (op ?? "(none)") + "' is not declared in the tool registry.", "op");
        }

        /// <summary>Does this operation change the model?</summary>
        public static bool Writes(string op)
        {
            return IsDeclared(op) && RiskOf(op) >= HeronRisk.Modify;
        }

        /// <summary>
        /// The declared operations as one readable line, for a refusal that
        /// has to say what IS available.
        ///
        /// Derived from the table rather than typed out. The message it
        /// replaced carried a hand-written list, and a hand-written list of
        /// something that already exists in a table is a list that will
        /// eventually disagree with it.
        /// </summary>
        public static string Describe()
        {
            var parts = new List<string>();
            foreach (var op in Operations) parts.Add(op);
            return string.Join(", ", parts.ToArray());
        }
    }
}
