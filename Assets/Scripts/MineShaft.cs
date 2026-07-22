using UnityEngine;
using TMPro; // Thêm thư viện này để dùng TextMeshPro
using System.Collections.Generic; // Thêm thư viện dùng List

// Kế thừa Facility thay vì MonoBehaviour để có sẵn tính năng Nâng cấp
public class MineShaft : Facility 
{
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
    private List<Miner> activeMiners = new List<Miner>();


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
        UpdateUI();

        // Không gọi CheckAndSpawnMiners() ở đây nữa để tránh đẻ thêm lúc mới vào game
        // Hầm đã có sẵn 1 người gốc do bạn đặt tay.
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

        // Hầm luôn mặc định có sẵn 1 người (bản gốc bạn tự đặt), nên chỉ đẻ thêm phần dư
        int requiredSpawns = targetCount - 1;

        while (activeMiners.Count < requiredSpawns)
        {
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
}
