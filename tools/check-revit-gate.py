# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
The Revit validation gate, run as a list instead of remembered.

    python tools/check-revit-gate.py                    the whole library
    python tools/check-revit-gate.py FRG-ELE-001        one fragment, all 14
    python tools/check-revit-gate.py --list collectors  the names behind a count

WHY. The fourteen questions are already this project's rules. They are spread
across docs/03, the fragment-proving skill, D-51, D-53 and FRAGMENT-ISSUES.md,
and NOTHING RAN THEM AS A LIST - docs/32 s4.3. The proving skill names five
mistakes that account for nearly every failed proof, and five of them are five
of these fourteen, which is the evidence that asking them in order pays.

WHAT THIS IS NOT
----------------
**It is not a proof and it cannot become one.** D-30 needs a positive case, a
negative case and a fingerprint, against a real model. Everything here is read
off files.

**It is not a gate. It exits 0.** A finding here is a question for a person,
and a tool that failed a build over "this collector has no view" would teach
people to write worse collectors to buy a green tick.

**IT CANNOT DECIDE WHETHER A FRAGMENT WRITES, and pretending otherwise was the
first design.** That attempt is worth recording because the failure is
instructive rather than embarrassing:

  * Searching for `.Create(` flagged three READ fragments - check-equipment-
    clearance, check-valve-accessibility, select-in-region. **All three were
    wrong.** `CurveLoop.Create`, `Line.CreateBound` and
    `GeometryCreationUtilities.CreateExtrusionGeometry` build geometry in
    MEMORY and never touch the document. In the Revit API "Create" is not a
    write signal.
  * Narrowing to calls that take `doc` then found ZERO mislabelled fragments -
    and missed 76 MODIFY fragments, because Revit writes through typed methods
    on typed objects: `view.HideElements(ids)`, `view.Scale = 2`,
    `param.Set(v)`. The write surface is the API, and no word list is the API.

**The real answer to question 13 is not static and already exists.** D-28's
executor runs a READ fragment with NO TRANSACTION OPEN, so Revit itself refuses
any model change - enforced by the host rather than asserted by a checker. That
is a better answer than any text search could give, and this tool says so
instead of competing with it.

WHAT IT DOES INSTEAD
--------------------
Each question gets one of four honest verdicts, and they describe EVIDENCE, not
lifecycle - docs/24's two axes are untouched and this adds no third vocabulary:

    ANSWERED     read from the fragment's own declared data or its compile record
    BY DESIGN    the architecture answers it for every fragment; the reason is named
    LOOK         something a person should look at, with the reason it was raised
    NEEDS A RUN  only a real model can answer it, and the tool says which tool runs it
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

FRAGMENTS = os.path.join(ROOT, "brain", "fragments")

ANSWERED = "ANSWERED"
BY_DESIGN = "BY DESIGN"
LOOK = "LOOK"
NEEDS_RUN = "NEEDS A RUN"

# The master architecture document's section 14, verbatim in order.
QUESTIONS = [
    "Which Revit versions are targeted?",
    "Is the API available in those versions?",
    "Is the document context correct?",
    "Is a transaction required?",
    "Is transaction scope minimal?",
    "Is the code touching the Revit API from a safe execution context?",
    "Are collectors appropriately scoped?",
    "Are linked documents handled correctly?",
    "Are element IDs/references stable for the intended operation?",
    "Are geometry operations version-safe?",
    "Are units handled correctly for the target version?",
    "Are null/deleted/invalid elements handled?",
    "Could the change unexpectedly modify the model?",
    "Is rollback/error reporting clear?",
]

# Words that state what a number MEANS. A length with none of these is the
# defect this catches: a caller reads "how far above the level" and passes
# 2700 thinking millimetres, and Revit reads 2700 feet.
UNIT_WORDS = ("internal feet", "internal unit", "feet", "millimet", " mm",
              "unitutils", "forgetypeid", "radian", "percent", "degree",
              "304.8", "m2", "m3", "ratio", "0 to 1", "0-1", "fraction",
              "normalis", "normaliz")


def load_yaml():
    try:
        import yaml
    except ImportError:
        sys.stderr.write("This needs PyYAML: pip install --user pyyaml\n")
        raise SystemExit(2)
    return yaml


def library():
    """Every fragment as (id, folder, doc, source-or-None)."""
    yaml = load_yaml()
    out = []
    for name in sorted(os.listdir(FRAGMENTS)):
        folder = os.path.join(FRAGMENTS, name)
        path = os.path.join(folder, "fragment.yaml")
        if not os.path.exists(path):
            continue
        with open(path, encoding="utf-8") as fh:
            raw = fh.read()
        try:
            doc = yaml.safe_load(raw)
        except Exception:
            continue
        if not doc or not doc.get("id"):
            continue
        impl = os.path.join(folder, "impl", "any", "fragment.cs")
        code = None
        if os.path.exists(impl):
            with open(impl, encoding="utf-8") as fh:
                code = fh.read()
        out.append((doc["id"], name, doc, raw, code))
    return out


def needs_of(doc):
    contract = doc.get("contract") or {}
    return contract.get("needs") or []


# The names a fragment gets from the executor rather than from its contract.
DOCUMENT_NAMES = ("doc", "uidoc", "app")


def _document_names_used(code):
    """Which document names the CODE actually uses.

    COMMENTS ARE STRIPPED FIRST, and that is not fussiness. Three checks in
    this repository have now been fooled by text ABOUT the thing rather than
    the thing - a space-stripped haystack, a tool matching its own docstring,
    and a string literal that made its own subject invisible. A fragment's
    header comment says "Assumes `doc` ... are in scope" in almost every file,
    so a check that read comments would find `doc` everywhere and mean nothing.
    """
    if not code:
        return []
    body = "\n".join(line for line in code.splitlines()
                     if not line.strip().startswith("//"))
    body = re.sub(r"/\*.*?\*/", "", body, flags=re.S)

    used = []
    for name in DOCUMENT_NAMES:
        if not re.search(r"\b%s\b" % name, body):
            continue
        # A LOCAL IS NOT AN UNDECLARED NEED, and this cost the check its last
        # false positive. `zoom-to-elements` declares `uidoc` and then writes
        #     var doc = uidoc.Document;
        # which is correct code deriving one from the other. Reported as an
        # undeclared need it would have sent somebody to edit a working
        # contract. Fourth time in one night that a text check has been fooled
        # by something that merely LOOKS like its subject.
        if re.search(r"\b(?:var|Document|UIDocument|UIApplication)\s+%s\s*="
                     % name, body):
            continue
        used.append(name)
    return used


def ask(fid, name, doc, raw, code):
    """The fourteen answers for one fragment, in order."""
    answers = []
    text = ((raw or "") + "\n" + (code or "")).lower()

    def say(verdict, detail):
        answers.append((verdict, detail))

    # 1 - which releases
    releases = doc.get("revit") or []
    say(ANSWERED, "declares %d release(s): %s"
        % (len(releases), ", ".join(str(r) for r in releases) or "none"))

    # 2 - is the API there
    say(ANSWERED if code else LOOK,
        "the compile gate answers this - tools/check-fragments-compile.py "
        "builds every fragment against every declared release's reference "
        "assemblies. Run `python brain/heron_matrix.py --fragments` for the "
        "recorded state" if code else
        "there is no impl/any/fragment.cs, so no compiler has read anything")

    # 3 - document context. The question is not "does it declare one" but
    # "does it USE one it did not declare", and the difference is 42 rows.
    #
    # Asking the first raised all 42 fragments that legitimately work on what
    # they are handed - apply-view-template takes views and a templateId and
    # needs no document at all. Every one of the 42 was correct, so the check
    # was crying wolf on the whole list. Asking the second raises none of them
    # and would still catch a real undeclared need, which is what D-29 made the
    # contract DATA in order to make askable.
    names = [n.get("name") for n in needs_of(doc)]
    context = [n for n in names if n in ("doc", "uidoc", "view", "app")]
    used = _document_names_used(code)
    undeclared = [n for n in used if n not in names]
    if undeclared:
        say(LOOK,
            "the code uses %s and the contract declares %s. An undeclared need "
            "cannot be bound, and D-29 made the contract data so this is "
            "checkable rather than remembered"
            % (", ".join(undeclared), ", ".join(names) or "nothing"))
    elif context:
        say(ANSWERED, "the contract declares %s" % ", ".join(context))
    elif code:
        say(ANSWERED,
            "declares no document and uses none - it works on what it is "
            "handed, which is a filter or an action composed after one")
    else:
        say(LOOK, "no code to read")

    # 4 and 5 - transactions
    opens_own = code is not None and "new transaction(" in code.lower()
    if opens_own:
        say(LOOK,
            "this fragment opens its OWN Transaction. Golden Rule 16 gives the "
            "whole job one TransactionGroup and one undo; a fragment opening "
            "its own breaks that, and no other fragment in the library does")
    else:
        say(BY_DESIGN,
            "no fragment opens a transaction. RevitFragment.Run decides: a "
            "read runs with none open at all, and a write runs inside ONE "
            "TransactionGroup named for the fragment, so one Ctrl+Z puts the "
            "model back (Golden Rule 16)")
    say(BY_DESIGN, "same answer as 4 - the scope is the executor's, not the "
                   "fragment's, and no fragment can widen it")

    # 6 - execution context
    say(BY_DESIGN,
        "D-09 and D-28: one ExternalEvent, one queue, one handler. A fragment "
        "cannot reach the API from anywhere else, so this is not a property of "
        "the fragment to get wrong")

    # 7 - collectors
    squashed = "".join((code or "").lower().split())
    if code and "newfilteredelementcollector(doc)" in squashed:
        say(LOOK,
            "collects over the WHOLE document. Often right, and it is what "
            "makes a proof slow: set-mep-size timed out on 307 ducts and sized "
            "22 immediately in a smaller view (proving skill, rule 1). Worth "
            "knowing before arranging a case, not necessarily worth changing")
    elif code:
        say(ANSWERED, "no unscoped whole-document collector")
    else:
        say(LOOK, "no code to read")

    # 8 - links
    collects = "filteredelementcollector" in squashed
    reads = doc.get("risk") in ("READ", "ANALYZE", "SUGGEST")
    if "revitlinkinstance" in text or "linked" in text:
        say(ANSWERED, "the fragment names linked documents, so the case was "
                      "considered")
    elif not collects:
        # This asked every fragment and raised 310 of 360, which is the shape of
        # the library rather than a finding. A fragment that sets a view's scale
        # has no link question to get wrong; only one that GOES LOOKING for
        # elements can miss the ones in a link.
        say(ANSWERED, "collects nothing from the document, so there is no link "
                      "question here to answer")
    elif not reads:
        # A LINKED ELEMENT BELONGS TO ANOTHER DOCUMENT and cannot be changed
        # through the host - you would have to open the linked file. So a
        # writer collecting the host only is not under-reaching the way a
        # reader is, and raising it put 45 fragments on a list they could do
        # nothing about.
        say(ANSWERED,
            "collects and says nothing about links, and is declared %s. A "
            "linked element belongs to another document and cannot be changed "
            "through this one, so there is nothing here to miss"
            % doc.get("risk"))
    else:
        say(LOOK,
            "READS by collecting from the host document and says nothing about "
            "links. In a federated model the elements a modeller can see are "
            "frequently in a link, and a fragment that looks only in the host "
            "returns a confident SMALLER number. Whether that is right is a "
            "design question, not a defect - see Q-48")

    # 9 - stable ids
    say(NEEDS_RUN,
        "only a run says whether a reference survived. tools/batch-prove.py "
        "with a negative case is what asks it (D-30, D-51)")

    # 10 - geometry version safety
    say(ANSWERED if code else LOOK,
        "the compile gate again, per release. docs/16 lists the breaks that "
        "matter - ElementId at 2024, the dimension subclasses at 2025")

    # 11 - units. The one static check that found something.
    doubles = [n.get("name") for n in needs_of(doc)
               if n.get("type") == "double"]
    if not doubles:
        say(ANSWERED, "takes no double, so there is no unit to get wrong")
    elif any(word in text for word in UNIT_WORDS):
        say(ANSWERED, "takes %s and states what the number means"
            % ", ".join(doubles))
    else:
        say(LOOK,
            "takes %s and NEVER SAYS WHAT UNIT. Revit's internal length is "
            "feet: a caller who reads the description and passes 2700 for "
            "millimetres gets 2700 feet, and nothing refuses it. This is D3's "
            "failure shape" % ", ".join(doubles))

    # 12 - null and invalid
    if code and any(g in code for g in ("!= null", "== null", "??", "?.")):
        say(ANSWERED, "guards against null")
    elif code:
        say(LOOK,
            "contains no null guard of any kind. Revit hands back null for a "
            "deleted element, an absent parameter and a level that is not "
            "there")
    else:
        say(LOOK, "no code to read")

    # 13 - unexpected modification
    risk = doc.get("risk")
    reader = risk in ("READ", "ANALYZE", "SUGGEST")
    if reader:
        say(BY_DESIGN,
            "declared %s, and a read runs with NO TRANSACTION OPEN - so Revit "
            "itself refuses any model change. Enforced by the host, not "
            "asserted here" % risk)
    else:
        say(BY_DESIGN,
            "declared %s, so YES - by design, and that is the point of the "
            "declaration. What stands in front of it is not this fragment: "
            "`write.enabled` defaults to false (D-19), the permission gate is "
            "in the add-in where nothing on the Python side can talk past it, "
            "and without an explicit `apply` the run happens and is ROLLED "
            "BACK - D-55 makes the preview the run itself, so a caller who "
            "forgets the flag gets the safe half"
            % (risk or "no risk"))
    say_extra = ("No text search can decide whether a fragment writes, and this "
                 "tool does not try: in the Revit API 'Create' builds geometry "
                 "as often as it builds an element, and Revit writes through "
                 "typed methods - view.HideElements(ids), view.Scale = 2 - "
                 "which no word list is.")
    answers[-1] = (answers[-1][0], answers[-1][1] + ". " + say_extra)

    # 14 - reporting
    if code and any(w in code.lower()
                    for w in ("refused", "skipped", "unresolved", "weakened")):
        say(ANSWERED, "reports what it turned down as well as what it did "
                      "(D-52)")
    elif code:
        say(LOOK,
            "reports no refusals. D-52: a count of what was turned down is not "
            "a count of what was found, and a fragment that silently skips "
            "looks identical to one that found nothing")
    else:
        say(LOOK, "no code to read")

    return answers


def one(fid, entries, out=None):
    write = (out or sys.stdout).write
    for entry in entries:
        if entry[0] != fid:
            continue
        answers = ask(*entry)
        write("REVIT VALIDATION GATE - %s (%s)\n" % (fid, entry[1]))
        write("=" * 70 + "\n")
        for i, (question, (verdict, detail)) in enumerate(
                zip(QUESTIONS, answers), 1):
            write("%2d. %s\n" % (i, question))
            write("    %-12s %s\n" % (verdict, detail))
            write("\n")
        return 0
    write("No fragment with id %s. Ids look like FRG-ELE-001.\n" % fid)
    return 2


def sweep(entries, show=None, out=None):
    write = (out or sys.stdout).write
    findings = {}
    for entry in entries:
        for i, (verdict, detail) in enumerate(ask(*entry), 1):
            if verdict == LOOK:
                findings.setdefault(i, []).append((entry[0], entry[1], detail))

    write("REVIT VALIDATION GATE - the whole library\n")
    write("=" * 70 + "\n")
    write("%d fragments, %d questions each. The fourteen are the master\n"
          % (len(entries), len(QUESTIONS)))
    write("architecture document's section 14, in its order.\n\n")

    labels = {LOOK: "worth a look"}
    for i, question in enumerate(QUESTIONS, 1):
        got = findings.get(i, [])
        write("%2d. %-58s %4d\n" % (i, question[:58], len(got)))
    write("\n")

    # Small lists are printed in full; a big one is a spread rather than a
    # defect list and is named only on request. A tool that dumps 143 rows
    # teaches people to scroll past it.
    for i in sorted(findings):
        got = findings[i]
        key = KEYS.get(i, str(i))
        if len(got) > 20 and show != key:
            write("Q%d - %d fragment(s). Too many to be a defect list; this is\n"
                  % (i, len(got)))
            write("     a spread. `--list %s` names them.\n\n" % key)
            continue
        write("Q%d - %s\n" % (i, QUESTIONS[i - 1]))
        write("-" * 70 + "\n")
        for fid, name, detail in got:
            write("  %-14s %s\n" % (fid, name))
            write("                 %s\n" % detail)
        write("\n")

    write("Not a gate; exits 0. A finding is a question for a person.\n")
    write("None of it is a proof - D-30 needs a real model, a negative case\n")
    write("and a fingerprint, and nothing here has met one.\n")
    return 0


# Short names for --list, so a reader types a word rather than a number.
KEYS = {
    2: "compile", 3: "context", 4: "transaction", 7: "collectors",
    8: "links", 11: "units", 12: "nulls", 14: "reporting",
}


def main(argv):
    entries = library()
    if not entries:
        print("No fragments found under brain/fragments.")
        return 2

    show = None
    if "--list" in argv:
        show = argv[argv.index("--list") + 1]
        argv = [a for a in argv if a != "--list" and a != show]

    wanted = [a for a in argv if a.startswith("FRG-")]
    if wanted:
        return one(wanted[0], entries)
    return sweep(entries, show=show)


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
