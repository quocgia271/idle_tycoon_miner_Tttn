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
    public double reviveCost = 1000;

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
                if (reviveCostText != null) reviveCostText.text = CurrencyFormatter.FormatMoney(reviveCost);
                
                if (reviveButton != null)
                {
                    bool canAfford = Gamemanager.Instance != null && Gamemanager.Instance.IdleCash >= reviveCost;
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
            if (Gamemanager.Instance.IdleCash >= reviveCost)
            {
                Debug.Log("Đủ tiền! Tiến hành trừ tiền và hồi sinh.");
                Gamemanager.Instance.IdleCash -= reviveCost;
                boundMiner.Revive();
            }
            else
            {
                Debug.LogWarning($"Không đủ tiền! Cần {reviveCost}, nhưng đang có {Gamemanager.Instance.IdleCash}");
            }
        }
        else
        {
            Debug.LogError("Gamemanager.Instance bị null!");
        }
    }
}
