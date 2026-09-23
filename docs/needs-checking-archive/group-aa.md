# NEEDS-CHECKING archive — Group AA

> **Checks that were done, moved out of the live register.** These are rows of Group AA of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *Stage 2 of the installer plan: can two Heron tabs live in one Revit? 2026-09-21* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row AA1

*Moved from the register on 2026-09-23.*

**Do this.** Deploy **both** proofs with the AI Bridge add-in also installed. Start Revit. **Screenshot.**

**Pass looks like.** **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AA was RUN*, below. *It was to look like:* **TWO tabs**: `Heron` and `Heron Doc`. The `Heron` tab carries **both** an `AI Bridge` panel and a `Tools` panel - **ONE tab, two panels**. **The failure to watch for is two tabs both reading `Heron`**, which on screen looks almost right: that would mean `CreateRibbonTab` made a second one instead of finding the first, and R-35 is broken. The `Heron` tab's **three existing buttons must still work** - press each one

---

### Row AA2

*Moved from the register on 2026-09-23.*

**Do this.** **TOOLS ONLY.** `.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-bridge -Remove`, leaving only the Tools proof. Start Revit. **Screenshot.**

**Pass looks like.** **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AA was RUN*, below. *It was to look like:* A `Heron` tab **exists**, carrying the `Tools` panel and **NO `AI Bridge` panel**. **THIS IS THE CASE EXPECTED TO BREAK FIRST** and the one [R-34](../work-notes/plans/plugin-extension/01-requirements.md) promises - a site modeller who wants Heron's tools and refuses the AI. If **no tab appears at all**, the tools piece assumed it loaded second and something has to create the tab. A FAIL here is worth as much as a pass: it is the answer Stage 2 was built to get

---

### Row AA3

*Moved from the register on 2026-09-23.*

**Do this.** **AI BRIDGE ONLY.** `-Product heron-tools -Remove`, then `-Product heron-bridge` to reinstall the add-in. Start Revit. **Screenshot.**

**Pass looks like.** **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AA was RUN*, below. *It was to look like:* The `Heron` tab exactly as it is today - one `AI Bridge` panel, three buttons, no `Tools` panel. This is the control case: it proves the proofs left nothing behind

---

### Row AA4

*Moved from the register on 2026-09-23.*

**Do this.** `.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-doc -Remove`, then start Revit

**Pass looks like.** **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AA was RUN*, below. *It was to look like:* **`Heron Doc` is gone and `Heron` is untouched.** This is the uninstall story proved before any uninstaller exists. Check the Addins folder afterwards: no `Heron.Doc` folder, no `Heron.Doc.addin`, and **no `.old` anything** - [R-38c](../work-notes/plans/plugin-extension/01-requirements.md)

---

### Row AA5

*Moved from the register on 2026-09-23.*

**Do this.** Do AA1 on **2020** as well as on a modern release

**Pass looks like.** **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AA was RUN*, below. *It was to look like:* 2020 is `net472` and 2027 is `net10.0-windows`. The ribbon API is the same across both, but this has never been run, and `Z9` is the precedent for checking rather than assuming

---

### Row AA7

*Moved from the register on 2026-09-23.*

**Do this.** **THE ONE THAT MATTERS MOST.** `.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-bridge` with **no other argument**, exactly as it has been run since Step 1. Start Revit

**Pass looks like.** **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AA was RUN*, below. *It was to look like:* The `Heron` tab, the `AI Bridge` panel, **and all three buttons working** - connect, disconnect, and the bridge answering `python mcp\client\heron_bridge_client.py ping`. **This is the regression check for generalising the script.** Every path in it used to say `Heron.Revit.Addin`; they now come from `platform/heron-products.json`. `tests/test_deploy_script.py` proves the default **resolves** to the same four values, which is a text check - **this is the run**

---

### Row AA8

*Moved from the register on 2026-09-23.*

**Do this.** After AA7, run it again to force a replace, then `.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-bridge -Rollback`. Start Revit

**Pass looks like.** **PASSED 2026-09-21 on the owner's PC** - the verdict is in *Group AA was RUN*, below. *It was to look like:* The add-in comes back and works. **The backup path CHANGED on 2026-09-21** - it is now `%LOCALAPPDATA%\Heron\install-backup\2024\Heron` rather than `...\2024`, so **a backup made before that date will not be found** and the script says so rather than restoring nothing. The rollback proof in `Group Z` was made against the old path and does **not** cover this

---
