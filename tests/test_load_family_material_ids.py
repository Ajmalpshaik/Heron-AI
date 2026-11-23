#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
LOAD_FAMILY compares each project material with ITSELF, by element id - never
two materials that happen to share a name.

    python tests/test_load_family_material_ids.py

WHY IT EXISTS - FRAGMENT-ISSUES ROW 5b-1995
-------------------------------------------
On 2026-11-23 a NEW load of a family into a real Revit 2020 project answered
`materialsChanged 8 item(s)` - Poche 0,0,0 -> 0,0,255, Default listed twice,
Glass twice, the three Phase materials' appearance assets - and none of it had
happened. The project held three materials named Default, three named Poche
and two each of Phase-Demo, Phase-Exist and Phase-Temp. Versions 1 to 3 of
brain/fragments/load-family/impl/any/fragment.cs keyed the before-reading on
`material.Name`, so the last material of each name won it and every other one
was compared against a different material. A modeller reading that answer
would believe the load had recoloured their project.

WHAT IT CHECKS, AS TEXT (comments removed first, so a rule only described in
words does not count)
  1. THE BEFORE-READING IS KEYED BY ELEMENT ID: a Dictionary<ElementId, ...>,
     filled at `[material.Id]`.
  2. THE AFTER-READING LOOKS UP THE SAME ID: TryGetValue(material.Id, ...).
     No lookup or fill by `material.Name` is left anywhere in the code.
  3. AN ID NOT THERE BEFORE IS ADDED, NOT CHANGED: the miss branch writes to
     `materialsAdded`, the moved branch to `materialsChanged`.
  4. A REPEATED NAME IS LABELLED, NOT PAIRED: the names are counted, a name
     held more than once carries "(id ", and one `findings` line names every
     such name.
  5. THE CARD FOLLOWS THE CODE: `findings` is a declared provide with role
     accounting, the version is at least 4, and the C# leaves `findings`.
  6. THE NEGATIVE CASE IS WRITTEN: the card's tests/cases.yaml has a negative
     case for two same-named materials reporting 0 changed.

WHAT IT CANNOT DO
Run the fragment. That ElementId keys match the same material across a
LoadFamily call, what Revit really does to a same-named material on a load,
and the 0-changed negative on a real model are NEEDS REAL REVIT - NEEDS-
CHECKING Group PU. A green run here means the rule is written, not that it has
been seen to work.
"""

import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CARD_DIR = os.path.join(ROOT, "brain", "fragments", "load-family")
IMPL = os.path.join(CARD_DIR, "impl", "any", "fragment.cs")
CARD = os.path.join(CARD_DIR, "fragment.yaml")
CASES = os.path.join(CARD_DIR, "tests", "cases.yaml")

failures = []
passes = [0]


def check(ok, what):
    print("  %-5s %s" % ("ok" if ok else "FAIL", what))
    if ok:
        passes[0] += 1
    else:
        failures.append(what)


def strip_comments(code):
    """C# without // and /* */ comments; string literals are kept whole."""
    out, i, n = [], 0, len(code)
    while i < n:
        c = code[i]
        if c == '"':
            j = i + 1
            while j < n and code[j] != '"':
                j += 2 if code[j] == "\\" else 1
            out.append(code[i:j + 1])
            i = j + 1
        elif code.startswith("//", i):
            j = code.find("\n", i)
            i = n if j < 0 else j
        elif code.startswith("/*", i):
            j = code.find("*/", i + 2)
            i = n if j < 0 else j + 2
        else:
            out.append(c)
            i += 1
    return "".join(out)


def read(path):
    with open(path, encoding="utf-8") as handle:
        return handle.read()


def main():
    code = strip_comments(read(IMPL))
    flat = re.sub(r"\s+", " ", code)
    card = read(CARD)
    cases = read(CASES)

    print("1. the before-reading is keyed by element id")
    check(re.search(r"materialsBefore = new Dictionary<ElementId, string>\(\)", flat)
          is not None, "materialsBefore is a Dictionary<ElementId, string>")
    check(re.search(r"materialsBefore\[material\.Id\] = describe\(material\)", flat)
          is not None, "it is filled at [material.Id]")

    print("2. the after-reading looks up the same id, and nothing pairs by name")
    check(re.search(r"materialsBefore\.TryGetValue\(material\.Id, out was\)", flat)
          is not None, "the after loop asks TryGetValue(material.Id, ...)")
    check(re.search(r"materialsBefore\[[^\]]*\.Name\]", flat) is None,
          "no materialsBefore[... .Name] fill is left")
    check(re.search(r"materialsBefore\.TryGetValue\([^,]*\.Name", flat) is None,
          "no TryGetValue by name is left")

    print("3. a new id is added, a moved id is changed")
    branch = re.search(
        r"if \(!materialsBefore\.TryGetValue\(material\.Id, out was\)\) ([^;]*;) "
        r"else if \(was != now\) ([^;]*;)", flat)
    check(branch is not None, "the miss branch and the moved branch are both there")
    if branch:
        check("materialsAdded.Add(" in branch.group(1)
              and "materialsChanged" not in branch.group(1),
              "an id not there before goes to materialsAdded only")
        check("materialsChanged.Add(" in branch.group(2)
              and "materialsAdded" not in branch.group(2),
              "an id whose reading moved goes to materialsChanged only")

    print("4. a repeated name is labelled with the id and named in findings")
    check(re.search(r"nameCount\[material\.Name\] = seen \+ 1", flat) is not None,
          "materials are counted by name after the load")
    check(re.search(r'nameCount\[material\.Name\] > 1', flat) is not None
          and '" (id "' in flat,
          "a name held more than once is labelled with \" (id \"")
    check(re.search(r"if \(pair\.Value > 1\) repeatedNames\.Add\(", flat) is not None,
          "every name held more than once is gathered")
    check(re.search(r"findings\.Add\(", flat) is not None
          and "compared with ITSELF" in flat,
          "findings names them and says each was compared with itself")

    print("5. the card follows the code")
    version = re.search(r"^version: (\d+)$", card, re.M)
    check(version is not None and int(version.group(1)) >= 4,
          "the card's version is at least 4 (found %s)"
          % (version.group(1) if version else "none"))
    provide = re.search(r"- name: findings\n(?:\s+#.*\n)*\s+role: accounting\n\s+type: IList<string>",
                        card)
    check(provide is not None, "findings is provided, role accounting, IList<string>")
    check(re.search(r"var findings = new List<string>\(\)", flat) is not None,
          "the C# declares the findings it leaves behind")

    print("6. the negative case is written")
    negative = cases.split("\nnegative:", 1)[1] if "\nnegative:" in cases else ""
    check("TWO materials of the same name" in negative
          and "`materialsChanged` 0 item(s)" in negative,
          "cases.yaml's negative: two same-named materials report 0 changed")

    print()
    if failures:
        print("FAIL - %d of %d checks" % (len(failures), len(failures) + passes[0]))
        for what in failures:
            print("  - " + what)
        return 1
    print("PASS - %d checks" % passes[0])
    return 0


if __name__ == "__main__":
    sys.exit(main())
