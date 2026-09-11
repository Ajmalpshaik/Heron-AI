# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-RAG-RSH-017
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
What Heron does not know, said precisely - and what an outside answer must carry.

    python brain/heron_research.py "what is the minimum duct insulation" \
        --scopes company,project --project "Tower B"
    python brain/heron_research.py --check draft.md

Stage 9 of docs/work-notes/plans/rag/00-structure.md s6. Closes R-26.

HERON DOES NOT FETCH, AND THAT IS THE STAGE
--------------------------------------------
"The Research agent may answer from OUTSIDE Heron's knowledge" reads as *give
Heron a web client*. It does not get one, for three reasons that stack:

  D-01     every model call is the host's, because Heron is a per-user install
           with no admin rights and no server. The network is the same
           boundary: Heron has no keys, no proxy policy and no way to promise
           a connection.
  D-25     the offline premise. docs/DECISIONS.md: "It has to work with no
           connection. Site visits, locked-down networks, a laptop on a
           plane." A capability that only works online is one that fails on
           exactly the days a modeller is on site.
  GR 19    an ingested file is at least a file somebody chose to put in. A web
           page is text a stranger controls, arriving at the moment of the
           question. If a document is DATA AND NEVER INSTRUCTION, a fetched
           page is that rule under load.

So the division is: **Heron says what it does not know and what an answer must
carry; the host, which has the model and the network, goes and finds out; Heron
then checks the SHAPE of what comes back.** A test greps this module for
`http`, `requests`, `urllib` and `socket`, the same way tests/test_ground.py
enforces R-47 - so adding a fetch is a deliberate act that fails the suite.

WHY THIS STAGE IS LAST, IN THE PLAN'S OWN WORDS
------------------------------------------------
02-implementation.md s12: *"an external answer without a working citation
system is exactly the invented-standard failure 05 s8 forbids - and it is the
most convincing kind of wrong answer the system can produce."*

That is the whole argument for the order. An answer from inside Heron carries a
chunk id that resolves to text a person can read. An answer from outside
carries whatever the model wrote down, and a model writing "per ISO 19650"
under a confident paragraph is indistinguishable, to a reader in a hurry, from
a citation. docs/05 s8 calls that the invented-standard failure and R-21 makes
it a BUG rather than a low-confidence answer.

WHAT CAN BE CHECKED, AND WHAT CANNOT
-------------------------------------
This is the honest boundary of the stage and it is stated before the code
rather than discovered after it.

  CHECKABLE     whether a claim carries a citation at all (R-21, R-65)
                whether that citation could be LOOKED UP by a person - a
                document that can be named, an edition that pins which one,
                and a locator that points inside it
                whether the answer is presented as external rather than as
                something Heron knows

  NOT CHECKABLE whether the claim is TRUE. Heron has not read the source and
                cannot. There is no chunk, no text, and nothing to compare -
                heron_ground.check() needs a packet and there is no packet.

So every external claim ends at UNVERIFIED, however well-formed it is, and no
verdict in this module can ever say otherwise. A test asserts that WELL_FORMED
is not an approval and that no verdict named `verified`, `correct` or `true`
exists - the same shape as R-53's test, and for the same reason: the day
somebody adds one, the suite says so.

THE WAY AN EXTERNAL ANSWER BECOMES A CHECKABLE ONE
---------------------------------------------------
It stops being external. Get the document the answer cites, put it in with

    python brain/heron_ingest.py <file> --scope company

and the next asking is answered from inside, with a chunk id, through
heron_ground. That is the only route from UNVERIFIED to grounded, it is one
command, and the brief says so every time rather than leaving it implied.
"""

from __future__ import print_function

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_retrieve as RETRIEVE                                # noqa: E402
import heron_ground as GROUND                                    # noqa: E402


# ---------------------------------------------------------------------------
# When Heron has actually missed
# ---------------------------------------------------------------------------

# The routes that ARE a miss, by name rather than by inference. R-19 made these
# three different sentences on purpose - an empty store, an unbuilt index and a
# genuine no-match need three different actions - and all three mean the same
# thing here: nothing in this scope answered.
FOUND_NOTHING = ("empty", "unindexed", "nothing")


class Searched(object):
    """One scope, and what asking it produced. A fact, not a judgement."""

    def __init__(self, label, route, note, clauses=0, skipped=None):
        self.label = label
        self.route = route
        self.note = note
        self.clauses = clauses
        self.skipped = skipped

    @property
    def missed(self):
        """Whether this scope was ASKED and returned nothing.

        A SKIPPED SCOPE IS NOT A MISS, and the first version counted it as
        one. In the default company,project workflow before a model has
        supplied its project key, the project store is not opened at all - so
        an empty company store plus a skipped project store made the gap
        CERTAIN and the brief said Heron has no knowledge on the question,
        while the project specification sat unopened and quite possibly
        holding the answer. Sending somebody to the internet for a clause that
        is in their own project spec is the worst outcome this stage has.
        Found by a review 2026-09-11, hours after the stage was written.
        """
        return not self.skipped and self.route in FOUND_NOTHING

    @property
    def unasked(self):
        """Whether this scope was never opened. A prerequisite, not a result."""
        return bool(self.skipped)

    def __repr__(self):
        return "<searched %s %s>" % (self.label, self.skipped or self.route)


class Gap(object):
    """What Heron does not know about one question, and how sure it is.

    `certain` IS NARROWER THAN IT LOOKS, AND THE NARROWNESS IS W-8.

    Heron can say with certainty that a scope returned nothing: the route says
    so by name. What it CANNOT say is whether clauses that DID come back have
    any claim on the question - that needs a retrieval floor, W-8 records that
    no floor can be derived on the lexical backend at this corpus size, and
    R-60 forbids inventing one from a hand-written word list.

    So there are two gaps and only one of them is detectable:

        certain       every scope asked returned nothing. Research is
                      warranted and Heron can say so.
        undetectable  clauses came back. They may answer the question or they
                      may be five clauses about anything, and nothing here can
                      tell the difference. Reported as exactly that.

    A third state is deliberately absent: this class never decides that
    research IS needed when clauses came back. R-62 puts that judgement in the
    host, which is also the only party that has read the question and the
    clauses together.
    """

    def __init__(self, question, searched):
        self.question = question
        self.searched = list(searched)

    @property
    def unasked(self):
        """The scopes that were never opened. Each is a prerequisite, not a miss."""
        return [one for one in self.searched if one.unasked]

    @property
    def certain(self):
        """EVERY named scope ran, and every one came back with nothing.

        Both halves are required. A scope that was skipped has not answered
        the question either way - it was never opened - and treating that as
        "nothing there" is how Heron would send somebody outside for a clause
        sitting in their own project specification.
        """
        if not self.searched or self.unasked:
            return False
        return all(one.missed for one in self.searched)

    @property
    def clauses(self):
        return sum(one.clauses for one in self.searched)

    def sentence(self):
        """The gap in words, because words are the contract (S-3)."""
        if not self.searched:
            return ("NO SCOPE WAS ASKED, so nothing is known about what Heron "
                    "holds on this. Name at least one scope.")
        if self.unasked:
            return ("%d OF THE SCOPES NAMED WAS NEVER OPENED, so nothing is "
                    "known about what it holds: %s. That is a PREREQUISITE, "
                    "not a result - settle it before going outside, because a "
                    "clause sitting in an unopened project specification is "
                    "the worst thing to research past."
                    % (len(self.unasked),
                       "; ".join("%s (%s)" % (one.label, one.skipped)
                                 for one in self.unasked)))
        if self.certain:
            return ("NOTHING IN THE SCOPES ASKED ANSWERS THIS. Every one was "
                    "opened and every one came back empty, unindexed, or with "
                    "no match - so an outside answer is not being preferred "
                    "over Heron's own knowledge, because Heron has none on "
                    "this question.")
        return ("HERON RETURNED %d CLAUSE(S) FOR THIS, and whether any of them "
                "ANSWERS it is not established. Retrieval cannot yet refuse a "
                "question nothing covers - the floor that would do it has to "
                "come from a measurement, and the measurement says it cannot "
                "be derived on this backend (W-8). READ THEM FIRST. Research "
                "is warranted only if they do not answer, and that is your "
                "judgement, not Heron's (R-62)." % self.clauses)


def gap(question, asked):
    """What Heron does not know, read off the Librarian's own answers.

    `asked` is a list of heron_retrieve.Asked - the SAME answers the caller was
    shown, not a second search. Stage 8 learned this the hard way: asking again
    inside a second module gave two shortlists that a finishing warm-up or a
    changed file could make different, so the sentence printed underneath an
    answer could be about other clauses than the ones above it.
    """
    searched = []
    for one in asked:
        if one.skipped:
            searched.append(Searched(one.label, None, None, 0, one.skipped))
            continue
        answer = one.answer
        if answer is None:
            searched.append(Searched(one.label, None, None, 0,
                                     "it was asked and returned no answer"))
            continue
        searched.append(Searched(one.label, answer.route, answer.note,
                                 len(answer.candidates or [])))
    return Gap(question, searched)


# ---------------------------------------------------------------------------
# The brief - what a usable outside answer has to carry
# ---------------------------------------------------------------------------

# WHAT MAKES A CITATION SOMETHING A PERSON CAN ACT ON.
#
# Not a style preference. Each of these three exists because leaving it out has
# a known way of going wrong on a Qatar project:
#
#   the document   "per the standard" names nothing. A modeller cannot open it,
#                  a checker cannot verify it, and a client cannot accept it.
#   the edition    "ISO 19650" is five parts across several years, and QCS 2014
#                  is not QCS 2010. A clause number without an edition points
#                  into whichever copy the reader happens to have.
#   the locator    a document without a clause number means "somewhere in 400
#                  pages", which in practice means nobody checks.
CONTRACT = (
    "  1. THE DOCUMENT, named so it can be found - the issuing body and the "
    "number or title.\n"
    "     'per the standard' and 'industry practice' are not documents.\n"
    "  2. THE EDITION OR YEAR. ISO 19650 is five parts across several years, "
    "and QCS 2014\n"
    "     is not QCS 2010 - a clause number without an edition points into "
    "whichever copy\n"
    "     the reader happens to be holding.\n"
    "  3. THE CLAUSE, SECTION OR TABLE NUMBER. A document with no locator "
    "means 'somewhere\n"
    "     in four hundred pages', which in practice means nobody checks it.")


def brief(gap_found, scopes=None):
    """The research brief: what was asked, what Heron holds, what an answer owes.

    Written for a host to act on and for a person to read over its shoulder.
    It carries no answer and no opinion about the question - R-62 - only what
    was searched, what came back, and the contract.

    IT NAMES NO INGEST TARGET, and the first version named one by taking the
    FIRST SCOPE SEARCHED. Searching `global,company` therefore printed

        python brain/heron_ingest.py <file> --scope global

    under a company standard - one client's document into the store every
    project on the machine reads, Golden Rule 5 broken by list order. Put
    `project` first and it printed a command heron_ingest refuses outright,
    because the project scope needs a key nothing here has. Search order is
    not storage intent and was never evidence of it. Found by a review
    2026-09-11.

    `scopes` is kept for the searched list and is deliberately no longer read
    for anything that decides where knowledge goes.
    """
    said = ['RESEARCH BRIEF', '', 'Question:  "%s"' % gap_found.question, '']

    said.append("WHAT HERON SEARCHED")
    if not gap_found.searched:
        said.append("  nothing - no scope was named.")
    for one in gap_found.searched:
        if one.skipped:
            said.append("  %-20s NOT ASKED - %s" % (one.label, one.skipped))
            continue
        said.append("  %-20s %s" % (one.label, one.route))
        if one.note:
            said.append("  %-20s %s" % ("", one.note))
    said.append("")

    said.append("WHAT THAT MEANS")
    said.append("  " + gap_found.sentence())
    said.append("")

    said.append("WHAT AN ANSWER MUST CARRY, PER CLAIM")
    said.append(CONTRACT)
    said.append("")

    said.append("WHAT HERON WILL AND WILL NOT DO WITH THE ANSWER")
    said.append("  It will check that every claim CARRIES a citation and that "
                "the citation could")
    said.append("  be looked up. It CANNOT check whether the claim is true: it "
                "has not read the")
    said.append("  source, there is no chunk to compare against, and a "
                "well-formed citation on a")
    said.append("  wrong sentence is the most convincing wrong answer this "
                "system can produce.")
    said.append("  Every external claim ends at UNVERIFIED.")
    said.append("")

    said.append("HOW TO MAKE THE NEXT ASKING ANSWERABLE FROM INSIDE")
    said.append("  Get the document this answer cites and put it in. WHICH "
                "STORE IS YOURS TO")
    said.append("  CHOOSE - it is not the order the scopes were searched in, "
                "and Heron does")
    said.append("  not guess it:")
    said.append("")
    said.append("      python brain/heron_ingest.py <file> --scope company")
    said.append("      python brain/heron_ingest.py <file> --scope global")
    said.append("      python brain/heron_ingest.py <file> --scope project "
                "--project <the project key>")
    said.append("")
    said.append("  global is shared by EVERY project on this machine and "
                "company by every")
    said.append("  project in the firm, so putting a project-specific "
                "specification in either")
    said.append("  is Golden Rule 5 broken by a default. The project scope "
                "needs its key and")
    said.append("  heron_ingest refuses without one.")
    said.append("")
    said.append("  After that the same question is answered with a chunk id "
                "that resolves to")
    said.append("  text a person can read, and heron_check can ground a draft "
                "against it. That")
    said.append("  is the only route from UNVERIFIED to grounded.")
    return "\n".join(said)


# ---------------------------------------------------------------------------
# Checking what came back - its SHAPE, never its truth
# ---------------------------------------------------------------------------

UNCITED = "uncited"          # a claim with no citation at all - R-21, a BUG
VAGUE = "vague"              # a citation a person could not go and look up
WELL_FORMED = "well-formed"  # it names a document, an edition and a locator
NOT_A_CLAIM = "not a claim"  # nothing in it to cite

# EVERY ONE OF THOSE IS STILL THIS. Named as a constant rather than left as a
# string in a print, because the whole stage rests on it: the best verdict this
# module can reach is "well-formed AND unverified", and there is deliberately
# no verdict above it. tests/test_research.py asserts that nothing called
# `verified`, `correct`, `true` or `approved` exists in this module.
UNVERIFIED = "UNVERIFIED - Heron has not read the source"

# A citation, as an external answer writes one. Three shapes are accepted
# because all three are what people actually type:
#
#     ISO 19650-2:2018 clause 5.1.4
#     QCS 2014, Section 22, Part 2.3
#     BS EN 12845:2015 table 3
#
# The parts are found independently rather than by one big pattern, because a
# citation missing its edition has to be reported as MISSING AN EDITION and not
# as "not a citation" - which is a different sentence and a different fix.
_EDITION = re.compile(r"\b(?:19|20)\d{2}\b")
_LOCATOR = re.compile(
    r"\b(?:clause|section|sub-?clause|sub-?section|table|figure|appendix|"
    r"annex|part|paragraph|item)\s*\.?\s*[0-9a-z]+(?:[.\-][0-9a-z]+)*", re.I)
_BARE_LOCATOR = re.compile(r"\b\d+(?:\.\d+){1,}\b")
# WHO ISSUED IT, AND - JUST AS OFTEN - WHAT IT IS CALLED.
#
# An acronym list alone refused the two documents this entire system exists
# for. "Acme Engineering BIM Standard 2026, clause 3.1" meets the stated
# contract in full and came back VAGUE, missing "the document", because no
# issuing body in the list appears in it. The company standard and the project
# specification are precisely the sources a modeller cites, and a hand-written
# catalogue of issuers could never contain them. Found by a review 2026-09-11.
#
# So there are two ways to name a document, and the second needs no list: a
# PROPER NAME is two or more capitalised words running together. "Acme
# Engineering BIM Standard" is one; "Company requires" is not, because only a
# sentence's first word is capitalised in ordinary prose.
_ISSUER = (r"ISO|BS|EN|DIN|ASTM|ASHRAE|NFPA|SMACNA|CIBSE|AWS|IEC|QCS|QCDD|"
           r"ASHGHAL|KAHRAMAA|UNICLASS|IFC")

# The issuer plus the few tokens that belong to it - a number, a part, an
# edition. It stops at the first lower-case word, which is where the citation
# ends and the sentence resumes. The first version ran on for forty characters
# and swallowed "requires X in the workflow d" into the document name.
_DOCUMENT = re.compile(
    r"\b(?:" + _ISSUER + r")\b(?:[\s:/-]+[A-Z0-9][\w.:/-]*){0,4}")

# Two or more capitalised words together. A locator word may not start it -
# "Section 2" is where to look, not what to look in.
_NAMED = re.compile(
    r"\b(?!(?:Section|Clause|Table|Figure|Appendix|Annex|Part|Item|Rule|"
    r"Paragraph)\b)"
    r"[A-Z][A-Za-z0-9&.\-]*(?:\s+[A-Z][A-Za-z0-9&.\-]*){1,7}")

# Words that read like a source and name none. Every one of these has been
# written under a confident paragraph by something that had no source at all.
_NOT_A_DOCUMENT = (
    "the standard", "industry standard", "industry practice", "best practice",
    "common practice", "the code", "local regulations", "the regulations",
    "standard practice", "the specification", "the spec", "generally accepted",
    "it is standard", "typically", "usually", "most projects", "the guidelines",
)


# AT MOST THIS MANY WORDS MAY SIT BETWEEN A DOCUMENT AND ITS LOCATOR.
#
# Two, which covers every way a citation is actually written - "QCS 2014,
# Section 22", "ISO 19650-2:2018 clause 5.1.4", "Acme Standard 2026, at
# clause 3.1" - and excludes a locator that belongs to a different sentence
# entirely. Not a threshold on a measurement (R-60 has nothing to bite on): it
# is a statement about where a citation's parts sit relative to each other.
_LOCATOR_GAP = 2


def _follows(text, document, where):
    """Whether the locator at `where` belongs to this document reference."""
    at = text.find(document)
    if at < 0:
        return True
    end = at + len(document)
    if where < end:
        return True
    between = text[end:where].strip(" ,;:-")
    return len(between.split()) <= _LOCATOR_GAP


class Cited(object):
    """One citation an external answer offered, and what it is missing."""

    def __init__(self, text, document=None, edition=None, locator=None):
        self.text = text
        self.document = document
        self.edition = edition
        self.locator = locator

    @property
    def missing(self):
        """Which of the three the contract asks for are absent."""
        gone = []
        if not self.document:
            gone.append("the document")
        if not self.edition:
            gone.append("the edition or year")
        if not self.locator:
            gone.append("the clause or section number")
        return gone

    @property
    def verdict(self):
        return VAGUE if self.missing else WELL_FORMED

    def __repr__(self):
        return "<cited %s %s>" % (self.verdict, self.text[:40])


def citation(text):
    """The citation in one sentence, or None. Reports parts, never guesses them."""
    if not text:
        return None
    flat = " ".join(str(text).split())

    document = None
    found = _DOCUMENT.search(flat)
    if found:
        document = found.group(0).strip(" ,;:-")
    else:
        # No issuing body named. A proper name will do, and for a company or
        # project document it is the only thing there is.
        for maybe in _NAMED.finditer(flat):
            name = maybe.group(0).strip(" ,;:-")
            if name.lower() in _NOT_A_DOCUMENT:
                continue
            document = name
            break

    # THE EDITION HAS TO BELONG TO THE DOCUMENT, and scanning the whole
    # sentence for a year meant any year satisfied the contract:
    #
    #     "Install by 2026 per ISO 19650 clause 5.1"
    #        -> well-formed, edition 2026
    #
    # ISO 19650 gave no edition at all; a delivery date was read as one, and
    # the citation still cannot be looked up. So the year is searched for in
    # the document reference and the few characters that follow it -
    # "ISO 19650-2:2018", "QCS 2014", "BS EN 12845:2015" - and nowhere else.
    # Found by a review 2026-09-11.
    edition = None
    if document:
        at = flat.find(document)
        span = flat[at:at + len(document) + 8] if at >= 0 else document
        found = _EDITION.search(span)
        if found:
            edition = found.group(0)

    # THE LOCATOR BELONGS TO THE DOCUMENT, the same way the edition does - and
    # the round that bound the edition left this half alone. Searched across
    # the whole sentence:
    #
    #     "ISO 19650:2018 requires X in the workflow described in Section 2"
    #        -> well-formed, locator "Section 2"
    #
    # Section 2 is a section of the ANSWER, not of ISO 19650, so the citation
    # was reported as something a person could look up when no locator within
    # that document had been given at all. A locator that belongs to a
    # citation follows it closely: a comma, a "clause", at most a word or two.
    # Found by a review 2026-09-11.
    locator = None
    for found in _LOCATOR.finditer(flat):
        if document and not _follows(flat, document, found.start()):
            continue
        locator = found.group(0)
        break
    if locator is None:
        # A BARE DOTTED NUMBER COUNTS ONLY WHEN A DOCUMENT WAS NAMED, and that
        # condition is the whole of why this is a separate pattern. "5.1.4" in
        # "ISO 19650-2:2018, 5.1.4" is a clause; "5.1.4" in "a fall of 1.5 to
        # 2.5" is a measurement. heron_graph learned the same lesson from the
        # other side and a review had to point it out twice.
        for found in _BARE_LOCATOR.finditer(flat):
            if not document or not _follows(flat, document, found.start()):
                continue
            locator = found.group(0)
            break

    lowered = flat.lower()
    vague_words = [phrase for phrase in _NOT_A_DOCUMENT if phrase in lowered]
    if not document and not edition and not locator and not vague_words:
        return None
    return Cited(flat, document, edition, locator)


# HOW A STANDARD WRITES A REQUIREMENT. Grammar, not vocabulary.
#
# This is deliberately NOT a list of BIM words, which is what R-60 forbids and
# for the reason it gives - a domain word list is wrong the week it is written
# and nobody maintains it. "shall", "must", "is required to" is how ISO, BS,
# NFPA and QCS each write a requirement, in English, regardless of subject. A
# sentence carrying one is asserting an obligation, and an obligation with no
# source behind it is the thing this whole stage exists to catch.
_NORMATIVE = re.compile(
    r"\b(?:shall|must|should|is\s+required|are\s+required|requires?|"
    r"mandator\w*|mandates?|prohibit\w*|permitted|allowed|complies\s+with|"
    r"comply\s+with|conform\w*\s+to)\b", re.I)


def _is_claim(sentence):
    """Whether this sentence asserts anything a source could support.

    WIDER THAN heron_ground's TEST, AND THE DIFFERENCE IS THE POINT. Grounding
    asks "is there a VALUE here that could be fabricated?", because it is about
    to compare that value against a chunk. This asks "is this sentence
    asserting something a source should back?", which is a different and
    broader question - and the first version borrowed the narrower one.

    Measured on its own output the same afternoon it was written:

        "Revisions are lettered per the standard."   ->   not a claim

    No number, so no fact, so never checked - while `per the standard` is the
    exact phrase _NOT_A_DOCUMENT exists to catch. The module's own vague-source
    list was unreachable for the sentence it was written for. Found by running
    the seam end to end rather than by a review.

    So four things make a sentence checkable here: a measured fact, a
    quotation, a normative verb, or a phrase that CLAIMS a source without
    naming one.

    ONE LIMIT, RECORDED RATHER THAN DISCOVERED: a bare assertion with no
    number, no modal and no source phrase - "Container names use six fields" -
    still reads as prose and is not checked. Widening further would start
    flagging "Let me know if you need more detail", and a report that flags
    everything is one nobody reads. Where that matters, the sentence almost
    always carries a number or a "shall" in any real standards answer.
    """
    text = sentence or ""
    if GROUND.facts(text):
        return True
    if re.search(r'["“”]', text):
        return True
    if _NORMATIVE.search(text):
        return True
    lowered = " ".join(text.lower().split())
    return any(phrase in lowered for phrase in _NOT_A_DOCUMENT)


class Claim(object):
    """One sentence of an external answer, and what its citation is worth."""

    def __init__(self, sentence, verdict, cited=None):
        self.sentence = sentence
        self.verdict = verdict
        self.cited = cited

    def __repr__(self):
        return "<claim %s>" % self.verdict


class Report(object):
    """What an external answer's citations are worth. NEVER whether it is right."""

    def __init__(self, claims):
        self.claims = claims

    @property
    def checked(self):
        return [c for c in self.claims if c.verdict != NOT_A_CLAIM]

    @property
    def uncited(self):
        return [c for c in self.claims if c.verdict == UNCITED]

    @property
    def vague(self):
        return [c for c in self.claims if c.verdict == VAGUE]

    @property
    def well_formed(self):
        return [c for c in self.claims if c.verdict == WELL_FORMED]

    @property
    def ok(self):
        """Every checkable claim carries a citation a person could look up.

        NOT "the answer is right", and the name is the closest this module
        comes to that word. See UNVERIFIED.
        """
        return not self.uncited and not self.vague

    def lines(self):
        """The report a person reads. R-52: the denominator, then the detail."""
        out = ["Checked %d claim(s) of %d sentence(s) in an EXTERNAL answer."
               % (len(self.checked), len(self.claims)),
               "",
               "THIS CHECKS CITATIONS, NOT FACTS. Heron has not read any of "
               "these sources.",
               "Every claim below is %s." % UNVERIFIED,
               ""]

        if self.uncited:
            out.append("%d claim(s) CITE NOTHING AT ALL. docs/05 s8 and R-21: "
                       "a standards claim" % len(self.uncited))
            out.append("with no source is a BUG, not a low-confidence answer.")
            for claim in self.uncited:
                out.append("  %s" % claim.sentence.strip())
            out.append("")

        if self.vague:
            out.append("%d citation(s) could NOT be looked up:" % len(self.vague))
            for claim in self.vague:
                out.append("  %s" % claim.sentence.strip())
                out.append("      missing: %s"
                           % ", ".join(claim.cited.missing))
            out.append("")

        if self.well_formed:
            out.append("%d citation(s) name a document, an edition and a "
                       "locator:" % len(self.well_formed))
            for claim in self.well_formed:
                out.append("  %s" % claim.sentence.strip())
            out.append("      Well-formed is not correct. Open them.")
            out.append("")

        out.append("To make any of this checkable, ingest the source and ask "
                   "again:")
        out.append("    python brain/heron_ingest.py <file> --scope company")
        return out


def check(answer):
    """An external answer, in. A report on its citations, out. Never a rewrite.

    R-53 holds here exactly as it does in heron_ground: this reports and does
    not repair. A module that corrected its own findings would turn a wrong
    answer into an invisible one, and an external wrong answer is the one this
    repository is most afraid of.
    """
    claims = []
    for sentence in GROUND.sentences(answer):
        if not _is_claim(sentence):
            claims.append(Claim(sentence, NOT_A_CLAIM))
            continue
        cited = citation(sentence)
        if cited is None:
            claims.append(Claim(sentence, UNCITED))
            continue
        claims.append(Claim(sentence, cited.verdict, cited))
    return Report(claims)


# ---------------------------------------------------------------------------
# The command
# ---------------------------------------------------------------------------

def _flag(argv, name, default=None):
    if name in argv:
        i = argv.index(name)
        value = argv[i + 1] if i + 1 < len(argv) else None
        if value is None or value.startswith("--"):
            raise ValueError("%s needs a value and was given none." % name)
        del argv[i:i + 2]
        return value
    return default


def main(argv):
    argv = list(argv)
    try:
        draft = _flag(argv, "--check")
        scopes = _flag(argv, "--scopes", "global")
        project = _flag(argv, "--project")
    except ValueError as why:
        print("  %s" % why)
        return 2

    stray = [a for a in argv if a.startswith("-")]
    if stray:
        # The same refusal heron_ingest carries, and for a weaker version of
        # the same reason: a mistyped flag here cannot publish anything, but a
        # command that silently ignores half of what it was given is how a
        # person comes to trust an answer built from different inputs.
        print("  not a flag this tool has: %s" % " ".join(stray))
        return 2

    if draft:
        if not os.path.isfile(draft):
            print("  no file at %s" % draft)
            return 2
        with open(draft, encoding="utf-8") as handle:
            report = check(handle.read())
        print("Citation check - %s"
              % ("every claim carries a citation that could be looked up"
                 if report.ok else "SOMETHING IS FLAGGED"))
        print()
        print("\n".join(report.lines()))
        return 0 if report.ok else 1

    if not argv:
        print('  python brain/heron_research.py "<question>" --scopes company,project')
        print('  python brain/heron_research.py --check <draft file>')
        print("  Heron never fetches. It says what it does not know and what")
        print("  an outside answer must carry.")
        return 2

    question = " ".join(argv)
    wanted = [s.strip().lower() for s in scopes.split(",") if s.strip()]
    asked = RETRIEVE.librarian(question, scopes=wanted, project=project)
    print(brief(gap(question, asked), wanted))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
