using System.Collections;
using Olam.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Olam.UI
{
    /// <summary>
    /// Экран загрузки пространства: обложка во весь экран, подзаголовок, название и вопрос урока.
    /// Появляется по клику на портал хаба, переживает загрузку сцены и гаснет уже в пространстве.
    /// Сцену грузит ScenePortal (через 0,15 с после клика) — экран только закрывает переход.
    /// </summary>
    public class OlamLoadingScreen : MonoBehaviour
    {
        const float FadeIn = 0.12f, Hold = 1.8f, FadeOut = 0.6f, Timeout = 12f;

        static readonly LText LoadingText = new("Загрузка…", "Yuklanmoqda…", "Loading…");
        static OlamLoadingScreen current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => current = null;

        CanvasGroup group;
        bool sceneLoaded;

        public static void Show(SpaceInfo space)
        {
            if (space == null || current != null) return;
            var root = Build(space);
            root.GetComponent<CanvasGroup>().alpha = 0f;
            DontDestroyOnLoad(root);
            current = root.AddComponent<OlamLoadingScreen>();
        }

        /// <summary>Собирает экран: для игры и для снимка в редакторе.</summary>
        public static GameObject Build(SpaceInfo space)
        {
            var canvas = OlamUiKit.Overlay("OLAM_LoadingScreen", 1000);
            var root = canvas.transform;
            OlamUiKit.Fill(OlamUiKit.Stretch(root, "Back"), OlamUiKit.Lapis);
            var art = OlamUiKit.Stretch(root, "Cover");
            var image = art.gameObject.AddComponent<RawImage>();
            image.texture = space.cover;
            image.raycastTarget = false;
            art.gameObject.AddComponent<CoverFit>().focus = space.focus;

            // тень снизу — под текстом; на светлых обложках бирюза без неё не читается
            var shade = OlamUiKit.Rect(root, "Shade", Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, 780f));
            OlamUiKit.Ramp(shade, OlamUiKit.Alpha(OlamUiKit.Lapis, 0.95f), false);

            var corner = Vector2.zero;
            var kicker = OlamUiKit.Label(root, "Kicker", corner, corner, new Vector2(120f, 300f), new Vector2(1500f, 44f),
                28f, OlamUiKit.Turquoise, TextAlignmentOptions.BottomLeft);
            kicker.fontStyle = FontStyles.UpperCase;
            kicker.characterSpacing = 6f;
            kicker.text = space.kicker.Get();
            var title = OlamUiKit.Label(root, "Title", corner, corner, new Vector2(120f, 180f), new Vector2(1600f, 120f),
                100f, OlamUiKit.Parchment, TextAlignmentOptions.BottomLeft);
            title.text = space.title.Get();
            var question = OlamUiKit.Label(root, "Question", corner, corner, new Vector2(122f, 96f), new Vector2(1400f, 80f),
                36f, OlamUiKit.Alpha(OlamUiKit.Parchment, 0.88f), TextAlignmentOptions.BottomLeft);
            question.fontStyle = FontStyles.Italic;
            question.text = space.question.Get();

            var status = OlamUiKit.Label(root, "Status", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-120f, 100f),
                new Vector2(500f, 40f), 24f, OlamUiKit.Alpha(OlamUiKit.Turquoise, 0.85f), TextAlignmentOptions.BottomRight);
            status.text = LoadingText.Get();

            var mark = OlamUiKit.Rect(root, "Mark", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(120f, -80f), new Vector2(84f, 84f));
            mark.gameObject.AddComponent<OlamMark>().color = OlamUiKit.Alpha(OlamUiKit.Parchment, 0.92f);
            return canvas.gameObject;
        }

        void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        void OnDestroy()
        {
            if (current == this) current = null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => sceneLoaded = true;

        IEnumerator Start()
        {
            group = GetComponent<CanvasGroup>();
            yield return Fade(0f, 1f, FadeIn);
            // если сцена так и не загрузилась (нет в Build Settings), экран всё равно уходит
            for (float waited = 0f; !sceneLoaded && waited < Timeout; waited += Time.unscaledDeltaTime)
                yield return null;
            yield return new WaitForSecondsRealtime(Hold);
            yield return Fade(1f, 0f, FadeOut);
            Destroy(gameObject);
        }

        IEnumerator Fade(float from, float to, float time)
        {
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / time);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
