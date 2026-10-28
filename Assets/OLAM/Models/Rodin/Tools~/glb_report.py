"""Read GLB files as data and print their structure: meshes, triangles, size, materials, textures.

Usage: python -I glb_report.py [--dump DIR] file.glb ...
--dump writes each model's base colour texture to DIR for a quick look.
"""
import json
import os
import struct
import sys

SOF = {0xC0, 0xC1, 0xC2, 0xC3, 0xC5, 0xC6, 0xC7, 0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF}


def img_dims(b):
    if b[:8] == b'\x89PNG\r\n\x1a\n':
        return list(struct.unpack('>II', b[16:24]))
    if b[:2] == b'\xff\xd8':
        i, n = 2, len(b)
        while i + 9 < n:
            if b[i] != 0xFF:
                i += 1
                continue
            m = b[i + 1]
            if m == 0xFF:
                i += 1
                continue
            if m in (0xD8, 0x01) or 0xD0 <= m <= 0xD7:
                i += 2
                continue
            seg = struct.unpack('>H', b[i + 2:i + 4])[0]
            if m in SOF:
                h, w = struct.unpack('>HH', b[i + 5:i + 9])
                return [w, h]
            i += 2 + seg
    if b[:4] == b'RIFF' and b[8:12] == b'WEBP':
        fmt = b[12:16]
        if fmt == b'VP8X':
            return [1 + int.from_bytes(b[24:27], 'little'), 1 + int.from_bytes(b[27:30], 'little')]
        if fmt == b'VP8 ':
            w, h = struct.unpack('<HH', b[26:30])
            return [w & 0x3FFF, h & 0x3FFF]
        if fmt == b'VP8L':
            bits = int.from_bytes(b[21:25], 'little')
            return [(bits & 0x3FFF) + 1, ((bits >> 14) & 0x3FFF) + 1]
    return None


def read_glb(path):
    with open(path, 'rb') as f:
        data = f.read()
    magic, _ver, _length = struct.unpack('<4sII', data[:12])
    if magic != b'glTF':
        raise ValueError('not a GLB file')
    off, js, binc = 12, None, b''
    while off + 8 <= len(data):
        clen, ctype = struct.unpack('<I4s', data[off:off + 8])
        chunk = data[off + 8:off + 8 + clen]
        if ctype == b'JSON':
            js = json.loads(chunk.decode('utf-8'))
        elif ctype == b'BIN\x00':
            binc = chunk
        off += 8 + clen
    return js, binc


def report(path, dump_dir=None):
    g, binc = read_glb(path)
    acc, bvs = g.get('accessors', []), g.get('bufferViews', [])
    textures = g.get('textures', [])

    def tex_src(info):
        if not info:
            return None
        t = textures[info['index']]
        src = t.get('source')
        for ext in t.get('extensions', {}).values():
            src = ext.get('source', src)
        return src

    verts = tris = prims = 0
    lo, hi = [float('inf')] * 3, [float('-inf')] * 3
    attrs = set()
    for mesh in g.get('meshes', []):
        for p in mesh.get('primitives', []):
            prims += 1
            attrs.update(p['attributes'].keys())
            pa = p['attributes'].get('POSITION')
            if pa is None:
                continue
            a = acc[pa]
            verts += a['count']
            if 'min' in a and 'max' in a:
                for k in range(3):
                    lo[k], hi[k] = min(lo[k], a['min'][k]), max(hi[k], a['max'][k])
            tris += (acc[p['indices']]['count'] if 'indices' in p else a['count']) // 3

    nodes = g.get('nodes', [])
    mats = []
    for mt in g.get('materials', []):
        pbr = mt.get('pbrMetallicRoughness', {})
        mats.append({
            'name': mt.get('name'),
            'baseImg': tex_src(pbr.get('baseColorTexture')),
            'mrImg': tex_src(pbr.get('metallicRoughnessTexture')),
            'normalImg': tex_src(mt.get('normalTexture')),
            'aoImg': tex_src(mt.get('occlusionTexture')),
            'emisImg': tex_src(mt.get('emissiveTexture')),
            'alpha': mt.get('alphaMode', 'OPAQUE'),
            'doubleSided': mt.get('doubleSided', False),
            'metal': pbr.get('metallicFactor', 1),
            'rough': pbr.get('roughnessFactor', 1),
            'ext': sorted(mt.get('extensions', {}).keys()),
        })

    imgs, img_bytes = [], 0
    for i, im in enumerate(g.get('images', [])):
        if 'bufferView' not in im:
            imgs.append({'i': i, 'uri': (im.get('uri') or '')[:40]})
            continue
        bv = bvs[im['bufferView']]
        s = bv.get('byteOffset', 0)
        b = binc[s:s + bv['byteLength']]
        img_bytes += bv['byteLength']
        imgs.append({'i': i, 'name': im.get('name'), 'mime': im.get('mimeType'),
                     'KB': round(bv['byteLength'] / 1024), 'px': img_dims(b)})

    base = os.path.splitext(os.path.basename(path))[0]
    if dump_dir and mats and mats[0]['baseImg'] is not None:
        im = g['images'][mats[0]['baseImg']]
        bv = bvs[im['bufferView']]
        s = bv.get('byteOffset', 0)
        ext = {'image/png': 'png', 'image/jpeg': 'jpg', 'image/webp': 'webp'}.get(im.get('mimeType'), 'bin')
        with open(os.path.join(dump_dir, f'{base}_base.{ext}'), 'wb') as f:
            f.write(binc[s:s + bv['byteLength']])

    return {
        'file': os.path.basename(path),
        'MB': round(os.path.getsize(path) / 1048576, 1),
        'generator': g.get('asset', {}).get('generator'),
        'extUsed': g.get('extensionsUsed', []),
        'extReq': g.get('extensionsRequired', []),
        'nodes': len(nodes),
        'nodeNames': [n.get('name') for n in nodes][:6],
        'nodeTransforms': [{k: n[k] for k in ('matrix', 'translation', 'rotation', 'scale') if k in n}
                           for n in nodes if any(k in n for k in ('matrix', 'translation', 'rotation', 'scale'))][:3],
        'meshes': len(g.get('meshes', [])),
        'primitives': prims,
        'attributes': sorted(attrs),
        'vertices': verts,
        'triangles': tris,
        'bbox': [round(hi[k] - lo[k], 3) for k in range(3)] if verts else None,
        'bboxMin': [round(v, 3) for v in lo] if verts else None,
        'materials': mats,
        'images': imgs,
        'imageMB': round(img_bytes / 1048576, 1),
        'geometryMB': round((len(binc) - img_bytes) / 1048576, 1),
    }


def main(argv):
    dump_dir = None
    if argv[:1] == ['--dump']:
        dump_dir, argv = argv[1], argv[2:]
        os.makedirs(dump_dir, exist_ok=True)
    for p in argv:
        try:
            r = report(p, dump_dir)
        except Exception as ex:  # noqa: BLE001 — report and continue with the next file
            r = {'file': os.path.basename(p), 'error': repr(ex)}
        print(json.dumps(r, ensure_ascii=False))


if __name__ == '__main__':
    main(sys.argv[1:])
