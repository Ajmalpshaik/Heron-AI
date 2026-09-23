# Needs checking — Group Z

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group Z - the renamed ribbon and the Bridge Status window, 2026-09-20

[D-85](../DECISIONS.md) renamed two ribbon labels and the Bridge Status dialog became a window.
**All of it compiles on all eight releases with 0 warnings, and none of it has been seen in Revit** -
the ribbon is built at `OnStartup`, so every row here needs the add-in deployed and Revit restarted.

**What was proved without Revit.** Neither window references a Revit API, so both were compiled
into a bare WPF host and rendered: the four Bridge Status states at `988x1246`, `988x1246`,
`988x1051` and `988x681`, and the Changes window at `988x877`, all at 2x, written to PNG and
looked at. Two faults were found that way and fixed - an
empty box under `THIS SESSION` when there is no identity, and a `Changes` card describing a Heron that
did not start. **That proves what it draws, not that Revit will host it**, and the rows below are the
difference between the two.

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**Z1**~~ | Deploy, start Revit, look at the ribbon — [full row](../needs-checking-archive/group-z.md#row-z1) | A tab reading Heron, holding a panel reading AI Bridge. |
| ~~**Z2**~~ | Press Bridge Status with the bridge connected — [full row](../needs-checking-archive/group-z.md#row-z2) | The window, not a TaskDialog. |
| ~~**Z3**~~ | Press it with the bridge NOT connected — [full row](../needs-checking-archive/group-z.md#row-z3) | Grey strip, lamp with no glow, `Not connected`, no `Pipe` row and no `ANNOUNCED IN`… |
| **Z4** | Press **Connect this session** inside the window | The window itself redraws to `Connected` - it does not close, and no second dialog appears. The **ribbon button's picture lights at the same time**, which is the whole reason the toggle was factored into `HeronBridgeToggle`. **HALF ANSWERED 2026-09-20.** The press happened and the bridge started - `Bridge listening on heron.2024.15920` at `19:39:39Z`, immediately followed by `Connected from Bridge Status`. **Neither half about the PICTURE is proved**: whether the window redrew to `Connected` rather than closing, and whether the ribbon icon lit, are both invisible to a log |
| **Z5** | Press **Copy details**, then paste somewhere | A short block of text with no empty values in it. One grey line appears in the footer and clears itself after six seconds |
| **Z6** | Turn **Changes** on from the ribbon, then open Bridge Status | The `Changes` card is amber, chip reads `ON`. Turn it off: grey, `OFF`. This is read from `heron.config` fresh each time the window opens, so it must agree with the ribbon padlock. **HALF ANSWERED 2026-09-20, and it is the half that could disagree.** The Revit 2020 screenshot shows the card amber with an `ON` chip, and the log for that same session reads `Write permission set to True from the ribbon` at `19:43:04Z`. So the window read the setting rather than remembering one, and it agrees with the ribbon. **The `OFF` appearance has only ever been seen in the harness**, never in Revit |
| **Z7** | Press `Esc`, and separately drag the window by its header | `Esc` closes it. The header drags it. Neither is Revit's, because the window has no system chrome at all |
| **Z8** | Look at it on a **150%** display | The one thing the render cannot answer. Text crisp, nothing clipped, the window centred on Revit rather than on the primary screen. Same unproven scaling path as `B17` above |
| ~~**Z9**~~ | Do Z1 and Z2 on 2020 as well as on a modern release — [full row](../needs-checking-archive/group-z.md#row-z9) | 2020 is `net472` and 2027 is `net10.0-windows` - two different WPF stacks under the same… |
| ~~**Z10**~~ | Press the Changes padlock while it is OFF — [full row](../needs-checking-archive/group-z.md#row-z10) | The Changes window, not a TaskDialog. |
| **Z11** | In that window press `Esc`, then the **X**, then **Leave it off** | All three leave it off. The padlock does not move and the log says `Write toggle: offered, declined. Still off.` **`Enter` must do NOTHING** - there is deliberately no default button, because a confirmation you can clear by leaning on a key is not one |
| ~~**Z12**~~ | Press **Turn changes on** | The padlock opens and its label changes to `Changes ON`. Open Bridge Status: the Changes card is amber and the chip reads `ON`, read from `heron.config` rather than remembered. **HALF PROVED 2026-09-20**: `Write permission set to True from the ribbon` at `19:39:50Z` is the setting moving, which is the half that matters. **What the log cannot see is the PICTURE** - whether the padlock redrew and whether Bridge Status then showed an amber chip. Still needs an eye |
| ~~**Z13**~~ | Press the padlock again, while it is ON — [full row](../needs-checking-archive/group-z.md#row-z13) | **No window at all.** It goes straight to off. Making the safe direction slower is how people learn to click through warnings, and D-19 says the ON direction is the only one that asks. **PROVED 2026-09-20 at `20:37`, by the logging that was added BECAUSE of this row.** The sentence this replaces said the log could not answer it, and that was true until both windows started recording that they opened. On Revit 2024 `pid 19600`:

```text
20:37:57Z  Bridge Status opened: Connected.
20:37:59Z  Write permission set to False from the ribbon.
20:38:00Z  Changes window asked.
20:38:01Z  Write permission set to True from the ribbon.
```

**No `Changes window asked` before the `False`**, and one before the `True`. Turning it off asked nothing; turning it on asked. That is D-19's shape, read off the machine |

**The button inside the panel keeps the name `Heron`.** Offered as a rename to `Connect` on 2026-09-20 and declined by the owner the same day, so the full path stays
`Heron > AI Bridge > Heron` and every printed string is already correct for it. Recorded so the repetition is not read later as an oversight worth tidying.

**Where the numbers came from.** The renders are reproducible: the harness compiles the three real
source files directly - `HeronBridgeStatusWindow.cs`, `HeronWindowStyle.cs` and
`HeronChangesWindow.cs` - with no copy of any of them, and nothing about it is checked in. It is a
scratch project and rebuilding it is six lines of csproj.

**The shared chrome was proved by rendering twice.** Moving the palette, the card, the title bar
and all three button templates out of Bridge Status and into `HeronWindowStyle` is a refactor of
code that already worked, so the four states were re-rendered afterwards and compared to the
earlier PNGs **byte for byte: 4 of 4 identical**. A refactor that moves no pixels is the only kind
worth doing to a window nobody has seen in Revit yet.

### Revit 2027 is not being checked, and that is a decision rather than an omission

**The owner's instruction, 2026-09-20:** *"some time for the revit 2027 is there issue... revit itself
its not opening or its getting stuck... i need to inform my company... no need to chek that."* Revit
2027 on that machine is unreliable in a way that has nothing to do with Heron, and he is raising it
with his company. So the .NET 10 half of `Z9` is **not** going to be answered by a person looking at a
screen, and nobody should later read the gap as work that was forgotten.

**What IS proved on .NET 10, and it is more than nothing.** `pid 14608` logged
`Heron loaded. Revit 2027, add-in 0.1.0.0` at `19:44:04Z`, nineteen minutes after that release's
assembly was installed at `19:24:56Z` - so it is the build carrying the renamed ribbon and both
windows. And that line is written **after** `BuildRibbon` returns: `OnStartup` builds the ribbon and
only then logs, wrapping the whole thing so a failure shows `Heron failed to start` and returns
`Result.Failed` instead. So on .NET 10:

- `CreateRibbonTab("Heron")` and `CreateRibbonPanel("Heron", "AI Bridge")` both succeeded,
- the split button, `Bridge Status`, the padlock and all four icons were created, and
- nothing in that path threw.

**What is NOT proved on .NET 10 is a window being DRAWN there** - `HeronWindowStyle`'s
`ControlTemplate`s, the `AllowsTransparency` card and the drop shadow. Those are proved on **net472**
and **net48**, which are the same WPF generation as each other and a different one from .NET 10.

**The honest summary is that the riskier direction was covered by luck rather than by plan.** If only
one framework family could be tested, the old one was the better one to have: it is where a modern
WPF idiom is most likely to be missing. .NET 10 dropping something that works on 4.7.2 is the rarer
failure. That is a reason to be reasonably confident, **not** a reason to write it down as proved.

**The currently installed 2027 build has never been loaded at all.** It went in at `19:57:46Z`,
thirteen minutes after the last 2027 session started. It differs from the one that did load by two log
lines and nothing else.

### The log could not answer "did the window open?", and now it can

**Asked on 2026-09-20 whether Bridge Status had been opened on Revit 2020, the log could not say.**
Every `_log` call in that window was a FAILURE line, so a window that drew perfectly was
indistinguishable from a button that did nothing, and the question could only be settled by the owner
sending a screenshot.

That is a gap in evidence rather than in behaviour, and [Golden Rule 14](../14-golden-rules.md) calls the
log evidence. The ribbon already records a person acting on it - `Connected from the ribbon`,
`Write permission set to True from the ribbon` - and these two windows were the one Heron surface that
stayed silent about it.

Both now say so as they open, **with the fact worth keeping rather than just the event**:

```text
Bridge Status opened: Connected.
Changes window asked.
```

The second one also retires an argument. Whether the Changes window drew could until now only be
inferred from the ABSENCE of a `failed to open` line - sound reasoning, and still reasoning. That line
is a fact.

**Not deployed yet.** All three Revits were open when this was written, and `deploy-addin.ps1` refuses
while they are - correctly. It goes out with the next deploy and needs a Revit restart like every
other add-in change.

### And deploying it made A12's unguarded gap actually happen

`A12` above recorded, on 2026-09-19, that `deploy-addin.ps1` **cannot tell `net472` from `net48`**
because neither emits a `deps.json`, and closed with *"A 2024 build deployed into 2020 would pass every
check. It did not happen here, but nothing stands behind that guard."*

**On 2026-09-20 it happened.** The build folder is shared, this session had last built **2024**, and
`deploy-addin.ps1 -RevitVersion 2020` copied that straight into `Addins/2020/Heron` and reported
success. Read back out of the deployed bytes:
`.NETFramework,Version=v4.8` sitting in the 2020 folder, which is supposed to be `v4.7.2`.

**Caught by reading the assembly rather than by the script**, which is the point: every guard the
script has said yes.

Rebuilt per release and redeployed, each one verified from the deployed bytes rather than from a
build log:

| release | TFM in the deployed DLL |
|---|---|
| 2020 | `.NETFramework,Version=v4.7.2` |
| 2024 | `.NETFramework,Version=v4.8` |
| 2027 | `.NETCoreApp,Version=v10.0` |

**It was probably harmless here** - .NET Framework 4.8 replaces 4.7.2 in place on Windows, so the
assembly would very likely have loaded - and that is exactly what makes it worth writing down. A
failure that does not show on the machine that caused it is one that shows on somebody else's.

**FIXED THE SAME DAY, and tested rather than reasoned about.** `deploy-addin.ps1` now reads the
`TargetFrameworkAttribute` out of `Heron.Revit.Addin.dll` before it copies anything, and refuses when
it disagrees with the release being deployed for. It reads the assembly as BYTES rather than loading
it: reflection would lock the file about to be replaced, and Windows PowerShell runs on .NET
Framework, which cannot load a .NET 10 assembly at all - so the release most worth checking is the one
reflection could not check.

The two proxies it replaces are gone, because they were standing in for exactly this fact and between
them could not see the case above. `$isDotNet` is now derived from the release rather than from
whether a `deps.json` happens to be lying in the build folder, which also removes the circularity the
2026-09-12 comment in that file still records.

**Six cases run on the PC, 2026-09-20:**

| build made for | deployed for | result |
|---|---|---|
| 2027 (.NET 10) | 2020 | refused |
| 2020 (net472) | 2027 | refused |
| **2024 (net48)** | **2020** | **refused** - the case that got through |
| 2020 (net472) | 2020 | allowed, deployed bytes read `v4.7.2` |
| 2027 (.NET 10) | 2027 | allowed, deployed bytes read `v10.0` |
| any | **2028** | refused - Heron does not know that release's runtime, and will not guess |

That last row is new behaviour rather than a restoration. The old guard treated anything from 2027
onward as .NET 10, so a 2027 build would have been deployed for a release nobody has seen.
`Directory.Build.props` calls an unlisted release an error rather than a guess; this now agrees with
it.

**One defect found by running it, which reasoning would not have caught.** The first run refused
correctly and said the build was made for `.` - PowerShell **unrolls a one-element array** on its way
out of a function, so the array came back as a bare string, whose `.Count` is also 1 and whose `[0]`
is the first character. The guard was right and its sentence was gibberish. `@()` at the call site.

---
