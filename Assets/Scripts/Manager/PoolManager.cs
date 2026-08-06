using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    [System.Serializable]
    public class PoolConfig
    {
        [Tooltip("Kéo thả Prefab Đạn/VFX vào đây")]
        public GameObject prefab;
        [Tooltip("Số lượng tạo sẵn ban đầu để chống lag")]
        public int prewarmCount = 10;
    }

    [Header("Manual Pool Configuration")]
    [Tooltip("Danh sách các kho đạn bạn muốn khai báo thủ công. Mở ra và kéo prefab vào.")]
    public List<PoolConfig> predefinedPools = new List<PoolConfig>();

    // Dictionary mapping a Prefab's instance ID to its queue of pooled objects
    private Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();

    // Dictionary mapping an active GameObject's instance ID back to its original Prefab's instance ID
    // This allows us to know which pool a GameObject belongs to when returning it
    private Dictionary<int, int> spawnedObjectsMapping = new Dictionary<int, int>();

    private void Start()
    {
        // Khởi tạo sẵn (Pre-warm) các kho đạn do người dùng kéo thả thủ công trên Inspector
        foreach (var config in predefinedPools)
        {
            if (config.prefab == null) continue;

            int prefabId = config.prefab.GetInstanceID();
            if (!poolDictionary.ContainsKey(prefabId))
            {
                poolDictionary.Add(prefabId, new Queue<GameObject>());
            }

            for (int i = 0; i < config.prewarmCount; i++)
            {
                GameObject obj = Instantiate(config.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                
                spawnedObjectsMapping.Add(obj.GetInstanceID(), prefabId);
                poolDictionary[prefabId].Enqueue(obj);
            }
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ PoolManager xuyên suốt các màn chơi
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Lấy một object từ pool hoặc tạo mới nếu pool trống.
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[PoolManager] Prefab is null!");
            return null;
        }

        int prefabId = prefab.GetInstanceID();

        // Tạo queue mới nếu chưa có kho cho loại prefab này
        if (!poolDictionary.ContainsKey(prefabId))
        {
            poolDictionary.Add(prefabId, new Queue<GameObject>());
        }

        Queue<GameObject> queue = poolDictionary[prefabId];
        GameObject objToSpawn = null;

        // Tìm một object đang rảnh rỗi trong kho
        while (queue.Count > 0 && objToSpawn == null)
        {
            objToSpawn = queue.Dequeue();
        }

        if (objToSpawn == null)
        {
            // Nếu kho trống, tạo object mới
            objToSpawn = Instantiate(prefab);
            // Ghi nhớ nguồn gốc của object này để sau này cất đúng kho
            spawnedObjectsMapping.Add(objToSpawn.GetInstanceID(), prefabId);
            
            // Đưa vào trong hierarchy của PoolManager cho gọn gàng (tùy chọn)
            objToSpawn.transform.SetParent(transform);
        }

        // Cập nhật vị trí, góc xoay và bật nó lên
        objToSpawn.transform.position = position;
        objToSpawn.transform.rotation = rotation;
        objToSpawn.SetActive(true);

        return objToSpawn;
    }

    /// <summary>
    /// Thu hồi một object về lại kho thay vì Destroy.
    /// </summary>
    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        int objId = obj.GetInstanceID();

        if (spawnedObjectsMapping.ContainsKey(objId))
        {
            int prefabId = spawnedObjectsMapping[objId];

            if (poolDictionary.ContainsKey(prefabId))
            {
                obj.SetActive(false);
                poolDictionary[prefabId].Enqueue(obj);
            }
            else
            {
                // Fallback (hiếm khi xảy ra)
                Destroy(obj);
            }
        }
        else
        {
            // Nếu vật thể này không được sinh ra từ PoolManager, thì cứ Destroy bình thường
            Debug.LogWarning($"[PoolManager] {obj.name} was not spawned by PoolManager, destroying normally.");
            Destroy(obj);
        }
    }
}
