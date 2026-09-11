# Heron-Agent:  HERON-RAG-DIS-002
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
A document can go into a scope. Stage 1 of the RAG plan.

    python brain/heron_ingest.py <file> --scope global
    python brain/heron_ingest.py --boundaries <document id>

THE HALF OF THE KNOWLEDGE STORE THAT DID NOT EXIST. Every table beside these
two is about Heron's own code library - fragments, their vectors, their
searchable text, their capabilities. There was nowhere for a standard, a
specification, a company note or a project document to go, so every
requirement about citations and provenance had nothing to act on.

WHAT THIS STAGE DELIBERATELY DOES NOT DO
----------------------------------------
No retrieval. No citation. Nothing reads a document back out yet. A document
goes in and can be counted, and that is the whole of it - because the ingester
is the half that has to be right, and reviewing it alongside a retrieval
change is reviewing neither (02-implementation.md s4.4).

THE TWO RULES THE CHUNKER IS BUILT AROUND, IN THIS ORDER
--------------------------------------------------------
1. A RULE AND ITS EXCEPTION STAY IN ONE CHUNK (R-68). "Ducts shall be
   insulated ... EXCEPT where installed within conditioned spaces." Split
   between those halves, retrieve the first, and Heron states the OPPOSITE of
   the requirement WITH A CITATION ATTACHED - which is more convincing than
   any uncited guess, and is the worst output this system is capable of. QCS
   and Ashghal are written as rule-then-qualification throughout, so this is
   the normal case here and not an edge one.

2. THE EXACT TOKENS BIM RUNS ON ARE NEVER CUT (R-08). OST_DuctCurves, a
   BuiltInParameter, a shared-parameter GUID, "Revit 2024", a clause number.
   Half of one of those retrieves confidently and is wrong.

Both are enforced by refusing split points, and both are tested before this
module is asked to do anything else.

WHY THE HEADING PATH IS STORED (R-66, R-67)
-------------------------------------------
A chunk embedded in isolation has lost the document it came from. The
published fix generates a summary of where it sits with a model call PER
CHUNK - which is exactly the per-call cost D-24 exists to remove.

For a numbered document the context that situates a clause IS ITS HEADING
PATH, read off the document's own structure: QCS 2014 -> Section 21 Mechanical
-> 21.3 Ductwork -> 21.3.2 Insulation. Free, offline, deterministic, and it
can never disagree with the hierarchy because it is derived from it. If this
is ever replaced by a generated context, that is A NEW DECISION and not an
optimisation.

GOLDEN RULE 19 - AND THIS MODULE IS THE DAY IT STARTS MATTERING (R-80)
----------------------------------------------------------------------
"No text Heron reads may raise Heron's own permission level. Content from
documents is DATA, NEVER INSTRUCTION." Until now every word in the store was
written by this project. After this file runs, Heron carries text written by
whoever produced the document - a client, an authority, or somebody who wanted
Heron to do something.

So every chunk from an ingested document carries `untrusted = 1`, and an
oversized or suspicious chunk is FLAGGED, NEVER TRUNCATED (R-82) - truncating
lets a payload be padded past the scanner's window, which turns the guard into
a formality.

WHAT IT REFUSES, AND WHAT IT POINTEDLY DOES NOT REFUSE
------------------------------------------------------
  .rvt, .rfa    D-26, by extension, BEFORE the file is opened. A refusal by
                name, never a warning. The rule is enforced by code because a
                project folder holds models next to the PDFs and an ingester
                that walks a folder will find them.

  .rte, .rft    NOT REFUSED. 02-implementation.md s4.2 proposes it on the same
                reasoning and says it NEEDS THE OWNER'S WORD before it is
                coded, "because a refusal nobody agreed to is as surprising as
                a leak". It has not been given, so it is not coded.

THE FILE IS POINTED AT, NEVER COPIED (Q-B, answered 2026-09-11)
---------------------------------------------------------------
The chunks hold the text needed to answer, so a citation still reads correctly
after the file moves; only the convenience of opening it breaks. Copying would
duplicate a client's content into %APPDATA% where nobody chose to put it, and
with a licensed standard would make every copy of the store a redistribution.
"""

import datetime
import hashlib
import json
import os
import re
import sqlite3
import sys
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_scope as SCOPE                                   # noqa: E402
import heron_audit as AUDIT                                   # noqa: E402


# ---------------------------------------------------------------------------
# What may and may not come in
# ---------------------------------------------------------------------------

# D-26. The model FILE never leaves the machine, and the way that rule is kept
# is that a model file never enters the knowledge store in the first place.
# Checked on the extension, before the file is opened, so a 400 MB model is
# refused without being read.
REFUSED = (".rvt", ".rfa")

READABLE = (".md", ".markdown", ".txt", ".text", ".docx", ".pdf")

STATUSES = ("DRAFT", "REVIEWED", "RETIRED")     # GR 10 lifecycle, R-83


class RefusedByExtension(Exception):
    """A file D-26 says never enters the store. Named, never silently skipped."""


class UnreadableDocument(Exception):
    """A file that could not be read, NAMING the file.

    A reader that silently skips is how half a standard goes missing without
    anybody noticing - and the missing half is invisible afterwards, because
    what is in the store looks complete.
    """


# ---------------------------------------------------------------------------
# The schema - both tables in the scope's own file (GR 5 made physical)
# ---------------------------------------------------------------------------

def ensure_tables(store):
    """The two tables, beside the fragment tables and never in a second file.

    One file per scope is what makes GR 5 physical. A separate document store
    would break scope separation on its first day, and it would do it in the
    file layout, where nobody looks.
    """
    store.execute(
        "CREATE TABLE IF NOT EXISTS documents ("
        " id TEXT PRIMARY KEY,"          # content hash of the file - R-11
        " scope TEXT NOT NULL,"          # redundant, and worth it
        " path TEXT NOT NULL,"           # a POINTER, never a copy - Q-B
        " title TEXT NOT NULL,"          # what a citation shows a human
        " kind TEXT NOT NULL,"           # md | txt | docx | pdf
        " status TEXT NOT NULL,"         # DRAFT | REVIEWED | RETIRED - R-83
        " added_utc TEXT NOT NULL,"
        " added_by TEXT,"
        " source_trust TEXT)")
    store.execute(
        "CREATE TABLE IF NOT EXISTS chunks ("
        " id TEXT PRIMARY KEY,"          # document id + ordinal
        " document_id TEXT NOT NULL,"
        " parent_id TEXT,"               # the chunk this sits inside - S-2
        " depth INTEGER NOT NULL,"       # derived from parent_id
        " ordinal INTEGER NOT NULL,"
        " locator TEXT,"                 # what a citation POINTS AT
        " heading_path TEXT,"            # ancestors' locators - R-66
        " text TEXT NOT NULL,"
        " split_by TEXT NOT NULL,"       # structure | length
        " untrusted INTEGER NOT NULL,"   # always 1 here - R-80
        " FOREIGN KEY (document_id) REFERENCES documents(id))")
    store.execute("CREATE INDEX IF NOT EXISTS chunks_by_document "
                  "ON chunks (document_id, ordinal)")

    # R-83's missing third. Identity is the content hash and lifecycle is the
    # status, but a changed file becomes a DIFFERENT DOCUMENT and nothing
    # joined it to the one it replaced - so "which edition is this clause
    # from?" was answerable and "what did it say before?" was not.
    #
    # ADD COLUMN is the whole migration, the same one heron_search does for
    # `fingerprint` and heron_embed for `kind`.
    try:
        store.db.execute("ALTER TABLE documents ADD COLUMN replaced_by TEXT")
    except sqlite3.OperationalError as exc:
        if "duplicate column" not in str(exc):
            raise
    store.db.commit()


# ---------------------------------------------------------------------------
# R-08 - the tokens a split may never fall inside
# ---------------------------------------------------------------------------

# docs/05 s4 is explicit that this domain is full of exact tokens embeddings
# handle badly, and a chunker splitting on a character count WILL cut one in
# half. The half is worse than useless: it retrieves confidently and is wrong.
#
# Order matters only in that the longest forms are listed first, so a clause
# reference is protected whole rather than as two numbers.
PROTECTED = [
    # QCS 2014 s21.3.2, ISO 19650-2 s5.1  - a standard, its year, its clause
    re.compile(r"[A-Z]{2,}\s+\d{4}(?:-\d+)?\s*(?:§|s)?\s*\d+(?:\.\d+)*"),
    # BuiltInParameter.RBS_DUCT_BOTTOM_ELEVATION
    re.compile(r"\b[A-Za-z][A-Za-z0-9]*(?:\.[A-Za-z][A-Za-z0-9_]*)+\b"),
    # OST_DuctCurves, RBS_DUCT_BOTTOM_ELEVATION
    re.compile(r"\b[A-Za-z][A-Za-z0-9]*_[A-Za-z0-9_]+\b"),
    # a shared-parameter GUID
    re.compile(r"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}"
               r"-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b"),
    # Revit 2024
    re.compile(r"\bRevit\s+\d{4}\b"),
    # a bare clause number, and the section symbol that may precede it
    re.compile(r"§\s*\d+(?:\.\d+)*"),
    re.compile(r"\b\d+(?:\.\d+)+\b"),
]


def protected_spans(text):
    """Every (start, end) a split may not fall strictly inside."""
    spans = []
    for pattern in PROTECTED:
        for m in pattern.finditer(text):
            spans.append((m.start(), m.end()))
    return spans


# ---------------------------------------------------------------------------
# R-68 - the words a split may never fall immediately before
# ---------------------------------------------------------------------------

# THE WORST THING THIS SYSTEM COULD DO, and it is one regular expression.
#
# "Ducts shall be insulated ... except where installed within conditioned
# spaces." Split there, retrieve the first half, and Heron states the opposite
# of the requirement with a citation attached.
# THIS LIST IS NOT EXHAUSTIVE AND CANNOT BE, which is why it is written out
# rather than hidden behind a name that sounds complete. Every form here was
# added because a real clause used it; the review on 2026-09-11 added the
# second line after pointing out that `subject to`, `notwithstanding`,
# `with the exception of` and `excluding` all begin a qualification and none
# of them was here - so a rule could be cut away from its exception and the
# first chunk would state the opposite of the source, with a citation on it.
#
# A structural mechanism that did not need a list would be better. There is
# not one: a qualification is a thing about MEANING, and this module has no
# model and no network (R-47). So the list grows, and the test walks each new
# form across a chunk boundary.
QUALIFIERS = re.compile(
    r"^\W*(except|unless|provided\s+that|save\s+that|however|other\s+than|"
    r"save\s+where|save\s+as|but\s+not|subject\s+to|notwithstanding|"
    r"with\s+the\s+exception\s+of|excluding|save\s+for|apart\s+from|"
    r"other\s+wise\s+than|in\s+no\s+case|only\s+where|only\s+if)\b", re.I)


def starts_a_qualification(text):
    """Whether `text` opens with a word that qualifies what came before it."""
    return bool(QUALIFIERS.match(text or ""))


# ---------------------------------------------------------------------------
# Structure - what a heading looks like, and how deep it is
# ---------------------------------------------------------------------------

_MARKDOWN = re.compile(r"^(#{1,6})\s+(.+?)\s*#*$")
_NUMBERED = re.compile(r"^\s*(\d+(?:\.\d+)*)[.)]?\s+(\S.*)$")
_KEYWORD = re.compile(
    r"^\s*(PART|SECTION|APPENDIX|ANNEX|CHAPTER|SCHEDULE)\s+"
    r"([0-9]+(?:\.[0-9]+)*|[A-Z])\b\s*[-–—:.]?\s*(.*)$", re.I)


class Heading(object):
    """One heading, with the locator a citation will point at."""

    def __init__(self, locator, title, depth, line):
        self.locator = locator
        self.title = title
        self.depth = depth
        self.line = line

    def label(self):
        return ("%s %s" % (self.locator, self.title)).strip()

    def __repr__(self):
        return "<h%d %s>" % (self.depth, self.label())


def read_heading(line):
    """A Heading, or None. Structure first - length is only ever a fallback.

    DEPTH COMES FROM THE DOCUMENT, NOT FROM A GUESS. S-2 settled that
    hierarchy is arbitrary depth stored as a parent link rather than a fixed
    Part/Section/Clause, because QCS, ISO 19650, an Ashghal requirement and a
    company standard all nest differently - and the first document with four
    levels breaks a schema that assumed three.

    So a dotted number carries its own depth: 21.3.2 is three levels down
    because the document says so.
    """
    m = _MARKDOWN.match(line)
    if m:
        return Heading("", m.group(2).strip(), len(m.group(1)), line)

    m = _KEYWORD.match(line)
    if m:
        word = m.group(1).title()
        number = m.group(2)
        locator = "%s %s" % (word, number)
        depth = number.count(".") + 1 if number[:1].isdigit() else 1
        return Heading(locator, m.group(3).strip(), depth, line)

    m = _NUMBERED.match(line)
    if m:
        # A HEADING, OR A PARAGRAPH THAT HAPPENS TO OPEN WITH A NUMBER?
        #
        # "4.1 Ductwork" is a heading. "2024 requirements shall apply to all
        # works." is a sentence, and reading it as a heading would invent a
        # clause 2024 and hang everything after it underneath.
        #
        # So a numbered heading is SHORT and does not end like a sentence.
        # This is a heuristic and it is called one - the first real numbered
        # document is what tests it, which is exactly what --boundaries (R-69)
        # is for: a person reads the boundaries once, and a heading this got
        # wrong is visible there rather than three stages later.
        text = line.strip()
        if len(text) <= 100 and not text.endswith((".", ";", ",")):
            number = m.group(1)
            return Heading(number, m.group(2).strip(),
                           number.count(".") + 1, line)

    return None


# ---------------------------------------------------------------------------
# The chunker
# ---------------------------------------------------------------------------

# A GUARD, NOT A TARGET, and it is said plainly because it is the one number
# in this file that was chosen rather than derived.
#
# Almost every chunk here is decided by STRUCTURE - a clause is a chunk
# because the document says it is one. This exists so that a single
# unnumbered block does not become one chunk the size of a chapter. Every
# chunk it creates records split_by = "length", so the risky ones can be
# listed rather than guessed at.
#
# It is one of the numbers to check when the first real document is read
# (S-4). It is not tuned to make an output look tidier.
MAX_CHARS = 2000


class Chunk(object):
    def __init__(self, text, locator, heading_path, depth, split_by,
                 parent_key=None, key=None, locator_label=None):
        self.text = text
        self.locator = locator
        # The heading as it reads in a path - "21.3 Ductwork". Kept so the
        # heading path can be walked up the PARENT CHAIN rather than rebuilt
        # from whatever the parser's stack happened to hold.
        self.locator_label = locator_label or locator
        self.heading_path = heading_path
        self.depth = depth
        self.split_by = split_by
        self.parent_key = parent_key
        self.key = key

    def indexed_text(self):
        """What retrieval will embed: the heading path, then the chunk. R-66.

        Kept as a method rather than a stored column so the two can never
        drift apart - the path is derived from the hierarchy, and a second
        copy of a derived thing is a cache that goes stale.
        """
        if not self.heading_path:
            return self.text
        return "%s\n%s" % (self.heading_path, self.text)

    def __repr__(self):
        return "<chunk %s d%d %s %d chars>" % (
            self.locator or "-", self.depth, self.split_by, len(self.text))


def _split_points(text):
    """(rank, position) for every place this may be cut. Rank 0 is preferred.

    THE RANK IS RETURNED RATHER THAN BAKED INTO THE ORDER, and that is the
    fix for a real defect. This used to sort by (rank, position) and hand back
    positions only, so EVERY blank line came before EVERY sentence end whatever
    their positions were. The caller walked that list and stopped at the first
    point past its limit - which meant one blank line just beyond the limit
    ended the search before any sentence end INSIDE the limit was looked at,
    and the piece came back oversized. Found by a review 2026-09-11.
    """
    points = []
    for m in re.finditer(r"\n\s*\n", text):
        points.append((0, m.end()))
    for m in re.finditer(r"(?<=[.;:])\s+(?=[A-Z(])", text):
        points.append((1, m.end()))
    points.sort()
    return points


def _cut(text, limit=MAX_CHARS):
    """`text` as pieces, none longer than `limit` unless refusing to cut it.

    REFUSING IS A REAL OUTCOME HERE. A piece stays oversized rather than being
    cut before a qualification (R-68) or through a token (R-08), and the
    caller flags it. R-82 - flagged, never truncated - is the same rule one
    layer up, and for the same reason: a guard that trims is a guard that can
    be padded past.
    """
    # ITERATIVE, AND IT LOOKS AT A WINDOW RATHER THAN AT THE WHOLE REMAINDER.
    #
    # Two versions of this were wrong before this one, and both were found by
    # running it rather than by reading it:
    #
    #   1. RECURSIVE - one level per cut, so a long enough document ended on
    #      RecursionError instead of on a chunk.
    #   2. ITERATIVE BUT WHOLE-TEXT - it re-scanned everything still to come
    #      at every cut, which is quadratic. Measured: 102 KB in 0.25 s,
    #      408 KB in 3.8 s, 1.6 MB in 60 s. A QCS section is that size.
    #
    # Only the next `limit` characters can hold the next split point, so only
    # they are scanned. The window runs a little past the limit so that a
    # token straddling its edge is still seen WHOLE - otherwise the fix for
    # the speed would have broken R-08, which is the trade this margin exists
    # to refuse.
    margin = 400
    pieces = []
    rest = text
    while len(rest) > limit:
        window = rest[:limit + margin]
        spans = protected_spans(window)

        # THE BEST LEGAL POINT WITHIN THE LIMIT, ACROSS EVERY RANK - not the
        # last one seen before the first point that overshoots. A better-ranked
        # point wins; among equals the latest wins, because a longer piece
        # keeps more of the clause together.
        best, best_rank = None, None
        for rank, at in _split_points(window):
            if at <= 0 or at >= len(rest):
                continue
            if at > limit:
                continue                   # too far. Keep looking, never stop.
            if any(start < at < end for start, end in spans):
                continue                   # R-08. Never through a token.
            if starts_a_qualification(rest[at:at + 40]):
                continue                   # R-68. Never before a qualifier.
            if best_rank is None or rank < best_rank or (rank == best_rank
                                                         and at > best):
                best, best_rank = at, rank

        if best is None:
            # NOTHING MAY BE CUT, and that is a real outcome. The piece stays
            # oversized and the caller flags it (R-82) - because a guard that
            # trims is a guard that can be padded past.
            break

        head = rest[:best].rstrip()
        tail = rest[best:].lstrip()
        if not head or not tail:
            break
        pieces.append(head)
        rest = tail

    if rest:
        pieces.append(rest)
    return pieces or [text]


# A title line, for a document that opens with one: short, not a sentence,
# and standing alone. QCS sections, ISO parts and company standards all open
# this way, and the alternative is worse than it sounds.
_TITLE_LINE = re.compile(r"^\s*\S.{0,118}$")


def document_title(text, fallback):
    """The document's own title if it has one, else the filename.

    THE FILENAME IS THE FALLBACK AND NOT THE ANSWER, for the reason
    heron_scope.py already gives when it refuses to name a store after a file:
    "renaming the file loses the knowledge". A citation shows this string to a
    person, and "qcs-sec-21-final-v3-USE-THIS" is not a citation anybody can
    check against a printed standard.

    A first line qualifies when it is short, does not end like a sentence, and
    is followed by a blank line. That is deterministic and it is wrong
    sometimes - which is why --title overrides it, and why the row keeps the
    path as well.
    """
    lines = (text or "").replace("\r\n", "\n").split("\n")
    for i, line in enumerate(lines):
        if not line.strip():
            continue
        if read_heading(line):
            return fallback          # it opens on a heading; that is not a title
        after = lines[i + 1].strip() if i + 1 < len(lines) else ""
        if (_TITLE_LINE.match(line) and not line.rstrip().endswith((".", ";", ":"))
                and not after):
            return line.strip()
        return fallback
    return fallback


def chunk_document(text, title, drop_title_line=False):
    """Text in, chunks out - structure first, length only as a fallback.

    Returns a list of Chunk, in reading order, each knowing its parent by key.
    """
    lines = (text or "").replace("\r\n", "\n").replace("\r", "\n").split("\n")

    if drop_title_line:
        # The line became the document's title, so leaving it in the preamble
        # would store it twice and put it in front of its own heading path.
        for i, line in enumerate(lines):
            if line.strip():
                lines = lines[i + 1:]
                break

    blocks = []                      # (Heading or None, [lines])
    current = (None, [])
    for line in lines:
        heading = read_heading(line)
        if heading:
            blocks.append(current)
            current = (heading, [])
        else:
            current[1].append(line)
    blocks.append(current)

    chunks = []
    stack = []                       # (depth, key, locator, label)
    by_locator = {}                  # locator -> (key, depth)

    for heading, body in blocks:
        body_text = "\n".join(body).strip()

        if heading is None:
            if not body_text:
                continue
            # A PREAMBLE, OR A WHOLE DOCUMENT WITH NO HEADINGS IN IT - which a
            # plain-text file and an unstructured PDF both are.
            #
            # These used to be stored with an EMPTY locator, and the comment
            # here said so: "a chunk with no locator can be retrieved and
            # CANNOT BE CITED - which R-21 calls a bug". It was kept anyway.
            # That is a chunk offered as a citable standards source with
            # nothing in the citation a person could follow back to a place in
            # the file. Found by a review 2026-09-11, which was right that
            # writing the defect down is not the same as not having it.
            #
            # So an unnumbered block gets the only locator its document can
            # honestly give: WHICH BLOCK IT IS. "para-3" is not a clause
            # number and does not pretend to be one - it is a position, and a
            # person can count to it. R-22 asks that a citation resolve to
            # something a human can open; with the path beside it, this does.
            for piece in _cut(body_text):
                chunks.append(Chunk(
                    piece, "para-%d" % (len(chunks) + 1), title, 0,
                    "structure" if len(body_text) <= MAX_CHARS else "length",
                    parent_key=None, key=len(chunks)))
            continue

        # PARENT BY THE DOCUMENT'S OWN NUMBERING FIRST. 21.3.2's parent is
        # 21.3 because the document numbered it that way, which is stronger
        # evidence than where it happens to sit in a stack.
        parent_key = None
        if "." in heading.locator:
            prefix = heading.locator.rsplit(".", 1)[0]
            if prefix in by_locator:
                parent_key = by_locator[prefix][0]

        while stack and stack[-1][0] >= heading.depth:
            stack.pop()
        if parent_key is None and stack:
            parent_key = stack[-1][1]

        # THE PATH FOLLOWS THE CHOSEN PARENT, NOT THE PARSING STACK. When
        # parent_key came from the document's own numbering it could differ from
        # where the stack happened to be - a numbered child after another
        # branch, or a mixed markdown-and-numbered document - and then parent_id
        # pointed at one section while the searchable, cited heading_path named
        # a different one. Two columns describing one tree, quietly disagreeing:
        # the same defect that was fixed for `depth` and left here.
        if parent_key is not None:
            up = []
            walk = parent_key
            while walk is not None:
                up.append(chunks[walk].locator_label)
                walk = chunks[walk].parent_key
            up.reverse()
            ancestors = [title] + up
        else:
            ancestors = [title]
        path = " → ".join([a for a in ancestors if a] + [heading.label()])

        # DEPTH IS DERIVED FROM parent_id, which is what the schema says it
        # is. It used to be the height of the heading stack - which agrees
        # with the parent chain right up until the parent comes from the
        # document's OWN numbering instead of from the stack, and then two
        # columns describing one tree quietly disagree.
        depth = 0 if parent_key is None else chunks[parent_key].depth + 1

        key = len(chunks)
        # A HEADING WITH NO BODY IS ITS OWN LABEL, NEVER AN EMPTY CHUNK.
        #
        # "Section 4 Mechanical Works" often has no prose of its own - it
        # exists to hold 4.1 and 4.2. The row still has to exist, because the
        # children point at it by parent_id and the heading path is built from
        # it. But a stored chunk with empty text is a chunk retrieval can
        # return, and returning nothing while looking like an answer is the
        # failure this whole plan is about. So its text is what it actually
        # is: its own heading.
        pieces = _cut(body_text) if body_text else [heading.label()]
        for i, piece in enumerate(pieces):
            chunks.append(Chunk(
                piece, heading.locator, path, depth,
                "structure" if len(pieces) == 1 else "length",
                parent_key=parent_key if i == 0 else key,
                key=len(chunks), locator_label=heading.label()))

        stack.append((heading.depth, key, heading.locator, heading.label()))
        if heading.locator:
            by_locator[heading.locator] = (key, heading.depth)

    return chunks


# ---------------------------------------------------------------------------
# Readers - one per kind, and every one of them says what it cannot do
# ---------------------------------------------------------------------------

def _read_text(path):
    with open(path, "rb") as handle:
        raw = handle.read()
    for encoding in ("utf-8", "utf-8-sig", "cp1252", "latin-1"):
        try:
            return raw.decode(encoding)
        except UnicodeDecodeError:
            continue
    raise UnreadableDocument(
        "%s is not text in any encoding tried (utf-8, cp1252, latin-1). "
        "It is named rather than skipped, because a reader that skips "
        "quietly is how half a standard goes missing." % path)


def _read_docx(path):
    """A .docx is a zip of XML, so this needs nothing installed.

    Paragraph styles carry the heading level, which is exactly the structure
    R-66 wants - so a Heading 2 becomes "##" and the chunker reads it the same
    way it reads a markdown file.
    """
    import xml.etree.ElementTree as ET
    ns = {"w": "http://schemas.openxmlformats.org/wordprocessingml/2006/main"}
    # THE XML PARSE IS INSIDE THE GUARD, and leaving it outside was a real
    # hole: a file that is a valid ZIP and contains word/document.xml but whose
    # XML is truncated - a half-copied file, a corrupt download - raised
    # ParseError past every named ingestion exception and came out as a
    # traceback, while every other bad document was refused by name. Found by
    # a review 2026-09-11.
    try:
        with zipfile.ZipFile(path) as archive:
            xml = archive.read("word/document.xml")
        root = ET.fromstring(xml)
    except Exception as problem:
        raise UnreadableDocument("%s could not be opened as a .docx: %s"
                                 % (path, problem))
    out = []
    for para in root.iter("{%s}p" % ns["w"]):
        style = ""
        for style_node in para.iter("{%s}pStyle" % ns["w"]):
            style = style_node.get("{%s}val" % ns["w"]) or ""
        text = "".join(node.text or "" for node in para.iter("{%s}t" % ns["w"]))
        m = re.match(r"Heading(\d)", style, re.I)
        if m and text.strip():
            out.append("%s %s" % ("#" * min(6, int(m.group(1))), text.strip()))
        else:
            out.append(text)
    return "\n".join(out)


def _read_pdf(path):
    """PDF text, IF a reader is installed - and a clear sentence if not.

    THE ONE FORMAT THIS MODULE CANNOT DO ALONE. Getting text out of a PDF
    means font encodings and compressed streams; the rest of this file is
    standard library and this part cannot be.

    So it is OPTIONAL and it is LOUD, which is the contract heron_embed.py
    already honours for the trained encoder: absent means a smaller Heron,
    never a broken one, and it says what would fix it. Nothing here is a hard
    stop (D-01).

    S-4 decides the rest with evidence rather than argument: run this on one
    real QCS section and read the output. Clean clause numbers and headings
    mean no further dependency. If it cannot cope, THAT is when Docling is
    taken, knowing exactly why - and not on a guess against a brain/ that
    needs pyyaml and nothing else.
    """
    try:
        import pypdf
    except ImportError:
        raise UnreadableDocument(
            "%s is a PDF, and reading one needs a PDF reader that is not "
            "installed. Everything else here works without it - .md, .txt "
            "and .docx need nothing. To read PDFs:  pip install --user pypdf "
            "(about 1 MB, per-user, no administrator rights). Nothing else in "
            "Heron changes either way." % path)
    try:
        reader = pypdf.PdfReader(path)
        return "\n".join(page.extract_text() or "" for page in reader.pages)
    except Exception as problem:
        raise UnreadableDocument("%s could not be read as a PDF: %s"
                                 % (path, problem))


READERS = {
    ".md": _read_text, ".markdown": _read_text,
    ".txt": _read_text, ".text": _read_text,
    ".docx": _read_docx,
    ".pdf": _read_pdf,
}


# ---------------------------------------------------------------------------
# Ingestion
# ---------------------------------------------------------------------------

class Ingested(object):
    """What one call did, in numbers a caller can print rather than guess."""

    def __init__(self, document_id, title, kind, chunks, reused=False,
                 oversized=None, duplicates=None, remembered=True):
        self.document_id = document_id
        self.title = title
        self.kind = kind
        self.chunks = chunks
        self.reused = reused
        self.oversized = oversized or []
        # R-28, at WRITE time. (locator, other document id, other title).
        self.duplicates = duplicates or []
        # WHETHER THE RECOVERY MANIFEST TOOK THE LINE. False means the store
        # is once again the ONLY registry for this document, so deleting it -
        # which Golden Rule 11 calls a safe action - would lose the document
        # rather than rebuild it. _remember() returns that fact and the first
        # version of this threw it away, reporting a clean ingest. Found by a
        # review 2026-09-11.
        #
        # NOT AN EXCEPTION: the document IS in the store and the chunks ARE
        # searchable, so failing here would be a lie in the other direction.
        # It is a degraded state, and the rule is that it degrades but SAYS SO.
        self.remembered = remembered

    def __repr__(self):
        return "<ingested %s %d chunk(s)%s%s>" % (
            self.document_id[:12], self.chunks,
            " REUSED" if self.reused else "",
            "" if self.remembered else " NOT-IN-MANIFEST")


def file_hash(path):
    """The content hash. The SAME mechanism heron_embed.py already uses.

    R-11, and the instruction that comes with it: copy the mechanism, do not
    invent one. Two hashing schemes in one store is a future afternoon lost to
    "which of these is stale?".

    On CONTENT, never on mtime. mtime means every git operation triggers a
    full re-index (docs/05 s7).
    """
    digest = hashlib.blake2b(digest_size=16)
    with open(path, "rb") as handle:
        while True:
            block = handle.read(1024 * 1024)
            if not block:
                break
            digest.update(block)
    return digest.hexdigest()


def ingest(store, path, added_by=None, source_trust="unknown", title=None,
           status=None):
    """One file into ONE scope. Never a list of scopes (R-05, GR 5).

    `store` is already a single scope, which is how the wall is kept: there is
    no argument here that could take two, so a cross-scope ingest cannot be
    written by accident any more than a cross-scope query can.
    """
    path = os.path.abspath(path)
    extension = os.path.splitext(path)[1].lower()

    # D-26 FIRST, before the file is opened. R-10.
    if extension in REFUSED:
        raise RefusedByExtension(
            "%s is a Revit model file (%s), and a model file never enters the "
            "knowledge store. D-26: the model FILE never leaves the machine, "
            "and it is kept by refusing the extension here rather than by the "
            "user remembering. Its DRAWINGS and SPECIFICATIONS are welcome; "
            "the model is not." % (os.path.basename(path), extension))

    if not os.path.isfile(path):
        raise UnreadableDocument("%s is not a file that exists" % path)

    if extension not in READERS:
        raise UnreadableDocument(
            "%s is a %s, and nothing here reads that. Readable: %s"
            % (os.path.basename(path), extension or "file with no extension",
               ", ".join(sorted(READABLE))))

    # NONE MEANS "THE CALLER DID NOT SAY", which is not the same as DRAFT and
    # the difference only shows on re-ingest: with a plain default of DRAFT,
    # re-ingesting a REVIEWED document to refresh it would silently demote it.
    asked_status = status
    status = status or "DRAFT"
    if status not in STATUSES:
        raise ValueError("status is one of %s, not %r"
                         % (", ".join(STATUSES), status))

    ensure_tables(store)
    document_id = file_hash(path)
    shown = title or os.path.splitext(os.path.basename(path))[0]

    # R-11. An unchanged file costs nothing to re-ingest, so re-indexing stays
    # something a person does freely rather than avoids.
    already = store.execute(
        "SELECT id, path, title, status FROM documents WHERE id = ?",
        (document_id,)).fetchone()
    if already:
        count = store.execute(
            "SELECT COUNT(*) AS n FROM chunks WHERE document_id = ?",
            (document_id,)).fetchone()["n"]
        # WHAT THE CALLER ASKED FOR IS APPLIED, and the first version of this
        # returned before any of it. Re-ingesting a DRAFT file as REVIEWED left
        # the row DRAFT and there was no other operation anywhere that moved a
        # document's lifecycle - so a status could be requested and silently
        # dropped. Found by a review 2026-09-11.
        changes, values = [], []
        if asked_status and asked_status != already["status"]:
            changes.append("status = ?")
            values.append(asked_status)
        if title and title != already["title"]:
            changes.append("title = ?")
            values.append(title)
        moved = already["path"] != path
        if moved:
            # Same bytes, new location. The pointer moves; nothing is re-read.
            changes.append("path = ?")
            values.append(path)
        if changes:
            store.execute("UPDATE documents SET %s WHERE id = ?"
                          % ", ".join(changes), tuple(values) + (document_id,))
            store.db.commit()

        # AND THE MANIFEST HEARS ABOUT IT. Without this line restore() reads
        # the OLD path after the derived store is deleted, reports the source
        # gone, and restores nothing - for a file that is sitting exactly where
        # this call just accepted it. The manifest is append-only (GR 4), so a
        # move is a new line rather than an edit. Found by a review.
        remembered = True
        if changes:
            remembered = _remember(store, "ingested", {
                "path": path, "scope": store.scope,
                "title": title or already["title"],
                "kind": extension.lstrip("."),
                "status": asked_status or already["status"],
                "added_by": added_by, "source_trust": source_trust,
                "document": document_id})
        AUDIT.record("knowledge.ingest", True,
                     fields={"document": document_id, "scope": store.scope,
                             "path": path, "reused": "yes"},
                     numbers={"chunks": count})
        # THE TITLE THE STORE ALREADY HOLDS, not the filename. Re-ingesting
        # printed the file's name while the row held the document's own
        # title, so the same document had two names depending on which run
        # you were reading.
        return Ingested(document_id, title or already["title"] or shown,
                        extension.lstrip("."), count, reused=True,
                        remembered=remembered)

    text = READERS[extension](path)

    # A DOCUMENT THAT YIELDS NOTHING IS REFUSED, NOT ACCEPTED EMPTY. A blank
    # file, or an image-only PDF with no text layer, produced a document row, a
    # commit, and a cheerful "ingested, 0 chunks" - a document that can never be
    # retrieved or cited, and which afterwards reads as merely unindexed.
    #
    # This is the scanned-PDF case, which is the most likely way a real
    # standard fails to come in, so the refusal names it.
    if not (text or "").strip():
        raise UnreadableDocument(
            "%s produced no text at all, so there is nothing to chunk, "
            "retrieve or cite. It is refused rather than stored empty.%s"
            % (os.path.basename(path),
               " A PDF with no text layer is a PICTURE of a document - open it "
               "and try to select a sentence with the mouse. If you cannot, it "
               "needs OCR before Heron can read it."
               if extension == ".pdf" else ""))

    if title is None:
        found = document_title(text, shown)
        chunks = chunk_document(text, found, drop_title_line=(found != shown))
        shown = found
    else:
        chunks = chunk_document(text, shown)

    store.execute(
        "INSERT OR REPLACE INTO documents (id, scope, path, title, kind, "
        "status, added_utc, added_by, source_trust) VALUES (?,?,?,?,?,?,?,?,?)",
        (document_id, store.scope, path, shown, extension.lstrip("."), status,
         datetime.datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ"),
         added_by, source_trust))
    store.execute("DELETE FROM chunks WHERE document_id = ?", (document_id,))

    keys = {}
    oversized = []
    for ordinal, piece in enumerate(chunks):
        chunk_id = "%s:%04d" % (document_id, ordinal)
        keys[piece.key] = chunk_id
        if len(piece.text) > MAX_CHARS:
            oversized.append(chunk_id)
        store.execute(
            "INSERT OR REPLACE INTO chunks (id, document_id, parent_id, depth, "
            "ordinal, locator, heading_path, text, split_by, untrusted) "
            "VALUES (?,?,?,?,?,?,?,?,?,1)",
            (chunk_id, document_id,
             keys.get(piece.parent_key), piece.depth, ordinal,
             piece.locator, piece.heading_path, piece.text, piece.split_by))
    store.db.commit()

    # GR 11. The line that makes these rows derivable again after somebody
    # deletes the store, which is the documented safe-recovery action.
    # THE RESULT IS CARRIED OUT, not dropped. A manifest that could not be
    # written leaves the derived store as the only registry for this document,
    # which is exactly the Golden Rule 11 promise this line exists to keep.
    remembered = _remember(store, "ingested",
                           {"path": path, "scope": store.scope,
                            "title": shown, "kind": extension.lstrip("."),
                            "status": status, "added_by": added_by,
                            "source_trust": source_trust,
                            "document": document_id})

    # GR 14, R-84. Ingestion is an important autonomous operation and was
    # leaving no trace at all. heron_audit.py exists so that a request
    # answered entirely inside the brain still leaves one.
    AUDIT.record("knowledge.ingest", True,
                 fields={"document": document_id, "scope": store.scope,
                         "path": path, "kind": extension.lstrip("."),
                         "status": status, "trust": source_trust},
                 numbers={"chunks": len(chunks), "oversized": len(oversized)})

    return Ingested(document_id, shown, extension.lstrip("."), len(chunks),
                    oversized=oversized, remembered=remembered,
                    duplicates=duplicate_clauses(store, document_id))


# ---------------------------------------------------------------------------
# The manifest - because Golden Rule 11 says the index is DERIVED
# ---------------------------------------------------------------------------

# GOLDEN RULE 11: "The index is derived, never authoritative. Deleting it must
# always be a safe recovery action."
#
# THE DOCUMENT ROWS BROKE THAT, AND IT TOOK A REVIEW TO SEE IT. Fragments are
# derived - delete every store and `heron_scope.py --rebuild` reads
# brain/fragments/ and puts them back. Documents had no such source: the
# `documents` table was the ONLY record of which external files had been
# ingested, into which scope, with which title, status and trust. Delete a
# scope file - the documented safe-recovery action - and all of that was gone
# even though every original file was still sitting on disk untouched.
#
# So the registry lives BESIDE the store and not inside it, as one append-only
# line per event. Append-only because Golden Rule 4 says a record is never
# destroyed, and because an appended line cannot corrupt the ones before it.
#
# It is NOT a copy of the documents. It is the list of what to re-read, which
# is the smallest thing that makes the index derivable again (Q-B: the file is
# pointed at, never copied - so the file is the authority and this is the
# pointer to it).

MANIFEST_SUFFIX = "-documents.jsonl"


def manifest_path(store):
    """Beside the scope's own file, never inside it.

    Derived from the store's path rather than rebuilt, so there is no second
    place that knows how a knowledge path is shaped.
    """
    base = store.path
    if base.endswith(".db"):
        base = base[:-3]
    return base + MANIFEST_SUFFIX


def _remember(store, event, row):
    """Append one line. Never fatal - a trail that can break the operation it
    records is worse than no trail, which is heron_audit's own rule."""
    line = dict(row)
    line["event"] = event
    line["at"] = datetime.datetime.utcnow().strftime("%Y-%m-%dT%H:%M:%SZ")
    try:
        with open(manifest_path(store), "a", encoding="utf-8") as handle:
            handle.write(json.dumps(line, sort_keys=True) + "\n")
        return True
    except (IOError, OSError):
        return False


def manifest(store):
    """Every line, oldest first. [] when there is none - a normal state."""
    path = manifest_path(store)
    if not os.path.isfile(path):
        return []
    out = []
    with open(path, encoding="utf-8") as handle:
        for line in handle:
            line = line.strip()
            if not line:
                continue
            try:
                out.append(json.loads(line))
            except ValueError:
                # One unreadable line must not hide the rest. A manifest is a
                # recovery tool; refusing to read it because of one bad row
                # would make the recovery need a recovery.
                continue
    return out


class Restored(object):
    def __init__(self):
        self.reingested = []
        self.gone = []
        self.skipped = []

    def __repr__(self):
        return "<restored %d, %d gone, %d skipped>" % (
            len(self.reingested), len(self.gone), len(self.skipped))


def restore(store):
    """Rebuild the document rows from the manifest. GR 11 made true.

    Delete a scope file, run this, and every document whose source still
    exists comes back with its scope, title, status and trust. A source that
    has moved is NAMED and not invented - the manifest says what was ingested,
    the file says what it said, and neither is guessed at.

    A document that was explicitly forgotten stays forgotten: the manifest
    records that too, so a restore does not resurrect what somebody removed.
    """
    ensure_tables(store)
    out = Restored()

    latest = {}
    for line in manifest(store):
        path = line.get("path")
        if path:
            latest[path] = line

    for path, line in sorted(latest.items()):
        if line.get("event") == "forgotten":
            out.skipped.append((path, "it was forgotten on purpose"))
            continue
        if not os.path.isfile(path):
            out.gone.append((path, line.get("title") or ""))
            continue
        have = store.execute(
            "SELECT id FROM documents WHERE path = ?", (path,)).fetchone()
        if have:
            out.skipped.append((path, "already in the store"))
            continue
        try:
            # THE TITLE IS PASSED BACK. Without it ingest() re-derives one
            # from the filename or the first line, so a document restored
            # after the safe deletion of a derived store came back under a
            # DIFFERENT NAME - and the name is half of every citation written
            # against it. The manifest recorded the title all along and this
            # call was not reading it. Found by a review 2026-09-11.
            got = ingest(store, path, added_by=line.get("added_by"),
                         source_trust=line.get("source_trust") or "unknown",
                         title=line.get("title") or None,
                         status=line.get("status") or "DRAFT")
        except (RefusedByExtension, UnreadableDocument) as why:
            out.gone.append((path, str(why)))
            continue
        out.reingested.append((path, got.document_id, got.chunks))

    AUDIT.record("knowledge.restore", True,
                 fields={"scope": store.scope},
                 numbers={"reingested": len(out.reingested),
                          "gone": len(out.gone),
                          "skipped": len(out.skipped)})
    return out


def duplicate_clauses(store, document_id):
    """Clauses in this document that are BYTE-IDENTICAL to ones already here.

    R-28, AT WRITE TIME. "The same standard ingested twice under two
    filenames is the normal case, not the exotic one" - and a content hash
    only catches the case where the two FILES are identical. Re-export the
    same PDF, add a cover page, save it from a different tool, and the hash
    differs while the clauses do not.

    NO THRESHOLD, AND THAT IS DELIBERATE. This compares a clause's text for
    exact equality at the same locator. W-8 is the record of what happens
    when a number is invented to decide whether two things are "the same
    enough": measured, it did not separate. Byte-equality needs no number.

    REPORTED, NEVER REFUSED. A project specification that quotes a company
    standard verbatim is not an error - it is Tuesday. What a person needs is
    to be told it happened, so they can decide whether they have two copies
    of one standard or two documents that legitimately agree.
    """
    ensure_tables(store)
    rows = store.execute(
        "SELECT c.locator, c.text FROM chunks c WHERE c.document_id = ? "
        "AND c.locator != ''", (document_id,)).fetchall()
    if not rows:
        return []

    out = []
    for row in rows:
        same = store.execute(
            "SELECT d.id, d.title FROM chunks c "
            "JOIN documents d ON d.id = c.document_id "
            "WHERE c.document_id != ? AND c.locator = ? AND c.text = ? "
            "LIMIT 1", (document_id, row["locator"], row["text"])).fetchone()
        if same:
            out.append((row["locator"], same["id"], same["title"]))
    return out


class Refreshed(object):
    """What one re-index pass did. Numbers a caller prints rather than guesses."""

    def __init__(self):
        self.unchanged = []
        self.changed = []          # (old id, new id, title)
        self.missing = []          # (id, title, path) - the source has moved

    @property
    def touched(self):
        return len(self.changed)

    def __repr__(self):
        return "<refreshed %d unchanged, %d changed, %d missing>" % (
            len(self.unchanged), len(self.changed), len(self.missing))


def refresh(store):
    """Re-ingest every document whose SOURCE FILE has changed. R-27.

    BY CONTENT HASH, NEVER BY mtime, and docs/05 s7 names the reason: a git
    checkout moves every file's mtime and changes none of their content, so
    anything keyed on time re-indexes the whole library for nothing. Keyed on
    content, an unchanged file costs one hash and stops.

    A CHANGED FILE IS A NEW DOCUMENT, AND THE OLD ONE IS RETIRED RATHER THAN
    DELETED. Golden Rule 4: a record is never destroyed. The old row keeps its
    chunks, gets status RETIRED so it is no longer offered as an answer
    (heron_retrieve.OFFERABLE_DOCUMENTS), and names its successor in
    `replaced_by` - which is what makes "what did this clause say before?"
    answerable at all.

    A MISSING SOURCE IS REPORTED AND NOTHING IS DELETED. Q-B: the store points
    at the file and never copies it, so the chunks still hold the text and a
    citation still READS correctly after the file moves - only the convenience
    of opening it breaks. Deleting the knowledge because somebody tidied a
    folder would be the opposite of what pointing at the file was for.

    Returns a Refreshed. INDEXING IS THE CALLER'S: this re-ingests, and
    heron_search.index_chunks / heron_embed.index_chunks rebuild from the rows.
    """
    ensure_tables(store)
    out = Refreshed()

    rows = store.execute(
        "SELECT id, path, title, status, added_by, source_trust "
        "FROM documents WHERE status != 'RETIRED'").fetchall()

    for row in rows:
        if not os.path.isfile(row["path"]):
            out.missing.append((row["id"], row["title"], row["path"]))
            continue

        if file_hash(row["path"]) == row["id"]:
            out.unchanged.append(row["id"])
            continue

        # THE TITLE TRAVELS, exactly as it does through restore(). Without
        # it a document ingested under an explicit name is re-ingested under
        # one derived from its filename or its changed first line - so the
        # correctly named row is RETIRED and its replacement carries a
        # different name, permanently, in every citation written afterwards.
        # The same defect was fixed in restore() one round earlier and left
        # here. Found by a review 2026-09-11.
        fresh = ingest(store, row["path"], added_by=row["added_by"],
                       source_trust=row["source_trust"], status=row["status"],
                       title=row["title"])
        store.execute(
            "UPDATE documents SET status = 'RETIRED', replaced_by = ? "
            "WHERE id = ?", (fresh.document_id, row["id"]))
        store.db.commit()
        out.changed.append((row["id"], fresh.document_id, fresh.title))

    AUDIT.record("knowledge.refresh", True,
                 fields={"scope": store.scope},
                 numbers={"unchanged": len(out.unchanged),
                          "changed": len(out.changed),
                          "missing": len(out.missing)})
    return out


def forget(store, document_id):
    """Remove a document and its chunks. Returns (chunks, remembered).

    GR 11 - the index is DERIVED. Deleting must stay a safe recovery action
    after documents exist, exactly as it already is for fragments. The source
    file is untouched, because the store never held it - it holds a path (Q-B).

    `remembered` IS THE HALF THAT MATTERS AND IT WAS BEING THROWN AWAY. The
    manifest is append-only, so forgetting writes a TOMBSTONE rather than
    erasing the earlier line. If that write fails - an unwritable folder, a
    full disk - the latest durable record of this document is still
    "ingested", and the next restore() brings back the document somebody
    deliberately removed, while this function reports success. Found by a
    review 2026-09-11.
    """
    ensure_tables(store)
    gone = store.execute("DELETE FROM chunks WHERE document_id = ?",
                         (document_id,)).rowcount
    was = store.execute("SELECT path, title FROM documents WHERE id = ?",
                        (document_id,)).fetchone()
    store.execute("DELETE FROM documents WHERE id = ?", (document_id,))
    store.db.commit()
    remembered = True
    if was:
        # So a restore does not bring back what somebody deliberately removed.
        remembered = _remember(store, "forgotten",
                               {"path": was["path"], "scope": store.scope,
                                "title": was["title"],
                                "document": document_id})
    AUDIT.record("knowledge.forget", remembered,
                 fields={"document": document_id, "scope": store.scope},
                 numbers={"chunks": gone})
    return gone, remembered


def boundaries(store, document_id):
    """Every chunk boundary in one document, for a person to read once. R-69.

    D-30 APPLIED ONE LAYER EARLIER: the machine gathers, a person signs. A
    badly chunked standard does not fail - it answers, confidently, forever,
    and every later stage inherits the mistake in silence. No UI is needed;
    printing the boundaries once IS the requirement.
    """
    ensure_tables(store)
    rows = store.execute(
        "SELECT id, parent_id, depth, ordinal, locator, heading_path, text, "
        "split_by FROM chunks WHERE document_id = ? ORDER BY ordinal",
        (document_id,)).fetchall()
    return [dict(r) for r in rows]


# ---------------------------------------------------------------------------
# The command
# ---------------------------------------------------------------------------

class MissingFlagValue(Exception):
    """A flag that takes a value was given none. Refused, never defaulted."""


def _flag(argv, name, default=None):
    """The value after `name`, or `default` when the flag is absent.

    A FLAG PRESENT WITH NO VALUE IS AN ERROR, NOT THE DEFAULT. `--scope` at the
    end of a command returned None and the caller substituted "global" - so a
    mistyped command meant for company or project knowledge ingested the
    document into the GLOBALLY SHARED scope and said nothing about it. That is
    Golden Rule 5 broken by a typo, which is the class of mistake the scope wall
    exists to make impossible.
    """
    if name in argv:
        i = argv.index(name)
        value = argv[i + 1] if i + 1 < len(argv) else None
        if value is None or value.startswith("--"):
            raise MissingFlagValue(
                "%s needs a value and was given none. Nothing was ingested - "
                "a missing scope is NOT the global scope." % name)
        del argv[i:i + 2]
        return value
    return default


def main(argv):
    """The command. A flag error is reported and refused, never defaulted."""
    try:
        return _main(list(argv))
    except MissingFlagValue as why:
        print("  %s" % why)
        return 2


def _main(argv):
    # LOWER, not upper. heron_scope.SCOPES are lowercase strings, and the
    # first version of this line upper-cased them - so every CLI call
    # died on "'GLOBAL' is not a knowledge scope" while the whole test
    # suite passed, because the suite called ingest() and never the
    # command. Found by running it. A test now calls main() too.
    scope = (_flag(argv, "--scope", SCOPE.GLOBAL) or SCOPE.GLOBAL).lower()
    project = _flag(argv, "--project")

    # A PROJECT KEY WITHOUT THE PROJECT SCOPE IS REFUSED, NOT QUIETLY IGNORED.
    #
    # `--project Tower` with no `--scope project` left scope at its default -
    # global - and still handed the key to open_scope(). So a command that
    # named a project put the document in the SHARED database and overwrote
    # that database's project metadata on the way. One forgotten flag, and
    # project knowledge is visible to every project: Golden Rule 5 undone by a
    # default. Found by a review 2026-09-11.
    #
    # Refused rather than corrected, because the two readings - "I meant the
    # project scope" and "I pasted the wrong flag" - want different answers
    # and only the person typing knows which.
    if project and scope != SCOPE.PROJECT:
        print("  --project names a project and the scope is '%s'." % scope)
        print("  A project key belongs to the PROJECT scope and nowhere else -")
        print("  putting it anywhere else would ingest project knowledge into a")
        print("  shared store (Golden Rule 5). Say which you meant:")
        print("    ... --scope project --project %s" % project)
        print("    ... --scope %s            (and drop --project)" % scope)
        return 2
    trust = _flag(argv, "--trust", "unknown")
    show = _flag(argv, "--boundaries")
    do_refresh = "--refresh" in argv
    if do_refresh:
        argv.remove("--refresh")

    if not argv and not show and not do_refresh:
        print('  python brain/heron_ingest.py <file> --scope global')
        print('  python brain/heron_ingest.py --boundaries <document id>')
        print('  python brain/heron_ingest.py --refresh')
        print("  a document is READ where it is. It is never moved, never")
        print("  changed, and never copied into Heron's folder.")
        return 2

    store = SCOPE.open_scope(scope, project)
    try:
        # THE SCHEMA EXISTS BEFORE ANYTHING IS COUNTED. On a fresh scope where
        # every path was refused, ingest() never ran, the tables were never
        # created, and the summary count below died on "no such table: chunks"
        # AFTER the refusals had printed - a crash where a refusal was the
        # correct and complete answer.
        ensure_tables(store)

        if show:
            rows = boundaries(store, show)
            if not rows:
                print("  no document %s in the %s scope." % (show, scope))
                print("  the store holds what was ingested, and this is not in it.")
                return 2
            print("Document %s - %d chunk(s). Read the boundaries, not the count."
                  % (show[:12], len(rows)))
            for row in rows:
                head = (row["text"] or "").strip().replace("\n", " ")
                print("  %-6s d%-2d %-12s %-9s %s"
                      % (row["ordinal"], row["depth"], row["locator"] or "-",
                         row["split_by"], head[:60] + ("..." if len(head) > 60 else "")))
            risky = [r for r in rows if r["split_by"] == "length"]
            print()
            print("  %d of %d were split by LENGTH rather than by structure."
                  % (len(risky), len(rows)))
            print("  Those are the ones to read. A clause that ends mid-sentence,")
            print("  or a rule parted from its exception, is what you are looking")
            print("  for - and it will not announce itself later.")
            return 0

        if do_refresh:
            done = refresh(store)
            print("Refreshed the %s scope." % scope)
            print("  unchanged  %d - a file whose content has not changed "
                  "costs one hash" % len(done.unchanged))
            for old_id, new_id, title in done.changed:
                print("  CHANGED    %s" % title)
                print("             %s is RETIRED and names %s as its "
                      "replacement" % (old_id[:12], new_id[:12]))
            for doc_id, title, path in done.missing:
                # Q-B. The store points at the file; it never held it.
                print("  MOVED      %s" % title)
                print("             the source file is no longer at %s" % path)
                print("             NOTHING WAS DELETED - the clauses and "
                      "their citations still read correctly. Only opening "
                      "the original is broken")
            if not done.changed and not done.missing:
                print("  nothing to do. Re-indexing on an unchanged library "
                      "is free, which is why it may be run at any time.")
            return 0

        refused = 0
        ingested_any = False
        for path in argv:
            try:
                got = ingest(store, path, source_trust=trust)
                ingested_any = True
            except (RefusedByExtension, UnreadableDocument) as why:
                print("  REFUSED  %s" % why)
                refused += 1
                continue
            print("%s  %s" % ("reused " if got.reused else "ingested", got.title))
            print("  id       %s" % got.document_id)
            print("  chunks   %d" % got.chunks)
            if not got.remembered:
                # DEGRADED, AND SAYING SO. The document is in and searchable;
                # what failed is the append-only manifest beside the store,
                # which is the line that makes these rows rebuildable after
                # somebody deletes the derived file. Without it the store is
                # again the only registry, and Golden Rule 11's safe recovery
                # action would lose this document instead of restoring it.
                print("  NOT RECORDED  the recovery manifest could not be "
                      "written:")
                print("             %s" % manifest_path(store))
                print("             The document IS ingested and searchable. "
                      "But deleting this")
                print("             scope's store would now LOSE it rather "
                      "than rebuild it.")
                print("             Check the folder is writable, then "
                      "re-ingest.")
            if got.duplicates:
                # R-28. REPORTED, never refused - a project spec quoting a
                # company standard verbatim is Tuesday, not an error.
                print("  DUPLICATE  %d clause(s) are byte-identical to "
                      "clauses already in this scope:" % len(got.duplicates))
                for locator, _other_id, other_title in got.duplicates[:5]:
                    print("             %-10s also in %s" % (locator,
                                                             other_title))
                if len(got.duplicates) > 5:
                    print("             ... and %d more"
                          % (len(got.duplicates) - 5))
                print("             Two copies of one standard, or two "
                      "documents that legitimately agree? That is yours to "
                      "say - nothing was refused")
            if got.oversized:
                # R-82. FLAGGED, never truncated.
                print("  FLAGGED  %d chunk(s) are over %d characters and were "
                      "NOT trimmed:" % (len(got.oversized), MAX_CHARS))
                for chunk_id in got.oversized:
                    print("             %s" % chunk_id)
                print("           trimming would let anything be padded past a")
                print("           reader's window, so they are reported whole.")

        total = store.execute("SELECT COUNT(*) AS n FROM chunks").fetchone()["n"]
        print()
        print("  %d chunk(s) in the %s scope." % (total, scope))
        # R-70. Said while the number is small, so the day it approaches the
        # ceiling is a day somebody NOTICES rather than a day search gets slow.
        print("  D-23's sqlite-vec brute-force search stays fast below roughly")
        print("  500,000 vectors. Nothing here is close to it.")
        if refused and not ingested_any:
            print("  NOTHING was ingested: every file given was refused. That "
                  "is a complete answer, and the exit code says so.")
        print("  Your file was not moved, not changed and not copied.")
        return 2 if (refused and not ingested_any) else 0
    finally:
        store.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
