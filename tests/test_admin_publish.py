#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Admin and Publish switches, on every path that can send a fragment - D-106.

    python tests/test_admin_publish.py

WHAT CHANGED, AND WHY IT NEEDS THIS MANY CHECKS
-----------------------------------------------
Until 2026-09-28 every fragment declared PUBLISH or ADMIN was refused by the
client and could not have been let through by the add-in: the only operation
that ran a changing fragment was `run_fragment_write`, declared Modify. On that
day it refused ADD_PROJECT_PARAMETER when the owner asked for two project
parameters, and he decided both levels become reachable ONLY through a switch
he turns on himself in Revit, beside Changes.

So one operation per level now exists in the add-in, the client picks it from
the fragment's OWN card, and the add-in decides by the switch. That is five
places that have to agree - the add-in's registry, its gate, the client, the
MCP tool and the ribbon - and one guard that must NOT move: a batch walking
the library alphabetically never reaches `export-*`.

WHAT THIS PROVES, AND WHAT IT CANNOT
------------------------------------
The CLIENT is driven for real, through a fake Revit that records which
operation each run was sent as - so "an ADMIN fragment goes as
run_fragment_admin" is observed, not read. The switch LOGIC is run in C# by
tests/Heron.Kernel.TestHost section 8, every combination of the three. The
add-in's gate, the MCP tool and the ribbon are read as TEXT, because running
them needs Revit and the MCP SDK - that half is NEEDS-CHECKING group BF, on
the owner's PC.
"""

import contextlib
import io
import json
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
REGISTRY_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronOperationRegistry.cs")
CONFIG_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronConfig.cs")
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")

# The client reads heron.config for its timeouts. Point it at a file that is
# not there, so the owner's own settings are neither read nor needed.
_SCRATCH = tempfile.mkdtemp(prefix="heron-switches-")
os.environ["HERON_CONFIG"] = os.path.join(_SCRATCH, "heron.config")

sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_bridge_client as CLIENT                          # noqa: E402
import heron_config as CONFIG                                  # noqa: E402
import heron_failure as FAILURE                                # noqa: E402
import heron_tools as TOOLS                                    # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %s  %s" % ("ok  " if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    try:
        return io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return ""


def card_risk(name):
    return CLIENT.fragment_risk(os.path.join(FRAGMENTS, name, "fragment.yaml"))


def library():
    """Every fragment folder with a card, in the order a batch walks them."""
    return [n for n in sorted(os.listdir(FRAGMENTS))
            if os.path.isfile(os.path.join(FRAGMENTS, n, "fragment.yaml"))]


def refusal_of(name, switched):
    """risk_refusal, asked the new way - or a failure, never a traceback.

    ASK BEFORE CALLING (heron-ship 2a): on the code as it stood there is no
    `switched` argument, and a TypeError here would replace every failure
    below with one traceback.
    """
    try:
        return CLIENT.risk_refusal(ROOT, name, switched=switched), True
    except TypeError:
        return CLIENT.risk_refusal(ROOT, name), False


class FakeBridge(object):
    """A Revit that answers every request and writes down what it was sent."""

    revit_version = "2024"
    pid = 4242

    def __init__(self):
        self.sent = []

    def request(self, op, **kwargs):
        self.sent.append(op)
        if op == "count_elements":
            return {"ok": True, "document": "Fake model", "count": 3}
        return {"ok": True, "document": "Fake model", "provides": {},
                "rolledBack": True, "verdict": "rolled back (fake)"}

    def release(self):
        pass

    def close(self):
        pass


@contextlib.contextmanager
def fake_revit():
    """The client with a pipe that is a list, and its output kept.

    `refuse_off_windows` is stood in for because this is about which
    operation is SENT, and the platform check is its own suite's business.
    `discover` hands back one fake Revit and counts how often it was asked -
    a run refused before anything was sent must never have asked at all.
    """
    bridge = FakeBridge()
    asked = {"discover": 0}

    def discover(prune=True):
        asked["discover"] += 1
        return [bridge], [], [], []

    saved = (CLIENT.refuse_off_windows, CLIENT.discover, CLIENT._prompt)
    CLIENT.refuse_off_windows = lambda: False
    CLIENT.discover = discover
    CLIENT._prompt = lambda: ""
    out = io.StringIO()
    try:
        with contextlib.redirect_stdout(out):
            yield bridge, asked, out
    finally:
        CLIENT.refuse_off_windows, CLIENT.discover, CLIENT._prompt = saved


def first_with(risk):
    for name in library():
        if card_risk(name) == risk:
            return name
    return None


def main():
    switched_ops = getattr(CLIENT, "SWITCHED_OPERATIONS", None)
    write_operation = getattr(CLIENT, "write_operation", None)
    also = getattr(TOOLS, "ALSO_REACHES", None)

    # ------------------------------------------------------------------
    print("1. The two settings exist, on both sides, and both default OFF")
    cs = read(CONFIG_CS)
    for key in ("admin.enabled", "publish.enabled"):
        check('{ "%s", "false" }' % key in cs,
              "HeronConfig declares %s, defaulting to false" % key)
        check(CONFIG.DEFAULTS.get(key) == "false",
              "and the Python mirror declares it the same")
    check(CONFIG.truthy(CONFIG.load(), "admin.enabled") is False
          and CONFIG.truthy(CONFIG.load(), "publish.enabled") is False,
          "with no settings file, both read as off")

    # ------------------------------------------------------------------
    print()
    print("2. The add-in declares ONE operation per level, and routes both")
    declared = dict(re.findall(r'\{\s*"([A-Za-z_]+)"\s*,\s*HeronRisk\.([A-Za-z]+)\s*\}',
                               read(REGISTRY_CS)))
    check(declared.get("run_fragment_publish") == "Publish",
          "run_fragment_publish is declared Publish - found %r"
          % declared.get("run_fragment_publish"))
    check(declared.get("run_fragment_admin") == "Admin",
          "run_fragment_admin is declared Admin - found %r"
          % declared.get("run_fragment_admin"))
    check(declared.get("run_fragment_write") == "Modify",
          "and run_fragment_write is still Modify - nothing about it moved")
    above = sorted(op for op, level in declared.items() if level in ("Publish", "Admin"))
    check(above == ["run_fragment_admin", "run_fragment_publish"],
          "the operations above Modify are exactly those two - found %s" % above)

    operations = read(os.path.join(ADDIN, "RevitOperations.cs"))
    routed = re.search(r'case "run_fragment_publish":\s*case "run_fragment_admin":\s*'
                       r'return RevitFragment\.Run\(app, request, true\);', operations)
    check(routed is not None,
          "both are routed to the SAME write executor run_fragment_write uses - "
          "one TransactionGroup, kept only on apply")
    gate = operations[operations.find("private static string Gate("):]
    gate = gate[:gate.find("\n        /// <summary>")]
    check(gate.count("HeronConfig.Load()") == 1,
          "the gate reads heron.config ONCE for the verdict, the words and the switch")
    for key, code in (("AdminEnabledKey", "admin_disabled"),
                      ("PublishEnabledKey", "publish_disabled")):
        check(re.search(r"case HeronPermissions\.%s:\s*return Json\.Error\(\"%s\""
                        % (key, code), gate) is not None,
              "a refusal by %s comes back as %s - the banner's one line says "
              "which switch" % (key, code))
    for code in ("admin_disabled", "publish_disabled"):
        failure = FAILURE.analyse({"ok": False, "error": code, "message": "x"},
                                  writes=True)
        check(failure is not None and failure.outcome == FAILURE.REFUSED
              and failure.touched_the_model is False,
              "%s is a REFUSAL that touched nothing - not the unknown outcome "
              "that would send the owner to check his model" % code)

    # ------------------------------------------------------------------
    print()
    print("3. The client, over every fragment, in the order a batch walks them")
    check(switched_ops is not None and write_operation is not None,
          "the client has SWITCHED_OPERATIONS and write_operation to ask")
    if switched_ops is None:
        switched_ops = {"PUBLISH": "run_fragment_publish", "ADMIN": "run_fragment_admin"}

    names = library()
    above_modify = [n for n in names if card_risk(n) in ("PUBLISH", "ADMIN")]
    check(len([n for n in above_modify if card_risk(n) == "ADMIN"]) > 0
          and len([n for n in above_modify if card_risk(n) == "PUBLISH"]) > 0,
          "the library has ADMIN and PUBLISH fragments to test against - %d in all"
          % len(above_modify))

    new_way = True
    not_refused, unnamed, let_through = [], [], []
    for name in above_modify:
        switch = "Admin" if card_risk(name) == "ADMIN" else "Publish"
        said, new_way = refusal_of(name, switched=False)
        if not said:
            not_refused.append(name)
            continue
        if ("turn on %s in Revit's Heron ribbon" % switch not in said
                or "Nothing was sent to Revit" not in said):
            unnamed.append(name)
        allowed, _ = refusal_of(name, switched=True)
        if allowed is not None:
            let_through.append(name)
    check(new_way, "risk_refusal takes `switched` - the paths that send these say so")
    check(not not_refused,
          "every PUBLISH and ADMIN fragment is refused on the default path - "
          "prove, plain fragment, validate, every batch job - not refused: %r"
          % not_refused[:5])
    check(not unnamed,
          "and every refusal NAMES THE SWITCH and says nothing was sent - "
          "unnamed: %r" % unnamed[:5])
    check(not let_through,
          "on the switched path every one goes through to the add-in, which "
          "decides by the switch - still refused: %r" % let_through[:5])

    # THE GUARD THAT MUST NOT MOVE. A batch walks the library in this order.
    #
    # TWO EXPORTS ARE REACHED, AND THAT IS RECORDED RATHER THAN HIDDEN.
    # `export-model-to-ifc` and `export-views-to-dwg` declare risk MODIFY, so
    # they travel under Changes alone and the Publish switch never sees them.
    # FRAGMENT-ISSUES section 6 found the mismatch on 2026-09-19 ("one of the
    # two is mis-declared") and left it for the owner; Q-59 puts it to him.
    # Re-declaring them here would close a policy question to make a test
    # pass. So they are NAMED: a new export-* that a batch can reach fails
    # this suite, and so does either of these once it no longer needs naming.
    KNOWN_MODIFY_EXPORTS = ["export-model-to-ifc", "export-views-to-dwg"]
    exports = [n for n in names if n.startswith("export-")]
    reached = [n for n in exports if refusal_of(n, switched=False)[0] is None]
    unexpected = [n for n in reached if n not in KNOWN_MODIFY_EXPORTS]
    check(len(exports) > 0 and not unexpected,
          "walking all %d fragments alphabetically, no export-* is reached "
          "except the %d named for Q-59 - also reached: %r"
          % (len(names), len(KNOWN_MODIFY_EXPORTS), unexpected))
    stale = [n for n in KNOWN_MODIFY_EXPORTS if card_risk(n) != "MODIFY"]
    check(not stale,
          "and each named one still declares MODIFY - when Q-59 is answered and "
          "one is re-declared, take it off the list: %r" % stale)
    print("        OPEN (Q-59): %s declare MODIFY, so a batch can reach them and the "
          "Publish switch does not guard them" % " and ".join(KNOWN_MODIFY_EXPORTS))
    first_export = exports[0] if exports else None
    check(first_export is not None
          and refusal_of(first_export, switched=False)[0] is not None
          and "Publish" in (refusal_of(first_export, switched=False)[0] or ""),
          "the FIRST export a batch meets (%s) is refused, by Publish's name"
          % first_export)
    check("PUBLISH" not in CLIENT.RUNNABLE_RISKS and "ADMIN" not in CLIENT.RUNNABLE_RISKS,
          "and RUNNABLE_RISKS - the list tools/generate-jobs.py reads - still "
          "holds neither")

    if write_operation is not None:
        wrong = []
        for name in names:
            risk = card_risk(name)
            got = write_operation(ROOT, name)
            if risk in switched_ops:
                want = switched_ops[risk]
            elif risk in CLIENT.RUNNABLE_RISKS:
                want = "run_fragment_write"
            else:
                want = None
            if got != want:
                wrong.append((name, risk, got))
        check(not wrong,
              "write_operation sends every fragment as its OWN level's operation - "
              "PUBLISH as run_fragment_publish, ADMIN as run_fragment_admin, the "
              "rest as run_fragment_write - wrong: %r" % wrong[:5])
        check(write_operation(ROOT, "no-such-fragment-here") is None,
              "and a fragment whose risk cannot be read gets no operation at all")

    # AN UNKNOWN LEVEL IS REFUSED ON EVERY PATH, and so is an unreadable one.
    plant = os.path.join(_SCRATCH, "root")
    for folder, risk in (("made-up-level", "DESTROY"), ("no-risk-line", None)):
        where = os.path.join(plant, "brain", "fragments", folder)
        os.makedirs(where)
        with io.open(os.path.join(where, "fragment.yaml"), "w", encoding="utf-8") as fh:
            fh.write(u"id: FRG-TEST-001\n" + (u"risk: %s\n" % risk if risk else u""))
    for folder in ("made-up-level", "no-risk-line"):
        try:
            said = CLIENT.risk_refusal(plant, folder, switched=True)
        except TypeError:
            said = CLIENT.risk_refusal(plant, folder)
        check(said is not None,
              "%s is refused even on the switched path - a level nobody can "
              "read or nobody knows is never sent" % folder)

    if also is not None:
        pairs = dict((TOOLS.NAMES[level], op)
                     for level, op in also.get("revit_change", {}).items())
        check(pairs == switched_ops,
              "heron_tools.ALSO_REACHES and the client's SWITCHED_OPERATIONS name "
              "the same operation for each level - %r and %r" % (pairs, switched_ops))
        check(all(declared.get(op) == level.capitalize() for level, op in pairs.items()),
              "and the add-in declares each at that level")
    else:
        check(False, "heron_tools declares ALSO_REACHES for revit_change")

    # ------------------------------------------------------------------
    print()
    print("4. The three commands, driven through a fake Revit")
    admin_one = "add-project-parameter" if card_risk("add-project-parameter") == "ADMIN" \
        else first_with("ADMIN")
    publish_one = "export-view-image" if card_risk("export-view-image") == "PUBLISH" \
        else first_with("PUBLISH")
    modify_one = "set-view-scale" if card_risk("set-view-scale") == "MODIFY" \
        else first_with("MODIFY")
    read_one = "list-levels" if card_risk("list-levels") == "READ" else first_with("READ")

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_prove([read_one, admin_one])
    check(code == 2 and asked["discover"] == 0 and not bridge.sent,
          "prove with an ADMIN fragment in the list refuses before Revit is asked "
          "anything (exit %s, %d request(s))" % (code, len(bridge.sent)))

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_fragment(admin_one)
    check(code == 2 and not bridge.sent and "turn on Admin" in out.getvalue(),
          "`fragment %s` with no --write is refused, and says turn on Admin"
          % admin_one)

    for name, want in ((admin_one, "run_fragment_admin"),
                       (publish_one, "run_fragment_publish"),
                       (modify_one, "run_fragment_write")):
        with fake_revit() as (bridge, asked, out):
            code = CLIENT.cmd_fragment(name, writing=True)
        check(code == 0 and bridge.sent == [want],
              "`fragment %s --write` (%s) is sent as %s - sent %r"
              % (name, card_risk(name), want, bridge.sent))

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_fragment(read_one)
    check(code == 0 and bridge.sent == ["run_fragment_read"],
          "and a plain read still goes as run_fragment_read - sent %r" % bridge.sent)

    record = os.path.join(_SCRATCH, "run.json")

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_validate(publish_one, out=record, negative_in="Other")
    check(code == 2 and asked["discover"] == 0,
          "`validate %s` with no flags is refused before Revit is asked" % publish_one)

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_validate(publish_one, out=record, negative_in="Other",
                                   allow_publish=True)
    check(code == 2 and asked["discover"] == 0,
          "--allow-publish alone is refused too: the READ path would have exported "
          "with no switch at all, and that route is closed")
    check("--allow-publish --write" in out.getvalue(),
          "and the refusal says what to add")

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_validate(publish_one, out=record, negative_in="Other",
                                   allow_publish=True, writing=True)
    runs = [op for op in bridge.sent if op != "count_elements"]
    check(code == 0 and runs == ["run_fragment_publish", "run_fragment_publish"],
          "--allow-publish --write sends BOTH phases as run_fragment_publish, so "
          "the add-in asks the Publish switch - sent %r" % runs)

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_validate(modify_one, out=record, negative_in="Other",
                                   writing=True, setup=[admin_one])
    check(code == 2 and asked["discover"] == 0 and not bridge.sent,
          "a setup step declared ADMIN is refused before anything is sent - it "
          "would have travelled inside a Modify request, past the Admin switch")

    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_validate(read_one, out=record, negative_in="Other")
    runs = [op for op in bridge.sent if op != "count_elements"]
    check(code == 0 and runs == ["run_fragment_read", "run_fragment_read"],
          "and an ordinary read proof is exactly what it was - sent %r" % runs)

    # ------------------------------------------------------------------
    print()
    print("5. revit_change sends a fragment as the operation its own card names")
    text = read(SERVER)
    start = text.find("\ndef revit_change(")
    body = text[start:] if start > 0 else ""
    end = body.find("\n@server.tool()")
    body = body[:end] if end > 0 else body
    check(start > 0, "revit_change is still in the server")
    refused = body.find("risk_refusal(root, folder, switched=True)")
    picked = body.find("write_operation(root, folder)")
    bound = body.find("binding.resolve(")
    check(refused > 0 and picked > 0 and bound > 0,
          "it asks the client's risk_refusal (switched) and write_operation")
    check(refused > 0 and picked > 0 and bound > 0 and refused < bound and picked < bound,
          "both BEFORE a session is bound - a refusal touches nothing")
    check("session.request(operation," in body
          and 'session.request("run_fragment_write"' not in body,
          "and the request goes as that operation, never a fixed run_fragment_write")
    check("SWITCHED_OPERATIONS" not in text and "def write_operation" not in text,
          "the server carries no copy of the client's routing")
    doc = body[:body.find('"""', body.find('"""') + 3)]
    check("Admin" in doc and "Publish" in doc and "Article 7" in doc
          and "Article 10" in doc,
          "its description tells the chat about both switches, and that Articles "
          "7 and 10 still stand")

    # ------------------------------------------------------------------
    print()
    print("6. The ribbon: two switches beside Changes, drawn from the setting")
    app = read(os.path.join(ADDIN, "HeronApplication.cs"))
    changes_at = app.find('"HeronWriteToggle"')
    admin_at = app.find('"HeronAdminToggle"')
    publish_at = app.find('"HeronPublishToggle"')
    check(changes_at > 0 and admin_at > changes_at and publish_at > admin_at,
          "BuildRibbon adds Admin and then Publish, after Changes")
    check("typeof(AdminToggleCommand)" in app and "typeof(PublishToggleCommand)" in app,
          "each button runs its own command")
    check("SetAdminIcon(HeronPermissions.AdminEnabled());" in app
          and "SetPublishIcon(HeronPermissions.PublishEnabled());" in app,
          "and each starts from what heron.config SAYS, so the picture is true "
          "when Revit starts")
    for icon in ("AdminLocked.png", "AdminUnlocked.png",
                 "PublishLocked.png", "PublishUnlocked.png"):
        path = os.path.join(ADDIN, "Resources", icon)
        with io.open(path, "rb") if os.path.isfile(path) else io.BytesIO(b"") as fh:
            head = fh.read(8)
        check('"%s"' % icon in app and head == b"\x89PNG\r\n\x1a\n",
              "%s is named by the ribbon and is a PNG in Resources" % icon)
    pictures = set()
    for icon in ("AdminLocked.png", "AdminUnlocked.png", "PublishLocked.png",
                 "PublishUnlocked.png", "WriteLocked.png", "WriteUnlocked.png"):
        path = os.path.join(ADDIN, "Resources", icon)
        if os.path.isfile(path):
            with io.open(path, "rb") as fh:
                pictures.add(fh.read())
    check(len(pictures) == 6,
          "and all six padlocks are different pictures - a state that looks like "
          "another is no state at all")

    commands = read(os.path.join(ADDIN, "Commands.cs"))
    toggle = commands[commands.find("internal static class HeronSwitchToggle"):]
    toggle = toggle[:toggle.find("private static bool AskPlainly")]
    check("if (!enabled)" in toggle
          and toggle.find("HeronSwitchWindow.Ask(") > toggle.find("if (!enabled)")
          and toggle.find("var now = write(!enabled);") > toggle.find("if (!enabled)"),
          "a switch ASKS only on the way on, and never on the way off")
    check("if (!answer.HasValue) answer = AskPlainly(asked);" in toggle,
          "a window that could not be drawn is asked again plainly, never taken "
          "as an answer")
    check("ask.DefaultButton = TaskDialogResult.CommandLink2;" in commands,
          "and the plain question's Enter lands on Leave it off")
    window = read(os.path.join(ADDIN, "HeronSwitchWindow.cs"))
    check(window and "Autodesk" not in window,
          "the question window touches no Revit API")

    print()
    shutil.rmtree(_SCRATCH, ignore_errors=True)
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for one in FAILURES:
            print("  - %s" % one)
        return 1
    print("PASSED - Admin and Publish reach Revit only as their own operations, the "
          "add-in decides by the owner's switch, and no batch reaches an export "
          "declared PUBLISH. Two export-* declared MODIFY wait on Q-59.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
