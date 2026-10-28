using System.Collections.Generic;
using UnityEngine;
using Olam.Core;

namespace Olam.Nature
{
    /// <summary>
    /// Синглтон гербария: Register(entry).
    /// </summary>
    public class Herbarium : MonoBehaviour
    {
        public static Herbarium Instance { get; private set; }

        readonly List<EncyclopediaEntry> collected = new List<EncyclopediaEntry>();

        public IReadOnlyList<EncyclopediaEntry> Collected => collected;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Register(EncyclopediaEntry entry)
        {
            if (entry == null || collected.Contains(entry)) return;
            collected.Add(entry);
            QuestManager.Instance?.Report("herbarium", 1);
        }
    }
}
