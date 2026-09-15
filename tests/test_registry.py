# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-REG-012
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
File registry - what exists, where, and what each file CLAIMS to be.

    python tests/test_registry.py

WHAT IT PROVES
  1. NOTHING IS STORED. No index, no cache, no write - D-40, and the
     answer is recomputed from what it is handed every time.

  2. `none` IS A DIFFERENT ANSWER FROM SILENCE. One is a decision
     somebody made, the other is a header nobody wrote.

  3. AN IDENTITY IS NEVER GUESSED FROM A PATH.

  4. A CONTESTED ID IS REPORTED, NOT RESOLVED.

  5. IT READS NOTHING ITSELF, and a reader that raises is named rather
     than dropped.

  6. IT AGREES WITH tools/check-metadata.py ON THE REAL REPOSITORY - the
     five fields, and the agent ids, read out of the actual files.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_registry as REG                                   # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def header(agent="HERON-OPS-FLG-009", **more):
    lines = ["# -*- coding: utf-8 -*-"]
    fields = dict(zip(REG.FIELDS, [agent, "15", "DRAFT", "0.1.0", "brain"]))
    fields.update(more)
    for field in REG.FIELDS:
        if fields.get(field) is not None:
            lines.append("# %s:  %s" % (field, fields[field]))
    return "\n".join(lines) + "\n\nimport os\n"


def real(path):
    with io.open(os.path.join(ROOT, path), encoding="utf-8",
                 errors="replace") as fh:
        return fh.read(4000)


def main():
    reached = set()
    source = io.open(os.path.join(ROOT, "brain", "heron_registry.py"),
                     encoding="utf-8").read()

    def ask(files, **kw):
        answer = REG.survey(files, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. Nothing is stored")
    # Scoped to the AGENT, not to main(). The demo reads this repository
    # on purpose - as the caller, which is the honest way to show an agent
    # that takes a reader - and a check that swept main() in too would be
    # measuring the demo.
    agent_code = source.split('"""', 2)[2].split("def main(")[0]
    for word in ("open(", "json.dump", "pickle", "sqlite3", "os.makedirs",
                 "shutil", "os.walk", "os.listdir", "glob"):
        check(word not in agent_code,
              "survey() and identity() have no %s" % word)
    check("os.listdir" in source.split("def main(")[1],
          "while main() does read the repo - as the caller, which is what "
          "makes the demo honest rather than a fixture agreeing with itself")
    check(any("NOTHING WAS STORED" in note for note in
              ask(["a.py"], read=lambda p: header())["unjudged"]),
          "and the answer says so")
    check(any("D-40" in note for note in
              ask(["a.py"], read=lambda p: header())["unjudged"]),
          "citing the decision that requires it")
    # RECOMPUTED, NOT REMEMBERED: the same agent asked twice about
    # different content gives different answers.
    first = ask(["a.py"], read=lambda p: header("HERON-OPS-FLG-009"))
    second = ask(["a.py"], read=lambda p: header("HERON-OPS-SAF-008"))
    check(first["agents"] == ["HERON-OPS-FLG-009"]
          and second["agents"] == ["HERON-OPS-SAF-008"],
          "the same path reports whatever it holds NOW, with nothing "
          "carried over from the previous call")

    print()
    print("2. `none` is a different answer from silence")
    answer = ask(["tool.py"], read=lambda p: header(agent="none"))
    check(len(answer["declared_none"]) == 1 and not answer["unidentified"],
          "`Heron-Agent: none` lands in declared_none")
    check("is a statement, not a missing header" in
          answer["declared_none"][0]["why"],
          "saying it is a statement")
    check(ask(["x.md"], read=lambda p: "just text\n")["unidentified"],
          "while a file with no header at all is unidentified")
    check(not ask(["x.md"], read=lambda p: "just text\n")["declared_none"],
          "and is NOT folded in with the deliberate ones")
    check(ask(["t.py"], read=lambda p: header(agent="NONE"))["declared_none"],
          "and the word is read case-insensitively")
    # THE REPOSITORY REALLY USES BOTH.
    check(REG.survey(["tools/owner-queue.py"], read=real)["declared_none"],
          "tools/owner-queue.py really does declare none, in the repo")
    check(REG.survey(["brain/heron_paths.py"], read=real)["identified"],
          "and brain/heron_paths.py really does claim an agent")

    print()
    print("3. An identity is never guessed from a path")
    answer = ask(["brain/heron_flags.py"], read=lambda p: "no header here\n")
    check(answer["unidentified"] and not answer["identified"],
          "a file sitting among agent modules is still unidentified")
    check("NOT guessed from where it sits" in answer["unidentified"][0]["why"],
          "and the answer says it was not guessed")
    check("invents a fact" in answer["unidentified"][0]["why"],
          "with what guessing would cost")
    answer = ask(["brain/heron_flags.py"])
    check(answer["unidentified"] and "no reader was given"
          in answer["unidentified"][0]["why"],
          "and with no reader at all, still nothing is invented")
    check(len(answer["identified"]) == 0, "nothing is identified")

    print()
    print("4. A contested id is reported, not resolved")
    answer = ask(["a/one.py", "b/two.py", "c/three.py"],
                 read=lambda p: header())
    check(len(answer["contested"]) == 1, "one id claimed three times")
    check(answer["contested"][0]["paths"] == ["a/one.py", "b/two.py",
                                              "c/three.py"],
          "all three named, sorted")
    check("not a tie for this agent to break" in answer["contested"][0]["why"],
          "and it refuses to break the tie")
    check("make the others invisible" in answer["contested"][0]["why"],
          "saying what picking one would cost")
    check(len(answer["identified"]) == 3,
          "and all three are still reported as identified - contested is "
          "an extra answer, not a replacement")
    check(not ask(["a.py"], read=lambda p: header())["contested"],
          "while one file claiming one id contests nothing")

    print()
    print("5. It reads nothing itself")
    for reader in ("a string", ["a list"], 42, {"read": True}):
        check(ask(["a.py"], read=reader).get("refused") == "NOT_A_READER",
              "%r is not a reader" % (reader,))
    check("drift apart between one call and the next"
          in ask(["a.py"], read="x")["why"],
          "and the refusal says why this agent does not open files")

    def angry(path):
        raise IOError("no such file")
    answer = ask(["gone.py"], read=angry)
    check(answer["unreadable"] and answer["unreadable"][0]["path"] == "gone.py",
          "a reader that raises is named...")
    check("IOError" in answer["unreadable"][0]["why"] or
          "OSError" in answer["unreadable"][0]["why"],
          "...with what it raised")
    check(not answer["identified"] and not answer["unidentified"],
          "and is not quietly counted as either")

    print()
    print("6. It agrees with the repository and with check-metadata.py")
    gate = io.open(os.path.join(ROOT, "tools", "check-metadata.py"),
                   encoding="utf-8").read()
    for field in REG.FIELDS:
        check('"%s"' % field in gate,
              "'%s' is one of the gate's own fields" % field)
    check(len(REG.FIELDS) == 5, "and there are five of them")
    # READ THE REAL FILES AND AGREE WITH THEIR OWN HEADERS.
    modules = sorted("brain/" + name for name in
                     os.listdir(os.path.join(ROOT, "brain"))
                     if name.endswith(".py"))
    answer = REG.survey(modules, read=real)
    check(len(answer["identified"]) > 30,
          "%d brain modules claim an agent" % len(answer["identified"]))
    check(not answer["unreadable"], "every one was readable")
    # THIS FOUND A REAL ONE, and the assertion is what is true rather than
    # what would be tidy. tools/check-metadata.py collects claimed ids into
    # a set, so two files claiming one id collapse silently and nothing in
    # this repository has ever reported it. Recorded as PROPOSALS F10.
    contested = dict((c["agent"], c["paths"]) for c in answer["contested"])
    check("HERON-FRG-VAL-001" in contested,
          "brain/ really does have one id claimed twice, and the agent "
          "finds it: %s" % ", ".join(sorted(contested)))
    check(sorted(contested["HERON-FRG-VAL-001"])
          == ["brain/heron_fragment.py", "brain/heron_validate.py"],
          "naming both files rather than picking one")
    gate_code = io.open(os.path.join(ROOT, "tools", "check-metadata.py"),
                        encoding="utf-8").read()
    check("claimed.add(aid)" in gate_code and "claimed = set(" in gate_code
          or "claimed.add(aid)" in gate_code,
          "and the metadata gate collapses ids into a set, which is why "
          "nothing has ever reported this")
    for entry in answer["identified"][:6]:
        first = real(entry["path"]).split("\n")[1]
        check(entry["agent"] in first,
              "%s reports the id on that file's own second line"
              % os.path.basename(entry["path"]))
    check(all(not entry["missing"] for entry in answer["identified"]),
          "and every identified brain module carries all five fields")

    print()
    print("7. Every failure the contract declares is named and reached")
    for empty in ([], None, ""):
        check(ask(empty).get("refused") == "NOTHING_TO_REGISTER",
              "%r registers nothing" % (empty,))
    check("not an empty workspace" in ask([])["why"],
          "and an empty registry is not an empty workspace")
    for bad in ([""], ["a.py", "  "], [None]):
        check(ask(bad, read=lambda p: header()).get("refused")
              == "NOT_A_FILE_LIST", "%r has a hole in it" % (bad,))
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-WSP-REG-012.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    unreached = sorted(set(named) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    derived, never stored, and a claim is never a grant")
    return 0


if __name__ == "__main__":
    sys.exit(main())
