# Session note — HERON'S FIRE ENGINE NOW READS EN 12845, FM GLOBAL AND BS 5839

> **Session note** from 2026-10-02, written straight into this folder, one file per sitting, as
> [`README.md`](README.md) asks. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**. A note records what was true on its own day.

---

### 2026-10-02 — HERON'S FIRE ENGINE NOW READS EN 12845, FM GLOBAL AND BS 5839

Cloud session, no Revit, straight after the fire engine merged (#386). The owner, in their own words:
*"do the best research okay maximum research and make it ... not only NFPA you can check it if there is
any another also you can consider."*

**NEW.**

- **Four more research readers**, about 45 searches each: NFPA 13's missing tables; BS EN 12845;
  Qatar, the UAE and Saudi Arabia; BS 5839-1, BS 5306, BS 9990, FM Global and NFPA 13R/13D. Every
  page fetch was still refused; a figure counted only where a search returned it without it being in
  the query. [`docs/42` §10](../42-fire-protection-design.md) says what each table rests on.
- **One standard at a time** ([42 §4a](../42-fire-protection-design.md)). `sprinkler_standard` now
  takes BS EN 12845 and FM Global beside an NFPA 13 edition. A class is read only in the project's
  own standard - EN 12845's OH1 is not NFPA 13's - and each standard's figures are offered. The pipe
  schedule and the beam rule are refused under EN 12845 and FM Global. `fire_authority` takes UAE
  Civil Defence.
- **NFPA 13's held tables grew**: C values, the K list, temperature ratings, the minimum operating
  pressure, the design area percentages (dry 30 %, slope, quick-response, high-temperature), light
  hazard's combustible rows, the beam rule to 5 ft 6 in, the storage clearance; 1.2 sqrt A confirmed.
- **Three new calculations**: `temperature_rating`, `equivalent_length` (fifteen chart cells, adjusted
  for C and bore) and `hose_reels`. `detector_layout` takes a radius (BS 5839-1's 7.5 m and 5.3 m,
  NFPA 72's 0.7 S), `extinguishers` BS 5306-8's combined rating, `coverage_check` two devices at every
  point (QCDD's hose reels), `water_storage` QCDD's compartments and refill.
- **A behaviour changed on evidence**: design area adjustments were multiplied in turn - the engine's
  own assumption. NFPA 13's example, through one site, adds each on the area first selected: 2500 +
  750 - 625 = 2625 ft2, not 2437.5. Changed, and NEEDS-CHECKING BU8 asks a copy to confirm.

**FOUND ON THE WAY, RECORDED, NOT FIXED.**

- **`main`'s CI has been red on every push since #379** - row
  [5b-279](../fragment-issues/section-5b-rows-176-200.md): `tests/test_heron_session.py` expects the
  session line on the repository, and #379 made that line silent in the main checkout on branch main,
  which is what CI's push checkout is. Pull requests are detached and pass. Reproduced both ways here.

**TO DO.**

- NEEDS-CHECKING [Group BU](../needs-checking/group-bu.md) BU4 to BU8: EN 12845 against A2:2026, FM
  Global's HC-1 dry area, BS 5839-1:2025, the fittings chart and its bore factor, and NFPA 13's own
  example for more than one adjustment.
- [F43](../proposals/f43.md) item 6: a copy of QCDD's guide and annex, so its tables are read rather
  than reported.
