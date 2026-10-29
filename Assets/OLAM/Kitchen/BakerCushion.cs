using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Подушка пекаря: на неё кладут тесто и ею прижимают его к горячей стенке, чтобы не обжечь руку.
    /// Для теста — цель («положить на подушку»), с тестом — предмет для стенки тандыра и миски.
    /// </summary>
    [RequireComponent(typeof(KitchenItem))]
    public class BakerCushion : MonoBehaviour, IKitchenTarget
    {
        [SerializeField] Transform seat;

        Collider ownCollider;

        public Transform Seat => seat != null ? seat : transform;
        public Dough Dough { get; private set; }

        void Awake() => ownCollider = GetComponent<Collider>();

        public bool TryAccept(KitchenItem item)
        {
            if (Dough != null || item.ItemKind != KitchenItem.Kind.Dough || !item.TryGetComponent<Dough>(out var dough))
                return false;
            if (dough.State != Dough.DoughState.Free || dough.BakeProgress > 0f) return false;
            dough.PutOnCushion(this);
            return true;
        }

        /// <summary>Тесто лежит на подушке: их коллайдеры друг друга не толкают.</summary>
        public void Hold(Dough dough, Collider doughCollider)
        {
            Dough = dough;
            if (ownCollider != null && doughCollider != null) Physics.IgnoreCollision(ownCollider, doughCollider, true);
        }

        public void Release(Collider doughCollider)
        {
            if (ownCollider != null && doughCollider != null) Physics.IgnoreCollision(ownCollider, doughCollider, false);
            Dough = null;
        }
    }
}
