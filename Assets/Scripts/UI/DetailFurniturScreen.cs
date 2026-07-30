using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using GLTFast;

public class DetailFurniturScreen : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _heroContainer;

    // Aset Viewer 3D Dinamis
    private GameObject _viewerContainer;
    private GameObject _spawnedModel;
    private Camera _viewerCamera;
    private Light _viewerLight;
    private RenderTexture _renderTexture;

    // Status Rotasi
    private bool _isDragging = false;
    private Vector2 _lastPointerPos;

    private void OnEnable()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null) return;
        _root = uiDoc.rootVisualElement;

        _heroContainer = _root.Q<VisualElement>("detail-hero");

        _root.Q<Label>("btn-back")
             ?.RegisterCallback<ClickEvent>(_ => OnBackClick());

        _root.Q<VisualElement>("btn-ar")
             ?.RegisterCallback<ClickEvent>(_ => OnBtnARClick());

        PopulateData();
    }

    private void OnDisable()
    {
        Cleanup3DViewer();
    }

    private void OnBackClick()
    {
        ScreenManager.Instance.ShowKatalog();
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
        _root.Q<Label>("spec-dimensi").text = $"{item.width} × {item.depth} × {item.height} cm";

        // Setup 3D Viewer jika URL model tersedia
        if (!string.IsNullOrEmpty(item.modelUrl))
        {
            Setup3DViewer(item);
        }
        else
        {
            Debug.LogWarning("[DetailFurnitur] URL model kosong/tidak tersedia.");
        }
    }

    private async void Setup3DViewer(FurnitureItem item)
    {
        Cleanup3DViewer();

        try
        {
            // 1. Buat kontainer di posisi terisolasi
            _viewerContainer = new GameObject("3DViewer_Container");
            _viewerContainer.transform.position = new Vector3(1000f, 1000f, 1000f);

            // 2. Buat Kamera Khusus
            var camGo = new GameObject("3DViewer_Camera");
            camGo.transform.SetParent(_viewerContainer.transform);
            camGo.transform.localPosition = new Vector3(0f, 0.8f, -2.5f);
            camGo.transform.LookAt(_viewerContainer.transform.position + Vector3.up * 0.3f);
            _viewerCamera = camGo.AddComponent<Camera>();
            _viewerCamera.clearFlags = CameraClearFlags.SolidColor;
            // Menyesuaikan warna latar belakang dengan tema UXML (#FEF7E5)
            _viewerCamera.backgroundColor = new Color(0.996f, 0.969f, 0.898f);
            _viewerCamera.fieldOfView = 45f;
            _viewerCamera.nearClipPlane = 0.1f;
            _viewerCamera.farClipPlane = 10f;

            _viewerCamera.useOcclusionCulling = false;

            // 3. Buat Light
            var lightGo = new GameObject("3DViewer_Light");
            lightGo.transform.SetParent(_viewerContainer.transform);
            lightGo.transform.localPosition = new Vector3(2f, 4f, -2f);
            _viewerLight = lightGo.AddComponent<Light>();
            _viewerLight.type = LightType.Directional;
            _viewerLight.intensity = 1.2f;
            _viewerLight.transform.LookAt(_viewerContainer.transform.position);

            // 4. Buat RenderTexture dan sambungkan ke UI
            _renderTexture = new RenderTexture(512, 512, 24);
            _viewerCamera.targetTexture = _renderTexture;

            if (_heroContainer != null)
            {
                _heroContainer.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_renderTexture));
                RegisterDragEvents();
            }

            // 5. Unduh & Instansiasi Model GLB menggunakan glTFast
            Label loadingLabel = new Label("Memuat Model 3D...");
            loadingLabel.name = "detail-loading";
            loadingLabel.style.color = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            loadingLabel.style.fontSize = 24f;
            loadingLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            loadingLabel.style.alignSelf = Align.Center;
            loadingLabel.style.flexGrow = 1;
            if (_heroContainer != null)
            {
                _heroContainer.Add(loadingLabel);
            }
            var gltfImport = new GltfImport();
            string loadPath = await CacheManager.GetLocalGLBPath(item.modelUrl);
            if (string.IsNullOrEmpty(loadPath))
            {
                loadPath = item.modelUrl;
            }
            bool success = await gltfImport.Load(loadPath);
            if (_heroContainer != null && loadingLabel != null)
            {
                _heroContainer.Remove(loadingLabel);
            }
            if (success && _viewerContainer != null)
            {
                _spawnedModel = new GameObject("GLB_Model");

                await gltfImport.InstantiateMainSceneAsync(_spawnedModel.transform);

                _spawnedModel.transform.SetParent(_viewerContainer.transform, false);
                _spawnedModel.transform.localPosition = Vector3.zero;

                _spawnedModel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

                float finalScale = item.scale > 0 ? item.scale : 1f;
                _spawnedModel.transform.localScale = Vector3.one * finalScale;

                FitModelToViewer(_spawnedModel, 1.2f);

                var animator = _spawnedModel.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                    Debug.LogWarning("[3DViewer Debug] Animator ditemukan dan telah dinonaktifkan.");
                }
            }
            else
            {
                Debug.LogWarning($"[3DViewer] Gagal memuat file GLB dari URL: {item.modelUrl}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[3DViewer] Error in Setup3DViewer: {e.Message}");
        }
    }

    private void FitModelToViewer(GameObject model, float targetSize = 1.2f)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // 1. Hitung total bounds awal dalam world space
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // 2. Hitung dimensi terbesar
        float maxDim = Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z));
        if (maxDim <= 0) return;

        // 3. Terapkan skala agar muat di ukuran target
        float scaleFactor = targetSize / maxDim;
        model.transform.localScale = model.transform.localScale * scaleFactor;

        // 4. Hitung ulang bounds setelah scaling untuk penyesuaian posisi
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // 5. Geser model agar pusat geometrinya berada di (1000, 1000.3, 1000)
        Vector3 targetCenter = _viewerContainer.transform.position + Vector3.up * 0.3f;
        Vector3 offset = targetCenter - bounds.center;
        model.transform.position += offset;
    }


    private void RegisterDragEvents()
    {
        if (_heroContainer == null) return;

        _heroContainer.pickingMode = PickingMode.Position;

        _heroContainer.RegisterCallback<PointerDownEvent>(evt =>
        {
            _isDragging = true;
            _lastPointerPos = (Vector2)evt.position;
            _heroContainer.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        });

        _heroContainer.RegisterCallback<PointerMoveEvent>(evt =>
        {
            if (!_isDragging || _spawnedModel == null) return;

            Vector2 currentPos = (Vector2)evt.position;
            Vector2 delta = currentPos - _lastPointerPos;
            _lastPointerPos = currentPos;

            _spawnedModel.transform.Rotate(Vector3.up, -delta.x * 0.5f, Space.World);
            _spawnedModel.transform.Rotate(Vector3.right, delta.y * 0.5f, Space.Self);
            evt.StopPropagation();
        });

        _heroContainer.RegisterCallback<PointerUpEvent>(evt =>
        {
            _isDragging = false;
            if (_heroContainer.HasPointerCapture(evt.pointerId))
                _heroContainer.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        });
    }

    private void Cleanup3DViewer()
    {
        if (_viewerContainer != null)
        {
            Destroy(_viewerContainer);
            _viewerContainer = null;
        }

        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }

        if (_heroContainer != null)
        {
            _heroContainer.style.backgroundImage = null;
        }

        _spawnedModel = null;
        _viewerCamera = null;
        _viewerLight = null;
        _isDragging = false;
    }

    private void OnBtnARClick()
    {
        SceneManager.LoadScene("ARSession");
    }
}
