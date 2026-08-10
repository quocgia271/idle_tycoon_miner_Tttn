using UnityEngine;

/// <summary>
/// Gắn script này vào các Prefab VFX (Miner chết, Đạn rồng nổ, Sập hầm, Skill của Boss).
/// Cứ khi nào VFX hiện lên (được Instantiate hoặc SetActive), âm thanh sẽ tự động kêu 1 lần.
/// Tách biệt hoàn toàn khỏi Logic Code của game.
/// </summary>
public class VFXAudioTrigger : MonoBehaviour
{
    [Tooltip("Âm thanh sẽ phát ra khi VFX này xuất hiện")]
    public AudioClip sfxClip;

    private void OnEnable()
    {
        // Chống lỗi PoolManager khởi tạo sẵn (Pre-warm) ở frame đầu tiên gây phát âm thanh
        if (Time.timeSinceLevelLoad < 0.5f) return;

        if (AudioManager.Instance != null && sfxClip != null)
        {
            AudioManager.Instance.PlaySFX(sfxClip);
        }
    }
}
