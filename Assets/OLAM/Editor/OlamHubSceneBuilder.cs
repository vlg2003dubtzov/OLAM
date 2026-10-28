#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Olam.Core;

namespace Olam.EditorTools
{
    /// <summary>
    /// Хаб OLAM + порталы между сценами + Build Settings.
    /// </summary>
    public static class OlamHubSceneBuilder
    {
        const string HubPath = "Assets/OLAM/Scenes/OlamHub.unity";

        struct PortalDef
        {
            public string scene;
            public string name;
            public Vector3 pos;
            public Color color;
            public LText label;
        }

        [MenuItem("OLAM/Build Hub + Wire Portals")]
        public static void Build()
        {
            EnsureFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.shadows = LightShadows.None;
                sun.intensity = 1.0f;
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.5f, 0.55f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Hub_Floor";
            floor.transform.localScale = new Vector3(3f, 1f, 3f);
            floor.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_HubFloor", new Color(0.22f, 0.28f, 0.32f));

            var systems = new GameObject("OLAM_Systems");
            systems.AddComponent<Localization>();

            // Title
            var titleGo = new GameObject("HubTitle");
            titleGo.transform.position = new Vector3(0f, 2.4f, 2f);
            titleGo.transform.localScale = Vector3.one * 0.012f;
            titleGo.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var title = MakeTmp(titleGo.transform, "Text", Vector2.zero, 52);
            title.rectTransform.sizeDelta = new Vector2(900, 160);
            title.text = "O'LAM · Карта пространств\nПрирода → Кухня → История\nЛКМ по порталу — войти";

            // Уровень 1: разделы. Природа ведёт в Купол (5 биомов), не сразу в сцену.
            var portals = new[]
            {
                new PortalDef
                {
                    scene = "NatureDome",
                    name = "Portal_Nature",
                    pos = new Vector3(-3f, 0.6f, 1.5f),
                    color = new Color(0.3f, 0.65f, 0.4f),
                    label = new LText("Природа\nКупол биомов", "Tabiat\nBiomlar", "Nature\nBiome Dome")
                },
                new PortalDef
                {
                    scene = "Tandoor",
                    name = "Portal_Tandoor",
                    pos = new Vector3(0f, 0.6f, 2.5f),
                    color = new Color(0.85f, 0.45f, 0.2f),
                    label = new LText("Кухня\nТандыр", "Oshxona\nTandir", "Kitchen\nTandoor")
                },
                new PortalDef
                {
                    scene = "ToprakKala",
                    name = "Portal_ToprakKala",
                    pos = new Vector3(3f, 0.6f, 1.5f),
                    color = new Color(0.75f, 0.62f, 0.3f),
                    label = new LText("История\nТопрак-Кала", "Tarix\nTopraqqal’a", "History\nToprak-Kala")
                }
            };

            foreach (var p in portals)
                CreatePortal(p, null);

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 2.0f, -4.5f);
                cam.transform.LookAt(new Vector3(0f, 0.8f, 1.5f));
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.35f, 0.45f, 0.55f);
            }

            EditorSceneManager.SaveScene(scene, HubPath);

            // Портал «назад в хаб» в каждой обучающей сцене
            AddReturnPortal("Assets/OLAM/Scenes/Tandoor.unity", new Vector3(-2.5f, 0.5f, -1.5f));
            AddReturnPortal("Assets/OLAM/Scenes/ChatkalNightTrail.unity", new Vector3(2.5f, 0.5f, -0.5f));
            AddReturnPortal("Assets/OLAM/Scenes/ToprakKala.unity", new Vector3(-3.5f, 0.5f, -1.5f));

            // Вернуться в хаб и зарегистрировать сцены (после сохранения Hub)
            EditorSceneManager.OpenScene(HubPath);
            RegisterBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("OLAM: хаб и порталы готовы. Стартовая сцена — OlamHub (в Build Settings).");
        }

        static void CreatePortal(PortalDef def, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = def.name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = def.pos;
            go.transform.localScale = new Vector3(1.2f, 0.6f, 1.2f);
            go.GetComponent<MeshRenderer>().sharedMaterial = CreateUrpLit($"Mat_{def.name}", def.color);

            go.AddComponent<OlamInteractable>();
            var portal = go.AddComponent<ScenePortal>();
            SetString(portal, "sceneName", def.scene);

            var labelRoot = new GameObject("Label");
            labelRoot.transform.SetParent(go.transform, false);
            labelRoot.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            labelRoot.transform.localScale = Vector3.one * 0.015f;
            labelRoot.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var tmp = MakeTmp(labelRoot.transform, "Text", Vector2.zero, 36);
            tmp.rectTransform.sizeDelta = new Vector2(500, 160);
            tmp.text = def.label.ru;

            var so = new SerializedObject(portal);
            so.FindProperty("labelText").objectReferenceValue = tmp;
            so.FindProperty("label").FindPropertyRelative("ru").stringValue = def.label.ru;
            so.FindProperty("label").FindPropertyRelative("uz").stringValue = def.label.uz;
            so.FindProperty("label").FindPropertyRelative("en").stringValue = def.label.en;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddReturnPortal(string scenePath, Vector3 pos)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            // удалить старый, если пересобираем
            var existing = GameObject.Find("Portal_ReturnHub");
            if (existing != null) Object.DestroyImmediate(existing);

            CreatePortal(new PortalDef
            {
                scene = "OlamHub",
                name = "Portal_ReturnHub",
                pos = pos,
                color = new Color(0.3f, 0.75f, 0.55f),
                label = new LText("← Хаб OLAM", "← OLAM markaz", "← OLAM Hub")
            }, null);

            EditorSceneManager.SaveScene(scene);
        }

        static void RegisterBuildSettings()
        {
            var paths = new[]
            {
                HubPath,
                "Assets/OLAM/Scenes/NatureDome.unity",
                "Assets/OLAM/Scenes/Biome_Desert.unity",
                "Assets/OLAM/Scenes/Biome_Adyr.unity",
                "Assets/OLAM/Scenes/Biome_Mountains.unity",
                "Assets/OLAM/Scenes/Biome_Tugai.unity",
                "Assets/OLAM/Scenes/ChatkalNightTrail.unity",
                "Assets/OLAM/Scenes/Tandoor.unity",
                "Assets/OLAM/Scenes/ToprakKala.unity"
            };

            var list = new List<EditorBuildSettingsScene>();
            foreach (var path in paths)
            {
                var abs = System.IO.Path.GetFullPath(path);
                if (System.IO.File.Exists(abs) || AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                    list.Add(new EditorBuildSettingsScene(path, true));
                else
                    Debug.LogWarning($"OLAM: сцена не найдена для Build Settings: {path}");
            }

            // сохранить GetStarted если был
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.path != null && s.path.Contains("GetStarted") && list.TrueForAll(x => x.path != s.path))
                    list.Add(s);
            }

            EditorBuildSettings.scenes = list.ToArray();
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/OLAM/Scenes"))
                AssetDatabase.CreateFolder("Assets/OLAM", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/OLAM/Materials"))
                AssetDatabase.CreateFolder("Assets/OLAM", "Materials");
        }

        static Material CreateUrpLit(string name, Color color)
        {
            string path = $"Assets/OLAM/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                return existing;
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("URP/Lit")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static TMP_Text MakeTmp(Transform parent, string name, Vector2 anchored, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.rectTransform.anchoredPosition = anchored;
            tmp.rectTransform.sizeDelta = new Vector2(700, 60);
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }

        static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
