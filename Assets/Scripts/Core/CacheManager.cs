using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class CacheManager
{
    private static readonly string CacheDirectory = Path.Combine(Application.persistentDataPath, "GLBCache");

    public static async Task<string> GetLocalGLBPath(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        // Buat direktori cache jika belum ada
        if (!Directory.Exists(CacheDirectory))
        {
            Directory.CreateDirectory(CacheDirectory);
        }

        // Buat nama file unik berdasarkan hash MD5 dari URL
        string fileName = GetMd5Hash(url) + ".glb";
        string localPath = Path.Combine(CacheDirectory, fileName);

        // Jika file sudah ada di lokal, gunakan file tersebut
        if (File.Exists(localPath))
        {
            Debug.Log($"[GLBCache] Memuat dari cache lokal: {localPath}");
            return localPath;
        }

        // Jika belum ada di lokal, unduh dari URL
        Debug.Log($"[GLBCache] File tidak ditemukan di lokal. Mengunduh: {url}");
        bool success = await DownloadFileAsync(url, localPath);
        if (success)
        {
            Debug.Log($"[GLBCache] Download berhasil dan disimpan ke: {localPath}");
            return localPath;
        }

        // Jika gagal mengunduh, kembalikan null agar script fallback ke URL asli
        Debug.LogWarning("[GLBCache] Gagal mengunduh file, fallback ke URL asli.");
        return null;
    }

    private static async Task<bool> DownloadFileAsync(string url, string destinationPath)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            var operation = webRequest.SendWebRequest();

            // Tunggu hingga proses download selesai
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[GLBCache] Error saat mendownload GLB: {webRequest.error}");
                return false;
            }

            try
            {
                // Tulis data byte yang diunduh ke file lokal
                byte[] data = webRequest.downloadHandler.data;
                File.WriteAllBytes(destinationPath, data);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[GLBCache] Gagal menulis file ke disk: {e.Message}");
                return false;
            }
        }
    }

    private static string GetMd5Hash(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
