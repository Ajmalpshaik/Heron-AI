# Needs checking — Group BE

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-28 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there.

## Group BE - D-59's first batch: eleven reading tools look inside linked models when asked (2026-09-28)

**[D-59](../decisions/D-59.md) built for the eleven reading fragments the owner runs most, ranked from the
September audit log on his PC.** Each takes `includeLinks` - absent means this model only, exactly what its
proof measured - and reports `linksSearched`, the number of linked FILES actually read, and
`linkedMatches`, what was found there as text. **Nothing from a link is ever selected or carried to the next
step**: the chain looks carried ids up in the host, and a linked id that happens to be in use there binds an
unrelated element ([row 75](../FRAGMENT-ISSUES.md)). Why and what was decided: [row 5b-253](../FRAGMENT-ISSUES.md).

**Every one of them compiles on all eight releases and none has been run.** Ten were `PROVEN` and went back to
`DRAFT` as version 2, because their proofs describe code that no longer runs ([D-30](../DECISIONS.md));
`report-door-room-links` was already `DRAFT`.

**BE1 comes first and the rest cannot pass without it.** Before this change the add-in REFUSED any request
value left out ([row 5b-227](../FRAGMENT-ISSUES.md)), so a tool that took `includeLinks` stopped running for
everyone who did not know the switch was there - `report-areas` has been in that state since it gained the
switch ([row 5b-252](../FRAGMENT-ISSUES.md)). The add-in now binds an absent `bool` marked `optional: true`
to `false`. **That is a C# change: it needs the add-in built, deployed and Revit restarted.** The fragments
alone reach Revit on the next call; the binder does not.

**The model is `Snowdon-scratch_ajmal.al`**, which has six links, the architectural one holding the doors,
walls and rooms. A second, **link-free model** is the negative case for every row - check Manage Links on it first, because nothing here knows which of his models have none.

| ID | Do this | Pass looks like |
|---|---|---|
| **BE1** | Build and deploy the add-in, restart Revit, and run `select-by-category-name` with `categoryName=Ducts` and **no** `includeLinks` | It RUNS - no `needs_request_values` - and returns the same 307 ducts version 1's proof did. The binding note names `includeLinks` as not given and bound false. `linksSearched 0`, and `linkedMatches` starts `Host model only - links not read`. **If it refuses, the add-in in Revit is older than this change** |
| **BE2** | `select-by-category-name`, `categoryName=Doors`, `includeLinks=true`. Then again with `inViewOnly` set to a plan that shows the architectural link | Without the view: `elements` exactly what the host-only run gives, `linksSearched` the number of loaded link FILES (not placements), and the architectural link's line carrying its door count - check it against a door schedule opened IN that link. With the view: a smaller linked count, the doors that plan draws. **Negative:** the link-free model with `includeLinks=true` reads `Links asked for, NONE loaded - host only`, not the host-only sentence |
| **BE3** | `select-by-categories`, `Doors` and `Walls`, `includeLinks=true` | `elements` and `perCategory` unchanged from the host-only run; the link's line shows its own `Doors N, Walls M`. Walls on Snowdon: [row 75](../FRAGMENT-ISSUES.md) measured 1128 in the architectural link, so that is the number to expect. **Negative:** as BE2 |
| **BE4** | `select-by-numeric-parameter`, `Walls`, `Unconnected Height gt 0`, `includeLinks=true` | `elements` unchanged; the link's line reads `N of M match` with M its wall count. **Negative:** a parameter name nothing carries - every link line says all M lack it |
| **BE5** | `select-by-parameter-value`, `Doors`, `Family contains Door`, `includeLinks=true` | As BE4, with the link's doors matched by their family name. This is the one whose TYPE lookup moved from the host to the element's own document - a linked door matched by `Family` at all is the evidence that moved correctly |
| **BE6** | `filter-elements-by-category`, `Doors`, a host level whose name exists in the architectural link, `includeLinks=true` | `elements` unchanged; the link's line reads `N on a level named '<name>'`, N matching a door schedule in the link filtered to that level. **Negative:** a host level whose name the link does not have - the link's count is 0, and says which name it looked for |
| **BE7** | `list-levels`, `includeLinks=true` | The host's levels as before, then each link's levels in height order. Where a link is placed higher or lower, a line gives the host-origin figure - check one against a section cut through both |
| **BE8** | `select-by-connection-status`, `Ducts`, `wantOpenEnds=false`, `includeLinks=true` | `elements` unchanged; each link with ducts reports its joined-of-scanned count. A link with none reads `0 of 0` |
| **BE9** | `select-unenclosed-rooms`, `includeRooms=true`, `includeLinks=true` | The host lists unchanged; each faulty LINKED room named with its number and `NOT PLACED` or `NOT ENCLOSED`. Make one on purpose in the architectural link first - an unplaced room from its schedule - so the positive has something to find |
| **BE10** | Select the host ducts, run `count-by-spatial-container` with `containerKind=room`, `includeLinks=true` | THE CASE D-59 EXISTS FOR. Host-only puts every duct `(in no room)`; with links they land under `<link> :: <room>`. `placed + unplaced` still equals the ducts handed in. Pick three and check the room by eye in plan |
| **BE11** | `report-door-room-links` with a host door or two selected, `includeLinks=true` | The host doors as before, then a line per link: its door count, the phase used, and every door whose From/To disagrees with the geometry. **Flip one door in the link first** and confirm it is the one reported |
| **BE12** | `select-in-region`, a volume around one floor of the architectural link, `includeLinks=true`, once with `exact=false` and once with `exact=true` | `elements` unchanged. The link's count from the box test is at least the exact test's - the box is looser. On a link placed at an angle, a line says the box was looser still |
| **BE13** | Any one of the above with `includeLinks=true` on a model where a link is LINKED INSIDE A LINK (attach) | The nested link is NOT read, and the last lines say `N link placement(s) nested inside those links were NOT read`. **This is the open half of D-59** - decide from what you see whether nested links should be read, and [row 5b-253](../FRAGMENT-ISSUES.md) records the answer |

**What cannot be answered here.** Every row needs a Revit. What was checked without one: the eleven compile
on 2020 to 2027, and the binder's new rule is run in `tests/Heron.BindingNote.TestHost` through
`tests/test_binding_note.py`.
