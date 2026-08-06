using UnityEngine;
using UnityEngine.EventSystems;

public class WorkerInput : MonoBehaviour
{
    private WorkerHealth workerHealth;
    private Miner miner;

    private void Awake()
    {
        workerHealth = GetComponent<WorkerHealth>();
        miner = GetComponent<Miner>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = GetComponent<Collider2D>();
            
            if (col != null && col.OverlapPoint(mouseWorldPos))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

                if (workerHealth != null && (workerHealth.healthState == WorkerHealth.HealthState.Injured || workerHealth.healthState == WorkerHealth.HealthState.Dead))
                {
                    QuickHeal();
                }
                else if (miner != null && miner.IsIdle() && miner.currentShaft != null && !miner.currentShaft.isBroken)
                {
                    miner.StartWalkingToDig();
                }
            }
        }
    }

    private void QuickHeal()
    {
        if (miner == null || miner.currentShaft == null || Gamemanager.Instance == null || workerHealth == null) return;
        
        if (workerHealth.healthState == WorkerHealth.HealthState.Dead)
        {
            double workerProductivity = miner.currentShaft.GetWorkerProductivity(miner.currentShaft.Level);
            double cost = workerProductivity * 3; 
            if (Gamemanager.Instance.DeductCash(cost))
            {
                workerHealth.Revive();
            }
        }
        else if (workerHealth.healthState == WorkerHealth.HealthState.Injured)
        {
            float missingMorale = workerHealth.maxMorale - workerHealth.morale;
            double workerProductivity = miner.currentShaft.GetWorkerProductivity(miner.currentShaft.Level);
            double cost = workerProductivity * 0.03 * missingMorale;
            if (Gamemanager.Instance.DeductCash(cost))
            {
                workerHealth.Cleanse();
            }
        }
    }
}
