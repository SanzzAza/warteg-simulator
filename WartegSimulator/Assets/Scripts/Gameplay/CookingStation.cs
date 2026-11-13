using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Satu tungku kompor. Memasak 1 jenis menu dalam beberapa batch sekaligus:
    /// bahan diambil dari gudang saat mulai, progress bar berjalan, lalu hasilnya
    /// otomatis masuk ke etalase (atau ditampung jika etalase penuh).
    /// </summary>
    public class CookingStation : InteractableBase
    {
        public int index;
        public bool Busy { get; private set; }
        public string RecipeId { get; private set; }
        public int Batches { get; private set; }
        public float Progress01 { get { return duration > 0f ? Mathf.Clamp01(elapsed / duration) : 0f; } }

        float elapsed;
        float duration;
        int pendingPortions;
        string pendingRecipe;

        Transform potRoot;
        GameObject flame;
        Transform foodVisual;
        WorldBar bar;
        float flicker;

        public void Setup(int idx, Vector3 localPos)
        {
            index = idx;
            label = "Kompor " + (idx + 1);
            radius = 2f;
            pointOffset = new Vector3(0f, 0f, 1.1f);
            transform.localPosition = localPos;
            BuildVisual();
        }

        void BuildVisual()
        {
            // meja kompor
            BuildKit.Box(transform, "base", new Vector3(0f, 0.42f, 0f), new Vector3(1.15f, 0.84f, 0.9f), MaterialLibrary.Palette.DarkMetal);
            BuildKit.Box(transform, "top", new Vector3(0f, 0.87f, 0f), new Vector3(1.2f, 0.07f, 0.95f), MaterialLibrary.Palette.Metal, 0.6f);
            // tungku
            BuildKit.Cyl(transform, "burner", new Vector3(0f, 0.93f, 0f), new Vector3(0.42f, 0.03f, 0.42f), new Color(0.18f, 0.18f, 0.2f));
            // dudukan wajan
            BuildKit.Cyl(transform, "rack", new Vector3(0f, 0.96f, 0f), new Vector3(0.5f, 0.012f, 0.5f), new Color(0.25f, 0.25f, 0.27f));

            potRoot = BuildKit.Empty(transform, "pot", new Vector3(0f, 0.97f, 0f)).transform;
            GameObject pot = BuildKit.Cyl(potRoot, "wajan", new Vector3(0f, 0.13f, 0f), new Vector3(0.62f, 0.13f, 0.62f),
                new Color(0.22f, 0.22f, 0.24f), 0.5f);
            BuildKit.Cyl(potRoot, "handle", new Vector3(0.38f, 0.16f, 0f), new Vector3(0.06f, 0.16f, 0.06f), MaterialLibrary.Palette.WoodDark);
            pot.transform.localRotation = Quaternion.identity;

            // api (emissive) - hanya tampil saat memasak
            flame = BuildKit.Cyl(transform, "flame", new Vector3(0f, 0.95f, 0f), new Vector3(0.32f, 0.06f, 0.32f),
                new Color(1f, 0.55f, 0.1f));
            Renderer fr = flame.GetComponent<Renderer>();
            if (fr != null) fr.sharedMaterial = MaterialLibrary.Emissive(MaterialLibrary.Palette.Flame, 2.2f);
            flame.SetActive(false);

            bar = WorldBar.Create(transform, new Vector3(0f, 1.65f, 0f), new Color(0.95f, 0.65f, 0.15f), 120f);
            bar.Show(false);

            EnsureClickCollider(new Vector3(1.4f, 1.3f, 1.2f), new Vector3(0f, 0.65f, 0f));
        }

        // -----------------------------------------------------------------
        public bool CanStart(RecipeDef recipe, int batches, out string reason)
        {
            reason = "";
            if (Busy) { reason = "Kompor sedang dipakai"; return false; }
            if (recipe == null) { reason = "Menu tidak valid"; return false; }
            if (!GameManager.I.IsUnlocked(recipe.id)) { reason = "Menu belum terbuka (Level " + recipe.unlockLevel + ")"; return false; }
            if (batches < 1) { reason = "Jumlah batch minimal 1"; return false; }
            if (!GameManager.I.HasIngredients(recipe, batches))
            {
                reason = "Bahan kurang: " + GameManager.I.MissingIngredientText(recipe);
                return false;
            }
            if (!GameManager.I.Display.HasSpace())
            {
                reason = "Etalase penuh";
                return false;
            }
            return true;
        }

        public bool StartCook(RecipeDef recipe, int batches, bool byStaff = false)
        {
            string reason;
            if (!CanStart(recipe, batches, out reason))
            {
                if (!byStaff) GameManager.I.Notify(reason, 2);
                return false;
            }
            if (!GameManager.I.ConsumeIngredients(recipe, batches)) return false;

            RecipeId = recipe.id;
            Batches = batches;
            elapsed = 0f;
            float speedMul = GameManager.I.Upgrades != null ? GameManager.I.Upgrades.CookSpeedMul : 1f;
            duration = Mathf.Max(1.5f, recipe.cookSeconds * batches * speedMul);
            Busy = true;

            if (flame != null) flame.SetActive(true);
            if (bar != null)
            {
                bar.Show(true);
                bar.SetLabel(recipe.name + " (" + batches + "x)");
                bar.SetValue(0f);
            }
            if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.Cook);
            GameManager.I.RegisterCooked(recipe.id, recipe.yield * batches);
            if (GameManager.I.Hud != null) GameManager.I.Hud.OnCookingStarted(recipe, batches);
            return true;
        }

        void Update()
        {
            if (!Busy) return;
            if (GameManager.I.IsPaused) return;

            elapsed += Time.deltaTime;
            if (bar != null) bar.SetValue(Progress01);

            // efek api berkedip
            if (flame != null)
            {
                flicker += Time.deltaTime * 18f;
                float s = 1f + Mathf.Sin(flicker) * 0.12f;
                flame.transform.localScale = new Vector3(0.32f * s, 0.06f + Mathf.Abs(Mathf.Sin(flicker * 0.7f)) * 0.05f, 0.32f * s);
            }

            if (elapsed >= duration) Finish();
        }

        void Finish()
        {
            Busy = false;
            if (flame != null) flame.SetActive(false);
            if (bar != null) bar.Show(false);

            RecipeDef recipe = Catalog.Recipe(RecipeId);
            int portions = recipe != null ? recipe.yield * Batches : 0;

            bool stored = false;
            if (recipe != null)
            {
                stored = GameManager.I.Display.AddPortions(recipe.id, portions);
            }

            if (!stored)
            {
                pendingPortions += portions;
                pendingRecipe = RecipeId;
                GameManager.I.Notify("Etalase penuh — " + portions + " porsi menunggu di kompor. Klik kompor untuk ambil.", 2);
            }
            else
            {
                GameManager.I.Notify("Matang! " + (recipe != null ? recipe.name : "") + " +" + portions + " porsi ke etalase.", 1);
            }

            if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.Serve);
            if (JuiceFX.I != null)
            {
                JuiceFX.I.Burst(transform.position + Vector3.up * 1.2f, new Color(1f, 0.8f, 0.3f), 14);
                JuiceFX.I.PopText(transform.position + Vector3.up * 1.6f, "Matang!", new Color(1f, 0.85f, 0.35f), 1.1f);
            }
            if (GameManager.I.Hud != null) GameManager.I.Hud.OnCookingFinished();
            RecipeId = null;
        }

        /// <summary>Ambil hasil masakan yang tertahan karena etalase penuh.</summary>
        public bool CollectPending()
        {
            if (pendingPortions <= 0) return false;
            if (GameManager.I.Display.AddPortions(pendingRecipe, pendingPortions))
            {
                GameManager.I.Notify("Dipindah ke etalase: " + pendingPortions + " porsi.", 1);
                pendingPortions = 0;
                return true;
            }
            GameManager.I.Notify("Etalase masih penuh!", 2);
            return false;
        }

        public int PendingPortions { get { return pendingPortions; } }

        public override void Interact(PlayerController player)
        {
            if (pendingPortions > 0)
            {
                CollectPending();
                return;
            }
            if (GameManager.I.Management != null) GameManager.I.Management.OpenCookPanel(this);
        }

        public void CancelCook()
        {
            Busy = false;
            if (flame != null) flame.SetActive(false);
            if (bar != null) bar.Show(false);
            // bahan hangus sebagian
            GameManager.I.Notify("Masakan dibatalkan, sebagian bahan terbuang.", 2);
        }
    }
}
