using UnityEngine;
using TMPro; // Thêm thư viện này để dùng TextMeshPro
using System.Collections.Generic; // Thêm thư viện dùng List
using DG.Tweening; // Thư viện tạo hiệu ứng DOTween

// Kế thừa Facility thay vì MonoBehaviour để có sẵn tính năng Nâng cấp
public partial class MineShaft : Facility 
{
    public override FacilityType GetFacilityType() => FacilityType.MineShaft;

    [Header("Shaft Settings")]
    [Tooltip("Thứ tự của hầm (từ 1 đến 10). Dùng để tính toán Cost/Income theo độ sâu.")]
    public int ShaftIndex = 1;

    // Ghi đè để áp dụng Toán học độ sâu hầm
    public override double ScaledBaseCost 
    {
        get 
        {
            if (Config == null) return 0;
            double roundMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0;
            if (Gamemanager.Instance != null && Gamemanager.Instance.GlobalConfig != null)
            {
                return Config.BaseCost * System.Math.Pow(Gamemanager.Instance.GlobalConfig.ShaftDepthMultiplier, ShaftIndex - 1) * roundMultiplier;
            }
            return Config.BaseCost * System.Math.Pow(15, ShaftIndex - 1) * roundMultiplier;
        }
    }
    
    // Ghi đè hệ số nhân mặc định nếu cần
    public override double CostMultiplier => 1.14;

    public double CurrentResource = 0; 
    
    public double BaseResourcePerSecond = 10; 
    
    [Header("Manager Settings")]
    // Các buff này sẽ nằm chung dưới thẻ Manager Buffs của lớp cha Facility
    public float MinerMoveSpeedBuff = 1f;
    public float MinerDigSpeedBuff = 1f;
    public float ProductivityBuff = 1f;

    // Năng suất của một thợ mỏ
    public double ResourcePerSecond => GetWorkerProductivity(Level); 

    public double GetWorkerProductivity(int targetLevel, bool isOfflineCalculation = false)
    {
        double depthMultiplier = 12;
        double levelMultiplier = 1.07;
        if (Gamemanager.Instance != null && Gamemanager.Instance.GlobalConfig != null)
        {
            depthMultiplier = Gamemanager.Instance.GlobalConfig.ShaftIncomeDepthMultiplier;
            levelMultiplier = Gamemanager.Instance.GlobalConfig.ShaftIncomeLevelMultiplier;
        }

        // Thu nhập cơ bản tăng theo độ sâu hầm (Gấp 12 lần mỗi hầm)
        double scaledBaseIncome = BaseResourcePerSecond * System.Math.Pow(depthMultiplier, ShaftIndex - 1); 
        // 1.07 là hệ số nhân mũ mỗi cấp độ của hầm.
        double exponentialIncome = scaledBaseIncome * System.Math.Pow(levelMultiplier, targetLevel - 1);
        
        double prestigeMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.PrestigeMultiplier : 1.0;
        double roundMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0;
        
        double currentProductivityBuff = isOfflineCalculation ? 1.0 : ProductivityBuff;
        double finalIncome = exponentialIncome * currentProductivityBuff * prestigeMultiplier * roundMultiplier;

        // --- FIRE DEBUFF LOGIC ---
        if (!isOfflineCalculation)
        {
            if (bigBurnTimer > 0)
            {
                finalIncome *= 0.1; // Giảm 90%
            }
            else if (normalBurnTimer > 0)
            {
                finalIncome *= 0.5; // Giảm 50%
            }
        }

        return finalIncome;
    }

    public double GetTotalExtractionPerSecond(int targetLevel, bool isOfflineCalculation = false)
    {
        // Tổng lượng đào = Năng suất 1 thợ * Số lượng thợ
        return GetWorkerProductivity(targetLevel, isOfflineCalculation) * GetMinersCount(targetLevel);
    }

    [Header("UI")]
    public TextMeshProUGUI shaftCashText; // Hiển thị số tiền/tài nguyên hiện tại của hầm

    protected override void Start()
    {
        base.Start(); // Gọi hàm Start của lớp cha (Facility) để update text Level

        // Tìm thợ mỏ có sẵn trong cảnh (con của hầm) và thêm vào danh sách nếu chưa có
        Miner[] existingMiners = GetComponentsInChildren<Miner>();
        foreach (Miner m in existingMiners)
        {
            if (!activeMiners.Contains(m))
            {
                m.currentShaft = this;
                activeMiners.Add(m);
            }
        }

        UpdateUI();
        UpdateEnduranceUI();
    }

    protected override void Update()
    {
        base.Update();
        
        // --- CLICK TO EXTINGUISH FIRE ---
        if ((normalBurnTimer > 0 || bigBurnTimer > 0) && Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = GetComponent<Collider2D>();
            if (col != null && col.OverlapPoint(mouseWorldPos))
            {
                ExtinguishFireClick();
            }
        }
    }

    public override void ActivateManagerSkill()
    {
        // Khóa không cho bật kỹ năng (kể cả bất tử) nếu hầm đang trong trạng thái bị vỡ/hỏng
        if (isBroken) return;
        
        base.ActivateManagerSkill();
    }

    protected override void ApplyManagerBuff()
    {
        base.ApplyManagerBuff();
        if (currentManager == null) return;
        
        // --- SENIOR SPECIAL FEATURE (Invincibility) ---
        if (currentManager.SpecialFeature == SeniorSpecialFeature.SpecialFeature)
        {
            isInvincible = true;
            if (invincibilityVFX != null) invincibilityVFX.SetActive(true);
            
            // Hồi đầy máu ngay lập tức
            currentEndurance = maxEndurance;
            UpdateEnduranceUI();
            SpawnDamagePopup(100f, Color.yellow, true);
            
            // Xóa hiệu ứng lửa của Rồng
            normalBurnTimer = 0f;
            bigBurnTimer = 0f;
            fireClicksRemaining = 0;
            StopVFXSmoothly(normalBurnVFX);
            StopVFXSmoothly(bigBurnVFX);
            
            // Hồi sinh và giải độc toàn bộ thợ mỏ trong hầm
            if (activeMiners != null)
            {
                foreach (var miner in activeMiners)
                {
                    if (miner != null)
                    {
                        WorkerHealth wh = miner.GetComponent<WorkerHealth>();
                        if (wh != null) wh.Revive();
                    }
                }
            }
            
            // Xóa DOT skill 3 của Boss
            ForceStopSkill3();

            // Tiêu diệt Minion đang có trong hầm
            MinionController[] minions = GetComponentsInChildren<MinionController>(true);
            foreach (var minion in minions)
            {
                if (minion.gameObject.activeInHierarchy)
                {
                    minion.TakeDamage(9999f); // Tiêu diệt minion
                }
            }
        }
        
        float buffMultiplier = 1f + (currentManager.BuffValue / 100f);
        float costDiscount = 1f - (currentManager.BuffValue / 100f);

        switch (currentManager.BuffType)
        {
            case ManagerBuffType.MoveSpeed:
                MinerMoveSpeedBuff = buffMultiplier;
                break;
            case ManagerBuffType.MiningSpeed:
                MinerDigSpeedBuff = buffMultiplier;
                break;
        }
    }

    protected override void RemoveManagerBuff()
    {
        base.RemoveManagerBuff();
        isInvincible = false;
        if (invincibilityVFX != null) invincibilityVFX.SetActive(false);

        MinerMoveSpeedBuff = 1f;
        MinerDigSpeedBuff = 1f;
        UpgradeCostDiscount = 1f;
        UpdateUpgradeUI();
    }

    private void UpdateUI()
    {
        if (shaftCashText != null)
        {
            // Sử dụng CurrencyFormatter để hiển thị số mượt hơn (K, M, B)
            shaftCashText.text = CurrencyFormatter.FormatMoney(CurrentResource);
        }
    }

    // Logic xử lý thêm (nếu có) khi Hầm mỏ được nâng cấp
    protected override void OnUpgraded()
    {
        // Vì ResourcePerSecond tính trực tiếp từ Level, nên nó tự động tăng.
        // Cập nhật số lượng thợ mỏ nếu đạt đủ level
        CheckAndSpawnMiners();
    }


    public override (string curVal, string nextVal) GetStatDisplay(int statIndex, int currentLevel, int nextLevel)
    {
        string curVal = "0";
        string nextVal = "0";
        
        switch (statIndex)
        {
            case 0: // Tổng khai thác
                curVal = CurrencyFormatter.FormatMoney(GetTotalExtractionPerSecond(currentLevel)) + "/s";
                nextVal = CurrencyFormatter.FormatMoney(GetTotalExtractionPerSecond(nextLevel)) + "/s";
                break;
            case 1: // Số thợ mỏ
                curVal = GetMinersCount(currentLevel).ToString();
                nextVal = GetMinersCount(nextLevel).ToString();
                break;
            case 2: // Tốc độ di chuyển
                curVal = GetMinerMoveSpeed(currentLevel).ToString("F2");
                nextVal = GetMinerMoveSpeed(nextLevel).ToString("F2");
                break;
            case 3: // Tốc độ khai thác (thời gian cuốc đất)
                curVal = GetMinerDigTime(currentLevel).ToString("F2") + "s";
                nextVal = GetMinerDigTime(nextLevel).ToString("F2") + "s";
                break;
            case 4: // Năng suất 1 thợ mỏ
                curVal = CurrencyFormatter.FormatMoney(GetWorkerProductivity(currentLevel)) + "/s";
                nextVal = CurrencyFormatter.FormatMoney(GetWorkerProductivity(nextLevel)) + "/s";
                break;
        }

        return (curVal, nextVal);
    }

}
