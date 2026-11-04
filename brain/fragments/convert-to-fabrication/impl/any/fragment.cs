// NOT STANDALONE. Assumes `doc`, `elements` and `service` are in scope; leaves
// `fabricationParts`, `notDesign`, `notConverted`, `report` and `findings`
// behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry and
// the caller owns it; nothing here opens or commits one.
//
// REVIT'S DESIGN TO FABRICATION: design ducts, pipes and their fittings and
// accessories become fabrication parts of one loaded service, by the one call
// Revit has for it, unchanged 2020 to 2027 (tools/api-surface --members,
// 2026-11-02): DesignToFabricationConverter(Document), its
// Convert(ISet<ElementId>, int serviceId) returning Success or PartialFailure,
// GetConvertedFabricationParts(), GetPartialConvertFailureResults() and
// GetElementsWithOpenConnector().
//
// REACHED BY REFLECTION, AND WHY. The converter lives in the Fabrication
// namespace under Revit's DB namespace, which is not among the namespaces the
// executor imports (HeronFragmentImports) - and naming it in full would put a
// vendor namespace in brain/, which check-structure refuses. So the type is
// found beside FabricationPart, in the same assembly, by its namespace plus
// ".Fabrication", and its four members are called by name with the signatures
// read above. What Revit throws arrives wrapped in a
// TargetInvocationException, and Revit's own words are unwrapped from it.
//
// THE SERVICE IS NEVER GUESSED. It is matched to a service LOADED in this
// project by its whole name or its abbreviation, ignoring capitals and nothing
// else. Not named, not matched, or matched twice: nothing is converted and the
// loaded services are listed by name, so the modeller picks one. A service in
// the configuration but not loaded is said so, with where it is loaded.
//
// NO FABRICATION CONFIGURATION: refused in Revit words - Systems > Fabrication
// > MEP Fabrication Settings - before anything is touched.
//
// ONLY DESIGN DUCTS, PIPES, FITTINGS AND ACCESSORIES ARE SENT. A duct or pipe
// placeholder (Convert Placeholder first), a flex duct or pipe, a part that is
// already fabrication, equipment, a wall - each goes in `notDesign` with its
// category, type and id and is left as it was. RevitAPI.xml: Convert throws
// for "invalid elements to convert", so this also keeps one wrong element from
// refusing the whole batch.
//
// DUCTWORK AND PIPEWORK GO TO REVIT SEPARATELY. RevitAPI.xml: Convert throws
// when "the specified fabrication service is not valid for all domains in the
// selection" - a ductwork service cannot take pipes. So the duct side and the
// pipe side are each one call; a side Revit refuses is named in
// `notConverted` with Revit's words, and the other side still runs.
//
// ELEMENT OWNERSHIP IS A NORMAL OUTCOME (Article 12): a design element borrowed
// by somebody else on a workshared model is named in `notConverted` with who
// has it, never taken.
//
// WHAT HAPPENS TO THE DESIGN ELEMENTS IS REVIT'S, AND IS READ BACK, NOT
// ASSUMED. Nothing here deletes anything. After a regenerate every design
// element sent is looked at again: gone from the model (Revit replaced it
// with fabrication parts) or still there (Revit left it). Which of those Revit
// does on success is not stated in RevitAPI.xml - see the NEEDS-CHECKING group.
//
// READ BACK: each id Revit returns as a new part is read as a FabricationPart
// with its service, size and straight length; Revit's partial failures are
// said in words, with the elements left with open ends.
//
// AN ElementId IS COMPARED WITH ==, NEVER READ AS A NUMBER. Lengths are feet
// x 304.8, said in mm (D-20).

var fabricationParts = new List<ElementId>();
var notDesign = new List<string>();
var notConverted = new List<string>();
var findings = new List<string>();
string report = "";

var invariant = System.Globalization.CultureInfo.InvariantCulture;
Func<double, string> mm = feet => Math.Round(feet * 304.8, 0).ToString("0", invariant);
Func<string, string> quoted = n => "\"" + (n ?? "") + "\"";
Func<Element, string> named = e =>
    (e.Category == null ? "element" : e.Category.Name) + " '" + e.Name + "' (id " + e.Id + ")";
Func<Element, BuiltInCategory, bool> isOf = (e, c) => e.Category != null && e.Category.Id == new ElementId(c);

var given = new List<Element>();
var seen = new HashSet<ElementId>();
foreach (var e in elements ?? new List<Element>())
    if (e != null && seen.Add(e.Id)) given.Add(e);

var config = FabricationConfiguration.GetFabricationConfiguration(doc);
var wanted = (service ?? "").Trim();
FabricationService chosen = null;
var refused = "";

if (config == null)
    refused = "This model has no fabrication configuration - choose one in Systems > Fabrication > MEP "
            + "Fabrication Settings and load a service, then convert. Nothing was converted";
else
{
    var loaded = (config.GetAllLoadedServices() ?? new List<FabricationService>()).Where(s => s != null).ToList();
    var loadedNames = loaded.Count == 0 ? "none - services are loaded in MEP Fabrication Settings"
        : string.Join(", ", loaded.Select(s => quoted(s.Name)
            + (string.IsNullOrEmpty(s.Abbreviation) ? "" : " (" + s.Abbreviation + ")")).ToArray());
    Func<FabricationService, bool> same = s =>
        string.Equals((s.Name ?? "").Trim(), wanted, StringComparison.OrdinalIgnoreCase)
        || string.Equals((s.Abbreviation ?? "").Trim(), wanted, StringComparison.OrdinalIgnoreCase);

    if (wanted.Length == 0)
        refused = "No fabrication service was named - Revit converts to one service at a time. The services "
                + "loaded in this project are: " + loadedNames + ". Name one. Nothing was converted";
    else
    {
        var matches = loaded.Where(same).ToList();
        if (matches.Count == 1)
            chosen = matches[0];
        else if (matches.Count > 1)
            refused = "More than one loaded service answers to " + quoted(wanted) + ": "
                    + string.Join(", ", matches.Select(s => quoted(s.Name) + " (service id " + s.ServiceId + ")").ToArray())
                    + " - name it by its whole name. Nothing was converted";
        else
        {
            var inConfiguration = (config.GetAllServices() ?? new List<FabricationService>())
                .Where(s => s != null && same(s)).ToList();
            refused = (inConfiguration.Count > 0
                    ? quoted(inConfiguration[0].Name) + " is in the configuration but not loaded in this project - "
                      + "load it in Systems > Fabrication > MEP Fabrication Settings first"
                    : "No loaded service is called " + quoted(wanted))
                + ". The services loaded in this project are: " + loadedNames
                + ". The whole name or the abbreviation is needed. Nothing was converted";
        }
    }
}

var converterType = typeof(FabricationPart).Assembly.GetType(typeof(FabricationPart).Namespace + ".Fabrication.DesignToFabricationConverter");
if (refused.Length == 0 && converterType == null)
    refused = "This Revit's API has no Design to Fabrication converter where Revit 2020 to 2027 keep it - "
            + "nothing was converted";

// ---- sort what was handed in ------------------------------------------------------------
var ductSide = new List<ElementId>();
var pipeSide = new List<ElementId>();
var label = new Dictionary<ElementId, string>();

if (refused.Length == 0)
{
    foreach (var e in given)
    {
        if (e is FabricationPart)
        {
            notDesign.Add(named(e) + ": already a fabrication part - left as it is");
            continue;
        }
        var asDuct = e as Duct;
        var asPipe = e as Pipe;
        if ((asDuct != null && asDuct.IsPlaceholder) || (asPipe != null && asPipe.IsPlaceholder))
        {
            notDesign.Add(named(e) + ": a placeholder - Convert Placeholder makes it a real "
                        + (asDuct != null ? "duct" : "pipe") + " first. Left as it is");
            continue;
        }
        bool duct = asDuct != null || isOf(e, BuiltInCategory.OST_DuctFitting) || isOf(e, BuiltInCategory.OST_DuctAccessory);
        bool pipe = asPipe != null || isOf(e, BuiltInCategory.OST_PipeFitting) || isOf(e, BuiltInCategory.OST_PipeAccessory);
        if (!duct && !pipe)
        {
            notDesign.Add(named(e) + ": not a design duct, pipe, fitting or accessory - only those are converted. Left as it is");
            continue;
        }
        if (doc.IsWorkshared)
        {
            string owner;
            if (WorksharingUtils.GetCheckoutStatus(doc, e.Id, out owner) == CheckoutStatus.OwnedByOtherUser)
            {
                notConverted.Add(named(e) + ": borrowed by " + (string.IsNullOrEmpty(owner) ? "another user" : owner)
                               + " - left alone; ask them to relinquish it");
                continue;
            }
        }
        label[e.Id] = named(e);
        (duct ? ductSide : pipeSide).Add(e.Id);
    }
}

// ---- convert, one call per side -----------------------------------------------------------
var made = new List<ElementId>();
var sent = new List<ElementId>();
var openEnds = new HashSet<ElementId>();
var partial = new List<string>();
Action<List<ElementId>, string> convert = (ids, side) =>
{
    if (ids.Count == 0 || chosen == null) return;
    object converter = null;
    try
    {
        converter = Activator.CreateInstance(converterType, new object[] { doc });
        var result = converterType.GetMethod("Convert", new[] { typeof(ISet<ElementId>), typeof(int) })
            .Invoke(converter, new object[] { new HashSet<ElementId>(ids), chosen.ServiceId });
        sent.AddRange(ids);
        var back = converterType.GetMethod("GetConvertedFabricationParts", Type.EmptyTypes)
            .Invoke(converter, null) as IEnumerable<ElementId>;
        if (back != null) made.AddRange(back);
        if (result != null && result.ToString() == "PartialFailure")
        {
            var whys = converterType.GetMethod("GetPartialConvertFailureResults", Type.EmptyTypes)
                .Invoke(converter, null) as System.Collections.IEnumerable;
            foreach (var why in whys ?? new object[0])
            {
                var word = why == null ? "" : why.ToString();
                var words =
                    word == "NotAllPartsConverted" ? "not every " + side + " element could be converted"
                  : word == "InvalidConnections" ? "some new " + side + " parts have connections Revit calls invalid"
                  : word == "HaveOpenConnectors" ? "some " + side + " parts or elements were left with open ends, where a fitting did not convert"
                  : word == "HaveDifferentOffsets" ? "some new " + side + " parts sit at a different offset from the design they replace"
                  : word == "NoMatchingSize" ? "the service " + quoted(chosen.Name) + " has no matching size for some " + side + " elements"
                  : side + ": " + word;
                if (!partial.Contains(words)) partial.Add(words);
            }
            var open = converterType.GetMethod("GetElementsWithOpenConnector", Type.EmptyTypes)
                .Invoke(converter, null) as IEnumerable<ElementId>;
            foreach (var id in open ?? new List<ElementId>())
                openEnds.Add(id);
        }
    }
    catch (System.Reflection.TargetInvocationException wrapped)
    {
        var said = (wrapped.InnerException ?? wrapped).Message;
        foreach (var id in ids)
            if (!sent.Contains(id))
                notConverted.Add(label[id] + ": Revit refused to convert the " + side + " to " + quoted(chosen.Name) + " - " + said);
        if (sent.Contains(ids[0]))
            findings.Add("Revit converted the " + side + " and then refused to report on it - " + said);
    }
    finally
    {
        var disposable = converter as IDisposable;
        if (disposable != null) disposable.Dispose();
    }
};
convert(ductSide, "ductwork");
convert(pipeSide, "pipework");

// ---- read back ----------------------------------------------------------------------------
var partLines = new List<string>();
var gone = 0;
var stayed = new List<string>();
if (sent.Count > 0)
{
    doc.Regenerate();
    foreach (var id in made.Distinct())
    {
        var fp = doc.GetElement(id) as FabricationPart;
        if (fp == null) continue;
        fabricationParts.Add(id);
        partLines.Add("id " + id + " " + quoted(fp.Name) + " on " + quoted(fp.ServiceName)
            + (string.IsNullOrEmpty(fp.Size) ? "" : ", " + fp.Size)
            + (fp.IsAStraight() ? ", straight " + mm(fp.CenterlineLength) + " mm" : ""));
    }
    foreach (var id in sent)
    {
        var after = doc.GetElement(id);
        if (after == null) { gone++; continue; }
        stayed.Add(label[id] + ": still in the model as a design element"
                 + (openEnds.Contains(id) ? ", with an open end" : ""));
    }
    if (fabricationParts.Count == 0)
        foreach (var line in stayed)
            notConverted.Add(line + " - Revit made no fabrication part");
    if (made.Distinct().Count() > fabricationParts.Count)
        findings.Add((made.Distinct().Count() - fabricationParts.Count)
            + " id(s) Revit returned as converted are not fabrication parts after a regenerate");
}

// ---- the answer ---------------------------------------------------------------------------
if (refused.Length > 0)
{
    report = refused;
    findings.Add(refused);
}
else if (given.Count == 0)
    report = "Nothing was handed in - select the design ducts, pipes and fittings to convert. Nothing was converted";
else if (ductSide.Count + pipeSide.Count == 0)
    report = "None of the " + given.Count + " element(s) given is a design duct, pipe, fitting or accessory that can be "
           + "converted - " + string.Join("; ", notDesign.Concat(notConverted).ToArray()) + ". Nothing was converted";
else
    report = "Design to Fabrication with the service " + quoted(chosen.Name) + ": " + sent.Count + " of "
           + (ductSide.Count + pipeSide.Count) + " design element(s) taken by Revit's conversion (" + ductSide.Count
           + " ductwork, " + pipeSide.Count + " pipework handed to it); Revit made " + fabricationParts.Count
           + " fabrication part(s)" + (partLines.Count > 0 ? ": " + string.Join("; ", partLines.ToArray()) : "")
           + ". Revit " + (sent.Count == 0 ? "converted nothing"
                         : partial.Count > 0 ? "reported a partial failure, to be fixed by hand - " + string.Join("; ", partial.ToArray())
                         : "reported the conversion a success")
           + ". Of the design elements Revit took, " + gone + " are no longer in the model (Revit replaced them) and "
           + stayed.Count + " are still there" + (stayed.Count > 0 ? " - " + string.Join("; ", stayed.ToArray()) : "")
           + ". " + notConverted.Count + " not converted"
           + (notConverted.Count > 0 ? " - " + string.Join("; ", notConverted.ToArray()) : "")
           + ". " + notDesign.Count + " left alone as not design ducts or pipes"
           + (notDesign.Count > 0 ? " - " + string.Join("; ", notDesign.ToArray()) : "");
