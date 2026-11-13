using UnityEngine;
using UnityEngine.UI;

namespace Warteg
{
    /// <summary>Pengaturan: volume musik/SFX, kecepatan waktu, kualitas grafis.</summary>
    public class SettingsUI : MonoBehaviour
    {
        Canvas canvas;
        GameObject overlay;
        Slider musicSlider, sfxSlider, speedSlider;
        Text qualityText;
        int qualityLevel = 2;

        public bool IsOpen { get { return overlay != null && overlay.activeSelf; } }

        public void Build()
        {
            CanvasScaler scaler;
            canvas = UIFactory.CreateCanvas("Settings_Canvas", 40, out scaler);
            RectTransform root = canvas.GetComponent<RectTransform>();

            Image dim = UIFactory.Img(root, "overlay", new Color(0f, 0f, 0f, 0.72f));
            overlay = dim.gameObject;

            Image panel = UIFactory.Panel(dim.transform, "panel", UITheme.Panel);
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(820f, 700f);

            UIFactory.LabelAt(panel.transform, "t", "PENGATURAN", 44, UITheme.TextDark,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -90f), new Vector2(-30f, -20f), TextAnchor.MiddleCenter);

            SettingsSave s = GameManager.I.Data != null ? GameManager.I.Data.settings : new SettingsSave();

            musicSlider = SliderRow(panel.transform, "Volume Musik", s.musicVolume, -140f, v =>
            {
                GameManager.I.Data.settings.musicVolume = v;
                if (GameManager.I.Audio != null) GameManager.I.Audio.ApplySettings(GameManager.I.Data.settings);
            });
            sfxSlider = SliderRow(panel.transform, "Volume Efek Suara", s.sfxVolume, -230f, v =>
            {
                GameManager.I.Data.settings.sfxVolume = v;
                if (GameManager.I.Audio != null) GameManager.I.Audio.ApplySettings(GameManager.I.Data.settings);
            });
            speedSlider = SliderRow(panel.transform, "Kecepatan Waktu", Mathf.InverseLerp(0.5f, 2.5f, s.timeScale), -320f, v =>
            {
                GameManager.I.Data.settings.timeScale = Mathf.Lerp(0.5f, 2.5f, v);
            });

            qualityLevel = QualitySettings.GetQualityLevel();
            Button q = UIFactory.Button(panel.transform, "quality", "Kualitas: " + QualityName(qualityLevel),
                new Color(0.35f, 0.32f, 0.30f), UITheme.TextLight, () =>
                {
                    qualityLevel = (qualityLevel + 1) % 3;
                    QualitySettings.SetQualityLevel(Mathf.Min(qualityLevel, QualitySettings.names.Length - 1));
                    if (qualityText != null) qualityText.text = "Kualitas: " + QualityName(qualityLevel);
                }, 24);
            RectTransform qrt = q.GetComponent<RectTransform>();
            qrt.anchorMin = new Vector2(0.5f, 1f); qrt.anchorMax = new Vector2(0.5f, 1f);
            qrt.pivot = new Vector2(0.5f, 1f); qrt.anchoredPosition = new Vector2(0f, -410f); qrt.sizeDelta = new Vector2(520f, 74f);
            qualityText = q.transform.Find("text").GetComponent<Text>();
            if (qualityText != null) qualityText.text = "Kualitas: " + QualityName(qualityLevel);

            Button save = UIFactory.Button(panel.transform, "save", "Simpan & Tutup", UITheme.Green, UITheme.TextLight, Close, 26);
            RectTransform srt = save.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.5f, 0f); srt.anchorMax = new Vector2(0.5f, 0f);
            srt.pivot = new Vector2(0.5f, 0f); srt.anchoredPosition = new Vector2(0f, 40f); srt.sizeDelta = new Vector2(520f, 84f);

            overlay.SetActive(false);
        }

        static string QualityName(int level)
        {
            switch (level)
            {
                case 0: return "Rendah";
                case 1: return "Sedang";
                default: return "Tinggi";
            }
        }

        Slider SliderRow(Transform parent, string label, float value01, float y, UnityEngine.Events.UnityAction<float> onChange)
        {
            UIFactory.LabelAt(parent, "lbl_" + label, label, 26, UITheme.TextDark,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(50f, y - 60f), new Vector2(-50f, y), TextAnchor.MiddleLeft);

            RectTransform holder = UIFactory.Rect(parent, "slider_" + label,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(50f, y - 118f), new Vector2(-50f, y - 66f));

            Image bg = UIFactory.Img(holder, "bg", new Color(0.80f, 0.77f, 0.72f), Sprites.Rounded);
            bg.rectTransform.anchorMin = new Vector2(0f, 0.3f);
            bg.rectTransform.anchorMax = new Vector2(1f, 0.7f);
            bg.rectTransform.offsetMin = Vector2.zero;
            bg.rectTransform.offsetMax = Vector2.zero;

            RectTransform fillArea = UIFactory.Rect(holder, "fillArea");
            fillArea.anchorMin = new Vector2(0f, 0.3f);
            fillArea.anchorMax = new Vector2(1f, 0.7f);
            fillArea.offsetMin = new Vector2(2f, 2f);
            fillArea.offsetMax = new Vector2(-2f, -2f);
            Image fill = UIFactory.Img(fillArea, "fill", UITheme.Accent, Sprites.Rounded);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(1f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            RectTransform handleArea = UIFactory.Rect(holder, "handleArea");
            handleArea.offsetMin = new Vector2(12f, 0f);
            handleArea.offsetMax = new Vector2(-12f, 0f);
            Image handle = UIFactory.Img(handleArea, "handle", UITheme.Primary, Sprites.Circle);
            handle.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            handle.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            handle.rectTransform.sizeDelta = new Vector2(42f, 42f);

            Slider slider = holder.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = Mathf.Clamp01(value01);
            slider.onValueChanged.AddListener(onChange);
            return slider;
        }

        public void Open()
        {
            if (GameManager.I.IsPlaying && !IsOpen) GameManager.I.PushPause();
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            SyncFromData();
        }

        void SyncFromData()
        {
            if (GameManager.I.Data == null) return;
            SettingsSave s = GameManager.I.Data.settings;
            if (musicSlider != null) musicSlider.SetValueWithoutNotify(s.musicVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(s.sfxVolume);
            if (speedSlider != null) speedSlider.SetValueWithoutNotify(Mathf.InverseLerp(0.5f, 2.5f, s.timeScale));
        }

        public void Close()
        {
            if (!IsOpen) return;
            overlay.SetActive(false);
            if (GameManager.I.Data != null)
            {
                GameManager.I.SaveSettings();
                if (GameManager.I.IsPlaying) GameManager.I.SaveGame(true);
            }
            if (GameManager.I.IsPlaying) GameManager.I.PopPause();
        }
    }
}
