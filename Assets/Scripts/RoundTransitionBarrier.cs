using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoundTransitionBarrier : MonoBehaviour
{
    [Header("Yêu cầu qua màn")]
    public double requireCashToPass = 10000000; // Số tiền cần để đập vỡ vách ngăn
    
    [Header("UI References")]
    public Button unlockButton;
    public TextMeshProUGUI costText;

    [Header("Confirmation Modal UI")]
    public GameObject confirmModal;
    public TextMeshProUGUI confirmText;
    public Button confirmYesButton;
    public Button confirmNoButton;

    private void Start()
    {
        // Tự động tính toán giá đập vách ngăn (Giá trị bằng với việc mở Hầm số 11)
        // Hầm 11 (Index = 11) -> 50 * 15^(11-1) = 50 * 15^10 = ~28.8 Nghìn Tỷ (Trillion)
        double roundMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0;
        requireCashToPass = 50 * System.Math.Pow(15, 10) * roundMultiplier;

        if (costText != null)
        {
            costText.text = $"Phá Vách Ngăn\nGiá: {CurrencyFormatter.FormatMoney(requireCashToPass)}";
        }

        if (unlockButton != null)
        {
            unlockButton.onClick.AddListener(OnBarrierClicked);
        }

        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.AddListener(ExecuteBarrierAction);
        }
        else
        {
            Debug.LogError("[RoundTransitionBarrier] BẠN CHƯA KÉO NÚT YES VÀO Ô 'Confirm Yes Button' TRONG INSPECTOR!");
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.AddListener(CloseConfirmModal);
        }
        else
        {
            Debug.LogError("[RoundTransitionBarrier] BẠN CHƯA KÉO NÚT NO VÀO Ô 'Confirm No Button' TRONG INSPECTOR!");
        }

        if (confirmModal != null)
        {
            confirmModal.SetActive(false);
        }

        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged += CheckAffordability;
            CheckAffordability(Gamemanager.Instance.IdleCash);
        }
    }

    private void OnDestroy()
    {
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged -= CheckAffordability;
        }
    }

    private void CheckAffordability(double currentCash)
    {
        if (costText != null)
        {
            costText.color = currentCash >= requireCashToPass ? Color.white : Color.red;
        }
    }

    private void OnBarrierClicked()
    {
        // 1. Kiểm tra xem người chơi đã mở đủ 10 hầm chưa?
        if (!AreAllShaftsUnlocked())
        {
            Debug.Log("<color=yellow>Phải mở khóa toàn bộ 10 hầm mới được đập vách ngăn này!</color>");
            return;
        }

        // 2. Kiểm tra xem đủ tiền không?
        if (Gamemanager.Instance.IdleCash < requireCashToPass)
        {
            Debug.Log("<color=red>Không đủ tiền để phá vách ngăn!</color>");
            return;
        }

        // 3. Hiện Modal Xác nhận
        ShowConfirmModal();
    }

    private void ShowConfirmModal()
    {
        if (confirmModal != null && confirmText != null && Gamemanager.Instance != null)
        {
            int round = Gamemanager.Instance.CurrentRound;
            string formattedCost = CurrencyFormatter.FormatMoney(requireCashToPass);

            if (round == 1)
            {
                confirmText.text = $"Liệu bạn có đủ sức vượt qua Round 2 với những yêu tinh mạnh mẽ hơn?\n(Cần {formattedCost} để phá vách)\n\n<color=red>⚠️ LƯU Ý: Chuyển sang Vòng mới sẽ Khởi tạo lại Hệ số Chuyển Sinh (Prestige) về 1x!</color>";
            }
            else if (round == 2)
            {
                confirmText.text = $"Độ khó sẽ đạt mức tối đa ở Round 3! Bạn đã chuẩn bị sẵn sàng chưa?\n(Cần {formattedCost})\n\n<color=red>⚠️ LƯU Ý: Chuyển sang Vòng mới sẽ Khởi tạo lại Hệ số Chuyển Sinh (Prestige) về 1x!</color>";
            }
            else if (round == 3)
            {
                confirmText.text = $"Lá chắn đã yếu đi! Phá vỡ nó sẽ giải phóng giới hạn sát thương lên Boss và Rồng. Bắt đầu cuộc tổng tiến công?\n(Cần {formattedCost})";
            }

            confirmModal.SetActive(true);
        }
        else
        {
            // Fallback nếu người chơi chưa gán Modal trong Inspector
            ExecuteBarrierAction();
        }
    }

    private void CloseConfirmModal()
    {
        if (confirmModal != null)
        {
            confirmModal.SetActive(false);
        }
    }

    private void ExecuteBarrierAction()
    {
        Debug.Log("Đã bấm nút YES Xác nhận đập vách ngăn!");
        CloseConfirmModal();

        if (Gamemanager.Instance.DeductCash(requireCashToPass))
        {
            if (Gamemanager.Instance.CurrentRound == 3)
            {
                Debug.Log("<color=green>Đã đập vỡ vách ngăn! GỠ BỎ GIÁP BẤT TỬ CỦA RỒNG!</color>");
                
                // Mở khóa cho phép tấn công Boss
                BossHealth[] allBosses = FindObjectsOfType<BossHealth>();
                foreach (var boss in allBosses)
                {
                    boss.RemoveInvincibility();
                }

                // Kích nộ Rồng và Boss
                DragonBossController dragon = FindObjectOfType<DragonBossController>();
                if (dragon != null) dragon.Enrage();
                
                BossPhase3Controller boss3 = FindObjectOfType<BossPhase3Controller>();
                if (boss3 != null) boss3.Enrage();

                // Ẩn vách ngăn đi để người chơi thấy rõ Rồng
                gameObject.SetActive(false);
            }
            else
            {
                Debug.Log("<color=green>Đã đập vỡ vách ngăn! Bắt đầu Qua Màn...</color>");
                // 3. Gọi lệnh Qua màn trong GameManager (Dành cho Round 1 và 2)
                Gamemanager.Instance.ProceedToNextRound();
            }
        }
    }

    private bool AreAllShaftsUnlocked()
    {
        // Tìm tất cả các hầm mỏ
        MineShaft[] allShafts = FindObjectsOfType<MineShaft>();
        
        // Cần đảm bảo có đúng 10 hầm (hoặc kiểm tra theo số lượng thực tế)
        if (allShafts.Length < 10) 
        {
            Debug.LogWarning("Bản đồ chưa đủ 10 hầm mỏ!");
        }

        foreach (var shaft in allShafts)
        {
            ShaftUnlocker unlocker = shaft.GetComponentInChildren<ShaftUnlocker>(true);
            
            // Nếu có cục unlocker đang hiển thị -> Hầm này CHƯA được mở
            if (unlocker != null && unlocker.gameObject.activeInHierarchy)
            {
                return false; 
            }
        }
        return true; // Tất cả hầm đã được mở
    }

    // Đã gỡ bỏ AreAllBossesDead khỏi đây vì logic kiểm tra Win Game đã chuyển sang BossHealth.cs
}
