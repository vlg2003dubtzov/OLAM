using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Приём дров в топку.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class FuelZone : MonoBehaviour
    {
        [SerializeField] TandoorThermal thermal;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var wood = other.GetComponentInParent<Firewood>();
            if (wood == null || thermal == null) return;

            thermal.AddFuel(wood.MassKg, wood.EnergyPerKg);
            Destroy(wood.gameObject);
        }
    }
}
