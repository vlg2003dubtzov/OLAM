using UnityEngine;
using TMPro;
using Olam.Core;

namespace Olam.Kitchen
{
    /// <summary>
    /// Градусник тандыра: температура стенок и готовность печи словами.
    /// </summary>
    public class TandoorGauge : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] LText title = new LText("Тандыр", "Tandir", "Tandoor");
        [SerializeField] LText cold = new LText("холодный", "sovuq", "cold");
        [SerializeField] LText heating = new LText("греется", "qizimoqda", "heating up");
        [SerializeField] LText ready = new LText("готов", "tayyor", "ready");

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
            var word = thermal.Temperature >= thermal.ReadyTemp ? ready
                : thermal.Burning || thermal.Temperature > 60f ? heating : cold;
            label.text = $"{title.Get()}\n{thermal.Temperature:0} °C\n{word.Get()}";
        }
    }
}
