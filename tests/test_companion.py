#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Heron Companion, Phase 1 (D-108, docs/40): a page that shows, and the
rules that keep it safe - run against a REAL server on this machine.

What each group holds:

  * PROTECTION (docs/40 section 11). 127.0.0.1 only; a wrong Host is refused;
    every /api call needs the custom header and the paired cookie; pairing
    needs this server's own Origin, and a one-time code works once; nothing
    answers a CORS pre-flight; the page cannot be framed.
  * SILENCE. The server lives inside the MCP process, which speaks to Claude
    Code on stdout. One byte printed there corrupts the protocol, so the whole
    run is watched and must leave stdout empty.
  * NO WAY TO AN AI. The Companion's source may import nothing that opens an
    outgoing connection or names a model provider.
  * THE LIVE FILE. Only a Revit whose bridge is connected is shown; a file left
    by a crash, a half-written file or one naming another process is skipped;
    and with several Revits and no binding, NONE is picked (Article 12a).
  * THE ADD-IN SIDE, read from the C#: one path on every release (Idling,
    throttled, no version branch), and nothing written unless the bridge is
    connected.

    python tests/test_companion.py
"""

import http.client
import io
import json
import os
import re
import sys
import tempfile
import time

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "companion"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_companion as hc                                 # noqa: E402
import heron_tools as tools                                  # noqa: E402

SOURCE = os.path.join(ROOT, "mcp", "companion", "heron_companion.py")
PAGE_JS = os.path.join(ROOT, "mcp", "companion", "static", "companion.js")
LIVE_CS = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronLiveState.cs")
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def ask(port, method, path, headers=None, body=None, host=None):
    conn = http.client.HTTPConnection("127.0.0.1", port, timeout=5)
    sent = {"Host": host or "127.0.0.1:%d" % port}
    sent.update(headers or {})
    conn.request(method, path, body=body, headers=sent)
    res = conn.getresponse()
    data = res.read()
    headers_out = {k.lower(): v for k, v in res.getheaders()}
    conn.close()
    return res.status, headers_out, data


def write(path, payload):
    with io.open(path, "w", encoding="utf-8") as fh:
        fh.write(payload if isinstance(payload, str) else json.dumps(payload))


def live_record(pid, title="heron ai bulding", count=3):
    return {"format": 1, "pid": pid, "revitVersion": "2024",
            "document": {"title": title, "path": None},
            "view": {"name": "Level 1", "type": "FloorPlan", "id": 312},
            "selection": {"count": count, "categories": {"Ducts": count},
                          "ids": [1, 2, 3][:count], "idsTruncated": False,
                          "countCapped": False}}


def test_live_files(tmp):
    print("The live file: only a connected Revit, never a guess among several")
    live = os.path.join(tmp, "live")
    bridges = os.path.join(tmp, "bridges")
    os.makedirs(live)
    os.makedirs(bridges)

    check(hc.state(None, live, bridges)["choice"] == "none",
          "nothing connected -> 'none', nothing shown")

    write(os.path.join(live, "pid-100.json"), live_record(100))
    check(hc.state(None, live, bridges)["choice"] == "none",
          "a live file whose Revit has no discovery file (a crash) is not shown")

    write(os.path.join(bridges, "100.json"), {"pid": 100})
    got = hc.state(None, live, bridges)
    check(got["choice"] == "only" and got["revit"]["pid"] == 100,
          "one connected Revit, unbound -> shown, labelled the only one")
    check(isinstance(got["revit"].get("updatedSecondsAgo"), int),
          "and it says how long ago Revit last wrote")

    write(os.path.join(live, "pid-200.json"), live_record(200, "Tower A"))
    write(os.path.join(bridges, "200.json"), {"pid": 200})
    got = hc.state(None, live, bridges)
    check(got["choice"] == "several" and got["revit"] is None and got["others"] == 2,
          "two connected and this chat has chosen none -> NEITHER is picked (Article 12a)")

    got = hc.state(200, live, bridges)
    check(got["choice"] == "bound" and got["revit"]["document"]["title"] == "Tower A",
          "bound to one -> exactly that one, whatever else is connected")

    got = hc.state(300, live, bridges)
    check(got["choice"] == "bound-gone" and got["revit"] is None,
          "bound to a Revit that is gone -> nothing shown, never slides onto another")

    write(os.path.join(live, "pid-200.json"), '{"format": 1, "pid": 20')
    check(200 not in hc.read_live(live, bridges),
          "a half-written file is skipped, not shown")

    write(os.path.join(live, "pid-200.json"), live_record(999))
    check(200 not in hc.read_live(live, bridges),
          "a file naming a different process than its own name is skipped")


def test_server(tmp):
    print()
    print("Protection: a real server on this machine")
    live = os.path.join(tmp, "live")
    bridges = os.path.join(tmp, "bridges")
    companion = hc.Companion(bound_pid=lambda: 100, folder=live, discovery_dir=bridges)

    real_stdout, sys.stdout = sys.stdout, io.StringIO()
    try:
        address = companion.pairing_address()
        port = companion.port
        results = {}

        results["bound"] = companion._server.server_address[0]
        results["page"] = ask(port, "GET", "/")
        results["wrong_host"] = ask(port, "GET", "/api/state",
                                    {"X-Heron-Companion": "1"}, host="evil.example:%d" % port)
        results["no_header"] = ask(port, "GET", "/api/state")
        results["not_paired"] = ask(port, "GET", "/api/state", {"X-Heron-Companion": "1"})

        code = address.split("?pair=", 1)[1]
        origin = "http://127.0.0.1:%d" % port
        body = json.dumps({"code": code})
        results["no_origin"] = ask(port, "POST", "/api/pair",
                                   {"X-Heron-Companion": "1", "Content-Type": "application/json"}, body)
        results["bad_origin"] = ask(port, "POST", "/api/pair",
                                    {"X-Heron-Companion": "1", "Origin": "http://evil.example",
                                     "Content-Type": "application/json"}, body)
        results["pair"] = ask(port, "POST", "/api/pair",
                              {"X-Heron-Companion": "1", "Origin": origin,
                               "Content-Type": "application/json"}, body)
        results["pair_again"] = ask(port, "POST", "/api/pair",
                                    {"X-Heron-Companion": "1", "Origin": origin,
                                     "Content-Type": "application/json"}, body)
        cookie = results["pair"][1].get("set-cookie", "").split(";", 1)[0]
        results["state"] = ask(port, "GET", "/api/state",
                               {"X-Heron-Companion": "1", "Cookie": cookie})
        results["state_no_header"] = ask(port, "GET", "/api/state", {"Cookie": cookie})
        results["options"] = ask(port, "OPTIONS", "/api/state",
                                 {"Origin": "http://evil.example",
                                  "Access-Control-Request-Method": "GET",
                                  "Access-Control-Request-Headers": "x-heron-companion"})

        # An expired code, forced rather than waited for.
        stale = companion.pairing_address().split("?pair=", 1)[1]
        companion._codes[stale] = time.time() - 1
        results["expired"] = ask(port, "POST", "/api/pair",
                                 {"X-Heron-Companion": "1", "Origin": origin,
                                  "Content-Type": "application/json"},
                                 json.dumps({"code": stale}))
    finally:
        printed, sys.stdout = sys.stdout.getvalue(), real_stdout
        companion.stop()

    check(results["bound"] == "127.0.0.1", "listens on 127.0.0.1 only, never 0.0.0.0")
    status, headers, data = results["page"]
    check(status == 200 and b"Heron Companion" in data, "the page itself is served")
    check("frame-ancestors 'none'" in headers.get("content-security-policy", "")
          and headers.get("x-frame-options") == "DENY",
          "and it may not be put in a frame (clickjacking)")
    check("script-src 'self'" in headers.get("content-security-policy", ""),
          "and it runs no script from anywhere else")
    check(results["wrong_host"][0] == 421, "a request naming another Host is refused (DNS rebinding)")
    check(results["no_header"][0] == 403, "/api without the custom header is refused")
    check(results["not_paired"][0] == 403, "/api without the paired cookie is refused")
    check(results["no_origin"][0] == 403, "pairing without an Origin is refused")
    check(results["bad_origin"][0] == 403, "pairing from another site's Origin is refused")
    status, headers, _ = results["pair"]
    check(status == 200 and "HttpOnly" in headers.get("set-cookie", "")
          and "SameSite=Strict" in headers.get("set-cookie", ""),
          "the one-time code buys an HttpOnly, SameSite=Strict cookie")
    check(results["pair_again"][0] == 403, "the same code a second time is refused")
    check(results["expired"][0] == 403, "an expired code is refused")
    status, _, data = results["state"]
    check(status == 200 and json.loads(data.decode("utf-8"))["ok"] is True,
          "paired, with the header -> the state is answered")
    check(results["state_no_header"][0] == 403, "the cookie alone is not enough without the header")
    status, headers, _ = results["options"]
    check(status == 405 and not any(k.startswith("access-control-") for k in headers),
          "a CORS pre-flight is refused and no Access-Control header is ever sent")
    check(printed == "", "NOTHING reached stdout, where MCP speaks (got %r)" % printed[:80])
    check(not companion.running, "and it stops when asked")


def test_no_way_to_an_ai():
    print()
    print("No way to an AI, and no address in a reply")
    text = io.open(SOURCE, encoding="utf-8").read()
    imports = "\n".join(l for l in text.splitlines() if re.match(r"\s*(import|from)\s", l))
    for banned in ("urllib", "http.client", "requests", "httpx", "aiohttp", "websocket",
                   "anthropic", "openai", "create_connection"):
        check(banned not in imports, "the Companion imports nothing like '%s'" % banned)
    check("create_connection" not in text and "connect((" not in text,
          "and opens no outgoing socket anywhere in its code")

    server = io.open(SERVER, encoding="utf-8").read()
    body = server[server.index("def heron_companion("):]
    body = body[:body.index("\n@server.tool()")] if "\n@server.tool()" in body else body
    returns = [l for l in body.splitlines() if l.strip().startswith("return")]
    check(returns and not any("address" in l for l in returns),
          "heron_companion never puts the one-time address in its reply to the chat")
    check(tools.TOOLS.get("heron_companion") == (tools.READ, None),
          "heron_companion is declared READ with no Revit operation")

    js = io.open(PAGE_JS, encoding="utf-8").read()
    check("innerHTML" not in js and "insertAdjacentHTML" not in js,
          "the page puts model text on screen as text, never as HTML")


def test_addin_side():
    print()
    print("The add-in side, read from HeronLiveState.cs")
    text = io.open(LIVE_CS, encoding="utf-8").read()
    check("application.Idling += OnIdling;" in text
          and not re.search(r"^\s*#(if|else|endif)", text, re.M),
          "one path on every release: the selection is watched on Idling, with no version branch")
    check("SinceIdleCheck.Elapsed < IdleInterval" in text,
          "and each Idling call ends at once unless 200 ms have passed (D-108 amends D-09)")
    write_body = text[text.index("private static void Write("):]
    write_body = write_body[:write_body.index("private static void Delete(")]
    check(write_body.index("Connected()") < write_body.index("WriteAllText"),
          "nothing is written unless the bridge is connected")
    check("Transaction" not in text, "it opens no transaction")
    check("System.Net" not in text, "and touches no network")


def main():
    tmp = tempfile.mkdtemp(prefix="heron-companion-")
    test_live_files(tmp)
    test_server(tmp)
    test_no_way_to_an_ai()
    test_addin_side()

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1
    print("PASSED - the Companion shows, stays silent, and cannot be reached from elsewhere.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
