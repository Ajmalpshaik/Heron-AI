// NOT STANDALONE. Assumes `doc` and `elements` are in scope; leaves `findings`,
// `heatingW`, `coolingW`, `airflowLs`, `notSpaces` and `noLoad` behind.
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
            + "and ask again",
            string.Format("{0} {1}", space.Number ?? "", space.Name ?? "").Trim()));
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

    var name = string.Format("{0} {1}", space.Number ?? "", space.Name ?? "").Trim();

    // Everything zero is the "nothing has been run" case, and it is reported as
    // that rather than as three zeroes.
    if (heat <= 1e-9 && cool <= 1e-9 && air <= 1e-9)
    {
        noLoad.Add(space.Id);
        findings.Add(string.Format(
            "{0}  - NO LOAD. Either the heating and cooling analysis has never been "
            + "run, or the space is unbounded and has no volume to load", name));
        continue;
    }

    // Every figure says where it came from. A design figure that disagrees with
    // the calculated one is either a decision or a stale analysis, and the row
    // has to let somebody tell which.
    findings.Add(string.Format(
        "{0}  - heating {1:0} W ({2}), cooling {3:0} W ({4}), supply {5:0.#} L/s ({6})",
        name,
        heat * wattsPerInternalLoad, heatIsDesign ? "entered" : "calculated",
        cool * wattsPerInternalLoad, coolIsDesign ? "entered" : "calculated",
        air * litresPerCubicFoot, airIsDesign ? "entered" : "calculated"));
}
