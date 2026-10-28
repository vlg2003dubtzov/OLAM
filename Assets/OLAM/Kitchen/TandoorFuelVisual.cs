using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Визуал углей / жара.
    /// </summary>
    public class TandoorFuelVisual : MonoBehaviour
    {
        [SerializeField] MeshRenderer embers;
        [SerializeField] Light fireLight;
        [SerializeField] ParticleSystem sparks;
        [SerializeField] Color cool = new Color(0.15f, 0.05f, 0.02f);
        [SerializeField] Color hot = new Color(1f, 0.35f, 0.05f);
        [SerializeField] float maxLight = 2.5f;

        public void OnFuelAdded(float massKg, float heat01)
        {
            if (sparks != null) sparks.Play();
            SetHeat(heat01);
        }

        public void SetHeat(float heat01)
        {
            heat01 = Mathf.Clamp01(heat01);
            if (embers != null)
                embers.material.color = Color.Lerp(cool, hot, heat01);
            if (fireLight != null)
            {
                fireLight.enabled = heat01 > 0.05f;
                fireLight.intensity = heat01 * maxLight;
                fireLight.color = Color.Lerp(cool, hot, heat01);
            }
        }
    }
}
