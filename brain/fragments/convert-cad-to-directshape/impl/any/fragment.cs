// NOT STANDALONE. Assumes `doc`, `elements` and `category` are in scope, and
// leaves `created`, `skippedSolids`, `notAnImport` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16). One import, one undo.
//
// AN IMPORT'S SOLIDS ARE INSIDE A GEOMETRY INSTANCE.
//
// Reading the top level of an import's geometry finds no solids at all - just
// the instance that holds them. The walk below goes through it, and a version
// that stopped at the top would report a clean run over a file it never
// opened.
//
// ONLY REAL SOLIDS BECOME ELEMENTS.
//
// A CAD file carries surfaces, lines and degenerate shells beside its solids.
// The ones with no volume are counted rather than converted: five elements
// from a hundred pieces means the file was exported as surfaces, which is a
// conversation with whoever sent it - and a number is what starts that
// conversation.
//
// NOTHING IS DELETED.
//
// The import stays. Converting is additive, and which of the two overlapping
// sets to keep is not a decision to make inside a conversion.

var created = new List<ElementId>();
int skippedSolids = 0;
var notAnImport = new List<ElementId>();
string refused = "";

var categoryId = new ElementId(category);

// Asked once. A category that will not take one of these fails every solid
// identically, and finding that out a hundred times is a hundred identical
// rows in a report.
bool categoryTakesIt = false;
try { categoryTakesIt = DirectShape.IsValidCategoryId(categoryId, doc); } catch { }

if (!categoryTakesIt)
{
    refused = "Category " + category.ToString() + " will not hold a direct shape. Every solid would " +
              "fail the same way, so nothing was attempted.";
}
else
{
    var geometryOptions = new Options();
    geometryOptions.ComputeReferences = false;
    geometryOptions.IncludeNonVisibleObjects = false;
    geometryOptions.DetailLevel = ViewDetailLevel.Fine;

    foreach (var element in elements)
    {
        var import = element as ImportInstance;
        if (import == null)
        {
            if (element != null) notAnImport.Add(element.Id);
            continue;
        }

        var solids = new List<Solid>();
        GeometryElement geometry = null;
        try { geometry = import.get_Geometry(geometryOptions); } catch { }

        if (geometry != null)
        {
            var pending = new Stack<GeometryElement>();
            pending.Push(geometry);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                foreach (GeometryObject item in current)
                {
                    var solid = item as Solid;
                    if (solid != null)
                    {
                        bool real = false;
                        try { real = solid.Faces.Size > 0 && solid.Volume > 0; } catch { }
                        if (real) solids.Add(solid); else skippedSolids++;
                        continue;
                    }
                    var nested = item as GeometryInstance;
                    if (nested != null)
                    {
                        try { pending.Push(nested.GetInstanceGeometry()); } catch { }
                    }
                }
            }
        }

        foreach (var solid in solids)
        {
            DirectShape shape = null;
            try
            {
                shape = DirectShape.CreateElement(doc, categoryId);
                var geometryObjects = new List<GeometryObject>();
                geometryObjects.Add(solid);
                shape.SetShape(geometryObjects);
            }
            catch { skippedSolids++; continue; }

            if (shape != null) created.Add(shape.Id);
        }
    }
}
