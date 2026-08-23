using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening; // Thêm DOTween cho animation

public class MoraleModalUI : MonoBehaviour
{
    public static MoraleModalUI Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [Header("Animation")]
    public RectTransform modalPanel; // Kéo Panel con (chứa nền bảng) vào đây để làm hiệu ứng bung ra

    [Header("UI References")]
    public TextMeshProUGUI shaftTitleText;
    public Transform contentParent;
    public GameObject moraleItemPrefab;
    
    [Header("Buttons")]
    public Button btnAdd25;
    public Button btnMaxOne;
    public Button btnMaxAll;
    
    [Header("Button Visuals")]
    public Sprite normalBtnSprite; 
    public Sprite selectedBtnSprite;
    
    [Header("Payment")]
    public TextMeshProUGUI costText;
    public Button btnPay;
    
    private MineShaft currentShaft;
    private Miner selectedMiner;
    private int selectedOption = 0; // 0: +25, 1: Max 1, 2: Max All
    private double currentCost = 0;
    
    [Header("Endurance UI")]
    public TextMeshProUGUI enduranceText;
    public Image enduranceFillImage;
    
    [Header("Endurance Purchase UI")]
    public TextMeshProUGUI enduranceCostText;
    public Button btnEnduranceAdd25;
    public Button btnEnduranceMax;
    public Button btnEndurancePay;

    [Header("Endurance Button Visuals")]
    public Sprite normalEnduranceBtnSprite;
    public Sprite selectedEnduranceBtnSprite;

    private int selectedEnduranceOption = 0; // 0: +25, 1: Max
    private double currentEnduranceCost = 0;

    private List<MoraleItemUI> spawnedItems = new List<MoraleItemUI>();

    private void Start()
    {
        if (btnAdd25 != null) btnAdd25.onClick.AddListener(() => SelectOption(0));
        if (btnMaxOne != null) btnMaxOne.onClick.AddListener(() => SelectOption(1));
        if (btnMaxAll != null) btnMaxAll.onClick.AddListener(() => SelectOption(2));
        if (btnPay != null) btnPay.onClick.AddListener(OnPayClicked);
        
        if (btnEnduranceAdd25 != null) btnEnduranceAdd25.onClick.AddListener(() => SelectEnduranceOption(0));
        if (btnEnduranceMax != null) btnEnduranceMax.onClick.AddListener(() => SelectEnduranceOption(1));
        if (btnEndurancePay != null) btnEndurancePay.onClick.AddListener(OnEndurancePayClicked);
        // Xóa dòng gameObject.SetActive(false) ở đây để không bị lỗi click 2 lần nữa
    }

    public void ShowModal(MineShaft shaft)
    {
        currentShaft = shaft;
        selectedMiner = null;
        
        if (shaft.activeMiners != null && shaft.activeMiners.Count > 0)
        {
            selectedMiner = shaft.activeMiners[0];
        }
        
        if (shaftTitleText != null)
        {
            shaftTitleText.text = $"Quản lý tinh thần hầm số {shaft.Level}"; // You can modify this format as needed
        }
        
        gameObject.SetActive(true);
        SelectOption(0); // Default to +25
        SelectEnduranceOption(0); // Default to +25 for endurance
        RefreshList();
        RefreshEnduranceUI();

        // Animation bung ra
        if (modalPanel != null)
        {
            modalPanel.DOKill();
            modalPanel.localScale = Vector3.zero;
            modalPanel.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }
    
    public void CloseModal()
    {
        if (modalPanel != null)
        {
            modalPanel.DOKill();
            modalPanel.DOScale(Vector3.zero, 0.15f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => 
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
    
    public void OnItemSelected(Miner miner)
    {
        selectedMiner = miner;
        RefreshList();
        CalculateCost();
    }
    
    public void RefreshList()
    {
        if (currentShaft == null || currentShaft.activeMiners == null) return;
        
        // Spawn items
        while(spawnedItems.Count < currentShaft.activeMiners.Count)
        {
            GameObject go = Instantiate(moraleItemPrefab, contentParent);
            spawnedItems.Add(go.GetComponent<MoraleItemUI>());
        }
        
        // Setup data
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            if (i < currentShaft.activeMiners.Count)
            {
                spawnedItems[i].gameObject.SetActive(true);
                Miner miner = currentShaft.activeMiners[i];
                bool isSelected = (miner == selectedMiner);
                spawnedItems[i].Setup(i + 1, miner, this, isSelected);
            }
            else
            {
                spawnedItems[i].gameObject.SetActive(false);
            }
        }
    }
    
    public void SelectOption(int optionIndex)
    {
        selectedOption = optionIndex;
        
        // Đổi hình ảnh nút đang chọn
        SetButtonVisual(btnAdd25, optionIndex == 0);
        SetButtonVisual(btnMaxOne, optionIndex == 1);
        SetButtonVisual(btnMaxAll, optionIndex == 2);
        
        CalculateCost();
    }
    
    private void SetButtonVisual(Button btn, bool isSelected)
    {
        if (btn == null || btn.image == null) return;
        
        if (isSelected)
        {
            if (selectedBtnSprite != null) btn.image.sprite = selectedBtnSprite;
            btn.image.color = Color.white;
        }
        else
        {
            if (normalBtnSprite != null) 
            {
                btn.image.sprite = normalBtnSprite;
                btn.image.color = Color.white;
            }
            else 
            {
                // Tắt tàng hình nếu không dùng ảnh viền cho nút bình thường
                btn.image.color = new Color(1f, 1f, 1f, 0f); 
            }
        }
    }
    
    private void CalculateCost()
    {
        currentCost = 0;
        
        // CÂN BẰNG TOÁN HỌC (Dynamic Income Taxation):
        // 100 Tinh thần (Max) = 3 giây thu nhập của 1 thợ mỏ.
        // Vậy 1 Tinh thần = 0.03 giây thu nhập.
        double minCost = (Gamemanager.Instance != null && Gamemanager.Instance.economyConfig != null) ? Gamemanager.Instance.economyConfig.MinimumServiceCost : 100.0;
        double moraleUnitCost = minCost; // Default fallback
        if (currentShaft != null)
        {
            float costMultiplier = (Gamemanager.Instance != null && Gamemanager.Instance.economyConfig != null) ? Gamemanager.Instance.economyConfig.MoraleCostMultiplier : 0.03f;
            moraleUnitCost = currentShaft.GetWorkerProductivity(currentShaft.Level) * costMultiplier;
            if (moraleUnitCost < minCost) moraleUnitCost = minCost; // Giá tối thiểu
        }
        
        if (selectedOption == 0) // +25 for selected
        {
            WorkerHealth wh = selectedMiner != null ? selectedMiner.GetComponent<WorkerHealth>() : null;
            if (wh != null && wh.morale < wh.maxMorale)
            {
                float amount = Mathf.Min(25f, wh.maxMorale - wh.morale);
                currentCost = amount * moraleUnitCost;
            }
        }
        else if (selectedOption == 1) // Max for selected
        {
            WorkerHealth wh = selectedMiner != null ? selectedMiner.GetComponent<WorkerHealth>() : null;
            if (wh != null)
            {
                float missingMorale = wh.maxMorale - wh.morale;
                currentCost = missingMorale * moraleUnitCost;
            }
        }
        else if (selectedOption == 2) // Max for all
        {
            if (currentShaft != null && currentShaft.activeMiners != null)
            {
                foreach(var miner in currentShaft.activeMiners)
                {
                    WorkerHealth wh = miner != null ? miner.GetComponent<WorkerHealth>() : null;
                    if (wh != null) 
                    {
                        float missing = wh.maxMorale - wh.morale;
                        currentCost += missing * moraleUnitCost;
                    }
                }
            }
        }
        
        if (costText != null)
        {
            costText.text = CurrencyFormatter.FormatMoney(currentCost);
            
            bool canAfford = Gamemanager.Instance != null && Gamemanager.Instance.IdleCash >= currentCost;
            costText.color = canAfford && currentCost > 0 ? Color.white : Color.red;
            
            if (btnPay != null) btnPay.interactable = canAfford && currentCost > 0;
        }
    }
    
    public void OnPayClicked()
    {
        if (currentCost <= 0) return;
        
        if (Gamemanager.Instance != null && Gamemanager.Instance.IdleCash >= currentCost)
        {
            Gamemanager.Instance.IdleCash -= currentCost;
            
            if (selectedOption == 0) // +25
            {
                WorkerHealth wh = selectedMiner != null ? selectedMiner.GetComponent<WorkerHealth>() : null;
                if (wh != null) wh.AddMorale(25f);
            }
            else if (selectedOption == 1) // Max 1
            {
                WorkerHealth wh = selectedMiner != null ? selectedMiner.GetComponent<WorkerHealth>() : null;
                if (wh != null) wh.AddMorale(wh.maxMorale);
            }
            else if (selectedOption == 2) // Max all
            {
                if (currentShaft != null && currentShaft.activeMiners != null)
                {
                    foreach (var miner in currentShaft.activeMiners)
                    {
                        WorkerHealth wh = miner != null ? miner.GetComponent<WorkerHealth>() : null;
                        if (wh != null) wh.AddMorale(wh.maxMorale);
                    }
                }
            }
            
            RefreshList();
            CalculateCost();
            
            // Cap nhat UI tong the game neu can (vi du tien bi tru)
            // Gamemanager.Instance.UpdateTopUI() etc.
        }
    }

    public void SelectEnduranceOption(int optionIndex)
    {
        selectedEnduranceOption = optionIndex;
        SetEnduranceButtonVisual(btnEnduranceAdd25, optionIndex == 0);
        SetEnduranceButtonVisual(btnEnduranceMax, optionIndex == 1);
        CalculateEnduranceCost();
    }

    private void SetEnduranceButtonVisual(Button btn, bool isSelected)
    {
        if (btn == null || btn.image == null) return;
        
        if (isSelected)
        {
            if (selectedEnduranceBtnSprite != null) btn.image.sprite = selectedEnduranceBtnSprite;
            btn.image.color = Color.white;
        }
        else
        {
            if (normalEnduranceBtnSprite != null) 
            {
                btn.image.sprite = normalEnduranceBtnSprite;
                btn.image.color = Color.white;
            }
            else 
            {
                btn.image.color = new Color(1f, 1f, 1f, 0f); 
            }
        }
    }

    public void RefreshEnduranceUI()
    {
        if (currentShaft == null) return;
        
        if (enduranceText != null)
        {
            enduranceText.text = $"Endurance: {Mathf.RoundToInt(currentShaft.currentEndurance)}/{Mathf.RoundToInt(currentShaft.maxEndurance)}";
        }
        
        if (enduranceFillImage != null)
        {
            enduranceFillImage.fillAmount = currentShaft.currentEndurance / currentShaft.maxEndurance;
        }
        
        CalculateEnduranceCost();
    }

    private void CalculateEnduranceCost()
    {
        currentEnduranceCost = 0;
        if (currentShaft == null) return;

        // CÂN BẰNG TOÁN HỌC (Dynamic Income Taxation):
        // Boss 3 đánh mỗi 40s (Rút 50% máu). Để tạo Thuế 10-20%, 
        // 100 Máu hầm (Max) = 8 giây tổng thu nhập của hầm.
        // Vậy 1 Máu = 0.08 giây thu nhập.
        double minCost = (Gamemanager.Instance != null && Gamemanager.Instance.economyConfig != null) ? Gamemanager.Instance.economyConfig.MinimumServiceCost : 100.0;
        double enduranceUnitCost = minCost; // Default fallback
        if (currentShaft != null)
        {
            float costMultiplier = (Gamemanager.Instance != null && Gamemanager.Instance.economyConfig != null) ? Gamemanager.Instance.economyConfig.EnduranceCostMultiplier : 0.08f;
            enduranceUnitCost = MathHelper.CalculateEnduranceUnitCost(currentShaft.GetTotalExtractionPerSecond(currentShaft.Level), costMultiplier);
            if (enduranceUnitCost < minCost) enduranceUnitCost = minCost; // Giá tối thiểu
        }

        float missingEndurance = currentShaft.maxEndurance - currentShaft.currentEndurance;

        if (selectedEnduranceOption == 0) // +25
        {
            float amount = Mathf.Min(25f, missingEndurance);
            currentEnduranceCost = amount * enduranceUnitCost;
        }
        else if (selectedEnduranceOption == 1) // Max
        {
            currentEnduranceCost = missingEndurance * enduranceUnitCost;
        }

        if (enduranceCostText != null)
        {
            enduranceCostText.text = CurrencyFormatter.FormatMoney(currentEnduranceCost);
            bool canAfford = Gamemanager.Instance != null && Gamemanager.Instance.IdleCash >= currentEnduranceCost;
            enduranceCostText.color = canAfford && currentEnduranceCost > 0 ? Color.white : Color.red;

            if (btnEndurancePay != null) btnEndurancePay.interactable = canAfford && currentEnduranceCost > 0;
        }
    }

    public void OnEndurancePayClicked()
    {
        if (currentEnduranceCost <= 0 || currentShaft == null) return;

        if (Gamemanager.Instance != null && Gamemanager.Instance.IdleCash >= currentEnduranceCost)
        {
            Gamemanager.Instance.IdleCash -= currentEnduranceCost;

            if (selectedEnduranceOption == 0) // +25
            {
                currentShaft.AddEndurance(25f);
            }
            else if (selectedEnduranceOption == 1) // Max
            {
                currentShaft.AddEndurance(currentShaft.maxEndurance); 
            }

            RefreshEnduranceUI();
        }
    }
}
