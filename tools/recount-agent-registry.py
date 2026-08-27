# -*- coding: utf-8 -*-
"""Recompute every stated count in the agent registry from its own rows."""
import io, re, sys, collections

def w(s):
    sys.stdout.write(s.encode('ascii', 'replace').decode('ascii'))

P = 'docs/28-agent-registry.md'
lines = io.open(P, encoding='utf-8').read().split('\n')

# --- pass 1: measure ---
dept_order, counts, cur = [], collections.OrderedDict(), None
for l in lines:
    m = re.match(r'^## ([0-9a-z]+)\. (.+?) — (\d+)', l)
    if m:
        cur = m.group(2).strip()
        if cur not in counts:
            dept_order.append(cur)
            counts[cur] = collections.Counter()
        continue
    if l.startswith('| `HERON-') and cur:
        counts[cur][l.split('|')[4].strip()] += 1

tot = collections.Counter()
for d in dept_order:
    tot.update(counts[d])
T1, T2, T3 = tot['T1'], tot['T2'], tot['T3']
TOTAL = T1 + T2 + T3

# --- pass 2: rewrite headings ---
out = []
for l in lines:
    m = re.match(r'^(## [0-9a-z]+\. )(.+?)( — )(\d+)(.*)$', l)
    if m and m.group(2).strip() in counts:
        n = sum(counts[m.group(2).strip()].values())
        l = '%s%s%s%d%s' % (m.group(1), m.group(2), m.group(3), n, m.group(5))
    out.append(l)
s = '\n'.join(out)

# --- pass 3: header totals ---
s = re.sub(r'\*\*Totals: \d+ agents · \d+ T1 · \d+ T2 · \d+ T3\.\*\*',
           '**Totals: %d agents · %d T1 · %d T2 · %d T3.**' % (TOTAL, T1, T2, T3), s, count=1)

# --- pass 4: summary table, rebuilt from the measurements ---
rows = []
for d in dept_order:
    c = counts[d]
    n = sum(c.values())
    bold = d in ('Reporting & Output', 'Skill Lifecycle', 'User & Personalization', 'Learning & Self-Growth')
    nm = '**%s**' % d if bold else d
    ct = '**%d**' % n if bold else str(n)
    rows.append('| %s | %s | %d | %d | %d |' % (nm, ct, c['T1'], c['T2'], c['T3']))
rows.append('| **Total** | **%d** | **%d** | **%d** | **%d** |' % (TOTAL, T1, T2, T3))
new_table = '| Department | Agents | T1 | T2 | T3 |\n|---|---|---|---|---|\n' + '\n'.join(rows)

m = re.search(r'\| Department \| Agents \| T1 \| T2 \| T3 \|\n\|[-| ]+\|\n(?:\|.*\|\n)+', s)
if m:
    s = s[:m.start()] + new_table + '\n' + s[m.end():]
    w("  summary table rebuilt (%d departments)\n" % len(dept_order))
else:
    w("  MISS summary table\n")

# --- pass 5: prose figures ---
s = re.sub(r'\*\*\d+ of \d+ agents never call a model\*\*',
           '**%d of %d agents never call a model**' % (T1, TOTAL), s)
s = re.sub(r'a normal application with about \d+ services, \d+ narrow model calls, and\n\d+ genuine agentic workflows',
           'a normal application with about %d services, %d narrow model calls, and\n%d genuine agentic workflows' % (T1, T2, T3), s)
s = re.sub(r'Of the rest, \d+ make one scoped call and \d+ run a real agentic loop\.',
           'Of the rest, %d make one scoped call and %d run a real agentic loop.' % (T2, T3), s)
s = re.sub(r'(\| \*\*Worker\*\* \| \*\*T1\*\* — deterministic service \| )\d+( \|)', r'\g<1>%d\g<2>' % T1, s)
s = re.sub(r'(\| \*\*Pro / skilled\*\* \| \*\*T2\*\* — one scoped model call \| )\d+( \|)', r'\g<1>%d\g<2>' % T2, s)
s = re.sub(r'(\| \*\*Senior / lead\*\* \| \*\*T3\*\* — agentic loop \| )\d+( \|)', r'\g<1>%d\g<2>' % T3, s)
s = s.replace('across 244 agents', 'across %d agents' % TOTAL)

io.open(P, 'w', encoding='utf-8').write(s)
w("  TOTAL=%d  T1=%d T2=%d T3=%d  departments=%d\n" % (TOTAL, T1, T2, T3, len(dept_order)))

# --- verify ---
chk = io.open(P, encoding='utf-8').read()
bad, cur, n = [], None, 0
for l in chk.split('\n'):
    m = re.match(r'^## [0-9a-z]+\. (.+?) — (\d+)', l)
    if m:
        if cur and n != cur[1]:
            bad.append('%s %d!=%d' % (cur[0], cur[1], n))
        cur = (m.group(1), int(m.group(2))); n = 0
    elif l.startswith('| `HERON-') and cur:
        n += 1
if cur and n != cur[1]:
    bad.append('%s %d!=%d' % (cur[0], cur[1], n))
w("  verify: %s\n" % (bad or 'all departments reconcile'))
