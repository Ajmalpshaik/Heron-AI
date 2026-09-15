# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-GIT-ISS-006
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Issues - triage by the code, never by the prose, and a code is not an
address.

    python brain/heron_issue.py

WHAT IT IS FOR (docs/28, HERON-GIT-ISS-006)
--------------------------------------------
"Issues and triage." T1, risk PUBLISH. It posts nothing.

TRIAGE WITHOUT UNDERSTANDING THE LANGUAGE
-------------------------------------------
"Triage" normally means reading what somebody wrote and deciding where
it goes, which is language, which is D-34's wall and D-01's host.

But Heron's failures are not prose. D-21 says so about
HERON-ORC-FAIL-004: "Heron's failures are its own bounded set of codes,
so the classification is a table." Every agent declares its failures in
its contract, so the set is not a table somebody maintains - it is
derived from brain/agents, and an issue quoting one of those codes can
be routed without reading a word of the sentence around it.

An issue quoting none is not triaged here. It is handed on, and the
answer says so rather than guessing from the wording.

A CODE IS NOT AN ADDRESS, AND THE NUMBERS SAY SO
--------------------------------------------------
Measured on 2026-09-15: 81 contracts declare 361 distinct codes, and 47
of them are declared by MORE THAN ONE agent. `REGISTER_UNREADABLE` is
declared by ten.

That is not a defect. `NOT_A_VERSION` meaning the same thing in seven
agents is the vocabulary being reused on purpose, which is what makes it
a vocabulary. What it means is that routing by code is not addressing -
so a code claimed by several comes back with ALL of them and no
tie-break, because a tie-break here would be a guess wearing a routing
table's clothes.

AN ISSUE IS AN EGRESS, AND THOSE CHECKS COME FIRST
----------------------------------------------------
An issue is public the moment it is posted. So the two rules
HERON-RPT-RED-003 and HERON-GIT-PR-005 apply: a Revit file by name
alone, and a credential refused whole with the value never in the
answer. They run before triage, because triaging a leak neatly is still
publishing one.

NO CONFIRMATION IS DEMANDED HERE, AND THAT IS DELIBERATE
----------------------------------------------------------
docs/28 puts "explicit confirmation, every time" on HERON-GIT-PR-005 and
"per-item human review of the actual payload" on HERON-GIT-COM-010. It
says neither about this row. Demanding one here would be inventing a
requirement the register did not state - so the answer says plainly that
nothing was posted and the decision is the host's, rather than
manufacturing a gate and calling it policy.
"""

from __future__ import annotations

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_contract as CON  # noqa: E402
import heron_release as RELEASE  # noqa: E402
import heron_secrets as SECRETS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

AGENTS = os.path.join(ROOT, "brain", "agents")

NEVER_LEAVES = RELEASE.NEVER_LEAVES

# SCREAMING_SNAKE with at least one underscore. The underscore is what
# tells a failure code from a word somebody shouted - REVIT and MCP and
# BIM are not codes, and flagging them would bury the ones that are.
LOOKS_LIKE_A_CODE = re.compile(r"\b[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)+\b")

AN_ISSUE_CARRIES = (
    ("title", "what somebody sees in a list of two hundred"),
    ("body", "what happened - an empty one asks a maintainer to guess"),
)


def declared(where=None):
    """
    failure code -> the agents declaring it, straight off the contracts.

    Derived rather than listed. D-21 calls the classification a table;
    this is that table, assembled from the contracts that own it.
    """
    folder = where or AGENTS
    book = {}
    if not os.path.isdir(folder):
        return book
    for name in sorted(os.listdir(folder)):
        if not name.endswith((".yaml", ".yml")):
            continue
        try:
            contract = CON.load(os.path.join(folder, name))
        except Exception:                            # noqa: BLE001
            # One unreadable contract costs one contract, not the table.
            continue
        who = str(contract.get("agent") or name).strip()
        for code in (contract.get("failures") or []):
            book.setdefault(str(code).strip(), []).append(who)
    for code in book:
        book[code] = sorted(set(book[code]))
    return book


def triage(issue, secrets=None, contracts=None):
    """
    {routed, ambiguous, unknown_codes, why} - or a refusal. Nothing is
    posted.
    """
    if not issue:
        return {"triaged": False, "refused": "NOTHING_TO_FILE",
                "why": "no issue was handed in."}

    issue = getattr(issue, "data", issue)
    if not isinstance(issue, dict):
        return {"triaged": False, "refused": "NOT_AN_ISSUE",
                "why": "%r is not an issue. One carries %s."
                       % (issue, ", ".join(field for field, _
                                           in AN_ISSUE_CARRIES))}

    missing = [field for field, _ in AN_ISSUE_CARRIES
               if not str(issue.get(field) or "").strip()]
    if missing:
        return {"triaged": False, "refused": "NOT_AN_ISSUE",
                "missing": missing,
                "why": "the issue is missing %s. %s"
                       % (", ".join(missing),
                          " ".join(why for field, why in AN_ISSUE_CARRIES
                                   if field in missing))}

    # THE EGRESS CHECKS FIRST. Triaging a leak neatly is still
    # publishing one.
    binaries = RELEASE._binaries(issue.get("attachments") or [])
    if binaries:
        return {"triaged": False, "refused": "CARRIES_A_MODEL",
                "attachments": [name for name, _ in binaries],
                "why": "%s. D-26 draws the line at the FILE - and an issue "
                       "is public the moment it is posted, to everybody, "
                       "for as long as the repository exists."
                       % "; ".join("'%s' is a %s" % (name, extension)
                                   for name, extension in binaries)}

    keeper = secrets if secrets is not None else SECRETS.Secrets()
    for field in ("title", "body"):
        _, found = keeper.redact(str(issue.get(field) or ""))
        if found:
            return {"triaged": False, "refused": "CARRIES_A_SECRET",
                    "where": field, "found": sorted(set(found)),
                    "why": "the %s carries %s. Refused WHOLE rather than "
                           "stripped, and the value is not in this answer "
                           "- only that it was there. A pasted stack trace "
                           "is the commonest way a token reaches a public "
                           "issue." % (field, ", ".join(sorted(set(found))))}

    book = contracts if contracts is not None else declared()
    if not book:
        return {"triaged": False, "refused": "NO_CONTRACTS",
                "why": "no contracts could be read from %s, so there is no "
                       "table to triage against. D-21 makes the "
                       "classification a table; a table nobody can read is "
                       "not one this agent will improvise around."
                       % os.path.relpath(AGENTS, ROOT)}

    text = "%s\n%s" % (issue["title"], issue["body"])
    spotted = []
    for code in LOOKS_LIKE_A_CODE.findall(text):
        if code not in spotted:
            spotted.append(code)

    routed, ambiguous, unknown = [], [], []
    for code in spotted:
        owners = book.get(code)
        if not owners:
            # REPORTED, NEVER DROPPED. A code nothing declares is either
            # a typo or a failure somebody removed, and both are worth
            # a maintainer's eye.
            unknown.append(code)
        elif len(owners) == 1:
            routed.append({"code": code, "agent": owners[0]})
        else:
            ambiguous.append({"code": code, "agents": owners,
                              "why": "%d agents declare %s, so the code "
                                     "does not say which. No tie-break is "
                                     "applied - one here would be a guess "
                                     "wearing a routing table's clothes."
                                     % (len(owners), code)})

    return {
        "triaged": True, "title": str(issue["title"]).strip(),
        "routed": routed, "ambiguous": ambiguous,
        "unknown_codes": unknown, "codes_seen": spotted,
        "of": len(book),
        "why": "%s. %d code(s) in the text: %d routed to one agent, %d "
               "claimed by several, %d declared by nobody."
               % ("triaged by code" if routed or ambiguous
                  else "no failure code in the text, so nothing was triaged",
                  len(spotted), len(routed), len(ambiguous), len(unknown)),
        "unjudged": [
            "%s" % ("WHAT THE ISSUE SAYS. Only the CODES were read; the "
                    "sentences around them were not. Routing on the prose "
                    "is language, which D-34 builds nothing for and D-01 "
                    "leaves with the host." if spotted else
                    "EVERYTHING. No failure code appears in this issue, so "
                    "nothing here triaged it. D-21 makes the "
                    "classification a table of CODES, and an issue without "
                    "one is prose - the host's under D-01, not this "
                    "agent's to guess at."),
            "%s" % ("%d CODE(S) ARE CLAIMED BY SEVERAL AGENTS AND CAME "
                    "BACK WITH ALL OF THEM: %s. Measured across the whole "
                    "library, 47 of 361 codes are - which is the "
                    "vocabulary being reused on purpose, not a defect, and "
                    "it means routing by code is not addressing."
                    % (len(ambiguous),
                       ", ".join(one["code"] for one in ambiguous))
                    if ambiguous else
                    "every code found is declared by exactly one agent, so "
                    "every route here is unambiguous."),
            "%s" % ("%d TOKEN(S) LOOK LIKE A FAILURE CODE AND ARE DECLARED "
                    "BY NOBODY: %s. Reported rather than dropped - a code "
                    "nothing declares is either a typo or a failure "
                    "somebody removed, and both want a maintainer's eye."
                    % (len(unknown), ", ".join(unknown)) if unknown else
                    "every code-shaped token in the text is declared "
                    "somewhere."),
            "NOTHING WAS POSTED, AND NO CONFIRMATION WAS DEMANDED. docs/28 "
            "puts 'explicit confirmation, every time' on "
            "HERON-GIT-PR-005 and 'per-item human review' on "
            "HERON-GIT-COM-010, and says neither here - inventing one "
            "would be manufacturing a gate and calling it policy. The "
            "decision is the host's.",
        ],
    }


def main(argv):
    print("ISSUES   triage by the code, never by the prose")
    print("=" * 72)

    book = declared()
    shared = sorted((code for code in book if len(book[code]) > 1),
                    key=lambda code: -len(book[code]))
    print("\n%d distinct failure code(s) across the contracts, %d claimed "
          "by more than one agent" % (len(book), len(shared)))
    for code in shared[:3]:
        print("  %-26s %d agents" % (code, len(book[code])))

    answer = triage({
        "title": "heron_context refuses with NOT_A_SCOPE",
        "body": "Running heron_context on a company store gives "
                "NOT_A_SCOPE, and before that I saw NOT_A_FRAGMENT and "
                "something called WIDGET_EXPLODED. Revit 2024.",
    })
    print("\n%s" % answer["why"])
    for one in answer["routed"]:
        print("  routed     %-22s -> %s" % (one["code"], one["agent"]))
    for one in answer["ambiguous"]:
        print("  ambiguous  %-22s -> %s" % (one["code"],
                                            ", ".join(one["agents"])))
    for one in answer["unknown_codes"]:
        print("  unknown    %s" % one)

    quiet = triage({"title": "it is slow", "body": "Selecting ducts takes "
                                                   "ages on a big model."})
    print("\n%s" % quiet["why"])

    print("\nrefused")
    for issue, kwargs in (
            (None, {}), ("a string", {}), ({"title": "x"}, {}),
            ({"title": "x", "body": "y", "attachments": ["Tower A.rvt"]}, {}),
            ({"title": "x", "body": "token ghp_" + "A" * 36}, {}),
            ({"title": "x", "body": "y"}, {"contracts": {}})):
        bad = triage(issue, **kwargs)
        print("  %-22s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
