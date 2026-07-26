using UnityEngine;
using DG.Tweening;

public class UIButtonPulse : MonoBehaviour
{
    [Header("Pulse Settings")]
    [Tooltip("Độ phóng to (1.05 = to hơn 5%)")]
    public float scaleMultiplier = 1.05f; 
    
    [Tooltip("Thời gian phình to ra (giây)")]
    public float duration = 0.5f; 

    private Vector3 originalScale;

    private void Awake()
    {
        // Lưu lại kích thước gốc của nút bấm để lúc tắt hiệu ứng nó không bị biến dạng
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        // Bắt đầu hiệu ứng thở: Phóng to -> Thu nhỏ liên tục (Yoyo), lặp mãi mãi (-1)
        transform.DOScale(originalScale * scaleMultiplier, duration)
                 .SetLoops(-1, LoopType.Yoyo)
                 .SetEase(Ease.InOutSine);
    }

    private void OnDisable()
    {
        // Quan trọng: Khi nút bị ẩn đi (SetActive(false)), phải DỪNG hiệu ứng DOTween lại
        // và trả kích thước về nguyên trạng để tránh lỗi scale to dần đều
        transform.DOKill();
        transform.localScale = originalScale;
    }
}
