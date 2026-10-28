#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Olam.Core;
using Olam.Nature;

namespace Olam.EditorTools
{
    /// <summary>
    /// Купол Природы: 5 биом-порталов + лёгкие stub-сцены с эко-сканером.
    /// </summary>
    public static class OlamNatureDomeBuilder
    {
        const string DomePath = "Assets/OLAM/Scenes/NatureDome.unity";
        const string NatureSO = "Assets/OLAM/ScriptableObjects/Nature";

        struct PortalDef
        {
            public string scene, name;
            public Vector3 pos;
            public Color color;
            public LText label;
        }

        [MenuItem("OLAM/Build Nature Dome + Biome Stubs")]
        public static void BuildAll()
        {
            OlamNatureContentSeeder.Seed();
            BuildDome();
            BuildBiomeStub("Biome_Desert", new Color(0.85f, 0.7f, 0.4f), "Пустыня · Кызылкум",
                new[] { "saxaul_black", "juzgun", "cistanche", "desert_monitor", "toadhead_agama" },
                SpeciesCategory.TreesShrubs);
            BuildBiomeStub("Biome_Adyr", new Color(0.55f, 0.7f, 0.35f), "Адыры · весенний ковер",
                new[] { "tulip_greigii", "eremurus", "tulip_uzbekistanica" },
                SpeciesCategory.HerbsFlowers);
            BuildBiomeStub("Biome_Mountains", new Color(0.35f, 0.5f, 0.32f), "Горы · арча и яблоня",
                new[] { "archa_juniper", "malus_sieversii" },
                SpeciesCategory.TreesShrubs);
            BuildBiomeStub("Biome_Tugai", new Color(0.3f, 0.5f, 0.38f), "Тугаи · Амударья",
                new[] { "turanga", "bukhara_deer" },
                SpeciesCategory.TreesShrubs);
            WireChatkalScanner();
            RegisterBuild();
            EditorSceneManager.OpenScene(DomePath);
            AssetDatabase.SaveAssets();
            Debug.Log("OLAM: NatureDome + biome stubs готовы.");
        }

        static void BuildDome()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null) { sun.shadows = LightShadows.None; sun.intensity = 1.1f; }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.55f, 0.5f);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Dome_Platform";
            floor.transform.position = new Vector3(0f, 0f, 0f);
            floor.transform.localScale = new Vector3(8f, 0.15f, 8f);
            floor.GetComponent<MeshRenderer>().sharedMaterial =
                CreateMat("Mat_DomeFloor", new Color(0.2f, 0.32f, 0.28f));

            var systems = new GameObject("OLAM_Systems");
            systems.AddComponent<Localization>();

            var titleGo = new GameObject("DomeTitle");
            titleGo.transform.position = new Vector3(0f, 2.6f, 0f);
            titleGo.transform.localScale = Vector3.one * 0.01f;
            titleGo.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var t = MakeTmp(titleGo.transform, "Text", Vector2.zero, 48);
            t.rectTransform.sizeDelta = new Vector2(1000, 180);
            t.text = "Купол Природы\nВыбери биом · ЛКМ";

            // Полукруг из 5 порталов
            var biomes = new[]
            {
                ("Biome_Desert", "Пустыни\nЧўл", "Cho‘llar", "Deserts", new Color(0.9f, 0.72f, 0.35f), -72f),
                ("Biome_Adyr", "Адыры\nПредгорья", "Adirlar", "Foothills", new Color(0.85f, 0.35f, 0.45f), -36f),
                ("Biome_Mountains", "Горы\nТау", "Tog‘lar", "Mountains", new Color(0.35f, 0.55f, 0.35f), 0f),
                ("ChatkalNightTrail", "Яйлау\nВысокогорья", "Yaylov", "Highlands", new Color(0.7f, 0.82f, 0.95f), 36f),
                ("Biome_Tugai", "Тугаи\nПойма", "Tugaylar", "Tugai", new Color(0.25f, 0.55f, 0.4f), 72f)
            };

            float radius = 3.2f;
            foreach (var (sceneName, ru, uz, en, col, angle) in biomes)
            {
                float rad = angle * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Sin(rad) * radius, 0.7f, Mathf.Cos(rad) * radius);
                CreatePortal(new PortalDef
                {
                    scene = sceneName,
                    name = "Portal_" + sceneName,
                    pos = pos,
                    color = col,
                    label = new LText(ru, uz, en)
                });
            }

            CreatePortal(new PortalDef
            {
                scene = "OlamHub",
                name = "Portal_ReturnHub",
                pos = new Vector3(0f, 0.5f, -3.5f),
                color = new Color(0.3f, 0.75f, 0.55f),
                label = new LText("← Хаб пространств", "← Markaz", "← Spaces Hub")
            });

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 2.2f, -5f);
                cam.transform.LookAt(new Vector3(0f, 1f, 0f));
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.45f, 0.65f, 0.75f);
            }

            EditorSceneManager.SaveScene(scene, DomePath);
        }

        static void BuildBiomeStub(string sceneName, Color ground, string title,
            string[] entryIds, SpeciesCategory defaultFilter)
        {
            string path = $"Assets/OLAM/Scenes/{sceneName}.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null) sun.shadows = LightShadows.None;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Ground";
            floor.transform.localScale = new Vector3(3.5f, 1f, 3.5f);
            var sand = AssetDatabase.LoadAssetAtPath<Material>("Assets/OLAM/Materials/Mat_DesertSand.mat");
            floor.GetComponent<MeshRenderer>().sharedMaterial = sand != null ? sand : CreateMat($"Mat_{sceneName}_Ground", ground);

            var systems = new GameObject("OLAM_Systems");
            systems.AddComponent<Localization>();
            systems.AddComponent<Herbarium>();
            systems.AddComponent<EcoScanner>();
            var filter = systems.AddComponent<BiomeCategoryFilter>();
            var filterSo = new SerializedObject(filter);
            filterSo.FindProperty("current").enumValueIndex = (int)defaultFilter;
            filterSo.ApplyModifiedPropertiesWithoutUndo();

            var quests = systems.AddComponent<QuestManager>();
            var qso = new SerializedObject(quests);
            var goals = qso.FindProperty("goals");
            goals.arraySize = 1;
            var g = goals.GetArrayElementAtIndex(0);
            g.FindPropertyRelative("id").stringValue = "eco_scan";
            g.FindPropertyRelative("title").FindPropertyRelative("ru").stringValue = "Отсканируй виды";
            g.FindPropertyRelative("title").FindPropertyRelative("uz").stringValue = "Turlarni skaner qil";
            g.FindPropertyRelative("title").FindPropertyRelative("en").stringValue = "Scan species";
            g.FindPropertyRelative("required").intValue = Mathf.Min(3, entryIds.Length);
            qso.ApplyModifiedPropertiesWithoutUndo();

            CreateHologramRig();

            // фильтр-кнопки
            var pad = new GameObject("CategoryPad");
            pad.transform.position = new Vector3(-1.6f, 1.4f, 0.5f);
            pad.transform.localScale = Vector3.one * 0.008f;
            pad.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            MakeFilterButton(pad.transform, "🌳 Деревья", new Vector2(0, 80), () => { });
            // кнопки через OlamInteractable на 3D-кубиках рядом
            CreateFilterCube("Filter_Trees", new Vector3(-1.8f, 0.3f, 1.2f), new Color(0.3f, 0.55f, 0.25f), SpeciesCategory.TreesShrubs);
            CreateFilterCube("Filter_Herbs", new Vector3(-1.8f, 0.3f, 2.0f), new Color(0.75f, 0.35f, 0.45f), SpeciesCategory.HerbsFlowers);
            CreateFilterCube("Filter_Fauna", new Vector3(-1.8f, 0.3f, 2.8f), new Color(0.55f, 0.4f, 0.25f), SpeciesCategory.Fauna);

            var tip = new GameObject("Tip");
            tip.transform.position = new Vector3(0f, 2.2f, 2.5f);
            tip.transform.localScale = Vector3.one * 0.01f;
            tip.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var tipT = MakeTmp(tip.transform, "T", Vector2.zero, 36);
            tipT.rectTransform.sizeDelta = new Vector2(900, 120);
            tipT.text = title + "\nЭко-сканер: клик по виду · фильтр слева";

            for (int i = 0; i < entryIds.Length; i++)
            {
                var entry = AssetDatabase.LoadAssetAtPath<EncyclopediaEntry>($"{NatureSO}/Entry_{entryIds[i]}.asset");
                float x = -1.2f + (i % 3) * 1.4f;
                float z = 1.5f + (i / 3) * 1.6f;
                SpawnScannable(entryIds[i], entry, new Vector3(x, 0.35f, z));
            }

            CreatePortal(new PortalDef
            {
                scene = "NatureDome",
                name = "Portal_ReturnDome",
                pos = new Vector3(2.5f, 0.5f, -1.5f),
                color = new Color(0.3f, 0.7f, 0.55f),
                label = new LText("← Купол Природы", "← Tabiat gumbazi", "← Nature Dome")
            });

            EditorSceneManager.SaveScene(scene, path);
        }

        static void WireChatkalScanner()
        {
            const string path = "Assets/OLAM/Scenes/ChatkalNightTrail.unity";
            if (!File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var systems = GameObject.Find("OLAM_Systems");
            if (systems == null) systems = new GameObject("OLAM_Systems");
            if (systems.GetComponent<EcoScanner>() == null) systems.AddComponent<EcoScanner>();
            if (systems.GetComponent<BiomeCategoryFilter>() == null) systems.AddComponent<BiomeCategoryFilter>();
            if (systems.GetComponent<Herbarium>() == null) systems.AddComponent<Herbarium>();

            if (GameObject.Find("HologramCard") == null)
                CreateHologramRig();

            // добавить сканируемых барса-плейсхолдер у AnimalSpawn
            var leopard = AssetDatabase.LoadAssetAtPath<EncyclopediaEntry>($"{NatureSO}/Entry_snow_leopard.asset");
            if (leopard != null && GameObject.Find("Scan_snow_leopard") == null)
                SpawnScannable("snow_leopard", leopard, new Vector3(0.5f, 0.4f, 11f));

            var juniper = AssetDatabase.LoadAssetAtPath<EncyclopediaEntry>($"{NatureSO}/Entry_archa_juniper.asset");
            var plant = GameObject.Find("Plant_Juniper");
            if (plant != null && plant.GetComponent<ScannableOrganism>() == null)
            {
                var scan = plant.AddComponent<ScannableOrganism>();
                var so = new SerializedObject(scan);
                so.FindProperty("entry").objectReferenceValue = juniper;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.SaveScene(scene);
        }

        static void SpawnScannable(string id, EncyclopediaEntry entry, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Scan_" + id;
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.35f, 0.45f, 0.35f);
            var col = entry != null && entry.Category == SpeciesCategory.Fauna
                ? new Color(0.55f, 0.42f, 0.28f)
                : new Color(0.25f, 0.5f, 0.2f);
            var foliage = AssetDatabase.LoadAssetAtPath<Material>("Assets/OLAM/Materials/Mat_FoliageWind.mat");
            if (foliage != null)
            {
                var m = new Material(foliage);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                go.GetComponent<MeshRenderer>().sharedMaterial = m;
            }

            var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beacon.name = "Beacon";
            beacon.transform.SetParent(go.transform, false);
            beacon.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            beacon.transform.localScale = new Vector3(0.15f, 0.6f, 0.15f);
            Object.DestroyImmediate(beacon.GetComponent<Collider>());
            beacon.GetComponent<MeshRenderer>().sharedMaterial = CreateMat("Mat_Beacon", new Color(1f, 0.95f, 0.4f, 0.8f));
            beacon.SetActive(false);

            go.AddComponent<OlamInteractable>();
            var scan = go.AddComponent<ScannableOrganism>();
            var so = new SerializedObject(scan);
            so.FindProperty("entry").objectReferenceValue = entry;
            so.FindProperty("highlightBeacon").objectReferenceValue = beacon;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void CreateFilterCube(string name, Vector3 pos, Color color, SpeciesCategory cat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.35f;
            go.GetComponent<MeshRenderer>().sharedMaterial = CreateMat("Mat_" + name, color);
            go.AddComponent<OlamInteractable>();
            var bridge = go.AddComponent<CategoryFilterButton>();
            var so = new SerializedObject(bridge);
            so.FindProperty("category").enumValueIndex = (int)cat;
            so.ApplyModifiedPropertiesWithoutUndo();

            var label = new GameObject("L");
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            label.transform.localScale = Vector3.one * 0.04f;
            label.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var tmp = MakeTmp(label.transform, "T", Vector2.zero, 28);
            tmp.text = cat switch
            {
                SpeciesCategory.TreesShrubs => "🌳",
                SpeciesCategory.HerbsFlowers => "🌸",
                _ => "🦎"
            };
        }

        static void CreateHologramRig()
        {
            var go = new GameObject("HologramCard");
            go.transform.position = new Vector3(0f, 1.6f, 1.5f);
            go.transform.localScale = Vector3.one * 0.008f;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            go.AddComponent<UnityEngine.UI.CanvasScaler>();
            go.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var title = MakeTmp(go.transform, "Title", new Vector2(0, 140), 40);
            title.rectTransform.sizeDelta = new Vector2(700, 100);
            var status = MakeTmp(go.transform, "Status", new Vector2(0, 70), 28);
            var body = MakeTmp(go.transform, "Body", new Vector2(0, -20), 26);
            body.rectTransform.sizeDelta = new Vector2(680, 160);
            var power = MakeTmp(go.transform, "Power", new Vector2(0, -120), 24);
            power.color = new Color(1f, 0.92f, 0.55f);
            var interact = MakeTmp(go.transform, "Interact", new Vector2(0, -170), 24);
            interact.color = new Color(0.7f, 0.95f, 1f);

            var card = go.AddComponent<HologramCard>();
            var so = new SerializedObject(card);
            so.FindProperty("canvas").objectReferenceValue = canvas;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("statusText").objectReferenceValue = status;
            so.FindProperty("bodyText").objectReferenceValue = body;
            so.FindProperty("powerText").objectReferenceValue = power;
            so.FindProperty("interactText").objectReferenceValue = interact;
            so.ApplyModifiedPropertiesWithoutUndo();
            canvas.enabled = false;
        }

        static void CreatePortal(PortalDef def)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = def.name;
            go.transform.position = def.pos;
            go.transform.localScale = Vector3.one * 1.1f;
            go.GetComponent<MeshRenderer>().sharedMaterial = CreateMat($"Mat_{def.name}", def.color);
            go.AddComponent<OlamInteractable>();
            var portal = go.AddComponent<ScenePortal>();
            var so = new SerializedObject(portal);
            so.FindProperty("sceneName").stringValue = def.scene;

            var labelRoot = new GameObject("Label");
            labelRoot.transform.SetParent(go.transform, false);
            labelRoot.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            labelRoot.transform.localScale = Vector3.one * 0.02f;
            labelRoot.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var tmp = MakeTmp(labelRoot.transform, "Text", Vector2.zero, 32);
            tmp.rectTransform.sizeDelta = new Vector2(420, 140);
            tmp.text = def.label.ru;
            so.FindProperty("labelText").objectReferenceValue = tmp;
            so.FindProperty("label").FindPropertyRelative("ru").stringValue = def.label.ru;
            so.FindProperty("label").FindPropertyRelative("uz").stringValue = def.label.uz;
            so.FindProperty("label").FindPropertyRelative("en").stringValue = def.label.en;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void RegisterBuild()
        {
            var paths = new[]
            {
                "Assets/OLAM/Scenes/OlamHub.unity",
                DomePath,
                "Assets/OLAM/Scenes/Biome_Desert.unity",
                "Assets/OLAM/Scenes/Biome_Adyr.unity",
                "Assets/OLAM/Scenes/Biome_Mountains.unity",
                "Assets/OLAM/Scenes/Biome_Tugai.unity",
                "Assets/OLAM/Scenes/ChatkalNightTrail.unity",
                "Assets/OLAM/Scenes/Tandoor.unity",
                "Assets/OLAM/Scenes/ToprakKala.unity"
            };
            var list = new List<EditorBuildSettingsScene>();
            foreach (var p in paths)
            {
                if (File.Exists(p) || AssetDatabase.LoadAssetAtPath<SceneAsset>(p) != null)
                    list.Add(new EditorBuildSettingsScene(p, true));
            }
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.path != null && list.TrueForAll(x => x.path != s.path))
                    list.Add(s);
            }
            EditorBuildSettings.scenes = list.ToArray();
        }

        static Material CreateMat(string name, Color color)
        {
            string path = $"Assets/OLAM/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) { existing.color = color; return existing; }
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("URP/Lit") ?? Shader.Find("Standard");
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

        static void MakeFilterButton(Transform parent, string text, Vector2 pos, System.Action _)
        {
            var tmp = MakeTmp(parent, text, pos, 28);
            tmp.text = text;
        }
    }
}
#endif
