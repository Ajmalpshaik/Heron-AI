# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-PKG-012
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Package Manager - an order to install in, and every step still goes through
the gate.

    python brain/heron_packages.py

WHAT IT IS FOR (docs/28, HERON-INS-PKG-012)
--------------------------------------------
"Optional Heron capabilities - skill packs, Revit tool packs, additional
providers. Resolves dependencies, checks compatibility, installs,
registers, and can uninstall cleanly." T1, risk ADMIN.

RESOLVING IS THE EASY HALF. THE HARD HALF IS REFUSING TO GUESS
----------------------------------------------------------------
A resolver that always produces an order is a resolver that has invented
something. There are four situations where the honest answer is no, and
each one is a real shape a package set arrives in:

  A DEPENDENCY NOBODY LISTS    the graph has a hole and installing anyway
                               means installing something incomplete
  A CYCLE                      A needs B needs A. Constitution article 25:
                               never loop. A resolver that "breaks the
                               cycle somewhere" has chosen for the user
  TWO VERSIONS OF ONE THING    the diamond. One package needs 1.0 and
                               another needs 2.0, and picking either is
                               silently breaking the other
  A RANGE                      see below - this one is a decision, not an
                               inconvenience

A RANGE CLAIMS EVERY FUTURE RELEASE
-------------------------------------
`heron_fragment.py` records why, and D-05 is the reason: Heron supports
Revit 2020 through the latest release, and **an unlisted release is an
error, never a guess. The table is never extrapolated forward.** A package
declaring `revit: ">=2020"` is asserting it works on Revit 2029, which
nobody has seen. A list cannot silently assert 2029, so a list is what is
required - and a range is refused rather than read as one.

EVERY PACKAGE GOES THROUGH THE SUPPLY CHAIN GATE, INCLUDING DEPENDENCIES
-------------------------------------------------------------------------
HERON-INS-SUP-013 is where identity, trust, modification and approval are
decided, and this agent does not repeat any of it - it calls it, once per
package in the resolved order, and stops at the first refusal. A
dependency pulled in by a package the user asked for is code the user did
not ask for, which makes it the one most worth checking rather than the
one to wave through.

"UNINSTALL CLEANLY" MEANS THE PRODUCT GOES AND THE DATA STAYS
---------------------------------------------------------------
docs/06 s2 splits the workspace into product, data and derived, and says
data "must survive every update, uninstall and reinstall". A skill pack
that produced fragments, memory or project notes leaves them behind when
it goes. That is not untidiness - it is the whole reason the split exists,
and an uninstaller that tidied them away would be deleting the only thing
in the workspace nobody can regenerate.

And nothing is removed while something still needs it. A package two other
packages depend on is not the user's to remove yet, and saying which two
is more useful than removing it and letting them fail later.

IT INSTALLS NOTHING AND REMOVES NOTHING
-----------------------------------------
It returns the order, and the list of what may go. Somebody else does it,
in a process started on purpose - the line HERON-OPS-HEA-006 draws, for
the reason D-84 recorded: nothing here contains a process Heron starts.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_supply as SUP                                     # noqa: E402

# Mirrors heron_fragment.REVIT_VERSIONS, and for the same reason (D-05):
# an unlisted release is an error, never a guess.
REVIT_VERSIONS = ("2020", "2021", "2022", "2023", "2024", "2025", "2026",
                  "2027")

# What a version field must NOT contain. A range claims every future
# release, which is precisely what D-05 forbids.
A_RANGE = (">", "<", "~", "^", "*", "x", "latest", "any")


def _listed(package):
    """(refusal, why) - does it declare Revit versions as an explicit list?"""
    declared = package.get("revit")
    if isinstance(declared, str):
        return "VERSION_IS_A_RANGE", (
            "`revit: %r` is one string. Even when it names a single release "
            "it is the shape a range arrives in, and the field that has to "
            "be a LIST is the one nobody notices has stopped being one."
            % declared)
    if not isinstance(declared, (list, tuple)) or not declared:
        return "REVIT_VERSION_UNLISTED", (
            "nothing says which Revit releases this works on. D-05: an "
            "unlisted release is an error, never a guess.")
    for entry in declared:
        text = str(entry).strip().lower()
        for marker in A_RANGE:
            if marker in text:
                return "VERSION_IS_A_RANGE", (
                    "%r is a range, not a release. A range CLAIMS EVERY "
                    "FUTURE RELEASE - `>=2020` asserts Revit 2029, which "
                    "nobody has seen. A list cannot silently assert 2029, "
                    "which is why D-05 asks for one." % entry)
        if str(entry).strip() not in REVIT_VERSIONS:
            return "REVIT_VERSION_UNLISTED", (
                "%r is not a release Heron supports (%s-%s). The table is "
                "never extrapolated forward."
                % (entry, REVIT_VERSIONS[0], REVIT_VERSIONS[-1]))
    return None, None


def _need(entry):
    """
    (name, version) for one dependency - the version is None when it is
    not pinned.

    A dependency that pins nothing takes whatever the catalogue holds. A
    dependency that pins is what makes a diamond possible at all, and a
    diamond is the case where resolving is choosing.
    """
    if isinstance(entry, dict):
        return (str(entry.get("name") or entry.get("id") or "").strip(),
                str(entry.get("version") or "").strip() or None)
    return str(entry).strip(), None


def order(wanted, catalogue):
    """
    The install order, dependencies first - or a refusal.

    Iterative, with a seen-set: Constitution article 25 says never loop,
    and a resolver that recursed into a cycle would be the thing the
    article is about rather than the thing enforcing it.
    """
    if not wanted:
        return {"refused": "NOTHING_WANTED",
                "why": "no package was asked for. An empty install is not a "
                       "successful one - it is a run nobody asked for."}
    if not isinstance(catalogue, dict):
        return {"refused": "NOT_IN_THE_CATALOGUE",
                "why": "no catalogue was given, so nothing can be looked up "
                       "and every name would be taken on trust."}

    resolved, visiting, path = [], set(), []
    stack = [(_need(entry) + (False,)) for entry in reversed(list(wanted))]
    asked_for = set(_need(entry)[0] for entry in wanted)
    pinned = {}

    while stack:
        name, pin, closing = stack.pop()
        if closing:
            visiting.discard(name)
            if path and path[-1] == name:
                path.pop()
            if name not in resolved:
                resolved.append(name)
            continue
        # THE PIN IS CHECKED BEFORE ANYTHING SHORT-CIRCUITS. A dependency
        # already resolved is exactly where a diamond hides: the second
        # package pins a different version and the resolver, seeing a name
        # it has already dealt with, would walk straight past the conflict.
        if pin:
            if name in pinned and pinned[name] != pin:
                return {"refused": "TWO_VERSIONS_WANTED",
                        "why": "%s is pinned at %s by one package and at %s "
                               "by another. Picking either silently breaks "
                               "whatever asked for the other, and this agent "
                               "does not choose which."
                               % (name, pinned[name], pin)}
            pinned[name] = pin
            found = catalogue.get(name)
            if isinstance(found, dict):
                declared = str(found.get("version") or "").strip()
                if declared != pin:
                    return {"refused": "TWO_VERSIONS_WANTED",
                            "why": "%s is pinned at %s and the catalogue "
                                   "holds %s. The pin is a requirement "
                                   "somebody wrote down; installing what "
                                   "happens to be on the shelf instead "
                                   "would satisfy nobody."
                                   % (name, pin, declared or "no version")}
        if name in resolved:
            continue
        if name in visiting:
            cycle = path[path.index(name):] + [name] if name in path \
                else [name, name]
            return {"refused": "CIRCULAR_DEPENDENCY",
                    "why": "%s. Constitution article 25: never loop. A "
                           "resolver that broke the cycle somewhere would "
                           "have chosen for the user, silently, and the "
                           "choice would be invisible in the result."
                           % " needs ".join(cycle),
                    "cycle": cycle}
        found = catalogue.get(name)
        if not isinstance(found, dict):
            return {"refused": ("NOT_IN_THE_CATALOGUE" if name in asked_for
                                else "DEPENDENCY_MISSING"),
                    "why": "%s is not in the catalogue%s. A graph with a "
                           "hole in it resolves to an install that is "
                           "incomplete and looks finished."
                           % (name,
                              "" if name in asked_for else
                              ", and something depends on it")}

        visiting.add(name)
        path.append(name)
        stack.append((name, pin, True))
        for need in reversed(list(found.get("depends") or [])):
            stack.append(_need(need) + (False,))

    return {"order": resolved,
            "why": "%d package%s, dependencies first."
                   % (len(resolved), "" if len(resolved) == 1 else "s")}


def plan(wanted, catalogue=None, register=None, approvers=None,
         measured=None, revit=None):
    """
    {install, order, checked, why} - or the first refusal, in order.

    Every package in the order goes through HERON-INS-SUP-013. This agent
    repeats none of that gate's checks and does not get to skip it for a
    dependency - a dependency is code the user did not ask for.
    """
    found = order(wanted, catalogue)
    if found.get("refused"):
        return dict(found, install=False)

    revit = str(revit or "").strip()
    checked = []
    for name in found["order"]:
        package = dict(catalogue[name], id=name)

        refusal, why = _listed(package)
        if refusal:
            return {"install": False, "refused": refusal, "package": name,
                    "why": "%s: %s" % (name, why)}
        if revit and revit not in [str(entry).strip()
                                   for entry in package["revit"]]:
            return {"install": False, "refused": "REVIT_VERSION_UNLISTED",
                    "package": name,
                    "why": "%s lists %s and this is Revit %s. Not listed is "
                           "not 'probably fine' - D-05 again."
                           % (name, ", ".join(str(entry) for entry
                                              in package["revit"]), revit)}

        gate = SUP.judge(package, register=register, approvers=approvers,
                         measured=(measured or {}).get(name))
        if not gate.get("install"):
            return {"install": False, "refused": "SUPPLY_CHAIN_REFUSED",
                    "package": name, "gate": gate.get("refused"),
                    "why": "%s did not pass the supply chain gate: %s. %s"
                           % (name, gate.get("refused"), gate.get("why")),
                    "checked": checked}
        checked.append({"package": name, "trust": gate["trust"],
                        "approved_by": gate["approved_by"],
                        "first_run": gate["first_run"]})

    return {
        "install": True, "order": found["order"], "checked": checked,
        "why": "%d package%s in order, each listed for Revit %s and each "
               "past the supply chain gate."
               % (len(checked), "" if len(checked) == 1 else "s",
                  revit or "(no version given)"),
        "unjudged": [
            "NOTHING WAS INSTALLED. This is an order and a set of passes; "
            "somebody else does it, in a process started on purpose - the "
            "line HERON-OPS-HEA-006 draws, for the reason D-84 recorded: "
            "nothing here contains a process Heron starts.",
            "%s"
            % ("no Revit version was given, so the per-package release lists "
               "were checked for SHAPE and not against anything. A package "
               "listed only for 2020 would pass this."
               if not revit else
               "each package lists Revit %s explicitly. Nothing was read as "
               "a range." % revit),
            "every FIRST run is still SANDBOX (Golden Rule 18), including "
            "the dependencies nobody asked for by name.",
        ],
    }


def remove(package_id, installed=None):
    """
    {remove, keeps, why} - or a refusal. Nothing is deleted.

    "Cleanly" is about what is LEFT: the product goes, the data stays.
    """
    package_id = str(package_id or "").strip()
    installed = installed if isinstance(installed, dict) else {}
    if package_id not in installed:
        return {"remove": False, "refused": "NOT_INSTALLED",
                "why": "%s is not installed. Removing something that is not "
                       "there is not a no-op worth reporting as a success."
                       % package_id}

    needed_by = sorted(name for name, entry in installed.items()
                       if name != package_id
                       and package_id in (entry.get("depends") or []))
    if needed_by:
        return {"remove": False, "refused": "STILL_DEPENDED_ON",
                "needed_by": needed_by,
                "why": "%s still needs %s. Removing it and letting them fail "
                       "later is the same outcome reached less usefully."
                       % (", ".join(needed_by), package_id),
                "proposal": "remove %s first, or keep it."
                            % " and ".join(needed_by)}

    entry = installed[package_id]
    keeps = [str(thing) for thing in (entry.get("produced") or [])]
    return {
        "remove": True, "package": package_id,
        "keeps": keeps,
        "why": "%s can go. %s"
               % (package_id,
                  "It produced %s, and %s stay%s - docs/06 s2: data must "
                  "survive every update, uninstall and reinstall."
                  % (", ".join(keeps), "those" if len(keeps) > 1 else "that",
                     "" if len(keeps) > 1 else "s")
                  if keeps else "It produced nothing of the user's."),
        "unjudged": [
            "NOTHING WAS DELETED. This says what may go and what must stay.",
            "what the package produced is read from the install record. A "
            "package that produced something and did not record it is not "
            "visible here, and this agent cannot tell that from a package "
            "that produced nothing.",
        ],
    }


def main(argv):
    print("PACKAGE MANAGER   an order, and every step still goes through")
    print("=" * 72)

    catalogue = {
        "mep-sizing-pack": {"version": "1.2.0", "source": "VERIFIED",
                            "author": "someone-else",
                            "revit": ["2024", "2025", "2026"],
                            "depends": ["geometry-helpers"]},
        "geometry-helpers": {"version": "0.9.1", "source": "OFFICIAL",
                             "author": "heron", "revit": ["2020", "2021",
                                                          "2022", "2023",
                                                          "2024", "2025",
                                                          "2026", "2027"]},
    }
    proof = {"at": "2026-09-10", "positive": "ran", "negative": "refused"}
    register = {
        "mep-sizing-pack": {"trust": "VERIFIED", "hash": "aaa",
                            "approval": {"by": "ajmal", "proof": dict(proof)}},
        "geometry-helpers": {"trust": "OFFICIAL", "hash": "bbb",
                             "approval": {"by": "ajmal",
                                          "proof": dict(proof)}},
    }
    answer = plan(["mep-sizing-pack"], catalogue=catalogue,
                  register=register, approvers=["ajmal"],
                  measured={"mep-sizing-pack": "aaa",
                            "geometry-helpers": "bbb"}, revit="2025")
    print("  order: %s" % " -> ".join(answer["order"]))
    print("  %s" % answer["why"])
    for entry in answer["checked"]:
        print("    %-20s %-9s approved by %-7s first run %s"
              % (entry["package"], entry["trust"], entry["approved_by"],
                 entry["first_run"]))

    print()
    print("  The four situations where the honest answer is no:")
    cycles = {"a": {"version": "1", "revit": ["2025"], "depends": ["b"]},
              "b": {"version": "1", "revit": ["2025"], "depends": ["a"]}}
    for label, wanted, cat in (
            ("a dependency nobody lists", ["mep-sizing-pack"],
             {"mep-sizing-pack": catalogue["mep-sizing-pack"]}),
            ("a cycle", ["a"], cycles),
            ("a range", ["r"], {"r": {"version": "1",
                                      "revit": [">=2020"]}}),
            ("one string, not a list", ["s"], {"s": {"version": "1",
                                                     "revit": "2025"}}),
            ("a release nobody supports", ["f"], {"f": {"version": "1",
                                                        "revit": ["2029"]}}),
            ("the wrong Revit", ["geometry-helpers"],
             {"geometry-helpers": dict(catalogue["geometry-helpers"],
                                       revit=["2020"])}),
            ("nothing asked for", [], catalogue)):
        answer = plan(wanted, catalogue=cat, register=register,
                      approvers=["ajmal"],
                      measured={"geometry-helpers": "bbb"}, revit="2025")
        print("    %-28s %s" % (label, answer["refused"]))
    answer = plan(["mep-sizing-pack"], catalogue=catalogue, register=register,
                  approvers=["ajmal"], measured={"mep-sizing-pack": "aaa"},
                  revit="2025")
    print("    %-28s %s (%s)" % ("a dependency the gate stops",
                                 answer["refused"], answer["gate"]))

    print()
    print("  Uninstall cleanly - the product goes, the data stays:")
    installed = {
        "mep-sizing-pack": {"depends": ["geometry-helpers"],
                            "produced": ["12 fragments", "a company standard"]},
        "geometry-helpers": {"depends": []},
    }
    answer = remove("geometry-helpers", installed)
    print("    geometry-helpers   %s - %s"
          % (answer["refused"], answer["why"][:64]))
    answer = remove("mep-sizing-pack", installed)
    print("    mep-sizing-pack    remove=%s" % answer["remove"])
    print("      %s" % answer["why"][:96])
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
