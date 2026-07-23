using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;

public class DetailProyekScreen : MonoBehaviour
{
    private VisualElement _root;

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;
        _root = uiDoc.rootVisualElement;

        _root.Q<Label>("btn-back")
             ?.RegisterCallback<ClickEvent>(_ =>
                 ScreenManager.Instance.ShowProyek());

        if (!string.IsNullOrEmpty(AppState.ActiveProjectId))
            LoadScreenshots(AppState.ActiveProjectId);
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
            Debug.LogWarning($"[DetailProyek] {e.Message}");
        }
    }

    private VisualElement CreatePhotoItem(ScreenshotData ss)
    {
        var item = new VisualElement();
        item.AddToClassList("photo-item");

        // Load gambar dari local path jika ada
        if (!string.IsNullOrEmpty(ss.path) && File.Exists(ss.path))
        {
            var bytes = File.ReadAllBytes(ss.path);
            var tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            item.style.backgroundImage = new StyleBackground(tex);
            item.style.unityBackgroundScaleMode =
                new StyleEnum<ScaleMode>(ScaleMode.ScaleAndCrop);
        }
        else
        {
            var icon = new Label("📷");
            icon.AddToClassList("photo-item__icon");
            item.Add(icon);
        }

        // Label nama di bawah
        var nama = new Label(ss.nama);
        nama.style.fontSize = 18;
        nama.style.color = new UnityEngine.Color(1, 1, 1, 0.8f);
        nama.style.position = Position.Absolute;
        nama.style.bottom = 8;
        nama.style.left = 8;
        item.Add(nama);

        return item;
    }
}