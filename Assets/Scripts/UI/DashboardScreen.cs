using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class DashboardScreen : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _previewKatalog;
    private VisualElement _proyekList;
    private VisualElement _proyekEmptyState;
    private bool _scrollSetupDone = false;

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        _root = uiDoc.rootVisualElement;
        _previewKatalog = _root.Q<VisualElement>("preview-katalog");
        _proyekList = _root.Q<VisualElement>("proyek-list");
        _proyekEmptyState = _root.Q<VisualElement>("proyek-empty-state");

        _root.Q<VisualElement>("card-hero")
             ?.RegisterCallback<ClickEvent>(OnHeroClick);

        _root.Q<Label>("btn-lihat-semua-katalog")
             ?.RegisterCallback<ClickEvent>(OnLihatKatalog);

        _root.Q<Label>("btn-lihat-semua-proyek")
             ?.RegisterCallback<ClickEvent>(OnLihatProyek);

        if (!_scrollSetupDone)
        {
            var scrollPreview = _root.Q<ScrollView>("scroll-preview-katalog");
            if (scrollPreview != null)
            {
                SetupManualHorizontalScroll(scrollPreview);
                _scrollSetupDone = true;
            }
        }

        PopulateKatalogPreview();
        LoadProyekPreview();
    }

    private void OnDisable()
    {
        // Unregister named callbacks
        _root?.Q<VisualElement>("card-hero")
              ?.UnregisterCallback<ClickEvent>(OnHeroClick);
        _root?.Q<Label>("btn-lihat-semua-katalog")
              ?.UnregisterCallback<ClickEvent>(OnLihatKatalog);
        _root?.Q<Label>("btn-lihat-semua-proyek")
              ?.UnregisterCallback<ClickEvent>(OnLihatProyek);

        // Reset flag agar scroll di-setup ulang dengan element baru
        _scrollSetupDone = false;
    }

    private void OnHeroClick(ClickEvent evt) =>
        SceneManager.LoadScene("ARSession");

    private void OnLihatKatalog(ClickEvent evt) =>
        ScreenManager.Instance.ShowKatalog();

    private void OnLihatProyek(ClickEvent evt) =>
        ScreenManager.Instance.ShowProyek();

    private void SetupManualHorizontalScroll(ScrollView scrollView)
    {
        Vector2 pointerStart = Vector2.zero;
        float scrollStart = 0f;
        bool isDragging = false;
        int activePointerId = -1;
        const float dragThreshold = 12f; // pixel — di bawah ini dianggap tap

        scrollView.RegisterCallback<PointerDownEvent>(evt =>
        {
            pointerStart = evt.position;
            scrollStart = scrollView.scrollOffset.x;
            isDragging = false;
            activePointerId = evt.pointerId;
            // Tidak StopPropagation di sini — biarkan ClickEvent tetap bisa jalan
        }, TrickleDown.TrickleDown);

        scrollView.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (evt.pointerId != activePointerId) return;

            float moved = Mathf.Abs(evt.position.x - pointerStart.x);

            if (!isDragging && moved > dragThreshold)
            {
                // Threshold terlewati → ini drag, bukan tap
                isDragging = true;
                scrollView.CapturePointer(evt.pointerId);
            }

            if (isDragging)
            {
                float delta = pointerStart.x - evt.position.x;
                scrollView.scrollOffset = new Vector2(
                    Mathf.Max(0, scrollStart + delta), 0);
                evt.StopPropagation(); // Stop hanya saat drag aktif
            }
        }, TrickleDown.TrickleDown);

        scrollView.RegisterCallback<PointerUpEvent>(evt =>
        {
            if (evt.pointerId != activePointerId) return;
            if (isDragging && scrollView.HasPointerCapture(evt.pointerId))
                scrollView.ReleasePointer(evt.pointerId);
            isDragging = false;
            activePointerId = -1;
        }, TrickleDown.TrickleDown);

        scrollView.RegisterCallback<PointerCancelEvent>(evt =>
        {
            if (scrollView.HasPointerCapture(evt.pointerId))
                scrollView.ReleasePointer(evt.pointerId);
            isDragging = false;
            activePointerId = -1;
        }, TrickleDown.TrickleDown);
    }
    private void PopulateKatalogPreview()
    {
        if (_previewKatalog == null) return;
        _previewKatalog.Clear();

        var items = FurnitureDatabase.GetAll();
        int count = Mathf.Min(6, items.Count);
        for (int i = 0; i < count; i++)
            _previewKatalog.Add(CreatePreviewCard(items[i]));
    }

    private VisualElement CreatePreviewCard(FurnitureItem item)
    {
        var card = new VisualElement();
        card.AddToClassList("preview-card");

        var thumb = new VisualElement();
        thumb.AddToClassList("preview-card__thumb");
        var iconBox = new VisualElement();
        iconBox.AddToClassList("preview-card__icon-box");
        var iconInner = new VisualElement();
        iconInner.AddToClassList("preview-card__icon-inner");
        iconBox.Add(iconInner);
        thumb.Add(iconBox);

        var info = new VisualElement();
        info.AddToClassList("preview-card__info");
        var nameLabel = new Label(item.name);
        nameLabel.AddToClassList("preview-card__name");
        var catLabel = new Label(item.category);
        catLabel.AddToClassList("preview-card__category");
        info.Add(nameLabel);
        info.Add(catLabel);

        card.Add(thumb);
        card.Add(info);

        card.RegisterCallback<ClickEvent>(_ =>
        {
            AppState.SelectedFurnitureId = item.id;
            ScreenManager.Instance.ShowDetailFurnitur();
        });

        return card;
    }

    private async void LoadProyekPreview()
    {
        if (_proyekList == null) return;
        _proyekList.Clear();

        // Tunggu sampai FirebaseManager.Instance terinisialisasi dan siap (IsReady)
        float timeout = 5f;
        float elapsed = 0f;
        while ((FirebaseManager.Instance == null || !FirebaseManager.Instance.IsReady) && elapsed < timeout)
        {
            await System.Threading.Tasks.Task.Delay(100);
            elapsed += 0.1f;
        }

        if (FirebaseManager.Instance == null || !FirebaseManager.Instance.IsReady)
        {
            ShowProyekEmptyState(true);
            return;
        }

        try
        {
            var proyekList = await FirebaseManager.Instance.GetAllProyekAsync();
            if (proyekList == null || proyekList.Count == 0)
            {
                ShowProyekEmptyState(true);
            }
            else
            {
                ShowProyekEmptyState(false);
                int count = Mathf.Min(2, proyekList.Count);
                for (int i = 0; i < count; i++)
                {
                    _proyekList.Add(CreateProyekCardDashboard(proyekList[i]));
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[DashboardScreen] LoadProyekPreview: {e.Message}");
            ShowProyekEmptyState(true);
        }
    }

    private void ShowProyekEmptyState(bool isEmpty)
    {
        if (isEmpty)
        {
            _proyekEmptyState?.SetDisplay(true);
            _proyekList?.SetDisplay(false);
        }
        else
        {
            _proyekEmptyState?.SetDisplay(false);
            _proyekList?.SetDisplay(true);
        }
    }

    private VisualElement CreateProyekCardDashboard(ProyekData proyek)
    {
        var card = new VisualElement();
        card.AddToClassList("proyek-card__dashboard");

        // Thumbnail
        var thumb = new VisualElement();
        thumb.AddToClassList("proyek-card__thumb-dashboard");

        if (!string.IsNullOrEmpty(proyek.thumbnailPath) &&
            System.IO.File.Exists(proyek.thumbnailPath))
        {
            var bytes = System.IO.File.ReadAllBytes(proyek.thumbnailPath);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            thumb.style.backgroundImage = new StyleBackground(tex);
            thumb.style.backgroundSize = new StyleBackgroundSize(
                new BackgroundSize(BackgroundSizeType.Cover));
        }
        else
        {
            var placeholder = new VisualElement();
            placeholder.AddToClassList("proyek-card__thumb-placeholder");
            thumb.Add(placeholder);
        }

        // Info
        var info = new VisualElement();
        info.AddToClassList("proyek-card__info");

        var nama = new Label(proyek.nama);
        nama.AddToClassList("proyek-card__name-dashboard");

        var tanggal = new Label(FormatTanggal(proyek.tanggal));
        tanggal.AddToClassList("proyek-card__date-dashboard");

        info.Add(nama);
        info.Add(tanggal);

        // Arrow
        var arrow = new Label("›");
        arrow.AddToClassList("proyek-card__arrow");

        card.Add(thumb);
        card.Add(info);
        card.Add(arrow);

        card.RegisterCallback<ClickEvent>(_ =>
        {
            AppState.ActiveProjectId = proyek.id;
            ScreenManager.Instance.ShowDetailProyek();
        });

        return card;
    }

    private string FormatTanggal(string iso)
    {
        if (DateTime.TryParse(iso, out var dt))
            return dt.ToString("dd MMM yyyy");
        return iso;
    }
}