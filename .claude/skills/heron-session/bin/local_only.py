#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
One line before a prompt that needs Revit on the owner's PC, when the session
is somewhere that has none.

A UserPromptSubmit hook, wired from .claude/settings.json so it runs before
every prompt. When the prompt is MARKED LOCAL and the session runs in a CLOUD
CONTAINER, it adds one line of context:

    This prompt is marked LOCAL (it needs Revit on the owner's PC) and this is
    a cloud container with no Revit. Say so in one line and stop. Tell the
    owner to start it on the Revit PC.

Every other prompt it lets pass without a word.

WHY
---
On 2026-10-17 a suggested task that began "LOCAL SESSION - run this on the
Windows PC with Revit 2024" was started as a cloud session. It had no Revit,
the heron tools did not connect, and it spent a whole turn finding out it
could do nothing. The session-start line (session_line.py) already says "Not
Windows" - but it cannot see the prompt, and a SessionStart hook never can.
UserPromptSubmit is the one event that sees both the prompt and the machine.

WHAT MARKS A PROMPT LOCAL
-------------------------
Narrow on purpose, so a prompt that only mentions Revit does not fire it. In
the prompt's FIRST PARAGRAPH - every line before the first blank one:

  * a line that BEGINS "LOCAL SESSION", in capitals;
  * a prompt that begins "Local:";
  * a line that begins by saying to run it on the Windows PC - "Run this on
    the Windows PC", "This must run on the Windows PC".

"Begins" allows the markdown around it - a heading's #, a quote's >, a list's
-, bold's ** - and nothing else. A marker in the MIDDLE of a sentence is a
mention, not a mark: the prompt that asked for this hook quotes "LOCAL SESSION
- run this on the Windows PC" mid-sentence, and a plain "contains" would have
stopped the very task that built it.

The first paragraph is where a prompt says where it runs - the 2026-10-17
prompt did it in its first line, and so did the one that asked for this hook.
Further down, a "## LOCAL SESSION (later)" section or a "- Run this on the
Windows PC afterwards" step belongs to a cloud task that hands work on, and
warning there would stop the task this hook exists to save. A mark missed is
cheaper than a mark invented: the session-start line still says Not Windows.

WHAT MAKES A SESSION A CLOUD CONTAINER
--------------------------------------
Asked in this order, and the first that answers wins:

  1. CLAUDE_CODE_REMOTE is "true" - what Claude Code documents: "the session
     VM's environment carries that variable as true, it's never true locally"
     (code.claude.com/docs/en/cloud-environments, read 2026-10-17).
  2. CLAUDE_CODE_REMOTE_SDK_URL and CCR_AGENT_PROXY_ENABLED are both set -
     OBSERVED in a cloud container on 2026-10-17, on Linux, and documented
     nowhere. Kept for a release that stops setting the first.
  3. Not Windows - the FALLBACK. The heron MCP server exits on any system but
     Windows (session_line.py makes the same os.name test), so there is no
     Revit here either way. The line then says "this machine is not Windows"
     rather than claim a cloud it cannot see.

A cloud session on Windows still warns: LOCAL means the owner's PC, and a
remote runner is not that, whatever it has installed.

ADVICE, NEVER A GATE
--------------------
It never decides. The output carries no "decision" at all, so the prompt
reaches the AI exactly as typed, with the line beside it. The AI is the one
that says it to the person, in one line - the line is written to the AI.

Every failure is silence and exit 0: a payload that will not parse, a prompt
that is not text, anything at all. A hook that fails a prompt is a hook that
gets switched off.

THE DIARY
---------
A marked prompt is one line in the hooks' diary (hook_log.py): warned-cloud,
warned-not-windows, or local-here when it is marked and this is the owner's
PC. An unmarked prompt decided nothing and writes nothing. The PROMPT'S OWN
WORDS are never written - only what this hook said.

WINDOWS
-------
Run as `python "$CLAUDE_PROJECT_DIR/..."`, which Git Bash expands on Windows
the same way bash does on Linux. The payload is read as bytes and decoded as
UTF-8, and the output is JSON with every non-ASCII character escaped, for the
code-page trap the heron-ship skill describes.

THIS IS FOR DEVELOPING HERON. IT IS NOT PART OF WHAT A MODELLER INSTALLS.
"""

import json
import os
import re
import sys

HOOK = "heron-local-only"

CLOUD_SAID = ("This prompt is marked LOCAL (it needs Revit on the owner's PC) "
              "and this is a cloud container with no Revit. Say so in one line "
              "and stop. Tell the owner to start it on the Revit PC.")
NOT_WINDOWS_SAID = ("This prompt is marked LOCAL (it needs Revit on the owner's "
                    "PC) and this machine is not Windows, so it has no Revit. "
                    "Say so in one line and stop. Tell the owner to start it on "
                    "the Revit PC.")

# Where a line begins: any markdown in front of the words, and nothing else.
_LEAD = r"^[ \t#>*_\-\[(]*"
LOCAL_SESSION = re.compile(_LEAD + r"LOCAL SESSION\b", re.M)
LOCAL_COLON = re.compile(r"\A\s*[#>*_\-\[(]*\s*Local:", re.I)
WINDOWS_PC = re.compile(
    _LEAD + r"(?:(?:this|it)\s+)?(?:(?:must|has to|needs to)\s+)?(?:be\s+)?"
    r"run\s+(?:(?:this|it)\s+)?on\s+the\s+(?:owner's\s+)?Windows\s+PC\b",
    re.M | re.I)


def head_of(prompt):
    """The prompt's first paragraph: every line before the first blank one."""
    head = []
    for line in prompt.strip().splitlines():
        if not line.strip():
            break
        head.append(line)
    return "\n".join(head)


def marked_local(prompt):
    """True when the prompt's first paragraph says it must run on the
    owner's PC."""
    if not isinstance(prompt, str):
        return False
    head = head_of(prompt)
    return bool(LOCAL_COLON.search(head) or LOCAL_SESSION.search(head)
                or WINDOWS_PC.search(head))


def machine_of(env, system):
    """'cloud', 'not-windows' or None - None is the owner's PC, or may be."""
    if str(env.get("CLAUDE_CODE_REMOTE") or "").strip().lower() == "true":
        return "cloud"
    if env.get("CLAUDE_CODE_REMOTE_SDK_URL") and env.get("CCR_AGENT_PROXY_ENABLED"):
        return "cloud"
    if system != "nt":
        return "not-windows"
    return None


def advice_for(payload, env=None, system=None):
    """(decision, text) - decision is None for a prompt not marked LOCAL, and
    text is None when there is nothing to say. `env` and `system` are the
    environment and os.name to answer for; None is this process's."""
    if not isinstance(payload, dict) or not marked_local(payload.get("prompt")):
        return None, None
    machine = machine_of(os.environ if env is None else env,
                         os.name if system is None else system)
    if machine == "cloud":
        return "warned-cloud", CLOUD_SAID
    if machine == "not-windows":
        return "warned-not-windows", NOT_WINDOWS_SAID
    return "local-here", None


def diary(decision, said, session):
    """One line in the hooks' log (hook_log.py). Never raises."""
    try:
        sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
        import hook_log
        hook_log.record(HOOK, decision, said, session)
    except Exception:                               # noqa: BLE001 - a diary, not a gate
        pass


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


def main():
    session, decision, said, text = "", None, "", None
    try:
        payload = read_payload()
        if not isinstance(payload, dict):
            return 0
        session = payload.get("session_id") or ""
        decision, text = advice_for(payload)
        if decision is None:
            # Not marked LOCAL: nearly every prompt. No output, and no diary
            # line - it decided nothing.
            return 0
        said = text or ""
    except Exception as exc:                        # noqa: BLE001 - advice only
        decision, said, text = "error", type(exc).__name__, None
    if text:
        print(json.dumps({
            "hookSpecificOutput": {"hookEventName": "UserPromptSubmit",
                                   "additionalContext": text},
        }, ensure_ascii=True))
        sys.stdout.flush()
    diary(decision, said, session)
    return 0


if __name__ == "__main__":
    sys.exit(main())
