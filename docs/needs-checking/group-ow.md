# Needs checking — Group OW

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OW - what the add-in fixes of 2026-11-20 left behind, issue #1102: the bridge keeps listening when every pipe is in use, two models of one title are told apart, and the chat reads the run id, the rollback and the count the add-in now sends - BUILT, NOT RUN IN REVIT (2026-11-20)

**Tracked as GitHub issue [#1136](https://github.com/Ajmalpshaik/Heron-AI/issues/1136) since 2026-11-20** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** issue [#1102](https://github.com/Ajmalpshaik/Heron-AI/issues/1102) holds what the change for
issues #932 to #934 and #1057 left: the bridge's listener gave up after a minute with every pipe instance
in use and still said it was running ([row 5b-1900](../fragment-issues/row-5b-1900.md)); the binder took
the selection from a model of the same title ([row 5b-1902](../fragment-issues/row-5b-1902.md)), and the
answer called such a model *in front* ([row 5b-1927](../fragment-issues/row-5b-1927.md)); and the server
did not read the run id, the rollback or the count the add-in now sends
([row 5b-1901](../fragment-issues/row-5b-1901.md)). Fixed in a cloud container with no Revit. The C# compiles
for every release (`tools/check-compile.py`, the .NET 10 SDK); the server half runs here against stand-in
replies; **none of it has met a Revit**. The add-in must be rebuilt and Revit restarted before OW1 to OW3
(`python tools/heron-ready.py`); OW4 to OW6 need only the server, but a real add-in's replies.

| Change | Files | Row | Checks |
|---|---|---|---|
| The listener never gives up while the bridge runs | `BridgeServer.cs` | [5b-1900](../fragment-issues/row-5b-1900.md) | OW1 |
| The selection is bound only from the target model itself | `RevitFragment.cs` (`BindNeeds`) | [5b-1902](../fragment-issues/row-5b-1902.md) | OW2 |
| *In front* means the target model itself | `RevitFragment.cs` (`Report`) | [5b-1927](../fragment-issues/row-5b-1927.md) | OW3 |
| A wait's run id is said; a rollback that did not hold is OUTCOME UNKNOWN, one that held says so; a stopped read says its count | `heron_failure.py`, `heron_server_core.py`, `heron_mcp_server.py` | [5b-1901](../fragment-issues/row-5b-1901.md) | OW4, OW5, OW6 |

**Proved off Revit, 2026-11-20:** `tests/test_bridge_listener.py` and `tests/test_document_identity.py`
(the C# read as text) and `tests/test_failure_analysis.py`'s section *what the add-in says beside its code*
(the server against stand-in replies) - each new check seen to FAIL on the code as it stood. **Read as text,
not run:** every line that calls the Revit API or a named pipe.

**The models: scratch copies, never a working model.** For OW2 and OW3 the two copies of one project
saved under one file name that Group OS's OS9 uses, `%TEMP%\heron-work\or\a\Twin.rvt` and
`%TEMP%\heron-work\or\b\Twin.rvt`; for OW4 to OW6 any scratch model, as Group OS's OS4 to OS6 say. Read
the document name on every reply.

| ID | Do this | Pass looks like |
|---|---|---|
| **OW1** | *Every pipe instance in use, then one comes free* - row 5b-1095's burst again on Windows: `tests/Heron.Bridge.TestHost` built for 2024 and driven by twenty threads at once, then hold sixteen connections open over a minute and open a seventeenth; close one | The add-in (or host) log says *"Every pipe instance is in use; trying again until one is"*, then once a minute *"Still no free pipe instance after N minute(s); still trying"* - never *"Bridge stopped listening"* - and the seventeenth connects within about a second of one closing; a ping then answers. **Negative:** the same with the bridge toggled off part way - it stops, and no later line says it is still trying. Write down whether run a's 60 s hang of row 5b-1095 came back |
| **OW2** | *A selection in a model of the same title, positive* - open `or\a\Twin.rvt` and `or\b\Twin.rvt` in one Revit (OS9 writes down whether Revit allows it); pin the chat to `a` (*use this model* with `a` in front), select two walls in `b`, bring `b` to the front, then `revit_read` a card that takes the selection - the chat aims it at `a` | Aimed at `a` while `b`, of the same title, is in front with its own selection: the selection is not bound - the binding note does not say `elements from the selection`, and the reply asks for the elements. **Negative:** the two walls selected in `a`, with `a` in front - bound from the selection, two walls |
| **OW3** | *In front, by identity* - with OW2's two models, a `revit_read` aimed at `a` while `b` is in front | The answer's `inFront` is **false**, and it says the model read is not the one on screen. **Negative:** aimed at the model in front - `inFront` true |
| **OW4** | *A wait's run id, said* - Group OS's OS4: *Ask to Keep* ON, a small `revit_change` on a scratch model, the box left open past `revit.askTimeoutSeconds` | The chat's reply says *"Its run is wf-...: once Revit has finished it, heron_run_record run=wf-... says how it ended"*; press Keep, then `heron_run_record` with that id reads the run, KEPT. **Negative:** the same call answered at once - no such sentence |
| **OW5** | *A rollback's word* - Group OS's OS6's write refusal (a MODIFY card that throws inside its run, asked to keep) | `outcome: ROLLED BACK - Heron did not keep it, and Revit's Heron said the rollback held - the model is as it was; ...`, and the model re-read is as it was. **Negative:** none can be forced safely on a real model - write down that no rollback that did not hold was met; the not-held path is proved off Revit only |
| **OW6** | *A stopped read's count* - Group OS's OS6's read that throws, through `revit_read` | `outcome: NOT ANSWERED`, and the reply says *"Nothing in the model was changed: Heron counted N element(s) before this read and the same after it."* with N the model's count. **Negative:** a read refused before it runs (a card left without a value it needs) - no such sentence |
