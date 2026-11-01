// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `findings`,
// `heatingW`, `coolingW`, `airflowLs`, `notSpaces` and `noLoad` behind - and,
// since version 2, `specifiedSupplyLs`, `calculatedSupplyLs`,
// `specifiedReturnLs`, `specifiedExhaustLs`, `actualSupplyLs`,
// `actualReturnLs`, `actualExhaustLs`, `noDesignFigure` and `unplaced`.
//
// VERSION 2 (2026-11-01) took in REPORT_SPACE_AIRFLOW, which is DEPRECATED,
// merged into this card. What it read that this did not is read here now:
// the SPECIFIED (typed) supply, return and exhaust airflow and the ACTUAL
// supply, return and exhaust - the sum of what the space's terminals carry,
// Revit's own figure - in litres per second; `noDesignFigure`, the spaces
// with no typed airflow of any kind, so a zero-against-zero space never reads
// as balanced; and `unplaced`, a space with no area, named and not read.
//
// TYPED AND CALCULATED ARE KEPT APART - the safer of the two cards' rules.
// `airflowLs` is what version 1 gave: the typed figure where one was entered,
// the calculated one otherwise, and the finding says which. The design TARGET
// is never taken from a calculated figure silently: `specifiedSupplyLs` holds
// only what was typed, `calculatedSupplyLs` only what the analysis gave, and
// `noDesignFigure` is decided on typed figures alone.
//
// READ ONLY. Opens no transaction and needs none.
//
// TWO CONVERSIONS, AND ONLY ONE OF THEM IS PROVABLE HERE.
//
//   AIRFLOW  ft3/s to L/s. 1 ft = 0.3048 m EXACTLY by the definition of the
//            international inch, so 1 ft3 = 0.3048^3 m3 = 0.028316846592 m3 =
//            28.316846592 L. Exact, checkable on paper, same class as D-20's
//            304.8, and nothing for a unit API rewrite to reach.
//
//   LOAD     ASSUMED to be British thermal units per second and converted at
//            1055.05585262 W - the IT definition of the BTU. THIS IS NOT
//            PROVEN. Revit's internal HVAC power unit has to be established
//            against a real Revit, and reflection over the assembly shows a
//            property type, never a unit.
//
// >> IF THAT ASSUMPTION IS WRONG, EVERY LOAD IS OUT BY A CONSTANT FACTOR, which
// >> is the most dangerous shape a unit error takes: each number looks
// >> plausible on its own, every comparison between spaces still works, and
// >> only somebody sizing a real chiller finds out. FIRST THING TO CHECK
// >> against a model: one space's load here, against what Revit puts on screen.
//
// DESIGN AND CALCULATED ARE BOTH READ, AND WHICH ONE IS REPORTED IS SAID. The
// calculated figure comes from the heating and cooling analysis; the design
// figure is what a person typed and it overrides. Where they disagree it is
// either a deliberate decision or an analysis nobody re-ran, and reporting one
// number without saying which hides the difference.
//
// A ZERO IS NOT A LOAD. It means the analysis has never been run, or the space
// is unbounded and has no volume to load. Handing back "0 W" puts a
// number-shaped non-answer in front of somebody sizing equipment.

var findings = new List<string>();
var heatingW = new Dictionary<ElementId, double>();
var coolingW = new Dictionary<ElementId, double>();
var airflowLs = new Dictionary<ElementId, double>();
var notSpaces = new List<ElementId>();
var noLoad = new List<ElementId>();

// VERSION 2, from REPORT_SPACE_AIRFLOW - see the header.
var specifiedSupplyLs = new Dictionary<ElementId, double>();
var calculatedSupplyLs = new Dictionary<ElementId, double>();
var specifiedReturnLs = new Dictionary<ElementId, double>();
var specifiedExhaustLs = new Dictionary<ElementId, double>();
var actualSupplyLs = new Dictionary<ElementId, double>();
var actualReturnLs = new Dictionary<ElementId, double>();
var actualExhaustLs = new Dictionary<ElementId, double>();
var noDesignFigure = new List<ElementId>();
var unplaced = new List<ElementId>();

// A built-in airflow parameter in Revit's internal cubic feet per second, 0
// when the space has none - REPORT_SPACE_AIRFLOW's own read.
Func<Element, BuiltInParameter, double> flowOf = (element, which) =>
{
    try
    {
        var p = element.get_Parameter(which);
        if (p != null && p.HasValue) return p.AsDouble();
    }
    catch (Exception) { }
    return 0.0;
};

// Exact. See above.
var litresPerCubicFoot = 28.316846592;

// ASSUMED. See above, and check it first.
var wattsPerInternalLoad = 1055.05585262;

foreach (var element in elements)
{
    if (element == null) continue;

    var space = element as Space;

    // A Room is not a Space and carries no load. Recorded rather than skipped:
    // a model with rooms and no spaces answers nothing here, and that is the
    // finding, not an empty result.
    if (space == null) { notSpaces.Add(element.Id); continue; }

    var label = string.Format("{0} {1}", space.Number ?? "", space.Name ?? "").Trim();

    // AN UNPLACED SPACE IS REPORTED, NOT HIDDEN (version 2, from
    // REPORT_SPACE_AIRFLOW). Revit gives it an area of zero and it carries no
    // meaningful load or flow; a space in the schedule that is not in the model
    // is a real finding, and reading it would print zeros as if they were
    // figures.
    double area = 0.0;
    try { area = space.Area; } catch (Exception) { area = 0.0; }
    if (area <= 0.0)
    {
        unplaced.Add(space.Id);
        findings.Add(string.Format("{0}  - NOT PLACED. In the schedule, not in the model - it has no area, "
            + "so nothing is read from it", label));
        continue;
    }

    // THE AIRFLOWS, TYPED AND ACTUAL (version 2). Read before the loads,
    // because they do not throw where the loads do, and a space whose loads
    // cannot be computed still has its typed design flow and its terminals.
    var typedSupply = flowOf(space, BuiltInParameter.ROOM_DESIGN_SUPPLY_AIRFLOW_PARAM);
    var typedReturn = flowOf(space, BuiltInParameter.ROOM_DESIGN_RETURN_AIRFLOW_PARAM);
    var typedExhaust = flowOf(space, BuiltInParameter.ROOM_DESIGN_EXHAUST_AIRFLOW_PARAM);
    var realSupply = flowOf(space, BuiltInParameter.ROOM_ACTUAL_SUPPLY_AIRFLOW_PARAM);
    var realReturn = flowOf(space, BuiltInParameter.ROOM_ACTUAL_RETURN_AIRFLOW_PARAM);
    var realExhaust = flowOf(space, BuiltInParameter.ROOM_ACTUAL_EXHAUST_AIRFLOW_PARAM);

    specifiedSupplyLs[space.Id] = typedSupply * litresPerCubicFoot;
    specifiedReturnLs[space.Id] = typedReturn * litresPerCubicFoot;
    specifiedExhaustLs[space.Id] = typedExhaust * litresPerCubicFoot;
    actualSupplyLs[space.Id] = realSupply * litresPerCubicFoot;
    actualReturnLs[space.Id] = realReturn * litresPerCubicFoot;
    actualExhaustLs[space.Id] = realExhaust * litresPerCubicFoot;

    // A SPACE WITH NO DESIGN FLOW LOOKS BALANCED - zero typed, zero actual,
    // difference zero. No typed target of ANY kind - supply, return and
    // exhaust all zero - is the case; a return-only space is not it. A
    // CALCULATED supply is never counted as a design target here.
    var noDesign = typedSupply <= 1e-9 && typedReturn <= 1e-9 && typedExhaust <= 1e-9;
    if (noDesign) noDesignFigure.Add(space.Id);

    var flows = string.Format(
        "; specified supply {0:0.#}, return {1:0.#}, exhaust {2:0.#} L/s; actual supply {3:0.#}, "
        + "return {4:0.#}, exhaust {5:0.#} L/s{6}",
        typedSupply * litresPerCubicFoot, typedReturn * litresPerCubicFoot, typedExhaust * litresPerCubicFoot,
        realSupply * litresPerCubicFoot, realReturn * litresPerCubicFoot, realExhaust * litresPerCubicFoot,
        noDesign ? " - NO DESIGN AIRFLOW TYPED: zero against zero is not a balance, nobody has designed it" : "");

    // THESE SIX PROPERTIES THROW, AND THE HANDLER FOR IT WAS UNREACHABLE.
    //
    // Found 2026-09-07 by running this against 16 spaces in Snowdon Towers: the
    // whole fragment died with Revit's own `Not Computed!`. That is what
    // DesignHeatingLoad and its siblings raise when the model's Areas and
    // Volumes computation is off, or the space is unbounded.
    //
    // The bitter part is that the `noLoad` branch twenty lines below already
    // says "the space is unbounded and has no volume to load" - the right
    // answer, written, and never once printed, because the read threw before
    // any value existed. A guard placed after the thing it guards against.
    //
    // So the reads are guarded and a throw routes into that same `noLoad`
    // list, which is where it was always meant to end up. One space with
    // volumes off costs that space, not the report for the other fifteen -
    // the rule this file already applies everywhere else.
    double designHeat, calcHeat, designCool, calcCool, designAir, calcAir;
    try
    {
        designHeat = space.DesignHeatingLoad;
        calcHeat = space.CalculatedHeatingLoad;
        designCool = space.DesignCoolingLoad;
        calcCool = space.CalculatedCoolingLoad;
        designAir = space.DesignSupplyAirflow;
        calcAir = space.CalculatedSupplyAirflow;
    }
    // `catch (Exception)` rather than Revit's own exception type, and not by
    // preference. Revit's exceptions namespace is not among the ones the
    // executor imports (HeronFragmentImports), so the type cannot be named
    // unqualified - and naming it in full would put a vendor namespace inside
    // `brain/`, which check-structure refuses on sight, comments included: the
    // adapter boundary in docs/16 section 4. It is also what every other
    // fragment in this library does. The catch is narrow in REACH even if broad
    // in TYPE: it wraps six property reads and nothing else.
    catch (Exception)
    {
        noLoad.Add(space.Id);
        findings.Add(string.Format(
            "{0}  - NO LOAD READABLE. Revit refused the figures, which it does when "
            + "the model's Areas and Volumes computation is off or the space is "
            + "unbounded. Turn on Area and Volume Computations, or bound the space, "
            + "and ask again{1}",
            label, flows));
        continue;
    }

    // Design overrides calculated where somebody has entered one, which is what
    // Revit itself does. Zero means nothing was entered.
    var heatIsDesign = designHeat > 1e-9;
    var heat = heatIsDesign ? designHeat : calcHeat;

    var coolIsDesign = designCool > 1e-9;
    var cool = coolIsDesign ? designCool : calcCool;

    var airIsDesign = designAir > 1e-9;
    var air = airIsDesign ? designAir : calcAir;

    heatingW[space.Id] = heat * wattsPerInternalLoad;
    coolingW[space.Id] = cool * wattsPerInternalLoad;
    airflowLs[space.Id] = air * litresPerCubicFoot;
    calculatedSupplyLs[space.Id] = calcAir * litresPerCubicFoot;

    var name = string.Format("{0} {1}", space.Number ?? "", space.Name ?? "").Trim();

    // Everything zero is the "nothing has been run" case, and it is reported as
    // that rather than as three zeroes.
    if (heat <= 1e-9 && cool <= 1e-9 && air <= 1e-9)
    {
        noLoad.Add(space.Id);
        findings.Add(string.Format(
            "{0}  - NO LOAD. Either the heating and cooling analysis has never been "
            + "run, or the space is unbounded and has no volume to load{1}", name, flows));
        continue;
    }

    // Every figure says where it came from. A design figure that disagrees with
    // the calculated one is either a decision or a stale analysis, and the row
    // has to let somebody tell which.
    findings.Add(string.Format(
        "{0}  - heating {1:0} W ({2}), cooling {3:0} W ({4}), supply {5:0.#} L/s ({6}){7}",
        name,
        heat * wattsPerInternalLoad, heatIsDesign ? "entered" : "calculated",
        cool * wattsPerInternalLoad, coolIsDesign ? "entered" : "calculated",
        air * litresPerCubicFoot, airIsDesign ? "entered" : "calculated", flows));
}
