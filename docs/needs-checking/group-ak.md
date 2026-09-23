# Needs checking — Group AK

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group AK - the cloud setup script, pasted into the environment again (2026-09-23)

**Not the owner's PC and not Revit: the Heron cloud environment's settings, at claude.ai/code or in the
desktop app.** [`tools/cloud-setup.sh`](../../tools/cloud-setup.sh) is not run from a checkout. It is
pasted into the environment's **Setup script** box ([38 §1](../38-the-cloud-environment.md)), so a fix
to the file reaches no session until the text in that box is replaced. On 2026-09-22 the old text
installed nothing and exited 0 (FRAGMENT-ISSUES row 5b-167). The fixed script was proved against
stand-ins recorded from that image (`tests/test_cloud_setup.py`) and has not yet been run by a real
environment.

**The `heron` MCP server will still not connect in a cloud session afterwards.** It exits off Windows by
design (FRAGMENT-ISSUES row 5b-162), so that is not a failure of these rows.

*Placed 2026-09-23 with the fixes for FRAGMENT-ISSUES rows 5b-167 to 5b-169.*

| # | Check | Expected |
|---|---|---|
| **AK1** | Replace the text in the Heron environment's Setup script box with the whole of `tools/cloud-setup.sh`, then start a fresh cloud session | Its setup output ends with `[heron] dotnet 10.0...` and `[heron] python packages: all five import`. A `MISSING:` line names what did not install and the log that says why - read that log before anything else |
| **AK2** | In that session: `python tests/test_mcp_serves.py` | Exit 0, with `SDK version` naming what pip chose - not exit 3. Exit 3 means `mcp` did not arrive, whatever the setup output said |
