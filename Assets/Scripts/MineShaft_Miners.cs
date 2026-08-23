using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public partial class MineShaft : Facility 
{
    [Header("Miner Spawn Settings")]
    public Miner minerPrefab;
    public Transform minerStartPos;
    public Transform minerDigPos;
    public float spawnOffsetX = 0.5f;
    public List<Miner> activeMiners = new List<Miner>();

    public int GetMinersCount(int targetLevel)
    {
        int max = Config != null ? Config.MaxWorkers : 5;
        int levelsPerWorker = Config != null ? Config.LevelsPerWorker : 10;
        
        int count = 1 + (targetLevel / levelsPerWorker);
        return Mathf.Min(count, max);
    }

    public float GetMinerMoveSpeed(int targetLevel)
    {
        float maxSpeed = Config != null ? Config.MaxSpeed : 5f;
        float speedInc = Config != null ? Config.SpeedIncreasePerLevel : 0.05f;
        float baseSpeed = Config != null ? Config.BaseSpeed : 2f;
        
        float speed = baseSpeed + (targetLevel * speedInc);
        speed = Mathf.Min(speed, maxSpeed);
        return speed * MinerMoveSpeedBuff;
    }

    public float GetMinerDigTime(int targetLevel)
    {
        float baseTime = Config != null ? Config.BaseActionTime : 2f;
        float decrease = Config != null ? Config.ActionTimeDecreasePerLevel : 0.01f;
        float minTime = Config != null ? Config.MinActionTime : 0.5f;
        
        float digTime = baseTime - (targetLevel * decrease);
        digTime = Mathf.Max(digTime, minTime);
        return digTime / MinerDigSpeedBuff;
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
}
