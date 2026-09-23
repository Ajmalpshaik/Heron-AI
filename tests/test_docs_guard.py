# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-VAL-009, HERON-DOC-RDM-007, HERON-DOC-ARC-006
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The documentation guard - and that it can see what it is claimed to see.

    python tests/test_docs_guard.py

WHY THIS FILE EXISTS
  F23 asked whether HERON-DOC-RDM-007 and HERON-DOC-ARC-006 should be
  built beside `tools/check-docs.py` or folded into it, and the owner
  folded them (D-77). Folding closes two rows WITHOUT WRITING A LINE OF
  CODE, which makes it exactly the kind of claim this repository keeps
  finding it has believed without checking - a claim quietly ageing into
  a fact.

  So the claim is checked rather than asserted: an error is PLANTED where
  each row says the guard should be looking, and the guard has to find
  it. This repository's own rule is prove the pattern can see what you
  know is there, and it was broken four times in the session that wrote
  this.

WHAT IT PROVES
  1. THE CLAIM IS DERIVED, NOT TYPED. The three rows come from
     check-docs.py's own header and are checked against docs/28 - so a
     renamed row fails here rather than going unnoticed.

  2. THE GUARD PASSES ON THE TREE AS IT STANDS. Without this the two
     findings below mean nothing: a checker that always fails would
     "catch" a planted error by accident.

  3. A FALSE COUNT PLANTED IN README.md IS CAUGHT - which is what
     HERON-DOC-RDM-007 asks of it, and why that row folds in here.

  4. A BROKEN REFERENCE PLANTED IN AN ARCHITECTURE DOC IS CAUGHT - which
     is HERON-DOC-ARC-006's half, and the reason the sweep has to reach
     `docs/`, not only the README.

  5. IT WRITES NOTHING. A guard that edits what it guards is a generator.
     These two rows were folded as the GUARDING half ONLY, and D-77 says
     so; this is what makes that limitation a fact rather than a promise.

  6. A TOOL AND A SKILL NO README NAMES ARE CAUGHT - a shell script as well
     as a .py, because three of the nine tools found unnamed on 2026-09-22
     were not Python - and so is an MCP tool total that disagrees with
     len(heron_tools.TOOLS), while another project's count and a quotation
     are left alone.

  7. TEXT HYGIENE READS WHAT GIT TRACKS: a planted control character, a dash
     double-encoded through the Windows code page, and merge-conflict
     markers are each named. With no git - the plain copy in 2 - it says
     NOT RUN rather than passing on nothing read.

  8. A HIT INSIDE A PROVEN FRAGMENT'S impl/ WAITS for its re-proof rather
     than failing the run, because fixing it makes the proof stale (D-30).

  9. A DECISION REFERENCE IS COMPARED WITH THE LOG WHATEVER THE LENGTH OF
     ITS NUMBER. It read exactly two digits, so from D-100 on a mistyped
     number passed unread (row 5b-184).

WHAT IT DOES NOT PROVE
  That the README is WRITTEN by anybody. Nothing generates it, and after
  D-77 nothing is scheduled to. The rows are closed on the guarding half
  and the register says which half that is.
"""

import hashlib
import io
import os
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

GUARD = os.path.join("tools", "check-docs.py")
ARCH = os.path.join("docs", "06-heron-platform.md")

# Copied rather than checked in place: this suite plants deliberate errors,
# and the repository it is running in may have somebody else working in it.
#
# `.claude` IS COPIED, and leaving it out was the first version's bug. The
# guard sweeps 140 markdown files and ten of them live there; a copy without
# them fails on links into files that were never missing, which would have
# made every finding below meaningless. `worktrees` is skipped by NAME,
# which is what check-docs.py itself skips and what stops a worktree copying
# itself.
SKIP = (".git", "worktrees", "bin", "obj", "node_modules",
        "__pycache__", ".vs")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def claimed_ids(path):
    """The agent ids a file's header claims, read the way agent-count reads."""
    found = set()
    with io.open(path, encoding="utf-8") as handle:
        for i, line in enumerate(handle):
            if i > 40:
                break
            m = re.match(r"^\s*(?://|#)\s*Heron-Agent\s*:\s*(.+?)\s*$", line)
            if not m or m.group(1).strip().lower() == "none":
                continue
            found |= set(a.strip() for a in m.group(1).split(",") if a.strip())
    return found


def run_guard(where):
    """(exit code, output) from running the guard inside a tree."""
    done = subprocess.run(
        [sys.executable, GUARD], cwd=where,
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=280)
    return done.returncode, done.stdout.decode("utf-8", "replace")


def section(out, title):
    """One numbered section of the guard's output, up to the next one."""
    after = out.split(title, 1)
    if len(after) < 2:
        return ""
    return after[1].split("\n=== ", 1)[0]


def proven_impl_file(tree):
    """A file inside some PROVEN fragment's impl/, in the copy, or None."""
    fragments = os.path.join(tree, "brain", "fragments")
    for name in sorted(os.listdir(fragments)):
        card = os.path.join(fragments, name, "fragment.yaml")
        if not os.path.isfile(card):
            continue
        if not re.search(r"^heron-status:\s*PROVEN\b",
                         io.open(card, encoding="utf-8").read(), re.M):
            continue
        for dirpath, _dirs, files in os.walk(os.path.join(fragments, name,
                                                           "impl")):
            for one in sorted(files):
                if one.endswith(".cs"):
                    return os.path.join(dirpath, one)
    return None


def fingerprint(where):
    """Every markdown file's bytes, so a write of any kind is visible."""
    prints = {}
    for dirpath, dirnames, filenames in os.walk(where):
        dirnames[:] = [d for d in dirnames if d not in SKIP]
        for name in filenames:
            if not name.endswith(".md"):
                continue
            path = os.path.join(dirpath, name)
            with io.open(path, "rb") as handle:
                prints[os.path.relpath(path, where)] = hashlib.sha256(
                    handle.read()).hexdigest()
    return prints


def main():
    print("THE DOCUMENTATION GUARD, AND WHAT IT CAN SEE")
    print("=" * 72)

    print("\n1. the claim is derived, not typed")
    ids = claimed_ids(os.path.join(ROOT, GUARD))
    import heron_contract as CON
    known = CON.registry_ids()
    check(len(ids) == 3,
          "check-docs.py claims three rows: %s" % ", ".join(sorted(ids)))
    check(all(one in known for one in ids),
          "and every one of them is a real row in docs/28")
    check("HERON-DOC-RDM-007" in ids and "HERON-DOC-ARC-006" in ids,
          "including the two D-77 folded in")

    home = tempfile.mkdtemp(prefix="heron-docs-guard-")
    tree = os.path.join(home, "repo")
    try:
        shutil.copytree(ROOT, tree, ignore=shutil.ignore_patterns(*SKIP))

        print("\n2. the guard passes on the tree as it stands")
        before = fingerprint(tree)
        code, clean = run_guard(tree)
        check(code == 0,
              "a clean copy of this repository passes, so a failure below "
              "is the planted error and not the weather")
        hygiene = section(clean, "11. TEXT HYGIENE")
        check("NOT RUN" in hygiene,
              "the copy has no git, and the text-hygiene section says it did "
              "NOT RUN rather than passing on nothing read")

        print("\n5. and it wrote nothing while doing it")
        # Asserted HERE, against the clean run, because a guard is at its
        # most tempting to "fix things up" on the pass that finds nothing
        # wrong. D-77 folded these rows as the GUARDING half only.
        after = fingerprint(tree)
        changed = sorted(k for k in before
                         if before[k] != after.get(k)) + sorted(
                             set(after) - set(before))
        check(not changed,
              "not one markdown file changed: %d checked%s"
              % (len(before),
                 "" if not changed else ", but %s did" % ", ".join(changed)))

        print("\n3. a false count planted in README.md is caught "
              "(HERON-DOC-RDM-007)")
        readme = os.path.join(tree, "README.md")
        kept = io.open(readme, encoding="utf-8").read()
        io.open(readme, "w", encoding="utf-8").write(
            kept + "\n\nOf these, 99 answered and 3 open.\n")
        code, out = run_guard(tree)
        check(code != 0, "the guard fails on a README that claims 99 answered")
        tail = out.split("THE SAME CLAIM", 1)[-1]
        check("README.md" in tail,
              "and names README.md as the file that said it")
        check("99" in tail, "quoting the number it did not believe")
        io.open(readme, "w", encoding="utf-8").write(kept)

        print("\n4. a broken reference planted in an architecture doc is "
              "caught (HERON-DOC-ARC-006)")
        arch = os.path.join(tree, ARCH)
        check(os.path.isfile(arch),
              "%s is there to plant one in" % ARCH.replace(os.sep, "/"))
        kept = io.open(arch, encoding="utf-8").read()
        io.open(arch, "w", encoding="utf-8").write(
            kept + "\n\nSee [the platform rules]"
                   "(#there-is-no-heading-with-this-name-anywhere).\n")
        code, out = run_guard(tree)
        check(code != 0,
              "the guard fails on an architecture doc with a dead anchor")
        head = out.split("2. GOLDEN RULES", 1)[0]
        check("06-heron-platform.md" in head,
              "and names the architecture doc, so the sweep reaches docs/ "
              "and not only the README")
        io.open(arch, "w", encoding="utf-8").write(kept)

        print("\n6. a tool and a skill no README names, and an MCP tool total "
              "nobody derived, are caught")
        stray_tool = os.path.join(tree, "tools", "stray-helper.sh")
        io.open(stray_tool, "w", encoding="utf-8").write("echo stray\n")
        stray_skill = os.path.join(tree, ".claude", "skills", "stray-skill")
        os.makedirs(stray_skill)
        io.open(os.path.join(stray_skill, "SKILL.md"), "w",
                encoding="utf-8").write("# stray\n")
        mcp_doc = os.path.join(tree, "docs", "04-heron-mcp.md")
        kept = io.open(mcp_doc, encoding="utf-8").read()
        io.open(mcp_doc, "w", encoding="utf-8").write(
            kept + "\n\nHeron has 99 MCP tools.\n\n"
                   "Their 314 MCP tools are somebody else's count.\n\n"
                   'Its README says "learn 314 MCP tools" and means its own.\n')
        code, out = run_guard(tree)
        named = section(out, "10. EVERY TOOL")
        check(code != 0, "the guard fails")
        check("tools/stray-helper.sh" in named,
              "and names the tool - a shell script too, not only a .py")
        check(".claude/skills/stray-skill" in named, "and the skill folder")
        drift = [l for l in out.splitlines() if "DRIFT" in l and "MCP" in l]
        check(any("04-heron-mcp.md" in l and "99 MCP tools" in l
                  for l in drift),
              "and the MCP tool total a sentence typed wrong")
        check(not any("314" in l for l in drift),
              "but not another project's count, nor a quotation of one")
        os.remove(stray_tool)
        shutil.rmtree(stray_skill)
        io.open(mcp_doc, "w", encoding="utf-8").write(kept)

        print("\n7. text hygiene over what git tracks")
        made = subprocess.run(["git", "init", "-q"], cwd=tree,
                              capture_output=True)
        added = subprocess.run(["git", "add", "-A"], cwd=tree,
                               capture_output=True)
        check(made.returncode == 0 and added.returncode == 0,
              "the copy is made a git tree, so there is something to track")
        kept = io.open(arch, encoding="utf-8").read()
        # Built, never typed: this file is tracked, so it is read by the very
        # section it tests, and a literal control character here would be
        # found here.
        backspace = chr(8)
        em_dash_misread = u"—".encode("utf-8").decode("cp1252")
        io.open(arch, "w", encoding="utf-8").write(
            kept + "\n\nA planted" + backspace + " character.\n\n"
                   "A planted " + em_dash_misread + " dash read twice.\n")
        conflict = os.path.join(tree, "docs", "planted-conflict.md")
        io.open(conflict, "w", encoding="utf-8").write(
            "\n".join(["<" * 7 + " ours", "one", "=" * 7, "two",
                       ">" * 7 + " theirs", ""]))
        subprocess.run(["git", "add", "-A"], cwd=tree, capture_output=True)
        code, out = run_guard(tree)
        hygiene = section(out, "11. TEXT HYGIENE")
        check(code != 0, "the guard fails")
        check("06-heron-platform.md" in hygiene and "U+0008" in hygiene,
              "and names the control character and where it is")
        check("double-encoded" in hygiene and "EM DASH" in hygiene,
              "and the double-encoded dash, by the character it meant")
        check("planted-conflict.md" in hygiene
              and "merge-conflict marker" in hygiene,
              "and the merge-conflict markers")
        io.open(arch, "w", encoding="utf-8").write(kept)
        os.remove(conflict)
        subprocess.run(["git", "add", "-A"], cwd=tree, capture_output=True)

        print("\n8. a hit inside a PROVEN fragment's impl/ waits for its "
              "re-proof rather than failing")
        target = proven_impl_file(tree)
        check(target is not None, "a PROVEN fragment with an impl/ is there")
        if target:
            kept = io.open(target, encoding="utf-8").read()
            io.open(target, "w", encoding="utf-8").write(
                kept + "\n// " + chr(12) + "\n")
            code, out = run_guard(tree)
            hygiene = section(out, "11. TEXT HYGIENE")
            check("WAITING" in hygiene and "U+000C" in hygiene,
                  "it is reported as WAITING - fixing it would make the "
                  "proof stale (D-30), so it goes with the re-proof")
            check("0 finding(s) that fail, 1 waiting" in hygiene,
                  "and it is not counted as a failure")
            io.open(target, "w", encoding="utf-8").write(kept)

        print("\n9. a decision reference is compared with the log whatever "
              "the length of its number")
        # The references were read as exactly two digits, so from D-100 on
        # none was compared with the log (FRAGMENT-ISSUES row 5b-184). It is
        # a report, not a failure - so it is the report that is read here.
        kept = io.open(arch, encoding="utf-8").read()
        io.open(arch, "w", encoding="utf-8").write(
            kept + chr(10) * 2 + "See D-999, and D-98 beside it." + chr(10))
        code, out = run_guard(tree)
        said = [line for line in section(out, "3. DECISIONS").split(chr(10))
                if "REFERENCED BUT NOT DEFINED" in line]
        said = said[0] if said else ""
        check("'D-999'" in said,
              "a reference to D-999, which the log does not define, is "
              "reported as referenced but not defined")
        check("'D-98'" not in said,
              "and D-98 beside it, which the log does define, is not")
        io.open(arch, "w", encoding="utf-8").write(kept)

        code, _out = run_guard(tree)
        check(code == 0,
              "and with every planted error removed it passes again - so "
              "each finding was the plant and nothing else")
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the guard sees what D-77 claims it sees")
    return 0


if __name__ == "__main__":
    sys.exit(main())
