using Olam.Core;
using Olam.UI;
using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Топка тандыра: дрова прогорают за пару секунд и греют глину, а остывает печь медленно — глина копит тепло.
    /// Время ускорено (timeScale), чтобы урок шёл минуты, а не час. На readyTemp печь готова — цель квеста.
    /// </summary>
    public class TandoorThermal : MonoBehaviour
    {
        [SerializeField] float heatCapacity = 120f;
        [SerializeField] float ambientTemp = 25f;
        [SerializeField] float maxTemp = 400f;
        [SerializeField] float coolRate = 0.4f;
        [SerializeField] float burnRate = 60f;
        [SerializeField] float readyTemp = 250f;
        [SerializeField] string readyQuestId = "heat";
        [SerializeField] string[] statusGoals = { "heat", "bake_bread" };   // у этих шагов панель показывает температуру
        [SerializeField] float sootPerFuel = 0.05f;
        [SerializeField] float timeScale = 8f;
        [SerializeField] float currentTemp = 25f;
        [SerializeField] float soot;
        [SerializeField] TandoorFuelVisual fuelVisual;
        [SerializeField] TandoorGauge gauge;

        float burning;   // тепло дров, которые ещё горят, °C
        bool readyReported;
        float statusTimer;

        public float Temperature => currentTemp;
        public float Soot => soot;
        public float ReadyTemp => readyTemp;
        public bool Burning => burning > 0f;
        public float NormalizedHeat => Mathf.InverseLerp(ambientTemp, maxTemp, currentTemp);

        public void AddFuel(float massKg, float energyPerKg = 18000f)
        {
            burning += massKg * energyPerKg / Mathf.Max(1f, heatCapacity);
            soot = Mathf.Clamp01(soot + massKg * sootPerFuel);
            fuelVisual?.OnFuelAdded(massKg, NormalizedHeat);
        }

        void Update()
        {
            if (burning > 0f)
            {
                float add = Mathf.Min(burning, burnRate * Time.deltaTime);
                burning -= add;
                currentTemp = Mathf.Min(maxTemp, currentTemp + add);
            }
            else
            {
                currentTemp = Mathf.MoveTowards(currentTemp, ambientTemp, coolRate * Time.deltaTime * timeScale);
            }
            if (!readyReported && currentTemp >= readyTemp)
            {
                readyReported = true;
                QuestManager.Instance?.Report(readyQuestId);
            }
            // пока горят дрова, угли пылают ярко; потом светятся по жару стенок
            fuelVisual?.SetHeat(Burning ? Mathf.Max(NormalizedHeat, 0.8f) : NormalizedHeat);
            gauge?.Refresh(this);
            statusTimer -= Time.deltaTime;
            if (statusTimer > 0f) return;
            statusTimer = 0.25f;
            foreach (var goal in statusGoals)
                OlamMissionPanel.SetStatus(goal, $"{currentTemp:0} °C");
        }
    }
}
