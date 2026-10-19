# Needs checking — Group HK

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-18 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group HK - one model, one chat at a time, and Free this model: D-141 (2026-10-18)

[D-141](../decisions/D-141.md) (the owner's design, 2026-10-18) from
[row 5b-1150](../fragment-issues/section-5b-rows-401-425.md). **What changed.** Every request carries
the chat's own `conversation` id beside the person's `client` id, which the D-22 lease still uses. A model
a chat's request ran against is **held** for that chat; another chat asking for it - read or change - is
refused *"Model 'X' is in use by another chat ... Nothing was sent to the model."* before anything runs.
One chat may hold several models. A hold ends after **3 quiet minutes** (`bridge.modelHoldMinutes`), when
the chat calls `revit_release`, when the model or Revit closes, when the bridge is disconnected, and with
**Free this model** on the Heron ribbon, whoever holds it. The carried chain and the Ask to Keep grant
(D-138) are now each chat's own.

**What is tested and not run.** It compiles for 2020 to 2027. The hold's rule is run without Revit by
`tests/Heron.ModelHold.TestHost`; the envelope, the MCP tool, the failure's class and the wiring by
`tests/test_model_hold.py`. **Nothing here has run in Revit.** The owner says when these are run.

**Setup.** The add-in rebuilt from this change and deployed with Revit closed. One Revit with **two
saved scratch models** open - call them *A* and *B*. Two chats, **chat 1** and **chat 2**, both opened
after the change (a chat opened before it runs the old Python - mcp/README rule 5). Changes ON for the
rows that change anything.

| # | Run | Look for |
|---|---|---|
| **HK1** | *Second chat refused.* With *A* in front, ask chat 1 *"how many levels"*; within a minute ask chat 2 the same | Chat 1 answers about *A*. Chat 2 is refused *"Model 'A' is in use by another chat. It is free when that chat says it is done, after 3 quiet minutes, or with Free this model on the Heron ribbon ... Nothing was sent to the model."* The banner over Revit says *"Another chat is using this model - nothing was sent"*. The add-in log says *"Model hold: 'A' is now held by chat ..."* and then *"refused to chat ..."*. **The D-30 negative:** chat 1 asking again is answered, not refused |
| **HK2** | *Another model in the same Revit works.* Right after HK1, bring *B* to the front and ask chat 2 *"how many levels"* | Chat 2 answers about *B*, not refused. Chat 1, asked about *A* again, still answers - each chat holds its own model |
| **HK3** | *Three quiet minutes free it.* After HK1, send nothing from chat 1 for more than three minutes; then ask chat 2 about *A* | Chat 2 is answered about *A* - chat 1 was never closed. Then ask chat 1 about *A*: now chat 1 is refused, naming *A* |
| **HK4** | *"Done" frees it.* With chat 1 holding *A* (ask it one question), tell chat 1 *"I'm done with this model"* - it calls `revit_release`; at once ask chat 2 about *A* | `revit_release` answers *"Freed A. Another chat can use it now; this chat can come back to it later. Nothing in the model was changed."* Chat 2 is answered at once, inside the three minutes. **The negative:** `revit_release` from chat 2 while chat 1 holds *A* frees nothing and says this chat held none |
| **HK5** | *The ribbon button frees it.* With chat 1 holding *A* and *A* in front, press **Free this model** on the Heron tab; at once ask chat 2 about *A* | No window, no popup. The add-in log says *"Free this model: 'A' freed from chat ..."*. Chat 2 is answered at once. On the start screen with no model open the button is greyed. Pressed again with nothing held, the log says *"'A' was not held by any chat. Nothing changed."* |
| **HK6** | *A model closing frees it.* With chat 1 holding *B*, close *B*, open it again, and ask chat 2 about *B* | The log says *"'B' closed, so chat ... no longer holds it."* Chat 2 is answered |
| **HK7** | *The carried chain is not shared.* Chat 1: select ducts in *A* with a selecting tool that leaves its elements for the next. Free *A* (HK4), then in chat 2 run a tool on *A* that consumes what an earlier one left, naming chat 1's tool in `expect_from` | Chat 2 is refused - nothing carried - because what chat 1 left is chat 1's. In chat 1 the same consumer, after its own select, binds the ducts |
| **HK8** | *The Keep grant is not shared.* Ask to Keep ON. Chat 1 asks a PROVEN change on *A* and presses **Keep, and keep the rest of this job without asking**. Chat 1 then frees *A* (`revit_release`), and chat 2 asks a change on *A* within ten minutes | Chat 2's change puts **the box up**, and the box says the grant ended because this change comes from another chat; the log says so. Before D-141 this change was kept without asking. Chat 1, back again, is asked too |
| **HK9** | *An older chat.* A chat opened before this change, on a model nobody holds, asks a question; then chat 1 asks about the same model | The older chat is answered as before and holds nothing, so chat 1 is answered too. With chat 1 holding the model, the older chat is refused. With Ask to Keep ON, the older chat's box never shows the third link |
| **HK9a** | *Asking whether Heron works takes nothing.* With nobody holding *A*, ask chat 2 *"is Revit connected?"* (`revit_health`); at once ask chat 1 about *A* | `revit_health` names *A* and its count. Chat 1 is answered - the health check claimed nothing. With chat 1 holding *A*, chat 2's `revit_health` line for that Revit says *A* is in use by another chat |
| **HK10** | *A command line beside a chat.* Chat 1 holds *A*. From a terminal, `python mcp/client/heron_bridge_client.py fragment list-levels` with *A* in front; then `revit_release` in chat 1 and the same command again; then the command twice in a row | The first is refused *"in use by another chat"* - a command line is a conversation of its own. After `revit_release` it answers. Two commands in a row both answer: each hands back what it held when it ends |

**Rows of other groups this changes.** [HG5](group-hg.md) still runs as written, but chat B is refused for
up to three minutes after chat A's last request unless chat A says it is done or **Free this model** is
pressed. [HG7](group-hg.md) set `HERON_CLIENT_ID` to make `try-once` *"the same chat"*; since D-141 that
id names the person, and a command line is a conversation of its own - so `try-once` there is refused while
the chat holds the model, and once the chat frees it the try-once is asked as **another chat**, ending the
grant. HG7 as written cannot show what it was written to show; it needs the chat's own route to a DRAFT's
one run, which Heron does not have.
