import React, { useState, useEffect } from 'react';
import { collection, addDoc, deleteDoc, doc, onSnapshot, updateDoc } from 'firebase/firestore';
import ModelViewerModal from '../components/ModelViewerModel';

function AdminPage({ db, handleLogout, showToast }) {
    const [items, setItems] = useState([]);
    const [loading, setLoading] = useState(false);
    const [editingItem, setEditingItem] = useState(null);
    const [previewItem, setPreviewItem] = useState(null);
    const [searchQuery, setSearchQuery] = useState('');
    const [filterCategory, setFilterCategory] = useState('Semua');

    const [name, setName] = useState('');
    const [category, setCategory] = useState('Meja');
    const [description, setDescription] = useState('');
    const [width, setWidth] = useState('');
    const [depth, setDepth] = useState('');
    const [height, setHeight] = useState('');
    const [scale, setScale] = useState(1.0);
    const [thumbnailUrl, setThumbnailUrl] = useState('');
    const [modelUrl, setModelUrl] = useState('');

    useEffect(() => {
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
        window.scrollTo({ top: 0, behavior: 'smooth' });
    };

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

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (!thumbnailUrl || !modelUrl) {
            showToast("Harap isi URL Gambar dan URL Model 3D!", "error");
            return;
        }

        setLoading(true);
        showToast(editingItem ? "Memperbarui item..." : "Menyimpan ke Firestore...", "info");

        try {
            if (editingItem) {
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

            handleCancelEdit();
            e.target.reset();

        } catch (error) {
            console.error("Error submit:", error);
            showToast("Gagal menyimpan item: " + error.message, "error");
        } finally {
            setLoading(false);
        }
    };

    const handleDelete = async (item) => {
        if (!window.confirm(`Hapus ${item.name} dari katalog?`)) return;

        showToast("Menghapus item...", "info");

        try {
            await deleteDoc(doc(db, 'katalog', item.id));
            showToast("Item berhasil dihapus!", "success");
        } catch (error) {
            console.error("Error delete:", error);
            showToast("Gagal menghapus item: " + error.message, "error");
        }
    };

    return (
        <div className="admin-layout">
            {/* Navbar Layar Penuh Baru */}
            <nav className="top-navbar">
                <div className="nav-brand">
                    <span className="brand-primary">HOMEI</span>
                    <span className="brand-divider">|</span>
                    <span className="brand-secondary">GLB Console</span>
                </div>
                <button onClick={handleLogout} className="btn-logout-nav">
                    Logout
                </button>
            </nav>
            <div className="container">
                {/* Form Tambah Item */}
                <div className="sidebar-form">
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
                    <div className="card">
                        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px', paddingBottom: '15px', borderBottom: '1px solid var(--color-border)' }}>
                            <h2 style={{ margin: 0, borderBottom: 'none', paddingBottom: 0 }}>
                                Daftar Katalog Furnitur ({items.length})
                            </h2>

                            {/* Kotak Filter Kategori & Pencarian Nama */}
                            <div style={{ display: 'flex', gap: '10px' }}>
                                <select
                                    value={filterCategory}
                                    onChange={(e) => setFilterCategory(e.target.value)}
                                    style={{ padding: '10px 15px', borderRadius: '30px', border: '1px solid #d1cfc9', backgroundColor: '#f7f6f2', outline: 'none', cursor: 'pointer' }}
                                >
                                    <option value="Semua">Semua Kategori</option>
                                    <option value="Meja">Meja</option>
                                    <option value="Kursi">Kursi</option>
                                    <option value="Sofa">Sofa</option>
                                    <option value="Lemari">Lemari</option>
                                    <option value="Kasur">Kasur</option>
                                    <option value="Lainnya">Lainnya</option>
                                </select>
                                <div style={{ position: 'relative', width: '220px' }}>
                                    <input
                                        type="text"
                                        placeholder="Cari nama..."
                                        value={searchQuery}
                                        onChange={(e) => setSearchQuery(e.target.value)}
                                        style={{ width: '100%', padding: '10px 15px 10px 20px', borderRadius: '30px', border: '1px solid #d1cfc9', backgroundColor: '#f7f6f2', boxSizing: 'border-box', outline: 'none' }}
                                    />
                                </div>
                            </div>
                        </div>
                        <div className="catalog-list">
                            {items
                                .filter(item => filterCategory === 'Semua' || item.category === filterCategory)
                                .filter(item => item.name.toLowerCase().includes(searchQuery.toLowerCase()))
                                .map((item) => (
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
                <ModelViewerModal previewItem={previewItem} setPreviewItem={setPreviewItem} />

            </div>
        </div>
    );
}

export default AdminPage;
