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
    public int TotalHiredCount = 0; // Số lần đã thuê trong lịch sử (dùng để tính tiền và đếm mốc)
    public int HiresUntilPity = 10;
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
            HiresUntilPity = Config.PityThreshold;
        }
    }

    public double GetCurrentHireCost()
    {
        return BaseHireCost * Math.Pow(HireMultiplier, TotalHiredCount);
    }

    public bool CanUnlockSenior()
    {
        // Yêu cầu: Đã mua 10 lần VÀ Level người chơi >= 6
        bool hasEnoughHires = TotalHiredCount >= 10;
        bool hasEnoughLevel = Gamemanager.Instance != null && Gamemanager.Instance.PlayerLevel >= 6;
        return hasEnoughHires && hasEnoughLevel;
    }

    public bool HireManager()
    {
        double cost = GetCurrentHireCost();

        if (Gamemanager.Instance == null || !Gamemanager.Instance.DeductCash(cost))
        {
            Debug.Log("Không đủ tiền thuê quản lý!");
            return false;
        }

        // Tạo quản lý mới
        ManagerData newManager = GenerateRandomManager(cost);
        OwnedManagers.Add(newManager);
        
        TotalHiredCount++;
        HiresUntilPity--;

        if (HiresUntilPity <= 0)
        {
            HiresUntilPity = Config != null ? Config.PityThreshold : 10;
        }
        
        OnManagerListUpdated?.Invoke();
        OnPityUpdated?.Invoke();
        return true;
    }

    private ManagerData GenerateRandomManager(double hirePrice)
    {
        ManagerData md = new ManagerData();
        md.OriginalHirePrice = hirePrice;
        
        if (Config == null || Config.RaritySettings == null || Config.RaritySettings.Count == 0)
        {
            Debug.LogError("Chưa cài đặt ManagerConfigSO hoặc RaritySettings trống!");
            return md;
        }

        // Xác định độ hiếm (có xét bảo hiểm)
        if (HiresUntilPity <= 1)
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
            HiresUntilPity = 1; // Sẽ bị trừ về 0 sau khi hàm này chạy xong và reset về Threshold
        }

        // Random Buff Type
        md.BuffType = (ManagerBuffType)Random.Range(0, 3); // 0: Mining, 1: Move, 2: Cost

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
    public List<ManagerData> GetManagersByFilter(ManagerBuffType buffType)
    {
        return OwnedManagers.FindAll(m => m.BuffType == buffType);
    }
}
