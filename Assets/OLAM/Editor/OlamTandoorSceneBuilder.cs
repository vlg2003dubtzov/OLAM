#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Olam.Core;
using Olam.Kitchen;
using Olam.UI;

namespace Olam.EditorTools
{
    /// <summary>
    /// Меню: OLAM → Build Tandoor Learning Space — сцена «Тандыр» с миссией «Первая лепёшка».
    /// Слева дрова у стола с поленом, в центре тандыр, справа стол с тестом, миской, чекичем и подушкой,
    /// дальше дастархан со стопкой лепёшек (хлеб на землю не кладут). Шаги — цели QuestManager,
    /// панель миссии и «Во двор» — OlamMissionPanel. Снимки: OLAM → Kitchen → Capture Tandoor.
    /// </summary>
    public static class OlamTandoorSceneBuilder
    {
        const string ScenePath = "Assets/OLAM/Scenes/Tandoor.unity";
        const string RodinPrefabs = "Assets/OLAM/Models/Rodin/Prefabs/";
        const int IgnoreRaycast = 2;   // цели кухни — триггеры, клики по предметам их не должны задевать

        [MenuItem("OLAM/Build Tandoor Learning Space")]
        public static void Build()
        {
            EnsureFolders();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2f, 1f, 2f);

            // ---------- язык, шаги миссии, панель ----------
            var systems = new GameObject("OLAM_Systems");
            systems.AddComponent<Localization>();
            // шаги — на своём объекте: Localization, придя из хаба, удаляет свой дубликат вместе с объектом
            var quests = new SerializedObject(new GameObject("OLAM_Quests").AddComponent<QuestManager>());
            var goals = quests.FindProperty("goals");
            goals.arraySize = KitchenText.Steps.Length;
            for (int i = 0; i < KitchenText.Steps.Length; i++)
            {
                var (id, required, title, _) = KitchenText.Steps[i];
                var goal = goals.GetArrayElementAtIndex(i);
                goal.FindPropertyRelative("id").stringValue = id;
                SetLText(goal.FindPropertyRelative("title"), title);
                goal.FindPropertyRelative("required").intValue = required;
            }
            quests.ApplyModifiedPropertiesWithoutUndo();

            var mission = new SerializedObject(new GameObject("OLAM_Mission").AddComponent<OlamMissionPanel>());
            mission.FindProperty("spaceScene").stringValue = "Tandoor";
            SetLText(mission.FindProperty("title"), KitchenText.MissionTitle);
            SetLText(mission.FindProperty("question"), KitchenText.MissionQuestion);
            SetLText(mission.FindProperty("doneTitle"), KitchenText.DoneTitle);
            SetLText(mission.FindProperty("doneBody"), KitchenText.DoneBody);
            var hints = mission.FindProperty("hints");
            hints.arraySize = KitchenText.Steps.Length;
            for (int i = 0; i < KitchenText.Steps.Length; i++)
            {
                var hint = hints.GetArrayElementAtIndex(i);
                hint.FindPropertyRelative("goalId").stringValue = KitchenText.Steps[i].id;
                SetLText(hint.FindPropertyRelative("text"), KitchenText.Steps[i].hint);
            }
            mission.ApplyModifiedPropertiesWithoutUndo();

            // ---------- тандыр ----------
            var tandoor = new GameObject("Tandoor");
            tandoor.transform.position = new Vector3(0f, 0f, 1.5f);

            var tubeGo = new GameObject("Wall");
            tubeGo.transform.SetParent(tandoor.transform, false);
            var tube = tubeGo.AddComponent<ProceduralTube>();
            // клик по тандыру — заглянуть внутрь (KitchenCameraRig)
            var clicks = new System.Collections.Generic.List<OlamInteractable> { tubeGo.AddComponent<OlamInteractable>() };

            // Снаружи — модель Rodin (0,9 м, подогнана под высоту трубы 1,1 м). Она сплошная: у оси дно выемки
            // лишь на ~0,1 м ниже края. Дно вырезается (OpenTandoor), а внутренняя стенка — труба под вырезом:
            // её внешняя сторона спрятана в толще модели, внутренняя видна из устья, на ней печётся лепёшка.
            const float shellScale = 1.1f / 0.9f;
            float innerRadius = 0.47f, tubeHeight = 1.1f;
            var shell = PlaceRodin("tandoor_a_body_left", tandoor.transform, Vector3.zero, 0f, shellScale);
            var shellFilter = shell != null ? shell.GetComponentInChildren<MeshFilter>() : null;
            if (shellFilter != null && shellFilter.sharedMesh != null)
            {
                foreach (var col in shell.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(col);
                var (open, floorY, wallR) = OpenTandoor(shellFilter.sharedMesh);
                shellFilter.sharedMesh = open;
                shellFilter.gameObject.AddComponent<MeshCollider>().sharedMesh = open;
                clicks.Add(shellFilter.gameObject.AddComponent<OlamInteractable>());
                var tubeSo = new SerializedObject(tube);
                tubeSo.FindProperty("radius").floatValue = wallR * shellScale + 0.005f;
                tubeSo.FindProperty("thickness").floatValue = 0.04f;
                tubeSo.FindProperty("height").floatValue = tubeHeight = floorY * shellScale + 0.01f;
                tubeSo.FindProperty("segments").intValue = 32;
                tubeSo.ApplyModifiedPropertiesWithoutUndo();
                innerRadius = wallR * shellScale - 0.035f;
            }
            tube.Rebuild();
            tubeGo.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_TandoorInner", new Color(0.3f, 0.18f, 0.11f));

            var thermal = tandoor.AddComponent<TandoorThermal>();

            // топка — вся полость: полено засчитывается, куда бы в устье ни попало
            var fuelGo = new GameObject("FuelZone") { layer = IgnoreRaycast };
            fuelGo.transform.SetParent(tandoor.transform, false);
            var fuelCol = fuelGo.AddComponent<BoxCollider>();
            fuelCol.isTrigger = true;
            fuelCol.center = new Vector3(0f, 0.5f, 0f);
            fuelCol.size = new Vector3(0.8f, 1f, 0.8f);
            var fuel = fuelGo.AddComponent<FuelZone>();

            var wallZone = new GameObject("WallZone") { layer = IgnoreRaycast };
            wallZone.transform.SetParent(tandoor.transform, false);
            wallZone.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            var wallCol = wallZone.AddComponent<SphereCollider>();
            wallCol.isTrigger = true;
            wallCol.radius = 0.5f;
            var wall = wallZone.AddComponent<TandoorWallZone>();
            // лепёшку прилепляют к дальней от зрителя стенке: сверху, из устья, её видно.
            // Край лепёшки (0,15 м от середины) не должен уходить в изогнутую стенку
            var anchor = new GameObject("DoughAnchor");
            anchor.transform.SetParent(wallZone.transform, false);
            float anchorR = Mathf.Sqrt(Mathf.Max(0.01f, innerRadius * innerRadius - 0.15f * 0.15f)) - 0.005f;
            anchor.transform.localPosition = new Vector3(0f, Mathf.Min(0.08f, tubeHeight - 0.85f), anchorR);
            anchor.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);

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

            // карточки урока по температуре: без своей таблички они уходят на панель миссии
            var milestones = new SerializedObject(tandoor.AddComponent<TandoorMilestones>());
            milestones.FindProperty("thermal").objectReferenceValue = thermal;
            var cards = milestones.FindProperty("cards");
            cards.arraySize = 2;
            SetCard(cards.GetArrayElementAtIndex(0), "clay", 100f, KitchenText.ClayTitle, KitchenText.ClayBody);
            SetCard(cards.GetArrayElementAtIndex(1), "ready", 250f, KitchenText.ReadyTitle, KitchenText.ReadyBody);
            milestones.ApplyModifiedPropertiesWithoutUndo();

            var canvasGo = new GameObject("TandoorGaugeCanvas");
            canvasGo.transform.SetParent(tandoor.transform, false);
            canvasGo.transform.localPosition = new Vector3(0.78f, 1.35f, 0f);
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
            tmp.text = "Тандыр\n25 °C\nхолодный";
            var gauge = canvasGo.AddComponent<TandoorGauge>();
            Wire(gauge, "label", tmp);
            Wire(thermal, "gauge", gauge);

            // ---------- дрова: стол с поленом и поленница перед ним ----------
            // ближе к камере: дальше их закрывала бы панель миссии слева сверху
            PlaceRodin("work_table_table_with_log", null, new Vector3(-1.55f, 0f, 0.75f), 90f, 1f);
            var woodMat = CreateUrpLit("Mat_Wood", new Color(0.35f, 0.22f, 0.1f));
            var pile = new GameObject("Woodpile");
            var template = MakeLog(pile.transform, "FirewoodTemplate", Vector3.zero, woodMat);
            template.SetActive(false);
            Vector3[] logPositions = { new(-1.42f, 0.05f, 0.2f), new(-1.3f, 0.05f, 0.2f), new(-1.36f, 0.14f, 0.2f) };
            var supply = new SerializedObject(pile.AddComponent<FirewoodSupply>());
            supply.FindProperty("template").objectReferenceValue = template;
            var logs = supply.FindProperty("logs");
            logs.arraySize = logPositions.Length;
            for (int i = 0; i < logPositions.Length; i++)
                logs.GetArrayElementAtIndex(i).objectReferenceValue = MakeLog(pile.transform, $"Firewood_{i}", logPositions[i], woodMat);
            supply.ApplyModifiedPropertiesWithoutUndo();

            // ---------- стол пекаря: тесто, миска воды, чекич, подушка ----------
            // Mat_WoodTable общий с Топрак-калой: тот же цвет, что задаёт её сборщик
            var tableMat = CreateUrpLit("Mat_WoodTable", new Color(0.4f, 0.28f, 0.15f));
            Box("PrepTable", null, new Vector3(1.05f, 0.35f, 0.55f), new Vector3(0.9f, 0.7f, 0.8f), tableMat, true);

            var doughGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            doughGo.name = "Dough";
            doughGo.transform.position = new Vector3(0.88f, 0.711f, 0.72f);
            doughGo.transform.localScale = new Vector3(0.3f, 0.01f, 0.3f);
            // у цилиндра-примитива капсула: при такой толщине это шар, тесто висело бы над столом
            Object.DestroyImmediate(doughGo.GetComponent<Collider>());
            doughGo.AddComponent<BoxCollider>().size = new Vector3(1f, 2f, 1f);
            doughGo.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_Dough", new Color(0.92f, 0.85f, 0.7f));
            doughGo.AddComponent<Rigidbody>().mass = 0.4f;
            SetBool(doughGo.AddComponent<OlamInteractable>(), "grabbable", true);
            SetKind(doughGo.AddComponent<KitchenItem>(), KitchenItem.Kind.Dough, true);
            var mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mark.name = "ChekichMark";
            Object.DestroyImmediate(mark.GetComponent<Collider>());
            mark.transform.SetParent(doughGo.transform, false);
            mark.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            mark.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            mark.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
            mark.GetComponent<MeshRenderer>().sharedMaterial = ChekichMarkMat();
            mark.SetActive(false);
            var dough = doughGo.AddComponent<Dough>();
            Wire(dough, "meshRenderer", doughGo.GetComponent<MeshRenderer>());
            Wire(dough, "stampMark", mark);

            var bowl = new GameObject("WaterBowl") { layer = IgnoreRaycast };
            bowl.transform.position = new Vector3(1.3f, 0.7f, 0.75f);
            Part(PrimitiveType.Cylinder, "Dish", bowl.transform, new Vector3(0f, 0.035f, 0f), new Vector3(0.26f, 0.035f, 0.26f),
                CreateUrpLit("Mat_ClayDish", new Color(0.62f, 0.36f, 0.22f)));
            Part(PrimitiveType.Cylinder, "Water", bowl.transform, new Vector3(0f, 0.04f, 0f), new Vector3(0.22f, 0.036f, 0.22f),
                CreateUrpLit("Mat_Water", new Color(0.3f, 0.55f, 0.85f)));
            var bowlCol = bowl.AddComponent<BoxCollider>();
            bowlCol.isTrigger = true;
            bowlCol.center = new Vector3(0f, 0.1f, 0f);
            bowlCol.size = new Vector3(0.32f, 0.2f, 0.32f);
            bowl.AddComponent<WaterBowl>();

            // чекич — штамп с ручкой: им накалывают середину лепёшки (модель — потом из Rodin)
            var chekich = new GameObject("Chekich");
            chekich.transform.position = new Vector3(1.32f, 0.7f, 0.35f);
            var metal = CreateUrpLit("Mat_ChekichHead", new Color(0.62f, 0.6f, 0.56f));
            Part(PrimitiveType.Cylinder, "Head", chekich.transform, new Vector3(0f, 0.012f, 0f), new Vector3(0.085f, 0.012f, 0.085f), metal);
            Part(PrimitiveType.Cylinder, "Handle", chekich.transform, new Vector3(0f, 0.084f, 0f), new Vector3(0.03f, 0.06f, 0.03f), woodMat);
            Part(PrimitiveType.Sphere, "Knob", chekich.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.045f, 0.045f, 0.045f), woodMat);
            var chekichCol = chekich.AddComponent<BoxCollider>();
            chekichCol.center = new Vector3(0f, 0.085f, 0f);
            chekichCol.size = new Vector3(0.09f, 0.17f, 0.09f);
            chekich.AddComponent<Rigidbody>().mass = 0.2f;
            SetBool(chekich.AddComponent<OlamInteractable>(), "grabbable", true);
            SetKind(chekich.AddComponent<KitchenItem>(), KitchenItem.Kind.Chekich, true);

            // подушка пекаря — на неё кладут тесто и ею прижимают его к стенке (модель — потом из Rodin)
            var cushionGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cushionGo.name = "BakerCushion";
            cushionGo.transform.position = new Vector3(0.82f, 0.745f, 0.33f);
            cushionGo.transform.localScale = new Vector3(0.32f, 0.09f, 0.32f);
            Object.DestroyImmediate(cushionGo.GetComponent<Collider>());
            cushionGo.AddComponent<BoxCollider>();
            cushionGo.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_Cushion", new Color(0.55f, 0.16f, 0.12f));
            cushionGo.AddComponent<Rigidbody>().mass = 0.3f;
            SetBool(cushionGo.AddComponent<OlamInteractable>(), "grabbable", true);
            SetKind(cushionGo.AddComponent<KitchenItem>(), KitchenItem.Kind.Cushion, true);
            var seat = new GameObject("Seat");
            seat.transform.SetParent(cushionGo.transform, false);
            seat.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            Wire(cushionGo.AddComponent<BakerCushion>(), "seat", seat.transform);

            // ---------- дастархан: стол со скатертью и стопкой лепёшек ----------
            Box("NonTable", null, new Vector3(2.05f, 0.35f, 1.35f), new Vector3(0.8f, 0.7f, 0.6f), tableMat, true);
            Box("Dastarkhan", null, new Vector3(2.05f, 0.705f, 1.35f), new Vector3(0.78f, 0.01f, 0.58f),
                CreateUrpLit("Mat_Dastarkhan", new Color(0.62f, 0.16f, 0.13f)), false);
            PlaceRodin("non_stack_non_stack", null, new Vector3(2.05f, 0.71f, 1.35f), 0f, 1f);
            var serve = new GameObject("ServeZone") { layer = IgnoreRaycast };
            serve.transform.position = new Vector3(2.05f, 0.95f, 1.35f);
            var serveCol = serve.AddComponent<BoxCollider>();
            serveCol.isTrigger = true;
            serveCol.size = new Vector3(0.8f, 0.5f, 0.6f);
            var serveSeat = new GameObject("Seat");
            serveSeat.transform.SetParent(serve.transform, false);
            serveSeat.transform.position = new Vector3(2.05f, 0.912f, 1.35f);
            Wire(serve.AddComponent<ServeZone>(), "seat", serveSeat.transform);

            // ---------- камера: общий план, по клику на тандыр — сверху в устье ----------
            var cam = Camera.main;
            if (cam != null)
            {
                var rig = cam.gameObject.AddComponent<KitchenCameraRig>();
                var rigSo = new SerializedObject(rig);
                var clickList = rigSo.FindProperty("tandoorClicks");
                clickList.arraySize = clicks.Count;
                for (int i = 0; i < clicks.Count; i++)
                    clickList.GetArrayElementAtIndex(i).objectReferenceValue = clicks[i];
                var hidden = rigSo.FindProperty("hideInPeek");
                hidden.arraySize = 1;
                hidden.GetArrayElementAtIndex(0).objectReferenceValue = canvasGo;
                // заглядывая в устье, камера смотрит туда, где на стенке печётся лепёшка
                rigSo.FindProperty("peekTarget").vector3Value = anchor.transform.position;
                rigSo.ApplyModifiedPropertiesWithoutUndo();
                var from = rigSo.FindProperty("overviewPosition").vector3Value;
                var at = rigSo.FindProperty("overviewTarget").vector3Value;
                cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(at - from));
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"OLAM: сцена сохранена → {ScenePath}. Миссия «Первая лепёшка»: ЛКМ — взять и тащить, отпустить над целью.");
        }

        /// <summary>
        /// Снимки сцены «Тандыр» с панелью миссии: общий план и вид в устье с румяной лепёшкой на стенке.
        /// Сцена не сохраняется. Файлы — Logs/OLAM_Captures/tandoor_*.png.
        /// </summary>
        [MenuItem("OLAM/Kitchen/Capture Tandoor")]
        public static void CaptureScene()
        {
            Directory.CreateDirectory(OlamSceneDressing.CaptureDir);
            OlamSceneDressing.InScene("Tandoor", false, scene =>
            {
                Camera cam = null;
                OlamMissionPanel panel = null;
                QuestManager quests = null;
                foreach (var g in scene.GetRootGameObjects())
                {
                    if (cam == null) cam = g.GetComponentInChildren<Camera>(true);
                    if (panel == null) panel = g.GetComponentInChildren<OlamMissionPanel>(true);
                    if (quests == null) quests = g.GetComponentInChildren<QuestManager>(true);
                }
                if (cam == null || panel == null || quests == null)
                {
                    Debug.LogWarning("OLAM Kitchen: в сцене нет камеры, панели или квестов — сначала OLAM → Build Tandoor Learning Space");
                    return;
                }
                var rig = new SerializedObject(cam.GetComponent<KitchenCameraRig>());
                void Pose(string position, string target)
                {
                    var from = rig.FindProperty(position).vector3Value;
                    cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(rig.FindProperty(target).vector3Value - from));
                }

                // первые кадры без окна Unity рисует, пока не готово освещение от неба: цвета уезжают — прогреваем
                DynamicGI.UpdateEnvironment();
                var warm = RenderTexture.GetTemporary(256, 144, 24);
                cam.targetTexture = warm;
                for (int i = 0; i < 3; i++) cam.Render();
                cam.targetTexture = null;
                RenderTexture.ReleaseTemporary(warm);

                Pose("overviewPosition", "overviewTarget");
                OlamMenuUiBuilder.Shot(cam, "tandoor_overview",
                    panel.BuildPreview(quests.Goals, 2, KitchenText.ClayTitle, KitchenText.ClayBody));

                // румяная лепёшка с узором чекича — на стенке, как в игре
                var dough = Find(scene, "Dough");
                var anchor = Find(scene, "DoughAnchor");
                if (dough != null && anchor != null)
                {
                    dough.transform.SetPositionAndRotation(anchor.transform.position, anchor.transform.rotation);
                    var renderer = dough.GetComponent<MeshRenderer>();
                    renderer.sharedMaterial = new Material(renderer.sharedMaterial) { color = new Color(0.76f, 0.5f, 0.24f) };
                    dough.transform.Find("ChekichMark")?.gameObject.SetActive(true);
                }
                // в устье камера заглядывает без градусника — как в игре (KitchenCameraRig.hideInPeek)
                var gaugeCanvas = Find(scene, "TandoorGaugeCanvas");
                if (gaugeCanvas != null) gaugeCanvas.SetActive(false);
                Pose("peekPosition", "peekTarget");
                OlamSceneDressing.Shoot(cam, $"{OlamSceneDressing.CaptureDir}/tandoor_peek_bare.png");
                OlamMenuUiBuilder.Shot(cam, "tandoor_peek",
                    panel.BuildPreview(quests.Goals, 5, KitchenText.GoldenTitle, KitchenText.GoldenBody));
            });
            Debug.Log($"OLAM Kitchen: снимки тандыра → {OlamSceneDressing.CaptureDir}/tandoor_*.png");
        }

        /// <summary>Снимок деталей тандыра, стола и лепёшек в ряд — спереди и сверху, чтобы выбрать, из чего собрать печь.</summary>
        [MenuItem("OLAM/Kitchen/Capture Model Parts")]
        public static void CaptureParts()
        {
            string[] names =
            {
                "tandoor_a_body_left", "tandoor_a_body_right", "tandoor_a_top", "tandoor_a_cone_black",
                "tandoor_b_body_left", "tandoor_b_body_right", "tandoor_b_top", "tandoor_b_cone_black", "tandoor_b_chunk",
                "work_table_table_with_log", "non_stack_non_stack"
            };
            // сцена-превью: открытые сцены (и безымянная сцена запуска без окна) не трогаются
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var sun = new GameObject("Sun").AddComponent<Light>();
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sun.gameObject, scene);
                sun.type = LightType.Directional;
                sun.intensity = 1.3f;
                sun.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
                var fill = new GameObject("Fill").AddComponent<Light>();
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fill.gameObject, scene);
                fill.type = LightType.Directional;
                fill.intensity = 0.6f;
                fill.transform.rotation = Quaternion.Euler(20f, 150f, 0f);
                float x = 0f;
                var report = new System.Text.StringBuilder();
                foreach (var name in names)
                {
                    var go = PlaceRodin(name, null, Vector3.zero, 0f, 1f);
                    if (go == null) continue;
                    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                    var b = new Bounds(go.transform.position, Vector3.zero);
                    foreach (var r in go.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
                    go.transform.position = new Vector3(x + b.extents.x - (b.center.x - go.transform.position.x), 0f, 0f);
                    report.Append($"{name}: x={x + b.extents.x:F2} size={b.size.x:F2}×{b.size.y:F2}×{b.size.z:F2}; ");
                    x += b.size.x + 0.35f;
                }
                Debug.Log("OLAM Kitchen parts: " + report);
                var cam = new GameObject("Cam").AddComponent<Camera>();
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cam.gameObject, scene);
                cam.scene = scene;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.32f, 0.36f, 0.42f);
                cam.fieldOfView = 30f;
                float mid = x * 0.5f;
                cam.transform.position = new Vector3(mid, 1.6f, -mid / Mathf.Tan(15f * Mathf.Deg2Rad) * 0.62f);
                cam.transform.LookAt(new Vector3(mid, 0.45f, 0f));
                Directory.CreateDirectory(OlamSceneDressing.CaptureDir);
                OlamSceneDressing.Shoot(cam, $"{OlamSceneDressing.CaptureDir}/kitchen_parts_front.png");
                cam.transform.position = new Vector3(mid, mid * 2.2f, -0.6f);
                cam.transform.LookAt(new Vector3(mid, 0f, 0f));
                OlamSceneDressing.Shoot(cam, $"{OlamSceneDressing.CaptureDir}/kitchen_parts_top.png");

                // корпуса тандыров с четырёх сторон: где разрез с внутренней стенкой
                foreach (var root in scene.GetRootGameObjects())
                    if (root.GetComponentInChildren<Renderer>() != null && root.GetComponent<Light>() == null) Object.DestroyImmediate(root);
                string[] bodies = { "tandoor_a_body_left", "tandoor_b_body_left", "tandoor_b_body_right" };
                for (int b = 0; b < bodies.Length; b++)
                    for (int k = 0; k < 4; k++)
                    {
                        var go = PlaceRodin(bodies[b], null, new Vector3(k * 1.15f, 0f, b * -1.3f), k * 90f, 1f);
                        if (go != null) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);
                    }
                cam.fieldOfView = 34f;
                cam.transform.position = new Vector3(1.72f, 2.4f, -6.6f);
                cam.transform.LookAt(new Vector3(1.72f, 0.2f, -1.3f));
                OlamSceneDressing.Shoot(cam, $"{OlamSceneDressing.CaptureDir}/kitchen_bodies_turns.png");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        // ---------- детали сцены ----------

        const string OpenTandoorPath = "Assets/OLAM/Environment/Meshes/Tandoor_a_body_open.asset";

        /// <summary>
        /// Сетка корпуса тандыра без дна выемки — в устье видна труба-стенка под ним. Дно — треугольники у оси
        /// внутри стенки выемки. Возвращает сетку (ассет в Environment/Meshes), высоту дна и радиус стенки
        /// выемки — в единицах модели.
        /// </summary>
        static (Mesh mesh, float floorY, float wallR) OpenTandoor(Mesh source)
        {
            var v = source.vertices;
            var b = source.bounds;
            var axis = new Vector2(b.center.x, b.center.z);
            float R(Vector3 p) => Vector2.Distance(new Vector2(p.x, p.z), axis);

            float floorY = b.center.y;
            foreach (var p in v)
                if (R(p) < 0.05f && p.y > b.center.y) floorY = Mathf.Max(floorY, p.y);
            var wall = new System.Collections.Generic.List<float>();
            foreach (var p in v)
                if (p.y > floorY + 0.02f && p.y < b.max.y - 0.03f && R(p) < b.extents.x - 0.05f) wall.Add(R(p));
            wall.Sort();
            float wallR = wall.Count > 0 ? wall[wall.Count / 2] : b.extents.x * 0.75f;

            var open = Object.Instantiate(source);
            open.name = "Tandoor_a_body_open";
            for (int s = 0; s < open.subMeshCount; s++)
            {
                var tris = open.GetTriangles(s);
                var keep = new System.Collections.Generic.List<int>(tris.Length);
                for (int i = 0; i < tris.Length; i += 3)
                {
                    bool floor = true;
                    for (int k = 0; k < 3 && floor; k++)
                    {
                        var p = v[tris[i + k]];
                        // только само дно: кромка устья выше и местами нависает внутрь — её не трогаем
                        floor = R(p) < wallR - 0.01f && p.y > floorY - 0.06f && p.y < floorY + 0.03f;
                    }
                    if (!floor) keep.AddRange(new[] { tris[i], tris[i + 1], tris[i + 2] });
                }
                open.SetTriangles(keep, s);
            }
            open.RecalculateBounds();

            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(OpenTandoorPath);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(open, OpenTandoorPath);
                existing = open;
            }
            else
            {
                EditorUtility.CopySerialized(open, existing);
                Object.DestroyImmediate(open);
            }
            Debug.Log($"OLAM Kitchen: дно выемки вырезано — дно {floorY:F2}, стенка выемки {wallR:F2} (ед. модели)");
            return (existing, floorY, wallR);
        }

        /// <summary>Полено лёжа (цилиндр: капсулы походили на сосиски): берут рукой, бросают или отпускают над тандыром.</summary>
        static GameObject MakeLog(Transform parent, string name, Vector3 position, Material material)
        {
            var log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = name;
            log.transform.SetParent(parent, false);
            log.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, 0f, 0f));
            log.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);
            log.GetComponent<MeshRenderer>().sharedMaterial = material;
            log.AddComponent<Rigidbody>().mass = 0.8f;
            var grab = log.AddComponent<OlamInteractable>();
            SetBool(grab, "grabbable", true);
            SetBool(grab, "throwOnDetach", true);
            log.AddComponent<Firewood>();
            SetKind(log.AddComponent<KitchenItem>(), KitchenItem.Kind.Firewood, false);
            return log;
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collider)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            if (parent != null) box.transform.SetParent(parent, false);
            box.transform.position = position;
            box.transform.localScale = size;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        /// <summary>Видимая часть предмета без своего коллайдера: ловит клики и толкается сам предмет.</summary>
        static void Part(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = name;
            Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            part.layer = parent.gameObject.layer;
        }

        /// <summary>Узор чекича: кольца точек вокруг центральной; прозрачное вокруг отрезается (alpha clip).</summary>
        static Material ChekichMarkMat()
        {
            const string texPath = "Assets/OLAM/Materials/Tex_ChekichMark.png";
            const string matPath = "Assets/OLAM/Materials/Mat_ChekichMark.mat";
            if (!File.Exists(texPath))
            {
                const int n = 256;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var pixels = new Color32[n * n];
                var dots = new System.Collections.Generic.List<Vector2> { Vector2.zero };
                foreach (var (radius, count) in new[] { (0.13f, 8), (0.25f, 14), (0.37f, 20) })
                    for (int k = 0; k < count; k++)
                    {
                        float a = k * Mathf.PI * 2f / count + radius * 3f;
                        dots.Add(new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
                    }
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        var p = new Vector2((x + 0.5f) / n - 0.5f, (y + 0.5f) / n - 0.5f);
                        bool on = Mathf.Abs(p.magnitude - 0.46f) < 0.012f;   // тонкое кольцо по краю середины
                        foreach (var d in dots)
                            if ((p - d).sqrMagnitude < 0.022f * 0.022f) { on = true; break; }
                        pixels[y * n + x] = on ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                    }
                tex.SetPixels32(pixels);
                tex.Apply();
                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
                if (AssetImporter.GetAtPath(texPath) is TextureImporter ti)
                {
                    ti.alphaIsTransparency = true;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.SaveAndReimport();
                }
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Mat_ChekichMark" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            mat.SetColor("_BaseColor", new Color(0.45f, 0.29f, 0.14f));
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------- служебное ----------

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

        static GameObject Find(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var g in scene.GetRootGameObjects())
                foreach (var t in g.GetComponentsInChildren<Transform>(true))
                    if (t.name == name) return t.gameObject;
            return null;
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

        static void SetKind(KitchenItem item, KitchenItem.Kind kind, bool returnHome)
        {
            var so = new SerializedObject(item);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("returnHome").boolValue = returnHome;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetLText(SerializedProperty property, LText text)
        {
            property.FindPropertyRelative("ru").stringValue = text.ru;
            property.FindPropertyRelative("uz").stringValue = text.uz;
            property.FindPropertyRelative("en").stringValue = text.en;
        }

        static void SetCard(SerializedProperty card, string id, float unlockTemp, LText title, LText body)
        {
            card.FindPropertyRelative("id").stringValue = id;
            card.FindPropertyRelative("unlockTemp").floatValue = unlockTemp;
            SetLText(card.FindPropertyRelative("title"), title);
            SetLText(card.FindPropertyRelative("body"), body);
        }
    }
}
#endif
