# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-DEP-005
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Dependencies - confirm, never automatic, and degrade only out loud.

    python tests/test_dependencies.py

WHAT IT PROVES
  1. EVERY DEGRADED PATH COMES BACK WITH THE SENTENCE THAT HAS TO BE SAID,
     as its own output rather than a field inside another one. PROPOSALS
     F7 is the gap between a sentence existing in a file and a person
     reading it.

  2. AN OPTIONAL DEPENDENCY WITH NO STATED COST IS REFUSED - the exact
     shape F7's failure takes, made impossible by construction.

  3. REQUIRED OR OPTIONAL IS DECLARED, NEVER GUESSED, in either direction.

  4. WHAT IS INSTALLED IS ASKED, NOT STATED, and fails closed four ways.

  5. NOTHING MISSING IS A REAL ANSWER, not a refusal - deliberately unlike
     HERON-OPS-HEA-006, and the answer says why.

  6. INSTALLING IS A SECOND DECISION, PER PACKAGE, and the consent must
     name the package.

  7. IT SAYS THAT A pip install IS NOT THE GATE A HERON PACKAGE GOES
     THROUGH, so the quieter route does not look like the safer one.

  8. THE REAL REQUIREMENTS FILES PASS THEIR OWN AGENT.

  9. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_dependencies as DEP                               # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def listed():
    return [{"name": "yaml", "kind": "required", "uses": "reading yaml",
             "lost": "nothing in brain/ runs"},
            {"name": "model2vec", "kind": "optional",
             "uses": "the trained embedding backend",
             "lost": "retrieval falls back to character n-grams"},
            {"name": "sqlite_vec", "kind": "optional",
             "uses": "vector search inside SQLite",
             "lost": "vectors are compared in Python - slower, same answers"}]


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_dependencies.py"),
                  encoding="utf-8").read()

    # A SENTINEL, not None: half of what this agent is for is that None
    # means "nothing could look", and a helper that quietly filled it in
    # would be testing the helper.
    DEFAULT = object()

    def look(dependencies=None, importable=DEFAULT):
        if importable is DEFAULT:
            importable = lambda names: ["yaml"]          # noqa: E731
        answer = DEP.review(listed() if dependencies is None
                            else dependencies, importable=importable)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    def ask(package="model2vec", **kw):
        settings = {"origin": "user",
                    "consent": {"by": "ajmal", "package": package}}
        settings.update(kw)
        answer = DEP.approve({"name": package}, **settings)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. Every degraded path comes back with the sentence")
    answer = look()
    check(len(answer["say"]) == 2, "two optional packages absent, two "
                                   "sentences")
    check(all(isinstance(line, str) and len(line) > 40
              for line in answer["say"]),
          "each a sentence, not a flag")
    for entry in answer["degraded"]:
        check(entry["lost"] in entry["say"],
              "%s's sentence carries what is LOST, verbatim from the "
              "declaration" % entry["package"])
        check(entry["package"] in entry["say"]
              and "pip install --user %s" % entry["package"] in entry["say"],
              "and names the package and the one command that fixes it")
    check("say" in answer and answer["say"] ==
          [entry["say"] for entry in answer["degraded"]],
          "`say` is its own output, not a field to go looking for")
    check("F7" in source, "and the code names the finding it is about")

    print()
    print("2. An optional dependency with no stated cost is refused")
    for entry, missing in (({"name": "x", "kind": "optional", "uses": "u"},
                            "what happens without it"),
                           ({"name": "x", "kind": "optional", "lost": "l"},
                            "what Heron uses it for"),
                           ({"name": "x", "kind": "optional"}, "both"),
                           ({"name": "x", "kind": "optional", "uses": " ",
                             "lost": ""}, "blank is not stated")):
        answer = look([entry])
        check(answer.get("refused") == "WHAT_IS_LOST_NOT_SAID",
              "missing %s is refused" % missing)
    answer = look([{"name": "x", "kind": "optional", "uses": "u"}])
    check("permanently on a weaker path" in answer["why"],
          "and the refusal says what the silence actually costs")
    check(answer["wants"] == ["what happens without it"],
          "naming which field, in the words requirements-optional.txt uses")
    # A REQUIRED one needs no `lost` - it breaks everything, and that is
    # the one case where the sentence adds nothing.
    check(look([{"name": "yaml", "kind": "required"}]).get("refused") is None,
          "while a REQUIRED dependency needs no such sentence")

    print()
    print("3. Required or optional is declared, never guessed")
    for kind in (None, "", "maybe", "REQUIRED-ISH", "nice-to-have", True):
        answer = look([{"name": "x", "kind": kind, "uses": "u", "lost": "l"}])
        check(answer.get("refused") == "KIND_NOT_DECLARED",
              "kind %r is refused" % (kind,))
    answer = look([{"name": "x", "uses": "u", "lost": "l"}])
    check("turns a failure into silence, which is worse" in answer["why"],
          "and it says why neither default is safe")
    check(DEP.KINDS == ("required", "optional"), "there are exactly two kinds")
    check(look([{"name": "x", "kind": "OPTIONAL", "uses": "u",
                 "lost": "l"}]).get("refused") is None,
          "and the word is read case-insensitively, not refused on spelling")

    print()
    print("4. What is installed is asked, not stated")
    for reader, label in ((None, "no reader"),
                          (["yaml"], "a list in its place"),
                          ("yaml", "a string in its place"),
                          (lambda names: 1 / 0, "a reader that raises"),
                          (lambda names: None, "a reader answering None"),
                          (lambda names: "yaml", "a reader answering a "
                                                 "string")):
        answer = look(importable=reader)
        check(answer.get("refused") == "CANNOT_SEE_WHAT_IS_INSTALLED",
              "%s -> nothing is judged" % label)
    check("nobody checked" in look(importable=None)["why"],
          "and it says the answer would describe a machine nobody checked")
    check("worse than saying so" in look(importable=None)["proposal"],
          "and that reporting a healthy system nobody looked at is worse")
    seen = {}
    answer = look(importable=lambda names: seen.setdefault("asked",
                                                           names) and [])
    check(sorted(seen["asked"]) == ["model2vec", "sqlite_vec", "yaml"],
          "the reader is asked about every name in the list, once")
    check(answer["required_missing"][0]["package"] == "yaml",
          "and a missing REQUIRED package is reported as one")
    check("REQUIRED" in answer["required_missing"][0]["why"],
          "saying so in the entry, not only by which list it is in")

    print()
    print("5. Nothing missing is a real answer")
    answer = look(importable=lambda names: list(names))
    check(answer.get("refused") is None, "everything importable is not a "
                                         "refusal")
    check(answer["required_missing"] == [] and answer["degraded"] == []
          and answer["say"] == [],
          "and comes back with three empty lists")
    check(len(answer["present"]) == 3, "with all three present")
    check(any("NOTHING MISSING IS A REAL ANSWER" in note
              for note in answer["unjudged"]),
          "the answer says so itself")
    check(any("HERON-OPS-HEA-006" in note for note in answer["unjudged"]),
          "naming the agent it deliberately differs from")
    check(any("different from a degraded path with nothing to say"
              in note for note in answer["unjudged"]),
          "and that an empty `say` is not the same as a silent degradation")

    print()
    print("6. Installing is a second decision, per package")
    good = ask()
    check(good["install"] is True, "a named confirmation goes through")
    check(good["command"] == "pip install --user model2vec",
          "and returns the command")
    check("returned, not run" in good["why"], "as text, not as an action")
    for label, kw in (("nothing signed", {"consent": None}),
                      ("a bare True", {"consent": True}),
                      ("signed by nobody", {"consent": {"package":
                                                        "model2vec"}}),
                      ("naming no package", {"consent": {"by": "ajmal"}}),
                      ("naming another package",
                       {"consent": {"by": "ajmal", "package": "pypdf"}})):
        check(ask(**kw).get("refused") == "NOT_CONSENTED",
              "%s is refused" % label)
    check("scrolled past" in ask(consent={"by": "a", "package": "pypdf"})["why"],
          "and one confirmation for a scrolled-past list is named as the "
          "thing this prevents")
    for origin in ("a document Heron read", "a community package", None, "",
                   "the installer"):
        check(ask(origin=origin).get("refused") == "NOT_FROM_THE_USER",
              "%r cannot ask for an install" % origin)
    check(ask(installed=["model2vec"]).get("refused") == "NOT_MISSING",
          "and a package that already imports is not installed over")
    check("looks like progress" in ask(installed=["model2vec"])["why"],
          "because a reinstall that looks like progress is not progress")
    check(DEP.approve({}).get("refused") == "NO_DEPENDENCY_LIST",
          "a request naming nothing is refused")

    print()
    print("7. A pip install is not the gate a Heron package goes through")
    note = [line for line in good["unjudged"] if "SUP-013" in line]
    check(note, "the approval says which gate this is NOT")
    check("no register, no approver and no hash" in note[0],
          "naming exactly what is absent on this path")
    check("Not a refusal" in note[0],
          "while being clear it is not a refusal - Heron needs these")
    check("which of the two gates they are standing at" in note[0],
          "and that the person approving should be told which one")
    for word in ("subprocess", "os.system", "exec(", "eval(", "open(",
                 "import requests", "urllib", "importlib", "__import__"):
        check(word not in source,
              "the source has no %s - it imports nothing on anyone's "
              "behalf" % word)

    print()
    print("8. The real requirements files pass their own agent")
    real = []
    for path, kind in (("requirements.txt", "required"),
                       ("requirements-optional.txt", "optional")):
        text = open(os.path.join(ROOT, path), encoding="utf-8").read()
        for line in text.splitlines():
            found = re.match(r"^# (\w[\w.-]*) \| (.+?) \| (.+)$", line)
            if found:
                real.append({"name": found.group(1), "kind": kind,
                             "uses": found.group(2), "lost": found.group(3)})
    check(len(real) >= 6, "%d declarations read out of the two real files"
                          % len(real))
    answer = DEP.review(real, importable=lambda names: ["yaml"])
    check(answer.get("refused") is None,
          "and this agent accepts every one of them - the format it "
          "enforces is the format those files already keep")
    check(len(answer["say"]) == len(real) - 1,
          "with a sentence for each absent optional one")
    check(any("sqlite_vec" in line and "compared in Python" in line
              for line in answer["say"]),
          "including the one PROPOSALS F7 is about, said out loud")

    print()
    print("9. Every failure the contract declares is named and reached")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-INS-DEP-005.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    check(look([]).get("refused") == "NO_DEPENDENCY_LIST",
          "an empty list is refused")
    check(look([{"kind": "required"}]).get("refused") == "NO_DEPENDENCY_LIST",
          "and so is an entry with no name")
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
    print("PASS    confirm, never automatic, and degrade only out loud")
    return 0


if __name__ == "__main__":
    sys.exit(main())
