// NOT STANDALONE. Assumes `doc`, `materialNames`, `colours` and `transparencies`
// are in scope; leaves `created`, `alreadyThere` and `findings` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// A NAME ALREADY IN USE IS REPORTED, NEVER DUPLICATED. Revit refuses a second
// material of one name, and a batch that threw on the third of five would leave
// two made, three not, and nothing saying which.
//
// IT SETS THE SHADING COLOUR AND NOTHING ELSE. A real material also has an
// appearance asset, a physical asset, a cut pattern and a surface pattern. None
// of those are touched, so what comes out works for scheduling, colouring and
// takeoffs and is NOT rendering-ready. Better said here than discovered in a
// render.
//
// TRANSPARENCY IS CLAMPED, NOT REFUSED. Revit rejects anything outside 0 to 100,
// and a clamp is what somebody asking for 120 meant.

var created = new List<ElementId>();
var alreadyThere = new List<string>();
var findings = new List<string>();

var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
foreach (var material in new FilteredElementCollector(doc).OfClass(typeof(Material)).Cast<Material>())
    existing.Add(material.Name);

for (var i = 0; i < materialNames.Count; i++)
{
    var wanted = materialNames[i];
    if (string.IsNullOrEmpty(wanted) || wanted.Trim().Length == 0)
    {
        findings.Add("A blank material name was asked for and skipped");
        continue;
    }

    var name = wanted.Trim();

    if (existing.Contains(name)) { alreadyThere.Add(name); continue; }

    try
    {
        var id = Material.Create(doc, name);
        var made = doc.GetElement(id) as Material;

        if (made == null)
        {
            findings.Add(string.Format("'{0}' was created and could not be read back", name));
            continue;
        }

        if (i < colours.Count && colours[i] != null) made.Color = colours[i];

        if (i < transparencies.Count)
        {
            var asked = transparencies[i];
            var clamped = Math.Max(0, Math.Min(100, asked));
            made.Transparency = clamped;
            if (clamped != asked)
                findings.Add(string.Format("'{0}': transparency {1} is outside 0 to 100 and was set to "
                    + "{2}", name, asked, clamped));
        }

        created.Add(id);
        existing.Add(name);
    }
    catch (Exception ex)
    {
        findings.Add(string.Format("'{0}' was refused: {1}", name, ex.Message));
    }
}

findings.Add(string.Format("{0} material(s) created{1}. They carry a SHADING COLOUR only - no "
    + "appearance asset, no cut or surface pattern - so they schedule and colour correctly and are not "
    + "ready for a render",
    created.Count,
    alreadyThere.Count == 0 ? ""
        : string.Format(", and {0} already existed and were left alone: {1}",
              alreadyThere.Count, string.Join(", ", alreadyThere))));
