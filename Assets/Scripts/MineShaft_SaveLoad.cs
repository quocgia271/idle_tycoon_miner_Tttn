using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public partial class MineShaft : Facility 
{
    // =====================================
    // LƯU TRỮ VÀ TẢI DỮ LIỆU (SAVE/LOAD)
    // =====================================
    public override FacilitySaveData SaveState()
    {
        FacilitySaveData data = base.SaveState();
        data.Index = this.ShaftIndex;
        
        ShaftUnlocker unlocker = GetComponentInChildren<ShaftUnlocker>(true);
        // Hầm được coi là đã mở khóa nếu: UI Unlock bị tắt, HOẶC đã mua (đang chờ xây), HOẶC đang bị hỏng
        data.IsUnlocked = unlocker == null || !unlocker.gameObject.activeSelf || unlocker.isPurchased || this.isBroken;
        
        data.CurrentResource = this.CurrentResource;
        data.CurrentEndurance = this.currentEndurance;
        data.IsBroken = this.isBroken;
        data.NormalBurnTimer = this.normalBurnTimer;
        data.BigBurnTimer = this.bigBurnTimer;
        data.FireClicksRemaining = this.fireClicksRemaining;
        
        data.Skill3Timer = this.currentSkill3Timer;
        data.Skill3DPS = this.currentSkill3DPS;
        data.Skill3ColorIndex = this.currentSkill3Index;
        
        // Cập nhật máu Minion đang bám trên hầm
        data.MinionHealth = 0f;
        MinionController[] minions = GetComponentsInChildren<MinionController>(true);
        foreach (var minion in minions)
        {
            if (minion.gameObject.activeInHierarchy && !minion.IsDead)
            {
                data.MinionHealth = minion.CurrentHealth;
                break; // Chỉ lấy 1 con đầu tiên
            }
        }
        
        data.MinersData.Clear();
        foreach (var m in activeMiners)
        {
            if (m != null)
            {
                WorkerHealth wh = m.GetComponent<WorkerHealth>();
                data.MinersData.Add(new MinerSaveData {
                    Morale = wh != null ? wh.morale : 100f,
                    HealthState = wh != null ? (int)wh.healthState : 0
                });
            }
        }
        
        return data;
    }

    public override void LoadState(FacilitySaveData data)
    {
        base.LoadState(data); // Gọi hàm cha để nạp Level
        
        if (data == null) return;
        
        this.CurrentResource = data.CurrentResource;
        this.currentEndurance = (data.CurrentEndurance == 0 && !data.IsBroken) ? maxEndurance : data.CurrentEndurance; 
        
        ShaftUnlocker unlocker = GetComponentInChildren<ShaftUnlocker>(true);
        
        if (data.IsBroken)
        {
            BreakShaft(true); // Khôi phục trạng thái vỡ mà không play VFX nổ
        }
        else
        {
            // Tắt UI khóa hầm nếu đã mở khóa
            if (data.IsUnlocked && unlocker != null)
            {
                unlocker.HideLockInstantly();
                unlocker.gameObject.SetActive(false);
            }
            // Nếu chưa mở khóa, đảm bảo bật UI lên
            else if (!data.IsUnlocked && unlocker != null)
            {
                unlocker.gameObject.SetActive(true);
            }
            
            UpdateEnduranceUI();
            
            CheckAndSpawnMiners();
            if (data.MinersData != null && data.MinersData.Count > 0)
            {
                for (int i = 0; i < data.MinersData.Count && i < activeMiners.Count; i++)
                {
                    if (activeMiners[i] != null)
                    {
                        activeMiners[i].LoadState(data.MinersData[i]);
                    }
                }
            }

            // Phục hồi hiệu ứng lửa
            if (data.NormalBurnTimer > 0) TriggerBurnVFX(data.NormalBurnTimer, false);
            if (data.BigBurnTimer > 0) TriggerBurnVFX(data.BigBurnTimer, true);
            this.fireClicksRemaining = data.FireClicksRemaining;
            
            // Phục hồi Skill 3
            if (data.Skill3Timer > 0)
            {
                TriggerSkill3VFX(data.Skill3Timer, data.Skill3DPS, data.Skill3ColorIndex);
            }
        }
        
        // Bắt buộc gọi OnUpgraded để update lại các chỉ số dựa trên Level mới nạp
        UpdateUI();
    }

    public override void PopulateSaveData(SaveData data)
    {
        data.MineShafts.Add(this.SaveState());
    }

    public override void LoadFromSaveData(SaveData data)
    {
        var savedData = data.MineShafts.Find(s => s.Index == this.ShaftIndex);
        if (savedData != null) 
        {
            this.LoadState(savedData);
        }
    }
}
