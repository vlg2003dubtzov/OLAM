using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Olam.Core
{
    /// <summary>
    /// Кнопки переключения языка (ru / uz / en).
    /// </summary>
    public class LanguageSwitcher : MonoBehaviour
    {
        [SerializeField] Button ruButton;
        [SerializeField] Button uzButton;
        [SerializeField] Button enButton;
        [SerializeField] TMP_Text label;

        [SerializeField] LText labelText = new LText("Язык", "Til", "Language");

        void Awake()
        {
            if (ruButton != null) ruButton.onClick.AddListener(() => OnSelect(Language.Ru));
            if (uzButton != null) uzButton.onClick.AddListener(() => OnSelect(Language.Uz));
            if (enButton != null) enButton.onClick.AddListener(() => OnSelect(Language.En));
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

        public void OnSelect(Language language)
        {
            if (Localization.Instance != null)
                Localization.Instance.SetLanguage(language);
            else
                Refresh();
        }

        void Refresh()
        {
            if (label != null)
                label.text = labelText.Get();
        }
    }
}
