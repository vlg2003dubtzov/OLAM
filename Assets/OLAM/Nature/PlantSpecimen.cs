using UnityEngine;
using Olam.Core;

namespace Olam.Nature
{
    /// <summary>
    /// Растение. isTarget = false — для сравнительных образцов.
    /// </summary>
    [RequireComponent(typeof(OlamInteractable))]
    public class PlantSpecimen : MonoBehaviour
    {
        [SerializeField] EncyclopediaEntry entry;
        [SerializeField] bool isTarget = true;

        OlamInteractable interactable;

        public bool IsTarget => isTarget;

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
            if (entry == null) return;
            InfoPanel.Instance?.Show(entry);
            if (isTarget)
                Herbarium.Instance?.Register(entry);
        }
    }
}
