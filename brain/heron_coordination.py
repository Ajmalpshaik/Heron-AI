# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-QA-CLS-012
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Clash and coordination - you cannot clash against a link that is not
loaded.

    python brain/heron_coordination.py

WHAT IT IS FOR (docs/28, HERON-QA-CLS-012)
-------------------------------------------
"Clash analysis, clearance, system coordination, LINKED-MODEL
COORDINATION REPORTS." T2, risk ANALYZE.

THE CLASH ITSELF IS GEOMETRY AND IS NOT DONE HERE
---------------------------------------------------
Whether a duct passes through a beam is solid intersection, inside
Revit, and nothing in the brain can see it. That half of the row needs
the Revit API's own solid-intersection filter, or a clash engine, and it
is named here rather than pretended at.

What CAN be done on this side is the half that goes wrong first on a
real job, and it is arithmetic.

THE ANSWER THAT IS WORSE THAN NO ANSWER
-----------------------------------------
    "Clash check complete. 0 clashes."

with the structural model unloaded. HERON-REVIT-LNK-015 already reports
that an unloaded link's element count is NOT KNOWN rather than zero, and
this is the same truth one level up: a link that is not loaded cannot
clash, so a clean result against it is a result nobody took.

So every link is sorted into one of three, and they are never added
together:

    CLASHABLE      loaded, with elements. A clash check over it means
                   something.
    NOT CLASHABLE  not loaded, not found, in a closed workset. A clean
                   answer here is not a clean model.
    EMPTY          loaded and holding nothing. Different again - the
                   link is there and there is nothing in it.

COORDINATION IS AGREEMENT ABOUT THE THINGS EVERYTHING HANGS FROM
------------------------------------------------------------------
Before geometry, two models have to agree about levels and grids.
docs/28 gives HERON-REVIT-LVL-027 its own row - "the hosts almost
everything depends on" - and a duct at Level 3 in a host whose link
calls the same slab Level 03 is a coordination problem that no clash
engine reports, because nothing intersects.

That comparison is set arithmetic over names, and it is done here: in
both, only in the host, only in the link. Names are compared as written,
because `L03` and `Level 3` being the same floor is a judgement and
HERON-STD-NAM-004 already refuses to tidy a name.

NOTHING IS CALLED A CLASH AND NOTHING IS CALLED WRONG
-------------------------------------------------------
A level in the host and not in a link may be perfectly correct - the
architect models no plant room. What is reported is the difference and
which side it is on, and the whole answer says what it could not check.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The three states a link can be in for the purposes of a clash check.
# Never added together: a clean answer over the second is not a clean
# model, and a clean answer over the third is not a check.
CLASHABLE = "clashable"
NOT_CLASHABLE = "not clashable"
EMPTY = "empty"

# What HERON-REVIT-LNK-015 reports as a link's status when it is loaded.
# Anything else means Revit does not have it in memory.
LOADED = "loaded"

# What the two models must agree about before geometry is worth looking
# at. docs/28 gives levels and grids their own row - "the hosts almost
# everything depends on".
HOSTS = ("levels", "grids")

CANNOT = ("the clash itself. Whether a duct passes through a beam is "
          "solid intersection, inside Revit, and nothing in the brain "
          "can see it. That needs the Revit API or a clash engine")


def _named(things):
    """Names as written, de-duplicated, order kept."""
    out = []
    for one in (things or []):
        one = str(getattr(one, "data", one))
        if one and one not in out:
            out.append(one)
    return out


def clashable(links):
    """
    Each link sorted into one of three, with why.

    Reads HERON-REVIT-LNK-015's own record: `status` and `elements`,
    where `elements` is None when the link is not loaded - that agent
    reports NOT KNOWN rather than 0, and this keeps the distinction.
    """
    out = []
    for link in (links or []):
        link = getattr(link, "data", link)
        if not isinstance(link, dict):
            continue
        status = str(link.get("status") or "").strip().lower()
        count = link.get("elements")
        card = {"name": link.get("name"), "status": link.get("status")}

        if status != LOADED:
            card["state"] = NOT_CLASHABLE
            card["why"] = ("the link is %s, so nothing in it can clash with "
                           "anything. A clean result against it is a result "
                           "nobody took" % (link.get("status") or "not "
                                            "loaded"))
        elif not isinstance(count, int):
            card["state"] = NOT_CLASHABLE
            card["why"] = ("the link says it is loaded and reports no "
                           "element count. HERON-REVIT-LNK-015 returns NOT "
                           "KNOWN rather than 0, and a check over an "
                           "unknown is not a check")
        elif count == 0:
            card["state"] = EMPTY
            card["elements"] = 0
            card["why"] = ("the link is loaded and holds nothing. That is "
                           "not the same as not being loaded - it is there, "
                           "and there is nothing in it")
        else:
            card["state"] = CLASHABLE
            card["elements"] = count
            card["why"] = "loaded, holding %d element(s)" % count
        out.append(card)
    return out


def hosts(host, link):
    """Levels and grids: in both, only in the host, only in the link."""
    out = {}
    for what in HOSTS:
        mine = _named((host or {}).get(what))
        theirs = _named((link or {}).get(what))
        out[what] = {
            "inBoth": [one for one in mine if one in theirs],
            "onlyInHost": [one for one in mine if one not in theirs],
            "onlyInLink": [one for one in theirs if one not in mine],
            "of": {"host": len(mine), "link": len(theirs)},
        }
    return out


def coordinate(host, links=None):
    """
    {coordinated, links, hosts, cannot} - or a refusal. No clash is
    computed and nothing is called wrong.
    """
    host = getattr(host, "data", host)
    if not isinstance(host, dict) or not host:
        return {"coordinated": False, "refused": "NOT_A_MODEL",
                "why": "%r is not a host model's facts. One carries its "
                       "levels and grids by name, and the links it holds."
                       % (host,)}

    links = list(links if links is not None else (host.get("links") or []))
    if not links:
        return {"coordinated": False, "refused": "NOTHING_LINKED",
                "why": "the host links no model. There is nothing to "
                       "coordinate WITH, which is a fact about this model "
                       "and not a clean coordination report."}

    sorted_links = clashable(links)
    if not sorted_links:
        return {"coordinated": False, "refused": "NOT_A_LINK",
                "why": "%r holds nothing that reads as a link. "
                       "HERON-REVIT-LNK-015 returns {name, status, "
                       "elements, ...} for each." % (links,)}

    ready = [card for card in sorted_links if card["state"] == CLASHABLE]
    blind = [card for card in sorted_links if card["state"] == NOT_CLASHABLE]
    hollow = [card for card in sorted_links if card["state"] == EMPTY]

    agreement = []
    for link in links:
        link = getattr(link, "data", link)
        if not isinstance(link, dict):
            continue
        if not any(link.get(what) for what in HOSTS):
            continue
        agreement.append({"link": link.get("name"),
                          "hosts": hosts(host, link)})

    disagreeing = [one for one in agreement
                   if any(one["hosts"][what]["onlyInHost"]
                          or one["hosts"][what]["onlyInLink"]
                          for what in HOSTS)]

    return {
        "coordinated": True,
        "of": len(sorted_links),
        "links": sorted_links,
        "clashable": [card["name"] for card in ready],
        "notClashable": [card["name"] for card in blind],
        "empty": [card["name"] for card in hollow],
        "hosts": agreement,
        "cannot": [CANNOT],
        "clashesFound": None,
        "why": "%d link%s: %d a clash check would mean something over, %d "
               "it would not, %d loaded and empty. %s No clash was "
               "computed."
               % (len(sorted_links), "" if len(sorted_links) == 1 else "s",
                  len(ready), len(blind), len(hollow),
                  "%d link%s disagrees with the host about levels or grids."
                  % (len(disagreeing), "" if len(disagreeing) == 1 else "s")
                  if disagreeing else
                  "Every link that reported levels or grids agrees with the "
                  "host about them." if agreement else
                  "No link reported its levels or grids."),
        "unjudged": [
            "THE CLASH ITSELF WAS NOT COMPUTED, and `clashesFound` is null "
            "rather than 0. %s" % CANNOT,
            ("%d LINK%s CANNOT BE CLASHED AGAINST: %s. A clean result over "
             "%s is not a clean model - it is a result nobody took, and it "
             "is the most dangerous answer this agent could give."
             % (len(blind), "" if len(blind) == 1 else "S",
                ", ".join(str(card["name"]) for card in blind),
                "it" if len(blind) == 1 else "them")
             if blind else
             "every link is loaded and holds elements, so a clash check "
             "over any of them would mean something."),
            ("%d LINK%s LOADED AND EMPTY, which is a third state: the link "
             "is there and there is nothing in it. Not the same as not "
             "being loaded, and not the same as a clean check."
             % (len(hollow), "" if len(hollow) == 1 else "S")
             if hollow else
             "no link was loaded and empty."),
            ("%d LINK%s DISAGREES WITH THE HOST ABOUT %s, WHICH NO CLASH "
             "ENGINE WOULD REPORT - nothing intersects. It is also not "
             "necessarily wrong: the architect may model no plant room."
             % (len(disagreeing), "" if len(disagreeing) == 1 else "S",
                " or ".join(HOSTS))
             if disagreeing else
             "no link disagreed with the host about levels or grids, among "
             "those that reported them."),
            "NAMES ARE COMPARED AS WRITTEN. `L03` and `Level 3` being the "
            "same floor is a judgement, and HERON-STD-NAM-004 already "
            "refuses to tidy a name for the same reason.",
            "NOTHING WAS CALLED WRONG. What is reported is the difference "
            "and which side it is on.",
        ],
    }


def main(argv):
    print("CLASH AND COORDINATION   you cannot clash against a link that is "
          "not loaded")
    print("=" * 72)
    print("\nthree states, never added together: %s / %s / %s"
          % (CLASHABLE, NOT_CLASHABLE, EMPTY))

    host = {"levels": ["L01", "L02", "L03", "ROOF"],
            "grids": ["A", "B", "C"]}
    links = [
        {"name": "Tower B ARCH", "status": "loaded", "elements": 48210,
         "levels": ["L01", "L02", "L03", "ROOF"], "grids": ["A", "B", "C"]},
        {"name": "Tower B STRUCT", "status": "unloaded", "elements": None},
        {"name": "Tower B ELEC", "status": "loaded", "elements": 0},
        {"name": "Site", "status": "not found", "elements": None},
        {"name": "Tower B PLUMB", "status": "loaded", "elements": 3100,
         "levels": ["Level 1", "Level 2", "Level 3"], "grids": ["A", "B"]},
    ]

    answer = coordinate(host, links)
    print("\n%s" % answer["why"])
    print("\n%-18s %-16s %s" % ("link", "state", "why"))
    for card in answer["links"]:
        print("  %-16s %-16s %s" % (card["name"], card["state"],
                                    card["why"][:38]))

    print("\nlevels and grids")
    for one in answer["hosts"]:
        for what in HOSTS:
            sets = one["hosts"][what]
            if sets["onlyInHost"] or sets["onlyInLink"]:
                print("  %-16s %-7s host only %s | link only %s"
                      % (one["link"], what,
                         ", ".join(sets["onlyInHost"]) or "-",
                         ", ".join(sets["onlyInLink"]) or "-"))
            else:
                print("  %-16s %-7s agree on all %d"
                      % (one["link"], what, sets["of"]["host"]))

    print("\nclashes found: %r  (not 0 - nothing was computed)"
          % answer["clashesFound"])

    print("\nrefused")
    for these, link in ((None, links), ({}, links), (host, []),
                        (host, ["a string"])):
        bad = coordinate(these, link)
        print("  %-20s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
