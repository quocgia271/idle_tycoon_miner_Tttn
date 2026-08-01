using UnityEngine;
using TMPro; // Sử dụng thư viện UI TextMeshPro của Unity

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI cashText; // Kéo thả Text hiển thị tiền vào đây
    public TextMeshProUGUI levelText; // Kéo thả Text hiển thị Level vào đây
    public TextMeshProUGUI roundText; // Text hiển thị Vòng chơi (Round 1, 2, 3)
    public TextMeshProUGUI multiplierText; // Text hiển thị Hệ số nhân (Multiplier)

    void Start()
    {
        // Đăng ký lắng nghe sự kiện từ Gamemanager khi game bắt đầu
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged += UpdateCashUI;
            Gamemanager.Instance.OnLevelChanged += UpdateLevelUI;
            
            // Cập nhật giao diện lần đầu tiên lúc vừa vào game
            UpdateCashUI(Gamemanager.Instance.IdleCash);
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

    void OnDestroy()
    {
        // Hủy đăng ký lắng nghe khi UI bị xóa/tắt game (tránh lỗi rò rỉ bộ nhớ)
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged -= UpdateCashUI;
            Gamemanager.Instance.OnLevelChanged -= UpdateLevelUI;
        }
    }

    // Hàm này sẽ tự động được gọi mỗi khi tiền tăng lên
    private void UpdateCashUI(double newCash)
    {
        if (cashText != null)
        {
            // Hiển thị số tiền bằng CurrencyFormatter để hỗ trợ M, B, aa, ab...
            cashText.text = CurrencyFormatter.FormatMoney(newCash);
        }
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
