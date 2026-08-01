using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public double IdleCash;
    public double LifetimeCash;
    public int PlayerLevel;
    public double PrestigeMultiplier = 1.0;
    public int CurrentRound = 1;
    
    // Lưu trữ thời điểm tắt game (Unix Timestamp dạng chuỗi hoặc số giây từ Epoch)
    // Để cho an toàn khi parse, ta dùng long (ticks hoặc giây). Ở đây dùng số giây tính từ Epoch.
    public long LastSaveTimeUnixSeconds; 
    
    // Dữ liệu tĩnh và động của các cơ sở
    public List<FacilitySaveData> MineShafts = new List<FacilitySaveData>();
    public FacilitySaveData Elevator = new FacilitySaveData();
    public FacilitySaveData Warehouse = new FacilitySaveData();
    
    // Quản lý
    public List<ManagerSaveData> OwnedManagers = new List<ManagerSaveData>();
    
    // Boss & Minion (Toàn cục)
    public BossSaveData BossData = new BossSaveData();
    
    // Lưu trạng thái vách ngăn ở Round 3 và Boss
    public bool IsRound3BarrierBroken = false;
    public List<BossHealthSaveData> BossHealths = new List<BossHealthSaveData>();
}

[Serializable]
public class BossHealthSaveData
{
    public string BossName;
    public float CurrentHealth;
    public bool IsDead;
}

[Serializable]
public class MinerSaveData
{
    public float Morale;
    public int HealthState; // 0 = Normal, 1 = Injured, 2 = Dead
}

[System.Serializable]
public class FacilitySaveData
{
    public int Index; // Dùng cho MineShaft (ví dụ 1, 2, 3...)
    public int Level; // Cấp độ hiện tại của hầm/thang máy/nhà kho
    public string ActiveManagerID; // ID của người quản lý đang được gắn
    public bool IsUnlocked; // Trạng thái đã mở khóa hay chưa

    // --- Các thông số dành riêng cho MineShaft (Lưu trạng thái máu, lửa, tài nguyên) ---
    public double CurrentResource; 
    public float CurrentEndurance; 
    public bool IsBroken;
    
    // Debuff/Burn từ Boss 1, 3
    public float NormalBurnTimer;
    public float BigBurnTimer;
    public int FireClicksRemaining;
    
    // Độc hầm (Skill 3)
    public float Skill3Timer;
    public float Skill3DPS;
    public int Skill3ColorIndex;
    
    // Minion
    public float MinionHealth; // Nếu > 0 nghĩa là có minion đang chặn cửa
    
    // Lưu trạng thái của thợ mỏ (dành cho tính năng Event Freezing)
    public List<MinerSaveData> MinersData = new List<MinerSaveData>();
    
    // Manager Skill (chỉ dành cho hầm)
    // Skill Timer của Quản lý đã chuyển sang lưu toàn cục dạng Unix Timestamp thay vì số đếm lùi để dễ tính toán offline
}

[Serializable]
public class ManagerSaveData
{
    public string Id;
    public string Name;
    public string CharacterID;
    public int Rarity; // Ép kiểu int để lưu enum
    public int BuffType; // Ép kiểu int
    public int AssignedFacilityType; // Ép kiểu int
    public int SpecialFeature; // Ép kiểu int
    public float BuffValue;
    public float BuffDuration;
    public float CooldownDuration;
    public double OriginalHirePrice;
    
    public bool IsAssigned;
    public int AssignedShaftId = -1;
    
    // Quan trọng: Thay vì lưu Time.time, ta lưu mốc thời gian hoàn thành dưới dạng Unix Time (giây)
    public long SkillEndUnixTime; 
    public long CooldownEndUnixTime;
}

[Serializable]
public class BossSaveData
{
    // Boss 2: Tinh thần worker toàn khu mỏ
    public float CurrentGlobalSpirit;
    public bool IsSpiritBroken;
    
    // Boss 3: Kỹ năng Độc toàn bản đồ (Skill 4)
    public float Skill4Timer;
    
    // Boss 3: Màn chắn (Barrier)
    public float ElevatorBarrierTimer;
    public float WarehouseBarrierTimer;
}
