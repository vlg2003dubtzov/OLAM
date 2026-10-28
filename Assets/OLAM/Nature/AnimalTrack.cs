using System;
using UnityEngine;
using UnityEngine.Events;
using Olam.Core;

namespace Olam.Nature
{
    /// <summary>
    /// След животного. Found после выбора игроком (лучше при свете фонарика).
    /// </summary>
    [RequireComponent(typeof(OlamInteractable))]
    public class AnimalTrack : MonoBehaviour
    {
        [SerializeField] string trackId;
        [SerializeField] EncyclopediaEntry entry;
        [SerializeField] bool found;
        [SerializeField] bool requireFlashlight = true;
        [SerializeField] UnityEvent onFound;
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] Color foundColor = new Color(0.9f, 0.75f, 0.2f);

        OlamInteractable interactable;
        Action foundCallback;

        public string TrackId => trackId;
        public bool Found => found;

        void Awake()
        {
            interactable = GetComponent<OlamInteractable>();
            interactable.Selected += OnSelected;
            if (meshRenderer == null)
                meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        void OnDestroy()
        {
            if (interactable != null)
                interactable.Selected -= OnSelected;
        }

        public void BindFound(Action callback) => foundCallback = callback;

        public void OnSelected()
        {
            if (found) return;

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

            found = true;
            if (meshRenderer != null)
                meshRenderer.material.color = foundColor;
            if (entry != null) InfoPanel.Instance?.Show(entry);
            onFound?.Invoke();
            foundCallback?.Invoke();
        }
    }
}
