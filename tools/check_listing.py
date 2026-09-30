#!/usr/bin/env python3
"""Checks every field in store/LISTING.md marked "(N max)" against its character limit."""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
text = open(os.path.join(ROOT, 'store', 'LISTING.md'), encoding='utf-8').read()
ok = True
for m in re.finditer(r'\*\*([^*\n]+?)\*\* \((\d+) max[^)]*\)\s*```\n(.*?)\n```', text, re.S):
    name, limit, body = m.group(1), int(m.group(2)), m.group(3)
    n = len(body)
    status = 'OK ' if n <= limit else 'TOO LONG'
    ok &= n <= limit
    print(f'{status} {name:<20} {n:5d} / {limit}')
sys.exit(0 if ok else 1)
