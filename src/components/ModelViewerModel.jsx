import React from 'react';

function ModelViewerModal({ previewItem, setPreviewItem }) {
    if (!previewItem) return null;

    return (
        <div style={{
            position: 'fixed', top: 0, left: 0, right: 0, bottom: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.8)', zIndex: 9999,
            display: 'flex', justifyContent: 'center', alignItems: 'center',
            backdropFilter: 'blur(8px)', padding: '20px'
        }}>
            <div style={{
                backgroundColor: 'var(--color-surface)', borderRadius: '16px',
                border: '2px solid var(--color-primary)', padding: '24px',
                width: '100%', maxWidth: '640px', position: 'relative',
                boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.3)',
                display: 'flex', flexDirection: 'column', gap: '16px',
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
                            backgroundColor: 'transparent', color: 'var(--color-text)', border: 'none',
                            fontSize: '1.5rem', cursor: 'pointer', padding: '4px 8px', borderRadius: '50%', lineHeight: '1'
                        }}
                    >
                        &times;
                    </button>
                </div>

                {/* Model 3D Container */}
                <div style={{
                    width: '100%', height: '380px', backgroundColor: '#e7e7e7', borderRadius: '8px',
                    overflow: 'hidden', border: '1px solid var(--color-border)',
                    display: 'flex', alignItems: 'center', justifyContent: 'center', boxSizing: 'border-box'
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
                    ></model-viewer>
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
    );
}

export default ModelViewerModal;
