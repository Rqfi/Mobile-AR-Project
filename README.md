# AR Interior Design

> Aplikasi media promosi desain interior berbasis Augmented Reality untuk Android

![Unity](https://img.shields.io/badge/Unity-6000.3.19f1-black?logo=unity)
![Platform](https://img.shields.io/badge/Platform-Android-green?logo=android)
![AR Foundation](<https://img.shields.io/badge/AR%20Foundation-6.3.5-blue>)
![Firebase](https://img.shields.io/badge/Firebase-Firestore-orange?logo=firebase)

Aplikasi AR yang memungkinkan pengguna melihat furnitur secara virtual di ruangan nyata menggunakan kamera smartphone. Dikembangkan sebagai Tugas Akhir di Politeknik Elektronika Negeri Surabaya Program Studi Multimedia Broadcasting.

---

## Fitur

- **Katalog Furnitur** — Browse item furnitur dengan filter kategori (Meja, Kursi, Sofa, Lemari) dan pencarian
- **AR Placement** — Tempatkan furnitur virtual di ruangan nyata menggunakan ARCore
- **Screenshot** — Ambil tangkapan layar hasil AR dan simpan ke galeri
- **Manajemen Proyek** — Kelompokkan screenshot ke dalam proyek dan simpan ke cloud via Firebase

---

## Tech Stack


| Komponen        | Teknologi                                 |
| ----------------- | ------------------------------------------- |
| Game Engine     | Unity 6000.3.19f1 LTS                     |
| AR Framework    | AR Foundation 6.3.5 + ARCore XR Plugin    |
| UI System       | UI Toolkit (UXML/USS) + uGUI (AR overlay) |
| Database        | Firebase Cloud Firestore                  |
| Authentication  | Firebase Anonymous Auth                   |
| Target Platform | Android (min API 26 / Android 8.0)        |
| Perangkat Uji   | Vivo V2550, Android 16 (API 36), ARM64    |

---

## Arsitektur

### Struktur Scene

```
Main.unity
└── Semua screen UI dikelola ScreenManager
    ├── Dashboard      — Beranda & entrypoint
    ├── Katalog        — Browse furnitur
    ├── Detail Furnitur — Info produk & CTA ke AR
    ├── Proyek         — Riwayat proyek
    ├── Detail Proyek  — Galeri screenshot per proyek
    └── Tangkapan Layar — Simpan & anotasi hasil AR

ARSession.unity
└── AR camera feed + UI overlay (uGUI Canvas)
    ├── Plane detection visualization
    ├── Placement reticle
    ├── Furniture selector panel
    └── Screenshot button
```

### Navigasi

Stack-based navigation menggunakan `ScreenManager` dengan history stack.

```
Dashboard → Katalog → Detail Furnitur → AR Session
    ↓                                       ↓
  Proyek → Detail Proyek          Tangkapan Layar
```

### Hybrid UI Architecture

```
Scene Main      → UI Toolkit (UXML/USS)
Scene ARSession → uGUI Canvas
```

Dua sistem UI tidak aktif bersamaan karena scene management memastikan hanya satu scene yang loaded pada satu waktu.

---

## Setup & Installation

### Prerequisites

- Unity Hub + Unity 6000.3.19f1 (dengan AR Mobile template)
- Android Build Support + NDK + OpenJDK
- Android SDK API 26+
- Firebase project dengan Firestore & Anonymous Auth aktif
- Perangkat Android dengan ARCore support

**Import Firebase SDK**

Download Firebase Unity SDK dari https://firebase.google.com/docs/unity/setup lalu import:
Assets → Import Package → Custom Package
Import file yang diperlukan:

- `FirebaseAuth.unitypackage`
- `FirebaseFirestore.unitypackage`
- `FirebaseAnalytics.unitypackage`

## Known Issues & Limitations


| Issue                               | Status      | Keterangan                                        |
| ------------------------------------- | ------------- | --------------------------------------------------- |
| Model 3D placeholder                | In Progress | Menunggu aset furnitur final                      |
| Fitur Proyek tidak restore AR state | By Design   | Hanya menyimpan screenshot, bukan posisi furnitur |
| Firebase cold start delay           | Known       | Firebase butuh ~2-3 detik inisialisasi            |

---

## ARCore Requirements

Aplikasi membutuhkan device dengan:

- ARCore support (cek di [ARCore supported devices](https://developers.google.com/ar/devices))
- Kamera belakang
- Android 8.0 (API 26) ke atas

Fitur yang digunakan:

- Horizontal & Vertical Plane Detection
- Environment Depth (Occlusion)
- AR Anchors

---

## Acknowledgements

- [AR Foundation Documentation](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.0/)
- [Firebase Unity SDK](https://firebase.google.com/docs/unity/setup)
- [Unity UI Toolkit Manual](https://docs.unity3d.com/Manual/UIElements.html)
- PT Homei Teknologi Indonesia — client & stakeholder proyek

---

## Lisensi

Proyek ini dikembangkan untuk keperluan akademik (Tugas Akhir). Semua hak cipta aset furnitur milik PT Homei Teknologi Indonesia selaku mitra.
