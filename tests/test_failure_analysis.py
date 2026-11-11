#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Failure Analysis Agent - never blind-retries. Runs without Revit.

The dangerous mistake this exists to prevent has a specific shape, and it is
not "Heron reported the wrong error". It is:

    The move worked. The answer was lost on the way back. Heron decided that
    looked like a failure, tried again, and moved the same ducts 200 mm twice.

Nothing in the model says that happened. The ducts are simply 400 mm out, and
the person who finds out is whoever measures them later.

So the tests below are mostly not about classification being pretty. They are
one property, asserted from several directions:

    NO failure of an operation that can change the model may EVER come back
    saying "safe to retry" unless it is PROVEN the request never ran.

    python tests/test_failure_analysis.py
"""

import io
import json
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
# Appended, not inserted: brain/ goes behind mcp/ so it can shadow nothing
# this suite imports from there.
sys.path.append(os.path.join(ROOT, "brain"))

import heron_failure as fa                                   # noqa: E402
from heron_failure import analyse, explain                   # noqa: E402
import heron_relpath as RELPATH                              # noqa: E402

FAILURES = []

# A build's output folders. Skipped by WHOLE FOLDER NAME below the folder
# walked, never by a word anywhere in the path above it (5b row 462).
BUILD_OUTPUT = ("bin", "obj")


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def refusal(code, message=None):
    return {"ok": False, "error": code, "message": message}


def build_output(folder, base):
    """Is `folder` a build's output - inside a bin or obj folder below `base`?

    Asked of the path RELATIVE to `base`, one whole folder name at a time, so
    nothing above `base` - the checkout's own folder - can answer it.
    """
    below = RELPATH.relpath(folder, base)
    return any(part in BUILD_OUTPUT for part in below.split(os.sep))


def codes_in(base):
    """Every literal code a Json.Error call in the C# under `base` produces."""
    produced = set()
    for folder, _, names in os.walk(base):
        if build_output(folder, base):
            continue
        for name in names:
            if not name.endswith(".cs"):
                continue
            text = io.open(os.path.join(folder, name), encoding="utf-8",
                           errors="replace").read()
            produced.update(re.findall(r'Json\.Error\(\s*"([a-z_]+)"', text))
    return produced


def plant(path, text):
    """Write one scratch file, making its folders."""
    folder = os.path.dirname(path)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    with io.open(path, "w", encoding="utf-8") as handle:
        handle.write(text)


class _Pipe(object):
    """A pipe whose Revit answers each request with the next of `answers`,
    bytes as given - readable or not, short or long."""

    def __init__(self, *answers):
        self.answers = list(answers)
        self.sent = []
        self.closed = 0
        self._left = b""

    def write(self, data):
        self.sent.append(data)
        self._left = self.answers.pop(0) if self.answers else b""
        return len(data)

    def read(self, size):
        chunk, self._left = self._left[:size], self._left[size:]
        return chunk

    def close(self):
        self.closed += 1


def arrived_unusable():
    """
    AN ANSWER THAT ARRIVED AND COULD NOT BE USED IS NOT "NEVER RAN"
    (FRAGMENT-ISSUES 5b-1167). Asked of the client's own `request`, with only
    the pipe replaced, and then of the classification - so the two halves are
    held to each other and neither needs a Revit.
    """
    print()
    print("An answer that arrived and could not be used (5b-1167)")
    import heron_bridge_client as bridge

    class StandIn(bridge.Bridge):
        def __init__(self, pipe):
            super(StandIn, self).__init__(
                {"pid": 4242, "pipeName": "heron-5b-1167", "revitVersion": "2024",
                 "protocolVersion": bridge.PROTOCOL_VERSION, "token": "stand-in"}, None)
            self.pipe = pipe

        def _connect(self, timeout):
            return self.pipe

    def error_of(reply):
        return reply.get("error") if isinstance(reply, dict) else None

    pipe = _Pipe(b"this is not an answer Heron can read\n")
    reply = StandIn(pipe).request("run_fragment_read", response_timeout=2.0)
    check(error_of(reply) == "reply_unreadable",
          "a READ whose answer arrived and would not parse is NOT 'nothing came back': "
          "%r" % (reply,))
    check(len(pipe.sent) == 1, "and it is not sent a second time - it ran")
    read = analyse(reply, writes=False)
    check(read is not None and read.outcome == fa.UNKNOWN and read.may_retry is False
          and read.touched_the_model is None,
          "it is classified as an UNKNOWN outcome, never retried - it arrived, so the "
          "request ran: %r" % (read,))
    said = explain(read) or ""
    check("RAN" in said and "costs nothing" not in said and "never ran" not in said.lower(),
          "and the chat is told it ran, not that asking again costs nothing: %s" % said[:120])

    pipe = _Pipe(b"{ half an answer\n")
    reply = StandIn(pipe).request("run_fragment_write", response_timeout=2.0,
                                  idempotent=False)
    wrote = analyse(reply, writes=True)
    check(error_of(reply) == "reply_unreadable" and wrote is not None
          and wrote.outcome == fa.UNKNOWN and wrote.next_step == fa.LOOK_AT_THE_MODEL,
          "a WRITE's unreadable answer is the same code, and still a person looking at "
          "the model: %r" % (wrote,))

    print()
    print("  and one larger than Heron reads off the pipe")
    limit = getattr(bridge, "REPLY_LIMIT_BYTES", None)
    check(isinstance(limit, int) and limit > 6.2 * 1024 * 1024,
          "the client names ONE limit on an answer, above the largest on record "
          "(6.2 MB, row 5b-489): %r" % (limit,))
    small = 8 * 1024
    big = (json.dumps({"ok": True, "document": "stand-in", "pad": "x" * (5 * small)})
           + "\n").encode("utf-8")
    fine = (json.dumps({"ok": True, "document": "stand-in"}) + "\n").encode("utf-8")
    bridge.REPLY_LIMIT_BYTES = small
    try:
        pipe = _Pipe(big, fine)
        session = StandIn(pipe)
        reply = session.request("run_fragment_read", response_timeout=2.0)
        again = session.request("run_fragment_read", response_timeout=2.0)
    finally:
        if limit is None:
            del bridge.REPLY_LIMIT_BYTES
        else:
            bridge.REPLY_LIMIT_BYTES = limit
    check(error_of(reply) == "reply_too_large",
          "an answer over the limit is refused, not parsed: %s"
          % (error_of(reply) or "it was read whole"))
    message = (reply.get("message") or "") if isinstance(reply, dict) else ""
    check("MB" in message and "RAN" in message and "run record" in message,
          "in a sentence that says how much had arrived, that it ran, and where a "
          "change's whole answer is: %s" % message[:100])
    check(len(pipe.sent) == 2 and pipe.closed >= 1 and isinstance(again, dict)
          and again.get("ok") is True,
          "the connection it stopped reading is closed, and the next request gets its "
          "own answer - never the rest of that line")
    large = analyse(reply, writes=False)
    check(large is not None and large.outcome == fa.UNKNOWN and large.may_retry is False,
          "and it is an UNKNOWN outcome too: asking again brings the same answer")

    print()
    print("  and nothing at all coming back says only what is known")
    nothing = analyse(None, writes=False)
    told = explain(nothing) or ""
    check("costs nothing" not in told and "Revit was reached" in told,
          "no reply to a READ no longer says asking again 'costs nothing' - a request "
          "whose answer never came may have run: %s" % told)
    table = getattr(fa, "_ARRIVED_UNUSABLE", None)
    made = [getattr(bridge, name, None) for name in ("unreadable_reply", "too_large_reply")]
    codes = sorted(make(1).get("error") for make in made if make is not None)
    check(table is not None and codes == sorted(table) and len(codes) == 2,
          "and the codes classified are exactly the ones the client produces: %s" % codes)
    check(table is None or not set(table) & set(fa._KNOWN),
          "in a table of their own - _KNOWN stays the add-in's codes")

    print()
    print("  and discovery still does not list a pipe it could not read")
    # `discover` asked "is not None" of a ping, and an unreadable reply WAS
    # None. With a code of its own it would have counted as a live session.
    could_read = getattr(bridge, "could_read", None)
    check(could_read is not None, "the client has one question for 'did it answer?'")
    if could_read is None:
        def could_read(reply):
            return reply is not None
    check(not could_read(None), "nothing coming back is not an answer")
    check(all(not could_read(make(1)) for make in made if make is not None) and len(made) == 2
          and None not in made,
          "nor is either of the client's own stand-ins for an answer it could not use")
    check(could_read({"ok": True, "pong": True})
          and could_read({"ok": False, "error": "unauthorized"}),
          "an answer that was read IS one - a refusal included, as before")
    source = io.open(os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py"),
                     encoding="utf-8").read()
    listing = source[source.find("\ndef discover("):]
    listing = listing[:listing.find("\ndef ", 1)]
    check('if could_read(bridge.request("ping")):' in listing
          and 'bridge.request("ping") is not None' not in listing,
          "and discover asks it of the ping, so a pipe answering something unreadable "
          "is not listed as a live Revit")


def main():
    print("Success is not a failure")
    check(analyse({"ok": True, "moved": 4}, writes=True) is None,
          "a successful reply analyses to nothing at all")

    print()
    print("THE PROPERTY: a write is never silently retried unless it provably never ran")
    offenders = []
    for code in fa._KNOWN:
        f = analyse(refusal(code), writes=True)
        if f.may_retry and f.outcome != fa.NEVER_RAN:
            offenders.append("%s (%s)" % (code, f.outcome))
    check(not offenders,
          "no known code is retryable without NEVER_RAN%s"
          % ("" if not offenders else " - offenders: " + ", ".join(offenders)))

    print()
    print("The lost answer - the case the whole file exists for")
    lost = analyse(refusal("unknown_outcome"), writes=True)
    check(lost.outcome == fa.UNKNOWN, "a lost answer is UNKNOWN, not a failure")
    check(lost.may_retry is False, "and it is NEVER retried automatically")
    check(lost.next_step == fa.LOOK_AT_THE_MODEL, "a person must look at the model")
    check(lost.touched_the_model is None,
          "'might it have changed?' answers None - genuinely unknown, not False")

    print()
    print("Nothing came back at all")
    check(analyse(None, writes=True).outcome == fa.UNKNOWN,
          "no reply to a WRITE is unknown - it cannot be assumed not to have happened")
    check(analyse(None, writes=True).may_retry is False,
          "and so it is not retried")
    check(analyse(None, writes=False).may_retry is True,
          "no reply to a READ is safe to ask again - it costs nothing")

    print()
    print("Still running is not failure, and asking again is the wrong move")
    running = analyse(refusal("still_running"), writes=True)
    check(running.outcome == fa.RUNNING, "Revit took it and is working")
    check(running.next_step == fa.WAIT, "the answer is to wait, not to retry")
    check(running.may_retry is False, "asking again would run the same job twice")

    print()
    print("Fail closed on what it has never seen")
    novel = analyse(refusal("some_future_error"), writes=True)
    check(novel.outcome == fa.UNKNOWN,
          "an unrecognised WRITE failure is treated as unknown, not as retryable")
    check(novel.may_retry is False, "so it is not retried")

    novel_read = analyse(refusal("some_future_error"), writes=False)
    check(novel_read.outcome == fa.REFUSED,
          "the same code on a READ is only a refusal - no model to endanger")

    print()
    print("Never ran means never ran - these are the only free retries")
    for code in ("not_ready", "raise_failed"):
        f = analyse(refusal(code), writes=True)
        check(f.outcome == fa.NEVER_RAN and f.may_retry,
              "%s: proven not to have reached the model, so retrying is free" % code)

    print()
    print("Refusals did not touch anything, but still need a person to act")
    for code, step in (("write_disabled", fa.FIX_FIRST),
                       # D-106's two switches refuse the same way Changes does.
                       ("admin_disabled", fa.FIX_FIRST),
                       ("publish_disabled", fa.FIX_FIRST),
                       ("stopped", fa.FIX_FIRST),
                       ("revit_busy", fa.FIX_FIRST),
                       ("preview_expired", fa.START_OVER),
                       ("model_moved_on", fa.START_OVER)):
        f = analyse(refusal(code), writes=True)
        check(f.touched_the_model is False and f.next_step == step,
              "%s: nothing happened to the model, next step is %s" % (code, step))

    print()
    print("The lease refusal happened BEFORE anything reached the model")
    busy = analyse(refusal("session_in_use"), writes=True)
    check(busy.outcome == fa.NEVER_RAN,
          "another chat holding the Revit means the request never ran")
    check(busy.touched_the_model is False,
          "so the model is untouched - NOT the unknown case")
    check(busy.next_step == fa.FIX_FIRST and not busy.may_retry,
          "the user waits or takes the session; Heron does not retry into a fight")

    print()
    print("Golden Rule 20 survives the classification")
    closed = analyse(refusal("document_closed"), writes=True)
    check(closed.next_step == fa.STOP,
          "the approved model being CLOSED is a stop, never a retry elsewhere")
    front = analyse(refusal("document_not_in_front"), writes=True)
    check(front.next_step == fa.FIX_FIRST and not front.may_retry,
          "clicking away is fixable by the user, but never auto-retried")

    print()
    print("A rolled-back failure left the model alone")
    rolled = analyse(refusal("move_failed"), writes=True)
    check(rolled.outcome == fa.ROLLED_BACK, "it ran and was rolled back")
    check(rolled.touched_the_model is False, "so the model is as it was")
    check(rolled.may_retry is False,
          "still not automatic - it failed for a reason nobody has addressed yet")

    print()
    print("Two halves on different versions is a stop, not a retry loop")
    check(analyse(refusal("unknown_op"), writes=False).next_step == fa.STOP,
          "an operation the add-in does not know will never start working on retry")

    print()
    print("What the user is told")
    said = explain(analyse(refusal("unknown_outcome", "The answer was lost."), writes=True))
    check("The answer was lost." in said,
          "it keeps the far side's own words rather than reinventing them")
    check("twice" in said.lower(),
          "and adds the part the far side cannot know - that repeating could do it twice")
    check(explain(None) is None, "success explains to nothing")

    print()
    print("EVERY CODE THE ADD-IN CAN PRODUCE IS CLASSIFIED")
    # A GATE, NOT A REPORT, and the reason is what the gap costs. An
    # unclassified code on a write falls to the fail-closed default and is
    # reported as an UNKNOWN outcome - "look at the model before trying
    # again, repeating it could do the work twice". For `compile_failed`, or
    # a caller who left a value out, that is frightening and false: nothing
    # ran. EIGHTEEN codes were missing on 2026-09-21, and the file's own
    # comments record the same gap being found twice before, one code at a
    # time (FRAGMENT-ISSUES section 5b, row 28).
    #
    # Read from the C# rather than from a list here, because a list here
    # would be the third copy of the same set and would go stale the same
    # way. The pattern is the one the add-in uses everywhere: Json.Error with
    # a literal code.
    produced = codes_in(os.path.join(ROOT, "revit"))
    check(len(produced) > 30,
          "found %d error codes in the add-in's C#" % len(produced))

    # AND IT FINDS THEM FROM A CHECKOUT WHOSE OWN PATH SAYS obj AND bin. The
    # Claude app names a worktree at random, and until 2026-10-14 this scan
    # skipped any folder whose WHOLE path held "<sep>obj" or "<sep>bin" - so
    # from .claude/worktrees/objective-chebyshev-fda7f7 it skipped all of
    # revit/, found 0 codes and called every classified code ORPHANED, on a
    # commit that passed under any other name (5b row 462). Both directions:
    # the source file is read, and a bin and an obj inside revit/ still are not.
    scratch = tempfile.mkdtemp(prefix="heron-5b462-")
    try:
        base = os.path.join(scratch, "obj", "objective-chebyshev", "bin", "revit")
        plant(os.path.join(base, "Heron.Revit.Addin", "Handler.cs"),
              u'return Json.Error("from_source", "read");\n')
        plant(os.path.join(base, "Heron.Revit.Addin", "obj", "Debug", "Made.cs"),
              u'return Json.Error("from_obj", "skipped");\n')
        plant(os.path.join(base, "Heron.Revit.Addin", "bin", "x64", "Copy.cs"),
              u'return Json.Error("from_bin", "skipped");\n')
        found = codes_in(base)
    finally:
        shutil.rmtree(scratch, ignore_errors=True)
    check("from_source" in found,
          "a checkout whose own path holds obj and bin is still scanned - "
          "found %s" % (sorted(found) or "nothing"))
    leaked = sorted(found & set(["from_obj", "from_bin"]))
    check(not leaked,
          "and a bin or obj folder inside revit/ is still skipped%s"
          % ("" if not leaked else " - READ: " + ", ".join(leaked)))

    # handler_failed is deliberately absent and the file says why: it is
    # raised when an exception escapes from anywhere, so only the
    # writes-aware default can answer it correctly on both paths.
    DELIBERATELY_OPEN = set(["handler_failed"])
    missing = sorted(produced - set(fa._KNOWN) - DELIBERATELY_OPEN)
    check(not missing,
          "every one of them is classified, or named as deliberately open%s"
          % ("" if not missing else " - MISSING: " + ", ".join(missing)))
    for code in sorted(DELIBERATELY_OPEN):
        check(code not in fa._KNOWN,
              "'%s' is still left to the fail-closed default on purpose" % code)

    # And the other direction, which is how a code that was RENAMED in the
    # C# shows up: a row here for something nothing can produce any more.
    # unknown_outcome is the client's own, not the add-in's.
    CLIENT_SIDE = set(["unknown_outcome"])
    orphans = sorted(set(fa._KNOWN) - produced - CLIENT_SIDE)
    check(not orphans,
          "and no row classifies a code the add-in cannot produce%s"
          % ("" if not orphans else " - ORPHANED: " + ", ".join(orphans)))

    # ONE ROW PER CODE, READ OFF THE SOURCE. A dict literal keeps the LAST of
    # two equal keys and says nothing, so a second row for one code is a row
    # nobody reads - "no_view" was written twice, by two changes on one day,
    # and the first sentence never reached anybody (row 5b-593).
    import ast
    with io.open(os.path.join(ROOT, "mcp", "server", "heron_failure.py"),
                 encoding="utf-8") as fh:
        tree = ast.parse(fh.read())
    keys = []
    for node in ast.walk(tree):
        if isinstance(node, ast.Assign) and any(
                isinstance(target, ast.Name) and target.id == "_KNOWN"
                for target in node.targets) and isinstance(node.value, ast.Dict):
            keys = [k.value for k in node.value.keys if isinstance(k, ast.Constant)]
    doubled = sorted(set(k for k in keys if keys.count(k) > 1))
    check(len(keys) > 0 and not doubled,
          "and every code has ONE row in _KNOWN's source, none silently overwritten%s"
          % ("" if not doubled else " - TWICE: " + ", ".join(doubled)))

    arrived_unusable()

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("PASSED - never blind-retries, and fails closed on what it has not met.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
