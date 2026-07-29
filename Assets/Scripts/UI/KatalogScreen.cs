using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.Networking;

public class KatalogScreen : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _grid;
    private TextField _searchInput;
    private string _activeCategory = "Semua";

    private readonly string[] _categories = { "Semua", "Meja", "Kursi", "Sofa", "Lemari", "Kasur" };

    private bool _scrollSetupDone = false;

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        _root = uiDoc.rootVisualElement;
        _grid = _root.Q<VisualElement>("furniture-grid");
        _searchInput = _root.Q<TextField>("search-input");

        _root.Q<Label>("btn-back")
             ?.RegisterCallback<ClickEvent>(OnBackClick);

        if (!_scrollSetupDone)
        {
            var filterScroll = _root.Q<ScrollView>("filter-scroll");
            if (filterScroll != null)
            {
                SetupManualHorizontalScroll(filterScroll);
                _scrollSetupDone = true;
            }
        }

        foreach (var cat in _categories)
        {
            var chip = _root.Q<VisualElement>("chip-" + cat.ToLower());
            if (chip == null) continue;
            var catCopy = cat;
            chip.RegisterCallback<ClickEvent>(_ => OnCategorySelected(catCopy));
        }

        _searchInput?.RegisterValueChangedCallback(
            evt => RefreshGrid(evt.newValue));

        LoadCatalogFromFirebase();
    }

    private async void LoadCatalogFromFirebase()
    {
        _grid?.Clear();
        var loadingLabel = new Label("Memuat katalog...");
        loadingLabel.style.alignSelf = Align.Center;
        loadingLabel.style.marginTop = 40;
        loadingLabel.style.color = new Color(0.5f, 0.5f, 0.5f);
        _grid?.Add(loadingLabel);

        // Tunggu Firebase Manager siap jika masih inisialisasi
        int retries = 0;
        while ((FirebaseManager.Instance == null || !FirebaseManager.Instance.IsReady) && retries < 100)
        {
            await System.Threading.Tasks.Task.Delay(100);
            retries++;
        }

        if (FirebaseManager.Instance != null && FirebaseManager.Instance.IsReady)
        {
            var items = await FirebaseManager.Instance.GetKatalogAsync();
            FurnitureDatabase.SetItems(items);
            RefreshGrid(_searchInput?.value ?? "");
        }
        else
        {
            if (_grid != null)
            {
                _grid.Clear();
                var errorLabel = new Label("Gagal terhubung ke Firebase.");
                errorLabel.style.alignSelf = Align.Center;
                errorLabel.style.marginTop = 40;
                errorLabel.style.color = Color.red;
                _grid.Add(errorLabel);
            }
        }
    }

    private async void LoadThumbnailAsync(VisualElement element, string url)
    {
        try
        {
            using (var webRequest = UnityWebRequestTexture.GetTexture(url))
            {
                var operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                    await System.Threading.Tasks.Task.Delay(50);

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var tex = DownloadHandlerTexture.GetContent(webRequest);
                    element.style.backgroundImage = new StyleBackground(tex);
                    element.style.backgroundSize = new StyleBackgroundSize(new BackgroundSize(BackgroundSizeType.Cover));
                }
                else
                {
                    Debug.LogWarning($"[Katalog] Gagal load thumbnail dari URL: {url}. Error: {webRequest.error}, Code: {webRequest.responseCode}");
                    var thumbIcon = new Label("⚠");
                    thumbIcon.AddToClassList("furniture-card__thumb-icon");
                    element.Add(thumbIcon);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Katalog] Gagal load thumbnail: {e.Message}");
        }
    }

    private void OnSearchChanged(ChangeEvent<string> evt) => RefreshGrid(evt.newValue);

    private void OnDisable()
    {
        _root?.Q<Label>("btn-back")
              ?.UnregisterCallback<ClickEvent>(OnBackClick);
        _searchInput?.UnregisterValueChangedCallback(OnSearchChanged);
        _scrollSetupDone = false;
    }

    private void OnBackClick(ClickEvent evt) =>
        ScreenManager.Instance.ShowDashboard();

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

    private void OnCategorySelected(string category)
    {
        _activeCategory = category;

        foreach (var cat in _categories)
        {
            var chip = _root.Q<VisualElement>("chip-" + cat.ToLower());
            if (chip == null) continue;
            chip.EnableInClassList("chip--active", cat == category);
        }

        RefreshGrid(_searchInput?.value ?? "");
    }

    private void RefreshGrid(string searchQuery)
    {
        _grid.Clear();

        var items = FurnitureDatabase.GetByCategory(_activeCategory);

        if (!string.IsNullOrEmpty(searchQuery))
            items = items.FindAll(i =>
                i.name.ToLower().Contains(searchQuery.ToLower()));

        foreach (var item in items)
            _grid.Add(CreateFurnitureCard(item));
    }

    private VisualElement CreateFurnitureCard(FurnitureItem item)
    {
        var card = new VisualElement();
        card.AddToClassList("furniture-card");

        var thumb = new VisualElement();
        thumb.AddToClassList("furniture-card__thumb");

        // Memuat thumbnail dinamis jika ada URL-nya
        if (!string.IsNullOrEmpty(item.thumbnailUrl))
        {
            LoadThumbnailAsync(thumb, item.thumbnailUrl);
        }
        else
        {
            var thumbIcon = new Label("◈");
            thumbIcon.AddToClassList("furniture-card__thumb-icon");
            thumb.Add(thumbIcon);
        }

        var info = new VisualElement();
        info.AddToClassList("furniture-card__info");
        var nameLabel = new Label(item.name);
        nameLabel.AddToClassList("furniture-card__name");
        var catLabel = new Label(item.category);
        catLabel.AddToClassList("furniture-card__category");
        info.Add(nameLabel);
        info.Add(catLabel);

        card.Add(thumb);
        card.Add(info);

        card.RegisterCallback<ClickEvent>(_ => OnFurnitureCardClick(item));

        return card;
    }

    private void OnFurnitureCardClick(FurnitureItem item)
    {
        AppState.SelectedFurnitureId = item.id;
        ScreenManager.Instance.ShowDetailFurnitur();
    }
}