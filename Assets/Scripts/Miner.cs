using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using DG.Tweening;
public class Miner : MonoBehaviour
{
    public enum MinerState
    {
        Idle,
        WalkingToDig,
        Digging,
        WalkingBack
    }

    public enum HealthState
    {
        Normal,
        Injured,
        Dead
    }

    [Header("References")]
    public MineShaft currentShaft;
    public Animator anim;
    
    [Header("Positions")]
    public Transform startPos;
    public Transform digPos;

    // Tự động lấy tốc độ chạy và tốc độ đào từ Hầm mỏ để đồng bộ Level và Buff
    public float moveSpeed => currentShaft != null ? currentShaft.GetMinerMoveSpeed(currentShaft.Level) : 2f;
    public float digTime => currentShaft != null ? currentShaft.GetMinerDigTime(currentShaft.Level) : 2f;

    [Header("UI")]
    public ProgressBar progressBar; // Kéo thả cục Prefab chứa script ProgressBar vào đây
    public ProgressBar moraleBar; // Thanh UI dưới chân nhân vật (cần tạo thêm ProgressBar mới)

    [Header("Morale System")]
    public float morale = 100f;
    public float maxMorale = 100f;
    public HealthState healthState = HealthState.Normal;

    [Header("Boss Phase 2")]
    public List<GameObject> hurtVFXList; // Thêm danh sách VFX cho Miner
    public GameObject deathVFX; // VFX biến mất khi bị hút
    private BossPhase2Controller currentBoss;
    private float dotTimer = 0f;
    private SpriteRenderer spriteRenderer;

    private MinerState currentState = MinerState.Idle;
    private float currentDigTime = 0f;
    private Vector3 initialScale;
    private Vector3 originalDeathVFXPos;
    private Vector3 originalDeathVFXScale;

    private void Start()
    {
        initialScale = transform.localScale;
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (deathVFX != null) 
        {
            originalDeathVFXPos = deathVFX.transform.localPosition;
            originalDeathVFXScale = deathVFX.transform.localScale;
        }

        // Gọi ngay animation Idle lúc vừa vào game
        if (anim != null) 
        {
            anim.SetTrigger("idle");
        }

        // Khởi tạo thanh tinh thần lúc bắt đầu
        UpdateMoraleUI();
    }

    public void AddMorale(float amount)
    {
        morale = Mathf.Clamp(morale + amount, 0f, maxMorale);
        UpdateMoraleUI();
    }

    public void UpdateMoraleUI()
    {
        if (moraleBar != null)
        {
            moraleBar.SetProgress(morale / maxMorale);
        }
    }

    private void Update()
    {
        HandleHauntDOT();

        // LOGIC BẮT CLICK KIỂU MỚI (Xuyên qua các Collider cản đường)
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Collider2D col = GetComponent<Collider2D>();
            
            if (col != null && col.OverlapPoint(mouseWorldPos))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

                Debug.Log("Miner clicked via OverlapPoint! Current state: " + currentState);
                if (currentState == MinerState.Idle && currentShaft != null && !currentShaft.isBroken)
                {
                    ChangeState(MinerState.WalkingToDig);
                }
            }
        }

        // NẾU ĐÃ CHẾT THÌ KHÔNG LÀM GÌ NỮA CẢ (tránh đè lên logic bay lượn DOTween)
        if (healthState == HealthState.Dead) return;

        // Tự động di chuyển nếu hầm có người quản lý và hầm không bị vỡ
        if (currentState == MinerState.Idle && currentShaft != null && currentShaft.currentManager != null && healthState != HealthState.Injured && !currentShaft.isBroken)
        {
            ChangeState(MinerState.WalkingToDig);
        }

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

    // --- LOGIC BOSS PHASE 2 ---
    public void ApplyHaunt(BossPhase2Controller boss)
    {
        if (healthState == HealthState.Injured) return; // Đang bị ám/chấn thương rồi thì thôi

        healthState = HealthState.Injured;
        currentBoss = boss;
        
        // Cập nhật lại UI bảng MoraleModal nếu bảng đang mở
        if (MoraleModalUI.Instance != null && MoraleModalUI.Instance.gameObject.activeInHierarchy)
        {
            // Tạm thời gọi trực tiếp hoặc thiết kế 1 event, ở đây ta gọi UI tự refresh bằng cách gọi hàm Refresh (tuy nhiên private, nên tạm cập nhật UI nếu cần)
        }

        // Bật ngẫu nhiên 1 VFX (đã tắt hết các cái cũ)
        if (hurtVFXList != null && hurtVFXList.Count > 0)
        {
            foreach (var vfx in hurtVFXList) if (vfx != null) vfx.SetActive(false);
            
            int rndIndex = Random.Range(0, hurtVFXList.Count);
            if (hurtVFXList[rndIndex] != null) hurtVFXList[rndIndex].SetActive(true);
        }

        // Nhấp nháy đỏ liên tục
        if (spriteRenderer != null)
        {
            spriteRenderer.DOColor(Color.red, 0.5f).SetLoops(-1, LoopType.Yoyo);
        }
    }

    private void HandleHauntDOT()
    {
        if (healthState == HealthState.Injured && currentBoss != null)
        {
            dotTimer += Time.deltaTime;
            if (dotTimer >= currentBoss.dotInterval)
            {
                dotTimer = 0f;
                // Trừ tinh thần dựa trên thiết lập của Boss
                morale -= currentBoss.dotDamage;
                UpdateMoraleUI();
                
                if (morale <= 0f)
                {
                    DieFromHaunt();
                }
            }
        }
    }
    
    private void DieFromHaunt()
    {
        healthState = HealthState.Dead;
        morale = 0f;
        currentState = MinerState.Idle;
        if (anim != null) anim.enabled = false;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill(); // Ngừng nháy đỏ
            spriteRenderer.color = Color.white;
        }

        // Bay vào hố đen (suckTargetPos của Boss)
        if (currentBoss != null && currentBoss.suckTargetPos != null)
        {
            // Tách thợ mỏ ra khỏi hầm để không bị ảnh hưởng bởi tọa độ và scale của hầm
            transform.SetParent(null, true);

            // Tắt Collider để không bị kẹt vật lý
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            // KHÓA TRỤC Z: Đảm bảo thợ mỏ luôn cùng độ sâu với màn hình, không bị chìm ra sau background
            Vector3 targetPos = new Vector3(currentBoss.suckTargetPos.position.x, currentBoss.suckTargetPos.position.y, transform.position.z);

            // 1. Hiệu ứng Scale 3D: Phóng to ra một chút (cảm giác bay lại gần màn hình) rồi mới teo lại về 0
            Sequence scaleSeq = DOTween.Sequence();
            // Dùng InOutSine cho cả hai nhịp để chuyển giao scale cực kỳ mềm mại không bị khựng
            scaleSeq.Append(transform.DOScale(initialScale * 1.3f, 0.9f).SetEase(Ease.InOutSine)); 
            scaleSeq.Append(transform.DOScale(Vector3.zero, 0.9f).SetEase(Ease.InOutSine)); 

            // 2. Hiệu ứng Đường bay Parabol đa hướng
            Vector3 startPos = transform.position;
            Vector3 midPoint = startPos + (targetPos - startPos) / 2f;
            midPoint.y += 1.5f; // Giảm độ cao xuống một chút cho đỡ gắt
            midPoint.x += Random.Range(-1f, 1f); // Thu hẹp phạm vi lệch trái/phải lại

            Vector3[] path = new Vector3[] { midPoint, targetPos };

            // Bay mượt mà theo đường cong Bezier trong 1.8 giây (chậm lại một chút để cảm nhận độ mượt)
            transform.DOPath(path, 1.8f, PathType.CatmullRom).SetEase(Ease.InOutQuad).OnComplete(() =>
            {
                if (spriteRenderer != null) spriteRenderer.enabled = false; // Tàng hình
                
                // Xử lý VFX biến mất
                if (deathVFX != null)
                {
                    deathVFX.transform.SetParent(null, true); // Tách ra ngoài
                    deathVFX.transform.position = targetPos; // Đặt đúng vị trí lỗ đen
                    deathVFX.transform.localScale = originalDeathVFXScale; // Trả lại kích thước gốc (vì Miner đã teo = 0)
                    deathVFX.SetActive(false);
                    deathVFX.SetActive(true);
                }
            });
        }
        else
        {
            // Nếu không có tâm hút thì tàng hình luôn
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            
            // Bật VFX biến mất
            if (deathVFX != null)
            {
                deathVFX.SetActive(false);
                deathVFX.SetActive(true);
            }
        }

        // Nếu bảng Morale đang mở, yêu cầu nó Refresh để cập nhật chữ "Đã chết"
        if (MoraleModalUI.Instance != null && MoraleModalUI.Instance.gameObject.activeInHierarchy)
        {
            MoraleModalUI.Instance.RefreshList();
        }
    }

    public void Revive()
    {
        healthState = HealthState.Normal;
        morale = maxMorale;
        UpdateMoraleUI();
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
        }
        
        if (anim != null) anim.enabled = true;
        
        // Tắt hết VFX
        if (hurtVFXList != null)
        {
            foreach (var vfx in hurtVFXList) if (vfx != null) vfx.SetActive(false);
        }
        
        if (deathVFX != null) 
        {
            deathVFX.SetActive(false);
            // Gắn lại VFX làm con của Miner và đưa về vị trí ban đầu
            deathVFX.transform.SetParent(this.transform, false);
            deathVFX.transform.localPosition = originalDeathVFXPos;
            deathVFX.transform.localScale = originalDeathVFXScale;
        }
        
        transform.DOKill();
        
        // Gắn lại thợ mỏ vào hầm
        if (currentShaft != null) transform.SetParent(currentShaft.transform, true);
        
        transform.localScale = initialScale;
        transform.position = startPos.position;
        transform.rotation = Quaternion.identity; // Reset góc xoay
        
        // Cập nhật lại UI Morale nếu đang mở
        if (MoraleModalUI.Instance != null && MoraleModalUI.Instance.gameObject.activeInHierarchy)
        {
            MoraleModalUI.Instance.RefreshList();
        }
    }
}
