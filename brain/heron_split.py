# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-FRG-SPL-003
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
Splitting - reuse justifies a split, tidiness does not.

    python brain/heron_split.py

WHAT IT IS FOR (docs/28, HERON-FRG-SPL-003)
--------------------------------------------
"Decomposes a compound fragment. Only when >=2 real consumers exist."
T3, risk SUGGEST. It proposes; it never applies.

docs/09 s120 GIVES THE RULE AND THE REASON FOR IT
---------------------------------------------------
    "Split has a real failure mode: over-decomposition. Ten fragments
    each three lines long, with nine layers of indirection, is worse
    than one clear fragment. Suggested guard: a fragment should only be
    split when the extracted part has AT LEAST TWO DISTINCT CONSUMERS,
    ACTUAL OR CLEARLY IMMINENT. Reuse justifies a split; tidiness does
    not."

Two distinct consumers is a COUNT, which is why it can be enforced at
all. "Ten fragments three lines long" is not a rule, it is a warning,
and turning it into a line count would be inventing the number this
project keeps refusing to invent.

"CLEARLY IMMINENT" IS THE HALF THAT WOULD BE WAVED THROUGH
------------------------------------------------------------
A consumer that exists is a fact. A consumer that is coming is a claim,
and a claim with nothing behind it makes the two-consumer rule mean one
consumer and an intention. So an imminent consumer must say WHAT makes
it imminent - D-33, asked once rather than assumed - and the answer
carries the reason back for a person to disbelieve.

The two kinds are counted together, because docs/09 says they count
together, and reported apart, because they are not the same evidence.

A SPLIT INTO ONE PART IS NOT A SPLIT
--------------------------------------
It is a rename, and HERON-NAM-REN-003 does those. Refused rather than
quietly accepted, because a one-part split that passes here is a way to
move a fragment without anything checking the move.

NOTHING AT PRODUCTION IS APPLIED, AND NOTHING HERE APPLIES ANYTHING
---------------------------------------------------------------------
docs/09 s118: "All three PROPOSE; none may apply autonomously to
anything at PRODUCTION. Splitting a production fragment silently changes
the behaviour of every skill that depends on it." This agent's risk is
SUGGEST and it writes nothing at all, so the first half is true of every
fragment here and the answer says which ones the second half binds.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_fragment as FRAG  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/09 s120's count. Two, because two is what it says.
ENOUGH = 2

# The state docs/09 s118 protects by name.
GUARDED = "PRODUCTION"

ACTUAL = "actual"
IMMINENT = "imminent"
KINDS = (ACTUAL, IMMINENT)

A_PART_CARRIES = (
    ("name", "what the extracted fragment would be called"),
    ("does", "what it would do on its own - a part nobody can describe "
             "without the whole is not a part"),
)


def propose(fragment, parts, consumers=None):
    """
    {proposed, parts, why} - or a refusal. Nothing is written or split.
    """
    if not fragment:
        return {"split": False, "refused": "NOTHING_TO_SPLIT",
                "why": "no fragment was handed in."}

    card = getattr(fragment, "data", fragment)
    if not isinstance(card, dict):
        return {"split": False, "refused": "NOT_A_FRAGMENT",
                "why": "%r is not a fragment card." % (card,)}
    who = str(getattr(fragment, "slug", None) or card.get("id") or "").strip()
    if not who:
        return {"split": False, "refused": "NOT_A_FRAGMENT",
                "why": "the fragment has no id, so nothing here can say "
                       "what is being split."}

    parts = list(parts or [])
    if not parts:
        return {"split": False, "refused": "NOTHING_TO_SPLIT",
                "why": "no parts were proposed. A split with no parts is "
                       "a question, not a proposal."}
    if len(parts) < 2:
        return {"split": False, "refused": "ONE_PART",
                "why": "'%s' would be split into one part, which is a "
                       "rename - HERON-NAM-REN-003 does those. Accepting "
                       "it here would be a way to move a fragment with "
                       "nothing checking the move." % who}

    named, shaped = [], []
    for part in parts:
        part = getattr(part, "data", part)
        if not isinstance(part, dict):
            return {"split": False, "refused": "NOT_A_PART",
                    "why": "%r is not a part. Each carries %s."
                           % (part, ", ".join(field for field, _
                                              in A_PART_CARRIES))}
        absent = [field for field, _ in A_PART_CARRIES
                  if not str(part.get(field) or "").strip()]
        if absent:
            return {"split": False, "refused": "NOT_A_PART",
                    "missing": absent,
                    "why": "a part is missing %s. %s"
                           % (", ".join(absent),
                              " ".join(why for field, why in A_PART_CARRIES
                                       if field in absent))}
        name = str(part["name"]).strip()
        if name in named:
            return {"split": False, "refused": "NOT_A_PART",
                    "why": "'%s' appears twice. Two parts under one name "
                           "means the consumers of one are the consumers "
                           "of neither." % name}
        named.append(name)
        shaped.append(part)

    book = {}
    for entry in (consumers or []):
        entry = getattr(entry, "data", entry)
        if not isinstance(entry, dict):
            return {"split": False, "refused": "NOT_A_CONSUMER",
                    "why": "%r is not a consumer. Each is {part, name, "
                           "kind} and, when the kind is '%s', `because`."
                           % (entry, IMMINENT)}
        kind = str(entry.get("kind") or ACTUAL).strip().lower()
        if kind not in KINDS:
            return {"split": False, "refused": "NOT_A_CONSUMER",
                    "why": "'%s' is neither %s. docs/09 s120 counts "
                           "'actual or clearly imminent' and nothing else."
                           % (kind, " nor ".join(KINDS))}
        name = str(entry.get("name") or "").strip()
        part = str(entry.get("part") or "").strip()
        if not name or not part:
            return {"split": False, "refused": "NOT_A_CONSUMER",
                    "why": "a consumer is missing its %s. Both are needed: "
                           "the count is of DISTINCT consumers, and "
                           "distinct needs a name."
                           % ("name" if not name else "part")}
        if part not in named:
            return {"split": False, "refused": "NOT_A_CONSUMER",
                    "why": "'%s' consumes '%s', which is not one of the "
                           "parts proposed: %s."
                           % (name, part, ", ".join(named))}
        # A CLAIM WITH NOTHING BEHIND IT makes two consumers mean one
        # consumer and an intention.
        because = str(entry.get("because") or "").strip()
        if kind == IMMINENT and not because:
            return {"split": False, "refused": "NOT_IMMINENT",
                    "consumer": name, "part": part,
                    "asked": "What makes '%s' imminent?" % name,
                    "why": "'%s' is claimed as a clearly imminent consumer "
                           "of '%s' and nothing says what makes it "
                           "imminent. A consumer that exists is a fact; "
                           "one that is coming is a claim, and an "
                           "unevidenced claim turns 'two consumers' into "
                           "one consumer and an intention."
                           % (name, part)}
        book.setdefault(part, []).append({"name": name, "kind": kind,
                                          "because": because or None})

    thin = []
    for name in named:
        mine = book.get(name) or []
        distinct = sorted(set(one["name"] for one in mine))
        if len(distinct) < ENOUGH:
            thin.append({"part": name, "consumers": distinct,
                         "have": len(distinct)})
    if thin:
        return {"split": False, "refused": "TOO_FEW_CONSUMERS",
                "parts": thin, "need": ENOUGH,
                "why": "%s. docs/09 s120: a fragment should only be split "
                       "when the extracted part has at least %d distinct "
                       "consumers, actual or clearly imminent - 'reuse "
                       "justifies a split; tidiness does not'."
                       % ("; ".join("'%s' has %d" % (one["part"],
                                                     one["have"])
                                    for one in thin), ENOUGH)}

    status = str(card.get("heron-status") or "").strip().upper()
    out = []
    for part in shaped:
        name = str(part["name"]).strip()
        mine = book[name]
        out.append({
            "name": name, "does": str(part["does"]).strip(),
            "consumers": sorted(set(one["name"] for one in mine)),
            "actual": sorted(set(one["name"] for one in mine
                                 if one["kind"] == ACTUAL)),
            "imminent": [{"name": one["name"], "because": one["because"]}
                         for one in mine if one["kind"] == IMMINENT]})

    everybody = sorted(set(one["name"] for mine in book.values()
                           for one in mine))
    return {
        "split": False, "proposed": True, "fragment": who,
        "status": status or None, "parts": out, "of": len(out),
        "consumers": everybody,
        "may_be_applied": status != GUARDED,
        "why": "'%s' into %d part(s), each with at least %d distinct "
               "consumer(s). Proposed, never applied%s."
               % (who, len(out), ENOUGH,
                  " - and '%s' is at %s, which docs/09 s118 says nothing "
                  "may change autonomously" % (who, GUARDED)
                  if status == GUARDED else ""),
        "unjudged": [
            "NOTHING WAS SPLIT. This agent's risk is SUGGEST and it writes "
            "nothing at all. docs/09 s118: all three of split, merge and "
            "evolution PROPOSE%s."
            % (", and none may apply autonomously to anything at %s - "
               "which '%s' is" % (GUARDED, who) if status == GUARDED
               else ""),
            "OVER-DECOMPOSITION. docs/09 s120 warns about ten fragments "
            "three lines long with nine layers of indirection, and that "
            "is a WARNING rather than a rule - turning it into a line "
            "count would invent the number. %d part(s) each cleared the "
            "two-consumer bar; whether that leaves something a person "
            "wants to read is theirs." % len(out),
            "%s" % ("%d CONSUMER(S) ARE CLAIMED AS IMMINENT RATHER THAN "
                    "ACTUAL: %s. Each said why, and the reason is carried "
                    "back for somebody to disbelieve - nothing here "
                    "checked whether any of them is really coming."
                    % (len([one for mine in book.values() for one in mine
                            if one["kind"] == IMMINENT]),
                       ", ".join(sorted(set(
                           one["name"] for mine in book.values()
                           for one in mine if one["kind"] == IMMINENT))))
                    if any(one["kind"] == IMMINENT for mine in book.values()
                           for one in mine) else
                    "every consumer counted here is an ACTUAL one; nothing "
                    "rests on a claim about the future."),
            "WHETHER THE PARTS ARE THE RIGHT PARTS. Each was described by "
            "whoever proposed it and counted by its consumers. Nothing "
            "here read the fragment's implementation.",
        ],
    }


def main(argv):
    print("SPLITTING   reuse justifies a split, tidiness does not")
    print("=" * 72)
    print("\ndocs/09 s120's bar: at least %d distinct consumer(s), %s"
          % (ENOUGH, " or clearly ".join(KINDS)))

    frag = {"id": "FRG-T-001", "heron-status": "PROVEN"}
    parts = [{"name": "filter-by-category", "does": "narrows to a category"},
             {"name": "count-them", "does": "counts what it is given"}]
    consumers = [
        {"part": "filter-by-category", "name": "count-elements",
         "kind": "actual"},
        {"part": "filter-by-category", "name": "select-elements",
         "kind": "actual"},
        {"part": "count-them", "name": "count-elements", "kind": "actual"},
        {"part": "count-them", "name": "size-breakdown", "kind": "imminent",
         "because": "its card already names COUNT_ELEMENTS in `needs`"},
    ]

    answer = propose(frag, parts, consumers)
    print("\n%s" % answer["why"])
    for one in answer["parts"]:
        print("  %-22s %d consumer(s): %s"
              % (one["name"], len(one["consumers"]),
                 ", ".join(one["consumers"])))

    print("\nrefused")
    for fragment, these, those in (
            (None, parts, consumers),
            ("a string", parts, consumers),
            (frag, [], consumers),
            (frag, parts[:1], consumers),
            (frag, ["a string"], consumers),
            (frag, parts + [{"name": "count-them", "does": "again"}],
             consumers),
            (frag, parts, [{"part": "count-them", "name": "x",
                            "kind": "hoped-for"}]),
            (frag, parts, [{"part": "nowhere", "name": "x"}]),
            (frag, parts, [{"part": "count-them", "name": "x",
                            "kind": "imminent"}]),
            (frag, parts, consumers[:2])):
        bad = propose(fragment, these, those)
        print("  %-22s %s" % (bad["refused"], bad["why"][:42]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
