using System.Collections.Generic;
using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Membangun seluruh lingkungan warteg secara prosedural: lantai, dinding,
    /// atap, kanopi, dapur, etalase, meja, dekorasi, dan jalan di luar.
    /// Semua memakai primitive + material stylized (tanpa asset eksternal).
    /// </summary>
    public class WorldBuilder : MonoBehaviour
    {
        public Camera cam;
        public Light sun;
        public Transform envRoot;
        public Transform furnitureRoot;
        public Transform tableRoot;
        public Transform kitchenRoot;

        public KitchenManager Kitchen;
        public DisplayCase Etalase;
        public readonly List<TableUnit> Tables = new List<TableUnit>();
        public RainFX Rain;

        public Transform sinkT, cashierT, storageT, marketT;

        readonly List<Light> interiorLights = new List<Light>();
        readonly List<Renderer> lampBulbs = new List<Renderer>();
        TextMesh signText;
        GameObject fridgeObj, bannerObj;
        bool expanded;

        float halfW = 6f, backZ = -5f, frontZ = 5f;
        const float WallH = 3.3f;

        // posisi meja (meter) — 6 meja dasar + 2 untuk area perluasan
        static readonly Vector3[] TableSlots =
        {
            new Vector3(-4.0f, 0f, 0.2f),
            new Vector3(-1.4f, 0f, 0.2f),
            new Vector3(1.4f, 0f, 0.2f),
            new Vector3(4.0f, 0f, 0.2f),
            new Vector3(-4.0f, 0f, 2.8f),
            new Vector3(4.0f, 0f, 2.8f),
            new Vector3(-2.8f, 0f, 5.4f),
            new Vector3(2.8f, 0f, 5.4f),
        };

        public Vector3 DoorOutside { get { return new Vector3(0f, 0f, frontZ + 1.6f); } }
        public Vector3 DoorInside { get { return new Vector3(0f, 0f, frontZ - 1.4f); } }
        public Vector3 PlayerSpawnPoint { get { return new Vector3(-1.6f, 0f, frontZ - 2.8f); } }
        public Vector3 CameraFocus { get { return new Vector3(0f, 1.2f, 0f); } }

        // =====================================================================
        public void BuildWorld(Camera camera, Light sunLight)
        {
            cam = camera;
            sun = sunLight;

            if (envRoot == null) envRoot = BuildKit.Empty(transform, "Environment", Vector3.zero).transform;
            if (furnitureRoot == null) furnitureRoot = BuildKit.Empty(transform, "Furniture", Vector3.zero).transform;
            if (tableRoot == null) tableRoot = BuildKit.Empty(transform, "Tables", Vector3.zero).transform;
            if (kitchenRoot == null) kitchenRoot = BuildKit.Empty(transform, "Kitchen", Vector3.zero).transform;

            ClearChildren(envRoot);
            ClearChildren(furnitureRoot);
            BuildShell();
            BuildOutdoor();
            BuildEtalase();
            BuildStorageRack();
            BuildSink();
            BuildCashier();
            BuildDecor();
            BuildRain();
        }

        static void ClearChildren(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--) Destroy(t.GetChild(i).gameObject);
        }

        // ---------------------------------------------------------------------
        //  Struktur bangunan
        // ---------------------------------------------------------------------
        void BuildShell()
        {
            expanded = GameManager.I != null && GameManager.I.Upgrades != null && GameManager.I.Upgrades.Has("perluasan");
            halfW = expanded ? 7.5f : 6f;
            frontZ = expanded ? 7f : 5f;
            backZ = -5f;

            float depth = frontZ - backZ;

            // ---- lantai keramik kotak-kotak ----
            GameObject floorRoot = BuildKit.Empty(envRoot, "Floor", Vector3.zero);
            float tile = 2f;
            int cols = Mathf.CeilToInt(halfW * 2f / tile);
            int rows = Mathf.CeilToInt(depth / tile);
            for (int x = 0; x < cols; x++)
            {
                for (int z = 0; z < rows; z++)
                {
                    bool alt = (x + z) % 2 == 0;
                    Color c = alt ? MaterialLibrary.Palette.Floor : MaterialLibrary.Palette.FloorTile;
                    Vector3 pos = new Vector3(-halfW + tile * (x + 0.5f), 0.01f, backZ + tile * (z + 0.5f));
                    BuildKit.Box(floorRoot.transform, "tile", pos, new Vector3(tile - 0.04f, 0.06f, tile - 0.04f), c, 0.28f);
                }
            }
            // collider lantai (untuk klik ke tanah)
            GameObject floorCol = BuildKit.Box(envRoot, "FloorCollider", new Vector3(0f, -0.06f, (frontZ + backZ) * 0.5f),
                new Vector3(halfW * 2f, 0.1f, depth), MaterialLibrary.Palette.FloorTile, 0.2f, true);
            floorCol.GetComponent<Renderer>().enabled = false;

            // ---- dinding ----
            Color wall = MaterialLibrary.Palette.Wall;
            Color wain = new Color(0.78f, 0.62f, 0.46f);
            Color frame = MaterialLibrary.Palette.WoodDark;

            // dinding belakang
            BuildKit.Box(envRoot, "wallBack", new Vector3(0f, WallH * 0.5f, backZ - 0.1f), new Vector3(halfW * 2f + 0.4f, WallH, 0.2f), wall);
            BuildKit.Box(envRoot, "wainBack", new Vector3(0f, 0.55f, backZ + 0.02f), new Vector3(halfW * 2f, 1.1f, 0.06f), wain);
            // dinding kiri & kanan
            BuildKit.Box(envRoot, "wallLeft", new Vector3(-halfW - 0.1f, WallH * 0.5f, (frontZ + backZ) * 0.5f),
                new Vector3(0.2f, WallH, depth + 0.4f), wall);
            BuildKit.Box(envRoot, "wainLeft", new Vector3(-halfW + 0.02f, 0.55f, (frontZ + backZ) * 0.5f),
                new Vector3(0.06f, 1.1f, depth), wain);
            BuildKit.Box(envRoot, "wallRight", new Vector3(halfW + 0.1f, WallH * 0.5f, (frontZ + backZ) * 0.5f),
                new Vector3(0.2f, WallH, depth + 0.4f), wall);
            BuildKit.Box(envRoot, "wainRight", new Vector3(halfW - 0.02f, 0.55f, (frontZ + backZ) * 0.5f),
                new Vector3(0.06f, 1.1f, depth), wain);

            // dinding depan dengan pintu di tengah
            float doorHalf = 1.3f;
            float segW = halfW - doorHalf;
            BuildKit.Box(envRoot, "wallFrontL", new Vector3(-(doorHalf + segW * 0.5f), WallH * 0.5f, frontZ + 0.1f),
                new Vector3(segW, WallH, 0.2f), wall);
            BuildKit.Box(envRoot, "wallFrontR", new Vector3(doorHalf + segW * 0.5f, WallH * 0.5f, frontZ + 0.1f),
                new Vector3(segW, WallH, 0.2f), wall);
            BuildKit.Box(envRoot, "wallFrontTop", new Vector3(0f, WallH - 0.45f, frontZ + 0.1f), new Vector3(doorHalf * 2f, 0.9f, 0.2f), wall);

            // kusen pintu
            BuildKit.Box(envRoot, "doorFrameL", new Vector3(-doorHalf, 1.15f, frontZ + 0.1f), new Vector3(0.14f, 2.3f, 0.3f), frame);
            BuildKit.Box(envRoot, "doorFrameR", new Vector3(doorHalf, 1.15f, frontZ + 0.1f), new Vector3(0.14f, 2.3f, 0.3f), frame);
            BuildKit.Box(envRoot, "doorFrameTop", new Vector3(0f, 2.28f, frontZ + 0.1f), new Vector3(doorHalf * 2f, 0.14f, 0.3f), frame);

            // jendela kiri & kanan depan
            BuildWindow(new Vector3(-halfW * 0.55f, 1.7f, frontZ + 0.02f), 2.2f);
            BuildWindow(new Vector3(halfW * 0.55f, 1.7f, frontZ + 0.02f), 2.2f);
            BuildWindow(new Vector3(-halfW + 0.06f, 1.8f, 2.0f), 2.4f, true);
            BuildWindow(new Vector3(halfW - 0.06f, 1.8f, 2.0f), 2.4f, true);

            // ---- atap ----
            BuildKit.Box(envRoot, "roof", new Vector3(0f, WallH + 0.1f, (frontZ + backZ) * 0.5f),
                new Vector3(halfW * 2f + 0.8f, 0.22f, depth + 0.8f), new Color(0.62f, 0.28f, 0.22f));
            BuildKit.Box(envRoot, "roofTrim", new Vector3(0f, WallH - 0.02f, (frontZ + backZ) * 0.5f),
                new Vector3(halfW * 2f + 0.9f, 0.12f, depth + 0.9f), new Color(0.45f, 0.20f, 0.16f));

            // ---- kanopi bergaris merah putih di depan ----
            int stripes = 10;
            for (int i = 0; i < stripes; i++)
            {
                float w = (halfW * 2f + 1.6f) / stripes;
                float x = -halfW - 0.8f + w * (i + 0.5f);
                Color c = i % 2 == 0 ? MaterialLibrary.Palette.Awning : MaterialLibrary.Palette.Awning2;
                GameObject s = BuildKit.Box(envRoot, "awning", new Vector3(x, 2.85f, frontZ + 0.75f),
                    new Vector3(w, 0.1f, 1.6f), c, 0.25f);
                s.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
            }
            // tiang kanopi
            BuildKit.Cyl(envRoot, "awningPoleL", new Vector3(-halfW * 0.7f, 1.4f, frontZ + 1.4f), new Vector3(0.08f, 1.4f, 0.08f), frame);
            BuildKit.Cyl(envRoot, "awningPoleR", new Vector3(halfW * 0.7f, 1.4f, frontZ + 1.4f), new Vector3(0.08f, 1.4f, 0.08f), frame);

            // ---- papan nama ----
            GameObject signBoard = BuildKit.Box(envRoot, "signBoard", new Vector3(0f, WallH - 0.05f, frontZ + 0.28f),
                new Vector3(5.2f, 1.0f, 0.14f), new Color(0.72f, 0.16f, 0.14f), 0.3f);
            signText = BuildKit.WorldText(signBoard.transform, "signText", "WARTEG BERKAH", Vector3.zero, 0.11f, Color.white);
            signText.transform.localRotation = Quaternion.identity;
            signText.transform.localPosition = new Vector3(0f, 0f, 0.12f);

            BuildInteriorLights();
        }

        void BuildWindow(Vector3 pos, float width, bool side = false)
        {
            Color frame = MaterialLibrary.Palette.WoodDark;
            Color glass = new Color(0.70f, 0.84f, 0.95f);
            float rot = side ? 90f : 0f;
            GameObject root = BuildKit.Empty(envRoot, "window", pos);
            root.transform.localRotation = Quaternion.Euler(0f, rot, 0f);
            BuildKit.Box(root.transform, "glass", Vector3.zero, new Vector3(width, 1.1f, 0.06f), glass, 0.9f);
            BuildKit.Box(root.transform, "top", new Vector3(0f, 0.6f, 0f), new Vector3(width + 0.16f, 0.1f, 0.14f), frame);
            BuildKit.Box(root.transform, "bottom", new Vector3(0f, -0.6f, 0f), new Vector3(width + 0.16f, 0.1f, 0.14f), frame);
            BuildKit.Box(root.transform, "sideL", new Vector3(-width * 0.5f, 0f, 0f), new Vector3(0.1f, 1.2f, 0.14f), frame);
            BuildKit.Box(root.transform, "sideR", new Vector3(width * 0.5f, 0f, 0f), new Vector3(0.1f, 1.2f, 0.14f), frame);
            BuildKit.Box(root.transform, "bar", Vector3.zero, new Vector3(0.07f, 1.2f, 0.08f), frame);
        }

        void BuildInteriorLights()
        {
            interiorLights.Clear();
            lampBulbs.Clear();

            Vector3[] lampPos = { new Vector3(-3.2f, 2.85f, 0.4f), new Vector3(0f, 2.85f, 1.6f), new Vector3(3.2f, 2.85f, 0.4f) };
            for (int i = 0; i < lampPos.Length; i++)
            {
                GameObject holder = BuildKit.Empty(envRoot, "lamp" + i, lampPos[i]);
                BuildKit.Cyl(holder.transform, "cord", new Vector3(0f, 0.25f, 0f), new Vector3(0.02f, 0.22f, 0.02f), Color.black);
                GameObject shade = BuildKit.Cyl(holder.transform, "shade", new Vector3(0f, -0.05f, 0f),
                    new Vector3(0.42f, 0.16f, 0.42f), new Color(0.95f, 0.86f, 0.62f), 0.5f);
                shade.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
                GameObject bulb = BuildKit.Sphere(holder.transform, "bulb", new Vector3(0f, -0.16f, 0f), Vector3.one * 0.16f,
                    new Color(1f, 0.92f, 0.7f));
                Renderer br = bulb.GetComponent<Renderer>();
                if (br != null) { br.sharedMaterial = MaterialLibrary.Emissive(new Color(1f, 0.9f, 0.65f), 2.2f); lampBulbs.Add(br); }

                GameObject lightGo = new GameObject("pointLight");
                lightGo.transform.SetParent(holder.transform, false);
                lightGo.transform.localPosition = new Vector3(0f, -0.3f, 0f);
                Light l = lightGo.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 8f;
                l.intensity = 1.2f;
                l.color = new Color(1f, 0.88f, 0.68f);
                l.shadows = LightShadows.None;
                interiorLights.Add(l);
            }
        }

        // ---------------------------------------------------------------------
        //  Bagian luar
        // ---------------------------------------------------------------------
        void BuildOutdoor()
        {
            GameObject outdoor = BuildKit.Empty(envRoot, "Outdoor", Vector3.zero);

            // trotoar
            BuildKit.Box(outdoor.transform, "sidewalk", new Vector3(0f, -0.02f, frontZ + 3.2f),
                new Vector3(40f, 0.1f, 5f), new Color(0.72f, 0.71f, 0.68f));
            // jalan
            BuildKit.Box(outdoor.transform, "road", new Vector3(0f, -0.06f, frontZ + 11f),
                new Vector3(60f, 0.1f, 11f), MaterialLibrary.Palette.Road);
            // marka jalan
            for (int i = 0; i < 8; i++)
                BuildKit.Box(outdoor.transform, "mark", new Vector3(-14f + i * 4f, -0.005f, frontZ + 11f),
                    new Vector3(2f, 0.02f, 0.22f), new Color(0.95f, 0.93f, 0.8f));
            // rumput
            BuildKit.Box(outdoor.transform, "grassL", new Vector3(-halfW - 6f, -0.03f, frontZ + 3.2f),
                new Vector3(12f, 0.1f, 5f), MaterialLibrary.Palette.Grass);
            BuildKit.Box(outdoor.transform, "grassR", new Vector3(halfW + 6f, -0.03f, frontZ + 3.2f),
                new Vector3(12f, 0.1f, 5f), MaterialLibrary.Palette.Grass);

            // bangunan tetangga
            Color[] bcolors = { new Color(0.85f, 0.80f, 0.72f), new Color(0.78f, 0.72f, 0.80f), new Color(0.72f, 0.80f, 0.78f) };
            for (int i = 0; i < 3; i++)
            {
                Vector3 pos = new Vector3(-16f + i * 14f, 2.6f, frontZ + 18f);
                GameObject b = BuildKit.Box(outdoor.transform, "neighborHouse" + i, pos,
                    new Vector3(9f, 5.2f, 8f), bcolors[i % bcolors.Length]);
                BuildKit.Box(b.transform, "roofN", new Vector3(0f, 0.55f, 0f), new Vector3(1.02f, 0.12f, 1.02f),
                    new Color(0.55f, 0.28f, 0.24f));
                BuildKit.Box(outdoor.transform, "neighborWin" + i, pos + new Vector3(0f, 0.6f, -4.05f),
                    new Vector3(2.2f, 1.2f, 0.1f), new Color(0.70f, 0.84f, 0.95f));
            }

            // pohon
            BuildTrees(outdoor.transform, new Vector3(-halfW - 2.5f, 0f, frontZ + 4.6f));
            BuildTrees(outdoor.transform, new Vector3(halfW + 2.5f, 0f, frontZ + 4.6f));

            // tiang listrik + lampu jalan
            GameObject pole = BuildKit.Cyl(outdoor.transform, "pole", new Vector3(halfW + 1.8f, 3f, frontZ + 1.8f),
                new Vector3(0.16f, 3f, 0.16f), new Color(0.55f, 0.55f, 0.56f));
            BuildKit.Box(pole.transform, "arm", new Vector3(-0.6f, 0.95f, 0f), new Vector3(1.4f, 0.08f, 0.08f), new Color(0.4f, 0.4f, 0.42f));
            BuildKit.Sphere(pole.transform, "lampStreet", new Vector3(-1.15f, 0.9f, 0f), Vector3.one * 0.3f, new Color(1f, 0.95f, 0.75f));

            // motor parkir (stylized)
            GameObject bike = BuildKit.Empty(outdoor.transform, "bike", new Vector3(-halfW - 1.6f, 0f, frontZ + 1.6f));
            bike.transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            BuildKit.Cyl(bike.transform, "wheelF", new Vector3(0.55f, 0.3f, 0f), new Vector3(0.55f, 0.06f, 0.55f), Color.black);
            BuildKit.Cyl(bike.transform, "wheelB", new Vector3(-0.55f, 0.3f, 0f), new Vector3(0.55f, 0.06f, 0.55f), Color.black);
            BuildKit.Box(bike.transform, "bikeBody", new Vector3(0f, 0.55f, 0f), new Vector3(1.1f, 0.32f, 0.3f), new Color(0.80f, 0.18f, 0.16f));
            BuildKit.Box(bike.transform, "seat", new Vector3(-0.2f, 0.78f, 0f), new Vector3(0.5f, 0.12f, 0.28f), Color.black);
            BuildKit.Box(bike.transform, "handle", new Vector3(0.55f, 0.82f, 0f), new Vector3(0.1f, 0.4f, 0.5f), new Color(0.3f, 0.3f, 0.32f));

            // papan pasar (klik untuk belanja bahan)
            GameObject market = BuildKit.Empty(outdoor.transform, "marketStall", new Vector3(halfW + 0.4f, 0f, frontZ + 2.4f));
            BuildKit.Box(market.transform, "table", new Vector3(0f, 0.45f, 0f), new Vector3(2.2f, 0.9f, 1.2f), MaterialLibrary.Palette.Wood);
            BuildKit.Box(market.transform, "top", new Vector3(0f, 0.95f, 0f), new Vector3(2.4f, 0.1f, 1.35f), new Color(0.85f, 0.72f, 0.5f));
            BuildKit.Box(market.transform, "board", new Vector3(0f, 1.75f, -0.5f), new Vector3(1.8f, 0.9f, 0.08f), new Color(0.30f, 0.45f, 0.30f));
            BuildKit.WorldText(market.transform, "marketTxt", "PASAR", new Vector3(0f, 1.75f, -0.58f), 0.08f, Color.white, TextAnchor.MiddleCenter, 180f);
            // sayuran di atas meja
            BuildKit.Sphere(market.transform, "veg1", new Vector3(-0.7f, 1.05f, 0.1f), Vector3.one * 0.28f, MaterialLibrary.Palette.Leaf);
            BuildKit.Sphere(market.transform, "veg2", new Vector3(-0.2f, 1.05f, 0.25f), Vector3.one * 0.22f, new Color(0.85f, 0.35f, 0.2f));
            BuildKit.Sphere(market.transform, "veg3", new Vector3(0.4f, 1.05f, 0.05f), Vector3.one * 0.24f, new Color(0.95f, 0.8f, 0.3f));

            MarketStall stall = market.AddComponent<MarketStall>();
            stall.Init(market.transform);
            marketT = market.transform;

            // guard rail
            for (int i = 0; i < 6; i++)
                BuildKit.Cyl(outdoor.transform, "rail", new Vector3(-halfW - 5f + i * 2f, 0.5f, frontZ + 5.6f),
                    new Vector3(0.08f, 0.5f, 0.08f), new Color(0.7f, 0.7f, 0.72f));
        }

        void BuildTrees(Transform parent, Vector3 pos)
        {
            GameObject tree = BuildKit.Empty(parent, "tree", pos);
            BuildKit.Cyl(tree.transform, "trunk", new Vector3(0f, 1.2f, 0f), new Vector3(0.32f, 1.2f, 0.32f), MaterialLibrary.Palette.WoodDark);
            BuildKit.Sphere(tree.transform, "leafTop", new Vector3(0f, 2.9f, 0f), new Vector3(2.2f, 1.8f, 2.2f), MaterialLibrary.Palette.Leaf, 0.15f);
            BuildKit.Sphere(tree.transform, "leaf2", new Vector3(0.7f, 2.4f, 0.4f), new Vector3(1.4f, 1.2f, 1.4f),
                MaterialLibrary.Palette.Leaf * 0.9f, 0.15f);
        }

        // ---------------------------------------------------------------------
        //  Etalase, gudang, wastafel, kasir
        // ---------------------------------------------------------------------
        void BuildEtalase()
        {
            GameObject root = BuildKit.Empty(furnitureRoot, "Etalase", new Vector3(3.4f, 0f, backZ + 1.0f));
            root.transform.localRotation = Quaternion.identity;

            // badan etalase
            BuildKit.Box(root.transform, "base", new Vector3(0f, 0.45f, 0f), new Vector3(3.6f, 0.9f, 1.0f), MaterialLibrary.Palette.Wood);
            BuildKit.Box(root.transform, "counter", new Vector3(0f, 0.92f, 0f), new Vector3(3.7f, 0.08f, 1.1f),
                new Color(0.90f, 0.88f, 0.84f), 0.5f);
            // kaca
            BuildKit.GlassPanel(root.transform, "glassFront", new Vector3(0f, 1.5f, -0.42f), new Vector3(3.5f, 1.0f, 0.04f));
            BuildKit.GlassPanel(root.transform, "glassSideL", new Vector3(-1.75f, 1.5f, 0f), new Vector3(0.04f, 1.0f, 0.9f));
            BuildKit.GlassPanel(root.transform, "glassSideR", new Vector3(1.75f, 1.5f, 0f), new Vector3(0.04f, 1.0f, 0.9f));
            // rangka kaca
            Color frame = MaterialLibrary.Palette.Metal;
            BuildKit.Box(root.transform, "frameTop", new Vector3(0f, 2.02f, 0f), new Vector3(3.7f, 0.1f, 1.1f), frame, 0.6f);
            BuildKit.Box(root.transform, "frameL", new Vector3(-1.8f, 1.5f, 0f), new Vector3(0.1f, 1.1f, 1.1f), frame, 0.6f);
            BuildKit.Box(root.transform, "frameR", new Vector3(1.8f, 1.5f, 0f), new Vector3(0.1f, 1.1f, 1.1f), frame, 0.6f);

            Etalase = root.AddComponent<DisplayCase>();
            Etalase.Setup();
        }

        void BuildStorageRack()
        {
            GameObject root = BuildKit.Empty(furnitureRoot, "Gudang", new Vector3(-halfW + 0.8f, 0f, -1.2f));
            root.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            Color wood = MaterialLibrary.Palette.Wood;

            BuildKit.Box(root.transform, "back", new Vector3(0f, 1.2f, 0f), new Vector3(2.6f, 2.4f, 0.1f), wood * 0.85f);
            for (int i = 0; i < 3; i++)
            {
                BuildKit.Box(root.transform, "shelf" + i, new Vector3(0f, 0.5f + i * 0.75f, 0.35f),
                    new Vector3(2.4f, 0.08f, 0.8f), wood);
                // bahan di rak
                for (int k = 0; k < 3; k++)
                {
                    Color c = new Color(0.85f, 0.80f, 0.65f);
                    if (i == 0) c = new Color(0.92f, 0.90f, 0.85f);
                    if (i == 1) c = new Color(0.80f, 0.55f, 0.30f);
                    if (i == 2) c = new Color(0.55f, 0.65f, 0.40f);
                    BuildKit.Box(root.transform, "sack", new Vector3(-0.8f + k * 0.8f, 0.72f + i * 0.75f, 0.35f),
                        new Vector3(0.5f, 0.4f, 0.5f), c);
                }
            }
            BuildKit.WorldText(root.transform, "label", "GUDANG", new Vector3(0f, 2.5f, 0.3f), 0.07f, Color.white);

            StorageRack rack = root.AddComponent<StorageRack>();
            rack.Init();
            storageT = root.transform;
        }

        void BuildSink()
        {
            GameObject root = BuildKit.Empty(furnitureRoot, "Wastafel", new Vector3(-halfW + 0.9f, 0f, 2.2f));
            root.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            BuildKit.Box(root.transform, "sinkBody", new Vector3(0f, 0.45f, 0f), new Vector3(1.2f, 0.9f, 0.8f), new Color(0.85f, 0.86f, 0.88f));
            BuildKit.Box(root.transform, "basin", new Vector3(0f, 0.9f, 0f), new Vector3(1.0f, 0.1f, 0.6f), new Color(0.55f, 0.60f, 0.65f));
            BuildKit.Cyl(root.transform, "tap", new Vector3(0f, 1.15f, -0.2f), new Vector3(0.06f, 0.25f, 0.06f), MaterialLibrary.Palette.Metal, 0.8f);
            BuildKit.Box(root.transform, "mirror", new Vector3(0f, 1.9f, -0.42f), new Vector3(0.9f, 0.9f, 0.05f), new Color(0.75f, 0.86f, 0.92f), 0.9f);

            CleaningStation clean = root.AddComponent<CleaningStation>();
            clean.Init();
            sinkT = root.transform;
        }

        void BuildCashier()
        {
            GameObject root = BuildKit.Empty(furnitureRoot, "Kasir", new Vector3(halfW - 1.4f, 0f, frontZ - 3.2f));
            root.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            BuildKit.Box(root.transform, "desk", new Vector3(0f, 0.5f, 0f), new Vector3(2.0f, 1.0f, 0.8f), MaterialLibrary.Palette.Wood);
            BuildKit.Box(root.transform, "deskTop", new Vector3(0f, 1.02f, 0f), new Vector3(2.1f, 0.06f, 0.9f), new Color(0.92f, 0.90f, 0.86f));
            GameObject reg = BuildKit.Box(root.transform, "register", new Vector3(0.4f, 1.2f, 0f), new Vector3(0.5f, 0.35f, 0.45f),
                new Color(0.30f, 0.32f, 0.36f), 0.5f);
            BuildKit.Box(reg.transform, "screen", new Vector3(0f, 0.9f, 0f), new Vector3(1.1f, 0.7f, 0.2f), new Color(0.35f, 0.75f, 0.55f));
            BuildKit.Box(root.transform, "note", new Vector3(-0.5f, 1.08f, 0.1f), new Vector3(0.3f, 0.02f, 0.22f), Color.white);

            CashierDesk desk = root.AddComponent<CashierDesk>();
            desk.Init();
            cashierT = root.transform;
        }

        void BuildDecor()
        {
            GameObject decor = BuildKit.Empty(furnitureRoot, "Decor", Vector3.zero);

            // poster dinding
            Color[] posterColors = { new Color(0.85f, 0.35f, 0.25f), new Color(0.30f, 0.55f, 0.75f), new Color(0.95f, 0.80f, 0.35f) };
            for (int i = 0; i < 3; i++)
            {
                GameObject p = BuildKit.Box(decor.transform, "poster" + i,
                    new Vector3(-2.5f + i * 2.5f, 2.1f, backZ + 0.12f), new Vector3(1.1f, 0.8f, 0.05f), posterColors[i]);
                BuildKit.Box(p.transform, "posterInner", new Vector3(0f, 0f, -0.1f), new Vector3(0.9f, 0.62f, 0.02f), Color.white);
            }

            // tanaman pot
            BuildPlant(decor.transform, new Vector3(-halfW + 0.9f, 0f, frontZ - 1.0f));
            BuildPlant(decor.transform, new Vector3(halfW - 0.9f, 0f, backZ + 0.9f));

            // tempat sampah
            GameObject trash = BuildKit.Empty(decor.transform, "TrashBin", new Vector3(-halfW + 0.9f, 0f, 0.2f));
            BuildKit.Cyl(trash.transform, "bin", new Vector3(0f, 0.35f, 0f), new Vector3(0.5f, 0.35f, 0.5f), new Color(0.30f, 0.45f, 0.35f));
            BuildKit.Cyl(trash.transform, "lid", new Vector3(0f, 0.72f, 0f), new Vector3(0.55f, 0.04f, 0.55f), new Color(0.22f, 0.34f, 0.26f));

            // rak piring di dapur
            BuildKit.Box(decor.transform, "dishRack", new Vector3(-1.2f, 1.4f, backZ + 0.2f), new Vector3(1.6f, 0.08f, 0.5f), MaterialLibrary.Palette.Metal, 0.6f);
            for (int i = 0; i < 5; i++)
                BuildKit.Cyl(decor.transform, "plate", new Vector3(-1.9f + i * 0.35f, 1.48f, backZ + 0.2f),
                    new Vector3(0.30f, 0.02f, 0.30f), new Color(0.95f, 0.95f, 0.93f));

            // rice cooker
            GameObject rc = BuildKit.Empty(decor.transform, "RiceCooker", new Vector3(1.0f, 0f, backZ + 1.2f));
            BuildKit.Cyl(rc.transform, "body", new Vector3(0f, 0.25f, 0f), new Vector3(0.6f, 0.25f, 0.6f), new Color(0.92f, 0.93f, 0.95f));
            BuildKit.Sphere(rc.transform, "lid", new Vector3(0f, 0.52f, 0f), new Vector3(0.6f, 0.2f, 0.6f), new Color(0.85f, 0.87f, 0.9f));

            // meja persiapan (prep table)
            BuildKit.Box(decor.transform, "prepTable", new Vector3(-2.0f, 0.45f, backZ + 1.1f), new Vector3(2.0f, 0.9f, 0.9f),
                new Color(0.80f, 0.82f, 0.85f), 0.4f);
            BuildKit.Box(decor.transform, "prepTop", new Vector3(-2.0f, 0.93f, backZ + 1.1f), new Vector3(2.1f, 0.06f, 1.0f),
                new Color(0.60f, 0.63f, 0.66f), 0.75f);
            // talenan + bahan
            BuildKit.Box(decor.transform, "board", new Vector3(-2.3f, 0.98f, backZ + 1.1f), new Vector3(0.5f, 0.04f, 0.35f), new Color(0.85f, 0.70f, 0.45f));
            BuildKit.Sphere(decor.transform, "onion", new Vector3(-1.6f, 1.02f, backZ + 1.1f), Vector3.one * 0.18f, new Color(0.85f, 0.45f, 0.65f));

            // kulkas (hanya jika sudah dibeli)
            fridgeObj = BuildKit.Empty(decor.transform, "Kulkas", new Vector3(2.3f, 0f, backZ + 1.4f));
            BuildKit.Box(fridgeObj.transform, "body", new Vector3(0f, 0.85f, 0f), new Vector3(1.0f, 1.7f, 0.75f), new Color(0.93f, 0.94f, 0.96f), 0.6f);
            BuildKit.Box(fridgeObj.transform, "door", new Vector3(0f, 0.9f, -0.4f), new Vector3(0.95f, 1.5f, 0.06f), new Color(0.86f, 0.88f, 0.92f), 0.7f);
            BuildKit.Box(fridgeObj.transform, "handle", new Vector3(0.4f, 0.9f, -0.45f), new Vector3(0.06f, 0.6f, 0.06f), MaterialLibrary.Palette.Metal, 0.8f);
            fridgeObj.SetActive(false);

            // banner (jika dibeli)
            bannerObj = BuildKit.Empty(decor.transform, "Banner", new Vector3(-halfW + 0.15f, 2.2f, 0f));
            bannerObj.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            BuildKit.Box(bannerObj.transform, "cloth", Vector3.zero, new Vector3(2.2f, 0.8f, 0.05f), new Color(0.92f, 0.75f, 0.20f));
            BuildKit.WorldText(bannerObj.transform, "txt", "PROMO! MURAH & ENAK", new Vector3(0f, 0f, -0.06f), 0.055f,
                new Color(0.25f, 0.15f, 0.10f), TextAnchor.MiddleCenter, 180f);
            bannerObj.SetActive(false);
        }

        void BuildPlant(Transform parent, Vector3 pos)
        {
            GameObject pot = BuildKit.Empty(parent, "plant", pos);
            BuildKit.Cyl(pot.transform, "pot", new Vector3(0f, 0.25f, 0f), new Vector3(0.5f, 0.25f, 0.5f), new Color(0.72f, 0.45f, 0.32f));
            BuildKit.Sphere(pot.transform, "leaf", new Vector3(0f, 0.75f, 0f), new Vector3(0.9f, 0.8f, 0.9f), MaterialLibrary.Palette.Leaf, 0.2f);
            BuildKit.Sphere(pot.transform, "leaf2", new Vector3(0.2f, 0.95f, 0.1f), new Vector3(0.55f, 0.5f, 0.55f),
                MaterialLibrary.Palette.Leaf * 1.1f, 0.2f);
        }

        void BuildRain()
        {
            GameObject go = BuildKit.Empty(transform, "Rain", Vector3.zero);
            Rain = go.AddComponent<RainFX>();
            Bounds b = new Bounds(new Vector3(0f, 4f, 2f), new Vector3(30f, 10f, 24f));
            Rain.Setup(140, b);
        }

        // =====================================================================
        //  Dynamic rebuild (mengikuti upgrade)
        // =====================================================================
        public void Rebuild(SaveData data)
        {
            int wantTables = GameManager.I.Upgrades != null ? GameManager.I.Upgrades.TableCount : Catalog.BaseTables;
            bool needExpand = GameManager.I.Upgrades != null && GameManager.I.Upgrades.Has("perluasan");
            if (needExpand != expanded)
            {
                BuildWorld(cam, sun);
            }

            ClearChildren(tableRoot);
            Tables.Clear();
            int max = Mathf.Min(wantTables, TableSlots.Length);
            for (int i = 0; i < max; i++)
            {
                Vector3 p = TableSlots[i];
                if (p.z > frontZ - 1.5f) continue;
                GameObject go = new GameObject("Meja_" + (i + 1));
                go.transform.SetParent(tableRoot, false);
                TableUnit t = go.AddComponent<TableUnit>();
                t.Setup(i, p);
                Tables.Add(t);
            }

            if (fridgeObj != null) fridgeObj.SetActive(GameManager.I.Upgrades != null && GameManager.I.Upgrades.Has("kulkas"));
            if (bannerObj != null) bannerObj.SetActive(GameManager.I.Upgrades != null && GameManager.I.Upgrades.Has("banner"));
            if (Etalase != null) Etalase.Rebuild(data);
            RefreshSignboard();
            SetWeatherVisual(GameManager.I.Time != null ? GameManager.I.Time.Weather : Weather.Cerah);
        }

        public void RefreshSignboard()
        {
            if (signText == null) return;
            bool resto = GameManager.I.Upgrades != null && GameManager.I.Upgrades.Has("restoran");
            string name = GameManager.I.Data != null ? GameManager.I.Data.wartegName : "Warteg Berkah";
            if (resto) name = "RESTORAN " + name.Replace("Warteg ", "").ToUpper();
            else name = name.ToUpper();
            signText.text = name;
        }

        public TableUnit ReserveTable()
        {
            for (int i = 0; i < Tables.Count; i++)
                if (Tables[i].HasFreeSeat) return Tables[i];
            return null;
        }

        public int FreeSeatCount()
        {
            int n = 0;
            for (int i = 0; i < Tables.Count; i++)
                for (int k = 0; k < Tables[i].occupants.Length; k++)
                    if (Tables[i].occupants[k] == null) n++;
            return n;
        }

        public int OccupiedSeatCount()
        {
            int n = 0;
            for (int i = 0; i < Tables.Count; i++) n += Tables[i].OccupancyCount;
            return n;
        }

        // =====================================================================
        //  Cuaca & pencahayaan
        // =====================================================================
        public void SetWeatherVisual(Weather w)
        {
            if (cam != null)
            {
                switch (w)
                {
                    case Weather.Cerah: cam.backgroundColor = new Color(0.62f, 0.80f, 0.95f); break;
                    case Weather.Mendung: cam.backgroundColor = new Color(0.62f, 0.65f, 0.70f); break;
                    default: cam.backgroundColor = new Color(0.42f, 0.47f, 0.55f); break;
                }
            }
            if (Rain != null) Rain.SetActive(w == Weather.Hujan);
            UpdateLighting(true);
        }

        void Update()
        {
            if (GameManager.I == null || GameManager.I.Time == null) return;
            UpdateLighting(false);
        }

        void UpdateLighting(bool force)
        {
            if (sun == null) return;
            float hour = GameManager.I != null && GameManager.I.Time != null ? GameManager.I.Time.Hour : 12f;
            Weather w = GameManager.I != null && GameManager.I.Time != null ? GameManager.I.Time.Weather : Weather.Cerah;

            // 0 = siang, 1 = malam
            float night = 0f;
            if (hour < 7f) night = 1f;
            else if (hour < 9f) night = Mathf.InverseLerp(7f, 9f, hour) * -1f + 1f;
            else if (hour < 17f) night = 0f;
            else if (hour < 19f) night = Mathf.InverseLerp(17f, 19f, hour);
            else night = 1f;

            float weatherMul = w == Weather.Cerah ? 1f : (w == Weather.Mendung ? 0.7f : 0.45f);
            float intensity = Mathf.Lerp(1.25f, 0.12f, night) * weatherMul;
            sun.intensity = intensity;
            sun.color = Color.Lerp(new Color(1f, 0.96f, 0.86f), new Color(0.72f, 0.78f, 1f), night);

            Color ambient = Color.Lerp(new Color(0.55f, 0.55f, 0.58f), new Color(0.16f, 0.18f, 0.28f), night);
            if (w == Weather.Hujan) ambient *= 0.8f;
            RenderSettings.ambientLight = ambient;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            // lampu dalam menyala saat gelap
            float lampOn = Mathf.Clamp01(night * 1.2f);
            for (int i = 0; i < interiorLights.Count; i++)
                if (interiorLights[i] != null) interiorLights[i].intensity = Mathf.Lerp(0.35f, 1.9f, lampOn);
            for (int i = 0; i < lampBulbs.Count; i++)
                if (lampBulbs[i] != null)
                    lampBulbs[i].sharedMaterial = MaterialLibrary.Emissive(Color.Lerp(new Color(1f, 0.9f, 0.65f), new Color(1f, 0.95f, 0.8f), lampOn), 1.2f + lampOn);
        }
    }
}
