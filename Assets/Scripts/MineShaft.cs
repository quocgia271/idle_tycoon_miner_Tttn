using UnityEngine;

// Kế thừa Facility thay vì MonoBehaviour để có sẵn tính năng Nâng cấp
public class MineShaft : Facility 
{
    public double CurrentResource = 0; 
    
    public double BaseResourcePerSecond = 10; 
    
    // Năng suất tăng theo Level (Code sạch, không cần cập nhật rườm rà)
    public double ResourcePerSecond => BaseResourcePerSecond * Level; 

    void Update()
    {
        CurrentResource += ResourcePerSecond * Time.deltaTime;
    }

    public double TakeResource(double amountToTake)
    {
        if (amountToTake > CurrentResource)
        {
            double taken = CurrentResource;
            CurrentResource = 0;
            return taken;
        }
        else
        {
            CurrentResource -= amountToTake;
            return amountToTake;
        }
    }

    // Logic xử lý thêm (nếu có) khi Hầm mỏ được nâng cấp
    protected override void OnUpgraded()
    {
        // Vì ResourcePerSecond tính trực tiếp từ Level, nên nó tự động tăng.
        // Có thể thêm hiệu ứng particle/âm thanh ở đây.
    }
}
