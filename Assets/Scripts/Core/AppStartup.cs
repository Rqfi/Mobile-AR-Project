using UnityEngine;

public class AppStartup : MonoBehaviour
{
    private void Awake()
    {
        // Tampilkan navbar Android
        Screen.fullScreenMode = FullScreenMode.Windowed;

        // Jaga orientasi portrait
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
    }
}