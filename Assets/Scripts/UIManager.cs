using UnityEngine;
using TMPro; // Sử dụng thư viện UI TextMeshPro của Unity

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI cashText; // Kéo thả Text hiển thị tiền vào đây
    public TextMeshProUGUI levelText; // Kéo thả Text hiển thị Level vào đây
    public TextMeshProUGUI roundText; // Text hiển thị Vòng chơi (Round 1, 2, 3)
    public TextMeshProUGUI multiplierText; // Text hiển thị Hệ số nhân (Multiplier)

    // --- Tối ưu hóa UI (Throttling) ---
    private double currentCashToDisplay = 0;
    private bool isCashDirty = false;
    private float cashUpdateTimer = 0f;
    private const float CASH_UPDATE_INTERVAL = 0.05f; // Cập nhật 20 lần/giây (50ms)

    void Start()
    {
        // Đăng ký lắng nghe sự kiện từ Gamemanager khi game bắt đầu
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged += UpdateCashUI;
            Gamemanager.Instance.OnLevelChanged += UpdateLevelUI;
            
            // Cập nhật giao diện lần đầu tiên lúc vừa vào game
            UpdateCashUI(Gamemanager.Instance.IdleCash);
            ForceUpdateCashText(); // Ép cập nhật ngay lập tức khi vừa vào game
            UpdateLevelUI(Gamemanager.Instance.PlayerLevel);
            
            if (roundText != null)
            {
                roundText.text = $"Round {Gamemanager.Instance.CurrentRound}";
            }

            if (multiplierText != null)
            {
                multiplierText.text = $"x{Gamemanager.Instance.PrestigeMultiplier:F2}";
            }
        }
    }

    void Update()
    {
        // Thay vì vẽ lại chữ (Canvas Rebuild) liên tục mỗi frame do thợ mỏ nộp tiền,
        // Ta chỉ vẽ lại sau mỗi 0.05 giây để tiết kiệm CPU. 
        // Thời gian 0.05s là cực kỳ nhỏ, mắt người sẽ thấy tiền vẫn nhảy liên tục và mượt mà.
        if (isCashDirty)
        {
            cashUpdateTimer += Time.deltaTime;
            if (cashUpdateTimer >= CASH_UPDATE_INTERVAL)
            {
                ForceUpdateCashText();
            }
        }
    }

    void OnDestroy()
    {
        // Hủy đăng ký lắng nghe khi UI bị xóa/tắt game (tránh lỗi rò rỉ bộ nhớ)
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged -= UpdateCashUI;
            Gamemanager.Instance.OnLevelChanged -= UpdateLevelUI;
        }
    }

    // Hàm này sẽ tự động được gọi mỗi khi tiền tăng lên, nhưng chỉ lưu con số chứ KHÔNG vẽ chữ ngay
    private void UpdateCashUI(double newCash)
    {
        currentCashToDisplay = newCash;
        isCashDirty = true;
    }

    // Hàm thực hiện việc vẽ chữ lên màn hình
    private void ForceUpdateCashText()
    {
        if (cashText != null)
        {
            cashText.text = CurrencyFormatter.FormatMoney(currentCashToDisplay);
        }
        cashUpdateTimer = 0f;
        isCashDirty = false;
    }

    // Hàm này sẽ tự động được gọi mỗi khi level thay đổi
    private void UpdateLevelUI(int newLevel)
    {
        if (levelText != null)
        {
            levelText.text = "Lv." + newLevel.ToString();
        }
    }
}
