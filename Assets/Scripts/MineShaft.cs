using UnityEngine;
using TMPro; // Thêm thư viện này để dùng TextMeshPro

// Kế thừa Facility thay vì MonoBehaviour để có sẵn tính năng Nâng cấp
public class MineShaft : Facility 
{
    public double CurrentResource = 0; 
    
    public double BaseResourcePerSecond = 10; 
    
    // Năng suất tăng theo Level (Code sạch, không cần cập nhật rườm rà)
    public double ResourcePerSecond => BaseResourcePerSecond * Level; 

    [Header("UI")]
    public TextMeshProUGUI shaftCashText; // Hiển thị số tiền/tài nguyên hiện tại của hầm

    protected override void Start()
    {
        base.Start(); // Gọi hàm Start của lớp cha (Facility) để update text Level
        UpdateUI();
    }

    // Hàm này được gọi bởi con thợ mỏ sau khi nó đào xong
    public void AddResource(double amount)
    {
        CurrentResource += amount;
        UpdateUI();
    }

    public double TakeResource(double amountToTake)
    {
        double taken = 0;
        if (amountToTake > CurrentResource)
        {
            taken = CurrentResource;
            CurrentResource = 0;
        }
        else
        {
            CurrentResource -= amountToTake;
            taken = amountToTake;
        }
        
        UpdateUI(); // Cập nhật lại UI sau khi thang máy lấy đi
        return taken;
    }

    private void UpdateUI()
    {
        if (shaftCashText != null)
        {
            // Sử dụng CurrencyFormatter để hiển thị số mượt hơn (K, M, B)
            shaftCashText.text = CurrencyFormatter.FormatMoney(CurrentResource);
        }
    }

    // Logic xử lý thêm (nếu có) khi Hầm mỏ được nâng cấp
    protected override void OnUpgraded()
    {
        // Vì ResourcePerSecond tính trực tiếp từ Level, nên nó tự động tăng.
        // Có thể thêm hiệu ứng particle/âm thanh ở đây.
    }
}
