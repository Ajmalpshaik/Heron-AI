#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
What a capability asks its caller to type, said BEFORE the call. Needs no
Revit and no store.

    python tests/test_needs.py

WHY THIS EXISTS AT ALL. Measured on 2026-09-22 against SET_CATEGORY_GRAPHICS:
four refusals to learn one fixed fact - the parameter names, then that a view
wants its exact Project Browser name, then the semicolon syntax, then the nine
override settings. The same capability's second and third use cost one call
each. A DIFFERENT fragment taking the identical nine settings cost the
discovery over again, because nothing said they were the same. The contract
was declared in fragment.yaml the whole time and never crossed to the side
doing the typing.

WHAT IT PROVES
  1. EVERY caller-supplied value in the library has a hint. This is the clause
     that matters: the other six are about today, this one is about a type
     added next month. Without it a new type prints a blank line, which reads
     exactly like "nothing to type" and sends the caller back to guessing.
  2. The block for SET_CATEGORY_GRAPHICS carries all four facts that the four
     refusals revealed - named here individually, so a hint that loses one
     fails rather than passes on being roughly right.
  3. Only `source: request` is listed. A need Heron finds in the model is not
     the caller's to type, and offering it is the same wrong turn as omitting
     one that is.
  4. A relative folder resolves from ANY working directory. The store keeps
     "brain/fragments/x", and resolving that against the cwd is correct from
     the repo root and silently empty from anywhere else - and empty is
     indistinguishable here from "needs nothing".
  5. A missing, empty or nonsense folder returns [] and does not raise. The
     block is an extra on an answer that was already complete.
  6. Spaces are stripped before a type is matched, the same way FromRequest
     does it - `(type ?? "").Replace(" ", "")`. The library declares
     `IDictionary<string, double>` WITH a space and the add-in matches it
     anyway, so a table keeping the space would report a gap on a type that
     is handled.
  7. No module name is shared between brain/, mcp/server/ and mcp/client/.
     heron_brain.py puts brain/ at sys.path[0], ahead of mcp/server/, so a
     shared name is SILENTLY shadowed - the import succeeds, binds the wrong
     module, and fails later at the first attribute. That is not theoretical:
     this file's own module was called heron_contract for an hour and collided
     with brain/heron_contract.py, which is the AGENT contract and a different
     thing entirely.
  8. A contract the server cannot read prints NOT KNOWN rather than nothing.
     77 of the 396 fragments legitimately need nothing typed, so an empty
     block is already a meaningful answer - and a failure that renders as one
     sends the caller back to the guessing the block exists to end.

WHAT IT CANNOT DO
  It does not check that a hint is TRUE. The authority on what a typed value
  may look like is RevitFragment.cs's FromRequest, which needs Revit to run;
  these hints were read off its refusal messages by hand on 2026-09-22. A hint
  that is merely stale will pass every clause here. The thing that catches
  that is a caller typing what it says and being refused - so a refusal whose
  wording disagrees with a hint is a bug in this table, not in the caller.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def modules(folder):
    where = os.path.join(ROOT, *folder)
    if not os.path.isdir(where):
        return set()
    return set(f[:-3] for f in os.listdir(where)
               if f.endswith(".py") and not f.startswith("__"))


def main():
    import heron_needs as NEEDS

    fragments = os.path.join(ROOT, "brain", "fragments")
    folders = sorted(os.listdir(fragments))

    # --- 1. every caller-supplied type has a hint --------------------------
    print("Every value a caller has to type")
    total = 0
    unknown = {}
    for name in folders:
        for row in NEEDS.needs(os.path.join(fragments, name)):
            total += 1
            if not row["known"]:
                unknown.setdefault(row["type"], []).append(name)

    check(total > 700,
          "the library declares %d caller-supplied value(s) - a count far "
          "below this means the parse stopped finding them" % total)
    # KNOWN, not hinted. `string` is known and deliberately has no text - the
    # type says it already. An UNKNOWN type is the defect, and it prints the
    # same blank line, so only this distinction catches it.
    check(not unknown,
          "all %d types are known to the table%s" % (
              total,
              "" if not unknown else " - MISSING: " + ", ".join(
                  "%s (%s)" % (t, f[0]) for t, f in sorted(unknown.items()))))

    # --- 2. the four facts four refusals cost ------------------------------
    print()
    print("SET_CATEGORY_GRAPHICS, the capability that cost four refusals")
    graphics = os.path.join(fragments, "set-category-graphics")
    rows = NEEDS.needs(graphics)
    named = dict((r["name"], r) for r in rows)

    check(set(named) == {"view", "categories", "overrides"},
          "names all three values - refusal 1 gave these and nothing else")

    view_hint = (named.get("view") or {}).get("hint") or ""
    check("Project Browser" in view_hint,
          "the view hint says Project Browser - refusal 2 was 'no view "
          "called \"active\"'")

    over_hint = (named.get("overrides") or {}).get("hint") or ""
    check("semicolon" in over_hint.lower(),
          "the overrides hint gives the separator - refusal 3")
    for setting in ("halftone", "cut-colour", "projection-line-colour",
                    "surface-colour", "cut-line-colour", "transparency",
                    "detail-level", "projection-line-weight",
                    "cut-line-weight"):
        check(setting in over_hint,
              "the overrides hint names %s - refusal 4 listed all nine"
              % setting)

    # The second fragment is the point of the whole exercise: it takes the
    # SAME nine settings and the discovery was paid twice.
    solid = NEEDS.needs(os.path.join(fragments, "set-category-solid-fill"))
    check([r["type"] for r in solid] == [r["type"] for r in rows],
          "set-category-solid-fill declares the identical contract - the "
          "repeat discovery this block exists to stop")

    # --- 3. only what the caller supplies ----------------------------------
    print()
    print("Only the caller's values")
    text = open(os.path.join(graphics, "fragment.yaml"),
                encoding="utf-8").read()
    check("source: model" not in text or
          not any(r["name"] == "document" for r in rows),
          "a model-sourced need is not offered to the caller")
    check(all(r["type"] for r in rows),
          "every listed value carries its declared type")

    # --- 4. any working directory ------------------------------------------
    print()
    print("From anywhere")
    here = os.getcwd()
    other = os.path.dirname(ROOT) or os.path.abspath(os.sep)
    try:
        os.chdir(other)
        moved = NEEDS.needs(os.path.join("brain", "fragments",
                                         "set-category-graphics"))
    finally:
        os.chdir(here)
    check(len(moved) == 3,
          "a relative folder resolves from %s, not just the repo root"
          % other)

    # --- 5. nothing to read is not a crash ---------------------------------
    print()
    print("Nothing to read")
    for bad in (None, "", "no-such-folder",
                os.path.join(ROOT, "brain", "fragments")):
        try:
            got = NEEDS.needs(bad)
            check(got == [], "%r returns [] rather than raising" % (bad,))
        except Exception as why:
            check(False, "%r raised %s" % (bad, why))

    # --- 6. spaces stripped, as FromRequest does ---------------------------
    print()
    print("Spelled as the add-in compares it")
    check(NEEDS.hint("IDictionary<string, double>") ==
          NEEDS.hint("IDictionary<string,double>"),
          "a space in a declared type does not lose its hint")
    check(NEEDS.hint("IList<Category>") == NEEDS.hint("List< Category >"),
          "the list wrapper is stripped whatever the spacing")
    check(NEEDS.hint("NoSuchTypeAnywhere") is None,
          "an unknown type answers None, never a plausible guess")
    check(not NEEDS.known("NoSuchTypeAnywhere"),
          "and known() says it is unknown - the clause above depends on "
          "this telling 'no format to give' from 'never heard of it'")
    check(NEEDS.known("string") and NEEDS.hint("string") is None,
          "string is known and deliberately silent - the type says it")
    check(NEEDS.hint("IList<string>") is not None,
          "but a LIST of them still says how to separate them")

    # --- 7. no shadowed module names ---------------------------------------
    print()
    print("No module shadows another")
    brain = modules(("brain",))
    server = modules(("mcp", "server"))
    client = modules(("mcp", "client"))
    for one, two, a, b in (("brain", "mcp/server", brain, server),
                           ("brain", "mcp/client", brain, client),
                           ("mcp/server", "mcp/client", server, client)):
        shared = sorted(a & b)
        check(not shared,
              "%s and %s share no module name%s"
              % (one, two, "" if not shared else " - " + ", ".join(shared)))

    # --- 8. an unreadable contract says so ---------------------------------
    #
    # Read as TEXT, the technique test_brain_reachable.py uses on the server
    # and for the same reason: importing it needs the MCP SDK, and this suite
    # must answer on a machine that has not got one.
    print()
    print("A contract that cannot be read does not go quiet")
    server = open(os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py"),
                  encoding="utf-8").read()
    check("import heron_needs" in server,
          "the server imports the module at all")
    check("needs.block(" in server,
          "heron_resolve asks for the block")
    check("takes  NOT KNOWN" in server,
          "a failure to read one prints NOT KNOWN rather than nothing - "
          "77 fragments legitimately need nothing typed, so silence is "
          "already taken as an answer")
    check("not the same as needing nothing" in server,
          "and it says so in those words, because that is the confusion")

    print()
    if FAILURES:
        print("%d FAILURE(S):" % len(FAILURES))
        for f in FAILURES:
            print("  - %s" % f)
        return 1
    print("All clauses pass.")
    print()
    print("This proves the hints are PRESENT and consistently shaped. It does")
    print("not prove one is TRUE - FromRequest is the authority and needs")
    print("Revit. A refusal whose wording disagrees with a hint here is a bug")
    print("in the table, not in whoever typed what it said.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
