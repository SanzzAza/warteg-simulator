# 🥘 Build APK — Cara Terbaru (Unity sudah menutup aktivasi manual)

## Kenapa halaman license.unity3d.com/manual itu buntu

Itu **bukan salah kamu**. Unity sudah menghentikan aktivasi manual (`.alf` → `.ulf`)
untuk lisensi **Personal/free**. Halaman itu sekarang hanya melayani
Enterprise/Industry (dan Pro serial lama). Jadi langkah upload `.alf` yang saya
tulis sebelumnya memang sudah tidak bisa dipakai lagi — maaf, itu info lama.

Yang masih **berfungsi** untuk akun gratis: **aktivasi online** memakai
**email + password** akun Unity. Itu yang sekarang dipakai pipeline di repo ini
(`game-ci/unity-builder@v6.0.0`, rilis 10 Sep 2026).

---

## Langkah kamu (tanpa install apa pun, ±3 menit)

### 1. Simpan kredensial Unity
Buka repo → tab **Actions** → pilih workflow **"Set Kredensial Unity (jalankan sekali)"**
→ tombol **Run workflow** → isi:

| Input | Isi |
|---|---|
| `unity_email` | email akun Unity kamu |
| `unity_password` | password akun Unity kamu |

> Password dienkripsi (libsodium sealed box) lalu disimpan sebagai *GitHub secret*,
> jadi tidak terlihat di log maupun di halaman repo.

⚠️ **Kalau akun Unity kamu pakai 2FA/OTP**, CI tidak bisa menjawab kodenya —
matikan 2FA sementara, atau buat akun Unity baru khusus untuk CI (gratis).

### 2. Jalankan build
Tab **Actions** → **"Build Warteg Simulator (APK + Windows)"** → **Run workflow**.

### 3. Unduh hasilnya
±20–40 menit kemudian buka run terakhir → bagian **Artifacts**:

| Artifact | Isi |
|---|---|
| `WartegSimulator-Android-APK` | `WartegSimulator.apk` → install ke HP |
| `WartegSimulator-Windows64` | `WartegSimulator.exe` + folder data → main di PC |

---

## Kalau build gagal — baca bagian ini

| Pesan di log | Arti & solusi |
|---|---|
| `Missing Unity License File and no Serial` | Workflow masih versi lama, atau `UNITY_EMAIL`/`UNITY_PASSWORD` belum diisi. Pastikan build.yml memakai `unity-builder@v6.0.0` (sudah) dan jalankan langkah 1. |
| `Licensing method: personal` lalu `2FA` / `two-factor` | Matikan 2FA sementara di akun Unity, build lagi. |
| `No free seat` / `seat` | Akun Unity kehabisan seat aktif. Keluar dari Unity Hub di komputer lain, atau tunggu beberapa menit, lalu build lagi. |
| `Android Build Support` | Sudah ditangani otomatis oleh image Docker game-ci (tidak perlu install manual). |
| Objek **pink** saat main | Jalankan menu **Warteg → Perbaiki Shader untuk Build** di editor lokal, atau biarkan — setup otomatis sudah mendaftarkan shader (hanya relevan kalau shader-nya tidak ditemukan). |
| Tombol UI tak merespons | `Project Settings → Player → Active Input Handling = Both` (di-set otomatis oleh ProjectSetup). |

Kalau masih gagal, kirim potongan log-nya ke saya — saya perbaiki workflow-nya.

---

## Alternatif kalau CI tetap tidak bisa dipakai

1. **Build lokal** (butuh Unity Hub + modul Android, ±10 GB):
   buka project → menu **Warteg → Build → Android APK**.
2. **Unity Cloud Build** (butuh akun Unity, gratis untuk Personal terbatas).
3. **Minta bantuan orang lain build**: kirim folder project, mereka cukup
   membuka di Unity dan menekan menu **Warteg → Build → Android APK**.

---

## Ringkasan status teknis (jujur)

| Item | Status |
|---|---|
| Source code (38 file C#) | ✅ kompilasi 0 error, 0 warning |
| Project Unity (scene, URP, Input System, save) | ✅ lengkap di repo |
| Pipeline build cloud | ✅ terpasang, butuh kredensial Unity kamu |
| APK sudah pernah di-build di sini? | ❌ tidak — sandbox tidak punya lisensi Unity & modul Android Linux |
| Playtest oleh pengguna | ⬜ belum — perlu APK hasil build kamu |
