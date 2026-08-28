#!/usr/bin/env python3
# Heron-Agent:  HERON-ORC-FAIL-004
# Heron-Step:   6
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
Failure Analysis Agent. Why something failed, and what may safely happen next.

Its one rule, from the registry: NEVER BLIND-RETRIES.

A blind retry is not a small mistake. Heron's failures come back looking alike
- a refusal, a timeout and a lost answer are all "it did not work" - but they
are three completely different situations, and only one of them is dangerous:

    NEVER RAN       the request never reached the model. Asking again is free.
    REFUSED         Revit answered "no". Nothing happened. Fix the cause first.
    RUNNING         Revit took it and has not finished. Asking again while it
                    works is how the same job gets done twice.
    ROLLED BACK     it ran, it failed, and the model is as it was.
    UNKNOWN         it was SENT and the answer was lost. Whether Revit ran it
                    cannot be determined from here, by any means, ever.

That last one is the whole reason this file exists. Step 5 built the mechanism
- `idempotent=False` on the client - and this is the judgement that goes with
it: for anything that CHANGES a model, an unknown outcome must never be
retried automatically. Repeating a move because the answer went missing moves
the same ducts twice, and "it looked like it failed" is exactly how that
happens. The only correct next step is a person looking at the model.

FAIL CLOSED ON WHAT IT DOES NOT RECOGNISE. An error code this file has never
seen, on an operation that writes, is treated as UNKNOWN - not as retryable.
A future operation added by someone who never read this file gets the safe
answer by default rather than the convenient one.
"""


# ------------------------------------------------------- what actually happened

NEVER_RAN = "never_ran"
REFUSED = "refused"
RUNNING = "running"
ROLLED_BACK = "rolled_back"
UNKNOWN = "unknown"


# ------------------------------------------------------------ what to do now

RETRY = "retry"                    # safe to send again, unchanged
WAIT = "wait"                      # it is still working; asking again is wrong
FIX_FIRST = "fix_first"            # something must change before it can work
LOOK_AT_THE_MODEL = "look"         # a person must check before anything else
START_OVER = "start_over"          # the request is stale; build it again
STOP = "stop"                      # do not retry, and do not work around it


class Failure(object):
    """One failure, classified."""

    def __init__(self, code, outcome, next_step, reason, message=None):
        self.code = code
        self.outcome = outcome
        self.next_step = next_step
        self.reason = reason
        self.message = message

    @property
    def may_retry(self):
        """
        Whether Heron may send the same request again WITHOUT a person
        deciding to. Deliberately narrow: only a request that provably never
        reached the model qualifies.
        """
        return self.next_step == RETRY

    @property
    def touched_the_model(self):
        """
        Might the model have changed? None means genuinely unknown, which is
        different from False and must never be collapsed into it.
        """
        if self.outcome in (NEVER_RAN, REFUSED, ROLLED_BACK):
            return False
        if self.outcome == UNKNOWN:
            return None
        return True

    def __repr__(self):
        return "<Failure %s %s -> %s>" % (self.code, self.outcome, self.next_step)


# The failures Heron can actually produce, each with what it means and what
# follows. Anything not here is handled by the fail-closed default below.
#
#   code: (outcome, next step, reason)
_KNOWN = {
    # --- never reached the model -------------------------------------------
    "revit_busy":        (NEVER_RAN, FIX_FIRST,
                          "Revit never took the request - a dialog is open or a command is running"),
    "not_ready":         (NEVER_RAN, RETRY,
                          "Heron had not finished starting; nothing was sent"),
    "raise_failed":      (NEVER_RAN, RETRY,
                          "the request could not be handed to Revit at all"),
    "unauthorized":      (NEVER_RAN, FIX_FIRST,
                          "the session token was refused - that Revit was rebound or restarted"),
    "session_in_use":    (NEVER_RAN, FIX_FIRST,
                          "another chat holds the lease on that Revit, so Heron refused rather "
                          "than taking it over mid-job"),
    "unknown_op":        (NEVER_RAN, STOP,
                          "this Revit's add-in does not know that operation - the two halves "
                          "are different versions"),

    # --- Revit answered "no", and nothing happened -------------------------
    "no_document":       (REFUSED, FIX_FIRST, "no model is open"),
    "read_only":         (REFUSED, STOP, "the model is open read-only"),
    "no_category":       (REFUSED, FIX_FIRST, "no category was given"),
    "unknown_category":  (REFUSED, FIX_FIRST, "Heron does not know that category yet"),
    "no_distance":       (REFUSED, FIX_FIRST, "no distance was given"),
    "bad_distance":      (REFUSED, FIX_FIRST, "the distance could not be read"),
    "zero_distance":     (REFUSED, FIX_FIRST, "moving by zero would change nothing"),
    "nothing_to_move":   (REFUSED, STOP, "there is nothing there to move"),
    "write_disabled":    (REFUSED, FIX_FIRST,
                          "Heron's ability to change the model is switched off"),
    "stopped":           (REFUSED, FIX_FIRST, "the Emergency Stop is on"),

    # --- the approval was stale, so it was refused before touching anything -
    "no_preview":        (REFUSED, START_OVER, "there was nothing waiting to be approved"),
    "wrong_preview":     (REFUSED, START_OVER, "the approval did not match what Heron was holding"),
    "preview_expired":   (REFUSED, START_OVER, "the preview was too old to still describe the model"),
    "model_moved_on":    (REFUSED, START_OVER, "the model changed after the preview was shown"),
    "document_not_in_front": (REFUSED, FIX_FIRST,
                          "another model is in front; the approved one is still open"),
    "document_closed":   (REFUSED, STOP,
                          "the approved model was closed - Heron will not move to another one"),

    # --- it ran ------------------------------------------------------------
    "still_running":     (RUNNING, WAIT,
                          "Revit took it and has not finished; it is still working"),
    "move_failed":       (ROLLED_BACK, FIX_FIRST,
                          "it ran, failed, and was rolled back completely"),
    "operation_failed":  (ROLLED_BACK, FIX_FIRST,
                          "the operation threw; nothing was committed"),

    # --- the one that cannot be resolved from here -------------------------
    "unknown_outcome":   (UNKNOWN, LOOK_AT_THE_MODEL,
                          "the request reached Revit and the answer was lost"),
}


def analyse(reply, writes=False):
    """
    Classify one bridge reply.

    `reply` is what the client returned: a parsed dict, or None when nothing
    came back at all. `writes` says whether the operation could change the
    model - it decides how an unrecognised or ambiguous failure is treated,
    and it must be passed truthfully.

    Returns None when the reply is a success. There is no failure to analyse,
    and returning a Failure that means "fine" is how a caller ends up treating
    success as something to recover from.
    """
    if reply is None:
        # Nothing came back and there is no code to reason from. For a read
        # that is a lost answer to a harmless question. For a write it is the
        # dangerous case by default - Heron cannot prove the request never
        # left, so it must not assume it.
        if writes:
            return Failure(
                "no_reply", UNKNOWN, LOOK_AT_THE_MODEL,
                "nothing came back, and for a change to the model that cannot be "
                "assumed to mean it did not happen",
                "Heron did not get an answer, so it cannot tell you whether that happened. "
                "Look at the model before trying again - repeating it could do the work twice.")
        return Failure(
            "no_reply", NEVER_RAN, RETRY,
            "nothing came back, and the request only reads, so asking again costs nothing")

    if reply.get("ok"):
        return None

    code = reply.get("error") or "unspecified"
    message = reply.get("message")

    known = _KNOWN.get(code)
    if known is not None:
        outcome, next_step, reason = known
        return Failure(code, outcome, next_step, reason, message)

    # FAIL CLOSED. An unrecognised code on an operation that writes is treated
    # as an unknown outcome, because this file cannot prove otherwise and the
    # cost of being wrong is the model. On a read it is merely a refusal.
    if writes:
        return Failure(
            code, UNKNOWN, LOOK_AT_THE_MODEL,
            "Heron does not recognise this failure, and it came from an operation that "
            "can change the model, so it is treated as though the outcome is unknown",
            message)

    return Failure(code, REFUSED, FIX_FIRST,
                   "Heron does not recognise this failure, but the operation only reads",
                   message)


def explain(failure):
    """
    The failure as a person should hear it: what happened, then what to do.

    Prefers the message the far side already wrote - it knows the specifics,
    and rewording it here would produce two different sentences for one event.
    This adds only the part the far side cannot know: whether it is safe to
    simply ask again.
    """
    if failure is None:
        return None

    lines = [failure.message or ("It failed: %s." % failure.reason)]

    if failure.next_step == LOOK_AT_THE_MODEL:
        lines.append("Heron will NOT try that again on its own - repeating it could do the "
                     "work twice. Check the model first.")
    elif failure.next_step == WAIT:
        lines.append("Do not ask again while it is still working.")
    elif failure.next_step == START_OVER:
        lines.append("Ask for the change again and Heron will show a fresh preview.")
    elif failure.next_step == STOP:
        lines.append("Heron has stopped rather than working around this.")

    return "\n".join(lines)
