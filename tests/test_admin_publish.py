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

SECTION 7 IS ARTICLE 11 ON THE SAME PATH (D-117). The fake Revit also writes
down what each run carried, so "no DRAFT is sent to be KEPT" is observed over
every write fragment in the library, and so is the other half: a DRAFT run
WITHOUT --apply is still sent, rolled back, because that is how one is proved.
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
    """A Revit that answers every request and writes down what it was sent -
    the operation in `sent`, and what the run carried in `args`, so a check
    can say WHICH fragment went and whether it went to be kept."""

    revit_version = "2024"
    pid = 4242

    def __init__(self):
        self.sent = []
        self.args = []

    def request(self, op, **kwargs):
        self.sent.append(op)
        self.args.append(dict(kwargs.get("op_args") or {}))
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


def current(found):
    """Whether proof_status's answer is a proof that still stands."""
    return (found is not None and found.get("status") in ("PROVEN", "PRODUCTION")
            and not found.get("stale"))


def article_11(names):
    """Section 7 - a fragment no current proof stands behind is never sent to
    be KEPT (Constitution Article 11, D-117)."""
    print()
    print("7. Article 11: what no current proof stands behind is never sent to be kept")

    # ASK BEFORE CALLING (heron-ship 2a). On the code as it stood neither
    # exists, and the stand-ins make every check below FAIL rather than crash.
    proof_of = getattr(CLIENT, "proof_of", None)
    refusal = getattr(CLIENT, "unproven_refusal", None)
    check(proof_of is not None and refusal is not None,
          "the client has proof_of and unproven_refusal to ask")
    if proof_of is None:
        proof_of = lambda name: None                                   # noqa: E731
    if refusal is None:
        refusal = lambda what, found: None                             # noqa: E731

    for found, why in (({"status": "DRAFT", "stale": False}, "a DRAFT"),
                       ({"status": "PROVEN", "stale": True, "date": "2026-09-01"},
                        "a PROVEN one whose code moved after its proof"),
                       ({"status": None, "stale": False}, "one with no status at all"),
                       (None, "one whose status could not be read")):
        said = refusal("SOME_CAPABILITY", found) or ""
        check("SOME_CAPABILITY" in said and "Nothing was sent to Revit" in said,
              "%s is refused, by name, saying nothing was sent" % why)
    stale = refusal("SOME_CAPABILITY", {"status": "PROVEN", "stale": True,
                                        "date": "2026-09-01"}) or ""
    check("D-30" in stale and "2026-09-01" in stale,
          "and a stale one names D-30 and the date of the proof that no longer holds")
    check(refusal("X", {"status": "PROVEN", "stale": False}) is None
          and refusal("X", {"status": "PRODUCTION", "stale": False}) is None,
          "a current PROVEN or PRODUCTION proof is let through")

    # OVER THE WHOLE LIBRARY, through the fake Revit: every write fragment,
    # asked for with --write --apply, and what reached Revit to be KEPT read
    # back off the fake's own record.
    writers = [n for n in names if card_risk(n) not in ("READ", "ANALYZE")]
    standing = set(n for n in writers if current(proof_of(n)))
    with fake_revit() as (bridge, asked, out):
        for name in writers:
            CLIENT.cmd_fragment(name, writing=True, apply_it=True)
    kept = [a.get("name") for a in bridge.args if a.get("apply") == "true"]
    check(len(standing) > 0 and len(standing) < len(writers),
          "the library has write fragments on both sides - %d of %d with a proof "
          "that stands (derived, not typed)" % (len(standing), len(writers)))
    check(set(kept) == standing and len(kept) == len(standing),
          "with --apply, exactly those were sent to be kept, each once - sent "
          "without a standing proof: %r, standing and not sent: %r"
          % (sorted(set(kept) - standing)[:5], sorted(standing - set(kept))[:5]))
    drafts = [n for n in kept if (proof_of(n) or {}).get("status") == "DRAFT"]
    check(not drafts, "and not one DRAFT reached Revit to be kept - sent: %r" % drafts[:5])

    pure = [n for n in writers if card_risk(n) == "MODIFY"
            and (proof_of(n) or {}).get("status") == "DRAFT"
            and not (proof_of(n) or {}).get("stale")]
    draft_one = pure[0] if pure else None
    check(draft_one is not None, "the library has a never-proved MODIFY DRAFT to try")
    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_fragment(draft_one or "", writing=True, apply_it=True)
    check(code == 2 and asked["discover"] == 0 and not bridge.sent,
          "`fragment %s --write --apply` is refused before Revit is asked anything"
          % draft_one)
    check("DRAFT" in out.getvalue() and "Article 11" in out.getvalue()
          and "Without --apply it still runs" in out.getvalue(),
          "and says it is DRAFT, under Article 11, and that a rolled-back run is "
          "still open to it")
    with fake_revit() as (bridge, asked, out):
        code = CLIENT.cmd_fragment(draft_one or "", writing=True)
    check(code == 0 and bridge.sent == ["run_fragment_write"]
          and not any(a.get("apply") for a in bridge.args),
          "without --apply the same DRAFT is still sent, to be rolled back - which "
          "is how a DRAFT is proved at all (D-55)")

    proven_one = sorted(standing)[0] if standing else None
    for made, why, word in ((lambda found: dict(found or {}, stale=True),
                             "with its proof made stale", "D-30"),
                            (lambda found: None,
                             "whose status cannot be read", "could not be read")):
        saved = CLIENT.proof_of if hasattr(CLIENT, "proof_of") else None
        CLIENT.proof_of = lambda name, made=made, saved=saved: made(
            saved(name) if saved else None)
        try:
            with fake_revit() as (bridge, asked, out):
                code = CLIENT.cmd_fragment(proven_one or "", writing=True, apply_it=True)
        finally:
            CLIENT.proof_of = saved
        check(code == 2 and not bridge.sent and word in out.getvalue(),
              "a PROVEN fragment (%s) %s is refused, saying so" % (proven_one, why))


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
    # NO EXCEPTION IS NAMED ANY MORE. `export-model-to-ifc` and
    # `export-views-to-dwg` declared MODIFY until the owner answered Q-59 on
    # 2026-09-28 (D-107): both are PUBLISH, so the Publish switch guards every
    # export and an export-* that a batch can reach fails this suite.
    exports = [n for n in names if n.startswith("export-")]
    reached = [n for n in exports if refusal_of(n, switched=False)[0] is None]
    check(len(exports) > 0 and not reached,
          "walking all %d fragments alphabetically, no export-* is reached - "
          "reached: %r" % (len(names), reached))
    check(not [n for n in exports if card_risk(n) != "PUBLISH"],
          "and every export-* declares PUBLISH, so the Publish switch guards "
          "each one (D-107) - not PUBLISH: %r"
          % [(n, card_risk(n)) for n in exports if card_risk(n) != "PUBLISH"])
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

    article_11(names)

    # ------------------------------------------------------------------
    print()
    print("8. A picture for the chat is not an export, and does not move one (D-123)")
    # capture_view writes a PNG too, and could be mistaken for a way round
    # the Publish switch. What makes it not an export is WHERE the file goes
    # and what becomes of it - so those are read here, and the export that
    # already exists is held exactly where D-107 put it.
    check(card_risk("export-view-image") == "PUBLISH",
          "export-view-image - the export that writes views to a folder the "
          "caller names - is still PUBLISH")
    check(declared.get("capture_view") == "Analyze"
          and TOOLS.TOOLS.get("revit_view_image", (None, None))[1] == "capture_view"
          and TOOLS.risk_of("revit_view_image") == TOOLS.ANALYZE
          if "revit_view_image" in TOOLS.TOOLS else False,
          "capture_view is Analyze in the add-in and revit_view_image ANALYZE here")
    # export-* MEANS PUBLISH in this repository (D-107), so no tool or
    # operation may wear the word at the front unless it is one - renaming
    # capture_view to export_view would fail here.
    named = [n for n in list(TOOLS.TOOLS) + list(declared) if n.startswith("export")]
    check(not named and not os.path.isdir(os.path.join(FRAGMENTS, "capture-view")),
          "no MCP tool or add-in operation is named export* - found %s - and the "
          "capture is no fragment a batch could walk" % (named or "none"))
    capture = read(os.path.join(ADDIN, "RevitCapture.cs"))
    check(re.search(r'var folder = Path\.Combine\(HeronPaths\.Captures,', capture) is not None
          and "options.FilePath = Path.Combine(folder," in capture,
          "the picture is written only inside HeronPaths.Captures - Heron's own "
          "Derived folder")
    check(not re.search(r'ReadString\(request,\s*"(folder|path|filePath|destination)"\)',
                        capture),
          "and no folder or path is taken from the request - a caller cannot point "
          "it anywhere shared")
    check("Directory.Delete(folder, true)" in capture
          and capture.find("deleted = DeleteFolder(folder);")
          < capture.find('Json.Str("png", Convert.ToBase64String(picture))'),
          "the folder is deleted before the answer is built, and the picture "
          "travels in the reply")

    print()
    shutil.rmtree(_SCRATCH, ignore_errors=True)
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for one in FAILURES:
            print("  - %s" % one)
        return 1
    print("PASSED - Admin and Publish reach Revit only as their own operations, the "
          "add-in decides by the owner's switch, no batch reaches any export-*, and "
          "nothing without a standing proof is sent to be kept.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
