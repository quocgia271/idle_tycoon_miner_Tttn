using UnityEngine;

[CreateAssetMenu(fileName = "GlobalGameConfig", menuName = "Tycoon/Global Game Config")]
public class GlobalGameConfigSO : ScriptableObject
{
    [Header("Economy Settings")]
    [Tooltip("Tiền khởi nghiệp khi mới vào game hoặc sau khi chuyển sinh/qua màn")]
    public double InitialStartingCash = 150;

    [Header("Shaft Settings")]
    [Tooltip("Base cost to unlock the first shaft (or used as multiplier base)")]
    public double ShaftUnlockBaseCost = 50;
    
    [Tooltip("Multiplier for cost as shaft depth increases (e.g. 15)")]
    public float ShaftDepthMultiplier = 15f;
    
    [Tooltip("Multiplier for base income as shaft depth increases (e.g. 12)")]
    public float ShaftIncomeDepthMultiplier = 12f;
    
    [Tooltip("Multiplier for income per shaft level (e.g. 1.07)")]
    public float ShaftIncomeLevelMultiplier = 1.07f;

    [Header("Capacity Settings")]
    [Tooltip("Multiplier for capacity increase per level (e.g. 1.1 for 10%)")]
    public float CapacityLevelMultiplier = 1.1f;

    [Header("Round / Prestige Settings")]
    [Tooltip("Multiplier for difficulty/income per round (e.g. 1000000)")]
    public double RoundDifficultyMultiplier = 1000000;

    [Tooltip("Base cash requirement to prestige")]
    public double BasePrestigeRequirement = 1000000;

    [Tooltip("The shaft depth level where the barrier appears (e.g. 10 for shaft 11)")]
    public int BarrierShaftDepthIndex = 10;
}
