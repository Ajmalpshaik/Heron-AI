// NOT STANDALONE. Assumes `doc`, `materialName` and `colour` are in scope;
// leaves `changed`, `wasColour`, `nowColour`, `alreadyThat`, `nearMisses` and
// `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION and does not open one (Golden Rule 16).
//
// IT REPORTS THE OLD COLOUR BECAUSE THAT IS THE EVIDENCE. "Set to 184,115,51"
// is equally true of a material that was already that colour and of one that
// was grey, and those are different facts.
//
// SHADING COLOUR IS NOT THE APPEARANCE ASSET. This sets what a shaded view
// draws - the Graphics tab. The Appearance tab a rendering uses is a separate
// asset this does not touch.
//
// A NAME THAT MATCHES NOTHING RETURNS NEAR MISSES rather than creating
// anything: a material made here would sit beside the one that was meant,
// with nothing pointing at it.

var findings = new List<string>();
var nearMisses = new List<string>();
var changed = 0;
var wasColour = "";
var nowColour = "";
var alreadyThat = false;

var wanted = (materialName ?? "").Trim();

if (wanted.Length == 0)
{
    findings.Add("No material was named.");
}
else if (colour == null)
{
    findings.Add("No colour was given. A colour is three numbers 0 to 255, comma separated.");
}
else
{
    Material material = null;
    var all = new List<Material>();
    try
    {
        foreach (var found in new FilteredElementCollector(doc).OfClass(typeof(Material)).ToElements())
        {
            var candidate = found as Material;
            if (candidate != null) all.Add(candidate);
        }
    }
    catch (Exception) { }

    foreach (var candidate in all)
    {
        if (candidate.Name != null
            && string.Equals(candidate.Name, wanted, StringComparison.OrdinalIgnoreCase))
        {
            material = candidate;
            break;
        }
    }

    if (material == null)
    {
        foreach (var candidate in all)
        {
            if (candidate.Name == null) continue;
            if (candidate.Name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0)
                nearMisses.Add(candidate.Name);
        }
        findings.Add(string.Format("No material is called '{0}'. {1}", wanted,
            nearMisses.Count == 0 ? "Nothing came close either."
                : string.Format("{0} name(s) came close and are listed.", nearMisses.Count)));
    }
    else
    {
        var before = material.Color;
        wasColour = before == null ? "<none>"
            : string.Format("{0},{1},{2}", before.Red, before.Green, before.Blue);

        alreadyThat = before != null
            && before.Red == colour.Red
            && before.Green == colour.Green
            && before.Blue == colour.Blue;

        if (alreadyThat)
        {
            nowColour = wasColour;
            findings.Add(string.Format("'{0}' is already {1} - nothing was changed.",
                material.Name, wasColour));
        }
        else
        {
            try
            {
                material.Color = colour;
                changed = 1;
            }
            catch (Exception error)
            {
                findings.Add(string.Format("'{0}' refused the colour: {1}", material.Name,
                    error.Message));
            }

            // READ BACK. A property that took the assignment without complaint
            // and stored something else is the failure worth catching.
            var after = material.Color;
            nowColour = after == null ? "<none>"
                : string.Format("{0},{1},{2}", after.Red, after.Green, after.Blue);

            if (changed == 1)
            {
                findings.Add(string.Format("'{0}': {1} -> {2}", material.Name, wasColour,
                    nowColour));
                findings.Add("This is the SHADING colour, which a shaded or consistent-colours "
                    + "view draws. The Appearance asset a rendering uses is separate and was "
                    + "not touched.");
            }
        }
    }
}

findings.Insert(0, string.Format("{0} material(s) recoloured; was {1}, now {2}",
    changed, wasColour.Length == 0 ? "<not read>" : wasColour,
    nowColour.Length == 0 ? "<not read>" : nowColour));
