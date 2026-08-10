using UnityEngine;
using TMPro; // Khai báo sử dụng TextMeshPro
using UnityEngine.UI;
using DG.Tweening;

public enum FacilityType
{
    MineShaft,
    Elevator,
    Warehouse
}

// Lớp cha trừu tượng (Abstract) quản lý mọi thông số chung cho việc Nâng cấp
public abstract class Facility : MonoBehaviour, ISaveable
{
    public int Level = 1;

    [Header("Data Configuration")]
    [Tooltip("Kéo file Scriptable Object (vd: ElevatorConfig) vào đây")]
    public FacilityConfigSO Config;

    [Header("UI Reference")]
    public TextMeshProUGUI UpgradeText; // Kéo thả cái Text bên trong Button vào đây

    [Header("Manager Buffs")]
    public float UpgradeCostDiscount = 1f;

    public abstract FacilityType GetFacilityType();

    [Header("Manager Settings")]
    public ManagerData currentManager;
    public SimpleSpriteAnimator WorldManagerAnimator; 
    public Transform EmptyManagerLine; 
    
    [Header("Manager Skill State")]
    public bool IsSkillActive = false;
    public float SkillTimer = 0f;
    public float CooldownTimer = 0f;

    [Header("World Space Manager UI")]
    public Button worldSkillButton;
    public TextMeshProUGUI worldSkillTimerText;
    public Image worldSkillIconImage;

    protected Sprite defaultManagerSprite;

    // Thay vì tính trực tiếp, tạo 2 hàm ảo để lớp con (như MineShaft) có thể ghi đè (override)
    public virtual double ScaledBaseCost 
    {
        get
        {
            if (Config == null) return 0;
            double roundMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0;
            return Config.BaseCost * roundMultiplier;
        }
    }
    public virtual double CostMultiplier => Config != null ? Config.CostMultiplier : 1.14;

    // Tự động tính toán chi phí hiện tại bằng cách gọi sang MathHelper
    public virtual double CurrentUpgradeCost => MathHelper.CalculateUpgradeCost(ScaledBaseCost, CostMultiplier, Level) * UpgradeCostDiscount;

    protected virtual void Awake()
    {
        // Fix lỗi Unity tự động khởi tạo biến [Serializable] trước khi game chạy
        // Gán null ngay từ lúc Awake để các script khác như Miner không bị nhầm lẫn
        currentManager = null; 
    }

    // Chạy lần đầu tiên khi mở game để cập nhật số Level 1 lên nút bấm
    protected virtual void Start()
    {
        UpdateUpgradeUI();

        if (WorldManagerAnimator != null)
        {
            var img = WorldManagerAnimator.GetComponent<Image>();
            if (img != null) defaultManagerSprite = img.sprite;
            else
            {
                var sr = WorldManagerAnimator.GetComponent<SpriteRenderer>();
                if (sr != null) defaultManagerSprite = sr.sprite;
            }
        }

        if (worldSkillButton != null)
        {
            worldSkillButton.onClick.AddListener(ActivateManagerSkill);
            UpdateWorldSkillUI();
        }

        SpawnManagerVisual();
        
        if (worldSkillIconImage == null)
        {
            Debug.LogWarning($"Chú ý: {gameObject.name} chưa được kéo Ảnh Icon Kỹ Năng vào ô 'World Skill Icon Image' trong Inspector!");
        }
    }

    protected virtual void Update()
    {
        if (currentManager == null) 
        {
            if (worldSkillButton != null && worldSkillButton.gameObject.activeSelf)
                worldSkillButton.gameObject.SetActive(false); 
            return;
        }
        else
        {
            if (worldSkillButton != null && !worldSkillButton.gameObject.activeSelf)
                worldSkillButton.gameObject.SetActive(true); 
        }

        if (currentManager.IsSkillActive())
        {
            if (!IsSkillActive)
            {
                IsSkillActive = true;
                ApplyManagerBuff();
            }
        }
        else
        {
            if (IsSkillActive)
            {
                IsSkillActive = false;
                RemoveManagerBuff();
            }
        }

        UpdateWorldSkillUI();
    }

    // Gọi hàm này khi người chơi bấm nút "Nâng cấp" trên bản đồ game
    // Nó sẽ bật cái bảng UI Modal to đùng lên thay vì trừ tiền ngay
    public void OpenUpgradeModal()
    {
        if (this is MineShaft shaft && shaft.isBroken)
        {
            Debug.LogWarning("Hầm đang bị vỡ, phải sửa chữa mới được nâng cấp!");
            return;
        }

        // Khắc phục lỗi Modal bị tắt (Deactivated) từ đầu khiến Awake không chạy
        if (UpgradeModalUI.Instance == null)
        {
            UpgradeModalUI.Instance = FindObjectOfType<UpgradeModalUI>(true);
        }

        if (UpgradeModalUI.Instance != null)
        {
            UpgradeModalUI.Instance.ShowModal(this, Config);
        }
        else
        {
            Debug.LogError($"<color=red>Lỗi: Không tìm thấy UpgradeModalUI trong Scene! Hãy đảm bảo bạn đã kéo script UpgradeModalUI vào Canvas.</color>");
        }
    }

    // Bất kỳ nút [Upgrade] nào trên UI cũng có thể gọi thẳng vào hàm này
    public void TryUpgrade()
    {
        if (Gamemanager.Instance != null && Gamemanager.Instance.DeductCash(CurrentUpgradeCost))
        {
            Level++;
            OnUpgraded(); // Gọi lớp con để cập nhật chỉ số
            UpdateUpgradeUI(); // Cập nhật lại Text trên nút bấm
            Debug.Log($"<color=green>{gameObject.name} upgraded to level {Level}</color>");
        }
        else
        {
            Debug.Log($"<color=red>Not enough cash to upgrade {gameObject.name}</color>");
        }
    }

    // Hàm chuyên dùng để đổi chữ trên nút bấm
    protected virtual void UpdateUpgradeUI()
    {
        if (UpgradeText != null)
        {
            // Hiển thị Level và Giá tiền xuống dòng (\n)
            UpgradeText.text = $"Level {Level}";
        }
    }

    // Các lớp con (Hầm mỏ, Thang máy...) phải tự định nghĩa hàm này 
    // để quyết định xem chúng nó tăng hiệu suất gì sau khi lên cấp.
    protected abstract void OnUpgraded();

    // --- CÁC HÀM TÍNH TOÁN CHỈ SỐ THỰC TẾ ĐỂ CHẠY LOGIC GAME VÀ HIỂN THỊ UI ---

    // 1. Tính Sức Chứa (Tăng theo hàm mũ)
    public virtual double GetCapacity(int targetLevel)
    {
        if (Config == null) return 0;
        double roundMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0;
        double prestigeMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.PrestigeMultiplier : 1.0;
        float capacityMultiplier = 1.1f;
        if (Gamemanager.Instance != null && Gamemanager.Instance.GlobalConfig != null)
        {
            capacityMultiplier = Gamemanager.Instance.GlobalConfig.CapacityLevelMultiplier;
        }
        return Config.BaseCapacity * System.Math.Pow(capacityMultiplier, targetLevel - 1) * roundMultiplier * prestigeMultiplier;
    }

    // 2. Tính Tốc Độ (Tăng từ từ tuyến tính để không hỏng animation)
    public virtual float GetSpeed(int targetLevel)
    {
        if (Config == null) return 0;
        return Config.BaseSpeed + ((targetLevel - 1) * 0.05f); 
    }

    // 3. Tính Tổng Sản Lượng (Throughput = Sức chứa x Tốc độ)
    public virtual double GetTotalThroughput(int targetLevel)
    {
        return GetCapacity(targetLevel) * GetSpeed(targetLevel);
    }

    // 4. Lấy thông tin hiển thị UI (Cho phép lớp con tùy chỉnh)
    public virtual (string curVal, string nextVal) GetStatDisplay(int statIndex, int currentLevel, int nextLevel)
    {
        string curVal = "0";
        string nextVal = "0";
        
        if (statIndex == 0) // Slot 1: Tổng Sản lượng
        {
            curVal = CurrencyFormatter.FormatMoney(GetTotalThroughput(currentLevel)) + "/s";
            nextVal = CurrencyFormatter.FormatMoney(GetTotalThroughput(nextLevel)) + "/s";
        }
        else if (statIndex == 1) // Slot 2: Sức chứa
        {
            curVal = CurrencyFormatter.FormatMoney(GetCapacity(currentLevel));
            nextVal = CurrencyFormatter.FormatMoney(GetCapacity(nextLevel));
        }
        else if (statIndex == 2) // Slot 3: Tốc độ
        {
            curVal = GetSpeed(currentLevel).ToString("F2");
            nextVal = GetSpeed(nextLevel).ToString("F2");
        }

        return (curVal, nextVal);
    }

    public virtual void AssignManager(ManagerData manager)
    {
        currentManager = manager;
        IsSkillActive = manager.IsSkillActive();
        
        SpawnManagerVisual();
        
        if (IsSkillActive) ApplyManagerBuff();
        else RemoveManagerBuff();
        
        UpdateWorldSkillUI();
    }

    public virtual void RemoveManager()
    {
        if (currentManager != null && currentManager.IsSkillActive())
        {
            // Nếu đang bật skill mà bị tháo ra -> Tắt skill lập tức và đưa vào Cooldown
            currentManager.SkillEndTime = 0f;
            currentManager.CooldownEndTime = Time.time + currentManager.CooldownDuration;
        }

        currentManager = null;
        IsSkillActive = false;
        
        SpawnManagerVisual(); 
        
        RemoveManagerBuff();
        UpdateWorldSkillUI();
    }

    protected virtual void SpawnManagerVisual()
    {
        if (ManagerController.Instance == null || ManagerController.Instance.Config == null) return;

        if (currentManager != null)
        {
            if (WorldManagerAnimator != null)
            {
                WorldManagerAnimator.gameObject.SetActive(true);
                var charVis = ManagerController.Instance.Config.GetCharacterVisual(currentManager.CharacterID);
                if (charVis != null && charVis.AnimationFrames != null && charVis.AnimationFrames.Length > 0)
                {
                    WorldManagerAnimator.enabled = true; 
                    WorldManagerAnimator.frames = charVis.AnimationFrames;
                    
                    var sr = WorldManagerAnimator.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = charVis.AnimationFrames[0];
                    var img = WorldManagerAnimator.GetComponent<Image>();
                    if (img != null) img.sprite = charVis.AnimationFrames[0];
                }
                
                WorldManagerAnimator.transform.DOKill();
                WorldManagerAnimator.transform.localScale = Vector3.one;
            }
            
            if (worldSkillIconImage != null)
            {
                worldSkillIconImage.sprite = ManagerController.Instance.Config.GetSkillIcon(currentManager.BuffType);
            }
        }
        else
        {
            if (WorldManagerAnimator != null)
            {
                WorldManagerAnimator.gameObject.SetActive(true); 
                WorldManagerAnimator.enabled = false; 

                var sr = WorldManagerAnimator.GetComponent<SpriteRenderer>();
                if (sr != null && defaultManagerSprite != null) sr.sprite = defaultManagerSprite;
                var img = WorldManagerAnimator.GetComponent<Image>();
                if (img != null && defaultManagerSprite != null) img.sprite = defaultManagerSprite;

                WorldManagerAnimator.transform.DOKill();
                WorldManagerAnimator.transform.localScale = Vector3.one;
                WorldManagerAnimator.transform.DOScale(1.1f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
        }
    }

    public virtual void ActivateManagerSkill()
    {
        if (currentManager != null && !currentManager.IsSkillActive() && !currentManager.IsOnCooldown())
        {
            currentManager.SkillEndTime = Time.time + currentManager.BuffDuration;
            currentManager.CooldownEndTime = currentManager.SkillEndTime + currentManager.CooldownDuration;

            IsSkillActive = true;
            ApplyManagerBuff();
            UpdateWorldSkillUI();
        }
    }

    protected virtual void ApplyManagerBuff()
    {
        if (currentManager == null) return;
        
        float costDiscount = 1f - (currentManager.BuffValue / 100f);

        switch (currentManager.BuffType)
        {
            case ManagerBuffType.ReduceCost:
                UpgradeCostDiscount = Mathf.Max(0.1f, costDiscount); 
                UpdateUpgradeUI(); 
                break;
        }
    }

    protected virtual void RemoveManagerBuff()
    {
        UpgradeCostDiscount = 1f;
        UpdateUpgradeUI();
    }

    protected virtual void UpdateWorldSkillUI()
    {
        if (worldSkillButton == null || worldSkillTimerText == null) return;

        if (currentManager == null)
        {
            worldSkillTimerText.gameObject.SetActive(false);
            return;
        }

        if (currentManager.IsSkillActive())
        {
            worldSkillButton.interactable = false;
            worldSkillTimerText.gameObject.SetActive(true);
            worldSkillTimerText.text = $"{Mathf.CeilToInt(currentManager.GetRemainingSkillTime())}s";
            worldSkillTimerText.color = Color.green;
        }
        else if (currentManager.IsOnCooldown())
        {
            worldSkillButton.interactable = false;
            worldSkillTimerText.gameObject.SetActive(true);
            worldSkillTimerText.text = $"{Mathf.CeilToInt(currentManager.GetRemainingCooldownTime())}s";
            worldSkillTimerText.color = Color.red;
        }
        else
        {
            worldSkillButton.interactable = true;
            worldSkillTimerText.gameObject.SetActive(false); 
        }
    }

    public void OpenManagerModal()
    {
        Debug.Log("Đang gọi lệnh mở Modal Quản lý...");
        ManagerModalUI modal = ManagerModalUI.Instance;
        if (modal == null)
        {
            modal = FindObjectOfType<ManagerModalUI>(true);
        }

        if (modal != null)
        {
            modal.gameObject.SetActive(true); 
            modal.OpenModal(this);
            Debug.Log("Đã bật Modal Quản lý thành công.");
        }
        else
        {
            Debug.LogError("Chưa kéo ManagerModalUI vào scene hoặc đã bị xóa!");
        }
    }

    // =====================================
    // LƯU TRỮ VÀ TẢI DỮ LIỆU (SAVE/LOAD)
    // =====================================
    public virtual FacilitySaveData SaveState()
    {
        FacilitySaveData data = new FacilitySaveData();
        data.Level = this.Level;
        data.ActiveManagerID = this.currentManager != null ? this.currentManager.Id : "";
        return data;
    }

    public virtual void LoadState(FacilitySaveData data)
    {
        if (data == null) return;
        this.Level = data.Level == 0 ? 1 : data.Level;
        
        // Khôi phục Manager bằng cách gọi đúng hàm AssignManager để nó spawn UI và Visual
        if (!string.IsNullOrEmpty(data.ActiveManagerID) && ManagerController.Instance != null)
        {
            ManagerData foundManager = ManagerController.Instance.OwnedManagers.Find(m => m.Id == data.ActiveManagerID);
            if (foundManager != null)
            {
                AssignManager(foundManager);
            }
        }

        // Bắt buộc gọi OnUpgraded để update lại các chỉ số dựa trên Level mới nạp
        OnUpgraded();
        UpdateUpgradeUI();
    }

    public virtual void PopulateSaveData(SaveData data)
    {
        // Override by children to populate specific data list/field in SaveData
    }

    public virtual void LoadFromSaveData(SaveData data)
    {
        // Override by children to read from specific data list/field in SaveData
    }
}
