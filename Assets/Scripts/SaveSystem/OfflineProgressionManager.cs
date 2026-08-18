using System;
using UnityEngine;

public class OfflineProgressionManager : MonoBehaviour
{
    public static OfflineProgressionManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ProcessOfflineProgression(SaveData currentSaveData)
    {
        if (currentSaveData == null) 
        {
            Debug.Log("[Offline] CurrentSaveData is null. Aborting.");
            return;
        }
        if (currentSaveData.LastSaveTimeUnixSeconds == 0) 
        {
            Debug.Log("[Offline] LastSaveTimeUnixSeconds is 0 (new save). Aborting.");
            return;
        }

        long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        long offlineSeconds = currentTime - currentSaveData.LastSaveTimeUnixSeconds;
        
        Debug.Log($"[Offline] CurrentTime: {currentTime}, LastSaveTime: {currentSaveData.LastSaveTimeUnixSeconds}, OfflineSeconds: {offlineSeconds}");

        if (offlineSeconds <= 0) 
        {
            Debug.Log("[Offline] offlineSeconds <= 0. Aborting.");
            return;
        }

        // Kỹ thuật Anti-Cheat: Dual Time-Delta (Đo lường Delta Kép)
        long currentUptime = Environment.TickCount / 1000;
        long uptimeDelta = currentUptime - currentSaveData.LastSaveUptimeSeconds;
        
        // Nếu uptimeDelta >= 0 (máy chưa bị khởi động lại) và thời gian trôi qua trên đồng hồ lớn hơn bất thường so với thời gian máy chạy
        // (Cho phép sai số 60 giây để hệ điều hành sync giờ)
        if (uptimeDelta >= 0 && offlineSeconds > uptimeDelta + 60)
        {
            Debug.LogWarning($"[Anti-Cheat] Phát hiện gian lận đổi giờ! WallClockDelta: {offlineSeconds}s, UptimeDelta: {uptimeDelta}s. Ép thời gian Offline về Uptime thật.");
            offlineSeconds = uptimeDelta; // Ép thời gian trôi qua chỉ bằng thời gian máy thực sự đã chạy
        }

        // Theo thiết kế: Max offline time 4 giờ
        long maxOfflineSeconds = 14400; 
        long effectiveOfflineSeconds = Math.Min(offlineSeconds, maxOfflineSeconds);

        Debug.Log($"[SaveManager] Player was offline for {offlineSeconds}s. Calculating rewards for {effectiveOfflineSeconds}s.");

        // Tính Offline Math
        double offlineIncomeRate = CalculateBaseBottleneckIncome(currentSaveData);
        // Thu nhập offline chỉ bằng 25% (0.25)
        double totalOfflineCash = offlineIncomeRate * effectiveOfflineSeconds * 0.25;

        Debug.Log($"[Offline] BaseIncomeRate (Bottleneck): {offlineIncomeRate}, TotalOfflineCash: {totalOfflineCash}");

        if (totalOfflineCash > 0)
        {
            Debug.Log($"[SaveManager] Offline Cash Earned: {totalOfflineCash}");
            if (Gamemanager.Instance != null)
            {
                Gamemanager.Instance.AddCash(totalOfflineCash);
            }
            
            // Bật Popup UI báo cáo cho người chơi
            if (OfflineModalUI.Instance != null)
            {
                OfflineModalUI.Instance.ShowOfflineRewards(effectiveOfflineSeconds, totalOfflineCash);
            }
            else
            {
                Debug.LogWarning("[SaveManager] Không tìm thấy OfflineModalUI trong scene. Vui lòng kiểm tra GameObject chứa script này có đang bị Disable không.");
            }
        }
        else
        {
            Debug.Log("[Offline] totalOfflineCash is 0. No modal will be shown.");
        }
    }

    private double CalculateBaseBottleneckIncome(SaveData currentSaveData)
    {
        double totalShaftIncome = 0;
        MineShaft[] shafts = FindObjectsOfType<MineShaft>();
        foreach (var shaft in shafts)
        {
            if (!shaft.isBroken && shaft.currentManager != null) 
            {
                var savedData = currentSaveData.MineShafts.Find(s => s.Index == shaft.ShaftIndex);
                if (savedData != null && savedData.MinionHealth > 0)
                {
                    Debug.Log($"[Offline] Shaft {shaft.ShaftIndex} is blocked by minion.");
                    continue; 
                }

                double shaftIncome = shaft.GetTotalExtractionPerSecond(shaft.Level, true);
                totalShaftIncome += shaftIncome;
                Debug.Log($"[Offline] Shaft {shaft.ShaftIndex} adds {shaftIncome} to totalShaftIncome.");
            }
            else
            {
                Debug.Log($"[Offline] Shaft {shaft.ShaftIndex} ignored (Broken: {shaft.isBroken}, Manager: {shaft.currentManager != null}).");
            }
        }

        double elevatorThroughput = 0;
        Elevator elevator = FindObjectOfType<Elevator>();
        if (elevator != null && elevator.currentManager != null)
        {
             float avgRoundTripTime = elevator.GetLoadTime(elevator.Level) + elevator.GetUnloadTime(elevator.Level) + 5f; 
             elevatorThroughput = elevator.GetCapacity(elevator.Level) / avgRoundTripTime;
             Debug.Log($"[Offline] Elevator Throughput: {elevatorThroughput} (Capacity: {elevator.GetCapacity(elevator.Level)}, Time: {avgRoundTripTime})");
        }
        else
        {
             Debug.Log($"[Offline] Elevator ignored (Exists: {elevator != null}, Manager: {elevator?.currentManager != null}).");
        }

        double warehouseThroughput = 0;
        Warehouse warehouse = FindObjectOfType<Warehouse>();
        if (warehouse != null && warehouse.currentManager != null)
        {
            warehouseThroughput = warehouse.GetTotalThroughputDisplay(warehouse.Level);
            Debug.Log($"[Offline] Warehouse Throughput: {warehouseThroughput}");
        }
        else
        {
            Debug.Log($"[Offline] Warehouse ignored (Exists: {warehouse != null}, Manager: {warehouse?.currentManager != null}).");
        }

        if (elevator == null || warehouse == null) 
        {
            Debug.Log("[Offline] Elevator or Warehouse is completely NULL in scene. Returning 0.");
            return 0;
        }

        double bottleneck = Math.Min(totalShaftIncome, Math.Min(elevatorThroughput, warehouseThroughput));
        Debug.Log($"[Offline] Final Bottleneck: Min({totalShaftIncome}, {elevatorThroughput}, {warehouseThroughput}) = {bottleneck}");
        return bottleneck;
    }
}
