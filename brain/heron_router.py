# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-KRN-MDL-010
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The Model Router - Heron declares the intent, never the model.

    python brain/heron_router.py        the five intents, and what they route to

THE SHAPE, FROM docs/23 s8
---------------------------
    Heron AI Interface -> Model Router -> Provider Adapter -> Model

The two layers were conflated until that section separated them, and the
separation is the whole design:

    "this task needs strong reasoning"   Heron. Always.
    "which model, from which provider"   the host when hosted; Heron's own
                                         adapter for its own batch work

That is what closes the tension D-11 recorded - a Heron-owned router
duplicating Claude Code, which already chooses the model. It does not choose a
model. It chooses who chooses.

SO NO MODEL ID APPEARS IN THIS FILE, and none may appear in any caller. A
module that asks for a named model has hard-coded a provider into Heron, and
the day that model is retired the failure surfaces in whatever BIM tool
happened to call it. Ask for CLASSIFY and let the adapter answer.

CONFIDENTIALITY NARROWS, IT NEVER WIDENS (Q-12, Article 16)
------------------------------------------------------------
A project marked confidential routes only to an adapter that keeps the work on
the machine. Nothing above this layer needs to know that happened - which is
the point of putting the decision here - but two rules make it safe:

  - a confidential request NEVER reaches a cloud or host adapter, and when no
    local adapter is registered the answer is a refusal naming what is
    missing. Falling back to a reachable cloud adapter is the one failure this
    module exists to prevent.
  - the flag only ever removes candidates. There is no value of it that adds
    one, so a bug in whatever sets it cannot open a door.

WHAT IT DOES NOT DO
--------------------
It does not call anything. It answers "who should do this", and the answer is
data. Whether that provider is actually reachable is a different question,
owned by the Model Availability & Fallback Agent (HERON-KRN-MAV-017); mixing
the two would mean a router that cannot be tested without a network.
"""

import sys

# The declared set. Small on purpose: an intent vocabulary long enough to need
# looking up is one where callers guess, and a guessed intent routes work to
# the wrong kind of thinking. Each says what it NEEDS, never who provides it.
INTENTS = {
    "CLASSIFY":  {"strong": False, "bulk": True,
                  "what": "put a thing in one of a known set of boxes"},
    "EXTRACT":   {"strong": False, "bulk": True,
                  "what": "pull stated facts out of text that contains them"},
    "SCORE":     {"strong": False, "bulk": True,
                  "what": "rank or rate against a stated measure"},
    "SUMMARISE": {"strong": False, "bulk": False,
                  "what": "say the same thing in fewer words"},
    "REASON":    {"strong": True, "bulk": False,
                  "what": "work something out that the text does not state"},
}

# Where an adapter sends the work. Only LOCAL keeps it on the machine.
LOCAL, HOST, CLOUD = "local", "host", "cloud"
KINDS = (LOCAL, HOST, CLOUD)


class Router(object):
    """Adapters in, a choice out. No network, no model names, no globals."""

    def __init__(self):
        self._adapters = []          # (name, kind, intents, strong)

    def register(self, name, kind, intents=None, strong=False):
        """
        Declare an adapter. `intents` limits it to some of the declared set;
        None means all of them. `strong` says it can serve work that needs
        real reasoning.
        """
        if kind not in KINDS:
            raise ValueError("adapter '%s' has kind '%s' - one of: %s"
                             % (name, kind, ", ".join(KINDS)))
        for intent in intents or ():
            if intent not in INTENTS:
                raise ValueError("adapter '%s' offers intent '%s', which is "
                                 "not declared" % (name, intent))
        self._adapters.append((name, kind, tuple(intents or ()), bool(strong)))
        return name

    def candidates(self, intent, confidential=False):
        """
        Every adapter that could serve this, in registration order.

        SAME SPELLING RULES AS route(), AND THAT IS A FIX RATHER THAN A
        DETAIL. Until 2026-09-21 this took the intent exactly as handed in
        while route() upper-cased it, so `route("classify")` answered and
        `candidates("classify")` raised a bare `KeyError: 'classify'` - two
        public methods on one class disagreeing about the same input.

        AN UNKNOWN INTENT RAISES, AND IT RAISES A SENTENCE. It cannot return
        [] the way route() returns a refusal, because a list has nowhere to
        put a reason - and an empty one would read as "no adapter offers
        this" when the truth is "that is not an intent". Those are different
        answers and this module exists to keep them apart. ValueError with
        the whole explanation is what register() already does for exactly
        this mistake, so the module answers it one way rather than two.
        Row 5b-84.
        """
        intent = (intent or "").upper()
        if intent not in INTENTS:
            raise ValueError(
                "'%s' is not a declared intent. The set is %s, and it is "
                "deliberately short - a guessed intent sends work to the "
                "wrong kind of thinking. Call route() instead if you want "
                "that refusal as data rather than as an exception."
                % (intent, ", ".join(sorted(INTENTS))))

        need = INTENTS[intent]
        found = []
        for name, kind, intents, strong in self._adapters:
            if intents and intent not in intents:
                continue
            if need["strong"] and not strong:
                continue
            if confidential and kind != LOCAL:
                continue
            found.append((name, kind))
        return found

    def route(self, intent, confidential=False):
        """
        {adapter, reason} - or {refused, why}. It never raises and never
        picks something it was told not to.

        A refusal is data rather than an exception because the caller's next
        move is to tell a modeller what is missing, and an exception two
        layers down arrives there as a stack trace.
        """
        intent = (intent or "").upper()
        if intent not in INTENTS:
            return {"refused": "UNKNOWN_INTENT",
                    "why": "'%s' is not a declared intent. The set is %s, and "
                           "it is deliberately short - a guessed intent sends "
                           "work to the wrong kind of thinking."
                           % (intent, ", ".join(sorted(INTENTS)))}

        if not self._adapters:
            return {"refused": "NO_ADAPTER_REGISTERED",
                    "why": "nothing has registered a provider adapter, so "
                           "there is nobody to ask."}

        found = self.candidates(intent, confidential)
        if not found:
            if confidential and self.candidates(intent, False):
                return {"refused": "NO_CONFIDENTIAL_ADAPTER",
                        "why": "this scope is confidential and no adapter "
                               "keeps the work on this machine. An adapter "
                               "that could answer exists, and using it would "
                               "send the model's contents off the machine - "
                               "so the answer is no, not that one."}
            return {"refused": "NO_ADAPTER_REGISTERED",
                    "why": "no registered adapter offers %s%s."
                           % (intent, " with the reasoning it needs"
                              if INTENTS[intent]["strong"] else "")}

        name, kind = found[0]
        return {"adapter": name,
                "reason": "%s (%s) was registered first among %d that offer "
                          "%s%s" % (name, kind, len(found), intent,
                                    ", and this scope is confidential so only "
                                    "on-machine adapters were considered"
                                    if confidential else "")}


def main(argv):
    router = Router()
    router.register("claude-code", HOST, strong=True)
    router.register("on-machine", LOCAL, intents=("CLASSIFY", "EXTRACT",
                                                  "SCORE", "SUMMARISE"))

    print("MODEL ROUTER   intent in, adapter out - never a model name")
    print("=" * 67)
    for intent in sorted(INTENTS):
        plain = router.route(intent)
        private = router.route(intent, confidential=True)
        print("  %-10s %-14s | confidential: %s"
              % (intent,
                 plain.get("adapter", plain.get("refused")),
                 private.get("adapter", private.get("refused"))))

    print()
    print("  REASON needs reasoning the on-machine adapter did not declare,")
    print("  so a confidential scope is refused rather than sent to the host:")
    print("    %s" % router.route("REASON", confidential=True)["why"])
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
