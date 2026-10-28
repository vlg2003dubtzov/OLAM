using UnityEngine;
using Olam.Core;

namespace Olam.History
{
    /// <summary>
    /// Переключение руины ↔ реконструкция.
    /// </summary>
    public class EraSwitcher : MonoBehaviour
    {
        [SerializeField] GameObject ruinsRoot;
        [SerializeField] GameObject reconstructionRoot;
        [SerializeField] bool showReconstruction;
        [SerializeField] OlamInteractable interactable;

        void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<OlamInteractable>();
            if (interactable != null)
                interactable.Selected += OnSelected;
            Apply();
        }

        void OnDestroy()
        {
            if (interactable != null)
                interactable.Selected -= OnSelected;
        }

        public void OnSelected()
        {
            showReconstruction = !showReconstruction;
            Apply();
        }

        void Apply()
        {
            if (ruinsRoot != null) ruinsRoot.SetActive(!showReconstruction);
            if (reconstructionRoot != null) reconstructionRoot.SetActive(showReconstruction);
        }
    }
}
