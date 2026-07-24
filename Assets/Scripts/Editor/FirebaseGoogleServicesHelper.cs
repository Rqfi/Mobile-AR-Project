#if UNITY_ANDROID
using UnityEngine;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;

public class FirebaseGoogleServicesHelper : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 999;

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string googleServicesSource = Path.Combine(
            Application.dataPath, "google-services.json");

        if (!File.Exists(googleServicesSource))
        {
            Debug.LogError("[Firebase] google-services.json tidak ditemukan di Assets/");
            return;
        }

        // Copy ke launcher module
        string launcherPath = Path.Combine(
            Path.GetDirectoryName(path), "launcher");

        if (!Directory.Exists(launcherPath))
            Directory.CreateDirectory(launcherPath);

        string destination = Path.Combine(launcherPath, "google-services.json");
        File.Copy(googleServicesSource, destination, overwrite: true);

        Debug.Log($"[Firebase] google-services.json di-copy ke: {destination}");
    }
}
#endif