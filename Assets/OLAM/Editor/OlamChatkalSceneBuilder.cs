#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Olam.Core;
using Olam.Nature;

namespace Olam.EditorTools
{
    /// <summary>
    /// Меню: OLAM → Build Chatkal Night Trail
    /// Ночная тропа: фонарик + следы → появление зверя.
    /// </summary>
    public static class OlamChatkalSceneBuilder
    {
        const string ScenePath = "Assets/OLAM/Scenes/ChatkalNightTrail.unity";
        const string EntryPath = "Assets/OLAM/ScriptableObjects/Entry_SnowLeopard.asset";

        [MenuItem("OLAM/Build Chatkal Night Trail")]
        public static void Build()
        {
            EnsureFolders();
            var entry = CreateOrLoadSnowLeopardEntry();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Ночь
            var sun = Object.FindFirstObjectByType<Light>();
            if (sun == null)
            {
                var sunGo = new GameObject("Sun");
                sun = sunGo.AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            sun.shadows = LightShadows.None;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Ground_Chatkal";
            floor.transform.localScale = new Vector3(4f, 1f, 6f);
            floor.GetComponent<MeshRenderer>().sharedMaterial =
                CreateUrpLit("Mat_ChatkalSoil", new Color(0.18f, 0.16f, 0.12f));

            // Камни / кусты-заглушки
            for (int i = 0; i < 8; i++)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = $"Rock_{i}";
                float x = -6f + (i % 4) * 3.5f + Random.Range(-0.4f, 0.4f);
                float z = -2f + (i / 4) * 8f + Random.Range(-1f, 1f);
                rock.transform.position = new Vector3(x, 0.25f, z);
                rock.transform.localScale = Vector3.one * Random.Range(0.6f, 1.4f);
                rock.GetComponent<MeshRenderer>().sharedMaterial =
                    CreateUrpLit("Mat_Rock", new Color(0.28f, 0.27f, 0.25f));
            }

            var systems = new GameObject("OLAM_Systems");
            systems.AddComponent<Localization>();
            var dayNight = systems.AddComponent<DayNightController>();
            Wire(dayNight, "sun", sun);
            WireBool(dayNight, "isNight", true);

            var quests = systems.AddComponent<QuestManager>();
            var qso = new SerializedObject(quests);
            var goals = qso.FindProperty("goals");
            goals.arraySize = 2;
            SetGoal(goals.GetArrayElementAtIndex(0), "find_tracks", "Найди следы", "Izlarni top", "Find the tracks", 4);
            SetGoal(goals.GetArrayElementAtIndex(1), "spot_animal", "Увидь барса", "Qoplonni ko‘r", "Spot the snow leopard", 1);
            qso.ApplyModifiedPropertiesWithoutUndo();

            // InfoPanel
            var panelGo = new GameObject("InfoPanel");
            panelGo.transform.position = new Vector3(0f, 1.7f, 2.5f);
            panelGo.transform.localScale = Vector3.one * 0.008f;
            var canvas = panelGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            panelGo.AddComponent<CanvasScaler>();
            panelGo.AddComponent<GraphicRaycaster>();
            var title = MakeTmp(panelGo.transform, "Title", new Vector2(0, 80), 44);
            var sci = MakeTmp(panelGo.transform, "Scientific", new Vector2(0, 30), 28);
            var body = MakeTmp(panelGo.transform, "Body", new Vector2(0, -60), 26);
            body.rectTransform.sizeDelta = new Vector2(700, 180);
            var panel = panelGo.AddComponent<InfoPanel>();
            Wire(panel, "canvas", canvas);
            Wire(panel, "titleText", title);
            Wire(panel, "scientificText", sci);
            Wire(panel, "bodyText", body);
            canvas.enabled = false;

            // Фонарик
            var flashGo = new GameObject("Flashlight");
            var spot = flashGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.shadows = LightShadows.None;
            var flash = flashGo.AddComponent<Flashlight>();
            Wire(flash, "spot", spot);

            // Следы вдоль тропы
            var trailRoot = new GameObject("TrackTrail");
            var tracks = new AnimalTrack[4];
            Vector3[] positions =
            {
                new Vector3(-1.2f, 0.05f, 1.5f),
                new Vector3(0.4f, 0.05f, 4.5f),
                new Vector3(-0.8f, 0.05f, 7.5f),
                new Vector3(0.6f, 0.05f, 10.5f)
            };

            for (int i = 0; i < 4; i++)
            {
                var trackGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trackGo.name = $"Track_{i + 1}";
                trackGo.transform.SetParent(trailRoot.transform, false);
                trackGo.transform.position = positions[i];
                trackGo.transform.localScale = new Vector3(0.35f, 0.02f, 0.55f);
                trackGo.transform.rotation = Quaternion.Euler(0f, 15f * i, 0f);
                Object.DestroyImmediate(trackGo.GetComponent<Collider>());
                var col = trackGo.AddComponent<BoxCollider>();
                trackGo.GetComponent<MeshRenderer>().sharedMaterial =
                    CreateUrpLit("Mat_Track", new Color(0.12f, 0.1f, 0.08f));

                // RevealByLight — след почти невидим без фонаря
                var reveal = trackGo.AddComponent<RevealByLight>();
                Wire(reveal, "target", trackGo.GetComponent<MeshRenderer>());

                trackGo.AddComponent<OlamInteractable>();
                var track = trackGo.AddComponent<AnimalTrack>();
                WireBool(track, "requireFlashlight", true);
                Wire(track, "meshRenderer", trackGo.GetComponent<MeshRenderer>());
                Wire(track, "entry", entry);
                SetString(track, "trackId", $"paw_{i + 1}");
                tracks[i] = track;
            }

            var spawnPoint = new GameObject("AnimalSpawn").transform;
            spawnPoint.SetParent(trailRoot.transform, false);
            spawnPoint.position = new Vector3(0f, 0.7f, 13f);

            var spawnerGo = new GameObject("AnimalSpawner");
            spawnerGo.transform.SetParent(trailRoot.transform, false);
            var spawner = spawnerGo.AddComponent<AnimalSpawner>();
            Wire(spawner, "spawnPoint", spawnPoint);
            Wire(spawner, "entry", entry);
            var audio = spawnerGo.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            Wire(spawner, "audioSource", audio);

            var trail = trailRoot.AddComponent<TrackTrail>();
            WireBool(trail, "strictOrder", true);
            Wire(trail, "animalSpawner", spawner);
            var trailSo = new SerializedObject(trail);
            var tracksProp = trailSo.FindProperty("tracks");
            tracksProp.arraySize = tracks.Length;
            for (int i = 0; i < tracks.Length; i++)
                tracksProp.GetArrayElementAtIndex(i).objectReferenceValue = tracks[i];
            trailSo.ApplyModifiedPropertiesWithoutUndo();

            // Подсказка
            var tip = new GameObject("LearningTip");
            tip.transform.position = new Vector3(0f, 2.0f, 0.5f);
            tip.transform.localScale = Vector3.one * 0.01f;
            var tipCanvas = tip.AddComponent<Canvas>();
            tipCanvas.renderMode = RenderMode.WorldSpace;
            var tipText = MakeTmp(tip.transform, "Text", Vector2.zero, 40);
            tipText.rectTransform.sizeDelta = new Vector2(900, 140);
            tipText.text = "OLAM · Чаткал, ночь\nF — фонарик  ·  наведи свет на след  ·  ЛКМ — осмотреть";

            var cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 1.6f, -1.5f);
                cam.transform.LookAt(new Vector3(0f, 0.5f, 4f));
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.03f, 0.08f);
            }

            // Применить ночь сразу
            dayNight.SetNight(true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"OLAM: сцена сохранена → {ScenePath}");
        }

        static EncyclopediaEntry CreateOrLoadSnowLeopardEntry()
        {
            var existing = AssetDatabase.LoadAssetAtPath<EncyclopediaEntry>(EntryPath);
            if (existing != null) return existing;

            var entry = ScriptableObject.CreateInstance<EncyclopediaEntry>();
            var so = new SerializedObject(entry);
            so.FindProperty("id").stringValue = "snow_leopard";
            so.FindProperty("scientificName").stringValue = "Panthera uncia";
            so.FindProperty("title").FindPropertyRelative("ru").stringValue = "Снежный барс";
            so.FindProperty("title").FindPropertyRelative("uz").stringValue = "Qor qoploni";
            so.FindProperty("title").FindPropertyRelative("en").stringValue = "Snow leopard";
            so.FindProperty("description").FindPropertyRelative("ru").stringValue =
                "Редкий хищник высокогорий Тянь-Шаня и Памиро-Алая. Следы крупные, округлые, без отпечатков когтей.";
            so.FindProperty("description").FindPropertyRelative("uz").stringValue =
                "Tyan-Shan va Pomir-Oloy tog‘larining kam uchraydigan yirtqichi. Izi yirik, yumaloq, tirnoqsiz.";
            so.FindProperty("description").FindPropertyRelative("en").stringValue =
                "A rare highland predator of the Tian Shan. Tracks are large and round, without claw marks.";
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
            // Прозрачность для RevealByLight
            if (name == "Mat_Track")
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetOverrideTag("RenderType", "Transparent");
            }
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

        static void SetGoal(SerializedProperty g, string id, string ru, string uz, string en, int required)
        {
            g.FindPropertyRelative("id").stringValue = id;
            g.FindPropertyRelative("title").FindPropertyRelative("ru").stringValue = ru;
            g.FindPropertyRelative("title").FindPropertyRelative("uz").stringValue = uz;
            g.FindPropertyRelative("title").FindPropertyRelative("en").stringValue = en;
            g.FindPropertyRelative("required").intValue = required;
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
    }
}
#endif
