using UnityEngine;

namespace Olam.Nature
{
    /// <summary>
    /// Переключение дня/ночи (ambient + directional light).
    /// </summary>
    public class DayNightController : MonoBehaviour
    {
        [SerializeField] Light sun;
        [SerializeField] bool isNight = true;
        [SerializeField] Color dayAmbient = new Color(0.55f, 0.6f, 0.65f);
        [SerializeField] Color nightAmbient = new Color(0.05f, 0.07f, 0.12f);
        [SerializeField] float dayIntensity = 1.1f;
        [SerializeField] float nightIntensity = 0.05f;
        [SerializeField] Color daySky = new Color(0.45f, 0.65f, 0.9f);
        [SerializeField] Color nightSky = new Color(0.02f, 0.03f, 0.08f);

        public bool IsNight => isNight;

        void Start() => Apply();

        public void SetNight(bool night)
        {
            isNight = night;
            Apply();
        }

        public void Toggle() => SetNight(!isNight);

        void Apply()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = isNight ? nightAmbient : dayAmbient;

            if (sun != null)
            {
                sun.intensity = isNight ? nightIntensity : dayIntensity;
                sun.shadows = LightShadows.None;
                if (isNight)
                    sun.transform.rotation = Quaternion.Euler(10f, -30f, 0f);
                else
                    sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            Camera cam = Camera.main;
            if (cam != null)
                cam.backgroundColor = isNight ? nightSky : daySky;
        }
    }
}
