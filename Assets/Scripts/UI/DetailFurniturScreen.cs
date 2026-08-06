using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Materials;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
            _viewerContainer = new GameObject("3DViewer_Container");
            _viewerContainer.transform.position = new Vector3(1000f, 1000f, 1000f);

            var camGo = new GameObject("3DViewer_Camera");
            camGo.transform.SetParent(_viewerContainer.transform);
            camGo.transform.localPosition = new Vector3(0f, 0.8f, -3.5f);
            camGo.transform.LookAt(_viewerContainer.transform.position + Vector3.up * 0.3f);
            _viewerCamera = camGo.AddComponent<Camera>();
            _viewerCamera.clearFlags = CameraClearFlags.SolidColor;
            _viewerCamera.backgroundColor = new Color(0.996f, 0.969f, 0.898f);
            _viewerCamera.fieldOfView = 45f;
            _viewerCamera.nearClipPlane = 0.1f;
            _viewerCamera.farClipPlane = 10f;
            _viewerCamera.useOcclusionCulling = false;

            var urpCameraData = camGo.AddComponent<UniversalAdditionalCameraData>();
            urpCameraData.renderType = CameraRenderType.Base;
            urpCameraData.renderPostProcessing = false;
            urpCameraData.antialiasing = AntialiasingMode.None;

            var lightGo = new GameObject("3DViewer_Light");
            lightGo.transform.SetParent(_viewerContainer.transform);
            lightGo.transform.localPosition = new Vector3(2f, 4f, -2f);
            _viewerLight = lightGo.AddComponent<Light>();
            _viewerLight.type = LightType.Directional;
            _viewerLight.intensity = 1.5f;
            _viewerLight.color = Color.white;
            _viewerLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.4f, 0.4f, 0.4f);

            _renderTexture = new RenderTexture(512, 512, 24);
            _viewerCamera.targetTexture = _renderTexture;

            if (_heroContainer != null)
            {
                _heroContainer.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_renderTexture));
                _heroContainer.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
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
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var gltfImport = new GltfImport(materialGenerator: new UniversalRPMaterialGenerator(urpAsset));
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

                await Task.Delay(50);
                await FitModelToViewerAsync(_spawnedModel);

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

    private async Task FitModelToViewerAsync(GameObject model)
    {
        if (model == null || _viewerCamera == null || _heroContainer == null) return;

        Bounds bounds = new Bounds(model.transform.position, Vector3.zero);
        bool hasBounds = false;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(100);
            if (model == null) return;

            // FIX: Paksa kalkulasi ulang bounds mesh (penting untuk GLB dari Blender)
            var meshFilters = model.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in meshFilters)
                if (mf.sharedMesh != null) mf.sharedMesh.RecalculateBounds();

            var skinnedMeshes = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var smr in skinnedMeshes)
                if (smr.sharedMesh != null) { smr.sharedMesh.RecalculateBounds(); smr.localBounds = smr.sharedMesh.bounds; }

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            hasBounds = false;
            foreach (var r in renderers)
            {
                if (r.bounds.size == Vector3.zero) continue;

                if (!hasBounds)
                {
                    bounds = r.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            if (hasBounds && bounds.size != Vector3.zero)
            {
                break;
            }
        }

        if (!hasBounds || bounds.size == Vector3.zero)
        {
            Debug.LogWarning("[3DViewer] Gagal mendapatkan bounds model untuk framing kamera.");
            return;
        }

        float containerWidth = _heroContainer.resolvedStyle.width > 0 ? _heroContainer.resolvedStyle.width : 512f;
        float containerHeight = _heroContainer.resolvedStyle.height > 0 ? _heroContainer.resolvedStyle.height : 512f;
        float aspect = containerWidth / containerHeight;
        _viewerCamera.aspect = aspect;

        float vertFovRad = _viewerCamera.fieldOfView * Mathf.Deg2Rad;
        float horizFovRad = 2f * Mathf.Atan(Mathf.Tan(vertFovRad * 0.5f) * aspect);

        float distanceVert = bounds.extents.y / Mathf.Tan(vertFovRad * 0.5f);
        float distanceHoriz = bounds.extents.x / Mathf.Tan(horizFovRad * 0.5f);
        float requiredDistance = Mathf.Max(distanceVert, distanceHoriz);

        float finalDistance = Mathf.Max(requiredDistance * 1.4f, 2f);

        _viewerCamera.farClipPlane = 1000f;
        Vector3 camPos = bounds.center - (Vector3.forward * finalDistance);
        _viewerCamera.transform.position = camPos;
        _viewerCamera.transform.LookAt(bounds.center);
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
