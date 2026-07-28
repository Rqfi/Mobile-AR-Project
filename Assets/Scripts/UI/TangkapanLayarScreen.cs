using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class TangkapanLayarScreen : MonoBehaviour
{
    private VisualElement _root;
    private TextField _inputNamaBaru;
    private TextField _inputNama;
    private TextField _inputCatatan;

    private VisualElement _dropdownTrigger;
    private VisualElement _dropdownList;
    private VisualElement _newProyekContainer;
    private Label _labelSelected;
    private Label _labelArrow;

    private bool _dropdownOpen = false;
    private bool _isNewProject = false;
    private string _selectedProyekId = null;
    private string _selectedProyekNama = null;

    private List<ProyekData> _existingProyek = new List<ProyekData>();

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;
        _root = uiDoc.rootVisualElement;

        _inputNamaBaru = _root.Q<TextField>("input-proyek-baru");
        _inputNama = _root.Q<TextField>("input-nama");
        _inputCatatan = _root.Q<TextField>("input-catatan");

        _dropdownTrigger = _root.Q<VisualElement>("proyek-dropdown-trigger");
        _dropdownList = _root.Q<VisualElement>("proyek-dropdown-list");
        _newProyekContainer = _root.Q<VisualElement>("new-proyek-container");
        _labelSelected = _root.Q<Label>("label-proyek-selected");
        _labelArrow = _root.Q<Label>("label-arrow");

        _dropdownTrigger?.RegisterCallback<ClickEvent>(_ => ToggleDropdown());

        _root.Q<Label>("btn-close")
             ?.RegisterCallback<ClickEvent>(_ => OnClose());
        _root.Q<VisualElement>("btn-simpan-galeri")
             ?.RegisterCallback<ClickEvent>(_ => OnSimpanGaleri());
        _root.Q<VisualElement>("btn-simpan-proyek")
             ?.RegisterCallback<ClickEvent>(_ => OnSimpanProyek());

        ResetDropdown();
        LoadScreenshotPreview();
        LoadExistingProyek();
    }

    private void ResetDropdown()
    {
        _dropdownOpen = false;
        _isNewProject = false;
        _selectedProyekId = null;
        _selectedProyekNama = null;

        if (_labelSelected != null)
        {
            _labelSelected.text = "Pilih atau buat proyek...";
            _labelSelected.AddToClassList("dropdown-trigger__placeholder");
        }
        if (_labelArrow != null) _labelArrow.text = "∨";
        _dropdownList?.SetDisplay(false);
        _newProyekContainer?.SetDisplay(false);
    }

    private async void LoadExistingProyek()
    {
        if (!FirebaseManager.Instance.IsReady)
        {
            // Tunggu sebentar lalu coba lagi
            await Task.Delay(2000);
            if (!FirebaseManager.Instance.IsReady) return;
        }

        try
        {
            _existingProyek = await FirebaseManager.Instance.GetAllProyekAsync();
            BuildDropdownItems();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TangkapanLayar] LoadProyek: {e.Message}");
        }
    }

    private void BuildDropdownItems()
    {
        if (_dropdownList == null) return;
        _dropdownList.Clear();

        // Item proyek yang sudah ada
        foreach (var proyek in _existingProyek)
        {
            var item = CreateDropdownItem(proyek.nama, false, proyek.id, proyek.nama);
            _dropdownList.Add(item);
        }

        // Item "+ Tambah Proyek"
        var addItem = new VisualElement();
        addItem.AddToClassList("dropdown-item");
        addItem.AddToClassList("dropdown-item--add");
        var addLabel = new Label("+ Tambah Proyek");
        addLabel.AddToClassList("dropdown-item__label");
        addItem.Add(addLabel);
        addItem.RegisterCallback<ClickEvent>(_ => OnAddNewProyek());
        _dropdownList.Add(addItem);
    }

    private VisualElement CreateDropdownItem(
        string displayText,
        bool isSelected,
        string proyekId,
        string proyekNama)
    {
        var item = new VisualElement();
        item.AddToClassList("dropdown-item");
        if (isSelected) item.AddToClassList("dropdown-item--selected");

        var label = new Label(displayText);
        label.AddToClassList("dropdown-item__label");
        item.Add(label);

        item.RegisterCallback<ClickEvent>(_ =>
            OnProyekSelected(proyekId, proyekNama));

        return item;
    }

    private void ToggleDropdown()
    {
        _dropdownOpen = !_dropdownOpen;

        _dropdownList?.SetDisplay(_dropdownOpen);
        if (_labelArrow != null)
            _labelArrow.text = _dropdownOpen ? "∧" : "∨";
    }

    private void OnProyekSelected(string id, string nama)
    {
        _selectedProyekId = id;
        _selectedProyekNama = nama;
        _isNewProject = false;

        if (_labelSelected != null)
        {
            _labelSelected.text = nama;
            _labelSelected.RemoveFromClassList("dropdown-trigger__placeholder");
        }

        _dropdownOpen = false;
        _dropdownList?.SetDisplay(false);
        _newProyekContainer?.SetDisplay(false);
        if (_labelArrow != null) _labelArrow.text = "∨";

        // Update highlight di list
        BuildDropdownItems();
    }

    private void OnAddNewProyek()
    {
        _isNewProject = true;
        _selectedProyekId = null;
        _selectedProyekNama = null;

        if (_labelSelected != null)
        {
            _labelSelected.text = "+ Proyek Baru";
            _labelSelected.RemoveFromClassList("dropdown-trigger__placeholder");
        }

        _dropdownOpen = false;
        _dropdownList?.SetDisplay(false);
        _newProyekContainer?.SetDisplay(true);
        if (_labelArrow != null) _labelArrow.text = "∨";
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
        // Tentukan nama proyek
        string namaProyek = "";

        if (_isNewProject)
        {
            namaProyek = _inputNamaBaru?.value?.Trim() ?? "";
            if (string.IsNullOrEmpty(namaProyek))
            {
                ShowToast("Masukkan nama proyek baru");
                return;
            }
        }
        else if (!string.IsNullOrEmpty(_selectedProyekNama))
        {
            namaProyek = _selectedProyekNama;
        }
        else
        {
            ShowToast("Pilih atau buat proyek terlebih dahulu");
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
            string projectId;

            if (_isNewProject)
            {
                // Buat proyek baru
                projectId = await FirebaseManager.Instance
                    .GetOrCreateProyekAsync(namaProyek);
            }
            else
            {
                // Pakai proyek yang sudah ada
                projectId = _selectedProyekId;
            }

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