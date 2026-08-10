using UnityEngine;
using DG.Tweening;

/// <summary>
/// Gắn script này vào Prefab UI Image (bàn tay hoặc mũi tên chỉ vào nút).
/// Nó sẽ tự động tạo animation co giãn liên tục (giống như click) mỗi khi được bật lên.
/// </summary>
public class ClickIndicatorAnimation : MonoBehaviour
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Sequence animSequence;
    private Vector2 originalPos;
    private bool posSaved = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Tự động lấy hoặc thêm CanvasGroup để làm hiệu ứng Fade (mờ dần)
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        originalPos = rectTransform.anchoredPosition;
        posSaved = true;
    }

    void OnEnable()
    {
        if (rectTransform == null) return;
        
        if (!posSaved)
        {
            originalPos = rectTransform.anchoredPosition;
            posSaved = true;
        }

        // Bắt đầu từ dưới lên 80 pixel để quãng đường trượt dài và mượt hơn
        Vector2 startPos = originalPos + new Vector2(0, -80f); 
        
        // Trạng thái khởi tạo: Tàng hình, nằm bên dưới, hơi thu nhỏ
        rectTransform.localScale = Vector3.one * 0.8f;
        rectTransform.anchoredPosition = startPos;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
        
        animSequence = DOTween.Sequence();
        
        // Nhịp 1: Trồi lên, to ra và Fade In cực kỳ mượt mà
        animSequence.Append(canvasGroup.DOFade(1f, 0.5f).SetEase(Ease.OutQuad));
        animSequence.Join(rectTransform.DOScale(1f, 0.5f).SetEase(Ease.OutBack));
        animSequence.Join(rectTransform.DOAnchorPos(originalPos, 0.5f).SetEase(Ease.OutCubic));
        
        // Chờ 0.1s trước khi bấm
        animSequence.AppendInterval(0.1f);

        // Nhịp 2: Nhấn vào nút (Lực nhấn MẠNH HƠN, dứt khoát hơn)
        animSequence.Append(rectTransform.DOScale(0.7f, 0.1f).SetEase(Ease.InCubic));
        
        // Nhịp 3: Trì (giữ nút) lâu hơn 1 chút tạo cảm giác nhấn có sức nặng
        animSequence.AppendInterval(0.2f);
        
        // Nhịp 4: Nhả tay ra (Nảy lên)
        animSequence.Append(rectTransform.DOScale(1.15f, 0.15f).SetEase(Ease.OutBack));
        animSequence.Append(rectTransform.DOScale(1.0f, 0.15f).SetEase(Ease.OutQuad));
        
        // Nhịp 5: Nán lại lâu hơn để thu hút ánh nhìn
        animSequence.AppendInterval(0.7f);

        // Nhịp 6: Fade Out (mờ đi) và hơi trượt xuống lại
        animSequence.Append(canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InQuad));
        animSequence.Join(rectTransform.DOAnchorPos(startPos, 0.3f).SetEase(Ease.InQuad));
        
        // Nhịp 7: Chờ 1 giây rồi lặp lại
        animSequence.AppendInterval(1f);

        animSequence.SetLoops(-1, LoopType.Restart)
                    .SetUpdate(true);
    }

    void OnDisable()
    {
        animSequence?.Kill();
        if (rectTransform != null) 
        {
            rectTransform.localScale = Vector3.one;
            if (posSaved) rectTransform.anchoredPosition = originalPos;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }
}
