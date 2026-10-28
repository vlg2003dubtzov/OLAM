using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Olam.Core
{
    /// <summary>
    /// Локальный интерактабл (без XRI): ЛКМ в Editor.
    /// Когда XR Interaction Toolkit станет совместим с Unity 6000.6 — обернём его сюда.
    /// </summary>
    public class OlamInteractable : MonoBehaviour
    {
        [SerializeField] UnityEvent onSelected;
        [SerializeField] bool grabbable;
        [SerializeField] bool throwOnDetach;

        public event Action Selected;
        public event Action Deselected;

        public bool Grabbable => grabbable;
        public bool ThrowOnDetach => throwOnDetach;

        Camera cam;
        bool holding;
        float holdDistance;
        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            cam = Camera.main;
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            if (mouse.leftButton.wasPressedThisFrame)
            {
                var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                if (Physics.Raycast(ray, out var hit, 50f) && hit.collider.transform.IsChildOf(transform))
                {
                    NotifySelected();
                    if (grabbable && rb != null)
                    {
                        holding = true;
                        holdDistance = hit.distance;
                        rb.isKinematic = true;
                    }
                }
            }

            if (holding && mouse.leftButton.isPressed)
            {
                var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
                transform.position = ray.GetPoint(holdDistance);
            }

            if (holding && mouse.leftButton.wasReleasedThisFrame)
            {
                holding = false;
                if (rb != null)
                {
                    rb.isKinematic = false;
                    if (throwOnDetach)
                        rb.linearVelocity = cam.transform.forward * 3f;
                }
                Deselected?.Invoke();
            }
        }

        public void NotifySelected()
        {
            onSelected?.Invoke();
            Selected?.Invoke();
        }

        public void OnSelect() => NotifySelected();
    }
}
