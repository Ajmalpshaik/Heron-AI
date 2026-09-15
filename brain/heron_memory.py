# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-USR-MEM-002
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
User memory - never-stored is checked first, and it is a refusal.

    python brain/heron_memory.py

WHAT IT IS FOR (docs/28, HERON-USR-MEM-002)
--------------------------------------------
"Decides what is worth remembering, what expires, WHAT MUST NEVER BE
STORED, and when an old preference is superseded. The judgement is the
job." T2, risk MODIFY. It writes nothing: a decision comes back and a
caller carries it out.

NEVER-STORED COMES FIRST, AND IT IS A REFUSAL RATHER THAN A FILTER
--------------------------------------------------------------------
Constitution article 17: "Secrets live in the credential store, nowhere
else. Never write an API key, token or credential into a fragment,
skill, prompt, source file, log or commit. NEVER INCLUDE ONE IN A
RESULT."

So a candidate carrying one is refused WHOLE. It is not stored with the
secret taken out, for two reasons: the rest of a sentence built around a
credential usually means nothing without it, and a redact-and-store
habit teaches a system that credentials are handleable. The answer says
where it belongs instead - the credential store, through a handle.

The shapes are HERON-KRN-SEC-0xx's, called rather than copied:
`heron_secrets.Secrets().redact()` already knows six, and a second list
here would be a second thing to keep current while the first one moved.

SUPERSESSION REPLACES AND RECORDS - IT DOES NOT SIT BESIDE
------------------------------------------------------------
docs/10 s276, in the document's own words:

    "New knowledge that contradicts old knowledge REPLACES it and
    records the replacement. It does not sit beside it."

Both halves are the rule. Replacing without recording loses the fact
that somebody changed their mind, which is exactly the thing worth
knowing the next time they seem to contradict themselves. So a
superseding decision NAMES what it replaces, and the suite checks that
the old value survives in the record rather than only in a count -
Golden Rule 14.

EXPIRY IS APPLIED, NOT INVENTED
---------------------------------
docs/10 s276 again: "Temporary memory has a TTL. Project memory is
archived when the project closes." So a temporary candidate must ARRIVE
with a TTL and one without is refused; a project candidate is recorded
as archiving when that project closes. No duration is chosen here,
because choosing one would be inventing the policy rather than applying
it.

"WORTH REMEMBERING" IS EVIDENCE, NOT A CUTOFF
-----------------------------------------------
The register calls the judgement the job, and the honest part of that
judgement is refusing to invent a number. HERON-LRN-ANA-002's row says
it in the neighbouring department: "frequency and corroboration, NOT
NOVELTY". So each decision carries how many times the thing was seen and
over what window, and nothing is refused for being seen too few times -
a caller reading "seen once, on one day" needs no threshold from here.

What IS refused is a candidate with no evidence at all: `seen` absent is
not `seen: 1`, and treating it as one would be the invention this avoids.
"""

from __future__ import annotations

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_scope as SCOPE  # noqa: E402
import heron_secrets as SECRETS  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# docs/10 s276's two expiry rules, and the scopes they attach to. Read from
# HERON-RAG-LIB-001's list rather than retyped.
NEEDS_A_TTL = SCOPE.TEMPORARY
ARCHIVES_ON_CLOSE = SCOPE.PROJECT

# What a candidate must carry before anything can be said about it.
A_CANDIDATE_CARRIES = (
    ("about", "what it is ABOUT - the key supersession replaces on. A "
              "memory nobody can name can never be replaced, so it would "
              "accumulate forever beside its own contradictions, which is "
              "the thing docs/10 s276 exists to stop."),
    ("what", "the thing itself"),
    ("scope", "where it would live, so its expiry rule is known"),
    ("seen", "how many times it was observed, so the evidence travels"),
)


def _secret_in(text):
    """Which secret shapes are in this text, through the agent that knows."""
    _clean, found = SECRETS.Secrets().redact(str(text))
    return found


def decide(candidates, remembered=None, closing=None):
    """
    {store, supersede, refused_names, why, unjudged} - or a refusal.

    Nothing is written. `remembered` is what is already held, handed in.
    """
    if not candidates:
        return {"stored": False, "refused": "NOTHING_TO_DECIDE",
                "why": "no candidates were handed in. An empty run deciding "
                       "that nothing is worth remembering is a statement "
                       "about the call rather than about the user."}

    held = {}
    for entry in (remembered or []):
        if isinstance(entry, dict) and str(entry.get("about") or "").strip():
            held[str(entry["about"]).strip().lower()] = entry

    store, supersede, refused = [], [], []
    for candidate in candidates:
        if not isinstance(candidate, dict):
            refused.append({"candidate": repr(candidate)[:50],
                            "refused": "NOT_A_CANDIDATE",
                            "why": "each candidate carries %s."
                                   % ", ".join(name for name, _why
                                               in A_CANDIDATE_CARRIES)})
            continue

        what = candidate.get("what")
        scope = str(candidate.get("scope") or "").strip().lower()
        seen = candidate.get("seen")
        about = str(candidate.get("about") or "").strip()

        missing = [name for name, _why in A_CANDIDATE_CARRIES
                   if candidate.get(name) in (None, "")]
        if missing:
            refused.append({"about": about or None,
                            "refused": "NOT_A_CANDIDATE",
                            "why": "carries no %s. %s"
                                   % (", ".join(missing),
                                      " ".join(why for name, why
                                               in A_CANDIDATE_CARRIES
                                               if name in missing))})
            continue

        # FIRST, AND BEFORE ANYTHING IS DECIDED ABOUT IT - article 17.
        leaking = _secret_in(what)
        if leaking:
            refused.append({"about": about or None,
                            "refused": "NEVER_STORED",
                            "shapes": leaking,
                            "why": "it carries %s. Constitution article 17: "
                                   "secrets live in the credential store, "
                                   "nowhere else, and never in a result. "
                                   "Refused WHOLE rather than stored with "
                                   "the secret taken out - the rest of a "
                                   "sentence built around a credential "
                                   "usually means nothing without it, and "
                                   "redact-and-store teaches a system that "
                                   "credentials are handleable. It belongs "
                                   "in the credential store, reached "
                                   "through a handle."
                                   % ", ".join("a %s" % shape
                                               for shape in leaking)})
            continue

        if scope not in SCOPE.SCOPES:
            refused.append({"about": about or None,
                            "refused": "NOT_A_SCOPE",
                            "why": "'%s' is not a scope. Known: %s - read "
                                   "from HERON-RAG-LIB-001 rather than "
                                   "listed here, and the scope is what says "
                                   "which expiry rule applies."
                                   % (scope, ", ".join(SCOPE.SCOPES))})
            continue

        # docs/10 s276. The rule is APPLIED; no duration is chosen here.
        expiry = None
        if scope == NEEDS_A_TTL:
            ttl = str(candidate.get("ttl") or "").strip()
            if not ttl:
                refused.append({"about": about or None,
                                "refused": "NO_TTL",
                                "why": "temporary memory has a TTL "
                                       "(docs/10 s276) and this arrived "
                                       "without one. A duration is not "
                                       "chosen here: choosing one would be "
                                       "inventing the policy rather than "
                                       "applying it, and temporary memory "
                                       "with no expiry is permanent memory "
                                       "under another name."})
                continue
            expiry = {"rule": "ttl", "ttl": ttl}
        elif scope == ARCHIVES_ON_CLOSE:
            expiry = {"rule": "archived when the project closes",
                      "project": str(candidate.get("project") or "").strip()
                      or None}

        evidence = {"seen": seen, "window": candidate.get("window")}
        decision = {"about": about or None, "what": what, "scope": scope,
                    "expiry": expiry, "evidence": evidence,
                    "why": "%s, %s."
                           % ("seen %s time(s)%s" % (
                               seen, " over %s" % candidate["window"]
                               if candidate.get("window") else ""),
                              expiry["rule"] if expiry
                              else "no expiry rule attaches to '%s'" % scope)}

        older = held.get(about.lower())
        if older is not None and str(older.get("what")) != str(what):
            supersede.append(dict(
                decision,
                replaces={"what": older.get("what"),
                          "since": older.get("since"),
                          "source": older.get("source")},
                why="replaces what was held about '%s'. docs/10 s276: new "
                    "knowledge that contradicts old REPLACES it AND RECORDS "
                    "the replacement - it does not sit beside it. The old "
                    "value is in `replaces` rather than only in a count, "
                    "because somebody changing their mind is the thing "
                    "worth knowing next time they seem to contradict "
                    "themselves." % about))
            continue

        store.append(decision)

    landed = len(store) + len(supersede) + len(refused)
    return {
        "stored": False, "store": store, "supersede": supersede,
        "refused_names": refused, "of": len(candidates),
        "why": "%d candidate(s): %d to store, %d superseding, %d refused. "
               "Nothing was written."
               % (len(candidates), len(store), len(supersede), len(refused)),
        "unjudged": [
            "NOTHING WAS WRITTEN. A decision comes back and a caller "
            "carries it out; what is already held was HANDED IN, not looked "
            "up.",
            "EVERY CANDIDATE LANDS IN EXACTLY ONE LIST (%d of %d)."
            % (landed, len(candidates)),
            "NOTHING WAS REFUSED FOR BEING SEEN TOO FEW TIMES. That would "
            "need a cutoff and every cutoff is invented - "
            "HERON-LRN-ANA-002's row says frequency and corroboration, not "
            "novelty, and the frequency travels in `evidence` so a caller "
            "reading 'seen once, on one day' needs no number from here.",
            "WHETHER ANY OF IT IS WORTH REMEMBERING IS STILL SOMEBODY'S "
            "CALL. What was decided here is narrower and answerable: that "
            "it carries no credential, that its scope has an expiry rule, "
            "and whether it replaces something already held.",
        ],
    }


def main(argv):
    print("USER MEMORY   never-stored is checked first, and it is a refusal")
    print("=" * 72)

    answer = decide(
        [{"about": "duct insulation", "what": "25 mm in this tower",
          "scope": "project", "project": "Tower A", "seen": 4,
          "window": "three weeks"},
         {"about": "units", "what": "millimetres", "scope": "user",
          "seen": 12, "window": "since June"},
         {"about": "api key", "what": "use " + SECRETS.FAKE_FORGE_TOKEN,
          "scope": "user", "seen": 1},
         {"about": "scratch", "what": "last selection", "scope": "temporary",
          "seen": 1},
         {"about": "scratch", "what": "last selection", "scope": "temporary",
          "seen": 1, "ttl": "1 hour"},
         {"about": "units", "what": "metres", "scope": "user", "seen": 2}],
        remembered=[{"about": "units", "what": "millimetres",
                     "since": "2026-06-01", "source": "Ajmal"}])

    print("\n%s" % answer["why"])
    for row in answer["store"]:
        print("  store      %-16s %-22s %s"
              % (row["about"], str(row["what"])[:22], row["why"][:34]))
    for row in answer["supersede"]:
        print("  SUPERSEDE  %-16s %-22s was: %s"
              % (row["about"], str(row["what"])[:22],
                 row["replaces"]["what"]))
    for row in answer["refused_names"]:
        print("  REFUSED    %-16s %s"
              % (row.get("about") or "?", row["refused"]))

    print("\nwhat this agent does not judge")
    for line in answer["unjudged"]:
        print("  - %s" % line)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
