using System;
using UnityEngine;

namespace Olam.Core
{
    /// <summary>
    /// Синглтон локализации. Подписчики обновляют UI через событие Changed.
    /// </summary>
    public class Localization : MonoBehaviour
    {
        public static Localization Instance { get; private set; }

        [SerializeField] Language current = Language.Ru;

        public Language Current => current;

        public event Action Changed;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // только сам дубликат: на том же объекте сцены бывают QuestManager и другие системы
                Destroy(this);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetLanguage(Language language)
        {
            if (current == language) return;
            current = language;
            Changed?.Invoke();
        }

        public void Cycle()
        {
            var next = (Language)(((int)current + 1) % 3);
            SetLanguage(next);
        }
    }
}
