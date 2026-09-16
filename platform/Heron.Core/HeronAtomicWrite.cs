// Heron-Agent:  none
// Heron-Step:   6
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System.IO;
using System.Text;

namespace Heron.Core
{
    /// <summary>
    /// Write a file so that an interrupted write cannot destroy what was
    /// already there.
    ///
    /// WHY THIS EXISTS, AND IT IS NOT THE HAZARD IT LOOKS LIKE. Two callers
    /// already wrote to a sibling `.tmp` and then moved it over the target,
    /// which reads as careful. Both then did this:
    ///
    ///     if (File.Exists(target)) File.Delete(target);
    ///     File.Move(tmp, target);
    ///
    /// and that is WORSE than a plain overwrite, not better. A plain write
    /// leaves a truncated file if it is interrupted. Delete-then-move leaves
    /// NO FILE AT ALL, and the window is wider because a second syscall sits
    /// inside it.
    ///
    /// WHAT IT COST IN HeronConfig. The config is the file whose own header
    /// says "Owned by you. Never overwritten by an update." Its loader returns
    /// defaults when the file is missing and swallows IOException without a
    /// word, and the next Save() writes those defaults back. So a process that
    /// died between the Delete and the Move did not corrupt the user's
    /// settings - it erased them, and the next save made the erasure
    /// permanent, silently. Every step of that is individually reasonable and
    /// the combination loses data with no message.
    ///
    /// The bridge discovery file had the same two lines under a comment
    /// reading "so a client never reads a half-written file". True, and it did
    /// not cover the case where a client reads NO file and concludes no bridge
    /// is running.
    ///
    /// WHY File.Replace AND NOT File.Move(tmp, target, overwrite: true). The
    /// overwrite argument arrived in .NET Core 3.0 and Heron.Core is built for
    /// net472 and net48 as well, where it does not exist. File.Replace has
    /// been there since .NET Framework 2.0, is a single atomic operation on
    /// NTFS, and needs no conditional compilation.
    ///
    /// IT DELIBERATELY DOES NOT CATCH. A caller that could not save must find
    /// out. Both existing callers already handle their own failures and this
    /// throws exactly what File.WriteAllText would.
    /// </summary>
    public static class HeronAtomicWrite
    {
        /// <summary>
        /// Write <paramref name="contents"/> to <paramref name="path"/>
        /// completely, or leave what was there untouched. Never both, and
        /// never neither.
        ///
        /// UTF-8 without a byte order mark, because every file Heron writes
        /// this way is read back by Python as well as by C#, and a BOM turns
        /// the first key of a config file into one nothing matches.
        /// </summary>
        public static void WriteAllText(string path, string contents)
        {
            var folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // The temporary file is a SIBLING on purpose. File.Replace needs
            // both paths on one volume, and a temp folder is frequently on
            // another drive - which is the same class of mistake as building a
            // path against ROOT and meeting a caller on C: with the repository
            // on D:.
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, contents, new UTF8Encoding(false));

            if (File.Exists(path))
            {
                // Atomic on NTFS: a reader sees the whole old file or the
                // whole new one, never an absence. Passing null for the backup
                // is what asks for no third file to be left behind.
                File.Replace(tmp, path, null);
            }
            else
            {
                // Nothing to replace. A rename onto a free name is itself
                // atomic, so the first write is as safe as every later one.
                File.Move(tmp, path);
            }
        }
    }
}
