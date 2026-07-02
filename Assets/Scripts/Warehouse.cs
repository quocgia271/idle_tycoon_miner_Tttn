using UnityEngine;
using System.Collections;

public class Warehouse : MonoBehaviour
{
    public int Level = 1;
    
    // Thông số cơ bản (Hào sẽ cân bằng sau)
    public double Capacity = 40;
    public float MoveTime = 3f; // Thời gian đi và về

    public Elevator elevator; // Kéo thả Elevator vào đây trên Unity

    void Start()
    {
        StartCoroutine(WarehouseRoutine());
    }

    IEnumerator WarehouseRoutine()
    {
        while (true)
        {
            // 1. Xe goòng chạy từ Kho đến Thang máy
            yield return new WaitForSeconds(MoveTime);

            double collected = 0;
            // 2. Lấy tài nguyên từ thùng chứa của Thang máy
            if (elevator != null)
            {
                collected = elevator.TakeResource(Capacity);
            }

            // 3. Xe goòng chạy về Kho
            yield return new WaitForSeconds(MoveTime);

            // 4. Bán tài nguyên thành tiền và nạp vào GameManager
            if (Gamemanager.Instance != null && collected > 0)
            {
                Gamemanager.Instance.AddCash(collected);
            }
        }
    }
}
