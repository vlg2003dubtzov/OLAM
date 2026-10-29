using Olam.Core;
using UnityEngine;

namespace Olam.Kitchen
{
    /// <summary>
    /// Камера кухни: общий план и «заглянуть в тандыр» — сверху в устье, где на стенке печётся лепёшка.
    /// В VR игрок наклонится сам; здесь камера переезжает по клику на тандыр и сама, когда лепёшку прилепили и сняли.
    /// </summary>
    public class KitchenCameraRig : MonoBehaviour
    {
        public static KitchenCameraRig Instance { get; private set; }

        [SerializeField] Vector3 overviewPosition = new(0f, 1.65f, -2f);
        [SerializeField] Vector3 overviewTarget = new(0.35f, 0.62f, 1.15f);
        [SerializeField] Vector3 peekPosition = new(0f, 2.2f, 0.55f);
        [SerializeField] Vector3 peekTarget = new(0f, 0.62f, 1.92f);
        [SerializeField] OlamInteractable[] tandoorClicks = System.Array.Empty<OlamInteractable>();
        [SerializeField] GameObject[] hideInPeek = System.Array.Empty<GameObject>();   // градусник у самой камеры заслонил бы устье
        [SerializeField] float moveTime = 0.8f;

        bool peek;
        float t = 1f;
        Vector3 fromPosition;
        Quaternion fromRotation;

        void Awake()
        {
            Instance = this;
            transform.SetPositionAndRotation(overviewPosition, Quaternion.LookRotation(overviewTarget - overviewPosition));
            foreach (var click in tandoorClicks)
                if (click != null) click.Selected += Toggle;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            foreach (var click in tandoorClicks)
                if (click != null) click.Selected -= Toggle;
        }

        public void Toggle() => LookIntoTandoor(!peek);

        public void LookIntoTandoor(bool on)
        {
            if (on == peek) return;
            peek = on;
            foreach (var item in hideInPeek)
                if (item != null) item.SetActive(!on);
            fromPosition = transform.position;
            fromRotation = transform.rotation;
            t = 0f;
        }

        void LateUpdate()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.deltaTime / moveTime);
            float k = Mathf.SmoothStep(0f, 1f, t);
            var toPosition = peek ? peekPosition : overviewPosition;
            var toRotation = Quaternion.LookRotation((peek ? peekTarget : overviewTarget) - toPosition);
            transform.SetPositionAndRotation(Vector3.Lerp(fromPosition, toPosition, k), Quaternion.Slerp(fromRotation, toRotation, k));
        }
    }
}
