using UnityEngine;

namespace Olam.Core
{
    /// <summary>
    /// Карточка энциклопедии (растение, артефакт, понятие).
    /// </summary>
    [CreateAssetMenu(menuName = "OLAM/Encyclopedia Entry", fileName = "Entry_")]
    public class EncyclopediaEntry : ScriptableObject
    {
        [SerializeField] string id;
        [SerializeField] LText title;
        [SerializeField] string scientificName;
        [SerializeField] LText description;
        [SerializeField] Sprite image;
        [SerializeField] AudioClip narration;

        public string Id => id;
        public LText Title => title;
        public string ScientificName => scientificName;
        public LText Description => description;
        public Sprite Image => image;
        public AudioClip Narration => narration;
    }
}
