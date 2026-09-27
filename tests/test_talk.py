#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Heron Talk's listening half, with no Revit, no SDK and no .NET (D-104).

    python tests/test_talk.py

WHAT IT PROVES
  1. Talk is OFF unless a chat is started for it, and the one way to start
     one - mcp/heron-talk.cmd, through .mcp.json - turns it on.
  2. A message is claimed ONCE. Two chats racing for one message cannot both
     take it, and a message still being written, or already taken, is
     nobody's.
  3. What the chat receives puts the modeller's words FIRST AND UNCHANGED,
     then says where they came from and what was selected - and a message in
     a format this server does not read is said to be unreadable rather than
     guessed at.
  4. A message is taken only from a Revit whose bridge is live.
  5. heron_selection answers from the saved list, a page at a time, says
     when the list was cut short or an element could not be read, finds a
     number in whichever Revit saved it, and asks rather than guesses when
     two Revits could be meant.
  6. The heartbeat the Talk button looks for is written and removed.
  7. The server wires all of it: the tool is declared READ with no bridge
     operation, the instructions grow only when Talk is on, and the server
     listens only when Talk is on.

WHAT IT CANNOT DO
  It never serves the channel through a real MCP SDK - that is
  tests/test_talk_served.py - and it never reads what the C# add-in actually
  writes - that is tests/test_talk_contract.py. Neither Revit nor Claude Code
  is here: NEEDS-CHECKING group AS is the proof on the owner's PC.
"""

import io
import json
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_talk as talk                      # noqa: E402
import heron_tools as tools                    # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write(path, record):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with io.open(path, "w", encoding="utf-8") as handle:
        handle.write(json.dumps(record))


def message(number, pid=4100, text="resize these to 400x300", selected=12, **extra):
    """A message shaped the way HeronTalkMailbox.cs writes one."""
    record = {
        "format": talk.FORMAT, "message": number, "text": text,
        "sentUtc": "2026-09-27T13:05:11Z", "revitPid": pid, "revitVersion": "2024",
        "document": "Project1", "documentPath": "C:\\Projects\\Project1.rvt",
        "view": "Level 1 - Mech",
        "selection": number if selected else None, "selected": selected,
        "categories": ([{"name": "Ducts", "count": 10}, {"name": "Duct Fittings", "count": 2}]
                       if selected else []),
    }
    record.update(extra)
    return record


def selection(number, pid=4100, count=12, total=None, unreadable=0, truncated=False):
    elements = [{"id": str(1491000 + i), "uniqueId": "u-%d" % i,
                 "category": "Ducts" if i % 6 else "Duct Fittings", "name": "Duct %d" % i}
                for i in range(count)]
    return {
        "format": talk.FORMAT, "selection": number, "takenUtc": "2026-09-27T13:05:11Z",
        "revitPid": pid, "revitVersion": "2024", "document": "Project1",
        "documentPath": "", "view": "Level 1 - Mech",
        "total": total if total is not None else count + unreadable,
        "saved": count, "unreadable": unreadable, "truncated": truncated,
        "categories": [{"name": "Ducts", "count": count}], "elements": elements,
    }


def main():
    work = tempfile.mkdtemp(prefix="heron-talk-")
    try:
        return run(work)
    finally:
        shutil.rmtree(work, ignore_errors=True)


def run(work):
    print("1. Talk is off unless a chat is started for it")
    check(not talk.enabled({}), "no HERON_TALK means off")
    check(not talk.enabled({"HERON_TALK": "off"}), "HERON_TALK=off is off - .mcp.json's default")
    check(talk.enabled({"HERON_TALK": "on"}), "HERON_TALK=on is on")
    check(not talk.enabled({"HERON_TALK": "${HERON_TALK:-off}"}),
          "an unexpanded .mcp.json value is not mistaken for on")

    config = json.load(io.open(os.path.join(ROOT, ".mcp.json"), encoding="utf-8"))
    env = config["mcpServers"]["heron"].get("env", {})
    check(env.get("HERON_TALK") == "${HERON_TALK:-off}",
          ".mcp.json passes HERON_TALK on, and off when it is not set")
    check(env.get("HERON_CLIENT_ID") == "ajmal-pc",
          "and still pins the one client id (FRAGMENT-ISSUES 5b-51)")

    launcher = io.open(os.path.join(ROOT, "mcp", "heron-talk.cmd"), encoding="utf-8").read()
    check("set HERON_TALK=on" in launcher, "the launcher turns Talk on")
    check("--dangerously-load-development-channels server:heron" in launcher,
          "the launcher names Heron's server as the development channel")
    check("--allowedTools mcp__heron" in launcher,
          "the launcher pre-allows Heron's own tools, and nothing else")
    check(not re.search(r"--allowedTools\s+\S*(Bash|Edit|Write)", launcher),
          "it does not pre-allow a shell or file edits")

    print()
    print("2. A message is claimed once, and only a finished one")
    root = os.path.join(work, "talk")
    folder = os.path.join(root, "4100")
    write(os.path.join(folder, "message-1.json"), message(1))
    write(os.path.join(folder, "message-2.json"), message(2, text="and these"))
    write(os.path.join(folder, "message-3.json.tmp"), message(3))
    write(os.path.join(folder, "message-0.taken.json"), message(0))
    write(os.path.join(folder, "selection-1.json"), selection(1))

    waiting = talk.pending(root)
    check([n for _, n, _ in waiting] == [1, 2],
          "pending lists 1 and 2, oldest first - not the .tmp, not the taken one: %s"
          % [n for _, n, _ in waiting])

    first = talk.claim(waiting[0][2])
    check(first is not None and first.get("text") == "resize these to 400x300",
          "claiming returns the message")
    check(os.path.exists(os.path.join(folder, "message-1.taken.json"))
          and not os.path.exists(os.path.join(folder, "message-1.json")),
          "and renames it, so it is kept but nobody else's to take")
    check(talk.claim(waiting[0][2]) is None,
          "a second chat claiming the same message gets nothing")
    check([n for _, n, _ in talk.pending(root)] == [2], "only 2 is left waiting")

    print()
    print("3. What the chat receives")
    content, meta = talk.channel_event(message(7, text="  resize these to 400x300  "))
    check(content.split("\n")[0] == "resize these to 400x300",
          "the modeller's words come first, trimmed and otherwise unchanged")
    check('model "Project1"' in content and 'view "Level 1 - Mech"' in content
          and "Revit 2024" in content, "then which Revit, model and view")
    check("Selection 7 saved: 12 elements - Ducts 10, Duct Fittings 2" in content
          and "heron_selection(7)" in content,
          "then the count, the categories and how to read the list - not the list")
    check(meta == {"message": "7", "revit": "4100", "selection": "7", "selected": "12"},
          "meta routes it: %s" % meta)
    check(all(re.match(r"^[A-Za-z0-9_]+$", k) for k in meta),
          "every meta key is an identifier, which is all Claude Code keeps")
    check("4100" not in content, "the process number stays out of the words (mcp/README rule 4)")

    content, meta = talk.channel_event(message(8, selected=0))
    check("(Nothing was selected.)" in content and meta["selection"] == "none",
          "a message with nothing selected says so")

    content, meta = talk.channel_event(message(9, format=2))
    check("format this Heron does not read" in content and "resize" not in content
          and meta == {"problem": "format"},
          "a newer format is reported as unreadable, and its words are not guessed at")

    print()
    print("4. Only from a live Revit")
    write(os.path.join(root, "5200", "message-1.json"), message(1, pid=5200))
    claimed = talk.collect(root, lambda: set([4100]))
    check([m.get("message") for m in claimed] == [2],
          "a live Revit's message is claimed: %s" % [m.get("message") for m in claimed])
    check(os.path.exists(os.path.join(root, "5200", "message-1.json")),
          "one from a Revit with no live bridge is left where it is")
    check(talk.collect(root, set([4100])) == [], "and nothing is claimed twice")

    print()
    print("5. heron_selection")
    answer = talk.selection_answer(0, folder_root=root)
    check(answer.startswith("Selection 1, saved 2026-09-27T13:05:11Z from Revit 2024")
          and "1491000 | u-0 | Duct Fittings | Duct 0" in answer,
          "number 0 is the latest, listed id | UniqueId | category | name")
    check("check an element still exists before acting on it" in answer,
          "and it says this is the selection when the modeller spoke, not the live one")

    write(os.path.join(folder, "selection-3.json"), selection(3, count=450))
    page1 = talk.selection_answer(3, folder_root=root)
    page3 = talk.selection_answer(3, page=3, folder_root=root)
    check("Elements 1-200 of 450" in page1 and "heron_selection(3, page=2)" in page1,
          "a long list comes 200 to a page, and says how to get the next")
    check("Elements 401-450 of 450" in page3 and "page=4" not in page3,
          "and the last page says no more")
    check("Elements 401-450" in talk.selection_answer(3, page=99, folder_root=root),
          "a page past the end is the last page, not an error")

    write(os.path.join(folder, "selection-4.json"),
          selection(4, count=5, total=9, unreadable=1, truncated=True))
    cut = talk.selection_answer(4, folder_root=root)
    check("Only the first 5 were saved" in cut and "1 element could not be read" in cut,
          "a list cut short and an element that could not be read are both said, separately")

    missing = talk.selection_answer(42, folder_root=root)
    check("no saved selection 42" in missing and "1, 3, 4" in missing,
          "a number not kept says which ones are")

    write(os.path.join(root, "5200", "selection-9.json"), selection(9, pid=5200))
    ambiguous = talk.selection_answer(0, folder_root=root)
    check("More than one Revit" in ambiguous,
          "with two Revits and no chosen one, 'the latest' is a question, not a guess")
    check(talk.selection_answer(0, bound=4100, folder_root=root).startswith("Selection 4,"),
          "the latest is the chosen Revit's latest")
    check(talk.selection_answer(9, bound=4100, folder_root=root).startswith("Selection 9,"),
          "but a number is found in whichever Revit saved it - a message from another Revit "
          "is answered about its own elements")

    write(os.path.join(root, "5200", "selection-3.json"), selection(3, pid=5200, count=2))
    clash = talk.selection_answer(3, bound=4100, folder_root=root)
    check("more than one Revit" in clash and "revit=" in clash,
          "a number two Revits both saved is a question, even for a chat using one of them")
    check(talk.selection_answer(3, revit=5200, folder_root=root).startswith("Selection 3,")
          and "2 selected" in talk.selection_answer(3, revit=5200, folder_root=root),
          "and the message's revit attribute settles it")
    check("from that Revit" in talk.selection_answer(0, revit=6300, folder_root=root),
          "a Revit that saved nothing is told so")

    write(os.path.join(folder, "selection-5.json"), dict(selection(5), format=2))
    check("format this Heron does not read" in talk.selection_answer(5, folder_root=root),
          "a selection in a newer format is not read")

    empty = os.path.join(work, "nothing-here")
    check("No selection has been saved" in talk.selection_answer(0, folder_root=empty),
          "with no talk folder at all, it says nothing has been saved")

    print()
    print("6. The heartbeat the Talk button looks for")
    beat = talk.heartbeat(root, 777)
    check(os.path.basename(beat) == "listener-777.json" and os.path.exists(beat),
          "a listening chat writes listener-<pid>.json in the talk folder")
    record = json.load(io.open(beat, encoding="utf-8"))
    check(record.get("pid") == 777 and record.get("format") == talk.FORMAT,
          "naming itself, in the format both halves know")
    check(not os.path.exists(beat + ".tmp"), "written whole, not left half-written")
    talk.stop_heartbeat(root, 777)
    check(not os.path.exists(beat), "and removed when it stops listening")

    print()
    print("7. The server wires it")
    check(tools.TOOLS.get("heron_selection") == (tools.READ, None),
          "heron_selection is declared READ, with no bridge operation")
    server = io.open(os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py"),
                     encoding="utf-8").read()
    check(re.search(r"@server\.tool\(\)\s*\ndef heron_selection\(", server) is not None,
          "the server registers heron_selection")
    check(re.search(r"if talk\.enabled\(\):\s*\n\s*talk\.serve\(server, _talk_arrived\)\s*\n\s*else:"
                    r"\s*\n\s*server\.run\(\)", server) is not None,
          "it listens only when Talk is on, and serves exactly as before otherwise")
    check(re.search(r"if talk\.enabled\(\):\s*\n\s*text \+= .*talk\.instructions\(\)", server)
          is not None, "and tells the chat about Talk only when it is on")
    words = talk.instructions()
    check("heron_selection" in words and "never assume the live selection" in words,
          "the instructions say to read the saved selection, not the live one")

    print()
    if FAILURES:
        print("FAILED")
        for failure in FAILURES:
            print("  - %s" % failure)
        return 1
    print("PASSED - Talk's listening half does what D-104 says.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
