using System;

public static class MathHelper
{
    // Công thức tính chi phí nâng cấp: Cost = BaseCost * (Multiplier ^ Level)
    // Sau này Hào có thể chỉnh sửa công thức này thoải mái mà không sợ hỏng game
    public static double CalculateUpgradeCost(double baseCost, double costMultiplier, int level)
    {
        return baseCost * Math.Pow(costMultiplier, level - 1);
    }
}
