using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Olam.Core
{
    /// <summary>
    /// World Space панель энциклопедии (синглтон).
    /// </summary>
    public class InfoPanel : MonoBehaviour
    {
        public static InfoPanel Instance { get; private set; }

        [SerializeField] Canvas canvas;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text scientificText;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Image image;
        [SerializeField] AudioSource narrationSource;

        EncyclopediaEntry current;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (canvas != null)
                canvas.enabled = false;
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

        public void Show(EncyclopediaEntry entry)
        {
            current = entry;
            if (canvas != null) canvas.enabled = true;
            Refresh();

            if (narrationSource != null && entry != null && entry.Narration != null)
            {
                narrationSource.clip = entry.Narration;
                narrationSource.Play();
            }
        }

        public void Hide()
        {
            current = null;
            if (canvas != null) canvas.enabled = false;
            if (narrationSource != null) narrationSource.Stop();
        }

        void Refresh()
        {
            if (current == null) return;

            if (titleText != null) titleText.text = current.Title.Get();
            if (scientificText != null) scientificText.text = current.ScientificName;
            if (bodyText != null) bodyText.text = current.Description.Get();
            if (image != null)
            {
                image.sprite = current.Image;
                image.enabled = current.Image != null;
            }
        }
    }
}
