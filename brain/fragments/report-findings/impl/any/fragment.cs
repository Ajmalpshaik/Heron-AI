// NOT STANDALONE. Assumes `findings`, `checkedCount` and `whatWasChecked` are in
// scope. Any check in the library can feed this, which is why it takes plain
// strings rather than a shape only one caller can build.
//
// WHY `checkedCount` IS REQUIRED AND NOT OPTIONAL, AND WHY IT IS NOT NAMED
// `checked`: that is a reserved C# keyword and will not compile.
//
// "No problems found" is two completely different sentences depending on how
// much was looked at, and the difference is invisible in the answer:
//
//   checked 247, found 0    the model is clean
//   checked 0,   found 0    the filter matched nothing and the check never ran
//
// The second is the failure this whole library is built around, and it reads
// as good news. So the count of what was examined is part of the report, always,
// and a check that examined nothing says so first rather than reporting a pass.

var sb = new System.Text.StringBuilder();

if (checkedCount == 0)
{
    sb.AppendLine("NOTHING WAS CHECKED - " + whatWasChecked +
                  " matched no elements, so this is not a pass.");
}
else if (findings.Count == 0)
{
    sb.AppendLine("Checked " + checkedCount + " " + whatWasChecked + ". No problems found.");
}
else
{
    sb.AppendLine("Checked " + checkedCount + " " + whatWasChecked + ". " +
                  findings.Count + " to look at:");
    foreach (var line in findings) sb.AppendLine("  - " + line);
}

var report = sb.ToString();
