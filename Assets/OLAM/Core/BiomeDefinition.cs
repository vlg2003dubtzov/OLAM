using UnityEngine;

namespace Olam.Core
{
    /// <summary>
    /// Биом «Купола Природы»: портал + сцена + ambient.
    /// </summary>
    [CreateAssetMenu(menuName = "OLAM/Biome Definition", fileName = "Biome_")]
    public class BiomeDefinition : ScriptableObject
    {
        [SerializeField] BiomeId id;
        [SerializeField] LText title;
        [SerializeField] LText blurb;
        [SerializeField] string sceneName;
        [SerializeField] Color portalColor = Color.white;
        [SerializeField] AudioClip ambientLoop;
        [SerializeField] EncyclopediaEntry[] featuredSpecies;

        public BiomeId Id => id;
        public LText Title => title;
        public LText Blurb => blurb;
        public string SceneName => sceneName;
        public Color PortalColor => portalColor;
        public AudioClip AmbientLoop => ambientLoop;
        public EncyclopediaEntry[] FeaturedSpecies => featuredSpecies;
    }
}
