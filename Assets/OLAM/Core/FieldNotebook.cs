using System.Text;
using UnityEngine;
using TMPro;

namespace Olam.Core
{
    /// <summary>
    /// Полевой дневник ученика: вопрос урока + чеклист находок (флора/фауна/артефакты).
    /// </summary>
    public class FieldNotebook : MonoBehaviour
    {
        public static FieldNotebook Instance { get; private set; }

        [System.Serializable]
        public class Item
        {
            public string id;
            public LText title;
            public EncyclopediaEntry entry;
            [HideInInspector] public bool found;
        }

        [SerializeField] LText lessonQuestion = new LText(
            "Как узнать снежного барса по следу и что растёт рядом?",
            "Qor qoplonini izidan qanday bilamiz va yonida nima o‘sadi?",
            "How do we recognize a snow leopard by its track, and what grows nearby?");

        [SerializeField] Item[] items;
        [SerializeField] TMP_Text questionText;
        [SerializeField] TMP_Text listText;
        [SerializeField] Canvas canvas;

        void Awake()
        {
            Instance = this;
            if (canvas != null) canvas.enabled = true;
        }

        void OnEnable()
        {
            if (Localization.Instance != null)
                Localization.Instance.Changed += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            if (Localization.Instance != null)
                Localization.Instance.Changed -= Refresh;
        }

        public void MarkFound(string id)
        {
            if (items == null) return;
            foreach (var item in items)
            {
                if (item == null || item.found || item.id != id) continue;
                item.found = true;
                if (item.entry != null)
                    InfoPanel.Instance?.Show(item.entry);
                Refresh();
                CheckComplete();
                return;
            }
        }

        public void MarkFound(EncyclopediaEntry entry)
        {
            if (entry == null) return;
            MarkFound(entry.Id);
        }

        void CheckComplete()
        {
            foreach (var item in items)
                if (item != null && !item.found) return;
            QuestManager.Instance?.Report("field_notebook");
        }

        void Refresh()
        {
            if (questionText != null)
                questionText.text = lessonQuestion.Get();

            if (listText == null || items == null) return;
            var sb = new StringBuilder();
            int done = 0;
            foreach (var item in items)
            {
                if (item == null) continue;
                if (item.found) done++;
                sb.Append(item.found ? "✓ " : "○ ");
                sb.AppendLine(item.title.Get());
            }
            sb.AppendLine();
            sb.Append($"{done}/{items.Length}");
            listText.text = sb.ToString();
        }
    }
}
