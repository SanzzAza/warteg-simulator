using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    // =====================================================================
    //  DEFINISI BAHAN MENTAH
    // =====================================================================

    public class IngredientDef
    {
        public string id;
        public string name;
        public int unitPrice;       // Rp per unit
        public string unit;         // satuan
        public string icon;         // emoji untuk UI
        public int packSize = 1;    // 1x klik beli = berapa unit

        public IngredientDef(string id, string name, int unitPrice, string unit, string icon, int packSize = 1)
        {
            this.id = id; this.name = name; this.unitPrice = unitPrice;
            this.unit = unit; this.icon = icon; this.packSize = packSize;
        }
    }

    // =====================================================================
    //  DEFINISI MENU / RESEP
    // =====================================================================

    public enum FoodVisual { Dome, Slab, Pieces, Bowl, Glass, Bottle }

    public class RecipeDef
    {
        public string id;
        public string name;
        public FoodCategory category;
        public int defaultPrice;
        public int unlockLevel;
        public float cookSeconds;
        public int yield;
        public float popularity;    // 0..1
        public FoodVisual visual;
        public Color color;
        public string desc;
        public List<IngredientCost> ingredients = new List<IngredientCost>();

        public class IngredientCost
        {
            public string id;
            public int qty;
            public IngredientCost(string id, int qty) { this.id = id; this.qty = qty; }
        }

        /// <summary>Biaya bahan per porsi (Rp).</summary>
        public int CostPerPortion()
        {
            float total = 0f;
            for (int i = 0; i < ingredients.Count; i++)
            {
                IngredientDef ing = Catalog.Ingredient(ingredients[i].id);
                if (ing != null) total += ing.unitPrice * ingredients[i].qty;
            }
            return Mathf.Max(1, Mathf.RoundToInt(total / Mathf.Max(1, yield)));
        }

        public string IngredientSummary()
        {
            string s = "";
            for (int i = 0; i < ingredients.Count; i++)
            {
                IngredientDef ing = Catalog.Ingredient(ingredients[i].id);
                if (ing == null) continue;
                if (s.Length > 0) s += ", ";
                s += ing.name + " " + ingredients[i].qty + " " + ing.unit;
            }
            return s;
        }
    }

    // =====================================================================
    //  ARKETIPE PELANGGAN
    // =====================================================================

    public class CustomerArchetype
    {
        public CustomerKind kind;
        public string label;
        public string desc;
        public int budget;
        public float patience;        // menit game
        public float walkSpeed;
        public float tipChance;
        public float priceSensitivity; // 0..1 (1 = sangat sensitif)
        public Color shirtColor;
        public Color pantsColor;
        public List<string> preferredFood = new List<string>();
        public int partyMin = 1, partyMax = 1;
    }

    // =====================================================================
    //  DEFINISI UPGRADE
    // =====================================================================

    public class UpgradeDef
    {
        public string id;
        public string name;
        public string desc;
        public int price;
        public int levelReq;
        public string icon;

        // efek
        public int addStove;
        public int addTable;
        public int addDisplaySlots;
        public int addStorage;
        public float cookSpeedMul = 1f;
        public float spoilMul = 1f;
        public float repMul = 1f;
        public float comfort;
        public float demandMul = 1f;
        public float tipBonus;
        public float cleanDecayMul = 1f;
        public bool isExpansion;
        public bool isRestaurant;
    }

    // =====================================================================
    //  KATALOG (data statis game)
    // =====================================================================

    public static class Catalog
    {
        public const int StartMoney = 2000000;
        public const int BaseTables = 1;
        public const int BaseStoves = 1;
        public const int BaseDisplaySlots = 8;
        public const int BaseStorage = 60;
        public const int SeatsPerTable = 2;
        public const float SpoilageHours = 12f;      // jam game sampai makanan basi
        public const float OpenHour = 6f;
        public const float CloseHour = 23f;
        public const int RentPerWeek = 350000;       // sewa tempat dibayar tiap 7 hari

        static List<IngredientDef> ingredients;
        static List<RecipeDef> recipes;
        static List<UpgradeDef> upgrades;
        static List<CustomerArchetype> archetypes;
        static Dictionary<string, IngredientDef> ingMap;
        static Dictionary<string, RecipeDef> recMap;
        static Dictionary<string, UpgradeDef> upMap;
        static Dictionary<CustomerKind, CustomerArchetype> archMap;

        public static readonly string[] FirstNames =
        {
            "Budi", "Siti", "Agus", "Dewi", "Rudi", "Ani", "Joko", "Rina", "Bambang", "Sri",
            "Andi", "Nur", "Hendra", "Maya", "Dedi", "Lestari", "Fajar", "Wati", "Iwan", "Yanti",
            "Rizky", "Putri", "Bayu", "Indah", "Slamet", "Fitri", "Tono", "Ratna", "Yusuf", "Ayu",
            "Galih", "Nia", "Wawan", "Tari", "Arif", "Sari", "Eko", "Dian", "Hadi", "Mega"
        };

        public static readonly string[] LastNames =
        {
            "Santoso", "Pratama", "Wijaya", "Saputra", "Halim", "Kusuma", "Siregar", "Nugroho",
            "Ramadhan", "Hidayat", "Anggraini", "Purnama", "Sutanto", "Wibowo", "Maulana", "Safitri"
        };

        // -----------------------------------------------------------------
        public static void EnsureLoaded()
        {
            if (recipes != null) return;

            // ============ BAHAN MENTAH ============
            ingredients = new List<IngredientDef>
            {
                new IngredientDef("beras",     "Beras",            12000, "kg",     "🍚", 1),
                new IngredientDef("ayam",      "Ayam Potong",      36000, "kg",     "🍗", 1),
                new IngredientDef("telur",     "Telur Ayam",        2500, "butir",  "🥚", 10),
                new IngredientDef("tempe",     "Tempe",             1000, "potong", "🟫", 5),
                new IngredientDef("tahu",      "Tahu",               600, "potong", "⬜", 5),
                new IngredientDef("ikan",      "Ikan Nila",         9000, "ekor",   "🐟", 1),
                new IngredientDef("sayurAsem", "Paket Sayur Asem",  4000, "paket",  "🥬", 1),
                new IngredientDef("kangkung",  "Kangkung",          3000, "ikat",   "🌿", 1),
                new IngredientDef("minyak",    "Minyak Goreng",     1800, "100ml",  "🛢️", 10),
                new IngredientDef("bumbu",     "Bumbu Dasar",       2000, "paket",  "🧄", 2),
                new IngredientDef("santan",    "Santan",            5000, "pack",   "🥥", 1),
                new IngredientDef("teh",       "Teh Celup",          800, "sachet", "🫖", 10),
                new IngredientDef("gula",      "Gula Pasir",        1500, "100gr",  "🍬", 10),
                new IngredientDef("kopi",      "Kopi Sachet",       1500, "sachet", "☕", 10),
                new IngredientDef("jeruk",     "Jeruk Peras",       1200, "buah",   "🍊", 5),
                new IngredientDef("es",        "Es Batu",            500, "porsi",  "🧊", 10),
                new IngredientDef("air",       "Air Mineral",       2500, "botol",  "💧", 6),
                new IngredientDef("gas",       "Gas LPG (1%)",       220, "%",      "🔥", 20),
            };

            // ============ MENU MAKANAN ============
            recipes = new List<RecipeDef>();

            AddRecipe("nasi_putih", "Nasi Putih", FoodCategory.Makanan, 5000, 1, 6f, 8, 0.92f,
                FoodVisual.Dome, new Color(0.98f, 0.96f, 0.90f),
                "Nasi putih hangat, dasar semua hidangan.",
                new string[] { "beras", "gas" }, new int[] { 1, 8 });

            AddRecipe("nasi_uduk", "Nasi Uduk", FoodCategory.Makanan, 7000, 2, 12f, 8, 0.62f,
                FoodVisual.Dome, new Color(0.95f, 0.92f, 0.80f),
                "Nasi gurih dengan santan dan rempah.",
                new string[] { "beras", "santan", "gas" }, new int[] { 1, 1, 6 });

            AddRecipe("ayam_goreng", "Ayam Goreng", FoodCategory.Makanan, 12000, 1, 16f, 6, 0.88f,
                FoodVisual.Pieces, new Color(0.72f, 0.44f, 0.18f),
                "Ayam goreng krispi bumbu kuning.",
                new string[] { "ayam", "minyak", "bumbu", "gas" }, new int[] { 1, 2, 2, 8 });

            AddRecipe("ayam_kecap", "Ayam Kecap", FoodCategory.Makanan, 12000, 2, 18f, 6, 0.70f,
                FoodVisual.Pieces, new Color(0.45f, 0.24f, 0.10f),
                "Ayam masak kecap manis, favorit anak kos.",
                new string[] { "ayam", "bumbu", "gula", "gas" }, new int[] { 1, 2, 2, 8 });

            AddRecipe("telur_balado", "Telur Balado", FoodCategory.Makanan, 7000, 2, 12f, 4, 0.64f,
                FoodVisual.Pieces, new Color(0.85f, 0.28f, 0.18f),
                "Telur rebus digoreng dengan sambal balado.",
                new string[] { "telur", "bumbu", "minyak", "gas" }, new int[] { 4, 2, 1, 6 });

            AddRecipe("telur_dadar", "Telur Dadar", FoodCategory.Makanan, 5000, 1, 8f, 3, 0.80f,
                FoodVisual.Slab, new Color(0.97f, 0.85f, 0.40f),
                "Telur dadar tipis garing, murah dan cepat.",
                new string[] { "telur", "minyak", "gas" }, new int[] { 2, 1, 5 });

            AddRecipe("tempe_goreng", "Tempe Goreng", FoodCategory.Makanan, 3000, 1, 7f, 5, 0.85f,
                FoodVisual.Slab, new Color(0.80f, 0.62f, 0.35f),
                "Tempe goreng garing, lauk sejuta umat.",
                new string[] { "tempe", "minyak", "gas" }, new int[] { 4, 1, 4 });

            AddRecipe("tahu_goreng", "Tahu Goreng", FoodCategory.Makanan, 3000, 1, 7f, 5, 0.82f,
                FoodVisual.Slab, new Color(0.93f, 0.82f, 0.55f),
                "Tahu kuning goreng hangat.",
                new string[] { "tahu", "minyak", "gas" }, new int[] { 4, 1, 4 });

            AddRecipe("ikan_goreng", "Ikan Goreng", FoodCategory.Makanan, 10000, 3, 18f, 2, 0.52f,
                FoodVisual.Pieces, new Color(0.78f, 0.60f, 0.30f),
                "Ikan nila goreng garing dengan sambal.",
                new string[] { "ikan", "minyak", "bumbu", "gas" }, new int[] { 1, 1, 1, 6 });

            AddRecipe("sayur_asem", "Sayur Asem", FoodCategory.Makanan, 5000, 1, 14f, 6, 0.72f,
                FoodVisual.Bowl, new Color(0.75f, 0.55f, 0.25f),
                "Kuah asem segar berisi sayuran.",
                new string[] { "sayurAsem", "bumbu", "gas" }, new int[] { 1, 2, 6 });

            AddRecipe("tumis_kangkung", "Tumis Kangkung", FoodCategory.Makanan, 5000, 1, 9f, 5, 0.78f,
                FoodVisual.Bowl, new Color(0.30f, 0.58f, 0.22f),
                "Kangkung tumis oseng bawang.",
                new string[] { "kangkung", "bumbu", "minyak", "gas" }, new int[] { 1, 1, 1, 5 });

            AddRecipe("orek_tempe", "Orek Tempe", FoodCategory.Makanan, 5000, 2, 11f, 5, 0.68f,
                FoodVisual.Bowl, new Color(0.55f, 0.36f, 0.16f),
                "Tempe iris manis pedas.",
                new string[] { "tempe", "bumbu", "gula", "minyak", "gas" }, new int[] { 4, 2, 2, 1, 5 });

            AddRecipe("sambal", "Sambal", FoodCategory.Makanan, 2000, 1, 5f, 8, 0.90f,
                FoodVisual.Bowl, new Color(0.85f, 0.16f, 0.10f),
                "Sambal terasi pedas, wajib ada.",
                new string[] { "bumbu", "gas" }, new int[] { 2, 2 });

            // ============ MENU MINUMAN ============
            AddRecipe("es_teh", "Es Teh", FoodCategory.Minuman, 4000, 1, 4f, 4, 0.96f,
                FoodVisual.Glass, new Color(0.62f, 0.42f, 0.18f),
                "Es teh manis segar.",
                new string[] { "teh", "gula", "es" }, new int[] { 2, 2, 2 });

            AddRecipe("teh_hangat", "Teh Hangat", FoodCategory.Minuman, 3000, 1, 3f, 4, 0.74f,
                FoodVisual.Glass, new Color(0.70f, 0.50f, 0.24f),
                "Teh hangat manis.",
                new string[] { "teh", "gula" }, new int[] { 2, 2 });

            AddRecipe("es_jeruk", "Es Jeruk", FoodCategory.Minuman, 5000, 1, 5f, 4, 0.80f,
                FoodVisual.Glass, new Color(0.95f, 0.62f, 0.15f),
                "Perasan jeruk segar dengan es.",
                new string[] { "jeruk", "gula", "es" }, new int[] { 2, 2, 2 });

            AddRecipe("kopi_hitam", "Kopi Hitam", FoodCategory.Minuman, 4000, 2, 4f, 4, 0.66f,
                FoodVisual.Glass, new Color(0.20f, 0.13f, 0.09f),
                "Kopi tubruk pahit, teman lembur.",
                new string[] { "kopi", "gula" }, new int[] { 2, 1 });

            AddRecipe("air_mineral", "Air Mineral", FoodCategory.Minuman, 4000, 1, 2f, 1, 0.60f,
                FoodVisual.Bottle, new Color(0.72f, 0.86f, 0.95f),
                "Air mineral botol dingin.",
                new string[] { "air" }, new int[] { 1 });

            // ============ UPGRADE TOKO ============
            upgrades = new List<UpgradeDef>
            {
                new UpgradeDef { id = "kompor_2", name = "Kompor Tambahan", icon = "🔥", price = 1500000, levelReq = 2,
                    desc = "Satu tungku lagi: bisa memasak 2 menu sekaligus.", addStove = 1 },
                new UpgradeDef { id = "kompor_3", name = "Kompor Ketiga", icon = "🔥", price = 3200000, levelReq = 4,
                    desc = "Dapur makin produktif, 3 masakan paralel.", addStove = 1 },
                new UpgradeDef { id = "meja_a", name = "Meja & Kursi Ekstra", icon = "🪑", price = 750000, levelReq = 1,
                    desc = "Menambah 1 meja (2 kursi) untuk pelanggan.", addTable = 1 },
                new UpgradeDef { id = "meja_b", name = "Meja Tambahan II", icon = "🪑", price = 1200000, levelReq = 3,
                    desc = "Menambah 1 meja lagi.", addTable = 1 },
                new UpgradeDef { id = "meja_c", name = "Meja Tambahan III", icon = "🪑", price = 2000000, levelReq = 5,
                    desc = "Menambah 1 meja lagi.", addTable = 1 },
                new UpgradeDef { id = "etalase_a", name = "Etalase Kaca", icon = "🧊", price = 1800000, levelReq = 2,
                    desc = "Kapasitas etalase +6 porsi.", addDisplaySlots = 6 },
                new UpgradeDef { id = "etalase_b", name = "Etalase Besar", icon = "🧊", price = 3000000, levelReq = 4,
                    desc = "Kapasitas etalase +8 porsi.", addDisplaySlots = 8 },
                new UpgradeDef { id = "kulkas", name = "Kulkas", icon = "❄️", price = 2500000, levelReq = 3,
                    desc = "Makanan di etalase basi 2x lebih lambat.", spoilMul = 0.5f },
                new UpgradeDef { id = "peralatan", name = "Peralatan Masak", icon = "🍳", price = 1200000, levelReq = 2,
                    desc = "Wajan & pisau baru: memasak 20% lebih cepat.", cookSpeedMul = 0.8f },
                new UpgradeDef { id = "dapur_modern", name = "Dapur Modern", icon = "⚡", price = 4000000, levelReq = 5,
                    desc = "Rice cooker & kompor listrik: memasak 30% lebih cepat.", cookSpeedMul = 0.7f },
                new UpgradeDef { id = "lampu", name = "Lampu & Ventilasi", icon = "💡", price = 400000, levelReq = 1,
                    desc = "Warteg lebih terang dan nyaman.", comfort = 8f },
                new UpgradeDef { id = "dekorasi", name = "Dekorasi Dinding", icon = "🖼️", price = 800000, levelReq = 2,
                    desc = "Reputasi naik lebih cepat, pelanggan betah.", comfort = 10f, repMul = 1.15f },
                new UpgradeDef { id = "kasir", name = "Mesin Kasir", icon = "🧾", price = 1800000, levelReq = 3,
                    desc = "Pembayaran lebih cepat dan uang kembalian akurat.", tipBonus = 0.08f },
                new UpgradeDef { id = "gudang", name = "Rak Gudang", icon = "📦", price = 900000, levelReq = 2,
                    desc = "Kapasitas gudang bahan +40.", addStorage = 40 },
                new UpgradeDef { id = "wastafel", name = "Wastafel Cuci Tangan", icon = "🚰", price = 600000, levelReq = 1,
                    desc = "Kebersihan turun 25% lebih lambat.", cleanDecayMul = 0.75f },
                new UpgradeDef { id = "banner", name = "Banner & Spanduk", icon = "📣", price = 350000, levelReq = 1,
                    desc = "Pelanggan datang 8% lebih sering.", demandMul = 1.08f },
                new UpgradeDef { id = "perluasan", name = "Perluas Warteg", icon = "🏗️", price = 8000000, levelReq = 6,
                    desc = "Bangunan diperluas: +2 meja, gudang & etalase lebih besar.", addTable = 2, addDisplaySlots = 4, addStorage = 30, isExpansion = true },
                new UpgradeDef { id = "restoran", name = "Naik Kelas ke Restoran", icon = "🏆", price = 25000000, levelReq = 8,
                    desc = "Warteg Anda menjadi restoran besar! Reputasi +10, pelanggan +25%.", demandMul = 1.25f, comfort = 15f, isRestaurant = true },
            };

            // ============ ARKETIPE PELANGGAN ============
            archetypes = new List<CustomerArchetype>
            {
                new CustomerArchetype { kind = CustomerKind.Karyawan, label = "Karyawan Kantoran", desc = "Butuh makan siang cepat, budget lumayan.",
                    budget = 30000, patience = 42f, walkSpeed = 1.5f, tipChance = 0.35f, priceSensitivity = 0.35f,
                    shirtColor = new Color(0.85f, 0.87f, 0.92f), pantsColor = new Color(0.20f, 0.24f, 0.35f),
                    preferredFood = new List<string> { "nasi_putih", "ayam_goreng", "sayur_asem", "tumis_kangkung", "es_teh" } },

                new CustomerArchetype { kind = CustomerKind.AnakSekolah, label = "Anak Sekolah", desc = "Uang jajan terbatas, suka gorengan.",
                    budget = 12000, patience = 32f, walkSpeed = 1.9f, tipChance = 0.08f, priceSensitivity = 0.85f,
                    shirtColor = new Color(0.95f, 0.95f, 0.95f), pantsColor = new Color(0.25f, 0.30f, 0.55f),
                    preferredFood = new List<string> { "nasi_putih", "tempe_goreng", "tahu_goreng", "telur_dadar", "es_teh", "es_jeruk" } },

                new CustomerArchetype { kind = CustomerKind.Mahasiswa, label = "Mahasiswa", desc = "Anak kos hemat, teliti soal harga.",
                    budget = 18000, patience = 38f, walkSpeed = 1.6f, tipChance = 0.15f, priceSensitivity = 0.95f,
                    shirtColor = new Color(0.55f, 0.72f, 0.55f), pantsColor = new Color(0.15f, 0.18f, 0.22f),
                    preferredFood = new List<string> { "nasi_putih", "telur_balado", "orek_tempe", "sayur_asem", "es_teh", "kopi_hitam" } },

                new CustomerArchetype { kind = CustomerKind.Ojol, label = "Pengemudi Ojol", desc = "Sedang buru-buru, tidak sabaran.",
                    budget = 22000, patience = 24f, walkSpeed = 2.2f, tipChance = 0.12f, priceSensitivity = 0.7f,
                    shirtColor = new Color(0.20f, 0.75f, 0.35f), pantsColor = new Color(0.12f, 0.12f, 0.14f),
                    preferredFood = new List<string> { "nasi_putih", "ayam_goreng", "telur_dadar", "es_teh", "air_mineral" } },

                new CustomerArchetype { kind = CustomerKind.Warga, label = "Warga Sekitar", desc = "Langganan setia, sabar dan ramah.",
                    budget = 25000, patience = 55f, walkSpeed = 1.3f, tipChance = 0.45f, priceSensitivity = 0.45f,
                    shirtColor = new Color(0.75f, 0.55f, 0.40f), pantsColor = new Color(0.30f, 0.25f, 0.20f),
                    preferredFood = new List<string> { "nasi_putih", "nasi_uduk", "sayur_asem", "tempe_goreng", "sambal", "teh_hangat" } },

                new CustomerArchetype { kind = CustomerKind.Keluarga, label = "Keluarga", desc = "Bawa anak istri, pesanan banyak.",
                    budget = 65000, patience = 60f, walkSpeed = 1.2f, tipChance = 0.55f, priceSensitivity = 0.3f,
                    shirtColor = new Color(0.45f, 0.60f, 0.85f), pantsColor = new Color(0.18f, 0.20f, 0.28f),
                    preferredFood = new List<string> { "nasi_putih", "ayam_goreng", "ayam_kecap", "ikan_goreng", "tumis_kangkung", "es_jeruk" },
                    partyMin = 2, partyMax = 3 },
            };

            // index
            ingMap = new Dictionary<string, IngredientDef>();
            for (int i = 0; i < ingredients.Count; i++) ingMap[ingredients[i].id] = ingredients[i];

            recMap = new Dictionary<string, RecipeDef>();
            for (int i = 0; i < recipes.Count; i++) recMap[recipes[i].id] = recipes[i];

            upMap = new Dictionary<string, UpgradeDef>();
            for (int i = 0; i < upgrades.Count; i++) upMap[upgrades[i].id] = upgrades[i];

            archMap = new Dictionary<CustomerKind, CustomerArchetype>();
            for (int i = 0; i < archetypes.Count; i++) archMap[archetypes[i].kind] = archetypes[i];
        }

        static void AddRecipe(string id, string name, FoodCategory cat, int price, int lvl, float cook, int yield,
                              float pop, FoodVisual visual, Color color, string desc, string[] ingIds, int[] ingQty)
        {
            RecipeDef r = new RecipeDef();
            r.id = id; r.name = name; r.category = cat; r.defaultPrice = price; r.unlockLevel = lvl;
            r.cookSeconds = cook; r.yield = yield; r.popularity = pop; r.visual = visual;
            r.color = color; r.desc = desc;
            for (int i = 0; i < ingIds.Length; i++) r.ingredients.Add(new RecipeDef.IngredientCost(ingIds[i], ingQty[i]));
            recipes.Add(r);
        }

        // -----------------------------------------------------------------
        public static List<IngredientDef> Ingredients { get { EnsureLoaded(); return ingredients; } }
        public static List<RecipeDef> Recipes { get { EnsureLoaded(); return recipes; } }
        public static List<UpgradeDef> Upgrades { get { EnsureLoaded(); return upgrades; } }
        public static List<CustomerArchetype> Archetypes { get { EnsureLoaded(); return archetypes; } }

        public static IngredientDef Ingredient(string id)
        {
            EnsureLoaded();
            IngredientDef d;
            return ingMap.TryGetValue(id, out d) ? d : null;
        }

        public static RecipeDef Recipe(string id)
        {
            EnsureLoaded();
            RecipeDef d;
            return recMap.TryGetValue(id, out d) ? d : null;
        }

        public static UpgradeDef Upgrade(string id)
        {
            EnsureLoaded();
            UpgradeDef d;
            return upMap.TryGetValue(id, out d) ? d : null;
        }

        public static CustomerArchetype Archetype(CustomerKind k)
        {
            EnsureLoaded();
            CustomerArchetype d;
            return archMap.TryGetValue(k, out d) ? d : archetypes[0];
        }

        // -----------------------------------------------------------------
        public static int LevelXpRequirement(int level)
        {
            return Mathf.RoundToInt(120f * Mathf.Pow(level, 1.35f));
        }

        public static string LevelTitle(int level)
        {
            if (level <= 1) return "Warteg Kecil";
            if (level <= 3) return "Warteg Ramai";
            if (level <= 5) return "Warteg Terkenal";
            if (level <= 7) return "Rumah Makan";
            return "Restoran Besar";
        }

        public static string ReputationLabel(int rep)
        {
            if (rep < 20) return "Sepi";
            if (rep < 40) return "Biasa";
            if (rep < 60) return "Cukup Ramai";
            if (rep < 80) return "Ramai";
            return "Sangat Terkenal";
        }

        public static string PhaseName(float hour)
        {
            if (hour < 10f) return "Pagi";
            if (hour < 15f) return "Siang";
            if (hour < 18f) return "Sore";
            return "Malam";
        }

        public static DayPhase Phase(float hour)
        {
            if (hour < 10f) return DayPhase.Pagi;
            if (hour < 15f) return DayPhase.Siang;
            if (hour < 18f) return DayPhase.Sore;
            return DayPhase.Malam;
        }

        public static string WeatherName(Weather w)
        {
            switch (w)
            {
                case Weather.Cerah: return "Cerah";
                case Weather.Mendung: return "Mendung";
                default: return "Hujan";
            }
        }

        public static string WeatherIcon(Weather w)
        {
            switch (w)
            {
                case Weather.Cerah: return "☀️";
                case Weather.Mendung: return "☁️";
                default: return "🌧️";
            }
        }

        public static string RoleName(StaffRole r)
        {
            switch (r)
            {
                case StaffRole.Koki: return "Koki";
                case StaffRole.Kasir: return "Kasir";
                case StaffRole.Pelayan: return "Pelayan";
                default: return "Petugas Kebersihan";
            }
        }

        public static string CustomerKindLabel(CustomerKind k) { return Archetype(k).label; }

        public static string Money(int amount)
        {
            return "Rp" + amount.ToString("#,##0", System.Globalization.CultureInfo.GetCultureInfo("id-ID"));
        }

        public static string MoneyShort(int amount)
        {
            if (amount >= 1000000) return "Rp" + (amount / 1000000f).ToString("0.#") + "jt";
            if (amount >= 1000) return "Rp" + (amount / 1000f).ToString("0") + "k";
            return "Rp" + amount;
        }
    }
}
