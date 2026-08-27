import React, { useState, useEffect } from 'react';
import { collection, addDoc, deleteDoc, doc, onSnapshot, updateDoc } from 'firebase/firestore';
import ModelViewerModal from '../components/ModelViewerModel';
import '../styles/AdminPage.css';

function AdminPage({ db, handleLogout, showToast }) {
    const [items, setItems] = useState([]);
    const [loading, setLoading] = useState(false);
    const [editingItem, setEditingItem] = useState(null);
    const [previewItem, setPreviewItem] = useState(null);
    const [searchQuery, setSearchQuery] = useState('');
    const [filterCategory, setFilterCategory] = useState('Semua');

    const [isModalOpen, setIsModalOpen] = useState(false);

    const [name, setName] = useState('');
    const [category, setCategory] = useState('Meja');
    const [description, setDescription] = useState('');
    const [width, setWidth] = useState('');
    const [depth, setDepth] = useState('');
    const [height, setHeight] = useState('');
    const [scale, setScale] = useState(1.0);

    const [uploadMode, setUploadMode] = useState('link');
    const [thumbnailUrl, setThumbnailUrl] = useState('');
    const [modelUrl, setModelUrl] = useState('');
    const [thumbnailFile, setThumbnailFile] = useState(null);
    const [modelFile, setModelFile] = useState(null);

    const uploadToCloudinary = async (file, isModel3D) => {
        const resourceType = isModel3D ? 'raw' : 'image';
        const formData = new FormData();
        formData.append('file', file);
        formData.append('upload_preset', 'glb_preset');
        const response = await fetch(`https://api.cloudinary.com/v1_1/amdz3ibk/${resourceType}/upload`, {
            method: 'POST',
            body: formData
        });

        const data = await response.json();
        return data.secure_url;
    };

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
        setIsModalOpen(true);
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
        setThumbnailFile(null);
        setModelFile(null);
        setIsModalOpen(false);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setLoading(true);

        try {
            let finalThumbnailUrl = thumbnailUrl;
            let finalModelUrl = modelUrl;

            if (uploadMode === 'file') {
                if (thumbnailFile) {
                    showToast("Sedang mengunggah gambar ke Cloudinary...", "success");
                    finalThumbnailUrl = await uploadToCloudinary(thumbnailFile, false);
                }
                if (modelFile) {
                    showToast("Sedang mengunggah file 3D (.glb) ke Cloudinary...", "success");
                    finalModelUrl = await uploadToCloudinary(modelFile, true);
                }
            }

            const itemData = {
                name,
                category,
                description,
                width: Number(width),
                depth: Number(depth),
                height: Number(height),
                scale: Number(scale),
                thumbnailUrl: finalThumbnailUrl,
                modelUrl: finalModelUrl,
                updatedAt: new Date().toISOString()
            };

            if (editingItem) {
                await updateDoc(doc(db, 'katalog', editingItem.id), itemData);
                showToast("Berhasil diperbarui!", "success");
            } else {
                itemData.createdAt = new Date().toISOString();
                await addDoc(collection(db, 'katalog'), itemData);
                showToast("Furnitur berhasil ditambahkan ke katalog!", "success");
            }

            handleCancelEdit();
        } catch (error) {
            console.error("Error saving:", error);
            showToast("Gagal menyimpan data: " + error.message, "error");
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
        <div className="admin-wrapper">
            {/* Sidebar Kiri Baru */}
            <aside className="sidebar-new">
                <div className="sidebar-brand-new">
                    <span className="brand-text-primary">HOMEI</span>
                    <span className="brand-divider-new"></span>
                    <span className="brand-text-secondary">GLB Console</span>
                </div>
                <div className="sidebar-nav-new">
                    <div className="nav-item-new active">
                        <span className="material-icons-outlined nav-icon">inventory_2</span>
                        Catalog
                    </div>
                </div>
                <div className="sidebar-footer-new">
                    <button onClick={handleLogout} className="btn-logout-new">
                        <span className="material-icons-outlined nav-icon">logout</span>
                        Logout
                    </button>
                </div>
            </aside>

            {/* Konten Utama */}
            <main className="main-content-new">
                <div className="catalog-panel-new">
                    {/* Header Panel */}
                    <div className="panel-header-new">
                        <h2>Daftar Katalog Furnitur ({items.length})</h2>
                        <div className="panel-actions-new">
                            <button onClick={() => setIsModalOpen(true)} className="btn-add-new">
                                <span className="material-icons-outlined">add</span>
                                Tambah Item
                            </button>
                            <select
                                value={filterCategory}
                                onChange={(e) => setFilterCategory(e.target.value)}
                                className="select-filter-new"
                            >
                                <option value="Semua">Semua Kategori</option>
                                <option value="Meja">Meja</option>
                                <option value="Kursi">Kursi</option>
                                <option value="Sofa">Sofa</option>
                                <option value="Lemari">Lemari</option>
                                <option value="Kasur">Kasur</option>
                                <option value="Lainnya">Lainnya</option>
                            </select>
                            <div className="search-box-new">
                                <span className="material-icons-outlined search-icon">search</span>
                                <input
                                    type="text"
                                    placeholder="Cari nama..."
                                    value={searchQuery}
                                    onChange={(e) => setSearchQuery(e.target.value)}
                                />
                            </div>
                        </div>
                    </div>

                    {/* Grid Katalog */}
                    <div className="panel-body-new">
                        <div className="catalog-grid-new">
                            {items
                                .filter(item => filterCategory === 'Semua' || item.category === filterCategory)
                                .filter(item => item.name.toLowerCase().includes(searchQuery.toLowerCase()))
                                .map((item) => (
                                    <div className="card-item-new" key={item.id}>
                                        <div className="card-image-new">
                                            {item.thumbnailUrl ? (
                                                <img src={item.thumbnailUrl} alt={item.name} />
                                            ) : (
                                                <div className="no-image-new">No Image</div>
                                            )}
                                        </div>
                                        <div className="card-content-new">
                                            <div className="card-top-new">
                                                <div>
                                                    <h3 className="card-title-new">{item.name}</h3>
                                                    <span className="card-category-new">{item.category} (x{item.scale})</span>
                                                </div>
                                                <div className="card-actions-icons">
                                                    <button onClick={() => handleStartEdit(item)} className="icon-btn edit">
                                                        <span className="material-icons-outlined">edit</span>
                                                    </button>
                                                    <button onClick={() => handleDelete(item)} className="icon-btn delete">
                                                        <span className="material-icons-outlined">delete</span>
                                                    </button>
                                                </div>
                                            </div>
                                            <p className="card-desc-new">{item.description}</p>
                                            <div className="card-specs-new">
                                                <span>L: {item.width}cm</span>
                                                <span>D: {item.depth}cm</span>
                                                <span>T: {item.height}cm</span>
                                            </div>
                                            <button onClick={() => setPreviewItem(item)} className="btn-preview-new">
                                                <span className="material-icons-outlined">visibility</span>
                                                Preview 3D
                                            </button>
                                        </div>
                                    </div>
                                ))}
                            {items.length === 0 && (
                                <div className="empty-state-new">Belum ada barang di katalog.</div>
                            )}
                        </div>
                    </div>
                </div>
            </main>

            {/* === POP-UP MODAL FORM === */}
            {isModalOpen && (
                <div className="modal-overlay-new">
                    <div className="modal-box-new">
                        <div className="modal-header-new">
                            <h2>{editingItem ? `Edit Item: ${editingItem.name}` : "Tambah Item Baru"}</h2>
                            <button className="btn-close-modal" onClick={handleCancelEdit}>
                                <span className="material-icons-outlined">close</span>
                            </button>
                        </div>
                        <div className="modal-body-new">
                            <form onSubmit={handleSubmit} id="itemForm">
                                <div className="form-group">
                                    <label>Nama Furnitur</label>
                                    <input type="text" className="form-control" value={name} onChange={(e) => setName(e.target.value)} required />
                                </div>
                                <div className="form-group">
                                    <label>Kategori</label>
                                    <select className="form-control" value={category} onChange={(e) => setCategory(e.target.value)} required>
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
                                    <textarea className="form-control" rows="3" value={description} onChange={(e) => setDescription(e.target.value)} required></textarea>
                                </div>
                                <div className="form-group">
                                    <label>Dimensi Fisik (L x D x T - cm)</label>
                                    <div className="row-grid">
                                        <input type="number" className="form-control" placeholder="L" value={width} onChange={(e) => setWidth(e.target.value)} required />
                                        <input type="number" className="form-control" placeholder="D" value={depth} onChange={(e) => setDepth(e.target.value)} required />
                                        <input type="number" className="form-control" placeholder="H" value={height} onChange={(e) => setHeight(e.target.value)} required />
                                    </div>
                                </div>
                                <div className="form-group">
                                    <label>Skala Visual 3D</label>
                                    <input type="number" step="any" className="form-control" value={scale} onChange={(e) => setScale(e.target.value)} required />
                                </div>
                                <div className="form-group upload-mode-box">
                                    <label>Metode Input File</label>
                                    <select className="form-control" value={uploadMode} onChange={(e) => setUploadMode(e.target.value)}>
                                        <option value="link">Link URL</option>
                                        <option value="file">Upload File</option>
                                    </select>
                                </div>
                                {uploadMode === 'link' ? (
                                    <>
                                        <div className="form-group">
                                            <label>URL Link Gambar Thumbnail</label>
                                            <input type="url" className="form-control" value={thumbnailUrl} onChange={(e) => setThumbnailUrl(e.target.value)} required={!editingItem} />
                                        </div>
                                        <div className="form-group">
                                            <label>URL Link Model 3D (.glb)</label>
                                            <input type="url" className="form-control" value={modelUrl} onChange={(e) => setModelUrl(e.target.value)} required={!editingItem} />
                                        </div>
                                    </>
                                ) : (
                                    <>
                                        <div className="form-group">
                                            <label>Upload Gambar Thumbnail</label>
                                            <input type="file" accept="image/*" className="form-control file-input" onChange={(e) => setThumbnailFile(e.target.files[0])} required={!editingItem} />
                                        </div>
                                        <div className="form-group">
                                            <label>Upload Model 3D (.glb)</label>
                                            <input type="file" accept=".glb" className="form-control file-input" onChange={(e) => setModelFile(e.target.files[0])} required={!editingItem} />
                                        </div>
                                    </>
                                )}
                            </form>
                        </div>
                        <div className="modal-footer-new">
                            <button type="submit" form="itemForm" className="btn btn-save" disabled={loading}>
                                {loading ? "Menyimpan..." : (editingItem ? "Perbarui Item" : "Simpan Item")}
                            </button>
                            {editingItem && (
                                <button type="button" onClick={handleCancelEdit} className="btn btn-cancel">Batal</button>
                            )}
                        </div>
                    </div>
                </div>
            )}

            {/* 3D Viewer Modal */}
            <ModelViewerModal previewItem={previewItem} setPreviewItem={setPreviewItem} />
        </div>
    );
}

export default AdminPage;
