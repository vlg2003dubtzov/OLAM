using UnityEngine;

namespace Olam.Core
{
    /// <summary>
    /// Телепорт XR Origin / камеры к заданной точке при выборе.
    /// </summary>
    [RequireComponent(typeof(OlamInteractable))]
    public class TeleportToPoint : MonoBehaviour
    {
        [SerializeField] Transform destination;
        [SerializeField] Transform xrOrigin;
        [SerializeField] bool matchRotation;

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
            if (destination == null || xrOrigin == null) return;
            xrOrigin.position = destination.position;
            if (matchRotation)
                xrOrigin.rotation = destination.rotation;
        }
    }
}
