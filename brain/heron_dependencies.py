# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-DEP-005
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Dependencies - confirm, never automatic, and degrade only out loud.

    python brain/heron_dependencies.py

WHAT IT IS FOR (docs/28, HERON-INS-DEP-005)
--------------------------------------------
"Checks and installs dependencies. **Confirm, never automatic**." T1, risk
ADMIN. docs/07 s9 says the same in one row: missing dependency, install,
confirm.

THE RULE THIS AGENT EXISTS TO KEEP IS "DEGRADE SILENTLY BUT SAY SO"
---------------------------------------------------------------------
Heron degrades rather than breaks when an optional package is absent, and
that is right. The failure is the second half. `requirements-optional.txt`
gives every entry three fields:

    # <import name> | <what Heron uses it for> | <what happens without it>

and all five entries fill in all three. So the sentence a person needs
already exists, written down, for every optional dependency. What has gone
wrong (PROPOSALS F7) is that the sentence never reaches them: `sqlite_vec`
falls back to comparing vectors in Python and nothing says so, so Heron
gets slower and stays that way, and nobody knows there is anything to
install.

That is why this agent will not accept an optional dependency that does
not say what is LOST without it. Not because the field might be missing -
it is not, today - but because an optional dependency with no stated cost
is the exact shape the failure takes, and the day somebody adds the sixth
entry in a hurry is the day it comes back.

REQUIRED OR OPTIONAL IS DECLARED, NEVER GUESSED
-------------------------------------------------
The two get opposite treatment - one stops Heron, the other is the normal
case and costs nothing to report - so reading the wrong one is not a small
error. A dependency arriving without a kind is refused rather than
defaulted, in either direction: defaulting to required turns a normal
install into a failure, and defaulting to optional turns a failure into
silence, which is worse.

WHAT IS INSTALLED IS ASKED, NOT STATED
----------------------------------------
"Installed" here means importable, and only the running interpreter knows.
A caller that can state the list is a caller that can make Heron report a
backend it does not have. So it is a reader, and with no reader - or one
that raises - nothing can be judged at all and the answer says so. It does
not report a healthy system it could not see.

INSTALLING IS A SECOND DECISION, PER PACKAGE
----------------------------------------------
Looking needs no permission. Installing does, one package at a time:
"confirm, never automatic" is not one confirmation for a list somebody
scrolled past. And the consent must name the package, for the same reason
a flag approval must name the flag.

AND A pip install IS NOT CHECKED THE WAY A HERON PACKAGE IS
------------------------------------------------------------
HERON-INS-SUP-013 refuses a community fragment with no approval record,
because installing it runs somebody else's code. `pip install model2vec`
also runs somebody else's code, and there is no register, no approver and
no hash on that path. This agent says so in every answer rather than
letting the quieter route look like the safer one. It is not a refusal -
Heron needs these packages - but it is not nothing either, and a person
approving one should be told which of the two gates they are standing at.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_flags as FLG                                      # noqa: E402

KINDS = ("required", "optional")

# The three fields requirements-optional.txt already gives every entry.
# `lost` is the one PROPOSALS F7 is about.
DECLARES = (("uses", "what Heron uses it for"),
            ("lost", "what happens without it"))


def _installed(importable, names):
    """
    (found, why) - which of these are importable, asked rather than stated.

    None when nothing could look. A caller that can STATE the list is a
    caller that can make Heron report a backend it does not have.
    """
    if importable is None:
        return None, ("no reader was given, so nothing looked at what is "
                      "actually importable. An answer built from the list "
                      "alone would describe a machine nobody checked")
    if not callable(importable):
        return None, ("a %s was passed where a reader belongs. Only the "
                      "running interpreter knows what imports"
                      % type(importable).__name__)
    try:
        found = importable(list(names))
    except Exception as failure:                     # noqa: BLE001
        return None, ("the reader raised %s, so what is installed is "
                      "unknown - which is not the same as nothing missing"
                      % type(failure).__name__)
    if not isinstance(found, (list, tuple, set)):
        return None, ("the reader answered %s rather than a list of what "
                      "imports" % type(found).__name__)
    return set(str(name).strip() for name in found), "asked the interpreter"


def review(dependencies, importable=None):
    """
    {required_missing, degraded, say, why} - what is here and what is not.

    Looking needs no permission. Every missing OPTIONAL dependency comes
    back with the sentence that has to be said about it, because the
    sentence existing in a file is not the same as it reaching a person.
    """
    if not isinstance(dependencies, (list, tuple)) or not dependencies:
        return {"refused": "NO_DEPENDENCY_LIST",
                "why": "nothing said what Heron depends on. An empty list "
                       "reads as a system with no dependencies, and this "
                       "one has six."}

    entries = {}
    for index, entry in enumerate(dependencies):
        if not isinstance(entry, dict) or not str(
                entry.get("name") or "").strip():
            return {"refused": "NO_DEPENDENCY_LIST",
                    "why": "entry %d does not name a package. A list with a "
                           "hole in it is not a list." % (index + 1)}
        name = str(entry["name"]).strip()
        kind = str(entry.get("kind") or "").strip().lower()
        if kind not in KINDS:
            return {"refused": "KIND_NOT_DECLARED", "package": name,
                    "why": "%s does not say whether it is required or "
                           "optional, and the two get opposite treatment. "
                           "Defaulting to required turns a normal install "
                           "into a failure; defaulting to optional turns a "
                           "failure into silence, which is worse." % name}
        if kind == "optional":
            missing = [what for field, what in DECLARES
                       if not str(entry.get(field) or "").strip()]
            if missing:
                return {"refused": "WHAT_IS_LOST_NOT_SAID", "package": name,
                        "wants": missing,
                        "why": "%s is optional and does not say %s. An "
                               "optional dependency with no stated cost is "
                               "how Heron ends up permanently on a weaker "
                               "path with nobody told - PROPOSALS F7, and "
                               "requirements-optional.txt already asks for "
                               "this field." % (name, "; ".join(missing))}
        entries[name] = dict(entry, kind=kind)

    here, how = _installed(importable, sorted(entries))
    if here is None:
        return {"refused": "CANNOT_SEE_WHAT_IS_INSTALLED", "why": how,
                "proposal": "pass a reader that imports each name and "
                            "answers which succeeded. Reporting a healthy "
                            "system nobody looked at is the one outcome "
                            "worse than saying so."}

    required_missing, degraded, present = [], [], []
    for name in sorted(entries):
        entry = entries[name]
        if name in here:
            present.append(name)
        elif entry["kind"] == "required":
            required_missing.append({"package": name,
                                     "uses": entry.get("uses"),
                                     "why": "REQUIRED: %s"
                                            % (entry.get("lost")
                                               or "nothing says what breaks, "
                                                  "which is allowed for a "
                                                  "required package only "
                                                  "because it breaks "
                                                  "everything")})
        else:
            degraded.append({"package": name, "uses": entry.get("uses"),
                             "lost": entry["lost"],
                             "say": "Heron is running without %s: %s. It "
                                    "still works - %s - and `pip install "
                                    "--user %s` gets the better path."
                                    % (name, entry["lost"],
                                       entry.get("uses"), name)})

    return {
        "required_missing": required_missing, "degraded": degraded,
        "present": present,
        "say": [entry["say"] for entry in degraded],
        "why": "%d of %d importable. %d required missing, %d running "
               "degraded. %s."
               % (len(present), len(entries), len(required_missing),
                  len(degraded), how),
        "unjudged": [
            "NOTHING MISSING IS A REAL ANSWER HERE, not a refusal. A "
            "dependency check that found everything is the outcome the "
            "check exists to confirm - unlike a repair run nobody asked "
            "for (HERON-OPS-HEA-006), which is a different thing.",
            "'installed' means IMPORTABLE and nothing more. Whether the "
            "version is the right one is not asked - nothing in this "
            "repository has been measured against an older release, so a "
            "floor would be a guess wearing a number.",
            "%s"
            % ("every degraded path above has a sentence in `say`, and this "
               "agent's job ends when it is returned. PROPOSALS F7 is "
               "exactly the gap between a sentence existing and a person "
               "reading it." if degraded else
               "nothing is running degraded, so `say` is empty - which is "
               "different from a degraded path with nothing to say about "
               "it."),
        ],
    }


def approve(package, origin=None, consent=None, installed=None):
    """
    {install, why} - may this one be installed? Per package, every time.

    Looking needs no permission; installing does. "Confirm, never
    automatic" is not one confirmation for a list somebody scrolled past.
    """
    name = str((package or {}).get("name") or "").strip() \
        if isinstance(package, dict) else str(package or "").strip()
    if not name:
        return {"install": False, "refused": "NO_DEPENDENCY_LIST",
                "why": "nothing was named."}

    if name in set(str(one).strip() for one in (installed or [])):
        return {"install": False, "refused": "NOT_MISSING",
                "why": "%s already imports. Installing over it is a change "
                       "nobody asked for, and 'it was already there' is a "
                       "better answer than a reinstall that looks like "
                       "progress." % name}

    allowed, why_origin = FLG.origin_allowed(origin, "installing a package")
    if not allowed:
        return {"install": False, "refused": "NOT_FROM_THE_USER",
                "why": why_origin,
                "proposal": "ask the user. Installing a package runs "
                            "somebody else's code on their machine."}

    given = consent if isinstance(consent, dict) else None
    if not given or not str(given.get("by") or "").strip():
        return {"install": False, "refused": "NOT_CONSENTED",
                "why": "docs/28 gives this agent one instruction in three "
                       "words - confirm, never automatic - and nobody "
                       "confirmed."}
    if str(given.get("package") or "").strip() != name:
        return {"install": False, "refused": "NOT_CONSENTED",
                "why": "the consent is for %s and this is %s. One "
                       "confirmation for a list somebody scrolled past is "
                       "not 'confirm' in any sense that protects them."
                       % (given.get("package") or "no package", name)}

    return {
        "install": True, "package": name, "by": str(given["by"]).strip(),
        "command": "pip install --user %s" % name,
        "why": "%s confirmed %s specifically. The command is returned, not "
               "run." % (str(given["by"]).strip(), name),
        "unjudged": [
            "THIS IS NOT THE GATE A HERON PACKAGE GOES THROUGH. "
            "HERON-INS-SUP-013 refuses a community fragment with no "
            "approval record because installing it runs somebody else's "
            "code; `pip install %s` runs somebody else's code too, with no "
            "register, no approver and no hash on that path. Not a refusal "
            "- Heron needs these - but the person approving it should know "
            "which of the two gates they are standing at." % name,
            "nothing was installed. The command is text, for a process "
            "somebody started on purpose.",
        ],
    }


def main(argv):
    print("DEPENDENCIES   confirm, never automatic, and degrade out loud")
    print("=" * 72)

    dependencies = [
        {"name": "yaml", "kind": "required",
         "uses": "reading fragment.yaml, the skills, and every report",
         "lost": "nothing in brain/ or tools/ runs"},
        {"name": "model2vec", "kind": "optional",
         "uses": "the trained embedding backend, so retrieval matches on "
                 "meaning",
         "lost": "retrieval falls back to character n-grams"},
        {"name": "sqlite_vec", "kind": "optional",
         "uses": "vector search inside SQLite instead of in Python",
         "lost": "vectors are compared in Python - slower, same answers"},
        {"name": "pypdf", "kind": "optional",
         "uses": "reading PDF documents into the knowledge store",
         "lost": "PDFs cannot be ingested"},
    ]

    answer = review(dependencies, importable=lambda names: ["yaml", "pypdf"])
    print("  %s" % answer["why"])
    print()
    print("  AND THIS IS THE PART THAT GOES MISSING (PROPOSALS F7):")
    for line in answer["say"]:
        print("    - %s" % line[:110])

    print()
    print("  It refuses what would make that silence possible:")
    for label, broken in (
            ("no kind declared", [{"name": "x", "uses": "u", "lost": "l"}]),
            ("kind is 'maybe'", [{"name": "x", "kind": "maybe"}]),
            ("optional, no cost stated",
             [{"name": "x", "kind": "optional", "uses": "u"}]),
            ("optional, no use stated",
             [{"name": "x", "kind": "optional", "lost": "l"}]),
            ("an entry with no name", [{"kind": "optional"}]),
            ("no list at all", [])):
        print("    %-26s %s"
              % (label, review(broken,
                               importable=lambda names: [])["refused"]))
    for label, reader in (("no reader", None), ("a list, not a reader", []),
                          ("a reader that raises", lambda names: 1 / 0),
                          ("a reader answering a string",
                           lambda names: "yaml")):
        print("    %-26s %s"
              % (label, review(dependencies,
                               importable=reader)["refused"]))

    print()
    print("  Installing is a second decision, per package:")
    for label, kw in (
            ("nothing signed", {}),
            ("a document asked", {"origin": "a document Heron read",
                                  "consent": {"by": "ajmal",
                                              "package": "model2vec"}}),
            ("consent for another", {"origin": "user",
                                     "consent": {"by": "ajmal",
                                                 "package": "pypdf"}}),
            ("already importable", {"origin": "user", "installed": ["model2vec"],
                                    "consent": {"by": "ajmal",
                                                "package": "model2vec"}})):
        print("    %-26s %s"
              % (label, approve({"name": "model2vec"},
                                **kw).get("refused")))
    answer = approve({"name": "model2vec"}, origin="user",
                     consent={"by": "ajmal", "package": "model2vec"})
    print("    %-26s %s" % ("confirmed by name", answer["command"]))
    print()
    print("  %s" % answer["unjudged"][0][:200])
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
