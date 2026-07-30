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
        // Ghi đè cứng thời gian cháy chuẩn (30s nhỏ, 45s to)
        burnDuration = isBigFireball ? 45f : 30f;
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
        }
        else
        {
            // Dự phòng nếu không có mục tiêu: bay thẳng xuống
            transform.Translate(Vector3.down * speed * Time.deltaTime, Space.Self);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem đạn có va chạm trúng hầm không
        MineShaft shaft = collision.GetComponent<MineShaft>();
        
        if (shaft != null)
        {
            // Nếu đạn có mục tiêu cụ thể, bỏ qua tất cả các hầm khác trên đường bay
            if (targetShaft != null && shaft.transform != targetShaft)
            {
                return;
            }

            // 1. KIỂM TRA HẦM BỊ KHÓA:
            // Tìm cục khóa (ShaftUnlocker) nằm trong hầm
            ShaftUnlocker unlocker = shaft.GetComponentInChildren<ShaftUnlocker>(true);
            // Nếu cục khóa đang hiện hình (active) nghĩa là hầm chưa được mở -> Bay xuyên qua luôn
            if (unlocker != null && unlocker.gameObject.activeInHierarchy)
            {
                return; // Thoát hàm, không tính va chạm, để đạn bay tiếp
            }

            // 2. KIỂM TRA HẦM BỊ PHÁ HỦY CHƯA:
            // GIẢ SỬ hầm ngục có biến kiểm tra bể như isDestroyed. Bạn tự gắn logic này của bạn vào nhé.
            /*
            if (shaft.isDestroyed) 
            {
                return; // Hầm đã bể thì bay xuyên qua
            }
            */

            // Đạn đã chạm đúng hầm mục tiêu hợp lệ!
            Debug.Log($"Đạn rồng bắn trúng hầm: {shaft.gameObject.name}");

            // 3. TẠO HIỆU ỨNG NỔ:
            if (explosionVFXPrefab != null)
            {
                // Sinh ra vfx nổ ngay vị trí của viên đạn
                GameObject vfx = Instantiate(explosionVFXPrefab, transform.position, Quaternion.identity);
                
                // Tự động xóa vfx nổ sau 2 giây để tránh rác game
                Destroy(vfx, 2f); 
            }

            // 4. ÁP DỤNG TRẠNG THÁI LỬA GIẢM NĂNG SUẤT HẦM:
            // Đạn bự hay nhỏ đều kích hoạt cháy hầm, giảm năng suất theo cơ chế mới
            shaft.TriggerBurnVFX(burnDuration, isBigFireball);

            if (isBigFireball)
            {
                Debug.Log($"Hầm {shaft.gameObject.name} bị trúng đạn bự và bốc cháy lớn trong {burnDuration} giây!");
            }
            else
            {
                Debug.Log($"Hầm {shaft.gameObject.name} trúng đạn thường, cháy nhỏ trong {burnDuration} giây.");
            }

            // Cuối cùng: Hủy viên đạn ngay khi nó va chạm thành công
            Destroy(gameObject);
        }
    }
}
