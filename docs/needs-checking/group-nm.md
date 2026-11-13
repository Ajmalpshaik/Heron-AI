# Needs checking — Group NM

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NM - two add-in agent proofs read STALE and are run again, and the proof tool now says which file a proof was signed for (2026-11-11)

On 2026-11-11 every agent in [the register](../28-agent-registry.md) was checked in a cloud container,
where there is no Windows and no Revit. `python tools/prove-agent.py check` read two of the add-in's
signed records **STALE** - `HERON-REVIT-SEL-008` and `HERON-REVIT-VIE-013` - and the rest current.
**Nothing was signed, re-signed or edited there**: a stale record stays stale until a person runs the
agent again on a real model and signs it. That is this group.

| Row | What changed | Held without Revit by | What that cannot see |
|---|---|---|---|
| [5b-1552](../fragment-issues/section-5b-rows-401-425.md) | A new draft is fingerprinted on the file its operation is in; `check` reads a record against the file it was signed for and names, on a `ONE FILE` line, every other file claiming the agent | [`tests/test_prove_agent.py`](../../tests/test_prove_agent.py) section 12, on a scratch tree | a real `track` or `vary` against Revit, and the real add-in files |
| [5b-1544](../fragment-issues/section-5b-rows-401-425.md) | `python brain/heron_pullrequest.py` and `python brain/heron_scaffold.py` run to the end instead of raising | [`tests/test_pullrequest.py`](../../tests/test_pullrequest.py), [`tests/test_scaffold.py`](../../tests/test_scaffold.py) | Windows - the cp1252 half of the sweep was a stand-in for it |

**The PC, Revit 2020** - the release the fifteen records were signed on, on 2026-10-25. A record names
its models and sessions and **no release** ([row 5b-1546](../fragment-issues/section-5b-rows-401-425.md)),
so say the release in the pull request that carries the new records. **SAVED scratch copies of sample
models, never the owner's own file.** Both agents only read or select; nothing in this group keeps a
change. Rough work - a redirected output, a copy of a model - goes in `%TEMP%\heron-work\nm`, never
beside Heron.

| # | Run | Look for |
|---|---|---|
| **NM1** | *What `check` says before anything is run.* `python tools/prove-agent.py check`, then `echo %ERRORLEVEL%` | `HERON-REVIT-SEL-008` and `HERON-REVIT-VIE-013` are **STALE** and no other; a `ONE FILE` line for each - `SEL-008`'s names `RevitTemporaryView.cs`, `VIE-013`'s says it was signed on `RevitCapture.cs` and names `RevitTemporaryView.cs` and `RevitViews.cs`; exit **1**. Any other agent STALE is a finding of its own: its file changed after this was written |
| **NM2** | *`SEL-008` again, the way it was signed: one model, the category varied.* A saved copy of Revit 2020's `rme_advanced_sample_project.rvt` open and connected - the 2026-10-25 record calls its copy *Agent Proof MEP*. `python tools/prove-agent.py sessions` for the session id, then `python tools/prove-agent.py vary HERON-REVIT-SEL-008 --operation select_by_category --session <id> --arg-name category --arg-values "Ducts,Pipes,Air Terminals,Flex Pipes,Pipe Insulation,Duct Insulation"`, then `python tools/prove-agent.py review HERON-REVIT-SEL-008` | The draft's `source:` is `revit/Heron.Revit.Addin/RevitOperations.cs`. **Read `gaps:` first**: it says the negative leg WAS run when a category the model has none of selected nothing (row 5b-1339's repair, never yet on a signed record), and it carries one new gap, *THE FINGERPRINT COVERS ONE FILE*, naming `RevitTemporaryView.cs`. In Revit, after the last value, what is selected is that category and nothing else. The counts are that model's own and need not equal 2026-10-25's |
| **NM3** | *`VIE-013` again: two models, `list_views`.* Two Revit 2020 sessions with **different** saved models open - the 2026-10-25 record used *Project1* and *color test*. `python tools/prove-agent.py sessions`, then `python tools/prove-agent.py track HERON-REVIT-VIE-013 --operation list_views --first <id> --second <id>`, then `python tools/prove-agent.py review HERON-REVIT-VIE-013` | **The draft's `source:` is `revit/Heron.Revit.Addin/RevitViews.cs`** - not `RevitCapture.cs`, which is what row 5b-1552 repaired; `source: ...RevitCapture.cs` fails the row. `gaps:` names `RevitCapture.cs` and `RevitTemporaryView.cs` as files the fingerprint leaves out. The view counts moved between the two models; if nothing moved, the draft says so and it is not signed |
| **NM4** | *Sign, and read `check` again.* Only the owner: `python tools/prove-agent.py accept HERON-REVIT-SEL-008 --by "Ajmal PS"` and the same for `HERON-REVIT-VIE-013` - after reading each draft's gaps, and not if either reads wrong. Then `python tools/prove-agent.py check` and `echo %ERRORLEVEL%` | Every record reads current, none STALE, exit **0** - and the two `ONE FILE` lines are still there, because one fingerprint still covers one file. `VIE-013`'s now says it was signed on `RevitViews.cs` |
| **NM5** | *Negative - the file a proof was signed for is the one that stales it.* After NM4, with nothing built or deployed in between: add one blank line to the end of `revit\Heron.Revit.Addin\RevitCapture.cs` and run `check`; then add one to `RevitViews.cs` and run it again; then `git checkout -- revit/Heron.Revit.Addin/RevitCapture.cs revit/Heron.Revit.Addin/RevitViews.cs` and run it a third time | First run: `VIE-013` still **current** - the picture file is named on its `ONE FILE` line and is not what was signed. Second: `VIE-013` **STALE**. Third: current again, and `git status` shows neither file changed. If the first run reads STALE, the record was signed on the wrong file and NM3 failed |
| **NM6** | *The two repaired command lines on Windows, redirected* - the one condition in which Python prints in the ANSI code page. `python brain\heron_pullrequest.py > "%TEMP%\heron-work\nm\pr.txt" 2>&1`, `echo %ERRORLEVEL%`; the same for `brain\heron_scaffold.py` | Both exit **0**; neither file holds `Traceback`; the first ends with four lines under *what this agent does not judge* and the second with seven |

**Then** the group closes on NM1 to NM6. A row that fails reopens its FRAGMENT-ISSUES row; nothing in a
row changes to make it pass. The records NM4 writes go in a pull request that names Revit 2020, and that
pull request brings the dated sentences in [the register](../28-agent-registry.md)'s `SEL-008` and
`VIE-013` rows up to the day: they say *reads STALE (measured 2026-11-11)* until then.

### Run record - 2026-11-13, NM1, NM2 and NM6 on the laptop, Revit 2020

Branch `claude/pc-proofs-2026-11-12` (main at 10334996 and this sitting's records). Revit 2020, session
41776, the add-in built from 10334996. The model is **P1102 MEP**, the saved scratch copy of Revit 2020's
`rme_advanced_sample_project.rvt` under `%TEMP%\heron-work\prove-1102` - the same sample the 2026-10-25
record's *Agent Proof MEP* was copied from, not the same copy. Output in `%TEMP%\heron-work\nm`.

| # | Result | What was seen |
|---|---|---|
| NM1 | **PASS** | `15 proof(s): 13 current, 2 STALE, 0 whose source is gone`; STALE `HERON-REVIT-SEL-008` and `HERON-REVIT-VIE-013` and no other; `ONE FILE` for SEL-008 names `RevitTemporaryView.cs`; for VIE-013 says *signed on revit/Heron.Revit.Addin/RevitCapture.cs* and names `RevitTemporaryView.cs`, `RevitViews.cs`; exit 1 |
| NM2 | **PASS** - signed in NM4 | `vary ... --session 41776`: 6 inputs, 4 distinct answers - Ducts 730, Pipes 488, Air Terminals 309, Flex Pipes 0, Pipe Insulation 0, Duct Insulation 0, each `categories: 1`. Draft `source: 'revit/Heron.Revit.Addin/RevitOperations.cs'`, fingerprint `75a543b6b1a2a209`. `gaps:` 2 - *D-30's NEGATIVE LEG WAS RUN AND IS RECORDED: category=Flex Pipes returned an empty answer*, and *THE FINGERPRINT COVERS ONE FILE ... RevitTemporaryView.cs*. After the last value, `read-selection` read 0 elements selected: Duct Insulation, of which the model has none, and nothing else |
| NM3 | **NOT RUN** | needs two Revit 2020 sessions open at once; this PC holds one Revit at a time (16 GB, the owner's rule). The owner decides how it is run |
| NM4 | **HALF DONE** | `accept HERON-REVIT-SEL-008 --by "Ajmal PS"` on the owner's word (*"You can sign"*), 2026-11-13; then `check`: `15 proof(s): 14 current, 1 STALE` - VIE-013 STALE, as it must be until NM3; both `ONE FILE` lines still there; exit 1. VIE-013 has no draft until NM3 |
| NM5 | **NOT RUN** | after NM4 |
| NM6 | **PASS** | `brain\heron_pullrequest.py` and `brain\heron_scaffold.py`, each redirected to a file through `cmd`: exit 0 both, no `Traceback` in either; the first ends with four lines under *what this agent does not judge*, the second with seven |
