using UnityEngine;
using UnityEngine.Events;
using Olam.Core;

namespace Olam.History
{
    /// <summary>
    /// Этапы сборки макета.
    /// </summary>
    public class ReconstructionManager : MonoBehaviour
    {
        [SerializeField] BuildSlot[] slots;
        [SerializeField] string questId = "rebuild_site";
        [SerializeField] UnityEvent onStageComplete;

        bool reported;

        void Update()
        {
            if (reported || slots == null) return;
            foreach (var slot in slots)
            {
                if (slot == null || slot.State != SlotState.Filled) return;
            }

            reported = true;
            onStageComplete?.Invoke();
            QuestManager.Instance?.Report(questId);
        }
    }
}
