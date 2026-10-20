// NOT STANDALONE. Assumes `doc`, `elements`, `view`, `unobscured` and `solid`
// are in scope; leaves `changed`, `alreadyAsAsked`, `skipped`, `report`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// REVIT'S "VIEW STATE" FOR REINFORCEMENT, IN ONE VIEW: drawn on top of the
// concrete (unobscured) and, in a 3D view, drawn as solid bars. Each is set on
// the bar, for that view only. A concrete host handed in stands for every bar
// in it, so "show the rebar in these columns" needs no second selection.
//
// SOLID IS GONE FROM REVIT 2024. SetSolidInView exists 2020-2023 and is
// obsolete at 2023: from 2024 Revit draws every bar solid in a 3D view at Fine
// detail, and the answer says so rather than pretending to set it.

var changed = new List<ElementId>();
var alreadyAsAsked = 0;
var skipped = new List<string>();
var findings = new List<string>();
var refused = "";
var lines = new List<string>();

// "yes" / "no" / "keep" (left out). Null for keep; refused for anything else.
Func<string, string, bool?> yesNo = (text, what) =>
{
    var t = (text ?? "").Trim().ToLowerInvariant();
    if (t.Length == 0 || t == "keep") return null;
    if (t == "yes" || t == "true" || t == "on") return true;
    if (t == "no" || t == "false" || t == "off") return false;
    refused = "'" + text + "' is not yes or no for " + what + ". Nothing was changed";
    return null;
};
var wantUnobscured = yesNo(unobscured, "unobscured");
bool? wantSolid = null;
if (refused.Length == 0) wantSolid = yesNo(solid, "solid");
if (refused.Length == 0 && wantUnobscured == null && wantSolid == null)
    refused = "Neither unobscured nor solid was asked. Say yes or no for at least one. Nothing was changed";
if (refused.Length == 0 && view == null)
    refused = "No view was named. Nothing was changed";
if (refused.Length == 0 && view.IsTemplate)
    refused = "'" + view.Name + "' is a view template; the view state is set in a view. Nothing was changed";
var view3D = view as View3D;
if (refused.Length == 0 && wantSolid != null)
{
#if REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023
    if (view3D == null)
        refused = "Solid is a 3D view's setting, and '" + view.Name + "' is not a 3D view. Nothing was changed";
#else
    findings.Add("This Revit draws rebar solid in a 3D view whenever the view's Detail Level is Fine - there is no "
        + "separate solid setting since Revit 2024, so it was not set. Set the 3D view's Detail Level to Fine.");
    wantSolid = null;
    if (wantUnobscured == null)
        refused = "Only solid was asked, and this Revit has no separate solid setting. Nothing was changed";
#endif
}

// Every reinforcement element to set: those handed in, and the bars in any
// concrete host handed in.
var targets = new List<Element>();
var seen = new HashSet<ElementId>();
Action<Element> add = e => { if (e != null && seen.Add(e.Id)) targets.Add(e); };
if (refused.Length == 0)
{
    foreach (var element in elements)
    {
        if (element == null) continue;
        if (element is Rebar || element is AreaReinforcement || element is PathReinforcement || element is RebarInSystem || element is FabricSheet)
        {
            add(element);
            continue;
        }
        if (RebarHostData.IsValidHost(element))
        {
            var data = RebarHostData.GetRebarHostData(element);
            var before = targets.Count;
            foreach (var r in data.GetRebarsInHost()) add(r);
            foreach (var a in data.GetAreaReinforcementsInHost()) add(a);
            foreach (var p in data.GetPathReinforcementsInHost()) add(p);
            foreach (var f in data.GetFabricSheetsInHost()) add(f);
            if (targets.Count == before) skipped.Add("'" + element.Name + "' (id " + element.Id + ") holds no reinforcement");
            continue;
        }
        skipped.Add("'" + element.Name + "' (id " + element.Id + ") is neither reinforcement nor a rebar host");
    }
}

// The setting on one element, read and written by its own class.
Func<Element, bool?> readUnobscured = e =>
{
    if (e is Rebar) return ((Rebar)e).IsUnobscuredInView(view);
    if (e is AreaReinforcement) return ((AreaReinforcement)e).IsUnobscuredInView(view);
    if (e is PathReinforcement) return ((PathReinforcement)e).IsUnobscuredInView(view);
    if (e is RebarInSystem) return ((RebarInSystem)e).IsUnobscuredInView(view);
#if !REVIT2020
    if (e is FabricSheet) return ((FabricSheet)e).IsUnobscuredInView(view);
#endif
    return null;
};
Action<Element, bool> writeUnobscured = (e, on) =>
{
    if (e is Rebar) ((Rebar)e).SetUnobscuredInView(view, on);
    else if (e is AreaReinforcement) ((AreaReinforcement)e).SetUnobscuredInView(view, on);
    else if (e is PathReinforcement) ((PathReinforcement)e).SetUnobscuredInView(view, on);
    else if (e is RebarInSystem) ((RebarInSystem)e).SetUnobscuredInView(view, on);
#if !REVIT2020
    else if (e is FabricSheet) ((FabricSheet)e).SetUnobscuredInView(view, on);
#endif
};
#if REVIT2020 || REVIT2021 || REVIT2022 || REVIT2023
Func<Element, bool?> readSolid = e =>
{
    if (view3D == null) return null;
    if (e is Rebar) return ((Rebar)e).IsSolidInView(view3D);
    if (e is AreaReinforcement) return ((AreaReinforcement)e).IsSolidInView(view3D);
    if (e is PathReinforcement) return ((PathReinforcement)e).IsSolidInView(view3D);
    return null;
};
Action<Element, bool> writeSolid = (e, on) =>
{
    if (e is Rebar) ((Rebar)e).SetSolidInView(view3D, on);
    else if (e is AreaReinforcement) ((AreaReinforcement)e).SetSolidInView(view3D, on);
    else if (e is PathReinforcement) ((PathReinforcement)e).SetSolidInView(view3D, on);
};
#else
Func<Element, bool?> readSolid = e => null;
Action<Element, bool> writeSolid = (e, on) => { };
#endif

if (refused.Length == 0)
{
    foreach (var target in targets)
    {
        var did = false;
        var what = target.GetType().Name + " " + target.Id;
        try
        {
            if (wantUnobscured != null)
            {
                var now = readUnobscured(target);
                if (now == null) skipped.Add(what + ": this Revit has no unobscured setting for it");
                else if (now.Value == wantUnobscured.Value) alreadyAsAsked++;
                else { writeUnobscured(target, wantUnobscured.Value); did = true; }
            }
            if (wantSolid != null)
            {
                var now = readSolid(target);
                if (now == null) skipped.Add(what + ": it has no solid setting");
                else if (now.Value == wantSolid.Value) alreadyAsAsked++;
                else { writeSolid(target, wantSolid.Value); did = true; }
            }
        }
        catch (Exception ex)
        {
            skipped.Add(what + ": Revit refused - " + ex.Message);
            continue;
        }
        if (!did) continue;

        // READ BACK - only a setting that now reads as asked counts.
        var right = true;
        if (wantUnobscured != null && readUnobscured(target) != wantUnobscured) right = false;
        if (wantSolid != null && readSolid(target) != null && readSolid(target) != wantSolid) right = false;
        if (right) changed.Add(target.Id);
        else skipped.Add(what + ": reads back other than asked");
    }
    lines.Add("'" + view.Name + "': " + targets.Count + " reinforcement element(s) looked at; " + changed.Count + " changed"
        + (wantUnobscured == null ? "" : ", unobscured " + (wantUnobscured.Value ? "on" : "off"))
        + (wantSolid == null ? "" : ", solid " + (wantSolid.Value ? "on" : "off")) + "; " + alreadyAsAsked + " setting(s) already as asked");
}

var report = string.Join(" | ", lines);
if (refused.Length > 0) findings.Add(refused + ".");
else
{
    findings.Add(report + ".");
    findings.AddRange(skipped);
}
