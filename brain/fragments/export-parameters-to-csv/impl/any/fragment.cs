// NOT STANDALONE. Assumes `doc`, `elements`, `parameterNames` and `csvPath`
// are in scope; leaves `rows` and `findings` behind.
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

var rows = 0;
var findings = new List<string>();

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

    Func<Element, string, string> read = (element, name) =>
    {
        var p = element.LookupParameter(name);
        if (p == null)
        {
            var type = doc.GetElement(element.GetTypeId());
            if (type != null) p = type.LookupParameter(name);
        }
        if (p == null || !p.HasValue) return "";

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

        var missingEverywhere = new List<string>();
        foreach (var name in parameterNames) missingEverywhere.Add(name);

        foreach (var element in elements)
        {
            if (element == null) continue;

            var cells = new List<string>();
            cells.Add(element.Id.ToString());

            for (var i = 0; i < parameterNames.Count; i++)
            {
                var value = read(element, parameterNames[i]);
                if (value.Length > 0) missingEverywhere.Remove(parameterNames[i]);
                cells.Add(quoted(value));
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

        if (missingEverywhere.Count > 0)
            findings.Add(string.Format("{0} column(s) are empty for EVERY element: {1}. That is a "
                + "parameter name that does not exist on these, not a set of blank values",
                missingEverywhere.Count, string.Join(", ", missingEverywhere.ToArray())));
    }
    catch (Exception ex)
    {
        findings.Add(string.Format("Writing '{0}' failed: {1}. Nothing can be assumed about what is "
            + "in that file now", csvPath, ex.Message));
    }
}
