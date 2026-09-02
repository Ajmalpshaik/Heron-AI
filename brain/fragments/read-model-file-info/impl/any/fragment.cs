// NOT STANDALONE. Assumes `paths` is in scope, and leaves `savedFormat`,
// `isCentral`, `owner`, `savedToCentral`, `isNewerThanThisRevit` and
// `unreadable` behind.
//
// READ ONLY, and it does not touch the open model at all - it reads FILES.
//
// THE HEADER, NOT THE MODEL.
//
// Opening a Revit file to answer "what version is this" costs minutes on a real
// project and locks the file while it happens. Revit writes a small header at
// the front of every .rvt, and extracting it comes back in milliseconds. A
// folder of fifty models is a few seconds rather than an afternoon.
//
// WHAT IT ANSWERS THAT AN OPEN MODEL CANNOT.
//
//   isCentral        Opening somebody's LOCAL by mistake - or detaching and
//                    saving over it - is the classic way to break a workshared
//                    job, and the file name never reliably says which it is.
//   owner            Whose copy this is.
//   savedToCentral   FALSE means the model they sent is missing work that only
//                    exists on their machine. Nothing inside the file looks
//                    wrong; it is simply not all there.
//   newer than this  "Why won't this open", answered BEFORE the failed attempt.
//
// THE VERSION VERDICT DEPENDS ON WHICH REVIT IS ASKING, AND THAT IS CORRECT.
// The saved format is a fixed fact about the file. Whether it counts as current
// is a comparison against the Revit running right now, so the same file answers
// differently in 2020 and in 2027. The raw format is returned beside it so the
// verdict can be read rather than believed.
//
// ONE BAD PATH COSTS ONE ROW.
// Each file is guarded on its own. A folder check that stops at the first
// missing or non-Revit file is a folder check nobody can use.

var savedFormat = new Dictionary<string, string>();
var isCentral = new Dictionary<string, bool>();
var owner = new Dictionary<string, string>();
var savedToCentral = new Dictionary<string, bool>();
var isNewerThanThisRevit = new List<string>();
var unreadable = new Dictionary<string, string>();

foreach (var path in paths)
{
    if (string.IsNullOrWhiteSpace(path)) continue;

    BasicFileInfo info = null;
    try { info = BasicFileInfo.Extract(path); }
    catch (Exception ex) { unreadable[path] = ex.Message; continue; }

    if (info == null)
    {
        // Extract can come back null for a file that is not a Revit model at
        // all. Recorded as a reason rather than skipped, so "I did not check
        // that one" never reads as "that one is fine".
        unreadable[path] = "not a Revit file, or the header could not be read";
        continue;
    }

    try { savedFormat[path] = info.Format; } catch { }
    try { isCentral[path] = info.IsCentral; } catch { }
    try { owner[path] = info.Username ?? ""; } catch { }
    try { savedToCentral[path] = info.AllLocalChangesSavedToCentral; } catch { }

    // Compared against the Revit RUNNING NOW, which is why the raw format above
    // is reported too.
    try { if (!info.IsSavedInCurrentVersion) isNewerThanThisRevit.Add(path); }
    catch { }
}
