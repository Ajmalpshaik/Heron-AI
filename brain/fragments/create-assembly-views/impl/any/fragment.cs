// NOT STANDALONE. Assumes `doc`, `elements`, `withPartList` and
// `withMaterialTakeoff` are in scope, and leaves `created`,
// `alreadyHadViews`, `notAnAssembly` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// THESE VIEWS BELONG TO THE ASSEMBLY, WHICH IS THE WHOLE POINT.
//
// They show only its members, orient to its own origin rather than to project
// north, and follow it if it moves. A section drawn across an assembly by hand
// is just a section: it shows whatever else is in the way and stays put when
// the assembly is relocated.
//
// AN INCOMPLETE ASSEMBLY REFUSES, AND IT REFUSES ALONE.
//
// An assembly whose type is not settled will not make views. In a set of
// twenty there is usually one left half made, so that is a per-assembly
// refusal rather than the end of the run.
//
// AND IT MUST NOT RUN TWICE.
//
// Making the views again leaves two of everything - on a prefabrication job,
// two drawings claiming to be the same part. An assembly that already owns
// views is named and left alone.

var created = new Dictionary<ElementId, int>();
var alreadyHadViews = new List<ElementId>();
var notAnAssembly = new List<ElementId>();
var refused = new List<ElementId>();

// Which assemblies already own views. Asked once, over the whole document,
// rather than per assembly.
var alreadyOwning = new HashSet<ElementId>();
foreach (var element in new FilteredElementCollector(doc).OfClass(typeof(View)))
{
    var view = element as View;
    if (view == null) continue;
    try
    {
        if (view.AssociatedAssemblyInstanceId != ElementId.InvalidElementId)
            alreadyOwning.Add(view.AssociatedAssemblyInstanceId);
    }
    catch { }
}

foreach (var element in elements)
{
    var assembly = element as AssemblyInstance;
    if (assembly == null)
    {
        if (element != null) notAnAssembly.Add(element.Id);
        continue;
    }

    if (alreadyOwning.Contains(assembly.Id)) { alreadyHadViews.Add(assembly.Id); continue; }

    // An assembly whose type is not settled will not make views at all.
    bool complete = false;
    try { complete = assembly.AssemblyTypeName != null && assembly.AssemblyTypeName.Length > 0; }
    catch { }
    if (!complete) { refused.Add(assembly.Id); continue; }

    int madeHere = 0;

    try
    {
        var orthographic = AssemblyViewUtils.Create3DOrthographic(doc, assembly.Id);
        if (orthographic != null) madeHere++;
    }
    catch { }

    foreach (var orientation in new AssemblyDetailViewOrientation[]
             {
                 AssemblyDetailViewOrientation.ElevationFront,
                 AssemblyDetailViewOrientation.ElevationLeft,
                 AssemblyDetailViewOrientation.ElevationTop
             })
    {
        try
        {
            var section = AssemblyViewUtils.CreateDetailSection(doc, assembly.Id, orientation);
            if (section != null) madeHere++;
        }
        catch { }
    }

    if (withPartList)
    {
        try
        {
            var partList = AssemblyViewUtils.CreatePartList(doc, assembly.Id);
            if (partList != null) madeHere++;
        }
        catch { }
    }

    if (withMaterialTakeoff)
    {
        try
        {
            var takeoff = AssemblyViewUtils.CreateMaterialTakeoff(doc, assembly.Id);
            if (takeoff != null) madeHere++;
        }
        catch { }
    }

    if (madeHere > 0) created[assembly.Id] = madeHere;
    else refused.Add(assembly.Id);
}
