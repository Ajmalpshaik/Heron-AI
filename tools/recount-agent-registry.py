# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-AGT-002
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""Recompute every stated count in the agent registry from its own rows.

    python tools/recount-agent-registry.py

AGENTS.md's second Never is why this exists: "never type a number a command
can derive". This is that command for docs/28-agent-registry.md.

EVERY, AND IT IS CHECKED. Two things were measured on 2026-09-22 by running
the version before this one:

  * it left `across 249 agents` in a registry it had just counted to 250.
    That one substitution of the seven was a LITERAL - `.replace('across 244
    agents', ...)` - so it worked exactly once, and the figure has been
    frozen ever since 244 stopped being the total. A tool that exists to
    stop numbers going stale had a stale number in it;

  * given a registry whose table had gained one column, it wrote `Totals: 0
    agents`, a summary table of zeros, printed `verify: all departments
    reconcile`, and exited 0 - over a document still listing 250 agents. The
    tier is read by column position, and every row had landed in a bucket
    that was not T1, T2 or T3. D-52, the plausible zero.

So now: a tier that is not a tier is REFUSED before anything is written, the
whole document is built in memory and read back before it replaces the one
on disk, and the exit code follows the reconciliation rather than being 0
whatever happened.
"""
import collections
import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
# ABSOLUTE, NOT RELATIVE. It was 'docs/28-agent-registry.md', which means
# whichever docs/ happened to be beside the working directory.
P = os.path.join(ROOT, 'docs', '28-agent-registry.md')

TIERS = ('T1', 'T2', 'T3')
BOLD = ('Reporting & Output', 'Skill Lifecycle', 'User & Personalization',
        'Learning & Self-Growth')


def w(s):
    sys.stdout.write(s.encode('ascii', 'replace').decode('ascii'))


def measure(lines):
    """(department order, per-department tier counts, rows seen, odd rows)."""
    dept_order, counts, cur = [], collections.OrderedDict(), None
    rows, odd = 0, []
    for n, l in enumerate(lines, 1):
        m = re.match(r'^## ([0-9a-z]+)\. (.+?) — (\d+)', l)
        if m:
            cur = m.group(2).strip()
            if cur not in counts:
                dept_order.append(cur)
                counts[cur] = collections.Counter()
            continue
        if l.startswith('| `HERON-') and cur:
            rows += 1
            tier = l.split('|')[4].strip()
            counts[cur][tier] += 1
            if tier not in TIERS:
                odd.append((n, l.split('|')[1].strip(), tier))
    return dept_order, counts, rows, odd


def rebuild(text):
    """The registry with every stated count recomputed, or a refusal.

    Returns (new text, None) or (None, why it was refused).
    """
    lines = text.split('\n')
    dept_order, counts, rows, odd = measure(lines)

    if not rows:
        return None, ("no agent rows were found at all. An empty registry is "
                      "not a registry with no agents in it")
    if odd:
        detail = "; ".join("line %d %s reads %r" % one for one in odd[:5])
        return None, ("%d row(s) do not carry a tier in the column this reads "
                      "- %s. A registry that counts to nought is the one "
                      "nobody questions, so nothing was written"
                      % (len(odd), detail))

    tot = collections.Counter()
    for d in dept_order:
        tot.update(counts[d])
    T1, T2, T3 = tot['T1'], tot['T2'], tot['T3']
    TOTAL = T1 + T2 + T3
    if TOTAL != rows:
        return None, ("%d rows were read but only %d carry a tier. The two "
                      "must agree before either is published" % (rows, TOTAL))

    # --- department headings ---
    out = []
    for l in lines:
        m = re.match(r'^(## [0-9a-z]+\. )(.+?)( — )(\d+)(.*)$', l)
        if m and m.group(2).strip() in counts:
            n = sum(counts[m.group(2).strip()].values())
            l = '%s%s%s%d%s' % (m.group(1), m.group(2), m.group(3), n,
                                m.group(5))
        out.append(l)
    s = '\n'.join(out)

    # --- header totals ---
    s = re.sub(r'\*\*Totals: \d+ agents · \d+ T1 · \d+ T2 · \d+ T3\.\*\*',
               '**Totals: %d agents · %d T1 · %d T2 · %d T3.**'
               % (TOTAL, T1, T2, T3), s, count=1)

    # --- summary table, rebuilt from the measurements ---
    table_rows = []
    for d in dept_order:
        c = counts[d]
        n = sum(c.values())
        nm = '**%s**' % d if d in BOLD else d
        ct = '**%d**' % n if d in BOLD else str(n)
        table_rows.append('| %s | %s | %d | %d | %d |'
                          % (nm, ct, c['T1'], c['T2'], c['T3']))
    table_rows.append('| **Total** | **%d** | **%d** | **%d** | **%d** |'
                      % (TOTAL, T1, T2, T3))
    new_table = ('| Department | Agents | T1 | T2 | T3 |\n|---|---|---|---|---|\n'
                 + '\n'.join(table_rows))

    m = re.search(r'\| Department \| Agents \| T1 \| T2 \| T3 \|\n\|[-| ]+\|\n'
                  r'(?:\|.*\|\n)+', s)
    if not m:
        return None, ("the summary table was not found, so it could not be "
                      "rebuilt and nothing was written")
    s = s[:m.start()] + new_table + '\n' + s[m.end():]

    # --- prose figures ---
    s = re.sub(r'\*\*\d+ of \d+ agents never call a model\*\*',
               '**%d of %d agents never call a model**' % (T1, TOTAL), s)
    s = re.sub(r'a normal application with about \d+ services, \d+ narrow '
               r'model calls, and\n\d+ genuine agentic workflows',
               'a normal application with about %d services, %d narrow model '
               'calls, and\n%d genuine agentic workflows' % (T1, T2, T3), s)
    s = re.sub(r'Of the rest, \d+ make one scoped call and \d+ run a real '
               r'agentic loop\.',
               'Of the rest, %d make one scoped call and %d run a real '
               'agentic loop.' % (T2, T3), s)
    s = re.sub(r'(\| \*\*Worker\*\* \| \*\*T1\*\* — deterministic service \| )\d+( \|)',
               r'\g<1>%d\g<2>' % T1, s)
    s = re.sub(r'(\| \*\*Pro / skilled\*\* \| \*\*T2\*\* — one scoped model call \| )\d+( \|)',
               r'\g<1>%d\g<2>' % T2, s)
    s = re.sub(r'(\| \*\*Senior / lead\*\* \| \*\*T3\*\* — agentic loop \| )\d+( \|)',
               r'\g<1>%d\g<2>' % T3, s)
    # A REGEX, NOT A LITERAL. This read `.replace('across 244 agents', ...)`,
    # which stopped matching the day the total left 244 and left the figure
    # frozen at a number nobody derived.
    s = re.sub(r'across \d+ agents', 'across %d agents' % TOTAL, s)
    return s, None


def disagreements(text):
    """Every figure in `text` that does not match the rows underneath it."""
    lines = text.split('\n')
    dept_order, counts, rows, odd = measure(lines)
    bad = []
    if odd:
        bad.append('%d row(s) carry no tier' % len(odd))
    tot = collections.Counter()
    for d in dept_order:
        tot.update(counts[d])
    T1, T2, T3 = tot['T1'], tot['T2'], tot['T3']
    TOTAL = T1 + T2 + T3

    for l in lines:
        m = re.match(r'^## [0-9a-z]+\. (.+?) — (\d+)', l)
        if m and sum(counts.get(m.group(1).strip(), {}).values()) != int(m.group(2)):
            bad.append('%s says %s, %d rows'
                       % (m.group(1).strip(), m.group(2),
                          sum(counts.get(m.group(1).strip(), {}).values())))

    head = re.search(r'\*\*Totals: (\d+) agents · (\d+) T1 · (\d+) T2 · (\d+) T3\.\*\*', text)
    if head and tuple(int(g) for g in head.groups()) != (TOTAL, T1, T2, T3):
        bad.append('the totals line says %s' % ', '.join(head.groups()))

    row = re.search(r'^\| \*\*Total\*\* \| \*\*(\d+)\*\* \| \*\*(\d+)\*\* \| '
                    r'\*\*(\d+)\*\* \| \*\*(\d+)\*\* \|', text, re.M)
    if row and tuple(int(g) for g in row.groups()) != (TOTAL, T1, T2, T3):
        bad.append('the summary table says %s' % ', '.join(row.groups()))

    for n in re.findall(r'across (\d+) agents', text):
        if int(n) != TOTAL:
            bad.append('"across %s agents" against a total of %d' % (n, TOTAL))
    return bad


def main():
    if not os.path.isfile(P):
        w("  there is no registry at %s\n" % P)
        return 2
    text = io.open(P, encoding='utf-8').read()

    # BUILT AND CHECKED BEFORE IT IS WRITTEN. The version before this one
    # wrote first and looked afterwards, which is how `Totals: 0 agents`
    # reached a document that lists 250 of them.
    fresh, refused = rebuild(text)
    if refused:
        w("  NOTHING WAS WRITTEN. %s\n" % refused)
        return 1

    bad = disagreements(fresh)
    if bad:
        w("  NOTHING WAS WRITTEN. The rebuilt registry still disagrees with "
          "its own rows: %s\n" % '; '.join(bad))
        return 1

    if fresh == text:
        w("  every stated count already matches its rows. Nothing to change.\n")
        return 0

    io.open(P, 'w', encoding='utf-8', newline='').write(fresh)
    dept_order, counts, rows, _odd = measure(fresh.split('\n'))
    tot = collections.Counter()
    for d in dept_order:
        tot.update(counts[d])
    w("  TOTAL=%d  T1=%d T2=%d T3=%d  departments=%d\n"
      % (rows, tot['T1'], tot['T2'], tot['T3'], len(dept_order)))
    w("  every stated count now matches its rows.\n")
    return 0


if __name__ == '__main__':
    sys.exit(main())
