"""Prepare Rodin GLB sheets for Unity in background Blender.

blender --background --factory-startup --python rodin_process.py -- SRC_DIR STAGING_DIR PREVIEW_DIR [set_id ...]

For every sheet: weld vertices, split into loose parts, drop the scale silhouettes, name parts,
decimate, set the pivot to the bottom centre, apply a real-world scale, downscale textures,
pack a URP/Lit metallic-smoothness map and export one FBX per part plus a preview GLB.
"""
import json
import math
import os
import sys
import traceback

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

# Part centres come from the glTF file (Y up); Blender imports them as (x, -z, y).
SETS = {
    'tandoor_a': {
        'src': 'OLAM_31MB.glb', 'tex': 2048,
        'scale': ('part_height', 'body_left', 0.90),
        'parts': [('body_right', (0.32, -0.29, 0.02), 6000), ('body_left', (-0.32, -0.29, 0.02), 6000),
                  ('cone_black', (0.80, -0.29, 0.00), 2000), ('top', (0.29, 0.40, -0.07), 3000),
                  (None, (-0.83, -0.23, 0.02), 0)],
    },
    'tandoor_b': {
        'src': 'OLAM_extra_33MB.glb', 'tex': 2048,
        'scale': ('part_height', 'body_left', 0.90),
        'parts': [('body_right', (0.33, -0.30, 0.00), 6000), ('body_left', (-0.33, -0.30, -0.05), 6000),
                  ('top', (0.32, 0.42, -0.08), 3000), ('cone_black', (0.82, -0.32, -0.02), 2000),
                  (None, (-0.83, -0.25, 0.03), 0), ('chunk', (-0.06, -0.58, 0.22), 600)],
    },
    'work_table': {
        'src': 'OLAM_15MB.glb', 'tex': 1024,
        'scale': ('tabletop', 'table_with_log', 0.78),
        'parts': [('table_with_log', (0.00, 0.01, 0.00), 6000), (None, (0.00, -0.48, 0.04), 0)],
    },
    'non_stack': {
        'src': 'OLAM_10MB.glb', 'tex': 1024,
        'scale': ('part_width', 'non_stack', 0.30),
        'parts': [('non_stack', (0.00, 0.00, 0.00), 4000)],
    },
    'tulip_greigii': {
        'src': 'OLAM_extra7_31.9MB.glb', 'tex': 1024,
        # высота растения 20–40 см (RHS, gardenia.net, GRIN Czech) — берём 0,3 м
        'scale': ('part_height', 'a', 0.30),
        'parts': [('a', (-0.66, -0.01, 0.02), 3000), ('b', (-0.12, -0.02, -0.04), 3000),
                  ('c', (0.37, -0.02, -0.01), 3000), ('flower', (0.76, 0.24, -0.03), 1500)],
    },
    'saxaul_black': {
        # Исходник — 6,5 млн tris, напрямую Blender его не берёт (импорт > 5 ГБ памяти). Поэтому:
        # glb_split.py → куски, rodin_chunk.py → упрощение каждого, rodin_merge.py → этот файл (~80k tris)
        # в SRC_DIR. Альтернатива для машины с 8+ ГБ свободной памяти: 'pre_decimate': 56000 на исходнике.
        'src': 'OLAM_extra6_204.1MB.glb', 'tex': 2048,
        # чёрный саксаул обычно 4–9 м, до 12 м (БСЭ, Википедия) — берём нижнюю границу, 4 м
        'scale': ('part_height', 'large', 4.0),
        'parts': [('large', (-0.14, -0.01, 0.01), 40000), ('small', (0.75, -0.30, 0.00), 15000),
                  (None, (-0.85, -0.15, 0.02), 0)],
    },
    'snow_leopard': {
        'src': 'OLAM_extra2_32.9MB.glb', 'tex': 2048,
        # самая высокая стоящая поза — 0,66 м по макушке, тогда холка ≈ 0,56 м (Toronto Zoo, WLT)
        'scale': ('max_height', None, 0.66),
        'parts': [('stand', (0.21, -0.00, -0.73), 20000), ('stand_look', (-0.30, -0.00, -0.72), 20000),
                  ('walk', (-0.03, -0.06, 0.23), 20000), ('crouch', (0.28, -0.07, 0.20), 20000),
                  ('lie', (0.13, -0.11, 0.87), 20000),
                  (None, (-0.33, -0.03, 0.32), 0), (None, (-0.38, -0.01, 0.40), 0)],
    },
}


def tris_of(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def world_bbox(obj):
    """Bounding box from the vertices themselves: obj.bound_box goes stale after mesh edits."""
    co = np.empty(len(obj.data.vertices) * 3, dtype=np.float64)
    obj.data.vertices.foreach_get('co', co)
    co = co.reshape(-1, 3)
    m = np.array(obj.matrix_world)
    co = co @ m[:3, :3].T + m[:3, 3]
    return Vector(co.min(axis=0)), Vector(co.max(axis=0))


def select_only(obj):
    bpy.context.view_layer.update()
    for o in list(bpy.context.view_layer.objects):
        if o is not None and o != obj:
            o.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def tabletop_height(obj):
    """Height of the largest horizontal up-facing surface above 30 % of the object's height."""
    lo, hi = world_bbox(obj)
    height = hi.z - lo.z
    bins = {}
    for p in obj.data.polygons:
        if p.normal.z > 0.95:
            b = round((p.center.z - lo.z) / (height / 200))
            bins[b] = bins.get(b, 0.0) + p.area
    cand = [(a, b) for b, a in bins.items() if b * height / 200 > 0.3 * height]
    return max(cand)[1] * height / 200 if cand else height


def image_roles(mat):
    """Map 'base' / 'normal' / 'mr' to the image texture nodes feeding the Principled BSDF."""
    roles = {}
    for node in mat.node_tree.nodes:
        if node.type != 'TEX_IMAGE' or node.image is None:
            continue
        frontier, seen = [node], set()
        while frontier:
            n = frontier.pop()
            if n.name in seen:
                continue
            seen.add(n.name)
            for out in n.outputs:
                for link in out.links:
                    to = link.to_node
                    if to.type == 'BSDF_PRINCIPLED':
                        sock = link.to_socket.name
                        if sock == 'Base Color':
                            roles['base'] = node.image
                        elif sock in ('Metallic', 'Roughness'):
                            roles['mr'] = node.image
                        elif sock == 'Normal':
                            roles['normal'] = node.image
                    else:
                        frontier.append(to)
    return roles


def save_pixels(name, size, pixels, path, colorspace):
    img = bpy.data.images.new(name, size, size, alpha=True)
    img.colorspace_settings.name = colorspace
    img.alpha_mode = 'STRAIGHT'
    img.pixels.foreach_set(pixels.ravel())
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save()


def read_scaled(img, size):
    if tuple(img.size) != (size, size):
        img.scale(size, size)
    px = np.empty(size * size * 4, dtype=np.float32)
    img.pixels.foreach_get(px)
    return px.reshape(size, size, 4)


def process(set_id, cfg, src_dir, staging, preview_dir):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    # Очень плотные листы: вершины склеиваются уже при импорте и вся сетка один раз
    # упрощается до разделения, чтобы BMesh строился только один раз (экономия памяти).
    heavy = 'pre_decimate' in cfg
    bpy.ops.import_scene.gltf(filepath=os.path.join(src_dir, cfg['src']), merge_vertices=heavy)
    meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    assert len(meshes) == 1, f'{set_id}: expected one mesh, got {len(meshes)}'
    obj = meshes[0]
    obj.parent = None
    select_only(obj)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    try:
        with bpy.context.temp_override(object=obj, active_object=obj):
            bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except RuntimeError:
        pass
    if heavy:
        mod = obj.modifiers.new('PreDecimate', 'DECIMATE')
        mod.ratio = cfg['pre_decimate'] / tris_of(obj)
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    else:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
        bm.to_mesh(obj.data)
        bm.free()
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.separate(type='LOOSE')
    bpy.ops.object.mode_set(mode='OBJECT')

    pieces = [o for o in bpy.context.scene.objects if o.type == 'MESH']
    named, report = {}, {'set': set_id, 'src': cfg['src'], 'parts': [], 'dropped': [], 'unmatched': []}
    expected = [(name, Vector((c[0], -c[2], c[1])), target) for name, c, target in cfg['parts']]
    for piece in pieces:
        lo, hi = world_bbox(piece)
        centre = (lo + hi) / 2
        best = min(expected, key=lambda e: (e[1] - centre).length)
        if (best[1] - centre).length > 0.08 or tris_of(piece) < 150:
            report['unmatched'].append({'tris': tris_of(piece), 'centre': [round(v, 3) for v in centre]})
            bpy.data.objects.remove(piece, do_unlink=True)
            continue
        if best[0] is None:
            report['dropped'].append({'tris': tris_of(piece), 'size': [round(v, 3) for v in hi - lo]})
            bpy.data.objects.remove(piece, do_unlink=True)
            continue
        assert best[0] not in named, f'{set_id}: two pieces match {best[0]}'
        piece.name = piece.data.name = f'{set_id}_{best[0]}'
        named[best[0]] = (piece, best[2])

    mode, ref, metres = cfg['scale']
    if mode == 'part_height':
        lo, hi = world_bbox(named[ref][0])
        factor = metres / (hi.z - lo.z)
    elif mode == 'part_width':
        lo, hi = world_bbox(named[ref][0])
        factor = metres / max(hi.x - lo.x, hi.y - lo.y)
    elif mode == 'tabletop':
        factor = metres / tabletop_height(named[ref][0])
    else:
        factor = metres / max(world_bbox(p)[1].z - world_bbox(p)[0].z for p, _ in named.values())
    report['scale_factor'] = round(factor, 4)

    mat = named[next(iter(named))][0].data.materials[0]
    mat.name = f'{set_id}_Mat'
    roles = image_roles(mat)
    out_dir = os.path.join(staging, set_id)
    os.makedirs(out_dir, exist_ok=True)
    size = cfg['tex']
    base = read_scaled(roles['base'], size)
    save_pixels(f'{set_id}_BaseColor', size, base, os.path.join(out_dir, f'{set_id}_BaseColor.png'), 'sRGB')
    if 'normal' in roles:
        nrm = read_scaled(roles['normal'], size)
        save_pixels(f'{set_id}_Normal', size, nrm, os.path.join(out_dir, f'{set_id}_Normal.png'), 'Non-Color')
    if 'mr' in roles:
        mr = read_scaled(roles['mr'], size)
        ms = np.zeros_like(mr)
        ms[..., 0] = mr[..., 2]          # metallic: glTF blue → URP red
        ms[..., 3] = 1.0 - mr[..., 1]    # smoothness: 1 − glTF roughness (green) → URP alpha
        save_pixels(f'{set_id}_MetallicSmoothness', size, ms,
                    os.path.join(out_dir, f'{set_id}_MetallicSmoothness.png'), 'Non-Color')
    report['textures'] = {'size': size, 'maps': sorted(roles.keys())}

    offset = 0.0
    for part, (piece, target) in named.items():
        before = tris_of(piece)
        if target and before > target:
            mod = piece.modifiers.new('Decimate', 'DECIMATE')
            mod.ratio = target / before
            mod.use_collapse_triangulate = True
            select_only(piece)
            bpy.ops.object.modifier_apply(modifier=mod.name)
        lo, hi = world_bbox(piece)
        pivot = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
        piece.data.transform(Matrix.Translation(-pivot))
        piece.data.transform(Matrix.Scale(factor, 4))
        piece.location = (0, 0, 0)
        piece.data.update()
        for poly in piece.data.polygons:
            poly.use_smooth = True
        bm = bmesh.new()
        bm.from_mesh(piece.data)
        for e in bm.edges:
            if len(e.link_faces) == 2 and e.calc_face_angle(0.0) > math.radians(40):
                e.smooth = False
        bm.to_mesh(piece.data)
        bm.free()
        lo, hi = world_bbox(piece)
        fbx = os.path.join(out_dir, f'{piece.name}.fbx')
        select_only(piece)
        bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={'MESH'},
                                 apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL',
                                 axis_forward='-Z', axis_up='Y', bake_space_transform=True,
                                 use_mesh_modifiers=True, mesh_smooth_type='OFF',
                                 add_leaf_bones=False, path_mode='STRIP', embed_textures=False)
        report['parts'].append({'name': piece.name, 'tris_before': before, 'tris_after': tris_of(piece),
                                'size_m': [round(v, 3) for v in hi - lo], 'fbx': os.path.basename(fbx)})
        piece.location = (offset - lo.x, 0, 0)
        offset += (hi.x - lo.x) + 0.15

    bpy.context.view_layer.update()
    for o in list(bpy.context.view_layer.objects):
        if o is not None:
            o.select_set(o.type == 'MESH')
    os.makedirs(preview_dir, exist_ok=True)
    bpy.ops.export_scene.gltf(filepath=os.path.join(preview_dir, f'{set_id}.glb'),
                              export_format='GLB', use_selection=True)
    return report


def main():
    args = sys.argv[sys.argv.index('--') + 1:]
    src_dir, staging, preview_dir = args[:3]
    only = args[3:] or list(SETS)
    reports = []
    for set_id in only:
        try:
            reports.append(process(set_id, SETS[set_id], src_dir, staging, preview_dir))
        except Exception:  # noqa: BLE001 — record the failure and go on with the next sheet
            reports.append({'set': set_id, 'error': traceback.format_exc()})
    os.makedirs(staging, exist_ok=True)
    with open(os.path.join(staging, 'report.json'), 'w', encoding='utf-8') as f:
        json.dump(reports, f, ensure_ascii=False, indent=1)
    print('RODIN_REPORT ' + json.dumps(reports, ensure_ascii=False))


main()
