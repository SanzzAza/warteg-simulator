using UnityEngine;

namespace Warteg
{
    /// <summary>
    /// Pemain: berjalan dengan WASD / joystick layar, atau klik-untuk-jalan.
    /// Klik pada objek = jalan mendekat lalu berinteraksi otomatis.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public CharacterRig rig;
        public Camera cam;
        public float speed = 3.6f;
        public Vector3 moveBoundsCenter = Vector3.zero;
        public Vector2 moveBounds = new Vector2(11f, 12f);

        Vector3 moveTarget;
        bool hasTarget;
        IInteractable pending;
        GameObject marker;
        Vector3 lastPos;

        public bool IsMoving { get; private set; }

        public void Init(Camera camera, Vector3 spawn)
        {
            cam = camera;
            transform.position = spawn;
            lastPos = transform.position;

            rig = CharacterBuilder.Build(transform, "playerRig",
                new Color(0.85f, 0.35f, 0.25f), new Color(0.22f, 0.26f, 0.36f), 1.05f);
            rig.transform.localPosition = Vector3.zero;
            // topi koki kecil biar khas pemilik warteg
            BuildKit.Cyl(rig.head, "cap", new Vector3(0f, 0.62f, 0f), new Vector3(0.85f, 0.18f, 0.85f),
                new Color(0.96f, 0.96f, 0.94f), 0.4f);

            marker = BuildKit.Cyl(null, "moveMarker", Vector3.zero, new Vector3(0.7f, 0.01f, 0.7f),
                new Color(0.95f, 0.85f, 0.3f));
            Renderer mr = marker.GetComponent<Renderer>();
            if (mr != null) mr.sharedMaterial = MaterialLibrary.Emissive(new Color(0.98f, 0.86f, 0.30f), 1.2f);
            marker.SetActive(false);
        }

        void Update()
        {
            if (GameManager.I == null || GameManager.I.Data == null) return;
            if (GameManager.I.IsPaused) { rig.SetMoving(false); return; }

            HandleInput();

            Vector2 axis = InputService.MoveAxis();
            if (axis.sqrMagnitude > 0.04f)
            {
                hasTarget = false;
                pending = null;
                if (marker != null) marker.SetActive(false);
                Vector3 dir = CameraRelative(axis.normalized);
                MoveBy(dir * speed * Time.deltaTime);
                rig.FaceDirection(dir);
                rig.SetMoving(true);
            }
            else if (hasTarget)
            {
                Vector3 delta = moveTarget - transform.position;
                delta.y = 0f;
                float dist = delta.magnitude;
                if (dist < 0.12f)
                {
                    hasTarget = false;
                    IsMoving = false;
                    if (marker != null) marker.SetActive(false);
                    rig.SetMoving(false);
                    TryPendingInteract();
                }
                else
                {
                    MoveBy(delta.normalized * speed * Time.deltaTime);
                    rig.FaceDirection(delta);
                    rig.SetMoving(true);
                }
            }
            else
            {
                rig.SetMoving(false);
                if (InputService.InteractPressed()) InteractNearest();
            }

            // cek pending interaksi saat sudah dekat (misalnya target tidak terjangkau)
            if (pending != null && !hasTarget)
            {
                if (Vector3.Distance(Flat(transform.position), Flat(pending.InteractPoint)) <= pending.InteractRadius + 0.4f)
                    TryPendingInteract();
            }

            IsMoving = (transform.position - lastPos).sqrMagnitude > 0.00001f;
            lastPos = transform.position;
        }

        Vector3 CameraRelative(Vector2 axis)
        {
            if (cam == null) return new Vector3(axis.x, 0f, axis.y);
            Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 right = cam.transform.right; right.y = 0f; right.Normalize();
            Vector3 dir = fwd * axis.y + right * axis.x;
            return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
        }

        void MoveBy(Vector3 delta)
        {
            Vector3 p = transform.position + delta;
            p.x = Mathf.Clamp(p.x, moveBoundsCenter.x - moveBounds.x, moveBoundsCenter.x + moveBounds.x);
            p.z = Mathf.Clamp(p.z, moveBoundsCenter.z - moveBounds.y, moveBoundsCenter.z + moveBounds.y);
            p.y = 0f;
            transform.position = p;
        }

        void HandleInput()
        {
            if (!InputService.PointerDown()) return;
            if (InputService.PointerOverUI()) return;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(InputService.PointerPosition());
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200f))
            {
                IInteractable inter = hit.collider.GetComponentInParent<IInteractable>();
                if (inter != null && inter.CanInteract)
                {
                    pending = inter;
                    SetTarget(inter.InteractPoint);
                    return;
                }
                // klik tanah / objek lain → jalan ke titik itu
                pending = null;
                SetTarget(new Vector3(hit.point.x, 0f, hit.point.z));
                return;
            }

            // klik ke langit / area tanpa collider → pakai bidang datar
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            float d;
            if (plane.Raycast(ray, out d))
            {
                Vector3 p = ray.GetPoint(d);
                pending = null;
                SetTarget(new Vector3(p.x, 0f, p.z));
            }
        }

        void SetTarget(Vector3 p)
        {
            p.y = 0f;
            moveTarget = p;
            hasTarget = true;
            if (marker != null)
            {
                marker.SetActive(true);
                marker.transform.position = p + Vector3.up * 0.02f;
                marker.transform.localScale = new Vector3(0.62f, 0.006f, 0.62f);
            }
        }

        void TryPendingInteract()
        {
            if (pending == null) return;
            if (!pending.CanInteract) { pending = null; return; }
            float dist = Vector3.Distance(Flat(transform.position), Flat(pending.InteractPoint));
            if (dist <= pending.InteractRadius + 0.6f)
            {
                IInteractable p = pending;
                pending = null;
                p.Interact(this);
                Vector3 d = p.InteractPoint - transform.position;
                if (d.sqrMagnitude > 0.01f) rig.FaceDirection(d);
            }
            else
            {
                SetTarget(pending.InteractPoint);
            }
        }

        void InteractNearest()
        {
            // cari objek ber-interaksi terdekat di sekitar pemain
            Collider[] cols = Physics.OverlapSphere(transform.position + Vector3.up * 0.6f, 3.2f);
            IInteractable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < cols.Length; i++)
            {
                IInteractable it = cols[i].GetComponentInParent<IInteractable>();
                if (it == null || !it.CanInteract) continue;
                float d = Vector3.Distance(Flat(transform.position), Flat(it.InteractPoint));
                if (d < bestDist) { bestDist = d; best = it; }
            }
            if (best != null) best.Interact(this);
        }

        /// <summary>Jalan mendekati objek lalu berinteraksi (dipakai tombol "Layani" di HUD).</summary>
        public void WalkToAndInteract(IInteractable target)
        {
            if (target == null) return;
            pending = target;
            SetTarget(target.InteractPoint);
        }

        public void Teleport(Vector3 p)
        {
            transform.position = new Vector3(p.x, 0f, p.z);
            hasTarget = false;
            pending = null;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(moveBoundsCenter.x, 0f, moveBoundsCenter.z),
                new Vector3(moveBounds.x * 2f, 0.1f, moveBounds.y * 2f));
        }

        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }
    }

    /// <summary>Kamera isometrik yang mengikuti pemain; bisa diputar & di-zoom.</summary>
    public class CameraRig : MonoBehaviour
    {
        public Camera cam;
        public Transform target;
        public float yaw = 180f;
        public float pitch = 52f;
        public float distance = 15f;
        public float minDistance = 8f;
        public float maxDistance = 26f;

        Vector3 smoothTargetPos;
        bool dragging;
        Vector2 lastPointer;

        public void Init(Camera camera, Transform followTarget)
        {
            cam = camera;
            target = followTarget;
            if (target != null) smoothTargetPos = target.position;
            Apply();
        }

        public void Rotate(float degrees) { yaw += degrees; }

        public void Zoom(float delta)
        {
            distance = Mathf.Clamp(distance - delta, minDistance, maxDistance);
        }

        void Update()
        {
            if (GameManager.I == null) return;

            // rotasi kamera dengan Q/E atau tombol HUD
            if (!GameManager.I.IsPaused)
            {
                if (InputService.RotateLeft()) yaw -= 70f * Time.deltaTime;
                if (InputService.RotateRight()) yaw += 70f * Time.deltaTime;

                float scroll = InputService.ScrollDelta();
                if (Mathf.Abs(scroll) > 0.01f) Zoom(scroll * 1.6f);
            }

            // drag dengan tombol mouse kanan / dua jari untuk memutar
            HandleDrag();
        }

        void HandleDrag()
        {
            if (GameManager.I.IsPaused) { dragging = false; return; }
            if (InputService.PointerDown() && !InputService.PointerOverUI())
            {
                dragging = true;
                lastPointer = InputService.PointerPosition();
            }
            if (!InputService.PointerHeld()) dragging = false;

            if (dragging)
            {
                Vector2 pos = InputService.PointerPosition();
                Vector2 delta = pos - lastPointer;
                lastPointer = pos;
                if (Mathf.Abs(delta.x) > 3f) yaw += delta.x * 0.28f;
            }
        }

        void LateUpdate()
        {
            if (target == null) return;
            smoothTargetPos = Vector3.Lerp(smoothTargetPos, target.position, Time.deltaTime * 6f);
            Apply();
        }

        void Apply()
        {
            if (cam == null) return;
            Vector3 focus = smoothTargetPos + Vector3.up * 1.1f;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 offset = rot * new Vector3(0f, 0f, -distance);
            cam.transform.position = focus + offset;
            cam.transform.rotation = Quaternion.LookRotation(focus - cam.transform.position, Vector3.up);
        }
    }
}
