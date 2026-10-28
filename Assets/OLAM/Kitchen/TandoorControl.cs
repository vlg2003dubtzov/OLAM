using UnityEngine;
using Olam.Core;

namespace Olam.Kitchen
{
    /// <summary>
    /// Крышка / поддувало тандыра.
    /// </summary>
    public class TandoorControl : MonoBehaviour
    {
        public enum ControlKind { Lid, Damper }

        [SerializeField] ControlKind kind = ControlKind.Lid;
        [SerializeField] Transform movingPart;
        [SerializeField] Vector3 closedLocalEuler;
        [SerializeField] Vector3 openLocalEuler = new Vector3(-70f, 0f, 0f);
        [SerializeField] bool isOpen;
        [SerializeField] OlamInteractable interactable;
        [SerializeField] TandoorThermal thermal;

        void Awake()
        {
            if (interactable == null)
                interactable = GetComponent<OlamInteractable>();
            if (interactable != null)
                interactable.Selected += OnSelected;
            ApplyPose();
        }

        void OnDestroy()
        {
            if (interactable != null)
                interactable.Selected -= OnSelected;
        }

        public void OnSelected()
        {
            isOpen = !isOpen;
            ApplyPose();
            _ = kind;
            _ = thermal;
        }

        void ApplyPose()
        {
            if (movingPart == null) return;
            movingPart.localEulerAngles = isOpen ? openLocalEuler : closedLocalEuler;
        }
    }
}
