# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-MCP-AUT-006
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Who may call - the bridge's three promises, checked against the C#.

    python tests/test_authentication.py

WHY IT IS A PYTHON SUITE ABOUT C#
-----------------------------------
HERON-MCP-AUT-006 is "who may call - pipe ACLs, local-only enforcement",
and it was already enforced before it was ever named: the ACL is in
`BridgeServer.CreatePipe`, the token is in `BridgeIdentity`, and the
check is the first line of `Dispatch`. Nothing was missing except the
header saying so, and a second implementation in Python would have been
a second place for a security rule to be right in.

So the header now claims the id and this suite turns three comments into
three checked statements. `BridgeServer.cs` says:

    "Local only by construction. A named pipe has no network surface,
    the ACL restricts it to the current user, and a per-session token
    means another process of that same user cannot reach Revit without
    first reading the discovery file."

Three claims. A comment that says a thing is secure is worth what the
code behind it is worth, and until now nothing compared the two.

WHAT IT PROVES
  1. NO NETWORK SURFACE, by absence - and the absence IS the guarantee.

  2. THE ACL NAMES ONE PRINCIPAL, the current user, and no well-known
     group anywhere.

  3. THE TOKEN IS CHECKED BEFORE ANYTHING ELSE, including before the
     operation is read - so an unauthenticated caller learns nothing
     about what the bridge can do. That is an ORDER inside one method,
     and it is checked as an order.

  4. THE COMPARISON IS CONSTANT TIME over a fixed-length token, so the
     length test leaks nothing.

  5. A NEW SESSION INVALIDATES EVERY EARLIER TOKEN, and ending one
     leaves nothing to replay.

  6. BOTH BUILD BRANCHES EXIST - .NET Framework supplies the ACL and
     .NET 8 relies on the default. Neither may be quietly dropped.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BRIDGE = os.path.join(ROOT, "revit", "Heron.Bridge")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(name):
    return io.open(os.path.join(BRIDGE, name), encoding="utf-8").read()


def body(source, signature):
    """One method's text, from its signature to the matching close brace."""
    at = source.index(signature)
    depth, i = 0, source.index("{", at)
    start = i
    while i < len(source):
        if source[i] == "{":
            depth += 1
        elif source[i] == "}":
            depth -= 1
            if depth == 0:
                return source[start:i + 1]
        i += 1
    raise AssertionError("no matching brace for %s" % signature)


def main():
    server = read("BridgeServer.cs")
    identity = read("BridgeIdentity.cs")

    print("0. The file claims the agent it implements")
    check(server.splitlines()[0].strip().endswith("HERON-MCP-AUT-006"),
          "BridgeServer.cs claims HERON-MCP-AUT-006")
    register = io.open(os.path.join(ROOT, "docs", "28-agent-registry.md"),
                       encoding="utf-8").read()
    row = [line for line in register.splitlines() if "MCP-AUT-006" in line]
    check(len(row) == 1, "and docs/28 declares it once")
    said = row[0].lower()
    check("pipe acl" in said and "local-only enforcement" in said,
          "with the job this file does: pipe ACLs, local-only enforcement")
    check("ADMIN" in row[0],
          "at ADMIN risk, the highest the register has")

    print("\n1. No network surface - and the absence IS the guarantee")
    every = "\n".join(read(name) for name in sorted(os.listdir(BRIDGE))
                      if name.endswith(".cs"))
    for surface in ("TcpListener", "TcpClient", "Socket", "HttpListener",
                    "HttpClient", "WebClient", "UdpClient", "localhost",
                    "127.0.0.1", "0.0.0.0", "IPAddress", "IPEndPoint"):
        check(surface not in every,
              "nothing in Heron.Bridge mentions %s" % surface)
    check("NamedPipeServerStream" in server,
          "the transport is a named pipe, which has no network surface at "
          "all - there is no port to bind and none to firewall")
    check("System.IO.Pipes" in server and "System.Net" not in every,
          "and System.Net is never even imported")

    print("\n2. The ACL names one principal")
    made = body(server, "private NamedPipeServerStream CreatePipe()")

    # PER BRANCH, NOT ACROSS THE WHOLE METHOD. These three used to count over
    # the method entire and expect ONE of each, which was true only while the
    # .NET 8+ branch passed no PipeSecurity at all - and that turned out to be
    # the defect, not the design (section 6 below, and FRAGMENT-ISSUES 5b row
    # 23). Counting over the whole body would now demand the two branches
    # SHARE one rule between them, which is the opposite of what is wanted.
    # Each branch gets its own, and each must name exactly one principal.
    halves = made.split("#else")
    check(len(halves) == 2, "CreatePipe has exactly two build branches")
    for label, half in (("the .NET Framework branch", halves[0]),
                        ("the .NET 8+ branch", halves[-1])):
        check("WindowsIdentity.GetCurrent().User" in half,
              "%s names the CURRENT USER" % label)
        check(half.count("AddAccessRule") == 1,
              "%s adds exactly one rule (%d)" % (label, half.count("AddAccessRule")))
        check(half.count("AccessControlType.Allow") == 1,
              "%s has exactly one Allow" % label)
    for group in ("WellKnownSidType", "Everyone", "AuthenticatedUsers",
                  "NetworkService", "BUILTIN", "S-1-1-0", "Users",
                  "SecurityIdentifier("):
        check(group not in made,
              "no %s - the pipe is not opened to a group" % group)
    check("AccessControlType.Deny" not in made,
          "and no Deny rule, which would imply somebody else was allowed "
          "in the first place")

    print("\n3. The token is checked before anything else")
    dispatch = body(server, "private string Dispatch(string request)")
    checked_at = dispatch.index("TokenMatches")
    check("TokenMatches" in dispatch, "Dispatch authenticates")
    read_op = dispatch.index('Json.ReadString(request, "op")')
    check(checked_at < read_op,
          "and it does so BEFORE the operation is even read, so an "
          "unauthenticated caller learns nothing about what the bridge "
          "can do")
    for op in ('"ping"', '"info"'):
        check(checked_at < dispatch.index(op),
              "  before %s is considered" % op)
    # THE ORDER IS THE POINT, so prove it is not merely the first mention.
    before = dispatch[:checked_at]
    check("op ==" not in before and "switch" not in before,
          "nothing branches on the request before the check")
    check("unauthorized" in dispatch,
          "and a wrong token gets one word back: unauthorized")
    refusal = dispatch[checked_at:checked_at + 400]
    check("ping" not in refusal and "info" not in refusal,
          "whose message names no operation - only where to read the token")

    print("\n4. Constant time, over a fixed-length token")
    compare = body(identity, "public bool TokenMatches(string candidate)")
    check("difference |=" in compare and "^" in compare,
          "the comparison accumulates a difference with |= and ^")
    check("return difference == 0" in compare,
          "and returns only after the whole loop")
    check("return true" not in compare,
          "there is no early exit on a match")
    check("for (" in compare, "it loops over the whole token")
    loop = compare[compare.index("for ("):compare.index("return difference")]
    check("return" not in loop,
          "and there is no return anywhere inside that loop - an early "
          "return is exactly what leaks the token one character at a time")
    check(compare.count("return") == 3,
          "three returns in the whole method (%d): two refusals before the "
          "loop and the verdict after it" % compare.count("return"))
    check("expected == candidate" not in compare
          and "Equals(" not in compare,
          "no plain comparison anywhere in it")
    mint = body(identity, "public void BeginSession()")
    size = re.search(r"new byte\[(\d+)\]", mint)
    check(size is not None and int(size.group(1)) >= 16,
          "the token is %s random bytes"
          % (size.group(1) if size else "an unknown number of"))
    check("RandomNumberGenerator.Create()" in mint,
          "from the platform CSPRNG, not Random()")
    check("Convert.ToBase64String" in mint,
          "base64 of a fixed byte count, so every token is the SAME "
          "length - which is why the length test in the comparison leaks "
          "nothing")

    print("\n5. A new session invalidates every earlier token")
    check("invalidating every earlier one" in mint
          or "invalidating every earlier one" in identity,
          "BeginSession says it invalidates every earlier token")
    check("BeginSession()" in server,
          "and the server calls it on start")
    ends = body(identity, "public void EndSession()")
    check("Token = null" in ends,
          "EndSession drops the token rather than remembering it")
    check("expected == null" in compare and "return false" in compare,
          "and a null token matches nothing - not even null, which would "
          "make an ended session accept every caller")

    print("\n6. Both build branches exist")
    check("#if NET472 || NET48" in made,
          ".NET Framework supplies the ACL explicitly")
    check("#else" in made and "#endif" in made,
          "and .NET 8 has its own branch")
    after = made.split("#else")[1]

    # THIS CHECK USED TO ASSERT THE OPPOSITE, and it was wrong. It read
    # `"PipeSecurity" not in after` - "which passes no PipeSecurity, the
    # default ACL already restricts to the creating user there" - repeating
    # the claim the source made. Measured 2026-09-21 by creating the pipe with
    # exactly those arguments on net8.0-windows and reading its ACL back: the
    # default grants READ to the world group and to the anonymous logon
    # account as well as to the user. The Framework branch gave one rule; that
    # one gave five. So the branch now asks for the ACL too, and this asserts
    # it does. FRAGMENT-ISSUES section 5b, row 23.
    check("PipeSecurity" in after,
          "and it asks for an ACL rather than trusting the default, which "
          "was measured and does NOT restrict the pipe to the creating user")
    check("NamedPipeServerStreamAcl.Create" in after,
          "through NamedPipeServerStreamAcl.Create - .NET Core has no "
          "NamedPipeServerStream constructor that takes a PipeSecurity")
    check("security" in made.split("#else")[0],
          "while the first branch passes one to its constructor")
    check("CreateNewInstance" in made,
          "and CreateNewInstance is granted, without which only the FIRST "
          "pipe instance can be made")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    local only, one principal, and the token checked first")
    return 0


if __name__ == "__main__":
    sys.exit(main())
