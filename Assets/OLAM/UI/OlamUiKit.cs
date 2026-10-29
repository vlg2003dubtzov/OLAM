using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Olam.UI
{
    /// <summary>
    /// Детали интерфейса OLAM: цвета айдентики и сборка элементов из кода. Меню строится при запуске,
    /// а не хранится в сцене, поэтому пересборка сцен его не ломает.
    /// </summary>
    public static class OlamUiKit
    {
        public static readonly Color Lapis = Hex(0x142B4C);
        public static readonly Color Turquoise = Hex(0x4FC3C0);
        public static readonly Color TurquoiseSoft = Hex(0xE3EFEE);
        public static readonly Color Parchment = Hex(0xF6F1E7);

        static Sprite rampAcross, rampUp;

        public static Color Hex(int rgb, float alpha = 1f) =>
            new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, alpha);

        public static Color Alpha(Color c, float alpha) => new(c.r, c.g, c.b, alpha);

        /// <summary>Холст поверх экрана; макет 1920×1080, масштаб по высоте экрана.</summary>
        public static Canvas Overlay(string name, int order, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            return canvas;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name) =>
            Rect(parent, name, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        public static Image Fill(RectTransform rt, Color color)
        {
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Переход цвета в прозрачность: across — слева прозрачно, справа плотно; иначе плотно снизу, flip — сверху.</summary>
        public static Image Ramp(RectTransform rt, Color color, bool across, bool flip = false)
        {
            var image = Fill(rt, color);
            if (across)
            {
                if (rampAcross == null) rampAcross = MakeRamp(true);
                image.sprite = rampAcross;
            }
            else
            {
                if (rampUp == null) rampUp = MakeRamp(false);
                image.sprite = rampUp;
            }
            if (flip) rt.localScale = across ? new Vector3(-1f, 1f, 1f) : new Vector3(1f, -1f, 1f);
            return image;
        }

        public static TMP_Text Label(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position,
            Vector2 size, float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var rt = Rect(parent, name, anchor, anchor, pivot, position, size);
            var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = align;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Button Button(Transform parent, string name, Vector2 position, Vector2 size, Color fill,
            Color textColor, float fontSize, out TMP_Text label)
        {
            var centre = new Vector2(0.5f, 0.5f);
            var rt = Rect(parent, name, centre, centre, centre, position, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = fill;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.9f, 0.9f, 0.9f);
            colors.highlightedColor = Color.white;
            colors.selectedColor = colors.normalColor;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            button.colors = colors;
            label = Label(rt, "Text", centre, centre, Vector2.zero, size, fontSize, textColor);
            return button;
        }

        /// <summary>Без EventSystem кнопки не нажимаются; в проекте только новый Input System.</summary>
        public static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        static Sprite MakeRamp(bool across)
        {
            const int n = 256;
            var tex = new Texture2D(across ? n : 1, across ? 1 : n, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
            for (int i = 0; i < n; i++)
            {
                float t = i / (n - 1f);
                var c = new Color(1f, 1f, 1f, t * t * (3f - 2f * t));
                if (across) tex.SetPixel(i, 0, c);
                else tex.SetPixel(0, n - 1 - i, c);
            }
            tex.Apply();
            var sprite = Sprite.Create(tex, new UnityEngine.Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            sprite.hideFlags = HideFlags.DontSave;
            return sprite;
        }
    }
}
