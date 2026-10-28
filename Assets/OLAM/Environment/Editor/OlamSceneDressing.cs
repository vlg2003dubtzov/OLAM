#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Olam.Core;
using Olam.Nature;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Olam.EditorTools
{
    /// <summary>
    /// Меню: OLAM → Environment → Dress Scenes / Capture Previews.
    /// Окружение и модели Rodin поверх заготовок сцен: рельеф с землёй своего биома, трава, камни,
    /// туман, модели вместо капсул-заглушек. Всё добавленное лежит под корнем OLAM_Dressing и при
    /// каждом запуске пересоздаётся; заглушки остаются со сканером и карточкой, у них только
    /// выключается отрисовка. Сцены открываются добавочно и закрываются после сохранения —
    /// открытую в редакторе сцену и её несохранённые правки не трогает.
    /// После пересборки сцен сборщиками (Build Nature Dome …) запустить Dress Scenes ещё раз.
    /// </summary>
    public static class OlamSceneDressing
    {
        const string Prefabs = "Assets/OLAM/Models/Rodin/Prefabs/";
        const string Env = "Assets/OLAM/Environment";
        const string RootName = "OLAM_Dressing";
        const string CaptureDir = "Temp/OLAM_Captures";

        static readonly string[] Scenes =
            { "OlamHub", "NatureDome", "Biome_Desert", "Biome_Adyr", "Biome_Mountains", "Biome_Tugai", "ChatkalNightTrail" };

        struct Relief
        {
            public float flat, size, height, freq, ringStart, ringHeight, riverZ, riverWidth;
            public bool dunes;
            public int seed, res;
        }

        static Mesh[] rocks;
        static readonly Dictionary<string, (Mesh frame, Mesh panel)> arches = new();

        [MenuItem("OLAM/Environment/Dress Scenes")]
        public static void DressAll()
        {
            EnsureFolder("Assets/OLAM", "Environment");
            EnsureFolder(Env, "Meshes");
            EnsureFolder(Env, "Materials");
            rocks = new[] { RockMesh("Rock_A", 3), RockMesh("Rock_B", 7), RockMesh("Rock_C", 12) };

            InScene("Biome_Desert", true, DressDesert);
            InScene("Biome_Adyr", true, DressAdyr);
            InScene("Biome_Mountains", true, DressMountains);
            InScene("Biome_Tugai", true, DressTugai);
            InScene("ChatkalNightTrail", true, DressChatkal);
            InScene("NatureDome", true, DressDome);
            InScene("OlamHub", true, DressHub);
            AssetDatabase.SaveAssets();
            Debug.Log("OLAM Environment: хаб, купол, биомы и тропа Чаткала оформлены");
        }

        [MenuItem("OLAM/Environment/Capture Previews")]
        public static void CaptureAll()
        {
            Directory.CreateDirectory(CaptureDir);
            foreach (var name in Scenes)
                InScene(name, false, scene => Capture(scene, name));
            Debug.Log($"OLAM Environment: снимки сцен → {CaptureDir}");
        }

        // ---------- сцены ----------

        static void DressDesert(Scene scene)
        {
            var root = NewRoot(scene);
            var relief = new Relief { flat = 12f, size = 150f, height = 3.5f, freq = 0.05f, dunes = true, seed = 1, res = 110 };
            Ground(scene, root, "Biome_Desert", relief, Load<Material>("Assets/OLAM/Materials/Mat_DesertSand.mat"));
            Atmosphere(new Color(0.87f, 0.79f, 0.63f), new Color(0.62f, 0.56f, 0.47f), 25f, 110f);
            var rng = new System.Random(11);
            Scatter(scene, root, relief, rng, "saxaul_black_large", 3, 16f, 40f, 0.85f, 1.1f);
            Scatter(scene, root, relief, rng, "saxaul_black_small", 5, 8f, 30f, 0.7f, 1.2f);
            Rocks(root, relief, rng, 8, 8f, 35f, 0.4f, 1.4f);
            Swap(Find(scene, "Scan_saxaul_black"), ("saxaul_black_small", Vector3.zero, 30f));
            ArchPortals(scene, root);
        }

        static void DressAdyr(Scene scene)
        {
            var root = NewRoot(scene);
            var relief = new Relief { flat = 12f, size = 150f, height = 7f, freq = 0.03f, seed = 2, res = 110 };
            Ground(scene, root, "Biome_Adyr", relief,
                GroundMat("Mat_Env_AdyrGround", new Color(0.58f, 0.56f, 0.34f), new Color(0.40f, 0.38f, 0.22f), 5f));
            Atmosphere(new Color(0.72f, 0.80f, 0.82f), new Color(0.55f, 0.60f, 0.55f), 30f, 120f);
            var rng = new System.Random(22);
            Grass(root, rng, GrassMat("Mat_Env_GrassAdyr", new Color(0.24f, 0.52f, 0.14f), new Color(0.66f, 0.84f, 0.30f)),
                16, 11f, 220, 2.2f, new Vector2(0.18f, 0.40f));
            TulipClumps(scene, root, rng, 7, 3.5f, 10f);
            Rocks(root, relief, rng, 5, 10f, 30f, 0.4f, 1.2f);
            Swap(Find(scene, "Scan_tulip_greigii"), Tulips());
            ArchPortals(scene, root);
        }

        static void DressMountains(Scene scene)
        {
            var root = NewRoot(scene);
            var relief = new Relief { flat = 12f, size = 170f, height = 5f, freq = 0.04f, ringStart = 40f, ringHeight = 30f, seed = 3, res = 130 };
            Ground(scene, root, "Biome_Mountains", relief,
                GroundMat("Mat_Env_MountainGround", new Color(0.45f, 0.47f, 0.38f), new Color(0.28f, 0.29f, 0.24f), 3f));
            Atmosphere(new Color(0.70f, 0.76f, 0.82f), new Color(0.50f, 0.55f, 0.60f), 35f, 140f);
            var rng = new System.Random(33);
            Grass(root, rng, GrassMat("Mat_Env_GrassMountains", new Color(0.28f, 0.42f, 0.18f), new Color(0.50f, 0.60f, 0.28f)),
                10, 11f, 140, 2f, new Vector2(0.15f, 0.40f));
            Rocks(root, relief, rng, 16, 7f, 45f, 0.5f, 2.5f);
            ArchPortals(scene, root);
        }

        static void DressTugai(Scene scene)
        {
            var root = NewRoot(scene);
            var relief = new Relief { flat = 12f, size = 150f, height = 2f, freq = 0.05f, riverZ = 18f, riverWidth = 9f, seed = 4, res = 110 };
            Ground(scene, root, "Biome_Tugai", relief,
                GroundMat("Mat_Env_TugaiGround", new Color(0.46f, 0.44f, 0.30f), new Color(0.30f, 0.29f, 0.19f), 6f));
            Atmosphere(new Color(0.70f, 0.78f, 0.74f), new Color(0.50f, 0.58f, 0.52f), 25f, 100f);

            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "River";
            water.transform.SetParent(root, false);
            water.transform.position = new Vector3(0f, -0.45f, relief.riverZ);
            water.transform.localScale = new Vector3(15f, 1f, 0.8f);
            Object.DestroyImmediate(water.GetComponent<Collider>());
            water.GetComponent<MeshRenderer>().sharedMaterial = Load<Material>("Assets/OLAM/Materials/Mat_Water.mat");

            var rng = new System.Random(44);
            Grass(root, rng, GrassMat("Mat_Env_GrassTugai", new Color(0.18f, 0.44f, 0.16f), new Color(0.52f, 0.76f, 0.30f)),
                20, 11f, 240, 2.4f, new Vector2(0.25f, 0.70f));
            Rocks(root, relief, rng, 4, 9f, 25f, 0.3f, 0.9f);
            ArchPortals(scene, root);
        }

        static void DressChatkal(Scene scene)
        {
            var root = NewRoot(scene);
            // ночная тропа уже со своим окружением; здесь барс вместо капсулы и арка у портала
            Swap(Find(scene, "Scan_snow_leopard"), ("snow_leopard_stand", Vector3.zero, 180f));
            ArchPortals(scene, root);
        }

        static void DressDome(Scene scene)
        {
            var root = NewRoot(scene);
            // у каждого портала — маленький образ того, что внутри
            Preview(scene, root, "Portal_Biome_Desert", 0.35f, ("saxaul_black_small", Vector3.zero, 20f));
            Preview(scene, root, "Portal_Biome_Adyr", 1.4f, Tulips());
            Preview(scene, root, "Portal_ChatkalNightTrail", 0.8f, ("snow_leopard_stand", Vector3.zero, 160f));
            var mountains = Find(scene, "Portal_Biome_Mountains");
            if (mountains != null)
                RockAt(root, InFront(mountains.transform.position), 0.6f, 0);
            ArchPortals(scene, root);
        }

        static void DressHub(Scene scene)
        {
            var root = NewRoot(scene);
            // двор хаба (Hub_Floor) остаётся; вокруг — степь, рельеф чуть ниже двора, чтобы не мерцал
            var relief = new Relief { flat = 16f, size = 160f, height = 5f, freq = 0.03f, ringStart = 45f, ringHeight = 18f, seed = 5, res = 110 };
            Ground(scene, root, "OlamHub", relief,
                GroundMat("Mat_Env_AdyrGround", new Color(0.58f, 0.56f, 0.34f), new Color(0.40f, 0.38f, 0.22f), 5f),
                hideBase: false, y: -0.02f);
            Atmosphere(new Color(0.86f, 0.76f, 0.62f), new Color(0.58f, 0.55f, 0.50f), 30f, 130f);
            var rng = new System.Random(55);
            Grass(root, rng, GrassMat("Mat_Env_GrassAdyr", new Color(0.24f, 0.52f, 0.14f), new Color(0.66f, 0.84f, 0.30f)),
                14, 0f, 220, 2.4f, new Vector2(0.18f, 0.40f), 16f, 24f);
            Scatter(scene, root, relief, rng, "saxaul_black_large", 3, 30f, 55f, 0.8f, 1.1f);
            Rocks(root, relief, rng, 10, 18f, 50f, 0.5f, 2.2f);

            // у порталов — то, что за ними
            PlaceAt(scene, root, "Hub_Nature_Leopard", new Vector3(-1.85f, 0f, 1.3f), 200f, 1f, ("snow_leopard_stand", Vector3.zero, 0f));
            PlaceAt(scene, root, "Hub_Nature_Tulips", new Vector3(-4.1f, 0f, 1.1f), 0f, 1.5f, Tulips());
            PlaceAt(scene, root, "Hub_Tandoor", new Vector3(1.15f, 0f, 2.4f), 0f, 0.8f, ("tandoor_a_body_left", Vector3.zero, 0f));
            PlaceAt(scene, root, "Hub_WorkTable", new Vector3(-1.5f, 0f, 2.7f), 70f, 0.6f, ("work_table_table_with_log", Vector3.zero, 0f));

            // двор караван-сарая — светлый камень вместо тёмной плоскости
            var floor = Find(scene, "Hub_Floor");
            if (floor != null && floor.TryGetComponent<MeshRenderer>(out var floorRenderer))
                floorRenderer.sharedMaterial = GroundMat("Mat_Env_HubYard",
                    new Color(0.74f, 0.67f, 0.55f), new Color(0.56f, 0.50f, 0.41f), 9f);
            // заголовок хаба — выше арок (холст в мире: поле пишется напрямую)
            if (Find(scene, "HubTitle") is GameObject title && title.transform is RectTransform titleRect)
            {
                var so = new SerializedObject(titleRect);
                so.FindProperty("m_AnchoredPosition").vector2Value = new Vector2(titleRect.anchoredPosition.x, 3.15f);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            ArchPortals(scene, root);
        }

        // ---------- порталы-арки ----------

        /// <summary>
        /// Стрельчатая арка (пештак, как в знаке OLAM) вокруг каждого портала: глиняная рама и панель цвета
        /// портала внутри. Примитив портала остаётся ради коллайдера (клик), его не видно; подпись — над аркой.
        /// Порталы ближе 3 м к камере не трогает: арка закрыла бы вид.
        /// </summary>
        static void ArchPortals(Scene scene, Transform root)
        {
            var cam = FindCamera(scene);
            var clay = ClayMat("Mat_Env_ArchClay", new Color(0.64f, 0.28f, 0.16f));
            var group = Group(root, "PortalArches");
            foreach (var g in scene.GetRootGameObjects())
                foreach (var portal in g.GetComponentsInChildren<ScenePortal>(true))
                {
                    if (!portal.TryGetComponent<MeshRenderer>(out var mr) || !portal.TryGetComponent<MeshFilter>(out var mf))
                        continue;
                    var local = mf.sharedMesh.bounds;
                    var size = Vector3.Scale(local.size, portal.transform.lossyScale);
                    var centre = portal.transform.TransformPoint(local.center);
                    var pos = new Vector3(centre.x, centre.y - size.y * 0.5f, centre.z);
                    var toCamera = cam != null ? cam.transform.position - pos : -pos;
                    toCamera.y = 0f;
                    if (cam != null && toCamera.magnitude < 3f)
                    {
                        // у самой камеры арка закрыла бы вид: портал остаётся как есть, подпись — небольшая, над ним
                        SetLabel(portal.transform.Find("Label") as RectTransform,
                            new Vector3(centre.x, centre.y + size.y * 0.5f + 0.35f, centre.z), null);
                        continue;
                    }

                    float width = Mathf.Max(1.3f, Mathf.Max(size.x, size.z) + 0.35f);
                    float height = width * 1.5f;
                    var (frame, panel) = ArchMeshes(width, height);
                    var arch = new GameObject("Arch_" + portal.name);
                    arch.transform.SetParent(group, false);
                    arch.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(-toCamera.normalized));
                    arch.AddComponent<MeshFilter>().sharedMesh = frame;
                    arch.AddComponent<MeshRenderer>().sharedMaterial = clay;
                    var inner = new GameObject("Panel");
                    inner.transform.SetParent(arch.transform, false);
                    inner.AddComponent<MeshFilter>().sharedMesh = panel;
                    inner.AddComponent<MeshRenderer>().sharedMaterial = mr.sharedMaterial;
                    mr.enabled = false;

                    // подпись — вывеска внутри проёма, перед панелью
                    float depth = Mathf.Max(0.25f, width * 0.22f);
                    SetLabel(portal.transform.Find("Label") as RectTransform,
                        arch.transform.TransformPoint(new Vector3(0f, height * 0.5f, -(depth * 0.5f + 0.06f))),
                        arch.transform.rotation);
                }
        }

        /// <summary>
        /// Подпись портала: позиция в мире, поворот (null — прежний) и ровный масштаб текста без сплющивания
        /// от масштаба портала. Это холст в мире: в добавочно открытой сцене его Transform не сохраняется,
        /// поэтому поля пишутся напрямую.
        /// </summary>
        static void SetLabel(RectTransform label, Vector3 world, Quaternion? rotation)
        {
            if (label == null) return;
            const float textScale = 0.0064f;
            var local = label.parent.InverseTransformPoint(world);
            var parentScale = label.parent.lossyScale;
            var so = new SerializedObject(label);
            so.FindProperty("m_AnchoredPosition").vector2Value = new Vector2(local.x, local.y);
            so.FindProperty("m_LocalPosition").vector3Value = local;
            if (rotation.HasValue)
                so.FindProperty("m_LocalRotation").quaternionValue = Quaternion.Inverse(label.parent.rotation) * rotation.Value;
            so.FindProperty("m_LocalScale").vector3Value = new Vector3(
                textScale / parentScale.x, textScale / parentScale.y, textScale / parentScale.z);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static (Mesh frame, Mesh panel) ArchMeshes(float width, float height)
        {
            string key = $"{width:F2}x{height:F2}".Replace(',', '.');
            if (arches.TryGetValue(key, out var cached) && cached.frame != null) return cached;

            float a = width * 0.5f;
            float t = Mathf.Max(0.16f, width * 0.14f);
            float depth = Mathf.Max(0.25f, width * 0.22f);
            float spring = height - a * Mathf.Sqrt(3f);   // равносторонняя стрельчатая арка
            var outer = ArchContour(a, 2f * a, spring, 14);
            var innerLine = ArchContour(a, 2f * a - t, spring, 14);   // дуги концентричны: рама одной толщины

            var v = new List<Vector3>();
            var tri = new List<int>();
            float z0 = -depth * 0.5f, z1 = depth * 0.5f;
            var mid = new Vector2(0f, spring);
            for (int i = 0; i < outer.Count - 1; i++)
            {
                Vector3 o0 = outer[i], o1 = outer[i + 1], n0 = innerLine[i], n1 = innerLine[i + 1];
                Quad(v, tri, At(o0, z0), At(o1, z0), At(n1, z0), At(n0, z0), Vector3.back);
                Quad(v, tri, At(o0, z1), At(o1, z1), At(n1, z1), At(n0, z1), Vector3.forward);
                Quad(v, tri, At(o0, z0), At(o1, z0), At(o1, z1), At(o0, z1), (Vector2)(o0 + o1) * 0.5f - mid);
                Quad(v, tri, At(n0, z0), At(n1, z0), At(n1, z1), At(n0, z1), mid - (Vector2)(n0 + n1) * 0.5f);
            }
            var frame = SaveMesh("Arch_" + key, v.ToArray(), PlanarUV(v), tri.ToArray());

            // панель: веер из середины проёма, видна с обеих сторон
            var pv = new List<Vector3>();
            var pt = new List<int>();
            var centre = new Vector3(0f, spring * 0.6f, 0f);
            for (int i = 0; i < innerLine.Count; i++)
            {
                Vector3 p0 = At(innerLine[i], 0f), p1 = At(innerLine[(i + 1) % innerLine.Count], 0f);
                int k = pv.Count;
                pv.Add(centre); pv.Add(p0); pv.Add(p1);
                pv.Add(centre); pv.Add(p0); pv.Add(p1);
                bool front = Vector3.Dot(Vector3.Cross(p0 - centre, p1 - centre), Vector3.back) >= 0f;
                pt.AddRange(front ? new[] { k, k + 1, k + 2, k + 3, k + 5, k + 4 } : new[] { k, k + 2, k + 1, k + 3, k + 4, k + 5 });
            }
            var panel = SaveMesh("ArchPanel_" + key, pv.ToArray(), PlanarUV(pv), pt.ToArray());
            arches[key] = (frame, panel);
            return (frame, panel);
        }

        /// <summary>Контур: низ левой опоры → пята → дуга → вершина → дуга → правая пята → низ.</summary>
        static List<Vector2> ArchContour(float centreOffset, float radius, float spring, int segments)
        {
            float half = radius - centreOffset;
            float rise = Mathf.Sqrt(radius * radius - centreOffset * centreOffset);
            float apexAngle = Mathf.Atan2(rise, -centreOffset);
            var left = new List<Vector2>();
            for (int s = 1; s <= segments; s++)
            {
                float ang = Mathf.Lerp(Mathf.PI, apexAngle, s / (float)segments);
                left.Add(new Vector2(centreOffset + radius * Mathf.Cos(ang), spring + radius * Mathf.Sin(ang)));
            }
            var pts = new List<Vector2> { new(-half, 0f), new(-half, spring) };
            pts.AddRange(left);
            for (int s = left.Count - 2; s >= 0; s--)
                pts.Add(new Vector2(-left[s].x, left[s].y));
            pts.Add(new Vector2(half, spring));
            pts.Add(new Vector2(half, 0f));
            return pts;
        }

        static Vector3 At(Vector2 p, float z) => new(p.x, p.y, z);

        static void Quad(List<Vector3> v, List<int> t, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) >= 0f)
                t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            else
                t.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
        }

        static Vector2[] PlanarUV(List<Vector3> v)
        {
            var uv = new Vector2[v.Count];
            for (int i = 0; i < v.Count; i++) uv[i] = new Vector2(v[i].x + v[i].z, v[i].y);
            return uv;
        }

        static Camera FindCamera(Scene scene)
        {
            foreach (var g in scene.GetRootGameObjects())
            {
                var cam = g.GetComponentInChildren<Camera>(true);
                if (cam != null) return cam;
            }
            return null;
        }

        static Material ClayMat(string name, Color color)
        {
            string path = $"{Env}/Materials/{name}.mat";
            var m = Load<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("OLAM/Clay Stylized") ?? Shader.Find("Universal Render Pipeline/Lit");
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------- модели ----------

        static void PlaceAt(Scene scene, Transform root, string name, Vector3 pos, float yaw, float scale,
            params (string, Vector3, float)[] parts)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(root, false);
            holder.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            holder.transform.localScale = Vector3.one * scale;
            Place(scene, holder.transform, parts);
        }

        static (string, Vector3, float)[] Tulips() => new[]
        {
            ("tulip_greigii_a", new Vector3(-0.12f, 0f, 0f), 0f),
            ("tulip_greigii_b", new Vector3(0.10f, 0f, 0.08f), 120f),
            ("tulip_greigii_c", new Vector3(0.02f, 0f, -0.12f), 240f),
        };

        /// <summary>Модель на месте заглушки: капсула остаётся (сканер, карточка, коллайдер), её не видно.</summary>
        static void Swap(GameObject placeholder, params (string prefab, Vector3 offset, float yaw)[] parts)
        {
            if (placeholder == null) return;
            var old = placeholder.transform.Find("RodinModel");
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var holder = new GameObject("RodinModel");
            holder.transform.SetParent(placeholder.transform, false);
            var s = placeholder.transform.lossyScale;
            holder.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
            var p = placeholder.transform.position;
            holder.transform.SetPositionAndRotation(new Vector3(p.x, 0f, p.z), Quaternion.identity);

            float top = Place(placeholder.scene, holder.transform, parts);
            foreach (var r in placeholder.GetComponents<MeshRenderer>())
                r.enabled = false;
            var beacon = placeholder.transform.Find("Beacon");
            if (beacon != null)
                beacon.position = new Vector3(p.x, top + 0.35f, p.z);
        }

        static void Preview(Scene scene, Transform root, string portal, float scale, params (string, Vector3, float)[] parts)
        {
            var target = Find(scene, portal);
            if (target == null) return;
            var holder = new GameObject("Preview_" + portal);
            holder.transform.SetParent(root, false);
            holder.transform.position = InFront(target.transform.position);
            holder.transform.localScale = Vector3.one * scale;
            Place(scene, holder.transform, parts);
        }

        // перед порталом, ближе к центру купола, на платформе (верх на 0,15 м)
        static Vector3 InFront(Vector3 portal)
        {
            var flat = new Vector3(portal.x, 0f, portal.z);
            return flat.normalized * (flat.magnitude - 0.9f) + Vector3.up * 0.15f;
        }

        static float Place(Scene scene, Transform parent, (string prefab, Vector3 offset, float yaw)[] parts)
        {
            float top = parent.position.y;
            foreach (var (prefab, offset, yaw) in parts)
            {
                var asset = Load<GameObject>(Prefabs + prefab + ".prefab");
                if (asset == null) continue;
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = offset;
                go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
                foreach (var r in go.GetComponentsInChildren<Renderer>())
                    top = Mathf.Max(top, r.bounds.max.y);
            }
            return top;
        }

        static void Scatter(Scene scene, Transform root, Relief relief, System.Random rng, string prefab,
            int count, float rMin, float rMax, float sMin, float sMax)
        {
            var asset = Load<GameObject>(Prefabs + prefab + ".prefab");
            if (asset == null) return;
            var group = Group(root, "Decor_" + prefab);
            for (int i = 0; i < count; i++)
            {
                var p = RingPoint(rng, rMin, rMax);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
                go.transform.SetParent(group, false);
                go.transform.position = new Vector3(p.x, Height(relief, p.x, p.y) - 0.05f, p.y);
                go.transform.rotation = Quaternion.Euler(0f, Random01(rng) * 360f, 0f);
                go.transform.localScale = Vector3.one * Mathf.Lerp(sMin, sMax, Random01(rng));
            }
        }

        static void TulipClumps(Scene scene, Transform root, System.Random rng, int count, float rMin, float rMax)
        {
            var group = Group(root, "Decor_tulips");
            for (int i = 0; i < count; i++)
            {
                var p = FreePoint(rng, rMin, rMax);
                var clump = new GameObject("Clump_" + i);
                clump.transform.SetParent(group, false);
                clump.transform.SetPositionAndRotation(new Vector3(p.x, 0f, p.y), Quaternion.Euler(0f, Random01(rng) * 360f, 0f));
                Place(scene, clump.transform, Tulips());
            }
        }

        // ---------- окружение ----------

        static void Ground(Scene scene, Transform root, string name, Relief relief, Material mat,
            bool hideBase = true, float y = 0f)
        {
            var mesh = ReliefMesh(name + "_Relief", relief);
            var go = new GameObject("Relief");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(0f, y, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            if (!hideBase) return;
            // плоскость заготовки остаётся ради коллайдера, но её не видно: по центру рельеф ровный, на той же высоте
            foreach (var g in scene.GetRootGameObjects())
                if (g.name == "Ground" && g.TryGetComponent<MeshRenderer>(out var mr))
                    mr.enabled = false;
        }

        static void Atmosphere(Color fog, Color ambient, float start, float end)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fog;
            RenderSettings.fogStartDistance = start;
            RenderSettings.fogEndDistance = end;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient;
        }

        static void Grass(Transform root, System.Random rng, Material mat, int patches, float rMax,
            int blades, float radius, Vector2 height, float ringMin = 0f, float ringMax = 0f)
        {
            var group = Group(root, "Grass");
            for (int i = 0; i < patches; i++)
            {
                var p = ringMax > 0f ? RingPoint(rng, ringMin, ringMax) : FreePoint(rng, 0f, rMax);
                var go = new GameObject("GrassPatch_" + i);
                go.transform.SetParent(group, false);
                go.transform.position = new Vector3(p.x, 0f, p.y);
                var patch = go.AddComponent<GrassPatch>();
                var so = new SerializedObject(patch);
                so.FindProperty("grassMaterial").objectReferenceValue = mat;
                so.FindProperty("count").intValue = blades;
                so.FindProperty("radius").floatValue = radius;
                so.FindProperty("heightRange").vector2Value = height;
                so.FindProperty("widthRange").vector2Value = new Vector2(0.025f, 0.05f);
                so.FindProperty("seed").intValue = rng.Next(1, 9999);
                so.ApplyModifiedPropertiesWithoutUndo();
                patch.Rebuild();
            }
        }

        static void Rocks(Transform root, Relief relief, System.Random rng, int count, float rMin, float rMax, float sMin, float sMax)
        {
            var group = Group(root, "Rocks");
            for (int i = 0; i < count; i++)
            {
                var p = RingPoint(rng, rMin, rMax);
                var go = RockAt(group, new Vector3(p.x, Height(relief, p.x, p.y) - 0.1f, p.y), Mathf.Lerp(sMin, sMax, Random01(rng)), i);
                go.transform.rotation = Quaternion.Euler(0f, Random01(rng) * 360f, 0f);
            }
        }

        static GameObject RockAt(Transform parent, Vector3 pos, float scale, int variant)
        {
            var go = new GameObject("Rock");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<MeshFilter>().sharedMesh = rocks[variant % rocks.Length];
            go.AddComponent<MeshRenderer>().sharedMaterial = Load<Material>("Assets/OLAM/Materials/Mat_Rock.mat");
            return go;
        }

        // ---------- рельеф и камни ----------

        static float Height(Relief r, float x, float z)
        {
            float d = Mathf.Sqrt(x * x + z * z);
            float blend = Mathf.SmoothStep(0f, 1f, (d - r.flat) / (r.flat * 1.5f));
            float n = Mathf.PerlinNoise(x * r.freq + r.seed * 13.1f, z * r.freq + r.seed * 7.7f);
            if (r.dunes) n = 1f - Mathf.Abs(n * 2f - 1f);
            float h = n * r.height * blend;
            if (r.ringHeight > 0f)
            {
                float ring = Mathf.SmoothStep(0f, 1f, (d - r.ringStart) / 30f);
                h += ring * r.ringHeight * (0.45f + 0.55f * Mathf.PerlinNoise(x * 0.025f + 5f, z * 0.025f + 9f));
            }
            if (r.riverWidth > 0f)
            {
                float k = Mathf.Abs(z - r.riverZ) / r.riverWidth;   // 0 — середина русла
                h = Mathf.Lerp(-0.9f, h, Mathf.SmoothStep(0f, 1f, k * 2f - 0.6f));
            }
            return h;
        }

        static Mesh ReliefMesh(string name, Relief r)
        {
            int n = r.res + 1;
            var v = new Vector3[n * n];
            var uv = new Vector2[n * n];
            float half = r.size * 0.5f;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float x = -half + r.size * i / r.res, z = -half + r.size * j / r.res;
                    v[j * n + i] = new Vector3(x, Height(r, x, z), z);
                    uv[j * n + i] = new Vector2(x, z) * 0.125f;
                }
            var t = new int[r.res * r.res * 6];
            int k = 0;
            for (int j = 0; j < r.res; j++)
                for (int i = 0; i < r.res; i++)
                {
                    int a = j * n + i, b = a + 1, c = a + n, d = c + 1;
                    t[k++] = a; t[k++] = c; t[k++] = b;
                    t[k++] = b; t[k++] = c; t[k++] = d;
                }
            return SaveMesh(name, v, uv, t);
        }

        /// <summary>Камень: икосфера с шумом, приплюснутая, плоские грани (низкополигональный стиль).</summary>
        static Mesh RockMesh(string name, int seed)
        {
            float g = (1f + Mathf.Sqrt(5f)) / 2f;
            var verts = new List<Vector3>
            {
                new(-1, g, 0), new(1, g, 0), new(-1, -g, 0), new(1, -g, 0), new(0, -1, g), new(0, 1, g),
                new(0, -1, -g), new(0, 1, -g), new(g, 0, -1), new(g, 0, 1), new(-g, 0, -1), new(-g, 0, 1),
            };
            var faces = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
            };
            for (int step = 0; step < 2; step++)
            {
                var cache = new Dictionary<long, int>();
                var next = new List<int>();
                int Mid(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (cache.TryGetValue(key, out int m)) return m;
                    verts.Add((verts[a] + verts[b]) * 0.5f);
                    cache[key] = verts.Count - 1;
                    return verts.Count - 1;
                }
                for (int f = 0; f < faces.Count; f += 3)
                {
                    int a = faces[f], b = faces[f + 1], c = faces[f + 2];
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                faces = next;
            }
            for (int i = 0; i < verts.Count; i++)
            {
                var p = verts[i].normalized;
                float noise = (Mathf.PerlinNoise(p.x * 1.7f + seed, p.y * 1.7f + seed * 0.37f)
                             + Mathf.PerlinNoise(p.y * 1.7f + seed * 0.71f, p.z * 1.7f + seed)
                             + Mathf.PerlinNoise(p.z * 1.7f + seed * 0.13f, p.x * 1.7f + seed * 0.53f)) / 3f;
                p *= 0.75f + 0.5f * noise;
                if (p.y < -0.25f) p.y = -0.25f + (p.y + 0.25f) * 0.25f;
                p.y *= 0.7f;
                verts[i] = p * 0.5f;
            }
            var v = new Vector3[faces.Count];
            var idx = new int[faces.Count];
            for (int f = 0; f < faces.Count; f += 3)
            {
                Vector3 a = verts[faces[f]], b = verts[faces[f + 1]], c = verts[faces[f + 2]];
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f) (b, c) = (c, b);
                v[f] = a; v[f + 1] = b; v[f + 2] = c;
                idx[f] = f; idx[f + 1] = f + 1; idx[f + 2] = f + 2;
            }
            float minY = float.MaxValue;
            foreach (var p in v) minY = Mathf.Min(minY, p.y);
            var uv = new Vector2[v.Length];
            for (int i = 0; i < v.Length; i++)
            {
                v[i].y -= minY;
                uv[i] = new Vector2(v[i].x + v[i].y, v[i].z + v[i].y);
            }
            return SaveMesh(name, v, uv, idx);
        }

        static Mesh SaveMesh(string name, Vector3[] v, Vector2[] uv, int[] t)
        {
            string path = $"{Env}/Meshes/{name}.asset";
            var mesh = Load<Mesh>(path);
            bool fresh = mesh == null;
            if (fresh) mesh = new Mesh { name = name };
            mesh.Clear();
            mesh.indexFormat = v.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.vertices = v;
            mesh.uv = uv;
            mesh.triangles = t;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            if (fresh) AssetDatabase.CreateAsset(mesh, path);
            else EditorUtility.SetDirty(mesh);
            return mesh;
        }

        // ---------- материалы ----------

        static Material GroundMat(string name, Color baseColor, Color shadow, float scale)
        {
            string path = $"{Env}/Materials/{name}.mat";
            var m = Load<Material>(path);
            if (m == null)
            {
                var shader = Shader.Find("OLAM/Sand Ground") ?? Shader.Find("Universal Render Pipeline/Lit");
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_ShadowColor", shadow);
            m.SetFloat("_Scale", scale);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material GrassMat(string name, Color baseColor, Color tip)
        {
            string path = $"{Env}/Materials/{name}.mat";
            var m = Load<Material>(path);
            if (m == null)
            {
                m = new Material(Load<Material>("Assets/OLAM/Materials/Mat_GrassChatkal.mat")) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_TipColor", tip);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------- снимки ----------

        static void Capture(Scene scene, string name)
        {
            Camera cam = null;
            foreach (var g in scene.GetRootGameObjects())
            {
                cam = g.GetComponentInChildren<Camera>(true);
                if (cam != null) break;
            }
            if (cam == null) return;

            // другие открытые сцены на время снимка прячутся и возвращаются как были
            var hidden = new List<GameObject>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var other = SceneManager.GetSceneAt(i);
                if (other == scene || !other.isLoaded) continue;
                foreach (var g in other.GetRootGameObjects())
                    if (g.activeSelf) { g.SetActive(false); hidden.Add(g); }
            }
            var overview = new GameObject("CaptureOverview").AddComponent<Camera>();
            // трава рисуется в LateUpdate (DrawMeshInstanced); в редакторе снимок его не вызывает — вызываем сами
            var lateUpdate = typeof(GrassPatch).GetMethod("LateUpdate",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            try
            {
                foreach (var g in scene.GetRootGameObjects())
                    foreach (var patch in g.GetComponentsInChildren<GrassPatch>())
                        lateUpdate?.Invoke(patch, null);
                Shoot(cam, $"{CaptureDir}/{name}.png");
                overview.CopyFrom(cam);
                overview.transform.SetPositionAndRotation(new Vector3(0f, 7f, -18f), Quaternion.Euler(18f, 0f, 0f));
                Shoot(overview, $"{CaptureDir}/{name}_overview.png");
            }
            finally
            {
                Object.DestroyImmediate(overview.gameObject);
                foreach (var g in hidden) g.SetActive(true);
            }
        }

        static void Shoot(Camera cam, string path)
        {
            var rt = new RenderTexture(1280, 720, 24);
            var prev = cam.targetTexture;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                cam.targetTexture = prev;
                RenderTexture.active = null;
                rt.Release();
                Object.DestroyImmediate(rt);
            }
        }

        // ---------- служебное ----------

        static void InScene(string name, bool save, System.Action<Scene> body)
        {
            string path = $"Assets/OLAM/Scenes/{name}.unity";
            if (!File.Exists(path)) return;
            var scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            var active = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);   // RenderSettings и новые объекты — в этой сцене
            try
            {
                body(scene);
                if (save) EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (active.IsValid() && active != scene) SceneManager.SetActiveScene(active);
                if (!wasLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        static Transform NewRoot(Scene scene)
        {
            foreach (var g in scene.GetRootGameObjects())
                if (g.name == RootName) Object.DestroyImmediate(g);
            var root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);
            return root.transform;
        }

        static Transform Group(Transform root, string name)
        {
            var g = new GameObject(name);
            g.transform.SetParent(root, false);
            return g.transform;
        }

        static GameObject Find(Scene scene, string name)
        {
            foreach (var g in scene.GetRootGameObjects())
                foreach (var t in g.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            return null;
        }

        static float Random01(System.Random rng) => (float)rng.NextDouble();

        static Vector2 RingPoint(System.Random rng, float rMin, float rMax)
        {
            float a = Random01(rng) * Mathf.PI * 2f;
            float d = Mathf.Sqrt(Mathf.Lerp(rMin * rMin, rMax * rMax, Random01(rng)));
            return new Vector2(Mathf.Sin(a) * d, Mathf.Cos(a) * d);
        }

        // точка вне площадки с видами, фильтрами и порталом (x −2,4…3,2, z −2,2…4,4) и не у самой камеры (z < −6)
        static Vector2 FreePoint(System.Random rng, float rMin, float rMax)
        {
            for (int i = 0; i < 50; i++)
            {
                var p = RingPoint(rng, rMin, rMax);
                bool onStage = p.x > -2.4f && p.x < 3.2f && p.y > -2.2f && p.y < 4.4f;
                if (!onStage && p.y > -6f) return p;
            }
            return RingPoint(rng, Mathf.Max(rMin, 6f), rMax);
        }

        static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

        static void EnsureFolder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{name}"))
                AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
