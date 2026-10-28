#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Olam.Nature;

namespace Olam.EditorTools
{
    /// <summary>
    /// Шейдеры травы/листвы + VR-пресет рендера. Координируется с Claude Code через AGENT_COORD.md.
    /// </summary>
    public static class OlamNatureShadersUpgrade
    {
        const string MatFolder = "Assets/OLAM/Materials";

        [MenuItem("OLAM/Nature Shaders + VR Perf")]
        public static void Run()
        {
            EnsureFolders();
            CreateOrUpdateMat("Mat_FoliageWind", "OLAM/Foliage Wind", new Color(0.22f, 0.48f, 0.18f));
            CreateOrUpdateMat("Mat_GrassChatkal", "OLAM/Grass", new Color(0.26f, 0.48f, 0.16f), tip: new Color(0.5f, 0.68f, 0.25f));
            CreateOrUpdateMat("Mat_DesertSand", "OLAM/Sand Ground", new Color(0.76f, 0.64f, 0.42f));
            CreateOrUpdateMat("Mat_ChatkalSoil", "OLAM/Sand Ground", new Color(0.2f, 0.18f, 0.12f));
            CreateOrUpdateMat("Mat_TandoorClay", "OLAM/Clay Stylized", new Color(0.58f, 0.38f, 0.24f));

            ApplyVrUrpPreset();
            SeedGrassInChatkal();
            AssetDatabase.SaveAssets();
            Debug.Log("OLAM: Nature shaders + VR perf applied. См. AGENT_COORD.md");
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/OLAM"))
                AssetDatabase.CreateFolder("Assets", "OLAM");
            if (!AssetDatabase.IsValidFolder(MatFolder))
                AssetDatabase.CreateFolder("Assets/OLAM", "Materials");
        }

        static void CreateOrUpdateMat(string name, string shaderName, Color color, Color? tip = null)
        {
            string path = $"{MatFolder}/{name}.mat";
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning("OLAM: shader not found: " + shaderName);
                return;
            }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
                mat.shader = shader;

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            if (tip.HasValue && mat.HasProperty("_TipColor"))
                mat.SetColor("_TipColor", tip.Value);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
        }

        static void ApplyVrUrpPreset()
        {
            // Render Scale 0.7, без теней — по .cursorrules
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
            {
                Debug.LogWarning("OLAM: URP asset не найден в GraphicsSettings.");
                return;
            }

            var so = new SerializedObject(urp);
            var rs = so.FindProperty("m_RenderScale");
            if (rs != null) rs.floatValue = 0.7f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(urp);

            foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light == null) continue;
                light.shadows = LightShadows.None;
            }

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.35f, 0.38f, 0.42f);
            Debug.Log("OLAM: URP renderScale=0.7, shadows off, ambient flat.");
        }

        static void SeedGrassInChatkal()
        {
            const string scenePath = "Assets/OLAM/Scenes/ChatkalNightTrail.unity";
            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning("OLAM: ChatkalNightTrail не найден — траву пропустим.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MatFolder}/Mat_GrassChatkal.mat");
            var soil = AssetDatabase.LoadAssetAtPath<Material>($"{MatFolder}/Mat_ChatkalSoil.mat");
            var foliage = AssetDatabase.LoadAssetAtPath<Material>($"{MatFolder}/Mat_FoliageWind.mat");

            var ground = GameObject.Find("Ground_Chatkal");
            if (ground != null && soil != null)
                ground.GetComponent<MeshRenderer>().sharedMaterial = soil;

            // растения-капсулы → foliage
            if (foliage != null)
            {
                foreach (var name in new[] { "Plant_Juniper", "Plant_Ephedra" })
                {
                    var p = GameObject.Find(name);
                    if (p == null) continue;
                    var r = p.GetComponent<MeshRenderer>();
                    if (r != null)
                    {
                        var m = new Material(foliage) { name = name + "_Foliage" };
                        if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
                            m.SetColor("_BaseColor", r.sharedMaterial.GetColor("_BaseColor"));
                        m.enableInstancing = true;
                        string matPath = $"{MatFolder}/{name}_Foliage.mat";
                        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                        if (existing == null)
                            AssetDatabase.CreateAsset(m, matPath);
                        else
                        {
                            existing.shader = foliage.shader;
                            existing.CopyPropertiesFromMaterial(m);
                            EditorUtility.SetDirty(existing);
                            m = existing;
                        }
                        r.sharedMaterial = m;
                    }
                }
            }

            var old = GameObject.Find("GrassPatches");
            if (old != null) Object.DestroyImmediate(old);

            var root = new GameObject("GrassPatches");
            Vector3[] centers =
            {
                new(-2f, 0f, 2f),
                new(2.5f, 0f, 5f),
                new(-1.5f, 0f, 9f),
                new(1.2f, 0f, 12f)
            };

            for (int i = 0; i < centers.Length; i++)
            {
                var go = new GameObject($"GrassPatch_{i}");
                go.transform.SetParent(root.transform, false);
                go.transform.position = centers[i];
                var patch = go.AddComponent<GrassPatch>();
                var so = new SerializedObject(patch);
                so.FindProperty("grassMaterial").objectReferenceValue = mat;
                so.FindProperty("count").intValue = 40 + i * 8;
                so.FindProperty("radius").floatValue = 2.2f;
                so.FindProperty("seed").intValue = 40 + i * 11;
                so.ApplyModifiedPropertiesWithoutUndo();
                patch.Rebuild();
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log("OLAM: GrassPatches посеяны в ChatkalNightTrail.");
        }
    }
}
#endif
