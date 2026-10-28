#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Olam.Core;
using Olam.Kitchen;

namespace Olam.EditorTools
{
    /// <summary>
    /// Меню: OLAM → Build Tandoor Learning Space
    /// </summary>
    public static class OlamTandoorSceneBuilder
    {
        const string ScenePath = "Assets/OLAM/Scenes/Tandoor.unity";

        [MenuItem("OLAM/Build Tandoor Learning Space")]
        public static void Build()
        {
            EnsureFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            var systems = new GameObject("OLAM_Systems");
            systems.AddComponent<Localization>();
            var quests = systems.AddComponent<QuestManager>();
            var so = new SerializedObject(quests);
            var goals = so.FindProperty("goals");
            goals.arraySize = 1;
            var g0 = goals.GetArrayElementAtIndex(0);
            g0.FindPropertyRelative("id").stringValue = "bake_bread";
            g0.FindPropertyRelative("title").FindPropertyRelative("ru").stringValue = "Испеки лепёшку";
            g0.FindPropertyRelative("title").FindPropertyRelative("uz").stringValue = "Non yopish";
            g0.FindPropertyRelative("title").FindPropertyRelative("en").stringValue = "Bake flatbread";
            g0.FindPropertyRelative("required").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();

            var tandoor = new GameObject("Tandoor");
            tandoor.transform.position = new Vector3(0f, 0f, 1.5f);

            var tubeGo = new GameObject("Wall");
            tubeGo.transform.SetParent(tandoor.transform, false);
            var tube = tubeGo.AddComponent<ProceduralTube>();
            tube.Rebuild();
            tubeGo.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_TandoorClay", new Color(0.55f, 0.35f, 0.22f));

            // Модель Rodin, если префабы собраны (OLAM → Rodin → Build Prefabs): видимая стенка.
            // Труба остаётся ради коллайдера, её отрисовка выключена. Модель — 0,9 м,
            // труба — 1,1 м, поэтому экземпляр подогнан под трубу; свой коллайдер у него снят.
            var shell = PlaceRodin("tandoor_a_body_left", tandoor.transform, Vector3.zero, 0f, 1.1f / 0.9f);
            if (shell != null)
            {
                foreach (var col in shell.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(col);
                tubeGo.GetComponent<MeshRenderer>().enabled = false;
            }

            var thermal = tandoor.AddComponent<TandoorThermal>();

            var fuelZone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fuelZone.name = "FuelZone";
            fuelZone.transform.SetParent(tandoor.transform, false);
            fuelZone.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            fuelZone.transform.localScale = new Vector3(0.7f, 0.15f, 0.7f);
            Object.DestroyImmediate(fuelZone.GetComponent<Collider>());
            var fuelCol = fuelZone.AddComponent<SphereCollider>();
            fuelCol.isTrigger = true;
            fuelCol.radius = 0.55f;
            var fuel = fuelZone.AddComponent<FuelZone>();

            var wallZone = new GameObject("WallZone");
            wallZone.transform.SetParent(tandoor.transform, false);
            wallZone.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            var wallCol = wallZone.AddComponent<SphereCollider>();
            wallCol.isTrigger = true;
            wallCol.radius = 0.45f;
            var wall = wallZone.AddComponent<TandoorWallZone>();
            var anchor = new GameObject("DoughAnchor");
            anchor.transform.SetParent(wallZone.transform, false);
            anchor.transform.localPosition = new Vector3(0.4f, 0f, 0f);

            var embers = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            embers.name = "Embers";
            embers.transform.SetParent(tandoor.transform, false);
            embers.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            embers.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
            Object.DestroyImmediate(embers.GetComponent<Collider>());
            embers.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_Embers", new Color(0.2f, 0.05f, 0.02f));

            var lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(tandoor.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            var fireLight = lightGo.AddComponent<Light>();
            fireLight.type = LightType.Point;
            fireLight.shadows = LightShadows.None;
            fireLight.range = 4f;
            fireLight.intensity = 0f;

            var fuelVisual = tandoor.AddComponent<TandoorFuelVisual>();

            Wire(fuel, "thermal", thermal);
            Wire(wall, "thermal", thermal);
            Wire(wall, "attachAnchor", anchor.transform);
            Wire(fuelVisual, "embers", embers.GetComponent<MeshRenderer>());
            Wire(fuelVisual, "fireLight", fireLight);
            Wire(thermal, "fuelVisual", fuelVisual);

            var canvasGo = new GameObject("TandoorGaugeCanvas");
            canvasGo.transform.SetParent(tandoor.transform, false);
            canvasGo.transform.localPosition = new Vector3(0.8f, 1.2f, 0f);
            canvasGo.transform.localScale = Vector3.one * 0.005f;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(canvasGo.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.rectTransform.sizeDelta = new Vector2(400, 200);
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            var gauge = canvasGo.AddComponent<TandoorGauge>();
            Wire(gauge, "label", tmp);
            Wire(thermal, "gauge", gauge);

            for (int i = 0; i < 3; i++)
            {
                var log = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                log.name = $"Firewood_{i}";
                log.transform.position = new Vector3(-1.2f + i * 0.35f, 0.2f, 0.3f);
                log.transform.localScale = new Vector3(0.15f, 0.25f, 0.15f);
                log.GetComponent<MeshRenderer>().sharedMaterial =
                    CreateUrpLit("Mat_Wood", new Color(0.35f, 0.22f, 0.1f));
                var rb = log.AddComponent<Rigidbody>();
                rb.mass = 0.8f;
                var grab = log.AddComponent<OlamInteractable>();
                SetBool(grab, "grabbable", true);
                SetBool(grab, "throwOnDetach", true);
                log.AddComponent<Firewood>();
            }

            var doughGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            doughGo.name = "Dough";
            doughGo.transform.position = new Vector3(1.0f, 0.08f, 0.4f);
            doughGo.transform.localScale = new Vector3(0.35f, 0.03f, 0.35f);
            doughGo.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_Dough", new Color(0.92f, 0.85f, 0.7f));
            doughGo.AddComponent<Rigidbody>().mass = 0.4f;
            var doughGrab = doughGo.AddComponent<OlamInteractable>();
            SetBool(doughGrab, "grabbable", true);
            doughGo.AddComponent<Dough>();

            var bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "WaterBowl";
            bowl.transform.position = new Vector3(1.0f, 0.1f, -0.2f);
            bowl.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
            Object.DestroyImmediate(bowl.GetComponent<Collider>());
            var bowlCol = bowl.AddComponent<SphereCollider>();
            bowlCol.isTrigger = true;
            bowlCol.radius = 0.6f;
            bowl.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_Water", new Color(0.3f, 0.55f, 0.85f));
            bowl.AddComponent<WaterBowl>();

            // Стол с поленом — у дров, слева. Лепёшки — на столике справа: хлеб на землю не кладут.
            PlaceRodin("work_table_table_with_log", null, new Vector3(-1.9f, 0f, 0.9f), 90f, 1f);
            var nonStack = AssetDatabase.LoadAssetAtPath<GameObject>(RodinPrefabs + "non_stack_non_stack.prefab");
            if (nonStack != null)
            {
                var nonTable = GameObject.CreatePrimitive(PrimitiveType.Cube);
                nonTable.name = "NonTable";
                nonTable.transform.position = new Vector3(1.75f, 0.35f, 0.9f);
                nonTable.transform.localScale = new Vector3(0.7f, 0.7f, 0.5f);
                // Mat_WoodTable общий с Топрак-калой: тот же цвет, что задаёт её сборщик
                nonTable.GetComponent<MeshRenderer>().sharedMaterial =
                    CreateUrpLit("Mat_WoodTable", new Color(0.4f, 0.28f, 0.15f));
                PlaceRodin("non_stack_non_stack", null, new Vector3(1.75f, 0.7f, 0.9f), 0f, 1f);
            }

            var tip = new GameObject("LearningTip");
            tip.transform.position = new Vector3(0f, 1.8f, 0f);
            tip.transform.localScale = Vector3.one * 0.01f;
            var tipCanvas = tip.AddComponent<Canvas>();
            tipCanvas.renderMode = RenderMode.WorldSpace;
            var tipTextGo = new GameObject("Text");
            tipTextGo.transform.SetParent(tip.transform, false);
            var tipText = tipTextGo.AddComponent<TextMeshProUGUI>();
            tipText.rectTransform.sizeDelta = new Vector2(800, 120);
            tipText.fontSize = 48;
            tipText.alignment = TextAlignmentOptions.Center;
            tipText.text = "OLAM · Тандыр\n1) Смочи лепёшку  2) Кинь дрова в топку  3) Прилепи к стенке";

            // Камера ближе к столу
            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1.6f, -2.2f);
                cam.transform.LookAt(new Vector3(0f, 0.6f, 1.2f));
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"OLAM: сцена сохранена → {ScenePath}. Play: ЛКМ — взять дрова/лепёшку.");
        }

        const string RodinPrefabs = "Assets/OLAM/Models/Rodin/Prefabs/";

        /// <summary>Ставит префаб модели Rodin, если он уже собран; иначе null.</summary>
        static GameObject PlaceRodin(string name, Transform parent, Vector3 localPos, float yaw, float scale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RodinPrefabs + name + ".prefab");
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            return go;
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

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("URP/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var mat = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void Wire(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetBool(Object target, string field, bool value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
