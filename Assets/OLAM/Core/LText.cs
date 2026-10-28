using System;
using UnityEngine;

namespace Olam.Core
{
    /// <summary>
    /// Трёхъязычный текст: русский / узбекский / английский.
    /// </summary>
    [Serializable]
    public struct LText
    {
        [SerializeField] public string ru;
        [SerializeField] public string uz;
        [SerializeField] public string en;

        public LText(string ru, string uz, string en)
        {
            this.ru = ru;
            this.uz = uz;
            this.en = en;
        }

        public string Get()
        {
            var lang = Localization.Instance != null
                ? Localization.Instance.Current
                : Language.Ru;

            return lang switch
            {
                Language.Uz => string.IsNullOrEmpty(uz) ? ru : uz,
                Language.En => string.IsNullOrEmpty(en) ? ru : en,
                _ => ru
            };
        }

        public string Get(Language language)
        {
            return language switch
            {
                Language.Uz => string.IsNullOrEmpty(uz) ? ru : uz,
                Language.En => string.IsNullOrEmpty(en) ? ru : en,
                _ => ru
            };
        }
    }

    public enum Language
    {
        Ru = 0,
        Uz = 1,
        En = 2
    }
}
