using System;
using Olam.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Olam.Kitchen
{
    /// <summary>
    /// Предмет кухни, который берут в руку (мышь: зажать ЛКМ и тащить). Когда его отпускают, луч из камеры
    /// через курсор ищет цель — IKitchenTarget: топку, миску, стенку тандыра, подушку, тесто, дастархан.
    /// Не принял никто — тесто, хлеб и инструменты возвращаются на место, дрова просто падают.
    /// В VR вместо луча будет касание руки, цели останутся те же.
    /// </summary>
    [RequireComponent(typeof(OlamInteractable), typeof(Rigidbody))]
    public class KitchenItem : MonoBehaviour
    {
        public enum Kind { Firewood, Dough, Cushion, Chekich }

        [SerializeField] Kind kind;
        [SerializeField] bool returnHome = true;

        OlamInteractable interactable;
        Rigidbody body;
        Transform homeParent;
        Vector3 homePosition;
        Quaternion homeRotation;

        public Kind ItemKind => kind;
        public event Action PickedUp;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            interactable = GetComponent<OlamInteractable>();
            interactable.Selected += OnPicked;
            interactable.Deselected += OnDropped;
            homeParent = transform.parent;
            homePosition = transform.position;
            homeRotation = transform.rotation;
        }

        void OnDestroy()
        {
            if (interactable == null) return;
            interactable.Selected -= OnPicked;
            interactable.Deselected -= OnDropped;
        }

        void OnPicked() => PickedUp?.Invoke();

        void OnDropped()
        {
            if (DropOnTarget()) return;
            if (returnHome) ReturnHome();
        }

        bool DropOnTarget()
        {
            var cam = Camera.main;
            var mouse = Mouse.current;
            if (cam == null || mouse == null) return false;
            // цели — триггеры на слое Ignore Raycast: луч по всем слоям и вместе с триггерами
            var hits = Physics.RaycastAll(cam.ScreenPointToRay(mouse.position.ReadValue()), 40f, ~0,
                QueryTriggerInteraction.Collide);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                foreach (var target in hit.collider.GetComponentsInParent<IKitchenTarget>())
                    if (target.TryAccept(this)) return true;
            }
            return false;
        }

        public void ReturnHome() => Place(homeParent, homePosition, homeRotation, false);

        /// <summary>Положить предмет в позу; kinematic — держится там, где положили, не падает.</summary>
        public void Place(Transform parent, Vector3 position, Quaternion rotation, bool kinematic)
        {
            transform.SetParent(parent, true);
            transform.SetPositionAndRotation(position, rotation);
            if (kinematic)
            {
                body.isKinematic = true;
                return;
            }
            body.isKinematic = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
    }
}
