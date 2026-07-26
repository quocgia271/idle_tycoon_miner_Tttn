using UnityEngine;
using TMPro; // Thêm thư viện này để dùng TextMeshPro
using System.Collections.Generic; // Thêm thư viện dùng List
using DG.Tweening; // Thư viện tạo hiệu ứng DOTween

// Kế thừa Facility thay vì MonoBehaviour để có sẵn tính năng Nâng cấp
public class MineShaft : Facility 
{
    public override FacilityType GetFacilityType() => FacilityType.MineShaft;

    public double CurrentResource = 0; 
    
    public double BaseResourcePerSecond = 10; 
    
    [Header("Miner Settings")]
    public int MaxMiners = 5;
    public float MaxMoveSpeed = 5f;
    public float MinDigTime = 0.5f;

    [Header("Miner Spawn Settings")]
    public Miner minerPrefab;
    public Transform minerStartPos;
    public Transform minerDigPos;
    public float spawnOffsetX = 0.5f;
    public List<Miner> activeMiners = new List<Miner>();

    [Header("Manager Settings")]
    // Các buff này sẽ nằm chung dưới thẻ Manager Buffs của lớp cha Facility
    public float MinerMoveSpeedBuff = 1f;
    public float MinerDigSpeedBuff = 1f;
    public float ProductivityBuff = 1f;

    // Năng suất của một thợ mỏ
    public double ResourcePerSecond => GetWorkerProductivity(Level); 

    public int GetMinersCount(int targetLevel)
    {
        // Mỗi 10 cấp thêm 1 thợ, tối đa là MaxMiners
        int count = 1 + (targetLevel / 10);
        return Mathf.Min(count, MaxMiners);
    }

    public float GetMinerMoveSpeed(int targetLevel)
    {
        float speed = Config != null ? Config.BaseSpeed + (targetLevel * 0.05f) : 2f;
        speed = Mathf.Min(speed, MaxMoveSpeed);
        return speed * MinerMoveSpeedBuff;
    }

    public float GetMinerDigTime(int targetLevel)
    {
        float digTime = 2f - (targetLevel * 0.01f);
        digTime = Mathf.Max(digTime, MinDigTime);
        return digTime / MinerDigSpeedBuff;
    }

    public double GetWorkerProductivity(int targetLevel)
    {
        return (BaseResourcePerSecond * targetLevel) * ProductivityBuff;
    }

    public double GetTotalExtractionPerSecond(int targetLevel)
    {
        // Tổng lượng đào = Năng suất 1 thợ * Số lượng thợ
        return GetWorkerProductivity(targetLevel) * GetMinersCount(targetLevel);
    }

    [Header("UI")]
    public TextMeshProUGUI shaftCashText; // Hiển thị số tiền/tài nguyên hiện tại của hầm

    protected override void Start()
    {
        base.Start(); // Gọi hàm Start của lớp cha (Facility) để update text Level
        
        // Sửa lỗi: Unity tự động khởi tạo class [Serializable] làm hầm bị kẹt một quản lý "ảo" từ đầu
        currentManager = null; 

        // Tìm thợ mỏ có sẵn trong cảnh (con của hầm) và thêm vào danh sách nếu chưa có
        Miner[] existingMiners = GetComponentsInChildren<Miner>();
        foreach (Miner m in existingMiners)
        {
            if (!activeMiners.Contains(m))
            {
                m.currentShaft = this;
                activeMiners.Add(m);
            }
        }

        UpdateUI();
    }

    protected override void ApplyManagerBuff()
    {
        base.ApplyManagerBuff();
        if (currentManager == null) return;
        
        float buffMultiplier = 1f + (currentManager.BuffValue / 100f);
        float costDiscount = 1f - (currentManager.BuffValue / 100f);

        switch (currentManager.BuffType)
        {
            case ManagerBuffType.MoveSpeed:
                MinerMoveSpeedBuff = buffMultiplier;
                break;
            case ManagerBuffType.MiningSpeed:
                MinerDigSpeedBuff = buffMultiplier;
                break;
                break;
        }
    }

    protected override void RemoveManagerBuff()
    {
        base.RemoveManagerBuff();
        MinerMoveSpeedBuff = 1f;
        MinerDigSpeedBuff = 1f;
        UpgradeCostDiscount = 1f;
        UpdateUpgradeUI();
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
        // Cập nhật số lượng thợ mỏ nếu đạt đủ level
        CheckAndSpawnMiners();
    }

    private void CheckAndSpawnMiners()
    {
        if (minerPrefab == null || minerStartPos == null || minerDigPos == null) return;

        int targetCount = GetMinersCount(Level);

        // Vì lúc Start() chúng ta đã tự tìm và add người đầu tiên vào activeMiners
        // Nên bây giờ activeMiners.Count đã phản ánh đúng số lượng thực tế
        while (activeMiners.Count < targetCount)
        {
            SpawnSingleMiner();
        }
    }

    private void SpawnSingleMiner()
    {
        if (minerPrefab == null || minerStartPos == null || minerDigPos == null) return;
        
        // Đánh số thứ tự bắt đầu từ 1 để nó lùi về sau lưng con gốc
        int index = activeMiners.Count + 1; 
        
        // Tính toán vị trí lùi về sau (bên trái) theo X
        Vector3 spawnPos = minerStartPos.position - new Vector3(spawnOffsetX * index, 0, 0);

        Miner newMiner = Instantiate(minerPrefab, spawnPos, Quaternion.identity, transform);
        newMiner.currentShaft = this;
        
        // Gán lại startPos ảo cho thợ mỏ này bằng một object rỗng tạo ra tại chỗ
        GameObject tempStart = new GameObject($"StartPos_Miner_{index}");
        tempStart.transform.position = spawnPos;
        tempStart.transform.SetParent(transform);

        newMiner.startPos = tempStart.transform;
        newMiner.digPos = minerDigPos;

        activeMiners.Add(newMiner);
    }

    public void BuyBackMiner(double cost)
    {
        int targetCount = GetMinersCount(Level);
        if (activeMiners.Count < targetCount)
        {
            if (Gamemanager.Instance != null && Gamemanager.Instance.IdleCash >= cost)
            {
                Gamemanager.Instance.IdleCash -= cost;
                SpawnSingleMiner();
            }
            else
            {
                Debug.LogWarning("Không đủ tiền mua lại thợ mỏ!");
            }
        }
        else
        {
            Debug.LogWarning("Hầm đã đầy đủ nhân viên, không cần mua lại.");
        }
    }

    public override (string curVal, string nextVal) GetStatDisplay(int statIndex, int currentLevel, int nextLevel)
    {
        string curVal = "0";
        string nextVal = "0";
        
        switch (statIndex)
        {
            case 0: // Tổng khai thác
                curVal = GetTotalExtractionPerSecond(currentLevel).ToString("F1") + "/s";
                nextVal = GetTotalExtractionPerSecond(nextLevel).ToString("F1") + "/s";
                break;
            case 1: // Số thợ mỏ
                curVal = GetMinersCount(currentLevel).ToString();
                nextVal = GetMinersCount(nextLevel).ToString();
                break;
            case 2: // Tốc độ di chuyển
                curVal = GetMinerMoveSpeed(currentLevel).ToString("F2");
                nextVal = GetMinerMoveSpeed(nextLevel).ToString("F2");
                break;
            case 3: // Tốc độ khai thác (thời gian cuốc đất)
                curVal = GetMinerDigTime(currentLevel).ToString("F2") + "s";
                nextVal = GetMinerDigTime(nextLevel).ToString("F2") + "s";
                break;
            case 4: // Năng suất 1 thợ mỏ
                curVal = GetWorkerProductivity(currentLevel).ToString("F1") + "/s";
                nextVal = GetWorkerProductivity(nextLevel).ToString("F1") + "/s";
                break;
        }

        return (curVal, nextVal);
    }

    // ==========================================
    // LOGIC BỊ ĐỐT CHÁY BỞI RỒNG
    // ==========================================
    [Header("VFX Hầm")]
    public GameObject normalBurnVFX; // Lửa nhỏ cho đạn thường
    public GameObject bigBurnVFX;    // Lửa to cho đạn bự

    private float normalBurnTimer = 0f;
    private float bigBurnTimer = 0f;
    private Coroutine burnCoroutine;

    public void TriggerBurnVFX(float duration, bool isBig)
    {
        if (isBig)
        {
            // Trúng đạn to: Tắt ngay lửa nhỏ, cộng dồn thời gian lửa to
            normalBurnTimer = 0f; 
            if (normalBurnVFX != null) normalBurnVFX.SetActive(false);
            bigBurnTimer += duration;
        }
        else
        {
            // Trúng đạn nhỏ: Cộng dồn thời gian lửa nhỏ (nếu bị trúng liên tục)
            normalBurnTimer += duration;
        }

        // Bật hệ thống đếm ngược nếu nó chưa chạy
        if (burnCoroutine == null)
        {
            burnCoroutine = StartCoroutine(BurnTimerRoutine());
        }
    }

    private System.Collections.IEnumerator BurnTimerRoutine()
    {
        while (normalBurnTimer > 0 || bigBurnTimer > 0)
        {
            // Xử lý lửa to
            if (bigBurnTimer > 0)
            {
                if (bigBurnVFX != null && !bigBurnVFX.activeSelf) bigBurnVFX.SetActive(true);
                bigBurnTimer -= Time.deltaTime;
                
                if (bigBurnTimer <= 0 && bigBurnVFX != null) 
                    bigBurnVFX.SetActive(false);
            }

            // Xử lý lửa nhỏ
            if (normalBurnTimer > 0)
            {
                if (normalBurnVFX != null && !normalBurnVFX.activeSelf) normalBurnVFX.SetActive(true);
                normalBurnTimer -= Time.deltaTime;
                
                if (normalBurnTimer <= 0 && normalBurnVFX != null) 
                    normalBurnVFX.SetActive(false);
            }

            yield return null; // Chờ frame tiếp theo
        }
        
        burnCoroutine = null; // Khi cả 2 lửa đều tắt, reset coroutine
    }
}
