using UnityEngine;
using Olam.Core;

namespace Olam.Nature
{
    /// <summary>
    /// Появление зверя после цепочки следов: объект, звук, квест.
    /// </summary>
    public class AnimalSpawner : MonoBehaviour
    {
        [SerializeField] GameObject animalPrefab;
        [SerializeField] Transform spawnPoint;
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip appearSound;
        [SerializeField] EncyclopediaEntry entry;
        [SerializeField] string questId = "spot_animal";
        [SerializeField] float appearScaleFrom = 0.1f;
        [SerializeField] float appearDuration = 1.2f;

        GameObject instance;
        bool spawned;

        public void Spawn()
        {
            if (spawned) return;
            spawned = true;

            var point = spawnPoint != null ? spawnPoint : transform;
            if (animalPrefab != null)
                instance = Instantiate(animalPrefab, point.position, point.rotation);
            else
            {
                instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                instance.name = "Animal_Placeholder";
                instance.transform.SetPositionAndRotation(point.position, point.rotation);
                instance.transform.localScale = new Vector3(0.6f, 0.7f, 1.1f);
                var mr = instance.GetComponent<MeshRenderer>();
                if (mr != null) mr.material.color = new Color(0.45f, 0.32f, 0.18f);
            }

            if (audioSource != null && appearSound != null)
            {
                audioSource.clip = appearSound;
                audioSource.Play();
            }

            QuestManager.Instance?.Report(questId);
            if (entry != null)
            {
                InfoPanel.Instance?.Show(entry);
                FieldNotebook.Instance?.MarkFound(entry);
            }

            StartCoroutine(AppearTween(instance.transform));
        }

        System.Collections.IEnumerator AppearTween(Transform t)
        {
            float elapsed = 0f;
            Vector3 target = t.localScale;
            t.localScale = target * appearScaleFrom;
            while (elapsed < appearDuration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, elapsed / appearDuration);
                t.localScale = Vector3.Lerp(target * appearScaleFrom, target, k);
                yield return null;
            }
            t.localScale = target;
        }
    }
}
