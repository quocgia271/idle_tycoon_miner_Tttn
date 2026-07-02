using UnityEngine;
using System.Collections;

public class Elevator : MonoBehaviour
{
    public int Level = 1;
    public double CurrentLoad = 0; // Lượng quặng đang chở trên thang máy
    public double DroppedResource = 0; // Lượng quặng đã đổ ở thùng chứa trên mặt đất
    
    // Thông số cơ bản (Hào sẽ cân bằng sau)
    public double Capacity = 50; 
    public float MoveTime = 2f; // Thời gian đi lên/xuống (giây)

    public MineShaft mineShaft; // Kéo thả MineShaft vào đây trên Unity

    void Start()
    {
        StartCoroutine(ElevatorRoutine());
    }

    IEnumerator ElevatorRoutine()
    {
        while (true)
        {
            // 1. Thang máy chạy xuống hầm
            yield return new WaitForSeconds(MoveTime);

            // 2. Lấy tài nguyên từ hầm
            if (mineShaft != null)
            {
                double spaceLeft = Capacity - CurrentLoad;
                double collected = mineShaft.TakeResource(spaceLeft);
                CurrentLoad += collected;
            }

            // 3. Thang máy chạy lên mặt đất
            yield return new WaitForSeconds(MoveTime);

            // 4. Đổ tài nguyên vào thùng chứa mặt đất (Surface Bin)
            DroppedResource += CurrentLoad;
            CurrentLoad = 0;
        }
    }

    // Nhà kho sẽ gọi hàm này để lấy tài nguyên từ thùng chứa mặt đất
    public double TakeResource(double amountToTake)
    {
        if (amountToTake > DroppedResource)
        {
            double taken = DroppedResource;
            DroppedResource = 0;
            return taken;
        }
        else
        {
            DroppedResource -= amountToTake;
            return amountToTake;
        }
    }
}
