using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ManagerController : MonoBehaviour, ISaveable
{
    public static ManagerController Instance { get; private set; }

    [Header("Manager Settings")]
    public ManagerConfigSO Config;
    public double BaseHireCost = 100;
    [Tooltip("Hệ số nhân giá mua manager Thang máy & Nhà kho")]
    public float GlobalHireMultiplier = 5.0f;
    [Tooltip("Hệ số nhân giá mua manager Hầm mỏ")]
    public float LocalHireMultiplier = 4.0f;
    
    [Header("Data")]
    public Dictionary<FacilityType, int> TotalHiredCounts = new Dictionary<FacilityType, int>();
    public Dictionary<FacilityType, int> HiresUntilPitys = new Dictionary<FacilityType, int>();
    public List<ManagerData> OwnedManagers = new List<ManagerData>();

    public Action OnManagerListUpdated; // Sự kiện khi danh sách thay đổi
    public Action OnPityUpdated; // Sự kiện khi biến đếm Pity thay đổi

    private int GetSafePityThreshold()
    {
        int pity = Config != null ? Config.PityThreshold : 10;
        return pity > 0 ? pity : 10; // Fallback to 10 nếu cấu hình bị lỗi (<= 0)
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ nguyên danh sách Manager khi qua Round mới
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (Config != null)
        {
            foreach (FacilityType type in Enum.GetValues(typeof(FacilityType)))
            {
                if (!TotalHiredCounts.ContainsKey(type)) TotalHiredCounts[type] = 0;
                if (!HiresUntilPitys.ContainsKey(type)) HiresUntilPitys[type] = GetSafePityThreshold();
            }
        }
    }

    public double GetCurrentHireCost(FacilityType type)
    {
        int count = TotalHiredCounts.ContainsKey(type) ? TotalHiredCounts[type] : 0;
        double roundMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0;
        
        double baseCost = BaseHireCost; // Mặc định: 100
        double currentMultiplier; 

        if (type == FacilityType.Elevator || type == FacilityType.Warehouse)
        {
            // Thang máy & Nhà kho: Hỗ trợ toàn cục, chống spam mạnh mẽ.
            currentMultiplier = GlobalHireMultiplier; 
        }
        else 
        {
            // Hầm mỏ: Tác động cục bộ 1 hầm.
            currentMultiplier = LocalHireMultiplier;
        }

        // CÂN BẰNG TOÁN HỌC CHUẨN IDLE GAME: Giá thuê tăng theo hàm mũ (Multiplier ^ count)
        // Hệ số vòng được nhân vào để giữ nguyên độ cân bằng qua từng màn chơi.
        // Khi Prestige, hệ số lạm phát KHÔNG ĐƯỢC nhân vào đây nữa vì quản lý đã bị reset.
        return MathHelper.CalculateManagerCost(baseCost, currentMultiplier, count, roundMultiplier);
    }

    public bool CanUnlockSenior(FacilityType type)
    {
        // Yêu cầu: Đã mua 10 lần VÀ Level người chơi >= 6
        int count = TotalHiredCounts.ContainsKey(type) ? TotalHiredCounts[type] : 0;
        bool hasEnoughHires = count >= 10;
        bool hasEnoughLevel = Gamemanager.Instance != null && Gamemanager.Instance.PlayerLevel >= 6;
        return hasEnoughHires && hasEnoughLevel;
    }

    public void ResetManagers()
    {
        OwnedManagers.Clear();
        TotalHiredCounts.Clear();
        HiresUntilPitys.Clear();

        if (Config != null)
        {
            foreach (FacilityType type in Enum.GetValues(typeof(FacilityType)))
            {
                TotalHiredCounts[type] = 0;
                HiresUntilPitys[type] = GetSafePityThreshold();
            }
        }

        OnManagerListUpdated?.Invoke();
        OnPityUpdated?.Invoke();
        Debug.Log("<color=yellow>Đã xóa toàn bộ Quản lý (Hard Reset) để bắt đầu Vòng mới!</color>");
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
        if (!HiresUntilPitys.ContainsKey(type)) HiresUntilPitys[type] = GetSafePityThreshold();

        // Tạo quản lý mới
        ManagerData newManager = GenerateRandomManager(cost, type);
        OwnedManagers.Add(newManager);
        
        TotalHiredCounts[type]++;
        HiresUntilPitys[type]--;

        if (HiresUntilPitys[type] <= 0)
        {
            HiresUntilPitys[type] = GetSafePityThreshold();
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
        int count = TotalHiredCounts.ContainsKey(facilityType) ? TotalHiredCounts[facilityType] : 0;

        if (pity <= 1)
        {
            md.Rarity = ManagerRarity.Senior; // Bảo hiểm 100% ra Senior
        }
        else
        {
            float totalWeight = 0;
            foreach(var r in Config.RaritySettings)
            {
                totalWeight += r.Weight;
            }
            
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

        // Thiết lập chỉ số Buff linh hoạt từ file cấu hình SO
        ManagerConfigSO.RaritySetting setting = Config.GetRaritySetting(md.Rarity);
        
        // Chốt an toàn: Nếu file SO chưa được cấu hình (MaxBuffValue = 0) thì dùng cấu hình dự phòng
        if (setting != null && setting.MaxBuffValue > 0)
        {
            // Lấy ngẫu nhiên sức mạnh và thời gian trong khoảng cho phép của SO
            md.BuffValue = Random.Range(setting.MinBuffValue, setting.MaxBuffValue);
            md.BuffDuration = Random.Range(setting.MinDuration, setting.MaxDuration);
            md.CooldownDuration = Random.Range(setting.MinCooldown, setting.MaxCooldown);
            
            // Quản lý cấp cao (Senior) 100% có tính năng đặc biệt
            md.SpecialFeature = md.Rarity == ManagerRarity.Senior ? SeniorSpecialFeature.SpecialFeature : SeniorSpecialFeature.None;
        }
        else
        {
            // Dự phòng (Fallback) an toàn nếu file cấu hình bị lỗi
            switch (md.Rarity)
            {
                case ManagerRarity.Junior:
                    md.BuffValue = 50f; 
                    md.BuffDuration = 60f; 
                    md.CooldownDuration = 120f; 
                    md.SpecialFeature = SeniorSpecialFeature.None;
                    break;
                    
                case ManagerRarity.Director:
                    md.BuffValue = 70f; 
                    md.BuffDuration = 120f; 
                    md.CooldownDuration = 300f; 
                    md.SpecialFeature = SeniorSpecialFeature.None;
                    break;
                    
                case ManagerRarity.Senior:
                    md.BuffValue = 85f; 
                    md.BuffDuration = 300f; 
                    md.CooldownDuration = 600f; 
                    md.SpecialFeature = SeniorSpecialFeature.SpecialFeature; 
                    break;
            }
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

    public void PopulateSaveData(SaveData data)
    {
        data.OwnedManagers.Clear();
        foreach (var md in OwnedManagers)
        {
            ManagerSaveData smd = new ManagerSaveData();
            smd.Id = md.Id;
            smd.Name = md.Name;
            smd.CharacterID = md.CharacterID;
            smd.Rarity = (int)md.Rarity;
            smd.BuffType = (int)md.BuffType;
            smd.AssignedFacilityType = (int)md.AssignedFacilityType;
            smd.SpecialFeature = (int)md.SpecialFeature;
            smd.BuffValue = md.BuffValue;
            smd.BuffDuration = md.BuffDuration;
            smd.CooldownDuration = md.CooldownDuration;
            smd.OriginalHirePrice = md.OriginalHirePrice;
            smd.IsAssigned = md.IsAssigned;
            smd.AssignedShaftId = md.AssignedShaftId;
            
            long currentUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            float remainingSkill = Mathf.Max(0, md.SkillEndTime - Time.time);
            float remainingCooldown = Mathf.Max(0, md.CooldownEndTime - Time.time);
            
            smd.SkillEndUnixTime = currentUnix + (long)remainingSkill;
            smd.CooldownEndUnixTime = currentUnix + (long)remainingCooldown;
            
            data.OwnedManagers.Add(smd);
        }
    }

    public void LoadFromSaveData(SaveData data)
    {
        if (data.OwnedManagers == null) return;

        OwnedManagers.Clear();
        foreach (var smd in data.OwnedManagers)
        {
            ManagerData md = new ManagerData();
            md.Id = smd.Id;
            md.Name = smd.Name;
            md.CharacterID = smd.CharacterID;
            md.Rarity = (ManagerRarity)smd.Rarity;
            md.BuffType = (ManagerBuffType)smd.BuffType;
            md.AssignedFacilityType = (FacilityType)smd.AssignedFacilityType;
            md.SpecialFeature = (SeniorSpecialFeature)smd.SpecialFeature;
            md.BuffValue = smd.BuffValue;
            md.BuffDuration = smd.BuffDuration;
            md.CooldownDuration = smd.CooldownDuration;
            md.OriginalHirePrice = smd.OriginalHirePrice;
            md.IsAssigned = smd.IsAssigned;
            md.AssignedShaftId = smd.AssignedShaftId;
            
            long currentUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            float remainingSkill = Mathf.Max(0, smd.SkillEndUnixTime - currentUnix);
            float remainingCooldown = Mathf.Max(0, smd.CooldownEndUnixTime - currentUnix);
            
            md.SkillEndTime = Time.time + remainingSkill;
            md.CooldownEndTime = Time.time + remainingCooldown;
            
            OwnedManagers.Add(md);
        }
        OnManagerListUpdated?.Invoke();
    }
}
