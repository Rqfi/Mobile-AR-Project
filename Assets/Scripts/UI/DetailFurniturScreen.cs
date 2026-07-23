using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class DetailFurniturScreen : MonoBehaviour
{
    private VisualElement _root;

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        _root = uiDoc.rootVisualElement;

        _root.Q<Label>("btn-back")
             .RegisterCallback<ClickEvent>(_ => ScreenManager.Instance.ShowKatalog());

        _root.Q<VisualElement>("btn-ar")
             .RegisterCallback<ClickEvent>(_ => OnBtnARClick());

        PopulateData();
    }

    private void PopulateData()
    {
        if (string.IsNullOrEmpty(AppState.SelectedFurnitureId)) return;

        var items = FurnitureDatabase.GetAll();
        var item = items.Find(i => i.id == AppState.SelectedFurnitureId);
        if (item == null) return;

        _root.Q<Label>("label-name").text = item.name;
        _root.Q<Label>("label-description").text = item.description;
        _root.Q<Label>("spec-kategori").text = item.category;
        _root.Q<Label>("spec-dimensi").text =
            $"{item.width} × {item.depth} × {item.height}";
    }

    private void OnBtnARClick()
    {
        SceneManager.LoadScene("ARSession");
    }
}