# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-SUP-013
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Supply Chain Security - unapproved is refused, and there is no warning.

    python tests/test_supply.py

WHAT IT PROVES
  1. THERE IS NO WARNING ANYWHERE. Two outcomes and a list of refusals -
     no third state where it installs and somebody was told. D-35: a
     warning is a decision handed to somebody who has no way to judge it
     and every reason to click through.

  2. NOTHING A PACKAGE SAYS ABOUT ITSELF COUNTS. Its trust level, its
     approver and its hash are each checked against something handed in
     from outside it, and a package claiming MORE than the register grants
     is refused by name.

  3. IT FAILS CLOSED ON EVERY MISSING INPUT. No register, no approver
     list, no measured hash - each is a refusal, not a pass.

  4. APPROVAL AND PROOF ARE THE SAME GATE, and a submission with no
     NEGATIVE case is RETURNED rather than reviewed.

  5. NOBODY APPROVES THEIR OWN WORK - Golden Rule 7, at the human scale.

  6. A DEPENDENCY IS CODE TOO. A package is exactly as trusted as the
     least trusted thing it pulls in.

  7. GOLDEN RULE 18 APPLIES TO EVERYONE. `first_run` is SANDBOX and there
     is no value that is not.

  8. THE HOLE IT WAS BUILT AROUND IS REALLY THERE: every fragment.yaml in
     this repository claims the top of the trust ladder, and nothing reads
     it.

  9. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import glob
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_supply as SUP                                     # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

HASH = "a1b2c3d4e5f6"
PROOF = {"at": "2026-09-10", "positive": "sized 41 ducts in a real model",
         "negative": "refused a duct with no system classification"}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def register(**more):
    found = {"mep-sizing-pack": {"trust": "VERIFIED", "hash": HASH,
                                 "approval": {"by": "ajmal",
                                              "at": "2026-09-10",
                                              "proof": dict(PROOF)}},
             "geometry-helpers": {"trust": "OFFICIAL", "hash": "ffee",
                                  "approval": {"by": "ajmal",
                                               "proof": dict(PROOF)}}}
    found.update(more)
    return found


def package(**more):
    found = {"id": "mep-sizing-pack", "version": "1.2.0",
             "source": "VERIFIED", "author": "someone-else",
             "depends": ["geometry-helpers"]}
    found.update(more)
    return found


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_supply.py"),
                  encoding="utf-8").read()

    def ask(what=None, **kw):
        settings = {"register": register(), "approvers": ["ajmal"],
                    "measured": HASH}
        settings.update(kw)
        answer = SUP.judge(package() if what is None else what, **settings)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. There is no warning anywhere")
    good = ask()
    check(good["install"] is True, "a package meeting every check installs")
    for outcome in ("warn", "warning", "caution", "confirm", "proceed_anyway",
                    "override"):
        check(outcome not in [key.lower() for key in good],
              "no '%s' key in the answer" % outcome)
    # NOT a word search: "warning" appears in this module, and the claim is
    # that every mention refuses one.
    import re
    flat = " ".join(source.split())
    mentions = re.findall(r".{0,80}\bwarn\w*\b.{0,80}", flat)
    check(mentions, "the source does mention a warning, to rule it out")
    ruling = [m for m in mentions
              if any(word in m.lower() for word in
                     ("no warning", "not a warning", "refuses", "refusal",
                      "click through", "never"))]
    check(len(ruling) == len(mentions),
          "and all %d mentions rule one out (%s)"
          % (len(mentions),
             "; ".join(sorted(set(m.strip() for m in mentions
                                  if m not in ruling))) or "none left"))
    check("D-35" in source and "click through" in source,
          "citing the decision and the reason")
    for label, answer in (("no identity", ask({})),
                          ("no register", ask(register=None)),
                          ("a modified package", ask(measured="0000")),
                          ("an unknown dependency",
                           ask(package(depends=["nobody-knows"])))):
        check(answer["install"] is False and answer.get("refused"),
              "%s answers install=False with a named refusal, never a "
              "middle state" % label)

    print()
    print("2. Nothing a package says about itself counts")
    answer = ask(package(source="OFFICIAL"))
    check(answer.get("refused") == "SELF_DECLARED_TRUST",
          "claiming OFFICIAL when the register says VERIFIED is refused")
    check("Golden Rule 19" in answer["why"],
          "citing the rule that makes its claim data")
    check("the register is what changes" in answer["proposal"],
          "and says where a real promotion happens")
    check(ask(package(source="UNKNOWN"))["install"] is True,
          "while claiming LESS than the register grants is not a problem - "
          "the register is the authority in both directions")
    answer = ask(package(source="TOTALLY_SAFE"))
    check(answer["install"] is True,
          "and a word outside the ladder cannot rank above it either")
    check(any("not on docs/00d s38's ladder" in note
              for note in answer["unjudged"]),
          "but it is RECORDED rather than dropped - Golden Rule 14, and a "
          "package inventing its own trust word is worth reading")
    check(not any("ladder at all" in note for note in good["unjudged"]),
          "while a package claiming a real level says nothing extra")
    answer = ask(register={"mep-sizing-pack": {"trust": "ABSOLUTE",
                                               "hash": HASH}})
    check(answer.get("refused") == "NOT_IN_THE_TRUSTED_REGISTER",
          "a register entry with an unreadable trust level is not an entry")
    check(SUP.TRUST[0] == "UNKNOWN" and SUP.TRUST[-1] == "OFFICIAL",
          "and the ladder is docs/00d s38's, lowest first")

    print()
    print("3. It fails closed on every missing input")
    for label, kw, refusal in (
            ("no register at all", {"register": None}, "NO_REGISTER"),
            ("a register that is a list", {"register": []}, "NO_REGISTER"),
            ("no approver list", {"approvers": []}, "SELF_APPROVED"),
            ("no measured hash", {"measured": None}, "NOT_MEASURED"),
            ("an empty measured hash", {"measured": ""}, "NOT_MEASURED")):
        answer = ask(**kw)
        check(answer.get("refused") == refusal,
              "%s -> %s" % (label, refusal))
    answer = ask(register={"mep-sizing-pack": {"trust": "VERIFIED"}})
    check(answer.get("refused") == "NOT_MEASURED",
          "a register with no hash for it is refused too")
    check("look identical from here" in answer["why"],
          "saying 'nobody checked' and 'unmodified' are indistinguishable")
    check("installs nothing" in ask(register=None)["proposal"],
          "and that installing nothing is the correct default")
    answer = ask(measured="deadbeef0000")
    check(answer.get("refused") == "MODIFIED",
          "a hash that does not match is refused")
    check("a different package than the one on disk" in answer["why"],
          "saying the approval on file is for something else")
    check("worse outcome than a refusal" in answer["proposal"],
          "and that retrying until it matches is worse than stopping")
    for label, what in (("no id", {}), ("id only", {"id": "x"}),
                        ("version only", {"version": "1"}),
                        ("not a record", "a package")):
        check(ask(what).get("refused") == "NO_IDENTITY",
              "%s is no identity" % label)

    print()
    print("4. Approval and proof are the same gate")
    answer = ask(register=register(**{"mep-sizing-pack": {
        "trust": "VERIFIED", "hash": HASH}}))
    check(answer.get("refused") == "NO_APPROVAL_RECORD",
          "no approval record means it does not run")
    for field, _ in SUP.A_PROOF_CARRIES:
        thin = dict(PROOF)
        thin.pop(field)
        answer = ask(register=register(**{"mep-sizing-pack": {
            "trust": "VERIFIED", "hash": HASH,
            "approval": {"by": "ajmal", "proof": thin}}}))
        check(answer.get("refused") == "RETURNED_NO_NEGATIVE_CASE",
              "a proof with no '%s' is returned" % field)
        check(answer.get("returned") is True,
              "returned to the submitter, not queued for review")
    answer = ask(register=register(**{"mep-sizing-pack": {
        "trust": "VERIFIED", "hash": HASH,
        "approval": {"by": "ajmal", "proof": {"at": "x", "positive": "y"}}}}))
    check(any("correctly REFUSED" in want for want in answer["missing"]),
          "and the negative case is described as the one that gets left out")
    check("finite, and refusable" in answer["why"],
          "with why returning rather than reviewing keeps the gate alive")
    answer = ask(register=register(**{"mep-sizing-pack": {
        "trust": "VERIFIED", "hash": HASH,
        "approval": {"by": "ajmal", "proof": "it works"}}}))
    check(answer.get("refused") == "RETURNED_NO_NEGATIVE_CASE",
          "a proof that is a sentence is not a proof")

    print()
    print("5. Nobody approves their own work")
    check(ask(package(author="ajmal")).get("refused") == "SELF_APPROVED",
          "the author approving it is refused")
    check("Golden Rule 7" in ask(package(author="ajmal"))["why"],
          "citing the rule, at the human scale")
    check(ask(approvers=["someone-else"]).get("refused") == "SELF_APPROVED",
          "an approver not on the list is refused")
    answer = ask(register=register(**{"mep-sizing-pack": {
        "trust": "VERIFIED", "hash": HASH,
        "approval": {"by": "", "proof": dict(PROOF)}}}))
    check(answer.get("refused") == "SELF_APPROVED",
          "and an approval naming nobody is refused")
    check("decided outside the package" in ask(approvers=["x"])["why"],
          "because who may approve is decided outside")

    print()
    print("6. A dependency is code too")
    answer = ask(package(depends=["geometry-helpers", "fast-json-9000"]))
    check(answer.get("refused") == "DEPENDENCY_UNKNOWN",
          "a dependency nothing vouches for refuses the whole package")
    check("fast-json-9000" in answer["why"] and
          "geometry-helpers" not in answer["why"],
          "naming only the one that is unknown")
    check("least trusted thing it pulls in" in answer["why"],
          "with what a package's trust actually is")
    check("same gate, or the package does not" in answer["proposal"],
          "and that each dependency goes through this gate")
    check(ask(package(depends=[]))["install"] is True,
          "while a package with no dependencies is fine")

    print()
    print("7. Golden Rule 18 applies to everyone")
    check(good["first_run"] == "SANDBOX", "first_run is SANDBOX")
    check(source.count('"first_run": ') == 1,
          "and there is exactly one place it is ASSIGNED - no value that "
          "is not SANDBOX")
    check(any("whoever wrote it" in note for note in good["unjudged"]),
          "the answer says it applies whoever wrote it")
    check(any("NOBODY HERE READ THE CODE" in note
              for note in good["unjudged"]),
          "and that nobody read the code")
    check(any("no stronger claim than that" in note
              for note in good["unjudged"]),
          "and how weak 'unmodified' actually is")
    for word in ("subprocess", "os.system", "exec(", "eval(", "open(",
                 "import requests", "urllib", "hashlib"):
        check(word not in source,
              "the source has no %s - it never sees the bytes" % word)

    print()
    print("8. The hole it was built around is really there")
    manifests = glob.glob(os.path.join(ROOT, "brain", "fragments", "*",
                                       "fragment.yaml"))
    check(len(manifests) > 300, "there are %d fragment manifests" % len(manifests))
    claiming = [path for path in manifests
                if "source: OFFICIAL" in open(path, encoding="utf-8").read()]
    check(len(claiming) == len(manifests),
          "and all %d claim source: OFFICIAL, the top of docs/00d s38's "
          "ladder" % len(claiming))
    reader = open(os.path.join(ROOT, "brain", "heron_fragment.py"),
                  encoding="utf-8").read()
    # PROVEN is in both ladders - docs/24's lifecycle and docs/00d s38's
    # trust levels - so it proves nothing here either way. The five words
    # that belong ONLY to the trust ladder are the test.
    only_trust = [level for level in SUP.TRUST if level != "PROVEN"]
    for level in only_trust:
        check(level not in reader,
              "the fragment loader never mentions %s..." % level)
    check("PROVEN" in reader and "STATUSES = (" in reader,
          "...and the one word it does share, PROVEN, is there as a "
          "docs/24 lifecycle STATUS, which is a different ladder")
    check("SOURCES = (\"fragment\", \"ambient\", \"request\")" in reader,
          "because the `source` it validates is a different field entirely "
          "- contract.needs[].source, not the trust level")

    print()
    print("9. Every failure the contract declares is named and reached")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-INS-SUP-013.yaml"))
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
    print("PASS    unapproved is refused, and there is no warning")
    return 0


if __name__ == "__main__":
    sys.exit(main())
