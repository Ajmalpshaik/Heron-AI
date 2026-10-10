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

import copy
import hashlib
import io
import os
import re
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

    @property
    def purpose(self):
        """The method, in the skill's own words - for a family, every step in
        the order it has to happen."""
        return self.data.get("purpose") or ""

    @property
    def risk(self):
        return self.data.get("risk")

    def preconditions(self):
        return list(self.data.get("preconditions", []) or [])

    def __repr__(self):
        return "<Skill %s %s>" % (self.id, self.status)


def load(path):
    """Read one skill file. Every failure leaves as a ValueError (D-48).

    The reason is load_all's, not this function's: it catches so that one
    unreadable skill costs one skill. An IOError escaping from here walked
    straight out of load_all and took the whole library with it - measured on
    2026-09-06 with a DIRECTORY named `x.yaml`, which load_all happily passed
    to io.open because it filtered on the extension and never asked whether the
    entry was a file. Two skills beside it, and it returned neither.

    THE PARSE IS KEPT WHILE THE BYTES ARE THE SAME, as heron_fragment.load()
    keeps a card's: heron_lookup asks which skills a request belongs to on
    every call, and parsing every skill each time is pure-Python YAML for
    nothing. The mark is heron_fragment.file_mark()'s, so an unchanged file is
    not even opened; a hit is a deep copy, so no caller changes the next read.
    """
    try:
        mark, raw = FRAG.file_mark(path)
        held = _PARSED.get(path)
        if held is not None and held[0] == mark:
            return Skill(copy.deepcopy(held[1]), path)
        if raw is None:
            with io.open(path, "rb") as handle:
                raw = handle.read()
            mark = hashlib.blake2b(raw, digest_size=16).digest()
        data = yaml.safe_load(raw.decode("utf-8"))
    except yaml.YAMLError as exc:
        raise ValueError("could not be parsed - %s" % " ".join(str(exc).split()))
    except UnicodeDecodeError as exc:
        raise ValueError("could not be read - %s" % " ".join(str(exc).split()))
    except (IOError, OSError) as exc:
        raise ValueError("could not be read - %s" % " ".join(str(exc).split()))
    if not isinstance(data, dict):
        raise ValueError("%s is not a mapping" % path)
    _PARSED[path] = (mark, copy.deepcopy(data))
    return Skill(data, path)


#: path -> (blake2b of the file's bytes, parsed mapping). See load().
_PARSED = {}


def load_all(root=None):
    root = root or SKILLS_DIR
    found, problems = {}, []
    if not os.path.isdir(root):
        return found, problems

    for name in sorted(os.listdir(root)):
        if not name.endswith((".yaml", ".yml")):
            continue
        path = os.path.join(root, name)
        if not os.path.isfile(path):
            # A directory named `x.yaml` is not a skill. This is checked rather
            # than assumed because the extension filter above cannot tell.
            problems.append("%s: not a file, so it is not a skill" % name)
            continue
        try:
            skill = load(path)
        except (ValueError, yaml.YAMLError) as exc:
            problems.append("%s: %s" % (name, exc))
            continue
        except Exception as exc:                     # noqa: BLE001 - deliberate
            # D-48: one broken skill costs one skill. A bare except is normally
            # a smell; here it IS the guarantee, and it is commented so it is
            # not tidied away by someone who reads it as laziness.
            problems.append("%s: unreadable - %s: %s"
                            % (name, type(exc).__name__,
                               " ".join(str(exc).split())))
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

    # THE RULE THIS FILE STATES IN CAPITALS, AND `isupper()` WAS NOT IT.
    #
    # "A SKILL NAMES CAPABILITIES, NEVER FRAGMENTS" is the whole reason a
    # fragment can be improved, replaced, split into three or retired with no
    # skill edited - and the test behind the refusal below was
    # `capability.isupper()`, which `FRG-ELE-001` passes. So `needs:
    # [FRG-ELE-001]` validated CLEAN, and `main()` then listed it under
    # CAPABILITY GAPS - the list it calls "what to build next, in the order
    # real work asks for it - not a guess". Naming a fragment produced an
    # instruction to go and build a capability called FRG-ELE-001.
    # FRAGMENT-ISSUES row 5b-106.
    #
    # THE PATTERN IS THE FRAGMENT SIDE'S OWN, imported rather than written
    # again here, so the two halves cannot come to disagree about what a
    # capability name looks like - which is how a rule ends up enforced in one
    # place and not the other. Measured before it was tightened: it accepts
    # 396 of 396 capabilities in the library and 0 of 396 fragment ids, every
    # one of which carries hyphens, and it refuses none of the requirements
    # the ten skills already declare.
    for capability in skill.needs():
        if (not isinstance(capability, str)
                or not FRAG.CAPABILITY_PATTERN.match(capability)):
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


# ---------------------------------------------------------------------------
# Which jobs a request is one step of
# ---------------------------------------------------------------------------

# Words every request has and no job is told apart by. Asking to MAKE or
# CREATE something is the request, not the subject of it.
_PLAIN = frozenset((
    "a an the this that these those it its my our your me i we you to of for "
    "in on at with from and or all please can could would will be is are do "
    "does make create build new add put get one two three four five six "
    "seven eight nine ten").split())


def _words(text):
    """The words of `text` that can tell one job from another, lower case and
    singular - "families" is "family", "ducts" is "duct", "glass" stays."""
    found = []
    for word in re.findall(r"[a-z0-9]+", (text or "").lower()):
        if len(word) > 4 and word.endswith("ies"):
            word = word[:-3] + "y"
        elif len(word) > 3 and word.endswith("s") and not word.endswith("ss"):
            word = word[:-1]
        if word not in _PLAIN:
            found.append(word)
    return found


def methods_for(skills, request, capabilities, limit=2):
    """The skills `request` is one step of, best first - at most `limit`.

    WHY THIS EXISTS. A family is not one capability, it is a method of fifteen
    steps in an order that matters, and that method lives in a skill's
    `purpose`. heron_lookup answered "create a family" with ONE capability -
    CREATE_FAMILY_SWEPT_BLEND, a ranked guess - and nothing a tool returned
    named the method. A chat with no memory of Heron then read the skill file
    and docs/43 off disk to work it out: minutes on a new PC, where the old one
    remembered.

    WHAT LINKS A SKILL TO A REQUEST IS DATA, NOT A GUESS ABOUT MEANING. A
    skill is offered only when it NEEDS one of `capabilities` - what retrieval
    already returned - and shares at least one word with the request in its
    name or its utterances. The words only ORDER what that link allows: the
    most shared words first, then how often they recur in the skill's own
    phrasing - which puts the general family method ahead of the special ones
    it sends to - then the most of the request's capabilities it uses. That
    last is the weakest sign, because a ranked guess's runners-up are where
    retrieval is noisiest. The host still decides what the user meant (D-01);
    this names the methods so it does not have to read them off disk.
    """
    asked = set(_words(request))
    wanted = [c for c in capabilities if c]
    if not asked or not wanted:
        return []
    ranked = []
    for skill in skills:
        needs = set(skill.needs())
        linked = len([c for c in set(wanted) if c in needs])
        if not linked:
            continue
        said = _words(" ".join([skill.name or "", (skill.id or "").replace(
            "-", " ")] + [str(u) for u in skill.utterances()]))
        shared = asked.intersection(said)
        if not shared:
            continue
        recur = len([w for w in said if w in shared])
        ranked.append(((-len(shared), -recur, -linked, skill.id or ""), skill))
    ranked.sort(key=lambda pair: pair[0])
    return [skill for _key, skill in ranked[:limit]]


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
