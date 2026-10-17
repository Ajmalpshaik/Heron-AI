# Needs checking — Group GW

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-17 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group GW - Heron's own C# compiler on a .NET Revit, whatever another add-in loaded first (2026-10-17)

On the owner's laptop on 2026-10-17 every fragment failed on Revit 2026.5 - *"Method 'CommonGetSemanticModel'
... does not have an implementation"* - because AJ Tools had loaded its Roslyn 4.14 into the load context every
add-in shares on a .NET Revit, and Heron's 4.8 C# layer bound to it; on 2027.3 Heron compiled on AJ Tools'
compiler by accident ([FRAGMENT-ISSUES 5b-575](../fragment-issues/section-5b-rows-401-425.md),
[Group EQ](group-eq.md) EQ5 and EQ6). (Groups up to GP were taken on other branches when this was written, and
other sessions were running, so this group skipped to GW.)

**What changed.** On Revit 2025 and later the add-in compiles through
[`HeronScriptHost`](../../revit/Heron.Revit.Addin/HeronScriptHost.cs), which loads `Heron.Scripting.dll` - the
engine and the stack guard - into a load context of Heron's own: `Heron.Scripting` and every
`Microsoft.CodeAnalysis*` file come from Heron's folder, everything else (the Revit API, the add-in, `Heron.Core`,
the runtime) from the default context. No manifest changed. The deployed folder gains one file,
`Heron.Scripting.dll`, beside the four compiler files it already had; on 2025 and later those four are no longer
in `Heron.Revit.Addin.deps.json`. Revit 2020 to 2024 call the same engine directly from the add-in, as before.
The warm-up's log line now names the compiler it used: on 2025 and later it ends *"in Heron's own load
context"*.

**What is proved and what is not.** **Nothing below has run in Revit.** Measured without Revit on 2026-10-17
by [`tests/test_script_isolation.py`](../../tests/test_script_isolation.py), which plays Revit in one process per
case: another add-in's Roslyn (4.14 on .NET 10, 4.13 on .NET 8) loaded into the default context first, Heron's
folder packaged by the same `Heron.Scripting.targets` the add-in imports. The old binding reproduced the row's
exact `TypeLoadException`; the door compiled and ran on Heron's 4.8 in every case, and the script reached the
host's own types. **What only Revit can settle:** that Revit 2026 and 2027 resolve an add-in's files the way that
host does, that a real fragment compiles and runs with AJ Tools still installed, that AJ Tools still works beside
it, and that 2020 and 2024 did not move.

Build the add-in from this branch for each release and deploy it as Group EQ did (`tools/deploy-addin.ps1`),
then check the deployed folder has `Heron.Scripting.dll` beside `Heron.Revit.Addin.dll` on 2026 and 2027. **Keep
AJ Tools installed** - it is the case. A scratch copy of each release's own `Samples` model, as in EQ; never a
working model. Read `%LOCALAPPDATA%\Heron\logs\addin-<date>.log` before pressing the Heron button - it is a
toggle ([Group EQ](group-eq.md)).

| # | Run | Look for |
|---|---|---|
| **GW1** | **Revit 2026**, AJ Tools still installed. Start it on "EQ-2026 Snowdon Towers Sample HVAC", let Heron load, open the add-in log | `Fragment warm-up compiled and emitted a throwaway script in ... ms, on Microsoft.CodeAnalysis.CSharp 4.8.0.0 from <Heron's add-in folder>\Microsoft.CodeAnalysis.CSharp.dll, in Heron's own load context.` **Not** *"Fragment warm-up failed"*. Write down the line |
| **GW2** | Connect the bridge; `python mcp/client/heron_bridge_client.py fragment --session <pid> --in "EQ-2026 Snowdon Towers Sample HVAC" list-sheets`, then `list-levels` | Both answer - the model's sheets and levels - where EQ5 got `operation_failed`. Write down both counts |
| **GW3** | In PowerShell: `(Get-Process -Id <pid>).Modules \| Where-Object ModuleName -like 'Microsoft.CodeAnalysis*' \| Select-Object ModuleName, FileVersion, FileName` | `Microsoft.CodeAnalysis.CSharp.dll` - and the other three - **from Heron's add-in folder**, 4.8; AJ Tools' 4.14 files from AJ Tools' folder beside them if AJ Tools has loaded them. Copy the table into this row |
| **GW4** | AJ Tools, in the same Revit: one of its commands that compiles or runs C#, if it has one | It still works. Heron no longer puts its 4.8 into the shared context, so AJ Tools should be unaffected whichever loads first; a failure here is a finding, with AJ Tools' own message |
| **GW5** | GW1 to GW3 on **Revit 2027**, AJ Tools still installed, "EQ-2027 Snowdon Towers Sample HVAC" | The warm-up line names Heron's folder and *"in Heron's own load context"* - where on 2027.3 Heron had compiled on AJ Tools' compiler; `list-sheets` answers the model's 17 sheets; the module table shows `Microsoft.CodeAnalysis.CSharp.dll` from Heron's folder |
| **GW6** | **Revit 2020 and 2024**, the same build: start each, read the warm-up line, run `list-sheets` | 2020-2024 unchanged: the warm-up compiles (its line names the compiler and does **not** say *"in Heron's own load context"* - that branch is the .NET one), and `list-sheets` answers |
| **GW7** | Each .NET release's journal, for the line EQ5 found at 1438: *"Addin's module Microsoft.CodeAnalysis of version 4.8.0.0 conflicts with same preloaded module of version 4.14.0.0"* | Record whether it still appears. Either way it decides nothing - GW1 to GW3 do. Heron's folder still holds 4.8, but the add-in's deps.json no longer lists it, and which of the two Revit reads for that line is not known |
| **GW8** | **Revit 2025**, when a PC has it - not installed on the owner's laptop | GW1 to GW3 there: 2025 is .NET 8 and has no manifest isolation at all, and the test above ran on .NET 8 against Roslyn 4.13 |
