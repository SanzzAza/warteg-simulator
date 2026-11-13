using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    /// <summary>Reputasi, level, pengalaman, dan statistik penjualan.</summary>
    public partial class GameManager
    {
        public int Level { get { return Data.level; } }
        public int Reputation { get { return Data.reputation; } }
        public float Xp { get { return Data.xp; } }
        public float XpNeeded { get { return Catalog.LevelXpRequirement(Data.level); } }

        public void AddReputation(float delta)
        {
            float mul = Upgrades != null ? Upgrades.ReputationMul : 1f;
            if (delta > 0f) delta *= mul;
            float before = Data.reputation;
            Data.reputation = Mathf.Clamp(Mathf.RoundToInt(Data.reputation + delta), 0, 100);
            if (Mathf.Abs(before - Data.reputation) >= 1f)
            {
                if (OnProgressChanged != null) OnProgressChanged();
                if (Hud != null) Hud.RefreshStatus();
                if (delta >= 3f) Notify("Reputasi naik! (" + Data.reputation + "/100)", 1);
                else if (delta <= -2f) Notify("Reputasi turun... (" + Data.reputation + "/100)", 2);
            }
        }

        public void AddXp(float amount)
        {
            if (amount <= 0f) return;
            Data.xp += amount;
            bool leveled = false;
            while (Data.xp >= Catalog.LevelXpRequirement(Data.level))
            {
                Data.xp -= Catalog.LevelXpRequirement(Data.level);
                Data.level++;
                leveled = true;
            }
            if (leveled)
            {
                UnlockMenuForLevel();
                if (Upgrades != null) Upgrades.RefreshAvailability();
                Notify("LEVEL UP! Sekarang Level " + Data.level + " — " + Catalog.LevelTitle(Data.level), 1);
                if (Audio != null) Audio.PlaySfx(Sfx.LevelUp);
                if (World != null) World.RefreshSignboard();
            }
            if (OnProgressChanged != null) OnProgressChanged();
            if (Hud != null) Hud.RefreshStatus();
        }

        void UnlockMenuForLevel()
        {
            for (int i = 0; i < Catalog.Recipes.Count; i++)
            {
                RecipeDef r = Catalog.Recipes[i];
                if (r.unlockLevel <= Data.level)
                {
                    MenuEntrySave e = MenuEntry(r.id);
                    if (e != null && !e.unlocked)
                    {
                        e.unlocked = true;
                        Notify("Menu baru terbuka: " + r.name + "!", 1);
                    }
                }
            }
        }

        // =====================================================================
        //  STATISTIK & PENJUALAN
        // =====================================================================
        public void RegisterSale(string recipeId, int qty, int totalPrice, string customerName)
        {
            AddMoney(totalPrice, LedgerCategory.Penjualan, "Jual " + RecipeName(recipeId) + " x" + qty);
            BumpStack(Data.soldToday, recipeId, qty);
            BumpStack(Data.stats.soldPerItem, recipeId, qty);
            if (Hud != null) Hud.RefreshAll();
            if (Data != null) dirtySave = true;
        }

        public void RegisterCooked(string recipeId, int qty)
        {
            BumpStack(Data.stats.cookedPerItem, recipeId, qty);
        }

        public void RecordCustomerServed(float satisfaction)
        {
            Data.dayCustomersServed++;
            Data.stats.totalCustomers++;
            Data.daySatisfactionSum += satisfaction;
            Data.daySatisfactionCount++;

            // reputasi & xp dari kepuasan pelanggan
            float repDelta = (satisfaction - 45f) / 45f * 1.7f;
            AddReputation(repDelta);
            AddXp(6f + satisfaction * 0.09f);
            if (Hud != null) Hud.RefreshStatus();
        }

        public void RecordCustomerLost()
        {
            Data.dayCustomersLost++;
            Data.stats.totalLost++;
            AddReputation(-1.8f);
            if (Hud != null) Hud.RefreshStatus();
        }

        public void RecordSpoiled(int qty)
        {
            Data.stats.totalSpoiled += qty;
        }

        static void BumpStack(List<IngredientStack> list, string id, int qty)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].id == id) { list[i].qty += qty; return; }
            list.Add(new IngredientStack(id, qty));
        }

        public string RecipeName(string id)
        {
            RecipeDef r = Catalog.Recipe(id);
            return r != null ? r.name : id;
        }

        // =====================================================================
        //  Harga & permintaan
        // =====================================================================
        /// <summary>Rasio harga pemain terhadap harga wajar (1 = wajar).</summary>
        public float PriceRatio(string recipeId)
        {
            RecipeDef r = Catalog.Recipe(recipeId);
            if (r == null) return 1f;
            return PriceOf(recipeId) / (float)r.defaultPrice;
        }

        /// <summary>Pengaruh reputasi + level + cuaca terhadap jumlah pelanggan (0.2 .. 2.2).</summary>
        public float DemandFactor()
        {
            float rep = Data.reputation / 100f;
            float lvl = 1f + (Data.level - 1) * 0.09f;
            float weather = 1f;
            if (Time != null)
            {
                if (Time.Weather == Weather.Hujan) weather = 0.6f;
                else if (Time.Weather == Weather.Mendung) weather = 0.88f;
            }
            float clean = 0.75f + Mathf.Clamp01(Data.cleanliness / 100f) * 0.35f;
            float up = Upgrades != null ? Upgrades.DemandMul : 1f;
            return Mathf.Clamp((0.35f + rep * 1.3f) * lvl * weather * clean * up, 0.15f, 2.4f);
        }

        /// <summary>Pengali harga wajar; pelanggan enggan jika harga kemahalan.</summary>
        public float PriceHappiness(string recipeId)
        {
            float ratio = PriceRatio(recipeId);
            if (ratio <= 0.75f) return 1.05f;          // murah: senang
            if (ratio <= 1.0f) return 1.0f;
            if (ratio <= 1.25f) return 0.85f;
            if (ratio <= 1.5f) return 0.62f;
            return 0.35f;
        }
    }
}
