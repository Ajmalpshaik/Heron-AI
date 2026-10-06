# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MEP-HVD-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
A project's design basis - the standards that govern its HVAC and its fire
protection design - asked once, kept for that project, and never carried to
another (D-111).

"Design basis" is the MEP name for it: the codes and criteria a project is
designed to, written down once at the start. For HVAC it is four answers, for
fire protection two, and the module is named for the basis rather than for an
engine that asks it, so no module name in brain/ is the start of another's
(tests/test_references.py).

    python brain/heron_designbasis.py <project-key> [hvac|fire]    what is recorded

WHY IT EXISTS
-------------
Which edition of ASHRAE 62.1 governs a project, whether ASHRAE 90.1 applies,
which QCS edition, whether CIBSE is used beside ASHRAE - none of it could be
established from any source the HVAC engine was built from, and each one
changes what an answer may check. D-33 says ask, and ask ONCE, because
"always ask" without memory becomes noise that is clicked through. The owner,
2026-10-02: "ask standards once per project". The answers are kept here.

What the questions ARE, and what a valid answer looks like, is each engine's
(heron_hvac.PROJECT_STANDARDS, heron_fire.PROJECT_STANDARDS). This module only
keeps what an engine has already accepted - it stores names and values and
judges neither.

ONE FILE PER PROJECT, AND THE PROJECT IS NEVER GUESSED
-------------------------------------------------------
knowledge/projects/<key>.hvac.json, and <key>.fire.json beside it - one file per
project and discipline, beside the project's own store and named by the same
key, the model's own CreationGUID the add-in reports (heron_scope._safe_key says
why it is that and never the file name). Until D-113 it was the Project
Information UniqueId, which every model made from one template shares - so
Project2's answers were used in another model (FRAGMENT-ISSUES 5b-324); what
was kept under it is asked about, never used (heron_earlier). A discipline's answers never
land in another's file, so an HVAC question is never answered from a fire
record. No key, no file: a project this chat has not seen is asked again
rather than filed under a guess, because one client's answer in another
client's file is a breach, not a bug (docs/10 s2, Golden Rule 5).

THIS IS DATA, NOT AN INDEX
--------------------------
The .db files beside it are derived and safe to delete. This file is not: it
is what the modeller said. So it is written whole to a temporary file and
moved into place; a file that cannot be read is SET ASIDE under a new name and
kept, never overwritten and never deleted; and a changed answer REPLACES the
old one and RECORDS the replacement (docs/10 s276), so the record of who said
what survives a change of mind.
"""

import datetime
import io
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

# One file per discipline. SUFFIX is HVAC's, as it was before fire had one.
SUFFIXES = {"hvac": ".hvac.json", "fire": ".fire.json"}
SUFFIX = SUFFIXES["hvac"]
FORMAT = 1


def _scope():
    # Imported here, not at the top: heron_scope reads fragment cards and so
    # needs PyYAML, and the engine this module serves needs nothing beyond
    # Python itself. Without it, asking for memory is refused in words and the
    # calculation still runs - it just keeps nothing.
    try:
        import heron_scope
    except ImportError as why:
        raise ValueError("a project's answers cannot be kept here - the knowledge "
                         "folder's module could not be loaded (%s)" % why)
    return heron_scope


def path_for(project_key, discipline="hvac"):
    """The one file this project's answers for one discipline live in."""
    if discipline not in SUFFIXES:
        raise ValueError("no design basis is kept for %r - only for %s"
                         % (discipline, ", ".join(sorted(SUFFIXES))))
    if not project_key:
        raise ValueError(
            "No project key, so there is no file to name. Heron does not guess "
            "which project this is (D-33) - ask, then pass the key.")
    scope = _scope()
    base = scope.knowledge_dir()
    if base is None:
        raise ValueError(
            "No %APPDATA% and no HERON_KNOWLEDGE, so there is nowhere to keep a "
            "project's answers. Set HERON_KNOWLEDGE to a folder.")
    return os.path.join(base, "projects", scope._safe_key(project_key) + SUFFIXES[discipline])


def _now():
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _load(path):
    """The parsed record, or ValueError naming what is wrong with it."""
    with io.open(path, encoding="utf-8") as handle:
        data = json.loads(handle.read())
    if not isinstance(data, dict) or not isinstance(data.get("standards"), dict):
        raise ValueError("it holds no standards object")
    for name, entry in data["standards"].items():
        if not isinstance(entry, dict) or "value" not in entry:
            raise ValueError("its entry for %s has no value" % name)
    if not isinstance(data.get("history", []), list):
        raise ValueError("its history is not a list")
    return data


def read(project_key, discipline="hvac"):
    """
    (standards, note) for one project and discipline. `standards` maps each
    recorded name to {"value", "recorded"}; `note` is None, or the sentence
    saying why nothing recorded could be used.
    """
    if not project_key:
        return {}, None
    path = path_for(project_key, discipline)
    if not os.path.exists(path):
        return {}, None
    try:
        return _load(path)["standards"], None
    except (ValueError, OSError) as why:
        return {}, ("this project's recorded standards could not be read (%s), so "
                    "none was used - the file is kept as it is: %s" % (why, path))


def record(project_key, given, project_name=None, when=None, discipline="hvac"):
    """
    Keep what was given for one project and discipline. Returns (changes,
    note): `changes` is a list of (name, old value or None, new value), empty
    when nothing differed from the record; `note` is None or a sentence about
    the file itself.

    A value equal to the one recorded changes nothing and writes nothing. A
    different one replaces it, and the replaced value goes into the record's
    history with when it was said and when it was replaced.
    """
    if not project_key or not given:
        return [], None
    path = path_for(project_key, discipline)
    note = None
    data = {"format": FORMAT, "project_key": str(project_key), "standards": {},
            "history": []}
    if os.path.exists(path):
        try:
            data = _load(path)
        except (ValueError, OSError) as why:
            # NEVER OVERWRITTEN. Set aside under a name that says when, so the
            # modeller's earlier answers are still on disk to be read by a
            # person, and a new record is started.
            aside = "%s.unreadable-%s" % (path, _now().replace(":", ""))
            os.replace(path, aside)
            note = ("the old record could not be read (%s); it was set aside as %s "
                    "and a new one started" % (why, os.path.basename(aside)))
    when = when or _now()
    changes = []
    for name in sorted(given):
        value = given[name]
        old = data["standards"].get(name)
        if old is not None and old.get("value") == value:
            continue
        if old is not None:
            data.setdefault("history", []).append(
                {"name": name, "value": old.get("value"), "said": old.get("recorded"),
                 "replaced_by": value, "replaced": when})
        data["standards"][name] = {"value": value, "recorded": when}
        changes.append((name, None if old is None else old.get("value"), value))
    if project_name and data.get("project_name") != project_name:
        data["project_name"] = project_name
    elif not changes:
        return [], note
    folder = os.path.dirname(path)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    temporary = path + ".writing"
    with io.open(temporary, "w", encoding="utf-8") as handle:
        handle.write(json.dumps(data, indent=2, sort_keys=True))
    os.replace(temporary, path)
    return changes, note


def main(argv):
    if len(argv) < 2:
        print("usage: python brain/heron_designbasis.py <project-key> [hvac|fire]")
        return 2
    discipline = argv[2] if len(argv) > 2 else "hvac"
    try:
        standards, note = read(argv[1], discipline)
        path = path_for(argv[1], discipline)
    except ValueError as why:
        print(str(why))
        return 2
    print(path)
    if note:
        print(note)
    if not standards:
        print("nothing recorded for this project")
        return 0
    for name in sorted(standards):
        print("  %-22s %-12s recorded %s" % (name, standards[name]["value"],
                                              standards[name].get("recorded")))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
