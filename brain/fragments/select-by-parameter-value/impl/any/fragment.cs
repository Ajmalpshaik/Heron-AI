// NOT STANDALONE. Assumes `doc`, `parameterName`, `matchText`, `matchMode`,
// `includeTypeParameters` and `categories` are in scope; leaves `elements`,
// `withoutParameter` and `findings` behind.
//
// A MISSING PARAMETER IS NOT AN EMPTY STRING, AND THE DIFFERENCE ONLY SHOWS UP
// ON A NEGATIVE QUERY. Read a missing parameter as "" and "Mark does not
// contain ABC" matches every element in the model that has no Mark - thousands
// of them, all reported as hits. So an element that does not carry the
// parameter is COUNTED in withoutParameter and never matched, whatever the
// mode. The same count is what turns a mistyped parameter name into
// "0 matched, 4,812 do not carry it" instead of a plausible zero.
//
// THIS MATCHES TEXT. A double only becomes "500" after Revit formats it in the
// project's display units, so searching for a number here is a coin toss -
// SELECT_BY_NUMERIC_PARAMETER compares the stored value instead.
//
// FAMILY, TYPE AND "FAMILY AND TYPE" ARE UNDERSTOOD AS NAMES because that is
// what a modeller says. "Family and Type" is matched as `Family : Type`, the
// way Revit's own properties palette writes it.

var elements = new List<Element>();
var withoutParameter = 0;
var findings = new List<string>();

var wantName = string.IsNullOrEmpty(parameterName) ? "" : parameterName.Trim();
var needle = matchText == null ? "" : matchText;
var mode = string.IsNullOrEmpty(matchMode) ? "contains" : matchMode.Trim().ToLowerInvariant();

if (wantName.Length == 0)
{
    findings.Add("No parameter was named - say which parameter to match, or Family, Type or "
        + "'Family and Type'");
}
else
{
    Func<Element, ElementType> typeOf = element =>
    {
        var instance = element as FamilyInstance;
        if (instance != null && instance.Symbol != null) return instance.Symbol;
        return doc.GetElement(element.GetTypeId()) as ElementType;
    };

    Func<ElementType, string> familyNameOf = type =>
    {
        if (type == null) return null;
        var name = type.FamilyName;
        if (!string.IsNullOrEmpty(name)) return name;

        var p = type.get_Parameter(BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM)
             ?? type.get_Parameter(BuiltInParameter.ALL_MODEL_FAMILY_NAME);
        return p == null ? null : p.AsString();
    };

    Func<Parameter, string> textOf = p =>
    {
        if (p == null || !p.HasValue) return null;

        if (p.StorageType == StorageType.String) return p.AsString() ?? p.AsValueString();
        if (p.StorageType == StorageType.Integer) return p.AsValueString() ?? p.AsInteger().ToString();
        if (p.StorageType == StorageType.Double) return p.AsValueString();
        if (p.StorageType == StorageType.ElementId)
        {
            var id = p.AsElementId();
            if (id == ElementId.InvalidElementId) return null;
            var referenced = doc.GetElement(id);
            // BY NAME, never by the id as a number.
            return referenced == null ? null : referenced.Name;
        }
        return null;
    };

    // null means "this element does not carry that parameter at all", which is
    // a different answer from "it carries it and it is empty".
    Func<Element, string> valueOf = element =>
    {
        if (string.Equals(wantName, "Family and Type", StringComparison.OrdinalIgnoreCase))
        {
            var type = typeOf(element);
            if (type == null) return null;
            var family = familyNameOf(type);
            return string.IsNullOrEmpty(family) ? type.Name : family + " : " + type.Name;
        }

        if (string.Equals(wantName, "Family", StringComparison.OrdinalIgnoreCase))
            return familyNameOf(typeOf(element));

        if (string.Equals(wantName, "Type", StringComparison.OrdinalIgnoreCase))
        {
            var type = typeOf(element);
            return type == null ? null : type.Name;
        }

        var p = element.LookupParameter(wantName);
        if (p == null && includeTypeParameters)
        {
            var type = typeOf(element);
            if (type != null) p = type.LookupParameter(wantName);
        }
        if (p == null) return null;

        return textOf(p) ?? "";   // present but empty is "", not null
    };

    Func<string, bool> matchesNeedle = value =>
    {
        if (mode == "equals") return string.Equals(value, needle, StringComparison.OrdinalIgnoreCase);
        if (mode == "begins") return value.StartsWith(needle, StringComparison.OrdinalIgnoreCase);
        if (mode == "ends") return value.EndsWith(needle, StringComparison.OrdinalIgnoreCase);
        if (mode == "notcontains") return value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0;
        if (mode == "notequals") return !string.Equals(value, needle, StringComparison.OrdinalIgnoreCase);
        return value.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    };

    var collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
    if (categories != null && categories.Count > 0)
        collector = collector.WherePasses(new ElementMulticategoryFilter(categories));

    var scanned = 0;
    foreach (var element in collector)
    {
        scanned++;
        string value = null;
        try { value = valueOf(element); }
        catch { value = null; }

        if (value == null)
        {
            withoutParameter++;
            continue;
        }

        if (matchesNeedle(value)) elements.Add(element);
    }

    findings.Add(string.Format("{0} of {1} scanned element(s) match {2} {3} '{4}'. {5} do not carry "
        + "'{2}' at all and were never tested",
        elements.Count, scanned, wantName, mode, needle, withoutParameter));

    if (withoutParameter == scanned && scanned > 0)
        findings.Add(string.Format("NOTHING carries '{0}'. That is a parameter name that does not "
            + "exist on these elements, not a model with no matches - check the spelling, and whether "
            + "it is a type parameter", wantName));

    if (mode == "notcontains" || mode == "notequals")
        findings.Add("This is a NEGATIVE match, so elements that do not carry the parameter are "
            + "excluded rather than counted as matches - they are in the untested figure above");
}
