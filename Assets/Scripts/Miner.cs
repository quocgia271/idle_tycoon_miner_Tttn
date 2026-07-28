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
        if (currentState == MinerState.Idle && currentShaft != null && currentShaft.currentManager != null && !currentShaft.isBroken)
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
                if (anim != null) anim.SetTrigger("idle"); 
                
                // Hướng mặt về bên phải (góc xoay Y = 0)
                transform.rotation = Quaternion.Euler(0, 0, 0);
                
                if (currentShaft != null)
                {
                    double resourceGathered = currentShaft.ResourcePerSecond * digTime;
                    currentShaft.AddResource(resourceGathered);
                }
                break;

            case MinerState.WalkingToDig:
                if (anim != null) anim.SetTrigger("walk");
                // Hướng mặt về bên phải (góc xoay Y = 0)
                transform.rotation = Quaternion.Euler(0, 0, 0);
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
                // Lật ngược hình ảnh bằng cách xoay 180 độ trục Y
                transform.rotation = Quaternion.Euler(0, 180, 0);
                break;
        }

        // CHỐNG LỖI UI BỊ KÉO DÀI VÔ TẬN:
        // Vì ta dùng Rotation Y 180 độ để lật Miner thay vì Scale âm,
        // Scale sẽ luôn dương -> Không bao giờ bị lỗi nháy UI!
        // Giờ chỉ cần xoay Canvas UI ngược lại 180 độ để chữ/thanh máu không bị ngược chiều
        
        bool isFlipped = transform.rotation.eulerAngles.y > 90f;
        
        if (moraleBar != null)
        {
            moraleBar.transform.localRotation = Quaternion.Euler(0, isFlipped ? 180f : 0f, 0);
        }
        
        if (progressBar != null)
        {
            progressBar.transform.localRotation = Quaternion.Euler(0, isFlipped ? 180f : 0f, 0);
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
            scaleSeq.SetTarget(transform); // <--- Quan trọng: Gắn target để lúc Revive gọi DOKill() nó sẽ chết
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
        // Tránh gọi lặp lại nếu đã khỏe mạnh
        if (healthState == HealthState.Normal && morale >= maxMorale) return;

        bool wasDead = (healthState == HealthState.Dead);
        
        healthState = HealthState.Normal;
        morale = maxMorale;
        UpdateMoraleUI();
        
        // Tắt hết VFX
        if (hurtVFXList != null)
        {
            foreach (var vfx in hurtVFXList) if (vfx != null) vfx.SetActive(false);
        }
        
        if (deathVFX != null) 
        {
            deathVFX.SetActive(false);
            deathVFX.transform.SetParent(this.transform, false);
            deathVFX.transform.localPosition = originalDeathVFXPos;
            deathVFX.transform.localScale = originalDeathVFXScale;
        }
        
        transform.DOKill();
        if (spriteRenderer != null) spriteRenderer.DOKill();
        
        StartCoroutine(ReviveAnimationRoutine(wasDead));
    }

    // Thanh tẩy mượt mà: Giải độc và hồi một lượng nhỏ tinh thần (VD: 25)
    public void Cleanse()
    {
        if (healthState != HealthState.Injured) return; // Chỉ thanh tẩy người đang bị thương
        
        healthState = HealthState.Normal;
        
        // Hồi lại một chút tinh thần cho thợ mỏ (25)
        AddMorale(25f);
        SpawnHealPopup(25f);
        
        // Fade out mượt mà các VFX độc
        if (hurtVFXList != null)
        {
            foreach (var vfx in hurtVFXList)
            {
                if (vfx != null && vfx.activeInHierarchy)
                {
                    StartCoroutine(FadeOutVFXSmoothly(vfx));
                }
            }
        }
        
        // Từ từ trả lại màu gốc (hết nháy đỏ/tím)
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
            spriteRenderer.DOColor(Color.white, 0.5f);
        }
    }

    private void SpawnHealPopup(float amount)
    {
        if (currentShaft != null && currentShaft.damagePopupPrefab != null)
        {
            // Spawn popup trên đầu thợ mỏ một chút
            DamagePopup popup = DamagePopup.Create(currentShaft.damagePopupPrefab, transform.position + Vector3.up * 0.8f, transform, DamagePopup.PopupSourceType.Mineshaft);
            popup.Setup(amount, 0.1f, Color.yellow, true);
        }
    }

    private System.Collections.IEnumerator FadeOutVFXSmoothly(GameObject vfxObject)
    {
        SpriteRenderer[] srs = vfxObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs)
        {
            sr.DOFade(0f, 0.5f);
        }

        ParticleSystem[] pSystems = vfxObject.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in pSystems)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        yield return new WaitForSeconds(1f);
        
        foreach (var sr in srs)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }
        vfxObject.SetActive(false);
    }

    private System.Collections.IEnumerator ReviveAnimationRoutine(bool wasDead)
    {
        currentState = MinerState.Idle; // Tạm dừng mọi hoạt động
        if (anim != null) anim.SetTrigger("idle");
        
        // Nếu đang bay lơ lửng, làm mờ đi ngay tại chỗ nó đang bay
        if (wasDead && spriteRenderer != null)
        {
            yield return spriteRenderer.DOFade(0f, 0.4f).WaitForCompletion();
        }

        // Bật lại vật lý và animation
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
        if (anim != null) anim.enabled = true;
        
        // Đưa về Hầm (set parent, position, scale)
        if (currentShaft != null) transform.SetParent(currentShaft.transform, true);
        transform.localScale = initialScale;
        transform.position = startPos.position;
        transform.rotation = Quaternion.identity;
        
        // Hiệu ứng Fade In và Nảy lên mừng rỡ
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            Color c = Color.white;
            c.a = 0f;
            spriteRenderer.color = c;
            
            spriteRenderer.DOFade(1f, 0.5f);
        }

        // Animation nhảy tưng tưng + lộn vòng
        Sequence bounceSeq = DOTween.Sequence();
        bounceSeq.SetTarget(transform);
        bounceSeq.Append(transform.DOMoveY(startPos.position.y + 1f, 0.25f).SetEase(Ease.OutQuad));
        bounceSeq.Append(transform.DOMoveY(startPos.position.y, 0.25f).SetEase(Ease.InQuad));
        bounceSeq.Append(transform.DOMoveY(startPos.position.y + 0.5f, 0.2f).SetEase(Ease.OutQuad));
        bounceSeq.Append(transform.DOMoveY(startPos.position.y, 0.2f).SetEase(Ease.InQuad));
        
        transform.DORotate(new Vector3(0, 0, 360), 0.9f, RotateMode.FastBeyond360).SetEase(Ease.OutBack);

        yield return bounceSeq.WaitForCompletion();
        
        // Reset lại vòng lặp làm việc bình thường (Vì đang là Idle nên Update sẽ cho tự đi làm)
        
        if (MoraleModalUI.Instance != null && MoraleModalUI.Instance.gameObject.activeInHierarchy)
        {
            MoraleModalUI.Instance.RefreshList();
        }
    }
}
