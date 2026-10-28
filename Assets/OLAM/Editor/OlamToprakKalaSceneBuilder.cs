#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Olam.Core;
using Olam.History;

namespace Olam.EditorTools
{
    /// <summary>
    /// Меню: OLAM → Build Toprak-Kala Learning Space
    /// Руины ↔ реконструкция, сборка деталей в гнёзда.
    /// </summary>
    public static class OlamToprakKalaSceneBuilder
    {
        const string ScenePath = "Assets/OLAM/Scenes/ToprakKala.unity";
        const string EntryPath = "Assets/OLAM/ScriptableObjects/Entry_ToprakKala.asset";

        [MenuItem("OLAM/Build Toprak-Kala Learning Space")]
        public static void Build()
        {
            EnsureFolders();
            var entry = CreateOrLoadEntry();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var sun = Object.FindFirstObjectByType<Light>();
            if (sun != null)
            {
                sun.shadows = LightShadows.None;
                sun.intensity = 1.15f;
                sun.transform.rotation = Quaternion.Euler(42f, -25f, 0f);
            }
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.7f, 0.62f, 0.48f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Desert_Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            ground.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_DesertSand", new Color(0.78f, 0.65f, 0.42f));

            var systems = new GameObject("OLAM_Systems");
            systems.AddComponent<Localization>();
            var quests = systems.AddComponent<QuestManager>();
            var qso = new SerializedObject(quests);
            var goals = qso.FindProperty("goals");
            goals.arraySize = 1;
            var g0 = goals.GetArrayElementAtIndex(0);
            g0.FindPropertyRelative("id").stringValue = "rebuild_site";
            g0.FindPropertyRelative("title").FindPropertyRelative("ru").stringValue = "Собери макет крепости";
            g0.FindPropertyRelative("title").FindPropertyRelative("uz").stringValue = "Qal’a maketini yig‘";
            g0.FindPropertyRelative("title").FindPropertyRelative("en").stringValue = "Rebuild the fortress model";
            g0.FindPropertyRelative("required").intValue = 1;
            qso.ApplyModifiedPropertiesWithoutUndo();

            // Info panel
            var panelGo = new GameObject("InfoPanel");
            panelGo.transform.position = new Vector3(2.2f, 1.8f, 1.5f);
            panelGo.transform.localScale = Vector3.one * 0.008f;
            var canvas = panelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            panelGo.AddComponent<CanvasScaler>();
            panelGo.AddComponent<GraphicRaycaster>();
            var title = MakeTmp(panelGo.transform, "Title", new Vector2(0, 80), 42);
            var sci = MakeTmp(panelGo.transform, "Scientific", new Vector2(0, 30), 26);
            var body = MakeTmp(panelGo.transform, "Body", new Vector2(0, -60), 24);
            body.rectTransform.sizeDelta = new Vector2(700, 180);
            var panel = panelGo.AddComponent<InfoPanel>();
            Wire(panel, "canvas", canvas);
            Wire(panel, "titleText", title);
            Wire(panel, "scientificText", sci);
            Wire(panel, "bodyText", body);
            canvas.enabled = false;

            // Карточка-триггер энциклопедии
            var card = GameObject.CreatePrimitive(PrimitiveType.Cube);
            card.name = "Encyclopedia_Plaque";
            card.transform.position = new Vector3(2.2f, 1.0f, 1.5f);
            card.transform.localScale = new Vector3(0.6f, 0.8f, 0.05f);
            card.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_Plaque", new Color(0.55f, 0.4f, 0.25f));
            card.AddComponent<OlamInteractable>();
            // простой показ при выборе через InfoPanel helper
            var plaque = card.AddComponent<ShowEncyclopediaOnSelect>();
            Wire(plaque, "entry", entry);

            // --- Руины ---
            var ruinsRoot = new GameObject("Ruins");
            CreateRuinBlock(ruinsRoot.transform, "Ruin_Wall_L", new Vector3(-1.2f, 0.4f, 3f), new Vector3(0.4f, 0.8f, 2.2f), 0.55f);
            CreateRuinBlock(ruinsRoot.transform, "Ruin_Wall_R", new Vector3(1.2f, 0.25f, 3.2f), new Vector3(0.4f, 0.5f, 1.6f), 0.5f);
            CreateRuinBlock(ruinsRoot.transform, "Ruin_Column_Stump", new Vector3(0f, 0.2f, 4.2f), new Vector3(0.35f, 0.4f, 0.35f), 0.45f);

            // --- Реконструкция (слоты + собранный вид скрыт до fill) ---
            var reconRoot = new GameObject("Reconstruction");
            reconRoot.SetActive(true);

            var slotIds = new[] { "wall_n", "wall_e", "column", "arch" };
            var slotPos = new[]
            {
                new Vector3(0f, 0.6f, 4.8f),
                new Vector3(1.6f, 0.6f, 3.2f),
                new Vector3(0f, 0.7f, 3.5f),
                new Vector3(0f, 1.35f, 4.8f)
            };
            var slotScale = new[]
            {
                new Vector3(2.4f, 1.2f, 0.25f),
                new Vector3(0.25f, 1.2f, 2.0f),
                new Vector3(0.4f, 1.4f, 0.4f),
                new Vector3(1.6f, 0.35f, 0.25f)
            };

            var slots = new BuildSlot[4];
            for (int i = 0; i < 4; i++)
            {
                var ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ghost.name = $"Slot_{slotIds[i]}";
                ghost.transform.SetParent(reconRoot.transform, false);
                ghost.transform.position = slotPos[i];
                ghost.transform.localScale = slotScale[i];
                Object.DestroyImmediate(ghost.GetComponent<Collider>());
                var trigger = ghost.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                var mr = ghost.GetComponent<MeshRenderer>();
                mr.sharedMaterial = CreateUrpLit("Mat_SlotGhost", new Color(0.4f, 0.75f, 1f, 0.35f));
                // полупрозрачный призрак
                var c = mr.sharedMaterial.color;
                c.a = 0.35f;
                mr.sharedMaterial.color = c;

                var snap = new GameObject("Snap");
                snap.transform.SetParent(ghost.transform, false);

                var slot = ghost.AddComponent<BuildSlot>();
                SetString(slot, "requiredPieceId", slotIds[i]);
                Wire(slot, "snapPoint", snap.transform);
                slots[i] = slot;
            }

            var reconMgr = reconRoot.AddComponent<ReconstructionManager>();
            var rmSo = new SerializedObject(reconMgr);
            var slotsProp = rmSo.FindProperty("slots");
            slotsProp.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; i++)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            rmSo.ApplyModifiedPropertiesWithoutUndo();

            // Детали на столе
            var table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.name = "WorkTable";
            table.transform.position = new Vector3(-2.5f, 0.35f, 0.5f);
            table.transform.localScale = new Vector3(2.2f, 0.1f, 1.2f);
            table.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_WoodTable", new Color(0.4f, 0.28f, 0.15f));

            CreatePiece("wall_n", new Vector3(-3.0f, 0.7f, 0.3f), slotScale[0] * 0.45f, new Color(0.72f, 0.58f, 0.4f));
            CreatePiece("wall_e", new Vector3(-2.4f, 0.7f, 0.7f), slotScale[1] * 0.45f, new Color(0.7f, 0.55f, 0.38f));
            CreatePiece("column", new Vector3(-2.0f, 0.75f, 0.2f), new Vector3(0.25f, 0.7f, 0.25f), new Color(0.75f, 0.62f, 0.45f));
            CreatePiece("arch", new Vector3(-2.7f, 0.6f, 0.9f), new Vector3(0.7f, 0.2f, 0.15f), new Color(0.68f, 0.52f, 0.36f));

            // Переключатель эпох
            var switcherGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            switcherGo.name = "EraSwitcher";
            switcherGo.transform.position = new Vector3(2.5f, 0.5f, 3.5f);
            switcherGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            switcherGo.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_EraSwitch", new Color(0.85f, 0.7f, 0.2f));
            switcherGo.AddComponent<OlamInteractable>();
            var era = switcherGo.AddComponent<EraSwitcher>();
            Wire(era, "ruinsRoot", ruinsRoot);
            Wire(era, "reconstructionRoot", reconRoot);
            WireBool(era, "showReconstruction", true);
            // старт: реконструкция видна (слоты), руины тоже как фон — покажем руины рядом
            // EraSwitcher: ruins when !showReconstruction. Начнём с руин, клик → реконструкция
            WireBool(era, "showReconstruction", false);
            ruinsRoot.SetActive(true);
            reconRoot.SetActive(false);

            // Подсказка
            var tip = new GameObject("LearningTip");
            tip.transform.position = new Vector3(0f, 2.1f, 0f);
            tip.transform.localScale = Vector3.one * 0.01f;
            tip.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var tipText = MakeTmp(tip.transform, "Text", Vector2.zero, 38);
            tipText.rectTransform.sizeDelta = new Vector2(950, 130);
            tipText.text = "OLAM · Топрак-Кала\nЛКМ — взять деталь → отпустить у синего гнезда\nЖёлтый цилиндр — руины ↔ реконструкция";

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 2.2f, -3.5f);
                cam.transform.LookAt(new Vector3(0f, 0.8f, 3f));
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.72f, 0.78f, 0.88f);
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"OLAM: сцена сохранена → {ScenePath}");
        }

        static void CreateRuinBlock(Transform parent, string name, Vector3 pos, Vector3 scale, float shade)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_RuinClay", new Color(shade, shade * 0.85f, shade * 0.65f));
        }

        static void CreatePiece(string id, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Piece_{id}";
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = CreateUrpLit($"Mat_Piece_{id}", color);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1.2f;
            var grab = go.AddComponent<OlamInteractable>();
            SetBool(grab, "grabbable", true);
            var piece = go.AddComponent<BuildPiece>();
            SetString(piece, "pieceId", id);
            SetFloat(piece, "slotSearchRadius", 0.55f);
        }

        static EncyclopediaEntry CreateOrLoadEntry()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EncyclopediaEntry>(EntryPath);
            if (existing != null) return existing;

            var entry = ScriptableObject.CreateInstance<EncyclopediaEntry>();
            var so = new SerializedObject(entry);
            so.FindProperty("id").stringValue = "toprak_kala";
            so.FindProperty("scientificName").stringValue = "Toprak-Kala / Excavation site";
            so.FindProperty("title").FindPropertyRelative("ru").stringValue = "Топрак-Кала";
            so.FindProperty("title").FindPropertyRelative("uz").stringValue = "Topraqqal’a";
            so.FindProperty("title").FindPropertyRelative("en").stringValue = "Toprak-Kala";
            so.FindProperty("description").FindPropertyRelative("ru").stringValue =
                "Древняя крепость Хорезма (II–III вв.). Глинобитные стены, залы с росписями. Реконструкция помогает понять планировку дворца.";
            so.FindProperty("description").FindPropertyRelative("uz").stringValue =
                "Xorazmning qadimiy qal’asi (II–III asrlar). Loy devorlar, naqshli zallar. Rekonstruksiya saroy rejasini tushunishga yordam beradi.";
            so.FindProperty("description").FindPropertyRelative("en").stringValue =
                "An ancient Khorezm fortress (2nd–3rd c.). Mud-brick walls and painted halls. Reconstruction clarifies the palace layout.";
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(entry, EntryPath);
            return entry;
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/OLAM/Scenes"))
                AssetDatabase.CreateFolder("Assets/OLAM", "Scenes");
            if (!AssetDatabase.IsValidFolder("Assets/OLAM/Materials"))
                AssetDatabase.CreateFolder("Assets/OLAM", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/OLAM/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets/OLAM", "ScriptableObjects");
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

        static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Object target, string field, string value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
