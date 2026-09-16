# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  brain
# See docs/29-metadata-standard.md

"""
The provider adapter - the layer three finished agents have been waiting for.

    python brain/heron_provider.py        probe whatever is on this machine

WHY THE HEADER SAYS `none`
----------------------------
This is INFRASTRUCTURE UNDER three agents, not a twenty-sixth row. The only
`adapter` rows in docs/28 are the deferred CAD ones - AutoCAD, Navisworks, IFC,
Rhino - and claiming an id here would be inventing a row nobody wrote, which is
the failure F27 exists to record. docs/37 is the scope.

WHAT WAS MISSING, EXACTLY
--------------------------
HERON-KRN-MDL-010 answers WHICH adapter and returns `{"adapter": name}` - a
STRING. Nothing in this repository said what a caller then does with it. A grep
for call, complete or probe across brain/ found nothing.

Three agents were already written against a provider none of them had met:

    heron_router.py         picks one by intent, refuses cloud for confidential
    heron_availability.py   sorts a probe into reachable/auth/slow/too small
    heron_budget.py         records ONLY what a provider reported (D-58)

Every field below is READ FROM one of those three rather than invented here.

STDLIB ONLY, AND THAT IS NOT A PREFERENCE
-------------------------------------------
`requests` is not installed on the machine this was written on, and neither are
torch, transformers or llama_cpp. `urllib.request` is in the standard library
and always there. An adapter that cannot be imported is worse than no adapter,
because the failure arrives as an ImportError somewhere unrelated.

heron_rerank.py already set this rule for heavy optional imports - lazy, inside
the function, with a graceful path when the thing is absent. Nothing here is
heavy enough to need that, because nothing here is imported at all.

WHAT `auth` MEANS WHEN A PROVIDER HAS NO PASSWORD
---------------------------------------------------
judge() is explicit: "MISSING IS NOT YES - a question nobody asked is not a
yes." A local Ollama has no credentials, so there is nothing to accept.

Reporting `auth: True` anyway would claim credentials were accepted when none
were offered. Leaving it out makes judge() report AUTH, so the adapter would
read as permanently rejected.

The honest answer is that A SUCCESSFUL REQUEST IS THE AUTH CHECK. If a real
request came back 2xx then whatever credentials were required - including none
- were accepted, and the question WAS asked. So:

    2xx            auth = True     asked, and answered yes
    401 or 403     auth = False    asked, and answered no
    never asked    auth absent     which judge() correctly refuses to read as a yes

CONTEXT LIMIT IS UNKNOWN UNTIL A MODEL SAYS SO
-----------------------------------------------
judge() skips the check when `context_limit` is None, and that is right:
unknown is not "too small". This reports the number only when the provider
reported one, and says `contextLimitKnown: False` otherwise so a reader can
tell "no limit problem" from "nobody asked".
"""

from __future__ import annotations

import json
import os
import sys
import time
import urllib.error
import urllib.request

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import heron_router as ROUTER  # noqa: E402

# Ollama's default. Overridable because a second instance on one machine is a
# real thing, and because the port is the first thing anybody changes.
OLLAMA_URL = os.environ.get("HERON_OLLAMA_URL", "http://127.0.0.1:11434")

# A probe must not hang a caller. judge()'s own ceiling is 3000 ms and a probe
# that takes longer than the ceiling has already answered the question.
PROBE_TIMEOUT_S = 5.0

# A generation is not a probe and may legitimately take much longer.
CALL_TIMEOUT_S = 120.0

# The intents an Ollama-hosted model is offered for. REASON is deliberately
# NOT here by default: docs/23 marks it `strong`, and whether a given local
# model is strong enough is a property of THAT MODEL, not of the adapter.
# Registering for it blind would route the hardest work to whatever happens to
# be installed.
DEFAULT_INTENTS = ("CLASSIFY", "EXTRACT", "SCORE", "SUMMARISE")


def _get(url, timeout):
    """
    (status, body, error). Never raises - a transport failure IS the answer.

    An exception here would reach judge() as a crash rather than as a probe
    result, and judge() has a PROBE_FAILED state precisely so that "the probe
    did not complete" stays distinguishable from "the provider is down".
    """
    request = urllib.request.Request(url, headers={"Accept": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=timeout) as answer:
            return answer.getcode(), answer.read().decode("utf-8", "replace"), None
    except urllib.error.HTTPError as error:
        # AN HTTP ERROR IS A REPLY. 401 is the provider answering, and calling
        # it unreachable would make Heron retry a refusal for ever.
        body = ""
        try:
            body = error.read().decode("utf-8", "replace")
        except Exception:
            pass
        return error.code, body, None
    except urllib.error.URLError as error:
        return None, "", str(getattr(error, "reason", error))
    except Exception as error:                      # noqa: BLE001
        return None, "", "%s: %s" % (type(error).__name__, error)


def probe(base_url=None, timeout=PROBE_TIMEOUT_S):
    """
    The dict heron_availability.judge() reads, and nothing it does not.

    Returns {reachable, auth, latency_ms, context_limit, error} plus a few
    fields judge() ignores and a person reading a report does not.
    """
    base = (base_url or OLLAMA_URL).rstrip("/")
    started = time.time()
    status, body, error = _get(base + "/api/tags", timeout)
    latency_ms = int((time.time() - started) * 1000)

    if status is None:
        # NOTHING ANSWERED. This is a REAL provider result, not a fixture -
        # it is what judge() calls UNREACHABLE, and it is the first time that
        # path has been exercised against something other than a test dict.
        return {
            "reachable": False,
            "latency_ms": latency_ms,
            "provider": "ollama",
            "url": base,
            "why": "nothing answered at %s (%s). Ollama is not running, or "
                   "not installed, or listening elsewhere." % (base, error),
        }

    if status in (401, 403):
        # ASKED, AND ANSWERED NO. Reachable is True on purpose: something is
        # there and it refused. judge() turns this into AUTH, which is the
        # state that must never be retried.
        return {
            "reachable": True,
            "auth": False,
            "latency_ms": latency_ms,
            "provider": "ollama",
            "url": base,
            "why": "it answered %d and refused the credentials." % status,
        }

    if not (200 <= status < 300):
        return {
            "reachable": True,
            "error": "HTTP %d" % status,
            "latency_ms": latency_ms,
            "provider": "ollama",
            "url": base,
            "why": "it answered %d, which is neither a refusal nor a result, "
                   "so nothing is known about the provider either way."
                   % status,
        }

    try:
        payload = json.loads(body or "{}")
    except ValueError:
        return {
            "reachable": True,
            "error": "the reply was not JSON",
            "latency_ms": latency_ms,
            "provider": "ollama",
            "url": base,
            "why": "it answered 200 with something that is not JSON, so the "
                   "probe did not complete.",
        }

    models = [one.get("name") for one in (payload.get("models") or [])
              if one.get("name")]

    answer = {
        # A SUCCESSFUL REQUEST IS THE AUTH CHECK - see the module docstring.
        "reachable": True,
        "auth": True,
        "latency_ms": latency_ms,
        "provider": "ollama",
        "url": base,
        "models": models,
        # UNKNOWN IS NOT TOO SMALL. judge() skips the check when this is None,
        # and this flag lets a reader tell that apart from a limit that passed.
        "contextLimitKnown": False,
        "why": "reachable and it answered; %d model(s) installed%s"
               % (len(models),
                  "" if models else " - so there is nothing to call yet"),
    }
    return answer


def call(model, prompt, base_url=None, timeout=CALL_TIMEOUT_S,
         degraded=False, adapter="ollama"):
    """
    {ok, text, usage, degraded, why} - or a refusal. Never raises.

    `usage` is THE PROVIDER'S OWN FIELDS, UNRENAMED. D-58 says Heron has no
    tokeniser, and heron_budget.record() takes only what a provider reported;
    renaming them here would put a translation table between the two and that
    table is what goes stale.

    `degraded` rides on the result because heron_availability's
    counts_as_evidence() already exists to read it: an answer from the
    second-choice provider is evidence about the fallback, not about the work.

    SO DOES `adapter`, AND FINDING THAT OUT IS WHY docs/37 SAID TO DO THIS
    NOW. counts_as_evidence() reads BOTH - `"adapter" in result` and
    `degraded is False` - and the first version of this function returned
    neither. A result that looked complete would have counted toward nothing,
    silently, because the one line that decides promotion could not see it.

    `adapter` rides on a RESULT and never on a refusal. Those two fields are
    all counts_as_evidence() reads, so a refusal carrying them would count as
    evidence toward promoting work that never ran.
    """
    base = (base_url or OLLAMA_URL).rstrip("/")
    if not str(model or "").strip():
        return {"ok": False, "refused": "NO_MODEL", "degraded": bool(degraded),
                "why": "no model was named. The router says WHICH ADAPTER, "
                       "never which model - that is this adapter's to know, "
                       "and it has to be told."}
    if not str(prompt or "").strip():
        return {"ok": False, "refused": "NOTHING_ASKED",
                "degraded": bool(degraded),
                "why": "nothing was asked. An empty prompt spends a call to "
                       "learn nothing."}

    body = json.dumps({"model": model, "prompt": prompt,
                       "stream": False}).encode("utf-8")
    request = urllib.request.Request(
        base + "/api/generate", data=body,
        headers={"Content-Type": "application/json"})
    started = time.time()
    try:
        with urllib.request.urlopen(request, timeout=timeout) as answer:
            status = answer.getcode()
            text = answer.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as error:
        status, text = error.code, ""
        try:
            text = error.read().decode("utf-8", "replace")
        except Exception:
            pass
    except Exception as error:                      # noqa: BLE001
        return {"ok": False, "refused": "UNREACHABLE",
                "degraded": bool(degraded),
                "why": "nothing answered at %s: %s: %s"
                       % (base, type(error).__name__, error)}

    latency_ms = int((time.time() - started) * 1000)
    if status in (401, 403):
        return {"ok": False, "refused": "AUTH", "degraded": bool(degraded),
                "latencyMs": latency_ms,
                "why": "it answered %d and refused the credentials - "
                       "retrying will not change that." % status}
    if not (200 <= status < 300):
        return {"ok": False, "refused": "PROVIDER_ERROR",
                "degraded": bool(degraded), "latencyMs": latency_ms,
                "why": "it answered %d: %s" % (status, text[:200])}

    try:
        payload = json.loads(text or "{}")
    except ValueError:
        return {"ok": False, "refused": "UNREADABLE_REPLY",
                "degraded": bool(degraded), "latencyMs": latency_ms,
                "why": "it answered 200 with something that is not JSON."}

    # EVERY FIELD OLLAMA CALLS USAGE, UNDER ITS OWN NAME. Nothing is summed
    # and nothing is renamed here; usage_tokens() below does the one addition
    # anybody needs and says which fields it added.
    usage = dict((key, payload[key]) for key in
                 ("prompt_eval_count", "eval_count", "total_duration",
                  "load_duration", "prompt_eval_duration", "eval_duration")
                 if key in payload)

    return {
        "ok": True,
        # ADAPTER AND DEGRADED TOGETHER ARE WHAT counts_as_evidence() READS.
        # Neither is decoration and neither may move to the refusal paths.
        "adapter": adapter,
        "text": payload.get("response", ""),
        "usage": usage,
        "model": payload.get("model", model),
        "degraded": bool(degraded),
        "latencyMs": latency_ms,
        "why": "%d character(s) back from %s in %d ms%s"
               % (len(payload.get("response", "")), payload.get("model", model),
                  latency_ms,
                  "" if usage else ", and it reported no usage at all"),
        "unjudged": [
            "WHETHER THE ANSWER IS ANY GOOD. This adapter moved bytes; "
            "judging the text is the calling agent's, and nothing here read "
            "it.",
            "WHAT IT COST IN MONEY. The provider reported %s and no more. "
            "Heron has no tokeniser and no price list (D-58), so a cost is "
            "not derivable here and is not offered."
            % (", ".join(sorted(usage)) or "nothing"),
        ],
    }


def usage_tokens(usage):
    """
    (total, which fields were added) - or (None, why not).

    THE ONE ADDITION ANYBODY NEEDS, AND IT NAMES ITS TERMS. Adding two counts
    a provider REPORTED is not counting tokens, which is what D-58 forbids.
    Inventing a number when the provider reported none would be.
    """
    fields = [one for one in ("prompt_eval_count", "eval_count")
              if isinstance((usage or {}).get(one), int)]
    if not fields:
        return None, ("the provider reported no token counts, so there is no "
                      "number to record. A ledger with a gap is honest where "
                      "an estimate is not.")
    return sum(usage[one] for one in fields), ", ".join(fields)


def register(router, name="ollama", intents=DEFAULT_INTENTS, strong=False):
    """
    Declare this adapter to HERON-KRN-MDL-010 as a LOCAL one.

    LOCAL IS THE LOAD-BEARING WORD. heron_router refuses a cloud adapter for
    confidential work and refuses EVERYTHING when no local one is registered.
    Both branches were written and neither had ever had a local adapter to
    prove against, which is why docs/37 puts this one first.
    """
    router.register(name, ROUTER.LOCAL, intents, strong=strong)
    return name


def main(argv):
    print("THE PROVIDER ADAPTER   what is actually on this machine")
    print("=" * 72)

    import heron_availability as AVAIL

    base = argv[0] if argv else OLLAMA_URL
    print("\nprobing %s" % base)
    result = probe(base)
    state, why = AVAIL.judge(result)
    print("  judge()   %-14s %s" % (state, why))
    print("  probe     %s" % result.get("why"))
    for key in ("reachable", "auth", "latency_ms", "context_limit"):
        shown = result.get(key, "<absent>")
        print("    %-16s %s" % (key, shown))

    print("\nregistered with the router as LOCAL")
    router = ROUTER.Router()
    register(router)
    for intent in ("CLASSIFY", "REASON"):
        picked = router.route(intent)
        print("  %-10s %s" % (intent, picked.get("adapter")
                              or picked.get("why", "")[:56]))
    confidential = router.route("CLASSIFY", confidential=True)
    print("  %-10s %s" % ("CLASSIFY*", confidential.get("adapter")
                          or confidential.get("why", "")[:56]))
    print("           * confidential - only a LOCAL adapter may serve it")

    if result.get("reachable") and result.get("models"):
        print("\ncalling %s" % result["models"][0])
        answer = call(result["models"][0], "Reply with the single word: ready")
        print("  %s" % answer.get("why"))
        total, which = usage_tokens(answer.get("usage"))
        print("  tokens    %s (%s)" % (total, which))
    else:
        print("\nno model to call - what is unproven is in NEEDS-CHECKING K3")
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
