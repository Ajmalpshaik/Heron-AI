// NOT STANDALONE. Assumes `app`, `search` and `folder` are in scope; leaves
// `files`, `fileCount`, `foldersSearched` and `findings` behind.
//
// READ ONLY. Opens no transaction, touches no model and reads only folders.
//
// FAMILY FILES ON DISK WHOSE NAME HOLDS EVERY WORD SEARCHED FOR - "gate
// valve", "fcu ceiling" - in the folder given, or, with none given, in every
// library folder Revit is set to (Options > File Locations > Places). The
// search goes through sub-folders; the full path of each match comes back, so
// LOAD_FAMILY can load it next.
//
// It stops after 200 matches or 50,000 files looked at, and says so - a whole
// drive is not a family library.

const int MaxMatches = 200;
const int MaxFiles = 50000;
var files = new List<string>();
var foldersSearched = new List<string>();
var findings = new List<string>();
var fileCount = 0;

var words = (search ?? "").ToLowerInvariant().Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
var roots = new List<string>();
var given = (folder ?? "").Trim().Trim('"');
if (given != "") roots.Add(given);
else
{
    try { foreach (var path in app.GetLibraryPaths().Values) roots.Add(path); } catch { }
}

var stopped = false;
if (words.Length == 0) findings.Add("No word to search for was given, so nothing was searched.");
else
{
    foreach (var root in roots.Distinct())
    {
        if (!System.IO.Directory.Exists(root)) { findings.Add("No folder at " + root + "."); continue; }
        foldersSearched.Add(root);
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0 && !stopped)
        {
            var current = pending.Pop();
            try { foreach (var sub in System.IO.Directory.GetDirectories(current)) pending.Push(sub); } catch { continue; }
            string[] here;
            try { here = System.IO.Directory.GetFiles(current, "*.rfa"); } catch { continue; }
            foreach (var file in here)
            {
                fileCount++;
                var name = System.IO.Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                if (words.All(w => name.Contains(w))) files.Add(file);
                if (files.Count >= MaxMatches || fileCount >= MaxFiles) { stopped = true; break; }
            }
        }
        if (stopped) break;
    }
    findings.Add(files.Count + " family file(s) match \"" + search + "\" among " + fileCount + " looked at in "
        + foldersSearched.Count + " folder(s)" + (stopped ? " - STOPPED at the limit, so there may be more." : "."));
    if (roots.Count == 0) findings.Add("Revit has no library folder set, and no folder was given.");
}
