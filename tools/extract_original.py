#!/usr/bin/env python3
"""Extracts the alien graphics, movement paths and formations from the original
tape image of The Ultra (PSS, 1983) and writes Ultra.Core/OriginalData.cs.

Usage:  python3 tools/extract_original.py "path/to/Ultra.tap"

How the original works (found by disassembling the 6502 code):
  * $06C0 holds an 8-byte descriptor per sheet (16 sheets):
      graphics ptr, path ptr, start-list ptr, path length, flags.
  * Graphics: 3 animation frames of two redefined characters (12x8 pixels),
    played 0,1,2,1 each time the aliens step.
  * Path: one direction code per step. Bits 0-1 select the screen offset
    (1, 41, 40, 39 bytes = left/right, diagonal, vertical, other diagonal) and
    bit 2 selects add (right/down) or subtract (left/up).
  * Start list: (screen address lo, hi, path index) per alien, ended by hi == $FF.
  * Aliens step one text cell at a time, wrapping across the 37 playable columns
    and from the bottom of the screen back to row 1.
  * Flag bit 7 doubles the stepping rate (sheet 7).

The remake draws the aliens at 1.5x, so formations are re-spaced: aliens that
share a track have their gaps along the path widened by 1.5x, and fixed
formations are scaled about their centre, whichever keeps more aliens. Any alien
that would still overlap another (where the original pair did not) is dropped.
"""
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DIRS = {0: (-1, 0), 1: (-1, -1), 2: (0, -1), 3: (1, -1), 4: (1, 0), 5: (1, 1), 6: (0, 1), 7: (-1, 1)}
SCALE = 1.5


def parse_tap(path):
    d = open(path, 'rb').read()
    i = 0
    while d[i] == 0x16:
        i += 1
    i += 1                                   # $24 sync marker
    h = d[i:i + 9]
    i += 9
    end, start = h[4] << 8 | h[5], h[6] << 8 | h[7]
    while d[i] != 0:
        i += 1
    i += 1
    return start, d[i:i + end - start + 1]


def extract(path):
    start, data = parse_tap(path)
    m = lambda a: data[a - start]
    w = lambda a: m(a) | m(a + 1) << 8

    def char(a):
        return [''.join('X' if (m(a + r) & 0x3F) & (0x20 >> i) else '.' for i in range(6)) for r in range(8)]

    sheets = []
    for n in range(16):
        b = 0x06C0 + n * 8
        gfx, path_ptr, st, length, flags = w(b), w(b + 2), w(b + 4), m(b + 6), m(b + 7)
        frames = []
        for f in range(3):
            left, right = char(gfx + f * 16), char(gfx + f * 16 + 8)
            frames.append([left[r] + right[r] for r in range(8)])
        starts, k = [], 0
        while m(st + k + 1) != 0xFF:
            off = (m(st + k) | m(st + k + 1) << 8) - 0xBB80
            starts.append((off // 40, off % 40, m(st + k + 2)))
            k += 3
        sheets.append(dict(frames=frames, path=[m(path_ptr + i) for i in range(length)],
                           starts=starts, interval=0.075 if flags & 0x80 else 0.15))

    a, c, e, b, d_, f = (char(0x0600 + k * 8) for k in (0, 2, 4, 1, 3, 5))
    ship = [a[r] + c[r] + e[r] for r in range(8)] + [b[r] + d_[r] + f[r] for r in range(8)]
    return sheets, ship, char(0x0630), char(0x0638)


# ---------------------------------------------------------------- re-spacing

def wrap(c, r):
    if c < 2: c += 37
    if c > 38: c -= 37
    if r > 27: r -= 27
    if r < 1: r += 27
    return c, r


def step(s, c, r, i):
    dc, dr = DIRS[s['path'][i % len(s['path'])]]
    c, r = wrap(c + dc, r + dr)
    return c, r, (i + 1) % len(s['path'])


def track(s, c, r, i, n):
    out = []
    for _ in range(n):
        out.append((c, r))
        c, r, i = step(s, c, r, i)
    return out


def bounds(s):
    pts = [(x, y) for f in s['frames'] for y, row in enumerate(f) for x, ch in enumerate(row) if ch == 'X']
    return max(p[0] for p in pts) - min(p[0] for p in pts) + 1, max(p[1] for p in pts) - min(p[1] for p in pts) + 1


def drop_conflicts(s, placed, reference):
    """Keep aliens that never overlap another at 1.5x unless the original pair also overlapped."""
    L, (bw, bh) = len(s['path']), bounds(s)
    trs = [track(s, c, r, i, L) for (r, c, i) in placed]
    ref = [track(s, c, r, i, L) for (r, c, i) in reference]
    keep = []
    for n, t in enumerate(trs):
        ok = True
        for k in keep:
            for (c1, r1), (c2, r2), (o1, p1), (o2, p2) in zip(t, trs[k], ref[n], ref[k]):
                if abs(c1 - c2) * 6 < bw * SCALE and abs(r1 - r2) * 8 < bh * SCALE and \
                        not (abs(o1 - o2) * 6 < bw and abs(p1 - p2) * 8 < bh):
                    ok = False
                    break
            if not ok:
                break
        if ok:
            keep.append(n)
    return [placed[k] for k in keep]


def track_spacing(s):
    L, starts = len(s['path']), s['starts']
    tracks = [track(s, c, r, i, 2 * L) for (r, c, i) in starts]
    group = [-1] * len(starts)
    groups = []
    for j in range(len(starts)):
        if group[j] >= 0:
            continue
        g = [(j, 0)]
        group[j] = len(groups)
        for k in range(j + 1, len(starts)):
            if group[k] < 0:
                for sh in range(L):
                    if tracks[k][:L] == tracks[j][sh:sh + L]:
                        g.append((k, sh))
                        group[k] = len(groups)
                        break
        groups.append(g)
    placed, reference = [], []
    for g in groups:
        if len(g) == 1 or L <= 1:
            placed += [starts[k] for k, _ in g]
            reference += [starts[k] for k, _ in g]
            continue
        g.sort(key=lambda t: t[1])
        r0, c0, i0 = starts[g[0][0]]
        prev = None
        for k, sh in g:
            ns = round(sh * SCALE)
            if prev is not None and ns <= prev:
                ns = prev + 1
            prev = ns
            if ns >= L:
                break
            c, r, i = c0, r0, i0
            for _ in range(ns):
                c, r, i = step(s, c, r, i)
            placed.append((r, c, i))
            reference.append(starts[k])
    return drop_conflicts(s, placed, reference)


def formation_scaling(s):
    st = s['starts']
    cr = sum(r for r, _, _ in st) / len(st)
    cc = sum(c for _, c, _ in st) / len(st)
    out = [(round(cr + (r - cr) * SCALE), round(cc + (c - cc) * SCALE), i) for r, c, i in st]
    minc, maxc = min(c for _, c, _ in out), max(c for _, c, _ in out)
    minr, maxr = min(r for r, _, _ in out), max(r for r, _, _ in out)
    dc = 2 - minc if minc < 2 else (38 - maxc if maxc > 38 else 0)
    dr = 1 - minr if minr < 1 else (24 - maxr if maxr > 24 else 0)
    out = [(r + dr, c + dc, i) for r, c, i in out]
    if min(c for _, c, _ in out) < 2 or max(c for _, c, _ in out) > 38 or \
            min(r for r, _, _ in out) < 1 or max(r for r, _, _ in out) > 24:
        return []
    return drop_conflicts(s, out, st)


def respace(s):
    a, b = track_spacing(s), formation_scaling(s)
    return b if len(b) > len(a) else a


# ---------------------------------------------------------------- output

def write_cs(sheets, ship, shot, bomb, path):
    L = ['// Generated from the original tape image of The Ultra (PSS, 1983) by tools/extract_original.py.',
         '// Do not edit by hand.', 'namespace Ultra.Core;', '', '/// <summary>',
         '/// Alien graphics, movement paths and starting formations of the 16 sheets in the original',
         '/// Oric game. Positions are in Oric text cells (40x28, 6x8 pixels); aliens are two cells wide.',
         '/// Starts were re-spaced for the 1.5x sprites so aliens do not overlap.', '/// </summary>',
         'public static class OriginalData', '{',
         '    /// <summary>[sheet][frame] -> 8 rows of 12 pixels. Frames play 0,1,2,1.</summary>',
         '    public static readonly string[][][] AlienFrames =', '    {']
    for s in sheets:
        L += ['        new[]', '        {']
        L += ['            new[] { ' + ', '.join(f'"{r}"' for r in fr) + ' },' for fr in s['frames']]
        L += ['        },']
    L += ['    };', '', '    /// <summary>Direction codes per step: 0 L, 1 UL, 2 U, 3 UR, 4 R, 5 DR, 6 D, 7 DL.</summary>',
          '    public static readonly byte[][] Paths =', '    {']
    L += ['        new byte[] { ' + ', '.join(str(x) for x in s['path']) + ' },' for s in sheets]
    L += ['    };', '', '    /// <summary>Starting (row, column, path index) of each alien.</summary>',
          '    public static readonly (int Row, int Col, int Index)[][] Starts =', '    {']
    L += ['        new[] { ' + ', '.join(f'({r}, {c}, {i})' for r, c, i in s['respaced']) + ' },' for s in sheets]
    L += ['    };', '', '    /// <summary>Original alien count per sheet, before re-spacing.</summary>',
          '    public static readonly int[] OriginalCounts = { ' + ', '.join(str(len(s['starts'])) for s in sheets) + ' };',
          '', '    /// <summary>Seconds between movement steps (measured on the original).</summary>',
          '    public static readonly float[] StepIntervals = { ' + ', '.join(f"{s['interval']}f" for s in sheets) + ' };',
          '', "    /// <summary>The player's ship: 3x2 text cells (18x16 pixels).</summary>",
          '    public static readonly string[] PlayerShip =', '    {']
    L += [f'        "{r}",' for r in ship]
    L += ['    };', '', '    public static readonly string[] Shot = { ' + ', '.join(f'"{r}"' for r in shot) + ' };',
          '    public static readonly string[] Bomb = { ' + ', '.join(f'"{r}"' for r in bomb) + ' };', '}']
    open(path, 'w').write('\n'.join(L) + '\n')


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__)
    sheets, ship, shot, bomb = extract(sys.argv[1])
    for n, s in enumerate(sheets):
        s['respaced'] = respace(s)
        print(f"sheet {n + 1:2d}: {len(s['starts']):2d} aliens -> {len(s['respaced']):2d} at 1.5x")
    out = os.path.join(ROOT, 'Ultra.Core', 'OriginalData.cs')
    write_cs(sheets, ship, shot, bomb, out)
    print('wrote', os.path.relpath(out, ROOT))


if __name__ == '__main__':
    main()
