# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-APR-014
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Import approval - one rung below what Heron writes itself.

    python tests/test_import.py

WHAT IT PROVES
  1. DISCOVERED IS docs/09's BOTTOM RUNG, and DRAFT is where Heron's own
     authors enter - compared against those two agents themselves, not
     against a sentence about them.

  2. EVERY RUNG ABOVE IT IS REFUSED, one by one, and refused rather than
     quietly lowered.

  3. A CARRIED PROOF IS REFUSED, and the instruction behind it is read
     out of heron_fragment.py where it is recorded.

  4. A MODEL AND A CREDENTIAL NEVER ENTER, and the value is nowhere in
     the answer.

  5. THE ORDER HOLDS: an item that is all four things at once comes back
     as the model.

  6. NOTHING IS ACCEPTED AND NO FILE IS OPENED.

  7. EVERY FAILURE THE CONTRACT DECLARES IS NAMED AND REACHED.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_import as IMP                                     # noqa: E402
import heron_fragment as FRAG                                  # noqa: E402
import heron_generate as FRG_CRE                               # noqa: E402
import heron_authoring as SKL_CRE                              # noqa: E402
import heron_release as RELEASE                                # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

TOKEN = "ghp_" + "A" * 36


def item(**changes):
    one = {"name": "count-ducts", "kind": "fragment",
           "from": "AJ-Tools/Scripts/CountDucts.py"}
    one.update(changes)
    return one


def manifest(*items):
    return {"from": "C:/AJ-Tools", "items": list(items) or [item()]}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    reached = set()
    whole = io.open(os.path.join(ROOT, "brain", "heron_import.py"),
                    encoding="utf-8").read()
    logic = whole.split("\nfrom __future__", 1)[1].split("\ndef main(")[0]
    fragment_source = io.open(os.path.join(ROOT, "brain",
                                           "heron_fragment.py"),
                              encoding="utf-8").read()

    print("\n1. DISCOVERED is the bottom rung, DRAFT is where Heron enters")
    check(IMP.ENTERS_AT == FRAG.STATUSES[0] == "DISCOVERED",
          "DISCOVERED is docs/09's first rung, read from heron_fragment")
    check(IMP.AUTHORED_AT == FRAG.STATUSES[1] == "DRAFT",
          "and DRAFT is the second")
    # COMPARED AGAINST THE AGENTS THEMSELVES.
    check(FRG_CRE.ENTERS_AT == IMP.AUTHORED_AT,
          "HERON-FRG-CRE-007 really does enter its own work at %s"
          % IMP.AUTHORED_AT)
    check(SKL_CRE.ENTERS_AT == IMP.AUTHORED_AT,
          "and so does HERON-SKL-CRE-002")
    check(FRAG.STATUSES.index(IMP.ENTERS_AT)
          < FRAG.STATUSES.index(IMP.AUTHORED_AT),
          "so an import enters strictly BELOW what Heron writes itself")
    good = IMP.present(manifest())
    check(good["status"] == IMP.ENTERS_AT
          and all(one["status"] == IMP.ENTERS_AT for one in good["items"]),
          "and every item comes back at %s" % IMP.ENTERS_AT)

    print("\n2. every rung above it is refused")
    for rung in FRAG.STATUSES[1:]:
        answer = IMP.present(manifest(item(status=rung)))
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "ENTERS_TOO_HIGH",
              "'%s' is too high" % rung)
        check(answer.get("said") == rung, "  and the answer names it")
    check(IMP.present(manifest(item(status="discovered")))["presented"],
          "while DISCOVERED itself goes through, in any case")
    check("efused rather than quietly lowered"
          in IMP.present(manifest(item(status="PROVEN")))["why"],
          "refused rather than lowered - a manifest that asked for more "
          "than it may have is a thing a reviewer should see")

    print("\n3. a carried proof is refused")
    carried = IMP.present(manifest(item(proof={"model": "Tower A, 2024"})))
    reached.add(carried.get("refused"))
    check(carried["refused"] == "CARRIES_A_PROOF",
          "a proof from another library is refused")
    # THE INSTRUCTION IS READ OUT OF WHERE IT IS RECORDED.
    said = ("even in the AJ AI proven fragment don't mark in Heron this is "
            "proven, because we will check each and every one again in "
            "Heron AI.")
    flat = " ".join(fragment_source.replace("#", " ").split())
    check(said in flat,
          "and heron_fragment.py records the instruction behind it")
    check(said in " ".join(carried["why"].split()),
          "which the answer quotes in full")
    check("a request to skip the checking" in carried["why"],
          "with what a manifest arriving with one is asking for")
    check(IMP.present(manifest(item(proof=None)))["presented"],
          "an item with no proof goes through")

    print("\n4. a model and a credential never enter")
    check(IMP.NEVER_LEAVES is RELEASE.NEVER_LEAVES,
          "the extension list is heron_release's object, not a copy")
    for extension in IMP.NEVER_LEAVES:
        answer = IMP.present(manifest(item(files=["Tower A%s" % extension])))
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "CARRIES_A_MODEL",
              "%s never enters" % extension)
    named = IMP.present(manifest(item(name="Tower A.rvt")))
    check(named["refused"] == "CARRIES_A_MODEL",
          "an item merely NAMED like one is refused too")
    check("it is somebody's building" in named["why"],
          "with the reason: a model is not knowledge")
    leaky = IMP.present(manifest(item(content="the token is %s" % TOKEN)))
    reached.add(leaky.get("refused"))
    check(leaky["refused"] == "CARRIES_A_SECRET",
          "a credential in an item's content is refused")
    check(TOKEN not in repr(leaky), "and the value is nowhere in the answer")
    check(leaky["item"] == "count-ducts" and leaky["found"],
          "only the item and the kind: %s" % ", ".join(leaky["found"]))
    check("likeliest place in this project" in leaky["why"],
          "noting imported code is where a credential most often sits")

    print("\n5. the order holds")
    # ALL FOUR THINGS AT ONCE.
    worst = IMP.present(manifest(item(
        files=["Tower A.rvt"], content="token %s" % TOKEN,
        proof={"model": "x"}, status="PROVEN")))
    check(worst["refused"] == "CARRIES_A_MODEL",
          "a model beats a credential, a proof and a status")
    without = IMP.present(manifest(item(
        content="token %s" % TOKEN, proof={"model": "x"}, status="PROVEN")))
    check(without["refused"] == "CARRIES_A_SECRET",
          "a credential beats a proof and a status")
    then = IMP.present(manifest(item(proof={"model": "x"},
                                     status="PROVEN")))
    check(then["refused"] == "CARRIES_A_PROOF",
          "and a proof beats a status")

    print("\n6. nothing is accepted and no file is opened")
    check(good["accepted"] is False,
          "`accepted` is false on the good path, and it is always false")
    check("Nothing was accepted" in good["why"], "and the answer says so")
    for writing in ("open(", "write(", "makedirs", "listdir", "walk("):
        check(writing not in logic, "the agent never uses %s" % writing)
    check(any("never a file on disk" in line.lower()
              or "opened a file on disk" in line for line in
              good["unjudged"]),
          "and the answer says only names and supplied content were read")
    check(good["kinds"] == {"fragment": 1},
          "the shape of the manifest comes back for the reviewer: %s"
          % good["kinds"])

    print("\n7. every declared failure is named and reached")
    for these, name in (
            (None, "NOTHING_TO_PRESENT"),
            ({"from": "x"}, "NOT_A_MANIFEST"),
            ({"items": []}, "NOT_A_MANIFEST"),
            ("a string", "NOT_A_MANIFEST"),
            (manifest("a string"), "NOT_AN_ITEM"),
            (manifest({"name": "x"}), "NOT_AN_ITEM"),
            (manifest(item(), item()), "NOT_AN_ITEM")):
        answer = IMP.present(these)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == name, "%s is reached" % name)

    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-IMP-APR-014.yaml"))
    named_failures = contract.get("failures") or []
    check(len(named_failures) == 7, "the contract declares 7 failures")
    for failure in named_failures:
        check(failure in logic, "the code names %s" % failure)
    unreached = sorted(set(named_failures) - reached)
    check(not unreached,
          "and every one was reached above%s"
          % ("" if not unreached else ": %s" % ", ".join(unreached)))
    check(len(good["unjudged"]) == 4, "four things are left unjudged")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    one rung below what Heron writes itself")
    return 0


if __name__ == "__main__":
    sys.exit(main())
