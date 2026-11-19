# `policy/` — Group Policy templates for Heron's three ceilings

**For an office's IT, not for a modeller.** Heron's **Changes**, **Admin** and **Publish** switches are
turned on only by a click in Heron's Settings window in Revit, and are off whenever Revit starts
([D-166](../../docs/decisions/D-166.md)). Above them sits a ceiling only an administrator can set:
`HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Heron`, DWORDs `AllowChanges`, `AllowAdmin`, `AllowPublish` - read by
`HeronMachinePolicy` in [`HeronApplication.cs`](../../revit/Heron.Revit.Addin/HeronApplication.cs).
These two files let IT set that ceiling from Group Policy or Intune, as it sets everything else.

| File | What it is |
|---|---|
| [`Heron.admx`](Heron.admx) | The template: three machine policies under *Computer Configuration > Administrative Templates > Heron* |
| [`en-US/Heron.adml`](en-US/Heron.adml) | Its English words - what each policy does and does not do |

| Policy | Enabled | Disabled | Not configured |
|---|---|---|---|
| Allow Heron's Changes switch | `AllowChanges` = 1: the modeller may turn it on | 0: it cannot be turned on on this PC | no value, no ceiling |
| Allow Heron's Admin switch | `AllowAdmin` = 1 | 0 | no value |
| Allow Heron's Publish switch | `AllowPublish` = 1 | 0 | no value |

**A ceiling never turns a switch on.** Enabled only allows the modeller to turn it on in Revit. Any value
that is not a DWORD reads as 0 - closed - and a registry that cannot be read holds all three off.

**To install:** copy `Heron.admx` to `C:\Windows\PolicyDefinitions\` (or the domain's central store,
`\\<domain>\SYSVOL\<domain>\Policies\PolicyDefinitions\`) and `en-US\Heron.adml` to the `en-US` folder
beside it. For Intune, import the ADMX and ADML as a custom administrative template.

**Held by** [`tests/test_policy_templates.py`](../../tests/test_policy_templates.py): every name is read
from the add-in's own C#, so a value renamed there fails the test, not a PC. **Not yet run** on a
domain or in Intune - NEEDS a Windows PC with the Group Policy editor. Not shipped in the Setup or the
release assets: IT takes the two files from here.
