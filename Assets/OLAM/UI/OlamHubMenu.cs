using System;
using System.Collections;
using System.Collections.Generic;
using Olam.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Olam.UI
{
    /// <summary>
    /// Главное меню OLAM в хабе, плашки как интерфейс: титульный экран с главной плашкой (раз за запуск игры),
    /// подсказка выбора, плашка пространства при наведении на арку с его обложкой и экран загрузки при входе.
    /// Порталы не трогает: слушает клик того же OlamInteractable, на который подписан ScenePortal.
    /// </summary>
    public class OlamHubMenu : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public OlamInteractable portal;
            public SpaceInfo info;
            public Renderer cover;
        }

        [SerializeField] Texture2D titleArt;
        [SerializeField] Entry[] spaces = Array.Empty<Entry>();
        [SerializeField, Range(0f, 1f)] float coverDim = 0.82f;

        static readonly LText SloganText = new("Узбекистан, который ученик проходит сам",
            "O‘quvchi O‘zbekistonni o‘zi kashf etadi", "Uzbekistan, explored first-hand");
        static readonly LText StartText = new("Начать", "Boshlash", "Start");
        static readonly LText ProjectText = new("Совместный российско-узбекский образовательный проект",
            "Rossiya va O‘zbekistonning qo‘shma ta’lim loyihasi", "A joint Russian–Uzbek educational project");
        static readonly LText ChooseText = new("Выберите пространство", "Makonni tanlang", "Choose a space");
        static readonly LText EnterText = new("Нажмите, чтобы войти", "Kirish uchun bosing", "Click to enter");
        static readonly LText DoneText = new("пройдено", "bajarildi", "completed");
        static readonly (Language lang, string code)[] Languages =
            { (Language.Ru, "RU"), (Language.Uz, "O‘Z"), (Language.En, "EN") };
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        // титул — раз за запуск: после возвращения из пространства сразу двор
        static bool titleSeen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() => titleSeen = false;   // Play в редакторе без перезагрузки домена

        Canvas titleCanvas;
        CanvasGroup titleGroup, hudGroup, plateGroup;
        TMP_Text slogan, startLabel, project, choose, plateKicker, plateTitle, plateQuestion, plateHint;
        readonly List<(Language lang, TMP_Text label)> languageLabels = new();
        readonly List<(OlamInteractable portal, Action handler)> handlers = new();
        List<OlamInteractable> paused;
        MaterialPropertyBlock block;
        Entry hovered;
        Camera cam;
        bool closing;

        void Awake()
        {
            OlamUiKit.EnsureEventSystem();
            BuildHud();
            foreach (var space in spaces)
            {
                if (space == null || space.portal == null || space.info == null) continue;
                var info = space.info;
                Action handler = () => OlamLoadingScreen.Show(info);
                space.portal.Selected += handler;
                handlers.Add((space.portal, handler));
                Dim(space, coverDim);
            }
            if (!titleSeen && titleArt != null) OpenTitle();
            else hudGroup.alpha = 1f;
        }

        void Start()
        {
            if (Localization.Instance != null) Localization.Instance.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (Localization.Instance != null) Localization.Instance.Changed -= Refresh;
            foreach (var (portal, handler) in handlers)
                if (portal != null) portal.Selected -= handler;
        }

        void Update()
        {
            if (titleCanvas != null)
            {
                var keys = Keyboard.current;
                if (keys != null && (keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame)) CloseTitle();
                return;
            }
            var next = Pointed();
            if (next != hovered)
            {
                if (hovered != null) Dim(hovered, coverDim);
                hovered = next;
                if (hovered != null)
                {
                    Dim(hovered, 1f);
                    ShowPlate(hovered.info);
                }
            }
            plateGroup.alpha = Mathf.MoveTowards(plateGroup.alpha, hovered != null ? 1f : 0f, Time.unscaledDeltaTime * 6f);
        }

        /// <summary>Пространство, на арку которого смотрит курсор (позже — луч контроллера).</summary>
        Entry Pointed()
        {
            if (cam == null) cam = Camera.main;
            var mouse = Mouse.current;
            if (cam == null || mouse == null) return null;
            if (!Physics.Raycast(cam.ScreenPointToRay(mouse.position.ReadValue()), out var hit, 60f)) return null;
            foreach (var space in spaces)
                if (space != null && space.portal != null && space.info != null &&
                    hit.collider.transform.IsChildOf(space.portal.transform))
                    return space;
            return null;
        }

        // ---------- титульный экран ----------

        void OpenTitle()
        {
            BuildTitle();
            hudGroup.alpha = 0f;
            // пока открыт титул, клики не должны доходить до порталов за ним
            paused = new List<OlamInteractable>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var item in root.GetComponentsInChildren<OlamInteractable>())
                    if (item.enabled)
                    {
                        item.enabled = false;
                        paused.Add(item);
                    }
        }

        public void CloseTitle()
        {
            if (titleCanvas == null || closing) return;
            closing = true;
            titleSeen = true;
            StartCoroutine(FadeTitle());
        }

        IEnumerator FadeTitle()
        {
            const float time = 0.5f;
            for (float t = 0f; t < time; t += Time.unscaledDeltaTime)
            {
                titleGroup.alpha = 1f - t / time;
                hudGroup.alpha = t / time;
                yield return null;
            }
            hudGroup.alpha = 1f;
            Destroy(titleCanvas.gameObject);
            titleCanvas = null;
            closing = false;
            if (paused != null)
                foreach (var item in paused)
                    if (item != null) item.enabled = true;
            paused = null;
        }

        /// <summary>
        /// Главная плашка слева во всю высоту, без обрезки; справа на лазурите — знак, OLAM, слоган,
        /// «Начать» и выбор языка. Если экран уже, колонка ложится на плашку через мягкий край.
        /// </summary>
        void BuildTitle()
        {
            titleCanvas = OlamUiKit.Overlay("OLAM_Title", 500, transform);
            titleGroup = titleCanvas.GetComponent<CanvasGroup>();
            var root = titleCanvas.transform;
            OlamUiKit.Fill(OlamUiKit.Stretch(root, "Back"), OlamUiKit.Lapis);

            float aspect = titleArt.width / (float)titleArt.height;
            var art = OlamUiKit.Rect(root, "Art", Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(1080f * aspect, 0f));
            var image = art.gameObject.AddComponent<UnityEngine.UI.RawImage>();
            image.texture = titleArt;
            image.raycastTarget = false;
            OlamUiKit.Ramp(OlamUiKit.Rect(art, "Fade", new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(300f, 0f)), OlamUiKit.Lapis, true);

            var column = OlamUiKit.Rect(root, "Column", new Vector2(1f, 0f), Vector2.one, new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(780f, 0f));
            OlamUiKit.Fill(column, OlamUiKit.Lapis);
            OlamUiKit.Ramp(OlamUiKit.Rect(column, "Edge", Vector2.zero, new Vector2(0f, 1f), new Vector2(1f, 0.5f),
                Vector2.zero, new Vector2(240f, 0f)), OlamUiKit.Lapis, true);

            var c = new Vector2(0.5f, 0.5f);
            var mark = OlamUiKit.Rect(column, "Mark", c, c, c, new Vector2(0f, 280f), new Vector2(170f, 170f));
            mark.gameObject.AddComponent<OlamMark>().color = OlamUiKit.Turquoise;
            var word = OlamUiKit.Label(column, "Wordmark", c, c, new Vector2(0f, 105f), new Vector2(720f, 160f),
                140f, OlamUiKit.Parchment);
            word.characterSpacing = 24f;
            word.text = "OLAM";
            slogan = OlamUiKit.Label(column, "Slogan", c, c, new Vector2(0f, -20f), new Vector2(620f, 100f),
                36f, OlamUiKit.TurquoiseSoft);

            var start = OlamUiKit.Button(column, "Start", new Vector2(0f, -160f), new Vector2(400f, 90f),
                OlamUiKit.Turquoise, OlamUiKit.Lapis, 40f, out startLabel);
            startLabel.fontStyle = FontStyles.Bold;
            start.onClick.AddListener(CloseTitle);

            languageLabels.Clear();
            for (int i = 0; i < Languages.Length; i++)
            {
                var (lang, code) = Languages[i];
                var button = OlamUiKit.Button(column, "Lang_" + code, new Vector2((i - 1) * 112f, -275f),
                    new Vector2(100f, 60f), Color.clear, OlamUiKit.Parchment, 30f, out var label);
                label.text = code;
                button.onClick.AddListener(() => SetLanguage(lang));
                languageLabels.Add((lang, label));
            }

            project = OlamUiKit.Label(column, "Project", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 56f),
                new Vector2(660f, 60f), 22f, OlamUiKit.Alpha(OlamUiKit.Parchment, 0.6f));
            Refresh();
        }

        void SetLanguage(Language lang)
        {
            if (Localization.Instance != null) Localization.Instance.SetLanguage(lang);
            Refresh();
        }

        // ---------- двор: подсказка и плашка пространства ----------

        void BuildHud()
        {
            var canvas = OlamUiKit.Overlay("OLAM_HubHud", 100, transform);
            hudGroup = canvas.GetComponent<CanvasGroup>();
            hudGroup.alpha = 0f;
            hudGroup.blocksRaycasts = false;
            var root = canvas.transform;
            OlamUiKit.Ramp(OlamUiKit.Rect(root, "TopShade", new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 190f)), OlamUiKit.Alpha(OlamUiKit.Lapis, 0.7f), false, true);
            var corner = new Vector2(0f, 1f);
            var mark = OlamUiKit.Rect(root, "Mark", corner, corner, corner, new Vector2(56f, -36f), new Vector2(64f, 64f));
            mark.gameObject.AddComponent<OlamMark>().color = OlamUiKit.Turquoise;
            var word = OlamUiKit.Label(root, "Wordmark", corner, corner, new Vector2(134f, -40f), new Vector2(240f, 56f),
                40f, OlamUiKit.Parchment, TextAlignmentOptions.Left);
            word.characterSpacing = 12f;
            word.text = "OLAM";
            var top = new Vector2(0.5f, 1f);
            choose = OlamUiKit.Label(root, "Choose", top, top, new Vector2(0f, -44f), new Vector2(1000f, 56f),
                38f, OlamUiKit.Parchment);

            var bottom = new Vector2(0.5f, 0f);
            var plate = OlamUiKit.Rect(root, "Plate", bottom, bottom, bottom, new Vector2(0f, 40f), new Vector2(1200f, 250f));
            plateGroup = plate.gameObject.AddComponent<CanvasGroup>();
            plateGroup.alpha = 0f;
            OlamUiKit.Fill(plate, OlamUiKit.Alpha(OlamUiKit.Lapis, 0.9f));
            OlamUiKit.Fill(OlamUiKit.Rect(plate, "Line", new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 4f)), OlamUiKit.Turquoise);
            var c = new Vector2(0.5f, 0.5f);
            plateKicker = OlamUiKit.Label(plate, "Kicker", c, c, new Vector2(0f, 84f), new Vector2(1120f, 40f),
                26f, OlamUiKit.Turquoise);
            plateKicker.fontStyle = FontStyles.UpperCase;
            plateKicker.characterSpacing = 6f;
            plateTitle = OlamUiKit.Label(plate, "Title", c, c, new Vector2(0f, 30f), new Vector2(1120f, 74f),
                62f, OlamUiKit.Parchment);
            plateQuestion = OlamUiKit.Label(plate, "Question", c, c, new Vector2(0f, -40f), new Vector2(1100f, 70f),
                30f, OlamUiKit.Alpha(OlamUiKit.Parchment, 0.9f));
            plateQuestion.fontStyle = FontStyles.Italic;
            plateHint = OlamUiKit.Label(plate, "Hint", c, c, new Vector2(0f, -96f), new Vector2(1100f, 36f),
                22f, OlamUiKit.Alpha(OlamUiKit.Turquoise, 0.85f));
        }

        void ShowPlate(SpaceInfo info)
        {
            plateKicker.text = OlamProgress.IsDone(info.sceneName) ? $"{info.kicker.Get()} · {DoneText.Get()}" : info.kicker.Get();
            plateTitle.text = info.title.Get();
            plateQuestion.text = info.question.Get();
        }

        void Refresh()
        {
            var current = Localization.Instance != null ? Localization.Instance.Current : Language.Ru;
            if (slogan != null) slogan.text = SloganText.Get(current);
            if (startLabel != null) startLabel.text = StartText.Get(current);
            if (project != null) project.text = ProjectText.Get(current);
            foreach (var (lang, label) in languageLabels)
                if (label != null)
                    label.color = lang == current ? OlamUiKit.Turquoise : OlamUiKit.Alpha(OlamUiKit.Parchment, 0.55f);
            if (choose != null) choose.text = ChooseText.Get(current);
            if (plateHint != null) plateHint.text = EnterText.Get(current);
            if (hovered != null) ShowPlate(hovered.info);
        }

        /// <summary>Обложка в арке тусклее, под курсором — в полную яркость.</summary>
        void Dim(Entry space, float value)
        {
            if (space.cover == null) return;
            block ??= new MaterialPropertyBlock();
            space.cover.GetPropertyBlock(block);
            block.SetColor(BaseColor, new Color(value, value, value, 1f));
            space.cover.SetPropertyBlock(block);
        }

        /// <summary>
        /// Для снимков в редакторе: титул (hover &lt; 0) или двор с плашкой пространства под курсором.
        /// Клики и порталы не трогает. Возвращает собранные холсты — их удаляет вызывающий,
        /// подсветку обложек снимает <see cref="ClearPreview"/>.
        /// </summary>
        public Canvas[] BuildPreview(int hover)
        {
            BuildHud();
            var hud = hudGroup.GetComponent<Canvas>();
            if (hover < 0 && titleArt != null)
            {
                BuildTitle();
                return new[] { titleCanvas, hud };
            }
            hudGroup.alpha = 1f;
            hovered = hover >= 0 && hover < spaces.Length ? spaces[hover] : null;
            foreach (var space in spaces)
                if (space != null) Dim(space, space == hovered ? 1f : coverDim);
            Refresh();
            if (hovered != null && hovered.info != null)
            {
                ShowPlate(hovered.info);
                plateGroup.alpha = 1f;
            }
            return new[] { hud };
        }

        public void ClearPreview()
        {
            foreach (var space in spaces)
                if (space?.cover != null) space.cover.SetPropertyBlock(null);
            hovered = null;
            titleCanvas = null;
        }
    }
}
