using UnityEngine;
using System.Collections;
using TMPro;

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
    public Warehouse warehouse; // Tham chiếu đến nhà kho quản lý nhân viên này
    public Animator anim;
    public ProgressBar progressBar;
    public TextMeshProUGUI moneyText; // Text hiển thị tiền và bay lên

    private WorkerState currentState = WorkerState.Idle;
    private double currentLoad = 0;
    private float currentTimer = 0f;
    private Vector3 initialScale;
    private Vector3 initialTextLocalPos;
    private bool isFloatingText = false;

    private void Start()
    {
        initialScale = transform.localScale;
        
        if (moneyText != null)
        {
            initialTextLocalPos = moneyText.transform.localPosition;
            moneyText.gameObject.SetActive(false); // Ẩn text đi lúc đầu
        }

        ChangeState(WorkerState.Idle); // Bắt đầu ở trạng thái đứng chờ
    }

    private void Update()
    {
        if (warehouse == null) return; // Không có nhà kho thì không làm gì cả

        switch (currentState)
        {
            case WorkerState.Idle:
                // CHỈ đi lấy tiền khi thang máy thực sự có tiền
                if (warehouse.elevator != null && warehouse.elevator.DroppedResource > 0)
                {
                    ChangeState(WorkerState.WalkingToElevator);
                }
                break;
            case WorkerState.WalkingToElevator:
                MoveTowards(warehouse.elevatorPos.position, WorkerState.Loading);
                break;
            case WorkerState.Loading:
                HandleLoading();
                break;
            case WorkerState.WalkingToDeposit:
                MoveTowards(warehouse.depositPos.position, WorkerState.Depositing);
                break;
            case WorkerState.Depositing:
                HandleDepositing();
                break;
        }
    }

    private void MoveTowards(Vector3 targetPos, WorkerState nextState)
    {
        Vector3 targetPosXOnly = new Vector3(targetPos.x, transform.position.y, transform.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosXOnly, warehouse.moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosXOnly) < 0.01f)
        {
            ChangeState(nextState);
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
                // Sức chứa của nhân viên lấy từ sức chứa của kho
                currentLoad = warehouse.elevator.TakeResource(warehouse.Capacity);
                Debug.Log($"Công nhân vừa lấy được {currentLoad} tiền từ thang máy.");
            }
            else
            {
                currentLoad = 0;
            }
            
            // Xong load -> báo cho nhà kho biết mình đã lấy xong (để kho tắt VFX nếu cần)
            if (warehouse != null) warehouse.RemoveLoadingWorker();
            
            // đi nạp tiền
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
                StartCoroutine(ShowFloatingText());
            }
            
            currentLoad = 0;

            // Xong nạp -> Quay lại trạng thái chờ (Idle) để đợi mẻ tiền tiếp theo
            ChangeState(WorkerState.Idle);
        }
    }

    private void ChangeState(WorkerState newState)
    {
        currentState = newState;
        Vector3 currentScale = transform.localScale;

        switch (currentState)
        {
            case WorkerState.Idle:
                if (anim != null) anim.SetTrigger("idle");
                // Khi nạp xong đứng chờ, tự động quay mặt về phía thang máy để "ngóng" (scale x = -1)
                currentScale.x = -Mathf.Abs(initialScale.x);
                transform.localScale = currentScale;
                FlipTextIfNeeded();
                break;

            case WorkerState.WalkingToElevator:
                if (anim != null) anim.SetTrigger("push");
                // Hướng mặt về bên trái (đi tới thang máy) (scale x = -1)
                currentScale.x = -Mathf.Abs(initialScale.x);
                transform.localScale = currentScale;
                FlipTextIfNeeded();
                
                // Ẩn text nếu đang đi lấy tiền
                if (moneyText != null && !isFloatingText) 
                {
                    moneyText.gameObject.SetActive(false);
                }
                break;

            case WorkerState.Loading:
                if (anim != null) anim.SetTrigger("idle");
                if (progressBar != null) progressBar.StartLoading(warehouse.loadTime);
                
                // Báo cho nhà kho biết mình đang lấy tiền (để kho bật VFX)
                if (warehouse != null) warehouse.AddLoadingWorker();
                break;

            case WorkerState.WalkingToDeposit:
                if (anim != null) anim.SetTrigger("push");
                // Hướng mặt về bên phải (đi nạp tiền) (scale x = 1)
                currentScale.x = Mathf.Abs(initialScale.x);
                transform.localScale = currentScale;
                FlipTextIfNeeded();
                
                // Show text số tiền đang giữ khi đi về kho
                if (moneyText != null && currentLoad > 0)
                {
                    moneyText.gameObject.SetActive(true);
                    moneyText.text = CurrencyFormatter.FormatMoney(currentLoad);
                    
                    // Nếu đang bay thì dừng lại và reset
                    if (isFloatingText)
                    {
                        StopAllCoroutines(); 
                        ResetTextState();
                    }
                }
                else if (currentLoad <= 0)
                {
                    Debug.Log("Tiền lấy được = 0 nên sẽ không hiện Text!");
                }
                break;

            case WorkerState.Depositing:
                if (anim != null) anim.SetTrigger("idle");
                if (progressBar != null) progressBar.StartLoading(warehouse.loadTime);
                break;
        }
    }

    private void FlipTextIfNeeded()
    {
        if (moneyText == null) return;
        Vector3 tScale = moneyText.transform.localScale;
        
        // Tránh lỗi ngược chữ (Backface Culling làm chữ bị tàng hình) khi nhân vật bị lật Scale.
        // Nhân vật đi về kho (bên phải -> Scale X dương) thì ta lật âm Scale chữ lại để nó vẫn hướng ra màn hình.
        if (transform.localScale.x > 0)
        {
            tScale.x = -Mathf.Abs(tScale.x);
        }
        else
        {
            tScale.x = Mathf.Abs(tScale.x);
        }
        
        moneyText.transform.localScale = tScale;
    }

    private void ResetTextState()
    {
        if (moneyText == null) return;
        isFloatingText = false;
        moneyText.transform.localPosition = initialTextLocalPos;
        Color c = moneyText.color;
        c.a = 1f;
        moneyText.color = c;
    }

    private IEnumerator ShowFloatingText()
    {
        if (moneyText == null) yield break;
        
        isFloatingText = true;
        
        // Dùng vị trí World (Tuyệt đối) thay vì Local, để lúc nhân vật quay lưng đi lấy tiền
        // thì cái Text vẫn đứng nguyên ở kho và bay lên, không bị trôi theo nhân vật.
        Vector3 startWorldPos = moneyText.transform.position;
        Vector3 targetWorldPos = startWorldPos + new Vector3(0, 2f, 0); // Bay lên 2 unit
        
        float duration = 1.0f;
        float time = 0;
        
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            
            // Bay lên và mờ dần tại chỗ
            moneyText.transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, t);
            
            Color c = moneyText.color;
            c.a = Mathf.Lerp(1f, 0f, t);
            moneyText.color = c;
            
            yield return null;
        }
        
        moneyText.gameObject.SetActive(false);
        ResetTextState();
    }
}
