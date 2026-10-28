using UnityEngine;

namespace Olam.Nature
{
    /// <summary>
    /// Простой кластер травы: крест-квады + GPU Instanting material (OLAM/Grass).
    /// Без коллайдеров — только визуал для VR.
    /// </summary>
    [ExecuteAlways]
    public class GrassPatch : MonoBehaviour
    {
        [SerializeField] Material grassMaterial;
        [SerializeField] int count = 48;
        [SerializeField] float radius = 2.5f;
        [SerializeField] Vector2 heightRange = new Vector2(0.25f, 0.55f);
        [SerializeField] Vector2 widthRange = new Vector2(0.08f, 0.16f);
        [SerializeField] int seed = 17;
        [SerializeField] bool rebuildOnValidate = true;

        Mesh _blade;
        Matrix4x4[] _matrices;
        MaterialPropertyBlock _mpb;

        void OnEnable()
        {
            EnsureMesh();
            Rebuild();
        }

        void OnValidate()
        {
            if (rebuildOnValidate)
            {
                EnsureMesh();
                Rebuild();
            }
        }

        void OnDisable()
        {
            if (_blade != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(_blade);
                else
#endif
                    Destroy(_blade);
                _blade = null;
            }
        }

        void LateUpdate()
        {
            if (grassMaterial == null || _matrices == null || _matrices.Length == 0 || _blade == null)
                return;
            Graphics.DrawMeshInstanced(_blade, 0, grassMaterial, _matrices, _matrices.Length, _mpb,
                UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer);
        }

        public void Rebuild()
        {
            count = Mathf.Clamp(count, 1, 512);
            _matrices = new Matrix4x4[count];
            var rng = new System.Random(seed);
            Vector3 origin = transform.position;

            for (int i = 0; i < count; i++)
            {
                float ang = (float)(rng.NextDouble() * Mathf.PI * 2.0);
                float r = (float)rng.NextDouble() * radius;
                float x = Mathf.Cos(ang) * r;
                float z = Mathf.Sin(ang) * r;
                float h = Mathf.Lerp(heightRange.x, heightRange.y, (float)rng.NextDouble());
                float w = Mathf.Lerp(widthRange.x, widthRange.y, (float)rng.NextDouble());
                float yaw = (float)rng.NextDouble() * 360f;
                var pos = origin + new Vector3(x, 0f, z);
                var rot = Quaternion.Euler(0f, yaw, 0f);
                var scale = new Vector3(w, h, w);
                _matrices[i] = Matrix4x4.TRS(pos, rot, scale);
            }

            _mpb ??= new MaterialPropertyBlock();
        }

        void EnsureMesh()
        {
            if (_blade != null) return;
            // Два пересекающихся квада (крест) — читается с любого угла, мало вершин
            _blade = new Mesh { name = "OLAM_GrassBlade" };
            var v = new Vector3[]
            {
                new(-0.5f, 0f, 0f), new(0.5f, 0f, 0f), new(0.5f, 1f, 0f), new(-0.5f, 1f, 0f),
                new(0f, 0f, -0.5f), new(0f, 0f, 0.5f), new(0f, 1f, 0.5f), new(0f, 1f, -0.5f)
            };
            var uv = new Vector2[]
            {
                new(0, 0), new(1, 0), new(1, 1), new(0, 1),
                new(0, 0), new(1, 0), new(1, 1), new(0, 1)
            };
            var c = new Color[]
            {
                new(1, 1, 1, 0), new(1, 1, 1, 0), new(1, 1, 1, 1), new(1, 1, 1, 1),
                new(1, 1, 1, 0), new(1, 1, 1, 0), new(1, 1, 1, 1), new(1, 1, 1, 1)
            };
            var t = new int[]
            {
                0, 2, 1, 0, 3, 2,
                4, 6, 5, 4, 7, 6
            };
            _blade.vertices = v;
            _blade.uv = uv;
            _blade.colors = c;
            _blade.triangles = t;
            _blade.RecalculateNormals();
            _blade.RecalculateBounds();
        }
    }
}
