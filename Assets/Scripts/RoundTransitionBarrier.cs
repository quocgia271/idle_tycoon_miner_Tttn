using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

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

    [Header("Barrier Dirt Area Settings")]
    public Transform barrierDirtArea;
    [Tooltip("Danh sách prefab của các round để hiển thị mỏ của phần kế tiếp (index 0 = Round 1, 1 = Round 2,...)")]
    public GameObject[] roundDirtPrefabs;

    private void Start()
    {
        // Đăng ký sự kiện nạp Save (nếu màn chơi chưa đổi nhưng số Round thay đổi từ Save)
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged += CheckAffordability;
            Gamemanager.Instance.OnRoundChanged += UpdateRoundVisuals;
        }

        UpdateRoundVisuals(Gamemanager.Instance != null ? Gamemanager.Instance.CurrentRound : 1);

        // Tự động tính toán giá đập vách ngăn (Giá trị bằng với việc mở Hầm số 11)
        // Hầm 11 (Index = 11) -> 50 * 15^(11-1) = 50 * 15^10 = ~28.8 Nghìn Tỷ (Trillion)
        double roundMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0;
        if (Gamemanager.Instance != null && Gamemanager.Instance.GlobalConfig != null)
        {
            var config = Gamemanager.Instance.GlobalConfig;
            requireCashToPass = config.ShaftUnlockBaseCost * System.Math.Pow(config.ShaftDepthMultiplier, 10) * roundMultiplier;
        }
        else
        {
            requireCashToPass = 50 * System.Math.Pow(15, 10) * roundMultiplier;
        }

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
            CheckAffordability(Gamemanager.Instance.IdleCash);
        }
    }

    private void UpdateRoundVisuals(int round)
    {
        // --- CẬP NHẬT PREFAB CHO AREA FILLER CỦA ROUND TIẾP THEO ---
        if (barrierDirtArea != null && roundDirtPrefabs != null)
        {
            // Do currentRound = 1, nếu index trong mảng là 1 sẽ lấy prefab ở index 1 (tức là Round 2).
            int nextRoundIndex = round; 
            if (nextRoundIndex < roundDirtPrefabs.Length && roundDirtPrefabs[nextRoundIndex] != null)
            {
                AreaFiller filler = barrierDirtArea.GetComponent<AreaFiller>();
                if (filler != null)
                {
                    filler.itemPrefab = roundDirtPrefabs[nextRoundIndex];
                    
                    // Đảm bảo clear các cục đất cũ (nếu AreaFiller lỡ rải từ trước) và rải lại
                    filler.ClearAll();
                    filler.FillRandomly();
                }
                else
                {
                    foreach (Transform child in barrierDirtArea)
                    {
                        Destroy(child.gameObject);
                    }
                    Instantiate(roundDirtPrefabs[nextRoundIndex], barrierDirtArea.position, Quaternion.identity, barrierDirtArea);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged -= CheckAffordability;
            Gamemanager.Instance.OnRoundChanged -= UpdateRoundVisuals;
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
            
            CanvasGroup cg = confirmModal.GetComponent<CanvasGroup>();
            if (cg == null) cg = confirmModal.AddComponent<CanvasGroup>();
            RectTransform rect = confirmModal.GetComponent<RectTransform>();

            DOTween.Kill(cg);
            DOTween.Kill(rect);

            cg.alpha = 0f;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -150f);
            
            cg.DOFade(1f, 0.35f).SetUpdate(true).SetEase(Ease.OutQuad);
            rect.DOAnchorPosY(0f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
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
            CanvasGroup cg = confirmModal.GetComponent<CanvasGroup>();
            RectTransform rect = confirmModal.GetComponent<RectTransform>();

            if (cg != null && rect != null)
            {
                DOTween.Kill(cg);
                DOTween.Kill(rect);

                cg.DOFade(0f, 0.25f).SetUpdate(true).SetEase(Ease.OutQuad);
                rect.DOAnchorPosY(-150f, 0.25f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => {
                    confirmModal.SetActive(false);
                });
            }
            else
            {
                confirmModal.SetActive(false);
            }
        }
    }

    private void ExecuteBarrierAction()
    {
        Debug.Log("Đã bấm nút YES Xác nhận đập vách ngăn!");
        CloseConfirmModal();

        int currentRound = Gamemanager.Instance.CurrentRound;

        if (currentRound == 3)
        {
            // Ở Round 3, đập vách ngăn tốn tiền nhưng không qua màn, chỉ gỡ giáp rồng
            if (Gamemanager.Instance.DeductCash(requireCashToPass))
            {
                Debug.Log("<color=green>Đã đập vỡ vách ngăn! GỠ BỎ GIÁP BẤT TỬ CỦA RỒNG!</color>");
                
                BossHealth[] allBosses = FindObjectsOfType<BossHealth>(true);
                foreach (var boss in allBosses)
                {
                    if (boss != null) boss.RemoveInvincibility();
                }

                // Kích nộ Rồng và Boss
                DragonBossController dragon = FindObjectOfType<DragonBossController>();
                if (dragon != null) dragon.Enrage();
                
                BossPhase3Controller boss3 = FindObjectOfType<BossPhase3Controller>();
                if (boss3 != null) boss3.Enrage();

                // Ẩn vách ngăn đi để người chơi thấy rõ Rồng
                gameObject.SetActive(false);
            }
        }
        else
        {
            // Ở Round 1 và 2, KHÔNG DÙNG DeductCash vì nó sẽ lưu file save giữa chừng gây lỗi nếu tắt game.
            // Chỉ cần check đủ tiền, sau đó gọi ProceedToNextRound (hàm này tự Reset tiền về 150M rồi).
            if (Gamemanager.Instance.IdleCash >= requireCashToPass)
            {
                Debug.Log("<color=green>Đã đập vỡ vách ngăn! Bắt đầu Qua Màn...</color>");
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
