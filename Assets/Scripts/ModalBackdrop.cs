using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Image))]
public class ModalBackdrop : MonoBehaviour
{
    private Image backdropImage;
    public float targetAlpha = 0.6f;
    public float fadeDuration = 0.25f;

    void Awake()
    {
        backdropImage = GetComponent<Image>();
    }

    void OnEnable()
    {
        if (backdropImage != null)
        {
            // Reset alpha to 0
            Color c = backdropImage.color;
            c.a = 0f;
            backdropImage.color = c;

            // Fade in to target alpha
            backdropImage.DOFade(targetAlpha, fadeDuration).SetUpdate(true);
        }
    }
}
