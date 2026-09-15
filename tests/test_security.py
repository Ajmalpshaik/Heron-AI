# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DEV-SEC-009
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Security review - the requirement is real, and a review of a previous
version is not a review.

    python tests/test_security.py

WHAT IT PROVES
  1. THE LADDER IS HERON-KRN-CAP-005's, and MODIFY's place on it is
     LOOKED UP - the ladder is lengthened under the agent's feet and the
     answer moves with it.

  2. BELOW THE LINE IS NOT A PASS, and the answer says so in words.

  3. A REVIEW IS A SIGNATURE AND A FINGERPRINT. Missing either is not a
     review.

  4. NO AUTHOR REVIEWS THEIR OWN CHANGE (Golden Rule 7).

  5. THE FINGERPRINT IS RECOMPUTED. One space added afterwards makes the
     review stale, and stale is refused (D-35).

  6. A CREDENTIAL IS FOUND BY ITS VALUE, NESTED, THROUGH THE OWNER'S OWN
     METHOD - and the value never appears in the answer.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import ast
import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_security as SEC                                   # noqa: E402
import heron_capability as CAP                                 # noqa: E402
import heron_secrets as SECRETS                                # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

CHANGE = {"name": "move_elements", "risk": "MODIFY", "by": "Ajmal",
          "allowed-tools": ["revit_apply_move"]}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_security.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]

    print("\n1. the ladder is the owner's, and the position is looked up")
    check(SEC.RISK is CAP.RISK_ORDER,
          "SEC.RISK IS CAP.RISK_ORDER - the same object, not a fifth copy")
    at = list(CAP.RISK_ORDER).index(SEC.FROM_RISK)
    check(SEC.required_at() == tuple(CAP.RISK_ORDER[at:]),
          "required_at() is every rung from %s up: %s"
          % (SEC.FROM_RISK, ", ".join(SEC.required_at())))
    # THE REAL PROOF IS THAT THE LADDER MOVES. A hard-coded index would
    # survive a word search - it does not survive an extra rung.
    longer = tuple(["SQUINT_AT"] + list(CAP.RISK_ORDER))
    was = SEC.RISK
    try:
        SEC.RISK = longer
        check(SEC.required_at() == tuple(longer[at + 1:]),
              "a rung inserted BELOW %s and required_at() still starts at "
              "%s - the position is looked up, not written as %d"
              % (SEC.FROM_RISK, SEC.FROM_RISK, at))
        moved = SEC.review(dict(CHANGE))
        check(moved["evidence"]["rung"] == at + 2
              and moved["evidence"]["of"] == len(longer),
              "and the reported rung moves with it: %d of %d"
              % (moved["evidence"]["rung"], moved["evidence"]["of"]))
    finally:
        SEC.RISK = was
    check(SEC.RISK is CAP.RISK_ORDER, "and the real ladder is put back")
    # NO RUNG IS WRITTEN DOWN AS A VALUE except the one the register
    # names. A word search would trip over the prose in `unjudged`, which
    # mentions EXECUTE and READ inside a sentence and is right to - so the
    # string constants are collected and compared whole.
    tree = ast.parse(whole)
    spelt = set(node.value for node in ast.walk(tree)
                if isinstance(node, ast.Constant)
                and isinstance(node.value, str)) & set(CAP.RISK_ORDER)
    check(spelt == {SEC.FROM_RISK},
          "the only rung spelt out in the whole module is %s, the one the "
          "register names: %s" % (SEC.FROM_RISK, ", ".join(sorted(spelt))))

    print("\n2. below the line is not a pass")
    low = SEC.review({"name": "count", "risk": "READ", "by": "Ajmal"})
    check(low["required"] is False, "READ requires no review")
    check(low.get("refused") is None, "and that is not a refusal")
    check(low["reviewed"] is None,
          "`reviewed` is None, not True - nothing was reviewed")
    check("NOT a pass" in low["why"], "and the answer says it is NOT a pass")
    check(any("NOT A PASS" in line for line in low["unjudged"]),
          "which is also the thing left unjudged")
    check(low["evidence"]["risk"] == "READ",
          "the evidence is gathered anyway - a change below the line "
          "carrying a credential is still worth seeing")

    print("\n3. a review is a signature and a fingerprint")
    sha = SEC.fingerprint(CHANGE)
    check(sorted(field for field, _ in SEC.A_REVIEW_CARRIES) == ["by", "of"],
          "a review carries `by` and `of`")
    for review, missing in (({"of": sha}, "by"), ({"by": "Reviewer"}, "of"),
                            ({"by": " ", "of": sha}, "by")):
        answer = SEC.review(CHANGE, review)
        check(answer.get("refused") == "NOT_A_REVIEW"
              and answer["missing"] == [missing],
              "a review missing %s is NOT_A_REVIEW" % missing)
    good = SEC.review(CHANGE, {"by": "Reviewer", "of": sha})
    check(good["reviewed"] is True and good["by"] == "Reviewer",
          "signed and current is accepted")
    check(good["safe"] is None,
          "`safe` is None - whether it is safe is the reviewer's answer")
    check(any("WHETHER THE CHANGE IS SAFE" in line
              for line in good["unjudged"]),
          "and that is the first thing left unjudged")

    print("\n4. no author reviews their own change")
    for who in ("Ajmal", "ajmal", "  AJMAL "):
        answer = SEC.review(CHANGE, {"by": who, "of": sha})
        check(answer.get("refused") == "REVIEWED_BY_THE_AUTHOR",
              "%r reviewing their own change is refused" % who)
    check("Golden Rule 7" in SEC.review(
              CHANGE, {"by": "Ajmal", "of": sha})["why"],
          "citing Golden Rule 7")
    unsigned = SEC.review({"name": "x", "risk": "MODIFY"},
                          {"by": "Reviewer", "of": SEC.fingerprint(
                              {"name": "x", "risk": "MODIFY"})})
    check(unsigned["reviewed"] is True,
          "a change with no author named is not refused for it - there is "
          "no name appearing twice")

    print("\n5. the fingerprint is recomputed, not trusted")
    check(SEC.fingerprint({"a": 1, "b": 2}) == SEC.fingerprint({"b": 2,
                                                               "a": 1}),
          "a field reordered is the same review")
    check(SEC.fingerprint({"a": "1"}) != SEC.fingerprint({"a": "1 "}),
          "one trailing space is a different one")
    later = dict(CHANGE, name="move_elements ")
    stale = SEC.review(later, {"by": "Reviewer", "of": sha})
    check(stale.get("refused") == "STALE_REVIEW",
          "a review of the version before that space is STALE_REVIEW")
    check(stale["isNow"] == SEC.fingerprint(later)[:16]
          and stale["reviewedOf"] == sha[:16],
          "and both shas are shown, truncated")
    check(stale["isNow"] != stale["reviewedOf"],
          "  which are different, which is the whole point")

    print("\n6. a credential is found by its value, nested")
    check(SEC.CREDENTIALS_IN is SECRETS.Secrets.refuse_secret_input,
          "SEC.CREDENTIALS_IN IS Secrets.refuse_secret_input - the owner's "
          "own answer, called rather than re-implemented")
    # THE DEFECT THIS PINS. `redact` returns (clean, found) - a TUPLE - and
    # the first version compared that to the text it was given. A tuple is
    # never equal to a string, so every non-empty field came back as a
    # credential: the demo reported five in a change carrying one.
    check(isinstance(SECRETS.Secrets().redact("ordinary text"), tuple),
          "redact() returns a TUPLE, so comparing it to the text is never "
          "equal and would call every field a credential")
    # THE MODULE NAMES redact() IN A DOCSTRING, explaining exactly this,
    # and should. So the check is that it is never CALLED - which a word
    # search cannot tell apart from the comment warning against it.
    calls = [node for node in ast.walk(ast.parse(whole))
             if isinstance(node, ast.Call)
             and isinstance(node.func, ast.Attribute)
             and node.func.attr == "redact"]
    check(not calls,
          "and the module never CALLS redact() - only the method built to "
          "answer this question")
    nested = dict(CHANGE, auth={"header": "Bearer " + SECRETS.FAKE_FORGE_TOKEN})
    found = SEC.review(nested)["evidence"]["credentialShaped"]
    check([one["field"] for one in found] == ["auth.header"],
          "exactly one field, nested two deep, is named: %s"
          % ", ".join(one["field"] for one in found))
    check(found[0]["shape"] == "github token", "with the shape that matched")
    check(SECRETS.FAKE_FORGE_TOKEN not in repr(SEC.review(nested)),
          "AND THE VALUE IS NOWHERE IN THE ANSWER")
    # VALUE, NOT KEY NAME - both directions.
    named = SEC.review({"name": "count", "risk": CAP.RISK_ORDER[0],
                        "by": "Ajmal", "token": "an-ordinary-word",
                        "password": "a-plain-phrase"})
    check(named["evidence"]["credentialShaped"] == [],
          "fields CALLED token and password holding ordinary words are not "
          "credentials - the value is what is judged")
    blurb = SEC.review(dict(CHANGE, blurb=SECRETS.FAKE_FORGE_TOKEN))
    check([one["field"] for one in blurb["evidence"]["credentialShaped"]]
          == ["blurb"],
          "and a field called `blurb` holding a token is")
    handle = SEC.review(dict(CHANGE,
                             cred=SECRETS.HANDLE_PREFIX + "github-token"))
    check(handle["evidence"]["credentialShaped"] == [],
          "a handle is not a credential - it is the thing that SHOULD "
          "travel (docs/12 s5a.3)")
    # THE STORE IS ASKED, not a pattern list written here.
    keeper = SECRETS.Secrets()
    keeper.register_for_redaction(SECRETS.HANDLE_PREFIX + "db", "hunter2")
    taught = SEC.review(dict(CHANGE, pw="hunter2"), secrets=keeper)
    check([one["field"] for one in taught["evidence"]["credentialShaped"]]
          == ["pw"],
          "a value the store was TAUGHT is caught though it matches no "
          "shape - the store is asked, not a pattern list written here")
    check(any("NONE OF THE SIX KNOWN SHAPES" in line
              for line in named["unjudged"]),
          "and an empty list is reported as `none of the six known shapes "
          "appeared`, not as `there is no credential`")
    check(any("hunter2" in line for line in named["unjudged"]),
          "  naming HERON-GIT-REP-002's edge: a password that looks like a "
          "word is not caught, and saying so beats a reassuring sentence")
    # AND NO VALUE OF THE CHANGE TRAVELS, credential-shaped or not. The
    # values above appear nowhere in the module, so this cannot pass by
    # matching the module's own prose - which is how the first version of
    # this check failed, tripping over `hunter2` in the sentence above.
    for value in ("an-ordinary-word", "a-plain-phrase"):
        check(value not in repr(named),
              "  and %r is nowhere in the answer" % value)

    print("\n7. every failure the contract declares is named and reached")
    for change, review, name in (
            (None, None, "NOTHING_TO_REVIEW"),
            ("a string", None, "NOT_A_CHANGE"),
            ({"name": "x"}, None, "NOT_A_CHANGE"),
            ({"risk": "DANGEROUS"}, None, "UNKNOWN_RISK"),
            (CHANGE, None, "NOT_REVIEWED"),
            (CHANGE, "a string", "NOT_A_REVIEW"),
            (CHANGE, {"by": "Ajmal", "of": sha}, "REVIEWED_BY_THE_AUTHOR"),
            (CHANGE, {"by": "R", "of": "x" * 64}, "STALE_REVIEW")):
        answer = SEC.review(change, review)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)
    check(SEC.review(CHANGE)["required"] is True,
          "and NOT_REVIEWED still says the review was REQUIRED")

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-DEV-SEC-009.yaml"))
    named_failures = contract.get("failures") or []
    check(len(named_failures) == 7, "the contract declares 7 failures")
    for failure in named_failures:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named_failures) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(good["unjudged"]) == 5, "five things are left unjudged")
    check(not (contract.get("allowed-tools") or []),
          "and the contract allows no tools - nothing is executed here")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the requirement is real, and stale is refused")
    return 0


if __name__ == "__main__":
    sys.exit(main())
