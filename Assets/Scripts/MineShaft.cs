using UnityEngine;
using TMPro; // Thêm thư viện này để dùng TextMeshPro
using System.Collections.Generic; // Thêm thư viện dùng List
using DG.Tweening; // Thư viện tạo hiệu ứng DOTween

// Kế thừa Facility thay vì MonoBehaviour để có sẵn tính năng Nâng cấp
public class MineShaft : Facility 
{
    public double CurrentResource = 0; 
    
    public double BaseResourcePerSecond = 10; 
    
    [Header("Miner Settings")]
    public int MaxMiners = 5;
    public float MaxMoveSpeed = 5f;
    public float MinDigTime = 0.5f;

    [Header("Miner Spawn Settings")]
    public Miner minerPrefab;
    public Transform minerStartPos;
    public Transform minerDigPos;
    public float spawnOffsetX = 0.5f;
    private List<Miner> activeMiners = new List<Miner>();

    [Header("Manager Settings")]
    public SimpleSpriteAnimator WorldManagerAnimator; // Kéo object nhân vật có sẵn ngoài hầm vào đây
    public Transform EmptyManagerLine; // Kéo ảnh "Line" (chưa có quản lý) vào đây để làm hiệu ứng nhịp đập


    public ManagerData currentManager;
    public bool IsSkillActive = false;
    public float SkillTimer = 0f;
    public float CooldownTimer = 0f;


    // Các buff này sẽ nằm chung dưới thẻ Manager Buffs của lớp cha Facility
    public float MinerMoveSpeedBuff = 1f;
    public float MinerDigSpeedBuff = 1f;
    public float ProductivityBuff = 1f;

    // Năng suất của một thợ mỏ
    public double ResourcePerSecond => GetWorkerProductivity(Level); 

    public int GetMinersCount(int targetLevel)
    {
        // Mỗi 10 cấp thêm 1 thợ, tối đa là MaxMiners
        int count = 1 + (targetLevel / 10);
        return Mathf.Min(count, MaxMiners);
    }

    public float GetMinerMoveSpeed(int targetLevel)
    {
        float speed = Config != null ? Config.BaseSpeed + (targetLevel * 0.05f) : 2f;
        speed = Mathf.Min(speed, MaxMoveSpeed);
        return speed * MinerMoveSpeedBuff;
    }

    public float GetMinerDigTime(int targetLevel)
    {
        float digTime = 2f - (targetLevel * 0.01f);
        digTime = Mathf.Max(digTime, MinDigTime);
        return digTime / MinerDigSpeedBuff;
    }

    public double GetWorkerProductivity(int targetLevel)
    {
        return (BaseResourcePerSecond * targetLevel) * ProductivityBuff;
    }

    public double GetTotalExtractionPerSecond(int targetLevel)
    {
        // Tổng lượng đào = Năng suất 1 thợ * Số lượng thợ
        return GetWorkerProductivity(targetLevel) * GetMinersCount(targetLevel);
    }

    [Header("UI")]
    public TextMeshProUGUI shaftCashText; // Hiển thị số tiền/tài nguyên hiện tại của hầm
    
    [Header("World Space Manager UI")]
    public UnityEngine.UI.Button worldSkillButton;
    public TextMeshProUGUI worldSkillTimerText;
    public UnityEngine.UI.Image worldSkillIconImage; // Ảnh Icon của kỹ năng

    private Sprite defaultManagerSprite; // Lưu lại ảnh viền gốc

    protected override void Start()
    {
        base.Start(); // Gọi hàm Start của lớp cha (Facility) để update text Level
        
        // Sửa lỗi: Unity tự động khởi tạo class [Serializable] làm hầm bị kẹt một quản lý "ảo" từ đầu
        currentManager = null; 

        // Lưu lại ảnh viền ban đầu
        if (WorldManagerAnimator != null)
        {
            var img = WorldManagerAnimator.GetComponent<UnityEngine.UI.Image>();
            if (img != null) defaultManagerSprite = img.sprite;
            else
            {
                var sr = WorldManagerAnimator.GetComponent<SpriteRenderer>();
                if (sr != null) defaultManagerSprite = sr.sprite;
            }
        }

        UpdateUI();

        // Đăng ký sự kiện cho nút Kích hoạt kỹ năng WorldSpace
        if (worldSkillButton != null)
        {
            worldSkillButton.onClick.AddListener(ActivateManagerSkill);
            UpdateWorldSkillUI(); // Cập nhật hiển thị ban đầu
        }

        // Gọi hàm này để nó set trạng thái DOTween đập nhịp và tắt quản lý ảo lúc mới vào game
        SpawnManagerVisual();
        
        if (worldSkillIconImage == null)
        {
            Debug.LogWarning("Chú ý: Hầm mỏ chưa được kéo Ảnh Icon Kỹ Năng vào ô 'World Skill Icon Image' trong Inspector!");
        }
    }

    private void Update()
    {
        if (currentManager == null) 
        {
            if (worldSkillButton != null && worldSkillButton.gameObject.activeSelf)
                worldSkillButton.gameObject.SetActive(false); // Ẩn nút nếu không có quản lý
            return;
        }
        else
        {
            if (worldSkillButton != null && !worldSkillButton.gameObject.activeSelf)
                worldSkillButton.gameObject.SetActive(true); // Hiện nút nếu có quản lý
        }

        if (IsSkillActive)
        {
            SkillTimer -= Time.deltaTime;
            if (SkillTimer <= 0)
            {
                IsSkillActive = false;
                CooldownTimer = currentManager.CooldownDuration;
                RemoveManagerBuff();
            }
            UpdateWorldSkillUI();
        }
        else if (CooldownTimer > 0)
        {
            CooldownTimer -= Time.deltaTime;
            UpdateWorldSkillUI();
        }
    }

    private void UpdateWorldSkillUI()
    {
        if (worldSkillButton == null || worldSkillTimerText == null) return;

        // Nếu không có quản lý, tắt luôn text
        if (currentManager == null)
        {
            worldSkillTimerText.gameObject.SetActive(false);
            return;
        }

        if (IsSkillActive)
        {
            worldSkillButton.interactable = false;
            worldSkillTimerText.gameObject.SetActive(true);
            worldSkillTimerText.text = $"{Mathf.CeilToInt(SkillTimer)}s";
        }
        else if (CooldownTimer > 0)
        {
            worldSkillButton.interactable = false;
            worldSkillTimerText.gameObject.SetActive(true);
            worldSkillTimerText.text = $"{Mathf.CeilToInt(CooldownTimer)}s";
        }
        else
        {
            worldSkillButton.interactable = true;
            worldSkillTimerText.gameObject.SetActive(false); // Ẩn Text đi khi sẵn sàng
        }
    }

    public void AssignManager(ManagerData manager)
    {
        currentManager = manager;
        IsSkillActive = false;
        SkillTimer = 0f;
        CooldownTimer = 0f;
        
        SpawnManagerVisual();
        
        RemoveManagerBuff();
        UpdateWorldSkillUI();
    }

    public void RemoveManager()
    {
        currentManager = null;
        IsSkillActive = false;
        SkillTimer = 0f;
        CooldownTimer = 0f;
        
        SpawnManagerVisual(); // Sẽ tắt hiển thị nhân vật vì currentManager = null
        
        RemoveManagerBuff();
        UpdateWorldSkillUI();
    }

    private void SpawnManagerVisual()
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
                    WorldManagerAnimator.enabled = true; // Bật animation
                    WorldManagerAnimator.frames = charVis.AnimationFrames;
                    
                    var sr = WorldManagerAnimator.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.sprite = charVis.AnimationFrames[0];
                    var img = WorldManagerAnimator.GetComponent<UnityEngine.UI.Image>();
                    if (img != null) img.sprite = charVis.AnimationFrames[0];
                }
                
                // Tắt hiệu ứng nhịp đập vì đã có quản lý
                WorldManagerAnimator.transform.DOKill();
                WorldManagerAnimator.transform.localScale = Vector3.one;
            }
            
            // Đổi Icon nút Kỹ năng ngoài World
            if (worldSkillIconImage != null)
            {
                worldSkillIconImage.sprite = ManagerController.Instance.Config.GetSkillIcon(currentManager.BuffType);
            }
        }
        else
        {
            if (WorldManagerAnimator != null)
            {
                WorldManagerAnimator.gameObject.SetActive(true); // Vẫn bật object để hiển thị viền
                WorldManagerAnimator.enabled = false; // Tắt script animation để khỏi đè hình

                // Khôi phục lại ảnh viền gốc ban đầu (nếu lưu được lúc Start)
                var sr = WorldManagerAnimator.GetComponent<SpriteRenderer>();
                if (sr != null && defaultManagerSprite != null) sr.sprite = defaultManagerSprite;
                var img = WorldManagerAnimator.GetComponent<UnityEngine.UI.Image>();
                if (img != null && defaultManagerSprite != null) img.sprite = defaultManagerSprite;

                // Bật hiệu ứng nhịp đập cho ảnh viền
                WorldManagerAnimator.transform.DOKill();
                WorldManagerAnimator.transform.localScale = Vector3.one;
                WorldManagerAnimator.transform.DOScale(1.1f, 0.5f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
        }
    }

    public void ActivateManagerSkill()
    {
        if (currentManager != null && !IsSkillActive && CooldownTimer <= 0)
        {
            IsSkillActive = true;
            SkillTimer = currentManager.BuffDuration;
            ApplyManagerBuff();
            UpdateWorldSkillUI();
        }
    }

    private void ApplyManagerBuff()
    {
        if (currentManager == null) return;
        
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
            case ManagerBuffType.ReduceCost:
                UpgradeCostDiscount = Mathf.Max(0.1f, costDiscount); // Giảm tối đa 90%
                UpdateUpgradeUI(); // Update UI cost
                break;
        }
    }

    private void RemoveManagerBuff()
    {
        MinerMoveSpeedBuff = 1f;
        MinerDigSpeedBuff = 1f;
        UpgradeCostDiscount = 1f;
        UpdateUpgradeUI();
    }

    // Hàm này được gọi bởi con thợ mỏ sau khi nó đào xong
    public void AddResource(double amount)
    {
        CurrentResource += amount;
        UpdateUI();
    }

    public double TakeResource(double amountToTake)
    {
        double taken = 0;
        if (amountToTake > CurrentResource)
        {
            taken = CurrentResource;
            CurrentResource = 0;
        }
        else
        {
            CurrentResource -= amountToTake;
            taken = amountToTake;
        }
        
        UpdateUI(); // Cập nhật lại UI sau khi thang máy lấy đi
        return taken;
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

    private void CheckAndSpawnMiners()
    {
        if (minerPrefab == null || minerStartPos == null || minerDigPos == null) return;

        int targetCount = GetMinersCount(Level);

        // Hầm luôn mặc định có sẵn 1 người (bản gốc bạn tự đặt), nên chỉ đẻ thêm phần dư
        int requiredSpawns = targetCount - 1;

        while (activeMiners.Count < requiredSpawns)
        {
            // Đánh số thứ tự bắt đầu từ 1 để nó lùi về sau lưng con gốc
            int index = activeMiners.Count + 1; 
            
            // Tính toán vị trí lùi về sau (bên trái) theo X
            Vector3 spawnPos = minerStartPos.position - new Vector3(spawnOffsetX * index, 0, 0);

            Miner newMiner = Instantiate(minerPrefab, spawnPos, Quaternion.identity, transform);
            newMiner.currentShaft = this;
            
            // Gán lại startPos ảo cho thợ mỏ này bằng một object rỗng tạo ra tại chỗ
            GameObject tempStart = new GameObject($"StartPos_Miner_{index}");
            tempStart.transform.position = spawnPos;
            tempStart.transform.SetParent(transform);

            newMiner.startPos = tempStart.transform;
            newMiner.digPos = minerDigPos;

            activeMiners.Add(newMiner);
        }
    }

    public override (string curVal, string nextVal) GetStatDisplay(int statIndex, int currentLevel, int nextLevel)
    {
        string curVal = "0";
        string nextVal = "0";
        
        switch (statIndex)
        {
            case 0: // Tổng khai thác
                curVal = GetTotalExtractionPerSecond(currentLevel).ToString("F1") + "/s";
                nextVal = GetTotalExtractionPerSecond(nextLevel).ToString("F1") + "/s";
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
                curVal = GetWorkerProductivity(currentLevel).ToString("F1") + "/s";
                nextVal = GetWorkerProductivity(nextLevel).ToString("F1") + "/s";
                break;
        }

        return (curVal, nextVal);
    }

    // Hàm gọi từ Button UI của Hầm mỏ để mở bảng Quản lý
    public void OpenManagerModal()
    {
        Debug.Log("Đang gọi lệnh mở Modal Quản lý...");
        // Khắc phục lỗi nếu UI Modal bị tắt (Inactive) từ đầu khiến Awake không chạy được
        ManagerModalUI modal = ManagerModalUI.Instance;
        if (modal == null)
        {
            modal = FindObjectOfType<ManagerModalUI>(true);
        }

        if (modal != null)
        {
            modal.gameObject.SetActive(true); // Bật GameObject lên trước
            modal.OpenModal(this);
            Debug.Log("Đã bật Modal Quản lý thành công.");
        }
        else
        {
            Debug.LogError("Chưa kéo ManagerModalUI vào scene hoặc đã bị xóa!");
        }
    }
}
