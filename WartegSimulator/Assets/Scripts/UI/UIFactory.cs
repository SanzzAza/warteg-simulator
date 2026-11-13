using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Warteg
{
    /// <summary>Warna & ukuran standar UI (tema hangat khas warteg).</summary>
    public static class UITheme
    {
        public static Color Bg = new Color(0.12f, 0.10f, 0.09f, 0.92f);
        public static Color Panel = new Color(0.98f, 0.95f, 0.89f, 0.98f);
        public static Color PanelDark = new Color(0.20f, 0.17f, 0.15f, 0.98f);
        public static Color Card = new Color(1f, 0.99f, 0.96f, 1f);
        public static Color CardAlt = new Color(0.94f, 0.90f, 0.83f, 1f);
        public static Color Primary = new Color(0.80f, 0.28f, 0.20f);
        public static Color PrimaryDark = new Color(0.62f, 0.19f, 0.13f);
        public static Color Accent = new Color(0.95f, 0.68f, 0.18f);
        public static Color Green = new Color(0.24f, 0.62f, 0.34f);
        public static Color Red = new Color(0.78f, 0.24f, 0.20f);
        public static Color TextDark = new Color(0.16f, 0.13f, 0.11f);
        public static Color TextLight = new Color(0.99f, 0.98f, 0.95f);
        public static Color TextMuted = new Color(0.45f, 0.40f, 0.36f);
        public static Color Gold = new Color(1f, 0.82f, 0.28f);

        public static int FontTitle = 46;
        public static int FontHeader = 32;
        public static int FontBody = 24;
        public static int FontSmall = 20;
    }

    /// <summary>Pabrik komponen UGUI supaya semua layar konsisten dan ringkas dibuat.</summary>
    public static class UIFactory
    {
        public static Canvas CreateCanvas(string name, int sortOrder, out CanvasScaler scaler)
        {
            GameObject go = new GameObject(name);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();

            // pakai InputSystemUIInputModule bila Input System tersedia, kalau tidak modul lama
            Type t = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (t != null)
            {
                Component c = go.AddComponent(t);
                if (c != null) return;
            }
            go.AddComponent<StandaloneInputModule>();
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            return rt;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                                         Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rt = Rect(parent, name);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Anchor(Transform parent, string name, Vector2 anchor, Vector2 pivot,
                                           Vector2 pos, Vector2 size)
        {
            RectTransform rt = Rect(parent, name);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Img(Transform parent, string name, Color color, Sprite sprite = null)
        {
            RectTransform rt = Rect(parent, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            if (sprite != null)
            {
                img.sprite = sprite;
                img.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            }
            return img;
        }

        public static Image Panel(Transform parent, string name, Color color, Sprite sprite = null)
        {
            Image img = Img(parent, name, color, sprite ?? Sprites.Rounded);
            return img;
        }

        public static Text Label(Transform parent, string name, string text, int size, Color color,
                                 TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            RectTransform rt = Rect(parent, name);
            Text t = rt.gameObject.AddComponent<Text>();
            t.text = text;
            t.font = FontProvider.Get();
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }

        public static Text LabelAt(Transform parent, string name, string text, int size, Color color,
                                   Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
                                   TextAnchor align = TextAnchor.MiddleLeft)
        {
            Text t = Label(parent, name, text, size, color, align);
            RectTransform rt = t.rectTransform;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return t;
        }

        public static Button Button(Transform parent, string name, string text, Color bg, Color textColor,
                                    Action onClick, int fontSize = 0)
        {
            Image img = Img(parent, name, bg, Sprites.Rounded);
            Button b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            cb.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            b.transition = Selectable.Transition.ColorTint;

            Text t = Label(img.transform, "text", text, fontSize > 0 ? fontSize : UITheme.FontBody, textColor, TextAnchor.MiddleCenter);
            RectTransform trt = t.rectTransform;
            trt.anchorMin = new Vector2(0f, 0f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(10f, 4f);
            trt.offsetMax = new Vector2(-10f, -4f);

            if (onClick != null)
                b.onClick.AddListener(() =>
                {
                    if (GameManager.I != null && GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.Click);
                    onClick();
                });

            return b;
        }

        public static Button SmallButton(Transform parent, string text, Color bg, Color textColor, Action onClick)
        {
            return Button(parent, "btn_" + text, text, bg, textColor, onClick, UITheme.FontSmall);
        }

        /// <summary>Scroll view vertikal; mengembalikan RectTransform content.</summary>
        public static RectTransform ScrollList(Transform parent, string name, out ScrollRect scroll)
        {
            RectTransform root = Rect(parent, name);
            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.02f);
            bg.raycastTarget = true;

            RectTransform viewport = Rect(root, "viewport");
            RectMask2D mask = viewport.gameObject.AddComponent<RectMask2D>();
            mask.padding = Vector4.zero;

            RectTransform content = Rect(viewport, "content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = new Vector2(0f, 0f);
            content.offsetMax = new Vector2(0f, 0f);
            content.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vl = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 10f;
            vl.padding = new RectOffset(6, 14, 6, 12);
            vl.childForceExpandHeight = false;
            vl.childForceExpandWidth = true;
            vl.childControlHeight = false;
            vl.childControlWidth = true;

            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll = root.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 32f;

            return content;
        }

        public static RectTransform Row(Transform parent, float height)
        {
            RectTransform rt = Rect(parent, "row");
            LayoutElement le = rt.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleWidth = 1f;
            return rt;
        }

        public static Image ProgressBar(Transform parent, string name, float value01, Color fillColor, Color bgColor)
        {
            Image bg = Img(parent, name, bgColor, Sprites.Rounded);
            RectTransform fillRt = Rect(bg.transform, "fill", new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(2f, 2f), new Vector2(-2f, -2f));
            Image fill = fillRt.gameObject.AddComponent<Image>();
            fill.color = fillColor;
            fill.sprite = Sprites.Rounded;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = Mathf.Clamp01(value01);
            fill.raycastTarget = false;
            return fill;
        }

        public static void SetBar(Image bar, float value01, Color color)
        {
            if (bar == null) return;
            bar.fillAmount = Mathf.Clamp01(value01);
            bar.color = color;
        }

        public static void Clear(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                GameObject go = t.GetChild(i).gameObject;
                go.SetActive(false);
                UnityEngine.Object.Destroy(go);
            }
        }

        public static string Stars(float rating01)
        {
            int full = Mathf.RoundToInt(Mathf.Clamp01(rating01) * 5f);
            string s = "";
            for (int i = 0; i < 5; i++) s += i < full ? "★" : "☆";
            return s;
        }
    }

    /// <summary>Sprite yang dibuat saat runtime (sudut membulat & lingkaran) tanpa file PNG.</summary>
    public static class Sprites
    {
        static Sprite rounded;
        static Sprite circle;

        public static Sprite Rounded
        {
            get
            {
                if (rounded == null) rounded = MakeRounded(48, 14, new Vector4(16, 16, 16, 16));
                return rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (circle == null) circle = MakeRounded(64, 32, Vector4.zero);
                return circle;
            }
        }

        static Sprite MakeRounded(int size, int radius, Vector4 border)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = 0f, dy = 0f;
                    if (x < radius) dx = radius - x;
                    else if (x >= size - radius) dx = x - (size - radius - 1);
                    if (y < radius) dy = radius - y;
                    else if (y >= size - radius) dy = y - (size - radius - 1);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    byte a = (byte)(Mathf.Clamp01(radius - dist + 0.5f) * 255f);
                    px[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }
    }
}
