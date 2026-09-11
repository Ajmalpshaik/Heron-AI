# Heron-Agent:  HERON-RAG-CNF-015
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Two scopes answering one question with different numbers. Said out loud, never settled.

    python brain/heron_conflict.py "how thick should duct insulation be" \
        --scopes company,project --project "Tower B"

Stage 8 of docs/work-notes/plans/rag/00-structure.md s6, and the half of it that
can be built. Closes R-24.

WHAT THIS IS FOR, IN ONE CASE THAT ALREADY EXISTS
--------------------------------------------------
Stage 4 built the Librarian and left this sitting in its own test:

    company            Acme Engineering BIM Standard 2026  3.1   insulate to 30mm
    project (Tower B)  Tower B Project Specification       3.1   insulate to 40mm

**Two different thicknesses, both correctly reported, and nothing saying they
disagree.** A modeller reading that answer sees two clauses and has to notice the
numbers themselves. On a bad afternoon they do not, and 30mm goes on a wall that
the project specified at 40.

docs/20 s4 is blunt about what this costs if it is left to ranking: two sources
disagree, retrieval returns whichever ranked higher that day, and the knowledge
base becomes quietly non-deterministic. In a consultancy that is the difference
between a tool and a liability.

IT SURFACES AND IT NEVER RESOLVES, AND THAT IS THE WHOLE DESIGN
----------------------------------------------------------------
R-24: two sources disagreeing is **surfaced and asked about**, not silently
resolved by rank. So this module adds a sentence and removes nothing. Both
clauses still come back, still labelled with the scope they came from, still
cited. Deciding which governs is the host's act and, past the host, a person's -
the same boundary D-01 draws everywhere else.

**The hierarchy is reported and NOT applied.** docs/20 s2 orders knowledge -
project above company, company above user, and so on - and then says the thing
that matters: *"overrides must not mean silently replaces"*. So the report says
which one that ordering would weigh higher and says plainly that Heron has not
acted on it. A reader who wants the project's number can have it; they cannot
get it without being told a choice existed.

WHAT CROSSES THE WALL, AND IT IS DELIBERATELY SMALL
-----------------------------------------------------
Golden Rule 5 and D-33: scopes are separate files and a cross-scope query cannot
be written. `heron_retrieve.librarian()` keeps that by opening each store on its
own and never comparing a score from one with a score from another.

A disagreement cannot be seen without SOMETHING crossing. What crosses here is a
**number and a clause number** - `40mm at 3.1` - and never a clause. Each store
is opened, asked, reduced to its measured values, and closed before the next is
opened. No scope's text is ever held beside another's.

WHAT IT CANNOT TELL, WRITTEN HERE RATHER THAN DISCOVERED LATER
---------------------------------------------------------------
**It does not know whether the two clauses are about the same requirement.**
"Insulation 25mm" and "clearance 40mm" are different subjects and this would
report them as a disagreement. Deciding sameness is meaning, and this module has
no model and no network - the same limit heron_ground.py lives by (R-47).

So the report says what it actually observed: *these two answers to one question
carry different values of the same unit*. A person decides whether they are the
same requirement. Reporting more than that would be inventing the half it cannot
see, which is what R-24 exists to prevent.

IT INHERITS THE MISSING FLOOR, AND THAT IS THE BIGGEST THING TO KNOW
---------------------------------------------------------------------
Retrieval has no floor. R-56 would drop a candidate with no real claim on the
question and R-58 would refuse a question nothing covers, and BOTH ARE BLOCKED
(W-8): the measurement that would set the floor does not separate a BIM question
from a question about cats on the backend this runs on, and R-60 says a floor
comes from a measurement or it is not set at all.

So a question neither scope covers still returns five clauses from each, and
this module will faithfully report that their numbers differ. **Found by the
test that expected silence and did not get it**, which is the right way round.

Nothing here invents a floor to paper over that. What it does instead is carry
the measurements retrieval already makes, and that took two goes.

The first version carried `Contest.words_selected_nothing` - the words route
matched at least as much as the filter left, so it RANKED the scope rather than
choosing from it. On the test corpus it fired on the REAL question and stayed
silent on the irrelevant one, which is exactly backwards. The reason is already
written down one class up: below a pool of twenty, `pool_is_evidence` is false
and none of those counts means anything yet. Two chunks in a scope will always
look either "matched everything" or "matched nothing" for reasons that are about
the corpus size and not about the question.

So the caveat is gated on Contest's own existing comparison. **Where the pool is
too small to be evidence, that is what the report says** - not a guess dressed
as a finding. Where it is big enough, the route measurement is carried. Neither
is a new number: both are comparisons of counts that `Contest` already makes,
which is why R-60 has nothing to bite on.

ONLY QUANTITIES ARE COMPARED
-----------------------------
A fact with a unit - 30mm, 2.5m, 50pct, 1:100. NOT a clause number, a year, a
Revit category, a parameter name or a bare count.

The reason is noise rather than principle. heron_ground.facts() pulls "9" and
"1" out of "clause 9.1.1", and two scopes citing different clause numbers is not
a disagreement about anything. **Over-flagging teaches people to ignore flags**,
which is heron_ground's own recorded lesson about R-50, and a missed
disagreement is recoverable because both clauses come back either way.

WHAT IS NOT BUILT, AND WHY IT IS A QUESTION RATHER THAN A GAP
---------------------------------------------------------------
docs/20 s4 asks for detection **at write time** as well: when a document is
ingested, compare it against what already covers the same ground.

At write time there is no question yet, so the only way to find a conflict is to
read ANOTHER SCOPE while ingesting into this one - a crossing nothing currently
authorises. The Librarian's crossing happens because a host asked one question of
two named scopes; an ingest has asked nothing of anybody. That is a contractual
question (D-33) and not a coding one, so it is recorded in the working note for
the owner rather than decided here.

And the trust half of Stage 8 - R-16 and R-25, a score combining accuracy,
freshness, usage and success rate - is **blocked on Q-C**: two of its four
signals do not exist because nothing records them, and whether Heron should keep
a usage log at all is the owner's call.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_scope as SCOPE                                   # noqa: E402
import heron_search as SEARCH                                 # noqa: E402
import heron_ground as GROUND                                 # noqa: E402
import heron_retrieve as RETRIEVE                             # noqa: E402

# THE ORDER docs/20 s2 SETS OUT, mapped onto the scopes this repository has.
#
# "Project-specific knowledge overrides generic knowledge where appropriate" -
# a project's own standard beats the company default, which beats a stranger's
# fragment. It is how a competent engineer weighs sources.
#
# REPORTED, NEVER APPLIED. docs/20 s2's own caveat is the load-bearing half:
# "overrides must not mean silently replaces". Nothing in this file sorts,
# drops or prefers anything - the order is printed so a reader knows what a
# choice would look like, and knows that nobody has made it.
#
# `community` from that list has no scope here, so it is absent rather than
# guessed at.
HIERARCHY = (SCOPE.PROJECT, SCOPE.COMPANY, SCOPE.USER, SCOPE.GLOBAL,
             SCOPE.EXPERIMENTAL, SCOPE.TEMPORARY)

# A gradient first: "1in100" would otherwise read as 1 inch followed by 100.
_GRADIENT = re.compile(r"^(\d+)in(\d+)$")
_QUANTITY = re.compile(r"^(-?\d+(?:\.\d+)?)\s*([a-z]+[0-9]?)$")

def quantity(fact):
    """(unit, value) for a measured fact, or None for anything else.

    None is the common answer and not a failure: a clause number, a year, a
    bare count and a category all arrive here and all of them mean "there is
    nothing to disagree about".
    """
    gradient = _GRADIENT.match(fact or "")
    if gradient:
        # A gradient is its own unit - 1:100 and 1:80 are comparable with each
        # other and with nothing else.
        return "gradient", "%s:%s" % gradient.groups()

    # A NUMBER FIRST, THEN LETTERS. That one shape is the whole filter, and it
    # is why there is no list of things to exclude here: a clause number
    # ("9.1.1") has no trailing letters, and a category ("ost_ductcurves") or a
    # nominal bore ("dn50") begins with them. The first version carried an
    # exclusion list for those two and it was DEAD CODE - they never reached
    # it. Checked by running them through, not by reading.
    got = _QUANTITY.match(fact or "")
    if not got:
        return None
    value, unit = got.groups()
    return unit, value


def same_number(value):
    """A value canonicalised for COMPARISON. The spelling is kept for display.

    "30" and "30.0" are one measurement written two ways, and comparing the
    raw strings reported a disagreement between two sources that agree - a
    flag on nothing, which is the one thing that reliably teaches people to
    stop reading flags. Found by a review 2026-09-11.

    The display spelling is never touched: a clause that says 30.0mm is quoted
    back saying 30.0mm, because the report's job is to show what each source
    actually wrote.
    """
    try:
        number = float(value)
    except (TypeError, ValueError):
        return value
    return "%g" % number


def _flat(value):
    """A document-derived string, safe to put on a line that does not quote it.

    The same treatment heron_context.as_metadata gives a packet field, kept
    here rather than imported because this module answers a command line with
    no MCP server and no packet anywhere near it. Whitespace collapses so the
    value cannot break out of its line, a delimiter marks it as somebody
    else's words, and the length is left exactly as it was.
    """
    flat = re.sub(r"\s+", " ", str(value or "")).strip()
    return "\u00ab%s\u00bb" % flat if flat else ""


class Value(object):
    """One number a scope's answer carried, and enough to go and look at it."""

    def __init__(self, scope, label, unit, value, document, locator, path,
                 chunk, claimed=True, pool_is_evidence=True,
                 document_id=None):
        # WHETHER THE WORDS ROUTE ACTUALLY SELECTED THIS SHORTLIST, or merely
        # ranked the whole scope - and whether that question can be answered at
        # this corpus size at all. Both come from Contest, and the second one
        # governs: below a pool of twenty the first is noise about the corpus.
        self.claimed = claimed
        self.pool_is_evidence = pool_is_evidence
        self.scope = scope
        self.label = label            # the Librarian's label, project included
        self.unit = unit
        self.value = value
        self.document = document      # the title - for a person to read
        self.document_id = document_id  # the identity - for grouping
        self.locator = locator
        self.path = path
        self.chunk = chunk

    @property
    def source(self):
        """Scope and document together - what R-24 means by a source.

        Two standards in one company store are two sources. One standard is
        one source however many of its clauses come back.

        BY DOCUMENT ID, NOT BY TITLE. Keyed on the title, two different
        documents that happen to share one - two projects' copies of one
        template, an old and a new edition both offerable - counted as a
        single source, their values were merged into one `says` entry and the
        disagreement between them was dropped. That is the same failure this
        property was written to fix, one level in. The title stays for
        display and the id does the matching. Found by a review 2026-09-11.

        Falls back to the title when no id came through, because a source
        that cannot be told apart is better than a source that vanishes.
        """
        return (self.label, self.document_id or self.document or "")

    def __repr__(self):
        return "<%s %s%s %s %s>" % (self.label, self.value, self.unit,
                                    self.document, self.locator)


class Disagreement(object):
    """One unit, two or more scopes, and values that are not the same."""

    def __init__(self, unit, values):
        self.unit = unit
        self.values = values

    @property
    def sources(self):
        """The distinct SOURCES, which are documents and not scopes.

        R-24 says two SOURCES disagreeing. The first version grouped by scope
        label, so two company standards giving 30mm and 40mm were treated as
        one source and the disagreement was silently dropped - while the
        comment beside it claimed it was excluding one DOCUMENT with two
        clauses in it. The comment described one thing and the code did
        another. Found by a review 2026-09-11.
        """
        out = []
        for value in self.values:
            if value.source not in out:
                out.append(value.source)
        return out

    @property
    def scopes(self):
        out = []
        for value in self.values:
            if value.label not in out:
                out.append(value.label)
        return out

    @property
    def same_locator(self):
        """Whether every value sits at the same clause number.

        NOT A THRESHOLD AND NOT A VERDICT - an observation, reported because it
        is the difference between a reader glancing and a reader stopping. Two
        documents both numbering a clause 3.1 and giving it different values is
        a far stronger signal than two unrelated clauses that happen to carry
        millimetres. It is still not proof they are the same requirement.
        """
        seen = set(v.locator for v in self.values if v.locator)
        return len(seen) == 1 and len(self.values) > 1

    @property
    def would_be_preferred(self):
        """The label docs/20 s2's hierarchy would weigh highest. NOT applied.

        None when every source is in the SAME scope, and that is not a
        shortcoming - docs/20 s2 orders scopes, and it has nothing whatever to
        say about two standards sitting in one company store. Printing
        "the hierarchy would weigh company highest" when both sides ARE company
        is a sentence that looks like guidance and carries none.
        """
        if len(set(v.scope for v in self.values)) < 2:
            return None
        ranked = [v for v in self.values if v.scope in HIERARCHY]
        if not ranked:
            return None
        return min(ranked, key=lambda v: HIERARCHY.index(v.scope)).label

    def sentence(self):
        """The disagreement in words, and the word for what it is not.

        THE TITLE AND THE CLAUSE NUMBER ARE DOCUMENT-DERIVED TEXT, and these
        lines do not quote them. A title carrying a newline and a speaker
        label would sit in this report looking like Heron talking - Golden
        Rule 19 undone by a column. Collapsed to one line and delimited here,
        the same way heron_context.as_metadata does it for a packet, and
        NOTHING is trimmed (R-82).
        """
        said = ["THESE ANSWERS DISAGREE, and Heron has not decided between them:"]
        for value in self.values:
            said.append("  %-18s %6s%-8s %s %s"
                        % (value.label, value.value, self.unit,
                           _flat(value.document), _flat(value.locator)))
        if self.same_locator:
            said.append("  Both sit at clause %s, which makes it likelier they "
                        "are the same requirement - but Heron cannot tell, and "
                        "does not claim to."
                        % _flat(self.values[0].locator))
        else:
            said.append("  They sit at different clause numbers, so they may "
                        "well be about different things. Heron compared the "
                        "UNITS, not the subjects.")
        if not all(v.pool_is_evidence for v in self.values):
            said.append("  A SCOPE HERE HOLDS FEWER CLAUSES THAN THE POOL, so "
                        "nothing can yet say whether these clauses had a claim "
                        "on your question - every clause in a small scope comes "
                        "back for every question. The numbers above are real; "
                        "whether they ANSWER what you asked is not established.")
        elif not any(v.claimed for v in self.values):
            said.append("  NEITHER SHORTLIST WAS SELECTED BY THE WORDS ROUTE - "
                        "it matched at least as much as the filter left, so it "
                        "ranked each scope rather than choosing from it. These "
                        "clauses may have no claim on your question at all. "
                        "Retrieval cannot yet refuse a question nothing covers "
                        "(R-56, R-58, blocked on W-8).")
        elif not all(v.claimed for v in self.values):
            said.append("  ONE of these shortlists was not selected by the "
                        "words route - it ranked its whole scope rather than "
                        "choosing from it, so that side may have no claim on "
                        "the question (W-8).")
        preferred = self.would_be_preferred
        if preferred is None and len(set(v.scope for v in self.values)) < 2:
            said.append("  THESE ARE BOTH IN ONE SCOPE, so the docs/20 s2 "
                        "hierarchy has nothing to say about them - it orders "
                        "scopes, not the documents inside one. Two standards "
                        "in one store disagreeing is a question for whoever "
                        "keeps that store.")
        if preferred:
            said.append("  docs/20 s2 would weigh %s highest of these. THAT "
                        "ORDERING HAS NOT BEEN APPLIED - both clauses are "
                        "returned, and choosing is yours." % preferred)
        return "\n".join(said)

    def __repr__(self):
        return "<disagree %s %s>" % (self.unit,
                                     " vs ".join(v.value for v in self.values))


def _values_from(store, asked):
    """Every measured value one scope's answer carried. The store stays here.

    THE ANSWER IS THE LIBRARIAN'S, NOT A SECOND ONE. The first version asked
    each scope again from in here, so every scope was searched twice - wasted
    work, and worse, a second source of truth that could one day disagree with
    the first about what the shortlist was. The Librarian has already asked;
    this only needs the clause TEXT, which is why the store is reopened at all.

    THE STORE IS OPENED A SECOND TIME AND THAT IS DELIBERATE. librarian()
    closes each store before it returns, and that closing is part of what keeps
    nothing pooled - handing a live store back out would undo it. Reopening a
    local SQLite file is cheap; reopening the QUESTION would not be, and is not
    what happens.

    The answer, its clauses and their text never leave this function - only
    Value objects, which hold a number and a pointer. That is the whole of what
    crosses between scopes.
    """
    answer = asked.answer
    if answer is None or answer.route in ("empty", "unindexed", "nothing"):
        return []

    # THE MEASUREMENTS RETRIEVAL ALREADY MAKES, rather than a floor invented
    # here. Both are comparisons of counts that Contest makes anyway, not
    # thresholds - R-60 has nothing to bite on.
    contest = getattr(answer, "contest", None)
    claimed = not (contest is not None and contest.words_selected_nothing)
    evidence = contest is None or contest.pool_is_evidence

    out = []
    for hit in answer.candidates:
        row = store.execute("SELECT text FROM chunks WHERE id = ?",
                            (hit["id"],)).fetchone()
        if row is None:
            continue
        for fact in GROUND.facts(row["text"]):
            got = quantity(fact)
            if got is None:
                continue
            unit, value = got
            out.append(Value(asked.scope, asked.label, unit, value,
                             hit.get("document"), hit.get("locator"),
                             hit.get("path"), hit["id"],
                             claimed=claimed, pool_is_evidence=evidence,
                             document_id=hit.get("document_id")))
    return out


def disagreements(text, scopes=None, project=None, limit=5, asked=None):
    """Ask each scope on its own, and report where their numbers differ.

    ONE STORE IS OPEN AT A TIME. Each is opened, asked, reduced to values and
    closed before the next is opened, so no scope's text is ever in memory
    beside another's. The comparison at the end is over numbers.

    `asked` IS THE LIBRARIAN'S ANSWER WHEN A CALLER ALREADY HAS ONE, and
    passing it is not only an optimisation. A caller that displays the clauses
    and then calls this got TWO searches of every scope - twice the work, and
    two shortlists that a finishing warm-up or a changed file could make
    different, so the disagreement reported could be about clauses other than
    the ones shown above it. Found by a review 2026-09-11.
    """
    wanted = list(scopes or [])
    if not wanted:
        # Nothing was asked of anybody. An empty list here means "no scope was
        # named", not "they agree" - and the caller named none.
        return []

    # THE GATE IS TWO SOURCES, NOT TWO SCOPE NAMES, and the first version
    # tested the wrong one. `if len(wanted) < 2: return []` carried the
    # comment "a scope cannot disagree with itself" - true, and beside the
    # point: TWO DOCUMENTS IN ONE COMPANY STORE disagree all the time, which
    # is the case the source grouping below was built for. So that grouping
    # was unreachable for exactly the situation it existed to report, and a
    # single-scope caller was told nothing while its own store held 30mm and
    # 40mm for the same thing. A comment describing one rule sitting on top
    # of code enforcing another. Found by a review 2026-09-11.
    #
    # There is no cheaper gate to put here: whether two sources exist is not
    # knowable before the stores are read, and `says` below already refuses
    # to report a unit fewer than two sources carry.

    if asked is None:
        asked = RETRIEVE.librarian(text, scopes=wanted, project=project,
                                   limit=limit)

    values = []
    for one in asked:
        if one.skipped or one.answer is None:
            continue
        try:
            store = SCOPE.open_scope(one.scope, project)
        except Exception:
            continue
        try:
            values.extend(_values_from(store, one))
        finally:
            store.close()

    by_unit = {}
    for value in values:
        by_unit.setdefault(value.unit, []).append(value)

    found = []
    for unit in sorted(by_unit):
        group = by_unit[unit]

        # WHAT EACH SOURCE SAYS, AS A SET. Two things were wrong in the first
        # version and a review found both:
        #
        #   * it grouped by SCOPE, so two standards in one company store were
        #     one source and their disagreement vanished. R-24 says two
        #     SOURCES, and a source is a document.
        #   * it kept only the FIRST value each scope offered, so a clause
        #     carrying "clearance 25mm, insulation 30mm" against one carrying
        #     "clearance 25mm, insulation 40mm" matched on 25 and reported
        #     nothing - the conflicting pair was thrown away before comparison.
        #
        # Comparing SETS fixes both and fixes a third thing neither finding
        # named: two sources that both carry 25mm and 30mm AGREE, and reporting
        # "two sources, two values" about them would have been noise.
        # KEYED ON THE CANONICAL NUMBER, holding the value as written.
        says = {}
        for value in group:
            says.setdefault(value.source, {})[same_number(value.value)] = value
        if len(says) < 2:
            continue

        shapes = [frozenset(v) for v in says.values()]
        if len(set(shapes)) < 2:
            continue          # every source says the same thing. Not a conflict.

        chosen = []
        for source in sorted(says, key=lambda k: _rank(says[k])):
            for number in sorted(says[source]):
                chosen.append(says[source][number])
        found.append(Disagreement(unit, chosen))
    return found


def _rank(by_number):
    """Sort key for one source: the hierarchy's order, then its name.

    REPORTING ONLY. Nothing here drops or prefers anything - the order decides
    which line is printed first and nothing else.
    """
    value = list(by_number.values())[0]
    try:
        return (HIERARCHY.index(value.scope), value.label, value.document or "")
    except ValueError:
        return (len(HIERARCHY), value.label, value.document or "")


def main(argv):
    scopes = None
    project = None
    if "--scopes" in argv:
        i = argv.index("--scopes")
        scopes = [s.strip().lower() for s in argv[i + 1].split(",") if s.strip()]
        argv = argv[:i] + argv[i + 2:]
    if "--project" in argv:
        i = argv.index("--project")
        project = argv[i + 1]
        argv = argv[:i] + argv[i + 2:]

    unknown = [a for a in argv if a.startswith("-")]
    if unknown:
        print("  not a flag this tool has: %s" % " ".join(unknown))
    elif not argv:
        print("  ask a question:")
    elif not scopes:
        print("  two scopes or more, or there is nothing to compare:")
    if unknown or not argv or not scopes:
        print('  python brain/heron_conflict.py "how thick is duct insulation"'
              ' \\')
        print('      --scopes company,project --project "Tower B"')
        return 2

    text = " ".join(argv)
    found = disagreements(text, scopes=scopes, project=project)

    print("Asked:  %s" % text)
    print("Scopes: %s" % ", ".join(scopes))
    print("")
    if not found:
        # THE PLAUSIBLE ZERO (D-52). "No disagreement" and "nothing to compare"
        # are different, and printing one for the other is how a silent miss
        # reads as a clean bill of health.
        print("No disagreement found in the numbers these scopes returned.")
        print("")
        print("THAT IS NOT THE SAME AS 'THEY AGREE'. It means no two scopes")
        print("returned different values of the same unit for this question -")
        print("which is also what an empty scope, an unindexed one, or a")
        print("question neither covers would produce. Read each scope's own")
        print("answer:  python brain/heron_retrieve.py \"%s\"" % text)
        return 0

    for one in found:
        print(one.sentence())
        print("")
    print("Heron has decided NOTHING here (R-24). Both clauses are still")
    print("returned by the Librarian, each under its own scope, each cited.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
