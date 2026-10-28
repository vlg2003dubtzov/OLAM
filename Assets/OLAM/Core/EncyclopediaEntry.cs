using UnityEngine;

namespace Olam.Core
{
    public enum ConservationStatus
    {
        Common = 0,
        RedBook = 1,
        Endemic = 2
    }

    public enum SpeciesCategory
    {
        TreesShrubs = 0,
        HerbsFlowers = 1,
        Fauna = 2
    }

    public enum BiomeId
    {
        Desert = 0,
        Adyr = 1,
        Mountains = 2,
        Highlands = 3,
        Tugai = 4,
        Kitchen = 10,
        History = 11
    }

    /// <summary>
    /// Карточка энциклопедии / голограммы эко-сканера (≤40 слов в description).
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

        [Header("VR / Эко-сканер")]
        [SerializeField] BiomeId biome;
        [SerializeField] SpeciesCategory category = SpeciesCategory.HerbsFlowers;
        [SerializeField] ConservationStatus status = ConservationStatus.Common;
        [SerializeField] LText powerFact;
        [SerializeField] LText interactHint;
        [SerializeField] string interactActionId;

        public string Id => id;
        public LText Title => title;
        public string ScientificName => scientificName;
        public LText Description => description;
        public Sprite Image => image;
        public AudioClip Narration => narration;
        public BiomeId Biome => biome;
        public SpeciesCategory Category => category;
        public ConservationStatus Status => status;
        public LText PowerFact => powerFact;
        public LText InteractHint => interactHint;
        public string InteractActionId => interactActionId;

        public string StatusLabelRu => status switch
        {
            ConservationStatus.RedBook => "🔴 Красная книга",
            ConservationStatus.Endemic => "🌍 Эндемик",
            _ => "🟢 Обычный вид"
        };

        public string StatusLabel()
        {
            var lang = Localization.Instance != null ? Localization.Instance.Current : Language.Ru;
            return lang switch
            {
                Language.Uz => status switch
                {
                    ConservationStatus.RedBook => "🔴 Qizil kitob",
                    ConservationStatus.Endemic => "🌍 Endemik",
                    _ => "🟢 Oddiy tur"
                },
                Language.En => status switch
                {
                    ConservationStatus.RedBook => "🔴 Red List",
                    ConservationStatus.Endemic => "🌍 Endemic",
                    _ => "🟢 Common"
                },
                _ => StatusLabelRu
            };
        }
    }
}
