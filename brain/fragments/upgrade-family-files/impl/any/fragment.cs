// NOT STANDALONE. Assumes `app`, `sourceFiles`, `destinationFolder`,
// `allowProjectFiles` and `maxFiles` are in scope; leaves `upgraded`, `failed`,
// `skipped` and `findings` behind.
//
// NO TRANSACTION, AND IT MUST NOT BE RUN INSIDE ONE. It does not touch the open
// model. It opens and closes other documents, which a transaction on this one
// has no business wrapping.
//
// A FILE OPENED THROUGH THE APPLICATION HAS NO WINDOW, and that is why this
// works at all. Revit refuses to let an add-in save the document somebody is
// looking at; a background document saves normally.
//
// IT ALWAYS WRITES SOMEWHERE ELSE. An upgrade is one-way - once saved by a
// newer Revit the older one can never open it again - so overwriting the
// originals destroys the only copy that still works for anyone left behind.
//
// A WORKSHARED PROJECT IS DETACHED FIRST. Opened without saying otherwise, a
// central file opens AS the central, and upgrading and saving from there
// damages a live job. But asking to detach a file that is NOT workshared is
// itself refused, so the file is asked which it is before the option is set -
// and a project file that will not answer is skipped rather than opened
// hopefully.
//
// WORKSETS ARE OPENED, NOT CLOSED. The opposite of the usual advice, and for a
// reason: an upgrade rewrites every element, so a closed workset leaves its
// content unconverted and the file reports as upgraded while part of it is not.

var upgraded = new Dictionary<string, string>();
var failed = new List<string>();
var skipped = new List<string>();
var findings = new List<string>();

if (string.IsNullOrEmpty(destinationFolder) || !System.IO.Directory.Exists(destinationFolder))
{
    findings.Add("The destination folder does not exist. Nothing was opened. It is not created "
        + "here on purpose: an upgrade that invents its own output folder is an upgrade nobody "
        + "can find afterwards.");
}
else
{
    // Files already open in this Revit cannot be opened again. Collected once
    // rather than discovered by a throw halfway through the batch.
    var alreadyOpen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (Document open in app.Documents)
    {
        if (open == null) continue;
        if (string.IsNullOrEmpty(open.PathName)) continue;
        alreadyOpen.Add(open.PathName);
    }

    var attempted = 0;

    foreach (var file in sourceFiles)
    {
        if (string.IsNullOrEmpty(file)) continue;

        if (attempted >= maxFiles)
        {
            skipped.Add(file + " - the maxFiles brake was reached.");
            continue;
        }

        if (!System.IO.File.Exists(file))
        {
            skipped.Add(file + " - no such file.");
            continue;
        }

        var extension = System.IO.Path.GetExtension(file).ToLowerInvariant();
        var isProject = extension == ".rvt";

        if (extension != ".rfa" && extension != ".rft" && extension != ".rte" && !isProject)
        {
            skipped.Add(file + " - not a Revit content file.");
            continue;
        }

        if (isProject && !allowProjectFiles)
        {
            skipped.Add(file + " - a project file, and project files are off. A project is a "
                + "decision about a job rather than a maintenance task.");
            continue;
        }

        if (alreadyOpen.Contains(file))
        {
            skipped.Add(file + " - already open in this Revit and cannot be opened twice.");
            continue;
        }

        var target = System.IO.Path.Combine(destinationFolder, System.IO.Path.GetFileName(file));

        if (string.Equals(System.IO.Path.GetFullPath(target), System.IO.Path.GetFullPath(file),
                StringComparison.OrdinalIgnoreCase))
        {
            skipped.Add(file + " - the destination is the source. Upgrading in place is refused: "
                + "the older Revit could never open the file again.");
            continue;
        }

        // Is it workshared? Asked before the open, because the answer decides
        // whether detaching is correct or itself an error.
        var workshared = false;
        var worksharingKnown = false;
        try
        {
            var info = BasicFileInfo.Extract(file);
            if (info != null)
            {
                workshared = info.IsWorkshared;
                worksharingKnown = true;
            }
        }
        catch
        {
            // A family will often not answer this, and that is fine - a family
            // is never workshared. A PROJECT that will not answer is the
            // dangerous case and is handled below.
        }

        if (isProject && !worksharingKnown)
        {
            skipped.Add(file + " - a project file whose worksharing could not be read. Opening it "
                + "without knowing risks opening a live central file as the central.");
            continue;
        }

        attempted++;

        Document opened = null;

        try
        {
            var openOptions = new OpenOptions();
            openOptions.Audit = false;

            if (workshared)
            {
                // The only correct way to open somebody else's model for a
                // batch: the copy on disk is untouched until the save below,
                // and the detached document cannot write back to the central.
                openOptions.DetachFromCentralOption =
                    DetachFromCentralOption.DetachAndPreserveWorksets;

                // A folder of files to upgrade routinely holds other people's
                // locals, and Revit simply refuses those without this.
                openOptions.AllowOpeningLocalByWrongUser = true;

                // Open, not closed. An upgrade rewrites every element, and a
                // closed workset leaves its content unconverted.
                openOptions.SetOpenWorksetsConfiguration(
                    new WorksetConfiguration(WorksetConfigurationOption.OpenAllWorksets));
            }

            opened = app.OpenDocumentFile(
                ModelPathUtils.ConvertUserVisiblePathToModelPath(file), openOptions);

            var saveOptions = new SaveAsOptions();
            saveOptions.OverwriteExistingFile = true;

            // The upgrade rewrites the whole file anyway, so this costs nothing
            // and is the difference between a library that grows every release
            // and one that does not.
            saveOptions.Compact = true;

            // THE THUMBNAIL. A library is browsed by preview image in Revit's
            // own dialog, and a blank one makes it unnavigable. Keep whatever
            // the file already nominated; else a 3D view; else a plan. Each
            // candidate is put to Revit's own test first, because a view that
            // cannot be a preview makes the save throw.
            try
            {
                var previewSettings = opened.GetDocumentPreviewSettings();
                if (previewSettings != null)
                {
                    if (previewSettings.PreviewViewId != ElementId.InvalidElementId)
                    {
                        saveOptions.PreviewViewId = previewSettings.PreviewViewId;
                    }
                    else
                    {
                        ElementId chosen = null;

                        foreach (var candidate in new FilteredElementCollector(opened)
                                     .OfClass(typeof(View3D)))
                        {
                            var view = candidate as View;
                            if (view == null || view.IsTemplate) continue;
                            if (!previewSettings.IsViewIdValidForPreview(view.Id)) continue;
                            chosen = view.Id;
                            break;
                        }

                        if (chosen == null)
                        {
                            foreach (var candidate in new FilteredElementCollector(opened)
                                         .OfClass(typeof(ViewPlan)))
                            {
                                var view = candidate as View;
                                if (view == null || view.IsTemplate) continue;
                                if (!previewSettings.IsViewIdValidForPreview(view.Id)) continue;
                                chosen = view.Id;
                                break;
                            }
                        }

                        if (chosen != null) saveOptions.PreviewViewId = chosen;
                    }
                }
            }
            catch
            {
                // No preview is worse than a good one and better than a failed
                // save. The file still upgrades.
            }

            opened.SaveAs(target, saveOptions);
            upgraded[file] = target;
        }
        catch (Exception failure)
        {
            // A family that will not upgrade usually will not for a real reason
            // - a corrupt nested family, a missing type catalogue. The reason is
            // kept against the file and the batch continues.
            failed.Add(file + " - " + failure.Message);
        }
        finally
        {
            // Closed without saving again. A document left open holds the file
            // and the memory, and a library is hundreds of files.
            if (opened != null)
            {
                try { opened.Close(false); } catch { }
            }
        }
    }

    findings.Add("Upgraded " + upgraded.Count + " file(s) into the destination folder, "
        + failed.Count + " failed, " + skipped.Count + " skipped. The originals were not touched: "
        + "an upgrade is one-way, and the old Revit can never open an upgraded file again.");

    if (attempted >= maxFiles)
    {
        findings.Add("The maxFiles brake was reached at " + maxFiles + ". Anything past it is in "
            + "the skipped list, not lost - raise the brake once a small batch has come back "
            + "right and two of the results have been opened by hand.");
    }
}
