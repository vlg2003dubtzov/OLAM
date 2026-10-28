using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Топка тандыра: дрова → теплоёмкость → температура, сажа, ускорение времени.
    /// </summary>
    public class TandoorThermal : MonoBehaviour
    {
        [SerializeField] float heatCapacity = 120f;
        [SerializeField] float ambientTemp = 25f;
        [SerializeField] float maxTemp = 400f;
        [SerializeField] float coolRate = 4f;
        [SerializeField] float sootPerFuel = 0.05f;
        [SerializeField] float timeScale = 8f;
        [SerializeField] float currentTemp = 25f;
        [SerializeField] float soot;
        [SerializeField] TandoorFuelVisual fuelVisual;
        [SerializeField] TandoorGauge gauge;

        public float Temperature => currentTemp;
        public float Soot => soot;
        public float NormalizedHeat => Mathf.InverseLerp(ambientTemp, maxTemp, currentTemp);

        public void AddFuel(float massKg, float energyPerKg = 18000f)
        {
            float energy = massKg * energyPerKg;
            float delta = energy / Mathf.Max(1f, heatCapacity);
            currentTemp = Mathf.Min(maxTemp, currentTemp + delta);
            soot = Mathf.Clamp01(soot + massKg * sootPerFuel);
            fuelVisual?.OnFuelAdded(massKg, NormalizedHeat);
            gauge?.Refresh(this);
        }

        void Update()
        {
            float dt = Time.deltaTime * timeScale;
            currentTemp = Mathf.MoveTowards(currentTemp, ambientTemp, coolRate * dt);
            fuelVisual?.SetHeat(NormalizedHeat);
            gauge?.Refresh(this);
        }
    }
}
