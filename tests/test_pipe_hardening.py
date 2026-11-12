#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The pipe to Revit refuses another computer, and the chat checks who serves it (D-162).

    python tests/test_pipe_hardening.py

WHY THIS EXISTS
---------------
The bridge's pipe was already this user's alone, and every request carries a
per-session token. Three gaps were left:

  ANOTHER COMPUTER   the same user's account logged on over the network
                     carries the user's SID, and a named pipe is reachable
                     over SMB. Microsoft's guidance for a local-only pipe:
                     deny NT AUTHORITY\\NETWORK.
  ANOTHER PROCESS    the pipe's name is known to every process of the user;
                     one that created it first would be handed the requests,
                     and the token. The chat now asks Windows which process
                     serves the pipe, and refuses one that is not the Revit
                     the discovery file named - before writing a byte.
  THE TOKEN'S FILE   rested on the profile folder's rules; it is now set to
                     this user alone, on the file itself.

WHAT RUNS HERE AND WHAT CANNOT
------------------------------
The client's rule - server_mismatch, and a connection refused before anything
is written - runs here on a stand-in pipe. The C# is read as TEXT: the deny
rule in both .NET branches, the file's rules after it is written. Whether
Windows refuses a real network logon, and reports the server's pid to a
client handle, is the PC's: NEEDS-CHECKING Group NW.

MEASURED AGAINST THE CODE AS IT STOOD: every check fails, no traceback.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

SERVER = os.path.join(ROOT, "revit", "Heron.Bridge", "BridgeServer.cs")
IDENTITY = os.path.join(ROOT, "revit", "Heron.Bridge", "BridgeIdentity.cs")
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


class _Pipe(object):
    """A pipe handle that records what was written to it."""

    def __init__(self):
        self.written, self.closed = [], False

    def write(self, data):
        self.written.append(data)

    def close(self):
        self.closed = True


def client():
    print("1. the chat refuses a pipe another process serves - before writing a byte")
    import heron_bridge_client as B
    mismatch = getattr(B, "server_mismatch", None)
    check(mismatch is not None and hasattr(B, "server_pid"),
          "the client has server_mismatch and server_pid")
    if mismatch is None:
        return
    check(mismatch(4321, 4321) is None, "the Revit the session names: used")
    said = mismatch(4321, 999)
    check(said is not None and "NOTHING was sent" in said and "999" in said and "4321" in said,
          "another process: refused, naming both processes")
    check(mismatch(None, 999) is None and mismatch(4321, None) is None
          and mismatch("x", 4) is None,
          "a pid that could not be read is never a mismatch - refused only on evidence")
    if os.name != "nt":
        check(B.server_pid(_Pipe()) is None, "off Windows the server is not asked, and nothing raises")

    bridge = B.Bridge({"pid": 4321, "pipeName": "heron-test", "token": "secret"}, "/nowhere")
    pipe = _Pipe()
    real_open, real_pid = getattr(B, "open", None), B.server_pid
    B.open = lambda *a, **k: pipe
    B.server_pid = lambda handle: 999
    try:
        reply = bridge.request("ping", timeout=0.2)
    finally:
        B.server_pid = real_pid
        if real_open is None:
            del B.open
        else:
            B.open = real_open
    check(isinstance(reply, dict) and reply.get("error") == "wrong_server",
          "the request is answered wrong_server - found %r" % (reply,))
    check(pipe.closed and not pipe.written,
          "the pipe was closed with NOTHING written to it - the token never left")

    import importlib
    sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
    F = importlib.import_module("heron_failure")
    failure = F.analyse(reply if isinstance(reply, dict) else None, writes=True)
    check(failure is not None and failure.outcome == F.REFUSED,
          "and it is a refusal - nothing ran - not an unknown outcome")


def addin():
    print("\n2. the add-in denies network logons on the pipe, and narrows the token's file")
    server = io.open(SERVER, encoding="utf-8").read()
    deny = server[server.find("void DenyNetwork("):server.find("NamedPipeServerStream CreatePipe(")]
    check("WellKnownSidType.NetworkSid" in deny and "AccessControlType.Deny" in deny,
          "a deny rule for NT AUTHORITY\\NETWORK")
    create = server[server.find("NamedPipeServerStream CreatePipe("):]
    create = create[:create.find("string ReadLine") if "string ReadLine" in create else 6000]
    check(create.count("DenyNetwork(security);") == 2,
          "added on BOTH .NET branches - Framework (2020-2024) and .NET 8+ (2025 on)")
    identity = io.open(IDENTITY, encoding="utf-8").read()
    publish = identity[identity.find("public void Publish()"):identity.find("public void Unpublish()")]
    check(re.search(r"HeronAtomicWrite\.WriteAllText\(DiscoveryFilePath,[^;]*\);\s*"
                    r"OnlyThisUser\(DiscoveryFilePath\);", publish) is not None,
          "the token's file is narrowed to this user right after it is written")
    only = publish[publish.find("void OnlyThisUser("):]
    check("SetAccessRuleProtection(true, false)" in only and "catch (Exception" in only
          and "OperatingSystem.IsWindows()" in only,
          "its folder's rules no longer inherited; Windows only; and never a reason the bridge "
          "fails to start")


def main():
    client()
    addin()
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the pipe refuses another computer, and the chat checks who serves it")
    return 0


if __name__ == "__main__":
    sys.exit(main())
