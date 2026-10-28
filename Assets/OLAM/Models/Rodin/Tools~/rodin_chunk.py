"""Decimate one chunk of a dense model in background Blender, keeping its open borders in place.

blender --background --factory-startup --python rodin_chunk.py -- IN.glb OUT.glb TARGET_TRIS
Vertices on open borders (the cut between chunks) get no weight in the 'free' vertex group, and the
Decimate modifier's group factor makes collapsing them very expensive, so neighbouring chunks still
meet edge to edge when they are joined again.
"""
import sys

import bmesh
import bpy
import numpy as np

src, dst, target = sys.argv[sys.argv.index('--') + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
obj = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
bpy.context.view_layer.objects.active = obj
obj.select_set(True)

bm = bmesh.new()
bm.from_mesh(obj.data)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
bm.verts.index_update()
border = np.fromiter((v.is_boundary for v in bm.verts), dtype=bool, count=len(bm.verts))
bm.to_mesh(obj.data)
bm.free()

group = obj.vertex_groups.new(name='free')
group.add(np.nonzero(~border)[0].tolist(), 1.0, 'REPLACE')
obj.data.calc_loop_triangles()
before = len(obj.data.loop_triangles)
mod = obj.modifiers.new('Decimate', 'DECIMATE')
mod.ratio = min(1.0, int(target) / before)
mod.use_collapse_triangulate = True
mod.vertex_group = 'free'
mod.vertex_group_factor = 1000.0
bpy.ops.object.modifier_apply(modifier=mod.name)
obj.data.calc_loop_triangles()
print(f'CHUNK {src}: {before} -> {len(obj.data.loop_triangles)} tris, border vertices {int(border.sum())}')
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', use_selection=False)
