# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-IMP-CLS-003
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Content classification - it asks once per KIND of file, not once per file.

    python brain/heron_classify.py

WHAT IT IS FOR (docs/28, HERON-IMP-CLS-003)
--------------------------------------------
"Code, documentation, config, metadata, asset." T2, risk READ. Step 10
of docs/00 s28's sixteen, and the first one in the import pipeline that
needs a model.

IT CLASSIFIES NOTHING ITSELF, AND THE ROW'S TIER IS WHY
---------------------------------------------------------
HERON-IMP-FIL-002 walks the folder and reports the EXTENSION, refusing
to name a category, because if a suffix could sort files into those five
this row would not be T2. `.txt` holds C# often enough to matter, `.md`
holds a licence as often as a manual, and `report.pdf.exe` is neither.

So this agent does the deterministic half - gather the evidence, group
the work, refuse what cannot be answered - and hands the host one
question per group. Deciding is the host's act, which is D-01
everywhere else in this project too.

THE COST PROBLEM IS THE DESIGN PROBLEM, AND IT IS WRITTEN DOWN
----------------------------------------------------------------
docs/19 s96, on the hybrid routing option:

    "Batch classification of 20,000 fragments should not run on a
     frontier model."

An `AJ-Tools` clone is tens of thousands of files. One question per file
is the version of this agent that nobody can afford to run, and it is
also the version that asks the same question about four thousand `.py`
files four thousand times.

So the unit of work is the GROUP, not the file: every file sharing an
extension AND a shape - text or binary, parses as JSON or does not - is
one question. Sixty-one thousand files in a real repository come back as
a handful of questions, and the answer count is reported so nobody has
to guess what a run will cost.

WHAT IS EVIDENCE AND WHAT WOULD BE A GUESS
--------------------------------------------
Everything gathered here is a fact about the bytes:

    text or binary      a NUL in the first 8 KB. Decidable.
    parses as JSON      json.loads succeeded. Decidable.
    starts with `<`     it is markup of some kind. Decidable.
    Heron's own header  the five fields docs/29 requires. Decidable.

None of them IS a category. A binary file might be an asset or a
compiled library; JSON is usually config and is sometimes data. The
evidence narrows the question and never answers it, and the difference
is the whole reason this file exists rather than a lookup table.

NOTHING IS CLASSIFIED WITHOUT AN ANSWER, AND AN ANSWER IS CHECKED
-------------------------------------------------------------------
`accept` takes the host's answers back and refuses anything outside the
five categories - a category invented in the reply is not a category.
A group with no answer stays unclassified and is named, never dropped
(Golden Rule 14).
"""

from __future__ import annotations

import io
import json
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_walk as WALK  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/28's own five words, quoted rather than expanded. Adding a sixth is
# a change to the register, not a change to this file.
CATEGORIES = ("code", "documentation", "config", "metadata", "asset")

# How much of a file is read to decide whether it is text. Enough to find a
# NUL in anything that has one near the front, small enough that a folder of
# large binaries costs nothing.
SNIFF = 8192

# The five fields docs/29 requires on everything Heron creates. Their
# presence is a fact about the bytes; what it MEANS about the file is not.
HERON_HEADER = "Heron-Agent"

# The extension list is HERON-IMP-FIL-002's job, and its answer is what this
# agent reads. Bound so the two cannot disagree about what a suffix is.
extension_of = WALK.extension_of


def _shape(path):
    """What the first bytes say. Facts only - no category is named."""
    card = {"text": None, "json": False, "markup": False, "heron": False,
            "read": False}
    try:
        with io.open(path, "rb") as handle:
            head = handle.read(SNIFF)
    except (IOError, OSError):
        return card

    card["read"] = True
    card["text"] = b"\x00" not in head
    if not card["text"]:
        return card

    try:
        body = head.decode("utf-8")
    except UnicodeDecodeError:
        # Readable bytes that are not UTF-8. Still text by the NUL rule, and
        # saying which encoding would be a guess, so the rest is left alone.
        return card

    card["markup"] = body.lstrip().startswith("<")
    card["heron"] = HERON_HEADER in body

    # THE CLAIM STAYS "IT PARSES", AND THE COST STOPS BEING UNBOUNDED.
    #
    # This used to call json.load on the whole file for EVERY text file, and
    # json.load reads all of it before it looks at the first character.
    # MEASURED 2026-09-21: 120 MB of plain prose was read in full and held
    # in memory - a peak of 240 MB, and 0.12s warm - only to be rejected on
    # its FIRST character. The sniff above is bounded because "a folder of
    # large binaries costs nothing"; the bound protected binaries and not
    # text, which is the half this agent actually walks. On an imported
    # repository, which is this agent's whole job, a log or a SQL dump that
    # size is ordinary. Row 5b-88.
    #
    # Two steps, and NEITHER changes a single answer:
    #
    #   1  a file that fits inside the sniff is ALREADY read, so it is
    #      parsed from memory. Same bytes, same verdict, no second open.
    #   2  a larger one is rejected on the head when the head cannot begin
    #      a JSON value at all. That test is SOUND rather than a heuristic:
    #      RFC 8259 says a JSON text is one value, and every value starts
    #      with {, [, ", -, a digit, or the exact words true/false/null.
    #      Anything else cannot parse, whatever the other 120 MB hold.
    #
    # Only a large file that really does start like JSON is read in full,
    # and that is the one case where reading it is the only way to know.
    # tests/test_classify.py s8 counts the opens rather than trusting this.
    card["json"] = _parses_as_json(path, body)
    return card


# Every character RFC 8259 allows a JSON value to begin with. The three
# literals are matched whole: a file starting `t` is only JSON if the word
# is `true`, and that is the difference between rejecting 120 MB of prose
# on its head and reading all of it first.
JSON_STARTS = "{[\"-0123456789"
JSON_WORDS = ("true", "false", "null")


def _parses_as_json(path, body):
    """
    Whether this file parses as JSON, without reading more than it must.

    The answer is identical to json.load on the whole file. What changes is
    how much is read to get it - see the note in _shape().
    """
    lead = body.lstrip()
    if not lead:
        return False
    if lead[0] not in JSON_STARTS and not lead.startswith(JSON_WORDS):
        return False

    # Small enough that the sniff already holds all of it: parse what is in
    # hand rather than opening the file a second time.
    try:
        if os.path.getsize(path) <= SNIFF:
            json.loads(body)
            return True
    except ValueError:
        return False
    except (IOError, OSError):
        return False

    try:
        with io.open(path, encoding="utf-8") as handle:
            json.load(handle)
        return True
    except (ValueError, IOError, OSError, UnicodeDecodeError):
        return False


def _key(card, shape):
    """What makes two files one question: the suffix and the shape."""
    return (card["extension"],
            "binary" if shape["text"] is False else "text",
            "json" if shape["json"] else
            "markup" if shape["markup"] else "plain")


def brief(walked, root=None):
    """
    {briefed, groups, asks, files} - or a refusal. Nothing is classified.
    """
    if not walked:
        return {"briefed": False, "refused": "NOTHING_TO_CLASSIFY",
                "why": "no walk was handed in. HERON-IMP-FIL-002 produces "
                       "one."}

    walked = getattr(walked, "data", walked)
    if not isinstance(walked, dict) or not walked.get("walked") \
            or not isinstance(walked.get("files"), list):
        return {"briefed": False, "refused": "NOT_A_WALK",
                "why": "%r is not a walk. HERON-IMP-FIL-002 returns "
                       "{walked, root, files, ...} and a refusal carries no "
                       "files." % (walked,)}

    files = walked["files"]
    if not files:
        return {"briefed": False, "refused": "NOTHING_TO_CLASSIFY",
                "why": "the walk found no files to carry forward. Every "
                       "entry was a Revit model, a link or unreadable - "
                       "there is nothing here to classify."}

    where = root if root is not None else walked.get("root")
    groups, unreadable = {}, []
    for card in files:
        path = os.path.join(where, card["at"]) if where else card["at"]
        shape = _shape(path)
        if not shape["read"]:
            unreadable.append({"at": card["at"],
                               "why": "it was listed by the walk and could "
                                      "not be opened here"})
            continue
        key = _key(card, shape)
        group = groups.setdefault(key, {
            "extension": key[0], "shape": key[1], "content": key[2],
            "files": [], "bytes": 0, "heron": 0})
        group["files"].append(card["at"])
        group["bytes"] += int(card.get("bytes") or 0)
        if shape["heron"]:
            group["heron"] += 1

    out, asks = [], []
    for key in sorted(groups):
        group = groups[key]
        evidence = []
        evidence.append("%d file%s, %d bytes"
                        % (len(group["files"]),
                           "" if len(group["files"]) == 1 else "s",
                           group["bytes"]))
        evidence.append("the name says %s"
                        % (group["extension"] or "nothing - no extension"))
        evidence.append("the bytes are %s" % group["shape"])
        if group["content"] == "json":
            evidence.append("it parses as JSON")
        elif group["content"] == "markup":
            evidence.append("it starts with `<`, so it is markup of some kind")
        if group["heron"]:
            evidence.append("%d of them %s Heron's own %s header"
                            % (group["heron"],
                               "carries" if group["heron"] == 1 else "carry",
                               HERON_HEADER))

        # The empty extension is rendered so a reader can see it. It is
        # still the group's key, so an answer comes back against the same
        # string that was asked about.
        card = {"group": "%s/%s/%s" % ((key[0] or "(no extension)",) + key[1:]),
                "extension": group["extension"],
                "shape": group["shape"], "content": group["content"],
                "of": len(group["files"]), "bytes": group["bytes"],
                "examples": sorted(group["files"])[:5],
                "evidence": evidence}
        out.append(card)
        asks.append({
            "group": card["group"],
            "question": "which of %s are these? %s. Examples: %s."
                        % (", ".join(CATEGORIES), "; ".join(evidence),
                           ", ".join(card["examples"])),
            "categories": list(CATEGORIES),
            "of": len(group["files"])})

    return {
        "briefed": True,
        "of": len(files),
        "groups": out,
        "asks": asks,
        "unreadable": unreadable,
        "classified": [],
        "why": "%d file%s in %d group%s: %d question%s to ask, not %d."
               % (len(files), "" if len(files) == 1 else "s", len(out),
                  "" if len(out) == 1 else "s", len(asks),
                  "" if len(asks) == 1 else "s", len(files)),
        "unjudged": [
            "NOTHING WAS CLASSIFIED. The evidence gathered here is facts "
            "about the bytes - a NUL, a successful json.loads, a leading "
            "`<`. None of them IS a category: a binary might be an asset or "
            "a compiled library, and JSON is usually config and sometimes "
            "data. docs/28 makes this row T2 for that reason.",
            "IT ASKS ONCE PER KIND, NOT ONCE PER FILE. %d questions for %d "
            "files. docs/19 s96 says batch classification should not run on "
            "a frontier model, and asking the same question about four "
            "thousand .py files four thousand times is the version of this "
            "agent nobody can afford to run."
            % (len(asks), len(files)),
            ("%d file%s the walk listed and this agent could not open, named "
             "rather than dropped (Golden Rule 14)."
             % (len(unreadable), "" if len(unreadable) == 1 else "s")
             if unreadable else
             "every file the walk listed could be opened and read."),
            "THE FIVE CATEGORIES ARE docs/28's OWN WORDS, quoted. Adding a "
            "sixth is a change to the register, and `accept` refuses one "
            "invented in a reply.",
            "WHETHER A GROUP IS ONE THING. Files sharing a suffix and a "
            "shape are asked about together, which is what makes this "
            "affordable - and a folder where one .txt is a licence and "
            "another is a parameter dump will get one answer for both. The "
            "examples travel with the question so that is visible.",
        ],
    }


def accept(briefed, answers):
    """
    {classified, unclassified} - or a refusal. An invented category is
    refused, and a group nobody answered is named rather than dropped.
    """
    if not briefed or not briefed.get("briefed"):
        return {"accepted": False, "refused": "NOTHING_TO_CLASSIFY",
                "why": "there is no brief to answer. Hand in one that "
                       "briefed something."}

    answers = answers or {}
    if not isinstance(answers, dict):
        return {"accepted": False, "refused": "NOT_AN_ANSWER",
                "why": "%r is not a set of answers. It maps a group to one "
                       "of %s." % (answers, ", ".join(CATEGORIES))}

    classified, missing = [], []
    for card in briefed["groups"]:
        said = answers.get(card["group"])
        if said is None:
            missing.append(dict(card,
                                why="no answer came back for this group, so "
                                    "it stays unclassified"))
            continue
        said = str(said).strip().lower()
        if said not in CATEGORIES:
            return {"accepted": False, "refused": "NOT_A_CATEGORY",
                    "why": "%r is not one of %s. A category invented in the "
                           "reply is not a category - docs/28 names five."
                           % (said, ", ".join(CATEGORIES))}
        classified.append(dict(card, category=said))

    return {
        "accepted": True,
        "of": len(briefed["groups"]),
        "classified": classified,
        "unclassified": missing,
        "files": sum(card["of"] for card in classified),
        "why": "%d of %d group%s answered, covering %d file%s. %d "
               "unclassified."
               % (len(classified), len(briefed["groups"]),
                  "" if len(briefed["groups"]) == 1 else "s",
                  sum(card["of"] for card in classified),
                  "" if sum(card["of"] for card in classified) == 1 else "s",
                  len(missing)),
        "unjudged": [
            "THE CATEGORIES CAME FROM THE ANSWER, NOT FROM HERE. Every one "
            "was checked against docs/28's five and nothing else was "
            "accepted.",
            ("%d group%s went unanswered and %s named rather than dropped "
             "(Golden Rule 14). An unclassified group is not an empty one."
             % (len(missing), "" if len(missing) == 1 else "s",
                "is" if len(missing) == 1 else "are")
             if missing else
             "every group came back with a category."),
            "WHETHER THE ANSWER IS RIGHT. This agent checked that each one "
            "is a category Heron knows, not that a .json full of duct sizes "
            "is really config rather than data. Nothing here read the "
            "answer against the files again.",
        ],
    }


def main(argv):
    import shutil
    import tempfile

    print("CONTENT CLASSIFICATION   one question per kind, not per file")
    print("=" * 72)
    print("\ncategories (docs/28's own five): %s" % ", ".join(CATEGORIES))

    where = argv[0] if argv else None
    made = None
    if not where:
        made = where = tempfile.mkdtemp(prefix="heron-classify-")
        os.makedirs(os.path.join(where, "tools"))
        for at, text in (
                ("README.md", "# AJ-Tools\nWhat this is."),
                ("LICENCE", "MIT"),
                ("settings.json", '{"revit": "2024"}'),
                ("app.config", "<configuration></configuration>"),
                (os.path.join("tools", "CountDucts.py"), "# code\npass\n"),
                (os.path.join("tools", "TagSheet.py"), "# code\npass\n"),
                (os.path.join("tools", "Filter.py"),
                 "# Heron-Agent:  HERON-X\npass\n")):
            with io.open(os.path.join(where, at), "w",
                         encoding="utf-8") as handle:
                handle.write(text)
        with io.open(os.path.join(where, "logo.png"), "wb") as handle:
            handle.write(b"\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR")

    try:
        walked = WALK.walk(where)
        answer = brief(walked)
        print("\n%s" % answer["why"])
        for card in answer["groups"]:
            print("\n  GROUP  %-22s %d file(s)" % (card["group"], card["of"]))
            for line in card["evidence"]:
                print("         %s" % line)

        print("\nwhat the host is asked")
        for ask in answer["asks"]:
            print("  %-22s %s" % (ask["group"], ask["question"][:60]))

        said = dict((card["group"],
                     "code" if card["extension"] == ".py" else
                     "documentation" if card["extension"] == ".md" else
                     "config" if card["content"] in ("json", "markup") else
                     "asset" if card["shape"] == "binary" else None)
                    for card in answer["groups"])
        said = dict((k, v) for k, v in said.items() if v)
        back = accept(answer, said)
        print("\n%s" % back["why"])
        for card in back["classified"]:
            print("  %-14s %-22s %d file(s)"
                  % (card["category"], card["group"], card["of"]))
        for card in back["unclassified"]:
            print("  %-14s %-22s %s" % ("(none)", card["group"],
                                        card["why"][:36]))

        print("\nrefused")
        for bad in (None, {"walked": False}, {"walked": True, "files": []}):
            said = brief(bad)
            print("  %-22s %s" % (said["refused"], said["why"][:42]))
        for bad in (None, {"briefed": False}):
            said = accept(bad, {})
            print("  %-22s %s" % (said["refused"], said["why"][:42]))
        said = accept(answer, "a string")
        print("  %-22s %s" % (said["refused"], said["why"][:42]))
        said = accept(answer,
                      dict((card["group"], "spreadsheet")
                           for card in answer["groups"]))
        print("  %-22s %s" % (said["refused"], said["why"][:42]))

        print("\nwhat this agent does not judge")
        for line in answer["unjudged"]:
            print("  - %s" % line)
    finally:
        if made:
            shutil.rmtree(made, ignore_errors=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
