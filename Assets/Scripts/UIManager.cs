using UnityEngine;
using TMPro; // Sử dụng thư viện UI TextMeshPro của Unity

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI cashText; // Kéo thả Text hiển thị tiền vào đây

    void Start()
    {
        // Đăng ký lắng nghe sự kiện từ Gamemanager khi game bắt đầu
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged += UpdateCashUI;
            
            // Cập nhật giao diện lần đầu tiên lúc vừa vào game
            UpdateCashUI(Gamemanager.Instance.IdleCash);
        }
    }

    void OnDestroy()
    {
        // Hủy đăng ký lắng nghe khi UI bị xóa/tắt game (tránh lỗi rò rỉ bộ nhớ)
        if (Gamemanager.Instance != null)
        {
            Gamemanager.Instance.OnCashChanged -= UpdateCashUI;
        }
    }

    // Hàm này sẽ tự động được gọi mỗi khi tiền tăng lên
    private void UpdateCashUI(double newCash)
    {
        if (cashText != null)
        {
            // Hiển thị số tiền kèm dấu $ và làm tròn 2 chữ số (VD: $150.50)
            cashText.text = "$" + newCash.ToString("F2");
        }
    }
}
