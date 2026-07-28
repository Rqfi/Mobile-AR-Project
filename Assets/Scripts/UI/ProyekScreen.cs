using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;

public class ProyekScreen : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _emptyState;
    private VisualElement _proyekList;

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;
        _root = uiDoc.rootVisualElement;

        _emptyState = _root.Q<VisualElement>("empty-state");
        _proyekList = _root.Q<VisualElement>("proyek-list");

        _root.Q<Label>("btn-back")
             ?.RegisterCallback<ClickEvent>(_ =>
                 ScreenManager.Instance.ShowDashboard());

        LoadProyek();
    }

    private async void LoadProyek()
    {
        ShowLoading(true);

        if (!FirebaseManager.Instance.IsReady)
        {
            ShowEmptyState("Koneksi belum siap...");
            ShowLoading(false);
            return;
        }

        try
        {
            var proyekList = await FirebaseManager.Instance.GetAllProyekAsync();
            RenderProyek(proyekList);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ProyekScreen] {e.Message}");
            ShowEmptyState("Gagal memuat proyek");
        }
        finally
        {
            ShowLoading(false);
        }
    }

    private void RenderProyek(List<ProyekData> list)
    {
        _proyekList?.Clear();

        if (list == null || list.Count == 0)
        {
            ShowEmptyState("Belum ada proyek");
            return;
        }

        _emptyState?.SetDisplay(false);
        _proyekList?.SetDisplay(true);

        foreach (var proyek in list)
            _proyekList?.Add(CreateProyekCard(proyek));
    }

    private VisualElement CreateProyekCard(ProyekData proyek)
    {
        var card = new VisualElement();
        card.AddToClassList("proyek-card");

        // Thumbnail
        var thumb = new VisualElement();
        thumb.AddToClassList("proyek-card__thumb");

        if (!string.IsNullOrEmpty(proyek.thumbnailPath) &&
            System.IO.File.Exists(proyek.thumbnailPath))
        {
            // Load gambar dari local path
            var bytes = System.IO.File.ReadAllBytes(proyek.thumbnailPath);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            thumb.style.backgroundImage = new StyleBackground(tex);
            thumb.style.backgroundSize = new StyleBackgroundSize(
                new BackgroundSize(BackgroundSizeType.Cover));
        }
        else
        {
            // Placeholder jika tidak ada gambar
            var placeholder = new VisualElement();
            placeholder.AddToClassList("proyek-card__thumb-placeholder");
            thumb.Add(placeholder);
        }

        // Info
        var info = new VisualElement();
        info.AddToClassList("proyek-card__info");

        var nama = new Label(proyek.nama);
        nama.AddToClassList("proyek-card__name");

        var tanggal = new Label(FormatTanggal(proyek.tanggal));
        tanggal.AddToClassList("proyek-card__date");

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

    private void ShowEmptyState(string pesan)
    {
        _emptyState?.Q<Label>()?.SetText(pesan);
        _emptyState?.SetDisplay(true);
        _proyekList?.SetDisplay(false);
    }

    private void ShowLoading(bool show)
    {
        // Opsional: tambah loading indicator di UXML
    }

    private string FormatTanggal(string iso)
    {
        if (DateTime.TryParse(iso, out var dt))
            return dt.ToString("dd MMM yyyy");
        return iso;
    }
}

// Extension helper
public static class VisualElementExtensions
{
    public static void SetDisplay(this VisualElement el, bool visible)
    {
        if (el != null)
            el.style.display = visible
                ? DisplayStyle.Flex
                : DisplayStyle.None;
    }

    public static void SetText(this Label label, string text)
    {
        if (label != null) label.text = text;
    }
}