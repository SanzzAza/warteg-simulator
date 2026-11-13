using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Karakter 3D stylized yang dirakit dari primitive + animasi prosedural
    /// (jalan, berdiri, duduk, mengangguk). Tidak butuh file animasi.
    /// </summary>
    public class CharacterRig : MonoBehaviour
    {
        public Transform pivot;
        public Transform head;
        public Transform body;
        public Transform armL, armR;
        public Transform legL, legR;
        public Transform carryPoint;

        public bool IsMoving { get; private set; }
        public bool IsSitting { get; private set; }

        float phase;
        float bob;
        Vector3 headBase;
        Vector3 armLBase, armRBase, legLBase, legRBase;
        Quaternion armLBaseRot, armRBaseRot, legLBaseRot, legRBaseRot;
        float sitAmount;    // 0..1 interpolasi halus
        float moveAmount;   // 0..1

        void Awake()
        {
            if (head != null) headBase = head.localPosition;
            if (armL != null) { armLBase = armL.localPosition; armLBaseRot = armL.localRotation; }
            if (armR != null) { armRBase = armR.localPosition; armRBaseRot = armR.localRotation; }
            if (legL != null) { legLBase = legL.localPosition; legLBaseRot = legL.localRotation; }
            if (legR != null) { legRBase = legR.localPosition; legRBaseRot = legR.localRotation; }
        }

        public void SetMoving(bool moving)
        {
            if (IsMoving == moving) return;
            IsMoving = moving;
            if (moving) phase = Random.Range(0f, 6.28f);
        }

        public void SetSitting(bool sitting) { IsSitting = sitting; }

        public void FaceDirection(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0004f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir.normalized, Vector3.up), Time.deltaTime * 8f);
        }

        public void FaceInstant(Vector3 dir)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0004f) return;
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            moveAmount = Mathf.MoveTowards(moveAmount, IsMoving ? 1f : 0f, dt * 6f);
            sitAmount = Mathf.MoveTowards(sitAmount, IsSitting ? 1f : 0f, dt * 5f);

            if (IsMoving) phase += dt * 9.5f;

            float swing = Mathf.Sin(phase) * 32f * moveAmount;

            if (legL != null)
                legL.localRotation = legLBaseRot * Quaternion.Euler(swing * 0.9f - sitAmount * 78f, 0f, 0f);
            if (legR != null)
                legR.localRotation = legRBaseRot * Quaternion.Euler(-swing * 0.9f - sitAmount * 78f, 0f, 0f);
            if (armL != null)
                armL.localRotation = armLBaseRot * Quaternion.Euler(-swing * 0.8f + sitAmount * 55f, 0f, 0f);
            if (armR != null)
                armR.localRotation = armRBaseRot * Quaternion.Euler(swing * 0.8f + sitAmount * 55f, 0f, 0f);

            // bob saat berjalan + nafas saat diam
            bob = Mathf.Abs(Mathf.Sin(phase)) * 0.055f * moveAmount;
            float breathe = Mathf.Sin(Time.time * 1.9f) * 0.012f * (1f - moveAmount);
            if (pivot != null)
                pivot.localPosition = new Vector3(0f, bob + breathe - sitAmount * 0.42f, 0f);

            if (head != null)
            {
                head.localPosition = headBase + new Vector3(0f, Mathf.Sin(Time.time * 1.6f) * 0.006f * (1f - moveAmount), 0f);
                head.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.7f + GetInstanceID()) * 6f * (1f - moveAmount), 0f);
            }
        }
    }

    /// <summary>Pabrik karakter stylized dari primitive.</summary>
    public static class CharacterBuilder
    {
        public static CharacterRig Build(Transform parent, string name, Color shirt, Color pants, float scale = 1f)
        {
            GameObject root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * scale;

            Transform pivot = new GameObject("pivot").transform;
            pivot.SetParent(root.transform, false);

            CharacterRig rig = root.AddComponent<CharacterRig>();
            rig.pivot = pivot;

            Color skin = MaterialLibrary.Palette.Skin;
            Color hair = MaterialLibrary.Palette.Hair;

            // --- kaki ---
            Transform legL = new GameObject("legL").transform;
            legL.SetParent(pivot, false);
            legL.localPosition = new Vector3(-0.11f, 0.74f, 0f);
            BuildKit.Capsule(legL, "legMesh", new Vector3(0f, -0.33f, 0f), new Vector3(0.20f, 0.33f, 0.20f), pants);

            Transform legR = new GameObject("legR").transform;
            legR.SetParent(pivot, false);
            legR.localPosition = new Vector3(0.11f, 0.74f, 0f);
            BuildKit.Capsule(legR, "legMesh", new Vector3(0f, -0.33f, 0f), new Vector3(0.20f, 0.33f, 0.20f), pants);

            // --- badan ---
            GameObject body = BuildKit.Capsule(pivot, "body", new Vector3(0f, 1.06f, 0f), new Vector3(0.44f, 0.34f, 0.34f), shirt);
            rig.body = body.transform;
            // kerah
            BuildKit.Box(pivot, "collar", new Vector3(0f, 1.30f, 0f), new Vector3(0.30f, 0.05f, 0.28f), shirt * 1.1f);

            // --- lengan ---
            Transform armL = new GameObject("armL").transform;
            armL.SetParent(pivot, false);
            armL.localPosition = new Vector3(-0.28f, 1.30f, 0f);
            BuildKit.Capsule(armL, "armMesh", new Vector3(0f, -0.22f, 0f), new Vector3(0.15f, 0.24f, 0.15f), shirt);
            BuildKit.Sphere(armL, "hand", new Vector3(0f, -0.44f, 0f), Vector3.one * 0.13f, skin);

            Transform armR = new GameObject("armR").transform;
            armR.SetParent(pivot, false);
            armR.localPosition = new Vector3(0.28f, 1.30f, 0f);
            BuildKit.Capsule(armR, "armMesh", new Vector3(0f, -0.22f, 0f), new Vector3(0.15f, 0.24f, 0.15f), shirt);
            BuildKit.Sphere(armR, "hand", new Vector3(0f, -0.44f, 0f), Vector3.one * 0.13f, skin);

            // --- kepala ---
            GameObject head = BuildKit.Sphere(pivot, "head", new Vector3(0f, 1.63f, 0f), new Vector3(0.30f, 0.32f, 0.30f), skin, 0.4f);
            rig.head = head.transform;

            // rambut
            BuildKit.Sphere(head.transform, "hair", new Vector3(0f, 0.45f, -0.02f), new Vector3(1.06f, 0.72f, 1.06f), hair, 0.3f);
            // mata
            BuildKit.Sphere(head.transform, "eyeL", new Vector3(-0.22f, 0.05f, 0.46f), Vector3.one * 0.13f, Color.black);
            BuildKit.Sphere(head.transform, "eyeR", new Vector3(0.22f, 0.05f, 0.46f), Vector3.one * 0.13f, Color.black);
            // mulut
            BuildKit.Box(head.transform, "mouth", new Vector3(0f, -0.30f, 0.44f), new Vector3(0.22f, 0.05f, 0.05f), new Color(0.55f, 0.25f, 0.22f));

            // bayangan sederhana (blob shadow)
            GameObject shadow = BuildKit.Cyl(pivot, "shadow", new Vector3(0f, 0.02f, 0f), new Vector3(0.55f, 0.004f, 0.55f),
                new Color(0.15f, 0.13f, 0.12f));
            MeshRenderer sr = shadow.GetComponent<MeshRenderer>();
            if (sr != null) sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            rig.armL = armL; rig.armR = armR;
            rig.legL = legL; rig.legR = legR;
            rig.carryPoint = armR;

            // matikan bayangan real-time untuk karakter (biar ringan di mobile)
            MeshRenderer[] mrs = root.GetComponentsInChildren<MeshRenderer>();
            for (int i = 0; i < mrs.Length; i++)
            {
                mrs[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                mrs[i].receiveShadows = false;
            }
            if (sr != null) sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            return rig;
        }
    }
}
