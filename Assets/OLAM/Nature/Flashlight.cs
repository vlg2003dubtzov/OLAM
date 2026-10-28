using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Olam.Nature
{
    /// <summary>
    /// Фонарик: глобальный список All и проверка Illuminates(point).
    /// В Editor следует за камерой / лучом мыши. F — вкл/выкл.
    /// </summary>
    public class Flashlight : MonoBehaviour
    {
        public static readonly List<Flashlight> All = new List<Flashlight>();

        [SerializeField] Light spot;
        [SerializeField] float range = 8f;
        [SerializeField] float coneAngle = 35f;
        [SerializeField] bool isOn = true;
        [SerializeField] bool followCamera = true;
        [SerializeField] bool aimWithMouse = true;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Awake()
        {
            if (spot == null) spot = GetComponentInChildren<Light>();
            Apply();
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                SetOn(!isOn);

            if (!followCamera) return;
            var cam = Camera.main;
            if (cam == null) return;

            transform.position = cam.transform.position;

            if (aimWithMouse && Mouse.current != null)
            {
                var ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
                transform.rotation = Quaternion.LookRotation(ray.direction, Vector3.up);
            }
            else
            {
                transform.rotation = cam.transform.rotation;
            }
        }

        public void SetOn(bool on)
        {
            isOn = on;
            Apply();
        }

        void Apply()
        {
            if (spot != null)
            {
                spot.enabled = isOn;
                spot.type = LightType.Spot;
                spot.range = range;
                spot.spotAngle = coneAngle * 2f;
                spot.intensity = 4f;
                spot.shadows = LightShadows.None;
            }
        }

        public bool Illuminates(Vector3 point)
        {
            if (!isOn || spot == null || !spot.enabled) return false;

            Vector3 origin = spot.transform.position;
            Vector3 to = point - origin;
            float dist = to.magnitude;
            if (dist > range || dist < 0.01f) return false;

            float angle = Vector3.Angle(spot.transform.forward, to);
            if (angle > coneAngle) return false;

            // без теней: луч не блокируется
            return true;
        }
    }
}
