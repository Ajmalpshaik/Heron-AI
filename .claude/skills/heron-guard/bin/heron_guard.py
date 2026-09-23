#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
The adapter boundary, checked BEFORE the edit lands rather than after.

A PreToolUse hook, wired from .claude/settings.json so it runs in EVERY
session. It reads one proposed Write or Edit from stdin and refuses it if the
new text would put the Revit vendor namespace outside `revit/` - docs/16
section 4, the boundary that keeps the core testable without Revit.

WIRED FROM SETTINGS, NOT FROM THE SKILL - CORRECTED 2026-09-23
--------------------------------------------------------------
Until then the hook was declared in the heron-guard skill's own frontmatter,
which is where gstack keeps its guards. A hook declared there is registered
only when the skill is INVOKED, so in every session that never loaded
heron-guard this file never ran. Proven 2026-09-22 by hand: this script
refused a forbidden edit piped into it, and the same edit made through the
editor in a normal session went straight through. A guard that runs only
when somebody remembers to load it is the gate-somebody-has-to-remember this
file was written to replace, one level up.

So .claude/settings.json wires it now, and the frontmatter declares nothing:
the host runs a skill's copy of a hook SEPARATELY from the settings' copy, so
declaring it in both places would run it twice. tests/test_heron_guard.py
holds both halves.

EVERY DECISION IS WRITTEN DOWN, AND THE DIARY CANNOT CHANGE ONE
---------------------------------------------------------------
Each allow, deny, crash and switched-off edit appends one line to the hooks'
log outside this repository (.claude/skills/heron-session/bin/hook_log.py
says where, and why it is never a typed path), so tools/hook-report.py can
show from evidence that the guard runs in every session and how often it
refuses. The line is written AFTER the decision is printed, and a diary that
cannot be written is a missing line - never a refusal, and never a crash that
trap 2 below would have to turn into one.

WHY THIS EXISTS AND WHY IT IS NOT check-structure.py
-----------------------------------------------------
It IS check-structure.py's rule. The difference is WHEN.

`tools/check-structure.py` walks the whole repository and is run by a person.
That is the right shape for a full sweep and the wrong shape for a boundary,
because a gate somebody has to remember is a gate that is skipped on the day
it matters. On 2026-09-09 the author of this file broke that exact boundary
while writing a comment EXPLAINING it, and found out only because the gate
happened to be run afterwards. This makes the same rule answer at the moment
the edit is proposed.

The full sweep stays. This is not a replacement - it catches one file's
proposed content, and check-structure.py still catches everything else it
checks and every file that reached disk another way.

THE THREE TRAPS, FROM gstack's check-freeze.sh (docs/33 section 5.7)
--------------------------------------------------------------------
Read at c8f0c4e, MIT. The mechanism is re-authored here, not copied - D-25 -
and in Python rather than bash, because Heron is developed on Windows and a
bash hook would simply not run there.

  1. THE DECISION MUST BE NESTED. A `permissionDecision` at the top level is
     ignored, which "silently no-ops the block" - a hook that looks like it
     works and refuses nothing.

  2. A HOOK THAT DIES IS READ AS PERMISSION. Any unexpected exit with no
     decision on stdout is treated as non-blocking and the edit proceeds. So
     an unexpected failure here prints a DENY rather than dying quietly.

     AN ALLOW PRINTS NOTHING, AND THAT IS NOT THE SAME HOLE. Silence is what
     the host reads as "no objection", which is the outcome an allow wants -
     so the only thing the trap above costs is that a CRASH must not look
     like one. tests/test_heron_guard.py pins both halves: a crash prints a
     deny, and an edit this hook has nothing to say about is "allowed
     silently". This paragraph said "every path out of here prints a
     decision" until 2026-09-21, which the allow path has never done - a
     small thing to be wrong about in the one file whose subject is a hook
     that looks like it works and refuses nothing.

  3. POLARITY IS A DECISION, NOT A DEFAULT. This is deny-tier and fails
     CLOSED, because "a boundary that fails open is not a boundary". gstack's
     `careful` is ask-tier and fails the other way, deliberately.

AND A FOURTH, FOUND 2026-09-23: A CRASH THAT IS NOBODY'S FAULT
-------------------------------------------------------------
Trap 2 makes a crash refuse. So anything that crashes this hook for a reason
unrelated to the boundary refuses an edit that was fine - and on Windows a
piped stdin is decoded in the ANSI code page, which has no character for five
byte values UTF-8 uses constantly. An edit carrying Arabic was refused for
that, in every session. The payload is read as bytes and decoded as UTF-8
now (read_payload), and tests/test_heron_guard.py section 4a sends Arabic
under that code page.

AND ONE ESCAPE HATCH, WHICH IS NOT OPTIONAL FOR A FAIL-CLOSED HOOK
-------------------------------------------------------------------
`HERON_GUARD=off` disables it. A hook that fails closed and cannot be turned
off is one bad edit away from a repository nobody can work in, and the person
who needs the hatch is the person whose tooling is already broken. gstack ships
the same hatch for the same reason.

THIS IS FOR DEVELOPING HERON. IT IS NOT PART OF WHAT A MODELLER INSTALLS.
--------------------------------------------------------------------------
Hooks are the HOST's mechanism and D-01 gives the host orchestration. Nothing
here reaches a Revit model, a fragment, or a user of the add-in. Somebody
typing "select all ducts" never meets this file.
"""

import json
import os
import re
import sys

# THE PATTERN IS A SECOND COPY, AND A TEST PROVES THE TWO AGREE.
#
# tools/check-structure.py owns this rule and cannot be imported: it is a
# script that runs its whole sweep at import time. So the pattern is restated
# here and tests/test_heron_guard.py asserts it is character-for-character the
# one check-structure.py uses. Two copies with a test between them is this
# repository's existing answer to exactly this problem - the same shape
# tests/test_fragment_imports.py uses for the executor's import list.
#
# It is BUILT FROM PARTS rather than written out, because check-structure.py
# greps file TEXT and would fail this very file for containing the string it
# exists to forbid. That is not hypothetical: the first draft of a docstring
# in brain/heron_context.py did it on 2026-09-09 and failed the build.
_VENDOR = "Autodesk" + "." + "Revit"
REVIT_API = re.compile(r"\b" + _VENDOR.replace(".", r"\.") + r"\b")

# Where the vendor namespace is allowed. check-structure.py exempts `revit/`
# because that is the adapter, and `tools/` because a checker has to be able
# to name what it checks. Same two, for the same two reasons.
ALLOWED = ("revit", "tools")


def decision(verdict, reason):
    """The only thing this file ever prints. Trap 1: nested, or ignored."""
    return {
        "hookSpecificOutput": {
            "hookEventName": "PreToolUse",
            "permissionDecision": verdict,
            "permissionDecisionReason": reason,
        }
    }


def proposed_text(tool_input):
    """The text this edit would put in the file, whatever tool is doing it.

    Write carries `content`. Edit carries `new_string`. MultiEdit carries a
    list of edits. A tool this does not recognise contributes no text and is
    allowed - it is not this hook's business, and guessing would be.
    """
    parts = []
    for key in ("content", "new_string"):
        got = tool_input.get(key)
        if isinstance(got, str):
            parts.append(got)
    for edit in tool_input.get("edits") or []:
        if isinstance(edit, dict) and isinstance(edit.get("new_string"), str):
            parts.append(edit["new_string"])
    return "\n".join(parts)


def top_folder(file_path, root):
    """The repository part this path belongs to, or None if it is outside.

    A path outside the repository is not this hook's business. Compared with
    os.path.commonpath rather than startswith, so `/home/user/Heron-AI-other`
    is not mistaken for a file inside `/home/user/Heron-AI`.
    """
    try:
        full = os.path.abspath(file_path)
        if os.path.commonpath([full, root]) != root:
            return None
        rel = os.path.relpath(full, root)
    except (ValueError, TypeError):
        # Different drives on Windows, or a path that is not a string. Either
        # way it is not inside this repository.
        return None
    first = rel.replace("\\", "/").split("/")[0]
    return first or None


def check(payload, root):
    """(verdict, reason). The whole rule, so a test can call it directly."""
    tool_input = payload.get("tool_input") or {}
    file_path = tool_input.get("file_path")
    if not isinstance(file_path, str) or not file_path:
        # Not a file-editing tool. Nothing to say about it.
        return "allow", ""

    part = top_folder(file_path, root)
    if part is None or part in ALLOWED:
        return "allow", ""

    text = proposed_text(tool_input)
    if not text or not REVIT_API.search(text):
        return "allow", ""

    return "deny", (
        "This edit would put the Revit vendor namespace in %s, and it may "
        "appear only inside revit/ or tools/ - docs/16 section 4, the adapter "
        "boundary that keeps the core testable without Revit.\n\n"
        "A COMMENT COUNTS. tools/check-structure.py greps the file's text, so "
        "explaining the rule by quoting it fails the same way writing the code "
        "would. Name the namespace indirectly, or move the code into revit/.\n\n"
        "If this is wrong, HERON_GUARD=off disables this hook." % part
    )


def read_payload():
    """The host's JSON, read as UTF-8 whatever this console's code page is.

    The host writes UTF-8. On Windows a piped stdin is decoded in the ANSI
    code page instead, and cp1252 has no character for five byte values UTF-8
    uses all the time - Arabic among them - so reading text would crash on
    them. Reading the bytes and decoding them here cannot.
    """
    stream = getattr(sys.stdin, "buffer", sys.stdin)
    raw = stream.read()
    if isinstance(raw, bytes):
        raw = raw.decode("utf-8", "replace")
    return json.loads(raw) if raw.strip() else {}


def diary(verdict, said, session):
    """One line in the hooks' log. Never raises, and never changes a decision.

    Called only after the decision has been printed. Everything here is
    inside one guard, because a failure to write a diary line is not a reason
    for trap 2 to refuse an edit.
    """
    try:
        sys.path.insert(0, os.path.join(
            os.path.dirname(os.path.abspath(__file__)),
            "..", "..", "heron-session", "bin"))
        import hook_log
        hook_log.record("heron-guard", verdict, said, session)
    except Exception:                             # noqa: BLE001 - a diary, not a gate
        pass


def main():
    if os.environ.get("HERON_GUARD", "").lower() in ("off", "0", "false"):
        diary("off", "HERON_GUARD is set to switch the guard off", "")
        return 0

    root = os.path.abspath(
        os.path.join(os.path.dirname(os.path.abspath(__file__)),
                     "..", "..", "..", ".."))
    session = ""
    try:
        payload = read_payload()
        if isinstance(payload, dict):
            session = payload.get("session_id") or ""
        verdict, reason = check(payload, root)
    except Exception as exc:                      # noqa: BLE001 - trap 2
        # TRAP 2: an unexpected death with nothing on stdout is read as
        # PERMISSION, and this hook is deny-tier. So a failure here refuses
        # the edit and says how to get moving again, rather than quietly
        # becoming an allow.
        reason = ("The Heron boundary hook failed (%s: %s) and it is "
                  "deny-tier, so it refuses rather than letting an unchecked "
                  "edit through. Set HERON_GUARD=off to disable it, or fix "
                  ".claude/skills/heron-guard/bin/heron_guard.py."
                  % (type(exc).__name__, exc))
        print(json.dumps(decision("deny", reason)))
        sys.stdout.flush()
        diary("crash", reason, session)
        return 0

    if verdict == "deny":
        print(json.dumps(decision("deny", reason)))
        sys.stdout.flush()
    diary(verdict, reason, session)
    return 0


if __name__ == "__main__":
    sys.exit(main())
