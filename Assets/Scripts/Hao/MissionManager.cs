using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MissionType
{
    BuildMine,      // Xay / Mua ham mo moi
    UpgradeMine,    // Nang cap ham mo
    EarnCash,       // Tich luy Vang
    RestoreMorale,  // Hoi phuc tinh than worker (Man 2)
    DefeatBoss      // Tieu diet Boss & Rong (Man 3)
}

[System.Serializable]
public class MissionData
{
    public string id;
    public string title;
    public MissionType type;
    public int targetAmount;
    public int currentAmount;
    public int requiredStage; // Nhiem vu thuoc MAN (STAGE) nao: 1, 2, hoac 3
    public bool isCompleted;

    public MissionData(string id, string title, MissionType type, int targetAmount, int requiredStage)
    {
        this.id = id;
        this.title = title;
        this.type = type;
        this.targetAmount = targetAmount;
        this.currentAmount = 0;
        this.requiredStage = requiredStage;
        this.isCompleted = false;
    }
}

/// <summary>
/// MissionManager quan ly Nhiem vu CHỈ THEO 3 MÀN (Stage 1, Stage 2, Stage 3)
/// Theo dung file newideas (IDLE TYCOON V2.docx)
/// </summary>
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Stage Missions List")]
    public List<MissionData> stageMissions = new List<MissionData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitStageMissions();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        RegisterEventHandlers();
        ScanExistingSceneMines();
    }

    private void OnDestroy()
    {
        UnregisterEventHandlers();
    }

    private void RegisterEventHandlers()
    {
        GameEvents.OnShaftPurchased += HandleShaftPurchased;
        GameEvents.OnShaftUpgraded += HandleShaftUpgraded;
        GameEvents.OnMoneyChanged += HandleMoneyChanged;
    }

    private void UnregisterEventHandlers()
    {
        GameEvents.OnShaftPurchased -= HandleShaftPurchased;
        GameEvents.OnShaftUpgraded -= HandleShaftUpgraded;
        GameEvents.OnMoneyChanged -= HandleMoneyChanged;
    }

    /// <summary>
    /// Khoi tao Nhiem vu ngau nhien theo DUNG 3 MAN
    /// </summary>
    private void InitStageMissions()
    {
        stageMissions.Clear();
        GenerateRandomMissionsForStage(1);
        GenerateRandomMissionsForStage(2);
        GenerateRandomMissionsForStage(3);
    }

    /// <summary>
    /// Sinh ngẫu nhiên bộ 3 nhiệm vụ PHÂN LOẠI RÕ RÀNG (1 Xây Dựng + 1 Nâng Cấp + 1 Tích Vàng/Hành Động)
    /// </summary>
    public void GenerateRandomMissionsForStage(int stage)
    {
        stageMissions.RemoveAll(m => m.requiredStage == stage);
        List<MissionData> pool = new List<MissionData>();

        int rnd = UnityEngine.Random.Range(10, 99);

        if (stage == 1)
        {
            // Nhóm 1: XÂY DỰNG
            List<MissionData> buildPool = new List<MissionData>()
            {
                new MissionData($"M1_B1_{rnd}", "Xây dựng 3 Hầm mỏ", MissionType.BuildMine, 3, 1),
                new MissionData($"M1_B2_{rnd}", "Xây dựng 4 Hầm mỏ", MissionType.BuildMine, 4, 1)
            };

            // Nhóm 2: NÂNG CẤP
            List<MissionData> upgradePool = new List<MissionData>()
            {
                new MissionData($"M1_U1_{rnd}", "Nâng cấp Hầm mỏ lên Cấp 5", MissionType.UpgradeMine, 5, 1),
                new MissionData($"M1_U2_{rnd}", "Nâng cấp 2 Hầm mỏ lên Cấp 3", MissionType.UpgradeMine, 3, 1)
            };

            // Nhóm 3: TÍCH VÀNG
            List<MissionData> goldPool = new List<MissionData>()
            {
                new MissionData($"M1_G1_{rnd}", "Tích lũy 500 Vàng", MissionType.EarnCash, 500, 1),
                new MissionData($"M1_G2_{rnd}", "Tích lũy 800 Vàng", MissionType.EarnCash, 800, 1)
            };

            pool.Add(buildPool[UnityEngine.Random.Range(0, buildPool.Count)]);
            pool.Add(upgradePool[UnityEngine.Random.Range(0, upgradePool.Count)]);
            pool.Add(goldPool[UnityEngine.Random.Range(0, goldPool.Count)]);
        }
        else if (stage == 2)
        {
            // Nhóm 1: XÂY DỰNG (Màn 2)
            List<MissionData> buildPool = new List<MissionData>()
            {
                new MissionData($"M2_B1_{rnd}", "Xây dựng 7 Hầm mỏ", MissionType.BuildMine, 7, 2),
                new MissionData($"M2_B2_{rnd}", "Xây dựng 8 Hầm mỏ", MissionType.BuildMine, 8, 2)
            };

            // Nhóm 2: NÂNG CẤP (Màn 2)
            List<MissionData> upgradePool = new List<MissionData>()
            {
                new MissionData($"M2_U1_{rnd}", "Nâng cấp 2 Hầm mỏ lên Cấp 7", MissionType.UpgradeMine, 7, 2),
                new MissionData($"M2_U2_{rnd}", "Nâng cấp Hầm mỏ lên Cấp 10", MissionType.UpgradeMine, 10, 2)
            };

            // Nhóm 3: TÍCH VÀNG / KHÔI PHỤC TINH THẦN (Màn 2)
            List<MissionData> actionPool = new List<MissionData>()
            {
                new MissionData($"M2_A1_{rnd}", "Khôi phục tinh thần cho 3 Công nhân", MissionType.RestoreMorale, 3, 2),
                new MissionData($"M2_A2_{rnd}", "Tích lũy 5,000 Vàng", MissionType.EarnCash, 5000, 2),
                new MissionData($"M2_A3_{rnd}", "Tích lũy 8,000 Vàng", MissionType.EarnCash, 8000, 2)
            };

            pool.Add(buildPool[UnityEngine.Random.Range(0, buildPool.Count)]);
            pool.Add(upgradePool[UnityEngine.Random.Range(0, upgradePool.Count)]);
            pool.Add(actionPool[UnityEngine.Random.Range(0, actionPool.Count)]);
        }
        else if (stage == 3)
        {
            // Nhóm 1: XÂY DỰNG (Màn 3)
            List<MissionData> buildPool = new List<MissionData>()
            {
                new MissionData($"M3_B1_{rnd}", "Xây dựng 12 Hầm mỏ", MissionType.BuildMine, 12, 3),
                new MissionData($"M3_B2_{rnd}", "Xây dựng 15 Hầm mỏ", MissionType.BuildMine, 15, 3)
            };

            // Nhóm 2: NÂNG CẤP (Màn 3)
            List<MissionData> upgradePool = new List<MissionData>()
            {
                new MissionData($"M3_U1_{rnd}", "Nâng cấp 3 Hầm mỏ lên Cấp 12", MissionType.UpgradeMine, 12, 3),
                new MissionData($"M3_U2_{rnd}", "Nâng cấp Hầm mỏ lên Cấp 15", MissionType.UpgradeMine, 15, 3)
            };

            // Nhóm 3: DIỆT BOSS / TÍCH VÀNG CAO CẤP (Màn 3)
            List<MissionData> actionPool = new List<MissionData>()
            {
                new MissionData($"M3_A1_{rnd}", "Tiêu diệt Boss Phase 3 và Rồng", MissionType.DefeatBoss, 1, 3),
                new MissionData($"M3_A2_{rnd}", "Tích lũy 50,000 Vàng", MissionType.EarnCash, 50000, 3),
                new MissionData($"M3_A3_{rnd}", "Tích lũy 80,000 Vàng", MissionType.EarnCash, 80000, 3)
            };

            pool.Add(buildPool[UnityEngine.Random.Range(0, buildPool.Count)]);
            pool.Add(upgradePool[UnityEngine.Random.Range(0, upgradePool.Count)]);
            pool.Add(actionPool[UnityEngine.Random.Range(0, actionPool.Count)]);
        }

        stageMissions.AddRange(pool);
        ScanExistingSceneMines();
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            T temp = list[i];
            int randomIndex = UnityEngine.Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    /// <summary>
    /// Quet CHỈ NHỮNG HẦM ĐÃ MỞ KHÓA THỰC TẾ trong Scene khi game bat dau
    /// </summary>
    public void ScanExistingSceneMines()
    {
        MineShaft[] existingShafts = FindObjectsOfType<MineShaft>();
        int unlockedCount = 0;
        int maxLevel = 0;

        foreach (var shaft in existingShafts)
        {
            // Kiểm tra xem hầm này đã mở khóa chưa (không bị khóa bởi ShaftUnlocker đang active)
            ShaftUnlocker unlocker = shaft.GetComponentInChildren<ShaftUnlocker>(true);
            bool isUnlocked = unlocker == null || !unlocker.gameObject.activeSelf;

            if (isUnlocked && shaft.gameObject.activeInHierarchy)
            {
                unlockedCount++;
                if (shaft.Level > maxLevel) maxLevel = shaft.Level;
            }
        }

        SetProgressDirect(MissionType.BuildMine, unlockedCount);
        SetProgressDirect(MissionType.UpgradeMine, maxLevel);

        if (Gamemanager.Instance != null)
        {
            SetProgressDirect(MissionType.EarnCash, (int)Gamemanager.Instance.IdleCash);
        }
    }

    /// <summary>
    /// Cap nhat gia tri tien do truc tiep
    /// </summary>
    public void SetProgressDirect(MissionType type, int targetValue)
    {
        int currentStage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 1;

        foreach (var mission in stageMissions)
        {
            if (mission.requiredStage == currentStage && mission.type == type && !mission.isCompleted)
            {
                mission.currentAmount = targetValue;

                if (mission.currentAmount >= mission.targetAmount)
                {
                    mission.currentAmount = mission.targetAmount;
                    mission.isCompleted = true;
                    Debug.Log($"<color=green>[MissionManager] DA HOAN THANH NHIEM VU MAN {currentStage}: {mission.title}</color>");
                    GameEvents.OnMissionCompleted?.Invoke(mission.id);
                }

                GameEvents.OnMissionProgressUpdated?.Invoke(mission.id, mission.currentAmount, mission.targetAmount);
            }
        }
    }

    /// <summary>
    /// Cong dồn tien do nhiem vu
    /// </summary>
    public void ReportProgress(MissionType type, int amount = 1)
    {
        int currentStage = StageManager.Instance != null ? StageManager.Instance.CurrentStage : 1;

        foreach (var mission in stageMissions)
        {
            if (mission.requiredStage == currentStage && mission.type == type && !mission.isCompleted)
            {
                mission.currentAmount += amount;
                if (mission.currentAmount >= mission.targetAmount)
                {
                    mission.currentAmount = mission.targetAmount;
                    mission.isCompleted = true;
                    Debug.Log($"<color=green>[MissionManager] DA HOAN THANH NHIEM VU MAN {currentStage}: {mission.title}</color>");
                    GameEvents.OnMissionCompleted?.Invoke(mission.id);
                }

                GameEvents.OnMissionProgressUpdated?.Invoke(mission.id, mission.currentAmount, mission.targetAmount);
            }
        }
    }

    private void HandleShaftPurchased(int shaftCount)
    {
        ScanExistingSceneMines();
    }

    private void HandleShaftUpgraded(int newLevel)
    {
        ScanExistingSceneMines();
    }

    private void HandleMoneyChanged(double currentMoney)
    {
        SetProgressDirect(MissionType.EarnCash, (int)currentMoney);
    }

    public bool AreMissionsCompletedForStage(int stage)
    {
        foreach (var mission in stageMissions)
        {
            if (mission.requiredStage == stage && !mission.isCompleted)
            {
                return false;
            }
        }
        return true;
    }

    public List<MissionData> GetMissionsForStage(int stage)
    {
        return stageMissions.FindAll(m => m.requiredStage == stage);
    }
}
