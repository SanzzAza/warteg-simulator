using UnityEngine;

namespace Warteg
{
    /// <summary>Satu meja makan dengan 2 kursi. Pelanggan duduk di sini dan dilayani pemain.</summary>
    public class TableUnit : InteractableBase
    {
        public int index;
        public Transform[] seatPoints = new Transform[Catalog.SeatsPerTable];
        public CustomerAI[] occupants = new CustomerAI[Catalog.SeatsPerTable];

        GameObject orderMarker;

        public void Setup(int idx, Vector3 worldPos)
        {
            index = idx;
            label = "Meja " + (idx + 1);
            radius = 1.9f;
            pointOffset = new Vector3(0f, 0f, -1.15f);
            transform.position = worldPos;
            BuildVisual();
            BuildSeats();
        }

        void BuildVisual()
        {
            Color wood = MaterialLibrary.Palette.Wood;
            Color woodDark = MaterialLibrary.Palette.WoodDark;

            // meja
            BuildKit.Box(transform, "tabletop", new Vector3(0f, 0.74f, 0f), new Vector3(1.15f, 0.08f, 1.15f), wood, 0.35f);
            BuildKit.Box(transform, "tableLeg", new Vector3(0f, 0.36f, 0f), new Vector3(0.14f, 0.72f, 0.14f), woodDark);
            // taplak / hiasan
            BuildKit.Box(transform, "cloth", new Vector3(0f, 0.79f, 0f), new Vector3(0.9f, 0.02f, 0.9f),
                new Color(0.86f, 0.36f, 0.28f), 0.2f);

            // 2 kursi di kiri & kanan meja
            BuildChair(new Vector3(-0.95f, 0f, 0f), 90f);
            BuildChair(new Vector3(0.95f, 0f, 0f), -90f);

            // penanda pesanan (bola kuning melayang)
            orderMarker = BuildKit.Sphere(transform, "orderMarker", new Vector3(0f, 1.9f, 0f), Vector3.one * 0.22f,
                new Color(1f, 0.85f, 0.2f), 0.6f);
            Renderer r = orderMarker.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = MaterialLibrary.Emissive(new Color(1f, 0.85f, 0.2f), 1.8f);
            orderMarker.SetActive(false);

            EnsureClickCollider(new Vector3(1.6f, 1.2f, 1.6f), new Vector3(0f, 0.6f, 0f));
        }

        void BuildChair(Vector3 pos, float rotY)
        {
            GameObject chair = BuildKit.Empty(transform, "chair", pos);
            chair.transform.localRotation = Quaternion.Euler(0f, rotY, 0f);
            BuildKit.Box(chair.transform, "seat", new Vector3(0f, 0.45f, 0f), new Vector3(0.46f, 0.07f, 0.46f),
                MaterialLibrary.Palette.WoodDark, 0.3f);
            BuildKit.Box(chair.transform, "back", new Vector3(0f, 0.72f, -0.2f), new Vector3(0.46f, 0.5f, 0.06f),
                MaterialLibrary.Palette.WoodDark, 0.3f);
            BuildKit.Box(chair.transform, "leg", new Vector3(0f, 0.22f, 0f), new Vector3(0.1f, 0.44f, 0.1f),
                MaterialLibrary.Palette.WoodDark);
        }

        void BuildSeats()
        {
            for (int i = 0; i < seatPoints.Length; i++)
            {
                if (seatPoints[i] != null) continue;
                GameObject s = new GameObject("seat" + i);
                s.transform.SetParent(transform, false);
                s.transform.localPosition = new Vector3(i == 0 ? -0.95f : 0.95f, 0f, 0f);
                seatPoints[i] = s.transform;
            }
        }

        public bool HasFreeSeat
        {
            get
            {
                for (int i = 0; i < occupants.Length; i++) if (occupants[i] == null) return true;
                return false;
            }
        }

        public int FreeSeatIndex()
        {
            for (int i = 0; i < occupants.Length; i++) if (occupants[i] == null) return i;
            return -1;
        }

        public void Occupy(int seat, CustomerAI c)
        {
            if (seat < 0 || seat >= occupants.Length) return;
            occupants[seat] = c;
        }

        public void Vacate(CustomerAI c)
        {
            for (int i = 0; i < occupants.Length; i++) if (occupants[i] == c) occupants[i] = null;
        }

        public int OccupancyCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < occupants.Length; i++) if (occupants[i] != null) n++;
                return n;
            }
        }

        public bool AnyoneWaiting
        {
            get
            {
                for (int i = 0; i < occupants.Length; i++)
                    if (occupants[i] != null && occupants[i].WantsService) return true;
                return false;
            }
        }

        void Update()
        {
            bool waiting = AnyoneWaiting;
            if (orderMarker != null && orderMarker.activeSelf != waiting) orderMarker.SetActive(waiting);
            if (orderMarker != null && waiting)
            {
                orderMarker.transform.localPosition = new Vector3(0f, 1.9f + Mathf.Sin(Time.time * 4f) * 0.1f, 0f);
                orderMarker.transform.Rotate(Vector3.up, Time.deltaTime * 120f, Space.World);
            }
        }

        public override void Interact(PlayerController player)
        {
            int served = 0;
            for (int i = 0; i < occupants.Length; i++)
            {
                CustomerAI c = occupants[i];
                if (c != null && c.WantsService)
                {
                    if (c.TryServeFromDisplay()) served++;
                }
            }
            if (served > 0) return;

            // kalau tidak ada yang menunggu, beri info
            if (OccupancyCount == 0) GameManager.I.Notify("Meja kosong. Pelanggan akan datang sendiri.", 0);
            else if (AnyoneWaiting) GameManager.I.Notify("Pesanan belum bisa dilayani — cek etalase!", 2);
        }

        public Vector3 ServePosition { get { return transform.position + new Vector3(0f, 0f, -1.3f); } }
    }
}
