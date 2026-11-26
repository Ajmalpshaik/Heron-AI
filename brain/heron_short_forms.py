# Heron-Agent:  HERON-RAG-RET-003
# Heron-Step:   11
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Short forms - a modeller's own list, read before the search runs.

    python brain/heron_short_forms.py                 where the list is, and what it holds
    python brain/heron_short_forms.py "isolate the VCDs"   what the search would be handed

WHY THIS EXISTS
---------------
The words route matches letters and the built-in nearness route matches
pieces of words (heron_embed: "synonyms: nothing"). So "VCD" and "volume
control damper" share nothing either route can see, and a modeller who types
the short form is ranked as if he had typed a word no card uses.

WHAT IT DOES, AND THE FOUR THINGS IT NEVER DOES
-----------------------------------------------
A request whose words include a short form somebody RECORDED is searched with
that form's long form added after it. The modeller's own words stay, in their
own order, first:

    "isolate the VCDs"  ->  "isolate the VCDs volume control damper"

  it never removes a word     the long form is added, nothing is replaced
  it never runs before the    an exact declared phrase and a remembered
  user's own words            answer are asked with the words as typed
                              (heron_retrieve.find) - this only widens the
                              ranked search that runs when both miss
  it never guesses            a word nobody recorded is left exactly as it
                              was typed (D-33, D-34). No plural rule, no
                              spelling rule: "FCUs" is not "FCU" until
                              somebody records it
  it never works quietly      the answer's note names each short form it
                              read, what it read it as, and who recorded it

WHY HERON SHIPS NO LIST
-----------------------
D-34: "Heron builds nothing to understand language. No phrase list, no
synonym table." And in its own consequences: a site word that maps to a
Revit word "is knowledge - it belongs in Heron's own knowledge store where it
can be looked up and corrected". A file in this folder is PRODUCT - replaced
on every update, never edited by the modeller (heron_paths) - so it could be
neither looked up by him nor corrected by him. The list is DATA: one file in
Heron's home, the modeller's own.

    %USERPROFILE%\\.heron\\short-forms.yaml      (HERON_DATA\\short-forms.yaml when set)

`short-forms.example.yaml` beside this file shows the shape, with the three
short forms the owner recorded on 2026-09-23. Heron never reads it: it is
there to be copied.

EVERY ENTRY NAMES A PERSON AND A DATE (the retired Keyword Agent's rules,
docs/28 HERON-NAM-KEY-005, kept): an entry with no `by` or `at` is not a
record, an entry whose `by` says it was derived, guessed or inferred is
refused, and a short form recorded twice with two meanings is read as
neither - which one was meant is a question for the modeller, not a choice
for Heron.
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_paths as PATHS                                   # noqa: E402

FILE_NAME = "short-forms.yaml"
EXAMPLE = os.path.join(ROOT, "brain", "short-forms.example.yaml")

# What an entry must carry to be a record. `source` is asked for and not
# required: a modeller writing his own office's list is himself the source.
CARRIES = ("forms", "means", "by", "at")

# A `by` with any of these words is a process, not a person.
NOT_A_PERSON = ("derived", "guessed", "inferred", "automatic", "auto",
                "model", "suggested", "expanded", "generated")

_held = {"key": None, "list": None}


def where(environ=None):
    """The modeller's own list - a path, or None on a machine with no home."""
    home = PATHS.data_home(environ)
    return os.path.join(home, FILE_NAME) if home else None


def _person(who):
    words = str(who or "").strip().lower().split()
    return bool(words) and not any(w in NOT_A_PERSON for w in words)


def read(path):
    """(entries, problems) from one file. A file that is not there is an
    empty list and no problem: nobody has recorded anything yet."""
    if not path or not os.path.isfile(path):
        return [], []
    import yaml
    try:
        with io.open(path, encoding="utf-8") as handle:
            doc = yaml.safe_load(handle) or {}
    except (OSError, UnicodeError, yaml.YAMLError) as why:
        return [], ["%s could not be read, so no short form was used: %s"
                    % (path, why)]
    rows = doc.get("short-forms") if isinstance(doc, dict) else None
    if rows is None:
        return [], []
    if not isinstance(rows, list):
        return [], ["%s: `short-forms:` is not a list" % path]

    entries, problems, meanings = [], [], {}
    for n, row in enumerate(rows, 1):
        if not isinstance(row, dict):
            problems.append("entry %d is not a record" % n)
            continue
        forms = row.get("forms")
        forms = [forms] if isinstance(forms, str) else forms
        forms = [str(f).strip() for f in (forms or []) if str(f).strip()]
        missing = [k for k in CARRIES
                   if not (forms if k == "forms" else str(row.get(k) or "").strip())]
        if missing:
            problems.append("entry %d carries no %s, so it is not a record"
                            % (n, ", ".join(missing)))
            continue
        if not _person(row["by"]):
            problems.append("entry %d was recorded by '%s', which is not a "
                            "person (D-34): refused" % (n, row["by"]))
            continue
        entry = {"forms": forms, "means": str(row["means"]).strip(),
                 "by": str(row["by"]).strip(), "at": str(row["at"]).strip(),
                 "source": str(row.get("source") or "").strip()}
        entries.append(entry)
        for form in forms:
            meanings.setdefault(form.lower(), set()).add(entry["means"].lower())

    twice = sorted(f for f, said in meanings.items() if len(said) > 1)
    if twice:
        problems.append("recorded with two meanings, so read as neither - ask "
                        "which is meant: %s" % ", ".join(twice))
        entries = [dict(e, forms=[f for f in e["forms"] if f.lower() not in twice])
                   for e in entries]
        entries = [e for e in entries if e["forms"]]
    return entries, problems


def recorded(environ=None):
    """The modeller's list, read again only when the file changed."""
    path = where(environ)
    try:
        stat = os.stat(path) if path else None
        key = (path, stat.st_mtime, stat.st_size) if stat else (path, None, None)
    except OSError:
        key = (path, None, None)
    if _held["key"] != key:
        _held["key"], _held["list"] = key, read(path)
    return _held["list"]


def _pattern(form):
    return re.compile(r"(?<!\w)%s(?!\w)" % re.escape(form), re.IGNORECASE)


def expand(text, entries=None):
    """(text to search, [entries used]). The words as typed, then each long
    form a recorded short form in them stands for, once, unless the long form
    is already there. Nothing recorded in them - the text exactly as given."""
    if entries is None:
        entries = recorded()[0]
    said = text or ""
    lowered = re.sub(r"\s+", " ", said.lower())
    added, used = [], []
    for entry in entries:
        if entry["means"].lower() in lowered:
            continue
        if any(_pattern(form).search(said) for form in entry["forms"]):
            added.append(entry["means"])
            used.append(entry)
    if not added:
        return said, []
    return "%s %s" % (said.rstrip(), " ".join(added)), used


def sentence(used):
    """What the answer's note says about it - never left out when it ran."""
    if not used:
        return ""
    return ("searched with the modeller's short forms: %s"
            % "; ".join("%s read as '%s' (recorded by %s, %s)"
                        % ("/".join(e["forms"]), e["means"], e["by"], e["at"])
                        for e in used))


def main(argv):
    path = where()
    entries, problems = recorded()
    if not path:
        print("No home: no HERON_DATA and not Windows, so no list is read.")
    elif not os.path.isfile(path):
        print("No list at %s - nobody has recorded a short form here." % path)
        print("To add yours: copy %s there and edit it." % EXAMPLE)
    else:
        print("%s - %d short form(s)" % (path, len(entries)))
        for e in entries:
            print("  %-12s %s   (%s, %s)" % ("/".join(e["forms"]), e["means"],
                                            e["by"], e["at"]))
    for problem in problems:
        print("  NOT USED: %s" % problem)
    if argv:
        searched, used = expand(" ".join(argv), entries)
        print("searched as: %s" % searched)
        if used:
            print(sentence(used))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
