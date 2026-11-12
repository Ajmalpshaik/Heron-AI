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
ON, it is off, or *this Revit's Heron does not say*. The protocol number is unchanged: a client that does not
know the field ignores it, and an add-in that does not send it gets the third line, never a default.

**Built, compiled for every supported release, NOT YET RUN IN REVIT.**

| What changed | Held without Revit by | What that cannot see |
|---|---|---|
| `info` carries `askToKeep`, read at the moment of the call | [`tests/test_bridge_roundtrip.py`](../../tests/test_bridge_roundtrip.py), *Ask to Keep, said by info*: the real bridge, a settings file of its own, the switch flipped under a running host - Linux only | the Windows settings folder, the real Settings window writing the file, and a real Revit's add-in |
| The client reads true, false or nothing, and the three lines | [`tests/test_ask_to_keep_health.py`](../../tests/test_ask_to_keep_health.py) | which of them a real Revit sends |
| `revit_health` says the line under each Revit | [`tests/test_mcp_serves.py`](../../tests/test_mcp_serves.py), the real tool against three stand-in Revits | a real reply, and how the lines read in a chat |

**The PC.** A **SAVED scratch copy** of a sample model, never the owner's own file; NP4 keeps two small
changes in it. Rough work goes in `%TEMP%\heron-work\np`, never beside Heron. **Run NP3 first**: it needs one
Revit release still running the add-in as it was before this change, so deploy the new add-in to **one**
release only (`tools/deploy-addin.ps1`), run NP3, then deploy to the rest. Restart the Claude app after
pulling, so the chat runs the new server. The switch is turned in Revit - *Heron > AI Bridge > Settings*, the
*Ask to Keep* row - and never by editing `heron.config`.

| # | Run | Look for |
|---|---|---|
| **NP1** | *Positive - on is said on.* In the Revit with the new add-in, turn **Ask to Keep ON** in Settings. In the chat ask *"is Heron connected?"* | Under that Revit's numbered line, one line that starts **`Ask to Keep is ON in this Revit right now`** and says Revit shows a Keep or Discard box. Exactly one such line for that Revit |
| **NP2** | *Negative - off is said off, with nothing restarted.* Straight after NP1, turn **Ask to Keep off** in Settings (answer *Stop asking*). Do **not** restart Revit or the chat. Ask *"is Heron connected?"* again | The line now starts **`Ask to Keep is off in this Revit right now`** and says a change is kept at once and Undo takes it back. If it still says ON, the add-in answered from an earlier read and the row fails. Turn it ON and ask once more: ON again |
| **NP3** | *An older Heron does not say - run first.* Two Revits connected: one release with the new add-in, one still on the add-in from before 2026-11-12. Ask *"is Heron connected?"* | Under the older one: **`Ask to Keep: this Revit's Heron does not say which way the switch is set`**. It must not say ON, and must not say off - whichever way the switch really is. Under the newer one: its true state. Note both releases and both add-in versions in the result |
| **NP4** | *The line is true of what happens next.* In the scratch copy, with Changes on. Ask to Keep **ON**: ask *"is Heron connected?"*, read the line, then ask the chat for a level `NP On`. Then Ask to Keep **off**: ask *"is Heron connected?"* again, read the line, then ask for a level `NP Off` | ON: the Keep or Discard box comes up before anything is kept, as the line said; press **Discard** and nothing is kept. Off: no box, the level is kept at once, and Revit's Undo list has one new entry that takes it back. A box after the off line, or none after the ON line, fails the row |
| **NP5** | *Asking takes nothing.* A second chat connected to the same Revit and holding it (it has just read something). In the first chat ask *"is Heron connected?"* | The first chat's reply carries the Ask to Keep line for that Revit and says it is in use by another chat; the second chat's next request is not refused, and it was not cut off |
| **NP6** | *On the laptop, whose Claude app keeps its own copy of the settings ([Q-70](../open-questions/tier-2.md)).* A Revit the owner started himself. Turn Ask to Keep ON in it, ask *"is Heron connected?"*, then open the copy of `heron.config` under the app's `LocalCache` and read `write.askBeforeKeeping` | The line says **ON** - what that Revit's own Settings show - whatever the copy holds. Record both values. This row does not settle Q-70: the write-gate line is still read from the server's file |

**Then** the group closes on NP1 to NP6. A row that fails reopens the change; nothing in a row changes to
make it pass. NP6 can only be run on a PC with a packaged Claude app; on any other it is **NOT RUN**, said so.
