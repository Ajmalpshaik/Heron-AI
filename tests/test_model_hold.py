#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   7
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
One model, one chat at a time - D-141. Runs without Revit.

    python tests/test_model_hold.py

WHY THIS EXISTS
---------------
The owner, 2026-10-18: "One chat can access multiple models. The same model
cannot be accessed by multiple chats at the same time. After one finishes, then
the next goes; then the first can come back." Until then every chat on his PC
sent the same `client` id ("ajmal-pc", set in .mcp.json for the D-22 lease), so
Revit could not tell his chats apart: two of them on one model shared the
carried chain and the D-138 Keep grant, and nothing stopped them working one
model at once.

THREE HALVES, BECAUSE ONE OF THEM CANNOT BE RUN HERE
-----------------------------------------------------
  THE ENVELOPE  the client puts a conversation id, made once per process, on
                every request, in a key no operation argument may set. Run
                here in Python, against the real Bridge.request.
  THE RULE      who may claim a model, who is refused and in what words, three
                quiet minutes, "done", Free this model, a closing model, an
                older client - HeronModelHold.cs and HeronConversation.cs, which
                name no Autodesk type and are linked by source into
                tests/Heron.ModelHold.TestHost. Built and RUN here.
  THE WIRING    where the add-in claims (after the gate, before anything is
                routed), what frees a hold (release_models, DocumentClosing,
                the ribbon, a disconnect), the chain and grant keyed on the
                conversation, the MCP tool and the failure's class. That half
                calls the Revit API and is read as TEXT - a weak test of
                behaviour and a strong one of a missing line or a line in the
                wrong place.

None of it proves Revit then does what it is told. That is the PC's, and
NEEDS-CHECKING Group HK says what it owes.

MEASURED AGAINST THE CODE AS IT STOOD: with the files from before D-141 the
envelope, wiring and registry sections fail and the host does not build, and
this suite exits 1 rather than raising.

IT EXITS 3 WHEN .NET IS ABSENT AND EVERYTHING ELSE PASSED, and that is NOT a
pass: the rule was not run.
"""

import io
import json
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "tests"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    sys.stderr.reconfigure(encoding="utf-8", errors="replace")

# The same readers test_keep_question.py uses on the same files - comments out,
# string contents blanked, a braces-balanced body.
import test_failure_note as NOTE_SUITE                            # noqa: E402
import heron_bridge_client as BRIDGE                               # noqa: E402
import heron_config as CFG                                         # noqa: E402
import heron_failure as FAIL                                       # noqa: E402
import heron_tools as TOOLS                                        # noqa: E402

ADDIN = os.path.join(ROOT, "revit", "Heron.Revit.Addin")
HOLD = os.path.join(ADDIN, "HeronModelHold.cs")
CONVERSATION = os.path.join(ADDIN, "HeronConversation.cs")
OPERATIONS = os.path.join(ADDIN, "RevitOperations.cs")
EXECUTOR = os.path.join(ADDIN, "RevitFragment.cs")
GRANT = os.path.join(ADDIN, "HeronKeepGrant.cs")
APPLICATION = os.path.join(ADDIN, "HeronApplication.cs")
COMMANDS = os.path.join(ADDIN, "Commands.cs")
DISPATCHER = os.path.join(ADDIN, "RevitDispatcher.cs")
ICON = os.path.join(ADDIN, "Resources", "FreeModel.png")
REGISTRY = os.path.join(ROOT, "platform", "Heron.Core", "HeronOperationRegistry.cs")
CONFIG_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronConfig.cs")
LEASE = os.path.join(ROOT, "platform", "Heron.Core", "HeronLease.cs")
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
BATCH = os.path.join(ROOT, "tools", "batch-prove.py")
HOST = os.path.join(ROOT, "tests", "Heron.ModelHold.TestHost")
COULD_NOT_RUN = 3

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    try:
        return io.open(path, encoding="utf-8").read()
    except (IOError, OSError):
        return ""


def code(path):
    return NOTE_SUITE.code_only(read(path))


def before(text, first, then):
    """`first` appears, `then` appears, and the first comes first."""
    a, b = text.find(first), text.find(then)
    return 0 <= a < b


class _Pipe(object):
    """A pipe handle that keeps what was written and answers one line."""

    def __init__(self):
        self.written = b""
        self._answer = b'{"ok": true}\n'

    def write(self, data):
        self.written += data

    def read(self, size):
        out, self._answer = self._answer[:size], self._answer[size:]
        return out

    def close(self):
        pass


def envelope():
    print("\n1. every request names its conversation, and no argument can")
    conversation = getattr(BRIDGE, "CONVERSATION_ID", None)
    check(isinstance(conversation, str) and len(conversation) >= 16,
          "the client makes a conversation id - CONVERSATION_ID")
    if not isinstance(conversation, str):
        conversation = ""
    check(conversation != BRIDGE.CLIENT_ID,
          "and it is not the client id, which names the person (D-22's lease)")
    check("conversation" in BRIDGE.RESERVED_KEYS
          and set(("op", "token", "client")) <= set(BRIDGE.RESERVED_KEYS),
          "`conversation` is reserved beside op, token and client")

    bridge = BRIDGE.Bridge({"pid": 1, "pipeName": "heron-test", "token": "t0ken"}, "x.json")
    pipe = _Pipe()
    bridge._handle = pipe
    reply = bridge.request("count_elements", op_args={"category": "ducts"})
    sent = {}
    try:
        sent = json.loads(pipe.written.decode("utf-8").strip())
    except ValueError:
        pass
    check(reply == {"ok": True}, "a request through the real Bridge.request is answered")
    check(sent.get("conversation") == conversation and conversation,
          "and it carried this process's conversation")
    check(sent.get("client") == BRIDGE.CLIENT_ID and sent.get("token") == "t0ken",
          "beside the client id and the token, unchanged")

    try:
        bridge._handle = _Pipe()
        bridge.request("count_elements", op_args={"conversation": "someone-else"})
        check(False, "an argument named `conversation` is refused before anything is sent")
    except ValueError as refused:
        check("conversation" in str(refused),
              "an argument named `conversation` is refused before anything is sent")

    print("\n2. one per process: each chat, and each command-line run, is its own")
    env = dict(os.environ)
    env.pop("HERON_CONVERSATION_ID", None)
    env["PYTHONPATH"] = os.path.join(ROOT, "mcp", "client")
    ask = [sys.executable, "-c", "import heron_bridge_client as b; print(b.CONVERSATION_ID)"]
    first = subprocess.run(ask, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                           encoding="utf-8").stdout.strip()
    second = subprocess.run(ask, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                            encoding="utf-8").stdout.strip()
    check(first and second and first != second and first != conversation,
          "two processes make two conversations - two chats are two chats")
    env["HERON_CLIENT_ID"] = "ajmal-pc"
    shared = subprocess.run(ask, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                            encoding="utf-8").stdout.strip()
    check(shared not in ("", "ajmal-pc") and shared != first,
          "even with .mcp.json's one HERON_CLIENT_ID for every chat")
    env["HERON_CONVERSATION_ID"] = "batch-of-fourteen"
    pinned = subprocess.run(ask, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                            encoding="utf-8").stdout.strip()
    check(pinned == "batch-of-fourteen",
          "HERON_CONVERSATION_ID makes several processes one conversation - batch-prove's jobs")

    for name in (".mcp.json", os.path.join(".codex", "config.toml")):
        text = read(os.path.join(ROOT, name))
        check(text and "HERON_CONVERSATION_ID" not in text,
              "%s does not set HERON_CONVERSATION_ID - every chat would be one again" % name)

    print("\n2a. a command line hands back what it held when it ends")
    hand_back = getattr(BRIDGE, "hand_back_models", None)
    asked = getattr(BRIDGE, "_ASKED", None)
    check(callable(hand_back) and isinstance(asked, dict),
          "the client keeps which Revits it asked, and can hand back to them")
    if callable(hand_back) and isinstance(asked, dict):
        asked.clear()
        one = BRIDGE.Bridge({"pid": 7, "pipeName": "heron-7", "token": "t"}, "x.json")
        one._handle = _Pipe()
        one.request("ping")
        check(7 not in asked, "a ping holds no model, so it is not remembered")
        one._handle = _Pipe()
        one.request("list_levels")
        check(asked.get(7) is one, "a request about a model is remembered")
        saved = os.environ.pop("HERON_CONVERSATION_ID", None)
        try:
            os.environ["HERON_CONVERSATION_ID"] = "borrowed"
            sent = _Pipe()
            one._handle = sent
            hand_back()
            check(sent.written == b"" and 7 in asked,
                  "a borrowed conversation hands back nothing - its owner does")
            del os.environ["HERON_CONVERSATION_ID"]
            sent = _Pipe()
            one._handle = sent
            one._connect = lambda timeout: sent
            hand_back()
            said = {}
            try:
                said = json.loads(sent.written.decode("utf-8").strip())
            except ValueError:
                pass
            check(said.get("op") == "release_models" and said.get("conversation") == conversation,
                  "its own conversation hands back with release_models, for itself alone")
            check(not asked, "and forgets the Revits it handed back to")
        finally:
            os.environ.pop("HERON_CONVERSATION_ID", None)
            if saved is not None:
                os.environ["HERON_CONVERSATION_ID"] = saved
    main_block = read(os.path.join(ROOT, "mcp", "client", "heron_bridge_client.py"))
    main_block = main_block[main_block.rfind('if __name__ == "__main__":'):]
    check(re.search(r"try:\s*_code = main\(sys\.argv\)\s*finally:\s*hand_back_models\(\)",
                    main_block) is not None,
          "and it does so however the command ends")

    batch = read(BATCH)
    check('env.setdefault("HERON_CONVERSATION_ID", BRIDGE.CONVERSATION_ID)' in batch,
          "batch-prove makes its jobs one conversation of its own")
    check(re.search(r'free_models\(env\["HERON_CONVERSATION_ID"\]', batch) is not None
          and '"release_models"' in batch,
          "and hands its models back when the batch ends, rather than holding them three minutes")


def failure():
    print("\n3. a refusal for a held model is NEVER RAN - nothing reached the model")
    for writes in (False, True):
        got = FAIL.analyse({"ok": False, "error": "model_in_use",
                            "message": "Model 'Tower' is in use by another chat."}, writes=writes)
        check(got is not None and got.outcome == FAIL.NEVER_RAN
              and got.touched_the_model is False and not got.may_retry,
              "model_in_use on a %s: never ran, nothing touched, not retried on its own"
              % ("change" if writes else "read"))
    check(got is not None and got.message == "Model 'Tower' is in use by another chat.",
          "and the add-in's own sentence is what the chat says")


def registry():
    print("\n4. the hand-back is a declared READ operation, and revit_release sends it")
    registry_cs = read(REGISTRY)
    check(re.search(r'\{\s*"release_models"\s*,\s*HeronRisk\.Read\s*\}', registry_cs) is not None,
          "release_models is declared Read in the add-in's registry")
    check(TOOLS.TOOLS.get("revit_release") == (TOOLS.READ, "release_models"),
          "revit_release is READ and calls release_models - found %r"
          % (TOOLS.TOOLS.get("revit_release"),))
    server = read(SERVER)
    body = server[server.find("def revit_release("):]
    body = body[:body.find("\n@server.tool()")] if "\n@server.tool()" in body else body
    check("def revit_release(" in server and 'session.request("release_models"' in body,
          "the MCP server serves revit_release and sends release_models")
    check('"all", "this"' in body and "pinned.title" in body,
          "it frees all of this chat's models, or the one it is working on")
    check("conversation" not in re.sub(r"#.*", "", body),
          "and it sends no conversation of its own - the envelope does, so it cannot "
          "name another chat")


def wiring():
    operations = code(OPERATIONS)
    raw_operations = read(OPERATIONS)

    print("\n5. the rule names no Autodesk type, so a host can run it without Revit")
    for path in (HOLD, CONVERSATION):
        text = code(path)
        check(bool(text), "%s is there" % os.path.basename(path))
        check(bool(text) and "Autodesk" not in text, "and names no Autodesk type")
    check("heron.config" not in code(HOLD) and "HeronConfig" not in code(HOLD),
          "holds live in memory only - nothing in the rule reads or writes heron.config")

    print("\n6. the add-in claims after the gate and before anything is routed")
    run = NOTE_SUITE.body_of(operations, "public static string Run(UIApplication app, string request)")
    check(bool(run), "RevitOperations.Run was found")
    check(before(run, "Gate(op)", "HoldModel(app, request, out holdKey)"),
          "the gate first - a request the switches refuse claims nothing")
    check(before(run, "HoldModel(app, request, out holdKey)", "Route(app, request, op)"),
          "the hold before routing - a refused request reaches no operation")
    check("switch (op)" not in run and "RevitFragment.Run(" not in run,
          "and Run itself routes nothing - every operation is behind the hold")
    raw_run = NOTE_SUITE.body_of(raw_operations,
                                 "public static string Run(UIApplication app, string request)")
    check(before(raw_run, 'op == "release_models"', "HoldModel(")
          and "return ReleaseModels(app, request)" in run,
          "handing back is answered before the hold, so it never claims what it frees")
    check(re.search(r"finally\s*\{[^}]*HeronModelHold\.Session\.Touch\(", run) is not None,
          "the quiet time restarts when the request ENDS, however it returns")
    hold_model = NOTE_SUITE.body_of(operations, "private static string HoldModel(")
    check("HeronModelHold.Session.Claim(" in hold_model
          and "RevitOperations.DocumentKey" not in hold_model and "DocumentKey(doc)" in hold_model,
          "the model is claimed by its key - DocumentKey, never its title")
    check(re.search(r'Json\.Error\("model_in_use"', raw_operations) is not None,
          "a refusal is model_in_use, carrying the rule's own sentence")
    check("Json.ReadString(request, HeronConversation.EnvelopeField)" in operations,
          "the chat is the request's conversation, not its client")
    target = NOTE_SUITE.body_of(raw_operations, "internal static Document HoldTarget(")
    check(before(target, '"documentPath"', "candidate.IsLinked")
          and "return ActiveDocument(app);" in target and "matches == 1" in target,
          "the model held is the one the request runs against: the path, a title only one "
          "open model has, else the model in front - and never a link")
    check(re.search(r'string\.Equals\(Json\.ReadString\(request, "claim"\), "no"',
                    raw_operations) is not None
          and "HeronModelHold.Session.Check(" in hold_model,
          "a request that only looks - \"claim\": \"no\" - is checked, never claims")
    health = read(SERVER)
    health = health[health.find("def revit_health("):]
    health = health[:health.find("\n@server.tool()")]
    check(re.search(r'request\("count_elements",\s*op_args=\{"claim": "no"\}\)', health)
          is not None,
          "revit_health's count looks without claiming - asking whether Heron works never "
          "takes another chat's model")
    release = NOTE_SUITE.body_of(operations, "private static string ReleaseModels(")
    check("HeronModelHold.Session.Release(conversation" in release
          and "Free(" not in release,
          "release_models frees only the asking chat's holds - never another chat's")

    print("\n7. what else frees a hold")
    application = code(APPLICATION)
    closing = NOTE_SUITE.body_of(application, "private static void OnDocumentClosing(")
    check("HeronModelHold.Session.Free(" in closing and "RevitOperations.DocumentKey(closing)" in closing,
          "a closing model frees its hold, whoever held it")
    commands = code(COMMANDS)
    toggle = NOTE_SUITE.body_of(commands, "internal static void Toggle(string where)")
    check(before(toggle, "bridge.Stop()", "HeronModelHold.Session.Clear()"),
          "disconnecting the bridge from the ribbon clears every hold")

    print("\n8. Free this model on the Heron ribbon")
    raw_app = read(APPLICATION)
    check('typeof(FreeModelCommand).FullName' in raw_app
          and '"Free this\\nmodel"' in raw_app,
          "the ribbon has the button, labelled Free this model, bound by typeof")
    check(re.search(r'freeModel\.ToolTip\s*=\s*"[^"]+\."', raw_app) is not None
          and "freeModel.LongDescription" in raw_app,
          "with a one-sentence tooltip and a longer description")
    check("AvailabilityClassName" not in raw_app,
          "no availability class: Revit greys it on the start screen, where there is no model")
    check(os.path.isfile(ICON), "its picture is in Resources - FreeModel.png")
    free = NOTE_SUITE.body_of(commands, "public sealed class FreeModelCommand")
    check("HeronModelHold.Session.Free(" in free and "DocumentKey(doc)" in free,
          "it frees the ACTIVE model's hold, whoever holds it")
    check("Transaction(" not in free.replace("[Transaction(", "")
          and "new Transaction" not in free and "TaskDialog" not in free,
          "no transaction and no popup - it changes no element and announces nothing")
    check("[Transaction(TransactionMode.Manual)]" in read(COMMANDS)[
        read(COMMANDS).find("FreeModelCommand") - 400:read(COMMANDS).find("FreeModelCommand")],
          "declared Manual, so Revit opens none for it either")

    print("\n9. the carried chain and the Keep grant are the conversation's, not the person's")
    executor = code(EXECUTOR)
    raw_executor = read(EXECUTOR)
    check('Json.ReadString(request, "client")' not in raw_executor,
          "RevitFragment reads no `client` - the person's id keys nothing of one chat's")
    check("HeronConversation.ChatOf(" in executor
          and "Json.ReadString(request, HeronConversation.EnvelopeField)" in executor,
          "the chat it keys the chain and the grant on is ChatOf(the conversation)")
    run_fragment = NOTE_SUITE.body_of(executor, "public static string Run(UIApplication app, string request,")
    check(before(run_fragment, "ForgetOneRequestChains()", "HeronConversation.ChatOf("),
          "a request with no conversation keeps nothing past itself - its chain is dropped "
          "as the next run begins")
    check(before(run_fragment, "HeronConversation.ChatOf(", "Forget(client)"),
          "so a \"reset\" forgets this chat's chain, never another chat's")
    grant = code(GRANT)
    can_hold = NOTE_SUITE.body_of(grant, "public static bool CanHold(")
    check("HeronConversation.IsOneRequest(client)" in can_hold,
          "a chat of one request is never offered \"keep the rest of this job\"")

    print("\n10. the D-22 lease is untouched, and so is `client`")
    lease = code(LEASE)
    check("conversation" not in lease.lower() and "HeronModelHold" not in lease,
          "HeronLease knows nothing of conversations - it is still per Revit and per person")
    check(re.search(r'HERON_CLIENT_ID"\s*:\s*"ajmal-pc"', read(os.path.join(ROOT, ".mcp.json")))
          is not None, ".mcp.json still sends the one client id the lease is held by")

    print("\n11. three minutes, on both sides of the pipe")
    check(CFG.DEFAULTS.get("bridge.modelHoldMinutes") == "3",
          "heron_config.py declares bridge.modelHoldMinutes, 3")
    check(re.search(r'\{\s*"bridge\.modelHoldMinutes"\s*,\s*"3"\s*\}', read(CONFIG_CS)) is not None,
          "and HeronConfig.cs the same")
    check("HeronModelHold.MinutesKey" in operations,
          "and the add-in reads it, fresh at each request")
    check(re.search(r'case "model_in_use":\s*return "[^"]*nothing was sent"', read(DISPATCHER))
          is not None,
          "the banner over Revit says another chat is using the model and nothing was sent")


def rule():
    """The half that runs: build the host and let it check the rule."""
    print("\n12. the rule itself, built and run without Revit")
    tfm = NOTE_SUITE._tfm()
    if tfm is None:
        print("  NOT RUN - no framework here that an installed SDK can build AND")
        print("  a runtime can run. Linux: apt-get install -y dotnet-sdk-10.0")
        return COULD_NOT_RUN

    out_dir = "bin/x64/Debug-%s/" % tfm
    built = subprocess.run(
        ["dotnet", "build", HOST, "-p:RevitVersion=2024", "-p:HeronTfm=%s" % tfm,
         "-p:OutputPath=%s" % out_dir, "--nologo", "-v", "q"],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        check(False, "the host builds for %s" % tfm)
        for line in (built.stdout or b"").decode("utf-8", "replace").splitlines()[-20:]:
            print("    %s" % line)
        return 1

    dll = os.path.join(HOST, out_dir, "Heron.ModelHold.TestHost.dll")
    ran = subprocess.run(["dotnet", dll], stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    said = (ran.stdout or b"").decode("utf-8", "replace")
    for line in said.splitlines():
        print("  " + line)
    check(ran.returncode == 0, "the host's own checks pass (exit %d)" % ran.returncode)
    return 0


def main():
    envelope()
    failure()
    registry()
    wiring()
    host = rule()

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for what in FAILURES:
            print("  - %s" % what)
        return 1
    if host == COULD_NOT_RUN:
        print("COULD NOT RUN - the envelope and the wiring passed and the rule was not run.")
        print("Exit 3, which is NOT a pass.")
        return COULD_NOT_RUN
    print("PASS    one model, one chat at a time - D-141")
    return 0


if __name__ == "__main__":
    sys.exit(main())
