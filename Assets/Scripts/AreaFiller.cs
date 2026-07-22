using System.Collections.Generic;
using UnityEngine;

public class AreaFiller : MonoBehaviour
{
    [Header("Cài đặt Prefab")]
    [Tooltip("Kéo Prefab ảnh/đất đá của bạn vào đây, code sẽ tự động đo kích thước của nó!")]
    public GameObject itemPrefab;

    [Header("Tùy chọn Mật độ")]
    [Tooltip("Số lượng mảnh TỐI THIỂU bạn muốn rải ra")]
    public int minItems = 12;
    [Tooltip("Số lượng mảnh TỐI ĐA bạn muốn rải ra")]
    public int maxItems = 15;
    
    [Tooltip("Khoảng cách đệm giữa các mảnh. Để số âm (VD: -0.1) nếu muốn chúng khít vào nhau hơn.")]
    public float padding = 0f;

    [Header("Tùy chọn Khác")]
    [Tooltip("Xoay ngẫu nhiên các mảnh đất đá để nhìn tự nhiên hơn")]
    public bool randomRotation = true;
    [Tooltip("Nếu tick, nó sẽ tự động lấp đầy khi vừa chạy game")]
    public bool fillOnStart = false;

    private SpriteRenderer backgroundSprite;

    private void Start()
    {
        if (fillOnStart)
        {
            FillRandomly();
        }
    }

    private bool TryGetAreaSize(out Vector2 areaSize, out Vector3 centerPos, out Bounds bounds)
    {
        if (backgroundSprite == null)
            backgroundSprite = GetComponent<SpriteRenderer>();

        if (backgroundSprite != null)
        {
            bounds = backgroundSprite.bounds;
            areaSize = new Vector2(bounds.size.x, bounds.size.y);
            centerPos = bounds.center;
            return true;
        }
        else
        {
            Debug.LogError("Không tìm thấy SpriteRenderer trên GameObject này!");
            areaSize = Vector2.zero;
            centerPos = transform.position;
            bounds = new Bounds();
            return false;
        }
    }

    private bool TryGetItemSize(out Vector2 itemSize)
    {
        if (itemPrefab == null)
        {
            itemSize = Vector2.zero;
            return false;
        }

        SpriteRenderer sr = itemPrefab.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Vector2 spriteSize = sr.sprite.bounds.size; 
            Vector3 scale = itemPrefab.transform.localScale; 
            itemSize = new Vector2(Mathf.Abs(spriteSize.x * scale.x), Mathf.Abs(spriteSize.y * scale.y));
            return true;
        }

        itemSize = Vector2.one; 
        return true;
    }

    [ContextMenu("Lấp đầy vùng (Fill Area Randomly)")]
    public void FillRandomly()
    {
        if (itemPrefab == null)
        {
            Debug.LogError("Chưa gắn Item Prefab!");
            return;
        }

        Bounds areaBounds;
        if (!TryGetAreaSize(out Vector2 areaSize, out Vector3 centerPos, out areaBounds)) return;
        if (!TryGetItemSize(out Vector2 itemSize)) return;

        // Dùng bán kính trung bình để các mảnh có thể xếp khít vào nhau hơn (thay vì Max)
        float itemCollisionRadius = ((itemSize.x + itemSize.y) / 4f) + padding;

        List<Vector2> placedPositions = new List<Vector2>();

        float minX = areaBounds.min.x + itemSize.x / 2f;
        float maxX = areaBounds.max.x - itemSize.x / 2f;
        float minY = areaBounds.min.y + itemSize.y / 2f;
        float maxY = areaBounds.max.y - itemSize.y / 2f;

        if (minX > maxX || minY > maxY)
        {
            Debug.LogError("Kích thước cục đất lớn hơn cả cái hầm!");
            return;
        }

        int failedAttempts = 0;
        int maxSequentialFails = 1500; // Tăng số lần thử lên cực cao để AI cố gắng luồn lách tìm chỗ trống
        int targetItems = Random.Range(minItems, maxItems + 1); // Chọn ngẫu nhiên số lượng cần sinh

        while (placedPositions.Count < targetItems && failedAttempts < maxSequentialFails)
        {
            float randomX = Random.Range(minX, maxX);
            float randomY = Random.Range(minY, maxY);
            Vector2 candidatePos = new Vector2(randomX, randomY);

            bool isOverlapping = false;
            foreach (Vector2 pos in placedPositions)
            {
                if (Vector2.Distance(candidatePos, pos) < (itemCollisionRadius * 2f)) 
                {
                    isOverlapping = true;
                    break;
                }
            }

            if (!isOverlapping)
            {
                placedPositions.Add(candidatePos);
                failedAttempts = 0; 
            }
            else
            {
                failedAttempts++;
            }
        }

        foreach (Vector2 pos in placedPositions)
        {
            Quaternion rotation = Quaternion.identity;
            if (randomRotation)
            {
                rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            }

            GameObject obj = Instantiate(itemPrefab, pos, rotation);
            obj.transform.SetParent(transform); 
            obj.transform.localScale = itemPrefab.transform.localScale;
        }

        if (backgroundSprite != null)
        {
            backgroundSprite.enabled = false; 
        }

        if (placedPositions.Count < targetItems)
        {
            Debug.LogWarning($"Đã cố gắng rải nhưng chỉ xếp được {placedPositions.Count}/{targetItems} mảnh vì hết chỗ trống. Nếu muốn nhiều hơn, hãy giảm Padding hoặc làm hầm to ra.");
        }
        else
        {
            Debug.Log($"Đã rải ngẫu nhiên {placedPositions.Count} mảnh! Bố cục hoàn toàn tự nhiên không lặp lại.");
        }
    }

    [ContextMenu("Xóa toàn bộ (Clear All)")]
    public void ClearAll()
    {
        if (backgroundSprite == null) backgroundSprite = GetComponent<SpriteRenderer>();
        if (backgroundSprite != null) backgroundSprite.enabled = true;

        int childCount = transform.childCount;
        for (int i = childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        Bounds areaBounds;
        if (TryGetAreaSize(out Vector2 areaSize, out Vector3 centerPos, out areaBounds))
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(centerPos, new Vector3(areaSize.x, areaSize.y, 0));
        }
    }
}
