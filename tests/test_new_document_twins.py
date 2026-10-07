# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
CREATE_FAMILY_DOCUMENT and CREATE_PROJECT_DOCUMENT stay one body.

    python tests/test_new_document_twins.py

WHY. The two fragments do one job - make a document from a template, save it
to the caller's path without overwriting, close the windowless copy, open the
file in a window - and differ only in a family against a project. They are two
cards because they route differently (a "new project" request is not a "new
family" one), and so they are two files. Two files of the same code drift: a
fix to the overwrite check in one and not the other is exactly how a model
gets written over. So everything below each file's SHARED line must be
identical, and the lines above it must be the only place they differ.

It also holds the safety rules the bodies carry, so a later edit to both at
once cannot quietly drop them: never overwrite, never invent a path, never
create a folder, and never open a window while Heron's transaction is open in
the document in front.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FRAGMENTS = os.path.join(ROOT, "brain", "fragments")
FAMILY = os.path.join(FRAGMENTS, "create-family-document", "impl", "any", "fragment.cs")
PROJECT = os.path.join(FRAGMENTS, "create-project-document", "impl", "any", "fragment.cs")
MARKER = "from here down ----"


def read(path):
    # Normalised at the read: a CRLF checkout must not fail a body compare.
    with io.open(path, encoding="utf-8") as handle:
        return handle.read().replace("\r\n", "\n")


def split(path):
    text = read(path)
    if text.count(MARKER) != 1:
        return None, None
    head, body = text.split(MARKER, 1)
    return head, body


def main():
    failures = []

    family_head, family_body = split(FAMILY)
    project_head, project_body = split(PROJECT)
    if family_body is None or project_body is None:
        print("FAIL: each fragment needs exactly one line ending '%s'" % MARKER)
        return 1

    if family_body != project_body:
        a, b = family_body.split("\n"), project_body.split("\n")
        for number, (left, right) in enumerate(zip(a, b), 1):
            if left != right:
                failures.append("bodies differ at shared line %d:\n  family : %s\n  project: %s"
                                % (number, left.strip(), right.strip()))
                break
        else:
            failures.append("bodies differ in length: %d against %d lines" % (len(a), len(b)))

    expected = {
        FAMILY: ('"family"', '".rft"', '".rfa"', "NewFamilyDocument", "FamilyTemplatePath"),
        PROJECT: ('"project"', '".rte"', '".rvt"', "NewProjectDocument", "DefaultProjectTemplate"),
    }
    for path, head in ((FAMILY, family_head), (PROJECT, project_head)):
        for word in expected[path]:
            if word not in head:
                failures.append("%s: the kind-specific lines lack %s"
                                % (os.path.basename(os.path.dirname(os.path.dirname(
                                    os.path.dirname(path)))), word))

    rules = {
        "OverwriteExistingFile = false": "the save must never overwrite",
        "File.Exists(target)": "an existing savePath must be refused before anything is made",
        "IsPathRooted(target)": "a relative savePath must be refused",
        "inFront.IsModifiable": "no window may be opened while the document in front is modifiable",
        "openNext": "the reply must name the path ACTIVATE_DOCUMENT opens",
    }
    for needle, why in rules.items():
        if needle not in family_body:
            failures.append("shared body lost '%s' - %s" % (needle, why))
    if "CreateDirectory" in family_body:
        failures.append("shared body creates a folder - it must refuse instead")

    if failures:
        for failure in failures:
            print("FAIL: " + failure)
        return 1
    print("PASS: the two new-document fragments share one body and keep its rules")
    return 0


if __name__ == "__main__":
    sys.exit(main())
