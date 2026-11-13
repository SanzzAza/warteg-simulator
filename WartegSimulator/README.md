# Warteg Simulator

Game simulasi bisnis warteg Indonesia yang dibuat dengan **Unity + C# + URP**.
Pemain memulai dari warteg kecil bermodal **Rp2.000.000**, membeli bahan, memasak,
menentukan harga, melayani pelanggan, membayar gaji & tagihan, lalu mengembangkan
warteg menjadi restoran besar.

Target platform: **Android** dan **Windows**.

---

## 1. Cara Membuka Project

1. Instal **Unity Hub** lalu Unity **2022.3 LTS** (project dibuat untuk 2022.3.62f1).
2. Buka folder project ini lewat Unity Hub → **Add** → pilih folder `WartegSimulator`.
3. Tunggu proses import selesai. Package berikut akan terpasang otomatis
   (lihat `Packages/manifest.json`):
   - `com.unity.render-pipelines.universal` 14.0.11 (URP)
   - `com.unity.inputsystem` 1.7.0
   - `com.unity.ugui` 1.0.0
4. Saat pertama dibuka, script editor `ProjectSetup` berjalan otomatis
   (`[InitializeOnLoad]`): membuat scene `Assets/Scenes/Main.unity`, mengisi
   Build Settings, membuat URP asset, dan menerapkan Player Settings.
   Jika perlu diulang: menu **Warteg → Jalankan Setup Project**.
5. Buka scene **Main**, tekan **Play**. Game langsung berjalan — seluruh dunia,
   karakter, dan UI dibuat secara prosedural oleh `GameBootstrap` saat runtime,
   jadi tidak perlu prefab manual.

> Catatan: scene `Main.unity` sudah disertakan di project, jadi langkah 4
> hanya diperlukan bila scene terhapus (menu **Warteg → Buat Ulang Scene Main**).

## 2. Cara Bermain (Ringkasan Loop)

1. **Main Menu** → *Play Game* (game baru) atau *Continue* (lanjut save terakhir).
2. Beli bahan mentah di tab **Pasar** (Gudang), lalu masak di dapur:
   klik kompor/etalase di dunia 3D atau lewat tab **Inventaris** → pilih resep →
   atur jumlah porsi → masak (ada progress bar + animasi).
3. Makanan matang masuk ke **etalase**. Makanan punya masa segar (±12 jam game);
   yang basi otomatis tidak bisa dijual dan bisa dibuang.
4. Pelanggan datang otomatis sesuai jam, cuaca, dan reputasi. Mereka duduk,
   memesan, menunggu, membayar, lalu pergi. Pelayanan lambat = pelanggan kecewa.
5. Atur **harga jual** di tab Menu — terlalu mahal mengurangi pelanggan,
   terlalu murah mengurangi keuntungan.
6. Rekrut karyawan (Koki, Kasir, Pelayan, Petugas Kebersihan) di tab **Karyawan**.
7. Beli upgrade (kompor, meja, etalase, kulkas, perluasan, dll.) di tab **Upgrade**.
8. Setiap hari ditutup dengan **laporan keuangan harian** (omzet, pengeluaran,
   gaji, listrik, air, sewa mingguan, keuntungan bersih).
9. Naikkan **level & reputasi** lewat makanan enak, pelayanan cepat, harga wajar,
   dan warteg bersih. Level membuka menu, peralatan, karyawan, dan upgrade baru.
10. Progres tersimpan **otomatis** (save lokal JSON) — keluar dan masuk lagi,
    *Continue* akan melanjutkan dari posisi terakhir.

Kontrol: klik/tap untuk jalan & berinteraksi, **Q/E** atau tombol HUD untuk
memutar kamera, scroll/pinch untuk zoom, **Esc** untuk menu pause.

## 3. Cara Build

> **APK belum tersedia di paket ini.** Build APK/EXE butuh Unity Editor berlisensi
> + modul Android, yang tidak tersedia di lingkungan pembuatan game ini
> (sudah diuji: editor Unity 2022.3.62f3 berhasil diunduh & dijalankan di sini,
> tetapi berhenti di `No valid Unity Editor license found`, dan modul Android
> untuk Linux tidak dipublikasikan Unity). Panduan lengkap langkah-per-langkah
> untuk menghasilkan APK ada di **[BUILD-APK.md](BUILD-APK.md)** — termasuk
> build otomatis lewat GitHub Actions tanpa perlu memasang Unity.

### Lewat Editor (paling mudah)
- Menu **Warteg → Build → Android AAB (Play Store)** juga tersedia
- Menu **Warteg → Build → Windows 64-bit** → hasil di `Builds/Windows/WartegSimulator.exe`
- Menu **Warteg → Build → Android APK** → hasil di `Builds/Android/WartegSimulator.apk`
  (pastikan Android Build Support terinstal di Unity Hub)

### Lewat Command Line
```bash
# Windows
Unity -batchmode -quit -projectPath WartegSimulator \
  -executeMethod Warteg.EditorTools.BuildScript.BuildWindowsCLI

# Android
Unity -batchmode -quit -projectPath WartegSimulator \
  -executeMethod Warteg.EditorTools.BuildScript.BuildAndroidCLI
```
Atau jalankan helper `Tools/build.sh` (atur path Unity di dalam file).

## 4. Struktur Project

```
Assets/
├── Scenes/Main.unity            Scene utama (kamera + matahari + bootstrap)
├── Scripts/
│   ├── Core/                    Data, katalog menu/upgrade, save, waktu,
│   │                            ekonomi, progres, audio, bootstrap
│   ├── World/                   Builder dunia 3D, material, karakter, FX
│   ├── Gameplay/                Pelanggan, karyawan, dapur, etalase, upgrade
│   ├── UI/                      HUD, menu manajemen, main menu, laporan harian
│   └── Editor/                  Setup otomatis project + script build
├── Settings/                    (dibuat otomatis: URP asset)
Packages/manifest.json           URP 14, Input System 1.7, uGUI 1.0
ProjectSettings/ProjectVersion.txt
```

Semua aset 3D (warteg, dapur, meja, etalase, karakter, makanan) dibuat
prosedural dari kode — tidak ada asset eksternal, jadi project bisa langsung
dibuka tanpa download tambahan selain package Unity.

## 5. Sistem yang Diimplementasikan

- **Ekonomi Rupiah lengkap**: saldo, ledger pemasukan/pengeluaran, gaji,
  listrik, air, sewa mingguan, perawatan, laporan harian, dashboard keuangan.
- **18 menu** (13 makanan + 5 minuman) sesuai daftar: harga modal, harga jual
  (bisa diubah pemain), waktu masak, popularitas, kepuasan, resep bahan.
- **Memasak**: progress bar, animasi, kontrol porsi, kesegaran & kebasian,
  etalase, gudang bahan mentah.
- **6 tipe pelanggan** (kantoran, anak sekolah, mahasiswa, ojek online, warga,
  keluarga) dengan uang, selera, kesabaran, kepuasan, dan AI lengkap
  (masuk → duduk → pesan → tunggu → bayar → keluar / kabur bila kesal).
- **4 peran karyawan** dengan nama, gaji, kecepatan, pengalaman, skill.
- **18 upgrade**: kompor, meja, etalase, kulkas, peralatan, dapur modern,
  lampu, dekorasi, kasir, gudang, wastafel, banner, perluasan, restoran.
- **Waktu & cuaca**: pagi/siang/sore/malam memengaruhi tipe pelanggan;
  hujan/cerah/mendung memengaruhi jumlah pelanggan.
- **Level & reputasi** dengan XP dan unlock bertahap.
- **Audio prosedural**: SFX transaksi/masak, ambience, BGM santai (tanpa file audio).
- **Save otomatis** lokal (JSON) + backup + perbaikan data rusak.
- **UI lengkap**: Main Menu, HUD, 9 tab manajemen, laporan harian;
  mendukung mouse & touchscreen.

## 6. Catatan Verifikasi (Jujur)

- Seluruh source C# (`Assets/Scripts`, termasuk script Editor) telah
  **terkompilasi bersih: 0 error, 0 warning** memakai harness kompilasi
  berbasis assembly Unity asli + stub uGUI/Input System/Editor.
- Environment pengembangan ini **tidak memiliki Unity Editor**, sehingga
  build APK/EXE dan playtest interaktif **belum dapat dijalankan di sini**.
  Untuk menghasilkan build yang bisa dimainkan, buka project di Unity
  2022.3 LTS lalu gunakan menu **Warteg → Build** (lihat bagian 3).
- Semua fitur di atas adalah implementasi nyata (bukan placeholder/mockup),
  tetapi hasil build tetap perlu diuji di perangkat target sebelum rilis.
