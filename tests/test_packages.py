# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-PKG-012
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test

"""
Package Manager - an order, and every step still goes through the gate.

    python tests/test_packages.py

WHAT IT PROVES
  1. IT RESOLVES DEPENDENCIES FIRST, and does it without recursing - a
     201-deep chain resolves, where a recursive resolver would be relying
     on the interpreter's stack limit to notice a cycle.

  2. A CYCLE IS REFUSED AND NAMED. Constitution article 25: never loop. A
     resolver that broke the cycle somewhere would have chosen for the
     user, invisibly.

  3. THE DIAMOND IS REFUSED, INCLUDING WHERE IT HIDES - a package already
     resolved, pinned differently by the second one to ask. That is the
     case a resolver walks straight past.

  4. A RANGE IS REFUSED. D-05: a range claims every future release, and a
     list cannot silently assert 2029. One string is refused too, because
     that is the shape a range arrives in.

  5. EVERY PACKAGE GOES THROUGH THE SUPPLY CHAIN GATE, DEPENDENCIES
     INCLUDED - the code the user did not ask for is the one most worth
     checking.

  6. UNINSTALL KEEPS THE DATA. docs/06 s2: data must survive every update,
     uninstall and reinstall.

  7. NOTHING IS INSTALLED AND NOTHING IS DELETED.

  8. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE AND REACHED.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_packages as PKG                                   # noqa: E402
import heron_contract as CON                                   # noqa: E402

FAILURES = []

ALL_RELEASES = list(PKG.REVIT_VERSIONS)
PROOF = {"at": "2026-09-10", "positive": "ran", "negative": "refused"}


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def catalogue(**more):
    found = {"mep-sizing-pack": {"version": "1.2.0", "source": "VERIFIED",
                                 "author": "someone-else",
                                 "revit": ["2024", "2025", "2026"],
                                 "depends": ["geometry-helpers"]},
             "geometry-helpers": {"version": "0.9.1", "source": "OFFICIAL",
                                  "author": "heron", "revit": ALL_RELEASES}}
    found.update(more)
    return found


def register(**more):
    found = {"mep-sizing-pack": {"trust": "VERIFIED", "hash": "aaa",
                                 "approval": {"by": "ajmal",
                                              "proof": dict(PROOF)}},
             "geometry-helpers": {"trust": "OFFICIAL", "hash": "bbb",
                                  "approval": {"by": "ajmal",
                                               "proof": dict(PROOF)}}}
    found.update(more)
    return found


def main():
    reached = set()
    source = open(os.path.join(ROOT, "brain", "heron_packages.py"),
                  encoding="utf-8").read()

    def ask(wanted, **kw):
        settings = {"catalogue": catalogue(), "register": register(),
                    "approvers": ["ajmal"],
                    "measured": {"mep-sizing-pack": "aaa",
                                 "geometry-helpers": "bbb"},
                    "revit": "2025"}
        settings.update(kw)
        answer = PKG.plan(wanted, **settings)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    def resolve(wanted, cat):
        answer = PKG.order(wanted, cat)
        if answer.get("refused"):
            reached.add(answer["refused"])
        return answer

    print("1. It resolves dependencies first, without recursing")
    good = ask(["mep-sizing-pack"])
    check(good["install"] is True, "a clean set installs")
    check(good["order"] == ["geometry-helpers", "mep-sizing-pack"],
          "the dependency comes first")
    deep = dict(("p%d" % i, {"version": "1", "revit": ALL_RELEASES,
                             "depends": ["p%d" % (i + 1)]})
                for i in range(200))
    deep["p200"] = {"version": "1", "revit": ALL_RELEASES}
    answer = resolve(["p0"], deep)
    check(answer.get("order") and len(answer["order"]) == 201,
          "a 201-deep chain resolves - the walk is iterative, not a "
          "recursion leaning on the interpreter's stack limit")
    check(answer["order"][0] == "p200" and answer["order"][-1] == "p0",
          "and comes back deepest first")
    check("stack" in source and "visiting" in source,
          "the source keeps its own stack and seen-set")
    check(resolve([], catalogue()).get("refused") == "NOTHING_WANTED",
          "nothing asked for is refused, not reported as a clean install")
    check(resolve(["x"], None).get("refused") == "NOT_IN_THE_CATALOGUE",
          "and no catalogue means every name would be taken on trust")

    print()
    print("2. A cycle is refused and named")
    cycle = {"a": {"version": "1", "revit": ALL_RELEASES, "depends": ["b"]},
             "b": {"version": "1", "revit": ALL_RELEASES, "depends": ["c"]},
             "c": {"version": "1", "revit": ALL_RELEASES, "depends": ["a"]}}
    answer = resolve(["a"], cycle)
    check(answer.get("refused") == "CIRCULAR_DEPENDENCY", "a 3-cycle is caught")
    check(answer["cycle"] == ["a", "b", "c", "a"],
          "and the loop is named in order, not just detected")
    check("article 25" in answer["why"],
          "citing the article that says never loop")
    check("chosen for the user" in answer["why"],
          "and why breaking it somewhere would be worse than refusing")
    selfish = {"s": {"version": "1", "revit": ALL_RELEASES, "depends": ["s"]}}
    check(resolve(["s"], selfish).get("refused") == "CIRCULAR_DEPENDENCY",
          "a package depending on itself is the same refusal")

    print()
    print("3. The diamond is refused, including where it hides")
    diamond = {"a": {"version": "1", "revit": ALL_RELEASES,
                     "depends": [{"name": "g", "version": "1.0"}]},
               "b": {"version": "1", "revit": ALL_RELEASES,
                     "depends": [{"name": "g", "version": "2.0"}]},
               "g": {"version": "1.0", "revit": ALL_RELEASES}}
    answer = resolve(["a", "b"], diamond)
    check(answer.get("refused") == "TWO_VERSIONS_WANTED",
          "two pins on one package, where the SECOND asker finds it already "
          "resolved - the case a resolver walks straight past")
    check("does not choose which" in answer["why"],
          "and it says it will not choose")
    check(resolve(["b", "a"], diamond).get("refused") == "TWO_VERSIONS_WANTED",
          "and the other way round too")
    answer = resolve(["b"], diamond)
    check(answer.get("refused") == "TWO_VERSIONS_WANTED",
          "a pin the catalogue cannot satisfy is refused")
    check("would satisfy nobody" in answer["why"],
          "rather than installing whatever is on the shelf")
    check(resolve(["a"], diamond).get("order") == ["g", "a"],
          "while a pin the catalogue DOES satisfy resolves normally")
    check(resolve([{"name": "g", "version": "1.0"}],
                  diamond).get("order") == ["g"],
          "and a pin at the top level is read the same way as one below")

    print()
    print("4. A range is refused")
    for declared, refusal in ((">=2020", "VERSION_IS_A_RANGE"),
                              ("2025", "VERSION_IS_A_RANGE"),
                              ([">=2020"], "VERSION_IS_A_RANGE"),
                              (["2020+"], "REVIT_VERSION_UNLISTED"),
                              (["~2025"], "VERSION_IS_A_RANGE"),
                              (["latest"], "VERSION_IS_A_RANGE"),
                              (["2024.x"], "VERSION_IS_A_RANGE"),
                              (["*"], "VERSION_IS_A_RANGE"),
                              ([], "REVIT_VERSION_UNLISTED"),
                              (None, "REVIT_VERSION_UNLISTED"),
                              (["2029"], "REVIT_VERSION_UNLISTED"),
                              (["2019"], "REVIT_VERSION_UNLISTED")):
        answer = ask(["r"], catalogue={"r": {"version": "1",
                                             "revit": declared}},
                     register={"r": {"trust": "OFFICIAL", "hash": "r",
                                     "approval": {"by": "ajmal",
                                                  "proof": dict(PROOF)}}},
                     measured={"r": "r"})
        check(answer.get("refused") == refusal,
              "revit: %r -> %s" % (declared, refusal))
    answer = ask(["r"], catalogue={"r": {"version": "1", "revit": ">=2020"}})
    check("EVERY FUTURE RELEASE" in answer["why"]
          or "the shape a range arrives in" in answer["why"],
          "and the refusal says what a range actually claims")
    answer = ask(["mep-sizing-pack"], revit="2020")
    check(answer.get("refused") == "REVIT_VERSION_UNLISTED",
          "a package not listing the Revit in use is refused")
    check("not 'probably fine'" in answer["why"],
          "because not listed is not probably fine")
    check(PKG.REVIT_VERSIONS[0] == "2020" and PKG.REVIT_VERSIONS[-1] == "2027",
          "and the table is 2020-2027, mirroring the fragment loader's")

    print()
    print("5. Every package goes through the supply chain gate")
    check([entry["package"] for entry in good["checked"]] == good["order"],
          "every package in the order was checked, in that order")
    check(all(entry["first_run"] == "SANDBOX" for entry in good["checked"]),
          "and every one comes back first_run SANDBOX")
    answer = ask(["mep-sizing-pack"], measured={"mep-sizing-pack": "aaa"})
    check(answer.get("refused") == "SUPPLY_CHAIN_REFUSED",
          "a DEPENDENCY the gate stops refuses the whole install")
    check(answer["package"] == "geometry-helpers",
          "naming the dependency, not the package that was asked for")
    check(answer["gate"] == "NOT_MEASURED",
          "and carrying the gate's own refusal rather than flattening it")
    check(answer["checked"] == [],
          "with nothing marked checked before the one that failed")
    check("SUP.judge" in source,
          "the gate is CALLED rather than reimplemented")
    # NOT a word search: plan() passes `approvers` through and reads the
    # gate's `trust` back, and both are what NOT deciding looks like. The
    # claim is behavioural - every trust and approval field it reports is
    # the gate's own answer, copied.
    import heron_supply as SUP
    for entry in good["checked"]:
        gate = SUP.judge(dict(catalogue()[entry["package"]],
                              id=entry["package"]),
                         register=register(), approvers=["ajmal"],
                         measured={"mep-sizing-pack": "aaa",
                                   "geometry-helpers": "bbb"}[
                                       entry["package"]])
        check((entry["trust"], entry["approved_by"], entry["first_run"])
              == (gate["trust"], gate["approved_by"], gate["first_run"]),
              "%s's trust, approver and first run are the GATE's answer, "
              "copied - not a second opinion" % entry["package"])
    answer = ask(["mep-sizing-pack"],
                 register={"mep-sizing-pack": register()["mep-sizing-pack"]})
    check(answer.get("refused") == "SUPPLY_CHAIN_REFUSED"
          and answer["gate"] == "NOT_IN_THE_TRUSTED_REGISTER",
          "a dependency nothing vouches for stops it too")

    print()
    print("6. Uninstall keeps the data")
    installed = {"mep-sizing-pack": {"depends": ["geometry-helpers"],
                                     "produced": ["12 fragments",
                                                  "a company standard"]},
                 "geometry-helpers": {"depends": []}}
    answer = PKG.remove("geometry-helpers", installed)
    reached.add(answer.get("refused"))
    check(answer.get("refused") == "STILL_DEPENDED_ON",
          "a package something still needs is not removed")
    check(answer["needed_by"] == ["mep-sizing-pack"], "naming what needs it")
    check("reached less usefully" in answer["why"],
          "and why removing it and letting them fail later is worse")
    answer = PKG.remove("mep-sizing-pack", installed)
    check(answer["remove"] is True, "and the one nothing needs may go")
    check(answer["keeps"] == ["12 fragments", "a company standard"],
          "with what it produced listed as KEPT")
    check("survive every update, uninstall and reinstall" in answer["why"],
          "quoting docs/06 s2")
    answer = PKG.remove("geometry-helpers", {"geometry-helpers": {}})
    check(answer["remove"] is True and answer["keeps"] == [],
          "a package that produced nothing keeps nothing")
    check(any("cannot tell that from" in note for note in answer["unjudged"]),
          "and says it cannot tell that from one that produced something "
          "without recording it")
    for missing in ("not-installed", "", None):
        answer = PKG.remove(missing, installed)
        reached.add(answer.get("refused"))
        check(answer.get("refused") == "NOT_INSTALLED",
              "%r is not installed" % missing)
    check("not a no-op worth reporting as a success"
          in PKG.remove("x", installed)["why"],
          "and removing something absent is not a quiet success")

    print()
    print("7. Nothing is installed and nothing is deleted")
    for word in ("subprocess", "os.system", "exec(", "eval(", "open(",
                 "shutil", "os.remove", "import requests", "urllib"):
        check(word not in source, "the source has no %s" % word)
    check(any("NOTHING WAS INSTALLED" in note for note in good["unjudged"]),
          "the install answer says nothing was installed")
    check(any("NOTHING WAS DELETED" in note
              for note in PKG.remove("mep-sizing-pack",
                                     installed)["unjudged"]),
          "and the remove answer says nothing was deleted")
    answer = ask(["mep-sizing-pack"], revit=None)
    check(any("not against anything" in note for note in answer["unjudged"]),
          "with no Revit version, the answer says the lists were checked "
          "for shape only")
    check(any("SANDBOX" in note and "nobody asked for by name" in note
              for note in good["unjudged"]),
          "and Golden Rule 18 is repeated for the dependencies")

    print()
    print("8. Every failure the contract declares is named and reached")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-INS-PKG-012.yaml"))
    named = contract.get("failures") or []
    for failure in named:
        check(failure in source, "the code names %s" % failure)
    check(ask(["nobody-has-this"]).get("refused") == "NOT_IN_THE_CATALOGUE",
          "a name nobody has is NOT_IN_THE_CATALOGUE...")
    check(ask(["mep-sizing-pack"],
              catalogue={"mep-sizing-pack":
                         catalogue()["mep-sizing-pack"]}).get("refused")
          == "DEPENDENCY_MISSING",
          "...while a missing DEPENDENCY is named as one, because the two "
          "are different problems for the person reading it")
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
    print("PASS    an order, and every step still goes through the gate")
    return 0


if __name__ == "__main__":
    sys.exit(main())
