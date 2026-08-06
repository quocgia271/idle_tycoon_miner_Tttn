using UnityEngine;

public class WarehouseWorker : MonoBehaviour
{
    public enum WorkerState
    {
        Idle,
        WalkingToElevator,
        Loading,
        WalkingToDeposit,
        Depositing
    }

    [Header("References")]
    public Warehouse warehouse;
    public Transform myDepositPos;
    
    private WorkerState currentState = WorkerState.Idle;
    private double currentLoad = 0;
    private float currentTimer = 0f;
    
    public float currentSpeedMultiplier = 1f;

    private WorkerMovement workerMovement;
    private WorkerAnimation workerAnimation;
    private WorkerUI workerUI;

    private void Awake()
    {
        workerMovement = GetComponent<WorkerMovement>();
        workerAnimation = GetComponent<WorkerAnimation>();
        workerUI = GetComponent<WorkerUI>();

        if (workerMovement == null) workerMovement = gameObject.AddComponent<WorkerMovement>();
    }

    private void Start()
    {
        ChangeState(WorkerState.Idle);
    }

    private void Update()
    {
        if (warehouse == null) return;

        if (currentState == WorkerState.Idle)
        {
            if (warehouse.currentManager != null && warehouse.elevator != null && warehouse.elevator.DroppedResource > 0)
            {
                ChangeState(WorkerState.WalkingToElevator);
            }
        }
        else if (currentState == WorkerState.Loading)
        {
            HandleLoading();
        }
        else if (currentState == WorkerState.Depositing)
        {
            HandleDepositing();
        }
    }

    public void ManualStart()
    {
        if (currentState == WorkerState.Idle)
        {
            ChangeState(WorkerState.WalkingToElevator);
        }
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            var pointerEventData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current)
            {
                position = Input.mousePosition
            };
            var raycastResults = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerEventData, raycastResults);
            if (raycastResults.Count > 0)
            {
                Canvas parentCanvas = raycastResults[0].gameObject.GetComponentInParent<Canvas>();
                string canvasName = parentCanvas != null ? parentCanvas.name : "Unknown Canvas";
                Debug.LogError($"[TÌM THẤY THỦ PHẠM CHẶN CLICK] Bạn vừa click trúng cái UI có tên là: '{raycastResults[0].gameObject.name}' (nằm trong Canvas '{canvasName}'). Hãy tìm cái này trong Hierarchy và TẮT dấu tick 'Raycast Target' của nó đi nhé!");
            }
            return;
        }

        if (warehouse != null && warehouse.elevator != null && warehouse.elevator.DroppedResource > 0)
        {
            ManualStart();
        }
    }

    private void HandleLoading()
    {
        currentTimer += Time.deltaTime;
        if (currentTimer >= warehouse.loadTime)
        {
            currentTimer = 0f;
            if (warehouse.elevator != null)
            {
                currentLoad = warehouse.elevator.TakeResource(warehouse.Capacity);
            }
            else
            {
                currentLoad = 0;
            }
            
            if (warehouse != null) warehouse.RemoveLoadingWorker();
            ChangeState(WorkerState.WalkingToDeposit);
        }
    }

    private void HandleDepositing()
    {
        currentTimer += Time.deltaTime;
        if (currentTimer >= warehouse.loadTime)
        {
            currentTimer = 0f;
            
            if (Gamemanager.Instance != null && currentLoad > 0)
            {
                Gamemanager.Instance.AddCash(currentLoad);
                if (workerUI != null) workerUI.TriggerFloatingText();
            }
            
            currentLoad = 0;
            ChangeState(WorkerState.Idle);
        }
    }

    private void ChangeState(WorkerState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case WorkerState.Idle:
                if (workerAnimation != null) workerAnimation.PlayAnimTrigger("idle");
                if (workerUI != null) workerUI.HideMoneyText();
                if (workerMovement != null) workerMovement.StopMoving();
                
                // Khi đứng chờ thang máy (bên trái), quay mặt sang trái (Y=0)
                transform.rotation = Quaternion.Euler(0, 0, 0);
                break;

            case WorkerState.WalkingToElevator:
                if (workerAnimation != null) workerAnimation.PlayAnimTrigger("push");
                if (workerUI != null) workerUI.HideMoneyText();
                
                workerMovement.MoveTo(warehouse.elevatorPos.position, warehouse.moveSpeed * currentSpeedMultiplier, () => ChangeState(WorkerState.Loading));
                break;

            case WorkerState.Loading:
                if (workerAnimation != null) workerAnimation.PlayAnimTrigger("idle");
                if (workerUI != null) workerUI.StartLoadingProgress(warehouse.loadTime);
                if (warehouse != null) warehouse.AddLoadingWorker();
                break;

            case WorkerState.WalkingToDeposit:
                if (workerAnimation != null) workerAnimation.PlayAnimTrigger("push");
                if (workerUI != null && currentLoad > 0) workerUI.ShowMoneyText(currentLoad);
                
                Vector3 targetDep = myDepositPos != null ? myDepositPos.position : warehouse.depositPos.position;
                workerMovement.MoveTo(targetDep, warehouse.moveSpeed * currentSpeedMultiplier, () => ChangeState(WorkerState.Depositing));
                break;

            case WorkerState.Depositing:
                if (workerAnimation != null) workerAnimation.PlayAnimTrigger("idle");
                if (workerUI != null) workerUI.StartLoadingProgress(warehouse.loadTime);
                break;
        }
    }
}
