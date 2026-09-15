# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RPT-RED-003
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Report release - a report is an egress surface, and the line is the file.

    python brain/heron_release.py

WHAT IT IS FOR (docs/28, HERON-RPT-RED-003)
--------------------------------------------
"The gate before a report can be shared or leave the machine. Strips
project identifiers, enforces scope, blocks confidential-project egress.
A REPORT IS AN EGRESS SURFACE." T1, risk PUBLISH - the highest risk of
the four in its department, and the only one that is about leaving.

The last sentence is the whole point. A report is assembled from the
model, the knowledge store and the audit log, and then it is emailed. It
is the one artefact whose normal use is to go somewhere.

"STRIPS PROJECT IDENTIFIERS" IS THE FRAMING D-26 NARROWED AWAY FROM
--------------------------------------------------------------------
D-26 opens with a warning to its own reader:

    "This decision was refined three times on the day it was written,
    each time in the same direction: from NOTHING MAY TRAVEL toward THE
    FILE MAY NOT TRAVEL. The rule below is the final one... a reader
    needs to know which version won."

And the rule that won:

    Never leaves                  Fine in the conversation
    the .rvt and .rfa files       project names, file names, content names
    family and project templates  element data - counts, sizes, parameters
    any Revit binary              engineering ideas, reasoning, and code

So project names are explicitly FINE. This agent does not strip them,
because stripping them would implement the framing D-26 moved away from
- and doing it quietly would leave a report that reads as anonymised
without anybody having decided it should be. Recorded as PROPOSALS F20.

WHAT SURVIVES, AND ALL OF IT IS A REFUSAL RATHER THAN A STRIP
--------------------------------------------------------------
  A REVIT BINARY NEVER LEAVES   .rvt, .rfa, .rte, .rft. Categorical, and
                                the same rule HERON-WSP-TPL-006 keeps on
                                the other side of the machine.

  A SECRET NEVER LEAVES         Constitution article 17: never include
                                one in a result. Refused whole, and the
                                shape is named rather than the value.

  PROJECTS STAY SEGREGATED      D-26's own third point: "project-based
                                knowledge must be kept segregated and
                                separated. Project A's knowledge does
                                not leak into Project B." That is what
                                "enforces scope" means once the
                                identifier-stripping is gone, and it is
                                the half of the row that survives whole.

  A PROJECT MAY FORBID IT       Declared by the project, never inferred.
                                docs/12 s85 is about contracts that
                                prohibit egress, and a contract is a
                                thing somebody signed - not something to
                                guess from a name.

NOTHING LEAVES WITHOUT A NAMED DESTINATION
--------------------------------------------
Egress cannot be checked against "somewhere". A release with no
destination is refused before anything else is looked at, because every
rule below is a rule about where it is going.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_secrets as SECRETS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# D-26's left-hand column. The line is the FILE, not the information.
NEVER_LEAVES = (".rvt", ".rfa", ".rte", ".rft")

# D-26's right-hand column, kept here so the answer can say what IS fine
# rather than only what is not.
TRAVELS = ("project names", "file names", "content names",
           "element data - counts, sizes, parameters",
           "engineering ideas, reasoning, and code")


def _binaries(names):
    """
    Which attached names are Revit files.

    A BARE STRING IS ONE FILENAME. `attachments: "Tower.rvt"` is the
    natural way to write a single attachment, and iterating it gives
    eleven characters, none of which ends in `.rvt` - so the guard
    returned nothing and the model went out. The whole point of this
    function is that a Revit model NEVER LEAVES, and it failed open.

    Found by a review on 2026-09-15. Failing open is the direction that
    matters here: a name wrongly flagged costs somebody a sentence, and
    a model wrongly cleared is a client's building leaving the office.
    """
    if isinstance(names, str):
        names = [names]
    out = []
    for name in (names or []):
        low = str(name).strip().lower()
        for extension in NEVER_LEAVES:
            if low.endswith(extension):
                out.append((str(name), extension))
                break
    return out


def release(report, to=None, projects=None, policy=None):
    """
    {released, refused, why, unjudged} - a verdict, and nothing is sent.

    `projects` is which projects the report draws on; `policy` maps a
    project to what it allows.
    """
    if not isinstance(report, dict) or not str(
            report.get("content") or "").strip():
        return {"released": False, "refused": "NOT_A_REPORT",
                "why": "a report is a map carrying `content`, and "
                       "optionally `attachments`. A report nobody can read "
                       "cannot be checked before it leaves."}

    where = str(to or "").strip()
    if not where:
        return {"released": False, "refused": "NO_DESTINATION",
                "why": "no destination was named. Egress cannot be checked "
                       "against 'somewhere': every rule here is a rule "
                       "about where it is going, and a gate that passes an "
                       "unaddressed report is not a gate."}

    content = str(report["content"])
    drawn = sorted(set(str(each).strip() for each in (projects or [])
                       if str(each).strip()))
    rules = dict((str(name).strip().lower(), value)
                 for name, value in (policy or {}).items())

    # 1. A REVIT BINARY NEVER LEAVES - D-26's own left-hand column.
    carried = _binaries(report.get("attachments"))
    if carried:
        return {"released": False, "refused": "CARRIES_A_MODEL_FILE",
                "files": [name for name, _ext in carried],
                "why": "%s. D-26: the model file is never uploaded - the "
                       ".rvt and .rfa themselves, family and project "
                       "templates, any Revit binary. Partly confidentiality "
                       "and partly that it is hundreds of megabytes and no "
                       "useful answer needs it."
                       % "; ".join("'%s' is a %s" % (name, ext)
                                   for name, ext in carried)}

    # 2. A SECRET NEVER LEAVES - article 17, and the value is not repeated.
    _clean, shapes = SECRETS.Secrets().redact(content)
    if shapes:
        return {"released": False, "refused": "CARRIES_A_SECRET",
                "shapes": shapes,
                "why": "the report carries %s. Constitution article 17: "
                       "secrets live in the credential store, nowhere else, "
                       "and never in a result. Refused rather than redacted "
                       "- a report that had a credential in it is one "
                       "somebody should look at, not one to quietly clean "
                       "and send. The shape is named here and the value is "
                       "not."
                       % ", ".join("a %s" % shape for shape in shapes)}

    # 3. PROJECTS STAY SEGREGATED - D-26's third point.
    if len(drawn) > 1:
        return {"released": False, "refused": "MIXES_PROJECTS",
                "projects": drawn,
                "why": "the report draws on %d projects: %s. D-26: "
                       "'project-based knowledge must be kept segregated "
                       "and separated' - project A's knowledge does not "
                       "leak into project B, and a report is the easiest "
                       "way for it to."
                       % (len(drawn), ", ".join(drawn))}

    # 4. A PROJECT MAY FORBID IT - declared, never inferred.
    for project in drawn:
        allowed = rules.get(project.lower())
        if allowed is None:
            return {"released": False, "refused": "THE_PROJECT_FORBIDS_IT",
                    "project": project,
                    "why": "'%s' has no declared release policy, so nothing "
                           "says this may leave. Absence is not permission: "
                           "docs/12 s85 is about contracts that prohibit "
                           "egress, and a contract is something somebody "
                           "signed rather than something to assume from "
                           "silence." % project}
        if str(allowed).strip().lower() in ("no", "false", "never", "none"):
            return {"released": False, "refused": "THE_PROJECT_FORBIDS_IT",
                    "project": project, "policy": str(allowed),
                    "why": "'%s' declares '%s'. The project said so and "
                           "this agent does not weigh it against anything."
                           % (project, allowed)}

    return {
        "released": False, "may_release": True, "to": where,
        "projects": drawn, "travels": list(TRAVELS),
        "why": "nothing here stops it going to %s.%s Nothing was sent: this "
               "is a verdict, and a caller sends."
               % (where,
                  " Drawn from '%s', which declares '%s'."
                  % (drawn[0], rules.get(drawn[0].lower()))
                  if drawn else " No project was named as a source."),
        "unjudged": [
            "NOTHING WAS SENT AND NOTHING WAS STRIPPED. This is a verdict "
            "about whether a report may leave, not the leaving.",
            "PROJECT NAMES WERE NOT REMOVED, AND THAT IS D-26's RULE "
            "RATHER THAN AN OVERSIGHT. Its table puts project names, file "
            "names, content names, element data, engineering ideas, "
            "reasoning and code in the 'fine in the conversation' column. "
            "docs/28's row for this agent says 'strips project "
            "identifiers', which is the framing D-26 narrowed away from - "
            "PROPOSALS F20.",
            "WHAT WAS CHECKED IS FOUR THINGS, ALL REFUSALS: a Revit binary "
            "attached, a credential in the text, more than one project, "
            "and a project that has not declared this may leave. Nothing "
            "was cleaned so that it could pass.",
            "WHETHER THE REPORT SHOULD BE SENT AT ALL IS NOT JUDGED HERE. "
            "This says it MAY go to '%s'; whether it is the right thing to "
            "send, and to them, is a person's." % where,
        ],
    }


def main(argv):
    print("REPORT RELEASE   a report is an egress surface")
    print("=" * 72)

    print("\nD-26's two columns")
    print("  never leaves   %s" % ", ".join(NEVER_LEAVES))
    for each in TRAVELS:
        print("  travels        %s" % each)

    clean = {"content": "Tower A has 47 ducts over 300x300.\n"}
    ok = release(clean, to="client@example.com", projects=["Tower A"],
                 policy={"Tower A": "yes"})
    print("\n%s" % ok["why"])

    print("\nrefused")
    for report, args in (
            (clean, {"to": "", "projects": ["Tower A"]}),
            ({"content": "see attached", "attachments": ["Tower A.rvt"]},
             {"to": "x", "projects": ["Tower A"],
              "policy": {"Tower A": "yes"}}),
            ({"content": "token " + SECRETS.FAKE_FORGE_TOKEN},
             {"to": "x", "projects": ["Tower A"],
              "policy": {"Tower A": "yes"}}),
            (clean, {"to": "x", "projects": ["Tower A", "Tower B"],
                     "policy": {"Tower A": "yes", "Tower B": "yes"}}),
            (clean, {"to": "x", "projects": ["Tower A"]}),
            (clean, {"to": "x", "projects": ["Tower A"],
                     "policy": {"Tower A": "never"}})):
        answer = release(report, **args)
        print("  %-24s %s" % (answer["refused"], answer["why"][:46]))

    print("\nwhat this agent does not judge")
    for line in ok["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
