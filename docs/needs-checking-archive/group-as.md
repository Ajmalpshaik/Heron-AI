# NEEDS-CHECKING archive — Group AS

> **Checks that were done, moved out of the live register.** These are rows of Group AS of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *Talk: from Revit into the open chat, with the selection saved (2026-09-27)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row AS1

*Moved from the register on 2026-11-17.*

**Check.** Deploy the add-in to Revit 2024, open a test copy, and look at **Heron > AI Bridge**

**Expected.** **PASSED 2026-09-27 on the owner's PC, Revit 2024** - the verdict is in *Group AS was RUN*, below. Talk was removed that evening ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)). *It was to look like:* A **Talk** button with the speech-bubble picture, enabled with a model open. **FAIL** - no button, or the whole tab missing - is the ribbon code: the add-in log names why

---

### Row AS2

*Moved from the register on 2026-11-17.*

**Check.** In the Heron folder, double-click `mcp\heron-talk.cmd`

**Expected.** **PASSED 2026-09-27 on the owner's PC, Revit 2024** - the verdict is in *Group AS was RUN*, below. Talk was removed that evening ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)). *It was to look like:* Claude Code shows its development-channels warning naming `server:heron`; choose *I am using this for local development*. It then notes that messages from `server:heron` inject into the session, `/mcp` shows `heron` connected, and `%LOCALAPPDATA%\Heron\talk\listener-<pid>.json` exists and its time moves every few seconds. **FAIL** - no such note, or a *blocked by org policy* line - means Talk cannot work on this account yet: write down the exact words and the Claude Code version

---

### Row AS4

*Moved from the register on 2026-11-17.*

**Check.** **The positive.** The AS2 chat open, Heron connected, three ducts selected in the test copy. **Talk**: *how many are selected and what size are they* - Enter

**Expected.** **FAILED 2026-09-27 on the owner's PC** - the message was saved and never shown, and when the chat closed it was claimed and lost; the verdict is in *Group AS was RUN*, below. Talk was removed that evening ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)). *It was to look like:* The box closes at once and Revit is usable. Within about a second the terminal shows the message arriving from `heron`; Claude reads the saved selection with `heron_selection` and answers about **three ducts in this model**. The talk folder now holds `message-1.taken.json` and `selection-1.json`. **FAIL** - nothing arrives - is the channel; Claude answering about the live selection instead of the saved one is the instructions

---

### Row AS5

*Moved from the register on 2026-11-17.*

**Check.** **Revit does not wait, and "these" means what was sent.** Straight after an AS4-style message, click around Revit and select two walls instead. Then **Talk**: *select the ones I sent you*

**Expected.** **CANNOT BE RUN from 2026-09-27.** Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 failed; this was never run. *It was to look like:* Revit stayed responsive the whole time, apart from Heron's banner while a tool ran. The ducts from the message are selected - **not** the walls. **FAIL** - the walls, or Revit frozen until the answer - breaks the two things D-104 promised

---

### Row AS6

*Moved from the register on 2026-11-17.*

**Check.** In the Talk box, press **Win+H** and say *select all ducts on level one*

**Expected.** **CANNOT BE RUN from 2026-09-27.** Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 failed; this was never run. *It was to look like:* The words appear in the box, and Enter sends them. **Record either way**: the box is a transparent, borderless WPF window, and whether Windows voice typing writes into it has not been seen. Step 2 must not lean on Win+H if this fails

---

### Row AS7

*Moved from the register on 2026-11-17.*

**Check.** **Changes still guard the model.** With **Changes off**, **Talk**: *move these up 200* on three selected ducts

**Expected.** **CANNOT BE RUN from 2026-09-27.** Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 failed; this was never run. *It was to look like:* Heron refuses the change and Claude says so; nothing moved. With **Changes ON**, the same message moves them, and **one Ctrl+Z** puts them back. **FAIL** - a move with Changes off - would mean pre-allowing Heron's tools bypassed Heron's own gate, which D-104 says it must not

---

### Row AS8

*Moved from the register on 2026-11-17.*

**Check.** **Two Revits.** Two Revits open and connected; in the chat, choose the first with `revit_use_session`. Then **Talk** from the second

**Expected.** **CANNOT BE RUN from 2026-09-27.** Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 failed; this was never run. *It was to look like:* The message arrives with a note that it came from a different Revit from the one chosen, and Claude **asks** before sending anything to Revit. Then close both chats, start a fresh Talk chat, and Talk from the second Revit: the chat binds to the second, as chosen

---

### Row AS9

*Moved from the register on 2026-11-17.*

**Check.** Close Revit

**Expected.** **CANNOT BE RUN from 2026-09-27.** Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 failed. Half of it was seen anyway - see *Group AS was RUN*, below. *It was to look like:* `%LOCALAPPDATA%\Heron\talk\<that Revit's pid>` is gone, and `heron_selection` in the chat says nothing is saved from it

---

### Row AS10

*Moved from the register on 2026-11-17.*

**Check.** AS1, AS3 and AS4 on **Revit 2020** and **Revit 2027**

**Expected.** **CANNOT BE RUN from 2026-09-27.** Talk was removed ([D-105](../DECISIONS.md#d-105--talk-is-removed-and-the-modeller-works-with-heron-in-the-chat-only)) after AS4 failed; this was never run. *It was to look like:* The same - the box is WPF on .NET Framework 4.7.2 at one end and .NET 10 at the other

---
