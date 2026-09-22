# NEEDS-CHECKING archive — Group B

> **Checks that were done, moved out of the live register.** These are rows of Group B of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *does Revit still load* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row B2

*Moved from the register on 2026-09-23.*

**Do this.** ~~Look at the panel~~

**Pass looks like.** **DONE 2026-09-06, twice.** First as *two* controls. Re-checked after Emergency Stop was removed ([D-46](../DECISIONS.md)): the panel showed **one** control, as intended. Revit 2020 and 2024 were both open at that moment and which one was looked at was not established — the two carry the same source, so this proves the ribbon is right, not which release it is right on

---

### Row B2a

*Moved from the register on 2026-09-23.*

**Do this.** ~~Click the arrow under Heron~~

**Pass looks like.** **DONE 2026-09-06.** The list opens with Bridge Status in it

---

### Row B2b

*Moved from the register on 2026-09-23.*

**Do this.** ~~Pick Bridge Status, then look at the top of the split button~~

**Pass looks like.** **DONE 2026-09-06.** Top stayed **Heron**, icon intact. `IsSynchronizedWithCurrentItem = false` does take effect in a real Revit, which a compile could not have told us. **The release it was run on was not recorded** — re-run on the other two before this counts for all of 2020-2027

---

### Row B3

*Moved from the register on 2026-09-23.*

**Do this.** ~~Press Heron~~

**Pass looks like.** **DONE 2026-09-06.** Connects, icon lights, goes dark again on the second press. The state indicator survived the move into a split button — which is what `IsSynchronizedWithCurrentItem = false` was there to protect, now proven from the user's side rather than from the flag

---

### Row B5

*Moved from the register on 2026-09-23.*

**Do this.** ~~`python tools/check-compile.py`~~

**Pass looks like.** **DONE 2026-09-07, on Linux, all eight releases.** `Heron.Core`, `Heron.Bridge`, `Heron.Revit.Addin` and `Heron.Bridge.TestHost` — **ok on 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027**, and the add-in builds **0 warnings, 0 errors** on 2024. The first WPF-heavy file in the add-in does not break the build on any release it claims. **It says nothing about whether the banner appears, where, or in the right colour**

---

### Row B5b

*Moved from the register on 2026-09-23.*

**Do this.** ~~Build on Windows and deploy into Revit~~

**Pass looks like.** **DONE 2026-09-07, on the owner's PC.** `dotnet build -c Debug -p:RevitVersion=2024` — **0 warnings, 0 errors**, the first time the banner has been compiled by the **Windows** toolchain rather than Linux/NuGet reference assemblies. Deployed with `tools/deploy-addin.ps1 -RevitVersion 2024` while Revit was closed, and the **deployed** `Heron.Revit.Addin.dll` (68,608 bytes, was 53,760) was read back off disk to confirm it carries `HeronActivityBanner` and the literals `is reading your model`, `is changing your model`, `READING`, `CHANGING`. `ui.activityBanner` is absent from `%APPDATA%\Heron\config\heron.config`, so the **default `true`** applies and the banner is armed. **The bits Revit will load are now on disk — that is all this says. It still has never appeared on a screen; B6-B13 are untouched**

---

### Row B6

*Moved from the register on 2026-09-23.*

**Do this.** ~~Connect, then ask for a count~~

**Pass looks like.** **PROVED 2026-09-07**, Revit 2024, `Snowdon Towers Sample HVAC` (9,628 elements), on DISPLAY1 - the **non-primary** monitor at x=-1920. Captured by screen-grabbing Revit's own window every ~33 ms while the job ran. **SEEN.** Dark card, **top centre of Revit's window**, **"Heron AI is reading your model"**, sub-line **"Counting what is in the model"**, blue **READING** chip. It appeared **before** the green finished card, which is what settles the ordering: had `Raise` come after the work there would have been no reading frame at all, and there was one. **Caveat on the word 'frozen'** - the Revit-side work was **12 ms**, so nothing was frozen long enough to see. The pre-`Raise` ordering holds; "visible during a long freeze" still wants a genuinely slow job

---

### Row B7

*Moved from the register on 2026-09-23.*

**Do this.** ~~Watch the same card after the answer arrives~~

**Pass looks like.** **PROVED 2026-09-07**, Revit 2024, `Snowdon Towers Sample HVAC` (9,628 elements), on DISPLAY1 - the **non-primary** monitor at x=-1920. Captured by screen-grabbing Revit's own window every ~33 ms while the job ran. **Turns green**, **"Heron AI has finished"**, sub-line **"Done - 12 ms"**, green **DONE** chip. Held **1,533 ms** measured frame-to-frame, then gone - the spec said about 1.4 s. Neither firing early nor failing to fire: `End` is reached

---

### Row B8

*Moved from the register on 2026-09-23.*

**Do this.** ~~With `write.enabled = true`, ask to move ducts and approve~~

**Pass looks like.** **PASSED 2026-09-07, after the token fix.** Revit 2024, `Project1 work_ajmal.al`, **3 ducts moved up 200 mm** - the first time Heron has ever changed a Revit model. The card went **AMBER**: amber dot, **"Heron AI is changing your model"**, sub-line **"Moving elements"**, amber **CHANGING** chip. **The distinction is proved, not assumed:** seconds earlier the *preview* of the same move showed **BLUE READING** with *"Working out what a move would do"*. Same feature, same session, same category - read and write told apart correctly, from `HeronOperationRegistry` by operation name. Found by scanning all 918 captured frames for RGB(240,163,44); amber held ~9 frames, about 300 ms, because the write itself is that fast. `write.enabled` was returned to false immediately afterwards. **The owner confirmed that model is scrap** - opened for this test, not real work - so the 3 ducts were left 200 mm up rather than moved back. The name looks like a workshared local and is not one to worry about

---

### Row B9

*Moved from the register on 2026-09-23.*

**Do this.** ~~Click a ribbon button through the card while it is up~~

**Pass looks like.** **PROVED 2026-09-07 by reading the window itself**, which is stronger than a click and touches nothing. The live banner window - WPF, titled **"Heron AI"**, class `HwndWrapper[DefaultDomain;;...]`, 460x78 at -1190,4 - has `exStyle` **0x80800A8**: **`WS_EX_TRANSPARENT` (0x20) is SET**, so clicks pass through and it cannot eat one. Also `WS_EX_NOACTIVATE` - never steals focus - and `WS_EX_TOOLWINDOW` - stays out of Alt+Tab. **`SetWindowLongPtrW` was found on 2024**, so the clickable-banner fallback was not taken. Older releases still unproven

---

### Row B11

*Moved from the register on 2026-09-23.*

**Do this.** ~~Run a batch of fragments back to back~~

**Pass looks like.** **PROVED 2026-09-07**, Revit 2024, `Snowdon Towers Sample HVAC` (9,628 elements), on DISPLAY1 - the **non-primary** monitor at x=-1920. Captured by screen-grabbing Revit's own window every ~33 ms while the job ran. **ONE steady card, no strobe.** Five jobs back to back: the banner went up once and stayed up **2,664 ms** through all five, never once returning to the no-banner frame, and changed appearance only **twice** (reading, then finished). The hide timer is being cancelled by the next `Begin`. **Needs `HERON_CLIENT_ID` pinned** or each CLI call is a new chat and the lease refuses the second - see the note under this table

---

### Row B13

*Moved from the register on 2026-09-23.*

**Do this.** ~~Put `ui.activityBanner = false` in `%APPDATA%\Heron\config\heron.config`, restart Revit, ask for a count~~

**Pass looks like.** **PASSED 2026-09-07.** Revit restarted at 18:47:09, ten minutes after the setting was written, so it is a genuine cold read of the value. The count answered normally - **3,435 elements in `Project1 work_ajmal.al`, 339 ms** - and **no card appeared**. Checked by scanning **all 359 captured frames** for the card's own colours: **0 blue, 0 amber, 0 green, 0 red pixels** anywhere in the banner region. The only frames that differed were the ribbon redrawing. Proves both halves: the switch is read, and the banner is not on the answer's path. Setting removed afterwards, so the default `true` applies again

---
