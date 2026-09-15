# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-API-001
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The API Documentation Agent - the reference it generates.

    python tests/test_api_docs.py

WHY THIS EXISTS
---------------
tests/test_catalog.py's rule: a generator that only draws needs no test,
one that CONCLUDES does. This one decides whether a parameter is
explained, and a wrong answer there is invisible - the page renders
perfectly either way.

WHAT IT PROVES
  1. IT PARSES AND DOES NOT IMPORT. The server module is absent from
     sys.modules after a full run, so this works on a checkout with no
     MCP SDK installed - which is exactly where documentation is wanted.

  2. "EXPLAINED" IS A WORD BOUNDARY, NOT A SUBSTRING. `full` is not
     explained by "fully" and `path` is not explained by "pathological".
     A substring match would call both documented and the gap would stay
     invisible, which is the one thing this page exists to prevent.

  3. RISK AND OPERATION ARE ASKED OF heron_tools, not copied - every row
     matches what that module answers for the same tool.

  4. A TOOL IT CANNOT CLASSIFY IS REPORTED, NEVER DROPPED - in both
     directions: served but not declared, and declared but not served.

  5. REQUIRED AND DEFAULTED PARAMETERS ARE TOLD APART, because a caller
     reading the schema needs to know which it must send.

  6. THE PAGE IS REAL OUTPUT: the placeholder is gone, the embedded data
     is valid JSON, and its counts match what was parsed.

WHAT IT DOES NOT PROVE. That the page LOOKS right. Nothing here renders it.
"""

import importlib.util
import io
import json
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

import heron_tools as TOOLS                                   # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def load_tool():
    """The generator, loaded by path - its filename has hyphens in it."""
    path = os.path.join(ROOT, "tools", "generate-api-docs.py")
    spec = importlib.util.spec_from_file_location("heron_api_docs", path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def main():
    tool = load_tool()

    print("\n1. it parses, it does not import")
    sys.modules.pop("heron_mcp_server", None)
    rows, notes = tool.collect()
    check("heron_mcp_server" not in sys.modules,
          "the server module is NOT in sys.modules after a full run - this "
          "works on a checkout with no MCP SDK installed")
    check(len(rows) == len(TOOLS.TOOLS),
          "and all %d tool(s) were still found" % len(TOOLS.TOOLS))
    check(sorted(one["name"] for one in rows) == sorted(TOOLS.TOOLS),
          "by the same names heron_tools declares")
    check(all(one["doc"] for one in rows),
          "each carrying its own docstring")

    # EVERY TOOL MUST BE DEFINED BEFORE THE BLOCK THAT BLOCKS. The
    # configured entry point runs this file directly, so `server.run()`
    # inside `if __name__ == "__main__":` never returns - a @server.tool()
    # written BELOW it never executes its decorator, and the tool is
    # simply absent from tools/list while the registry still declares it.
    # Nothing failed, nothing logged, the tool was just not there.
    #
    # It happened on 2026-09-15: `revit_phases` was appended to the end of
    # the file and sat 16 lines past the main block. Found by a review,
    # not by anything here - which is why this check exists now.
    import ast
    tree = ast.parse(io.open(tool.SERVER, encoding="utf-8").read())
    blocks = [node.lineno for node in tree.body
              if isinstance(node, ast.If)
              and "__main__" in ast.dump(node.test)]
    check(len(blocks) == 1,
          "the server has exactly one `if __name__ == \"__main__\"` block, "
          "at line %s" % (blocks or "nowhere"))
    late = [one["name"] for one in rows if one["line"] > blocks[0]]
    check(not late,
          "and every one of the %d tool(s) is defined ABOVE it - a tool "
          "below never registers, because server.run() does not return%s"
          % (len(rows),
             "" if not late else ": %s" % ", ".join(late)))

    print("\n2. explained is a word boundary, not a substring")
    check(tool.explains("the depth to walk to", "depth"),
          "a docstring naming the parameter explains it")
    # A SUBSTRING MATCH WOULD CALL BOTH OF THESE DOCUMENTED.
    check(not tool.explains("it reads the file fully", "full"),
          "`full` is NOT explained by the word 'fully'")
    check(not tool.explains("a pathological case", "path"),
          "`path` is NOT explained by 'pathological'")
    # A PLURAL-ONLY MENTION COUNTS AS UNEXPLAINED. That is a false
    # positive and it is the right direction: naming a parameter that
    # is arguably documented costs a reader one glance, and missing one
    # that is not hides the gap this page exists to find.
    check(not tool.explains("requests are queued", "request"),
          "'requests' alone does NOT explain `request` - strict, and "
          "erring towards naming one parameter too many")
    check(not tool.explains("", "anything"),
          "an empty docstring explains nothing")
    check(not tool.explains(None, "anything"),
          "and a missing one does not crash")

    print("\n3. risk and operation are asked of heron_tools")
    for one in rows:
        want = TOOLS.NAMES[TOOLS.risk_of(one["name"])]
        if one["risk"] != want:
            check(False, "%s says %s, heron_tools says %s"
                         % (one["name"], one["risk"], want))
            break
    else:
        check(True, "every row's risk is the one heron_tools answers")
    check(all(one["operation"] == TOOLS.operation_of(one["name"])
              for one in rows),
          "and so is every bridge operation")
    writers = [one["name"] for one in rows if one["changes_model"]]
    check(writers == [name for name in sorted(TOOLS.TOOLS)
                      if TOOLS.writes(name)],
          "and `changes the model` is heron_tools.writes(), not a "
          "hand-typed flag: %s" % ", ".join(writers))

    print("\n4. a tool it cannot classify is reported, never dropped")
    kept = dict(TOOLS.TOOLS)
    victim = sorted(TOOLS.TOOLS)[0]
    try:
        del TOOLS.TOOLS[victim]
        blind, said = tool.collect()
        check(len(blind) == len(rows),
              "a served tool missing from the registry is still on the "
              "page - all %d rows" % len(blind))
        orphan = [one for one in blind if one["name"] == victim][0]
        check(orphan["risk"] is None and orphan["changes_model"] is False,
              "with its risk left EMPTY rather than guessed at")
        check(any(victim in line and "not declared" in line
                  for line in said),
              "and the page says it could not be classified")
    finally:
        TOOLS.TOOLS.clear()
        TOOLS.TOOLS.update(kept)

    kept = dict(TOOLS.TOOLS)
    try:
        TOOLS.TOOLS["heron_imaginary"] = (TOOLS.READ, None)
        _, said = tool.collect()
        check(any("heron_imaginary" in line and "@server.tool()" in line
                  for line in said),
              "and the other direction too - declared but never served")
    finally:
        TOOLS.TOOLS.clear()
        TOOLS.TOOLS.update(kept)

    check(tool.collect()[1] == [],
          "with the real registry restored, nothing is unclassified")

    print("\n5. required and defaulted parameters are told apart")
    params = [one for row in rows for one in row["params"]]
    check(params, "%d parameter(s) were read off the signatures"
                  % len(params))
    required = [one for one in params if one["default"] is None]
    check(required and any(one["default"] is not None for one in params),
          "%d must be sent and %d have a default - a caller reading the "
          "schema needs to know which"
          % (len(required), len(params) - len(required)))
    check(all("explained" in one for one in params),
          "and every one carries whether its docstring names it")
    named = [one for row in rows for one in row["silent"]]
    print("       %d parameter(s) are named by no docstring: %s"
          % (len(named), ", ".join(named) or "none"))

    print("\n6. the page is real output")
    where = tempfile.mkdtemp()
    try:
        out = os.path.join(where, "page.html")
        was = os.environ.get("HERON_API_DOCS_OUT")
        os.environ["HERON_API_DOCS_OUT"] = out
        try:
            check(load_tool().main() == 0, "the generator runs and returns 0")
        finally:
            if was is None:
                os.environ.pop("HERON_API_DOCS_OUT", None)
            else:
                os.environ["HERON_API_DOCS_OUT"] = was
        text = io.open(out, encoding="utf-8").read()
        check("__DATA__" not in text, "the placeholder is gone")
        found = re.search(r"const DATA = (\{.*\});", text, re.S)
        check(found is not None, "and the payload is where the page expects it")
        payload = json.loads(found.group(1))
        check(len(payload["rows"]) == len(rows),
              "the embedded data holds all %d tool(s)" % len(rows))
        check(str(len(params)) in payload["subtitle"],
              "the subtitle's parameter count is the one that was counted")
        check("parsed, never imported" in payload["footer"],
              "and the footer says how the schema was read")
    finally:
        shutil.rmtree(where)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    every tool from its own signature, and every gap named")
    return 0


if __name__ == "__main__":
    sys.exit(main())
