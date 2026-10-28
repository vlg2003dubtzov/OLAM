using UnityEngine;
using Olam.Core;

namespace Olam.Nature
{
    /// <summary>
    /// Кнопка фильтра категорий на планшете биома.
    /// </summary>
    [RequireComponent(typeof(OlamInteractable))]
    public class CategoryFilterButton : MonoBehaviour
    {
        [SerializeField] SpeciesCategory category;

        OlamInteractable interactable;

        void Awake()
        {
            interactable = GetComponent<OlamInteractable>();
            interactable.Selected += OnSelected;
        }

        void OnDestroy()
        {
            if (interactable != null)
                interactable.Selected -= OnSelected;
        }

        public void OnSelected()
        {
            BiomeCategoryFilter.Instance?.SetCategory(category);
        }
    }
}
