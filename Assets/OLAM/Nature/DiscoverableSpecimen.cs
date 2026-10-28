using UnityEngine;
using Olam.Core;

namespace Olam.Nature
{
    /// <summary>
    /// Растение/объект для полевого дневника: осмотр → энциклопедия + отметка.
    /// </summary>
    [RequireComponent(typeof(OlamInteractable))]
    public class DiscoverableSpecimen : MonoBehaviour
    {
        [SerializeField] EncyclopediaEntry entry;
        [SerializeField] string notebookId;
        [SerializeField] bool registerHerbarium = true;
        [SerializeField] bool requireFlashlight;

        OlamInteractable interactable;
        bool done;

        void Awake()
        {
            interactable = GetComponent<OlamInteractable>();
            interactable.Selected += OnSelected;
            if (string.IsNullOrEmpty(notebookId) && entry != null)
                notebookId = entry.Id;
        }

        void OnDestroy()
        {
            if (interactable != null)
                interactable.Selected -= OnSelected;
        }

        public void OnSelected()
        {
            if (done) return;

            if (requireFlashlight)
            {
                bool lit = false;
                for (int i = 0; i < Flashlight.All.Count; i++)
                {
                    if (Flashlight.All[i].Illuminates(transform.position))
                    {
                        lit = true;
                        break;
                    }
                }
                if (!lit) return;
            }

            done = true;
            if (entry != null)
            {
                InfoPanel.Instance?.Show(entry);
                if (registerHerbarium)
                    Herbarium.Instance?.Register(entry);
            }
            FieldNotebook.Instance?.MarkFound(notebookId);
        }
    }
}
