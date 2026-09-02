// NOT STANDALONE. Assumes `views` and `templateId` are in scope; leaves
// `applied`, `alreadyOnIt` and `refused` behind.
//
// ASSUMES AN OPEN TRANSACTION (Golden Rule 16).
//
// WHAT THIS DOES TO WORK THAT IS ALREADY ON THE VIEW. A template locks every
// setting it controls. Hand-applied graphic overrides are not deleted - they
// are OVERRULED, and become invisible and uneditable, which reads exactly like
// somebody's work having been lost. The grayout job is the case in this
// library: grey a background by category, then apply a template that controls
// V/G, and the grayout is gone with nothing reported by either call.
//
// A TEMPLATE CANNOT BE APPLIED TO A TEMPLATE, and some view kinds refuse
// outright. Each view is set individually so a refusal is recorded and stepped
// over rather than abandoning the rest of the batch.
//
// READ BEFORE AND AFTER. Before, so a view already on that template is not
// counted as changed. After, because assigning the property throws nothing when
// it does not take - the same silent no-op this repository has met on the move
// and parameter paths.

var applied = 0;
var alreadyOnIt = 0;
var refused = new List<ElementId>();

foreach (var view in views)
{
    if (view == null) continue;

    if (view.IsTemplate)
    {
        refused.Add(view.Id);
        continue;
    }

    if (view.ViewTemplateId == templateId)
    {
        alreadyOnIt++;
        continue;
    }

    try
    {
        view.ViewTemplateId = templateId;
    }
    catch (Exception)
    {
        refused.Add(view.Id);
        continue;
    }

    if (view.ViewTemplateId == templateId) applied++;
    else refused.Add(view.Id);
}
