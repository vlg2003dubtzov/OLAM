"""Split each GLB mesh into connected parts (vertices welded by position) and print their sizes.

Usage: python -I glb_parts.py file.glb ...
Reads the files as data only. Sizes are in model units (glTF: metres, Y up).
"""
import json
import os
import struct
import sys
from array import array

COMP = {5121: ('B', 1), 5123: ('H', 2), 5125: ('I', 4)}


def read_glb(path):
    with open(path, 'rb') as f:
        data = f.read()
    off, js, binc = 12, None, b''
    while off + 8 <= len(data):
        clen, ctype = struct.unpack('<I4s', data[off:off + 8])
        if ctype == b'JSON':
            js = json.loads(data[off + 8:off + 8 + clen].decode('utf-8'))
        elif ctype == b'BIN\x00':
            binc = data[off + 8:off + 8 + clen]
        off += 8 + clen
    return js, binc


def accessor(g, binc, i):
    a = g['accessors'][i]
    bv = g['bufferViews'][a['bufferView']]
    start = bv.get('byteOffset', 0) + a.get('byteOffset', 0)
    if a['componentType'] == 5126:
        width = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4}[a['type']]
        stride = bv.get('byteStride', 4 * width)
        out = array('f')
        if stride == 4 * width:
            out.frombytes(binc[start:start + a['count'] * stride])
        else:
            for k in range(a['count']):
                out.frombytes(binc[start + k * stride:start + k * stride + 4 * width])
        return out
    code, size = COMP[a['componentType']]
    out = array(code)
    out.frombytes(binc[start:start + a['count'] * size])
    return out


def parts(path):
    g, binc = read_glb(path)
    prim = g['meshes'][0]['primitives'][0]
    pos = accessor(g, binc, prim['attributes']['POSITION'])
    idx = accessor(g, binc, prim['indices'])
    n = len(pos) // 3
    weld, key_to_id = [0] * n, {}
    for v in range(n):
        key = (round(pos[3 * v] * 2000), round(pos[3 * v + 1] * 2000), round(pos[3 * v + 2] * 2000))
        weld[v] = key_to_id.setdefault(key, len(key_to_id))
    parent = list(range(len(key_to_id)))

    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a

    for t in range(0, len(idx), 3):
        a, b, c = find(weld[idx[t]]), find(weld[idx[t + 1]]), find(weld[idx[t + 2]])
        if a != b:
            parent[b] = a
        c = find(c)
        if c != a:
            parent[c] = a
    stats = {}
    for t in range(0, len(idx), 3):
        r = find(weld[idx[t]])
        s = stats.get(r)
        if s is None:
            s = stats[r] = [0, [1e9] * 3, [-1e9] * 3]
        s[0] += 1
        for corner in idx[t:t + 3]:
            for k in range(3):
                x = pos[3 * corner + k]
                if x < s[1][k]:
                    s[1][k] = x
                if x > s[2][k]:
                    s[2][k] = x
    rows = sorted(stats.values(), key=lambda s: -s[0])
    return rows, len(idx) // 3


for p in sys.argv[1:]:
    rows, total = parts(p)
    big = [r for r in rows if r[0] >= 200]
    print(f'== {os.path.basename(p)}: {total} tris, {len(rows)} parts, {len(big)} with >=200 tris')
    for tris, lo, hi in big[:14]:
        size = [hi[k] - lo[k] for k in range(3)]
        centre = [(hi[k] + lo[k]) / 2 for k in range(3)]
        print(f'   {tris:7d} tris  size x{size[0]:.3f} y{size[1]:.3f} z{size[2]:.3f}'
              f'  centre ({centre[0]:+.2f},{centre[1]:+.2f},{centre[2]:+.2f})  floor y{lo[1]:+.3f}')
    small = sum(r[0] for r in rows if r[0] < 200)
    print(f'   small debris: {small} tris in {len(rows) - len(big)} parts')
