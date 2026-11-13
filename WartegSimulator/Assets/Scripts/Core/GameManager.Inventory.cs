using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    /// <summary>Gudang bahan mentah + pembelian ke pasar.</summary>
    public partial class GameManager
    {
        public int Qty(string ingredientId)
        {
            for (int i = 0; i < Data.storage.Count; i++)
                if (Data.storage[i].id == ingredientId) return Data.storage[i].qty;
            return 0;
        }

        public int StorageCap
        {
            get { return Catalog.BaseStorage + (Upgrades != null ? Upgrades.StorageBonus : 0); }
        }

        public int StorageUsed
        {
            get
            {
                int used = 0;
                for (int i = 0; i < Data.storage.Count; i++) used += Data.storage[i].qty;
                return used;
            }
        }

        public bool AddIngredient(string id, int qty)
        {
            if (qty <= 0) return false;
            if (StorageUsed + qty > StorageCap)
            {
                Notify("Gudang penuh! (kapasitas " + StorageCap + ")", 2);
                return false;
            }
            for (int i = 0; i < Data.storage.Count; i++)
            {
                if (Data.storage[i].id == id) { Data.storage[i].qty += qty; OnInventoryChangedInternal(); return true; }
            }
            Data.storage.Add(new IngredientStack(id, qty));
            OnInventoryChangedInternal();
            return true;
        }

        public bool TakeIngredient(string id, int qty)
        {
            for (int i = 0; i < Data.storage.Count; i++)
            {
                if (Data.storage[i].id != id) continue;
                if (Data.storage[i].qty < qty) return false;
                Data.storage[i].qty -= qty;
                if (Data.storage[i].qty <= 0) Data.storage.RemoveAt(i);
                OnInventoryChangedInternal();
                return true;
            }
            return false;
        }

        void OnInventoryChangedInternal()
        {
            if (OnInventoryChanged != null) OnInventoryChanged();
        }

        public bool HasIngredients(RecipeDef r, int batches)
        {
            if (r == null || batches <= 0) return false;
            for (int i = 0; i < r.ingredients.Count; i++)
            {
                if (Qty(r.ingredients[i].id) < r.ingredients[i].qty * batches) return false;
            }
            return true;
        }

        public bool ConsumeIngredients(RecipeDef r, int batches)
        {
            if (!HasIngredients(r, batches)) return false;
            for (int i = 0; i < r.ingredients.Count; i++)
                TakeIngredient(r.ingredients[i].id, r.ingredients[i].qty * batches);
            return true;
        }

        /// <summary>Berapa batch yang bisa dimasak dari stok sekarang.</summary>
        public int MaxBatches(RecipeDef r)
        {
            if (r == null) return 0;
            int max = 999;
            for (int i = 0; i < r.ingredients.Count; i++)
            {
                int have = Qty(r.ingredients[i].id);
                max = Mathf.Min(max, have / Mathf.Max(1, r.ingredients[i].qty));
            }
            return Mathf.Max(0, max);
        }

        public string MissingIngredientText(RecipeDef r)
        {
            string missing = "";
            for (int i = 0; i < r.ingredients.Count; i++)
            {
                IngredientDef def = Catalog.Ingredient(r.ingredients[i].id);
                if (Qty(r.ingredients[i].id) < r.ingredients[i].qty)
                {
                    if (missing.Length > 0) missing += ", ";
                    missing += (def != null ? def.name : r.ingredients[i].id);
                }
            }
            return missing.Length == 0 ? "-" : missing;
        }

        /// <summary>Beli bahan dari pasar. packs = jumlah paket (lihat IngredientDef.packSize).</summary>
        public bool BuyIngredient(string ingredientId, int packs)
        {
            IngredientDef def = Catalog.Ingredient(ingredientId);
            if (def == null || packs <= 0) return false;
            int qty = def.packSize * packs;
            if (StorageUsed + qty > StorageCap)
            {
                int space = StorageCap - StorageUsed;
                if (space <= 0) { Notify("Gudang penuh!", 2); return false; }
                packs = space / def.packSize;
                if (packs <= 0) { Notify("Gudang tidak cukup untuk " + def.name, 2); return false; }
                qty = def.packSize * packs;
            }
            int cost = def.unitPrice * qty;
            if (!SpendMoney(cost, LedgerCategory.Belanja, "Beli " + def.name + " " + qty + " " + def.unit)) return false;
            AddIngredient(ingredientId, qty);
            if (Audio != null) Audio.PlaySfx(Sfx.Coin);
            Notify("+" + qty + " " + def.unit + " " + def.name, 1);
            if (Hud != null) Hud.RefreshAll();
            return true;
        }

        public int TotalIngredientValue()
        {
            int total = 0;
            for (int i = 0; i < Data.storage.Count; i++)
            {
                IngredientDef def = Catalog.Ingredient(Data.storage[i].id);
                if (def != null) total += def.unitPrice * Data.storage[i].qty;
            }
            return total;
        }
    }
}
