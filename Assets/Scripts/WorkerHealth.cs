using UnityEngine;
using System;

public class WorkerHealth : MonoBehaviour
{
    public enum HealthState { Normal, Injured, Dead }
    
    [Header("Morale System")]
    public float morale = 100f;
    public float maxMorale = 100f;
    public HealthState healthState = HealthState.Normal;
    
    // Events để giao tiếp lỏng lẻo (Loose Coupling)
    public event Action<float, float> OnMoraleChanged; // current, max
    public event Action<BossPhase2Controller> OnInjured;
    public event Action<bool> OnDied; // isOffline
    public event Action OnRevived;
    public event Action OnCleansed;

    private BossPhase2Controller currentBoss;
    private float dotTimer = 0f;
    private float hauntTimer = 0f;

    private void Start()
    {
        OnMoraleChanged?.Invoke(morale, maxMorale);
    }

    private void Update()
    {
        if (healthState == HealthState.Injured && currentBoss != null)
        {
            if (currentBoss.dotDuration > 0)
            {
                hauntTimer += Time.deltaTime;
                if (hauntTimer >= currentBoss.dotDuration)
                {
                    Cleanse();
                    return;
                }
            }

            dotTimer += Time.deltaTime;
            if (dotTimer >= currentBoss.dotInterval)
            {
                dotTimer = 0f;
                morale -= currentBoss.dotDamage;
                OnMoraleChanged?.Invoke(morale, maxMorale);
                
                if (morale <= 0f)
                {
                    DieFromHaunt();
                }
            }
        }
    }

    public void AddMorale(float amount)
    {
        morale = Mathf.Clamp(morale + amount, 0f, maxMorale);
        OnMoraleChanged?.Invoke(morale, maxMorale);
    }

    public void ApplyHaunt(BossPhase2Controller boss)
    {
        if (healthState == HealthState.Injured) return;
        healthState = HealthState.Injured;
        currentBoss = boss;
        hauntTimer = 0f;
        dotTimer = 0f;
        OnInjured?.Invoke(boss);
    }

    public void DieFromHaunt()
    {
        healthState = HealthState.Dead;
        morale = 0f;
        OnDied?.Invoke(false);
    }

    public void SetDeadStateOffline()
    {
        healthState = HealthState.Dead;
        morale = 0f;
        OnDied?.Invoke(true); 
    }

    public void Revive()
    {
        if (healthState == HealthState.Normal && morale >= maxMorale) return;
        healthState = HealthState.Normal;
        morale = maxMorale;
        OnMoraleChanged?.Invoke(morale, maxMorale);
        OnRevived?.Invoke();
    }

    public void Cleanse()
    {
        if (healthState != HealthState.Injured) return;
        healthState = HealthState.Normal;
        AddMorale(25f);
        OnCleansed?.Invoke();
    }
    
    public void LoadState(int savedHealthState, float savedMorale)
    {
        this.morale = savedMorale;
        this.healthState = (HealthState)savedHealthState;
        OnMoraleChanged?.Invoke(morale, maxMorale);
        
        if (this.healthState == HealthState.Injured)
        {
            BossPhase2Controller boss = FindObjectOfType<BossPhase2Controller>(true);
            if (boss != null && boss.gameObject.activeInHierarchy)
            {
                currentBoss = boss;
                OnInjured?.Invoke(boss);
            }
            else Cleanse();
        }
        else if (this.healthState == HealthState.Dead)
        {
            SetDeadStateOffline();
        }
    }
}
