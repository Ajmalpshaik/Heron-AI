# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-BRN-007
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Brain initialisation - the one install step that can destroy what it finds.

    python tests/test_brain_init.py

WHAT IT PROVES
  1. AN EXISTING STORE IS NEVER RE-INITIALISED. The dangerous case is a
     SUCCESSFUL initialisation on a machine that already had stores, and
     it is named and left rather than rebuilt.

  2. THE SCOPES, PATHS, MEANINGS AND SCHEMA ARE heron_scope's OWN, not a
     second copy that would drift.

  3. ONE STORE PER SCOPE, PHYSICALLY - two scopes that would share a file
     are refused BEFORE anything reaches disk, including two project keys
     that sanitise to the same name.

  4. THE PROJECT SCOPE NEEDS A PROJECT AND NOTHING DEFAULTS IT, and a
     project key on a scope that has no projects is refused too.

  5. IT IS ADMIN, FROM THE USER.

  6. IT CREATES NOTHING, and is honest that `existing` was handed in
     rather than looked up.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

# A knowledge directory of this test's own, so nothing here depends on the
# machine and nothing writes outside it. Set BEFORE heron_scope is imported.
SCRATCH = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                       "..", ".test-knowledge")
os.environ["HERON_KNOWLEDGE"] = os.path.abspath(SCRATCH)

import heron_brain_init as BRN                                 # noqa: E402
import heron_scope as SCOPE                                    # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

SIGNED = {"by": "ajmal", "at": "2026-09-14T12:00Z"}
WANTED = ["global", "company", "user",
          {"scope": "project", "project": "Tower-B"}]


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_brain_init.py"),
                  encoding="utf-8").read()
    DEFAULT = object()

    def ask(wanted=DEFAULT, **kw):
        kw.setdefault("existing", [])
        kw.setdefault("origin", "user")
        kw.setdefault("approval", dict(SIGNED))
        answer = BRN.plan(WANTED if wanted is DEFAULT else wanted, **kw)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. An existing store is never re-initialised")
    fresh = ask()
    check(len(fresh["create"]) == 4 and fresh["keep"] == [],
          "on an empty machine, four stores to create")
    already = [entry["path"] for entry in fresh["create"][:2]]
    answer = ask(existing=already)
    check(len(answer["create"]) == 2 and len(answer["keep"]) == 2,
          "with two already there, two to create and two left alone")
    check([entry["path"] for entry in answer["keep"]] == already,
          "and the two left are exactly the two that existed")
    check("left alone" in answer["keep"][0]["why"],
          "each saying it was left")
    check("looks exactly like a fresh install" in answer["keep"][0]["why"],
          "and why an empty knowledge base is the dangerous outcome")
    check("finds out weeks later" in answer["keep"][0]["why"],
          "and when the person finds out")
    answer = ask(existing=[entry["path"] for entry in fresh["create"]])
    check(answer["create"] == [] and len(answer["keep"]) == 4,
          "a fully-installed machine creates nothing at all")
    check(answer.get("refused") is None,
          "and that is not a refusal - nothing to do is the right answer "
          "here, unlike an empty request")

    print()
    print("2. The scopes and paths are heron_scope's own")
    check("SCOPE.SCOPES" in source and "SCOPE.SCOPE_MEANING" in source,
          "the scope list and meanings are imported")
    check("SCOPE.scope_path" in source, "and so are the paths")
    check("SCOPE.SCHEMA_VERSION" in source, "and the schema version")
    for entry in fresh["create"]:
        check(entry["meaning"] == SCOPE.SCOPE_MEANING[entry["scope"]],
              "%s's meaning is heron_scope's own words" % entry["scope"])
        check(entry["path"] == SCOPE.scope_path(entry["scope"],
                                                entry["project"]),
              "and its path is heron_scope's own answer")
        check(entry["schema"] == SCOPE.SCHEMA_VERSION,
              "and the schema version is too")
    for name in ("knowledge", "GLOBAL", "", "Project", "temp"):
        if name.lower() in SCOPE.SCOPES:
            continue
        check(ask([name]).get("refused") == "NOT_A_SCOPE",
              "'%s' is not a scope" % name)
    check("a second copy to disagree with" in ask(["knowledge"])["why"],
          "and the refusal says why the list is not restated here")

    print()
    print("3. One store per scope, physically")
    answer = ask([{"scope": "project", "project": "Tower B"},
                  {"scope": "project", "project": "Tower-B"}])
    check(answer.get("refused") == "TWO_SCOPES_ONE_STORE",
          "two project keys that sanitise to one filename are refused")
    check(answer["path"].endswith(".db"), "naming the file they would share")
    check("Golden Rule 5" in answer["why"] and "D-23" in answer["why"],
          "citing the rule that makes separation physical")
    check("WHERE clause somebody can forget" in answer["why"],
          "and what two scopes in one file amounts to")
    check(ask(["global", "global"])["create"],
          "while the SAME scope asked for twice is not a conflict - it is "
          "one store, requested twice")
    check(len(ask(["global", "global"])["create"]) == 1,
          "and it appears once")

    print()
    print("4. The project scope needs a project")
    for wanted in (["project"], [{"scope": "project"}],
                   [{"scope": "project", "project": ""}],
                   [{"scope": "project", "project": "   "}]):
        check(ask(wanted).get("refused") == "PROJECT_KEY_MISSING",
              "%r names no project" % (wanted,))
    answer = ask(["project"])
    check("contractual breach rather than a bug" in answer["why"],
          "and says what guessing would actually be")
    check("docs/10 s2" in answer["why"], "citing where that is written")
    check("asks, once" in answer["proposal"], "with D-33's rule as the fix")
    for scope in ("global", "company", "user", "temporary"):
        answer = ask([{"scope": scope, "project": "Tower-B"}])
        check(answer.get("refused") == "PROJECT_KEY_NOT_WANTED",
              "a project key on %s is refused" % scope)
    check("expecting per-project separation from a store that has none"
          in ask([{"scope": "global", "project": "X"}])["why"],
          "and says what the caller was probably expecting")

    print()
    print("5. It is ADMIN, from the user")
    for origin in ("a document Heron read", "a community package", None, ""):
        check(ask(origin=origin).get("refused") == "NOT_FROM_THE_USER",
              "%r cannot initialise knowledge stores" % origin)
    check("nothing regenerates" in ask(origin=None)["proposal"],
          "and the refusal says why this step is different")
    for approval in (None, {}, True, {"at": "T"}, "ajmal"):
        check(ask(approval=approval).get("refused") == "NOT_APPROVED",
              "%r is not an approval" % (approval,))
    check("SUCCESSFUL one on a machine that already had stores"
          in ask(approval=None)["why"],
          "naming what the signature is actually against")
    for empty in ([], None, ""):
        check(ask(empty).get("refused") == "NOTHING_TO_INITIALISE",
              "%r asks for nothing" % (empty,))

    print()
    print("6. It creates nothing")
    check(fresh["created"] is False, "`created` is False even on a good plan")
    for word in ("sqlite3", "os.makedirs", "open(", "os.remove", "shutil",
                 "subprocess", "SCOPE.open_scope(", "SCOPE.rebuild("):
        check(word not in source, "the source has no %s" % word)
    check(not os.path.exists(os.environ["HERON_KNOWLEDGE"]),
          "and no knowledge directory was created by any of this")
    check(any("NOTHING WAS CREATED" in note for note in fresh["unjudged"]),
          "the answer says nothing was created")
    handed = [note for note in fresh["unjudged"] if "HANDED IN" in note]
    check(handed, "and that `existing` was handed in")
    check("stale list" in handed[0],
          "with what a caller passing a stale list actually gets")
    check("shape of the mistake it exists to prevent" in handed[0],
          "and why it does not look for itself")

    print()
    print("7. Every failure the contract declares is named and reached")
    # NOWHERE_TO_PUT_IT - no knowledge directory at all.
    keep = os.environ.pop("HERON_KNOWLEDGE")
    appdata = os.environ.pop("APPDATA", None)
    answer = ask(["global"])
    os.environ["HERON_KNOWLEDGE"] = keep
    if appdata is not None:
        os.environ["APPDATA"] = appdata
    check(answer.get("refused") == "NOWHERE_TO_PUT_IT",
          "with nowhere to keep knowledge, it refuses rather than picking "
          "a folder")
    check("nowhere to keep knowledge" in answer["why"],
          "in heron_scope's own words, passed through rather than reworded")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-INS-BRN-007.yaml"))
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
    print("PASS    an existing store is named and left, never rebuilt")
    return 0


if __name__ == "__main__":
    sys.exit(main())
