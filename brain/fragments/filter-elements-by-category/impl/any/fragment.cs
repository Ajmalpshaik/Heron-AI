// NOT STANDALONE. Assumes `doc` and `category` are already in scope, and
// leaves `elements` in scope for whatever is composed after it. The wrapper
// supplies `doc`; `category` comes from the request.

var elements = new FilteredElementCollector(doc)
    .OfCategory(category)
    .WhereElementIsNotElementType()
    .ToElements();
