using System;
using System.Collections;
using System.Collections.Generic;
using Olam.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Olam.UI
{
    /// <summary>
    /// Панель миссии поверх экрана: название, вопрос урока и шаги из QuestManager (сделано · сейчас · потом)
    /// с подсказкой к текущему шагу; карточки урока внизу; «Во двор» (и Esc); финальная карточка, когда
    /// все шаги сделаны. Тексты задаёт сборщик сцены, шаги и их порядок — цели QuestManager.
    /// </summary>
    public class OlamMissionPanel : MonoBehaviour
    {
        [Serializable]
        public class Hint
        {
            public string goalId;
            public LText text;
        }

        [SerializeField] string spaceScene;
        [SerializeField] LText title;
        [SerializeField] LText question;
        [SerializeField] Hint[] hints = Array.Empty<Hint>();
        [SerializeField] LText doneTitle;
        [SerializeField] LText doneBody;
        [SerializeField] string hubScene = "OlamHub";
        [SerializeField] float cardTime = 9f;

        static readonly LText MissionText = new("Миссия", "Topshiriq", "Mission");
        static readonly LText HubText = new("Во двор", "Hovliga", "To the courtyard");
        static readonly LText AgainText = new("Ещё раз", "Yana bir bor", "Once more");
        static OlamMissionPanel instance;

        readonly List<(QuestManager.QuestGoal goal, TMP_Text line)> lines = new();
        readonly Dictionary<string, string> status = new();
        RectTransform panel;
        TMP_Text kicker, titleText, questionText, hintText, cardTitle, cardBody, hubLabel, doneTitleText, doneBodyText,
            doneHubLabel, doneAgainLabel;
        CanvasGroup cardGroup, doneGroup;
        LText shownCardTitle, shownCardBody;
        float cardLeft;
        bool finished;

        /// <summary>Карточка урока внизу экрана; держится cardTime секунд, следующая её сменяет.</summary>
        public static void ShowCard(LText cardTitleText, LText cardBodyText)
        {
            if (instance != null) instance.Card(cardTitleText, cardBodyText);
        }

        /// <summary>Короткое значение рядом с шагом, например температура печи.</summary>
        public static void SetStatus(string goalId, string text)
        {
            if (instance == null || (instance.status.TryGetValue(goalId, out var old) && old == text)) return;
            instance.status[goalId] = text;
            instance.RefreshSteps();
        }

        void Awake()
        {
            instance = this;
            OlamUiKit.EnsureEventSystem();
            Build();
        }

        IEnumerator Start()
        {
            // QuestManager и Localization — синглтоны из своих Awake: подписываемся, когда все проснулись
            yield return null;
            if (Localization.Instance != null) Localization.Instance.Changed += Refresh;
            var quests = QuestManager.Instance;
            if (quests != null)
            {
                quests.onGoalProgress += OnGoalProgress;
                quests.onAllCompletedEvent += Finish;
                lines.Clear();
                BuildSteps(quests.Goals);
            }
            Refresh();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
            if (Localization.Instance != null) Localization.Instance.Changed -= Refresh;
            if (QuestManager.Instance == null) return;
            QuestManager.Instance.onGoalProgress -= OnGoalProgress;
            QuestManager.Instance.onAllCompletedEvent -= Finish;
        }

        void Update()
        {
            if (cardLeft > 0f) cardLeft -= Time.unscaledDeltaTime;
            cardGroup.alpha = Mathf.MoveTowards(cardGroup.alpha, cardLeft > 0f ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            if (finished) doneGroup.alpha = Mathf.MoveTowards(doneGroup.alpha, 1f, Time.unscaledDeltaTime * 2f);
            var keys = Keyboard.current;
            if (keys != null && keys.escapeKey.wasPressedThisFrame) ToHub();
        }

        void OnGoalProgress(QuestManager.QuestGoal goal) => RefreshSteps();

        void Finish()
        {
            finished = true;
            doneGroup.blocksRaycasts = true;
            OlamProgress.MarkDone(spaceScene);
            RefreshSteps();
        }

        void Card(LText cardTitleText, LText cardBodyText)
        {
            shownCardTitle = cardTitleText;
            shownCardBody = cardBodyText;
            cardTitle.text = cardTitleText.Get();
            cardBody.text = cardBodyText.Get();
            cardLeft = cardTime;
        }

        void ToHub() => SceneManager.LoadScene(hubScene);

        // ---------- сборка ----------

        void Build()
        {
            var canvas = OlamUiKit.Overlay("OLAM_MissionPanel", 200, transform);
            var root = canvas.transform;

            // слева сверху — карточка миссии со списком шагов (высота — по числу шагов)
            var corner = new Vector2(0f, 1f);
            panel = OlamUiKit.Rect(root, "Mission", corner, corner, corner, new Vector2(32f, -32f), new Vector2(560f, 300f));
            OlamUiKit.Fill(panel, OlamUiKit.Alpha(OlamUiKit.Lapis, 0.88f));
            OlamUiKit.Fill(OlamUiKit.Rect(panel, "Stripe", Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(6f, 0f)), OlamUiKit.Turquoise);
            kicker = OlamUiKit.Label(panel, "Kicker", corner, corner, new Vector2(30f, -22f), new Vector2(500f, 30f),
                22f, OlamUiKit.Turquoise, TextAlignmentOptions.TopLeft);
            kicker.fontStyle = FontStyles.UpperCase;
            kicker.characterSpacing = 6f;
            titleText = OlamUiKit.Label(panel, "Title", corner, corner, new Vector2(30f, -52f), new Vector2(510f, 52f),
                40f, OlamUiKit.Parchment, TextAlignmentOptions.TopLeft);
            questionText = OlamUiKit.Label(panel, "Question", corner, corner, new Vector2(30f, -104f), new Vector2(500f, 64f),
                22f, OlamUiKit.Alpha(OlamUiKit.Parchment, 0.85f), TextAlignmentOptions.TopLeft);
            questionText.fontStyle = FontStyles.Italic;
            hintText = OlamUiKit.Label(panel, "Hint", corner, corner, new Vector2(30f, -180f), new Vector2(500f, 60f),
                21f, OlamUiKit.TurquoiseSoft, TextAlignmentOptions.TopLeft);
            hintText.fontStyle = FontStyles.Italic;

            // справа сверху — «Во двор»
            var back = OlamUiKit.Button(root, "ToHub", Vector2.zero, new Vector2(240f, 64f),
                OlamUiKit.Alpha(OlamUiKit.Lapis, 0.88f), OlamUiKit.Parchment, 28f, out hubLabel);
            var backRect = (RectTransform)back.transform;
            backRect.anchorMin = backRect.anchorMax = backRect.pivot = Vector2.one;
            backRect.anchoredPosition = new Vector2(-32f, -32f);
            back.onClick.AddListener(ToHub);

            // внизу — карточка урока
            var bottom = new Vector2(0.5f, 0f);
            var card = OlamUiKit.Rect(root, "Card", bottom, bottom, bottom, new Vector2(0f, 36f), new Vector2(1120f, 180f));
            cardGroup = card.gameObject.AddComponent<CanvasGroup>();
            cardGroup.alpha = 0f;
            cardGroup.blocksRaycasts = false;
            OlamUiKit.Fill(card, OlamUiKit.Alpha(OlamUiKit.Lapis, 0.9f));
            OlamUiKit.Fill(OlamUiKit.Rect(card, "Line", new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 4f)), OlamUiKit.Turquoise);
            var c = new Vector2(0.5f, 0.5f);
            cardTitle = OlamUiKit.Label(card, "Title", c, c, new Vector2(0f, 44f), new Vector2(1060f, 50f),
                36f, OlamUiKit.Parchment);
            cardBody = OlamUiKit.Label(card, "Body", c, c, new Vector2(0f, -26f), new Vector2(1040f, 90f),
                26f, OlamUiKit.Alpha(OlamUiKit.Parchment, 0.9f));

            // по центру — финал миссии
            var done = OlamUiKit.Stretch(root, "Done");
            doneGroup = done.gameObject.AddComponent<CanvasGroup>();
            doneGroup.alpha = 0f;
            doneGroup.blocksRaycasts = false;
            OlamUiKit.Fill(done, OlamUiKit.Alpha(OlamUiKit.Lapis, 0.55f));
            var box = OlamUiKit.Rect(done, "Box", c, c, c, new Vector2(0f, 40f), new Vector2(920f, 440f));
            OlamUiKit.Fill(box, OlamUiKit.Lapis);
            var mark = OlamUiKit.Rect(box, "Mark", c, c, c, new Vector2(0f, 160f), new Vector2(86f, 86f));
            mark.gameObject.AddComponent<OlamMark>().color = OlamUiKit.Turquoise;
            doneTitleText = OlamUiKit.Label(box, "Title", c, c, new Vector2(0f, 82f), new Vector2(860f, 64f),
                50f, OlamUiKit.Parchment);
            doneBodyText = OlamUiKit.Label(box, "Body", c, c, new Vector2(0f, -30f), new Vector2(820f, 150f),
                26f, OlamUiKit.Alpha(OlamUiKit.Parchment, 0.9f));
            var hub = OlamUiKit.Button(box, "Hub", new Vector2(-150f, -160f), new Vector2(260f, 72f),
                OlamUiKit.Turquoise, OlamUiKit.Lapis, 30f, out doneHubLabel);
            doneHubLabel.fontStyle = FontStyles.Bold;
            hub.onClick.AddListener(ToHub);
            var again = OlamUiKit.Button(box, "Again", new Vector2(150f, -160f), new Vector2(260f, 72f),
                OlamUiKit.Alpha(OlamUiKit.Parchment, 0.12f), OlamUiKit.Parchment, 30f, out doneAgainLabel);
            again.onClick.AddListener(() => SceneManager.LoadScene(gameObject.scene.name));
        }

        void BuildSteps(IReadOnlyList<QuestManager.QuestGoal> goals)
        {
            var corner = new Vector2(0f, 1f);
            const float top = -176f, step = 36f;
            for (int i = 0; i < goals.Count; i++)
            {
                var line = OlamUiKit.Label(panel, "Step_" + goals[i].id, corner, corner, new Vector2(30f, top - i * step),
                    new Vector2(510f, 34f), 23f, OlamUiKit.Parchment, TextAlignmentOptions.TopLeft);
                line.textWrappingMode = TextWrappingModes.NoWrap;
                lines.Add((goals[i], line));
            }
            float stepsEnd = top - goals.Count * step;
            hintText.rectTransform.anchoredPosition = new Vector2(30f, stepsEnd - 8f);
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, -stepsEnd + 84f);
        }

        // ---------- тексты ----------

        void Refresh()
        {
            kicker.text = MissionText.Get();
            titleText.text = title.Get();
            questionText.text = question.Get();
            hubLabel.text = "← " + HubText.Get();
            doneTitleText.text = doneTitle.Get();
            doneBodyText.text = doneBody.Get();
            doneHubLabel.text = HubText.Get();
            doneAgainLabel.text = AgainText.Get();
            if (cardLeft > 0f)
            {
                cardTitle.text = shownCardTitle.Get();
                cardBody.text = shownCardBody.Get();
            }
            RefreshSteps();
        }

        void RefreshSteps()
        {
            bool current = false;
            string hint = "";
            foreach (var (goal, line) in lines)
            {
                string count = goal.required > 1 ? $"  {goal.current}/{goal.required}" : "";
                if (goal.done)
                {
                    line.text = $"●  {goal.title.Get()}{count}";
                    line.color = OlamUiKit.Alpha(OlamUiKit.TurquoiseSoft, 0.5f);
                    continue;
                }
                if (current)
                {
                    line.text = $"○  {goal.title.Get()}{count}";
                    line.color = OlamUiKit.Alpha(OlamUiKit.Parchment, 0.5f);
                    continue;
                }
                current = true;
                status.TryGetValue(goal.id, out var extra);
                string tail = string.IsNullOrEmpty(extra) ? "" : $"  <color=#4FC3C0>{extra}</color>";
                line.text = $"►  {goal.title.Get()}{count}{tail}";
                line.color = OlamUiKit.Parchment;
                foreach (var h in hints)
                    if (h != null && h.goalId == goal.id) hint = h.text.Get();
            }
            if (hintText != null) hintText.text = finished ? "" : hint;
        }

        /// <summary>
        /// Для снимка в редакторе: панель с шагами (doneSteps первых — сделаны) и карточкой; ничего не подписывает.
        /// Возвращает холст — его удаляет вызывающий.
        /// </summary>
        public Canvas BuildPreview(IReadOnlyList<QuestManager.QuestGoal> goals, int doneSteps, LText previewCardTitle,
            LText previewCardBody)
        {
            Build();
            lines.Clear();
            BuildSteps(goals);
            for (int i = 0; i < goals.Count; i++)
                goals[i].done = i < doneSteps;
            Card(previewCardTitle, previewCardBody);
            cardGroup.alpha = 1f;
            Refresh();
            foreach (var goal in goals) goal.done = false;
            return panel.GetComponentInParent<Canvas>();
        }
    }
}
