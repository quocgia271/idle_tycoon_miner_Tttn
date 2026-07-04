using UnityEngine;
using System.Collections;

public class Warehouse : Facility
{
    public double BaseCapacity = 40;
    // Sức chứa của nhà kho tự động scale theo Level
    public double Capacity => BaseCapacity * Level;

    public float MoveTime = 3f; 

    public Elevator elevator; 

    protected override void Start()
    {
        base.Start(); // Gọi hàm Start của lớp cha Facility để cập nhật Text
        StartCoroutine(WarehouseRoutine());
    }

    IEnumerator WarehouseRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(MoveTime);

            double collected = 0;
            if (elevator != null)
            {
                collected = elevator.TakeResource(Capacity);
            }

            yield return new WaitForSeconds(MoveTime);

            if (Gamemanager.Instance != null && collected > 0)
            {
                Gamemanager.Instance.AddCash(collected);
            }
        }
    }

    protected override void OnUpgraded()
    {
        // Chạy hiệu ứng nhà kho to ra, hoặc xe bự ra
    }
}
