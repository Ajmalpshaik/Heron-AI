# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The read-only door: `revit_read` runs what reads, and nothing else.

    python tests/test_read_door.py

WHAT WAS MISSING
----------------
The add-in has had `run_fragment_read` since D-28 - declared ANALYZE, opening
no transaction, so Revit itself refuses any change - and only the command
line ever sent it. A chat could reach a READ or ANALYZE fragment through
`revit_change` alone: with Changes switched ON, inside the executor that opens
a transaction. A question about the model needed write permission. And the
server's own text told every chat "Heron can now RUN a read-only fragment",
which was true of the command line and of no chat.

`revit_read` is the door. This suite holds it to these, with no Revit and no
MCP SDK, so CI runs it:

  1. ITS RISK IS ITS OPERATION'S. The registry row says ANALYZE and
     `run_fragment_read`, and the add-in declares that operation ANALYZE -
     read out of the C#, so the two cannot drift apart quietly.
  2. ITS CEILING, OVER THE WHOLE LIBRARY. Every fragment declared READ or
     ANALYZE may go through; every other one is refused, by a sentence that
     says nothing was sent; an unreadable risk is refused too. Counted over
     the real fragments, never a sample.
  3. ITS WIRING, READ AS TEXT. It refuses BEFORE it binds a session or reads
     any code; it sends run_fragment_read and never the write operation or
     `apply`; it aims at the pinned model through the same helper
     revit_change uses; it classifies a failure as a READ; it prints the
     proof status; and the server no longer says reads run without naming
     the door.
  4. THE AUDITOR HAS IT. heron-model-auditor's tool list names the door, and
     its instructions on both hosts say when to use it.
  5. ITS PROOF STATUS IS READ FROM THE FILES. A PROVEN fragment reads as
     PROVEN with its model; a DRAFT one as DRAFT; a folder that is not there
     as "could not be read" - never as either.

WHAT IT CANNOT DO
-----------------
Call the tool. The server imports the MCP SDK, which CI does not install, so
the tool's behaviour through a real SDK - with a stand-in Revit - is checked
in tests/test_mcp_serves.py, where the SDK is. Whether a read answers
correctly against a real model is a proof, and a proof needs Revit.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
REGISTRY_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronOperationRegistry.cs")
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
AUDITOR_MD = os.path.join(ROOT, ".claude", "agents", "heron-model-auditor.md")
AUDITOR_TOML = os.path.join(ROOT, ".codex", "agents", "heron-model-auditor.toml")

sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_tools as TOOLS                                     # noqa: E402
import heron_bridge_client as CLIENT                            # noqa: E402
import heron_brain as BRAIN                                     # noqa: E402

DOOR = "revit_read"
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    with io.open(path, encoding="utf-8") as handle:
        return handle.read()


def card_line(card, key):
    """One top-level `key:` value of a fragment.yaml, or None."""
    with io.open(card, encoding="utf-8", errors="replace") as fh:
        for line in fh:
            if line.startswith(key + ":"):
                return line.split(":", 1)[1].strip().strip("\"'")
    return None


def body_of(text, name):
    """One function's source, from its def to the next top-level def."""
    start = text.find("\ndef %s(" % name)
    if start < 0:
        return None
    rest = text[start + 1:]
    end = re.search(r"\n(?:@server\.tool\(\)\n)?def ", rest)
    return rest[:end.start()] if end else rest


def main():
    # ASK BEFORE CALLING - heron-ship 2a. On the code as it stood these names
    # do not exist, and a traceback would replace every failure below.
    door_refusal = getattr(TOOLS, "door_refusal", None)
    proof_status = getattr(BRAIN, "proof_status", None)

    print("1. Its risk is its operation's")
    declared = TOOLS.TOOLS.get(DOOR)
    check(declared is not None, "%s is declared in heron_tools.TOOLS" % DOOR)
    check(declared == (TOOLS.ANALYZE, "run_fragment_read"),
          "as ANALYZE, calling run_fragment_read - found %r" % (declared,))
    check(declared is not None and not TOOLS.writes(DOOR),
          "and so it does not count as a write")
    cs = dict(re.findall(r'\{\s*"([A-Za-z_]+)"\s*,\s*HeronRisk\.([A-Za-z]+)\s*\}',
                         read(REGISTRY_CS)))
    check(cs.get("run_fragment_read") == "Analyze",
          "and the add-in declares run_fragment_read Analyze - below its "
          "read-only ceiling, so it runs with Changes OFF")

    print()
    print("2. Its ceiling, over every fragment in the library")
    check(door_refusal is not None, "heron_tools has a door_refusal to ask")
    allowed, refused, unreadable = [], [], []
    for name in sorted(os.listdir(FRAGMENTS)):
        card = os.path.join(FRAGMENTS, name, "fragment.yaml")
        if not os.path.isfile(card):
            continue
        risk = CLIENT.fragment_risk(card)
        if door_refusal is None:
            continue
        said = door_refusal(DOOR, risk, card_line(card, "capability") or name)
        if risk is None:
            unreadable.append((name, said))
        elif risk in ("READ", "ANALYZE"):
            allowed.append((name, said))
        else:
            refused.append((name, risk, said))

    # Neither half may be empty, or the two checks after it pass on nothing.
    check(len(allowed) > 100,
          "the library has READ and ANALYZE fragments to let through - %d"
          % len(allowed))
    check(len(refused) > 100,
          "and fragments above ANALYZE to refuse - %d" % len(refused))
    check(all(said is None for _, said in allowed),
          "every READ and ANALYZE fragment may go through - refused: %r"
          % [n for n, s in allowed if s is not None][:5])
    check(all(said for _, _, said in refused),
          "every fragment above ANALYZE is refused - let through: %r"
          % [n for n, _, s in refused if not s][:5])
    kinds = sorted(set(risk for _, risk, _ in refused))
    check("EXECUTE" in kinds and "MODIFY" in kinds,
          "and the refused include EXECUTE as well as MODIFY - a selection "
          "change needs no transaction, so Revit would not stop it (%s)"
          % ", ".join(kinds))
    check(all("Nothing has been sent to Revit" in said
              for _, _, said in refused if said),
          "and every refusal says nothing was sent")
    check(unreadable == [],
          "every fragment declares a risk the client can read - unreadable: %r"
          % [n for n, _ in unreadable][:5])
    if door_refusal is not None:
        check(door_refusal(DOOR, None, "X") is not None
              and door_refusal(DOOR, "banana", "X") is not None,
              "an unreadable or unknown risk is REFUSED, never read as READ")
        check(door_refusal(DOOR, "analyze", "X") is None,
              "and a risk word is read without regard to case")

    proven = [n for n, _ in allowed
              if (card_line(os.path.join(FRAGMENTS, n, "fragment.yaml"),
                            "heron-status") or "").upper() == "PROVEN"]
    print("        (%d of the %d that may go through are PROVEN - derived, "
          "not typed)" % (len(proven), len(allowed)))

    print()
    print("3. Its wiring, read as text")
    text = read(SERVER)
    body = body_of(text, DOOR)
    check(body is not None, "%s is a function in the server" % DOOR)
    body = body or ""
    check(("@server.tool()\ndef %s(" % DOOR) in text,
          "and it is registered as a tool")

    # EVERY POSITION IS CHECKED FOR PRESENCE BEFORE IT IS COMPARED - `find`
    # returns -1 for a string that is not there, and `a < b` then passes
    # loudest when the guard it orders has been deleted (heron-ship 2a).
    ceiling = body.find("tools.door_refusal(")
    bind = body.find("binding.resolve(")
    source = body.find("fh.read()")
    check(ceiling > 0, "it ASKS the registry for its ceiling rather than "
                       "carrying a list of risks")
    check(bind > 0 and source > 0,
          "the session binding and the source read are where this suite "
          "looks for them")
    check(ceiling > 0 and bind > 0 and ceiling < bind,
          "it refuses BEFORE binding a session - a refusal touches nothing")
    check(ceiling > 0 and source > 0 and ceiling < source,
          "and before reading the fragment's code")
    check("bridge.fragment_risk(" in body,
          "and the fragment's risk is read by the client's one reader of it")
    # CODE, NOT COMMENTS: the body explains in words why it never sends
    # `apply`, and a comment naming the flag is not the flag being sent.
    code = "\n".join(line for line in body.splitlines()
                     if not line.lstrip().startswith("#"))
    check('"run_fragment_read"' in code and "run_fragment_write" not in code,
          "it sends run_fragment_read, and never the write operation")
    check('"apply"' not in code,
          "and never `apply` - there is no transaction to keep")
    check("_aim_at_pin(args)" in body,
          "it aims at the pinned model")
    change = body_of(text, "revit_change") or ""
    check("_aim_at_pin(args)" in change,
          "through the SAME helper revit_change uses - one copy of the aim")
    aim = body_of(text, "_aim_at_pin") or ""
    check(all(line in aim for line in ('args["expectProject"] = pinned.project_key or ""',
                                        'args["document"] = pinned.title',
                                        'args["documentPath"] = pinned.document_path')),
          "and that helper carries all three identities")
    check('writes=tools.writes("revit_read")' in body,
          "a failure is classified by the registry's answer for THIS tool - "
          "a lost reply to a read is safe to ask again")
    check("_proof_line(capability, folder)" in body,
          "it prints how far the capability is proven")
    check("Nothing in the model was changed" in body,
          "and says, every time, that nothing was changed")

    print()
    print("4. The server no longer says reads run without naming the door")
    live = [line for line in text.splitlines()
            if "Heron can now RUN a read-only fragment" in line
            and not line.lstrip().startswith("#")]
    check(not live,
          "the sentence that told every chat reads could run is gone from "
          "live code")
    said = body_of(text, "_cannot_run") or ""
    check("revit_read" in said,
          "and _cannot_run names the door a chat reads through")
    check("WRITES" in said and "revit_change" in said,
          "while still saying a fragment that WRITES goes through "
          "revit_change")

    print()
    print("5. The auditor has the door, on both hosts")
    md = read(AUDITOR_MD)
    head = md.split("---", 2)[1] if md.startswith("---") else ""
    tools_line = re.search(r"^tools:\s*(.+)$", head, re.M)
    listed = [t.strip() for t in tools_line.group(1).split(",")] if tools_line else []
    check("mcp__heron__revit_read" in listed,
          "heron-model-auditor's tool list names mcp__heron__revit_read")
    for host, path in ((".claude", AUDITOR_MD), (".codex", AUDITOR_TOML)):
        check("`revit_read`" in read(path),
              "and its %s instructions say when to use it" % host)

    print()
    print("6. The proof status is read from the files")
    check(proof_status is not None, "heron_brain has a proof_status to ask")
    if proof_status is not None:
        pick = {}
        for name in sorted(os.listdir(FRAGMENTS)):
            card = os.path.join(FRAGMENTS, name, "fragment.yaml")
            if not os.path.isfile(card) or card_line(card, "risk") not in ("READ", "ANALYZE"):
                continue
            state = (card_line(card, "heron-status") or "").upper()
            pick.setdefault(state, name)
        good = proof_status(pick.get("PROVEN", ""))
        check(good is not None and good["status"] == "PROVEN" and good["model"],
              "a PROVEN read (%s) reads as PROVEN, with the model it was "
              "proved on" % pick.get("PROVEN"))
        draft = proof_status(pick.get("DRAFT", ""))
        check(draft is not None and draft["status"] == "DRAFT"
              and not draft["stale"],
              "a DRAFT read (%s) reads as DRAFT - and not as stale, which "
              "only a proof can be" % pick.get("DRAFT"))
        check(proof_status("no-such-fragment-here") is None,
              "and a folder that is not there is None - never PROVEN, "
              "never DRAFT")

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for one in FAILURES:
            print("  - %s" % one)
        return 1
    print("PASSED - revit_read runs what reads, refuses the rest before "
          "anything is sent, and says how far what it ran is proven.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
