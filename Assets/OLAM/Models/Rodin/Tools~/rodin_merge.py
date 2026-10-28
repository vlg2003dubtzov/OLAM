"""Join decimated chunks back into one model in background Blender.

blender --background --factory-startup --python rodin_merge.py -- OUT.glb IN1.glb IN2.glb ...
Borders are welded, all faces get the first chunk's material, the copies are dropped.
"""
import sys

import bmesh
import bpy
import numpy as np

args = sys.argv[sys.argv.index('--') + 1:]
dst, sources = args[0], args[1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
for path in sources:
    bpy.ops.import_scene.gltf(filepath=path)
meshes = [o for o in bpy.context.scene.objects if o.type == 'MESH']
for o in bpy.context.scene.objects:
    o.select_set(o.type == 'MESH')
bpy.context.view_layer.objects.active = meshes[0]
bpy.ops.object.join()
obj = bpy.context.view_layer.objects.active
obj.parent = None
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

mesh = obj.data
mesh.polygons.foreach_set('material_index', np.zeros(len(mesh.polygons), dtype=np.int32))
while len(mesh.materials) > 1:
    mesh.materials.pop(index=len(mesh.materials) - 1)

bm = bmesh.new()
bm.from_mesh(mesh)
before = len(bm.verts)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
after = len(bm.verts)
bm.to_mesh(mesh)
bm.free()
mesh.calc_loop_triangles()
print(f'MERGE: {len(sources)} chunks, {len(mesh.loop_triangles)} tris, welded {before - after} vertices')
bpy.ops.export_scene.gltf(filepath=dst, export_format='GLB', use_selection=False)
