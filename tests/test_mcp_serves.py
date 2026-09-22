#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   3
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The MCP server, actually served by a real SDK.

    python tests/test_mcp_serves.py

WHY THIS EXISTS, AND WHAT IT COST TO FIND OUT
  Every other test in this repository reads `heron_mcp_server.py` as TEXT.
  test_brain_reachable.py says so itself and names the limit: text proves a
  tool is DECLARED, never that an SDK will serve it. That gap hid a defect
  that would have taken every Heron tool out of the host at once.

  MEASURED 2026-08-31, by installing the SDK for the first time: `pip install
  --user mcp` - the line tools/HeronRevit.ps1 hands the user - now resolves to
  2.x, and 2.x DELETED `mcp.server.fastmcp`. FastMCP was renamed MCPServer.
  The server's import line was written against 1.x, so a user following
  Heron's own install instruction got an ImportError before a single tool was
  registered. Nothing in the repository could see it, because nothing here had
  ever imported the SDK.

  The fix was one import. The reason it went unseen for so long was the
  missing half of a test, and this is that half.

WHAT IT PROVES, when an SDK is installed
  1. The server MODULE IMPORTS at all, against whichever SDK is present.
  2. Every tool the server declares is actually SERVED - the count and the
     names, from the SDK's own registry rather than from the source text.
  3. Every served tool carries a description. A tool with no description
     reaches the host as a name and nothing else, and the host is being asked
     to choose between them.
  4. Each tool's ARGUMENTS are the ones declared. A renamed argument is
     invisible to a text read and fatal to a caller.
  5. The three brain tools ANSWER when called through the SDK's own dispatch,
     not by importing the function underneath it.
  6. Both refusals survive that round trip: every brain answer still says it
     CANNOT RUN what it resolved, and that nothing underneath is PROVEN. Those
     two sentences are the whole reason the brain tools are safe to expose,
     and a wrapper that dropped them would look identical from the outside.
  7. EVERY SERVED TOOL CARRIES THE SAFETY LABELS ITS REGISTRY RISK IMPLIES,
     read back from the SDK and compared with heron_tools.annotations - so no
     label a host sees can disagree with the registry the add-in's gate is
     checked against. A write labelled read-only is one a host may run
     without asking.
  8. THE SERVER'S INSTRUCTIONS ARE THE REGISTRY'S `host.chat`, assembled from
     the Constitution - not empty, and not the sentence saying they could not
     be assembled.
  9. `revit_read`, CALLED THROUGH THE SDK'S DISPATCH AGAINST A STAND-IN REVIT.
     A capability above ANALYZE is refused before any session is bound; a
     read goes out as run_fragment_read with no `apply`, aims at the pin once
     there is one, names an ignored value and a model not in front, and says
     how far the capability is proven.

WHAT IT CANNOT DO
  It does not start a transport and it is not Claude Code. It calls the
  server's own dispatch in-process, so it cannot show that a host connected
  over stdio, rendered a docstring, or chose a tool. That is `A8` in
  NEEDS-CHECKING.md and it takes the PC.

  It says nothing about whether any fragment WORKS. Every skill and every
  fragment is DRAFT.

  AND IT DOES NOT RUN WITHOUT THE SDK. The SDK is an optional dependency: the
  machine this was written on has no Revit and had no SDK either. So with none
  installed this exits 3 - not 0 and not 1 - and check-gaps.py reads that as
  WAITING rather than as a pass. A skipped test reported as `ok` is the same
  failure as a compile run that was killed before it printed: the absence of a
  complaint reads as success.
"""

import io
import os
import re
import sys
import shutil
import asyncio
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

SKIPPED = 3

FAILURES = []


def check(condition, description):
    print("  %-4s %s" % ("ok" if condition else "FAIL", description))
    if not condition:
        FAILURES.append(description)


def sdk_version():
    """Whichever SDK is installed, or None, or the reason it is unusable.

    Returns (version, broken) - `broken` is a string when something IS
    installed and cannot be imported, which is a third state and not a
    rounding error on the other two.

    BaseException, not Exception, and that is not defensiveness. Measured on
    the machine this was written on: a half-broken install raised
    `pyo3_runtime.PanicException` out of a native extension, and that inherits
    BaseException - so `except Exception` did not catch it and the test died
    with a stack trace instead of reporting a condition. A test whose job is
    to survive a missing dependency must survive a BROKEN one too, because
    from the outside they look the same and only one of them is tidy.
    """
    try:
        import mcp                                  # noqa: F401
    except ImportError:
        return None, None
    except BaseException as exc:                    # noqa: BLE001
        return None, "%s: %s" % (type(exc).__name__, exc)
    try:
        import importlib.metadata as meta
        return meta.version("mcp"), None
    except Exception:
        return "unknown", None


def declared_in_source():
    """The tool names the source claims, read as text.

    This is deliberately the OLD technique, kept as the other side of the
    comparison: the point of this file is that text and a running SDK can
    disagree, so it has to hold both and check they match.
    """
    path = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
    source = io.open(path, encoding="utf-8").read()
    return set(re.findall(r"@server\.tool\(\)\s*\ndef\s+(\w+)", source))


def schema_of(tool):
    """The SDK renamed this attribute too - 1.x inputSchema, 2.x input_schema."""
    return getattr(tool, "inputSchema", None) or getattr(tool, "input_schema", None) or {}


def text_of(result):
    """Flatten whatever call_tool returned into text.

    1.x hands back a list of content blocks; 2.x hands back a result object
    carrying `.content`. Both are read here rather than one being assumed,
    for exactly the reason this file exists.
    """
    blocks = getattr(result, "content", None)
    if blocks is None:
        blocks = result if isinstance(result, (list, tuple)) else [result]
    return "\n".join(getattr(b, "text", str(b)) for b in blocks)


async def exercise(server):
    tools = await server.list_tools()
    served = {t.name: t for t in tools}

    declared = declared_in_source()
    print()
    print("  the SDK's own registry, against the source text")
    check(declared and served, "both sides produced something to compare")
    check(set(served) == declared,
          "every declared tool is served, and no extra: %d" % len(served))
    missing = sorted(declared - set(served))
    if missing:
        check(False, "declared but NOT served: %s" % ", ".join(missing))

    print()
    print("  what a host would actually see")
    undescribed = sorted(n for n, t in served.items() if not (t.description or "").strip())
    check(not undescribed,
          "every served tool carries a description"
          + ("" if not undescribed else " - missing on %s" % ", ".join(undescribed)))

    # The arguments, from the generated schema rather than from the signature.
    expected = {
        "heron_resolve": ["capability"],
        "heron_lookup": ["request"],
        "revit_use_session": ["session"],
        "revit_select_by_category": ["category"],
        "revit_preview_move": ["category", "distance"],
        "revit_read": ["capability", "values", "expect_from"],
    }
    for name, args in sorted(expected.items()):
        if name not in served:
            check(False, "%s is not served, so its arguments cannot be checked" % name)
            continue
        got = sorted(schema_of(served[name]).get("properties", {}))
        check(got == sorted(args),
              "%s takes %s" % (name, ", ".join(sorted(args)) or "no arguments"))

    print()
    print("  the three brain tools, called through the SDK's own dispatch")
    answers = {}
    for name, arguments in (("heron_capabilities", {}),
                            ("heron_resolve", {"capability": "SET_SELECTION"}),
                            ("heron_lookup", {"request": "select all the ducts"})):
        try:
            answers[name] = text_of(await server.call_tool(name, arguments))
            check(bool(answers[name].strip()), "%s answered" % name)
        except Exception as exc:
            check(False, "%s raised %s: %s" % (name, type(exc).__name__, exc))

    print()
    print("  and both refusals survived the round trip")
    for name, answer in sorted(answers.items()):
        # RE-BASED 2026-09-06. Both of these matched wording that was true
        # when written and false by the end of that day: D-28's executor was
        # built, twenty fragments ran against a real model, and thirteen were
        # promoted on a recorded proof. Looking for the words "cannot run" and
        # "draft" would now demand the tool lie.
        #
        # THE INTENT IS KEPT EXACTLY: an answer must carry its own limits, so
        # the host cannot read a resolution as a promise. Only the limits
        # changed.
        low = answer.lower()

        # A fragment that WRITES still has no way to reach Revit. That is the
        # half of the old caution which is still true, and the half a plan
        # fails on at its last step.
        check("writes" in low and "revit" in low,
              "%s still warns that a fragment which WRITES cannot reach Revit"
              % name)

        # And it must still say how much is unproved rather than implying the
        # library is finished. PROVEN counts are read from the fragment files,
        # so this sentence moves on its own as fragments are promoted.
        check("proven" in low or "proved" in low,
              "%s still says how much underneath is proved" % name)

    print()
    print("  the safety labels a host sees, against the registry")
    import heron_tools as registry
    derive = getattr(registry, "annotations", None)
    check(derive is not None, "the registry derives labels to compare against")
    wrong = []
    for name, tool in sorted(served.items() if derive else ()):
        label = getattr(tool, "annotations", None)
        got = (label.model_dump(by_alias=True, exclude_none=True)
               if label is not None else {})
        # Only the three hints the registry derives are compared; a title,
        # if the SDK adds one, says nothing about what the tool can change.
        got = dict((k, v) for k, v in got.items()
                   if k in ("readOnlyHint", "destructiveHint", "idempotentHint",
                            "openWorldHint"))
        if got != derive(name):
            wrong.append("%s served %r, registry says %r"
                         % (name, got, derive(name)))
    check(served and derive is not None and not wrong,
          "every served tool's labels are the registry's (%d tools)%s"
          % (len(served), "" if not wrong else " - " + "; ".join(wrong[:3])))
    writers = sorted(n for n, t in served.items()
                     if getattr(getattr(t, "annotations", None), "model_dump", None)
                     and not t.annotations.model_dump(by_alias=True).get("readOnlyHint"))
    check("revit_change" in writers and "revit_apply_move" in writers,
          "and the two tools that change the model are among the ones a host "
          "sees as NOT read-only")

    return answers


class StandIn(object):
    """A Revit that answers from a script and remembers what it was asked."""

    revit_version = "2024"
    pid = 4242

    def __init__(self, reply):
        self.reply = reply
        self.asked = []

    def request(self, op, op_args=None, idempotent=True, **_ignored):
        self.asked.append((op, dict(op_args or {}), idempotent))
        return self.reply

    def close(self):
        pass


def first_with(risk, status):
    """The capability of the first fragment declaring both, or None."""
    folder = os.path.join(ROOT, "brain", "fragments")
    for name in sorted(os.listdir(folder)):
        card = os.path.join(folder, name, "fragment.yaml")
        if not os.path.isfile(card):
            continue
        seen = {}
        with io.open(card, encoding="utf-8", errors="replace") as fh:
            for line in fh:
                for key in ("capability", "risk", "heron-status"):
                    if line.startswith(key + ":"):
                        seen[key] = line.split(":", 1)[1].strip().strip("\"'")
        if seen.get("risk") == risk and seen.get("heron-status") == status:
            return seen.get("capability"), name
    return None, None


async def ask(server, arguments):
    """revit_read through the SDK's dispatch - or the reason it could not be.

    A server without the tool raises from the dispatch, and one traceback
    would replace every check below it (heron-ship 2a). So the absence comes
    back as text that no check below will mistake for an answer.
    """
    try:
        return text_of(await server.call_tool("revit_read", arguments)) or ""
    except Exception as exc:                                    # noqa: BLE001
        return "(revit_read could not be called: %s: %s)" % (type(exc).__name__, exc)


async def exercise_door(module):
    server = module.server

    print()
    print("  the instructions a host is given")
    given = getattr(server, "instructions", None) or ""
    check(given and not given.startswith("Heron could not give this chat"),
          "the server carries instructions, and they were assembled")
    assemble = getattr(module.brain, "host_instructions", None)
    check(assemble is not None and given == assemble(),
          "and they are the registry's host.chat, as assembled from the "
          "Constitution now")
    for phrase in ("Revit's own Undo", "Show before you change", "D-99",
                   "Never call a model, a system or a design"):
        check(phrase in given, "they include: %s" % phrase)

    print()
    print("  revit_read against a stand-in Revit, through the SDK's dispatch")
    resolved = []
    reply = {"ok": True, "ran": "x", "document": "Project1",
             "documentPath": "C:/jobs/Project1.rvt", "projectKey": "key-1",
             "wasActiveDocument": True, "bound": "all as given",
             "provides": {"found": "2 item(s) [Level 1, Level 2]"},
             "providesCount": 1}
    revit = StandIn(reply)

    def resolve():
        resolved.append(1)
        return revit

    module.binding.resolve = resolve
    module.pinned.forget()
    try:
        for risk in ("MODIFY", "EXECUTE"):
            capability, _folder = first_with(risk, "PROVEN")
            said = await ask(server, {"capability": capability})
            check("Nothing has been sent to Revit" in said and not revit.asked
                  and not resolved,
                  "%s (%s) is refused before any session is bound" % (capability, risk))

        capability, folder = first_with("READ", "PROVEN")
        said = await ask(server, {"capability": capability})
        check(len(revit.asked) == 1, "a PROVEN read (%s) sends one request" % capability)
        op, args, idempotent = revit.asked[-1] if revit.asked else (None, {}, None)
        check(op == "run_fragment_read", "as run_fragment_read - found %r" % op)
        check("apply" not in args and args.get("chain") == "reset" and args.get("source"),
              "with its source, a reset chain, and no `apply`")
        check(idempotent is True, "sent as safe to ask again, because it only reads")
        check("%s read Project1." % capability in said,
              "the answer names the model it read")
        check("found" in said and "Level 1" in said, "and shows what the fragment left")
        status = getattr(module.brain, "proof_status", None)
        proof = status(folder) if status else None
        expect = "CODE HAS CHANGED SINCE" if proof and proof["stale"] else "has not changed since"
        check("PROVEN" in said and expect in said,
              "and how far it is proven, from its own files")
        check("Nothing in the model was changed" in said, "and that nothing was changed")

        revit.asked[:] = []
        revit.reply = dict(reply, wasActiveDocument=False)
        said = await ask(server, {"capability": capability,
                                  "values": "noSuchValue=1"})
        op, args, _ = revit.asked[-1] if revit.asked else (None, {}, None)
        check(args.get("document") == "Project1"
              and args.get("documentPath") == "C:/jobs/Project1.rvt"
              and args.get("expectProject") == "key-1",
              "once pinned, the next read is AIMED at the pinned model by key, "
              "path and title")
        check("IGNORED: 'noSuchValue'" in said, "a value nothing declares is named")
        check("is not the model in front" in said,
              "and a model that is not the one on screen is said to be so")

        capability, _folder = first_with("READ", "DRAFT")
        revit.reply = reply
        said = await ask(server, {"capability": capability})
        check("DRAFT" in said and "claim" in said,
              "a DRAFT read (%s) says it is a claim, not a proof" % capability)
    finally:
        del module.binding.resolve
        module.pinned.forget()


def main():
    version, broken = sdk_version()
    print("Heron's MCP server, served by a real SDK")
    print("=" * 62)

    if version is None:
        print()
        if broken:
            print("SKIPPED - an MCP SDK is installed on this machine and cannot")
            print("be imported, so nothing here could be served:")
            print("  %s" % broken)
            print()
            print("That is a broken environment rather than a Heron defect, and")
            print("it is reported rather than swallowed: on the user's PC the")
            print("same condition takes every Heron tool out of the host, and it")
            print("would look identical to the SDK simply being absent.")
        else:
            print("SKIPPED - no MCP SDK is installed on this machine, so nothing")
            print("here could be served. That is a normal condition: the SDK is")
            print("an optional dependency and the machine this was written on")
            print("had none. Install it with:  pip install --user mcp")
        print()
        print("NOTHING IS PROVEN BY THIS RUN. Exiting %d rather than 0 so that"
              % SKIPPED)
        print("check-gaps.py reports it as WAITING and not as a pass.")
        return SKIPPED

    print("  SDK version %s" % version)

    # Knowledge has to live somewhere; the brain refuses rather than invent a
    # location, which is correct and would fail this test for the wrong reason.
    home = tempfile.mkdtemp(prefix="heron-mcp-serves-")
    had = os.environ.get("HERON_KNOWLEDGE")
    os.environ["HERON_KNOWLEDGE"] = home
    try:
        try:
            import heron_mcp_server as server_module
        except Exception as exc:
            print()
            print("  FAIL the server could not even be imported: %s: %s"
                  % (type(exc).__name__, exc))
            print()
            print("FAILED - and this is the exact defect this file was written")
            print("for. The SDK moved and the import did not. Every Heron tool")
            print("is absent from the host until it is fixed.")
            return 1

        server = server_module.server
        print("  server class %s.%s"
              % (type(server).__module__, type(server).__name__))

        asyncio.run(exercise(server))
        asyncio.run(exercise_door(server_module))

        print()
        if FAILURES:
            print("FAILED - %d" % len(FAILURES))
            for line in FAILURES:
                print("  - %s" % line)
            return 1

        print("PASSED - a real MCP SDK serves every tool Heron declares, with")
        print("its description and its arguments, and the three brain tools")
        print("answer through the SDK's own dispatch.")
        print()
        print("It is still not a host. Nothing here shows Claude Code connected")
        print("over stdio or chose a tool - that is A8 - and nothing here says")
        print("any fragment WORKS. Every skill and every fragment is DRAFT.")
        return 0
    finally:
        if had is None:
            os.environ.pop("HERON_KNOWLEDGE", None)
        else:
            os.environ["HERON_KNOWLEDGE"] = had
        shutil.rmtree(home, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main())
