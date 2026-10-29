#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Olam.Core;
using Olam.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Olam.EditorTools
{
    /// <summary>
    /// Плашки OLAM как интерфейс игры. Обложки — в UI/Covers, карточки пространств (SpaceInfo) — в UI/Spaces,
    /// меню хаба (OlamHubMenu) ставит оформление хаба: OLAM → Environment → Dress Scenes.
    /// Снимки интерфейса: OLAM → UI → Capture Menu → Logs/OLAM_Captures/ui_*.png.
    /// </summary>
    public static class OlamMenuUiBuilder
    {
        const string Ui = "Assets/OLAM/UI";
        const string CoverDir = Ui + "/Covers";
        const string SpaceDir = Ui + "/Spaces";
        const string MainCover = CoverDir + "/Cover_Main.jpg";

        struct Def
        {
            public string id, scene, cover;
            public Vector2 focus;
            public LText title, kicker, question;
        }

        // узбекские строки ждут проверки носителем
        static readonly Def[] Defs =
        {
            new()
            {
                id = "Nature", scene = "NatureDome", cover = "Cover_Nature.jpg", focus = new Vector2(0.40f, 0.5f),
                title = new LText("Купол природы", "Tabiat gumbazi", "Nature Dome"),
                kicker = new LText("Природа · Экспедиция юного натуралиста", "Tabiat · Yosh tabiatshunos ekspeditsiyasi",
                    "Nature · Young naturalist's expedition"),
                question = new LText("Как растения и звери выживают в пустыне, горах и тугаях?",
                    "O‘simliklar va hayvonlar cho‘l, tog‘ va to‘qaylarda qanday yashaydi?",
                    "How do plants and animals survive in deserts, mountains and tugai forests?")
            },
            new()
            {
                id = "Tandoor", scene = "Tandoor", cover = "Cover_Tandoor.jpg", focus = new Vector2(0.5f, 0.56f),
                title = new LText("Тандыр", "Tandir", "Tandoor"),
                kicker = new LText("Кухня · Хлеб из глиняной печи", "Oshxona · Loy tandirda non", "Kitchen · Bread from a clay oven"),
                question = new LText("Почему лепёшка держится на стенке тандыра и не падает в огонь?",
                    "Nega non tandir devorida ushlanib turadi va olovga tushmaydi?",
                    "Why does the flatbread stay on the tandoor wall instead of falling into the fire?")
            },
            new()
            {
                id = "ToprakKala", scene = "ToprakKala", cover = "Cover_ToprakKala.jpg", focus = new Vector2(0.5f, 0.42f),
                title = new LText("Топрак-кала", "Tuproqqal’a", "Toprak-Kala"),
                kicker = new LText("История · Каракалпакстан", "Tarix · Qoraqalpog‘iston", "History · Karakalpakstan"),
                question = new LText("Как археологи узнают, какой была древняя крепость?",
                    "Arxeologlar qadimiy qal’a qanday bo‘lganini qanday bilib oladi?",
                    "How do archaeologists find out what an ancient fortress looked like?")
            }
        };

        /// <summary>Карточки пространств по имени сцены; обложки получают настройки импорта для арок и экрана.</summary>
        public static Dictionary<string, SpaceInfo> EnsureSpaces()
        {
            if (!AssetDatabase.IsValidFolder(SpaceDir)) AssetDatabase.CreateFolder(Ui, "Spaces");
            ImportCover(MainCover);
            var result = new Dictionary<string, SpaceInfo>();
            foreach (var d in Defs)
            {
                string coverPath = $"{CoverDir}/{d.cover}";
                ImportCover(coverPath);
                string path = $"{SpaceDir}/Space_{d.id}.asset";
                var info = AssetDatabase.LoadAssetAtPath<SpaceInfo>(path);
                if (info == null)
                {
                    info = ScriptableObject.CreateInstance<SpaceInfo>();
                    AssetDatabase.CreateAsset(info, path);
                }
                info.sceneName = d.scene;
                info.cover = AssetDatabase.LoadAssetAtPath<Texture2D>(coverPath);
                info.focus = d.focus;
                info.title = d.title;
                info.kicker = d.kicker;
                info.question = d.question;
                EditorUtility.SetDirty(info);
                result[d.scene] = info;
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        /// <summary>Размер не подгоняется под степень двойки (иначе исказится пропорция), мипмапы — для арок вдали.</summary>
        static void ImportCover(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter ti) return;
            if (ti.textureType == TextureImporterType.Default && ti.npotScale == TextureImporterNPOTScale.None &&
                ti.mipmapEnabled && ti.wrapMode == TextureWrapMode.Clamp && ti.maxTextureSize == 2048)
                return;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.mipmapEnabled = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Trilinear;
            ti.anisoLevel = 4;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }

        /// <summary>Меню хаба под корнем оформления: титул, подсказка, плашки пространств, экран загрузки.</summary>
        public static void AddHubMenu(Transform root, List<(ScenePortal portal, Renderer cover, SpaceInfo info)> covered)
        {
            var go = new GameObject("OLAM_HubMenu");
            go.transform.SetParent(root, false);
            var so = new SerializedObject(go.AddComponent<OlamHubMenu>());
            so.FindProperty("titleArt").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Texture2D>(MainCover);
            var list = so.FindProperty("spaces");
            list.arraySize = covered.Count;
            for (int i = 0; i < covered.Count; i++)
            {
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("portal").objectReferenceValue = covered[i].portal.GetComponent<OlamInteractable>();
                e.FindPropertyRelative("info").objectReferenceValue = covered[i].info;
                e.FindPropertyRelative("cover").objectReferenceValue = covered[i].cover;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("OLAM/UI/Capture Menu")]
        public static void CaptureMenu()
        {
            Directory.CreateDirectory(OlamSceneDressing.CaptureDir);
            var spaces = EnsureSpaces();
            OlamSceneDressing.InScene("OlamHub", false, scene =>
            {
                OlamHubMenu menu = null;
                Camera cam = null;
                foreach (var g in scene.GetRootGameObjects())
                {
                    if (menu == null) menu = g.GetComponentInChildren<OlamHubMenu>(true);
                    if (cam == null) cam = g.GetComponentInChildren<Camera>(true);
                }
                if (menu == null || cam == null)
                {
                    Debug.LogWarning("OLAM UI: в хабе нет меню или камеры — сначала OLAM → Environment → Dress Scenes");
                    return;
                }
                OlamSceneDressing.PrepareCapture(scene);
                Shot(cam, "ui_title", menu.BuildPreview(-1));
                menu.ClearPreview();
                for (int i = 0; i < 3; i++)
                {
                    Shot(cam, $"ui_hub_{i}", menu.BuildPreview(i));
                    menu.ClearPreview();
                }
                foreach (var info in spaces.Values)
                {
                    var screen = OlamLoadingScreen.Build(info);
                    Shot(cam, $"ui_loading_{info.sceneName}", screen.GetComponent<Canvas>());
                }
            });
            Debug.Log($"OLAM UI: снимки меню → {OlamSceneDressing.CaptureDir}/ui_*.png");
        }

        /// <summary>Холсты на время снимка рисует камера сцены; потом они удаляются.</summary>
        internal static void Shot(Camera cam, string name, params Canvas[] canvases)
        {
            const int w = 1600, h = 900;
            var rt = new RenderTexture(w, h, 24);
            var prev = cam.targetTexture;
            try
            {
                cam.targetTexture = rt;
                foreach (var canvas in canvases)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = cam;
                    canvas.planeDistance = Mathf.Max(cam.nearClipPlane * 2f, 0.5f);
                    if (canvas.TryGetComponent<CanvasScaler>(out var scaler)) scaler.enabled = false;
                    canvas.scaleFactor = h / 1080f;
                }
                Canvas.ForceUpdateCanvases();
                foreach (var canvas in canvases)
                {
                    foreach (var fit in canvas.GetComponentsInChildren<CoverFit>(true)) fit.Apply();
                    foreach (var text in canvas.GetComponentsInChildren<TMP_Text>(true)) text.ForceMeshUpdate();
                }
                Canvas.ForceUpdateCanvases();
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                File.WriteAllBytes($"{OlamSceneDressing.CaptureDir}/{name}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            finally
            {
                cam.targetTexture = prev;
                RenderTexture.active = null;
                rt.Release();
                Object.DestroyImmediate(rt);
                foreach (var canvas in canvases)
                    if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
            }
        }
    }
}
#endif
