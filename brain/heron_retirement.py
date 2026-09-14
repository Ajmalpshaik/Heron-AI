# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-RET-010
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Agent Retirement - archive, never delete, and only when nothing still needs it.

    python brain/heron_retirement.py HERON-KRN-EVT-004

WHAT THE REGISTER ASKS FOR
---------------------------
"Retires with history preserved and rollback possible. ARCHIVE, NEVER DELETE."

Three requirements in eleven words, and each is a rule this module enforces
rather than repeats.

ARCHIVE, NEVER DELETE
----------------------
This module removes nothing. No unlink, no rmtree, no rm, and the test asserts
that against the source text rather than against behaviour - because the day
somebody adds one, the behaviour test would still pass on every case anybody
thought to write.

The reason is not sentiment. A deleted agent takes its history with it: what it
was for, what it got wrong, and why somebody decided it should stop. The next
person to want that job done then rebuilds it, including the mistake.

ROLLBACK POSSIBLE, SO THE RECORD IS CHECKED
--------------------------------------------
"Rollback possible" is only true if the record holds enough to bring the agent
back - its files, its contract and version, the stage it left. So a retirement
whose record would be incomplete is REFUSED. A retirement that cannot be undone
is a deletion with better manners.

STILL REFERENCED IS A REFUSAL, NOT A WARNING
---------------------------------------------
If another file still names this agent, retiring it quietly breaks whatever
that file was doing. Name a successor and the record carries it, so the thing
that pointed here has somewhere to be pointed.

The one file exempt from that check is docs/28-agent-registry.md itself. The
register keeps retired agents - that is what a register is - and treating its
row as a live reference would make every retirement impossible.

WHO MAY SIGN
-------------
The same rule as deployment, for the same reason: an approver that looks like a
Heron agent id is refused. Golden Rule 7 is not satisfied by another agent
signing.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

RETIRED_STAGES = ("DEPRECATED", "ARCHIVED")
AGENT_ID = "HERON-"

# Where a reference would matter. The register is deliberately not here: it
# keeps retired agents, and counting its row as a live reference would make
# every retirement impossible.
SEARCHED = ("brain", "mcp", "platform", "tools", "tests")
SKIP_DIRS = {"__pycache__", "bin", "obj", ".git", "node_modules"}


def references(agent_id, own_files=()):
    """Every file that names this agent and is not one of its own."""
    own = set(own_files)
    found = []
    for part in SEARCHED:
        root = os.path.join(ROOT, part)
        if not os.path.isdir(root):
            continue
        for dirpath, dirnames, filenames in os.walk(root):
            dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]
            for name in filenames:
                if not name.endswith((".py", ".cs", ".ps1", ".yaml", ".yml")):
                    continue
                path = os.path.join(dirpath, name)
                relative = os.path.relpath(path, ROOT).replace(os.sep, "/")
                if relative in own:
                    continue
                try:
                    text = io.open(path, encoding="utf-8",
                                   errors="replace").read()
                except IOError:                              # pragma: no cover
                    continue
                if agent_id in text:
                    found.append(relative)
    return sorted(found)


def retire(agent_id, to_stage, reason=None, approved_by=None, successor=None,
           record_of=None, known_agents=None):
    """
    {retired, record, references} or a refusal. It deletes nothing, ever.

    `record_of` is the agent's record from the Agent Registry - passed in so
    this module never has to assemble one, and so a caller cannot retire an
    agent it never looked up.
    """
    to_stage = (to_stage or "").upper()

    if to_stage not in RETIRED_STAGES:
        return {"retired": False, "refused": "UNKNOWN_STAGE",
                "why": "'%s' is not a retirement stage. docs/24 has two: "
                       "DEPRECATED, superseded or unreliable, and ARCHIVED, "
                       "retained for history." % to_stage}

    if not (reason or "").strip():
        return {"retired": False, "refused": "NO_REASON_GIVEN",
                "why": "a retirement with no reason is a gap in the history "
                       "it claims to preserve. Say why, for whoever reads it "
                       "in a year."}

    if not approved_by:
        return {"retired": False, "refused": "NEEDS_HUMAN_APPROVAL",
                "why": "retiring an agent is an ADMIN act and nothing signed "
                       "it. Nothing has been changed."}

    if str(approved_by).upper().startswith(AGENT_ID):
        return {"retired": False, "refused": "MACHINE_MAY_NOT_SIGN",
                "why": "'%s' is a Heron agent. Golden Rule 7 is not satisfied "
                       "by another agent signing." % approved_by}

    if not record_of:
        return {"retired": False, "refused": "NOTHING_TO_RETIRE",
                "why": "no record of %s was given, so nothing could be kept - "
                       "and a retirement that cannot be undone is a deletion "
                       "with better manners." % agent_id}

    # THE RECORD MUST BE THIS AGENT'S. Nothing checked, so a record for a
    # different agent was archived under the requested one's name: an audit
    # entry naming one agent while preserving another, and useless for the
    # rollback it exists to make possible.
    if str(record_of.get("id") or "").strip().upper() \
            != str(agent_id).strip().upper():
        return {"retired": False, "refused": "NOTHING_TO_RETIRE",
                "why": "the record given is for %s and the retirement asked "
                       "for is %s. Archiving one under the other's name "
                       "produces a record that cannot roll either back."
                       % (record_of.get("id"), agent_id)}

    files = record_of.get("files") or []
    pointing = references(agent_id, files)
    # A SUCCESSOR IS AN AGENT, NOT A STRING. Any truthy value used to open
    # the gate, so a typo or a placeholder retired a referenced agent with
    # nothing actually taking its work - which is the one outcome this gate
    # exists to prevent.
    if successor:
        if str(successor).strip().upper() == str(agent_id).strip().upper():
            return {"retired": False, "refused": "STILL_REFERENCED",
                    "references": references(agent_id,
                                             record_of.get("files") or []),
                    "why": "%s cannot succeed itself." % agent_id}
        if known_agents is None:
            import heron_agents as REG
            known_agents, _claims, _host = REG._agent_count()
        if successor not in known_agents:
            return {"retired": False, "refused": "STILL_REFERENCED",
                    "references": references(agent_id,
                                             record_of.get("files") or []),
                    "why": "'%s' is not in docs/28-agent-registry.md, so "
                           "nothing would actually take this agent's work. A "
                           "successor that does not exist is a typo holding a "
                           "gate open." % successor}

    if pointing and not successor:
        return {"retired": False, "refused": "STILL_REFERENCED",
                "references": pointing,
                "why": "%d file(s) still name %s, and retiring it quietly "
                       "would break whatever they were doing: %s. Name a "
                       "successor."
                       % (len(pointing), agent_id, ", ".join(pointing[:4]))}

    # Rollback is only possible if what comes back is described. Checked here
    # rather than trusted, because "archive" that cannot be reversed is the
    # deletion this agent exists to refuse.
    missing = [field for field in ("id", "name", "department", "files")
               if not record_of.get(field)]
    if missing:
        return {"retired": False, "refused": "NOTHING_TO_RETIRE",
                "why": "the record is missing %s, so this could not be "
                       "brought back. Rollback possible is a requirement, not "
                       "a hope." % ", ".join(missing)}

    return {
        "retired": True,
        "references": pointing,
        "why": "%s moved to %s, kept in full%s"
               % (agent_id, to_stage,
                  ", successor %s" % successor if successor else ""),
        "record": {
            "agent": agent_id,
            "stage": to_stage,
            "reason": reason,
            "approved_by": approved_by,
            "successor": successor,
            "kept": {
                "name": record_of.get("name"),
                "role": record_of.get("role"),
                "department": record_of.get("department"),
                "tier": record_of.get("tier"),
                "files": list(files),
                "contract": record_of.get("contract"),
                "contract_version": record_of.get("contract_version"),
                "state_when_retired": record_of.get("state"),
            },
            "still_referenced_by": pointing,
            "deleted": False,
        },
    }


def main(argv):
    import heron_agents as REG

    agent = argv[1].upper() if len(argv) > 1 else "HERON-KRN-EVT-004"
    found = REG.record(agent)
    if not found:
        print("NO_SUCH_AGENT: '%s' is not in the register." % agent)
        return 1

    print("AGENT RETIREMENT   archive, never delete")
    print("=" * 68)
    for label, kw in (
            ("no reason", dict(reason="", approved_by="the owner")),
            ("nobody signed", dict(reason="superseded", approved_by=None)),
            ("a machine signed", dict(reason="superseded",
                                      approved_by="HERON-AHR-CRT-006")),
            ("no successor", dict(reason="superseded",
                                  approved_by="the owner")),
            ("with a successor", dict(reason="superseded",
                                      approved_by="the owner",
                                      successor="HERON-KRN-WFL-007"))):
        answer = retire(agent, "DEPRECATED", record_of=found, **kw)
        print("  %-18s %-22s %s"
              % (label,
                 "retired" if answer["retired"] else answer["refused"],
                 answer["why"][:60]))

    answer = retire(agent, "DEPRECATED", reason="superseded",
                    approved_by="the owner", successor="HERON-KRN-WFL-007",
                    record_of=found)
    print()
    print("  what is kept, so it can come back:")
    for key, value in sorted(answer["record"]["kept"].items()):
        print("    %-20s %s" % (key, value))
    print("    %-20s %s" % ("deleted", answer["record"]["deleted"]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
