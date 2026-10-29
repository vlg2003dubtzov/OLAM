using Olam.Core;
using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Топка: полено отпускают над тандыром или бросают в устье — оно сгорает, печь греется.
    /// Триггер на всю полость тандыра, поэтому полено засчитывается, куда бы в устье ни попало.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FuelZone : MonoBehaviour, IKitchenTarget
    {
        [SerializeField] TandoorThermal thermal;
        [SerializeField] string questId = "fuel";

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var wood = other.GetComponentInParent<Firewood>();
            if (wood != null) Burn(wood);
        }

        public bool TryAccept(KitchenItem item)
        {
            if (item.ItemKind != KitchenItem.Kind.Firewood || !item.TryGetComponent<Firewood>(out var wood)) return false;
            Burn(wood);
            return true;
        }

        void Burn(Firewood wood)
        {
            if (thermal == null || !wood.TryUse()) return;
            thermal.AddFuel(wood.MassKg, wood.EnergyPerKg);
            QuestManager.Instance?.Report(questId);
            Destroy(wood.gameObject);
        }
    }
}
