using UnityEngine;

public class MineShaft : MonoBehaviour
{
    public int Level = 1;
    public double CurrentResource = 0; // Tài nguyên đang tích trữ dưới hầm
    
    // Các thông số này Hào sẽ thay đổi công thức sau
    public double ResourcePerSecond = 10; 

    void Update()
    {
        // Tự động sinh tài nguyên theo thời gian (Tuần 2)
        CurrentResource += ResourcePerSecond * Time.deltaTime;
    }

    // Thang máy sẽ gọi hàm này để lấy tài nguyên
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
}
