#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Settings and platform health. Runs without Revit.

TWO REAL BUGS ARE PINNED HERE, both found by writing these agents rather than
by reasoning about them, and both invisible to a compiler:

1. `write.enabled` was READ by HeronPermissions but never DECLARED in
   HeronConfig.Defaults - and Load() adopts a file value only for a declared
   key. So setting it to true in the config would have been silently ignored,
   and writing could never have been turned on, while the refusal told the user
   to set the very thing they had just set. The check below is the general form:
   every key the C# reads must be declared.

2. The client's response deadline was the constant 90.0 while the add-in's
   operationTimeoutSeconds was configurable at 60. Correct only by coincidence.
   Raise the add-in's to 120 - reasonable on a slow model - and the client gave
   up first, replacing "Revit started it and is still working" with "no
   answer". The check below asserts the ordering holds at every value, not just
   the default.

AND ONE SETTINGS FILE IS SOMETIMES TWO (Q-70, decided 2026-11-15). On a PC
whose Claude app keeps its own copy of the Heron folder the chat reads the copy
and a Revit the owner started reads the real file, which is the one it decides
by. So the add-in's `info` says the file it read and its Changes, Admin and
Publish switches, and revit_health states them and says when the chat's copy
differs. The last section holds the add-in's words as text, the client's
reader, the lines and the write gate's source with stand-in answers - the same
file, a differing value, a differing path, an older add-in; the real tool is
tests/test_mcp_serves.py's and the real pipe tests/test_bridge_roundtrip.py's.

    python tests/test_config_and_health.py
"""

import io
import os
import re
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_config as cfg                                   # noqa: E402
import heron_health as hp                                    # noqa: E402
import heron_tools as TOOLS                                  # noqa: E402

CONFIG_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronConfig.cs")
CS_DIRS = [os.path.join(ROOT, "platform"), os.path.join(ROOT, "revit")]

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def csharp_declared():
    """The keys HeronConfig.Defaults declares."""
    text = io.open(CONFIG_CS, encoding="utf-8").read()
    block = text[text.index("Defaults ="):]
    block = block[:block.index("};")]
    return dict(re.findall(r'\{\s*"([^"]+)"\s*,\s*"([^"]*)"\s*\}', block))


def csharp_keys_read():
    """
    Every config key the C# actually reads.

    Covers both the literal form - config.GetBool("write.enabled", false) - and
    the constant form, which is what HeronPermissions uses and what a
    literal-only scan would have missed entirely.
    """
    keys = set()
    for root in CS_DIRS:
        for dirpath, _, filenames in os.walk(root):
            for name in filenames:
                if not name.endswith(".cs"):
                    continue
                text = io.open(os.path.join(dirpath, name), encoding="utf-8").read()
                keys.update(re.findall(r'\.Get(?:Int|Bool)?\(\s*"([^"]+)"', text))
                keys.update(re.findall(
                    r'public\s+const\s+string\s+\w*Key\w*\s*=\s*"([^"]+)"', text))
    return keys


HOST_ID = re.compile(r'HERON_CLIENT_ID"?\s*[:=]\s*"([^"]+)"')

# `worktrees` BY NAME - test_docs_guard.py carries the same entry for the
# same reason, and check-docs.py skips the path. `.claude/worktrees/<name>/`
# is another session's full checkout, and the `.codex/config.toml` inside it
# pins whatever HERON_CLIENT_ID that session was started with. This suite was
# reading those as host doors in THIS tree.
#
# Measured 2026-09-22: 4 failures, all 4 inside a worktree, none here. A
# worktree is not committed, so CI never sees one and this is green there
# forever - which makes it worse, not better: a suite red locally and green
# in CI is one people learn to skip past.
SKIP_FOLDERS = (".git", "worktrees", "__pycache__", "node_modules", "build",
                "obj", "bin", "packages")


def host_configs():
    """
    Every file that STARTS the MCP server, found rather than listed.

    A `.json` or `.toml` naming the server's entry file - the last on
    heron_tools' list of the files its source is - is a host door, whoever
    added it. Returns [(path relative to root, text)], sorted.
    """
    entry = os.path.basename(TOOLS.server_files()[-1])
    found = []
    for where, folders, files in os.walk(ROOT):
        folders[:] = [f for f in folders if f not in SKIP_FOLDERS]
        for name in files:
            if not name.endswith((".json", ".toml")):
                continue
            path = os.path.join(where, name)
            try:
                with io.open(path, encoding="utf-8") as handle:
                    text = handle.read()
            except (IOError, UnicodeDecodeError):
                continue
            if entry in text:
                found.append((os.path.relpath(path, ROOT), text))
    return sorted(found)


BRIDGE_CS = os.path.join(ROOT, "revit", "Heron.Bridge", "BridgeServer.cs")
PERMISSIONS_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronPermissions.cs")

# A stand-in add-in's answers, typed here on purpose: the other side of the
# comparison, so the code under test cannot agree with itself.
REVIT_FILE = "C:\\Users\\Owner\\AppData\\Roaming\\Heron\\config\\heron.config"
OTHER_FILE = "D:\\Somewhere else\\Heron\\config\\heron.config"


def _info_block():
    """BridgeServer's `info` block, `//` comment lines left out, or ""."""
    text = io.open(BRIDGE_CS, encoding="utf-8").read()
    at = text.find('if (op == "info")')
    start = text.find("{", at) if at >= 0 else -1
    if start < 0:
        return ""
    depth = 0
    for index in range(start, len(text)):
        if text[index] == "{":
            depth += 1
        elif text[index] == "}":
            depth -= 1
            if depth == 0:
                block = text[start:index + 1]
                return "\n".join(line for line in block.split("\n")
                                 if not line.strip().startswith("//"))
    return ""


class _Answers(object):
    """A connected Revit whose `info` answers with one payload."""

    def __init__(self, payload, pid=4242, release="2024"):
        self.payload, self.pid, self.revit_version = payload, pid, release
        self.asked = []

    def request(self, op, **_kwargs):
        self.asked.append(op)
        return self.payload


def settings_said_by_revit():
    """
    Q-70: Revit says the settings file it decides by and its switches, and the
    connection check states them as Revit has them and says when the chat's
    copy differs. Each piece is asked for before it is called, so on the code
    as it stood this section FAILS check by check rather than stopping.
    """
    import heron_bridge_client as bridge

    print()
    print("Q-70 - Revit says its own settings file and switches, and the chat's copy "
          "is only compared")
    info = _info_block()
    check(bool(info), "BridgeServer answers `info` in a block of its own")
    check('Json.Str("settingsFile", settingsFile)' in info
          and "var settingsFile = HeronConfig.FilePath;" in info,
          "`info` names the settings file the add-in reads - HeronConfig.FilePath")
    check('Json.Bool("settingsFileFound", File.Exists(settingsFile))' in info,
          "and says whether that file was there")
    gate = {}
    for word, key in (("changes", "WriteEnabledKey"), ("admin", "AdminEnabledKey"),
                      ("publish", "PublishEnabledKey")):
        gate[word] = ('Json.Bool("%s", settings.GetBool(HeronPermissions.%s, false))'
                      % (word, key)) in info
    check('"\\"switches\\": " + Json.Obj(' in info and all(gate.values()),
          "and Changes, Admin and Publish in one `switches` object, each read on the "
          "gate's own key and off when unread - %s" % gate)
    permissions = io.open(PERMISSIONS_CS, encoding="utf-8").read()
    check(all(("config.GetBool(%s, false)" % key) in permissions
              for key in ("WriteEnabledKey", "PublishEnabledKey", "AdminEnabledKey")),
          "which is how the gate itself reads each of the three (HeronPermissions.Allows)")
    check(info.count("HeronConfig.Load()") == 1,
          "from ONE load of the settings, fresh on every `info`, so the three cannot "
          "disagree with each other")
    check(".Save(" not in info and "SetSwitch" not in info and ".Set(" not in info
          and "Json.Read" not in info.replace('Json.ReadString(request, "client")', ""),
          "and only read: nothing in `info` saves a setting, and nothing on the request "
          "chooses what is said")

    state = getattr(bridge, "settings_state", None)
    check(state is not None, "the client can ask a Revit for its settings")
    if state is None:
        def state(_session):                    # so the checks below RUN and FAIL
            return "(the client has no such reader)"
    not_sent = getattr(bridge, "SETTINGS_NOT_SENT", None)
    check(isinstance(not_sent, str) and not_sent == getattr(hp, "SETTINGS_NOT_SENT", None),
          "the client and heron_health have ONE word for an add-in that sent no settings")
    if not isinstance(not_sent, str):
        not_sent = "(the client has no such word)"
    full = {"ok": True, "askToKeep": True, "settingsFile": REVIT_FILE,
            "settingsFileFound": True,
            "switches": {"changes": True, "admin": False, "publish": True}}
    one = _Answers(full)
    got = state(one)
    check(got == {"file": REVIT_FILE, "found": True, "changes": True, "admin": False,
                  "publish": True},
          "a newer add-in's answer is read whole - its file, whether it was there, and "
          "the three switches: %r" % (got,))
    check(one.asked == ["info"], "asked through `info` alone, which claims nothing")
    older = state(_Answers({"ok": True, "askToKeep": False, "activeModel": "Project1"}))
    check(older == not_sent,
          "an older add-in - a good answer with neither field - is NOT SENT, never a "
          "guess: %r" % (older,))
    bad = [name for name, reply in (
        ("nothing back", None),
        ("a refusal", {"ok": False, "error": "unauthorized", "settingsFile": REVIT_FILE}),
        ("a file that is not text", dict(full, settingsFile=7)),
        ("an empty file name", dict(full, settingsFile=" ")),
        ("no word on whether it was there", dict((k, v) for k, v in full.items()
                                                 if k != "settingsFileFound")),
        ("switches that are not an object", dict(full, switches=[True])),
        ("a switch that is text", dict(full, switches={"changes": "true", "admin": False,
                                                       "publish": False})),
        ("a switch missing", dict(full, switches={"changes": True, "admin": False})),
        ("a file and no switches", dict((k, v) for k, v in full.items() if k != "switches")))
        if state(_Answers(reply)) is not None]
    check(not bad, "an answer that could not be read is None - never NOT SENT, never "
          "half-read: %s" % (bad or "none"))

    switches = getattr(cfg, "switches", None)
    check(switches is not None, "heron_config reads the chat's own four switches in one place")
    if switches is None:
        def switches(_values=None):
            return {}
    mine = switches(dict(cfg.DEFAULTS, **{"write.enabled": "on", "publish.enabled": "1",
                                          "write.askBeforeKeeping": "yes"}))
    check(mine == {"changes": True, "admin": False, "publish": True, "ask_to_keep": True},
          "Changes, Admin, Publish and Ask to Keep, by the add-in's own idea of true: %r"
          % (mine,))

    lines = getattr(hp, "settings_lines", None)
    check(lines is not None, "heron_health has the lines for what a Revit said")
    if lines is None:
        def lines(*_args):
            return []
    here_off = {"changes": False, "admin": False, "publish": False, "ask_to_keep": False}

    def said(path=REVIT_FILE, found=True, **on):
        return dict({"file": path, "found": found, "changes": False, "admin": False,
                     "publish": False}, **on)

    same = lines(said(), False, REVIT_FILE.lower(), True, here_off)
    check(len(same) == 1 and REVIT_FILE in same[0] and "Changes off" in same[0]
          and "Admin off" in same[0] and "Publish off" in same[0]
          and "decides every change by" in same[0],
          "the SAME FILE: one line - this Revit's switches and the file it decides by, "
          "and nothing else (a name in another case is the same name) - %r" % (same,))
    value = lines(said(changes=True), True, REVIT_FILE, True, here_off)
    check(len(value) == 2 and "Changes on" in value[0],
          "a DIFFERING VALUE: the switches as Revit has them - Changes on - and a second "
          "line - %r" % (value,))
    second = value[1] if len(value) == 2 else ""
    check(REVIT_FILE in second and "both are named" in second
          and "Changes (this Revit: on, this chat's: off)" in second
          and "Ask to Keep (this Revit: on, this chat's: off)" in second
          and "Admin (" not in second and "two files" in second,
          "and the second names the file, every switch that differs - Ask to Keep "
          "included - and none that does not, and says one name is two files here")
    check("decides by its own file" in second and "\n" not in second,
          "in one line, saying Revit's own file is the one that counts")
    path = lines(said(path=OTHER_FILE), False, REVIT_FILE, True, here_off)
    check(len(path) == 2 and OTHER_FILE in path[0] and REVIT_FILE in path[1]
          and OTHER_FILE in path[1] and "agree just now" in path[1],
          "a DIFFERING PATH: both files named, and that the switches agree just now - %r"
          % (path,))
    both = lines(said(path=OTHER_FILE, admin=True), False, REVIT_FILE, True, here_off)
    check(len(both) == 2 and "Admin (this Revit: on, this chat's: off)" in both[1]
          and REVIT_FILE in both[1] and OTHER_FILE in both[1],
          "a differing path and a differing value: both files and the switch")
    found = lines(said(), False, REVIT_FILE, False, here_off)
    check(len(found) == 2 and "found none" in found[1],
          "one name, and only this Revit found a file there: two files, said so")
    defaults = lines(said(found=False), False, REVIT_FILE, False, here_off)
    check(len(defaults) == 1 and "default" in defaults[0] and "found no settings file"
          in defaults[0],
          "no file on either side: each switch at its default, and nothing differs")
    unasked = lines(said(), getattr(bridge, "ASK_TO_KEEP_NOT_SENT", "not sent"),
                    REVIT_FILE, True, dict(here_off, ask_to_keep=True))
    check(len(unasked) == 1,
          "Ask to Keep is compared only when this Revit said it - %r" % (unasked,))
    old = lines(not_sent, False, REVIT_FILE, True, here_off)
    unread = lines(None, None, REVIT_FILE, True, here_off)
    check(len(old) == 1 and "does not report its settings" in old[0]
          and "older" in old[0],
          "an OLDER ADD-IN: one line, that this Revit did not report its settings - %r"
          % (old,))
    check(len(unread) == 1 and "could not read" in unread[0] and "older" not in unread[0],
          "no usable answer: one line saying that, not blamed on an older add-in")
    claims = [name for name, words in (("older", old), ("unread", unread))
              for w in (" on,", " off,", REVIT_FILE, "decides every change by")
              if any(w in x for x in words)]
    check(not claims, "and neither claims a switch or a file: %s" % (claims or "none"))
    check(not any("Ask to Keep" in x for x in same + path + old + unread),
          "none of them names Ask to Keep unless it differs - its own line says it")

    by = getattr(hp, "writing_said_by", None)
    check(by is not None, "heron_health names who said the write gate's Changes")
    named = by([one], True) if by else ""
    try:
        hot = hp.assess(live=[1], writing=True, writing_said_by=named)
        cold = hp.assess(live=[1], writing=False,
                         writing_said_by=by([one], False) if by else "")
    except TypeError as why:
        hot = cold = None
        check(False, "assess takes who said the write gate - %s" % why)
    if hot is not None:
        gate_hot = [c for c in hot.components if c.name == "write gate"][0]
        gate_cold = [c for c in cold.components if c.name == "write gate"][0]
        check(gate_hot.state == hp.WARNING and "Revit 2024, session 4242" in gate_hot.detail
              and gate_hot.detail.startswith("write.enabled is TRUE (in Revit 2024"),
              "the write gate says whose Changes it is: %r" % gate_hot.detail[:80])
        check(gate_cold.state == hp.HEALTHY and "Revit 2024, session 4242" in gate_cold.detail,
              "closed, it says whose too: %r" % gate_cold.detail)
    plain = [c for c in hp.assess(live=[1], writing=False).components
             if c.name == "write gate"][0]
    check(plain.detail == "closed - Heron cannot change the model",
          "and with no Revit's word the words are exactly as they were")


def main():
    declared = csharp_declared()

    print("The two settings tables agree")
    check(set(cfg.DEFAULTS) == set(declared),
          "Python DEFAULTS and C# Defaults declare the same keys"
          + ("" if set(cfg.DEFAULTS) == set(declared) else
             "  <- only in Python: %s ; only in C#: %s"
             % (sorted(set(cfg.DEFAULTS) - set(declared)),
                sorted(set(declared) - set(cfg.DEFAULTS)))))

    for key in sorted(set(cfg.DEFAULTS) & set(declared)):
        check(cfg.DEFAULTS[key] == declared[key],
              "%s defaults to %r on both sides" % (key, declared[key]))

    print()
    print("BUG 1 - every key the C# reads is declared, or it is silently ignored")
    read = csharp_keys_read()
    check(len(read) > 0, "found %d config key(s) being read in the C#" % len(read))
    for key in sorted(read):
        check(key in declared,
              "'%s' is read AND declared - an undeclared key is dropped by Load()" % key)

    print()
    print("BUG 2 - the client's deadline is always beyond the add-in's")
    for operation in (1, 10, 30, 60, 90, 120, 600, 3000):
        values = dict(cfg.DEFAULTS)
        values["revit.operationTimeoutSeconds"] = str(operation)
        client = cfg.response_timeout(values)
        server = cfg.operation_timeout(values)
        check(client > server,
              "operationTimeout=%s -> add-in %g, client %g (client must be higher)"
              % (operation, server, client))

    print()
    print("BUG 2, THE QUEUE - the add-in's deadline is its busy wait AND its operation wait")
    # The add-in waits up to revit.busyTimeoutSeconds for Revit to START a
    # request, counted from the enqueue, and THEN up to the operation timeout
    # for it to finish. The client's clock starts when the request is sent, so
    # it has to outlast both. Compared with the operation timeout alone, a busy
    # wait above the old 30-second margin let the client give up on a request
    # that was still going to answer - "the answer was lost" for work that then
    # completed. FRAGMENT-ISSUES section 5b, row 458.
    for operation in (10, 60, 120, 600):
        for busy in (1, 10, 30, 31, 45, 60, 120, 600):
            if busy > operation:
                continue                  # refused by problems(), checked below
            values = dict(cfg.DEFAULTS)
            values["revit.operationTimeoutSeconds"] = str(operation)
            values["revit.busyTimeoutSeconds"] = str(busy)
            longest = cfg.busy_timeout(values) + cfg.operation_timeout(values)
            for name, client in (("response", cfg.response_timeout(values)),
                                 ("fragment", cfg.fragment_timeout(values))):
                check(client > longest,
                      "busy=%s operation=%s -> add-in answers within %g, %s wait %g "
                      "(must be higher)" % (busy, operation, longest, name, client))

    print()
    print("BUG 2 AGAIN, ONE LAYER ALONG - a fragment run waits longer, still by derivation")
    # FIVE CALL SITES TYPED `response_timeout=180.0` BY HAND: the write path in
    # heron_mcp_server.py and four fragment runs in heron_bridge_client.py. A
    # fragment is compiled by Roslyn inside Revit before it runs, so a longer
    # wait is right - but a LITERAL longer wait is only beyond the add-in's
    # deadline by coincidence, which is what the block above exists to refuse.
    # At revit.operationTimeoutSeconds = 300 the add-in waited 300 and this
    # side gave up at 180, two minutes early, reporting "no answer" for a
    # write it had sent idempotent=False and therefore could never ask about
    # again. FRAGMENT-ISSUES section 5b.
    for operation in (1, 10, 60, 120, 179, 180, 181, 300, 3000):
        values = dict(cfg.DEFAULTS)
        values["revit.operationTimeoutSeconds"] = str(operation)
        fragment = cfg.fragment_timeout(values)
        server = cfg.operation_timeout(values)
        check(fragment > server,
              "operationTimeout=%s -> add-in %g, fragment wait %g (must be higher)"
              % (operation, server, fragment))
    check(cfg.fragment_timeout(dict(cfg.DEFAULTS)) == 180.0,
          "and at the defaults it is still exactly the 180 those five sites chose")
    check(cfg.fragment_timeout(dict(cfg.DEFAULTS))
          >= cfg.response_timeout(dict(cfg.DEFAULTS)),
          "never shorter than an ordinary request's wait either")

    print()
    print("And no fragment run carries the literal back")
    # THE ONLY THING THAT CATCHES THE SIXTH ONE. A helper nobody uses is not a
    # fix, and the next fragment-running call site will be written by copying
    # one of these five.
    literals = []
    for where in ["mcp/client/heron_bridge_client.py"] + [
            os.path.relpath(path, ROOT).replace(os.sep, "/")
            for path in TOOLS.server_files()]:
        body = io.open(os.path.join(ROOT, where), encoding="utf-8").read()
        for number, line in enumerate(body.split("\n"), 1):
            if re.search(r"response_timeout\s*=\s*\d", line) and "def " not in line:
                literals.append("%s:%d" % (where, number))
    # heron_bridge_client's own `release` sends a 10-second wait on a request
    # the add-in answers without touching Revit, and that one is deliberate.
    allowed = [one for one in literals if "release" in
               io.open(os.path.join(ROOT, one.split(":")[0]),
                       encoding="utf-8").read().split("\n")[int(one.split(":")[1]) - 1]]
    check(sorted(set(literals) - set(allowed)) == [],
          "no fragment run hard-codes its own deadline%s"
          % ("" if not (set(literals) - set(allowed)) else
             " <- %s" % ", ".join(sorted(set(literals) - set(allowed)))))

    print()
    print("And the default is unchanged, so nothing proven in Steps 1-5 moves")
    check(cfg.response_timeout(dict(cfg.DEFAULTS)) == 90.0,
          "with defaults the client deadline is still exactly 90.0")

    print()
    print("Settings are a closed set - a typo must not become a setting")
    tmp = os.path.join(tempfile.gettempdir(), "heron-test.config")
    io.open(tmp, "w", encoding="utf-8").write(
        u"# a comment\n"
        u"write.enabled = true\n"
        u"revit.operationTimeoutSeconds=45\n"
        u"write.enabledd = true\n"          # typo - must be ignored
        u"nonsense\n"                        # no '=' - must be ignored
        u"totally.made.up = 99\n")
    loaded = cfg.load(tmp)
    check(cfg.writing_enabled(loaded), "a declared key is read from the file")
    check(cfg.operation_timeout(loaded) == 45.0, "and so is a numeric one")
    check("write.enabledd" not in loaded, "a typo'd key is NOT adopted")
    check("totally.made.up" not in loaded, "an undeclared key is NOT adopted")
    os.remove(tmp)

    print()
    print("A missing or unreadable file is the defaults, never an error")
    check(cfg.load("/no/such/path/heron.config") == cfg.DEFAULTS,
          "a missing file gives exactly the defaults")

    print()
    print("The add-in's own idea of true")
    for raw, expected in (("true", True), ("TRUE", True), ("1", True), ("yes", True),
                          ("on", True), (" true ", True),
                          ("false", False), ("0", False), ("no", False), ("off", False),
                          ("", False), ("maybe", False)):
        values = dict(cfg.DEFAULTS)
        values["write.enabled"] = raw
        check(cfg.writing_enabled(values) is expected,
              "write.enabled=%r is %s" % (raw, expected))

    print()
    print("ONE SETTINGS FILE, TWO READERS, ONE VOCABULARY")
    # THE WORDS, COMPARED AGAINST THE C# THAT READS THE SAME FILE. The keys
    # were already compared, and the words were not - so on 2026-09-21 the
    # add-in learned `on` and `off` and a fallback for anything it does not
    # recognise (FRAGMENT-ISSUES section 5b row 8) while this side went on
    # reading `on` as false. `write.enabled = on` would then have let the
    # add-in permit a change while the MCP server's health report said
    # writing was off - two answers from one file, which is the drift the
    # key comparison above exists to prevent and could not see.
    cs = io.open(CONFIG_CS, encoding="utf-8").read()
    body = cs[cs.index("public bool GetBool"):]
    body = body[:body.index("\n        }")]
    said = set(re.findall(r'value\.Equals\("([^"]+)"', body))
    mine = set(cfg.TRUE_WORDS) | set(cfg.FALSE_WORDS)
    check(said == mine,
          "GetBool and heron_config know the same %d words%s"
          % (len(mine), "" if said == mine else
             " - C# only: %s; Python only: %s"
             % (sorted(said - mine) or "none", sorted(mine - said) or "none")))
    check("return fallback;" in body,
          "and the C# returns the FALLBACK for a word neither understands, "
          "which is what truthy() mirrors with the declared default")

    print()
    print("A value nobody can parse is the DEFAULT, in both directions")
    # write.enabled defaults to false and ui.activityBanner to true, so one
    # key proves nothing about the other - the bug being guarded against is
    # exactly an unrecognised value collapsing to false.
    for key, expected in (("write.enabled", False), ("ui.activityBanner", True)):
        for raw in ("maybe", "enabled", "2", ""):
            values = dict(cfg.DEFAULTS)
            values[key] = raw
            check(cfg.truthy(values, key) is expected,
                  "%s=%r falls back to its declared default (%s)" % (key, raw, expected))

    print()
    print("A busy timeout longer than the operation timeout is reported")
    values = dict(cfg.DEFAULTS)
    values["revit.busyTimeoutSeconds"] = "120"
    found = cfg.problems(values)
    check(len(found) == 1 and "busy" in found[0].lower(),
          "waiting longer to START than to FINISH is flagged, not silently allowed")
    check(cfg.problems(dict(cfg.DEFAULTS)) == [],
          "and the defaults report no problems at all")

    print()
    print("Health: worst state wins")
    check(hp.assess(live=[1]).state == hp.HEALTHY,
          "one Revit answering and nothing amiss -> HEALTHY, all five components green")
    check(hp.assess(live=[1], starting=[2]).state == hp.DEGRADED,
          "one answering and one not yet -> DEGRADED, because something usable is not")
    check(hp.assess(live=[1], discovery_readable=False).state == hp.FAILED,
          "an unreadable discovery folder is FAILED - Heron cannot find any Revit")
    check(hp.assess(live=[1], versions_agree=False).state == hp.DEGRADED,
          "two halves on different versions is DEGRADED")

    print()
    print("Health: no Revit connected is NOT a failure")
    idle = hp.assess()
    check(idle.state == hp.WARNING,
          "it is Heron's designed resting state, so it must not read as broken")
    check("invisible to Heron by design" in idle.describe(),
          "and the reason is said, so the word FAILED keeps its meaning for real faults")

    print()
    print("Health: writing being on is surfaced loudly")
    hot = hp.assess(live=[1], writing=True)
    gate = [c for c in hot.components if c.name == "write gate"][0]
    check(gate.state == hp.WARNING, "an open write gate is a WARNING, never HEALTHY")
    # THE CLAIM CHANGED BECAUSE THE WORLD DID, NOT TO GET TO GREEN. This
    # asserted the words "never been proven" and described them as "the path
    # has not met a real Revit" - and the path HAS met one: NEEDS-CHECKING
    # records it moving three ducts under a single undo entry. A test that
    # pins an expired sentence keeps the sentence alive, which is how the
    # same claim survived in four files (FRAGMENT-ISSUES section 5b row 26).
    # What is still worth saying is the MEASUREMENT - D3, "move them, then
    # MEASURE one" - so that is what is checked, by naming the row rather than
    # the prose around it. On 2026-10-17 D3 was measured in Revit 2024, so the
    # sentence now says how far that measurement reaches instead of that
    # nobody had taken one.
    check("D3" in gate.detail and "measured" in gate.detail.lower(),
          "and it says why - not that the path is untried, which expired, "
          "but how far its measurement (D3) reaches")
    check("never been proven" not in gate.detail
          and "never been compiled" not in gate.detail,
          "and it does not repeat a sentence a reader can disprove in a minute")

    print()
    print("Every host that starts the MCP server pins the SAME client id")
    # ONE ID PER PERSON, SET EVERYWHERE - and "everywhere" has to mean every
    # door. heron_bridge_client.py states the rule and the incident: a single
    # conversation using BOTH the MCP server and the command line was seen as
    # two chats and refused, five times in one session on 2026-09-12. That
    # comment named .mcp.json, the exported value and batch-prove.py - and
    # .codex/config.toml was already in the tree, unlisted and unpinned, and
    # nothing read either file. FRAGMENT-ISSUES row 5b-51.
    #
    # THE CONFIGS ARE FOUND, NOT LISTED, because a typed list goes stale the
    # first time somebody adds a host and it goes stale silently - which is
    # exactly how this happened.
    hosts = host_configs()
    check(len(hosts) >= 2,
          "found %d host config(s) that start the MCP server: %s"
          % (len(hosts), ", ".join(name for name, _ in hosts) or "none"))
    pinned = {}
    for name, text in hosts:
        found = HOST_ID.search(text)
        check(found is not None,
              "%s starts the server and pins HERON_CLIENT_ID" % name)
        if found:
            pinned[name] = found.group(1)
    check(len(set(pinned.values())) <= 1,
          "and every one pins the same id: %s"
          % (sorted(set(pinned.values())) or "none found"))

    print()
    print("Health: a healthy system says so briefly")
    fine = hp.assess(live=[1], writing=False)
    fine.components = [c for c in fine.components if c.state == hp.HEALTHY]
    check("Every component healthy." in fine.describe(),
          "nothing to report reads as one line, not a wall of green")

    settings_said_by_revit()

    print()
    if FAILURES:
        print("FAILED")
        for f in FAILURES:
            print("  - %s" % f)
        return 1

    print("PASSED - settings agree across both halves, and health means something.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
