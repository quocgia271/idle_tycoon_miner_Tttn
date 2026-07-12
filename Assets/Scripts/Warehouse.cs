using UnityEngine;
using System.Collections.Generic;

public class Warehouse : Facility
{
    [Header("Positions")]
    public Transform elevatorPos; // Điểm lấy tiền
    public Transform depositPos;  // Điểm nạp tiền vào kho
    
    [Header("Warehouse Settings")]
    public double BaseCapacity = 40;
    // Sức chứa của 1 công nhân (Tạm tính bằng Capacity của kho, nếu nhiều công nhân sẽ tính phân bổ khác)
    public double Capacity => BaseCapacity * Level;
    
    public float moveSpeed = 3f;
    public float loadTime = 2f;
    
    public Elevator elevator; 

    [Header("Workers")]
    public List<WarehouseWorker> workers = new List<WarehouseWorker>();

    [Header("Effects")]
    public ParticleSystem loadingVFX; // Kéo thả hiệu ứng VFX vào đây
    private int loadingWorkersCount = 0;

    protected override void Start()
    {
        base.Start(); // Gọi hàm Start của lớp cha Facility để cập nhật Text
        
        // Tự động tìm các Worker là con của Warehouse nếu danh sách rỗng
        if (workers.Count == 0)
        {
            workers = new List<WarehouseWorker>(GetComponentsInChildren<WarehouseWorker>());
            
            // Tự động gán warehouse cho các worker nếu chưa có
            foreach(var worker in workers)
            {
                if (worker.warehouse == null)
                {
                    worker.warehouse = this;
                }
            }
        }

        // Tắt VFX lúc mới vào game (phòng trường hợp bạn để Play On Awake)
        if (loadingVFX != null)
        {
            loadingVFX.Stop();
        }
    }

    public void AddLoadingWorker()
    {
        loadingWorkersCount++;
        // Nếu đây là người ĐẦU TIÊN bắt đầu lấy tiền -> Bật VFX
        if (loadingWorkersCount == 1 && loadingVFX != null)
        {
            loadingVFX.Play();
        }
    }

    public void RemoveLoadingWorker()
    {
        loadingWorkersCount--;
        // Nếu KHÔNG CÒN AI đang lấy tiền nữa -> Tắt VFX
        if (loadingWorkersCount <= 0)
        {
            loadingWorkersCount = 0;
            if (loadingVFX != null)
            {
                loadingVFX.Stop();
            }
        }
    }

    protected override void OnUpgraded()
    {
        // Chạy hiệu ứng nhà kho to ra, hoặc xe bự ra
        // Bạn có thể code logic sinh thêm WarehouseWorker tại đây nếu đạt Level nhất định
    }
}
