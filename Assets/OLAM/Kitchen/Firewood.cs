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

        public float MassKg => massKg;
        public float EnergyPerKg => energyPerKg;

        void Awake()
        {
            GetComponent<Rigidbody>().mass = massKg;
        }
    }
}
