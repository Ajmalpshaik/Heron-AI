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
sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real
# trail (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant()

import heron_companion as hc                                 # noqa: E402
import heron_tools as tools                                  # noqa: E402

SOURCE = os.path.join(ROOT, "mcp", "companion", "heron_companion.py")
PAGE_JS = os.path.join(ROOT, "mcp", "companion", "static", "companion.js")
LIVE_CS = os.path.join(ROOT, "revit", "Heron.Revit.Addin", "HeronLiveState.cs")

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

    # D-147: the model's change count, read from the same file and nothing else.
    check(hc.live_changes(100, live, bridges) is None,
          "a live file from an add-in before D-147 carries no count - None, never 0")
    for bad in (True, -1, "7", 2.5):
        write(os.path.join(live, "pid-100.json"), dict(live_record(100), changes=bad))
        check(hc.live_changes(100, live, bridges) is None,
              "a count that is not a whole number of changes (%r) is not believed" % (bad,))
    write(os.path.join(live, "pid-100.json"), dict(live_record(100), changes=7))
    check(hc.live_changes(100, live, bridges) == 7 and hc.live_changes(None, live, bridges) is None
          and hc.live_changes(999, live, bridges) is None,
          "the count of the Revit asked about, and None for no Revit or another one")
    write(os.path.join(live, "pid-100.json"), live_record(100))

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

    server = tools.server_text()
    body = server[server.index("def heron_companion("):]
    # TO THE END OF THE FUNCTION, not to the next tool. No tool is defined
    # after this one in its file since row 5b-1555 - the ones that followed it
    # are in another file, earlier in the one text - so "the next
    # @server.tool()" stopped being a boundary and the cut ran on to the end
    # of the text. The first line back at the margin ends a top-level function.
    end = re.search(r"\n(?=\S)", body)
    body = body[:end.start()] if end else body
    returns = [l for l in body.splitlines() if l.strip().startswith("return")]
    check(returns and not any("address" in l for l in returns),
          "heron_companion never puts the one-time address in its reply to the chat")
    check("return binding.pid if binding.was_chosen else None" in server
          and "bound_pid=_companion_revit" in server
          and "bound_pid=lambda: binding.pid" not in server,
          "the page calls a Revit this chat's only when the modeller CHOSE it")
    check(tools.TOOLS.get("heron_companion") == (tools.READ, None),
          "heron_companion is declared READ with no Revit operation")
    _what_the_chat_is_told(server)

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
    # D-147: the model's changes are COUNTED on DocumentChanged - nothing read,
    # nothing written in the event - and the count rides Idling's fingerprint.
    changed = text[text.index("private static void OnDocumentChanged("):]
    changed = changed[:changed.index("private static string ChangeKey(")]
    check("DocumentChanged += OnDocumentChanged;" in text
          and "DocumentChanged -= OnDocumentChanged;" in text,
          "DocumentChanged is subscribed at startup and unsubscribed at shutdown (D-147 amends D-09)")
    check("GetElement" not in changed and "Write(" not in changed and "WriteAllText" not in changed
          and "GetAddedElementIds" not in changed and "GetModifiedElementIds" not in changed,
          "and the event only counts: no element is read and no file is written in it")
    fingerprint = text[text.index("private static string Fingerprint("):]
    fingerprint = fingerprint[:fingerprint.index("private static void Remember(")]
    check("ChangeCount(doc)" in fingerprint and 'Json.Num("changes", changes)' in text,
          "the count is in Idling's fingerprint, so the throttled check writes it to the live file")


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
    # HOW IT ENDED IS THE TOOL'S TO SAY (row 5b-274). The words are read only
    # when no tool said, and two kinds of reply read them backwards: a
    # words-path hand-back says "Nothing has been sent to Revit" and is not a
    # refusal; an add-in refusal can open with no word of refusing at all.
    handed = getattr(hc.Activity, "HANDED_BACK", None)
    check(handed is not None and handed in getattr(hc.Activity, "OUTCOMES", ()),
          "the list has a word of its own for a words-path hand-back")
    handed = handed or "handed_back"
    told = hc.Activity()
    told.record("revit_read", "10:01:00", 0.1, outcome=handed,
                reply="Nothing has been sent to Revit: the words \"x\" did not settle one "
                      "capability on their own.")
    told.record("revit_read", "10:01:01", 0.3, outcome="refused",
                reply="This request expects values left by 'find-views', but what is "
                      "carried was left by 'x'. NOTHING WAS BOUND and no fragment ran.")
    told.record("revit_read", "10:01:02", 0.2, outcome="maybe",
                reply="Revit 2024 did not answer.")
    told.record("revit_read", "10:01:03", 0.2, reply="Revit 2024 did not answer.")
    got = [i["outcome"] for i in told.since(0)]
    check(got[0] == handed,
          "a hand-back the tool names is shown as one, though its first line says "
          "nothing was sent")
    check(got[1] == "refused",
          "a refusal the tool names is shown as one, though its first line never says so")
    check(got[2] == "failed",
          "an outcome the page has no word for is not believed - the words decide "
          "(found %r)" % got[2])
    check(got[3] == "failed", "and an outcome nobody gave is still read from the words")
    page = io.open(PAGE_JS, encoding="utf-8").read()
    check(re.search(r'\b%s:\s*"handed back"' % handed, page) is not None,
          "and the page says 'handed back' for it")

    check(items[0]["summary"] == "Ducts: 24 in Level 1",
          "only the first line of an answer is kept")
    check(len(act.since(2)) == 1 and act.since(2)[0]["tool"] == "revit_views",
          "the page asks only for what is newer than it has")
    for n in range(hc.Activity.LIMIT + 20):
        act.record("t", "x", 0, reply="x" * 400)
    check(len(act.since(0)) == hc.Activity.LIMIT
          and len(act.since(0)[-1]["summary"]) <= hc.Activity.FIRST_LINE,
          "at most 500 kept, each line at most 200 characters")

    server = tools.server_text()
    check("register(*args, **options)(_recorded(fn))" in server
          and "@functools.wraps(fn)" in server,
          "every tool is recorded through one wrapper that keeps its signature")
    # READ AS TEXT SO CI CHECKS IT; tests/test_mcp_serves.py calls the doors
    # through a real SDK and reads the list back (row 5b-274).
    check("reply=reply, outcome=said)" in server,
          "and the wrapper passes on how the tool SAID it ended, not outcome=None")
    words = server[server.find("\ndef _by_words("):]
    words = words[:words.find("\ndef ", 1)]
    check(words.count('_ended("handed_back"') == 2,
          "the words path calls what it hands back 'handed_back', not a refusal")
    check(server.count("_ended(_failure_ended(failure), explain(failure))") == 4,
          "and revit_read, revit_show, revit_change and revit_apply_move label a "
          "failure from the add-in's reply, not from the words that explain it")
    _single_purpose_endings(server)


def _what_the_chat_is_told(server):
    """
    heron_companion SAYS WHAT THE PAGE IS TODAY (FRAGMENT-ISSUES 5b-1133). Its
    reply and its description said the page shows "the selection" and "changes
    nothing": the Selected card is off (SHOW_SELECTION in companion.js), and
    Apply, Finalize and Place change the model through revit_change's own
    body. The chat repeats the reply, so the modeller was told a page that can
    change the model is read-only. Read with ast, as text: CI cannot import
    the server.
    """
    import ast
    tool = None
    for node in ast.walk(ast.parse(server)):
        if isinstance(node, ast.FunctionDef) and node.name == "heron_companion":
            tool = node
    check(tool is not None, "heron_companion can be read out of the server")
    if tool is None:
        return
    described = " ".join((ast.get_docstring(tool) or "").split())
    replies = [" ".join(node.value.value.split()) for node in ast.walk(tool)
               if isinstance(node, ast.Return) and isinstance(node.value, ast.Constant)
               and isinstance(node.value.value, str)]
    opened = [said for said in replies if "is open in your browser" in said]
    check(len(opened) == 1, "it has one reply for a page it opened")
    opened = opened[0] if opened else ""

    js = io.open(os.path.join(ROOT, "mcp", "companion", "static", "companion.js"),
                 encoding="utf-8").read()
    check("const SHOW_SELECTION = false;" in js,
          "the Selected card is switched off on the page (docs/40 section 21.6)")
    check("selection" not in opened.lower() and "selected" not in described.lower(),
          "so neither the reply nor the description says the page shows the selection: "
          "%s" % opened[:90])
    writes = [name for name, (level, _op) in tools.COMPANION_ACTIONS.items()
              if level >= tools.MODIFY]
    check(all(name in writes for name in
              ("companion_table_apply", "companion_loads_finalize",
               "companion_sprinkler_place")),
          "the page's Apply, Finalize and Place are declared MODIFY in heron_tools")
    for where, said in (("reply", opened), ("description", described)):
        check(all(word in said for word in ("Apply", "Finalize", "Place"))
              and "change the model" in said,
              "the %s says the page's Apply, Finalize and Place change the model" % where)
        check("Changes switch" in said and "Keep or Discard" in said,
              "and that they do it under the same Changes switch and Keep or Discard "
              "question as a change from the chat (%s)" % where)
        check(not said.rstrip().endswith("changes nothing.")
              and "It reads nothing from Revit through the pipe and changes nothing"
              not in said,
              "and the %s no longer says, of the page, that it changes nothing" % where)
    check("Opening it changed nothing" in opened and "OPENING it reads nothing" in described,
          "what changes nothing is OPENING the page, and both say so of that alone")


def _single_purpose_endings(server):
    """
    THE SINGLE-PURPOSE revit_* TOOLS SAY HOW THEY ENDED TOO (row 5b-389), read
    as text because CI cannot run tests/test_mcp_serves.py, which calls
    revit_levels, revit_select_by_category and revit_edit_table through a
    real SDK. Before it they said nothing, and the list read their first
    line: NotBound, with several Revits connected, opens "2 Revit sessions
    are connected, so it is not safe to guess which one you mean:", so a
    refusal was labelled ok.
    """
    # revit_undo_last IS READ WITH THE DOORS SINCE 2026-11-12 (row 5b-1610): it
    # says an outcome word, so it classifies its own reply as they do
    # (`analyse`, `_failure_ended`) and no longer through `_ended_by_reply`;
    # with no pin it refuses before it sends; and an Undo sent in another
    # model is not a refusal - it was SENT. The forms below are the read
    # tools'. Its every ending is held by tests/test_outcome_line.py section 9
    # and tests/test_mcp_serves.py (exercise_undo).
    doors = ("revit_read", "revit_show", "revit_change", "revit_apply_move",
             "revit_undo_last")
    bodies = {}
    for found in re.finditer(r"\n@server\.tool\(\)\ndef (revit_\w+)\(", server):
        rest = server[found.end():]
        end = re.search(r"\n(?=\S)", rest)
        bodies[found.group(1)] = rest[:end.start()] if end else rest
    single = sorted(name for name in bodies if name not in doors)
    silent = [name for name in single
              if "_ended(" not in bodies[name] and "_ended_by_reply(" not in bodies[name]]
    check(single and "revit_levels" in single and not silent,
          "every single-purpose revit_* tool (%d read from the server) says how it ended "
          "- silent: %s" % (len(single), silent))
    bare = [name for name in single
            if re.search(r"return str\((?:unbound|bad)\)", bodies[name])]
    check(not bare, "a refusal before anything is sent - several Revits and none chosen, a "
          "distance it cannot read - is 'refused': still bare in %s" % bare)
    pin = [name for name in single if re.search(r"return wrong_model\b", bodies[name])]
    check(not pin, "the pin's refusal is 'refused': still bare in %s" % pin)
    unread = [name for name in single if "session.request(" in bodies[name]
              and re.findall(r'_ended_by_reply\("(\w+)", reply\)', bodies[name]) != [name]]
    check(not unread, "each that sends one request labels its ending from the add-in's "
          "reply, once and under its own name: not so in %s" % unread)
    passed = [name for name in single if "_through(revit_read" in bodies[name]
              and '_ended(out.get("ended"), said)' not in bodies[name]]
    check(not passed, "each that hands back revit_read's answer hands back the label "
          "revit_read gave it: not so in %s" % passed)
    helper = server[server.find("\ndef _ended_by_reply(") + 1:]
    helper = helper[:helper.find("\ndef ")] if server.find("\ndef _ended_by_reply(") >= 0 else ""
    check("analyse(reply, writes=tools.writes(tool))" in helper
          and "_failure_ended(failure)" in helper,
          "and the reply is labelled by heron_failure's classification through "
          "_failure_ended, with the tool's declared risk, as the three doors label theirs")
    through = server[server.find("\ndef _through("):]
    through = through[:through.find("\ndef ", 1)]
    check('reply_out["ended"] = getattr(_ctx, "outcome", None)' in through
          and "_ctx.reply_out, _ctx.origin, _ctx.outcome = reply_out, origin, None" in through,
          "and _through hands the body's own label to its caller, emptied first so it is "
          "never the caller's word")
    _single_purpose_labels(server, bodies, single, through)


def _single_purpose_labels(server, bodies, single, through):
    """
    THE LABEL ITSELF, NOT ONLY THAT ONE IS SAID (the review of row 5b-389).
    The checks above passed with revit_views' pin refusal labelled "ok", with
    revit_rooms' NotBound labelled "ok", with revit_building_loads' Companion
    switched off labelled nothing, and with revit_views' reply label said only
    for a good reply - each a refusal or failure shown as OK again, which is
    the defect the row names. So each kind of ending is read in the one form
    that says it right, and every place it occurs in a single-purpose tool
    must be in that form: one form for each, counted, never sampled.
    """
    forms = (
        ("several Revits and none chosen (NotBound) is 'refused'",
         r"except NotBound as unbound:\n",
         r"except NotBound as unbound:\n        return _ended\(\"refused\", str\(unbound\)\)\n",
         ()),
        ("a distance it cannot read is 'refused'",
         r"except BadDistance as bad:\n",
         r"except BadDistance as bad:\n        return _ended\(\"refused\", str\(bad\)\)\n",
         ()),
        ("the pin's refusal is 'refused'",
         r"return [^\n]*\bwrong_model\b",
         r"return _ended\(\"refused\", wrong_model\)\n",
         ()),
        ("the Companion switched off is 'refused', said before its answer",
         r"if not companion_page\.enabled\(\):\n",
         r"if not companion_page\.enabled\(\):\n        _ended\(\"refused\"\)\n        return \(",
         # Says "refused" once, before every early return - read below.
         ("revit_offer_settings",)),
        ("revit_read's answer, handed back, carries revit_read's own label",
         r"if not isinstance\(reply, dict\) or not reply\.get\(\"ok\"\):\n",
         r"if not isinstance\(reply, dict\) or not reply\.get\(\"ok\"\):\n"
         r"        return _ended\(out\.get\(\"ended\"\), said\)\n",
         ()),
        ("the pin's refusal after revit_read is 'refused', and a good read 'ok'",
         r"if pinned\.check\(reply\):\n",
         r"if pinned\.check\(reply\):\n        return _ended\(\"refused\", said\)\n"
         r"    _ended\(\"ok\"\)\n",
         ()),
        ("the brain not there is 'failed'",
         r"except brain\.BrainUnavailable as why:\n",
         r"except brain\.BrainUnavailable as why:\n"
         r"        return _ended\(\"failed\", str\(why\)\)\n",
         ()),
        ("the brain's own 'Nothing was calculated / solved / laid out' is 'failed'",
         r"is None:\n        return [^\n]*answer\[\"said\"\]",
         r"is None:\n        return _ended\(\"failed\", answer\[\"said\"\]\)\n",
         ()),
    )
    for what, anywhere, right, exempt in forms:
        places = sum(len(re.findall(anywhere, bodies[name])) for name in single
                     if name not in exempt)
        wrong = [name for name in single if name not in exempt
                 and len(re.findall(anywhere, bodies[name]))
                 != len(re.findall(right, bodies[name]))]
        check(places and not wrong,
              "%s, in the one form that says so, at every place (%d read): not so in %s"
              % (what, places, wrong))

    # THE REPLY'S LABEL IS SAID WHATEVER THE REPLY - straight after the one
    # request is closed, never under a condition that skips a lost answer or
    # a refusal and leaves it to the guess.
    loose = [name for name in single if "session.request(" in bodies[name]
             and not re.search(r"\n    session\.close\(\)\n(?:    #[^\n]*\n)*"
                               r"    _ended_by_reply\(\"%s\", reply\)\n" % name, bodies[name])]
    check(not loose, "each reply is labelled unconditionally, straight after its request "
          "is closed: not so in %s" % loose)

    offer = bodies.get("revit_offer_settings", "")
    first_return = offer.find("return ")
    check(0 <= offer.find('\n    _ended("refused")\n') < first_return
          and offer.count('_ended("ok")') == 1
          and re.search(r'\n    _ended\("ok"\)\n    return \("Offered on the Heron Companion',
                        offer) is not None,
          "revit_offer_settings says 'refused' before its first early return, and 'ok' only "
          "where the table is offered")

    picked = bodies.get("revit_select_by_category", "")
    check(re.search(r'if reply\.get\("selected"\):\n(?:            #[^\n]*\n)*'
                    r'            _ended\("failed"\)\n            return \("Heron SELECTED',
                    picked) is not None,
          "revit_select_by_category: a selection the add-in already made in another model "
          "is 'failed' - something happened, so it is not refused, and not ok")

    health = bodies.get("revit_health", "")
    check(re.search(r'\n        _ended\("failed"\)\n        return \("Heron could not read its '
                    r'own session list', health) is not None
          and 'return _ended("ok", "\\n".join(lines))' in health,
          "revit_health: a session list it could not read is 'failed', a report it made 'ok'")

    chose = bodies.get("revit_use_session", "")
    check(re.search(r'\n    _ended\("ok"\)\n    return \("Now working with Revit', chose)
          is not None,
          "revit_use_session: the choice made is 'ok'")

    ended_at = through.find('reply_out["ended"] = getattr(_ctx, "outcome", None)')
    restored_at = through.find('_ctx.reply_out, _ctx.origin, _ctx.outcome = None, "chat", kept')
    check(0 <= ended_at < restored_at,
          "and _through reads the body's label BEFORE it puts the caller's back - read after, "
          "it is the caller's own word, and a refusal handed back is left to the guess")


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

    server = tools.server_text()
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
    server = tools.server_text()
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
    server = tools.server_text()
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

    def recalc(takeoff, inputs, identity, keep=None):
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
    # The Model Dashboard's 3D view joined the list on 2026-11-01 (docs/40 s21.13).
    check(hc.PAGES.get("/loads3d.js", (None,))[0] == "loads3d.js"
          and set(hc.PAGES) == {"/", "/companion.js", "/companion.css", "/loads3d.js",
                                "/sprinkler3d.js", "/model3d.js"},
          "the page files are a fixed list - the three 3D views' scripts are on it, nothing "
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

    server = tools.server_text()
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
    kept.recalculate_hook = lambda t, i, ident, keep=None: brain_seam.building_loads(t, i,
                                                                                     save=False)
    kept.recalculate({"project": GROUNDED, "profiles": {"Office": OFFICE}})
    check(kept.current()["read_at"] == "08:00:00",
          "Recalculate keeps the time the MODEL was read - it reads nothing itself")
    for hook in ("recalculate_hook = _loads_recalculate", "report_hook = _loads_report",
                 "finalize_hook = _loads_finalize", "confirm_hook = _loads_confirm"):
        check(hook in server, "the server sets %s" % hook.split(" =")[0])


def test_loads_recalculate_overtaken_by_a_fresh_read():
    """FRAGMENT-ISSUES 5b-333 (1), 2026-10-06: the chat read the model again
    (revit_building_loads) while the modeller's Recalculate was working on the
    take-off the page held. The Recalculate saved its run after the fresh one
    and opened its answer over it, so the page showed a building no longer
    there. Now it is neither kept nor shown, and the page is told why."""
    print()
    print("The Loads panel: a Recalculate overtaken by a fresh read of the model")
    sys.path.insert(0, os.path.join(ROOT, "brain"))
    sys.path.insert(0, os.path.join(ROOT, "tests"))
    import copy as _copy
    import heron_brain as brain_seam
    from test_takeoff import ROOM
    from test_building_loads import PROJECT, OFFICE
    inputs = {"project": PROJECT, "profiles": {"Office": OFFICE}}
    old = brain_seam.building_loads(json.dumps(ROOM), inputs, save=False)
    moved = _copy.deepcopy(ROOM)
    moved["spaces"][0]["area_m2"] = 27.5                          # a wall moved since
    fresh = brain_seam.building_loads(json.dumps(moved), inputs, save=False)
    panel = hc.LoadsPanel()
    panel.open("Project1", old, ("Project1", "", "11"))
    kept = []

    def overtaken(takeoff, given, identity, keep=None):
        reading = getattr(panel, "reading", None)                 # the chat reads again...
        if reading is not None:
            reading()
        panel.open("Project1", fresh, ("Project1", "", "11"))     # ...and opens its answer
        answer = brain_seam.building_loads(takeoff, given, save=False)
        if keep is not None:
            keep(lambda: kept.append("old") or "a path")
        return answer
    panel.recalculate_hook = overtaken
    said = panel.recalculate(inputs)
    check(said["ok"] is False and "read again" in (said.get("said") or ""),
          "a Recalculate overtaken by a fresh read answers that the model was read again")
    check(panel.current()["spaces"][0]["area_m2"] == 27.5,
          "the page still shows the fresh read, not the take-off the Recalculate started on")
    check(not kept, "the overtaken Recalculate's run is not kept")

    def calm(takeoff, given, identity, keep=None):
        answer = brain_seam.building_loads(takeoff, given, save=False)
        if keep is not None:
            keep(lambda: kept.append("new") or "a path")
        return answer
    panel.recalculate_hook = calm
    again = panel.recalculate(inputs)
    check(again["ok"] and kept == ["new"],
          "a Recalculate nothing overtook is kept and shown, as before")

    server = tools.server_text()
    tool = server[server.index("def revit_building_loads("):]
    tool = tool[:tool.index(chr(10) + "@server.tool()")]
    check(-1 < tool.find("LOADS_PANEL.reading()") < tool.find("brain.building_loads("),
          "the chat's fresh read tells the panel before it works the loads out and keeps them")
    recalc = server[server.index("def _loads_recalculate("):]
    recalc = recalc[:recalc.index(chr(10) + "def ", 10)]
    check("keep=keep" in recalc, "Recalculate hands the brain the panel's keep")


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
    server = tools.server_text()
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
    # FRAGMENT-ISSUES 5b-335 (2026-10-06): diffusers placed with no level were
    # never on the level Finalize asked for, so none was handed over - and the
    # page blamed their family. And the second Finalize's schedule step threw.
    check('"category=Air Terminals" + nl + "levelId=none"' in fin
          and '"category=Air Terminals" + nl + "levelId=" + level' not in fin,
          "the diffusers are handed over by category with no level - the file of ids picks "
          "which are written, whatever level parameter each carries")
    check("LOADS.diffusers_said(" in fin and "sit on a level no calculated" not in fin,
          "each diffuser not written is put down to the reason SET_AIR_TERMINAL_FLOW gave")
    names = fin.find("_schedule_names()")
    check(-1 < names < fin.find('"CREATE_SCHEDULE"') and "LOADS.schedule_made(" in fin,
          "Finalize looks for the schedule before making it, so a second Finalize says it is "
          "there instead of letting Revit refuse a second one of that name")


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
    server = tools.server_text()
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


def test_layout_panel():
    """The Sprinkler Layout panel (docs/47): data and hooks only, every route behind
    the pairing, one level placed once, by name and value through revit_change."""
    print()
    print("The Sprinkler Layout panel: Preview places nothing; Apply places one level, once")
    sys.path.insert(0, os.path.join(ROOT, "brain"))
    sys.path.insert(0, os.path.join(ROOT, "tests"))
    import heron_brain as brain_seam
    import test_sprinkler_layout as LAY
    panel = hc.LayoutPanel()
    check(panel.preview({})["said"] == hc.LayoutPanel.GONE
          and panel.place({"level": "L2"})["said"] == hc.LayoutPanel.GONE,
          "with no chat connected, Preview and Apply say so and do nothing")
    data = json.dumps(LAY.read())
    answer = brain_seam.sprinkler_layout(data, {})
    panel.open("Project1", answer, ("Project1", "", "11"), read_at="08:00:00")
    shown = panel.current()
    check(shown["status"] == "asked" and shown["asked"] and shown["types"]
          and shown["fields"]["hazard_classes"] and len(shown["view"]) == 3,
          "the panel shows the questions, the types and each room's plan data from the brain")
    panel.preview_hook = lambda d, inputs, identity: brain_seam.sprinkler_layout(d, inputs)
    got = panel.preview(LAY.answers())
    check(got["ok"] and got["layout"]["status"] == "ok" and got["layout"]["levels"]["L2"]["heads"],
          "Preview hands the page's answers to the hook and shows the layout")
    check(panel.preview({"rooms": "not a map"})["ok"] is False,
          "answers that are not a map are refused - nothing is laid out")
    room = [r for r in panel.current()["rooms"] if r["key"] == "u-office"][0]
    check(room["measured"] and room["measured"]["rows"] and room["checks"],
          "each room's per-head measure and checks reach the page, to be read before Apply")
    newer = brain_seam.sprinkler_layout(data, {})

    def slow(d, inputs, identity):
        panel.open("Project1", newer, ("Project1", "", "11"))   # the chat reads again meanwhile
        return brain_seam.sprinkler_layout(d, inputs)
    panel.preview_hook = slow
    late = panel.preview(LAY.answers())
    check(late["ok"] is False and panel.current()["status"] == "asked",
          "a preview that finishes after a newer read is dropped - the newer read stays")
    panel.preview_hook = lambda d, inputs, identity: brain_seam.sprinkler_layout(d, inputs)
    panel.preview(LAY.answers())
    calls = []

    def place(d, result, inputs, level, identity):
        calls.append(level)
        return {"ok": True, "said": "placed", "placed": {"level": level}}
    panel.place_hook = place
    first = panel.place({"level": "L2"})
    again = panel.place({"level": "L2"})
    check(first["ok"] and not again["ok"] and calls == ["L2"],
          "a level is placed once: a second press places nothing")
    panel.place_hook = lambda *a: {"ok": False, "said": "refused"}
    panel.open("Project1", brain_seam.sprinkler_layout(data, LAY.answers()), ("Project1", "", "11"))
    panel.place({"level": "L2"})
    check("L2" not in panel.current()["placed"],
          "a placing that was refused leaves the level free to try again")
    check(panel.place({})["ok"] is False, "Apply with no level places nothing")
    source = io.open(os.path.join(ROOT, "mcp", "companion", "heron_companion.py"),
                     encoding="utf-8").read()
    i = source.index('if path == "/api/layout":')
    j = source.index('if path in ("/api/layout/preview", "/api/layout/place"):')
    check(source.index("why = self._api_ok()", i) < source.index("LAYOUT_PANEL.current()", i)
          and source.index("why = self._api_ok()", j) < source.index("LAYOUT_PANEL.preview(", j),
          "every Layout route is behind _api_ok like every other route")
    server = tools.server_text()
    for hook in ("preview_hook = _sprinkler_layout_preview",
                 "place_hook = _sprinkler_layout_place"):
        check(hook in server, "the server sets %s" % hook.split(" =")[0])
    place_src = server[server.index("def _sprinkler_layout_place("):]
    place_src = place_src[:place_src.index(chr(10) + "def ", 10)]
    check("_moved_since(identity)" in place_src and "with _revit_lock:" in place_src
          and '"PLACE_FAMILY_INSTANCES"' in place_src and "LAYOUT.changed(" in place_src
          and "LAYOUT.read_back(" in place_src and "Selection" not in place_src,
          "Apply checks the pin and that the rooms have not moved under the lock, places by "
          "name and value, and reads back")
    check(tools.TOOLS.get("revit_sprinkler_layout") == (tools.ANALYZE, "run_fragment_read")
          and tools.COMPANION_ACTIONS.get("companion_sprinkler_place")
          == (tools.MODIFY, "run_fragment_write"),
          "the chat tool reads only; the page's Apply is declared at revit_change's level")
    page = io.open(os.path.join(ROOT, "mcp", "companion", "static", "companion.js"),
                   encoding="utf-8").read()
    check('"/api/layout/place"' in page and "createElementNS" in page,
          "the page places through its own route and draws the plans in SVG it makes")
    print()


def test_dashboard_panel():
    """The Model Dashboard (docs/40 s21.10): data and one hook only, its routes behind
    the pairing, Refresh only when pressed and one at a time, and nothing on it
    writes to Revit."""
    print()
    print("The Model Dashboard: data and one hook only, read when pressed, writes nothing")
    sys.path.insert(0, os.path.join(ROOT, "brain"))
    sys.path.insert(0, os.path.join(ROOT, "tests"))
    import heron_brain as brain_seam
    import test_model_dashboard as DASH
    panel = hc.DashboardPanel()
    check(panel.current() is None, "with nothing read, the panel holds nothing")
    check(panel.refresh() == {"ok": False, "said": "Not read: %s." % hc.DashboardPanel.GONE},
          "with no chat connected, Refresh says so and reads nothing")
    answer = brain_seam.model_dashboard(DASH.sample())
    panel.open("Tower MEP", answer, ("Tower MEP", "", "11"))
    shown = panel.current()
    check(shown["document"] == "Tower MEP" and shown["tiles"] and shown["charts"]
          and shown["look"] and shown["states"] and shown["busy"] is False,
          "the panel shows the brain's tiles, charts, rows and colours as they came")
    calls, asked, boards_asked = [], [], []

    def hook(parameters, board):
        calls.append(panel.current()["busy"])
        asked.append(parameters)
        boards_asked.append(board)
        check(panel.refresh()["said"] == "Not read: %s." % hc.DashboardPanel.BUSY,
              "a second Refresh while one reads is told so and reads nothing")
        return {"ok": True, "said": "read again"}
    panel.refresh_hook = hook
    check(panel.refresh() == {"ok": True, "said": "read again"} and calls == [True],
          "Refresh calls the hook once, marked busy while it runs")
    check(panel.current()["busy"] is False, "busy is cleared after the read")
    panel.refresh({"parameters": "Mark, Fire Rating"})
    panel.open("Tower MEP", answer, ("Tower MEP", "", "11"), parameters="Mark, Fire Rating")
    panel.refresh({})
    check(asked == ["", "Mark, Fire Rating", "Mark, Fire Rating"],
          "Refresh carries the parameters typed on the page, and keeps the last read's "
          "when none are sent - found %r" % (asked,))
    panel.refresh({"board": "rooms"})
    check(boards_asked[-1] == "rooms", "Refresh carries the board picked on the page")

    def broken(_parameters, _board):
        raise RuntimeError("Revit closed")
    panel.refresh_hook = broken
    try:
        panel.refresh()
    except RuntimeError:
        pass
    check(panel.current()["busy"] is False, "busy is cleared even when the read raises")

    source = io.open(SOURCE, encoding="utf-8").read()
    i = source.index('if path == "/api/dashboard":')
    j = source.index('if path == "/api/dashboard/refresh":')
    k = source.index('if path == "/api/dashboard/show":')
    m = source.index('if path == "/api/dashboard/demo":')
    q = source.index('if path == "/api/dashboard/colour":')
    check(source.index("why = self._api_ok()", q) < source.index("DASHBOARD_PANEL.colour(", q),
          "the colour route is behind _api_ok like every other route")
    n = source.index('if path == "/api/dashboard/load3d":')
    o = source.index('if path == "/api/dashboard/view":')
    check(source.index("why = self._api_ok()", n) < source.index("DASHBOARD_PANEL.load3d(", n)
          and source.index("why = self._api_ok()", o) < source.index("DASHBOARD_PANEL.view()", o),
          "the 3D routes are behind _api_ok like every other route")
    check(source.index("why = self._api_ok()", i) < source.index("DASHBOARD_PANEL.current()", i)
          and source.index("why = self._api_ok()", j) < source.index("DASHBOARD_PANEL.refresh(", j)
          and source.index("why = self._api_ok()", k) < source.index("DASHBOARD_PANEL.show(", k)
          and source.index("why = self._api_ok()", m) < source.index("DASHBOARD_PANEL.demo(", m),
          "every Dashboard route is behind _api_ok like every other route")
    server = tools.server_text()
    check("DASHBOARD_PANEL.refresh_hook = _dashboard_refresh" in server,
          "the server sets the Dashboard's refresh_hook")
    refresh_src = server[server.index("def _dashboard_refresh("):]
    refresh_src = refresh_src[:refresh_src.index(chr(10) + "def ", 10)]
    read_src = server[server.index("def _read_dashboard("):]
    read_src = read_src[:read_src.index(chr(10) + "def ", 10)]
    show_src = server[server.index("def _dashboard_show("):]
    show_src = show_src[:show_src.index(chr(10) + "def ", 10)]
    check("with _revit_lock:" in show_src and "_moved_since(identity)" in show_src
          and "revit_change" not in show_src and '"SET_SELECTION"' in show_src
          and '"FILTER_ELEMENTS_BY_ID"' in show_src,
          "Select, Isolate and Reset run under the lock, on the read's model only, and "
          "never through revit_change")
    check("DASHBOARD_PANEL.show_hook = _dashboard_show" in server
          and "DASHBOARD_PANEL.demo_hook = _dashboard_demo" in server,
          "the server sets the Dashboard's show_hook and demo_hook")
    colour_src = server[server.index("def _dashboard_colour("):]
    colour_src = colour_src[:colour_src.index(chr(10) + "def ", 10)]
    check("DASHBOARD_PANEL.colour_hook = _dashboard_colour" in server
          and "with _revit_lock:" in colour_src and "_moved_since(identity)" in colour_src
          and "_through(revit_change" in colour_src and '"COLOR_BY_PARAMETER"' in colour_src
          and '"RESET_GRAPHIC_OVERRIDES"' in colour_src and '"FILTER_ELEMENTS_BY_CATEGORY"' in colour_src,
          "Colour runs under the lock, on the read's model only, through revit_change's own "
          "body - the gate, the switch, one undo entry")
    check("DASHBOARD_PANEL.load3d_hook = _dashboard_load3d" in server
          and '"REPORT_MODEL_GEOMETRY"' in server,
          "the server sets the 3D view's load3d_hook, reading REPORT_MODEL_GEOMETRY")
    check("with _revit_lock:" in refresh_src and "revit_change" not in read_src
          and '"REPORT_MODEL_DASHBOARD"' in read_src and "pinned.check(reply)" in read_src,
          "Refresh reads under the lock, through revit_read only, and the pin's refusal stands")
    mine = dict((k, v) for k, v in tools.COMPANION_ACTIONS.items() if "dashboard" in k)
    on_screen = ("companion_dashboard_isolate", "companion_dashboard_reset",
                 "companion_dashboard_select")
    view_changes = ("companion_dashboard_clear_colours", "companion_dashboard_colour")
    check(tools.TOOLS.get("revit_model_dashboard") == (tools.ANALYZE, "run_fragment_read")
          and sorted(mine) == sorted(on_screen + view_changes)
          and all(mine[k][0] == tools.EXECUTE for k in on_screen)
          and all(mine[k] == (tools.MODIFY, "run_fragment_write") for k in view_changes),
          "the chat tool reads only; the page's Select, Isolate and Reset are on screen only, at "
          "EXECUTE; Colour and Clear change the view's graphics at revit_change's level, and "
          "nothing goes above MODIFY (docs/40 s21.14)")

    # THE ROW BUTTONS (docs/40 s21.11): the page names a row, never ids.
    shown = hc.DashboardPanel()
    check(shown.show({"action": "select", "row": "rooms-unplaced"})["said"]
          == "Nothing was sent to Revit: %s." % hc.DashboardPanel.GONE,
          "with no chat connected, Select says so and sends nothing")
    sent = []
    shown.show_hook = lambda action, ids, identity: (sent.append((action, ids, identity))
                                                    or {"ok": True, "said": "selected"})
    check(shown.show({"action": "select", "row": "rooms-unplaced"})["ok"] is False and not sent,
          "with nothing read, a row cannot be selected")
    shown.open("Tower MEP", answer, ("Tower MEP", "", "11"))
    got = shown.show({"action": "select", "row": "rooms-unplaced", "ids": ["999"]})
    check(got == {"ok": True, "said": "selected"}
          and sent == [("select", ["501", "502"], ["Tower MEP", "", "11"])],
          "Select sends the ids the panel's own read found, never ids the page typed, "
          "with the model the read came from")
    check(shown.show({"action": "isolate", "row": "groups-unused"})["ok"] is False
          and len(sent) == 1,
          "a row with no ids sends nothing")
    check(shown.show({"action": "delete", "row": "rooms-unplaced"})["ok"] is False
          and len(sent) == 1,
          "an action that is not select, isolate or reset is refused")
    check(shown.show({"action": "reset"})["ok"] and sent[-1] == ("reset", [], ["Tower MEP", "", "11"]),
          "Reset needs no row")

    # THE MODEL HAS CHANGED (D-147): the panel keeps the count at its read; the
    # page compares it with the live file's; the server records it after a read.
    counted = hc.DashboardPanel()
    counted.open("Tower MEP", answer, ("Tower MEP", "", "11"), changes=12)
    check(counted.current()["changes_at_read"] == 12, "the panel keeps the change count at its read")
    counted.open("Tower MEP", answer, ("Tower MEP", "", "11"), changes="12")
    check(counted.current()["changes_at_read"] is None, "and believes only a whole number")
    read_body = server[server.index("def _read_dashboard("):]
    read_body = read_body[:read_body.index(chr(10) + "def ", 10)]
    check("companion_page.live_changes(binding.pid)" in read_body and "changes=changes" in read_body,
          "the server records the live count right after a good read")
    page_js = io.open(PAGE_JS, encoding="utf-8").read()
    check("function dashChanged(state)" in page_js and "dashChanged(body.state);" in page_js
          and "now > was" in page_js,
          "the page shows the note when the live count is higher than at the read - on the "
          "state poll it already makes, reading nothing from Revit")
    boarded = brain_seam.model_dashboard(DASH.sample(breakdowns=DASH.BREAKDOWNS), board="rooms")
    shown.open("Tower MEP", boarded, ("Tower MEP", "", "11"), board="rooms")
    check(shown.show({"action": "select", "bar": ["rooms-level", "Level 01"]})["ok"]
          and sent[-1][1] == ["11", "12"],
          "a board bar selects the ids the panel's own read found for it")
    check(shown.show({"action": "select", "bar": ["rooms-level", "Level 09"]})["ok"] is False,
          "a bar the panel does not hold sends nothing")

    # THE DEMO (docs/40 s21.12): invented, made in brain/ through a hook, and
    # nothing on it ever reaches Revit.
    shown.demo_hook = brain_seam.model_dashboard_demo
    check(shown.demo()["ok"] is False and shown.current()["document"] == "Tower MEP",
          "the demo never replaces a read of the modeller's model")
    demo = hc.DashboardPanel()
    check(demo.demo()["ok"] is False, "with no chat connected there is no demo")
    demo.demo_hook = brain_seam.model_dashboard_demo
    demo.show_hook = shown.show_hook
    before = len(sent)
    check(demo.demo()["ok"] and demo.current()["demo"] is True,
          "the empty panel opens the demo, marked as the demo")
    check(demo.show({"action": "reset"})["ok"] is False and len(sent) == before,
          "on the demo, not even Reset reaches Revit")

    # COLOUR THE VIEW (docs/40 s21.14): a change, through the hook only, never
    # on the demo, and never without a parameter.
    coloured = []
    demo.colour_hook = lambda p, clear, identity: (coloured.append(p) or {"ok": True})
    check(demo.colour({"parameter": "System Type"})["ok"] is False and not coloured,
          "the demo is never coloured")
    check(hc.DashboardPanel().colour({"parameter": "System Type"})["said"]
          == "Nothing was changed: %s." % hc.DashboardPanel.GONE,
          "with no chat connected, Colour says so and changes nothing")
    shown.colour_hook = lambda p, clear, identity: (coloured.append((p, clear, identity))
                                                   or {"ok": True, "said": "coloured"})
    check(shown.colour({"parameter": "  "})["ok"] is False and not coloured,
          "no parameter, no change")
    check(shown.colour({"parameter": " System Type "})["ok"]
          and coloured[-1] == ("System Type", False, ["Tower MEP", "", "11"]),
          "Colour hands the parameter and the read's model to the hook")
    check(shown.colour({"clear": True})["ok"] and coloured[-1][1] is True,
          "Clear colours needs no parameter")
    check(tools.COMPANION_ACTIONS.get("companion_dashboard_colour") == (tools.MODIFY, "run_fragment_write")
          and tools.COMPANION_ACTIONS.get("companion_dashboard_clear_colours")
          == (tools.MODIFY, "run_fragment_write"),
          "Colour and Clear are declared at revit_change's own level")
    # THE 3D VIEW (docs/40 s21.13): held apart, fetched on its own, and a
    # click selects only an element the panel's own 3D read found.
    import heron_brain as brain3d
    import test_model_view as VIEW
    check(demo.load3d({})["ok"] is False, "no 3D read with no chat connected")
    demo.load3d_hook = lambda whole, rooms: {"ok": True, "said": "read"}
    check(demo.load3d({})["ok"] is False and "DEMO" in demo.load3d({})["said"],
          "no 3D read while the dashboard shows the demo")
    whole_asked = []
    shown.load3d_hook = lambda whole, rooms: (whole_asked.append((whole, rooms))
                                              or {"ok": True, "said": "read"})
    check(shown.load3d({"whole": True})["ok"] and shown.load3d({"whole": "yes"})["ok"]
          and [w for w, _ in whole_asked] == [True, False],
          "Load 3D asks for the whole model only when the page says exactly true")
    check(shown.load3d({"rooms": True})["ok"] and shown.load3d({"rooms": 1})["ok"]
          and [r for _, r in whole_asked[2:]] == [True, False],
          "and for rooms as volumes only when the page says exactly true (s21.18)")
    got3d = brain3d.model_view(VIEW.sample())
    shown.set_view(got3d, ("Tower MEP", "", "11"))
    small = shown.current()["view3d"]
    check("materials" not in small and "hasMaterials" in small and "materialLegend" in small,
          "it carries the material legend, never each element's materials (s21.18)")
    check(small["drawn"] == 4 and "elements" not in small and shown.view()["elements"],
          "the panel's state carries the 3D summary, never the shapes - those come from "
          "/api/dashboard/view")
    check(shown.show({"action": "select", "element": "102"})["ok"] and sent[-1][1] == ["102"],
          "a click in the 3D view selects that element by the panel's own 3D read")
    check(shown.show({"action": "select", "element": "999"})["ok"] is False,
          "an element the 3D read did not find sends nothing")
    shown.set_view({"view": None})
    check(shown.view() is None and "view3d" not in shown.current(),
          "a failed 3D read leaves no stale view behind")
    # THE 3D VIEW'S OWN TOOLS (docs/40 s21.17): the page's alone - cameras,
    # zoom to what is lit, a cut height, categories hidden, a picture saved on
    # the PC, full screen. None of them reaches Revit or fetches anything new.
    m3d = io.open(os.path.join(os.path.dirname(PAGE_JS), "model3d.js"), encoding="utf-8").read()
    check(all(word in m3d for word in ("function preset(", "function zoomLit(", "function setHidden(",
                                       "function savePicture(", "requestFullscreen", "uCut")),
          "the 3D view has Top/Front/Side/3D, Zoom to lit, a cut height, category hiding, "
          "Save picture and Full screen")
    check(re.findall(r'fetch\(\s*"([^"]+)"', m3d) == ["/api/dashboard/view"]
          and "innerHTML" not in m3d and "XMLHttpRequest" not in m3d,
          "and it fetches only its own shapes, and puts text in as text")
    check("aVis > 0.5" in m3d and m3d.count("aVis") >= 4,
          "a hidden category is neither drawn nor picked - both passes honour it")
    page = io.open(PAGE_JS, encoding="utf-8").read()
    check("window.HeronModel3D.setHidden(" in page and "d-legend-switch" in page,
          "the 3D legend is the category switch")
    dash = page[page.index("// ---------------------------------------------------------------- dashboard"):
                page.index("// ---------------------------------------------------------------- sprinkler")]
    check('"/api/dashboard/refresh"' in dash and "setInterval" not in dash
          and "innerHTML" not in dash and "setTimeout(dashRefresh" not in dash,
          "the page reads again only on Refresh, never on a timer, and puts text in as text")
    print()


def test_checklist_panel(tmp):
    """The QA Checklist workspace (docs/50): data and hooks only, its routes behind
    the pairing on a real server, Run one at a time and only when pressed, each
    check's read under the lock for that read alone, and Select in Revit the Model
    Dashboard's own route with the ids the run found - never ids the page sent."""
    print()
    print("The QA Checklist: data and hooks only, run when pressed, Select by the dashboard's route")
    panel = hc.ChecklistPanel()
    shown = panel.current()
    check(shown["connected"] is False and shown["lists"] == [] and shown["result"] is None,
          "with no chat connected, the panel holds nothing")
    check(panel.run({"name": "Before issue"}) == {"ok": False, "said": "Nothing was run: %s."
                                                  % hc.ChecklistPanel.GONE},
          "with no chat connected, Run says so and runs nothing")
    panel.list_hook = lambda: {"lists": [{"name": "Before issue", "checks": 2, "description": "",
                                          "sentences": ["a", "b"]}], "broken": ["x.json"]}
    rows = [{"n": 1, "state": "pass", "ids": [], "idsComplete": False},
            {"n": 2, "state": "fail", "ids": ["501", "502"], "idsComplete": True},
            {"n": 3, "state": "fail", "ids": [], "idsComplete": False, "idsTotal": 40}]
    answer = {"name": "Before issue", "rows": rows, "counts": {"pass": 1, "fail": 2, "not run": 0},
              "identity": ["Tower MEP", "", "11"], "said": "said", "run": "run-1"}
    seen = []

    def run(name, hold):
        seen.append((name, panel.current()["busy"]))
        check(panel.run({"name": name})["said"] == "Nothing was run: %s." % hc.ChecklistPanel.BUSY,
              "a second Run while one runs is told so in words and runs nothing")
        hold(dict(answer, running=True, rows=rows[:1]))
        check(panel.current()["result"]["running"] is True, "each check's row reaches the page as it is done")
        hold(answer)
        return {"ok": True, "said": "ran"}
    panel.run_hook = run
    check(panel.run({})["ok"] is False and not seen, "a Run with no checklist named runs nothing")
    check(panel.run({"name": "Before issue"}) == {"ok": True, "said": "ran"}
          and seen == [("Before issue", True)], "Run calls the hook once with the name, marked busy")
    shown = panel.current()
    check(shown["busy"] is False and shown["lists"][0]["name"] == "Before issue"
          and shown["broken"] == ["x.json"] and len(shown["result"]["rows"]) == 3,
          "the page is sent the brain's list and rows as they came")
    check("identity" not in shown["result"], "and never the model's identity, which stays with the panel")

    def broken(_name, hold):
        hold(dict(answer, running=True, rows=rows[:1], run="run-0"))
        raise RuntimeError("Revit closed")
    panel.run_hook = broken
    got = panel.run({"name": "Before issue"})
    shown = panel.current()
    check(got["ok"] is False and "The run stopped: Revit closed" in got["said"]
          and shown["busy"] is False and shown["result"]["running"] is False
          and "stopped" in shown["result"]["said"],
          "a run that raises is told in words, its held result made final - never left Running")
    panel.hold(answer)

    # ONE RUN AT A TIME, CHAT AND PAGE TOGETHER.
    check(panel.begin("chat") is None, "a chat run claims the one run")
    check(panel.run({"name": "Before issue"})["said"] == "Nothing was run: %s." % hc.ChecklistPanel.BUSY_CHAT
          and panel.current()["busyWith"] == "chat",
          "a page Run while a chat run goes is refused in words, naming the chat")
    check(panel.show({"action": "select", "row": 2, "run": "run-1"})["ok"] is False,
          "and so is Select, until the chat run ends")
    panel.end()

    def page_run(_name, _hold):
        seen.append(("chat said", panel.begin("chat")))
        return {"ok": True, "said": "ran"}
    panel.run_hook = page_run
    panel.run({"name": "Before issue"})
    check(seen[-1] == ("chat said", hc.ChecklistPanel.BUSY),
          "a chat run while a page run goes is refused in words, naming the page")
    check(panel.begin("chat") is None, "after the page run ends, the chat may run")
    panel.end()

    sent = []
    check(panel.show({"action": "select", "row": 2})["said"]
          == "Nothing was sent to Revit: %s." % hc.ChecklistPanel.GONE, "Select with no chat sends nothing")
    panel.show_hook = lambda action, ids, identity: sent.append((action, ids, identity)) or {"ok": True,
                                                                                         "said": "selected"}
    got = panel.show({"action": "select", "row": 2, "run": "run-1", "ids": ["999"]})
    check(got == {"ok": True, "said": "selected"} and sent == [("select", ["501", "502"],
                                                                ["Tower MEP", "", "11"])],
          "Select sends the ids the panel's own run found, never ids the page typed, with the model it read")
    check(panel.show({"action": "select", "row": 3, "run": "run-1"})["ok"] is False and len(sent) == 1,
          "a row whose ids did not all arrive sends nothing")
    check(panel.show({"action": "select", "row": 1, "run": "run-1"})["ok"] is False and len(sent) == 1,
          "a passing row has no ids to send")
    check(panel.show({"action": "isolate", "row": 2, "run": "run-1"})["ok"] is False and len(sent) == 1,
          "an action that is not select is refused")
    # A CHAT RUN REPLACED THE RESULT BEFORE THE PAGE REDREW: the page's row 2 is
    # the old run's, so nothing is sent.
    panel.hold(dict(answer, run="run-2", rows=[dict(rows[1], ids=["901"])]))
    got = panel.show({"action": "select", "row": 2, "run": "run-1"})
    check(got["ok"] is False and got["said"] == "Nothing was sent to Revit: %s." % hc.ChecklistPanel.REPLACED
          and len(sent) == 1, "Select on a run the page no longer shows is refused in words")
    check(panel.show({"action": "select", "row": 2})["ok"] is False and len(sent) == 1,
          "and so is Select with no run named")
    check(panel.show({"action": "select", "row": 2, "run": "run-2"})["ok"] and sent[-1][1] == ["901"],
          "the run the page does show selects its own ids")

    def raising_list():
        raise ValueError("one bad file")
    panel.list_hook = raising_list
    check(panel.current()["result"] is not None, "a list that cannot be read never stops the page updating")

    source = io.open(SOURCE, encoding="utf-8").read()
    i = source.index('if path == "/api/checklist":')
    j = source.index('if path in ("/api/checklist/run", "/api/checklist/show"):')
    check(source.index("why = self._api_ok()", i) < source.index("CHECKLIST_PANEL.current()", i)
          and source.index("why = self._api_ok()", j) < source.index("CHECKLIST_PANEL.run(", j)
          and source.index("why = self._api_ok()", j) < source.index("CHECKLIST_PANEL.show(", j),
          "every QA Checklist route is behind _api_ok like every other route")
    server = tools.server_text()
    check("CHECKLIST_PANEL.list_hook = _checklist_list" in server
          and "CHECKLIST_PANEL.run_hook = _checklist_run_page" in server
          and "CHECKLIST_PANEL.show_hook = _checklist_show" in server
          and "_dashboard_show(action, ids, identity, moved_said=CHECKLIST_MOVED)" in server,
          "the server sets the hooks, and Select is the Model Dashboard's own _dashboard_show, in "
          "the checklist's own words")
    page_run = server[server.index("def _checklist_run_page("):]
    page_run = page_run[:page_run.index(chr(10) + "def ", 10)]
    ask_src = server[server.index("def _checklist_ask("):]
    ask_src = ask_src[:ask_src.index(chr(10) + "def _run_checklist(", 10)]
    check("_revit_lock" not in page_run and ask_src.count("with _revit_lock:") == 1
          and "_through(revit_read, reply_out=out, origin=origin)" in ask_src
          and "revit_change" not in page_run + ask_src and "pinned.check(reply)" in ask_src,
          "a page run takes the lock per check, for that read alone, through revit_read's own body "
          "with the pin's refusal - never revit_change")
    check(tools.TOOLS.get("heron_checklist") == (tools.ANALYZE, "run_fragment_read")
          and not any("checklist" in k for k in tools.COMPANION_ACTIONS),
          "the run is declared at revit_read's level on the chat's tool; the page adds no action of its own")

    # A REAL SERVER: the routes answer only a paired page with the header.
    companion = hc.Companion(bound_pid=lambda: 100, folder=os.path.join(tmp, "qa-live"),
                             discovery_dir=os.path.join(tmp, "qa-bridges"), is_alive=lambda pid: True)
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
        no_cookie = ask(port, "GET", "/api/checklist", {"X-Heron-Companion": "1"})
        no_origin = ask(port, "POST", "/api/checklist/run", {"X-Heron-Companion": "1", "Cookie": cookie,
                                                              "Content-Type": "application/json"},
                        json.dumps({"name": "Before issue"}))
        got = ask(port, "GET", "/api/checklist", {"X-Heron-Companion": "1", "Cookie": cookie})
    finally:
        printed, sys.stdout = sys.stdout.getvalue(), real_stdout
        companion.stop()
    check(no_cookie[0] == 403 and no_origin[0] == 403,
          "without the cookie, or a POST without the page's Origin, the routes refuse")
    check(got[0] == 200 and json.loads(got[2].decode("utf-8"))["ok"] is True,
          "a paired page with the header is answered")
    check(printed == "", "NOTHING reached stdout, where MCP speaks")

    js = io.open(PAGE_JS, encoding="utf-8").read()
    block = js[js.index("// ---------------------------------------------------------------- QA checklist"):
               js.index("// ---------------------------------------------------------------- sprinkler")]
    check("innerHTML" not in block and "insertAdjacentHTML" not in block,
          "the page puts model text on the page with textContent only")
    check(set(re.findall(r'fetch\("([^"]+)"', block)) <= {"/api/checklist"}
          and "/api/checklist/run" in block and "/api/checklist/show" in block
          and '{ action: "select", row: row, run: run }' in block,
          "it asks Heron only, and Select sends the row's number and its run, never ids")
    check("checklist_();" in js[js.index("async function poll()"):js.index("function theme()")]
          and '["qa", "q-empty"]' in js, "the poll reads it, and it is a workspace in the sidebar")
    print()


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
    server = tools.server_text()
    for hook in ("calculate_hook = _sprinkler_calculate", "suggest_hook = _sprinkler_suggest",
                 "confirm_hook = _sprinkler_confirm", "report_hook = _sprinkler_report"):
        check(hook in server, "the server sets %s" % hook.split(" =")[0])
    calc_src = server[server.index("def _sprinkler_calculate("):]
    calc_src = calc_src[:calc_src.index(chr(10) + "def ", 10)]
    check("_moved_since(identity)" in calc_src and "revit_change" not in calc_src,
          "Calculate is refused when the chat moved models, and sends nothing to Revit")
    check(tools.TOOLS.get("revit_sprinkler_hydraulics") == (tools.ANALYZE, "run_fragment_read"),
          "the tool is declared at revit_read's level - it reads and changes nothing")
    # Phase 1b (docs/46 s13): the spacing and fire water inputs go through untouched.
    body2 = dict(body, spacing={"hazard": {"7001": "light hazard"}},
                 water={"include": {"pump": True}, "pump": {"rated_flow_lpm": 2000}})
    panel.calculate(body2)
    check(seen["inputs"]["spacing"] == body2["spacing"]
          and seen["inputs"]["water"] == body2["water"],
          "Calculate hands the spacing and fire water inputs to the hook as the page sent them")
    check(panel.calculate(dict(body, water="not a map"))["ok"] is False,
          "fire water inputs that are not a map are refused - nothing is calculated")
    shown = panel.current()
    check(shown["water_parts"] and shown["water_fields"].get("pump")
          and shown["spacing_fields"] and shown["hazard_classes"],
          "the page is sent the fire water questions, the spacing limits and the classes by "
          "the brain - it holds no list of its own")
    page = io.open(os.path.join(ROOT, "mcp", "companion", "static", "companion.js"),
                   encoding="utf-8").read()
    check("Spacing in each Space" in page and "Fire water - what the source must give" in page
          and "not included" in page,
          "the page shows the Spacing and Fire water sections, and says a part not included")
    check("innerHTML" not in page and "el(\"div\", null, row.why)" in page
          and "[row.label, row.level" in page,
          "Space names and their results reach the page as text only (Golden Rule 19)")


def test_page_is_in_the_brand():
    """The page's own colours are the brand kit's (D-149, docs/40 s21.19). The palette
    is read from docs/brand/README.md, as tools/check-brand.py reads it; the paint
    colour is the model's and stays outside, on the owner's word (docs/brand)."""
    print()
    print("The page in the brand's colours")
    brand = io.open(os.path.join(ROOT, "docs", "brand", "README.md"), encoding="utf-8").read()
    section = brand[brand.index("## The colours"):brand.index("## Not the brand")]
    palette = {h.upper() for h in re.findall(r"`#([0-9A-Fa-f]{6})`", section)}
    check(len(palette) >= 8, "the palette is read from docs/brand (%d colours)" % len(palette))
    static = os.path.join(ROOT, "mcp", "companion", "static")
    css = io.open(os.path.join(static, "companion.css"), encoding="utf-8").read()
    found = []
    for line in css.splitlines():
        if "var(--paint," in line:
            continue                      # the model's paint colour (docs/brand, "not the brand")
        for h in re.findall(r"#([0-9A-Fa-f]{6})(?:[0-9A-Fa-f]{2})?\b", line):
            found.append(h.upper())
        for r, g, b in re.findall(r"rgba?\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)", line):
            found.append("%02X%02X%02X" % (int(r), int(g), int(b)))
    outside = sorted(set(found) - palette)
    check(found and not outside, "every colour the stylesheet types is a brand colour, or one see-through"
          + (" - outside: %s" % ", ".join(outside) if outside else ""))
    index = io.open(os.path.join(static, "index.html"), encoding="utf-8").read()
    check("heron-mark-white.svg" in index, "the sidebar's logo is the brand's heron mark")
    print()


def test_workspace_tabs():
    """A workspace full of data is split into tabs (docs/40 s21.20): its header stays,
    one job per tab, and the tabs only show and hide what the panel drew - they read
    nothing from Heron or Revit and run on no timer of their own."""
    print()
    print("Workspace tabs instead of one long page")
    js = io.open(PAGE_JS, encoding="utf-8").read()
    css = io.open(os.path.join(ROOT, "mcp", "companion", "static", "companion.css"), encoding="utf-8").read()
    tabs = js[js.index("const TABS = {"):js.index("function tabGo(")]
    for ws, labels in (("dashboard", ["Overview", "Issues", "3D view", "Parameters"]),
                       ("loads", ["Results", "3D take-off", "Inputs", "Checks"]),
                       ("sprinkler", ["Design inputs", "Remote area & spacing", "Fire water", "3D", "Checks"])):
        check(("  %s: [" % ws) in tabs and all(('label: "%s"' % l) in tabs for l in labels),
              "%s has its tabs: %s" % (ws, ", ".join(labels)))
    engine = js[js.index("function tabsSync()"):js.index("function workspaces()")]
    check("fetch(" not in tabs + engine and "setInterval" not in engine and "setTimeout" not in engine,
          "the tabs read nothing and run on no timer of their own")
    check(".tab-off { display: none !important; }" in css and "classList.toggle(\"tab-off\"" in engine,
          "a tab hides with a class, never with the panel's own `hidden`")
    check('n.classList.contains("ws-tabs")' in engine, "the tab bar is never counted as a part it switches")
    check("tabGo(\"dashboard\", \"3d\")" in js and "tabGo(\"loads\", \"3d\")" in js,
          "Show in 3D and a Space's name open the 3D tab")
    index = io.open(os.path.join(ROOT, "mcp", "companion", "static", "index.html"), encoding="utf-8").read()
    check("jumpSync" not in js and 'id="jump"' not in index and ".jump" not in css,
          "the old Jump to row is gone from every workspace (s21.21)")
    print()


def test_table_and_layout_friendly():
    """The Element Table and Sprinkler Layout made easy to use (docs/40 s21.21): the
    table's action bar, find and Discard; the layout's one card a room. Read from the
    page's own files - how it looks is a person's eye."""
    print()
    print("The Element Table's action bar, and the Sprinkler Layout's room cards")
    js = io.open(PAGE_JS, encoding="utf-8").read()
    table = js[js.index("function renderTable(t)"):js.index("async function table_()")]
    check('"t-bar"' in table and '"t-find"' in table and "Discard edits" in table,
          "the table has a bar with Find, the row count, Discard edits and Apply")
    check("/api/table/apply" in table and table.count("fetch(") == 1,
          "Find and Discard read nothing and send nothing - only Apply reaches Revit")
    fill = js[js.index("function fillDown(tbody)"):js.index("function fillDown(tbody)") + 900]
    check("tbody.rows[i].hidden) continue" in fill, "fill-down never fills a row the search hides")
    check("FIXED" in table and 'i === 0 ||' in table, "an empty Category, Family or Type column is left out; the id always shows")
    rooms = js[js.index("function layoutRooms(Y, state)"):js.index("function renderLayout(Y)")]
    check('"y-cards"' in rooms and '"y-card"' in rooms and "layoutPlan(v)" in rooms,
          "each room is one card holding its plan, its fields and its result")
    check("mine.hazard = haz.value" in rooms and "mine.ceiling_mm" in rooms and "mine.angle_deg" in rooms,
          "the room's fields keep their bindings")
    check('classList.add("off")' in js and 'classList.remove("off")' in js,
          "with no Revit the top bar says so in one line")
    print()


def test_page_frame():
    """The page's frame (docs/40 s21.16, docs/50): a sidebar of six workspaces, one shown at a
    time, desktop only. Read from the page's own files - how it LOOKS is a person's eye."""
    print()
    print("The page frame: six workspaces in a sidebar, one at a time, no phone layout")
    static = os.path.join(ROOT, "mcp", "companion", "static")
    index = io.open(os.path.join(static, "index.html"), encoding="utf-8").read()
    css = io.open(os.path.join(static, "companion.css"), encoding="utf-8").read()
    js = io.open(PAGE_JS, encoding="utf-8").read()
    shown = re.sub(r"(?s)<template\b.*?</template>", "", index)
    spaces = ["dashboard", "loads", "sprinkler", "layout", "table", "qa"]
    check(all(('<section class="ws" id="%s"' % w) in shown for w in spaces)
          and all(('data-ws="%s"' % w) in shown for w in spaces),
          "each of the six panels is a workspace with its own sidebar button")
    check(all(("body[data-ws=\"%s\"] #%s" % (w, w)) in css for w in spaces),
          "and the stylesheet shows exactly the one the page is on")
    check(re.search(r'<section class="ws" id="(sprinkler|layout)"[^>]*\bhidden\b', shown) is None
          and "$(\"sprinkler\").hidden" not in js and "$(\"layout\").hidden" not in js,
          "the Sprinkler workspaces are always there, empty until the chat opens them")
    check(all(("%s-empty" % k) in shown or ("id=\"%s-empty\"" % k) in shown for k in "dlkytq"),
          "every workspace keeps its empty state, which the page shows until it holds something")
    check("viewport" not in index, "no viewport tag: this is not a phone page")
    queries = re.findall(r"@media\s*([^{]+)\{", css)
    allowed = ("prefers-color-scheme: dark", "prefers-reduced-motion: reduce", "print", "max-width: 1279px")
    check(all(any(a in q for a in allowed) for q in queries),
          "the only media queries are dark mode, reduced motion, print and the half-screen sidebar")
    check("pointer: coarse" not in css and "touch-action" not in css and "max-width: 560px" not in css,
          "no touch or phone rule is left")
    check("function wsShow(" in js and "dispatchEvent(new Event(\"resize\"))" in js,
          "a workspace shown after its 3D view was drawn tells the view to draw again")
    check("setInterval" not in js[js.index("// ---------------------------------------------------------------- workspaces"):],
          "the workspace code runs no interval of its own, and asks Heron for nothing")


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
        test_loads_recalculate_overtaken_by_a_fresh_read()
    except Exception as why:                    # noqa: BLE001 - reported, not hidden
        check(False, "the overtaken-Recalculate checks ran to the end (they raised %r)" % (why,))
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
    try:
        test_dashboard_panel()
    except Exception as why:                    # noqa: BLE001 - reported, not hidden
        check(False, "the Model Dashboard checks ran to the end (they raised %r)" % (why,))
    try:
        test_layout_panel()
    except Exception as why:                    # noqa: BLE001 - reported, not hidden
        check(False, "the Sprinkler Layout panel checks ran to the end (they raised %r)" % (why,))
    try:
        test_checklist_panel(tmp)
    except Exception as why:                    # noqa: BLE001 - reported, not hidden
        check(False, "the QA Checklist panel checks ran to the end (they raised %r)" % (why,))
    test_page_frame()
    test_page_is_in_the_brand()
    test_workspace_tabs()
    test_table_and_layout_friendly()
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
