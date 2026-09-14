# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-AHR-TRN-005
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Trainer - an em-dash is not READ, and the nearest thing is not an
approved example.

    python tests/test_trainer.py

WHAT IT PROVES
  1. A ROW WITH NO RISK IS REFUSED, NOT DEFAULTED. An empty Risk column
     means nobody has decided, which is a different fact from READ. 57 of the
     register's 250 rows are in that state, and flattening them into READ is
     the quiet version of getting permissions wrong.

  2. A RISK WORD NOBODY RECOGNISES IS NOT READ AS THE MILDEST ONE.

  3. RISK DECIDES THE RULES, AND THE REGISTER DECIDES THE RISK. A READ agent
     is told its permission stops at reading; a MODIFY agent is not told
     that, and the suite checks the words rather than the instruction id.

  4. THE RULES ARE ASSEMBLED, NEVER COPIED. The pack's text comes from
     HERON-KRN-PRO-011 verbatim, and the Trainer's own source does not
     contain it.

  5. THE STANDARDS ARE THE GATES THAT REALLY RUN, listed from tools/ rather
     than typed - so a gate added tomorrow is in tomorrow's pack.

  6. AN APPROVED EXAMPLE IS PROVEN OR PRODUCTION AND NOTHING ELSE. A DRAFT
     agent has never met a real model, and offering one as an example would
     teach whatever it got wrong with the Trainer's authority behind it.

  7. WITH NO APPROVED EXAMPLE, THE LIST IS EMPTY AND SAYS SO - which is
     today's real answer for the whole register.

  8. NOTHING IS STORED. The pack is assembled on every call (D-40).

  9. EVERY FAILURE THE CONTRACT DECLARES IS NAMED BY THE CODE.
"""

import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))

import heron_trainer as TRN                                   # noqa: E402
import heron_agents as REG                                    # noqa: E402
import heron_contract as CON                                  # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def main():
    agents, claims, host = REG._agent_count()
    records = REG.records(agents=agents, claims=claims, host=host)

    def ask(agent_id, **kw):
        kw.setdefault("agents", agents)
        kw.setdefault("records", records)
        return TRN.train(agent_id, **kw)

    print("1. A row with no risk is refused, not defaulted")
    blank = sorted(a for a, row in agents.items()
                   if not (row.get("risk") or "").strip())
    check(blank, "the register really has rows with an empty Risk column")
    answer = ask(blank[0])
    check(answer.get("refused") == "NO_RISK_ASSIGNED",
          "%s is refused rather than trained as READ" % blank[0])
    check("not the same as READ" in answer["why"],
          "and the refusal says why an em-dash is not READ")

    print()
    print("2. A risk word nobody recognises is not read as the mildest one")
    made_up = dict(agents)
    made_up["HERON-AHR-WFP-015"] = dict(agents["HERON-AHR-WFP-015"],
                                        risk="SUPERUSER")
    answer = ask("HERON-AHR-WFP-015", agents=made_up)
    check(answer.get("refused") == "UNKNOWN_RISK_LEVEL",
          "'SUPERUSER' is refused, not treated as READ")

    print()
    print("3. Risk decides the rules, and the register decides the risk")
    reading = [a for a, row in agents.items()
               if (row.get("risk") or "").upper() == "READ"]
    writing = [a for a, row in agents.items()
               if (row.get("risk") or "").upper() == "MODIFY"]
    check(reading and writing, "the register carries both kinds")
    read_pack = ask(reading[0])["pack"]
    write_pack = ask(writing[0])["pack"]
    check(read_pack["instruction"] == "agent.read",
          "a READ agent gets agent.read")
    check(write_pack["instruction"] == "agent.modify",
          "a MODIFY agent gets agent.modify")
    check("Your permission stops at reading" in read_pack["rules"],
          "and the READ agent is actually told its permission stops there")
    check("Your permission stops at reading" not in write_pack["rules"],
          "and the MODIFY agent is NOT told that - the words, not the id")
    check(read_pack["risk"] == "READ"
          and read_pack["risk"] == agents[reading[0]]["risk"].upper(),
          "the risk in the pack is the register's, unchanged")

    print()
    print("4. The rules are assembled, never copied")
    import heron_instructions as PRO
    text, _used = PRO.compose("agent.read")
    check(read_pack["rules"] == text,
          "the pack's text is HERON-KRN-PRO-011's output verbatim")
    source = open(os.path.join(ROOT, "brain", "heron_trainer.py"),
                  encoding="utf-8").read()
    check("Your permission stops at reading" not in source,
          "and the Trainer's own source does not contain the rule text")
    # THEY COME BACK AS STRINGS, and the YAML declares them as ints.
    # heron_instructions normalises with str() because the Constitution's
    # own numbers are captured as text - so `14 in articles` is False and
    # `"14" in articles` is True. Pinned here because a caller that guesses
    # wrong gets a silent empty match rather than an error.
    check(read_pack["articles"]
          and all(str(n).isdigit() for n in read_pack["articles"]),
          "the articles come back as digit strings, one per article")
    check("14" in read_pack["articles"] and 14 not in read_pack["articles"],
          "as STRINGS - the YAML declares 14 and the pack carries '14'")

    print()
    print("5. The standards are the gates that really run")
    listed = TRN.standards()
    on_disk = sorted("tools/%s" % n
                     for n in os.listdir(os.path.join(ROOT, "tools"))
                     if n.startswith("check-") and n.endswith(".py"))
    check(listed == on_disk and len(listed) > 4,
          "%d gates, listed from tools/ rather than typed" % len(listed))
    check("tools/check-metadata.py" in listed and
          "tools/check-structure.py" in listed,
          "and the four mandatory ones are among them")

    print()
    print("6. An approved example is PROVEN or PRODUCTION and nothing else")
    check(TRN.APPROVED_STAGES == ("PROVEN", "PRODUCTION"),
          "the two stages are the two docs/24 calls proven")
    pretend = {
        "A": {"id": "A", "state": "DRAFT", "does": "a draft"},
        "B": {"id": "B", "state": "SHADOW", "does": "in shadow"},
        "C": {"id": "C", "state": "PROVEN", "does": "proven"},
        "D": {"id": "D", "state": "PRODUCTION", "does": "in production"},
        "E": {"id": "E", "state": "VALIDATED", "does": "validated"},
    }
    found = [e["agent"] for e in TRN.approved_examples(pretend)]
    check(found == ["C", "D"],
          "PROVEN and PRODUCTION qualify; DRAFT, VALIDATED and SHADOW do not")

    print()
    print("7. With no approved example the list is empty, and says so")
    real = TRN.approved_examples(records)
    check(real == [],
          "no agent in the real register is above DRAFT, so there are none")
    answer = ask(reading[0])
    check(answer["pack"]["examples"] == [],
          "the pack hands over an empty list rather than the nearest thing")
    check(any("NO APPROVED EXAMPLE EXISTS" in n for n in answer["unjudged"]),
          "and says so, every time, in unjudged")
    check(any("ENOUGH" in n for n in answer["unjudged"]),
          "and never claims the pack is sufficient - that needs a reading")

    print()
    print("8. Nothing is stored")
    for call in ("open(", "makedirs", "json.dump", "yaml.dump", ".write("):
        check(call not in source,
              "the Trainer's source has no %s - the pack is assembled (D-40)"
              % call)

    print()
    print("9. Every failure the contract declares is named by the code")
    contract = CON.load(os.path.join(ROOT, "brain", "agents",
                                     "HERON-AHR-TRN-005.yaml"))
    for failure in contract.get("failures") or []:
        check(failure in source, "the code names %s" % failure)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    an em-dash is not READ, and no example beats a wrong one")
    return 0


if __name__ == "__main__":
    sys.exit(main())
