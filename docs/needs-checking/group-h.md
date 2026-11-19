# Needs checking — Group H

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group H — the lease (needs TWO chats and one Revit)

**Tracked as GitHub issue [#971](https://github.com/Ajmalpshaik/Heron-AI/issues/971) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

Changes behaviour proven in Step 1, so it is the group most likely to surprise you.

**Do `A4` first.** The round-trip test already covers the refusal, the `ping`/`info` exemption, the
`inUse`/`mine` reporting and that the first chat is never cut off — all without Revit. What is left here
is only what genuinely needs real Revits and real chats: the picker column, expiry over real time, the
button releasing it, and two Revits not interfering.

| ID | Do this | Pass looks like |
|---|---|---|
| **H1** | One chat, one Revit. Ask anything | Works exactly as before. The lease is claimed silently — you should notice nothing |
| **H2** | Open a **second** Claude chat, connect to the **same** Revit, ask anything | **Refused**, saying the Revit is in use by another chat and roughly when it frees. It must NOT cut the first chat off. *(Mostly covered by `A4` — this confirms it end to end through a real chat)* |
| **H3** | Go back to the **first** chat, ask again | Still works. It never lost its hold |
| **H4** | In the second chat, run `revit_health` | Shows the Revit — `ping`/`info` are lease-exempt, so *looking* must never claim it. *(The exemption itself is covered by `A4`; this checks the tool uses it)* |
| **H5** | Two Revits open, one held by another chat. Ask for the picker | The rows read `(free)` and `(in use by another chat, ~N min left)`. **This is the column that did not exist before** |
| **H6** | Leave the first chat idle over 5 minutes, then ask from the second | Now granted — the lease lapsed. An abandoned chat must not hold a Revit forever |
| **H7** | First chat holding it, press the **Heron button** to disconnect, then ask from the second | Granted immediately. Pressing the button releases it rather than making anyone wait out the timer |
| **H8** | Two chats, **different** Revits (2020 and 2024) | No interference at all. The lease is per process |
| **H9** | `revit_health` from a chat holding **nothing**, with a free Revit open. Then ask from a *second* chat | The second chat is **granted**. A health check must NOT have claimed the free Revit — that bug existed for one commit: it called `count_elements` on every session, which is not lease-exempt |
| **H10** | First chat mid-request, second chat connects and is refused. Watch the FIRST chat | It loses that one reply and recovers on the next. Known limitation, [docs/25](../25-multi-session-and-binding.md): the pipe is displaced at connect, before the lease can speak. If a **write** was in flight it must report the outcome as *unknown*, never as failed |
| **H11** | **A job waiting for the modeller keeps its chat's lease past `bridge.leaseMinutes`** ([row 5b-1745](../fragment-issues/row-5b-1745.md); the add-in rebuilt and redeployed from 2026-11-16). One Revit on a scratch project arranged as [NH0](group-nh.md), *Level 1* in front, `bridge.leaseMinutes` at its default 5, two terminals in the Heron folder. **A:** `set HERON_CLIENT_ID=h11-a`, then `python mcp/client/heron_bridge_client.py fragment pick-on-screen` - and leave Revit's prompt unanswered. **B,** at about 5 minutes 30 seconds, timed from A's start: `set HERON_CLIENT_ID=h11-b`, then `python mcp/client/heron_bridge_client.py list`, then `python mcp/client/heron_bridge_client.py count`. Then, before six minutes have passed, click one end of the line in Revit; once A has printed its point, B's `count` again | B's `list` reads `(in use by another chat, ~5 min left)`, and B's first `count` is **refused at once**, nothing sent - *This Revit is in use by another chat ... That chat's job is waiting for an answer in Revit - a box to answer, or a click ...*. A gets its point as in NH1 and hands the Revit back as it always has; B's second `count` then runs. **Before this change** B's first `count` took the lapsed lease and came back about 10 s later as `revit_busy`, and from then the Revit was B's: a request from A was refused `session_in_use` until B's lease lapsed. **A FAIL** is that old answer: check the add-in deployed is this one. The same holds for a Keep or Discard box or the delete box left open: all three waits go through `RevitDispatcher.AwaitingModeller` |
