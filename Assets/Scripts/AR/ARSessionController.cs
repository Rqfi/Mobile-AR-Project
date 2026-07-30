using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using GLTFast;

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
    }

    private void OnDestroy()
    {
        if (_objectSpawner != null)
        {
            _objectSpawner.objectSpawned -= OnObjectSpawned;
        }
    }

    private void OnObjectSpawned(GameObject spawnedObject)
    {
        _currentSelectedObject = spawnedObject;

        // Tampilkan panel kontrol
        if (interactionPanel != null)
            interactionPanel.SetActive(true);

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
            // 1. Sembunyikan visual bawaan (MeshRenderer) dari spawnedObject agar tidak tumpang tindih
            var renderers = spawnedObject.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = false;
            }

            // 2. Buat container baru untuk GLB
            GameObject glbContainer = new GameObject("GLB_Holder");
            glbContainer.transform.SetParent(spawnedObject.transform, false);
            glbContainer.transform.localPosition = Vector3.zero;
            glbContainer.transform.localRotation = Quaternion.identity;

            // 3. Load GLB menggunakan glTFast
            var gltfImport = new GltfImport();
            bool success = await gltfImport.Load(item.modelUrl);
            if (success)
            {
                await gltfImport.InstantiateMainSceneAsync(glbContainer.transform);

                // 4. Sesuaikan skala berdasarkan item.scale dari database
                float finalScale = item.scale > 0 ? item.scale : 1f;
                glbContainer.transform.localScale = Vector3.one * finalScale;

                // 5. Matikan animator bawaan agar tidak mengunci transform
                var animator = glbContainer.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    animator.enabled = false;
                }

                // 6. Buat BoxCollider baru jika parent tidak memiliki collider agar bisa di-interact
                var parentCollider = spawnedObject.GetComponentInChildren<Collider>();
                if (parentCollider == null)
                {
                    Bounds bounds = new Bounds(glbContainer.transform.position, Vector3.zero);
                    var glbRenderers = glbContainer.GetComponentsInChildren<Renderer>();
                    bool hasBounds = false;
                    foreach (var r in glbRenderers)
                    {
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
                    }
                }

                Debug.Log($"[ARSession] Berhasil memuat model AR untuk {item.name}");
            }
            else
            {
                Debug.LogError($"[ARSession] Gagal memuat model GLB dari URL: {item.modelUrl}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ARSession] Error loading GLB in AR: {e.Message}");
        }
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

    private void Update()
    {
        // 1. Deteksi sentuhan di layar untuk memilih objek lain yang sudah ditempatkan
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                // Jangan pilih objek jika sentuhan berada di atas tombol UI
                if (UnityEngine.EventSystems.EventSystem.current == null ||
                    !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId))
                {
                    Ray ray = Camera.main.ScreenPointToRay(touch.position);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        // Cari objek parent root di bawah Spawner
                        Transform target = hit.collider.transform;
                        while (target.parent != null &&
                               target.parent.name != "Object Spawner" &&
                               target.parent.GetComponent<ObjectSpawner>() == null)
                        {
                            target = target.parent;
                        }

                        _currentSelectedObject = target.gameObject;
                        if (interactionPanel != null)
                            interactionPanel.SetActive(true);

                        Debug.Log($"[ARSession] Objek terpilih: {_currentSelectedObject.name}");
                    }
                }
            }
        }

        // Jika tidak ada objek terpilih, sembunyikan panel kontrol dan lewati sisa logika gerakan
        if (_currentSelectedObject == null)
        {
            if (interactionPanel != null && interactionPanel.activeSelf)
                interactionPanel.SetActive(false);
            return;
        }

        // 2. Tangani Geser Objek (Move) Relatif Terhadap Arah Kamera HP
        Vector3 cameraForward = Camera.main.transform.forward;
        cameraForward.y = 0f; // Kunci sumbu Y agar gerakan tetap mendatar di lantai
        cameraForward.Normalize();

        Vector3 cameraRight = Camera.main.transform.right;
        cameraRight.y = 0f;
        cameraRight.Normalize();

        Vector3 moveDirection = Vector3.zero;
        if (_isMovingForward) moveDirection += cameraForward;
        if (_isMovingBackward) moveDirection -= cameraForward;
        if (_isMovingLeft) moveDirection -= cameraRight;
        if (_isMovingRight) moveDirection += cameraRight;

        if (moveDirection != Vector3.zero)
        {
            _currentSelectedObject.transform.position += moveDirection.normalized * (moveSpeed * Time.deltaTime);
        }

        // 3. Tangani Rotasi Objek (Rotate)
        if (_isRotatingLeft)
        {
            _currentSelectedObject.transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
        }
        if (_isRotatingRight)
        {
            _currentSelectedObject.transform.Rotate(Vector3.up, -rotateSpeed * Time.deltaTime, Space.World);
        }
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