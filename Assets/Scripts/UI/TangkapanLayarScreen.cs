using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;
using System.Threading.Tasks;

public class TangkapanLayarScreen : MonoBehaviour
{
    private VisualElement _root;
    private TextField _inputProyek;
    private TextField _inputNama;
    private TextField _inputCatatan;

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;
        _root = uiDoc.rootVisualElement;

        _inputProyek = _root.Q<TextField>("input-proyek");
        _inputNama = _root.Q<TextField>("input-nama");
        _inputCatatan = _root.Q<TextField>("input-catatan");

        _root.Q<Label>("btn-close")
             ?.RegisterCallback<ClickEvent>(_ => OnClose());

        _root.Q<VisualElement>("btn-simpan-galeri")
             ?.RegisterCallback<ClickEvent>(_ => OnSimpanGaleri());

        _root.Q<VisualElement>("btn-simpan-proyek")
             ?.RegisterCallback<ClickEvent>(_ => OnSimpanProyek());

        LoadScreenshotPreview();
    }

    private void LoadScreenshotPreview()
    {
        var preview = _root.Q<VisualElement>("preview-screenshot");
        var placeholder = _root.Q<Label>("preview-placeholder");

        if (AppState.LastScreenshotTexture != null && preview != null)
        {
            preview.style.backgroundImage = new StyleBackground(
                AppState.LastScreenshotTexture);
            preview.style.backgroundSize = new StyleBackgroundSize(
                new BackgroundSize(BackgroundSizeType.Cover));

            if (placeholder != null)
                placeholder.style.display = DisplayStyle.None;
        }
    }

    private void OnClose()
    {
        ClearScreenshotState();
        ScreenManager.Instance.ShowDashboard();
    }

    private void OnSimpanGaleri()
    {
        ShowToast("Screenshot tersimpan di galeri");
        ClearScreenshotState();
        ScreenManager.Instance.ShowDashboard();
    }

    private async void OnSimpanProyek()
    {
        string namaProyek = _inputProyek?.value?.Trim() ?? "";

        if (string.IsNullOrEmpty(namaProyek))
        {
            ShowToast("Nama proyek tidak boleh kosong");
            return;
        }

        if (!FirebaseManager.Instance.IsReady)
        {
            ShowToast("Koneksi belum siap, coba lagi");
            return;
        }

        string namaTangkapan = _inputNama?.value?.Trim() ?? "";
        string catatan = _inputCatatan?.value?.Trim() ?? "";

        if (string.IsNullOrEmpty(namaTangkapan))
            namaTangkapan = $"AR_{DateTime.Now:yyyyMMdd_HHmmss}";

        var btnSimpan = _root.Q<VisualElement>("btn-simpan-proyek");
        if (btnSimpan != null) btnSimpan.SetEnabled(false);

        try
        {
            string projectId = await FirebaseManager.Instance
                .GetOrCreateProyekAsync(namaProyek);

            if (projectId == null)
            {
                ShowToast("Gagal menyimpan, coba lagi");
                return;
            }

            await FirebaseManager.Instance.AddScreenshotAsync(
                projectId,
                namaTangkapan,
                catatan,
                AppState.LastScreenshotPath ?? "");

            ShowToast("Tersimpan ke proyek: " + namaProyek);
            ClearScreenshotState();
            ScreenManager.Instance.ShowProyek();
        }
        catch (Exception e)
        {
            Debug.LogError($"[TangkapanLayar] {e.Message}");
            ShowToast("Terjadi kesalahan");
        }
        finally
        {
            if (btnSimpan != null) btnSimpan.SetEnabled(true);
        }
    }

    private void ClearScreenshotState()
    {
        if (AppState.LastScreenshotTexture != null)
        {
            Destroy(AppState.LastScreenshotTexture);
            AppState.LastScreenshotTexture = null;
        }
        AppState.LastScreenshotPath = null;
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