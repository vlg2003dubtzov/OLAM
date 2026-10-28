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
    /// Визуальный апгрейд (OLAM-шейдеры) + образовательный слой флоры/фауны в Чаткале.
    /// </summary>
    public static class OlamVisualEducationUpgrade
    {
        [MenuItem("OLAM/Upgrade Visuals + Chatkal Field Lesson")]
        public static void Run()
        {
            EnsureFolders();
            CreateMaterials();
            var juniper = MakeEntry("archa_juniper", "Арча (можжевельник)", "Archa", "Juniper",
                "Juniperus seravschanica",
                "Вечнозелёный хвойный кустарник / дерево Западного Тянь-Шаня. Хвоя колючая, ягоды сизые — ориентир для краеведов.",
                "G‘arbiy Tyan-Shanning doim yashil ignabargli o‘simligi. Ignalari tikanli, mevalari ko‘kimtir.",
                "Evergreen conifer of the Western Tian Shan. Spiny needles and bluish berries — a landmark for field study.");
            var tulip = MakeEntry("tulip_greigii", "Тюльпан Грейга", "Greyg lolasii", "Greig's tulip",
                "Tulipa greigii",
                "Эндемик Средней Азии. Крупный красный цветок с тёмным пятном у основания — символ весенних предгорий.",
                "O‘rta Osiyo endemigi. Katta qizil gul, asosida qorong‘u dog‘ — bahorgi adirlar belgisi.",
                "A Central Asian endemic. Large red flower with a dark basal blotch — symbol of spring foothills.");
            var ephedra = MakeEntry("ephedra", "Хвойник (эфедра)", "Qirqbo‘g‘im", "Ephedra",
                "Ephedra equisetina",
                "Засухоустойчивый кустарник каменистых склонов. Стебли членистые, без листьев — пример приспособления к аридному климату.",
                "Qurg‘oqchilikka chidamli buta. Barglarisiz bo‘g‘imli poyalar — qurg‘oq iqlimga moslashuv.",
                "Drought-tolerant shrub of rocky slopes. Jointed leafless stems show arid adaptation.");
            var leopard = AssetDatabase.LoadAssetAtPath<EncyclopediaEntry>(
                "Assets/OLAM/ScriptableObjects/Entry_SnowLeopard.asset");

            ApplyShaderToMat("Mat_DesertSand", "OLAM/Sand Ground", new Color(0.76f, 0.64f, 0.42f));
            ApplyShaderToMat("Mat_ChatkalSoil", "OLAM/Sand Ground", new Color(0.22f, 0.2f, 0.14f));
            ApplyShaderToMat("Mat_TandoorClay", "OLAM/Clay Stylized", new Color(0.58f, 0.38f, 0.24f));
            ApplyShaderToMat("Mat_RuinClay", "OLAM/Clay Stylized", new Color(0.55f, 0.42f, 0.3f));
            ApplyShaderToMat("Mat_Piece_wall_n", "OLAM/Clay Stylized", new Color(0.7f, 0.55f, 0.38f));
            ApplyShaderToMat("Mat_Piece_wall_e", "OLAM/Clay Stylized", new Color(0.68f, 0.52f, 0.36f));
            ApplyShaderToMat("Mat_Piece_column", "OLAM/Clay Stylized", new Color(0.72f, 0.58f, 0.42f));
            ApplyShaderToMat("Mat_Piece_arch", "OLAM/Clay Stylized", new Color(0.66f, 0.5f, 0.34f));
            ApplyShaderToMat("Mat_Rock", "OLAM/Clay Stylized", new Color(0.32f, 0.3f, 0.28f));

            UpgradeChatkal(juniper, tulip, ephedra, leopard);
            AssetDatabase.SaveAssets();
            Debug.Log("OLAM: шейдеры применены, в Chatkal добавлен полевой урок (флора + фауна).");
        }

        static void UpgradeChatkal(
            EncyclopediaEntry juniper, EncyclopediaEntry tulip,
            EncyclopediaEntry ephedra, EncyclopediaEntry leopard)
        {
            var path = "Assets/OLAM/Scenes/ChatkalNightTrail.unity";
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            // системы
            var systems = GameObject.Find("OLAM_Systems");
            if (systems == null) systems = new GameObject("OLAM_Systems");
            if (systems.GetComponent<Herbarium>() == null) systems.AddComponent<Herbarium>();

            // удалить старый дневник при перезапуске
            var oldNb = GameObject.Find("FieldNotebook");
            if (oldNb != null) Object.DestroyImmediate(oldNb);

            var nbGo = new GameObject("FieldNotebook");
            nbGo.transform.position = new Vector3(-1.8f, 1.7f, 0.8f);
            nbGo.transform.localScale = Vector3.one * 0.0075f;
            var canvas = nbGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            nbGo.AddComponent<CanvasScaler>();
            nbGo.AddComponent<GraphicRaycaster>();

            var q = MakeTmp(nbGo.transform, "Question", new Vector2(0, 140), 30);
            q.rectTransform.sizeDelta = new Vector2(720, 100);
            q.color = new Color(1f, 0.92f, 0.7f);
            var list = MakeTmp(nbGo.transform, "List", new Vector2(0, -40), 28);
            list.rectTransform.sizeDelta = new Vector2(720, 280);
            list.alignment = TextAlignmentOptions.TopLeft;

            var notebook = nbGo.AddComponent<FieldNotebook>();
            Wire(notebook, "questionText", q);
            Wire(notebook, "listText", list);
            Wire(notebook, "canvas", canvas);

            var so = new SerializedObject(notebook);
            var items = so.FindProperty("items");
            items.arraySize = 4;
            SetNotebookItem(items.GetArrayElementAtIndex(0), "archa_juniper", "Арча", "Archa", "Juniper", juniper);
            SetNotebookItem(items.GetArrayElementAtIndex(1), "tulip_greigii", "Тюльпан Грейга", "Greyg lolasii", "Greig's tulip", tulip);
            SetNotebookItem(items.GetArrayElementAtIndex(2), "ephedra", "Эфедра", "Qirqbo‘g‘im", "Ephedra", ephedra);
            SetNotebookItem(items.GetArrayElementAtIndex(3), "snow_leopard", "Снежный барс (по следам)", "Qor qoploni", "Snow leopard (by tracks)", leopard);
            so.ApplyModifiedPropertiesWithoutUndo();

            // квест гербария
            var qm = systems.GetComponent<QuestManager>();
            if (qm != null)
            {
                var qso = new SerializedObject(qm);
                var goals = qso.FindProperty("goals");
                // добавить цель полевого дневника если нет
                bool has = false;
                for (int i = 0; i < goals.arraySize; i++)
                    if (goals.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == "field_notebook")
                        has = true;
                if (!has)
                {
                    int i = goals.arraySize;
                    goals.arraySize++;
                    var g = goals.GetArrayElementAtIndex(i);
                    g.FindPropertyRelative("id").stringValue = "field_notebook";
                    g.FindPropertyRelative("title").FindPropertyRelative("ru").stringValue = "Заполни полевой дневник";
                    g.FindPropertyRelative("title").FindPropertyRelative("uz").stringValue = "Dala daftarini to‘ldir";
                    g.FindPropertyRelative("title").FindPropertyRelative("en").stringValue = "Complete the field notebook";
                    g.FindPropertyRelative("required").intValue = 1;
                }
                qso.ApplyModifiedPropertiesWithoutUndo();
            }

            SpawnPlant("Plant_Juniper", new Vector3(-3.5f, 0.5f, 3f), new Color(0.18f, 0.35f, 0.14f), juniper, false, 1.4f);
            SpawnPlant("Plant_Tulip", new Vector3(2.8f, 0.25f, 2.2f), new Color(0.85f, 0.15f, 0.12f), tulip, true, 0.45f);
            SpawnPlant("Plant_Ephedra", new Vector3(-2.2f, 0.35f, 6.5f), new Color(0.35f, 0.45f, 0.28f), ephedra, false, 0.9f);

            // foliagemat на «кусты»-заглушки
            var foliage = AssetDatabase.LoadAssetAtPath<Material>("Assets/OLAM/Materials/Mat_FoliageWind.mat");
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (r == null) continue;
                if (r.name.StartsWith("Rock_")) continue;
                if (r.sharedMaterial != null && r.sharedMaterial.name.Contains("ChatkalSoil"))
                    r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/OLAM/Materials/Mat_ChatkalSoil.mat");
            }

            // обновить подсказку
            var tip = GameObject.Find("LearningTip");
            if (tip != null)
            {
                var tmp = tip.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                    tmp.text = "OLAM · Чаткал — полевой урок\nF фонарик · найди 3 растения + следы барса\nСмотри дневник слева";
            }

            EditorSceneManager.SaveScene(scene);
        }

        static void SpawnPlant(string name, Vector3 pos, Color color, EncyclopediaEntry entry, bool needLight, float height)
        {
            var old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.35f, height * 0.5f, 0.35f);
            var mat = new Material(Shader.Find("OLAM/Foliage Wind")) { color = color };
            // инстанс в сцене ок; лучше шаренный
            var shared = AssetDatabase.LoadAssetAtPath<Material>("Assets/OLAM/Materials/Mat_FoliageWind.mat");
            if (shared != null)
            {
                var m = new Material(shared) { color = color, name = name + "_Mat" };
                AssetDatabase.CreateAsset(m, $"Assets/OLAM/Materials/{name}_Mat.mat");
                go.GetComponent<MeshRenderer>().sharedMaterial = m;
            }
            else
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;

            go.AddComponent<OlamInteractable>();
            var disc = go.AddComponent<DiscoverableSpecimen>();
            Wire(disc, "entry", entry);
            WireBool(disc, "requireFlashlight", needLight);
            SetString(disc, "notebookId", entry.Id);

            var label = new GameObject("Hint");
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            label.transform.localScale = Vector3.one * 0.02f;
            label.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var t = MakeTmp(label.transform, "T", Vector2.zero, 28);
            t.text = "?";
            t.color = new Color(1f, 0.95f, 0.5f);
        }

        static void CreateMaterials()
        {
            CreateMatWithShader("Mat_FoliageWind", "OLAM/Foliage Wind", new Color(0.22f, 0.48f, 0.18f));
            CreateMatWithShader("Mat_DesertSand", "OLAM/Sand Ground", new Color(0.76f, 0.64f, 0.42f));
            CreateMatWithShader("Mat_ChatkalSoil", "OLAM/Sand Ground", new Color(0.2f, 0.18f, 0.12f));
            CreateMatWithShader("Mat_TandoorClay", "OLAM/Clay Stylized", new Color(0.58f, 0.38f, 0.24f));
        }

        static void CreateMatWithShader(string name, string shaderName, Color color)
        {
            string path = $"Assets/OLAM/Materials/{name}.mat";
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("Shader not found yet (will resolve after import): " + shaderName);
                return;
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            EditorUtility.SetDirty(mat);
        }

        static void ApplyShaderToMat(string matName, string shaderName, Color color)
        {
            string path = $"Assets/OLAM/Materials/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            var shader = Shader.Find(shaderName);
            if (mat == null || shader == null) return;
            mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            EditorUtility.SetDirty(mat);
        }

        static EncyclopediaEntry MakeEntry(string id, string ru, string uz, string en,
            string sci, string dru, string duz, string den)
        {
            string path = $"Assets/OLAM/ScriptableObjects/Entry_{id}.asset";
            var entry = AssetDatabase.LoadAssetAtPath<EncyclopediaEntry>(path);
            if (entry == null)
            {
                entry = ScriptableObject.CreateInstance<EncyclopediaEntry>();
                AssetDatabase.CreateAsset(entry, path);
            }
            var so = new SerializedObject(entry);
            so.FindProperty("id").stringValue = id;
            so.FindProperty("scientificName").stringValue = sci;
            so.FindProperty("title").FindPropertyRelative("ru").stringValue = ru;
            so.FindProperty("title").FindPropertyRelative("uz").stringValue = uz;
            so.FindProperty("title").FindPropertyRelative("en").stringValue = en;
            so.FindProperty("description").FindPropertyRelative("ru").stringValue = dru;
            so.FindProperty("description").FindPropertyRelative("uz").stringValue = duz;
            so.FindProperty("description").FindPropertyRelative("en").stringValue = den;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(entry);
            return entry;
        }

        static void SetNotebookItem(SerializedProperty p, string id, string ru, string uz, string en, Object entry)
        {
            p.FindPropertyRelative("id").stringValue = id;
            p.FindPropertyRelative("title").FindPropertyRelative("ru").stringValue = ru;
            p.FindPropertyRelative("title").FindPropertyRelative("uz").stringValue = uz;
            p.FindPropertyRelative("title").FindPropertyRelative("en").stringValue = en;
            p.FindPropertyRelative("entry").objectReferenceValue = entry;
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

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/OLAM/Materials"))
                AssetDatabase.CreateFolder("Assets/OLAM", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/OLAM/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets/OLAM", "ScriptableObjects");
            if (!AssetDatabase.IsValidFolder("Assets/OLAM/Shaders"))
                AssetDatabase.CreateFolder("Assets/OLAM", "Shaders");
        }
    }
}
#endif
