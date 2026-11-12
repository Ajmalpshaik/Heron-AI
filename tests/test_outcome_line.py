# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
How a call through one of the three doors ENDED, said in one fixed word.

    python tests/test_outcome_line.py

FRAGMENT-ISSUES row 5b-1159: every reply was prose, so a chat told "kept"
from "refused" from "nobody knows" by reading English - and got it wrong (a
refusal read as success; "never ran" said of an answer that was lost). The
server already worked the outcome out (`_ended`, heron_failure) and showed
it to the Companion's activity list alone. revit_change, revit_read and
revit_show now say it, on one line:

    outcome: REFUSED - Heron or Revit looked at what was asked and said no; ...

The real tool functions are called, against a stand-in Revit that answers
from a script - the way tests/test_mcp_serves.py stands the platform aside.
No pipe is opened and no discovery folder is read.

WHAT IT PROVES
  1. ONE VOCABULARY, IN ONE PLACE. heron_server_core.OUTCOMES is the table:
     every word once, each with what it means, and heron_tools.STATES_OUTCOME
     names the tools that say one. docs/04 section 8.12 prints the same
     words in the same order, and is held to the table - a second copy of a
     list is how one of them goes stale.

  2. A WORD PER WAY OF ENDING, each forced through the door itself: a kept
     change; refusals of several kinds (the switch off, a DRAFT, a read sent
     to revit_change, the add-in's own wrong-model refusal); a rollback; a
     read that stopped before it answered; a lost, oversize or unreadable
     answer - OUTCOME UNKNOWN, never REFUSED and never KEPT; a run still
     going; a read answered; nothing by that name, no session, and words
     that settled nothing - NOT RUN, whichever way the capability was named.

  3. NEVER MORE THAN IS KNOWN. Ask to Keep still open is STILL RUNNING and a
     Discard is ROLLED BACK - neither is KEPT. A Discard Revit did not
     confirm, a reply that names no verdict, a verdict its own reply
     contradicts, and a change behind the first call's pin check are OUTCOME
     UNKNOWN. So is every failure code heron_failure has no row for, at
     every door - `handler_failed` is raised after the job may have run - and
     a reply with no code at all. ROLLED BACK is said only where a
     transaction group was open to roll back: a read that threw is NOT
     ANSWERED and a show that threw is OUTCOME UNKNOWN, because its selection
     may stand. A door that returned without saying how it ended is OUTCOME
     UNKNOWN too: the line fails closed.

  3b. A DOOR THAT BREAKS AFTER ITS REQUEST HAS LEFT STILL ANSWERS. Heron's
     own code raising once Revit has been asked - or Revit's answer being
     JSON that is not an object - is OUTCOME UNKNOWN with the sentence that
     sends a person to the model, never the SDK's bare "Error executing
     tool", which invites sending a change a second time. Raised before
     anything was sent, it is the SDK's to report, as it always was.

  4. EVERY PATH, EXACTLY ONE LINE, IN ONE PLACE. Each case above and each
     further return of the three doors that could be forced: one line that
     starts `outcome: `, the SECOND line of the reply, its word in the table.
     The paths that could not be reached are listed, not passed over.

  5. ADDITIVE. Take the line out and what is left is, character for
     character, what the door's own body returned - every sentence, in the
     same order.

  6. WHERE A HOST THAT READS STRUCTURE FINDS IT. Through the SDK's own
     dispatch the same word is `outcome` in the structured content beside
     `result`; the output schema a host is listed is unchanged; and no
     outcome is flagged `isError` - a refusal is Heron working.

  7. NOTHING ELSE MOVED. The Companion's activity line still shows the
     reply's own first sentence and the label the tool gave; the page's and
     the panel's route into a door (`_through`) carries no line; a tool that
     is not a door carries none.

  8. THE CHAT IS TOLD, IN ITS INSTRUCTIONS (row 5b-1562). host.chat says the
     doors' replies carry an `outcome:` line and that the line, not the
     wording, says how the request ended. Room was made by leaving the
     Constitution's CITATIONS - "(Golden Rule 9)", and the targets of its
     links - out of what a chat is given: every word of every Article is
     still there, the Constitution's own file still carries the citations,
     and Heron's own agents are still given them. Needs no SDK, so it runs
     wherever this file does.

  9. revit_undo_last SAYS A WORD TOO (D-157, 2026-11-12) - the one tool on
     the list that is not a door. Revit's Undo SENT is STILL RUNNING, never
     KEPT and never ANSWERED: Revit runs it after the reply. A check
     (`check_only`) is ANSWERED and sends `undo_check`, never `undo_last`.
     Each refusal is REFUSED, a chat pinned to no model among them, with
     nothing sent; no session and an older add-in are NOT RUN; no answer, an
     answer lost, and an Undo sent in a model this chat does not work on are
     OUTCOME UNKNOWN - and that last says the Undo WAS sent, and where.

WHAT A REVIEW CHANGED, AND WHICH OF THIS FILE'S OWN ASSERTIONS WERE WRONG.
The first version of this suite pinned four words the review of 2026-11-12
showed to be untrue, and each is changed here rather than defended:
  - a read or a show that threw was ROLLED BACK. Nothing is rolled back on
    the path that opens no transaction; now NOT ANSWERED and OUTCOME UNKNOWN.
  - nothing by that name was REFUSED when named by `capability` and NOT RUN
    when named by `request`. One fact, one word: NOT RUN. No session is NOT
    RUN too - nothing judged the request, there was nowhere to send it.
  - KEPT and ANSWERED carried no words after them. A chat reads the line
    with nothing else to explain it, so every word now says what it means.
  - the table held seven words. It holds eight.

WHY THE SECOND LINE, AND NOT THE FIRST OR THE LAST. Both ends of a door's
reply are held by tests/test_mcp_serves.py: a kept change to a model never
saved must OPEN with the NEVER SAVED warning, and a hand-back must CLOSE on
the sentence that names the door to call again. A first line breaks the one
and a last line the other, and neither assertion may be edited. Directly
under the opening sentence breaks nothing.

WHAT IT DOES NOT
  It is not a Revit. Which reply the add-in really sends for each ending is
  the C#'s and NEEDS-CHECKING's. It does not follow a run that is still
  going: no id comes back for one yet (row 5b-1159's second half - C#, and a
  real Revit).

  AND IT DOES NOT RUN WITHOUT THE SDK - the doors are functions of the
  server, which cannot be imported without it. Exit 3, which check-gaps
  reads as WAITING and never as a pass.
"""

import io
import os
import re
import sys
import shutil
import asyncio
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real trail.
scratch_audit.plant()

SKIPPED = 3
FAILURES = []

# THE LINE'S OWN FORM, TYPED HERE ON PURPOSE. This is the other side of the
# comparison: what a host is told to look for. Read from the code under test
# it would agree with whatever that code wrote.
PREFIX = "outcome: "
TAIL = " - "
DOORS = ("revit_change", "revit_read", "revit_show")
# AND THE ONE TOOL THAT IS NOT A DOOR AND SAYS A WORD (2026-11-12, D-157):
# every return of revit_undo_last was read against the table, as row 5b-1563
# asks of a tool that joins. Section 9 walks them.
UNDO_TOOL = "revit_undo_last"
SAYS = DOORS + (UNDO_TOOL,)
WORDS =("KEPT", "ANSWERED", "NOT ANSWERED", "REFUSED", "NOT RUN", "ROLLED BACK",
         "STILL RUNNING", "OUTCOME UNKNOWN")


def check(condition, description):
    print("  %-4s %s" % ("ok" if condition else "FAIL", description))
    if not condition:
        FAILURES.append(description)


def lines_of(said):
    """Every line of a reply that is an outcome line, with where it stands."""
    return [(at, line) for at, line in enumerate((said or "").split("\n"))
            if line.startswith(PREFIX)]


def stated(said):
    """The word a reply states - or None when it states none, or two."""
    found = lines_of(said)
    if len(found) != 1:
        return None
    return found[0][1][len(PREFIX):].split(TAIL, 1)[0]


def without_line(said):
    """The reply with its outcome line taken out, and nothing else touched."""
    return "\n".join(line for line in (said or "").split("\n")
                     if not line.startswith(PREFIX))


class StandIn(object):
    """A Revit that answers from a script and remembers what it was asked."""

    revit_version = "2024"
    pid = 4242

    def __init__(self, reply):
        self.reply = reply
        self.asked = []

    def request(self, op, op_args=None, idempotent=True, **_ignored):
        self.asked.append((op, dict(op_args or {}), idempotent))
        return self.reply

    def close(self):
        pass


def cards():
    """Each fragment's folder with its capability, risk and heron-status."""
    folder = os.path.join(ROOT, "brain", "fragments")
    for name in sorted(os.listdir(folder)):
        card = os.path.join(folder, name, "fragment.yaml")
        if not os.path.isfile(card):
            continue
        seen = {}
        with io.open(card, encoding="utf-8", errors="replace") as fh:
            for line in fh:
                for key in ("capability", "risk", "heron-status"):
                    if line.startswith(key + ":"):
                        seen[key] = line.split(":", 1)[1].strip().strip("\"'")
        yield name, seen


def first_with(risk, status, also=None):
    """The capability of the first fragment declaring both - and passing
    `also(folder, capability)` when one is given - or (None, None)."""
    for name, seen in cards():
        if seen.get("risk") == risk and seen.get("heron-status") == status:
            if also is None or also(name, seen.get("capability")):
                return seen.get("capability"), name
    return None, None


# ------------------------------------------------------------- the replies

MODEL = {"document": "Project1", "documentPath": "C:/jobs/Project1.rvt",
         "projectKey": "key-1", "wasActiveDocument": True}

READ_OK = dict(MODEL, ok=True, ran="x", bound="all as given",
               provides={"found": "2 item(s) [Level 1, Level 2]"},
               providesCount=1, elementsCounted=1234)

UNDO = ("IF anything was written, all of it is ONE undo step; if nothing was, there "
        "is no new undo entry and Ctrl+Z would undo whatever you did before this.")

KEPT = dict(MODEL, ok=True, ran="x", bound="all as given",
            provides={"renamed": "3"}, applied=True, rolledBack=False,
            warnings=0, warningsDismissed=[],
            verdict="'x' was KEPT: Revit accepted the transaction rather than "
                    "rolling it back. " + UNDO,
            verdictKind="kept", verdictShort="'x' was KEPT. " + UNDO)

ASKED_KEPT = dict(KEPT, asked=True, kept=True, modellerAnswer="keep",
                  asking="You were asked in Revit and pressed KEEP after seeing "
                         "the result.")

DISCARDED = dict(KEPT, applied=False, rolledBack=True, asked=True, kept=False,
                 modellerAnswer="discard", verdictKind="discarded", verdictShort=None,
                 verdict="NOTHING WAS KEPT. You were asked in Revit and pressed "
                         "DISCARD, and Revit reported the group rolled back.")

DISCARD_UNCONFIRMED = dict(DISCARDED, rolledBack=False,
                           verdict="You pressed DISCARD and Revit did not report "
                                   "the group rolled back - THE MODEL MAY STILL "
                                   "HOLD THIS CHANGE.")

NO_VERDICT = dict(MODEL, ok=True, ran="x", provides={"renamed": "3"})

# A VERDICT ITS OWN REPLY CONTRADICTS. No add-in sends one today; a word may
# still never be read off one field while the field beside it says otherwise.
KEPT_BUT_ROLLED_BACK = dict(KEPT, rolledBack=True)
KEPT_BUT_NOT_APPLIED = dict(KEPT, applied=False)
KEPT_BUT_ASKED_AND_DECLINED = dict(KEPT, asked=True, kept=False)
DISCARDED_BUT_KEPT = dict(DISCARDED, kept=True)
DISCARDED_BUT_APPLIED = dict(DISCARDED, applied=True)

ELSEWHERE = dict(READ_OK, document="Tower B", documentPath="C:/jobs/TowerB.rvt",
                 projectKey="key-2")

# revit_undo_last (D-157): what the add-in answers when it POSTED Revit's Undo,
# and when it was only asked whether it would.
UNDO_SENT = dict(MODEL, ok=True, sent=True, undoing="rename-views",
                 undoEntry="Heron: rename-views",
                 said="Revit's own Undo was sent for 'rename-views' (Heron: rename-views, "
                      "kept at 14:32). Heron has seen nothing change this model since.")
UNDO_CHECK = dict(MODEL, ok=True, checked=True, sent=False, wouldSend=True,
                  undoing="rename-views",
                  said="Yes - Undo would be sent now, for 'rename-views'. This was only "
                       "a check: nothing was sent to Revit.")


def error(code, message=None):
    return {"ok": False, "error": code,
            "message": message or "The add-in answered %s." % code}


# --------------------------------------------------------------- the stage

class Stage(object):
    """One server module, one stand-in Revit, and the state a call starts
    from put back before every call - so two calls of one case differ only
    in which face of the door was called."""

    def __init__(self, module, core):
        self.module, self.core = module, core
        self.revit = StandIn(dict(READ_OK))
        self._undo = []

    def fresh(self, reply, pin=None):
        module = self.module
        self.revit.reply = reply
        self.revit.asked[:] = []
        module.binding.resolve = lambda: self.revit
        module.pinned.forget()
        if pin is not None:
            module.pinned.check(dict(pin))
        runs = getattr(module, "runs", None)
        if runs is not None:
            module._BATCH = runs.Batch()

    def patch(self, owner, name, value):
        had = getattr(owner, name)
        setattr(owner, name, value)
        self._undo.append((owner, name, had))

    def unpatch(self):
        while self._undo:
            owner, name, had = self._undo.pop()
            setattr(owner, name, had)

    def close(self):
        self.unpatch()
        try:
            del self.module.binding.resolve
        except AttributeError:
            pass
        self.module.pinned.forget()


class Case(object):
    """One way a door can return: which door, with what, against which
    reply, with what stood in - and the word that is true of it."""

    def __init__(self, name, door, word, arguments, reply=None, pin=None,
                 setup=None, kind=None):
        self.name, self.door, self.word = name, door, word
        self.arguments, self.reply, self.pin = arguments, reply, pin
        self.setup, self.kind = setup, kind
        self.logged = ""

    def run(self, stage, body=False):
        """The door's reply - through the tool function a host is served, or
        (`body`) through the page's own route to the same body, which never
        passed the wrapper. A crash comes back as text no check will take for
        an answer (heron-ship 2a)."""
        stage.fresh(self.reply, self.pin)
        # WHAT THE SERVER WRITES TO ITS OWN LOG is kept, not printed among the
        # checks: a door that breaks after sending records the traceback
        # there on purpose, and one in this suite's output reads as a crash.
        had, sys.stderr = sys.stderr, io.StringIO()
        try:
            if self.setup is not None:
                self.setup(stage)
            door = getattr(stage.module, self.door)
            if body:
                door = stage.module._through(door)
            return door(**self.arguments)
        except Exception as exc:                                # noqa: BLE001
            return "(%s raised %s: %s)" % (self.door, type(exc).__name__, exc)
        finally:
            self.logged, sys.stderr = sys.stderr.getvalue(), had
            stage.unpatch()


def build_cases(module, core):
    """Every return of the three doors this suite knows how to force."""
    tools, bridge, brain = module.tools, module.bridge, module.brain
    status = getattr(brain, "proof_status", None)

    def stands(folder, _capability):
        proof = status(folder) if status else None
        return bool(proof) and not proof.get("stale")

    def shows(folder, capability):
        card = os.path.join(ROOT, "brain", "fragments", folder, "fragment.yaml")
        source = os.path.join(ROOT, "brain", "fragments", folder, "impl", "any",
                              "fragment.cs")
        if not os.path.isfile(source):
            return False
        with io.open(source, encoding="utf-8") as fh:
            code = fh.read()
        return not (tools.door_refusal("revit_show", bridge.fragment_risk(card), capability)
                    or tools.disk_refusal("revit_show", capability, code))

    change, _f = first_with("MODIFY", "PROVEN", stands)
    draft, _f = first_with("MODIFY", "DRAFT")
    read, _f = first_with("READ", "PROVEN")
    show, _f = first_with("EXECUTE", "PROVEN", shows)
    if show is None:
        show, _f = first_with("EXECUTE", "DRAFT", shows)
    picked = {"change": change, "draft": draft, "read": read, "show": show}

    ranked = {"capability": read, "risk": "READ", "route": "ranked",
              "candidates": [{"capability": read, "risk": "READ", "why": "close"},
                             {"capability": change, "risk": "MODIFY",
                              "why": "also close"}]}

    def lookup_ranked(stage):
        stage.patch(brain, "lookup", lambda *_a, **_k: dict(ranked))

    def lookup_nothing(stage):
        stage.patch(brain, "lookup", lambda *_a, **_k: {"capability": None})

    def lookup_unreadable(stage):
        def gone(*_a, **_k):
            raise brain.BrainUnavailable("Heron's library could not be opened.")
        stage.patch(brain, "lookup", gone)

    def not_bound(stage):
        def none():
            raise module.NotBound("No Revit is connected to Heron.")
        stage.module.binding.resolve = none

    def old_code(stage):
        stage.patch(core, "_old_code", lambda: ["mcp/server/heron_changed.py"])

    def patched(owner, name, value):
        return lambda stage: stage.patch(owner, name, value)

    def breaks_after_asking(stage):
        def boom(_reply):
            raise RuntimeError("a fault in Heron's own code, after Revit was asked")
        stage.patch(module, "_ctx_reply", boom)

    def pipe_breaks(stage):
        def boom(*_a, **_k):
            raise OSError("the pipe broke with the request on it")
        stage.patch(stage.revit, "request", boom)

    def no_code(stage):
        real = os.path.isfile
        stage.patch(os.path, "isfile",
                    lambda path: False if str(path).endswith("fragment.cs") else real(path))

    C, R, S, U = "revit_change", "revit_read", "revit_show", UNDO_TOOL
    say = {"request": "count the things"}
    cases = [
        # ---- the change door --------------------------------------------
        Case("a change Revit kept", C, "KEPT", {"capability": change}, KEPT,
             kind="kept"),
        Case("a change the modeller was asked about and KEPT", C, "KEPT",
             {"capability": change}, ASKED_KEPT),
        Case("a change the modeller DISCARDED, the rollback confirmed", C,
             "ROLLED BACK", {"capability": change}, DISCARDED, kind="rollback"),
        Case("a Discard whose rollback Revit did not confirm", C,
             "OUTCOME UNKNOWN", {"capability": change}, DISCARD_UNCONFIRMED),
        Case("an `ok` that names no verdict (an add-in from before it)", C,
             "OUTCOME UNKNOWN", {"capability": change}, NO_VERDICT),
        Case("Ask to Keep still open in Revit (awaiting_modeller)", C,
             "STILL RUNNING", {"capability": change}, error("awaiting_modeller"),
             kind="running"),
        Case("a change still running in Revit (still_running)", C,
             "STILL RUNNING", {"capability": change}, error("still_running"),
             kind="running"),
        Case("the delete box still open (awaiting_delete_answer)", C,
             "STILL RUNNING", {"capability": change}, error("awaiting_delete_answer")),
        Case("the Changes switch off (write_disabled)", C, "REFUSED",
             {"capability": change}, error("write_disabled"), kind="refused"),
        Case("the Publish switch off (publish_disabled)", C, "REFUSED",
             {"capability": change}, error("publish_disabled")),
        Case("a DRAFT capability, refused before anything is sent", C, "REFUSED",
             {"capability": draft}, KEPT, kind="refused"),
        Case("a READ sent to revit_change", C, "REFUSED", {"capability": read},
             KEPT, kind="refused"),
        Case("the add-in's own wrong-model refusal (wrong_document)", C, "REFUSED",
             {"capability": change}, error("wrong_document"), kind="refused"),
        Case("nothing by that name", C, "NOT RUN",
             {"capability": "NO_SUCH_CAPABILITY_AT_ALL"}, KEPT),
        Case("no Revit session to send it to", C, "NOT RUN",
             {"capability": change}, KEPT, setup=not_bound),
        Case("a risk Heron cannot read (risk_refusal)", C, "REFUSED",
             {"capability": change}, KEPT,
             setup=patched(bridge, "risk_refusal",
                           lambda *_a, **_k: "Its risk could not be read, so nothing "
                                             "has been sent to Revit.")),
        Case("a risk with no operation (write_operation)", C, "REFUSED",
             {"capability": change}, KEPT,
             setup=patched(bridge, "write_operation", lambda *_a, **_k: None)),
        Case("a contract that cannot be read (fragment_needs)", C, "REFUSED",
             {"capability": change}, KEPT,
             setup=patched(bridge, "fragment_needs", lambda *_a, **_k: None)),
        Case("a card with no code behind it", C, "REFUSED",
             {"capability": change}, KEPT, setup=no_code),
        Case("this chat running old code", C, "REFUSED",
             {"capability": change}, KEPT, setup=old_code),
        Case("the fragment threw and the job was rolled back", C, "ROLLED BACK",
             {"capability": change}, error("fragment_threw"), kind="rollback"),
        Case("Heron's run clock stopped it (ran_too_long)", C, "ROLLED BACK",
             {"capability": change}, error("ran_too_long")),
        Case("the modeller said no to the delete box (delete_declined)", C,
             "ROLLED BACK", {"capability": change}, error("delete_declined")),
        Case("NO ANSWER AT ALL to a change", C, "OUTCOME UNKNOWN",
             {"capability": change}, None, kind="lost"),
        Case("an answer LOST after the change was sent (unknown_outcome)", C,
             "OUTCOME UNKNOWN", {"capability": change}, error("unknown_outcome"),
             kind="lost"),
        Case("an answer TOO LARGE to read (reply_too_large)", C, "OUTCOME UNKNOWN",
             {"capability": change}, error("reply_too_large"), kind="lost"),
        Case("an answer that could NOT BE READ (reply_unreadable)", C,
             "OUTCOME UNKNOWN", {"capability": change}, error("reply_unreadable"),
             kind="lost"),
        Case("a failure code Heron has never seen, on a change", C,
             "OUTCOME UNKNOWN", {"capability": change}, error("never_seen_before")),
        Case("a rollback Revit did not confirm after a dialog", C,
             "OUTCOME UNKNOWN", {"capability": change}, error("dialog_unconfirmed")),
        Case("the bridge's own handler failed, on a change (handler_failed)", C,
             "OUTCOME UNKNOWN", {"capability": change}, error("handler_failed"),
             kind="unrecognised"),
        Case("a reply with no `ok` and no code, on a change", C,
             "OUTCOME UNKNOWN", {"capability": change}, {}, kind="unrecognised"),
        Case("a verdict of kept beside `rolledBack`", C, "OUTCOME UNKNOWN",
             {"capability": change}, KEPT_BUT_ROLLED_BACK, kind="contradiction"),
        Case("a verdict of kept beside `applied: false`", C, "OUTCOME UNKNOWN",
             {"capability": change}, KEPT_BUT_NOT_APPLIED, kind="contradiction"),
        Case("a verdict of kept beside asked-and-not-kept", C, "OUTCOME UNKNOWN",
             {"capability": change}, KEPT_BUT_ASKED_AND_DECLINED, kind="contradiction"),
        Case("a verdict of discarded beside `kept`", C, "OUTCOME UNKNOWN",
             {"capability": change}, DISCARDED_BUT_KEPT, kind="contradiction"),
        Case("a verdict of discarded beside `applied`", C, "OUTCOME UNKNOWN",
             {"capability": change}, DISCARDED_BUT_APPLIED, kind="contradiction"),
        Case("Heron's own code raising after the change was sent", C,
             "OUTCOME UNKNOWN", {"capability": change}, KEPT,
             setup=breaks_after_asking, kind="raised"),
        Case("the request itself raising, on a change", C, "OUTCOME UNKNOWN",
             {"capability": change}, KEPT, setup=pipe_breaks, kind="raised"),
        Case("an answer that is a JSON list, on a change", C, "OUTCOME UNKNOWN",
             {"capability": change}, [1, 2, 3], kind="lost"),
        Case("an answer that is a JSON string, on a change", C, "OUTCOME UNKNOWN",
             {"capability": change}, "ok", kind="lost"),
        Case("a change that came back from ANOTHER model than the pinned one", C,
             "OUTCOME UNKNOWN", {"capability": change},
             dict(KEPT, document="Tower B", documentPath="C:/jobs/TowerB.rvt",
                  projectKey="key-2"), pin=MODEL),
        Case("Revit busy, so it never took the change (revit_busy)", C, "NOT RUN",
             {"capability": change}, error("revit_busy"), kind="not run"),
        Case("another chat holds that model (model_in_use)", C, "NOT RUN",
             {"capability": change}, error("model_in_use")),
        Case("words that settled no one capability, at revit_change", C, "NOT RUN",
             say, KEPT, setup=lookup_ranked, kind="not run"),
        Case("words nothing in Heron matches, at revit_change", C, "NOT RUN",
             say, KEPT, setup=lookup_nothing),
        Case("words, with Heron's library unreadable, at revit_change", C,
             "NOT RUN", say, KEPT, setup=lookup_unreadable),

        # ---- the read door ----------------------------------------------
        Case("a read Revit answered", R, "ANSWERED", {"capability": read},
             READ_OK, kind="answered"),
        Case("a read that consumed what an earlier one left", R, "ANSWERED",
             {"capability": read, "expect_from": "find-views"}, READ_OK),
        Case("a change sent to revit_read", R, "REFUSED", {"capability": change},
             READ_OK, kind="refused"),
        Case("nothing by that name, at revit_read", R, "NOT RUN",
             {"capability": "NO_SUCH_CAPABILITY_AT_ALL"}, READ_OK),
        Case("no Revit session, at revit_read", R, "NOT RUN",
             {"capability": read}, READ_OK, setup=not_bound),
        Case("a read whose code writes to disk (disk_refusal)", R, "REFUSED",
             {"capability": read}, READ_OK,
             setup=patched(tools, "disk_refusal",
                           lambda *_a, **_k: "It writes a file, so nothing has been "
                                             "sent to Revit.")),
        Case("a read whose contract cannot be read", R, "REFUSED",
             {"capability": read}, READ_OK,
             setup=patched(bridge, "fragment_needs", lambda *_a, **_k: None)),
        Case("a read with no code behind it", R, "REFUSED",
             {"capability": read}, READ_OK, setup=no_code),
        Case("a value the caller did not give (needs_request_values)", R,
             "REFUSED", {"capability": read}, error("needs_request_values")),
        Case("the add-in's wrong-model refusal, at revit_read", R, "REFUSED",
             {"capability": read}, error("wrong_document"), kind="refused"),
        Case("a read answered from ANOTHER model than the pinned one", R,
             "REFUSED", {"capability": read}, ELSEWHERE, pin=MODEL, kind="refused"),
        Case("a read that threw (fragment_threw)", R, "NOT ANSWERED",
             {"capability": read}, error("fragment_threw"), kind="not answered"),
        Case("a read Heron's run clock stopped (ran_too_long)", R, "NOT ANSWERED",
             {"capability": read}, error("ran_too_long"), kind="not answered"),
        Case("a read a Revit dialog stopped (dialog_refused)", R, "NOT ANSWERED",
             {"capability": read}, error("dialog_refused"), kind="not answered"),
        Case("the bridge's own handler failed, on a read (handler_failed)", R,
             "OUTCOME UNKNOWN", {"capability": read}, error("handler_failed"),
             kind="unrecognised"),
        Case("a failure code Heron has never seen, on a read", R,
             "OUTCOME UNKNOWN", {"capability": read}, error("never_seen_before"),
             kind="unrecognised"),
        Case("a reply with no `ok` and no code, on a read", R, "OUTCOME UNKNOWN",
             {"capability": read}, {}, kind="unrecognised"),
        Case("Heron's own code raising after the read was sent", R,
             "OUTCOME UNKNOWN", {"capability": read}, READ_OK,
             setup=breaks_after_asking, kind="raised"),
        Case("an answer that is a JSON list, on a read", R, "OUTCOME UNKNOWN",
             {"capability": read}, [1, 2, 3], kind="lost"),
        Case("an answer that is a JSON list, on a consuming read", R,
             "OUTCOME UNKNOWN", {"capability": read, "expect_from": "find-views"},
             [1, 2, 3], kind="raised"),
        Case("a read still running in Revit", R, "STILL RUNNING",
             {"capability": read}, error("still_running"), kind="running"),
        Case("NO ANSWER AT ALL to a read", R, "OUTCOME UNKNOWN",
             {"capability": read}, None, kind="lost"),
        Case("a read's answer TOO LARGE to read", R, "OUTCOME UNKNOWN",
             {"capability": read}, error("reply_too_large"), kind="lost"),
        Case("a read's answer that could NOT BE READ", R, "OUTCOME UNKNOWN",
             {"capability": read}, error("reply_unreadable"), kind="lost"),
        Case("a consuming read whose answer was LOST", R, "OUTCOME UNKNOWN",
             {"capability": read, "expect_from": "find-views"},
             error("unknown_outcome"), kind="lost"),
        Case("a read counted changing the model (read_changed_model)", R,
             "OUTCOME UNKNOWN", {"capability": read}, error("read_changed_model")),
        Case("Revit busy, so it never took the read", R, "NOT RUN",
             {"capability": read}, error("revit_busy"), kind="not run"),
        Case("words that settled no one capability, at revit_read", R, "NOT RUN",
             say, READ_OK, setup=lookup_ranked, kind="not run"),
        Case("words, with Heron's library unreadable, at revit_read", R,
             "NOT RUN", say, READ_OK, setup=lookup_unreadable),
        Case("a read answered, this chat running old code", R, "ANSWERED",
             {"capability": read}, READ_OK, setup=old_code),

        # ---- the show door ----------------------------------------------
        Case("a show Revit ran", S, "ANSWERED", {"capability": show}, READ_OK,
             kind="answered"),
        Case("a change sent to revit_show", S, "REFUSED", {"capability": change},
             READ_OK, kind="refused"),
        Case("nothing by that name, at revit_show", S, "NOT RUN",
             {"capability": "NO_SUCH_CAPABILITY_AT_ALL"}, READ_OK),
        Case("no Revit session, at revit_show", S, "NOT RUN",
             {"capability": show}, READ_OK, setup=not_bound),
        Case("a show whose contract cannot be read", S, "REFUSED",
             {"capability": show}, READ_OK,
             setup=patched(bridge, "fragment_needs", lambda *_a, **_k: None)),
        Case("a show with no code behind it", S, "REFUSED",
             {"capability": show}, READ_OK, setup=no_code),
        Case("a show whose code writes to disk (disk_refusal)", S, "REFUSED",
             {"capability": show}, READ_OK,
             setup=patched(tools, "disk_refusal",
                           lambda *_a, **_k: "It writes a file, so nothing has been "
                                             "sent to Revit.")),
        Case("the add-in's wrong-model refusal, at revit_show", S, "REFUSED",
             {"capability": show}, error("wrong_document")),
        Case("a show that ran in ANOTHER model than the pinned one", S,
             "OUTCOME UNKNOWN", {"capability": show}, ELSEWHERE, pin=MODEL),
        Case("a show that threw - its selection may stand", S, "OUTCOME UNKNOWN",
             {"capability": show}, error("fragment_threw"), kind="show stopped"),
        Case("a show Heron's run clock stopped", S, "OUTCOME UNKNOWN",
             {"capability": show}, error("ran_too_long"), kind="show stopped"),
        Case("a show a Revit dialog stopped", S, "OUTCOME UNKNOWN",
             {"capability": show}, error("dialog_refused"), kind="show stopped"),
        Case("the bridge's own handler failed, on a show (handler_failed)", S,
             "OUTCOME UNKNOWN", {"capability": show}, error("handler_failed"),
             kind="unrecognised"),
        Case("a reply with no `ok` and no code, on a show", S, "OUTCOME UNKNOWN",
             {"capability": show}, {}, kind="unrecognised"),
        Case("Heron's own code raising after the show was sent", S,
             "OUTCOME UNKNOWN", {"capability": show}, READ_OK,
             setup=breaks_after_asking, kind="raised"),
        Case("an answer that is a JSON number, on a show", S, "OUTCOME UNKNOWN",
             {"capability": show}, 7, kind="lost"),
        Case("a show still running in Revit", S, "STILL RUNNING",
             {"capability": show}, error("still_running")),
        Case("NO ANSWER AT ALL to a show", S, "OUTCOME UNKNOWN",
             {"capability": show}, None, kind="lost"),
        Case("a consuming show whose answer was LOST", S, "OUTCOME UNKNOWN",
             {"capability": show, "expect_from": "find-views"},
             error("unknown_outcome"), kind="lost"),
        Case("Revit busy, so it never took the show", S, "NOT RUN",
             {"capability": show}, error("revit_busy")),
        Case("words that settled no one capability, at revit_show", S, "NOT RUN",
             say, READ_OK, setup=lookup_ranked),

        # ---- revit_undo_last (D-157) -------------------------------------
        # Not a door: it takes no capability. Every return it has, each with
        # the chat pinned to MODEL unless the case is about having no pin.
        Case("Revit's Undo SENT - Revit runs it after the reply", U, "STILL RUNNING",
             {}, UNDO_SENT, pin=MODEL, kind="undo sent"),
        Case("a check that would send, and sent nothing", U, "ANSWERED",
             {"check_only": True}, UNDO_CHECK, pin=MODEL, kind="undo check"),
        Case("a check that would NOT send", U, "ANSWERED", {"check_only": True},
             dict(UNDO_CHECK, wouldSend=False, said="No - Undo would not be sent now."),
             pin=MODEL, kind="undo check"),
        Case("the guard's refusal: something was done after it (undo_refused)", U,
             "REFUSED", {}, error("undo_refused"), pin=MODEL, kind="undo refused"),
        Case("the Changes switch off, at revit_undo_last", U, "REFUSED", {},
             error("write_disabled"), pin=MODEL, kind="undo refused"),
        Case("the add-in's wrong-model refusal, at revit_undo_last", U, "REFUSED", {},
             error("wrong_document"), pin=MODEL, kind="undo refused"),
        Case("no model open, at revit_undo_last", U, "REFUSED", {},
             error("no_document"), pin=MODEL, kind="undo refused"),
        Case("a chat pinned to no model: refused before anything is sent", U,
             "REFUSED", {}, UNDO_SENT, kind="undo refused"),
        Case("this chat running old code, at revit_undo_last", U, "REFUSED", {},
             UNDO_SENT, pin=MODEL, setup=old_code, kind="undo refused"),
        Case("a check answered from ANOTHER model than the pinned one", U, "REFUSED",
             {"check_only": True},
             dict(UNDO_CHECK, document="Tower B", documentPath="C:/jobs/TowerB.rvt",
                  projectKey="key-2"), pin=MODEL),
        Case("no Revit session, at revit_undo_last", U, "NOT RUN", {}, UNDO_SENT,
             pin=MODEL, setup=not_bound, kind="undo not run"),
        Case("an add-in older than undo_last (unknown_op)", U, "NOT RUN", {},
             error("unknown_op"), pin=MODEL, kind="undo not run"),
        Case("Revit busy, so it never took the Undo request", U, "NOT RUN", {},
             error("revit_busy"), pin=MODEL, kind="undo not run"),
        Case("another chat holds that model, at revit_undo_last", U, "NOT RUN", {},
             error("model_in_use"), pin=MODEL, kind="undo not run"),
        Case("NO ANSWER AT ALL to revit_undo_last", U, "OUTCOME UNKNOWN", {}, None,
             pin=MODEL, kind="undo unknown"),
        Case("NO ANSWER AT ALL to a check", U, "OUTCOME UNKNOWN", {"check_only": True},
             None, pin=MODEL, kind="undo unknown"),
        Case("an answer LOST after the Undo request was sent", U, "OUTCOME UNKNOWN", {},
             error("unknown_outcome"), pin=MODEL, kind="undo unknown"),
        Case("Undo SENT in ANOTHER model than the pinned one", U, "OUTCOME UNKNOWN", {},
             dict(UNDO_SENT, document="Tower B", documentPath="C:/jobs/TowerB.rvt",
                  projectKey="key-2"), pin=MODEL, kind="undo elsewhere"),
        Case("an `ok` that does not say Undo was sent", U, "OUTCOME UNKNOWN", {},
             dict(MODEL, ok=True), pin=MODEL, kind="undo unknown"),
        Case("a failure code Heron has never seen, at revit_undo_last", U,
             "OUTCOME UNKNOWN", {}, error("never_seen_before"), pin=MODEL,
             kind="undo unknown"),
        Case("an answer that is a JSON list, at revit_undo_last", U, "OUTCOME UNKNOWN",
             {}, [1, 2, 3], pin=MODEL, kind="undo unknown"),
        Case("the request itself raising, at revit_undo_last", U, "OUTCOME UNKNOWN", {},
             UNDO_SENT, pin=MODEL, setup=pipe_breaks, kind="undo raised"),
    ]
    return picked, cases


# THE RETURNS NO CASE ABOVE REACHES, said rather than passed over. Each is a
# plain refusal before anything is sent - `return _ended("refused", ...)` or
# the hand-back - so its word is the one its neighbours above were seen to
# get; that is an argument from the source, not a run.
NOT_REACHED = (
    "revit_show: a READ sent to it, when heron_tools.door_refusal sends one back "
    "(the ISOLATE rule) - no card in the library both passes the ceiling and trips it",
    "the three doors: words that match a PROVEN phrasing exactly and are short of a "
    "value (`_by_words`' `exact` branches) - the hand-back they end in is the one "
    "the ranked case reaches, by the same return",
    "a door's body that raises BEFORE anything is sent: the SDK answers for it "
    "(`isError`, and the exception's text stays on the server), so there is no reply "
    "to carry a line - section 3b sees it still raise, and sees that nothing was sent",
)


# ------------------------------------------------------------- the checks

def the_vocabulary(core, registry):
    print()
    print("1. One vocabulary, in one place")
    table = getattr(core, "OUTCOMES", None)
    check(table is not None, "heron_server_core.OUTCOMES is the table")
    rows = list(table or ())
    words = [row[0] for row in rows]
    check(sorted(words) == sorted(WORDS),
          "it holds exactly the eight words a host is told - found %r" % (words,))
    check(len(set(words)) == len(words), "each once")
    check(all(len(row) == 3 and (row[2] or "").strip() for row in rows),
          "and each with a plain sentence saying what it means")
    told = dict((row[0], row[1]) for row in rows if len(row) == 3)
    # EVERY WORD, KEPT AND ANSWERED TOO (the review of 2026-11-12). This check
    # held the opposite - that those two add nothing - on the argument that
    # the reply below says the rest. A chat reads the line with nothing to
    # explain it, so each word says what it means, alone.
    check(all((told.get(word) or "").strip() for word in WORDS),
          "every word's line goes on to say what it means - a chat is given "
          "nothing else to read the word by")
    kept_says = told.get("KEPT") or ""
    check("undo" not in kept_says.lower() and "Ctrl" not in kept_says
          and "if anything" in kept_says,
          "KEPT claims only that the transaction was kept - never that anything "
          "changed, and never an undo step that may not exist - found %r" % kept_says)
    unknown_says = told.get("OUTCOME UNKNOWN") or ""
    check("look at the model" in unknown_says and "again" in unknown_says
          and "reply" not in unknown_says,
          "OUTCOME UNKNOWN says it inline - look at the model, and send no change "
          "again - and leans on no sentence of the reply - found %r" % unknown_says)
    rolled_says = told.get("ROLLED BACK") or ""
    check("asked Revit to roll" in rolled_says and "reply" not in rolled_says
          and "confirmed" not in rolled_says,
          "ROLLED BACK says what is known - Heron asked Revit to roll the whole job "
          "back - and promises nothing of what the reply says - found %r" % rolled_says)
    meant = dict((row[0], row[2]) for row in rows if len(row) == 3)
    check("REFUSED" in (meant.get("NOT RUN") or ""),
          "the table says in one sentence how NOT RUN differs from REFUSED")
    running = told.get("STILL RUNNING") or ""
    check("again" in running and "id" in running,
          "STILL RUNNING says not to send it again, and that no id follows a "
          "run yet - found %r" % running)
    check("wf-" not in running and "run=" not in running,
          "and invents none")
    # THIS HELD "== DOORS" UNTIL 2026-11-12. revit_undo_last carried no line
    # at all - a refusal, an Undo sent and a lost answer were told apart by
    # reading sentences, which is what row 5b-1159 was (row 5b-1610) - so the
    # list is the three doors and that one tool, and a fifth name fails here.
    check(tuple(getattr(registry, "STATES_OUTCOME", ())) == SAYS,
          "heron_tools.STATES_OUTCOME names the tools that say one: the three doors, "
          "and revit_undo_last - found %r" % (getattr(registry, "STATES_OUTCOME", ()),))
    line = getattr(core, "outcome_line", None)
    read_back = getattr(core, "outcome_of", None)
    check(line is not None and read_back is not None,
          "the core writes the line and reads it back (outcome_line, outcome_of)")
    if line is None or read_back is None:
        return
    check(all(line(word).startswith(PREFIX + word) for word in WORDS),
          "every line is `outcome: ` and then the word")
    check(all(read_back("First sentence.\n%s\nthe rest" % line(word)) == word
              for word in WORDS),
          "and every word reads back off the second line of a reply")
    check(read_back("First sentence.\nthe rest") is None
          and read_back("outcome: KEPT\nFirst sentence.") is None
          and read_back("First sentence.\noutcome: MADE UP") is None,
          "a reply with no line there, or a word that is not in the table, "
          "reads back as none")

    # THE PAGE A PERSON READS IS HELD TO THE TABLE THE CODE READS.
    with io.open(os.path.join(ROOT, "docs", "04-heron-mcp.md"), encoding="utf-8") as fh:
        page = fh.read()
    at = page.find("### 8.12 ")
    printed = re.findall(r"^\s*\| `([A-Z ]+)` \|", page[at:] if at >= 0 else "", re.M)
    check(at >= 0 and printed == words,
          "docs/04 section 8.12 prints the table's words, in the table's order - "
          "found %r" % (printed,))


def a_word_per_ending(stage, cases):
    print()
    print("2. A word per way of ending, through the door itself")
    replies = {}
    for case in cases:
        said = case.run(stage)
        replies[case] = said
        check(stated(said) == case.word,
              "%-13s %-15s %s - found %s"
              % (case.door, case.word, case.name,
                 stated(said) or "no outcome line: %r" % (said or "")[:70]))
    return replies


def never_more_than_known(stage, core, cases, replies):
    print()
    print("3. Never more than is known")
    lost = [c for c in cases if c.kind == "lost"]
    check(len(lost) >= 8 and all(stated(replies[c]) == "OUTCOME UNKNOWN" for c in lost),
          "a lost, oversize or unreadable answer is OUTCOME UNKNOWN at every door "
          "(%d cases) - found %r" % (len(lost), sorted(set(str(stated(replies[c]))
                                                            for c in lost))))
    check(not any(stated(replies[c]) in ("KEPT", "REFUSED", "ANSWERED", "NOT RUN")
                  for c in lost),
          "and never KEPT, REFUSED, ANSWERED or NOT RUN")
    only_kept = [c.name for c in cases if stated(replies[c]) == "KEPT"]
    check(sorted(only_kept) == sorted(c.name for c in cases
                                      if (c.reply is KEPT or c.reply is ASKED_KEPT)
                                      and c.word == "KEPT"),
          "KEPT is said only of a reply whose own verdict is `kept` - found %r"
          % (only_kept,))
    running = [c for c in cases if c.kind == "running"]
    check(running and all("again" in replies[c].split("\n")[1] for c in running
                          if len(replies[c].split("\n")) > 1),
          "a run still going says, on the line itself, not to send it again")

    # EVERY CODE heron_failure HAS NO ROW FOR, AT EVERY DOOR (the review's
    # blocker). On a read it files one under "refused", and `handler_failed`
    # is raised by the bridge after the job may have run.
    strange = [c for c in cases if c.kind == "unrecognised"]
    check(len(strange) >= 7 and sorted(set(c.door for c in strange)) == sorted(DOORS)
          and all(stated(replies[c]) == "OUTCOME UNKNOWN" for c in strange),
          "a code nothing recognises, and a reply with none, is OUTCOME UNKNOWN at "
          "all three doors (%d cases) - found %r"
          % (len(strange), sorted(set("%s %s" % (c.door, stated(replies[c]))
                                      for c in strange))))
    torn = [c for c in cases if c.kind == "contradiction"]
    check(len(torn) >= 5 and all(stated(replies[c]) == "OUTCOME UNKNOWN" for c in torn),
          "a verdict the fields beside it contradict is OUTCOME UNKNOWN, never the "
          "verdict (%d cases) - found %r"
          % (len(torn), sorted(set(str(stated(replies[c])) for c in torn))))
    rolled = [c for c in cases if stated(replies[c]) == "ROLLED BACK"]
    check(rolled and all(c.door == "revit_change" for c in rolled),
          "ROLLED BACK is said only by the door that opens something to roll back "
          "- found it at %r" % sorted(set(c.door for c in rolled)))
    check(all(stated(replies[c]) == "NOT ANSWERED" for c in cases
              if c.kind == "not answered")
          and all(stated(replies[c]) == "OUTCOME UNKNOWN" for c in cases
                  if c.kind == "show stopped"),
          "a read that stopped is NOT ANSWERED, and a show that stopped is OUTCOME "
          "UNKNOWN - its selection may stand")

    # THE LINE FAILS CLOSED. A door that returns having said nothing, or only
    # that it was "ok", is never guessed to have worked.
    said = getattr(core, "_said_outcome", None)
    check(said is not None, "the core has the one function that adds the line")
    if said is None:
        return
    for label in (None, "ok", "failed", "something new"):
        got = stated(said("revit_change", "It ran.", label, None))
        check(got == "OUTCOME UNKNOWN",
              "a door that said %r and no word is OUTCOME UNKNOWN - found %r"
              % (label, got))
    check(stated(said("revit_change", "It ran.", "ok", "INVENTED")) == "OUTCOME UNKNOWN",
          "and so is a word that is not in the table")
    check(stated(said("revit_read", "No.", "refused", None)) == "REFUSED"
          and stated(said("revit_read", "Choose.", "handed_back", None)) == "NOT RUN",
          "a refusal and a hand-back are the two labels that are a word by "
          "themselves")
    check(stated(said("revit_change", "It threw.", "failed", "ROLLED BACK")) == "ROLLED BACK"
          and stated(said("revit_read", "It threw.", "failed", "ROLLED BACK")) == "NOT ANSWERED"
          and stated(said("revit_show", "It threw.", "failed", "ROLLED BACK"))
          == "OUTCOME UNKNOWN",
          "ROLLED BACK said at a door that opens no transaction is never printed: "
          "NOT ANSWERED at revit_read, OUTCOME UNKNOWN at revit_show")
    check(said("revit_health", "Connected.", "ok", None) == "Connected."
          and said("revit_read", ["words", object()], "ok", None)[0] == "words",
          "a tool that is not on the list, and an answer that is not text, are "
          "handed back untouched")


def every_path_one_line(cases, replies, core):
    print()
    print("4. Every path: exactly one line, the second, its word in the table")
    table = [row[0] for row in (getattr(core, "OUTCOMES", None) or ())]
    one, second, known, crashed = [], [], [], []
    for case in cases:
        said = replies[case]
        found = lines_of(said)
        if said.startswith("(%s raised " % case.door):
            crashed.append(case.name)
        if len(found) != 1:
            one.append(case.name)
            continue
        if found[0][0] != 1:
            second.append("%s (line %d)" % (case.name, found[0][0] + 1))
        if stated(said) not in table:
            known.append(case.name)
    check(not crashed, "no door crashed on any of the %d paths - %s"
          % (len(cases), "; ".join(crashed) or "none did"))
    check(not one, "each of the %d replies carries exactly one outcome line - not: %s"
          % (len(cases), "; ".join(one[:6]) or "none"))
    check(not second, "it is the reply's second line, under its opening sentence "
          "- not: %s" % ("; ".join(second[:6]) or "none"))
    check(not known, "and its word is one of the table's - not: %s"
          % ("; ".join(known[:6]) or "none"))
    by_door = dict((door, len([c for c in cases if c.door == door])) for door in DOORS)
    check(all(by_door[door] >= 15 for door in DOORS),
          "walked: %s" % ", ".join("%s %d paths" % (door, by_door[door])
                                   for door in DOORS))
    print("       NOT REACHED, and said so:")
    for path in NOT_REACHED:
        print("         - %s" % path)


def additive(stage, cases, replies, core):
    print()
    print("5. Additive: take the line out and the door's own reply is left")
    said_old = getattr(getattr(core, "running_code", None), "SAID", None)
    differs = []
    for case in cases:
        if case.kind in ("raised", "undo raised"):
            # THE BODY RAISED: there is no reply of its own to compare, and
            # the whole answer is the wrapper's. Section 3b reads it.
            continue
        body = case.run(stage, body=True)
        whole = replies[case]
        if said_old and body != whole and whole.endswith(said_old) \
                and said_old not in body:
            # THE ONE LINE THE WRAPPER ALREADY ADDED, before this change: a
            # read answered by a chat running old code ends by saying so.
            body = body + "\n\n" + said_old
        if without_line(whole) != body:
            differs.append(case.name)
        if lines_of(body):
            differs.append("%s (its body carries a line of its own)" % case.name)
    compared = len([c for c in cases if c.kind not in ("raised", "undo raised")])
    check(not differs,
          "all %d: every sentence the door wrote is there, in the same order - "
          "not: %s" % (compared, "; ".join(differs[:5]) or "none"))


def breaks_after_sending(stage, cases, replies, core):
    print()
    print("3b. A door that breaks after its request has left still answers")
    module = stage.module
    raised = [c for c in cases if c.kind == "raised"]
    check(len(raised) >= 5 and sorted(set(c.door for c in raised)) == sorted(DOORS),
          "forced at all three doors (%d cases)" % len(raised))
    check(all(not replies[c].startswith("(%s raised " % c.door) for c in raised),
          "none of them reaches the SDK as an exception - found %r"
          % [replies[c][:60] for c in raised if replies[c].startswith("(")])
    check(all(stated(replies[c]) == "OUTCOME UNKNOWN" for c in raised),
          "each is OUTCOME UNKNOWN")
    check(all("Look at the model" in replies[c] for c in raised),
          "each sends a person to the model before anything is sent again")
    check(all("Error executing tool" not in replies[c] for c in raised),
          "and none is the SDK's bare sentence, which says nothing of what Revit did")
    names = ("RuntimeError", "OSError", "AttributeError")
    check(all(any(name in replies[c] for name in names) for c in raised)
          and not any("a fault in Heron's own code" in replies[c]
                      or "the pipe broke" in replies[c] for c in raised),
          "the reply names the KIND of fault and not its text - the text stays on "
          "the server, as the SDK keeps a crash's")

    # WHAT RAISED IS RECORDED: the traceback, text and all, in the server's
    # own log - and the call on the Companion's activity list, as a failure.
    try:
        activity = module._companion_module().ACTIVITY
        before = (activity.since(0) or [{"seq": 0}])[-1]["seq"]
        raised[0].run(stage)
        check("Traceback" in raised[0].logged
              and "a fault in Heron's own code" in raised[0].logged
              and raised[0].door in raised[0].logged,
              "the server's log has the traceback, the fault's own words and the "
              "door it broke in - found %r" % raised[0].logged[:90])
        mine = [i for i in activity.since(before) if i.get("tool") == raised[0].door]
        check(len(mine) == 1 and mine[0].get("outcome") == "failed",
              "the activity list records it once, as failed - found %r" % (mine,))
    except Exception as exc:                                    # noqa: BLE001
        check(False, "the activity list can be read: %s: %s" % (type(exc).__name__, exc))

    # BEFORE ANYTHING WAS SENT, IT IS THE SDK'S TO REPORT, AS IT ALWAYS WAS -
    # and nothing was sent, which is why saying so costs nothing.
    picked_change = [c for c in cases if c.name == "a change Revit kept"][0]

    def boom(*_a, **_k):
        raise RuntimeError("a fault before anything was sent")
    stage.fresh(dict(KEPT))
    stage.patch(module.bridge, "fragment_needs", boom)
    try:
        said = module.revit_change(**picked_change.arguments)
        early = "returned %r" % said[:60]
    except RuntimeError:
        early = "raised"
    finally:
        stage.unpatch()
    check(early == "raised" and not stage.revit.asked,
          "a fault BEFORE the request leaves still raises, and nothing was sent - "
          "found: %s, asked %d" % (early, len(stage.revit.asked)))


async def through_the_sdk(stage, cases, picked):
    print()
    print("6. Through the SDK's own dispatch: structured, and never an error")
    module = stage.module
    server = module.server

    def parts(result):
        """(text, structured content, flagged as an error) - 1.x and 2.x."""
        structured = None
        if isinstance(result, tuple) and len(result) == 2 and isinstance(result[1], dict):
            result, structured = result
        blocks = getattr(result, "content", None)
        if blocks is None:
            blocks = result if isinstance(result, (list, tuple)) else [result]
        text = "\n".join(getattr(b, "text", str(b)) for b in blocks)
        if structured is None:
            structured = (getattr(result, "structured_content", None)
                          or getattr(result, "structuredContent", None))
        flagged = bool(getattr(result, "is_error", False)
                       or getattr(result, "isError", False))
        return text, structured, flagged

    wanted = ("kept", "refused", "rollback", "lost", "running", "answered", "not run",
              "raised", "unrecognised", "not answered",
              "undo sent", "undo check", "undo refused", "undo not run", "undo unknown",
              "undo elsewhere", "undo raised")
    seen = set()
    for case in cases:
        if case.kind not in wanted or (case.kind, case.door) in seen:
            continue
        seen.add((case.kind, case.door))
        stage.fresh(case.reply, case.pin)
        had, sys.stderr = sys.stderr, io.StringIO()
        try:
            if case.setup is not None:
                case.setup(stage)
            try:
                result = await server.call_tool(case.door, dict(case.arguments))
            except Exception as exc:                            # noqa: BLE001
                check(False, "%s could not be called: %s: %s"
                      % (case.door, type(exc).__name__, exc))
                continue
        finally:
            sys.stderr = had
            stage.unpatch()
        text, structured, flagged = parts(result)
        structured = structured if isinstance(structured, dict) else {}
        check(stated(text) == case.word and structured.get("outcome") == case.word,
              "%-13s %-15s the text says it and so does structured `outcome` - "
              "found %r" % (case.door, case.word, structured.get("outcome")))
        check(structured.get("result") == text,
              "              and `result` is still the whole reply, as the SDK "
              "has always sent it")
        check(not flagged,
              "              and it is NOT flagged isError - the tool worked, "
              "whatever Revit answered")

    listed = dict((t.name, t) for t in await server.list_tools())
    for door in DOORS:
        tool = listed.get(door)
        schema = (getattr(tool, "outputSchema", None)
                  or getattr(tool, "output_schema", None) or {})
        check(tool is not None and sorted((schema.get("properties") or {})) == ["result"]
              and schema.get("additionalProperties") is not False,
              "%s is listed with the output schema it always had - `result`, and "
              "nothing that forbids a key beside it" % door)

    stage.fresh(dict(READ_OK))
    try:
        health = await server.call_tool("revit_health", {})
        _text, structured, _flag = parts(health)
        check(isinstance(structured, dict) and "outcome" not in structured,
              "a tool that is not a door gets no `outcome` in its structured "
              "content")
    except Exception as exc:                                    # noqa: BLE001
        check(False, "revit_health could not be called: %s: %s"
              % (type(exc).__name__, exc))


def nothing_else_moved(stage, cases, replies, picked):
    print()
    print("7. Nothing else moved")
    module = stage.module
    try:
        activity = module._companion_module().ACTIVITY
    except Exception as exc:                                    # noqa: BLE001
        check(False, "the Companion's activity list can be read: %s: %s"
              % (type(exc).__name__, exc))
        return

    def newest():
        items = activity.since(0)
        return items[-1]["seq"] if items else 0

    wanted = {"a change Revit kept": "ok",
              "the Changes switch off (write_disabled)": "refused",
              "a change the modeller DISCARDED, the rollback confirmed": "refused",
              "the fragment threw and the job was rolled back": "failed",
              "NO ANSWER AT ALL to a change": "failed",
              "words that settled no one capability, at revit_change": "handed_back",
              "a read Revit answered": "ok"}
    wrong = []
    for case in cases:
        if case.name not in wanted:
            continue
        before = newest()
        said = case.run(stage)
        mine = [i for i in activity.since(before) if i.get("tool") == case.door]
        first = said.split("\n")[0]
        if not (len(mine) == 1 and mine[0].get("outcome") == wanted[case.name]
                and mine[0].get("summary", "").rstrip(u"\u2026")
                and first.startswith(mine[0]["summary"].rstrip(u"\u2026"))
                and not mine[0]["summary"].startswith(PREFIX)):
            wrong.append("%s: %r" % (case.name, mine))
    check(not wrong,
          "the activity list shows each call once, with the reply's own first "
          "sentence and the label the tool gave - not: %s"
          % ("; ".join(wrong[:3]) or "none"))

    stage.fresh(dict(READ_OK))
    said = module.revit_health()
    check(not lines_of(said), "revit_health carries no outcome line")
    levels = getattr(module, "revit_levels", None)
    if levels is None:
        import heron_inventory
        levels = getattr(heron_inventory, "revit_levels", None)
    stage.fresh({"ok": True, "levels": [], "document": "Project1",
                 "documentPath": "C:/jobs/Project1.rvt", "projectKey": "key-1"})
    try:
        said = levels() if levels is not None else None
    except Exception as exc:                                    # noqa: BLE001
        said = "(revit_levels raised %s: %s)" % (type(exc).__name__, exc)
    check(said is not None and not lines_of(said),
          "an inventory read (revit_levels) carries none")
    stage.fresh(dict(READ_OK))
    try:
        said = module.heron_lookup(request="count the ducts")
    except Exception as exc:                                    # noqa: BLE001
        said = "(heron_lookup raised %s: %s)" % (type(exc).__name__, exc)
    check(not lines_of(said), "a library tool (heron_lookup) carries none")


def the_undo_tool(stage, cases, replies):
    """
    Section 9: revit_undo_last (D-157), the one tool that is not a door and
    says a word. Its every return was walked above with the doors'; this
    holds what is its own.
    """
    print()
    print("9. revit_undo_last: sent is not done, a check sends nothing, and no pin sends nothing")
    module = stage.module
    mine = [c for c in cases if c.door == UNDO_TOOL]
    check(len(mine) >= 20, "walked: %s %d paths" % (UNDO_TOOL, len(mine)))

    sent = [c for c in mine if c.kind == "undo sent"]
    check(sent and all(stated(replies[c]) == "STILL RUNNING" for c in sent)
          and all(replies[c].startswith("Revit's own Undo was sent") for c in sent),
          "Undo posted is STILL RUNNING - Revit runs it after the reply - and opens by "
          "saying it was SENT")
    never = sorted(set(str(stated(replies[c])) for c in mine)
                   & set(["KEPT", "ROLLED BACK", "NOT ANSWERED"]))
    check(not never,
          "it never says KEPT, ROLLED BACK or NOT ANSWERED - it keeps nothing, rolls "
          "nothing back and is no read: found %s" % (never or "none"))
    answered = [c for c in mine if stated(replies[c]) == "ANSWERED"]
    check(answered and all(c.arguments.get("check_only") for c in answered),
          "ANSWERED is said only of a check - never of an Undo that was sent")

    refused = [c for c in mine if c.kind == "undo refused"]
    check(len(refused) >= 5 and all(stated(replies[c]) == "REFUSED" for c in refused),
          "each refusal - the guard's, the switch, the wrong model, no model, no pin, old "
          "code - is REFUSED (%d cases)" % len(refused))
    not_run = [c for c in mine if c.kind == "undo not run"]
    check(len(not_run) >= 4 and all(stated(replies[c]) == "NOT RUN" for c in not_run),
          "no session, an older add-in, Revit busy and a model held by another chat are "
          "NOT RUN (%d cases)" % len(not_run))
    older = [c for c in mine if "unknown_op" in c.name]
    check(older and all("older than this chat's Heron" in replies[c]
                        and "Nothing was sent to Revit" in replies[c] for c in older),
          "an older add-in is said in Revit's words, with nothing sent")
    unknown = [c for c in mine if c.kind in ("undo unknown", "undo elsewhere", "undo raised")]
    check(len(unknown) >= 7 and all(stated(replies[c]) == "OUTCOME UNKNOWN" for c in unknown),
          "no answer, an answer lost or unreadable, an `ok` that does not say it sent, a "
          "code nobody knows, Undo sent in another model and a break after sending are "
          "OUTCOME UNKNOWN (%d cases)" % len(unknown))
    elsewhere = [c for c in mine if c.kind == "undo elsewhere"]
    check(elsewhere and all("Revit's Undo WAS SENT" in replies[c] and "Tower B" in replies[c]
                            and "Nothing has been sent to Revit" not in replies[c]
                            for c in elsewhere),
          "an Undo sent in another model says it WAS sent, and where - never that "
          "nothing was sent")

    # WHAT WAS ASKED OF REVIT, read off the stand-in.
    stage.fresh(dict(UNDO_SENT))
    said = module.revit_undo_last()
    check(not stage.revit.asked and stated(said) == "REFUSED"
          and "Nothing has been sent to Revit" in said,
          "with no pin NOTHING is sent - asked %d" % len(stage.revit.asked))
    stage.fresh(dict(UNDO_CHECK), MODEL)
    try:
        module.revit_undo_last(check_only=True)
    except TypeError:
        # The tool as it stood takes no `check_only` (heron-ship 2a: fail,
        # not crash) - nothing was asked, and the check below says so.
        pass
    asked =[(op, idempotent) for op, _args, idempotent in stage.revit.asked]
    check(asked == [("undo_check", True)],
          "a check sends undo_check, once, and never undo_last - found %r" % (asked,))
    stage.fresh(dict(UNDO_SENT), MODEL)
    module.revit_undo_last()
    asked = [(op, idempotent, sorted(args)) for op, args, idempotent in stage.revit.asked]
    check(asked == [("undo_last", False, ["document", "documentPath", "expectProject"])],
          "the real one sends undo_last once, never safe to resend, naming the pinned "
          "model by path and title - found %r" % (asked,))


def the_chat_is_told():
    """Section 8, and it runs first: it needs no SDK and no server."""
    print()
    print("8. The chat is told, in its instructions (row 5b-1562)")
    sys.path.insert(0, os.path.join(ROOT, "brain"))
    try:
        import heron_instructions as INS
        rules = INS.articles()
        known = INS.instructions()
        text, used = INS.compose(INS.HOST)
        agents, _used = INS.compose("agent.modify")
    except Exception as exc:                                    # noqa: BLE001
        check(False, "host.chat assembles: %s: %s" % (type(exc).__name__, exc))
        return
    own = (known.get(INS.HOST) or {}).get("text") or ""
    told = [part for part in own.split("\n\n") if PREFIX.strip() in part]
    check(len(told) == 1 and all(door in told[0] for door in DOORS),
          "host.chat says, once, that the three doors' replies carry an `outcome:` "
          "line - found %r" % [t[:80] for t in told])
    check(told and "not the wording" in " ".join(told[0].split()),
          "and that the line, not the wording around it, says how the request ended")
    guards = " ".join(" ".join(case.get("must-contain") or [])
                      for case in (known.get(INS.HOST) or {}).get("cases") or [])
    check(PREFIX.strip() in guards, "a case in host.chat guards the sentence")
    with io.open(os.path.join(ROOT, "tests", "test_instructions.py"), encoding="utf-8") as fh:
        budget = re.search(r"^HOST_BUDGET_WORDS = (\d+)", fh.read(), re.M)
    words = len(text.split())
    check(budget is not None and words <= int(budget.group(1)),
          "it is %d words, inside the budget tests/test_instructions.py holds (%s) - "
          "which was not raised" % (words, budget.group(1) if budget else "unread"))

    def citation(line):
        return line.strip().startswith("*(") and line.strip().endswith(")*")

    def plain(article):
        kept = [line for line in article.splitlines() if not citation(line)]
        return re.sub(r"\[([^\]]+)\]\([^)\s]+\)", r"\1", "\n".join(kept)).strip()

    cited = [n for n in used if any(citation(line) for line in rules[n].splitlines())]
    check(len(cited) >= 5,
          "the Constitution's own file still carries its citations (%d of the "
          "Articles a chat is given end in one)" % len(cited))
    check("Golden Rule" not in text and "](" not in text,
          "a chat is given none of them, and no link target")
    missing = [n for n in used if plain(rules[n]) not in text]
    check(not missing,
          "and every word of every Article it is given is still there, in order - "
          "all %d; not: %s" % (len(used), ", ".join(missing) or "none"))
    check("Golden Rule" in agents,
          "Heron's own agents are still given the citations: only what a chat is "
          "given changed")


# ------------------------------------------------------------------- main

def sdk_version():
    """(version, broken) - as tests/test_mcp_serves.py reads it, and for its
    reason: a half-broken install raises out of a native extension."""
    try:
        import mcp                                  # noqa: F401
    except ImportError:
        return None, None
    except BaseException as exc:                    # noqa: BLE001
        return None, "%s: %s" % (type(exc).__name__, exc)
    try:
        import importlib.metadata as meta
        return meta.version("mcp"), None
    except Exception:                               # noqa: BLE001
        return "unknown", None


def main():
    print("How a call through a door ended, in one fixed word (row 5b-1159)")
    print("=" * 66)
    the_chat_is_told()
    version, broken = sdk_version()
    if version is None:
        print()
        if FAILURES:
            print("FAILED  %d check(s) that need no SDK:" % len(FAILURES))
            for failure in FAILURES:
                print("  - %s" % failure[:200])
            return 1
        print("SKIPPED - %s" % ("an MCP SDK is installed and cannot be imported: %s"
                                % broken if broken else
                                "no MCP SDK is installed, and the doors are functions "
                                "of a server that cannot be imported without one."))
        print("NOTHING IS PROVEN BY THIS RUN. Exiting %d, which check-gaps reads as "
              "WAITING and not as a pass." % SKIPPED)
        return SKIPPED
    print("  SDK version %s" % version)

    # NO CALL HERE MAY REACH A REVIT RUNNING ON THIS PC (row 5b-461), and
    # nothing is written to the owner's knowledge, trail or run records.
    import live_revit_guard as GUARD
    import heron_bridge_client as bridge
    guard = GUARD.Guard.around(bridge).install()
    home = tempfile.mkdtemp(prefix="heron-outcome-line-")
    had = dict((key, os.environ.get(key)) for key in ("HERON_KNOWLEDGE", "HERON_AUDIT"))
    os.environ["HERON_KNOWLEDGE"] = home
    os.environ["HERON_AUDIT"] = os.path.join(home, "audit")
    real_discovery = bridge.DISCOVERY_DIR
    bridge.DISCOVERY_DIR = os.path.join(home, "bridges")
    os.makedirs(bridge.DISCOVERY_DIR)
    real_runs = getattr(bridge, "RUNS_DIR", None)
    bridge.RUNS_DIR = os.path.join(home, "runs")
    os.makedirs(bridge.RUNS_DIR)
    stage = None
    try:
        try:
            import heron_mcp_server as module
        except Exception as exc:                                # noqa: BLE001
            print("  FAIL the server could not be imported: %s: %s"
                  % (type(exc).__name__, exc))
            return 1
        import heron_tools as registry
        core = sys.modules[type(module.server).__module__]

        picked, cases = build_cases(module, core)
        print()
        print("  the cards the doors are asked for, read from the library:")
        for role in ("change", "draft", "read", "show"):
            print("    %-7s %s" % (role, picked[role]))
        check(all(picked.values()),
              "the library has a PROVEN change whose proof stands, a DRAFT change, "
              "a PROVEN read and something revit_show runs")
        if not all(picked.values()):
            return 1

        stage = Stage(module, core)
        the_vocabulary(core, registry)
        replies = a_word_per_ending(stage, cases)
        never_more_than_known(stage, core, cases, replies)
        breaks_after_sending(stage, cases, replies, core)
        every_path_one_line(cases, replies, core)
        additive(stage, cases, replies, core)
        asyncio.run(through_the_sdk(stage, cases, picked))
        nothing_else_moved(stage, cases, replies, picked)
        the_undo_tool(stage, cases, replies)

        guard.stop()
        print()
        touched = guard.touched()
        check(not touched, "the Revit running on this PC was never reached - %r"
              % (touched,))
    finally:
        if stage is not None:
            stage.close()
        bridge.DISCOVERY_DIR = real_discovery
        if real_runs is not None:
            bridge.RUNS_DIR = real_runs
        for key, value in had.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s):" % len(FAILURES))
        for failure in FAILURES[:40]:
            print("  - %s" % failure[:200])
        return 1
    print("PASS    every door says how the call ended, once, in a word from one table")
    return 0


if __name__ == "__main__":
    sys.exit(main())
