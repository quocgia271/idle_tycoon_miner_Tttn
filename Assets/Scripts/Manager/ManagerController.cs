using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ManagerController : MonoBehaviour
{
    public static ManagerController Instance { get; private set; }

    [Header("Manager Settings")]
    public ManagerConfigSO Config;
    public double BaseHireCost = 100;
    public float HireMultiplier = 1.5f;
    
    [Header("Data")]
    public Dictionary<FacilityType, int> TotalHiredCounts = new Dictionary<FacilityType, int>();
    public Dictionary<FacilityType, int> HiresUntilPitys = new Dictionary<FacilityType, int>();
    public List<ManagerData> OwnedManagers = new List<ManagerData>();

    public Action OnManagerListUpdated; // Sự kiện khi danh sách thay đổi
    public Action OnPityUpdated; // Sự kiện khi biến đếm Pity thay đổi

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (Config != null)
        {
            foreach (FacilityType type in Enum.GetValues(typeof(FacilityType)))
            {
                if (!TotalHiredCounts.ContainsKey(type)) TotalHiredCounts[type] = 0;
                if (!HiresUntilPitys.ContainsKey(type)) HiresUntilPitys[type] = Config.PityThreshold;
            }
        }
    }

    public double GetCurrentHireCost(FacilityType type)
    {
        int count = TotalHiredCounts.ContainsKey(type) ? TotalHiredCounts[type] : 0;
        return BaseHireCost * Math.Pow(HireMultiplier, count);
    }

    public bool CanUnlockSenior(FacilityType type)
    {
        // Yêu cầu: Đã mua 10 lần VÀ Level người chơi >= 6
        int count = TotalHiredCounts.ContainsKey(type) ? TotalHiredCounts[type] : 0;
        bool hasEnoughHires = count >= 10;
        bool hasEnoughLevel = Gamemanager.Instance != null && Gamemanager.Instance.PlayerLevel >= 6;
        return hasEnoughHires && hasEnoughLevel;
    }

    public bool HireManager(FacilityType type)
    {
        double cost = GetCurrentHireCost(type);

        if (Gamemanager.Instance == null || !Gamemanager.Instance.DeductCash(cost))
        {
            Debug.Log("Không đủ tiền thuê quản lý!");
            return false;
        }

        if (!TotalHiredCounts.ContainsKey(type)) TotalHiredCounts[type] = 0;
        if (!HiresUntilPitys.ContainsKey(type)) HiresUntilPitys[type] = Config != null ? Config.PityThreshold : 10;

        // Tạo quản lý mới
        ManagerData newManager = GenerateRandomManager(cost, type);
        OwnedManagers.Add(newManager);
        
        TotalHiredCounts[type]++;
        HiresUntilPitys[type]--;

        if (HiresUntilPitys[type] <= 0)
        {
            HiresUntilPitys[type] = Config != null ? Config.PityThreshold : 10;
        }
        
        OnManagerListUpdated?.Invoke();
        OnPityUpdated?.Invoke();
        return true;
    }

    private ManagerData GenerateRandomManager(double hirePrice, FacilityType facilityType)
    {
        ManagerData md = new ManagerData();
        md.OriginalHirePrice = hirePrice;
        md.AssignedFacilityType = facilityType;
        
        if (Config == null || Config.RaritySettings == null || Config.RaritySettings.Count == 0)
        {
            Debug.LogError("Chưa cài đặt ManagerConfigSO hoặc RaritySettings trống!");
            return md;
        }

        // Xác định độ hiếm (có xét bảo hiểm)
        int pity = HiresUntilPitys.ContainsKey(facilityType) ? HiresUntilPitys[facilityType] : 10;
        if (pity <= 1)
        {
            md.Rarity = ManagerRarity.Senior; // Bảo hiểm 100% ra Senior
        }
        else
        {
            float totalWeight = 0;
            foreach(var r in Config.RaritySettings) totalWeight += r.Weight;
            
            float roll = Random.Range(0, totalWeight);
            float currentSum = 0;
            md.Rarity = ManagerRarity.Junior; // Default fallback
            
            foreach (var r in Config.RaritySettings)
            {
                currentSum += r.Weight;
                if (roll <= currentSum)
                {
                    md.Rarity = r.Rarity;
                    break;
                }
            }
        }

        // Nếu quay trúng Senior sớm hơn bảo hiểm, reset bảo hiểm luôn!
        if (md.Rarity == ManagerRarity.Senior)
        {
            HiresUntilPitys[facilityType] = 1; // Sẽ bị trừ về 0 sau khi hàm này chạy xong và reset về Threshold
        }

        // Random Buff Type
        md.BuffType = (ManagerBuffType)Random.Range(0, 3); // 0: Mining, 1: Move, 2: Cost

        // Mặc định mua ra
        if (md.Rarity == ManagerRarity.Senior)
        {
            // Nếu là quản lý cấp cao, random 50% có tính năng đặc biệt (có thể chỉnh lại sau)
            md.SpecialFeature = Random.value > 0.5f ? SeniorSpecialFeature.SpecialFeature : SeniorSpecialFeature.None;
        }
        else
        {
            md.SpecialFeature = SeniorSpecialFeature.None;
        }

        // Gán chỉ số từ SO
        var setting = Config.GetRaritySetting(md.Rarity);
        if (setting != null)
        {
            md.BuffValue = Random.Range(setting.MinBuffValue, setting.MaxBuffValue);
            md.BuffDuration = Random.Range(setting.MinDuration, setting.MaxDuration);
            md.CooldownDuration = Random.Range(setting.MinCooldown, setting.MaxCooldown);
        }

        // Bốc ngẫu nhiên 1 nhân vật trong danh sách SO
        if (Config.CharacterVisuals != null && Config.CharacterVisuals.Count > 0)
        {
            int randIndex = Random.Range(0, Config.CharacterVisuals.Count);
            md.CharacterID = Config.CharacterVisuals[randIndex].CharacterID;
        }

        md.Name = md.Rarity.ToString() + " " + Random.Range(100, 999);

        return md;
    }

    public void SellManager(ManagerData manager)
    {
        if (OwnedManagers.Contains(manager))
        {
            // Trả lại 50% tiền
            double refund = manager.OriginalHirePrice * 0.5;
            if (Gamemanager.Instance != null)
            {
                Gamemanager.Instance.AddCash(refund);
            }

            OwnedManagers.Remove(manager);
            OnManagerListUpdated?.Invoke();
        }
    }

    // Tiện ích lấy danh sách theo loại để Filter UI
    public List<ManagerData> GetManagersByFilter(FacilityType facilityType, ManagerBuffType? buffType = null)
    {
        if (buffType.HasValue)
        {
            return OwnedManagers.FindAll(m => m.AssignedFacilityType == facilityType && m.BuffType == buffType.Value);
        }
        return OwnedManagers.FindAll(m => m.AssignedFacilityType == facilityType);
    }
}
