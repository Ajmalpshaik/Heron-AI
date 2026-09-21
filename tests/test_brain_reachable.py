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
  It does not import the MCP server. So clause 6 reads the server as TEXT, the
  same technique test_tool_registry.py uses on the C# and for the same reason -
  something has to look at both sides when they cannot import each other. It
  proves the tools are declared and worded, never that an SDK serves them.

  THAT LIMIT IS NOW COVERED ELSEWHERE, and the gap it left was not theoretical.
  tests/test_mcp_serves.py installs the missing half: it imports the server
  against a real SDK and reads the SDK's own registry. Written 2026-08-31,
  it immediately found that the server could not be imported at all under the
  current SDK major - a defect this file's text read could not have seen, and
  by construction never could. A test that names its own limit is doing its
  job; a limit nobody ever covers is where the next defect lives.

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
        # The seven capabilities that were missing were written on 2026-08-29,
        # so the real library reports NO gaps - and asserting that it has one
        # would be asserting the work is unfinished. What still has to hold is
        # that an unprovided capability RESOLVES TO NOTHING rather than to an
        # error or to a plausible wrong provider, which is tested with a name
        # nothing will ever provide.
        gaps = dict(found["gaps"])
        check(not gaps,
              "no capability is wanted and unprovided - all %d skills are "
              "fully provided" % len(found["skills"]))
        missing = BRAIN.resolve("NO_SUCH_CAPABILITY_EXISTS")
        check(missing["providers"] == [] and not missing["known"],
              "a capability nobody provides resolves to NO PROVIDER rather "
              "than to an error - the absence IS the answer")
        check(not missing.get("blocked_by_version"),
              "and it is not mistaken for a version refusal, which would send "
              "the reader looking for a fragment that does not exist")

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
        check(ok.get("risks_unreadable") is None,
              "and on an ordinary answer nothing is reported unreadable")

        # --- A RISK COLUMN NOBODY COULD READ IS NOT A CLEAN ANSWER ----------
        # The block that fills these risks used to end `except Exception:
        # risks = {}` and say nothing. With no risks, the server's "a question
        # answered by something that writes" warning finds nothing above READ
        # and prints nothing - a silent pass that looks exactly like a clean
        # one, in the single case that block exists to reveal.
        # FRAGMENT-ISSUES section 5b, row 35.
        # _risks is patched rather than store.fragments, because retrieval
        # calls store.fragments too - breaking it would break the whole
        # lookup instead of the one read this check is about.
        real_risks = BRAIN._risks

        def refuses(_store):
            raise RuntimeError("the fragments table would not open")

        BRAIN._risks = refuses
        try:
            hurt = BRAIN.lookup("select all ducts", revit="2024")
        finally:
            BRAIN._risks = real_risks
        check(hurt.get("risks_unreadable"),
              "when the risks cannot be read the answer SAYS so rather than "
              "reporting every fragment as risk None")
        check("would not open" in (hurt.get("risks_unreadable") or ""),
              "and it carries the reason, not just a flag")

        # --- 3b. the Context Manager, through the same seam -----------------
        # It is here because of what its own tooling found on the day it was
        # built: measure-routes.py established that heron_search.remember() is
        # called from a test and nowhere else, so the utterance cache can never
        # fill (Q-43). A Context Manager reachable only from a command line
        # would be that mistake again, in the same week. This check is what
        # says it is reachable.
        print()
        print("The Context Manager, through the seam")
        packet = BRAIN.context("select all ducts", revit="2024")
        check(packet["carried"] == packet["budget"][:len(packet["carried"])]
              and set(packet["carried"]) <= set(packet["budget"]),
              "every part carried is inside the path's budget (%s)"
              % ", ".join(packet["carried"]))
        check(packet["parts"] and all(p["source"] for p in packet["parts"]),
              "every part says where it came from - docs/19 s1 asks for that "
              "and a part that cannot say cannot be checked when it is wrong")
        check(packet["parts"][0]["kind"] == "request",
              "the request comes first, always")
        check(all(p["body"] is None for p in packet["parts"]),
              "bodies are withheld unless full=True - the packet's SHAPE is "
              "cheap to look at and its contents are not")

        assumed = BRAIN.context("some wording nobody ever declared")
        check(assumed["assumed_path"],
              "an unclassified request is MARKED assumed - D-01 puts intent "
              "in the host and a default must never read as a decision")

        # DEPTH, AND THE MARKER THAT HAS TO REACH THE CALLER WHO ASKED FOR IT.
        #
        # The seam returned `depth` and a per-part `cut` from the day depth was
        # built and the MCP tool's render loop printed NEITHER - so the CLI
        # told a person what had been left out and the host was told nothing.
        # A shorter packet that reads exactly like a complete one is D-52's
        # plausible zero at the surface where it matters most. Found by a
        # security review of the depth change, as its one non-security note.
        check(packet["depth"] == "full"
              and not any(p["cut"] for p in packet["parts"]),
              "a full packet reports depth 'full' and NO cut on any part, so "
              "the marker means something when it does appear")

        shallow = BRAIN.context("select all ducts", revit="2024",
                                path="generation", depth="abstract")
        deep = BRAIN.context("select all ducts", revit="2024",
                             path="generation")
        check(shallow["size"] < deep["size"],
              "an abstract packet is smaller than a full one (%d < %d "
              "characters)" % (shallow["size"], deep["size"]))
        cuts = [p for p in shallow["parts"] if p["cut"]]
        check(shallow["depth"] == "abstract" and cuts,
              "and it says so, per part: %d part(s) report what they lost"
              % len(cuts))
        check(all("of" in p["cut"] and "not carried" in p["cut"] for p in cuts),
              "each cut says HOW MUCH was left out, not merely that some was")

        said = [p for p in shallow["parts"] if p["kind"] == "request"]
        deep_said = [p for p in deep["parts"] if p["kind"] == "request"]
        check(said and deep_said and said[0]["size"] == deep_said[0]["size"]
              and not said[0]["cut"],
              "THE REQUEST IS THE SAME SIZE AT EVERY DEPTH AND IS NEVER CUT - "
              "it is what a shortener takes first and the one thing that may "
              "never be shortened")

        bad_depth = None
        try:
            BRAIN.context("select all ducts", depth="tiny")
        except ValueError as why:
            bad_depth = str(why)
        check(bad_depth is not None and "not a depth" in bad_depth,
              "an unknown depth is a ValueError naming the three that exist, "
              "not a silently ignored argument")

        refused = None
        try:
            BRAIN.context("check this against our standard", path="standards")
        except BRAIN.ContextRefused as why:
            refused = str(why)
        check(refused is not None and "clause" in refused,
              "a path whose source does not exist raises ContextRefused, "
              "naming the source rather than returning a packet three quarters "
              "of what it claims")

        # A REFUSAL IS NOT A FAULT, and until ContextRefused existed the tool
        # caught Exception and called all of it a refusal - so a TypeError
        # would have been reported to the caller as "Heron refused", a
        # sentence about a decision Heron never made.
        wrong_path = None
        try:
            BRAIN.context("anything", path="nonsense")
        except BRAIN.ContextRefused:
            wrong_path = "refused"
        except ValueError as why:
            wrong_path = str(why)
        check(wrong_path and "not a path" in str(wrong_path),
              "an unknown path is a ValueError naming the four that exist, "
              "NOT a refusal - the caller made a mistake, Heron did not "
              "decline")

        # --- 4. THE ACCEPTANCE TEST, and it MUTATES the library --------------
        # Deliberately last of the brain checks: it adds a provider and
        # deletes another, so anything reading the library as it ships has
        # to run before it. Written the other way round first, and the
        # lookup above then resolved to a different capability - which was
        # the retirement working and the test being wrong about the order.
        print()
        print("Add a provider, retire a provider - and the call site is one line")
        # THE FIXTURE NOW SAYS WHAT IT MEANS. This block's claim is that
        # TRUST decides, and it needs a DRAFT original and a PROVEN
        # newcomer. It used to get the DRAFT half BY ACCIDENT, from
        # whatever status FRG-ELE-001 happened to ship with - and on
        # 2026-09-13 that fragment was proved against a real model and
        # promoted, so both providers were PROVEN, the newcomer outranked
        # nothing, and this check failed against a library that was
        # correct. Same shape as test_graph and test_reachable on
        # 2026-09-12. The store here is a throwaway in a temp directory,
        # so setting the status is a fixture and touches no fragment on
        # disk - and the claim below is unchanged and still fails if
        # trust stops deciding.
        store = SCOPE.open_scope(SCOPE.GLOBAL)
        try:
            store.execute("UPDATE fragments SET status = 'DRAFT' "
                          "WHERE id = 'FRG-ELE-001'")
            store.db.commit()
        finally:
            store.close()

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
            # RE-BASED 2026-09-06, and the reason matters more than the check.
            # This looked for the constant _CANNOT_RUN, whose text said "a
            # fragment's code has no way to reach Revit". D-28's executor was
            # built that day and the sentence went false, so both the constant
            # and the claim were replaced by _cannot_run(), which says what is
            # now true: a READ fragment runs, a fragment that WRITES still
            # cannot reach Revit.
            #
            # The intent is unchanged - every brain tool must hand back the
            # limit along with the answer, so a plan is not built on something
            # that fails at its last step.
            check("_cannot_run()" in body,
                  "%s tells the caller what it still cannot RUN" % name)

        # --- 6b. a window that empties the trail is not an empty trail ------
        # ROW 5b-93, AND IT IS SECTION 7's ARGUMENT ONE TOOL ALONG. `gaps`
        # took a `days` with no floor, `since()` returned nothing for one
        # below 1, and the tool above turned that into "Heron has no record
        # of doing anything yet" - said to somebody whose trail is full. The
        # two answers send a user in opposite directions, which is exactly
        # what section 7 exists to prevent for the catalogue.
        print()
        print("When the window, not the trail, is empty")
        said = BRAIN.gaps(-7)
        check(said.get("refused") == "NOT_A_WINDOW",
              "a window of -7 days comes back REFUSED, as data")
        check("not a window" in (said.get("why") or ""),
              "and the reason says what was wrong with it, so the tool can "
              "put it in front of the user instead of a wrong sentence")
        check("found" not in said,
              "and it does NOT come back looking like an answer with nothing "
              "in it, which is the shape that read as 'never been asked'")
        for real in (None, 0, 7):
            ok = BRAIN.gaps(real)
            check("found" in ok,
                  "%r is still a real window and still answers" % (real,))

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
        print("It says NOTHING about whether any of it works. Every skill is")
        print("DRAFT, most fragments are, and the proof D-30 asks for needs a")
        print("real model. See NEEDS-CHECKING.md.")
        print()
        print("This paragraph read 'every fragment is DRAFT, nothing can")
        print("execute one' until 2026-09-09. Both halves had been false since")
        print("2026-09-06 - D-28's executor runs fragments and 142 are PROVEN.")
        print("A passing test printing a false sentence is the same defect")
        print("test_served_claims.py exists to catch in the MCP replies.")
        return 0
    finally:
        shutil.rmtree(home, ignore_errors=True)
        os.environ.pop("HERON_KNOWLEDGE", None)


if __name__ == "__main__":
    sys.exit(main())
