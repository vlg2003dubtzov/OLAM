using UnityEngine;
using TMPro;
using Olam.Core;

namespace Olam.Kitchen
{
    /// <summary>
    /// Карточки физики тандыра (теплоёмкость, сажа, влажность).
    /// </summary>
    public class TandoorMilestones : MonoBehaviour
    {
        [System.Serializable]
        public class Card
        {
            public string id;
            public LText title;
            public LText body;
            public float unlockTemp;
            [HideInInspector] public bool shown;
        }

        [SerializeField] Card[] cards;
        [SerializeField] TandoorThermal thermal;
        [SerializeField] TMP_Text cardTitle;
        [SerializeField] TMP_Text cardBody;
        [SerializeField] GameObject cardRoot;

        void OnEnable()
        {
            if (Localization.Instance != null)
                Localization.Instance.Changed += RefreshVisible;
            if (cardRoot != null) cardRoot.SetActive(false);
        }

        void OnDisable()
        {
            if (Localization.Instance != null)
                Localization.Instance.Changed -= RefreshVisible;
        }

        void Update()
        {
            if (thermal == null) return;
            foreach (var card in cards)
            {
                if (card.shown) continue;
                if (thermal.Temperature < card.unlockTemp) continue;
                Show(card);
                card.shown = true;
                break;
            }
        }

        void Show(Card card)
        {
            // своей таблички в мире нет — карточка уходит на панель миссии
            if (cardRoot == null && cardTitle == null)
            {
                Olam.UI.OlamMissionPanel.ShowCard(card.title, card.body);
                return;
            }
            if (cardRoot != null) cardRoot.SetActive(true);
            if (cardTitle != null) cardTitle.text = card.title.Get();
            if (cardBody != null) cardBody.text = card.body.Get();
        }

        void RefreshVisible()
        {
            if (cardRoot == null && cardTitle == null) return;   // панель миссии переводит свою карточку сама
            foreach (var card in cards)
            {
                if (!card.shown) continue;
                Show(card);
                return;
            }
        }
    }
}
