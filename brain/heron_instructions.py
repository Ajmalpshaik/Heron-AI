# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-PRO-011
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Prompt / Instruction Registry - one home for every instruction Heron gives.

    python brain/heron_instructions.py              validate them, run the cases
    python brain/heron_instructions.py agent.read   print what that agent is told

WHY IT IS NOT "JUST STRINGS IN THE CODE" (docs/23 s9)
-----------------------------------------------------
Scattered prompts are the most common reason an AI system stops being
maintainable: behaviour changes and nobody can find which string caused it. The
kernel document asks for three properties and this module exists to make them
real rather than aspirational.

  VERSIONED    every instruction carries a version, so a wording change is a
               diff with an author and a date rather than a mystery.
  TESTABLE     every instruction carries evaluation cases, so a "small wording
               improvement" that drops a rule is caught by `python
               brain/heron_instructions.py` and not by a modeller.
  COMPOSABLE   an instruction declares the Constitution articles it needs and
               they are ASSEMBLED from HERON_CONSTITUTION.md, never copied.

THE COPY RULE, AND WHY IT IS ENFORCED RATHER THAN ASKED FOR
------------------------------------------------------------
The Constitution says it itself: "Articles are assembled into an agent's
instructions from this file via the Prompt/Instruction Registry, so there is
one source and no copies to drift. An agent receives the Articles relevant to
its permission level and department - not all 30."

A copied article is worse than a missing one. It looks current, it reads as
authority, and it goes stale the first time the Constitution is amended - and
nothing would ever say so. So an instruction whose own text repeats a sentence
from an article is REFUSED here, and told to declare the article number.

WHAT THIS MODULE DOES NOT DO
-----------------------------
It does not call a model, and it does not decide which model would be called -
that is the Model Router (HERON-KRN-MDL-010). It assembles text. A T2 agent
asks this for its words and the router for its provider, and neither knows
about the other.
"""

import io
import os
import re
import sys

try:
    import yaml
except ImportError:                                          # pragma: no cover
    sys.stderr.write(
        "Heron's brain needs PyYAML to read an instruction.\n"
        "  pip install --user pyyaml\n")
    raise

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HERE = os.path.dirname(os.path.abspath(__file__))
if HERE not in sys.path:
    sys.path.insert(0, HERE)

# For repo_relative() and nothing else. `os.path.relpath` RAISES on Windows
# across drives rather than returning something useless, and INSTRUCTIONS_DIR is
# not always inside the checkout - a test points it at a temp folder, which on
# this machine is on C: while the repository is on D:. That raise happened
# BEFORE the duplicate-id check below, so a duplicate could not be reported at
# all, and the ValueError raised instead was indistinguishable BY TYPE from the
# one this module raises on purpose. heron_fragment already owns the one answer
# to this question, so this adds no second implementation.
import heron_fragment as FRAG                     # noqa: E402

INSTRUCTIONS_DIR = os.path.join(ROOT, "brain", "instructions")
CONSTITUTION = os.path.join(ROOT, "HERON_CONSTITUTION.md")

REQUIRED = ("id", "version", "purpose", "text", "cases")

# A numbered rule in the Constitution: "**12a. Pin the document...**". The
# letter suffixes are real - 12a, 12b and 12c were added when session binding
# grew three rules of its own - so the id is a string and never an int.
RULE = re.compile(r"^\*\*(\d+[a-z]?)\.\s+(.+?)\*\*\s*$")

# How much of an article has to reappear in an instruction's own text before it
# counts as a copy. Eight words is long enough that a shared phrase like "the
# active document" does not trip it, and short enough that a pasted sentence
# cannot slip under it.
COPY_WINDOW = 8


def articles():
    """
    {number: text} for every numbered rule in the Constitution.

    Read at assembly time rather than cached to a file. A cache would be a
    copy, and this whole module exists because a copy goes stale.
    """
    found = {}
    number = None
    lines = []
    if not os.path.exists(CONSTITUTION):
        raise IOError("CONSTITUTION_UNREADABLE: %s is not there, and every "
                      "instruction assembles its rules from it"
                      % CONSTITUTION)
    with io.open(CONSTITUTION, encoding="utf-8") as fh:
        for line in fh:
            match = RULE.match(line.rstrip("\n"))
            if match:
                if number:
                    found[number] = "".join(lines).strip()
                number = match.group(1)
                lines = ["%s. %s\n" % (number, match.group(2))]
                continue
            if number is None:
                continue
            if line.startswith("## "):                # the article ended
                found[number] = "".join(lines).strip()
                number, lines = None, []
                continue
            lines.append(line)
    if number:
        found[number] = "".join(lines).strip()
    return found


def instructions():
    """{id: declaration} for every instruction on disk."""
    found = {}
    if not os.path.isdir(INSTRUCTIONS_DIR):
        return found
    for name in sorted(os.listdir(INSTRUCTIONS_DIR)):
        if not name.endswith((".yaml", ".yml")):
            continue
        path = os.path.join(INSTRUCTIONS_DIR, name)
        with io.open(path, encoding="utf-8") as fh:
            data = yaml.safe_load(fh) or {}
        data["_path"] = FRAG.repo_relative(path).replace(os.sep, "/")
        key = data.get("id") or name
        if key in found:
            # NEVER SILENTLY. The later filename used to win, so a duplicate
            # could replace a permission-bearing instruction and take its
            # articles and its cases with it, and the registry would report
            # nothing wrong at all.
            raise ValueError(
                "INSTRUCTION_DUPLICATED: '%s' is declared by both %s and %s. "
                "One of them would silently replace the other, taking its "
                "articles and its evaluation cases with it."
                % (key, found[key]["_path"], data["_path"]))
        found[key] = data
    return found


def _words(text):
    return re.findall(r"[a-z0-9]+", (text or "").lower())


def _shingles(text, size=COPY_WINDOW):
    words = _words(text)
    return {tuple(words[i:i + size]) for i in range(len(words) - size + 1)}


def compose(instruction_id, known=None, rules=None, extra_articles=(),
            _seen=None, _render=True):
    """
    (text, articles_used) - the words an agent is actually given.

    Order is fixed and not a preference: the instruction's own purpose and
    text first, then what it includes, then the Constitution articles. A rule
    read last is the one that stays read.

    THE ARTICLES ARE RENDERED ONCE, AT THE OUTERMOST CALL. An included
    instruction returns its words and the NUMBERS it declared, not the article
    text - because the parent appends everything it collected at the end, and
    a nested call that had already rendered its own would put every article of
    agent.base into agent.modify twice. It did, until 2026-09-14: article 22
    arrived in the real agent.modify instruction two times, along with a second
    copy of the line introducing them. A rule an agent is told twice is a rule
    weighted twice, in the one place where weighting is invisible.

    Raises KeyError for a missing instruction, article or include, and
    ValueError for a cycle - each of which is a failure state the contract
    declares. A composer that quietly returned a partial instruction would be
    an agent running with a rule it was never given.
    """
    known = instructions() if known is None else known
    rules = articles() if rules is None else rules
    seen = list(_seen or [])

    if instruction_id in seen:
        raise ValueError("INCLUDE_CYCLE: %s"
                         % " -> ".join(seen + [instruction_id]))
    if instruction_id not in known:
        raise KeyError("INSTRUCTION_NOT_FOUND: no instruction '%s'"
                       % instruction_id)

    seen = seen + [instruction_id]
    declaration = known[instruction_id]
    parts = [(declaration.get("text") or "").strip()]
    used = []

    for included in declaration.get("includes") or []:
        if included not in known:
            raise KeyError("INCLUDE_NOT_FOUND: %s includes '%s', which "
                           "does not exist" % (instruction_id, included))
        text, inner = compose(included, known, rules, (), seen, _render=False)
        parts.append(text)
        for number in inner:
            if number not in used:
                used.append(number)

    wanted = [str(a) for a in (declaration.get("articles") or [])]
    wanted += [str(a) for a in extra_articles]
    for number in wanted:
        if number not in rules:
            raise KeyError("ARTICLE_NOT_FOUND: no Constitution article '%s'"
                           % number)
        if number in used:
            continue
        used.append(number)

    if used and _render:
        parts.append("Rules you may not violate, from HERON_CONSTITUTION.md:")
        parts.extend(rules[n] for n in used)

    return "\n\n".join(p for p in parts if p), used


def validate(known=None, rules=None):
    """Everything wrong with the registry, as sentences a person can act on."""
    known = instructions() if known is None else known
    rules = articles() if rules is None else rules
    problems = []

    for key, declaration in sorted(known.items()):
        where = declaration.get("_path", key)

        for field in REQUIRED:
            if not declaration.get(field):
                problems.append("%s: has no '%s'" % (where, field))

        declared_id = declaration.get("id")
        if declared_id and declared_id != key:
            problems.append("%s: declares id '%s' but is registered as '%s'"
                            % (where, declared_id, key))

        version = str(declaration.get("version", ""))
        if version and not re.match(r"^\d+\.\d+\.\d+$", version):
            problems.append("%s: version '%s' is not MAJOR.MINOR.PATCH"
                            % (where, version))

        cases = declaration.get("cases") or []
        if not cases:
            problems.append(
                "%s: has no evaluation case - an instruction nothing tests is "
                "a wording change nobody can catch (docs/23 s9)" % where)
        for case in cases:
            case = case if isinstance(case, dict) else {}
            for field in ("must-contain", "must-not-contain"):
                stated = case.get(field)
                if stated is None:
                    continue
                if isinstance(stated, str) or not isinstance(stated, (list,
                                                                      tuple)):
                    # "do" is truthy and iterable, so run_cases() scored it as
                    # two assertions, 'd' and 'o' - both trivially present.
                    # A malformed case made an instruction look tested.
                    problems.append(
                        "%s: case '%s' declares %s as '%s'. It must be a "
                        "LIST of phrases - a bare string is iterated one "
                        "character at a time and passes trivially."
                        % (where, case.get("name", "unnamed"), field, stated))
                elif any(not isinstance(p, str) or not p.strip()
                         for p in stated):
                    problems.append(
                        "%s: case '%s' has an empty or non-text phrase in %s."
                        % (where, case.get("name", "unnamed"), field))
            if not (case.get("must-contain") or case.get("must-not-contain")):
                # A case with a name and no assertion passed the "has cases"
                # check and then ran zero assertions, so an instruction could
                # be fully untested while looking tested.
                problems.append(
                    "%s: case '%s' asserts nothing. A case needs at least one "
                    "must-contain or must-not-contain, or it is a name where "
                    "a test should be."
                    % (where, case.get("name", "unnamed")))

        for number in declaration.get("articles") or []:
            if str(number) not in rules:
                problems.append("%s: declares article '%s', which is not in "
                                "HERON_CONSTITUTION.md" % (where, number))

        for included in declaration.get("includes") or []:
            if included not in known:
                problems.append("%s: includes '%s', which does not exist"
                                % (where, included))

        # The copy rule. Compared against every article, not only the declared
        # ones: copying an article you did not declare is the worse half.
        own = _shingles(declaration.get("text"))
        if own:
            for number, rule_text in sorted(rules.items()):
                if own & _shingles(rule_text):
                    problems.append(
                        "%s: repeats the wording of article %s in its own "
                        "text. Declare it under 'articles' instead - one "
                        "source, no copies to drift" % (where, number))
                    break

        try:
            compose(key, known, rules)
        except ValueError as exc:
            problems.append("%s: %s" % (where, exc))
        except KeyError:
            pass                          # already reported above, in detail

    return problems


def run_cases(known=None, rules=None):
    """
    (passed, failures) for every evaluation case in the registry.

    A case is deliberately dumb: assemble the instruction and assert that a
    phrase is present, or absent. It cannot tell whether the wording is GOOD -
    nothing without a model can - but it catches the failure that actually
    happens, which is a rule quietly falling out of an instruction during a
    tidy-up.
    """
    known = instructions() if known is None else known
    rules = articles() if rules is None else rules
    passed, failures = 0, []

    for key, declaration in sorted(known.items()):
        try:
            text, _used = compose(key, known, rules)
        except (KeyError, ValueError) as exc:
            failures.append("%s: cannot be assembled - %s" % (key, exc))
            continue
        lowered = text.lower()
        for case in declaration.get("cases") or []:
            name = case.get("name", "unnamed case")
            if any(isinstance(case.get(f), str)
                   for f in ("must-contain", "must-not-contain")):
                failures.append("%s / %s: an assertion is a bare string, "
                                "which validate() refuses - not run here"
                                % (key, name))
                continue
            for phrase in case.get("must-contain") or []:
                if phrase.lower() in lowered:
                    passed += 1
                else:
                    failures.append("%s / %s: the assembled instruction does "
                                    "not contain '%s'" % (key, name, phrase))
            for phrase in case.get("must-not-contain") or []:
                if phrase.lower() not in lowered:
                    passed += 1
                else:
                    failures.append("%s / %s: the assembled instruction "
                                    "contains '%s', and must not"
                                    % (key, name, phrase))
    return passed, failures


def main(argv):
    rules = articles()
    known = instructions()

    if len(argv) == 2:
        try:
            text, used = compose(argv[1], known, rules)
        except (KeyError, ValueError) as exc:
            print("%s" % exc)
            return 1
        print(text)
        print()
        print("--- assembled from %d article(s): %s"
              % (len(used), ", ".join(used) or "none"))
        return 0

    print("INSTRUCTION REGISTRY   brain/instructions/")
    print("=" * 67)
    problems = validate(known, rules)
    for key in sorted(known):
        broken = [p for p in problems if p.startswith(
            known[key].get("_path", key))]
        print("  %-5s %-24s v%-8s %d article(s), %d case(s)"
              % ("FAIL" if broken else "ok", key,
                 known[key].get("version", "?"),
                 len(known[key].get("articles") or []),
                 len(known[key].get("cases") or [])))

    passed, failures = run_cases(known, rules)
    print()
    print("  %d instruction(s) - %d of the Constitution's %d articles are "
          "assembled, never copied"
          % (len(known),
             len({a for d in known.values() for a in (d.get("articles") or [])}),
             len(rules)))
    print("  %d evaluation assertion(s) passed" % passed)

    if problems or failures:
        print()
        for line in problems + failures:
            print("  %s" % line)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
