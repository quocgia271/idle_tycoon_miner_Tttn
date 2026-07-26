using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening; // Thêm thư viện DOTween vào đây để hết báo lỗi

public class BossHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    public float maxHealth = 1000f;
    private float currentHealth;

    [Header("UI References")]
    public Slider healthSlider; // Kéo thanh máu World Space vào đây

    [Header("Events (Optional)")]
    // Bạn có thể kéo thả các hàm chết của Rồng/Boss vào đây trên Inspector
    public UnityEvent OnDeathEvent; 

    [Header("Procedural Death Effect")]
    public bool useProceduralDeath = true; // Bật tắt hiệu ứng chết tự động
    public float deathDuration = 1.5f;     // Thời gian giãy giụa
    public GameObject deathVFX;            // Kéo cái VFX (đang bị tắt) đã nằm sẵn bên trong con Boss/Rồng vào đây!

    private bool isDead = false;

    private void OnEnable()
    {
        // Reset máu mỗi khi Boss/Rồng xuất hiện lại
        currentHealth = maxHealth;
        isDead = false;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
            healthSlider.gameObject.SetActive(true);
        }
        
        // Bật lại Collider để nhận click
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;
    }

    public bool IsInvincible { get; set; } = false;

    // Hàm này được tự động gọi bởi EnemyClickReceiver hoặc sau này là Trụ bắn đạn
    public void TakeDamage(float amount)
    {
        if (isDead || IsInvincible) return;

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

        // Tắt khả năng nhận click (không cho bấm văng VFX nữa)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Ẩn thanh máu đi
        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(false);
        }

        // Tắt Animator để con Boss đứng hình (đóng băng) ngay lập tức
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;

        // Gọi các sự kiện chết bổ sung (nếu có cài trên Inspector)
        OnDeathEvent?.Invoke();

        Debug.Log($"[{gameObject.name}] Đã bị tiêu diệt!");

        if (useProceduralDeath)
        {
            StartCoroutine(ProceduralDeathRoutine());
        }
        else
        {
            Destroy(gameObject, 0.5f);
        }
    }

    private System.Collections.IEnumerator ProceduralDeathRoutine()
    {
        // 1. Xóa mọi hiệu ứng DOTween đang dở dang (ví dụ bị đẩy lùi)
        transform.DOKill();

        // 2. Ép chớp đỏ liên tục (Nếu là 2D Sprite)
        SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs)
        {
            sr.DOColor(Color.red, 0.15f).SetLoops(-1, DG.Tweening.LoopType.Yoyo);
        }

        // 3. Rung lắc nhẹ nhàng như đang hấp hối (giảm độ mạnh từ 0.5 xuống 0.1)
        // Lưu ý: tắt chức năng snapping để không làm dịch chuyển vị trí gốc
        transform.DOShakePosition(deathDuration, new Vector3(0.1f, 0.1f, 0), 15, 90, false, false);

        // Đợi rung lắc xong
        yield return new WaitForSeconds(deathDuration);

        // 4. Ẩn hoàn toàn hình ảnh con Boss đi (tàng hình)
        foreach (var sr in srs)
        {
            sr.enabled = false;
        }

        // Bật Collider lên thành Trigger, hoặc tắt luôn (đã tắt ở trên rồi nên không sao)
        
        // 5. Bật VFX Nổ lên (VẪN GIỮ NGUYÊN NÓ LÀ CON CỦA BOSS)
        if (deathVFX != null)
        {
            deathVFX.SetActive(true);
            // Đợi 3 giây cho VFX chạy hết vòng đời của nó
            yield return new WaitForSeconds(3f); 
        }

        // 6. Sau khi VFX bay xong hết thì mới Xóa sổ hoàn toàn cái xác
        Destroy(gameObject);
    }
}
