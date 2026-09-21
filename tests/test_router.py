# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-MDL-010
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The model router - it chooses who chooses, and confidential never leaks.

    python tests/test_router.py

WHAT IT PROVES
  1. AN INTENT IS ROUTED TO AN ADAPTER, and the answer says why. docs/23 s8:
     Heron declares the intent, the adapter owns the model.

  2. NO MODEL ID APPEARS ANYWHERE IN THE ROUTER. Asserted against the source
     text, because the rule is about what may be written, not only about what
     runs. A named model in Heron is a provider hard-coded into a BIM tool.

  3. A CONFIDENTIAL SCOPE NEVER REACHES A CLOUD OR HOST ADAPTER - the one
     failure this module exists to prevent (Q-12, Article 16).

  4. AND WHEN NOTHING LOCAL CAN SERVE IT, THE ANSWER IS A REFUSAL rather than
     the reachable cloud adapter, with a reason a modeller can read.

  5. THE FLAG ONLY EVER REMOVES CANDIDATES. Confidential candidates are a
     subset of open ones for every intent - so a bug in whatever sets the flag
     cannot open a door.

  6. AN UNDECLARED INTENT IS REFUSED, not guessed at.

  7. A REFUSAL IS DATA, NEVER AN EXCEPTION. The caller's next move is to tell
     a modeller what is missing.

  8. WORK NEEDING REAL REASONING DOES NOT GO TO AN ADAPTER THAT DID NOT
     DECLARE IT.

  9. AN ADAPTER REGISTERED WITH A KIND OR AN INTENT NOBODY DECLARED IS
     REFUSED AT REGISTRATION.
"""

import io
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_router as ROUTER                                 # noqa: E402

FAILURES = []

# Model families and versioned model ids, in the shapes a caller would be
# tempted to write. None of these may appear in the router's source.
MODEL_NAMES = ("gpt-", "claude-3", "claude-4", "claude-opus", "claude-sonnet",
               "claude-haiku", "llama", "mistral", "gemini", "-turbo")


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def loaded():
    router = ROUTER.Router()
    router.register("claude-code", ROUTER.HOST, strong=True)
    router.register("on-machine", ROUTER.LOCAL,
                    intents=("CLASSIFY", "EXTRACT", "SCORE", "SUMMARISE"))
    router.register("a-cloud", ROUTER.CLOUD, strong=True)
    return router


def main():
    print("1. An intent is routed, and the answer says why")
    router = loaded()
    chosen = router.route("CLASSIFY")
    check(chosen.get("adapter") == "claude-code",
          "CLASSIFY routes to the first adapter that offers it")
    check("registered first" in chosen.get("reason", ""),
          "and the reason says how the choice was made")

    print()
    print("2. No model id is written anywhere in the router")
    source = io.open(os.path.join(ROOT, "brain", "heron_router.py"),
                     encoding="utf-8").read().lower()
    # The module's own name for the host adapter in its example is a product,
    # not a model - the test names the model shapes explicitly instead.
    found = [name for name in MODEL_NAMES if name in source]
    check(found == [], "no model family or version appears in the source%s"
          % (" - found %s" % ", ".join(found) if found else ""))

    print()
    print("3, 4 and 5. Confidential narrows, and never leaks")
    for intent in sorted(ROUTER.INTENTS):
        private = router.route(intent, confidential=True)
        if "adapter" in private:
            kinds = dict((n, k) for n, k in router.candidates(intent, True))
            check(all(k == ROUTER.LOCAL for k in kinds.values()),
                  "%s under confidential considers only on-machine adapters"
                  % intent)
        else:
            check(private["refused"] == "NO_CONFIDENTIAL_ADAPTER",
                  "%s under confidential is refused rather than sent out"
                  % intent)
            check("so the answer is no" in private["why"],
                  "and the refusal says why, in words a modeller can read")

        open_set = {n for n, _k in router.candidates(intent, False)}
        private_set = {n for n, _k in router.candidates(intent, True)}
        check(private_set <= open_set,
              "%s: the confidential candidates are a subset, never a superset"
              % intent)

    print()
    print("6 and 7. An undeclared intent is refused, as data")
    for asked in ("THINK_REALLY_HARD", "", "classify a duct"):
        answer = router.route(asked)
        check(answer.get("refused") == "UNKNOWN_INTENT",
              "'%s' is refused rather than guessed at" % asked)
    check(isinstance(router.route("NOPE"), dict),
          "a refusal comes back as data, not as an exception")

    print()
    print("8. Reasoning work needs an adapter that declared it")
    weak = ROUTER.Router()
    weak.register("cheap-and-local", ROUTER.LOCAL, strong=False)
    answer = weak.route("REASON")
    check(answer.get("refused") == "NO_ADAPTER_REGISTERED"
          and "reasoning it needs" in answer["why"],
          "REASON is refused when no adapter declared strong reasoning")
    check(weak.route("CLASSIFY").get("adapter") == "cheap-and-local",
          "and the same adapter still serves the work it can do")

    print()
    print("9. An adapter nobody can describe is refused at registration")
    bad = ROUTER.Router()
    for kind, intents, what in (("magic", None, "an undeclared kind"),
                                (ROUTER.LOCAL, ("VIBES",),
                                 "an undeclared intent")):
        refused = False
        try:
            bad.register("x", kind, intents=intents)
        except ValueError:
            refused = True
        check(refused, "%s is refused" % what)

    print()
    print("10. An empty router refuses rather than inventing a provider")
    check(ROUTER.Router().route("CLASSIFY").get("refused")
          == "NO_ADAPTER_REGISTERED",
          "with nothing registered, there is nobody to ask and it says so")

    print()
    print("11. The two public methods answer the same input the same way")
    #
    # Until 2026-09-21 they did not. route() upper-cased the intent and
    # candidates() did not, so route("classify") answered and
    # candidates("classify") raised a bare KeyError: 'classify' - from a
    # module whose own docstring says a refusal is data rather than an
    # exception "because an exception two layers down arrives there as a
    # stack trace". Row 5b-84.
    pair = ROUTER.Router()
    pair.register("claude-code", ROUTER.HOST, strong=True)

    check(pair.route("classify").get("adapter") == "claude-code",
          "route() takes a lower-case intent")
    try:
        same = pair.candidates("classify")
    except Exception as e:                                   # noqa: BLE001
        same = e
    check(same == [("claude-code", ROUTER.HOST)],
          "and candidates() takes it too, rather than raising on the "
          "spelling its sibling accepts")

    # An unknown intent RAISES here rather than returning [], and that is
    # deliberate: an empty list would read as "no adapter offers this" when
    # the truth is "that is not an intent", and this module exists to keep
    # those two apart.
    try:
        pair.candidates("nonsense")
        raised = None
    except ValueError as e:
        raised = e
    except Exception as e:                                   # noqa: BLE001
        raised = ("wrong type", e)
    check(isinstance(raised, ValueError),
          "an unknown intent raises ValueError, the way register() already "
          "does for the same mistake - not a bare KeyError")
    check(isinstance(raised, ValueError) and "not a declared intent" in str(raised)
          and "route()" in str(raised),
          "and the message names the mistake and points at the method that "
          "returns that refusal as data instead")

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    intent in, adapter out, and confidential stays on the machine")
    return 0


if __name__ == "__main__":
    sys.exit(main())
