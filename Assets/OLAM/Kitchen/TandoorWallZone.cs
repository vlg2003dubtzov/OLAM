using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Триггер внутренней стенки: лепёшка «прилипает» в позе OnWall.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TandoorWallZone : MonoBehaviour
    {
        [SerializeField] TandoorThermal thermal;
        [SerializeField] Transform attachAnchor;

        void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var dough = other.GetComponentInParent<Dough>();
            if (dough == null || dough.State != Dough.DoughState.Free) return;

            var pose = attachAnchor != null ? attachAnchor : transform;
            dough.StickToWall(pose, thermal);
        }
    }
}
