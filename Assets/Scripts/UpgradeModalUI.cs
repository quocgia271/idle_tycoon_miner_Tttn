using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening; // Import DOTween

public class UpgradeModalUI : MonoBehaviour
{
    public static UpgradeModalUI Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [Header("Top Info")]
    [Tooltip("Kéo thả cái Panel nhỏ chứa toàn bộ giao diện bảng (bỏ qua nền đen) vào đây để làm hiệu ứng rung")]
    public RectTransform ModalPanel; 
    public TextMeshProUGUI FacilityNameAndLevelText;
    public Image FacilityIcon;
    public TextMeshProUGUI UpgradeCostText;
    public TextMeshProUGUI NextLevelText; // Kéo text hiển thị Cấp độ tiếp theo vào đây

    [Header("Middle Stats (Scroll View Content)")]
    [Tooltip("Kéo thả prefab của 1 cái Item (chứa script UpgradeStatItemUI) vào đây")]
    public GameObject StatItemPrefab; 
    [Tooltip("Kéo thả object 'Content' của Scroll View vào đây")]
    public Transform StatsContentParent; 
    private List<UpgradeStatItemUI> spawnedStats = new List<UpgradeStatItemUI>();

    [Header("Bottom Buttons")]
    public Button BtnX1;
    public Button BtnX10;
    public Button BtnX50;
    public Button BtnMax;
    public Button BtnUpgradeConfirm;

    [Header("Button Visuals")]
    public Sprite NormalBtnSprite; // Ảnh lúc nút bình thường
    public Sprite SelectedBtnSprite; // Ảnh lúc nút đang được chọn

    private Facility currentFacility;
    private FacilityConfigSO currentConfig;
    private int upgradeMultiplier = 1;
    private int currentTabIndex = 0; // 0:x1, 1:x10, 2:x50, 3:MAX
    private double currentTotalCost = 0; // Lưu tạm tổng tiền để check khi bấm nút

    public void ShowModal(Facility facility, FacilityConfigSO config)
    {
        currentFacility = facility;
        currentConfig = config;
        upgradeMultiplier = 1;
        
        gameObject.SetActive(true);
        SetMultiplier(1); // Mặc định mở lên là mua x1 và bôi sáng nút x1

        if (ModalPanel != null)
        {
            ModalPanel.DOKill(); // Ngắt animation cũ (nếu có)
            ModalPanel.localScale = Vector3.zero; // Thu nhỏ về 0
            ModalPanel.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true); // Tốc độ vừa phải, mượt mà (0.25s)
        }
    }

    public void CloseModal()
    {
        if (ModalPanel != null)
        {
            ModalPanel.DOKill();
            // Thu nhỏ lại cũng mượt hơn chút (0.15s)
            ModalPanel.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => 
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // Gắn vào các nút x1, x10, x50 trên UI
    public void SetMultiplier(int multi)
    {
        upgradeMultiplier = multi;

        // Đổi hình ảnh nút
        if (multi == 1) currentTabIndex = 0;
        else if (multi == 10) currentTabIndex = 1;
        else if (multi == 50) currentTabIndex = 2;

        UpdateButtonVisuals(currentTabIndex);
        RefreshUI();
    }

    // Gắn vào nút MAX trên UI
    public void SetMaxMultiplier()
    {
        currentTabIndex = 3; // 3 là index của nút MAX
        UpdateButtonVisuals(currentTabIndex); 
        RefreshUI();
    }

    // Hàm cập nhật hình ảnh nút đang được chọn
    private void UpdateButtonVisuals(int selectedIndex)
    {
        if (SelectedBtnSprite == null) return; // Chỉ bắt buộc phải có ảnh Selected
        
        SetButtonVisual(BtnX1, selectedIndex == 0);
        SetButtonVisual(BtnX10, selectedIndex == 1);
        SetButtonVisual(BtnX50, selectedIndex == 2);
        SetButtonVisual(BtnMax, selectedIndex == 3);
    }

    private void SetButtonVisual(Button btn, bool isSelected)
    {
        if (btn == null || btn.image == null) return;

        if (isSelected)
        {
            btn.image.sprite = SelectedBtnSprite;
            btn.image.color = Color.white; // Hiện hình rõ ràng
        }
        else
        {
            btn.image.sprite = NormalBtnSprite; // Có thể gán thành None (null)
            
            // Nếu bạn không cài ảnh Normal, Unity sẽ tự vẽ 1 cục màu trắng tinh. 
            // Do đó phải chỉnh Alpha (độ mờ) về 0 để nó tàng hình luôn.
            if (NormalBtnSprite == null)
            {
                btn.image.color = new Color(1f, 1f, 1f, 0f); // Trong suốt (Tàng hình)
            }
            else
            {
                btn.image.color = Color.white; // Bình thường
            }
        }
    }

    private void RefreshUI()
    {
        if (currentFacility == null || currentConfig == null) return;

        // Nếu đang ở tab MAX, bắt buộc phải tính lại số Level liên tục dựa theo tiền hiện có
        if (currentTabIndex == 3)
        {
            if (Gamemanager.Instance != null)
                upgradeMultiplier = MathHelper.CalculateMaxLevel(Gamemanager.Instance.IdleCash, currentConfig.BaseCost, currentConfig.CostMultiplier, currentFacility.Level);
            else
                upgradeMultiplier = 1;
        }

        int currentLevel = currentFacility.Level;
        int nextLevel = currentLevel + upgradeMultiplier;
        
        if (FacilityNameAndLevelText != null)
            FacilityNameAndLevelText.text = $"{currentConfig.FacilityName} Cấp {currentLevel}";
            
        if (NextLevelText != null)
            NextLevelText.text = $"Cấp {nextLevel}"; // Hiển thị cấp độ mà bạn sẽ đạt được
            
        if (FacilityIcon != null)
            FacilityIcon.sprite = currentConfig.FacilityIcon;
        
        currentTotalCost = 0;
        for (int i = 0; i < upgradeMultiplier; i++)
        {
            double costForNextLevel = MathHelper.CalculateUpgradeCost(currentConfig.BaseCost, currentConfig.CostMultiplier, currentLevel + i);
            currentTotalCost += costForNextLevel * currentFacility.UpgradeCostDiscount;
        }
        
        if (UpgradeCostText != null)
        {
            UpgradeCostText.text = CurrencyFormatter.FormatMoney(currentTotalCost);
            
            // Đổi sang màu đỏ nếu không đủ tiền
            bool canAfford = Gamemanager.Instance != null && Gamemanager.Instance.IdleCash >= currentTotalCost;
            UpgradeCostText.color = canAfford ? Color.white : Color.red; 
        }

        // --- CẬP NHẬT DANH SÁCH CHỈ SỐ (MỞ RỘNG ĐƯỢC) ---
        
        // 1. Tạo thêm UI Item nếu bị thiếu so với List trong SO
        while (spawnedStats.Count < currentConfig.StatNames.Count)
        {
            GameObject obj = Instantiate(StatItemPrefab, StatsContentParent);
            obj.transform.localScale = Vector3.one; // Sửa lỗi bị phóng to/nhỏ khi spawn
            spawnedStats.Add(obj.GetComponent<UpgradeStatItemUI>());
        }

        // 2. Ẩn những UI Item bị thừa (Nếu SO chỉ có 2 chỉ số mà đã sinh 3 item)
        for (int i = 0; i < spawnedStats.Count; i++)
        {
            spawnedStats[i].gameObject.SetActive(i < currentConfig.StatNames.Count);
        }

        // 3. Đổ dữ liệu thật từ Logic của Facility vào UI
        for (int i = 0; i < currentConfig.StatNames.Count; i++)
        {
            string statName = currentConfig.StatNames[i];
            var (curVal, nextVal) = currentFacility.GetStatDisplay(i, currentLevel, nextLevel);
            spawnedStats[i].Setup(statName, curVal, nextVal);
        }
    }

    public void OnConfirmUpgradeClicked()
    {
        if (Gamemanager.Instance != null && Gamemanager.Instance.IdleCash >= currentTotalCost)
        {
            // Đủ tiền -> Mua
            for(int i = 0; i < upgradeMultiplier; i++)
            {
                currentFacility.TryUpgrade();
            }
            RefreshUI(); 
        }
        else
        {
            // Không đủ tiền -> Lắc cái Panel bên trong (không lắc nền đen)
            if (ModalPanel != null)
            {
                ModalPanel.DOComplete(); // Dừng các hiệu ứng lắc trước đó nếu có
                ModalPanel.DOShakePosition(0.3f, new Vector3(15, 0, 0), 10, 0, false, true);
            }
            else
            {
                transform.DOComplete();
                transform.DOShakePosition(0.3f, new Vector3(15, 0, 0), 10, 0, false, true);
            }
        }
    }
}
