// NOT STANDALONE. Assumes `doc`, `everySubcategory` and `categories` are in
// scope; leaves `names`, `bindable`, `objectStyles`, `familySubcategories`,
// `styleRows` and `findings`.
//
// A READ. It opens no transaction and needs none.
//
// NOTHING LISTED THESE. SELECT_BY_CATEGORY_NAME returns near misses once you
// have guessed wrong, which is not a list, so "what is even in here" had no
// answer at all.
//
// THE NAMES ARE LOCALISED. On a French or Arabic Revit they come back in that
// language, and that is the point rather than a defect: this is what tells
// somebody which names SELECT_BY_CATEGORY_NAME will accept on THIS machine.
//
// ITERATED, NOT INDEXED. Revit promises no ordering for Categories.
//
// VERSION 2 - OBJECT STYLES (FRAGMENT-ISSUES 5b-464). Every category's own
// graphics as Manage > Object Styles shows them: the projection line colour,
// the projection line weight, the cut line weight where the category is cut,
// and the projection line pattern. Read off the category itself, never off a
// view, so a view's override is not in it - that is REPORT_CATEGORY_OVERRIDES.
//
// ONE STRING, ONE LINE PER ROW. A reply shows a list as its first three items,
// each cut to sixty characters, and a string whole - so `objectStyles` is the
// answer, and `names` stays the list it was for anything that consumes it.
//
// IN A FAMILY, THE FAMILY'S OWN CATEGORY AND EVERY SUBCATEGORY OF IT COME
// FIRST - the rows its own lines and forms sit on, Object Styles in the Family
// Editor. Every other category follows as its own row, sorted by name; their
// subcategories only with everySubcategory, because a project has hundreds and
// a reply would carry every one. Left out, everySubcategory is false
// (HeronBindingNote.AbsentValue), and names, bindable and the first finding
// are exactly version 1's.
//
// AN INTERNAL CATEGORY HAS NO ROW. Object Styles shows model, annotation and
// analytical categories; an internal one is counted and left out.
//
// VERSION 3 - NARROWED BY NAME (FRAGMENT-ISSUES 5b-540). `categories` names
// the rows to report, comma separated: "Generic Annotations" is that
// category's own row and every subcategory of it, "Generic Annotations >
// Signature" is that one row, and a bare name no category has is every
// subcategory of that name. Capitals and extra spaces are forgiven. Left out it
// is "all" (the card's `absent:`), and "all" or nothing typed is version 2's
// answer exactly. Asking for one row printed all 303 before this, twice in one
// chat, to check one colour.
//
// A NAME THE DOCUMENT HAS NOT GOT IS ANSWERED WITH THE NEAREST NAMES, NEVER
// WITH AN EMPTY TABLE. A column line over no rows reads as "nothing there";
// what is true is "not by that name". When nothing asked matched, there is no
// column line at all - only what was asked and the names nearest to it.
//
// A comma splits the names, so a subcategory whose own name holds a comma
// cannot be asked for by name; leave `categories` out to see it.

var names = new List<string>();
var bindable = 0;
var findings = new List<string>();
var objectStyles = "";
var familySubcategories = 0;
var styleRows = 0;

var askedText = (categories ?? "").Trim();
var narrowed = askedText.Replace(",", "").Trim().Length > 0
    && !string.Equals(askedText, "all", StringComparison.OrdinalIgnoreCase);

var subTotal = 0;
var settings = doc.Settings;

if (settings == null || settings.Categories == null)
{
    findings.Add("This document reports no category settings at all, which is "
        + "not something a project should do");
}
else
{
    var all = new List<Category>();
    foreach (Category category in settings.Categories)
    {
        if (category == null) continue;

        var name = category.Name;
        if (string.IsNullOrEmpty(name)) continue;
        names.Add(name);
        all.Add(category);

        try
        {
            if (category.AllowsBoundParameters) bindable++;
        }
        catch
        {
        }

        try
        {
            if (category.SubCategories != null) subTotal += category.SubCategories.Size;
        }
        catch
        {
        }
    }

    names.Sort(StringComparer.OrdinalIgnoreCase);

    findings.Add(string.Format(
        "{0} category/categories in this document, {1} of which can carry a "
        + "project parameter, with {2} sub-category/sub-categories between them. "
        + "These names are LOCALISED - on a non-English Revit they are the names "
        + "that machine will accept, and the internal identifiers are not",
        names.Count, bindable, subTotal));

    // OBJECT STYLES - one row per category or subcategory.
    var solid = LinePatternElement.GetSolidPatternId();
    var unreadable = 0;

    Func<Category, string, string> row = (category, label) =>
    {
        string colourText, projection, cut, pattern;
        try
        {
            var c = category.LineColor;
            colourText = c == null || !c.IsValid ? "none" : c.Red + "," + c.Green + "," + c.Blue;
        }
        catch (Exception) { colourText = "unreadable"; unreadable++; }
        try
        {
            var w = category.GetLineWeight(GraphicsStyleType.Projection);
            projection = w == null ? "none" : w.Value.ToString();
        }
        catch (Exception) { projection = "unreadable"; unreadable++; }
        try
        {
            if (!category.IsCuttable) cut = "-";
            else
            {
                var w = category.GetLineWeight(GraphicsStyleType.Cut);
                cut = w == null ? "none" : w.Value.ToString();
            }
        }
        catch (Exception) { cut = "unreadable"; unreadable++; }
        try
        {
            var id = category.GetLinePatternId(GraphicsStyleType.Projection);
            if (id == null || id == ElementId.InvalidElementId) pattern = "none";
            else if (id == solid) pattern = "Solid";
            else
            {
                var element = doc.GetElement(id);
                pattern = element == null ? "none" : element.Name;
            }
        }
        catch (Exception) { pattern = "unreadable"; unreadable++; }
        return label + " | " + colourText + " | " + projection + " | " + cut + " | " + pattern;
    };

    Func<Category, List<Category>> subcategoriesOf = category =>
    {
        var found = new List<Category>();
        try
        {
            if (category.SubCategories != null)
            {
                foreach (Category sub in category.SubCategories)
                {
                    if (sub != null && !string.IsNullOrEmpty(sub.Name)) found.Add(sub);
                }
            }
        }
        catch (Exception) { unreadable++; }
        found.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
        return found;
    };

    // THE FAMILY'S OWN CATEGORY, in a family open in the Family Editor.
    Category own = null;
    if (doc.IsFamilyDocument)
    {
        try
        {
            own = doc.OwnerFamily == null ? null : doc.OwnerFamily.FamilyCategory;
        }
        catch (Exception) { unreadable++; }
    }

    // THE COLUMNS, one line under the first in either answer below.
    var columns = "category > subcategory | projection line colour R,G,B | projection line weight | "
        + "cut line weight (- not cut) | projection line pattern";

    if (narrowed)
    {
        // VERSION 3 - ONLY THE ROWS ASKED FOR. Names compared with capitals and
        // runs of spaces forgiven, in whatever language this Revit names them.
        Func<string, string> plain = text => string.Join(" ",
            (text ?? "").Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();

        // Edit distance, for the names nearest to one the document has not got.
        Func<string, string, int> distance = (a, b) =>
        {
            var previous = new int[b.Length + 1];
            var current = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++) previous[j] = j;
            for (var i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (var j = 1; j <= b.Length; j++)
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1),
                        previous[j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1));
                var swap = previous;
                previous = current;
                current = swap;
            }
            return previous[b.Length];
        };
        Func<string, string, int> closeness = (wanted, candidate) =>
            candidate.Contains(wanted) || wanted.Contains(candidate) ? 0 : distance(wanted, candidate);

        // EVERY CATEGORY WITH AN OBJECT STYLES ROW, the family's own first, each
        // with its subcategories read once; and every internal one, so asking
        // for one is answered as internal rather than as missing.
        var styled = new List<Category>();
        var styledSubs = new List<List<Category>>();
        var internalOnes = new List<Category>();
        if (own != null)
        {
            styled.Add(own);
            styledSubs.Add(subcategoriesOf(own));
            familySubcategories = styledSubs[0].Count;
        }
        all.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
        foreach (var category in all)
        {
            if (own != null && category.Id == own.Id) continue;

            CategoryType type;
            try { type = category.CategoryType; }
            catch (Exception) { type = CategoryType.Invalid; }
            if (type != CategoryType.Model && type != CategoryType.Annotation
                && type != CategoryType.AnalyticalModel)
            {
                internalOnes.Add(category);
                continue;
            }
            styled.Add(category);
            styledSubs.Add(subcategoriesOf(category));
        }

        // Each label a row could carry: as printed, plain, and its last part plain.
        var labels = new List<string[]>();
        for (var i = 0; i < styled.Count; i++)
        {
            labels.Add(new[] { styled[i].Name, plain(styled[i].Name), plain(styled[i].Name) });
            foreach (var sub in styledSubs[i])
                labels.Add(new[] { styled[i].Name + " > " + sub.Name,
                    plain(styled[i].Name) + " > " + plain(sub.Name), plain(sub.Name) });
        }

        // THE FIVE NEAREST, closest first. `whole` compares the whole label,
        // for a name typed as "category > subcategory"; otherwise a bare name
        // is also held against each subcategory's own name.
        Func<string, List<string[]>, bool, string> nearest = (wanted, pool, whole) =>
        {
            var scored = new List<KeyValuePair<int, string>>();
            foreach (var label in pool)
            {
                var score = closeness(wanted, label[1]);
                if (!whole) score = Math.Min(score, closeness(wanted, label[2]));
                scored.Add(new KeyValuePair<int, string>(score, label[0]));
            }
            scored.Sort((a, b) => a.Key != b.Key ? a.Key.CompareTo(b.Key)
                : StringComparer.OrdinalIgnoreCase.Compare(a.Value, b.Value));
            var picked = new List<string>();
            foreach (var pair in scored)
            {
                if (picked.Count == 5) break;
                picked.Add(pair.Value);
            }
            return string.Join(", ", picked.ToArray())
                + (pool.Count > picked.Count ? " (" + pool.Count + " names in all)" : "");
        };

        var rows = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var asked = new List<string>();
        var notFound = new List<string>();
        foreach (var piece in askedText.Split(','))
        {
            var item = piece.Trim();
            if (item.Length == 0) continue;
            asked.Add(item);

            var gt = item.IndexOf('>');
            var categoryPart = plain(gt >= 0 ? item.Substring(0, gt) : item);
            var subPart = gt >= 0 ? plain(item.Substring(gt + 1)) : "";

            var at = -1;
            for (var i = 0; i < styled.Count && at < 0; i++)
                if (plain(styled[i].Name) == categoryPart) at = i;

            if (at >= 0 && subPart.Length == 0)
            {
                // A CATEGORY: its own row and every subcategory of it.
                if (seen.Add(styled[at].Name)) rows.Add(row(styled[at], styled[at].Name));
                foreach (var sub in styledSubs[at])
                {
                    var label = styled[at].Name + " > " + sub.Name;
                    if (seen.Add(label)) rows.Add(row(sub, label));
                }
                continue;
            }

            if (at >= 0)
            {
                // CATEGORY > SUBCATEGORY: that one row.
                Category match = null;
                foreach (var sub in styledSubs[at])
                {
                    if (plain(sub.Name) == subPart) { match = sub; break; }
                }
                if (match != null)
                {
                    var label = styled[at].Name + " > " + match.Name;
                    if (seen.Add(label)) rows.Add(row(match, label));
                    continue;
                }

                var typed = item.Substring(gt + 1).Trim();
                if (styledSubs[at].Count == 0)
                {
                    notFound.Add(styled[at].Name + " has no subcategory \"" + typed + "\" - it has no "
                        + "subcategories at all; its own row is \"" + styled[at].Name + "\".");
                }
                else
                {
                    var theirs = new List<string[]>();
                    foreach (var sub in styledSubs[at])
                        theirs.Add(new[] { styled[at].Name + " > " + sub.Name, plain(sub.Name), plain(sub.Name) });
                    notFound.Add(styled[at].Name + " has no subcategory \"" + typed + "\" - nearest: "
                        + nearest(subPart, theirs, true) + ".");
                }
                continue;
            }

            var isInternal = false;
            foreach (var category in internalOnes)
            {
                if (plain(category.Name) == categoryPart) { isInternal = true; break; }
            }
            if (isInternal)
            {
                notFound.Add("\"" + item + "\" is an internal category - Object Styles has no row for it.");
                continue;
            }

            if (gt < 0)
            {
                // A BARE NAME NO CATEGORY HAS: every subcategory of that name.
                var any = false;
                for (var i = 0; i < styled.Count; i++)
                {
                    foreach (var sub in styledSubs[i])
                    {
                        if (plain(sub.Name) != categoryPart) continue;
                        any = true;
                        var label = styled[i].Name + " > " + sub.Name;
                        if (seen.Add(label)) rows.Add(row(sub, label));
                    }
                }
                if (any) continue;
            }

            notFound.Add("Nothing in this document is named \"" + item + "\" - nearest: "
                + (labels.Count == 0 ? "none, this document has no Object Styles row at all"
                    : nearest(gt < 0 ? categoryPart : categoryPart + " > " + subPart, labels, gt >= 0))
                + ".");
        }

        styleRows = rows.Count;
        var missing = notFound.Count == 0 ? "" : " " + string.Join(" ", notFound.ToArray());
        if (everySubcategory) missing += " everySubcategory has no effect when names are asked for.";
        if (unreadable > 0) missing += " " + unreadable + " value(s) could not be read and say unreadable.";

        // NO ROW, NO TABLE: the column line over nothing would read as "there is
        // nothing there", and what is true is "nothing by that name".
        string first;
        if (rows.Count == 0)
        {
            first = "Object Styles: NO ROW - nothing in this document is named as asked ("
                + string.Join(", ", asked.ToArray()) + ")." + missing
                + " Ask again with a name this document has, or leave categories out for every row.";
            objectStyles = first;
        }
        else
        {
            first = "Object Styles, " + rows.Count + " row(s), read off each category - not a view's override. "
                + "Only the name(s) asked: " + string.Join(", ", asked.ToArray()) + "." + missing;
            objectStyles = first + "\n" + columns + "\n" + string.Join("\n", rows.ToArray());
        }
        findings.Add(first);
    }
    else
    {
        var rows = new List<string>();
        if (own != null)
        {
            rows.Add(row(own, own.Name));
            foreach (var sub in subcategoriesOf(own))
            {
                rows.Add(row(sub, own.Name + " > " + sub.Name));
                familySubcategories++;
            }
        }

        all.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
        var internalLeftOut = 0;
        var subcategoriesLeftOut = 0;
        foreach (var category in all)
        {
            if (own != null && category.Id == own.Id) continue;

            CategoryType type;
            try { type = category.CategoryType; }
            catch (Exception) { type = CategoryType.Invalid; }
            if (type != CategoryType.Model && type != CategoryType.Annotation
                && type != CategoryType.AnalyticalModel)
            {
                internalLeftOut++;
                continue;
            }

            rows.Add(row(category, category.Name));
            var subs = subcategoriesOf(category);
            if (everySubcategory)
            {
                foreach (var sub in subs) rows.Add(row(sub, category.Name + " > " + sub.Name));
            }
            else
            {
                subcategoriesLeftOut += subs.Count;
            }
        }

        // THE FIRST LINE SAYS WHAT THE ROWS ARE, because the findings below reach
        // a reply cut to sixty characters and this string reaches it whole.
        string scope;
        if (doc.IsFamilyDocument && own == null)
            scope = "This family reports no category of its own, so no row is its own.";
        else if (own != null)
            scope = "This family's own category, " + own.Name + ", and its " + familySubcategories
                + " subcategory(ies) are the first " + (familySubcategories + 1) + " row(s)"
                + (familySubcategories == 0 ? " - it has no subcategories, so its own row stands alone." : ".");
        else if (everySubcategory)
            scope = "This is a project: every category, sorted by name, each followed by its subcategories.";
        else
            scope = "This is a project: one row per category, sorted by name.";

        var leftOut = "";
        if (subcategoriesLeftOut > 0)
            leftOut += " " + subcategoriesLeftOut + " subcategory(ies) of the other categories are not "
                + "listed - everySubcategory=true lists every one.";
        if (internalLeftOut > 0)
            leftOut += " " + internalLeftOut + " internal category(ies) have no Object Styles row and are left out.";
        if (unreadable > 0)
            leftOut += " " + unreadable + " value(s) could not be read and say unreadable.";

        objectStyles = "Object Styles, " + rows.Count + " row(s), read off each category - not a view's "
            + "override. " + scope + leftOut + "\n"
            + columns + "\n"
            + string.Join("\n", rows.ToArray());

        findings.Add(scope + leftOut);
        styleRows = rows.Count;
    }
}
