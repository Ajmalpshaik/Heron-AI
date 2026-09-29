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
    # The test's Revits are made up, so none is a running process: say they
    # run, and prove the dead case on its own below.
    real_alive = hc.revit_alive
    hc.revit_alive = lambda pid: pid != 666
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

    write(os.path.join(live, "pid-666.json"), live_record(666, "Killed"))
    write(os.path.join(bridges, "666.json"), {"pid": 666})
    check(666 not in hc.read_live(live, bridges),
          "a Revit that was killed - both files left behind - is not shown as connected")
    hc.revit_alive = real_alive


def test_server(tmp):
    print()
    print("Protection: a real server on this machine")
    live = os.path.join(tmp, "live")
    bridges = os.path.join(tmp, "bridges")
    companion = hc.Companion(bound_pid=lambda: 100, folder=live, discovery_dir=bridges,
                             is_alive=lambda pid: True)

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
    check("return binding.pid if binding.was_chosen else None" in server
          and "bound_pid=_companion_revit" in server
          and "bound_pid=lambda: binding.pid" not in server,
          "the page calls a Revit this chat's only when the modeller CHOSE it")
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


def test_switch(tmp):
    print()
    print("The Companion switch (D-109): on keeps the page and the note; off removes both")
    config = os.path.join(tmp, "heron.config")
    notes = os.path.join(tmp, "notes")
    companion = hc.Companion(bound_pid=lambda: 4242, folder=os.path.join(tmp, "live"),
                             discovery_dir=os.path.join(tmp, "bridges"))

    pending = []
    real_stdout, sys.stdout = sys.stdout, io.StringIO()
    try:
        write(config, "companion.enabled = true\n")
        on = companion.keep_once(config, notes)
        path = os.path.join(notes, "chat-%d.json" % os.getpid())
        note = json.load(io.open(path, encoding="utf-8")) if os.path.exists(path) else {}
        pending.append((on and companion.running, "switch on -> the page is served"))
        pending.append((note.get("port") == str(companion.port) and note.get("revitPid") == "4242"
              and note.get("mcpPid") == str(os.getpid()),
              "and the note names this chat, its port and the Revit it is bound to"))
        code = note.get("code", "")
        pending.append((re.match(r"^[A-Za-z0-9_-]{16,64}$", code) is not None,
              "the note's code is one the Revit button accepts"))

        first = companion.redeem(code)
        pending.append((first is not None, "the note's code opens the page once"))
        pending.append((companion.redeem(code) is None, "and never a second time"))
        companion.keep_once(config, notes)
        fresh = json.load(io.open(path, encoding="utf-8")).get("code")
        pending.append((fresh and fresh != code, "the next look writes a fresh code into the note"))

        write(config, "companion.enabled = false\n")
        off = companion.keep_once(config, notes)
        pending.append((not off and not companion.running and not os.path.exists(path),
              "switch off -> the page stops and the note is gone"))
        pending.append((hc.enabled(config) is False and hc.enabled(os.path.join(tmp, "absent")) is True,
              "off is read from the file; a missing file means the default, on"))
    finally:
        printed, sys.stdout = sys.stdout.getvalue(), real_stdout
        companion.stop()
        companion.unpublish()
    for condition, what in pending:
        check(condition, what)
    check(printed == "", "and still nothing reached stdout")

    cs = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronCompanionCommands.cs"),
                 encoding="utf-8").read()
    check("127.0.0.1:" in cs and "Alive(owner)" in cs,
          "the Revit button opens only 127.0.0.1, and only for a chat still running")
    check("System.Net" not in cs and "Socket" not in cs,
          "and Revit opens no socket to do it")
    live = io.open(LIVE_CS, encoding="utf-8").read()
    check("if (!Connected() || !_enabled) return;" in live,
          "the add-in writes nothing while the Companion is off")
    check("if (seen == _lastSeen) return;" in live,
          "and Idling stops before reading any element when nothing changed")


def test_activity():
    print()
    print("Phase 2: the activity list")
    act = hc.Activity()
    act.record("revit_read", "10:00:00", 0.42, reply="Ducts: 24 in Level 1\nmore lines")
    act.record("revit_change", "10:00:05", 1.2,
               reply="add-project-parameter is declared risk: ADMIN ... Nothing was sent to Revit.")
    act.record("revit_views", "10:00:09", 0.1, error=RuntimeError("pipe closed"))
    items = act.since(0)
    check([i["outcome"] for i in items] == ["ok", "refused", "failed"],
          "ok / refused / failed, read from the answer's own words")
    check(hc.Activity.outcome("Revit 2024 did not answer.") == "failed"
          and hc.Activity.outcome("Heron could not read its own session list") == "failed",
          "an answer saying the work did not happen is failed, not OK")
    check(hc.Activity.outcome("12 gaps: 3 were refused or failed", tool="heron_gaps") == "ok"
          and hc.Activity.outcome("Filters read.\n'X' failed its check", tool="revit_read") == "ok"
          and hc.Activity.outcome("Revit 2024 did not answer.", tool="revit_read") == "failed",
          "only a Revit-facing tool's FIRST line decides failed - a report may quote the word")
    check(items[0]["summary"] == "Ducts: 24 in Level 1",
          "only the first line of an answer is kept")
    check(len(act.since(2)) == 1 and act.since(2)[0]["tool"] == "revit_views",
          "the page asks only for what is newer than it has")
    for n in range(hc.Activity.LIMIT + 20):
        act.record("t", "x", 0, reply="x" * 400)
    check(len(act.since(0)) == hc.Activity.LIMIT
          and len(act.since(0)[-1]["summary"]) <= hc.Activity.FIRST_LINE,
          "at most 500 kept, each line at most 200 characters")

    server = io.open(SERVER, encoding="utf-8").read()
    check("register(*args, **options)(_recorded(fn))" in server
          and "@functools.wraps(fn)" in server,
          "every tool is recorded through one wrapper that keeps its signature")


def test_changes():
    print()
    print("Phase 3: the after-change tables - same capability, same names, one line each")
    import glob
    import yaml
    changes = hc.Changes()
    changes.offer("SET_CATEGORY_GRAPHICS", "Project1",
                  [("view", "Level 1"), ("categories", "Ducts"),
                   ("overrides", "projection-line-colour=255,0,0; surface-foreground-colour=255,0,0")])
    card = changes.cards()[0]
    check(changes.apply(card["id"], [])["ok"] is False,
          "with no chat ready to apply, nothing is sent")

    sent = []
    changes.apply_hook = lambda capability, pairs, identity=None: (sent.append((capability, pairs))
                                                     or "SET_CATEGORY_GRAPHICS ran in Project1.")
    same = [{"name": r["name"], "value": r["value"]} for r in card["rows"]]
    renamed = [dict(v) for v in same]
    renamed[0]["name"] = "viewName"
    check(changes.apply(card["id"], renamed)["ok"] is False and not sent,
          "a renamed value is refused and nothing is sent")
    check(changes.apply(card["id"], same[:2])["ok"] is False and not sent,
          "a dropped value is refused")
    smuggled = [dict(v) for v in same]
    smuggled[1]["value"] = "Ducts" + chr(10) + "overrides=none"
    check(changes.apply(card["id"], smuggled)["ok"] is False and not sent,
          "a line break - which would smuggle in a second value - is refused")

    blue = [dict(v) for v in same]
    blue[2]["value"] = "projection-line-colour=0,0,255; surface-foreground-colour=0,0,255"
    result = changes.apply(card["id"], blue)
    check(result["ok"] and result["outcome"] == "ok"
          and sent == [("SET_CATEGORY_GRAPHICS", [(v["name"], v["value"]) for v in blue])],
          "an edited value goes to the SAME capability with the same names")
    check(changes.cards()[0]["rows"][2]["value"].startswith("projection-line-colour=0,0,255"),
          "and the table keeps the values that were applied")
    check(changes.apply(999, blue)["ok"] is False, "a table that is gone cannot be applied")

    many = hc.Changes()
    for name, rgb in (("Exhaust Air", "139,69,19"), ("Return Air", "255,0,255"),
                      ("Supply Air", "0,0,255")):
        many.offer("APPLY_VIEW_FILTER", "", [("view", "FloorPlan: 1 - Mech"), ("filter", name),
                   ("overrides", "projection-line-colour=" + rgb), ("visible", "true")])
    check(len(many.cards()) == 3,
          "three filters offered one after another keep three tables, not the last one")
    many.offer("APPLY_VIEW_FILTER", "", [("view", "FloorPlan: 1 - Mech"), ("filter", "Supply Air"),
               ("overrides", "projection-line-colour=0,255,0"), ("visible", "true")])
    check(len(many.cards()) == 3 and many.cards()[0]["rows"][2]["value"].endswith("0,255,0"),
          "a new change to the SAME filter replaces its table rather than adding a fourth")
    mats = hc.Changes()
    for name in ("100", "200"):
        mats.offer("SET_MATERIAL_COLOUR", "", [("materialName", name), ("colour", "255,0,0")],
                   None, ["materialName"])
    check(len(mats.cards()) == 2,
          "the card names its subject: materials called 100 and 200 keep two tables")

    marked = []
    for card_path in glob.glob(os.path.join(ROOT, "brain", "fragments", "*", "fragment.yaml")):
        with io.open(card_path, encoding="utf-8") as fh:
            text = fh.read()
        if "companion: settings" not in text:
            continue
        data = yaml.safe_load(text)
        needs = (data.get("contract") or {}).get("needs") or []
        marked.append(os.path.basename(os.path.dirname(card_path)))
        subject = [s.strip() for s in str(data.get("companion-subject") or "").split(",")
                   if s.strip()]
        check(subject and set(subject) <= set(n.get("name") for n in needs),
              "%s: names which of its inputs say WHICH THING a table is about"
              % marked[-1])
        check(data.get("risk") == "MODIFY" and all(
            n.get("source") == "request" or n.get("type") in
            ("Document", "UIDocument", "UIApplication", "Application") for n in needs),
            "%s: MODIFY, and every input a typed value - never the selection" % marked[-1])
    check("set-category-graphics" in marked and "override-graphics-in-view" not in marked,
          "category graphics is offered; per-element overrides (read from the selection) are not")

    server = io.open(SERVER, encoding="utf-8").read()
    check('_through(revit_change, origin="companion")(capability, values)' in server
          and '_through(revit_change, reply_out=out, origin="companion")(' in server
          and 'body = getattr(tool, "__wrapped__", tool)' in server,
          "Apply runs revit_change's OWN body - no second write path")
    check("with _revit_lock:" in server and "def _settings_card(folder):" in server,
          "and holds the same lock every tool call holds, so it never displaces the chat")
    check('"companion_apply":' in io.open(os.path.join(ROOT, "mcp", "server", "heron_tools.py"),
                                          encoding="utf-8").read(),
          "the Apply action's risk is declared in heron_tools, at MODIFY")


def test_tables():
    print()
    print("Phase 3: the element table - the old value is Heron's, never the page's")
    import yaml
    tables = hc.Tables()
    tables.open("Project1", {"columns": ["Mark", "Comments"], "truncated": 0, "rows": [
        {"id": "101", "uniqueId": "u-101", "cells": {
            "Mark": {"value": "FCU-01", "editable": True},
            "Comments": {"value": "shown", "editable": False, "why": "read-only"}}}]})
    tid = tables.current()["id"]
    check(tables.apply(tid, [{"id": "101", "name": "Mark", "value": "X"}])["ok"] is False,
          "with no chat ready to apply, nothing is sent")

    sent = []
    def hook(rows, identity=None):
        sent.append(rows)
        return "SET_PARAMETER_VALUES_BY_ID ran in Project1.", {
            "applied": True, "stale": [],
            "readBack": [{"id": "101", "name": "Mark", "value": "FCU-1A"}]}
    tables.apply_hook = hook
    check(tables.apply(tid, [{"id": "101", "name": "Comments", "value": "x"}])["ok"] is False
          and not sent, "a cell Heron marked not editable is refused and nothing is sent")
    check(tables.apply(tid, [{"id": "999", "name": "Mark", "value": "x"}])["ok"] is False
          and not sent, "a row that is not in the table is refused")
    check(tables.apply(tid, [{"id": "101", "name": "Mark", "value": "a" + chr(10) + "b"}])["ok"]
          is False and not sent, "a value with a line break is refused")
    check(tables.apply(tid + 1, [{"id": "101", "name": "Mark", "value": "x"}])["ok"] is False,
          "a table that is no longer open cannot be applied")

    result = tables.apply(tid, [{"id": "101", "name": "Mark", "value": "FCU-1A", "was": "LIE"}])
    check(result["applied"] and sent == [[("101", "u-101", "Mark", "FCU-01", "FCU-1A")]],
          "the old value sent is the one Heron showed - a 'was' from the page is ignored")
    check(tables.current()["rows"][0]["cells"]["Mark"]["value"] == "FCU-1A",
          "after Apply the table shows what Revit read back")

    tables.apply_hook = lambda rows, identity=None: ("SET_PARAMETER_VALUES_BY_ID ran in Project1.", {
        "applied": False, "readBack": [],
        "stale": [{"id": "101", "name": "Mark", "why": "it was changed in Revit"}]})
    stopped = tables.apply(tid, [{"id": "101", "name": "Mark", "value": "FCU-1B"}])
    last = tables.current()["last"]
    check(stopped["applied"] is False and last["outcome"] == "refused" and last["stale"]
          and tables.current()["rows"][0]["cells"]["Mark"]["value"] == "FCU-1A",
          "a stale row stops the Apply, is marked, and the table keeps what Revit holds")

    folder = os.path.join(ROOT, "brain", "fragments")
    writer = yaml.safe_load(io.open(os.path.join(folder, "set-parameter-values-by-id",
                                                 "fragment.yaml"), encoding="utf-8"))
    names = [n["name"] for n in writer["contract"]["needs"]]
    check(writer["risk"] == "MODIFY" and "elements" not in names,
          "the write takes its elements from the rows, never from the selection")
    code = io.open(os.path.join(folder, "set-parameter-values-by-id", "impl", "any",
                                "fragment.cs"), encoding="utf-8").read()
    check(code.index("if (stale.Count > 0)") < code.index("p.SetValueString(")
          and "throw new InvalidOperationException" in code,
          "it checks every row before writing any, and a value Revit refuses rolls all back")
    reader = io.open(os.path.join(folder, "read-element-table", "impl", "any", "fragment.cs"),
                     encoding="utf-8").read()
    check("a type parameter - changing it changes every element of this type" in reader
          and "matches.Count > 1" in reader,
          "the table marks type parameters and shared names as not editable")
    check(tools.TOOLS.get("revit_edit_table") == (tools.ANALYZE, "run_fragment_read")
          and tools.COMPANION_ACTIONS.get("companion_table_apply") == (tools.MODIFY,
                                                                       "run_fragment_write"),
          "opening a table reads (ANALYZE); applying it writes (MODIFY), each declared once")
    server = io.open(SERVER, encoding="utf-8").read()
    offer = server[server.index("def revit_offer_settings("):server.index("def revit_edit_table(")]
    check(tools.TOOLS.get("revit_offer_settings") == (tools.READ, None)
          and "session.request" not in offer and "_change(" not in offer
          and "subject = _settings_card(folder)" in offer
          and "if not pinned.is_pinned:" in offer,
          "offering a settings table sends nothing to Revit, only for a settings card, "
          "and never before a model is pinned")
    tab = hc.Tables()
    tab.open("A", {"columns": ["Mark"], "rows": [{"id": 1, "uniqueId": "u",
               "cells": {"Mark": {"value": "x", "editable": True}}}]}, ("A", "", "11"))
    rows, why, identity = tab.snapshot(1, [{"id": 1, "name": "Mark", "value": "y"}])
    check(rows and identity == ["A", "", "11"],
          "a table's rows and its model come from one locked snapshot")


def test_model_guard():
    print()
    print("A table made in one model never applies in another (Codex review of #362)")
    server = io.open(SERVER, encoding="utf-8").read()
    for name in ("def _apply_change(", "def _apply_table("):
        body = server[server.index(name):]
        body = body[:body.index(chr(10) + "def ", 10)]
        check("moved = _moved_since(identity)" in body
              and body.index("moved = _moved_since(identity)") < body.index("_through(revit_change"),
              "%s refuses when the chat has moved to another model, BEFORE anything is sent"
              % name[4:-1])
    check("CHANGES.offer(capability, document, pairs, _pin_identity(), subject)" in server
          and 'str(binding.pid or "")' in server
          and "TABLES.open(document, table, _pin_identity())" in server,
          "every table and card records the model it was made in - and which Revit holds it")
    page = io.open(os.path.join(ROOT, "mcp", "companion", "heron_companion.py"),
                   encoding="utf-8").read()
    check("with self._life:" in page and page.count("with self._life:") == 2,
          "starting and stopping the page are one step each, never two servers at once")
    check("if len(line) > 700000:" in server,
          "a change set too big for the bridge's one line is refused whole, never split")
    cs = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronCompanionCommands.cs"),
                 encoding="utf-8").read()
    check("AddSeconds(-NoteSeconds)" in cs and "HeronConfig.Load().GetBool(HeronLiveState.EnabledKey" in cs,
          "the button ignores a stale note, and the switch reads the SHARED setting before flipping")
    live = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronLiveState.cs"),
                   encoding="utf-8").read()
    check('Json.ReadString(text, "started")' in cs and 'view.Id + "|" + view.Name' in live,
          "the button picks the newest chat by when it started; a renamed view refreshes the page")


def main():
    tmp = tempfile.mkdtemp(prefix="heron-companion-")
    test_live_files(tmp)
    test_server(tmp)
    test_switch(tmp)
    test_activity()
    test_changes()
    test_tables()
    test_model_guard()
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
