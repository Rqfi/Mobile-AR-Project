using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using GLTFast;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using GLTFast.Materials;
using UnityEngine.XR.ARFoundation;

public class ARSessionController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button btnBack;
    [SerializeField] private Button btnScreenshot;
    [SerializeField] private GameObject flashOverlay;

    [Header("AR Object Interaction UI")]
    [SerializeField] private GameObject interactionPanel; // Kontainer semua tombol kontrol
    [SerializeField] private Button btnDelete;
    [SerializeField] private Button btnRotateLeft;
    [SerializeField] private Button btnRotateRight;
    [SerializeField] private Button btnMoveUp;    // Maju (menjauhi kamera)
    [SerializeField] private Button btnMoveDown;  // Mundur (mendekati kamera)
    [SerializeField] private Button btnMoveLeft;  // Geser Kiri
    [SerializeField] private Button btnMoveRight; // Geser Kanan

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 0.5f;   // Kecepatan geser objek (meter per detik)
    [SerializeField] private float rotateSpeed = 60f;  // Kecepatan rotasi objek (derajat per detik)

    private GameObject _currentSelectedObject;
    private Rigidbody _cachedRb;
    private GameObject _selectionIndicator;

    // Status hold tombol
    private bool _isRotatingLeft;
    private bool _isRotatingRight;
    private bool _isMovingForward;
    private bool _isMovingBackward;
    private bool _isMovingLeft;
    private bool _isMovingRight;

    private void OnBackClick()
    {
        SceneManager.LoadScene("Main");
    }

    private bool _isCapturing = false;

    private void OnScreenshotClick()
    {
        if (_isCapturing) return;
        _isCapturing = true;
        StartCoroutine(TakeScreenshot());
    }

    private IEnumerator TakeScreenshot()
    {
        Canvas_AR_SetActive(false);
        yield return new WaitForEndOfFrame();

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();

        Canvas_AR_SetActive(true);

        if (flashOverlay != null)
            StartCoroutine(FlashEffect());

        string savedPath = SaveScreenshot(screenshot);

        AppState.LastScreenshotPath = savedPath;
        AppState.LastScreenshotTexture = screenshot;

        AppState.ReturnToScreen = "TangkapanLayar";
        Debug.Log($"[ARSession] ReturnToScreen set to: {AppState.ReturnToScreen}");
        yield return new WaitForSeconds(0.4f);
        SceneManager.LoadScene("Main");
    }

    private string SaveScreenshot(Texture2D screenshot)
    {
        // Buat nama file unik berdasarkan timestamp
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"AR_{timestamp}.png";

        // Simpan ke persistent data path (internal storage app)
        string folderPath = Path.Combine(
            Application.persistentDataPath, "Screenshots");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string fullPath = Path.Combine(folderPath, filename);
        byte[] pngData = screenshot.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngData);

        // Simpan juga ke galeri Android
        SaveToGallery(pngData, filename);

        return fullPath;
    }

    private void SaveToGallery(byte[] imageData, string filename)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Simpan dulu ke temp path
            string tempPath = Path.Combine(
                Application.temporaryCachePath, filename);
            File.WriteAllBytes(tempPath, imageData);

            // Scan file agar muncul di galeri
            using var mediaScannerConnection = new AndroidJavaClass(
                "android.media.MediaScannerConnection");
            using var unityPlayer = new AndroidJavaClass(
                "com.unity3d.player.UnityPlayer");
            using var activity = unityPlayer.GetStatic<AndroidJavaObject>(
                "currentActivity");

            mediaScannerConnection.CallStatic(
                "scanFile",
                activity,
                new string[] { tempPath },
                null,
                null);

            Debug.Log($"[Gallery] Disimpan ke galeri: {tempPath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Gallery] Gagal simpan ke galeri: {e.Message}");
        }
#else
        Debug.Log("[Gallery] Simpan ke galeri hanya di Android device.");
#endif
    }

    private void Canvas_AR_SetActive(bool active)
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
            canvas.gameObject.SetActive(active);
    }

    private IEnumerator FlashEffect()
    {
        flashOverlay.SetActive(true);

        var image = flashOverlay.GetComponent<Image>();
        if (image == null) yield break;

        // Fade in
        float t = 0f;
        while (t < 0.1f)
        {
            t += Time.deltaTime;
            image.color = new Color(1, 1, 1, Mathf.Lerp(0, 1, t / 0.1f));
            yield return null;
        }

        // Fade out
        t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            image.color = new Color(1, 1, 1, Mathf.Lerp(1, 0, t / 0.2f));
            yield return null;
        }

        flashOverlay.SetActive(false);
    }

    private ObjectSpawner _objectSpawner;

    private void Start()
    {
        btnBack.onClick.AddListener(OnBackClick);
        btnScreenshot.onClick.AddListener(OnScreenshotClick);

        // Cari ObjectSpawner di scene dan daftarkan event spawn
        _objectSpawner = FindFirstObjectByType<ObjectSpawner>();
        if (_objectSpawner != null)
        {
            _objectSpawner.objectSpawned += OnObjectSpawned;
        }

        // FIX: Paksa ARPlaneManager untuk mendeteksi dinding vertikal (berjaga-jaga jika ter-reset)
        var planeManager = FindFirstObjectByType<ARPlaneManager>();
        if (planeManager != null)
        {
            planeManager.requestedDetectionMode = UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Horizontal | UnityEngine.XR.ARSubsystems.PlaneDetectionMode.Vertical;
        }

        // Hubungkan tombol Hapus
        if (btnDelete != null)
        {
            btnDelete.onClick.AddListener(DeleteSelectedObject);
        }

        // Hubungkan hold event untuk tombol navigasi & rotasi
        SetupHoldButton(btnRotateLeft, state => _isRotatingLeft = state);
        SetupHoldButton(btnRotateRight, state => _isRotatingRight = state);
        SetupHoldButton(btnMoveUp, state => _isMovingForward = state);
        SetupHoldButton(btnMoveDown, state => _isMovingBackward = state);
        SetupHoldButton(btnMoveLeft, state => _isMovingLeft = state);
        SetupHoldButton(btnMoveRight, state => _isMovingRight = state);

        // Sembunyikan panel kontrol di awal sebelum ada objek yang di-spawn
        if (interactionPanel != null)
            interactionPanel.SetActive(false);

        // Sembunyikan flash overlay
        if (flashOverlay != null)
            flashOverlay.SetActive(false);

        CreateSelectionIndicator();
    }

    private void OnDestroy()
    {
        if (_objectSpawner != null)
        {
            _objectSpawner.objectSpawned -= OnObjectSpawned;
        }
    }

    // Variabel baru untuk membedakan objek di dinding vs lantai
    private System.Collections.Generic.Dictionary<GameObject, bool> _objectOnWallMap = new System.Collections.Generic.Dictionary<GameObject, bool>();
    private System.Collections.Generic.Dictionary<GameObject, Vector3> _objectWallNormalMap = new System.Collections.Generic.Dictionary<GameObject, Vector3>();

    private void OnObjectSpawned(GameObject spawnedObject)
    {
        _currentSelectedObject = spawnedObject;

        // Tampilkan panel kontrol
        if (interactionPanel != null)
            interactionPanel.SetActive(true);

        bool isOnWall = Vector3.Angle(spawnedObject.transform.up, Vector3.up) > 45f;
        _objectOnWallMap[spawnedObject] = isOnWall;

        if (isOnWall)
        {
            Vector3 wallNormal = spawnedObject.transform.up;
            _objectWallNormalMap[spawnedObject] = wallNormal;
            spawnedObject.transform.rotation = Quaternion.LookRotation(wallNormal, Vector3.up);
        }
        else
        {
            Vector3 forward = spawnedObject.transform.forward;
            forward.y = 0;
            if (forward != Vector3.zero)
                spawnedObject.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        string selectedId = AppState.SelectedFurnitureId;
        if (string.IsNullOrEmpty(selectedId))
        {
            Debug.LogWarning("[ARSession] SelectedFurnitureId kosong di AppState.");
            return;
        }

        var item = FurnitureDatabase.GetAll().Find(i => i.id == selectedId);
        if (item == null)
        {
            Debug.LogWarning($"[ARSession] Item dengan ID {selectedId} tidak ditemukan di database.");
            return;
        }

        LoadGLBModelInAR(spawnedObject, item);
    }

    private async void LoadGLBModelInAR(GameObject spawnedObject, FurnitureItem item)
    {
        try
        {
            // Tampilkan status teks loading di layar
            ShowARLoadingStatus($"Mengunduh {item.name}...");

            // 1. Buat container baru untuk GLB
            GameObject glbContainer = new GameObject("GLB_Holder");
            glbContainer.transform.SetParent(spawnedObject.transform, false);
            glbContainer.transform.localPosition = Vector3.zero;
            glbContainer.transform.localRotation = Quaternion.identity;

            // 2. Load GLB menggunakan glTFast (dengan Cache Lokal)
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var gltfImport = new GltfImport(materialGenerator: new UniversalRPMaterialGenerator(urpAsset));

            string loadPath = await CacheManager.GetLocalGLBPath(item.modelUrl);
            if (string.IsNullOrEmpty(loadPath))
            {
                loadPath = item.modelUrl;
            }
            bool success = await gltfImport.Load(loadPath);

            // Sembunyikan status teks loading setelah selesai memuat
            HideARLoadingStatus();

            if (success)
            {
                await gltfImport.InstantiateMainSceneAsync(glbContainer.transform);

                // 3. Sembunyikan visual bawaan (MeshRenderer placeholder) dari spawnedObject setelah model siap
                var renderers = spawnedObject.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    // Pastikan kita tidak menyembunyikan renderer dari model GLB yang baru di-instantiate
                    if (r.transform != glbContainer.transform && !r.transform.IsChildOf(glbContainer.transform))
                    {
                        r.enabled = false;
                    }
                }

                // 4. Sesuaikan skala berdasarkan item.scale dari database
                await ScaleModelToRealWorldSizeAsync(glbContainer, item);

                // Buat BoxCollider dinamis berdasarkan ukuran model asli untuk interaktivitas
                AddBoxColliderToModel(spawnedObject, glbContainer);

                // Matikan animator bawaan model jika ada
                var animator = glbContainer.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }
            }
            else
            {
                Debug.LogError($"[ARSession] Gagal memuat model GLB dari URL: {item.modelUrl}");

                // Jika gagal total, sembunyikan placeholder agar tidak membingungkan pengguna
                var renderers = spawnedObject.GetComponentsInChildren<Renderer>();
                foreach (var r in renderers)
                {
                    r.enabled = false;
                }
            }
        }
        catch (Exception e)
        {
            HideARLoadingStatus();
            Debug.LogError($"[ARSession] Error loading GLB in AR: {e.Message}");
        }
    }

    private async Task ScaleModelToRealWorldSizeAsync(GameObject glbContainer, FurnitureItem item)
    {
        Bounds bounds = default;
        bool hasBounds = false;
        int rendererCount = 0;

        for (int attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(100);
            if (glbContainer == null) return;

            Transform parentTransform = glbContainer.transform.parent;
            Quaternion originalRotation = parentTransform != null ? parentTransform.rotation : Quaternion.identity;
            if (parentTransform != null)
                parentTransform.rotation = Quaternion.identity;

            var renderers = glbContainer.GetComponentsInChildren<Renderer>();
            rendererCount = renderers.Length;
            hasBounds = false;

            // FIX: Paksa kalkulasi ulang bounds mesh. 
            // Beberapa model GLB (terutama yang diekspor dari Blender tanpa setelan yang tepat) 
            // tidak memiliki metadata bounds, membuat bounds.size bernilai (0,0,0) meskipun objeknya terlihat.
            var meshFilters = glbContainer.GetComponentsInChildren<MeshFilter>();
            foreach (var mf in meshFilters)
            {
                if (mf.sharedMesh != null) mf.sharedMesh.RecalculateBounds();
            }

            var skinnedMeshes = glbContainer.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var smr in skinnedMeshes)
            {
                if (smr.sharedMesh != null)
                {
                    smr.sharedMesh.RecalculateBounds();
                    smr.localBounds = smr.sharedMesh.bounds;
                }
            }

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

            if (parentTransform != null)
                parentTransform.rotation = originalRotation;

            if (hasBounds && bounds.size != Vector3.zero)
            {
                break;
            }
        }

        if (!hasBounds || bounds.size == Vector3.zero)
        {
            Debug.LogWarning($"[ARSession] GAGAL BACA UKURAN. Renderer ditemukan: {rendererCount}. Objek mungkin rusak/kosong.");

            float fallback = item.scale > 0 ? item.scale : 1f;
            glbContainer.transform.localScale = Vector3.one * fallback;
            return;
        }

        Vector3 modelSize = bounds.size;
        float targetWidth = item.width / 100f;
        float targetHeight = item.height / 100f;
        float targetDepth = item.depth / 100f;

        // Amankan dari pembagian nol
        if (modelSize.x == 0) modelSize.x = 0.01f;
        if (modelSize.y == 0) modelSize.y = 0.01f;
        if (modelSize.z == 0) modelSize.z = 0.01f;

        float scaleX = targetWidth / modelSize.x;
        float scaleY = targetHeight / modelSize.y;
        float scaleZ = targetDepth / modelSize.z;

        // Pakai skala terkecil/proporsional jika model tidak kubus sempurna (menjaga rasio bentuk asli)
        float finalScale = Mathf.Min(scaleX, Mathf.Min(scaleY, scaleZ));

        // Atur skala
        glbContainer.transform.localScale = new Vector3(finalScale, finalScale, finalScale);

        Debug.Log($"[ARSession] Sukses set skala. Asli: {modelSize:F2}, Target: {targetWidth:F2}x{targetHeight:F2}x{targetDepth:F2}, Skala final: {finalScale:F3}");
    }

    private void DeleteSelectedObject()
    {
        if (_currentSelectedObject != null)
        {
            Destroy(_currentSelectedObject);
            _currentSelectedObject = null;

            if (interactionPanel != null)
                interactionPanel.SetActive(false);
        }
    }

    private void CreateSelectionIndicator()
    {
        if (_selectionIndicator != null) return;

        _selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        _selectionIndicator.name = "SelectionIndicator";

        // Hapus collider agar tidak mengganggu raycast
        var col = _selectionIndicator.GetComponent<Collider>();
        if (col != null) Destroy(col);

        // Buat sangat tipis (flat disc)
        _selectionIndicator.transform.localScale = new Vector3(0.1f, 0.002f, 0.1f);

        // Material semi-transparan kuning/emas
        var renderer = _selectionIndicator.GetComponent<Renderer>();
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = new Color(1f, 0.8f, 0f, 0.4f);
        renderer.material = mat;

        _selectionIndicator.SetActive(false);
    }

    private void UpdateSelectionIndicator()
    {
        if (_selectionIndicator == null) return;

        if (_currentSelectedObject == null)
        {
            _selectionIndicator.SetActive(false);
            return;
        }

        // Hitung bounds dari seluruh renderer di objek terpilih
        var allRenderers = _currentSelectedObject.GetComponentsInChildren<Renderer>();
        var renderers = System.Array.FindAll(allRenderers, r => r.enabled);
        if (renderers.Length == 0)
        {
            _selectionIndicator.SetActive(false);
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // Posisikan di bawah tengah objek
        Vector3 indicatorPos = new Vector3(bounds.center.x, bounds.min.y + 0.001f, bounds.center.z);
        _selectionIndicator.transform.position = indicatorPos;

        // Skala sesuai lebar/kedalaman objek (sedikit lebih besar)
        float diameter = Mathf.Max(bounds.size.x, bounds.size.z) * 1.2f;
        _selectionIndicator.transform.localScale = new Vector3(diameter, 0.002f, diameter);

        _selectionIndicator.SetActive(true);
    }

    private void Update()
    {
        // 1. Deteksi sentuhan menggunakan New Input System
        if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
        {
            Vector2 screenPos = Pointer.current.position.ReadValue();

            bool isOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            Debug.Log($"[ARSession] Touch detected! pos={screenPos}, OverUI={isOverUI}");

            if (!isOverUI)
            {
                Ray ray = Camera.main.ScreenPointToRay(screenPos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Debug.Log($"[ARSession] Raycast HIT: {hit.collider.gameObject.name}, parent: {hit.collider.transform.parent?.name}");

                    // Cari objek parent root di bawah Spawner
                    Transform target = hit.collider.transform;
                    bool isFurniture = false;

                    while (target.parent != null)
                    {
                        if (target.parent.name == "Object Spawner" || target.parent.GetComponent<ObjectSpawner>() != null)
                        {
                            isFurniture = true;
                            break;
                        }
                        target = target.parent;
                    }

                    if (isFurniture)
                    {
                        _currentSelectedObject = target.gameObject;
                        if (interactionPanel != null)
                            interactionPanel.SetActive(true);

                        Debug.Log($"[ARSession] Objek terpilih: {_currentSelectedObject.name}");
                    }
                    else
                    {
                        // Jika klik di luar furnitur (misal: di lantai AR Plane), hilangkan seleksi
                        _currentSelectedObject = null;
                        if (interactionPanel != null)
                            interactionPanel.SetActive(false);

                        Debug.Log("[ARSession] Raycast MISS - Klik pada lantai/AR Plane");
                    }
                }
                else
                {
                    // Tidak kena apa-apa
                    _currentSelectedObject = null;
                    if (interactionPanel != null)
                        interactionPanel.SetActive(false);

                    Debug.Log("[ARSession] Raycast MISS - tidak mengenai collider apapun");
                }
            }
        }

        UpdateSelectionIndicator();

        // Jika tidak ada objek terpilih, sembunyikan panel kontrol dan lewati sisa logika gerakan
        if (_currentSelectedObject == null)
        {
            if (interactionPanel != null && interactionPanel.activeSelf)
                interactionPanel.SetActive(false);
            return;
        }

        // 2. Tangani Geser Objek (Move) Berdasarkan Permukaan
        Vector3 moveDirection = Vector3.zero;
        bool isOnWall = _objectOnWallMap.ContainsKey(_currentSelectedObject) && _objectOnWallMap[_currentSelectedObject];

        if (isOnWall)
        {
            Vector3 wallUp = Vector3.up;
            // Ambil arah asli dinding (jika tidak ada, gunakan arah depan objek sebagai cadangan)
            Vector3 wallNormal = _objectWallNormalMap.ContainsKey(_currentSelectedObject) ? _objectWallNormalMap[_currentSelectedObject] : _currentSelectedObject.transform.forward;

            // Rumus Cross Product: menghasilkan arah Kanan (Right) yang 100% sejajar dengan dinding
            Vector3 wallRight = Vector3.Cross(wallNormal, wallUp).normalized;

            if (_isMovingForward) moveDirection += wallUp;
            if (_isMovingBackward) moveDirection -= wallUp;
            if (_isMovingRight) moveDirection += wallRight;
            if (_isMovingLeft) moveDirection -= wallRight;
        }
        else
        {
            // Jika di lantai: pergerakan mendatar (X, Z) mengikuti arah kamera
            Vector3 cameraForward = Camera.main.transform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            Vector3 cameraRight = Camera.main.transform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            if (_isMovingForward) moveDirection += cameraForward;
            if (_isMovingBackward) moveDirection -= cameraForward;
            if (_isMovingRight) moveDirection += cameraRight;
            if (_isMovingLeft) moveDirection -= cameraRight;
        }

        if (_cachedRb == null || _cachedRb.gameObject != _currentSelectedObject)
        {
            _cachedRb = _currentSelectedObject.GetComponent<Rigidbody>();
        }
        if (moveDirection != Vector3.zero)
        {
            if (_cachedRb != null)
            {
                _cachedRb.MovePosition(_currentSelectedObject.transform.position + moveDirection.normalized * (moveSpeed * Time.deltaTime));
            }
            else
            {
                _currentSelectedObject.transform.position += moveDirection.normalized * (moveSpeed * Time.deltaTime);
            }
        }

        // 3. Tangani Rotasi Objek (Rotate)
        if (_isRotatingRight)
        {
            if (_cachedRb != null) _cachedRb.MoveRotation(_cachedRb.rotation * Quaternion.Euler(0, rotateSpeed * Time.deltaTime, 0));
            else _currentSelectedObject.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }
        if (_isRotatingLeft)
        {
            if (_cachedRb != null) _cachedRb.MoveRotation(_cachedRb.rotation * Quaternion.Euler(0, -rotateSpeed * Time.deltaTime, 0));
            else _currentSelectedObject.transform.Rotate(Vector3.up, -rotateSpeed * Time.deltaTime, Space.World);
        }
    }

    private GameObject _loadingTextGo;

    private void ShowARLoadingStatus(string message)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        if (_loadingTextGo == null)
        {
            _loadingTextGo = new GameObject("AR_LoadingStatus");
            _loadingTextGo.transform.SetParent(canvas.transform, false);

            var rect = _loadingTextGo.AddComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(0f, 150f); // Diposisikan di atas Bottom Bar
            rect.sizeDelta = new Vector2(500f, 60f);

            var text = _loadingTextGo.AddComponent<UnityEngine.UI.Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            var outline = _loadingTextGo.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, 1.5f);
        }

        var uiText = _loadingTextGo.GetComponent<UnityEngine.UI.Text>();
        if (uiText != null)
        {
            uiText.text = message;
            _loadingTextGo.SetActive(true);
        }
    }

    private void HideARLoadingStatus()
    {
        if (_loadingTextGo != null)
        {
            _loadingTextGo.SetActive(false);
        }
    }

    private void AddBoxColliderToModel(GameObject spawnedObject, GameObject glbContainer)
    {
        // Hapus BoxCollider lama di parent saja (jika ada), jangan sentuh collider child
        var existingBox = spawnedObject.GetComponent<BoxCollider>();
        if (existingBox != null)
            Destroy(existingBox);

        // FIX: Simpan rotasi asli dan reset sementara agar kalkulasi bounds (hitbox) tidak membesar
        Quaternion originalRot = spawnedObject.transform.rotation;
        spawnedObject.transform.rotation = Quaternion.identity;

        Bounds bounds = new Bounds(glbContainer.transform.position, Vector3.zero);
        var glbRenderers = glbContainer.GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        foreach (var r in glbRenderers)
        {
            if (r.bounds.size == Vector3.zero) continue;

            if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
            else bounds.Encapsulate(r.bounds);
        }

        if (hasBounds)
        {
            var box = spawnedObject.AddComponent<BoxCollider>();
            Vector3 localCenter = spawnedObject.transform.InverseTransformPoint(bounds.center);
            Vector3 localSize = spawnedObject.transform.InverseTransformVector(bounds.size);
            box.center = localCenter;
            box.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

            // Tambahkan Fisika (Rigidbody)
            var rb = spawnedObject.GetComponent<Rigidbody>();
            if (rb == null) rb = spawnedObject.AddComponent<Rigidbody>();
            rb.useGravity = false;      // Matikan gravitasi agar tidak jatuh ke bawah
            rb.isKinematic = false;     // Harus false agar bisa bertabrakan
            rb.linearDamping = 10f;              // Rem/gesekan agar objek tidak meluncur terus saat ditabrak
            rb.angularDamping = 10f;

            // Kunci putaran agar objek tidak terguling saat saling tabrak
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;
        }

        // Kembalikan rotasi ke semula
        spawnedObject.transform.rotation = originalRot;
    }

    private void SetupHoldButton(Button button, Action<bool> onStateChanged)
    {
        if (button == null) return;

        var trigger = button.gameObject.GetComponent<UnityEngine.EventSystems.EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
        }

        // Ketika tombol ditekan
        var pointerDown = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerDown.eventID = UnityEngine.EventSystems.EventTriggerType.PointerDown;
        pointerDown.callback.AddListener((data) => { onStateChanged(true); });
        trigger.triggers.Add(pointerDown);

        // Ketika tombol dilepas
        var pointerUp = new UnityEngine.EventSystems.EventTrigger.Entry();
        pointerUp.eventID = UnityEngine.EventSystems.EventTriggerType.PointerUp;
        pointerUp.callback.AddListener((data) => { onStateChanged(false); });
        trigger.triggers.Add(pointerUp);
    }

}