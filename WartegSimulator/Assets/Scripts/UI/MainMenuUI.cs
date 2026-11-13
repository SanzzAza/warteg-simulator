using UnityEngine;
using UnityEngine.UI;

namespace Warteg
{
    /// <summary>Menu utama: Play Game, Continue, Settings, Exit Game.</summary>
    public class MainMenuUI : MonoBehaviour
    {
        Canvas canvas;
        RectTransform root;
        GameObject overlayRoot;
        Button continueButton;
        Text saveInfo;
        GameObject confirmRoot;

        public void Build()
        {
            CanvasScaler scaler;
            canvas = UIFactory.CreateCanvas("MainMenu_Canvas", 30, out scaler);
            root = canvas.GetComponent<RectTransform>();

            Image bg = UIFactory.Img(root, "bg", new Color(0.06f, 0.05f, 0.05f, 0.62f));
            overlayRoot = bg.gameObject;

            // panel kiri
            Image panel = UIFactory.Panel(bg.transform, "panel", new Color(0.14f, 0.11f, 0.10f, 0.94f));
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(0f, 1f);
            prt.pivot = new Vector2(0f, 0.5f);
            prt.offsetMin = new Vector2(0f, 0f);
            prt.offsetMax = new Vector2(760f, 0f);

            UIFactory.LabelAt(panel.transform, "title", "WARTEG", 92, UITheme.Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(70f, -230f), new Vector2(-20f, -80f), TextAnchor.MiddleLeft);
            UIFactory.LabelAt(panel.transform, "title2", "SIMULATOR", 72, UITheme.TextLight,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(70f, -320f), new Vector2(-20f, -200f), TextAnchor.MiddleLeft);
            UIFactory.LabelAt(panel.transform, "sub", "Kelola warteg dari modal Rp2.000.000\nsampai jadi restoran besar.",
                26, new Color(0.82f, 0.78f, 0.72f),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(70f, -420f), new Vector2(-40f, -330f), TextAnchor.UpperLeft);

            float y = -470f;
            Button play = UIFactory.Button(panel.transform, "play", "PLAY GAME", UITheme.Primary, UITheme.TextLight, OnPlayPressed, 32);
            Place(play, y); y -= 100f;

            continueButton = UIFactory.Button(panel.transform, "continue", "CONTINUE", UITheme.Green, UITheme.TextLight, OnContinuePressed, 32);
            Place(continueButton, y); y -= 100f;

            Button settings = UIFactory.Button(panel.transform, "settings", "SETTINGS", new Color(0.35f, 0.32f, 0.30f), UITheme.TextLight,
                () => { if (GameManager.I.Settings != null) GameManager.I.Settings.Open(); }, 32);
            Place(settings, y); y -= 100f;

            Button exit = UIFactory.Button(panel.transform, "exit", "EXIT GAME", UITheme.Red, UITheme.TextLight,
                () => GameManager.I.QuitApplication(), 32);
            Place(exit, y); y -= 120f;

            saveInfo = UIFactory.LabelAt(panel.transform, "saveinfo", "", 22, new Color(0.78f, 0.74f, 0.68f),
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(70f, 60f), new Vector2(-40f, 160f), TextAnchor.LowerLeft);

            UIFactory.LabelAt(bg.transform, "version", "Warteg Simulator v1.0 • Unity URP • Dibuat untuk Android & Windows",
                20, new Color(0.85f, 0.85f, 0.85f, 0.7f),
                new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-520f, 22f), new Vector2(-30f, 60f), TextAnchor.MiddleRight);

            BuildConfirm(bg.transform);
            RefreshContinueButton();
        }

        void Place(Button b, float y)
        {
            RectTransform rt = b.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(70f, y);
            rt.sizeDelta = new Vector2(560f, 82f);
        }

        void BuildConfirm(Transform parent)
        {
            Image dim = UIFactory.Img(parent, "confirm", new Color(0f, 0f, 0f, 0.6f));
            confirmRoot = dim.gameObject;

            Image panel = UIFactory.Panel(dim.transform, "p", UITheme.Panel);
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(760f, 380f);

            UIFactory.LabelAt(panel.transform, "t", "Mulai permainan baru?", 40, UITheme.TextDark,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -110f), new Vector2(-30f, -30f), TextAnchor.MiddleCenter);
            UIFactory.LabelAt(panel.transform, "s", "Progres yang tersimpan akan ditimpa.", 24, UITheme.TextMuted,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -180f), new Vector2(-30f, -110f), TextAnchor.MiddleCenter);

            Button yes = UIFactory.Button(panel.transform, "yes", "Ya, Mulai Baru", UITheme.Red, UITheme.TextLight, () =>
            {
                confirmRoot.SetActive(false);
                GameManager.I.StartNewGame();
            }, 26);
            RectTransform yrt = yes.GetComponent<RectTransform>();
            yrt.anchorMin = new Vector2(0f, 0f); yrt.anchorMax = new Vector2(0f, 0f);
            yrt.pivot = new Vector2(0f, 0f); yrt.anchoredPosition = new Vector2(40f, 40f); yrt.sizeDelta = new Vector2(320f, 76f);

            Button no = UIFactory.Button(panel.transform, "no", "Batal", new Color(0.6f, 0.56f, 0.52f), UITheme.TextLight,
                () => confirmRoot.SetActive(false), 26);
            RectTransform nrt = no.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(1f, 0f); nrt.anchorMax = new Vector2(1f, 0f);
            nrt.pivot = new Vector2(1f, 0f); nrt.anchoredPosition = new Vector2(-40f, 40f); nrt.sizeDelta = new Vector2(320f, 76f);

            confirmRoot.SetActive(false);
        }

        void OnPlayPressed()
        {
            if (SaveSystem.HasSave) confirmRoot.SetActive(true);
            else GameManager.I.StartNewGame();
        }

        void OnContinuePressed()
        {
            if (!GameManager.I.ContinueGame())
                GameManager.I.Notify("Belum ada progres tersimpan.", 2);
        }

        public void RefreshContinueButton()
        {
            if (saveInfo == null) return;
            bool has = SaveSystem.HasSave;
            saveInfo.text = has ? "Progres tersimpan:\n" + SaveSystem.SaveInfo() : "Belum ada progres tersimpan";
            if (continueButton != null) continueButton.interactable = has;
        }

        public void SetVisible(bool visible)
        {
            if (overlayRoot != null) overlayRoot.SetActive(visible);
            if (visible) RefreshContinueButton();
        }
    }
}
