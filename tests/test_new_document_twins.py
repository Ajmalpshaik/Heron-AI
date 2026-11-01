# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
CREATE_FAMILY_DOCUMENT makes a family or a project from one body.

    python tests/test_new_document_twins.py

WHAT CHANGED ON 2026-11-01, AND WHY THIS TEST CHANGED WITH IT. Until then
there were two live cards, and this test held their bodies level - the
history below. CREATE_PROJECT_DOCUMENT was merged into CREATE_FAMILY_DOCUMENT
on the owner's instruction ("make sure there is no duplicate tools") and is
DEPRECATED: the template's extension now chooses the kind inside one file, so
there is no second live copy to drift. The fact the comparison held is gone;
what it protected is not. So this now holds that the one card carries BOTH
kinds' lines - the extensions, the two creating calls, the two template
folders - that the retired card is DEPRECATED and points at it, and the
same safety rules as before, in the one body.

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
    if family_body is None:
        print("FAIL: create-family-document needs exactly one line ending '%s'" % MARKER)
        return 1

    # BOTH KINDS IN THE ONE CARD (2026-11-01). Every word the two cards' heads
    # each carried is in the keeper's head now.
    for word in ('"family"', '".rft"', '".rfa"', "NewFamilyDocument", "FamilyTemplatePath",
                 '"project"', '".rte"', '".rvt"', "NewProjectDocument", "DefaultProjectTemplate"):
        if word not in family_head:
            failures.append("create-family-document: the kind-specific lines lack %s" % word)

    # THE RETIRED CARD IS DEPRECATED AND POINTS HERE - so it is offered by
    # nothing, and its impl/ is the record, no longer compared.
    card = read(os.path.join(FRAGMENTS, "create-project-document", "fragment.yaml"))
    if "heron-status: DEPRECATED" not in card:
        failures.append("create-project-document is not DEPRECATED - two live cards would "
                        "make one body two drifting copies again")
    if "superseded-by: CREATE_FAMILY_DOCUMENT" not in card:
        failures.append("create-project-document does not name CREATE_FAMILY_DOCUMENT "
                        "as its successor")
    if not os.path.isfile(PROJECT):
        failures.append("create-project-document's impl/ is gone - a retired card is "
                        "kept, never deleted (Article 1)")

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
    print("PASS: one new-document fragment makes both kinds and keeps the body's rules")
    return 0


if __name__ == "__main__":
    sys.exit(main())
