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

    static = os.path.join(ROOT, "mcp", "companion", "static")
    for name in sorted(n for n in os.listdir(static) if n.endswith(".js")):
        js = io.open(os.path.join(static, name), encoding="utf-8").read()
        check("innerHTML" not in js and "insertAdjacentHTML" not in js
              and "outerHTML" not in js and "document.write" not in js,
              "%s puts model text on screen as text, never as HTML" % name)
        check(not re.search(r"https?://", js.replace("http://127.0.0.1", "")),
              "%s names no address outside this PC" % name)


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
    changes.apply_hook = lambda capability, pairs, identity=None, still=None: (sent.append((capability, pairs))
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
    check(result["applied"] and sent == [[("101", "u-101", "Mark", "FCU-01", "FCU-1A", "")]],
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
    writer_cs = io.open(os.path.join(folder, "set-parameter-values-by-id", "impl", "any",
                                     "fragment.cs"), encoding="utf-8").read()
    check("VariesAcrossGroups" in reader and "groupLocked(e, p)" in reader
          and "groupLocked(element, target)" in writer_cs,
          "a model-group member that would change in every copy is never editable, "
          "and the writer refuses it too")
    check("storedValue(target) != wasStored" in writer_cs and 'esc("raw")' in reader,
          "numbers are checked by the value Revit stores, not only the rounded display")
    check("BuiltInParameter.ALL_MODEL_MARK" in reader and "cell.noCopy" in io.open(
        os.path.join(ROOT, "mcp", "companion", "static", "companion.js"), encoding="utf-8").read(),
          "Mark is recognised by what it is, in any Revit language, and never copied")
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
    rows, why, identity = tab.rows_and_model(1, [{"id": 1, "name": "Mark", "value": "y"}])
    check(rows and identity == ["A", "", "11"],
          "a table's rows and its model come from one locked snapshot")
    tab.apply_hook = lambda rows, identity: ("1 row changed in Revit. Nothing was sent.",
                                             {"applied": False, "stale": [
                                                 {"id": 1, "name": "Mark", "why": "w", "now": "z"}]})
    tab.apply(1, [{"id": 1, "name": "Mark", "value": "y"}])
    check(tab.current()["rows"][0]["cells"]["Mark"]["value"] == "z",
          "a cell changed in Revit becomes the table's new baseline, so Apply again can pass")
    check("binding.resolve()" in server[server.index("def _moved_since("):
                                       server.index("def _offer_change(")],
          "the model guard asks which Revit is live NOW, never the cached one")
    check("if _from_chat() and _took(reply):" in server,
          "a change Revit did not take leaves no table")
    check('"REVIT DID NOT KEEP" in str(provides)' in server and "_PARTIAL" in server,
          "nor does a change Revit kept only part of")
    check("if still is not None and not still():" in server
          and 'if v["name"] in taken]' in server,
          "a card the chat replaced while Apply waited is refused; a typo never becomes a row")
    check("if pinned.check(reply):" in server and "missing = [" in server,
          "a table read from another model is never opened; a card lacking an input is never offered")


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
    check("with self._life:" in page and page.count("with self._life:") >= 2,
          "starting and stopping the page are one step each, never two servers at once")
    check("if len(line) > 700000:" in server,
          "a change set too big for the bridge's one line is refused whole, never split")
    cs = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronCompanionCommands.cs"),
                 encoding="utf-8").read()
    check("AddSeconds(-NoteSeconds)" in cs and "HeronConfig.Load().GetBool(HeronLiveState.EnabledKey" in cs,
          "the button ignores a stale note, and the switch reads the SHARED setting before flipping")
    live = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronLiveState.cs"),
                   encoding="utf-8").read()
    check('Json.ReadString(text, "started")' in cs and 'view.Id + "|" + view.Name' in live
          and "AddYears(-100)" in cs and "_lastSeen = null;" in live,
          "the button picks the newest chat by when it started; a renamed view refreshes the page")


def test_load_from_revit():
    """The page's Load buttons: what REPORT_VIEW_FILTERS and
    REPORT_CATEGORY_OVERRIDES said about a real view (heron ai bulding,
    {3D}, 2026-09-30) becomes the values APPLY_VIEW_FILTER and
    SET_CATEGORY_GRAPHICS take - and Clear forgets every table."""
    print("load from revit")
    said = ("View '{3D}' (ThreeD): 1 filter(s)." + chr(10) +
            "  'Supply Air' - Enable Filter ON; Visibility ON; Projection/Surface Lines: colour "
            "0,0,255; Surface Patterns: foreground <Solid fill>, colour 0,0,255 / background no "
            "override; Transparency 0; Cut Lines: colour 0,0,255; Cut Patterns: foreground colour "
            "0,0,255 / background no override; Halftone off; cut not applicable - none of Ducts "
            "can be cut, so the Cut columns are greyed out.")
    cards = hc.filter_cards("{3D}", said)
    check(len(cards) == 1 and cards[0][0] == "APPLY_VIEW_FILTER", "one filter, one table")
    values = dict(cards[0][1])
    check(values["filter"] == "Supply Air" and values["enabled"] == "true"
          and values["visible"] == "true", "the filter, its Enable and Visibility ticks")
    check("surface-foreground-colour=0,0,255" in values["overrides"]
          and "surface-foreground-pattern=solid" in values["overrides"], "solid fill and colour")
    check("cut-" not in values["overrides"], "cut values left out where Revit greys the Cut columns")
    check(values["solidFill"] == "" and values["keepOtherSettings"] == "true",
          "solidFill blank - 'true' made every Apply refuse (2026-09-30) - and other settings kept")

    listed = ("Walls: line RGB(255,179,186), cut line RGB(255,179,186), surface RGB(255,179,186), "
              "surface pattern <Solid fill>  ||  Ceilings: line RGB(0,0,255), 30% transparent")
    cards = hc.category_cards("{3D}", listed)
    check([dict(c[1])["categories"] for c in cards] == ["Walls", "Ceilings"], "one table per category")
    walls = dict(cards[0][1])["overrides"]
    check("cut-line-colour=255,179,186" in walls and "projection-line-colour=255,179,186" in walls,
          "cut line never read as line")
    check("transparency=30" in dict(cards[1][1])["overrides"], "transparency")
    check(hc.filter_cards("{3D}", "") == [] and hc.category_cards("{3D}", "") == [],
          "an empty answer makes no table")

    changes = hc.Changes()
    changes.offer("APPLY_VIEW_FILTER", "m", [("view", "{3D}")])
    changes.clear()
    check(changes.cards() == [], "Clear tables forgets every table")
    check(not changes.load("filters")["ok"], "Load refuses before the chat has set its reader")
    changes.load_hook = lambda kind: ("read", [])
    check(not changes.load("walls")["ok"] and changes.load("filters")["ok"],
          "only the two known kinds load")


def test_loads():
    """The Loads panel (docs/44 section 7): it holds what the brain worked out
    and calls hooks - no arithmetic, no BIM rule - and its routes are behind
    the same header, cookie and Origin as the table's Apply."""
    print()
    print("The Loads panel: data and hooks only, and its writes behind the pairing")
    sys.path.insert(0, os.path.join(ROOT, "brain"))
    sys.path.insert(0, os.path.join(ROOT, "tests"))
    import heron_brain as brain_seam
    from test_takeoff import ROOM
    from test_building_loads import PROJECT, OFFICE
    answer = brain_seam.building_loads(json.dumps(ROOM), {"project": PROJECT,
                                                          "profiles": {"Office": OFFICE}},
                                       save=False)
    panel = hc.LoadsPanel()
    check(panel.current() is None and panel.recalculate({})["ok"] is False
          and panel.finalize()["ok"] is False and panel.report()["ok"] is False,
          "an empty panel, with no chat connected, does nothing")
    panel.open("Project1", answer, ("Project1", "", "11"))
    shown = panel.current()
    check(shown["document"] == "Project1" and shown["spaces"][0]["status"] == "ok"
          and shown["building"]["block_w"] == answer["result"]["building"]["block_w"]
          and "takeoff" not in shown and "result" not in shown,
          "the panel returns what it was given - the take-off and hour rows stay off the page")
    before = json.dumps(panel.current(), sort_keys=True)
    said = panel.recalculate({"project": PROJECT, "profiles": {"Office": OFFICE}})
    check(said["ok"] is False and "no longer connected" in said["said"]
          and json.dumps(panel.current(), sort_keys=True) == before,
          "Recalculate with no hook answers 'no longer connected' and changes nothing")
    seen = []

    def recalc(takeoff, inputs, identity):
        seen.append((takeoff, inputs, identity))
        return brain_seam.building_loads(takeoff, inputs, save=False)
    panel.recalculate_hook = recalc
    office = dict(OFFICE, equipment_w_per_m2=40)
    after = panel.recalculate({"project": PROJECT, "profiles": {"Office": office}})
    check(after["ok"] and seen and seen[0][0] is answer["takeoff"]
          and seen[0][2] == ["Project1", "", "11"]
          and after["loads"]["building"]["block_w"] > answer["result"]["building"]["block_w"],
          "Recalculate runs the brain on the take-off ALREADY HELD, in the model it came from")
    check(panel.recalculate({"project": "not a dict"})["ok"] is False,
          "inputs that are not an object are refused at the door")
    check(panel.earlier("../x") is None, "an earlier run's id must be a run id")

    source = io.open(SOURCE, encoding="utf-8").read()
    body = source[source.index("class LoadsPanel("):source.index("LOADS_PANEL = LoadsPanel()")]
    check(not re.search(r"[0-9]\s*[*/]\s*[a-z_(]|W_PER_TR|3\.6", body)
          and "import heron_" not in body,
          "the class does no arithmetic and imports no brain module (README rule 4)")
    for route in ("/api/loads/recalculate", "/api/loads/report", "/api/loads/finalize"):
        check(route in source, "the page can ask for %s" % route)
    i = source.index('if path in ("/api/loads/recalculate"')
    check(source.index("why = self._api_ok()", i) < source.index("LOADS_PANEL.recalculate", i),
          "every Loads POST is behind _api_ok - header, Origin and the paired cookie")

    companion = hc.Companion(bound_pid=lambda: 100, folder=tempfile.mkdtemp(),
                             discovery_dir=tempfile.mkdtemp(), is_alive=lambda pid: True)
    real_stdout, sys.stdout = sys.stdout, io.StringIO()
    try:
        companion.pairing_address()
        port = companion.port
        origin = "http://127.0.0.1:%d" % port
        refused = ask(port, "POST", "/api/loads/finalize",
                      {"X-Heron-Companion": "1", "Origin": origin,
                       "Content-Type": "application/json"}, "{}")
        unpaired_get = ask(port, "GET", "/api/loads", {"X-Heron-Companion": "1"})
        page = ask(port, "GET", "/")
    finally:
        printed, sys.stdout = sys.stdout.getvalue(), real_stdout
        companion.stop()
    check(refused[0] == 403 and unpaired_get[0] == 403,
          "Finalize and the panel's data are refused without a redeemed pairing")
    check(b'id="loads"' in page[2], "the page has the Loads section")
    check(printed == "", "nothing reached stdout")

    # The 3D view (docs/44 s12): its data is made in brain/ and served on its
    # own route; the page file is served from the whitelist; the take-off is
    # confirmed through a hook, behind the same pairing.
    view_panel = hc.LoadsPanel()
    check(view_panel.view() is None and view_panel.confirm()["ok"] is False,
          "an empty panel has no 3D view and confirms nothing")
    from test_loads_view import l_building
    from test_loads_view import GROUNDED
    shaped = brain_seam.building_loads(json.dumps(l_building()), {
        "project": GROUNDED, "profiles": {"Office": OFFICE}}, save=False)
    view_panel.open("Project1", shaped, ("Project1", "", "11"))
    drawn = view_panel.view()
    check(drawn and drawn["faces"] and "view" not in view_panel.current()
          and view_panel.current()["has_view"] is True,
          "the 3D view's data is served on its own route, not inside every poll")
    check(view_panel.confirm()["ok"] is False and view_panel.current()["confirmed"] is False,
          "with no chat connected the take-off cannot be confirmed")
    view_panel.confirm_hook = lambda t, r, identity: brain_seam.loads_confirm(t, r)
    said = view_panel.confirm()
    check(said["ok"] and view_panel.current()["confirmed"] is True,
          "'The take-off is right' records the check through the brain")
    check(hc.PAGES.get("/loads3d.js", (None,))[0] == "loads3d.js"
          and set(hc.PAGES) == {"/", "/companion.js", "/companion.css", "/loads3d.js",
                                "/sprinkler3d.js"},
          "the page files are a fixed list - the two 3D views' scripts are on it, nothing "
          "else (the Sprinkler panel's added 2026-10-04, docs/46)")
    for route in ("/api/loads/view", "/api/loads/confirm"):
        check(route in source, "the page can ask for %s" % route)
    i = source.index('if path == "/api/loads/view":')
    check(source.index("why = self._api_ok()", i) < source.index("LOADS_PANEL.view()", i),
          "the 3D view's data is behind _api_ok like every other route")
    page_js = io.open(os.path.join(ROOT, "mcp", "companion", "static", "loads3d.js"),
                      encoding="utf-8").read()
    check("fetch(\"/api/loads/view\"" in page_js and "import " not in page_js,
          "the 3D view fetches only its own data and loads no library")

    server = io.open(SERVER, encoding="utf-8").read()
    fin = server[server.index("def _loads_finalize("):]
    fin = fin[:fin.index(chr(10) + "def ", 10)]
    check("_apply_table(rows, identity)" in fin
          and fin.index("_apply_table(rows, identity)") < fin.index('"SET_AIR_TERMINAL_FLOW"')
          and "moved = _moved_since(identity)" in fin,
          "Finalize writes the Spaces through the table's own Apply first, then the "
          "diffusers, each refused when the chat has moved to another model")
    check(tools.COMPANION_ACTIONS.get("companion_loads_finalize") == (tools.MODIFY,
                                                                      "run_fragment_write"),
          "Finalize is declared MODIFY through run_fragment_write")
    check("LOADS.finalize_rows(takeoff, result)" in fin,
          "Finalize builds its rows through the brain, which refuses a take-off nobody confirmed")
    # Review I9 and I10: Finalize reads the model again and refuses a changed
    # one; it counts the undo entries it actually made.
    reread = fin.find('"REPORT_SPACE_ENVELOPE"')
    check(reread != -1 and reread < fin.index("_apply_table(rows, identity)")
          and "LOADS.fingerprint(fresh) != LOADS.fingerprint(takeoff)" in fin,
          "Finalize reads the model again and refuses if its geometry changed")
    check("Two undo entries in Revit:" not in fin and "entries += 1" in fin,
          "Finalize says how many undo entries it made, not always two")
    # The second review, m1: the rows carry the values Heron SHOWED, so an edit
    # made in Revit since the read refuses the whole write; m3: confirming
    # checks the chat is still on the model the take-off came from.
    check("LOADS.finalize_rows(fresh, result)" not in fin
          and fin.count("LOADS.finalize_rows(takeoff, result)") == 2
          and 'setdefault("current", {})[field] = new' in fin,
          "Finalize checks each row against the value Heron showed, and then holds what it wrote")
    conf = server[server.index("def _loads_confirm("):]
    conf = conf[:conf.index(chr(10) + "def ", 10)]
    check("moved = _moved_since(identity)" in conf
          and conf.index("moved = _moved_since(identity)") < conf.index("brain.loads_confirm("),
          "confirming the take-off is refused when the chat has moved to another model")
    kept = hc.LoadsPanel()
    kept.open("Project1", shaped, ("Project1", "", "11"), read_at="08:00:00")
    kept.recalculate_hook = lambda t, i, ident: brain_seam.building_loads(t, i, save=False)
    kept.recalculate({"project": GROUNDED, "profiles": {"Office": OFFICE}})
    check(kept.current()["read_at"] == "08:00:00",
          "Recalculate keeps the time the MODEL was read - it reads nothing itself")
    for hook in ("recalculate_hook = _loads_recalculate", "report_hook = _loads_report",
                 "finalize_hook = _loads_finalize", "confirm_hook = _loads_confirm"):
        check(hook in server, "the server sets %s" % hook.split(" =")[0])


def test_loads_report_page():
    """The page after Ajmal's first look (2026-10-04): Report asks where the
    sheet goes, the sheet opens in the browser through a key only the paired
    page holds, the inputs sit above the results, the title names both loads,
    and the 3D view turns the way the mouse moves, as Revit's does."""
    print()
    print("The Loads report: asked where, opened in the browser")
    sys.path.insert(0, os.path.join(ROOT, "brain"))
    sys.path.insert(0, os.path.join(ROOT, "tests"))
    import subprocess
    import heron_brain as brain_seam
    from test_takeoff import ROOM
    from test_building_loads import PROJECT, OFFICE
    answer = brain_seam.building_loads(json.dumps(ROOM), {"project": PROJECT,
                                                          "profiles": {"Office": OFFICE}},
                                       save=False)
    panel = hc.LoadsPanel()
    panel.open("Project1", answer, ("Project1", "", "11"))
    chosen = tempfile.mkdtemp(prefix="heron-report-")
    asked, wrote = [], []
    panel.report_hook = lambda t, r, folder: (wrote.append(folder)
                                              or brain_seam.loads_report(t, r, folder=folder))
    panel.folder_hook = lambda start: asked.append(start) or {"cancelled": True}
    said = panel.report({"ask": True})
    check(said["ok"] is False and asked and not wrote and panel.current()["report"] is None,
          "Report asks where to save the sheet first, and Cancel writes nothing")
    panel.folder_hook = lambda start: {"folder": chosen}
    said = panel.report({"ask": True})
    held = panel.current()["report"]
    check(said["ok"] and wrote == [chosen] and os.path.dirname(said["html"] or "") == chosen,
          "the sheet is written into the folder chosen")
    check(held["folder"] == chosen and len(held.get("token") or "") >= 20
          and panel.current()["report_folder"] == chosen,
          "the page is told where it went, holds a key to open it, and offers that folder next time")
    check(panel.report_file("x" * 32, "html") is None
          and panel.report_file(held["token"], "csv") is None
          and panel.report_file(held["token"], "../html") is None,
          "a wrong key, or a kind of file it does not open, opens nothing")
    got = panel.report_file(held["token"], "html")
    check(got is not None and got[0] == said["html"] and got[1].startswith("text/html"),
          "the key opens the sheet that report wrote")
    panel.folder_hook = lambda start: {"unavailable": "no window here"}
    wrote[:] = []
    from test_building_loads import knowledge_folder
    with knowledge_folder():
        # The usual place is the project's own folder - a scratch one here.
        by_default = panel.report_hook
        panel.report_hook = lambda t, r, folder: (wrote.append(folder) or brain_seam.loads_report(
            t, r, folder=folder, project="project-t"))
        later = panel.report({"ask": True})
        panel.report_hook = by_default
    check(later["ok"] and wrote == [None] and "could not open" in later["said"]
          and panel.report_file(held["token"], "html") is None,
          "with no folder window the sheet goes to the usual place, says so, and the old key is gone")

    # The folder window: a Windows dialog in a process of its own, which
    # prints nothing where MCP speaks and takes the start folder as data.
    calls = []

    def fake_run(command, **kw):
        calls.append((command, kw))

        class Done(object):
            returncode = 0
            stdout = (chosen + "\r\n").encode("utf-8")
        return Done()
    picked = hc.pick_folder("C:\\x'; Remove-Item C:\\", run=fake_run)
    if os.name != "nt":
        check("unavailable" in picked and not calls,
              "off Windows there is no folder window, and nothing is started")
    else:
        command, kw = calls[0]
        check(picked == {"folder": chosen}, "the folder window answers with the folder chosen")
        check(kw.get("stdout") == subprocess.PIPE and kw.get("stdin") == subprocess.DEVNULL
              and kw.get("stderr") == subprocess.DEVNULL,
              "its process prints nothing where MCP speaks")
        check("Remove-Item" not in " ".join(command)
              and kw["env"].get("HERON_FOLDER_START") == "C:\\x'; Remove-Item C:\\",
              "the start folder goes in as data, never as code")

        def cancelled(command, **kw):
            class Done(object):
                returncode = 0
                stdout = b""
            return Done()

        def broken(command, **kw):
            raise OSError("no powershell")
        check(hc.pick_folder(None, run=cancelled) == {"cancelled": True},
              "Cancel in the window is a cancel")
        check("unavailable" in hc.pick_folder(None, run=broken),
              "a window that cannot open is said, never a crash")

    # The route that opens the sheet: the paired page only, the right key only,
    # and a page that may run no script.
    panel.folder_hook = lambda start: {"folder": chosen}
    said = panel.report({"ask": True})
    token = panel.current()["report"]["token"]
    saved_panel = hc.LOADS_PANEL
    hc.LOADS_PANEL = panel
    companion = hc.Companion(bound_pid=lambda: 100, folder=tempfile.mkdtemp(),
                             discovery_dir=tempfile.mkdtemp(), is_alive=lambda pid: True)
    real_stdout, sys.stdout = sys.stdout, io.StringIO()
    try:
        address = companion.pairing_address()
        port = companion.port
        origin = "http://127.0.0.1:%d" % port
        code = address.split("?pair=", 1)[1]
        paired = ask(port, "POST", "/api/pair", {"X-Heron-Companion": "1", "Origin": origin,
                                                 "Content-Type": "application/json"},
                     json.dumps({"code": code}))
        cookie = paired[1].get("set-cookie", "").split(";", 1)[0]
        no_cookie = ask(port, "GET", "/report/%s/html" % token)
        good = ask(port, "GET", "/report/%s/html" % token, {"Cookie": cookie})
        wrong = ask(port, "GET", "/report/%s/html" % ("x" * 32), {"Cookie": cookie})
        evil = ask(port, "GET", "/report/%s/html" % token, {"Cookie": cookie},
                   host="evil.example:%d" % port)
        pdf = ask(port, "GET", "/report/%s/pdf" % token, {"Cookie": cookie})
    finally:
        printed, sys.stdout = sys.stdout.getvalue(), real_stdout
        companion.stop()
        hc.LOADS_PANEL = saved_panel
    csp = good[1].get("content-security-policy", "")
    check(no_cookie[0] == 403 and wrong[0] == 404 and evil[0] == 421,
          "the sheet is refused without the pairing, with a wrong key, or for another Host")
    check(good[0] == 200 and good[1].get("content-type", "").startswith("text/html")
          and b"HVAC Load Calculation" in good[2],
          "the paired page opens the sheet in the browser")
    check("default-src 'none'" in csp and "script-src" not in csp
          and "frame-ancestors 'none'" in csp,
          "and the sheet may run no script and sit in no frame")
    check((pdf[0] == 200 and pdf[1].get("content-type") == "application/pdf"
           and pdf[2][:4] == b"%PDF") if said.get("pdf") else pdf[0] == 404,
          "the PDF opens in the browser too - or is 'not found' when no browser could print it")
    check(printed == "", "nothing reached stdout")

    # The page: Report asks, the sheet opens in a new tab, one column, both
    # loads named, and the drag follows the mouse.
    static = os.path.join(ROOT, "mcp", "companion", "static")
    js = io.open(os.path.join(static, "companion.js"), encoding="utf-8").read()
    css = io.open(os.path.join(static, "companion.css"), encoding="utf-8").read()
    index = io.open(os.path.join(static, "index.html"), encoding="utf-8").read()
    view = io.open(os.path.join(static, "loads3d.js"), encoding="utf-8").read()
    check('loadsPost("/api/loads/report", { ask: true }' in js,
          "the Report button asks where the sheet goes")
    check('"/report/" + encodeURIComponent(' in js and 'rel = "noopener"' in js
          and 'target = "_blank"' in js,
          "and the page offers to open the sheet and the PDF in a new tab")
    check("Cooling (AC) load" in js and "Heating load" in js,
          "the results say which columns are the cooling (AC) load and which the heating load")
    check("HVAC Load Calculation" in index and "Cooling (AC) and heating load per Space" in index
          and "not an hourly simulation like HAP" in index,
          "the panel is named for what it is - an HVAC load calculation, both loads, not HAP")
    check("max-width: 38%" not in css and "#loads .l-inputs, #loads .l-results { display: block;"
          in css, "the inputs sit above the results, each the page's full width")
    check("cam.yaw -= dx" in view and "cam.yaw += dx" not in view,
          "dragging turns the building the way the mouse moves, as Revit's orbit does")


def test_finalize_asks_for_nothing_by_typed_ids():
    """The first real Finalize (Project2, Revit 2024, 2026-10-05) wrote the three
    Spaces right - read back value by value - but its read-back and its diffuser
    step chained from FILTER_ELEMENTS_BY_ID with typed ids, which the add-in
    never accepts ("elementIds ... cannot be typed"): the read-back was lost, and
    on a model with diffusers their flows would have been. Ajmal asked the same
    day for the Spaces schedule to be made by Finalize too."""
    print()
    print("Finalize on a real add-in: no typed ids, a real read-back, the schedule")
    server = io.open(SERVER, encoding="utf-8").read()
    fin = server[server.index("def _loads_finalize("):]
    fin = fin[:fin.index(chr(10) + "def ", 10)]
    check("elementIds=" not in fin and "FILTER_ELEMENTS_BY_ID" not in fin,
          "Finalize asks the add-in for no element by a typed id")
    at = fin.find('"SET_AIR_TERMINAL_FLOW"')
    check(at != -1 and '"FILTER_ELEMENTS_BY_CATEGORY"' in fin
          and -1 < fin.find("category=Air Terminals") < at
          and "filter-elements-by-category where category=Air Terminals" in fin,
          "the diffusers are found by category on each calculated Space's level, and the "
          "file of ids picks which are written")
    applied = fin.find("_apply_table(rows, identity)")
    check(applied != -1 and fin.find('"REPORT_SPACE_ENVELOPE"', applied) != -1
          and "LOADS.read_back(rows, " in fin,
          "the read-back reads the take-off again after the write and compares it value by value")
    check('"CREATE_SCHEDULE"' in fin and "LOADS.SCHEDULE_NAME" in fin
          and "LOADS.SCHEDULE_FIELDS" in fin,
          "Finalize makes the Spaces schedule of what it wrote, once")


def test_switched_off_parts():
    """The owner's word (Ajmal PS, 2026-10-04): the Selected card and the
    Changes tables, with the colour books that paint them, come off the page,
    and nothing keeps working for them in the background - but their code is
    KEPT, switched off, to be brought back later (docs/40 s21.6)."""
    print()
    print("Switched off and kept: what is selected, and the Changes tables")
    static = os.path.join(ROOT, "mcp", "companion", "static")
    index = io.open(os.path.join(static, "index.html"), encoding="utf-8").read()
    js = io.open(os.path.join(static, "companion.js"), encoding="utf-8").read()
    shown = re.sub(r"(?s)<template\b.*?</template>", "", index)
    kept = "".join(re.findall(r"(?s)<template\b.*?</template>", index))
    for name in ("selection", "changes", "paint"):
        check('id="%s"' % name not in shown and 'id="%s"' % name in kept,
              "the '%s' section is off the page, and kept in a template that shows nothing" % name)
    check("const SHOW_SELECTION = false;" in js and "const SHOW_CHANGES = false;" in js,
          "the page's two switches are off")
    check("function showSelection(" in js and "function paint(" in js
          and "async function changes(" in js
          and "if (SHOW_SELECTION) showSelection(r);" in js
          and "if (SHOW_CHANGES) changes();" in js and "if (SHOW_CHANGES) paint();" in js,
          "their code is kept, and nothing calls it while the switches are off")
    check(hc.SHOW_CHANGES is False and callable(hc.Changes().offer),
          "the server's switch is off, and its Changes tables are still whole")
    server = io.open(SERVER, encoding="utf-8").read()
    for name in ("def _offer_change(", "def _load_settings(", "def revit_offer_settings("):
        body = server[server.index(name):]
        body = body[:body.index(chr(10) + "def ", 10) if chr(10) + "def " in body[10:] else len(body)]
        gate = body.find("SHOW_CHANGES")
        check(gate != -1 and gate < body.find("CHANGES.offer") if "CHANGES.offer" in body
              else gate != -1 and gate < body.find("LOADS[kind]"),
              "%s stops at the switch before it makes any table" % name.split("(")[0][4:])
    addin = io.open(os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronLiveState.cs"),
                    encoding="utf-8").read()
    reads = [m.start() for m in re.finditer(r"Selection\.GetElementIds\(\)", addin)]
    guards = [m.start() for m in re.finditer(r"if \(WatchSelection\)", addin)]
    check("private static readonly bool WatchSelection = false;" in addin
          and len(reads) == 2 and len(guards) == 2
          and all(any(g < r for g in guards) for r in reads),
          "the add-in reads no selection while WatchSelection is off - the code is kept")
    check('"\\"selection\\": null"' in addin and "WatchSelection ? 200 : 1000" in addin,
          "its live file says 'selection: null', and Idling looks once a second, not five times")


def test_sprinkler_panel():
    """The Sprinkler panel (docs/46): data and hooks only, every route behind the
    pairing, its own 3D view, and nothing on it writes to Revit."""
    print()
    print("The Sprinkler panel: data and hooks only, and it writes nothing to Revit")
    sys.path.insert(0, os.path.join(ROOT, "brain"))
    sys.path.insert(0, os.path.join(ROOT, "tests"))
    import heron_brain as brain_seam
    import test_sprinkler_run as RUNS
    network = json.dumps(RUNS.NET.net())
    panel = hc.SprinklerPanel()
    check(panel.calculate({})["said"] == hc.SprinklerPanel.GONE
          and panel.suggest({})["said"] == hc.SprinklerPanel.GONE
          and panel.confirm()["said"] == hc.SprinklerPanel.GONE
          and panel.report({})["said"] == hc.SprinklerPanel.GONE,
          "with no chat connected, every button says so and does nothing")
    answer = brain_seam.sprinkler_hydraulics(network, {}, save=False)
    panel.open("Project1", answer, ("Project1", "", "11"), read_at="08:00:00")
    shown = panel.current()
    check(shown["status"] == "missing" and shown["asked"] and shown["has_view"]
          and "view" not in shown and "result" not in shown,
          "the panel shows the questions; the 3D data is on its own route")
    check([f[0] for f in shown["fields"]][:2] == ["density_mm_min", "area_per_sprinkler_m2"],
          "the criteria the page asks for come from the brain, in its order")
    seen = {}

    def calc(net, inputs, identity):
        seen["inputs"] = inputs
        return brain_seam.sprinkler_hydraulics(net, inputs, save=False)
    panel.calculate_hook = calc
    body = RUNS.given(operating=["401", "402"])
    got = panel.calculate(body)
    check(got["ok"] and seen["inputs"]["operating"] == ["401", "402"]
          and got["sprinkler"]["status"] == "ok" and got["sprinkler"]["read_at"] == "08:00:00",
          "Calculate hands the page's inputs to the hook and keeps when the model was read")
    check(panel.calculate({"criteria": "not a map"})["ok"] is False,
          "inputs that are not a map are refused - nothing is calculated")
    panel.suggest_hook = brain_seam.sprinkler_suggest
    sug = panel.suggest(body)
    check(sug["ok"] and sorted(sug["heads"]) == ["401", "402"],
          "Suggest returns ticks for the page and solves nothing")
    panel.confirm_hook = lambda n, r, identity: brain_seam.sprinkler_confirm(n, r)
    check(panel.confirm()["ok"] and panel.current()["confirmed"] is True,
          "'The network is right' records the check through the brain")
    folder = tempfile.mkdtemp(prefix="heron-sprinkler-")
    panel.report_hook = lambda n, r, f: brain_seam.sprinkler_report(n, r, folder=folder)
    panel.folder_hook = lambda start, title=None: {"cancelled": True}
    check(panel.report({"ask": True})["ok"] is False and panel.current()["report"] is None,
          "Cancel in the folder window writes no report")
    panel.folder_hook = lambda start, title=None: {"folder": folder}
    wrote = panel.report({"ask": True})
    token = panel.current()["report"]["token"]
    check(wrote["ok"] and panel.report_file(token, "html")
          and panel.report_file(token, "html")[0].startswith(folder)
          and panel.report_file("wrong", "html") is None and panel.report_file(token, "csv") is None,
          "the sheet opens through the key this page was given, html or pdf only")
    source = io.open(os.path.join(ROOT, "mcp", "companion", "heron_companion.py"),
                     encoding="utf-8").read()
    for route in ("/api/sprinkler", "/api/sprinkler/view", "/api/sprinkler/calculate",
                  "/api/sprinkler/suggest", "/api/sprinkler/confirm", "/api/sprinkler/report"):
        check(route in source, "the page can ask for %s" % route)
    i = source.index('if path == "/api/sprinkler":')
    j = source.index('if path in ("/api/sprinkler/calculate"')
    check(source.index("why = self._api_ok()", i) < source.index("SPRINKLER_PANEL.current()", i)
          and source.index("why = self._api_ok()", j) < source.index("SPRINKLER_PANEL.calculate(", j),
          "every Sprinkler route is behind _api_ok like every other route")
    check("def finalize" not in source[source.index("class SprinklerPanel"):
                                       source.index("SPRINKLER_PANEL = SprinklerPanel()")]
          and "/api/sprinkler/finalize" not in source,
          "the Sprinkler panel has no Finalize - phase 1 writes nothing to Revit")
    page_js = io.open(os.path.join(ROOT, "mcp", "companion", "static", "sprinkler3d.js"),
                      encoding="utf-8").read()
    check('fetch("/api/sprinkler/view"' in page_js and "import " not in page_js,
          "the Sprinkler 3D view fetches only its own data and loads no library")
    server = io.open(SERVER, encoding="utf-8").read()
    for hook in ("calculate_hook = _sprinkler_calculate", "suggest_hook = _sprinkler_suggest",
                 "confirm_hook = _sprinkler_confirm", "report_hook = _sprinkler_report"):
        check(hook in server, "the server sets %s" % hook.split(" =")[0])
    calc_src = server[server.index("def _sprinkler_calculate("):]
    calc_src = calc_src[:calc_src.index(chr(10) + "def ", 10)]
    check("_moved_since(identity)" in calc_src and "revit_change" not in calc_src,
          "Calculate is refused when the chat moved models, and sends nothing to Revit")
    check(tools.TOOLS.get("revit_sprinkler_hydraulics") == (tools.ANALYZE, "run_fragment_read"),
          "the tool is declared at revit_read's level - it reads and changes nothing")


def main():
    tmp = tempfile.mkdtemp(prefix="heron-companion-")
    test_live_files(tmp)
    test_server(tmp)
    test_switch(tmp)
    test_activity()
    test_changes()
    test_tables()
    test_load_from_revit()
    test_model_guard()
    test_loads()
    try:
        test_loads_report_page()
    except Exception as why:                    # noqa: BLE001 - reported, not hidden
        check(False, "the Loads report checks ran to the end (they raised %r)" % (why,))
    try:
        test_switched_off_parts()
    except Exception as why:                    # noqa: BLE001 - reported, not hidden
        check(False, "the switched-off checks ran to the end (they raised %r)" % (why,))
    try:
        test_finalize_asks_for_nothing_by_typed_ids()
    except Exception as why:                    # noqa: BLE001 - reported, not hidden
        check(False, "the Finalize checks ran to the end (they raised %r)" % (why,))
    try:
        test_sprinkler_panel()
    except Exception as why:                    # noqa: BLE001 - reported, not hidden
        check(False, "the Sprinkler panel checks ran to the end (they raised %r)" % (why,))
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
