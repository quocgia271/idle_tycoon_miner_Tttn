using UnityEngine;
using System.Collections.Generic;
using DG.Tweening; // Thêm thư viện DOTween

public class EnemyClickReceiver : MonoBehaviour
{
    [Header("Click Attack Settings")]
    public float clickDamage = 10f;
    
    [Header("Damage Popup")]
    public DamagePopup damagePopupPrefab;
    [Tooltip("Vị trí sinh ra số sát thương. Nếu để trống sẽ lấy tâm của vật.")]
    public Transform popupSpawnPoint;

    private IDamageable damageableTarget;
    private Vector3 originalScale;

    [Header("Hit Feedback Settings")]
    public float punchCooldown = 0.15f; // Thời gian giãn cách giữa các lần giật hình (tránh kẹt Boss)
    private float lastPunchTime = 0f;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        // Đảm bảo mỗi lần xuất hiện đều giữ đúng kích thước chuẩn
        if (originalScale != Vector3.zero) 
        {
            transform.localScale = originalScale;
        }
    }

    private void Start()
    {
        // Tự động tìm kiếm bất kỳ Script nào có kế thừa IDamageable nằm trên cùng GameObject này
        damageableTarget = GetComponent<IDamageable>();
        
        if (damageableTarget == null)
        {
            Debug.LogWarning($"[EnemyClickReceiver] Cảnh báo: '{gameObject.name}' không có script nào kế thừa IDamageable (như MinionController, BossController) để nhận sát thương!");
        }
    }

    private void Update()
    {
        // Khi người chơi bấm chuột trái (hoặc chạm tay vào màn hình)
        if (Input.GetMouseButtonDown(0))
        {
            // Chuyển tọa độ chuột trên màn hình thành tọa độ trong game 2D
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            
            // Lấy cái Collider 2D của con yêu quái này
            Collider2D col = GetComponent<Collider2D>();
            
            // Nếu con chuột đang nằm TRONG VÙNG của cái Collider đó -> Nghĩa là bấm trúng!
            if (col != null && col.OverlapPoint(mouseWorldPos))
            {
                ExecuteClick();
            }
        }
    }

    private void ExecuteClick()
    {
        if (damageableTarget != null && damageableTarget.IsInvincible) return;

        // 0. Hiệu ứng bị đẩy lùi (Hit Feedback) - Có Cooldown để tránh kẹt hình Boss khi spam click
        if (Time.time - lastPunchTime > punchCooldown)
        {
            lastPunchTime = Time.time;
            transform.DOKill(true); 
            transform.DOPunchPosition(new Vector3(0.05f, 0, 0), 0.15f, 1, 0f);
        }

        // 1. Gửi lệnh Trừ máu cho script chính (Không cần quan tâm nó là Minion hay Rồng hay Boss)
        if (damageableTarget != null)
        {
            damageableTarget.TakeDamage(clickDamage);
        }

        // 2. Tạo Floating Text từ Object Pool để tránh giật lag máy
        if (damagePopupPrefab != null)
        {
            DamagePopup popup;
            if (popupSpawnPoint != null)
            {
                popup = DamagePopup.Create(damagePopupPrefab, popupSpawnPoint.position, popupSpawnPoint);
            }
            else
            {
                popup = DamagePopup.Create(damagePopupPrefab, transform.position, null);
            }
            popup.Setup(clickDamage);
        }
    }
}
