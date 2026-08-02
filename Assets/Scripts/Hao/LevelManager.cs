using System;
using UnityEngine;

/// <summary>
/// LevelManager quan ly Level (1 - 15) nguoi choi trong cac Man
/// Suu dung cong thuc chuan tu MathHelper
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level Settings")]
    public int CurrentLevel = 1;
    public const int MAX_LEVEL = 15;

    [Header("Time Gate Settings")]
    private float timeGateRemaining = 0f;
    private bool isTimeGateActive = false;

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
        }
    }

    private void Start()
    {
        if (Gamemanager.Instance != null)
        {
            CurrentLevel = Gamemanager.Instance.PlayerLevel;
        }
    }

    private void Update()
    {
        if (isTimeGateActive && timeGateRemaining > 0f)
        {
            timeGateRemaining -= Time.deltaTime;
            if (timeGateRemaining <= 0f)
            {
                timeGateRemaining = 0f;
                isTimeGateActive = false;
                Debug.Log($"<color=cyan>[LevelManager] Time Gate cho Level {CurrentLevel} da hoan thanh!</color>");
            }
        }
    }

    /// <summary>
    /// Tinh chi phi Vang de len Level hien tai (Dung MathHelper.CalculateLevelUpCost)
    /// </summary>
    public double GetLevelUpCost(int level)
    {
        return MathHelper.CalculateLevelUpCost(level);
    }

    public double GetCurrentLevelUpCost()
    {
        return GetLevelUpCost(CurrentLevel);
    }

    /// <summary>
    /// Kiem tra dieu kien de Len Level (Vang & Time Gate)
    /// </summary>
    public bool CanLevelUp()
    {
        if (CurrentLevel >= MAX_LEVEL) return false;

        // 1. Dieu kien Vang
        bool goldOk = Gamemanager.Instance != null && Gamemanager.Instance.IdleCash >= GetCurrentLevelUpCost();

        // 2. Dieu kien Time Gate
        bool timeGateOk = !isTimeGateActive || timeGateRemaining <= 0f;

        return goldOk && timeGateOk;
    }

    /// <summary>
    /// Thuc hien Len Level va tru Vang
    /// </summary>
    public bool TryLevelUp()
    {
        if (!CanLevelUp())
        {
            Debug.LogWarning("[LevelManager] Chua du dieu kien de Len Level!");
            return false;
        }

        double cost = GetCurrentLevelUpCost();
        if (Gamemanager.Instance != null && Gamemanager.Instance.DeductCash(cost))
        {
            CurrentLevel++;
            if (Gamemanager.Instance != null)
            {
                Gamemanager.Instance.PlayerLevel = CurrentLevel;
                Gamemanager.Instance.OnLevelChanged?.Invoke(CurrentLevel);
            }

            Debug.Log($"<color=green>[LevelManager] Len Level thanh cong! Level hien tai: {CurrentLevel}</color>");
            
            GameEvents.OnLevelUp?.Invoke(CurrentLevel);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Kich hoat Time Gate cho Level dung cong thuc MathHelper
    /// </summary>
    public void StartTimeGateForCurrentLevel()
    {
        float duration = MathHelper.CalculateLevelTimeGateDuration(CurrentLevel);
        timeGateRemaining = duration;
        isTimeGateActive = true;
        Debug.Log($"[LevelManager] Bat dau Time Gate cho Level {CurrentLevel}: {duration}s");
    }

    /// <summary>
    /// Rollback Level ve Level dau tien cua Stage hien tai (Khi bi phat Checkpoint)
    /// </summary>
    public void RollbackToStageStart(int stage)
    {
        int startLevel = (stage - 1) * 5 + 1;
        CurrentLevel = Mathf.Clamp(startLevel, 1, MAX_LEVEL);

        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.PlayerLevel = CurrentLevel;
            Gamemanager.Instance.OnLevelChanged?.Invoke(CurrentLevel);
        }

        // Bật lại các ổ khóa ShaftUnlocker thuộc Màn hiện tại để Reset tiến trình hầm mỏ
        ResetMinesForStage(stage);

        Debug.Log($"<color=red>[LevelManager] Bi ROLLBACK ve Level dau Man {stage}: Level {CurrentLevel}! Reset tat ca ham mo da xay.</color>");
        GameEvents.OnLevelUp?.Invoke(CurrentLevel);
    }

    /// <summary>
    /// Khóa lại các hầm mỏ đã mở trong Màn hiện tại
    /// </summary>
    public void ResetMinesForStage(int stage)
    {
        // Chỉ tìm các GameObject thực sự nằm trong Scene (tránh biến dạng Prefab Asset làm hỏng UI)
        ShaftUnlocker[] unlockers = FindObjectsOfType<ShaftUnlocker>(true);
        foreach (var unlocker in unlockers)
        {
            if (unlocker == null || unlocker.gameObject == null) continue;

            int minLevel = (stage - 1) * 5 + 1;
            int maxLevel = stage * 5;

            // Nếu ô ổ khóa nằm trong tầm Level của Stage này -> Reset sạch trạng thái cho phép mua lại
            if (unlocker.requiredLevel >= minLevel && unlocker.requiredLevel <= maxLevel)
            {
                unlocker.ResetUnlockerState();
            }
        }

        // Reset level các MineShaft trong Scene về level 1
        MineShaft[] shafts = FindObjectsOfType<MineShaft>(true);
        foreach (var shaft in shafts)
        {
            if (shaft != null) shaft.Level = 1;
        }

        // Quét lại tiến độ nhiệm vụ
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.ScanExistingSceneMines();
        }
    }
}
