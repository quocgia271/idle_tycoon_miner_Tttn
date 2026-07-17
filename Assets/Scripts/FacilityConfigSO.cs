using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Facility Config", menuName = "Tycoon/Facility Config")]
public class FacilityConfigSO : ScriptableObject
{
    [Header("General Info")]
    public string FacilityName;
    public Sprite FacilityIcon;
    
    [Header("Stats Definition (List)")]
    // Dùng List để bạn có thể thêm bớt bao nhiêu chỉ số tùy thích
    public List<string> StatNames = new List<string>();

    [Header("Base Stats Logic (Chỉnh ở đây, game tự update)")]
    public double BaseCost = 50;
    public double CostMultiplier = 1.15; // Mỗi level giá tăng 15%
    public float BaseCapacity = 100f; // Sức chứa cơ bản ở Level 1
    public float BaseSpeed = 2f;      // Tốc độ cơ bản ở Level 1
}
