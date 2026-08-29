#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Phase 2's third definition-of-done clause: the host resolves through
capabilities rather than agent names. Runs without Revit.

    python tests/test_brain_reachable.py

WHAT IT PROVES
  1. The brain answers from the MCP side at all - which it could not before
     this seam existed, when eight modules sat imported by nothing but their
     own tests.
  2. A capability nobody provides IS the gap, through the seam.
  3. The Revit version filter is still a WALL through the seam - not demoted,
     absent - and the answer says which case it is.
  4. THE ACCEPTANCE TEST: add a better provider and the call site does not
     change; delete the original and the same call still answers. That is
     Step 12's own test, re-run one layer up, where the host actually sits.
  5. Every brain tool is declared READ and reaches no bridge operation, so
     none of them can touch a model.
  6. Every tool the server declares is in the registry, and every brain tool
     tells the caller it cannot RUN what it resolved.
  7. With nowhere to keep knowledge it REFUSES, rather than answering with
     an empty catalogue - "knows nothing" and "cannot read what it knows"
     send a user in opposite directions.

WHAT IT CANNOT DO
  It does not import the MCP server: the MCP SDK is not installed on a machine
  with no Revit, and no test here imports it. So clause 6 reads the server as
  TEXT, the same technique test_tool_registry.py uses on the C# and for the
  same reason - something has to look at both sides when they cannot import
  each other. It proves the tools are declared and worded, never that FastMCP
  serves them. That takes the PC.

  And none of it says a fragment WORKS. Resolution returning a capability and
  that capability doing the job are separate claims, and only the first is
  tested here.
"""

import io
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def add_provider(store, fid, capability, status="DRAFT", risk="READ",
                 revit="2020,2024", kind="filter", domain="test"):
    """A second way of doing something. Inserted directly, because what is
    being tested is that the CALLER does not change - not how it got there."""
    store.execute(
        "INSERT OR REPLACE INTO fragments (id, capability, semantic_identity, "
        "kind, status, domain, risk, folder, revit) VALUES (?,?,?,?,?,?,?,?,?)",
        (fid, capability, "does %s" % capability, kind, status, domain,
         risk, "x", revit))
    store.db.commit()


def main():
    home = tempfile.mkdtemp(prefix="heron-reach-")
    os.environ["HERON_KNOWLEDGE"] = home

    import heron_brain as BRAIN
    import heron_tools as TOOLS
    import heron_scope as SCOPE

    try:
        # --- 1. it answers at all ------------------------------------------
        print("The brain, reached from the MCP side")
        found = BRAIN.catalogue()
        check(len(found["skills"]) >= 10,
              "the catalogue names %d skill(s) - before this seam, no tool "
              "could see one" % len(found["skills"]))
        check(any(s["ready"] for s in found["skills"]),
              "at least one skill has every capability it needs")
        check(not found["problems"],
              "no provider disagrees with another about risk or shape")

        # --- 2. the gap names itself ----------------------------------------
        print()
        print("A capability nobody provides")
        gaps = dict(found["gaps"])
        check(bool(gaps), "%d capability(ies) are wanted and unprovided, and "
                          "the absence IS the report" % len(gaps))
        check("TRACE_CONNECTIVITY" in gaps,
              "TRACE_CONNECTIVITY is named as wanted by %s"
              % ", ".join(gaps.get("TRACE_CONNECTIVITY", [])))
        missing = BRAIN.resolve("TRACE_CONNECTIVITY")
        check(missing["providers"] == [] and not missing["known"],
              "resolving it returns no provider rather than an error")

        # --- 3. the wall ----------------------------------------------------
        print()
        print("The Revit version filter, through the seam")
        blocked = BRAIN.resolve("FILTER_ELEMENTS_BY_CATEGORY", revit="2019")
        check(blocked["providers"] == [],
              "on a release no provider declares, nothing is returned")
        check(blocked["blocked_by_version"],
              "and it is reported as a VERSION refusal, not as 'never heard "
              "of it' - the two send a user in opposite directions")

        looked = BRAIN.lookup("select all ducts", revit="2019")
        check(looked["capability"] is None,
              "a lookup on 2019 resolves to nothing, however well the words "
              "match - the wall has no door in it for a good match")
        check(any("Revit" in e["reason"] for e in looked["excluded"]),
              "the excluded fragments are reported with the reason, so "
              "'found nothing' is never confused with 'not for this release'")

        # --- the ordinary case still works ----------------------------------
        ok = BRAIN.lookup("select all ducts", revit="2024")
        check(ok["capability"] == "FILTER_ELEMENTS_BY_CATEGORY",
              "on 2024 the same sentence resolves to %s" % ok["capability"])
        check(ok["route"] in ("identity", "cache", "hybrid"),
              "and it says which route answered (%s), because an exact "
              "phrasing and a ranked guess are not the same claim" % ok["route"])

        # --- 4. THE ACCEPTANCE TEST, and it MUTATES the library --------------
        # Deliberately last of the brain checks: it adds a provider and
        # deletes another, so anything reading the library as it ships has
        # to run before it. Written the other way round first, and the
        # lookup above then resolved to a different capability - which was
        # the retirement working and the test being wrong about the order.
        print()
        print("Add a provider, retire a provider - and the call site is one line")
        first = BRAIN.resolve("FILTER_ELEMENTS_BY_CATEGORY", revit="2024")
        check(first["providers"][0]["id"] == "FRG-ELE-001",
              "FILTER_ELEMENTS_BY_CATEGORY resolves to %s"
              % first["providers"][0]["id"])

        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            add_provider(store, "FRG-TEST-002", "FILTER_ELEMENTS_BY_CATEGORY",
                         status="PROVEN", revit="2020,2024")
        finally:
            store.close()

        second = BRAIN.resolve("FILTER_ELEMENTS_BY_CATEGORY", revit="2024")
        check(second["providers"][0]["id"] == "FRG-TEST-002",
              "a PROVEN provider now answers first - and the call above is "
              "the SAME LINE, because it named a capability and not a fragment")
        check(len(second["providers"]) == 2,
              "both providers are offered, most trusted first")

        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            store.execute("DELETE FROM fragments WHERE id = 'FRG-ELE-001'")
            store.db.commit()
        finally:
            store.close()

        third = BRAIN.resolve("FILTER_ELEMENTS_BY_CATEGORY", revit="2024")
        check(third["providers"] and third["providers"][0]["id"] == "FRG-TEST-002",
              "delete the original and the same call still answers - nothing "
              "above ever knew which fragment it was getting")

        # --- 5. the tools cannot touch a model ------------------------------
        print()
        print("What the three tools are allowed to do")
        for name in ("heron_capabilities", "heron_resolve", "heron_lookup"):
            check(TOOLS.risk_of(name) == TOOLS.READ,
                  "%s is declared READ" % name)
            check(TOOLS.operation_of(name) is None,
                  "%s reaches no bridge operation, so it cannot touch a model"
                  % name)
            check(not TOOLS.writes(name), "%s cannot write" % name)

        # --- 6. declared, and honest about what it cannot do ----------------
        print()
        print("The server's own declarations, read as text")
        server = io.open(os.path.join(ROOT, "mcp", "server",
                                      "heron_mcp_server.py"),
                         encoding="utf-8").read()
        declared = set(re.findall(r"@server\.tool\(\)\s*\ndef\s+(\w+)", server))
        check(declared == set(TOOLS.TOOLS),
              "every tool the server declares is in the registry and vice "
              "versa (%d)" % len(declared))

        # Each brain tool must SAY it cannot run what it resolved. There is no
        # executor - a fragment's C# has no route to Revit - and a host that
        # inferred otherwise would build a plan that fails at the last step.
        for name in ("heron_capabilities", "heron_resolve", "heron_lookup"):
            body = server.split("def %s(" % name, 1)[-1].split("@server.tool()")[0]
            check("_CANNOT_RUN" in body,
                  "%s tells the caller it cannot RUN what it resolved" % name)

        # --- 7. it refuses rather than answering "nothing" ------------------
        # Last, because it takes the knowledge folder away. "Heron knows how to
        # do nothing" and "Heron cannot read what it knows" send a user in
        # opposite directions, and an empty catalogue reads as the first.
        print()
        print("When it cannot read what it knows")
        saved = os.environ.pop("HERON_KNOWLEDGE", None)
        had_appdata = os.environ.pop("APPDATA", None)
        try:
            refused = None
            try:
                BRAIN.catalogue()
            except BRAIN.BrainUnavailable as why:
                refused = str(why)
            check(refused is not None,
                  "with nowhere to keep knowledge it REFUSES rather than "
                  "returning an empty catalogue")
            check(refused is not None and "HERON_KNOWLEDGE" in refused,
                  "and the refusal names what to set, so it can be acted on")
        finally:
            if saved is not None:
                os.environ["HERON_KNOWLEDGE"] = saved
            if had_appdata is not None:
                os.environ["APPDATA"] = had_appdata

        print()
        if FAILURES:
            print("FAILED - %d" % len(FAILURES))
            for line in FAILURES:
                print("  - %s" % line)
            return 1

        print("PASSED - the brain is reachable from the host, and a request")
        print("resolves through a CAPABILITY rather than through the name of")
        print("whatever happens to serve it today.")
        print()
        print("It says NOTHING about whether any of it works. Every skill and")
        print("every fragment is DRAFT, nothing can execute one, and the")
        print("proof D-30 asks for needs a real model. See NEEDS-CHECKING.md.")
        return 0
    finally:
        shutil.rmtree(home, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)


if __name__ == "__main__":
    sys.exit(main())
