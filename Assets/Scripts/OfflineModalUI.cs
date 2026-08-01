using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class OfflineModalUI : MonoBehaviour
{
    public static OfflineModalUI Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI offlineTimeText; 
    public TextMeshProUGUI earnedCashText;  
    public Button claimButton;              

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(CloseModal);
        }
        
        gameObject.SetActive(false);
    }

    public void ShowOfflineRewards(long secondsOffline, double cashEarned)
    {
        gameObject.SetActive(true);

        // --- DOTween Animation ---
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        RectTransform rect = GetComponent<RectTransform>();

        // Kill mọi animation cũ đang chạy để tránh đụng độ
        DOTween.Kill(cg);
        DOTween.Kill(rect);

        cg.alpha = 0f;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -150f);
        
        cg.DOFade(1f, 0.35f).SetUpdate(true).SetEase(Ease.OutQuad);
        rect.DOAnchorPosY(0f, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);
        // -------------------------

        if (offlineTimeText != null)
        {
            long hours = secondsOffline / 3600;
            long minutes = (secondsOffline % 3600) / 60;
            long seconds = secondsOffline % 60;

            if (hours > 0)
            {
                offlineTimeText.text = $"Bạn đã đi vắng {hours} giờ {minutes} phút {seconds} giây";
            }
            else if (minutes > 0)
            {
                offlineTimeText.text = $"Bạn đã đi vắng {minutes} phút {seconds} giây";
            }
            else
            {
                offlineTimeText.text = $"Bạn đã đi vắng {seconds} giây";
            }
        }

        if (earnedCashText != null)
        {
            earnedCashText.text = "+" + CurrencyFormatter.FormatMoney(cashEarned);
        }
    }

    public void CloseModal()
    {
        CanvasGroup cg = GetComponent<CanvasGroup>();
        RectTransform rect = GetComponent<RectTransform>();

        if (cg != null && rect != null)
        {
            // Kill mọi animation cũ đang chạy để tránh đụng độ
            DOTween.Kill(cg);
            DOTween.Kill(rect);

            cg.DOFade(0f, 0.25f).SetUpdate(true).SetEase(Ease.OutQuad);
            rect.DOAnchorPosY(-150f, 0.25f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => {
                gameObject.SetActive(false);
            });
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
