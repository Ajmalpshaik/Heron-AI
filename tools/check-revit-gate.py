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

import heron_fragment as FRAG                      # noqa: E402

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


def library():
    """Every fragment as (id, folder name, parsed doc, raw yaml text, code).

    THE PARSE IS heron_fragment.load_all()'s, NOT THIS FILE'S. The first
    version read and parsed each fragment.yaml itself with a bare
    `except Exception: continue`, which silently skipped anything malformed -
    so a broken fragment would have been absent from a report that counts
    fragments, and nothing would have said so.

    D-48 is exactly that failure, from the other direction: one malformed
    fragment.yaml once took down all 343 because a YAMLError is not a
    ValueError. The rule it settled is "one broken part costs one part, NEVER
    the whole library" - and the second half of it is that the part is NAMED.
    load_all() returns its problems; this returns them too, and main() prints
    them above everything else.

    The RAW TEXT is read separately and deliberately: `safe_load` discards
    comments, and question 11 looks for a unit stated in one.
    """
    found, problems = FRAG.load_all(FRAGMENTS)
    out = []
    for frag in sorted(found.values(), key=lambda f: f.id):
        folder = frag.folder
        name = os.path.basename(folder)
        raw = ""
        try:
            with open(os.path.join(folder, "fragment.yaml"), encoding="utf-8") as fh:
                raw = fh.read()
        except (OSError, UnicodeDecodeError):
            problems.append("%s: fragment.yaml could not be re-read for its "
                            "comments" % name)
        impl = os.path.join(folder, "impl", "any", "fragment.cs")
        code = None
        if os.path.exists(impl):
            try:
                with open(impl, encoding="utf-8") as fh:
                    code = fh.read()
            except (OSError, UnicodeDecodeError):
                problems.append("%s: impl/any/fragment.cs could not be read"
                                % name)
        out.append((frag.id, name, frag.data, raw, code))
    return out, problems


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


# Calls whose result Revit can hand back as null. A fragment that uses one
# without a guard is the question; a fragment that uses none has nothing to
# guard against, however guard-free it looks.
NULLABLE = (
    (r"\.GetElement\(", "GetElement"),
    (r"\.get_Parameter\(", "get_Parameter"),
    (r"\.LookupParameter\(", "LookupParameter"),
    (r"\bas\s+[A-Z][A-Za-z]*\b", "a cast with `as`"),
    (r"\.Level\b", ".Level"),
    (r"\.GetLinkDocument\(", "GetLinkDocument"),
    (r"\.Symbol\b", ".Symbol"),
)


def _nullable_calls(code):
    """Which nullable calls the CODE uses. Comments stripped, for the fourth
    time in this file and for the same reason."""
    body = "\n".join(line for line in code.splitlines()
                     if not line.strip().startswith("//"))
    body = re.sub(r"/\*.*?\*/", "", body, flags=re.S)
    return [label for pattern, label in NULLABLE if re.search(pattern, body)]


# A class name that names something PROJECT-LEVEL rather than something placed
# in a view. Suffix-matched rather than listed, because the list would go stale
# and the naming convention will not.
DEFINITION_NAME = re.compile(
    r"(Element|Symbol|Type|View|ViewSchedule|Material|Family|Level|Phase|Workset)$")
OF_CLASS = re.compile(
    r"OfClass\(\s*typeof\(\s*([A-Za-z_][A-Za-z0-9_]*)\s*\)\s*\)")


def _definition_classes(code):
    """The project-level classes a collector asks for, or [] if it wants
    instances.

    ONLY `OfClass(typeof(X))` COUNTS, not every `typeof()` in the file.
    Matching all of them excused create-view-filters-by-value for the wrong
    reason - it also writes `typeof(string)` and `typeof(
    ParameterFilterRuleFactory)` for unrelated reflection, and a rule that
    reads those as "collects definitions" would excuse a fragment that
    collects instances the moment it did any reflection at all.
    """
    if not code:
        return []
    squashed = "".join(code.lower().split())
    if "whereelementisnotelementtype" in squashed:
        return []                                  # it wants instances
    classes = OF_CLASS.findall(code)
    if not classes or not all(DEFINITION_NAME.search(c) for c in classes):
        return []
    return sorted(set(classes))


def _can_drop(code):
    """Whether the code can pass over a candidate inside a loop.

    A `continue` or a conditional `.Add(` is what turns a search into something
    that can come back short without saying so. Comments stripped, as
    everywhere else in this file.
    """
    body = "\n".join(line for line in (code or "").splitlines()
                     if not line.strip().startswith("//"))
    body = re.sub(r"/\*.*?\*/", "", body, flags=re.S)
    return bool(re.search(r"\bcontinue\s*;", body)) or ".Add(" in body


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
    whole = code is not None and "newfilteredelementcollector(doc)" in squashed
    scoped = "newfilteredelementcollector(doc," in squashed
    declares_view = "view" in [n.get("name") for n in needs_of(doc)]
    definitions = _definition_classes(code)
    if whole and declares_view and not scoped and definitions:
        # FIVE OF THE SIX WERE THIS, and it is not a judgement call: a fill
        # pattern, a parameter filter, a family symbol and a view are
        # PROJECT-LEVEL. They do not live in a view, so a view-scoped collector
        # would return nothing. Scoping is not merely unnecessary here, it is
        # wrong, and reporting them asked somebody to consider a change that
        # would break the fragment.
        say(ANSWERED,
            "is handed a `view` and collects the whole document for %s, which "
            "is project-level and does not live in a view. A view-scoped "
            "collector would return nothing"
            % ", ".join(definitions))
    elif whole and declares_view and not scoped:
        say(LOOK,
            "is handed a `view` and still collects INSTANCES from the whole "
            "document, never scoping to it. Sometimes right - reporting which "
            "categories the model contains needs the model, not the view - and "
            "worth confirming that is the intent rather than an oversight")
    elif whole:
        # Raising all 114 was raising the shape of the library. A whole-model
        # collector is usually the job. Where it MATTERS is proof speed, and
        # that is said here rather than counted as a finding.
        say(ANSWERED,
            "collects over the whole document, which is usually the job. It is "
            "what makes a PROOF slow though: set-mep-size timed out on 307 "
            "ducts and sized 22 immediately in a smaller view (proving skill, "
            "rule 1). Worth knowing before arranging a case")
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
        # D-59 SETTLED WHAT THIS SHOULD SAY, so it stopped being a design
        # question and became a worklist. Before 2026-09-09 this branch ended
        # "whether that is right is a design question, not a defect - see Q-48".
        # The owner answered: the MODELLER decides, job by job, and the answer
        # reports how many links were actually read.
        #
        # So the check is now specific enough to be wrong, which the old wording
        # was not. Two fields, and the SECOND is the one that matters - an echo
        # of the input says what was asked for, a count says what happened.
        declared = [n.get("name") for n in needs_of(doc)]
        gives = [n.get("name")
                 for n in (doc.get("contract") or {}).get("provides") or []]
        has_in = "includeLinks" in declared
        has_out = "linksSearched" in gives
        if has_in and has_out:
            say(ANSWERED,
                "takes includeLinks and reports linksSearched, which is D-59")
        elif has_in or has_out:
            # HALF IS WORSE THAN NEITHER and this branch exists to say so. A
            # fragment that takes includeLinks and reports nothing has been
            # asked to look in the links and cannot say whether it found any to
            # look in; one that reports linksSearched with no way to ask can
            # only ever report zero.
            say(LOOK,
                "declares %s of D-59's two fields and not the other. Half of "
                "this contract is worse than neither: %s"
                % ("includeLinks" if has_in else "linksSearched",
                   "asked to read links, unable to say whether any were there"
                   if has_in else
                   "reports a link count it has no way to be asked for"))
        else:
            say(LOOK,
                "READS by collecting from the host document and says nothing "
                "about links. In a federated model the elements a modeller can "
                "see are frequently in a link, and a fragment that looks only "
                "in the host returns a confident SMALLER number. D-59 settled "
                "the shape: takes includeLinks (absent means host only), "
                "reports linksSearched. This one has neither yet")

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

    # 12 - null and invalid. Asking "does it guard" raised 7 fragments that
    # never touch anything nullable: count-elements counts a list it was
    # handed, set-selection selects one, group-and-count groups one. The
    # question is whether it dereferences something REVIT CAN HAND BACK NULL
    # FOR without checking, which is a different question with a different
    # answer.
    if not code:
        say(LOOK, "no code to read")
    elif any(g in code for g in ("!= null", "== null", "??", "?.")):
        say(ANSWERED, "guards against null")
    else:
        nullable = _nullable_calls(code)
        if nullable:
            say(LOOK,
                "no null guard of any kind, and it uses %s. Revit hands back "
                "null for a deleted element, an absent parameter, a level that "
                "is not there and a cast that does not hold"
                % ", ".join(nullable))
        else:
            say(ANSWERED,
                "no null guard, and none needed - it touches nothing Revit can "
                "hand back as null, only what it was given")

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

    # 14 - reporting. Asking "does it report a refusal" raised 143 of 360, and
    # the library's own practice says why that is too broad: a fragment that
    # WRITES names what it refused 172 times out of 202 (85%), and one that
    # READS does it 45 times out of 158 (28%). The norm is real and it is not
    # uniform, so the question has to be narrower than the count.
    #
    # It is narrowed to the shape D-52 is actually about: a fragment that GOES
    # LOOKING and can DROP something on the way. One that counts a list it was
    # handed cannot skip anything, however silent it is.
    if not code:
        say(LOOK, "no code to read")
    elif any(w in code.lower()
             for w in ("refused", "skipped", "unresolved", "weakened")):
        say(ANSWERED, "reports what it turned down as well as what it did "
                      "(D-52)")
    elif "filteredelementcollector" in squashed and _can_drop(code):
        # D-64 settled the SHAPE, so this looks at the contract and not only at
        # the code. A dropped-count is a declared output - unresolvedLevel,
        # skipped, refused - and the name varies by fragment because what was
        # dropped varies. The rule is that ONE OF THEM EXISTS, never that a
        # particular word does; a rule about the word would be a naming
        # convention wearing a correctness rule's clothes.
        dropped = [n.get("name") or "" for n
                   in (doc.get("contract") or {}).get("provides") or []]
        names_it = [n for n in dropped
                    if any(w in n.lower() for w in
                           ("unresolved", "skipped", "refused", "dropped",
                            "missing", "failed", "weakened"))]
        if names_it:
            say(ANSWERED,
                "the contract declares %s, so the caller is told what was "
                "dropped even when the code reads silent (D-64)"
                % ", ".join(names_it))
        else:
            say(LOOK,
                "collects, drops candidates in its loop, and names none of "
                "them - not in the code and not in `provides`. D-52: a count "
                "of what was turned down is not a count of what was found. "
                "filter-elements-by-type returns `found: 0` when its exemplar "
                "has no type, and nothing separates that from 'there are none "
                "of this type'. D-64: declare a dropped-count in `provides`, "
                "carried only when the result is empty")
    else:
        say(ANSWERED,
            "names no refusals, and does not go looking - it works on what it "
            "was handed, so there is nothing it could silently drop")

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


def _stood_note(counts, total):
    """What stood in place of a look, for a question nothing flagged.

    Empty when something WAS worth a look - the count says it. Otherwise the
    verdict that covered the library, so a reader can tell "checked and clean"
    from "never checked".
    """
    others = dict((v, n) for v, n in (counts or {}).items() if v != LOOK)
    if not others:
        return ""
    if len(others) == 1:
        only = list(others)[0]
        return "   %s for all %d" % (only, total)
    return "   " + ", ".join("%s %d" % (v, n) for v, n in sorted(others.items()))


def sweep(entries, show=None, out=None):
    write = (out or sys.stdout).write
    findings = {}
    # EVERY VERDICT, NOT JUST THE ONES WORTH A LOOK. The summary below is
    # fourteen rows of one number, and a row reading 0 used to mean three
    # different things at once - measured 2026-09-22 across 396 fragments:
    #
    #   Q11  ANSWERED for all 396    the unit check ran and found nothing
    #   Q9   NEEDS A RUN for all 396 nothing was checked at all
    #   Q13  BY DESIGN for all 396   and this file's own docstring says it
    #                                CANNOT decide that question
    #
    # One presentation, three meanings, in the tool whose docstring insists
    # the four verdicts "describe EVIDENCE, not lifecycle". AGENTS.md is
    # blunter: separate the four states and never merge them. So the row
    # carries the verdict that stood when no fragment was worth a look.
    stood = {}
    for entry in entries:
        for i, (verdict, detail) in enumerate(ask(*entry), 1):
            stood.setdefault(i, {})[verdict] = stood.setdefault(
                i, {}).get(verdict, 0) + 1
            if verdict == LOOK:
                findings.setdefault(i, []).append((entry[0], entry[1], detail))

    write("REVIT VALIDATION GATE - the whole library\n")
    write("=" * 70 + "\n")
    write("%d fragments, %d questions each. The fourteen are the master\n"
          % (len(entries), len(QUESTIONS)))
    write("architecture document's section 14, in its order.\n\n")

    write("%-62s %4s\n" % ("", "look"))
    for i, question in enumerate(QUESTIONS, 1):
        got = findings.get(i, [])
        write("%2d. %-58s %4d%s\n"
              % (i, question[:58], len(got), _stood_note(stood.get(i), len(entries))))
    write("\n")
    write("A 0 with a verdict beside it is what stood INSTEAD of a look:\n")
    write("  ANSWERED     read and nothing was found\n")
    write("  BY DESIGN    the architecture answers it; there is nothing to find\n")
    write("  NEEDS A RUN  nothing was checked - only a real model can say\n")
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
    entries, problems = library()
    if problems:
        print("FRAGMENTS THAT COULD NOT BE READ  (%d)" % len(problems))
        print("-" * 70)
        for line in problems:
            print("  %s" % line)
        print("  D-48: one broken part costs one part, never the whole")
        print("  library - and the part is NAMED rather than skipped.")
        print("")
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
