using System;
using UnityEngine;

[Serializable]
public enum ManagerRarity
{
    Junior,   // Trẻ tuổi
    Director, // Giám đốc
    Senior    // Cấp cao
}

[Serializable]
public enum ManagerBuffType
{
    MiningSpeed, // Tốc độ khai thác
    MoveSpeed,   // Tốc độ di chuyển
    ReduceCost   // Giảm chi phí nâng cấp
}

[Serializable]
public enum SeniorSpecialFeature
{
    None,           // Không có tính năng đặc biệt
    SpecialFeature  // Có tính năng đặc biệt riêng biệt tùy theo Facility (Ví dụ: Oxi ở Hầm, Bảo trì ở Thang máy, v.v.)
}

[Serializable]
public class ManagerData
{
    public string Id;
    public string Name;
    public string CharacterID; // Dùng để tra cứu hình ảnh/animation trong SO
    public ManagerRarity Rarity;
    public ManagerBuffType BuffType;
    public FacilityType AssignedFacilityType;
    
    [Tooltip("Dành cho quản lý cấp cao (Senior): Có tính năng đặc biệt hay không?")]
    public SeniorSpecialFeature SpecialFeature;
    
    [Tooltip("Chỉ số phần trăm được cộng. Ví dụ: 5 nghĩa là +5%")]
    public float BuffValue; 

    [Tooltip("Thời gian Buff hoạt động (tính bằng giây)")]
    public float BuffDuration;

    [Tooltip("Thời gian chờ trước khi kích hoạt lại (tính bằng giây)")]
    public float CooldownDuration;

    public double OriginalHirePrice;

    public bool IsAssigned;
    public int AssignedShaftId = -1; // -1 nghĩa là chưa gán cho hầm nào

    public ManagerData()
    {
        Id = Guid.NewGuid().ToString();
    }
}
