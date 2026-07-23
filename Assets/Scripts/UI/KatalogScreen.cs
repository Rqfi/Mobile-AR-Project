using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

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

        RefreshGrid("");
    }

    private void OnDisable()
    {
        _root?.Q<Label>("btn-back")
              ?.UnregisterCallback<ClickEvent>(OnBackClick);
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
        var thumbIcon = new Label("◈");
        thumbIcon.AddToClassList("furniture-card__thumb-icon");
        thumb.Add(thumbIcon);

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