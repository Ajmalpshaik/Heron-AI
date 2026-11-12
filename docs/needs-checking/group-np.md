# Needs checking — Group NP

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group NP - `revit_health` says, under each connected Revit, whether that Revit will ask Keep or Discard: the add-in's `info` answer now carries the Ask to Keep switch (2026-11-12)

The owner's *Ask to Keep* switch ([D-126](../decisions/D-126.md), [D-138](../decisions/D-138.md), in Heron's
Settings window since [D-150](../decisions/D-150.md)) is read inside the add-in at the moment a change is
kept, and it was sent to nobody. A chat could not say beforehand *"Revit will ask you to Keep or Discard"*
or *"this is kept at once, and Undo takes it back"*; it learned which only afterwards, from the verdict.

On 2026-11-12 the add-in's `info` answer ([`BridgeServer.cs`](../../revit/Heron.Bridge/BridgeServer.cs))
gained one optional field, `askToKeep`, from `HeronPermissions.AsksBeforeKeeping` - the reader the
executor decides by, which loads the settings on every call. `info` takes no lease and no model, so asking
claims nothing, and nothing on the wire can set the switch. `revit_health` prints one line under each
connected Revit ([`heron_health.py`](../../mcp/server/heron_health.py) `ask_to_keep_line`): the switch is
ON, it is off, *this Revit's Heron does not say* (an add-in that answered and sent no such field), or *Heron
could not read it from this Revit just now* (no usable answer). The protocol number is unchanged: a client
that does not know the field ignores it, and neither of the last two lines is ever a default.

**On and off are said of a change made through `revit_change`, and name what is different** - corrected the
same day, after a review found the first wording untrue of four kinds of change: `revit_apply_move` never
reads the switch and keeps the preview it showed ([D-138](../decisions/D-138.md) item 9); an export, save or
sync has left the model before the box opens, so neither Discard nor Undo takes it back
([D-126](../decisions/D-126.md)); a delete is asked in its own Delete box whatever the switch says
([D-142](../decisions/D-142.md)); and a change Revit counted nothing in, or the rest of a job the modeller
chose to keep, is kept unasked.

**Built, compiled for every supported release, NOT YET RUN IN REVIT.**

| What changed | Held without Revit by | What that cannot see |
|---|---|---|
| `info` carries `askToKeep`, read at the moment of the call | [`tests/test_bridge_roundtrip.py`](../../tests/test_bridge_roundtrip.py), *Ask to Keep, said by info*: the real bridge, a settings file of its own, the switch flipped under a running host - Linux only | the Windows settings folder, the real Settings window writing the file, and a real Revit's add-in |
| The client reads true, false, *not sent* or *not read*, and the four lines | [`tests/test_ask_to_keep_health.py`](../../tests/test_ask_to_keep_health.py) | which of them a real Revit sends |
| `revit_health` says the line under each Revit | [`tests/test_mcp_serves.py`](../../tests/test_mcp_serves.py), the real tool against four stand-in Revits | a real reply, and how the lines read in a chat |

**What the line does not say, recorded and not built** ([row 5b-1579](../fragment-issues/row-5b-1579.md)):

- **A settings file the add-in could not read is reported as off.** `HeronConfig.Load` gives the defaults when
  the file cannot be read, so `info` says `false`. The executor reads through the same call and would keep
  without asking at that instant, so the line matches what Revit would do - but it is the default, not the
  owner's setting, and it is not *does not say*.
- **A job's *keep the rest* grant is not reported.** The ON line names it in words; whether one is held now
  lives in the add-in (`HeronKeepGrant`) and `info` does not carry it.
- **No tool description tells a chat the line exists.** The always-loaded descriptions are at their budget
  ([D-118](../decisions/D-118.md)), so `revit_health`'s was not lengthened.
- **`tests/test_keep_question.py` counts the settings key's bare name as text**, so a keyword-argument send
  under another spelling would pass it. The new suite reads the Python for `askToKeep` as a string, a keyword
  or a name; the older check is unchanged.

**The PC.** A **SAVED scratch copy** of a sample model, never the owner's own file; NP4 keeps two small
changes in it. Rough work goes in `%TEMP%\heron-work\np`, never beside Heron. **Run NP3 first**: it needs one
Revit release still running the add-in as it was before this change, so deploy the new add-in to **one**
release only (`tools/deploy-addin.ps1`), run NP3, then deploy to the rest. Restart the Claude app after
pulling, so the chat runs the new server. The switch is turned in Revit - *Heron > AI Bridge > Settings*, the
*Ask to Keep* row - and never by editing `heron.config`.

| # | Run | Look for |
|---|---|---|
| **NP1** | *Positive - on is said on.* In the Revit with the new add-in, turn **Ask to Keep ON** in Settings. In the chat ask *"is Heron connected?"* | Under that Revit's numbered line, one line that starts **`Ask to Keep is ON in this Revit right now`** and says a change made through `revit_change` is shown in Revit's Keep or Discard box. Exactly one such line for that Revit |
| **NP2** | *Negative - off is said off, with nothing restarted.* Straight after NP1, turn **Ask to Keep off** in Settings (answer *Stop asking*). Do **not** restart Revit or the chat. Ask *"is Heron connected?"* again | The line now starts **`Ask to Keep is off in this Revit right now`** and says a change made through `revit_change` is kept at once, as one entry in Revit's Undo list. If it still says ON, the add-in answered from an earlier read and the row fails. Turn it ON and ask once more: ON again |
| **NP3** | *An older Heron does not say - run first.* Two Revits connected: one release with the new add-in, one still on the add-in from before 2026-11-12. Ask *"is Heron connected?"* | Under the older one: **`Ask to Keep: this Revit's Heron does not say which way the switch is set`**, and that its add-in is older. It must not say ON, and must not say off - whichever way the switch really is. Under the newer one: its true state. Note both releases and both add-in versions in the result |
| **NP4** | *The line is true of what happens next.* In the scratch copy, with Changes on. Ask to Keep **ON**: ask *"is Heron connected?"*, read the line, then ask the chat for a level `NP On`. Then Ask to Keep **off**: ask *"is Heron connected?"* again, read the line, then ask for a level `NP Off` | ON: the Keep or Discard box comes up before anything is kept, as the line said; press **Discard** and nothing is kept. Off: no box, the level is kept at once, and Revit's Undo list has one new entry that takes it back. A box after the off line, or none after the ON line, fails the row |
| **NP5** | *Asking takes nothing.* A second chat connected to the same Revit and holding it (it has just read something). In the first chat ask *"is Heron connected?"* | The first chat's reply carries the Ask to Keep line for that Revit and says it is in use by another chat; the second chat's next request is not refused, and it was not cut off |
| **NP6** | *On the laptop, whose Claude app keeps its own copy of the settings ([Q-70](../open-questions/tier-2.md)).* A Revit the owner started himself. Turn Ask to Keep ON in it, ask *"is Heron connected?"*, then open the copy of `heron.config` under the app's `LocalCache` and read `write.askBeforeKeeping` | The line says **ON** - what that Revit's own Settings show - whatever the copy holds. Record both values. This row does not settle Q-70: the write-gate line is still read from the server's file |

| **NP7** | *What the ON line says is different, is.* Ask to Keep **ON**, Changes on, the scratch copy. (a) select two walls and ask the chat to delete them; (b) ask the chat to move the ducts 200 mm - preview, then apply | (a) a **Delete** box of its own, naming how many go; after **Delete** no Keep or Discard box follows. (b) the preview is shown, the apply is kept, and **no** Keep or Discard box comes up. Either one arriving in the Keep or Discard box means the line's list is wrong |
| **NP8** | *A Revit that cannot answer is not called old.* With the new add-in connected, make its answer unusable for one check - the simplest way found on the day; say which - and ask *"is Heron connected?"* | If the Revit is still listed, its line is **`Ask to Keep: Heron could not read which way the switch is set from this Revit just now`** and does not say its add-in is older. If no way to arrange it is found, **NOT RUN**, said so - `tests/test_mcp_serves.py` holds the sentence against a stand-in |

**Then** the group closes on NP1 to NP8. A row that fails reopens the change; nothing in a row changes to
make it pass. NP6 can only be run on a PC with a packaged Claude app; on any other it is **NOT RUN**, said so.
