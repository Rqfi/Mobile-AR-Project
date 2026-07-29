using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;

public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance { get; private set; }

    public bool IsReady { get; private set; } = false;

    private FirebaseAuth _auth;
    private FirebaseFirestore _db;
    private string _userId;

    public string UserId => _userId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeFirebase();
    }

    private async void InitializeFirebase()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();

            if (dependencyStatus != DependencyStatus.Available)
            {
                Debug.LogError($"[Firebase] Dependency error: {dependencyStatus}");
                return;
            }

            _auth = FirebaseAuth.DefaultInstance;
            _db = FirebaseFirestore.DefaultInstance;

            await SignInAnonymous();

            IsReady = true;
            Debug.Log($"[Firebase] Ready. UserId: {_userId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase] Init failed: {e.Message}");
        }
    }

    private async Task SignInAnonymous()
    {
        if (_auth.CurrentUser != null)
        {
            _userId = _auth.CurrentUser.UserId;
            return;
        }

        var result = await _auth.SignInAnonymouslyAsync();
        _userId = result.User.UserId;
        Debug.Log($"[Firebase] Signed in anonymously: {_userId}");
    }

    // ── Reference Helpers ──────────────────────────────

    private CollectionReference ProjectsRef() =>
        _db.Collection("users").Document(_userId).Collection("projects");

    private CollectionReference ScreenshotsRef(string projectId) =>
        ProjectsRef().Document(projectId).Collection("screenshots");

    // ── Proyek ─────────────────────────────────────────

    public async Task<List<ProyekData>> GetAllProyekAsync()
    {
        var result = new List<ProyekData>();
        try
        {
            var snapshot = await ProjectsRef()
                .OrderByDescending("tanggal")
                .GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                string thumbPath = "";
                try { thumbPath = doc.GetValue<string>("thumbnailPath"); }
                catch { }

                result.Add(new ProyekData
                {
                    id = doc.Id,
                    nama = doc.GetValue<string>("nama"),
                    tanggal = doc.GetValue<string>("tanggal"),
                    thumbnailPath = thumbPath,
                    screenshots = new List<ScreenshotData>()
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Firebase] GetAllProyek: {e.Message}");
        }
        return result;
    }

    public async Task<string> GetOrCreateProyekAsync(string nama)
    {
        try
        {
            var snapshot = await ProjectsRef()
                .WhereEqualTo("nama", nama)
                .Limit(1)
                .GetSnapshotAsync();

            // Ganti FirstOrDefault dengan loop manual
            DocumentSnapshot firstDoc = null;
            foreach (var doc in snapshot.Documents)
            {
                firstDoc = doc;
                break;
            }

            if (firstDoc != null)
                return firstDoc.Id;

            var data = new Dictionary<string, object>
        {
            { "nama",    nama },
            { "tanggal", DateTime.Now.ToString("o") }
        };

            var docRef = await ProjectsRef().AddAsync(data);
            Debug.Log($"[Firebase] Proyek dibuat: {docRef.Id}");
            return docRef.Id;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase] GetOrCreateProyek: {e.Message}");
            return null;
        }
    }

    public async Task DeleteProyekAsync(string projectId)
    {
        try
        {
            var screenshots = await ScreenshotsRef(projectId).GetSnapshotAsync();
            foreach (var doc in screenshots.Documents)
                await doc.Reference.DeleteAsync();

            await ProjectsRef().Document(projectId).DeleteAsync();
            Debug.Log($"[Firebase] Proyek dihapus: {projectId}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Firebase] DeleteProyek: {e.Message}");
        }
    }

    // ── Screenshot ─────────────────────────────────────

    public async Task AddScreenshotAsync(
    string projectId,
    string nama,
    string catatan,
    string localPath)
    {
        try
        {
            var data = new Dictionary<string, object>
        {
            { "nama",      nama },
            { "catatan",   catatan },
            { "localPath", localPath },
            { "tanggal",   DateTime.Now.ToString("o") }
        };

            await ScreenshotsRef(projectId).AddAsync(data);

            var proyekDoc = await ProjectsRef().Document(projectId).GetSnapshotAsync();
            string existingThumb = "";
            try { existingThumb = proyekDoc.GetValue<string>("thumbnailPath"); }
            catch { }

            if (string.IsNullOrEmpty(existingThumb) && !string.IsNullOrEmpty(localPath))
            {
                await ProjectsRef().Document(projectId).UpdateAsync(
                    new Dictionary<string, object>
                    {
                    { "thumbnailPath", localPath }
                    });
            }

            Debug.Log($"[Firebase] Screenshot disimpan ke proyek: {projectId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase] AddScreenshot: {e.Message}");
        }
    }

    public async Task<List<ScreenshotData>> GetScreenshotsAsync(string projectId)
    {
        var result = new List<ScreenshotData>();
        try
        {
            var snapshot = await ScreenshotsRef(projectId)
                .OrderByDescending("tanggal")
                .GetSnapshotAsync();

            foreach (var doc in snapshot.Documents)
            {
                result.Add(new ScreenshotData
                {
                    id = doc.Id,
                    nama = doc.GetValue<string>("nama"),
                    catatan = doc.GetValue<string>("catatan"),
                    path = doc.GetValue<string>("localPath"),
                    tanggal = doc.GetValue<string>("tanggal")
                });
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Firebase] GetScreenshots: {e.Message}");
        }
        return result;
    }

    public async Task DeleteScreenshotAsync(string projectId, string screenshotId)
    {
        try
        {
            await ScreenshotsRef(projectId)
                .Document(screenshotId)
                .DeleteAsync();

            Debug.Log($"[Firebase] Screenshot dihapus: {screenshotId}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Firebase] DeleteScreenshot: {e.Message}");
        }
    }

    public async Task<ProyekData> GetProyekByIdAsync(string projectId)
    {
        try
        {
            var doc = await ProjectsRef()
                .Document(projectId)
                .GetSnapshotAsync();

            if (!doc.Exists) return null;

            string thumbPath = "";
            try { thumbPath = doc.GetValue<string>("thumbnailPath"); }
            catch { }

            return new ProyekData
            {
                id = doc.Id,
                nama = doc.GetValue<string>("nama"),
                tanggal = doc.GetValue<string>("tanggal"),
                thumbnailPath = thumbPath,
                screenshots = new System.Collections.Generic.List<ScreenshotData>()
            };
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Firebase] GetProyekById: {e.Message}");
            return null;
        }
    }

    public async Task UpdateProyekNamaAsync(string projectId, string namaBaru)
    {
        try
        {
            await ProjectsRef().Document(projectId).UpdateAsync(
                new Dictionary<string, object>
                {
                { "nama", namaBaru }
                });

            Debug.Log($"[Firebase] Nama proyek diupdate: {namaBaru}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase] UpdateProyekNama: {e.Message}");
            throw;
        }
    }

    public async Task<List<FurnitureItem>> GetKatalogAsync()
    {
        var result = new List<FurnitureItem>();
        try
        {
            var snapshot = await _db.Collection("katalog").GetSnapshotAsync();
            foreach (var doc in snapshot.Documents)
            {
                var item = new FurnitureItem { id = doc.Id };
                try { item.name = doc.GetValue<string>("name"); } catch { item.name = "Tanpa Nama"; }
                try { item.category = doc.GetValue<string>("category"); } catch { item.category = "Lainnya"; }
                try { item.description = doc.GetValue<string>("description"); } catch { item.description = ""; }
                try { item.width = doc.GetValue<float>("width"); } catch { item.width = 0f; }
                try { item.depth = doc.GetValue<float>("depth"); } catch { item.depth = 0f; }
                try { item.height = doc.GetValue<float>("height"); } catch { item.height = 0f; }
                try { item.scale = doc.GetValue<float>("scale"); } catch { item.scale = 1.0f; }
                try { item.thumbnailUrl = doc.GetValue<string>("thumbnailUrl"); } catch { item.thumbnailUrl = ""; }
                try { item.modelUrl = doc.GetValue<string>("modelUrl"); } catch { item.modelUrl = ""; }
                result.Add(item);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Firebase] GetKatalogAsync failed: {e.Message}");
        }
        return result;
    }
}