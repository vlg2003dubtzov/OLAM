using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Миска воды: тесто (или подушку с тестом) отпускают над ней — тесто смачивается и возвращается на стол.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WaterBowl : MonoBehaviour, IKitchenTarget
    {
        [SerializeField] float wetAmount = 0.25f;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var dough = other.GetComponentInParent<Dough>();
            if (dough != null) dough.Wet(wetAmount);
        }

        public bool TryAccept(KitchenItem item)
        {
            Dough dough = null;
            if (item.ItemKind == KitchenItem.Kind.Dough) item.TryGetComponent(out dough);
            else if (item.ItemKind == KitchenItem.Kind.Cushion && item.TryGetComponent<BakerCushion>(out var cushion))
                dough = cushion.Dough;
            if (dough == null || dough.BakeProgress > 0f) return false;
            dough.Wet(wetAmount);
            item.ReturnHome();
            return true;
        }
    }
}
