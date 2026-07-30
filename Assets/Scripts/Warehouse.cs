// Forced Recompile
using UnityEngine;
using System.Collections.Generic;

public class Warehouse : Facility
{
    public override FacilityType GetFacilityType() => FacilityType.Warehouse;

    [Header("Positions")]
    public Transform elevatorPos; // Điểm lấy tiền
    public Transform depositPos;  // Điểm nạp tiền vào kho
    
    // Bỏ thuộc tính ProjectileDamage tĩnh vì sát thương sẽ được tính động theo % Máu của Mục Tiêu
    [Tooltip("Chỉnh vị trí đạn cắm vào Boss đi bộ (vd y=1 để trúng ngực thay vì chân)")]
    public Vector3 groundBossTargetOffset = new Vector3(0, 1f, 0);
    [Tooltip("Chỉnh vị trí đạn cắm vào Rồng bay")]
    public Vector3 flyingDragonTargetOffset = new Vector3(0, 0f, 0);
    public Transform turretPos;
    public GameObject projectilePrefab;
    public GameObject shootVFX; // Hiệu ứng nòng súng (muzzle flash)
    public float shootInterval = 1.5f;
    private float shootTimer = 0f;
    
    [Header("Warehouse Settings")]
    public double BaseCapacity = 40;
    
    [Header("Worker Settings")]
    public int MaxWorkers = 5;
    public float MaxMoveSpeed = 5f;
    public float MinLoadTime = 0.5f;

    [Header("Worker Spawn Settings")]
    public WarehouseWorker workerPrefab;
    public float spawnOffsetX = 0.5f;

    public float WorkerMoveSpeedBuff = 1f;
    public float WorkerLoadSpeedBuff = 1f;

    public double Capacity => GetWorkerCapacity(Level);
    public float moveSpeed => GetWorkerMoveSpeed(Level) * WorkerMoveSpeedBuff;
    public float loadTime => GetWorkerLoadTime(Level) / WorkerLoadSpeedBuff;

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
        double baseCap = BaseCapacity * System.Math.Pow(1.1f, targetLevel - 1);
        
        // --- CHUẨN GAME DESIGN: MILESTONE JUMPS ---
        // Cơ chế bùng nổ sức chứa tại các mốc Level chẵn để bắt kịp sản lượng của Hầm mới.
        double milestoneMult = 1.0;
        if (targetLevel >= 10) milestoneMult *= 2;
        if (targetLevel >= 25) milestoneMult *= 2;
        if (targetLevel >= 50) milestoneMult *= 3;
        if (targetLevel >= 100) milestoneMult *= 4;
        if (targetLevel >= 200) milestoneMult *= 5;
        if (targetLevel >= 300) milestoneMult *= 5;
        if (targetLevel >= 400) milestoneMult *= 10;
        if (targetLevel >= 500) milestoneMult *= 10;
        
        baseCap *= milestoneMult;
        // ------------------------------------------

        double prestigeMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.PrestigeMultiplier : 1.0;
        double roundMultiplier = Gamemanager.Instance != null ? Gamemanager.Instance.RoundMultiplier : 1.0;
        return baseCap * prestigeMultiplier * roundMultiplier;
    }

    public override double GetCapacity(int targetLevel) => GetWorkerCapacity(targetLevel);

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

    protected override void Update()
    {
        base.Update();
        
        if (IsSkillActive && currentManager != null && currentManager.SpecialFeature == SeniorSpecialFeature.SpecialFeature)
        {
            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0)
            {
                shootTimer = shootInterval;
                ShootProjectile();
            }
        }
    }

    private void ShootProjectile()
    {
        if (turretPos == null || projectilePrefab == null) return;
        
        Transform target = null;
        
        DragonBossController dragon = FindObjectOfType<DragonBossController>();
        if (dragon != null && dragon.gameObject.activeInHierarchy && dragon.GetComponent<Collider2D>().enabled)
        {
            IDamageable d = dragon.GetComponent<IDamageable>();
            if (d == null || !d.IsInvincible) target = dragon.transform;
        }
        
        if (target == null)
        {
            BossPhase2Controller boss2 = FindObjectOfType<BossPhase2Controller>();
            if (boss2 != null && boss2.gameObject.activeInHierarchy)
            {
                IDamageable d = boss2.GetComponent<IDamageable>();
                if (d == null || !d.IsInvincible) target = boss2.transform;
            }
            else
            {
                BossPhase3Controller boss3 = FindObjectOfType<BossPhase3Controller>();
                if (boss3 != null && boss3.gameObject.activeInHierarchy)
                {
                    IDamageable d = boss3.GetComponent<IDamageable>();
                    if (d == null || !d.IsInvincible) target = boss3.transform;
                }
            }
        }

        if (target != null)
        {
            // Bắn với góc khác nhau: Lắc nòng súng ngẫu nhiên lên xuống một chút (ví dụ -20 đến 20 độ)
            float randomAngle = UnityEngine.Random.Range(-20f, 20f);
            turretPos.localRotation = Quaternion.Euler(0f, 0f, randomAngle);

            // Bật hiệu ứng nòng súng (Muzzle Flash)
            if (shootVFX != null)
            {
                ParticleSystem[] pss = shootVFX.GetComponentsInChildren<ParticleSystem>();
                if (pss.Length > 0)
                {
                    // Nếu dùng Particle System, chỉ gọi Play() để tránh lỗi nháy đúp do SetActive
                    foreach (var ps in pss)
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.Play(true);
                    }
                }
                else
                {
                    // Nếu dùng Animation/Sprite thường, bật tắt để reset
                    shootVFX.SetActive(false);
                    shootVFX.SetActive(true);
                }
            }

            GameObject proj = Instantiate(projectilePrefab, turretPos.position, Quaternion.identity);
            // Lựa chọn offset bắn trúng đích tùy theo loại quái
            Vector3 offsetToUse = groundBossTargetOffset;
            if (target.GetComponent<DragonBossController>() != null || target.GetComponentInChildren<DragonBossController>() != null)
            {
                offsetToUse = flyingDragonTargetOffset;
            }

            WarehouseProjectile wp = proj.GetComponent<WarehouseProjectile>();
            if (wp == null) wp = proj.AddComponent<WarehouseProjectile>();

            // Lấy sát thương chuẩn 5% Máu Tối Đa của chính Mục Tiêu đó (Canonical Math)
            float finalDamage = 5f; // Fallback mặc định
            IDamageable damageableTarget = target.GetComponent<IDamageable>();
            if (damageableTarget != null)
            {
                finalDamage = damageableTarget.MaxHealth * 0.02f; // Nerf từ 5% xuống 2%
            }

            wp.Setup(target, finalDamage, offsetToUse);
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
                curVal = CurrencyFormatter.FormatMoney(GetTotalThroughputDisplay(currentLevel)) + "/s";
                nextVal = CurrencyFormatter.FormatMoney(GetTotalThroughputDisplay(nextLevel)) + "/s";
                break;
            case 1: // Số lượng nhân viên
                curVal = GetWorkersCount(currentLevel).ToString();
                nextVal = GetWorkersCount(nextLevel).ToString();
                break;
            case 2: // Sức chứa 1 người
                curVal = CurrencyFormatter.FormatMoney(GetWorkerCapacity(currentLevel));
                nextVal = CurrencyFormatter.FormatMoney(GetWorkerCapacity(nextLevel));
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

    protected override void ApplyManagerBuff()
    {
        base.ApplyManagerBuff();
        if (currentManager == null) return;

        float buffMultiplier = 1f + (currentManager.BuffValue / 100f);
        
        switch (currentManager.BuffType)
        {
            case ManagerBuffType.MoveSpeed:
                WorkerMoveSpeedBuff = buffMultiplier;
                break;
            case ManagerBuffType.MiningSpeed:
                WorkerLoadSpeedBuff = buffMultiplier;
                break;
        }
    }

    protected override void RemoveManagerBuff()
    {
        base.RemoveManagerBuff();
        WorkerMoveSpeedBuff = 1f;
        WorkerLoadSpeedBuff = 1f;
    }
}
