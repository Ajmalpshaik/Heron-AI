# Needs checking — Group OR

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OR - a resumed chat takes back its model, health names who holds it and which model a change goes to, the panel's outcome line, and two add-in fixes (2026-11-20)

**Why:** issues [#1033](https://github.com/Ajmalpshaik/Heron-AI/issues/1033), [#1031](https://github.com/Ajmalpshaik/Heron-AI/issues/1031),
[#1043](https://github.com/Ajmalpshaik/Heron-AI/issues/1043), [#953](https://github.com/Ajmalpshaik/Heron-AI/issues/953),
[#952](https://github.com/Ajmalpshaik/Heron-AI/issues/952) and [#1082](https://github.com/Ajmalpshaik/Heron-AI/issues/1082),
worked in a cloud container with no Revit on 2026-11-20. **What is tested and not run:** every project
compiles for Revit 2020 to 2027 (`tools/check-compile.py`); the hold's rule is run without Revit by
`tests/Heron.ModelHold.TestHost`; the bridge's `info` on the wire by `tests/test_bridge_roundtrip.py`; the chat side by `tests/test_model_hold.py`, `tests/test_session_binding.py`,
`tests/test_outcome_line.py`, `tests/test_delete_box.py` and `tests/test_category_by_name.py`. **Nothing here
has run in Revit.**

**What changed, in one line each:**

- **[D-186](../decisions/D-186.md), #1033** - the MCP server's conversation is made from the Claude app's own
  name for the chat, so a chat the app resumes after a restart takes back the model it held.
- **Row [5b-1645](../fragment-issues/row-5b-1645.md)** - the bridge's `info` says who holds the model in front
  (looked at, never claimed), and `revit_health` says *"the model in front is in use by another chat"* where
  it said *"(free)"*.
- **#1031** - `revit_health` names the model this chat is pinned to when it is not the one in front, and
  `revit_change` says so once, before its first write, for each pair of models (D-74 still aims the write at
  the pinned model).
- **#1043** - the Commands panel's answer carries the outcome line (in `main` since #1064; only the run is
  owed here).
- **Row [5b-1865](../fragment-issues/row-5b-1865.md)** - a second set named by category whose collector
  faults is refused, never read as an empty category.
- **Row [5b-1830](../fragment-issues/row-5b-1830.md)** - the run made again after Delete in Heron's delete
  box is bound from the selection the first run was bound from.

**Setup.** Update the checkout to the branch of this change (or `main` once it is merged), run
`python tools/heron-ready.py`, and start Revit. One Revit with **two saved scratch models** open - *A* and
*B* - never a live project. Two chats opened after the update - **chat 1** and **chat 2**. Changes ON for
OR3, OR4 and OR6.

| # | Run | Look for |
|---|---|---|
| **OR1** | *A resumed chat (#1033, D-186).* Chat 1 asks one question about *A*. Within a minute, quit the Claude app, open it again and RESUME chat 1 - the same conversation, not a new one. Ask it about *A* again at once | Answered, not refused. **The negative:** a NEW chat 2, asked about *A* in the same minute, is refused *"Model 'A' is in use by another chat"* - two chats are still two. If chat 1 is refused after the resume, the app gave the resumed chat a new session id, and that is the finding |
| **OR2** | *Health says who holds the model (5b-1645).* Chat 2 asks a question about *A* with *A* in front. Chat 1, which has not used this Revit yet, asks *"is Revit connected?"* | `revit_health` lists that Revit *"(the model in front is in use by another chat, ~3 min left)"*, not *"(free)"*. **The negative:** chat 2 says it is done (`revit_release`), and chat 1's health then says *"(free)"* |
| **OR3** | *Health and the pin agree (#1031).* Chat 1 asks a question about *A*, so it is pinned to *A*. Bring *B* to the front. Ask chat 1 `revit_health`, then a small PROVEN change - duplicate a wall type, say | Health names *B* in front and says *"This chat works on A ... A change from this chat goes to A, not to the model in front"*. The change then runs in *A* - asked once by health, so not refused. In a NEW chat pinned to *A* with *B* in front and no health asked, the first change is refused *"... Nothing was sent to Revit"* naming both, and the same request again runs in *A*. **The negative:** with *A* in front, nothing is said and nothing refused |
| **OR4** | *The Commands panel's outcome line (#1043).* Save the 2026-11-18 command - `DUPLICATE_TYPE` with the ambiguous bare name `Generic - 300mm` - and run it from Heron's Commands panel | Its answer shows `outcome: NOTHING CHANGED` (or `REFUSED`) on its second line, and no *"was KEPT"*. **The negative:** a saved command that does change something shows `outcome: KEPT` |
| **OR5** | *A second set by category reads as before (5b-1865).* `CHECK_ROOM_MEP_COMPLETENESS` with rooms selected and `devices` = a category the model has, then one it has none of | The first answers as it did before this change; the second answers with an empty second set, not a refusal. The fault the change refuses cannot be made to happen on purpose - this row only shows the ordinary path did not move |
| **OR6** | *The run made again after Delete (5b-1830).* OE7 and OE9's kept halves as [Group OE](group-oe.md) writes them - a `try-once` that raises Heron's delete box - pressing **Delete** | The second run binds and the Keep or Discard box comes up; no `needs_unbound`. **The negative:** **Don't delete** deletes nothing and keeps nothing |

**After the runs:** write each result into this file - Revit release, model, date, what came back - and
strike an id at both ends (`~~**OR1**~~`) when it held. A failure stays, says what failed, and a wrong piece
of code becomes a row in the register.
