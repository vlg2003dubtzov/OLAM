using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Olam.Core;

namespace Olam.Nature
{
    /// <summary>
    /// Голографическая карточка эко-сканера (World Space, крупный шрифт, ≤40 слов).
    /// </summary>
    public class HologramCard : MonoBehaviour
    {
        public static HologramCard Instance { get; private set; }

        [SerializeField] Canvas canvas;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] TMP_Text powerText;
        [SerializeField] TMP_Text interactText;
        [SerializeField] Button interactButton;
        [SerializeField] AudioSource audioSource;
        [SerializeField] float followDistance = 1.6f;
        [SerializeField] float followHeight = -0.15f;

        EncyclopediaEntry current;
        ScannableOrganism target;
        Camera cam;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (canvas != null) canvas.enabled = false;
            if (interactButton != null)
                interactButton.onClick.AddListener(OnInteractPressed);
        }

        void OnEnable()
        {
            if (Localization.Instance != null)
                Localization.Instance.Changed += Refresh;
        }

        void OnDisable()
        {
            if (Localization.Instance != null)
                Localization.Instance.Changed -= Refresh;
        }

        void LateUpdate()
        {
            if (canvas == null || !canvas.enabled) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            var pos = cam.transform.position
                      + cam.transform.forward * followDistance
                      + Vector3.up * followHeight;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }

        public void Show(EncyclopediaEntry entry, ScannableOrganism organism)
        {
            current = entry;
            target = organism;
            if (canvas != null) canvas.enabled = true;
            Refresh();

            if (audioSource != null && entry != null && entry.Narration != null)
            {
                audioSource.clip = entry.Narration;
                audioSource.Play();
            }
        }

        public void Hide()
        {
            current = null;
            target = null;
            if (canvas != null) canvas.enabled = false;
            if (audioSource != null) audioSource.Stop();
        }

        void Refresh()
        {
            if (current == null) return;
            if (titleText != null)
                titleText.text = $"{current.Title.Get()}\n<size=70%>{current.ScientificName}</size>";
            if (statusText != null)
                statusText.text = current.StatusLabel();
            if (bodyText != null)
                bodyText.text = current.Description.Get();
            if (powerText != null)
            {
                var p = current.PowerFact.Get();
                powerText.text = string.IsNullOrEmpty(p) ? "" : $"⚡ {p}";
                powerText.gameObject.SetActive(!string.IsNullOrEmpty(p));
            }
            if (interactText != null)
            {
                var h = current.InteractHint.Get();
                interactText.text = h;
                bool show = !string.IsNullOrEmpty(h) || !string.IsNullOrEmpty(current.InteractActionId);
                if (interactButton != null) interactButton.gameObject.SetActive(show);
                else interactText.gameObject.SetActive(show);
            }
        }

        void OnInteractPressed()
        {
            target?.TriggerInteractAction();
        }
    }
}
