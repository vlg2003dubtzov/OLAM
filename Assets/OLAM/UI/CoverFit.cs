using UnityEngine;
using UnityEngine.UI;

namespace Olam.UI
{
    /// <summary>
    /// Картинка во весь прямоугольник без искажения: лишнее обрезается вокруг точки фокуса (доли картинки, 0…1).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RawImage))]
    public class CoverFit : MonoBehaviour
    {
        public Vector2 focus = new(0.5f, 0.5f);

        void OnEnable() => Apply();
        void OnRectTransformDimensionsChange() => Apply();

        public void Apply()
        {
            var image = GetComponent<RawImage>();
            var tex = image != null ? image.texture : null;
            var size = ((RectTransform)transform).rect.size;
            if (tex == null || size.x <= 0f || size.y <= 0f) return;
            float view = size.x / size.y, art = tex.width / (float)tex.height;
            var uv = new Rect(0f, 0f, 1f, 1f);
            if (view > art)
            {
                uv.height = art / view;
                uv.y = Mathf.Clamp(focus.y - uv.height * 0.5f, 0f, 1f - uv.height);
            }
            else
            {
                uv.width = view / art;
                uv.x = Mathf.Clamp(focus.x - uv.width * 0.5f, 0f, 1f - uv.width);
            }
            image.uvRect = uv;
        }
    }
}
