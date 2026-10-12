// NOT STANDALONE. Assumes `doc`, `patternName`, `segmentsMm` and
// `replaceExisting` are in scope; leaves `pattern`, `madeNew`, `segmentsRead`,
// `refused` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One call is one undo entry.
//
// A LINE PATTERN FROM WORDS. `segmentsMm` is the pattern as a person says it,
// in millimetres on paper: "dash 6, space 2, dot, space 2". Revit's rules for
// a pattern are checked before anything is made, because Revit's own refusal
// names none of them:
//
//   it starts with a dash or a dot, and dashes and dots alternate with spaces
//   it ends with a space
//   every dash and space is longer than zero; a dot has no length
//
// AN EXISTING NAME IS REFUSED, not overwritten, unless replaceExisting is
// true - and then the pattern's segments are replaced in place, so every
// line style already using it takes the new look without being touched.
//
// READ BACK: the segments are read off the pattern after it is made and
// printed in the same words, so what was stored is what is seen.

const double MmPerFoot = 304.8;
var findings = new List<string>();
string refused = null;
LinePatternElement pattern = null;
var madeNew = false;
var segmentsRead = "";

var name = (patternName ?? "").Trim();
var segments = new List<LinePatternSegment>();

if (name == "") refused = "No pattern name was given, so NOTHING WAS MADE.";

if (refused == null)
{
    var culture = System.Globalization.CultureInfo.InvariantCulture;
    var parts = (segmentsMm ?? "").Split(',');
    foreach (var raw in parts)
    {
        var words = raw.Trim().ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) continue;
        if (words[0] == "dot")
        {
            segments.Add(new LinePatternSegment(LinePatternSegmentType.Dot, 0));
            continue;
        }
        double length;
        if ((words[0] != "dash" && words[0] != "space") || words.Length != 2
            || !double.TryParse(words[1], System.Globalization.NumberStyles.Float, culture, out length)
            || length <= 0)
        {
            refused = "\"" + raw.Trim() + "\" is not a segment. Write each as \"dash 6\", \"space 2\" or "
                + "\"dot\", lengths in millimetres, separated by commas. NOTHING WAS MADE.";
            break;
        }
        segments.Add(new LinePatternSegment(
            words[0] == "dash" ? LinePatternSegmentType.Dash : LinePatternSegmentType.Space,
            length / MmPerFoot));
    }
}

if (refused == null)
{
    if (segments.Count < 2)
        refused = "A line pattern needs at least a dash or dot and a space. NOTHING WAS MADE.";
    else if (segments[0].Type == LinePatternSegmentType.Space)
        refused = "A line pattern starts with a dash or a dot, not a space. NOTHING WAS MADE.";
    else if (segments[segments.Count - 1].Type != LinePatternSegmentType.Space)
        refused = "A line pattern ends with a space, so it can repeat. NOTHING WAS MADE.";
    else
    {
        for (var i = 1; i < segments.Count; i++)
        {
            var wasSpace = segments[i - 1].Type == LinePatternSegmentType.Space;
            var isSpace = segments[i].Type == LinePatternSegmentType.Space;
            if (wasSpace == isSpace)
            {
                refused = "Dashes and dots must alternate with spaces - segment " + (i + 1)
                    + " follows one of its own kind. NOTHING WAS MADE.";
                break;
            }
        }
    }
}

if (refused == null)
{
    var existing = LinePatternElement.GetLinePatternElementByName(doc, name);
    var linePattern = new LinePattern(name);
    linePattern.SetSegments(segments);
    try
    {
        if (existing != null)
        {
            if (!replaceExisting)
                refused = "A line pattern named \"" + name + "\" already exists, so NOTHING WAS MADE. Say "
                    + "replaceExisting to change its segments in place - every line style using it follows.";
            else
            {
                existing.SetLinePattern(linePattern);
                pattern = existing;
            }
        }
        else
        {
            pattern = LinePatternElement.Create(doc, linePattern);
            madeNew = true;
        }
    }
    catch (Exception ex)
    {
        refused = "Revit refused the pattern: " + ex.Message + ". NOTHING WAS MADE.";
        pattern = null;
    }
}

if (pattern != null)
{
    doc.Regenerate();
    var read = new List<string>();
    foreach (var segment in pattern.GetLinePattern().GetSegments())
    {
        if (segment.Type == LinePatternSegmentType.Dot) read.Add("dot");
        else read.Add((segment.Type == LinePatternSegmentType.Dash ? "dash " : "space ")
            + Math.Round(segment.Length * MmPerFoot, 3).ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
    segmentsRead = string.Join(", ", read);
    findings.Add((madeNew ? "Made" : "Replaced the segments of") + " line pattern \"" + name + "\": "
        + segmentsRead + " (millimetres, read back).");
}
if (refused != null) findings.Add(refused);
