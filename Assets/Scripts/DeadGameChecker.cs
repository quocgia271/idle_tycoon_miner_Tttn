using UnityEngine;
using System.Collections;
using DG.Tweening;

/// <summary>
/// Chịu trách nhiệm riêng biệt cho việc theo dõi trạng thái Dead Game (Kẹt game)
/// Tuân thủ nguyên tắc Single Responsibility (SRP) trong SOLID
/// </summary>
public class DeadGameChecker : MonoBehaviour
{
    public static DeadGameChecker Instance;

    [Header("UI & VFX")]
    [Tooltip("Kéo VFX cảnh báo Dead Game (ở nút Prestige) vào đây")]
    public GameObject deadGameVFX;
    
    [Tooltip("Kéo Prefab UI Image (bàn tay chỉ click) vào đây")]
    public GameObject clickIndicatorUI;
    
    [Header("Settings")]
    public float checkInterval = 1.5f; // Quét 1.5s/lần cho nhẹ máy

    public bool IsDeadGame { get; private set; }
    private bool wasDeadGame = false;

    private void Awake()
    {
        // Mỗi khi load lại Scene (qua Round 2, 3), một DeadGameChecker mới sẽ được tạo ra.
        // Ta chỉ cần cập nhật lại biến Instance để Gamemanager (kẻ sống dai) có thể tìm thấy nó.
        // Không dùng DontDestroyOnLoad ở đây vì nó chứa reference tới UI của Scene hiện tại.
        Instance = this;
    }

    private void Start()
    {
        StartCoroutine(CheckRoutine());
    }

    private IEnumerator CheckRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkInterval);
            IsDeadGame = PerformDeadGameCheck();
            
            // Chỉ cập nhật UI nếu trạng thái thay đổi (tránh gọi DOTween hay SetActive liên tục)
            if (IsDeadGame != wasDeadGame)
            {
                wasDeadGame = IsDeadGame;

                if (deadGameVFX != null) deadGameVFX.SetActive(IsDeadGame);
                if (clickIndicatorUI != null) clickIndicatorUI.SetActive(IsDeadGame);
            }
        }
    }

    private bool PerformDeadGameCheck()
    {
        if (Gamemanager.Instance == null) return false;
        
        double currentCash = Gamemanager.Instance.IdleCash;
        MineShaft[] shafts = FindObjectsOfType<MineShaft>();
        bool hasAnyUnlockedShaft = false;
        bool hasProductiveOrFixableShaft = false;
        double cheapestUnlockCost = double.MaxValue;

        foreach (var shaft in shafts)
        {
            if (shaft != null && shaft.gameObject.activeInHierarchy && shaft.activeMiners != null && shaft.activeMiners.Count > 0)
            {
                // BỎ QUA CÁC HẦM CHƯA ĐƯỢC MUA (LOCKED)
                // Hầm chưa mua sẽ có UI ShaftUnlocker đang bật, chưa purchased, và hầm không trong trạng thái isBroken
                ShaftUnlocker unlocker = shaft.GetComponentInChildren<ShaftUnlocker>();
                if (unlocker != null && unlocker.gameObject.activeInHierarchy && !unlocker.isPurchased && !shaft.isBroken)
                {
                    if (unlocker.requiredGold < cheapestUnlockCost)
                    {
                        cheapestUnlockCost = unlocker.requiredGold;
                    }
                    continue; // Hầm này đang bị khóa, người chơi không thể lấy tiền từ nó, bỏ qua!
                }

                hasAnyUnlockedShaft = true;
                
                bool isBroken = shaft.isBroken;
                double repairCost = shaft.repairCost;

                if (isBroken)
                {
                    // Nếu hầm bị hỏng, kiểm tra xem có đủ tiền sửa không
                    if (currentCash >= repairCost)
                    {
                        hasProductiveOrFixableShaft = true; // Có tiền sửa -> Chưa Dead Game
                    }
                }
                else
                {
                    // Hầm không hỏng, kiểm tra thợ mỏ
                    bool hasAliveMiner = false;
                    double lowestReviveCost = double.MaxValue;

                    foreach (var miner in shaft.activeMiners)
                    {
                        if (miner == null) continue;
                        WorkerHealth wh = miner.GetComponent<WorkerHealth>();
                        if (wh != null)
                        {
                            if (wh.healthState != WorkerHealth.HealthState.Dead)
                            {
                                hasAliveMiner = true;
                            }
                            else
                            {
                                // Lấy giá hồi sinh (theo logic của game)
                                float reviveMultiplier = (Gamemanager.Instance != null && Gamemanager.Instance.economyConfig != null) ? Gamemanager.Instance.economyConfig.ReviveIncomeSeconds : 3f;
                                double reviveCost = MathHelper.CalculateReviveCost(shaft.GetWorkerProductivity(shaft.Level), reviveMultiplier);
                                if (reviveCost < lowestReviveCost) lowestReviveCost = reviveCost;
                            }
                        }
                    }

                    if (hasAliveMiner)
                    {
                        hasProductiveOrFixableShaft = true; // Vẫn còn thợ sống -> kiếm được tiền -> Chưa Dead Game
                    }
                    else if (currentCash >= lowestReviveCost)
                    {
                        hasProductiveOrFixableShaft = true; // Thợ chết hết nhưng ĐỦ TIỀN hồi sinh -> Chưa Dead Game
                    }
                }
            }
        }

        // Nếu chưa mở hầm nào (hoặc vừa prestige xong, chưa có hầm)
        if (!hasAnyUnlockedShaft) 
        {
            // Trả về true (Dead Game) nếu người chơi CŨNG KHÔNG ĐỦ TIỀN để mua cái hầm rẻ nhất
            if (currentCash < cheapestUnlockCost)
            {
                return true; 
            }
            return false;
        }

        // Trả về true nếu KHÔNG CÓ hầm nào đang hoạt động và CŨNG KHÔNG ĐỦ TIỀN để sửa hầm / cứu thợ
        return !hasProductiveOrFixableShaft;
    }
}
