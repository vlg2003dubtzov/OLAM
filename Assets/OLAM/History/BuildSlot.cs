using UnityEngine;
using UnityEngine.Events;

namespace Olam.History
{
    public enum SlotState { Open, Filled }

    /// <summary>
    /// Гнездо для детали реконструкции.
    /// </summary>
    public class BuildSlot : MonoBehaviour
    {
        [SerializeField] string requiredPieceId;
        [SerializeField] SlotState state = SlotState.Open;
        [SerializeField] Transform snapPoint;
        [SerializeField] UnityEvent onFilled;

        public SlotState State => state;
        public string RequiredPieceId => requiredPieceId;

        public bool Fill(BuildPiece piece)
        {
            if (state != SlotState.Open || piece == null) return false;
            if (!string.IsNullOrEmpty(requiredPieceId) && piece.PieceId != requiredPieceId)
                return false;

            state = SlotState.Filled;
            var anchor = snapPoint != null ? snapPoint : transform;
            piece.SnapTo(anchor);
            onFilled?.Invoke();
            return true;
        }
    }
}
