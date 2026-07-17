using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Button))]
public class ButtonAnimator : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private Vector3 originalScale;
    public float scaleFactor = 0.9f;
    public float animationDuration = 0.1f;
    private Button button;

    void Awake()
    {
        originalScale = transform.localScale;
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button != null && button.interactable)
        {
            transform.DOScale(originalScale * scaleFactor, animationDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button != null && button.interactable)
        {
            transform.DOScale(originalScale, animationDuration).SetEase(Ease.OutBounce).SetUpdate(true);
        }
    }
}
