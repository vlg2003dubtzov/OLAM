using UnityEngine;
using UnityEngine.Events;
using Olam.Core;

namespace Olam.Nature
{
    /// <summary>
    /// Объект для Эко-Сканера: скан → голограмма + гербарий (не «сорвать»).
    /// </summary>
    [RequireComponent(typeof(OlamInteractable))]
    public class ScannableOrganism : MonoBehaviour
    {
        [SerializeField] EncyclopediaEntry entry;
        [SerializeField] bool registerHerbarium = true;
        [SerializeField] UnityEvent onInteractAction;
        [SerializeField] GameObject highlightBeacon;
        [SerializeField] AudioClip scanSound;

        OlamInteractable interactable;
        bool scanned;
        AudioSource audioSource;

        public EncyclopediaEntry Entry => entry;
        public SpeciesCategory Category => entry != null ? entry.Category : SpeciesCategory.HerbsFlowers;
        public bool Scanned => scanned;

        void Awake()
        {
            interactable = GetComponent<OlamInteractable>();
            interactable.Selected += OnSelected;
            audioSource = GetComponent<AudioSource>();
            if (highlightBeacon != null) highlightBeacon.SetActive(false);
        }

        void OnDestroy()
        {
            if (interactable != null)
                interactable.Selected -= OnSelected;
        }

        public void OnSelected()
        {
            EcoScanner.Instance?.Scan(this);
        }

        public void ApplyScan()
        {
            if (entry == null) return;

            if (!scanned)
            {
                scanned = true;
                if (registerHerbarium)
                    Herbarium.Instance?.Register(entry);
                QuestManager.Instance?.Report("eco_scan", 1);
                FieldNotebook.Instance?.MarkFound(entry);
            }

            if (scanSound != null)
            {
                if (audioSource != null) audioSource.PlayOneShot(scanSound);
                else AudioSource.PlayClipAtPoint(scanSound, transform.position);
            }

            HologramCard.Instance?.Show(entry, this);
            InfoPanel.Instance?.Show(entry);
        }

        public void TriggerInteractAction()
        {
            onInteractAction?.Invoke();
        }

        public void SetCategoryHighlight(bool on)
        {
            if (highlightBeacon != null)
                highlightBeacon.SetActive(on);
            else if (on)
            {
                // простой fallback: лёгкий emissive tint через цвет
                var r = GetComponentInChildren<Renderer>();
                if (r != null && r.material != null && r.material.HasProperty("_BaseColor"))
                {
                    var c = r.material.GetColor("_BaseColor");
                    r.material.SetColor("_BaseColor", Color.Lerp(c, Color.yellow, 0.35f));
                }
            }
        }
    }
}
