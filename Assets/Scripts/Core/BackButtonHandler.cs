using UnityEngine;
using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class BackButtonHandler : MonoBehaviour
{
    private static BackButtonHandler _instance;

    private bool _waitingForExitConfirm = false;
    private float _exitConfirmTimer = 0f;
    private const float ExitConfirmDuration = 2f;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        if (IsBackPressed())
            HandleBack();

        if (_waitingForExitConfirm)
        {
            _exitConfirmTimer -= Time.deltaTime;
            if (_exitConfirmTimer <= 0f)
                _waitingForExitConfirm = false;
        }
    }

    private bool IsBackPressed()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        return Keyboard.current != null &&
               Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }

    private void HandleBack()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "ARSession")
        {
            SceneManager.LoadScene("Main");
            return;
        }

        // ScreenManager mungkin belum ready jika scene baru saja dimuat
        if (ScreenManager.Instance == null) return;

        if (ScreenManager.Instance.CanGoBack())
        {
            ScreenManager.Instance.NavigateBack();
            return;
        }

        ShowExitConfirmation();
    }

    private void ShowExitConfirmation()
    {
        if (_waitingForExitConfirm)
        {
            Application.Quit();
            return;
        }

        _waitingForExitConfirm = true;
        _exitConfirmTimer = ExitConfirmDuration;
        ShowToast("Tekan sekali lagi untuk keluar");
    }

    private void ShowToast(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var toastClass = new AndroidJavaClass("android.widget.Toast");
            activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                toastClass.CallStatic<AndroidJavaObject>(
                    "makeText", activity, message,
                    toastClass.GetStatic<int>("LENGTH_SHORT"))
                    .Call("show");
            }));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Toast] " + e.Message);
        }
#else
        Debug.Log($"[Toast] {message}");
#endif
    }
}