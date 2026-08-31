// NOT STANDALONE. Assumes `doc` is in scope, and leaves `references`, `kinds`,
// `savedPaths`, `resolvedPaths`, `unresolved` and `unloaded` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// NOT A LIST OF REVIT LINKS.
//
// Collecting RevitLinkInstance is the obvious implementation, and it reports a
// model whose real risk is three unresolved DWGs and a point cloud as CLEAN.
// Revit keeps ONE list covering links, imports, point clouds, keynote tables
// and IFC, and that is what is read here - so a handover checklist built on it
// cannot be short by a whole category nobody thought to ask about.
//
// TWO PATHS, ANSWERING DIFFERENT QUESTIONS.
//
//   saved     what is written in the file. May be relative.
//   resolved  what it currently points at on THIS machine.
//
// After a move the saved path is usually still right and the resolved one has
// gone stale. Report one and the wrong thing gets named as broken.
//
// A ModelPath IS NOT A STRING. It goes through
// ConvertModelPathToUserVisiblePath before anyone can read it, and that call is
// guarded per reference: a cloud-hosted path has no user-visible form, and an
// unguarded throw would lose the entire list rather than one row.
//
// NOT LOADED IS NOT BROKEN, AND THERE ARE THREE WAYS TO BE NOT LOADED.
//
// LinkedFileStatus carries seven values and the split is not the obvious
// two-way one. THREE of them mean "deliberately not here":
//
//   Unloaded         somebody unloaded it for everyone
//   LocallyUnloaded  somebody unloaded it for THEMSELVES only
//   InClosedWorkset  its workset is closed in this session
//
// and two are faults:
//
//   NotFound         the file is not where the model expects it
//   Invalid          the reference itself is broken
//
// CanBeUpgraded is loaded, from an older Revit - not a fault either.
//
// Treating anything that is not Loaded as broken puts three false alarms on a
// checklist, and a checklist with false alarms stops being read - the same
// outcome as not writing one. The enum was read from the shipped assembly
// rather than assumed: a first attempt here guessed at a `NotLoaded` member
// that does not exist, and the compile gate refused it on all eight releases.

var references = new List<Element>();
var kinds = new Dictionary<ElementId, string>();
var savedPaths = new Dictionary<ElementId, string>();
var resolvedPaths = new Dictionary<ElementId, string>();
var unresolved = new List<ElementId>();
var unloaded = new List<ElementId>();

Func<ModelPath, string> readable = path =>
{
    if (path == null) return "";
    try { return ModelPathUtils.ConvertModelPathToUserVisiblePath(path); }
    catch { return ""; }
};

ICollection<ElementId> found = null;
try { found = ExternalFileUtils.GetAllExternalFileReferences(doc); } catch { }

if (found != null)
{
    foreach (var id in found)
    {
        var element = doc.GetElement(id);
        if (element == null) continue;

        ExternalFileReference reference = null;
        try { reference = ExternalFileUtils.GetExternalFileReference(doc, id); } catch { }
        if (reference == null) continue;

        references.Add(element);

        try { kinds[id] = reference.ExternalFileReferenceType.ToString(); } catch { }
        try { savedPaths[id] = readable(reference.GetPath()); } catch { }
        try { resolvedPaths[id] = readable(reference.GetAbsolutePath()); } catch { }

        LinkedFileStatus status = LinkedFileStatus.Invalid;
        bool statusKnown = false;
        try { status = reference.GetLinkedFileStatus(); statusKnown = true; } catch { }
        if (!statusKnown) continue;

        // Unloaded is a choice. NotFound and Invalid are faults. They go to
        // different lists because they go to different people.
        if (status == LinkedFileStatus.Unloaded
            || status == LinkedFileStatus.LocallyUnloaded
            || status == LinkedFileStatus.InClosedWorkset)
            unloaded.Add(id);
        else if (status == LinkedFileStatus.NotFound || status == LinkedFileStatus.Invalid)
            unresolved.Add(id);
    }
}
