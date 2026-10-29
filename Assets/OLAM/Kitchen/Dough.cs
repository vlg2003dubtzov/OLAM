using Olam.Core;
using Olam.UI;
using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Лепёшка от теста до дастархана: наколоть чекичем → смочить → на подушку → на стенку тандыра → печётся,
    /// пока стенки горячие → румяная: снять → на дастархан. Передержал — подгорела, на столе новое тесто.
    /// Цвет — по степени выпечки, узор чекича — дочерний значок stampMark. Шаги — цели QuestManager.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(KitchenItem))]
    public class Dough : MonoBehaviour, IKitchenTarget
    {
        public enum DoughState { Free, OnCushion, OnWall, Served }

        [SerializeField] float moisture = 0.1f;
        [SerializeField] float wetEnough = 0.2f;
        [SerializeField] float bakeTempThreshold = 180f;
        [SerializeField] float bakeSpeed = 0.08f;
        [SerializeField] float burnAt = 1.7f;
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] GameObject stampMark;
        [SerializeField] Color rawColor = new(0.92f, 0.85f, 0.7f);
        [SerializeField] Color bakedColor = new(0.76f, 0.5f, 0.24f);
        [SerializeField] Color burntColor = new(0.2f, 0.11f, 0.05f);

        DoughState state;
        float startMoisture, bakeProgress, coldTime;
        bool stamped, coldShown;
        KitchenItem item;
        OlamInteractable interactable;
        Collider ownCollider;
        TandoorThermal wallThermal;
        BakerCushion cushion;

        public DoughState State => state;
        public bool IsWet => moisture >= wetEnough;
        public float BakeProgress => bakeProgress;
        public bool Golden => bakeProgress >= 1f && bakeProgress < burnAt;

        void Awake()
        {
            item = GetComponent<KitchenItem>();
            interactable = GetComponent<OlamInteractable>();
            ownCollider = GetComponent<Collider>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            startMoisture = moisture;
            item.PickedUp += OnPickedUp;
            ResetDough();
        }

        void OnDestroy()
        {
            if (item != null) item.PickedUp -= OnPickedUp;
        }

        /// <summary>Сняли рукой с подушки — тесто снова на столе; взяли румяную со стенки — лепёшка снята.</summary>
        void OnPickedUp()
        {
            if (state == DoughState.OnCushion) LeaveCushion();
            else if (state == DoughState.OnWall) TakeFromWall();
        }

        /// <summary>Чекич отпустили над тестом.</summary>
        public bool TryAccept(KitchenItem other)
        {
            if (other.ItemKind != KitchenItem.Kind.Chekich || !Stamp()) return false;
            other.ReturnHome();
            return true;
        }

        bool Stamp()
        {
            if (stamped || bakeProgress > 0f || state == DoughState.OnWall || state == DoughState.Served) return false;
            stamped = true;
            if (stampMark != null) stampMark.SetActive(true);
            QuestManager.Instance?.Report("stamp");
            OlamMissionPanel.ShowCard(KitchenText.StampTitle, KitchenText.StampBody);
            return true;
        }

        public void Wet(float amount)
        {
            if (bakeProgress > 0f) return;
            bool before = IsWet;
            moisture = Mathf.Clamp01(moisture + amount);
            if (!before && IsWet) QuestManager.Instance?.Report("wet");
        }

        public void PutOnCushion(BakerCushion onto)
        {
            cushion = onto;
            state = DoughState.OnCushion;
            item.Place(onto.Seat, onto.Seat.position, onto.Seat.rotation, true);
            onto.Hold(this, ownCollider);
        }

        void LeaveCushion()
        {
            if (cushion != null) cushion.Release(ownCollider);
            cushion = null;
            state = DoughState.Free;
            transform.SetParent(null, true);
        }

        public void StickToWall(Transform anchor, TandoorThermal thermal)
        {
            if (cushion != null) LeaveCushion();
            state = DoughState.OnWall;
            wallThermal = thermal;
            coldTime = 0f;
            coldShown = false;
            item.Place(anchor, anchor.position, anchor.rotation, true);
            interactable.enabled = false;   // сырую со стенки не снять: включится, когда зарумянится
            QuestManager.Instance?.Report("stick");
            KitchenCameraRig.Instance?.LookIntoTandoor(true);
        }

        void TakeFromWall()
        {
            state = DoughState.Free;
            wallThermal = null;
            transform.SetParent(null, true);
            QuestManager.Instance?.Report("take_out");
            KitchenCameraRig.Instance?.LookIntoTandoor(false);
        }

        public void Serve(Transform seat)
        {
            state = DoughState.Served;
            item.Place(seat, seat.position, seat.rotation, true);
            interactable.enabled = false;
            QuestManager.Instance?.Report("serve");
        }

        void Update()
        {
            if (state != DoughState.OnWall || wallThermal == null) return;
            if (wallThermal.Temperature < bakeTempThreshold)
            {
                coldTime += Time.deltaTime;
                if (!coldShown && coldTime > 4f)
                {
                    coldShown = true;
                    OlamMissionPanel.ShowCard(KitchenText.ColdTitle, KitchenText.ColdBody);
                }
                return;
            }
            coldTime = 0f;
            float before = bakeProgress;
            bakeProgress += bakeSpeed * Time.deltaTime * Mathf.Max(0.35f, wallThermal.NormalizedHeat);
            ApplyColor();
            if (before < 1f && bakeProgress >= 1f)
            {
                QuestManager.Instance?.Report("bake_bread");
                interactable.enabled = true;
                OlamMissionPanel.ShowCard(KitchenText.GoldenTitle, KitchenText.GoldenBody);
            }
            if (bakeProgress >= burnAt) Burn();
        }

        /// <summary>Подгоревшую убираем: на столе снова сырое тесто, шаги можно повторить.</summary>
        void Burn()
        {
            OlamMissionPanel.ShowCard(KitchenText.BurntTitle, KitchenText.BurntBody);
            ResetDough();
            item.ReturnHome();
            KitchenCameraRig.Instance?.LookIntoTandoor(false);
        }

        void ResetDough()
        {
            state = DoughState.Free;
            wallThermal = null;
            bakeProgress = 0f;
            moisture = startMoisture;
            stamped = false;
            if (stampMark != null) stampMark.SetActive(false);
            interactable.enabled = true;
            ApplyColor();
        }

        void ApplyColor()
        {
            if (meshRenderer == null) return;
            meshRenderer.material.color = bakeProgress <= 1f
                ? Color.Lerp(rawColor, bakedColor, bakeProgress)
                : Color.Lerp(bakedColor, burntColor, Mathf.InverseLerp(1f, burnAt, bakeProgress));
        }
    }
}
