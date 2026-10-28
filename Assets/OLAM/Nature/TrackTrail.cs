using UnityEngine;
using UnityEngine.Events;
using Olam.Core;

namespace Olam.Nature
{
    /// <summary>
    /// Цепочка следов. При strictOrder следующий след активен только после предыдущего.
    /// OnTrackFound → AnimalSpawner.
    /// </summary>
    public class TrackTrail : MonoBehaviour
    {
        [SerializeField] AnimalTrack[] tracks;
        [SerializeField] bool strictOrder = true;
        [SerializeField] string questId = "find_tracks";
        [SerializeField] UnityEvent onTrailCompleted;
        [SerializeField] AnimalSpawner animalSpawner;

        int nextIndex;
        bool completed;

        void Awake()
        {
            if (tracks == null) return;
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i] == null) continue;
                int captured = i;
                tracks[i].BindFound(() => OnTrackFound(captured));
                // В строгом порядке прячем «будущие» следы до своей очереди
                if (strictOrder)
                    tracks[i].gameObject.SetActive(i == 0);
            }
        }

        public void OnTrackFound(int index)
        {
            if (completed) return;

            if (strictOrder && index != nextIndex)
                return;

            nextIndex = Mathf.Max(nextIndex, index + 1);
            QuestManager.Instance?.Report(questId, 1);

            if (strictOrder && nextIndex < tracks.Length && tracks[nextIndex] != null)
                tracks[nextIndex].gameObject.SetActive(true);

            if (nextIndex >= tracks.Length)
            {
                completed = true;
                onTrailCompleted?.Invoke();
                animalSpawner?.Spawn();
            }
        }
    }
}
