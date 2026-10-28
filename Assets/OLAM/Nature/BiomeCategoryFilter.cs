using System.Collections.Generic;
using UnityEngine;
using Olam.Core;

namespace Olam.Nature
{
    /// <summary>
    /// Фильтр категорий биома (наручный планшет / голоменю): подсветка видов.
    /// </summary>
    public class BiomeCategoryFilter : MonoBehaviour
    {
        public static BiomeCategoryFilter Instance { get; private set; }

        [SerializeField] SpeciesCategory current = SpeciesCategory.TreesShrubs;
        readonly List<ScannableOrganism> organisms = new List<ScannableOrganism>();

        public SpeciesCategory Current => current;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start() => RefreshList();

        public void RefreshList()
        {
            organisms.Clear();
            organisms.AddRange(FindObjectsByType<ScannableOrganism>());
            Apply();
        }

        public void SetCategory(SpeciesCategory category)
        {
            current = category;
            Apply();
        }

        public void OnSelectTrees() => SetCategory(SpeciesCategory.TreesShrubs);
        public void OnSelectHerbs() => SetCategory(SpeciesCategory.HerbsFlowers);
        public void OnSelectFauna() => SetCategory(SpeciesCategory.Fauna);

        void Apply()
        {
            for (int i = 0; i < organisms.Count; i++)
            {
                var o = organisms[i];
                if (o == null) continue;
                o.SetCategoryHighlight(o.Category == current);
            }
        }
    }
}
