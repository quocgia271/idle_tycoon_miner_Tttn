using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class ManagerModalUI : MonoBehaviour
{
    [Header("Top Section (Assigned Manager)")]
    public ManagerConfigSO Config;
    public RectTransform ModalPanel; // Để làm hiệu ứng bật lên
    public TextMeshProUGUI ShaftTitleText;
    public GameObject AssignedManagerPanel; // Bật tắt cả cụm thông tin
    public SimpleSpriteAnimator TopAvatarAnimator; // Nếu có thì dùng Animator
    public Image TopAvatarImage;
    public Image TopSkillIcon; // Icon skill trên top
    public TextMeshProUGUI TopNameText;
    public TextMeshProUGUI TopRarityText;
    public TextMeshProUGUI TopBuffDescText;
    public TextMeshProUGUI TopDurationText; // Hiển thị thời gian đếm ngược hoặc thời gian gốc
    public Button UnassignButton;

    [Header("Sell Modal")]
    public SellManagerModalUI SellModal;

    [Header("Scroll View (Inventory)")]
    public Transform InventoryContent;
    public ManagerItemUI ItemPrefab;

    [Header("Bottom Section")]
    public TextMeshProUGUI HirePriceText;
    public TextMeshProUGUI PityCountdownText; // Text đếm ngược 10 lần
    public Button HireButton;
    public Button FilterMiningBtn;
    public Button FilterMoveBtn;
    public Button FilterCostBtn;
    public Button FilterAllBtn; // Nút hiện tất cả (nếu có)

    [Header("Filter Visuals")]
    public Sprite FilterNormalSprite; 
    public Sprite FilterSelectedSprite;

    public static ManagerModalUI Instance { get; private set; }

    private MineShaft currentShaft;
    private ManagerBuffType? currentFilter = null;

    private void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Chú ý: Đang có 2 bảng ManagerModalUI trong Scene! Có thể bạn đã kéo script này vào 2 object khác nhau.");
        }
    }

    private void Start()
    {
        HireButton.onClick.AddListener(OnHireButtonClicked);
        UnassignButton.onClick.AddListener(OnUnassignClicked);
        
        if (FilterMiningBtn) FilterMiningBtn.onClick.AddListener(() => SetFilter(ManagerBuffType.MiningSpeed));
        if (FilterMoveBtn) FilterMoveBtn.onClick.AddListener(() => SetFilter(ManagerBuffType.MoveSpeed));
        if (FilterCostBtn) FilterCostBtn.onClick.AddListener(() => SetFilter(ManagerBuffType.ReduceCost));
        if (FilterAllBtn) FilterAllBtn.onClick.AddListener(() => SetFilter(null));

        if (ManagerController.Instance != null)
        {
            ManagerController.Instance.OnManagerListUpdated += RefreshInventory;
            ManagerController.Instance.OnPityUpdated += RefreshPityText;
        }

        // Cập nhật trạng thái ảnh ban đầu cho Filter
        UpdateFilterButtonVisuals();
        RefreshPityText();
    }

    // Đã xóa hàm Update vì Modal không cần đếm ngược thời gian (đếm ngược chỉ ở WorldSpace)
    public void OpenModal(MineShaft shaft)
    {
        Debug.Log($"Bắt đầu OpenModal cho hầm: {shaft.gameObject.name}");
        currentShaft = shaft;
        gameObject.SetActive(true);

        if (ShaftTitleText != null)
        {
            ShaftTitleText.text = $"Quản lý {shaft.gameObject.name}";
        }
        else
        {
            Debug.LogWarning("Bạn quên chưa kéo UI Text vào ô 'Shaft Title Text' trong Inspector rồi!");
        }

        // --- BẬT HIỆU ỨNG DOTWEEN ---
        if (ModalPanel != null)
        {
            Debug.Log("Chạy animation bật Modal...");
            ModalPanel.DOKill(); // Ngắt animation cũ (nếu có)
            ModalPanel.localScale = Vector3.zero; // Thu nhỏ về 0
            ModalPanel.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true); // Phóng to mượt
        }
        else
        {
            Debug.Log("Không có ModalPanel, hiển thị bình thường không có animation.");
        }

        RefreshTopPanel();
        RefreshBottomPanel();
        RefreshInventory();
    }

    public void CloseModal()
    {
        Debug.Log("Nút CloseModal đã được bấm!");
        if (ModalPanel != null)
        {
            Debug.Log("Chạy animation tắt Modal...");
            ModalPanel.DOKill();
            // Thu nhỏ lại mượt mà rồi mới tắt gameObject
            ModalPanel.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => 
            {
                Debug.Log("Tắt gameObject sau khi chạy xong animation.");
                gameObject.SetActive(false);
                currentShaft = null;
            });
        }
        else
        {
            Debug.Log("Tắt gameObject ngay lập tức (không có ModalPanel).");
            gameObject.SetActive(false);
            currentShaft = null;
        }
    }

    private void RefreshTopPanel()
    {
        if (currentShaft.currentManager != null)
        {
            AssignedManagerPanel.SetActive(true);

            ManagerData md = currentShaft.currentManager;
            TopNameText.text = md.Name;

            if (Config != null)
            {
                var charVis = Config.GetCharacterVisual(md.CharacterID);
                if (charVis != null)
                {
                    if (TopAvatarAnimator != null)
                        TopAvatarAnimator.frames = charVis.AnimationFrames;
                    if (TopAvatarImage != null && charVis.AvatarStatic != null)
                        TopAvatarImage.sprite = charVis.AvatarStatic;
                }

                if (TopSkillIcon != null)
                {
                    TopSkillIcon.sprite = Config.GetSkillIcon(md.BuffType);
                }
            }
            
            switch (md.Rarity)
            {
                case ManagerRarity.Junior: TopRarityText.text = "Trẻ tuổi"; break;
                case ManagerRarity.Director: TopRarityText.text = "Giám đốc"; break;
                case ManagerRarity.Senior: TopRarityText.text = $"Cấp cao_{md.SupportType}"; break;
            }

            string buffDesc = "";
            switch (md.BuffType)
            {
                case ManagerBuffType.MiningSpeed: buffDesc = $"Tăng tốc đào: +{md.BuffValue:F1}%"; break;
                case ManagerBuffType.MoveSpeed: buffDesc = $"Tốc di chuyển: +{md.BuffValue:F1}%"; break;
                case ManagerBuffType.ReduceCost: buffDesc = $"Giảm chi phí: {md.BuffValue:F1}%"; break;
            }
            TopBuffDescText.text = buffDesc;
            string durationStr = md.BuffDuration >= 60 ? (md.BuffDuration / 60f).ToString("0.##") + " phút" : md.BuffDuration + "s";
            TopDurationText.text = durationStr;
        }
        else
        {
            AssignedManagerPanel.SetActive(false);
        }
    }

    private void RefreshBottomPanel()
    {
        if (ManagerController.Instance != null)
        {
            double price = ManagerController.Instance.GetCurrentHireCost();
            HirePriceText.text = "Thuê: " + CurrencyFormatter.FormatMoney(price);
            
            // Có thể làm mờ nút thuê nếu không đủ tiền
            if (Gamemanager.Instance != null)
            {
                HireButton.interactable = Gamemanager.Instance.IdleCash >= price;
            }
        }
    }

    private void RefreshPityText()
    {
        if (PityCountdownText != null && ManagerController.Instance != null)
        {
            PityCountdownText.text = $"Quay {ManagerController.Instance.HiresUntilPity} lần nữa chắc chắn nhận Cấp Cao!";
        }
    }

    private void RefreshInventory()
    {
        // BẢO VỆ: Chống kéo nhầm làm xóa cả Modal
        if (InventoryContent == null || InventoryContent == transform || InventoryContent == transform.parent)
        {
            Debug.LogError("LỖI: Ô 'Inventory Content' đang bị kéo nhầm thành Modal hoặc Parent! Phải kéo cục 'Content' của Scroll View vào đây.");
            return;
        }

        // Xóa danh sách cũ
        foreach (Transform child in InventoryContent)
        {
            Destroy(child.gameObject);
        }

        if (ManagerController.Instance == null) return;

        List<ManagerData> managers = currentFilter.HasValue 
            ? ManagerController.Instance.GetManagersByFilter(currentFilter.Value) 
            : ManagerController.Instance.OwnedManagers;

        foreach (var md in managers)
        {
            // Tùy chọn: Ẩn bớt các quản lý đã được gán cho hầm KHI ĐÓ KHÔNG PHẢI LÀ HẦM HIỆN TẠI
            // Ở đây mình cứ hiện hết để họ có thể xem
            ManagerItemUI item = Instantiate(ItemPrefab, InventoryContent);
            item.Setup(md, this);
        }
    }

    private void SetFilter(ManagerBuffType? filterType)
    {
        currentFilter = filterType;
        UpdateFilterButtonVisuals();
        RefreshInventory();
    }

    private void UpdateFilterButtonVisuals()
    {
        SetFilterButtonVisual(FilterMiningBtn, currentFilter == ManagerBuffType.MiningSpeed);
        SetFilterButtonVisual(FilterMoveBtn, currentFilter == ManagerBuffType.MoveSpeed);
        SetFilterButtonVisual(FilterCostBtn, currentFilter == ManagerBuffType.ReduceCost);
        SetFilterButtonVisual(FilterAllBtn, currentFilter == null);
    }

    private void SetFilterButtonVisual(Button btn, bool isSelected)
    {
        if (btn == null || btn.image == null) return;
        
        if (isSelected)
        {
            if (FilterSelectedSprite != null) btn.image.sprite = FilterSelectedSprite;
            btn.image.color = Color.white;
        }
        else
        {
            if (FilterNormalSprite != null) 
            {
                btn.image.sprite = FilterNormalSprite;
                btn.image.color = Color.white;
            }
            else 
            {
                // Tắt tàng hình ảnh nếu không dùng ảnh cho nút bình thường
                btn.image.color = new Color(1f, 1f, 1f, 0f); 
            }
        }
    }

    public void OnAssignManager(ManagerData manager)
    {
        if (currentShaft != null)
        {
            // Nếu hầm đang có quản lý thì phải tháo ra trước
            if (currentShaft.currentManager != null)
            {
                currentShaft.currentManager.IsAssigned = false;
                currentShaft.RemoveManager();
            }

            // Gán quản lý mới
            manager.IsAssigned = true;
            // TODO: Gán AssignedShaftId nếu MineShaft có ID
            currentShaft.AssignManager(manager);

            RefreshTopPanel();
            RefreshInventory(); // Cập nhật lại nút Gán bị mờ
        }
    }

    public void OnUnassignClicked()
    {
        if (currentShaft != null && currentShaft.currentManager != null)
        {
            currentShaft.currentManager.IsAssigned = false;
            currentShaft.RemoveManager();
            
            RefreshTopPanel();
            RefreshInventory();
        }
    }

    public void OnSellManager(ManagerData manager, GameObject itemObj)
    {
        Debug.Log($"OnSellManager called for {manager.Name}. SellModal is {(SellModal != null ? "Assigned" : "NULL")}");
        if (SellModal != null)
        {
            SellModal.Setup(manager, this);
        }
        else
        {
            ExecuteSellManager(manager);
        }
    }

    public void ExecuteSellManager(ManagerData manager)
    {
        Debug.Log($"ExecuteSellManager called for {manager.Name}");
        // Nếu quản lý này đang được gán ở hầm hiện tại, phải tháo ra
        if (currentShaft != null && currentShaft.currentManager == manager)
        {
            OnUnassignClicked();
        }

        ManagerController.Instance.SellManager(manager);
        RefreshBottomPanel(); // Cập nhật có thể bấm đc Hire ko (vì vừa cộng tiền)
        RefreshInventory();   // Refresh danh sách sau khi bán
        Debug.Log("ExecuteSellManager completed!");
    }

    private void OnHireButtonClicked()
    {
        if (ManagerController.Instance.HireManager())
        {
            RefreshBottomPanel(); // Cập nhật giá thuê mới
        }
    }

    private void OnDestroy()
    {
        if (ManagerController.Instance != null)
        {
            ManagerController.Instance.OnManagerListUpdated -= RefreshInventory;
            ManagerController.Instance.OnPityUpdated -= RefreshPityText;
        }
    }
}
