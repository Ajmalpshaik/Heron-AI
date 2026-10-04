#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
`fragment --in "Doc"` - a fragment run aimed at a model that is not in front.

    python tests/test_fragment_in_document.py

WHY
---
The add-in honours a `document` key on every fragment run, kept writes
included (RevitFragment.cs, "THE ACTIVE DOCUMENT IS A CHOICE, NOT A LIMIT").
`validate --in` and `prove --in` could say it; `fragment` could not, so a kept
write on a family two windows back was sent on 2026-10-04 by patching
Bridge.request to add the key by hand. This suite holds the flag to:

  * the real parser, through the client's own `main`, with `cmd_fragment`
    replaced - nothing connects and nothing is discovered
  * `--in` read in front of the fragment name, like `validate --in`
  * `--in` AFTER the name refused, never ignored - ignored, a kept write
    would land on whatever window is in front
  * the key reaching the request as `document`, and absent when not asked
  * the document the run landed on printed with the result

WHAT IT CANNOT SAY
------------------
That Revit opens the named document and keeps the change there. That is a
run on the owner's PC, not a container.
"""

import contextlib
import io
import os
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# The client reads heron.config for its timeouts. Point it at a file that is
# not there, so the owner's own settings are neither read nor needed.
_SCRATCH = tempfile.mkdtemp(prefix="heron-fragment-in-")
os.environ["HERON_CONFIG"] = os.path.join(_SCRATCH, "heron.config")

sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_bridge_client as CLIENT                          # noqa: E402

FAILURES = []

FAMILY = "TRG_PLMB_VLV_PPR Check Valve Inline_CVI_R0"


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def parse(*tokens):
    """The client's own main, with cmd_fragment replaced. Returns (code, call)."""
    captured = {}

    def capture(name, values=None, writing=False, apply_it=False, **kwargs):
        captured.update(name=name, values=values, writing=writing,
                        apply_it=apply_it, **kwargs)
        return 0

    real, CLIENT.cmd_fragment = CLIENT.cmd_fragment, capture
    out = io.StringIO()
    try:
        with contextlib.redirect_stdout(out):
            code = CLIENT.main(["heron_bridge_client.py", "fragment"] + list(tokens))
    finally:
        CLIENT.cmd_fragment = real
    return code, captured, out.getvalue()


class FakeBridge(object):
    """A Revit that answers every request and keeps the arguments it was sent."""

    revit_version = "2024"
    pid = 4242

    def __init__(self, landed, in_front):
        self.sent = []
        self.landed = landed
        self.in_front = in_front

    def request(self, op, op_args=None, **kwargs):
        self.sent.append((op, dict(op_args or {})))
        return {"ok": True, "document": self.landed,
                "wasActiveDocument": self.in_front, "provides": {},
                "applied": True, "verdict": "kept (fake)"}

    def release(self):
        pass

    def close(self):
        pass


def run(name, landed, in_front, **kwargs):
    bridge = FakeBridge(landed, in_front)
    saved = (CLIENT.refuse_off_windows, CLIENT.discover)
    CLIENT.refuse_off_windows = lambda: False
    CLIENT.discover = lambda prune=True: ([bridge], [], [], [])
    out = io.StringIO()
    try:
        with contextlib.redirect_stdout(out):
            try:
                code = CLIENT.cmd_fragment(name, **kwargs)
            except TypeError as error:
                # A client without the argument is a FAIL below, not a
                # traceback in place of every check (heron-ship 2a).
                code, bridge.sent = None, []
                out.write(u"%s" % error)
    finally:
        CLIENT.refuse_off_windows, CLIENT.discover = saved
    return code, bridge.sent, out.getvalue()


def main():
    print("1. The parser reads --in in front of the fragment name")
    code, call, _ = parse("--in", FAMILY, "set-family-connector-roles",
                          "--write", "--apply")
    check(code == 0 and call.get("name") == "set-family-connector-roles",
          "the fragment name survives --in - got %r" % call.get("name"))
    check(call.get("in_document") == FAMILY,
          "the whole title arrives as one value, spaces included - got %r"
          % call.get("in_document"))
    check(call.get("writing") is True and call.get("apply_it") is True,
          "--write --apply still read alongside it")

    code, call, _ = parse("--session", "4242", "--in", "Project1", "list-levels",
                          "--view", "Level 1")
    check(code == 0 and call.get("in_document") == "Project1"
          and call.get("session") == "4242" and call.get("name") == "list-levels",
          "--session before --in, and --view after the name, all still read")
    check([v["value"] for v in (call.get("values") or []) if v["name"] == "view"]
          == ["Level 1"], "and the view value arrives whole")

    code, call, _ = parse("list-levels")
    check(code == 0 and call.get("in_document") is None,
          "without --in nothing is named - the front window, as before")

    print()
    print("2. A misplaced or empty --in is refused, never ignored")
    code, call, out = parse("set-family-connector-roles", "--in", FAMILY,
                            "--write", "--apply")
    check(code == 2 and not call,
          "--in after the name is refused before anything runs (exit %s)" % code)
    check("before the fragment name" in out, "and the refusal says where it goes")

    code, call, out = parse("--in")
    check(code == 2 and not call, "--in with nothing after it is refused")

    print()
    print("3. cmd_fragment sends it as `document` and says where the run landed")
    code, sent, out = run("set-family-connector-roles", FAMILY, False,
                          writing=True, apply_it=True, in_document=FAMILY)
    check(code == 0 and len(sent) == 1
          and sent[0][1].get("document") == FAMILY,
          "a kept write carries document=%r - sent %r"
          % (FAMILY, [a.get("document") for _, a in sent]))
    check(sent and sent[0][1].get("apply") == "true",
          "and is still sent as a kept write")
    check("document: %s" % FAMILY in out,
          "the document the run landed on is printed")
    check("NOT the document on screen" in out,
          "and a run behind the front window says so")

    code, sent, out = run("list-levels", "Project1", True)
    check(code == 0 and sent and "document" not in sent[0][1],
          "without --in no document key is sent - the add-in's own default stands")
    check("document: Project1" in out and "NOT the document on screen" not in out,
          "and the front document is still named, with no warning")

    print()
    if FAILURES:
        print("%d check(s) FAILED" % len(FAILURES))
        return 1
    print("All checks passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
