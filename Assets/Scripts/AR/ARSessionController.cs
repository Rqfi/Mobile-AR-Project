using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ARSessionController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button btnBack;
    [SerializeField] private Button btnScreenshot;
    [SerializeField] private GameObject flashOverlay;

    private void Start()
    {
        btnBack.onClick.AddListener(OnBackClick);
        btnScreenshot.onClick.AddListener(OnScreenshotClick);

        // Sembunyikan flash overlay
        if (flashOverlay != null)
            flashOverlay.SetActive(false);
    }

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
}