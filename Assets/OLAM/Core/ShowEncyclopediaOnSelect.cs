using UnityEngine;

namespace Olam.Core
{
    /// <summary>
    /// Показывает карточку энциклопедии при выборе объекта.
    /// </summary>
    [RequireComponent(typeof(OlamInteractable))]
    public class ShowEncyclopediaOnSelect : MonoBehaviour
    {
        [SerializeField] EncyclopediaEntry entry;

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
            if (entry != null)
                InfoPanel.Instance?.Show(entry);
        }
    }
}
