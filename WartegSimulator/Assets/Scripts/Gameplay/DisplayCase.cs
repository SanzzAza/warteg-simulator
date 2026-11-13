using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Etalase kaca tempat makanan siap jual dipajang.
    /// Setiap tumpukan punya jumlah porsi dan tingkat kesegaran (0-100).
    /// Kesegaran turun seiring jam game; jika 0 porsi menjadi basi dan tidak bisa dijual.
    /// </summary>
    public class DisplayCase : InteractableBase
    {
        public List<DisplaySlotSave> Slots { get { return GameManager.I.Data.display; } }

        Transform plateRoot;
        readonly List<GameObject> plateVisuals = new List<GameObject>();
        public int Capacity { get { return GameManager.I.Upgrades != null ? GameManager.I.Upgrades.DisplaySlots : Catalog.BaseDisplaySlots; } }

        public void Setup()
        {
            label = "Etalase Makanan";
            radius = 2.2f;
            pointOffset = new Vector3(0f, 0f, 1.2f);
            EnsureClickCollider(new Vector3(3.4f, 1.4f, 1.0f), new Vector3(0f, 0.8f, 0f));
            plateRoot = transform.Find("plates");
            if (plateRoot == null) plateRoot = BuildKit.Empty(transform, "plates", new Vector3(0f, 0.95f, 0f)).transform;
            RebuildVisual();
        }

        public int TotalPortions
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Slots.Count; i++) if (!IsSpoiled(Slots[i])) total += Slots[i].portions;
                return total;
            }
        }

        public int UsedSlots { get { return Slots.Count; } }

        public bool IsSpoiled(DisplaySlotSave s) { return s.freshness <= 0.01f; }

        public bool HasSpace(int slots = 1) { return Slots.Count + slots <= Capacity; }

        public void TickSpoilage(float gameHours)
        {
            if (gameHours <= 0f) return;
            float spoilMul = GameManager.I.Upgrades != null ? GameManager.I.Upgrades.SpoilMul : 1f;
            float decay = (100f / Catalog.SpoilageHours) * gameHours * spoilMul;
            bool changed = false;
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].freshness <= 0f) continue;
                float before = Slots[i].freshness;
                Slots[i].freshness = Mathf.Max(0f, Slots[i].freshness - decay);
                if (before > 0f && Slots[i].freshness <= 0f)
                {
                    changed = true;
                    GameManager.I.RecordSpoiled(Slots[i].portions);
                    GameManager.I.Notify(GameManager.I.RecipeName(Slots[i].foodId) + " basi! Buang di etalase.", 2);
                    if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.Spoil);
                }
            }
            if (changed) RebuildVisual();
            if (HudNeedsFreshness) RefreshFreshnessVisual();
        }

        bool HudNeedsFreshness { get { return Time.frameCount % 20 == 0; } }

        void RefreshFreshnessVisual()
        {
            if (plateRoot == null) return;
            for (int i = 0; i < plateVisuals.Count && i < Slots.Count; i++)
            {
                if (plateVisuals[i] == null) continue;
                Renderer[] rs = plateVisuals[i].GetComponentsInChildren<Renderer>();
                Color tint = IsSpoiled(Slots[i]) ? new Color(0.35f, 0.38f, 0.25f) : FreshTint(Slots[i].freshness);
                for (int k = 0; k < rs.Length; k++)
                {
                    if (rs[k].name.StartsWith("food")) rs[k].sharedMaterial = MaterialLibrary.Lit(tint);
                }
            }
        }

        static Color FreshTint(float freshness)
        {
            RecipeDef r = null;
            return new Color(1f, 1f, 1f);
        }

        // -----------------------------------------------------------------
        public bool AddPortions(string recipeId, int portions, float freshness = 100f)
        {
            if (portions <= 0) return false;
            RecipeDef def = Catalog.Recipe(recipeId);
            if (def == null) return false;

            // gabung dengan tumpukan sejenis yang masih segar
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].foodId == recipeId && !IsSpoiled(Slots[i]))
                {
                    Slots[i].portions += portions;
                    Slots[i].freshness = Mathf.Min(100f, Mathf.Max(Slots[i].freshness, freshness));
                    Slots[i].cookHour = GameManager.I.Time.Hour;
                    RebuildVisual();
                    return true;
                }
            }

            if (!HasSpace())
            {
                GameManager.I.Notify("Etalase penuh! Perbesar etalase atau buang makanan basi.", 2);
                return false;
            }

            DisplaySlotSave s = new DisplaySlotSave();
            s.foodId = recipeId;
            s.portions = portions;
            s.freshness = freshness;
            s.cookHour = GameManager.I.Time.Hour;
            Slots.Add(s);
            RebuildVisual();
            GameManager.I.NotifyDisplayChanged();
            return true;
        }

        public int AvailablePortions(string recipeId)
        {
            for (int i = 0; i < Slots.Count; i++)
                if (Slots[i].foodId == recipeId && !IsSpoiled(Slots[i])) return Slots[i].portions;
            return 0;
        }

        public bool HasItem(string recipeId) { return AvailablePortions(recipeId) > 0; }

        public DisplaySlotSave FindStack(string recipeId)
        {
            for (int i = 0; i < Slots.Count; i++)
                if (Slots[i].foodId == recipeId && !IsSpoiled(Slots[i])) return Slots[i];
            return null;
        }

        public bool TakePortion(string recipeId, int count = 1)
        {
            DisplaySlotSave s = FindStack(recipeId);
            if (s == null || s.portions < count) return false;
            s.portions -= count;
            if (s.portions <= 0) Slots.Remove(s);
            RebuildVisual();
            GameManager.I.NotifyDisplayChanged();
            return true;
        }

        /// <summary>Buang semua makanan basi. Mengembalikan jumlah porsi yang dibuang.</summary>
        public int DiscardSpoiled()
        {
            int removed = 0;
            for (int i = Slots.Count - 1; i >= 0; i--)
            {
                if (IsSpoiled(Slots[i]))
                {
                    removed += Slots[i].portions;
                    Slots.RemoveAt(i);
                }
            }
            if (removed > 0)
            {
                RebuildVisual();
                GameManager.I.CleanWarteg(2f);
                if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.Trash);
            }
            return removed;
        }

        public float AverageFreshness()
        {
            float sum = 0f; int n = 0;
            for (int i = 0; i < Slots.Count; i++)
            {
                if (Slots[i].portions <= 0) continue;
                sum += Slots[i].freshness; n++;
            }
            return n > 0 ? sum / n : 100f;
        }

        // -----------------------------------------------------------------
        public void Rebuild(SaveData data)
        {
            if (plateRoot == null)
            {
                plateRoot = transform.Find("plates");
                if (plateRoot == null) plateRoot = BuildKit.Empty(transform, "plates", new Vector3(0f, 0.95f, 0f)).transform;
            }
            RebuildVisual();
        }

        void RebuildVisual()
        {
            if (plateRoot == null) return;
            for (int i = 0; i < plateVisuals.Count; i++) if (plateVisuals[i] != null) Destroy(plateVisuals[i]);
            plateVisuals.Clear();

            int maxShown = Mathf.Min(Slots.Count, Capacity);
            for (int i = 0; i < maxShown; i++)
            {
                DisplaySlotSave s = Slots[i];
                RecipeDef def = Catalog.Recipe(s.foodId);
                if (def == null) continue;

                float x = -1.5f + (i % 8) * 0.42f;
                float y = 0f + (i / 8) * 0.30f;
                GameObject holder = new GameObject("plate_" + s.foodId);
                holder.transform.SetParent(plateRoot, false);
                holder.transform.localPosition = new Vector3(x, y, 0f);

                bool spoiled = IsSpoiled(s);
                // piring
                BuildKit.Cyl(holder.transform, "dish", new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.02f, 0.34f),
                    spoiled ? new Color(0.45f, 0.44f, 0.38f) : new Color(0.95f, 0.95f, 0.93f));
                // makanan
                Color c = spoiled ? new Color(0.32f, 0.36f, 0.22f) : def.color;
                GameObject food = FoodModel.Build(holder.transform, "food", def.visual, c, 0.22f);
                // label porsi
                BuildKit.WorldText(holder.transform, "qty", "x" + s.portions, new Vector3(0f, 0.16f, -0.02f), 0.05f,
                    spoiled ? new Color(0.9f, 0.4f, 0.3f) : Color.white);
                plateVisuals.Add(holder);
            }
        }

        public override void Interact(PlayerController player)
        {
            if (GameManager.I.Management != null) GameManager.I.Management.OpenTab(TabId.Inventaris);
        }
    }

    /// <summary>Model makanan sederhana sesuai jenis hidangan.</summary>
    public static class FoodModel
    {
        public static GameObject Build(Transform parent, string name, FoodVisual visual, Color color, float size)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            switch (visual)
            {
                case FoodVisual.Dome:
                    BuildKit.Sphere(root.transform, "food_dome", new Vector3(0f, size * 0.35f, 0f),
                        new Vector3(size, size * 0.55f, size), color);
                    break;
                case FoodVisual.Slab:
                    BuildKit.Box(root.transform, "food_slab", new Vector3(0f, size * 0.18f, 0f),
                        new Vector3(size, size * 0.28f, size * 0.8f), color);
                    break;
                case FoodVisual.Pieces:
                    for (int i = 0; i < 3; i++)
                        BuildKit.Box(root.transform, "food_piece" + i,
                            new Vector3((i - 1) * size * 0.42f, size * 0.22f, (i % 2) * size * 0.18f),
                            new Vector3(size * 0.42f, size * 0.34f, size * 0.42f), color);
                    break;
                case FoodVisual.Bowl:
                    BuildKit.Cyl(root.transform, "food_bowl", new Vector3(0f, size * 0.22f, 0f),
                        new Vector3(size, size * 0.30f, size), color);
                    break;
                case FoodVisual.Glass:
                    BuildKit.Cyl(root.transform, "food_glass", new Vector3(0f, size * 0.40f, 0f),
                        new Vector3(size * 0.62f, size * 0.45f, size * 0.62f), color, 0.75f);
                    break;
                case FoodVisual.Bottle:
                    BuildKit.Cyl(root.transform, "food_bottle", new Vector3(0f, size * 0.45f, 0f),
                        new Vector3(size * 0.48f, size * 0.5f, size * 0.48f), color, 0.8f);
                    BuildKit.Cyl(root.transform, "food_cap", new Vector3(0f, size * 1.0f, 0f),
                        new Vector3(size * 0.22f, size * 0.1f, size * 0.22f), new Color(0.2f, 0.4f, 0.8f));
                    break;
            }
            return root;
        }
    }
}
