// NOT STANDALONE. Assumes `doc`, `elements`, `flexDuctType` and `maxLengthMm`
// are in scope; leaves `flexDucts`, `removed`, `notConverted`, `report` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry and
// the caller owns it; nothing here opens or commits one. Each duct is its own
// SubTransaction inside it, so a duct Revit refuses is put back whole and the
// others still convert.
//
// REVIT'S CONVERT TO FLEX DUCT HAS NO API CALL. What it does is done in three
// steps, each present and unchanged 2020 to 2027 (tools/api-surface --members,
// 2026-11-02): read the two connectors the rigid duct is joined to, delete the
// rigid duct (Document.Delete), and draw a flex duct between those two
// connectors (Creation.Document.NewFlexDuct(Connector, Connector,
// FlexDuctType)), which joins both ends as it is made.
//
// DELETING THE RIGID DUCT IS PART OF THE JOB, AND SAID. Every rigid duct this
// removes is in `removed` by id and named in the report. A kept change that
// deletes is asked in Revit first, in its own box (D-142), so this takes no
// `confirm`. Anything Revit would delete with the duct beyond what hangs on it
// - its insulation, lining and tags (Element.GetDependentElements, read BEFORE
// the delete, because afterwards there is nothing left to ask) - or either
// fitting it was joined to, puts that duct back untouched.
//
// ONLY A STRAIGHT DUCT WITH EXACTLY TWO JOINED ENDS. A duct, not a placeholder,
// a flex duct, a pipe or a fitting; its line straight; both end connectors
// joined to one other element each; nothing tapped into its side. Anything
// else is named in `notConverted` with why, and left as it was.
//
// THE FLEX TYPE IS NAMED, NEVER GUESSED. Matched to a loaded flex duct type by
// its type name, or "family: type", ignoring capitals and nothing else. Not
// named, not matched, or matched twice: nothing changes and the loaded flex
// duct types are listed.
//
// SIZE AND SYSTEM KEPT, AND READ BACK. The flex gets the rigid duct's diameter
// (round) or width and height (rectangular), its system type and its level.
// A flex type of another shape than the duct is refused for that duct, before
// anything is deleted - Revit draws flex at the shape of its type. After the
// flex is made its size, its system and both ends are read back; any of them
// wrong puts the duct back and says which.
//
// `maxLengthMm` IS REVIT'S OWN MAXIMUM LENGTH on its Convert to Flex Duct:
// a straight longer than it is left and named. Left out, it is 0 - no limit.
//
// ELEMENT OWNERSHIP IS A NORMAL OUTCOME (Article 12): a duct, or a fitting it
// joins, borrowed by somebody else is named and left, never taken.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER. Lengths are feet
// x 304.8, said in mm (D-20).

var flexDucts = new List<ElementId>();
var removed = new List<ElementId>();
var notConverted = new List<string>();
var findings = new List<string>();
string report = "";

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString("0", invariant);
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<Element, string> named = e =>
    (e.Category == null ? "element" : e.Category.Name) + " '" + e.Name + "' (id " + e.Id + ")";
Func<Element, ConnectorManager> connectorsOf = e =>
{
    var curve = e as MEPCurve;
    if (curve != null) return curve.ConnectorManager;
    var instance = e as FamilyInstance;
    return instance != null && instance.MEPModel != null ? instance.MEPModel.ConnectorManager : null;
};
Func<ConnectorProfileType, string> shapeWord = s =>
    s == ConnectorProfileType.Round ? "round" : s == ConnectorProfileType.Rectangular ? "rectangular"
  : s == ConnectorProfileType.Oval ? "oval" : s.ToString();
Func<Element, string> borrowedBy = e =>
{
    if (!doc.IsWorkshared) return null;
    string owner;
    return WorksharingUtils.GetCheckoutStatus(doc, e.Id, out owner) == CheckoutStatus.OwnedByOtherUser
        ? (string.IsNullOrEmpty(owner) ? "another user" : owner) : null;
};

var given = new List<Element>();
var seen = new HashSet<ElementId>();
foreach (var e in elements ?? new List<Element>())
    if (e != null && seen.Add(e.Id)) given.Add(e);

// ---- the flex type, by name -------------------------------------------------------------
var loadedTypes = new FilteredElementCollector(doc).OfClass(typeof(FlexDuctType))
    .Cast<FlexDuctType>().OrderBy(t => t.FamilyName + ": " + t.Name).ToList();
var typeNames = loadedTypes.Count == 0 ? "none - load a flex duct type first"
    : string.Join(", ", loadedTypes.Select(t => quoted(t.FamilyName + ": " + t.Name)
        + " (" + shapeWord(t.Shape) + ")").ToArray());
var wanted = (flexDuctType ?? "").Trim();
FlexDuctType chosen = null;
var refused = "";
if (wanted.Length == 0)
    refused = "No flex duct type was named. The flex duct types in this project are: " + typeNames
            + ". Name one. Nothing was changed";
else
{
    var matches = loadedTypes.Where(t =>
        string.Equals((t.Name ?? "").Trim(), wanted, StringComparison.OrdinalIgnoreCase)
        || string.Equals((t.FamilyName + ": " + t.Name).Trim(), wanted, StringComparison.OrdinalIgnoreCase)).ToList();
    if (matches.Count == 1) chosen = matches[0];
    else if (matches.Count > 1)
        refused = "More than one flex duct type answers to " + quoted(wanted) + ": "
                + string.Join(", ", matches.Select(t => quoted(t.FamilyName + ": " + t.Name)).ToArray())
                + " - name it as family: type. Nothing was changed";
    else
        refused = "No flex duct type is called " + quoted(wanted) + ". The flex duct types in this project are: "
                + typeNames + ". Nothing was changed";
}
var limit = maxLengthMm > 0 ? maxLengthMm / 304.8 : 0.0;
const double near = 1.0 / 304.8;   // 1 mm, to find a connector again by where it is

// ---- one duct at a time -------------------------------------------------------------------
var lines = new List<string>();
if (refused.Length == 0)
{
    foreach (var e in given)
    {
        var duct = e as Duct;
        if (duct == null)
        {
            notConverted.Add(named(e) + ": " + (e is FlexDuct ? "already a flex duct"
                : "not a duct - only a straight duct is converted to flex") + ". Left as it is");
            continue;
        }
        if (duct.IsPlaceholder)
        {
            notConverted.Add(named(e) + ": a duct placeholder - Convert Placeholder makes it a real duct first. Left as it is");
            continue;
        }
        var located = duct.Location as LocationCurve;
        var line = located == null ? null : located.Curve as Line;
        if (line == null)
        {
            notConverted.Add(named(e) + ": not a straight duct. Left as it is");
            continue;
        }
        if (limit > 0 && line.Length > limit + 1e-9)
        {
            notConverted.Add(named(e) + ": " + mm(line.Length) + " mm long, over the maximum length of "
                + maxLengthMm.ToString("0", invariant) + " mm. Left as it is");
            continue;
        }
        var who = borrowedBy(duct);
        if (who != null)
        {
            notConverted.Add(named(e) + ": borrowed by " + who + " - left alone; ask them to relinquish it");
            continue;
        }

        // ---- its two joined ends ----------------------------------------------------------
        var ends = new List<Connector>();
        var sideTaps = 0;
        foreach (Connector c in duct.ConnectorManager.Connectors)
        {
            if (c.ConnectorType == ConnectorType.End) ends.Add(c);
            else if (c.ConnectorType == ConnectorType.Curve && c.IsConnected) sideTaps++;
        }
        var neighbourIds = new List<ElementId>();
        var neighbourAt = new List<XYZ>();
        var endProblem = "";
        if (sideTaps > 0) endProblem = "something is tapped into its side";
        else if (ends.Count != 2) endProblem = "it has " + ends.Count + " end connector(s), not two";
        else
        {
            foreach (var end in ends)
            {
                var joined = new List<Connector>();
                foreach (Connector other in end.AllRefs)
                    if (other.Owner != null && other.Owner.Id != duct.Id && !(other.Owner is MEPSystem)
                        && other.ConnectorType == ConnectorType.End)
                        joined.Add(other);
                if (joined.Count != 1)
                {
                    endProblem = joined.Count == 0 ? "an end is not joined to anything - Convert to Flex Duct needs both ends joined"
                                                   : "an end is joined to " + joined.Count + " elements";
                    break;
                }
                neighbourIds.Add(joined[0].Owner.Id);
                neighbourAt.Add(joined[0].Origin);
            }
            if (endProblem.Length == 0 && neighbourIds[0] == neighbourIds[1])
                endProblem = "both its ends are joined to the same element";
        }
        if (endProblem.Length > 0)
        {
            notConverted.Add(named(e) + ": " + endProblem + ". Left as it is");
            continue;
        }
        var heldNeighbour = neighbourIds.Select(id => doc.GetElement(id)).Where(n => n != null && borrowedBy(n) != null).ToList();
        if (heldNeighbour.Count > 0)
        {
            notConverted.Add(named(e) + ": " + named(heldNeighbour[0]) + " it joins is borrowed by " + borrowedBy(heldNeighbour[0])
                + " - left alone");
            continue;
        }

        // ---- shape, size, system and level, read before anything is touched ---------------
        var shape = ends[0].Shape;
        if (shape != chosen.Shape)
        {
            notConverted.Add(named(e) + ": a " + shapeWord(shape) + " duct, and " + quoted(chosen.FamilyName + ": " + chosen.Name)
                + " is " + shapeWord(chosen.Shape) + " - name a " + shapeWord(shape) + " flex duct type. Left as it is");
            continue;
        }
        var round = shape == ConnectorProfileType.Round;
        var sizeWas = round ? new[] { duct.Diameter } : new[] { duct.Width, duct.Height };
        var sizeSaid = round ? "d" + mm(sizeWas[0]) : mm(sizeWas[0]) + "x" + mm(sizeWas[1]);
        var systemParam = duct.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM);
        var systemWas = systemParam == null ? ElementId.InvalidElementId : systemParam.AsElementId();
        var systemName = systemWas == ElementId.InvalidElementId || doc.GetElement(systemWas) == null
            ? "no system" : doc.GetElement(systemWas).Name;
        var levelWas = duct.ReferenceLevel;
        var ductName = named(e);
        var ductId = duct.Id;
        var lengthSaid = mm(line.Length);

        var mayGoWith = new HashSet<ElementId>();
        var hanging = 0;
        try
        {
            foreach (var id in duct.GetDependentElements(null) ?? new List<ElementId>())
                if (id != ductId && !neighbourIds.Contains(id)) mayGoWith.Add(id);
        }
        catch (Exception) { }

        // ---- delete, draw, read back - or put back ----------------------------------------
        string putBack = null;
        FlexDuct flex = null;
        using (var attempt = new SubTransaction(doc))
        {
            try
            {
                attempt.Start();
                var deleted = doc.Delete(ductId);
                var unexpected = new List<string>();
                foreach (var id in deleted ?? new List<ElementId>())
                {
                    if (id == ductId) continue;
                    if (neighbourIds.Contains(id)) unexpected.Add("the element it joins (id " + id + ")");
                    else if (mayGoWith.Contains(id)) hanging++;
                    else unexpected.Add("id " + id);
                }
                if (unexpected.Count > 0)
                    putBack = "Revit would also have deleted " + string.Join(", ", unexpected.ToArray());

                var at = new List<Connector>();
                if (putBack == null)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var neighbour = doc.GetElement(neighbourIds[i]);
                        var manager = neighbour == null ? null : connectorsOf(neighbour);
                        Connector found = null;
                        if (manager != null)
                            foreach (Connector c in manager.Connectors)
                                if (c.ConnectorType == ConnectorType.End && c.Origin.DistanceTo(neighbourAt[i]) < near
                                    && (found == null || !c.IsConnected))
                                    found = c;
                        if (found == null) { putBack = "the open end of the element it joined (id " + neighbourIds[i] + ") could not be found again"; break; }
                        at.Add(found);
                    }
                }

                if (putBack == null)
                {
                    flex = doc.Create.NewFlexDuct(at[0], at[1], chosen);
                    if (flex == null) putBack = "Revit made no flex duct between the two ends";
                }
                if (putBack == null)
                {
                    if (levelWas != null)
                        try { flex.ReferenceLevel = levelWas; } catch (Exception) { }
                    var sizeNames = round ? new[] { BuiltInParameter.RBS_CURVE_DIAMETER_PARAM }
                                          : new[] { BuiltInParameter.RBS_CURVE_WIDTH_PARAM, BuiltInParameter.RBS_CURVE_HEIGHT_PARAM };
                    for (int i = 0; i < sizeNames.Length; i++)
                    {
                        var p = flex.get_Parameter(sizeNames[i]);
                        if (p != null && !p.IsReadOnly && Math.Abs(p.AsDouble() - sizeWas[i]) > near / 2) p.Set(sizeWas[i]);
                    }
                    var flexSystem = flex.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM);
                    if (systemWas != ElementId.InvalidElementId && flexSystem != null && !flexSystem.IsReadOnly
                        && flexSystem.AsElementId() != systemWas)
                        flexSystem.Set(systemWas);
                    doc.Regenerate();

                    var sizeNow = round ? new[] { flex.Diameter } : new[] { flex.Width, flex.Height };
                    var openEnds = 0;
                    foreach (Connector c in flex.ConnectorManager.Connectors)
                        if (c.ConnectorType == ConnectorType.End && !c.IsConnected) openEnds++;
                    var systemNow = flexSystem == null ? ElementId.InvalidElementId : flexSystem.AsElementId();
                    if (openEnds > 0)
                        putBack = openEnds + " end(s) of the new flex duct did not join";
                    else if (Enumerable.Range(0, sizeWas.Length).Any(i => Math.Abs(sizeNow[i] - sizeWas[i]) > near / 2))
                        putBack = "Revit made the flex " + (round ? "d" + mm(sizeNow[0]) : mm(sizeNow[0]) + "x" + mm(sizeNow[1]))
                                + ", not " + sizeSaid;
                    else if (systemWas != ElementId.InvalidElementId && systemNow != systemWas)
                        putBack = "the new flex duct is not on " + quoted(systemName);
                }

                if (putBack != null) attempt.RollBack();
                else attempt.Commit();
            }
            catch (Exception ex)
            {
                try { if (attempt.HasStarted() && !attempt.HasEnded()) attempt.RollBack(); } catch (Exception) { }
                putBack = "Revit refused - " + ex.Message;
            }
        }

        if (putBack != null)
        {
            notConverted.Add(ductName + ": " + putBack + " - put back as it was");
            continue;
        }
        removed.Add(ductId);
        flexDucts.Add(flex.Id);
        lines.Add(ductName + " (" + lengthSaid + " mm) removed and replaced by flex duct id " + flex.Id + ", "
            + sizeSaid + " on " + quoted(systemName) + ", joined at both ends"
            + (hanging > 0 ? "; " + hanging + " element(s) hanging on it - insulation, lining or tags - went with it" : ""));
    }
}

// ---- the answer ---------------------------------------------------------------------------
if (refused.Length > 0)
{
    report = refused;
    findings.Add(refused);
}
else if (given.Count == 0)
    report = "Nothing was handed in - select the straight ducts to turn into flex. Nothing was changed";
else
    report = "Convert to flex duct with " + quoted(chosen.FamilyName + ": " + chosen.Name) + ": " + flexDucts.Count + " of "
           + given.Count + " converted"
           + (lines.Count > 0 ? " - " + string.Join("; ", lines.ToArray()) : "")
           + ". Rigid duct(s) removed: " + (removed.Count == 0 ? "none" : string.Join(", ", removed.Select(id => id.ToString()).ToArray()))
           + ". " + notConverted.Count + " left as it was"
           + (notConverted.Count > 0 ? " - " + string.Join("; ", notConverted.ToArray()) : "");
