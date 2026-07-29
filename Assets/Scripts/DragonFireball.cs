using UnityEngine;

public class DragonFireball : MonoBehaviour
{
    public float speed = 10f;
    public float damage = 10f;
    public bool isBigFireball = false; 
    public float burnDuration = 5f; // Thời gian hầm bị cháy khi trúng đạn bự
    
    [Header("VFX")]
    public GameObject explosionVFXPrefab; // Kéo prefab hiệu ứng nổ vào đây

    void Update()
    {
        // Đạn luôn bay xuống theo trục nội bộ
        transform.Translate(Vector3.down * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem đạn có va chạm trúng hầm không
        MineShaft shaft = collision.GetComponent<MineShaft>();
        
        if (shaft != null)
        {
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

            // 4. TRỪ MÁU TRỰC TIẾP VÀ BẬT HIỆU ỨNG CHÁY HẦM:
            // Sát thương từ đạn của Rồng được cân bằng dựa trên maxEndurance của hầm
            float scaledDamage = shaft.maxEndurance * 0.3f; // Bắn bay 30% máu
            if (isBigFireball) scaledDamage = shaft.maxEndurance * 0.8f; // Chiêu cuối bắn 80% máu
            
            // Sát thương từ đạn của Rồng luôn luôn màu Đỏ
            shaft.AddEndurance(-scaledDamage, Color.red);

            // Đạn bự hay nhỏ đều kích hoạt cháy hầm, thời gian cháy lấy từ biến burnDuration
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
