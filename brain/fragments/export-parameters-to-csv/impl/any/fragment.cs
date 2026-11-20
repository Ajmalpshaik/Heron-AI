// NOT STANDALONE. Assumes `doc`, `elements`, `parameterNames` and `csvPath`
// are in scope; leaves `rows`, `findings` and - since version 2 - `ambiguous`,
// `notCarried` and `blankEverywhere` behind.
//
// PUBLISH, NOT MODIFY. The model is untouched and a file appears where other
// people can open it, so the path is reported.
//
// THE ELEMENT ID IS THE FIRST COLUMN AND IS NOT OPTIONAL. A round trip needs a
// key; without one an edited file cannot be matched back except by a name that
// may not be unique and may be the very thing somebody edited.
//
// VALUES GO OUT THE WAY REVIT DISPLAYS THEM, not the way it stores them - a
// length as millimetres, not decimal feet. That is what makes the file
// editable, and anything reading it back has to use the same convention or the
// numbers change meaning on the way home.
//
// COMMAS, QUOTES AND LINE BREAKS ARE QUOTED PROPERLY. A Comments field with a
// comma silently splits one row into two columns, and every column after it is
// wrong for that element alone - the hardest kind of error to see in a
// spreadsheet.
//
// VERSION 2 (2026-11-20). A NAME TWO PARAMETERS SHARE IS NOT READ
// (FRAGMENT-ISSUES row 5b-203, D-54 s3). LookupParameter hands back "the
// first one encountered" when an element carries two parameters by one name -
// a shared or project parameter bound beside a built-in one, or a curtain
// wall type's doubled mullion settings - and Autodesk's reference says that
// match "is determined at random". So the name is counted with GetParameters
// on the receiver it is then looked up on - the element, and its type where
// the read falls through - and a name carried twice leaves that cell EMPTY,
// the element in `ambiguous` and named in findings as NOT READ. Never a
// marker in the cell: a file brought back would carry it into the model.
//
// AND A COLUMN BLANK ON EVERY ELEMENT IS NOT A NAME THAT DOES NOT EXIST
// (row 5b-1551). Version 1 kept one list - a column left it only when some
// element gave it a value - and called every column on it "a parameter name
// that does not exist on these". A Mark exported in order to be filled in is
// there and blank on all of them. Each column is now counted twice: on how
// many elements the parameter was FOUND (instance or type), and on how many
// it held a value. Found on none is `notCarried`; found and empty on every
// one is `blankEverywhere`, and each is said in its own words.

var rows = 0;
var findings = new List<string>();
var ambiguous = new List<ElementId>();
var notCarried = new List<string>();
var blankEverywhere = new List<string>();

if (elements == null || elements.Count == 0)
{
    findings.Add("No elements were given to export");
}
else if (parameterNames == null || parameterNames.Count == 0)
{
    findings.Add("No parameters were named - say which columns the file should have");
}
else if (string.IsNullOrEmpty(csvPath))
{
    findings.Add("No file path was given - an export has to be told where to write");
}
else
{
    Func<string, string> quoted = value =>
    {
        var v = value == null ? "" : value;
        if (v.IndexOf(',') < 0 && v.IndexOf('"') < 0 && v.IndexOf('\n') < 0) return v;
        return "\"" + v.Replace("\"", "\"\"") + "\"";
    };

    // Names an element carries twice, in the order met - one entry each time
    // a cell is turned down, so a column turned down on every element is told
    // from one carried by none.
    var doubled = new List<string>();

    // NULL when neither the element nor its type carries the name, or when
    // one of them carries it twice (then `doubled` grew); "" when it is there
    // and holds nothing.
    Func<Element, string, string> read = (element, name) =>
    {
        var onInstance = element.GetParameters(name).Count;
        if (onInstance > 1)
        {
            if (!ambiguous.Contains(element.Id)) ambiguous.Add(element.Id);
            doubled.Add(name);
            return null;
        }
        var p = element.LookupParameter(name);
        if (p == null)
        {
            var type = doc.GetElement(element.GetTypeId());
            if (type != null)
            {
                var onType = type.GetParameters(name).Count;
                if (onType > 1)
                {
                    if (!ambiguous.Contains(element.Id)) ambiguous.Add(element.Id);
                    doubled.Add(name);
                    return null;
                }
                p = type.LookupParameter(name);
            }
        }
        if (p == null) return null;
        if (!p.HasValue) return "";

        string v = null;
        try { v = p.AsValueString(); } catch { v = null; }

        if (string.IsNullOrEmpty(v) && p.StorageType == StorageType.String) v = p.AsString();
        if (string.IsNullOrEmpty(v) && p.StorageType == StorageType.Integer) v = p.AsInteger().ToString();

        return v == null ? "" : v;
    };

    try
    {
        var lines = new List<string>();

        var header = new List<string>();
        header.Add("ElementId");
        foreach (var name in parameterNames) header.Add(quoted(name));
        lines.Add(string.Join(",", header.ToArray()));

        // Per column: on how many elements the parameter was found, and on how
        // many it held a value - the two counts version 1 had as one.
        var found = new int[parameterNames.Count];
        var valued = new int[parameterNames.Count];

        foreach (var element in elements)
        {
            if (element == null) continue;

            var cells = new List<string>();
            cells.Add(element.Id.ToString());

            for (var i = 0; i < parameterNames.Count; i++)
            {
                var value = read(element, parameterNames[i]);
                if (value != null)
                {
                    found[i]++;
                    if (value.Length > 0) valued[i]++;
                }
                // Not read and not carried both leave the cell EMPTY.
                cells.Add(quoted(value ?? ""));
            }

            lines.Add(string.Join(",", cells.ToArray()));
            rows++;
        }

        var folder = System.IO.Path.GetDirectoryName(csvPath);
        if (!string.IsNullOrEmpty(folder)) System.IO.Directory.CreateDirectory(folder);
        System.IO.File.WriteAllLines(csvPath, lines.ToArray());

        findings.Add(string.Format("{0} row(s) and {1} parameter column(s) written to '{2}'. The "
            + "element id is the first column, and it is what an edited file has to be matched back "
            + "on", rows, parameterNames.Count, csvPath));

        findings.Add("Values are written as Revit DISPLAYS them - a length as millimetres, not as "
            + "decimal feet - so anything reading this file back has to set them the same way");

        for (var i = 0; i < parameterNames.Count; i++)
        {
            var name = parameterNames[i];
            var twice = doubled.Count(n => n == name);
            if (found[i] == 0 && twice == 0) notCarried.Add(name);
            else if (found[i] > 0 && valued[i] == 0) blankEverywhere.Add(name);
        }

        if (notCarried.Count > 0)
            findings.Add(string.Format("{0} column(s) are on NO element given - neither the element "
                + "nor its type carries a parameter by that name: {1}. Check the spelling against "
                + "the Properties palette; the column is in the file, empty",
                notCarried.Count, string.Join(", ", notCarried.ToArray())));

        if (blankEverywhere.Count > 0)
            findings.Add(string.Format("{0} column(s) are FOUND on the elements and EMPTY on every "
                + "one: {1}. The parameter is there - these are blank values to fill in, not a "
                + "wrong name", blankEverywhere.Count, string.Join(", ", blankEverywhere.ToArray())));

        if (ambiguous.Count > 0)
            findings.Add(string.Format("{0} element(s) carry TWO OR MORE parameters called {1} - "
                + "NOT READ, because Revit hands back whichever of them it meets first; their cell "
                + "is left EMPTY, and which one was meant is yours to say (D-54): {2}",
                ambiguous.Count,
                string.Join(", ", doubled.Distinct().Select(n => "'" + n + "'").ToArray()),
                string.Join(", ", ambiguous.Select(id => id.ToString()).ToArray())));
    }
    catch (Exception ex)
    {
        findings.Add(string.Format("Writing '{0}' failed: {1}. Nothing can be assumed about what is "
            + "in that file now", csvPath, ex.Message));
    }
}
