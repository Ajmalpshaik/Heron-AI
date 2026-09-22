# Heron-Agent:  HERON-RAG-CIT-014
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Check a proposed answer against the clauses it cites. Flag, never rewrite.

    python brain/heron_ground.py --draft <file> --question "..."

WHY A MODULE THAT CANNOT WRITE AN ANSWER IS ALLOWED TO JUDGE ONE
-----------------------------------------------------------------
D-01 gives the reply to the host: Heron does not write the answer the user
reads. At first sight that means the brain cannot check one either.

It can, because checking and writing are different acts, and this repository
has already separated them once. heron_validate.py - the Fragment Validation
Agent - GATHERS THE EVIDENCE FOR A PROOF AND NEVER SIGNS ONE, because D-30
says an agent that can stamp 193 fragments is the fastest machine ever built
for making an unproven claim look proven.

This is that shape one layer up:

    THE BRAIN CANNOT WRITE THE ANSWER. IT CAN REFUSE TO ENDORSE ONE.

So this is a call the host makes BACK into the brain, with its draft and the
packet the draft was built from. It returns a report. It NEVER returns a
rewrite (R-53) - a checker that quietly repairs its own findings is how a
wrong answer becomes an invisible one.

IT CALLS NO MODEL AND TOUCHES NO NETWORK (R-47)
------------------------------------------------
difflib and re. That is the whole dependency list. It must run on a machine
with no keys, because the machines this is for are locked-down ones - and
because a check that needs the thing it is checking cannot be trusted to
disagree with it.

THE ROW MOST LIKELY TO BE GOT WRONG, AND ITS TEST IS FIRST (R-51)
------------------------------------------------------------------
A modeller who writes "the duct needs insulation" about a clause saying
"ducts in unconditioned spaces shall be insulated to 25mm" has said something
TRUE AND LESS SPECIFIC. A checker that calls that a fabrication will be
switched off within a week.

So the mechanism is not "how similar is this" alone. It is:

    FABRICATION IS AN ADDED FACT, NOT A MISSING ONE.

A claim whose every fact - number, dimension, clause reference, category,
parameter - is present in its source cannot have invented one, whatever a
similarity ratio says about the prose around it. That is structural, and it
is why R-51 is a rule here rather than a threshold somebody tunes.

WHAT A THRESHOLD IS FOR, AND WHAT IT IS NEVER FOR (R-49, R-55)
---------------------------------------------------------------
A quoted clause is held tighter than a paraphrase of one; one number for every
kind of sentence is either too loose for quotes or too tight for prose.

AND A THRESHOLD IS NEVER LOWERED TO REDUCE FLAGS. If the flags are wrong, the
NORMALISER or the SELECTOR is wrong - fix the step that is wrong and record
what it was, the way retrieval-history.md records the three times an utterance
was left alone rather than weakened.
"""

import datetime
import difflib
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))


# ---------------------------------------------------------------------------
# Step 1 - normalise, one function, both sides, identically (R-48)
# ---------------------------------------------------------------------------

# THE UNIT LIST IS HERON'S OWN, AND IT IS WHERE THIS DIFFERS MOST FROM WHAT
# WAS READ. The outside version normalised frequency words and numeric
# suffixes, because that is what its corpus varied in. BIM TEXT VARIES IN
# UNITS AND CLAUSE NUMBERS, and a spelling difference must NEVER read as an
# invention:
#
#   150mm / 150 mm        the same dimension, written two ways
#   DN150 / 150Ø / Ø150   the same nominal bore, three ways
#   §21.3.2 / 21.3.2      the same clause, with and without the symbol
#   Revit 2024 / 2024     the same release
#
# This is the part most likely to decide whether the check is trusted, because
# every false flag it produces is a true sentence called a lie.
_UNITS = r"(?:mm|cm|m|km|in|ft|kg|g|t|l|ml|pa|kpa|bar|mbar|c|k|w|kw|mw|" \
         r"va|kva|hz|v|kv|a|ma|db|lux|lm|cfm|ls|m2|m3)"
# No "%" here: a percentage is canonicalised to "pct" by its own rule above
# any unit matching, so listing it here would be a branch nothing reaches.

_RULES = [
    # THOUSANDS SEPARATORS FIRST, and with no trailing word boundary. In
    # "1,500mm" the digits are followed by a letter, so \b never matched and
    # the comma survived into the text as a space - "1 500mm" against
    # "1500 mm", which is a typographic difference reading as a different
    # number. Found by the test that asserts the pair normalises equal.
    (re.compile(r"\b(\d),(\d{3})"), r"\1\2"),
    # A section symbol adds nothing a clause number does not already say.
    (re.compile(r"§\s*"), ""),
    # "Revit 2024" and "2024" are the same release when a release is meant.
    (re.compile(r"\brevit\s+(\d{4})\b"), r"\1"),
    # DN150, 150Ø and Ø150 are one nominal bore.
    (re.compile(r"\bdn\s*(\d+)\b"), r"dn\1"),
    (re.compile(r"\b(\d+)\s*[Øø⌀]"), r"dn\1"),
    (re.compile(r"[Øø⌀]\s*(\d+)\b"), r"dn\1"),
    # A GRADIENT AND A PERCENTAGE MUST SURVIVE THE PUNCTUATION STRIP, and
    # neither did. normalise() removes every character outside [\w\s.\-/], so
    # "1:100" became "1 100" and "50%" became "50" - both BEFORE the selector
    # looked for them, so a drainage fall was not a checkable fact at all and
    # could be invented freely. Canonical forms fix it, and they fold the
    # three ways a fall is actually written into one.
    (re.compile(r"\b(\d+)\s*(?::|/)\s*(\d+)\b"), r"\1in\2"),
    (re.compile(r"\b(\d+)\s+in\s+(\d+)\b"), r"\1in\2"),
    # NO TRAILING \b AFTER THE SYMBOL. "%" is not a word character, so at the
    # end of "50%" there is no word boundary and the rule silently did not
    # fire - "50%" normalised to "50" while "50 per cent" normalised to
    # "50pct", which is the exact shape R-48 exists to prevent.
    (re.compile(r"\b(\d+(?:\.\d+)?)\s*%"), r"\1pct"),
    (re.compile(r"\b(\d+(?:\.\d+)?)\s*per\s?cent\b"), r"\1pct"),
    # A number and its unit, with or without the space.
    (re.compile(r"\b(\d+(?:\.\d+)?)\s*" + _UNITS + r"\b"),
     lambda m: "%s%s" % (m.group(1), m.group(0)[len(m.group(1)):].strip())),
]


def normalise(text):
    """One function. Called on BOTH sides, identically, always.

    Two normalisers that drift apart produce a difference that is not in
    either text - which is the failure mode this whole module exists to
    prevent, arriving inside it.
    """
    out = (text or "").lower()
    for pattern, replacement in _RULES:
        out = pattern.sub(replacement, out)
    out = re.sub(r"[\"'`“”‘’]", "", out)
    out = re.sub(r"[^\w\s.\-/]", " ", out)
    out = re.sub(r"\s+", " ", out)
    return out.strip()


# ---------------------------------------------------------------------------
# Step 3 - which sentences are checkable at all (R-50)
# ---------------------------------------------------------------------------

# How a sentence says which chunk it came from. A locator is what a person
# writes - "[4.1.1]" - and a chunk id is what a machine writes.
#
# A MARKER IS CITATION-SHAPED, AND ACCEPTING ANY BRACKETED TEXT LET A CLAIM
# WALK OUT THROUGH THE BRACKETS. facts() strips every marker before it looks
# for facts - it has to, because "[9.1.1]" IS a clause number and would
# otherwise read as a fabricated one. But with any bracket counting as a
# marker:
#
#     Use [50mm] insulation [abc123:0001]
#
# lost BOTH brackets, found no fact, was classified SKIPPED, and the report
# said ok - against a clause requiring 25mm. Bracketing a number turned the
# fabrication check off for that sentence. Found by a review 2026-09-11.
#
# So the shape is named. A citation is a chunk id, or a clause locator, or a
# named one ("Section 3", "para-7"); it is never a value carrying a unit.
# Anything else in brackets stays in the sentence, where facts() can see it
# and the check can do its job.
_BRACKETED = re.compile(r"\[([^\]]{1,120})\]")

# What a citation can LOOK like. TWO SHAPES AND NO CATCH-ALL.
#
# The first version added a permissive "one token with no spaces" arm so that
# a caller's own short chunk ids would keep working - and that arm let every
# NAMED fact through:
#
#     Use [DN100] pipe [abc123:0001]        -> both stripped, no facts, SKIPPED
#     Use [OST_DuctCurves] here [abc:0001]  -> same
#
# against a clause specifying DN50. The round before had closed the same door
# for "[50mm]" by refusing anything with a UNIT on it, which only ever caught
# numeric-leading measurements - a nominal bore, a Revit category and a
# parameter name all walked past it. Found by a review 2026-09-11, one round
# after the first half.
#
# So a marker is a chunk id or a locator, both of which START with a digit
# after any naming word. A short id that this shape does not recognise is
# still a citation when the packet CARRIES it - that is what `known` is for,
# and it is a fact about the packet rather than a guess about the text.
_CITE_SHAPED = re.compile(
    r"""^\s*(?:
          [0-9a-f]{8,}(?::[0-9]+)?             # a chunk id
        | (?:section|clause|sub-?clause|sub-?section|table|appendix|annex|
             part|paragraph|para|item|rule|figure)
          [\s.-]*\d+(?:[.-]\d+)*[a-z]?        # Section 3, para-7, clause 4.1a
        | \d+(?:\.\d+)*[a-z]?                 # 4, 4.1, 9.1.1, 7a
    )\s*$""", re.I | re.X)


# A VALUE WITH A UNIT ON IT, kept as a SECOND guard behind the shape above.
#
# The shape alone is not quite enough: "50m" is one digit-run and one letter,
# which is also what a clause "7a" looks like, so the locator arm accepts it.
# A unit settles it. Narrow on purpose - a clause number, a year and a bare
# count are all shapes a real locator takes, and only a measurement is
# something no locator has ever been.
_MEASURED = re.compile(
    r"^\s*(?:-?\d+(?:\.\d+)?\s*[a-z]+[0-9]?|\d+\s*in\s*\d+)\s*$", re.I)


def _states_a_measurement(text):
    """Whether this span is a measured value rather than a pointer to one."""
    return bool(_MEASURED.match((text or "").strip()))


def _is_marker(found, known=None):
    """Whether one bracketed span is a citation rather than part of the claim.

    ACCEPTING ANY BRACKETED TEXT LET A CLAIM WALK OUT THROUGH THE BRACKETS.
    facts() strips every marker before looking for facts - it has to, because
    "[9.1.1]" IS a clause number and would otherwise read as a fabricated one.
    But with every bracket counting as a marker:

        Use [50mm] insulation [abc123:0001]

    lost BOTH brackets, found no fact, was classified SKIPPED, and the report
    said ok - against a clause requiring 25mm. Bracketing a number turned the
    fabrication check off for that sentence. Found by a review 2026-09-11.

    Two questions, in this order:

      does it RESOLVE?   `known` is what this packet actually carries. A span
                         that names a chunk or a locator in it is a citation
                         whatever it looks like, which is what keeps short
                         ids working.
      does it MEASURE?   otherwise it has to be citation-SHAPED and carry no
                         unit. A clause number, a year and a bare count are
                         all shapes a real locator takes; "50mm" is not.

    "[99]" is the honest edge. A bare count is both a plausible locator and a
    checkable fact, so `known` decides it: a citation when the packet holds
    locator 99, and a claim when it does not. Where nothing resolves it, this
    still reads it as a marker - which is the pre-existing behaviour, and it
    is written down here rather than left to be discovered.
    """
    key = (found or "").strip()
    if known and key in known:
        return True
    if not _CITE_SHAPED.match(key):
        return False
    return not _states_a_measurement(key)


class _Markers(object):
    """The bracketed spans that are citations, and nothing else.

    One object rather than one regex because "is this a marker" is now two
    questions, and every caller needs the same answer to both.
    """

    @staticmethod
    def findall(text, known=None):
        return [found for found in _BRACKETED.findall(text or "")
                if _is_marker(found, known)]

    @staticmethod
    def search(text, known=None):
        return bool(_Markers.findall(text, known))

    @staticmethod
    def sub(with_what, text, known=None):
        def swap(match):
            return (with_what if _is_marker(match.group(1), known)
                    else match.group(0))
        return _BRACKETED.sub(swap, text or "")


_MARKER = _Markers()


def cited_ids(text):
    """Every bracketed span in a draft that could be a CHUNK ID, deduplicated.

    Deliberately looser than _is_marker: this is the list a caller looks up BY
    ID in a store, and a lookup that misses costs one query and answers None.
    Being strict here would reintroduce the thing it exists to prevent - a
    real clause that a shortlist did not rank being reported as a citation
    resolving to nothing.

    A span containing whitespace is skipped: "Section 3" is a locator that a
    person writes, and locators are not unique across documents, so resolving
    one by id is meaningless.
    """
    out = []
    for found in _BRACKETED.findall(text or ""):
        key = found.strip()
        if not key or " " in key or "\t" in key:
            continue
        if key not in out:
            out.append(key)
    return out

# A SENTENCE WITH NO FACT IN IT CANNOT FABRICATE ONE, and flagging it teaches
# people to ignore flags. "This is worth reviewing" is not a claim about an
# indexed source; "ducts shall be insulated to 25mm" is.
_FACT = [
    # THE SIGN IS PART OF THE VALUE. Written without it, a source requiring a
    # fall of -200mm and a draft claiming +200mm produced the SAME fact and
    # the draft passed - a reversed gradient reported as grounded. Found by a
    # review 2026-09-11.
    re.compile(r"-?\d+(?:\.\d+)*\s*" + _UNITS + r"\b"),  # a dimension
    re.compile(r"\bdn\d+\b"),                             # a nominal bore
    re.compile(r"\b\d+(?:\.\d+){1,}\b"),                  # a clause number
    # LOWERCASE, BECAUSE facts() RUNS THESE ON NORMALISED TEXT. Both of these
    # were written case-sensitive - "OST_" and an uppercase letter after the
    # dot - and normalise() lowercases before they run, so NEITHER EVER
    # MATCHED. A sentence whose only claim was OST_DuctCurves or
    # BuiltInParameter.RBS_DUCT_BOTTOM_ELEVATION produced no facts at all, was
    # skipped, and left the report saying ok - which is R-50's list of
    # checkable things quietly containing two entries that could not be
    # checked. Found by a review; the test below now covers both.
    re.compile(r"\bost_[a-z0-9_]+\b"),                      # a category
    re.compile(r"\b[a-z]+\.[a-z][a-z0-9_]*_[a-z0-9_]+\b"),  # a parameter
    re.compile(r"\b\d+in\d+\b"),                           # a gradient, 1:100
    re.compile(r"\b\d+(?:\.\d+)?pct\b"),                    # a percentage
    re.compile(r"\b\d{4}\b"),                             # a year or release
    # A BARE COUNT IS A CLAIM ABOUT THE SOURCE TOO, and leaving it out meant
    # "install 99 supports" against a clause requiring 2 was reported ok - the
    # sentence had no unit, no clause number and no year, so facts() was empty
    # and R-50 skipped it. Found by a review 2026-09-11.
    #
    # THIS MAKES MORE SENTENCES CHECKABLE, WHICH MEANS MORE FLAGS. That is the
    # safe direction for a standards check and it is not free: a draft that
    # mentions a number the clause does not carry is now FLAGGED rather than
    # skipped. The module flags and never rewrites (R-53), so the cost is a
    # person reading a line, and the alternative was a wrong count passing.
    re.compile(r"-?\b\d+\b"),                            # a plain count
]


def facts(text, known=None):
    """Every checkable fact in a sentence, normalised. Empty means not checkable.

    `known` is what the packet carries - chunk ids and locators - so a
    bracketed span that RESOLVES is stripped as a citation and one that does
    not is left in the sentence to be checked. See _is_marker.

    The list IS the selector: a sentence with no facts is skipped, and the
    same list decides what R-51 compares.

    THE CITATION MARKER IS STRIPPED FIRST, AND MISSING THAT MADE THE CHECKER
    CALL EVERY TRUE SENTENCE A LIE. "[9.1.1]" looks exactly like a clause
    reference because it IS one - so the sentence "insulated to 25mm [9.1.1]"
    appeared to state a clause number its source did not contain, and was
    flagged as a fabrication. A chunk id ending in "0003" did the same through
    the year pattern. The marker says WHERE the claim came from; it is not
    part of the claim.
    """
    seen = []
    flat = normalise(_MARKER.sub(" ", text or "", known))
    for pattern in _FACT:
        for match in pattern.finditer(flat):
            found = match.group(0).strip()
            if found not in seen:
                seen.append(found)
    return seen


def sentences(text):
    """A draft as sentences. Deliberately simple, and it says so.

    A full sentence splitter is a research problem and this does not need
    one: a clause reference is protected by the normaliser's own shapes, and
    an over-split sentence is checked against the same chunk anyway.
    """
    parts = re.split(r"(?<=[.!?])\s+(?=[A-Z(])|\n+", text or "")
    return [bit.strip() for bit in parts if bit and bit.strip()]


# ---------------------------------------------------------------------------
# Step 4 - a threshold per kind of claim, from a named table (R-49)
# ---------------------------------------------------------------------------

QUOTE = "quote"              # the draft presents it as the source's own words
REFERENCE = "reference"      # it names a clause number
NUMERIC = "numeric"          # it carries a dimension or a quantity
PARAPHRASE = "paraphrase"    # prose about a named thing

# ONE NUMBER FOR EVERY KIND OF SENTENCE IS EITHER TOO LOOSE FOR QUOTES OR TOO
# TIGHT FOR PROSE - and measuring it produced something sharper than a table
# of four numbers. THREE OF THE FOUR KINDS GET NO RATIO GATE AT ALL, because
# the ratio was measured and could not do the job.
#
# MEASURED 2026-09-11, six true paraphrases against six wrong claims, all
# carrying a fact the source did carry:
#
#     true paraphrases   0.222 - 0.682
#     wrong claims       0.204 - 0.588
#
# They overlap almost entirely. "25mm insulation is required on ducts" - true
# - scores 0.222, BELOW "drainage shall fall at 25mm" at 0.236. Any threshold
# on that column either flags true paraphrases or passes wrong claims, and
# flagging a modeller's own wording is the thing R-51 exists to prevent.
#
# So the ratio is REPORTED and never enforced for those three. The added-fact
# rule is what catches an invention, and it is structural rather than tuned.
#
# A QUOTATION IS DIFFERENT, AND THE MEASUREMENT SAYS SO. A claim in quotation
# marks asserts it IS the source's words, so the right question is not "how
# similar" but "IS IT IN THERE" - the share of the quoted text that appears
# verbatim in the source:
#
#     true quotations    1.000, 1.000
#     misquotations      0.265, 0.167
#
# A clean separation with nothing in between. 0.90 is not tuned to that gap -
# it is what "these are the source's words" means, allowing for an ellipsis
# or a dropped article and nothing more.
#
# R-55 BINDS ALL OF IT: a threshold is NEVER lowered to reduce flags. What
# changed here was the MECHANISM, not a number - and the mechanism changed
# because a measurement said the old one did not work, which is the one
# reason this repository accepts. The numbers above are the record.
NO_RATIO_GATE = None

THRESHOLDS = {
    # 1.0 BECAUSE coverage() IS NOW CONTAINMENT. The measurement that set
    # 0.90 recorded a true quotation at exactly 1.000 and a false one at
    # 0.265; the slack between was never evidence, and a reversed quotation
    # measured 0.982 walked straight through it. See coverage().
    QUOTE:      1.0,             # containment, not similarity - see above
    REFERENCE:  NO_RATIO_GATE,   # the added-fact rule carries these three
    NUMERIC:    NO_RATIO_GATE,
    PARAPHRASE: NO_RATIO_GATE,
}

# What a quotation claims: that these words are in the source.
_QUOTED = re.compile(r'["“]([^"”]{4,})["”]')


def coverage(claim, source):
    """The share of a quoted span that appears verbatim in its source.

    CONTAINMENT, NOT SIMILARITY - and the first version of this said so and
    did not do it. It returned the LONGEST COMMON RUN over the quote's length,
    which is a different measurement: a long quotation with a short reversal
    at its START keeps a very long matching tail.

        source  "No ducts shall be installed within the ceiling void unless..."
        quoted  "All ducts shall be installed within the ceiling void unless..."
        scored   0.982, against a gate of 0.90 - REPORTED GROUNDED

    Measured 2026-09-11 on this exact code. The claim reversed the clause and
    the check endorsed it, which is the single failure this module exists to
    prevent. Found by a review.

    So containment is now literal: a quotation IS in the clause or it is not.
    The gate moves to 1.0 with it, and that is not a threshold being tuned
    (R-55) - the measurement that set 0.90 recorded a true quote at EXACTLY
    1.000 and a false one at 0.265, with nothing in between. The 0.90 was
    slack around a number that had no spread, and the slack is what the
    reversal walked through.

    The ratio is still returned when there is no containment, because the
    report shows it and a reader can see how near a miss was.

    EACH QUOTED SPAN IS ITS OWN CLAIM, AND JOINING THEM INVENTED A THIRD.
    The first containment version glued every span together with one space
    and looked for that string:

        source  "ducts shall be insulated"
        draft   The clause says "ducts" shall be "insulated" [chunk]
        joined  "ducts insulated"  - which the source does not contain
        scored  flagged, and BOTH quotations were exact

    The draft never presented those words as one quotation; the check did.
    A reader told their correct sentence is fabricated stops reading the
    reports, which costs more than the miss it was guarding. Found by a
    review 2026-09-11.

    Combined by the WORST span, not the average: the gate is 1.0, so every
    span has to be in the clause, and the number a reader sees should be the
    one that failed rather than a figure softened by the spans that passed.
    """
    spans = _QUOTED.findall(claim or "")
    if not spans:
        return None

    # `source` MAY BE SEVERAL SOURCES, because a sentence may cite several.
    # Each span is then measured against the BEST of them and the claim
    # against the WORST of its spans - "every quotation is in one of the
    # clauses I cited" - which is the rule a conflict sentence needs:
    #
    #   Company says "…25mm" [a], while project says "…50mm" [b]
    #
    # Taking the best whole-CLAIM coverage across the bodies was not enough
    # and was the first attempt: against chunk a the 50mm span still failed
    # and against chunk b the 25mm span did, so the worst span lost either
    # way. The choice has to be per span. Found by measuring the fix.
    texts = [source] if isinstance(source, str) else list(source or [])
    grounds = [normalise(text) for text in texts]
    grounds = [got for got in grounds if got]
    if not grounds:
        return 0.0

    worst = None
    for span in spans:
        want = normalise(span)
        if not want:
            continue
        best = 0.0
        for got in grounds:
            if want in got:
                best = 1.0
                break
            matcher = difflib.SequenceMatcher(None, want, got)
            longest = matcher.find_longest_match(0, len(want), 0, len(got))
            best = max(best, longest.size / float(len(want)))
        if worst is None or best < worst:
            worst = best
    if worst is None:
        return 0.0
    return worst


# A REVERSAL IS NOT AN ADDED FACT, WHICH IS WHY THE ADDED-FACT RULE MISSED IT.
#
#     clause  "Duct insulation shall not exceed 25mm"
#     draft   "Duct insulation shall exceed 25mm [chunk]"
#
# Both yield exactly one fact, 25mm. Nothing was added, the ratio has no gate
# on a NUMERIC claim, and the report said GROUNDED about a sentence stating the
# opposite of its source. Measured 2026-09-11; found by a review.
_NEGATIONS = ("not", "no", "never", "cannot", "without", "neither", "nor",
              "except", "unless", "exclude", "excludes", "excluding")


def _negations(text, known=None):
    """Which negating words a sentence carries, as a set.

    `known` TRAVELS BECAUSE A MARKER LEFT IN CHANGES THE WORD LIST. Stripping
    without it left "[c1]" in the sentence, so the claim's words and the
    source's words could never match and reverses() stopped firing - which put
    back the round-three defect where a reversed clause passed the check.
    Every marker-stripping site takes the packet now.
    """
    words = set(normalise(_MARKER.sub(" ", text or "", known)).split())
    return set(word for word in _NEGATIONS if word in words)


def reverses(claim, source_sentence, known=None):
    """Whether the claim is its source with the negation taken out or put in.

    STRUCTURAL, AND WITH NO THRESHOLD IN IT - which is the only kind of rule
    this module is allowed to add. Strip the negating words from both sides;
    if what is left is IDENTICAL and the negating words differ, then the only
    difference between the two sentences is the negation, and one of them
    states the opposite of the other.

    WHAT IT DOES NOT CATCH, said plainly rather than discovered later. A
    reversal that also rewords - "no ducts" against "all ducts" - leaves
    different remainders and is invisible here. That is the general paraphrase
    problem, and R-46's measurement already recorded that the similarity ratio
    cannot separate a true paraphrase from a wrong claim. A QUOTED reversal is
    caught by coverage(), which is containment; an unquoted reworded one needs
    something this module deliberately does not have (R-47: no model, no
    network).
    """
    mine = _negations(claim, known)
    theirs = _negations(source_sentence, known)
    if mine == theirs:
        return False
    return _bare(claim, known) == _bare(source_sentence, known)


def _bare(text, known=None):
    """The sentence as words, with the negation and the full stops taken out.

    THE TRAILING FULL STOP IS THE WHOLE REASON THIS IS A FUNCTION. normalise()
    keeps "." because a clause number needs it, so a source sentence ends
    "25mm." and a claim ends "25mm" - and comparing the two lists made every
    reversal look like a different sentence. The first version of reverses()
    passed its own unit check and still returned GROUNDED end to end, which is
    what testing it through check() caught and testing the helper alone did
    not.
    """
    words = normalise(_MARKER.sub(" ", text or "", known)).split()
    out = []
    for word in words:
        word = word.strip(".,;:-")
        if word and word not in _NEGATIONS:
            out.append(word)
    return out


def nearest_sentence(claim, source):
    """The sentence of `source` this claim is closest to. Never None.

    support() already finds this score and throws the sentence away. The
    polarity check needs the SENTENCE - comparing a claim's negation against a
    whole fifteen-hundred-character chunk would find a "not" somewhere in it
    almost every time.
    """
    want = normalise(claim)
    best, score = source or "", -1.0
    for piece in sentences(source) or [source or ""]:
        got = normalise(piece)
        if not got:
            continue
        ratio = difflib.SequenceMatcher(None, want, got).ratio()
        if ratio > score:
            best, score = piece, ratio
    return best


def kind_of(sentence, known=None):
    """Which threshold this sentence is held to.

    THE CITATION MARKER IS STRIPPED FIRST, for the same reason facts() strips
    it: "[9.1.1]" is a clause number, so every cited sentence was being called
    a REFERENCE claim because of the marker that says where it came from
    rather than because of anything it says.
    """
    claim = _MARKER.sub(" ", sentence or "", known)
    if re.search(r"[\"“”]", claim):
        return QUOTE
    if re.search(r"\b\d+(?:\.\d+){1,}\b", normalise(claim)):
        return REFERENCE
    if re.search(r"\d", claim):
        return NUMERIC
    return PARAPHRASE


# ---------------------------------------------------------------------------
# Step 2 - compare, with the standard library and nothing else (R-47)
# ---------------------------------------------------------------------------

def support(claim, source):
    """How well `source` supports `claim`. 0.0 to 1.0, best window wins.

    THE WHOLE CHUNK IS THE WRONG THING TO COMPARE AGAINST, and getting that
    wrong would have made every long clause look like a fabrication. A ratio
    between one sentence and fifteen hundred characters is dominated by the
    length difference, not by whether the claim is in there. So the claim is
    compared against each sentence of the source and the BEST match is the
    score - which is what "compared against the chunk it cites" has to mean
    once a chunk is bigger than a sentence.
    """
    want = normalise(claim)
    if not want:
        return 0.0
    best = 0.0
    for piece in sentences(source) or [source]:
        got = normalise(piece)
        if not got:
            continue
        best = max(best, difflib.SequenceMatcher(None, want, got).ratio())
    # And against the whole thing too, for a claim that spans two sentences.
    whole = normalise(source)
    if whole:
        best = max(best, difflib.SequenceMatcher(None, want, whole).ratio())
    return best


# ---------------------------------------------------------------------------
# The verdicts
# ---------------------------------------------------------------------------

SKIPPED = "skipped"          # no fact in it - nothing to fabricate
GROUNDED = "grounded"        # every fact is in the source, and it reads like it
UNDERSTATED = "understated"  # says LESS than the source. A PASS (R-51)
FLAGGED = "flagged"          # a fact the source does not carry
UNCITED = "uncited"          # a fact with no chunk behind it - a BUG (R-65)
UNRESOLVED = "unresolved"    # it cites something this packet does not carry
AMBIGUOUS_CITE = "ambiguous"  # the locator names more than one document
REVERSED = "reversed"        # its source with the negation taken out or put in
MISPLACED = "misplaced"      # the value is in the chunk, under a DIFFERENT subject

PASSES = (SKIPPED, GROUNDED, UNDERSTATED)

# THE TWO MECHANISMS, AND EACH HAS ONE JOB.
#
# Measured 2026-09-11 on twelve claims, six true and six false, and the first
# version of this module got two of the six false ones wrong:
#
#   "insulated to 25mm except within 10m"   against a source saying 3m
#   "mineral wool of density 96 kg/m3"      against a source saying 48
#
# Both PASSED, because the prose around the number was nearly identical to the
# source and the similarity ratio carried them over the threshold. That is the
# defect exactly inverted: ONE CHARACTER IS THE WHOLE FABRICATION, and a ratio
# is at its blindest precisely there.
#
# So the two mechanisms were separated, and the separation is what this
# module's own principle already said:
#
#   AN ADDED FACT IS A FLAG, WHATEVER THE RATIO SAYS. A number, a dimension, a
#   gradient or a clause reference that the source does not carry is invented,
#   and no amount of surrounding agreement vouches for it.
#
#   THE RATIO JUDGES THE PROSE, and only where every fact already checks out.
#   "Ducts shall be painted red to 25mm" carries no invented NUMBER and is
#   still not what the clause says - that is what a per-kind threshold is for,
#   and it is the only thing it is for.


class Claim(object):
    def __init__(self, sentence, verdict, kind=None, ratio=None,
                 threshold=None, citation=None, added=None):
        self.sentence = sentence
        self.verdict = verdict
        self.kind = kind
        self.ratio = ratio
        self.threshold = threshold
        self.citation = citation
        self.added = added or []

    def __repr__(self):
        return "<%s %s%s>" % (self.verdict, self.sentence[:40],
                              "" if self.ratio is None else
                              " %.2f" % self.ratio)


class Report(object):
    """What the check found. A REPORT, and never a corrected answer (R-53)."""

    def __init__(self, claims, thresholds, sources):
        self.claims = claims
        self.thresholds = dict(thresholds)
        self.sources = sources
        self.at = datetime.datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")

    @property
    def checked(self):
        """THE DENOMINATOR, and it is what makes two runs comparable.

        A flag count alone is not a measurement - three flags out of four
        claims and three out of four hundred are not the same result.
        """
        return len([c for c in self.claims if c.verdict != SKIPPED])

    @property
    def flagged(self):
        return [c for c in self.claims if c.verdict == FLAGGED]

    @property
    def uncited(self):
        return [c for c in self.claims if c.verdict == UNCITED]

    @property
    def unresolved(self):
        return [c for c in self.claims if c.verdict == UNRESOLVED]

    @property
    def ambiguous(self):
        return [c for c in self.claims if c.verdict == AMBIGUOUS_CITE]

    @property
    def misplaced(self):
        """Claims stating a value that sits under another subject in the same
        chunk. Nothing was invented; it was moved."""
        return [c for c in self.claims if c.verdict == MISPLACED]

    @property
    def reversed_claims(self):
        """Claims that are their source with the negation moved. The worst
        verdict here, because the sentence is otherwise word for word right."""
        return [c for c in self.claims if c.verdict == REVERSED]

    @property
    def ok(self):
        return (not self.flagged and not self.uncited and not self.unresolved
                and not self.ambiguous and not self.reversed_claims
                and not self.misplaced)

    def lines(self):
        """The report a person reads. R-52: thresholds, denominator, ratios."""
        out = ["Checked %d claim(s) of %d sentence(s), against %d cited "
               "source(s)." % (self.checked, len(self.claims), self.sources),
               "Thresholds used: %s"
               % ", ".join("%s %s" % (k, "no ratio gate - the added-fact rule"
                                      if v is None else "%.2f coverage" % v)
                           for k, v in sorted(self.thresholds.items())),
               ""]
        if self.ok:
            out.append("Nothing flagged. Every checkable claim is carried by "
                       "the clause it cites.")
        for claim in self.uncited:
            out.append("UNCITED    %s" % claim.sentence[:70])
            out.append("           it states %s and cites nothing. R-21: an "
                       "uncited standards answer is a BUG, not a "
                       "low-confidence answer" % ", ".join(claim.added))
        for claim in self.ambiguous:
            out.append("AMBIGUOUS  %s" % claim.sentence[:70])
            out.append("           more than one document in this packet has "
                       "that clause number, so the check will not guess which "
                       "one. Cite the chunk id instead")
        for claim in self.unresolved:
            out.append("UNRESOLVED %s" % claim.sentence[:70])
            out.append("           it cites a source this packet does not "
                       "carry. R-22: a citation a human cannot follow is "
                       "decoration - check whether the clause exists")
        # FIRST IN THE LIST, because it is the one a reader would otherwise
        # skim past: every word matches its source except the one that
        # reverses it.
        for claim in self.reversed_claims:
            out.append("REVERSED   %s" % claim.sentence[:70])
            out.append("           this is the cited clause with its negation "
                       "TAKEN OUT or PUT IN - every other word matches. It "
                       "states the opposite of its source")
        for claim in self.misplaced:
            out.append("MISPLACED  %s" % claim.sentence[:70])
            out.append("           it states %s, which IS in the cited chunk "
                       "but under a different requirement than the one this "
                       "sentence matches. Nothing was invented - it was moved"
                       % ", ".join(claim.added))
        for claim in self.flagged:
            out.append("FLAGGED    %s" % claim.sentence[:70])
            if claim.threshold is None:
                out.append("           %s, scored %.2f against %s - flagged on "
                           "an ADDED FACT, not on the score"
                           % (claim.kind, claim.ratio or 0.0,
                              (claim.citation or {}).get("locator", "its source")))
            else:
                out.append("           %s, held at %.2f coverage, scored %.2f "
                           "against %s"
                           % (claim.kind, claim.threshold, claim.ratio or 0.0,
                              (claim.citation or {}).get("locator", "its source")))
            if claim.added:
                out.append("           the source does not carry: %s"
                           % ", ".join(claim.added))
        out.append("")
        out.append("This is a REPORT. Nothing was rewritten, and nothing here "
                   "is a signature - a person decides what to do with it "
                   "(D-30).")
        return out


# ---------------------------------------------------------------------------
# The check
# ---------------------------------------------------------------------------

AMBIGUOUS = object()      # a locator that names more than one document


def _cited(sentence, by_locator, by_chunk):
    """The source a sentence cites, AMBIGUOUS, or None.

    A LOCATOR IS NOT UNIQUE ACROSS DOCUMENTS, and the first version of this
    stored one per locator in a dict - so when two documents both had a clause
    "4.1", the second overwrote the first and a draft citing [4.1] was checked
    against whichever happened to be ingested last. A TRUE claim about the
    company standard was flagged as a fabrication because the project
    specification's 4.1 won the dictionary.

    That is the false alarm R-51 exists to prevent, arriving through the
    citation rather than through the comparison. So every match is kept, and a
    locator matching more than one document is AMBIGUOUS - which is reported
    and asks for a chunk id, rather than guessed at.
    """
    # EVERY MARKER, NOT THE FIRST. Returning on the first one checked a whole
    # sentence against a single source, and the sentence a standards answer
    # most needs to write is the one that cites two:
    #
    #     "Company requires 25mm [a], but project requires 50mm [b]"
    #
    # 50mm was reported as invented, because only chunk `a` was ever consulted
    # - so the grounding gate rejected the natural way to report exactly the
    # disagreement Stage 8 exists to surface. Found by a review 2026-09-11.
    #
    # The claim is checked against all of them together: it cited both, so a
    # fact carried by either is a fact it is entitled to state. A fact in
    # neither is still added, which is the rule that matters.
    resolved = []
    for found in _MARKER.findall(sentence or "",
                                 set(by_chunk) | set(by_locator)):
        key = found.strip()
        if key in by_chunk:
            resolved.append(by_chunk[key])
            continue
        if key in by_locator:
            matches = by_locator[key]
            if len(matches) == 1:
                resolved.append(matches[0])
                continue
            return AMBIGUOUS
    return resolved or None


def check(draft, packet):
    """A draft and the packet it was built from, in. A Report, out.

    `packet` is a heron_context.Context. Its STANDARD parts carry the chunk
    each one came from (R-63), and that binding is what gives this comparison
    a defined target (R-64) instead of a guess.
    """
    by_locator = {}
    by_chunk = {}
    for part in getattr(packet, "parts", []):
        cite = getattr(part, "citation", None)
        if not cite:
            continue
        # THE RAW CLAUSE, NOT THE RENDERED ONE. part.body is wrapped by
        # heron_context.as_quoted_source() with the document title and the
        # locator on its first line, and taking facts() of that made the LABEL
        # into evidence: a title of "QCS 2014" supplied the year 2014 to every
        # claim citing it. Falls back to the body for any part built before
        # this field existed, so an older caller still gets checked.
        body = getattr(part, "evidence", None) or part.body
        by_chunk[cite["chunk"]] = (cite, body)
        if cite.get("locator"):
            by_locator.setdefault(cite["locator"], []).append((cite, body))

    # WHAT THIS PACKET ACTUALLY CARRIES, so a bracketed span that resolves is
    # read as a citation and one that does not is read as part of the claim.
    known = set(by_chunk) | set(by_locator)

    claims = []
    for sentence in sentences(draft):
        found = facts(sentence, known)

        # A QUOTATION IS CHECKABLE WHETHER OR NOT IT CARRIES A NUMBER, and
        # missing that let the one gate which survived measurement be skipped
        # entirely. `The clause says "Ducts shall be painted red" [4.1]` has no
        # number, no unit, no clause reference - so facts() was empty, the
        # sentence was SKIPPED, and a fabricated quotation came back ok.
        #
        # A quotation asserts THESE ARE THE SOURCE'S WORDS. That is a claim
        # about an indexed source whatever else the sentence contains.
        quoted = bool(_QUOTED.search(_MARKER.sub(" ", sentence or "", known)))

        if not found and not quoted:
            # R-50. No fact, nothing to invent, and flagging it would teach
            # people to ignore flags.
            claims.append(Claim(sentence, SKIPPED))
            continue

        marked = bool(_MARKER.search(sentence or "", known))
        cited = _cited(sentence, by_locator, by_chunk)
        if cited is AMBIGUOUS:
            claims.append(Claim(sentence, AMBIGUOUS_CITE, added=found))
            continue
        if cited is None:
            # TWO DIFFERENT WRONGS, AND THEY NEED DIFFERENT ANSWERS.
            #
            # No marker at all is R-65: "per the specification" is not a
            # citation, and R-21 calls that a bug rather than a weak answer.
            #
            # A marker naming something this packet does not carry is the
            # other one - R-22's citation that a human cannot follow, the tag
            # pointing at no element. It may be a source outside the packet or
            # an invented clause number, and the check cannot tell which, so
            # it says which question to ask rather than guessing the answer.
            claims.append(Claim(sentence, UNRESOLVED if marked else UNCITED,
                                added=found))
            continue

        # THE FIRST CITATION LEADS, EVERY CITATION COUNTS. The citation shown
        # on the claim is the first one it named; the EVIDENCE is all of them,
        # because the sentence cited all of them.
        citation, body = cited[0]
        bodies = [text for _cite, text in cited]
        kind = kind_of(sentence, known)
        ratio = max(support(sentence, text) for text in bodies)
        in_source = []
        for text in bodies:
            in_source.extend(facts(text))
        added = [fact for fact in found if fact not in in_source]

        # WHICH SENTENCE OF THE CHUNK THIS CLAIM IS ACTUALLY ABOUT.
        #
        # facts(body) is the whole chunk, and a chunk is usually several
        # requirements. So a value could be lifted off ONE requirement and
        # attached to ANOTHER and still pass, because it was "in the source":
        #
        #   clause  "Duct insulation shall be 25mm. Pipe insulation shall
        #            be 50mm."
        #   draft   "Duct insulation shall be 50mm [chunk]"   -> UNDERSTATED, ok
        #
        # Measured 2026-09-11 on this exact code. Nothing was added, nothing
        # was negated, and the checker endorsed a requirement moved from pipes
        # to ducts - which on site is a different failure from an invented
        # number and exactly as expensive. Found by a review.
        # THE NEAREST SENTENCE OF WHICHEVER CITED CHUNK IS CLOSEST. With one
        # citation this is exactly what it was; with two it stops the
        # misplaced-value rule reporting a fact that is correctly lifted from
        # the SECOND source.
        nearest = max((nearest_sentence(sentence, text) for text in bodies),
                      key=lambda near: support(sentence, near))
        in_nearest = []
        for text in bodies:
            in_nearest.extend(facts(nearest_sentence(sentence, text)))

        # A REVERSAL IS CHECKED BEFORE ANYTHING ELSE, because it is invisible
        # to every other rule here. It adds no fact, so the added-fact rule
        # passes it; it is almost word for word its source, so the ratio is
        # HIGH rather than low. "shall not exceed 25mm" against "shall exceed
        # 25mm" was returning GROUNDED. Found by a review 2026-09-11.
        if all(reverses(sentence, nearest_sentence(sentence, text), known)
               for text in bodies):
            claims.append(Claim(sentence, REVERSED, kind=kind, ratio=ratio,
                                threshold=THRESHOLDS[kind], citation=citation,
                                added=["the source's negation is not this "
                                       "claim's"]))
            continue

        if added:
            # AN ADDED FACT IS A FLAG, AND THE RATIO DOES NOT GET A VOTE.
            claims.append(Claim(sentence, FLAGGED, kind=kind, ratio=ratio,
                                threshold=THRESHOLDS[kind], citation=citation,
                                added=added))
            continue

        # A FACT THAT IS IN THE CHUNK BUT NOT IN THE SENTENCE THIS CLAIM
        # MATCHES. Structural, and with no threshold in it: the claim's own
        # nearest sentence is the one it is about, and a value it states that
        # is somewhere ELSE in the chunk has been moved between subjects.
        #
        # WHAT THIS COSTS, said rather than discovered. A draft that honestly
        # summarises TWO sentences of one chunk - "ducts are 25mm and pipes
        # 50mm" - is flagged too, because a checker with no model (R-47)
        # cannot tell that from the swap above. That is the safe direction and
        # it has a remedy the system already provides: cite each clause
        # separately, which is what a chunk-level citation is for. It is a
        # flag, never a rewrite (R-53), so the cost is a person reading a line.
        elsewhere = [fact for fact in found if fact not in in_nearest]
        if elsewhere:
            claims.append(Claim(sentence, MISPLACED, kind=kind, ratio=ratio,
                                threshold=THRESHOLDS[kind], citation=citation,
                                added=elsewhere))
            continue

        # R-51, AND IT IS A RULE RATHER THAN A THRESHOLD. Every fact the claim
        # states is in the sentence it is about, so it cannot have invented one
        # and cannot have moved one.
        verdict = (UNDERSTATED if len(in_nearest) > len(found) else GROUNDED)

        # The one gate that survived measurement: a quotation must be IN its
        # source. Everything else is reported and not enforced.
        gate = THRESHOLDS[kind]
        # AGAINST EVERY CITED BODY, NOT THE FIRST. The round before combined
        # FACTS across all the cited chunks and left this line reading `body`,
        # which is only the first citation - so the quoted form of the same
        # conflict sentence was still flagged:
        #
        #   Company says "…25mm" [a], while project says "…50mm" [b]
        #
        # Each quotation occurs exactly in its own source and the check saw
        # only one of them. Fixed one half of a finding and left its twin,
        # which is the shape this plan keeps producing. Found by a review
        # 2026-09-11.
        held = coverage(sentence, bodies) if kind == QUOTE else None
        if gate is not None and held is not None and held < gate:
            verdict = FLAGGED
            claims.append(Claim(sentence, verdict, kind=kind, ratio=held,
                                threshold=gate, citation=citation,
                                added=["words not in the source"]))
            continue

        claims.append(Claim(sentence, verdict, kind=kind, ratio=ratio,
                            threshold=gate, citation=citation))

    return Report(claims, THRESHOLDS, len(by_chunk))


def main(argv):
    argv = list(argv)
    question = ""
    if "--question" in argv:
        i = argv.index("--question")
        question = argv[i + 1] if i + 1 < len(argv) else ""
        del argv[i:i + 2]
    draft_path = None
    if "--draft" in argv:
        i = argv.index("--draft")
        draft_path = argv[i + 1] if i + 1 < len(argv) else None
        del argv[i:i + 2]

    # A FLAG THIS TOOL DOES NOT HAVE WAS DROPPED WITHOUT A WORD, and the check
    # then ran on as though nothing had been typed. Measured before this:
    # `--draft <file> --question "duct insulation" --rebuild` lost the flag,
    # opened the store and answered the question alone. heron_retrieve.main
    # states the reason this is refused rather than ignored - a typo silently
    # searched for "reads as a measured result rather than a typo" - and
    # heron_conflict and heron_research answer in the same words. REFUSED
    # BEFORE THE BRAIN IMPORTS, so a typo costs nothing. Row 5b-112.
    unknown = [word for word in argv if word.startswith("-")]
    if unknown:
        print("  not a flag this tool has: %s" % " ".join(unknown))
        print('  python brain/heron_ground.py --draft <file> --question "..."')
        print("  the flags are --draft and --question, and each takes a value")
        return 2

    if not draft_path or not question:
        print('  python brain/heron_ground.py --draft <file> --question "..."')
        print("  the draft is checked against the clauses the question")
        print("  retrieves. Nothing is rewritten - the report is the output.")
        return 2

    import heron_scope as SCOPE
    import heron_search as SEARCH
    import heron_embed as EMBED
    import heron_context as CTX

    with open(draft_path, encoding="utf-8") as handle:
        draft = handle.read()

    store = SCOPE.open_scope(SCOPE.GLOBAL)
    try:
        SEARCH.index_chunks(store)
        EMBED.index_chunks(store)
        try:
            packet = CTX.assemble(store, question, path=CTX.STANDARDS)
        except CTX.SourceMissing as refused:
            print("  REFUSED  %s" % refused)
            return 2
        report = check(draft, packet)
        for line in report.lines():
            print(line)
        return 0 if report.ok else 1
    finally:
        store.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
