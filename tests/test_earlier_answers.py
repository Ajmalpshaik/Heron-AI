#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   8
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Answers kept under the id every template-born project shares are a QUESTION,
never a match (FRAGMENT-ISSUES 5b-324, D-113).

WHAT WENT WRONG. Until this change a project's kept answers - its governing
standards (D-111), its load runs, its sprinkler runs - were filed under the
UniqueId of the model's Project Information element. A new project inherits
that element from its TEMPLATE, id and all, so every model made from Revit's
default template shared one file. Measured 2026-10-06: "Heron loads test", a
new project, asked no question at all and used Project2's design weather, set
points, Office profile and door absorptance, and an element id from Project2.

THE OLD FILES ARE STILL THE MODELLER'S ANSWERS, so they are neither deleted
nor quietly attached to whichever model asks next. This suite holds the
brain half of that: what is filed under the shared id is described by the
model name each answer was given for, nothing is used until the modeller
names one, the names chosen are COPIED to the model's own files (the old
files stay byte for byte), and each model is asked once.

Runs against a scratch HERON_KNOWLEDGE, never the owner's own folder.

    python tests/test_earlier_answers.py
"""

import hashlib
import io
import json
import os
import shutil
import sqlite3
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

SCRATCH = tempfile.mkdtemp(prefix="heron-earlier-")
os.environ["HERON_KNOWLEDGE"] = SCRATCH

FAILURES = []

# The shared id, as the add-in reported it for every template-born project on
# 2026-09-15 and 2026-10-06; the two model ids are CreationGUIDs - the first
# read from "Heron loads test" in Revit 2024 on 2026-10-06.
OLD = "8764c510-57b7-44c3-bddf-266d86c26380-0000c160"
NEW = "c95db604-0abd-4bc5-9ebd-3ece37b3ce52"
OTHER = "e66f36a9-173f-46c8-bdca-d5787b826308"
THIRD = "414a595d-281b-4af1-b26c-d24151fdd73a"

PROJECTS = os.path.join(SCRATCH, "projects")


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write(path, data):
    folder = os.path.dirname(path)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(json.dumps(data, indent=1))


def run(document, run_id, when, profiles, fingerprint):
    return {"format": 1, "run_id": run_id, "when": when, "document": document,
            "takeoff_fingerprint": fingerprint,
            "inputs": {"project": {"design_weather": "doha-0.4",
                                   "room_dry_bulb_c": 24},
                       "profiles": dict((p, {"people_per_m2": 0.1}) for p in profiles),
                       "overrides": {}},
            "spaces": [], "building": {"block_w": 9700}}


def arrange():
    """The owner's knowledge folder as it stood on 2026-10-06, in miniature."""
    write(os.path.join(PROJECTS, OLD + ".hvac.json"),
          {"format": 1, "project_key": OLD, "project_name": "Project2",
           "standards": dict((name, {"value": value, "recorded": "2026-10-03T16:53:00Z"})
                             for name, value in (("ventilation_standard", "62.1-2022"),
                                                 ("energy_standard", "90.1-2022"),
                                                 ("qcs_edition", "QCS 2024"),
                                                 ("cibse_beside_ashrae", "no"))),
           "history": []})
    loads = os.path.join(PROJECTS, OLD + ".loads")
    write(os.path.join(loads, "20261004-224547.json"),
          run("Project2", "20261004-224547", "2026-10-04T22:45:47",
              ["Office", "Reception"], "1bd46b86fd42738c"))
    write(os.path.join(loads, "20261005-004947.json"),
          run("Project2", "20261005-004947", "2026-10-05T00:49:47",
              ["Office", "Reception"], "c7602128204ab45e"))
    write(os.path.join(loads, "20261006-182218.json"),
          run("Heron loads test", "20261006-182218", "2026-10-06T18:22:18",
              ["Meeting Room", "Office", "Reception"], "95a5ad1a0c8dea8d"))
    for fingerprint in ("1bd46b86fd42738c", "c7602128204ab45e", "95a5ad1a0c8dea8d"):
        write(os.path.join(loads, "takeoff-%s.json" % fingerprint), {"spaces": []})
    write(os.path.join(loads, "report-folder.json"), {"folder": "C:/reports"})
    sprinkler = os.path.join(PROJECTS, OLD + ".sprinkler")
    write(os.path.join(sprinkler, "20261005-010000.json"),
          {"format": 1, "run_id": "20261005-010000", "when": "2026-10-05T01:00:00",
           "system": {"id": "77", "name": "FP 1"}, "status": "ok",
           "network_fingerprint": "aaaabbbbccccdddd"})
    write(os.path.join(sprinkler, "network-aaaabbbbccccdddd.json"),
          {"document": "Project2", "elements": []})


def fingerprints(folder):
    out = {}
    for base, _dirs, files in os.walk(folder):
        for name in files:
            path = os.path.join(base, name)
            with io.open(path, "rb") as fh:
                out[os.path.relpath(path, folder)] = hashlib.sha256(fh.read()).hexdigest()
    return out


def own_files(key):
    return sorted(name for name in os.listdir(PROJECTS) if name.startswith(key))


def main():
    print("Answers kept under the shared id are a question, never a match (5b-324)")
    try:
        import heron_earlier as E
    except ImportError as why:
        check(False, "brain/heron_earlier.py can be imported (%s)" % why)
        return finish()
    import heron_building_loads as LOADS
    import heron_designbasis as KEEP
    import heron_sprinkler_run as SPRINKLER

    arrange()
    before = fingerprints(PROJECTS)

    print()
    print("What is filed under the shared id, by the model each answer was given for")
    said = E.question(NEW, OLD)
    check(isinstance(said, str) and "Project2" in said and "Heron loads test" in said,
          "the question names both models the old answers were given for")
    check(said is not None and "NOT used" in said,
          "and says they were NOT used here")
    check(said is not None and "2026-10-05" in said and "2026-10-06" in said
          and "2026-10-03" in said,
          "with the dates each was last given")
    check(said is not None and "Reception" in said,
          "and the room profiles the load runs carry - the 'Reception' the "
          "owner saw on 2026-10-06 in a model with no Reception")
    check(said is not None and "none" in said,
          "and says how to answer that none of them is this model")
    check(own_files(NEW) == [],
          "asking writes nothing for the model: %s" % own_files(NEW))

    print()
    print("Nothing to ask")
    check(E.question(None, OLD) is None,
          "with no model id - Revit 2020 to 2023 - there is no model to ask for")
    check(E.question(NEW, None) is None, "with no shared id, nothing was kept under it")
    check(E.question(NEW, NEW) is None, "a model's own id is not an earlier one")
    check(E.question(NEW, "0b0b0b0b-0000-4000-8000-000000000000-0000c160") is None,
          "and a shared id with nothing filed under it asks nothing")

    print()
    print("A name that was not given is refused, and nothing is written")
    ok, said = E.decide(NEW, OLD, "Project9")
    check(not ok and "Project2" in said and "Heron loads test" in said,
          "the refusal lists the names there are")
    check(own_files(NEW) == [], "and writes nothing: %s" % own_files(NEW))

    print()
    print("The modeller names Project2: its answers are COPIED to this model")
    ok, said = E.decide(NEW, OLD, "project2")
    check(ok, "a name is matched whatever its case: %s" % said.splitlines()[0])
    standards, _note = KEEP.read(NEW, "hvac")
    check(sorted(standards) == ["cibse_beside_ashrae", "energy_standard",
                                "qcs_edition", "ventilation_standard"],
          "the four HVAC standards are this model's now: %s" % sorted(standards))
    with io.open(KEEP.path_for(NEW, "hvac"), encoding="utf-8") as fh:
        record = json.loads(fh.read())
    check(record.get("project_key") == NEW,
          "the copy is filed under this model's own id")
    check((record.get("copied_from") or {}).get("key") == OLD,
          "and says where it came from")
    last = LOADS.load(NEW)
    check(last is not None and last.get("run_id") == "20261005-004947",
          "the newest Project2 load run is this model's newest run")
    kept = sorted(os.listdir(LOADS._folder(NEW)))
    check("20261006-182218.json" not in kept,
          "and the run given for 'Heron loads test' was NOT copied: %s" % kept)
    check("takeoff-c7602128204ab45e.json" in kept
          and "takeoff-95a5ad1a0c8dea8d.json" not in kept,
          "each copied run brings the take-off it was worked out from, and no other")
    check("report-folder.json" not in kept,
          "the report folder is not carried - nothing says which model chose it")
    check(SPRINKLER.load(NEW) is not None,
          "the sprinkler run whose network was read from Project2 is copied too")
    now = fingerprints(PROJECTS)
    check(all(now.get(name) == digest for name, digest in before.items()),
          "and EVERY old file is still there, byte for byte - copied, never moved")
    check(E.question(NEW, OLD) is None,
          "asked once: the model has answered, so it is not asked again")
    ok, said = E.decide(NEW, OLD, "Heron loads test")
    check(not ok and "Project2" in said,
          "and a second answer is refused rather than mixing two models' "
          "answers into one")

    print()
    print("Another model from the same template is asked for itself")
    said = E.question(OTHER, OLD)
    check(said is not None and "Project2" in said,
          "the old answers were not used up by the first model's copy")
    ok, said = E.decide(OTHER, OLD, "none")
    check(ok, "'none' is an answer")
    check(KEEP.read(OTHER, "hvac")[0] == {} and LOADS.load(OTHER) is None,
          "and copies nothing")
    check(E.question(OTHER, OLD) is None, "and is not asked again")

    print()
    print("A model's own answers are never overwritten by an earlier one")
    KEEP.record(THIRD, {"ventilation_standard": "62.1-2019"}, "School")
    ok, said = E.decide(THIRD, OLD, "Project2")
    check(ok, "the copy goes ahead for what this model does not have")
    check(KEEP.read(THIRD, "hvac")[0]["ventilation_standard"]["value"] == "62.1-2019",
          "its own HVAC standards are kept as they were")
    check("kept" in said.lower() and "hvac" in said.lower(),
          "and the answer says which earlier file was kept back")
    check(LOADS.load(THIRD) is not None, "while the load runs it lacked are copied")

    print()
    print("The shared id itself is never written to")
    after = fingerprints(PROJECTS)
    check(all(after.get(name) == digest for name, digest in before.items()),
          "after three models answered, the old files are exactly as they were")

    # --- found by the review of this change, 2026-10-06 -------------------
    print()
    print("A copy that fails part way leaves nothing behind")
    fourth = "5a5a5a5a-1111-4222-8333-444444444444"
    blocker = SPRINKLER._folder(fourth)       # a FILE where its folder must go
    with io.open(blocker, "w", encoding="utf-8") as fh:
        fh.write("not a folder")
    try:
        ok, said = E.decide(fourth, OLD, "Project2")
    except Exception as why:                                    # noqa: BLE001
        ok, said = None, "raised %s: %s" % (type(why).__name__, why)
    check(ok is False, "the answer says the copy failed: %s" % said[:90])
    check(KEEP.read(fourth, "hvac")[0] == {} and LOADS.load(fourth) is None,
          "and nothing copied before the failure is left for the model to use - "
          "the standards and load runs stayed, and a later 'none' would have been "
          "recorded beside answers still in use")
    check(E.decision(fourth) is None and E.question(fourth, OLD) is not None,
          "no answer is recorded, so the question is asked again")
    os.remove(blocker)

    print()
    print("A record not in the shape Heron writes is passed over, never a crash")
    bad, fifth = "0badbad0-57b7-44c3-bddf-266d86c26380-0000c160", \
        "6b6b6b6b-1111-4222-8333-555555555555"
    write(os.path.join(PROJECTS, bad + ".hvac.json"),
          {"format": 1, "project_name": "Old job",
           "standards": {"ventilation_standard": "62.1-2022"}})
    write(os.path.join(PROJECTS, bad + ".loads", "20261001-100000.json"),
          {"run_id": "20261001-100000", "when": "2026-10-01T10:00:00",
           "document": "Old job", "inputs": ["not", "an", "object"]})
    try:
        said, raised = E.question(fifth, bad), None
    except Exception as why:                                    # noqa: BLE001
        said, raised = None, why
    check(raised is None, "asking does not raise on it: %r" % (raised,) if raised
          else "asking does not raise on it - one such record broke the HVAC, fire, "
               "loads and sprinkler answers of every model from that template")
    check(said is not None and "Old job" in said and "1 load run" in said,
          "and the run that CAN be read is still offered")
    try:
        ok, said = E.decide(fifth, bad, "Old job")
    except Exception as why:                                    # noqa: BLE001
        ok, said = None, "raised %s: %s" % (type(why).__name__, why)
    check(ok is True and LOADS.load(fifth) is not None,
          "and answering copies what can be read: %s" % said[:90])
    check(KEEP.read(fifth, "hvac")[0] == {},
          "but not the standards record that cannot be")

    print()
    print("A knowledge folder with a # in its name is read as it is")
    hashed = os.path.join(SCRATCH, "with#hash")
    os.environ["HERON_KNOWLEDGE"] = hashed
    try:
        write(os.path.join(hashed, "projects", OLD + ".hvac.json"),
              {"format": 1, "project_key": OLD, "project_name": "Project2", "history": [],
               "standards": {"qcs_edition": {"value": "QCS 2024",
                                             "recorded": "2026-10-03T16:53:00Z"}}})
        db = sqlite3.connect(os.path.join(hashed, "projects", OLD + ".db"))
        db.execute("CREATE TABLE documents (id TEXT)")
        db.execute("INSERT INTO documents VALUES ('a clause')")
        db.commit()
        db.close()
        said = E.question(NEW, OLD)
        check(said is not None and "1 document" in said,
              "the project index under the shared id is counted and named in the "
              "question")
        check(not os.path.exists(os.path.join(SCRATCH, "with")),
              "and nothing is made beside it - the # cut the address short and "
              "SQLite created a file where it was cut")
    finally:
        os.environ["HERON_KNOWLEDGE"] = SCRATCH
    return finish()


def finish():
    print()
    if FAILURES:
        print("FAILED")
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("PASSED - earlier answers are asked about, copied on the modeller's word, "
          "and never moved.")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    finally:
        # Cleaned up however it ends - a check that raised used to leave the
        # scratch folder behind (the review of this change).
        shutil.rmtree(SCRATCH, ignore_errors=True)
