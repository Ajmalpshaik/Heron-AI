// Heron-Agent:  HERON-INS-PKG-012
// Heron-Step:   18
// Heron-Status: DRAFT
// Heron-Since:  0.1.0
// Heron-Layer:  platform
// See docs/29-metadata-standard.md

using System;

namespace Heron.Installer
{
    /// <summary>What was asked for, and whether Heron will install from it.</summary>
    public sealed class SourceVerdict
    {
        /// <summary>True only for Heron's own published release.</summary>
        public bool Accepted { get; internal set; }

        /// <summary>
        /// Where the assets are, when accepted. Null otherwise.
        ///
        /// BUILT HERE FROM WHAT WAS ACCEPTED, so no caller ever assembles a
        /// URL of its own out of text a user typed.
        /// </summary>
        public string AssetsUrl { get; internal set; }

        /// <summary>The release this names, or null for "the newest".</summary>
        public string Tag { get; internal set; }

        /// <summary>
        /// Why not, in plain English, when refused. Null when accepted.
        ///
        /// IT SAYS WHAT WOULD BE ACCEPTED. A refusal that only says no leaves
        /// somebody guessing, and the guess they make is usually to try
        /// harder rather than to try the right thing.
        /// </summary>
        public string Why { get; internal set; }
    }

    /// <summary>
    /// The gate on route 1 - *"install this"* - and it is a security rule
    /// rather than a convenience.
    ///
    /// R-48: *"install this"* means **Heron's own official installation
    /// link**, the signed release Heron publishes, and **never an arbitrary
    /// repository**. R-49: route 1 **checks the source and refuses** anything
    /// else, in a sentence saying why - **enforced, not expected**, because a
    /// rule nothing enforces is one the first user breaks by accident.
    ///
    /// WHY THIS IS NOT A POLITENESS. docs/07 section 1a refused the shape
    /// *"point an AI at a URL and let it execute whatever it finds there"* in
    /// as many words: it is the exact shape of a supply-chain attack, and it
    /// is the pattern a contractor's IT department is trained to refuse. The
    /// users work at contractors. For a tool that writes to live client
    /// models it is the wrong first precedent, and Golden Rule 19 says no
    /// text Heron reads may raise its own permission level - which a pasted
    /// URL whose contents become instructions does, performed by the user's
    /// own hand.
    ///
    /// AND NOTHING HERE READS A REPOSITORY TO DECIDE WHAT TO INSTALL - R-50.
    /// This judges a STRING. It opens no socket and fetches nothing: the
    /// manifest is read from the release, as data, only after the source has
    /// been accepted.
    ///
    /// WHO HERON IS COMES FROM THE MANIFEST, not from a constant in here -
    /// the `source` block added for Stage 5. A repository that is renamed is
    /// then a line in a file rather than a rebuild, and the same fact serves
    /// the downloader and this gate rather than being written twice.
    /// </summary>
    public static class InstallSource
    {
        /// <summary>The only host a release may come from.</summary>
        private const string GitHub = "github.com";

        /// <summary>
        /// Whether Heron will install from what the user named.
        /// </summary>
        /// <param name="asked">
        /// What they said or pasted. Anything at all - this is the untrusted
        /// input, and treating it as untrusted is the whole job.
        /// </param>
        /// <param name="manifest">Who Heron is, read from its product list.</param>
        public static SourceVerdict Judge(string asked, ProductManifest manifest)
        {
            if (manifest == null || !manifest.KnowsItsSource)
            {
                return No("Heron does not know which repository publishes its own releases, " +
                          "so it cannot tell whether that is one of them. The product list is " +
                          "incomplete - fetch Heron again.");
            }

            var mine = "https://" + GitHub + "/" + manifest.SourceOwner + "/" +
                       manifest.SourceRepo + "/releases";

            if (string.IsNullOrWhiteSpace(asked))
            {
                return No("Nothing was named, so there is nothing to install from. " +
                          "Heron installs from its own release: " + mine);
            }

            Uri url;
            if (!Uri.TryCreate(asked.Trim(), UriKind.Absolute, out url))
            {
                // A LOCAL FOLDER IS NOT A REFUSAL OF THE SAME KIND, and the
                // sentence says so. Somebody who cloned the repository and
                // pointed at it has done something reasonable - it is route 2
                // rather than route 1 - and telling them "that is not Heron's
                // release" without saying which door to use is a dead end.
                return No("'" + Shorten(asked) + "' is not a web address, so Heron cannot " +
                          "tell whose release it is. If those are files already on this PC, " +
                          "install from the folder instead. Otherwise Heron installs from its " +
                          "own release: " + mine);
            }

            // A WINDOWS PATH IS A VALID ABSOLUTE URI, and that is why this
            // check sits above the scheme one rather than below it.
            // Uri.TryCreate turns D:\Heron-AI and \\server\share into
            // file: addresses quite happily, so they reach here parsed rather
            // than rejected - and the first draft of this then told somebody
            // who had pointed at their own clone that "Heron will only fetch
            // over https, and that address is file", which is true and
            // useless. Found by the check that asks for the other door to be
            // named.
            if (url.IsFile)
            {
                return No("'" + Shorten(asked) + "' is a folder on this PC rather than " +
                          "Heron's release. If those are Heron's files already, install " +
                          "from the folder instead of asking for a download. Otherwise " +
                          "Heron installs from its own release: " + mine);
            }

            // HTTPS ONLY. Over http, what arrives is whatever the network
            // decided to send, and the checksum published alongside it came
            // over the same wire.
            if (!string.Equals(url.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                return No("Heron will only fetch over https, and that address is " +
                          url.Scheme + ". Anything else can be changed on the way. " +
                          "Heron installs from its own release: " + mine);
            }

            // A NAME BEFORE AN @ IS NOT THE HOST, and this is the oldest
            // trick in the list: https://github.com/Ajmalpshaik@evil.example/
            // reads as GitHub to a person and resolves to evil.example. Uri
            // parses it correctly, so the refusal is to have any user info at
            // all rather than to try to interpret it.
            if (!string.IsNullOrEmpty(url.UserInfo))
            {
                return No("That address carries a name before the @, which means the part " +
                          "that looks like the site is not the site it would reach. Heron " +
                          "installs from its own release: " + mine);
            }

            // THE HOST, WHOLE AND EXACT. Not "contains", not "ends with" -
            // github.com.evil.example ends with nothing useful and
            // evil-github.com contains it. Compared as one string, ignoring
            // case only, which is all that host names are insensitive to.
            if (!string.Equals(url.Host, GitHub, StringComparison.OrdinalIgnoreCase))
            {
                return No("That address is on " + url.Host + ", and Heron's releases are on " +
                          GitHub + ". Heron will not install software from anywhere else. " +
                          "Its own release is here: " + mine);
            }

            if (!url.IsDefaultPort)
            {
                return No("That address names a port of its own, which GitHub's releases " +
                          "never do. Heron installs from its own release: " + mine);
            }

            // /<owner>/<repo>/releases/...  and the first two must be Heron's.
            var parts = url.AbsolutePath.Trim('/').Split('/');
            if (parts.Length < 3
                || !string.Equals(parts[0], manifest.SourceOwner, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(parts[1], manifest.SourceRepo, StringComparison.OrdinalIgnoreCase))
            {
                return No("That is on " + GitHub + ", but it is not Heron's own repository. " +
                          "Heron will not install software from somebody else's - the files " +
                          "would run inside Revit with your models open. Its own release is " +
                          "here: " + mine);
            }

            // AND IT MUST BE A RELEASE, not the repository. R-48 says the
            // signed release, and a repository is source code somebody could
            // have changed a minute ago - which is the thing docs/07 section
            // 1a refused: "not whatever the default branch happens to say
            // today".
            if (!string.Equals(parts[2], "releases", StringComparison.OrdinalIgnoreCase))
            {
                return No("That points at Heron's repository rather than at a release of it. " +
                          "Heron installs a published, versioned release - source code from a " +
                          "branch is whatever it happens to say today. Use: " + mine);
            }

            // .../releases/tag/v0.1.0  ->  a named release
            // .../releases/latest      ->  the newest
            // .../releases             ->  the newest
            var tag = Tagged(parts);

            return new SourceVerdict
            {
                Accepted = true,
                Tag = tag,
                AssetsUrl = tag == null
                    ? ReleaseDownload.LatestUrlFor(manifest.SourceOwner, manifest.SourceRepo)
                    : ReleaseDownload.UrlFor(manifest.SourceOwner, manifest.SourceRepo, tag),
            };
        }

        /// <summary>
        /// The tag named in a releases path, or null for "the newest".
        ///
        /// ONLY THE TWO SHAPES GITHUB ACTUALLY PUBLISHES. Anything else under
        /// /releases/ is not a release a person can install - /releases/new is
        /// a form, for one - so it falls through to null, which means the
        /// newest published one. That is the conservative answer: a real
        /// release rather than a guess at what the odd path meant.
        /// </summary>
        private static string Tagged(string[] parts)
        {
            if (parts.Length >= 5
                && string.Equals(parts[3], "tag", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(parts[4]))
            {
                return parts[4];
            }

            if (parts.Length >= 5
                && string.Equals(parts[3], "download", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrEmpty(parts[4]))
            {
                return parts[4];
            }

            return null;
        }

        private static SourceVerdict No(string why)
        {
            return new SourceVerdict { Accepted = false, Why = why };
        }

        /// <summary>
        /// Enough of what they typed to recognise, and no more.
        ///
        /// A REFUSAL SHOULD NOT REPEAT A WALL OF TEXT back at somebody, and a
        /// very long string in a message is also how a refusal gets used to
        /// push everything useful off the screen.
        /// </summary>
        private static string Shorten(string text)
        {
            var one = text.Trim().Replace("\r", " ").Replace("\n", " ");
            return one.Length <= 60 ? one : one.Substring(0, 57) + "...";
        }
    }
}
