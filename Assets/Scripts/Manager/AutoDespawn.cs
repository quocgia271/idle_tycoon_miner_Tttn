using UnityEngine;

public class AutoDespawn : MonoBehaviour
{
    public float lifetime = 2f; // Thời gian sống mặc định
    private float timer;

    private void OnEnable()
    {
        timer = lifetime;
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (PoolManager.Instance != null)
            {
                PoolManager.Instance.Despawn(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
