# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-NAM-REN-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Auto rename - identity first, then the name, then every reference.

    python brain/heron_rename.py

WHAT IT IS FOR (docs/28, HERON-NAM-REN-003)
--------------------------------------------
"Renames - ONLY AFTER IDENTITY EXISTS, never before." T1, risk MODIFY.
Nothing is renamed here: a plan comes back and a caller carries it out,
the same shape HERON-WSP-REP-004 and HERON-WSP-CLN-009 use.

THE RULE THE WHOLE FILE IS BUILT AROUND
-----------------------------------------
docs/06 s136, in full:

    "The Auto Rename Agent is only safe because of Knowledge Identity
    (s30): identity is an ID, never a filename. ORDER OF OPERATIONS
    MATTERS - identity must exist BEFORE anything is allowed to rename
    automatically. Renaming files that are identified by name is how
    knowledge bases lose track of themselves."

So the first thing checked is not the new name. It is whether the thing
being renamed can still be recognised afterwards. A file whose only
identity is where it sits is not renamed by this agent at any price -
NO_IDENTITY_YET - because the rename is what destroys the identity, and
it destroys it at the moment it succeeds.

A RENAME IS NOT ONE STEP, AND THAT IS WHY IT NEEDS THREE AGENTS
-----------------------------------------------------------------
    HERON-WSP-REG-012   does an identity exist?     asked here, first
    HERON-NAM-VAL-002   is the new name right?      asked here, second
    HERON-NAM-REF-007   what points at the old one? NOT asked here

The third is the one this agent cannot do and must not proceed without.
docs/28 gives REF-007 "after any rename or move: imports, references,
metadata, registry, docs. NO BROKEN REFERENCES." So a file something
else names is refused - REFERENCES_NOT_UPDATED - unless the caller says
those references are part of the same change. Renaming first and fixing
references afterwards is the same plan with a window in the middle
where the repository does not build.

A RENAME IS NOT A MOVE
------------------------
`to` is a NAME. A `to` carrying a separator is a move, and a move is a
different operation with a different owner: HERON-WSP-PLC-005 decides
where a new thing belongs and HERON-WSP-REP-004 corrects what is
misplaced. Doing it here would be this agent quietly acquiring the right
to put a file anywhere.

AND THE ONE ONLY THE BATCH CAN SEE
------------------------------------
Two files renamed to the same name are each fine on their own. Together
one of them is gone. That is checked across the whole batch, the same
reason HERON-WSP-ARC-001 exists one department along.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_naming as NAMING  # noqa: E402
import heron_registry as REGISTRY  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# A name is a name. Anything here makes it a path.
SEPARATORS = ("/", "\\", "..", ":", "\0", "\n", "\r")

# Which kind of name to check a new one against, by what is being renamed.
KINDS = {"module": "module", "suite": "suite",
         "fragment-folder": "fragment-folder"}


def _plain(name):
    """Which separator a name carries, or None."""
    text = str(name or "")
    for mark in SEPARATORS:
        if mark in text:
            return mark
    return None


def _identified(entry, text):
    """
    (True, why) if this thing can be recognised after the rename.

    Two ways, and both are the thing's own statement about itself rather
    than anything worked out from where it sits: the five-field header
    HERON-WSP-REG-012 reads, or a declared `identity` handed in for
    something that carries one elsewhere - a fragment's `id` lives in
    its fragment.yaml, not in a comment.
    """
    declared = str(entry.get("identity") or "").strip()
    if declared:
        return True, "it declares the identity '%s', which the rename does " \
                     "not touch." % declared
    if text is None:
        return False, "nothing was read for it and it declares no identity, " \
                      "so there is no evidence it can be recognised " \
                      "afterwards. Absence of evidence is refused here " \
                      "rather than read as absence of the problem."
    found = REGISTRY.identity(text)
    agent = found["claims"].get("Heron-Agent", "").strip()
    if agent and agent.lower() != "none":
        return True, "its header claims %s, and a header travels with the " \
                     "file." % agent
    return False, "it claims no Heron-Agent and declares no identity. Its " \
                  "name is the only thing that says what it is, and a " \
                  "rename destroys that at the moment it succeeds - " \
                  "docs/06 s136."


def plan(renames, read=None, referenced_by=None, also_updating=None):
    """
    {rename, refused_names, why, unjudged} - or a refusal.

    Nothing is renamed. `read` returns a file's text and is handed in;
    `referenced_by` says what names each file, and is handed in too.
    """
    if not isinstance(renames, (list, tuple)) or not renames:
        return {"renamed": False, "refused": "NOTHING_TO_RENAME",
                "why": "no rename was given. An empty batch reports success "
                       "and changes nothing, which reads exactly like a "
                       "working one."}
    if read is not None and not callable(read):
        return {"renamed": False, "refused": "NOT_A_READER",
                "why": "a %s was passed where a reader belongs. This agent "
                       "does not open files - looking and then acting on "
                       "what was seen is the shape of the mistake."
                       % type(read).__name__}

    points_at = referenced_by if callable(referenced_by) else (
        lambda path: list((referenced_by or {}).get(path, [])))
    fixing = set(str(each).strip() for each in (also_updating or []))

    ready, refused, wanted = [], [], {}
    for entry in renames:
        if not isinstance(entry, dict):
            refused.append({"entry": repr(entry)[:50],
                            "refused": "NOT_A_RENAME",
                            "why": "each rename is {from, to, kind}, and a "
                                   "batch that half-applies is worse than "
                                   "one that refuses."})
            continue

        was = str(entry.get("from") or "").strip()
        now = str(entry.get("to") or "").strip()
        kind = str(entry.get("kind") or "").strip().lower()

        if not was or not now:
            refused.append({"from": was or None, "to": now or None,
                            "refused": "NOT_A_RENAME",
                            "why": "a rename needs both a thing and a new "
                                   "name for it."})
            continue
        if was == now or os.path.basename(was) == now:
            refused.append({"from": was, "to": now,
                            "refused": "NOT_A_RENAME",
                            "why": "'%s' is already called that. A rename "
                                   "that changes nothing still costs every "
                                   "reference a look." % was})
            continue

        mark = _plain(now)
        if mark is not None:
            refused.append({"from": was, "to": now,
                            "refused": "A_RENAME_IS_NOT_A_MOVE",
                            "why": "'%s' carries '%s', so it names a place "
                                   "rather than a thing. A move belongs to "
                                   "HERON-WSP-PLC-005 and HERON-WSP-REP-004; "
                                   "doing it here would be this agent "
                                   "quietly acquiring the right to put a "
                                   "file anywhere." % (now, mark)})
            continue

        # FIRST, AND BEFORE THE NAME IS EVEN LOOKED AT - docs/06 s136.
        text = read(was) if read is not None else None
        known, why_known = _identified(entry, text)
        if not known:
            refused.append({"from": was, "to": now,
                            "refused": "NO_IDENTITY_YET",
                            "why": "%s Identity must exist BEFORE anything "
                                   "is renamed automatically, and renaming "
                                   "files that are identified by name is "
                                   "how knowledge bases lose track of "
                                   "themselves." % why_known})
            continue

        if kind not in KINDS:
            refused.append({"from": was, "to": now,
                            "refused": "NOT_A_RENAME",
                            "why": "'%s' is not a kind of name this system "
                                   "renames. Known: %s. HERON-NAM-VAL-002 "
                                   "owns the conventions and this agent "
                                   "does not add one."
                                   % (kind, ", ".join(sorted(KINDS)))})
            continue

        judged = NAMING.check(KINDS[kind], now,
                              of=entry.get("of"))
        if not judged.get("ok"):
            refused.append({"from": was, "to": now,
                            "refused": "THE_NEW_NAME_IS_WRONG",
                            "by": judged.get("refused"),
                            "why": "HERON-NAM-VAL-002 refuses '%s': %s"
                                   % (now, judged["why"])})
            continue

        names_it = [each for each in (points_at(was) or [])
                    if str(each).strip() not in fixing]
        if names_it:
            refused.append({"from": was, "to": now,
                            "refused": "REFERENCES_NOT_UPDATED",
                            "referenced_by": sorted(names_it),
                            "why": "%d thing(s) name '%s' and are not part "
                                   "of this change: %s. docs/28 gives "
                                   "HERON-NAM-REF-007 imports, references, "
                                   "metadata, registry and docs after any "
                                   "rename, with NO BROKEN REFERENCES - so "
                                   "renaming first and fixing them "
                                   "afterwards is the same plan with a "
                                   "window in the middle where nothing "
                                   "builds."
                                   % (len(names_it), was,
                                      ", ".join(sorted(names_it)[:3]))})
            continue

        wanted.setdefault(now, []).append(was)
        ready.append({"from": was, "to": now, "kind": kind,
                      "identity": why_known,
                      "checked": judged.get("checked")})

    # WHAT ONLY THE BATCH CAN SEE. Each is fine alone; together one is gone.
    clash = sorted(name for name, sources in wanted.items()
                   if len(sources) > 1)
    if clash:
        return {"renamed": False, "refused": "TWO_RENAMES_COLLIDE",
                "colliding": dict((name, sorted(wanted[name]))
                                  for name in clash),
                "why": "%s. Each rename is fine on its own and together one "
                       "of them is gone, which is the kind of thing only "
                       "the whole batch can see."
                       % "; ".join("%d things would all be called '%s'"
                                   % (len(wanted[name]), name)
                                   for name in clash)}

    landed = len(ready) + len(refused)
    return {
        "renamed": False, "rename": ready, "refused_names": refused,
        "of": len(renames),
        "why": "%d rename(s): %d ready, %d refused. Nothing was renamed."
               % (len(renames), len(ready), len(refused)),
        "unjudged": [
            "NOTHING WAS RENAMED AND NOTHING WAS OPENED. The plan comes "
            "back and a caller carries it out; `read` and `referenced_by` "
            "were HANDED IN, because looking and then acting on what was "
            "seen is the shape of the mistake.",
            "EVERY ENTRY LANDS IN EXACTLY ONE LIST (%d of %d)." % (landed,
                                                                   len(renames)),
            "IDENTITY WAS CHECKED FIRST, BEFORE THE NAME. That order is "
            "docs/06 s136's and not a preference: the rename is what "
            "destroys a name-only identity, and it destroys it at the "
            "moment it succeeds.",
            "WHAT POINTS AT THE OLD NAME IS THE CALLER'S TO FIND. This "
            "agent refuses a rename whose references are not part of the "
            "same change; it cannot tell whether the list it was handed is "
            "complete, and HERON-NAM-REF-007 is the agent that will.",
        ],
    }


def main(argv):
    print("AUTO RENAME   identity first, then the name, then references")
    print("=" * 72)

    headers = {
        "brain/heron_paths.py": "# Heron-Agent:  HERON-WSP-PTH-007\n#\n",
        "brain/helper.py": "# a helper nobody ever identified\n",
    }

    answer = plan(
        [{"from": "brain/heron_paths.py", "to": "heron_pathing.py",
          "kind": "module"},
         {"from": "brain/helper.py", "to": "heron_helper.py",
          "kind": "module"},
         {"from": "brain/heron_flags.py", "to": "brain/heron_flag.py",
          "kind": "module", "identity": "HERON-OPS-FLG-009"},
         {"from": "brain/heron_shadow.py", "to": "shadow.py",
          "kind": "module", "identity": "HERON-OPS-SHD-011"},
         {"from": "brain/heron_update.py", "to": "heron_updates.py",
          "kind": "module", "identity": "HERON-OPS-UPD-010"}],
        read=lambda path: headers.get(path),
        referenced_by={"brain/heron_update.py": ["brain/heron_migration.py"]})

    print("\n%s" % answer["why"])
    for row in answer["rename"]:
        print("  rename   %-24s -> %s" % (row["from"], row["to"]))
    for row in answer["refused_names"]:
        print("  REFUSED  %-24s %s" % (row.get("from", "?"), row["refused"]))

    print("\nand the one only the batch can see")
    both = plan([{"from": "brain/a.py", "to": "heron_x.py", "kind": "module",
                  "identity": "HERON-A"},
                 {"from": "brain/b.py", "to": "heron_x.py", "kind": "module",
                  "identity": "HERON-B"}])
    print("  %s  %s" % (both["refused"], both["why"][:56]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
