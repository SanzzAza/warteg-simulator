using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    /// <summary>Mengelola semua tungku kompor + logika "masak apa selanjutnya".</summary>
    public class KitchenManager : MonoBehaviour
    {
        public readonly List<CookingStation> Stations = new List<CookingStation>();
        public Transform root;

        public int StoveCount { get { return Stations.Count; } }
        public int BusyCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Stations.Count; i++) if (Stations[i].Busy) n++;
                return n;
            }
        }

        public void Rebuild(SaveData data)
        {
            int want = GameManager.I.Upgrades != null ? GameManager.I.Upgrades.StoveCount : Catalog.BaseStoves;
            if (root == null) root = transform;

            // hapus kompor berlebih
            for (int i = Stations.Count - 1; i >= want; i--)
            {
                if (Stations[i] != null) Destroy(Stations[i].gameObject);
                Stations.RemoveAt(i);
            }
            // tambah kompor baru
            for (int i = Stations.Count; i < want; i++)
            {
                GameObject go = new GameObject("Kompor_" + (i + 1));
                go.transform.SetParent(root, false);
                CookingStation st = go.AddComponent<CookingStation>();
                st.Setup(i, StovePosition(i));
                Stations.Add(st);
            }
        }

        public static Vector3 StovePosition(int i)
        {
            return new Vector3(-4.5f + i * 1.45f, 0f, -3.9f);
        }

        public CookingStation FreeStation()
        {
            for (int i = 0; i < Stations.Count; i++) if (!Stations[i].Busy) return Stations[i];
            return null;
        }

        /// <summary>Menu yang paling menguntungkan untuk dimasak sekarang (dipakai koki).</summary>
        public RecipeDef PickBestRecipe()
        {
            DisplayCase disp = GameManager.I.Display;
            if (disp == null) return null;

            RecipeDef best = null;
            float bestScore = -1f;
            for (int i = 0; i < Catalog.Recipes.Count; i++)
            {
                RecipeDef r = Catalog.Recipes[i];
                if (!GameManager.I.IsUnlocked(r.id)) continue;
                if (!GameManager.I.HasIngredients(r, 1)) continue;

                int stock = disp.AvailablePortions(r.id);
                float stockRatio = Mathf.Clamp01(stock / (float)Mathf.Max(1, r.yield * 2));
                float priceScore = Mathf.Clamp(GameManager.I.PriceHappiness(r.id), 0.2f, 1.1f);
                float score = r.popularity * priceScore * (1.15f - stockRatio);
                if (r.category == FoodCategory.Makanan) score *= 1.15f;   // lauk lebih prioritas
                if (score > bestScore) { bestScore = score; best = r; }
            }
            return best;
        }

        public bool TryAutoCook()
        {
            CookingStation free = FreeStation();
            if (free == null) return false;
            if (!GameManager.I.Display.HasSpace()) return false;
            RecipeDef r = PickBestRecipe();
            if (r == null) return false;
            return free.StartCook(r, 1, true);
        }
    }
}
