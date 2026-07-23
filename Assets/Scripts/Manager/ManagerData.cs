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
public class ManagerData
{
    public string Id;
    public string Name;
    public string CharacterID; // Dùng để tra cứu hình ảnh/animation trong SO
    public ManagerRarity Rarity;
    public ManagerBuffType BuffType;
    
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
