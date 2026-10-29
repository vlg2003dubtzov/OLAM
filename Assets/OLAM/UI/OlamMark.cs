using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Olam.UI
{
    /// <summary>
    /// Знак OLAM — стрельчатая арка-портал в круге, как в favicon.svg сайта (рисунок 200×200).
    /// Рисуется сеткой, без картинки: чёткий при любом размере.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class OlamMark : MaskableGraphic
    {
        [SerializeField] float ringWidth = 8f;
        [SerializeField] int arcSteps = 16;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float s = Mathf.Min(r.width, r.height) / 200f;
            var c = r.center;
            // координаты рисунка: y вниз, центр круга (100, 100)
            Vector2 P(Vector2 p) => c + new Vector2(p.x - 100f, 100f - p.y) * s;

            const int ring = 96;
            float outer = 92f + ringWidth * 0.5f, inner = 92f - ringWidth * 0.5f;
            for (int i = 0; i < ring; i++)
            {
                float a0 = i * Mathf.PI * 2f / ring, a1 = (i + 1) * Mathf.PI * 2f / ring;
                var d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
                var d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
                var o = new Vector2(100f, 100f);
                Quad(vh, P(o + d0 * outer), P(o + d1 * outer), P(o + d1 * inner), P(o + d0 * inner));
            }

            // арка — полоса между внешним и внутренним контуром; низ опор лежит на круге
            var a = Contour(64f, 136f, 104f, 72f, 184.7f);
            var b = Contour(82f, 118f, 120f, 36f, 190.2f);
            for (int i = 0; i < a.Count - 1; i++)
                Quad(vh, P(a[i]), P(a[i + 1]), P(b[i + 1]), P(b[i]));
        }

        /// <summary>Левая опора снизу вверх → дуга с центром у правой пяты → вершина → дуга → правая опора вниз.</summary>
        List<Vector2> Contour(float left, float right, float spring, float radius, float bottom)
        {
            var pts = new List<Vector2> { new(left, bottom), new(left, spring) };
            for (int k = 1; k <= arcSteps; k++)
            {
                float ang = Mathf.Lerp(180f, 240f, k / (float)arcSteps) * Mathf.Deg2Rad;
                pts.Add(new Vector2(right + radius * Mathf.Cos(ang), spring + radius * Mathf.Sin(ang)));
            }
            for (int k = 1; k <= arcSteps; k++)
            {
                float ang = Mathf.Lerp(300f, 360f, k / (float)arcSteps) * Mathf.Deg2Rad;
                pts.Add(new Vector2(left + radius * Mathf.Cos(ang), spring + radius * Mathf.Sin(ang)));
            }
            pts.Add(new Vector2(right, bottom));
            return pts;
        }

        void Quad(VertexHelper vh, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
        {
            int i = vh.currentVertCount;
            vh.AddVert(p0, color, Vector4.zero);
            vh.AddVert(p1, color, Vector4.zero);
            vh.AddVert(p2, color, Vector4.zero);
            vh.AddVert(p3, color, Vector4.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
