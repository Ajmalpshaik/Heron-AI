# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-INS-HLT-009
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  bridge
# See docs/29-metadata-standard.md

"""
Install health - "done" is not evidence, and end to end means every step.

    python mcp/server/heron_install_health.py

WHAT IT IS FOR (docs/28, HERON-INS-HLT-009)
--------------------------------------------
"Verifies the install end to end." T1, risk READ. It is step 15 of the
sixteen in docs/07 s1, and step 16 - "user is told installation is
complete" - is the thing it is allowed to refuse.

"DONE" IS NOT EVIDENCE
------------------------
An installer reports what it attempted. Every step in docs/07 s1 can be
attempted successfully and still leave nothing working: a package
installs and imports as a different version, an add-in deploys to the
wrong per-user folder, an MCP server registers under a name the host
never reads. So a step is verified by a CHECK - something that could come
back FAILED - and never by the installer's own word.

A STEP VERIFIED BY THE THING THAT PERFORMED IT IS NOT VERIFIED
----------------------------------------------------------------
Golden Rule 7 says no agent approves itself, and this is the same shape
one layer down. `checked_by` naming the same component as `installed_by`
is refused, because an installer that also confirms its own work has a
success path with no observer on it - which is exactly the state an
install check exists to rule out.

END TO END MEANS EVERY STEP
-----------------------------
The register row says "end to end", so a report covering twelve of the
sixteen is not a good report - it is a partial one, and the difference is
the four nobody looked at. Anything unchecked keeps the overall state
below HEALTHY and is named. `complete()` - step 16, the sentence the user
reads - REFUSES while anything is unverified or failed, which is the only
part of this agent that says no rather than describes.

IT REUSES HERON-OPS-HLT-004's FOUR STATES
-------------------------------------------
HEALTHY / WARNING / DEGRADED / FAILED already mean four different things
in `heron_health.py`, and a second vocabulary for the same idea is two
vocabularies that disagree. The severity order comes from there too, so
"worst wins" means the same thing in both.

IT FIXES NOTHING AND RE-RUNS NOTHING
--------------------------------------
Risk READ. It reads what the checks found and says what that adds up to.
An install verifier that also re-ran a failed step would be an installer,
and then nothing would be checking it.
"""

import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)

ROOT = os.path.dirname(os.path.dirname(HERE))

import heron_health as HEALTH                                  # noqa: E402

# docs/07 s1's sixteen, in its order. A name outside this is refused: the
# install is a defined sequence and a step nobody specified is not one.
STEPS = (
    "claude-code", "workspace-folder", "environment", "revit-versions",
    "dependencies-checked", "packages-installed", "components-built",
    "addin-deployed", "mcp-configured", "tools-registered",
    "brain-initialised", "rag-initialised", "skills-installed",
    "fragments-indexed", "health-checked", "user-told",
)

# The last is step 16 and is what `complete()` produces - it is not
# something a check reports on.
VERIFIED_BY_CHECKS = STEPS[:-1]


def _state(check):
    """One step's state from its check, or None if it did not run."""
    if not isinstance(check, dict):
        return None
    state = str(check.get("state") or "").strip().upper()
    return state if state in HEALTH.SEVERITY else None


def report(checks):
    """
    {state, steps, unchecked, why} - what the install actually adds up to.

    Every step in docs/07 s1, whether or not anybody checked it. A step
    with no check is UNCHECKED and keeps the overall state below HEALTHY,
    because "nobody looked" and "fine" are the two readings an install
    report must never merge.
    """
    if not isinstance(checks, dict) or not checks:
        return {"refused": "NO_STEPS",
                "why": "nothing was checked. An install report with no "
                       "checks in it is not a clean install - it is a "
                       "report nobody ran."}

    for name in checks:
        if str(name).strip() not in STEPS:
            return {"refused": "NOT_A_STEP", "step": name,
                    "why": "'%s' is not one of the sixteen steps docs/07 s1 "
                           "defines. The install is a defined sequence, and "
                           "a step nobody specified is not one this agent "
                           "verifies." % name}

    steps, unchecked, worst = [], [], HEALTH.HEALTHY
    for name in VERIFIED_BY_CHECKS:
        check = checks.get(name)
        state = _state(check)
        if state is None:
            unchecked.append(name)
            steps.append({"step": name, "state": "UNCHECKED",
                          "why": "nothing checked this. 'Done' is not "
                                 "evidence: the installer reports what it "
                                 "ATTEMPTED, and every step here can be "
                                 "attempted successfully and leave nothing "
                                 "working."})
            continue

        # A STEP VERIFIED BY THE THING THAT PERFORMED IT IS NOT VERIFIED.
        installed_by = str(check.get("installed_by") or "").strip().lower()
        checked_by = str(check.get("checked_by") or "").strip().lower()
        if not checked_by:
            return {"refused": "STEP_NOT_CHECKED", "step": name,
                    "why": "%s reports %s and does not say what checked it. "
                           "A state with no observer is the installer's own "
                           "word wearing a check's clothes."
                           % (name, state)}
        if installed_by and checked_by == installed_by:
            return {"refused": "INSTALLER_VERIFIED_ITSELF", "step": name,
                    "why": "%s was installed by %s and checked by %s. Golden "
                           "Rule 7 - no agent approves itself - and this is "
                           "the same shape one layer down: a success path "
                           "with no observer on it is the state an install "
                           "check exists to rule out."
                           % (name, check.get("installed_by"),
                              check.get("checked_by"))}

        steps.append({"step": name, "state": state,
                      "checked_by": check.get("checked_by"),
                      "why": str(check.get("why") or "no detail given")})
        if HEALTH.SEVERITY[state] > HEALTH.SEVERITY[worst]:
            worst = state

    # ANYTHING UNCHECKED KEEPS IT BELOW HEALTHY. End to end means every
    # step, and the difference between twelve of sixteen and sixteen is
    # the four nobody looked at.
    if unchecked and HEALTH.SEVERITY[worst] < HEALTH.SEVERITY[
            HEALTH.WARNING]:
        worst = HEALTH.WARNING

    return {
        "state": worst, "steps": steps, "unchecked": unchecked,
        "checked": len(VERIFIED_BY_CHECKS) - len(unchecked),
        "of": len(VERIFIED_BY_CHECKS),
        "why": "%d of %d steps checked, worst state %s.%s"
               % (len(VERIFIED_BY_CHECKS) - len(unchecked),
                  len(VERIFIED_BY_CHECKS), worst,
                  "" if not unchecked else
                  " %d unchecked: %s - which is why this is not HEALTHY."
                  % (len(unchecked), ", ".join(unchecked))),
        "unjudged": [
            "NOTHING WAS RE-RUN OR FIXED. This is risk READ: it reads what "
            "the checks found and says what that adds up to. A verifier "
            "that also re-ran a failed step would be an installer, and then "
            "nothing would be checking it.",
            "each state came from a check somebody else ran. This agent "
            "cannot tell a thorough check from a shallow one - it can only "
            "tell a check from no check, and it says which steps had "
            "neither.",
            "the four states are HERON-OPS-HLT-004's, and so is the "
            "severity order. A second vocabulary for the same idea is two "
            "vocabularies that disagree.",
        ],
    }


def complete(checks):
    """
    Step 16 - "user is told installation is complete" - or a refusal.

    The only part of this agent that says no. Everything unverified or
    failed is named, because the sentence a user reads after an install
    is the one they will remember when something does not work.
    """
    found = report(checks)
    if found.get("refused"):
        return found

    wrong = [entry for entry in found["steps"]
             if entry["state"] != HEALTH.HEALTHY]
    if wrong:
        return {"complete": False, "refused": "NOT_COMPLETE",
                "state": found["state"], "outstanding": wrong,
                "why": "%d of %d steps are not HEALTHY: %s. 'Installation "
                       "complete' is the sentence a user remembers when "
                       "something does not work later, and it is worth "
                       "exactly what it was checked against."
                       % (len(wrong), found["of"],
                          ", ".join("%s (%s)" % (entry["step"],
                                                 entry["state"])
                                    for entry in wrong)),
                "proposal": "fix or check them, then ask again. This agent "
                            "does neither - it is READ."}

    return {"complete": True, "state": found["state"], "steps": found["steps"],
            "why": "all %d steps checked and HEALTHY, each by something "
                   "other than what installed it." % found["of"],
            "unjudged": found["unjudged"]}


def main(argv):
    print("INSTALL HEALTH   'done' is not evidence, end to end means every")
    print("=" * 72)

    def ok(name, by, why):
        return {"state": HEALTH.HEALTHY, "installed_by": name,
                "checked_by": by, "why": why}

    checks = dict((step, ok("installer", "heron-verify", "checked"))
                  for step in VERIFIED_BY_CHECKS)
    answer = complete(checks)
    print("  %s" % answer["why"])

    print()
    print("  End to end means every step - twelve of fifteen is not a pass:")
    partial = dict((step, checks[step]) for step in VERIFIED_BY_CHECKS[:12])
    answer = report(partial)
    print("    state %s  -  %s" % (answer["state"], answer["why"]))
    answer = complete(partial)
    print("    complete=%s  %s" % (answer["complete"], answer["refused"]))

    print()
    print("  A step verified by the thing that installed it is not verified:")
    answer = report(dict(checks, **{
        "addin-deployed": ok("deploy-addin", "deploy-addin", "it said ok")}))
    print("    %s" % answer["refused"])
    print("      %s" % answer["why"][:96])

    print()
    print("  And a state with no observer is the installer's own word:")
    answer = report(dict(checks, **{
        "mcp-configured": {"state": "HEALTHY", "installed_by": "setup"}}))
    print("    %s - %s" % (answer["refused"], answer["why"][:78]))

    print()
    print("  The worst state wins, and it is HERON-OPS-HLT-004's ladder:")
    for state in (HEALTH.WARNING, HEALTH.DEGRADED, HEALTH.FAILED):
        answer = report(dict(checks, **{
            "rag-initialised": {"state": state, "installed_by": "setup",
                                "checked_by": "heron-verify",
                                "why": "the vector store is empty"}}))
        print("    one step %-9s -> overall %s" % (state, answer["state"]))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
