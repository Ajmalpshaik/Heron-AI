#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The bridge never stops listening while it says it is running.

    python tests/test_bridge_listener.py

WHY
---
Row 5b-1900. When every pipe instance (16) was in use, the bridge's listener
tried again every 100 ms, 600 times - a minute - then logged "Bridge stopped
listening" and the listen loop ended. Any other fault making the next
instance ended it at once. Nothing cleared `_running` on either path, so
`IsRunning` stayed true and the ribbon said connected with nothing
listening: every later connection failed until the bridge was toggled or
Revit restarted. A full house is temporary - each instance is held by a
request Revit is finishing - and a request can take longer than a minute.

So the listener now waits for as long as the bridge runs: every 100 ms for
the first minute, then every second, saying so in the log once a minute.
It hands back no instance only when the bridge was stopped.

WHAT RUNS HERE AND WHAT CANNOT
------------------------------
The C# is read as TEXT. Holding sixteen instances for over a minute and
seeing the seventeenth connect once one closes is a run of row 5b-1095's
burst on Windows: NEEDS-CHECKING Group OW.

MEASURED AGAINST THE CODE AS IT STOOD: every check fails, no traceback.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SERVER = os.path.join(ROOT, "revit", "Heron.Bridge", "BridgeServer.cs")
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def body_of(text, signature):
    """The method's body, braces balanced, or "" when it is not there."""
    at = text.find(signature)
    if at < 0:
        return ""
    open_at = text.find("{", at)
    depth = 0
    for index in range(open_at, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                return text[open_at:index + 1]
    return ""


def code_only(body):
    return "\n".join(line for line in body.splitlines()
                     if not line.strip().startswith("//"))


def main():
    server = io.open(SERVER, encoding="utf-8").read()
    make = code_only(body_of(server, "Task<NamedPipeServerStream> CreateWaitingPipeAsync("))
    listen = code_only(body_of(server, "Task ListenLoopAsync("))

    print("\n1. the next instance is waited for as long as the bridge runs (5b-1900)")
    check(make != "", "CreateWaitingPipeAsync is found")
    check(re.search(r"attempt\s*>=\s*\d", make) is None,
          "no count of tries after which it gives up")
    check("Bridge stopped listening" not in make,
          "and it never says it stopped listening")
    # EVERY `return null` IS A STOP: the token was cancelled, said on the same
    # line, or the wait between tries was cancelled.
    returns = len(re.findall(r"return null;", make))
    stops = len(re.findall(r"if \(token\.IsCancellationRequested\) return null;", make)) \
        + len(re.findall(r"catch \(OperationCanceledException\) \{ return null; \}", make))
    check(returns > 0 and returns == stops,
          "it hands back no instance only when the bridge was stopped (%d of %d)"
          % (stops, returns))
    check("catch (IOException" not in make and "catch (Exception ex)" in make,
          "a fault of any kind making an instance is tried again - one catch, and "
          "the count above says it does not return")

    print("\n2. it says so, and slows down rather than spinning")
    check("Every pipe instance is in use" in make,
          "the first full house is logged, as before")
    check(re.search(r"Still no free pipe instance", make) is not None
          and "FromMinutes(1)" in make,
          "and once a minute after that, that it is still trying")
    check(re.search(r"\?\s*100\s*:\s*1000", make) is not None,
          "every 100 ms for the first minute, then every second")

    print("\n3. the listen loop stops only when the bridge does")
    check(listen.count("if (waiting == null) break;") == 2,
          "the loop breaks on no instance - which now means stopped, and only that")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the bridge never stops listening while it says it is running")
    return 0


if __name__ == "__main__":
    sys.exit(main())
