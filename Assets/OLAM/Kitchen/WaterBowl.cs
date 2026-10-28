using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Миска для смачивания лепёшки.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WaterBowl : MonoBehaviour
    {
        [SerializeField] float wetAmount = 0.25f;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            var dough = other.GetComponentInParent<Dough>();
            dough?.Wet(wetAmount);
        }
    }
}
