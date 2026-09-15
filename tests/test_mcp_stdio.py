# Heron-Agent:  HERON-MCP-SRV-001
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The MCP server as a HOST sees it: a real subprocess, over real stdio.

    python tests/test_mcp_stdio.py

WHY THIS FILE EXISTS, and why test_mcp_serves.py could not have caught it.

That file calls the tools through the SDK's own dispatch, IN THIS PROCESS. It
proves the tools are registered, described and answerable, and on 2026-09-06 it
passed green while `heron_capabilities` was completely unanswerable from a real
host: a Claude Code tool call sat on it for thirty minutes and got nothing.

The difference is one line of stack. In-process, the trained encoder is already
imported or fails instantly. In the server, the first call reached
`import model2vec` -> `import numpy` -> loading numpy's native extension, ON THE
ASYNCIO EVENT LOOP, and the reply never came. That import costs 1.0 s in a fresh
process and was measured still running 40 s later there.

It needed BOTH halves, and neither is a mistake alone: before model2vec was
installed the import failed instantly and Heron degraded to `lexical`, so the
handler always answered. Installing it to prove the search understands meaning
(`A7`) is what made `A8` hang. Two checks, each correct, never run together.

So this file starts the server the way a host does and holds it to a DEADLINE.
A test that waits forever cannot tell a slow answer from no answer, which is
the whole failure being guarded against.
"""

import json
import os
import subprocess
import sys
import threading
import time

sys.path.insert(0, os.path.join(
    os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "mcp", "server"))
from heron_tools import TOOLS                                  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")

# Generous next to the 6 s it takes, tight next to the failure, which was
# unbounded. A number here is a claim about the worst acceptable wait.
DEADLINE = 90

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class Host(object):
    """The smallest thing that behaves like an MCP host over stdio."""

    def __init__(self):
        self.proc = subprocess.Popen(
            [sys.executable, "-u", SERVER], cwd=ROOT,
            stdin=subprocess.PIPE, stdout=subprocess.PIPE,
            stderr=subprocess.PIPE, text=True, encoding="utf-8", bufsize=1)
        self.err = []
        threading.Thread(target=self._drain, daemon=True).start()

    def _drain(self):
        for line in self.proc.stderr:
            self.err.append(line)

    def send(self, obj):
        self.proc.stdin.write(json.dumps(obj) + "\n")
        self.proc.stdin.flush()

    def read(self, timeout):
        box = {}
        t = threading.Thread(target=lambda: box.__setitem__("l", self.proc.stdout.readline()))
        t.daemon = True
        t.start()
        t.join(timeout)
        return box.get("l")

    def call(self, ident, name, args, timeout):
        t0 = time.time()
        self.send({"jsonrpc": "2.0", "id": ident, "method": "tools/call",
                   "params": {"name": name, "arguments": args}})
        line = self.read(timeout)
        return line, time.time() - t0

    def close(self):
        try:
            self.proc.terminate()
        except Exception:
            pass


def text_of(line):
    if not line:
        return None
    try:
        d = json.loads(line)
    except ValueError:
        return None
    if "result" not in d:
        return None
    return "".join(c.get("text", "") for c in d["result"].get("content", []))


def main():
    if os.name != "nt":
        print("SKIPPED - the server refuses to start off Windows by design.")
        return 0

    print("Heron over real stdio, as a host sees it")
    host = Host()
    try:
        t0 = time.time()
        host.send({"jsonrpc": "2.0", "id": 1, "method": "initialize", "params": {
            "protocolVersion": "2024-11-05", "capabilities": {},
            "clientInfo": {"name": "heron-stdio-test", "version": "0"}}})
        host.send({"jsonrpc": "2.0", "method": "notifications/initialized"})
        init = host.read(DEADLINE)
        check(init is not None, "the server answers initialize (%.1fs)" % (time.time() - t0))

        host.send({"jsonrpc": "2.0", "id": 2, "method": "tools/list", "params": {}})
        listed = host.read(DEADLINE)
        names = []
        if listed:
            try:
                names = [t["name"] for t in json.loads(listed)["result"]["tools"]]
            except Exception:
                names = []
        # THE COUNT IS DERIVED, because typing it has now gone stale twice -
        # left at ten when the eleventh arrived, and at eleven when the twelfth
        # did. heron_tools.TOOLS is the declaration; this asserts the server
        # SERVES exactly what is declared, which catches everything the typed
        # number caught and cannot rot.
        declared = set(TOOLS)
        offered = set(names)
        check(offered == declared,
              "the host is offered exactly the declared tools (declared %d, "
              "offered %d%s)"
              % (len(declared), len(offered),
                 "" if offered == declared else
                 "; only in registry: %s; only served: %s"
                 % (sorted(declared - offered) or "none",
                    sorted(offered - declared) or "none")))
        check("heron_capabilities" in names, "heron_capabilities is among them")
        check("heron_gaps" in names, "heron_gaps is among them")

        print()
        print("  the call that hung for thirty minutes")
        line, took = host.call(3, "heron_capabilities", {}, DEADLINE)
        answered = text_of(line)
        check(answered is not None,
              "heron_capabilities REPLIES, within %ds (took %.1fs)" % (DEADLINE, took))
        if answered:
            check("job(s) Heron knows by name" in answered,
                  "it lists the jobs it knows")

            # THIS ASSERTION WAS RE-BASED ON 2026-09-06, hours after it was
            # written, and the reason is worth more than the check.
            #
            # It used to require the word "cannot" - Heron saying it could not
            # RUN anything it named. That was true when this file was written
            # and false by the time it next ran: D-28's executor landed in
            # another worktree the same day, and a read-only fragment now runs
            # against the open model. The test encoded yesterday's truth about
            # a system that had moved, which is the same shape as test_embed
            # and test_retrieve being broken by A7 - and the standard is the
            # same too: re-base it on what is true, with the reason written
            # down, never edit it until green.
            #
            # IT HAPPENED A SECOND TIME, TO THIS VERY ASSERTION, and the
            # paragraph above is left standing because it predicted it.
            # "The limit that has NOT moved" was: a fragment that WRITES
            # cannot reach Revit at all. It moved on 2026-09-15 - #144,
            # "Changes ON now changes something" - and _cannot_run() now says
            # a writing fragment reaches Revit through revit_change, which
            # KEEPS what it did, while the ribbon switch is on. The old
            # sentence survives ONLY in a comment in heron_mcp_server.py, so
            # this check could never pass again.
            #
            # Re-based on what is true, by the standard written above: the
            # durable claim is no longer "writing cannot happen" but "writing
            # is GATED, and Heron says so". That is what must never silently
            # stop being true - a Heron that claimed it could write freely
            # would be the actual danger. Asserting both halves keeps it
            # falsifiable: delete the permission wording and this fails.
            check("Changes" in answered and "refuses" in answered,
                  "it says plainly that writing is GATED - reaching Revit only "
                  "while Changes is switched on, refused by name when it is not")
            check("PROVEN" in answered and "not" in answered,
                  "and it still separates what is proved from what is merely "
                  "present")

        print()
        print("  and the tool beside it still works")
        line, took = host.call(4, "heron_lookup",
                               {"request": "select all the ducts"}, DEADLINE)
        found = text_of(line)
        check(found is not None,
              "heron_lookup replies too (%.1fs)" % took)
        if found:
            check("would need" in found,
                  "it names a capability for a sentence in BIM language")
    finally:
        host.close()

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  %s" % line)
        return 1

    print("PASSED - a real host starts Heron, is offered every declared tool,")
    print("and gets an")
    print("answer from every one it asks - inside a deadline rather than eventually.")
    print()
    # DERIVED, because it was typed as "13 of 348" and the library has been
    # 44 of 349 for some time. A number in a closing sentence goes stale the
    # day after it is written and is believed for months.
    try:
        sys.path.insert(0, os.path.join(
            os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "brain"))
        import heron_fragment as HF
        found, _problems = HF.load_all()
        proven = sum(1 for f in found.values() if f.status in ("PROVEN", "PRODUCTION"))
        tally = "%d of %d are PROVEN" % (proven, len(found))
    except Exception:
        tally = "most are not PROVEN"
    print("It says NOTHING about whether any fragment WORKS. %s" % tally)
    print("and the rest are claims, and D3 in NEEDS-CHECKING.md is still")
    print("the line that catches a unit error.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
