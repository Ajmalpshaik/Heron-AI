#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A card that waits for the modeller says so on its label, and Heron holds its
timers for it - Q-86, decided 2026-11-15. Runs without Revit.

    python tests/test_person_wait.py

WHAT WAS WRONG (docs/open-questions/tier-2.md, Q-86; FRAGMENT-ISSUES row
5b-1507). `PICK_ON_SCREEN` waits for a click in Revit, and nothing in Heron
knew a card could wait for a person: the dispatcher gave the job
revit.operationTimeoutSeconds and stopped listening, so a click after about a
minute reached nobody; the run clock (D-129) would stop a slow series of
clicks at the next pass of a loop and report "a loop that never ends"; and the
chat's side gave up at its own floor. The longer wait the Keep or Discard box
is given (D-119) could not be asked for from a card.

WHAT IT HOLDS

  1. THE LABEL. `waitsForModeller` is an ambient need of type bool, spelt the
     same in brain/heron_fragment.py's AMBIENT, the add-in's
     RevitFragment.WaitsForModellerNeed and the bridge client's
     WAITS_FOR_MODELLER_NEED. PICK_ON_SCREEN carries it and validates, the
     client's reader finds it, and an unmarked card reads as unmarked. Over
     the library, every card whose code calls Revit's pick carries it, every
     card that carries it calls one, and none is declared above EXECUTE.
  2. THE CHAT'S SIDE WAITS AS LONG. `heron_config.fragment_timeout(waiting=
     True)` outlasts the add-in's longest wait for such a card at every
     setting, adds the ask timeout once - never twice beside `keeping` - and a
     card without the label waits exactly what it waited before.
  3. THE SERVER READS IT, as text: revit_read and revit_show wait by the
     label and tell the host it is a click they wait for; the bridge client's
     `fragment` command and the batch run wait by it too.
  4. THE ADD-IN HOLDS ITS TIMERS FOR IT, as text: the read and show path
     tells the dispatcher a person is being waited for and pauses the run
     clock around the script - only when the label is declared; a run that
     can change the model is refused first; the dispatcher's answer when even
     the longer wait runs out has words of its own, asked before Keep or
     Discard's. The run clock's own Pause - that time spent in it is not the
     job's - is RUN by tests/Heron.StackGuard.TestHost (tests/test_stack_
     guard.py); this suite checks that case is still there.
  5. THE TWO NEW ANSWERS ARE CLASSIFIED: `awaiting_click` is still running
     (wait, never ask again), `waits_on_change_path` is refused.

WHAT IT CANNOT DO
-----------------
Run Revit's pick. Whether a click after a minute really reaches the card, and
a slow series of clicks really comes back as points, is NEEDS-CHECKING Group
NH, rows NH12 to NH14.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
PICK = os.path.join(FRAGMENTS, "pick-on-screen")
UNMARKED = os.path.join(FRAGMENTS, "read-selection")
ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
FRAGMENT_CS = os.path.join(ADDIN, "RevitFragment.cs")
DISPATCHER_CS = os.path.join(ADDIN, "RevitDispatcher.cs")
STACK_HOST = os.path.join(ROOT, "tests", "Heron.StackGuard.TestHost", "Program.cs")
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
CLIENT_PY = os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py")

sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_bridge_client as CLIENT                            # noqa: E402
import heron_config as CFG                                      # noqa: E402
import heron_failure as FAIL                                    # noqa: E402
import heron_fragment as FRAG                                   # noqa: E402
import heron_tools as TOOLS                                     # noqa: E402

# The label's spelling, as the fix writes it - used below only so the checks
# still RUN, and fail, against code that has none (heron-ship 2a).
LABEL = "waitsForModeller"

# Revit's own picks: Selection.PickPoint, PickObject, PickObjects,
# PickElementsByRectangle and PickBox, read as code - a comment that names one
# is not a call.
PICKS = re.compile(r"\.(PickPoint|PickObjects?|PickElementsByRectangle|PickBox)\s*\(")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    try:
        with io.open(path, encoding="utf-8") as handle:
            return handle.read()
    except (OSError, IOError):
        return ""


def body_of(text, start_marker, end_marker):
    """The text from start_marker to the next end_marker after it, or ""."""
    at = text.find(start_marker)
    if at < 0:
        return ""
    end = text.find(end_marker, at + len(start_marker))
    return text[at:end] if end >= 0 else text[at:]


def py_body(text, name):
    """One Python function, from its def to the next top-level def."""
    start = text.find("\ndef %s(" % name)
    if start < 0:
        return ""
    rest = text[start + 1:]
    end = re.search(r"\n(?:@server\.tool\(\)\n)?def ", rest)
    return rest[:end.start()] if end else rest


def label_section():
    print()
    print("1. The label - one ambient need, one spelling on every side")
    declared = FRAG.AMBIENT.get(LABEL)
    check(declared == "bool",
          "heron_fragment.AMBIENT declares %s as an ambient bool - found %r" % (LABEL, declared))
    check(FRAG.need_source({"name": LABEL, "type": "bool"}) == "ambient",
          "so a card's %s need is the host's, never a value to ask the caller for" % LABEL)

    executor = read(FRAGMENT_CS)
    found = re.search(r'const string WaitsForModellerNeed = "([^"]+)";', executor)
    check(found is not None and found.group(1) == LABEL,
          "the add-in binds it by the same name (RevitFragment.WaitsForModellerNeed) - "
          "found %r" % (found.group(1) if found else None))
    client_name = getattr(CLIENT, "WAITS_FOR_MODELLER_NEED", None)
    check(client_name == LABEL,
          "and the bridge client waits by the same name - found %r" % (client_name,))

    waits = getattr(CLIENT, "waits_for_modeller", None)
    check(waits is not None, "the bridge client can say whether a card waits for the modeller")
    if waits is None:
        def waits(needs):
            return any(n.get("name") == LABEL for n in needs or [])

    try:
        card = FRAG.load(PICK)
        problems = FRAG.validate(card)
    except ValueError as failure:
        card, problems = None, [str(failure)]
    check(card is not None and not problems,
          "PICK_ON_SCREEN loads and validates with the label on it - %s"
          % ("; ".join(problems[:2]) or "no problems"))
    needs = CLIENT.fragment_needs(os.path.join(PICK, "fragment.yaml"))
    check(needs is not None and waits(needs),
          "PICK_ON_SCREEN carries the label, and the client's own reader of a card's "
          "needs finds it")
    entry = [n for n in needs or [] if n.get("name") == LABEL]
    check(bool(entry) and entry[0].get("type") == "bool",
          "declared as the ambient type, so the add-in binds it rather than refusing it "
          "as never supplied")
    other = CLIENT.fragment_needs(os.path.join(UNMARKED, "fragment.yaml"))
    check(other is not None and not waits(other),
          "READ_SELECTION, which waits for nobody, reads as unmarked")
    check(not waits(None) and not waits([]),
          "needs nobody could read, or none, read as unmarked - the caller refuses an "
          "unreadable card by name anyway")

    # OVER THE LIBRARY. A pick with no label is the defect Q-86 is about; a
    # label on a card that picks nothing would hold the clocks for nobody.
    unlabelled, borrowed, too_high, picking = [], [], [], []
    for name in sorted(os.listdir(FRAGMENTS)):
        card_path = os.path.join(FRAGMENTS, name, "fragment.yaml")
        code_path = os.path.join(FRAGMENTS, name, "impl", "any", "fragment.cs")
        if not os.path.isfile(card_path):
            continue
        code = TOOLS.code_only(read(code_path))
        picks = PICKS.search(code) is not None
        labelled = waits(CLIENT.fragment_needs(card_path))
        if picks:
            picking.append(name)
        if picks and not labelled:
            unlabelled.append(name)
        if labelled and not picks:
            borrowed.append(name)
        if labelled:
            level = TOOLS.level_named(CLIENT.fragment_risk(card_path))
            if level is None or level > TOOLS.EXECUTE:
                too_high.append(name)
    check(bool(picking), "the library has a card that calls Revit's pick: %s"
          % (", ".join(picking) or "none"))
    check(not unlabelled,
          "every card whose code calls Revit's pick carries the label - unlabelled: %s"
          % (", ".join(unlabelled) or "none"))
    check(not borrowed,
          "and no card carries it that picks nothing - borrowed: %s"
          % (", ".join(borrowed) or "none"))
    check(not too_high,
          "and none that carries it is declared above EXECUTE, where the add-in would "
          "refuse it on every run - above: %s" % (", ".join(too_high) or "none"))


def client_section():
    print()
    print("2. The chat's side waits as long as the add-in does - and no longer for "
          "anything else")
    code = CFG.fragment_timeout.__code__
    takes = "waiting" in code.co_varnames[:code.co_argcount]
    check(takes, "heron_config.fragment_timeout can be told the card waits for the modeller")

    def waited(values, **kw):
        if not takes:
            kw.pop("waiting", None)
        return CFG.fragment_timeout(values, **kw)

    for operation in (10, 60, 300):
        for ask_for in (1, 60, 300, 3600):
            values = dict(CFG.DEFAULTS)
            values["revit.operationTimeoutSeconds"] = str(operation)
            values["revit.askTimeoutSeconds"] = str(ask_for)
            # What the add-in waits, at most, for a card that waits for the
            # modeller: Revit taking the job, the operation, then the ask.
            longest = (CFG.busy_timeout(values) + CFG.operation_timeout(values)
                       + CFG.ask_timeout(values))
            check(waited(values, waiting=True) > longest,
                  "operation=%s ask=%s: the add-in answers within %g, a card that waits for "
                  "the modeller is waited for %g" % (operation, ask_for, longest,
                                                      waited(values, waiting=True)))

    values = dict(CFG.DEFAULTS)
    check(CFG.fragment_timeout(values) == 180.0,
          "a card without the label waits exactly the 180 it always did")
    check(waited(values, waiting=False) == CFG.fragment_timeout(values),
          "and saying it does not wait is the same as saying nothing")
    check(waited(values, waiting=True) == CFG.fragment_timeout(values) + CFG.ask_timeout(values),
          "the label adds the ask timeout once, as a keep does")
    check(waited(values, waiting=True, keeping=True)
          == CFG.fragment_timeout(values, keeping=True),
          "and never twice: one job has one longer wait")
    check(CFG.fragment_timeout(values, keeping=True)
          == CFG.fragment_timeout(values) + CFG.ask_timeout(values),
          "a keep still waits what it waited before")


def server_section():
    print()
    print("3. The server and the command line wait by the label, and say it is a click")
    server = read(SERVER)
    for door, op in (("revit_read", "run_fragment_read"), ("revit_show", "run_fragment_show")):
        body = py_body(server, door)
        sent = body.find('session.request("%s"' % op)
        asked = body.find("bridge.waits_for_modeller(needs)")
        check(asked >= 0 and sent > asked,
              "%s reads the label off the card's needs before it sends %s" % (door, op))
        call = body[sent:body.find(")", body.find("on_wait", sent)) + 1] if sent >= 0 else ""
        check("fragment_timeout(waiting=waits)" in call,
              "%s waits for a labelled card as long as the add-in does" % door)
        check("_waiting(ctx, waits)" in call,
              "and tells the host what it waits for - %s" % (call.split("on_wait=")[-1][:40]
                                                             if call else "no call found"))

    words = re.search(r'WAITING_FOR_CLICK = \("([^)]*)\)', server, re.S)
    said = re.sub(r'"\s*"', "", words.group(1)) if words else ""
    check("click in Revit" in said and "%d" in said,
          "the host is told \"Waiting for you to click in Revit - N s so far\" - found %r"
          % said[:70])
    waiting = py_body(server, "_waiting")
    check("for_click" in waiting.split("\n", 1)[0]
          and "WAITING_FOR_CLICK if for_click else WAITING_ON_REVIT" in waiting,
          "_waiting says the click only when it is told the card waits for one, and "
          "Waiting for Revit otherwise")

    client = read(CLIENT_PY)
    command = py_body(client, "cmd_fragment")
    batch = client[client.find("def run_fragment(phase"):] if "def run_fragment(phase" in client else ""
    check("waiting=waits_for_modeller(needs)" in command,
          "the bridge client's `fragment` command waits for a labelled card as long")
    check("waiting=waits_for_modeller(needs)" in batch[:6000],
          "and so does the batch run that proves one (NH1)")


def addin_section():
    print()
    print("4. The add-in holds its timers for a labelled card, and for no other")
    executor = read(FRAGMENT_CS)
    run = body_of(executor, "public static string Run(UIApplication app", "\n        private static ")
    declared = run.find("DeclaresNeed(needs, WaitsForModellerNeed)")
    check(declared >= 0, "Run reads the label off the needs the request carries")

    refused = run.find('Json.Error("waits_on_change_path"')
    group = run.find("new TransactionGroup(")
    compiled = run.find("Compile(")
    check(refused > declared >= 0
          and "waitsForPerson && writing" in run[declared:refused],
          "a run that can change the model is refused for a labelled card")
    check(refused >= 0 and (group < 0 or refused < group) and (compiled < 0 or refused < compiled),
          "before anything is compiled and before any transaction group opens")
    words = run[refused:run.find(");", refused)] if refused >= 0 else ""
    check("Nothing was run" in words and "revit_show" in words,
          "and the refusal says nothing ran and which door runs it")

    read_path = body_of(run, "counted = ReadFootprint.Of(target);", "ReadChangedModel(")
    check("if (waitsForPerson) RevitDispatcher.AwaitingClick();" in read_path,
          "on the read and show path the dispatcher is told a person is being waited for - "
          "only when the label is declared")
    held = re.search(r"using \(waitsForPerson \? HeronRunClock\.Pause\(\) : null\)\s*\{\s*"
                     r"threw = RunScript\(", read_path)
    check(held is not None,
          "and the run clock is held around the script, for a labelled card only - an "
          "unmarked card's script runs on the clock it always had")
    check(executor.count("HeronRunClock.Pause()") == 1,
          "the one pause in the executor is that one: the write path's scripts are never held")

    prologue = body_of(executor, "if (name == WaitsForModellerNeed)", "continue;")
    check('lines.Append("bool ").Append(name).Append(" = true;\\n");' in prologue,
          "the label is bound true in the prologue, as noTransactionOpen is - not refused as "
          "never supplied")

    dispatcher = read(DISPATCHER_CS)
    click = body_of(dispatcher, "internal static void AwaitingClick()", "\n        }")
    check("job.MarkAwaitingClick()" in click and "AwaitingModeller();" in click,
          "AwaitingClick marks the job and asks for the Keep or Discard box's own hold - the "
          "longer wait, and the banner out of the way")
    dispatch = body_of(dispatcher, "public string Dispatch(string request)", "\n        private string AnswerStop")
    check("job.AskedModeller && job.WaitForFinish(_askTimeout)" in dispatch,
          "the dispatcher still gives a job that asked the modeller revit.askTimeoutSeconds more")
    click_at = dispatch.find('Json.Error("awaiting_click"')
    keep_at = dispatch.find('Json.Error("awaiting_modeller"')
    delete_at = dispatch.find('Json.Error("awaiting_delete_answer"')
    check(click_at >= 0 and keep_at > click_at > delete_at >= 0,
          "when even that runs out, a click is answered awaiting_click, asked before Keep or "
          "Discard's awaiting_modeller")
    words = dispatch[click_at:dispatch.find(");", click_at)] if click_at >= 0 else ""
    check("click" in words and "Esc" in words and "Keep" not in words and "Discard" not in words,
          "in words of its own - the click and Esc, never Keep or Discard")

    host = read(STACK_HOST)
    check("HeronRunClock.Pause()" in host and "time spent in Pause() is not the job's" in host,
          "the run clock's Pause is run by the stack guard host - time spent in it is not the "
          "job's (tests/test_stack_guard.py)")


def failure_section():
    print()
    print("5. The two new answers are classified, never the fail-closed unknown")
    for writes in (False, True):
        late = FAIL.analyse({"ok": False, "error": "awaiting_click", "message": "m"}, writes=writes)
        check(late is not None and late.outcome == FAIL.RUNNING
              and late.next_step == FAIL.WAIT and not late.may_retry,
              "awaiting_click (writes=%s) is still running: wait, never ask again - found %r"
              % (writes, late))
        refused = FAIL.analyse({"ok": False, "error": "waits_on_change_path", "message": "m"},
                               writes=writes)
        check(refused is not None and refused.outcome == FAIL.REFUSED
              and refused.touched_the_model is False,
              "waits_on_change_path (writes=%s) is refused - nothing ran - found %r"
              % (writes, refused))


def main():
    print("A card that waits for the modeller - the label, and the timers held for it (Q-86)")
    print("=" * 78)
    label_section()
    client_section()
    server_section()
    addin_section()
    failure_section()
    print()
    if FAILURES:
        print("FAILED - %d check(s)" % len(FAILURES))
        return 1
    print("PASSED")
    return 0


if __name__ == "__main__":
    sys.exit(main())
