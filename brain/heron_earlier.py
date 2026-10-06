# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RAG-LIB-001
# Heron-Step:   8
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Answers kept under the id every template-born project shares - asked about,
never matched (FRAGMENT-ISSUES 5b-324, D-113).

    python brain/heron_earlier.py <shared id>      what is kept under it, by model

WHAT HAPPENED
-------------
Until D-113 a project's kept answers - its governing standards (D-111,
heron_designbasis), its load runs (heron_building_loads) and its sprinkler
runs (heron_sprinkler_run) - were filed under the UniqueId of the model's
Project Information element. A new project INHERITS that element from its
template, id and all. Measured 2026-10-06: "Heron loads test", a new project
from Revit 2024's default template, reported the same id as the Project2
whose answers were kept the day before, and its loads run asked nothing - it
used Project2's design weather, set points, profiles and an element id that
exists only in Project2. Every one of the owner's own 18 saved projects
descends from that one template element.

From D-113 a model's answers are filed under its OWN id, Document.CreationGUID
(DocumentPin.project_key). What was filed under the shared id before is
still the modeller's - so it is neither deleted nor attached to whichever
model asks next. This module is what happens to it instead.

WHAT IT DOES
------------
1. DESCRIBES what is kept under the shared id, grouped by the model name each
   answer was given for. The files say: a standards record carries the
   `project_name` it was last given under, a load run the `document` it was
   worked out in, a sprinkler run the document its network was read from.
2. ASKS, once per model, whether one of those names is THIS model - and uses
   nothing until the modeller says (D-33: no design value the modeller did
   not give for this project).
3. On a name, COPIES what was given under that name into this model's own
   files. On "none", copies nothing. Either answer is recorded beside the
   model's other files, so the question is not asked twice (D-33, asks once).

WHAT IT NEVER DOES
------------------
- Moves, renames, rewrites or deletes a file under the shared id. Another
  model made from the same template may yet claim the same answers, and the
  owner's word on 2026-10-06 was that his files are not moved or deleted
  without being asked by name. A copy costs a few kilobytes.
- Overwrites one of the model's OWN files. What the model already has was
  given for it; an earlier answer for a model of the same name is kept back
  and the answer says so.
- Mixes two models' answers into one. A model answers once; a second answer
  is refused and says what the first one was.
- Copies the project index (`<id>.db`). It is DERIVED (heron_scope's header):
  what it lists is pointed at, not held, and is added again for the model it
  belongs to. When it lists anything, the question says so.
- Carries the report folder of the load runs. Nothing records which model
  chose it, and a report written into another job's folder is the same
  mistake as the one this module exists for.
"""

import datetime
import io
import json
import os
import pathlib
import shutil
import sqlite3
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

#: This model's answer to the question, beside its other files.
SUFFIX = ".earlier.json"
FORMAT = 1

#: The bucket for a kept run that names no model.
NO_NAME = "(no model name recorded)"

#: What the modeller says when none of the names is this model.
NONE_WORDS = ("none", "no", "neither", "not this model", "none of them")


def _scope():
    import heron_designbasis
    return heron_designbasis._scope()


def _projects():
    base = _scope().knowledge_dir()
    if base is None:
        raise ValueError("No %APPDATA% and no HERON_KNOWLEDGE, so there is nowhere Heron "
                         "keeps a project's answers. Set HERON_KNOWLEDGE to a folder.")
    return os.path.join(base, "projects")


def _now():
    return datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def _day(stamp):
    return (stamp or "")[:10] or "an unknown date"


def _name(value):
    """A model name as a kept file recorded it, or None for anything else."""
    return value.strip() if isinstance(value, str) and value.strip() else None


def _read_json(path):
    try:
        with io.open(path, encoding="utf-8") as fh:
            got = json.loads(fh.read())
    except (OSError, ValueError):
        return None
    return got if isinstance(got, dict) else None


def record_path(project_key):
    """Where this model's answer to the question is kept."""
    return os.path.join(_projects(), _scope()._safe_key(project_key) + SUFFIX)


def decision(project_key):
    """This model's recorded answer, or None while it has not given one."""
    if not project_key:
        return None
    try:
        path = record_path(project_key)
    except ValueError:
        return None
    if not os.path.isfile(path):
        return None
    return _read_json(path) or {"answer": "(a record that could not be read)",
                                "path": path}


# --- what is kept under the shared id ---------------------------------------

def _standards():
    import heron_designbasis
    return heron_designbasis


def _runs_in(folder, skip_prefix):
    """(file name, parsed run) for every run file in a runs folder."""
    if not os.path.isdir(folder):
        return []
    out = []
    for name in sorted(os.listdir(folder)):
        if not name.endswith(".json") or name.startswith(skip_prefix) \
                or name == "report-folder.json":
            continue
        got = _read_json(os.path.join(folder, name))
        if got is not None:
            out.append((name, got))
    return out


def _loads_folder(key):
    import heron_building_loads
    return heron_building_loads._folder(key)


def _sprinkler_folder(key):
    import heron_sprinkler_run
    return heron_sprinkler_run._folder(key)


def _indexed(earlier_key):
    """How many documents the shared id's project index lists - read only, and
    0 when there is no index or it cannot be read. heron_scope.open_scope is
    not used: it writes the store's meta rows on opening, and nothing under
    the shared id is written to."""
    try:
        path = os.path.join(_projects(), _scope()._safe_key(earlier_key) + ".db")
    except ValueError:
        return 0
    if not os.path.isfile(path):
        return 0
    try:
        # AN ADDRESS, BUILT BY pathlib, NOT BY HAND. "file:%s?mode=ro" was cut
        # short by a "#" in the folder's name - SQLite then opened the part
        # before it read-write and CREATED a file there - and broken by a "%".
        # Found by the review of this change, 2026-10-06.
        address = pathlib.Path(os.path.abspath(path)).as_uri() + "?mode=ro"
        db = sqlite3.connect(address, uri=True)
        try:
            return db.execute("SELECT COUNT(*) FROM documents").fetchone()[0]
        finally:
            db.close()
    except (sqlite3.Error, ValueError):
        return 0


def kept(earlier_key):
    """
    What is filed under `earlier_key`, by the model name each answer was given
    for: {name: {"standards": {discipline: count}, "standards_when",
    "loads": [file], "loads_when", "profiles": [...], "takeoffs": [...],
    "sprinkler": [file], "sprinkler_when", "networks": [...]}}. {} when
    nothing is - including when the knowledge folder cannot be found.
    """
    groups = {}

    def group(name):
        return groups.setdefault(name or NO_NAME, {
            "standards": {}, "standards_when": None, "standard_files": [],
            "loads": [], "loads_when": None, "profiles": set(), "takeoffs": set(),
            "sprinkler": [], "sprinkler_when": None, "networks": set()})

    if not earlier_key:
        return {}
    # A FILE NOT IN THE SHAPE HERON WRITES IS PASSED OVER, NEVER A CRASH. This
    # read every field as if it were, and one standards record holding a bare
    # value where Heron writes {"value", "recorded"} raised out of here - which
    # took down the HVAC, fire, loads and sprinkler answers of every model made
    # from that template, since each asks this question first. Found by the
    # review of this change, 2026-10-06. A record passed over stays on disk as
    # it is, exactly as heron_designbasis.read leaves one it cannot use.
    try:
        keep = _standards()
        for discipline in sorted(keep.SUFFIXES):
            path = keep.path_for(earlier_key, discipline)
            if not os.path.isfile(path):
                continue
            try:
                data = keep._load(path)
            except (ValueError, OSError):
                continue
            if not data["standards"]:
                continue
            g = group(_name(data.get("project_name")))
            g["standards"][discipline] = len(data["standards"])
            g["standard_files"].append(discipline)
            said = max((str(v.get("recorded") or "") for v in data["standards"].values()),
                       default="")
            g["standards_when"] = max(g["standards_when"] or "", said) or None

        folder = _loads_folder(earlier_key)
        for name, run in _runs_in(folder, "takeoff-"):
            g = group(_name(run.get("document")))
            g["loads"].append(name)
            g["loads_when"] = max(g["loads_when"] or "", str(run.get("when") or "")) or None
            inputs = run.get("inputs")
            profiles = inputs.get("profiles") if isinstance(inputs, dict) else None
            if isinstance(profiles, dict):
                g["profiles"].update(str(p) for p in profiles)
            if run.get("takeoff_fingerprint"):
                g["takeoffs"].add(str(run["takeoff_fingerprint"]))

        folder = _sprinkler_folder(earlier_key)
        for name, run in _runs_in(folder, "network-"):
            fingerprint = str(run.get("network_fingerprint") or "")
            network = _read_json(os.path.join(folder, "network-%s.json" % fingerprint)) \
                if fingerprint else None
            g = group(_name((network or {}).get("document")) or _name(run.get("document")))
            g["sprinkler"].append(name)
            g["sprinkler_when"] = max(g["sprinkler_when"] or "", str(run.get("when") or "")) \
                or None
            if fingerprint:
                g["networks"].add(fingerprint)
    except ValueError:
        return {}

    for g in groups.values():
        g["profiles"] = sorted(g["profiles"])
        g["takeoffs"] = sorted(g["takeoffs"])
        g["networks"] = sorted(g["networks"])
    return groups


#: A discipline as a modeller says it.
DISCIPLINES = {"hvac": "HVAC", "fire": "fire protection"}


def _what(g):
    """What one model name holds, in a modeller's words."""
    parts = []
    for discipline in sorted(g["standards"]):
        parts.append("%s standards (%d, kept %s)" % (DISCIPLINES.get(discipline, discipline),
                                                     g["standards"][discipline],
                                                     _day(g["standards_when"])))
    if g["loads"]:
        parts.append("%d load run%s, the last %s%s" % (
            len(g["loads"]), "" if len(g["loads"]) == 1 else "s", _day(g["loads_when"]),
            (" (room profiles %s)" % ", ".join(g["profiles"])) if g["profiles"] else ""))
    if g["sprinkler"]:
        parts.append("%d sprinkler run%s, the last %s" % (
            len(g["sprinkler"]), "" if len(g["sprinkler"]) == 1 else "s",
            _day(g["sprinkler_when"])))
    return "; ".join(parts)


def _describe(name, g):
    return '  - "%s": %s' % (name, _what(g))


def question(project_key, earlier_key):
    """
    The question to put to the modeller, or None when there is nothing to ask:
    no model id (Revit 2020 to 2023 have none), no shared id, the model's own
    id given as the shared one, nothing kept under it, or a model that has
    already answered.
    """
    if not project_key or not earlier_key or project_key == earlier_key:
        return None
    if decision(project_key):
        return None
    groups = kept(earlier_key)
    if not groups:
        return None
    lines = ["EARLIER ANSWERS - NOT USED HERE, a question for the modeller (5b-324).",
             "Before D-113 Heron filed a project's answers under its Project Information id, "
             "which every model made from the same template shares - so one model's answers "
             "were used in another. This model now keeps its own, and these earlier ones were "
             "NOT used:"]
    for name in sorted(groups, key=lambda n: (n == NO_NAME, n.lower())):
        lines.append(_describe(name, groups[name]))
    indexed = _indexed(earlier_key)
    if indexed:
        lines.append("  The project index under the shared id also lists %d document(s). It "
                     "is not copied: add them again for the model they belong to." % indexed)
    lines.append("Ask the modeller whether one of these names is THIS model. If one is, "
                 "call heron_earlier_answers with use=\"<that name>\" and Heron copies those "
                 "answers to this model - the old files stay exactly as they are. If none "
                 "is, call it with use=\"none\" and Heron will not ask again for this model. "
                 "Never answer this for the modeller (D-33).")
    return "\n".join(lines)


# --- the answer ----------------------------------------------------------------

def _folder_for(path, made):
    folder = os.path.dirname(path)
    if not os.path.isdir(folder):
        os.makedirs(folder)
        made["folders"].append(folder)


def _write_whole(path, data, made):
    """Written whole and moved into place, as heron_designbasis writes."""
    _folder_for(path, made)
    temporary = path + ".writing"
    made["files"].append(temporary)
    with io.open(temporary, "w", encoding="utf-8") as fh:
        fh.write(json.dumps(data, indent=2, sort_keys=True))
    os.replace(temporary, path)
    made["files"].append(path)


def _copy(source, target, what, copied, kept_back, made):
    """Copy one file, never over one the model already has. `what` says what
    it is in a modeller's words, for the answer."""
    if os.path.exists(target):
        kept_back.append(what)
        return
    _folder_for(target, made)
    made["files"].append(target)
    shutil.copy2(source, target)
    copied.append(os.path.basename(target))


def _undo(made):
    """Take back what ONE failed attempt made - every path in `made` was
    created by it, under the model's own id, so nothing that was there before
    and nothing under the shared id can be among them."""
    for path in reversed(made["files"]):
        try:
            os.remove(path)
        except OSError:
            pass                    # never made, or already gone
    for folder in reversed(made["folders"]):
        try:
            os.rmdir(folder)
        except OSError:
            pass                    # not empty, so not only this attempt's


def decide(project_key, earlier_key, answer, by="the modeller, in the chat"):
    """
    The modeller's answer: a model name from question(), or "none". Returns
    (ok, said). On a name, what was given under it is COPIED to this model's
    own files; on "none", nothing is. Either way the answer is recorded and
    the question is not asked again. Nothing under the shared id is touched.
    """
    if not project_key:
        return False, ("Heron does not know this model's own id, so there is nowhere to "
                       "keep its answers. Revit 2024 and later report one; Revit 2020 to "
                       "2023 do not. Nothing was copied.")
    if not earlier_key or earlier_key == project_key:
        return False, "No answers are kept under a shared id for this model. Nothing was copied."
    # A COPY THAT FAILS PART WAY IS TAKEN BACK WHOLE. It used to stop with what
    # it had copied still in place and no answer recorded: the model then used
    # those answers, the question came back, and a "none" was recorded beside
    # answers still in use - the cross-model leak 5b-324 is about, by another
    # road. Found by the review of this change, 2026-10-06.
    made = {"files": [], "folders": []}
    try:
        return _decide(project_key, earlier_key, answer, by, made)
    except (ValueError, OSError) as why:
        _undo(made)
        return False, ("Nothing was copied and nothing was recorded (%s). What this attempt "
                       "had copied was taken back, so this model uses none of it, and the "
                       "question is still open." % why)


def _decide(project_key, earlier_key, answer, by, made):
    already = decision(project_key)
    if already:
        return False, ("This model has already answered: %s, on %s. Heron does not mix a "
                       "second model's earlier answers into it. Nothing was copied."
                       % ('none of them' if already.get("answer") == "none"
                          else 'use "%s"' % already.get("answer"),
                          _day(already.get("when"))))
    groups = kept(earlier_key)
    if not groups:
        return False, "Nothing is kept under the shared id any more. Nothing was copied."

    said = str(answer or "").strip()
    names = sorted(groups, key=lambda n: (n == NO_NAME, n.lower()))
    if not said:
        return False, ("No answer was given. The names are: %s - or \"none\"."
                       % ", ".join('"%s"' % n for n in names))
    if said.lower() in NONE_WORDS:
        _write_whole(record_path(project_key),
                     {"format": FORMAT, "project_key": project_key,
                      "earlier_key": earlier_key, "answer": "none", "when": _now(),
                      "by": by, "copied": [], "kept_back": []}, made)
        return True, ("Recorded: none of the earlier answers is this model's. Nothing was "
                      "copied, and Heron will not ask again for this model. The old files "
                      "are as they were.")

    matches = [n for n in names if n.lower() == said.lower()]
    if len(matches) != 1:
        return False, ("%s is not one of the names the earlier answers were given for. "
                       "They are: %s - or \"none\". Nothing was copied."
                       % ('"%s"' % said, ", ".join('"%s"' % n for n in names)))
    name = matches[0]
    g = groups[name]
    copied, kept_back = [], []

    keep = _standards()
    for discipline in g["standard_files"]:
        source = keep.path_for(earlier_key, discipline)
        target = keep.path_for(project_key, discipline)
        if os.path.exists(target):
            kept_back.append("its %s standards" % DISCIPLINES.get(discipline, discipline))
            continue
        data = keep._load(source)
        data["project_key"] = project_key
        data["copied_from"] = {"key": earlier_key, "name": name, "when": _now(), "by": by}
        _write_whole(target, data, made)
        copied.append(os.path.basename(target))

    source_folder, target_folder = _loads_folder(earlier_key), _loads_folder(project_key)
    for run in g["loads"]:
        _copy(os.path.join(source_folder, run), os.path.join(target_folder, run),
              "a load run with the same time", copied, kept_back, made)
    for fingerprint in g["takeoffs"]:
        source = os.path.join(source_folder, "takeoff-%s.json" % fingerprint)
        if os.path.isfile(source):
            _copy(source, os.path.join(target_folder, os.path.basename(source)),
                  "the same take-off", copied, kept_back, made)

    source_folder, target_folder = _sprinkler_folder(earlier_key), _sprinkler_folder(project_key)
    for run in g["sprinkler"]:
        _copy(os.path.join(source_folder, run), os.path.join(target_folder, run),
              "a sprinkler run with the same time", copied, kept_back, made)
    for fingerprint in g["networks"]:
        source = os.path.join(source_folder, "network-%s.json" % fingerprint)
        if os.path.isfile(source):
            _copy(source, os.path.join(target_folder, os.path.basename(source)),
                  "the same sprinkler network", copied, kept_back, made)

    _write_whole(record_path(project_key),
                 {"format": FORMAT, "project_key": project_key, "earlier_key": earlier_key,
                  "answer": name, "when": _now(), "by": by, "copied": copied,
                  "kept_back": kept_back}, made)
    lines = ['Copied what was kept for "%s" to this model: %s.' % (name, _what(g))]
    if kept_back:
        lines.append("Kept back, because this model already has its own: %s."
                     % ", ".join(sorted(set(kept_back))))
    lines.append("The old files are exactly as they were, and Heron will not ask again for "
                 "this model.")
    return True, " ".join(lines)


def main(argv):
    if len(argv) < 2:
        print("usage: python brain/heron_earlier.py <shared Project Information id>")
        return 2
    groups = kept(argv[1])
    if not groups:
        print("Nothing is kept under %s." % argv[1])
        return 0
    for name in sorted(groups, key=lambda n: (n == NO_NAME, n.lower())):
        print(_describe(name, groups[name]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
