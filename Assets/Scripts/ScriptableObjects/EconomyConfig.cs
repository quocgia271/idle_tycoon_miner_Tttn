using UnityEngine;

[CreateAssetMenu(fileName = "EconomyConfig", menuName = "Configs/Economy Config", order = 1)]
public class EconomyConfig : ScriptableObject
{
    [Header("Balancing - Game Economy")]
    [Tooltip("Số giây thu nhập dùng làm giá Hồi sinh")]
    public float ReviveIncomeSeconds = 3f;

    [Tooltip("Hệ số chi phí 1 Tinh thần (tương đương 100 max morale = 3 giây)")]
    public float MoraleCostMultiplier = 0.03f;

    [Tooltip("Hệ số chi phí 1 Sức bền (tương đương 100 max health = 8 giây)")]
    public float EnduranceCostMultiplier = 0.08f;

    [Tooltip("Số giây tổng thu nhập dùng làm giá Sửa Hầm (khi bị Boss đánh sập)")]
    public float RepairCostDurationSec = 30f;

    [Tooltip("Giá sàn tối thiểu cho các dịch vụ")]
    public double MinimumServiceCost = 100.0;
}
