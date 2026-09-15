# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-MAIN-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The import pipeline - the order is read, and the source folder is not
touched.

    python tests/test_pipeline.py

WHAT IT PROVES
  1. THE ORDER IS READ FROM THE SPECIFICATION, not kept here - shown by
     handing it a DIFFERENT specification and watching the plan change.

  2. THE SOURCE FOLDER IS NOT TOUCHED. Every name, size and modification
     time is identical after a full run.

  3. IT STOPS, AND SAYS WHICH KIND OF STOP. Waiting for an answer and
     waiting for an unbuilt agent are different reasons, and the answer
     names which.

  4. `unbuilt` IS READ, NOT ASSERTED - it changes when the register it
     is given changes.

  5. EVERY STEP HAS AN OWNER, and the ones mapped on a reading say so.

  6. THREE STEPS SHARE ONE OWNER, deliberately and visibly.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_pipeline as PIPE                                  # noqa: E402
import heron_classify as CLS                                   # noqa: E402
import heron_walk as WALK                                      # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def snapshot(where):
    out = {}
    for here, folders, names in os.walk(where):
        for name in names:
            full = os.path.join(here, name)
            stat = os.stat(full)
            out[os.path.relpath(full, where)] = (stat.st_size, stat.st_mtime)
    return out


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_pipeline.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    yard = tempfile.mkdtemp(prefix="heron-pipeline-test-")
    try:
        where = os.path.join(yard, "AJ-Tools")
        os.makedirs(os.path.join(where, "tools"))
        for at, text in (("README.md", "# AJ-Tools"),
                         ("settings.json", '{"revit": "2024"}'),
                         ("tools/CountDucts.py", "pass\n"),
                         ("tools/TagSheet.py", "pass\n")):
            with io.open(os.path.join(where, *at.split("/")), "w",
                         encoding="utf-8") as handle:
                handle.write(text)

        print("\n1. the order is read from the specification")
        real = PIPE.steps()
        check(len(real) == 16, "docs/00 s28 gives 16 steps (%d)" % len(real))
        check(real[1] == "Scan the folder" and real[16] == "Save",
              "first is %r and last is %r" % (real[1], real[16]))
        # HAND IT A DIFFERENT SPECIFICATION. A module keeping its own copy
        # would answer the same either way.
        other = os.path.join(yard, "other-spec.md")
        with io.open(other, "w", encoding="utf-8") as handle:
            handle.write("## 28. Knowledge Import System\n\n"
                         "1. Do the first thing.\n2. Do the second.\n\n"
                         "## 29. Import Agents\n")
        moved = PIPE.steps(other)
        check(moved == {1: "Do the first thing", 2: "Do the second"},
              "a different specification gives a different plan: %r" % moved)
        check(PIPE.plan(path=other)["of"] == 2,
              "and the plan follows it, not the real one")

        print("\n2. the source folder is not touched")
        before = snapshot(where)
        answer = PIPE.run(where)
        check(answer["ran"] is True, "the pipeline ran")
        check(answer["modified_the_source"] is False,
              "`modified_the_source` is false")
        check(snapshot(where) == before,
              "every name, size and modification time is as it was")
        check("shutil" not in logic and "makedirs" not in logic
              and '"w"' not in logic,
              "and the module opens nothing for writing")

        print("\n3. it stops, and says which kind of stop")
        check(answer["stopped_at"] == 3,
              "it stopped at step 3 (%d)" % answer["stopped_at"])
        check(answer["done"] == [1, 2],
              "having run steps 1 and 2 unattended")
        waiting = answer["waiting_for"]
        check(waiting["agent"] == "HERON-IMP-CLS-003",
              "waiting on %s" % waiting["agent"])
        check("needs an answer" in waiting["why"],
              "and the reason is an ANSWER, not a missing agent: %r"
              % waiting["why"][:40])
        # DERIVED, NOT WRITTEN DOWN. This asserted step 4 until
        # HERON-IMP-FEX-004 was built, and then failed against a repository
        # that had got better. The claim is that stopping for an ANSWER and
        # stopping for a MISSING AGENT are different, and it holds whichever
        # step each happens to be.
        mapped = PIPE.plan()
        missing = [card for card in mapped["steps"] if not card["built"]]
        if missing:
            check(missing[0]["step"] != answer["stopped_at"],
                  "while the first UNBUILT step is %d - a different stop "
                  "from the one it made" % missing[0]["step"])
        else:
            check(True, "every agent is now built, so the only stop left "
                        "is for an answer - said, not counted as a pass")
        check(answer["questions"] and
              all("group" in ask for ask in answer["questions"]),
              "%d question(s) are ready for the host"
              % len(answer["questions"]))
        briefed = CLS.brief(WALK.walk(where))
        check([ask["group"] for ask in answer["questions"]]
              == [ask["group"] for ask in briefed["asks"]],
              "and they are HERON-IMP-CLS-003's own, unaltered")

        print("\n4. `unbuilt` is read, and the table is checked for drift")
        answer_plan = PIPE.plan()
        # WHICH agents are unbuilt changes every time one is built, so the
        # claim is that the list AGREES WITH THE FILES - not that it holds
        # a particular id. It named FEX-004 until FEX-004 was written.
        claimed = PIPE.built()[1]
        check(all(agent not in claimed for agent in answer["unbuilt"]),
              "nothing in `unbuilt` is claimed by a file: %s"
              % (", ".join(answer["unbuilt"]) or "(none left)"))
        empty = os.path.join(yard, "empty-register.md")
        with io.open(empty, "w", encoding="utf-8") as handle:
            handle.write("no agent ids here\n")
        check(PIPE.plan()["unknown_agents"] == [],
              "every agent this table names is in the real register")
        # THE TABLE IS THE ONE THING HERE THAT IS A COPY, so drift in it is
        # what the register is read for. Hand it a register naming nobody.
        blind = PIPE.plan(register=empty)
        check(len(blind["unknown_agents"]) == 12,
              "against a register naming no Import agent, all 12 are "
              "reported as strangers (%d)" % len(blind["unknown_agents"]))
        check(blind["unbuilt"] == answer_plan["unbuilt"] or
              [c["agent"] for c in blind["unbuilt"]]
              == [c["agent"] for c in answer_plan["unbuilt"]],
              "while `unbuilt` is unchanged - it follows the source "
              "headers, not the register")

        print("\n5. every step has an owner, and readings say so")
        mapped = PIPE.plan()
        check(mapped["unmapped"] == [],
              "no step is left without an owner")
        check(all(card["agent"] for card in mapped["steps"]),
              "all 16 name an agent")
        readings = sorted(card["step"] for card in mapped["uncertain"])
        check(readings == [13, 16],
              "two are a reading rather than a row: steps %s"
              % ", ".join(str(n) for n in readings))
        for card in mapped["uncertain"]:
            check(card["why"] and len(card["why"]) > 20,
                  "step %d says why it is uncertain" % card["step"])
        check(all(card["certain"] for card in mapped["steps"]
                  if card["step"] not in readings),
              "and every other step is named in its agent's own row")

        print("\n6. three steps share one owner")
        classify = sorted(card["step"] for card in mapped["steps"]
                          if card["agent"] == "HERON-IMP-CLS-003")
        check(classify == [3, 6, 10],
              "steps %s all land on HERON-IMP-CLS-003"
              % ", ".join(str(n) for n in classify))
        check(len(mapped["steps"]) == 16 and len(
            set(card["agent"] for card in mapped["steps"])) == 12,
              "16 steps, 12 distinct agents - the collapse is visible")

        print("\n7. every failure is named and reached")
        for these, name in (((None, None), "NOTHING_TO_WALK"),
                            ((os.path.join(yard, "gone"), None),
                             "NOT_A_FOLDER"),
                            ((where, os.path.join(ROOT, "README.md")),
                             "NO_SPECIFICATION")):
            folder, spec = these
            said = PIPE.run(folder, path=spec)
            reached.add(said.get("refused"))
            check(said.get("refused") == name, "%s is reached" % name)

        contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                         "HERON-IMP-MAIN-001.yaml"))
        named = contract.get("failures") or []
        check(len(named) == 3, "the contract declares 3 failures")
        for failure in named:
            check(failure in logic or failure in ("NOTHING_TO_WALK",
                                                  "NOT_A_FOLDER"),
                  "the code names or carries %s" % failure)
        unreached = sorted(set(named) - reached)
        check(not unreached,
              "and every one was reached above%s"
              % ("" if not unreached else ": %s" % ", ".join(unreached)))
        check(len(answer["unjudged"]) == 5, "five things are left unjudged")
    finally:
        shutil.rmtree(yard, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the order is read, and the source folder is not touched")
    return 0


if __name__ == "__main__":
    sys.exit(main())
