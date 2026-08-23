using UnityEngine;
using UnityEngine.UI;

public class ShinyEffectController : MonoBehaviour
{
    private SpriteRenderer[] spriteRenderers;
    private Image[] uiImages;
    private MaterialPropertyBlock propBlock;
    private bool isShiny = false;
    private bool isInitialized = false;

    private void Initialize()
    {
        if (isInitialized) return;
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        uiImages = GetComponentsInChildren<Image>(true);
        propBlock = new MaterialPropertyBlock();
        isInitialized = true;
        
        Debug.Log($"[ShinyEffect] Khởi tạo trên {gameObject.name}. Tìm thấy {spriteRenderers.Length} SpriteRenderer và {uiImages.Length} Image.");
    }

    private void Awake()
    {
        Initialize();
    }

    public void SetShiny(bool enabled)
    {
        if (isShiny == enabled) return;
        isShiny = enabled;

        Initialize(); // Đảm bảo luôn được khởi tạo dù Awake chưa chạy

        float shinyVal = enabled ? 1f : 0f;

        Debug.Log($"[ShinyEffect] {gameObject.name} chuyển trạng thái Shiny thành: {enabled}");

        if (spriteRenderers != null)
        {
            foreach (var sr in spriteRenderers)
            {
                if (sr != null)
                {
                    sr.GetPropertyBlock(propBlock);
                    propBlock.SetFloat("_ShinyEnabled", shinyVal);
                    sr.SetPropertyBlock(propBlock);
                }
            }
        }

        if (uiImages != null)
        {
            foreach (var img in uiImages)
            {
                if (img != null && img.material != null)
                {
                    if (img.material.HasProperty("_ShinyEnabled"))
                    {
                        img.material.SetFloat("_ShinyEnabled", shinyVal);
                    }
                }
            }
        }
    }
}
