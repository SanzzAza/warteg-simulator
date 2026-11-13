using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Warteg
{
    public enum TabId
    {
        Dashboard = 0, Menu = 1, Inventaris = 2, Gudang = 3, Pasar = 4,
        Keuangan = 5, Karyawan = 6, Upgrade = 7, Statistik = 8
    }

    /// <summary>
    /// Menu manajemen warteg: dashboard, menu & harga, inventaris/etalase, gudang,
    /// pasar, keuangan, karyawan, upgrade, dan statistik.
    /// </summary>
    public class ManagementUI : MonoBehaviour
    {
        Canvas canvas;
        RectTransform root;
        GameObject overlay;
        RectTransform content;
        ScrollRect contentScroll;
        Text headerText, subHeaderText;
        TabId current = TabId.Dashboard;

        readonly Dictionary<TabId, Button> tabButtons = new Dictionary<TabId, Button>();
        static readonly TabId[] TabOrder =
        {
            TabId.Dashboard, TabId.Menu, TabId.Inventaris, TabId.Gudang,
            TabId.Pasar, TabId.Keuangan, TabId.Karyawan, TabId.Upgrade, TabId.Statistik
        };
        static readonly string[] TabNames =
        {
            "Dashboard", "Menu & Harga", "Etalase", "Gudang",
            "Pasar", "Keuangan", "Karyawan", "Upgrade", "Statistik"
        };

        // panel masak
        GameObject cookPanel;
        CookingStation cookStation;
        string cookRecipeId;
        int cookBatches = 1;
        RectTransform cookList;
        RectTransform cookFooter;

        public bool IsOpen { get { return overlay != null && overlay.activeSelf; } }

        // =====================================================================
        public void Build()
        {
            CanvasScaler scaler;
            canvas = UIFactory.CreateCanvas("Management_Canvas", 20, out scaler);
            root = canvas.GetComponent<RectTransform>();

            Image dim = UIFactory.Img(root, "overlay", new Color(0f, 0f, 0f, 0.55f));
            overlay = dim.gameObject;

            Button closeArea = dim.gameObject.AddComponent<Button>();
            closeArea.transition = Selectable.Transition.None;
            closeArea.onClick.AddListener(Close);

            Image panel = UIFactory.Panel(dim.transform, "panel", UITheme.Panel);
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0f, 0f);
            prt.anchorMax = new Vector2(1f, 1f);
            prt.offsetMin = new Vector2(70f, 60f);
            prt.offsetMax = new Vector2(-70f, -60f);
            // klik di dalam panel jangan menutup
            Image blocker = panel.gameObject.AddComponent<Image>();
            blocker.color = Color.clear;

            BuildTabRail(panel.transform);

            // header
            headerText = UIFactory.LabelAt(panel.transform, "header", "Dashboard", UITheme.FontTitle, UITheme.TextDark,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(340f, -84f), new Vector2(-140f, -18f), TextAnchor.MiddleLeft);
            subHeaderText = UIFactory.LabelAt(panel.transform, "sub", "", 22, UITheme.TextMuted,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(340f, -122f), new Vector2(-140f, -84f), TextAnchor.MiddleLeft);

            Button close = UIFactory.Button(panel.transform, "close", "✕", UITheme.Red, UITheme.TextLight, Close, 30);
            RectTransform crt = close.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1f, 1f);
            crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(1f, 1f);
            crt.anchoredPosition = new Vector2(-18f, -18f);
            crt.sizeDelta = new Vector2(64f, 64f);

            Image separator = UIFactory.Img(panel.transform, "sep", new Color(0.80f, 0.75f, 0.68f, 0.8f));
            RectTransform srt = separator.rectTransform;
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.offsetMin = new Vector2(330f, -130f);
            srt.offsetMax = new Vector2(-130f, -128f);

            content = UIFactory.ScrollList(panel.transform, "content", out contentScroll);
            RectTransform cntRt = contentScroll.GetComponent<RectTransform>();
            cntRt.anchorMin = new Vector2(0f, 0f);
            cntRt.anchorMax = new Vector2(1f, 1f);
            cntRt.offsetMin = new Vector2(330f, 20f);
            cntRt.offsetMax = new Vector2(-30f, -136f);

            BuildCookPanel(dim.transform);

            overlay.SetActive(false);
            GameManager.I.OnStateReloaded += () => { if (IsOpen) Rebuild(); };
        }

        void BuildTabRail(Transform parent)
        {
            Image rail = UIFactory.Panel(parent, "rail", UITheme.PanelDark);
            RectTransform rrt = rail.rectTransform;
            rrt.anchorMin = new Vector2(0f, 0f);
            rrt.anchorMax = new Vector2(0f, 1f);
            rrt.pivot = new Vector2(0f, 0.5f);
            rrt.offsetMin = new Vector2(0f, 0f);
            rrt.offsetMax = new Vector2(310f, 0f);

            UIFactory.LabelAt(rail.transform, "brand", "WARTEG\nSIMULATOR", 26, UITheme.Accent,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -100f), new Vector2(-10f, -18f), TextAnchor.MiddleCenter);

            for (int i = 0; i < TabOrder.Length; i++)
            {
                TabId tab = TabOrder[i];
                Button b = UIFactory.Button(rail.transform, "tab_" + tab, TabNames[i], new Color(0.28f, 0.24f, 0.22f), UITheme.TextLight,
                    () => OpenTab(tab), UITheme.FontBody);
                RectTransform rt = b.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.offsetMin = new Vector2(18f, 0f);
                rt.offsetMax = new Vector2(-18f, 0f);
                rt.anchoredPosition = new Vector2(0f, -120f - i * 78f);
                rt.sizeDelta = new Vector2(-36f, 66f);
                tabButtons[tab] = b;
            }
        }

        // =====================================================================
        public void OpenTab(TabId tab)
        {
            current = tab;
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            if (!GameManager.I.IsPaused) GameManager.I.PushPause();
            Rebuild();
        }

        public void Close()
        {
            if (!IsOpen) return;
            if (cookPanel != null) cookPanel.SetActive(false);
            overlay.SetActive(false);
            GameManager.I.PopPause();
        }

        public void RefreshOpenTabs()
        {
            if (IsOpen) Rebuild();
        }

        void Rebuild()
        {
            headerText.text = TabNames[(int)current];
            subHeaderText.text = SubHeaderFor(current);
            HighlightTab();
            UIFactory.Clear(content);
            switch (current)
            {
                case TabId.Dashboard: BuildDashboard(); break;
                case TabId.Menu: BuildMenuTab(); break;
                case TabId.Inventaris: BuildInventaris(); break;
                case TabId.Gudang: BuildGudang(); break;
                case TabId.Pasar: BuildPasar(); break;
                case TabId.Keuangan: BuildKeuangan(); break;
                case TabId.Karyawan: BuildKaryawan(); break;
                case TabId.Upgrade: BuildUpgrade(); break;
                case TabId.Statistik: BuildStatistik(); break;
            }
            if (cookPanel != null && cookPanel.activeSelf) RebuildCookList();
        }

        string SubHeaderFor(TabId t)
        {
            switch (t)
            {
                case TabId.Dashboard: return "Ringkasan kondisi warteg hari ini";
                case TabId.Menu: return "Atur harga jual — harga terlalu mahal membuat pelanggan kabur";
                case TabId.Inventaris: return "Isi etalase & kesegaran makanan yang siap dijual";
                case TabId.Gudang: return "Stok bahan mentah di gudang";
                case TabId.Pasar: return "Belanja bahan mentah ke pasar";
                case TabId.Keuangan: return "Pemasukan, pengeluaran, tagihan, dan laporan harian";
                case TabId.Karyawan: return "Rekrut dan kelola karyawan warteg";
                case TabId.Upgrade: return "Kembangkan warteg jadi restoran besar";
                default: return "Statistik sepanjang permainan";
            }
        }

        void HighlightTab()
        {
            foreach (KeyValuePair<TabId, Button> kv in tabButtons)
            {
                Image img = kv.Value.GetComponent<Image>();
                if (img != null) img.color = kv.Key == current ? UITheme.Primary : new Color(0.28f, 0.24f, 0.22f);
            }
        }

        // =====================================================================
        //  Helper baris kartu
        // =====================================================================
        RectTransform Card(float height, out Image bg, Color? color = null)
        {
            RectTransform row = UIFactory.Row(content, height);
            bg = UIFactory.Img(row, "bg", color.HasValue ? color.Value : UITheme.Card, Sprites.Rounded);
            return row;
        }

        Text CardTitle(RectTransform card, string text, Color? color = null, float leftPad = 18f)
        {
            return UIFactory.LabelAt(card, "title", text, UITheme.FontBody, color.HasValue ? color.Value : UITheme.TextDark,
                new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(leftPad, 0f), new Vector2(-14f, -6f), TextAnchor.MiddleLeft);
        }

        Text CardSub(RectTransform card, string text, Color? color = null, float leftPad = 18f)
        {
            return UIFactory.LabelAt(card, "sub", text, UITheme.FontSmall, color.HasValue ? color.Value : UITheme.TextMuted,
                new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(leftPad, 4f), new Vector2(-14f, 0f), TextAnchor.MiddleLeft);
        }

        Button CardBtn(RectTransform card, string label, Color color, System.Action act, float width = 140f, float offsetFromRight = 0f)
        {
            Button b = UIFactory.SmallButton(card, label, color, UITheme.TextLight, act);
            RectTransform rt = b.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-12f - offsetFromRight, 0f);
            rt.sizeDelta = new Vector2(width, 50f);
            return b;
        }

        void SectionHeader(string text)
        {
            RectTransform row = UIFactory.Row(content, 46f);
            UIFactory.Label(row, "sec", text, UITheme.FontHeader - 6, UITheme.PrimaryDark, TextAnchor.MiddleLeft);
        }

        void BarRow(string title, float value01, string valueText, Color color)
        {
            RectTransform row = UIFactory.Row(content, 74f);
            UIFactory.LabelAt(row, "t", title, UITheme.FontSmall, UITheme.TextDark,
                new Vector2(0f, 0.5f), new Vector2(0.45f, 1f), new Vector2(10f, 0f), Vector2.zero, TextAnchor.MiddleLeft);
            UIFactory.LabelAt(row, "v", valueText, UITheme.FontSmall, UITheme.TextMuted,
                new Vector2(0.45f, 0.5f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-10f, 0f), TextAnchor.MiddleRight);
            Image bar = UIFactory.ProgressBar(row, "bar", value01, color, new Color(0.86f, 0.83f, 0.78f));
            RectTransform brt = bar.transform.parent.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 0f);
            brt.anchorMax = new Vector2(1f, 0f);
            brt.pivot = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0f, 6f);
            brt.sizeDelta = new Vector2(-20f, 10f);
        }

        // =====================================================================
        //  TAB: DASHBOARD
        // =====================================================================
        void BuildDashboard()
        {
            GameManager gm = GameManager.I;

            Image bg;
            RectTransform card = Card(120f, out bg, UITheme.PanelDark);
            UIFactory.LabelAt(card, "money", Catalog.Money(gm.Money), 46,
                gm.Money < 0 ? UITheme.Red : UITheme.Gold, new Vector2(0f, 0.4f), new Vector2(1f, 1f),
                new Vector2(22f, 0f), new Vector2(-14f, -8f), TextAnchor.MiddleLeft);
            UIFactory.LabelAt(card, "sub", "Laba hari ini: " + Catalog.Money(gm.DayProfit) +
                "   •   Omzet: " + Catalog.Money(gm.Data.dayRevenue) + "   •   Biaya: " + Catalog.Money(gm.Data.dayExpense),
                22, UITheme.TextLight, new Vector2(0f, 0f), new Vector2(1f, 0.45f),
                new Vector2(22f, 8f), new Vector2(-14f, 0f), TextAnchor.MiddleLeft);

            SectionHeader("Progres");
            BarRow("Level " + gm.Level + " — " + Catalog.LevelTitle(gm.Level), gm.XpNeeded > 0f ? gm.Xp / gm.XpNeeded : 0f,
                Mathf.RoundToInt(gm.Xp) + " / " + Mathf.RoundToInt(gm.XpNeeded) + " XP", UITheme.Accent);
            BarRow("Reputasi — " + Catalog.ReputationLabel(gm.Reputation), gm.Reputation / 100f,
                gm.Reputation + " / 100", gm.Reputation > 55 ? UITheme.Green : UITheme.Red);
            BarRow("Kebersihan", gm.Cleanliness / 100f, Mathf.RoundToInt(gm.Cleanliness) + " %",
                gm.Cleanliness > 55 ? UITheme.Green : UITheme.Red);

            SectionHeader("Hari ini");
            int waiting = gm.Spawner != null ? gm.Spawner.WaitingCount : 0;
            InfoRow("Pelanggan dilayani", gm.Data.dayCustomersServed.ToString());
            InfoRow("Pelanggan kecewa", gm.Data.dayCustomersLost.ToString(), gm.Data.dayCustomersLost > 0 ? UITheme.Red : UITheme.TextDark);
            InfoRow("Pesanan menunggu", waiting.ToString(), waiting > 0 ? UITheme.Primary : UITheme.TextDark);
            InfoRow("Porsi di etalase", gm.Display != null ? gm.Display.TotalPortions + " / " + gm.Display.Capacity : "-");
            InfoRow("Cuaca", Catalog.WeatherIcon(gm.Time.Weather) + " " + Catalog.WeatherName(gm.Time.Weather));

            SectionHeader("Peringatan & Saran");
            bool any = false;
            if (gm.Display != null && !gm.Display.HasSpace()) { WarningRow("Etalase penuh! Buang makanan basi atau beli etalase baru."); any = true; }
            if (gm.Cleanliness < 40f) { WarningRow("Warteg kotor — bersihkan di wastafel agar pelanggan puas."); any = true; }
            if (gm.Qty("gas") < 10) { WarningRow("Gas LPG hampir habis. Beli di Pasar."); any = true; }
            if (gm.Qty("beras") < 1) { WarningRow("Beras habis — tidak bisa masak nasi."); any = true; }
            if (gm.Data.staff.Count == 0) { WarningRow("Belum ada karyawan. Rekrut koki/pelayan agar tidak kewalahan."); any = true; }
            if (gm.Reputation < 25) { WarningRow("Reputasi rendah — harga terlalu mahal atau pelayanan lambat?"); any = true; }
            if (!any) InfoRow("Semua berjalan lancar 👍", "Aman", UITheme.Green);

            SectionHeader("Aksi cepat");
            RectTransform actions = UIFactory.Row(content, 80f);
            Button b1 = UIFactory.SmallButton(actions, "Masak", UITheme.Primary, UITheme.TextLight, () => OpenCookPanelNearest());
            SetAnchored(b1.GetComponent<RectTransform>(), 0f, 190f);
            Button b2 = UIFactory.SmallButton(actions, "Belanja", UITheme.Green, UITheme.TextLight, () => OpenTab(TabId.Pasar));
            SetAnchored(b2.GetComponent<RectTransform>(), 200f, 190f);
            Button b3 = UIFactory.SmallButton(actions, "Harga Menu", UITheme.Accent, UITheme.TextDark, () => OpenTab(TabId.Menu));
            SetAnchored(b3.GetComponent<RectTransform>(), 400f, 190f);
            Button b4 = UIFactory.SmallButton(actions, "Servis & Upgrade", UITheme.PrimaryDark, UITheme.TextLight, () => OpenTab(TabId.Upgrade));
            SetAnchored(b4.GetComponent<RectTransform>(), 600f, 240f);
        }

        static void SetAnchored(RectTransform rt, float x, float w)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(x + 10f, 0f);
            rt.sizeDelta = new Vector2(w, 58f);
        }

        void InfoRow(string label, string value, Color? valueColor = null)
        {
            Image bg;
            RectTransform card = Card(64f, out bg, UITheme.CardAlt);
            CardTitle(card, label);
            CardSub(card, "");
            UIFactory.LabelAt(card, "v", value, UITheme.FontBody, valueColor.HasValue ? valueColor.Value : UITheme.TextDark,
                new Vector2(0.5f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-18f, 0f), TextAnchor.MiddleRight);
        }

        void WarningRow(string text)
        {
            Image bg;
            RectTransform card = Card(72f, out bg, new Color(1f, 0.92f, 0.86f));
            CardTitle(card, "⚠  " + text, UITheme.Red);
        }

        // =====================================================================
        //  TAB: MENU & HARGA
        // =====================================================================
        void BuildMenuTab()
        {
            GameManager gm = GameManager.I;
            SectionHeader("MAKANAN");
            for (int i = 0; i < Catalog.Recipes.Count; i++)
            {
                RecipeDef r = Catalog.Recipes[i];
                if (r.category != FoodCategory.Makanan) continue;
                BuildMenuRow(r);
            }
            SectionHeader("MINUMAN");
            for (int i = 0; i < Catalog.Recipes.Count; i++)
            {
                RecipeDef r = Catalog.Recipes[i];
                if (r.category != FoodCategory.Minuman) continue;
                BuildMenuRow(r);
            }
        }

        void BuildMenuRow(RecipeDef r)
        {
            GameManager gm = GameManager.I;
            MenuEntrySave e = gm.MenuEntry(r.id);
            bool unlocked = e != null && e.unlocked;
            int price = e != null ? e.price : r.defaultPrice;
            int cost = r.CostPerPortion();
            int margin = price - cost;
            int stock = gm.Display != null ? gm.Display.AvailablePortions(r.id) : 0;

            Image bg;
            RectTransform card = Card(unlocked ? 104f : 84f, out bg, unlocked ? UITheme.Card : new Color(0.90f, 0.88f, 0.85f));

            string title = (unlocked ? "" : "🔒 ") + r.name + "   " + UIFactory.Stars(r.popularity);
            CardTitle(card, title, unlocked ? UITheme.TextDark : UITheme.TextMuted);
            if (unlocked)
            {
                CardSub(card, "Modal " + Catalog.Money(cost) + "  •  Laba/porsi " + Catalog.Money(margin) +
                    (margin <= 0 ? " (RUGI!)" : "") + "  •  Etalase: " + stock + " porsi");
                CardSub(card, "", margin <= 0 ? UITheme.Red : UITheme.Green);

                Text priceText = UIFactory.LabelAt(card, "price", Catalog.Money(price), 28,
                    margin > 0 ? UITheme.TextDark : UITheme.Red,
                    new Vector2(0.55f, 0.45f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-330f, -4f), TextAnchor.MiddleRight);

                RecipeDef def = r;
                Button minus = UIFactory.SmallButton(card, "−", new Color(0.55f, 0.50f, 0.46f), UITheme.TextLight,
                    () => ChangePrice(def, -500));
                RectTransform mrt = minus.GetComponent<RectTransform>();
                mrt.anchorMin = new Vector2(1f, 0.5f); mrt.anchorMax = new Vector2(1f, 0.5f);
                mrt.pivot = new Vector2(1f, 0.5f); mrt.anchoredPosition = new Vector2(-330f, 0f); mrt.sizeDelta = new Vector2(64f, 50f);

                Button plus = UIFactory.SmallButton(card, "+", UITheme.Green, UITheme.TextLight,
                    () => ChangePrice(def, 500));
                RectTransform prt = plus.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(1f, 0.5f); prt.anchorMax = new Vector2(1f, 0.5f);
                prt.pivot = new Vector2(1f, 0.5f); prt.anchoredPosition = new Vector2(-255f, 0f); prt.sizeDelta = new Vector2(64f, 50f);

                Button cook = UIFactory.SmallButton(card, "Masak", UITheme.Primary, UITheme.TextLight,
                    () => OpenCookPanelFor(def.id));
                RectTransform crt = cook.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(1f, 0.5f); crt.anchorMax = new Vector2(1f, 0.5f);
                crt.pivot = new Vector2(1f, 0.5f); crt.anchoredPosition = new Vector2(-12f, 0f); crt.sizeDelta = new Vector2(210f, 56f);
            }
            else
            {
                CardSub(card, "Terbuka di Level " + r.unlockLevel + " • Modal " + Catalog.Money(cost) +
                    " • Harga wajar " + Catalog.Money(r.defaultPrice));
            }
        }

        void ChangePrice(RecipeDef def, int delta)
        {
            MenuEntrySave e = GameManager.I.MenuEntry(def.id);
            if (e == null) return;
            int before = e.price;
            e.price = Mathf.Clamp(e.price + delta, 500, 200000);
            if (e.price != before)
            {
                if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.Click);
                Rebuild();
            }
        }

        // =====================================================================
        //  TAB: ETALASE
        // =====================================================================
        void BuildInventaris()
        {
            GameManager gm = GameManager.I;
            DisplayCase disp = gm.Display;
            if (disp == null) { InfoRow("Etalase belum siap", "-"); return; }

            BarRow("Kapasitas etalase", disp.UsedSlots / (float)disp.Capacity,
                disp.UsedSlots + " / " + disp.Capacity + " jenis", UITheme.Accent);

            RectTransform act = UIFactory.Row(content, 76f);
            Button cook = UIFactory.SmallButton(act, "Masak Sekarang", UITheme.Primary, UITheme.TextLight, () => OpenCookPanelNearest());
            SetAnchored(cook.GetComponent<RectTransform>(), 0f, 240f);
            Button clean = UIFactory.SmallButton(act, "Buang yang Basi", UITheme.Red, UITheme.TextLight, () =>
            {
                int n = GameManager.I.Display.DiscardSpoiled();
                GameManager.I.Notify(n > 0 ? "Membuang " + n + " porsi basi." : "Tidak ada makanan basi.", n > 0 ? 1 : 0);
                Rebuild();
            });
            SetAnchored(clean.GetComponent<RectTransform>(), 250f, 240f);

            SectionHeader("Isi etalase");
            int shown = 0;
            for (int i = 0; i < disp.Slots.Count; i++)
            {
                DisplaySlotSave s = disp.Slots[i];
                RecipeDef r = Catalog.Recipe(s.foodId);
                if (r == null) continue;
                shown++;

                bool spoiled = disp.IsSpoiled(s);
                Image bg;
                RectTransform card = Card(104f, out bg, spoiled ? new Color(0.93f, 0.88f, 0.82f) : UITheme.Card);
                CardTitle(card, r.name + "  ×" + s.portions + (spoiled ? "  (BASI)" : ""),
                    spoiled ? UITheme.Red : UITheme.TextDark);
                CardSub(card, "Harga jual " + Catalog.Money(gm.PriceOf(s.foodId)) +
                    "  •  dimasak jam " + s.cookHour.ToString("00") + ":00");

                Text freshText = UIFactory.LabelAt(card, "fresh",
                    spoiled ? "Basi — tidak bisa dijual" : "Kesegaran " + Mathf.RoundToInt(s.freshness) + "%",
                    20, spoiled ? UITheme.Red : UITheme.TextMuted,
                    new Vector2(0.45f, 0.45f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-150f, -4f), TextAnchor.MiddleRight);

                Image bar = UIFactory.ProgressBar(card, "freshBar", s.freshness / 100f,
                    s.freshness > 50f ? UITheme.Green : (s.freshness > 20f ? UITheme.Accent : UITheme.Red),
                    new Color(0.86f, 0.83f, 0.78f));
                RectTransform brt = bar.transform.parent.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0f, 0f);
                brt.anchorMax = new Vector2(1f, 0f);
                brt.pivot = new Vector2(0.5f, 0f);
                brt.anchoredPosition = new Vector2(-80f, 8f);
                brt.sizeDelta = new Vector2(-200f, 10f);

                if (spoiled)
                {
                    CardBtn(card, "Buang", UITheme.Red, () =>
                    {
                        GameManager.I.Display.DiscardSpoiled();
                        Rebuild();
                    }, 140f);
                }
            }

            if (shown == 0)
                InfoRow("Etalase masih kosong. Masak dulu di kompor!", "-");

            // hasil masakan yang tertahan
            KitchenManager k = gm.Kitchen;
            if (k != null)
            {
                bool anyPending = false;
                for (int i = 0; i < k.Stations.Count; i++)
                {
                    CookingStation st = k.Stations[i];
                    if (st != null && st.PendingPortions > 0) anyPending = true;
                }
                if (anyPending)
                {
                    SectionHeader("Menunggu di kompor");
                    for (int i = 0; i < k.Stations.Count; i++)
                    {
                        CookingStation st = k.Stations[i];
                        if (st == null || st.PendingPortions <= 0) continue;
                        Image bg2;
                        RectTransform card = Card(72f, out bg2, new Color(1f, 0.95f, 0.85f));
                        CardTitle(card, "Kompor " + (st.index + 1) + ": " + st.PendingPortions + " porsi tertahan");
                        CookingStation target = st;
                        CardBtn(card, "Pindahkan", UITheme.Primary, () =>
                        {
                            target.CollectPending();
                            Rebuild();
                        }, 160f);
                    }
                }
            }
        }

        // =====================================================================
        //  TAB: GUDANG
        // =====================================================================
        void BuildGudang()
        {
            GameManager gm = GameManager.I;
            BarRow("Kapasitas gudang", gm.StorageUsed / (float)gm.StorageCap,
                gm.StorageUsed + " / " + gm.StorageCap + " unit  •  nilai " + Catalog.Money(gm.TotalIngredientValue()),
                gm.StorageUsed > gm.StorageCap * 0.9f ? UITheme.Red : UITheme.Green);

            SectionHeader("Bahan tersedia");
            bool any = false;
            for (int i = 0; i < Catalog.Ingredients.Count; i++)
            {
                IngredientDef def = Catalog.Ingredients[i];
                int qty = gm.Qty(def.id);
                if (qty <= 0) continue;
                any = true;
                Image bg;
                RectTransform card = Card(76f, out bg, UITheme.Card);
                CardTitle(card, def.icon + "  " + def.name + "  —  " + qty + " " + def.unit);
                CardSub(card, "Nilai " + Catalog.Money(def.unitPrice * qty) + "  •  harga pasar " + Catalog.Money(def.unitPrice) + "/" + def.unit);

                IngredientDef d = def;
                CardBtn(card, "+1 paket", UITheme.Green, () =>
                {
                    GameManager.I.BuyIngredient(d.id, 1);
                    Rebuild();
                }, 150f);
            }
            if (!any) InfoRow("Gudang kosong — belanja di tab Pasar.", "-");

            SectionHeader("Belanja cepat");
            RectTransform act = UIFactory.Row(content, 76f);
            Button go = UIFactory.SmallButton(act, "Buka Pasar", UITheme.Green, UITheme.TextLight, () => OpenTab(TabId.Pasar));
            SetAnchored(go.GetComponent<RectTransform>(), 0f, 220f);
        }

        // =====================================================================
        //  TAB: PASAR
        // =====================================================================
        void BuildPasar()
        {
            GameManager gm = GameManager.I;
            Image bgTop;
            RectTransform top = Card(94f, out bgTop, UITheme.PanelDark);
            UIFactory.LabelAt(top, "info", "Saldo: " + Catalog.Money(gm.Money) +
                "    Gudang: " + gm.StorageUsed + "/" + gm.StorageCap,
                26, UITheme.Gold, Vector2.zero, Vector2.one, new Vector2(22f, 6f), new Vector2(-14f, -6f), TextAnchor.MiddleLeft);

            for (int i = 0; i < Catalog.Ingredients.Count; i++)
            {
                IngredientDef def = Catalog.Ingredients[i];
                int stock = gm.Qty(def.id);
                int cost = def.unitPrice * def.packSize;

                Image bg;
                RectTransform card = Card(96f, out bg, stock > 0 ? UITheme.Card : UITheme.CardAlt);
                CardTitle(card, def.icon + "  " + def.name + "   " + Catalog.Money(def.unitPrice) + " / " + def.unit);
                CardSub(card, "1 paket = " + def.packSize + " " + def.unit + " (" + Catalog.Money(cost) + ")  •  stok: " + stock + " " + def.unit);

                IngredientDef d = def;
                CardBtn(card, "+5", UITheme.Accent, () =>
                {
                    GameManager.I.BuyIngredient(d.id, 5);
                    Rebuild();
                }, 90f, 130f);

                CardBtn(card, "+1", UITheme.Green, () =>
                {
                    GameManager.I.BuyIngredient(d.id, 1);
                    Rebuild();
                }, 120f, 0f);
            }

            SectionHeader("Catatan");
            InfoRow("Gas LPG dipakai setiap memasak", "Butuh " + gm.Qty("gas") + "%", gm.Qty("gas") < 20 ? UITheme.Red : UITheme.TextDark);
        }

        // =====================================================================
        //  TAB: KEUANGAN
        // =====================================================================
        void BuildKeuangan()
        {
            GameManager gm = GameManager.I;

            Image bg;
            RectTransform sum = Card(126f, out bg, UITheme.PanelDark);
            UIFactory.LabelAt(sum, "omzet", "Omzet hari ini: " + Catalog.Money(gm.Data.dayRevenue), 28, UITheme.Gold,
                new Vector2(0f, 0.55f), new Vector2(1f, 1f), new Vector2(22f, 0f), new Vector2(-14f, -6f), TextAnchor.MiddleLeft);
            UIFactory.LabelAt(sum, "laba", "Pengeluaran: " + Catalog.Money(gm.Data.dayExpense) +
                "     Laba bersih: " + Catalog.Money(gm.DayProfit),
                24, UITheme.TextLight, new Vector2(0f, 0f), new Vector2(1f, 0.6f),
                new Vector2(22f, 8f), new Vector2(-14f, 0f), TextAnchor.MiddleLeft);

            SectionHeader("Perkiraan tagihan hari ini");
            InfoRow("Listrik", Catalog.Money(gm.ElectricityCost()));
            InfoRow("Air", Catalog.Money(gm.WaterCost()));
            InfoRow("Gaji karyawan (" + gm.Data.staff.Count + " orang)", Catalog.Money(gm.StaffSalaryTotal()));
            InfoRow("Perawatan peralatan", Catalog.Money(gm.MaintenanceCost()));
            InfoRow("Sewa tempat (tiap 7 hari)", gm.RentDueToday() ? Catalog.Money(gm.RentAmount) + " — JATUH TEMPO" : Catalog.Money(gm.RentAmount) + " (hari ke-" + NextRentDay(gm) + ")");

            SectionHeader("Transaksi hari ini");
            List<LedgerEntry> ledger = gm.TodayLedger();
            if (ledger.Count == 0) InfoRow("Belum ada transaksi hari ini", "-");
            int shown = 0;
            for (int i = 0; i < ledger.Count && shown < 40; i++)
            {
                LedgerEntry e = ledger[i];
                shown++;
                bool income = e.kind == (int)LedgerKind.Pemasukan;
                Image bg2;
                RectTransform card = Card(66f, out bg2, income ? new Color(0.94f, 0.99f, 0.94f) : new Color(0.99f, 0.95f, 0.93f));
                CardTitle(card, (income ? "+ " : "− ") + Catalog.Money(e.amount) + "   " + (e.note ?? ""),
                    income ? UITheme.Green : UITheme.Red, 18f);
                UIFactory.LabelAt(card, "t", Mathf.FloorToInt(e.hour).ToString("00") + ":00", 20, UITheme.TextMuted,
                    new Vector2(0.8f, 0f), new Vector2(1f, 1f), Vector2.zero, new Vector2(-18f, 0f), TextAnchor.MiddleRight);
            }

            SectionHeader("Laporan harian");
            int count = gm.Data.history.Count;
            if (count == 0) InfoRow("Belum ada laporan harian", "-");
            for (int i = count - 1, shownH = 0; i >= 0 && shownH < 10; i--, shownH++)
            {
                DailyReport rep = gm.Data.history[i];
                Image bg3;
                RectTransform card = Card(96f, out bg3, rep.profit >= 0 ? UITheme.Card : new Color(0.99f, 0.95f, 0.93f));
                CardTitle(card, "Hari " + rep.day + " — Laba " + Catalog.Money(rep.profit),
                    rep.profit >= 0 ? UITheme.Green : UITheme.Red);
                CardSub(card, "Omzet " + Catalog.Money(rep.revenue) + " • Biaya " + Catalog.Money(rep.expense) +
                    " • Pelanggan " + rep.customersServed + " • " + Catalog.WeatherName((Weather)rep.weather));
            }
        }

        static int NextRentDay(GameManager gm)
        {
            int day = gm.Time.Day;
            int next = day + (7 - day % 7);
            return next;
        }

        // =====================================================================
        //  TAB: KARYAWAN
        // =====================================================================
        void BuildKaryawan()
        {
            GameManager gm = GameManager.I;
            StaffManager sm = gm.StaffMgr;
            if (sm == null) return;

            InfoRow("Karyawan terlatih", gm.Data.staff.Count + " / " + sm.MaxStaff);
            SectionHeader("Karyawan Anda");

            if (gm.Data.staff.Count == 0)
                InfoRow("Belum ada karyawan. Rekrut di bawah ini!", "-");

            for (int i = 0; i < gm.Data.staff.Count; i++)
            {
                StaffSave s = gm.Data.staff[i];
                StaffRole role = (StaffRole)s.role;
                Image bg;
                RectTransform card = Card(112f, out bg, UITheme.Card);
                CardTitle(card, s.name + "  —  " + Catalog.RoleName(role));
                CardSub(card, "Gaji " + Catalog.Money(s.salary) + "/hari  •  Kecepatan " + s.speed.ToString("0.00") +
                    "  •  Kemampuan " + Mathf.RoundToInt(s.skill) + "  •  Lv " + s.level);
                CardSub(card, "", UITheme.TextMuted);

                string id = s.id;
                CardBtn(card, "Pecat", UITheme.Red, () =>
                {
                    if (GameManager.I.StaffMgr != null) GameManager.I.StaffMgr.Fire(id);
                    Rebuild();
                }, 140f);
            }

            SectionHeader("Pelamar hari ini");
            List<StaffSave> candidates = sm.GetCandidates();
            for (int i = 0; i < candidates.Count; i++)
            {
                StaffSave c = candidates[i];
                StaffRole role = (StaffRole)c.role;
                Image bg;
                RectTransform card = Card(112f, out bg, UITheme.CardAlt);
                CardTitle(card, c.name + "  —  " + Catalog.RoleName(role));
                CardSub(card, "Gaji " + Catalog.Money(c.salary) + "/hari  •  Kecepatan " + c.speed.ToString("0.00") +
                    "  •  Kemampuan " + Mathf.RoundToInt(c.skill));
                CardSub(card, "Biaya rekrut: " + Catalog.Money(StaffManager.HireCost(c)));

                StaffSave cand = c;
                CardBtn(card, "Rekrut", UITheme.Green, () =>
                {
                    if (GameManager.I.StaffMgr != null) GameManager.I.StaffMgr.Hire(cand);
                    Rebuild();
                }, 150f);
            }

            SectionHeader("Tugas karyawan");
            InfoRow("Koki", "Memasak otomatis menu paling laris");
            InfoRow("Pelayan", "Mengantar pesanan ke meja");
            InfoRow("Kasir", "Membayar lebih cepat, tip lebih besar");
            InfoRow("Kebersihan", "Membersihkan warteg otomatis");
        }

        // =====================================================================
        //  TAB: UPGRADE
        // =====================================================================
        void BuildUpgrade()
        {
            GameManager gm = GameManager.I;
            UpgradeSystem up = gm.Upgrades;
            if (up == null) return;

            InfoRow("Saldo", Catalog.Money(gm.Money), UITheme.Green);
            InfoRow("Kompor / Meja / Etalase", up.StoveCount + " / " + up.TableCount + " / " + up.DisplaySlots + " slot");

            SectionHeader("Toko Upgrade");
            List<UpgradeDef> list = up.AvailableUpgrades();
            for (int i = 0; i < list.Count; i++)
            {
                UpgradeDef d = list[i];
                bool levelOk = gm.Level >= d.levelReq;
                Image bg;
                RectTransform card = Card(120f, out bg, levelOk ? UITheme.Card : new Color(0.90f, 0.88f, 0.85f));
                CardTitle(card, d.icon + "  " + d.name + (levelOk ? "" : "  (Level " + d.levelReq + ")"),
                    levelOk ? UITheme.TextDark : UITheme.TextMuted);
                CardSub(card, d.desc);
                CardSub(card, "Harga " + Catalog.Money(d.price), gm.CanAfford(d.price) && levelOk ? UITheme.Green : UITheme.Red);

                UpgradeDef def = d;
                CardBtn(card, "Beli", levelOk ? UITheme.Green : new Color(0.6f, 0.6f, 0.6f), () =>
                {
                    if (GameManager.I.Upgrades != null && GameManager.I.Upgrades.Purchase(def)) Rebuild();
                }, 150f);
            }

            SectionHeader("Sudah dipasang");
            List<UpgradeDef> owned = up.OwnedUpgrades();
            if (owned.Count == 0) InfoRow("Belum ada upgrade terpasang", "-");
            for (int i = 0; i < owned.Count; i++)
            {
                Image bg;
                RectTransform card = Card(70f, out bg, UITheme.CardAlt);
                CardTitle(card, "✔  " + owned[i].icon + " " + owned[i].name, UITheme.Green);
            }
        }

        // =====================================================================
        //  TAB: STATISTIK
        // =====================================================================
        void BuildStatistik()
        {
            GameManager gm = GameManager.I;
            StatSave s = gm.Data.stats;

            SectionHeader("Total");
            InfoRow("Total pemasukan", Catalog.Money(s.totalRevenue), UITheme.Green);
            InfoRow("Total pengeluaran", Catalog.Money(s.totalExpense), UITheme.Red);
            InfoRow("Laba sepanjang masa", Catalog.Money(s.totalRevenue - s.totalExpense),
                s.totalRevenue - s.totalExpense >= 0 ? UITheme.Green : UITheme.Red);
            InfoRow("Hari dimainkan", s.daysPlayed + " hari");
            InfoRow("Pelanggan dilayani", s.totalCustomers.ToString());
            InfoRow("Pelanggan kecewa", s.totalLost.ToString());
            InfoRow("Porsi basi", s.totalSpoiled.ToString());
            InfoRow("Laba terbaik dalam sehari", Catalog.Money(s.bestDayProfit), UITheme.Green);

            SectionHeader("Menu terlaris");
            List<IngredientStack> sold = new List<IngredientStack>(s.soldPerItem);
            sold.Sort((a, b) => b.qty.CompareTo(a.qty));
            for (int i = 0; i < sold.Count && i < 8; i++)
            {
                if (sold[i].qty <= 0) continue;
                InfoRow((i + 1) + ". " + gm.RecipeName(sold[i].id), sold[i].qty + " porsi");
            }
            if (sold.Count == 0) InfoRow("Belum ada penjualan", "-");

            SectionHeader("Reputasi");
            BarRow("Reputasi warteg", gm.Reputation / 100f, gm.Reputation + "/100 — " + Catalog.ReputationLabel(gm.Reputation), UITheme.Accent);
            BarRow("Level bisnis", gm.XpNeeded > 0 ? gm.Xp / gm.XpNeeded : 0f, "Lv " + gm.Level + " — " + Catalog.LevelTitle(gm.Level), UITheme.Primary);
        }

        // =====================================================================
        //  PANEL MASAK
        // =====================================================================
        void BuildCookPanel(Transform parent)
        {
            Image panel = UIFactory.Panel(parent, "cookPanel", UITheme.Panel);
            cookPanel = panel.gameObject;
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.pivot = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(900f, 820f);

            UIFactory.LabelAt(panel.transform, "title", "MEMASAK", UITheme.FontTitle, UITheme.TextDark,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -80f), new Vector2(-120f, -16f), TextAnchor.MiddleLeft);

            Button close = UIFactory.Button(panel.transform, "close", "✕", UITheme.Red, UITheme.TextLight,
                () => { cookPanel.SetActive(false); }, 28);
            RectTransform crt = close.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1f, 1f); crt.anchorMax = new Vector2(1f, 1f);
            crt.pivot = new Vector2(1f, 1f); crt.anchoredPosition = new Vector2(-16f, -16f); crt.sizeDelta = new Vector2(60f, 60f);

            ScrollRect scroll;
            RectTransform list = UIFactory.ScrollList(panel.transform, "list", out scroll);
            RectTransform srt = scroll.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0f);
            srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(24f, 150f);
            srt.offsetMax = new Vector2(-24f, -90f);
            cookList = list;

            Image footer = UIFactory.Panel(panel.transform, "footer", UITheme.CardAlt);
            cookFooter = footer.rectTransform;
            cookFooter.anchorMin = new Vector2(0f, 0f);
            cookFooter.anchorMax = new Vector2(1f, 0f);
            cookFooter.pivot = new Vector2(0.5f, 0f);
            cookFooter.offsetMin = new Vector2(24f, 20f);
            cookFooter.offsetMax = new Vector2(-24f, 130f);

            cookPanel.SetActive(false);
        }

        CookingStation nearestStation;

        void OpenCookPanelNearest()
        {
            KitchenManager k = GameManager.I.Kitchen;
            if (k == null || k.Stations.Count == 0) return;
            CookingStation best = k.FreeStation();
            if (best == null) best = k.Stations[0];
            OpenCookPanel(best);
        }

        void OpenCookPanelFor(string recipeId)
        {
            KitchenManager k = GameManager.I.Kitchen;
            if (k == null || k.Stations.Count == 0) return;
            CookingStation st = k.FreeStation();
            if (st == null)
            {
                GameManager.I.Notify("Semua kompor sedang dipakai.", 2);
                return;
            }
            cookRecipeId = recipeId;
            OpenCookPanel(st);
        }

        public void OpenCookPanel(CookingStation station)
        {
            if (station == null) return;
            cookStation = station;
            if (cookRecipeId == null) cookRecipeId = "nasi_putih";
            cookBatches = 1;
            if (!IsOpen) { overlay.SetActive(true); if (!GameManager.I.IsPaused) GameManager.I.PushPause(); }
            HighlightTab();
            UIFactory.Clear(content);
            switch (current)
            {
                case TabId.Dashboard: BuildDashboard(); break;
                case TabId.Menu: BuildMenuTab(); break;
                case TabId.Inventaris: BuildInventaris(); break;
                case TabId.Gudang: BuildGudang(); break;
                case TabId.Pasar: BuildPasar(); break;
                case TabId.Keuangan: BuildKeuangan(); break;
                case TabId.Karyawan: BuildKaryawan(); break;
                case TabId.Upgrade: BuildUpgrade(); break;
                case TabId.Statistik: BuildStatistik(); break;
            }
            cookPanel.SetActive(true);
            cookPanel.transform.SetAsLastSibling();
            RebuildCookList();
        }

        void RebuildCookList()
        {
            if (cookList == null) return;
            UIFactory.Clear(cookList);

            if (cookStation != null)
            {
                RectTransform info = UIFactory.Row(cookList, 60f);
                string statusText = cookStation.Busy
                    ? "Kompor " + (cookStation.index + 1) + " sedang memasak " + GameManager.I.RecipeName(cookStation.RecipeId)
                    : "Kompor " + (cookStation.index + 1) + " siap dipakai";
                UIFactory.Label(info, "info", statusText, UITheme.FontSmall,
                    cookStation.Busy ? UITheme.Red : UITheme.Green, TextAnchor.MiddleLeft);
            }

            for (int i = 0; i < Catalog.Recipes.Count; i++)
            {
                RecipeDef r = Catalog.Recipes[i];
                bool unlocked = GameManager.I.IsUnlocked(r.id);
                if (!unlocked) continue;

                bool can = GameManager.I.HasIngredients(r, 1);
                bool selected = cookRecipeId == r.id;

                RectTransform row = UIFactory.Row(cookList, 92f);
                Image bg = UIFactory.Img(row, "bg", selected ? new Color(1f, 0.93f, 0.80f) : UITheme.Card, Sprites.Rounded);

                UIFactory.LabelAt(row, "name", r.name + "   " + (r.category == FoodCategory.Minuman ? "🥤" : "🍛"), UITheme.FontBody,
                    UITheme.TextDark, new Vector2(0f, 0.45f), new Vector2(1f, 1f), new Vector2(16f, 0f), new Vector2(-340f, -4f), TextAnchor.MiddleLeft);
                UIFactory.LabelAt(row, "sub",
                    "Modal " + Catalog.Money(r.CostPerPortion()) + "/porsi  •  hasil " + r.yield + " porsi  •  " +
                    (can ? "bahan cukup" : "bahan kurang: " + GameManager.I.MissingIngredientText(r)),
                    UITheme.FontSmall, can ? UITheme.TextMuted : UITheme.Red,
                    new Vector2(0f, 0f), new Vector2(1f, 0.55f), new Vector2(16f, 4f), new Vector2(-340f, 0f), TextAnchor.MiddleLeft);

                RecipeDef def = r;
                Button pick = UIFactory.SmallButton(row, selected ? "Dipilih" : "Pilih", selected ? UITheme.Primary : UITheme.PrimaryDark,
                    UITheme.TextLight, () =>
                    {
                        cookRecipeId = def.id;
                        cookBatches = 1;
                        RebuildCookList();
                    });
                RectTransform prt = pick.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(1f, 0.5f); prt.anchorMax = new Vector2(1f, 0.5f);
                prt.pivot = new Vector2(1f, 0.5f); prt.anchoredPosition = new Vector2(-12f, 0f); prt.sizeDelta = new Vector2(150f, 54f);
            }

            RebuildCookFooter();
        }

        void RebuildCookFooter()
        {
            UIFactory.Clear(cookFooter);
            RecipeDef r = Catalog.Recipe(cookRecipeId);

            if (r == null)
            {
                UIFactory.Label(cookFooter, "x", "Pilih menu dulu", UITheme.FontBody, UITheme.TextDark, TextAnchor.MiddleCenter);
                return;
            }

            int maxB = GameManager.I.MaxBatches(r);

            UIFactory.LabelAt(cookFooter, "sel", r.name + "  •  " + (r.yield * cookBatches) + " porsi  •  modal total " +
                Catalog.Money(r.CostPerPortion() * r.yield * cookBatches),
                UITheme.FontBody, UITheme.TextDark,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(20f, 0f), new Vector2(-320f, -6f), TextAnchor.MiddleLeft);

            UIFactory.LabelAt(cookFooter, "batch", "Jumlah batch (maks " + maxB + ", bahan tersedia)",
                UITheme.FontSmall, UITheme.TextMuted,
                new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(20f, 6f), new Vector2(-320f, 0f), TextAnchor.MiddleLeft);

            Button minus = UIFactory.SmallButton(cookFooter, "−", new Color(0.55f, 0.5f, 0.46f), UITheme.TextLight, () =>
            {
                cookBatches = Mathf.Max(1, cookBatches - 1);
                RebuildCookFooter();
            });
            RectTransform mrt = minus.GetComponent<RectTransform>();
            mrt.anchorMin = new Vector2(1f, 0.5f); mrt.anchorMax = new Vector2(1f, 0.5f);
            mrt.pivot = new Vector2(1f, 0.5f); mrt.anchoredPosition = new Vector2(-420f, 0f); mrt.sizeDelta = new Vector2(60f, 54f);

            UIFactory.LabelAt(cookFooter, "bnum", cookBatches + "x", UITheme.FontHeader, UITheme.TextDark,
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-360f, 0f), new Vector2(-300f, 0f), TextAnchor.MiddleCenter);

            Button plus = UIFactory.SmallButton(cookFooter, "+", UITheme.Green, UITheme.TextLight, () =>
            {
                cookBatches = Mathf.Min(Mathf.Max(1, maxB), cookBatches + 1);
                RebuildCookFooter();
            });
            RectTransform prt = plus.GetComponent<RectTransform>();
            prt.anchorMin = new Vector2(1f, 0.5f); prt.anchorMax = new Vector2(1f, 0.5f);
            prt.pivot = new Vector2(1f, 0.5f); prt.anchoredPosition = new Vector2(-240f, 0f); prt.sizeDelta = new Vector2(60f, 54f);

            RecipeDef def2 = r;
            int batches = cookBatches;
            Button cook = UIFactory.SmallButton(cookFooter, "MASAK", UITheme.Primary, UITheme.TextLight, () =>
            {
                if (cookStation == null) return;
                if (cookStation.StartCook(def2, batches))
                {
                    cookPanel.SetActive(false);
                    Close();
                }
            });
            RectTransform crt = cook.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(1f, 0.5f); crt.anchorMax = new Vector2(1f, 0.5f);
            crt.pivot = new Vector2(1f, 0.5f); crt.anchoredPosition = new Vector2(-20f, 0f); crt.sizeDelta = new Vector2(200f, 66f);
        }
    }
}
