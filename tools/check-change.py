# Heron-Agent:  HERON-DEV-REV-008
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Does this change do only what it said it would?

    python tools/check-change.py --intent "stop the duct filter losing its own
                                           utterances" --area brain --risk low
    python tools/check-change.py --intent-file docs/work-notes/fixes/my-fix.md
    python tools/check-change.py --base main --json

WHAT IT ASKS
------------
One question, in two halves.

  SCOPE     every file the diff touches, against the parts the change said it
            would touch. A file in no declared part and no supporting part is
            a QUESTION - not a failure, and never a silent pass.

  EVIDENCE  whether the gates this change's risk and signals oblige it to run
            were actually run, read out of an evidence record written by
            tools/change-evidence.py. No record means no evidence, and a
            change with no evidence is not a PASS.

WHY THE DECLARED AREA IS THE THING COMPARED AGAINST, AND NOT THE WORDS
----------------------------------------------------------------------
The obvious way to compare a diff against a one-line intent is to count words
shared between the sentence and the path. It is cheap and it is weak: it calls
`brain/heron_retrieve.py` unrelated to "fix retrieval" because the sentence
said "retrieval" and the path says "retrieve", and it calls `docs/README.md`
related to almost anything.

Heron does not have to guess. docs/PROJECT-MAP.md section B already says which
part owns what, and tools/check-structure.py already holds the table of who may
depend on whom - as CODE, enforced on every push. So the comparison here is
structural rather than lexical:

    required    the file is in a part the change declared
    supporting  the file is in a part a declared part is ALLOWED to depend on
    unrelated   neither

That second row is the one that earns its keep. An intent that declares `mcp`
and a diff that touches `brain/` is supporting work, because mcp may depend on
brain. The same intent touching `revit/` is not, because it may not - and that
is exactly the change that needs a person to look.

THE TABLE IS IMPORTED, NEVER COPIED
-----------------------------------
ALLOWED, PARTS and SUPPORT are read out of tools/check-structure.py at run
time. A second copy of the layering rules would be two homes for one fact, and
this repository has been bitten by that often enough to have written it down in
four separate places.

NOT EVERY FINDING IS A FAILURE, AND THE EXIT CODES SAY SO
---------------------------------------------------------
    0   PASS                     scope clean, obliged gates ran, nothing regressed
    1   SPLIT / REVISE / REVERT  the change needs work before it lands
    2   BLOCKED                  the intent is missing or unusable, so nothing
                                 here can be judged at all
    3   NEEDS REAL REVIT PROOF   everything answerable here is answered, and
                                 what is left needs a Revit this machine has not
                                 got. Same code tests/README.md already uses for
                                 "could not run here", and for the same reason:
                                 it must never be read as a pass.
"""

import argparse
import collections
import importlib.util
import io
import json
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# ---------------------------------------------------------------- vocabulary
# Every class a changed file can land in. Order is the order they are tested
# in, and the order matters - see classify().
REQUIRED = "required"
SUPPORTING = "supporting"
TESTS = "tests"
DOCS = "documentation"
BUILD = "build/config"
UNRELATED = "unrelated"

CLASSES = [REQUIRED, SUPPORTING, TESTS, DOCS, BUILD, UNRELATED]

RISKS = ("low", "medium", "high")


def w(s):
    """stdout that survives a console that is not UTF-8."""
    sys.stdout.write(s.encode("ascii", "replace").decode("ascii"))


# ------------------------------------------------------------ the one layering
def layering():
    """
    PARTS, SUPPORT and ALLOWED, read from check-structure.py rather than
    repeated. The filename has a hyphen so it cannot be imported by name;
    loading it by path is the price of not owning a second copy, and
    tools/agent-count.py already pays it for the same reason.
    """
    path = os.path.join(ROOT, "tools", "check-structure.py")
    spec = importlib.util.spec_from_file_location("heron_check_structure", path)
    if spec is None or spec.loader is None:
        return None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


# ------------------------------------------------------------------- the diff
LAST_GIT_ERROR = []


def git(args):
    """
    Run git, and KEEP WHAT IT SAID when it fails.

    This returned a bare None and the caller printed "git could not produce a
    diff here", which was true and useless. The real message - on a shallow
    clone, `fatal: main...HEAD: no merge base` - names both the cause and the
    fix, and swallowing it made an environment problem look like a broken tool.
    Same failure shape as check-narrow-errors.py exists to catch: a broken
    thing reported as an empty one.
    """
    out = subprocess.run(["git", "-C", ROOT] + args,
                         capture_output=True, text=True)
    if out.returncode != 0:
        LAST_GIT_ERROR.append((out.stderr or "").strip() or
                              "git exited %d" % out.returncode)
        return None
    return out.stdout


def changed_files(base=None, staged=False, rev_range=None):
    """
    (path, status, added, deleted) for every file in the diff.

    Three sources, because the useful moment differs. Mid-task the working
    tree is what matters; at review time the branch against its base is.
    """
    if rev_range:
        spec = [rev_range]
    elif base:
        spec = ["%s...HEAD" % base]
    elif staged:
        spec = ["--cached"]
    else:
        spec = ["HEAD"]

    names = git(["diff", "--name-status", "--find-renames"] + spec)
    stats = git(["diff", "--numstat", "--find-renames"] + spec)
    if names is None or stats is None:
        return None

    churn = {}
    for line in stats.splitlines():
        cols = line.split("\t")
        if len(cols) < 3:
            continue
        added = 0 if cols[0] == "-" else int(cols[0])
        deleted = 0 if cols[1] == "-" else int(cols[1])
        churn[cols[-1]] = (added, deleted)

    files = []
    for line in names.splitlines():
        cols = line.split("\t")
        if len(cols) < 2:
            continue
        status = cols[0][0]
        path = cols[-1].replace(os.sep, "/")
        added, deleted = churn.get(path, (0, 0))
        files.append({"path": path, "status": status,
                      "added": added, "deleted": deleted})

    # A NEW FILE IS INVISIBLE TO `git diff`, AND A SCOPE CHECK THAT CANNOT SEE
    # NEW FILES IS WORSE THAN NONE - the whole class of change this is meant to
    # catch, "something arrived that nobody asked for", arrives as an untracked
    # file. Found by running this tool on its own first diff, where it reported
    # zero files while two new ones sat beside it.
    #
    # Read-only: listing untracked files does not touch the index. `git add -N`
    # would make them visible to diff too, and is refused here because a
    # checker that edits the index is a checker somebody has to undo.
    if not rev_range and not base and not staged:
        others = git(["ls-files", "--others", "--exclude-standard"]) or ""
        for path in others.splitlines():
            path = path.strip().replace(os.sep, "/")
            if not path:
                continue
            try:
                with io.open(os.path.join(ROOT, path), encoding="utf-8",
                             errors="replace") as handle:
                    added = sum(1 for _ in handle)
            except OSError:
                added = 0
            files.append({"path": path, "status": "?",
                          "added": added, "deleted": 0})

    return sorted(files, key=lambda f: f["path"])


def diff_text(base=None, staged=False, rev_range=None, paths=None):
    """The diff body, with no context lines - only what actually changed."""
    if rev_range:
        spec = [rev_range]
    elif base:
        spec = ["%s...HEAD" % base]
    elif staged:
        spec = ["--cached"]
    else:
        spec = ["HEAD"]
    args = ["diff", "-U0", "--find-renames"] + spec
    if paths:
        args += ["--"] + list(paths)
    return git(args) or ""


# -------------------------------------------------------------- classification
BUILD_EXACT = {"Directory.Build.props", ".gitignore", ".mcp.json",
               "global.json", "packages.lock.json"}
BUILD_SUFFIX = (".csproj", ".sln", ".props", ".targets")
BUILD_PREFIX = (".github/",)


def is_build(path):
    name = path.rsplit("/", 1)[-1]
    if path in BUILD_EXACT or name in BUILD_EXACT:
        return True
    if path.endswith(BUILD_SUFFIX):
        return True
    if path.startswith(BUILD_PREFIX):
        return True
    return name.startswith("requirements") and name.endswith(".txt")


def part_of(path):
    return path.split("/")[0]


def classify(path, areas, allowed, known_parts):
    """
    One class per file, decided in this order. The order IS the rule.

    build first, because a change to how Heron is built or installed is the
    one class that must never be hidden inside another - it is what reaches a
    modeller's machine.

    Then tests, then documentation, both of which are always legitimate and
    are counted rather than questioned.

    Only then the structural comparison, which is the part that can raise a
    question.
    """
    if is_build(path):
        return BUILD
    if path.startswith("tests/"):
        return TESTS
    if path.endswith(".md") or path.startswith("docs/"):
        return DOCS

    part = part_of(path)
    if part in areas:
        return REQUIRED
    if part in allowed:
        return SUPPORTING

    # An unknown top-level folder is ALWAYS a question, even when the change
    # declared it. A part no rule recognises is the case a classifier is
    # worst at and a person is best at.
    if part not in known_parts:
        return UNRELATED
    return UNRELATED


# -------------------------------------------------------------------- signals
#
# A signal is not a verdict. It is a fact about the diff that changes WHICH
# gates this change owes, and each one names the paths that raised it so the
# claim can be checked in one look rather than believed.

TRUST_PATHS = (
    "platform/Heron.Core/HeronPermissions.cs",
    "platform/Heron.Core/HeronOperationRegistry.cs",
    "mcp/server/heron_tools.py",
    "mcp/server/heron_write.py",
    "revit/Heron.Revit.Addin/RevitWrite.cs",
    "HERON_CONSTITUTION.md",
    "docs/12-security-and-permissions.md",
    "docs/24-trust-model.md",
)

PACKAGING_PATHS = (
    "tools/deploy-addin.ps1",
    "tools/setup.ps1",
    "tools/HeronRevit.ps1",
    "Directory.Build.props",
    "platform/Heron.Core/HeronPaths.cs",
    "docs/07-installation-and-update.md",
)

GENERATED_FILES = ("docs/28-agent-registry.md",)

REVIT_SYMBOL = re.compile(r"^[+-].*#if\s+.*REVIT", re.M)
CONTRACT_LINE = re.compile(r"^[+-]\s*(capability|contract|needs|provides|revit|runtime):", re.M)
PACKAGE_REF = re.compile(r'^\+.*<PackageReference\s', re.M)
REQ_LINE = re.compile(r"^\+(?!#)\s*[A-Za-z0-9]", re.M)


def signals(files, base, staged, rev_range):
    """Every signal the diff raises, each with the paths behind it."""
    found = collections.OrderedDict()

    def raise_signal(name, why, paths):
        if not paths:
            return
        if name in found:
            found[name]["paths"] = sorted(set(found[name]["paths"]) | set(paths))
        else:
            found[name] = {"why": why, "paths": sorted(set(paths))}

    paths = [f["path"] for f in files]

    raise_signal("trust",
                 "a permission, risk-table or write-path file changed - "
                 "Constitution Articles 13 and 18 make this a human's decision",
                 [p for p in paths if p in TRUST_PATHS])

    raise_signal("packaging",
                 "something that decides what reaches a modeller's machine changed",
                 [p for p in paths if p in PACKAGING_PATHS or p.endswith(".addin")
                  or p.endswith(".csproj")])

    raise_signal("revit-version",
                 "Revit-facing code or the release-to-runtime map changed - "
                 "D-05 supports 2020 to 2027 and a release is never dropped quietly",
                 [p for p in paths if p.startswith("revit/") or p == "Directory.Build.props"])

    raise_signal("generated-by-hand",
                 "a file a generator owns was edited directly - re-run the generator instead",
                 [p for p in paths if p in GENERATED_FILES])

    raise_signal("deletion",
                 "a file was deleted - inspect the commit that introduced it and "
                 "which Revit releases it protects before removing it",
                 [f["path"] for f in files if f["status"] == "D"])

    raise_signal("fragment-behaviour",
                 "a fragment implementation or its lifecycle changed - "
                 "D-30 wants a run against a named model, not a compile",
                 [p for p in paths if p.startswith("brain/fragments/")])

    # The three that need the diff body rather than the path list.
    csprojs = [p for p in paths if p.endswith(".csproj")]
    reqs = [p for p in paths
            if p.rsplit("/", 1)[-1].startswith("requirements")
            and p.endswith(".txt")]
    if csprojs and PACKAGE_REF.search(diff_text(base, staged, rev_range, csprojs)):
        raise_signal("dependency",
                     "a package reference was added - a new dependency needs a stated reason",
                     csprojs)
    if reqs and REQ_LINE.search(diff_text(base, staged, rev_range, reqs)):
        raise_signal("dependency",
                     "a package reference was added - a new dependency needs a stated reason",
                     reqs)

    yamls = [p for p in paths if p.endswith("fragment.yaml")]
    tools_py = [p for p in paths if p == "mcp/server/heron_tools.py"]
    contract_paths = yamls + tools_py
    if contract_paths and CONTRACT_LINE.search(
            diff_text(base, staged, rev_range, contract_paths)):
        raise_signal("public-contract",
                     "a declared contract changed, not only an implementation - "
                     "callers depend on this and cannot see the change",
                     contract_paths)

    revit_paths = [p for p in paths if p.endswith((".cs", ".yaml"))]
    if revit_paths and REVIT_SYMBOL.search(
            diff_text(base, staged, rev_range, revit_paths)):
        raise_signal("revit-version",
                     "a version-conditional branch changed - every declared release "
                     "has to still be right",
                     revit_paths)

    return found


# ------------------------------------------------------------- obliged gates
#
# Three risk levels, and the gates each one owes, in Heron's own commands -
# D-68. A signal adds to that floor: touching a permission file owes a human
# review whatever risk was DECLARED, because the diff outranks the label.
#
# Nothing here runs a gate. It says which gates this change owes, and the
# evidence record says which were actually run. A tool that both sets the
# homework and marks it is a tool that will one day mark its own.

ALWAYS = ["check-docs", "check-metadata", "check-structure", "tests"]

BY_RISK = {
    "low": [],
    "medium": ["check-gaps"],
    "high": ["check-gaps", "human-review"],
}

BY_SIGNAL = {
    "trust": ["human-review"],
    "packaging": ["check-package"],
    "revit-version": ["check-compile", "check-api-surface"],
    "dependency": ["check-licence"],
    "public-contract": ["check-gaps"],
    "fragment-behaviour": ["check-routing", "REAL-REVIT-PROOF"],
    # Named for what it asks rather than for where the habit came from: read the
    # commit that introduced this, the fixes that followed it, and which Revit
    # releases it was protecting, BEFORE removing it.
    "deletion": ["history-check"],
    "generated-by-hand": ["regenerate"],
}


def obliged(risk, raised):
    owed = list(ALWAYS) + list(BY_RISK[risk])
    for name in raised:
        owed += BY_SIGNAL.get(name, [])
    seen = []
    for item in owed:
        if item not in seen:
            seen.append(item)
    return seen


# ------------------------------------------------------------------- the intent
def read_intent_file(path):
    """
    Three fields, read out of any text file so an intent can live inside a
    work note rather than in a format of its own.

        intent: one line saying exactly what should change
        area:   brain, tools
        risk:   low

    Three, and not the ten the plan first asked for. docs/29 section 4 sets
    the test for a new field - would a script fail the build over it? - and
    only these three survive it. Everything else the plan listed is either
    derivable from the diff (does this need Revit, which releases, is this a
    contract change) or is prose a script cannot check (acceptance criteria).
    """
    fields = {}
    try:
        text = io.open(path, encoding="utf-8", errors="replace").read()
    except OSError as exc:
        return None, str(exc)
    for line in text.splitlines():
        m = re.match(r"^\s*(intent|area|risk)\s*:\s*(.+?)\s*$", line, re.I)
        if m and m.group(1).lower() not in fields:
            fields[m.group(1).lower()] = m.group(2).strip()
    return fields, None


# -------------------------------------------------------------------- evidence
def read_evidence(path):
    try:
        with io.open(path, encoding="utf-8") as handle:
            return json.load(handle), None
    except (OSError, ValueError) as exc:
        return None, str(exc)


# ---------------------------------------------------------------------- report
def main(argv=None):
    parser = argparse.ArgumentParser(add_help=True)
    parser.add_argument("--intent")
    parser.add_argument("--area", help="comma separated, from the parts in "
                                       "tools/check-structure.py")
    parser.add_argument("--risk", choices=RISKS)
    parser.add_argument("--intent-file")
    parser.add_argument("--base", help="compare the branch against this ref")
    parser.add_argument("--staged", action="store_true")
    parser.add_argument("--range", dest="rev_range")
    parser.add_argument("--evidence", help="a record from tools/change-evidence.py")
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)

    struct = layering()
    if struct is None:
        w("BLOCKED: tools/check-structure.py could not be read, so the layering\n"
          "         rules this compares against are not available.\n")
        return 2
    known_parts = set(struct.PARTS) | set(struct.SUPPORT)

    intent = args.intent
    area = args.area
    risk = args.risk
    if args.intent_file:
        fields, err = read_intent_file(args.intent_file)
        if fields is None:
            w("BLOCKED: %s could not be read - %s\n" % (args.intent_file, err))
            return 2
        intent = intent or fields.get("intent")
        area = area or fields.get("area")
        risk = risk or fields.get("risk")

    # --- the intent contract is checked BEFORE it is used -------------------
    # Borrowed discipline, not borrowed code: a rule table that can be
    # silently wrong is worse than no rule table, so an unusable intent stops
    # the run rather than producing a confident report from nonsense.
    problems = []
    if not intent or not intent.strip():
        problems.append("no intent. One line saying exactly what should change.")
    if not area:
        problems.append("no area. One or more of: %s" % ", ".join(sorted(known_parts)))
    if not risk:
        problems.append("no risk. One of: %s" % ", ".join(RISKS))
    elif risk not in RISKS:
        problems.append("risk '%s' is not one of: %s" % (risk, ", ".join(RISKS)))

    areas = set()
    if area:
        for name in area.replace(";", ",").split(","):
            name = name.strip()
            if not name:
                continue
            if name not in known_parts:
                problems.append("area '%s' is not a part of this repository. "
                                "Known: %s" % (name, ", ".join(sorted(known_parts))))
            areas.add(name)

    if problems:
        w("BLOCKED - the change intent cannot be used:\n")
        for p in problems:
            w("  - %s\n" % p)
        w("\nNothing was judged. An implementation that cannot say what it is\n"
          "for cannot be reviewed against what it is for.\n")
        return 2

    allowed = set()
    for name in areas:
        allowed |= set(struct.ALLOWED.get(name, set()))
    allowed -= areas

    files = changed_files(args.base, args.staged, args.rev_range)
    if files is None:
        w("BLOCKED: git could not produce a diff.\n")
        for line in sorted(set(LAST_GIT_ERROR)):
            w("  git: %s\n" % line)
        if args.base and any("merge base" in line for line in LAST_GIT_ERROR):
            w("\n  A shallow clone has no shared history to compare against.\n"
              "  Fetch more of it - git fetch --deepen=200 origin %s - or use\n"
              "  --range, or leave the base off to judge the working tree.\n"
              % args.base)
        return 2

    rows = []
    for entry in files:
        rows.append(dict(entry,
                         **{"class": classify(entry["path"], areas, allowed, known_parts)}))

    raised = signals(files, args.base, args.staged, args.rev_range) if files else {}
    owed = obliged(risk, raised)

    # --- what the evidence record says --------------------------------------
    evidence = None
    evidence_error = None
    if args.evidence:
        evidence, evidence_error = read_evidence(args.evidence)

    ran = {}
    regressions = []
    compared = False
    regressed_gates = set()
    if evidence:
        ran = evidence.get("gates", {}) or {}
        regressions = evidence.get("regressions", []) or []
        compared = bool(evidence.get("compared_to"))
        regressed_gates = set((evidence.get("regressed") or {}).get("gates") or [])

    # A GATE THAT DID NOT RUN DID NOT RUN, and until 2026-09-21 this line read
    # `g not in ran` - so a gate the record carries as **NOT RUN** satisfied
    # the obligation and the change was told "every gate it owes was run".
    #
    # THIS TOOL IS THE ONE THAT ASKS WHETHER THE HOMEWORK WAS DONE, and
    # change-evidence.py keeps three states apart precisely so that it can be
    # asked: PASS, FAIL and NOT RUN, with `exit: null` on the third. Reading
    # the presence of the KEY rather than its VALUE collapsed two of them.
    # Found by watching it happen: a capture whose `--tests` argument matched
    # no suite recorded `tests: NOT RUN`, and this printed PASS - "scope
    # matches the declared intent and every gate it owes was run".
    #
    # It is the repository's own standing lesson one layer up: a guard that
    # fails open and says nothing is indistinguishable from a guard that
    # passed. FRAGMENT-ISSUES section 5b.
    unmet = [g for g in owed
             if (ran.get(g) or {}).get("result") not in ("PASS", "FAIL")]
    failed = sorted(g for g, r in ran.items()
                    if isinstance(r, dict) and r.get("result") == "FAIL")

    # A GATE THAT WAS ALREADY FAILING IS NOT THIS CHANGE, and saying otherwise
    # would make the verdict useless on any machine missing an optional
    # dependency: two suites here exit 1 for want of the MCP SDK, so the tests
    # gate reads FAIL on a plain container whatever anybody changed.
    #
    # It is only safe to say that when somebody MEASURED the before state -
    # which is what `compared_to` means. With no comparison there is no way to
    # tell a pre-existing failure from a new one, and the tool says the
    # cautious thing instead of the convenient one.
    #
    # This is the same mechanism .github/workflows/gates.yml already uses: it
    # compares the SET of failing suites against a known-failure list rather
    # than counting them.
    if compared:
        blocking = [g for g in failed if g in regressed_gates]
        pre_existing = [g for g in failed if g not in regressed_gates]
    else:
        blocking, pre_existing = failed, []

    # --- the verdict ---------------------------------------------------------
    unrelated = [r for r in rows if r["class"] == UNRELATED]

    reasons = []
    status = "PASS"

    if regressions:
        status = "REVERT"
        reasons.append("%d thing(s) that worked before this change do not now"
                       % len(regressions))
    elif unrelated:
        status = "SPLIT"
        reasons.append("%d file(s) are in no declared part and in no part a "
                       "declared part may depend on" % len(unrelated))
    elif blocking:
        # Owed or not, a gate that was run and failed is worth stopping for -
        # but the sentence has to say WHICH, because "a gate this change owes"
        # is a different claim from "a gate somebody ran".
        status = "REVISE"
        owed_failing = [g for g in blocking if g in owed]
        other_failing = [g for g in blocking if g not in owed]
        if owed_failing:
            reasons.append("a gate this change owes was run and failed: %s"
                           % ", ".join(owed_failing))
        if other_failing:
            reasons.append("a gate was run and failed, though this change did "
                           "not owe it: %s" % ", ".join(other_failing))
    elif evidence_error:
        status = "REVISE"
        reasons.append("the evidence record could not be read - %s" % evidence_error)
    elif not evidence:
        status = "REVISE"
        reasons.append("no evidence record. Run tools/change-evidence.py and pass "
                       "it with --evidence; a change with no evidence is not a pass")
    elif [g for g in unmet if g != "REAL-REVIT-PROOF"]:
        status = "REVISE"
        reasons.append("gate(s) this change owes were not run: %s"
                       % ", ".join(g for g in unmet if g != "REAL-REVIT-PROOF"))
    elif "REAL-REVIT-PROOF" in unmet:
        status = "NEEDS REAL REVIT PROOF"
        reasons.append("everything answerable without a Revit is answered; a "
                       "fragment's behaviour is not one of those things (D-30)")

    result = {
        "status": status,
        "reasons": reasons,
        "intent": intent,
        "areas": sorted(areas),
        "supporting_parts": sorted(allowed),
        "risk": risk,
        "files": rows,
        "signals": raised,
        "gates_owed": owed,
        # THE ONES THAT ACTUALLY RAN, which is what the line printing this
        # says. It listed every key in the record, so a gate recorded as
        # NOT RUN appeared under "Gates actually run" - the second half of the
        # same defect as `unmet` above, and the half a reader sees.
        "gates_run": sorted(g for g, r in ran.items()
                            if (r or {}).get("result") in ("PASS", "FAIL")),
        "gates_not_run": sorted(g for g, r in ran.items()
                                if (r or {}).get("result") not in ("PASS", "FAIL")),
        "gates_failed": failed,
        "gates_failing_before_too": pre_existing,
        "gates_unmet": unmet,
        "regressions": regressions,
    }

    if args.json:
        w(json.dumps(result, indent=2, sort_keys=True) + "\n")
    else:
        report(result)

    return {"PASS": 0, "SPLIT": 1, "REVISE": 1, "REVERT": 1,
            "NEEDS REAL REVIT PROOF": 3}[status]


def report(result):
    w("Intent:   %s\n" % result["intent"])
    w("Declared: %s   (may depend on: %s)\n"
      % (", ".join(result["areas"]) or "-",
         ", ".join(result["supporting_parts"]) or "nothing"))
    w("Risk:     %s\n\n" % result["risk"])

    counts = collections.Counter(r["class"] for r in result["files"])
    if not result["files"]:
        w("No files changed. Nothing to judge.\n\n")
    else:
        w("%d file(s) changed:\n" % len(result["files"]))
        for name in CLASSES:
            if counts.get(name):
                w("  %-14s %d\n" % (name, counts[name]))
        w("\n")

    for name in CLASSES:
        rows = [r for r in result["files"] if r["class"] == name]
        if not rows or name in (TESTS, DOCS):
            continue
        w("%s:\n" % name)
        for r in rows:
            w("  %s %-58s +%d -%d\n" % (r["status"], r["path"], r["added"], r["deleted"]))
        w("\n")

    if result["signals"]:
        w("Signals:\n")
        for name, body in result["signals"].items():
            w("  %-18s %s\n" % (name, body["why"]))
            for p in body["paths"][:6]:
                w("  %-18s   %s\n" % ("", p))
            if len(body["paths"]) > 6:
                w("  %-18s   ... and %d more\n" % ("", len(body["paths"]) - 6))
        w("\n")

    w("Gates this change owes: %s\n" % ", ".join(result["gates_owed"]))
    if result["gates_run"]:
        w("Gates actually run:     %s\n" % ", ".join(result["gates_run"]))
    if result.get("gates_not_run"):
        w("In the record, NOT RUN: %s\n" % ", ".join(result["gates_not_run"]))
    if result["gates_unmet"]:
        w("Owed and not run:       %s\n" % ", ".join(result["gates_unmet"]))
    if result.get("gates_failing_before_too"):
        w("Failing before too:     %s\n"
          % ", ".join(result["gates_failing_before_too"]))
        w("                        measured on both sides, so not this change\n")
    w("\n")

    w("%s\n" % result["status"])
    for reason in result["reasons"]:
        w("  %s\n" % reason)
    if result["status"] == "PASS":
        w("  Scope matches the declared intent and every gate it owes was run.\n")
    w("\nA clean scope report is not a proof. It says the change stayed where it\n"
      "said it would - not that what it does is right.\n")


if __name__ == "__main__":
    sys.exit(main())
