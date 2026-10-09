# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
A parameter name two parameters share is refused, never read from the first match.

    python tests/test_same_name_guard.py

WHY. `Element.LookupParameter(name)` returns "the first one encountered" when
two parameters on one element share the name, and Autodesk's own reference
says that match "is determined at random". A shared or project parameter bound
beside a built-in one of the same name does it; so does a curtain wall type,
which shows its mullion settings twice. D-54 s3 is the rule: a name that
matches twice is refused, never chosen from. FRAGMENT-ISSUES row 5b-203
(2026-09-24) found fragments taking the first match; the ones that change the
model were repaired when it was found, and this suite holds them and the
readers repaired since to it, so a later edit cannot quietly drop a guard.

WHAT IT CHECKS, AS TEXT
  1. THE GUARD. In each fragment below, every name handed to `LookupParameter`
     is also counted with `GetParameters` in the same file - the count that
     tells one parameter from two. Comments are removed first, so a guard
     described in words does not count.
  2. THE CARD SAYS SO. Each card cites row 5b-203, so whoever reads it before
     a proof knows the refusal is there.
  3. THE READERS' NEGATIVE CASE. Each fragment that only reads declares a
     negative case for a shared name in tests/cases.yaml - the case a proof
     has to arrange, because every proof so far used a name that occurs once.
  4. THE COUNT IS ACCOUNTING. Where a reader reports the refused elements as a
     count of its own, the card declares it `role: accounting`: an element
     turned down is evidence it looked, never a thing it found (D-52).

WHAT IT CANNOT DO
Run a fragment. Whether Revit really hands back two parameters for a name, and
which one LookupParameter would have picked, needs a model that carries a
duplicated name - NEEDS-CHECKING Group AP arranges it. A green run here means
the guard is written, not that it has been seen to work.
"""

import io
import os
import re
import sys

import yaml

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")

# The ones that change the model, repaired when row 5b-203 was found.
WRITERS = (
    "write-element-parameters", "copy-parameter-value", "edit-parameter-text",
    "edit-text-values", "import-parameter-values", "remove-parameter-value",
    "renumber-sequential", "assign-location-data", "create-view-filters-by-value",
    "color-by-parameter",
)

# The ones that only read, repaired since. Each answers a question, so a name
# read from the first match is a confidently wrong answer rather than a write.
READERS = (
    "check-model-standards",
    "select-by-parameter-value",
    "select-by-numeric-parameter",
    "check-family-standards",
    "check-sleeve-size",
)

# Readers that report the refused elements as a count of their own.
COUNTED = {
    "select-by-parameter-value": "ambiguous",
    "select-by-numeric-parameter": "ambiguous",
}

LOOKUP = re.compile(r"\.LookupParameter\(\s*([^()]+?)\s*\)")
COUNT = re.compile(r"\.GetParameters\(\s*([^()]+?)\s*\)")
LINE_COMMENT = re.compile(r"//[^\n]*")

failures = []
passes = [0]


def check(ok, what):
    if ok:
        passes[0] += 1
    else:
        failures.append(what)
        print("  FAIL: " + what)


def read(path):
    # Normalised at the read: a CRLF checkout must not move a match.
    with io.open(path, encoding="utf-8") as handle:
        return handle.read().replace("\r\n", "\n")


def impl_files(name):
    impl = os.path.join(FRAGMENTS, name, "impl")
    found = []
    if not os.path.isdir(impl):
        return found
    for release in sorted(os.listdir(impl)):
        path = os.path.join(impl, release, "fragment.cs")
        if os.path.isfile(path):
            found.append(path)
    return found


def the_guard():
    print("1. Every name handed to LookupParameter is also counted with GetParameters")
    for name in WRITERS + READERS:
        paths = impl_files(name)
        check(len(paths) > 0, "%s: no impl/*/fragment.cs to read" % name)
        for path in paths:
            code = LINE_COMMENT.sub("", read(path))
            looked = set(LOOKUP.findall(code))
            counted = set(COUNT.findall(code))
            unguarded = sorted(looked - counted)
            check(not unguarded,
                  "%s (%s): LookupParameter(%s) with no GetParameters count of the same "
                  "name - two parameters by that name would be read from whichever Revit "
                  "returned first (D-54 s3, 5b-203)"
                  % (name, os.path.basename(os.path.dirname(path)), ", ".join(unguarded)))


def the_card():
    print("2. Each card cites row 5b-203")
    for name in WRITERS + READERS:
        card = os.path.join(FRAGMENTS, name, "fragment.yaml")
        text = read(card) if os.path.isfile(card) else ""
        check("5b-203" in text, "%s: fragment.yaml does not cite 5b-203" % name)


def the_negative_case():
    print("3. Each reader declares a negative case for a shared name")
    for name in READERS:
        path = os.path.join(FRAGMENTS, name, "tests", "cases.yaml")
        cases = {}
        if os.path.isfile(path):
            try:
                cases = yaml.safe_load(read(path)) or {}
            except yaml.YAMLError as exc:
                check(False, "%s: tests/cases.yaml does not parse: %s" % (name, exc))
                continue
        negative = cases.get("negative") if isinstance(cases, dict) else None
        cited = [row for row in (negative or [])
                 if isinstance(row, dict) and "5b-203" in yaml.safe_dump(row)]
        check(len(cited) > 0,
              "%s: no negative case in tests/cases.yaml cites 5b-203 - a proof would "
              "never arrange a shared name" % name)


def the_count():
    print("4. A count of refused elements is declared as accounting")
    for name, provide in sorted(COUNTED.items()):
        card = os.path.join(FRAGMENTS, name, "fragment.yaml")
        data = yaml.safe_load(read(card)) if os.path.isfile(card) else {}
        provides = ((data or {}).get("contract") or {}).get("provides") or []
        entry = [p for p in provides if isinstance(p, dict) and p.get("name") == provide]
        check(len(entry) == 1, "%s: contract.provides has no single `%s`" % (name, provide))
        if entry:
            check(entry[0].get("role") == "accounting",
                  "%s: `%s` is role %r - an element turned down is accounting, never "
                  "a result (D-52)" % (name, provide, entry[0].get("role")))
        code = "".join(LINE_COMMENT.sub("", read(p)) for p in impl_files(name))
        check(re.search(r"\b%s\s*\+\+" % re.escape(provide), code) is not None,
              "%s: the code never counts `%s`" % (name, provide))


def main():
    the_guard()
    the_card()
    the_negative_case()
    the_count()
    if failures:
        print("\n%d check(s) failed, %d passed" % (len(failures), passes[0]))
        return 1
    print("\nPASS: %d checks - every listed fragment refuses a name two parameters share"
          % passes[0])
    return 0


if __name__ == "__main__":
    sys.exit(main())
