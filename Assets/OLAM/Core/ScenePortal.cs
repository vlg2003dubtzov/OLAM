using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace Olam.Core
{
    /// <summary>
    /// Портал между обучающими сценами OLAM (ЛКМ / будущий XR select).
    /// </summary>
    [RequireComponent(typeof(OlamInteractable))]
    public class ScenePortal : MonoBehaviour
    {
        [SerializeField] string sceneName;
        [SerializeField] LText label = new LText("Переход", "O‘tish", "Enter");
        [SerializeField] TMP_Text labelText;
        [SerializeField] float loadDelay = 0.15f;

        OlamInteractable interactable;
        bool loading;

        void Awake()
        {
            interactable = GetComponent<OlamInteractable>();
            interactable.Selected += OnSelected;
        }

        void OnEnable()
        {
            if (Localization.Instance != null)
                Localization.Instance.Changed += RefreshLabel;
            RefreshLabel();
        }

        void OnDisable()
        {
            if (Localization.Instance != null)
                Localization.Instance.Changed -= RefreshLabel;
        }

        void OnDestroy()
        {
            if (interactable != null)
                interactable.Selected -= OnSelected;
        }

        public void OnSelected()
        {
            if (loading || string.IsNullOrEmpty(sceneName)) return;
            loading = true;
            Invoke(nameof(DoLoad), loadDelay);
        }

        void DoLoad()
        {
            SceneManager.LoadScene(sceneName);
        }

        void RefreshLabel()
        {
            if (labelText != null)
                labelText.text = label.Get();
        }
    }
}
