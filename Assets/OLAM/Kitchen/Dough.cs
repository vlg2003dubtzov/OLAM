using UnityEngine;
using Olam.Core;

namespace Olam.Kitchen
{
    /// <summary>
    /// Лепёшка: влажность, запекание, состояния Free / OnWall.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Dough : MonoBehaviour
    {
        public enum DoughState { Free, OnWall, Baked }

        [SerializeField] float moisture = 0.6f;
        [SerializeField] float bakeProgress;
        [SerializeField] float bakeTempThreshold = 180f;
        [SerializeField] float bakeSpeed = 0.08f;
        [SerializeField] DoughState state = DoughState.Free;
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] Color rawColor = new Color(0.92f, 0.85f, 0.7f);
        [SerializeField] Color bakedColor = new Color(0.72f, 0.48f, 0.22f);
        [SerializeField] string questId = "bake_bread";

        Rigidbody rb;
        OlamInteractable interactable;
        TandoorThermal wallThermal;

        public DoughState State => state;
        public float Moisture => moisture;
        public float BakeProgress => bakeProgress;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            interactable = GetComponent<OlamInteractable>();
            if (meshRenderer == null)
                meshRenderer = GetComponentInChildren<MeshRenderer>();

            if (interactable != null)
                interactable.Selected += OnSelected;
        }

        void OnDestroy()
        {
            if (interactable != null)
                interactable.Selected -= OnSelected;
        }

        public void OnSelected()
        {
            if (state == DoughState.OnWall)
                DetachFromWall();
        }

        public void Wet(float amount)
        {
            moisture = Mathf.Clamp01(moisture + amount);
        }

        public void StickToWall(Transform anchor, TandoorThermal thermal)
        {
            if (state != DoughState.Free) return;
            if (moisture < 0.2f) return;

            state = DoughState.OnWall;
            wallThermal = thermal;

            rb.isKinematic = true;
            rb.detectCollisions = false;

            transform.SetParent(anchor, true);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        void DetachFromWall()
        {
            state = DoughState.Free;
            wallThermal = null;
            transform.SetParent(null, true);
            rb.isKinematic = false;
            rb.detectCollisions = true;
        }

        void Update()
        {
            if (state != DoughState.OnWall || wallThermal == null) return;
            if (wallThermal.Temperature < bakeTempThreshold) return;

            bakeProgress = Mathf.Clamp01(bakeProgress + bakeSpeed * Time.deltaTime * wallThermal.NormalizedHeat);
            ApplyColor();

            if (bakeProgress >= 1f && state != DoughState.Baked)
            {
                state = DoughState.Baked;
                QuestManager.Instance?.Report(questId);
            }
        }

        void ApplyColor()
        {
            if (meshRenderer == null) return;
            meshRenderer.material.color = Color.Lerp(rawColor, bakedColor, bakeProgress);
        }
    }
}
