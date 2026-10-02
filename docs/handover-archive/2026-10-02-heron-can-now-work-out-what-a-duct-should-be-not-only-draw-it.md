# Session note — HERON CAN NOW WORK OUT WHAT A DUCT SHOULD BE, NOT ONLY DRAW IT

> **Session note** from 2026-10-02, written straight into this folder, one file per sitting, as
> [`README.md`](README.md) asks. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**. A note records what was true on its own day.

---

### 2026-10-02 — HERON CAN NOW WORK OUT WHAT A DUCT SHOULD BE, NOT ONLY DRAW IT

Cloud session, no Revit. The owner's request, in their own words: *"you have capability on the ducting ... but
you are not aware of the designing part ... study HAP and ASHRAE ... make it part of our project, like
airflow, duct sizing, diffuser sizing, all kind of things."*

**NEW.**

- **An HVAC design engine**, [`brain/heron_hvac.py`](../../brain/heron_hvac.py) over
  [`brain/heron_psychro.py`](../../brain/heron_psychro.py), served as the READ-only `heron_hvac` MCP tool
  and owned by a new registry row, `HERON-MEP-HVD-001`, in a new department. Loads, the sun on a
  surface, supply air, ASHRAE 62.1 outdoor air, psychrometrics and coils, duct friction and sizing, the
  index run and fan power, diffusers, chilled water and pipes, units, and cited reference tables.
  [`docs/41`](../41-hvac-design.md) is what it was built from and what it is not.
- **Every design value is ASKED for** ([D-33](../decisions/D-33.md)); a standard's figure is OFFERED
  beside the question and never applied. The owner's call on whether to keep showing the figure is
  [F42](../proposals/f42.md), with four more.
- **Research ran as six parallel readers** and the network reached GitHub and the package index and
  almost nothing else. What made the solar and load equations trustworthy anyway: ASHRAE's own 2017
  load-calculation workbooks, in a public repository, reproduced to 1e-4. What could not be read:
  ashrae.org, carrier.com, every Qatari host. [`docs/41 §10`](../41-hvac-design.md) says, table by
  table, how far each figure was checked.

**FOUND ON THE WAY.**

- **The wet-bulb solver refused Doha.** Bracketing 45.7 C air at a 22.2 C wet bulb, it tried wet bulbs
  near the dew point, where ASHRAE's eq. 33 goes negative, and its own range check stopped the solve -
  on the exact air the engine exists for. The suite caught it; the solver now brackets on the raw
  equation.
- **Doha's peak dry bulb is dry air** - 7.1 g/kg against a 23 C 50 % room's 8.8 - so outdoor-air
  latent load is negative there and coil sizing needs the dehumidification condition. The engine warns.
- **One open-source solar library prints ASHRAE's beam exponent with the wrong sign** (-0.021 for
  +0.021), 38 W/m2 off ASHRAE's own figures. Recorded in docs/41 §4.3 so nobody copies it here.
- **`tests/test_naming.py` typed the register's size as `250`**, so adding any agent turned it red. It
  reads the register's own totals line now. `brain/heron_contract.py` printed *"of the 250 agents"*
  the same way and now derives it.
- **`tests/test_workforce.py` went red over the new row's wording.** It asks the Workforce Planner its
  questions against the live register, and *"Works out ... pipe sizing"* matched its sleeve-sizing
  proposal on three words. The row says *"Calculates"* now and the test is untouched; the coupling is
  [row 5b-278](../fragment-issues/section-5b-rows-176-200.md), recorded, not fixed.
- **The psychrometrics research came back last** and agreed with the module on every state-point vector
  it produced. The suite now carries ASHRAE's Examples 1 and 4 and water against IAPWS, and docs/41
  says plainly that 1.23 / 3010 / 1.20 were checked by unit conversion, never against the Handbook's page.

**THE OWNER ANSWERED TWO OF F42 THE SAME DAY** - *"Use 2.5 m/s, ask standards once per project."*

- **[D-110](../decisions/D-110.md):** a supply diffuser neck in an NC/RC 30 room is held to the owner's
  2.5 m/s, not ASHRAE's 2.2 - at that criterion only, named as the office's own figure every time, and
  given way to by any figure the modeller states. ASHRAE's table is kept as it prints.
- **[D-111](../decisions/D-111.md):** a project's four governing standards - the 62.1 edition, 90.1 or
  not, the QCS edition, CIBSE beside ASHRAE - are asked once and kept for that project in
  [`brain/heron_hvac_project.py`](../../brain/heron_hvac_project.py)'s one file per project. They never
  block a calculation, and a chat that has not read the model keeps nothing and says so. The real
  project key is NEEDS-CHECKING [Group BT](../needs-checking/group-bt.md).
- **Built so a new check FAILS on the old code rather than crashing** - fifteen of section 7's checks
  go red against the engine before this, with no traceback, and the three that do not are guards that
  must hold either way.

**WAITING.**

- [F42](../proposals/f42.md) - three owner decisions left of five.
- **A comparison against an engineer's own HAP or TRACE run** on one real room is what would carry
  weight; nothing in this session could do it.
- `tests/test_mcp_serves.py` still fails one check on `main` too - row 5b-263, recorded before this
  session, unchanged by it.
