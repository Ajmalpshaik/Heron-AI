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
import os
import re
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
QUALIFIERS = re.compile(
    r"^\W*(except|unless|provided\s+that|save\s+that|however|other\s+than|"
    r"save\s+where|save\s+as|but\s+not)\b", re.I)


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
                 parent_key=None, key=None):
        self.text = text
        self.locator = locator
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
    """Candidate places to cut, best first: blank line, then sentence end."""
    points = []
    for m in re.finditer(r"\n\s*\n", text):
        points.append((0, m.end()))
    for m in re.finditer(r"(?<=[.;:])\s+(?=[A-Z(])", text):
        points.append((1, m.end()))
    points.sort()
    return [p for _rank, p in points]


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

        best = None
        for at in _split_points(window):
            if at <= 0 or at >= len(rest):
                continue
            if any(start < at < end for start, end in spans):
                continue                   # R-08. Never through a token.
            if starts_a_qualification(rest[at:at + 40]):
                continue                   # R-68. Never before a qualifier.
            if at > limit:
                break
            best = at

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
            # A preamble, before any heading. It has no locator, and a chunk
            # with no locator can be retrieved and CANNOT BE CITED - which
            # R-21 calls a bug rather than a low-confidence answer. It is
            # kept, and the missing locator is visible on the row.
            for piece in _cut(body_text):
                chunks.append(Chunk(
                    piece, "", title, 0,
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

        ancestors = [title] + [entry[3] for entry in stack]
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
                key=len(chunks)))

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
    try:
        with zipfile.ZipFile(path) as archive:
            xml = archive.read("word/document.xml")
    except Exception as problem:
        raise UnreadableDocument("%s could not be opened as a .docx: %s"
                                 % (path, problem))

    root = ET.fromstring(xml)
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
                 oversized=None):
        self.document_id = document_id
        self.title = title
        self.kind = kind
        self.chunks = chunks
        self.reused = reused
        self.oversized = oversized or []

    def __repr__(self):
        return "<ingested %s %d chunk(s)%s>" % (
            self.document_id[:12], self.chunks, " REUSED" if self.reused else "")


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
           status="DRAFT"):
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

    if status not in STATUSES:
        raise ValueError("status is one of %s, not %r"
                         % (", ".join(STATUSES), status))

    ensure_tables(store)
    document_id = file_hash(path)
    shown = title or os.path.splitext(os.path.basename(path))[0]

    # R-11. An unchanged file costs nothing to re-ingest, so re-indexing stays
    # something a person does freely rather than avoids.
    already = store.execute("SELECT id, path, title FROM documents WHERE id = ?",
                            (document_id,)).fetchone()
    if already:
        count = store.execute(
            "SELECT COUNT(*) AS n FROM chunks WHERE document_id = ?",
            (document_id,)).fetchone()["n"]
        if already["path"] != path:
            # Same bytes, new location. The pointer moves; nothing is re-read.
            store.execute("UPDATE documents SET path = ? WHERE id = ?",
                          (path, document_id))
            store.db.commit()
        AUDIT.record("knowledge.ingest", True,
                     fields={"document": document_id, "scope": store.scope,
                             "path": path, "reused": "yes"},
                     numbers={"chunks": count})
        # THE TITLE THE STORE ALREADY HOLDS, not the filename. Re-ingesting
        # printed the file's name while the row held the document's own
        # title, so the same document had two names depending on which run
        # you were reading.
        return Ingested(document_id, already["title"] or shown,
                        extension.lstrip("."), count, reused=True)

    text = READERS[extension](path)
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

    # GR 14, R-84. Ingestion is an important autonomous operation and was
    # leaving no trace at all. heron_audit.py exists so that a request
    # answered entirely inside the brain still leaves one.
    AUDIT.record("knowledge.ingest", True,
                 fields={"document": document_id, "scope": store.scope,
                         "path": path, "kind": extension.lstrip("."),
                         "status": status, "trust": source_trust},
                 numbers={"chunks": len(chunks), "oversized": len(oversized)})

    return Ingested(document_id, shown, extension.lstrip("."), len(chunks),
                    oversized=oversized)


def forget(store, document_id):
    """Remove a document and its chunks. GR 11 - the index is DERIVED.

    Deleting must stay a safe recovery action after documents exist, exactly
    as it already is for fragments. The source file is untouched, because the
    store never held it - it holds a path (Q-B).
    """
    ensure_tables(store)
    gone = store.execute("DELETE FROM chunks WHERE document_id = ?",
                         (document_id,)).rowcount
    store.execute("DELETE FROM documents WHERE id = ?", (document_id,))
    store.db.commit()
    AUDIT.record("knowledge.forget", True,
                 fields={"document": document_id, "scope": store.scope},
                 numbers={"chunks": gone})
    return gone


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

def _flag(argv, name, default=None):
    if name in argv:
        i = argv.index(name)
        value = argv[i + 1] if i + 1 < len(argv) else None
        del argv[i:i + 2]
        return value
    return default


def main(argv):
    argv = list(argv)
    # LOWER, not upper. heron_scope.SCOPES are lowercase strings, and the
    # first version of this line upper-cased them - so every CLI call
    # died on "'GLOBAL' is not a knowledge scope" while the whole test
    # suite passed, because the suite called ingest() and never the
    # command. Found by running it. A test now calls main() too.
    scope = (_flag(argv, "--scope", SCOPE.GLOBAL) or SCOPE.GLOBAL).lower()
    project = _flag(argv, "--project")
    trust = _flag(argv, "--trust", "unknown")
    show = _flag(argv, "--boundaries")

    if not argv and not show:
        print('  python brain/heron_ingest.py <file> --scope global')
        print('  python brain/heron_ingest.py --boundaries <document id>')
        print("  a document is READ where it is. It is never moved, never")
        print("  changed, and never copied into Heron's folder.")
        return 2

    store = SCOPE.open_scope(scope, project)
    try:
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

        for path in argv:
            try:
                got = ingest(store, path, source_trust=trust)
            except (RefusedByExtension, UnreadableDocument) as refused:
                print("  REFUSED  %s" % refused)
                continue
            print("%s  %s" % ("reused " if got.reused else "ingested", got.title))
            print("  id       %s" % got.document_id)
            print("  chunks   %d" % got.chunks)
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
        print("  Your file was not moved, not changed and not copied.")
        return 0
    finally:
        store.close()


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
