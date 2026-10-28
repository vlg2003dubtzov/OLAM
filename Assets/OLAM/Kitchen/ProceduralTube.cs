using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Полый цилиндр (стенка тандыра). В редакторе: Rebuild.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class ProceduralTube : MonoBehaviour
    {
        [SerializeField] float radius = 0.55f;
        [SerializeField] float height = 1.1f;
        [SerializeField] float thickness = 0.08f;
        [SerializeField] int segments = 24;
        [SerializeField] bool generateCollider = true;

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            var mesh = BuildMesh();
            var filter = GetComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            if (generateCollider)
            {
                var col = GetComponent<MeshCollider>();
                col.sharedMesh = null;
                col.sharedMesh = mesh;
                col.convex = false;
            }
        }

        Mesh BuildMesh()
        {
            int ring = segments;
            // внешнее + внутреннее кольцо, верх и низ
            int vertCount = (ring + 1) * 4;
            var verts = new Vector3[vertCount];
            var norms = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];

            float inner = Mathf.Max(0.01f, radius - thickness);
            for (int i = 0; i <= ring; i++)
            {
                float t = (float)i / ring;
                float ang = t * Mathf.PI * 2f;
                float cos = Mathf.Cos(ang);
                float sin = Mathf.Sin(ang);

                Vector3 outDir = new Vector3(cos, 0f, sin);
                Vector3 inDir = -outDir;

                int o0 = i;
                int o1 = (ring + 1) + i;
                int i0 = (ring + 1) * 2 + i;
                int i1 = (ring + 1) * 3 + i;

                verts[o0] = outDir * radius;
                verts[o1] = outDir * radius + Vector3.up * height;
                verts[i0] = outDir * inner;
                verts[i1] = outDir * inner + Vector3.up * height;

                norms[o0] = norms[o1] = outDir;
                norms[i0] = norms[i1] = inDir;

                uvs[o0] = new Vector2(t, 0f);
                uvs[o1] = new Vector2(t, 1f);
                uvs[i0] = new Vector2(t, 0f);
                uvs[i1] = new Vector2(t, 1f);
            }

            // 2 стенки * ring * 2 треугольника * 3 индекса
            var tris = new int[ring * 2 * 2 * 3];
            int ti = 0;
            for (int i = 0; i < ring; i++)
            {
                // внешняя
                int a = i;
                int b = i + 1;
                int c = (ring + 1) + i;
                int d = (ring + 1) + i + 1;
                tris[ti++] = a; tris[ti++] = c; tris[ti++] = b;
                tris[ti++] = b; tris[ti++] = c; tris[ti++] = d;

                // внутренняя (обратный winding)
                int ia = (ring + 1) * 2 + i;
                int ib = (ring + 1) * 2 + i + 1;
                int ic = (ring + 1) * 3 + i;
                int id = (ring + 1) * 3 + i + 1;
                tris[ti++] = ia; tris[ti++] = ib; tris[ti++] = ic;
                tris[ti++] = ib; tris[ti++] = id; tris[ti++] = ic;
            }

            var mesh = new Mesh { name = "ProceduralTube" };
            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateBounds();
            return mesh;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!Application.isPlaying)
                Rebuild();
        }
#endif
    }
}
