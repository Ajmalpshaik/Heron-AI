# NEEDS-CHECKING archive — Group EQ

> **Checks that were done, moved out of the live register.** These are rows of Group EQ of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *"2020 → latest" backed by evidence: the add-in loaded on every release, and a core set proved on each (2026-10-17)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row EQ6

*Moved from the register on 2026-11-17.*

**Run.** **RUN, PASS 2026-10-17 ON ALL THREE, with today's build** - `76006767`, Debug, deployed at 13:39 by `tools/deploy-addin.ps1`, the three Heron DLLs' hashes equal to the build's, and each DLL's version text `0.1.0+76006767...`. On every release the ribbon was the one EQ5 lists - the Heron tab, panel **AI Bridge**: **Heron** (with **Bridge Status**), **Changes ON**, **Admin ON**, **Publish ON**, **Companion** (with **Turn Companion off**) - read through UI Automation and matched by seven `Added pushbutton` lines in each journal. **2024:** Revit 2024.3, PID 8168, "EQ-2024 Snowdon Towers Sample HVAC" (9,694 elements, from `Revit 2024\Samples`); journal.0008 line 1298 `API_SUCCESS { Starting External Application: Heron AI`, no *"cannot run"*, no unsigned question; `Heron loaded. Revit 2024, add-in 0.1.0.0, pid 8168.`, warm-up compiled in 5368 ms; bridge `heron.2024.8168` connected 13:49:05; `revit_health` *"Revit 2024, session 8168 - (free)"*; `ping` add-in 0.1.0.0, protocol 2. Left running for another chat's proofs. **2027:** Revit 2027.3, PID 6096, "EQ-2027 Snowdon Towers Sample HVAC" (9,813 elements, from `Revit 2027\Samples`); journal.0002 line 2393 `API_SUCCESS`, no *"cannot run"*, the unsigned question twice (lines 263 and 276), answered at the PC; `Heron loaded. Revit 2027, add-in 0.1.0.0, pid 6096.`, warm-up 2708 ms; bridge `heron.2027.6096` connected 13:58:26; `revit_health` *"Revit 2027, session 6096 - (free)"*; `list-sheets` (PROVEN) answered the model's 17 sheets. **Caveat:** that compile used AJ Tools' C# compiler, not Heron's - [row 5b-575](../fragment-issues/section-5b-rows-551-575.md). Closed by asking Revit to close, 13:58:53. **2020:** Revit 2020.2, PID 51568, "EQ-2020 rme_advanced_sample_project" (from `Revit 2020\Samples`; took six minutes to open, with 1.2 GB of 15.4 GB free); journal.0036 line 899 `API_SUCCESS { Starting External Application: Heron AI` (2020's journal leaves out `Assembly Version:`, [docs/07 §10.9](../07-installation-and-update.md) item 4), no *"cannot run"*, the unsigned question once (line 171), answered at the PC; `Heron loaded. Revit 2020, add-in 0.1.0.0, pid 51568.`, warm-up 4941 ms; bridge `heron.2020.51568` connected from the ribbon at 14:07:47, pressed at the PC; `revit_health` *"Revit 2020, session 51568 - (free)"*; `ping` add-in 0.1.0.0, protocol 2. **Element count not taken on 2020:** the owner started working in that same Revit (a new "Project1", then a family) before the bridge was up, so the session ran nothing there and left it open. EQ1 again on **2020, 2024 and 2027** with today's build

**Look for.** The same on all three. A12 was build `48ae1dc` and the add-in has changed since; a pass then is not a pass now

---
