using UnityEngine;
using DG.Tweening;

public class WarehouseProjectile : MonoBehaviour
{
    private Transform target;
    private float damage;
    public float flyDuration = 0.5f;
    public float curveHeight = 1f; 
    public GameObject hitVFXPrefab; 
    [Tooltip("Chỉnh sửa góc xoay cơ bản nếu hình ảnh đạn bị ngược (ví dụ: -90, 90, 180)")]
    public float rotationOffset = 0f;
    [Tooltip("Độ dời tâm ngắm: Dùng để chỉnh điểm chạm đạn (ví dụ y=1 để đạn cắm vào giữa ngực Boss thay vì dưới chân)")]
    public Vector3 targetOffset = Vector3.zero;

    [Header("Physics Feel")]
    [Tooltip("Đường cong tốc độ bay. Để bớt cứng đờ, đạn nên bay nhanh lúc đầu và chậm dần (hoặc ngược lại).")]
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 startPos;
    private float timeElapsed = 0f;
    private bool isFlying = false;
    private float dynamicCurveHeight; // Biến lưu độ bổng ngẫu nhiên của mỗi viên đạn

    public void Setup(Transform targetTransform, float dmg)
    {
        Setup(targetTransform, dmg, targetOffset);
    }

    public void Setup(Transform targetTransform, float dmg, Vector3 specificOffset)
    {
        target = targetTransform;
        damage = dmg;
        targetOffset = specificOffset;

        if (target != null)
        {
            startPos = transform.position;
            // Cho phép đạn bay bổng ngẫu nhiên lên trên hoặc xuống dưới để các góc bắn đa dạng
            dynamicCurveHeight = UnityEngine.Random.Range(-curveHeight, curveHeight * 1.5f);
            isFlying = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!isFlying) return;

        // Nếu Boss đột ngột chết hoặc biến mất giữa chừng thì tự hủy đạn
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        timeElapsed += Time.deltaTime;
        float rawT = Mathf.Clamp01(timeElapsed / flyDuration);
        
        // Đưa t qua Animation Curve để tạo cảm giác gia tốc vật lý (không bị trượt đều cứng đờ)
        float t = speedCurve.Evaluate(rawT);

        // Tính điểm điều khiển (giữa đường nhưng bổng ngẫu nhiên)
        Vector3 p0 = startPos;
        // Lấy vị trí tâm mục tiêu cộng thêm Offset để bắn trúng bụng/đầu thay vì chân
        Vector3 p2 = target.position + targetOffset; 
        Vector3 p1 = p0 + (p2 - p0) / 2f + (Vector3.up * dynamicCurveHeight); 

        // Công thức Bezier Curve
        float u = 1 - t;
        Vector3 currentPos = (u * u * p0) + (2 * u * t * p1) + (t * t * p2);

        // Xoay đầu đạn theo đúng hướng nó đang bay (Cảm giác vật lý như phóng lao)
        Vector3 moveDir = currentPos - transform.position;
        if (moveDir != Vector3.zero)
        {
            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle + rotationOffset);
        }

        transform.position = currentPos;

        if (t >= 1f)
        {
            isFlying = false;
            OnHitTarget();
        }
    }

    private void OnHitTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            // Sinh ra hiệu ứng nổ tại vị trí viên đạn chạm mục tiêu
            if (hitVFXPrefab != null)
            {
                GameObject hitVfx = Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);
                hitVfx.SetActive(true); // Bật nó lên trong trường hợp Prefab gốc bị tắt (Inactive)
            }

            EnemyClickReceiver receiver = target.GetComponent<EnemyClickReceiver>();
            if (receiver == null)
            {
                receiver = target.GetComponentInChildren<EnemyClickReceiver>();
            }

            if (receiver != null)
            {
                // Gọi hiệu ứng giật lùi
                receiver.PlayHitFeedback();
                
                // Aggro Logic: Nếu bắn trúng Boss 3, chọc giận nó!
                BossPhase3Controller boss3 = target.GetComponentInParent<BossPhase3Controller>();
                if (boss3 == null) boss3 = target.GetComponentInChildren<BossPhase3Controller>();
                if (boss3 != null)
                {
                    boss3.IncreaseBarrierAggro();
                }

                // Trừ máu và văng số sát thương (popup)
                IDamageable damageable = target.GetComponent<IDamageable>();
                if (damageable == null) damageable = target.GetComponentInChildren<IDamageable>();
                
                if (damageable != null)
                {
                    damageable.TakeDamage(damage);
                }
                
                receiver.SpawnDamagePopup(damage);
            }
            else
            {
                // Fallback nếu không có EnemyClickReceiver
                BossHealth boss = target.GetComponent<BossHealth>();
                if (boss != null)
                {
                    boss.TakeDamage(damage);
                    BossPhase3Controller boss3 = target.GetComponent<BossPhase3Controller>();
                    if (boss3 != null) boss3.IncreaseBarrierAggro();
                }
                else
                {
                    BossHealth dragonHealth = target.GetComponentInChildren<BossHealth>();
                    if (dragonHealth != null) dragonHealth.TakeDamage(damage);
                }
            }
        }
        
        Destroy(gameObject);
    }
}
