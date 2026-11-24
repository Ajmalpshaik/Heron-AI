# NEEDS-CHECKING archive

**Checks that were done, from [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md). Nothing here is a live
obligation.** One file per group; every row here still has its struck line in the register, with a link
to its full text in one of these files.

> Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py) every time it moves
> rows. **Change the tool, not this page.**

| Group | File |
|---|---|
| Group BB | [`group-bb.md`](README.md) |
| Group DH | [`group-dh.md`](README.md) |
| Group DV | [`group-dv.md`](README.md) |
| Group DW | [`group-dw.md`](README.md) |
| Group DX | [`group-dx.md`](README.md) |
| Group EA | [`group-ea.md`](README.md) |
| Group EB | [`group-eb.md`](README.md) |
| Group HK | [`group-hk.md`](README.md) |
| Group HP | [`group-hp.md`](README.md) |
| Group IZ | [`group-iz.md`](README.md) |
| Group JB | [`group-jb.md`](README.md) |
| Group JG | [`group-jg.md`](README.md) |
| Group NG | [`group-ng.md`](README.md) |
| Group NO | [`group-no.md`](README.md) |
| Group NP | [`group-np.md`](README.md) |
| Group NQ | [`group-nq.md`](README.md) |
| Group OI | [`group-oi.md`](README.md) |
| Group OJ | [`group-oj.md`](README.md) |
| Group OK | [`group-ok.md`](README.md) |
| Group OL | [`group-ol.md`](README.md) |
| Group OM | [`group-om.md`](README.md) |
| Group ON | [`group-on.md`](README.md) |
| Group OY | [`group-oy.md`](README.md) |

How many rows each file holds is derived, not typed: `grep -c '^### Row' docs/needs-checking-archive/*.md`

**Done checks are removed, not kept (2026-11-24, the owner's request).** On that day every row here
was deleted: each was a check already done, and no open GitHub issue read them. Its struck line
stays in the register, linking to this page. A removed row's full text is in git history:
`git log -S '### Row E11' --oneline -- docs/needs-checking-archive/`, then `git show` the commit
before it.
