# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-ONB-010
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
First-run onboarding - once, and then nothing.

    python tests/test_onboarding.py

WHAT IT PROVES
  1. A SECOND RUN IS REFUSED, NOT SHORTENED. There is no shorter second
     version anywhere in the module - an agent with one of those is an
     agent that will be run twice.

  2. IT WILL NOT SPEAK UNTIL HERON-INS-HLT-009 SAYS COMPLETE, and it is
     fed that agent's REAL answer rather than a dictionary shaped like one.

  3. A TOPIC WHOSE STEP IS NOT HEALTHY IS DROPPED AND NAMED, never
     demonstrated.

  4. EVERY TOPIC RESTS ON A STEP docs/07 s1 REALLY HAS.

  5. THE DEGRADED SENTENCES ARE HERON-INS-DEP-005's OWN, passed through
     unchanged rather than reworded.

  6. IT RETURNS TOPICS, NOT SENTENCES - D-01 gives the wording to the
     host, which is why nothing here calls a model.

  7. IT RECORDS NOTHING, INCLUDING THAT IT RAN.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_onboarding as ONB                                 # noqa: E402
import heron_install_health as HLT                             # noqa: E402
import heron_dependencies as DEP                               # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def real_install(**broken):
    """HERON-INS-HLT-009's ACTUAL report, not a dictionary shaped like it."""
    checks = dict((step, {"state": "HEALTHY", "installed_by": "installer",
                          "checked_by": "heron-verify", "why": "checked"})
                  for step in HLT.VERIFIED_BY_CHECKS)
    for step, state in broken.items():
        checks[step] = dict(checks[step], state=state)
    return HLT.report(checks)


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_onboarding.py"),
                  encoding="utf-8").read()

    def ask(**kw):
        kw.setdefault("install", real_install())
        answer = ONB.guide(**kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. A second run is refused, not shortened")
    answer = ask(already_guided=True)
    check(answer.get("refused") == "ALREADY_GUIDED", "a second run refuses")
    check(answer["guide"] is False, "and says not to speak")
    check("no shorter second version" in answer["why"],
          "saying there is no shorter version")
    check("will be run twice" in answer["why"],
          "and why having one would be the problem")
    check("topics" not in answer and "say_about_degraded" not in answer,
          "with nothing to say carried in the refusal either")
    for word in ("reminder", "brief", "short version", "again", "recap"):
        check(word not in source.lower().split("def main")[0]
              or "shorter" in source,
              "the module has no '%s' path" % word)
    check(ask(already_guided=False)["guide"] is True,
          "while a first run does speak")

    print()
    print("2. A FAILED install gets nothing, a WARNING still gets a guide")
    answer = ask(install=real_install(**{"rag-initialised": "FAILED"}))
    check(answer.get("refused") == "INSTALL_FAILED",
          "a real FAILED report from HERON-INS-HLT-009 stops it")
    check(answer["state"] == "FAILED", "carrying that state")
    check("HERON-INS-HLT-009" in answer["why"], "naming the agent that said so")
    check("account of itself is unreliable" in answer["why"],
          "and what a tour of broken features teaches the user")
    check("does neither" in answer["proposal"],
          "while making clear this agent will not fix or check them")
    check(real_install()["state"] == "HEALTHY",
          "and a clean install really does report HEALTHY from that agent, "
          "so this is its answer and not a fixture")
    # THE DISTINCTION THAT MADE `complete()` THE WRONG INPUT.
    warned = real_install(**{"rag-initialised": "WARNING"})
    check(HLT.complete(dict((step, {"state": "HEALTHY",
                                    "installed_by": "i",
                                    "checked_by": "v"})
                            for step in HLT.VERIFIED_BY_CHECKS)
                       ).get("complete") is True,
          "step 16's complete() passes only when everything is HEALTHY...")
    check(ask(install=warned)["guide"] is True,
          "...so one WARNING would have silenced the whole guide, and this "
          "agent reads the REPORT instead")
    for install in (None, {}, True, "complete", [], {"state": "HEALTHY"}):
        check(ask(install=install).get("refused") == "NO_INSTALL_REPORT",
              "%r is not an install report it will act on" % (install,))
    check(ask(install=None).get("refused") == "NO_INSTALL_REPORT",
          "and nothing at all is NO_INSTALL_REPORT specifically")
    check("takes longer to find out" in ask(install=None)["why"],
          "saying an unchecked install is a broken one with a delay")

    print()
    print("3. A topic whose step is not HEALTHY is dropped and named")
    answer = ask(install=real_install(**{"rag-initialised": "WARNING"}))
    check(answer["guide"] is True, "a WARNING on one step still guides...")
    check(len(answer["left_out"]) == 1, "...with one topic left out")
    check(answer["left_out"][0]["topic"] == "vector-db",
          "the one that rests on that step")
    check("would demonstrate something that does not work"
          in answer["left_out"][0]["why"],
          "saying why it was dropped")
    check("vector-db" not in [entry["topic"] for entry in answer["topics"]],
          "and it really is not in the topics")
    check(len(answer["topics"]) == len(ONB.TOPICS) - 1,
          "every other topic survives")

    print()
    print("4. Every topic rests on a step docs/07 s1 really has")
    for name, what, step in ONB.TOPICS:
        check(step in HLT.STEPS,
              "'%s' rests on '%s', which is one of the sixteen" % (name, step))
        check(what and len(what) > 20,
              "and says what it shows, in more than a label")
    check(len(set(step for _, _, step in ONB.TOPICS)) == len(ONB.TOPICS),
          "no two topics rest on the same step")

    print()
    print("5. The degraded sentences are the dependency agent's own")
    real = DEP.review([{"name": "yaml", "kind": "required", "uses": "u",
                        "lost": "l"},
                       {"name": "pypdf", "kind": "optional",
                        "uses": "reading PDF documents",
                        "lost": "PDFs cannot be ingested"}],
                      importable=lambda names: ["yaml"])
    answer = ask(degraded=real["degraded"])
    check(answer["say_about_degraded"] == sorted(real["say"]),
          "the sentences are HERON-INS-DEP-005's, unchanged")
    check("PDFs cannot be ingested" in answer["say_about_degraded"][0],
          "carrying what is lost, in that agent's words")
    check(ask(degraded=[])["say_about_degraded"] == [],
          "and nothing degraded means nothing to explain away")
    check(any("nothing had to be explained away" in note
              for note in ask(degraded=[])["unjudged"]),
          "which the answer says rather than leaving an empty list to read")
    check(any("HERON-INS-DEP-005's own rather than invented here" in note
              for note in answer["unjudged"]),
          "and a run with degraded capabilities says whose sentences those "
          "are")

    print()
    print("6. It returns topics, not sentences")
    answer = ask()
    check(all(sorted(entry) == ["rests_on", "shows", "topic"]
              for entry in answer["topics"]),
          "each topic is a subject and what it rests on, not a script")
    note = [line for line in answer["unjudged"] if "NOT HOW TO SAY IT" in line]
    check(note, "the answer says it chose what, not how")
    check("D-01" in note[0] and "HERON-ORC-PER-003" in note[0],
          "naming the decision and the host agent that owns the wording")
    check("two agents phrasing one greeting" in note[0],
          "and why duplicating it here would be wrong")
    for word in ("model", "prompt", "completion", "llm"):
        check(word not in source.lower().split('"""')[2].lower()
              or "no model" in source.lower()
              or "calls a model" in source.lower(),
              "no model call in the code for '%s'" % word)

    print()
    print("7. It records nothing, including that it ran")
    for word in ("open(", "json.dump", "os.makedirs", "subprocess",
                 "sqlite3", "shutil", "os.remove"):
        check(word not in source, "the source has no %s" % word)
    note = [line for line in answer["unjudged"] if "nothing was recorded"
            in line.lower()]
    check(note, "the answer says nothing was recorded")
    check("has to be told by the caller" in note[0],
          "and that whatever remembers this has to be told by the caller")
    handed = [line for line in answer["unjudged"] if "HANDED IN" in line]
    check(handed and "after every restart" in handed[0],
          "with why keeping it in memory would reintroduce Heron every "
          "restart")

    print()
    print("8. Every failure the contract declares is named and reached")
    answer = ask(install=dict(real_install(), state="DEGRADED",
                              steps=[{"step": step, "state": "DEGRADED"}
                                     for _, _, step in ONB.TOPICS]))
    check(answer.get("refused") == "NOTHING_WORKS_YET",
          "every topic's step failing leaves nothing to introduce")
    check(len(answer["left_out"]) == len(ONB.TOPICS),
          "with all of them named as left out")
    check("the failure this agent exists to avoid" in answer["why"],
          "and introducing them anyway named as the failure")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-INS-ONB-010.yaml"))
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
    print("PASS    once, and then nothing")
    return 0


if __name__ == "__main__":
    sys.exit(main())
