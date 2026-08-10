using UnityEngine;

/// <summary>
/// Gắn script này vào các Prefab VFX Lửa (Lửa cháy nhỏ, lửa cháy to của rồng đốt).
/// Khi có bất kỳ ngọn lửa nào bốc lên, nó báo AudioManager bật tiếng lửa loop dưới nền.
/// Khi tất cả ngọn lửa tắt đi, âm thanh tự động ngắt.
/// </summary>
public class FireAudioTrigger : MonoBehaviour
{
    private void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.AddFireCount();
        }
    }

    private void OnDisable()
    {
        // Bắt sự kiện khi VFX bị tắt đi (SetActive(false) hoặc bị Destroy)
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.RemoveFireCount();
        }
    }
}
