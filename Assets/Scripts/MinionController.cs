using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening; // Thêm thư viện DOTween

public class MinionController : MonoBehaviour, IDamageable
{
    [Header("Components")]
    public Animator anim;
    public MineShaft targetShaft;
    public Slider healthSlider;
    
    [Header("VFX")]
    public GameObject normalAttackVFX;
    public GameObject specialAttackVFX;

    [Header("Animation Settings")]
    public string idleStateName = "idle";
    public string deadTriggerName = "dead";
    public string attackTriggerName = "attack";
    public string specialTriggerName = "special";

    [Header("Settings")]
    public float maxHealth = 100f;
    public float MaxHealth => maxHealth;
    public float attackInterval = 3f;
    public float normalDamage = 10f;
    public float specialDamage = 30f;
    public float destroyDelayAfterDeath = 2f;
    public float fadeInDuration = 0.5f; // Thời gian Fade in

    [Header("Damage Colors")]
    public Color normalDamageColor = Color.white;
    public Color specialDamageColor = Color.yellow;

    private float currentHealth;
    private float attackTimer;
    private int attackCount = 0;
    private bool isDead = false;
    public bool IsDead => isDead;

    private void OnEnable()
    {
        currentHealth = maxHealth;
        isDead = false;
        attackCount = 0;
        attackTimer = attackInterval;
        
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
            healthSlider.gameObject.SetActive(true);
        }

        if (anim != null) 
        {
            // RESET TOÀN BỘ ANIMATOR VỀ TRẠNG THÁI GỐC CỦA PREFAB
            // Khắc phục triệt để lỗi kẹt Animation (VD kẹt tàng hình của state chết) khi tái sử dụng Object
            anim.Rebind(); 
            anim.Update(0f); 
            anim.Play(idleStateName);
        }

        // Gọi hàm Fade In khi xuất hiện
        DoFadeIn();
    }

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void DoFadeIn()
    {
        // 0. Reset lại scale gốc (đề phòng trước đó lỡ bị scale về 0)
        if (baseScale != Vector3.zero) 
        {
            transform.localScale = baseScale;
        }
        else 
        {
            transform.localScale = Vector3.one;
        }

        // Quét toàn bộ Renderer (kể cả những cái bị tắt ẩn)
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            // 1. Ép bật lại phòng trường hợp Animation Chết ở lần trước đã tắt chúng đi
            r.gameObject.SetActive(true);
            r.enabled = true;

            // 2. Nếu là Sprite 2D thì làm hiệu ứng mờ dần
            if (r is SpriteRenderer sr)
            {
                sr.DOKill(); // HỦY BỎ TẤT CẢ các lệnh mờ dần của lần chết trước (tránh lỗi kẹt Alpha = 0)
                Color c = sr.color;
                c.a = 0f;
                sr.color = c;
                sr.DOFade(1f, fadeInDuration);
            }
        }
    }

    private void Update()
    {
        if (isDead || targetShaft == null) return;

        // Nếu hầm bị sập (hết sức bền) thì yêu quái hoàn thành nhiệm vụ -> tự lăn ra chết và ẩn đi
        if (targetShaft.isBroken) 
        {
            Die();
            return;
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0)
        {
            PerformAttack();
            attackTimer = attackInterval;
        }
    }

    private void PerformAttack()
    {
        attackCount++;
        if (attackCount % 10 == 0)
        {
            if (anim != null) anim.SetTrigger(specialTriggerName);
        }
        else
        {
            if (anim != null) anim.SetTrigger(attackTriggerName);
        }
    }

    // ==========================================
    // ANIMATION EVENTS - Gắn các hàm này vào event trong Animation
    // ==========================================

    public void OnNormalAttackHit()
    {
        if (isDead || targetShaft == null) return;

        if (normalAttackVFX != null)
        {
            normalAttackVFX.SetActive(false);
            normalAttackVFX.SetActive(true);
            
            ParticleSystem[] pss = normalAttackVFX.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in pss)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }

        targetShaft.AddEndurance(-normalDamage, normalDamageColor);
    }

    public void OnSpecialAttackHit()
    {
        if (isDead || targetShaft == null) return;

        if (specialAttackVFX != null)
        {
            specialAttackVFX.SetActive(false);
            specialAttackVFX.SetActive(true);
            
            ParticleSystem[] pss = specialAttackVFX.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in pss)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
        }

        targetShaft.AddEndurance(-specialDamage, specialDamageColor);
    }

    // ==========================================
    // LOGIC NHẬN SÁT THƯƠNG (Interface IDamageable gọi vào)
    // ==========================================
    
    public bool IsInvincible => false;
    
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        
        // TẮT CHỨC NĂNG CLICK: Khóa Collider lại để không nhận click chuột nữa
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Debug.Log($"[MinionController] Yêu quái chết! Đang cố gắng Play State có tên là: '{deadTriggerName}'");
        
        if (anim != null) 
        {
            anim.Play(deadTriggerName); 
        }
        
        if (healthSlider != null) healthSlider.gameObject.SetActive(false);
        
        StartCoroutine(DeactivateAfterDeath(destroyDelayAfterDeath));
    }

    private IEnumerator DeactivateAfterDeath(float delay)
    {
        // Chờ thời gian delay (để xem animation nằm gục xuống)
        yield return new WaitForSeconds(delay);
        
        // HỦY BỎ HOÀN TOÀN thay vì ẩn đi. 
        // Đây là cách duy nhất trị triệt để lỗi của Animator tải từ mạng về.
        Destroy(gameObject); 
    }
}
