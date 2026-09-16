# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-NAM-VAL-002, HERON-NAM-GEN-001
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Naming validation - one place that answers "is this name right", and it
holds no rule of its own.

    python brain/heron_naming.py

WHAT IT IS FOR (docs/28, HERON-NAM-VAL-002)
--------------------------------------------
"Checks a name against the convention." T1, risk READ. It reports. It
renames nothing - HERON-NAM-REN-003 does that, and docs/06 s136 says it
may only do so after identity exists.

THE CONVENTION IS IN FOUR PLACES AND THIS IS NOT A FIFTH
----------------------------------------------------------
Every rule below is READ from the file that owns it:

    agent id        docs/28's own rows, through
                    heron_fragment.registry_agents()
    fragment id     heron_fragment.ID_PATTERN and .AREAS
    capability      heron_fragment.CAPABILITY_PATTERN
    the folder      docs/29 s130 - the capability, lower case, hyphens,
                    "DERIVED, never invented"
    a module        observed, not stated - see below

Nothing here retypes any of them. A naming validator with its own copy
of the convention is the thing that lets a name be right in one tool and
wrong in the next.

WHAT IS ENFORCED BY SHAPE NEEDS NO SECOND CHECK
-------------------------------------------------
docs/29 s127 says a fragment id "carries the area and a number and
NOTHING ELSE - never the name, never the kind, never the version". That
is not checked here, and deliberately: `FRG-[A-Z]{2,5}-[0-9]{3}` cannot
express a name, a kind or a version, so the rule holds by the shape of
the pattern. A check that cannot fail is decoration, and this file has
none.

ONE RULE IS OBSERVED RATHER THAN STATED, AND IT SAYS SO
---------------------------------------------------------
Every module in `brain/` is named `heron_<something>.py` and every suite
in `tests/` is `test_<something>.py`. Nothing in docs states it and
nothing enforces it; it is simply true of every file there. So this
agent reports it as OBSERVED - the count is in the answer - and a name
that breaks it is reported as breaking what everything else does, not as
breaking a rule somebody wrote. Those are different claims and reporting
the weaker one as the stronger is how a convention gets invented.

THE DEPARTMENT'S OWN NAME, STATED AT LAST (F15, D-79)
-------------------------------------------------------
Until 2026-09-16 this was the one kind it could not check. docs/28 said
five parts, docs/00c s368 said six, and NEITHER said what the name looks
like - no separator, no case, no order, no character set. It was refused
as UNSTATED_CONVENTION rather than guessed, because a guess would have
BECOME the convention by being the only thing enforcing one.

The owner settled it. Six parts, the version last:

    <domain>-<capability>-<purpose>-<platform>-<component>-v<n>
    mep-duct-insulation-check-revit-fitting-v1

THE RULE IS READ FROM docs/29, NOT KEPT HERE A SECOND TIME. That file
owns every other name shape in this system and now owns this one; the
regex below is the machine-readable half and the test asserts the two
agree, so the document cannot quietly drift away from the code.

AND THE HALF IT STILL CANNOT DO, SAID IN THE ANSWER
-----------------------------------------------------
A part may itself be hyphenated - `duct-insulation` is one part - so a
finished name CANNOT BE SPLIT BACK into its six. Seven segments, six
parts, and nothing in the string says where the capability ended.

So `check` reports that it checked the SHAPE, never that it checked the
parts, and `generate` is the half that knows them because it is handed
them. Single-token parts would make the name decomposable and would cost
searchability - `ductinsulation` - which is the one thing docs/06 s134
actually asks for. Readability won and the limit is declared.
"""

from __future__ import annotations

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/28's house style, the same row pattern heron_fragment reads the
# register with - anchored here so a name can be checked without a file.
AGENT_SHAPE = re.compile(r"^HERON-[A-Z0-9]+-[A-Z0-9]+-[0-9]+$")

# Observed, not stated. See the module docstring.
MODULE_SHAPE = re.compile(r"^heron_[a-z][a-z0-9_]*\.py$")
SUITE_SHAPE = re.compile(r"^test_[a-z][a-z0-9_]*\.py$")

KINDS = ("agent-id", "fragment-id", "capability", "fragment-folder",
         "module", "suite", "generated-name")

# THE GENERATED NAME - docs/29 "Naming - the generated name", D-79.
#
# Lower case, hyphen separated, version last. The regex is deliberately
# NOT six groups: a part may itself be hyphenated, so the segments cannot
# be mapped back onto the parts and a regex pretending otherwise would be
# a lie with a capture group in it.
GENERATED_SHAPE = re.compile(r"^[a-z0-9]+(?:-[a-z0-9]+)*-v[0-9]+$")

# The parts, in order, for generate() and for the test that asserts this
# tuple and docs/29's table are the same list.
GENERATED_PARTS = ("domain", "capability", "purpose", "platform",
                   "component", "version")

# Six parts need six separators' worth of segments at minimum. A name
# with fewer cannot be carrying all six, whatever it looks like.
GENERATED_MINIMUM = len(GENERATED_PARTS)


def _slug(value):
    """One part, lower case and hyphenated. Empty if nothing survives."""
    text = re.sub(r"[^a-z0-9]+", "-", str(value or "").strip().lower())
    return text.strip("-")


def generate(domain, capability, purpose, platform, component, version=1):
    """
    HERON-NAM-GEN-001. {ok, name, parts} - or a refusal.

    Every part is required. A MISSING one is refused rather than skipped,
    because a name silently five parts long would still LOOK like a valid
    generated name - it would pass the shape check, and nothing anywhere
    would say which part had gone.
    """
    given = (domain, capability, purpose, platform, component)
    parts = [_slug(one) for one in given]

    empty = [GENERATED_PARTS[i] for i, one in enumerate(parts) if not one]
    if empty:
        return {"ok": False, "refused": "MISSING_PART",
                "why": "%s had nothing usable in it. Every one of the six "
                       "is required - a name quietly built from five would "
                       "still pass the shape check, and nothing would say "
                       "which part had gone. docs/29, D-79."
                       % (", ".join(empty))}

    try:
        number = int(str(version).lower().lstrip("v"))
    except (TypeError, ValueError):
        number = -1
    if number < 1:
        return {"ok": False, "refused": "BAD_VERSION",
                "why": "%r is not a version. It is `v` and a whole number "
                       "from 1, and it is always the last part so that "
                       "every version of one thing sorts together."
                       % (version,)}

    name = "-".join(parts + ["v%d" % number])
    # GENERATED, THEN CHECKED BY THE SAME RULE THE VALIDATOR USES. If
    # these two could ever disagree, the department would be generating
    # names its own validator rejects.
    verdict = check("generated-name", name)
    if not verdict.get("ok"):
        return {"ok": False, "refused": "GENERATED_A_BAD_NAME",
                "why": "built %r and this agent's own validator refused "
                       "it: %s. That is a defect here, not bad input."
                       % (name, verdict.get("why"))}

    return {
        "ok": True,
        "name": name,
        "parts": dict(zip(GENERATED_PARTS, parts + ["v%d" % number])),
        "why": "six parts, version last, checked against the same rule "
               "HERON-NAM-VAL-002 applies. docs/29, D-79.",
        "unjudged": [
            "WHETHER THE PARTS ARE THE RIGHT WORDS. This agent is handed "
            "them and joins them; that they describe the thing is "
            "language, and nothing here read the thing.",
            "THE NAME CANNOT BE SPLIT BACK. A part may be hyphenated, so "
            "%d segments carry %d parts and the boundaries are gone. The "
            "parts are returned HERE because they were the input - "
            "reading them off the finished name is not possible."
            % (name.count("-") + 1, len(GENERATED_PARTS)),
        ],
    }


def folder_for(capability):
    """docs/29 s130: the capability, lower case, hyphens. DERIVED."""
    return str(capability or "").strip().lower().replace("_", "-")


def _observed(where, shape):
    """How many .py files in `where` match, and how many do not."""
    folder = os.path.join(ROOT, where)
    if not os.path.isdir(folder):
        return None
    every = [name for name in sorted(os.listdir(folder))
             if name.endswith(".py") and not name.startswith("__")]
    return (len([n for n in every if shape.match(n)]), len(every))


def check(kind, name, of=None):
    """
    {ok, kind, name, why} - or a refusal.

    `of` is what a name must be DERIVED from: a fragment folder's
    capability. Nothing is renamed and nothing is written.
    """
    kind = str(kind or "").strip().lower()
    if kind not in KINDS:
        return {"ok": False, "refused": "NOT_A_KIND_OF_NAME",
                "why": "'%s' is not a kind of name this system has. Known: "
                       "%s. A seventh is not invented here - the rules are "
                       "read from the files that own them, and there is no "
                       "file behind a kind nobody declared."
                       % (kind, ", ".join(KINDS))}

    text = str(name or "").strip()
    if not text or text != str(name or ""):
        return {"ok": False, "kind": kind, "refused": "NO_NAME",
                "why": "%s. A name that needs trimming before it can be "
                       "checked is already a different name from the one "
                       "on disk."
                       % ("nothing was named" if not text else
                          "the name %r has space around it" % name)}

    if kind == "generated-name":
        segments = text.split("-")
        if not GENERATED_SHAPE.match(text):
            return {"ok": False, "kind": kind, "name": text,
                    "refused": "WRONG_SHAPE",
                    "why": "a generated name is lower case letters, digits "
                           "and single hyphens, and ends with the version - "
                           "`v` and digits. %r does not. docs/29 \"Naming - "
                           "the generated name\", D-79." % text}
        if len(segments) < GENERATED_MINIMUM:
            return {"ok": False, "kind": kind, "name": text,
                    "refused": "WRONG_SHAPE",
                    "why": "%r has %d segment(s) and six parts need at "
                           "least %d: %s. A part may be hyphenated, so more "
                           "than six is normal and fewer is impossible."
                           % (text, len(segments), GENERATED_MINIMUM,
                              ", ".join(GENERATED_PARTS))}
        return {
            "ok": True, "kind": kind, "name": text,
            "why": "the SHAPE is right - lower case, single hyphens, "
                   "version last, %d segments for six parts. docs/29, D-79."
                   % len(segments),
            "unjudged": [
                "WHICH SEGMENT IS WHICH PART. A part may itself be "
                "hyphenated, so %d segments cannot be mapped back onto six "
                "parts and nothing here tried. This checked the shape; it "
                "did not check the parts, and saying otherwise would be "
                "the stronger claim reported as the weaker one."
                % len(segments),
            ],
        }

    if kind == "agent-id":
        if not AGENT_SHAPE.match(text):
            return {"ok": False, "kind": kind, "name": text,
                    "refused": "WRONG_SHAPE",
                    "why": "'%s' is not HERON-<AREA>-<ABBR>-<NNN>, the "
                           "register's house style." % text}
        known = FRAG.registry_agents()
        if not known:
            return {"ok": True, "kind": kind, "name": text,
                    "checked": "shape only",
                    "why": "'%s' has the right shape. docs/28 could not be "
                           "read, so whether it EXISTS is unknown - and an "
                           "unreadable register is a different problem from "
                           "a wrong name." % text}
        if text not in known:
            return {"ok": False, "kind": kind, "name": text,
                    "refused": "NOT_IN_THE_REGISTER",
                    "why": "'%s' has the right shape and is not one of the "
                           "%d agents docs/28 declares. A well-formed id "
                           "nobody assigned is the one that reads as "
                           "correct in a review." % (text, len(known))}
        return {"ok": True, "kind": kind, "name": text,
                "checked": "shape and the register",
                "why": "'%s' is in docs/28, read from it rather than from a "
                       "list kept here." % text}

    if kind == "fragment-id":
        if not FRAG.ID_PATTERN.match(text):
            return {"ok": False, "kind": kind, "name": text,
                    "refused": "WRONG_SHAPE",
                    "why": "'%s' is not FRG-<AREA>-<NNN>. docs/29 s127 says "
                           "an id carries the area and a number and nothing "
                           "else - never the name, never the kind, never "
                           "the version - and that rule is held by this "
                           "shape rather than by a second check, because a "
                           "shape that cannot express a version cannot "
                           "carry one." % text}
        area = text.split("-")[1]
        if area not in FRAG.AREAS:
            return {"ok": False, "kind": kind, "name": text,
                    "refused": "NOT_A_KNOWN_AREA",
                    "why": "'%s' is not one of the %d areas: %s. An "
                           "unlisted area is an error rather than a guess - "
                           "the moment it is open, one fragment says MEP "
                           "and the next says MECH and neither search finds "
                           "both."
                           % (area, len(FRAG.AREAS),
                              ", ".join(sorted(FRAG.AREAS)))}
        return {"ok": True, "kind": kind, "name": text, "area": area,
                "checked": "shape and the area list",
                "why": "'%s' is %s (%s), read from heron_fragment.AREAS."
                       % (text, area, FRAG.AREAS[area])}

    if kind == "capability":
        if not FRAG.CAPABILITY_PATTERN.match(text):
            return {"ok": False, "kind": kind, "name": text,
                    "refused": "WRONG_SHAPE",
                    "why": "'%s' is not SCREAMING_SNAKE_CASE. docs/29 s128: "
                           "a capability is a thing the system can DO, so "
                           "it reads as one, and it is the searchable, "
                           "renameable name." % text}
        return {"ok": True, "kind": kind, "name": text,
                "folder": folder_for(text),
                "checked": "shape",
                "why": "'%s' is well formed, and its folder must be '%s'. "
                       "Whether the first word is a VERB is not checked: "
                       "nothing here knows English, and a list of verbs "
                       "kept in this file would be a rule this agent "
                       "invented." % (text, folder_for(text))}

    if kind == "fragment-folder":
        if of is None:
            return {"ok": False, "kind": kind, "name": text,
                    "refused": "NOTHING_TO_DERIVE_FROM",
                    "why": "a fragment folder is DERIVED from its "
                           "capability (docs/29 s130), so it cannot be "
                           "checked on its own. Hand in the capability."}
        wanted = folder_for(of)
        if text != wanted:
            return {"ok": False, "kind": kind, "name": text,
                    "refused": "FOLDER_IS_NOT_DERIVED",
                    "why": "'%s' is not '%s', which is what '%s' lower "
                           "cased with hyphens gives. docs/29 s130 says the "
                           "folder is derived, NEVER invented - so a "
                           "fragment can be found from its capability and "
                           "cannot be quietly misfiled."
                           % (text, wanted, of)}
        return {"ok": True, "kind": kind, "name": text, "of": of,
                "checked": "derivation from the capability",
                "why": "'%s' is exactly what '%s' derives to." % (text, of)}

    # module or suite - the two observed rules.
    shape, where, what = ((MODULE_SHAPE, "brain", "heron_<name>.py")
                          if kind == "module" else
                          (SUITE_SHAPE, "tests", "test_<name>.py"))
    seen = _observed(where, shape)
    if not shape.match(text):
        return {"ok": False, "kind": kind, "name": text,
                "refused": "UNLIKE_EVERY_OTHER",
                "why": "'%s' is not %s. %s"
                       % (text, what,
                          "Nothing states this rule and nothing enforces "
                          "it; %s"
                          % ("%s/ could not be read, so it is not known "
                             "whether anything else follows it." % where
                             if seen is None else
                             "%d of the %d files in %s/ follow it. That "
                             "makes it what everything else does, which is "
                             "a weaker claim than a rule somebody wrote, "
                             "and it is reported as the weaker one."
                             % (seen[0], seen[1], where)))}
    return {"ok": True, "kind": kind, "name": text,
            "checked": "observed, not stated",
            "observed": seen,
            "why": "'%s' is %s. %s"
                   % (text, what,
                      "%s/ could not be read." % where if seen is None else
                      "So are %d of the %d files in %s/ - nothing states "
                      "this and nothing enforces it, so it is reported as "
                      "OBSERVED rather than as a rule."
                      % (seen[0], seen[1], where))}


def main(argv):
    print("NAMING VALIDATION   it holds no rule of its own")
    print("=" * 72)

    print("\nwhere each rule is read from")
    for kind, source in (
            ("agent-id", "docs/28, via heron_fragment.registry_agents()"),
            ("fragment-id", "heron_fragment.ID_PATTERN and .AREAS"),
            ("capability", "heron_fragment.CAPABILITY_PATTERN"),
            ("fragment-folder", "docs/29 s130 - derived from the capability"),
            ("module", "observed in brain/, stated nowhere"),
            ("suite", "observed in tests/, stated nowhere"),
            ("generated-name", "docs/29 - the generated name, D-79")):
        print("  %-16s %s" % (kind, source))

    print("\nchecked")
    for kind, name, of in (
            ("agent-id", "HERON-NAM-VAL-002", None),
            ("agent-id", "HERON-NAM-ZZZ-999", None),
            ("agent-id", "heron-naming", None),
            ("fragment-id", "FRG-SEL-001", None),
            ("fragment-id", "FRG-MECH-001", None),
            ("capability", "FILTER_ELEMENTS_BY_CATEGORY", None),
            ("fragment-folder", "filter-elements-by-category",
             "FILTER_ELEMENTS_BY_CATEGORY"),
            ("fragment-folder", "category-filter",
             "FILTER_ELEMENTS_BY_CATEGORY"),
            ("module", "heron_naming.py", None),
            ("module", "naming.py", None),
            ("generated-name", "mep-duct-insulation-check-revit-fitting-v1",
             None),
            ("generated-name", "MEP-Duct-Sizing-v1", None),
            ("generated-name", "mep-duct-v1", None)):
        answer = check(kind, name, of=of)
        mark = "ok     " if answer.get("ok") else answer["refused"]
        print("  %-22s %-42s %s" % (mark, name, answer["why"][:40]))

    print("\ngenerated")
    for parts in ((u"MEP", u"Duct Insulation", u"check", u"Revit",
                   u"fitting", 1),
                  (u"mep", u"", u"check", u"revit", u"fitting", 1),
                  (u"mep", u"duct", u"check", u"revit", u"fitting", u"latest")):
        made = generate(*parts)
        print("  %-22s %s" % (made.get("refused", "ok"),
                              made.get("name") or made["why"][:52]))
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
