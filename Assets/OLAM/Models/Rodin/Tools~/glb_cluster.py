"""Coarse pre-decimation of a very dense GLB by vertex clustering, with little memory.

Usage: python -I glb_cluster.py IN.glb OUT.glb TARGET_TRIS
Vertices are snapped to a grid whose cell size is searched so that about TARGET_TRIS triangles
survive. UVs stay per corner, normals are dropped (Blender recomputes them). Material, textures
and images are copied unchanged. Meant to run before rodin_process.py on raw Rodin meshes.
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


def cluster(pos, tris, cell, lo):
    keys = np.floor((pos - lo) / cell).astype(np.int64)
    span = keys.max(axis=0) + 1
    flat = (keys[:, 0] * span[1] + keys[:, 1]) * span[2] + keys[:, 2]
    uniq, inverse = np.unique(flat, return_inverse=True)
    t = inverse[tris]
    keep = (t[:, 0] != t[:, 1]) & (t[:, 1] != t[:, 2]) & (t[:, 0] != t[:, 2])
    return uniq.size, inverse, keep


def main(src, dst, target):
    g, binc = read_glb(src)
    prim = g['meshes'][0]['primitives'][0]
    pos = accessor(g, binc, prim['attributes']['POSITION']).astype(np.float64)
    uv = accessor(g, binc, prim['attributes']['TEXCOORD_0'])
    assert uv.dtype == np.float32, 'only float UVs are supported'
    tris = accessor(g, binc, prim['indices']).astype(np.int64).reshape(-1, 3)
    lo = pos.min(axis=0)
    diag = float(np.linalg.norm(pos.max(axis=0) - lo))

    small, big = diag / 20000, diag / 50
    for _ in range(18):
        cell = (small * big) ** 0.5
        n_clusters, inverse, keep = cluster(pos, tris, cell, lo)
        kept = int(keep.sum())
        if kept > target:
            small = cell
        else:
            big = cell
        if abs(kept - target) < target * 0.05:
            break
    print(f'cell {cell:.5f} of diag {diag:.3f}: {len(tris)} -> {kept} tris, {n_clusters} clusters')

    counts = np.bincount(inverse, minlength=n_clusters).astype(np.float64)
    centres = np.stack([np.bincount(inverse, weights=pos[:, k], minlength=n_clusters) for k in range(3)], 1)
    centres /= counts[:, None]
    t_old = tris[keep]
    new_pos = centres[inverse[t_old]].reshape(-1, 3).astype(np.float32)
    new_uv = uv[t_old].reshape(-1, 2).astype(np.float32)

    blobs = [new_pos.tobytes(), new_uv.tobytes()]
    views = [{'buffer': 0, 'byteLength': len(blobs[0]), 'target': 34962},
             {'buffer': 0, 'byteLength': len(blobs[1]), 'target': 34962}]
    images = []
    for im in g.get('images', []):
        bv = g['bufferViews'][im['bufferView']]
        s = bv.get('byteOffset', 0)
        blobs.append(binc[s:s + bv['byteLength']])
        views.append({'buffer': 0, 'byteLength': bv['byteLength']})
        images.append({k: v for k, v in im.items() if k != 'bufferView'} | {'bufferView': len(views) - 1})
    out_bin, offset = b'', 0
    for blob, view in zip(blobs, views):
        pad = (-offset) % 4
        out_bin += b'\x00' * pad
        offset += pad
        view['byteOffset'] = offset
        out_bin += blob
        offset += len(blob)
    out_bin += b'\x00' * ((-len(out_bin)) % 4)

    gj = {
        'asset': {'version': '2.0', 'generator': 'OLAM glb_cluster.py'},
        'scene': 0, 'scenes': [{'nodes': [0]}], 'nodes': [{'mesh': 0}],
        'meshes': [{'primitives': [{'attributes': {'POSITION': 0, 'TEXCOORD_0': 1},
                                    'material': prim.get('material', 0)}]}],
        'accessors': [
            {'bufferView': 0, 'componentType': 5126, 'count': len(new_pos), 'type': 'VEC3',
             'min': new_pos.min(axis=0).tolist(), 'max': new_pos.max(axis=0).tolist()},
            {'bufferView': 1, 'componentType': 5126, 'count': len(new_uv), 'type': 'VEC2'},
        ],
        'bufferViews': views, 'buffers': [{'byteLength': len(out_bin)}],
        'materials': g.get('materials', []), 'textures': g.get('textures', []),
        'samplers': g.get('samplers', []), 'images': images,
    }
    js = json.dumps(gj, separators=(',', ':')).encode('utf-8')
    js += b' ' * ((-len(js)) % 4)
    with open(dst, 'wb') as f:
        f.write(struct.pack('<4sII', b'glTF', 2, 12 + 8 + len(js) + 8 + len(out_bin)))
        f.write(struct.pack('<I4s', len(js), b'JSON') + js)
        f.write(struct.pack('<I4s', len(out_bin), b'BIN\x00') + out_bin)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2], int(sys.argv[3]))
