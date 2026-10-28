"""Split a dense GLB into chunks by X ranges of triangle centres (glTF coordinates), with little memory.

Usage (Blender's Python, it has numpy): python -I glb_split.py IN.glb OUT_DIR NAME:XMIN:XMAX [NAME:XMIN:XMAX ...]
Every range becomes OUT_DIR/NAME.glb: indexed POSITION + TEXCOORD_0, the original material and images.
Triangles outside every range (scale silhouettes) are dropped. Normals are not kept: Blender recomputes them.
"""
import json
import struct
import sys

import numpy as np

DTYPES = {5126: np.float32, 5125: np.uint32, 5123: np.uint16, 5121: np.uint8}
WIDTH = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4}


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
    dt = np.dtype(DTYPES[a['componentType']])
    w = WIDTH[a['type']]
    start = bv.get('byteOffset', 0) + a.get('byteOffset', 0)
    stride = bv.get('byteStride', dt.itemsize * w)
    if stride == dt.itemsize * w:
        arr = np.frombuffer(binc, dtype=dt, count=a['count'] * w, offset=start)
    else:
        raw = np.frombuffer(binc, dtype=np.uint8, count=a['count'] * stride, offset=start)
        arr = raw.reshape(a['count'], stride)[:, :dt.itemsize * w].copy().view(dt)
    return arr.reshape(a['count'], w) if w > 1 else arr


def write_glb(path, g, binc, prim, pos, uv, tris):
    blobs = [pos.astype(np.float32).tobytes(), uv.astype(np.float32).tobytes(), tris.astype(np.uint32).tobytes()]
    views = [{'buffer': 0, 'byteLength': len(blobs[0]), 'target': 34962},
             {'buffer': 0, 'byteLength': len(blobs[1]), 'target': 34962},
             {'buffer': 0, 'byteLength': len(blobs[2]), 'target': 34963}]
    images = []
    for im in g.get('images', []):
        bv = g['bufferViews'][im['bufferView']]
        s = bv.get('byteOffset', 0)
        blobs.append(binc[s:s + bv['byteLength']])
        views.append({'buffer': 0, 'byteLength': bv['byteLength']})
        images.append({k: v for k, v in im.items() if k != 'bufferView'} | {'bufferView': len(views) - 1})
    out, offset = bytearray(), 0
    for blob, view in zip(blobs, views):
        pad = (-offset) % 4
        out += b'\x00' * pad
        offset += pad
        view['byteOffset'] = offset
        out += blob
        offset += len(blob)
    out += b'\x00' * ((-len(out)) % 4)
    gj = {
        'asset': {'version': '2.0', 'generator': 'OLAM glb_split.py'},
        'scene': 0, 'scenes': [{'nodes': [0]}], 'nodes': [{'mesh': 0}],
        'meshes': [{'primitives': [{'attributes': {'POSITION': 0, 'TEXCOORD_0': 1}, 'indices': 2,
                                    'material': prim.get('material', 0)}]}],
        'accessors': [
            {'bufferView': 0, 'componentType': 5126, 'count': len(pos), 'type': 'VEC3',
             'min': pos.min(axis=0).tolist(), 'max': pos.max(axis=0).tolist()},
            {'bufferView': 1, 'componentType': 5126, 'count': len(uv), 'type': 'VEC2'},
            {'bufferView': 2, 'componentType': 5125, 'count': tris.size, 'type': 'SCALAR'},
        ],
        'bufferViews': views, 'buffers': [{'byteLength': len(out)}],
        'materials': g.get('materials', []), 'textures': g.get('textures', []),
        'samplers': g.get('samplers', []), 'images': images,
    }
    js = json.dumps(gj, separators=(',', ':')).encode('utf-8')
    js += b' ' * ((-len(js)) % 4)
    with open(path, 'wb') as f:
        f.write(struct.pack('<4sII', b'glTF', 2, 12 + 8 + len(js) + 8 + len(out)))
        f.write(struct.pack('<I4s', len(js), b'JSON') + js)
        f.write(struct.pack('<I4s', len(out), b'BIN\x00') + bytes(out))


def main(src, out_dir, specs):
    g, binc = read_glb(src)
    prim = g['meshes'][0]['primitives'][0]
    pos = accessor(g, binc, prim['attributes']['POSITION'])
    uv = accessor(g, binc, prim['attributes']['TEXCOORD_0'])
    tris = accessor(g, binc, prim['indices']).astype(np.int64).reshape(-1, 3)
    px = pos[:, 0].astype(np.float64)
    cx = (px[tris[:, 0]] + px[tris[:, 1]] + px[tris[:, 2]]) / 3
    for spec in specs:
        name, lo, hi = spec.split(':')
        mask = (cx >= float(lo)) & (cx < float(hi))
        sub = tris[mask]
        verts, inverse = np.unique(sub.ravel(), return_inverse=True)
        write_glb(f'{out_dir}/{name}.glb', g, binc, prim, pos[verts], uv[verts], inverse.reshape(-1, 3))
        print(f'{name}: {len(sub)} tris, {len(verts)} vertices')
    print(f'dropped: {len(tris) - sum(int(((cx >= float(s.split(":")[1])) & (cx < float(s.split(":")[2]))).sum()) for s in specs)} tris')


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], sys.argv[3:])
