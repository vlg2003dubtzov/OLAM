using UnityEngine;
using Olam.Core;

namespace Olam.Nature
{
    /// <summary>
    /// Эко-сканер натуралиста: вместо «корзинки для срыва» — фото/био-скан.
    /// </summary>
    public class EcoScanner : MonoBehaviour
    {
        public static EcoScanner Instance { get; private set; }

        [SerializeField] AudioClip defaultScanBeep;
        [SerializeField] float cooldown = 0.35f;

        float lastScan = -999f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Scan(ScannableOrganism organism)
        {
            if (organism == null || organism.Entry == null) return;
            if (Time.unscaledTime - lastScan < cooldown) return;
            lastScan = Time.unscaledTime;

            // педагогика: Красная книга — нельзя рвать
            if (organism.Entry.Status == ConservationStatus.RedBook
                || organism.Entry.Status == ConservationStatus.Endemic)
            {
                Debug.Log($"OLAM EcoScanner: {organism.Entry.Id} — только фото-скан (охраняемый вид).");
            }

            if (organism.GetComponent<AudioSource>() == null && defaultScanBeep != null)
                AudioSource.PlayClipAtPoint(defaultScanBeep, organism.transform.position);

            organism.ApplyScan();
        }
    }
}
