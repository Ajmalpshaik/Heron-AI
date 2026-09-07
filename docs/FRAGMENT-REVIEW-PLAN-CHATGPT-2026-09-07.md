# Fragment review and improvement plan

**Prepared by: ChatGPT (Codex)**  
**Date: 7 September 2026**  
**Project: Heron AI**  
**Status: Plan revised after discussion with Ajmal — implementation not started.**

No existing fragment, contract, implementation, test, or lifecycle status was changed. This report is the requested planning deliverable.

## 1. Recommendation

Yes, there is room to add capabilities and improve existing fragments. First fix the concrete behavior and contract problems below. Then make a small number of reusable extractions and add capabilities tied to actual workflows. Do not split everything or increase the count merely to make the library larger.

The library already has broad coverage. Its more immediate need is trustworthy behavior, structured outputs that other operations can consume, and demonstrated end-to-end workflows.

### Discussion decisions

- **Bulk execution:** When Ajmal has supplied the criteria and instructed an operation, validate automatically and execute the authorized batch. Do not ask for permission for each element or each internal step. Report changes, skipped items and failures together at the end. Ask only when a missing decision materially prevents correct execution; existing authorization does not need repeating.
- **C02:** Replace the earlier mandatory preview/pause recommendation with automatic validation before bulk sizing. A preview remains optional when requested. A message saying STOP is not a substitute for a programmatic check.
- **C03:** Ajmal agreed with exact matching unless an explicit wildcard is supplied. The family-name example explained matching semantics; extending this checker to family names was not agreed or verified.
- **C04:** Multiline CSV cells were explained and understood. Preserve one cell as one value; validate before bulk import without per-row prompts. The additional export-identity redesign remains an optional proposal, not a discussion decision.
- **C05:** Ajmal agreed with checking compatibility first, then choosing the nearest compatible duct within the supplied distance, and verifying the physical connection afterward.
- **C06–C09:** Ajmal agreed in principle with explicit ownership uncertainty, separate room-hole outlines, structured connector measurements and refreshed documentation.
- **New capabilities:** Ajmal requested that all six candidates remain in the plan and be tackled one by one. They are accepted planning backlog items, not authorization to implement all six now. The catalog was rechecked during discussion and still contained 349 fragments; targeted contract and implementation searches supported retaining these candidates.
- **Review position:** The first split proposal was introduced, but no split was explicitly selected. Split decisions remain pending. The current request authorizes updating this plan, not implementing fragment changes.

## 2. What was checked

The review inventoried all fragment manifests, capabilities, status declarations, domains and fragment-local test files; ran the existing fragment and skill validators; and inspected selected implementations and contracts for correctness, duplication and missing workflow steps. It is **not an exhaustive line-by-line audit of all 349 implementations**.

| Check | Result |
|---|---|
| Fragment folders/manifests | 349 |
| Metadata validator | All 349 well-formed |
| Declared lifecycle status | 333 DRAFT; 16 PROVEN |
| Duplicate capability names | None |
| Fragment-local test directories | All 349 present |
| Files in those test directories | 349 YAML files; these are not proof that executable tests ran |
| Skill validator | 10 skills valid; no missing capability providers reported |
| Skill status | All 10 DRAFT |

Commands run: `python brain/heron_fragment.py` and `python brain/heron_skill.py`, both successful. No Revit model was modified or tested. No fresh multi-version compilation was run. PROVEN counts are metadata declarations, not an independent re-verification of those proofs.

No existing `graphify-out/graph.json` was present; findings below come from repository files and validators, not a generated graph. Older memory did not provide relevant project evidence.

## 3. Existing fragments: proposed changes

Priorities: **P1** = fix before trusting the affected workflow; **P2** = improve reuse, clarity or coverage. Source links refer to the current implementation unless a manifest is specified. Runtime consequences are inferred from source and still require reproduction in the stated test cases.

### C01 — Preserve room holes or refuse creation (P1)

**Fragment:** `create-from-room-boundaries`  
**Evidence:** [implementation](../brain/fragments/create-from-room-boundaries/impl/any/fragment.cs), legacy floor branch and curve-loop construction.

The legacy floor branch creates from `curveLoops[0]` even when there are holes, adds a refusal message saying the hole was not cut, and still records the floor in `created`. Invalid loops can also be silently discarded before creation. A warning after construction does not preserve the requested geometry.

**Proposed change:** Validate every required loop before writing. If a required hole cannot be preserved, refuse that room before creation or use a separately verified opening implementation. Return separate created, refused and incomplete outcomes. Do not classify an incomplete slab as successful.

**Acceptance:** A room with two shafts preserves both; an invalid inner loop creates nothing for that room; a release without the required geometry support returns a clear refusal. Verify undo behavior with the operation owner.

### C02 — Make sizing checks actionable before writes (P1)

**Fragments:** `auto-size-mep`, `auto-size-pipe`  
**Evidence:** [duct sizing](../brain/fragments/auto-size-mep/impl/any/fragment.cs), [pipe sizing](../brain/fragments/auto-size-pipe/impl/any/fragment.cs).

The duct fragment appends a UNIT CHECK message telling the reader to stop if values disagree, then immediately continues into size writes. It also calls `OrderBy` on supplied size lists before validating them. The pipe fragment explicitly proceeds with approximate bore assumptions when bore data is absent or mismatched.

**Revised direction agreed in discussion:** Automatically validate the supplied criteria and size data before any writes, then execute the authorized batch without additional confirmation per duct, pipe or internal step. Reject null, non-finite, non-positive or inconsistent required size data before writes. Use the user's supplied policy for approximate pipe sizing; request clarification only if an essential policy is missing. Keep duct and pipe engineering calculations distinct. Replace the misleading STOP message with actual validation results. Return one final summary of applied sizes, skips, failures and unmet targets. Offer a read-only preview only when requested; an internal calculation phase must not introduce a mandatory approval pause.

**Acceptance:** Invalid required lists and NaN/infinite targets cause no mutation; a bore-list mismatch is explicit; actual stored sizes are rechecked against the requested velocity limit. A successful setter alone must not imply the design target was met. Valid criteria plus a bulk-change instruction execute the batch without per-element prompts and produce one consolidated result. Validation must not depend on the user reading a message after writes have already happened.

### C03 — Correct exact-pattern matching (P1)

**Fragment:** `check-model-standards`  
**Evidence:** [implementation](../brain/fragments/check-model-standards/impl/any/fragment.cs), local `matches` function.

For a pattern without `*`, the first-part branch checks `StartsWith`, then the function eventually returns true. Therefore the pattern `ABC` also accepts `ABC-extra`, although the documented language is literal text plus explicit wildcard `*`.

**Proposed change:** Treat a pattern without a wildcard as a full case-insensitive equality check. Preserve correct anchored behavior for wildcard patterns.

**Discussion example:** An exact rule `Supply Diffuser` rejects `Supply Diffuser OLD`; `Supply Diffuser*` allows the suffix. This illustrates the matching rule only. This item fixes checking, does not rename model elements, and does not add family-name coverage.

**Acceptance:** `ABC` matches `abc` and rejects `ABC-extra`; `ABC*` accepts the suffix; test leading, trailing, repeated and absent wildcards. These are pure logic cases and do not need a live model.

### C04 — Make CSV round trips preserve complete records (P1)

**Fragments:** `import-parameter-values`, `export-parameters-to-csv`  
**Evidence:** [import](../brain/fragments/import-parameter-values/impl/any/fragment.cs), `ReadAllLines` and per-line `splitRow`; [export](../brain/fragments/export-parameters-to-csv/impl/any/fragment.cs), quoted value output.

The importer parses each physical line independently. A quoted field containing a newline becomes multiple rows, so the current record reader cannot correctly round-trip multiline parameter text.

**Proposed change, clarified in discussion:** Parse CSV records across line boundaries so text entered with Alt+Enter inside one Excel cell remains one parameter value. Automatically validate the complete input and resolve targets before bulk writing; reject malformed quoting, conflicting rows and malformed headers. Apply the authorized import without per-row confirmation, then summarize the result. A preflight change list can be internal or shown on request; it is not a mandatory extra approval step.

**Optional extension, not agreed in discussion:** Consider document identity and stable element identity in a versioned export format: matching numeric ID text inside the selected scope alone cannot prove a file belongs to this model.

**Acceptance:** Export/import round trip with commas, quotes, Arabic text, blank values and embedded newlines; malformed input causes no writes; conflicting values for the same type parameter are reported before mutation.

### C05 — Use consistent connection evidence (P1)

**Fragments:** `connect-air-terminals`, `report-connectors`  
**Evidence:** [connection implementation](../brain/fragments/connect-air-terminals/impl/any/fragment.cs), `connectorOf` and `IsConnected`; [report contract](../brain/fragments/report-connectors/fragment.yaml), physical-reference policy.

The terminal connector helper returns the first End connector, without a domain or air-terminal category check. It uses `IsConnected` for both skipping and final verification. The connector report explicitly distinguishes that flag from actual partner references. Nearest-duct selection also occurs before compatibility filtering.

**Revised direction agreed in discussion:** Explicitly identify the intended HVAC connector and validate candidate elements. Filter ducts for compatibility first, then select the nearest compatible duct within the user's supplied distance limit and candidate scope. A nearer incompatible return duct must not prevent a supply diffuser from considering a compatible supply duct farther away within that limit. If no compatible candidate exists, report it without forcing a connection. Verify physical partners afterward, including that the terminal is joined to the intended duct. Return candidate pairs and rejection reasons without per-terminal confirmation for an already authorized batch.

**Acceptance:** Mixed input categories, a family with multiple connector domains, misleading connection flags, and a nearer incompatible duct beside a valid candidate. Rejected inputs must not be counted as connected.

### C06 — Preserve ownership uncertainty (P2)

**Fragments:** `read-element-ownership`, `report-element-ownership`  
**Evidence:** [read](../brain/fragments/read-element-ownership/impl/any/fragment.cs), [report](../brain/fragments/report-element-ownership/impl/any/fragment.cs).

One records an unreadable checkout status as owned by an unknown person; the other leaves it out of both editable and owned-by-others lists. Consumers therefore receive different classifications for the same uncertainty.

**Proposed change:** Introduce an explicit unknown-status result with a reason. Share the classification policy while retaining the richer report's creator and last-changer information. Treat editability as advisory, not a guarantee of a later write.

**Acceptance:** Unreadable ownership appears as unknown in both APIs; non-workshared models and known owners retain their intended results.

### C07 — Preserve loop structure in room geometry (P2)

**Fragment:** `read-room-geometry`  
**Evidence:** [implementation](../brain/fragments/read-room-geometry/impl/any/fragment.cs), flat `holes` list.

Outer and inner boundaries are separated, but every inner loop is flattened into a single list of curves. Multiple holes lose their individual grouping.

**Proposed change:** Add grouped loops through a versioned contract, preserving legacy outputs during migration. Record validity and containment classification rather than requiring consumers to reconstruct it from flat curves.

**Acceptance:** Two separate holes remain two separate loops; curved boundaries stay curved; layout and floor/ceiling consumers preserve excluded areas.

### C08 — Provide structured connector measurements (P2)

**Fragment:** `report-connectors`  
**Evidence:** [contract](../brain/fragments/report-connectors/fragment.yaml), `connectorFacts: IDictionary<ElementId, IList<string>>`.

Connector sizes, positions and directions are chiefly exposed as report strings. A downstream size filter or connection planner should not parse formatted prose to recover geometry.

**Proposed change:** Add a versioned structured connector result: owner identity, connector identity, domain, shape, numeric dimensions, origin, direction, physical partners and read failures. Retain human-readable reporting as a presentation layer. Agree on a contract type supported by the existing compiler/composer before implementation.

**Acceptance:** Exact size matching and connection planning consume numeric data with no string parsing; unsupported shapes have explicit unknown fields.

### C09 — Refresh the library documentation (P2)

**File:** [brain/README.md](../brain/README.md).

It still describes thirty-two fragments, all DRAFT, while the current validator finds 349 with 16 PROVEN. Its execution-status narrative should also be reconciled against the current implementation rather than copied into new documentation.

**Proposed change:** Generate count/status summaries from manifests or link to an authoritative current inventory. Check runtime claims independently. Documentation changes remain proposed here, not applied.

## 4. What should be split, shared, or kept separate?

The repository's [fragment guidance](09-skills-and-fragments.md) recommends splitting only when there are at least two actual or clearly imminent consumers. A shared implementation helper is sometimes more suitable than a separately searchable fragment.

| ID | Existing area | Proposed boundary | Consumers and decision |
|---|---|---|---|
| S01 | `create-from-room-boundaries` | Read/validate grouped room loops separately from host creation | Room-derived floors and ceilings, plus layout geometry. Reuse an improved `read-room-geometry`; do not create another competing reader. |
| S02 | `auto-size-mep`, `auto-size-pipe` | Separate automatic validation/calculation internally from applying verified sizes | Authorized bulk sizing and optional preview can reuse calculation; both can use a shared write/read-back mechanism related to `set-mep-size`. Keep their calculations separate. No mandatory user pause between phases; the extraction itself remains pending. |
| S03 | Ownership readers/reporters | Share status classification, retain lightweight and detailed public contracts | Both existing ownership fragments are consumers. Prefer a shared helper unless standalone composition has a demonstrated need. |
| S04 | `import-parameter-values` | Parse/resolve/validate before applying writes | Import preview and import execution need the same resolved change list. Put general CSV parsing at the appropriate host/shared layer; it need not be a searchable Revit fragment. |
| S05 | Connector report and terminal connection | Share structured connector reading and physical-partner rules | `report-connectors` and `connect-air-terminals`, then exact-size selection. Keep connection mutation separate from inspection. |

**Keep separate:** duct sizing versus pipe sizing, because bore semantics differ; `connect-open-ends` versus `connect-air-terminals` versus `place-mep-fitting`, because their connection operations differ; single-item and batch operations where their contracts genuinely differ. Do not merge based on similar names alone.

**Do not split just for size:** `plan-connection-order` already returns a plan without drawing. Large files such as `compare-models` are candidates for a later focused review, not evidence by themselves that another fragment is needed.

For any approved split, first run dependency analysis using `brain/heron_graph.py` on the affected fragment ID, preserve existing capability resolution, and check the compiler/composer against the proposed contracts. Avoid changing public outputs and every consumer in an untracked batch.

## 5. New capability candidates

These are **proposed library coverage gaps**, based on the inventory and targeted source searches. They are not promises of API support on every declared Revit release. Establish version support and demand before implementation. A gap in current skills is different from a useful workflow the ten current skills do not describe.

| ID | Candidate | Why existing coverage is insufficient | Initial scope and acceptance |
|---|---|---|---|
| N01 | Filter by connector size | Parameter filters do not directly express actual connector dimensions; `report-connectors` currently returns formatted facts | Consume structured HVAC connector data. Match rectangular dimensions with explicit tolerance and orientation policy, or round diameter. Return matched IDs plus unknown/no-connector cases; test an exact 230 × 230 mm selection. |
| N02 | Place hosted family instances | `place-family-instances` uses a point/symbol/level overload and has no host-face input | Separate face/host placement capability with explicit host/reference and orientation. Validate family placement type and returned host. Test invalid and linked-host references separately. |
| N03 | Create flex duct connections | Rigid duct creation and direct terminal tapping do not specify a flexible route | Explicit endpoints, type, route and length limits. Verify both physical connections and geometry; refuse unsupported systems. No automatic route guessing in the first version. |
| N04 | Create electrical circuits and assign panels | `create-electrical-run` creates containment, not an electrical circuit | Start with circuit creation from compatible selected devices; panel assignment can be a second capability if separately reused. Report circuit membership and refused devices; verify duplicate execution behavior. |
| N05 | Create coordinated openings/sleeves | `audit-mep-openings`, `check-sleeve-size` and clash checks inspect existing conditions; inspection is not creation | Start with a read-only opening proposal from a supported host/run intersection and explicit allowance. A separate approved write creates the supported opening or sleeve family. Test oblique penetrations and duplicates. |
| N06 | Disconnect selected physical connector pairs | Existing connection and trace operations do not provide a dedicated disconnection workflow in the inspected inventory | Explicit connector pairs, before/after partner checks, and a clear fitting-cleanup policy. Leave deletion to a separate explicit operation. |

### Additional requested capabilities: project and view switching

Ajmal requested the ability to switch between two or three projects already open in the same Revit session, continue working in the chosen project, and open or switch to a requested view. These are added to the plan only.

**N07 — Switch the active open project.** Discover the open user documents with enough identity information to distinguish similarly named projects. Activate the requested document and verify it is actually active before running subsequent operations. Refresh document, view and selection context after switching; never reuse element references or cached IDs from the previous document as if they belong to the new one. If the requested project is already active, return a successful no-op. If it is missing, ambiguous, closed during the request or cannot be activated, report that and do not continue the requested work in another project. Initial scope is already-open projects in one Revit process, not launching another Revit instance or automatically opening files from disk.

**N08 — Open or activate a view.** Resolve a requested existing view within the chosen document, open its UI view if necessary, and make it active. Reuse existing view discovery rather than creating another competing search capability. Verify both the active document and active view before dependent operations. An already-active view is a successful no-op. Missing, ambiguous or non-activatable views must produce clear results; do not create a new view as a substitute for opening an existing one.

**Example workflow:** “Switch to the MEP project, open the Level 02 ceiling plan, then select its diffusers.” Resolve the targets, switch project, verify it, activate the view, verify it, and only then perform the requested selection. Execute the already authorized sequence without asking at every step.

**Implementation placement to establish:** These are Revit UI/session capabilities and may require bridge or add-in support rather than ordinary model-editing fragments. A targeted source search found no calls to `OpenAndActivateDocument`, `RequestViewChange`, or an active-view assignment in the inspected Revit/MCP/brain sources. This is evidence of a missing direct implementation, not a full proof that every possible activation route is absent. Before coding, verify supported activation APIs, valid execution context, transaction restrictions and completion behavior against the supported Revit versions. Do not force UI activation into the normal model-edit transaction. Continue only after activation completes, not merely when a request is queued.

**Acceptance:** Three open projects; similarly named documents; unsaved projects; switching back and forth; a view in another open project; an already-active target; an invalid or closed target; an activation refused by Revit; and a subsequent operation verified to affect only the chosen document/view. If activation fails or the context changes unexpectedly, dependent work must not run against the previous or wrong project.

**N09 — Close opened view tabs.** Ajmal additionally requested closing views that are currently open. Enumerate the open UI views in the target document, resolve the requested tab or explicit set of tabs, and close those UI views without deleting the underlying saved Revit views or closing the project. Return which tabs closed, were already closed, or could not be closed, and report the resulting active document/view. If closing the active tab requires another view to be activated, use an explicitly supplied replacement or a documented deterministic fallback within the same project. Verify the supported API behavior and restrictions before implementation; if Revit prevents closing the last view, report that clearly without closing the document as a workaround. Reuse N08 for any required view activation. An authorized batch of tab closures should not prompt for each tab.

**N09 acceptance:** Close an inactive tab; close the active tab with another view available; close several explicitly requested tabs; request an already-closed tab; distinguish duplicate view names across projects; handle a last-view restriction; verify the saved views still exist and other projects remain open. Refresh active context before any subsequent operation. This addition is planning only; no view has been closed during this review.

**Agreed delivery approach: one item at a time.** The original six candidates and the three additional project/view capabilities are retained in the planning backlog. The recommended sequence below follows the MEP workflow discussed; Ajmal can change the order. IDs remain unchanged for traceability.

1. **N01 — Select by actual connector size.** Complete the necessary structured connector data first, then verify exact-size selection.
2. **N02 — Place families on a host face.** Start with a clearly supported host/family placement combination.
3. **N03 — Create flex duct connections.** Use explicit route and connection criteria, then verify both ends.
4. **N05 — Create coordinated openings or sleeves.** Define the supported host and opening/sleeve method before implementation.
5. **N04 — Create electrical circuits and assign panels.** Establish device compatibility and separate circuit creation from panel assignment where useful.
6. **N06 — Disconnect selected connector pairs.** Verify disconnection and make any fitting cleanup an explicit separate choice.
7. **N07 — Switch the active open project.** Establish verified document activation and refresh execution context.
8. **N08 — Open or activate an existing view.** Support same-project switching and compose with N07 for another open project. N07 and N08 can be prioritized ahead of the MEP additions when requested.
9. **N09 — Close opened view tabs.** Preserve saved views and projects, handle active/last-view restrictions, and verify the resulting active context. It can be prioritized with N07 and N08.

For each item, recheck existing coverage, establish the required inputs and supported Revit releases, then implement only when that item is requested. Validate and report its result before moving to the next item. Within an authorized item, automatic validation and bulk execution must not introduce repeated per-element permission prompts. These candidates have not yet been proven against live models or every Revit version.

Do **not** add a generic room/space placement fragment: `place-rooms` already describes both. Do not add separate tray and conduit creation fragments merely to increase coverage: `create-electrical-run` deliberately combines them.

## 6. Delivery plan after approval

1. **Correctness batch:** C01–C05. Reproduce each issue, add meaningful regression cases, then implement the approved fix. Pure CSV/pattern logic can be tested outside Revit; geometry and connectivity need controlled live-model checks.
2. **Contract batch:** C06–C08 with the selected S01–S05 extractions. Record old/new contracts, affected consumers and compatibility strategy. Keep human-readable results while adding machine-readable data.
3. **Coverage batch:** Start with one or two approved N-items. Add corresponding skills or workflow cases so the additions are discoverable and solve an end-to-end request.
4. **Evidence and documentation:** C09; run metadata/skill validation, the affected Python tests, composition checks and compilation on every claimed release. Record positive, negative, no-op and undo outcomes in real models before promoting status.

The presence of `tests/cases.yaml` is useful planning evidence, but does not replace an execution record. Do not promote the 333 DRAFT fragments as a bulk cleanup task. Do not invalidate or rewrite the 16 PROVEN declarations without examining their individual proofs.

## 7. Decision sheet for Ajmal

The discussion decisions below record agreement on direction, separately from authorization to implement. **Only this planning document has been updated.**

| Group | My recommendation | Decision |
|---|---|---|
| C01 | Preserve room holes or refuse creation | No specific decision recorded |
| C02 | Automatic validation, authorized bulk sizing, consolidated report | Revised direction agreed; implementation not requested |
| C03 | Exact match unless wildcard supplied | Direction agreed; implementation not requested |
| C04 | Preserve multiline cells; automatic validation and bulk import | Explained and understood; implementation not requested; optional identity extension pending |
| C05 | Nearest compatible duct within limit; verify physical connection | Direction agreed; implementation not requested |
| C06–C09 | Ownership uncertainty, grouped holes, structured connectors and documentation | Directions agreed in principle; implementation not requested |
| S01–S05 | Apply only with the related approved change and verified consumers | Discussion pending; S02 wording aligned with bulk-execution decision |
| N01–N02 | Best first additions for existing workflows | Accepted into plan; tackle individually when requested |
| N03–N06 | Flex connections, openings/sleeves, circuits/panels and disconnection | Accepted into plan; tackle individually in the recommended sequence above or Ajmal's chosen order |
| N07–N08 | Switch open projects and open/activate existing views | Explicitly requested for the plan; implementation not started |
| N09 | Close one or more requested open view tabs without deleting saved views | Explicitly requested for the plan; implementation not started |

**Author: ChatGPT (Codex). This is a planning report, not authorization to change the fragments.**
