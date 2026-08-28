# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-DOC-VAL-009
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

import io, os, re, sys

root = '.'
md = []
for dp, dn, fn in os.walk(root):
    if '.git' in dp:
        continue
    for f in fn:
        if f.endswith('.md'):
            md.append(os.path.join(dp, f).replace(os.sep, '/'))

# The docs carry characters the Windows console codepage cannot encode.
try:
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
    out = sys.stdout.write
except AttributeError:
    out = lambda s: sys.stdout.write(s.encode('ascii', 'replace').decode('ascii'))
out("=== FILES ===\n")
out("markdown files: %d\n\n" % len(md))

# ---------- 1. link integrity ----------
link = re.compile(r'\[([^\]]*)\]\(([^)]+)\)')
bad = []
for p in md:
    s = io.open(p, encoding='utf-8').read()
    base = os.path.dirname(p)
    for text, href in link.findall(s):
        if href.startswith(('http://', 'https://', '#', 'mailto:')):
            continue
        tgt = href.split('#')[0]
        if not tgt:
            continue
        full = os.path.normpath(os.path.join(base, tgt)).replace(os.sep, '/')
        if not os.path.exists(full):
            bad.append((p, href, text[:35]))

out("=== 1. BROKEN LOCAL LINKS: %d ===\n" % len(bad))
for p, h, t in bad:
    out("  %-45s -> %-40s [%s]\n" % (p, h, t))
out("\n")

# ---------- 2. reference targets exist ----------
allsrc = {}
for p in md:
    allsrc[p] = io.open(p, encoding='utf-8').read()
joined = "\n".join(allsrc.values())

def defined(pattern, path):
    s = allsrc.get(path, '')
    return set(re.findall(pattern, s))

# Golden rules defined in 14
gr_def = set(int(x) for x in re.findall(r'^### (\d+)\.', allsrc.get('./docs/14-golden-rules.md', ''), re.M))
gr_ref = set(int(x) for x in re.findall(r'Golden Rule[s]? (\d+)', joined))
out("=== 2. GOLDEN RULES ===\n")
out("  defined in doc 14: %s\n" % sorted(gr_def))
out("  referenced anywhere: %s\n" % sorted(gr_ref))
out("  REFERENCED BUT NOT DEFINED: %s\n\n" % sorted(gr_ref - gr_def))

# Decisions defined in DECISIONS.md
d_def = set(re.findall(r'^## (D-\d+)', allsrc.get('./docs/DECISIONS.md', ''), re.M))
d_ref = set(re.findall(r'\b(D-\d\d)\b', joined))
out("=== 3. DECISIONS ===\n")
out("  defined: %s\n" % sorted(d_def))
out("  REFERENCED BUT NOT DEFINED: %s\n\n" % sorted(d_ref - d_def))

# Questions defined in OPEN-QUESTIONS.md
q_def = set(re.findall(r'### (?:[^\n]*?)(Q-\d+[a-z]?)', allsrc.get('./docs/OPEN-QUESTIONS.md', '')))
q_ref = set(re.findall(r'\b(Q-\d+[a-z]?)\b', joined))
out("=== 4. QUESTIONS ===\n")
out("  defined: %s\n" % sorted(q_def))
out("  REFERENCED BUT NOT DEFINED: %s\n\n" % sorted(q_ref - q_def))

# ---------- 5. count claims ----------
out("=== 5. COUNT CLAIMS (verify by hand) ===\n")
for p in ['./README.md', './docs/README.md']:
    s = allsrc.get(p, '')
    for m in re.finditer(r'[^\n]*(?:answered|proposed|Articles|official rules)[^\n]*', s):
        line = m.group(0).strip()
        if len(line) < 220:
            out("  %s | %s\n" % (p, line))
out("\n")

# actual counts
q_answered = len(re.findall(r'### \S+ (Q-\d+[a-z]?)', allsrc.get('./docs/OPEN-QUESTIONS.md', '')))
answered_section = allsrc.get('./docs/OPEN-QUESTIONS.md', '').split('## Answered')
n_answered = len(re.findall(r'### ', answered_section[1])) if len(answered_section) > 1 else 0
articles = len(re.findall(r'^\*\*(\d+[a-c]?)\.', allsrc.get('./HERON_CONSTITUTION.md', ''), re.M))
out("  ACTUAL: questions defined=%d, in Answered section=%d, constitution articles=%d\n"
    % (len(q_def), n_answered, articles))
# All 21 are official since 2026-08-28 (Q-19). This line used to print
# "official 1-15, proposed [...]" and would have become the stale claim itself -
# a checker that reports a superseded split is worse than one that reports
# nothing, because it is believed.
out("  ACTUAL: golden rules defined=%d (all official; 16-21 accepted 2026-08-28)\n"
    % len(gr_def))

# ---------- 6. the Progress line, derived rather than believed ----------
#
# Section 5 above prints count claims and says "verify by hand". Nobody does,
# which is how the Progress line at the top of OPEN-QUESTIONS.md came to say
# "14 answered - 26 open" while the file held 20 and 20. Six questions had been
# answered and the sentence stayed still.
#
# So this one is derived and compared, and it is the ONLY part of this script
# that can fail. Everything above is a report; a report cannot be wrong, which
# is also why it cannot catch anything.
oq = allsrc.get('./docs/OPEN-QUESTIONS.md', '')
answered = open_ids = None
if oq:
    answered, open_ids = 0, []
    for block in re.split(r'\n(?=### )', oq):
        head = re.match(r'### (.*?)(Q-\d+[a-z]?)\s*—', block)
        if not head:
            continue
        if '✅' in head.group(1):
            answered += 1
            continue
        # An answered question has real text after its **Answer:** marker; an
        # open one has the bare placeholder and nothing before the block ends.
        mark = re.search(r'\*\*Answer:\*?\*?', block)
        body = re.split(r'\n---', block[mark.end():])[0].strip() if mark else ''
        if len(body) > 5:
            answered += 1
        else:
            open_ids.append(head.group(2))

out("\n=== 6. THE PROGRESS LINE ===\n")
failed = False
if answered is None:
    out("  OPEN-QUESTIONS.md not found - nothing to check\n")
else:
    out("  ACTUAL: %d answered, %d open\n" % (answered, len(open_ids)))
    out("  still open: %s\n" % ", ".join(open_ids))
    stated = re.search(r'\*\*Progress:\s*(\d+)\s*answered\s*·\s*(\d+)\s*open', oq)
    if not stated:
        out("  DRIFT: no '**Progress: N answered - M open' line to check against.\n")
        failed = True
    elif (int(stated.group(1)), int(stated.group(2))) != (answered, len(open_ids)):
        out("  DRIFT: the file says %s answered / %s open. The questions say %d / %d.\n"
            % (stated.group(1), stated.group(2), answered, len(open_ids)))
        out("  A stated count is a claim; a derived count is a fact. Fix the line.\n")
        failed = True
    else:
        out("  agrees with the stated Progress line\n")

sys.exit(1 if failed else 0)
