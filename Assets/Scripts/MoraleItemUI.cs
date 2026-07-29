using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MoraleItemUI : MonoBehaviour
{
    public TextMeshProUGUI indexText;
    public TextMeshProUGUI moraleText;
    public TextMeshProUGUI stateText;
    public Button selectButton;
    public Image backgroundImage;
    
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    
    private Miner boundMiner;
    private MoraleModalUI parentModal;

    [Header("Revive Feature")]
    public GameObject reviveGroup; // Group chứa nút hồi sinh và text giá tiền
    public Button reviveButton;
    public TextMeshProUGUI reviveCostText;
    
    // CÂN BẰNG TOÁN HỌC MỚI (Dynamic Income Taxation - Thuế Thu nhập Động):
    // Thay vì dựa vào Giá trị tài sản (UpgradeCost) vốn bị lỗi thời khi có Prestige (Uy danh),
    // Ta neo chi phí vào chính Năng suất của thợ mỏ.
    // Nếu Boss đánh mỗi 30s, thợ mỏ cày được 30s thu nhập.
    // Để tạo ra mức "Thuế 10%", giá hồi sinh phải bằng: 30s * 10% = 3 giây thu nhập của thợ mỏ.
    private double ActualReviveCost 
    {
        get 
        {
            if (boundMiner == null || boundMiner.currentShaft == null) return 1000 * (Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0);
            double workerProductivity = boundMiner.currentShaft.GetWorkerProductivity(boundMiner.currentShaft.Level);
            return workerProductivity * 3; // 3 giây thu nhập
        }
    }

    private void Awake()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(() => {
                if (parentModal != null && boundMiner != null) 
                {
                    parentModal.OnItemSelected(boundMiner);
                }
            });
        }
        
        if (reviveButton != null)
        {
            reviveButton.onClick.AddListener(OnReviveClicked);
        }
    }

    public void Setup(int index, Miner miner, MoraleModalUI modal, bool isSelected)
    {
        boundMiner = miner;
        parentModal = modal;
        
        if (indexText != null) indexText.text = $"#{index}";
        if (moraleText != null) moraleText.text = $"{Mathf.RoundToInt(miner.morale)}/{miner.maxMorale}";
        
        if (stateText != null) 
        {
            if (miner.healthState == Miner.HealthState.Dead)
            {
                stateText.text = "Đã chết";
                stateText.color = Color.gray;
            }
            else
            {
                stateText.text = miner.healthState == Miner.HealthState.Normal ? "Bình thường" : "Chấn thương";
                stateText.color = miner.healthState == Miner.HealthState.Normal ? Color.green : Color.red;
            }
        }
        
        if (backgroundImage != null) backgroundImage.color = isSelected ? selectedColor : normalColor;


        // Logic hiển thị nút Hồi Sinh
        if (reviveGroup != null)
        {
            if (isSelected && miner.healthState == Miner.HealthState.Dead)
            {
                reviveGroup.SetActive(true);
                if (reviveCostText != null) reviveCostText.text = CurrencyFormatter.FormatMoney(ActualReviveCost);
                
                if (reviveButton != null)
                {
                    bool canAfford = Gamemanager.Instance != null && Gamemanager.Instance.IdleCash >= ActualReviveCost;
                    reviveButton.interactable = canAfford;
                    if (reviveCostText != null) reviveCostText.color = canAfford ? Color.white : Color.red;
                }
            }
            else
            {
                reviveGroup.SetActive(false);
            }
        }
    }

    private void OnReviveClicked()
    {
        Debug.Log("Đã bấm nút Revive!");
        if (boundMiner == null) 
        {
            Debug.LogError("boundMiner bị null!");
            return;
        }
        
        if (boundMiner.healthState != Miner.HealthState.Dead) 
        {
            Debug.LogWarning("Thợ mỏ này chưa chết (Trạng thái hiện tại: " + boundMiner.healthState + "), không thể hồi sinh!");
            return;
        }
        
        if (Gamemanager.Instance != null)
        {
            if (Gamemanager.Instance.IdleCash >= ActualReviveCost)
            {
                Debug.Log("Đủ tiền! Tiến hành trừ tiền và hồi sinh.");
                Gamemanager.Instance.IdleCash -= ActualReviveCost;
                boundMiner.Revive();
            }
            else
            {
                Debug.LogWarning($"Không đủ tiền! Cần {ActualReviveCost}, nhưng đang có {Gamemanager.Instance.IdleCash}");
            }
        }
        else
        {
            Debug.LogError("Gamemanager.Instance bị null!");
        }
    }
}
