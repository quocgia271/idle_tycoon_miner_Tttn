using UnityEngine;

public class DragonFireball : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    public bool isBigFireball = false; 
    public float burnDuration = 30f; // Sẽ được ghi đè trong Start
    [HideInInspector] public Transform targetShaft; // Mục tiêu bay tới
    
    [Header("VFX")]
    public GameObject explosionVFXPrefab; // Kéo prefab hiệu ứng nổ vào đây

    void Start()
    {
        // Ghi đè cứng thời gian cháy chuẩn (15s nhỏ, 25s to)
        burnDuration = isBigFireball ? 25f : 15f;
    }

    void Update()
    {
        if (targetShaft != null)
        {
            // Homing: Bay đuổi theo mục tiêu
            Vector3 direction = (targetShaft.position - transform.position).normalized;
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
            
            // Xoay đầu đạn về phía mục tiêu
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            
            // Kiểm tra khoảng cách để kích hoạt nổ (chạm đúng tâm thay vì viền collider)
            if (Vector3.Distance(transform.position, targetShaft.position) < 0.5f)
            {
                MineShaft shaft = targetShaft.GetComponent<MineShaft>();
                if (shaft != null)
                {
                    TriggerHit(shaft);
                }
            }
        }
        else
        {
            // Dự phòng nếu không có mục tiêu: bay thẳng xuống
            transform.Translate(Vector3.down * speed * Time.deltaTime, Space.Self);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (targetShaft != null) return; // Nếu có mục tiêu thì đã check bằng Distance trong Update

        MineShaft shaft = collision.GetComponent<MineShaft>();
        if (shaft != null)
        {
            TriggerHit(shaft);
        }
    }

    private void TriggerHit(MineShaft shaft)
    {
        // 1. KIỂM TRA HẦM BỊ KHÓA:
        ShaftUnlocker unlocker = shaft.GetComponentInChildren<ShaftUnlocker>(true);
        if (unlocker != null && unlocker.gameObject.activeInHierarchy)
        {
            return;
        }

        Debug.Log($"Đạn rồng bắn trúng hầm: {shaft.gameObject.name}");

        // 3. TẠO HIỆU ỨNG NỔ:
        if (explosionVFXPrefab != null)
        {
            GameObject vfx = PoolManager.Instance != null 
                ? PoolManager.Instance.Spawn(explosionVFXPrefab, transform.position, Quaternion.identity)
                : Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
                
            if (vfx.GetComponent<AutoDespawn>() == null)
            {
                vfx.AddComponent<AutoDespawn>().lifetime = 2f;
            }
        }

        // 4. ÁP DỤNG TRẠNG THÁI LỬA GIẢM NĂNG SUẤT HẦM:
        shaft.TriggerBurnVFX(burnDuration, isBigFireball);

        if (isBigFireball)
        {
            Debug.Log($"Hầm {shaft.gameObject.name} bị trúng đạn bự và bốc cháy lớn trong {burnDuration} giây!");
        }
        else
        {
            Debug.Log($"Hầm {shaft.gameObject.name} trúng đạn thường, cháy nhỏ trong {burnDuration} giây.");
        }

        if (PoolManager.Instance != null) PoolManager.Instance.Despawn(gameObject); else Destroy(gameObject);
    }
}
