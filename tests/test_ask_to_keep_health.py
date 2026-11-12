#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Whether Revit will ask Keep or Discard, said by the Revit that decides it.

    python tests/test_ask_to_keep_health.py

The owner's Ask to Keep switch (D-126, D-138, D-150) is read inside the
add-in, at the moment a change is kept, and until 2026-11-12 it was sent to
nobody. So a chat could not say beforehand "Revit will ask you to Keep or
Discard" or "this is kept at once, and Undo takes it back" - it could only
find out afterwards, from the verdict. Now the add-in's lease-exempt `info`
answer carries what the switch says, read fresh on every call, and
revit_health says it in one line for each connected Revit.

Runs with no Revit, no SDK and no .NET: the C# is read as text, the client's
reader and the three sentences are called, and the server's wiring is read
from its source (heron_tools.server_text, since the server cannot be imported
without the MCP SDK). The same reply through the real tool is
tests/test_mcp_serves.py's, and the real `info` over a real pipe - the switch
flipped under a running bridge - is tests/test_bridge_roundtrip.py's.

WHAT IT PROVES
  1. THE ADD-IN SAYS IT, AND ONLY SAYS IT. `info` carries `askToKeep` from
     HeronPermissions.AsksBeforeKeeping - the same reader the executor
     decides by, which loads the settings fresh - and nothing in the add-in
     reads that name off a request, or sets the switch from the bridge. On
     the Python side the name is written once, where a reply is read: no
     request can carry it. (tests/test_keep_question.py already fails either
     side of the Python on the bare name of the settings key, for the same
     reason; the field is named for the switch as Revit shows it, so that
     assertion stands as it was written.)

  2. THE CLIENT READS FOUR ANSWERS, NOT TWO. True, False, a word of its own
     for an add-in that answered and named no switch - an older add-in - and
     None for no usable answer: a refused or missing reply, or a field that
     is not a JSON true or false. Neither of the last two is ever turned
     into off, which is only the default, and neither into the other.

  3. FOUR LINES A MODELLER CAN READ. On and off are said of a change made
     through revit_change and name what is different - a job's kept rest, a
     change Revit counted nothing in, a delete, a publish, revit_apply_move -
     because the first wording said them of every change and a review found
     that untrue. An older add-in "does not say"; an answer that never came
     "could not be read", and is not blamed on an older add-in. Each is one
     line, in Revit's words, and the last two claim neither on nor off.

  4. revit_health ASKS EVERY CONNECTED REVIT, the one this chat holds and the
     ones it does not, and never answers from the server's own copy of the
     settings file (Q-70: on some PCs that is not the file Revit reads).

WHAT IT DOES NOT
  It is not a Revit. That a real Revit's `info` says ON after the switch is
  turned on in Settings, off after it is turned off, and that a Revit running
  an older Heron gets the "does not say" line, is NEEDS-CHECKING Group NP.
"""

import ast
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_bridge_client as bridge                          # noqa: E402
import heron_health as HEALTH                                 # noqa: E402
import heron_tools as TOOLS                                   # noqa: E402

BRIDGE_CS = os.path.join(ROOT, "revit", "Heron.Bridge", "BridgeServer.cs")
PERMISSIONS_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronPermissions.cs")
CLIENT_PY = os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py")
CS_DIRS = (os.path.join(ROOT, "revit"), os.path.join(ROOT, "platform"))
SKIP_FOLDERS = ("obj", "bin")

# THE NAME ON THE WIRE, TYPED HERE ON PURPOSE: this is the other side of the
# comparison. Read from the code under test it would agree with whatever that
# code called it.
FIELD = "askToKeep"

FAILURES = []


def check(condition, description):
    print("  %-4s %s" % ("ok" if condition else "FAIL", description))
    if not condition:
        FAILURES.append(description)


def read(path):
    with io.open(path, encoding="utf-8") as fh:
        return fh.read()


def code_of(text):
    """C# with its `//` comment lines and trailing comments taken out, so a
    name in a comment is never mistaken for a call."""
    kept = []
    for line in text.split("\n"):
        stripped = line.strip()
        if stripped.startswith("//"):
            continue
        kept.append(re.sub(r"\s//[^\"]*$", "", line))
    return "\n".join(kept)


def block_after(text, opening):
    """The braces block that follows `opening`, or "" when it is not there."""
    at = text.find(opening)
    if at < 0:
        return ""
    start = text.find("{", at)
    if start < 0:
        return ""
    depth = 0
    for index in range(start, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[start:index + 1]
    return ""


def every_cs():
    for root in CS_DIRS:
        for dirpath, dirnames, filenames in os.walk(root):
            dirnames[:] = [d for d in dirnames if d not in SKIP_FOLDERS]
            for name in filenames:
                if name.endswith(".cs"):
                    yield os.path.join(dirpath, name)


def named_in_text(source):
    """Every place Python code names FIELD - [(kind, line)] - as a string, a
    keyword argument or a bare name. A docstring or a comment that mentions
    it is not code and is not counted."""
    found = []
    for node in ast.walk(ast.parse(source)):
        if isinstance(node, ast.Constant) and node.value == FIELD:
            found.append(("text", node.lineno))
        elif isinstance(node, ast.keyword) and node.arg == FIELD:
            found.append(("keyword", getattr(node.value, "lineno", 0)))
        elif isinstance(node, ast.Name) and node.id == FIELD:
            found.append(("name", node.lineno))
    return sorted(found, key=lambda item: item[1])


def named_in_code(path):
    return named_in_text(read(path))


class Reply(object):
    """A connected Revit that answers every request with one payload, and
    remembers which operations it was asked."""

    def __init__(self, payload):
        self.payload = payload
        self.asked = []

    def request(self, op, **_kwargs):
        self.asked.append(op)
        return self.payload


def the_addin_says_it():
    print()
    print("1. The add-in says it in `info`, read at the moment of the call")
    server = code_of(read(BRIDGE_CS))
    info = block_after(server, 'if (op == "info")')
    check(bool(info), "BridgeServer answers `info` in a block of its own")
    check('Json.Bool("%s", HeronPermissions.AsksBeforeKeeping())' % FIELD in info,
          "`info` carries %s, from HeronPermissions.AsksBeforeKeeping - the reader "
          "the executor decides by" % FIELD)
    check(server.count("AsksBeforeKeeping") == info.count("AsksBeforeKeeping")
          and info.count("AsksBeforeKeeping") == 1,
          "and it is read there and nowhere else in the bridge: no field keeps an "
          "earlier answer for a later call")

    permissions = read(PERMISSIONS_CS)
    reader = block_after(permissions, "public static bool AsksBeforeKeeping()")
    check("HeronConfig.Load()" in reader,
          "AsksBeforeKeeping loads the settings fresh on every call, so a switch "
          "flipped mid-session is what the next `info` says")

    setters, readers = [], []
    for path in every_cs():
        text = code_of(read(path))
        name = os.path.relpath(path, ROOT)
        if "Heron.Bridge" in name and "SetAsksBeforeKeeping" in text:
            setters.append(name)
        if re.search(r'Read\w*\(\s*\w+\s*,\s*"%s"' % FIELD, text):
            readers.append(name)
    check(not setters,
          "the bridge never SETS the switch - only a person in Revit does: %s"
          % (setters or "none"))
    check(not readers,
          "and nothing in the add-in reads %s off a request (Golden Rule 19): %s"
          % (FIELD, readers or "none"))

    # AND THE PYTHON SIDE CANNOT SEND IT. Read as Python, not as text: the
    # name as a string, as a keyword argument (`dict(askToKeep=...)`,
    # `request(..., askToKeep=...)`) or as a name of its own. Once in the
    # client, the string a reply is read by, and nowhere in the server's
    # files. A count of the quoted name alone would miss a keyword.
    client = named_in_code(CLIENT_PY)
    line = (read(CLIENT_PY).split("\n")[client[0][1] - 1] if len(client) == 1 else "")
    check(len(client) == 1 and client[0][0] == "text" and "reply.get(" in line,
          "the client names %s once in its code, where it reads a reply - found %r"
          % (FIELD, client))
    served = dict((os.path.basename(path), named_in_code(path))
                  for path in TOOLS.server_files())
    check(not any(served.values()),
          "and no file of the server names it at all, so no request can carry it: %s"
          % (dict((name, found) for name, found in served.items() if found) or "none"))
    planted = named_in_text("request('x', op_args=dict(%s=False))\n"
                            "send(**{'%s': 0})\n" % (FIELD, FIELD))
    check(sorted(kind for kind, _line in planted) == ["keyword", "text"],
          "the reader has teeth: a keyword argument and a quoted key are both found - %r"
          % (planted,))


def the_client_reads_four_answers():
    print()
    print("2. The client reads four answers, not two")
    state = getattr(bridge, "ask_to_keep_state", None)
    check(state is not None,
          "the client can ask a Revit whether it asks before keeping a change")
    if state is None:
        def state(_session):                    # so the checks below RUN and FAIL
            return "(the client has no such reader)"
    not_sent = getattr(bridge, "ASK_TO_KEEP_NOT_SENT", None)
    check(isinstance(not_sent, str) and bool(not_sent),
          "the client has a word of its own for a good answer that names no switch")
    if not isinstance(not_sent, str) or not not_sent:
        not_sent = "(the client has no such word)"

    check(state(Reply({"ok": True, FIELD: True})) is True,
          "a Revit that says true is ON")
    check(state(Reply({"ok": True, FIELD: False})) is False,
          "a Revit that says false is off")
    older = Reply({"ok": True, "inUse": False, "activeModel": "Project1"})
    got = state(older)
    check(got == not_sent,
          "an older add-in - a good answer with no such field - is NOT SENT, never off: %r"
          % (got,))
    check(older.asked == ["info"],
          "and it was asked `info` alone - lease-exempt, so asking claims nothing: %r"
          % (older.asked,))

    # NO USABLE ANSWER IS A DIFFERENT FACT FROM AN OLDER ADD-IN, and until the
    # review of 2026-11-12 the two were one None: a Revit that did not answer
    # was told its Heron was old. None now means only "could not be read".
    odd = [("null", None), ('the text "true"', "true"), ('the text "false"', "false"),
           ("the number 1", 1), ("the number 0", 0), ("a list", [True])]
    wrong = [name for name, value in odd
             if state(Reply({"ok": True, FIELD: value})) is not None]
    check(not wrong,
          "a field that is there and is not a JSON true or false could not be read - "
          "None, not a guess and not `older`: %s" % (wrong or "none"))
    check(state(Reply({"ok": False, "error": "unauthorized", FIELD: True})) is None
          and state(Reply({"ok": False, "error": "unauthorized"})) is None,
          "a refused `info` could not be read, whatever it carries - never `older`")
    check(state(Reply(None)) is None and state(Reply("not a reply")) is None,
          "and neither could no reply at all, or one that is not an object")


def four_lines_a_modeller_reads():
    print()
    print("3. Four lines a modeller can read")
    line = getattr(HEALTH, "ask_to_keep_line", None)
    check(line is not None, "heron_health has the sentence for each answer")
    if line is None:
        def line(_asks):                        # so the checks below RUN and FAIL
            return ""
    word = getattr(HEALTH, "ASK_TO_KEEP_NOT_SENT", None)
    check(word is not None and word == getattr(bridge, "ASK_TO_KEEP_NOT_SENT", object()),
          "heron_health and the client have ONE word for an add-in that sent nothing")
    if not isinstance(word, str):
        word = "(heron_health has no such word)"

    on, off, older, unread = line(True), line(False), line(word), line(None)
    print("       on:     %s" % on)
    print("       off:    %s" % off)
    print("       older:  %s" % older)
    print("       unread: %s" % unread)

    # ON AND OFF SAY ONLY WHAT IS TRUE OF EVERY CHANGE THEY NAME. The first
    # wording said "after a change from the chat ... keeps nothing until the
    # modeller chooses Keep", and four kinds of change break that: a move
    # applied from its preview never reads the switch (D-138 item 9), a
    # publish is done before the box opens (D-126), a delete gets the Delete
    # box instead (D-142), and a change Revit counted nothing in is kept
    # unasked (D-126 item 6). So each line is scoped to revit_change and
    # names what is different.
    check("Ask to Keep is ON" in on and "Keep or Discard" in on and "revit_change" in on
          and "only on Keep" in on,
          "ON is about a change made through revit_change: shown in a Keep or Discard "
          "box and kept only on Keep")
    check("after a change from the chat" not in on and "keeps nothing until" not in on,
          "and no longer says it of every change from the chat")
    named = [what for what, words in (
        ("the rest of a job the modeller chose to keep", "keep the rest"),
        ("a change Revit counted nothing in", "counted nothing"),
        ("a delete, which gets its own box", "Delete box"),
        ("an export, save or sync, done before the box", "export, save or sync"),
        ("revit_apply_move, which keeps its own preview", "revit_apply_move"))
        if words not in on]
    check(not named, "ON names what is different: missing %s" % (named or "nothing"))
    check("Discard cannot" in on,
          "and that Discard cannot take back what a publish sent out")
    check("kept at once" not in on, "and never that the change is kept at once")
    check("Ask to Keep is off" in off and "kept at once" in off and "revit_change" in off
          and "Undo" in off,
          "off is about a change made through revit_change: kept at once, one entry in "
          "Revit's Undo list")
    check("Delete box" in off and "asked" in off,
          "and that a delete is still asked about first, in its own box (D-142)")
    check("export, save or sync" in off and "Undo cannot" in off,
          "and that Undo cannot take back an export, a save or a sync")
    check("takes it back in one step" not in off,
          "and no longer promises one Undo for every change")

    check("does not say" in older and "older" in older
          and "Heron > AI Bridge > Settings" in older,
          "an add-in that sent nothing: this Revit's Heron does not say, it is an older "
          "add-in, and where the switch is")
    check("could not read" in unread and "Heron > AI Bridge > Settings" in unread,
          "no usable answer: Heron could not read it from this Revit just now, and where "
          "the switch is")
    check("older" not in unread and "does not say" not in unread,
          "and that line does not blame an older add-in for an answer that never came")
    silent = [name for name, said in (("older", older), ("unread", unread))
              if re.search(r"\bis (ON|off)\b", said) or "kept at once" in said
              or "Keep or Discard" in said]
    check(not silent,
          "neither claims ON or off - no default is passed off as an answer: %s"
          % (silent or "none"))
    check(len(set((on, off, older, unread))) == 4 and all((on, off, older, unread)),
          "four answers, four different lines")

    lines = (("on", on), ("off", off), ("older", older), ("unread", unread))
    code_words = (FIELD, "write.", "heron.config", "`info`", "protocol", "boolean",
                  "null", "True", "False", "None", "add-in's", "config")
    spoken = [(name, w) for name, said in lines for w in code_words if w in said]
    check(not spoken, "none of them uses a code word: %s" % (spoken or "none"))
    check(all("\n" not in said and said == said.strip() for _name, said in lines),
          "each is one line")
    check(all(said.count("Ask to Keep") == 1 for _name, said in lines),
          "and each names the switch once, so a reader can count one line per Revit")
    stray = [repr(value) for value in ("true", "false", 1, 0, "", [], "on")
             if line(value) != unread]
    check(not stray,
          "only a real true or false is an answer, and only the client's own word is "
          "`older`; anything else reads as could not read: %s" % (stray or "none"))


def health_asks_every_revit():
    print()
    print("4. revit_health asks every connected Revit, and never its own file")
    text = TOOLS.server_text()
    health = text[text.find("def revit_health("):]
    health = health[:health.find("\n@server.tool()")]
    check("def revit_health(" in health, "revit_health can be read from the server's source")
    asked = health.find("bridge.ask_to_keep_state(b)")
    check(asked >= 0 and "health.ask_to_keep_line(" in health,
          "it asks each session through the client's reader and says heron_health's line")
    loop = health[health.find("for index, b in enumerate(live"):]
    held = loop.find("if b.pid == binding.pid:")
    other = loop.find("else:", held)
    closing = loop.find("b.close()")
    ask = loop.find("bridge.ask_to_keep_state(b)")
    check(0 <= held < other < ask < closing,
          "for the Revit this chat holds and for the ones it does not - asked after "
          "both branches, and before the session is closed")
    check("asks_before_keeping" not in health,
          "and never from the server's own copy of the settings file (Q-70)")
    check("revit_health" not in TOOLS.STATES_OUTCOME,
          "revit_health is still not a door: it carries no outcome line")


def main():
    print("Whether Revit will ask Keep or Discard, said by the Revit that decides it")
    print("=" * 74)
    the_addin_says_it()
    the_client_reads_four_answers()
    four_lines_a_modeller_reads()
    health_asks_every_revit()

    print()
    if FAILURES:
        print("FAILED  %d check(s):" % len(FAILURES))
        for failure in FAILURES:
            print("  - %s" % failure[:200])
        return 1
    print("PASS    the add-in says what Ask to Keep is, and revit_health says it in "
          "one line")
    return 0


if __name__ == "__main__":
    sys.exit(main())
