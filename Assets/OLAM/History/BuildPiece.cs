using UnityEngine;
using Olam.Core;

namespace Olam.History
{
    /// <summary>
    /// Деталь макета: захват + Rigidbody.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(OlamInteractable))]
    public class BuildPiece : MonoBehaviour
    {
        [SerializeField] string pieceId;
        [SerializeField] float slotSearchRadius = 0.35f;

        Rigidbody rb;
        OlamInteractable interactable;
        bool locked;

        public string PieceId => pieceId;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            interactable = GetComponent<OlamInteractable>();
            interactable.Deselected += OnDeselected;
        }

        void OnDestroy()
        {
            if (interactable != null)
                interactable.Deselected -= OnDeselected;
        }

        public void OnDeselected()
        {
            if (locked) return;
            TrySnapNearby();
        }

        void TrySnapNearby()
        {
            var cols = Physics.OverlapSphere(transform.position, slotSearchRadius);
            foreach (var col in cols)
            {
                var slot = col.GetComponentInParent<BuildSlot>();
                if (slot != null && slot.Fill(this))
                    return;
            }
        }

        public void SnapTo(Transform anchor)
        {
            locked = true;
            if (interactable != null) interactable.enabled = false;
            rb.isKinematic = true;
            transform.SetParent(anchor, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
