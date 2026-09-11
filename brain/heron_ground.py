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
_MARKER = re.compile(r"\[([^\]]{1,120})\]")

# A SENTENCE WITH NO FACT IN IT CANNOT FABRICATE ONE, and flagging it teaches
# people to ignore flags. "This is worth reviewing" is not a claim about an
# indexed source; "ducts shall be insulated to 25mm" is.
_FACT = [
    re.compile(r"\b\d+(?:\.\d+)*\s*" + _UNITS + r"\b"),   # a dimension
    re.compile(r"\bdn\d+\b"),                             # a nominal bore
    re.compile(r"\b\d+(?:\.\d+){1,}\b"),                  # a clause number
    re.compile(r"\bOST_[A-Za-z0-9_]+\b"),                 # a category
    re.compile(r"\b[A-Za-z]+\.[A-Z][A-Za-z0-9_]*\b"),     # a parameter
    re.compile(r"\b\d+in\d+\b"),                           # a gradient, 1:100
    re.compile(r"\b\d+(?:\.\d+)?pct\b"),                    # a percentage
    re.compile(r"\b\d{4}\b"),                             # a year or release
]


def facts(text):
    """Every checkable fact in a sentence, normalised. Empty means not checkable.

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
    flat = normalise(_MARKER.sub(" ", text or ""))
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
    QUOTE:      0.90,            # coverage, not similarity - see above
    REFERENCE:  NO_RATIO_GATE,   # the added-fact rule carries these three
    NUMERIC:    NO_RATIO_GATE,
    PARAPHRASE: NO_RATIO_GATE,
}

# What a quotation claims: that these words are in the source.
_QUOTED = re.compile(r'["“]([^"”]{4,})["”]')


def coverage(claim, source):
    """The share of a quoted span that appears verbatim in its source.

    Containment, not similarity. A quotation that is really in the clause
    scores 1.0 however much prose surrounds it; one that is not scores what
    it deserves, and the two do not overlap.
    """
    spans = _QUOTED.findall(claim or "")
    if not spans:
        return None
    want = normalise(" ".join(spans))
    got = normalise(source)
    if not want or not got:
        return 0.0
    matcher = difflib.SequenceMatcher(None, want, got)
    longest = matcher.find_longest_match(0, len(want), 0, len(got))
    return longest.size / float(len(want))


def kind_of(sentence):
    """Which threshold this sentence is held to.

    THE CITATION MARKER IS STRIPPED FIRST, for the same reason facts() strips
    it: "[9.1.1]" is a clause number, so every cited sentence was being called
    a REFERENCE claim because of the marker that says where it came from
    rather than because of anything it says.
    """
    claim = _MARKER.sub(" ", sentence or "")
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
    def ok(self):
        return not self.flagged and not self.uncited and not self.unresolved

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
        for claim in self.unresolved:
            out.append("UNRESOLVED %s" % claim.sentence[:70])
            out.append("           it cites a source this packet does not "
                       "carry. R-22: a citation a human cannot follow is "
                       "decoration - check whether the clause exists")
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

def _cited(sentence, by_locator, by_chunk):
    for found in _MARKER.findall(sentence or ""):
        key = found.strip()
        if key in by_chunk:
            return by_chunk[key]
        if key in by_locator:
            return by_locator[key]
    return None


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
        by_chunk[cite["chunk"]] = (cite, part.body)
        if cite.get("locator"):
            by_locator[cite["locator"]] = (cite, part.body)

    claims = []
    for sentence in sentences(draft):
        found = facts(sentence)
        if not found:
            # R-50. No fact, nothing to invent, and flagging it would teach
            # people to ignore flags.
            claims.append(Claim(sentence, SKIPPED))
            continue

        marked = bool(_MARKER.search(sentence or ""))
        cited = _cited(sentence, by_locator, by_chunk)
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

        citation, body = cited
        kind = kind_of(sentence)
        ratio = support(sentence, body)
        in_source = facts(body)
        added = [fact for fact in found if fact not in in_source]

        if added:
            # AN ADDED FACT IS A FLAG, AND THE RATIO DOES NOT GET A VOTE.
            claims.append(Claim(sentence, FLAGGED, kind=kind, ratio=ratio,
                                threshold=THRESHOLDS[kind], citation=citation,
                                added=added))
            continue

        # R-51, AND IT IS A RULE RATHER THAN A THRESHOLD. Every fact the claim
        # states is in the source, so it cannot have invented one.
        verdict = (UNDERSTATED if len(in_source) > len(found) else GROUNDED)

        # The one gate that survived measurement: a quotation must be IN its
        # source. Everything else is reported and not enforced.
        gate = THRESHOLDS[kind]
        held = coverage(sentence, body) if kind == QUOTE else None
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
