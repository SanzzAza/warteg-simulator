using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Pelanggan NPC dengan AI berbasis state:
    /// masuk → pilih kursi → pesan → menunggu dilayani → makan → bayar → keluar.
    /// </summary>
    public class CustomerAI : InteractableBase
    {
        public enum State { WalkingIn, ToSeat, Ordering, Waiting, Eating, Paying, Leaving, Done }

        public class OrderItem
        {
            public string recipeId;
            public int price;
            public OrderItem(string id, int price) { recipeId = id; this.price = price; }
        }

        public CustomerKind kind;
        public CustomerArchetype arch;
        public CharacterRig rig;
        public State CurrentState { get; private set; }

        public TableUnit table;
        Vector3 seatPos;
        int seatIndex;
        readonly List<OrderItem> order = new List<OrderItem>();

        float patienceLeft;
        float satisfaction = 70f;
        float eatTimer;
        float stateTimer;
        bool paid;
        WorldBar bar;
        Vector3 lastPos;

        public bool WantsService { get { return CurrentState == State.Waiting && order.Count > 0; } }
        public int OrderTotal
        {
            get
            {
                int t = 0;
                for (int i = 0; i < order.Count; i++) t += order[i].price;
                return t;
            }
        }
        public string OrderSummary { get { return BuildOrderSummary(); } }

        string BuildOrderSummary()
        {
            if (order.Count == 0) return "-";
            string s = "";
            for (int i = 0; i < order.Count; i++)
            {
                if (i > 0) s += " + ";
                s += GameManager.I.RecipeName(order[i].recipeId);
            }
            return s;
        }
        public float PatienceLeft01 { get { return patienceLeft / Mathf.Max(0.01f, arch != null ? arch.patience : 40f); } }
        public float Satisfaction { get { return satisfaction; } }

        // =====================================================================
        public void Init(CustomerKind k, Vector3 spawnPos, Transform parent)
        {
            kind = k;
            arch = Catalog.Archetype(k);
            if (parent != null) transform.SetParent(parent, false);
            transform.position = spawnPos;

            label = arch.label;
            radius = 2.2f;
            pointOffset = Vector3.zero;

            string personName = RandomName();
            gameObject.name = "Pelanggan_" + personName;

            rig = CharacterBuilder.Build(transform, "rig", arch.shirtColor, arch.pantsColor,
                k == CustomerKind.AnakSekolah ? 0.82f : 1f);
            rig.transform.localPosition = Vector3.zero;

            CapsuleCollider col = gameObject.AddComponent<CapsuleCollider>();
            col.radius = 0.45f;
            col.height = 1.7f;
            col.center = new Vector3(0f, 0.85f, 0f);

            patienceLeft = arch.patience;
            CurrentState = State.WalkingIn;
            lastPos = transform.position;

            bar = WorldBar.Create(transform, new Vector3(0f, 2.15f, 0f), new Color(0.35f, 0.85f, 0.45f), 190f);
            bar.Show(false);
        }

        static string RandomName()
        {
            string f = Catalog.FirstNames[Random.Range(0, Catalog.FirstNames.Length)];
            string l = Catalog.LastNames[Random.Range(0, Catalog.LastNames.Length)];
            return f + " " + l;
        }

        // =====================================================================
        void Update()
        {
            if (GameManager.I == null || !GameManager.I.IsPlaying) return;
            if (GameManager.I.IsPaused) { if (rig != null) rig.SetMoving(false); return; }

            float dt = Time.deltaTime;
            stateTimer += dt;

            switch (CurrentState)
            {
                case State.WalkingIn: TickWalkingIn(dt); break;
                case State.ToSeat: TickToSeat(dt); break;
                case State.Ordering: TickOrdering(dt); break;
                case State.Waiting: TickWaiting(dt); break;
                case State.Eating: TickEating(dt); break;
                case State.Paying: TickPaying(dt); break;
                case State.Leaving: TickLeaving(dt); break;
            }

            UpdateBar();
        }

        void UpdateBar()
        {
            if (bar == null) return;
            bool show = CurrentState == State.Waiting || CurrentState == State.Ordering;
            bar.Show(show);
            if (!show) return;
            bar.SetValue(PatienceLeft01);
            bar.SetColor(PatienceLeft01 > 0.5f ? new Color(0.35f, 0.85f, 0.45f) :
                         (PatienceLeft01 > 0.22f ? new Color(0.95f, 0.78f, 0.25f) : new Color(0.90f, 0.28f, 0.25f)));
            bar.SetLabel(ShortOrderText());
            if (WorldBuilderRef.cam != null) bar.FaceCamera(WorldBuilderRef.cam);
        }

        string ShortOrderText()
        {
            string s = "";
            for (int i = 0; i < order.Count; i++)
            {
                if (i > 0) s += ", ";
                s += GameManager.I.RecipeName(order[i].recipeId);
            }
            if (s.Length == 0) s = "menunggu...";
            if (s.Length > 34) s = s.Substring(0, 31) + "...";
            return s;
        }

        static WorldBuilder WorldBuilderRef { get { return GameManager.I != null ? GameManager.I.World : null; } }

        // ---------------------------------------------------------------------
        void WalkTo(Vector3 target, float speed, float arriveDist)
        {
            Vector3 pos = transform.position;
            Vector3 dir = target - pos;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist <= arriveDist)
            {
                rig.SetMoving(false);
                return;
            }
            Vector3 step = dir.normalized * Mathf.Min(speed * Time.deltaTime, dist);
            transform.position = pos + step;
            rig.SetMoving(true);
            rig.FaceDirection(dir);
        }

        // ---------------------------------------------------------------------
        void TickWalkingIn(float dt)
        {
            WorldBuilder w = WorldBuilderRef;
            Vector3 doorInside = w != null ? w.DoorInside : new Vector3(0f, 0f, 3f);
            WalkTo(doorInside, arch.walkSpeed, 0.35f);
            if (stateTimer > 0.2f && Vector3.Distance(Flat(transform.position), Flat(doorInside)) < 0.6f)
            {
                table = w != null ? w.ReserveTable() : null;
                if (table == null)
                {
                    // tidak ada kursi kosong: tunggu sebentar lalu pergi kecewa
                    if (stateTimer > 4f)
                    {
                        GameManager.I.Notify("Tidak ada kursi kosong — pelanggan pergi.", 2);
                        BeginLeave(false);
                    }
                    return;
                }
                seatIndex = table.FreeSeatIndex();
                if (seatIndex < 0) { table = null; return; }
                table.Occupy(seatIndex, this);
                Transform sp = table.seatPoints[seatIndex];
                seatPos = sp.position;
                CurrentState = State.ToSeat;
                stateTimer = 0f;
            }
        }

        void TickToSeat(float dt)
        {
            WalkTo(seatPos, arch.walkSpeed, 0.12f);
            if (Vector3.Distance(Flat(transform.position), Flat(seatPos)) < 0.22f)
            {
                transform.position = new Vector3(seatPos.x, 0f, seatPos.z);
                Vector3 toTable = table.transform.position - transform.position;
                rig.FaceInstant(toTable);
                rig.SetSitting(true);
                rig.SetMoving(false);
                CurrentState = State.Ordering;
                stateTimer = 0f;
            }
        }

        void TickOrdering(float dt)
        {
            DrainPatience(dt);
            if (patienceLeft <= 0f) { BeginLeave(false); return; }
            if (stateTimer > 1.4f + Random.Range(0f, 1.2f))
            {
                GenerateOrder();
                if (order.Count == 0)
                {
                    GameManager.I.Notify(arch.label + " tidak sanggup bayar harga menu — pergi.", 2);
                    BeginLeave(false);
                    return;
                }
                CurrentState = State.Waiting;
                stateTimer = 0f;
                if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.Bubble, 0.7f);
                if (GameManager.I.Hud != null) GameManager.I.Hud.RefreshOrderList();
            }
        }

        void TickWaiting(float dt)
        {
            DrainPatience(dt);
            if (patienceLeft <= 0f)
            {
                GameManager.I.Notify(arch.label + " kehabisan sabar dan pergi kecewa!", 2);
                BeginLeave(false);
            }
        }

        void TickEating(float dt)
        {
            eatTimer -= dt;
            if (eatTimer <= 0f)
            {
                CurrentState = State.Paying;
                stateTimer = 0f;
            }
        }

        void TickPaying(float dt)
        {
            if (stateTimer > 0.6f) DoPayment();
        }

        void TickLeaving(float dt)
        {
            WorldBuilder w = WorldBuilderRef;
            Vector3 exit = w != null ? w.DoorOutside : new Vector3(0f, 0f, 8f);
            WalkTo(exit, arch.walkSpeed * 1.05f, 0.3f);
            if (Vector3.Distance(Flat(transform.position), Flat(exit)) < 0.8f || stateTimer > 20f)
            {
                CurrentState = State.Done;
                if (GameManager.I.Spawner != null) GameManager.I.Spawner.Unregister(this);
                Destroy(gameObject);
            }
        }

        void DrainPatience(float dt)
        {
            float gameMinutes = dt * TimeSystem.MinutesPerRealSecond * GameManager.I.Data.settings.timeScale;
            patienceLeft -= gameMinutes;
        }

        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

        // ---------------------------------------------------------------------
        //  Pesanan
        // ---------------------------------------------------------------------
        void GenerateOrder()
        {
            order.Clear();
            int budget = arch.budget;
            int spent = 0;

            // 1. nasi
            string rice = PickRice();
            if (rice != null)
            {
                int p = GameManager.I.PriceOf(rice);
                if (p <= budget)
                {
                    order.Add(new OrderItem(rice, p));
                    spent += p;
                }
            }

            // 2. lauk (1-2, lebih banyak jika budget besar)
            int laukWanted = arch.budget >= 50000 ? 2 : ((float)Random.value < 0.55f ? 2 : 1);
            List<string> lauk = PickDishes(FoodCategory.Makanan, laukWanted + 2, rice);
            for (int i = 0; i < lauk.Count && laukWanted > 0; i++)
            {
                int p = GameManager.I.PriceOf(lauk[i]);
                // pelanggan sensitif harga akan menolak jika kemahalan
                float ratio = GameManager.I.PriceRatio(lauk[i]);
                if (ratio > 1.0f + arch.priceSensitivity * 0.55f) continue;
                if (spent + p > budget) continue;
                order.Add(new OrderItem(lauk[i], p));
                spent += p;
                laukWanted--;
            }

            // 3. minuman
            if (Random.value < 0.62f)
            {
                List<string> drinks = PickDishes(FoodCategory.Minuman, 2, null);
                for (int i = 0; i < drinks.Count; i++)
                {
                    int p = GameManager.I.PriceOf(drinks[i]);
                    float ratio = GameManager.I.PriceRatio(drinks[i]);
                    if (ratio > 1.0f + arch.priceSensitivity * 0.6f) continue;
                    if (spent + p > budget) continue;
                    order.Add(new OrderItem(drinks[i], p));
                    spent += p;
                    break;
                }
            }

            // 4. sambal kadang-kadang
            if (order.Count > 0 && Random.value < 0.35f && GameManager.I.IsUnlocked("sambal"))
            {
                int p = GameManager.I.PriceOf("sambal");
                if (spent + p <= budget) order.Add(new OrderItem("sambal", p));
            }
        }

        string PickRice()
        {
            if (GameManager.I.IsUnlocked("nasi_uduk") && arch.preferredFood.Contains("nasi_uduk") && Random.value < 0.4f)
                return "nasi_uduk";
            if (GameManager.I.IsUnlocked("nasi_putih")) return "nasi_putih";
            return null;
        }

        List<string> PickDishes(FoodCategory cat, int count, string exclude)
        {
            List<string> result = new List<string>();
            List<RecipeDef> pool = new List<RecipeDef>();
            for (int i = 0; i < Catalog.Recipes.Count; i++)
            {
                RecipeDef r = Catalog.Recipes[i];
                if (r.category != cat) continue;
                if (!GameManager.I.IsUnlocked(r.id)) continue;
                if (exclude != null && r.id == exclude) continue;
                if (cat == FoodCategory.Makanan && r.id == "nasi_putih") continue;
                if (cat == FoodCategory.Makanan && r.id == "nasi_uduk") continue;
                pool.Add(r);
            }
            if (pool.Count == 0) return result;

            // berbobot: popularitas + preferensi pribadi
            for (int pick = 0; pick < count && pool.Count > 0; pick++)
            {
                float totalW = 0f;
                for (int i = 0; i < pool.Count; i++)
                {
                    float w = pool[i].popularity;
                    if (arch.preferredFood.Contains(pool[i].id)) w *= 2.4f;
                    totalW += w;
                }
                float roll = Random.value * totalW;
                int chosen = 0;
                for (int i = 0; i < pool.Count; i++)
                {
                    float w = pool[i].popularity;
                    if (arch.preferredFood.Contains(pool[i].id)) w *= 2.4f;
                    roll -= w;
                    if (roll <= 0f) { chosen = i; break; }
                }
                result.Add(pool[chosen].id);
                pool.RemoveAt(chosen);
            }
            return result;
        }

        // ---------------------------------------------------------------------
        //  Pelayanan
        // ---------------------------------------------------------------------
        public bool TryServeFromDisplay()
        {
            if (CurrentState != State.Waiting || order.Count == 0) return false;

            // cek ketersediaan di etalase
            DisplayCase disp = GameManager.I.Display;
            if (disp == null) return false;

            Dictionary<string, int> need = new Dictionary<string, int>();
            for (int i = 0; i < order.Count; i++)
            {
                string id = order[i].recipeId;
                if (need.ContainsKey(id)) need[id]++;
                else need[id] = 1;
            }

            foreach (KeyValuePair<string, int> kv in need)
            {
                if (disp.AvailablePortions(kv.Key) < kv.Value)
                {
                    GameManager.I.Notify("Etalase kurang: " + GameManager.I.RecipeName(kv.Key) + " (butuh " + kv.Value + ")", 2);
                    if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.Error);
                    return false;
                }
            }

            // ambil porsi
            foreach (KeyValuePair<string, int> kv in need) disp.TakePortion(kv.Key, kv.Value);

            ServeNow(disp.AverageFreshness());
            return true;
        }

        void ServeNow(float freshness)
        {
            float timeRatio = Mathf.Clamp01(PatienceLeft01);
            float priceHappy = 1f;
            float sumRatio = 0f;
            for (int i = 0; i < order.Count; i++) sumRatio += GameManager.I.PriceHappiness(order[i].recipeId);
            if (order.Count > 0) priceHappy = sumRatio / order.Count;

            float clean = GameManager.I.Cleanliness;
            float comfort = GameManager.I.Upgrades != null ? GameManager.I.Upgrades.Comfort : 0f;

            satisfaction = 52f
                + timeRatio * 18f                 // cepat dilayani
                + (freshness / 100f) * 16f        // makanan masih segar
                + (priceHappy - 1f) * 26f         // harga sesuai
                + Mathf.Clamp((clean - 55f) / 45f, -1f, 1f) * 9f
                + comfort;
            satisfaction = Mathf.Clamp(satisfaction, 5f, 100f);

            CurrentState = State.Eating;
            stateTimer = 0f;
            eatTimer = 6f + order.Count * 2.2f;

            if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.Serve);
            if (JuiceFX.I != null)
                JuiceFX.I.PopText(transform.position + Vector3.up * 2f, "Disajikan!", new Color(0.85f, 0.95f, 0.7f), 0.9f);
            if (bar != null) bar.Show(false);
            if (GameManager.I.Hud != null) GameManager.I.Hud.RefreshOrderList();
        }

        void DoPayment()
        {
            if (paid) return;
            paid = true;

            int total = OrderTotal;

            // catat penjualan per item
            Dictionary<string, int> count = new Dictionary<string, int>();
            for (int i = 0; i < order.Count; i++)
            {
                string id = order[i].recipeId;
                if (count.ContainsKey(id)) count[id]++;
                else count[id] = 1;
            }
            foreach (KeyValuePair<string, int> kv in count)
            {
                int unit = 0;
                for (int i = 0; i < order.Count; i++) if (order[i].recipeId == kv.Key) unit = order[i].price;
                GameManager.I.RegisterSale(kv.Key, kv.Value, unit * kv.Value, gameObject.name);
            }

            // tip kalau puas
            float tipChance = arch.tipChance + (GameManager.I.Upgrades != null ? GameManager.I.Upgrades.TipBonus : 0f);
            if (satisfaction > 68f && Random.value < tipChance)
            {
                int tip = Mathf.RoundToInt(total * Random.Range(0.05f, 0.13f) / 500f) * 500;
                tip = Mathf.Max(500, tip);
                GameManager.I.AddMoney(tip, LedgerCategory.Penjualan, "Tip dari " + arch.label);
                if (JuiceFX.I != null) JuiceFX.I.PopText(transform.position + Vector3.up * 1.9f, "+" + Catalog.Money(tip) + " tip",
                    MaterialLibrary.Palette.Gold, 1f);
            }

            if (JuiceFX.I != null)
            {
                JuiceFX.I.MoneyPop(transform.position + Vector3.up * 1.5f, total);
            }
            if (GameManager.I.Audio != null) GameManager.I.Audio.PlaySfx(Sfx.Pay);

            GameManager.I.RecordCustomerServed(satisfaction);
            GameManager.I.AddDirt(2.2f);

            CurrentState = State.Leaving;
            stateTimer = 0f;
            if (rig != null) rig.SetSitting(false);
            if (table != null) { table.Vacate(this); table = null; }
            if (GameManager.I.Hud != null) { GameManager.I.Hud.RefreshMoney(); GameManager.I.Hud.RefreshOrderList(); }
        }

        void BeginLeave(bool happy)
        {
            if (CurrentState == State.Done || CurrentState == State.Leaving) return;

            if (table != null) { table.Vacate(this); table = null; }
            if (rig != null) rig.SetSitting(false);

            if (!happy && !paid)
            {
                GameManager.I.RecordCustomerLost();
                if (JuiceFX.I != null)
                    JuiceFX.I.PopText(transform.position + Vector3.up * 2f, "Kecewa...", new Color(0.95f, 0.4f, 0.35f), 1f);
            }

            CurrentState = State.Leaving;
            stateTimer = 0f;
            if (bar != null) bar.Show(false);
            if (GameManager.I.Hud != null) GameManager.I.Hud.RefreshOrderList();
        }

        public override void Interact(PlayerController player)
        {
            if (CurrentState == State.Waiting) { TryServeFromDisplay(); return; }
            if (CurrentState == State.Ordering)
            {
                GameManager.I.Notify(arch.label + " masih memilih menu...", 0);
                return;
            }
            if (CurrentState == State.Eating)
            {
                GameManager.I.Notify(arch.label + " sedang makan.", 0);
            }
        }

        public override string Label
        {
            get
            {
                if (CurrentState == State.Waiting) return "Layani " + arch.label;
                return arch.label;
            }
        }
    }
}
