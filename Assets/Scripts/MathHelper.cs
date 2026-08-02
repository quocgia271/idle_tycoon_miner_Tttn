using System;
using UnityEngine;

/// <summary>
/// MathHelper chua toan bo Cong thuc tinh toan Chuan hoa theo GDD v2 & File newideas (IDLE TYCOON V2.docx)
/// </summary>
public static class MathHelper
{
    // ================================================================
    // 1. CÔNG THỨC CHI PHÍ NÂNG CẤP TỔNG QUÁT
    // Cost = BaseCost * (Multiplier ^ (Level - 1))
    // ================================================================
    public static double CalculateUpgradeCost(double baseCost, double costMultiplier, int level)
    {
        return baseCost * Math.Pow(costMultiplier, level - 1);
    }

    public static int CalculateMaxLevel(double currentCash, double baseCost, double costMultiplier, int currentLevel)
    {
        double costOfNextLevel = CalculateUpgradeCost(baseCost, costMultiplier, currentLevel);
        if (currentCash < costOfNextLevel) return 1;

        double r = costMultiplier;
        double a = costOfNextLevel;
        double innerLog = (currentCash * (r - 1) / a) + 1;
        double n = Math.Log(innerLog, r);
        int maxLevelsCanBuy = (int)Math.Floor(n);

        return maxLevelsCanBuy < 1 ? 1 : maxLevelsCanBuy;
    }

    // ================================================================
    // 2. CÔNG THỨC CHI PHÍ NÂNG LEVEL NGUỜI CHƠI (GDD Section 2)
    // BaseCost = 50, CostMultiplier = 1.15
    // ================================================================
    public static double CalculateLevelUpCost(int level)
    {
        return CalculateUpgradeCost(50.0, 1.15, level);
    }

    // ================================================================
    // 3. CÔNG THỨC CHI PHÍ MUA HẦM MỎ & LEVEL TỐI THIỂU (GDD Section 6)
    // MineCost(n) = 200 * 1.25 ^ (n - 1)   (n = 1..15)
    // MinLevelToBuyMine(n) = ceil(n / 3)
    // ================================================================
    public static double CalculateMineCost(int mineIndex)
    {
        return 200.0 * Math.Pow(1.25, Math.Max(1, mineIndex) - 1);
    }

    public static int GetMinLevelToBuyMine(int mineIndex)
    {
        return (int)Math.Ceiling(mineIndex / 3.0);
    }

    // ================================================================
    // 4. CÔNG THỨC VÀNG YÊU CẦU QUA MÀN - STAGE GATE COST (GDD Section 6)
    // StageGateCost(stage) = 500 * 3 ^ (stage - 1)
    // Màn 1 -> 2: 500 Vàng
    // Màn 2 -> 3: 1,500 Vàng
    // Màn 3 -> Uy Tín: 4,500 Vàng
    // ================================================================
    public static double CalculateStageGateCost(int stage)
    {
        return 500.0 * Math.Pow(3.0, Math.Max(1, stage) - 1);
    }

    // ================================================================
    // 5. CÔNG THỨC CHI PHÍ MUA MANAGER (GDD Section 6 & newideas)
    // ManagerCost(count) = 100 * 2.5 ^ count
    // Giá tăng theo cấp số nhân mỗi lần mua
    // ================================================================
    public static double CalculateManagerCost(int purchasedCount)
    {
        return 100.0 * Math.Pow(2.5, Math.Max(0, purchasedCount));
    }

    // ================================================================
    // 6. CÔNG THỨC SẢN LƯỢNG & TỐC ĐỘ KHAI THÁC HẦM MỎ (GDD Section 4.2 & 7.1)
    // Stage 1 (Than): x1.0
    // Stage 2 (Bạc): x1.5
    // Stage 3 (Vàng): x2.0
    // MiningSpeed = BaseSpeed (10) * 1.2 ^ (level - 1) * StageBonus
    // ================================================================
    public static double GetStageGoldMultiplier(int stagesCompleted)
    {
        return 1.0 + (0.5 * Math.Max(0, stagesCompleted));
    }

    public static double CalculateShaftMiningProductivity(double baseRate, int level, int stagesCompleted)
    {
        double levelBonus = baseRate * Math.Pow(1.2, level - 1);
        double stageBonus = GetStageGoldMultiplier(stagesCompleted);
        return levelBonus * stageBonus;
    }

    // ================================================================
    // 7. CÔNG THỨC THỜI GIAN (TIME GATE & XÂY HẦM) (GDD Section 2 & 6)
    // Thời gian xây hầm: BuildTime(n) = 5 * 1.2 ^ (n - 1) giây
    // Thời gian cho Time Gate: TimeGate(level) = 5 * level giây
    // ================================================================
    public static float CalculateMineBuildTime(int mineIndex)
    {
        return (float)(5.0 * Math.Pow(1.2, Math.Max(1, mineIndex) - 1));
    }

    public static float CalculateLevelTimeGateDuration(int level)
    {
        return (float)(5.0 * level);
    }
}
