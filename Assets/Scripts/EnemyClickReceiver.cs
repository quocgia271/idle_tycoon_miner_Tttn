using UnityEngine;
using System.Collections.Generic;
using DG.Tweening; // Thêm thư viện DOTween

public class EnemyClickReceiver : MonoBehaviour
{
    [Header("Click Attack Settings")]
    public float clickDamage = 10f;
    public List<GameObject> clickVFXPrefabs; 
    public float clickVFXRandomRadius = 0.5f; 

    private IDamageable damageableTarget;
    private Vector3 originalScale;

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

        // 0. Hiệu ứng bị đẩy lùi (Hit Feedback)
        // Lệnh DOKill(true) sẽ ép hiệu ứng trước đó phải nhảy về ngay trạng thái kết thúc (không bị lệch tọa độ khi bấm quá nhanh)
        transform.DOKill(true); 
        
        // DOPunchPosition: Bị đẩy lùi nhẹ về phía sau (trục X) 0.05 đơn vị rồi lập tức nảy về chỗ cũ trong 0.15s
        transform.DOPunchPosition(new Vector3(0.05f, 0, 0), 0.15f, 1, 0f);

        // 1. Gửi lệnh Trừ máu cho script chính (Không cần quan tâm nó là Minion hay Rồng hay Boss)
        if (damageableTarget != null)
        {
            damageableTarget.TakeDamage(clickDamage);
        }

        // 2. Chọn ngẫu nhiên 1 VFX và sinh ra tại vị trí chuột + độ lệch nhỏ
        if (clickVFXPrefabs != null && clickVFXPrefabs.Count > 0)
        {
            GameObject chosenVFX = clickVFXPrefabs[Random.Range(0, clickVFXPrefabs.Count)];
            
            if (chosenVFX != null)
            {
                Vector3 touchPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                touchPos.z = transform.position.z; 
                
                Vector2 randomOffset = Random.insideUnitCircle * clickVFXRandomRadius;
                touchPos.x += randomOffset.x;
                touchPos.y += randomOffset.y;
                
                GameObject vfx = Instantiate(chosenVFX, touchPos, Quaternion.identity);
                Destroy(vfx, 2f);
            }
        }
    }
}
