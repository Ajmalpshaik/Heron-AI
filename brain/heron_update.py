# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-OPS-UPD-010
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Update - seven rules, and every one of them is a way to say no.

    python brain/heron_update.py

WHAT IT IS FOR (docs/28, HERON-OPS-UPD-010)
--------------------------------------------
"Detects, downloads, migrates, validates, rolls back." T1, risk ADMIN.

docs/07 s7 gives seven update rules and they are not advice - each one is a
specific way a tool that writes to live client models ruins somebody's
week. This module is those seven rules as seven refusals, plus docs/07 s8's
three conditions on a migration.

  1. Never auto-update without consent      NOT_CONSENTED, NOT_FROM_THE_USER
  2. Never update while Revit has unsaved work   REVIT_MAY_HAVE_UNSAVED_WORK
  3. The add-in update needs a Revit restart     said, never reported as done
  4. Back up the data class before migrating it  NO_BACKUP
  5. Rollback must be TESTED, not implemented    ROLLBACK_NOT_TESTED
  6. Never touch the data class in a product update  TOUCHES_THE_DATA_CLASS
  7. Version pinning - "not now" means not asked again   PINNED

RULE 5 IS THE ONE WORTH READING TWICE
---------------------------------------
"Rollback must be tested, not merely implemented. An untested rollback path
is not a rollback path." That is the same shape as D-30 and D-39: a claim
is not evidence. So a release saying `rollback: true` is REFUSED here. What
gets past is a recorded rollback test - which version it went back to, when,
and what it was verified against - because the day a rollback is needed is
the day nobody has time to find out it never worked.

RULE 2 IS A READER, NOT A VALUE
---------------------------------
"Never update while Revit is open with unsaved work. Check first." A caller
that can state "nothing is unsaved" is a caller that can overwrite somebody's
morning, and it will be the convenient answer every time. So it is asked at
the moment of the decision, and with no reader, a reader that raises, or a
reader that cannot tell, THE ANSWER IS THAT THERE MAY BE UNSAVED WORK. The
scheduler settled this shape for "is a person working"; the cost of being
wrong here is higher.

RULE 3 IS A REPORTING RULE, WHICH IS WHY IT HAS NO REFUSAL
------------------------------------------------------------
Assemblies loaded into Revit cannot be unloaded, so an add-in update has not
taken effect until Revit restarts. The rule is "say so plainly; do not
report success for something that has not taken effect in memory" - so the
answer carries `takes_effect` and the add-in never comes back as done. A
refusal would be wrong; a cheerful "updated!" would be a lie.

WHAT IT DOES NOT DO
--------------------
It downloads nothing, writes nothing, migrates nothing and restarts nothing.
It reads a release's own description of itself and says whether the seven
rules are satisfied - and what is missing when they are not. Every "yes" it
gives is still only a yes to PROCEEDING.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_flags as FLG                                      # noqa: E402
import heron_paths as PATHS                                    # noqa: E402

# docs/06 s2's split lives in HERON-WSP-PTH-007 and is ASKED FOR here.
# This module carried its own copy until 2026-09-14, and so did three
# other agents: four copies of one rule is four places for it to drift,
# and the drift would be silent because each would still pass its own
# tests. Re-exported under the old names so a caller that reads them
# reads the one table.
PRODUCT = dict(PATHS.FOLDERS)[PATHS.PRODUCT]
DATA = dict(PATHS.FOLDERS)[PATHS.DATA]
DERIVED = dict(PATHS.FOLDERS)[PATHS.DERIVED]

# docs/07 s7 rule 3. Loaded into Revit and not unloadable.
NEEDS_A_RESTART = ("revit", "add-in", "addin")

# docs/07 s8. Every migration declares all three or it is not a migration.
A_MIGRATION_DECLARES = (
    ("idempotent", "that running it twice is safe"),
    ("version", "the schema version the data is written with"),
    ("reversible", "that it can be undone, or that a backup stands in"),
)


def _classify(component):
    """
    product / data / derived / unknown, for one named component.

    HERON-WSP-PTH-007's answer, not a second opinion. It tests DATA first
    on purpose - "Brain agents" contains a word from both lists, and the
    two wrong answers are not the same size: calling a product component
    data delays an update, and calling a data component product overwrites
    a modeller's fragment library.
    """
    return PATHS.classify(component)["class"]


def _unsaved(revit):
    """
    (answer, how) - might Revit be holding unsaved work?

    True when nobody could tell us. docs/07 s7 rule 2 says check first, and
    a caller that can simply state the answer is a caller that can overwrite
    somebody's morning with a value.
    """
    if revit is None:
        return True, ("no reader was given, so nothing looked at Revit. "
                      "docs/07 s7 rule 2 says check first, and 'nobody "
                      "checked' is not a check")
    if not callable(revit):
        return True, ("a %s was passed where a reader belongs. A caller "
                      "that can STATE that nothing is unsaved is a caller "
                      "that can overwrite somebody's morning, and it is the "
                      "convenient answer every time"
                      % type(revit).__name__)
    try:
        answer = revit()
    except Exception as failure:                     # noqa: BLE001
        return True, ("the reader raised %s. Something is wrong with the one "
                      "check that protects unsaved work, and the safe "
                      "reading of that is yes" % type(failure).__name__)
    if answer is None:
        return True, "the reader could not tell, which is not a no"
    return bool(answer), ("Revit reports unsaved work" if answer else
                          "Revit reports nothing unsaved")


def _migrations(release):
    """(refusal, why) - does every migration declare docs/07 s8's three?"""
    thin = []
    for index, migration in enumerate(release.get("migrations") or []):
        if not isinstance(migration, dict):
            thin.append((index + 1, ["it is not a record at all"]))
            continue
        missing = [what for field, what in A_MIGRATION_DECLARES
                   if not migration.get(field)]
        if missing:
            thin.append((index + 1, missing))
    if not thin:
        return None, None
    return "MIGRATION_NOT_DECLARED", [
        {"migration": index, "wants": wants} for index, wants in thin]


def plan(release, installed=None, pinned=None, revit=None, origin=None,
         consent=None):
    """
    {proceed, ...} - or one of the seven refusals, with what is missing.

    Nothing is downloaded, written, migrated or restarted. This reads a
    release's description of itself against docs/07 s7 and says whether
    proceeding is allowed.
    """
    if not isinstance(release, dict) or not str(
            release.get("version") or "").strip():
        return {"proceed": False, "refused": "NOT_AN_UPDATE",
                "why": "no release was described. A version, its components "
                       "and what it needs - an update nobody can read is not "
                       "an update anybody should run."}
    version = str(release["version"]).strip()
    installed = str(installed or "").strip()
    if version == installed:
        return {"proceed": False, "refused": "NOT_AN_UPDATE",
                "why": "%s is already installed. Nothing to do, and asking "
                       "would be the daily prompt rule 7 exists to stop."
                       % version}

    # RULE 7 FIRST, BECAUSE IT IS THE ONE ABOUT NOT ASKING. Checking consent
    # before the pin would mean prompting a user who already said not now.
    if str(pinned or "").strip():
        return {"proceed": False, "refused": "PINNED",
                "why": "this install is pinned to %s. A user mid-delivery "
                       "said 'not now', and rule 7 is that they stay pinned "
                       "WITHOUT being asked again every day - so this is "
                       "not a prompt, it is a stop." % pinned,
                "proposal": "nothing. The pin is the answer until the user "
                            "lifts it themselves."}

    # RULE 1. Consent, from the user, for THIS version.
    allowed, why_origin = FLG.origin_allowed(origin, "an update")
    if not allowed:
        return {"proceed": False, "refused": "NOT_FROM_THE_USER",
                "why": why_origin,
                "proposal": "ask the user. An update changes behaviour "
                            "mid-project, which rule 1 calls a liability "
                            "during a submission."}
    given = consent if isinstance(consent, dict) else None
    if not given or not str(given.get("by") or "").strip():
        return {"proceed": False, "refused": "NOT_CONSENTED",
                "why": "rule 1: never auto-update without consent. A tool "
                       "that silently changes behaviour mid-project is a "
                       "liability during a submission, and nobody said yes."}
    if str(given.get("version") or "").strip() != version:
        return {"proceed": False, "refused": "NOT_CONSENTED",
                "why": "the consent is for %s and this is %s. Consent to an "
                       "update is consent to a VERSION - the next one has "
                       "different components and different migrations."
                       % (given.get("version") or "no version", version)}

    # RULE 2. Asked, not assumed.
    unsaved, how = _unsaved(revit)
    if unsaved:
        return {"proceed": False, "refused": "REVIT_MAY_HAVE_UNSAVED_WORK",
                "why": "rule 2: never update while Revit is open with "
                       "unsaved work. %s." % how,
                "proposal": "ask the user to save and close Revit, then "
                            "check again - with something that can actually "
                            "see it."}

    # RULE 6. A product update does not touch the data class.
    touched = {}
    for component in (release.get("components") or []):
        touched.setdefault(_classify(component), []).append(component)
    # An unknown component cannot be shown NOT to be data, and rule 6 is
    # absolute. docs/06 s2 names every folder the architecture has, so a
    # release naming something else is naming something that does not exist.
    if touched.get("unknown"):
        return {"proceed": False, "refused": "COMPONENT_NOT_CLASSIFIED",
                "why": "%s %s not product, data or derived as docs/06 s2 "
                       "defines them, so nothing can show %s is not the "
                       "user's. Rule 6 is absolute, and an unclassified "
                       "component is not an exception to it - it is the "
                       "case the rule cannot see."
                       % (", ".join(touched["unknown"]),
                          "is" if len(touched["unknown"]) == 1 else "are",
                          "it" if len(touched["unknown"]) == 1 else "they"),
                "proposal": "name it as docs/06 s2 names it. If it is a real "
                            "component in none of the three classes, the "
                            "architecture is missing a row before the update "
                            "is missing a permission."}
    if touched.get("data"):
        return {"proceed": False, "refused": "TOUCHES_THE_DATA_CLASS",
                "why": "rule 6: never touch the data class during a product "
                       "update (docs/06 s2). %s belong%s to the user and "
                       "must survive every update, uninstall and reinstall."
                       % (", ".join(touched["data"]),
                          "" if len(touched["data"]) == 1 else "s"),
                "proposal": "a data-class change is a MIGRATION with a "
                            "backup, not a component of a product update. "
                            "They are different operations and rule 4 "
                            "applies to one of them."}

    # RULE 4. A migration without a backup is not allowed to start.
    if release.get("migrations") and not str(
            release.get("backup") or "").strip():
        return {"proceed": False, "refused": "NO_BACKUP",
                "why": "rule 4: back up the data class before migrating it. "
                       "%d migration%s and nothing says where the backup is. "
                       "Fragments, skills and memory are irreplaceable - "
                       "there is no second copy anywhere."
                       % (len(release["migrations"]),
                          "" if len(release["migrations"]) == 1 else "s")}
    refusal, missing = _migrations(release)
    if refusal:
        return {"proceed": False, "refused": refusal, "missing": missing,
                "why": "docs/07 s8: a migration is idempotent, versioned and "
                       "reversible-or-backed-up. %d do not say they are all "
                       "three, and a migration that has not said it is safe "
                       "to run twice will be run twice." % len(missing)}

    # RULE 5. Tested, not implemented.
    tested = release.get("rollback_tested")
    if not isinstance(tested, dict):
        return {"proceed": False, "refused": "ROLLBACK_NOT_TESTED",
                "why": "rule 5: rollback must be TESTED, not merely "
                       "implemented - an untested rollback path is not a "
                       "rollback path. %s is a claim, not a test."
                       % ("`rollback: %r`" % (release.get("rollback"),)
                          if release.get("rollback") is not None
                          else "Nothing says anything about rollback, which"),
                "proposal": "record the test: which version it went back to, "
                            "when, and what it was verified against. The day "
                            "a rollback is needed is the day nobody has time "
                            "to find out it never worked."}
    thin = [field for field in ("to", "at", "verified")
            if not str(tested.get(field) or "").strip()]
    if thin:
        return {"proceed": False, "refused": "ROLLBACK_NOT_TESTED",
                "why": "the rollback test does not say %s. A test nobody can "
                       "read back is the same evidence as no test."
                       % ", ".join(thin)}

    # RULE 3. Said plainly, and never reported as done.
    restarts = [component for component in (release.get("components") or [])
                if any(word in str(component).lower()
                       for word in NEEDS_A_RESTART)]

    return {
        "proceed": True, "version": version, "from": installed or None,
        "components": touched,
        "migrations": len(release.get("migrations") or []),
        "backup": release.get("backup"),
        "rollback_to": tested.get("to"),
        "takes_effect": ("after a Revit restart, for %s - assemblies loaded "
                         "into Revit cannot be unloaded"
                         % ", ".join(restarts)) if restarts else "immediately",
        "restart_required": bool(restarts),
        "why": "all seven rules in docs/07 s7 are satisfied for %s: %s "
               "consented to this version, %s, no data-class component, %d "
               "migration(s) with a backup at %s, and a rollback TESTED back "
               "to %s."
               % (version, str(given.get("by")).strip(), how,
                  len(release.get("migrations") or []),
                  release.get("backup") or "(none needed)", tested.get("to")),
        "unjudged": [
            "NOTHING HAS BEEN DOWNLOADED, WRITTEN OR MIGRATED. This is a yes "
            "to PROCEEDING, and every rule it checked was checked against "
            "the release's own description of itself.",
            "%s. Rule 3: do not report success for something that has not "
            "taken effect in memory, so %s"
            % (("the add-in is among the components"
                if restarts else "nothing here loads into Revit"),
               ("this agent will never report %s as updated - only as "
                "installed and waiting for a restart."
                % ", ".join(restarts)) if restarts else
               "there is nothing waiting on a restart."),
            "whether the release is AUTHENTIC is not checked here. docs/07 "
            "s1a is clear that an update fetches a signed release and never "
            "runs what it finds at a URL; the signature check belongs where "
            "the download happens, and this agent never sees the bytes.",
        ],
    }


def main(argv):
    print("UPDATE   seven rules, and every one of them is a way to say no")
    print("=" * 72)

    good = {
        "version": "0.4.0",
        "components": ["Core", "Agents", "Revit add-in", "MCP"],
        "migrations": [{"idempotent": True, "version": "4",
                        "reversible": "backed up"}],
        "backup": "Backup/2026-09-14-pre-0.4.0",
        "rollback_tested": {"to": "0.3.2", "at": "2026-09-12",
                            "verified": "a 0.4.0 install rolled back and the "
                                        "fragment library opened unchanged"},
    }
    consent = {"by": "ajmal", "version": "0.4.0", "at": "2026-09-14T12:00Z"}
    nothing_open = lambda: False                             # noqa: E731

    answer = plan(good, installed="0.3.2", origin="user", consent=consent,
                  revit=nothing_open)
    print("  %s" % answer["why"][:250])
    print()
    print("  takes effect: %s" % answer["takes_effect"])
    print("  And rule 3 is a REPORTING rule, so it has no refusal:")
    print("    %s" % answer["unjudged"][1][:150])

    print()
    print("  The seven rules, each shown refusing:")
    cases = [
        ("pinned mid-delivery", dict(pinned="0.3.2")),
        ("asked for by a document", dict(origin="a document Heron read")),
        ("nobody consented", dict(consent=None)),
        ("consent was for 0.3.9", dict(consent=dict(consent,
                                                    version="0.3.9"))),
        ("nothing looked at Revit", dict(revit=None)),
        ("Revit says unsaved work", dict(revit=lambda: True)),
        ("a reader that raises", dict(revit=lambda: 1 / 0)),
        ("it updates Fragments too",
         dict(release=dict(good, components=["Core", "Fragments"]))),
        ("migrating with no backup",
         dict(release=dict(good, backup=""))),
        ("a migration that says nothing",
         dict(release=dict(good, migrations=[{"idempotent": True}]))),
        ("rollback: true", dict(release=dict(good, rollback=True,
                                             rollback_tested=None))),
        ("a test with no verification",
         dict(release=dict(good, rollback_tested={"to": "0.3.2",
                                                  "at": "2026-09-12"}))),
        ("already installed", dict(installed="0.4.0")),
    ]
    for label, override in cases:
        release = override.pop("release", good)
        answer = plan(release, **dict({"installed": "0.3.2", "origin": "user",
                                       "consent": consent,
                                       "revit": nothing_open}, **override))
        print("    %-28s %s" % (label, answer["refused"]))

    print()
    print("  Rule 5 is the one worth reading twice. `rollback: true` is a")
    print("  CLAIM; what gets past is a recorded test - which version it")
    print("  went back to, when, and what it was verified against. The day")
    print("  a rollback is needed is the day nobody has time to find out")
    print("  it never worked.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
