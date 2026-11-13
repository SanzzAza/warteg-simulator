using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Warteg
{
    /// <summary>HUD dalam permainan: saldo, jam, tanggal, reputasi, pelanggan, tombol menu.</summary>
    public class HUDUI : MonoBehaviour
    {
        Canvas canvas;
        RectTransform root;

        Text moneyText, clockText, dateText, repText, customerText, cleanText, hintText, levelText, xpText;
        Image xpBar, cleanBar, repBar;
        Text toastText;
        RectTransform orderContent, cookStrip;
        readonly List<GameObject> orderRows = new List<GameObject>();
        readonly List<Image> cookBars = new List<Image>();
        readonly List<Text> cookLabels = new List<Text>();
        float toastTimer;
        RectTransform pauseOverlay;
        bool visible;

        public void Build()
        {
            CanvasScaler scaler;
            canvas = UIFactory.CreateCanvas("HUD_Canvas", 10, out scaler);
            root = canvas.GetComponent<RectTransform>();

            BuildTopBar();
            BuildOrderPanel();
            BuildActionBar();
            BuildCookStrip();
            BuildToast();
            BuildPauseMenu();

            GameManager.I.OnMoneyChanged += RefreshMoney;
            GameManager.I.OnProgressChanged += RefreshStatus;
            GameManager.I.OnClockChanged += RefreshClock;
            GameManager.I.OnNotify += ShowToast;

            RefreshAll();
            SetVisible(false);
        }

        // =====================================================================
        void BuildTopBar()
        {
            // ---- saldo ----
            Image moneyPanel = UIFactory.Panel(root, "moneyPanel", UITheme.PanelDark);
            RectTransform mrt = moneyPanel.rectTransform;
            mrt.anchorMin = new Vector2(0f, 1f);
            mrt.anchorMax = new Vector2(0f, 1f);
            mrt.pivot = new Vector2(0f, 1f);
            mrt.anchoredPosition = new Vector2(18f, -18f);
            mrt.sizeDelta = new Vector2(430f, 100f);

            moneyText = UIFactory.LabelAt(moneyPanel.transform, "money", "Rp0", 40, UITheme.Gold,
                new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(22f, 0f), new Vector2(-10f, -6f), TextAnchor.MiddleLeft);
            levelText = UIFactory.LabelAt(moneyPanel.transform, "level", "Lv 1", 22, UITheme.TextLight,
                new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(22f, 6f), new Vector2(-10f, 0f), TextAnchor.MiddleLeft);
            xpBar = UIFactory.ProgressBar(moneyPanel.transform, "xpBar", 0f, UITheme.Accent, new Color(0.35f, 0.32f, 0.30f));
            RectTransform xprt = xpBar.transform.parent.GetComponent<RectTransform>();
            xprt.anchorMin = new Vector2(0f, 0f);
            xprt.anchorMax = new Vector2(1f, 0f);
            xprt.pivot = new Vector2(0.5f, 0f);
            xprt.anchoredPosition = new Vector2(0f, 8f);
            xprt.sizeDelta = new Vector2(-40f, 10f);
            xpText = UIFactory.LabelAt(moneyPanel.transform, "xpText", "", 18, UITheme.TextMuted,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(22f, 20f), new Vector2(-10f, 40f), TextAnchor.MiddleLeft);

            // ---- jam & tanggal ----
            Image clockPanel = UIFactory.Panel(root, "clockPanel", UITheme.PanelDark);
            RectTransform crt = clockPanel.rectTransform;
            crt.anchorMin = new Vector2(0.5f, 1f);
            crt.anchorMax = new Vector2(0.5f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = new Vector2(0f, -18f);
            crt.sizeDelta = new Vector2(460f, 100f);

            clockText = UIFactory.LabelAt(clockPanel.transform, "clock", "06:00", 40, UITheme.TextLight,
                new Vector2(0f, 0.4f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, -4f), TextAnchor.MiddleCenter);
            dateText = UIFactory.LabelAt(clockPanel.transform, "date", "Hari 1", 22, UITheme.Accent,
                new Vector2(0f, 0f), new Vector2(1f, 0.45f), Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);

            // ---- reputasi & pelanggan ----
            Image statusPanel = UIFactory.Panel(root, "statusPanel", UITheme.PanelDark);
            RectTransform srt = statusPanel.rectTransform;
            srt.anchorMin = new Vector2(1f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(1f, 1f);
            srt.anchoredPosition = new Vector2(-18f, -18f);
            srt.sizeDelta = new Vector2(430f, 100f);

            repText = UIFactory.LabelAt(statusPanel.transform, "rep", "Reputasi ★", 24, UITheme.TextLight,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(20f, -4f), new Vector2(-10f, 0f), TextAnchor.MiddleLeft);
            customerText = UIFactory.LabelAt(statusPanel.transform, "cust", "Pelanggan: 0", 22, UITheme.Accent,
                new Vector2(0f, 0f), new Vector2(0.55f, 0.5f), new Vector2(20f, 4f), new Vector2(0f, 0f), TextAnchor.MiddleLeft);
            cleanText = UIFactory.LabelAt(statusPanel.transform, "clean", "Bersih 100%", 20, UITheme.TextLight,
                new Vector2(0.5f, 0f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(-14f, 0f), TextAnchor.MiddleRight);
            repBar = UIFactory.ProgressBar(statusPanel.transform, "repBar", 0.1f, UITheme.Accent, new Color(0.35f, 0.32f, 0.30f));
            RectTransform rprt = repBar.transform.parent.GetComponent<RectTransform>();
            rprt.anchorMin = new Vector2(0f, 0f);
            rprt.anchorMax = new Vector2(1f, 0f);
            rprt.pivot = new Vector2(0.5f, 0f);
            rprt.anchoredPosition = new Vector2(0f, 10f);
            rprt.sizeDelta = new Vector2(-40f, 8f);
        }

        // =====================================================================
        void BuildOrderPanel()
        {
            Image panel = UIFactory.Panel(root, "orderPanel", UITheme.PanelDark);
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = new Vector2(1f, 1f);
            prt.anchorMax = new Vector2(1f, 1f);
            prt.pivot = new Vector2(1f, 1f);
            prt.anchoredPosition = new Vector2(-18f, -134f);
            prt.sizeDelta = new Vector2(430f, 430f);

            UIFactory.LabelAt(panel.transform, "title", "PESANAN MASUK", 22, UITheme.Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -44f), new Vector2(-16f, -8f), TextAnchor.MiddleLeft);

            ScrollRect scroll;
            orderContent = UIFactory.ScrollList(panel.transform, "scroll", out scroll);
            RectTransform srt = scroll.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(8f, 10f);
            srt.offsetMax = new Vector2(-8f, -46f);
        }

        void BuildActionBar()
        {
            Image panel = UIFactory.Panel(root, "actionBar", new Color(0.15f, 0.13f, 0.12f, 0.55f));
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = new Vector2(1f, 0f);
            prt.anchorMax = new Vector2(1f, 0f);
            prt.pivot = new Vector2(1f, 0f);
            prt.anchoredPosition = new Vector2(-18f, 18f);
            prt.sizeDelta = new Vector2(560f, 150f);

            Button manage = UIFactory.Button(panel.transform, "btnManage", "KELOLA WARTEG", UITheme.Primary, UITheme.TextLight,
                () => { if (GameManager.I.Management != null) GameManager.I.Management.OpenTab(TabId.Dashboard); });
            SetRect(manage.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(300f, 60f));

            Button pasar = UIFactory.Button(panel.transform, "btnPasar", "PASAR", UITheme.Green, UITheme.TextLight,
                () => { if (GameManager.I.Management != null) GameManager.I.Management.OpenTab(TabId.Pasar); });
            SetRect(pasar.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(220f, 60f));

            Button rotL = UIFactory.Button(panel.transform, "btnRotL", "⟲", UITheme.PanelDark, UITheme.TextLight,
                () => { if (GameManager.I.World != null && CameraRigRef != null) CameraRigRef.Rotate(-35f); });
            SetRect(rotL.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(12f, 12f), new Vector2(90f, 54f));

            Button rotR = UIFactory.Button(panel.transform, "btnRotR", "⟳", UITheme.PanelDark, UITheme.TextLight,
                () => { if (CameraRigRef != null) CameraRigRef.Rotate(35f); });
            SetRect(rotR.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(112f, 12f), new Vector2(90f, 54f));

            Button zoomIn = UIFactory.Button(panel.transform, "btnZoomIn", "+", UITheme.PanelDark, UITheme.TextLight,
                () => { if (CameraRigRef != null) CameraRigRef.Zoom(2.5f); });
            SetRect(zoomIn.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(212f, 12f), new Vector2(90f, 54f));

            Button zoomOut = UIFactory.Button(panel.transform, "btnZoomOut", "−", UITheme.PanelDark, UITheme.TextLight,
                () => { if (CameraRigRef != null) CameraRigRef.Zoom(-2.5f); });
            SetRect(zoomOut.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(312f, 12f), new Vector2(90f, 54f));

            Button menu = UIFactory.Button(panel.transform, "btnMenu", "MENU", UITheme.Accent, UITheme.TextDark,
                () => TogglePauseMenu());
            SetRect(menu.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-12f, 12f), new Vector2(220f, 54f));

            hintText = UIFactory.LabelAt(root, "hint", "", 20, UITheme.TextLight,
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 22f), new Vector2(700f, 62f), TextAnchor.LowerLeft);
        }

        CameraRig CameraRigRef
        {
            get
            {
                if (GameManager.I == null || GameManager.I.Player == null) return null;
                Camera cam = GameManager.I.Player.cam;
                if (cam == null) return null;
                return cam.GetComponent<CameraRig>();
            }
        }

        static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(aMin.x, aMin.y);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        void BuildCookStrip()
        {
            Image panel = UIFactory.Panel(root, "cookStrip", new Color(0.15f, 0.13f, 0.12f, 0.55f));
            cookStrip = panel.rectTransform;
            cookStrip.anchorMin = new Vector2(0.5f, 0f);
            cookStrip.anchorMax = new Vector2(0.5f, 0f);
            cookStrip.pivot = new Vector2(0.5f, 0f);
            cookStrip.anchoredPosition = new Vector2(0f, 18f);
            cookStrip.sizeDelta = new Vector2(700f, 96f);
            cookStrip.gameObject.SetActive(false);
        }

        void BuildToast()
        {
            Image panel = UIFactory.Panel(root, "toast", new Color(0.10f, 0.09f, 0.08f, 0.85f));
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 1f);
            prt.anchorMax = new Vector2(0.5f, 1f);
            prt.pivot = new Vector2(0.5f, 1f);
            prt.anchoredPosition = new Vector2(0f, -132f);
            prt.sizeDelta = new Vector2(760f, 60f);
            toastText = UIFactory.LabelAt(panel.transform, "txt", "", 24, UITheme.TextLight,
                Vector2.zero, Vector2.one, new Vector2(18f, 4f), new Vector2(-18f, -4f), TextAnchor.MiddleCenter);
            panel.gameObject.SetActive(false);
            toastRoot = panel.gameObject;
        }

        GameObject toastRoot;

        void BuildPauseMenu()
        {
            Image overlay = UIFactory.Img(root, "pauseOverlay", new Color(0f, 0f, 0f, 0.65f));
            pauseOverlay = overlay.rectTransform;
            pauseOverlay.SetAsLastSibling();

            Image panel = UIFactory.Panel(pauseOverlay, "panel", UITheme.Panel);
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(560f, 620f);

            UIFactory.LabelAt(panel.transform, "title", "JEDA", 44, UITheme.TextDark,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -80f), new Vector2(0f, -20f), TextAnchor.MiddleCenter);

            string[] labels = { "Lanjutkan", "Simpan Progres", "Pengaturan", "Menu Utama" };
            System.Action[] actions =
            {
                () => TogglePauseMenu(),
                () => { GameManager.I.SaveGame(false); },
                () => { ClosePauseMenu(); if (GameManager.I.Settings != null) GameManager.I.Settings.Open(); },
                () => { ClosePauseMenu(); GameManager.I.QuitToMainMenu(true); }
            };

            for (int i = 0; i < labels.Length; i++)
            {
                int idx = i;
                Color bg = i == 0 ? UITheme.Green : (i == 3 ? UITheme.Red : UITheme.Primary);
                Button b = UIFactory.Button(panel.transform, "pauseBtn" + i, labels[i], bg, UITheme.TextLight,
                    () => actions[idx](), 26);
                RectTransform rt = b.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -140f - i * 96f);
                rt.sizeDelta = new Vector2(400f, 78f);
            }

            pauseOverlay.gameObject.SetActive(false);
        }

        // =====================================================================
        public void SetVisible(bool v)
        {
            visible = v;
            if (root != null) root.gameObject.SetActive(v);
        }

        public void TogglePauseMenu()
        {
            if (pauseOverlay == null) return;
            if (pauseOverlay.gameObject.activeSelf) ClosePauseMenu();
            else OpenPauseMenu();
        }

        void OpenPauseMenu()
        {
            pauseOverlay.gameObject.SetActive(true);
            GameManager.I.PushPause();
        }

        void ClosePauseMenu()
        {
            if (pauseOverlay == null || !pauseOverlay.gameObject.activeSelf) return;
            pauseOverlay.gameObject.SetActive(false);
            GameManager.I.PopPause();
        }

        public bool IsPausedMenuOpen { get { return pauseOverlay != null && pauseOverlay.gameObject.activeSelf; } }

        // =====================================================================
        public void RefreshAll()
        {
            if (GameManager.I == null || GameManager.I.Data == null) return;
            RefreshMoney();
            RefreshStatus();
            RefreshClock();
            RefreshOrderList();
        }

        public void RefreshMoney()
        {
            if (moneyText == null) return;
            moneyText.text = Catalog.Money(GameManager.I.Money);
            moneyText.color = GameManager.I.Money < 0 ? UITheme.Red : UITheme.Gold;
        }

        public void RefreshStatus()
        {
            if (GameManager.I == null || GameManager.I.Data == null) return;
            GameManager gm = GameManager.I;

            if (levelText != null)
                levelText.text = "Lv " + gm.Level + " • " + Catalog.LevelTitle(gm.Level);
            if (xpText != null)
                xpText.text = Mathf.RoundToInt(gm.Xp) + " / " + Mathf.RoundToInt(gm.XpNeeded) + " XP";
            UIFactory.SetBar(xpBar, gm.XpNeeded > 0f ? gm.Xp / gm.XpNeeded : 0f, UITheme.Accent);

            if (repText != null)
                repText.text = "Reputasi " + gm.Reputation + "/100 — " + Catalog.ReputationLabel(gm.Reputation);
            UIFactory.SetBar(repBar, gm.Reputation / 100f,
                gm.Reputation > 60 ? UITheme.Green : (gm.Reputation > 30 ? UITheme.Accent : UITheme.Red));

            if (customerText != null)
            {
                int waiting = gm.Spawner != null ? gm.Spawner.WaitingCount : 0;
                int total = gm.Spawner != null ? gm.Spawner.CustomerCount : 0;
                customerText.text = "Pelanggan: " + total + (waiting > 0 ? "  (" + waiting + " menunggu)" : "");
                customerText.color = waiting > 0 ? UITheme.Accent : UITheme.TextLight;
            }

            if (cleanText != null)
            {
                cleanText.text = "Bersih " + Mathf.RoundToInt(gm.Cleanliness) + "%";
                cleanText.color = gm.Cleanliness > 60 ? UITheme.TextLight : UITheme.Red;
            }
        }

        public void RefreshClock()
        {
            if (clockText == null || GameManager.I.Time == null) return;
            TimeSystem t = GameManager.I.Time;
            clockText.text = t.ClockText() + "  " + Catalog.WeatherIcon(t.Weather);
            dateText.text = "Hari " + t.Day + " • " + t.DayOfWeekName() + " • " + Catalog.Phase(t.Hour);
            if (hintText != null)
            {
                string hint = "Klik meja/etalase/kompor untuk berinteraksi • WASD untuk jalan";
                if (t.Hour >= Catalog.CloseHour - 0.5f) hint = "Warteg hampir tutup — bereskan pesanan!";
                hintText.text = hint;
            }
            RefreshAll2();
        }

        void RefreshAll2()
        {
            // perbarui daftar pesanan secara berkala
            if (Time.frameCount % 30 == 0) RefreshOrderList();
        }

        // =====================================================================
        public void RefreshOrderList()
        {
            if (orderContent == null || GameManager.I == null) return;

            for (int i = 0; i < orderRows.Count; i++) if (orderRows[i] != null) Destroy(orderRows[i]);
            orderRows.Clear();

            CustomerSpawner sp = GameManager.I.Spawner;
            if (sp == null) return;

            List<CustomerAI> waiting = sp.WaitingCustomers();
            if (waiting.Count == 0)
            {
                RectTransform empty = UIFactory.Row(orderContent, 60f);
                UIFactory.Label(empty, "txt", "Belum ada pesanan.\nPelanggan datang otomatis.", UITheme.FontSmall,
                    new Color(0.75f, 0.72f, 0.68f), TextAnchor.MiddleCenter);
                orderRows.Add(empty.gameObject);
                return;
            }

            for (int i = 0; i < waiting.Count; i++)
            {
                CustomerAI c = waiting[i];
                if (c == null) continue;

                RectTransform row = UIFactory.Row(orderContent, 96f);
                Image bg = UIFactory.Img(row, "bg", UITheme.CardAlt, Sprites.Rounded);

                UIFactory.LabelAt(row, "name", c.arch.label, 22, UITheme.TextDark,
                    new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(14f, 0f), new Vector2(-100f, -4f), TextAnchor.MiddleLeft);
                UIFactory.LabelAt(row, "order", c.OrderSummary, 18, UITheme.TextMuted,
                    new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(14f, 4f), new Vector2(-100f, 0f), TextAnchor.MiddleLeft);
                UIFactory.LabelAt(row, "price", Catalog.Money(c.OrderTotal), 20, UITheme.Green,
                    new Vector2(1f, 0.5f), new Vector2(1f, 1f), new Vector2(-96f, 0f), new Vector2(-8f, -4f), TextAnchor.MiddleRight);

                // bar kesabaran
                Image pat = UIFactory.ProgressBar(row, "patience", c.PatienceLeft01,
                    c.PatienceLeft01 > 0.5f ? UITheme.Green : (c.PatienceLeft01 > 0.25f ? UITheme.Accent : UITheme.Red),
                    new Color(0.80f, 0.78f, 0.74f));
                RectTransform prt = pat.transform.parent.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(0f, 0f);
                prt.anchorMax = new Vector2(1f, 0f);
                prt.pivot = new Vector2(0.5f, 0f);
                prt.anchoredPosition = new Vector2(-50f, 6f);
                prt.sizeDelta = new Vector2(-120f, 8f);

                CustomerAI target = c;
                Button serve = UIFactory.SmallButton(row, "Layani", UITheme.Primary, UITheme.TextLight,
                    () => ServeFromList(target));
                RectTransform srt = serve.GetComponent<RectTransform>();
                srt.anchorMin = new Vector2(1f, 0.5f);
                srt.anchorMax = new Vector2(1f, 0.5f);
                srt.pivot = new Vector2(1f, 0.5f);
                srt.anchoredPosition = new Vector2(-8f, 0f);
                srt.sizeDelta = new Vector2(84f, 44f);

                orderRows.Add(row.gameObject);
            }
        }

        void ServeFromList(CustomerAI c)
        {
            if (c == null) return;
            PlayerController p = GameManager.I.Player;
            if (p == null) { c.TryServeFromDisplay(); return; }
            p.WalkToAndInteract(c);
        }

        // =====================================================================
        public void OnCookingStarted(RecipeDef recipe, int batches) { RebuildCookStrip(); }
        public void OnCookingFinished() { RebuildCookStrip(); }

        void RebuildCookStrip()
        {
            if (cookStrip == null) return;
            UIFactory.Clear(cookStrip);
            cookBars.Clear();
            cookLabels.Clear();

            KitchenManager k = GameManager.I.Kitchen;
            if (k == null || k.StoveCount == 0)
            {
                cookStrip.gameObject.SetActive(false);
                return;
            }

            int active = 0;
            float y = 0f;
            for (int i = 0; i < k.Stations.Count; i++)
            {
                CookingStation st = k.Stations[i];
                if (st == null || !st.Busy) continue;

                UIFactory.LabelAt(cookStrip, "lbl" + i,
                    (st.RecipeId != null ? GameManager.I.RecipeName(st.RecipeId) : "Memasak"), 20, UITheme.TextLight,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(16f, -32f + y), new Vector2(-16f, -6f + y), TextAnchor.MiddleLeft);

                Image bar = UIFactory.ProgressBar(cookStrip, "bar" + i, st.Progress01, UITheme.Accent, new Color(0.35f, 0.33f, 0.30f));
                RectTransform brt = bar.transform.parent.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0f, 1f);
                brt.anchorMax = new Vector2(1f, 1f);
                brt.pivot = new Vector2(0.5f, 1f);
                brt.anchoredPosition = new Vector2(0f, -40f + y);
                brt.sizeDelta = new Vector2(-32f, 12f);

                cookBars.Add(bar);
                cookLabels.Add(null);
                y -= 44f;
                active++;
            }

            cookStrip.sizeDelta = new Vector2(700f, Mathf.Max(60f, 20f + active * 44f));
            cookStrip.anchoredPosition = new Vector2(0f, 18f);
            cookStrip.gameObject.SetActive(active > 0);
        }

        void Update()
        {
            if (GameManager.I == null || GameManager.I.Kitchen == null) return;

            // update progress bar memasak
            int idx = 0;
            for (int i = 0; i < GameManager.I.Kitchen.Stations.Count && idx < cookBars.Count; i++)
            {
                CookingStation st = GameManager.I.Kitchen.Stations[i];
                if (st == null || !st.Busy) continue;
                if (cookBars[idx] != null) cookBars[idx].fillAmount = st.Progress01;
                idx++;
            }
            if (idx == 0 && cookStrip != null && cookStrip.gameObject.activeSelf) RebuildCookStrip();

            if (toastTimer > 0f)
            {
                toastTimer -= Time.deltaTime;
                if (toastTimer <= 0f && toastRoot != null) toastRoot.SetActive(false);
            }

            if (Time.frameCount % 10 == 0) RefreshStatus();
        }

        // =====================================================================
        public void ShowToast(string message, int kind)
        {
            if (toastRoot == null || toastText == null) return;
            toastText.text = message;
            toastText.color = kind == 1 ? new Color(0.65f, 0.95f, 0.68f)
                             : kind == 2 ? new Color(1f, 0.62f, 0.55f) : UITheme.TextLight;
            toastRoot.SetActive(true);
            toastTimer = 3.4f;
        }
    }
}
