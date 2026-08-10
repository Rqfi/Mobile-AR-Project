# Admin Panel Katalog Furnitur AR

Konsol ini adalah web berbasis **React** yang berfungsi sebagai panel admin untuk mengelola data katalog furnitur (item 3D) yang disimpan di **Firebase Firestore**. Data yang dikelola di sini adalah sumber data yang dibaca oleh aplikasi Unity AR.

---

Gambaran Umum

Konsol ini memisahkan kepentingan pengelolaan konten (CMS) dari aplikasi Unity AR. Admin/content manager dapat menambah, mengedit, dan menghapus item furnitur termasuk metadata, dimensi fisik, skala Unity, dan URL aset tanpa menyentuh kode Unity sama sekali.

---

## Tech Stack


| Komponen          | Teknologi                         |
| ------------------- | ----------------------------------- |
| Framework UI      | React 18                          |
| Build Tool        | Vite                              |
| Database          | Firebase Firestore                |
| Autentikasi       | Firebase Email & Password         |
| Storage (Aset)    | Cloudinary API (Unsigned)         |
| State Management  | React Hooks (useState, useEffect) |
| Real-time Updates | Firestore`onSnapshot`             |

---

## Fitur

### CRUD Katalog Furnitur


| Operasi    | Keterangan                                      |
| ------------ | ------------------------------------------------- |
| **Create** | Tambah item furnitur baru                       |
| **Read**   | Tampil real-time dari Firestore via`onSnapshot` |
| **Update** | Edit item                                       |
| **Delete** | Hapus item                                      |

---

## Struktur Data Firestore

### Collection: `katalog`

```
katalog/
  └── {documentId}  
        ├── name          : string   — Nama furnitur (misal: "Meja Makan Minimalis")
        ├── category      : string   — Kategori: "Meja" | "Kursi" | "Sofa" | "Lemari" | "Kasur" | "Lainnya"
        ├── description   : string   — Deskripsi teks bebas
        ├── width         : number   — Lebar fisik dalam cm
        ├── depth         : number   — Kedalaman fisik dalam cm
        ├── height        : number   — Tinggi fisik dalam cm
        ├── scale         : number   — Faktor skala visual model 3D di Unity (misal: 1.0)
        ├── thumbnailUrl  : string   — URL gambar thumbnail (HTTPS)
        └── modelUrl      : string   — URL file model 3D format .glb (HTTPS)
```

**Contoh dokumen:**

```json
{
  "name": "Meja Makan Minimalis",
  "category": "Meja",
  "description": "Meja makan minimalis 4 orang, kayu solid jati.",
  "width": 120,
  "depth": 80,
  "height": 75,
  "scale": 1.0,
  "thumbnailUrl": "https://storage.example.com/furnitur/meja-makan.png",
  "modelUrl": "https://storage.example.com/furnitur/meja-makan.glb"
}
```

> **Catatan field `scale`:** Nilai ini digunakan Unity untuk menyesuaikan ukuran model 3D agar sesuai dengan dimensi fisik yang diinputkan. Nilai `1.0` jika model `.glb` sudah dalam skala meter yang benar. Nilai dapat disesuaikan jika model terlalu besar/kecil.

---

## Prasyarat

- **Node.js** versi 18 atau lebih baru
- **npm** versi 8 atau lebih baru (atau `pnpm` / `yarn`)
- Akses ke **Firebase project** yang sama dengan yang digunakan aplikasi Unity AR

---

## Sistem

### 1. Manajemen Aset (Thumbnail & File 3D)

Sistem dasbor ini mendukung dua mode penginputan file untuk fleksibilitas maksimal:

1. Gunakan Link URL: Anda dapat menempelkan *raw link* langsung dari layanan *hosting* publik tak terbatas seperti GitHub (contoh: `https://raw.githubusercontent.com/...`).
2. Upload File Langsung: Terintegrasi penuh dengan Cloudinary API (Unsigned Upload). Admin dapat memilih file gambar (JPG/PNG) dan 3D (`.glb`) langsung dari komputer, sistem akan otomatis mengunggahnya ke server Cloudinary, lalu menyimpan *secure URL*-nya ke dalam Firestore secara real-time.
   Syarat Model 3D: Format yang didukung wajib `.glb` (binary glTF) agar ringan, optimal, dan terstandardisasi saat dibaca oleh mesin AR Unity di HP.

### 2. Autentikasi & Keamanan Sesi

Konsol ini telah diamankan menggunakan Firebase Email & Password Authentication (*Production Grade*).

### Relasi dengan Aplikasi Unity

Data di collection `katalog` adalah sumber untuk item furnitur. Kelas `FurnitureDatabase.cs` di branch utama Unity saat ini masih menggunakan data statis lokal, integrasi untuk membaca dari Firestore adalah langkah selanjutnya dalam pengembangan.
