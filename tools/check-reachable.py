# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Built, tested, and called by nothing but a test.

    python tools/check-reachable.py
    python tools/check-reachable.py --all      include what is already recorded

WHY. Twice on 2026-09-09 the same defect was found by hand, hours apart:

  * `heron_search.remember()` writes the utterance cache that docs/19 s5 makes
    step 1 of the whole pipeline. It is called from tests/test_search.py and
    from nowhere else, so the cache can never fill (Q-43).
  * `heron_context.assemble()` was built the same day and reachable only from a
    command line, until it was put on the MCP seam.

Neither is a bug. Both are complete, tested code that no production path
touches - which mcp/server/heron_brain.py's own docstring already names as the
failure mode: *complete, tested, and invisible to any conversation is not what
"built" was meant to mean.* Nothing in this repository looked for the shape.

WHAT COUNTS AS A HIT
--------------------
A public function in brain/, mcp/ or platform/ that is:

    not called anywhere inside its own module        (a helper is fine)
    not called by any other production module
    not called by any tools/ script
    not dispatched by name - see below
    not decorated (an @server.tool() is CALLED BY THE HOST, not by code)

A HIT IS A CANDIDATE, NOT A DEFECT, and the difference matters enough to have
its own flag. Some are deliberate and already written down - the Workflow
Engine is the clearest: HANDOVER.md says *"nothing calls it yet - and that is
deliberate, not an oversight"*, because its customer is a later phase. Those
are listed under RECORDED and hidden unless `--all`, so the top of the report
is what nobody has explained yet.

DISPATCH, AND THE THREE WAYS A TEXT CHECK GETS THIS WRONG
----------------------------------------------------------
A CLI subcommand is called by name rather than by a `foo()` in the source, so a
naive check calls every one of them dead. The precise test is a **dict literal
whose value is the function** or a **getattr with a literal name**. Both are
structures; neither can be produced by prose.

WHAT IS RECORDED IS THE FUNCTION, NEVER THE KEY. `hits()` asks whether a
FUNCTION NAME is dispatched, so the value's identifier is what goes in. Until
2026-09-22 the key went in instead, and the two are the same only in the
example a reader writes. Measured across this repository: thirteen dict
entries have a module-level function of their own file as the value, and in
EVERY ONE the key differs - `"accept": cmd_accept` in prove-agent.py,
`"header_disagreements": disagreements` in heron_agents.py, `".md": _read_text`
in heron_ingest.py. The rule never once suppressed the thing it was written to
suppress.

AND THE VALUE MUST BE A FUNCTION OF THAT FILE, not merely a name. `{"check":
name, "passed": True}` is a RESULT, and the result dict is the commonest shape
in brain/. Matching any `{str: Name}` put 636 words into the suppression list -
`count`, `report`, `review`, `version`, `read`, `evidence` - of which 52 were
public production function names, each one permanently invisible to this
report. An imported function used as a dict value needs no rule here: a bare
name the file imported is already counted a reference below.

THE GETATTR NAME IS THE SECOND ARGUMENT, always. `args[-1]` is the DEFAULT of
the three-argument form, and 183 of the 191 getattr calls in these areas have
three arguments - so `''`, `'?'`, `'2024'` and `'.txt'` were being recorded as
dispatched names while the real ones were missed.

That precision was arrived at by getting it wrong three times in one night, and
all three failures are the same failure:

  1. `check-revit-gate.py` compared a space-stripped haystack against a needle
     that still had spaces, and reported a confident **0** where the answer is
     114.
  2. `measure-routes.py` grepped for the text `remember(` and matched **its own
     docstring**, which describes the problem - then printed the opposite
     conclusion in the one line it exists to be right about.
  3. This tool's first version treated any string literal `"remember"` as
     dispatch. `measure-routes.py` contains one, in the `ast` comparison that
     finds callers of `remember`. **The tool written to find the problem made
     the problem invisible to the next tool.**

Three heuristics, three times fooled by text ABOUT the thing rather than the
thing. Hence `ast` here, and structures rather than words.

WHAT IT CANNOT SEE. A function reached through `globals()`, a registry built at
run time, a plugin loader, or a name assembled from parts. It reports what it
can prove absent from the source, which is not the same as unreachable.

NOT A GATE. Exits 0. A hit is a question.
"""

import ast
import collections
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AREAS = ("brain", "mcp", "platform", "tools", "tests")
PRODUCTION = ("brain/", "mcp/", "platform/")

# A hit somebody has already explained, and where. Kept here rather than in a
# comment so the report can say WHY it is not a finding, and so an entry that
# stops being true is one edit rather than a re-read.
RECORDED = {
    # The Workflow Engine's finish/rollback/done_stages were listed here until
    # 2026-09-09 and are gone: they are METHODS on the Workflow class, and this
    # tool no longer reports a method at all - one reached through an instance
    # cannot be attributed by name. HANDOVER.md still records that nothing
    # calls that engine yet and that it is deliberate; the tool simply has
    # nothing to say about it. Removed after its own stale-record check named
    # all three, one day after that check was written.
    # `can_promote` was listed here until 2026-09-13 and is gone. The excuse
    # quoted DECISIONS.md - "that gate existed and nothing stood on it" - and
    # it was true for as long as nothing asked the question. `tools/check-
    # signatures.py` now asks it about every signed fragment, because telling
    # an UNUSED signature from a STALE one is exactly what `can_promote`
    # decides, and re-deriving that answer beside it would have been a second
    # rule to keep in step with the first. The gate now has something standing
    # on it, so the excuse goes.
    # `provide_role` was listed here until 2026-09-12 and is gone. The excuse
    # was that CALLING it was the bug - true of the one caller it had, which
    # built a total map in heron_validate.py where "absent means result" made
    # an undeclared name indistinguishable from a declared one. That caller is
    # gone and the map is built from the declaration itself.
    #
    # What made the entry stale is a DIFFERENT caller: tools/generate-jobs.py
    # has asked `provide_role(p) != "accounting"` since 2026-09-10, one entry at
    # a time, which is exactly what the function is for and where "absent means
    # result" is the safer read. A caller in tools/ counts as reached here, so
    # the function stopped being a hit that day and the excuse has been standing
    # over a closed gap ever since - which is the failure D-54 is about, and
    # tests/test_reachable.py has been red for it since.

    # D-54: this said "Q-43 ... is an open question, not an oversight" until
    # 2026-09-09, when D-61 answered it. The function is STILL uncalled from
    # production and the entry stays - what changed is why, and a reader sent
    # to an open question that has been closed is sent to the wrong place.
    ("brain/heron_search.py", "remember"):
        "D-61 answered Q-43: only a completed run may be cached, and evidence "
        "is a required argument. It stays uncalled because the evidence cannot "
        "reach the brain - a run happens in the add-in and no workflow id "
        "crosses the seam. One named seam, not an undecided design",
    ("mcp/server/heron_runtime.py", "snapshot"):
        "built with nothing wired to it yet, deliberately. It is the provider "
        "for planning code that does not exist - no MCP tool offers it, because "
        "adding one changes the risk table in heron_tools.py, which docs/12 s71 "
        "makes a declaration with a human in it. PROPOSALS.md F1",
}


def python_files():
    found = []
    for area in AREAS:
        base = os.path.join(ROOT, area)
        if not os.path.isdir(base):
            continue
        for where, dirs, files in os.walk(base):
            dirs[:] = [d for d in dirs if d != "__pycache__"]
            for name in files:
                if name.endswith(".py"):
                    # NORMALISED TO "/" HERE, AT THE ONE PLACE A PATH IS MADE,
                    # because every comparison downstream is written with "/":
                    # PRODUCTION is ("brain/", "mcp/", "platform/"), and hits()
                    # asks startswith("tools/") and startswith("tests/").
                    #
                    # os.path.relpath returns "brain\thing.py" on Windows, so
                    # every one of those was False there and hits() took its
                    # `continue` for EVERY definition. The tool reported
                    # nothing unreachable - not as an error, as a clean run.
                    # A gate that passes because it cannot see is the failure
                    # this repository keeps finding; it ran on every ship from
                    # the owner's PC and always said 0.
                    #
                    # The same defect as the one fixed in test_context.py on
                    # 2026-09-12, in its twin, found by running the suite on
                    # Windows for the first time.
                    found.append(os.path.relpath(os.path.join(where, name),
                                                 ROOT).replace(os.sep, "/"))
    return sorted(found)


def survey():
    """(definitions, calls, referenced, dispatched, decorated, unreadable)."""
    definitions = {}
    calls = collections.defaultdict(set)
    referenced = collections.defaultdict(set)
    dispatched = collections.defaultdict(set)
    decorated = set()
    unreadable = []

    for rel in python_files():
        try:
            with open(os.path.join(ROOT, rel), encoding="utf-8") as fh:
                tree = ast.parse(fh.read(), filename=rel)
        except (OSError, UnicodeDecodeError, SyntaxError):
            # Named, never skipped: "nothing calls it" must not be an artefact
            # of a file nobody could read.
            unreadable.append(rel)
            continue

        # MODULE LEVEL ONLY. `ast.walk` also finds nested closures and class
        # METHODS, and neither is a module's public surface:
        #
        #   heron_bridge_client.reader   a closure handed to threading.Thread
        #   heron_health.worst           a method, called through an instance
        #
        # Both were reported as "called by NOTHING AT ALL". A method reached
        # through an instance cannot be attributed by name at all, and a
        # closure belongs to the function that defines it.
        for node in tree.body:
            if isinstance(node, ast.FunctionDef):
                definitions[(rel, node.name)] = node
                if node.decorator_list:
                    decorated.add(node.name)

        # Names this file imported BY NAME. Only those may match a bare Name -
        # see referenced() below.
        imported = set()
        for node in ast.walk(tree):
            if isinstance(node, ast.ImportFrom):
                for alias in node.names:
                    imported.add(alias.asname or alias.name)

        # Module-level functions OF THIS FILE. A dict value naming one is
        # dispatch; a dict value naming a parameter or a local is a result.
        own = set(node.name for node in tree.body
                  if isinstance(node, ast.FunctionDef))

        for node in ast.walk(tree):
            if isinstance(node, ast.Call):
                fn = node.func
                name = (fn.attr if isinstance(fn, ast.Attribute)
                        else fn.id if isinstance(fn, ast.Name) else None)
                if name:
                    calls[name].add(rel)
                if (name == "getattr" and len(node.args) >= 2
                        and isinstance(node.args[1], ast.Constant)
                        and isinstance(node.args[1].value, str)):
                    # args[1], NEVER args[-1]: the three-argument form ends
                    # with the DEFAULT, and that is the form almost every call
                    # in this repository uses.
                    dispatched[node.args[1].value].add(rel)
            elif isinstance(node, ast.Attribute) and isinstance(node.ctx, ast.Load):
                # `SEARCH.remember` without calling it - handed to a thread,
                # stored in a table, passed as a callback. A use, not a call.
                referenced[node.attr].add(rel)
            elif (isinstance(node, ast.Name) and isinstance(node.ctx, ast.Load)
                  and node.id in imported):
                # A BARE name counts ONLY where the file imported it. Counting
                # every bare name lost `want()` - Q-47's whole subject - to
                # LOCAL VARIABLES called `want` in heron_fragment, heron_embed
                # and check-fragments-compile, and `worst` to locals in
                # heron_gaps and the MCP server. A permissive check that
                # reports nothing is the worse direction to be wrong in.
                referenced[node.id].add(rel)
            elif isinstance(node, ast.Dict):
                for key, value in zip(node.keys, node.values):
                    if (isinstance(key, ast.Constant)
                            and isinstance(key.value, str)
                            and isinstance(value, ast.Name)
                            and value.id in own):
                        # THE VALUE, not the key: the key is what the user
                        # types and the value is what gets called.
                        dispatched[value.id].add(rel)

    return definitions, calls, referenced, dispatched, decorated, unreadable


def hits():
    definitions, calls, referenced, dispatched, decorated, unreadable = survey()
    found = []
    for (rel, name) in sorted(definitions):
        if not rel.startswith(PRODUCTION):
            continue
        if name.startswith("_") or name == "main" or name in decorated:
            continue
        # A CALL or a qualified REFERENCE both count as being used.
        callers = calls.get(name, set()) | referenced.get(name, set())
        if rel in callers:
            continue
        if any(c.startswith(PRODUCTION) for c in callers):
            continue
        if any(c.startswith("tools/") for c in callers):
            continue
        if name in dispatched:
            continue
        tests = sorted(c for c in callers if c.startswith("tests/"))
        found.append((rel, name, tests))
    return found, unreadable


def main(argv):
    show_all = "--all" in argv
    found, unreadable = hits()

    new = [h for h in found if (h[0], h[1]) not in RECORDED]
    known = [h for h in found if (h[0], h[1]) in RECORDED]

    # A RECORDED entry that is no longer a hit is a STALE RECORD, and saying so
    # is the whole of D-54's lesson: a sentence describing a gap has to be
    # corrected when the gap closes, or it becomes the most convincing wrong
    # documentation in the repository. Without this the list below would go on
    # excusing `remember()` for ever after somebody wired it up.
    reached = set((rel, name) for rel, name, _t in found)
    stale = sorted(k for k in RECORDED if k not in reached)

    print("BUILT, AND NO PRODUCTION CODE CALLS IT")
    print("=" * 70)
    print("A hit is a CANDIDATE, not a defect. Some are deliberate and are")
    print("listed separately with where that is written down.")
    print("")

    if unreadable:
        print("COULD NOT BE READ - these are not evidence of anything")
        print("-" * 70)
        for rel in unreadable:
            print("  %s" % rel)
        print("")

    print("NOT EXPLAINED ANYWHERE  (%d)" % len(new))
    print("-" * 70)
    if not new:
        print("  None. Every unreached function has a reason written down.")
    for rel, name, tests in new:
        print("  %-34s %s" % (rel, name))
        print("       %s" % ("called by " + ", ".join(tests) if tests
                             else "called by NOTHING AT ALL, not even a test"))
    print("")

    if stale:
        print("THE RECORD IS OUT OF DATE  (%d)" % len(stale))
        print("-" * 70)
        print("  These are excused below and are NO LONGER reported as hits.")
        print("  Either something now uses them, or this check stopped")
        print("  classifying them as hits at all - the second is what happened")
        print("  to three Workflow Engine METHODS the day the definition rule")
        print("  narrowed to module level. Remove them from RECORDED either")
        print("  way, or the excuse goes on standing after its reason has")
        print("  gone (D-54).")
        for rel, name in stale:
            print("  %-34s %s" % (rel, name))
        print("")

    print("ALREADY RECORDED  (%d)%s"
          % (len(known), "" if show_all else " - `--all` to list"))
    print("-" * 70)
    if show_all:
        for rel, name, _tests in known:
            print("  %-34s %s" % (rel, name))
            print("       %s" % RECORDED[(rel, name)])
    print("")

    print("It cannot see a function reached through globals(), a registry")
    print("built at run time, or a name assembled from parts. Absent from the")
    print("source is not the same as unreachable.")
    print("")
    print("Not a gate; exits 0. A hit is a question.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
