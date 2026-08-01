using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BarrierController : MonoBehaviour
{
    public enum BarrierType { Elevator, Warehouse }
    
    [Header("Settings")]
    public BarrierType type;
    public float duration = 10f;
    public float slowMultiplier = 0.15f; // Tăng hiệu lực làm chậm lên 85%

    [Header("Elevator Specific")]
    public float elevatorSlowDuration = 5f; // Thời gian thang máy bị chậm sau khi chạm

    [Header("Warehouse Stun Logic")]
    public float timeBeforeStun = 2f; // Phải ở trong màn chắn bao lâu thì mới bị choáng
    public float stunDuration = 1.5f; // Bị choáng bao lâu

    private int activeEffects = 0;
    private bool timeExpired = false;

    public float currentTimer = 0f;

    private void OnEnable()
    {
        activeEffects = 0;
        timeExpired = false;
        
        if (currentTimer <= 0) currentTimer = duration;

        // Reset lại hiển thị và collider (phòng trường hợp trước đó Elevator chạm vào làm ẩn đi)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = true;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        // Nếu là Warehouse Barrier, Boss và Rồng sẽ trở nên bất tử
        if (type == BarrierType.Warehouse)
        {
            SetInvincibilityForAllBosses(true);
        }

        // Tự động tắt sau X giây (kết hợp chờ dư âm)
        StartCoroutine(LifeTimerRoutine());
    }

    private void OnDisable()
    {
        currentTimer = 0f; // Đảm bảo reset khi tắt
        // Khi tắt Warehouse Barrier, tắt bất tử
        if (type == BarrierType.Warehouse)
        {
            SetInvincibilityForAllBosses(false);
            
            // Trả lại tốc độ bình thường cho Worker nếu màn chắn tự động hết hạn mà Worker vẫn đang đi bên trong
            if (trappedWorker != null)
            {
                trappedWorker.currentSpeedMultiplier = 1f;
                trappedWorker = null;
            }
        }
    }

    private IEnumerator LifeTimerRoutine()
    {
        while (currentTimer > 0)
        {
            currentTimer -= Time.deltaTime;
            yield return null;
        }
        timeExpired = true;
        CheckAndDeactivate();
    }

    private void CheckAndDeactivate()
    {
        // Chỉ thực sự tắt GameObject đi khi: 
        // 1. Đã hết thời gian tồn tại cơ bản (duration)
        // 2. KHÔNG CÒN hiệu ứng dư âm (làm chậm) nào đang diễn ra
        if (timeExpired && activeEffects <= 0)
        {
            gameObject.SetActive(false);
        }
    }

    private void SetInvincibilityForAllBosses(bool isInvincible)
    {
        BossHealth[] allBosses = FindObjectsOfType<BossHealth>(true);
        foreach (var boss in allBosses)
        {
            if (boss != null)
            {
                boss.IsInvincible = isInvincible;
            }
        }
    }

    private WarehouseWorker trappedWorker;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (type == BarrierType.Elevator)
        {
            Elevator elevator = collision.GetComponentInParent<Elevator>();
            if (elevator != null)
            {
                // Làm chậm thang máy trong 1 khoảng thời gian rồi phục hồi
                StartCoroutine(SlowElevatorRoutine(elevator));
                
                // Ẩn Màn chắn đi ngay lập tức (nhưng KHÔNG tắt SetActive false vì sẽ làm chết Coroutine phục hồi tốc độ)
                Renderer[] renderers = GetComponentsInChildren<Renderer>();
                foreach (var r in renderers) r.enabled = false;
                
                Collider2D col = GetComponent<Collider2D>();
                if (col != null) col.enabled = false;
            }
        }
        else if (type == BarrierType.Warehouse)
        {
            WarehouseWorker worker = collision.GetComponentInParent<WarehouseWorker>();
            if (worker != null)
            {
                // Giảm tốc độ ngay lập tức khi chạm (KHÔNG ẨN MÀN CHẮN)
                trappedWorker = worker;
                worker.currentSpeedMultiplier = slowMultiplier;
                
                // Bắt đầu đếm giờ để gây choáng nếu ở trong màn chắn quá lâu
                StartCoroutine(WarehouseStunRoutine(worker));
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (type == BarrierType.Warehouse)
        {
            WarehouseWorker worker = collision.GetComponentInParent<WarehouseWorker>();
            if (worker != null && worker == trappedWorker)
            {
                // Trả lại tốc độ bình thường khi Worker ĐI XUYÊN QUA KHỎI màn chắn
                worker.currentSpeedMultiplier = 1f;
                trappedWorker = null;
            }
        }
    }

    private IEnumerator WarehouseStunRoutine(WarehouseWorker worker)
    {
        // Chờ 1 khoảng thời gian xem có thoát ra kịp không
        float timer = 0;
        while (timer < timeBeforeStun)
        {
            if (worker == null || worker != trappedWorker) yield break; // Hủy nếu worker đã ra ngoài
            timer += Time.deltaTime;
            yield return null;
        }

        // Bắt đầu choáng (đứng im)
        if (worker != null && worker == trappedWorker)
        {
            worker.currentSpeedMultiplier = 0f; 
            
            yield return new WaitForSeconds(stunDuration);
            
            // Hết choáng, trả lại tốc độ chậm (nếu vẫn còn ở trong)
            if (worker != null && worker == trappedWorker)
            {
                worker.currentSpeedMultiplier = slowMultiplier;
            }
        }
    }

    private IEnumerator SlowElevatorRoutine(Elevator elevator)
    {
        if (elevator == null) yield break;
        
        activeEffects++;
        elevator.ElevatorMoveSpeedBuff = slowMultiplier;
        
        // Bật VFX dư âm lên
        if (elevator.slowVFX != null)
        {
            elevator.slowVFX.SetActive(false);
            elevator.slowVFX.SetActive(true);
        }
        
        yield return new WaitForSeconds(elevatorSlowDuration);
        
        if (elevator != null)
        {
            elevator.ElevatorMoveSpeedBuff = 1f;
            // Tắt VFX dư âm đi
            if (elevator.slowVFX != null)
            {
                elevator.slowVFX.SetActive(false);
            }
        }
        
        activeEffects--;
        CheckAndDeactivate();
    }
}
