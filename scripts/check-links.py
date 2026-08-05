import re, os, glob, sys
bad = []
files = glob.glob('docs/**/*.md', recursive=True) + ['README.md', 'README.ko.md', 'CLAUDE.md', 'AGENTS.md']
for f in files:
    if not os.path.exists(f):
        continue
    d = os.path.dirname(f)
    for m in re.finditer(r'\]\(([^)]+)\)', open(f, encoding='utf-8').read()):
        t = m.group(1).split('#')[0].strip()
        if not t or t.startswith(('http://', 'https://', 'mailto:')):
            continue
        if not os.path.exists(os.path.normpath(os.path.join(d, t))):
            bad.append((f, t))
print(f'broken relative links = {len(bad)}')
for f, t in bad:
    print(' ', f, '->', t)
sys.exit(1 if bad else 0)
