using System;

public static class MathHelper
{
    // Công thức tính chi phí nâng cấp: Cost = BaseCost * (Multiplier ^ Level)
    // Sau này Hào có thể chỉnh sửa công thức này thoải mái mà không sợ hỏng game
    public static double CalculateUpgradeCost(double baseCost, double costMultiplier, int level)
    {
        return baseCost * Math.Pow(costMultiplier, level - 1);
    }

    // Hàm Hào sẽ viết vào Tuần 4: Tính số level tối đa mua được dựa trên số tiền hiện có (Dùng Logarit)
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
}
