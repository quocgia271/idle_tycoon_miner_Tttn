using UnityEngine;
using DG.Tweening;

public class BirdProjectile : MonoBehaviour
{
    private Transform target;
    private MinionController targetMinion;
    public float flyDuration = 0.5f;
    public float curveHeight = 1f; 
    public GameObject hitVFXPrefab; 
    [Tooltip("Chỉnh sửa góc xoay cơ bản nếu hình ảnh đạn bị ngược (ví dụ: -90, 90, 180)")]
    public float rotationOffset = 0f;
    [Tooltip("Độ dời tâm ngắm: Dùng để chỉnh điểm chạm đạn (ví dụ y=1 để đạn cắm vào giữa người quái thay vì chân)")]
    public Vector3 targetOffset = Vector3.zero;

    [Header("Physics Feel")]
    [Tooltip("Đường cong tốc độ bay. Để bớt cứng đờ, đạn nên bay nhanh lúc đầu và chậm dần (hoặc ngược lại).")]
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 startPos;
    private float timeElapsed = 0f;
    private bool isFlying = false;
    private float dynamicCurveHeight; 

    public void Setup(Transform targetTransform, MinionController minion)
    {
        target = targetTransform;
        targetMinion = minion;

        if (target != null)
        {
            startPos = transform.position;
            
            // Bay bổng ngẫu nhiên như đạn nhà kho (để quỹ đạo ngoằn ngoèo tự do)
            dynamicCurveHeight = UnityEngine.Random.Range(-curveHeight, curveHeight * 1.5f);

            // Xoay đầu đạn ngay lập tức hướng về phía mục tiêu ngay từ lúc vừa sinh ra
            Vector3 diff = (target.position + targetOffset) - startPos;
            float initialAngle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, initialAngle + rotationOffset);

            isFlying = true;
        }
        else
        {
            if (PoolManager.Instance != null) PoolManager.Instance.Despawn(gameObject); else Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!isFlying) return;

        if (target == null || !target.gameObject.activeInHierarchy || (targetMinion != null && targetMinion.IsDead))
        {
            if (PoolManager.Instance != null) PoolManager.Instance.Despawn(gameObject); else Destroy(gameObject);
            return;
        }

        timeElapsed += Time.deltaTime;
        float rawT = Mathf.Clamp01(timeElapsed / flyDuration);
        
        // Dùng lại AnimationCurve giống hệt WarehouseProjectile
        float t = speedCurve.Evaluate(rawT);

        Vector3 p0 = startPos;
        Vector3 p2 = target.position + targetOffset; 
        Vector3 p1 = p0 + (p2 - p0) / 2f + (Vector3.up * dynamicCurveHeight); 

        float u = 1 - t;
        Vector3 currentPos = (u * u * p0) + (2 * u * t * p1) + (t * t * p2);

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
        if (targetMinion != null && targetMinion.gameObject.activeInHierarchy && !targetMinion.IsDead)
        {
            if (hitVFXPrefab != null)
            {
                GameObject hitVfx = PoolManager.Instance != null
                    ? PoolManager.Instance.Spawn(hitVFXPrefab, transform.position, Quaternion.identity)
                    : Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);
                hitVfx.SetActive(true);
                
                if (hitVfx.GetComponent<AutoDespawn>() == null)
                {
                    hitVfx.AddComponent<AutoDespawn>().lifetime = 1.5f;
                }
            }

            targetMinion.TakeDamage(9999f); // Kill minion
        }

        if (PoolManager.Instance != null) PoolManager.Instance.Despawn(gameObject); else Destroy(gameObject);
    }
}
