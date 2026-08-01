using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    private const string SAVE_FILE_NAME = "idle_tycoon_save.json";
    
    // Lưu tạm thời dữ liệu để các component khác truy cập
    public SaveData CurrentSaveData { get; private set; }

    public Action OnGameLoaded; // Event gọi khi load game thành công

    [Header("Debug")]
    public bool LoadOnStart = true;

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
        if (LoadOnStart)
        {
            LoadGame();
        }
        StartCoroutine(AutoSaveRoutine());
    }

    private IEnumerator AutoSaveRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(10f);
            if (!isTransitioning)
            {
                SaveGame();
            }
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (Instance != this) return; // Chặn Evil Twin
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    private void OnApplicationQuit()
    {
        if (Instance != this) return; // Chặn Evil Twin
        SaveGame();
    }

    public string GetSaveFilePath()
    {
        return Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    }

    [ContextMenu("Save Game")]
    public void SaveGame()
    {
        if (Instance != this) return; // Chặn Evil Twin
        if (isTransitioning) return; // Chặn lưu game nếu đang trong quá trình chuyển scene
        if (CurrentSaveData == null) CurrentSaveData = new SaveData();

        // 1. Cập nhật dữ liệu từ GameManager
        if (Gamemanager.Instance != null)
        {
            CurrentSaveData.IdleCash = Gamemanager.Instance.IdleCash;
            CurrentSaveData.LifetimeCash = Gamemanager.Instance.LifetimeCash;
            CurrentSaveData.PlayerLevel = Gamemanager.Instance.PlayerLevel;
            CurrentSaveData.PrestigeMultiplier = Gamemanager.Instance.PrestigeMultiplier;
            CurrentSaveData.CurrentRound = Gamemanager.Instance.CurrentRound;
        }

        // 2. Ghi nhận thời gian hiện tại
        CurrentSaveData.LastSaveTimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // --- PROJECTILE SWEEPING (Chống Gian Lận Đạn Rồng) ---
        DragonFireball[] fireballs = FindObjectsOfType<DragonFireball>();
        foreach (var fb in fireballs)
        {
            if (fb.targetShaft != null)
            {
                MineShaft target = fb.targetShaft.GetComponent<MineShaft>();
                if (target != null)
                {
                    // Ép hầm nhận sát thương lửa ngay lập tức trước khi xuất file Save
                    target.TriggerBurnVFX(fb.burnDuration, fb.isBigFireball);
                }
            }
        }

        // 3. Lấy data từ các Facility
        CurrentSaveData.MineShafts.Clear();
        MineShaft[] shafts = FindObjectsOfType<MineShaft>();
        foreach (var shaft in shafts)
        {
            CurrentSaveData.MineShafts.Add(shaft.SaveState());
        }

        Elevator elevator = FindObjectOfType<Elevator>();
        if (elevator != null) CurrentSaveData.Elevator = elevator.SaveState();

        Warehouse warehouse = FindObjectOfType<Warehouse>();
        if (warehouse != null) CurrentSaveData.Warehouse = warehouse.SaveState();

        // 4. Lấy data từ ManagerController
        if (ManagerController.Instance != null)
        {
            CurrentSaveData.OwnedManagers.Clear();
            foreach (var md in ManagerController.Instance.OwnedManagers)
            {
                ManagerSaveData smd = new ManagerSaveData();
                smd.Id = md.Id;
                smd.Name = md.Name;
                smd.CharacterID = md.CharacterID;
                smd.Rarity = (int)md.Rarity;
                smd.BuffType = (int)md.BuffType;
                smd.AssignedFacilityType = (int)md.AssignedFacilityType;
                smd.SpecialFeature = (int)md.SpecialFeature;
                smd.BuffValue = md.BuffValue;
                smd.BuffDuration = md.BuffDuration;
                smd.CooldownDuration = md.CooldownDuration;
                smd.OriginalHirePrice = md.OriginalHirePrice;
                smd.IsAssigned = md.IsAssigned;
                smd.AssignedShaftId = md.AssignedShaftId;
                
                // Toán học Fast-Forward: Tính khoảng thời gian còn lại (số giây) và cộng vào mốc thời gian Unix hiện tại
                long currentUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                float remainingSkill = Mathf.Max(0, md.SkillEndTime - Time.time);
                float remainingCooldown = Mathf.Max(0, md.CooldownEndTime - Time.time);
                
                smd.SkillEndUnixTime = currentUnix + (long)remainingSkill;
                smd.CooldownEndUnixTime = currentUnix + (long)remainingCooldown;
                
                CurrentSaveData.OwnedManagers.Add(smd);
            }
        }

        // 5. Lưu trạng thái Boss & Barrier
        if (Gamemanager.Instance != null && Gamemanager.Instance.CurrentRound == 3)
        {
            RoundTransitionBarrier barrier = FindObjectOfType<RoundTransitionBarrier>(true);
            if (barrier != null)
            {
                CurrentSaveData.IsRound3BarrierBroken = !barrier.gameObject.activeInHierarchy;
            }
            
            CurrentSaveData.BossHealths.Clear();
            BossHealth[] bosses = FindObjectsOfType<BossHealth>(true);
            foreach (var boss in bosses)
            {
                CurrentSaveData.BossHealths.Add(new BossHealthSaveData
                {
                    BossName = boss.gameObject.name,
                    CurrentHealth = boss.CurrentHealth,
                    IsDead = boss.IsDead
                });
            }

            BossPhase3Controller boss3 = FindObjectOfType<BossPhase3Controller>(true);
            if (boss3 != null)
            {
                CurrentSaveData.BossData.Skill4Timer = boss3.CurrentSkill4Timer;
            }
            
            CurrentSaveData.BossData.ElevatorBarrierTimer = 0f;
            CurrentSaveData.BossData.WarehouseBarrierTimer = 0f;
            BarrierController[] barriers = FindObjectsOfType<BarrierController>(true);
            foreach (var b in barriers)
            {
                if (b.gameObject.activeInHierarchy && b.currentTimer > 0)
                {
                    if (b.type == BarrierController.BarrierType.Elevator) CurrentSaveData.BossData.ElevatorBarrierTimer = b.currentTimer;
                    if (b.type == BarrierController.BarrierType.Warehouse) CurrentSaveData.BossData.WarehouseBarrierTimer = b.currentTimer;
                }
            }
        }

        string json = JsonUtility.ToJson(CurrentSaveData, true);
        File.WriteAllText(GetSaveFilePath(), json);
        
        Debug.Log($"[SaveManager] Saved game at {GetSaveFilePath()}");
    }

    public static bool isTransitioning = false;

    // Hàm lưu state sạch sẽ chuyên dùng cho Prestige và Next Round
    public void SaveResetState()
    {
        CurrentSaveData = new SaveData(); // Tạo file mới tinh, xóa mọi Hầm/Quản lý cũ

        if (Gamemanager.Instance != null)
        {
            CurrentSaveData.IdleCash = Gamemanager.Instance.IdleCash;
            CurrentSaveData.LifetimeCash = Gamemanager.Instance.LifetimeCash;
            CurrentSaveData.PlayerLevel = Gamemanager.Instance.PlayerLevel;
            CurrentSaveData.PrestigeMultiplier = Gamemanager.Instance.PrestigeMultiplier;
            CurrentSaveData.CurrentRound = Gamemanager.Instance.CurrentRound;
        }

        CurrentSaveData.LastSaveTimeUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string json = JsonUtility.ToJson(CurrentSaveData, true);
        File.WriteAllText(GetSaveFilePath(), json);
        Debug.Log("[SaveManager] Đã lưu Reset State (Chuyển sinh/Qua màn).");
    }

    [ContextMenu("Load Game")]
    public void LoadGame()
    {
        string path = GetSaveFilePath();
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            CurrentSaveData = JsonUtility.FromJson<SaveData>(json);
            Debug.Log($"[SaveManager] Loaded game data.");
            
            // Bắn event trước để khởi tạo UI và Manager
            OnGameLoaded?.Invoke();
            
            // Trì hoãn 1 frame chờ các đối tượng Awake/Start
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
        yield return new WaitForEndOfFrame(); // Chờ các Object xuất hiện
        
        // 0. Phục hồi Quản lý (ManagerController)
        if (ManagerController.Instance != null && CurrentSaveData.OwnedManagers != null)
        {
            ManagerController.Instance.OwnedManagers.Clear();
            foreach (var smd in CurrentSaveData.OwnedManagers)
            {
                ManagerData md = new ManagerData();
                md.Id = smd.Id;
                md.Name = smd.Name;
                md.CharacterID = smd.CharacterID;
                md.Rarity = (ManagerRarity)smd.Rarity;
                md.BuffType = (ManagerBuffType)smd.BuffType;
                md.AssignedFacilityType = (FacilityType)smd.AssignedFacilityType;
                md.SpecialFeature = (SeniorSpecialFeature)smd.SpecialFeature;
                md.BuffValue = smd.BuffValue;
                md.BuffDuration = smd.BuffDuration;
                md.CooldownDuration = smd.CooldownDuration;
                md.OriginalHirePrice = smd.OriginalHirePrice;
                md.IsAssigned = smd.IsAssigned;
                md.AssignedShaftId = smd.AssignedShaftId;
                
                // Toán học Fast-Forward: Lấy thời điểm tương lai trừ đi thời điểm hiện tại để ra số giây còn lại
                long currentUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                float remainingSkill = Mathf.Max(0, smd.SkillEndUnixTime - currentUnix);
                float remainingCooldown = Mathf.Max(0, smd.CooldownEndUnixTime - currentUnix);
                
                // Gán ngược lại vào hệ quy chiếu Time.time của session hiện tại
                md.SkillEndTime = Time.time + remainingSkill;
                md.CooldownEndTime = Time.time + remainingCooldown;
                
                ManagerController.Instance.OwnedManagers.Add(md);
            }
            // Thông báo UI cập nhật lại danh sách quản lý
            ManagerController.Instance.OnManagerListUpdated?.Invoke();
        }

        // 1. Áp dụng Data vào các Facility hiện có trong Scene (Các hầm sẽ lấy Manager từ danh sách trên)
        MineShaft[] shafts = FindObjectsOfType<MineShaft>();
        foreach (var shaft in shafts)
        {
            var savedData = CurrentSaveData.MineShafts.Find(s => s.Index == shaft.ShaftIndex);
            if (savedData != null) 
            {
                shaft.LoadState(savedData);
            }
        }
        
        // 1.2 Phục hồi Minions
        BossPhase3Controller boss3Phase = FindObjectOfType<BossPhase3Controller>(true);
        if (boss3Phase != null)
        {
            foreach (var shaft in shafts)
            {
                var savedData = CurrentSaveData.MineShafts.Find(s => s.Index == shaft.ShaftIndex);
                if (savedData != null && savedData.MinionHealth > 0)
                {
                    boss3Phase.RestoreMinion(shaft, savedData.MinionHealth);
                }
            }
        }

        Elevator elevator = FindObjectOfType<Elevator>();
        if (elevator != null && CurrentSaveData.Elevator != null)
        {
            elevator.LoadState(CurrentSaveData.Elevator);
        }

        Warehouse warehouse = FindObjectOfType<Warehouse>();
        if (warehouse != null && CurrentSaveData.Warehouse != null)
        {
            warehouse.LoadState(CurrentSaveData.Warehouse);
        }

        // 1.5. Áp dụng trạng thái Boss & Barrier ở Round 3
        if (Gamemanager.Instance != null && Gamemanager.Instance.CurrentRound == 3)
        {
            RoundTransitionBarrier barrier = FindObjectOfType<RoundTransitionBarrier>(true);
            if (barrier != null && CurrentSaveData.IsRound3BarrierBroken)
            {
                barrier.gameObject.SetActive(false);
            }
            
            BossHealth[] bosses = FindObjectsOfType<BossHealth>(true);
            foreach (var boss in bosses)
            {
                var bossData = CurrentSaveData.BossHealths.Find(b => b.BossName == boss.gameObject.name);
                if (bossData != null)
                {
                    boss.LoadState(bossData);
                }
                
                if (CurrentSaveData.IsRound3BarrierBroken && !boss.IsDead)
                {
                    boss.RemoveInvincibility();
                }
                else
                {
                    boss.IsInvincible = true; // Khắc phục lỗi load game trước GameManager khiến Boss bị mất bất tử
                }
            }
            
            if (CurrentSaveData.IsRound3BarrierBroken)
            {
                DragonBossController dragon = FindObjectOfType<DragonBossController>(true);
                if (dragon != null) dragon.LoadEnrage();
                
                BossPhase3Controller boss3 = FindObjectOfType<BossPhase3Controller>(true);
                if (boss3 != null)
                {
                    boss3.LoadEnrage();
                    if (CurrentSaveData.BossData != null && CurrentSaveData.BossData.Skill4Timer > 0)
                    {
                        boss3.CurrentSkill4Timer = CurrentSaveData.BossData.Skill4Timer;
                        boss3.SendMessage("ExecuteSkill4", SendMessageOptions.DontRequireReceiver); // Gọi lại chiêu độc
                    }
                    
                    if (CurrentSaveData.BossData != null)
                    {
                        if (CurrentSaveData.BossData.ElevatorBarrierTimer > 0 && boss3.elevatorBarrier != null)
                        {
                            BarrierController bc = boss3.elevatorBarrier.GetComponent<BarrierController>();
                            if (bc != null) bc.currentTimer = CurrentSaveData.BossData.ElevatorBarrierTimer;
                            boss3.elevatorBarrier.SetActive(true);
                        }
                        if (CurrentSaveData.BossData.WarehouseBarrierTimer > 0 && boss3.warehouseBarrier != null)
                        {
                            BarrierController bc = boss3.warehouseBarrier.GetComponent<BarrierController>();
                            if (bc != null) bc.currentTimer = CurrentSaveData.BossData.WarehouseBarrierTimer;
                            boss3.warehouseBarrier.SetActive(true);
                        }
                    }
                }
            }
        }

        // 2. Xử lý Offline Progression
        // Chờ 0.4 giây để Unity xả hết giật lag (lag spike) lúc mới load scene rồi mới kích hoạt bảng Offline
        yield return new WaitForSeconds(0.4f);
        ProcessOfflineProgression();
    }

    private void ProcessOfflineProgression()
    {
        if (CurrentSaveData == null) 
        {
            Debug.Log("[Offline] CurrentSaveData is null. Aborting.");
            return;
        }
        if (CurrentSaveData.LastSaveTimeUnixSeconds == 0) 
        {
            Debug.Log("[Offline] LastSaveTimeUnixSeconds is 0 (new save). Aborting.");
            return;
        }

        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long offlineSeconds = currentTime - CurrentSaveData.LastSaveTimeUnixSeconds;
        
        Debug.Log($"[Offline] CurrentTime: {currentTime}, LastSaveTime: {CurrentSaveData.LastSaveTimeUnixSeconds}, OfflineSeconds: {offlineSeconds}");

        if (offlineSeconds <= 0) 
        {
            Debug.Log("[Offline] offlineSeconds <= 0. Aborting.");
            return;
        }

        // Theo thiết kế: Max offline time 4 giờ
        long maxOfflineSeconds = 14400; 
        long effectiveOfflineSeconds = Math.Min(offlineSeconds, maxOfflineSeconds);

        Debug.Log($"[SaveManager] Player was offline for {offlineSeconds}s. Calculating rewards for {effectiveOfflineSeconds}s.");

        // Tính Offline Math
        double offlineIncomeRate = CalculateBaseBottleneckIncome();
        // Thu nhập offline chỉ bằng 25% (0.25)
        double totalOfflineCash = offlineIncomeRate * effectiveOfflineSeconds * 0.25;

        Debug.Log($"[Offline] BaseIncomeRate (Bottleneck): {offlineIncomeRate}, TotalOfflineCash: {totalOfflineCash}");

        if (totalOfflineCash > 0)
        {
            Debug.Log($"[SaveManager] Offline Cash Earned: {totalOfflineCash}");
            Gamemanager.Instance.AddCash(totalOfflineCash);
            
            // Bật Popup UI báo cáo cho người chơi
            if (OfflineModalUI.Instance != null)
            {
                OfflineModalUI.Instance.ShowOfflineRewards(effectiveOfflineSeconds, totalOfflineCash);
            }
            else
            {
                Debug.LogWarning("[SaveManager] Không tìm thấy OfflineModalUI trong scene. Vui lòng kiểm tra GameObject chứa script này có đang bị Disable không.");
            }
        }
        else
        {
            Debug.Log("[Offline] totalOfflineCash is 0. No modal will be shown.");
        }
    }

    private double CalculateBaseBottleneckIncome()
    {
        double totalShaftIncome = 0;
        MineShaft[] shafts = FindObjectsOfType<MineShaft>();
        foreach (var shaft in shafts)
        {
            if (!shaft.isBroken && shaft.currentManager != null) 
            {
                var savedData = CurrentSaveData.MineShafts.Find(s => s.Index == shaft.ShaftIndex);
                if (savedData != null && savedData.MinionHealth > 0)
                {
                    Debug.Log($"[Offline] Shaft {shaft.ShaftIndex} is blocked by minion.");
                    continue; 
                }

                double shaftIncome = shaft.GetTotalExtractionPerSecond(shaft.Level, true);
                totalShaftIncome += shaftIncome;
                Debug.Log($"[Offline] Shaft {shaft.ShaftIndex} adds {shaftIncome} to totalShaftIncome.");
            }
            else
            {
                Debug.Log($"[Offline] Shaft {shaft.ShaftIndex} ignored (Broken: {shaft.isBroken}, Manager: {shaft.currentManager != null}).");
            }
        }

        double elevatorThroughput = 0;
        Elevator elevator = FindObjectOfType<Elevator>();
        if (elevator != null && elevator.currentManager != null)
        {
             float avgRoundTripTime = elevator.loadTime + elevator.unloadTime + 5f; 
             elevatorThroughput = elevator.GetCapacity(elevator.Level) / avgRoundTripTime;
             Debug.Log($"[Offline] Elevator Throughput: {elevatorThroughput} (Capacity: {elevator.GetCapacity(elevator.Level)}, Time: {avgRoundTripTime})");
        }
        else
        {
             Debug.Log($"[Offline] Elevator ignored (Exists: {elevator != null}, Manager: {elevator?.currentManager != null}).");
        }

        double warehouseThroughput = 0;
        Warehouse warehouse = FindObjectOfType<Warehouse>();
        if (warehouse != null && warehouse.currentManager != null)
        {
            warehouseThroughput = warehouse.GetTotalThroughputDisplay(warehouse.Level);
            Debug.Log($"[Offline] Warehouse Throughput: {warehouseThroughput}");
        }
        else
        {
            Debug.Log($"[Offline] Warehouse ignored (Exists: {warehouse != null}, Manager: {warehouse?.currentManager != null}).");
        }

        if (elevator == null || warehouse == null) 
        {
            Debug.Log("[Offline] Elevator or Warehouse is completely NULL in scene. Returning 0.");
            return 0;
        }

        double bottleneck = Math.Min(totalShaftIncome, Math.Min(elevatorThroughput, warehouseThroughput));
        Debug.Log($"[Offline] Final Bottleneck: Min({totalShaftIncome}, {elevatorThroughput}, {warehouseThroughput}) = {bottleneck}");
        return bottleneck;
    }
}
