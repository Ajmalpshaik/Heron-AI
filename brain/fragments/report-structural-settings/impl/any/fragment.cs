// NOT STANDALONE. Assumes `doc` is in scope; leaves `structuralSettings`,
// `reinforcementSettings`, `settingsRead` and `findings` behind.
//
// READS ONLY. No transaction is opened and nothing is set.
//
// TWO DIALOGS, READ IN THE WORDS SET_STRUCTURAL_SETTINGS AND
// SET_REINFORCEMENT_SETTINGS TAKE, so a value read here can be named back to
// them as it is:
//   Manage > Structural Settings - Symbolic Representation Settings (symbolic
//     cutback distances, brace symbols, kicker brace) and Boundary Conditions
//     Settings (symbol spacing and the four symbols); Revit 2024 and later the
//     loads display scaling, 2026 and later the steel boundary setback.
//   Structure > Reinforcement Settings - General (hooks and end treatments in
//     the rebar shape definition, host structural rebar), Rebar Presentation,
//     Rounding (rebar and fabric, total and segment length, with the method),
//     the varying length suffix and, how bars are numbered.
//
// Distances are millimetres, 0.01 mm. A rounding is read in the unit Revit's
// rounding manager names for it and printed in millimetres, the unit named
// beside it so a surprise shows.
//
// THE CONNECTION SYMBOLS ARE NOT READ: they have no member in the Revit API on
// any release 2020-2027 (tools/api-surface, 2026-11-02), and the answer says so.

var findings = new List<string>();
var structuralSettings = "";
var reinforcementSettings = "";
var settingsRead = 0;

Func<double, string> mm = feet => Math.Round(feet * 304.8, 2).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " mm";
Func<bool, string> yesNo = b => b ? "yes" : "no";
Func<ElementId, string> typeName = id =>
{
    if (id == null || id == ElementId.InvalidElementId) return "none";
    var t = doc.GetElement(id) as ElementType;
    if (t == null) return "none";
    return string.IsNullOrEmpty(t.FamilyName) ? "'" + t.Name + "'" : "'" + t.FamilyName + ": " + t.Name + "'";
};

// ---- Manage > Structural Settings --------------------------------------------
try
{
    var s = StructuralSettings.GetStructuralSettings(doc);
    if (s == null)
        findings.Add(doc.Title + " has no Structural Settings - Manage > Structural Settings is a project's.");
    else
    {
        var parts = new List<string>
        {
            "Symbolic cutback distance: brace cutback " + mm(s.SymbolicCutbackForBrace)
                + ", beam/truss cutback " + mm(s.SymbolicCutbackForBeamAndTruss)
                + ", column cutback " + mm(s.SymbolicCutbackForColumn),
            "Brace symbols: parallel line offset " + mm(s.BraceParallelLineOffset)
                + ", show brace above " + yesNo(s.ShowBraceAbove) + " (brace above symbol " + typeName(s.BraceAboveSymbol) + ")"
                + ", show brace below " + yesNo(s.ShowBraceBelow) + " (brace below symbol " + typeName(s.BraceBelowSymbol) + ")"
                + ", kicker brace symbol " + typeName(s.KickerBraceSymbol),
            "Boundary conditions: boundary condition symbol spacing " + mm(s.BoundaryConditionAreaAndLineSymbolSpacing)
                + ", fixed boundary condition symbol " + typeName(s.BoundaryConditionFamilySymbolFixed)
                + ", pinned boundary condition symbol " + typeName(s.BoundaryConditionFamilySymbolPinned)
                + ", roller boundary condition symbol " + typeName(s.BoundaryConditionFamilySymbolRoller)
                + ", user defined boundary condition symbol " + typeName(s.BoundaryConditionFamilySymbolUserDefined),
        };
        settingsRead += 15;
#if REVIT2024_OR_GREATER
        parts.Add("Loads: loads display scaling " + yesNo(s.UseLoadsDisplayScaling));
        settingsRead += 1;
#endif
#if REVIT2026_OR_GREATER
        parts.Add("Steel: boundary setback disabled for steel elements " + yesNo(s.BoundarySetbackDisabledForSteelElements));
        settingsRead += 1;
#endif
        structuralSettings = "Structural Settings - " + string.Join(" | ", parts);
        findings.Add("The Connection Symbols (beams and braces, column connection) are not readable through the Revit API "
            + "on any release 2020-2027 - Manage > Structural Settings shows them.");
    }
}
catch (Exception ex) { findings.Add("The Structural Settings could not be read: " + ex.Message); }

// ---- Structure > Reinforcement Settings ---------------------------------------
Func<RebarPresentationMode, string> modeText = m =>
    m == RebarPresentationMode.All ? "show all" : m == RebarPresentationMode.FirstLast ? "show first and last"
    : m == RebarPresentationMode.Middle ? "show middle" : "show selected";
Func<RoundingMethod, string> methodText = m => m == RoundingMethod.Up ? "up" : m == RoundingMethod.Down ? "down" : "nearest";
Func<ReinforcementRoundingManager, string> unitName = manager =>
{
#if REVIT2020
    return LabelUtils.GetLabelFor(manager.LengthDisplayUnit);
#else
    return LabelUtils.GetLabelForUnit(manager.GetLengthUnitTypeId());
#endif
};
Func<ReinforcementRoundingManager, double, string> roundingMm = (manager, value) =>
{
#if REVIT2020
    var n = UnitUtils.Convert(value, manager.LengthDisplayUnit, DisplayUnitType.DUT_MILLIMETERS);
#else
    var n = UnitUtils.Convert(value, manager.GetLengthUnitTypeId(), UnitTypeId.Millimeters);
#endif
    return Math.Round(n, 2).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " mm";
};
try
{
    var r = ReinforcementSettings.GetReinforcementSettings(doc);
    if (r == null)
        findings.Add(doc.Title + " has no Reinforcement Settings.");
    else
    {
        var parts = new List<string>
        {
            "General: include hooks in rebar shape definition " + yesNo(r.RebarShapeDefinesHooks)
                + ", include end treatments in rebar shape definition " + yesNo(r.RebarShapeDefinesEndTreatments)
                + ", host structural rebar " + yesNo(r.HostStructuralRebar),
            "Rebar presentation: rebar presentation in view " + modeText(r.RebarPresentationInView)
                + ", rebar presentation in section " + modeText(r.RebarPresentationInSection),
            "Numbering: varying length number suffix '" + r.RebarVaryingLengthNumberSuffix + "'"
#if REVIT2026_OR_GREATER
                + ", numbering method " + (r.NumberingMethod == ReinforcementNumberingMethod.NumberBarsIndividually ? "number bars individually"
                    : r.NumberingMethod == ReinforcementNumberingMethod.AssignUniqueNumberPerSet ? "assign a unique number per set"
                    : "match sets with identical bars"),
#else
                + ", number varying length rebars individually " + yesNo(r.NumberVaryingLengthRebarsIndividually),
#endif
        };
        settingsRead += 8;
        var rm = r.GetRebarRoundingManager();
        if (rm != null)
        {
            parts.Add("Rebar rounding: rebar total length rounding " + roundingMm(rm, rm.TotalLengthRounding) + " " + methodText(rm.TotalLengthRoundingMethod)
                + ", rebar segment length rounding " + roundingMm(rm, rm.SegmentLengthRounding) + " " + methodText(rm.SegmentLengthRoundingMethod)
                + " (held by Revit in " + unitName(rm) + ")");
            settingsRead += 4;
        }
        var fm = r.GetFabricRoundingManager();
        if (fm != null)
        {
            parts.Add("Fabric rounding: fabric total length rounding " + roundingMm(fm, fm.TotalLengthRounding) + " " + methodText(fm.TotalLengthRoundingMethod)
                + ", fabric segment length rounding " + roundingMm(fm, fm.SegmentLengthRounding) + " " + methodText(fm.SegmentLengthRoundingMethod)
                + " (held by Revit in " + unitName(fm) + ")");
            settingsRead += 4;
        }
        reinforcementSettings = "Reinforcement Settings - " + string.Join(" | ", parts);
    }
}
catch (Exception ex) { findings.Add("The Reinforcement Settings could not be read: " + ex.Message); }

findings.Add(settingsRead + " setting(s) read from " + doc.Title + ". SET_STRUCTURAL_SETTINGS and SET_REINFORCEMENT_SETTINGS "
    + "take each by the name printed here.");
