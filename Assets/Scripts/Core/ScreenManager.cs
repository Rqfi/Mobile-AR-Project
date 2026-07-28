using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class ScreenManager : MonoBehaviour
{
    public static ScreenManager Instance { get; private set; }

    [Header("Screen GameObjects")]
    [SerializeField] private GameObject screenDashboard;
    [SerializeField] private GameObject screenKatalog;
    [SerializeField] private GameObject screenDetailFurnitur;
    [SerializeField] private GameObject screenProyek;
    [SerializeField] private GameObject screenDetailProyek;
    [SerializeField] private GameObject screenTangkapanLayar;
    [SerializeField] private GameObject screenDetailFoto;

    private Dictionary<string, GameObject> _screens;
    private Stack<string> _history = new Stack<string>();
    private GameObject _currentScreen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _screens = new Dictionary<string, GameObject>
    {
        { "Dashboard",      screenDashboard },
        { "Katalog",        screenKatalog },
        { "DetailFurnitur", screenDetailFurnitur },
        { "Proyek",         screenProyek },
        { "DetailProyek",   screenDetailProyek },
        { "TangkapanLayar", screenTangkapanLayar },
        { "DetailFoto",     screenDetailFoto },
    };

        // Disable SEMUA screen dulu — cegah Dashboard aktif bersamaan
        foreach (var screen in _screens.Values)
            if (screen != null) screen.SetActive(false);

        string startScreen = AppState.ReturnToScreen ?? "Dashboard";
        AppState.ReturnToScreen = null;
        Debug.Log($"[ScreenManager] Awake → {startScreen}");
        NavigateTo(startScreen, clearHistory: true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void NavigateTo(string screenName, bool clearHistory = false)
    {
        Debug.Log($"[ScreenManager] NavigateTo: {screenName}, instance: {GetInstanceID()}");

        if (!_screens.ContainsKey(screenName))
        {
            Debug.LogWarning($"Screen '{screenName}' tidak ditemukan.");
            return;
        }

        if (clearHistory)
            _history.Clear();
        else if (_currentScreen != null)
            _history.Push(GetCurrentScreenName());

        SwitchTo(_screens[screenName]);
    }

    public bool NavigateBack()
    {
        if (_history.Count == 0) return false;
        SwitchTo(_screens[_history.Pop()]);
        return true;
    }

    public bool CanGoBack() => _history.Count > 0;

    public void OpenARSession()
    {
        AppState.ReturnToScreen = GetCurrentScreenName();
        SceneManager.LoadScene("ARSession");
    }

    public void BackToMain() => SceneManager.LoadScene("Main");

    public void ShowDashboard() => NavigateTo("Dashboard", clearHistory: true);
    public void ShowKatalog() => NavigateTo("Katalog");
    public void ShowDetailFurnitur() => NavigateTo("DetailFurnitur");
    public void ShowProyek() => NavigateTo("Proyek");
    public void ShowDetailProyek() => NavigateTo("DetailProyek");
    public void ShowTangkapanLayar() => NavigateTo("TangkapanLayar");
    public void ShowDetailFoto() => NavigateTo("DetailFoto");

    private void SwitchTo(GameObject target)
    {
        if (target == null) return;
        _currentScreen?.SetActive(false);
        target.SetActive(true);
        _currentScreen = target;
    }

    private string GetCurrentScreenName()
    {
        foreach (var pair in _screens)
            if (pair.Value == _currentScreen)
                return pair.Key;
        return "";
    }
}