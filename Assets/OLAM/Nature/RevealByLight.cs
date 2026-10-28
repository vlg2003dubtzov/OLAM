using UnityEngine;

namespace Olam.Nature
{
    /// <summary>
    /// Прозрачность / видимость объекта под светом фонарика.
    /// </summary>
    public class RevealByLight : MonoBehaviour
    {
        [SerializeField] MeshRenderer target;
        [SerializeField] float revealSpeed = 2f;
        [SerializeField] float hiddenAlpha = 0.05f;
        [SerializeField] float revealedAlpha = 1f;

        float alpha;

        void Awake()
        {
            if (target == null) target = GetComponent<MeshRenderer>();
            alpha = hiddenAlpha;
            Apply();
        }

        void Update()
        {
            bool lit = false;
            Vector3 p = transform.position;
            for (int i = 0; i < Flashlight.All.Count; i++)
            {
                if (Flashlight.All[i].Illuminates(p))
                {
                    lit = true;
                    break;
                }
            }

            float goal = lit ? revealedAlpha : hiddenAlpha;
            alpha = Mathf.MoveTowards(alpha, goal, revealSpeed * Time.deltaTime);
            Apply();
        }

        void Apply()
        {
            if (target == null) return;
            var c = target.material.color;
            c.a = alpha;
            target.material.color = c;
            target.enabled = alpha > 0.01f;
        }
    }
}
