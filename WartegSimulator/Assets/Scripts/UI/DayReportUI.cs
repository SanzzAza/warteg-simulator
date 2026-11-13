using UnityEngine;
using UnityEngine.UI;

namespace Warteg
{
    /// <summary>Laporan akhir hari: pemasukan, pengeluaran, tagihan, dan tombol lanjut.</summary>
    public class DayReportUI : MonoBehaviour
    {
        Canvas canvas;
        GameObject overlay;
        Text titleText, statsText, billsText, verdictText;

        public bool IsOpen { get { return overlay != null && overlay.activeSelf; } }

        public void Build()
        {
            CanvasScaler scaler;
            canvas = UIFactory.CreateCanvas("DayReport_Canvas", 45, out scaler);
            RectTransform root = canvas.GetComponent<RectTransform>();

            Image dim = UIFactory.Img(root, "overlay", new Color(0f, 0f, 0f, 0.78f));
            overlay = dim.gameObject;

            Image panel = UIFactory.Panel(dim.transform, "panel", UITheme.Panel);
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(880f, 860f);

            titleText = UIFactory.LabelAt(panel.transform, "t", "LAPORAN HARIAN", 46, UITheme.PrimaryDark,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(30f, -96f), new Vector2(-30f, -20f), TextAnchor.MiddleCenter);

            statsText = UIFactory.LabelAt(panel.transform, "stats", "", 26, UITheme.TextDark,
                new Vector2(0f, 0.28f), new Vector2(1f, 1f), new Vector2(48f, 0f), new Vector2(-48f, -110f), TextAnchor.UpperLeft);

            billsText = UIFactory.LabelAt(panel.transform, "bills", "", 24, UITheme.TextMuted,
                new Vector2(0f, 0.02f), new Vector2(1f, 0.42f), new Vector2(48f, 130f), new Vector2(-48f, 0f), TextAnchor.UpperLeft);

            verdictText = UIFactory.LabelAt(panel.transform, "verdict", "", 26, UITheme.Green,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(48f, 110f), new Vector2(-48f, 190f), TextAnchor.MiddleCenter);

            Button next = UIFactory.Button(panel.transform, "next", "HARI BERIKUTNYA", UITheme.Green, UITheme.TextLight,
                () => GameManager.I.ContinueToNextDay(), 30);
            RectTransform nrt = next.GetComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0.5f, 0f);
            nrt.anchorMax = new Vector2(0.5f, 0f);
            nrt.pivot = new Vector2(0.5f, 0f);
            nrt.anchoredPosition = new Vector2(0f, 30f);
            nrt.sizeDelta = new Vector2(520f, 84f);

            overlay.SetActive(false);
        }

        public void Show(DailyReport rep, bool rentCharged, int electricity, int water, int salary, int maintenance)
        {
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            GameManager.I.PushPause();

            titleText.text = "LAPORAN HARI " + rep.day;
            statsText.text =
                "Omzet penjualan      : " + Catalog.Money(rep.revenue) + "\n" +
                "Total pengeluaran    : " + Catalog.Money(rep.expense) + "\n" +
                "<b>Laba bersih          : " + Catalog.Money(rep.profit) + "</b>\n\n" +
                "Pelanggan dilayani   : " + rep.customersServed + " orang\n" +
                "Pelanggan kecewa     : " + rep.customersLost + " orang\n" +
                "Kepuasan rata-rata   : " + Mathf.RoundToInt(rep.avgSatisfaction) + "%\n" +
                "Menu terlaris        : " + rep.bestSeller + "\n" +
                "Cuaca hari ini       : " + Catalog.WeatherIcon((Weather)rep.weather) + " " + Catalog.WeatherName((Weather)rep.weather);

            billsText.text =
                "Rincian tagihan yang dibayar:\n" +
                "• Listrik " + Catalog.Money(electricity) + "\n" +
                "• Air " + Catalog.Money(water) + "\n" +
                "• Gaji karyawan " + Catalog.Money(salary) + "\n" +
                "• Perawatan " + Catalog.Money(maintenance) + "\n" +
                (rentCharged ? "• Sewa tempat " + Catalog.Money(Catalog.RentPerWeek) + " (mingguan)" : "• Sewa tempat: belum jatuh tempo");

            if (rep.profit > 0)
            {
                verdictText.text = "Untung! Pertahankan pelayanan cepat dan harga wajar.";
                verdictText.color = UITheme.Green;
            }
            else
            {
                verdictText.text = "Rugi hari ini. Kurangi belanja, naikkan efisiensi, atau sesuaikan harga.";
                verdictText.color = UITheme.Red;
            }
        }

        public void Close()
        {
            if (!IsOpen) return;
            overlay.SetActive(false);
            GameManager.I.PopPause();
        }
    }
}
