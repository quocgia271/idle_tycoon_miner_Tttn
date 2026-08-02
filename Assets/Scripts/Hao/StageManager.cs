using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// StageManager quan ly Qua Màn, Thoi gian dem nguoc & Checkpoint Phat (Màn 1, Màn 2, Màn 3)
/// </summary>
public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Stage Settings")]
    public int CurrentStage = 1;
    public const int MAX_STAGE = 3;
    public int MinesBuiltThisStage = 0;

    [Header("Countdown Timer Settings")]
    public float stageTimeLimitSeconds = 180f; // 3 phut cho moi Man Checkpoint
    public float stageTimeRemaining = 180f;
    private bool isTimerRunning = true;

    [Header("Checkpoint Fail Tracker")]
    private Dictionary<int, int> failCountByStage = new Dictionary<int, int>();

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

    /// <summary>
    /// Lấy thời gian đếm ngược chuẩn cho từng Round theo thiết kế:
    /// Round 1: 3 phút (180s)
    /// Round 2: 5 phút (300s)
    /// Round 3: 7.5 phút (450s)
    /// </summary>
    public float GetStageTimeLimitSeconds(int stage)
    {
        switch (stage)
        {
            case 1: return 180f; // 3 phút
            case 2: return 300f; // 5 phút
            case 3: return 450f; // 7.5 phút
            default: return 180f;
        }
    }

    private void Start()
    {
        stageTimeLimitSeconds = GetStageTimeLimitSeconds(CurrentStage);
        stageTimeRemaining = stageTimeLimitSeconds;
        GameEvents.OnShaftPurchased += HandleShaftPurchased;
    }

    private void OnDestroy()
    {
        GameEvents.OnShaftPurchased -= HandleShaftPurchased;
    }

    private void Update()
    {
#if UNITY_EDITOR
        // BẤM PHÍM 'T' TRONG PLAY MODE ĐỂ TEST HẾT GIỜ (TIMEOUT) TỨC THÌ:
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log("<color=yellow>[StageManager] PHÍM 'T' ĐƯỢC BẤM ➔ ÉP THỜI GIAN ĐẾM NGƯỢC CÒN 1S ĐỂ TEST HẾT GIỜ TỨC THÌ!</color>");
            ForceTimeoutForTesting();
        }
#endif

        if (isTimerRunning && stageTimeRemaining > 0f)
        {
            stageTimeRemaining -= Time.deltaTime;
            if (stageTimeRemaining <= 0f)
            {
                stageTimeRemaining = 0f;
                Debug.LogWarning("[StageManager] HẾT THỜI GIAN CHECKPOINT MAN! KÍCH HOẠT PHẠT!");
                OnCheckpointFail();
                stageTimeRemaining = stageTimeLimitSeconds; // Reset dem lai cho luot tiep theo
            }
        }
    }

    /// <summary>
    /// Hàm hỗ trợ Test hết giờ đếm ngược ngay lập tức (Nhấp chuột phải vào StageManager trong Inspector)
    /// </summary>
    [ContextMenu("Test Force Timeout Now")]
    public void ForceTimeoutForTesting()
    {
        stageTimeRemaining = 1f;
        Debug.Log("<color=yellow>[StageManager] Đã ép thời gian đếm ngược còn 1s để test Hết Giờ!</color>");
    }

    private void HandleShaftPurchased(int count)
    {
        MinesBuiltThisStage += count;
    }

    /// <summary>
    /// Lay thoi gian dem nguoc dinh dang MM:SS (vd: 02:45)
    /// </summary>
    public string GetFormattedTimeRemaining()
    {
        int minutes = Mathf.FloorToInt(stageTimeRemaining / 60f);
        int seconds = Mathf.FloorToInt(stageTimeRemaining % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    /// <summary>
    /// Tinh chi phi StageGateCost de sang Màn moi qua MathHelper
    /// </summary>
    public double GetStageGateCost()
    {
        return MathHelper.CalculateStageGateCost(CurrentStage);
    }

    /// <summary>
    /// Kiem tra Dieu kien QUA MÀN:
    /// 1. HOÀN THÀNH HẾT NHIỆM VỤ CỦA MÀN ĐÓ (MissionManager.AreMissionsCompletedForStage)
    /// 2. ĐỦ VÀNG ĐỂ TRẢ StageGateCost
    /// </summary>
    public bool CanPassStage()
    {
        if (CurrentStage > MAX_STAGE) return false;

        // 1. Hoan thanh HET Nhiem vu cua Màn
        bool missionsOk = MissionManager.Instance == null || MissionManager.Instance.AreMissionsCompletedForStage(CurrentStage);
        
        // 2. Du Vang de tra StageGateCost
        bool goldOk = Gamemanager.Instance != null && Gamemanager.Instance.IdleCash >= GetStageGateCost();

        return missionsOk && goldOk;
    }

    /// <summary>
    /// Thuc hien QUA MÀN
    /// </summary>
    public bool TryPassStage()
    {
        if (!CanPassStage())
        {
            Debug.LogWarning("[StageManager] Chua du dieu kien (Hoan thanh Nhiem vu + Du Vang) de qua Màn!");
            return false;
        }

        double gateCost = GetStageGateCost();
        if (Gamemanager.Instance != null && Gamemanager.Instance.DeductCash(gateCost))
        {
            CurrentStage++;
            MinesBuiltThisStage = 0;
            
            // Reset số lần thất bại cho Màn mới
            if (failCountByStage.ContainsKey(CurrentStage))
            {
                failCountByStage[CurrentStage] = 0;
            }

            // Cập nhật thời gian giới hạn đếm ngược chuẩn cho Round mới
            stageTimeLimitSeconds = GetStageTimeLimitSeconds(CurrentStage);
            stageTimeRemaining = stageTimeLimitSeconds;

            Debug.Log($"<color=magenta>[StageManager] CHÚC MỪNG! BẠN ĐÃ QUA MÀN {CurrentStage - 1} -> SANG MÀN {CurrentStage} ({stageTimeLimitSeconds / 60f} phút)</color>");
            
            GameEvents.OnStagePassed?.Invoke(CurrentStage);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Cơ chế Phạt Checkpoint 2 Lần:
    /// - Lần 1: Reset đếm ngược & reset tiến độ round nhưng GIỮ NGUYÊN SỐ VÀNG.
    /// - Lần 2: Reset lại từ đầu Round đó (Rollback toàn bộ).
    /// </summary>
    public void OnCheckpointFail()
    {
        if (!failCountByStage.ContainsKey(CurrentStage))
        {
            failCountByStage[CurrentStage] = 0;
        }

        failCountByStage[CurrentStage]++;
        int failCount = failCountByStage[CurrentStage];

        Debug.LogWarning($"<color=orange>[StageManager] THẤT BẠI CHECKPOINT MÀN {CurrentStage}! Lần thất bại: {failCount}/2</color>");

        if (failCount == 1)
        {
            // PHẠT LẦN 1: Reset tiến trình hầm mỏ đã xây trong round + reset đếm ngược & sinh nhiệm vụ mới, GIỮ NGUYÊN SỐ VÀNG
            stageTimeRemaining = stageTimeLimitSeconds;
            MinesBuiltThisStage = 0;

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RollbackToStageStart(CurrentStage); // Reset tiến trình hầm mỏ đã xây của round này
            }

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.GenerateRandomMissionsForStage(CurrentStage);
            }

            Debug.Log($"<color=yellow>[StageManager] PHẠT LẦN 1: HẾT GIỜ! Reset hầm mỏ đã xây & sinh nhiệm vụ mới! GIỮ NGUYÊN VÀNG.</color>");
        }
        else if (failCount >= 2)
        {
            // PHẠT LẦN 2: Reset toàn bộ từ đầu Round đó + trừ phạt Vàng
            stageTimeRemaining = stageTimeLimitSeconds;
            MinesBuiltThisStage = 0;

            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.RollbackToStageStart(CurrentStage);
            }

            if (Gamemanager.Instance != null)
            {
                double deductAmount = Gamemanager.Instance.IdleCash * 0.30;
                Gamemanager.Instance.DeductCash(deductAmount); // Trừ 30% Vàng ở lần 2
            }

            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.GenerateRandomMissionsForStage(CurrentStage);
            }

            failCountByStage[CurrentStage] = 0; // Reset số lần phạt về 0
            Debug.Log($"<color=red>[StageManager] PHẠT LẦN 2: THẤT BẠI TOÀN BỘ ROUND! Reset hầm mỏ + Trừ 30% Vàng.</color>");
        }

        GameEvents.OnCheckpointFailed?.Invoke(failCount);
    }

    private void DestroyRandomMineInCurrentStage()
    {
        Debug.Log("<color=red>[StageManager] Pha huy 1 ham mo ngau nhien do phat lan 2!</color>");
        if (MinesBuiltThisStage > 0)
        {
            MinesBuiltThisStage--;
        }
        GameEvents.OnMineCollapsed?.Invoke(-1);
    }
}
