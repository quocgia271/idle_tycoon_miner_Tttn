using UnityEngine;

public class Miner : MonoBehaviour
{
    public enum MinerState
    {
        Idle,
        WalkingToDig,
        Digging,
        WalkingBack
    }

    [Header("References")]
    public MineShaft currentShaft;
    
    [Header("Positions")]
    public Transform startPos;
    public Transform digPos;

    public float moveSpeed => currentShaft != null ? currentShaft.GetMinerMoveSpeed(currentShaft.Level) : 2f;
    public float digTime => currentShaft != null ? currentShaft.GetMinerDigTime(currentShaft.Level) : 2f;

    private MinerState currentState = MinerState.Idle;
    private float currentDigTime = 0f;
    public bool isReviving = false;
    
    private WorkerHealth workerHealth;
    private WorkerAnimation workerAnimation;
    private WorkerUI workerUI;
    private WorkerMovement workerMovement;

    private void Awake()
    {
        workerHealth = GetComponent<WorkerHealth>();
        workerAnimation = GetComponent<WorkerAnimation>();
        workerUI = GetComponent<WorkerUI>();
        workerMovement = GetComponent<WorkerMovement>();
        
        // Đảm bảo có WorkerMovement (tự động thêm nếu thiếu)
        if (workerMovement == null)
            workerMovement = gameObject.AddComponent<WorkerMovement>();
            
        // Fix lỗi đi ngược: Sprite của Miner được vẽ mặc định quay sang phải (khác với Warehouse quay trái)
        workerMovement.flipRotation = true;
    }

    private void Update()
    {
        // Nếu đã chết thì không làm gì cả
        if (workerHealth != null && workerHealth.healthState == WorkerHealth.HealthState.Dead) return;
        if (isReviving) return;

        // Tự động đi làm nếu hầm có quản lý và không hỏng
        if (currentState == MinerState.Idle && currentShaft != null && currentShaft.currentManager != null && !currentShaft.isBroken)
        {
            ChangeState(MinerState.WalkingToDig);
        }

        if (currentState == MinerState.Digging)
        {
            HandleDigging();
        }
    }

    private void HandleDigging()
    {
        currentDigTime += Time.deltaTime;
        if (currentDigTime >= digTime)
        {
            currentDigTime = 0f;
            ChangeState(MinerState.WalkingBack);
        }
    }

    private void ChangeState(MinerState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case MinerState.Idle:
                if (workerAnimation != null) workerAnimation.PlayAnimTrigger("idle");
                
                // Khi đứng rảnh rỗi chờ ở thang máy, Miner luôn hướng mặt về hầm (bên phải -> Y=0)
                transform.rotation = Quaternion.Euler(0, 0, 0);

                if (currentShaft != null)
                {
                    double resourceGathered = currentShaft.ResourcePerSecond * digTime;
                    currentShaft.AddResource(resourceGathered);
                }
                break;

            case MinerState.WalkingToDig:
                if (workerAnimation != null) workerAnimation.PlayAnimTrigger("walk");
                workerMovement.MoveTo(digPos.position, moveSpeed, () => ChangeState(MinerState.Digging));
                break;

            case MinerState.Digging:
                if (workerAnimation != null) workerAnimation.PlayAnimTrigger("digup");
                if (workerUI != null) workerUI.StartLoadingProgress(digTime);
                break;

            case MinerState.WalkingBack:
                if (workerAnimation != null) workerAnimation.PlayAnimTrigger("walk");
                workerMovement.MoveTo(startPos.position, moveSpeed, () => ChangeState(MinerState.Idle));
                break;
        }
    }

    public bool IsIdle()
    {
        return currentState == MinerState.Idle;
    }

    public void StartWalkingToDig()
    {
        if (currentState == MinerState.Idle)
        {
            ChangeState(MinerState.WalkingToDig);
        }
    }
    
    public void ResetStateToIdle()
    {
        currentState = MinerState.Idle;
        if (workerMovement != null) workerMovement.StopMoving();
    }
    
    public void LoadState(MinerSaveData data)
    {
        if (workerHealth != null)
        {
            workerHealth.LoadState(data.HealthState, data.Morale);
        }
    }
}
