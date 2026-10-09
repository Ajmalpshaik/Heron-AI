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
  5. THE REFUSAL ITSELF, in the readers whose fixed name is guarded inline.
     Check 1 asks only that a name is counted somewhere; a review on
     2026-10-09 deleted the refusal, kept the count, and check 1 stayed
     green. So for each `R.LookupParameter(X)` here: `V = R.GetParameters(X)
     .Count;` comes first, on the same receiver, as a statement of its own;
     nothing changes V before `if (V > 1)`, which follows it, before the
     lookup; that branch says NOT READ in a string, or adds the element to a
     list whose own `if (L.Count > 0)` says it; it assigns no value; the
     lookup is not inside it; and the lookup is reached only past it - the
     branch returns or continues, or the lookup sits in its `else`. Comments
     are removed first, string literals kept. A second review the same day
     weakened the count, reassigned it, read the first match inside the
     refusal and filled in a value there, and each passed until these rules.

WHAT IT CANNOT DO
Run a fragment. Whether Revit really hands back two parameters for a name, and
which one LookupParameter would have picked, needs a model that carries a
duplicated name - NEEDS-CHECKING Group AP arranges it. A green run here means
the guard is written, not that it has been seen to work. Check 5 reads one
shape of code, the inline one; the fragments that guard through a helper of
their own are held by check 1 alone, so a refusal deleted there with its count
kept would still pass.
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
    # Fixed names that spread after the row's census, found by this suite's own
    # rule run over the whole library and repaired 2026-10-09: a linked host's
    # "Fire Rating", read the way check-sleeve-size reads it...
    "audit-mep-openings",
    "check-ceiling-coordination",
    "check-equipment-clearance",
    "check-minimum-clearance",
    "check-surface-fit",
    "check-valve-accessibility",
    "find-clashes",
    "probe-around-elements",
    "propose-mep-openings",
    "select-touching",
    # ...a Space's "Space Type", and a fitting's "Angle".
    "report-space-envelope",
    "report-sprinkler-network",
)

# Readers whose guard is written inline around one fixed name, so check 5 can
# read the refusal and not only the count. The others above guard a name that
# arrives from outside through a helper of their own, a shape check 5 does not
# parse; check 1 is what holds them.
REFUSED = (
    "audit-mep-openings",
    "check-ceiling-coordination",
    "check-equipment-clearance",
    "check-minimum-clearance",
    "check-surface-fit",
    "check-valve-accessibility",
    "find-clashes",
    "probe-around-elements",
    "propose-mep-openings",
    "select-touching",
    "report-space-envelope",
    "report-sprinkler-network",
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


def literal_end(code, i):
    """The index just past the string or char literal starting at i, or None."""
    n = len(code)
    if code.startswith('@"', i):
        j = i + 2
        while j < n:
            if code[j] == '"':
                if code.startswith('""', j):
                    j += 2
                    continue
                return j + 1
            j += 1
        return n
    if code[i] in "\"'":
        quote, j = code[i], i + 1
        while j < n and code[j] != quote and code[j] != "\n":
            j += 2 if code[j] == "\\" else 1
        return min(j + 1, n)
    return None


def without_comments(code):
    """Line and block comments out, string and char literals kept whole."""
    out, i, n = [], 0, len(code)
    while i < n:
        if code.startswith("//", i):
            j = code.find("\n", i)
            i = n if j < 0 else j
            continue
        if code.startswith("/*", i):
            j = code.find("*/", i + 2)
            i = n if j < 0 else j + 2
            out.append(" ")
            continue
        end = literal_end(code, i)
        if end is not None:
            out.append(code[i:end])
            i = end
            continue
        out.append(code[i])
        i += 1
    return "".join(out)


def skip_space(code, i):
    while i < len(code) and code[i].isspace():
        i += 1
    return i


def statement_end(code, i):
    """Just past the one statement at i: a braced block, or up to its `;`."""
    depth, n, braced = 0, len(code), code.startswith("{", i)
    while i < n:
        end = literal_end(code, i)
        if end is not None:
            i = end
            continue
        c = code[i]
        if c in "({[":
            depth += 1
        elif c in ")}]":
            depth -= 1
            if braced and depth == 0:
                return i + 1
        elif c == ";" and depth == 0 and not braced:
            return i + 1
        i += 1
    return n


LITERAL = re.compile(r'@"(?:[^"]|"")*"|"(?:[^"\\\n]|\\.)*"')
SAYS_NOT_READ = re.compile(r"not read", re.IGNORECASE)
LOOKUP_ON = re.compile(r"(\w+)\s*\.\s*LookupParameter\(\s*([^()]+?)\s*\)")


def says_not_read(text):
    return any(SAYS_NOT_READ.search(lit) for lit in LITERAL.findall(text))


def refusal_problems(code):
    """What check 5 finds wrong with each LookupParameter in comment-free code."""
    problems = []
    for lookup in LOOKUP_ON.finditer(code):
        receiver, name = lookup.group(1), lookup.group(2)
        where = "%s.LookupParameter(%s)" % (receiver, name)
        # THE COUNT IS THE COUNT: the statement ends at `.Count;`, so neither
        # `.Count - 1` nor `.Count > 1 ? 1 : 0` passes for it - a review on
        # 2026-10-09 weakened it both ways and this check stayed green.
        counts = list(re.finditer(
            r"\b(\w+)\s*=\s*%s\s*\.\s*GetParameters\(\s*%s\s*\)\s*\.\s*Count\s*;"
            % (re.escape(receiver), re.escape(name)), code[:lookup.start()]))
        if not counts:
            problems.append("%s is not preceded by `V = %s.GetParameters(%s).Count;` on the "
                            "same receiver, the count and nothing else" % (where, receiver, name))
            continue
        var = counts[-1].group(1)
        guard = re.compile(r"\bif\s*\(\s*%s\s*(?:>\s*1|>=\s*2)\s*\)" % re.escape(var)).search(
            code, counts[-1].end(), lookup.start())
        if guard is None:
            problems.append("%s: nothing between the count and the lookup says `if (%s > 1)` - "
                            "two parameters by that name would be read from whichever came "
                            "first" % (where, var))
            continue
        # AND NOTHING CHANGES IT between the count and the `if`.
        between = LITERAL.sub('""', code[counts[-1].end():guard.start()])
        if re.search(r"\b%s\s*(?:[-+*/%%]?=(?![=>])|\+\+|--)|(?:\+\+|--)\s*%s\b"
                     % (re.escape(var), re.escape(var)), between):
            problems.append("%s: `%s` is changed between its count and `if (%s > 1)` - the "
                            "branch would test something other than the count"
                            % (where, var, var))
            continue
        start = skip_space(code, guard.end())
        end = statement_end(code, start)
        branch = code[start:end]

        # THE LOOKUP IS NOT IN THE REFUSAL. A branch that returns the first
        # match under a NOT READ label reads the name it refuses.
        if start <= lookup.start() < end:
            problems.append("%s sits inside the branch under `if (%s > 1)` - the refusal "
                            "reads the first match it exists to refuse" % (where, var))
            continue
        # AND THE REFUSAL SETS NO VALUE: a doubled name is NOT READ, never a
        # value put in its place (`angle = 0`, `spaceType = "Office"`).
        plain = LITERAL.sub('""', branch)
        if re.search(r"(?<![=!<>+\-*/%&|^])=(?![=>])|\+\+|--|[-+*/%]=", plain):
            problems.append("%s: the branch under `if (%s > 1)` assigns a value - a refused "
                            "name must be left unread, not filled in" % (where, var))

        leaves = re.search(r"\b(?:return|continue)\b", branch) is not None
        if not leaves:
            after = skip_space(code, end)
            inside_else = False
            if re.match(r"else\b", code[after:after + 5]):
                body = skip_space(code, after + 4)
                inside_else = body <= lookup.start() < statement_end(code, body)
            if not inside_else:
                problems.append("%s is reached after `if (%s > 1)` - the branch neither "
                                "returns nor continues, and the lookup is not in its `else`"
                                % (where, var))

        said = says_not_read(branch)
        if not said:
            for listed in re.findall(r"\b(\w+)\s*\.\s*Add\(", branch):
                later = re.compile(r"\bif\s*\(\s*%s\s*\.\s*Count\s*(?:>\s*0|>=\s*1|!=\s*0)\s*\)"
                                   % re.escape(listed)).search(code, end)
                if later is not None:
                    told = skip_space(code, later.end())
                    if says_not_read(code[told:statement_end(code, told)]):
                        said = True
        if not said:
            problems.append("%s: the branch under `if (%s > 1)` never says NOT READ - a "
                            "doubled name would be shown as a missing one, or not at all"
                            % (where, var))
    return problems


def the_refusal():
    print("5. In the inline readers, the refusal itself: counted, branched on, said, and "
          "the lookup only past it")
    for name in REFUSED:
        paths = impl_files(name)
        check(len(paths) > 0, "%s: no impl/*/fragment.cs to read" % name)
        for path in paths:
            code = without_comments(read(path))
            release = os.path.basename(os.path.dirname(path))
            check(LOOKUP_ON.search(code) is not None,
                  "%s (%s): no LookupParameter on a named receiver left to hold to the "
                  "refusal - check 5 has nothing to read" % (name, release))
            problems = refusal_problems(code)
            for problem in problems:
                check(False, "%s (%s): %s (D-54 s3, 5b-203)" % (name, release, problem))
            if not problems:
                check(True, "%s (%s): the refusal is in place" % (name, release))


def main():
    the_guard()
    the_card()
    the_negative_case()
    the_count()
    the_refusal()
    if failures:
        print("\n%d check(s) failed, %d passed" % (len(failures), passes[0]))
        return 1
    print("\nPASS: %d checks - every listed fragment refuses a name two parameters share"
          % passes[0])
    return 0


if __name__ == "__main__":
    sys.exit(main())
