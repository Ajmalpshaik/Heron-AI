# Heron-Agent:  HERON-SKL-VAL-004
# Heron-Step:   14
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
A skill: what the user can ASK FOR, in their own words.

    python brain/heron_skill.py            validate them, and list the gaps

Step 14 of docs/27-build-order.md.

SKILL, FRAGMENT, RECIPE (D-29)
------------------------------
  Skill     what the user asks for, in BIM language. "Select all the ducts."
  Fragment  a composable piece of the how, with a declared contract
  Recipe    a bespoke multi-stage job that genuinely cannot be composed

A SKILL NAMES CAPABILITIES, NEVER FRAGMENTS
-------------------------------------------
This is Step 12's whole purpose reaching its customer. A skill says it needs
FILTER_ELEMENTS_BY_CATEGORY; it never says FRG-ELE-001. So a fragment can be
improved, replaced, split into three or retired, and not one skill is edited.

Which means a skill can be written BEFORE the fragment that will serve it -
and that is not a loophole, it is the point. A skill whose capability nobody
provides is exactly the capability gap docs/18 wants visible, and writing the
skills first is how the gaps get FOUND rather than guessed at.

WHAT A SKILL HERE IS NOT
------------------------
It is not proven because it is written. Every skill starts DRAFT and stays
there until something has watched it work against a real model - the same bar
D-30 sets for fragments, for the same reason. `python tools/check-gaps.py`
counts them honestly.
"""

import io
import os
import sys

try:
    import yaml
except ImportError:                                          # pragma: no cover
    sys.stderr.write("Heron's brain needs PyYAML: pip install --user pyyaml\n")
    raise

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_fragment as FRAG                                 # noqa: E402

SKILLS_DIR = os.path.join(ROOT, "brain", "skills")

REQUIRED = ("heron-status", "heron-step", "heron-since", "heron-layer",
            "id", "name", "domain", "purpose", "utterances", "needs",
            "preconditions", "risk", "revit")

# docs/09 section 2's [NOTE] asked for preconditions alongside utterances:
# "preconditions let Heron fail early with a useful message instead of failing
# deep inside a transaction". Required for the same reason utterances are - a
# skill that cannot say what must be true before it runs will discover it the
# expensive way.


class Skill(object):
    def __init__(self, data, path):
        self.data = data
        self.path = path

    @property
    def id(self):
        return self.data.get("id")

    @property
    def name(self):
        return self.data.get("name")

    @property
    def status(self):
        return self.data.get("heron-status")

    def utterances(self):
        return list(self.data.get("utterances", []) or [])

    def needs(self):
        """The capabilities this skill requires. Never fragment ids."""
        return list(self.data.get("needs", []) or [])

    def __repr__(self):
        return "<Skill %s %s>" % (self.id, self.status)


def load(path):
    data = yaml.safe_load(io.open(path, encoding="utf-8").read())
    if not isinstance(data, dict):
        raise ValueError("%s is not a mapping" % path)
    return Skill(data, path)


def load_all(root=None):
    root = root or SKILLS_DIR
    found, problems = {}, []
    if not os.path.isdir(root):
        return found, problems

    for name in sorted(os.listdir(root)):
        if not name.endswith((".yaml", ".yml")):
            continue
        try:
            skill = load(os.path.join(root, name))
        except (ValueError, yaml.YAMLError) as exc:
            problems.append("%s: %s" % (name, exc))
            continue
        if not skill.id:
            problems.append("%s: no id" % name)
            continue
        if skill.id in found:
            problems.append("%s: id %s already used" % (name, skill.id))
            continue
        found[skill.id] = skill
    return found, problems


def validate(skill):
    problems = []
    where = os.path.basename(skill.path)

    for field in REQUIRED:
        if skill.data.get(field) in (None, "", [], {}):
            problems.append("%s: missing %s" % (where, field))

    if skill.status and skill.status not in FRAG.STATUSES:
        problems.append("%s: heron-status %r is not a lifecycle state"
                        % (where, skill.status))

    if skill.data.get("risk") and skill.data["risk"] not in (
            "READ", "ANALYZE", "SUGGEST", "EXECUTE", "MODIFY", "PUBLISH",
            "ADMIN"):
        problems.append("%s: risk %r is not a permission level"
                        % (where, skill.data["risk"]))

    said = skill.data.get("utterances")
    if said is not None and (not isinstance(said, list) or len(said) < 2):
        problems.append(
            "%s: a skill needs at least TWO utterances. One is a name; two is "
            "the beginning of knowing how somebody actually asks" % where)

    for capability in skill.needs():
        if not isinstance(capability, str) or not capability.isupper():
            problems.append(
                "%s: needs must be CAPABILITY names in SCREAMING_SNAKE, not %r. "
                "A skill that names a fragment defeats the registry"
                % (where, capability))

    unknown = [v for v in (skill.data.get("revit") or [])
               if str(v) not in FRAG.REVIT_VERSIONS]
    if unknown:
        problems.append("%s: revit names releases Heron does not know: %s"
                        % (where, ", ".join(str(u) for u in unknown)))
    return problems


def main():
    found, problems = load_all()
    print("Skills in %s" % os.path.relpath(SKILLS_DIR, ROOT))
    print()

    if not found and not problems:
        print("None yet.")
        return 0

    fragments, _ = FRAG.load_all()
    provided = set(f.data.get("capability") for f in fragments.values())

    wanted = {}
    for skill in sorted(found.values(), key=lambda s: s.id):
        broken = validate(skill)
        problems.extend(broken)
        missing = [c for c in skill.needs() if c not in provided]
        mark = "FAIL" if broken else ("gap" if missing else "ok")
        print("  %-5s %-26s %-46s %s"
              % (mark, skill.id, skill.name,
                 "needs %d capability(ies)" % len(skill.needs())))
        for capability in missing:
            wanted.setdefault(capability, []).append(skill.id)

    print()
    print("  %d skill(s), all DRAFT until something watches them work" % len(found))

    if wanted:
        print()
        print("  CAPABILITY GAPS - what these skills need and nothing provides:")
        for capability in sorted(wanted):
            print("    %-34s wanted by %s"
                  % (capability, ", ".join(sorted(wanted[capability]))))
        print()
        print("  That list is the point of writing skills first. It is what to")
        print("  build next, in the order real work asks for it - not a guess.")

    if problems:
        print()
        for line in problems:
            print("  %s" % line)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
