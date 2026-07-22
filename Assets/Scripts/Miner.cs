using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Thêm thư viện để check click UI

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
    public Animator anim;
    
    [Header("Positions")]
    public Transform startPos;
    public Transform digPos;

    [Header("Settings")]
    public float moveSpeed = 2f;
    public float digTime = 2f; // Thời gian đào mỗi lần

    [Header("UI")]
    public ProgressBar progressBar; // Kéo thả cục Prefab chứa script ProgressBar vào đây

    private MinerState currentState = MinerState.Idle;
    private float currentDigTime = 0f;
    private Vector3 initialScale;

    private void Start()
    {
        initialScale = transform.localScale;

        // Gọi ngay animation Idle lúc vừa vào game
        if (anim != null) 
        {
            anim.SetTrigger("idle");
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case MinerState.WalkingToDig:
                MoveTowards(digPos.position, MinerState.Digging);
                break;

            case MinerState.Digging:
                HandleDigging();
                break;

            case MinerState.WalkingBack:
                MoveTowards(startPos.position, MinerState.Idle);
                break;
        }
    }

    private void OnMouseDown()
    {
        // Kiểm tra xem ngón tay/con trỏ chuột có đang nằm trên UI nào không
        // Nếu có thì return luôn, không xử lý click vật lý của Hầm mỏ nữa
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Debug.Log("Miner clicked! Current state: " + currentState);
        // Chỉ cho phép click bắt đầu đi khi đang đứng chơi (Idle)
        if (currentState == MinerState.Idle)
        {
            ChangeState(MinerState.WalkingToDig);
        }
    }

    private void MoveTowards(Vector3 targetPos, MinerState nextState)
    {
        // Ép cứng Y và Z của nhân vật, chỉ cho phép đi theo trục X tới mục tiêu
        Vector3 targetPosXOnly = new Vector3(targetPos.x, transform.position.y, transform.position.z);

        transform.position = Vector3.MoveTowards(transform.position, targetPosXOnly, moveSpeed * Time.deltaTime);

        float dist = Vector3.Distance(transform.position, targetPosXOnly);
        // Kiểm tra xem đã đến nơi chưa
        if (dist < 0.01f)
        {
            Debug.Log("Đã đến đích! Chuyển sang trạng thái: " + nextState);
            ChangeState(nextState);
        }
    }

    private void HandleDigging()
    {
        currentDigTime += Time.deltaTime;

        if (currentDigTime >= digTime)
        {
            Debug.Log("Đào xong! Chuẩn bị đi về.");
            // Đào xong
            currentDigTime = 0f;
            ChangeState(MinerState.WalkingBack);
        }
    }

    private void ChangeState(MinerState newState)
    {
        Debug.Log($"Đổi trạng thái: {currentState} -> {newState}");
        currentState = newState;

        Vector3 currentScale = transform.localScale;

        switch (currentState)
        {
            case MinerState.Idle:
                // Trở về tới startPos
                if (anim != null) anim.SetTrigger("idle"); 
                
                // Hướng mặt về bên phải (x = dương)
                currentScale.x = Mathf.Abs(initialScale.x);
                transform.localScale = currentScale;
                
                // Cộng tài nguyên vào hầm
                if (currentShaft != null)
                {
                    // Lượng tài nguyên = ResourcePerSecond * thời gian đào
                    double resourceGathered = currentShaft.ResourcePerSecond * digTime;
                    currentShaft.AddResource(resourceGathered);
                    Debug.Log($"Đã cộng {resourceGathered} vào hầm.");
                }
                break;

            case MinerState.WalkingToDig:
                if (anim != null) anim.SetTrigger("walk");
                // Hướng mặt về bên phải (đi tới mỏ) (x = dương)
                currentScale.x = Mathf.Abs(initialScale.x);
                transform.localScale = currentScale;
                break;

            case MinerState.Digging:
                if (anim != null) anim.SetTrigger("digup");
                if (progressBar != null)
                {
                    progressBar.StartLoading(digTime);
                }
                break;

            case MinerState.WalkingBack:
                if (anim != null) anim.SetTrigger("walk");
                // Lật ngược hình ảnh để đi về (x = âm)
                currentScale.x = -Mathf.Abs(initialScale.x);
                transform.localScale = currentScale;
                break;
        }
    }
}
