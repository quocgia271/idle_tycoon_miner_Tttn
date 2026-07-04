using UnityEngine;
using TMPro; // Khai báo sử dụng TextMeshPro

// Lớp cha trừu tượng (Abstract) quản lý mọi thông số chung cho việc Nâng cấp
public abstract class Facility : MonoBehaviour
{
    public int Level = 1;
    public double BaseCost = 50;
    public double CostMultiplier = 1.15; // Mỗi level giá tăng 15%

    [Header("UI Reference")]
    public TextMeshProUGUI UpgradeText; // Kéo thả cái Text bên trong Button vào đây

    // Tự động tính toán chi phí hiện tại bằng cách gọi sang MathHelper
    public double CurrentUpgradeCost => MathHelper.CalculateUpgradeCost(BaseCost, CostMultiplier, Level);

    // Chạy lần đầu tiên khi mở game để cập nhật số Level 1 lên nút bấm
    protected virtual void Start()
    {
        UpdateUpgradeUI();
    }

    // Bất kỳ nút [Upgrade] nào trên UI cũng có thể gọi thẳng vào hàm này
    public void TryUpgrade()
    {
        if (Gamemanager.Instance != null && Gamemanager.Instance.DeductCash(CurrentUpgradeCost))
        {
            Level++;
            OnUpgraded(); // Gọi lớp con để cập nhật chỉ số
            UpdateUpgradeUI(); // Cập nhật lại Text trên nút bấm
            Debug.Log($"<color=green>{gameObject.name} upgraded to level {Level}</color>");
        }
        else
        {
            Debug.Log($"<color=red>Not enough cash to upgrade {gameObject.name}</color>");
        }
    }

    // Hàm chuyên dùng để đổi chữ trên nút bấm
    private void UpdateUpgradeUI()
    {
        if (UpgradeText != null)
        {
            // Hiển thị Level và Giá tiền xuống dòng (\n)
            UpgradeText.text = $"Level {Level}";
        }
    }

    // Các lớp con (Hầm mỏ, Thang máy...) phải tự định nghĩa hàm này 
    // để quyết định xem chúng nó tăng hiệu suất gì sau khi lên cấp.
    protected abstract void OnUpgraded();
}
