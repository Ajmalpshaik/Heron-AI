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

    outcome: REFUSED - Heron or Revit said no and the reply says why; ...

The real tool functions are called, against a stand-in Revit that answers
from a script - the way tests/test_mcp_serves.py stands the platform aside.
No pipe is opened and no discovery folder is read.

WHAT IT PROVES
  1. ONE VOCABULARY, IN ONE PLACE. heron_server_core.OUTCOMES is the table:
     every word once, each with what it means, and heron_tools.STATES_OUTCOME
     names the tools that say one.

  2. A WORD PER WAY OF ENDING, each forced through the door itself: a kept
     change; refusals of several kinds (the switch off, a DRAFT, a read sent
     to revit_change, the add-in's own wrong-model refusal, nothing by that
     name, no session); a rollback; a lost, oversize or unreadable answer -
     OUTCOME UNKNOWN, never REFUSED and never KEPT; a run still going; a read
     answered; words that settled nothing.

  3. NEVER MORE THAN IS KNOWN. Ask to Keep still open is STILL RUNNING and a
     Discard is ROLLED BACK - neither is KEPT. A Discard Revit did not
     confirm, a reply that names no verdict, and a change behind the first
     call's pin check are OUTCOME UNKNOWN. A door that returned without
     saying how it ended is OUTCOME UNKNOWN too: the line fails closed.

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
WORDS = ("KEPT", "ANSWERED", "REFUSED", "NOT RUN", "ROLLED BACK",
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

ELSEWHERE = dict(READ_OK, document="Tower B", documentPath="C:/jobs/TowerB.rvt",
                 projectKey="key-2")


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

    def run(self, stage, body=False):
        """The door's reply - through the tool function a host is served, or
        (`body`) through the page's own route to the same body, which never
        passed the wrapper. A crash comes back as text no check will take for
        an answer (heron-ship 2a)."""
        stage.fresh(self.reply, self.pin)
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

    def no_code(stage):
        real = os.path.isfile
        stage.patch(os.path, "isfile",
                    lambda path: False if str(path).endswith("fragment.cs") else real(path))

    C, R, S = "revit_change", "revit_read", "revit_show"
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
        Case("nothing by that name", C, "REFUSED",
             {"capability": "NO_SUCH_CAPABILITY_AT_ALL"}, KEPT, kind="refused"),
        Case("no Revit session to send it to", C, "REFUSED",
             {"capability": change}, KEPT, setup=not_bound, kind="refused"),
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
        Case("nothing by that name, at revit_read", R, "REFUSED",
             {"capability": "NO_SUCH_CAPABILITY_AT_ALL"}, READ_OK),
        Case("no Revit session, at revit_read", R, "REFUSED",
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
        Case("a read that threw (fragment_threw)", R, "ROLLED BACK",
             {"capability": read}, error("fragment_threw")),
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
        Case("nothing by that name, at revit_show", S, "REFUSED",
             {"capability": "NO_SUCH_CAPABILITY_AT_ALL"}, READ_OK),
        Case("no Revit session, at revit_show", S, "REFUSED",
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
        Case("a show that threw", S, "ROLLED BACK", {"capability": show},
             error("fragment_threw")),
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
    "a tool body that raises: the SDK answers for it (`isError`, and the "
    "exception's text stays on the server), so there is no reply to carry a line",
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
          "it holds exactly the seven words a host is told - found %r" % (words,))
    check(len(set(words)) == len(words), "each once")
    check(all(len(row) == 3 and (row[2] or "").strip() for row in rows),
          "and each with a plain sentence saying what it means")
    told = dict((row[0], row[1]) for row in rows if len(row) == 3)
    check(not told.get("KEPT") and not told.get("ANSWERED"),
          "the two that mean it worked add nothing to their line - the reply "
          "below already says what was done")
    check(all(told.get(word) for word in WORDS if word not in ("KEPT", "ANSWERED")),
          "and each of the others says, on the line, what to do next")
    running = told.get("STILL RUNNING") or ""
    check("again" in running and "id" in running,
          "STILL RUNNING says not to send it again, and that no id follows a "
          "run yet - found %r" % running)
    check("wf-" not in running and "run=" not in running,
          "and invents none")
    check(tuple(getattr(registry, "STATES_OUTCOME", ())) == DOORS,
          "heron_tools.STATES_OUTCOME names the tools that say one: the three doors")
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
                                      if c.reply in (KEPT, ASKED_KEPT)
                                      and c.word == "KEPT"),
          "KEPT is said only of a reply whose own verdict is `kept` - found %r"
          % (only_kept,))
    running = [c for c in cases if c.kind == "running"]
    check(running and all("again" in replies[c].split("\n")[1] for c in running
                          if len(replies[c].split("\n")) > 1),
          "a run still going says, on the line itself, not to send it again")

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
    check(not differs,
          "all %d: every sentence the door wrote is there, in the same order - "
          "not: %s" % (len(cases), "; ".join(differs[:5]) or "none"))


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

    wanted = ("kept", "refused", "rollback", "lost", "running", "answered", "not run")
    seen = set()
    for case in cases:
        if case.kind not in wanted or (case.kind, case.door) in seen:
            continue
        seen.add((case.kind, case.door))
        stage.fresh(case.reply, case.pin)
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
    version, broken = sdk_version()
    if version is None:
        print()
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
        every_path_one_line(cases, replies, core)
        additive(stage, cases, replies, core)
        asyncio.run(through_the_sdk(stage, cases, picked))
        nothing_else_moved(stage, cases, replies, picked)

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
