// NOT STANDALONE. Assumes `elements` is in scope; leaves `described` and
// `noType` behind.
//
// READ ONLY. Opens no transaction and needs none.
//
// THE TYPE IS THE SOURCE, NOT THE INSTANCE NAME. `Element.Name` on an instance
// is sometimes the type name, sometimes a mark, and on a system family neither.
// Reading the ElementType gives an answer that survives somebody renaming
// things, which is the standing rule here: names describe intent.
//
// THE ID IS FORMATTED, NEVER READ AS A NUMBER. `ElementId.IntegerValue` became
// a 64-bit `Value` in 2024 and the old property is deprecated - a fragment that
// reached for the number would compile on 2020 and rot. `ToString()` is stable
// across every release and is the only thing an id is wanted for here.
//
// NO TYPE IS A REPORTABLE ANSWER, NOT A SKIP. A detail line, a group, a view
// has no ElementType. Dropping them would answer about fewer elements than were
// asked about, and a caller comparing counts is the only one who would notice.

var described = new Dictionary<ElementId, string>();
var noType = new List<ElementId>();

foreach (var element in elements)
{
    if (element == null) continue;
    if (described.ContainsKey(element.Id)) continue;

    var category = element.Category != null ? element.Category.Name : "(no category)";

    var typeId = element.GetTypeId();
    if (typeId == null || typeId == ElementId.InvalidElementId)
    {
        noType.Add(element.Id);
        described[element.Id] = string.Format("{0} | (no type) | id {1}",
                                              category, element.Id.ToString());
        continue;
    }

    var type = element.Document.GetElement(typeId) as ElementType;
    if (type == null)
    {
        noType.Add(element.Id);
        described[element.Id] = string.Format("{0} | (no type) | id {1}",
                                              category, element.Id.ToString());
        continue;
    }

    described[element.Id] = string.Format("{0} | {1} : {2} | id {3}",
                                          category, type.FamilyName, type.Name,
                                          element.Id.ToString());
}
