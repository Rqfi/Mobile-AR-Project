using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;

public class DetailProyekScreen : MonoBehaviour
{
    private VisualElement _root;
    private string _currentNamaProyek;

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;
        _root = uiDoc.rootVisualElement;

        // Back
        _root.Q<Label>("btn-back")
             ?.RegisterCallback<ClickEvent>(_ =>
                 ScreenManager.Instance.ShowProyek());

        // Setting button (titik 3)
        _root.Q<Label>("btn-setting")
             ?.RegisterCallback<ClickEvent>(_ => ShowBottomSheet());

        // Bottom sheet
        _root.Q<VisualElement>("menu-backdrop")
             ?.RegisterCallback<ClickEvent>(_ => HideAll());
        _root.Q<VisualElement>("btn-edit-nama")
             ?.RegisterCallback<ClickEvent>(_ => ShowEditModal());
        _root.Q<VisualElement>("btn-hapus-proyek")
             ?.RegisterCallback<ClickEvent>(_ => ShowHapusModal());
        _root.Q<VisualElement>("btn-cancel-menu")
             ?.RegisterCallback<ClickEvent>(_ => HideAll());

        // Modal edit
        _root.Q<VisualElement>("btn-cancel-edit")
             ?.RegisterCallback<ClickEvent>(_ => HideAll());
        _root.Q<VisualElement>("btn-confirm-edit")
             ?.RegisterCallback<ClickEvent>(_ => OnConfirmEdit());

        // Modal hapus
        _root.Q<VisualElement>("btn-cancel-hapus")
             ?.RegisterCallback<ClickEvent>(_ => HideAll());
        _root.Q<VisualElement>("btn-confirm-hapus")
             ?.RegisterCallback<ClickEvent>(_ => OnConfirmHapus());

        if (!string.IsNullOrEmpty(AppState.ActiveProjectId))
        {
            LoadProyekDetail(AppState.ActiveProjectId);
            LoadScreenshots(AppState.ActiveProjectId);
        }
    }

    // ── Bottom Sheet ───────────────────────────────────

    private void ShowBottomSheet()
    {
        _root.Q<VisualElement>("menu-backdrop")?.SetDisplay(true);
        _root.Q<VisualElement>("bottom-sheet-menu")?.SetDisplay(true);
    }

    private void ShowEditModal()
    {
        HideAll();

        // Pre-fill nama saat ini
        var input = _root.Q<TextField>("input-edit-nama");
        if (input != null) input.value = _currentNamaProyek ?? "";

        _root.Q<VisualElement>("modal-edit")?.SetDisplay(true);
    }

    private void ShowHapusModal()
    {
        HideAll();
        _root.Q<VisualElement>("modal-hapus")?.SetDisplay(true);
    }

    private void HideAll()
    {
        _root.Q<VisualElement>("menu-backdrop")?.SetDisplay(false);
        _root.Q<VisualElement>("bottom-sheet-menu")?.SetDisplay(false);
        _root.Q<VisualElement>("modal-edit")?.SetDisplay(false);
        _root.Q<VisualElement>("modal-hapus")?.SetDisplay(false);
    }

    // ── Edit Nama ──────────────────────────────────────

    private async void OnConfirmEdit()
    {
        string namaBaru = _root.Q<TextField>("input-edit-nama")
            ?.value?.Trim() ?? "";

        if (string.IsNullOrEmpty(namaBaru))
        {
            ShowToast("Nama tidak boleh kosong");
            return;
        }

        if (namaBaru == _currentNamaProyek)
        {
            HideAll();
            return;
        }

        if (!FirebaseManager.Instance.IsReady) return;

        try
        {
            await FirebaseManager.Instance
                .UpdateProyekNamaAsync(AppState.ActiveProjectId, namaBaru);

            _currentNamaProyek = namaBaru;
            _root.Q<Label>("label-nama-proyek").text = namaBaru;

            HideAll();
            ShowToast("Nama proyek diperbarui");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DetailProyek] Edit: {e.Message}");
            ShowToast("Gagal memperbarui nama");
        }
    }

    // ── Hapus Proyek ───────────────────────────────────

    private async void OnConfirmHapus()
    {
        if (!FirebaseManager.Instance.IsReady) return;

        HideAll();

        try
        {
            await FirebaseManager.Instance
                .DeleteProyekAsync(AppState.ActiveProjectId);

            ShowToast("Proyek dihapus");
            ScreenManager.Instance.ShowProyek();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DetailProyek] Hapus: {e.Message}");
            ShowToast("Gagal menghapus proyek");
        }
    }

    // ── Load Data ──────────────────────────────────────

    private async void LoadProyekDetail(string projectId)
    {
        if (!FirebaseManager.Instance.IsReady) return;

        try
        {
            var doc = await FirebaseManager.Instance
                .GetProyekByIdAsync(projectId);

            if (doc == null) return;

            _currentNamaProyek = doc.nama;

            var labelNama = _root.Q<Label>("label-nama-proyek");
            var labelTanggal = _root.Q<Label>("label-tanggal");

            if (labelNama != null) labelNama.text = doc.nama;
            if (labelTanggal != null) labelTanggal.text = FormatTanggal(doc.tanggal);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DetailProyek] LoadDetail: {e.Message}");
        }
    }

    private async void LoadScreenshots(string projectId)
    {
        if (!FirebaseManager.Instance.IsReady) return;

        try
        {
            var screenshots = await FirebaseManager.Instance
                .GetScreenshotsAsync(projectId);

            var grid = _root.Q<VisualElement>("photo-grid");
            var empty = _root.Q<VisualElement>("photo-empty");

            grid?.Clear();

            if (screenshots == null || screenshots.Count == 0)
            {
                empty?.SetDisplay(true);
                grid?.SetDisplay(false);
                return;
            }

            empty?.SetDisplay(false);
            grid?.SetDisplay(true);

            foreach (var ss in screenshots)
                grid?.Add(CreatePhotoItem(ss));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DetailProyek] LoadScreenshots: {e.Message}");
        }
    }

    private VisualElement CreatePhotoItem(ScreenshotData ss)
    {
        var item = new VisualElement();
        item.AddToClassList("photo-item");
        if (!string.IsNullOrEmpty(ss.path) && File.Exists(ss.path))
        {
            var bytes = File.ReadAllBytes(ss.path);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            item.style.backgroundImage = new StyleBackground(tex);
            item.style.backgroundSize = new StyleBackgroundSize(
                new BackgroundSize(BackgroundSizeType.Cover));
        }
        else
        {
            var icon = new Label("📷");
            icon.AddToClassList("photo-item__icon");
            item.Add(icon);
        }

        var nama = new Label(ss.nama);
        nama.style.fontSize = 18;
        nama.style.color = new UnityEngine.Color(1, 1, 1, 0.8f);
        nama.style.position = Position.Absolute;
        nama.style.bottom = 8;
        nama.style.left = 8;
        item.Add(nama);

        item.RegisterCallback<ClickEvent>(_ => OnPhotoClick(ss));
        return item;
    }

    private void OnPhotoClick(ScreenshotData ss)
    {
        AppState.SelectedScreenshot = ss;
        ScreenManager.Instance.ShowDetailFoto();
    }

    private string FormatTanggal(string iso)
    {
        if (string.IsNullOrEmpty(iso)) return "-";
        if (DateTime.TryParse(iso, out var dt))
            return dt.ToString("dd MMMM yyyy");
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