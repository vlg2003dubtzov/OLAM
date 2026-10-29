using UnityEngine;
using Olam.Core;

namespace Olam.Kitchen
{
    /// <summary>
    /// Полено с массой. Throw On Detach — только для Firewood.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(OlamInteractable))]
    public class Firewood : MonoBehaviour
    {
        [SerializeField] float massKg = 0.8f;
        [SerializeField] float energyPerKg = 18000f;

        bool used;

        public float MassKg => massKg;
        public float EnergyPerKg => energyPerKg;

        void Awake()
        {
            GetComponent<Rigidbody>().mass = massKg;
        }

        /// <summary>Полено сгорает один раз, даже если в топку его засчитали и по лучу, и по триггеру.</summary>
        public bool TryUse()
        {
            if (used) return false;
            used = true;
            return true;
        }
    }
}
