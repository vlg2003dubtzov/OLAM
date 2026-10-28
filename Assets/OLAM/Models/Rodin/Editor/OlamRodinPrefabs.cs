#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Olam.EditorTools
{
    /// <summary>
    /// Меню: OLAM → Rodin → Build Prefabs.
    /// Для каждого набора в Models/Rodin/Processed/&lt;набор&gt;/ — материал URP/Lit,
    /// для каждой модели набора — префаб в Models/Rodin/Prefabs/.
    /// Открытую сцену не трогает: префабы собираются в сцене-превью.
    /// </summary>
    public static class OlamRodinPrefabs
    {
        const string ProcessedRoot = "Assets/OLAM/Models/Rodin/Processed";
        const string PrefabRoot = "Assets/OLAM/Models/Rodin/Prefabs";

        // Тонкие листья и ветки должны быть видны с обеих сторон
        static readonly string[] DoubleSidedSets = { "tulip_greigii", "saxaul_black" };

        [MenuItem("OLAM/Rodin/Build Prefabs")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh();
            if (!AssetDatabase.IsValidFolder(PrefabRoot))
                AssetDatabase.CreateFolder("Assets/OLAM/Models/Rodin", "Prefabs");

            int count = 0;
            foreach (var setDir in AssetDatabase.GetSubFolders(ProcessedRoot))
            {
                string set = Path.GetFileName(setDir);
                var mat = BuildMaterial(setDir, set);
                foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { setDir }))
                {
                    BuildPrefab(AssetDatabase.GUIDToAssetPath(guid), set, mat);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"OLAM Rodin: собрано префабов — {count} → {PrefabRoot}");
        }

        static Material BuildMaterial(string setDir, string set)
        {
            string path = $"{setDir}/{set}_Mat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = set + "_Mat" };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{setDir}/{set}_BaseColor.png"));
            mat.SetColor("_BaseColor", Color.white);

            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{setDir}/{set}_Normal.png");
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1f);
                mat.EnableKeyword("_NORMALMAP");
            }

            // R — металл, A — гладкость (1 − шероховатость glTF), так её пакует rodin_process.py
            var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>($"{setDir}/{set}_MetallicSmoothness.png");
            if (metallic != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallic);
                mat.SetFloat("_Smoothness", 1f);
                mat.SetFloat("_SmoothnessTextureChannel", 0f);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }

            if (System.Array.IndexOf(DoubleSidedSets, set) >= 0)
            {
                mat.SetFloat("_Cull", 0f);
                mat.doubleSidedGI = true;
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void BuildPrefab(string modelPath, string set, Material mat)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            string name = Path.GetFileNameWithoutExtension(modelPath);
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
                go.name = name;
                foreach (var r in go.GetComponentsInChildren<MeshRenderer>())
                    r.sharedMaterial = mat;
                AddCollider(go, set);
                PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabRoot}/{name}.prefab");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        // Тандыр и стол стоят на месте — точный MeshCollider; барс и лепёшки — BoxCollider по габаритам.
        static void AddCollider(GameObject go, string set)
        {
            var filter = go.GetComponentInChildren<MeshFilter>();
            if (filter == null) return;
            if (set.StartsWith("tandoor") || set == "work_table")
                filter.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
            else
                filter.gameObject.AddComponent<BoxCollider>();
        }
    }
}
#endif
