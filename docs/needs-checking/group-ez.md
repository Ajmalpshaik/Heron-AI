# Needs checking — Group EZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group EZ - a fragment that never ends: the run clock, and a stop sent over the bridge (2026-10-17)

**Tracked as GitHub issue [#968](https://github.com/Ajmalpshaik/Heron-AI/issues/968) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

Until 2026-10-17 a fragment that looped for ever held Revit's thread until Revit was ended: the dispatcher
stopped waiting after `revit.operationTimeoutSeconds` and could only answer `still_running`, and the stop
flag that might have helped could be set by nothing ([D-46](../decisions/D-46.md)).
[D-129](../decisions/D-129.md) changes both, and [row 5b-523](../fragment-issues/section-5b-rows-501-525.md)
records the defect.

**What changed.** `HeronStackGuard` puts a call to `HeronRunClock.Check()` at the head of every loop body and
at the entry of every method and lambda in a fragment. The clock trips when the job's time is up - the
dispatcher's own wait less a margin, 50 s of the default 60 s - or when a stop is asked for while the job
runs, and the trip is sticky, so a fragment that catches it cannot loop on. `RevitFragment` asks the clock
after every script, thrown or not, rolls the whole job back and answers `ran_too_long` or
`stopped_mid_run`, saying whether Revit confirmed the rollback. The new `stop` operation is answered by the
dispatcher on a bridge thread before anything is queued, so it reaches a Revit whose thread a runaway job
holds; it sets `HeronStop`, and Heron then stays stopped - writes refused, reads allowed - until Revit is
restarted.

**What is compiled and what is not run.** The add-in builds for all eight releases 2020 to 2027
(`tools/check-compile.py`, 2026-10-17). `tests/test_stack_guard.py` runs the rewriter over the whole
library - every loop in every fragment asks the clock, and the rewrite adds nothing but its two calls and
the braces a one-statement loop body needs - and then RUNS guarded runaways through CSharpScript on a
200 ms deadline: `while(true){}`, `try{while(true){}}catch(Exception){}`, a loop that catches the stop and
loops on, and a recursion with no loop all stop at about 200 ms, and a stop asked from another thread
stops one in about 50 ms. **None of it has run in Revit**, and none of it can until the add-in is rebuilt,
deployed for that release and Revit restarted. A rollback, a frozen Revit coming back, and a second pipe
reaching the bridge while the first waits are the three things only a Revit can show.

**A SCRATCH MODEL, never a working one, and a PROBE FRAGMENT that is deleted afterwards** - the way
[J9](group-j.md) was run. The probe is a MODIFY fragment in the library for the length of the session,
`zz-ej-runaway-probe`: it draws one model line on Level 1, then `while (true) { }`. Its twin
`zz-ej-swallow-probe` draws the same line, then `try { while (true) { } } catch (Exception) { }`, and
returns. Count the model lines before EZ1. Changes on.

| # | Run | Look for |
|---|---|---|
| ~~**EZ1**~~ | A runaway fragment on a scratch model is stopped and rolled back within the timeout. — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | Within 60 s - near 50 - the answer is `ran_too_long`, naming 50 seconds and the 60 Heron… |
| ~~**EZ2**~~ | Revit stays responsive — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | Revit is frozen while the probe runs, as it always was: the clock cannot make the loop… |
| **EZ3** | **A stop sent from a second pipe mid-run works.** Two terminals sharing one client id. A: `set HERON_CLIENT_ID=ej3` then the EZ1 command. B, within ten seconds of A starting: `set HERON_CLIENT_ID=ej3` then `python mcp/client/heron_bridge_client.py stop` | B answers at once - *Heron is stopped. Revit is running a job now ...* - while A is still waiting; A then gets `stopped_mid_run` within a second or two, not at 50 s, with the rollback confirmed and the model line count unchanged. Then a write is refused `stopped` and a read still answers; after Revit is restarted the same write is let through - the one way out `HeronStop` names. **The negatives:** B with a DIFFERENT `HERON_CLIENT_ID` is refused `session_in_use` and A runs on to `ran_too_long` - only the chat holding a Revit can stop it; and B with nothing running answers *Nothing was running*. Then restart Revit before anything else, because Heron stays stopped |
| **EZ4** | EZ1 and EZ3 once on Revit 2020 and once on 2027 | The same answers - the guard and the clock are in the add-in every release loads, and 2020 is the oldest Roslyn host Heron runs in |
| **EZ5** | *Watching, not a proof.* After EZ1 to EZ4, delete both probe fragments and run `python tools/check-routing.py --targets` | The library is back to what it was, and nothing routes to a probe |
| **EZ6** | **EZ3 again with TWO Revits connected - the stop goes to the one named and no other** ([row 5b-1094](../fragment-issues/section-5b-rows-1076-1100.md)). Two releases open on scratch models, e.g. 2020 and 2024, both with this add-in, and a client that has `stop --session` (row 5b-1094's fix). First, nothing running: `python mcp/client/heron_bridge_client.py stop` with no `--session`. Then EZ3 on the 2024 one: A `set HERON_CLIENT_ID=ez6` and EZ1's command with `--session <2024 pid>`; B within ten seconds, `set HERON_CLIENT_ID=ez6` then `stop --session <2024 pid>`. **Negatives:** B with a different `HERON_CLIENT_ID` and `--session <2024 pid>`; and `stop --session 99999` | The bare `stop` lists both Revits as `--session <pid>`, says *Nothing was sent, so no Revit was stopped*, exits 2, and neither add-in log has *Stop asked over the bridge*. The named stop gives EZ3's answers on 2024 - *Heron is stopped. Revit is running a job now ...*, A `stopped_mid_run` with the rollback confirmed - while the 2020 log has no stop line and a write preview there is not refused `stopped`. The stranger is refused `session_in_use` by 2024, A runs on to `ran_too_long`, and 2020 is not touched; `99999` is refused naming both sessions, nothing sent. Restart Revit 2024 afterwards; 2020 needs no restart. **Do not** send a stranger's stop with `--session <2020 pid>` to another chat's Revit: an idle Revit still takes it, by the add-in's rule as it stands - [Q-71](../open-questions/tier-2.md) |
| **EZ7** | **Two Revits on this add-in: a stop names the one it is for, and the other runs on and answers** ([Q-71](../open-questions/tier-2.md), decided 2026-11-15; the add-in half built 2026-11-16). EZ6 with the add-in built from this change deployed for BOTH releases and both restarted - e.g. 2020 and 2024, on scratch models. Nothing running: `python mcp/client/heron_bridge_client.py stop` with no `--session`; then close 2020, send the same with only 2024 up, and open 2020 again. Then EZ6's named stop on 2024 mid-run: A `set HERON_CLIENT_ID=ez7` and EZ1's command with `--session <2024 pid>`; B within ten seconds, `set HERON_CLIENT_ID=ez7` then `stop --session <2024 pid>`. Straight after, on 2020: `ping <2020 pid>`, `count <2020 pid>` and a write preview | The bare `stop` sends nothing with two Revits up AND with one: each connected Revit listed as `--session <pid>   Revit <release>`, *Nothing was sent, so no Revit was stopped*, exit 2, and no add-in log has a line for it. The named stop gives EZ3's answers on 2024 - *Heron is stopped. Revit is running a job now ...* at once, A `stopped_mid_run` with the rollback confirmed - while 2020 answers `ping`, its `count` returns the model, a write preview there is not refused `stopped`, and its log has no stop line, refused or kept. **A FAIL** - 2020 refusing a write `stopped`, or either Revit stopped by the bare `stop` - means a stop reached a Revit it did not name: check both add-ins deployed are this one before anything else. Restart Revit 2024 afterwards; 2020 needs no restart |
| **EZ8** | **The add-in refuses a stop that does not name it.** EZ7's two Revits after its restart of 2024, nothing running, from the Heron folder with `set HERON_CLIENT_ID=ez7`. First the old request an older client sends - `stop`, naming no session - to 2024 alone: `python -c "import sys; sys.path.insert(0, 'mcp/client'); import heron_bridge_client as c; b = [x for x in c.discover()[0] if x.pid == <2024 pid>][0]; print(b.request('stop')); b.release()"`. Then a stop naming the OTHER Revit, sent to 2024's pipe: the same line with `b.request('stop_session', op_args={'session': '2020/<2020 pid>'})` in place of `b.request('stop')` | Both refused by 2024 with nothing stopped: the first `stop_unnamed` - *This stop does not name the Revit it is for. Nothing was stopped, and Heron here works as before ... this is Revit 2024, session <pid>.*; the second `stop_wrong_session` - *This stop is for session 2020/<pid>, and this is Revit 2024, session <pid>. Nothing was stopped here ...*. 2024's log has *A stop over the bridge was refused* twice and no *Stop asked over the bridge*, its audit trail (`audit-YYYYMM.jsonl`) a refused `stop` and a refused `stop_session` carrying those two codes, and a write preview on 2024 is not refused `stopped`; 2020 has nothing. **A FAIL** - either answered *Heron is stopped* - means an older client's stop, or one for another Revit, still stops this one: the broadcast Q-71 ended. Neither Revit needs a restart after a pass |
| **EZ9** | **An add-in from before this change is not stopped by the named stop - version skew fails safe.** Two Revits, one left on an add-in built before 2026-11-16 but carrying D-129's run clock (2026-10-17) - e.g. 2020 on `6cb8bec8`; one older than the clock would hold Revit on the runaway below until Revit is ended - and the other on this one; nothing running. `python mcp/client/heron_bridge_client.py stop --session <old pid>`. Then EZ1's runaway in the OLD one, and within ten seconds `stop --session <old pid>` with the runaway's `HERON_CLIENT_ID` | Idle: *The Heron add-in in Revit 2020, session <pid> is older than this one and does not take a stop that names its Revit. Nothing was stopped. ...*, exit 1; a write preview there is not refused `stopped`, and its log has no *Stop asked over the bridge*. Mid-run: the same sentence after Revit's busy wait (`revit.busyTimeoutSeconds`, 10 s by default) rather than at once - the old add-in queues the name it does not know behind the job - and the runaway runs on to `ran_too_long`, rolled back by its own clock; the other Revit is untouched throughout. **A FAIL** - the old Revit answering *Heron is stopped* - means the named stop reads as a broadcast to an older add-in, the skew its new name was chosen to prevent. Update that Revit's add-in afterwards; neither needs a restart after a pass |
| **EZ10** | **A name this add-in does not know is answered `unknown_op` at once while a job holds Revit's thread - not `revit_busy`** ([row 5b-1742](../fragment-issues/row-5b-1742.md); the add-in rebuilt and redeployed from 2026-11-16). EZ3's arrangement on one Revit, one client id in both terminals. A: `set HERON_CLIENT_ID=ez10` then the EZ1 command. B, within ten seconds of A starting: `set HERON_CLIENT_ID=ez10` then `python -c "import sys, time; sys.path.insert(0, 'mcp/client'); import heron_bridge_client as c; b = [x for x in c.discover()[0] if x.pid == <pid>][0]; t = time.time(); print(b.request('no_such_operation'), round(time.time() - t, 1))"`. Once A has its answer and Revit is idle, B's line again | B's first answer comes back **within a second, while A is still waiting**: `unknown_op`, *Heron has no operation called 'no_such_operation'. It has: ...* - and the second, with Revit idle, is the same sentence word for word. The activity banner goes on naming A's probe alone - nothing is raised for B's request - and the audit trail (`audit-YYYYMM.jsonl`) has a refused `no_such_operation` carrying `unknown_op`. A ends `ran_too_long` as in EZ1. **A FAIL** - B told after about 10 s (`revit.busyTimeoutSeconds`) that *Revit is busy ... A dialog may be open* - is the add-in as it stood, or an older one deployed: check the add-in is this one before anything else. No restart needed after a pass |

**`stop` goes to ONE Revit since 2026-10-18 - and with two connected it must be named.** Until then the
client's `stop` took no session and was sent to every connected Revit; on 2026-10-18 one meant for Revit 2024
stopped another chat's idle Revit 2020 ([row 5b-1094](../fragment-issues/section-5b-rows-1076-1100.md)). Now
`stop --session <pid>` goes to that Revit, and with more than one Revit connected and no `--session` the
client **sends nothing** and lists them - the safe default, because a stop lasts until Revit restarts and a
guess costs another chat a restart. One Revit connected and no `--session` was stopped as before, and EZ6
is EZ3 with two.

**Since 2026-11-16 a stop must name the exact Revit session it is for** ([Q-71](../open-questions/tier-2.md),
decided 2026-11-15 on the owner's delegation). With no `--session` the client sends nothing, one Revit
connected or two, and lists them. The request is `stop_session`, carrying that Revit's own session
(`2024/63712`, release and process id), and the add-in stops only when it is its own: it refuses, with
nothing stopped, the old `stop`, which named none (`stop_unnamed`), and a session not its own
(`stop_wrong_session`). An add-in from before the change does not know `stop_session` and is not stopped by
it - the client says that Revit's Heron is older. So **EZ1 to EZ4's `stop` reads `stop --session <pid>`**
with this add-in deployed; EZ6's named stop needs this add-in on the Revit it stops, and EZ7 is EZ6 with it
on both. No lease change: a stranger who names the session still stops an idle Revit - a choice somebody
made, which the decision keeps - so EZ6's *Do not* stands. `tests/test_bridge_stop.py` holds the client and
reads the add-in's source; no Revit has run either (EZ7 to EZ9).

**What none of these rows can show,** and is said so that a green run is not read as more: a single long
Revit API call - a Regenerate, a LoadFamily, an export - is not interrupted by the clock or by `stop`,
because no check runs inside it; an expression-bodied lambda and a LINQ query carry no check; and nothing
in the MCP server sends `stop` yet, so a chat cannot be stopped from the chat - only from a second terminal
holding its client id. [D-129](../decisions/D-129.md) lists them.

### The 2026-10-18 run

**On the owner's laptop, 2026-10-18.** Revit 2024.3 session 63712, and for the rows marked 2027 Revit 2027.3
session 22024, both on add-in `0.1.0+48230c8c` - `main`, Debug, deployed 08:45 local by this run with
`tools/deploy-addin.ps1` for 2024, 2026 and 2027. Revit 2020 session 55496 belonged to another chat and ran
`ab77d5fb`; **nothing here ran on Revit 2020.** Models, all scratch copies in `%TEMP%\heron-work\safety-1018\models`:
*Heron Safety 2024* (Autodesk's `rme_basic_sample_project.rvt`, via the move-rails chat's *Rails-X2*; 21,562
elements at open), *Heron Guard B 2024* (a copy of *Rails-S*, the Dynamo sample) and *Heron Safety 2027* (Revit
2027's `Snowdon Towers Sample HVAC.rvt`, links unresolved; 9,813 elements). Changes, Admin and Publish on except
where a row says. **The owner pressed every Keep, Discard, Esc, X and Undo himself;** nothing answered a box for
him. Undo lists were read with UI Automation, read-only. Throwaway probe fragments were made in `brain/fragments/`
for the session and deleted by name at the end - none is committed.

The probes `zz-ej-runaway-probe` and `zz-ej-swallow-probe` were written as the group says, run with `--write`
and **no** `--apply` - D-117 refuses `--apply` for a DRAFT before anything is sent - and deleted at the end.

| # | Release | What was run and what came back | State |
|---|---|---|---|
| EZ1 | 2024 | Runaway: `ran_too_long` after 50 s, *"set inside the 60 seconds Heron waits"*, *"Revit confirmed the rollback"*; 21,568 elements before and after; no probe entry in the Undo list. Swallow: the same at 51 s | **PASS** |
| EZ2 | 2024 | Revit came back by itself: the same process (63712), `ping` 1 ms, a `count` straight after. The banner was not watched | **PASS** |
| EZ3 | 2024 | Positive: the runaway (client `ajmal-pc`) and, 6 s later, `stop` with the same id: *"Heron is stopped. Revit is running a job now ..."* at once; the runaway `stopped_mid_run` six seconds after it started, rollback confirmed. Then a `create-duct` preview: `stopped`, nothing sent; a `count` still answered. Negative: a stranger's id while the runaway ran - Revit 2024 refused, `in use by another chat`, and the runaway ran on to `ran_too_long`. **But `stop` takes no session, and went to every Revit:** Revit 2020 (another chat's, lease free) took the stranger's stop - *"Heron is stopped. Nothing was running."* - and stays stopped until restarted ([5b-1094](../fragment-issues/section-5b-rows-1076-1100.md)). The after-restart half not run | **PASS** on 2024; the finding is 5b-1094 |
| EZ4 | 2027 | EZ1's runaway: `ran_too_long` at 50 s, rollback confirmed, same process, `ping` 0 ms, the model still saved. EZ3 not run on 2027 - a stop reaches every Revit. 2020 not run | **PASS** (EZ1) |
| EZ5 | - | Both probes deleted; `tools/check-routing.py --targets` run with this change's gates | see the PR |
