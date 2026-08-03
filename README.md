# Admin Panel Katalog Furnitur AR

Konsol ini adalah web berbasis **React** yang berfungsi sebagai panel admin untuk mengelola data katalog furnitur (item 3D) yang disimpan di **Firebase Firestore**. Data yang dikelola di sini adalah sumber data yang dibaca oleh aplikasi Unity AR.

---

Gambaran Umum

Konsol ini memisahkan kepentingan pengelolaan konten (CMS) dari aplikasi Unity AR. Admin/content manager dapat menambah, mengedit, dan menghapus item furnitur — termasuk metadata, dimensi fisik, skala Unity, dan URL aset — tanpa menyentuh kode Unity sama sekali.

---

## Tech Stack


| Komponen          | Teknologi                         |
| ------------------- | ----------------------------------- |
| Framework UI      | React 18                          |
| Build Tool        | Vite                              |
| Database          | Firebase Firestore                |
| Autentikasi       | Firebase Anonymous Auth           |
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

## Catatan

### URL Aset (Thumbnail & Model 3D)

Konsol ini tidak menyediakan fitur upload file. URL thumbnail dan model 3D harus sudah tersedia di server yang dapat diakses publik. Beberapa opsi hosting aset:

- Firebase Storage
- Google Cloud Storage

Pastikan URL yang dimasukkan:

- Dapat diakses tanpa autentikasi (publik)
- Untuk model 3D: masih terdukung untuk format `.glb` (binary glTF), bukan `.gltf` atau `.fbx`

### Autentikasi Anonymous

Konsol maish menggunakan **Firebase Anonymous Authentication** untuk mengidentifikasi sesi. Ini berarti:

- Tidak ada login username/password
- Setiap sesi browser baru mendapat user ID anonim yang berbeda
- Data di Firestore collection `katalog` bersifat **global** (tidak per-user)

Konsol direncanakan menambahkan autentikasi email/password atau Google Sign-In dan menyesuaikan Firestore Security Rules.

### Relasi dengan Aplikasi Unity

Data di collection `katalog` adalah sumber untuk item furnitur. Kelas `FurnitureDatabase.cs` di branch utama Unity saat ini masih menggunakan data statis lokal, integrasi untuk membaca dari Firestore adalah langkah selanjutnya dalam pengembangan.
