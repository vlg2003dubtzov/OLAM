using UnityEngine;
using TMPro;
using Olam.Core;

namespace Olam.Kitchen
{
    /// <summary>
    /// Табличка температуры / сажи.
    /// </summary>
    public class TandoorGauge : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] LText title = new LText("Тандыр", "Tandir", "Tandoor");

        TandoorThermal cached;

        void OnEnable()
        {
            if (Localization.Instance != null)
                Localization.Instance.Changed += OnLang;
        }

        void OnDisable()
        {
            if (Localization.Instance != null)
                Localization.Instance.Changed -= OnLang;
        }

        void OnLang() => Refresh(cached);

        public void Refresh(TandoorThermal thermal)
        {
            cached = thermal;
            if (label == null || thermal == null) return;
            label.text = $"{title.Get()}\n{thermal.Temperature:0}°C\nsoot {thermal.Soot:0%}";
        }
    }
}
