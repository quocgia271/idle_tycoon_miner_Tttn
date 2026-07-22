using UnityEngine;
using System.Collections.Generic;

public class Warehouse : Facility
{
    [Header("Positions")]
    public Transform elevatorPos; // Điểm lấy tiền
    public Transform depositPos;  // Điểm nạp tiền vào kho
    
    [Header("Warehouse Settings")]
    public double BaseCapacity = 40;
    
    [Header("Worker Settings")]
    public int MaxWorkers = 5;
    public float MaxMoveSpeed = 5f;
    public float MinLoadTime = 0.5f;

    [Header("Worker Spawn Settings")]
    public WarehouseWorker workerPrefab;
    public float spawnOffsetX = 0.5f;

    public double Capacity => GetWorkerCapacity(Level);
    public float moveSpeed => GetWorkerMoveSpeed(Level);
    public float loadTime => GetWorkerLoadTime(Level);

    public int GetWorkersCount(int targetLevel)
    {
        int count = 1 + (targetLevel / 10);
        return Mathf.Min(count, MaxWorkers);
    }

    public float GetWorkerMoveSpeed(int targetLevel)
    {
        float speed = Config != null ? Config.BaseSpeed + (targetLevel * 0.05f) : 3f;
        return Mathf.Min(speed, MaxMoveSpeed);
    }

    public float GetWorkerLoadTime(int targetLevel)
    {
        float time = 2f - (targetLevel * 0.01f);
        return Mathf.Max(time, MinLoadTime);
    }

    public double GetWorkerCapacity(int targetLevel)
    {
        return BaseCapacity * targetLevel;
    }

    public double GetTotalThroughputDisplay(int targetLevel)
    {
        // Giả lập tổng vận chuyển (Sức chứa * Số lượng xe) / Thời gian trung bình
        return (GetWorkerCapacity(targetLevel) * GetWorkersCount(targetLevel)) / (GetWorkerLoadTime(targetLevel) + 2f);
    }
    
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
        CheckAndSpawnWorkers();
    }

    private void CheckAndSpawnWorkers()
    {
        if (workerPrefab == null || depositPos == null || elevatorPos == null) return;

        int targetCount = GetWorkersCount(Level);
        int requiredSpawns = targetCount - 1; // Luôn có 1 con gốc do bạn đặt tay

        while (workers.Count < requiredSpawns)
        {
            int index = workers.Count + 1;
            
            // Xếp hàng lùi ra sau (theo trục X)
            Vector3 spawnPos = depositPos.position + new Vector3(spawnOffsetX * index, 0, 0); 

            WarehouseWorker newWorker = Instantiate(workerPrefab, spawnPos, Quaternion.identity, transform);
            newWorker.warehouse = this;
            
            // Tạo 1 điểm chờ riêng để nó đi về đứng xếp hàng chứ không đè lên con gốc
            GameObject tempDeposit = new GameObject($"DepositPos_Worker_{index}");
            tempDeposit.transform.position = spawnPos;
            tempDeposit.transform.SetParent(transform);

            newWorker.myDepositPos = tempDeposit.transform;

            workers.Add(newWorker);
        }
    }

    public override (string curVal, string nextVal) GetStatDisplay(int statIndex, int currentLevel, int nextLevel)
    {
        string curVal = "0";
        string nextVal = "0";
        
        switch (statIndex)
        {
            case 0: // Tổng vận chuyển
                curVal = GetTotalThroughputDisplay(currentLevel).ToString("F1") + "/s";
                nextVal = GetTotalThroughputDisplay(nextLevel).ToString("F1") + "/s";
                break;
            case 1: // Số lượng nhân viên
                curVal = GetWorkersCount(currentLevel).ToString();
                nextVal = GetWorkersCount(nextLevel).ToString();
                break;
            case 2: // Sức chứa 1 người
                curVal = GetWorkerCapacity(currentLevel).ToString("F0");
                nextVal = GetWorkerCapacity(nextLevel).ToString("F0");
                break;
            case 3: // Tốc độ di chuyển
                curVal = GetWorkerMoveSpeed(currentLevel).ToString("F2");
                nextVal = GetWorkerMoveSpeed(nextLevel).ToString("F2");
                break;
            case 4: // Tốc độ bốc vác (thời gian)
                curVal = GetWorkerLoadTime(currentLevel).ToString("F2") + "s";
                nextVal = GetWorkerLoadTime(nextLevel).ToString("F2") + "s";
                break;
        }

        return (curVal, nextVal);
    }
}
