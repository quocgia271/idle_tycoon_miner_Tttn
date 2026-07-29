using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PrestigeModalUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI infoText;        // Đoạn text hiển thị thông tin chuyển sinh
    public Button confirmButton;            // Nút "Chuyển Sinh" (Xác nhận)
    public Button closeButton;              // Nút "Đóng" (Hủy)

    private void Start()
    {
        // Gắn sự kiện cho các nút bấm
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmPrestige);
        }
        
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseModal);
        }
    }

    // Hàm này sẽ được gọi khi bạn bấm nút "Chuyển Sinh" ở góc màn hình ngoài Game
    // Nó dùng để hiển thị bảng (Modal) này lên và cập nhật chữ
    public void OpenModal()
    {
        gameObject.SetActive(true);
        UpdateInfoText();
    }

    public void CloseModal()
    {
        gameObject.SetActive(false);
    }

    private void UpdateInfoText()
    {
        if (Gamemanager.Instance == null || infoText == null) return;

        double lifetimeCash = Gamemanager.Instance.LifetimeCash;
        double currentMultiplier = Gamemanager.Instance.PrestigeMultiplier;
        double nextMultiplier = Gamemanager.Instance.CalculateNextPrestigeMultiplier();

        double baseRequirement = 1000000 * Gamemanager.Instance.RoundMultiplier;

        // Kiểm tra xem đã đủ điều kiện chưa (Ví dụ: Cần 1 Triệu * RoundMultiplier)
        if (lifetimeCash < baseRequirement)
        {
            infoText.text = $"<color=red><b>CHƯA ĐỦ ĐIỀU KIỆN</b></color>\n\n" +
                            $"Bạn cần kiếm tổng cộng <b>{CurrencyFormatter.FormatMoney(baseRequirement)}</b> Vàng trong vòng này để có thể Chuyển Sinh.\n" +
                            $"Tiền cày được hiện tại: <b>{CurrencyFormatter.FormatMoney(lifetimeCash)}</b>";
            
            // Khóa nút xác nhận
            if (confirmButton != null) confirmButton.interactable = false;
        }
        else
        {
            // Nếu đã đủ tiền
            infoText.text = $"<color=green><b>SẴN SÀNG CHUYỂN SINH!</b></color>\n\n" +
                            $"Hầm mỏ và Vàng hiện tại sẽ bị reset về 0.\n" +
                            $"Các Quản Lý (Manager) vẫn được giữ nguyên.\n\n" +
                            $"Hệ số hiện tại: <b>x{currentMultiplier:F1}</b>\n" +
                            $"Hệ số mới sẽ nhận: <color=yellow><b>x{nextMultiplier:F1}</b></color>";

            // Mở khóa nút xác nhận
            if (confirmButton != null) confirmButton.interactable = true;
        }
    }

    private void OnConfirmPrestige()
    {
        if (Gamemanager.Instance != null)
        {
            // Gọi lệnh Prestige thực sự
            Gamemanager.Instance.Prestige();
            
            // Đóng bảng này lại (Thực ra Game sẽ reload lại Scene ngay sau hàm trên, nhưng cứ gọi cho an toàn)
            CloseModal();
        }
    }
}
