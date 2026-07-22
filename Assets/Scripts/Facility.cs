using UnityEngine;
using TMPro; // Khai báo sử dụng TextMeshPro

// Lớp cha trừu tượng (Abstract) quản lý mọi thông số chung cho việc Nâng cấp
public abstract class Facility : MonoBehaviour
{
    public int Level = 1;

    [Header("Data Configuration")]
    [Tooltip("Kéo file Scriptable Object (vd: ElevatorConfig) vào đây")]
    public FacilityConfigSO Config;

    [Header("UI Reference")]
    public TextMeshProUGUI UpgradeText; // Kéo thả cái Text bên trong Button vào đây

    [Header("Manager Buffs")]
    public float UpgradeCostDiscount = 1f;

    // Tự động tính toán chi phí hiện tại bằng cách gọi sang MathHelper
    public double CurrentUpgradeCost => Config == null ? 0 : MathHelper.CalculateUpgradeCost(Config.BaseCost, Config.CostMultiplier, Level) * UpgradeCostDiscount;

    // Chạy lần đầu tiên khi mở game để cập nhật số Level 1 lên nút bấm
    protected virtual void Start()
    {
        UpdateUpgradeUI();
    }

    // Gọi hàm này khi người chơi bấm nút "Nâng cấp" trên bản đồ game
    // Nó sẽ bật cái bảng UI Modal to đùng lên thay vì trừ tiền ngay
    public void OpenUpgradeModal()
    {
        // Khắc phục lỗi Modal bị tắt (Deactivated) từ đầu khiến Awake không chạy
        if (UpgradeModalUI.Instance == null)
        {
            UpgradeModalUI.Instance = FindObjectOfType<UpgradeModalUI>(true);
        }

        if (UpgradeModalUI.Instance != null)
        {
            UpgradeModalUI.Instance.ShowModal(this, Config);
        }
        else
        {
            Debug.LogError($"<color=red>Lỗi: Không tìm thấy UpgradeModalUI trong Scene! Hãy đảm bảo bạn đã kéo script UpgradeModalUI vào Canvas.</color>");
        }
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

    // --- CÁC HÀM TÍNH TOÁN CHỈ SỐ THỰC TẾ ĐỂ CHẠY LOGIC GAME VÀ HIỂN THỊ UI ---

    // 1. Tính Sức Chứa (Tăng theo hàm mũ)
    public virtual float GetCapacity(int targetLevel)
    {
        if (Config == null) return 0;
        return Config.BaseCapacity * Mathf.Pow(1.1f, targetLevel - 1); // Mỗi cấp tăng 10%
    }

    // 2. Tính Tốc Độ (Tăng từ từ tuyến tính để không hỏng animation)
    public virtual float GetSpeed(int targetLevel)
    {
        if (Config == null) return 0;
        return Config.BaseSpeed + ((targetLevel - 1) * 0.05f); 
    }

    // 3. Tính Tổng Sản Lượng (Throughput = Sức chứa x Tốc độ)
    public virtual float GetTotalThroughput(int targetLevel)
    {
        return GetCapacity(targetLevel) * GetSpeed(targetLevel);
    }

    // 4. Lấy thông tin hiển thị UI (Cho phép lớp con tùy chỉnh)
    public virtual (string curVal, string nextVal) GetStatDisplay(int statIndex, int currentLevel, int nextLevel)
    {
        string curVal = "0";
        string nextVal = "0";
        
        if (statIndex == 0) // Slot 1: Tổng Sản lượng
        {
            curVal = GetTotalThroughput(currentLevel).ToString("F1") + "/s";
            nextVal = GetTotalThroughput(nextLevel).ToString("F1") + "/s";
        }
        else if (statIndex == 1) // Slot 2: Sức chứa
        {
            curVal = Mathf.FloorToInt(GetCapacity(currentLevel)).ToString();
            nextVal = Mathf.FloorToInt(GetCapacity(nextLevel)).ToString();
        }
        else if (statIndex == 2) // Slot 3: Tốc độ
        {
            curVal = GetSpeed(currentLevel).ToString("F2");
            nextVal = GetSpeed(nextLevel).ToString("F2");
        }

        return (curVal, nextVal);
    }
}
