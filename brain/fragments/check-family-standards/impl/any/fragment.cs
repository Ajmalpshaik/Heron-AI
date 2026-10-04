// NOT STANDALONE. Assumes `doc`, `namePattern`, `requiredParameters` and
// `mustHaveConnectors` are in scope; leaves `findings`, `offStandard`,
// `notChecked`, `warned` and `familyReport` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// TWO DOCUMENTS, TWO AUDITS, ONE QUESTION - IS THIS FAMILY BUILT RIGHT?
//
// IN A PROJECT it audits the family TYPES loaded there (version 1, unchanged).
// IN A FAMILY OPEN IN THE FAMILY EDITOR (version 2, 2026-10-04) it audits that
// family itself, BEFORE it is loaded anywhere: will it join its run, and will
// it follow the size of the pipe or duct it is put on? Each check answers
// PASS, FAIL or WARN with the fix in Revit words, and the whole answer is one
// string, `familyReport`, because a list reaches the reply cut to three.
//
// ---------------------------------------------------------------------------
// IN A PROJECT
// ---------------------------------------------------------------------------
//
// IT AUDITS TYPES, NOT INSTANCES. One badly built type placed two hundred times
// is ONE thing to fix; an instance-based report shows it as two hundred
// findings and buries everything else. The instance count beside each row is
// what makes it urgent.
//
// CONNECTORS ARE READ THROUGH A PLACED INSTANCE. A FamilySymbol does not expose
// them - there is no symbol.Connectors - so one instance per type is sampled. A
// type with NO instance placed is reported NOT CHECKED rather than passed, and
// that distinction is the difference between an audit and a guess.
//
// SYSTEM FAMILIES HAVE NO FAMILY FILE - duct types, wall types, pipe types.
// Counted and excluded, never silently skipped.
//
// ---------------------------------------------------------------------------
// IN A FAMILY
// ---------------------------------------------------------------------------
//
// THE CHECKS GO BY CATEGORY AND PART TYPE, NEVER BY WHAT THE FAMILY IS CALLED.
// Pipe and duct accessories are INLINE: they break into their run. Pipe, duct,
// cable tray and conduit fittings are the run's own joints. Equipment,
// terminals, fixtures and devices sit at the end of a run. Every other
// category gets the name, parameter and tidiness checks only.
//
// 1. PLACEMENT. An inline accessory or a fitting that is Work Plane-Based will
//    not break into a pipe - measured 2026-10-04 on two PPR check valves that
//    would not connect until it was No. An inline accessory whose Part Type is
//    Normal will not break in either; a fitting whose Part Type is Normal is in
//    no routing preference group.
// 2. CONNECTORS. One pointing toward the family's own centre (FRAGMENT-ISSUES
//    5b-309); a pass-through pair not opposite on one line; a size no family
//    parameter drives; a size driven by a TYPE parameter, or by one with a
//    formula, which a pipe cannot change - so the family keeps its size when
//    the pipe is resized; and an inline accessory's System Classification not
//    Global (a WARN: some offices fix it on purpose).
// 3. TIDINESS. A family parameter nothing uses - no formula, label, array,
//    form field, nested family or connector reads it; a solid form shown at
//    Coarse or Medium where a nested family already draws that level.
//
// WHERE HERON HAS THE FIX, THE TOOL IS NAMED. SET_FAMILY_SETTINGS,
// SET_FAMILY_CONNECTOR_ROLES, DELETE_FAMILY_PARAMETERS and
// SET_FAMILY_FORM_VISIBILITY. The type-to-instance switch is still being built
// (FRAGMENT-ISSUES 5b-308), so that fix is given in the Family Types dialog.

var findings = new List<string>();
var offStandard = 0;
var notChecked = 0;
var warned = 0;
var familyReport = "";

if (doc.IsFamilyDocument)
{
    var invariant = System.Globalization.CultureInfo.InvariantCulture;
    var halfMillimetre = 0.5 / 304.8;
    Func<double, string> mm = feet => Math.Round(feet * 304.8, 1).ToString(invariant);
    Func<string, string> squash = text =>
        new string((text ?? "").ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());

    // "ValveBreaksInto" read as "Valve Breaks Into", the way a modeller says it.
    Func<string, string> spaced = name =>
    {
        var text = new System.Text.StringBuilder();
        for (var i = 0; i < (name ?? "").Length; i++)
        {
            if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1])) text.Append(' ');
            text.Append(name[i]);
        }
        return text.ToString();
    };

    var family = doc.OwnerFamily;
    var fm = doc.FamilyManager;
    var category = family == null ? null : family.FamilyCategory;
    var categoryName = category == null ? "no category" : category.Name;
    var familyName = (doc.Title ?? "").EndsWith(".rfa", StringComparison.OrdinalIgnoreCase)
        ? doc.Title.Substring(0, doc.Title.Length - 4) : (doc.Title ?? "");

    // A CATEGORY BY ITS BuiltInCategory NAME, read on the running Revit, so a
    // name one release has not got is simply not this family's category.
    Func<string, bool> isCategory = name =>
    {
        if (category == null) return false;
        BuiltInCategory bic;
        if (!Enum.TryParse<BuiltInCategory>(name, out bic)) return false;
        return category.Id.Equals(new ElementId(bic));
    };
    var inlineAccessory = isCategory("OST_PipeAccessory") || isCategory("OST_DuctAccessory");
    var fitting = isCategory("OST_PipeFitting") || isCategory("OST_DuctFitting")
        || isCategory("OST_CableTrayFitting") || isCategory("OST_ConduitFitting");
    var endOfRun = new[]
    {
        "OST_MechanicalEquipment", "OST_PlumbingFixtures", "OST_PlumbingEquipment", "OST_DuctTerminal",
        "OST_Sprinklers", "OST_ElectricalEquipment", "OST_ElectricalFixtures", "OST_LightingFixtures",
        "OST_LightingDevices", "OST_CommunicationDevices", "OST_DataDevices", "OST_FireAlarmDevices",
        "OST_SecurityDevices", "OST_NurseCallDevices", "OST_TelephoneDevices", "OST_MechanicalControlDevices",
        "OST_MedicalEquipment", "OST_FireProtection",
    }.Any(n => isCategory(n));
    var mep = inlineAccessory || fitting || endOfRun;

    var rows = new List<string>();
    var passed = 0;
    Action<string, string> fail = (what, fix) =>
    {
        offStandard++;
        rows.Add("FAIL " + what + (string.IsNullOrEmpty(fix) ? "" : " FIX: " + fix));
    };
    Action<string, string> warn = (what, fix) =>
    {
        warned++;
        rows.Add("WARN " + what + (string.IsNullOrEmpty(fix) ? "" : " FIX: " + fix));
    };
    Action<string> pass = what => { passed++; rows.Add("PASS " + what); };
    Action<string> notRead = what => { notChecked++; rows.Add("NOT CHECKED " + what); };

    // ---- every family parameter, once
    var parameters = new List<FamilyParameter>();
    foreach (FamilyParameter p in fm.Parameters) parameters.Add(p);
    var parameterNames = parameters.Select(p => p.Definition.Name).ToList();

    // THE NAME AS A WHOLE TOKEN IN A FORMULA - DELETE_FAMILY_PARAMETERS' rule,
    // the same words: "Width" is not found inside "Width Offset".
    Func<string, string, bool> formulaUses = (formula, name) =>
    {
        if (string.IsNullOrEmpty(formula) || string.IsNullOrEmpty(name)) return false;
        var text = formula;
        foreach (var longer in parameterNames.Where(n => n.Length > name.Length
            && n.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0).OrderByDescending(n => n.Length))
        {
            var at = text.IndexOf(longer, StringComparison.OrdinalIgnoreCase);
            while (at >= 0)
            {
                text = text.Substring(0, at) + new string('#', longer.Length) + text.Substring(at + longer.Length);
                at = text.IndexOf(longer, at + longer.Length, StringComparison.OrdinalIgnoreCase);
            }
        }
        Func<char, bool> joins = c => char.IsLetterOrDigit(c) || c == '_' || c == '#';
        var from = text.IndexOf(name, StringComparison.OrdinalIgnoreCase);
        while (from >= 0)
        {
            var end = from + name.Length;
            if ((from == 0 || !joins(text[from - 1])) && (end >= text.Length || !joins(text[end]))) return true;
            from = text.IndexOf(name, from + 1, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    };

    // Every parameter whose formula reads this one, directly or through
    // another - what has to change with it when it changes kind.
    Func<string, List<string>> dependents = name =>
    {
        var found = new List<string>();
        var frontier = new List<string> { name };
        while (frontier.Count > 0)
        {
            var next = parameters
                .Where(p => !found.Contains(p.Definition.Name) && p.Definition.Name != name)
                .Where(p => frontier.Any(f => formulaUses(p.Formula, f)))
                .Select(p => p.Definition.Name).ToList();
            found.AddRange(next);
            frontier = next;
        }
        return found;
    };

    // =======================================================================
    // NAME AND REQUIRED PARAMETERS - the project audit's two questions, asked
    // of the family itself
    // =======================================================================

    if (!string.IsNullOrEmpty(namePattern))
    {
        if (familyName.IndexOf(namePattern, StringComparison.OrdinalIgnoreCase) < 0)
            fail("Name: \"" + familyName + "\" does not contain \"" + namePattern + "\".",
                "rename the family file before it is loaded (RENAME_FAMILY renames it in a project).");
        else pass("Name contains \"" + namePattern + "\".");
    }
    foreach (var wanted in requiredParameters ?? new List<string>())
    {
        if (string.IsNullOrEmpty(wanted)) continue;
        var matches = parameters.Count(p => string.Equals(p.Definition.Name, wanted, StringComparison.Ordinal));
        if (matches == 0)
            fail("Required parameter \"" + wanted + "\" is not in this family.",
                "add it in Family Types (ADD_FAMILY_PARAMETERS).");
        else if (matches > 1)
            warn("Required parameter \"" + wanted + "\" is in this family " + matches + " times - a schedule or "
                + "tag reads whichever Revit finds first.", "keep one, delete the other.");
        else pass("Required parameter \"" + wanted + "\" is present.");
    }

    // =======================================================================
    // 1. PLACEMENT - the switches that stop a family joining its run
    // =======================================================================

    Func<BuiltInParameter, int?> setting = bip =>
    {
        try
        {
            var p = family == null ? null : family.get_Parameter(bip);
            return p == null || p.StorageType != StorageType.Integer ? (int?)null : p.AsInteger();
        }
        catch (Exception) { return null; }
    };
    var workPlaneBased = setting(BuiltInParameter.FAMILY_WORK_PLANE_BASED);
    var partValue = setting(BuiltInParameter.FAMILY_CONTENT_PART_TYPE);
    var partType = partValue.HasValue && Enum.IsDefined(typeof(PartType), partValue.Value)
        ? Enum.GetName(typeof(PartType), partValue.Value) : null;
    var partWords = partType == null ? "unread" : spaced(partType);

    // THE CONNECTORS, grouped by domain: a pass-through pair is two of one kind.
    var connectors = new FilteredElementCollector(doc).OfClass(typeof(ConnectorElement))
        .Cast<ConnectorElement>().OrderBy(c => c.Id.ToString(), StringComparer.Ordinal).ToList();
    Func<ConnectorElement, string> domainWords = c =>
        c.Domain == Domain.DomainHvac ? "duct" : c.Domain == Domain.DomainPiping ? "pipe"
        : c.Domain == Domain.DomainElectrical ? "electrical"
        : c.Domain == Domain.DomainCableTrayConduit ? "cable tray or conduit" : c.Domain.ToString();
    Func<ConnectorElement, string> label = c => domainWords(c) + " connector " + c.Id.ToString();
    var runDomain = connectors.Where(c => c.Domain == Domain.DomainHvac || c.Domain == Domain.DomainPiping
            || c.Domain == Domain.DomainCableTrayConduit)
        .GroupBy(c => c.Domain).OrderByDescending(g => g.Count()).FirstOrDefault();
    var runConnectors = runDomain == null ? new List<ConnectorElement>() : runDomain.ToList();

    // Part types that break into a run, by enum name - a name a release has
    // not got is simply never matched.
    var breaksInto = new[] { "BreaksInto", "ValveBreaksInto", "InlineSensor", "Damper" };
    var passThroughFittings = new[] { "Union", "Transition", "PipeFlange", "PipeMechanicalCoupling" };

    if (inlineAccessory || fitting)
    {
        if (!workPlaneBased.HasValue)
            notRead("Work Plane-Based: this family has no such setting to read.");
        else if (workPlaneBased.Value == 1)
            fail("Work Plane-Based is Yes on a " + categoryName + " family. A family that breaks into its run "
                + "must be No (Always Vertical Yes) - Work Plane-Based ones are placed on a plane, not in a pipe or "
                + "duct, and will not connect.",
                "Family Category and Parameters, Work Plane-Based off (SET_FAMILY_SETTINGS \"Work Plane-Based=No\").");
        else pass("Work Plane-Based is No.");
    }

    if (inlineAccessory)
    {
        if (partType == null)
            notRead("Part Type could not be read.");
        else if (breaksInto.Contains(partType))
            pass("Part Type is " + partWords + ".");
        else if (runConnectors.Count >= 2 && (partType == "Normal" || partType == "Undefined"))
            fail("Part Type is " + partWords + " on a " + categoryName + " family with " + runConnectors.Count + " "
                + domainWords(runConnectors[0]) + " connectors. An inline accessory breaks into its run only when its "
                + "Part Type is Breaks Into (Valve - Breaks Into for a valve) or In-line Sensor.",
                "Family Category and Parameters, Part Type (SET_FAMILY_SETTINGS \"Part Type=Valve Breaks Into\").");
        else
            warn("Part Type is " + partWords + " with " + runConnectors.Count + " run connector(s) - it will not "
                + "break into a pipe or duct. Right for an accessory that attaches to the outside of one; wrong for "
                + "one meant to sit in the run.",
                "if it should break in, SET_FAMILY_SETTINGS \"Part Type=Breaks Into\".");
    }
    else if (fitting)
    {
        if (partType == null)
            notRead("Part Type could not be read.");
        else if (partType == "Normal" || partType == "Undefined")
            fail("Part Type is " + partWords + " on a " + categoryName + " family. A fitting is offered by routing "
                + "preferences, and resized with its run, only by its Part Type - Elbow, Tee, Transition, Union, Cap.",
                "SET_FAMILY_SETTINGS \"Part Type=Elbow\" (or the part it is).");
        else pass("Part Type is " + partWords + ".");
    }

    // THE FAMILY'S OWN CENTRE: the middle of everything solid it draws -
    // its solid forms and its nested families. Voids are left out: they
    // cut, they do not draw. With nothing solid, the middle of the
    // connectors, when there are two or more.
    XYZ low = null, high = null;
    Action<BoundingBoxXYZ> grow = box =>
    {
        if (box == null) return;
        low = low == null ? box.Min : new XYZ(Math.Min(low.X, box.Min.X), Math.Min(low.Y, box.Min.Y), Math.Min(low.Z, box.Min.Z));
        high = high == null ? box.Max : new XYZ(Math.Max(high.X, box.Max.X), Math.Max(high.Y, box.Max.Y), Math.Max(high.Z, box.Max.Z));
    };
    var solidForms = new List<GenericForm>();
    var nestedInstances = new List<FamilyInstance>();
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
    {
        var form = element as GenericForm;
        var nested = element as FamilyInstance;
        // A nested annotation symbol draws in plan only; it is not the body.
        if (nested != null && nested.Category != null && nested.Category.CategoryType == CategoryType.Annotation) continue;
        try
        {
            if (form != null && form.IsSolid) { solidForms.Add(form); grow(form.get_BoundingBox(null)); }
            else if (nested != null) { nestedInstances.Add(nested); grow(nested.get_BoundingBox(null)); }
        }
        catch (Exception) { }
    }
    XYZ centre = null;
    var centreFrom = "";
    if (low != null) { centre = (low + high) / 2; centreFrom = "the middle of its solid forms and nested families"; }
    else if (connectors.Count >= 2)
    {
        var sum = XYZ.Zero;
        foreach (var c in connectors) sum = sum + c.Origin;
        centre = sum / connectors.Count;
        centreFrom = "the middle of its connectors";
    }

    // =======================================================================
    // 2. CONNECTORS
    // =======================================================================

    if (connectors.Count == 0)
    {
        if (mep || mustHaveConnectors)
            fail("No connectors. A " + categoryName + " family with none cannot join a system, carry flow or show "
                + "in the System Browser, while looking normal in every plan.",
                "ADD_FAMILY_CONNECTOR on each port face.");
    }
    else if (mep || mustHaveConnectors)
    {
        // ---- pointing inward
        foreach (var c in connectors)
        {
            XYZ origin = null, direction = null;
            try { origin = c.Origin; direction = c.Direction; }
            catch (Exception) { }
            if (centre == null || origin == null || direction == null)
            {
                notRead("Which way " + label(c) + " points: the family has no solid form to find its centre by.");
                continue;
            }
            var toCentre = centre - origin;
            if (toCentre.GetLength() < halfMillimetre * 2)
            {
                notRead("Which way " + label(c) + " points: it sits at the family's centre.");
                continue;
            }
            if (toCentre.Normalize().DotProduct(direction) > 0.5)
                fail(label(c) + " at X " + mm(origin.X) + ", Y " + mm(origin.Y) + ", Z " + mm(origin.Z) + " mm points "
                    + "INTO the family, toward " + centreFrom + ". A pipe or duct joined to it would run back through "
                    + "the body; the arrow must point out of the port.",
                    "SET_FAMILY_CONNECTOR_ROLES settings \"flip=yes\" on it (or the connector's Flip control).");
            else pass(label(c) + " points out of the family.");
        }

        // ---- a pass-through pair, opposite and on one line
        var pairExpected = (inlineAccessory && runConnectors.Count == 2)
            || (fitting && runConnectors.Count == 2 && partType != null && passThroughFittings.Contains(partType));
        if (pairExpected)
        {
            var a = runConnectors[0];
            var b = runConnectors[1];
            try
            {
                var opposite = a.Direction.DotProduct(b.Direction) < -0.9999;
                var offset = (b.Origin - a.Origin).CrossProduct(a.Direction).GetLength();
                if (!opposite)
                    fail("The two " + domainWords(a) + " connectors " + a.Id + " and " + b.Id + " are not opposite each "
                        + "other, so the family cannot sit in a straight run - a pipe or duct reaching one port meets "
                        + "the other at an angle.",
                        "flip the one pointing the wrong way (SET_FAMILY_CONNECTOR_ROLES \"flip=yes\"), or move it to "
                        + "the opposite face.");
                else if (offset > halfMillimetre)
                    warn("The two " + domainWords(a) + " connectors " + a.Id + " and " + b.Id + " are opposite but "
                        + mm(offset) + " mm off one line - the run steps sideways through the family. Right for an "
                        + "eccentric part; wrong for a straight-through valve.",
                        "line the two port faces up on one reference plane.");
                else pass("The two " + domainWords(a) + " connectors are opposite each other on one line.");
            }
            catch (Exception) { notRead("Whether the two run connectors line up: their directions could not be read."); }
        }

        // ---- size: associated, instance, not a formula
        var checkedDrivers = new List<string>();
        foreach (var c in connectors)
        {
            if (c.Domain == Domain.DomainElectrical) continue;
            var sizeParameters = new List<BuiltInParameter>();
            try
            {
                if (c.Shape == ConnectorProfileType.Round)
                    sizeParameters.AddRange(new[] { BuiltInParameter.CONNECTOR_DIAMETER, BuiltInParameter.CONNECTOR_RADIUS });
                else sizeParameters.AddRange(new[] { BuiltInParameter.CONNECTOR_WIDTH, BuiltInParameter.CONNECTOR_HEIGHT });
            }
            catch (Exception) { notRead("The size of " + label(c) + ": its shape could not be read."); continue; }

            var drivers = new List<FamilyParameter>();
            foreach (var bip in sizeParameters)
            {
                try
                {
                    var own = c.get_Parameter(bip);
                    var driver = own == null ? null : fm.GetAssociatedFamilyParameter(own);
                    if (driver != null) drivers.Add(driver);
                }
                catch (Exception) { }
            }
            // Round: diameter OR radius drives it. Rectangular: both sides must.
            var sized = c.Shape == ConnectorProfileType.Round ? drivers.Count > 0 : drivers.Count >= 2;
            var followsRun = inlineAccessory || fitting;
            if (!sized)
            {
                var what = label(c) + "'s size is driven by " + (drivers.Count == 0 ? "no family parameter"
                    : "only " + drivers[0].Definition.Name) + ", so it stays the size it was drawn at.";
                var fix = "associate the connector's " + (c.Shape == ConnectorProfileType.Round ? "Diameter" : "Width and Height")
                    + " with the family's size parameter (SET_FAMILY_CONNECTOR_ROLES settings \"sizeParameters=<the size parameter>\").";
                if (followsRun) fail(what + " A " + categoryName + " family has to take its pipe or duct's size.", fix);
                else warn(what + " Right only if every type of this " + categoryName + " has one fixed size.", fix);
                continue;
            }
            foreach (var driver in drivers)
            {
                var name = driver.Definition.Name;
                if (checkedDrivers.Contains(name)) continue;
                checkedDrivers.Add(name);
                if (!followsRun) { pass(label(c) + "'s size is driven by \"" + name + "\"."); continue; }

                var reading = dependents(name);
                var direct = parameters.Count(p => p.Definition.Name != name && formulaUses(p.Formula, name));
                if (!driver.IsInstance)
                    fail("\"" + name + "\", which sizes " + label(c) + ", is a TYPE parameter - so the family keeps its "
                        + "type's size and will NOT follow the pipe or duct it is placed on. " + reading.Count
                        + " formula(s) depend on it (" + direct + " directly)"
                        + (reading.Count > 0 ? ": " + string.Join(", ", reading.Take(12)) + (reading.Count > 12 ? ", ..." : "") : "")
                        + ".",
                        "Family Types, select \"" + name + "\", Modify, Instance - and every parameter whose formula "
                        + "reads it must become Instance with it, because a type formula cannot read an instance "
                        + "parameter. Heron's own switch for this is being built (FRAGMENT-ISSUES 5b-308).");
                else if (!string.IsNullOrEmpty(driver.Formula))
                    fail("\"" + name + "\", which sizes " + label(c) + ", is calculated by the formula " + driver.Formula
                        + " - so the pipe or duct cannot set it, and the family will not follow its size.",
                        "clear that formula (SET_FAMILY_FORMULA) and drive the other sizes from \"" + name + "\" instead.");
                else pass("\"" + name + "\", which sizes " + label(c) + ", is an instance parameter with no formula, "
                    + reading.Count + " formula(s) follow it.");
            }
        }

        // ---- system classification on an inline accessory
        if (inlineAccessory)
            foreach (var c in runConnectors)
            {
                string system = null;
                try { system = c.SystemClassification.ToString(); }
                catch (Exception) { }
                if (system == null) notRead("The System Classification of " + label(c) + " could not be read.");
                else if (system == "Global") pass(label(c) + " is Global.");
                else warn(label(c) + " is " + spaced(system) + ", not Global - the accessory only connects to that "
                    + "system. Right if your office fixes it on purpose; otherwise it should take the system of "
                    + "the pipe or duct it is put on.",
                    "SET_FAMILY_CONNECTOR_ROLES settings \"system=Global\".");
            }

    }

    // =======================================================================
    // 3b. A FORM DRAWN TWICE AT COARSE OR MEDIUM
    // =======================================================================
    //
    // A nested family that already draws Coarse or Medium - a simple
    // symbol - while a solid form of this family, in the same place, is
    // shown at that level too: both print, one on top of the other. The
    // nested family's drawing at each level is read from its geometry at
    // that detail level. Only forms inside the nested family's box count,
    // so a body that is merely beside a nested handwheel is not flagged.
    foreach (var nested in nestedInstances)
    {
        var levels = new List<string>();
        foreach (var level in new[] { ViewDetailLevel.Coarse, ViewDetailLevel.Medium })
        {
            var draws = false;
            try
            {
                var options = new Options { DetailLevel = level };
                var geometry = nested.get_Geometry(options);
                if (geometry != null)
                    foreach (var g in geometry)
                    {
                        var instance = g as GeometryInstance;
                        var parts = instance == null ? new List<GeometryObject> { g } : instance.GetInstanceGeometry().Cast<GeometryObject>().ToList();
                        if (parts.Any(part => (part is Solid && ((Solid)part).Volume > 0) || part is Curve)) { draws = true; break; }
                    }
            }
            catch (Exception) { }
            if (draws) levels.Add(level.ToString());
        }
        if (levels.Count == 0) continue;
        var box = nested.get_BoundingBox(null);
        if (box == null) continue;
        var nestedName = "";
        try { nestedName = nested.Symbol.Family.Name; }
        catch (Exception) { nestedName = nested.Name; }
        foreach (var form in solidForms)
        {
            FamilyElementVisibility seen = null;
            try { seen = form.GetVisibility(); }
            catch (Exception) { }
            if (seen == null) continue;
            var both = levels.Where(l => (l == "Coarse" && seen.IsShownInCoarse) || (l == "Medium" && seen.IsShownInMedium)).ToList();
            if (both.Count == 0) continue;
            var formBox = form.get_BoundingBox(null);
            if (formBox == null) continue;
            var inside = formBox.Min.X >= box.Min.X - halfMillimetre && formBox.Min.Y >= box.Min.Y - halfMillimetre
                && formBox.Min.Z >= box.Min.Z - halfMillimetre && formBox.Max.X <= box.Max.X + halfMillimetre
                && formBox.Max.Y <= box.Max.Y + halfMillimetre && formBox.Max.Z <= box.Max.Z + halfMillimetre;
            if (!inside) continue;
            warn(spaced(form.GetType().Name) + " " + form.Id + " is shown at " + string.Join(" and ", both)
                + " inside nested \"" + nestedName + "\", which already draws " + string.Join(" and ", levels)
                + " - both print there, one over the other.",
                "SET_FAMILY_FORM_VISIBILITY on " + form.Id + ": Fine only.");
        }
    }

    // =======================================================================
    // 3a. A FAMILY PARAMETER NOTHING USES
    // =======================================================================
    //
    // DELETE_FAMILY_PARAMETERS' list of what uses a parameter, read the same
    // way: a formula, a dimension or array label, or any element parameter
    // linked to it - a form's field, a nested family's parameter, a
    // connector's size. Revit's built-in parameters are never counted, and
    // neither is one the office REQUIRES. A SHARED one is a WARN: shared
    // parameters carry schedule and tag data that nothing in the family reads.
    var labelled = new HashSet<string>(StringComparer.Ordinal);
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
    {
        FamilyParameter labelOf = null;
        try
        {
            var dimension = element as Dimension;
            var array = element as BaseArray;
            if (dimension != null) labelOf = dimension.FamilyLabel;
            else if (array != null) labelOf = array.Label;
        }
        catch (Exception) { labelOf = null; }   // a spot dimension takes no label, and says so by throwing
        if (labelOf != null) labelled.Add(labelOf.Definition.Name);
    }
    var unused = new List<FamilyParameter>();
    foreach (var p in parameters)
    {
        var name = p.Definition.Name;
        var builtIn = p.Definition as InternalDefinition;
        if (builtIn != null && builtIn.BuiltInParameter != BuiltInParameter.INVALID) continue;
        if (requiredParameters != null && requiredParameters.Contains(name)) continue;
        if (labelled.Contains(name)) continue;
        if (parameters.Any(other => other.Definition.Name != name && formulaUses(other.Formula, name))) continue;
        var linked = 0;
        try { linked = p.AssociatedParameters.Size; }
        catch (Exception) { linked = 1; }
        if (linked > 0) continue;
        unused.Add(p);
    }
    var deadOnes = unused.Where(p => !p.IsShared).Select(p => "\"" + p.Definition.Name + "\"").ToList();
    var sharedOnes = unused.Where(p => p.IsShared).Select(p => "\"" + p.Definition.Name + "\"").ToList();
    if (deadOnes.Count > 0)
        fail(deadOnes.Count + " family parameter(s) nothing uses - no formula, label, array, form field, nested family "
            + "or connector reads them: " + string.Join(", ", deadOnes) + ". They show in Properties and change nothing.",
            "DELETE_FAMILY_PARAMETERS \"" + string.Join(", ", unused.Where(p => !p.IsShared).Select(p => p.Definition.Name)) + "\".");
    if (sharedOnes.Count > 0)
        warn(sharedOnes.Count + " SHARED parameter(s) nothing in the family uses: " + string.Join(", ", sharedOnes)
            + ". Shared parameters usually carry schedule or tag data, so they may be wanted.",
            "delete only the ones no schedule or tag reads (DELETE_FAMILY_PARAMETERS).");
    if (unused.Count == 0) pass("Every family parameter is used by something.");

    familyReport = "\"" + familyName + "\" (" + categoryName
        + (partType == null ? "" : ", Part Type " + partWords)
        + (workPlaneBased.HasValue ? ", Work Plane-Based " + (workPlaneBased.Value == 1 ? "Yes" : "No") : "")
        + ", " + connectors.Count + " connector(s)): " + offStandard + " FAIL, " + warned + " WARN, " + passed + " PASS, "
        + notChecked + " NOT CHECKED. " + string.Join(" | ", rows);
    findings.Add(familyReport);
    if (!mep)
        findings.Add("A " + categoryName + " family is not one this checks for joining a run - only its name, its "
            + "parameters" + (mustHaveConnectors ? " and that it has connectors" : "") + " were checked.");
    findings.Add("Checked in the family, before it is loaded. A copy already in a project keeps the old build until "
        + "the family is loaded there again.");
}
else
{
    var systemFamilies = 0;

    // One placed instance per type, gathered once. Doing it per type is the same
    // query run hundreds of times.
    var sampleByType = new Dictionary<ElementId, FamilyInstance>();
    var countByType = new Dictionary<ElementId, int>();

    foreach (var element in new FilteredElementCollector(doc)
        .OfClass(typeof(FamilyInstance)).WhereElementIsNotElementType())
    {
        var instance = element as FamilyInstance;
        if (instance == null) continue;
        var typeId = instance.GetTypeId();
        if (typeId == ElementId.InvalidElementId) continue;

        if (!sampleByType.ContainsKey(typeId)) sampleByType[typeId] = instance;
        countByType[typeId] = (countByType.ContainsKey(typeId) ? countByType[typeId] : 0) + 1;
    }

    foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)))
    {
        var symbol = element as FamilySymbol;
        if (symbol == null) continue;

        var family = symbol.Family;
        if (family == null) { systemFamilies++; continue; }

        // An in-place family belongs to one model and cannot be a library standard.
        var inPlace = false;
        try { inPlace = family.IsInPlace; }
        catch { }

        var placed = countByType.ContainsKey(symbol.Id) ? countByType[symbol.Id] : 0;
        var faults = new List<string>();

        // ---- name
        if (!string.IsNullOrEmpty(namePattern)
            && family.Name.IndexOf(namePattern, StringComparison.OrdinalIgnoreCase) < 0)
        {
            faults.Add(string.Format("name does not contain '{0}'", namePattern));
        }

        // ---- required parameters, on the TYPE
        foreach (var wanted in requiredParameters)
        {
            if (string.IsNullOrEmpty(wanted)) continue;
            var parameter = symbol.LookupParameter(wanted);
            if (parameter == null) faults.Add(string.Format("no '{0}' parameter", wanted));
        }

        // ---- connectors, through a placed instance only
        if (mustHaveConnectors)
        {
            if (placed == 0)
            {
                notChecked++;
                findings.Add(string.Format("{0} : {1}  - NOT CHECKED for connectors: no instance is placed, "
                    + "and a type does not expose its connectors. Place one and re-run",
                    family.Name, symbol.Name));
            }
            else
            {
                var sample = sampleByType[symbol.Id];
                var connectorCount = 0;
                try
                {
                    var manager = sample.MEPModel == null ? null : sample.MEPModel.ConnectorManager;
                    if (manager != null) connectorCount = manager.Connectors.Size;
                }
                catch { }

                if (connectorCount == 0)
                {
                    faults.Add("NO CONNECTORS - it cannot join a system, cannot carry flow and will never "
                        + "appear in a system browser, while looking normal in every plan");
                }
            }
        }

        if (faults.Count > 0)
        {
            offStandard++;
            findings.Add(string.Format("{0} : {1}  ({2} placed{3})  - {4}",
                family.Name, symbol.Name, placed, inPlace ? ", IN-PLACE" : "",
                string.Join("; ", faults)));
        }
    }

    findings.Insert(0, string.Format("{0} family type(s) off standard, {1} not checked for connectors. "
        + "{2} system family type(s) excluded - a duct type or a wall type has no family file and cannot "
        + "be audited this way", offStandard, notChecked, systemFamilies));
}
