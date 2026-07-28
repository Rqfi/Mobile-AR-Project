using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;

public class DetailFotoScreen : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _previewLargeImage;
    private Label _labelNama;
    private Label _labelTanggal;
    private Label _labelCatatan;

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;
        _root = uiDoc.rootVisualElement;

        _previewLargeImage = _root.Q<VisualElement>("preview-large-image");
        _labelNama = _root.Q<Label>("label-nama-foto");
        _labelTanggal = _root.Q<Label>("label-tanggal-foto");
        _labelCatatan = _root.Q<Label>("label-catatan-foto");

        // Kembali ke Detail Proyek
        _root.Q<Label>("btn-back")
             ?.RegisterCallback<ClickEvent>(_ => ScreenManager.Instance.ShowDetailProyek());

        // Simpan ke Galeri
        _root.Q<VisualElement>("btn-simpan-galeri")
             ?.RegisterCallback<ClickEvent>(_ => OnSimpanKeGaleri());

        // Hapus Gambar
        _root.Q<VisualElement>("btn-hapus-gambar")
             ?.RegisterCallback<ClickEvent>(_ => OnHapusGambar());

        LoadScreenshotDetail();
    }

    private void LoadScreenshotDetail()
    {
        var ss = AppState.SelectedScreenshot;
        if (ss == null)
        {
            ScreenManager.Instance.ShowDetailProyek();
            return;
        }

        if (_labelNama != null) _labelNama.text = ss.nama;
        if (_labelTanggal != null) _labelTanggal.text = FormatTanggal(ss.tanggal);
        if (_labelCatatan != null) _labelCatatan.text = string.IsNullOrEmpty(ss.catatan) ? "-" : ss.catatan;

        if (!string.IsNullOrEmpty(ss.path) && File.Exists(ss.path))
        {
            var bytes = File.ReadAllBytes(ss.path);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            if (_previewLargeImage != null)
            {
                _previewLargeImage.style.backgroundImage = new StyleBackground(tex);
            }
        }
    }

    private void OnSimpanKeGaleri()
    {
        var ss = AppState.SelectedScreenshot;
        if (ss == null) return;

        string sourcePath = ss.path;
        string filename = Path.GetFileName(sourcePath);

        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
        {
            ShowToast("File tidak ditemukan");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            byte[] imageData = File.ReadAllBytes(sourcePath);
            string tempPath = Path.Combine(Application.temporaryCachePath, filename);
            File.WriteAllBytes(tempPath, imageData);

            using var mediaScannerConnection = new AndroidJavaClass("android.media.MediaScannerConnection");
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            mediaScannerConnection.CallStatic(
                "scanFile",
                activity,
                new string[] { tempPath },
                null,
                null);

            ShowToast("Gambar disimpan ke Galeri");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Gallery] Gagal simpan ke galeri: {e.Message}");
            ShowToast("Gagal menyimpan ke Galeri");
        }
#else
        Debug.Log("[Gallery] Simpan ke galeri hanya di Android device.");
        ShowToast("Gambar disimpan (Simulasi)");
#endif
    }

    private async void OnHapusGambar()
    {
        var ss = AppState.SelectedScreenshot;
        if (ss == null || string.IsNullOrEmpty(AppState.ActiveProjectId)) return;

        if (!FirebaseManager.Instance.IsReady)
        {
            ShowToast("Koneksi belum siap");
            return;
        }

        try
        {
            // 1. Hapus dari database Firebase
            await FirebaseManager.Instance.DeleteScreenshotAsync(AppState.ActiveProjectId, ss.id);

            // 2. Hapus file fisik secara lokal
            if (!string.IsNullOrEmpty(ss.path) && File.Exists(ss.path))
            {
                File.Delete(ss.path);
            }

            ShowToast("Gambar berhasil dihapus");

            // 3. Kembali ke detail proyek
            ScreenManager.Instance.ShowDetailProyek();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DetailFoto] HapusGambar: {e.Message}");
            ShowToast("Gagal menghapus gambar");
        }
    }

    private string FormatTanggal(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return "-";
        if (DateTime.TryParse(iso, out var dt))
            return dt.ToString("dd MMMM yyyy HH:mm");
        return iso;
    }

    private void ShowToast(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity    = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var toastClass  = new AndroidJavaClass("android.widget.Toast");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                toastClass.CallStatic<AndroidJavaObject>(
                    "makeText", activity, message,
                    toastClass.GetStatic<int>("LENGTH_SHORT"))
                    .Call("show");
            }));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Toast] " + e.Message);
        }
#else
        Debug.Log($"[Toast] {message}");
#endif
    }
}
