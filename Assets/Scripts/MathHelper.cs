using System;

public static class MathHelper
{
    #region CỐT LÕI (Core Upgrade & MAX Level)
    
    // Công thức tính chi phí nâng cấp: Cost = BaseCost * (Multiplier ^ Level)
    // Sau này Hào có thể chỉnh sửa công thức này thoải mái mà không sợ hỏng game
    public static double CalculateUpgradeCost(double baseCost, double costMultiplier, int level)
    {
        return baseCost * Math.Pow(costMultiplier, level - 1);
    }


    public static int CalculateMaxLevel(double currentCash, double baseCost, double costMultiplier, int currentLevel)
    {
        // Tính giá của cấp độ ngay tiếp theo
        double costOfNextLevel = CalculateUpgradeCost(baseCost, costMultiplier, currentLevel);
        
        // Nếu tiền hiện tại không đủ mua 1 cấp, ép trả về 1 để UI hiển thị giá tiền
        if (currentCash < costOfNextLevel) return 1;

        // Công thức tính tổng cấp số nhân: Sum = a * (r^n - 1) / (r - 1)
        // Suy ra số cấp (n) = Log_r ( (Sum * (r - 1) / a) + 1 )
        double r = costMultiplier;
        double a = costOfNextLevel;
        
        double innerLog = (currentCash * (r - 1) / a) + 1;
        double n = Math.Log(innerLog, r);
        
        int maxLevelsCanBuy = (int)Math.Floor(n);
        
        if (maxLevelsCanBuy < 1) return 1; 
        
        return maxLevelsCanBuy;
    }
    
    #endregion


    #region HẦM MỎ & TÀI NGUYÊN (MineShaft & Resources)
    
    public static double CalculateScaledCostByDepth(double baseCost, double depthMultiplier, int depthIndex, double roundMultiplier = 1)
    {
        return baseCost * Math.Pow(depthMultiplier, depthIndex - 1) * roundMultiplier;
    }

    public static double CalculateBaseIncome(double baseResourcePerSecond, double depthMultiplier, int depthIndex)
    {
        return baseResourcePerSecond * Math.Pow(depthMultiplier, depthIndex - 1);
    }

    public static double CalculateExponentialIncome(double scaledBaseIncome, double levelMultiplier, int targetLevel)
    {
        return scaledBaseIncome * Math.Pow(levelMultiplier, targetLevel - 1);
    }
    
    #endregion


    #region NHÀ KHO & THANG MÁY (Warehouse & Elevator Capacity)
    
    public static double CalculateCapacity(double baseCapacity, double capacityMultiplier, int targetLevel, double roundMultiplier = 1, double prestigeMultiplier = 1)
    {
        return baseCapacity * Math.Pow(capacityMultiplier, targetLevel - 1) * roundMultiplier * prestigeMultiplier;
    }
    
    #endregion


    #region MỞ KHÓA & NÂNG CẤP HỆ THỐNG (Unlocks & Managers)
    
    public static double CalculateUnlockCost(double baseCost, double depthMultiplier, int depthIndex, double roundMultiplier = 1)
    {
        return baseCost * Math.Pow(depthMultiplier, depthIndex) * roundMultiplier;
    }

    public static double CalculateManagerCost(double baseCost, double currentMultiplier, int count, double roundMultiplier = 1)
    {
        return baseCost * Math.Pow(currentMultiplier, count) * roundMultiplier;
    }
    
    #endregion


    #region VĨ MÔ (Macro Economy: Rounds & Prestige)
    
    public static double CalculateRoundMultiplier(double difficultyMultiplier, int currentRound)
    {
        return Math.Pow(difficultyMultiplier, currentRound - 1);
    }

    public static double CalculatePrestigeMultiplier(double lifetimeCash, double baseRequirement)
    {
        return 1.0 + Math.Pow(lifetimeCash / baseRequirement, 0.5);
    }
    
    #endregion


    #region CHI PHÍ BẢO TRÌ (Maintenance & Penalties)
    
    public static double CalculateRepairCost(double totalExtractionPerSecond, float durationMultiplier)
    {
        return totalExtractionPerSecond * durationMultiplier;
    }

    public static double CalculateMoraleCost(double workerProductivity, float costMultiplier, float missingMorale)
    {
        return workerProductivity * costMultiplier * missingMorale;
    }

    public static double CalculateReviveCost(double workerProductivity, float reviveMultiplier)
    {
        return workerProductivity * reviveMultiplier;
    }

    public static double CalculateEnduranceUnitCost(double totalExtractionPerSecond, float costMultiplier)
    {
        return totalExtractionPerSecond * costMultiplier;
    }
    
    #endregion


    #region TÍNH TOÁN OFFLINE (Offline Progression)
    
    public static double CalculateOfflineIncome(double bottleneckIncomeRate, long effectiveOfflineSeconds, float offlineIncomeMultiplier)
    {
        return bottleneckIncomeRate * effectiveOfflineSeconds * offlineIncomeMultiplier;
    }
    
    #endregion
}
