using UnityEngine;
using UnityEngine.UI;

namespace Warteg
{
    /// <summary>Progress bar melayang di dunia 3D (dipakai kompor, aksi karyawan, dll).</summary>
    public class WorldBar : MonoBehaviour
    {
        Image fill;
        Text label;
        CanvasGroup group;
        RectTransform canvasRect;

        public static WorldBar Create(Transform parent, Vector3 localPos, Color fillColor, float width = 110f)
        {
            GameObject go = new GameObject("WorldBar");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 26f);
            rt.localScale = Vector3.one * 0.0075f;

            WorldBar bar = go.AddComponent<WorldBar>();
            bar.canvasRect = rt;
            bar.group = go.AddComponent<CanvasGroup>();
            bar.group.alpha = 0f;

            // background
            GameObject bg = new GameObject("bg");
            RectTransform bgRt = bg.AddComponent<RectTransform>();
            bgRt.SetParent(rt, false);
            bgRt.anchorMin = new Vector2(0f, 0f);
            bgRt.anchorMax = new Vector2(1f, 1f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.08f, 0.08f, 0.10f, 0.78f);
            bgImg.raycastTarget = false;

            // fill
            GameObject fg = new GameObject("fill");
            RectTransform fgRt = fg.AddComponent<RectTransform>();
            fgRt.SetParent(rt, false);
            fgRt.anchorMin = new Vector2(0f, 0f);
            fgRt.anchorMax = new Vector2(1f, 1f);
            fgRt.offsetMin = new Vector2(3f, 3f);
            fgRt.offsetMax = new Vector2(-3f, -3f);
            Image fgImg = fg.AddComponent<Image>();
            fgImg.color = fillColor;
            fgImg.raycastTarget = false;
            fgImg.type = Image.Type.Filled;
            fgImg.fillMethod = Image.FillMethod.Horizontal;
            fgImg.fillAmount = 0f;
            bar.fill = fgImg;

            // label
            GameObject lbl = new GameObject("label");
            RectTransform lblRt = lbl.AddComponent<RectTransform>();
            lblRt.SetParent(rt, false);
            lblRt.anchorMin = new Vector2(0f, 1f);
            lblRt.anchorMax = new Vector2(1f, 1f);
            lblRt.pivot = new Vector2(0.5f, 0f);
            lblRt.anchoredPosition = new Vector2(0f, 4f);
            lblRt.sizeDelta = new Vector2(width, 30f);
            Text txt = lbl.AddComponent<Text>();
            txt.font = FontProvider.Get();
            txt.fontSize = 26;
            txt.alignment = TextAnchor.LowerCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            bar.label = txt;

            return bar;
        }

        public void SetValue(float v01)
        {
            if (fill != null) fill.fillAmount = Mathf.Clamp01(v01);
        }

        public void SetLabel(string text)
        {
            if (label != null) label.text = text;
        }

        public void SetColor(Color c)
        {
            if (fill != null) fill.color = c;
        }

        public void Show(bool visible, float alpha = 1f)
        {
            if (group == null) return;
            group.alpha = visible ? alpha : 0f;
        }

        public void FaceCamera(Camera cam)
        {
            if (cam == null || canvasRect == null) return;
            canvasRect.rotation = cam.transform.rotation;
        }
    }
}
