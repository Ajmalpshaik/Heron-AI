// NOT STANDALONE. Assumes `doc` is in scope; leaves `findings`, `optionCount`
// and `activeOption` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// A DESIGN OPTION CHANGES WHAT "ALL THE DUCTS" MEANS. Elements inside a
// non-active option are excluded from most views and from schedules. A count
// that disagrees with the screen, a short schedule, an element that will not
// delete - same cause, and none of them says so.
//
// THE ACTIVE OPTION CANNOT BE CHANGED FROM CODE ON ANY RELEASE HERE. There is a
// getter and no setter, and Element.DesignOption is read-only too - checked
// against the reference assemblies at both ends. The Revit interface is the only
// route, and saying that is more useful than leaving it to be discovered.

var findings = new List<string>();
var activeOption = "Main Model";

var options = new List<Element>();
foreach (var element in new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_DesignOptions).WhereElementIsNotElementType())
{
    options.Add(element);
}

var optionCount = options.Count;

var activeId = DesignOption.GetActiveDesignOptionId(doc);
if (activeId != ElementId.InvalidElementId)
{
    var active = doc.GetElement(activeId);
    if (active != null) activeOption = active.Name;
}

if (optionCount == 0)
{
    findings.Add("this project has NO design options. Everything is in the Main Model, so nothing is "
        + "being hidden from a view or a schedule by one");
}
else
{
    // How much lives in each option. Counted in one pass rather than one query
    // per option.
    var countByOption = new Dictionary<ElementId, int>();
    foreach (var element in new FilteredElementCollector(doc).WhereElementIsNotElementType())
    {
        DesignOption option = null;
        try { option = element.DesignOption; }
        catch { continue; }
        if (option == null) continue;
        countByOption[option.Id] = (countByOption.ContainsKey(option.Id) ? countByOption[option.Id] : 0) + 1;
    }

    foreach (var element in options)
    {
        var option = element as DesignOption;
        var primary = false;
        try { if (option != null) primary = option.IsPrimary; }
        catch { }

        var setName = "(unknown set)";
        var setParameter = element.get_Parameter(BuiltInParameter.OPTION_SET_ID);
        if (setParameter != null)
        {
            var setElement = doc.GetElement(setParameter.AsElementId());
            if (setElement != null) setName = setElement.Name;
        }

        var held = countByOption.ContainsKey(element.Id) ? countByOption[element.Id] : 0;
        var isActive = element.Id == activeId;

        findings.Add(string.Format("{0} / {1}{2}{3} - {4} element(s) inside{5}",
            setName, element.Name,
            primary ? " [primary]" : "",
            isActive ? " [ACTIVE]" : "",
            held,
            (!isActive && held > 0)
                ? ". Those are hidden from most views and from every schedule while this option is not active"
                : ""));
    }

    findings.Insert(0, string.Format("{0} design option(s). Currently editing: {1}. Elements in a "
        + "NON-active option are excluded from most views and schedules - that is the usual reason a "
        + "count disagrees with the screen", optionCount, activeOption));

    findings.Add("The active option CANNOT be changed from code on any release here, and an element "
        + "cannot be moved into an option from here either - both are read-only in the API. Use the "
        + "status bar in Revit, or Manage then Design Options");
}
