using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private const string SAVE_FILE_NAME = "idle_tycoon_save.json";
    
    public SaveData CurrentSaveData { get; private set; }
    public Action OnGameLoaded; 
    public Action OnBeforeSave;

    [Header("Debug")]
    public bool LoadOnStart = true;
    
    // Tối ưu hóa I/O (isDirty Flag)
    public bool isDirty = true; // Mặc định true để lưu lần đầu
    
    public void MarkAsDirty()
    {
        isDirty = true;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (LoadOnStart) LoadGame();
        StartCoroutine(AutoSaveRoutine());
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            if (!isTransitioning) SaveGame();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (Instance != this) return; 
        if (pauseStatus) SaveGame();
    }

    private void OnApplicationQuit()
    {
        if (Instance != this) return; 
        SaveGame();
    }

    public string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    }

    public static bool isTransitioning = false;

    [ContextMenu("Save Game")]
    public void SaveGame()
    {
        if (Instance != this) return; 
        if (isTransitioning) return; 
        if (!isDirty) return; // Chỉ lưu khi có thay đổi thực sự
        
        if (CurrentSaveData == null) CurrentSaveData = new SaveData();
        
        OnBeforeSave?.Invoke();

        // 1. Cập nhật dữ liệu từ GameManager
        if (Gamemanager.Instance != null)
        {
            CurrentSaveData.IdleCash = Gamemanager.Instance.IdleCash;
            CurrentSaveData.LifetimeCash = Gamemanager.Instance.LifetimeCash;
            CurrentSaveData.PlayerLevel = Gamemanager.Instance.PlayerLevel;
            CurrentSaveData.PrestigeMultiplier = Gamemanager.Instance.PrestigeMultiplier;
            CurrentSaveData.CurrentRound = Gamemanager.Instance.CurrentRound;
        }

        CurrentSaveData.LastSaveTimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CurrentSaveData.LastSaveUptimeSeconds = Environment.TickCount / 1000;

        // 2. ISaveable Collection (Facilities, Bosses, Managers, etc)
        CurrentSaveData.MineShafts.Clear();
        CurrentSaveData.BossHealths.Clear();
        
        var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>();
        foreach (var saveable in saveables)
        {
            saveable.PopulateSaveData(CurrentSaveData);
        }

        // 3. Ghi file với mã hóa Base64
        string json = JsonUtility.ToJson(CurrentSaveData, true);
        string encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        
        // Đẩy việc ghi file sang luồng nền để chống khựng Main Thread (Async I/O)
        string filePath = GetSaveFilePath();
        Task.Run(() => 
        {
            File.WriteAllText(filePath, encoded);
            Debug.Log($"[SaveManager] Saved game at {filePath} (Async)");
        });
        
        isDirty = false; // Đã lưu xong, tắt cờ
    }

    public void SaveResetState()
    {
        CurrentSaveData = new SaveData(); 
        if (Gamemanager.Instance != null)
        {
            CurrentSaveData.IdleCash = Gamemanager.Instance.IdleCash;
            CurrentSaveData.LifetimeCash = Gamemanager.Instance.LifetimeCash;
            CurrentSaveData.PlayerLevel = Gamemanager.Instance.PlayerLevel;
            CurrentSaveData.PrestigeMultiplier = Gamemanager.Instance.PrestigeMultiplier;
            CurrentSaveData.CurrentRound = Gamemanager.Instance.CurrentRound;
        }
        CurrentSaveData.LastSaveTimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        CurrentSaveData.LastSaveUptimeSeconds = Environment.TickCount / 1000;
        
        string json = JsonUtility.ToJson(CurrentSaveData, true);
        string encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
        File.WriteAllText(GetSaveFilePath(), encoded); // Cố tình đồng bộ ở đây vì đang chuyển scene
        isDirty = false;
        Debug.Log("[SaveManager] Đã lưu Reset State (Chuyển sinh/Qua màn).");
    }

    [ContextMenu("Load Game")]
    public void LoadGame()
    {
        string path = GetSaveFilePath();
        if (File.Exists(path))
        {
            string fileContent = File.ReadAllText(path);
            try {
                byte[] decodedBytes = Convert.FromBase64String(fileContent);
                string json = System.Text.Encoding.UTF8.GetString(decodedBytes);
                CurrentSaveData = JsonUtility.FromJson<SaveData>(json);
            } catch (Exception) {
                // Tương thích ngược với file save cũ (JSON thuần)
                CurrentSaveData = JsonUtility.FromJson<SaveData>(fileContent);
            }
            
            Debug.Log($"[SaveManager] Loaded game data.");
            OnGameLoaded?.Invoke();
            StartCoroutine(ApplyDataAndCalculateOfflineRoutine());
        }
        else
        {
            Debug.Log($"[SaveManager] No save file found. Starting fresh.");
            CurrentSaveData = new SaveData();
        }
    }

    [ContextMenu("Delete Save")]
    public void DeleteSaveData()
    {
        string path = GetSaveFilePath();
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log("[SaveManager] Save file deleted.");
        }
        CurrentSaveData = new SaveData();
    }

    private IEnumerator ApplyDataAndCalculateOfflineRoutine()
    {
        yield return new WaitForEndOfFrame(); 
        
        // 1. Phục hồi các ISaveable (MineShaft, Elevator, Warehouse, BossHealth, Boss states)
        // QUAN TRỌNG: ManagerController PHẢI được load đầu tiên vì các Hầm (MineShaft) cần lấy dữ liệu Manager để gán lên UI.
        var saveables = FindObjectsOfType<MonoBehaviour>(true).OfType<ISaveable>().ToList();
        
        var managerSaveable = saveables.FirstOrDefault(s => s is ManagerController);
        if (managerSaveable != null)
        {
            managerSaveable.LoadFromSaveData(CurrentSaveData);
            saveables.Remove(managerSaveable);
        }

        foreach (var saveable in saveables)
        {
            saveable.LoadFromSaveData(CurrentSaveData);
        }

        // 2. Xử lý Offline Progression
        yield return new WaitForSeconds(0.4f);
        if (OfflineProgressionManager.Instance != null)
        {
            OfflineProgressionManager.Instance.ProcessOfflineProgression(CurrentSaveData);
        }
        else 
        {
            Debug.LogWarning("[SaveManager] OfflineProgressionManager Instance is null! Thêm script này vào 1 GameObject (vd GameManager).");
        }
    }
}
