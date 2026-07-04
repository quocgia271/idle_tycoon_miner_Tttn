using UnityEngine;
using System.Collections;

public class Elevator : Facility
{
    public double CurrentLoad = 0; 
    public double DroppedResource = 0; 
    
    public double BaseCapacity = 50; 
    // Sức chứa thang máy tăng trưởng tuyến tính theo cấp độ
    public double Capacity => BaseCapacity * Level; 

    public float MoveTime = 2f; 

    public MineShaft mineShaft; 

    protected override void Start()
    {
        base.Start(); // Gọi hàm Start của lớp cha Facility để cập nhật Text
        StartCoroutine(ElevatorRoutine());
    }

    IEnumerator ElevatorRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(MoveTime);

            if (mineShaft != null)
            {
                double spaceLeft = Capacity - CurrentLoad;
                double collected = mineShaft.TakeResource(spaceLeft);
                CurrentLoad += collected;
            }

            yield return new WaitForSeconds(MoveTime);

            DroppedResource += CurrentLoad;
            CurrentLoad = 0;
        }
    }

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

    protected override void OnUpgraded()
    {
        // Sức chứa tự tăng nhờ biến Capacity.
        // Có thể lập trình giảm MoveTime khi đạt cấp độ cao (chạy nhanh hơn)
    }
}
