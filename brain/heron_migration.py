# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-WSP-MIG-008
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Workspace migration - the chain is continuous, or nothing runs.

    python brain/heron_migration.py

WHAT IT IS FOR (docs/28, HERON-WSP-MIG-008)
--------------------------------------------
"Schema and layout migrations. **Idempotent, versioned, backed up**." T1,
risk ADMIN. Those three words are docs/07 s8's, and HERON-OPS-UPD-010
already refuses a release whose migrations do not declare them - so this
imports that list rather than writing a second one.

"VERSIONED" IS THE ONE THAT MAKES THE OTHER TWO REAL
------------------------------------------------------
docs/07 s8 asks for three things and they are not three independent
wishes. **The data records the schema version it was written with**, and
that recorded version is the only thing that can answer "has this already
run?" - which is what idempotent means in practice rather than in a
declaration.

So a workspace that does not record its schema version is refused. Not
because the number is interesting, but because without it every
migration is a guess about whether it has run before, and a migration
run twice on data that cannot tell is exactly the case the word
idempotent was supposed to cover.

THE CHAIN MUST BE CONTINUOUS, AND A GAP IS REFUSED RATHER THAN JUMPED
----------------------------------------------------------------------
Migrations go 1 to 2, 2 to 3, 3 to 4. If the workspace is at 1 and the
target is 4, all three must exist. A missing 2-to-3 is not something to
step over by running 1-to-2 and then 3-to-4: the data arriving at 3
never went through the transformation that defines what 3 means, and the
result is a workspace that reports schema 4 and holds something no
version of the code has ever seen.

That failure is silent and permanent, which is why this refuses the whole
run rather than doing the part that works. A partial migration leaves the
data somewhere no migration can start from.

BACKED UP BEFORE, NOT AFTER
-----------------------------
docs/07 s7 rule 4: back up the data class before migrating it. Fragments,
skills and memory are irreplaceable, and a backup taken after a migration
that went wrong is a copy of the damage.

IT MIGRATES NOTHING
---------------------
It returns the ordered chain and what each step needs. Running them is
somebody else's, in a process started on purpose - and ordering is the
part that is easy to get wrong silently, which is why it is the part
this agent does.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

import heron_paths as PATHS                                    # noqa: E402
from heron_update import A_MIGRATION_DECLARES                  # noqa: E402


def _version(value):
    """An integer schema version, or None for anything else."""
    try:
        found = int(str(value).strip())
    except (TypeError, ValueError):
        return None
    return found if found >= 0 else None


def plan(at=None, to=None, migrations=None, backup=None):
    """
    {chain, why} - the ordered migrations, or a refusal. Nothing runs.

    `at` is the schema version the WORKSPACE records about itself, and
    the whole design rests on it being recorded rather than guessed.
    """
    here = _version(at)
    if here is None:
        return {"migrated": False, "refused": "DATA_VERSION_UNKNOWN",
                "why": "the workspace records no schema version (%r). "
                       "docs/07 s8 asks for versioned data, and not because "
                       "the number is interesting: it is the only thing "
                       "that can answer 'has this already run?', which is "
                       "what idempotent means in practice rather than in a "
                       "declaration." % at,
                "proposal": "write the schema version into the data before "
                            "migrating it. A migration run twice on data "
                            "that cannot tell is the exact case the word "
                            "idempotent was supposed to cover."}

    target = _version(to)
    if target is None:
        return {"migrated": False, "refused": "NO_TARGET",
                "why": "nothing says which schema version to reach (%r). "
                       "A migration with no destination is a transformation "
                       "nobody can check afterwards." % to}

    if target == here:
        return {"migrated": False, "chain": [], "at": here, "to": target,
                "why": "the workspace is already at schema %d. Nothing to "
                       "do, and that is a real answer - unlike an empty "
                       "request." % here,
                "unjudged": ["whether schema %d is the RIGHT one is not "
                             "this agent's question" % here]}

    if target < here:
        return {"migrated": False, "refused": "WOULD_GO_BACKWARDS",
                "why": "the workspace is at schema %d and the target is %d. "
                       "Going back is a RESTORE from a backup, not a "
                       "migration - the forward transformation threw "
                       "information away and running it in reverse invents "
                       "what it threw." % (here, target),
                "proposal": "restore the backup taken before %d, which is "
                            "what docs/07 s7 rule 4 exists for."
                            % (here,)}

    # THE CHAIN. Indexed by the version each migration starts FROM, so a
    # gap is a missing key rather than something to step over.
    steps, thin, forked = {}, [], []
    for index, migration in enumerate(migrations or []):
        if not isinstance(migration, dict):
            thin.append((index + 1, ["it is not a record at all"]))
            continue
        missing = [what for field, what in A_MIGRATION_DECLARES
                   if not migration.get(field)]
        if missing:
            thin.append((index + 1, missing))
            continue
        starts = _version(migration.get("from"))
        ends = _version(migration.get("to"))
        if starts is None or ends is None or ends != starts + 1:
            thin.append((index + 1, ["a migration goes from one version to "
                                     "the next: from=%r to=%r is not a step"
                                     % (migration.get("from"),
                                        migration.get("to"))]))
            continue
        if starts in steps:
            # TWO MIGRATIONS OUT OF ONE VERSION. The later one used to
            # replace the earlier in this dict and the chain still
            # reported complete, so the run silently dropped a
            # transformation and nothing said which. Which of the two is
            # current is a question about the data, not one this agent
            # may answer by taking whichever arrived last.
            forked.append({"from": starts, "to": ends,
                           "migrations": [steps[starts].get("name")
                                          or "migration %d" % (index,),
                                          migration.get("name")
                                          or "migration %d" % (index + 1,)]})
            continue
        steps[starts] = migration

    if forked:
        return {"migrated": False, "refused": "CHAIN_FORKS",
                "forks": forked, "at": here, "to": target,
                "why": "%s. A version with two migrations out of it is not "
                       "a chain, and taking whichever arrived last would "
                       "run one transformation, skip the other, and report "
                       "the chain complete - silent, and by then the data "
                       "has been through neither or only half."
                       % "; ".join("%d migrations start at schema %d (%s)"
                                   % (len(one["migrations"]), one["from"],
                                      ", ".join(str(name) for name
                                                in one["migrations"]))
                                   for one in forked),
                "proposal": "keep one migration per version and delete or "
                            "renumber the other. Which is current is a "
                            "question about the data."}

    if thin:
        return {"migrated": False, "refused": "MIGRATION_NOT_DECLARED",
                "missing": [{"migration": index, "wants": wants}
                            for index, wants in thin],
                "why": "docs/07 s8: a migration is idempotent, versioned "
                       "and reversible-or-backed-up, and it goes from one "
                       "version to the next. %d do not say they are all of "
                       "that. The three conditions are HERON-OPS-UPD-010's "
                       "own list, imported rather than restated."
                       % len(thin)}

    chain, missing = [], []
    for version in range(here, target):
        if version not in steps:
            missing.append("%d to %d" % (version, version + 1))
            continue
        chain.append({"from": version, "to": version + 1,
                      "migration": steps[version]})

    if missing:
        return {"migrated": False, "refused": "CHAIN_IS_BROKEN",
                "missing_steps": missing, "at": here, "to": target,
                "why": "no migration exists for %s. This refuses the WHOLE "
                       "run rather than doing the part that works: data "
                       "arriving at a version it never went through the "
                       "transformation for is a workspace reporting schema "
                       "%d and holding something no version of the code has "
                       "ever seen - silent, and permanent."
                       % (", ".join(missing), target),
                "proposal": "write the missing migration. A partial run "
                            "leaves the data somewhere no migration can "
                            "start from."}

    # BACKED UP BEFORE, NOT AFTER.
    where = str(backup or "").strip()
    if not where:
        return {"migrated": False, "refused": "NO_BACKUP",
                "chain_length": len(chain),
                "why": "docs/07 s7 rule 4: back up the data class before "
                       "migrating it, and nothing says where the backup is. "
                       "%d step(s) would run over fragments, skills and "
                       "memory - the half nothing regenerates." % len(chain)}
    if PATHS.classify(where)["class"] != PATHS.DATA:
        return {"migrated": False, "refused": "NO_BACKUP",
                "why": "the backup is at %s, which is %s and not the data "
                       "class. A backup living where an update replaces "
                       "things wholesale is a backup that is gone the next "
                       "time one runs."
                       % (where, PATHS.classify(where)["class"])}

    return {
        "migrated": False, "chain": chain, "at": here, "to": target,
        "backup": where,
        "why": "%d migration(s), %d to %d, in order, each declaring all "
               "three of docs/07 s8's conditions, with a backup at %s. "
               "Nothing ran." % (len(chain), here, target, where),
        "unjudged": [
            "NOTHING RAN. The ordered chain comes back and a caller runs "
            "it - ordering is the part that is easy to get wrong silently, "
            "which is why it is the part this agent does.",
            "IDEMPOTENT IS DECLARED, NOT DEMONSTRATED. Each step says "
            "running it twice is safe; nothing here has run any of them "
            "twice and compared. The recorded schema version is what makes "
            "the claim checkable at all, which is why a workspace without "
            "one is refused outright.",
            "the backup was checked for CLASS, not for contents. That it "
            "lives in the data class is verifiable from the path; that it "
            "actually holds this workspace is not, and this agent does not "
            "open it.",
        ],
    }


def main(argv):
    print("WORKSPACE MIGRATION   the chain is continuous, or nothing runs")
    print("=" * 72)

    def step(low):
        return {"from": low, "to": low + 1, "idempotent": True,
                "version": str(low + 1), "reversible": "backed up",
                "what": "schema %d to %d" % (low, low + 1)}

    every = [step(1), step(2), step(3)]
    answer = plan(at=1, to=4, migrations=every, backup="Backup/pre-4")
    print("  %s" % answer["why"])
    for entry in answer["chain"]:
        print("    %d -> %d   %s" % (entry["from"], entry["to"],
                                     entry["migration"]["what"]))

    print()
    print("  A GAP is refused rather than jumped:")
    answer = plan(at=1, to=4, migrations=[step(1), step(3)],
                  backup="Backup/pre-4")
    print("    %s - missing %s" % (answer["refused"],
                                   ", ".join(answer["missing_steps"])))
    print("    %s" % answer["why"][60:200])

    print()
    print("  Every other way it refuses:")
    cases = [
        ("no recorded version", dict(at=None)),
        ("a version that is a word", dict(at="two")),
        ("no target", dict(to=None)),
        ("target behind the data", dict(at=4, to=1)),
        ("a migration declaring nothing", dict(migrations=[{"from": 1,
                                                            "to": 2}])),
        ("a migration that skips", dict(migrations=[dict(step(1), to=3)])),
        ("no backup", dict(backup="")),
        ("backup in the product class", dict(backup="Core/pre-4")),
    ]
    for label, override in cases:
        settings = {"at": 1, "to": 2, "migrations": [step(1)],
                    "backup": "Backup/pre-2"}
        settings.update(override)
        print("    %-30s %s" % (label, plan(**settings)["refused"]))

    print()
    print("  And already there is a real answer, not a refusal:")
    answer = plan(at=4, to=4, migrations=every, backup="Backup/x")
    print("    %s" % answer["why"])

    print()
    print("  A missing 2-to-3 is not something to step over. Data arriving")
    print("  at 3 that never went through what DEFINES 3 is a workspace")
    print("  reporting schema 4 and holding something no version of the")
    print("  code has ever seen - silent, and permanent.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
