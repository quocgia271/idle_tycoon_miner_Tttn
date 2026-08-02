using System;

/// <summary>
/// Kenh giao tiep su kien chung giua cac he thong (Hao & Nhan)
/// </summary>
public static class GameEvents
{
    // --- SU KIEN TIEN TRINH (DAO MAN / MAN) ---
    /// <summary>
    /// Ban ra khi nguoi choi len Level (Level 1 -> 15)
    /// </summary>
    public static Action<int> OnLevelUp;

    /// <summary>
    /// Ban ra khi nguoi choi vuot StageGate thanh cong (Stage 1 -> 3)
    /// </summary>
    public static Action<int> OnStagePassed;

    // --- SU KIEN TAI NGUYEN & MAM MONG ---
    /// <summary>
    /// Ban ra khi so tien IdleCash thay doi
    /// </summary>
    public static Action<double> OnMoneyChanged;

    /// <summary>
    /// Ban ra khi mua/xay moi 1 ham mo
    /// </summary>
    public static Action<int> OnShaftPurchased;

    /// <summary>
    /// Ban ra khi nang cap 1 ham mo
    /// </summary>
    public static Action<int> OnShaftUpgraded;

    // --- SU KIEN CHECKPOINT & HAZARDS ---
    /// <summary>
    /// Ban ra khi that bai Checkpoint o cuoi Man (Truyen vao so lan thua lien tiep: 1, 2, 3)
    /// </summary>
    public static Action<int> OnCheckpointFailed;

    /// <summary>
    /// Ban ra khi 1 ham mo bi sap hoan toan do khong bao tri (Nhan xu ly)
    /// </summary>
    public static Action<int> OnMineCollapsed;

    // --- SU KIEN NHIEM VU (MISSION) ---
    /// <summary>
    /// Ban ra khi tien do mission thay doi (MissionId, CurrentProgress, TargetAmount)
    /// </summary>
    public static Action<string, int, int> OnMissionProgressUpdated;

    /// <summary>
    /// Ban ra khi 1 mission hoan thanh
    /// </summary>
    public static Action<string> OnMissionCompleted;
}
