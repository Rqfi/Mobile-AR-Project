import React, { useState, useEffect } from 'react';
import { initializeApp } from 'firebase/app';
import { getAuth, signInAnonymously } from 'firebase/auth';
import {
  getFirestore,
  collection,
  addDoc,
  deleteDoc,
  doc,
  onSnapshot,
  updateDoc
} from 'firebase/firestore';

// Konfigurasi Firebase (Otomatis dari google-services.json)
const firebaseConfig = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
  storageBucket: import.meta.env.VITE_FIREBASE_STORAGE_BUCKET,
  appId: import.meta.env.VITE_FIREBASE_APP_ID
};

// Inisialisasi Firebase (Tanpa Storage)
const app = initializeApp(firebaseConfig);
const auth = getAuth(app);
const db = getFirestore(app);

function App() {
  // State Data Katalog & UI
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(false);
  const [authReady, setAuthReady] = useState(false);
  const [toasts, setToasts] = useState([]);
  const [editingItem, setEditingItem] = useState(null);
  const [previewItem, setPreviewItem] = useState(null);

  // State Form Input (Menggunakan URL String)
  const [name, setName] = useState('');
  const [category, setCategory] = useState('Meja');
  const [description, setDescription] = useState('');
  const [width, setWidth] = useState('');
  const [depth, setDepth] = useState('');
  const [height, setHeight] = useState('');
  const [scale, setScale] = useState(1.0);
  const [thumbnailUrl, setThumbnailUrl] = useState('');
  const [modelUrl, setModelUrl] = useState('');

  // Helper untuk menampilkan toast notification
  const showToast = (message, type = 'info') => {
    const id = Date.now();
    setToasts(prev => [...prev, { id, message, type }]);
    setTimeout(() => {
      setToasts(prev => prev.filter(t => t.id !== id));
    }, 4000);
  };

  // Login Anonim & Listener Realtime Firestore
  useEffect(() => {
    signInAnonymously(auth)
      .then((userCredential) => {
        setAuthReady(true);
        showToast("Terhubung ke Firebase secara aman", "success");
      })
      .catch((error) => {
        console.error("Gagal login anonim:", error);
        showToast("Gagal terhubung ke Firebase: " + error.message, "error");
      });

    // Realtime Listener
    const unsubscribe = onSnapshot(collection(db, 'katalog'), (snapshot) => {
      const catalogData = [];
      snapshot.forEach((doc) => {
        catalogData.push({ id: doc.id, ...doc.data() });
      });
      setItems(catalogData);
    }, (error) => {
      console.error("Gagal mengambil data:", error);
      showToast("Gagal memuat katalog: " + error.message, "error");
    });

    return () => unsubscribe();
  }, []);

  // Handler Submit Form (Langsung simpan metrik ke Firestore)
  // Handler untuk mengisi form dengan data item yang akan diedit
  const handleStartEdit = (item) => {
    setEditingItem(item);
    setName(item.name || '');
    setCategory(item.category || 'Meja');
    setDescription(item.description || '');
    setWidth(item.width || '');
    setDepth(item.depth || '');
    setHeight(item.height || '');
    setScale(item.scale || 1.0);
    setThumbnailUrl(item.thumbnailUrl || '');
    setModelUrl(item.modelUrl || '');
    // Scroll otomatis ke atas agar form edit terlihat jelas
    window.scrollTo({ top: 0, behavior: 'smooth' });
  };

  // Handler untuk membatalkan proses edit dan mengosongkan form
  const handleCancelEdit = () => {
    setEditingItem(null);
    setName('');
    setCategory('Meja');
    setDescription('');
    setWidth('');
    setDepth('');
    setHeight('');
    setScale(1.0);
    setThumbnailUrl('');
    setModelUrl('');
  };

  // Handler Submit Form (Menyimpan item baru ATAU memperbarui item yang ada)
  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!authReady) {
      showToast("Tunggu koneksi Firebase selesai...", "error");
      return;
    }
    if (!thumbnailUrl || !modelUrl) {
      showToast("Harap isi URL Gambar dan URL Model 3D!", "error");
      return;
    }

    setLoading(true);
    showToast(editingItem ? "Memperbarui item..." : "Menyimpan ke Firestore...", "info");

    try {
      if (editingItem) {
        // Mode Edit: Perbarui data di Firestore
        await updateDoc(doc(db, 'katalog', editingItem.id), {
          name,
          category,
          description,
          width: parseFloat(width),
          depth: parseFloat(depth),
          height: parseFloat(height),
          scale: parseFloat(scale),
          thumbnailUrl,
          modelUrl
        });
        showToast("Item berhasil diperbarui!", "success");
      } else {
        // Mode Tambah: Simpan dokumen baru ke Firestore
        await addDoc(collection(db, 'katalog'), {
          name,
          category,
          description,
          width: parseFloat(width),
          depth: parseFloat(depth),
          height: parseFloat(height),
          scale: parseFloat(scale),
          thumbnailUrl,
          modelUrl
        });
        showToast("Item berhasil ditambahkan ke katalog!", "success");
      }

      // Reset form dan matikan mode edit
      handleCancelEdit();
      e.target.reset();

    } catch (error) {
      console.error("Error submit:", error);
      showToast("Gagal menyimpan item: " + error.message, "error");
    } finally {
      setLoading(false);
    }
  };


  // Handler Hapus Item
  const handleDelete = async (item) => {
    if (!window.confirm(`Hapus ${item.name} dari katalog?`)) return;

    showToast("Menghapus item...", "info");

    try {
      // Hapus Dokumen dari Firestore
      await deleteDoc(doc(db, 'katalog', item.id));
      showToast("Item berhasil dihapus!", "success");
    } catch (error) {
      console.error("Error delete:", error);
      showToast("Gagal menghapus item: " + error.message, "error");
    }
  };

  return (
    <div className="container">
      {/* Form Tambah Item */}
      <div className="card">
        <h2>{editingItem ? `Edit Item: ${editingItem.name}` : "Tambah Item Baru"}</h2>
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label>Nama Furnitur</label>
            <input
              type="text"
              className="form-control"
              placeholder="Contoh: Meja Belajar Kayu"
              value={name}
              onChange={(e) => setName(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label>Kategori</label>
            <select
              className="form-control"
              value={category}
              onChange={(e) => setCategory(e.target.value)}
              required
            >
              <option value="Meja">Meja</option>
              <option value="Kursi">Kursi</option>
              <option value="Sofa">Sofa</option>
              <option value="Lemari">Lemari</option>
              <option value="Kasur">Kasur</option>
              <option value="Lainnya">Lainnya</option>
            </select>
          </div>

          <div className="form-group">
            <label>Deskripsi</label>
            <textarea
              className="form-control"
              rows="3"
              placeholder="Deskripsi detail barang..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              required
            ></textarea>
          </div>

          <div className="form-group">
            <label>Dimensi Fisik (Lebar x Dalam x Tinggi - cm)</label>
            <div className="row-grid">
              <input
                type="number"
                className="form-control"
                placeholder="L"
                value={width}
                onChange={(e) => setWidth(e.target.value)}
                required
              />
              <input
                type="number"
                className="form-control"
                placeholder="D"
                value={depth}
                onChange={(e) => setDepth(e.target.value)}
                required
              />
              <input
                type="number"
                className="form-control"
                placeholder="H"
                value={height}
                onChange={(e) => setHeight(e.target.value)}
                required
              />
            </div>
          </div>

          <div className="form-group">
            <label>Skala Visual 3D di Unity</label>
            <input
              type="number"
              step="any"
              className="form-control"
              value={scale}
              onChange={(e) => setScale(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label>URL Link Gambar Thumbnail</label>
            <label>https://raw.githubusercontent.com/...</label>
            <input
              type="url"
              className="form-control"
              placeholder="https://raw.githubusercontent.com/.../gambar.png"
              value={thumbnailUrl}
              onChange={(e) => setThumbnailUrl(e.target.value)}
              required
            />
          </div>

          <div className="form-group">
            <label>URL Link Model 3D (.glb)</label>
            <label>https://raw.githubusercontent.com/...</label>
            <input
              type="url"
              className="form-control"
              placeholder="https://raw.githubusercontent.com/.../model.glb"
              value={modelUrl}
              onChange={(e) => setModelUrl(e.target.value)}
              required
            />
          </div>

          <div style={{ display: 'flex', gap: '10px' }}>
            <button type="submit" className="btn" disabled={loading}>
              {loading ? "Menyimpan..." : (editingItem ? "Perbarui Item" : "Simpan ke Katalog")}
            </button>
            {editingItem && (
              <button
                type="button"
                onClick={handleCancelEdit}
                className="btn"
                style={{ backgroundColor: '#b5b5c2ff' }}
              >
                Batal Edit
              </button>
            )}
          </div>

        </form>
      </div>

      {/* Daftar Katalog */}
      <div className="catalog-container">
        <div className="card" style={{ width: '95%' }}>
          <h2>Daftar Katalog Furnitur ({items.length})</h2>
          <div className="catalog-list">
            {items.map((item) => (
              <div className="catalog-card" key={item.id}>
                <div
                  className="catalog-thumb"
                  style={{ backgroundImage: `url(${item.thumbnailUrl})` }}
                >
                  {!item.thumbnailUrl && "Tidak ada gambar"}
                </div>
                <div className="catalog-info">
                  <div className="catalog-name">{item.name}</div>
                  <div className="catalog-meta">{item.category} (x{item.scale})</div>
                  <div className="catalog-desc">{item.description}</div>
                  <div className="catalog-specs">
                    <span>L: {item.width}cm</span>
                    <span>D: {item.depth}cm</span>
                    <span>T: {item.height}cm</span>
                  </div>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', marginTop: '10px' }}>
                    <button
                      onClick={() => setPreviewItem(item)}
                      className="btn"
                      style={{
                        margin: 0,
                        padding: '8px',
                        fontSize: '12px',
                        borderRadius: '6px',
                        backgroundColor: 'var(--color-primary)',
                        color: '#000',
                        border: 'none',
                        fontWeight: '600',
                      }}
                    >
                      Preview 3D
                    </button>
                    <div style={{ display: 'flex', gap: '8px' }}>
                      <button
                        onClick={() => handleStartEdit(item)}
                        className="btn"
                        style={{
                          margin: 0,
                          padding: '8px',
                          fontSize: '12px',
                          borderRadius: '6px',
                          backgroundColor: 'transparent',
                          color: 'var(--color-primary)',
                          border: '1px solid var(--color-primary)',
                          flex: 1,
                        }}
                        onMouseEnter={(e) => {
                          e.target.style.backgroundColor = 'var(--color-primary)';
                          e.target.style.color = '#000';
                        }}
                        onMouseLeave={(e) => {
                          e.target.style.backgroundColor = 'transparent';
                          e.target.style.color = 'var(--color-primary)';
                        }}
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(item)}
                        className="btn-delete"
                        style={{ padding: '8px', fontSize: '12px', borderRadius: '6px', flex: 1 }}
                      >
                        Hapus
                      </button>
                    </div>
                  </div>

                </div>
              </div>
            ))}
            {items.length === 0 && (
              <div style={{ gridColumn: '1/-1', textAlign: 'center', padding: '40px', color: 'var(--color-text-muted)' }}>
                Belum ada barang di katalog.
              </div>
            )}
          </div>
        </div>
      </div>

      {/* 3D Viewer Modal */}
      {previewItem && (
        <div style={{
          position: 'fixed',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: 'rgba(0, 0, 0, 0.8)',
          zIndex: 9999,
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          backdropFilter: 'blur(8px)',
          padding: '20px'
        }}>
          <div style={{
            backgroundColor: 'var(--color-surface)',
            borderRadius: '16px',
            border: '2px solid var(--color-primary)',
            padding: '24px',
            width: '100%',
            maxWidth: '640px',
            position: 'relative',
            boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.3)',
            display: 'flex',
            flexDirection: 'column',
            gap: '16px',
            boxSizing: 'border-box'
          }}>
            {/* Header Modal */}
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <div>
                <h3 style={{ margin: 0, fontSize: '1.25rem', color: 'var(--color-text)' }}>3D Viewer: {previewItem.name}</h3>
                <span style={{ fontSize: '0.8rem', color: 'var(--color-primary)', fontWeight: 'bold' }}>{previewItem.category} (Visual Scale: {previewItem.scale})</span>
              </div>
              <button
                onClick={() => setPreviewItem(null)}
                style={{
                  backgroundColor: 'transparent',
                  color: 'var(--color-text)',
                  border: 'none',
                  fontSize: '1.5rem',
                  cursor: 'pointer',
                  padding: '4px 8px',
                  borderRadius: '50%',
                  lineHeight: '1'
                }}
              >
                &times;
              </button>
            </div>
            {/* Model 3D Container */}
            <div style={{
              width: '100%',
              height: '380px',
              backgroundColor: '#e7e7e7',
              borderRadius: '8px',
              overflow: 'hidden',
              border: '1px solid var(--color-border)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              boxSizing: 'border-box'
            }}>
              <model-viewer
                src={previewItem.modelUrl}
                alt={`3D Model: ${previewItem.name}`}
                auto-rotate
                camera-controls
                interaction-prompt="none"
                shadow-intensity="1.5"
                environment-image="neutral"
                exposure="1.0"
                style={{ width: '100%', height: '100%', outline: 'none' }}
              >
              </model-viewer>
            </div>
            {/* Deskripsi & Detail Ukuran */}
            <div style={{ fontSize: '0.85rem', color: 'var(--color-text-muted)', lineHeight: '1.5' }}>
              <div style={{ marginBottom: '8px' }}>
                <strong>Deskripsi:</strong> {previewItem.description || "Tidak ada deskripsi."}
              </div>
              <div style={{ display: 'flex', gap: '16px', fontWeight: '600', backgroundColor: 'var(--color-bg)', padding: '8px 12px', borderRadius: '6px', border: '1px solid rgba(255, 200, 0, 0.1)' }}>
                <span>L: {previewItem.width} cm</span>
                <span>D: {previewItem.depth} cm</span>
                <span>T: {previewItem.height} cm</span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Toast Notification Container */}
      <div className="toast-container">
        {toasts.map(toast => (
          <div className={`toast ${toast.type}`} key={toast.id}>
            {toast.message}
          </div>
        ))}
      </div>
    </div>
  );
}

export default App;
