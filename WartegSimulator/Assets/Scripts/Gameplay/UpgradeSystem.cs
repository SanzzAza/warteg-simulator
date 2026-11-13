using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Menyimpan upgrade yang sudah dibeli dan menghitung semua efeknya
    /// (jumlah kompor, meja, kapasitas etalase, kecepatan masak, dsb).
    /// </summary>
    public class UpgradeSystem : MonoBehaviour
    {
        readonly HashSet<string> owned = new HashSet<string>();

        public int StoveCount { get; private set; }
        public int TableCount { get; private set; }
        public int DisplaySlots { get; private set; }
        public int StorageBonus { get; private set; }
        public float CookSpeedMul { get; private set; }
        public float SpoilMul { get; private set; }
        public float ReputationMul { get; private set; }
        public float Comfort { get; private set; }
        public float DemandMul { get; private set; }
        public float TipBonus { get; private set; }
        public float CleanDecayMul { get; private set; }
        public int OwnedCount { get { return owned.Count; } }

        public void Rebuild(SaveData data)
        {
            owned.Clear();
            if (data != null && data.upgrades != null)
                for (int i = 0; i < data.upgrades.Count; i++) owned.Add(data.upgrades[i]);
            Recompute();
        }

        void Recompute()
        {
            StoveCount = Catalog.BaseStoves;
            TableCount = Catalog.BaseTables;
            DisplaySlots = Catalog.BaseDisplaySlots;
            StorageBonus = 0;
            CookSpeedMul = 1f;
            SpoilMul = 1f;
            ReputationMul = 1f;
            Comfort = 0f;
            DemandMul = 1f;
            TipBonus = 0f;
            CleanDecayMul = 1f;

            foreach (string id in owned)
            {
                UpgradeDef d = Catalog.Upgrade(id);
                if (d == null) continue;
                StoveCount += d.addStove;
                TableCount += d.addTable;
                DisplaySlots += d.addDisplaySlots;
                StorageBonus += d.addStorage;
                CookSpeedMul *= d.cookSpeedMul;
                SpoilMul *= d.spoilMul;
                ReputationMul *= d.repMul;
                Comfort += d.comfort;
                DemandMul *= d.demandMul;
                TipBonus += d.tipBonus;
                CleanDecayMul *= d.cleanDecayMul;
            }

            // batas wajar
            CookSpeedMul = Mathf.Clamp(CookSpeedMul, 0.4f, 1f);
            SpoilMul = Mathf.Clamp(SpoilMul, 0.2f, 1f);
            TableCount = Mathf.Min(TableCount, 8);
            DisplaySlots = Mathf.Min(DisplaySlots, 28);
            Comfort = Mathf.Min(Comfort, 30f);
        }

        public bool Has(string id) { return owned.Contains(id); }

        public bool IsAvailable(UpgradeDef d)
        {
            if (d == null) return false;
            if (GameManager.I.Level < d.levelReq) return false;
            if (owned.Contains(d.id)) return false;
            return true;
        }

        public bool Purchase(UpgradeDef d)
        {
            if (d == null) return false;
            if (owned.Contains(d.id)) { GameManager.I.Notify("Sudah dimiliki.", 2); return false; }
            if (GameManager.I.Level < d.levelReq)
            {
                GameManager.I.Notify("Butuh Level " + d.levelReq + " untuk membeli ini.", 2);
                return false;
            }
            if (!GameManager.I.CanAfford(d.price))
            {
                GameManager.I.Notify("Uang tidak cukup! Butuh " + Catalog.Money(d.price), 2);
                return false;
            }
            if (!GameManager.I.SpendMoney(d.price, LedgerCategory.Upgrade, "Upgrade: " + d.name)) return false;

            GameManager.I.Data.upgrades.Add(d.id);
            owned.Add(d.id);
            Recompute();

            // terapkan perubahan ke dunia
            GameManager.I.Data.money = GameManager.I.Money;
            if (GameManager.I.Upgrades != null)
            {
                if (GameManager.I.Kitchen != null) GameManager.I.Kitchen.Rebuild(GameManager.I.Data);
                if (GameManager.I.World != null) GameManager.I.World.Rebuild(GameManager.I.Data);
            }
            GameManager.I.Notify("Upgrade terpasang: " + d.name + "!", 1);
            if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.LevelUp, 0.8f);
            if (JuiceFX.I != null) JuiceFX.I.Burst(GameManager.I.World != null ? GameManager.I.World.CameraFocus : Vector3.zero,
                MaterialLibrary.Palette.Gold, 20);
            if (GameManager.I.Hud != null) { GameManager.I.Hud.RefreshAll(); GameManager.I.Hud.RefreshStatus(); }
            GameManager.I.SaveGame(true);
            return true;
        }

        public void RefreshAvailability()
        {
            if (GameManager.I.Management != null) GameManager.I.Management.RefreshOpenTabs();
        }

        // -----------------------------------------------------------------
        public List<UpgradeDef> AvailableUpgrades()
        {
            List<UpgradeDef> list = new List<UpgradeDef>();
            for (int i = 0; i < Catalog.Upgrades.Count; i++)
                if (!owned.Contains(Catalog.Upgrades[i].id)) list.Add(Catalog.Upgrades[i]);
            list.Sort((a, b) =>
            {
                int la = a.levelReq.CompareTo(b.levelReq);
                if (la != 0) return la;
                return a.price.CompareTo(b.price);
            });
            return list;
        }

        public List<UpgradeDef> OwnedUpgrades()
        {
            List<UpgradeDef> list = new List<UpgradeDef>();
            foreach (string id in owned)
            {
                UpgradeDef d = Catalog.Upgrade(id);
                if (d != null) list.Add(d);
            }
            return list;
        }
    }
}
